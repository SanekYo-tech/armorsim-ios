using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class RadarScenarioStore
{
	[Serializable]
	private sealed class Envelope
	{
		public int Version;

		public RadarScenarioData Data;
	}

	public const int MaxBytes = 2097152;

	public const int CurrentVersion = 4;

	public static string DefaultPath => Path.Combine(Application.persistentDataPath, "RadarScenario", "scenario.json");

	public static void Save(string path, RadarScenarioData data)
	{
		if (data == null)
		{
			throw new InvalidDataException("Missing scenario");
		}
		string text;
		if (!data.Validate(out text))
		{
			throw new InvalidDataException(text);
		}
		Envelope envelope = new Envelope
		{
			Version = CurrentVersion,
			Data = data
		};
		string text2 = JsonUtility.ToJson(envelope, prettyPrint: true);
		if (Encoding.UTF8.GetByteCount(text2) > MaxBytes)
		{
			throw new InvalidDataException("Scenario is too large");
		}
		string fullPath = Path.GetFullPath(path);
		Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
		string text3 = fullPath + ".tmp";
		File.WriteAllText(text3, text2, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		if (File.Exists(fullPath))
		{
			File.Replace(text3, fullPath, null);
		}
		else
		{
			File.Move(text3, fullPath);
		}
	}

	public static RadarScenarioData Load(string path)
	{
		FileInfo fileInfo = new FileInfo(path);
		if (fileInfo.Length > MaxBytes)
		{
			throw new InvalidDataException("Scenario is too large");
		}
		string json = File.ReadAllText(path, Encoding.UTF8);
		Envelope envelope = JsonUtility.FromJson<Envelope>(json);
		if (envelope == null || envelope.Version < 1 || envelope.Version > CurrentVersion || envelope.Data == null)
		{
			throw new InvalidDataException("Unsupported scenario file");
		}
		if (envelope.Version < 3)
		{
			MigrateLegacy(envelope.Data, envelope.Version);
		}
		if (envelope.Version < CurrentVersion)
		{
			ApplyPhysicalDefaults(envelope.Data);
		}
		if (!envelope.Data.Validate(out string error))
		{
			throw new InvalidDataException(error);
		}
		return envelope.Data;
	}

	private static void MigrateLegacy(RadarScenarioData data, int version)
	{
		float num = ((version == 1) ? 100f : 300f);
		if (data.Aircraft == null || data.Stations == null)
		{
			throw new InvalidDataException("Invalid legacy scene");
		}
		foreach (RadarScenarioAircraft aircraft in data.Aircraft)
		{
			if (aircraft == null || aircraft.Route == null)
			{
				throw new InvalidDataException("Invalid legacy aircraft");
			}
			foreach (RadarScenarioWaypoint waypoint in aircraft.Route)
			{
				if (waypoint == null || !LegacyPoint(waypoint.Position, num))
				{
					throw new InvalidDataException("Invalid legacy waypoint");
				}
			}
		}
		foreach (RadarScenarioStation station in data.Stations)
		{
			if (station == null || !LegacyPoint(station.Position, num))
			{
				throw new InvalidDataException("Invalid legacy station");
			}
		}
		float num2 = 900f / num;
		float num3 = 450f / num;
		foreach (RadarScenarioAircraft aircraft2 in data.Aircraft)
		{
			foreach (RadarScenarioWaypoint waypoint2 in aircraft2.Route)
			{
				waypoint2.Position = new Vector2(waypoint2.Position.x * num2, waypoint2.Position.y * num3);
			}
		}
		foreach (RadarScenarioStation station2 in data.Stations)
		{
			station2.Position = new Vector2(station2.Position.x * num2, station2.Position.y * num3);
			if (version == 1 && !RadarScenarioGroundCatalog.IsGround(station2.PresetIndex))
			{
				station2.PresetIndex = 3;
			}
		}
	}

	private static void ApplyPhysicalDefaults(RadarScenarioData data)
	{
		if (data.Aircraft == null || data.Stations == null)
		{
			throw new InvalidDataException("Invalid legacy scene");
		}
		data.DetectionModel = RadarDetectionModel.Arcade;
		data.SeaState = 2;
		data.CoastMode = false;
		foreach (RadarScenarioAircraft aircraft in data.Aircraft)
		{
			if (aircraft == null)
			{
				throw new InvalidDataException("Invalid legacy aircraft");
			}
			aircraft.SpeedMps = 250f;
			aircraft.MaxBankDeg = 60f;
			aircraft.RollRateDegPerSec = 90f;
			aircraft.PhysicalCoatingId = "metal";
			aircraft.CoatingF0Hz = 1E+10f;
			aircraft.CustomLayer = null;
		}
		foreach (RadarScenarioStation station in data.Stations)
		{
			if (station == null)
			{
				throw new InvalidDataException("Invalid legacy station");
			}
			station.MastHeightM = -1f;
			station.Polarization = 0;
			station.PhysicalStationId = "";
			station.Archetype = -1;
		}
	}

	private static bool LegacyPoint(Vector2 p, float size)
	{
		if (RadarScenarioData.Finite(p.x) && RadarScenarioData.Finite(p.y) && p.x >= 0f && p.y >= 0f && size >= p.x && size >= p.y)
		{
			return true;
		}
		return false;
	}
}
