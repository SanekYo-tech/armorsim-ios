using System;
using UnityEngine;

public sealed class SuperMeshVoxels
{
	public sealed class Part
	{
		public string name;

		public int[] cells;

		public int live;

		public int band;

		public long votedAny;

		public long ambiguous;

		public int[] lostTo;

		public int bi0;

		public int bi1;

		public int bj0;

		public int bj1;

		public int bk0;

		public int bk1;

		internal int[][] colX;

		internal int[][] colY;

		internal int[][] colZ;

		public int Count
		{
			get
			{
				if (cells == null)
				{
					return 0;
				}
				return cells.Length / 3;
			}
		}

		public bool Empty => bi0 > bi1 || bj0 > bj1 || bk0 > bk1;

		public double AmbiguousFraction
		{
			get
			{
				if (votedAny <= 0)
				{
					return 0.0;
				}
				return (double)ambiguous / (double)votedAny;
			}
		}
	}

	public string key;

	public string name;

	public float spacing;

	public float hu;

	public float hv;

	public float hn;

	public int iMin;

	public int iMax;

	public int jMin;

	public int jMax;

	public int kMin;

	public int kMax;

	public Part[] parts;

	public int NI => iMax - iMin + 1;

	public int NJ => jMax - jMin + 1;

	public int NK => kMax - kMin + 1;

	public long LatticeCells => (long)(kMax - kMin + 1) * (long)(jMax - jMin + 1) * (long)(iMax - iMin + 1);

	public int Votes(int p, int i, int j, int k)
	{
		Part part = parts[p];
		if (part.bi0 > part.bi1 || part.bj0 > part.bj1 || part.bk0 > part.bk1)
		{
			return 0;
		}
		if (i < part.bi0 || i > part.bi1 || j < part.bj0 || j > part.bj1 || k < part.bk0 || k > part.bk1)
		{
			return 0;
		}
		int ni = part.bi1 - part.bi0 + 1;
		int nj = part.bj1 - part.bj0 + 1;
		int nk = part.bk1 - part.bk0 + 1;
		return Parity(part.colX[(j - part.bj0) * nk + (k - part.bk0)], i) + Parity(part.colY[(k - part.bk0) * ni + (i - part.bi0)], j) + Parity(part.colZ[(i - part.bi0) * nj + (j - part.bj0)], k);
	}

	public int OwnerAt(int i, int j, int k)
	{
		if (i < iMin || i > iMax || j < jMin || j > jMax || k < kMin || k > kMax)
		{
			return -1;
		}
		for (int p = 0; p < parts.Length; p++)
		{
			if (Votes(p, i, j, k) >= 2)
			{
				return p;
			}
		}
		return -1;
	}

	public bool CellOf(Vector3 local, out int i, out int j, out int k)
	{
		i = (int)Math.Floor(local.x / spacing);
		j = (int)Math.Floor(local.y / spacing);
		k = (int)Math.Floor(local.z / spacing);
		return i >= iMin && i <= iMax && j >= jMin && j <= jMax && k >= kMin && k <= kMax;
	}

	internal static int Parity(int[] col, int idx)
	{
		if (col == null || col.Length == 0)
		{
			return 0;
		}
		int lo = 0;
		int hi = col.Length;
		while (lo < hi)
		{
			int mid = (lo + hi) >> 1;
			if (col[mid] > idx)
			{
				hi = mid;
			}
			else
			{
				lo = mid + 1;
			}
		}
		return lo & 1;
	}
}
