using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

public static class RadarAtlasBaker
{
	private struct Kahan
	{
		public double Sum;

		public double Correction;

		public void Add(double value)
		{
			double y = value - Correction;
			double t = Sum + y;
			Correction = (t - Sum) - y;
			Sum = t;
		}
	}

	public const double SurfaceOffsetFactor = 1E-07;

	public static int DefaultThreads => Math.Max(1, Environment.ProcessorCount - 1);

	public static RadarAtlasCellResult BakeCell(RadarScene scene, double wavenumber, RadarAtlasSpec spec, int i, int j, bool frozen, CancellationToken token, ref long prepareTicks, ref long integralTicks, RadarCoating coating = null)
	{
		RadarCounters counters = new RadarCounters();
		long start = Stopwatch.GetTimestamp();
		RadarPoPrepared prepared = null;
		if (frozen)
		{
			RadarDirectionBasis basis = RadarDirection.BasisDegrees((i + 0.5) * spec.DeltaDeg - 180.0, (j + 0.5) * spec.DeltaDeg - 90.0);
			prepared = RadarFacetPO.Prepare(scene, basis, scene.BoundingSphereRadius * SurfaceOffsetFactor, counters, token);
		}
		prepareTicks += Stopwatch.GetTimestamp() - start;
		return IntegrateCell(scene, wavenumber, spec, i, j, prepared, counters, token, ref prepareTicks, ref integralTicks, coating);
	}

	private static RadarPoPrepared PrepareCentre(RadarScene scene, RadarAtlasSpec spec, int i, int j, RadarCounters counters, CancellationToken token)
	{
		RadarDirectionBasis basis = RadarDirection.BasisDegrees((i + 0.5) * spec.DeltaDeg - 180.0, (j + 0.5) * spec.DeltaDeg - 90.0);
		return RadarFacetPO.Prepare(scene, basis, scene.BoundingSphereRadius * SurfaceOffsetFactor, counters, token);
	}

	private static RadarAtlasCellResult IntegrateCell(RadarScene scene, double wavenumber, RadarAtlasSpec spec, int i, int j, RadarPoPrepared frozenPrepared, RadarCounters counters, CancellationToken token, ref long prepareTicks, ref long integralTicks, RadarCoating coating)
	{
		RadarAtlasCellResult result = new RadarAtlasCellResult();
		double surfaceOffset = scene.BoundingSphereRadius * SurfaceOffsetFactor;
		Kahan[] kahan = new Kahan[9];
		double reciprocityPeak = 0.0;
		Kahan sumSigma = default(Kahan);
		Kahan sumSigmaSq = default(Kahan);
		double maxSigma = 0.0;
		double[] terms = new double[9];
		int total = spec.SamplesPerSide * spec.SamplesPerSide;
		for (int s = 0; s < total; s++)
		{
			spec.Probe(i, j, s, out var azDeg, out var elDeg, out var u1, out var u2);
			RadarDirectionBasis dir = RadarDirection.BasisDegrees(azDeg, elDeg);
			RadarPoPrepared prepared;
			if (frozenPrepared == null)
			{
				long start = Stopwatch.GetTimestamp();
				prepared = RadarFacetPO.Prepare(scene, dir, surfaceOffset, counters, token);
				prepareTicks += Stopwatch.GetTimestamp() - start;
			}
			else
			{
				prepared = frozenPrepared;
			}
			long integralStart = Stopwatch.GetTimestamp();
			Complex64 shh;
			Complex64 shv;
			Complex64 svh;
			Complex64 svv;
			if (coating != null && !coating.IsMetal)
			{
				RadarFacetPO.EvaluateMonostaticSCoated(scene, prepared, dir, wavenumber, coating, token, out shh, out shv, out svh, out svv);
			}
			else
			{
				RadarFacetPO.EvaluateMonostaticS(scene, prepared, dir, wavenumber, token, out shh, out shv, out svh, out svv);
			}
			integralTicks += Stopwatch.GetTimestamp() - integralStart;
			double maxS = Math.Max(Math.Max(shh.Magnitude, shv.Magnitude), Math.Max(svh.Magnitude, svv.Magnitude));
			if (maxS > 0.0)
			{
				Complex64 diff = shv - svh;
				reciprocityPeak = Math.Max(reciprocityPeak, diff.Magnitude / maxS);
			}
			CovarianceTerms(shh, shv, svh, svv, terms);
			for (int t = 0; t < 9; t++)
			{
				kahan[t].Add(terms[t]);
			}
			double sigma = RadarFarField.SigmaSquareMeters(shh);
			sumSigma.Add(sigma);
			sumSigmaSq.Add(sigma * sigma);
			if (sigma > maxSigma)
			{
				maxSigma = sigma;
			}
		}
		for (int t = 0; t < 9; t++)
		{
			result.C3[t] = kahan[t].Sum / total;
		}
		double meanSigma = sumSigma.Sum / total;
		double k2;
		if (meanSigma > 0.0)
		{
			k2 = sumSigmaSq.Sum / total / (meanSigma * meanSigma);
		}
		else
		{
			k2 = 0.0;
		}
		result.K2 = k2;
		result.MaxSigmaHH = maxSigma;
		result.ReciprocityRatio = reciprocityPeak;
		result.ShadowRays = counters.ShadowRays;
		result.EdgeOnFacets = counters.EdgeOnFacets;
		return result;
	}

