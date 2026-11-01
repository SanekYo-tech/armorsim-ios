using System;
using UnityEngine;

public sealed class RadarMapWorld
{
	public const double HeightScaleM = 3000.0;

	public const double LandClearanceM = 50.0;

	public const double SeaClearanceM = 1.0;

	public readonly int Seed;

	public readonly bool Coast;

	public readonly int SeaState;

	public RadarMapWorld(int seed, bool coast, int seaState)
	{
		if (seaState < 0 || seaState > 6)
		{
			throw new ArgumentOutOfRangeException("seaState");
		}
		Seed = seed;
		Coast = coast;
		SeaState = seaState;
	}

	public static RadarMapWorld Of(RadarScenarioData data)
	{
		return new RadarMapWorld(data.Seed, data.CoastMode, data.SeaState);
	}

	public RadarScenarioTerrainSample Sample(double x, double y)
	{
		return RadarScenarioTerrain.Sample(new Vector2((float)(x / 1000.0), (float)(y / 1000.0)), Seed, Coast);
	}

	public static double HeightOf(RadarScenarioTerrainSample sample)
	{
		if (sample.Kind == RadarScenarioTerrainKind.Sea)
		{
			return 0.0;
		}
		return (double)sample.Height01 * HeightScaleM;
	}

	public double Height(double x, double y)
	{
		return HeightOf(Sample(x, y));
	}

	public static RadarMapSurface SurfaceOf(RadarScenarioTerrainKind kind)
	{
		switch (kind)
		{
		case RadarScenarioTerrainKind.Plain:
			return RadarMapSurface.Ground;
		case RadarScenarioTerrainKind.Water:
			return RadarMapSurface.FreshWater;
		case RadarScenarioTerrainKind.Sea:
			return RadarMapSurface.SeaWater;
		default:
			return RadarMapSurface.Diffuse;
		}
	}

	public double Floor(double x, double y)
	{
		RadarScenarioTerrainSample sample = Sample(x, y);
		return HeightOf(sample) + ((sample.Kind == RadarScenarioTerrainKind.Sea) ? SeaClearanceM : LandClearanceM);
	}
}
