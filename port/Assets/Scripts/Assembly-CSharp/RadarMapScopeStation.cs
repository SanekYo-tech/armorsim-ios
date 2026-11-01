using System.Collections.Generic;
using UnityEngine;

public sealed class RadarMapScopeStation
{
	public int StationId;

	public string Name;

	public RadarMapStationSpec Spec;

	public RadarMapScanner Scanner;

	public bool Emitting;

	public bool LowAltitude;

	public double Pfa;

	public double AntennaHeightM;

	public Vector2 MapPosition;

	public IReadOnlyList<RadarPlot> Plots;
}
