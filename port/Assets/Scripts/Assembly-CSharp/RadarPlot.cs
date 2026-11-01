public readonly struct RadarPlot
{
	public readonly int PlotId;

	public readonly int StationId;

	public readonly long Scan;

	public readonly double Time;

	public readonly double RangeM;

	public readonly double BearingDeg;

	public readonly double ElevationDeg;

	public readonly double HeightM;

	public readonly double SnrDb;

	public readonly double SigmaHatM2;

	public readonly bool LowAngle;

	public RadarPlot(int plotId, int stationId, long scan, double time, double rangeM, double bearingDeg, double elevationDeg, double heightM, double snrDb, double sigmaHatM2, bool lowAngle = false)
	{
		PlotId = 0;
		StationId = 0;
		Scan = 0L;
		Time = 0.0;
		RangeM = 0.0;
		BearingDeg = 0.0;
		ElevationDeg = 0.0;
		HeightM = 0.0;
		SnrDb = 0.0;
		SigmaHatM2 = 0.0;
		LowAngle = false;
	}
}
