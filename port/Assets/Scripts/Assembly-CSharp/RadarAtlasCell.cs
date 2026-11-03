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
		I = i;
		J = j;
		AzimuthCentreDeg = az;
		ElevationCentreDeg = el;
		K2 = k2;
		MaxSigmaHH = max;
		SigmaHH = sigmaHH;
	}
}
