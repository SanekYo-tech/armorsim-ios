using System;
using System.Collections.Generic;

public static class RadarScenarioGroundCatalog
{
	private static readonly int[] Indices;

	private static readonly float[] Radii;

	public const int DefaultPresetIndex = 3;

	public static IReadOnlyList<int> Allowed { get; }

	public static bool IsGround(int index)
	{
		return Array.IndexOf(Indices, index) >= 0;
	}

	public static int Next(int index, int direction)
	{
		int num = Array.IndexOf(Indices, index);
		if (num < 0)
		{
			return DefaultPresetIndex;
		}
		int num2 = (direction < 0) ? (-1) : 1;
		return Indices[(Indices.Length + num + num2) % Indices.Length];
	}

	public static float CoverageRadius(int index)
	{
		int num = Array.IndexOf(Indices, index);
		if (num < 0)
		{
			return 0f;
		}
		return Radii[num];
	}

	public static bool IsRadar(RadarScenarioStation station)
	{
		if (station != null && !station.IsDefencePost)
		{
			return IsGround(station.PresetIndex);
		}
		return false;
	}

	static RadarScenarioGroundCatalog()
	{
		Indices = new int[6] { 0, 1, 3, 4, 5, 6 };
		Radii = new float[6] { 120f, 100f, 90f, 80f, 65f, 60f };
		Allowed = Array.AsReadOnly(Indices);
	}
}
