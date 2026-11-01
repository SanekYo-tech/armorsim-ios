using Unity.Mathematics;

public readonly struct RadarDirectionBasis
{
	public readonly double3 ToRadar;

	public readonly double3 Incident;

	public readonly double3 Horizontal;

	public readonly double3 Vertical;

	public RadarDirectionBasis(double3 toRadar, double3 horizontal, double3 vertical)
	{
		ToRadar = default;
		Incident = default;
		Horizontal = default;
		Vertical = default;
	}
}
