using System;

public static class RadarMapPropagation
{
	private const double Ae = 8494666.666666666;

	public const double WaterTemperatureC = 15.0;

	public const double SeaSalinity = 35.0;

	public const double FreshSalinity = 0.0;

	public const double RiverRoughnessM = 0.02;

	public const double PlainRoughnessM = 0.3;

	private static readonly double[] SeaStateH13 = new double[9] { 0.0, 0.05, 0.3, 0.875, 1.875, 3.25, 5.0, 7.5, 11.5 };

	public const int MaxSeaState = 6;

	public const double GroundMinGHz = 1.0;

	public const double GroundMaxGHz = 10.0;

	public static long SpecularPoints;

	public static long SpecularRefusals;

	public static long ArccosClamps;

	public const double BesselAsymptoticFrom = 30.0;

	public static double SigmaHSea(int seaState)
	{
		if (seaState < 0 || seaState >= SeaStateH13.Length)
		{
			throw new ArgumentOutOfRangeException("seaState");
		}
		return SeaStateH13[seaState] * 0.25;
	}

	public static double SeaStateWaveHeight(int seaState)
	{
		return SeaStateH13[seaState];
	}

	public static Complex64 WaterPermittivity(double fGHz, double temperatureC, double salinity, out double conductivity)
	{
		double num = 300.0 / (temperatureC + 273.15) - 1.0;
		double num2 = 77.66 + 103.3 * num;
		double num3 = 20.2 - 146.4 * num + 316.0 * num * num;
		double num4 = 0.0671 * num2;
		double num5 = Math.Exp(4.74868E-06 * salinity * salinity - 0.0033333 * salinity) * num2;
		double num6 = Math.Exp(0.000176032 * salinity * salinity - 0.00628908 * salinity - 9.22144E-05 * temperatureC * salinity) * num4;
		double num7 = (3.52 - 7.52 * num) * (1.0 + salinity * (0.000157883 * temperatureC - 0.00204265));
		double num8 = salinity * (0.014409 * salinity * salinity + 5.45216 * salinity + 37.5109) / (salinity * salinity + 182.283 * salinity + 1004.75);
		double num9 = 4.3047E-09 * temperatureC * temperatureC * temperatureC * temperatureC - 2.991E-06 * temperatureC * temperatureC * temperatureC + 0.0004738817 * temperatureC * temperatureC + 0.08607 * temperatureC + 2.903602;
		double num10 = 1.0 + (temperatureC - 15.0) * (3.2841 - 0.099486 * salinity * salinity) / ((salinity * salinity + 69.024 * salinity + 84.85) * (temperatureC + 49.843 + 0.00198 * salinity * salinity - 0.2276 * salinity));
		conductivity = num8 * num9 * num10;
		double num11 = 18.0 * conductivity / fGHz;
		double num12 = 8.9795E-09 * temperatureC * temperatureC * temperatureC * temperatureC + 3.5594E-07 * temperatureC * temperatureC * temperatureC + 3.6764E-06 * temperatureC * temperatureC - 7.9208E-05 * temperatureC + 0.0023232;
		double num13 = fGHz / (num3 * (1.0 + salinity * num12));
		double num14 = fGHz / (39.8 * num3 * (1.0 + salinity * (0.000181176 * temperatureC - 0.0199723)));
		double num15 = (num6 - num7) * num14 / (1.0 + num14 * num14) + (num5 - num6) * num13 / (1.0 + num13 * num13) + num11;
		double num16 = num7 + (num5 - num6) / (1.0 + num13 * num13) + (num6 - num7) / (1.0 + num14 * num14);
		return new Complex64(num16, 0.0 - num15);
	}

	public static Complex64 GroundPermittivity(double fGHz, out double conductivity, out bool clamped)
	{
		double num = Math.Min(Math.Max(fGHz, 1.0), 10.0);
		clamped = num != fGHz;
		conductivity = 0.035 * Math.Pow(num, 1.63);
		return new Complex64(15.0 * Math.Pow(num, -0.1), conductivity * -17.98 / num);
	}

	public static Complex64 Permittivity(RadarMapSurface surface, double frequencyHz, out bool clamped)
	{
		double num = frequencyHz / 1000000000.0;
		clamped = false;
		double conductivity = 0.0;
		switch (surface)
		{
		case RadarMapSurface.FreshWater:
			return WaterPermittivity(num, 15.0, 0.0, out conductivity);
		case RadarMapSurface.SeaWater:
			return WaterPermittivity(num, 15.0, 35.0, out conductivity);
		case RadarMapSurface.Ground:
			return GroundPermittivity(num, out conductivity, out clamped);
		default:
			return Complex64.Zero;
		}
	}

	public static void Fresnel(Complex64 eps, double grazing, out Complex64 gammaH, out Complex64 gammaV)
	{
		double num = Math.Sin(grazing);
		double num2 = Math.Cos(grazing);
		Complex64 complex = RadarCoating.Sqrt(eps - new Complex64(num2 * num2));
		if (complex.Real < 0.0)
		{
			complex *= -1.0;
		}
		Complex64 complex2 = new Complex64(num);
		gammaH = RadarCoating.Div(complex2 - complex, complex2 + complex);
		Complex64 complex3 = eps * num;
		gammaV = RadarCoating.Div(complex3 - complex, complex3 + complex);
	}

