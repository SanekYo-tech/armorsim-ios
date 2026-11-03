using Unity.Mathematics;

internal readonly struct RadarPoTriangle
{
	public readonly int Face;

	public readonly double3 A;

	public readonly double3 B;

	public readonly double3 C;

	public readonly double3 EffectiveNormal;

	public RadarPoTriangle(int face, double3 a, double3 b, double3 c, double3 effectiveNormal)
	{
		Face = face;
		A = a;
		B = b;
		C = c;
		EffectiveNormal = effectiveNormal;
	}
}
