using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public static class SuperMarchingTets
{
	public sealed class Result
	{
		public List<Vector3> vertices = new List<Vector3>();

		public List<int> triangles = new List<int>();

		public List<long> keys;

		public Result()
		{
		}
	}

	public sealed class Slab
	{
		public long[] keys;

		public List<Vector3> v;

		public List<int> t;

		public Slab()
		{
		}
	}

	public sealed class Weld
	{
		private readonly Dictionary<long, int> dict;

		private int[] slot;

		private long[] slotKey;

		private readonly List<int> used = new List<int>();

		private int i0;

		private int j0;

		private int w;

		private int h;

		private int kBase;

		private int nx;

		private int nxy;

		public bool dense => dict == null;

		public int Count
		{
			get
			{
				if (dict != null)
				{
					return dict.Count;
				}
				return used.Count;
			}
		}

		public long Bytes
		{
			get
			{
				if (dict != null)
				{
					return (long)dict.Count * WeldEntryBytes;
				}
				int slotLen = (slot != null) ? slot.Length : 0;
				int slotKeyLen = (slotKey != null) ? slotKey.Length : 0;
				return ((long)used.Capacity + (long)slotKeyLen * 2 + slotLen) * 4;
			}
		}

		public Weld()
		{
			dict = new Dictionary<long, int>();
		}

		public Weld(int nx, int nxy, int kBase, int i0, int j0, int w, int h)
		{
			Reuse(nx, nxy, kBase, i0, j0, w, h);
		}

		public void Reuse(int nx, int nxy, int kBase, int i0, int j0, int w, int h)
		{
			this.i0 = i0;
			this.nx = nx;
			this.j0 = j0;
			this.w = w;
			this.nxy = nxy;
			this.kBase = kBase;
			this.h = h;
			int n = w * h * 10;
			if (slot == null || slot.Length < n)
			{
				slot = new int[n];
				for (int i = 0; i < n; i++)
				{
					slot[i] = -1;
				}
				slotKey = (VerifyWeld ? new long[n] : null);
				used.Clear();
			}
			else
			{
				for (int i = 0; i < used.Count; i++)
				{
					slot[used[i]] = -1;
				}
				used.Clear();
				if (VerifyWeld && (slotKey == null || slotKey.Length < n))
				{
					slotKey = new long[n];
				}
			}
		}

		private int SlotOf(int ga, int gb)
		{
			int d = gb - ga;
			int dz = d / nxy;
			int rem = d % nxy;
			int dy = rem / nx;
			int dx = rem % nx;
			int kLayer = ga / nxy - kBase;
			if ((uint)kLayer > 1)
			{
				return -1;
			}
			int remA = ga % nxy;
			int jA = remA / nx;
			int iA = remA % nx;
			if (iA < i0)
			{
				return -1;
			}
			if (jA < j0)
			{
				return -1;
			}
			int i = iA - i0;
			if (i >= w)
			{
				return -1;
			}
			int j = jA - j0;
			if (j >= h)
			{
				return -1;
			}
			int dir = ((kLayer == 0) ? (dx + 2 * dy + 4 * dz - 1) : (dx + 2 * dy + 6));
			if (SabotageWeldDirSwap && dir == 4)
			{
				dir = 2;
			}
			if (dir > 9)
			{
				return -1;
			}
			int idx = dir + (j * w + i) * 10;
			if (idx < 0 || idx >= slot.Length)
			{
				return -1;
			}
			return idx;
		}

		public bool TryGet(long key, int ga, int gb, out int idx)
		{
			if (dict == null)
			{
				int s = SlotOf(ga, gb);
				if (s < 0)
				{
					throw new InvalidOperationException(string.Format("сварка рёбер: ребро {0}→{1} не легло в таблицу слоя {2} ", ga, gb, kBase) + "(нумерация 7+3 неполна или коробка пролёта мала) — это дефект, а не редкий случай");
				}
				if (slotKey != null && slot[s] >= 0 && slotKey[s] != key)
				{
					throw new InvalidOperationException(string.Format("сварка рёбер: ячейка {0} занята ключом {1}, пришёл {2} — ", s, slotKey[s], key) + "два РАЗНЫХ ребра делят одно место, нумерация направлений неполна");
				}
				idx = slot[s];
				return idx >= 0;
			}
			return dict.TryGetValue(key, out idx);
		}

		public void Set(long key, int ga, int gb, int idx)
		{
			if (dict == null)
			{
				int s = SlotOf(ga, gb);
				if (s < 0)
				{
					throw new InvalidOperationException(string.Format("сварка рёбер: ребру {0}→{1} нет места в таблице слоя {2}", ga, gb, kBase));
				}
				slot[s] = idx;
				if (slotKey != null)
				{
					slotKey[s] = key;
				}
				used.Add(s);
			}
			else
			{
				dict[key] = idx;
			}
		}
	}

	public sealed class RowSpans
	{
		public readonly int[] lo;

		public readonly int[] hi;

		public readonly int ny;

		public readonly int nz;

		public long Bytes => (long)(lo.Length + hi.Length) * 4;

		public RowSpans(int ny, int nz)
		{
			this.ny = ny;
			this.nz = nz;
			lo = new int[ny * nz];
			hi = new int[ny * nz];
			for (int i = 0; i < lo.Length; i++)
			{
				lo[i] = int.MaxValue;
				hi[i] = -1;
			}
		}

		public void Touch(int j, int k, int i0, int i1)
		{
			if (j < 0 || k < 0 || j >= ny || k >= nz)
			{
				return;
			}
			int row = k * ny + j;
			if (i0 < lo[row])
			{
				lo[row] = i0;
			}
			if (i1 > hi[row])
			{
				hi[row] = i1;
			}
		}
	}

	public sealed class Bytes
	{
		public long peak;

		public long weld;

		public Bytes()
		{
		}
	}

	public sealed class SlabMerge
	{
		public readonly Result res = new Result();

		private Dictionary<long, int> carry = new Dictionary<long, int>();

		public long liveSlabBytes;

		public SlabMerge()
		{
		}

		public void Append(Slab[] slabs, int kFrom, int kTo, int nxy, Bytes bytes)
		{
			if (bytes != null)
			{
				for (int i = 0; i < slabs.Length; i++)
				{
					liveSlabBytes += SlabBytes(slabs[i]);
				}
				long mergeBytes = bytes.weld + ((long)res.vertices.Capacity * 3 + res.triangles.Capacity) * 4 + liveSlabBytes;
				if (mergeBytes > bytes.peak)
				{
					bytes.peak = mergeBytes;
				}
			}
			for (int k = kFrom; k < kTo; k++)
			{
				Slab s = slabs[k - kFrom];
				int[] remap = new int[s.t.Count];
				Dictionary<long, int> next = new Dictionary<long, int>();
				for (int j = 0; j < s.t.Count; j++)
				{
					long key = s.keys[j];
					if (!carry.TryGetValue(key, out int idx))
					{
						idx = res.vertices.Count;
						res.vertices.Add(s.v[j]);
					}
					remap[j] = idx;
					if ((int)(key >> 32) / nxy == k + 1 && (int)key / nxy == k + 1)
					{
						next[key] = idx;
					}
				}
				foreach (int t in s.t)
				{
					res.triangles.Add(remap[t]);
				}
				carry = next;
				if (bytes != null)
				{
					liveSlabBytes -= SlabBytes(s);
					long b = liveSlabBytes + ((long)(carry.Count + remap.Length) * 7 + (long)res.vertices.Capacity * 3 + res.triangles.Capacity) * 4;
					if (b > bytes.peak)
					{
						bytes.peak = b;
					}
				}
				slabs[k - kFrom] = null;
			}
		}

		public Result Finish()
		{
			if (SabotageSwapMergedVertices && res.vertices.Count >= 2)
			{
				int last = res.vertices.Count - 1;
				Vector3 a = res.vertices[0];
				Vector3 b = res.vertices[last];
				res.vertices[0] = b;
				res.vertices[last] = a;
			}
			return res;
		}
	}

	private static readonly int[,] CornerOffset;

	private static readonly int[,] Tets;

	public static bool SabotageSwapMergedVertices;

	public const int DenseWeldCells = 4000000;

	public static int ForceWeldForm;

	public static bool SabotageWeldDirSwap;

	public static bool VerifyWeld;

	public static int DenseSlabs;

	public static int DictSlabs;

	public static bool SabotageSpanShrink;

	public const int SabotageSpanShrinkCubes = 3;

	private const int WeldEntryBytes = 28;

	static SuperMarchingTets()
	{
		CornerOffset = new int[8, 3]
		{
			{ 0, 0, 0 },
			{ 1, 0, 0 },
			{ 0, 1, 0 },
			{ 1, 1, 0 },
			{ 0, 0, 1 },
			{ 1, 0, 1 },
			{ 0, 1, 1 },
			{ 1, 1, 1 }
		};
		Tets = new int[6, 4]
		{
			{ 0, 1, 3, 7 },
			{ 0, 3, 2, 7 },
			{ 0, 2, 6, 7 },
			{ 0, 6, 4, 7 },
			{ 0, 4, 5, 7 },
			{ 0, 5, 1, 7 }
		};
		SabotageSwapMergedVertices = false;
		ForceWeldForm = 0;
		SabotageWeldDirSwap = false;
		VerifyWeld = false;
		SabotageSpanShrink = false;
	}

	public static Result Polygonize(float[] field, int nx, int ny, int nz, Vector3 lo, float cell, float iso)
	{
		Result r = new Result();
		Weld weld = new Weld();
		int[] cornerId = new int[8];
		float[] cornerV = new float[8];
		int[] tv = new int[4];
		for (int k = 0; k < nz - 1; k++)
		{
			ScanSlab(r, weld, k, field, 0, nx, ny, nx * ny, lo, cell, iso, cornerId, cornerV, tv, null);
		}
		return r;
	}

	private static void ScanSlab(Result r, Weld weld, int k, float[] field, int baseOffset, int nx, int ny, int nxy, Vector3 lo, float cell, float iso, int[] cornerId, float[] cornerV, int[] tv, RowSpans spans = null)
	{
		for (int j = 0; j < ny - 1; j++)
		{
			int from = 0;
			int to = nx - 2;
			if (spans != null)
			{
				if (!CubeSpan(spans, k, j, nx, out from, out to))
				{
					continue;
				}
			}
			if (from > to)
			{
				continue;
			}
			for (int i = from; i <= to; i++)
			{
				bool low = false;
				bool high = false;
				for (int c = 0; c < 8; c++)
				{
					int id = i + CornerOffset[c, 0] + (j + CornerOffset[c, 1]) * nx + (k + CornerOffset[c, 2]) * nxy;
					cornerId[c] = id;
					float v = field[id - baseOffset];
					cornerV[c] = v;
					if (v >= iso)
					{
						high = true;
					}
					else
					{
						low = true;
					}
				}
				if (!high || !low)
				{
					continue;
				}
				for (int t = 0; t < 6; t++)
				{
					for (int d = 0; d < 4; d++)
					{
						tv[d] = Tets[t, d];
					}
					EmitTet(r, weld, cornerId, cornerV, tv, field, nx, nxy, lo, cell, iso);
				}
			}
		}
	}

	public static void ResetWeldCounters()
	{
		DenseSlabs = 0;
		DictSlabs = 0;
	}

	private static bool CubeSpan(RowSpans s, int k, int j, int nx, out int from, out int to)
	{
		int row = k * s.ny + j;
		int rowUp = row + s.ny;
		int a = Math.Min(Math.Min(s.lo[row], s.lo[row + 1]), Math.Min(s.lo[rowUp], s.lo[rowUp + 1]));
		int b = Math.Max(Math.Max(s.hi[row], s.hi[row + 1]), Math.Max(s.hi[rowUp], s.hi[rowUp + 1]));
		if (b < 0)
		{
			from = 0;
			to = -1;
			return false;
		}
		int shrink = SabotageSpanShrink ? SabotageSpanShrinkCubes : 0;
		from = Math.Max(0, a - 1 + shrink);
		to = Math.Min(nx - 2, b - shrink);
		return from <= to;
	}

	private static long SlabBytes(Slab s)
	{
		if (s == null)
		{
			return 0L;
		}
		return ((long)s.v.Capacity * 3 + (long)s.keys.Length * 2 + s.t.Capacity) * 4;
	}

	public static Slab[] BuildSlabs(float[] field, int baseOffset, int kFrom, int kTo, int nx, int ny, Vector3 lo, float cell, float iso, int dop, Action onSlab, Bytes bytes, RowSpans spans = null)
	{
		int nxy = ny * nx;
		int nSlabs = Math.Max(0, kTo - kFrom);
		Slab[] slabs = new Slab[nSlabs];
		Stack<Weld> pool = new Stack<Weld>();
		object poolLock = new object();
		int maxWeld = 0;
		long weldBytesMax = 0;
		Action<int> action = delegate(int w)
		{
			int k = kFrom + w;
			Result r = new Result();
			r.keys = new List<long>();
			Weld weld = RentWeld(pool, poolLock, spans, k, nx, ny, nxy);
			int[] cornerId = new int[8];
			float[] cornerV = new float[8];
			int[] tv = new int[4];
			ScanSlab(r, weld, k, field, baseOffset, nx, ny, nxy, lo, cell, iso, cornerId, cornerV, tv, spans);
			Slab slab = new Slab();
			slab.keys = r.keys.ToArray();
			slab.v = r.vertices;
			slab.t = r.triangles;
			slabs[w] = slab;
			if (bytes != null)
			{
				int cnt = weld.Count;
				while (cnt > maxWeld)
				{
					int cur = maxWeld;
					if (Interlocked.CompareExchange(ref maxWeld, cnt, cur) == cur)
					{
						break;
					}
				}
				long wb = weld.Bytes;
				while (wb > weldBytesMax)
				{
					long cur2 = weldBytesMax;
					if (Interlocked.CompareExchange(ref weldBytesMax, wb, cur2) == cur2)
					{
						break;
					}
				}
			}
			lock (poolLock)
			{
				pool.Push(weld);
			}
			onSlab?.Invoke();
		};
		if (dop > 1)
		{
			SuperParallel.For(0, nSlabs, dop, action);
		}
		else
		{
			for (int w = 0; w < nSlabs; w++)
			{
				action(w);
			}
		}
		if (bytes != null)
		{
			bytes.weld = Math.Max(bytes.weld, Math.Max(weldBytesMax, (long)maxWeld * WeldEntryBytes) * Math.Max(1, dop));
		}
		return slabs;
	}

	private static Weld RentWeld(Stack<Weld> pool, object poolLock, RowSpans spans, int k, int nx, int ny, int nxy)
	{
		if (ForceWeldForm == 1)
		{
			Interlocked.Increment(ref DictSlabs);
			return new Weld();
		}
		int i0 = 0;
		int j0 = 0;
		int w = nx;
		int h = ny;
		if (ForceWeldForm != 2)
		{
			if (spans != null)
			{
				int iMin = int.MaxValue;
				int iMax = -1;
				int jMin = int.MaxValue;
				int jMax = -1;
				for (int j = 0; j < ny - 1; j++)
				{
					if (CubeSpan(spans, k, j, nx, out int from, out int to))
					{
						if (from < iMin)
						{
							iMin = from;
						}
						if (to > iMax)
						{
							iMax = to;
						}
					}
					if (j < jMin)
					{
						jMin = j;
					}
					if (j > jMax)
					{
						jMax = j;
					}
				}
				if (iMax < 0)
				{
					i0 = 0;
					j0 = 0;
					w = 1;
					h = 1;
				}
				else
				{
					i0 = iMin;
					j0 = jMin;
					w = iMax - iMin + 2;
					h = jMax - jMin + 2;
				}
			}
			else if (nx * ny * 10 > DenseWeldCells)
			{
				Interlocked.Increment(ref DictSlabs);
				return new Weld();
			}
			if (w * h * 10 > DenseWeldCells)
			{
				Interlocked.Increment(ref DictSlabs);
				return new Weld();
			}
		}
		Interlocked.Increment(ref DenseSlabs);
		Weld weld = null;
		lock (poolLock)
		{
			if (pool.Count > 0)
			{
				weld = pool.Pop();
			}
		}
		if (weld != null && weld.dense)
		{
			weld.Reuse(nx, nxy, k, i0, j0, w, h);
			return weld;
		}
		return new Weld(nx, nxy, k, i0, j0, w, h);
	}

	public static Result PolygonizeSlabs(float[] field, int nx, int ny, int nz, Vector3 lo, float cell, float iso, int dop, Action onSlab = null, Bytes bytes = null, RowSpans spans = null)
	{
		int kTo = Math.Max(0, nz - 1);
		Slab[] slabs = BuildSlabs(field, 0, 0, kTo, nx, ny, lo, cell, iso, dop, onSlab, bytes, spans);
		SlabMerge merge = new SlabMerge();
		merge.Append(slabs, 0, kTo, nx * ny, bytes);
		return merge.Finish();
	}

	private static void EmitTet(Result r, Weld weld, int[] cid, float[] cv, int[] tv, float[] field, int nx, int nxy, Vector3 lo, float cell, float iso)
	{
		Span<int> pos = stackalloc int[4];
		Span<int> neg = stackalloc int[4];
		int npos = 0;
		int nneg = 0;
		for (int i = 0; i < 4; i++)
		{
			int c = tv[i];
			if (cv[c] >= iso)
			{
				pos[npos++] = c;
			}
			else
			{
				neg[nneg++] = c;
			}
		}
		if (npos == 0 || npos == 4)
		{
			return;
		}
		Vector3 posSum = Vector3.zero;
		Vector3 negSum = Vector3.zero;
		for (int i = 0; i < npos; i++)
		{
			posSum += NodePos(cid[pos[i]], nx, nxy, lo, cell);
		}
		for (int i = 0; i < nneg; i++)
		{
			negSum += NodePos(cid[neg[i]], nx, nxy, lo, cell);
		}
		Vector3 outward = new Vector3(negSum.x / nneg - posSum.x / npos, negSum.y / nneg - posSum.y / npos, negSum.z / nneg - posSum.z / npos);
		if (npos == 1)
		{
			int a = Cut(r, weld, cid, cv, pos[0], neg[0], nx, nxy, lo, cell, iso);
			int b = Cut(r, weld, cid, cv, pos[0], neg[1], nx, nxy, lo, cell, iso);
			int c = Cut(r, weld, cid, cv, pos[0], neg[2], nx, nxy, lo, cell, iso);
			AddTri(r, a, b, c, outward);
		}
		else if (npos == 3)
		{
			int a = Cut(r, weld, cid, cv, pos[0], neg[0], nx, nxy, lo, cell, iso);
			int b = Cut(r, weld, cid, cv, pos[1], neg[0], nx, nxy, lo, cell, iso);
			int c = Cut(r, weld, cid, cv, pos[2], neg[0], nx, nxy, lo, cell, iso);
			AddTri(r, a, b, c, outward);
		}
		else
		{
			int a = Cut(r, weld, cid, cv, pos[0], neg[0], nx, nxy, lo, cell, iso);
			int b = Cut(r, weld, cid, cv, pos[0], neg[1], nx, nxy, lo, cell, iso);
			int c = Cut(r, weld, cid, cv, pos[1], neg[1], nx, nxy, lo, cell, iso);
			int d = Cut(r, weld, cid, cv, pos[1], neg[0], nx, nxy, lo, cell, iso);
			AddTri(r, a, b, c, outward);
			AddTri(r, a, c, d, outward);
		}
	}

	private static Vector3 NodePos(int id, int nx, int nxy, Vector3 lo, float cell)
	{
		int k = id / nxy;
		int rest = id % nxy;
		int j = rest / nx;
		int i = rest % nx;
		return new Vector3(lo.x + i * cell, lo.y + j * cell, lo.z + k * cell);
	}

	private static int Cut(Result r, Weld weld, int[] cid, float[] cv, int aCorner, int bCorner, int nx, int nxy, Vector3 lo, float cell, float iso)
	{
		int na = cid[aCorner];
		int nb = cid[bCorner];
		int ga = Math.Min(na, nb);
		int gb = Math.Max(na, nb);
		long key = ((long)ga << 32) | (uint)gb;
		if (weld.TryGet(key, ga, gb, out int idx))
		{
			return idx;
		}
		float va = cv[aCorner];
		float vb = cv[bCorner];
		float t;
		if (Math.Abs(vb - va) > 1e-30f)
		{
			t = (iso - va) / (vb - va);
		}
		else
		{
			t = 0.5f;
		}
		t = Math.Clamp(t, 1e-4f, 0.9999f);
		Vector3 pa = NodePos(cid[aCorner], nx, nxy, lo, cell);
		Vector3 pb = NodePos(cid[bCorner], nx, nxy, lo, cell);
		Vector3 p = Vector3.Lerp(pa, pb, t);
		idx = r.vertices.Count;
		r.vertices.Add(p);
		r.keys?.Add(key);
		weld.Set(key, ga, gb, idx);
		return idx;
	}

	private static void AddTri(Result r, int a, int b, int c, Vector3 outward)
	{
		if (a == b || b == c || a == c)
		{
			return;
		}
		Vector3 va = r.vertices[a];
		Vector3 vb = r.vertices[b];
		Vector3 vc = r.vertices[c];
		Vector3 n = Vector3.Cross(vb - va, vc - va);
		float dot = Vector3.Dot(n, outward);
		r.triangles.Add(a);
		r.triangles.Add(dot < 0f ? c : b);
		r.triangles.Add(dot < 0f ? b : c);
	}

	public static double SignedVolume(IList<Vector3> v, IList<int> tri)
	{
		RadarKahan sum = default(RadarKahan);
		for (int i = 0; i < tri.Count; i += 3)
		{
			Vector3 a = v[tri[i]];
			Vector3 b = v[tri[i + 1]];
			Vector3 c = v[tri[i + 2]];
			sum.Add((double)a.x * ((double)b.y * c.z - (double)b.z * c.y) - (double)a.y * ((double)b.x * c.z - (double)b.z * c.x) + (double)a.z * ((double)b.x * c.y - (double)b.y * c.x));
		}
		return sum.Value / 6.0;
	}

	public static bool IsClosedManifold(IList<int> tri, out string why)
	{
		HashSet<long> edges = new HashSet<long>();
		if (tri.Count == 0)
		{
			why = "поверхность пуста — замкнутость проверять не на чем";
			return false;
		}
		for (int i = 0; i < tri.Count; i += 3)
		{
			for (int j = 0; j < 3; j++)
			{
				int a = tri[i + j];
				int b = tri[i + (j + 1) % 3];
				if (!edges.Add(((long)a << 32) | (uint)b))
				{
					why = string.Format("направленное ребро {0}→{1} встретилось дважды (перевёрнутый треугольник или дубль)", a, b);
					return false;
				}
			}
		}
		foreach (long key in edges)
		{
			if (!edges.Contains((key >> 32) | (key << 32)))
			{
				why = string.Format("ребро {0}→{1} без пары — дырка в поверхности", (int)(key >> 32), (int)key);
				return false;
			}
		}
		why = null;
		return true;
	}

	public static int ConnectedComponents(int vertexCount, IList<int> tri)
	{
		if (tri.Count == 0)
		{
			return 0;
		}
		int[] parent = new int[vertexCount];
		for (int i = 0; i < vertexCount; i++)
		{
			parent[i] = i;
		}
		for (int i = 0; i < tri.Count; i += 3)
		{
			Union(tri[i], tri[i + 1]);
			Union(tri[i + 1], tri[i + 2]);
		}
		HashSet<int> roots = new HashSet<int>();
		for (int i = 0; i < tri.Count; i++)
		{
			roots.Add(Find(tri[i]));
		}
		return roots.Count;

		int Find(int a)
		{
			while (parent[a] != a)
			{
				parent[a] = parent[parent[a]];
				a = parent[a];
			}
			return a;
		}

		void Union(int a, int b)
		{
			int ra = Find(a);
			int rb = Find(b);
			if (ra != rb)
			{
				parent[ra] = rb;
			}
		}
	}

	public static bool IsParityClosed(IList<int> tri, out string why)
	{
		Dictionary<long, int> dir = new Dictionary<long, int>();
		if (tri.Count == 0)
		{
			why = "поверхность пуста — баланс проверять не на чем";
			return false;
		}
		for (int i = 0; i < tri.Count; i += 3)
		{
			for (int j = 0; j < 3; j++)
			{
				int a = tri[i + j];
				int b = tri[i + (j + 1) % 3];
				long key = ((long)a << 32) | (uint)b;
				dir.TryGetValue(key, out int n);
				dir[key] = n + 1;
			}
		}
		foreach (KeyValuePair<long, int> e in dir)
		{
			long rev = (e.Key >> 32) | (e.Key << 32);
			dir.TryGetValue(rev, out int m);
			if (m != e.Value)
			{
				why = string.Format("ребро {0}→{1}: ", (int)(e.Key >> 32), (int)e.Key) + string.Format("{0} прямых против {1} обратных — дырка", e.Value, m);
				return false;
			}
		}
		why = null;
		return true;
	}
}
