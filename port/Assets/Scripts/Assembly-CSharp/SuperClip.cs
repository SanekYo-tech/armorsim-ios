using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SuperClip
{
	public struct Counts
	{
		public long live;

		public long band;

		public long dropped;

		public long Kept => live + band;
	}

	public bool present;

	public Vector3 centre;

	public Vector3 u;

	public Vector3 v;

	public Vector3 n;

	public float hu;

	public float hv;

	public float hn;

	public const int BandLayers = 2;

	public static bool SabotageBandZero;

	private static readonly Dictionary<ulong, Counts> _cache = new Dictionary<ulong, Counts>();

	private static readonly Dictionary<string, (int n, int owner)> _inMesh = new Dictionary<string, (int n, int owner)>();

	public static int Band => (!SabotageBandZero) ? BandLayers : 0;

	public static SuperClip Box(Vector3 centre, Vector3 u, Vector3 v, Vector3 n, float widthM, float heightM, float depthM)
	{
		SuperClip result = default(SuperClip);
		result.present = true;
		result.centre = centre;
		result.u = u;
		result.v = v;
		result.n = n;
		result.hu = widthM * 0.5f;
		result.hv = heightM * 0.5f;
		result.hn = depthM * 0.5f;
		return result;
	}

	public int Classify(Vector3 p, float spacing)
	{
		Vector3 d = p - centre;
		float m = Mathf.Max(Mathf.Max(Mathf.Abs(Vector3.Dot(d, v)) - hv, Mathf.Abs(Vector3.Dot(d, n)) - hn), Mathf.Abs(Vector3.Dot(d, u)) - hu);
		if (m <= 0f)
		{
			return 0;
		}
		if (m <= (float)Band * spacing)
		{
			return 1;
		}
		return 2;
	}

	public void ShiftZ(float dz)
	{
		if (present)
		{
			centre.z += dz;
		}
	}

	public static void LatticeRange(float h, float s, out int iMin, out int iMax)
	{
		double r = (double)Band * s + h;
		iMin = (int)Math.Ceiling(-r / s - 0.5);
		iMax = (int)Math.Floor(r / s - 0.5);
		while (iMin <= iMax && Mathf.Abs(CellCoord(iMin, s)) > Band * s + h)
		{
			iMin++;
		}
		while (iMax >= iMin && Mathf.Abs(CellCoord(iMax, s)) > Band * s + h)
		{
			iMax--;
		}
		while (Band * s + h >= Mathf.Abs(CellCoord(iMin - 1, s)))
		{
			iMin--;
		}
		while (Band * s + h >= Mathf.Abs(CellCoord(iMax + 1, s)))
		{
			iMax++;
		}
	}

	public static float CellCoord(int i, float s)
	{
		return (i + 0.5f) * s;
	}

	public bool CellLive(int i, int j, int k, float s)
	{
		return hu >= Mathf.Abs(CellCoord(i, s)) && hv >= Mathf.Abs(CellCoord(j, s)) && hn >= Mathf.Abs(CellCoord(k, s));
	}

	public Vector3 CellCentre(int i, int j, int k, float s)
	{
		return centre + u * CellCoord(i, s) + v * CellCoord(j, s) + n * CellCoord(k, s);
	}

	public string Describe()
	{
		if (!present)
		{
			return "области нет";
		}
		return string.Format("область {0:F0}×{1:F0}×{2:F0} мм, центр ({3:F1}, {4:F1}, {5:F1}) мм", (hu + hu) * 1000f, (hv + hv) * 1000f, (hn + hn) * 1000f, centre.x * 1000f, centre.y * 1000f, centre.z * 1000f);
	}

	public static Counts CountPosedPlate(in SuperPlateSpec pl, float spacing)
	{
		if (!pl.clip.present || !pl.hasPose)
		{
			throw new InvalidOperationException("SuperClip.CountPosedPlate: у плиты нет среза или позы — считать нечего.");
		}
		ulong key = Key(in pl, spacing);
		lock (_cache)
		{
			if (_cache.TryGetValue(key, out Counts cached))
			{
				return cached;
			}
		}
		SuperBaker.PlateFrame(in pl, out Vector3 centre, out Vector3 u, out Vector3 v, out Vector3 n, out float halfU, out float halfV);
		int nt = Math.Max(1, Mathf.RoundToInt(pl.thicknessM / spacing));
		int nu = Math.Max(1, Mathf.RoundToInt(pl.widthM / spacing));
		int nv = Math.Max(1, Mathf.RoundToInt(pl.heightM / spacing));
		SuperClip k = pl.clip;
		double pad = (double)Band * spacing + spacing;
		Counts counts = default(Counts);
		for (int i = 0; i < nt; i++)
		{
			float kk = (i + 0.5f) * spacing - pl.thicknessM * 0.5f;
			for (int j = 0; j < nv; j++)
			{
				float jj = (j - (nv - 1) * 0.5f) * spacing;
				if (pl.roundU && !SuperBaker.RoundKeeps(in pl, jj, kk))
				{
					continue;
				}
				Vector3 q = centre + v * jj + n * kk;
				double lo = double.NegativeInfinity;
				double hi = double.PositiveInfinity;
				int kept = 0;
				if (Slab(Vector3.Dot(q, k.u), Vector3.Dot(u, k.u), k.hu + pad, ref lo, ref hi) && Slab(Vector3.Dot(q, k.v), Vector3.Dot(u, k.v), k.hv + pad, ref lo, ref hi) && Slab(Vector3.Dot(q, k.n), Vector3.Dot(u, k.n), k.hn + pad, ref lo, ref hi))
				{
					int iLo = Math.Max(0, (int)Math.Floor(lo / spacing + (double)(nu - 1) * 0.5) - 1);
					int iHi = Math.Min(nu - 1, (int)Math.Ceiling(hi / spacing + (double)(nu - 1) * 0.5) + 1);
					for (int iU = iLo; iU <= iHi; iU++)
					{
						float uu = ((float)iU - (nu - 1) * 0.5f) * spacing;
						Vector3 p = centre + u * uu + v * jj + n * kk;
						int c = k.Classify(p, spacing);
						if (c == 0)
						{
							counts.live++;
							kept++;
						}
						else if (c == 1)
						{
							counts.band++;
							kept++;
						}
					}
				}
				counts.dropped += nu - kept;
			}
		}
		lock (_cache)
		{
			if (_cache.Count > 64)
			{
				_cache.Clear();
			}
			_cache[key] = counts;
		}
		return counts;
	}

	public static int CountInMesh(in SuperPlateSpec pl, float spacing, SuperMeshVoxels vx, out int ownerPart)
	{
		ownerPart = -1;
		if (!pl.clip.present || !pl.hasPose || vx == null)
		{
			return 0;
		}
		ulong key = Key(in pl, spacing);
		string id = key.ToString("X16") + "#" + vx.key;
		lock (_inMesh)
		{
			if (_inMesh.TryGetValue(id, out var hit))
			{
				ownerPart = hit.owner;
				return hit.n;
			}
		}
		SuperBaker.PlateFrame(in pl, out Vector3 centre, out Vector3 u, out Vector3 v, out Vector3 n, out float halfU, out float halfV);
		int nt = Math.Max(1, Mathf.RoundToInt(pl.thicknessM / spacing));
		int nu = Math.Max(1, Mathf.RoundToInt(pl.widthM / spacing));
		int nv = Math.Max(1, Mathf.RoundToInt(pl.heightM / spacing));
		SuperClip k = pl.clip;
		double pad = (double)Band * spacing + spacing;
		int count = 0;
		int owner = -1;
		for (int i = 0; i < nt; i++)
		{
			float kk = (i + 0.5f) * spacing - pl.thicknessM * 0.5f;
			for (int j = 0; j < nv; j++)
			{
				float jj = (j - (nv - 1) * 0.5f) * spacing;
				if (pl.roundU && !SuperBaker.RoundKeeps(in pl, jj, kk))
				{
					continue;
				}
				Vector3 q = centre + v * jj + n * kk - k.centre;
				double lo = double.NegativeInfinity;
				double hi = double.PositiveInfinity;
				if (Slab(Vector3.Dot(q, k.u), Vector3.Dot(u, k.u), k.hu + pad, ref lo, ref hi) && Slab(Vector3.Dot(q, k.v), Vector3.Dot(u, k.v), k.hv + pad, ref lo, ref hi) && Slab(Vector3.Dot(q, k.n), Vector3.Dot(u, k.n), k.hn + pad, ref lo, ref hi))
				{
					int iLo = Math.Max(0, (int)Math.Floor(lo / spacing + (double)(nu - 1) * 0.5) - 1);
					int iHi = Math.Min(nu - 1, (int)Math.Ceiling(hi / spacing + (double)(nu - 1) * 0.5) + 1);
					for (int iU = iLo; iU <= iHi; iU++)
					{
						float uu = ((float)iU - (nu - 1) * 0.5f) * spacing;
						Vector3 p = centre + u * uu + v * jj + n * kk;
						if (k.Classify(p, spacing) == 2)
						{
							continue;
						}
						Vector3 rel = p - k.centre;
						Vector3 local = new Vector3(Vector3.Dot(rel, k.u), Vector3.Dot(rel, k.v), Vector3.Dot(rel, k.n));
						if (vx.CellOf(local, out int ci, out int cj, out int ck))
						{
							int cellOwner = vx.OwnerAt(ci, cj, ck);
							if (cellOwner >= 0)
							{
								count++;
								if (owner < 0)
								{
									owner = cellOwner;
								}
							}
						}
					}
				}
			}
		}
		lock (_inMesh)
		{
			if (_inMesh.Count > 64)
			{
				_inMesh.Clear();
			}
			_inMesh[id] = (count, owner);
		}
		ownerPart = owner;
		return count;
	}

	private static bool Slab(double o, double d, double h, ref double lo, ref double hi)
	{
		if (Math.Abs(d) < 1e-12)
		{
			return h + 1e-09 >= Math.Abs(o);
		}
		double a = (h - o) / d;
		double b = (-h - o) / d;
		if (a > b)
		{
			double num = a;
			a = b;
			b = num;
		}
		if (a > lo)
		{
			lo = a;
		}
		if (hi > b)
		{
			hi = b;
		}
		return hi + 1e-09 >= lo;
	}

	private static ulong Key(in SuperPlateSpec p, float s)
	{
		ulong h = 14695981039346656037uL;
		void F(float f)
		{
			h = (h ^ (uint)BitConverter.SingleToInt32Bits(f)) * 0x100000001B3L;
		}
		void V3(Vector3 v)
		{
			F(v.x);
			F(v.y);
			F(v.z);
		}
		F(s);
		F(p.thicknessM);
		F(p.widthM);
		F(p.heightM);
		V3(p.poseCentreM);
		V3(p.poseU);
		V3(p.poseV);
		V3(p.poseN);
		F(Band);
		V3(p.clip.centre);
		V3(p.clip.u);
		V3(p.clip.v);
		V3(p.clip.n);
		F(p.clip.hu);
		F(p.clip.hv);
		F(p.clip.hn);
		if (p.roundU)
		{
			F(1f);
		}
		return h;
	}
}
