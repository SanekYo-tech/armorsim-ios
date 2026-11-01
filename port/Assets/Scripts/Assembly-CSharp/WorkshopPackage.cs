using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class WorkshopPackage
{
	public const int CurrentVersion = 1;

	public const string EntryFileName = "item.armorsim.json";

	public const string ManifestFileName = "workshop.json";

	public const string TagArmorLayout = "Armor layout";

	public static WorkshopReadResult Accept(string manifestJson, string entryJson)
	{
		if (string.IsNullOrEmpty(manifestJson))
		{
			return Refuse(WorkshopReadStatus.RefusedNoManifest, "workshop.reject.no_manifest", "в папке предмета нет workshop.json");
		}
		WorkshopManifest manifest;
		try
		{
			manifest = JsonUtility.FromJson<WorkshopManifest>(manifestJson);
		}
		catch (Exception ex)
		{
			return Refuse(WorkshopReadStatus.RefusedMalformed, "workshop.reject.malformed", "манифест не разбирается: " + ex.Message);
		}
		if (manifest == null)
		{
			return Refuse(WorkshopReadStatus.RefusedMalformed, "workshop.reject.malformed", "манифест пуст");
		}
		if (manifest.formatVersion < 1)
		{
			return Refuse(WorkshopReadStatus.RefusedNoVersion, "workshop.reject.no_version", "formatVersion = " + manifest.formatVersion + " (поля нет в файле)");
		}
		if (manifest.formatVersion > CurrentVersion)
		{
			return Refuse(WorkshopReadStatus.RefusedNewer, "workshop.reject.newer", "formatVersion = " + manifest.formatVersion + ", сборка знает до " + CurrentVersion);
		}
		if (string.IsNullOrEmpty(entryJson))
		{
			return Refuse(WorkshopReadStatus.RefusedMalformed, "workshop.reject.no_entry", "в папке предмета нет item.armorsim.json");
		}
		SaveEntry entry;
		try
		{
			entry = JsonUtility.FromJson<SaveEntry>(entryJson);
		}
		catch (Exception ex2)
		{
			return Refuse(WorkshopReadStatus.RefusedMalformed, "workshop.reject.malformed", "item.armorsim.json не разбирается: " + ex2.Message);
		}
		if (entry == null || string.IsNullOrEmpty(entry.payload))
		{
			return Refuse(WorkshopReadStatus.RefusedMalformed, "workshop.reject.no_entry", "снимок сборки пуст");
		}
		if (entry.payloadVersion > SaveSystem.CurrentPayloadVersion)
		{
			return Refuse(WorkshopReadStatus.RefusedNewerPayload, "workshop.reject.newer", "payloadVersion = " + entry.payloadVersion + " > " + SaveSystem.CurrentPayloadVersion);
		}
		SimFamily entryFamily;
		if (!Enum.TryParse<SimFamily>(entry.family, out entryFamily))
		{
			return Refuse(WorkshopReadStatus.RefusedMalformed, "workshop.reject.malformed", "снимок называет неизвестное семейство «" + entry.family + "»");
		}
		SimFamily manifestFamily;
		if (!Enum.TryParse<SimFamily>(manifest.family, out manifestFamily))
		{
			return Refuse(WorkshopReadStatus.RefusedMalformed, "workshop.reject.malformed", "манифест называет неизвестное семейство «" + manifest.family + "»");
		}
		if (manifestFamily != entryFamily)
		{
			return Refuse(WorkshopReadStatus.RefusedFamilyMismatch, "workshop.reject.family_mismatch", "манифест: " + manifestFamily + ", снимок: " + entryFamily);
		}
		TargetSection section;
		if (!Enum.TryParse<TargetSection>(entry.section, out section))
		{
			return Refuse(WorkshopReadStatus.RefusedMalformed, "workshop.reject.malformed", "снимок называет неизвестный раздел «" + entry.section + "»");
		}
		if (string.IsNullOrEmpty(entry.displayName))
		{
			entry.displayName = manifest.title;
		}
		entry.isPreset = false;
		return new WorkshopReadResult
		{
			status = WorkshopReadStatus.Ok,
			manifest = manifest,
			entry = entry,
			detail = ""
		};
	}

	private static WorkshopReadResult Refuse(WorkshopReadStatus st, string reasonKey, string detail)
	{
		return new WorkshopReadResult
		{
			status = st,
			reasonKey = reasonKey,
			detail = detail
		};
	}

	public static WorkshopReadResult ReadFolder(string folder)
	{
		if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
		{
			return Refuse(WorkshopReadStatus.RefusedNoManifest, "workshop.reject.no_files", "папки нет: " + folder);
		}
		try
		{
			string manifestJson = null;
			string entryJson = null;
			string manifestPath = Path.Combine(folder, ManifestFileName);
			string entryPath = Path.Combine(folder, EntryFileName);
			if (File.Exists(manifestPath))
			{
				manifestJson = File.ReadAllText(manifestPath, Encoding.UTF8);
			}
			if (File.Exists(entryPath))
			{
				entryJson = File.ReadAllText(entryPath, Encoding.UTF8);
			}
			return Accept(manifestJson, entryJson);
		}
		catch (Exception ex)
		{
			return Refuse(WorkshopReadStatus.RefusedMalformed, "workshop.reject.unreadable", "чтение не удалось: " + ex.Message);
		}
	}

	public static bool WriteFolder(string folder, SaveEntry entry, WorkshopManifest manifest, out string detail)
	{
		detail = "";
		if (entry == null)
		{
			detail = "нечего упаковывать: сохранение пусто";
			return false;
		}
		if (manifest == null)
		{
			detail = "нечего упаковывать: манифест пуст";
			return false;
		}
		try
		{
			if (!Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}
			string manifestPath = Path.Combine(folder, ManifestFileName);
			string entryPath = Path.Combine(folder, EntryFileName);
			if (File.Exists(manifestPath))
			{
				File.Delete(manifestPath);
			}
			if (File.Exists(entryPath))
			{
				File.Delete(entryPath);
			}
			File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest), Encoding.UTF8);
			File.WriteAllText(entryPath, JsonUtility.ToJson(entry), Encoding.UTF8);
			detail = folder;
			return true;
		}
		catch (Exception ex)
		{
			detail = "запись не удалась: " + ex.Message;
			return false;
		}
	}

	public static WorkshopManifest ManifestFor(SaveEntry entry, string title, string description, string authorName)
	{
		WorkshopManifest workshopManifest = new WorkshopManifest();
		workshopManifest.formatVersion = CurrentVersion;
		workshopManifest.title = string.IsNullOrEmpty(title) ? (entry?.displayName ?? "") : title;
		workshopManifest.description = description ?? "";
		if (entry != null)
		{
			workshopManifest.family = entry.family;
			workshopManifest.section = entry.section;
			workshopManifest.objectCount = entry.objectCount;
			workshopManifest.payloadVersion = entry.payloadVersion;
		}
		else
		{
			workshopManifest.family = SimFamily.Analytic3D.ToString();
			workshopManifest.section = TargetSection.Hull.ToString();
			workshopManifest.objectCount = 0;
			workshopManifest.payloadVersion = CurrentVersion;
		}
		workshopManifest.appVersion = Application.version;
		workshopManifest.publishedUtc = DateTime.UtcNow.ToString("o");
		workshopManifest.authorName = authorName ?? "";
		workshopManifest.sourceSaveId = entry?.id ?? "";
		return workshopManifest;
	}

	public static List<string> SubFolders(string root)
	{
		List<string> list = new List<string>();
		if (Directory.Exists(root))
		{
			try
			{
				list.AddRange(Directory.GetDirectories(root));
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Мастерская] каталог " + root + " недоступен: " + ex.Message);
			}
		}
		list.Sort(StringComparer.Ordinal);
		return list;
	}
}
