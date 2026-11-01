using System.Collections.Generic;

public static class RadarCatalog
{
	public const string DefaultId = "x_air";

	public const string CustomId = "custom";

	private static readonly RadarPreset[] Data;

	private static readonly IReadOnlyList<RadarPreset> ReadOnlyData;

	public static IReadOnlyList<RadarPreset> Presets => null;

	public static RadarPreset Default => default;

	public static bool TryGet(string id, out RadarPreset preset)
	{
		preset = default;
		return false;
	}
}
