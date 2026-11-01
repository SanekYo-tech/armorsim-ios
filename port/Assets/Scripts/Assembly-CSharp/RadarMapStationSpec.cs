using System;

public sealed class RadarMapStationSpec
{
	public readonly string Id;

	public readonly double FrequencyHz;

	public readonly double PeakPowerW;

	public readonly double PulseSeconds;

	public readonly double GainDbi;

	public readonly double BeamAzimuthDeg;

	public readonly double BeamElevationDeg;

	public readonly RadarMapScan Scan;

	public readonly double Rpm;

	public readonly double SectorDeg;

	public readonly double DwellSeconds;

	public readonly double PrfHz;

	public readonly double NoiseFigureDb;

	public readonly double LossDb;

	public readonly double RangeCellM;

	public readonly double InstrumentedRangeM;

	public readonly double ElevationMinDeg;

	public readonly double ElevationMaxDeg;

	public readonly double MastM;

	public readonly bool Is3D;

	public double WavelengthM => 299792458.0 / FrequencyHz;

	public RadarMapStationSpec(string id, double frequencyHz, double peakPowerW, double pulseSeconds, double gainDbi, double beamAzimuthDeg, double beamElevationDeg, RadarMapScan scan, double rpm, double sectorDeg, double dwellSeconds, double prfHz, double noiseFigureDb, double lossDb, double rangeCellM, double instrumentedRangeM, double elevationMinDeg, double elevationMaxDeg, double mastM)
	{
		Id = id;
		FrequencyHz = frequencyHz;
		PeakPowerW = peakPowerW;
		PulseSeconds = pulseSeconds;
		GainDbi = gainDbi;
		BeamAzimuthDeg = beamAzimuthDeg;
		BeamElevationDeg = beamElevationDeg;
		Scan = scan;
		Rpm = rpm;
		SectorDeg = sectorDeg;
		DwellSeconds = dwellSeconds;
		PrfHz = prfHz;
		NoiseFigureDb = noiseFigureDb;
		LossDb = lossDb;
		RangeCellM = rangeCellM;
		InstrumentedRangeM = instrumentedRangeM;
		ElevationMinDeg = elevationMinDeg;
		ElevationMaxDeg = elevationMaxDeg;
		MastM = mastM;
		Is3D = !double.IsNaN(beamElevationDeg);
	}
}
