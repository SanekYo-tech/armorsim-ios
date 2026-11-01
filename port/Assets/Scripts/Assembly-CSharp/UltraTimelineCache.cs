using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class UltraTimelineCache : IUltraTimelineSink
{
	[Serializable]
	public sealed class Entry
	{
		public string key;

		public string file;

		public long bytes;

		public long lastUsedTicks;
	}

	public const string FolderName = "ultra_cache";

	public const string ManifestName = "manifest.txt";

	public const long DefaultCapBytes = 536870912L;

	private static UltraTimelineCache shared;

	public long capBytes;

	private readonly Dictionary<string, Entry> entries;

	private string root;

	public static UltraTimelineCache Shared
	{
		get
		{
			if (shared == null)
			{
				shared = new UltraTimelineCache();
			}
			return shared;
		}
	}

	public string Root
	{
		get
		{
			if (root == null)
			{
				root = Path.Combine(Application.persistentDataPath, "ultra_cache");
				Directory.CreateDirectory(root);
				LoadManifest();
			}
			return root;
		}
	}

	public long TotalBytes
	{
		get
		{
			long num = 0L;
			foreach (Entry value in entries.Values)
			{
				num += value.bytes;
			}
			return num;
		}
	}

	public int Count => entries.Count;

	public UltraTimelineCache()
	{
		capBytes = 536870912L;
		entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
	}

	public void UseRoot(string path)
	{
		root = path;
		Directory.CreateDirectory(root);
		entries.Clear();
		LoadManifest();
	}

	public bool Has(string key)
	{
		return !string.IsNullOrEmpty(key) && entries.ContainsKey(key);
	}

	public bool TryLoad(string key, out GeometryTimeline tl)
	{
		tl = null;
		if (string.IsNullOrEmpty(key) || !entries.TryGetValue(key, out Entry entry))
		{
			return false;
		}
		string path = Path.Combine(Root, entry.file);
		if (!File.Exists(path))
		{
			entries.Remove(key);
			SaveManifest();
			return false;
		}
		string text = null;
		try
		{
			using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
			using (BinaryReader binaryReader = new BinaryReader(stream))
			{
				string text2 = binaryReader.ReadString();
				if (text2 != "R/r0=1.279217/1128/489/S19-single-floor")
				{
					Debug.Log("ULTRA кэш: запись " + key + " записана отпечатком «" + text2 + "», текущий «R/r0=1.279217/1128/489/S19-single-floor» — ВЫБРОШЕНА (физика с тех пор менялась)");
					Discard(key);
					return false;
				}
				tl = GeometryTimelineFile.Load(stream, out text);
			}
		}
		catch (Exception ex)
		{
			text = ex.Message;
			tl = null;
		}
		if (tl != null)
		{
			entry.lastUsedTicks = DateTime.UtcNow.Ticks;
			SaveManifest();
			return true;
		}
		Debug.Log("ULTRA кэш: " + key + " не читается (" + text + ") — выброшена");
		Discard(key);
		return false;
	}

	public void Accept(string key, GeometryTimeline tl)
	{
		if (string.IsNullOrEmpty(key) || tl == null)
		{
			return;
		}
		if (!tl.Completed || tl.Cancelled)
		{
			Debug.Log("ULTRA кэш: " + key + " НЕ сохранён — решение не завершено (отмена)");
			return;
		}
		try
		{
			string text = UltraCacheKey.FileName(key);
			string path = Path.Combine(Root, text);
			WriteStamped(tl, path);
			Entry value = new Entry
			{
				key = key,
				file = text,
				bytes = new FileInfo(path).Length,
				lastUsedTicks = DateTime.UtcNow.Ticks
			};
			entries[key] = value;
			Evict();
			SaveManifest();
		}
		catch (Exception ex)
		{
			Debug.LogError("ULTRA кэш: запись не удалась — " + ex.Message);
		}
	}

	private static void WriteStamped(GeometryTimeline tl, string path)
	{
		string text = path + ".body";
		GeometryTimelineFile.Save(tl, text);
		using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
		using (BinaryWriter binaryWriter = new BinaryWriter(stream))
		{
			binaryWriter.Write("R/r0=1.279217/1128/489/S19-single-floor");
			using (FileStream stream2 = new FileStream(text, FileMode.Open, FileAccess.Read))
			{
				stream2.CopyTo(stream);
			}
		}
		if (File.Exists(text))
		{
			File.Delete(text);
		}
	}

	public void Discard(string key)
	{
		if (entries.TryGetValue(key, out Entry entry))
		{
			string path = Path.Combine(Root, entry.file);
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			entries.Remove(key);
			SaveManifest();
		}
	}

	public void Clear()
	{
		foreach (Entry item in new List<Entry>(entries.Values))
		{
			try
			{
				File.Delete(Path.Combine(Root, item.file));
			}
			catch
			{
			}
		}
		entries.Clear();
		SaveManifest();
	}

	private void Evict()
	{
		if (capBytes <= 0L)
		{
			return;
		}
		List<Entry> list = new List<Entry>(entries.Values);
		list.Sort((Entry a, Entry b) => a.lastUsedTicks.CompareTo(b.lastUsedTicks));
		long num = TotalBytes;
		int num2 = 0;
		while (num > capBytes && num2 < list.Count)
		{
			Entry entry = list[num2++];
			try
			{
				File.Delete(Path.Combine(Root, entry.file));
			}
			catch
			{
			}
			entries.Remove(entry.key);
			num -= entry.bytes;
			Debug.Log(string.Format("ULTRA кэш: вытеснена запись {0} ({1} КБ) — потолок ", entry.key, entry.bytes / 1024) + string.Format("{0} МБ", capBytes / 1048576));
		}
	}

	private void LoadManifest()
	{
		entries.Clear();
		string path = Path.Combine(root, "manifest.txt");
		if (!File.Exists(path))
		{
			return;
		}
		string[] array = File.ReadAllLines(path);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split('\t');
			if (array2.Length == 4 && long.TryParse(array2[2], out long result) && long.TryParse(array2[3], out long result2))
			{
				entries[array2[0]] = new Entry
				{
					key = array2[0],
					file = array2[1],
					bytes = result,
					lastUsedTicks = result2
				};
			}
		}
	}

	private void SaveManifest()
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (Entry value in entries.Values)
			{
				stringBuilder.Append(value.key).Append('\t').Append(value.file).Append('\t').Append(value.bytes).Append('\t').Append(value.lastUsedTicks).Append('\n');
			}
			File.WriteAllText(Path.Combine(root, "manifest.txt"), stringBuilder.ToString());
		}
		catch (Exception ex)
		{
			Debug.LogError("ULTRA кэш: манифест не записан — " + ex.Message);
		}
	}
}
