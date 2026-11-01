using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;

public sealed class RadarBvh
{
	private struct Node
	{
		public double3 Min;

		public double3 Max;

		public int Left;

		public int Right;

		public int First;

		public int Count;

		public bool IsLeaf => Count > 0;
	}

	private struct Bin
	{
		public double3 Min;

		public double3 Max;

		public int Count;

		public void Include(double3 min, double3 max)
		{
			if (Count == 0)
			{
				Min = min;
				Max = max;
				Count = 1;
				return;
			}
			Min = new double3(Math.Min(Min.x, min.x), Math.Min(Min.y, min.y), Math.Min(Min.z, min.z));
			Max = new double3(Math.Max(Max.x, max.x), Math.Max(Max.y, max.y), Math.Max(Max.z, max.z));
			Count++;
		}
	}

	private const int BinCount = 16;

	private const int MaxLeafFaces = 4;

	private readonly RadarScene scene;

	private readonly int[] order;

	private readonly double3[] faceMin;

	private readonly double3[] faceMax;

	private readonly Node[] nodes;

	public int NodeCount => nodes.Length;

	internal RadarBvh(RadarScene scene, CancellationToken cancellationToken)
	{
		this.scene = scene;
		int faceCount = scene.Triangles.Length / 3;
		order = new int[faceCount];
		faceMin = new double3[faceCount];
		faceMax = new double3[faceCount];
		for (int i = 0; i < faceCount; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			order[i] = i;
			scene.GetTriangle(i, out double3 a, out double3 b, out double3 c);
			faceMin[i] = new double3(
				Math.Min(a.x, Math.Min(b.x, c.x)),
				Math.Min(a.y, Math.Min(b.y, c.y)),
				Math.Min(a.z, Math.Min(b.z, c.z)));
			faceMax[i] = new double3(
				Math.Max(a.x, Math.Max(b.x, c.x)),
				Math.Max(a.y, Math.Max(b.y, c.y)),
				Math.Max(a.z, Math.Max(b.z, c.z)));
		}
		List<Node> builder = new List<Node>(Math.Max(1, faceCount * 2));
		Build(builder, 0, faceCount, cancellationToken);
		nodes = builder.ToArray();
	}

	private int Build(List<Node> builder, int first, int count, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		int index = builder.Count;
		builder.Add(default);
		int f0 = order[first];
		double3 bmin = faceMin[f0];
		double3 bmax = faceMax[f0];
		double3 cmin = scene.Centroids[f0];
		double3 cmax = cmin;
		for (int i = first + 1; i < first + count; i++)
		{
			int f = order[i];
			bmin = new double3(
				Math.Min(bmin.x, faceMin[f].x),
				Math.Min(bmin.y, faceMin[f].y),
				Math.Min(bmin.z, faceMin[f].z));
			bmax = new double3(
				Math.Max(bmax.x, faceMax[f].x),
				Math.Max(bmax.y, faceMax[f].y),
				Math.Max(bmax.z, faceMax[f].z));
			cmin = new double3(
				Math.Min(cmin.x, scene.Centroids[f].x),
				Math.Min(cmin.y, scene.Centroids[f].y),
				Math.Min(cmin.z, scene.Centroids[f].z));
			cmax = new double3(
				Math.Max(cmax.x, scene.Centroids[f].x),
				Math.Max(cmax.y, scene.Centroids[f].y),
				Math.Max(cmax.z, scene.Centroids[f].z));
		}
		if (count <= MaxLeafFaces)
		{
			builder[index] = new Node
			{
				Min = bmin,
				Max = bmax,
				Left = 0,
				Right = 0,
				First = first,
				Count = count
			};
			return index;
		}
		int bestAxis = -1;
		int bestSplit = -1;
		double bestCost = 1.0 / 0.0;
		for (int axis = 0; axis < 3; axis++)
		{
			double lo = Axis(cmin, axis);
			double span = Axis(cmax, axis) - lo;
			if (span > 0.0)
			{
				Bin[] bins = new Bin[BinCount];
				for (int i = first; i < first + count; i++)
				{
					int f = order[i];
					int b = BinOf(Axis(scene.Centroids[f], axis), lo, span);
					bins[b].Include(faceMin[f], faceMax[f]);
				}
				Bin[] left = new Bin[BinCount - 1];
				Bin[] right = new Bin[BinCount - 1];
				Bin lacc = default;
				for (int i = 0; i < BinCount - 1; i++)
				{
					lacc = Merge(lacc, bins[i]);
					left[i] = lacc;
				}
				Bin racc = default;
				for (int i = BinCount - 1; i > 0; i--)
				{
					racc = Merge(racc, bins[i]);
					right[i - 1] = racc;
				}
				for (int k = 0; k < BinCount - 1; k++)
				{
					if (left[k].Count == 0 || right[k].Count == 0)
					{
						continue;
					}
					double cost = Area(left[k].Min, left[k].Max) * left[k].Count + Area(right[k].Min, right[k].Max) * right[k].Count;
					if (bestCost > cost)
					{
						bestCost = cost;
						bestAxis = axis;
						bestSplit = k;
					}
				}
			}
		}
		int split;
		if (bestAxis < 0)
		{
			split = first + count / 2;
		}
		else
		{
			double lo = Axis(cmin, bestAxis);
			double span = Axis(cmax, bestAxis) - lo;
			int[] buffer = new int[count];
			int j = 0;
			for (int i = first; i < first + count; i++)
			{
				if (BinOf(Axis(scene.Centroids[order[i]], bestAxis), lo, span) <= bestSplit)
				{
					buffer[j++] = order[i];
				}
			}
			for (int i = first; i < first + count; i++)
			{
				if (BinOf(Axis(scene.Centroids[order[i]], bestAxis), lo, span) > bestSplit)
				{
					buffer[j++] = order[i];
				}
			}
			Array.Copy(buffer, 0, order, first, count);
			split = first + j;
		}
		if (split == first || split == first + count)
		{
			split = first + count / 2;
		}
		int leftIndex = Build(builder, first, split - first, token);
		int rightIndex = Build(builder, split, first + count - split, token);
		builder[index] = new Node
		{
			Min = bmin,
			Max = bmax,
			Left = leftIndex,
			Right = rightIndex,
			First = 0,
			Count = 0
		};
		return index;
	}