	private static Complex64 Conjugate(Complex64 z)
	{
		return new Complex64(z.Real, -z.Imaginary);
	}

	public static void CovarianceTerms(Complex64 hh, Complex64 hv, Complex64 vh, Complex64 vv, double[] terms)
	{
		Complex64 cross = (hv + vh) * 0.5 * Math.Sqrt(2.0);
		terms[0] = hh.MagnitudeSquared;
		terms[1] = cross.MagnitudeSquared;
		terms[2] = vv.MagnitudeSquared;
		Complex64 hhCross = hh * Conjugate(cross);
		terms[3] = hhCross.Real;
		terms[4] = hhCross.Imaginary;
		Complex64 hhVv = hh * Conjugate(vv);
		terms[5] = hhVv.Real;
		terms[6] = hhVv.Imaginary;
		Complex64 crossVv = cross * Conjugate(vv);
		terms[7] = crossVv.Real;
		terms[8] = crossVv.Imaginary;
	}

	public static RadarAtlas Bake(RadarScene scene, double frequencyHz, RadarAtlasSpec spec, int threads, CancellationToken token, RadarAtlasCounter done, out RadarAtlasBakeStats stats, RadarCoating coating = null)
	{
		ValueTuple<double, RadarCoating>[] variants = new ValueTuple<double, RadarCoating>[1];
		variants[0].Item1 = frequencyHz;
		variants[0].Item2 = coating;
		RadarAtlas[] array = BakeMany(scene, spec, variants, threads, token, done, out var statsArray);
		stats = statsArray[0];
		return array[0];
	}

