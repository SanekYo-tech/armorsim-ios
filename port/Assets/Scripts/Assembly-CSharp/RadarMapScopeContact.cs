using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RadarMapScopeContact
{
	public int StationId;

	public readonly List<int> PlotIds;

	public RadarPlot Last;

	public Vector2 LastMapPosition;

	public double ManualHeightM;

	public bool Active => PlotIds.Count > 0;

	public RadarMapScopeContact()
	{
		StationId = -1;
		PlotIds = new List<int>();
		ManualHeightM = double.NaN;
	}

	public bool Contains(int plotId)
	{
		return PlotIds.Contains(plotId);
	}

	public static Vector2 MapPositionOf(RadarPlot p, Vector2 stationMap, double antennaHeight = 0.0)
	{
		double ground;
		if (double.IsNaN(p.HeightM))
		{
			ground = p.RangeM;
		}
		else
		{
			double dh = p.HeightM - antennaHeight;
			ground = Math.Sqrt(Math.Max(0.0, p.RangeM * p.RangeM - dh * dh));
		}
		double bearing = p.BearingDeg * RadarMapUnits.DegToRad;
		return new Vector2((float)(Math.Cos(bearing) * ground / RadarMapUnits.MetersPerMapUnit) + stationMap.x, (float)(Math.Sin(bearing) * ground / RadarMapUnits.MetersPerMapUnit) + stationMap.y);
	}

	public void Start(int stationId, RadarPlot p, Vector2 stationMap)
	{
		StationId = stationId;
		PlotIds.Clear();
		PlotIds.Add(p.PlotId);
		Last = p;
		LastMapPosition = MapPositionOf(p, stationMap);
		ManualHeightM = double.NaN;
	}

	public void Offer(RadarPlot p, Vector2 stationMap, double gateKm)
	{
		if (PlotIds.Count <= 0)
		{
			return;
		}
		if (p.Scan <= Last.Scan)
		{
			return;
		}
		if (PlotIds.Contains(p.PlotId))
		{
			return;
		}
		Vector2 mapPosition = MapPositionOf(p, stationMap);
		if (Vector2.Distance(mapPosition, LastMapPosition) > gateKm)
		{
			return;
		}
		PlotIds.Add(p.PlotId);
		Last = p;
		LastMapPosition = mapPosition;
	}

	public void Clear()
	{
		StationId = -1;
		PlotIds.Clear();
		ManualHeightM = double.NaN;
	}
}
