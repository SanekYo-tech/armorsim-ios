using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class ProfileLathe
{
	public const int DefaultProfilePoints = 256;

	public const int DefaultSegments = 48;

	public const float AxisEpsFraction = 0.0001f;

	public static LatheKind ExtractOuterArc(NativeArray<Vector2> loop, Vector2[] dst)
	{
		if (!loop.IsCreated || loop.Length < 3 || dst == null || dst.Length < 3)
		{
			return LatheKind.Empty;
		}
		Vector2 vector = default(Vector2);
		for (int i = 0; i < loop.Length; i++)
		{
			if (loop[i].x > vector.x)
			{
				vector = loop[i];
			}
		}
		if (vector.x <= 0f)
		{
			return LatheKind.Empty;
		}
		float num = vector.x * AxisEpsFraction;
		int num2 = 0;
		int num3 = -1;
		int num4 = 0;
		while (num4 < loop.Length)
		{
			if (loop[num4].x >= num)
			{
				num4++;
				continue;
			}
			int num5 = 0;
			while (num5 < loop.Length && loop[(num4 + num5) % loop.Length].x < num)
			{
				num5++;
			}
			if (num5 > num2)
			{
				num3 = num4;
			}
			num2 = Math.Max(num5, num2);
			num4 += num5;
		}
		if (num2 == 0)
		{
			ResampleClosed(loop, loop.Length, dst);
			return LatheKind.Tube;
		}
		if (num2 >= loop.Length - 2)
		{
			return LatheKind.Empty;
		}
		ResampleOpen(loop, loop.Length, (num3 + num2 - 1) % loop.Length, loop.Length - num2 + 2, dst);
		dst[0] = new Vector2(0f, dst[0].y);
		dst[dst.Length - 1] = new Vector2(0f, dst[dst.Length - 1].y);
		return LatheKind.Capped;
	}

	private static void ResampleOpen(NativeArray<Vector2> loop, int n, int start, int count, Vector2[] dst)
	{
		float[] array = new float[count];
		array[0] = 0f;
		for (int i = 1; i < count; i++)
		{
			Vector2 vector = loop[(start + i - 1) % n];
			Vector2 vector2 = loop[(start + i) % n];
			float num = vector.x - vector2.x;
			float num2 = vector.y - vector2.y;
			array[i] = array[i - 1] + (float)Math.Sqrt(num * num + num2 * num2);
		}
		float num3 = array[count - 1];
		if (num3 <= 0f)
		{
			for (int j = 0; j < dst.Length; j++)
			{
				dst[j] = loop[start % n];
			}
			return;
		}
		for (int k = 0; k < dst.Length; k++)
		{
			float num4 = (float)k * num3 / (float)(dst.Length - 1);
			int num5 = 0;
			while (num5 < count - 2 && num4 > array[num5 + 1])
			{
				num5++;
			}
			float num6 = array[num5 + 1] - array[num5];
			float num7 = ((num6 > 1e-20f) ? ((num4 - array[num5]) / num6) : 0f);
			Vector2 vector3 = loop[(start + num5) % n];
			Vector2 vector4 = loop[(start + num5 + 1) % n];
			dst[k] = vector3 + (vector4 - vector3) * num7;
		}
	}

	private static void ResampleClosed(NativeArray<Vector2> loop, int n, Vector2[] dst)
	{
		float[] array = new float[n + 1];
		array[0] = 0f;
		for (int i = 1; i <= n; i++)
		{
			Vector2 vector = loop[i - 1];
			Vector2 vector2 = loop[i % n];
			float num = vector.x - vector2.x;
			float num2 = vector.y - vector2.y;
			array[i] = array[i - 1] + (float)Math.Sqrt(num * num + num2 * num2);
		}
		float num3 = array[n];
		if (num3 <= 0f)
		{
			for (int j = 0; j < dst.Length; j++)
			{
				dst[j] = loop[0];
			}
			return;
		}
		for (int k = 0; k < dst.Length; k++)
		{
			float num4 = (float)k * num3 / (float)dst.Length;
			int num5 = 0;
			while (num5 < n - 1 && num4 > array[num5 + 1])
			{
				num5++;
			}
			float num6 = array[num5 + 1] - array[num5];
			float num7 = ((num6 > 1e-20f) ? ((num4 - array[num5]) / num6) : 0f);
			Vector2 vector3 = loop[num5];
			Vector2 vector4 = loop[(num5 + 1) % n];
			dst[k] = vector3 + (vector4 - vector3) * num7;
		}
	}

	public static LatheTopology MakeTopology(LatheKind kind, int profilePoints, int segments)
	{
		LatheTopology latheTopology = new LatheTopology();
		latheTopology.kind = kind;
		latheTopology.profilePoints = profilePoints;
		latheTopology.segments = segments;
		if (kind == LatheKind.Empty || profilePoints < 3 || segments < 3)
		{
			latheTopology.vertexCount = 0;
			latheTopology.triangles = new int[0];
			return latheTopology;
		}
		if (kind == LatheKind.Tube)
		{
			latheTopology.vertexCount = profilePoints * segments;
			latheTopology.triangles = new int[profilePoints * segments * 6];
			int num = 0;
			for (int i = 0; i < profilePoints; i++)
			{
				int num2 = (i + 1) % profilePoints * segments;
				for (int j = 0; j < segments; j++)
				{
					int num3 = i * segments + j;
					int num4 = i * segments + (j + 1) % segments;
					int num5 = num2 + (j + 1) % segments;
					int innerNum6 = num2 + j;
					latheTopology.triangles[num] = num3;
					latheTopology.triangles[num + 1] = num4;
					latheTopology.triangles[num + 2] = num5;
					latheTopology.triangles[num + 3] = num3;
					latheTopology.triangles[num + 4] = num5;
					latheTopology.triangles[num + 5] = innerNum6;
					num += 6;
				}
			}
			return latheTopology;
		}
		latheTopology.vertexCount = (profilePoints - 2) * segments + 2;
		latheTopology.triangles = new int[(profilePoints - 2) * segments * 6];
		int num6 = 0;
		for (int k = 1; k <= segments; k++)
		{
			latheTopology.triangles[num6] = 0;
			latheTopology.triangles[num6 + 1] = k % segments + 1;
			latheTopology.triangles[num6 + 2] = k;
			num6 += 3;
		}
		for (int l = 1; l <= profilePoints - 3; l++)
		{
			for (int m = 0; m < segments; m++)
			{
				int num7 = latheTopology.RingBase(l) + m;
				int num8 = latheTopology.RingBase(l) + (m + 1) % segments;
				int num9 = latheTopology.RingBase(l + 1) + (m + 1) % segments;
				int num10 = latheTopology.RingBase(l + 1) + m;
				latheTopology.triangles[num6] = num7;
				latheTopology.triangles[num6 + 1] = num8;
				latheTopology.triangles[num6 + 2] = num9;
				latheTopology.triangles[num6 + 3] = num7;
				latheTopology.triangles[num6 + 4] = num9;
				latheTopology.triangles[num6 + 5] = num10;
				num6 += 6;
			}
		}
		int num11 = latheTopology.RingBase(profilePoints - 2);
		int num12 = latheTopology.RingBase(profilePoints - 1);
		for (int n = 0; n < segments; n++)
		{
			latheTopology.triangles[num6] = num11 + n;
			latheTopology.triangles[num6 + 1] = num11 + (n + 1) % segments;
			latheTopology.triangles[num6 + 2] = num12;
			num6 += 3;
		}
		return latheTopology;
	}

	public static void BuildVertices(Vector2[] arc, LatheTopology topo, NativeArray<Vector3> verts, NativeArray<Vector3> normals, bool flatShaded = false, int vertexOffset = 0)
	{
		if (topo.kind == LatheKind.Empty || topo.vertexCount == 0)
		{
			return;
		}
		int profilePoints = topo.profilePoints;
		int segments = topo.segments;
		if (profilePoints <= 0)
		{
			if (flatShaded)
			{
				FlattenNormals(topo, verts, normals, vertexOffset);
			}
			return;
		}
		for (int i = 0; i < profilePoints; i++)
		{
			Vector2 vector = ((i > 0) ? arc[i - 1] : ((topo.kind == LatheKind.Tube) ? arc[profilePoints - 1] : arc[0]));
			Vector2 vector2 = ((i < profilePoints - 1) ? arc[i + 1] : ((topo.kind == LatheKind.Tube) ? arc[0] : arc[i]));
			float num = vector2.x - vector.x;
			float num2 = vector2.y - vector.y;
			float num3 = Mathf.Sqrt(num * num + num2 * num2);
			if (num3 > 1e-20f)
			{
				num /= num3;
				num2 /= num3;
			}
			else
			{
				num = 0f;
				num2 = 1f;
			}
			if (topo.kind == LatheKind.Capped && (i == 0 || i == profilePoints - 1))
			{
				int num4 = topo.RingBase(i) + vertexOffset;
				verts[num4] = new Vector3(0f, 0f, arc[i].y);
				normals[num4] = new Vector3(0f, 0f, (num > 0f) ? (-1f) : 1f);
				continue;
			}
			int num5 = topo.RingBase(i) + vertexOffset;
			for (int j = 0; j < segments; j++)
			{
				float num6 = j * 6.28318548f / segments;
				float num7 = Mathf.Sin(num6);
				float num8 = Mathf.Cos(num6);
				verts[num5 + j] = new Vector3(arc[i].x * num7, arc[i].x * num8, arc[i].y);
				float num9 = Mathf.Sqrt(num2 * num2 + num * num);
				if (num9 > 1e-20f)
				{
					normals[num5 + j] = new Vector3(num2 * num7 / num9, num2 * num8 / num9, -num / num9);
				}
				else
				{
					normals[num5 + j] = new Vector3(num7, num8, 0f);
				}
			}
		}
		if (flatShaded)
		{
			FlattenNormals(topo, verts, normals, vertexOffset);
		}
	}

	private static void FlattenNormals(LatheTopology topo, NativeArray<Vector3> verts, NativeArray<Vector3> normals, int vertexOffset)
	{
		if (topo == null || topo.triangles == null)
		{
			return;
		}
		for (int i = 0; i + 2 < topo.triangles.Length; i += 3)
		{
			int num = topo.triangles[i] + vertexOffset;
			int num2 = topo.triangles[i + 1] + vertexOffset;
			int num3 = topo.triangles[i + 2] + vertexOffset;
			Vector3 vector = Vector3.Cross(verts[num3] - verts[num], verts[num2] - verts[num]);
			float num4 = (float)Math.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
			if (num4 > 1e-20f)
			{
				vector /= num4;
				normals[num] = vector;
				normals[num2] = vector;
				normals[num3] = vector;
			}
		}
	}

	public static int SplitShells(NativeArray<Vector2> profile, List<int> bounds)
	{
		bounds.Clear();
		if (!profile.IsCreated || profile.Length == 0)
		{
			return 0;
		}
		bounds.Add(0);
		for (int i = 1; i < profile.Length; i++)
		{
			if (profile[i] == profile[i - 1])
			{
				bounds.Add(i + 1);
				i++;
			}
		}
		if (bounds[bounds.Count - 1] != profile.Length)
		{
			bounds.Add(profile.Length);
		}
		return bounds.Count - 1;
	}

	public static void ComputeBounds(NativeArray<Vector3> verts, int count, out Vector3 min, out Vector3 max)
	{
		min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		for (int i = 0; i < count; i++)
		{
			Vector3 vector = verts[i];
			if (vector.x < min.x)
			{
				min.x = vector.x;
			}
			if (vector.x > max.x)
			{
				max.x = vector.x;
			}
			if (vector.y < min.y)
			{
				min.y = vector.y;
			}
			if (vector.y > max.y)
			{
				max.y = vector.y;
			}
			if (vector.z < min.z)
			{
				min.z = vector.z;
			}
			if (vector.z > max.z)
			{
				max.z = vector.z;
			}
		}
	}
}
