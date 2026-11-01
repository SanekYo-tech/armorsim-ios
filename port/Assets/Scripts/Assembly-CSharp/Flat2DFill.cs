using System;
using System.Collections.Generic;
using UnityEngine;

public static class Flat2DFill
{
	public enum Role
	{
		Armour = 0,
		Projectile = 1,
		Anchor = 2
	}

	public struct Poly
	{
		public Vector2[] pts;

		public uint material;

		public Role role;

		public Vector2[] path;

		public float halfWidth;

		public bool square;

		public bool erase;
	}

	public struct Result
	{
		public Vector2[] pos;

		public Vector2[] vel;

		public uint[] mat;

		public bool[] pinned;

		public byte[] ownerRole;

		public int[] ownerPoly;

		public uint[] paint;

		public int count;

		public bool ok;

		public string message;
	}

	public const int Ceiling = 1000000;

	private const long GridMax = 8000000L;

	private static int[] ownerBuf = new int[0];

	private static bool[] pinBuf = new bool[0];

	public static int CountOnly(IList<Poly> polys, float dx, int stopAbove = 2147483647)
	{
		if (!Bounds(polys, dx, out Vector2 min, out int nx, out int ny))
		{
			return 0;
		}
		if (GridFits(nx, ny))
		{
			return Rasterize(polys, dx, min, nx, ny, stopAbove);
		}
		int n = 0;
		for (int j = 0; j < ny; j++)
		{
			for (int i = 0; i < nx; i++)
			{
				Vector2 p = new Vector2(min.x + ((float)i + 0.5f) * dx, min.y + ((float)j + 0.5f) * dx);
				if (MaterialAt(polys, p, out uint _) && ++n > stopAbove)
				{
					return n;
				}
			}
		}
		return n;
	}

	public static Result Build(IList<Poly> polys, float dx, Vector2 velocity = default(Vector2))
	{
		Result r = default;
		if (polys == null || polys.Count == 0)
		{
			r.message = Flat2DText.Tr("заливка: список многоугольников пуст");
			return r;
		}
		if (!Bounds(polys, dx, out Vector2 min, out int nx, out int ny))
		{
			r.message = Flat2DText.Tr("заливка: у многоугольников нет площади");
			return r;
		}
		bool rasterized = false;
		int n;
		if (GridFits(nx, ny))
		{
			n = Rasterize(polys, dx, min, nx, ny, Ceiling);
			rasterized = true;
		}
		else
		{
			n = CountOnly(polys, dx, Ceiling);
		}
		if (n == 0)
		{
			r.message = Flat2DText.Tr("заливка: НИ ОДИН узел решётки не попал внутрь — фигура тоньше шага Δx или всё нарисованное стёрто ластиком");
			return r;
		}
		if (n > Ceiling)
		{
			r.count = n;
			r.message = Flat2DText.F("заливка ОТКАЗАНА: {0}{1} частиц при потолке {2} (счёт остановлен на потолке). Крупнее шаг Δx или меньше фигура. ⚠️ Молча урезать нельзя: усечённая сцена — это ДРУГАЯ сцена.", "≥", n, Ceiling);
			return r;
		}

		r.pos = new Vector2[n];
		r.vel = new Vector2[n];
		r.mat = new uint[n];
		r.pinned = new bool[n];
		r.ownerPoly = new int[n];
		int count = 0;
		if (!rasterized)
		{
			for (int j = 0; j < ny; j++)
			{
				for (int i = 0; i < nx; i++)
				{
					Vector2 p = new Vector2(min.x + ((float)i + 0.5f) * dx, min.y + ((float)j + 0.5f) * dx);
					if (MaterialAt(polys, p, out uint material, out int owner))
					{
						r.pos[count] = p;
						r.vel[count] = velocity;
						r.mat[count] = material;
						r.pinned[count] = InAnchor(polys, p);
						r.ownerPoly[count] = owner;
						count++;
					}
				}
			}
		}
		else
		{
			RasterizeAnchors(polys, dx, min, nx, ny);
			for (int j = 0; j < ny; j++)
			{
				for (int i = 0; i < nx; i++)
				{
					int k = j * nx + i;
					int owner = ownerBuf[k];
					if (owner < 0 || polys[owner].erase)
					{
						continue;
					}
					Vector2 p = new Vector2(min.x + ((float)i + 0.5f) * dx, min.y + ((float)j + 0.5f) * dx);
					r.pos[count] = p;
					r.vel[count] = velocity;
					r.mat[count] = polys[owner].material;
					r.pinned[count] = pinBuf[k];
					r.ownerPoly[count] = owner;
					count++;
				}
			}
		}
		r.count = count;
		r.ok = true;
		int pinned = 0;
		for (int i = 0; i < r.pinned.Length; i++)
		{
			if (r.pinned[i])
			{
				pinned++;
			}
		}
		r.message = Flat2DText.F("заливка: {0} частиц при потолке {1}; закреплено якорем {2}", count, Ceiling, pinned);
		return r;
	}

