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
		Time = time;
		SigmaBarM2 = sigmaBar;
		MeanSnr = meanSnr;
		Snr = snr;
		F4 = f4;
		Detected = detected;
		AircraftId = aircraftId;
		StationIndex = stationIndex;
	}
}