	private static Bin Merge(Bin a, Bin b)
	{
		if (a.Count == 0)
		{
			return b;
		}
		if (b.Count == 0)
		{
			return a;
		}
		Bin r = default;
		r.Min = new double3(
			Math.Min(a.Min.x, b.Min.x),
			Math.Min(a.Min.y, b.Min.y),
			Math.Min(a.Min.z, b.Min.z));
		r.Max = new double3(
			Math.Max(a.Max.x, b.Max.x),
			Math.Max(a.Max.y, b.Max.y),
			Math.Max(a.Max.z, b.Max.z));
		r.Count = a.Count + b.Count;
		return r;
	}

	private static double Area(double3 min, double3 max)
	{
		double dx = max.x - min.x;
		double dy = max.y - min.y;
		double dz = max.z - min.z;
		return 2.0 * (dx * dy + dy * dz + dz * dx);
	}

	private static double Axis(double3 v, int axis)
	{
		if (axis == 0)
		{
			return v.x;
		}
		if (axis == 1)
		{
			return v.y;
		}
		return v.z;
	}

	private static int BinOf(double value, double lo, double span)
	{
		return Math.Min(BinCount - 1, Math.Max(0, (int)((value - lo) / span * 16.0)));
	}

	public RadarHit Closest(double3 origin, double3 direction, int excludedFace = -1, double maxDistance = 1.0 / 0.0, RadarCounters counters = null)
	{
		int bestFace = -1;
		double bestT = maxDistance;
		ClosestNode(0, origin, direction, excludedFace, ref bestFace, ref bestT, counters);
		if (bestFace < 0)
		{
			return new RadarHit(-1, double.PositiveInfinity, default(double3));
		}
		return new RadarHit(bestFace, bestT, origin + direction * bestT);
	}

	public bool AnyHit(double3 origin, double3 direction, int excludedFace = -1, double maxDistance = 1.0 / 0.0, RadarCounters counters = null)
	{
		return AnyNode(0, origin, direction, excludedFace, maxDistance, counters);
	}

	private void ClosestNode(int index, double3 origin, double3 direction, int excludedFace, ref int bestFace, ref double bestT, RadarCounters counters)
	{
		Node node = nodes[index];
		double near;
		if (!BoxHit(node, origin, direction, bestT, out near))
		{
			return;
		}
		if (node.IsLeaf)
		{
			for (int i = node.First; i < node.First + node.Count; i++)
			{
				int face = order[i];
				double t;
				if (TriangleHit(face, origin, direction, bestT, out t))
				{
					if (face == excludedFace)
					{
						if (counters != null)
						{
							counters.SelfHitsSkipped++;
						}
					}
					else if (t < bestT || (t == bestT && (bestFace < 0 || face < bestFace)))
					{
						bestT = t;
						bestFace = face;
					}
				}
			}
			return;
		}
		double leftNear;
		double rightNear;
		bool leftHit = BoxHit(nodes[node.Left], origin, direction, bestT, out leftNear);
		bool rightHit = BoxHit(nodes[node.Right], origin, direction, bestT, out rightNear);
		if (leftHit && rightHit)
		{
			if (rightNear >= leftNear)
			{
				ClosestNode(node.Left, origin, direction, excludedFace, ref bestFace, ref bestT, counters);
				ClosestNode(node.Right, origin, direction, excludedFace, ref bestFace, ref bestT, counters);
			}
			else
			{
				ClosestNode(node.Right, origin, direction, excludedFace, ref bestFace, ref bestT, counters);
				ClosestNode(node.Left, origin, direction, excludedFace, ref bestFace, ref bestT, counters);
			}
		}
		else if (leftHit)
		{
			ClosestNode(node.Left, origin, direction, excludedFace, ref bestFace, ref bestT, counters);
		}
		else if (rightHit)
		{
			ClosestNode(node.Right, origin, direction, excludedFace, ref bestFace, ref bestT, counters);
		}
	}

