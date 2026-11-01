using System;

public static class RadarMapDetector
{
	public const double MonopulseSlope = 1.6;

	public static readonly double[] PfaSteps = new double[6] { 0.001, 0.0001, 1E-05, 1E-06, 1E-07, 1E-08 };

	public const double DefaultPfa = 1E-06;

	public static readonly int[] IntegrationSteps = new int[3] { 1, 2, 4 };

	public const int FalseAlarmLimitPerScan = 4000;

	private static readonly double FourPiCubed = Math.Pow(4.0 * Math.PI, 3.0);

	public static double Linear(double db)
	{
		return Math.Pow(10.0, db / 10.0);
	}

	public static double Db(double value)
	{
		if (value <= 0.0)
		{
			return double.NegativeInfinity;
		}
		return 10.0 * Math.Log10(value);
	}

	public static double Wavelength(RadarMapStationSpec s)
	{
		return 299792458.0 / s.FrequencyHz;
	}

	public static double SinglePulseSnr(RadarMapStationSpec s, double rangeM, double sigmaM2, double f4 = 1.0)
	{
		double num = Linear(s.GainDbi);
		double num2 = Wavelength(s);
		return s.PulseSeconds * s.PeakPowerW * num * num * num2 * num2 * sigmaM2 * f4 / (Math.Pow(rangeM, 4.0) * FourPiCubed * 1.380649E-23 * 290.0 * Linear(s.NoiseFigureDb) * Linear(s.LossDb));
	}

	public static double Dwell(RadarMapStationSpec s, int integration)
	{
		if (s.Scan == RadarMapScan.Rotating)
		{
			return s.BeamAzimuthDeg / (s.Rpm * 6.0 / integration);
		}
		return integration * s.DwellSeconds;
	}

	public static int Pulses(RadarMapStationSpec s, int integration)
	{
		return (int)Math.Floor(Dwell(s, integration) * s.PrfHz + 1E-09);
	}

	public static double Revisit(RadarMapStationSpec s, int integration, double sectorWidthDeg = 0.0 / 0.0)
	{
		if (s.Scan == RadarMapScan.Rotating)
		{
			return 60.0 / (s.Rpm / integration);
		}
		if (double.IsNaN(sectorWidthDeg))
		{
			sectorWidthDeg = s.SectorDeg;
		}
		return (double)SectorBeams(s, sectorWidthDeg) * s.DwellSeconds * integration;
	}

	public static int SectorBeams(RadarMapStationSpec s, double widthDeg)
	{
		return (int)Math.Ceiling(widthDeg / s.BeamAzimuthDeg - 1E-09);
	}

	public static double RequiredSnr(double pd, double pfa)
	{
		return Math.Log(pfa) / Math.Log(pd) - 1.0;
	}

	public static double ProbabilityOfDetection(double meanSnr, double pfa)
	{
		return Math.Pow(pfa, 1.0 / (meanSnr + 1.0));
	}

	public static double Threshold(double pfa)
	{
		return -Math.Log(pfa);
	}

	public static double R0(RadarMapStationSpec s, double sigmaM2, int integration = 1, double pd = 0.5, double pfa = 1E-06)
	{
		int num = Pulses(s, integration);
		double num2 = SinglePulseSnr(s, 1.0, sigmaM2);
		return Math.Pow((double)num * num2 / RequiredSnr(pd, pfa), 0.25);
	}

	public static double SigmaHat(RadarMapStationSpec s, int integration, double snrHat, double rangeHatM)
	{
		return snrHat / (SinglePulseSnr(s, rangeHatM, 1.0) * Pulses(s, integration));
	}

	public static double FalseAlarmsPerBeam(RadarMapStationSpec s, double pfa)
	{
		return Math.Floor(s.InstrumentedRangeM / s.RangeCellM + 1E-09) * pfa;
	}

	public static int BeamPositions(RadarMapStationSpec s, double sectorWidthDeg)
	{
		if (s.Scan == RadarMapScan.Rotating)
		{
			return (int)Math.Ceiling(360.0 / s.BeamAzimuthDeg - 1E-09);
		}
		return SectorBeams(s, sectorWidthDeg);
	}

	public static double OpenUnit(ref ulong state)
	{
		return 1.0 - RadarMapRng.NextUnit(ref state);
	}

	public static void Normals(ref ulong state, out double a, out double b)
	{
		double num = OpenUnit(ref state);
		double num2 = RadarMapRng.NextUnit(ref state);
		double num3 = Math.Sqrt(-2.0 * Math.Log(num));
		a = num3 * Math.Cos(num2 * (2.0 * Math.PI));
		b = num3 * Math.Sin(num2 * (2.0 * Math.PI));
	}

	public static int Poisson(double lambda, ref ulong state)
	{
		if (lambda <= 0.0)
		{
			return 0;
		}
		if (lambda > 500.0)
		{
			Normals(ref state, out double a, out double _);
			return Math.Max(0, (int)Math.Round(lambda + Math.Sqrt(lambda) * a));
		}
		double num = RadarMapRng.NextUnit(ref state);
		double num2 = Math.Exp(-lambda);
		double num3 = num2;
		int num4 = 0;
		while (num > num3 && num4 < 100000)
		{
			num4++;
			num2 *= lambda / num4;
			num3 += num2;
		}
		return num4;
	}

	public static bool Detect(double sigmaBar, double snrPerSquareMetre, double threshold, ref ulong state, out double sigmaDraw, out double snr, out double power)
	{
		double num = RadarMapRng.NextUnit(ref state);
		double num2 = RadarMapRng.NextUnit(ref state);
		sigmaDraw = -sigmaBar * Math.Log(1.0 - num);
		snr = sigmaDraw * snrPerSquareMetre;
		Normals(ref state, out double a, out double b);
		double num3 = Math.Sqrt(0.5);
		double num4 = Math.Sqrt(snr);
		double num5 = Math.Cos(num2 * (2.0 * Math.PI)) * num4 + num3 * a;
		double num6 = Math.Sin(num2 * (2.0 * Math.PI)) * num4 + num3 * b;
		power = num5 * num5 + num6 * num6;
		return power > threshold;
	}

	public static void Accuracy(RadarMapStationSpec s, double snr, out double rangeSigmaM, out double bearingSigmaDeg, out double elevationSigmaDeg)
	{
		double num = Math.Sqrt(2.0 * Math.Max(snr, 1.0));
		rangeSigmaM = s.RangeCellM / num;
		bearingSigmaDeg = s.BeamAzimuthDeg / (num * 1.6);
		elevationSigmaDeg = (s.Is3D ? (s.BeamElevationDeg / (num * 1.6)) : double.NaN);
	}
}
