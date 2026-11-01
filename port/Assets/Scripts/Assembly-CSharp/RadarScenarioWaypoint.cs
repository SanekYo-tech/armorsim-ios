using System;
using UnityEngine;

[Serializable]
public sealed class RadarScenarioWaypoint
{
	public Vector2 Position;

	public float Altitude;

	public RadarScenarioWaypoint()
	{
	}

	public RadarScenarioWaypoint(Vector2 position, float altitude = 1200f)
	{
	}
}
