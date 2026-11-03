using Unity.Mathematics;

public readonly struct RadarHit
{
	public readonly int Face;

	public readonly double Distance;

	public readonly double3 Point;

	public bool Found => Face >= 0;

	public RadarHit(int face, double distance, double3 point)
	{
		Face = face;
		Distance = distance;
		Point = point;
	}
}
