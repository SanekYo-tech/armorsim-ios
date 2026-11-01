using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public sealed class RadarMapModelRegistry
{
	[Serializable]
	private sealed class Envelope
	{
		public int Version;

		public List<RadarMapModelRecord> Models;
	}

	public const string Yf23Key = "yf23";

	public const string FileName = "models.json";

	public readonly string FilePath;

	public string LastError { get; private set; }

	public static string DefaultPath => Path.Combine(Application.persistentDataPath, "RadarMap", "models.json");

	public static RadarMapModelRegistry Default => new RadarMapModelRegistry(DefaultPath);

	public RadarMapModelRegistry(string filePath)
	{
		FilePath = filePath;
	}

	public static RadarMapModelRecord Yf23Record()
	{
		return new RadarMapModelRecord
		{
			Key = "yf23",
			SourcePath = "Resources/RadarYF23",
			FileSha256 = "",
			Unit = RadarLengthUnit.CustomLength.ToString(),
			CustomLengthM = 20.54,
			Orientation = OrientationInts(RadarOrientation.Identity),
			DisplayName = "YF-23",
			LongestSideM = 20.54
		};
	}

	public List<RadarMapModelRecord> Choices()
	{
		List<RadarMapModelRecord> list = new List<RadarMapModelRecord>();
		list.Add(Yf23Record());
		list.AddRange(LoadRecords());
		return list;
	}

	public RadarMapModelRecord Find(string key)
	{
		if (key == "yf23")
		{
			return Yf23Record();
		}
		foreach (RadarMapModelRecord item in LoadRecords())
		{
			if (item.Key == key)
			{
				return item;
			}
		}
		return null;
	}

	public string LatestKeyForPath(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		string text = Path.GetFullPath(path);
		List<RadarMapModelRecord> list = LoadRecords();
		for (int i = list.Count - 1; i >= 0; i--)
		{
			if (string.Equals(list[i].SourcePath, text, StringComparison.OrdinalIgnoreCase))
			{
				return list[i].Key;
			}
		}
		return null;
	}

	public List<RadarMapModelRecord> LoadRecords()
	{
		LastError = null;
		if (!File.Exists(FilePath))
		{
			return new List<RadarMapModelRecord>();
		}
		try
		{
			Envelope envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(FilePath, Encoding.UTF8));
			if (envelope == null || envelope.Version != 1 || envelope.Models == null)
			{
				throw new InvalidDataException("models.json");
			}
			List<RadarMapModelRecord> list = new List<RadarMapModelRecord>();
			foreach (RadarMapModelRecord model in envelope.Models)
			{
				if (model != null && !string.IsNullOrEmpty(model.Key) && model.Key != "yf23" && model.Orientation != null && model.Orientation.Length == 9)
				{
					list.Add(model);
				}
			}
			return list;
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			Debug.LogWarning("[RADAR MAP] model registry unreadable: " + ex.Message);
			return new List<RadarMapModelRecord>();
		}
	}

	public RadarMapModelRecord Register(string sourcePath, RadarLengthUnit unit, double customLengthMeters, RadarOrientation orientation, string displayName, double longestSideMeters)
	{
		string text = Path.GetFullPath(sourcePath);
		byte[] array = File.ReadAllBytes(text);
		RadarMapModelRecord radarMapModelRecord = new RadarMapModelRecord
		{
			SourcePath = text,
			FileSha256 = Hex(Sha256(array)),
			Unit = unit.ToString(),
			CustomLengthM = ((unit == RadarLengthUnit.CustomLength) ? customLengthMeters : 0.0),
			Orientation = OrientationInts(orientation),
			DisplayName = (string.IsNullOrWhiteSpace(displayName) ? Path.GetFileNameWithoutExtension(text) : displayName),
			LongestSideM = longestSideMeters
		};
		radarMapModelRecord.Key = KeyOf(array, radarMapModelRecord.Unit, radarMapModelRecord.CustomLengthM, radarMapModelRecord.Orientation);
		List<RadarMapModelRecord> list = LoadRecords();
		if (LastError != null)
		{
			throw new InvalidDataException("Model registry is unreadable: " + LastError);
		}
		list.RemoveAll((RadarMapModelRecord r) => r.Key == radarMapModelRecord.Key);
		list.Add(radarMapModelRecord);
		Envelope envelope = new Envelope
		{
			Version = 1,
			Models = list
		};
		Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
		string text2 = FilePath + ".tmp";
		string contents = JsonUtility.ToJson(envelope, true);
		File.WriteAllText(text2, contents, new UTF8Encoding(false));
		if (File.Exists(FilePath))
		{
			File.Replace(text2, FilePath, null);
		}
		else
		{
			File.Move(text2, FilePath);
		}
		return radarMapModelRecord;
	}

	public bool TryBuildModel(string key, out RadarModel model, out string refusalKey)
	{
		model = null;
		refusalKey = null;
		try
		{
			if (key == "yf23")
			{
				model = RadarBuiltinModels.Yf23Model(RadarBuiltinModels.ReadYf23Resource());
				return true;
			}
			RadarMapModelRecord radarMapModelRecord = Find(key);
			if (radarMapModelRecord == null || !File.Exists(radarMapModelRecord.SourcePath))
			{
				refusalKey = "map.phys.refuse.model";
				return false;
			}
			byte[] array = File.ReadAllBytes(radarMapModelRecord.SourcePath);
			if (Hex(Sha256(array)) != radarMapModelRecord.FileSha256)
			{
				refusalKey = "map.phys.refuse.changed";
				return false;
			}
			if (!TryOrientation(radarMapModelRecord.Orientation, out RadarOrientation orientation) || !Enum.TryParse<RadarLengthUnit>(radarMapModelRecord.Unit, out RadarLengthUnit result))
			{
				refusalKey = "map.phys.refuse.model";
				return false;
			}
			AeroMeshData data = ParseBytes(array, Path.GetExtension(radarMapModelRecord.SourcePath));
			model = RadarModelImport.FromAero(data, result, radarMapModelRecord.CustomLengthM, orientation);
			refusalKey = null;
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[RADAR MAP] model " + key + " refused: " + ex.Message);
			refusalKey = "map.phys.refuse.model";
			return false;
		}
	}

	public static AeroMeshData ParseBytes(byte[] bytes, string extension)
	{
		if (bytes != null && bytes.Length >= 84 && 84L + 50L * BitConverter.ToUInt32(bytes, 80) == bytes.Length)
		{
			return RadarLargeStlAdapter.ParseLargeBinary(bytes);
		}
		return AeroImporter.Parse(bytes, extension);
	}

	public static string KeyOf(byte[] fileBytes, string unit, double customLength, int[] orientation)
	{
		using (SHA256 sHA = SHA256.Create())
		{
			sHA.TransformBlock(fileBytes, 0, fileBytes.Length, null, 0);
			StringBuilder stringBuilder = new StringBuilder("|unit=");
			stringBuilder.Append(unit).Append("|length=").Append(customLength.ToString("R", CultureInfo.InvariantCulture)).Append("|orientation=");
			for (int i = 0; i < 9; i++)
			{
				stringBuilder.Append((i == 0) ? "" : ",").Append(orientation[i].ToString(CultureInfo.InvariantCulture));
			}
			byte[] array = Encoding.UTF8.GetBytes(stringBuilder.ToString());
			sHA.TransformFinalBlock(array, 0, array.Length);
			return Hex(sHA.Hash);
		}
	}

	public static int[] OrientationInts(RadarOrientation o)
	{
		return new int[9] { o.M00, o.M01, o.M02, o.M10, o.M11, o.M12, o.M20, o.M21, o.M22 };
	}

	public static bool TryOrientation(int[] m, out RadarOrientation orientation)
	{
		orientation = RadarOrientation.Identity;
		if (m == null || m.Length != 9)
		{
			return false;
		}
		List<RadarOrientation> list = new List<RadarOrientation>();
		list.Add(RadarOrientation.Identity);
		for (int i = 0; i < list.Count; i++)
		{
			RadarOrientation radarOrientation = list[i];
			int[] array = OrientationInts(radarOrientation);
			bool flag = true;
			for (int j = 0; j < 9; j++)
			{
				if (array[j] != m[j])
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				orientation = radarOrientation;
				return true;
			}
			RadarOrientation[] array2 = new RadarOrientation[3]
			{
				radarOrientation.RotateX(),
				radarOrientation.RotateY(),
				radarOrientation.RotateZ()
			};
			for (int k = 0; k < array2.Length; k++)
			{
				if (!list.Contains(array2[k]))
				{
					list.Add(array2[k]);
				}
			}
		}
		return false;
	}

	public static byte[] Sha256(byte[] bytes)
	{
		using (SHA256 sHA = SHA256.Create())
		{
			return sHA.ComputeHash(bytes);
		}
	}

	public static string Hex(byte[] hash)
	{
		StringBuilder stringBuilder = new StringBuilder(hash.Length * 2);
		for (int i = 0; i < hash.Length; i++)
		{
			stringBuilder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
		}
		return stringBuilder.ToString();
	}
}
