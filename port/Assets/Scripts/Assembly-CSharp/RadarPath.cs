using Unity.Mathematics;

public sealed class RadarPath
{
	public readonly double3[] Points;

	public readonly int[] Faces;

	public readonly int BounceCount;

	public readonly double Power;

	public readonly double Amplitude;

	public readonly bool EscapesToFarField;

	public readonly bool Truncated;

	public RadarPath(double3[] points, int[] faces, int bounceCount, double amplitude = 0.0, bool escapesToFarField = false, bool truncated = false)
	{
		Points = points;
		Faces = faces;
		BounceCount = bounceCount;
		Power = 1.0;
		Amplitude = amplitude;
		EscapesToFarField = escapesToFarField;
		Truncated = truncated;
	}
}
