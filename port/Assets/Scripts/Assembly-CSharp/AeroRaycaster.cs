using System;
using UnityEngine;

public sealed class AeroRaycaster
{
	private struct Node
	{
		public Vector3 Min;

		public Vector3 Max;

		public int Left;

		public int Right;

		public int Start;

		public int Count;
	}

	private readonly AeroMeshData data;

	private readonly int[] order;

	private readonly Vector3[] centroids;

	private readonly Node[] nodes;

	private int nodeCount;

	public Vector3 BoundsMin { get; private set; }

	public Vector3 BoundsMax { get; private set; }

	public AeroRaycaster(AeroMeshData mesh)
	{
		if (mesh == null || mesh.TriangleCount == 0)
		{
			throw new AeroException("aero.error.midship", Array.Empty<object>());
		}
		data = mesh;
		order = new int[mesh.TriangleCount];
		centroids = new Vector3[order.Length];
		nodes = new Node[order.Length * 2];
		float num = 3f;
		for (int i = 0; i < order.Length; i++)
		{
			order[i] = i;
			int num2 = mesh.Triangles[i * 3];
			int num3 = mesh.Triangles[i * 3 + 1];
			int num4 = mesh.Triangles[i * 3 + 2];
			centroids[i] = new Vector3((mesh.Vertices[num2].x + mesh.Vertices[num3].x + mesh.Vertices[num4].x) / num, (mesh.Vertices[num2].y + mesh.Vertices[num3].y + mesh.Vertices[num4].y) / num, (mesh.Vertices[num2].z + mesh.Vertices[num3].z + mesh.Vertices[num4].z) / num);
		}
		Build(0, order.Length);
		BoundsMin = nodes[0].Min;
		BoundsMax = nodes[0].Max;
	}

	private int Build(int start, int count)
	{
		int num = nodeCount++;
		Vector3 min = Vector3.one * float.PositiveInfinity;
		Vector3 max = Vector3.one * float.NegativeInfinity;
		for (int i = start; i < start + count; i++)
		{
			Vector3 centroid = centroids[order[i]];
			min = Vector3.Min(min, centroid);
			max = Vector3.Max(max, centroid);
			for (int j = 0; j < 3; j++)
			{
				Vector3 vertex = data.Vertices[data.Triangles[order[i] * 3 + j]];
				min = Vector3.Min(min, vertex);
				max = Vector3.Max(max, vertex);
			}
		}
		int num2 = 0;
		int num3 = 0;
		if (count > 8)
		{
			int axis = ((max.x - min.x < max.y - min.y) || (max.x - min.x < max.z - min.z)) ? (((max.y - min.y < max.z - min.z) ? 2 : 1)) : 0;
			int num4 = start + count / 2;
			PartitionMedian(start, start + count - 1, num4, axis);
			num2 = Build(start, num4 - start);
			num3 = Build(num4, count - (num4 - start));
			count = 0;
		}
		nodes[num].Min = min;
		nodes[num].Max = max;
		nodes[num].Left = num2;
		nodes[num].Right = num3;
		nodes[num].Start = start;
		nodes[num].Count = count;
		return num;
	}

	private void PartitionMedian(int first, int last, int middle, int axis)
	{
		int num = first;
		int num2 = last;
		while (num < num2)
		{
			float pivot = centroids[order[(num2 + num) / 2]][axis];
			int num3 = num;
			int num4 = num2;
			while (num3 <= num4)
			{
				while (centroids[order[num3]][axis] < pivot)
				{
					num3++;
				}
				while (pivot < centroids[order[num4]][axis])
				{
					num4--;
				}
				if (num3 <= num4)
				{
					int num5 = order[num3];
					order[num3] = order[num4];
					order[num4] = num5;
					num3++;
					num4--;
				}
			}
			if (num4 < middle)
			{
				if (middle < num3)
				{
					return;
				}
				num = num3;
			}
			else
			{
				num2 = num4;
			}
		}
	}

	public bool FirstHit(Vector3 origin, Vector3 direction, float maxDistance, out AeroRayHit hit)
	{
		hit = default(AeroRayHit);
		if (!Finite(origin) || !Finite(direction) || !Finite(maxDistance) || !(0f < maxDistance))
		{
			throw new AeroException("aero.error.calculation", Array.Empty<object>());
		}
		float magnitude = direction.magnitude;
		if (magnitude <= 1E-12f || !Finite(magnitude))
		{
			throw new AeroException("aero.error.calculation", Array.Empty<object>());
		}
		Vector3 vector = direction / magnitude;
		float num = maxDistance;
		int num2 = -1;
		Trace(0, origin, vector, ref num, ref num2);
		if (num2 < 0)
		{
			return false;
		}
		hit.TriangleIndex = num2;
		hit.Distance = num;
		hit.Point = origin + vector * num;
		hit.Normal = data.Normals[num2];
		return true;
	}

