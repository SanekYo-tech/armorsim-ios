using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class HalfLathe
{
	public static HalfLatheTopology MakeTopology(LatheKind kind, int profilePoints, int segments)
	{
		HalfLatheTopology halfLatheTopology = new HalfLatheTopology();
		halfLatheTopology.kind = kind;
		halfLatheTopology.profilePoints = profilePoints;
		halfLatheTopology.segments = segments;
		if (kind == LatheKind.Empty || profilePoints < 3 || segments < 2)
		{
			halfLatheTopology.sideVertexCount = 0;
			halfLatheTopology.triangles = new int[0];
			halfLatheTopology.sideTriangles = new int[0];
			halfLatheTopology.capTriangles = new int[0];
			return halfLatheTopology;
		}
		List<int> list = new List<int>((segments + 1) * profilePoints * 6);
		if (kind == LatheKind.Tube)
		{
			halfLatheTopology.sideVertexCount = (segments + 1) * profilePoints;
			for (int i = 0; i < profilePoints; i++)
			{
				int num = (i + 1) % profilePoints * (segments + 1);
				for (int j = 0; j < segments; j++)
				{
					int num2 = i * (segments + 1) + j;
					int num3 = num + j;
					list.Add(num2);
					list.Add(num2 + 1);
					list.Add(num3 + 1);
					list.Add(num2);
					list.Add(num3 + 1);
					list.Add(num3);
				}
			}
		}
		else
		{
			halfLatheTopology.sideVertexCount = (profilePoints - 2) * (segments + 1) + 2;
			for (int k = 1; k <= segments; k++)
			{
				list.Add(0);
				list.Add(k + 1);
				list.Add(k);
			}
			for (int l = 1; l < profilePoints - 2; l++)
			{
				for (int m = 0; m < segments; m++)
				{
					int num4 = halfLatheTopology.RingBase(l) + m + 1;
					int num5 = halfLatheTopology.RingBase(l + 1) + m + 1;
					list.Add(num4);
					list.Add(num4 + 1);
					list.Add(num5);
					list.Add(num4);
					list.Add(num5);
					list.Add(num5 - 1);
				}
			}
			int num6 = halfLatheTopology.RingBase(profilePoints - 2);
			int num7 = halfLatheTopology.RingBase(profilePoints - 1);
			for (int n = 0; n < segments; n++)
			{
				list.Add(num7);
				list.Add(num6 + n);
				list.Add(num6 + n + 1);
			}
		}
		int count = list.Count;
		halfLatheTopology.capVertexCount = profilePoints * 2;
		int num8 = ((kind != LatheKind.Tube) ? (profilePoints - 1) : profilePoints);
		for (int num9 = 0; num9 < num8; num9++)
		{
			int num10 = (num9 + 1) % profilePoints;
			list.Add(halfLatheTopology.CapIndex(num9, 0));
			list.Add(halfLatheTopology.CapIndex(num10, 1));
			list.Add(halfLatheTopology.CapIndex(num9, 1));
			list.Add(halfLatheTopology.CapIndex(num9, 0));
			list.Add(halfLatheTopology.CapIndex(num10, 0));
			list.Add(halfLatheTopology.CapIndex(num10, 1));
		}
		halfLatheTopology.triangles = list.ToArray();
		halfLatheTopology.sideTriangles = new int[count];
		Array.Copy(halfLatheTopology.triangles, 0, halfLatheTopology.sideTriangles, 0, count);
		halfLatheTopology.capTriangles = new int[halfLatheTopology.triangles.Length - count];
		Array.Copy(halfLatheTopology.triangles, count, halfLatheTopology.capTriangles, 0, halfLatheTopology.capTriangles.Length);
		return halfLatheTopology;
	}

	public static void BuildVertices(Vector2[] arc, HalfLatheTopology topo, NativeArray<Vector3> verts, NativeArray<Vector3> norms, int vertexOffset = 0)
	{
		BuildVerticesInto(arc, topo, delegate(int i, Vector3 v)
		{
			verts[i + vertexOffset] = v;
		}, delegate(int i, Vector3 n)
		{
			norms[i + vertexOffset] = n;
		});
	}

	public static void BuildVertices(Vector2[] arc, HalfLatheTopology topo, Vector3[] verts, Vector3[] norms)
	{
		BuildVerticesInto(arc, topo, delegate(int i, Vector3 v)
		{
			verts[i] = v;
		}, delegate(int i, Vector3 n)
		{
			norms[i] = n;
		});
	}

	private static void BuildVerticesInto(Vector2[] arc, HalfLatheTopology topo, Action<int, Vector3> setVert, Action<int, Vector3> setNorm)
	{
		if (topo.VertexCount == 0)
		{
			return;
		}
		int segments = topo.segments;
		int profilePoints = topo.profilePoints;
		int num = -1;
		float num2 = float.MinValue;
		for (int i = 0; i < profilePoints; i++)
		{
			if (topo.kind == LatheKind.Capped && (i == 0 || i == profilePoints - 1))
			{
				continue;
			}
			if (arc[i].x > num2)
			{
				num2 = arc[i].x;
				num = i;
			}
		}
		float num3;
		if (num >= 0)
		{
			num3 = ((MeridianNormal(arc, topo, num, 1f).x >= 0f) ? 1f : (-1f));
		}
		else
		{
			num3 = 1f;
			if (profilePoints <= 0)
			{
				return;
			}
		}
		for (int j = 0; j < profilePoints; j++)
		{
			if (topo.kind == LatheKind.Capped && (j == 0 || j == profilePoints - 1))
			{
				int num4 = topo.RingBase(j);
				setVert(num4, new Vector3(0f, 0f, arc[j].y));
				setNorm(num4, new Vector3(0f, 0f, (j == 0) ? (-1f) : 1f));
				continue;
			}
			Vector2 vector = MeridianNormal(arc, topo, j, num3);
			int num5 = topo.RingBase(j);
			for (int k = 0; k <= segments; k++)
			{
				float num6 = k * Mathf.PI / segments;
				float num7 = Mathf.Sin(num6);
				float num8 = Mathf.Cos(num6);
				setVert(num5 + k, new Vector3(arc[j].x * num7, arc[j].x * num8, arc[j].y));
				setNorm(num5 + k, new Vector3(vector.x * num7, vector.x * num8, vector.y));
			}
		}
		for (int l = 0; l < profilePoints; l++)
		{
			setVert(topo.CapIndex(l, 0), new Vector3(arc[l].x, 0f, arc[l].y));
			setVert(topo.CapIndex(l, 1), new Vector3(-arc[l].x, 0f, arc[l].y));
			setNorm(topo.CapIndex(l, 0), new Vector3(0f, -1f, 0f));
			setNorm(topo.CapIndex(l, 1), new Vector3(0f, -1f, 0f));
		}
	}

	private static Vector2 MeridianNormal(Vector2[] arc, HalfLatheTopology topo, int k, float orient)
	{
		int num;
		int num2;
		if (topo.kind == LatheKind.Tube)
		{
			num = (k - 1 + topo.profilePoints) % topo.profilePoints;
			num2 = (k + 1) % topo.profilePoints;
		}
		else
		{
			num = Math.Max(k - 1, 0);
			num2 = Math.Min(topo.profilePoints - 1, k + 1);
		}
		float num3 = arc[num2].x - arc[num].x;
		float num4 = arc[num2].y - arc[num].y;
		float num5 = (float)Math.Sqrt(num3 * num3 + num4 * num4);
		if (num5 < 1e-12f)
		{
			return new Vector2(1f, 0f);
		}
		return new Vector2(orient / num5 * num4, orient / num5 * -num3);
	}

	private static float ProfileOrientation(Vector2[] arc, HalfLatheTopology topo)
	{
		int num = -1;
		float num2 = float.MinValue;
		for (int i = 0; i < topo.profilePoints; i++)
		{
			if (topo.kind == LatheKind.Capped && (i == 0 || i == topo.profilePoints - 1))
			{
				continue;
			}
			if (arc[i].x > num2)
			{
				num2 = arc[i].x;
				num = i;
			}
		}
		if (num < 0)
		{
			return 1f;
		}
		return (MeridianNormal(arc, topo, num, 1f).x >= 0f) ? 1f : (-1f);
	}

	public static float ClampOrbitAzimuth(float azimuthDeg, UltraViewMode mode)
	{
		float num = azimuthDeg - Mathf.Floor(azimuthDeg / 360f) * 360f;
		num = Mathf.Clamp(num, 0f, 360f);
		if (mode == UltraViewMode.Exterior)
		{
			return num;
		}
		return Mathf.Clamp(num, 184f, 356f);
	}
}
