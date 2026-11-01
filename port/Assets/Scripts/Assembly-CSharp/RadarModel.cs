using System;
using System.Collections.Generic;
using Unity.Mathematics;

public sealed class RadarModel
{
	public readonly double3[] Vertices;

	public readonly int[] Triangles;

	public readonly double3[] Normals;

	public readonly double[] Areas;

	public readonly double3 BoundsMin;

	public readonly double3 BoundsMax;

	public readonly double3 Center;

	public readonly double3 Size;

	public readonly double BoundingSphereRadius;

	public readonly int DegenerateTriangles;

	public int TriangleCount => Triangles.Length / 3;

	public double LongestSide => Math.Max(Size.x, Math.Max(Size.y, Size.z));

	private RadarModel(double3[] vertices, int[] triangles, double3[] normals, double[] areas, double3 boundsMin, double3 boundsMax, double3 center, double radius, int discarded)
	{
		Vertices = vertices;
		Triangles = triangles;
		Normals = normals;
		Areas = areas;
		BoundsMin = boundsMin;
		BoundsMax = boundsMax;
		Center = center;
		Size = boundsMax - boundsMin;
		BoundingSphereRadius = radius;
		DegenerateTriangles = discarded;
	}

	public static RadarModel Build(double3[] verticesMeters, int[] triangleIndices)
	{
		return Build(verticesMeters, triangleIndices, RadarOrientation.Identity);
	}

