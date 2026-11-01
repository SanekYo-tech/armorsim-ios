using Unity.Mathematics;

public readonly struct RadarHit
{
	public readonly int Face;

	public readonly double Distance;

	public readonly double3 Point;

	public bool Found => false;

	public RadarHit(int face, double distance, double3 point)
	{
		Face = 0;
		Distance = 0.0;
		Point = default;
	}
}
