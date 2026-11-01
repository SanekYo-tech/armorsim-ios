using System;
using UnityEngine;

public sealed class AeroMeshData
{
	public readonly Vector3[] Vertices;

	public readonly int[] Triangles;

	public readonly Vector3[] Normals;

	public Vector3 OriginalSize;

	public int DegenerateTriangles;

	public int TriangleCount => Triangles.Length / 3;

	public AeroMeshData(Vector3[] vertices, int[] triangles, Vector3[] normals = null)
	{
		if (vertices == null || triangles == null || triangles.Length % 3 != 0)
		{
			throw new AeroException("aero.error.format", new object[1] { "triangle array" });
		}
		if (normals != null && normals.Length != triangles.Length / 3)
		{
			throw new AeroException("aero.error.format", new object[1] { "normal array" });
		}
		Vertices = (Vector3[])vertices.Clone();
		Triangles = (int[])triangles.Clone();
		Normals = new Vector3[Triangles.Length / 3];
		for (int i = 0; i < Vertices.Length; i++)
		{
			RequireFinite(Vertices[i]);
		}
		for (int j = 0; j < Triangles.Length / 3; j++)
		{
			int num = Triangles[j * 3];
			int num2 = Triangles[j * 3 + 1];
			int num3 = Triangles[j * 3 + 2];
			if (num < 0 || num2 < 0 || num3 < 0 || num >= Vertices.Length || num2 >= Vertices.Length || num3 >= Vertices.Length)
			{
				throw new AeroException("aero.error.indices", Array.Empty<object>());
			}
			Normals[j] = ((normals == null) ? GeometricNormal(Vertices[num], Vertices[num2], Vertices[num3]) : Unit(normals[j]));
		}
	}

	public AeroMeshData Rotated(Quaternion rotation)
	{
		RequireFinite(new Vector3(rotation.x, rotation.y, rotation.z));
		if (!Finite(rotation.w))
		{
			throw new AeroException("aero.error.finite", Array.Empty<object>());
		}
		double num = Math.Sqrt((double)rotation.x * rotation.x + (double)rotation.y * rotation.y + (double)rotation.z * rotation.z + (double)rotation.w * rotation.w);
		if (num <= 0.0)
		{
			throw new AeroException("aero.error.format", new object[1] { "zero rotation" });
		}
		rotation = new Quaternion((float)((double)rotation.x / num), (float)((double)rotation.y / num), (float)((double)rotation.z / num), (float)((double)rotation.w / num));
		Vector3[] array = new Vector3[Vertices.Length];
		Vector3[] array2 = new Vector3[Normals.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = rotation * Vertices[i];
		}
		for (int j = 0; j < array2.Length; j++)
		{
			array2[j] = rotation * Normals[j];
		}
		AeroMeshData result = new AeroMeshData(array, Triangles, array2);
		result.OriginalSize = OriginalSize;
		result.DegenerateTriangles = DegenerateTriangles;
		return result;
	}

	public AeroMeshData Flipped()
	{
		int[] array = (int[])Triangles.Clone();
		Vector3[] array2 = new Vector3[Normals.Length];
		for (int i = 0; i < Triangles.Length / 3; i++)
		{
			int num = array[i * 3 + 1];
			array[i * 3 + 1] = array[i * 3 + 2];
			array[i * 3 + 2] = num;
			array2[i] = new Vector3(-Normals[i].x, -Normals[i].y, -Normals[i].z);
		}
		AeroMeshData result = new AeroMeshData(Vertices, array, array2);
		result.OriginalSize = OriginalSize;
		result.DegenerateTriangles = DegenerateTriangles;
		return result;
	}

	internal static bool Finite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}

	internal static void RequireFinite(Vector3 value)
	{
		if (!Finite(value.x) || !Finite(value.y) || !Finite(value.z))
		{
			throw new AeroException("aero.error.finite", Array.Empty<object>());
		}
	}

	internal static Vector3 GeometricNormal(Vector3 a, Vector3 b, Vector3 c)
	{
		double num = (double)b.x - a.x;
		double num2 = (double)b.y - a.y;
		double num3 = (double)b.z - a.z;
		double num4 = (double)c.x - a.x;
		double num5 = (double)c.y - a.y;
		double num6 = (double)c.z - a.z;
		return Unit(num2 * num6 - num3 * num5, num3 * num4 - num * num6, num * num5 - num2 * num4);
	}

	internal static Vector3 Unit(Vector3 value)
	{
		RequireFinite(value);
		return Unit(value.x, value.y, value.z);
	}

	internal static Vector3 Unit(double x, double y, double z)
	{
		double num = Math.Sqrt(x * x + y * y + z * z);
		if (num == 0.0)
		{
			return Vector3.zero;
		}
		if (double.IsNaN(num) || double.IsInfinity(num))
		{
			throw new AeroException("aero.error.finite", Array.Empty<object>());
		}
		return new Vector3((float)(x / num), (float)(y / num), (float)(z / num));
	}
}