	private void Trace(int index, Vector3 origin, Vector3 direction, ref float nearest, ref int triangle)
	{
		Node node = nodes[index];
		if (!Box(node, origin, direction, nearest, out var entry))
		{
			return;
		}
		if (node.Count < 1)
		{
			bool flag = Box(nodes[node.Left], origin, direction, nearest, out var entry2);
			bool flag2 = Box(nodes[node.Right], origin, direction, nearest, out var entry3);
			if (flag && flag2)
			{
				int num;
				int num2;
				if (entry3 < entry2)
				{
					num = node.Right;
					num2 = node.Left;
				}
				else
				{
					num = node.Left;
					num2 = node.Right;
				}
				Trace(num, origin, direction, ref nearest, ref triangle);
				Trace(num2, origin, direction, ref nearest, ref triangle);
				return;
			}
			if (!flag && !flag2)
			{
				return;
			}
			Trace(flag ? node.Left : node.Right, origin, direction, ref nearest, ref triangle);
			return;
		}
		int num3 = node.Start + node.Count;
		for (int i = node.Start; i < num3; i++)
		{
			int num4 = order[i];
			if (Triangle(num4, origin, direction, nearest, out var distance) && ((distance < nearest || triangle < 0) || (distance == nearest && num4 < triangle)))
			{
				nearest = distance;
				triangle = num4;
			}
		}
	}

	private static bool Box(Node node, Vector3 origin, Vector3 direction, float limit, out float entry)
	{
		double num = 0.0;
		double num2 = limit;
		for (int i = 0; i < 3; i++)
		{
			double num3 = direction[i];
			double num4 = origin[i];
			if (1e-30 > Math.Abs(num3))
			{
				if ((double)node.Min[i] > num4 || num4 > (double)node.Max[i])
				{
					entry = 0f;
					return false;
				}
				continue;
			}
			double num5 = ((double)node.Min[i] - num4) / num3;
			double num6 = ((double)node.Max[i] - num4) / num3;
			if (num5 > num6)
			{
				double num7 = num5;
				num5 = num6;
				num6 = num7;
			}
			num = Math.Max(num, num5);
			num2 = Math.Min(num2, num6);
			if (num > num2)
			{
				entry = 0f;
				return false;
			}
		}
		entry = (float)num;
		return true;
	}

	private bool Triangle(int index, Vector3 origin, Vector3 direction, float limit, out float distance)
	{
		int num = index * 3;
		Vector3 a = data.Vertices[data.Triangles[num]];
		Vector3 b = data.Vertices[data.Triangles[num + 1]];
		Vector3 c = data.Vertices[data.Triangles[num + 2]];
		double num2 = (double)b.x - (double)a.x;
		double num3 = (double)b.y - (double)a.y;
		double num4 = (double)b.z - (double)a.z;
		double num5 = (double)c.x - (double)a.x;
		double num6 = (double)c.y - (double)a.y;
		double num7 = (double)c.z - (double)a.z;
		double num8 = (double)direction.y * num7 - (double)direction.z * num6;
		double num9 = (double)direction.z * num5 - (double)direction.x * num7;
		double num10 = (double)direction.x * num6 - (double)direction.y * num5;
		double num11 = num2 * num8 + num3 * num9 + num4 * num10;
		if (1E-30 > Math.Abs(num11))
		{
			distance = 0f;
			return false;
		}
		double num12 = 1.0 / num11;
		double num13 = (double)origin.x - (double)a.x;
		double num14 = (double)origin.y - (double)a.y;
		double num15 = (double)origin.z - (double)a.z;
		double num16 = (num13 * num8 + num14 * num9 + num15 * num10) * num12;
		if (-1E-10 > num16 || num16 > 1.0000000001)
		{
			distance = 0f;
			return false;
		}
		double num17 = num14 * num4 - num15 * num3;
		double num18 = num15 * num2 - num13 * num4;
		double num19 = num13 * num3 - num14 * num2;
		double num20 = ((double)direction.y * num18 + (double)direction.x * num17 + (double)direction.z * num19) * num12;
		double num21 = (num18 * num6 + num17 * num5 + num19 * num7) * num12;
		if (-1E-10 <= num20 && num20 + num16 <= 1.0000000001 && 0.0 <= num21 && num21 <= (double)limit)
		{
			distance = (float)num21;
			return true;
		}
		distance = 0f;
		return false;
	}

	internal static bool Finite(float value)
	{
		return float.IsFinite(value);
	}

	internal static bool Finite(Vector3 value)
	{
		return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
	}
}
