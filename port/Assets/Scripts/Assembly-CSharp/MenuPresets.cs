using System;
using System.Collections.Generic;
using UnityEngine;

public static class MenuPresets
{
	private static List<SaveEntry> cache;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Install()
	{
		SaveSystem.ExtraPresets = Build;
	}

	public static List<SaveEntry> Build()
	{
		if (cache != null)
		{
			return cache;
		}
		List<SaveEntry> list = new List<SaveEntry>();
		foreach (SceneFile item in ScenePresets.All())
		{
			if (item == null)
			{
				continue;
			}
			try
			{
				item.formatVersion = SceneSave.CurrentVersion;
				string payload = SceneSave.ToJson(item);
				SaveEntry saveEntry = new SaveEntry();
				saveEntry.id = "preset:" + item.name;
				saveEntry.displayName = item.name;
				saveEntry.createdUtc = "";
				saveEntry.objectCount = ((item.objects != null) ? item.objects.Count : 0);
				saveEntry.payloadVersion = 1;
				saveEntry.payload = payload;
				saveEntry.section = SceneSave.SectionOf(item).ToString();
				saveEntry.isPreset = true;
				list.Add(saveEntry);
			}
			catch (Exception e)
			{
				Debug.LogError("[MenuPresets] Пресет «" + item.name + "» не сериализуется: " + e.Message);
			}
		}
		cache = list;
		Debug.Log(string.Format("[MenuPresets] Пресетов песочницы отдано меню: {0}.", list.Count));
		return cache;
	}
}
