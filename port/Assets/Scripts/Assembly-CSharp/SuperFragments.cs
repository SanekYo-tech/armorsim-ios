using System.Collections.Generic;
using UnityEngine;

public static class SuperFragments
{
	public sealed class Piece
	{
		public List<int> particles;

		public double massKg;

		public Vector3 centroid;

		public Vector3 meanVelocity;

		public Piece()
		{
			particles = new List<int>();
		}
	}

	public const float LinkFactor = 1.45f;

	public static List<Piece> Split(Vector3[] pos, Vector3[] vel, float[] mass, int start, int count, float spacingM, float[] damage = null)
	{
		bool Cracked(int i)
		{
			return damage != null && damage[i] >= 1f;
		}
		float cell = spacingM * LinkFactor;
		float cellSq = cell * cell;
		Dictionary<long, List<int>> cells = new Dictionary<long, List<int>>();
		for (int i = start; i < start + count; i++)
		{
			long key = Key(pos[i], cell);
			if (!cells.TryGetValue(key, out var list))
			{
				list = new List<int>();
				cells[key] = list;
			}
			list.Add(i);
		}
		Dictionary<int, int> seen = new Dictionary<int, int>();
		List<Piece> pieces = new List<Piece>();
		Stack<int> stack = new Stack<int>();
		for (int j = start; j < start + count; j++)
		{
			if (seen.ContainsKey(j) || Cracked(j))
			{
				continue;
			}
			Piece piece = new Piece();
			int idx = pieces.Count;
			pieces.Add(piece);
			stack.Push(j);
			seen[j] = idx;
			while (stack.Count > 0)
			{
				int p = stack.Pop();
				piece.particles.Add(p);
				int bx = (int)(pos[p].x / cell);
				int by = (int)(pos[p].y / cell);
				int bz = (int)(pos[p].z / cell);
				for (int dz = -1; dz <= 1; dz++)
				{
					for (int dy = -1; dy <= 1; dy++)
					{
						for (int dx = -1; dx <= 1; dx++)
						{
							long nk = (long)((bx + dx) & 0x1FFFFF) << 42 | (long)((by + dy) & 0x1FFFFF) << 21 | (long)((bz + dz) & 0x1FFFFF);
							if (!cells.TryGetValue(nk, out var cellList))
							{
								continue;
							}
							foreach (int n in cellList)
							{
								if (seen.ContainsKey(n) || Cracked(n))
								{
									continue;
								}
								Vector3 d = pos[n] - pos[p];
								if (d.x * d.x + d.y * d.y + d.z * d.z > cellSq)
								{
									continue;
								}
								seen[n] = idx;
								stack.Push(n);
							}
						}
					}
				}
			}
		}
		foreach (Piece piece2 in pieces)
		{
			RadarKahan m = new RadarKahan();
			RadarKahan vx = new RadarKahan();
			RadarKahan vy = new RadarKahan();
			RadarKahan vz = new RadarKahan();
			RadarKahan cx = new RadarKahan();
			RadarKahan cy = new RadarKahan();
			RadarKahan cz = new RadarKahan();
			foreach (int n2 in piece2.particles)
			{
				float w = mass[n2];
				m.Add(w);
				vx.Add((double)vel[n2].x * w);
				vy.Add((double)vel[n2].y * w);
				vz.Add((double)vel[n2].z * w);
				cx.Add((double)pos[n2].x * w);
				cy.Add((double)pos[n2].y * w);
				cz.Add((double)pos[n2].z * w);
			}
			piece2.massKg = m.Value;
			double inv = ((m.Value > 1e-30) ? (1.0 / m.Value) : 0.0);
			piece2.centroid = new Vector3((float)(cx.Value * inv), (float)(cy.Value * inv), (float)(cz.Value * inv));
			piece2.meanVelocity = new Vector3((float)(vx.Value * inv), (float)(vy.Value * inv), (float)(vz.Value * inv));
		}
		pieces.Sort(delegate(Piece a, Piece b)
		{
			return b.massKg.CompareTo(a.massKg);
		});
		return pieces;
	}

	private static long Key(Vector3 p, float cell)
	{
		long k = (long)(int)(p.x / cell) & 0x1FFFFF;
		k = (k << 21) | ((long)(int)(p.y / cell) & 0x1FFFFF);
		k = (k << 21) | ((long)(int)(p.z / cell) & 0x1FFFFF);
		return k;
	}
}
