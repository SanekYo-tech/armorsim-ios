using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RadarScenarioData
{
	public const float MapWidth = 900f;

	public const float MapHeight = 450f;

	public const int MaxAircraft = 12;

	public const int MaxRadars = 2;

	public const int MaxDefencePosts = 4;

	public const int MaxWaypoints = 256;

	public const float MinSpeed = 0.25f;

	public const float MaxSpeed = 20f;

	public const float MaxAltitude = 12000f;

	public int Seed;

	public List<RadarScenarioAircraft> Aircraft;

	public List<RadarScenarioStation> Stations;

	public RadarDetectionModel DetectionModel;

	public int SeaState;

	public bool CoastMode;

	public const float MinSpeedMps = 60f;

	public const float MaxSpeedMps = 1000f;

	public const float MinBankDeg = 10f;

	public const float MaxBankDeg = 80f;

	public const float MinRollRateDegPerSec = 30f;

	public const float MaxRollRateDegPerSec = 360f;

	public const float MinCoatingF0Hz = 100000000f;

	public const float MaxCoatingF0Hz = 4E+10f;

	public static readonly string[] PhysicalCoatingIds;

	static RadarScenarioData()
	{
		PhysicalCoatingIds = new string[4] { "metal", "salisbury", "jaumann", "custom" };
	}

	public RadarScenarioData()
	{
		Seed = 23;
		Aircraft = new List<RadarScenarioAircraft>();
		Stations = new List<RadarScenarioStation>();
		SeaState = 2;
	}

	public static Vector2 MapCentre => new Vector2(450f, 225f);

	public bool Physical => DetectionModel == RadarDetectionModel.Physical;

	public static RadarScenarioData CreateDefault()
	{
		RadarScenarioData radarScenarioData = new RadarScenarioData();
		RadarScenarioAircraft radarScenarioAircraft = new RadarScenarioAircraft
		{
			Id = 1,
			Name = "YF-23 · 1",
			ModelName = "YF-23",
			Loop = true
		};
		radarScenarioAircraft.Route.Add(new RadarScenarioWaypoint
		{
			Position = new Vector2(16f, 18f),
			Altitude = 800f
		});
		radarScenarioAircraft.Route.Add(new RadarScenarioWaypoint
		{
			Position = new Vector2(37f, 46f),
			Altitude = 1800f
		});
		radarScenarioAircraft.Route.Add(new RadarScenarioWaypoint
		{
			Position = new Vector2(76f, 78f),
			Altitude = 3800f
		});
		radarScenarioAircraft.Route.Add(new RadarScenarioWaypoint
		{
			Position = new Vector2(83f, 24f),
			Altitude = 1200f
		});
		radarScenarioData.Aircraft.Add(radarScenarioAircraft);
		radarScenarioData.Stations.Add(new RadarScenarioStation
		{
			Id = 1,
			Position = new Vector2(48f, 39f),
			PresetIndex = 3
		});
		radarScenarioData.Stations.Add(new RadarScenarioStation
		{
			Id = 2,
			Position = new Vector2(31f, 62f),
			IsDefencePost = true
		});
		foreach (RadarScenarioAircraft item in radarScenarioData.Aircraft)
		{
			foreach (RadarScenarioWaypoint item2 in item.Route)
			{
				item2.Position = new Vector2(item2.Position.x * 3f + 300f, item2.Position.y * 3f + 75f);
			}
		}
		foreach (RadarScenarioStation item3 in radarScenarioData.Stations)
		{
			item3.Position = new Vector2(item3.Position.x * 3f + 300f, item3.Position.y * 3f + 75f);
		}
		return radarScenarioData;
	}

	public bool Validate(out string error)
	{
		if (Aircraft == null || Aircraft.Count < 1 || Aircraft.Count > MaxAircraft)
		{
			error = "Add between 1 and 12 aircraft.";
			return false;
		}
		if (Stations == null)
		{
			error = "Add at least one radar station.";
			return false;
		}
		HashSet<int> hashSet = new HashSet<int>();
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < Stations.Count; i++)
		{
			RadarScenarioStation radarScenarioStation = Stations[i];
			if (radarScenarioStation == null || !InsideMap(radarScenarioStation.Position))
			{
				error = "Every station must be inside the map.";
				return false;
			}
			if (radarScenarioStation.Id <= 0 || !hashSet.Add(radarScenarioStation.Id))
			{
				error = "Station IDs must be positive and unique.";
				return false;
			}
			if (!radarScenarioStation.IsDefencePost)
			{
				if (!RadarScenarioGroundCatalog.IsGround(radarScenarioStation.PresetIndex))
				{
					error = "Select an available ground radar preset.";
					return false;
				}
				num++;
			}
			else
			{
				num2++;
			}
		}
		if (num < 1 || num > MaxRadars || num2 > MaxDefencePosts)
		{
			error = "Use 1 or 2 radar stations and at most 4 defence posts.";
			return false;
		}
		hashSet.Clear();
		for (int j = 0; j < Aircraft.Count; j++)
		{
			RadarScenarioAircraft radarScenarioAircraft = Aircraft[j];
			if (radarScenarioAircraft == null || radarScenarioAircraft.Id <= 0 || !hashSet.Add(radarScenarioAircraft.Id))
			{
				error = "Aircraft IDs must be positive and unique.";
				return false;
			}
			if (string.IsNullOrWhiteSpace(radarScenarioAircraft.Name) || string.IsNullOrWhiteSpace(radarScenarioAircraft.ModelKey))
			{
				error = "Each aircraft needs a name and a model.";
				return false;
			}
			if (radarScenarioAircraft.Coating < 0 || radarScenarioAircraft.Coating >= RadarScenarioGameModel.CoatingNames.Length)
			{
				error = "Select an available fictional coating.";
				return false;
			}
			if (!Finite(radarScenarioAircraft.Speed) || radarScenarioAircraft.Speed < MinSpeed || radarScenarioAircraft.Speed > MaxSpeed)
			{
				error = "Aircraft speed must be between 0.25 and 20 map units/s.";
				return false;
			}
			if (radarScenarioAircraft.Route == null || radarScenarioAircraft.Route.Count < 2 || radarScenarioAircraft.Route.Count > MaxWaypoints)
			{
				error = "Draw between 2 and 256 waypoints for every aircraft.";
				return false;
			}
			float num3 = 0f;
			for (int k = 0; k < radarScenarioAircraft.Route.Count; k++)
			{
				RadarScenarioWaypoint radarScenarioWaypoint = radarScenarioAircraft.Route[k];
				if (radarScenarioWaypoint == null || !InsideMap(radarScenarioWaypoint.Position))
				{
					error = "All aircraft waypoints must be inside the map.";
					return false;
				}
				if (!Finite(radarScenarioWaypoint.Altitude) || radarScenarioWaypoint.Altitude < 0f || radarScenarioWaypoint.Altitude > MaxAltitude)
				{
					error = "Waypoint altitude must be between 0 and 12000.";
					return false;
				}
				if (k > 0)
				{
					num3 += Vector2.Distance(radarScenarioAircraft.Route[k - 1].Position, radarScenarioWaypoint.Position);
				}
			}
			if (num3 < 0.01f)
			{
				error = "Every route needs at least two different positions.";
				return false;
			}
		}
		return ValidatePhysicalFields(out error);
	}

	private bool ValidatePhysicalFields(out string error)
	{
		if (!Enum.IsDefined(typeof(RadarDetectionModel), DetectionModel))
		{
			error = "Unknown detection model.";
			return false;
		}
		if (SeaState < 0 || SeaState > 6)
		{
			error = "Sea state must be a WMO code 0-6.";
			return false;
		}
		foreach (RadarScenarioAircraft item in Aircraft)
		{
			if (!Finite(item.SpeedMps) || item.SpeedMps < MinSpeedMps || item.SpeedMps > MaxSpeedMps)
			{
				error = "Physical speed must be between 60 and 1000 m/s.";
				return false;
			}
			if (!Finite(item.MaxBankDeg) || item.MaxBankDeg < MinBankDeg || item.MaxBankDeg > MaxBankDeg)
			{
				error = "Maximum bank must be between 10 and 80 degrees.";
				return false;
			}
			if (!Finite(item.RollRateDegPerSec) || item.RollRateDegPerSec < MinRollRateDegPerSec || item.RollRateDegPerSec > MaxRollRateDegPerSec)
			{
				error = "Roll rate must be between 30 and 360 degrees/s.";
				return false;
			}
			if (Array.IndexOf(PhysicalCoatingIds, item.PhysicalCoatingId) < 0)
			{
				error = "Unknown physical coating.";
				return false;
			}
			if (!Finite(item.CoatingF0Hz) || item.CoatingF0Hz < MinCoatingF0Hz || item.CoatingF0Hz > MaxCoatingF0Hz)
			{
				error = "Coating tuning frequency must be between 0.1 and 40 GHz.";
				return false;
			}
			if (item.PhysicalCoatingId == "custom")
			{
				if (item.CustomLayer == null || item.CustomLayer.Length != 5)
				{
					error = "A custom layer needs five finite numbers.";
					return false;
				}
				for (int i = 0; i < item.CustomLayer.Length; i++)
				{
					if (!Finite(item.CustomLayer[i]))
					{
						error = "A custom layer needs five finite numbers.";
						return false;
					}
				}
			}
		}
		foreach (RadarScenarioStation item2 in Stations)
		{
			if (item2.MastHeightM != -1f)
			{
				if (!Finite(item2.MastHeightM) || item2.MastHeightM < RadarMapStations.MinMastM || item2.MastHeightM > RadarMapStations.MaxMastM)
				{
					error = "Mast height must be the station default or 3-60 m.";
					return false;
				}
			}
			if (item2.Polarization != 0 && item2.Polarization != 1)
			{
				error = "Polarization must be H or V.";
				return false;
			}
			if (!Finite(item2.SectorAzimuthDeg) || item2.SectorAzimuthDeg < 0f || item2.SectorAzimuthDeg > 360f)
			{
				error = "Sector centre must be between 0 and 360 degrees.";
				return false;
			}
			if (!string.IsNullOrEmpty(item2.PhysicalStationId) && !RadarMapStations.TryGet(item2.PhysicalStationId, out var spec))
			{
				error = "Unknown physical station.";
				return false;
			}
			if (item2.Archetype < -1 || item2.Archetype > 4)
			{
				error = "Unknown station archetype.";
				return false;
			}
		}
		error = string.Empty;
		return true;
	}

	public static bool InsideMap(Vector2 point)
	{
		if (!Finite(point.x) || !Finite(point.y) || point.x < 0f || point.y < 0f || MapWidth < point.x)
		{
			return false;
		}
		return MapHeight >= point.y;
	}

	public static bool Finite(float value)
	{
		return float.IsFinite(value);
	}

	private static bool Fail(string message, out string error)
	{
		error = message;
		return false;
	}
}