	public static RadarAtlas[] BakeMany(RadarScene scene, RadarAtlasSpec spec, IReadOnlyList<(double frequencyHz, RadarCoating coating)> variants, int threads, CancellationToken token, RadarAtlasCounter done, out RadarAtlasBakeStats[] stats)
	{
		if (scene == null)
		{
			throw new ArgumentNullException("scene");
		}
		if (variants == null)
		{
			throw new ArgumentNullException("variants");
		}
		int variantCount = variants.Count;
		RadarAtlasSpec[] array = new RadarAtlasSpec[variantCount];
		RadarCoating[] array2 = new RadarCoating[variantCount];
		double[] array3 = new double[variantCount];
		double[] wavenumbers = new double[variantCount];
		for (int i = 0; i < variantCount; i++)
		{
			RadarCoating coating = variants[i].coating;
			bool flag = coating == null || coating.IsMetal;
			RadarAtlasSpec spec2 = ((variantCount != 1) ? spec.For(coating) : spec);
			array[i] = spec2;
			array2[i] = coating;
			bool flag2 = spec2.SolverTag == RadarAtlasSpec.CoatedSolverTag;
			if (flag != flag2)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "RadarAtlasBaker: вариант {0}: solver tag {1} does not match coating {2}", i, spec2.SolverTag, ((coating != null) ? coating.Describe() : "metal")));
			}
			double num = variants[i].frequencyHz;
			array3[i] = scene.LongestSide / (299792458.0 / num);
			wavenumbers[i] = num * 6.283185307179586 / 299792458.0;
			if (array3[i] < RadarAtlasSpec.RefuseBelowWavelengths)
			{
				throw new ArgumentOutOfRangeException("variants", string.Format(CultureInfo.InvariantCulture, "L/λ = {0:F2} < 3: no atlas", array3[i]));
			}
		}
		int cellCount = spec.CellCount;
		int azCells = spec.AzimuthCells;
		float[][] array4 = new float[variantCount][];
		float[][] array5 = new float[variantCount][];
		float[][] array6 = new float[variantCount][];
		for (int j = 0; j < variantCount; j++)
		{
			array4[j] = new float[cellCount * 9];
			array5[j] = new float[cellCount];
			array6[j] = new float[cellCount];
		}
		RadarAtlasCellResult[][] array10 = new RadarAtlasCellResult[variantCount][];
		for (int k = 0; k < variantCount; k++)
		{
			array10[k] = new RadarAtlasCellResult[cellCount];
		}
		long timestamp = Stopwatch.GetTimestamp();
		int maxDegreeOfParallelism = Math.Max(1, threads);
		double[][] sigmas = new double[variantCount][];
		for (int k = 0; k < variantCount; k++)
		{
			sigmas[k] = new double[cellCount];
		}
		long[] prepareTotal = new long[variantCount];
		long[] integralTotal = new long[variantCount];
		RadarCounters[] allCounters = new RadarCounters[variantCount];
		for (int k = 0; k < variantCount; k++)
		{
			allCounters[k] = new RadarCounters();
		}
		Parallel.For(0, cellCount, new ParallelOptions
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism,
			CancellationToken = token
		}, delegate(int cell)
		{
			int i = cell % azCells;
			int j = cell / azCells;
			RadarCounters counters = new RadarCounters();
			long start = Stopwatch.GetTimestamp();
			RadarDirectionBasis incident = RadarDirection.BasisDegrees((i + 0.5) * spec.DeltaDeg - 180.0, (j + 0.5) * spec.DeltaDeg - 90.0);
			RadarPoPrepared frozen = RadarFacetPO.Prepare(scene, incident, scene.BoundingSphereRadius * SurfaceOffsetFactor, counters, token);
			long prepareElapsed = Stopwatch.GetTimestamp() - start;
			long integralElapsed = 0;
			for (int k = 0; k < variantCount; k++)
			{
				long num5 = prepareElapsed;
				long num6 = integralElapsed;
				RadarAtlasCellResult radarAtlasCellResult = IntegrateCell(scene, wavenumbers[k], array[k], i, j, frozen, counters, token, ref num5, ref num6, array2[k]);
				array10[k][cell] = radarAtlasCellResult;
				float[] array11 = array4[k];
				double[] c = radarAtlasCellResult.C3;
				int num7 = cell * 9;
				for (int l = 0; l < 9; l++)
				{
					array11[num7 + l] = (float)c[l];
				}
				array5[k][cell] = (float)radarAtlasCellResult.K2;
				array6[k][cell] = (float)radarAtlasCellResult.MaxSigmaHH;
				sigmas[k][cell] = radarAtlasCellResult.SigmaHH;
				Interlocked.Add(ref prepareTotal[k], num5);
				Interlocked.Add(ref integralTotal[k], num6);
				RadarCounters obj = allCounters[k];
				Interlocked.Add(ref obj.ShadowRays, counters.ShadowRays);
				Interlocked.Add(ref obj.EdgeOnFacets, counters.EdgeOnFacets);
			}
			if (done != null)
			{
				Interlocked.Increment(ref done.Value);
			}
		});
		long wallTicks = Stopwatch.GetTimestamp() - timestamp;
		RadarAtlas[] array12 = new RadarAtlas[variantCount];
		stats = new RadarAtlasBakeStats[variantCount];
		for (int m = 0; m < variantCount; m++)
		{
			double frequencyHz = variants[m].frequencyHz;
			RadarCounters counters2 = allCounters[m];
			RadarAtlasCellResult[] array11 = array10[m];
			double num10 = 0.0;
			foreach (RadarAtlasCellResult radarAtlasCellResult2 in array11)
			{
				num10 = Math.Max(num10, radarAtlasCellResult2.ReciprocityRatio);
			}
			Statistics(sigmas[m], out var minDb, out var medianDb, out var maxDb);
			RadarAtlasBakeStats radarAtlasBakeStats = new RadarAtlasBakeStats();
			radarAtlasBakeStats.WallSeconds = (double)wallTicks / Stopwatch.Frequency;
			radarAtlasBakeStats.PrepareSeconds = (double)prepareTotal[m] / Stopwatch.Frequency;
			radarAtlasBakeStats.IntegralSeconds = (double)integralTotal[m] / Stopwatch.Frequency;
			radarAtlasBakeStats.Threads = maxDegreeOfParallelism;
			radarAtlasBakeStats.Cells = cellCount;
			radarAtlasBakeStats.SharedVariants = variantCount;
			radarAtlasBakeStats.ShadowRays = counters2.ShadowRays;
			radarAtlasBakeStats.EdgeOnFacets = counters2.EdgeOnFacets;
			radarAtlasBakeStats.ReciprocityMaxRatio = num10;
			radarAtlasBakeStats.CellsPerSecond = ((radarAtlasBakeStats.WallSeconds > 0.0) ? ((double)cellCount / radarAtlasBakeStats.WallSeconds) : 0.0);
			stats[m] = radarAtlasBakeStats;
			RadarAtlasHeader radarAtlasHeader = new RadarAtlasHeader();
			radarAtlasHeader.format = RadarAtlasSpec.FormatVersion;
			radarAtlasHeader.key = RadarAtlasKey.Of(scene, frequencyHz, array[m], array2[m]);
			radarAtlasHeader.frequencyHz = frequencyHz;
			radarAtlasHeader.deltaDeg = array[m].DeltaDeg;
			radarAtlasHeader.samplesPerSide = array[m].SamplesPerSide;
			radarAtlasHeader.seed = array[m].Seed.ToString(CultureInfo.InvariantCulture);
			radarAtlasHeader.solverTag = array[m].SolverTag;
			radarAtlasHeader.faces = scene.TriangleCount;
			radarAtlasHeader.sizeInWavelengths = array3[m];
			radarAtlasHeader.boundaryRegion = array3[m] < RadarAtlasSpec.BoundaryBelowWavelengths;
			radarAtlasHeader.date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
			radarAtlasHeader.bakeSeconds = radarAtlasBakeStats.WallSeconds;
			radarAtlasHeader.prepareSeconds = radarAtlasBakeStats.PrepareSeconds;
			radarAtlasHeader.integralSeconds = radarAtlasBakeStats.IntegralSeconds;
			radarAtlasHeader.threads = radarAtlasBakeStats.Threads;
			radarAtlasHeader.cellsPerSecond = radarAtlasBakeStats.CellsPerSecond;
			radarAtlasHeader.minDb = minDb;
			radarAtlasHeader.medianDb = medianDb;
			radarAtlasHeader.maxDb = maxDb;
			radarAtlasHeader.reciprocityMaxRatio = num10;
			radarAtlasHeader.sharedVariants = variantCount;
			radarAtlasHeader.edgeOnFacets = counters2.EdgeOnFacets;
			radarAtlasHeader.shadowRays = counters2.ShadowRays;
			radarAtlasHeader.coating = RadarAtlasCoatingInfo.Of(array2[m]);
			array12[m] = new RadarAtlas(array[m], radarAtlasHeader, array4[m], array5[m], array6[m]);
		}
		return array12;
	}

	private static void Statistics(double[] sigma, out double minDb, out double medianDb, out double maxDb)
	{
		static double Db(double value)
		{
			if (value <= 0.0)
			{
				return -400.0;
			}
			return 10.0 * Math.Log10(value);
		}

		double[] sorted = (double[])sigma.Clone();
		Array.Sort(sorted);
		minDb = Db(sorted[0]);
		maxDb = Db(sorted[sorted.Length - 1]);
		int n = sorted.Length;
		double median = ((n % 2 == 1) ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) * 0.5);
		medianDb = Db(median);
	}

	public static double EstimateSeconds(RadarScene scene, double frequencyHz, RadarAtlasSpec spec, int threads, CancellationToken token, RadarCoating coating = null)
	{
		if (scene == null)
		{
			throw new ArgumentNullException("scene");
		}
		if (spec == null)
		{
			throw new ArgumentNullException("spec");
		}
		double wavenumber = frequencyHz * 6.283185307179586 / 299792458.0;
		int cellCount = spec.CellCount;
		int azCells = (int)(360.0 / spec.DeltaDeg);
		long elapsed = 0;
		Parallel.For(0, Math.Max(1, threads), new ParallelOptions
		{
			CancellationToken = token
		}, delegate(int t)
		{
			int num = (int)((double)(t + 0.5) * (double)cellCount / 20.0);
			int i = num % azCells;
			int j = num / azCells;
			long timestamp = Stopwatch.GetTimestamp();
			RadarCounters counters = new RadarCounters();
			RadarDirectionBasis basis = RadarDirection.BasisDegrees((i + 0.5) * spec.DeltaDeg - 180.0, (j + 0.5) * spec.DeltaDeg - 90.0);
			RadarPoPrepared prepared = RadarFacetPO.Prepare(scene, basis, scene.BoundingSphereRadius * SurfaceOffsetFactor, counters, token);
			long prepareElapsed = 0;
			long integralElapsed = 0;
			IntegrateCell(scene, wavenumber, spec, i, j, prepared, counters, token, ref prepareElapsed, ref integralElapsed, coating);
			Interlocked.Add(ref elapsed, Stopwatch.GetTimestamp() - timestamp);
		});
		return (double)elapsed / Stopwatch.Frequency / 20.0 * (double)cellCount / Math.Max(1, threads);
	}

	public static string FormatDuration(double seconds)
	{
		if (seconds > 90.0)
		{
			return (seconds / 60.0).ToString("F1", CultureInfo.InvariantCulture) + " min";
		}
		return seconds.ToString("F0", CultureInfo.InvariantCulture) + " s";
	}
}
