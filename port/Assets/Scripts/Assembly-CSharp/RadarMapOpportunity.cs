public readonly struct RadarMapOpportunity
{
	public readonly double Time;

	public readonly double SigmaBarM2;

	public readonly double MeanSnr;

	public readonly double Snr;

	public readonly double F4;

	public readonly bool Detected;

	public readonly int AircraftId;

	public readonly int StationIndex;

	public RadarMapOpportunity(double time, int aircraftId, int stationIndex, double sigmaBar, double meanSnr, double snr, double f4, bool detected)
	{
		Time = 0.0;
		SigmaBarM2 = 0.0;
		MeanSnr = 0.0;
		Snr = 0.0;
		F4 = 0.0;
		Detected = false;
		AircraftId = 0;
		StationIndex = 0;
	}
}