	private static bool Bounds(IList<Poly> polys, float dx, out Vector2 min, out int nx, out int ny)
	{
		min = default;
		nx = 0;
		ny = 0;
		float minX = float.MaxValue;
		float minY = float.MaxValue;
		float maxX = float.MinValue;
		float maxY = float.MinValue;
		bool any = false;
		foreach (Poly poly in polys)
		{
			if (poly.erase)
			{
				continue;
			}
			if (poly.path != null && poly.path.Length > 0)
			{
				for (int i = 0; i < poly.path.Length; i++)
				{
					Vector2 p = poly.path[i];
					minX = Mathf.Min(minX, p.x - poly.halfWidth);
					minY = Mathf.Min(minY, p.y - poly.halfWidth);
					maxX = Mathf.Max(maxX, p.x + poly.halfWidth);
					maxY = Mathf.Max(maxY, p.y + poly.halfWidth);
					any = true;
				}
			}
			else if (poly.pts != null && poly.pts.Length > 2)
			{
				for (int i = 0; i < poly.pts.Length; i++)
				{
					Vector2 p = poly.pts[i];
					minX = Mathf.Min(minX, p.x);
					minY = Mathf.Min(minY, p.y);
					maxX = Mathf.Max(maxX, p.x);
					maxY = Mathf.Max(maxY, p.y);
					any = true;
				}
			}
		}
		if (!any)
		{
			return false;
		}
		min = new Vector2(minX, minY);
		nx = Math.Max(0, (int)Math.Ceiling((maxX - minX) / dx));
		ny = Math.Max(0, (int)Math.Ceiling((maxY - minY) / dx));
		return nx > 0 && ny > 0;
	}

	private static bool MaterialAt(IList<Poly> polys, Vector2 p, out uint material)
	{
		return MaterialAt(polys, p, out material, out int _);
	}

	private static bool MaterialAt(IList<Poly> polys, Vector2 p, out uint material, out int owner)
	{
		for (int i = 0; i < polys.Count; i++)
		{
			Poly poly = polys[i];
			if (poly.role == Role.Anchor || !Covers(in poly, p))
			{
				continue;
			}
			owner = i;
			if (poly.erase)
			{
				material = 0u;
				return false;
			}
			material = poly.material;
			return true;
		}
		material = 0u;
		owner = -1;
		return false;
	}

	private static bool GridFits(int nx, int ny)
	{
		return nx > 0 && ny > 0 && (long)nx * ny <= GridMax;
	}

	private static int Rasterize(IList<Poly> polys, float dx, Vector2 min, int nx, int ny, int stopAbove)
	{
		int cells = nx * ny;
		if (ownerBuf.Length < cells)
		{
			ownerBuf = new int[cells];
		}
		for (int i = 0; i < cells; i++)
		{
			ownerBuf[i] = -1;
		}
		int n = 0;
		for (int q = 0; q < polys.Count; q++)
		{
			Poly poly = polys[q];
			if (poly.role == Role.Anchor)
			{
				continue;
			}
			if (poly.path != null && poly.path.Length > 0)
			{
				if (poly.path.Length == 1)
				{
					if (StampPart(in poly, q, -1, dx, min, nx, ny, ref n, stopAbove))
					{
						return n;
					}
				}
				else
				{
					for (int part = 0; part + 1 < poly.path.Length; part++)
					{
						if (StampPart(in poly, q, part, dx, min, nx, ny, ref n, stopAbove))
						{
							return n;
						}
					}
				}
			}
			else if (poly.pts != null && poly.pts.Length > 2 && StampPart(in poly, q, -2, dx, min, nx, ny, ref n, stopAbove))
			{
				return n;
			}
		}
		return n;
	}

