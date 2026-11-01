using System;
using System.Globalization;

public static class Flat2DGlowPalette
{
	public enum GamutMode
	{
		Clip = 0,
		DesaturateToWhite = 1
	}

	public enum Mutant
	{
		None = 0,
		Cmf1964_10deg = 1,
		WymanSloanShirleyFit = 2
	}

	public struct LutEntry
	{
		public double TemperatureK;

		public double X;

		public double Y;

		public double R;

		public double G;

		public double B;

		public double W;
	}

	public const double DraperPointK = 798.0;

	public const GamutMode DefaultGamut = GamutMode.DesaturateToWhite;

	public const double DefaultExposure = 1.0;

	public const double DefaultHaloK = 2.0;

	public const double HaloProfileFlatCore = 0.7;

	public const bool DefaultOverlapNormalise = true;

	public const double AssumedEmissivity = 1.0;

	public const double DefaultLutStepK = 10.0;

	public const double DefaultTauFadeShare = 0.5;

	public const double DefaultLutMinK = 600.0;

	public const double FadeAnchorHighK = 1500.0;

	public const double DefaultRoomTempK = 293.0;

	public const double PlanckH = 6.62607015E-34;

	public const double LightC = 299792458.0;

	public const double BoltzK = 1.380649E-23;

	public const int GridCount = 95;

	public const double GridStartNm = 360.0;

	public const double GridStepNm = 5.0;

	private static readonly double[,] Cmf1931_2deg;

	private static readonly double[,] Cmf1964_10deg_MUTANT_ONLY;

	public static double WavelengthNm(int i)
	{
		return i * GridStepNm + GridStartNm;
	}

	private static double AsymGaussian(double x, double mu, double sigma1, double sigma2)
	{
		double sigma = x < mu ? sigma1 : sigma2;
		double t = (x - mu) / sigma;
		return Math.Exp(-0.5 * t * t);
	}

	private static void CmfWymanSloanShirley(double lambdaNm, out double xbar, out double ybar, out double zbar)
	{
		xbar = 1.056 * AsymGaussian(lambdaNm, 599.8, 37.9, 31.0) + 0.362 * AsymGaussian(lambdaNm, 442.0, 16.0, 26.7) - 0.065 * AsymGaussian(lambdaNm, 501.1, 20.4, 26.2);
		ybar = 0.821 * AsymGaussian(lambdaNm, 568.8, 46.9, 40.5) + 0.286 * AsymGaussian(lambdaNm, 530.9, 16.3, 31.1);
		zbar = 1.217 * AsymGaussian(lambdaNm, 437.0, 11.8, 36.0) + 0.681 * AsymGaussian(lambdaNm, 459.0, 26.0, 13.8);
	}

	private static void CmfAt(int gridIndex, Mutant mutant, out double xbar, out double ybar, out double zbar)
	{
		switch (mutant)
		{
		case Mutant.None:
			xbar = Cmf1931_2deg[gridIndex, 0];
			ybar = Cmf1931_2deg[gridIndex, 1];
			zbar = Cmf1931_2deg[gridIndex, 2];
			return;
		case Mutant.Cmf1964_10deg:
			xbar = Cmf1964_10deg_MUTANT_ONLY[gridIndex, 0];
			ybar = Cmf1964_10deg_MUTANT_ONLY[gridIndex, 1];
			zbar = Cmf1964_10deg_MUTANT_ONLY[gridIndex, 2];
			return;
		case Mutant.WymanSloanShirleyFit:
			CmfWymanSloanShirley(WavelengthNm(gridIndex), out xbar, out ybar, out zbar);
			return;
		default:
			throw new ArgumentOutOfRangeException("mutant", "неизвестный мутант палитры");
		}
	}

