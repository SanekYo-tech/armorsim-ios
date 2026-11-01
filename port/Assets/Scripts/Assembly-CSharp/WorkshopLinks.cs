using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class WorkshopLinks
{
	public const string FileName = "workshop_links.json";

	private static WorkshopLinkFile _cache;

	public static string Path => System.IO.Path.Combine(SaveSystem.SavesDir, FileName);

	private static WorkshopLinkFile Load()
	{
		if (_cache != null)
		{
			return _cache;
		}
		WorkshopLinkFile workshopLinkFile = new WorkshopLinkFile();
		workshopLinkFile.links = new List<WorkshopLink>();
		_cache = workshopLinkFile;
		try
		{
			if (File.Exists(Path))
			{
				WorkshopLinkFile workshopLinkFile2 = JsonUtility.FromJson<WorkshopLinkFile>(File.ReadAllText(Path, Encoding.UTF8));
				if (workshopLinkFile2 != null && workshopLinkFile2.links != null)
				{
					_cache = workshopLinkFile2;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Мастерская] таблица связей не прочиталась (" + ex.Message + "); повторная выгрузка создаст новый предмет вместо обновления.");
		}
		return _cache;
	}

	public static string ItemFor(string saveId)
	{
		if (!string.IsNullOrEmpty(saveId))
		{
			foreach (WorkshopLink link in Load().links)
			{
				if (link != null && link.saveId == saveId)
				{
					return link.publishedFileId ?? "";
				}
			}
		}
		return "";
	}

	public static void Remember(string saveId, string publishedFileId)
	{
		if (string.IsNullOrEmpty(saveId) || string.IsNullOrEmpty(publishedFileId))
		{
			return;
		}
		try
		{
			WorkshopLinkFile file = Load();
			WorkshopLink link = null;
			foreach (WorkshopLink item in file.links)
			{
				if (item != null && item.saveId == saveId)
				{
					link = item;
					break;
				}
			}
			if (link == null)
			{
				link = new WorkshopLink();
				link.saveId = saveId;
				file.links.Add(link);
			}
			link.publishedFileId = publishedFileId;
			link.publishedUtc = DateTime.UtcNow.ToString("o");
			if (!Directory.Exists(SaveSystem.SavesDir))
			{
				Directory.CreateDirectory(SaveSystem.SavesDir);
			}
			File.WriteAllText(Path, JsonUtility.ToJson(file, true), Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Мастерская] таблица связей не записалась (" + ex.Message + "); следующая выгрузка создаст новый предмет вместо обновления.");
		}
	}

	public static void ForgetCache()
	{
		_cache = null;
	}
}
