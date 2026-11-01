using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public sealed class FreeSurfaceExtractor : IDisposable
{
	private sealed class LoopsByCount
	{
		public static readonly Comparison<List<int>> Instance = (x, y) => y.Count.CompareTo(x.Count);
	}

	public NativeArray<int> segA;

	public NativeArray<int> segB;

	public NativeArray<byte> segBodyTag;

	public int segCount;

	public int projSegStart;

	public int projSegEnd;

	public int targSegStart;

	public int targSegEnd;

	public NativeArray<int> surfNodes;

	public int surfNodeCount;

	public int projNodeStart;

	public int projNodeEnd;

	public int targNodeStart;

	public int targNodeEnd;

	public readonly List<List<int>> projectileLoops;

	public readonly List<List<int>> targetLoops;

	private NativeArray<int> nextFromNode;

	private NativeArray<int> nextOutSeg;

	private NativeArray<byte> nodeSeen;

	private readonly List<int> tmpSegA;

	private readonly List<int> tmpSegB;

	private int capacitySeg;

	private int capacityNode;

	private int to0;

	public FreeSurfaceExtractor(AxiMesh mesh)
	{
		projectileLoops = new List<List<int>>();
		targetLoops = new List<List<int>>();
		tmpSegA = new List<int>();
		tmpSegB = new List<int>();
		if (mesh != null)
		{
			capacitySeg = mesh.elemCount * 4;
			capacityNode = mesh.nodeCount;
			segA = new NativeArray<int>(capacitySeg, Allocator.Persistent, NativeArrayOptions.ClearMemory);
			segB = new NativeArray<int>(capacitySeg, Allocator.Persistent, NativeArrayOptions.ClearMemory);
			segBodyTag = new NativeArray<byte>(capacitySeg, Allocator.Persistent, NativeArrayOptions.ClearMemory);
			surfNodes = new NativeArray<int>(capacityNode, Allocator.Persistent, NativeArrayOptions.ClearMemory);
			nextFromNode = new NativeArray<int>(capacitySeg, Allocator.Persistent, NativeArrayOptions.ClearMemory);
			nextOutSeg = new NativeArray<int>(capacitySeg, Allocator.Persistent, NativeArrayOptions.ClearMemory);
			nodeSeen = new NativeArray<byte>(capacityNode, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		}
	}

	public void Extract(AxiMesh mesh, SolverDiagnostics diag)
	{
		tmpSegA.Clear();
		tmpSegB.Clear();
		projectileLoops.Clear();
		targetLoops.Clear();
		projSegStart = 0;
		CollectBoundary(mesh, 0);
		projSegEnd = tmpSegA.Count;
		targSegStart = tmpSegA.Count;
		CollectBoundary(mesh, 1);
		targSegEnd = tmpSegA.Count;
		segCount = Math.Min(targSegEnd, capacitySeg);
		for (int i = 0; i < segCount; i++)
		{
			segA[i] = tmpSegA[i];
			segB[i] = tmpSegB[i];
			segBodyTag[i] = (byte)((i < projSegEnd) ? 1 : 0);
		}
		BuildSurfaceNodes();
		BuildLoops(mesh, diag);
	}

	private void CollectBoundary(AxiMesh mesh, byte body)
	{
		for (int e = 0; e < mesh.elemCount; e++)
		{
			if (mesh.elemAlive[e] != 0 && mesh.elemBody[e] == body)
			{
				for (int k = 0; k < 4; k++)
				{
					int a = mesh.elemNodes[e * 4 + k];
					int b = mesh.elemNodes[e * 4 + (k + 1) % 4];
					if (SharedWithLiveNeighbour(mesh, e, a, b))
					{
						tmpSegA.Add(a);
						tmpSegB.Add(b);
					}
				}
			}
		}
	}

	private static bool SharedWithLiveNeighbour(AxiMesh mesh, int self, int a, int b)
	{
		int start = mesh.nodeAdjStart[a];
		int end = mesh.nodeAdjStart[a + 1];
		for (int i = start; i < end; i++)
		{
			int e = mesh.nodeAdjElem[i];
			if (e != self && mesh.elemAlive[e] != 0)
			{
				int n0 = mesh.elemNodes[e * 4];
				int n1 = mesh.elemNodes[e * 4 + 1];
				int n2 = mesh.elemNodes[e * 4 + 2];
				int n3 = mesh.elemNodes[e * 4 + 3];
				if (n0 == b || n1 == b || n2 == b || n3 == b)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void BuildSurfaceNodes()
	{
		for (int i = 0; i < capacityNode; i++)
		{
			nodeSeen[i] = 0;
		}
		surfNodeCount = 0;
		projNodeStart = 0;
		AddNodesOfRange(projSegStart, projSegEnd);
		projNodeEnd = surfNodeCount;
		targNodeStart = surfNodeCount;
		AddNodesOfRange(targSegStart, targSegEnd);
		targNodeEnd = surfNodeCount;
	}

	private void AddNodesOfRange(int from, int to)
	{
		for (int i = from; i < to && i < segCount; i++)
		{
			int a = segA[i];
			if (nodeSeen[a] == 0 && surfNodeCount < capacityNode)
			{
				nodeSeen[a] = 1;
				surfNodes[surfNodeCount++] = a;
			}
			int b = segB[i];
			if (nodeSeen[b] == 0 && surfNodeCount < capacityNode)
			{
				nodeSeen[b] = 1;
				surfNodes[surfNodeCount++] = b;
			}
		}
	}

	private void BuildLoops(AxiMesh mesh, SolverDiagnostics diag)
	{
		BuildLoopsForRange(mesh, projSegStart, projSegEnd, projectileLoops, diag);
		BuildLoopsForRange(mesh, targSegStart, targSegEnd, targetLoops, diag);
	}

	private void BuildLoopsForRange(AxiMesh mesh, int from, int to, List<List<int>> loops, SolverDiagnostics diag)
	{
		if (to <= from)
		{
			return;
		}
		to0 = to;
		for (int i = 0; i < capacitySeg; i++)
		{
			nextFromNode[i] = -1;
		}
		int branched = 0;
		for (int s = from; s < to; s++)
		{
			int v = segA[s];
			if (nextFromNode[v] >= 0)
			{
				branched++;
			}
			nextOutSeg[s] = nextFromNode[v];
			nextFromNode[v] = s;
		}
		bool[] used = new bool[to - from];
		int broken = 0;
		for (int start = from; start < to; start++)
		{
			if (used[start - from])
			{
				continue;
			}
			List<int> loop = new List<int>();
			int cur = start;
			int len = 1;
			while (cur >= from && cur < to && !used[cur - from])
			{
				used[cur - from] = true;
				loop.Add(segA[cur]);
				int v2 = segB[cur];
				cur = PickNext(mesh, cur, v2, used, from);
				if (cur < 0)
				{
					loop.Add(v2);
					broken++;
					break;
				}
				if (cur == start || len > to - from)
				{
					break;
				}
				len++;
			}
			if (loop.Count > 2)
			{
				loops.Add(loop);
			}
		}
		if (branched > 0 && broken > 0)
		{
			diag.WarnOnce("branchSurface", string.Format("поверхность разветвлена в {0} узлах и {1} цепочек остались ", branched, broken) + "РАЗОМКНУТЫМИ — обход граней их не сомкнул, контур показа неполон");
		}
		loops.Sort(LoopsByCount.Instance);
	}

	private int PickNext(AxiMesh mesh, int cur, int v, bool[] used, int from)
	{
		float vz = mesh.nodeZ[v];
		float vr = mesh.nodeR[v];
		float baseAngle = Mathf.Atan2(mesh.nodeZ[segA[cur]] - vz, mesh.nodeR[segA[cur]] - vr);
		float best = float.MaxValue;
		int result = -1;
		int s = nextFromNode[v];
		while (s >= 0)
		{
			if (from <= s && s < to0 && !used[s - from])
			{
				int w = segB[s];
				float angle = Mathf.Atan2(mesh.nodeZ[w] - vz, mesh.nodeR[w] - vr);
				float delta = angle - baseAngle;
				while (delta <= 0f)
				{
					delta += 6.28318548f;
				}
				while (delta > 6.28318548f)
				{
					delta += -6.28318548f;
				}
				if (delta < best)
				{
					best = delta;
					result = s;
				}
			}
			s = nextOutSeg[s];
		}
		return result;
	}

	public NativeArray<Vector2> BuildProfile(AxiMesh mesh, byte body, out int loopCount, bool allShells = false)
	{
		List<List<int>> loops = ((body != 0) ? targetLoops : projectileLoops);
		loopCount = loops.Count;
		if (loopCount <= 0)
		{
			return new NativeArray<Vector2>(0, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		}
		if (!allShells)
		{
			List<int> best = loops[0];
			float bestArea = -1f;
			for (int i = 0; i < loops.Count; i++)
			{
				List<int> loop = loops[i];
				float area2 = 0f;
				for (int j = 0; j < loop.Count; j++)
				{
					int p = loop[j];
					int q = loop[(j + 1) % loop.Count];
					area2 += mesh.nodeZ[q] * mesh.nodeR[p] - mesh.nodeZ[p] * mesh.nodeR[q];
				}
				if (0f > area2)
				{
					area2 = -area2;
				}
				if (area2 > bestArea)
				{
					bestArea = area2;
					best = loop;
				}
			}
			NativeArray<Vector2> result2 = new NativeArray<Vector2>(best.Count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
			for (int k = 0; k < best.Count; k++)
			{
				int n = best[k];
				float r = mesh.nodeR[n];
				if (0f > r)
				{
					r = 0f;
				}
				result2[k] = new Vector2(r, mesh.nodeZ[n]);
			}
			return result2;
		}
		List<List<int>> sorted = new List<List<int>>(loops);
		sorted.Sort((x, y) => MinZ(mesh, y).CompareTo(MinZ(mesh, x)));
		int total = 0;
		for (int l = 0; l < sorted.Count; l++)
		{
			total += sorted[l].Count;
		}
		NativeArray<Vector2> result = new NativeArray<Vector2>(total, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		int idx = 0;
		for (int m = 0; m < sorted.Count; m++)
		{
			List<int> loop2 = sorted[m];
			for (int j2 = 0; j2 < loop2.Count; j2++)
			{
				int n2 = loop2[j2];
				float r2 = mesh.nodeR[n2];
				if (0f > r2)
				{
					r2 = 0f;
				}
				result[idx++] = new Vector2(r2, mesh.nodeZ[n2]);
			}
		}
		return result;
	}

	private static float MinZ(AxiMesh mesh, List<int> loop)
	{
		float min = float.MaxValue;
		for (int i = 0; i < loop.Count; i++)
		{
			int n = loop[i];
			if (mesh.nodeZ[n] <= min && min != mesh.nodeZ[n])
			{
				min = mesh.nodeZ[n];
			}
		}
		return min;
	}

	public void Dispose()
	{
		if (segA.IsCreated)
		{
			segA.Dispose();
		}
		if (segB.IsCreated)
		{
			segB.Dispose();
		}
		if (segBodyTag.IsCreated)
		{
			segBodyTag.Dispose();
		}
		if (surfNodes.IsCreated)
		{
			surfNodes.Dispose();
		}
		if (nextFromNode.IsCreated)
		{
			nextFromNode.Dispose();
		}
		if (nextOutSeg.IsCreated)
		{
			nextOutSeg.Dispose();
		}
		if (nodeSeen.IsCreated)
		{
			nodeSeen.Dispose();
		}
	}
}
