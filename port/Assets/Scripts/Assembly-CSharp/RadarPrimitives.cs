using System;
using System.Collections.Generic;
using Unity.Mathematics;

public static class RadarPrimitives
{
	public static RadarModel Build(RadarReferenceKind kind)
	{
		switch (kind)
		{
		case RadarReferenceKind.Sphere:
			return Sphere(6, 1.0);
		case RadarReferenceKind.Plate:
			return Plate(3, 1.0);
		case RadarReferenceKind.Cylinder:
			return Cylinder(256, 4, 0.5, 2.0);
		case RadarReferenceKind.Dihedral:
			return Dihedral(0.3);
		case RadarReferenceKind.SquareTrihedral:
			return SquareTrihedral(0.2);
		case RadarReferenceKind.TriangularTrihedral:
			return TriangularTrihedral(0.2);
		default:
			throw new ArgumentOutOfRangeException("kind", kind, "Unknown radar reference.");
		}
	}

	public static RadarModel Sphere(int subdivisionLevel = 6, double radiusMeters = 1.0)
	{
		RequirePositive(radiusMeters, "radiusMeters");
		if (subdivisionLevel > 6)
		{
			throw new ArgumentOutOfRangeException("subdivisionLevel");
		}
		double num = (Math.Sqrt(5.0) + 1.0) * 0.5;
		double num2 = Math.Sqrt(num * num + 1.0);
		List<double3> list = new List<double3>((10 << (subdivisionLevel + 1)) + 2);
		double num3 = num / num2;
		double num4 = 1.0 / num2;
		list.Add(new double3(-1.0 / num2, num3, 0.0));
		list.Add(new double3(num4, num3, 0.0));
		list.Add(new double3(-1.0 / num2, -num3, 0.0));
		list.Add(new double3(num4, -num3, 0.0));
		list.Add(new double3(0.0, -num4, num3));
		list.Add(new double3(0.0, num4, num3));
		list.Add(new double3(0.0, -num4, -num3));
		list.Add(new double3(0.0, num4, -num3));
		list.Add(new double3(num3, 0.0, -num4));
		list.Add(new double3(num3, 0.0, num4));
		list.Add(new double3(-num3, 0.0, -num4));
		list.Add(new double3(-num3, 0.0, num4));
		int[] array = new int[60]
		{
			0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
			1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
			3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
			4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
		};
		List<int> list2 = new List<int>(array);
		for (int i = 0; i < subdivisionLevel; i++)
		{
			List<int> list3 = new List<int>(list2.Count * 4);
			Dictionary<ulong, int> cache = new Dictionary<ulong, int>(list2.Count / 2);
			for (int j = 0; j < list2.Count; j += 3)
			{
				int num5 = list2[j];
				int num6 = list2[j + 1];
				int num7 = list2[j + 2];
				int num8 = Midpoint(num5, num6, list, cache);
				int num9 = Midpoint(num6, num7, list, cache);
				int num10 = Midpoint(num7, num5, list, cache);
				Add(list3, num5, num8, num10);
				Add(list3, num6, num9, num8);
				Add(list3, num7, num10, num9);
				Add(list3, num8, num9, num10);
			}
			list2 = list3;
		}
		double3[] array2 = list.ToArray();
		for (int k = 0; k < array2.Length; k++)
		{
			array2[k] = new double3(array2[k].x * radiusMeters, array2[k].y * radiusMeters, array2[k].z * radiusMeters);
		}
		return RadarModel.Build(array2, list2.ToArray(), RadarOrientation.Identity);
	}

