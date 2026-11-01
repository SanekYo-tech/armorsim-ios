public sealed class RadarAtlasCellResult
{
	public readonly double[] C3;

	public double K2;

	public double MaxSigmaHH;

	public double ReciprocityRatio;

	public long ShadowRays;

	public long EdgeOnFacets;

	public RadarAtlasCellResult()
	{
		C3 = new double[9];
	}

	public double SigmaHH => C3[0] * 12.566370614359172;
}
