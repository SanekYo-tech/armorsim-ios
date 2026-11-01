using System;

public sealed class RadarAtlas
{
	public const int Floats = 9;

	public readonly RadarAtlasSpec Spec;

	public readonly RadarAtlasHeader Header;

	private readonly float[] c3;

	private readonly float[] k2;

	private readonly float[] maxHH;

	public int CellCount => Spec.CellCount;

	internal float[] CovarianceData => c3;

	internal float[] FluctuationData => k2;

	internal float[] MaximumData => maxHH;

	public double FrequencyHz => (Header != null) ? Header.frequencyHz : 0.0;

	public RadarAtlas(RadarAtlasSpec spec, RadarAtlasHeader header, float[] covariance, float[] fluctuation, float[] maximum)
	{
		if (spec == null)
		{
			throw new ArgumentNullException("spec");
		}
		Spec = spec;
		Header = header;
		int cellCount = spec.CellCount;
		if (covariance == null || covariance.Length != cellCount * Floats || fluctuation == null || fluctuation.Length != cellCount || maximum == null || maximum.Length != cellCount)
		{
			throw new ArgumentException("Atlas payload does not match its grid.");
		}
		c3 = covariance;
		k2 = fluctuation;
		maxHH = maximum;
	}

	public float Element(int cell, int element)
	{
		return c3[cell * Floats + element];
	}

	public static void Weights(RadarAtlasPair pair, double xi, out double w0, out double w1, out double w2)
	{
		double c = Math.Cos(xi);
		double s = Math.Sin(xi);
		switch (pair)
		{
		case RadarAtlasPair.HH:
			w0 = c * c;
			w1 = Math.Sqrt(2.0) * c * s;
			w2 = s * s;
			return;
		case RadarAtlasPair.VV:
			w0 = s * s;
			w1 = -Math.Sqrt(2.0) * c * s;
			w2 = c * c;
			return;
		case RadarAtlasPair.HV:
			w0 = -c * s;
			w1 = (c * c - s * s) / Math.Sqrt(2.0);
			w2 = c * s;
			return;
		}
		throw new ArgumentOutOfRangeException("pair");
	}

	public static double Sigma(double[] e, RadarAtlasPair pair, double xi)
	{
		Weights(pair, xi, out double w0, out double w1, out double w2);
		return 4.0 * Math.PI * (w0 * w0 * e[0] + w1 * w1 * e[1] + w2 * w2 * e[2] + 2.0 * (w0 * w1 * e[3] + w0 * w2 * e[5] + w1 * w2 * e[7]));
	}

	public double SigmaMean(double azimuthRad, double elevationRad, double xiRad, RadarAtlasPair pair)
	{
		double[] e = new double[Floats];
		Interpolate(azimuthRad * (180.0 / Math.PI), elevationRad * (180.0 / Math.PI), e);
		Weights(pair, xiRad, out double w0, out double w1, out double w2);
		return 4.0 * Math.PI * (w0 * w0 * e[0] + w1 * w1 * e[1] + w2 * w2 * e[2] + 2.0 * (w0 * w1 * e[3] + w0 * w2 * e[5] + w1 * w2 * e[7]));
	}

	public void Interpolate(double azDeg, double elDeg, double[] result)
	{
		int nAz = (int)Math.Round(360.0 / Spec.DeltaDeg);
		int nEl = (int)Math.Round(180.0 / Spec.DeltaDeg);
		double u = (azDeg + 180.0) / Spec.DeltaDeg - 0.5;
		double fu = Math.Floor(u);
		int i0 = (int)fu % nAz;
		if (i0 < 0)
		{
			i0 += nAz;
		}
		int i1 = (i0 + 1) % nAz;
		u -= fu;
		double v = (elDeg + 90.0) / Spec.DeltaDeg - 0.5;
		if (v < 0.0)
		{
			v = 0.0;
		}
		else if (v > nEl - 1)
		{
			v = nEl - 1;
		}
		int j0 = Math.Min((int)Math.Floor(v), nEl - 2);
		v -= j0;
		int c00 = Spec.CellIndex(i0, j0) * Floats;
		int c10 = Spec.CellIndex(i1, j0) * Floats;
		int c01 = Spec.CellIndex(i0, j0 + 1) * Floats;
		int c11 = Spec.CellIndex(i1, j0 + 1) * Floats;
		for (int k = 0; k < Floats; k++)
		{
			result[k] = c3[c00 + k] * (1.0 - u) * (1.0 - v) + c3[c10 + k] * u * (1.0 - v) + c3[c01 + k] * (1.0 - u) * v + c3[c11 + k] * u * v;
		}
	}

	public RadarAtlasCell CellAt(double azDeg, double elDeg)
	{
		int nAz = (int)Math.Round(360.0 / Spec.DeltaDeg);
		int i = (int)Math.Floor((azDeg + 180.0) / Spec.DeltaDeg) % nAz;
		if (i < 0)
		{
			i += nAz;
		}
		int nEl = (int)Math.Round(180.0 / Spec.DeltaDeg);
		int j = Math.Min(nEl - 1, Math.Max(0, (int)Math.Floor((elDeg + 90.0) / Spec.DeltaDeg)));
		int cell = Spec.CellIndex(i, j);
		return new RadarAtlasCell(i, j, (i + 0.5) * Spec.DeltaDeg - 180.0, (j + 0.5) * Spec.DeltaDeg - 90.0, k2[cell], maxHH[cell], c3[cell * Floats] * (4.0 * Math.PI));
	}

	public double CellSigmaHH(int cell)
	{
		return c3[cell * Floats] * (4.0 * Math.PI);
	}
}
