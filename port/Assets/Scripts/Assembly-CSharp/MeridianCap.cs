using System.Collections.Generic;
using UnityEngine;

public static class MeridianCap
{
	public static int Failures;

	public static int Suspect;

	public static float LastAreaError;

	public const float AreaTolerance = 0.01f;

	public static int Simplified;

	public static int RemovedVerts;

	public static bool Triangulate(Vector3[] verts, List<int> poly, List<int> into)
	{
		if (poly.Count < 3)
		{
			return false;
		}
		if (SignedArea(verts, poly) < 0f)
		{
			poly.Reverse();
		}
		int before = RemovedVerts;
		Untangle(verts, poly);
		if (RemovedVerts > before)
		{
			Simplified++;
		}
		int n = poly.Count;
		if (n < 3)
		{
			return false;
		}
		int start = into.Count;
		List<int> idx = new List<int>(n);
		for (int i = 0; i < n; i++)
		{
			idx.Add(i);
		}
		float lo = float.MaxValue;
		float hi = float.MinValue;
		for (int j = 0; j < poly.Count; j++)
		{
			Vector3 p = verts[poly[j]];
			lo = Mathf.Min(lo, Mathf.Min(p.x, p.z));
			hi = Mathf.Max(hi, Mathf.Max(p.x, p.z));
		}
		float eps = Mathf.Max(hi - lo, 1e-9f) * 1e-6f;
		int guard = n * n + 16;
		int k = 0;
		while (idx.Count > 3 && k++ < guard)
		{
			if (!ClipOnce(verts, poly, idx, into, eps, true) && !ClipOnce(verts, poly, idx, into, eps, false))
			{
				Failures++;
				return false;
			}
		}
		if (idx.Count != 3)
		{
			Failures++;
			return false;
		}
		into.Add(poly[idx[0]]);
		into.Add(poly[idx[1]]);
		into.Add(poly[idx[2]]);
		float cs = 0f;
		for (int l = 0; l < poly.Count; l++)
		{
			Vector3 p2 = verts[poly[l]];
			Vector3 q = verts[poly[(l + 1) % poly.Count]];
			cs += q.z * p2.x - p2.z * q.x;
		}
		float polyArea = Mathf.Abs(cs * 0.5f);
		float tri = 0f;
		for (int t = start; t + 2 < into.Count; t += 3)
		{
			Vector3 a = verts[into[t]];
			Vector3 b = verts[into[t + 1]];
			Vector3 c = verts[into[t + 2]];
			tri += Mathf.Abs((c.z - a.z) * (b.x - a.x) - (b.z - a.z) * (c.x - a.x)) * 0.5f;
		}
		if (polyArea > 1e-12f)
		{
			LastAreaError = Mathf.Abs(tri - polyArea) / polyArea;
		}
		else
		{
			LastAreaError = 0f;
		}
		if (LastAreaError > AreaTolerance)
		{
			Suspect++;
		}
		return true;
	}

	private static void Untangle(Vector3[] v, List<int> poly)
	{
		int passes = 0;
		while (true)
		{
			int n = poly.Count;
			if (n < 5)
			{
				return;
			}
			int ia = -1;
			int ib = -1;
			for (int i = 0; i < n; i++)
			{
				if (ia >= 0)
				{
					break;
				}
				Vector3 a = v[poly[i]];
				Vector3 b = v[poly[(i + 1) % n]];
				for (int j = i + 2; j < n; j++)
				{
					if (i != 0 || j != n - 1)
					{
						Vector3 c = v[poly[j]];
						Vector3 d = v[poly[(j + 1) % n]];
						if (Crosses(a, b, c, d))
						{
							ia = i;
							ib = j;
							break;
						}
					}
				}
			}
			if (ia < 0)
			{
				return;
			}
			if (ia != 0)
			{
				List<int> head = poly.GetRange(0, ia);
				poly.RemoveRange(0, ia);
				poly.AddRange(head);
				ib -= ia;
			}
			float area1 = RingArea(v, poly, 1, ib);
			float area2 = RingArea(v, poly, ib + 1, poly.Count - 1);
			if (area1 > area2)
			{
				int count = poly.Count - ib - 1;
				RemovedVerts += count;
				poly.RemoveRange(ib + 1, count);
			}
			else
			{
				RemovedVerts += ib;
				poly.RemoveRange(1, ib);
			}
			passes++;
			if (passes >= 64)
			{
				return;
			}
		}
	}

