using System;

public static class RadarMapGeo
{
	private const double Ae = 8494666.666666666;

	public static double SlantRange(double groundRange, double radarHeight, double targetHeight)
	{
		double num = Math.Sin(groundRange / (2.0 * Ae));
		return Math.Sqrt(4.0 * (radarHeight + Ae) * (targetHeight + Ae) * num * num + (targetHeight - radarHeight) * (targetHeight - radarHeight));
	}

	public static double ElevationAtRadar(double groundRange, double radarHeight, double targetHeight)
	{
		double num = SlantRange(groundRange, radarHeight, targetHeight);
		if (num > 0.0)
		{
			double num2 = ((radarHeight + 2.0 * Ae + targetHeight) * (targetHeight - radarHeight) - num * num) / (2.0 * num * (radarHeight + Ae));
			return Math.Asin(Clamp1(num2));
		}
		return 0.0;
	}

	public static double ElevationAtTarget(double groundRange, double radarHeight, double targetHeight)
	{
		double num = SlantRange(groundRange, radarHeight, targetHeight);
		if (num > 0.0)
		{
			double num2 = ((radarHeight + 2.0 * Ae + targetHeight) * (radarHeight - targetHeight) - num * num) / (2.0 * num * (targetHeight + Ae));
			return Math.Asin(Clamp1(num2));
		}
		return 0.0;
	}

	public static double Horizon(double radarHeight, double targetHeight)
	{
		return Math.Sqrt(Math.Max(0.0, radarHeight * radarHeight + radarHeight * (2.0 * Ae))) + Math.Sqrt(Math.Max(0.0, targetHeight * targetHeight + targetHeight * (2.0 * Ae)));
	}

	public static double Bulge(double x, double groundRange)
	{
		return (groundRange - x) * x / (2.0 * Ae);
	}

	public static double TargetHeight(double slantRange, double elevationAtRadar, double radarHeight)
	{
		double num = radarHeight + Ae;
		return Math.Sqrt(Math.Sin(elevationAtRadar) * (2.0 * slantRange * num) + (num * num + slantRange * slantRange)) - Ae;
	}

	private static double Clamp1(double value)
	{
		return Math.Min(1.0, Math.Max(-1.0, value));
	}
}