	private static bool StampPart(in Poly poly, int q, int part, float dx, Vector2 min, int nx, int ny, ref int n, int stopAbove)
	{
		PartRange(in poly, part, dx, min, nx, ny, out int i0, out int i1, out int j0, out int j1);
		for (int j = j0; j <= j1; j++)
		{
			for (int i = i0; i <= i1; i++)
			{
				int k = j * nx + i;
				if (ownerBuf[k] >= 0)
				{
					continue;
				}
				Vector2 p = new Vector2(min.x + ((float)i + 0.5f) * dx, min.y + ((float)j + 0.5f) * dx);
				if (PartCovers(in poly, part, p))
				{
					ownerBuf[k] = q;
					if (!poly.erase && ++n > stopAbove)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private static void RasterizeAnchors(IList<Poly> polys, float dx, Vector2 min, int nx, int ny)
	{
		int cells = nx * ny;
		if (pinBuf.Length < cells)
		{
			pinBuf = new bool[cells];
		}
		Array.Clear(pinBuf, 0, cells);
		for (int q = 0; q < polys.Count; q++)
		{
			Poly poly = polys[q];
			if (poly.role != Role.Anchor || poly.erase)
			{
				continue;
			}
			int parts;
			if (poly.path != null && poly.path.Length > 0)
			{
				parts = poly.path.Length == 1 ? 1 : poly.path.Length - 1;
			}
			else if (poly.pts != null && poly.pts.Length > 2)
			{
				parts = 1;
			}
			else
			{
				continue;
			}
			for (int partIndex = 0; partIndex < parts; partIndex++)
			{
				int part;
				if (poly.path == null || poly.path.Length == 0)
				{
					part = -2;
				}
				else if (poly.path.Length == 1)
				{
					part = -1;
				}
				else
				{
					part = partIndex;
				}
				PartRange(in poly, part, dx, min, nx, ny, out int i0, out int i1, out int j0, out int j1);
				for (int j = j0; j <= j1; j++)
				{
					for (int i = i0; i <= i1; i++)
					{
						int k = j * nx + i;
						if (pinBuf[k] || ownerBuf[k] < 0 || polys[ownerBuf[k]].erase)
						{
							continue;
						}
						Vector2 p = new Vector2(min.x + ((float)i + 0.5f) * dx, min.y + ((float)j + 0.5f) * dx);
						if (PartCovers(in poly, part, p))
						{
							pinBuf[k] = true;
						}
					}
				}
			}
		}
	}

	private static bool PartCovers(in Poly poly, int part, Vector2 p)
	{
		if (part == -2)
		{
			return Inside(poly.pts, p);
		}
		if (part == -1)
		{
			Vector2 d = p - poly.path[0];
			if (poly.square)
			{
				return Mathf.Abs(d.x) <= poly.halfWidth && Mathf.Abs(d.y) <= poly.halfWidth;
			}
			return d.x * d.x + d.y * d.y <= poly.halfWidth * poly.halfWidth;
		}
		Vector2 a = poly.path[part];
		Vector2 b = poly.path[part + 1];
		if (poly.square)
		{
			return SegSquareCovers(a, b, poly.halfWidth, p);
		}
		return SqrDistPointSeg(p, a, b) <= poly.halfWidth * poly.halfWidth;
	}

	private static void PartRange(in Poly poly, int part, float dx, Vector2 min, int nx, int ny, out int i0, out int i1, out int j0, out int j1)
	{
		float minX;
		float minY;
		float maxX;
		float maxY;
		if (part == -2)
		{
			minX = float.MaxValue;
			minY = float.MaxValue;
			maxX = float.MinValue;
			maxY = float.MinValue;
			for (int i = 0; i < poly.pts.Length; i++)
			{
				Vector2 p = poly.pts[i];
				minX = Mathf.Min(minX, p.x);
				minY = Mathf.Min(minY, p.y);
				maxX = Mathf.Max(maxX, p.x);
				maxY = Mathf.Max(maxY, p.y);
			}
		}
		else
		{
			Vector2 a = poly.path[Math.Max(0, part)];
			Vector2 b = poly.path[Math.Max(0, part + 1)];
			minX = Mathf.Min(a.x, b.x) - poly.halfWidth;
			minY = Mathf.Min(a.y, b.y) - poly.halfWidth;
			maxX = Mathf.Max(a.x, b.x) + poly.halfWidth;
			maxY = Mathf.Max(a.y, b.y) + poly.halfWidth;
		}
		i0 = Math.Max(0, (int)Math.Round((minX - min.x) / dx - 0.5f) - 1);
		i1 = Math.Min(nx - 1, (int)Math.Ceiling((maxX - min.x) / dx - 0.5f) + 1);
		j0 = Math.Max(0, (int)Math.Round((minY - min.y) / dx - 0.5f) - 1);
		j1 = Math.Min(ny - 1, (int)Math.Ceiling((maxY - min.y) / dx - 0.5f) + 1);
	}

	private static bool InAnchor(IList<Poly> polys, Vector2 p)
	{
		foreach (Poly poly in polys)
		{
			if (poly.role == Role.Anchor && !poly.erase && Covers(in poly, p))
			{
				return true;
			}
		}
		return false;
	}

	public static bool Covers(in Poly poly, Vector2 p)
	{
		if (poly.path != null && poly.path.Length > 0)
		{
			return StrokeCovers(poly.path, poly.halfWidth, poly.square, p);
		}
		return poly.pts != null && poly.pts.Length >= 3 && Inside(poly.pts, p);
	}

	public static bool StrokeCovers(Vector2[] path, float halfW, bool square, Vector2 p)
	{
		if (path.Length == 1)
		{
			Vector2 d = p - path[0];
			if (square)
			{
				return Mathf.Abs(d.x) <= halfW && Mathf.Abs(d.y) <= halfW;
			}
			return d.x * d.x + d.y * d.y <= halfW * halfW;
		}
		for (int i = 0; i + 1 < path.Length; i++)
		{
			if (square ? SegSquareCovers(path[i], path[i + 1], halfW, p) : SqrDistPointSeg(p, path[i], path[i + 1]) <= halfW * halfW)
			{
				return true;
			}
		}
		return false;
	}

	private static float SqrDistPointSeg(Vector2 p, Vector2 a, Vector2 b)
	{
		Vector2 d = b - a;
		float len2 = d.x * d.x + d.y * d.y;
		float t = 0f;
		if (len2 >= 1e-20f)
		{
			t = ((p.x - a.x) * d.x + (p.y - a.y) * d.y) / len2;
			if (t < 0f)
			{
				t = 0f;
			}
			else if (t > 1f)
			{
				t = 1f;
			}
		}
		float x = p.x - (a.x + d.x * t);
		float y = p.y - (a.y + d.y * t);
		return x * x + y * y;
	}

	private static bool SegSquareCovers(Vector2 a, Vector2 b, float h, Vector2 p)
	{
		float t0 = 0f;
		float t1 = 1f;
		return AxisWindow(a.x, b.x - a.x, p.x, h, ref t0, ref t1) && AxisWindow(a.y, b.y - a.y, p.y, h, ref t0, ref t1) && t0 <= t1;
	}

	private static bool AxisWindow(float a, float d, float p, float h, ref float t0, ref float t1)
	{
		if (Mathf.Abs(d) < 1e-12f)
		{
			return Mathf.Abs(p - a) <= h;
		}
		float u0 = (p - h - a) / d;
		float u1 = (p + h - a) / d;
		if (u0 > u1)
		{
			float tmp = u0;
			u0 = u1;
			u1 = tmp;
		}
		t0 = Mathf.Max(t0, u0);
		t1 = Mathf.Min(t1, u1);
		return t0 <= t1;
	}

	public static bool LatticeOf(IList<Poly> polys, float dx, out Vector2 min, out int nx, out int ny)
	{
		return Bounds(polys, dx, out min, out nx, out ny);
	}

	public static bool Inside(Vector2[] v, Vector2 p)
	{
		bool inside = false;
		int j = v.Length - 1;
		for (int i = 0; i < v.Length; j = i, i++)
		{
			if ((p.y < v[i].y) != (p.y < v[j].y) && p.x < (v[j].x - v[i].x) * (p.y - v[i].y) / (v[j].y - v[i].y) + v[i].x)
			{
				inside = !inside;
			}
		}
		return inside;
	}

	public static Vector2[] Rect(Vector2 min, Vector2 size)
	{
		return new Vector2[4]
		{
			min,
			new Vector2(min.x + size.x, min.y),
			new Vector2(min.x + size.x, min.y + size.y),
			new Vector2(min.x, min.y + size.y)
		};
	}
}
