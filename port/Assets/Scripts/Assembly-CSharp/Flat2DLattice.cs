using UnityEngine;

public static class Flat2DLattice
{
	public static int Index(float coord, float dx)
	{
		return Mathf.RoundToInt(coord / dx - 0.5f);
	}

	public static long Key(Vector2 p, float dx)
	{
		return ((long)Index(p.x, dx) << 32) ^ (uint)Index(p.y, dx);
	}
}