	public static double PlanckRadiance(double lambdaM, double temperatureK)
	{
		if (temperatureK <= 0.0)
		{
			return 0.0;
		}
		double lambda5 = Math.Pow(lambdaM, 5.0);
		double exponential = Math.Exp(PlanckH * LightC / (lambdaM * BoltzK * temperatureK)) - 1.0;
		return 2.0 * PlanckH * LightC * LightC / (lambda5 * exponential);
	}

	public static void ComputeXYZ(double temperatureK, Mutant mutant, out double X, out double Y, out double Z)
	{
		X = 0.0;
		Y = 0.0;
		Z = 0.0;
		double previousRadiance = 0.0;
		double previousX = 0.0;
		double previousY = 0.0;
		double previousZ = 0.0;
		for (int i = 0; i < GridCount; i++)
		{
			double radiance = PlanckRadiance(WavelengthNm(i) * 1E-09, temperatureK);
			CmfAt(i, mutant, out double xbar, out double ybar, out double zbar);
			if (i > 0)
			{
				X += (radiance * xbar + previousRadiance * previousX) * 0.5 * 5E-09;
				Y += (radiance * ybar + previousRadiance * previousY) * 0.5 * 5E-09;
				Z += (radiance * zbar + previousRadiance * previousZ) * 0.5 * 5E-09;
			}
			previousRadiance = radiance;
			previousX = xbar;
			previousY = ybar;
			previousZ = zbar;
		}
	}

	public static void Chromaticity(double temperatureK, Mutant mutant, out double x, out double y)
	{
		ComputeXYZ(temperatureK, mutant, out double X, out double Y, out double Z);
		double sum = X + Y + Z;
		if (sum > 0.0)
		{
			x = X / sum;
			y = Y / sum;
		}
		else
		{
			x = 0.0;
			y = 0.0;
		}
	}

	public static void Chromaticity(double temperatureK, out double x, out double y)
	{
		Chromaticity(temperatureK, Mutant.None, out x, out y);
	}

	public static double Luminance(double temperatureK, Mutant mutant)
	{
		ComputeXYZ(temperatureK, mutant, out double _, out double Y, out double _);
		return Y;
	}

	public static double GlowWeight(double temperatureK, double tMeltK, double draperK, Mutant mutant)
	{
		return GlowWeightFrom(Luminance(temperatureK, mutant), Luminance(draperK, mutant), Luminance(tMeltK, mutant));
	}

	public static double GlowWeight(double temperatureK, double tMeltK)
	{
		return GlowWeightFrom(Luminance(temperatureK, Mutant.None), Luminance(DraperPointK, Mutant.None), Luminance(tMeltK, Mutant.None));
	}

	private static double GlowWeightFrom(double lT, double lDraper, double lMelt)
	{
		if (lT <= 0.0 || lDraper <= 0.0 || lMelt <= 0.0)
		{
			return 0.0;
		}
		double denominator = Math.Log(lMelt) - Math.Log(lDraper);
		if (denominator <= 0.0)
		{
			return 0.0;
		}
		return Clamp01((Math.Log(lT) - Math.Log(lDraper)) / denominator);
	}

	private static double Clamp01(double v)
	{
		if (v < 0.0)
		{
			return 0.0;
		}
		if (v > 1.0)
		{
			return 1.0;
		}
		return v;
	}

	public static double HaloProfile(double rho)
	{
		if (rho <= HaloProfileFlatCore)
		{
			return 1.0;
		}
		if (rho >= 1.0)
		{
			return 0.0;
		}
		double t = (rho - HaloProfileFlatCore) / (1.0 - HaloProfileFlatCore);
		return 1.0 - (3.0 * t * t - 2.0 * t * t * t);
	}

	public static double HaloOverlapSum(double haloRadiusOverDx)
	{
		const double step = 0.00025;
		double sum = 0.0;
		for (int i = 0; i < 4000; i++)
		{
			double r0 = i * step;
			double r1 = (i + 1) * step;
			sum += (HaloProfile(r1) * r1 + HaloProfile(r0) * r0) * 0.5 * step;
		}
		return 2.0 * Math.PI * haloRadiusOverDx * haloRadiusOverDx * sum;
	}