	public static RadarModel Plate(int squaresPerSide = 3, double sideMeters = 1.0)
	{
		RequirePositive(sideMeters, "sideMeters");
		if (squaresPerSide < 1 || squaresPerSide > 300)
		{
			throw new ArgumentOutOfRangeException("squaresPerSide");
		}
		int num = squaresPerSide + 1;
		double3[] array = new double3[num * num];
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				array[i * num + j] = new double3(((double)j / (double)squaresPerSide - 0.5) * sideMeters, ((double)i / (double)squaresPerSide - 0.5) * sideMeters, 0.0);
			}
		}
		List<int> list = new List<int>(squaresPerSide * squaresPerSide * 2);
		for (int k = 0; k < squaresPerSide; k++)
		{
			for (int l = 0; l < squaresPerSide; l++)
			{
				int num2 = (k + 1) * num + 1 + l;
				Add(list, num2 - num - 1, num2 - num, num2);
				Add(list, num2 - num - 1, num2, num2 - 1);
			}
		}
		return RadarModel.Build(array, list.ToArray());
	}

	public static RadarModel Cylinder(int radialSegments = 256, int axialBelts = 4, double radiusMeters = 0.5, double lengthMeters = 2.0)
	{
		RequirePositive(radiusMeters, "radiusMeters");
		RequirePositive(lengthMeters, "lengthMeters");
		if (radialSegments < 3 || radialSegments > 4096)
		{
			throw new ArgumentOutOfRangeException("radialSegments");
		}
		if (axialBelts < 1 || axialBelts > 100)
		{
			throw new ArgumentOutOfRangeException("axialBelts");
		}
		List<double3> list = new List<double3>((axialBelts + 1) * radialSegments + 2);
		List<int> list2 = new List<int>(6 * (axialBelts + 1) * radialSegments);
		for (int i = 0; i <= axialBelts; i++)
		{
			double num = ((double)i / (double)axialBelts - 0.5) * lengthMeters;
			for (int j = 0; j < radialSegments; j++)
			{
				double num2 = (double)j * 2.0 * Math.PI / (double)radialSegments;
				list.Add(new double3(radiusMeters * Math.Cos(num2), num, radiusMeters * Math.Sin(num2)));
			}
		}
		for (int k = 0; k < axialBelts; k++)
		{
			for (int l = 0; l < radialSegments; l++)
			{
				int num3 = (k + 1) * radialSegments + l;
				int num4 = (l + 1) % radialSegments + k * radialSegments;
				Add(list2, num3 - radialSegments, num3, num4);
				Add(list2, num3, (k + 1) * radialSegments + (l + 1) % radialSegments, num4);
			}
		}
		int num5 = list.Count;
		list.Add(new double3(0.0, -lengthMeters * 0.5, 0.0));
		int num6 = list.Count;
		list.Add(new double3(0.0, lengthMeters * 0.5, 0.0));
		for (int m = 0; m < radialSegments; m++)
		{
			int num7 = (m + 1) % radialSegments;
			Add(list2, num5, m, num7);
			Add(list2, num6, axialBelts * radialSegments + num7, axialBelts * radialSegments + m);
		}
		return RadarModel.Build(list.ToArray(), list2.ToArray());
	}

	public static RadarModel Dihedral(double sideMeters = 0.3)
	{
		RequirePositive(sideMeters, "sideMeters");
		double num = sideMeters * 0.5;
		double num2 = sideMeters / Math.Sqrt(2.0);
		double3[] array = new double3[6]
		{
			new double3(0.0, -num, 0.0),
			new double3(0.0, num, 0.0),
			new double3(-num2, -num, num2),
			new double3(-num2, num, num2),
			new double3(num2, -num, num2),
			new double3(num2, num, num2)
		};
		int[] array2 = new int[12] { 0, 1, 3, 0, 3, 2, 0, 4, 5, 0, 5, 1 };
		return RadarModel.Build(array, array2);
	}

	public static RadarModel SquareTrihedral(double sideMeters = 0.2)
	{
		RequirePositive(sideMeters, "sideMeters");
		double3[] array = new double3[7]
		{
			new double3(0.0, 0.0, 0.0),
			new double3(sideMeters, 0.0, 0.0),
			new double3(0.0, sideMeters, 0.0),
			new double3(0.0, 0.0, sideMeters),
			new double3(sideMeters, sideMeters, 0.0),
			new double3(sideMeters, 0.0, sideMeters),
			new double3(0.0, sideMeters, sideMeters)
		};
		int[] array2 = new int[18] { 0, 1, 4, 0, 4, 2, 0, 3, 5, 0, 5, 1, 0, 2, 6, 0, 6, 3 };
		return RadarModel.Build(array, array2);
	}

	public static RadarModel TriangularTrihedral(double legMeters = 0.2)
	{
		RequirePositive(legMeters, "legMeters");
		double3[] array = new double3[4]
		{
			new double3(0.0, 0.0, 0.0),
			new double3(legMeters, 0.0, 0.0),
			new double3(0.0, legMeters, 0.0),
			new double3(0.0, 0.0, legMeters)
		};
		int[] array2 = new int[9] { 0, 1, 2, 0, 3, 1, 0, 2, 3 };
		return RadarModel.Build(array, array2);
	}

	private static double3 Unit(double3 v)
	{
		double num = Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
		return new double3(v.x / num, v.y / num, v.z / num);
	}

	private static int Midpoint(int a, int b, List<double3> vertices, Dictionary<ulong, int> cache)
	{
		ulong key = ((ulong)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);
		if (cache.TryGetValue(key, out int value))
		{
			return value;
		}
		double3 @double = vertices[a];
		double3 double2 = vertices[b];
		double3 double3 = new double3((@double.x + double2.x) * 0.5, (@double.y + double2.y) * 0.5, (@double.z + double2.z) * 0.5);
		double num = Math.Sqrt(double3.x * double3.x + double3.y * double3.y + double3.z * double3.z);
		double3 double4 = new double3(double3.x / num, double3.y / num, double3.z / num);
		vertices.Add(double4);
		cache.Add(key, vertices.Count - 1);
		return vertices.Count - 1;
	}

	private static void Add(List<int> triangles, int a, int b, int c)
	{
		triangles.Add(a);
		triangles.Add(b);
		triangles.Add(c);
	}

	private static void RequirePositive(double value, string name)
	{
		if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
		{
			throw new ArgumentOutOfRangeException(name);
		}
	}
}
