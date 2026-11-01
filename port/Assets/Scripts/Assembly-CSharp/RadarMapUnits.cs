using System;

public static class RadarMapUnits
{
	public const double MetersPerMapUnit = 1000.0;

	public const double SpeedOfLight = 299792458.0;

	public const double Boltzmann = 1.380649E-23;

	public const double NoiseTemperature = 290.0;

	public const double Gravity = 9.80665;

	public const double EarthRadius = 6371000.0;

	public const double RefractionFactor = 1.3333333333333333;

	public const double EffectiveEarthRadius = 8494666.666666666;

	public const double DegToRad = Math.PI / 180.0;

	public const double RadToDeg = 180.0 / Math.PI;

	public static double MapToMeters(float mapUnits)
	{
		return (double)mapUnits * MetersPerMapUnit;
	}

	public static double MetersToMap(double meters)
	{
		return meters / MetersPerMapUnit;
	}

	public static double WrapPi(double radians)
	{
		double num = Math.IEEERemainder(radians, Math.PI * 2.0);
		if (num <= -Math.PI)
		{
			num += Math.PI * 2.0;
		}
		return num;
	}

	public static double Wrap2Pi(double radians)
	{
		double num = Math.IEEERemainder(radians, Math.PI * 2.0);
		if (num < 0.0)
		{
			num += Math.PI * 2.0;
		}
		if (num >= Math.PI * 2.0)
		{
			num = 0.0;
		}
		return num;
	}
}