	private static float RingArea(Vector3[] v, List<int> poly, int lo, int hi)
	{
		float s = 0f;
		for (int i = lo; i <= hi; i++)
		{
			Vector3 p = v[poly[i]];
			int j = (i != hi) ? (i + 1) : lo;
			Vector3 q = v[poly[j]];
			s += q.z * p.x - p.z * q.x;
		}
		if (s < 0f)
		{
			return s * -0.5f;
		}
		return s * 0.5f;
	}

	private static bool Crosses(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		float num = Side(c, a, b);
		float num2 = Side(d, a, b);
		float num3 = Side(a, c, d);
		float num4 = Side(b, c, d);
		return ((num > 0f && num2 < 0f) || (num < 0f && num2 > 0f)) && ((num3 > 0f && num4 < 0f) || (num3 < 0f && num4 > 0f));
	}

	private static bool ClipOnce(Vector3[] verts, List<int> poly, List<int> idx, List<int> into, float eps, bool strict)
	{
		for (int i = 0; i < idx.Count; i++)
		{
			int i0 = idx[(i - 1 + idx.Count) % idx.Count];
			int i1 = idx[i];
			int i2 = idx[(i + 1) % idx.Count];
			if (IsEar(verts, poly, idx, i0, i1, i2, eps, strict))
			{
				into.Add(poly[i0]);
				into.Add(poly[i1]);
				into.Add(poly[i2]);
				idx.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	private static float Extent(Vector3[] v, List<int> poly)
	{
		float lo = float.MaxValue;
		float hi = float.MinValue;
		for (int i = 0; i < poly.Count; i++)
		{
			Vector3 p = v[poly[i]];
			lo = Mathf.Min(lo, Mathf.Min(p.x, p.z));
			hi = Mathf.Max(hi, Mathf.Max(p.x, p.z));
		}
		return Mathf.Max(hi - lo, 1e-9f);
	}

	private static float SignedArea(Vector3[] v, List<int> poly)
	{
		float num = 0f;
		for (int i = 0; i < poly.Count; i++)
		{
			Vector3 vector = v[poly[i]];
			Vector3 vector2 = v[poly[(i + 1) % poly.Count]];
			num += vector2.z * vector.x - vector.z * vector2.x;
		}
		return num * 0.5f;
	}

	private static bool IsEar(Vector3[] v, List<int> poly, List<int> idx, int i0, int i1, int i2, float eps, bool strict)
	{
		Vector3 a = v[poly[i0]];
		Vector3 b = v[poly[i1]];
		Vector3 c = v[poly[i2]];
		float num = Side(b, c, a);
		if (strict)
		{
			if (num <= eps)
			{
				return false;
			}
		}
		else
		{
			if (num < -eps)
			{
				return false;
			}
			if (num <= eps)
			{
				return true;
			}
		}
		for (int j = 0; j < idx.Count; j++)
		{
			int k = idx[j];
			if (k != i0 && k != i1 && k != i2)
			{
				Vector3 p = v[poly[k]];
				Vector3 prev = v[poly[idx[(j - 1 + idx.Count) % idx.Count]]];
				Vector3 next = v[poly[idx[(j + 1) % idx.Count]]];
				if (Side(p, next, prev) <= eps && !Near(p, a, eps) && !Near(p, b, eps) && !Near(p, c, eps))
				{
					float num2 = Side(p, a, b);
					float num3 = Side(p, b, c);
					float num4 = Side(p, c, a);
					bool flag = num2 < 0f || num3 < 0f || num4 < 0f;
					bool flag2 = num2 > 0f || num3 > 0f || num4 > 0f;
					if (!flag2 || !flag)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	private static bool InTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
	{
		float num = Side(p, a, b);
		float num2 = Side(p, b, c);
		float num3 = Side(p, c, a);
		bool flag = num < 0f || num2 < 0f || num3 < 0f;
		bool flag2 = num > 0f || num2 > 0f || num3 > 0f;
		return !flag2 || !flag;
	}

	private static bool Near(Vector3 p, Vector3 q, float eps)
	{
		if (eps < Mathf.Abs(p.x - q.x))
		{
			return false;
		}
		return eps >= Mathf.Abs(p.z - q.z);
	}

	private static float Side(Vector3 p, Vector3 a, Vector3 b)
	{
		return (a.z - b.z) * (p.x - b.x) - (p.z - b.z) * (a.x - b.x);
	}
}
