using System;
using System.Collections.Generic;

[Serializable]
public sealed class RadarScenarioAircraft
{
	public int Id;

	public string Name;

	public string ModelName;

	public string ModelKey;

	public int Coating;

	public float Speed;

	public bool Loop;

	public List<RadarScenarioWaypoint> Route;

	public float SpeedMps;

	public float MaxBankDeg;

	public float RollRateDegPerSec;

	public string PhysicalCoatingId;

	public float CoatingF0Hz;

	public float[] CustomLayer;
}