	private bool AnyNode(int index, double3 origin, double3 direction, int excludedFace, double maxDistance, RadarCounters counters)
	{
		Node node = nodes[index];
		double near;
		if (!BoxHit(node, origin, direction, maxDistance, out near))
		{
			return false;
		}
		if (node.IsLeaf)
		{
			for (int i = node.First; i < node.First + node.Count; i++)
			{
				int face = order[i];
				double t;
				if (TriangleHit(face, origin, direction, maxDistance, out t))
				{
					if (face != excludedFace)
					{
						return true;
					}
					if (counters != null)
					{
						counters.SelfHitsSkipped++;
					}
				}
			}
			return false;
		}
		double leftNear;
		double rightNear;
		bool leftHit = BoxHit(nodes[node.Left], origin, direction, maxDistance, out leftNear);
		bool rightHit = BoxHit(nodes[node.Right], origin, direction, maxDistance, out rightNear);
		if (leftHit && rightHit && leftNear > rightNear)
		{
			if (AnyNode(node.Right, origin, direction, excludedFace, maxDistance, counters))
			{
				return true;
			}
			return AnyNode(node.Left, origin, direction, excludedFace, maxDistance, counters);
		}
		if (leftHit)
		{
			if (AnyNode(node.Left, origin, direction, excludedFace, maxDistance, counters))
			{
				return true;
			}
		}
		if (rightHit)
		{
			return AnyNode(node.Right, origin, direction, excludedFace, maxDistance, counters);
		}
		return false;
	}

	private static bool BoxHit(Node box, double3 origin, double3 direction, double maxDistance, out double near)
	{
		near = 0.0;
		double tMax = maxDistance;
		for (int axis = 0; axis < 3; axis++)
		{
			double o = Axis(origin, axis);
			double d = Axis(direction, axis);
			double lo = Axis(box.Min, axis);
			double hi = Axis(box.Max, axis);
			if (d == 0.0)
			{
				if (lo > o || o > hi)
				{
					return false;
				}
				continue;
			}
			double t1 = (lo - o) / d;
			double t2 = (hi - o) / d;
			if (t1 > t2)
			{
				double tmp = t1;
				t1 = t2;
				t2 = tmp;
			}
			near = Math.Max(near, t1);
			tMax = Math.Min(tMax, t2);
			if (near > tMax)
			{
				return false;
			}
		}
		return tMax > 0.0;
	}

	private bool TriangleHit(int face, double3 origin, double3 direction, double maxDistance, out double t)
	{
		scene.GetTriangle(face, out double3 a, out double3 b, out double3 c);
		double3 e1 = b - a;
		double3 e2 = c - a;
		double3 h = new double3(
			direction.y * e2.z - direction.z * e2.y,
			direction.z * e2.x - direction.x * e2.z,
			direction.x * e2.y - direction.y * e2.x);
		double det = e1.x * h.x + e1.y * h.y + e1.z * h.z;
		double eps = 2.0 * scene.Areas[face] * 1e-14;
		if (eps >= Math.Abs(det))
		{
			t = 0.0;
			return false;
		}
		double invDet = 1.0 / det;
		double3 s = origin - a;
		double u = (s.x * h.x + s.y * h.y + s.z * h.z) * invDet;
		if (u < 0.0 || u > 1.0)
		{
			t = 0.0;
			return false;
		}
		double3 q = new double3(
			s.y * e1.z - s.z * e1.y,
			s.z * e1.x - s.x * e1.z,
			s.x * e1.y - s.y * e1.x);
		double v = (direction.x * q.x + direction.y * q.y + direction.z * q.z) * invDet;
		if (v < 0.0 || u + v > 1.0)
		{
			t = 0.0;
			return false;
		}
		t = (q.x * e2.x + q.y * e2.y + q.z * e2.z) * invDet;
		return t > 0.0 && t <= maxDistance;
	}
}
