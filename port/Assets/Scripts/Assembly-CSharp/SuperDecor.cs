using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class SuperDecor
{
	public enum Kind : byte
	{
		Box = 0,
		ModelPart = 1
	}

	public sealed class Piece
	{
		public string name;

		public Kind kind;

		public Vector3[] vertices;

		public int TriangleCount => (vertices != null) ? vertices.Length / 3 : 0;

		public Piece()
		{
		}
	}

	public sealed class Sources
	{
		public SuperClip clip;

		public float dz;

		public float pad;

		public readonly List<BoxSource> boxes = new List<BoxSource>();

		public readonly List<MeshSource> meshes = new List<MeshSource>();

		public bool Any => boxes.Count > 0 || meshes.Count > 0;

		public Sources()
		{
		}
	}

	public struct BoxSource
	{
		public string name;

		public Vector3 centre;

		public Vector3 u;

		public Vector3 v;

		public Vector3 n;

		public float widthM;

		public float heightM;

		public float thicknessM;
	}

	public struct MeshSource
	{
		public string name;

		public ModelPart part;

		public Matrix4x4 toFrame;
	}

	public const byte Format = 1;

	public const int MaxTriangles = 8000000;

	public static bool SabotageNoFlip;

	public SuperClip region;

	public readonly List<Piece> pieces = new List<Piece>();

	public long droppedTriangles;

	private static readonly int[] BoxTrisRaw;

	[ThreadStatic]
	private static List<Vector3> _poly;

	[ThreadStatic]
	private static List<Vector3> _front;

	[ThreadStatic]
	private static List<Vector3> _back;

	public static bool Collision;

	static SuperDecor()
	{
		BoxTrisRaw = new int[36]
		{
			0, 2, 3, 0, 3, 1, 4, 5, 7, 4, 7, 6, 0, 1, 5, 0, 5, 4, 2, 6, 7, 2, 7, 3, 0, 4, 6, 0, 6, 2, 1, 3, 7, 1, 7, 5
		};
		Collision = true;
	}

	public SuperDecor()
	{
	}

	public int TriangleCount
	{
		get
		{
			int sum = 0;
			foreach (Piece p in pieces)
			{
				sum += p.TriangleCount;
			}
			return sum;
		}
	}

	public bool Empty => TriangleCount == 0;

	public static SuperDecor Build(Sources s)
	{
		if (s == null)
		{
			throw new ArgumentNullException("s");
		}
		if (!s.clip.present)
		{
			throw new InvalidOperationException("SuperDecor.Build: области нет — декору не от чего отрезаться.");
		}
		SuperDecor decor = new SuperDecor();
		decor.region = s.clip;
		if (decor.region.present)
		{
			decor.region.centre.z += s.dz;
		}
		SuperClip k = s.clip;
		k.hu += s.pad;
		k.hv += s.pad;
		k.hn += s.pad;
		List<Vector3> tri = new List<Vector3>(1024);
		int[] order = new int[36];
		int budget = MaxTriangles;
		foreach (BoxSource b in s.boxes)
		{
			tri.Clear();
			Vector3[] corners = BoxCorners(in b, order);
			for (int t = 0; t < 12; t++)
			{
				ClipOutside(in k, corners[order[t * 3]], corners[order[t * 3 + 1]], corners[order[t * 3 + 2]], tri);
			}
			decor.AddPiece(b.name, Kind.Box, tri, s.dz, ref budget);
		}
		foreach (MeshSource m in s.meshes)
		{
			tri.Clear();
			bool flip = Det3(m.toFrame) < 0f && !SabotageNoFlip;
			for (int i = 0; i + 2 < m.part.triangles.Length; i += 3)
			{
				Vector3 a = m.toFrame.MultiplyPoint3x4(m.part.vertices[m.part.triangles[i]]);
				Vector3 b2 = m.toFrame.MultiplyPoint3x4(m.part.vertices[m.part.triangles[i + 1]]);
				Vector3 c = m.toFrame.MultiplyPoint3x4(m.part.vertices[m.part.triangles[i + 2]]);
				if (flip)
				{
					ClipOutside(in k, a, b2, c, tri);
				}
				else
				{
					ClipOutside(in k, a, c, b2, tri);
				}
			}
			decor.AddPiece(m.name, Kind.ModelPart, tri, s.dz, ref budget);
		}
		return decor;
	}

	private void AddPiece(string name, Kind kind, List<Vector3> tri, float dz, ref int budget)
	{
		int n = tri.Count / 3;
		if (n == 0)
		{
			return;
		}
		if (n > budget)
		{
			droppedTriangles += n;
			Debug.LogWarning(string.Format("[Super] Декор «{0}»: {1} треугольников за областью — сверх потолка показа ", name ?? "", n) + string.Format("({0}); эта часть в ленту не пишется и в показе за областью её не будет.", MaxTriangles));
			return;
		}
		budget -= n;
		Vector3[] verts = new Vector3[tri.Count];
		for (int i = 0; i < verts.Length; i++)
		{
			Vector3 v = tri[i];
			v.z += dz;
			verts[i] = v;
		}
		Piece piece = new Piece();
		piece.name = name ?? "";
		piece.kind = kind;
		piece.vertices = verts;
		pieces.Add(piece);
	}

	private static Vector3[] BoxCorners(in BoxSource b, int[] order)
	{
		Vector3[] corners = new Vector3[8];
		for (int i = 0; i < 8; i++)
		{
			float su = ((i & 1) != 0) ? 0.5f : -0.5f;
			float sv = ((i & 2) != 0) ? 0.5f : -0.5f;
			float sn = ((i & 4) != 0) ? 0.5f : -0.5f;
			corners[i] = b.centre + b.u * (su * b.widthM) + b.v * (sv * b.heightM) + b.n * (sn * b.thicknessM);
		}
		for (int t = 0; t < 12; t++)
		{
			int i0 = BoxTrisRaw[t * 3];
			int i1 = BoxTrisRaw[t * 3 + 1];
			int i2 = BoxTrisRaw[t * 3 + 2];
			Vector3 e1 = corners[i1] - corners[i0];
			Vector3 e2 = corners[i2] - corners[i0];
			Vector3 n = Vector3.Cross(e1, e2);
			Vector3 face = (corners[i0] + corners[i1] + corners[i2]) / 3f;
			if (Vector3.Dot(face - b.centre, n) < 0f)
			{
				order[t * 3] = i0;
				order[t * 3 + 1] = i2;
				order[t * 3 + 2] = i1;
			}
			else
			{
				order[t * 3] = i0;
				order[t * 3 + 1] = i1;
				order[t * 3 + 2] = i2;
			}
		}
		return corners;
	}

	private static float Det3(Matrix4x4 m)
	{
		return m.m00 * (m.m11 * m.m22 - m.m12 * m.m21) - m.m01 * (m.m10 * m.m22 - m.m12 * m.m20) + m.m02 * (m.m10 * m.m21 - m.m11 * m.m20);
	}

	public static void ClipOutside(in SuperClip k, Vector3 a, Vector3 b, Vector3 c, List<Vector3> dst)
	{
		bool anyOutside = false;
		for (int p = 0; p < 6; p++)
		{
			float da = Dist(in k, p, a);
			float db = Dist(in k, p, b);
			float dc = Dist(in k, p, c);
			if (da > 0f && db > 0f && dc > 0f)
			{
				dst.Add(a);
				dst.Add(b);
				dst.Add(c);
				return;
			}
			if (da > 0f || db > 0f || dc > 0f)
			{
				anyOutside = true;
			}
		}
		if (!anyOutside)
		{
			return;
		}
		if (_poly == null)
		{
			_poly = new List<Vector3>(12);
			_front = new List<Vector3>(12);
			_back = new List<Vector3>(12);
		}
		_poly.Clear();
		_poly.Add(a);
		_poly.Add(b);
		_poly.Add(c);
		for (int p = 0; p < 6; p++)
		{
			if (_poly.Count < 3)
			{
				return;
			}
			Split(in k, p, _poly, _front, _back);
			Fan(_front, dst);
			_poly.Clear();
			_poly.AddRange(_back);
		}
	}

	private static float Dist(in SuperClip k, int p, Vector3 x)
	{
		Vector3 d = x - k.centre;
		switch (p)
		{
		case 0:
			return Vector3.Dot(d, k.u) - k.hu;
		case 1:
			return -Vector3.Dot(d, k.u) - k.hu;
		case 2:
			return Vector3.Dot(d, k.v) - k.hv;
		case 3:
			return -Vector3.Dot(d, k.v) - k.hv;
		case 4:
			return Vector3.Dot(d, k.n) - k.hn;
		default:
			return -Vector3.Dot(d, k.n) - k.hn;
		}
	}

	private static void Split(in SuperClip k, int p, List<Vector3> poly, List<Vector3> front, List<Vector3> back)
	{
		front.Clear();
		back.Clear();
		int n = poly.Count;
		if (n <= 0)
		{
			return;
		}
		for (int i = 0; i < n; i++)
		{
			Vector3 v = poly[i];
			Vector3 w = poly[(i + 1) % n];
			float d0 = Dist(in k, p, v);
			float d1 = Dist(in k, p, w);
			if (d0 > 0f)
			{
				front.Add(v);
			}
			else
			{
				back.Add(v);
			}
			if ((d0 > 0f) != (d1 > 0f))
			{
				Vector3 ip = v + (w - v) * (d0 / (d0 - d1));
				front.Add(ip);
				back.Add(ip);
			}
		}
	}

	private static void Fan(List<Vector3> poly, List<Vector3> dst)
	{
		for (int i = 1; i + 1 < poly.Count; i++)
		{
			dst.Add(poly[0]);
			dst.Add(poly[i]);
			dst.Add(poly[i + 1]);
		}
	}

	public byte[] Encode()
	{
		MemoryStream ms = new MemoryStream();
		using (BinaryWriter w = new BinaryWriter(ms))
		{
			w.Write(Format);
			W3(w, region.centre);
			W3(w, region.u);
			W3(w, region.v);
			W3(w, region.n);
			w.Write(region.hu);
			w.Write(region.hv);
			w.Write(region.hn);
			w.Write((uint)Math.Min(0xFFFFFFFFL, droppedTriangles));
			w.Write(pieces.Count);
			foreach (Piece p in pieces)
			{
				byte[] nameBytes = Encoding.UTF8.GetBytes(p.name ?? "");
				w.Write(nameBytes.Length);
				w.Write(nameBytes);
				w.Write((byte)p.kind);
				w.Write(p.TriangleCount);
				for (int i = 0; i < p.TriangleCount * 3; i++)
				{
					W3(w, p.vertices[i]);
				}
			}
		}
		return ms.ToArray();
	}

	private static void W3(BinaryWriter w, Vector3 v)
	{
		w.Write(v.x);
		w.Write(v.y);
		w.Write(v.z);
	}

	public static SuperDecor Decode(byte[] b)
	{
		if (b == null || b.Length < 1)
		{
			throw new InvalidDataException("декор ленты: секция пуста");
		}
		using MemoryStream ms = new MemoryStream(b);
		using BinaryReader r = new BinaryReader(ms);
		byte fmt = r.ReadByte();
		if (fmt != Format)
		{
			throw new InvalidDataException(string.Format("декор ленты: формат {0}, эта сборка читает {1}", fmt, Format));
		}
		SuperDecor d = new SuperDecor();
		d.region.present = true;
		d.region.centre = R3(r);
		d.region.u = R3(r);
		d.region.v = R3(r);
		d.region.n = R3(r);
		d.region.hu = r.ReadSingle();
		d.region.hv = r.ReadSingle();
		d.region.hn = r.ReadSingle();
		d.droppedTriangles = r.ReadUInt32();
		int count = r.ReadInt32();
		if (count > 65536)
		{
			throw new InvalidDataException(string.Format("декор ленты: частей {0}", count));
		}
		for (int i = 0; i < count; i++)
		{
			int nameLen = r.ReadInt32();
			if (nameLen < 0 || ms.Length - ms.Position < nameLen)
			{
				throw new InvalidDataException(string.Format("декор ленты: имя длиной {0}", nameLen));
			}
			byte[] nameBytes = r.ReadBytes(nameLen);
			string name = Encoding.UTF8.GetString(nameBytes);
			Kind kind = (Kind)r.ReadByte();
			int n = r.ReadInt32();
			if (n < 0 || ms.Length - ms.Position < n * 36)
			{
				throw new InvalidDataException(string.Format("декор ленты: часть «{0}» — {1} треугольников не помещаются в секцию", name, n));
			}
			Vector3[] verts = new Vector3[n * 3];
			for (int j = 0; j < verts.Length; j++)
			{
				verts[j] = R3(r);
			}
			Piece piece = new Piece();
			piece.name = name;
			piece.kind = kind;
			piece.vertices = verts;
			d.pieces.Add(piece);
		}
		if (ms.Position != ms.Length)
		{
			throw new InvalidDataException(string.Format("декор ленты: {0} лишних байт", ms.Length - ms.Position));
		}
		return d;
	}

	private static Vector3 R3(BinaryReader r)
	{
		return new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
	}

	public uint[] ObstacleMask(Vector3 origin, float h, int nx, int ny, int nz, int words, float skipM, out long marked)
	{
		if (h <= 0f || nx < 1 || ny < 1 || nz < 1)
		{
			throw new ArgumentException("SuperDecor.ObstacleMask: сетка пуста");
		}
		if ((long)words * 32L < (long)nx * ny * nz)
		{
			throw new ArgumentException(string.Format("SuperDecor.ObstacleMask: {0} слов не вмещают {1}×{2}×{3} узлов", words, nx, ny, nz));
		}
		uint[] mask = new uint[words];
		long markedLocal = 0L;
		float r = h * 0.5f * 1.7320508f;
		float rSq = r * r;
		int[] d = new int[3] { nx, ny, nz };
		int[] lo = new int[3];
		int[] hi = new int[3];
		foreach (Piece piece in pieces)
		{
			Vector3[] vs = piece.vertices;
			for (int t = 0; t + 2 < vs.Length; t += 3)
			{
				Vector3 v0 = vs[t];
				Vector3 v1 = vs[t + 1];
				Vector3 v2 = vs[t + 2];
				Vector3 min = new Vector3(Mathf.Min(v0.x, Mathf.Min(v1.x, v2.x)) - r, Mathf.Min(v0.y, Mathf.Min(v1.y, v2.y)) - r, Mathf.Min(v0.z, Mathf.Min(v1.z, v2.z)) - r);
				Vector3 max = new Vector3(Mathf.Max(v0.x, Mathf.Max(v1.x, v2.x)) + r, Mathf.Max(v0.y, Mathf.Max(v1.y, v2.y)) + r, Mathf.Max(v0.z, Mathf.Max(v1.z, v2.z)) + r);
				bool empty = false;
				for (int k = 0; k < 3; k++)
				{
					lo[k] = Math.Max(0, Mathf.FloorToInt((min[k] - origin[k]) / h));
					hi[k] = Math.Min(d[k] - 1, Mathf.CeilToInt((max[k] - origin[k]) / h));
					if (lo[k] > hi[k])
					{
						empty = true;
					}
				}
				if (empty)
				{
					continue;
				}
				Vector3 e1 = v1 - v0;
				Vector3 e2 = v2 - v0;
				Vector3 nrm = Vector3.Cross(e1, e2);
				float area = nrm.magnitude;
				int axis = -1;
				Vector3 nHat = Vector3.zero;
				float dd = 0f;
				int u = 0;
				int v = 1;
				if (area > 1e-12f)
				{
					nHat = nrm / area;
					float ax = Mathf.Abs(nHat.x);
					float ay = Mathf.Abs(nHat.y);
					float az = Mathf.Abs(nHat.z);
					axis = ((ax >= ay) ? ((ax >= az) ? 0 : 2) : ((ay >= az) ? 1 : 2));
					if (Mathf.Abs(nHat[axis]) < 1e-6f)
					{
						axis = -1;
					}
					else
					{
						u = (axis + 1) % 3;
						v = (axis + 2) % 3;
						dd = v0.x * nHat.x + v0.y * nHat.y + v0.z * nHat.z;
					}
				}
				for (int iu = lo[u]; iu <= hi[u]; iu++)
				{
					for (int iv = lo[v]; iv <= hi[v]; iv++)
					{
						int wLo;
						int wHi;
						if (axis >= 0)
						{
							int w = axis;
							float planeW = (dd - nHat[u] * (iu * h + origin[u]) - nHat[v] * (iv * h + origin[v])) / nHat[w];
							float rw = r / Mathf.Abs(nHat[w]);
							wLo = Math.Max(lo[w], Mathf.FloorToInt((planeW - rw - origin[w]) / h));
							wHi = Math.Min(hi[w], Mathf.CeilToInt((planeW + rw - origin[w]) / h));
						}
						else
						{
							int w = 3 - v - u;
							wLo = lo[w];
							wHi = hi[w];
						}
						for (int iw = wLo; iw <= wHi; iw++)
						{
							int x = (u == 0) ? iu : ((v == 0) ? iv : iw);
							int y = (u == 1) ? iu : ((v == 1) ? iv : iw);
							int z = (u == 2) ? iu : ((v == 2) ? iv : iw);
							long idx = ((long)z * ny + y) * nx + x;
							uint bit = 1u << (int)(idx & 31);
							int word = (int)(idx >> 5);
							if ((mask[word] & bit) == 0)
							{
								Vector3 cell = new Vector3(x * h + origin.x, y * h + origin.y, z * h + origin.z);
								float cheb = ChebyshevToRegion(cell);
								if (skipM < cheb)
								{
									float distSq = DistSqPointTriangle(cell, v0, v1, v2);
									if (distSq <= rSq)
									{
										mask[word] |= bit;
										markedLocal++;
									}
								}
							}
						}
					}
				}
			}
		}
		marked = markedLocal;
		return mask;
	}

	public float ChebyshevToRegion(Vector3 p)
	{
		Vector3 d = p - region.centre;
		float du = Mathf.Abs(Vector3.Dot(d, region.u)) - region.hu;
		float dv = Mathf.Abs(Vector3.Dot(d, region.v)) - region.hv;
		float dn = Mathf.Abs(Vector3.Dot(d, region.n)) - region.hn;
		return Mathf.Max(du, Mathf.Max(dv, dn));
	}

	public static float DistSqPointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
	{
		Vector3 ab = b - a;
		Vector3 ac = c - a;
		Vector3 ap = p - a;
		float d1 = Vector3.Dot(ab, ap);
		float d2 = Vector3.Dot(ac, ap);
		if (d1 <= 0f && d2 <= 0f)
		{
			return ap.sqrMagnitude;
		}
		Vector3 bp = p - b;
		float d3 = Vector3.Dot(ab, bp);
		float d4 = Vector3.Dot(ac, bp);
		if (d3 >= 0f && d4 <= d3)
		{
			return bp.sqrMagnitude;
		}
		float vc = d1 * d4 - d3 * d2;
		if (vc <= 0f && d1 >= 0f && d3 <= 0f)
		{
			float v = d1 / (d1 - d3);
			return (ap - ab * v).sqrMagnitude;
		}
		Vector3 cp = p - c;
		float d5 = Vector3.Dot(ab, cp);
		float d6 = Vector3.Dot(ac, cp);
		if (d6 >= 0f && d5 <= d6)
		{
			return cp.sqrMagnitude;
		}
		float vb = d5 * d2 - d1 * d6;
		if (vb <= 0f && d2 >= 0f && d6 <= 0f)
		{
			float w = d2 / (d2 - d6);
			return (ap - ac * w).sqrMagnitude;
		}
		float va = d3 * d6 - d5 * d4;
		if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
		{
			float d = (d4 - d3) / (d5 - d6 + (d4 - d3));
			return (p - (b + (c - b) * d)).sqrMagnitude;
		}
		float denom = 1f / (va + vb + vc);
		float v2 = vb * denom;
		float w2 = vc * denom;
		return (ap - ab * v2 - ac * w2).sqrMagnitude;
	}

	public Bounds Bounds()
	{
		Vector3 min = Vector3.zero;
		Vector3 max = Vector3.zero;
		bool any = false;
		foreach (Piece p in pieces)
		{
			Vector3[] vs = p.vertices;
			for (int i = 0; i < vs.Length; i++)
			{
				Vector3 v = vs[i];
				if (any)
				{
					min = Vector3.Min(min, v);
					max = Vector3.Max(max, v);
				}
				else
				{
					min = v;
					max = v;
					any = true;
				}
			}
		}
		Vector3 extents = (max - min) * 0.5f;
		return new Bounds(min + extents, extents);
	}
}
