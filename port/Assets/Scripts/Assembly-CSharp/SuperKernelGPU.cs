using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public sealed class SuperKernelGPU : IDisposable
{
	public sealed class FullState
	{
		public int count;

		public Vector3[] pos;

		public Vector3[] vel;

		public SuperMat3[] C;

		public SuperMat3[] F;

		public SuperMat3[] S;

		public SuperMat3[] sigma;

		public Vector4[] scal;

		public float[] dmg;

		public uint[] cause;

		public uint[] fail;

		public uint[] escaped;

		public uint[] guard;

		public float fixedPointScale;

		public const int BytesPerParticle = 192;

		public FullState()
		{
			fail = new uint[41];
			escaped = new uint[4];
			guard = new uint[4];
		}
	}

	public enum GridPathMode
	{
		Full = 0,
		Region = 1,
		Sparse = 2
	}

	private interface IPositions
	{
		int Count { get; }

		Vector3 At(int i);
	}

	private struct ArrayPositions : IPositions
	{
		public Vector3[] a;

		public int Count => a.Length;

		public Vector3 At(int i)
		{
			return a[i];
		}
	}

	private struct NativePositions : IPositions
	{
		public NativeArray<Vector3> a;

		public int count;

		public int Count => count;

		public Vector3 At(int i)
		{
			return a[i];
		}
	}

	public sealed class SparseStats
	{
		public bool sparse;

		public int nx;

		public int ny;

		public int nz;

		public long virtualNodes;

		public long vblocks;

		public long poolBlocks;

		public long touchedAtBuild;

		public long activeFirst;

		public long activeLast;

		public long activeMax;

		public int growths;

		public bool overflow;

		public string refusal;

		public long poolBytes;

		public long tableBytes;

		public List<KeyValuePair<int, uint>> trace;

		public List<string> growthLog;
	}

	public enum ProfSlot
	{
		EmptyReadback = 0,
		Uniforms = 1,
		KClearGridA = 2,
		KP2G_Atomic = 3,
		KGridFromInt = 4,
		RouteB = 5,
		KG2P = 6,
		KUpdate = 7,
		EraDrive = 8,
		KBounds = 9
	}

	public sealed class KernelProfile
	{
		public static readonly int Slots;

		public readonly long[] ticks;

		public readonly long[] counts;

		public KernelProfile()
		{
			ticks = new long[Slots];
			counts = new long[Slots];
		}

		public void Reset()
		{
			Array.Clear(ticks, 0, Slots);
			Array.Clear(counts, 0, Slots);
		}

		public void Add(ProfSlot s, long t)
		{
			ticks[(int)s] += t;
			counts[(int)s]++;
		}

		public double RawMeanMs(ProfSlot s)
		{
			if (counts[(int)s] <= 0)
			{
				return 0.0;
			}
			return (double)ticks[(int)s] * 1000.0 / (double)Stopwatch.Frequency / (double)counts[(int)s];
		}

		public double MeanMs(ProfSlot s)
		{
			if (s == ProfSlot.EmptyReadback || s == ProfSlot.Uniforms)
			{
				return RawMeanMs(s);
			}
			return Math.Max(0.0, RawMeanMs(s) - RawMeanMs(ProfSlot.EmptyReadback));
		}

		public long Calls(ProfSlot s)
		{
			return counts[(int)s];
		}
	}

	internal readonly struct ReconInputs
	{
		public readonly ComputeBuffer pos;

		public readonly ComputeBuffer vel;

		public readonly ComputeBuffer scal;

		public readonly ComputeBuffer s;

		public readonly ComputeBuffer dmg;

		public readonly int particles;

		public ReconInputs(ComputeBuffer pos, ComputeBuffer vel, ComputeBuffer scal, ComputeBuffer s, ComputeBuffer dmg, int particles)
		{
			this.pos = null;
			this.vel = null;
			this.scal = null;
			this.s = null;
			this.dmg = null;
			this.particles = 0;
		}
	}

	public const int MaxUavPerKernel = 8;

	public const int Group = 256;

	public static Func<ContinuumMaterialId, ContinuumProperties, ContinuumProperties> NightMaterialOverride;

	public static bool TwoFieldContact;

	public const float FixedPointLimit = 2E+09f;

	public const float FixedPointSafety = 8f;

	public static float TestScaleMassFloorKg;

	private readonly ComputeShader cs;

	private readonly int n;

	private int kClearA;

	private int kP2GA;

	private int kGridFromInt;

	private int kClearB;

	private int kHash;

	private int kClearCells;

	private int kCount;

	private int kScanBlocks;

	private int kScanBlockSums;

	private int kScanApply;

	private int kScatter;

	private int kSortCells;

	private int kP2GB;

	private int kGridFromFloat;

	private int kG2P;

	private int kUpdate;

	private int kReduce;

	private int kGuard;

	private int kEval;

	private int kEvalFail;

	private int kEvalSigma1;

	private int kBoundsP;

	private int kBoundsL;

	private int kRegionArgs;

	private int kClearA_R;

	private int kGridFromInt_R;

	private int kBlockClear;

	private int kBlockMark;

	private int kBlockList;

	private int kClearBlocks;

	private int kGridFromIntBlocks;

	private int kSparseProbe;

	private ComputeBuffer bPos;

	private ComputeBuffer bVel;

	private ComputeBuffer bC;

	private ComputeBuffer bF;

	private ComputeBuffer bS;

	private ComputeBuffer bSigma;

	private ComputeBuffer bScal;

	private ComputeBuffer bMass;

	private ComputeBuffer bV0;

	private ComputeBuffer bGridI;

	private ComputeBuffer bGridF;

	private ComputeBuffer bGridV;

	private ComputeBuffer bCellId;

	private ComputeBuffer bCellCount;

	private ComputeBuffer bCellStart;

	private ComputeBuffer bCellFill;

	private ComputeBuffer bOrder;

	private ComputeBuffer bBlockSums;

	private ComputeBuffer bMat;

	private ComputeBuffer bMatId;

	private ComputeBuffer bGuardBuf;

	private ComputeBuffer bFail;

	private ComputeBuffer bPartial;

	private ComputeBuffer bEval;

	private ComputeBuffer bEscaped;

	private ComputeBuffer bDmg;

	private ComputeBuffer bCause;

	private ComputeBuffer bPinned;

	private ComputeBuffer bNoFrag;

	private ComputeBuffer bXi;

	private ComputeBuffer bBoundsA;

	private ComputeBuffer bBoundsB;

	private ComputeBuffer bRegion;

	private ComputeBuffer bRegionArgs;

	private ComputeBuffer bBlockMark;

	private ComputeBuffer bBlockSlot;

	private ComputeBuffer bVSums1;

	private ComputeBuffer bVSums1Scan;

	private ComputeBuffer bVSums2;

	private ComputeBuffer bActiveList;

	private ComputeBuffer bBlockArgs;

	private ComputeBuffer bObst;

	private int nx;

	private int ny;

	private int nz;

	private int numNodes;

	private int numCells;

	private int numBlocks;

	private long virtualNodes;

	private int vbx;

	private int vby;

	private int vbz;

	private int numVBlocks;

	private int poolBlocks;

	private Vector3 origin;

	private float h;

	public static bool SabotageAllocateAll;

	private SuperScenePlan.Layout layout;

	private bool routeBAlloc;

	private readonly bool eagerLazy;

	private SuperScatterRoute _route;

	public bool anvil;

	public float anvilFriction;

	public float velocityCeiling;

	public bool elasticOnly;

	public bool skipGridVelocity;

	public bool clampInjects;

	public bool mutZeroDeviator;

	public bool mutNaNAtZero;

	public bool mutFpScaleX1024;

	public bool mutFailNudge;

	public bool mutShearNudge;

	private readonly bool contactAlloc;

	public int projectileParticleEnd;

	private readonly bool xiAlloc;

	private static readonly string[] KernelNames;

	private const string XiRow = "Xi";

	private SuperDecor _obstSrc;

	private float _obstSkipM;

	public static bool SabotageNoObstacle;

	public static int SabotageVerifyGridOnCall;

	private int _verifyGridCalls;

	public const int MaxGroupsPerDim = 65535;

	public static bool LogDispatch;

	public static readonly ContinuumMaterialId[] SteelRows;

	public static bool SabotageForce1D;

	public static int MaxParticlesPerDispatch;

	public static int MaxNodesPerDispatch;

	public static bool ChunkedDispatch;

	public static bool SabotageNoDispEnd;

	public static GridPathMode GridPath;

	public static bool SabotageShrinkRegion;

	private bool _regionPrimed;

	private bool _indirectVerified;

	private readonly int[] _regionFirst;

	public static bool SabotageSparseSkipMarkZ;

	public static bool SabotageSparseSkipMarkZFromStart;

	public static bool SabotageSparseShiftSlots;

	public static bool SabotageSparseSelfCheck;

	public static int SparseForceCapacityBlocks;

	public static bool SparseGrowthOn;

	public const double PoolGrowAt = 0.7;

	public const double PoolInitialFactor = 1.5;

	public const int PoolMinBlocks = 256;

	public static int SparseCheckEverySteps;

	public static int SparseSelfChecksPassed;

	private LocalKeyword _kwSparse;

	private LocalKeyword _kwXi;

	private LocalKeyword _kwObst;

	private bool _sparseVerified;

	private bool _poolOverflowSeen;

	private int _stepIndex;

	private int _stepsSinceCheck;

	private int _poolGrowths;

	private long _poolTouchedAtBuild;

	private long _activeFirst;

	private string _poolRefusal;

	private readonly List<KeyValuePair<int, uint>> _activeTrace;

	private readonly List<string> _growthLog;

	public static bool ProfileKernels;

	public static readonly KernelProfile Profile;

	private readonly uint[] _profGuard;

	private SuperGpuMaterial[] _auditMaterialTable;

	internal long ExternalVramBytes;

	private static int _liveKernels;

	private bool _disposed;

	public float FixedPointScale { get; private set; }

	public SuperScatterRoute route
	{
		get
		{
			return _route;
		}
		set
		{
			if (value == SuperScatterRoute.CellSortGather && !routeBAlloc)
			{
				throw new InvalidOperationException("SUPER: маршрут B: буферы не выделены — ядро построено для маршрута A (S34 P1, решение 19.09 §2: платить только за то, что маршрут читает). Нужны GridF, CellId, CellCount, CellStart, CellFill, Order, BlockSums: постройте ядро с маршрутом B (довод конструктора buildRoute) или позовите AllocateRouteB() ДО выбора маршрута. Тихо считать маршрутом B без его буферов запрещено.");
			}
			_route = value;
		}
	}

	public SuperScenePlan.Layout Layout => layout;

	public bool RouteBAllocated => routeBAlloc;

	public SuperScenePlan.Sizes SizesNow
	{
		get
		{
			if (layout == SuperScenePlan.Layout.Sparse)
			{
				return SuperScenePlan.Sizes.Sparse(n, poolBlocks, numVBlocks, contactAlloc);
			}
			return new SuperScenePlan.Sizes(n, numNodes, contactAlloc);
		}
	}

	public bool Sparse => layout == SuperScenePlan.Layout.Sparse;

	public bool ContactAllocated => contactAlloc;

	public bool XiAllocated => xiAlloc;

	public bool ContactActive => contactAlloc && projectileParticleEnd > 0 && _route == SuperScatterRoute.FixedPointAtomics;

	private int NodeStrideNow => GridIntsPerNodeFor(ContactActive);

	private int GridVStrideNow => GridVPerNodeFor(ContactActive);

	public int ParticleCount => n;

	public long VramBytes { get; private set; }

	public float H => h;

	public long ObstacleNodes { get; private set; }

	public bool ObstacleOn => bObst != null;

	public double ObstacleBuildMs { get; private set; }

	public bool DispatchOverflows => LayoutOverflows(n) || LayoutOverflows(numNodes) || LayoutOverflows(numCells) || LayoutOverflows(numVBlocks);

	public static uint SteelMask
	{
		get
		{
			uint mask = 0u;
			ContinuumMaterialId[] steelRows = SteelRows;
			for (int i = 0; i < steelRows.Length; i++)
			{
				ContinuumMaterialId row = steelRows[i];
				if ((int)row > 31)
				{
					throw new ArgumentException(string.Format("строка реестра {0} = {1} не помещается в 32-битную ", row, (int)row) + "маску стали (SuperKernelGPU.SteelMask)");
				}
				mask |= (uint)(1 << (int)row);
			}
			return mask;
		}
	}

	public long ChunkDispatches { get; private set; }

	public long PoolRefusalNeedBytes { get; private set; }

	public long PoolRefusalAllowedBytes { get; private set; }

	public int PoolRefusalStep { get; private set; }

	public long PoolRefusalActive { get; private set; }

	public string SparsePoolFailure
	{
		get
		{
			if (layout != SuperScenePlan.Layout.Sparse)
			{
				return null;
			}
			if (!_poolOverflowSeen && _poolRefusal == null)
			{
				return null;
			}
			return string.Format("пул разреженной сетки: ёмкость {0} блоков, ростов {1}", poolBlocks, _poolGrowths) + (_poolOverflowSeen ? ", активных блоков стало больше ёмкости (FAIL_GRIDPOOL)" : "") + ((_poolRefusal != null) ? "; " + _poolRefusal : "");
		}
	}

	public static string GameGpuPath
	{
		get
		{
			if (GridPath == GridPathMode.Sparse)
			{
				return "sparse";
			}
			return (GridPath == GridPathMode.Region) ? "region" : "full";
		}
	}

	public int QProfileBins
	{
		get
		{
			int bins = nz;
			if (bins < 1)
			{
				return 1;
			}
			if (bins >= 1024)
			{
				return 1024;
			}
			return bins;
		}
	}

	public Vector3 GridOrigin => origin;

	public float CellH => h;

	internal ReconInputs ReconInputsNow => new ReconInputs(bPos, bVel, bScal, bS, bDmg, n);

	internal IReadOnlyList<string> PoolGrowthLog => _growthLog;

	internal string PoolRefusalText => _poolRefusal ?? "";

	public int NodeCount => numNodes;

	public int GridNx => nx;

	public int GridNy => ny;

	public int GridNz => nz;

	public static int LiveKernels => _liveKernels;

	public static SuperKernelGPU LastBuilt { get; private set; }

	public static int GridIntsPerNodeFor(bool contact)
	{
		return contact ? 12 : 4;
	}

	public static int GridVPerNodeFor(bool contact)
	{
		return (!contact) ? 1 : 2;
	}

	private static ComputeBuffer Buf(int count, int stride, string name)
	{
		if (count < 1)
		{
			throw new InvalidOperationException(string.Format("SUPER: буфер «{0}» просит {1} элементов — неположительный размер ", name, count) + "почти всегда означает переполнение int выше по течению.");
		}
		ComputeBuffer computeBuffer = new ComputeBuffer(count, stride);
		if (computeBuffer == null)
		{
			throw new InvalidOperationException(string.Format("SUPER: не создана обёртка буфера «{0}»: {1} × {2} Б.", name, count, stride));
		}
		return computeBuffer;
	}

	public void VerifyUpload(SuperParticles p)
	{
		int count = p.count;
		if (count <= 0)
		{
			return;
		}
		int[] idx = new int[3] { 0, count / 2, count - 1 };
		Vector3[] back = new Vector3[count];
		bPos.GetData(back);
		for (int i = 0; i < idx.Length; i++)
		{
			int j = idx[i];
			Vector3 sent = p.x[j];
			Vector3 got = back[j];
			float dx = sent.x - got.x;
			float dy = sent.y - got.y;
			float dz = sent.z - got.z;
			if (dx * dx + dy * dy + dz * dz > 1e-18f)
			{
				throw new InvalidOperationException(string.Format("SUPER: ДАННЫЕ НЕ ДОЕХАЛИ ДО GPU. Частица {0} из {1}: отправлено ", j, count) + string.Format("{0:F6}, прочитано обратно {1:F6}. ⚠️ Это почти всегда ПРОВАЛ ", sent, got) + string.Format("ВЫДЕЛЕНИЯ буфера: {0} частиц × {1:0.###} Б = ", count, SuperMemory.BytesPerParticle(route)) + string.Format("{0:F0} МиБ при ", (double)count * SuperMemory.BytesPerParticle(route) * 1e-06) + string.Format("{0} МиБ на устройстве. D3D пишет «failed to ", SystemInfo.graphicsMemorySize) + "create buffer» НАТИВНО, мимо обработчика Unity, `IsValid()` при этом отвечает true, а прогон идёт дальше и возвращает правдоподобные числа. Замер 02.09 (Logs/jet/era2.log): 27 провалов выделения, оба прежних сторожа молчали, прогон отчитался «7331 шаг за 0.0 с» и глубину 0.00 мм как [PASS].");
			}
		}
	}

	public static float DeriveScale(SuperParticles p)
	{
		float maxMass = 0f;
		float maxVel = 1f;
		for (int i = 0; i < p.count; i++)
		{
			float m = p.mass[i];
			if (m > maxMass)
			{
				maxMass = m;
			}
			float mag = p.v[i].magnitude;
			if (mag > maxVel)
			{
				maxVel = mag;
			}
		}
		maxMass = Math.Max(maxMass, TestScaleMassFloorKg);
		float denom = maxMass * FixedPointSafety * maxVel;
		if (denom <= 1e-30f)
		{
			return 1e11f;
		}
		return FixedPointLimit / (denom * FixedPointSafety);
	}

	public SuperKernelGPU(ComputeShader shader, SuperParticles p, float cellSize, in ContinuumProperties material, SuperScatterRoute buildRoute = SuperScatterRoute.FixedPointAtomics)
	{
		anvil = true;
		anvilFriction = 0.12f;
		contactAlloc = TwoFieldContact;
		_regionFirst = new int[6];
		_activeFirst = -1L;
		_activeTrace = new List<KeyValuePair<int, uint>>();
		_growthLog = new List<string>();
		_profGuard = new uint[4];
		if (shader == null)
		{
			throw new ArgumentNullException("shader");
		}
		if (!SystemInfo.supportsComputeShaders)
		{
			throw new InvalidOperationException("SUPER: устройство не поддерживает compute-шейдеры");
		}
		cs = shader;
		n = p.count;
		h = cellSize;
		xiAlloc = p.xi.IsCreated;
		eagerLazy = SabotageAllocateAll;
		routeBAlloc = buildRoute == SuperScatterRoute.CellSortGather || SabotageAllocateAll;
		layout = (routeBAlloc ? SuperScenePlan.Layout.DenseB : SuperMemory.LayoutFor(buildRoute));
		_route = buildRoute;
		AssertKernels(shader);
		kClearA = cs.FindKernel("KClearGridA");
		kP2GA = cs.FindKernel("KP2G_Atomic");
		kGridFromInt = cs.FindKernel("KGridFromInt");
		kClearB = cs.FindKernel("KClearGridB");
		kHash = cs.FindKernel("KHash");
		kClearCells = cs.FindKernel("KClearCells");
		kCount = cs.FindKernel("KCount");
		kScanBlocks = cs.FindKernel("KScanBlocks");
		kScanBlockSums = cs.FindKernel("KScanBlockSums");
		kScanApply = cs.FindKernel("KScanApply");
		kScatter = cs.FindKernel("KScatterIdx");
		kSortCells = cs.FindKernel("KSortCells");
		kP2GB = cs.FindKernel("KP2G_Gather");
		kGridFromFloat = cs.FindKernel("KGridFromFloat");
		kG2P = cs.FindKernel("KG2P");
		kUpdate = cs.FindKernel("KUpdate");
		kReduce = cs.FindKernel("KReduceDiag");
		kGuard = cs.FindKernel("KGuardNaN");
		kEval = cs.FindKernel("KEvalConstitutive");
		kEvalFail = cs.FindKernel("KEvalFailure");
		kEvalSigma1 = cs.FindKernel("KEvalSigma1");
		kBoundsP = cs.FindKernel("KBoundsParticles");
		kBoundsL = cs.FindKernel("KBoundsLevel");
		kRegionArgs = cs.FindKernel("KRegionArgs");
		kClearA_R = cs.FindKernel("KClearGridA_R");
		kGridFromInt_R = cs.FindKernel("KGridFromInt_R");
		kBlockClear = cs.FindKernel("KBlockClear");
		kBlockMark = cs.FindKernel("KBlockMark");
		kBlockList = cs.FindKernel("KBlockList");
		kClearBlocks = cs.FindKernel("KClearBlocks");
		kGridFromIntBlocks = cs.FindKernel("KGridFromIntBlocks");
		kSparseProbe = cs.FindKernel("KSparseProbe");
		_kwSparse = new LocalKeyword(cs, "SUPER_SPARSE");
		if (layout == SuperScenePlan.Layout.Sparse && !_kwSparse.isValid)
		{
			throw new InvalidOperationException("SUPER S34 P2: в шейдере нет ключа варианта SUPER_SPARSE (#pragma multi_compile_local) — разреженная сетка непригодна: без варианта P2G/G2P адресовали бы узлы плотной сетки в пуле блоков.");
		}
		_kwXi = new LocalKeyword(cs, "SUPER_XI");
		if (xiAlloc && !_kwXi.isValid)
		{
			throw new InvalidOperationException("SUPER S32: частицы запечены с ξ (разброс деформации разрушения), а в шейдере нет ключа варианта SUPER_XI (#pragma multi_compile_local) — KUpdate посчитал бы без ξ, то есть разброса молча не было бы.");
		}
		_kwObst = new LocalKeyword(cs, "SUPER_OBST");
		cs.GetKernelThreadGroupSizes(kG2P, out uint tx, out uint ty, out uint tz);
		if (tx != 256)
		{
			throw new InvalidOperationException(string.Format("SUPER: numthreads в шейдере = {0}, а C# рассчитывает на {1}", tx, 256));
		}
		FixedPointScale = DeriveScale(p);
		Interlocked.Increment(ref _liveKernels);
		try
		{
			AllocateParticles(p, in material);
			Vector3 lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			Vector3 hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			for (int i = 0; i < p.count; i++)
			{
				Vector3 q = p.x[i];
				lo.x = Math.Min(lo.x, q.x);
				lo.y = Math.Min(lo.y, q.y);
				lo.z = Math.Min(lo.z, q.z);
				hi.x = Math.Max(hi.x, q.x);
				hi.y = Math.Max(hi.y, q.y);
				hi.z = Math.Max(hi.z, q.z);
			}
			Build(lo, hi);
			BindAll();
		}
		catch
		{
			Dispose();
			throw;
		}
		LastBuilt = this;
	}

	private static void AssertKernels(ComputeShader shader)
	{
		List<string> missing = new List<string>();
		foreach (string name in KernelNames)
		{
			try
			{
				if (!shader.HasKernel(name))
				{
					missing.Add(name + " (нет имени)");
				}
				else
				{
					shader.GetKernelThreadGroupSizes(shader.FindKernel(name), out uint x, out uint y, out uint z);
				}
			}
			catch (Exception ex)
			{
				missing.Add(name + " (" + ex.GetType() + ")");
			}
		}
		if (missing.Count > 0)
		{
			throw new InvalidOperationException(string.Format("SUPER: ВЫЧИСЛИТЕЛЬНЫЙ ШЕЙДЕР НЕ СКОМПИЛИРОВАН — непригодны {0} ядер ", missing.Count) + string.Format("из {0}: {1}. ⚠️ Почти всегда это ", KernelNames.Length, string.Join(", ", missing)) + "синтаксическая ошибка в SuperSolver.compute; смотри строку ошибки шейдера ВЫШЕ в консоли Unity. Этот класс дефекта НЕ ВИДЯТ ни `typecheck` (он читает только C#), ни сторож чужих ошибок Unity (отказ приходит не во время счёта), ни `HasKernel` (объявления `#pragma kernel` переживают провал компиляции), ни `ShaderUtil.GetComputeShaderMessages` (в batchmode список пуст). Без этой проверки прогон падал позже и не по адресу: замер 04.09 дал «Invalid kernelIndex (14) passed» внутри теста и обвал приёмки S4 до 1/14 — причина названа не была.");
		}
		List<string> absent = new List<string>();
		foreach (string name in KernelNames)
		{
			if (!shader.HasKernel(name))
			{
				absent.Add(name);
			}
		}
		if (absent.Count > 0)
		{
			throw new InvalidOperationException("SUPER: ВЫЧИСЛИТЕЛЬНЫЙ ШЕЙДЕР НЕ СКОМПИЛИРОВАН — не найдено ядер: " + string.Format("{0} (из {1}). ⚠️ Это НЕ отказ ", string.Join(", ", absent), KernelNames.Length) + "устройства и НЕ ошибка привязки: почти всегда это синтаксическая ошибка в SuperSolver.compute. Её НЕ ВИДЯТ ни `typecheck` (он читает только C#), ни сторож чужих ошибок Unity (отказ приходит не во время счёта). Без этой проверки прогон падал бы позже и не по адресу: замер 04.09 дал «Invalid kernelIndex (14) passed» внутри теста и обвал приёмки S4 до 1/14 — причина при этом названа не была. Смотри консоль Unity: строка с ошибкой шейдера идёт ВЫШЕ этого сообщения.");
		}
	}

	private void BindKernel(int kernel, string name, params SuperGpuBind.Bind[] binds)
	{
		SuperGpuBind.Checked(cs, kernel, name, binds);
	}

	private void BindAll()
	{
		BindRouteA();
		BindCommon();
		if (routeBAlloc)
		{
			BindRouteB();
		}
		BindLazy();
	}

	private void BindRouteA()
	{
		if (layout == SuperScenePlan.Layout.Sparse)
		{
			BindKernel(kP2GA, "KP2G_Atomic", SuperGpuBind.Bind.U("_GridI", bGridI), SuperGpuBind.Bind.U("_Guard", bGuardBuf), SuperGpuBind.Bind.U("_Fail", bFail), SuperGpuBind.Bind.R("_PosR", bPos), SuperGpuBind.Bind.R("_VelR", bVel), SuperGpuBind.Bind.R("_CR", bC), SuperGpuBind.Bind.R("_FR", bF), SuperGpuBind.Bind.R("_SigmaR", bSigma), SuperGpuBind.Bind.R("_MassR", bMass), SuperGpuBind.Bind.R("_V0R", bV0), SuperGpuBind.Bind.R("_DmgR", bDmg), SuperGpuBind.Bind.R("_BlockMarkR", bBlockMark), SuperGpuBind.Bind.R("_BlockSlotR", bBlockSlot));
			return;
		}
		BindKernel(kClearA, "KClearGridA", SuperGpuBind.Bind.U("_GridI", bGridI), SuperGpuBind.Bind.U("_Guard", bGuardBuf));
		BindKernel(kP2GA, "KP2G_Atomic", SuperGpuBind.Bind.U("_GridI", bGridI), SuperGpuBind.Bind.U("_Guard", bGuardBuf), SuperGpuBind.Bind.U("_Fail", bFail), SuperGpuBind.Bind.R("_PosR", bPos), SuperGpuBind.Bind.R("_VelR", bVel), SuperGpuBind.Bind.R("_CR", bC), SuperGpuBind.Bind.R("_FR", bF), SuperGpuBind.Bind.R("_SigmaR", bSigma), SuperGpuBind.Bind.R("_MassR", bMass), SuperGpuBind.Bind.R("_V0R", bV0), SuperGpuBind.Bind.R("_DmgR", bDmg));
		BindKernel(kGridFromInt, "KGridFromInt", SuperGpuBind.Bind.U("_GridV", bGridV), SuperGpuBind.Bind.U("_Guard", bGuardBuf), SuperGpuBind.Bind.U("_Fail", bFail), SuperGpuBind.Bind.R("_GridIR", bGridI));
		BindKernel(kBoundsP, "KBoundsParticles", SuperGpuBind.Bind.U("_BoundsOut", bBoundsA), SuperGpuBind.Bind.R("_PosR", bPos));
		BindKernel(kClearA_R, "KClearGridA_R", SuperGpuBind.Bind.U("_GridI", bGridI), SuperGpuBind.Bind.U("_Guard", bGuardBuf), SuperGpuBind.Bind.R("_RegionR", bRegion));
		BindKernel(kGridFromInt_R, "KGridFromInt_R", SuperGpuBind.Bind.U("_GridV", bGridV), SuperGpuBind.Bind.U("_Guard", bGuardBuf), SuperGpuBind.Bind.U("_Fail", bFail), SuperGpuBind.Bind.R("_GridIR", bGridI), SuperGpuBind.Bind.R("_RegionR", bRegion));
	}

	private void BindSparse()
	{
		BindKernel(kBlockClear, "KBlockClear", SuperGpuBind.Bind.U("_BlockMark", bBlockMark));
		BindKernel(kBlockMark, "KBlockMark", SuperGpuBind.Bind.U("_BlockMark", bBlockMark), SuperGpuBind.Bind.R("_PosR", bPos));
		BindKernel(kBlockList, "KBlockList", SuperGpuBind.Bind.U("_ActiveList", bActiveList), SuperGpuBind.Bind.U("_BlockArgs", bBlockArgs), SuperGpuBind.Bind.U("_Guard", bGuardBuf), SuperGpuBind.Bind.U("_Fail", bFail), SuperGpuBind.Bind.R("_BlockMarkR", bBlockMark), SuperGpuBind.Bind.R("_BlockSlotR", bBlockSlot));
		BindKernel(kClearBlocks, "KClearBlocks", SuperGpuBind.Bind.U("_GridI", bGridI), SuperGpuBind.Bind.U("_Guard", bGuardBuf));
		BindKernel(kGridFromIntBlocks, "KGridFromIntBlocks", SuperGpuBind.Bind.U("_GridV", bGridV), SuperGpuBind.Bind.U("_Guard", bGuardBuf), SuperGpuBind.Bind.U("_Fail", bFail), SuperGpuBind.Bind.R("_GridIR", bGridI), SuperGpuBind.Bind.R("_ActiveListR", bActiveList));
		BindKernel(kSparseProbe, "KSparseProbe", SuperGpuBind.Bind.U("_BlockMark", bBlockMark));
	}

	private void BindRouteB()
	{
		BindKernel(kClearB, "KClearGridB", SuperGpuBind.Bind.U("_GridF", bGridF));
		BindKernel(kHash, "KHash", SuperGpuBind.Bind.U("_CellId", bCellId), SuperGpuBind.Bind.R("_PosR", bPos));
		BindKernel(kClearCells, "KClearCells", SuperGpuBind.Bind.U("_CellCount", bCellCount), SuperGpuBind.Bind.U("_CellFill", bCellFill));
		BindKernel(kCount, "KCount", SuperGpuBind.Bind.U("_CellCount", bCellCount), SuperGpuBind.Bind.R("_CellIdR", bCellId));
		BindKernel(kScanBlocks, "KScanBlocks", SuperGpuBind.Bind.U("_CellStart", bCellStart), SuperGpuBind.Bind.U("_BlockSums", bBlockSums), SuperGpuBind.Bind.R("_CellCountR", bCellCount));
		BindKernel(kScanBlockSums, "KScanBlockSums", SuperGpuBind.Bind.U("_BlockSums", bBlockSums));
		BindKernel(kScanApply, "KScanApply", SuperGpuBind.Bind.U("_CellStart", bCellStart), SuperGpuBind.Bind.R("_BlockSumsR", bBlockSums));
		BindKernel(kScatter, "KScatterIdx", SuperGpuBind.Bind.U("_Order", bOrder), SuperGpuBind.Bind.U("_CellFill", bCellFill), SuperGpuBind.Bind.R("_CellIdR", bCellId), SuperGpuBind.Bind.R("_CellStartR", bCellStart));
		BindKernel(kSortCells, "KSortCells", SuperGpuBind.Bind.U("_Order", bOrder), SuperGpuBind.Bind.R("_CellStartR", bCellStart), SuperGpuBind.Bind.R("_CellCountR", bCellCount));
		BindKernel(kP2GB, "KP2G_Gather", SuperGpuBind.Bind.U("_GridF", bGridF), SuperGpuBind.Bind.R("_OrderR", bOrder), SuperGpuBind.Bind.R("_CellStartR", bCellStart), SuperGpuBind.Bind.R("_CellCountR", bCellCount), SuperGpuBind.Bind.R("_PosR", bPos), SuperGpuBind.Bind.R("_VelR", bVel), SuperGpuBind.Bind.R("_CR", bC), SuperGpuBind.Bind.R("_FR", bF), SuperGpuBind.Bind.R("_SigmaR", bSigma), SuperGpuBind.Bind.R("_MassR", bMass), SuperGpuBind.Bind.R("_V0R", bV0));
		BindKernel(kGridFromFloat, "KGridFromFloat", SuperGpuBind.Bind.U("_GridV", bGridV), SuperGpuBind.Bind.R("_GridFR", bGridF));
	}

	private void BindCommon()
	{
		if (layout == SuperScenePlan.Layout.Sparse)
		{
			BindKernel(kG2P, "KG2P", SuperGpuBind.Bind.U("_Pos", bPos), SuperGpuBind.Bind.U("_Vel", bVel), SuperGpuBind.Bind.U("_C", bC), SuperGpuBind.Bind.U("_Escaped", bEscaped), SuperGpuBind.Bind.R("_GridVR", bGridV), SuperGpuBind.Bind.R("_PinnedR", bPinned), SuperGpuBind.Bind.R("_DmgR", bDmg), SuperGpuBind.Bind.R("_BlockMarkR", bBlockMark), SuperGpuBind.Bind.R("_BlockSlotR", bBlockSlot));
		}
		else
		{
			BindKernel(kG2P, "KG2P", SuperGpuBind.Bind.U("_Pos", bPos), SuperGpuBind.Bind.U("_Vel", bVel), SuperGpuBind.Bind.U("_C", bC), SuperGpuBind.Bind.U("_Escaped", bEscaped), SuperGpuBind.Bind.R("_GridVR", bGridV), SuperGpuBind.Bind.R("_PinnedR", bPinned), SuperGpuBind.Bind.R("_DmgR", bDmg));
		}
		BindKernel(kUpdate, "KUpdate", SuperGpuBind.Bind.U("_F", bF), SuperGpuBind.Bind.U("_S", bS), SuperGpuBind.Bind.U("_Sigma", bSigma), SuperGpuBind.Bind.U("_Scal", bScal), SuperGpuBind.Bind.U("_Fail", bFail), SuperGpuBind.Bind.U("_Dmg", bDmg), SuperGpuBind.Bind.U("_Cause", bCause), SuperGpuBind.Bind.R("_CR", bC), SuperGpuBind.Bind.R("_Materials", bMat), SuperGpuBind.Bind.R("_MatIdR", bMatId), SuperGpuBind.Bind.R("_NoFragR", bNoFrag), SuperGpuBind.Bind.R("_PosR", bPos), SuperGpuBind.Bind.R("_XiR", bXi ?? bMass));
		BindKernel(kGuard, "KGuardNaN", SuperGpuBind.Bind.U("_Fail", bFail), SuperGpuBind.Bind.U("_Vel", bVel), SuperGpuBind.Bind.R("_PosR", bPos));
	}

	private void BindLazy()
	{
		if (bObst != null)
		{
			if (layout == SuperScenePlan.Layout.Sparse)
			{
				BindKernel(kGridFromIntBlocks, "KGridFromIntBlocks", SuperGpuBind.Bind.R("_ObstR", bObst));
			}
			else
			{
				BindKernel(kGridFromInt, "KGridFromInt", SuperGpuBind.Bind.R("_ObstR", bObst));
				BindKernel(kGridFromInt_R, "KGridFromInt_R", SuperGpuBind.Bind.R("_ObstR", bObst));
			}
			if (routeBAlloc)
			{
				BindKernel(kGridFromFloat, "KGridFromFloat", SuperGpuBind.Bind.R("_ObstR", bObst));
			}
		}
		if (bPartial != null)
		{
			BindKernel(kReduce, "KReduceDiag", SuperGpuBind.Bind.U("_Partial", bPartial), SuperGpuBind.Bind.R("_VelR", bVel), SuperGpuBind.Bind.R("_MassR", bMass));
		}
		if (bEval != null)
		{
			BindKernel(kEval, "KEvalConstitutive", SuperGpuBind.Bind.U("_Eval", bEval), SuperGpuBind.Bind.R("_ScalR", bScal), SuperGpuBind.Bind.R("_Materials", bMat));
			BindKernel(kEvalFail, "KEvalFailure", SuperGpuBind.Bind.U("_Eval", bEval), SuperGpuBind.Bind.R("_ScalR", bScal), SuperGpuBind.Bind.R("_Materials", bMat));
			BindKernel(kEvalSigma1, "KEvalSigma1", SuperGpuBind.Bind.U("_Eval", bEval), SuperGpuBind.Bind.R("_ScalR", bScal), SuperGpuBind.Bind.R("_Materials", bMat));
		}
	}

	private ref ComputeBuffer FieldOf(string name)
	{
		switch (name)
		{
		case "Pos":
			return ref bPos;
		case "Vel":
			return ref bVel;
		case "C":
			return ref bC;
		case "F":
			return ref bF;
		case "S":
			return ref bS;
		case "Sigma":
			return ref bSigma;
		case "Scal":
			return ref bScal;
		case "Mass":
			return ref bMass;
		case "V0":
			return ref bV0;
		case "GridI":
			return ref bGridI;
		case "GridF":
			return ref bGridF;
		case "GridV":
			return ref bGridV;
		case "CellId":
			return ref bCellId;
		case "CellCount":
			return ref bCellCount;
		case "CellStart":
			return ref bCellStart;
		case "CellFill":
			return ref bCellFill;
		case "Order":
			return ref bOrder;
		case "BlockSums":
			return ref bBlockSums;
		case "Mat":
			return ref bMat;
		case "MatId":
			return ref bMatId;
		case "Guard":
			return ref bGuardBuf;
		case "Fail":
			return ref bFail;
		case "Partial":
			return ref bPartial;
		case "Eval":
			return ref bEval;
		case "Escaped":
			return ref bEscaped;
		case "Dmg":
			return ref bDmg;
		case "Cause":
			return ref bCause;
		case "Pinned":
			return ref bPinned;
		case "NoFrag":
			return ref bNoFrag;
		case "Xi":
			return ref bXi;
		case "BoundsA":
			return ref bBoundsA;
		case "BoundsB":
			return ref bBoundsB;
		case "Region":
			return ref bRegion;
		case "RegionArgs":
			return ref bRegionArgs;
		case "BlockMark":
			return ref bBlockMark;
		case "BlockSlot":
			return ref bBlockSlot;
		case "VSums1":
			return ref bVSums1;
		case "VSums1Scan":
			return ref bVSums1Scan;
		case "VSums2":
			return ref bVSums2;
		case "ActiveList":
			return ref bActiveList;
		case "BlockArgs":
			return ref bBlockArgs;
		case "Obst":
			return ref bObst;
		}
		throw new InvalidOperationException("SUPER: строке таблицы «" + name + "» не отвечает ни одно поле ядра — SuperScenePlan.GpuBuffers и SuperKernelGPU.FieldOf разошлись (правило S34: буфер правится в трёх местах одним заходом).");
	}

	private ComputeBuffer AllocRow(SuperScenePlan.GpuBuffer row)
	{
		SuperScenePlan.Sizes sizes = SizesNow;
		long count = row.Count(in sizes);
		if (count > int.MaxValue)
		{
			throw new InvalidOperationException(string.Format("SUPER: буфер «{0}» просит {1} элементов — счётчик ComputeBuffer переполняет int ", row.name, count) + "(переполнение дало бы отрицательный размер и падение далеко от причины).");
		}
		if (!row.indirect)
		{
			return Buf((int)count, row.stride, row.name);
		}
		if (count < 1)
		{
			throw new InvalidOperationException(string.Format("SUPER: буфер доводов «{0}» просит {1} элементов.", row.name, count));
		}
		return new ComputeBuffer((int)count, row.stride, ComputeBufferType.IndirectArguments);
	}

	private bool Wanted(SuperScenePlan.GpuBuffer row)
	{
		if (row.name == XiRow)
		{
			return false;
		}
		if (row.Lazy)
		{
			return eagerLazy;
		}
		return row.In(layout);
	}

	private void AllocatePhase(SuperScenePlan.Phase phase)
	{
		foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.GpuBuffers)
		{
			if (row.phase != phase || !Wanted(row))
			{
				continue;
			}
			ref ComputeBuffer field = ref FieldOf(row.name);
			if (field != null)
			{
				throw new InvalidOperationException("SUPER: буфер «" + row.name + "» уже выделен — повторное выделение потеряло бы прежний.");
			}
			field = AllocRow(row);
		}
	}

	public void AllocateRouteB()
	{
		if (routeBAlloc)
		{
			return;
		}
		if (layout == SuperScenePlan.Layout.Sparse)
		{
			throw new InvalidOperationException("SUPER S34 P2: маршрут B на ядре РАЗРЕЖЕННОЙ сетки невозможен — сортировке нужна плотная сетка ячеек. Постройте ядро маршрута B (довод buildRoute) — оно всегда плотное.");
		}
		routeBAlloc = true;
		layout = SuperScenePlan.Layout.DenseB;
		if (bGridI != null)
		{
			foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.GpuBuffers)
			{
				if (row.name == XiRow || row.Lazy || !row.In(layout))
				{
					continue;
				}
				ref ComputeBuffer field = ref FieldOf(row.name);
				if (field == null)
				{
					field = AllocRow(row);
				}
			}
		}
		BindRouteA();
		BindCommon();
		if (routeBAlloc)
		{
			BindRouteB();
		}
		BindLazy();
		VramBytes = EstimateVram();
	}

	private void EnsureEval()
	{
		if (bEval == null)
		{
			bEval = AllocRow(SuperScenePlan.Row("Eval"));
			BindLazy();
			VramBytes = EstimateVram();
		}
	}

	private void EnsurePartial()
	{
		if (bPartial == null)
		{
			bPartial = AllocRow(SuperScenePlan.Row("Partial"));
			BindLazy();
			VramBytes = EstimateVram();
		}
	}

	private void AllocateParticles(SuperParticles p, in ContinuumProperties material)
	{
		AllocatePhase(SuperScenePlan.Phase.Particles);
		if (xiAlloc)
		{
			bXi = AllocRow(SuperScenePlan.Row("Xi"));
		}
		bCause.SetData(new uint[n]);
		numBlocks = Blocks(n);
		ContinuumMaterialId[] ids = (ContinuumMaterialId[])Enum.GetValues(typeof(ContinuumMaterialId));
		if (bMat.count != ids.Length)
		{
			throw new InvalidOperationException(string.Format("SUPER: буфер Mat выделен на {0} строк, реестр веществ — {1}: таблица и реестр разошлись.", bMat.count, ids.Length));
		}
		SuperGpuMaterial[] table = new SuperGpuMaterial[ids.Length];
		foreach (ContinuumMaterialId id in ids)
		{
			ContinuumProperties props = ContinuumMaterials.Get(id);
			if (NightMaterialOverride != null)
			{
				props = NightMaterialOverride(id, props);
			}
			table[(int)id] = SuperGpuMaterial.From(props);
		}
		bMat.SetData(table);
		_auditMaterialTable = table;
		Upload(p);
	}

	public void Upload(SuperParticles p)
	{
		Vector3[] pos = new Vector3[n];
		Vector3[] vel = new Vector3[n];
		SuperMat3[] C = new SuperMat3[n];
		SuperMat3[] F = new SuperMat3[n];
		SuperMat3[] S = new SuperMat3[n];
		SuperMat3[] sig = new SuperMat3[n];
		Vector4[] scal = new Vector4[n];
		float[] mass = new float[n];
		float[] V0 = new float[n];
		uint[] matId = new uint[n];
		uint[] pinned = new uint[n];
		uint[] noFrag = new uint[n];
		for (int i = 0; i < n; i++)
		{
			pinned[i] = p.pinned[i];
			noFrag[i] = p.noFrag[i];
			pos[i] = p.x[i];
			vel[i] = p.v[i];
			C[i] = p.C[i];
			F[i] = p.F[i];
			S[i] = p.s[i];
			sig[i] = p.sigma[i];
			scal[i] = new Vector4(p.epsP[i], p.epsRate[i], p.T[i], p.e[i]);
			mass[i] = p.mass[i];
			V0[i] = p.V0[i];
			matId[i] = p.matId[i];
		}
		bPos.SetData(pos);
		bVel.SetData(vel);
		bC.SetData(C);
		bF.SetData(F);
		bS.SetData(S);
		bSigma.SetData(sig);
		bScal.SetData(scal);
		bMass.SetData(mass);
		bV0.SetData(V0);
		bMatId.SetData(matId);
		bPinned.SetData(pinned);
		bNoFrag.SetData(noFrag);
		if (bXi == null)
		{
			if (p.xi.IsCreated)
			{
				throw new InvalidOperationException("SUPER S32: частицы несут ξ, а ядро построено БЕЗ буфера ξ (набор при постройке был без ξ) — KUpdate посчитал бы без разброса молча. Постройте ядро на наборе с ξ.");
			}
		}
		else
		{
			if (!p.xi.IsCreated || p.xi.Length != n)
			{
				throw new InvalidOperationException("SUPER S32: ядро построено с буфером ξ, а в загружаемом наборе ξ " + (p.xi.IsCreated ? string.Format("на {0} частиц из {1}", p.xi.Length, n) : "нет") + " — загрузка другого запекания в ядро с разбросом залила бы чужие (или никакие) множители ε_f.");
			}
			float[] xi = new float[n];
			bool s43 = SuperDamage.S43ScalingOn;
			for (int j = 0; j < n; j++)
			{
				float v = p.xi[j];
				if (v < 0.1f || float.IsInfinity(v))
				{
					throw new InvalidOperationException(string.Format("SUPER S32: ξ частицы {0} = {1:G9} — не конечен или ниже пола {2:F2}; делитель ε_f·ξ ", j, v, 0.1f) + "в KUpdate взорвал бы повреждение этой частицы. Поле ξ пекут только SuperXi.For (запекание, стенды).");
				}
				if (s43 && 10f < v)
				{
					string vText = v.ToString("G6");
					string capText = 10f.ToString("G6");
					if (vText == capText)
					{
						vText = v.ToString("R");
					}
					throw new InvalidOperationException("SUPER S43: " + Localization.T("super.s43.refuse.ceiling", j, vText, capText));
				}
				xi[j] = v;
			}
			bXi.SetData(xi);
		}
		float[] dmg = new float[n];
		if (p.damage.IsCreated)
		{
			for (int k = 0; k < n; k++)
			{
				dmg[k] = p.damage[k];
			}
		}
		if (bDmg != null)
		{
			bDmg.SetData(dmg);
		}
		bFail.SetData(new uint[3113]);
		bGuardBuf.SetData(new uint[4]);
		bEscaped.SetData(new uint[4]);
	}

	public FullState DownloadFull()
	{
		FullState st = new FullState();
		st.count = n;
		st.pos = new Vector3[n];
		st.vel = new Vector3[n];
		st.C = new SuperMat3[n];
		st.F = new SuperMat3[n];
		st.S = new SuperMat3[n];
		st.sigma = new SuperMat3[n];
		st.scal = new Vector4[n];
		st.dmg = new float[n];
		st.cause = new uint[n];
		st.fixedPointScale = FixedPointScale;
		bPos.GetData(st.pos);
		bVel.GetData(st.vel);
		bC.GetData(st.C);
		bF.GetData(st.F);
		bS.GetData(st.S);
		bSigma.GetData(st.sigma);
		bScal.GetData(st.scal);
		bDmg.GetData(st.dmg);
		bCause.GetData(st.cause);
		bFail.GetData(st.fail);
		bEscaped.GetData(st.escaped);
		bGuardBuf.GetData(st.guard);
		return st;
	}

	public void UploadFull(FullState st)
	{
		if (st.count != n)
		{
			throw new ArgumentException(string.Format("SUPER: состояние на {0} частиц, ядро на {1}", st.count, n));
		}
		if (st.fixedPointScale != FixedPointScale)
		{
			throw new InvalidOperationException(string.Format("SUPER: масштаб неподвижной точки контрольной точки {0:G9} не совпал ", st.fixedPointScale) + string.Format("с масштабом ядра {0:G9} побитово — ядро построено не на том запекании; ", FixedPointScale) + "продолжение не было бы тем же прогоном.");
		}
		bPos.SetData(st.pos);
		bVel.SetData(st.vel);
		bC.SetData(st.C);
		bF.SetData(st.F);
		bS.SetData(st.S);
		bSigma.SetData(st.sigma);
		bScal.SetData(st.scal);
		bDmg.SetData(st.dmg);
		bCause.SetData(st.cause);
		bFail.SetData(st.fail);
		bEscaped.SetData(st.escaped);
		bGuardBuf.SetData(st.guard);
	}

	public void VerifyUploadAgainst(Vector3[] want)
	{
		if (want == null || want.Length != n || n <= 0)
		{
			return;
		}
		int[] indices = new int[3] { 0, n / 2, n - 1 };
		Vector3[] got = new Vector3[n];
		bPos.GetData(got);
		for (int i = 0; i < indices.Length; i++)
		{
			int idx = indices[i];
			Vector3 w = want[idx];
			Vector3 g = got[idx];
			float dx = w.x - g.x;
			float dy = w.y - g.y;
			float dz = w.z - g.z;
			if (dx * dx + dy * dy + dz * dz > 1e-18f)
			{
				throw new InvalidOperationException(string.Format("SUPER: СОСТОЯНИЕ НЕ ДОЕХАЛО ДО GPU после восстановления: частица {0} из {1}, ", idx, n) + string.Format("отправлено {0:F6}, прочитано {1:F6}.", w, g));
			}
		}
	}

	public void Download(Vector3[] pos, Vector3[] vel, Vector4[] scal)
	{
		if (pos != null)
		{
			bPos.GetData(pos);
		}
		if (vel != null)
		{
			bVel.GetData(vel);
		}
		if (scal != null)
		{
			bScal.GetData(scal);
		}
	}

	public void DownloadPositionRange(Vector3[] dst, int start, int count)
	{
		if (dst == null || count <= 0)
		{
			return;
		}
		bPos.GetData(dst, 0, start, count);
	}

	public void DownloadVelocityRange(Vector3[] dst, int start, int count)
	{
		if (dst == null || count <= 0)
		{
			return;
		}
		bVel.GetData(dst, 0, start, count);
	}

	public void UploadVelocityRange(Vector3[] src, int start, int count)
	{
		if (src == null || count <= 0)
		{
			return;
		}
		bVel.SetData(src, 0, start, count);
	}

	public void DownloadDeviator(SuperMat3[] dev)
	{
		if (dev == null)
		{
			return;
		}
		bS.GetData(dev);
	}

	public void DownloadDamage(float[] dmg)
	{
		if (dmg == null)
		{
			return;
		}
		bDmg.GetData(dmg);
	}

	public void DownloadXi(float[] dst)
	{
		if (!xiAlloc || bXi == null)
		{
			throw new InvalidOperationException("SUPER S32: DownloadXi у ядра без ξ — частицы запечены без разброса (флаг SuperDamage.FailureStrainScatterOn был ВЫКЛ при запекании), буфера Xi нет.");
		}
		if (dst == null)
		{
			throw new ArgumentNullException("dst");
		}
		if (dst.Length != n)
		{
			throw new ArgumentException(string.Format("SUPER S32: DownloadXi в массив на {0} при {1} частицах ядра.", dst.Length, n), "dst");
		}
		bXi.GetData(dst);
	}

	public void DownloadCause(uint[] cause)
	{
		if (cause == null)
		{
			return;
		}
		bCause.GetData(cause);
	}

	public void DownloadSigma(SuperMat3[] sigma)
	{
		if (sigma == null)
		{
			return;
		}
		bSigma.GetData(sigma);
	}

	public void DownloadDeformationRange(SuperMat3[] dst, int start, int count)
	{
		if (dst == null || count <= 0)
		{
			return;
		}
		bF.GetData(dst, 0, start, count);
	}

	public void DownloadScalarsRange(Vector4[] dst, int start, int count)
	{
		if (dst == null || count <= 0)
		{
			return;
		}
		bScal.GetData(dst, 0, start, count);
	}

	public void DownloadSigmaRange(SuperMat3[] dst, int start, int count)
	{
		if (dst == null || count <= 0)
		{
			return;
		}
		bSigma.GetData(dst, 0, start, count);
	}

	public void DownloadDamageRange(float[] dst, int start, int count)
	{
		if (dst == null || count <= 0)
		{
			return;
		}
		bDmg.GetData(dst, 0, start, count);
	}

	public void DownloadCauseRange(uint[] dst, int start, int count)
	{
		if (dst == null || count <= 0)
		{
			return;
		}
		bCause.GetData(dst, 0, start, count);
	}

	public void GridDims(out int nx, out int ny, out int nz)
	{
		nx = this.nx;
		ny = this.ny;
		nz = this.nz;
	}

	public bool ReadNodeMass(int ix, int iy, int iz, out float massKg, out int quanta)
	{
		massKg = 0f;
		quanta = 0;
		if (bGridI == null || _route != SuperScatterRoute.FixedPointAtomics || ix < 0 || iy < 0 || iz < 0 || ix >= nx || iy >= ny || iz >= nz)
		{
			return false;
		}
		int nodeStride = NodeStrideNow;
		int node = (ny * iz + iy) * nx + ix;
		if (layout == SuperScenePlan.Layout.Sparse)
		{
			int block = ((iz >> 3) * vby + (iy >> 3)) * vbx + (ix >> 3);
			uint[] marks = new uint[1];
			bBlockMark.GetData(marks, 0, block, 1);
			if (marks[0] == 0)
			{
				return true;
			}
			bBlockSlot.GetData(marks, 0, block, 1);
			if ((uint)poolBlocks <= marks[0])
			{
				return false;
			}
			node = (ix & 7) + ((iy & 7) + (iz & 7) * 8) * 8 + (int)marks[0] * 512;
		}
		if (nodeStride * (node + 1) > numNodes * nodeStride || nodeStride * (node + 1) > bGridI.count)
		{
			return false;
		}
		int[] vals = new int[nodeStride];
		bGridI.GetData(vals, 0, node * nodeStride, nodeStride);
		int q = ((nodeStride < 5) ? vals[0] : (vals[0] + vals[4]));
		quanta = q;
		float scale = FixedPointScale;
		if (mutFpScaleX1024)
		{
			scale *= 1024f;
		}
		massKg = (1f / scale) * quanta;
		return true;
	}

	public void EvalSigma1(ContinuumMaterialId id, Vector4[] points, Vector2[] outValues)
	{
		EnsureEval();
		bScal.SetData(points);
		PushUniforms(0f);
		cs.SetInt("_EvalMatId", (int)id);
		Go(kEvalSigma1, points.Length);
		bEval.GetData(outValues);
	}

	public void EvalFailure(ContinuumMaterialId id, Vector4[] points, Vector2[] outValues)
	{
		EnsureEval();
		bScal.SetData(points);
		PushUniforms(0f);
		cs.SetInt("_EvalMatId", (int)id);
		Go(kEvalFail, points.Length);
		bEval.GetData(outValues);
	}

	public void RebuildFixed(Vector3 lo, Vector3 hi)
	{
		Build(lo, hi);
	}

	public void Rebuild(SuperParticles cpuMirror)
	{
		Vector3 lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		for (int i = 0; i < cpuMirror.count; i++)
		{
			Vector3 p = cpuMirror.x[i];
			lo.x = Math.Min(lo.x, p.x);
			lo.y = Math.Min(lo.y, p.y);
			lo.z = Math.Min(lo.z, p.z);
			hi.x = Math.Max(hi.x, p.x);
			hi.y = Math.Max(hi.y, p.y);
			hi.z = Math.Max(hi.z, p.z);
		}
		Build(lo, hi);
	}

	private void Build(Vector3 lo, Vector3 hi)
	{
		origin = SuperGrid.OriginFor(lo, h);
		SuperGrid.Dims(lo, hi, h, out nx, out ny, out nz);
		virtualNodes = (long)nz * ny * nx;
		if (layout == SuperScenePlan.Layout.Sparse)
		{
			BuildSparse();
			BuildObstacles();
			return;
		}
		int nodeCount = (int)((long)nz * ny * nx);
		long stride = contactAlloc ? 12 : 4;
		if (int.MaxValue < stride * nodeCount)
		{
			throw new InvalidOperationException(string.Format("SUPER: сетка {0}×{1}×{2} = {3} узлов — счётчик буфера сетки ", nx, ny, nz, nodeCount) + string.Format("({0} × {1}) переполняет int. Переполнение дало бы ОТРИЦАТЕЛЬНЫЙ размер ", nodeCount, (int)stride) + "буфера и падение далеко от причины, поэтому оно ловится здесь и по имени.");
		}
		if (bGridI == null || numNodes < nodeCount)
		{
			ReleaseGrid();
			numNodes = nodeCount;
			numCells = nodeCount;
			AllocatePhase(SuperScenePlan.Phase.Grid);
			BindRouteA();
			BindCommon();
			if (routeBAlloc)
			{
				BindRouteB();
			}
			BindLazy();
			VramBytes = EstimateVram();
		}
		numNodes = nodeCount;
		numCells = nodeCount;
		_regionPrimed = false;
		VerifyGrid();
		ReportDispatch();
		BuildObstacles();
	}

	public void SetObstacles(SuperDecor decor, float skipBeyondRegionM)
	{
		_obstSrc = decor;
		_obstSkipM = skipBeyondRegionM;
		if (bGridI != null)
		{
			BuildObstacles();
		}
	}

	private void BuildObstacles()
	{
		if (bObst != null)
		{
			bObst.Release();
			bObst = null;
		}
		ObstacleNodes = 0L;
		ObstacleBuildMs = 0.0;
		if (_obstSrc == null || _obstSrc.Empty)
		{
			return;
		}
		if (!_kwObst.isValid)
		{
			throw new InvalidOperationException("SUPER S40c: у сцены есть декор-преграда, а в шейдере нет ключа варианта SUPER_OBST (#pragma multi_compile_local) — вещество молча прошло бы сквозь декор.");
		}
		Stopwatch sw = Stopwatch.StartNew();
		SuperScenePlan.GpuBuffer row = SuperScenePlan.Row("Obst");
		SuperScenePlan.Sizes sizes = SizesNow;
		long count = row.Count(in sizes);
		if (count > int.MaxValue)
		{
			throw new InvalidOperationException(string.Format("SUPER S40c: маска преграды просит {0} слов — переполнение int.", count));
		}
		uint[] mask = _obstSrc.ObstacleMask(origin, h, nx, ny, nz, (int)count, _obstSkipM, out long marked);
		ObstacleNodes = marked;
		if (marked > 0)
		{
			bObst = AllocRow(row);
			bObst.SetData(mask);
			BindLazy();
			VramBytes = EstimateVram();
		}
		ObstacleBuildMs = sw.Elapsed.TotalMilliseconds;
	}

	private void VerifyGrid()
	{
		if (bGridI == null || numNodes <= 0)
		{
			return;
		}
		bool last = false;
		if (SabotageVerifyGridOnCall >= 1)
		{
			_verifyGridCalls++;
			last = _verifyGridCalls == SabotageVerifyGridOnCall;
		}
		int stride = contactAlloc ? 12 : 4;
		int[] words = new int[3];
		words[1] = numNodes / 2 * stride;
		words[2] = stride * numNodes - 4;
		int[] pattern = new int[4] { 0x5A5A5A01, 0x5A5A5A02, 0x5A5A5A03, 0x5A5A5A04 };
		int[] back = new int[4];
		for (int w = 0; w < words.Length; w++)
		{
			bGridI.SetData(pattern, 0, words[w], 4);
			bGridI.GetData(back, 0, words[w], 4);
			if (last)
			{
				back[0] ^= 1;
			}
			for (int i = 0; i < 4; i++)
			{
				if (pattern[i] != back[i])
				{
					throw new InvalidOperationException(string.Format("SUPER: БУФЕР СЕТКИ НЕ ДОЕХАЛ ДО GPU. Узел-смещение {0} из ", words[w] + i) + string.Format("{0}: записано {1:X8}, прочитано {2:X8}. ", (long)numNodes * stride, pattern[i], back[i]) + string.Format("⚠️ Сетка {0}×{1}×{2} = {3} узлов просит ", nx, ny, nz, numNodes) + string.Format("{0:F0} МиБ при ", (double)numNodes * SuperScenePlan.BytesPerUnit(layout, SuperScenePlan.Per.Node, contactAlloc) * 1e-06) + string.Format("{0} МиБ на устройстве. Это ПРОВАЛ ВЫДЕЛЕНИЯ: ", SystemInfo.graphicsMemorySize) + "D3D пишет «failed to create buffer» нативно, мимо обработчика Unity, `IsValid()` отвечает true, а прогон идёт дальше и возвращает правдоподобные числа, посчитанные ни на чём. Замер, на котором эта проба заведена (лестница S15, случай 1, 0.50 мм): 4.67 млн частиц за 34 с против 583 тыс. за 19 с — вдевятеро быстрее возможного, обломков 0, все сторожа зелены.");
				}
			}
		}
		int[] zero = new int[4];
		for (int w = 0; w < words.Length; w++)
		{
			bGridI.SetData(zero, 0, words[w], 4);
		}
	}

	public static void GroupLayout(int count, out int gx, out int gy)
	{
		int groups = Blocks(count);
		gx = Math.Min(groups, MaxGroupsPerDim);
		gy = (groups - 1 + gx) / gx;
	}

	public static bool LayoutOverflows(int count)
	{
		GroupLayout(count, out int gx, out int gy);
		return gy > MaxGroupsPerDim;
	}

	private void ReportDispatch()
	{
		if (!LogDispatch)
		{
			return;
		}
		GroupLayout(n, out int gxP, out int gyP);
		GroupLayout(numNodes, out int gxN, out int gyN);
		string[] report = new string[8];
		report[0] = string.Format("    [ДИСПЕТЧ] сетка {0}×{1}×{2}, узлов/ячеек {3} ", nx, ny, nz, numNodes);
		report[1] = string.Format("({0:F2} млн), частиц {1}\n", (double)numNodes / 1000000.0, n);
		report[2] = string.Format("              раскладка: частицы {0}×{1}, узлы {2}×{3}; ", gxP, gyP, gxN, gyN);
		report[3] = string.Format("предел на измерение {0}\n", MaxGroupsPerDim);
		report[4] = string.Format("              одномерным диспетчем было бы {0} групп ⇒ ", Blocks(numNodes));
		report[5] = (Blocks(numNodes) > MaxGroupsPerDim) ? "прежняя редакция ЗДЕСЬ УМИРАЛА" : "прежняя редакция справлялась";
		report[6] = "; двумерная раскладка ⇒ ";
		report[7] = (LayoutOverflows(n) || LayoutOverflows(numNodes) || LayoutOverflows(numCells) || LayoutOverflows(numVBlocks)) ? "⛔ НЕ ПОМЕЩАЕТСЯ ДАЖЕ В ДВЕ ОСИ" : "в пределах";
		Debug.Log(string.Concat(report));
	}

	private long EstimateVram()
	{
		SuperScenePlan.Sizes sizes = SizesNow;
		long total = 0L;
		foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.GpuBuffers)
		{
			ref ComputeBuffer field = ref FieldOf(row.name);
			if (field != null)
			{
				total += row.Bytes(in sizes);
			}
		}
		return total;
	}

	private void PushUniforms(float dt)
	{
		cs.SetInt("_N", n);
		cs.SetInt("_NumNodes", numNodes);
		cs.SetInt("_NumCells", numCells);
		cs.SetInt("_NumBlocks", Blocks(numCells));
		cs.SetInt("_Nx", nx);
		cs.SetInt("_Ny", ny);
		cs.SetInt("_Nz", nz);
		cs.SetVector("_Origin", origin);
		cs.SetFloat("_H", h);
		cs.SetFloat("_Dt", dt);
		cs.SetFloat("_Mu", anvilFriction);
		cs.SetFloat("_FpScale", mutFpScaleX1024 ? (FixedPointScale * 1024f) : FixedPointScale);
		cs.SetInt("_Anvil", anvil ? 1 : 0);
		cs.SetInt("_ElasticOnly", elasticOnly ? 1 : 0);
		cs.SetInt("_SkipGridVel", skipGridVelocity ? 1 : 0);
		cs.SetInt("_ClampInjects", clampInjects ? 1 : 0);
		if (projectileParticleEnd < 0 || projectileParticleEnd > n)
		{
			throw new InvalidOperationException(string.Format("SUPER S35: диапазон снаряда [0, {0}) вне набора из {1} частиц — поле частиц ", projectileParticleEnd, n) + "было бы посчитано не по тем телам.");
		}
		if (contactAlloc && projectileParticleEnd > 0 && _route != SuperScatterRoute.FixedPointAtomics)
		{
			throw new InvalidOperationException(string.Format("SUPER S35: двухполевой контакт (TwoFieldContact) включён и снаряд задан ([0, {0})), ", projectileParticleEnd) + string.Format("а выбран маршрут {0}. Контакт перенесён ТОЛЬКО на маршрут A (атомики); маршрут B посчитал бы ", route) + "одним полем, то есть с прилипанием стержня к мишени, — молча другой ответ. Выключите SuperKernelGPU.TwoFieldContact ДО построения ядра или возьмите маршрут A.");
		}
		if (SuperDamage.Enabled && SuperDamage.XiFieldOn != xiAlloc)
		{
			throw new InvalidOperationException("SUPER S43: " + Localization.T("super.s43.refuse.field_mismatch", SuperDamage.XiFieldOn, xiAlloc ? 1 : 0));
		}
		cs.SetInt("_Contact", ContactActive ? 1 : 0);
		cs.SetInt("_ProjEnd", ContactActive ? projectileParticleEnd : 0);
		cs.SetInt("_NodeStride", NodeStrideNow);
		cs.SetInt("_GridVStride", GridVStrideNow);
		cs.SetInt("_FailOn", SuperDamage.Enabled ? 1 : 0);
		cs.SetInt("_MutNoFail", SuperDamage.SabotageNoFailure ? 1 : 0);
		cs.SetInt("_MutDebrisTension", SuperDamage.SabotageDebrisTension ? 1 : 0);
		cs.SetInt("_MutNoSpall", SuperDamage.SabotageNoSpall ? 1 : 0);
		cs.SetInt("_MutNoCavitation", SuperDamage.SabotageNoCavitation ? 1 : 0);
		cs.SetInt("_MutFailNudge", mutFailNudge ? 1 : 0);
		cs.SetInt("_MutNoShear", SuperDamage.SabotageNoShear ? 1 : 0);
		cs.SetInt("_MutShearNudge", mutShearNudge ? 1 : 0);
		cs.SetFloat("_SoftenCoef", SuperDamage.SofteningCoefficient);
		cs.SetFloat("_SoftenFloor", SuperDamage.SofteningFloor);
		cs.SetInt("_AsbOn", SuperDamage.AsbErosionOn ? 1 : 0);
		cs.SetInt("_DebrisWorkOn", SuperDamage.DebrisWorkApplied ? 1 : 0);
		cs.SetInt("_DebrisShapeOn", SuperDamage.DebrisIsotropicF ? 1 : 0);
		cs.SetInt("_WholeShapeOn", SuperDamage.WholeIsotropicF ? 1 : 0);
		cs.SetFloat("_WholeShapeRatio", SuperDamage.WholeShapeResetRatio);
		cs.SetInt("_CohDegenExitOn", SuperDamage.CohesiveDegenerateExit ? 1 : 0);
		cs.SetFloat("_PressureCapN", SuperDamage.PressureCapFactorN);
		cs.SetFloat("_CoherentTensionFloor", SuperDamage.CoherentTensionFloorPa);
		cs.SetFloat("_VelCeiling", velocityCeiling);
		cs.SetInt("_MutZeroDeviator", mutZeroDeviator ? 1 : 0);
		cs.SetInt("_MutNaNAtZero", mutNaNAtZero ? 1 : 0);
		cs.SetInt("_MutFpScaleX1024", mutFpScaleX1024 ? 1 : 0);
		cs.SetInt("_QProfOn", SuperDamage.Enabled ? 1 : 0);
		cs.SetInt("_QProfBins", QProfileBins);
		cs.SetFloat("_RhoFloor", SuperDamage.RhoFloorKgM3);
		cs.SetFloat("_SigEqFloor", SuperDamage.SigEqReturnFloorPa);
		cs.SetInt("_MutEfCollapse", SuperDamage.SabotageEfCollapse ? 1 : 0);
		cs.SetInt("_MutPressureBlowup", SuperDamage.SabotagePressureBlowup ? 1 : 0);
		cs.SetInt("_YieldIters", SuperDamage.YieldIterations);
		cs.SetFloat("_AlphaEpsPa", SuperDamage.AlphaEpsilonTransitionPa);
		cs.SetInt("_SteelMask", (int)SteelMask);
		cs.SetInt("_VBx", vbx);
		cs.SetInt("_VBy", vby);
		cs.SetInt("_VBz", vbz);
		cs.SetInt("_NumVBlocks", numVBlocks);
		cs.SetInt("_PoolBlocks", poolBlocks);
		int sparseSab = (SabotageSparseSkipMarkZFromStart || (SabotageSparseSkipMarkZ && _sparseVerified)) ? 1 : 0;
		if (SabotageSparseShiftSlots)
		{
			sparseSab |= 2;
		}
		cs.SetInt("_SparseSab", sparseSab);
		if (_kwSparse.isValid)
		{
			cs.SetKeyword(in _kwSparse, layout == SuperScenePlan.Layout.Sparse);
		}
		if (_kwXi.isValid)
		{
			cs.SetKeyword(in _kwXi, xiAlloc);
		}
		if (_kwObst.isValid)
		{
			cs.SetKeyword(in _kwObst, bObst != null && !SabotageNoObstacle);
		}
	}

	public static int CountSteelParticles(SuperParticles p, out int coherentSteel)
	{
		uint mask = SteelMask;
		int total = 0;
		coherentSteel = 0;
		for (int i = 0; i < p.count; i++)
		{
			if ((mask & (1u << (p.matId[i] & 0x1F))) != 0)
			{
				total++;
				if (p.noFrag[i] != 0)
				{
					coherentSteel++;
				}
			}
		}
		return total;
	}

	private static int Blocks(int count)
	{
		return Math.Max((count + 255) / 256, 1);
	}

	private void Go(int kernel, int count)
	{
		DispatchRange(kernel, 0, count);
	}

	private void Go(int kernel, int count, int maxPerDispatch)
	{
		if (!ChunkedDispatch || SabotageForce1D || maxPerDispatch <= 0 || count <= maxPerDispatch)
		{
			DispatchRange(kernel, 0, count);
			return;
		}
		int done = 0;
		while (done < count)
		{
			int chunk = Math.Min(count - done, maxPerDispatch);
			DispatchRange(kernel, done, chunk);
			ChunkDispatches++;
			done += maxPerDispatch;
		}
	}

	private void DispatchRange(int kernel, int offset, int count)
	{
		GroupLayout(count, out int gx, out int gy);
		if (SabotageForce1D)
		{
			gx = Blocks(count);
			gy = 1;
		}
		cs.SetInt("_DispStrideX", gx << 8);
		cs.SetInt("_DispGroupsX", gx);
		cs.SetInt("_DispOffset", offset);
		cs.SetInt("_DispEnd", SabotageNoDispEnd ? (-1) : (offset + count));
		cs.Dispatch(kernel, gx, gy, 1);
	}

	public void SyncQueue()
	{
		bGuardBuf.GetData(_profGuard);
		if (layout == SuperScenePlan.Layout.Sparse)
		{
			SparseAfterSync();
		}
	}

	private static GridPathMode GridPathFromCommandLine(GridPathMode def)
	{
		GridPathMode result = def;
		string[] args = Environment.GetCommandLineArgs();
		for (int i = 0; i + 1 < args.Length; i++)
		{
			if (args[i] == "-s34gridpath")
			{
				string value = args[i + 1];
				if (Enum.TryParse(value, ignoreCase: true, out GridPathMode parsed) && Enum.IsDefined(typeof(GridPathMode), parsed))
				{
					result = parsed;
					i++;
				}
				else
				{
					Debug.LogError(string.Format("[S34] довод -s34gridpath «{0}» не понят (Full | Region | Sparse) — путь сетки {1}", value, def));
				}
			}
		}
		if (result != def)
		{
			Debug.Log(string.Format("[S34] ПУТЬ СЕТКИ РЕШАТЕЛЯ: {0} (довод -s34gridpath; умолчание {1})", result, def));
		}
		return result;
	}

	private void PrimeRegion()
	{
		Go(kClearA, numNodes, MaxNodesPerDispatch);
		Go(kGridFromInt, numNodes, MaxNodesPerDispatch);
		_regionPrimed = true;
	}

	private void ReduceBounds()
	{
		cs.SetInt("_RegionSabotage", SabotageShrinkRegion ? 1 : 0);
		Go(kBoundsP, n);
		int groups = Blocks(n);
		ComputeBuffer src = bBoundsA;
		ComputeBuffer dst = bBoundsB;
		while (groups > 0)
		{
			if (groups < 2)
			{
				break;
			}
			cs.SetInt("_BoundsCount", groups);
			cs.SetBuffer(kBoundsL, "_BoundsInR", src);
			cs.SetBuffer(kBoundsL, "_BoundsOut", dst);
			DispatchRange(kBoundsL, 0, groups);
			groups = Blocks(groups);
			ComputeBuffer tmp = src;
			src = dst;
			dst = tmp;
		}
		cs.SetBuffer(kRegionArgs, "_BoundsInR", src);
		cs.SetBuffer(kRegionArgs, "_Region", bRegion);
		cs.SetBuffer(kRegionArgs, "_RegionArgs", bRegionArgs);
		cs.Dispatch(kRegionArgs, 1, 1, 1);
	}

	private void VerifyIndirect()
	{
		int[] reg = new int[6];
		uint[] args = new uint[3];
		bRegion.GetData(reg);
		bRegionArgs.GetData(args);
		Array.Copy(reg, _regionFirst, 6);
		bool inverted = reg[0] > reg[3] || reg[1] > reg[4] || reg[2] > reg[5];
		int[] grid = new int[3] { nx, ny, nz };
		for (int axis = 0; axis < 3; axis++)
		{
			int want = (Math.Max(1, reg[3 + axis] - reg[axis] + 1) + 7) >> 3;
			bool fits = inverted || (reg[axis] >= 0 && reg[3 + axis] < grid[axis]);
			if (args[axis] != want || !fits)
			{
				throw new InvalidOperationException(string.Format("SUPER P3.2: доводы косвенного диспетча не сошлись с областью: область [{0},{1},{2}]…", reg[0], reg[1], reg[2]) + string.Format("[{0},{1},{2}] при сетке {3}×{4}×{5}, доводы ({6},{7},{8}), по оси {9} ", reg[3], reg[4], reg[5], nx, ny, nz, args[0], args[1], args[2], axis) + string.Format("ждали {0}. Буфер доводов ядром не записан или записан не тем — путь по области непригоден.", want));
			}
		}
		uint[] guard = new uint[4];
		bGuardBuf.GetData(guard);
		guard[0] = 1515870810u;
		guard[1] = 1u;
		bGuardBuf.SetData(guard);
		cs.DispatchIndirect(kClearA_R, bRegionArgs, 0u);
		bGuardBuf.GetData(guard);
		if (guard[0] != 0 || guard[1] != 0)
		{
			throw new InvalidOperationException(string.Format("SUPER P3.2: косвенный диспетч очистки НЕ ИСПОЛНИЛСЯ — сторож остался {0:X8}/{1} при доводах ", guard[0], guard[1]) + string.Format("({0},{1},{2}). Молчаливый пропуск диспетча — путь по области непригоден.", args[0], args[1], args[2]));
		}
		_indirectVerified = true;
		if (LogDispatch)
		{
			Debug.Log(string.Format("    [ОБЛАСТЬ] первый шаг: узлы [{0},{1},{2}]…[{3},{4},{5}] из сетки {6}×{7}×{8} — ", reg[0], reg[1], reg[2], reg[3], reg[4], reg[5], nx, ny, nz) + string.Format("{0:P1} узлов; групп 8³: {1}×{2}×{3}", RegionFraction(reg), args[0], args[1], args[2]));
		}
	}

	private double RegionFraction(int[] reg)
	{
		if (reg[0] > reg[3] || reg[1] > reg[4] || reg[2] > reg[5] || numNodes <= 0)
		{
			return 0.0;
		}
		return (double)(reg[4] - reg[1] + 1) * (double)(reg[3] - reg[0] + 1) * (double)(reg[5] - reg[2] + 1) / (double)numNodes;
	}

	public void RegionFractions(out double first, out double now)
	{
		first = -1.0;
		now = -1.0;
		if (!_indirectVerified)
		{
			return;
		}
		int[] reg = new int[6];
		bRegion.GetData(reg);
		first = RegionFraction(_regionFirst);
		now = RegionFraction(reg);
	}

	public static long InitialPoolBlocks(long touched)
	{
		return Math.Max(PoolMinBlocks, (long)Math.Ceiling((double)touched * PoolInitialFactor));
	}

	private static long TouchedBlocks<T>(T pos, Vector3 o, float h, int nx, int ny, int nz, int bx, int by, int bz, byte[] marks) where T : struct, IPositions
	{
		int count = pos.Count;
		if (count <= 0)
		{
			return 0L;
		}
		int nx1 = nx - 1;
		int ny1 = ny - 1;
		int nz1 = nz - 1;
		int[,] cell = new int[3, 2];
		int[] flag = new int[3];
		long touched = 0L;
		for (int p = 0; p < count; p++)
		{
			Vector3 v = pos.At(p);
			bool single = true;
			for (int a = 0; a < 3; a++)
			{
				double t = ((double)v[a] - o[a]) / h - 0.5;
				if (double.IsNaN(t) || t < -1e9 || t > 1e9)
				{
					goto NextParticle;
				}
				double b = Math.Floor(t);
				double frac = t - b;
				if (frac < 0.0001)
				{
					cell[a, 0] = (int)b - 1;
					cell[a, 1] = (int)b;
					single = false;
					flag[a] = 2;
				}
				else if (frac > 0.9999)
				{
					cell[a, 0] = (int)b;
					cell[a, 1] = (int)b + 1;
					single = false;
					flag[a] = 2;
				}
				else
				{
					cell[a, 0] = (int)b;
					flag[a] = 1;
				}
			}
			{
				int tag = (single ? 1 : 0) + 1;
				for (int i = 0; i < flag[0]; i++)
				{
					for (int j = 0; j < flag[1]; j++)
					{
						for (int k = 0; k < flag[2]; k++)
						{
							int cx = cell[0, i];
							int cy = cell[1, j];
							int cz = cell[2, k];
							if (cx < -2 || cy < -2 || cz < -2 || cx > nx1 || cy > ny1 || cz > nz1)
							{
								continue;
							}
							int xLo = Math.Max(cx, 0) >> 3;
							int xHi = Math.Max(0, Math.Min(cx + 2, nx1)) >> 3;
							int yLo = Math.Max(cy, 0) >> 3;
							int yHi = Math.Max(0, Math.Min(cy + 2, ny1)) >> 3;
							int zLo = Math.Max(cz, 0) >> 3;
							int zHi = Math.Max(0, Math.Min(cz + 2, nz1)) >> 3;
							for (int dz = 0; dz < 2; dz++)
							{
								for (int dy = 0; dy < 2; dy++)
								{
									for (int dx = 0; dx < 2; dx++)
									{
										int bi = ((dz == 0) ? zLo : zHi) * by + ((dy == 0) ? yLo : yHi);
										bi = bi * bx + ((dx == 0) ? xLo : xHi);
										if (marks[bi] == 0)
										{
											touched++;
										}
										if (marks[bi] < tag)
										{
											marks[bi] = (byte)tag;
										}
									}
								}
							}
						}
					}
				}
			}
			NextParticle:;
		}
		return touched;
	}

	public static long SparseCapacityFor(SuperParticles p, Vector3 lo, Vector3 hi, float h, out long vblocks, out long touched)
	{
		Vector3 origin = SuperGrid.OriginFor(lo, h);
		SuperGrid.Dims(lo, hi, h, out int nx, out int ny, out int nz);
		int bx = SuperScenePlan.VBlocksAxis(nx);
		int by = SuperScenePlan.VBlocksAxis(ny);
		int bz = SuperScenePlan.VBlocksAxis(nz);
		vblocks = (long)bz * by * bx;
		if (vblocks >= 0x80000000)
		{
			touched = 0L;
			return 0L;
		}
		byte[] marks = new byte[(int)vblocks];
		long t = TouchedBlocks(new NativePositions { a = p.x, count = p.count }, origin, h, nx, ny, nz, bx, by, bz, marks);
		touched = t;
		if (SparseForceCapacityBlocks < 1)
		{
			return InitialPoolBlocks(t);
		}
		return SparseForceCapacityBlocks;
	}

	private void ReleaseWhere(Func<SuperScenePlan.GpuBuffer, bool> pick)
	{
		foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.GpuBuffers)
		{
			if (!pick(row))
			{
				continue;
			}
			ref ComputeBuffer field = ref FieldOf(row.name);
			if (field != null)
			{
				field.Release();
			}
			field = null;
		}
	}

	private void AllocateWhere(Func<SuperScenePlan.GpuBuffer, bool> pick)
	{
		foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.GpuBuffers)
		{
			if (!pick(row) || !Wanted(row))
			{
				continue;
			}
			ref ComputeBuffer field = ref FieldOf(row.name);
			if (field != null)
			{
				throw new InvalidOperationException("SUPER: буфер «" + row.name + "» уже выделен — повторное выделение потеряло бы прежний.");
			}
			field = AllocRow(row);
		}
	}

	private static bool IsSparseTableRow(SuperScenePlan.GpuBuffer r)
	{
		if (r.phase != SuperScenePlan.Phase.Grid)
		{
			return false;
		}
		if (!r.In(SuperScenePlan.Layout.Sparse))
		{
			return false;
		}
		return !SuperScenePlan.IsPoolRow(r);
	}

	private long PoolBytes(long capacity)
	{
		SuperScenePlan.Sizes s = SuperScenePlan.Sizes.Sparse(n, capacity, numVBlocks, contactAlloc);
		long total = 0L;
		foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.GpuBuffers)
		{
			if (!SuperScenePlan.IsPoolRow(row) || !row.In(layout))
			{
				continue;
			}
			total += row.Bytes(in s);
		}
		return total;
	}

	private void BuildSparse()
	{
		int vbxA = SuperScenePlan.VBlocksAxis(nx);
		int vbyA = SuperScenePlan.VBlocksAxis(ny);
		int vbzA = SuperScenePlan.VBlocksAxis(nz);
		long vblocks = (long)vbzA * vbyA * vbxA;
		if (vblocks > int.MaxValue)
		{
			throw new InvalidOperationException(string.Format("SUPER S34 P2: сетка {0}×{1}×{2} даёт {3} виртуальных блоков 8³ — счётчик таблиц блоков переполняет int ", nx, ny, nz, vblocks) + "(предел разреженной сетки: numVBlocks ⩽ int.MaxValue).");
		}
		Vector3[] pos = new Vector3[n];
		bPos.GetData(pos);
		byte[] marks = new byte[vblocks];
		long touched = TouchedBlocks(new ArrayPositions
		{
			a = pos
		}, origin, h, nx, ny, nz, vbxA, vbyA, vbzA, marks);
		long target = ((SparseForceCapacityBlocks > 0) ? SparseForceCapacityBlocks : InitialPoolBlocks(touched));
		vby = vbyA;
		bool needPool = true;
		vbx = vbxA;
		vbz = vbzA;
		bool dimsChanged = bBlockMark == null || numVBlocks != vblocks;
		if (bGridI != null)
		{
			needPool = poolBlocks < target;
		}
		numVBlocks = (int)vblocks;
		if (dimsChanged)
		{
			ReleaseWhere(IsSparseTableRow);
			AllocateWhere(IsSparseTableRow);
		}
		if (needPool)
		{
			ReleaseWhere(SuperScenePlan.IsPoolRow);
			poolBlocks = (int)Math.Min(target, 0x7FFFFFFFL);
			numNodes = 0;
			AllocateWhere(r => SuperScenePlan.IsPoolRow(r) && r.In(layout));
		}
		numCells = poolBlocks << 9;
		numNodes = poolBlocks << 9;
		if (needPool || dimsChanged)
		{
			BindRouteA();
			BindCommon();
			if (routeBAlloc)
			{
				BindRouteB();
			}
			BindLazy();
			VramBytes = EstimateVram();
		}
		_poolTouchedAtBuild = touched;
		_sparseVerified = false;
		VerifyGrid();
		ReportDispatch();
	}

	private void ScanMarks()
	{
		int cells = numVBlocks;
		int lvl1 = Blocks(cells);
		int lvl2 = Blocks(lvl1);
		cs.SetInt("_NumCells", cells);
		cs.SetBuffer(kScanBlocks, "_CellCountR", bBlockMark);
		cs.SetBuffer(kScanBlocks, "_CellStart", bBlockSlot);
		cs.SetBuffer(kScanBlocks, "_BlockSums", bVSums1);
		Go(kScanBlocks, cells);
		cs.SetInt("_NumCells", lvl1);
		cs.SetBuffer(kScanBlocks, "_CellCountR", bVSums1);
		cs.SetBuffer(kScanBlocks, "_CellStart", bVSums1Scan);
		cs.SetBuffer(kScanBlocks, "_BlockSums", bVSums2);
		Go(kScanBlocks, lvl1);
		cs.SetInt("_NumBlocks", lvl2);
		cs.SetBuffer(kScanBlockSums, "_BlockSums", bVSums2);
		cs.Dispatch(kScanBlockSums, 1, 1, 1);
		cs.SetInt("_NumCells", lvl1);
		cs.SetBuffer(kScanApply, "_CellStart", bVSums1Scan);
		cs.SetBuffer(kScanApply, "_BlockSumsR", bVSums2);
		Go(kScanApply, lvl1);
		cs.SetInt("_NumCells", cells);
		cs.SetBuffer(kScanApply, "_CellStart", bBlockSlot);
		cs.SetBuffer(kScanApply, "_BlockSumsR", bVSums1Scan);
		Go(kScanApply, cells);
		cs.SetInt("_NumCells", numCells);
		cs.SetInt("_NumBlocks", Blocks(numCells));
	}

	private void SparseMarkAndList()
	{
		Go(kBlockMark, numVBlocks, MaxNodesPerDispatch);
		Go(kBlockList, n, MaxParticlesPerDispatch);
		ScanMarks();
		Go(kGridFromIntBlocks, numVBlocks, MaxNodesPerDispatch);
	}

	private void ProbeSparseVariant()
	{
		cs.Dispatch(kSparseProbe, 1, 1, 1);
		uint[] probe = new uint[1];
		bBlockMark.GetData(probe, 0, 0, 1);
		if (probe[0] == 0x5A5A0C0Du)
		{
			return;
		}
		uint value = probe[0];
		string reason = ((value == 0xBAD) ? "исполнился плотный вариант: ключ не включился" : "проба не исполнилась вовсе");
		throw new InvalidOperationException(string.Format("SUPER S34 P2: ВАРИАНТ ШЕЙДЕРА SUPER_SPARSE НЕ ИСПОЛНЯЕТСЯ — проба записала {0:X8} вместо 5A5A0C0D ", value) + "(" + reason + "). Разреженная сетка на этом устройстве/сборке непригодна: P2G/G2P адресовали бы узлы плотной сетки в пуле.");
	}

	private void SparseFirstStep()
	{
		ProbeSparseVariant();
		Vector3[] pos = new Vector3[n];
		bPos.GetData(pos);
		int blocks = numVBlocks;
		Go(kBlockClear, blocks, MaxNodesPerDispatch);
		Go(kBlockMark, n, MaxParticlesPerDispatch);
		ScanMarks();
		uint[] counts = new uint[blocks];
		uint[] slots = new uint[blocks];
		bBlockMark.GetData(counts);
		bBlockSlot.GetData(slots);
		long active = 0L;
		for (int i = 0; i < counts.Length; i++)
		{
			active += counts[i];
		}
		if ((double)poolBlocks * 0.7 < (double)active)
		{
			GrowPool(active, "первый шаг");
		}
		cs.SetInt("_PoolBlocks", poolBlocks);
		Go(kBlockList, blocks, MaxNodesPerDispatch);
		uint[] list = new uint[Math.Max(1, poolBlocks)];
		uint[] args = new uint[3];
		uint[] guard = new uint[4];
		bActiveList.GetData(list);
		bBlockArgs.GetData(args);
		bGuardBuf.GetData(guard);
		byte[] marks = new byte[blocks];
		TouchedBlocks(new ArrayPositions
		{
			a = pos
		}, origin, h, nx, ny, nz, vbx, vby, vbz, marks);
		if (SabotageSparseSelfCheck && blocks >= 1)
		{
			for (int i = 0; i < blocks; i++)
			{
				if (counts[i] != 0)
				{
					marks[i] = 0;
					break;
				}
			}
		}
		int firstDiv = -1;
		int strongNoGpu = 0;
		int gpuNoCpu = 0;
		int not01 = 0;
		int slotMismatch = 0;
		int listMismatch = 0;
		long slotSum = 0L;
		for (int i = 0; i < blocks; i++)
		{
			uint count = counts[i];
			bool rowDiv = count > 1;
			if (count > 1)
			{
				not01++;
			}
			if (marks[i] == 2)
			{
				if (count == 0)
				{
					strongNoGpu++;
					rowDiv = true;
				}
			}
			else if (count != 0 && marks[i] == 0)
			{
				gpuNoCpu++;
				rowDiv = true;
			}
			if (slots[i] != (uint)slotSum)
			{
				slotMismatch++;
				rowDiv = true;
			}
			if (count == 0 || poolBlocks <= slotSum)
			{
				if (rowDiv)
				{
					if (firstDiv < 0)
					{
						firstDiv = i;
					}
				}
			}
			else if (list[(int)slotSum] != (uint)i)
			{
				listMismatch++;
				if (firstDiv < 0)
				{
					firstDiv = i;
				}
			}
			slotSum += count;
		}
		int sample;
		int expectList;
		long capped = Math.Min(active, poolBlocks);
		if (capped == 0)
		{
			sample = 1;
			expectList = 0;
		}
		else
		{
			sample = (int)Math.Min(capped, 65535L);
			expectList = (int)((capped + sample - 1) / sample);
		}
		bool argsOk = args[0] == sample && args[1] == expectList && args[2] == 1;
		bool guardOk = guard.Length > 2 && guard[2] == (uint)active && (long)active <= (long)guard[3];
		if (listMismatch + slotMismatch + not01 + gpuNoCpu + strongNoGpu > 0 || !argsOk || !guardOk)
		{
			throw new InvalidOperationException("SUPER S34 P2 G-S3: САМОПРОВЕРКА ПЕРВОГО ШАГА РАЗРЕЖЕННОЙ СЕТКИ НЕ СОШЛАСЬ на " + SystemInfo.graphicsDeviceName + " " + string.Format("({0}): блоков {1}, активных по GPU {2}, ёмкость {3}; отметок GPU нет там, ", SystemInfo.graphicsDeviceType, blocks, active, poolBlocks) + string.Format("где CPU их требует: {0}; лишних отметок GPU: {1}; отметок не 0/1: {2}; слотов ≠ префиксной сумме: ", strongNoGpu, gpuNoCpu, not01) + string.Format("{0}; списка ≠ обратному слоту: {1}; доводы ({2},{3},{4}) против ({5},{6},1): ", slotMismatch, listMismatch, args[0], args[1], args[2], sample, expectList) + string.Format("{0}; сторож [2]={1}, [3]={2}: {3}; ", argsOk ? "сошлись" : "НЕ сошлись", guard[2], guard[3], guardOk ? "сошёлся" : "НЕ сошёлся") + string.Format("первый расходящийся блок {0}{1}. ", firstDiv, SabotageSparseSelfCheck ? " — ДИВЕРСИЯ SabotageSparseSelfCheck" : "") + "Разреженная сетка на этом устройстве непригодна — это растяжка D3D12/других производителей (F7), а не шум.");
		}
		int activeNow = (int)guard[2];
		int guard3 = (int)guard[3];
		guard[0] = 1515870810u;
		guard[1] = 1u;
		bGuardBuf.SetData(guard);
		cs.DispatchIndirect(kClearBlocks, bBlockArgs, 0u);
		bGuardBuf.GetData(guard);
		if (guard[0] != 0 || guard[1] != 0 || guard[2] != (uint)activeNow || guard[3] != (uint)guard3)
		{
			throw new InvalidOperationException(string.Format("SUPER S34 P2 G-S3: косвенный диспетч очистки блоков НЕ ИСПОЛНИЛСЯ как должен — сторож {0:X8}/{1}/", guard[0], guard[1]) + string.Format("{0}/{1} при доводах ({2},{3},{4}) (ждали 0/0/{5}/{6}). Молчаливый пропуск ", guard[2], guard[3], args[0], args[1], args[2], activeNow, guard3) + "диспетча или сброс, стирающий число активных блоков, — разреженная сетка непригодна.");
		}
		_sparseVerified = true;
		_activeFirst = active;
		if ((long)poolBlocks < active)
		{
			_poolOverflowSeen = true;
			Debug.LogWarning(string.Format("[S34 P2] ⛔ FAIL_GRIDPOOL на первом шаге: активных блоков {0} при ёмкости {1}", active, poolBlocks) + ((_poolRefusal != null) ? (" — " + _poolRefusal) : (SparseGrowthOn ? " (рост выключен)" : "")));
		}
		_activeTrace.Add(new KeyValuePair<int, uint>(_stepIndex, (uint)active));
		SparseSelfChecksPassed++;
		if (LogDispatch)
		{
			Debug.Log(string.Format("    [РАЗРЕЖЕННАЯ] первый шаг: активных блоков {0} из {1} ({2:P1}), ", active, blocks, (double)active / (double)Math.Max(1, blocks)) + string.Format("ёмкость пула {0}, тронуто запеканием {1}; самопроверка G-S3 сошлась", poolBlocks, _poolTouchedAtBuild));
		}
	}

	private string PoolWhyNot(long capTry, long capNow, out long need, out long allowed)
	{
		int stride = contactAlloc ? 12 : 4;
		need = ExternalVramBytes - PoolBytes(capNow) + PoolBytes(capTry) + EstimateVram();
		allowed = SuperMemory.AllowedBytes;
		SuperScenePlan.Sizes s = SuperScenePlan.Sizes.Sparse(n, capTry, numVBlocks, contactAlloc);
		long worst = 0L;
		string worstName = null;
		foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.GpuBuffers)
		{
			if (!SuperScenePlan.IsPoolRow(row) || !row.In(layout))
			{
				continue;
			}
			long bytes = row.Bytes(in s);
			if (worst < bytes)
			{
				worst = bytes;
				worstName = row.name;
			}
		}
		if ((long)stride * capTry * 512 >= 0x80000000L)
		{
			return string.Format("пул {0} блоков × {1}×{2} int переполняет счётчик буфера GridI (int)", capTry, 512, stride);
		}
		if (SuperMemory.DeviceBytes > 0 && SuperMemory.PerResourceCapBytes < worst)
		{
			return string.Format("буфер {0} стал бы {1:F0} МиБ одним ресурсом при потолке {2:F0} МиБ", worstName, (double)worst * 1e-06, (double)SuperMemory.PerResourceCapBytes * 1e-06);
		}
		if (allowed < 1 || need <= allowed)
		{
			return null;
		}
		return string.Format("нужно {0:F0} МиБ, допущено {1:F0} МиБ", (double)need * 1e-06, (double)allowed * 1e-06);
	}

	private bool GrowPool(long highWater, string where)
	{
		if (!SparseGrowthOn)
		{
			return false;
		}
		long cap = poolBlocks;
		if ((double)highWater <= (double)cap * PoolGrowAt)
		{
			return false;
		}
		long want = cap;
		do
		{
			want *= 2;
		}
		while ((double)want * PoolGrowAt < (double)highWater);
		if (want == cap)
		{
			return false;
		}
		string why = PoolWhyNot(want, cap, out long need, out long allowed);
		bool capped = false;
		long best = want;
		if (why != null)
		{
			long lo = cap;
			long hi = want;
			while (hi - lo > 1)
			{
				long mid = lo + (hi - lo) / 2;
				if (PoolWhyNot(mid, cap, out long midNeed, out long midAllowed) == null)
				{
					lo = mid;
				}
				else
				{
					hi = mid;
				}
			}
			if (lo <= cap)
			{
				if (_poolRefusal == null)
				{
					if (0 < allowed && allowed < need)
					{
						PoolRefusalAllowedBytes = allowed;
						PoolRefusalStep = _stepIndex;
						PoolRefusalNeedBytes = need;
						PoolRefusalActive = highWater;
					}
					_poolRefusal = string.Format("рост пула {0} → {1} блоков не допущен: {2}; шаг {3}, активных блоков ", cap, want, why, _stepIndex) + string.Format("(наибольшее) {0} ({1})", highWater, where);
					Debug.LogWarning("[S34 P2] " + _poolRefusal);
				}
				return false;
			}
			capped = true;
			best = lo;
		}
		ReleaseWhere(r => SuperScenePlan.IsPoolRow(r) && r.In(layout));
		poolBlocks = (int)best;
		AllocateWhere(r => SuperScenePlan.IsPoolRow(r) && r.In(layout));
		numCells = poolBlocks << 9;
		numNodes = poolBlocks << 9;
		BindRouteA();
		BindCommon();
		if (routeBAlloc)
		{
			BindRouteB();
		}
		BindLazy();
		VramBytes = EstimateVram();
		VerifyGrid();
		_poolGrowths++;
		string note = (capped ? " (×2 не допущен — наибольшая допущенная, S38b)" : "");
		string text = string.Format("шаг {0}: пул {1} → {2} блоков{3} ", _stepIndex, cap, best, note) + string.Format("(активных наибольшее {0}, {1}); видеопамять ядра {2:F1} МиБ", highWater, where, (double)VramBytes * 1e-06);
		_growthLog.Add(text);
		Debug.Log("[S34 P2] рост пула — " + text);
		return true;
	}

	private void SparseAfterSync()
	{
		_stepsSinceCheck = 0;
		if (!_sparseVerified)
		{
			return;
		}
		uint active = _profGuard[3];
		if (_activeTrace.Count < 100000)
		{
			_activeTrace.Add(new KeyValuePair<int, uint>(_stepIndex, _profGuard[2]));
		}
		if (active <= poolBlocks)
		{
			if ((double)active <= (double)poolBlocks * PoolGrowAt)
			{
				return;
			}
			GrowPool(active, "синхронизация");
			return;
		}
		if (!_poolOverflowSeen)
		{
			Debug.LogWarning(string.Format("[S34 P2] ⛔ FAIL_GRIDPOOL: активных блоков {0} при ёмкости {1} (шаг {2}) — ", active, poolBlocks, _stepIndex) + "узлы сверх ёмкости не считались; прогон недействителен, счёт остановлен до конца окна." + ((_poolRefusal != null) ? (" Причина: " + _poolRefusal) : ""));
		}
		_poolOverflowSeen = true;
	}

	public SparseStats ReadSparseStats()
	{
		SparseStats s = new SparseStats();
		s.sparse = layout == SuperScenePlan.Layout.Sparse;
		s.nx = nx;
		s.ny = ny;
		s.nz = nz;
		s.virtualNodes = virtualNodes;
		if (!s.sparse)
		{
			return s;
		}
		uint[] guard = new uint[4];
		bGuardBuf.GetData(guard);
		s.vblocks = numVBlocks;
		s.poolBlocks = poolBlocks;
		s.touchedAtBuild = _poolTouchedAtBuild;
		s.activeFirst = _activeFirst;
		s.activeLast = guard[2];
		s.activeMax = guard[3];
		s.growths = _poolGrowths;
		s.overflow = _poolOverflowSeen || (long)poolBlocks < (long)guard[3];
		s.refusal = _poolRefusal;
		s.poolBytes = PoolBytes(poolBlocks);
		s.tableBytes = 0L;
		SuperScenePlan.Sizes sizes = SizesNow;
		foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.GpuBuffers)
		{
			if (row.phase == SuperScenePlan.Phase.Grid && row.In(SuperScenePlan.Layout.Sparse) && !SuperScenePlan.IsPoolRow(row))
			{
				s.tableBytes += row.Bytes(in sizes);
			}
		}
		s.trace = new List<KeyValuePair<int, uint>>(_activeTrace);
		s.growthLog = new List<string>(_growthLog);
		return s;
	}

	public void Step(float dt)
	{
		_stepIndex++;
		if (layout == SuperScenePlan.Layout.Sparse)
		{
			if (GridPath != GridPathMode.Sparse)
			{
				throw new InvalidOperationException(string.Format("SUPER S34 P2: ядро построено на РАЗРЕЖЕННОЙ сетке, а GridPath = {0} — путь сетки переключён после ", GridPath) + "постройки ядра. У ядра нет плотной сетки; постройте новое ядро при нужном пути.");
			}
			if (_poolOverflowSeen)
			{
				return;
			}
			if (_sparseVerified)
			{
				_stepsSinceCheck++;
				if (SparseCheckEverySteps <= _stepsSinceCheck)
				{
					bGuardBuf.GetData(_profGuard);
					SparseAfterSync();
					if (_poolOverflowSeen)
					{
						return;
					}
				}
			}
		}
		else if (_route == SuperScatterRoute.FixedPointAtomics && GridPath == GridPathMode.Sparse && !SabotageForce1D)
		{
			throw new InvalidOperationException("SUPER S34 P2: ядро построено на ПЛОТНОЙ сетке, а GridPath = Sparse — путь сетки переключён после постройки. Пула у ядра нет; постройте новое ядро при нужном пути.");
		}
		long t0 = 0L;
		if (ProfileKernels)
		{
			t0 = ProfileMark();
		}
		PushUniforms(dt);
		long now = t0;
		if (ProfileKernels)
		{
			now = Stopwatch.GetTimestamp();
			Profile.Add(ProfSlot.Uniforms, now - t0);
		}
		if (layout == SuperScenePlan.Layout.Sparse)
		{
			if (!_sparseVerified)
			{
				SparseFirstStep();
			}
			else
			{
				Go(kBlockMark, numVBlocks, MaxNodesPerDispatch);
				Go(kBlockList, n, MaxParticlesPerDispatch);
				ScanMarks();
				Go(kGridFromIntBlocks, numVBlocks, MaxNodesPerDispatch);
			}
			if (ProfileKernels)
			{
				now = ProfileLap(ProfSlot.KBounds, now, sync: true);
			}
			cs.DispatchIndirect(kBlockClear, bBlockArgs, 0u);
			if (ProfileKernels)
			{
				now = ProfileLap(ProfSlot.KClearGridA, now, sync: true);
			}
			Go(kP2GA, n, MaxParticlesPerDispatch);
			if (ProfileKernels)
			{
				now = ProfileLap(ProfSlot.KP2G_Atomic, now, sync: true);
			}
			cs.DispatchIndirect(kGridFromIntBlocks, bBlockArgs, 0u);
			if (ProfileKernels)
			{
				now = ProfileLap(ProfSlot.KGridFromInt, now, sync: true);
			}
		}
		else if (_route == SuperScatterRoute.FixedPointAtomics && GridPath == GridPathMode.Region && !SabotageForce1D)
		{
			if (!_regionPrimed)
			{
				Go(kClearA, numNodes, MaxNodesPerDispatch);
				Go(kGridFromInt, numNodes, MaxNodesPerDispatch);
				_regionPrimed = true;
			}
			ReduceBounds();
			if (!_indirectVerified)
			{
				VerifyIndirect();
			}
			if (ProfileKernels)
			{
				now = ProfileLap(ProfSlot.KBounds, now, sync: true);
			}
			cs.DispatchIndirect(kClearA_R, bRegionArgs, 0u);
			if (ProfileKernels)
			{
				now = ProfileLap(ProfSlot.KClearGridA, now, sync: true);
			}
			Go(kP2GA, n, MaxParticlesPerDispatch);
			if (ProfileKernels)
			{
				now = ProfileLap(ProfSlot.KP2G_Atomic, now, sync: true);
			}
			cs.DispatchIndirect(kGridFromInt_R, bRegionArgs, 0u);
			if (ProfileKernels)
			{
				now = ProfileLap(ProfSlot.KGridFromInt, now, sync: true);
			}
		}
		else if (_route == SuperScatterRoute.FixedPointAtomics)
		{
			Go(kClearA, numNodes, MaxNodesPerDispatch);
			if (ProfileKernels)
			{
				now = ProfileLap(ProfSlot.KClearGridA, now, sync: true);
			}
			Go(kP2GA, n, MaxParticlesPerDispatch);
			if (ProfileKernels)
			{
				now = ProfileLap(ProfSlot.KP2G_Atomic, now, sync: true);
			}
			Go(kGridFromInt, numNodes, MaxNodesPerDispatch);
			if (ProfileKernels)
			{
				now = ProfileLap(ProfSlot.KGridFromInt, now, sync: true);
			}
		}
		else
		{
			Go(kClearB, numNodes);
			Go(kHash, n);
			Go(kClearCells, numCells);
			Go(kCount, n);
			Go(kScanBlocks, numCells);
			cs.Dispatch(kScanBlockSums, 1, 1, 1);
			Go(kScanApply, numCells);
			Go(kScatter, n);
			Go(kSortCells, numCells);
			Go(kP2GB, numNodes);
			Go(kGridFromFloat, numNodes);
			if (ProfileKernels)
			{
				now = ProfileLap(ProfSlot.RouteB, now, sync: true);
			}
		}
		Go(kG2P, n, MaxParticlesPerDispatch);
		if (ProfileKernels)
		{
			now = ProfileLap(ProfSlot.KG2P, now, sync: true);
		}
		Go(kUpdate, n, MaxParticlesPerDispatch);
		if (ProfileKernels)
		{
			ProfileLap(ProfSlot.KUpdate, now, sync: true);
		}
	}

	private void ProfSync()
	{
		bGuardBuf.GetData(_profGuard);
	}

	public long ProfileMark()
	{
		bGuardBuf.GetData(_profGuard);
		long t0 = Stopwatch.GetTimestamp();
		bGuardBuf.GetData(_profGuard);
		long t1 = Stopwatch.GetTimestamp();
		Profile.Add(ProfSlot.EmptyReadback, t1 - t0);
		return t1;
	}

	private long ProfileLap(ProfSlot s, long t0, bool sync = true)
	{
		if (sync)
		{
			bGuardBuf.GetData(_profGuard);
		}
		long now = Stopwatch.GetTimestamp();
		Profile.Add(s, now - t0);
		return now;
	}

	public void ProfileDriveDone(long t0)
	{
		ProfileLap(ProfSlot.EraDrive, t0, sync: true);
	}

	public void RunGuard()
	{
		Go(kGuard, n, MaxParticlesPerDispatch);
	}

	public SuperGpuFail ReadFail(out int pass, out int particle)
	{
		uint[] array = new uint[41];
		bFail.GetData(array);
		pass = (int)array[1];
		particle = (int)array[2];
		return (SuperGpuFail)array[0];
	}

	public void ReadWholeGuardHits(out uint jClampHits, out uint pressureCapHits)
	{
		uint[] array = new uint[41];
		bFail.GetData(array);
		jClampHits = array[3];
		pressureCapHits = array[4];
	}

	public uint[] ReadLimitCounters()
	{
		uint[] array = new uint[41];
		bFail.GetData(array);
		return array;
	}

	public uint[] ReadQProfile()
	{
		uint[] array = new uint[3113];
		bFail.GetData(array);
		return array;
	}

	public void ClearQProfile()
	{
		uint[] array = new uint[3072];
		bFail.SetData(array, 0, 41, 3072);
	}

	public uint EscapedParticleSteps()
	{
		uint[] array = new uint[4];
		bEscaped.GetData(array);
		return array[0];
	}

	public void ReadGuard(out uint maxEncoded, out bool overflow)
	{
		uint[] array = new uint[4];
		bGuardBuf.GetData(array);
		maxEncoded = array[0];
		overflow = array[1] != 0;
	}

	public void GridTotals(out double mass, out Vector3 mom)
	{
		if (layout == SuperScenePlan.Layout.Sparse)
		{
			throw new InvalidOperationException("SUPER S34 P2: GridTotals читает ВСЮ сетку, а ядро построено на разреженной — у пула нет «всей сетки». Прибор S1-4d строит ядро при GridPath = Full.");
		}
		int stride = GridVStrideNow;
		int count = stride * numNodes;
		Vector4[] vals = new Vector4[count];
		bGridV.GetData(vals, 0, 0, count);
		KahanSum km = default(KahanSum);
		KahanSum kx = default(KahanSum);
		KahanSum ky = default(KahanSum);
		KahanSum kz = default(KahanSum);
		for (int i = 0; i < count; i++)
		{
			Vector4 v = vals[i];
			km.Add(v.w);
			kx.Add((double)v.x * v.w);
			ky.Add((double)v.y * v.w);
			kz.Add((double)v.z * v.w);
		}
		mass = km.Value;
		mom = new Vector3((float)kx.Value, (float)ky.Value, (float)kz.Value);
	}

	public void ScatterOnly(float dt)
	{
		if (layout == SuperScenePlan.Layout.Sparse)
		{
			throw new InvalidOperationException("SUPER S34 P2: ScatterOnly — полный проход по ВСЕЙ сетке для прибора S1-4d, а ядро построено на разреженной. Постройте ядро при GridPath = Full.");
		}
		PushUniforms(dt);
		if (_route == SuperScatterRoute.FixedPointAtomics)
		{
			Go(kClearA, numNodes);
			Go(kP2GA, n);
			Go(kGridFromInt, numNodes);
			return;
		}
		Go(kClearB, numNodes);
		Go(kHash, n);
		Go(kClearCells, numCells);
		Go(kCount, n);
		Go(kScanBlocks, numCells);
		cs.Dispatch(kScanBlockSums, 1, 1, 1);
		Go(kScanApply, numCells);
		Go(kScatter, n);
		Go(kSortCells, numCells);
		Go(kP2GB, numNodes);
		Go(kGridFromFloat, numNodes);
	}

	public double KineticEnergy()
	{
		EnsurePartial();
		Go(kReduce, n);
		float[] array = new float[Math.Max(numBlocks, 1)];
		bPartial.GetData(array);
		KahanSum sum = default(KahanSum);
		for (int i = 0; i < array.Length; i++)
		{
			sum.Add(array[i]);
		}
		return sum.Value;
	}

	public void EvalConstitutive(ContinuumMaterialId id, Vector4[] points, Vector2[] outValues)
	{
		EnsureEval();
		bScal.SetData(points);
		PushUniforms(0f);
		cs.SetInt("_EvalMatId", (int)id);
		Go(kEval, points.Length);
		bEval.GetData(outValues);
	}

	public List<KeyValuePair<string, ComputeBuffer>> AuditBuffers()
	{
		List<KeyValuePair<string, ComputeBuffer>> list = new List<KeyValuePair<string, ComputeBuffer>>(SuperScenePlan.GpuBuffers.Count);
		foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.GpuBuffers)
		{
			ref ComputeBuffer field = ref FieldOf(row.name);
			list.Add(new KeyValuePair<string, ComputeBuffer>(row.name, field));
		}
		return list;
	}

	private bool PoolCapAdmissible(long capTry)
	{
		int stride = contactAlloc ? 6144 : 2048;
		if ((long)(int.MaxValue / stride) < capTry)
		{
			return false;
		}
		if (SuperMemory.DeviceBytes <= 0)
		{
			return true;
		}
		SuperScenePlan.Sizes s = SuperScenePlan.Sizes.Sparse(n, capTry, numVBlocks, contactAlloc);
		long worst = 0L;
		foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.GpuBuffers)
		{
			if (!SuperScenePlan.IsPoolRow(row) || !row.In(layout))
			{
				continue;
			}
			long bytes = row.Bytes(in s);
			if (worst < bytes)
			{
				worst = bytes;
			}
		}
		return worst <= SuperMemory.PerResourceCapBytes;
	}

	internal int MaxAdmissiblePoolBlocks()
	{
		if (layout != SuperScenePlan.Layout.Sparse)
		{
			return poolBlocks;
		}
		if (!PoolCapAdmissible(poolBlocks))
		{
			return poolBlocks;
		}
		long lo = poolBlocks;
		long hi = Math.Max(1L, poolBlocks);
		while (PoolCapAdmissible(hi) && hi < 0x10000000000L)
		{
			lo = hi;
			hi *= 2;
		}
		if (PoolCapAdmissible(hi))
		{
			return (int)Math.Min(int.MaxValue, hi);
		}
		while (hi - lo > 1)
		{
			long mid = lo + (hi - lo) / 2;
			if (PoolCapAdmissible(mid))
			{
				lo = mid;
			}
			else
			{
				hi = mid;
			}
		}
		return (int)Math.Min(int.MaxValue, lo);
	}

	internal long PoolHeadroomBytes()
	{
		if (layout != SuperScenePlan.Layout.Sparse)
		{
			return 0L;
		}
		int cap = MaxAdmissiblePoolBlocks();
		return Math.Max(0L, PoolBytes(cap) - PoolBytes(poolBlocks));
	}

	public SuperGpuMaterial[] AuditMaterialTable()
	{
		if (_auditMaterialTable != null)
		{
			return (SuperGpuMaterial[])_auditMaterialTable.Clone();
		}
		return null;
	}

	private void ReleaseGrid()
	{
		foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.GpuBuffers)
		{
			if (row.phase != SuperScenePlan.Phase.Grid)
			{
				continue;
			}
			ref ComputeBuffer field = ref FieldOf(row.name);
			if (field != null)
			{
				field.Release();
			}
			field = null;
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		Interlocked.Decrement(ref _liveKernels);
		foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.GpuBuffers)
		{
			if (row.phase == SuperScenePlan.Phase.Grid)
			{
				continue;
			}
			ref ComputeBuffer field = ref FieldOf(row.name);
			if (field != null)
			{
				field.Release();
			}
		}
		ReleaseGrid();
	}

	static SuperKernelGPU()
	{
		TwoFieldContact = true;
		SabotageAllocateAll = false;
		KernelNames = new string[32]
		{
			"KClearGridA", "KP2G_Atomic", "KGridFromInt", "KClearGridB", "KHash", "KClearCells", "KCount", "KScanBlocks",
			"KScanBlockSums", "KScanApply", "KScatterIdx", "KSortCells", "KP2G_Gather", "KGridFromFloat", "KG2P", "KUpdate",
			"KReduceDiag", "KGuardNaN", "KEvalConstitutive", "KEvalFailure", "KEvalSigma1", "KBoundsParticles", "KBoundsLevel", "KRegionArgs",
			"KClearGridA_R", "KGridFromInt_R", "KBlockClear", "KBlockMark", "KBlockList", "KClearBlocks", "KGridFromIntBlocks", "KSparseProbe"
		};
		LogDispatch = false;
		SabotageNoObstacle = false;
		SteelRows = new ContinuumMaterialId[5]
		{
			ContinuumMaterialId.Steel4340,
			ContinuumMaterialId.RHA_MeyerKleponis,
			ContinuumMaterialId.RHA_Grey,
			ContinuumMaterialId.HardCoreSteel_N12e,
			ContinuumMaterialId.CastArmourSteel
		};
		SabotageForce1D = false;
		MaxParticlesPerDispatch = 1048576;
		MaxNodesPerDispatch = 4194304;
		ChunkedDispatch = true;
		SabotageNoDispEnd = false;
		GridPath = GridPathFromCommandLine(GridPathMode.Sparse);
		SabotageShrinkRegion = false;
		SabotageSparseSkipMarkZ = false;
		SabotageSparseSkipMarkZFromStart = false;
		SabotageSparseShiftSlots = false;
		SabotageSparseSelfCheck = false;
		SparseForceCapacityBlocks = 0;
		SparseGrowthOn = true;
		SparseCheckEverySteps = 16;
		ProfileKernels = false;
		Profile = new KernelProfile();
	}
}
