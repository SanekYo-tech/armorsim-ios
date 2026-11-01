using System;
using System.Threading;
using Unity.Mathematics;

public static class RadarMapFrames
{
	private static long xiDegenerate;

	public const double XiDegenerateFloor = 1E-09;

	public static long XiDegenerateCount => Interlocked.Read(ref xiDegenerate);

	public static void Axes(double heading, double pitch, double bank, out double3 right, out double3 up, out double3 nose)
	{
		double num = Math.Sin(heading);
		double num2 = Math.Cos(heading);
		double num3 = Math.Sin(pitch);
		double num4 = Math.Cos(pitch);
		double num5 = Math.Sin(bank);
		double num6 = Math.Cos(bank);
		double3 b = new double3(num, num2, 0.0);
		double3 b2 = new double3(0.0, 0.0, 1.0);
		double3 b3 = new double3(num2, -num, 0.0);
		nose = b * num4 + b2 * num3;
		double3 b4 = b2 * num4 - b * num3;
		right = b3 * num6 - b4 * num5;
		up = b4 * num6 + b3 * num5;
	}

	public static void ModelAngles(double heading, double pitch, double bank, double bearingToRadar, double elevationToRadar, out double azimuth, out double elevation, out double xi, out double3 modelDirection)
	{
		RadarMapFrames.Axes(heading, pitch, bank, out double3 right, out double3 up, out double3 nose);
		double num = Math.Cos(elevationToRadar);
		double num2 = Math.Sin(bearingToRadar) * num;
		double num3 = Math.Cos(bearingToRadar) * num;
		double num4 = Math.Sin(elevationToRadar);
		double3 b = new double3(num2, num3, num4);
		modelDirection = new double3(math.dot(right, b), math.dot(up, b), math.dot(nose, b));
		azimuth = Math.Atan2(modelDirection.x, modelDirection.z);
		elevation = Math.Asin(Math.Max(-1.0, Math.Min(1.0, modelDirection.y)));
		double num5 = Math.Cos(azimuth);
		double num6 = Math.Sin(azimuth);
		double3 b2 = new double3(num5, 0.0, -num6);
		double3 b3 = math.cross(modelDirection, b2);
		double3 b4 = new double3(right.z, up.z, nose.z);
		double3 b5 = math.cross(b4, modelDirection);
		double num7 = Math.Sqrt(math.dot(b5, b5));
		if (num7 < XiDegenerateFloor)
		{
			Interlocked.Increment(ref xiDegenerate);
			xi = 0.0;
			return;
		}
		double3 b6 = b5 / num7;
		xi = Math.Atan2(math.dot(b3, b6), math.dot(b6, b2));
	}

	public static RadarAspect Aspect(RadarMapPose pose, double3 antenna)
	{
		double num = antenna.x - pose.Position.x;
		double num2 = antenna.y - pose.Position.y;
		double groundRange = Math.Sqrt(num * num + num2 * num2);
		double num3 = RadarMapGeo.SlantRange(groundRange, antenna.z, pose.Position.z);
		double num4 = RadarMapGeo.ElevationAtRadar(groundRange, antenna.z, pose.Position.z);
		double num5 = RadarMapGeo.ElevationAtTarget(groundRange, antenna.z, pose.Position.z);
		double num6 = RadarMapUnits.Wrap2Pi(Math.Atan2(num, num2));
		RadarMapFrames.ModelAngles(pose.Heading, pose.Pitch, pose.Bank, num6, num5, out double azimuth, out double num7, out double xi, out double3 modelDirection);
		double num8 = RadarMapGeo.Horizon(antenna.z, pose.Position.z);
		return new RadarAspect(groundRange, num3, num4, num5, num6, azimuth, num7, xi, modelDirection, num8);
	}
}
