using System;
using System.Collections.Generic;

public static class RadarCatalog
{
	public const string DefaultId = "x_air";

	public const string CustomId = "custom";

	private static readonly RadarPreset[] Data;

	private static readonly IReadOnlyList<RadarPreset> ReadOnlyData;

	static RadarCatalog()
	{
		Data = new RadarPreset[10]
		{
			new RadarPreset("hf_oth", "HF", 15000000.0, 3000000.0, 30000000.0),
			new RadarPreset("vhf_ew", "VHF", 150000000.0, 30000000.0, 300000000.0),
			new RadarPreset("uhf_aew", "UHF", 430000000.0, 300000000.0, 1000000000.0),
			new RadarPreset("l_surv", "L", 1300000000.0, 1000000000.0, 2000000000.0),
			new RadarPreset("s_surv", "S", 3000000000.0, 2000000000.0, 4000000000.0),
			new RadarPreset("c_fc", "C", 5500000000.0, 4000000000.0, 8000000000.0),
			new RadarPreset("x_fc", "X", 10000000000.0, 8000000000.0, 12000000000.0),
			new RadarPreset("x_air", "X", 10000000000.0, 8000000000.0, 12000000000.0),
			new RadarPreset("ku_seek", "Ku", 15000000000.0, 12000000000.0, 18000000000.0),
			new RadarPreset("ka_mmw", "Ka", 35000000000.0, 27000000000.0, 40000000000.0)
		};
		ReadOnlyData = Array.AsReadOnly(Data);
	}

	public static IReadOnlyList<RadarPreset> Presets => ReadOnlyData;

	public static RadarPreset Default => Data[7];

	public static bool TryGet(string id, out RadarPreset preset)
	{
		for (int i = 0; i < Data.Length; i++)
		{
			if (string.Equals(Data[i].Id, id, StringComparison.Ordinal))
			{
				preset = Data[i];
				return true;
			}
		}
		preset = default;
		return false;
	}
}
