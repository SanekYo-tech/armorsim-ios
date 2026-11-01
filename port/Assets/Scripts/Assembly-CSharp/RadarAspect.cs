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

	public double AzimuthDeg => 0.0;

	public double ElevationDeg => 0.0;

	public double XiDeg => 0.0;

	public double HorizonMargin => 0.0;

	public RadarAspect(double groundRange, double slantRange, double elevationAtRadar, double elevationAtTarget, double bearingToRadar, double azimuth, double elevation, double xi, double3 modelDirection, double horizon)
	{
		GroundRange = 0.0;
		SlantRange = 0.0;
		ElevationAtRadar = 0.0;
		ElevationAtTarget = 0.0;
		BearingToRadar = 0.0;
		Azimuth = 0.0;
		Elevation = 0.0;
		Xi = 0.0;
		ModelDirection = default;
		Horizon = 0.0;
	}
}
