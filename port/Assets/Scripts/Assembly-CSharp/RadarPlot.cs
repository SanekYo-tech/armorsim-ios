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
		PlotId = plotId;
		StationId = stationId;
		Scan = scan;
		Time = time;
		RangeM = rangeM;
		BearingDeg = bearingDeg;
		ElevationDeg = elevationDeg;
		HeightM = heightM;
		SnrDb = snrDb;
		SigmaHatM2 = sigmaHatM2;
		LowAngle = lowAngle;
	}
}
