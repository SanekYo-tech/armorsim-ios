using System;
using System.Collections.Generic;

public static class RadarMapStations
{
	public const string HfId = "hf_oth";

	public const float MinMastM = 3f;

	public const float MaxMastM = 60f;

	private static readonly RadarMapStationSpec[] Table;

	public static IReadOnlyList<RadarMapStationSpec> All { get; }

	static RadarMapStations()
	{
		Table = new RadarMapStationSpec[7]
		{
			new RadarMapStationSpec("vhf_ew", 150000000.0, 30000.0, 0.0001, 22.0, 6.0, double.NaN, RadarMapScan.Rotating, 0.0, 0.0, 0.0, 300.0, 4.0, 8.0, 600.0, 450000.0, -2.0, 20.0, 10.0),
			new RadarMapStationSpec("l_surv", 1300000000.0, 60000.0, 0.0001, 33.0, 2.0, double.NaN, RadarMapScan.Rotating, 0.0, 0.0, 0.0, 300.0, 3.0, 8.0, 150.0, 450000.0, -2.0, 30.0, 20.0),
			new RadarMapStationSpec("s_surv", 3000000000.0, 200000.0, 2.5E-05, 36.0, 1.5, 2.0, RadarMapScan.Rotating, 0.0, 0.0, 0.0, 400.0, 3.0, 8.0, 150.0, 375000.0, -2.0, 30.0, 20.0),
			new RadarMapStationSpec("c_fc", 5500000000.0, 100000.0, 1E-05, 39.0, 1.7, 1.7, RadarMapScan.Sector, 0.0, 120.0, 0.01, 1000.0, 3.0, 8.0, 75.0, 200000.0, -2.0, 60.0, 5.0),
			new RadarMapStationSpec("x_fc", 10000000000.0, 30000.0, 1E-05, 41.0, 1.2, 1.2, RadarMapScan.Sector, 0.0, 90.0, 0.005, 2000.0, 3.0, 8.0, 30.0, 150000.0, -2.0, 60.0, 25.0),
			new RadarMapStationSpec("x_rot60", 9500000000.0, 20000.0, 2E-05, 38.0, 1.5, 1.5, RadarMapScan.Rotating, 60.0, 0.0, 0.0, 3000.0, 3.0, 8.0, 50.0, 120000.0, -2.0, 40.0, 12.0),
			new RadarMapStationSpec("x_rot30", 9500000000.0, 10000.0, 2E-05, 36.0, 2.0, 2.0, RadarMapScan.Rotating, 30.0, 0.0, 0.0, 2000.0, 3.0, 8.0, 50.0, 100000.0, -2.0, 40.0, 10.0)
		};
		All = Array.AsReadOnly(Table);
	}

	public static bool TryGet(string id, out RadarMapStationSpec spec)
	{
		for (int i = 0; i < Table.Length; i++)
		{
			if (string.Equals(Table[i].Id, id, StringComparison.Ordinal))
			{
				spec = Table[i];
				return true;
			}
		}
		spec = null;
		return false;
	}

	public static string IdFor(RadarScenarioStation station)
	{
		if (station == null)
		{
			return null;
		}
		if (!string.IsNullOrEmpty(station.PhysicalStationId))
		{
			return station.PhysicalStationId;
		}
		if (station.PresetIndex < 0 || station.PresetIndex >= RadarCatalog.Presets.Count)
		{
			return null;
		}
		return RadarCatalog.Presets[station.PresetIndex].Id;
	}

	public static bool TryResolve(RadarScenarioStation station, out RadarMapStationSpec spec, out string refusalKey)
	{
		spec = null;
		string text = IdFor(station);
		if (text == HfId)
		{
			refusalKey = "map.phys.refuse.hf";
			return false;
		}
		if (TryGet(text, out spec))
		{
			refusalKey = null;
			return true;
		}
		refusalKey = "map.phys.refuse.station";
		return false;
	}

	public static double MastHeight(RadarScenarioStation station, RadarMapStationSpec spec)
	{
		if (station != null && station.MastHeightM >= 0f)
		{
			return (double)station.MastHeightM;
		}
		return spec.MastM;
	}

	public static bool ValidMast(float mast)
	{
		if (mast == -1f)
		{
			return true;
		}
		if (float.IsNaN(mast) || mast < MinMastM)
		{
			return false;
		}
		return mast <= MaxMastM;
	}
}
