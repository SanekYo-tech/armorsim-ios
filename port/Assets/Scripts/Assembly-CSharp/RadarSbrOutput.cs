internal sealed class RadarSbrOutput
{
	public readonly RadarCVector[,] ByOrder;

	public readonly Complex64[,,] ByFacet;

	public readonly int[] MaxBounceByFacet;

	public RadarPath[] Paths;

	public RadarPath[] FirstOrderPaths;

	public RadarSbrOutput(int faceCount)
	{
		ByOrder = new RadarCVector[4, 2];
		ByFacet = new Complex64[2, 2, faceCount];
		MaxBounceByFacet = new int[faceCount];
	}
}
