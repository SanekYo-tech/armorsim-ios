using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
	public const int CurrentPayloadVersion = 1;

	public static Func<List<SaveEntry>> ExtraPresets;

	public static string SavesDir => Path.Combine(Application.persistentDataPath, "ArmorSim", "Saves");

	private static void EnsureDir()
	{
		if (!Directory.Exists(SavesDir))
		{
			Directory.CreateDirectory(SavesDir);
		}
	}

	public static List<SaveEntry> LoadUserSaves()
	{
		List<SaveEntry> list = new List<SaveEntry>();
		EnsureDir();
		string[] array = Directory.GetFiles(SavesDir, "*.json");
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i];
			try
			{
				SaveEntry saveEntry = JsonUtility.FromJson<SaveEntry>(File.ReadAllText(text));
				if (saveEntry != null)
				{
					saveEntry.isPreset = false;
					list.Add(saveEntry);
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[SaveSystem] Битый файл сохранения " + text + ": " + ex.Message);
			}
		}
		list.Sort((SaveEntry a, SaveEntry b) => string.CompareOrdinal(b.createdUtc, a.createdUtc));
		return list;
	}

	public static SaveEntry Create(string displayName, string payload, int objectCount, TargetSection section, SimFamily family)
	{
		try
		{
			EnsureDir();
			SaveEntry saveEntry = new SaveEntry();
			saveEntry.section = section.ToString();
			saveEntry.family = family.ToString();
			saveEntry.id = Guid.NewGuid().ToString("N");
			if (string.IsNullOrEmpty(displayName))
			{
				displayName = Localization.T("saves.untitled");
			}
			saveEntry.displayName = displayName;
			saveEntry.createdUtc = DateTime.UtcNow.ToString("o");
			saveEntry.objectCount = objectCount;
			saveEntry.payloadVersion = CurrentPayloadVersion;
			saveEntry.payload = payload;
			saveEntry.isPreset = false;
			File.WriteAllText(Path.Combine(SavesDir, saveEntry.id + ".json"), JsonUtility.ToJson(saveEntry, true));
			return saveEntry;
		}
		catch (Exception ex)
		{
			Debug.LogError("[SaveSystem] Не удалось записать сохранение: " + ex.Message);
			return null;
		}
	}

	public static bool Delete(string id)
	{
		try
		{
			string path = Path.Combine(SavesDir, id + ".json");
			if (!File.Exists(path))
			{
				return false;
			}
			File.Delete(path);
			SavePreview.Delete(id);
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError("[SaveSystem] Не удалось удалить сохранение: " + ex.Message);
			return false;
		}
	}

	public static List<SaveEntry> LoadPresets()
	{
		List<SaveEntry> list = new List<SaveEntry>();
		try
		{
			if (ExtraPresets != null)
			{
				foreach (SaveEntry item in ExtraPresets())
				{
					if (item == null)
					{
						continue;
					}
					item.isPreset = true;
					list.Add(item);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[SaveSystem] Поставщик пресетов упал: " + ex.Message);
		}
		List<SaveEntry> list2 = new List<SaveEntry>();
		TextAsset[] array = Resources.LoadAll<TextAsset>("Presets");
		for (int i = 0; i < array.Length; i++)
		{
			TextAsset textAsset = array[i];
			try
			{
				SaveEntry saveEntry = JsonUtility.FromJson<SaveEntry>(textAsset.text);
				if (saveEntry != null)
				{
					saveEntry.isPreset = true;
					if (string.IsNullOrEmpty(saveEntry.id))
					{
						saveEntry.id = textAsset.name;
					}
					if (string.IsNullOrEmpty(saveEntry.displayName))
					{
						saveEntry.displayName = textAsset.name;
					}
					list2.Add(saveEntry);
				}
			}
			catch (Exception ex2)
			{
				Debug.LogWarning("[SaveSystem] Битый пресет " + textAsset.name + ": " + ex2.Message);
			}
		}
		list2.Sort((SaveEntry x, SaveEntry y) => string.CompareOrdinal(x.displayName, y.displayName));
		list.AddRange(list2);
		return list;
	}
}
