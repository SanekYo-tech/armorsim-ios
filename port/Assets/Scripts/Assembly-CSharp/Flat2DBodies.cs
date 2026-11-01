using System;
using System.Collections.Generic;
using UnityEngine;

public static class Flat2DBodies
{
	public static int Label(Vector2[] pos, double linkR, out int[] rank, uint[] mustMatch = null)
	{
		int n = pos.Length;
		int[] lab = new int[n];
		for (int i = 0; i < n; i++)
		{
			lab[i] = i;
		}

		int Find(int x)
		{
			while (lab[x] != x)
			{
				lab[x] = lab[lab[x]];
				x = lab[x];
			}
			return x;
		}

		Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();
		float cell = (float)linkR;
		for (int i = 0; i < n; i++)
		{
			int cx = (int)Math.Round(pos[i].x / cell);
			int cy = (int)Math.Round(pos[i].y / cell);
			long key = ((long)cx << 32) ^ (uint)cy;
			if (!grid.TryGetValue(key, out List<int> list))
			{
				list = new List<int>();
				grid[key] = list;
			}
			list.Add(i);
		}

		double link2 = linkR * linkR;
		for (int i = 0; i < n; i++)
		{
			int cx = (int)Math.Round(pos[i].x / cell);
			int cy = (int)Math.Round(pos[i].y / cell);
			for (int ox = -1; ox <= 1; ox++)
			{
				for (int oy = -1; oy <= 1; oy++)
				{
					long key = ((long)(cx + ox) << 32) ^ (uint)(cy + oy);
					if (!grid.TryGetValue(key, out List<int> list))
					{
						continue;
					}
					foreach (int j in list)
					{
						if (j <= i || (mustMatch != null && mustMatch[i] != mustMatch[j]))
						{
							continue;
						}
						double dx = pos[i].x - pos[j].x;
						double dy = pos[i].y - pos[j].y;
						if (dx * dx + dy * dy < link2)
						{
							int a = Find(i);
							int b = Find(j);
							if (a != b)
							{
								lab[a] = b;
							}
						}
					}
				}
			}
		}

		Dictionary<int, int> size = new Dictionary<int, int>();
		Dictionary<int, int> first = new Dictionary<int, int>();
		for (int i = 0; i < n; i++)
		{
			int root = Find(i);
			size.TryGetValue(root, out int count);
			size[root] = count + 1;
			if (!first.ContainsKey(root))
			{
				first[root] = i;
			}
		}

		List<int> roots = new List<int>(size.Keys);
		roots.Sort(delegate(int a, int b)
		{
			int bySize = size[b].CompareTo(size[a]);
			return bySize != 0 ? bySize : first[a].CompareTo(first[b]);
		});
		Dictionary<int, int> remap = new Dictionary<int, int>();
		for (int i = 0; i < roots.Count; i++)
		{
			remap[roots[i]] = i;
		}
		rank = new int[n];
		for (int i = 0; i < n; i++)
		{
			rank[i] = remap[Find(i)];
		}
		return roots.Count;
	}

	public static (int, int) Count(Vector2[] pos, double linkR)
	{
		int bodies = Label(pos, linkR, out int[] rank);
		int singletons = 0;
		for (int i = 0; i < rank.Length; i++)
		{
			if (rank[i] == 0)
			{
				singletons++;
			}
		}
		return (bodies, singletons);
	}

	public static uint[] Assign(Vector2[] pos, byte[] ownerRole, float dx)
	{
		if (pos == null || ownerRole == null || ownerRole.Length != pos.Length)
		{
			throw new InvalidOperationException("Flat2DBodies.Assign: нет роли владельца на каждый узел (заливка не через Flat2DDrawModel.BuildFill?)");
		}
		double link = 2.0 * (dx * 1.3f);
		List<int> armour = new List<int>();
		List<int> projectile = new List<int>();
		for (int i = 0; i < pos.Length; i++)
		{
			if (ownerRole[i] == 0)
			{
				armour.Add(i);
			}
			else if (ownerRole[i] == 1)
			{
				projectile.Add(i);
			}
			else
			{
				throw new InvalidOperationException(string.Format("Flat2DBodies.Assign: узел {0} без владельца (роль {1})", i, ownerRole[i]));
			}
		}
		uint[] ids = new uint[pos.Length];
		uint offset = (uint)LabelSubset(pos, armour, link, ids, 0u);
		LabelSubset(pos, projectile, link, ids, offset);
		return ids;
	}

	private static int LabelSubset(Vector2[] pos, List<int> idx, double link, uint[] ids, uint offset)
	{
		if (idx.Count == 0)
		{
			return 0;
		}
		Vector2[] subset = new Vector2[idx.Count];
		for (int i = 0; i < idx.Count; i++)
		{
			subset[i] = pos[idx[i]];
		}
		int bodies = Label(subset, link, out int[] rank);
		for (int i = 0; i < idx.Count; i++)
		{
			ids[idx[i]] = (uint)rank[i] + offset;
		}
		return bodies;
	}
}
