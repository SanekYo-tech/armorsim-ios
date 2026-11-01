using System;
using System.Collections.Generic;

public sealed class CraterProfile
{
	public float h;

	public int kMin;

	public int kMax;

	public float[] radius;

	public bool[] hasMaterial;

	public float floorDepthM;

	public float maxRadiusM;

	public float maxRadiusAtZM;

	public float frontMostZM;

	public int componentCount;

	public float detachedMassKg;

	public int detachedElements;

	public bool floorSaturated;

	public float LipM => frontMostZM < 0f ? 0f - frontMostZM : 0f;

	public float ZOf(int k)
	{
		return k * h;
	}

	public static CraterProfile Measure(AxiMesh m, float elementSizeM)
	{
		CraterProfile result = new CraterProfile
		{
			h = elementSizeM
		};
		int[] parent = new int[m.elemCount];
		for (int i = 0; i < m.elemCount; i++)
		{
			parent[i] = i;
		}
		Dictionary<long, int> edgeElements = new Dictionary<long, int>(m.elemCount * 2);
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemAlive[e] == 0 || m.elemBody[e] != 1)
			{
				continue;
			}
			for (int c = 0; c < 4; c++)
			{
				int a = m.elemNodes[e * 4 + c];
				int b = m.elemNodes[e * 4 + (c + 1) % 4];
				long key = (long)Math.Min(a, b) * m.nodeCount + Math.Max(a, b);
				if (!edgeElements.TryGetValue(key, out int other))
				{
					edgeElements[key] = e;
				}
				else
				{
					Union(parent, e, other);
				}
			}
		}
		HashSet<int> components = new HashSet<int>();
		int attachedRoot = -1;
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemAlive[e] == 0 || m.elemBody[e] != 1)
			{
				continue;
			}
			int root = Find(parent, e);
			components.Add(root);
			if (attachedRoot < 0)
			{
				for (int c = 0; c < 4; c++)
				{
					if (m.nodeFixed[m.elemNodes[e * 4 + c]] != 0)
					{
						attachedRoot = root;
						break;
					}
				}
			}
		}
		result.componentCount = components.Count;
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemAlive[e] != 0 && m.elemBody[e] == 1 && Find(parent, e) != attachedRoot)
			{
				result.detachedElements++;
				result.detachedMassKg += m.elemMass[e];
			}
		}
		bool[] nodes = new bool[m.nodeCount];
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemAlive[e] == 0 || m.elemBody[e] != 1)
			{
				continue;
			}
			if (attachedRoot < 0 || Find(parent, e) == attachedRoot)
			{
				for (int c = 0; c < 4; c++)
				{
					nodes[m.elemNodes[e * 4 + c]] = true;
				}
			}
		}
		float minZ = float.MaxValue;
		float maxZ = float.MinValue;
		int nodeTotal = 0;
		for (int i = 0; i < m.nodeCount; i++)
		{
			if (nodes[i])
			{
				nodeTotal++;
				minZ = Math.Min(minZ, m.nodeZ[i]);
				maxZ = Math.Max(maxZ, m.nodeZ[i]);
			}
		}
		if (nodeTotal == 0)
		{
			result.radius = new float[0];
			result.hasMaterial = new bool[0];
			return result;
		}
		result.frontMostZM = minZ;
		result.kMin = Band(minZ, result.h);
		result.kMax = Band(maxZ, result.h);
		int bandCount = result.kMax - result.kMin + 1;
		result.radius = new float[bandCount];
		result.hasMaterial = new bool[bandCount];
		for (int i = 0; i < bandCount; i++)
		{
			result.radius[i] = float.MaxValue;
		}
		for (int i = 0; i < m.nodeCount; i++)
		{
			if (!nodes[i])
			{
				continue;
			}
			int band = Band(m.nodeZ[i], result.h) - result.kMin;
			if (band < 0 || band >= bandCount)
			{
				continue;
			}
			result.hasMaterial[band] = true;
			result.radius[band] = Math.Min(result.radius[band], m.nodeR[i]);
		}
		for (int i = 0; i < bandCount; i++)
		{
			if (result.hasMaterial[i])
			{
				if (result.radius[i] <= m.axisTol)
				{
					result.radius[i] = 0f;
				}
			}
			else
			{
				result.radius[i] = 0f;
			}
		}
		int lastMaterial = int.MinValue;
		int lastPositive = int.MinValue;
		for (int i = 0; i < bandCount; i++)
		{
			if (result.hasMaterial[i])
			{
				lastMaterial = i;
				if (result.radius[i] > 0f)
				{
					lastPositive = i;
				}
			}
		}
		if (lastPositive != int.MinValue)
		{
			result.floorDepthM = (result.kMin + lastPositive) * result.h;
			result.floorSaturated = lastPositive == lastMaterial;
		}
		else
		{
			result.floorDepthM = 0f;
		}
		for (int i = 0; i < bandCount; i++)
		{
			float z = (result.kMin + i) * result.h;
			if (z > 0f && result.hasMaterial[i] && result.radius[i] > result.maxRadiusM)
			{
				result.maxRadiusM = result.radius[i];
				result.maxRadiusAtZM = z;
			}
		}
		return result;
	}

	private static int Band(float z, float h)
	{
		return (int)Math.Floor(z / h + 0.5f);
	}

	private static int Find(int[] parent, int x)
	{
		while (parent[x] != x)
		{
			parent[x] = parent[parent[x]];
			x = parent[x];
		}
		return x;
	}

	private static void Union(int[] parent, int a, int b)
	{
		a = Find(parent, a);
		b = Find(parent, b);
		if (a != b)
		{
			parent[b] = a;
		}
	}
}
