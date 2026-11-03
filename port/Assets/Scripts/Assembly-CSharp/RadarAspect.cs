using System;
using Unity.Mathematics;

public readonly struct RadarAspect
{
	public readonly double GroundRange;

	public readonly double SlantRange;

	public readonly double ElevationAtRadar;

	public readonly double ElevationAtTarget;

	public readonly double BearingToRadar;

	public readonly double Azimuth;

	public readonly double Elevation;

	public readonly double Xi;

	public readonly double3 ModelDirection;

	public readonly double Horizon;

	public double AzimuthDeg => Azimuth * (180.0 / Math.PI);

	public double ElevationDeg => Elevation * (180.0 / Math.PI);

	public double XiDeg => Xi * (180.0 / Math.PI);

	public double HorizonMargin => Horizon - GroundRange;

	public RadarAspect(double groundRange, double slantRange, double elevationAtRadar, double elevationAtTarget, double bearingToRadar, double azimuth, double elevation, double xi, double3 modelDirection, double horizon)
	{
		GroundRange = groundRange;
		SlantRange = slantRange;
		ElevationAtRadar = elevationAtRadar;
		ElevationAtTarget = elevationAtTarget;
		BearingToRadar = bearingToRadar;
		Azimuth = azimuth;
		Elevation = elevation;
		Xi = xi;
		ModelDirection = modelDirection;
		Horizon = horizon;
	}
}