	public static double HaloIntensityScale(double haloRadiusOverDx)
	{
		double sum = HaloOverlapSum(haloRadiusOverDx);
		return sum > 0.0 ? 1.0 / sum : 1.0;
	}

	public static void XyzToLinearSrgb(double X, double Y, double Z, out double r, out double g, out double b)
	{
		r = 3.2406255 * X - 1.537208 * Y - 0.4986286 * Z;
		g = -0.9689307 * X + 1.8757561 * Y + 0.0415175 * Z;
		b = 0.0557101 * X - 0.2040211 * Y + 1.0569959 * Z;
	}

	private static void DesaturateToWhiteRgb(ref double r, ref double g, ref double b)
	{
		double min = Math.Min(r, Math.Min(g, b));
		if (min < 0.0)
		{
			double scale = Clamp01(1.0 / (1.0 - min));
			r = scale * r + (1.0 - scale);
			g = scale * g + (1.0 - scale);
			b = scale * b + (1.0 - scale);
		}
	}

	public static void ToLinearSrgb(double temperatureK, GamutMode mode, Mutant mutant, out double r, out double g, out double b)
	{
		ComputeXYZ(temperatureK, mutant, out double X, out double Y, out double Z);
		if (Y <= 0.0)
		{
			r = 0.0;
			g = 0.0;
			b = 0.0;
			return;
		}
		X /= Y;
		Z /= Y;
		XyzToLinearSrgb(X, 1.0, Z, out r, out g, out b);
		if (mode == GamutMode.DesaturateToWhite)
		{
			DesaturateToWhiteRgb(ref r, ref g, ref b);
		}
		else
		{
			if (r < 0.0) r = 0.0;
			if (g < 0.0) g = 0.0;
			if (b < 0.0) b = 0.0;
		}
		double max = Math.Max(r, Math.Max(g, b));
		if (max > 0.0)
		{
			r /= max;
			g /= max;
			b /= max;
		}
		if (r < 0.0) r = 0.0;
		if (g < 0.0) g = 0.0;
		if (b < 0.0) b = 0.0;
	}

	public static double CoolingConstant(double tauFade, double highK, double draperK)
	{
		if (tauFade <= 0.0)
		{
			throw new ArgumentOutOfRangeException("tauFade", "τ_fade должно быть > 0");
		}
		return (Math.Pow(draperK, -3.0) - Math.Pow(highK, -3.0)) / (3.0 * tauFade);
	}

	public static double CoolingConstant(double tauFade)
	{
		return CoolingConstant(tauFade, FadeAnchorHighK, DraperPointK);
	}

	public static double TauFadeFromShot(double shotDurationSimSeconds, double share)
	{
		return shotDurationSimSeconds * share;
	}

	public static double TauFadeFromShot(double shotDurationSimSeconds)
	{
		return shotDurationSimSeconds * DefaultTauFadeShare;
	}

	public static double Cool(double temperatureK, double t, double c, double roomK)
	{
		if (temperatureK <= 0.0)
		{
			return roomK;
		}
		double inverseCube = Math.Pow(temperatureK, -3.0) + 3.0 * c * t;
		if (inverseCube <= 0.0)
		{
			return roomK;
		}
		return Math.Max(Math.Pow(inverseCube, -1.0 / 3.0), roomK);
	}

	public static double Cool(double temperatureK, double t, double c)
	{
		return Cool(temperatureK, t, c, DefaultRoomTempK);
	}

	public static double AdvanceDisplayTemperature(double tDispPrev, double tSolverNow, double tSolverPrev, double dtSim, double c, double roomK)
	{
		double target = Math.Max(roomK, tDispPrev + Math.Max(0.0, tSolverNow - tSolverPrev));
		return Cool(target, dtSim, c, roomK);
	}

