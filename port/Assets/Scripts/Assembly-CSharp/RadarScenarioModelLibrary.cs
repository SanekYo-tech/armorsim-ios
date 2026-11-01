using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public sealed class RadarScenarioModelLibrary : IDisposable
{
	public sealed class Entry
	{
		public string Key;

		public string Name;

		public Texture2D Texture;

		public int Triangles;

		public RadarScenarioShapeProfile ShapeProfile;

		public string RegistryKey;
	}

	private sealed class Work
	{
		public string Key;

		public string Name;

		public Task<Thumbnail> Task;
	}

	private sealed class Thumbnail
	{
		public Color32[] Pixels;

		public int Triangles;

		public RadarScenarioShapeProfile Shape;
	}

	public Func<string, string> RegistryKeyOfPath;

	private const int Resolution = 192;

	private readonly List<Entry> entries;

	private readonly List<Work> work;

	public Action<Entry> Completed;

	public Action<string, string> Failed;

	public IReadOnlyList<Entry> Entries => entries;

	public bool Busy => work.Count > 0;

	public RadarScenarioModelLibrary()
	{
		RegistryKeyOfPath = (string path) => RadarMapModelRegistry.Default.LatestKeyForPath(path);
		entries = new List<Entry>();
		work = new List<Work>();
		byte[] bytes = RadarBuiltinModels.ReadYf23Resource();
		Work item = new Work
		{
			Key = "yf23",
			Name = "YF-23",
			Task = Task.Run(() => Rasterize(RadarBuiltinModels.ParseYf23(bytes)))
		};
		work.Add(item);
	}

	public Entry FindByRegistryKey(string registryKey)
	{
		return entries.Find((Entry e) => e.RegistryKey == registryKey);
	}

	public Entry Find(string key)
	{
		return entries.Find((Entry e) => e.Key == key);
	}

	public Texture GetTexture(string key)
	{
		Entry entry = Find(key);
		if (entry == null)
		{
			return null;
		}
		return entry.Texture;
	}

	public void Import(string file)
	{
		string text = Path.GetFullPath(file);
		string text2 = Path.GetExtension(text).ToLowerInvariant();
		if (text2 != ".stl" && text2 != ".obj")
		{
			throw new ArgumentException(RadarScenarioText.Get("shell.fileType"));
		}
		if (Find(text) != null)
		{
			Completed?.Invoke(Find(text));
		}
		else if (!work.Exists((Work w) => w.Key == text))
		{
			if (entries.Count + work.Count >= 16)
			{
				throw new ArgumentException(RadarScenarioText.Get("shell.modelLimit"));
			}
			Work item = new Work
			{
				Key = text,
				Name = Path.GetFileNameWithoutExtension(text),
				Task = Task.Run(() => Rasterize(RadarLargeStlAdapter.Load(text)))
			};
			work.Add(item);
		}
	}

	public void Poll()
	{
		for (int num = work.Count - 1; num >= 0; num--)
		{
			Work item = work[num];
			if (!item.Task.IsCompleted)
			{
				continue;
			}
			work.RemoveAt(num);
			try
			{
				Thumbnail thumbnail = item.Task.Result;
				Texture2D texture2D = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
				texture2D.name = "Scenario silhouette " + item.Name;
				texture2D.filterMode = FilterMode.Bilinear;
				texture2D.SetPixels32(thumbnail.Pixels);
				texture2D.Apply(false, true);
				Entry entry = new Entry
				{
					Key = item.Key,
					Name = item.Name,
					Texture = texture2D,
					Triangles = thumbnail.Triangles,
					ShapeProfile = thumbnail.Shape,
					RegistryKey = ((item.Key != "yf23") ? RegistryKeyOfPath?.Invoke(item.Key) : "yf23")
				};
				entries.Add(entry);
				Completed?.Invoke(entry);
			}
			catch (Exception ex)
			{
				Failed?.Invoke(item.Key, item.Name + ": " + ex.Message);
			}
		}
	}

	private static Thumbnail Rasterize(AeroMeshData mesh)
	{
		Thumbnail thumbnail = new Thumbnail();
		thumbnail.Pixels = new Color32[Resolution * Resolution];
		thumbnail.Triangles = mesh.Triangles.Length / 3;
		thumbnail.Shape = RadarScenarioShapeProfile.FromMesh(mesh);
		Vector3 vector = Vector3.one * float.MaxValue;
		Vector3 vector2 = -(Vector3.one * float.MaxValue);
		for (int i = 0; i < mesh.Vertices.Length; i++)
		{
			Vector3 vertex = mesh.Vertices[i];
			vector = Vector3.Min(vector, vertex);
			vector2 = Vector3.Max(vector2, vertex);
		}
		Vector3 vector3 = vector2 - vector;
		int num;
		int num2;
		if (vector3.z >= vector3.x && vector3.z >= vector3.y)
		{
			num = 2;
			num2 = ((vector3.x >= vector3.y) ? 0 : 1);
		}
		else if (vector3.y >= vector3.x)
		{
			num = 1;
			num2 = ((vector3.x >= vector3.z) ? 0 : 2);
		}
		else
		{
			num = 0;
			num2 = ((vector3.y >= vector3.z) ? 1 : 2);
		}
		float num3 = float.MaxValue;
		float num4 = float.MaxValue;
		float num5 = float.MinValue;
		float num6 = float.MinValue;
		for (int j = 0; j < mesh.Vertices.Length; j++)
		{
			Vector3 vertex2 = mesh.Vertices[j];
			num3 = Mathf.Min(num3, vertex2[num2]);
			num4 = Mathf.Min(num4, vertex2[num]);
			num5 = Mathf.Max(num5, vertex2[num2]);
			num6 = Mathf.Max(num6, vertex2[num]);
		}
		float num9 = 180f / Mathf.Min(num5 - num3, num6 - num4, float.Epsilon);
		float num10 = (num3 + num5) * 0.5f;
		float num11 = (num4 + num6) * 0.5f;
		Vector2[] array = new Vector2[mesh.Vertices.Length];
		for (int k = 0; k < mesh.Vertices.Length; k++)
		{
			Vector3 vertex3 = mesh.Vertices[k];
			array[k] = Vector2.one * 96f + new Vector2((vertex3[num2] - num10) * num9, (vertex3[num] - num11) * num9);
		}
		for (int l = 0; l < mesh.Triangles.Length; l += 3)
		{
			Vector2 a = array[mesh.Triangles[l]];
			Vector2 b = array[mesh.Triangles[l + 1]];
			Vector2 c = array[mesh.Triangles[l + 2]];
			float num12 = Edge(a, b, c);
			if (MathF.Abs(num12) < 1E-05f)
			{
				continue;
			}
			int num13 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x)), 0, 191);
			int num14 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x, c.x)), 0, 191);
			int num15 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y)), 0, 191);
			int num16 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y)), 0, 191);
			for (int m = num15; m <= num16; m++)
			{
				for (int n = num13; n <= num14; n++)
				{
					Vector2 vector4 = new Vector2(n + 0.5f, m + 0.5f);
					if (Edge(a, b, vector4) / num12 >= 0f && Edge(b, c, vector4) / num12 >= 0f && Edge(c, a, vector4) / num12 >= 0f)
					{
						thumbnail.Pixels[m * 192 + n] = new Color32(225, 236, 239, byte.MaxValue);
					}
				}
			}
		}
		return thumbnail;
	}

	private static float Edge(Vector2 a, Vector2 b, Vector2 p)
	{
		return (p.y - a.y) * (b.x - a.x) - (b.y - a.y) * (p.x - a.x);
	}

	public void Dispose()
	{
		foreach (Entry entry in entries)
		{
			if (entry.Texture != null)
			{
				UnityEngine.Object.Destroy(entry.Texture);
			}
		}
		entries.Clear();
		foreach (Work item in work)
		{
			item.Task.ContinueWith(delegate(Task<Thumbnail> t)
			{
				_ = t.Exception;
			}, TaskContinuationOptions.OnlyOnFaulted);
		}
		work.Clear();
		Completed = null;
		Failed = null;
	}
}