	public static double RhoMillerBrownVegh(double sigmaH, double grazing, double wavelength)
	{
		double num = 2.0 * Math.PI * sigmaH * Math.Sin(grazing) / wavelength;
		return ScaledBesselI0(2.0 * num * num);
	}

	public static double RhoAment(double sigmaH, double grazing, double wavelength)
	{
		double num = 2.0 * Math.PI * sigmaH * Math.Sin(grazing) / wavelength;
		return Math.Exp(-2.0 * num * num);
	}

	public static double ScaledBesselI0(double x)
	{
		x = Math.Abs(x);
		if (x > 30.0)
		{
			double num = 1.0;
			double num2 = 1.0;
			for (int i = 1; i <= 40; i++)
			{
				double num3 = 2.0 * i - 1.0;
				double num4 = num2 * num3 * num3 / (i * 8.0 * x);
				if (num4 >= num2 || num * 1E-18 > num4)
				{
					break;
				}
				num += num4;
				num2 = num4;
			}
			return num / Math.Sqrt(2.0 * Math.PI * x);
		}
		double num5 = x * 0.25 * x;
		double num6 = 1.0;
		double num7 = 1.0;
		for (int j = 1; j < 400; j++)
		{
			num7 *= num5 / (j * j);
			num6 += num7;
			if (num6 * 1E-17 > num7)
			{
				break;
			}
		}
		return Math.Exp(-x) * num6;
	}

	public static bool KerrBlake(double groundRange, double radarHeight, double targetHeight, out RadarMapTwoRay ray)
	{
		ray = default;
		ray.GroundRange = groundRange;
		ray.RadarHeight = radarHeight;
		ray.TargetHeight = targetHeight;
		if (radarHeight <= 0.0 || targetHeight <= 0.0 || groundRange <= 0.0 || groundRange >= RadarMapGeo.Horizon(radarHeight, targetHeight))
		{
			SpecularRefusals++;
			return false;
		}
		double num = Math.Min(radarHeight, targetHeight);
		double num2 = Math.Max(radarHeight, targetHeight);
		double num3 = Math.Sqrt(2.0 * Ae * (num2 + num) + groundRange * groundRange * 0.25) * (2.0 / Math.Sqrt(3.0));
		double num4 = (num2 - num) * (2.0 * Ae) * groundRange / (num3 * num3 * num3);
		if (num4 > 1.0 || num4 < -1.0)
		{
			ArccosClamps++;
			num4 = Math.Max(-1.0, Math.Min(1.0, num4));
		}
		double num5 = groundRange * 0.5 - num3 * Math.Cos((Math.Acos(num4) + Math.PI) / 3.0);
		if (targetHeight < radarHeight)
		{
			num5 = groundRange - num5;
		}
		ray.RadarDistance = num5;
		ray.TargetDistance = groundRange - num5;
		ray.RadarEffective = radarHeight - num5 * num5 / (2.0 * Ae);
		ray.TargetEffective = targetHeight - ray.TargetDistance * ray.TargetDistance / (2.0 * Ae);
		ray.PathDifference = 2.0 * ray.RadarEffective * ray.TargetEffective / groundRange;
		ray.Grazing = ((num5 > 0.0) ? Math.Atan(ray.RadarEffective / num5) : 0.0);
		SpecularPoints++;
		if (ray.RadarEffective > 0.0 && ray.TargetEffective > 0.0)
		{
			return true;
		}
		SpecularRefusals++;
		return false;
	}

	public static double Roughness(RadarMapSurface surface, int seaState, double grazing, double wavelength)
	{
		switch (surface)
		{
		case RadarMapSurface.FreshWater:
			return RhoMillerBrownVegh(0.02, grazing, wavelength);
		case RadarMapSurface.SeaWater:
			return RhoMillerBrownVegh(SigmaHSea(seaState), grazing, wavelength);
		case RadarMapSurface.Ground:
			return RhoAment(0.3, grazing, wavelength);
		default:
			return 0.0;
		}
	}

	public static double Factor(in RadarMapTwoRay ray, double wavelength, Complex64 gamma, double rho, double patternRatio, out Complex64 reflected)
	{
		reflected = gamma * (rho * patternRatio) * Complex64.Phase(-2.0 * Math.PI / wavelength * ray.PathDifference);
		return (new Complex64(1.0) + reflected).Magnitude;
	}

	public static double Db4(double f)
	{
		return 40.0 * Math.Log10(Math.Max(f, 1E-12));
	}

	public static string SurfaceKey(RadarMapSurface surface)
	{
		switch (surface)
		{
		case RadarMapSurface.FreshWater:
			return "map.prop.surface.fresh";
		case RadarMapSurface.SeaWater:
			return "map.prop.surface.sea";
		case RadarMapSurface.Ground:
			return "map.prop.surface.ground";
		default:
			return "map.prop.surface.diffuse";
		}
	}

	public static void ResetGuards()
	{
		ArccosClamps = 0L;
		SpecularRefusals = 0L;
		SpecularPoints = 0L;
	}
}
