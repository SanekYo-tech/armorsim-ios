// PARTIAL: восстановлены TryMaterial, Material, SpacingCeilingFor, ResolveSpacing, AlongAxisForDepth,
// AlongAxisForFaceOnAxis, PlateCentre, FaceOnAxis, PlateFrame, IsFinite, RodVolume, PlateVolume,
// ParticleBudgetOk, Baked.Dispose/Volume/RawVolume, RodTipForClearance, FarDepthM, RoundKeeps,
// RoundCount, InsideMesh, обёртки Bake (все 4) и BakePlates, ValidatePose, FillPlate, FillPlatePosed,
// FillRoundPosed, FillRod, FillProfile, FillJet, FillShell (полностью, 0x1806728A0), ShellBody,
// CountRodInsidePlates, AssertNoOverlap, EstimateParticles.
// НЕ восстановлены (оставлены заглушки, см. отчёт):
// - AssignXi (0x180664610): цикл по телам (List<Body>.GetEnumerator), ComposeCount.get_Fresh,
//   SuperXi.ScaleMultiplier(...)+SuperXi.Compose(...)+SuperXi.Measure(...), массив через SzArrayNew,
//   CultureInfo.InvariantCulture в тексте; собирает xiCompose.
// - BakeMeshBody (0x1806665B0): ValidatePose/Material/ResolveSpacing, SuperMeshCache.GetOrCompute
//   (MeshFillCalls++), итерация ячеек SuperClip.CellCentre/CellLive, PackLatKey, List.Add*;
//   выбрасывает InvalidOperationException с текстом (доступен в ISIL 1065-1088).
// - главная Bake (0x180668090, 7636 строк ISIL) — проверенная структура (для следующего агента):
//   1) локальные списки: pos(r14), vel, mid, volScale, cellVol, pin(r12), nofrag(r13), bodies(rsi);
//      latKey = new List<ulong>() ТОЛЬКО если SuperDamage.FailureStrainScatterOn и
//      FailureStrainScatterModulus>0 (не NaN) — тогда lambda=SuperXi.Lambda(m,""), иначе latKey=null;
//      m/salt из SuperDamage statics (m@0x44, salt@0x48), S43Alpha@0x50/Beta@0x58/VRef@0x60/HRef@0x68;
//      при SuperDamage.S43ScalingOn → S43Anchors(out vRefM3, out hRefM).
//   2) rodSpacing = ResolveSpacing(rod.spacingM, spacing, "стержень"); jetSpacing =
//      ResolveSpacing(jet.spacingM, spacing, "струя").
//   3) if (!withRod) → сразу к плитам (1772); иначе по kind:
//      kind==Proj.Shell: локальный rod из s (diam/len/vel/nose/tipZ из SuperShellSpec s),
//      coreRad = s.diameterM*0.5f*Clamp(s.coreDiameterFrac,0,1), capLenM = s.lengthM*s.capLengthFrac,
//      FillShell(rod,rodSpacing,Core,s,coreRad,capLenM,...,Material(s.coreMaterial),...) — без гейта;
//      тела «сердечник» (start=pos.Count до, count=после; normal=(0,0,-1); name/поля как ShellBody);
//      затем if (s.HasJacket) FillShell(...,Jacket,...,Material(s.jacketMaterial),...) + тело «рубашка»;
//      затем if (s.HasCap) FillShell(...,Cap,...,Material(s.capMaterial),...) + тело «колпак»;
//      после каждой ветви заполнения снаряда: while(pin.Count<pos.Count) pin.Add(0);
//      while(nofrag.Count<pos.Count) nofrag.Add(0); (инлайн List.Add, 0x181232AF0=AddWithResize).
//      kind==Proj.Jet: FillJet(копия jet, jetSpacing, ..., Material(jet.material)) + тело «струя»
//      (thickness=jet.lengthM, faceCentre=(0,0,jet.tipZM), spacingM=jetSpacing) + паддинги; goto плиты.
//      kind==Proj.Rod: if (rod.cavityFraction>0) SuperCavity.TryResolve(rod, rodSpacing, out cavity,
//      out failKey, out failArgs, out prose) — при false throw по failKey/failArgs (2960-2985);
//      if (rod.hasProfile): Validate(profile, rodSpacing, ...) → throw 3350; проверки: профиль
//      согласован со стержнем (длина/диаметр; 3136), для каждой станции i: OuterRadiusAt((i+0.5)*
//      rodSpacing) == NoseRadiusFactorAt(rod.nose,a,noseLen,r)*r, иначе throw 3062; regionCounts =
//      new List<int>(profile.RegionCount); regionMats = new List<ContinuumMaterialId>(RegionCount);
//      FillProfile(rod, rodSpacing, pos,vel,mid,volScale,cellVol,latKey, in cavity, regionCounts,
//      regionMats, out removed, out removedVolM3, out solidVolM3); тела по областям; иначе FillRod(...).
//   4) цикл плит (1779+): ValidatePose/Material/ResolveSpacing/FillPlate (не-меш) или BakeMeshBody
//      (меш)/PlateFrame + тела; 0x181243AA0 = List.Sort(Comparison) при склейке тел;
//   5) финал: Baked..ctor + SuperParticles..ctor, bodies.ToArray(), AssignXi(baked, mid, latKey, salt,
//      m, lambda, scatterOn, scalingOn, alpha, beta, vRefM3, hRefM), AssertNoOverlap (гейт
//      OverlapGateEnabled), проверка бюджета (EstimateParticles+ParticleBudgetOk → throw 3015+).
// ABI подсказка: слоты параметров позиционные (int→rcx/rdx/r8/r9, float→xmm0..3, далее стек).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class SuperBaker
{
	public struct Body
	{
		public int start;

		public int count;

		public ContinuumMaterialId material;

		public string name;

		public Vector3 normal;

		public float thicknessM;

		public Vector3 faceCentre;

		public Vector3 centre;

		public Vector3 axisU;

		public Vector3 axisV;

		public float halfSideM;

		public float halfUM;

		public float halfVM;

		public bool noFragmentation;

		public float spacingM;

		public float fineSpacingM;

		public float fineCoreRadiusM;

		public bool isMesh;

		public SuperMeshVoxels meshVoxels;

		public int meshPart;

		public int clipAnchored;
	}

	public sealed class Baked : IDisposable
	{
		public SuperParticles particles;

		public List<Body> bodies;

		public float spacing;

		public float[] cellVol;

		public bool hasRod;

		public int projectileBodyCount;

		public SuperCavity cavity;

		public int cavityRemoved;

		public double cavityMeasuredFraction;

		public double xiModulus;

		public uint xiSalt;

		public SuperXi.ComposeCount xiCompose;

		public void Dispose()
		{
			if (particles != null)
			{
				particles.Dispose();
			}
		}

		public double Volume(int body)
		{
			Body b = bodies[body];
			RadarKahan sum = default(RadarKahan);
			for (int i = b.start; i < b.start + b.count; i++)
			{
				sum.Add(particles.V0[i]);
			}
			return sum.Value;
		}

		public double RawVolume(int body)
		{
			Body b = bodies[body];
			RadarKahan sum = default(RadarKahan);
			for (int i = b.start; i < b.start + b.count; i++)
			{
				sum.Add(cellVol[i]);
			}
			return sum.Value;
		}
	}

	private enum Proj
	{
		Rod = 0,
		Shell = 1,
		Jet = 2
	}

	private enum ShellPart
	{
		Core = 0,
		Jacket = 1,
		Cap = 2
	}

	public static bool OverlapGateEnabled = true;

	public static int ProfileFillCalls;

	public static long ProfileFillParticles;

	public static int MeshFillCalls;

	public const long RamBytesPerParticle = 209L;

	public const float RamShareCeiling = 0.35f;

	public static bool TryMaterial(string name, out ContinuumMaterialId id)
	{
		string s = (name ?? "").Trim().ToUpperInvariant();
		switch (s)
		{
		case "WHA":
			id = ContinuumMaterialId.WHA;
			return true;
		case "STEEL4340":
			id = ContinuumMaterialId.Steel4340;
			return true;
		case "RHA_A":
			id = ContinuumMaterialId.RHA_MeyerKleponis;
			return true;
		case "RHA_B":
			id = ContinuumMaterialId.RHA_Grey;
			return true;
		case "RHA":
			id = ContinuumMaterials.ActiveRhaSet;
			return true;
		case "AL5083":
			id = ContinuumMaterialId.Al5083H116;
			return true;
		case "TI6AL4V":
			id = ContinuumMaterialId.Ti6Al4V;
			return true;
		case "COPPER":
			id = ContinuumMaterialId.OFHC_Copper;
			return true;
		case "HARDCORE_N12E":
			id = ContinuumMaterialId.HardCoreSteel_N12e;
			return true;
		case "POLY_INTERLAYER":
			id = ContinuumMaterialId.Polyurethane_Interlayer;
			return true;
		case "CAST_STEEL":
			id = ContinuumMaterialId.CastArmourSteel;
			return true;
		case "NEOPRENE_INTERLAYER":
			id = ContinuumMaterialId.Neoprene_Interlayer;
			return true;
		case "DU_075TI":
			id = ContinuumMaterialId.DepletedUranium;
			return true;
		}
		id = default(ContinuumMaterialId);
		return false;
	}

	public static ContinuumMaterialId Material(string name)
	{
		if (TryMaterial(name, out ContinuumMaterialId id))
		{
			return id;
		}
		throw new ArgumentException("SUPER: вещество «" + name + "» не известно запеканию. Реестр несёт провенанс (MaterialModels.cs), и подставлять похожее вместо неизвестного запрещено. Известны: RHA, RHA_A, RHA_B, WHA, STEEL4340, AL5083, TI6AL4V, COPPER, HARDCORE_N12E, POLY_INTERLAYER, CAST_STEEL, NEOPRENE_INTERLAYER, DU_075TI.");
	}

	public static float SpacingCeilingFor(float sceneSpacing)
	{
		return sceneSpacing + sceneSpacing;
	}

	public static float ResolveSpacing(float ownM, float sceneSpacing, string bodyName)
	{
		if (ownM <= 0f)
		{
			return sceneSpacing;
		}
		float floor = SpacingCeilingFor(sceneSpacing);
		if (ownM > floor * (1f + 1e-06f))
		{
			throw new ArgumentException(string.Format("SUPER: шаг тела «{0}» {1:F3} мм крупнее пола {2:F3} мм ", bodyName, ownM * 1000f, floor * 1000f) + string.Format("(одна частица на ячейку сетки h = {0:F3} мм) — тело перестало бы быть ", floor * 1000f) + "квадратурой своего объёма. Это отказ по имени, а не огрубление.");
		}
		if (ownM < (1f - 1e-06f) * sceneSpacing)
		{
			throw new ArgumentException(string.Format("SUPER: шаг тела «{0}» {1:F3} мм мельче шага сцены {2:F3} мм — ", bodyName, ownM * 1000f, sceneSpacing * 1000f) + "сетка такое тело не разрешит, а лишние частицы стоили бы денег впустую. Мелкое тело задаёт шаг сцены, а не наоборот.");
		}
		return ownM;
	}

	public static float AlongAxisForDepth(float thicknessM, float depthM, float angleXDeg)
	{
		float c = MathF.Cos(angleXDeg * 0.017453292f);
		if (MathF.Abs(c) < 0.001f)
		{
			return -depthM;
		}
		return -depthM / c - thicknessM / (c + c) + thicknessM * 0.5f;
	}

	public static float AlongAxisForFaceOnAxis(float thicknessM, float faceZ, float angleXDeg)
	{
		float c = MathF.Cos(angleXDeg * 0.017453292f);
		if (MathF.Abs(c) < 0.001f)
		{
			return faceZ;
		}
		return faceZ - thicknessM / (c + c) + thicknessM * 0.5f;
	}

	public static Vector3 PlateCentre(SuperPlateSpec pl)
	{
		if (!pl.hasPose)
		{
			float rad = pl.angleXDeg * 0.017453292f;
			return new Vector3(pl.offsetUM, MathF.Cos(rad) * pl.offsetVM, MathF.Sin(rad) * pl.offsetVM + (pl.alongAxisM - pl.thicknessM * 0.5f));
		}
		return pl.poseCentreM;
	}

	public static float FaceOnAxis(SuperPlateSpec p)
	{
		if (p.hasPose)
		{
			Vector3 face = p.poseCentreM + p.poseN * (p.thicknessM * 0.5f);
			if (MathF.Abs(p.poseN.z) < 0.001f)
			{
				return face.z;
			}
			return Vector3.Dot(p.poseN, face) / p.poseN.z;
		}
		float c = MathF.Cos(p.angleXDeg * 0.017453292f);
		if (MathF.Abs(c) < 0.001f)
		{
			return p.alongAxisM;
		}
		return p.alongAxisM - p.thicknessM * 0.5f + p.thicknessM / (c + c);
	}

	public static float RodTipForClearance(SuperPlateSpec front, float rodDiameterM, float clearanceM)
	{
		if (front.hasPose)
		{
			throw new InvalidOperationException("[запекание] SuperBaker.RodTipForClearance: передняя плита ПОЗИРОВАНА (S35) — формула по одному наклону вокруг X для неё неверна и похоронила бы кромку стержня в стали. Кончик позированной сцены ставит SuperScenePose.TipZForClearance (по решётке запекателя).");
		}
		float rad = front.angleXDeg * 0.017453292f;
		float c = MathF.Cos(rad);
		float s = MathF.Abs(MathF.Sin(rad));
		float half = MathF.Max(0f, rodDiameterM) * 0.5f;
		if (0.001f > MathF.Abs(c))
		{
			return FaceOnAxis(front) + clearanceM;
		}
		return FaceOnAxis(front) + (half * s + clearanceM) / c;
	}

	public static void PlateFrame(in SuperPlateSpec pl, out Vector3 centre, out Vector3 u, out Vector3 v, out Vector3 n, out float halfU, out float halfV)
	{
		if (!pl.hasPose)
		{
			float rad = pl.angleXDeg * 0.017453292f;
			float c = MathF.Cos(rad);
			float s = MathF.Sin(rad);
			centre = PlateCentre(pl);
			u = new Vector3(1f, 0f, 0f);
			v = new Vector3(0f, c, s);
			n = new Vector3(0f, -s, c);
			halfU = pl.sideM * 0.5f;
			halfV = pl.sideM * 0.5f;
		}
		else
		{
			centre = pl.poseCentreM;
			u = pl.poseU;
			v = pl.poseV;
			n = pl.poseN;
			halfU = pl.widthM * 0.5f;
			halfV = pl.heightM * 0.5f;
		}
	}

	public static void ValidatePose(in SuperPlateSpec pl, string bodyName)
	{
		if (string.IsNullOrEmpty(bodyName))
		{
			bodyName = "тело";
		}
		if (pl.fineCoreRadiusM > 0f)
		{
			throw new ArgumentException("SUPER: «" + bodyName + "»: мелкое ядро S18 с позой не запекается (радиус ядра " + string.Format("{0:F2} мм). Градуированная плита — только прежняя (наклон ", pl.fineCoreRadiusM * 1000f) + "вокруг X, на оси); это отказ по имени, а не молчаливая однородная плита.");
		}
		if (!IsFinite(pl.poseCentreM.x) || !IsFinite(pl.poseCentreM.y) || !IsFinite(pl.poseCentreM.z))
		{
			throw new ArgumentException(string.Format("SUPER: «{0}»: центр позы не конечен ({1}).", bodyName, pl.poseCentreM));
		}
		if (pl.widthM <= 0f || !IsFinite(pl.widthM) || pl.heightM <= 0f || !IsFinite(pl.heightM) || pl.thicknessM <= 0f || !IsFinite(pl.thicknessM))
		{
			throw new ArgumentException("SUPER: «" + bodyName + "»: размеры позированного тела обязаны быть положительны и конечны: " + string.Format("ширина {0:F3} мм, высота {1:F3} мм, толщина ", pl.widthM * 1000f, pl.heightM * 1000f) + string.Format("{0:F3} мм.", pl.thicknessM * 1000f));
		}
		float magU = pl.poseU.magnitude;
		float magV = pl.poseV.magnitude;
		float magN = pl.poseN.magnitude;
		if (1e-4f < MathF.Abs(magU - 1f) || 1e-4f < MathF.Abs(magV - 1f) || 1e-4f < MathF.Abs(magN - 1f))
		{
			throw new ArgumentException(string.Format("SUPER: «{0}»: оси позы не единичны: |U| = {1:F6}, |V| = {2:F6}, |N| = {3:F6} ", bodyName, magU, magV, magN) + string.Format("(допуск {0:G}). Поза — поворот, а не растяжение.", 1e-4f));
		}
		float uv = Vector3.Dot(pl.poseU, pl.poseV);
		float un = Vector3.Dot(pl.poseU, pl.poseN);
		float vn = Vector3.Dot(pl.poseV, pl.poseN);
		if (1e-4f < MathF.Abs(uv) || 1e-4f < MathF.Abs(un) || 1e-4f < MathF.Abs(vn))
		{
			throw new ArgumentException(string.Format("SUPER: «{0}»: оси позы не ортогональны: U·V = {1:E2}, U·N = {2:E2}, V·N = {3:E2} ", bodyName, uv, un, vn) + string.Format("(допуск {0:G}). Коробка со скошенными осями — не коробка.", 1e-4f));
		}
		float handed = Vector3.Dot(Vector3.Cross(pl.poseU, pl.poseV), pl.poseN);
		if (handed < 0.999f)
		{
			throw new ArgumentException(string.Format("SUPER: «{0}»: тройка осей позы ЛЕВАЯ или вырождена: (U×V)·N = {1:F6} (нужно ⩾ 0.999). ", bodyName, handed) + "Зеркало превратило бы плиту, повёрнутую влево, в повёрнутую вправо.");
		}
		if (pl.poseN.z < -1e-06f)
		{
			throw new ArgumentException(string.Format("SUPER: «{0}»: лицевая нормаль позы смотрит ОТ стержня: N.z = {1:F6} ", bodyName, pl.poseN.z) + "(стержень летит в −z, лицевая грань — со стороны большего z, N.z ⩾ 0).");
		}
	}

	private static bool IsFinite(float x)
	{
		return !float.IsNaN(x) && !float.IsInfinity(x);
	}

	public static float FarDepthM(in SuperPlateSpec p)
	{
		if (!p.hasPose)
		{
			return MathF.Abs(p.alongAxisM) + p.thicknessM;
		}
		PlateFrame(in p, out Vector3 centre, out Vector3 u, out Vector3 v, out Vector3 n, out float halfU, out float halfV);
		return -(centre.z - MathF.Abs(u.z) * halfU - MathF.Abs(v.z) * halfV - MathF.Abs(n.z) * 0.5f * p.thicknessM);
	}

	public static Baked Bake(SuperRodSpec rod, IList<SuperPlateSpec> plates, float spacing)
	{
		return Bake(rod, plates, spacing, withRod: true);
	}

	public static Baked BakePlates(IList<SuperPlateSpec> plates, float spacing)
	{
		return Bake(default(SuperRodSpec), plates, spacing, withRod: false);
	}

	public static Baked Bake(SuperShellSpec shell, IList<SuperPlateSpec> plates, float spacing)
	{
		return Bake(default(SuperRodSpec), shell, default(SuperJetSpec), plates, spacing, withRod: true, Proj.Shell);
	}

	public static Baked Bake(SuperJetSpec jet, IList<SuperPlateSpec> plates, float spacing)
	{
		return Bake(default(SuperRodSpec), default(SuperShellSpec), jet, plates, spacing, withRod: true, Proj.Jet);
	}

	private static Baked Bake(SuperRodSpec rod, IList<SuperPlateSpec> plates, float spacing, bool withRod)
	{
		return Bake(rod, default(SuperShellSpec), default(SuperJetSpec), plates, spacing, withRod, Proj.Rod);
	}

	private static Baked Bake(SuperRodSpec rod, SuperShellSpec shell, SuperJetSpec jet, IList<SuperPlateSpec> plates, float spacing, bool withRod, Proj kind)
	{
		// PARTIAL: тело не восстановлено (0x180668090)
		return null;
	}

	private static void AssignXi(Baked baked, List<ContinuumMaterialId> mid, List<ulong> latKey, uint salt, double m, double lambda, bool scatterOn, bool scalingOn, double alpha, double beta, double vRefM3, double hRefM)
	{
		// PARTIAL (0x180664610). Проверено по ISIL:
		// ComposeCount[] compose = new ComposeCount[baked.bodies.Count];
		// начальное значение — ComposeCount.get_Fresh() (48-байтный struct, поля на [rax], [rax+16], [rax+32]);
		// цикл по телам (foreach baked.bodies, get_Item): для каждого i при latKey != null
		//   ulong key = latKey[i]; double mult = SuperXi.ScaleMultiplier(key, vRef/hRef-параметры...);
		//   иначе mult = 1; далее mid[i] (List.get_Item) и SuperXi.Compose(...) с параметрами
		//   (salt, m, lambda, scatterOn, scalingOn, alpha, beta, vRefM3, hRefM) — 8 аргументов+;
		// в конце SuperXi.Measure(...), CultureInfo.InvariantCulture, String.Concat для текста/проверки.
	}

	public static int CountRodInsidePlates(Baked b, out float deepestM, out int worstBody)
	{
		deepestM = 0f;
		worstBody = -1;
		if (!b.hasRod)
		{
			return 0;
		}
		if (b.bodies.Count < 2)
		{
			return 0;
		}
		int firstPlate = Math.Max(1, b.projectileBodyCount);
		int start = b.bodies[0].start;
		int end = b.bodies[firstPlate - 1].start + b.bodies[firstPlate - 1].count;
		int count = 0;
		for (int bodyIdx = firstPlate; bodyIdx < b.bodies.Count; bodyIdx++)
		{
			Body body = b.bodies[bodyIdx];
			if (body.isMesh)
			{
				Vector3 n = body.normal.normalized;
				float faceDot = Vector3.Dot(n, body.faceCentre);
				for (int i = start; i < end; i++)
				{
					Vector3 p = b.particles.x[i];
					if (!InsideMesh(in body, p))
					{
						continue;
					}
					count++;
					float depth = faceDot - Vector3.Dot(n, p);
					if (depth > deepestM)
					{
						deepestM = depth;
						worstBody = bodyIdx;
					}
				}
			}
			else
			{
				if (0f >= body.halfSideM)
				{
					continue;
				}
				Vector3 n = body.normal.normalized;
				float faceDot = Vector3.Dot(n, body.faceCentre);
				for (int i = start; i < end; i++)
				{
					Vector3 p = b.particles.x[i];
					Vector3 rel = p - body.centre;
					if (MathF.Abs(Vector3.Dot(rel, body.axisU)) > body.halfUM)
					{
						continue;
					}
					if (MathF.Abs(Vector3.Dot(rel, body.axisV)) > body.halfVM)
					{
						continue;
					}
					float depth = faceDot - Vector3.Dot(n, p);
					if (0f >= depth)
					{
						continue;
					}
					if (depth >= body.thicknessM)
					{
						continue;
					}
					count++;
					if (depth > deepestM)
					{
						deepestM = depth;
						worstBody = bodyIdx;
					}
				}
			}
		}
		return count;
	}

	private static void AssertNoOverlap(Baked b)
	{
		int inside = CountRodInsidePlates(b, out float deepestM, out int worstBody);
		if (inside == 0)
		{
			return;
		}
		int n = Mathf.Max(1, b.projectileBodyCount);
		int sum = 0;
		for (int i = 0; i < n && i < b.bodies.Count; i++)
		{
			sum = sum + b.bodies[i].count;
		}
		float share = 0f;
		if (sum > 0)
		{
			share = (float)inside / (float)sum;
		}
		b.Dispose();
		throw new InvalidOperationException(string.Format("[запекание] СТЕРЖЕНЬ РОЖДЁН ВНУТРИ СТАЛИ: {0} частиц ({1:P1} стержня) ", inside, share) + string.Format("внутри тела {0}, глубже всего {1:F2} мм. Это не возмущение — это ", worstBody, deepestM * 1000f) + "другой опыт, и считать его нельзя. Причина почти всегда одна: плиты разложены ПО ОСИ вместо НОРМАЛИ, и с ростом угла лицевая грань уезжает на t/(2·cos θ). Ставь плиты через SuperBaker.AlongAxisForDepth, а кончик стержня — через SuperBaker.RodTipForClearance. Позированная сцена (S35) — кончик только через SuperScenePose.TipZForClearance (по решётке запекателя).");
	}

	private static void FillJet(SuperJetSpec jet, float spacing, List<Vector3> pos, List<Vector3> vel, ContinuumMaterialId mat, List<ContinuumMaterialId> mid, List<float> volScale, List<float> cellVol, List<ulong> latKey)
	{
		int nz = Math.Max(1, Mathf.RoundToInt(jet.lengthM / spacing));
		int nr = Math.Max(1, (int)Math.Ceiling((double)(jet.diameterM / spacing)));
		float r = jet.diameterM * 0.5f;
		float r2 = r * r;
		float cell = spacing * spacing * spacing;
		for (int i = 0; i < nz; i++)
		{
			float a = (i + 0.5f) * spacing;
			float z = a + jet.tipZM;
			float vz = 0f - jet.VelocityAt(a);
			float half = (nr - 1) * 0.5f;
			int kept = 0;
			for (int j = 0; j < nr; j++)
			{
				float fy = (j - half) * spacing;
				for (int k = 0; k < nr; k++)
				{
					float fx = (k - half) * spacing;
					if (fx * fx + fy * fy > r2)
					{
						continue;
					}
					pos.Add(new Vector3(fx, fy, z));
					vel.Add(new Vector3(0f, 0f, vz));
					mid.Add(mat);
					cellVol.Add(cell);
					if (latKey != null)
					{
						latKey.Add(SuperXi.PackLatKey(i, j, k, 8));
					}
					kept++;
				}
			}
			float vol = (float)kept * cell;
			float scale;
			if (kept > 0 && vol > 0f)
			{
				scale = r2 * MathF.PI * spacing / vol;
			}
			else
			{
				scale = 1f;
			}
			for (int q = 0; q < kept; q++)
			{
				volScale.Add(scale);
			}
		}
	}

	private static Body ShellBody(int start, int end, ContinuumMaterialId mat, string name, SuperShellSpec s, float spacing)
	{
		Body b = default(Body);
		b.start = start;
		b.count = end - start;
		b.material = mat;
		b.name = name;
		b.normal = new Vector3(0f, 0f, -1f);
		b.thicknessM = s.lengthM;
		b.faceCentre = new Vector3(0f, 0f, s.tipZM);
		b.spacingM = spacing;
		return b;
	}

	private static void FillShell(SuperRodSpec rod, float spacing, ShellPart part, SuperShellSpec s, float coreRad, float capLenM, List<Vector3> pos, List<Vector3> vel, ContinuumMaterialId mat, List<ContinuumMaterialId> mid, List<float> volScale, List<float> cellVol, List<ulong> latKey)
	{
		float r = rod.diameterM * 0.5f;
		float noseLen = RodGeometry.NoseLengthM(rod.diameterM, rod.lengthM);
		int nz = Math.Max(1, Mathf.RoundToInt(rod.lengthM / spacing));
		int nr = Math.Max(1, (int)Math.Ceiling((double)(rod.diameterM / spacing)));
		float vz = 0f - rod.velocityMps;
		float cell = spacing * spacing * spacing;
		bool hasJacket = s.HasJacket;
		bool hasCap = s.HasCap;
		float coreR2 = coreRad * coreRad;
		for (int i = 0; i < nz; i++)
		{
			float a = (i + 0.5f) * spacing;
			float z = rod.tipZM + a;
			float rAt = RodGeometry.NoseRadiusFactorAt(rod.nose, a, noseLen, r) * r;
			float rAt2 = rAt * rAt;
			int kept = 0;
			for (int j = 0; j < nr; j++)
			{
				float fy = (j - (nr - 1) * 0.5f) * spacing;
				for (int k = 0; k < nr; k++)
				{
					float fx = (k - (nr - 1) * 0.5f) * spacing;
					float d2 = fx * fx + fy * fy;
					if (d2 <= rAt2)
					{
						kept++;
					}
				}
			}
			float vol = (float)kept * cell;
			float scale;
			if (kept > 0 && vol > 0f)
			{
				scale = rAt * MathF.PI * rAt * spacing / vol;
			}
			else
			{
				scale = 1f;
			}
			for (int j = 0; j < nr; j++)
			{
				float fy = (j - (nr - 1) * 0.5f) * spacing;
				for (int k = 0; k < nr; k++)
				{
					float fx = (k - (nr - 1) * 0.5f) * spacing;
					float d2 = fx * fx + fy * fy;
					if (d2 > rAt2)
					{
						continue;
					}
					int owner = 0;
					if (hasCap && capLenM > a)
					{
						owner = 2;
					}
					else if (hasJacket && d2 > coreR2)
					{
						owner = 1;
					}
					if (owner != (int)part)
					{
						continue;
					}
					pos.Add(new Vector3(fx, fy, z));
					vel.Add(new Vector3(0f, 0f, vz));
					mid.Add(mat);
					volScale.Add(scale);
					cellVol.Add(cell);
					if (latKey != null)
					{
						latKey.Add(SuperXi.PackLatKey(i, j, k, (int)part));
					}
				}
			}
		}
	}

	private static void FillRod(SuperRodSpec rod, float spacing, List<Vector3> pos, List<Vector3> vel, ContinuumMaterialId mat, List<ContinuumMaterialId> mid, List<float> volScale, List<float> cellVol, List<ulong> latKey, in SuperCavity cavity, out int removed, out double removedVolM3, out double solidVolM3)
	{
		float r = rod.diameterM * 0.5f;
		float noseLen = RodGeometry.NoseLengthM(rod.diameterM, rod.lengthM);
		int nz = Math.Max(1, Mathf.RoundToInt(rod.lengthM / spacing));
		int nr = Math.Max(1, (int)Math.Ceiling((double)(rod.diameterM / spacing)));
		float vz = 0f - rod.velocityMps;
		float cavityR2 = cavity.present ? (cavity.radiusM * cavity.radiusM) : 0f;
		int initial = pos.Count;
		removed = 0;
		float cell = spacing * spacing * spacing;
		for (int i = 0; i < nz; i++)
		{
			float a = (i + 0.5f) * spacing;
			float z = a + rod.tipZM;
			float rAt = RodGeometry.NoseRadiusFactorAt(rod.nose, a, noseLen, r) * r;
			float rAt2 = rAt * rAt;
			int stationStart = pos.Count;
			if (cavity.present && i >= cavity.firstLayer && i <= cavity.lastLayer)
			{
				int removedHere = 0;
				float half = (nr - 1) * 0.5f;
				for (int j = 0; j < nr; j++)
				{
					float fy = (j - half) * spacing;
					for (int k = 0; k < nr; k++)
					{
						float fx = (k - half) * spacing;
						float d2 = fx * fx + fy * fy;
						if (d2 > rAt2)
						{
							continue;
						}
						if (cavityR2 > d2)
						{
							removedHere++;
							continue;
						}
						pos.Add(new Vector3(fx, fy, z));
						vel.Add(new Vector3(0f, 0f, vz));
						mid.Add(mat);
						cellVol.Add(cell);
						if (latKey != null)
						{
							latKey.Add(SuperXi.PackLatKey(i, j, k, 8));
						}
					}
				}
				int kept = pos.Count - stationStart;
				float vol = (float)kept * cell;
				float scale;
				if (kept > 0 && vol > 0f)
				{
					scale = (rAt * rAt - cavityR2) * MathF.PI * spacing / vol;
				}
				else
				{
					scale = 1f;
				}
				for (int q = 0; q < kept; q++)
				{
					volScale.Add(scale);
				}
				removed += removedHere;
			}
			else
			{
				float half = (nr - 1) * 0.5f;
				for (int j = 0; j < nr; j++)
				{
					float fy = (j - half) * spacing;
					for (int k = 0; k < nr; k++)
					{
						float fx = (k - half) * spacing;
						float d2 = fx * fx + fy * fy;
						if (d2 > rAt2)
						{
							continue;
						}
						pos.Add(new Vector3(fx, fy, z));
						vel.Add(new Vector3(0f, 0f, vz));
						mid.Add(mat);
						cellVol.Add(cell);
						if (latKey != null)
						{
							latKey.Add(SuperXi.PackLatKey(i, j, k, 8));
						}
					}
				}
				int kept = pos.Count - stationStart;
				float vol = (float)kept * cell;
				float scale;
				if (kept > 0 && vol > 0f)
				{
					scale = rAt * MathF.PI * rAt * spacing / vol;
				}
				else
				{
					scale = 1f;
				}
				for (int q = 0; q < kept; q++)
				{
					volScale.Add(scale);
				}
			}
		}
		removedVolM3 = (double)removed * cell;
		solidVolM3 = (double)((pos.Count - initial) + removed) * cell;
	}

	private static void FillProfile(SuperRodSpec rod, float spacing, List<Vector3> pos, List<Vector3> vel, List<ContinuumMaterialId> mid, List<float> volScale, List<float> cellVol, List<ulong> latKey, in SuperCavity cavity, List<int> regionCounts, List<ContinuumMaterialId> regionMats, out int removed, out double removedVolM3, out double solidVolM3)
	{
		SuperShellProfile profile = rod.profile;
		ProfileFillCalls++;
		int nz = Math.Max(1, Mathf.RoundToInt(profile.LengthM / spacing));
		int nr = Math.Max(1, (int)Math.Ceiling((double)(profile.diameterM / spacing)));
		float vz = 0f - rod.velocityMps;
		float cavityR2 = cavity.present ? (cavity.radiusM * cavity.radiusM) : 0f;
		int initial = pos.Count;
		removed = 0;
		float cell = spacing * spacing * spacing;
		for (int regionIdx = 0; regionIdx < profile.regions.Length; regionIdx++)
		{
			ContinuumMaterialId regionMat = Material(profile.regions[regionIdx].materialId);
			regionMats.Add(regionMat);
			int sub = profile.regions[regionIdx].latSub;
			int regionStart = pos.Count;
			for (int i = 0; i < nz; i++)
			{
				float a = (i + 0.5f) * spacing;
				float z = rod.tipZM + a;
				float rAt = profile.OuterRadiusAt(a);
				float rAt2 = rAt * rAt;
				bool inSpan = cavity.present && i >= cavity.firstLayer && i <= cavity.lastLayer;
				int kept = 0;
				int removedHere = 0;
				float half = (nr - 1) * 0.5f;
				for (int j = 0; j < nr; j++)
				{
					float fy = (j - half) * spacing;
					for (int k = 0; k < nr; k++)
					{
						float fx = (k - half) * spacing;
						float d2 = fx * fx + fy * fy;
						if (d2 > rAt2)
						{
							continue;
						}
						if (inSpan && cavityR2 > d2)
						{
							removedHere++;
							continue;
						}
						kept++;
						if (regionIdx == 0)
						{
							int owners = profile.OwnerCount(a, d2);
							if (owners != 1)
							{
								throw new InvalidOperationException("[запекание] S41 [super.fail.profile_regions] узел решётки " + string.Format("(слой {0}, ряд {1}, столбец {2}; станция {3:F3} мм, ", i, j, k, a * 1000f) + string.Format("радиус {0:F3} мм) принадлежит {1} ", MathF.Sqrt(d2) * 1000f, owners) + "областям профиля вместо одной: коробки областей обязаны не пересекаться и покрывать тело.");
							}
						}
					}
				}
				float vol = (float)kept * cell;
				float scale;
				if (kept > 0 && vol > 0f)
				{
					if (inSpan)
					{
						scale = (rAt * rAt - cavityR2) * MathF.PI * spacing / vol;
					}
					else
					{
						scale = rAt * MathF.PI * rAt * spacing / vol;
					}
				}
				else
				{
					scale = 1f;
				}
				if (regionIdx == 0)
				{
					removed += removedHere;
				}
				for (int jj = 0; jj < nr; jj++)
				{
					float fy = (jj - half) * spacing;
					for (int kk = 0; kk < nr; kk++)
					{
						float fx = (kk - half) * spacing;
						float d2 = fx * fx + fy * fy;
						if (d2 > rAt2 || (inSpan && cavityR2 > d2) || !profile.regions[regionIdx].Matches(a, d2))
						{
							continue;
						}
						pos.Add(new Vector3(fx, fy, z));
						vel.Add(new Vector3(0f, 0f, vz));
						mid.Add(regionMat);
						volScale.Add(scale);
						cellVol.Add(cell);
						if (latKey != null)
						{
							latKey.Add(SuperXi.PackLatKey(i, jj, kk, sub));
						}
					}
				}
			}
			regionCounts.Add(pos.Count - regionStart);
		}
		ProfileFillParticles += pos.Count - initial;
		removedVolM3 = (double)removed * cell;
		solidVolM3 = (double)((pos.Count - initial) + removed) * cell;
	}

	private static Vector3 FillPlate(SuperPlateSpec pl, float spacing, List<Vector3> pos, List<ContinuumMaterialId> mid, ContinuumMaterialId mat, List<byte> pin, List<float> cellVol, List<ulong> latKey)
	{
		if (pl.hasPose)
		{
			return FillPlatePosed(pl, spacing, pos, mid, mat, pin, cellVol, latKey);
		}
		if (pl.clip.present)
		{
			throw new InvalidOperationException("[запекание] S40: срез по области счёта у ПРЕЖНЕЙ плиты (без позы) — маршрут с областью обязан быть общим (SuperShotRunner.Choose); молча испечь плиту без среза значило бы посчитать другую сцену.");
		}
		int nt = Math.Max(1, Mathf.RoundToInt(pl.thicknessM / spacing));
		int nv = Math.Max(1, Mathf.RoundToInt(pl.sideM / spacing));
		float rad = pl.angleXDeg * 0.017453292f;
		float c = Mathf.Cos(rad);
		float s = Mathf.Sin(rad);
		float fine = pl.fineCoreRadiusM;
		bool fineOn = fine > 0f;
		float fine2 = fine * fine;
		float quarter = spacing * 0.25f;
		float cell = spacing * spacing * spacing;
		float cellSub = cell * 0.125f;
		Vector3 centre = PlateCentre(pl);
		for (int i = 0; i < nt; i++)
		{
			float d = (i + 0.5f) * spacing - pl.thicknessM * 0.5f;
			for (int j = 0; j < nv; j++)
			{
				float a = (j - (nv - 1) * 0.5f) * spacing;
				for (int k = 0; k < nv; k++)
				{
					float b = (k - (nv - 1) * 0.5f) * spacing;
					bool edge = pl.clampedEdges && (k < 2 || k >= nv - 2 || j < 2 || j >= nv - 2);
					float x = centre.x + b;
					float y = centre.y + a * c - d * s;
					if (fineOn && x * x + y * y <= fine2)
					{
						for (int di = -1; di < 2; di += 2)
						{
							float dd = di * quarter + d;
							for (int aj = -1; aj < 2; aj += 2)
							{
								float aa = aj * quarter + a;
								for (int bk = -1; bk < 2; bk += 2)
								{
									float bb = bk * quarter + b;
									pos.Add(new Vector3(centre.x + bb, centre.y + aa * c - dd * s, centre.z + aa * s + dd * c));
									mid.Add(mat);
									pin.Add((byte)(edge ? 1 : 0));
									cellVol.Add(cellSub);
									if (latKey != null)
									{
										latKey.Add(SuperXi.PackLatKey(i, j, k, (bk + 1 >> 1) + ((di + 1 >> 1) + (aj + 1 >> 1) * 2) * 2));
									}
								}
							}
						}
					}
					else
					{
						pos.Add(new Vector3(x, y, centre.z + a * s + d * c));
						mid.Add(mat);
						pin.Add((byte)(edge ? 1 : 0));
						cellVol.Add(cell);
						if (latKey != null)
						{
							latKey.Add(SuperXi.PackLatKey(i, j, k, 8));
						}
					}
				}
			}
		}
		return new Vector3(0f, -s, c);
	}

	private static Vector3 FillPlatePosed(SuperPlateSpec pl, float spacing, List<Vector3> pos, List<ContinuumMaterialId> mid, ContinuumMaterialId mat, List<byte> pin, List<float> cellVol, List<ulong> latKey)
	{
		ValidatePose(in pl, "плита");
		if (pl.roundU)
		{
			return FillRoundPosed(pl, spacing, pos, mid, mat, pin, cellVol, latKey);
		}
		int nt = Math.Max(1, Mathf.RoundToInt(pl.thicknessM / spacing));
		int nu = Math.Max(1, Mathf.RoundToInt(pl.widthM / spacing));
		int nv = Math.Max(1, Mathf.RoundToInt(pl.heightM / spacing));
		float cell = spacing * spacing * spacing;
		PlateFrame(in pl, out Vector3 centre, out Vector3 u, out Vector3 v, out Vector3 n, out float halfU, out float halfV);
		for (int i = 0; i < nt; i++)
		{
			for (int j = 0; j < nv; j++)
			{
				float lv = (j - (nv - 1) * 0.5f) * spacing;
				for (int k = 0; k < nu; k++)
				{
					float lu = (k - (nu - 1) * 0.5f) * spacing;
					float d = (i + 0.5f) * spacing - pl.thicknessM * 0.5f;
					Vector3 p = centre + u * lu + v * lv + n * d;
					int cls = 0;
					if (pl.clip.present)
					{
						cls = pl.clip.Classify(p, spacing);
						if (cls == 2)
						{
							continue;
						}
					}
					bool edge = pl.clampedEdges && (k < 2 || k >= nu - 2 || j < 2 || j >= nv - 2);
					pos.Add(p);
					mid.Add(mat);
					pin.Add((byte)((edge || cls == 1) ? 1 : 0));
					cellVol.Add(cell);
					if (latKey != null)
					{
						latKey.Add(SuperXi.PackLatKey(i, j, k, 8));
					}
				}
			}
		}
		return pl.poseN;
	}

	public static bool RoundKeeps(in SuperPlateSpec pl, float ly, float lz)
	{
		double ry = (double)pl.heightM * 0.5;
		double rz = (double)pl.thicknessM * 0.5;
		if (ry <= 0.0 || rz <= 0.0)
		{
			return false;
		}
		double v = ly / ry;
		double w = lz / rz;
		return 1.0 >= v * v + w * w;
	}

	private static Vector3 FillRoundPosed(SuperPlateSpec pl, float spacing, List<Vector3> pos, List<ContinuumMaterialId> mid, ContinuumMaterialId mat, List<byte> pin, List<float> cellVol, List<ulong> latKey)
	{
		int nt = Math.Max(1, Mathf.RoundToInt(pl.thicknessM / spacing));
		int nu = Math.Max(1, Mathf.RoundToInt(pl.widthM / spacing));
		int nv = Math.Max(1, Mathf.RoundToInt(pl.heightM / spacing));
		float cell = spacing * spacing * spacing;
		PlateFrame(in pl, out Vector3 centre, out Vector3 u, out Vector3 v, out Vector3 n, out float halfU, out float halfV);
		for (int i = 0; i < nt; i++)
		{
			float lz = (i + 0.5f) * spacing - pl.thicknessM * 0.5f;
			for (int j = 0; j < nv; j++)
			{
				float ly = (j - (nv - 1) * 0.5f) * spacing;
				if (!RoundKeeps(in pl, ly, lz))
				{
					continue;
				}
				for (int k = 0; k < nu; k++)
				{
					float lu = (k - (nu - 1) * 0.5f) * spacing;
					Vector3 p = centre + u * lu + v * ly + n * lz;
					int cls = 0;
					if (pl.clip.present)
					{
						cls = pl.clip.Classify(p, spacing);
						if (cls == 2)
						{
							continue;
						}
					}
					bool edge = pl.clampedEdges && (k < 2 || k >= nu - 2);
					pos.Add(p);
					mid.Add(mat);
					pin.Add((byte)((edge || cls == 1) ? 1 : 0));
					cellVol.Add(cell);
					if (latKey != null)
					{
						latKey.Add(SuperXi.PackLatKey(i, j, k, 8));
					}
				}
			}
		}
		return pl.poseN;
	}

	public static long RoundCount(in SuperPlateSpec pl, float spacing)
	{
		int nt = Math.Max(1, Mathf.RoundToInt(pl.thicknessM / spacing));
		int nu = Math.Max(1, Mathf.RoundToInt(pl.widthM / spacing));
		int nv = Math.Max(1, Mathf.RoundToInt(pl.heightM / spacing));
		long kept = 0L;
		for (int j = 0; j < nt; j++)
		{
			for (int i = 0; i < nv; i++)
			{
				float ly = ((float)i - (nv - 1) * 0.5f) * spacing;
				float lz = (j + 0.5f) * spacing - pl.thicknessM * 0.5f;
				if (RoundKeeps(in pl, ly, lz))
				{
					kept++;
				}
			}
		}
		return (long)nu * kept;
	}

	private static Body BakeMeshBody(SuperPlateSpec pl, int index, float sceneSpacing, List<Vector3> pos, List<Vector3> vel, List<ContinuumMaterialId> mid, List<float> volScale, List<float> cellVol, List<byte> pin, List<byte> nofrag, List<ulong> latKey)
	{
		// PARTIAL (0x1806665B0). Проверено по ISIL:
		// string name = string.IsNullOrEmpty(pl.mesh.partName)
		//     ? string.Format("часть {0}", pl.mesh.part + 1) : pl.mesh.partName;
		// if (pl.hasPose && pl.clip.present) ValidatePose(in pl, name);
		// ContinuumMaterialId mat = Material(pl.material);
		// float spacing = ResolveSpacing(pl.spacingM, sceneSpacing, name);
		// if (pl.mesh.spacing != spacing) → 1187 (ошибка);
		// if (MathF.Abs(pl.clip.hu - pl.mesh.hu) > 0f || MathF.Abs(pl.clip.hv - pl.mesh.hv) > 0f) → 1161;
		// vx = SuperMeshCache.GetOrCompute(pl.mesh); (MeshFillCalls++ где-то рядом)
		// далее тройной цикл по ячейкам вокселей (Part.get_Count, bounds 1090/1092),
		// SuperClip.CellCentre/CellLive, PackLatKey → latKey, List.Add* (mid, volScale, cellVol, pin),
		// затем поля Body ([r12+4]=count, [r12+20]=...). Хвост: throw InvalidOperationException (1065-1088).
		return default(Body);
	}

	public static bool InsideMesh(in Body b, Vector3 p)
	{
		if (!b.isMesh || b.meshVoxels == null)
		{
			return false;
		}
		Vector3 rel = p - b.centre;
		Vector3 local = new Vector3(Vector3.Dot(rel, b.axisU), Vector3.Dot(rel, b.axisV), Vector3.Dot(rel, b.normal));
		if (!b.meshVoxels.CellOf(local, out int i, out int j, out int k))
		{
			return false;
		}
		return b.meshVoxels.OwnerAt(i, j, k) == b.meshPart;
	}

	public static long EstimateParticles(double rodVolumeM3, IList<SuperPlateSpec> plates, float spacing)
	{
		double particles = rodVolumeM3 / ((double)spacing * (double)spacing * (double)spacing);
		if (plates == null)
		{
			return (long)particles;
		}
		for (int i = 0; i < plates.Count; i++)
		{
			SuperPlateSpec pl = plates[i];
			if (pl.mesh != null)
			{
				SuperMeshVoxels vx = SuperMeshCache.GetOrCompute(pl.mesh);
				int part = pl.mesh.part;
				int count = 0;
				if (part >= 0 && part < vx.parts.Length)
				{
					count = vx.parts[part].Count;
				}
				particles += count;
			}
			else if (pl.clip.present && pl.hasPose)
			{
				float s = ((pl.spacingM <= 0f) ? spacing : pl.spacingM);
				particles += SuperClip.CountPosedPlate(in pl, s).Kept;
			}
			else if (pl.roundU)
			{
				float s = ((pl.spacingM <= 0f) ? spacing : pl.spacingM);
				particles += RoundCount(in pl, s);
			}
			else
			{
				float s = ((pl.spacingM <= 0f) ? spacing : pl.spacingM);
				double cell3 = (double)s * (double)s * (double)s;
				double vol = PlateVolume(pl);
				if (pl.fineCoreRadiusM > 0f)
				{
					double core = Math.Min(vol, (double)pl.fineCoreRadiusM * Math.PI * (double)pl.fineCoreRadiusM * (double)pl.thicknessM);
					particles += (vol - core) / cell3 + core * 8.0 / cell3;
				}
				else
				{
					particles += vol / cell3;
				}
			}
		}
		return (long)particles;
	}

	public static bool ParticleBudgetOk(long particles, out string why)
	{
		why = null;
		long need = particles * RamBytesPerParticle;
		long mem = (long)SystemInfo.systemMemorySize << 20;
		if (mem <= 0)
		{
			return true;
		}
		long cap = (long)(mem * RamShareCeiling);
		if (need <= cap)
		{
			return true;
		}
		why = string.Format("сцена даёт ≈{0:F1} млн частиц и просит {1:F0} МиБ ", (double)particles / 1000000.0, (double)need / 1048576.0) + string.Format("ОПЕРАТИВНОЙ памяти при {0:F0} МиБ на машине (потолок ", (double)mem / 1048576.0) + string.Format("{0:P0}). ⚠️ Это отказ ПО ИМЕНИ вместо УБИТОГО ПРОЦЕССА: сцена ", RamShareCeiling) + "«плита 1000 мм при шаге 1 мм» 01.09 оборвала Unity фатальной ошибкой прямо в конструкторе частиц. Огрубляй шаг (вчетверо меньше частиц на каждое удвоение) или уменьшай сцену.";
		return false;
	}

	public static double RodVolume(SuperRodSpec rod, int samples = 4096)
	{
		float r = rod.diameterM * 0.5f;
		float noseLen = RodGeometry.NoseLengthM(rod.diameterM, rod.lengthM);
		if (samples <= 0)
		{
			return 0.0;
		}
		double step = rod.lengthM / samples;
		double sum = 0.0;
		for (int i = 0; i < samples; i++)
		{
			float a = (float)((i + 0.5) * step);
			float rad = RodGeometry.NoseRadiusFactorAt(rod.nose, a, noseLen, r) * r;
			sum += Math.PI * (double)rad * (double)rad * step;
		}
		return sum;
	}

	public static double PlateVolume(SuperPlateSpec pl)
	{
		if (pl.hasPose)
		{
			return (double)pl.heightM * pl.widthM * pl.thicknessM;
		}
		return (double)pl.sideM * pl.sideM * pl.thicknessM;
	}
}
