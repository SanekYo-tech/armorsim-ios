// PARTIAL: восстановлены FrameFor, LineThroughBox, Slab, FanPoint, OnLine, Separation, AxisGap,
// TipKey, Lattice, PointNeed, IndexWindow, ObliquityDeg, TiltAzimuthRad, RotZ, CutAzimuthDeg,
// SetInformational, FromBox, PlaceLegacyLayer, AlignInPlane, MeshOnLine, ShiftZ, Dot, Cross, ObbOf,
// Finite, SelfCheck (try/catch-обёртка над SelfCheckCore), SpecTipNeedForCheck, CheckSegment,
// InsideBox, Legacy, LegacyOff, Posed, PoseTR, RotX, RotY, RotXRad, Rodrigues, Rotated,
// BakerFrame.Point/Dir, D3, Obb.E, .cctor.
// НЕ восстановлены: TipZForClearance (обе перегрузки), RodColumnsBoxNeed, SpecTipNeed, BruteTipNeed,
// MeshTipNeed, MeshSurfaceEntry, SelfCheck, SelfCheckCore.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class SuperScenePose
{
	public struct BakerFrame
	{
		public Vector3 origin;

		public Vector3 x;

		public Vector3 y;

		public Vector3 z;

		public float zShift;

		public Vector3 Point(Vector3 w)
		{
			Vector3 d = w - origin;
			return new Vector3(Vector3.Dot(x, d), Vector3.Dot(y, d), Vector3.Dot(z, d) + zShift);
		}

		public Vector3 Dir(Vector3 w)
		{
			return new Vector3(Vector3.Dot(x, w), Vector3.Dot(y, w), Vector3.Dot(z, w));
		}
	}

	private struct D3
	{
		public double x;

		public double y;

		public double z;

		public D3(double x, double y, double z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public D3(Vector3 v)
		{
			x = v.x;
			y = v.y;
			z = v.z;
		}
	}

	private struct Obb
	{
		public D3 c;

		public D3 e0;

		public D3 e1;

		public D3 e2;

		public double h0;

		public double h1;

		public double h2;

		public D3 E(int i)
		{
			switch (i)
			{
			case 0:
				return e0;
			case 1:
				return e1;
			default:
				return e2;
			}
		}
	}

	public const int FanLines = 17;

	private const float SqrtHalf = 0.70710677f;

	private static readonly float[] FanCos;

	private static readonly float[] FanSin;

	private static bool _tipCacheValid;

	private static ulong _tipCacheKey;

	private static float _tipCacheValue;

	private static int _tipCacheBinding;

	public static bool SabotageLatticeOnlyTip;

	private static readonly Dictionary<string, double> _meshTipCache;

	private static readonly Dictionary<string, float> _surfCache;

	static SuperScenePose()
	{
		FanCos = new float[8] { 1f, SqrtHalf, 0f, -SqrtHalf, -1f, -SqrtHalf, 0f, SqrtHalf };
		FanSin = new float[8] { 0f, SqrtHalf, 1f, SqrtHalf, 0f, -SqrtHalf, -1f, -SqrtHalf };
		_meshTipCache = new Dictionary<string, double>();
		_surfCache = new Dictionary<string, float>();
	}

	public static BakerFrame FrameFor(Vector3 origin, Vector3 shotDir)
	{
		if (!Finite(origin) || !Finite(shotDir))
		{
			throw new ArgumentException(string.Format("SuperScenePose.FrameFor: начало {0} или направление выстрела {1} не конечны.", origin, shotDir));
		}
		float m = shotDir.magnitude;
		if (m <= 1e-06f)
		{
			throw new ArgumentException(string.Format("SuperScenePose.FrameFor: направление выстрела нулевой длины ({0:G3}) — оси выстрела нет.", m));
		}
		Vector3 n = new Vector3(-shotDir.x / m, -shotDir.y / m, -shotDir.z / m);
		Vector3 up = Vector3.up - n * n.y;
		if (up.magnitude < 1e-04f)
		{
			up = Vector3.forward - n * n.z;
		}
		up /= up.magnitude;
		Vector3 x = Vector3.Cross(up, n);
		BakerFrame bakerFrame = default(BakerFrame);
		bakerFrame.origin = origin;
		bakerFrame.x = x;
		bakerFrame.y = up;
		bakerFrame.z = n;
		bakerFrame.zShift = 0f;
		return bakerFrame;
	}

	public static bool LineThroughBox(in SuperPlateSpec s, float x, float y, out float zNear, out float zFar)
	{
		if (!Finite(x) || !Finite(y))
		{
			throw new ArgumentException(string.Format("SuperScenePose.LineThroughBox: точка прямой ({0}, {1}) не конечна.", x, y));
		}
		Obb b = ObbOf(in s, "LineThroughBox");
		double dx = (double)x - b.c.x;
		double dy = (double)y - b.c.y;
		double dz = -b.c.z;
		double lo = double.NegativeInfinity;
		double hi = double.PositiveInfinity;
		if (!Slab(b.e0, b.h0, dx, dy, dz, ref lo, ref hi))
		{
			zNear = float.NegativeInfinity;
			zFar = float.NegativeInfinity;
			return false;
		}
		if (!Slab(b.e1, b.h1, dx, dy, dz, ref lo, ref hi))
		{
			zNear = float.NegativeInfinity;
			zFar = float.NegativeInfinity;
			return false;
		}
		if (!Slab(b.e2, b.h2, dx, dy, dz, ref lo, ref hi))
		{
			zNear = float.NegativeInfinity;
			zFar = float.NegativeInfinity;
			return false;
		}
		if (Math.Abs(lo) == double.PositiveInfinity || Math.Abs(hi) == double.PositiveInfinity)
		{
			throw new ArgumentException("SuperScenePose.LineThroughBox: все три оси тела лежат поперёк выстрела — рамка вырождена.");
		}
		zFar = (float)lo;
		zNear = (float)hi;
		return true;
	}

	private static bool Slab(in D3 a, double h, double ox, double oy, double oz, ref double lo, ref double hi)
	{
		double dot = oy * a.y + ox * a.x + oz * a.z;
		if (Math.Abs(a.z) < 1e-09)
		{
			return h >= Math.Abs(dot);
		}
		double t1 = (h - dot) / a.z;
		double t2 = (-h - dot) / a.z;
		double min = t2;
		double max = t1;
		if (t2 > t1)
		{
			min = t1;
			max = t2;
		}
		if (min > lo)
		{
			lo = min;
		}
		if (hi > max)
		{
			hi = max;
		}
		return hi >= lo;
	}

	public static Vector2 FanPoint(int k, float r)
	{
		if (k == 0)
		{
			return Vector2.zero;
		}
		if (k >= 1 && k <= 8)
		{
			float half = r * 0.5f;
			return new Vector2(FanCos[k - 1] * half, half * FanSin[k - 1]);
		}
		if (k >= 9 && k <= 16)
		{
			return new Vector2(FanCos[k - 9] * r, r * FanSin[k - 9]);
		}
		throw new ArgumentOutOfRangeException("k", k, string.Format("SuperScenePose.FanPoint: у веера {0} прямых (0..{1}).", FanLines, FanLines - 1));
	}

	public static bool OnLine(in SuperPlateSpec s, float rodRadiusM, out float entryZ)
	{
		if (rodRadiusM < 0f || !Finite(rodRadiusM))
		{
			throw new ArgumentException(string.Format("SuperScenePose.OnLine: радиус стержня {0} — не конечное неотрицательное число.", rodRadiusM));
		}
		entryZ = float.NegativeInfinity;
		bool hit = false;
		for (int k = 0; k < FanLines; k++)
		{
			Vector2 p = FanPoint(k, rodRadiusM);
			if (LineThroughBox(in s, p.x, p.y, out float zNear, out float zFar))
			{
				hit = true;
				if (entryZ < zNear)
				{
					entryZ = zNear;
				}
			}
		}
		return hit;
	}

	public static float Separation(in SuperPlateSpec a, in SuperPlateSpec b)
	{
		Obb A = ObbOf(in a, "Separation(A)");
		Obb B = ObbOf(in b, "Separation(B)");
		D3 d = new D3(B.c.x - A.c.x, B.c.y - A.c.y, B.c.z - A.c.z);
		double best = double.NegativeInfinity;
		for (int i = 0; i < 3; i++)
		{
			if (AxisGap(A.E(i), in A, in B, in d, out double gap) && gap > best)
			{
				best = gap;
			}
			if (AxisGap(B.E(i), in A, in B, in d, out gap) && gap > best)
			{
				best = gap;
			}
		}
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				if (AxisGap(Cross(A.E(i), B.E(j)), in A, in B, in d, out double gap) && gap > best)
				{
					best = gap;
				}
			}
		}
		if (best == double.NegativeInfinity)
		{
			throw new InvalidOperationException("SuperScenePose.Separation: ни одной оси — оси тел нулевые.");
		}
		return (float)best;
	}

	private static bool AxisGap(D3 L, in Obb A, in Obb B, in D3 d, out double gap)
	{
		double ll = L.x * L.x + L.y * L.y + L.z * L.z;
		if (ll < 1e-12)
		{
			gap = 0.0;
			return false;
		}
		double inv = 1.0 / Math.Sqrt(ll);
		D3 n = new D3(inv * L.x, inv * L.y, inv * L.z);
		double projD = Dot(n, d);
		double projA0 = Dot(n, A.e0);
		double projA1 = Dot(n, A.e1);
		double projA2 = Dot(n, A.e2);
		double projB0 = Dot(n, B.e0);
		double projB1 = Dot(n, B.e1);
		double projB2 = Dot(n, B.e2);
		gap = Math.Abs(projD) - (Math.Abs(projA0) * A.h0 + Math.Abs(projA1) * A.h1 + Math.Abs(projA2) * A.h2) - (Math.Abs(projB0) * B.h0 + Math.Abs(projB1) * B.h1 + Math.Abs(projB2) * B.h2);
		return true;
	}

	public static float TipZForClearance(IList<SuperPlateSpec> specs, float sceneSpacingM, float rodRadiusM, float clearanceM, out int bindingSpec)
	{
		// PARTIAL: тело не восстановлено (0x1807528B0, ~250 строк ISIL: кэш _tipCacheValid/_tipCacheKey + перебор)
		bindingSpec = 0;
		return 0f;
	}

	private static ulong TipKey(IList<SuperPlateSpec> specs, float s, float r, float g)
	{
		ulong h = 14695981039346656037L;
		void F(float f)
		{
			h = (h ^ (ulong)(uint)BitConverter.SingleToInt32Bits(f)) * 1099511628211L;
		}
		void V(Vector3 v)
		{
			F(v.x);
			F(v.y);
			F(v.z);
		}
		F(s);
		F(r);
		F(g);
		F(specs.Count);
		for (int i = 0; i < specs.Count; i++)
		{
			SuperPlateSpec sp = specs[i];
			F(sp.thicknessM);
			F(sp.angleXDeg);
			F(sp.alongAxisM);
			F(sp.sideM);
			F(sp.spacingM);
			F(sp.offsetUM);
			F(sp.offsetVM);
			F(sp.fineCoreRadiusM);
			F(sp.hasPose ? 1f : 0f);
			V(sp.poseCentreM);
			V(sp.poseU);
			V(sp.poseV);
			V(sp.poseN);
			F(sp.widthM);
			F(sp.heightM);
		}
		return h;
	}

	private static float TipZForClearance(IList<SuperPlateSpec> specs, float sceneSpacingM, float rodRadiusM, float clearanceM, out int bindingSpec, ref long visited)
	{
		// PARTIAL: тело не восстановлено (0x180752220, ~230 строк ISIL)
		bindingSpec = 0;
		return 0f;
	}

	private static double RodColumnsBoxNeed(IList<SuperPlateSpec> specs, float s, float r, out int bindingSpec)
	{
		// PARTIAL: тело не восстановлено (0x180749610)
		bindingSpec = 0;
		return 0.0;
	}

	private static void Lattice(in SuperPlateSpec sp, float s, out int nt, out int nu, out int nv)
	{
		nt = Mathf.RoundToInt(sp.thicknessM / s);
		if (nt < 1)
		{
			nt = 1;
		}
		if (!sp.hasPose)
		{
			nu = Mathf.RoundToInt(sp.sideM / s);
			if (nu < 1)
			{
				nu = 1;
			}
			nv = nu;
		}
		else
		{
			nu = Mathf.RoundToInt(sp.widthM / s);
			if (nu < 1)
			{
				nu = 1;
			}
			nv = Mathf.RoundToInt(sp.heightM / s);
			if (nv < 1)
			{
				nv = 1;
			}
		}
	}

	private static double PointNeed(bool posed, Vector3 c, Vector3 u, Vector3 v, Vector3 n, float lx, float ly, float lz, double r, double g, double reach)
	{
		Vector3 p = (posed ? (lx * u + c + ly * v + lz * n) : (lx * u + ly * v + lz * n + c));
		double d = Math.Sqrt((double)p.x * (double)p.x + (double)p.y * (double)p.y);
		if (d <= r)
		{
			return (double)p.z + g;
		}
		if (d < reach)
		{
			return Math.Sqrt(g * g - (d - r) * (d - r)) + (double)p.z;
		}
		return double.NegativeInfinity;
	}

	private static double SpecTipNeed(in SuperPlateSpec sp, float s, double r, double g, string name, ref long visited)
	{
		// PARTIAL: тело не восстановлено (0x180750EF0)
		return 0.0;
	}

	private static bool IndexWindow(double lo, double hi, float s, double half, int count, out int iLo, out int iHi)
	{
		double a = Math.Ceiling(lo / (double)s + half);
		double b = Math.Floor(hi / (double)s + half);
		a = Math.Max(a, 0.0);
		if (b > (double)(count - 1))
		{
			b = count - 1;
		}
		if (b < a)
		{
			iLo = 0;
			iHi = -1;
		}
		else
		{
			iLo = (int)a;
			iHi = (int)b;
		}
		return b >= a;
	}

	private static double BruteTipNeed(in SuperPlateSpec sp, float s, double r, double g, out long total)
	{
		// PARTIAL: тело не восстановлено (0x180744CF0)
		total = 0L;
		return 0.0;
	}

	public static float ObliquityDeg(Vector3 n)
	{
		return MathF.Acos(Mathf.Clamp(Mathf.Abs(n.z), 0f, 1f)) * 57.29578f;
	}

	public static float TiltAzimuthRad(Vector3 n)
	{
		if (!Finite(n))
		{
			throw new ArgumentException(string.Format("SuperScenePose.TiltAzimuthRad: нормаль {0} не конечна.", n));
		}
		float len = MathF.Sqrt(n.x * n.x + n.y * n.y);
		if (len < 1e-06f)
		{
			return 0f;
		}
		return MathF.Atan2(n.x, -n.y);
	}

	public static Vector3 RotZ(Vector3 v, float phi)
	{
		float c = MathF.Cos(phi);
		float s = MathF.Sin(phi);
		return new Vector3(c * v.x - s * v.y, s * v.x + c * v.y, v.z);
	}

	public static float CutAzimuthDeg(Vector3 n)
	{
		if (!Finite(n))
		{
			throw new ArgumentException(string.Format("SuperScenePose.CutAzimuthDeg: нормаль {0} не конечна.", n));
		}
		float len = MathF.Sqrt(n.x * n.x + n.y * n.y);
		if (len < 1e-04f)
		{
			return 0f;
		}
		float a = (MathF.Atan2(n.y, n.x) * 57.29578f + 90f) / 30f;
		a = MathF.Round(a);
		a = a * 30f + 90f;
		a -= 180f * MathF.Floor(a / 180f);
		if (a < 0f)
		{
			a = 0f;
		}
		else if (a > 180f)
		{
			a = 180f;
		}
		return a - 90f;
	}

	public static void SetInformational(ref SuperPlateSpec s)
	{
		if (!s.hasPose)
		{
			throw new InvalidOperationException("SuperScenePose.SetInformational: у плиты нет позы (hasPose = false) — её angleXDeg/alongAxisM настоящие, а не справочные, и переписывать их нельзя.");
		}
		if (s.fineCoreRadiusM > 0f)
		{
			throw new InvalidOperationException(string.Format("SuperScenePose.SetInformational: плита с позой и мелким ядром S18 (R = {0:F1} мм) — ", s.fineCoreRadiusM * 1000f) + "мелкое ядро S18 с позой не запекается.");
		}
		s.angleXDeg = ObliquityDeg(s.poseN);
		s.sideM = MathF.Max(s.widthM, s.heightM);
		s.alongAxisM = s.poseCentreM.z + s.thicknessM * 0.5f * s.poseN.z;
		s.offsetUM = 0f;
		s.offsetVM = 0f;
	}

	public static SuperPlateSpec FromBox(Vector3 centreB, Vector3 uB, Vector3 vB, Vector3 nB, float widthM, float heightM, float thicknessM)
	{
		if (nB.z < 0f)
		{
			nB = -nB;
			uB = -uB;
		}
		SuperPlateSpec s = default(SuperPlateSpec);
		s.thicknessM = thicknessM;
		s.hasPose = true;
		s.poseCentreM = centreB;
		s.poseU = uB;
		s.poseV = vB;
		s.poseN = nB;
		s.widthM = widthM;
		s.heightM = heightM;
		SetInformational(ref s);
		return s;
	}

	public static SuperPlateSpec PlaceLegacyLayer(in SuperPlateSpec layerL, float phi, Vector3 anchorB)
	{
		if (layerL.hasPose)
		{
			throw new InvalidOperationException("SuperScenePose.PlaceLegacyLayer: слой уже с позой — переносится только прежний слой кадра L.");
		}
		if (layerL.fineCoreRadiusM > 0f)
		{
			throw new InvalidOperationException("SuperScenePose.PlaceLegacyLayer: слой с мелким ядром S18 — мелкое ядро S18 с позой не запекается.");
		}
		SuperBaker.PlateFrame(in layerL, out Vector3 centre, out Vector3 u, out Vector3 v, out Vector3 n, out float halfU, out float halfV);
		SuperPlateSpec s = default(SuperPlateSpec);
		s.thicknessM = layerL.thicknessM;
		s.angleXDeg = layerL.angleXDeg;
		s.material = layerL.material;
		s.clampedEdges = layerL.clampedEdges;
		s.noFragmentation = layerL.noFragmentation;
		s.spacingM = layerL.spacingM;
		s.hasPose = true;
		s.poseCentreM = RotZ(centre, phi) + anchorB;
		s.poseU = RotZ(u, phi);
		s.poseV = RotZ(v, phi);
		s.poseN = RotZ(n, phi);
		s.widthM = layerL.sideM;
		s.heightM = layerL.sideM;
		SetInformational(ref s);
		return s;
	}

	public static void AlignInPlane(ref SuperPlateSpec s, Vector3 uHint)
	{
		if (!s.hasPose)
		{
			throw new InvalidOperationException("SuperScenePose.AlignInPlane: у слоя нет позы — поворачивать в плоскости нечего.");
		}
		if (!Finite(uHint))
		{
			throw new ArgumentException(string.Format("SuperScenePose.AlignInPlane: подсказка оси {0} не конечна.", uHint));
		}
		Vector3 n = s.poseN;
		Vector3 u = uHint - n * Vector3.Dot(n, uHint);
		if (u.magnitude < 1e-04f)
		{
			return;
		}
		u /= u.magnitude;
		s.poseU = u;
		s.poseV = Vector3.Cross(n, u);
	}

	public static double MeshTipNeed(SuperMeshVoxels vx, int part, in SuperClip clip, float s, float rodSpacing, float r, float g)
	{
		// PARTIAL: тело не восстановлено (0x180747740)
		return 0.0;
	}

	public static bool MeshOnLine(SuperMeshVoxels vx, int part, in SuperClip clip, float s, float r, out float entryZ)
	{
		entryZ = float.NegativeInfinity;
		if (vx == null || part < 0 || vx.parts == null || part >= vx.parts.Length)
		{
			return false;
		}
		int[] cells = vx.parts[part].cells;
		bool hit = false;
		for (int t = 0; t < cells.Length; t += 3)
		{
			Vector3 c = clip.CellCentre(cells[t], cells[t + 1], cells[t + 2], s);
			double d = Math.Sqrt((double)c.x * (double)c.x + (double)c.y * (double)c.y);
			if (d <= (double)s * 0.5 + r)
			{
				hit = true;
				float z = s * 0.5f + c.z;
				if (entryZ < z)
				{
					entryZ = z;
				}
			}
		}
		return hit;
	}

	public static float MeshSurfaceEntry(ModelPart part, Matrix4x4 toBaker, float rodRadiusM, string keyPrefix)
	{
		// PARTIAL: тело не восстановлено (0x180746AA0, StringBuilder-ключ + кэш _surfCache)
		return 0f;
	}

	public static void ShiftZ(ref SuperPlateSpec s, float dz)
	{
		if (s.hasPose)
		{
			s.poseCentreM.z += dz;
		}
		s.alongAxisM += dz;
		s.clip.ShiftZ(dz);
	}

	private static double Dot(in D3 a, in D3 b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	private static D3 Cross(in D3 a, in D3 b)
	{
		return new D3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
	}

	private static Obb ObbOf(in SuperPlateSpec s, string who)
	{
		SuperBaker.PlateFrame(in s, out Vector3 centre, out Vector3 u, out Vector3 v, out Vector3 n, out float halfU, out float halfV);
		if (!Finite(centre) || !Finite(u) || !Finite(v) || !Finite(n) || !Finite(halfU) || !Finite(halfV) || !Finite(s.thicknessM))
		{
			throw new ArgumentException(string.Format("SuperScenePose.{0}: рамка тела не конечна (центр {1}, N {2}).", who, centre, n));
		}
		if (halfU <= 0f || halfV <= 0f || s.thicknessM <= 0f)
		{
			throw new ArgumentException(string.Format("SuperScenePose.{0}: у тела неположительный габарит (½U = {1:F3} мм, ½V = {2:F3} мм, ", who, halfU * 1000f, halfV * 1000f) + string.Format("t = {0:F3} мм) — коробки нет.", s.thicknessM * 1000f));
		}
		Obb o = default(Obb);
		o.c = new D3(centre);
		o.e0 = new D3(u);
		o.e1 = new D3(v);
		o.e2 = new D3(n);
		o.h0 = halfU;
		o.h1 = halfV;
		o.h2 = s.thicknessM * 0.5;
		return o;
	}

	private static bool Finite(float f)
	{
		return !float.IsNaN(f) && !float.IsInfinity(f);
	}

	private static bool Finite(Vector3 v)
	{
		return Finite(v.x) && Finite(v.y) && Finite(v.z);
	}

	public static string SelfCheck()
	{
		try
		{
			return SelfCheckCore();
		}
		catch (Exception ex)
		{
			return "самопроверка позы: исключение " + ex.GetType().FullName + ": " + ex;
		}
	}

	private static string SelfCheckCore()
	{
		// PARTIAL: тело не восстановлено (0x18074A200, ~5200 строк ISIL)
		return null;
	}

	private static double SpecTipNeedForCheck(SuperPlateSpec sp, float s, double r, double g)
	{
		long visited = 0L;
		return SpecTipNeed(in sp, s, r, g, "самопроверка", ref visited);
	}

	private static string CheckSegment(in SuperPlateSpec s, float x, float y, float zNear, float zFar)
	{
		if (zNear < zFar)
		{
			return string.Format("отрезок пересечения перевёрнут: вход {0}, выход {1}", zNear, zFar);
		}
		float mid = (zNear + zFar) * 0.5f;
		if (!InsideBox(in s, new Vector3(x, y, mid), 1e-06f))
		{
			return "середина отрезка пересечения вне коробки";
		}
		if (InsideBox(in s, new Vector3(x, y, zNear + 1e-04f), 0f))
		{
			return "точка за входом всё ещё в коробке — вход занижен";
		}
		if (InsideBox(in s, new Vector3(x, y, zFar - 1e-04f), 0f))
		{
			return "точка за выходом всё ещё в коробке — выход завышен";
		}
		return null;
	}

	private static bool InsideBox(in SuperPlateSpec s, Vector3 p, float tol)
	{
		SuperBaker.PlateFrame(in s, out Vector3 centre, out Vector3 u, out Vector3 v, out Vector3 n, out float halfU, out float halfV);
		Vector3 d = p - centre;
		if (halfU + tol < Mathf.Abs(Vector3.Dot(d, u)))
		{
			return false;
		}
		if (halfV + tol < Mathf.Abs(Vector3.Dot(d, v)))
		{
			return false;
		}
		return Mathf.Abs(Vector3.Dot(d, n)) <= s.thicknessM * 0.5f + tol;
	}

	private static SuperPlateSpec Legacy(float t, float angleDeg, float along, float side)
	{
		SuperPlateSpec s = default(SuperPlateSpec);
		s.thicknessM = t;
		s.angleXDeg = angleDeg;
		s.alongAxisM = along;
		s.sideM = side;
		return s;
	}

	private static SuperPlateSpec LegacyOff(float t, float angleDeg, float along, float side, float offU, float offV)
	{
		SuperPlateSpec s = default(SuperPlateSpec);
		s.thicknessM = t;
		s.angleXDeg = angleDeg;
		s.alongAxisM = along;
		s.sideM = side;
		s.offsetUM = offU;
		s.offsetVM = offV;
		return s;
	}

	private static SuperPlateSpec Posed(Vector3 c, Vector3 u, Vector3 v, Vector3 n, float w, float h, float t)
	{
		SuperPlateSpec s = default(SuperPlateSpec);
		s.thicknessM = t;
		s.hasPose = true;
		s.poseCentreM = c;
		s.poseU = u;
		s.poseV = v;
		s.poseN = n;
		s.widthM = w;
		s.heightM = h;
		SetInformational(ref s);
		return s;
	}

	private static SuperPlateSpec PoseTR(float thetaDeg, float phiDeg, float psiDeg, Vector3 c, float w, float h, float t)
	{
		float theta = thetaDeg * 0.017453292f;
		float phi = phiDeg * 0.017453292f;
		float psi = psiDeg * 0.017453292f;
		Vector3 u = RotZ(RotXRad(RotZ(new Vector3(1f, 0f, 0f), psi), theta), phi);
		Vector3 v = RotZ(RotXRad(RotZ(new Vector3(0f, 1f, 0f), psi), theta), phi);
		Vector3 n = RotZ(RotXRad(new Vector3(0f, 0f, 1f), theta), phi);
		return Posed(c, u, v, n, w, h, t);
	}

	private static Vector3 RotX(Vector3 p, float deg)
	{
		float rad = deg * 0.017453292f;
		float c = MathF.Cos(rad);
		float s = MathF.Sin(rad);
		return new Vector3(p.x, c * p.y - s * p.z, s * p.y + c * p.z);
	}

	private static Vector3 RotY(Vector3 p, float deg)
	{
		float rad = deg * 0.017453292f;
		float c = MathF.Cos(rad);
		float s = MathF.Sin(rad);
		return new Vector3(c * p.x + s * p.z, p.y, c * p.z - s * p.x);
	}

	private static Vector3 RotXRad(Vector3 p, float a)
	{
		float c = MathF.Cos(a);
		float s = MathF.Sin(a);
		return new Vector3(p.x, c * p.y - s * p.z, s * p.y + c * p.z);
	}

	private static Vector3 Rodrigues(Vector3 p, Vector3 k, float a)
	{
		float c = MathF.Cos(a);
		float s = MathF.Sin(a);
		return p * c + Vector3.Cross(k, p) * s + k * (Vector3.Dot(p, k) * (1f - c));
	}

	private static SuperPlateSpec Rotated(SuperPlateSpec s, Vector3 k, float a)
	{
		SuperBaker.PlateFrame(in s, out Vector3 centre, out Vector3 u, out Vector3 v, out Vector3 n, out float halfU, out float halfV);
		s.hasPose = true;
		s.poseCentreM = Rodrigues(centre, k, a);
		s.poseU = Rodrigues(u, k, a);
		s.poseV = Rodrigues(v, k, a);
		s.poseN = Rodrigues(n, k, a);
		s.widthM = halfU + halfU;
		s.heightM = halfV + halfV;
		return s;
	}
}
