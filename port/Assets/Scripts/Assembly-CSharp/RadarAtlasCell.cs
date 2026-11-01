public readonly struct RadarAtlasCell
{
	public readonly int I;

	public readonly int J;

	public readonly double AzimuthCentreDeg;

	public readonly double ElevationCentreDeg;

	public readonly double K2;

	public readonly double MaxSigmaHH;

	public readonly double SigmaHH;

	public RadarAtlasCell(int i, int j, double az, double el, double k2, double max, double sigmaHH)
	{
		I = 0;
		J = 0;
		AzimuthCentreDeg = 0.0;
		ElevationCentreDeg = 0.0;
		K2 = 0.0;
		MaxSigmaHH = 0.0;
		SigmaHH = 0.0;
	}
}
