public sealed class HalfLatheTopology
{
	public LatheKind kind;

	public int profilePoints;

	public int segments;

	public int sideVertexCount;

	public int capVertexCount;

	public int[] triangles;

	public int[] sideTriangles;

	public int[] capTriangles;

	public int VertexCount => sideVertexCount + capVertexCount;

	public int RingBase(int k)
	{
		if (kind == LatheKind.Tube)
		{
			return (segments + 1) * k;
		}
		if (k <= 0)
		{
			return 0;
		}
		if (k >= profilePoints - 1)
		{
			return (profilePoints - 2) * (segments + 1) + 1;
		}
		return (k - 1) * (segments + 1) + 1;
	}

	public bool IsPole(int k)
	{
		if (kind != LatheKind.Capped)
		{
			return false;
		}
		if (k == 0)
		{
			return true;
		}
		return k == profilePoints - 1;
	}

	public int CapIndex(int k, int side)
	{
		return side * profilePoints + sideVertexCount + k;
	}
}
