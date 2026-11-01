using System;
using UnityEngine;

public static class AeroDisplay
{
	public static Color PressureColor(float cp)
	{
		float x = Mathf.Clamp01(cp * 0.5f);
		if (x < 0.35f)
		{
			float t = Mathf.Clamp01(x / 0.35f);
			return new Color(0.05f + t * (0.05f - 0.05f), 0.22f + t * (0.65f - 0.22f), 0.52f + t * (0.69f - 0.52f), 1f + t * (1f - 1f));
		}
		if (x < 0.7f)
		{
			float t = Mathf.Clamp01((x - 0.35f) / 0.35f);
			return new Color(0.05f + t * (0.9f - 0.05f), 0.65f + t * (0.67f - 0.65f), 0.69f + t * (0.1f - 0.69f), 1f + t * (1f - 1f));
		}
		float t2 = Mathf.Clamp01((x - 0.7f) / 0.3f);
		return new Color(0.9f + t2 * (0.7f - 0.9f), 0.67f + t2 * (0.08f - 0.67f), 0.1f + t2 * (0.055f - 0.1f), 1f + t2 * (1f - 1f));
	}

	public static Material Material()
	{
		Material source = Resources.Load<Material>("AeroPressure");
		if (source == null)
		{
			throw new AeroException("aero.error.shader", Array.Empty<object>());
		}
		return new Material(source);
	}

	public static Mesh Mesh(AeroMeshData data)
	{
		int n = data.Triangles.Length;
		Vector3[] vertices = new Vector3[n];
		Vector3[] normals = new Vector3[n];
		int[] indices = new int[n];
		Color32[] colors = new Color32[n];
		for (int i = 0; i < n; i++)
		{
			vertices[i] = data.Vertices[data.Triangles[i]];
			normals[i] = data.Normals[i / 3];
			indices[i] = i;
			Color c = PressureColor(0f);
			colors[i] = new Color32((byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f));
		}
		Mesh mesh = new Mesh();
		mesh.name = "Aero pressure surface";
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.colors32 = colors;
		mesh.triangles = indices;
		mesh.RecalculateBounds();
		return mesh;
	}

	public static void Paint(Mesh mesh, float[] cp)
	{
		Color32[] colors = new Color32[cp.Length * 3];
		for (int i = 0; i < cp.Length; i++)
		{
			Color c = PressureColor(cp[i]);
			Color32 k = new Color32((byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f));
			colors[i * 3] = k;
			colors[i * 3 + 1] = k;
			colors[i * 3 + 2] = k;
		}
		mesh.colors32 = colors;
	}
}