	public static RadarModel Build(double3[] verticesMeters, int[] triangleIndices, RadarOrientation orientation)
	{
		if (verticesMeters == null || triangleIndices == null || triangleIndices.Length == 0 || triangleIndices.Length % 3 != 0)
		{
			throw new ArgumentException("RadarModel needs triangle geometry.");
		}
		if (orientation == default(RadarOrientation))
		{
			throw new ArgumentException("Radar orientation must be an exact quarter-turn matrix.", "orientation");
		}
		for (int i = 0; i < verticesMeters.Length; i++)
		{
			if (!Finite(verticesMeters[i]))
			{
				throw new ArgumentException("Non-finite radar vertex.", "verticesMeters");
			}
		}
		List<int> used = new List<int>(triangleIndices.Length);
		int discarded = 0;
		for (int j = 0; j < triangleIndices.Length; j += 3)
		{
			int num = triangleIndices[j];
			int num2 = triangleIndices[j + 1];
			int num3 = triangleIndices[j + 2];
			if (num < 0 || num2 < 0 || num3 < 0 || num >= verticesMeters.Length || num2 >= verticesMeters.Length || num3 >= verticesMeters.Length)
			{
				throw new ArgumentException("Radar triangle index out of bounds.", "triangleIndices");
			}
			double3 e = verticesMeters[num2] - verticesMeters[num];
			double3 e2 = verticesMeters[num3] - verticesMeters[num];
			double3 normal = new double3(e.y * e2.z - e.z * e2.y, e.z * e2.x - e.x * e2.z, e.x * e2.y - e.y * e2.x);
			double num4 = math.length(normal);
			if (!Finite(num4))
			{
				throw new ArgumentException("Non-finite radar face area.", "verticesMeters");
			}
			if (num4 == 0.0)
			{
				discarded++;
				continue;
			}
			used.Add(num);
			used.Add(num2);
			used.Add(num3);
		}
		if (used.Count == 0)
		{
			throw new ArgumentException("Radar mesh has no non-degenerate triangles.", "triangleIndices");
		}
		double3 min = verticesMeters[used[0]];
		double3 max = min;
		for (int k = 1; k < used.Count; k++)
		{
			double3 v = verticesMeters[used[k]];
			if (!double.IsNaN(v.x) && v.x < min.x)
			{
				min.x = v.x;
			}
			if (!double.IsNaN(v.y) && v.y < min.y)
			{
				min.y = v.y;
			}
			if (!double.IsNaN(v.z) && v.z < min.z)
			{
				min.z = v.z;
			}
			if (!double.IsNaN(v.x) && v.x > max.x)
			{
				max.x = v.x;
			}
			if (!double.IsNaN(v.y) && v.y > max.y)
			{
				max.y = v.y;
			}
			if (!double.IsNaN(v.z) && v.z > max.z)
			{
				max.z = v.z;
			}
		}
		double3 center = new double3((max.x - min.x) * 0.5 + min.x, (max.y - min.y) * 0.5 + min.y, (max.z - min.z) * 0.5 + min.z);
		Dictionary<int, int> remap = new Dictionary<int, int>(verticesMeters.Length);
		List<double3> vertices = new List<double3>(Math.Min(verticesMeters.Length, used.Count));
		int[] triangles = new int[used.Count];
		for (int l = 0; l < used.Count; l++)
		{
			int num5 = used[l];
			int num6;
			if (!remap.TryGetValue(num5, out num6))
			{
				double3 p = orientation.Apply(verticesMeters[num5] - center) + center;
				if (!Finite(p))
				{
					throw new ArgumentException("Non-finite oriented radar vertex.", "verticesMeters");
				}
				num6 = vertices.Count;
				vertices.Add(p);
				remap.Add(num5, num6);
			}
			triangles[l] = num6;
		}
		double3[] array = vertices.ToArray();
		double3 boundsMin = array[0];
		double3 boundsMax = boundsMin;
		for (int m = 1; m < array.Length; m++)
		{
			double3 v2 = array[m];
			if (!double.IsNaN(v2.x) && v2.x < boundsMin.x)
			{
				boundsMin.x = v2.x;
			}
			if (!double.IsNaN(v2.y) && v2.y < boundsMin.y)
			{
				boundsMin.y = v2.y;
			}
			if (!double.IsNaN(v2.z) && v2.z < boundsMin.z)
			{
				boundsMin.z = v2.z;
			}
			if (!double.IsNaN(v2.x) && v2.x > boundsMax.x)
			{
				boundsMax.x = v2.x;
			}
			if (!double.IsNaN(v2.y) && v2.y > boundsMax.y)
			{
				boundsMax.y = v2.y;
			}
			if (!double.IsNaN(v2.z) && v2.z > boundsMax.z)
			{
				boundsMax.z = v2.z;
			}
		}
		double3 boundsCenter = new double3((boundsMax.x - boundsMin.x) * 0.5 + boundsMin.x, (boundsMax.y - boundsMin.y) * 0.5 + boundsMin.y, (boundsMax.z - boundsMin.z) * 0.5 + boundsMin.z);
		double radius = 0.0;
		for (int n = 0; n < array.Length; n++)
		{
			double3 v3 = array[n];
			double num7 = v3.x - boundsCenter.x;
			double num8 = v3.y - boundsCenter.y;
			double num9 = v3.z - boundsCenter.z;
			radius = Math.Max(radius, Math.Sqrt(num7 * num7 + num8 * num8 + num9 * num9));
		}
		if (!Finite(radius) || radius <= 0.0)
		{
			throw new ArgumentException("Radar mesh has zero or non-finite extent.", "verticesMeters");
		}
		int num10 = triangles.Length / 3;
		double3[] normals = new double3[num10];
		double[] areas = new double[num10];
		for (int num11 = 0; num11 < triangles.Length; num11 += 3)
		{
			int num12 = triangles[num11];
			int num13 = triangles[num11 + 1];
			int num14 = triangles[num11 + 2];
			double3 e3 = array[num13] - array[num12];
			double3 e4 = array[num14] - array[num12];
			double3 cross = new double3(e3.y * e4.z - e3.z * e4.y, e3.z * e4.x - e3.x * e4.z, e3.x * e4.y - e3.y * e4.x);
			double num15 = Math.Sqrt(cross.x * cross.x + cross.y * cross.y + cross.z * cross.z);
			if (!Finite(num15) || num15 <= 0.0)
			{
				throw new ArgumentException("Oriented radar face collapsed.", "verticesMeters");
			}
			normals[num11 / 3] = new double3(cross.x / num15, cross.y / num15, cross.z / num15);
			areas[num11 / 3] = num15 * 0.5;
		}
		return new RadarModel(array, triangles, normals, areas, boundsMin, boundsMax, boundsCenter, radius, discarded);
	}

	private static bool Finite(double value)
	{
		return !double.IsNaN(value) && !double.IsInfinity(value);
	}

	private static bool Finite(double3 value)
	{
		return Finite(value.x) && Finite(value.y) && Finite(value.z);
	}
}