	public static int LutCount(double tMinK, double tMaxK, double stepK)
	{
		if (stepK <= 0.0)
		{
			throw new ArgumentOutOfRangeException("stepK", "шаг таблицы должен быть > 0");
		}
		if (tMinK >= tMaxK)
		{
			throw new ArgumentException("верхняя граница таблицы должна быть выше нижней");
		}
		return (int)Math.Ceiling((tMaxK - tMinK) / stepK) + 1;
	}

	public static LutEntry[] BuildLutEntries(double tMinK, double tMaxK, double stepK, GamutMode mode, double draperK, Mutant mutant)
	{
		int count = LutCount(tMinK, tMaxK, stepK);
		LutEntry[] entries = new LutEntry[count];
		double lDraper = Luminance(draperK, mutant);
		double lMelt = Luminance(tMaxK, mutant);
		for (int i = 0; i < count; i++)
		{
			double temperature = tMinK + i * stepK;
			Chromaticity(temperature, mutant, out double x, out double y);
			ToLinearSrgb(temperature, mode, mutant, out double r, out double g, out double b);
			double w = GlowWeightFrom(Luminance(temperature, mutant), lDraper, lMelt);
			entries[i] = new LutEntry
			{
				TemperatureK = temperature,
				X = x,
				Y = y,
				R = r,
				G = g,
				B = b,
				W = w
			};
		}
		return entries;
	}

	public static float[] BuildLut(double tMinK, double tMaxK, double stepK, GamutMode mode)
	{
		LutEntry[] entries = BuildLutEntries(tMinK, tMaxK, stepK, mode, DraperPointK, Mutant.None);
		float[] result = new float[entries.Length * 4];
		for (int i = 0; i < entries.Length; i++)
		{
			result[i * 4] = (float)entries[i].R;
			result[i * 4 + 1] = (float)entries[i].G;
			result[i * 4 + 2] = (float)entries[i].B;
			result[i * 4 + 3] = (float)entries[i].W;
		}
		return result;
	}

	public static float[] BuildLut(double tMeltK)
	{
		return BuildLut(DefaultLutMinK, tMeltK, DefaultLutStepK, DefaultGamut);
	}

	public static string[] TuningReport()
	{
		CultureInfo culture = CultureInfo.InvariantCulture;
		Func<double, string> f = delegate(double v) { return v.ToString("0.####", culture); };
		return new string[12]
		{
			"DraperPointK       = " + f(DraperPointK) + "  K   [VISUAL_TUNING]",
			"DefaultGamut       = " + DefaultGamut + "        [VISUAL_TUNING]",
			"DefaultExposure    = " + f(DefaultExposure) + "        [VISUAL_TUNING]",
			"DefaultHaloK       = " + f(DefaultHaloK) + "        [VISUAL_TUNING]  (радиус ореола = k · 0.5·Δx = " + f(1.0) + " Δx)",
			"HaloProfileFlatCore= " + f(HaloProfileFlatCore) + "        [VISUAL_TUNING]  (ядро до этой доли R, кромка smoothstep до 1)",
			"DefaultOverlapNormalise = True     [VISUAL_TUNING]  (Σ(R) = " + f(HaloOverlapSum(1.0)) + " ореолов/пиксель, множитель силы 1/Σ = " + f(HaloIntensityScale(1.0)) + ")",
			"AssumedEmissivity  = " + f(AssumedEmissivity) + "        [ASSUMED, не VISUAL_TUNING — ни xy, ни w не меняет]",
			"DefaultLutStepK    = " + f(DefaultLutStepK) + "  K   [VISUAL_TUNING]",
			"DefaultTauFadeShare= " + f(DefaultTauFadeShare) + "        [VISUAL_TUNING]",
			"DefaultLutMinK     = " + f(DefaultLutMinK) + "  K   [VISUAL_TUNING]",
			"FadeAnchorHighK    = " + f(FadeAnchorHighK) + "  K   [VISUAL_TUNING]",
			"DefaultRoomTempK   = " + f(DefaultRoomTempK) + "  K   [реестр, НЕ VISUAL_TUNING]"
		};
	}

