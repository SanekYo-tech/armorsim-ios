using System;
using System.Collections.Generic;
using UnityEngine;

public static class MeshNormals
{
	public static void RecalculateSmooth(Mesh mesh, float smoothAngleDeg = 55f, bool weld = true)
	{
		Vector3[] vertices = mesh.vertices;
		int[] triangles = mesh.triangles;
		Vector3[] array = new Vector3[vertices.Length];
		for (int i = 0; i < triangles.Length; i += 3)
		{
			int num = triangles[i];
			int num2 = triangles[i + 1];
			int num3 = triangles[i + 2];
			Vector3 vector = Vector3.Cross(vertices[num2] - vertices[num], vertices[num3] - vertices[num]);
			if (vector.sqrMagnitude < 1E-16f)
			{
				continue;
			}
			vector = vector.normalized;
			array[num] += vector * Angle(vertices[num], vertices[num2], vertices[num3]);
			array[num2] += vector * Angle(vertices[num2], vertices[num], vertices[num3]);
			array[num3] += vector * Angle(vertices[num3], vertices[num], vertices[num2]);
		}
		if (weld)
		{
			Vector3 vector2 = mesh.bounds.size;
			float num4 = Mathf.Max(vector2.x, vector2.y, vector2.z);
			WeldNormals(vertices, array, num4 * 1E-04f + 1E-06f);
		}
		for (int j = 0; j < array.Length; j++)
		{
			if (array[j].sqrMagnitude <= 1E-12f)
			{
				array[j] = Vector3.up;
			}
			else
			{
				array[j] = array[j].normalized;
			}
		}
		mesh.normals = array;
	}

	private static float Angle(Vector3 p, Vector3 q, Vector3 r)
	{
		Vector3 vector = q - p;
		Vector3 vector2 = r - p;
		float num = vector.magnitude * vector2.magnitude;
		if (1E-12f > num)
		{
			return 0f;
		}
		float num2 = Vector3.Dot(vector, vector2) / num;
		return MathF.Acos(Mathf.Clamp(num2, -1f, 1f));
	}

	private static void WeldNormals(Vector3[] verts, Vector3[] normals, float eps)
	{
		int num = verts.Length;
		if (num <= 4000)
		{
			Vector3[] array = (Vector3[])normals.Clone();
			for (int i = 0; i < num; i++)
			{
				for (int j = i + 1; j < num; j++)
				{
					if ((verts[i] - verts[j]).sqrMagnitude < eps * eps)
					{
						array[i] += normals[j];
						array[j] += normals[i];
					}
				}
			}
			Array.Copy(array, normals, num);
		}
		else
		{
			WeldHashed(verts, normals, eps);
		}
	}

	private static void WeldHashed(Vector3[] verts, Vector3[] normals, float eps)
	{
		if (eps <= 1E-06f)
		{
			eps = 1E-06f;
		}
		float num = 1f / eps;
		Dictionary<(int, int, int), Vector3> dictionary = new Dictionary<(int, int, int), Vector3>();
		(int, int, int) Key(Vector3 p)
		{
			return (Mathf.RoundToInt(p.x * num), Mathf.RoundToInt(p.y * num), Mathf.RoundToInt(p.z * num));
		}
		for (int i = 0; i < verts.Length; i++)
		{
			(int, int, int) key = Key(verts[i]);
			if (!dictionary.TryGetValue(key, out Vector3 value))
			{
				value = Vector3.zero;
			}
			dictionary[key] = value + normals[i];
		}
		for (int j = 0; j < verts.Length; j++)
		{
			normals[j] = dictionary[Key(verts[j])];
		}
	}
}
