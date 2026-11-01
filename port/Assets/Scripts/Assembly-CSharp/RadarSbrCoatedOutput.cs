internal sealed class RadarSbrCoatedOutput
{
	public readonly RadarCVector[,] ElectricByOrder;

	public readonly RadarCVector[,] MagneticByOrder;

	public readonly Complex64[,,] ByFacet;

	public readonly int[] MaxBounceByFacet;

	public RadarPath[] Paths;

	public RadarPath[] FirstOrderPaths;

	public RadarSbrCoatedOutput(int faceCount)
	{
		ElectricByOrder = new RadarCVector[4, 2];
		MagneticByOrder = new RadarCVector[4, 2];
		ByFacet = new Complex64[2, 2, faceCount];
		MaxBounceByFacet = new int[faceCount];
	}
}