	static Flat2DGlowPalette()
	{
		Cmf1931_2deg = new double[95, 3]
		{
		{ 0.00012990000000000001, 3.9169999999999999e-06, 0.00060610000000000004 },
		{ 0.0002321, 6.9650000000000002e-06, 0.0010859999999999999 },
		{ 0.0004149, 1.239e-05, 0.001946 },
		{ 0.00074160000000000003, 2.2019999999999999e-05, 0.0034859999999999999 },
		{ 0.0013680000000000001, 3.8999999999999999e-05, 0.0064500010000000003 },
		{ 0.0022360000000000001, 6.3999999999999997e-05, 0.01054999 },
		{ 0.0042430000000000002, 0.00012, 0.02005001 },
		{ 0.0076499999999999997, 0.00021699999999999999, 0.036209999999999999 },
		{ 0.01431, 0.00039599999999999998, 0.067850010000000002 },
		{ 0.023189999999999999, 0.00064000000000000005, 0.11020000000000001 },
		{ 0.04351, 0.0012099999999999999, 0.2074 },
		{ 0.077630000000000005, 0.0021800000000000001, 0.37130000000000002 },
		{ 0.13438, 0.0040000000000000001, 0.64559999999999995 },
		{ 0.21476999999999999, 0.0073000000000000001, 1.0390501000000001 },
		{ 0.28389999999999999, 0.011599999999999999, 1.3855999999999999 },
		{ 0.32850000000000001, 0.016840000000000001, 1.62296 },
		{ 0.34827999999999998, 0.023, 1.7470600000000001 },
		{ 0.34805999999999998, 0.0298, 1.7826 },
		{ 0.3362, 0.037999999999999999, 1.7721100000000001 },
		{ 0.31869999999999998, 0.048000000000000001, 1.7441 },
		{ 0.2908, 0.059999999999999998, 1.6692 },
		{ 0.25109999999999999, 0.073899999999999993, 1.5281 },
		{ 0.19536000000000001, 0.090980000000000005, 1.2876399999999999 },
		{ 0.1421, 0.11260000000000001, 1.0419 },
		{ 0.095640000000000003, 0.13902, 0.81295010000000001 },
		{ 0.057950010000000003, 0.16930000000000001, 0.61619999999999997 },
		{ 0.032009999999999997, 0.20802000000000001, 0.46517999999999998 },
		{ 0.0147, 0.2586, 0.3533 },
		{ 0.0048999999999999998, 0.32300000000000001, 0.27200000000000002 },
		{ 0.0023999999999999998, 0.4073, 0.21229999999999999 },
		{ 0.0092999999999999992, 0.503, 0.15820000000000001 },
		{ 0.029100000000000001, 0.60819999999999996, 0.11169999999999999 },
		{ 0.063270000000000007, 0.70999999999999996, 0.078249990000000005 },
		{ 0.1096, 0.79320000000000002, 0.057250009999999997 },
		{ 0.16550000000000001, 0.86199999999999999, 0.042160000000000003 },
		{ 0.2257499, 0.9148501, 0.029839999999999998 },
		{ 0.29039999999999999, 0.95399999999999996, 0.020299999999999999 },
		{ 0.35970000000000002, 0.98029999999999995, 0.0134 },
		{ 0.4334499, 0.99495009999999995, 0.0087499989999999996 },
		{ 0.51205009999999995, 1, 0.0057499990000000004 },
		{ 0.59450000000000003, 0.995, 0.0038999999999999998 },
		{ 0.6784, 0.97860000000000003, 0.0027499989999999999 },
		{ 0.7621, 0.95199999999999996, 0.0020999999999999999 },
		{ 0.84250000000000003, 0.91539999999999999, 0.0018 },
		{ 0.9163, 0.87, 0.0016500009999999999 },
		{ 0.97860000000000003, 0.81630000000000003, 0.0014 },
		{ 1.0263, 0.75700000000000001, 0.0011000000000000001 },
		{ 1.0567, 0.69489999999999996, 0.001 },
		{ 1.0622, 0.63100000000000001, 0.00080000000000000004 },
		{ 1.0456000000000001, 0.56679999999999997, 0.00059999999999999995 },
		{ 1.0025999999999999, 0.503, 0.00034000000000000002 },
		{ 0.93840000000000001, 0.44119999999999998, 0.00024000000000000001 },
		{ 0.85444989999999998, 0.38100000000000001, 0.00019000000000000001 },
		{ 0.75139999999999996, 0.32100000000000001, 0.0001 },
		{ 0.64239999999999997, 0.26500000000000001, 4.9999990000000002e-05 },
		{ 0.54190000000000005, 0.217, 3.0000000000000001e-05 },
		{ 0.44790000000000002, 0.17499999999999999, 2.0000000000000002e-05 },
		{ 0.36080000000000001, 0.13819999999999999, 1.0000000000000001e-05 },
		{ 0.28349999999999997, 0.107, 0.0 },
		{ 0.21870000000000001, 0.081600000000000006, 0.0 },
		{ 0.16489999999999999, 0.060999999999999999, 0.0 },
		{ 0.1212, 0.044580000000000002, 0.0 },
		{ 0.087400000000000005, 0.032000000000000001, 0.0 },
		{ 0.063600000000000004, 0.023199999999999998, 0.0 },
		{ 0.046769999999999999, 0.017000000000000001, 0.0 },
		{ 0.032899999999999999, 0.01192, 0.0 },
		{ 0.022700000000000001, 0.0082100000000000003, 0.0 },
		{ 0.01584, 0.0057229999999999998, 0.0 },
		{ 0.01135916, 0.0041019999999999997, 0.0 },
		{ 0.0081109159999999993, 0.0029290000000000002, 0.0 },
		{ 0.005790346, 0.002091, 0.0 },
		{ 0.0041094570000000004, 0.0014840000000000001, 0.0 },
		{ 0.0028993270000000002, 0.001047, 0.0 },
		{ 0.0020491900000000002, 0.00073999999999999999, 0.0 },
		{ 0.001439971, 0.00051999999999999995, 0.0 },
		{ 0.00099994930000000008, 0.0003611, 0.0 },
		{ 0.00069007859999999999, 0.00024919999999999999, 0.0 },
		{ 0.00047602130000000002, 0.0001719, 0.0 },
		{ 0.0003323011, 0.00012, 0.0 },
		{ 0.0002348261, 8.4800000000000001e-05, 0.0 },
		{ 0.00016615050000000001, 6.0000000000000002e-05, 0.0 },
		{ 0.000117413, 4.2400000000000001e-05, 0.0 },
		{ 8.3075270000000005e-05, 3.0000000000000001e-05, 0.0 },
		{ 5.8706519999999999e-05, 2.12e-05, 0.0 },
		{ 4.1509940000000003e-05, 1.499e-05, 0.0 },
		{ 2.935326e-05, 1.06e-05, 0.0 },
		{ 2.0673830000000001e-05, 7.4657000000000004e-06, 0.0 },
		{ 1.455977e-05, 5.2577999999999999e-06, 0.0 },
		{ 1.0253980000000001e-05, 3.7029000000000002e-06, 0.0 },
		{ 7.2214559999999999e-06, 2.6077999999999998e-06, 0.0 },
		{ 5.0858679999999998e-06, 1.8365999999999999e-06, 0.0 },
		{ 3.5816519999999999e-06, 1.2934e-06, 0.0 },
		{ 2.522525e-06, 9.1093000000000002e-07, 0.0 },
		{ 1.7765090000000001e-06, 6.4153e-07, 0.0 },
		{ 1.251141e-06, 4.5181000000000002e-07, 0.0 },
		};
		Cmf1964_10deg_MUTANT_ONLY = new double[95, 3]
		{
		{ 1.222e-07, 1.3398e-08, 5.3502699999999996e-07 },
		{ 9.1927000000000005e-07, 1.0065e-07, 4.0283e-06 },
		{ 5.9586e-06, 6.511e-07, 2.6143699999999998e-05 },
		{ 3.3266000000000003e-05, 3.625e-06, 0.00014621999999999999 },
		{ 0.00015995199999999999, 1.7363999999999998e-05, 0.00070477599999999995 },
		{ 0.00066244000000000003, 7.1559999999999999e-05, 0.0029277999999999999 },
		{ 0.0023616000000000002, 0.00025339999999999998, 0.010482200000000001 },
		{ 0.0072423000000000001, 0.00076849999999999998, 0.032343999999999998 },
		{ 0.0191097, 0.0020043999999999999, 0.086010900000000001 },
		{ 0.043400000000000001, 0.004509, 0.19711999999999999 },
		{ 0.084736000000000006, 0.0087559999999999999, 0.38936599999999999 },
		{ 0.14063800000000001, 0.014456, 0.65676000000000001 },
		{ 0.20449200000000001, 0.021391, 0.97254200000000002 },
		{ 0.264737, 0.029496999999999999, 1.2825 },
		{ 0.31467899999999999, 0.038676000000000002, 1.55348 },
		{ 0.35771900000000001, 0.049602, 1.7985 },
		{ 0.38373400000000002, 0.062077, 1.9672799999999999 },
		{ 0.38672600000000001, 0.074704000000000007, 2.0272999999999999 },
		{ 0.37070199999999998, 0.089455999999999994, 1.9947999999999999 },
		{ 0.34295700000000001, 0.106256, 1.9007000000000001 },
		{ 0.30227300000000001, 0.12820100000000001, 1.7453700000000001 },
		{ 0.25408500000000001, 0.15276100000000001, 1.5548999999999999 },
		{ 0.19561799999999999, 0.18518999999999999, 1.3175600000000001 },
		{ 0.13234899999999999, 0.21994, 1.0302 },
		{ 0.080506999999999995, 0.25358900000000001, 0.77212499999999995 },
		{ 0.041071999999999997, 0.29766500000000001, 0.57006000000000001 },
		{ 0.016171999999999999, 0.33913300000000002, 0.41525400000000001 },
		{ 0.0051320000000000003, 0.39537899999999998, 0.30235600000000001 },
		{ 0.0038159999999999999, 0.46077699999999999, 0.218502 },
		{ 0.015443999999999999, 0.53136000000000005, 0.159249 },
		{ 0.037464999999999998, 0.60674099999999997, 0.112044 },
		{ 0.071358000000000005, 0.68566000000000005, 0.082248000000000002 },
		{ 0.11774900000000001, 0.76175700000000002, 0.060708999999999999 },
		{ 0.172953, 0.82333000000000001, 0.043049999999999998 },
		{ 0.23649100000000001, 0.87521099999999996, 0.030450999999999999 },
		{ 0.30421300000000001, 0.92381000000000002, 0.020584000000000002 },
		{ 0.376772, 0.96198799999999995, 0.013676000000000001 },
		{ 0.45158399999999999, 0.98219999999999996, 0.0079179999999999997 },
		{ 0.52982600000000002, 0.991761, 0.0039880000000000002 },
		{ 0.61605299999999996, 0.99911000000000005, 0.001091 },
		{ 0.70522399999999996, 0.99734, 0.0 },
		{ 0.79383199999999998, 0.98238000000000003, 0.0 },
		{ 0.87865499999999996, 0.95555199999999996, 0.0 },
		{ 0.95116199999999995, 0.91517499999999996, 0.0 },
		{ 1.01416, 0.86893399999999998, 0.0 },
		{ 1.0743, 0.825623, 0.0 },
		{ 1.11852, 0.77740500000000001, 0.0 },
		{ 1.1343000000000001, 0.72035300000000002, 0.0 },
		{ 1.12399, 0.65834099999999995, 0.0 },
		{ 1.0891, 0.59387800000000002, 0.0 },
		{ 1.0304800000000001, 0.52796299999999996, 0.0 },
		{ 0.95074000000000003, 0.46183400000000002, 0.0 },
		{ 0.85629699999999997, 0.39805699999999999, 0.0 },
		{ 0.75492999999999999, 0.33955400000000002, 0.0 },
		{ 0.64746700000000001, 0.28349299999999999, 0.0 },
		{ 0.53510999999999997, 0.22825400000000001, 0.0 },
		{ 0.43156699999999998, 0.17982799999999999, 0.0 },
		{ 0.34369, 0.140211, 0.0 },
		{ 0.26832899999999998, 0.10763300000000001, 0.0 },
		{ 0.20430000000000001, 0.081186999999999995, 0.0 },
		{ 0.15256800000000001, 0.060281000000000001, 0.0 },
		{ 0.11221, 0.044096000000000003, 0.0 },
		{ 0.081260600000000002, 0.0318004, 0.0 },
		{ 0.057930000000000002, 0.022601699999999999, 0.0 },
		{ 0.0408508, 0.015905099999999998, 0.0 },
		{ 0.028622999999999999, 0.011130299999999999, 0.0 },
		{ 0.019941299999999999, 0.0077488000000000001, 0.0 },
		{ 0.013842, 0.0053750999999999998, 0.0 },
		{ 0.0095768799999999994, 0.0037177400000000002, 0.0 },
		{ 0.0066052000000000003, 0.00256456, 0.0 },
		{ 0.0045526300000000002, 0.0017684700000000001, 0.0 },
		{ 0.0031446999999999998, 0.0012223900000000001, 0.0 },
		{ 0.0021749600000000001, 0.00084619000000000003, 0.0 },
		{ 0.0015057, 0.00058644000000000003, 0.0 },
		{ 0.00104476, 0.00040740999999999998, 0.0 },
		{ 0.00072745000000000004, 0.00028404100000000001, 0.0 },
		{ 0.00050825800000000004, 0.00019872999999999999, 0.0 },
		{ 0.00035638, 0.00013955, 0.0 },
		{ 0.00025096899999999998, 9.8428e-05, 0.0 },
		{ 0.00017772999999999999, 6.9819000000000004e-05, 0.0 },
		{ 0.00012638999999999999, 4.9737000000000002e-05, 0.0 },
		{ 9.0150999999999998e-05, 3.5540500000000003e-05, 0.0 },
		{ 6.4525799999999998e-05, 2.5486e-05, 0.0 },
		{ 4.6338999999999998e-05, 1.83384e-05, 0.0 },
		{ 3.3411699999999997e-05, 1.3249e-05, 0.0 },
		{ 2.4209000000000001e-05, 9.6196000000000002e-06, 0.0 },
		{ 1.7611499999999999e-05, 7.0128e-06, 0.0 },
		{ 1.2855e-05, 5.1298000000000004e-06, 0.0 },
		{ 9.4136299999999995e-06, 3.7647300000000001e-06, 0.0 },
		{ 6.9129999999999997e-06, 2.7708100000000001e-06, 0.0 },
		{ 5.09347e-06, 2.0461300000000002e-06, 0.0 },
		{ 3.7670999999999999e-06, 1.51677e-06, 0.0 },
		{ 2.79531e-06, 1.12809e-06, 0.0 },
		{ 2.0820000000000001e-06, 8.4216000000000002e-07, 0.0 },
		{ 1.55314e-06, 6.2969999999999998e-07, 0.0 },
		};
	}
}
