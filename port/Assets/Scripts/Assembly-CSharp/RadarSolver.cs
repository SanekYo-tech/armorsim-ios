using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Unity.Mathematics;

public static class RadarSolver
{
	public const string TooSmallRefusalKey = "radar.solve.too_small";

	public const string InvalidWavelengthRefusalKey = "radar.solve.invalid_wavelength";

	private static long coatedSolves;

	public static long CoatedSolves => Interlocked.Read(ref coatedSolves);

	public static RadarResult Solve(RadarScene scene, RadarQuery query, RadarQuality quality, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (scene == null)
		{
			throw new ArgumentNullException("scene");
		}
		if (quality < RadarQuality.Preview || quality > RadarQuality.Accurate)
		{
			throw new ArgumentOutOfRangeException("quality");
		}
		Stopwatch watch = Stopwatch.StartNew();
		double wavelength = 299792458.0 / query.FrequencyHz;
		double sizeInWavelengths = scene.BoundingSphereRadius / wavelength;
		RadarCounters counters = new RadarCounters();
		RadarResult result = new RadarResult();
		result.WavelengthMeters = wavelength;
		result.SizeInWavelengths = sizeInWavelengths;
		result.SurfaceOffsetMeters = scene.LongestSide * 1e-07;
		result.Counters = counters;
		result.BoundaryRegion = sizeInWavelengths >= 3.0 && 20.0 > sizeInWavelengths;
		result.Paths = new RadarPath[0];
		result.SurfaceOffsetOverWavelength = result.SurfaceOffsetMeters / wavelength;
		if (!(wavelength > 0.0) || double.IsNaN(wavelength) || double.IsInfinity(wavelength))
		{
			return Refuse(result, InvalidWavelengthRefusalKey, watch);
		}
		if (3.0 > sizeInWavelengths)
		{
			return Refuse(result, TooSmallRefusalKey, watch);
		}
		cancellationToken.ThrowIfCancellationRequested();
		int maxBounces;
		int rayBudget;
		double raysPerWavelength;
		if (quality == RadarQuality.Accurate)
		{
			maxBounces = 10;
			rayBudget = 32000000;
			raysPerWavelength = 10.0;
		}
		else if (quality == RadarQuality.Preview)
		{
			maxBounces = 3;
			rayBudget = 400000;
			raysPerWavelength = 5.0;
		}
		else if (quality == RadarQuality.Normal)
		{
			maxBounces = 5;
			rayBudget = 8000000;
			raysPerWavelength = 5.0;
		}
		else
		{
			maxBounces = 10;
			rayBudget = 32000000;
			raysPerWavelength = 5.0;
		}
		double sceneSizeInWavelengths = (scene.LongestSide + scene.LongestSide) / wavelength;
		double desiredGridSide = Math.Ceiling(raysPerWavelength * sceneSizeInWavelengths);
		int gridSide = (int)Math.Floor(Math.Sqrt(rayBudget));
		if (desiredGridSide > (double)gridSide)
		{
			counters.RayCapApplied = true;
			result.EffectiveRaysPerWavelength = (double)gridSide / sceneSizeInWavelengths;
		}
		else
		{
			gridSide = Math.Max(1, (int)desiredGridSide);
			result.EffectiveRaysPerWavelength = raysPerWavelength;
		}
		result.GridSide = gridSide;
		result.ResolutionBelowOne = 1.0 > result.EffectiveRaysPerWavelength;
		counters.ResolutionBelowOne = result.ResolutionBelowOne;
		double wavenumber = 6.283185307179586 / wavelength;
		double raySpacing = wavelength / result.EffectiveRaysPerWavelength;
		RadarDirectionBasis incident = query.IncidentBasis;
		RadarDirectionBasis receiver = query.ReceiverBasis;
		RadarPoPrepared prepared = RadarFacetPO.Prepare(scene, incident, result.SurfaceOffsetMeters, counters, cancellationToken);
		RadarCVector[,] po = RadarFacetPO.Evaluate(scene, prepared, incident, receiver, wavenumber, cancellationToken);
		RadarKahanVector[] kahan = new RadarKahanVector[2];
		for (int face = 0; face < scene.TriangleCount; face++)
		{
			if ((face & 0xFF) == 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			kahan[0].Add(po[face, 0]);
			kahan[1].Add(po[face, 1]);
		}
		RadarSbrOutput sbr = RadarSBR.Trace(scene, incident, receiver, wavelength, gridSide, raySpacing, maxBounces, result.SurfaceOffsetMeters, counters, cancellationToken);
		int faceCount = scene.TriangleCount;
		result.S = new Complex64[2, 2];
		result.SigmaSquareMeters = new double[2, 2];
		result.SigmaDbSquareMeters = new double[2, 2];
		result.SByOrder = new Complex64[4, 2, 2];
		result.FacetAmplitudeByPolarization = new double[2, 2, faceCount];
		result.MaxBounceByFacet = sbr.MaxBounceByFacet;
		result.FacetIlluminated = prepared.FacetIlluminated;
		for (int i = 0; i < faceCount; i++)
		{
			if (prepared.FacetIlluminated[i] && result.MaxBounceByFacet[i] < 1)
			{
				result.MaxBounceByFacet[i] = 1;
			}
		}
		for (int transmit = 0; transmit < 2; transmit++)
		{
			for (int receive = 0; receive < 2; receive++)
			{
				double3 receiverPolarization = ((receive == 0) ? receiver.Horizontal : receiver.Vertical);
				RadarKahanComplex sum = default(RadarKahanComplex);
				for (int order = 0; order < 4; order++)
				{
					RadarCVector field = ((order == 0) ? kahan[transmit].Value : sbr.ByOrder[order, transmit]);
					Complex64 projected = RadarFarField.Project(field, RadarCVector.Zero, receiver.ToRadar, receiverPolarization, wavenumber);
					result.SByOrder[order, receive, transmit] = projected;
					sum.Add(projected);
				}
				result.S[receive, transmit] = sum.Value;
				result.SigmaSquareMeters[receive, transmit] = RadarFarField.SigmaSquareMeters(sum.Value);
				result.SigmaDbSquareMeters[receive, transmit] = RadarFarField.SigmaDbSquareMeters(sum.Value);
				for (int facet = 0; facet < faceCount; facet++)
				{
					Complex64 facetProjected = RadarFarField.Project(po[facet, transmit], RadarCVector.Zero, receiver.ToRadar, receiverPolarization, wavenumber);
					result.FacetAmplitudeByPolarization[receive, transmit, facet] = (sbr.ByFacet[receive, transmit, facet] + facetProjected).Magnitude;
				}
			}
		}
		result.FacetAmplitude = result.GetFacetAmplitude(1, 1);
		result.Paths = new RadarPath[sbr.FirstOrderPaths.Length + sbr.Paths.Length];
		Array.Copy(sbr.FirstOrderPaths, result.Paths, sbr.FirstOrderPaths.Length);
		Array.Copy(sbr.Paths, 0, result.Paths, sbr.FirstOrderPaths.Length, sbr.Paths.Length);
		watch.Stop();
		result.ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds;
		return result;
	}

	private static RadarResult Refuse(RadarResult result, string key, Stopwatch watch)
	{
		watch.Stop();
		result.Refused = true;
		result.RefusalKey = key;
		result.ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds;
		return result;
	}

	public static RadarPatternResult SolvePattern(RadarScene scene, RadarQuery query, int receivePolarization, int transmitPolarization, CancellationToken token, Action<int, int> progress = null)
	{
		if (scene == null)
		{
			throw new ArgumentNullException("scene");
		}
		if (receivePolarization < 0 || receivePolarization > 1 || transmitPolarization < 0 || transmitPolarization > 1)
		{
			throw new ArgumentOutOfRangeException();
		}
		Stopwatch watch = Stopwatch.StartNew();
		double wavelength = 299792458.0 / query.FrequencyHz;
		if (!(wavelength > 0.0))
		{
			return new RadarPatternResult(Array.Empty<double3>(), Array.Empty<double>(), Array.Empty<double>(), 24, 13, refused: true, InvalidWavelengthRefusalKey, 0.0, watch.Elapsed.TotalMilliseconds);
		}
		double sizeInWavelengths = scene.BoundingSphereRadius / wavelength;
		if (double.IsNaN(wavelength) || double.IsInfinity(wavelength) || 3.0 > sizeInWavelengths)
		{
			return new RadarPatternResult(Array.Empty<double3>(), Array.Empty<double>(), Array.Empty<double>(), 24, 13, refused: true, TooSmallRefusalKey, 0.0, watch.Elapsed.TotalMilliseconds);
		}
		double3[] directions = new double3[312];
		double[] sigma = new double[312];
		double[] relativePower = new double[312];
		RadarDirectionBasis incident = query.IncidentBasis;
		RadarCounters counters = new RadarCounters();
		RadarPoPrepared prepared = RadarFacetPO.Prepare(scene, incident, scene.LongestSide * 1e-07, counters, token);
		double wavenumber = 6.283185307179586 / wavelength;
		double peakSigma = 0.0;
		int done = 0;
		int sample = 0;
		for (int elevIndex = 0; elevIndex < 13; elevIndex++)
		{
			for (int azIndex = 0; azIndex < 24; azIndex++)
			{
				token.ThrowIfCancellationRequested();
				RadarDirectionBasis basis = RadarDirection.BasisDegrees(azIndex * 15.0, elevIndex * 15.0 - 90.0);
				directions[sample] = basis.ToRadar;
				Complex64 s = RadarFacetPO.EvaluateSelected(scene, prepared, incident, basis, wavenumber, receivePolarization, transmitPolarization, scene.LongestSide * 1e-07, token);
				sigma[sample] = RadarFarField.SigmaSquareMeters(s);
				peakSigma = Math.Max(peakSigma, sigma[sample]);
				if (progress != null)
				{
					done++;
					progress(done, 312);
				}
				sample++;
			}
		}
		if (peakSigma > 0.0)
		{
			for (int i = 0; i < 312; i++)
			{
				relativePower[i] = sigma[i] / peakSigma;
			}
		}
		watch.Stop();
		return new RadarPatternResult(directions, relativePower, sigma, 24, 13, refused: false, null, peakSigma, watch.Elapsed.TotalMilliseconds);
	}

	public static RadarResult Solve(RadarScene scene, RadarQuery query, RadarCoating coating, RadarQuality quality, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (coating == null || coating.IsMetal)
		{
			return Solve(scene, query, quality, cancellationToken);
		}
		if (scene == null)
		{
			throw new ArgumentNullException("scene");
		}
		if (quality < RadarQuality.Preview || quality > RadarQuality.Accurate)
		{
			throw new ArgumentOutOfRangeException("quality");
		}
		Interlocked.Increment(ref coatedSolves);
		Stopwatch watch = Stopwatch.StartNew();
		double wavelength = 299792458.0 / query.FrequencyHz;
		double sizeInWavelengths = scene.BoundingSphereRadius / wavelength;
		RadarCounters counters = new RadarCounters();
		RadarResult result = new RadarResult();
		result.WavelengthMeters = wavelength;
		result.SizeInWavelengths = sizeInWavelengths;
		result.SurfaceOffsetMeters = scene.LongestSide * 1e-07;
		result.Counters = counters;
		result.BoundaryRegion = sizeInWavelengths >= 3.0 && 20.0 > sizeInWavelengths;
		result.Paths = new RadarPath[0];
		result.SurfaceOffsetOverWavelength = result.SurfaceOffsetMeters / wavelength;
		if (!(wavelength > 0.0) || double.IsNaN(wavelength) || double.IsInfinity(wavelength))
		{
			return Refuse(result, InvalidWavelengthRefusalKey, watch);
		}
		if (3.0 > sizeInWavelengths)
		{
			return Refuse(result, TooSmallRefusalKey, watch);
		}
		cancellationToken.ThrowIfCancellationRequested();
		int maxBounces;
		int rayBudget;
		double raysPerWavelength;
		if (quality == RadarQuality.Accurate)
		{
			maxBounces = 10;
			rayBudget = 32000000;
			raysPerWavelength = 10.0;
		}
		else if (quality == RadarQuality.Preview)
		{
			maxBounces = 3;
			rayBudget = 400000;
			raysPerWavelength = 5.0;
		}
		else if (quality == RadarQuality.Normal)
		{
			maxBounces = 5;
			rayBudget = 8000000;
			raysPerWavelength = 5.0;
		}
		else
		{
			maxBounces = 10;
			rayBudget = 32000000;
			raysPerWavelength = 5.0;
		}
		double sceneSizeInWavelengths = (scene.LongestSide + scene.LongestSide) / wavelength;
		double desiredGridSide = Math.Ceiling(raysPerWavelength * sceneSizeInWavelengths);
		int gridSide = (int)Math.Floor(Math.Sqrt(rayBudget));
		if (desiredGridSide > (double)gridSide)
		{
			counters.RayCapApplied = true;
			result.EffectiveRaysPerWavelength = (double)gridSide / sceneSizeInWavelengths;
		}
		else
		{
			gridSide = Math.Max(1, (int)desiredGridSide);
			result.EffectiveRaysPerWavelength = raysPerWavelength;
		}
		result.GridSide = gridSide;
		result.ResolutionBelowOne = 1.0 > result.EffectiveRaysPerWavelength;
		counters.ResolutionBelowOne = result.ResolutionBelowOne;
		double wavenumber = 6.283185307179586 / wavelength;
		double raySpacing = wavelength / result.EffectiveRaysPerWavelength;
		RadarDirectionBasis incident = query.IncidentBasis;
		RadarDirectionBasis receiver = query.ReceiverBasis;
		RadarPoPrepared prepared = RadarFacetPO.Prepare(scene, incident, result.SurfaceOffsetMeters, counters, cancellationToken);
		RadarCVector[,] electric;
		RadarCVector[,] magnetic;
		RadarFacetPO.EvaluateCoated(scene, prepared, incident, receiver, wavenumber, coating, cancellationToken, out electric, out magnetic);
		RadarKahanVector[] kahanElectric = new RadarKahanVector[2];
		RadarKahanVector[] kahanMagnetic = new RadarKahanVector[2];
		for (int face = 0; face < scene.TriangleCount; face++)
		{
			if ((face & 0xFF) == 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			kahanElectric[0].Add(electric[face, 0]);
			kahanElectric[1].Add(electric[face, 1]);
			kahanMagnetic[0].Add(magnetic[face, 0]);
			kahanMagnetic[1].Add(magnetic[face, 1]);
		}
		RadarSbrCoatedOutput sbr = RadarSBRCoated.TraceCoated(scene, incident, receiver, wavelength, gridSide, raySpacing, maxBounces, result.SurfaceOffsetMeters, coating, counters, cancellationToken);
		int faceCount = scene.TriangleCount;
		result.S = new Complex64[2, 2];
		result.SigmaSquareMeters = new double[2, 2];
		result.SigmaDbSquareMeters = new double[2, 2];
		result.SByOrder = new Complex64[4, 2, 2];
		result.FacetAmplitudeByPolarization = new double[2, 2, faceCount];
		result.MaxBounceByFacet = sbr.MaxBounceByFacet;
		result.FacetIlluminated = prepared.FacetIlluminated;
		for (int i = 0; i < faceCount; i++)
		{
			if (prepared.FacetIlluminated[i] && result.MaxBounceByFacet[i] < 1)
			{
				result.MaxBounceByFacet[i] = 1;
			}
		}
		for (int transmit = 0; transmit < 2; transmit++)
		{
			for (int receive = 0; receive < 2; receive++)
			{
				double3 receiverPolarization = ((receive == 0) ? receiver.Horizontal : receiver.Vertical);
				RadarKahanComplex sum = default(RadarKahanComplex);
				for (int order = 0; order < 4; order++)
				{
					RadarCVector electricField;
					RadarCVector magneticField;
					if (order == 0)
					{
						electricField = kahanElectric[transmit].Value;
						magneticField = kahanMagnetic[transmit].Value;
					}
					else
					{
						electricField = sbr.ElectricByOrder[order, transmit];
						magneticField = sbr.MagneticByOrder[order, transmit];
					}
					Complex64 projected = RadarFarField.Project(electricField, magneticField, receiver.ToRadar, receiverPolarization, wavenumber);
					result.SByOrder[order, receive, transmit] = projected;
					sum.Add(projected);
				}
				result.S[receive, transmit] = sum.Value;
				result.SigmaSquareMeters[receive, transmit] = RadarFarField.SigmaSquareMeters(sum.Value);
				result.SigmaDbSquareMeters[receive, transmit] = RadarFarField.SigmaDbSquareMeters(sum.Value);
				for (int facet = 0; facet < faceCount; facet++)
				{
					Complex64 facetProjected = RadarFarField.Project(electric[facet, transmit], magnetic[facet, transmit], receiver.ToRadar, receiverPolarization, wavenumber);
					result.FacetAmplitudeByPolarization[receive, transmit, facet] = (sbr.ByFacet[receive, transmit, facet] + facetProjected).Magnitude;
				}
			}
		}
		result.FacetAmplitude = result.GetFacetAmplitude(1, 1);
		result.Paths = new RadarPath[sbr.FirstOrderPaths.Length + sbr.Paths.Length];
		Array.Copy(sbr.FirstOrderPaths, result.Paths, sbr.FirstOrderPaths.Length);
		Array.Copy(sbr.Paths, 0, result.Paths, sbr.FirstOrderPaths.Length, sbr.Paths.Length);
		watch.Stop();
		result.ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds;
		return result;
	}

	public static RadarPatternResult SolvePattern(RadarScene scene, RadarQuery query, int receivePolarization, int transmitPolarization, RadarCoating coating, CancellationToken token, Action<int, int> progress = null)
	{
		if (coating == null || coating.IsMetal)
		{
			return SolvePattern(scene, query, receivePolarization, transmitPolarization, token, progress);
		}
		if (scene == null)
		{
			throw new ArgumentNullException("scene");
		}
		if (receivePolarization < 0 || receivePolarization > 1 || transmitPolarization < 0 || transmitPolarization > 1)
		{
			throw new ArgumentOutOfRangeException();
		}
		Interlocked.Increment(ref coatedSolves);
		Stopwatch watch = Stopwatch.StartNew();
		double wavelength = 299792458.0 / query.FrequencyHz;
		if (!(wavelength > 0.0))
		{
			return new RadarPatternResult(Array.Empty<double3>(), Array.Empty<double>(), Array.Empty<double>(), 24, 13, refused: true, InvalidWavelengthRefusalKey, 0.0, watch.Elapsed.TotalMilliseconds);
		}
		double sizeInWavelengths = scene.BoundingSphereRadius / wavelength;
		if (double.IsNaN(wavelength) || double.IsInfinity(wavelength) || 3.0 > sizeInWavelengths)
		{
			return new RadarPatternResult(Array.Empty<double3>(), Array.Empty<double>(), Array.Empty<double>(), 24, 13, refused: true, TooSmallRefusalKey, 0.0, watch.Elapsed.TotalMilliseconds);
		}
		double3[] directions = new double3[312];
		double[] sigma = new double[312];
		double[] relativePower = new double[312];
		RadarDirectionBasis incident = query.IncidentBasis;
		RadarCounters counters = new RadarCounters();
		RadarPoPrepared prepared = RadarFacetPO.Prepare(scene, incident, scene.LongestSide * 1e-07, counters, token);
		double wavenumber = 6.283185307179586 / wavelength;
		double peakSigma = 0.0;
		int done = 0;
		int sample = 0;
		for (int elevIndex = 0; elevIndex < 13; elevIndex++)
		{
			for (int azIndex = 0; azIndex < 24; azIndex++)
			{
				token.ThrowIfCancellationRequested();
				RadarDirectionBasis basis = RadarDirection.BasisDegrees(azIndex * 15.0, elevIndex * 15.0 - 90.0);
				directions[sample] = basis.ToRadar;
				Complex64 s = RadarFacetPO.EvaluateSelectedCoated(scene, prepared, incident, basis, wavenumber, receivePolarization, transmitPolarization, scene.LongestSide * 1e-07, coating, token);
				sigma[sample] = RadarFarField.SigmaSquareMeters(s);
				peakSigma = Math.Max(peakSigma, sigma[sample]);
				if (progress != null)
				{
					done++;
					progress(done, 312);
				}
				sample++;
			}
		}
		if (peakSigma > 0.0)
		{
			for (int i = 0; i < 312; i++)
			{
				relativePower[i] = sigma[i] / peakSigma;
			}
		}
		watch.Stop();
		return new RadarPatternResult(directions, relativePower, sigma, 24, 13, refused: false, null, peakSigma, watch.Elapsed.TotalMilliseconds);
	}
}
