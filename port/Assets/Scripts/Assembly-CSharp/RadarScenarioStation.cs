using System;
using UnityEngine;

[Serializable]
public sealed class RadarScenarioStation
{
	public int Id;

	public Vector2 Position;

	public int PresetIndex;

	public bool IsDefencePost;

	public float MastHeightM;

	public int Polarization;

	public float SectorAzimuthDeg;

	public string PhysicalStationId;

	public int Archetype;
}
