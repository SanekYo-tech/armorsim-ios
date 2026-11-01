using System;
using System.Collections.Generic;
using UnityEngine;

public static class AeroPrimitives
{
	public static AeroMeshData Plate(float width = 2f, float height = 2f, float z = 0f)
	{
		if (float.IsNaN(width) || float.IsInfinity(width) || float.IsNaN(height) || float.IsInfinity(height) || float.IsNaN(z) || float.IsInfinity(z) || width <= 0f || height <= 0f)
		{
			throw new AeroException("aero.error.calculation", Array.Empty<object>());
		}
		float num = width * 0.5f;
		float num2 = height * 0.5f;
		Vector3[] vertices = new Vector3[4]
		{
			new Vector3(-num, -num2, z),
			new Vector3(-num, num2, z),
			new Vector3(num, num2, z),
			new Vector3(num, -num2, z)
		};
		int[] triangles = new int[6] { 0, 1, 2, 0, 2, 3 };
		return new AeroMeshData(vertices, triangles);
	}

	public static AeroMeshData Sphere(int latitude = 48, int longitude = 96, float radius = 1f)
	{
		if (latitude < 3 || longitude < 3 || 2L * (latitude - 1) * longitude > 300000 || float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f)
		{
			throw new AeroException("aero.error.calculation", Array.Empty<object>());
		}
		List<Vector3> verts = new List<Vector3>((latitude - 1) * longitude + 2);
		List<int> tris = new List<int>((6 * latitude - 6) * longitude);
		verts.Add(Vector3.up * radius);
		for (int i = 1; i < latitude; i++)
		{
			double num = i * Math.PI / latitude;
			float num2 = (float)(Math.Cos(num) * radius);
			double num3 = Math.Sin(num) * radius;
			for (int j = 0; j < longitude; j++)
			{
				double num4 = 2.0 * Math.PI * j / longitude;
				verts.Add(new Vector3((float)(Math.Cos(num4) * num3), num2, (float)(Math.Sin(num4) * num3)));
			}
		}
		int num5 = verts.Count;
		verts.Add(Vector3.down * radius);
		for (int k = 0; k < longitude; k++)
		{
			int num6 = (k + 1) % longitude;
			Add(tris, 0, num6 + 1, k + 1);
			for (int l = 0; l < latitude - 2; l++)
			{
				int num7 = l * longitude + k + 1;
				int num8 = l * longitude + num6 + 1;
				Add(tris, num7, num8, num7 + longitude);
				Add(tris, num8, num8 + longitude, num7 + longitude);
			}
			int num9 = (latitude - 2) * longitude + 1;
			Add(tris, num5, num9 + k, num9 + num6);
		}
		return new AeroMeshData(verts.ToArray(), tris.ToArray());
	}

	private static void Add(List<int> triangles, int a, int b, int c)
	{
		triangles.Add(a);
		triangles.Add(b);
		triangles.Add(c);
	}
}
