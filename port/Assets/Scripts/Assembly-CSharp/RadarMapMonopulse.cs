using System;

public static class RadarMapMonopulse
{
	public struct Result
	{
		public double ThetaDirect;

		public double ThetaImage;

		public double Ratio;

		public double Alpha;

		public double ThetaHat;

		public double Height;

		public bool Clamped;

		public double Error => ThetaHat - ThetaDirect;
	}

	public const double LowAltitudePatternRatio = 0.3;

	public const double LowAltitudeElevationMinDeg = 0.2;

	public const double LowAltitudeLossBelowDeg = 0.5;

	public const double LowAltitudeLossDb = -3.0;

	public const double LowAngleRatio = 0.5;

	public static long Clamps;

	public static long Evaluations;

	public static double Indicated(double thetaDirect, double thetaImage, double ratio, double alpha)
	{
		double num = Math.Cos(alpha) * ratio;
		double num2 = Math.Sin(alpha) * ratio;
		double num3 = num + 1.0;
		double num4 = (thetaDirect + num * thetaImage) * num3 + num2 * thetaImage * num2;
		double num5 = num3 * num3 + num2 * num2;
		return num4 / num5;
	}

	public static Result Evaluate(in RadarMapPath path, double beamElevationDeg)
	{
		Result result = default(Result);
		Evaluations++;
		if (!path.HasRay)
		{
			result.Height = path.TargetHeight;
			return result;
		}
		result.ThetaDirect = (path.Ray.TargetEffective - path.Ray.RadarEffective) / path.Ray.GroundRange;
		result.ThetaImage = -(path.Ray.TargetEffective + path.Ray.RadarEffective) / path.Ray.GroundRange;
		if (path.Reflection)
		{
			result.Ratio = path.MonopulseRatio;
			result.Alpha = path.Alpha;
		}
		double num = Indicated(result.ThetaDirect, result.ThetaImage, result.Ratio, result.Alpha);
		double num2 = beamElevationDeg * 0.5 * RadarMapUnits.DegToRad;
		double num3 = Math.Min(Math.Max(num, result.ThetaImage - num2), result.ThetaDirect + num2);
		result.ThetaHat = num3;
		result.Clamped = num3 != num;
		if (result.Clamped)
		{
			Clamps++;
		}
		result.Height = HeightFromTangent(path.SurfaceLevel, path.Ray, num3);
		return result;
	}

	public static double HeightFromTangent(double level, in RadarMapTwoRay ray, double thetaHat)
	{
		return level + ray.RadarEffective + thetaHat * ray.GroundRange + ray.TargetDistance * ray.TargetDistance / (2.0 * RadarMapUnits.EffectiveEarthRadius);
	}

	public static void ResetGuards()
	{
		Evaluations = 0L;
		Clamps = 0L;
	}
}
