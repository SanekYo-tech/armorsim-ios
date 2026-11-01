using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public static class SuperMeshVoxelizer
{
	public struct PartReport
	{
		public string name;

		public int triangles;

		public bool closed;

		public int boundaryEdges;

		public int nonManifoldEdges;

		public double volumeM3;

		public long particleEstimate;

		public double ambiguousFraction;

		public float reportSpacingM;

		public bool coarsened;

		public long insideCells;
	}

	public const double AmbiguousRefuse = 0.05;

	public const long MaxLatticeCells = 4000000000L;

	public const long MaxStoredCells = 200000000L;

	public const long MaxQuanta = 524288L;

	public static bool SabotageNondeterministic;

	public const string KeyLatticeCap = "super.s40.refuse.lattice_cap";

	public const string KeyExtent = "super.s40.refuse.model_extent";

	public const string KeyAmbiguous = "super.s40.refuse.ambiguous";

	public const long ReportCellCap = 8000000L;

	private static ModelImportException Fail(string key, string prose, params object[] args)
	{
		return new ModelImportException(key, args, prose);
	}

	public static SuperMeshVoxels Voxelize(IList<ModelPart> parts, Matrix4x4 modelToRegion, float s, float hu, float hv, float hn, bool refuseAmbiguous = true, Action<float> progress = null, CancellationToken ct = default(CancellationToken))
	{
		if (parts == null || parts.Count == 0)
		{
			throw Fail("model.import.empty", "Модель без частей.", "—");
		}
		if (s <= 0f)
		{
			throw new ArgumentException(string.Format("SuperMeshVoxelizer: шаг {0} не положителен.", s));
		}
		SuperMeshVoxels vox = new SuperMeshVoxels();
		vox.spacing = s;
		vox.hu = hu;
		vox.hv = hv;
		vox.hn = hn;
		vox.parts = new SuperMeshVoxels.Part[parts.Count];
		SuperClip.LatticeRange(hu, s, out vox.iMin, out vox.iMax);
		SuperClip.LatticeRange(hv, s, out vox.jMin, out vox.jMax);
		SuperClip.LatticeRange(hn, s, out vox.kMin, out vox.kMax);
		if (vox.NI < 1 || vox.NJ < 1 || vox.NK < 1)
		{
			throw new ArgumentException("SuperMeshVoxelizer: решётка области пуста.");
		}
		if (MaxLatticeCells < (long)vox.NK * vox.NJ * vox.NI)
		{
			throw Fail(KeyLatticeCap, string.Format("Решётка области {0}×{1}×{2} = {3:F0} млн ячеек при шаге ", vox.NI, vox.NJ, vox.NK, (double)vox.LatticeCells / 1000000.0) + string.Format("{0:F2} мм выше потолка {1:F0} млн — уменьши область или огрубь шаг.", s * 1000f, (double)MaxLatticeCells / 1000000.0), (double)vox.LatticeCells / 1000000.0, (double)MaxLatticeCells / 1000000.0, s * 1000f);
		}
		SuperClip clip = default(SuperClip);
		clip.hu = hu;
		clip.hv = hv;
		clip.hn = hn;
		long totalTriangles = 0L;
		int p = 0;
		while (true)
		{
			if (p >= parts.Count)
			{
				return vox;
			}
			if (p > 0)
			{
				SuperMeshVoxels.Part prev = vox.parts[p - 1];
				if (prev != null && prev.cells != null)
				{
					totalTriangles += prev.cells.Length / 3;
				}
			}
			ct.ThrowIfCancellationRequested();
			ModelPart mp = parts[p];
			SuperMeshVoxels.Part part = new SuperMeshVoxels.Part();
			part.name = mp.name;
			part.lostTo = new int[p];
			vox.parts[p] = part;
			int vertexCount = mp.vertices.Length;
			long[] Q = new long[vertexCount * 3];
			long minX = long.MaxValue;
			long minY = long.MaxValue;
			long minZ = long.MaxValue;
			long maxX = long.MinValue;
			long maxY = long.MinValue;
			long maxZ = long.MinValue;
			for (int vi = 0; vi < vertexCount; vi++)
			{
				Vector3 v = mp.vertices[vi];
				Vector3 w = modelToRegion.MultiplyPoint3x4(v);
				long qx = Quant((double)w.x, (double)s);
				long qy = Quant((double)w.y, (double)s);
				long qz = Quant((double)w.z, (double)s);
				if (Math.Abs(qx) > MaxQuanta || Math.Abs(qy) > MaxQuanta || Math.Abs(qz) > MaxQuanta)
				{
					long maxQ = Math.Max(Math.Abs(qx), Math.Max(Math.Abs(qy), Math.Abs(qz)));
					throw Fail(KeyExtent, string.Format("Часть «{0}» уходит от центра области дальше {1} квантов (шаг/4 = {2:F3} мм): ", mp.name, MaxQuanta, s * 250f) + string.Format("{0:F2} м. Огрубь шаг или приблизь область к модели.", (double)maxQ * ((double)s * 0.5)), maxQ, (double)MaxQuanta, s * 250f);
				}
				Q[vi * 3] = qx;
				Q[vi * 3 + 1] = qy;
				Q[vi * 3 + 2] = qz;
				if (qx < minX)
				{
					minX = qx;
				}
				if (maxX < qx)
				{
					maxX = qx;
				}
				if (qy < minY)
				{
					minY = qy;
				}
				if (maxY < qy)
				{
					maxY = qy;
				}
				if (qz < minZ)
				{
					minZ = qz;
				}
				if (maxZ < qz)
				{
					maxZ = qz;
				}
			}
			part.bi0 = Math.Max(vox.iMin, CeilDiv(minX - 2, 4));
			part.bi1 = Math.Min(vox.iMax, FloorDiv(maxX - 2, 4));
			part.bj0 = Math.Max(vox.jMin, CeilDiv(minY - 2, 4));
			part.bj1 = Math.Min(vox.jMax, FloorDiv(maxY - 2, 4));
			part.bk0 = Math.Max(vox.kMin, CeilDiv(minZ - 2, 4));
			part.bk1 = Math.Min(vox.kMax, FloorDiv(maxZ - 2, 4));
			int ni = Math.Max(0, part.bi1 - part.bi0 + 1);
			int nj = Math.Max(0, part.bj1 - part.bj0 + 1);
			int nk = Math.Max(0, part.bk1 - part.bk0 + 1);
			bool emptyBox = part.bi1 < part.bi0 || part.bj1 < part.bj0 || part.bk1 < part.bk0;
			int colXLen = emptyBox ? 0 : (nk * nj);
			part.colX = new int[1][] { new int[colXLen] };
			int colYLen = emptyBox ? 0 : (nk * ni);
			part.colY = new int[1][] { new int[colYLen] };
			int colZLen = emptyBox ? 0 : (nj * ni);
			part.colZ = new int[1][] { new int[colZLen] };
			if (!emptyBox)
			{
				int[] mins = new int[3] { part.bi0, part.bj0, part.bk0 };
				int[] maxs = new int[3] { part.bi1, part.bj1, part.bk1 };
				int[] cnt = new int[3] { ni, nj, nk };
				Toggles(Q, mp.triangles, 0, mins, maxs, cnt, part.colX);
				Toggles(Q, mp.triangles, 1, mins, maxs, cnt, part.colY);
				Toggles(Q, mp.triangles, 2, mins, maxs, cnt, part.colZ);
			}
			if (progress != null)
			{
				progress(((float)p + 0.5f) / (float)parts.Count);
			}
			List<int> cells = new List<int>();
			if (part.bi0 <= part.bi1 && part.bj0 <= part.bj1 && part.bk0 <= part.bk1)
			{
				for (int i = part.bi0; i <= part.bi1; i++)
				{
					if ((i - part.bi0 & 0xF) == 0)
					{
						ct.ThrowIfCancellationRequested();
					}
					for (int j = part.bj0; j <= part.bj1; j++)
					{
						int[] col = part.colZ[(i - part.bi0) * nj - part.bj0 + j];
						int n = (col != null) ? col.Length : 0;
						int par = 0;
						for (int k = part.bk0; k <= part.bk1; k++)
						{
							while (par < n && col[par] <= k)
							{
								par++;
							}
							int votes = SuperMeshVoxels.Parity(part.colX[(j - part.bj0) * nk - part.bk0 + k], i) + SuperMeshVoxels.Parity(part.colY[(k - part.bk0) * ni - part.bi0 + i], j) + (par & 1);
							if (votes == 0)
							{
								continue;
							}
							part.votedAny++;
							if (votes < 3)
							{
								part.ambiguous++;
							}
							if (votes < 2)
							{
								continue;
							}
							int owner = -1;
							for (int q = 0; q < p; q++)
							{
								if (vox.Votes(q, i, j, k) > 1)
								{
									part.lostTo[q]++;
									owner = q;
									break;
								}
							}
							if (owner >= 0)
							{
								continue;
							}
							if ((long)cells.Count / 3 + totalTriangles >= MaxStoredCells)
							{
								throw Fail(KeyLatticeCap, string.Format("Модель даёт больше {0:F0} млн ячеек при шаге {1:F2} мм — ", (double)((long)cells.Count / 3 + totalTriangles) / 1000000.0, s * 1000f) + "столько частиц не вместит ни одна видеокарта; уменьши область или огрубь шаг.", (double)((long)cells.Count / 3 + totalTriangles) / 1000000.0, (double)MaxStoredCells / 1000000.0, s * 1000f);
							}
							cells.Add(i);
							cells.Add(j);
							cells.Add(k);
							if (clip.CellLive(i, j, k, s))
							{
								part.live++;
							}
							else
							{
								part.band++;
							}
						}
					}
				}
			}
			if (part.bi0 <= part.bi1 && part.bj0 <= part.bj1 && part.bk0 <= part.bk1)
			{
				long streak = StreakBeyond(part.colX, part.bi1, vox.iMax) + StreakBeyond(part.colY, part.bj1, vox.jMax) + StreakBeyond(part.colZ, part.bk1, vox.kMax);
				part.votedAny += streak;
				part.ambiguous += streak;
			}
			part.cells = cells.ToArray();
			if (SabotageNondeterministic && part.cells.Length > 5)
			{
				int tri = part.cells.Length / 3;
				int[] shuffled = new int[part.cells.Length];
				int shift = UnityEngine.Random.Range(0, tri - 1);
				for (int t = 0; t < tri; t++)
				{
					int dst = (t + shift + 1) % tri;
					shuffled[dst * 3] = part.cells[t * 3];
					shuffled[dst * 3 + 1] = part.cells[t * 3 + 1];
					shuffled[dst * 3 + 2] = part.cells[t * 3 + 2];
				}
				part.cells = shuffled;
			}
			if (refuseAmbiguous && part.votedAny > 0 && (double)part.ambiguous / (double)part.votedAny > AmbiguousRefuse)
			{
				throw Fail(KeyAmbiguous, string.Format("Часть «{0}»: неоднозначных ячеек {1:P1} (из {2}) выше ", mp.name, part.AmbiguousFraction, part.votedAny) + string.Format("{0:P0} — лучи трёх осей не согласны, у части дыры или вывернутые грани.", AmbiguousRefuse), mp.name, part.AmbiguousFraction, part.votedAny, AmbiguousRefuse);
			}
			if (progress != null)
			{
				progress(((float)p + 1f) / (float)parts.Count);
			}
			p++;
		}
	}

	private static long StreakBeyond(int[][] cols, int bboxHi, int latHi)
	{
		if (cols == null || latHi <= bboxHi)
		{
			return 0L;
		}
		long n = 0L;
		for (int i = 0; i < cols.Length; i++)
		{
			if (cols[i] != null && (cols[i].Length & 1) != 0)
			{
				n += latHi - bboxHi;
			}
		}
		return n;
	}

	private static long Quant(double x, double q)
	{
		return (long)Math.Round(x / (q + q)) * 2 + 1;
	}

	private static int FloorDiv(long a, long b)
	{
		return (int)FloorDivL(a, b);
	}

	private static int CeilDiv(long a, long b)
	{
		return (int)(-FloorDivL(-a, b));
	}

	private static long FloorDivL(long a, long b)
	{
		long q = a / b;
		if (a % b != 0 && (a < 0) != (b < 0))
		{
			q--;
		}
		return q;
	}

	private static void Toggles(long[] Q, int[] tri, int a, int[] mins, int[] maxs, int[] cnt, int[][] cols)
	{
		int u = (a + 1) % 3;
		int v = (a + 2) % 3;
		List<int>[] scratch = new List<int>[cols.Length];
		for (int t = 0; t < tri.Length; t += 3)
		{
			long au = Q[tri[t] * 3 + u];
			long av = Q[tri[t] * 3 + v];
			long aa = Q[tri[t] * 3 + a];
			long bu = Q[tri[t + 1] * 3 + u];
			long bv = Q[tri[t + 1] * 3 + v];
			long ba = Q[tri[t + 1] * 3 + a];
			long cu = Q[tri[t + 2] * 3 + u];
			long cv = Q[tri[t + 2] * 3 + v];
			long ca = Q[tri[t + 2] * 3 + a];
			long w = (cv - av) * (bu - au) - (cu - au) * (bv - av);
			if (w == 0)
			{
				continue;
			}
			if (w < 0)
			{
				long tu = bu;
				long tv = bv;
				long ta = ba;
				bu = cu;
				bv = cv;
				ba = ca;
				cu = tu;
				cv = tv;
				ca = ta;
				w = -w;
			}
			long minU = Math.Min(au, Math.Min(bu, cu));
			long maxU = Math.Max(au, Math.Max(bu, cu));
			long minV = Math.Min(av, Math.Min(bv, cv));
			long maxV = Math.Max(av, Math.Max(bv, cv));
			int iLo = Math.Max(mins[u], CeilDiv(minU - 2, 4));
			int iHi = Math.Min(maxs[u], FloorDiv(maxU - 2, 4));
			int jLo = Math.Max(mins[v], CeilDiv(minV - 2, 4));
			int jHi = Math.Min(maxs[v], FloorDiv(maxV - 2, 4));
			if (iLo > iHi || jLo > jHi)
			{
				continue;
			}
			for (int i = iLo; i <= iHi; i++)
			{
				long pu = (long)i * 4 + 2;
				for (int j = jLo; j <= jHi; j++)
				{
					long pv = (long)j * 4 + 2;
					long c1 = (pv - av) * (bu - au) - (pu - au) * (bv - av);
					if (c1 > 0 || (c1 == 0 && (bv - av > 0 || (bv - av == 0 && bu - au < 0))))
					{
						long c2 = (pv - bv) * (cu - bu) - (pu - bu) * (cv - bv);
						if (c2 > 0 || (c2 == 0 && (cv - bv > 0 || (cv - bv == 0 && cu - bu < 0))))
						{
							long c3 = (pv - cv) * (au - cu) - (pu - cu) * (av - cv);
							if (c3 > 0 || (c3 == 0 && (av - cv > 0 || (av - cv == 0 && au - cu < 0))))
							{
								long num = c2 * aa + c3 * ba + c1 * ca - w - w;
								long ka = FloorDivL(num, w + w + w + w) + 1;
								if (ka <= maxs[a] + 1)
								{
									if (ka < mins[a])
									{
										ka = mins[a];
									}
									int idx = (i - mins[u]) * cnt[v] - mins[v] + j;
									if (scratch[idx] == null)
									{
										scratch[idx] = new List<int>(4);
									}
									scratch[idx].Add((int)ka);
								}
							}
						}
					}
				}
			}
		}
		for (int c = 0; c < scratch.Length; c++)
		{
			if (scratch[c] != null)
			{
				int[] col = scratch[c].ToArray();
				Array.Sort(col);
				cols[c] = col;
			}
		}
	}

	private static bool In(long w, long db, long dc)
	{
		if (w > 0)
		{
			return true;
		}
		if (w < 0)
		{
			return false;
		}
		if (dc > 0)
		{
			return true;
		}
		if (dc < 0)
		{
			return false;
		}
		return db < 0;
	}

	public static PartReport Report(ModelPart p, float sceneSpacing, CancellationToken ct = default(CancellationToken))
	{
		PartReport rep = default(PartReport);
		rep.name = p.name;
		rep.triangles = p.triangles.Length / 3;
		rep.closed = p.closed;
		rep.boundaryEdges = p.boundaryEdges;
		rep.nonManifoldEdges = p.nonManifoldEdges;
		rep.volumeM3 = p.volumeM3;
		double cellVolume = (double)sceneSpacing * sceneSpacing * sceneSpacing;
		rep.particleEstimate = (long)(Math.Abs(p.volumeM3) / cellVolume);
		float s = sceneSpacing;
		float ex = p.bounds.extents.x;
		float ey = p.bounds.extents.y;
		float ez = p.bounds.extents.z;
		double estimate = ((double)(ey + ey) * 1.5f + (double)(s * 4f)) * ((double)(ex + ex) * 1.5f + (double)(s * 4f)) * ((double)(ez + ez) * 1.5f + (double)(s * 4f)) / cellVolume;
		if ((double)ReportCellCap < estimate)
		{
			s = (float)(Math.Pow(estimate / (double)ReportCellCap, 1.0 / 3.0) * (double)s);
			rep.coarsened = true;
		}
		rep.reportSpacingM = s;
		Matrix4x4 toCenter = Matrix4x4.identity;
		toCenter.m03 = -p.bounds.center.x;
		toCenter.m13 = -p.bounds.center.y;
		toCenter.m23 = -p.bounds.center.z;
		SuperMeshVoxels vx = Voxelize(new ModelPart[1] { p }, toCenter, s, (ex + ex) * 0.75f + s, (ey + ey) * 0.75f + s, (ez + ez) * 0.75f + s, false, null, ct);
		SuperMeshVoxels.Part part = vx.parts[0];
		rep.ambiguousFraction = ((part.votedAny > 0) ? ((double)part.ambiguous / (double)part.votedAny) : 0.0);
		rep.insideCells = ((part.cells != null) ? (part.cells.Length / 3) : 0);
		return rep;
	}
}
