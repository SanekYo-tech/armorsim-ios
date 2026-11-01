public sealed class LatheTopology
{
	public LatheKind kind;

	public int profilePoints;

	public int segments;

	public int vertexCount;

	public int[] triangles;

	public int RingBase(int k)
	{
		if (kind == LatheKind.Tube)
		{
			return k * segments;
		}
		if (k <= 0)
		{
			return 0;
		}
		if (k >= profilePoints - 1)
		{
			return (profilePoints - 2) * segments + 1;
		}
		return (k - 1) * segments + 1;
	}

	public bool IsPole(int k)
	{
		if (kind != LatheKind.Capped)
		{
			return false;
		}
		return k == 0 || k == profilePoints - 1;
	}
}
