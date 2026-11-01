using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static class MeshShapes
{
	public static Mesh RaggedRod(int radialSeg, float endJitter, float bend, int seed)
	{
		Random rnd = new Random(seed);
		float Rnd(float a, float b)
		{
			return (float)(rnd.NextDouble() * (b - a) + a);
		}
		Vector3 Axis(float y)
		{
			return new Vector3(Mathf.Sin((y + 0.5f) * Mathf.PI) * bend, 0f, 0f);
		}
		int n = Mathf.Max(6, radialSeg);
		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();
		for (int i = 0; i < n; i++)
		{
			float a = (float)i / n * Mathf.PI * 2f;
			verts.Add(Axis(0.5f) + new Vector3(Mathf.Cos(a) * 0.5f, 0.5f, Mathf.Sin(a) * 0.5f));
		}
		for (int i = 0; i < n; i++)
		{
			float a = (float)i / n * Mathf.PI * 2f;
			verts.Add(Axis(-0.5f) + new Vector3(Mathf.Cos(a) * 0.5f, -0.5f, Mathf.Sin(a) * 0.5f));
		}
		for (int i = 0; i < n; i++)
		{
			int j = (i + 1) % n;
			tris.Add(i);
			tris.Add(n + i);
			tris.Add(j);
			tris.Add(j);
			tris.Add(n + i);
			tris.Add(n + j);
		}
		RaggedCap(verts, tris, 0, n, 0.5f, 1f, endJitter, Rnd, top: true);
		RaggedCap(verts, tris, n, n, -0.5f, -1f, endJitter, Rnd, top: false);
		Mesh mesh = new Mesh();
		mesh.name = "RaggedRod";
		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		MeshNormals.RecalculateSmooth(mesh, 55f, weld: true);
		mesh.RecalculateBounds();
		return mesh;
	}

	private static void RaggedCap(List<Vector3> verts, List<int> tris, int ringStart, int n, float faceY, float outward, float jit, Func<float, float, float> Rnd, bool top)
	{
		for (int i = ringStart; i < ringStart + n; i++)
		{
			Vector3 v = verts[i];
			Vector2 d = new Vector2(v.x, v.z);
			if (d.sqrMagnitude > 1E-09f)
			{
				d.Normalize();
			}
			float r1 = Rnd(0f - jit, jit);
			float r2 = Rnd(0f - jit, jit);
			verts[i] = v + new Vector3(d.x * r1, r2 * outward, d.y * r1);
		}
		float cx = Rnd(0f - jit, jit);
		float cy = Rnd(0f, jit) * outward + faceY;
		float cz = Rnd(0f - jit, jit);
		int center = verts.Count;
		verts.Add(new Vector3(cx, cy, cz));
		for (int k = 0; k < n; k++)
		{
			int cur = ringStart + k;
			int next = ringStart + (k + 1) % n;
			if (top)
			{
				tris.Add(cur);
				tris.Add(center);
				tris.Add(next);
			}
			else
			{
				tris.Add(cur);
				tris.Add(next);
				tris.Add(center);
			}
		}
	}

	public static Mesh FinBlade(float sweep, float tipChord, float tipThick)
	{
		sweep = Mathf.Clamp01(sweep);
		tipChord = Mathf.Clamp(tipChord, 0.02f, 1f);
		tipThick = Mathf.Clamp(tipThick, 0.05f, 1f);
		float front = 0.5f - sweep;
		float back = front - tipChord;
		if (back < -0.5f)
		{
			back = -0.5f;
			front = Mathf.Min(0.5f, back + tipChord);
		}
		float halfThick = tipThick * 0.5f;
		List<Vector3> verts = new List<Vector3>();
		verts.Add(new Vector3(-0.5f, 0f, -0.5f));
		verts.Add(new Vector3(-0.5f, 0f, 0.5f));
		verts.Add(new Vector3(0.5f, 0f, 0.5f));
		verts.Add(new Vector3(0.5f, 0f, -0.5f));
		verts.Add(new Vector3(0f - halfThick, 1f, back));
		verts.Add(new Vector3(0f - halfThick, 1f, front));
		verts.Add(new Vector3(halfThick, 1f, front));
		verts.Add(new Vector3(halfThick, 1f, back));
		List<int> tris = new List<int>();
		tris.Add(0);
		tris.Add(1);
		tris.Add(5);
		tris.Add(0);
		tris.Add(5);
		tris.Add(4);
		tris.Add(3);
		tris.Add(7);
		tris.Add(6);
		tris.Add(3);
		tris.Add(6);
		tris.Add(2);
		tris.Add(1);
		tris.Add(2);
		tris.Add(6);
		tris.Add(1);
		tris.Add(6);
		tris.Add(5);
		tris.Add(0);
		tris.Add(4);
		tris.Add(7);
		tris.Add(0);
		tris.Add(7);
		tris.Add(3);
		tris.Add(4);
		tris.Add(5);
		tris.Add(6);
		tris.Add(4);
		tris.Add(6);
		tris.Add(7);
		tris.Add(0);
		tris.Add(3);
		tris.Add(2);
		tris.Add(0);
		tris.Add(2);
		tris.Add(1);
		Mesh mesh = new Mesh();
		mesh.name = "FinBlade";
		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	public static Mesh FinBladePiece(float sweep, float tipChord, float tipThick, float u0, float u1, int cut0, int cut1, int cuts, float jitter, int rows = 5)
	{
		sweep = Mathf.Clamp01(sweep);
		tipChord = Mathf.Clamp(tipChord, 0.02f, 1f);
		tipThick = Mathf.Clamp(tipThick, 0.05f, 1f);
		rows = Mathf.Max(2, rows);
		float Front(float y)
		{
			return Mathf.Lerp(0.5f, 0.5f - sweep, y);
		}
		float Chord(float y)
		{
			return Mathf.Lerp(1f, tipChord, y);
		}
		float Z(float y, float u)
		{
			return Front(y) - Mathf.Clamp01(u) * Chord(y);
		}
		float Half(float y)
		{
			return Mathf.Lerp(1f, tipThick, y) * 0.5f;
		}
		float Tear(int cut, int row)
		{
			if (cut < 1 || cuts <= cut)
			{
				return 0f;
			}
			float s = Mathf.Sin(cut * 37.719f + row * 12.9898f) * 43758.5469f;
			s -= MathF.Floor(s);
			return (s + s - 1f) * jitter;
		}
		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();
		for (int row = 0; row < rows; row++)
		{
			float t = (float)row / (rows - 1);
			float v0 = Mathf.Clamp01(u0 + Tear(cut0, row));
			float v1 = Mathf.Clamp01(u1 + Tear(cut1, row));
			if (v1 < v0 + 0.0001f)
			{
				v1 = Mathf.Min(v0 + 0.0001f, 1f);
			}
			float halfThick = Half(t);
			verts.Add(new Vector3(0f - halfThick, t, Z(t, v0)));
			verts.Add(new Vector3(0f - halfThick, t, Z(t, v1)));
			verts.Add(new Vector3(halfThick, t, Z(t, v1)));
			verts.Add(new Vector3(halfThick, t, Z(t, v0)));
		}
		for (int i = 0; i + 1 < rows; i++)
		{
			for (int k = 0; k < 4; k++)
			{
				int j = (k + 1) % 4;
				tris.Add(i * 4 + k);
				tris.Add((i + 1) * 4 + k);
				tris.Add(i * 4 + j);
				tris.Add(i * 4 + j);
				tris.Add((i + 1) * 4 + k);
				tris.Add((i + 1) * 4 + j);
			}
		}
		tris.Add(0);
		tris.Add(1);
		tris.Add(2);
		tris.Add(0);
		tris.Add(2);
		tris.Add(3);
		int last = (rows - 1) * 4;
		tris.Add(last + 2);
		tris.Add(last + 1);
		tris.Add(last);
		tris.Add(last + 3);
		tris.Add(last + 2);
		tris.Add(last);
		Mesh mesh = new Mesh();
		mesh.name = string.Format("FinBladePiece_{0}_{1}", cut0, cut1);
		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	public static Mesh PlateCell(int cx, int cy, int nx, int ny, float jitter)
	{
		Vector2 Node(int ix, int iy)
		{
			Vector2 p = new Vector2((float)ix / nx - 0.5f, (float)iy / ny - 0.5f);
			if (ix != 0 && ix != nx && iy != 0 && iy != ny && 0f < jitter)
			{
				int h = ix * 0x466F45D ^ iy * 0x127409F;
				p.x += ((h & 0x3FF) / 1023f - 0.5f) * jitter / nx;
				p.y += (((h >> 10) & 0x3FF) / 1023f - 0.5f) * jitter / ny;
			}
			return p;
		}
		Vector2 EdgeMid(int ax, int ay, int bx, int by)
		{
			Vector2 a = Node(ax, ay);
			Vector2 b = Node(bx, by);
			Vector2 mid = (a + b) * 0.5f;
			if ((ax == 0 && bx == 0) || (ax == nx && bx == nx) || (ay == 0 && by == 0) || (ay == ny && by == ny) || jitter <= 0f)
			{
				return mid;
			}
			int h1 = ay * 0x2915B14B ^ ax * 0x58894F7;
			int h2 = by * 0x2915B14B ^ bx * 0x58894F7;
			int h = (h1 < h2) ? (h1 * 31 + h2) : (h2 * 31 + h1);
			float f = ((h & 0x7FF) / 2047f - 0.5f) * jitter * 0.9f;
			return new Vector2(mid.x - (b.x - a.x) * f, mid.y + (b.y - a.y) * f);
		}
		Vector2[] ring = new Vector2[8]
		{
			Node(cx, cy),
			EdgeMid(cx, cy, cx + 1, cy),
			Node(cx + 1, cy),
			EdgeMid(cx + 1, cy, cx + 1, cy + 1),
			Node(cx + 1, cy + 1),
			EdgeMid(cx + 1, cy + 1, cx, cy + 1),
			Node(cx, cy + 1),
			EdgeMid(cx, cy + 1, cx, cy)
		};
		Vector2 c = Vector2.zero;
		for (int i = 0; i < 8; i++)
		{
			c += ring[i];
		}
		c *= 0.125f;
		float z = ((cy * -209 ^ cx * -79) & 511) / 511f * 0.5f + 0.75f;
		z *= 0.5f;
		Vector3[] verts = new Vector3[18];
		for (int i = 0; i < 8; i++)
		{
			verts[i] = new Vector3(ring[i].x, ring[i].y, 0f - z);
			verts[i + 8] = new Vector3(ring[i].x, ring[i].y, z);
		}
		verts[16] = new Vector3(c.x, c.y, 0f - z);
		verts[17] = new Vector3(c.x, c.y, z);
		List<int> tris = new List<int>(96);
		for (int i = 0; i < 8; i++)
		{
			int j = (i + 1) % 8;
			tris.Add(16);
			tris.Add(j);
			tris.Add(i);
			tris.Add(17);
			tris.Add(i + 8);
			tris.Add(j + 8);
			tris.Add(i);
			tris.Add(j);
			tris.Add(j + 8);
			tris.Add(i);
			tris.Add(j + 8);
			tris.Add(i + 8);
		}
		Mesh mesh = new Mesh();
		mesh.name = string.Format("PlateCell_{0}_{1}", cx, cy);
		mesh.vertices = verts;
		mesh.triangles = tris.ToArray();
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	public static Mesh Splinter(int seed)
	{
		Random rnd = new Random(seed);
		float R(float a, float b)
		{
			return (float)rnd.NextDouble() * (b - a) + a;
		}
		float a = R(0.28f, 0.42f);
		float b = R(0.07f, 0.13f);
		float c = R(-0.1f, 0.1f);
		float d = R(-0.05f, 0.05f);
		float e = R(-0.08f, 0.08f);
		float f = R(-0.04f, 0.04f);
		Vector3[] tetra = new Vector3[4];
		float s0 = R(0.8f, 1.2f);
		float t0 = R(0.7f, 1.3f);
		float z0 = R(-0.08f, 0.08f);
		tetra[0] = new Vector3(0f - a * s0, 0f - b * t0, z0);
		float s1 = R(0.8f, 1.2f);
		float t1 = R(0.7f, 1.3f);
		float z1 = R(-0.08f, 0.08f);
		tetra[1] = new Vector3(a * s1, 0f - b * t1, z1);
		float s2 = R(0.8f, 1.2f);
		float t2 = R(0.7f, 1.3f);
		float z2 = R(-0.08f, 0.08f);
		tetra[2] = new Vector3(a * s2, b * t2, z2);
		float s3 = R(0.8f, 1.2f);
		float t3 = R(0.7f, 1.3f);
		float z3 = R(-0.08f, 0.08f);
		tetra[3] = new Vector3(0f - a * s3, b * t3, z3);
		Vector3[] verts = new Vector3[6]
		{
			new Vector3(c, d, 0.5f),
			new Vector3(e, f, -0.5f),
			tetra[0],
			tetra[1],
			tetra[2],
			tetra[3]
		};
		List<int> tris = new List<int>(24);
		for (int i = 2; i < 6; i++)
		{
			int j = (i - 1) % 4 + 2;
			tris.Add(0);
			tris.Add(i);
			tris.Add(j);
			tris.Add(1);
			tris.Add(j);
			tris.Add(i);
		}
		Mesh mesh = new Mesh();
		mesh.name = "Splinter";
		mesh.vertices = verts;
		mesh.triangles = tris.ToArray();
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	public static Mesh BumpyChunk(int seed)
	{
		Random rnd = new Random(seed);
		float Rnd(float a, float b)
		{
			return (float)(rnd.NextDouble() * (b - a) + a);
		}
		BuildIco(out var verts, out var tris);
		float rx = Rnd(0.75f, 1.25f);
		float ry = Rnd(0.75f, 1.25f);
		float rz = Rnd(0.75f, 1.25f);
		for (int i = 0; i < verts.Count; i++)
		{
			Vector3 v = verts[i];
			Vector3 n = v;
			n.Normalize();
			float bump = (Rnd(-0.28f, 0.28f) + 1f) * 0.5f;
			verts[i] = new Vector3(n.x * bump * rx, n.y * bump * ry, n.z * bump * rz);
		}
		Mesh mesh = new Mesh();
		mesh.name = "BumpyChunk";
		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		MeshNormals.RecalculateSmooth(mesh, 40f, weld: true);
		mesh.RecalculateBounds();
		return mesh;
	}

	public static Mesh CasingPetal(float innerRadius, float thickness, float halfArcRad, float halfLength, int arcSeg, int axialSeg, int seed)
	{
		Random rnd = new Random(seed);
		int nu = Mathf.Max(3, arcSeg);
		int nv = Mathf.Max(2, axialSeg);
		innerRadius = Mathf.Max(innerRadius, 1E-05f);
		thickness = Mathf.Max(thickness, 1E-05f);
		halfArcRad = Mathf.Clamp(halfArcRad, 0.05f, Mathf.PI);
		halfLength = Mathf.Max(halfLength, 1E-05f);
		float Rnd(float a, float b)
		{
			return (float)(rnd.NextDouble() * (b - a) + a);
		}
		int Idx(int i, int j)
		{
			return (nu + 1) * j + i;
		}
		float crown = (float)(rnd.NextDouble() * 0.44 - 0.22);
		float[] arcs = new float[nv + 1];
		for (int j = 0; j <= nv; j++)
		{
			float t = (float)j / nv;
			float profile = Mathf.Clamp01(t * crown * (1f - t) + t);
			float wobble = (float)(rnd.NextDouble() * 0.24 + 0.88);
			arcs[j] = (Mathf.Sin(profile * Mathf.PI) * 0.6f + 0.4f) * halfArcRad * wobble;
		}
		Vector3[] pOut = new Vector3[(nv + 1) * (nu + 1)];
		Vector3[] pIn = new Vector3[(nv + 1) * (nu + 1)];
		for (int j = 0; j <= nv; j++)
		{
			for (int i = 0; i <= nu; i++)
			{
				float angle = Mathf.Lerp(0f - arcs[j], arcs[j], (float)i / nu);
				float y = Mathf.Lerp(0f - halfLength, halfLength, (float)j / nv);
				if (j == 0)
				{
					y -= Rnd(0f, 0.26f) * halfLength;
				}
				if (j == nv)
				{
					y += Rnd(0f, 0.26f) * halfLength;
				}
				if (i == 0)
				{
					angle -= Rnd(0f, 0.22f) * arcs[j];
				}
				if (i == nu)
				{
					angle += Rnd(0f, 0.22f) * arcs[j];
				}
				float rIn = (Rnd(-0.05f, 0.05f) + 1f) * innerRadius;
				float rOut = Rnd(0.6f, 1.4f) * thickness + rIn;
				pOut[Idx(i, j)] = new Vector3(rOut * Mathf.Sin(angle), rOut * Mathf.Cos(angle), y);
				pIn[Idx(i, j)] = new Vector3(rIn * Mathf.Sin(angle), rIn * Mathf.Cos(angle), y);
			}
		}
		List<Vector3> verts = new List<Vector3>(2 * ((nv + 1) * (nu + 1) + 2 * (nu + nv + 2)));
		List<int> tris = new List<int>(12 * (nv + 1) * (nu + 1));
		int start = verts.Count;
		verts.AddRange(pOut);
		int mid = verts.Count;
		verts.AddRange(pIn);
		for (int j = 0; j < nv; j++)
		{
			for (int i = 0; i < nu; i++)
			{
				int a = Idx(i, j);
				int b = Idx(i, j + 1);
				int c = Idx(i + 1, j);
				int d = Idx(i + 1, j + 1);
				tris.Add(start + a);
				tris.Add(start + b);
				tris.Add(start + c);
				tris.Add(start + c);
				tris.Add(start + b);
				tris.Add(start + d);
				tris.Add(mid + a);
				tris.Add(mid + c);
				tris.Add(mid + b);
				tris.Add(mid + c);
				tris.Add(mid + d);
				tris.Add(mid + b);
			}
		}
		void Wall(Func<int, int> node, int count, bool flip)
		{
			int wallStart = verts.Count;
			for (int k = 0; k < count; k++)
			{
				verts.Add(pOut[node(k)]);
			}
			for (int k = 0; k < count; k++)
			{
				verts.Add(pIn[node(k)]);
			}
			for (int k = 1; k < count; k++)
			{
				int a = wallStart + k - 1;
				int b = wallStart + k;
				int c = wallStart + count + k - 1;
				int d = wallStart + count + k;
				if (!flip)
				{
					tris.Add(a);
					tris.Add(b);
					tris.Add(c);
					tris.Add(b);
					tris.Add(d);
					tris.Add(c);
				}
				else
				{
					tris.Add(a);
					tris.Add(c);
					tris.Add(b);
					tris.Add(b);
					tris.Add(c);
					tris.Add(d);
				}
			}
		}
		Wall(k => k, nu + 1, flip: false);
		Wall(k => (nu + 1) * nv + k, nu + 1, flip: true);
		Wall(k => (nu + 1) * k + nu, nv + 1, flip: false);
		Wall(k => (nu + 1) * k, nv + 1, flip: true);
		float r = innerRadius + thickness * 0.5f;
		for (int i = 0; i < verts.Count; i++)
		{
			verts[i] = verts[i] - new Vector3(0f, r, 0f);
		}
		Mesh mesh = new Mesh();
		mesh.name = "CasingPetal";
		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		MeshNormals.RecalculateSmooth(mesh, 55f, weld: false);
		mesh.RecalculateBounds();
		return mesh;
	}

	private static void BuildIco(out List<Vector3> verts, out List<int> tris)
	{
		float t = (Mathf.Sqrt(5f) + 1f) * 0.5f;
		verts = new List<Vector3>();
		verts.Add(new Vector3(-1f, t, 0f));
		verts.Add(new Vector3(1f, t, 0f));
		verts.Add(new Vector3(-1f, 0f - t, 0f));
		verts.Add(new Vector3(1f, 0f - t, 0f));
		verts.Add(new Vector3(0f, -1f, t));
		verts.Add(new Vector3(0f, 1f, t));
		verts.Add(new Vector3(0f, -1f, 0f - t));
		verts.Add(new Vector3(0f, 1f, 0f - t));
		verts.Add(new Vector3(t, 0f, -1f));
		verts.Add(new Vector3(t, 0f, 1f));
		verts.Add(new Vector3(0f - t, 0f, -1f));
		verts.Add(new Vector3(0f - t, 0f, 1f));
		int[] faces = new int[60]
		{
			0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
			1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
			3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
			4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
		};
		Dictionary<long, int> mid = new Dictionary<long, int>();
		List<Vector3> vlist = verts;
		tris = new List<int>();
		int Mid(int a, int b)
		{
			long key = ((long)Math.Min(a, b) << 20) | Math.Max(a, b);
			if (mid.TryGetValue(key, out int i))
			{
				return i;
			}
			vlist.Add((vlist[a] + vlist[b]) * 0.5f);
			mid[key] = vlist.Count - 1;
			return vlist.Count - 1;
		}
		for (int f = 0; f < faces.Length; f += 3)
		{
			int a = faces[f];
			int b = faces[f + 1];
			int c = faces[f + 2];
			int ab = Mid(a, b);
			int bc = Mid(b, c);
			int ca = Mid(c, a);
			tris.AddRange(new int[12] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
		}
	}
}
