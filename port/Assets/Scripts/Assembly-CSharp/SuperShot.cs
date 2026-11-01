using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

public static class SuperShot
{
	private sealed class LogWatch : IDisposable
	{
		private int _errors;

		private string _first;

		public int Errors => _errors;

		public string First => _first;

		public LogWatch()
		{
			Application.logMessageReceived += OnLog;
		}

		public LogWatch(int errors, string first)
			: this()
		{
			_errors = errors;
			_first = first;
		}

		private void OnLog(string msg, string stack, LogType type)
		{
			if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
			{
				_errors++;
				if (_first == null && msg != null && msg.Length > 200)
				{
					_first = msg.Substring(0, 200);
				}
			}
		}

		public void Dispose()
		{
			Application.logMessageReceived -= OnLog;
		}
	}

	public struct Result
	{
		public SuperOutcomeResult outcome;

		public int steps;

		public int particles;

		public float simulatedTimeS;

		public float dtS;

		public double wallClockMs;

		public string failure;

		public string failureKey;

		public object[] failureArgs;

		public SuperRunRecord.FailClass failClass;

		public bool ok;

		public double escapedFraction;

		public double escapedMassFractionEnd;

		public float plateMeanDisplacementM;

		public float rodCentreTravelM;

		public int unityErrors;

		public long gridNodes;

		public long obstacleNodes;

		public double keyframeHookMs;

		public double bakeMs;

		public double uploadMs;

		public double stepLoopMs;

		public double hookWaitMs;

		public double yieldMs;

		public double paceYieldMs;

		public long chunkDispatches;

		public double regionFirstFraction;

		public double regionLastFraction;

		public SuperKernelGPU.SparseStats sparse;

		public float displayMarginM;

		public float solverMarginM;

		public long solverVBlocks;

		public uint wholeJClampHits;

		public uint wholePressureCapHits;

		public uint[] limitCounters;

		public float limitCapK;

		public int steelParticles;

		public int coherentSteelParticles;

		public double degeneracyFraction;

		public bool abortedEarly;

		public int abortedAtStep;

		public int stepsDone;
	}

	public struct FinalState
	{
		public SuperBaker.Baked baked;

		public Vector3[] pos;

		public Vector3[] vel;

		public float[] damage;

		public float spacing;
	}

	public struct KeyframeContext
	{
		public SuperKernelGPU kernel;

		public SuperBaker.Baked baked;

		public int step;

		public float simTimeS;

		public Vector3 gridLo;

		public Vector3 gridHi;

		public float dtS;

		public int nextKf;

		public int stepsTotal;

		public int unityErrors;

		public string unityFirst;
	}

	public sealed class Resume
	{
		public SuperKernelGPU.FullState state;

		public int step;

		public int nextKf;

		public int unityErrors;

		public string unityFirst;

		public SuperEraDrive.State[] drives;

		public float dtS;
	}

	internal enum Proj
	{
		Rod = 0,
		Shell = 1,
		Jet = 2
	}

	internal sealed class RunBox
	{
		public Result r;

		public int stepNow;

		public int stepsTotal;

		public bool abortRequested;

		public int abortFrame;

		public string abortText;

		public string abortKey;

		public object[] abortArgs;

		public int abortedAtStep;
	}

	public struct Quiet
	{
		public float maxSpeedMps;

		public float centroidDriftM;

		public float maxDisplacementM;

		public int pinnedCount;

		public int particles;

		public int steps;

		public double keJ;

		public bool ok;

		public string failure;
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass33_0
	{
		public Vector3[] pos;

		public Vector3[] vel;

		public Vector3[] plate0;

		public float[] cMass;

		public float[] cV0;

		public SuperBaker.Body cRod;

		public SuperBaker.Body cPlate;

		public float cDia;

		public double cKe0;

		public float[] cDmg;

		internal SuperOutcomeResult _003CRunCore_003Eb__0()
		{
			return default(SuperOutcomeResult);
		}
	}

	[CompilerGenerated]
	private sealed class _003CRunCore_003Ed__33 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public Proj kind;

		public SuperRodSpec rod;

		public float spacing;

		public RunBox box;

		public SuperShellSpec shell;

		public SuperJetSpec jet;

		public IList<SuperPlateSpec> plates;

		public int ensembleMember;

		public float durationS;

		public ComputeShader cs;

		public SuperScatterRoute route;

		public SuperDecor obstacles;

		public SuperEraDrive drive;

		public Resume resume;

		public Action<KeyframeContext> keyframe;

		public float keyframeEveryS;

		public Func<bool> hookReady;

		public SuperStepPacer pacer;

		private _003C_003Ec__DisplayClass33_0 _003C_003E8__1;

		public Action<FinalState> onFinal;

		private Result _003Cr_003E5__2;

		private SuperBaker.Baked _003Cbaked_003E5__3;

		private SuperKernelGPU _003Cg_003E5__4;

		private LogWatch _003Clogs_003E5__5;

		private float _003CrodV_003E5__6;

		private float _003CrodDia_003E5__7;

		private SuperParticles _003Cp_003E5__8;

		private SuperBaker.Body _003CrodBody_003E5__9;

		private SuperBaker.Body _003CplateBody_003E5__10;

		private double _003Cke0_003E5__11;

		private Vector3 _003CrodCentre0_003E5__12;

		private Vector3 _003Clo_003E5__13;

		private Vector3 _003Chi_003E5__14;

		private int _003Cs0_003E5__15;

		private Stopwatch _003Csw_003E5__16;

		private Stopwatch _003CstepSw_003E5__17;

		private Stopwatch _003CwaitSw_003E5__18;

		private Stopwatch _003CyieldSw_003E5__19;

		private Stopwatch _003CpaceSw_003E5__20;

		private Stopwatch _003ChookSw_003E5__21;

		private int _003CstepsToRun_003E5__22;

		private float[] _003CdmgFinal_003E5__23;

		private KeyframeContext _003Cctx_003E5__24;

		private int _003CnextKf_003E5__25;

		private Action _003CsyncQueue_003E5__26;

		private int _003Cs_003E5__27;

		private float _003Ct_003E5__28;

		private Task<SuperOutcomeResult> _003Cjudge_003E5__29;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CRunCore_003Ed__33(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			if (_003C_003E1__state == -3 || (uint)(_003C_003E1__state - 1) <= 7u)
			{
				_003C_003Em__Finally1();
			}
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			if (SuperShot.SabotageSkipAbortCleanup)
			{
				_003Clogs_003E5__5?.Dispose();
				SuperShot.LastBakedForAudit = _003Cbaked_003E5__3;
			}
			else
			{
				_003Clogs_003E5__5?.Dispose();
				_003Cg_003E5__4?.Dispose();
				_003Cbaked_003E5__3?.Dispose();
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	public static float Cfl;

	public static float MarginM;

	public static bool GameSolverDomain;

	public const long SolverTableBudgetBytes = 67108864L;

	public const float LivenessFloorFraction = 0.01f;

	public static bool SabotageSkipSolve;

	public static bool SabotageSkipAbortCleanup;

	public static long GridBytesPerNode => (long)Math.Ceiling(SuperScenePlan.BytesPerUnit(SuperScenePlan.Layout.DenseA, SuperScenePlan.Per.Node, SuperKernelGPU.TwoFieldContact));

	public static float VramShareCeiling => 0.8f;

	public static SuperBaker.Baked LastBakedForAudit { get; private set; }

	public static float SolverMarginFor(bool game, Vector3 displayLo, Vector3 displayHi, float h, float displayMarginM, float threatSpeedMps, float durationS, out long vblocks)
	{
		vblocks = 0L;
		float m = Mathf.Abs(threatSpeedMps) * durationS;
		if (!game || SuperMemory.LayoutFor(SuperScatterRoute.FixedPointAtomics) != SuperScenePlan.Layout.Sparse || !(m > displayMarginM))
		{
			return displayMarginM;
		}
		long budget = (long)(SolverTableBudgetBytes / Math.Max(1e-09, SuperScenePlan.BytesPerUnit(SuperScenePlan.Layout.Sparse, SuperScenePlan.Per.VBlock, SuperKernelGPU.TwoFieldContact)));
		vblocks = VB(m);
		if (vblocks <= budget)
		{
			return m;
		}
		float hi = m;
		float lo = displayMarginM;
		for (int i = 0; i < 40; i++)
		{
			float mid = (hi + lo) * 0.5f;
			if (VB(mid) > budget)
			{
				hi = mid;
			}
			else
			{
				lo = mid;
			}
		}
		vblocks = VB(lo);
		return lo;

		long VB(float margin)
		{
			float dm = margin - displayMarginM;
			Vector3 loBox = new Vector3(displayLo.x - dm, displayLo.y - dm, displayLo.z - dm);
			Vector3 hiBox = new Vector3(displayHi.x + dm, displayHi.y + dm, displayHi.z + dm);
			SuperGrid.Dims(loBox, hiBox, h, out int nx, out int ny, out int nz);
			return (long)SuperScenePlan.VBlocksAxis(nz) * SuperScenePlan.VBlocksAxis(ny) * SuperScenePlan.VBlocksAxis(nx);
		}
	}

	public static long GridBytesPerNodeFor(bool contact)
	{
		return (long)Math.Ceiling(SuperScenePlan.BytesPerUnit(SuperScenePlan.Layout.DenseA, SuperScenePlan.Per.Node, contact));
	}

	private static Vector3 BodyCentre(SuperParticles p, SuperBaker.Body b)
	{
		KahanSum sx = default(KahanSum);
		KahanSum sy = default(KahanSum);
		KahanSum sz = default(KahanSum);
		KahanSum sw = default(KahanSum);
		for (int i = b.start; i < b.start + b.count; i++)
		{
			float m = p.mass[i];
			Vector3 x = p.x[i];
			sx.Add(m * x.x);
			sy.Add(m * x.y);
			sz.Add(m * x.z);
			sw.Add(m);
		}
		if (!(sw.Value <= 1e-30))
		{
			return new Vector3((float)(sx.Value / sw.Value), (float)(sy.Value / sw.Value), (float)(sz.Value / sw.Value));
		}
		return Vector3.zero;
	}

	private static Vector3 BodyCentre(Vector3[] pos, SuperParticles p, SuperBaker.Body b)
	{
		KahanSum sx = default(KahanSum);
		KahanSum sy = default(KahanSum);
		KahanSum sz = default(KahanSum);
		KahanSum sw = default(KahanSum);
		for (int i = b.start; i < b.start + b.count; i++)
		{
			float m = p.mass[i];
			Vector3 x = pos[i];
			sx.Add(m * x.x);
			sy.Add(m * x.y);
			sz.Add(m * x.z);
			sw.Add(m);
		}
		if (!(sw.Value <= 1e-30))
		{
			return new Vector3((float)(sx.Value / sw.Value), (float)(sy.Value / sw.Value), (float)(sz.Value / sw.Value));
		}
		return Vector3.zero;
	}

	private static bool GridBudgetOk(Vector3 lo, Vector3 hi, float h, long particles, out string why)
	{
		return GridBudgetOk(lo, hi, h, particles, SuperScatterRoute.FixedPointAtomics, null, out why, out long want, out long allowed, out bool intOverflow, out string bigBuffer, out long bigBytes);
	}

	private static bool GridBudgetOk(Vector3 lo, Vector3 hi, float h, long particles, SuperScatterRoute route, SuperParticles p, out string why, out long want, out long allowed, out bool intOverflow, out string bigBuffer, out long bigBytes)
	{
		why = null;
		want = 0L;
		allowed = 0L;
		intOverflow = false;
		bigBuffer = null;
		bigBytes = 0L;
		SuperGrid.Dims(lo, hi, h, out int nx, out int ny, out int nz);
		long nodes = SuperGrid.NodeCountFor(lo, hi, h);
		SuperScenePlan.Layout layout = SuperMemory.LayoutFor(route);
		int gridInts = SuperKernelGPU.GridIntsPerNodeFor(SuperKernelGPU.TwoFieldContact);
		long vblocks = 0L;
		long poolBlocks = 0L;
		SuperScenePlan.Sizes sizes = default(SuperScenePlan.Sizes);
		if (layout == SuperScenePlan.Layout.Sparse)
		{
			if (p == null)
			{
				vblocks = (long)SuperScenePlan.VBlocksAxis(nz) * SuperScenePlan.VBlocksAxis(ny) * SuperScenePlan.VBlocksAxis(nx);
				poolBlocks = vblocks;
			}
			else
			{
				poolBlocks = SuperKernelGPU.SparseCapacityFor(p, lo, hi, h, out vblocks, out long touched);
			}
			sizes = SuperScenePlan.Sizes.Sparse(particles, poolBlocks, vblocks, SuperKernelGPU.TwoFieldContact);
			if (vblocks > int.MaxValue || (long)gridInts * poolBlocks << 9 > int.MaxValue)
			{
				intOverflow = true;
				why = string.Format("разреженная сетка {0}×{1}×{2}: {3} виртуальных блоков 8³ (предел int) и пул {4} блоков × ", nx, ny, nz, vblocks, poolBlocks) + string.Format("{0}×{1} int (предел int у GridI) — счётчик буфера переполняет int. ", 512, gridInts) + "Уменьшай габарит или огрубляй шаг.";
				return false;
			}
		}
		else
		{
			sizes = new SuperScenePlan.Sizes(particles, nodes, SuperKernelGPU.TwoFieldContact);
		}
		SuperScenePlan.GpuBytes(layout, in sizes, out long gridBytes, out long particleBytes, out long fixedBytes);
		want = fixedBytes + particleBytes + gridBytes;
		long deviceBytes = SuperMemory.DeviceBytes;
		allowed = SuperMemory.AllowedBytes;
		if (layout != SuperScenePlan.Layout.Sparse && (long)gridInts * nodes > int.MaxValue)
		{
			intOverflow = true;
			why = string.Format("сетка {0}×{1}×{2} = {3:F1} млн узлов: счётчик буфера сетки ", nx, ny, nz, (double)nodes / 1000000.0) + string.Format("({0} × {1} элемента) переполняет int. Уменьшай габарит или огрубляй шаг.", nodes, gridInts);
			return false;
		}
		if (deviceBytes > 0 && want > allowed)
		{
			why = string.Format("сцена просит {0:F0} МиБ видеопамяти при ", (double)want / 1048576.0) + string.Format("{0:F0} МиБ на устройстве (допущено {1:P0}, ASSUMED): ", (double)deviceBytes / 1048576.0, VramShareCeiling) + ((layout == SuperScenePlan.Layout.Sparse) ? string.Format("разреженная сетка {0}×{1}×{2} (пул {3} блоков 8³, таблицы {4} блоков) даёт ", nx, ny, nz, poolBlocks, vblocks) : string.Format("сетка {0}×{1}×{2} = {3:F1} млн узлов даёт ", nx, ny, nz, (double)nodes / 1000000.0)) + string.Format("{0:F0} МиБ, частиц {1:F1} млн ", (double)want / 1048576.0, (double)particles / 1000000.0) + string.Format("даёт {0:F0} МиБ (раскладка {1}). ⚠️ Это отказ ПО ПАМЯТИ, а не по числу ", (double)want / 1048576.0, layout) + "ячеек: предел числа ячеек снят двумерным диспетчером 01.09. Огрубляй шаг частиц (вчетверо меньше и частиц, и узлов на каждое удвоение) или уменьшай сцену. ⚠️ Отказать здесь ОБЯЗАТЕЛЬНО: без этого D3D молча не создаёт часть буферов, и решатель возвращает правдоподобные числа, посчитанные ни на чём.";
			return false;
		}
		bigBuffer = SuperScenePlan.LargestBuffer(layout, in sizes, out bigBytes);
		long cap = SuperMemory.PerResourceCapBytes;
		if (deviceBytes > 0 && bigBytes > cap)
		{
			why = string.Format("буфер {0} просит {1:F0} МиБ ОДНИМ ресурсом при потолке одного ", bigBuffer, (double)bigBytes / 1048576.0) + string.Format("ресурса {0:F0} МиБ на этом устройстве (min(max(128 МиБ, ¼·{1:F0} МиБ), ", (double)cap / 1048576.0, (double)deviceBytes / 1048576.0) + "2048 МиБ) — предел D3D11, [MS-Limits]). Сумма сцены при этом влезает: " + string.Format("{0:F0} из допущенных {1:F0} МиБ. Огрубляй шаг или уменьшай сцену.", (double)want / 1048576.0, (double)allowed / 1048576.0);
			return false;
		}
		bigBuffer = null;
		bigBytes = 0L;
		return true;
	}

	public static Result Run(ComputeShader cs, SuperRodSpec rod, SuperPlateSpec plate, float spacing, float durationS, SuperScatterRoute route = SuperScatterRoute.FixedPointAtomics, int ensembleMember = 0, Action<KeyframeContext> keyframe = null, float keyframeEveryS = 0f)
	{
		return Run(cs, rod, new SuperPlateSpec[1] { plate }, spacing, durationS, route, ensembleMember, keyframe, keyframeEveryS);
	}

	public static Result Run(ComputeShader cs, SuperRodSpec rod, IList<SuperPlateSpec> plates, float spacing, float durationS, SuperScatterRoute route = SuperScatterRoute.FixedPointAtomics, int ensembleMember = 0, Action<KeyframeContext> keyframe = null, float keyframeEveryS = 0f, SuperEraDrive drive = null, Action<FinalState> onFinal = null, Resume resume = null)
	{
		return Run(cs, rod, default(SuperShellSpec), default(SuperJetSpec), Proj.Rod, plates, spacing, durationS, route, ensembleMember, keyframe, keyframeEveryS, drive, onFinal, resume);
	}

	public static Result Run(ComputeShader cs, SuperShellSpec shell, IList<SuperPlateSpec> plates, float spacing, float durationS, SuperScatterRoute route = SuperScatterRoute.FixedPointAtomics, int ensembleMember = 0, Action<KeyframeContext> keyframe = null, float keyframeEveryS = 0f, SuperEraDrive drive = null, Action<FinalState> onFinal = null)
	{
		return Run(cs, default(SuperRodSpec), shell, default(SuperJetSpec), Proj.Shell, plates, spacing, durationS, route, ensembleMember, keyframe, keyframeEveryS, drive, onFinal, null);
	}

	public static Result Run(ComputeShader cs, SuperJetSpec jet, IList<SuperPlateSpec> plates, float spacing, float durationS, SuperScatterRoute route = SuperScatterRoute.FixedPointAtomics, int ensembleMember = 0, Action<KeyframeContext> keyframe = null, float keyframeEveryS = 0f, SuperEraDrive drive = null, Action<FinalState> onFinal = null, Resume resume = null)
	{
		return Run(cs, default(SuperRodSpec), default(SuperShellSpec), jet, Proj.Jet, plates, spacing, durationS, route, ensembleMember, keyframe, keyframeEveryS, drive, onFinal, resume);
	}

	private static Result Run(ComputeShader cs, SuperRodSpec rod, SuperShellSpec shell, SuperJetSpec jet, Proj kind, IList<SuperPlateSpec> plates, float spacing, float durationS, SuperScatterRoute route, int ensembleMember, Action<KeyframeContext> keyframe, float keyframeEveryS, SuperEraDrive drive, Action<FinalState> onFinal, Resume resume)
	{
		RunBox box = new RunBox();
		box.abortFrame = -1;
		box.abortedAtStep = -1;
		IEnumerator it = RunCore(cs, rod, shell, jet, kind, plates, spacing, durationS, route, ensembleMember, keyframe, keyframeEveryS, drive, onFinal, resume, box, null, null, null);
		while (it.MoveNext())
		{
		}
		return box.r;
	}

	[IteratorStateMachine(typeof(_003CRunCore_003Ed__33))]
	internal static IEnumerator RunCore(ComputeShader cs, SuperRodSpec rod, SuperShellSpec shell, SuperJetSpec jet, Proj kind, IList<SuperPlateSpec> plates, float spacing, float durationS, SuperScatterRoute route, int ensembleMember, Action<KeyframeContext> keyframe, float keyframeEveryS, SuperEraDrive drive, Action<FinalState> onFinal, Resume resume, RunBox box, Func<bool> hookReady, SuperStepPacer pacer = null, SuperDecor obstacles = null)
	{
		_003CRunCore_003Ed__33 stateMachine = new _003CRunCore_003Ed__33(0);
		stateMachine.cs = cs;
		stateMachine.rod = rod;
		stateMachine.shell = shell;
		stateMachine.jet = jet;
		stateMachine.kind = kind;
		stateMachine.plates = plates;
		stateMachine.spacing = spacing;
		stateMachine.durationS = durationS;
		stateMachine.route = route;
		stateMachine.ensembleMember = ensembleMember;
		stateMachine.keyframe = keyframe;
		stateMachine.keyframeEveryS = keyframeEveryS;
		stateMachine.drive = drive;
		stateMachine.onFinal = onFinal;
		stateMachine.resume = resume;
		stateMachine.box = box;
		stateMachine.hookReady = hookReady;
		stateMachine.pacer = pacer;
		stateMachine.obstacles = obstacles;
		return stateMachine;
	}

	public static Quiet Quiescence(ComputeShader cs, SuperPlateSpec plate, float spacing, float durationS, bool clampInjects = false)
	{
		return default(Quiet);
	}

	public static int ProjectileParticleEnd(SuperBaker.Baked b)
	{
		if (b == null || b.bodies == null || b.projectileBodyCount < 1)
		{
			return 0;
		}
		if (b.bodies.Count < b.projectileBodyCount)
		{
			throw new InvalidOperationException(string.Format("SUPER S35: тел снаряда {0}, а тел в запекании {1}.", b.projectileBodyCount, b.bodies.Count));
		}
		int end = 0;
		for (int i = 0; i < b.projectileBodyCount; i++)
		{
			SuperBaker.Body body = b.bodies[i];
			if (body.start != end)
			{
				throw new InvalidOperationException(string.Format("SUPER S35: тело снаряда {0} («{1}») начинается с частицы {2}, а ждали {3} — ", i, body.name, body.start, end) + "тела снаряда лежат не первыми или с разрывом; диапазон поля 0 был бы неверен.");
			}
			end = body.start + body.count;
		}
		return end;
	}

	public static void Bounds(SuperBaker.Baked b, out Vector3 lo, out Vector3 hi)
	{
		lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		if (b != null && b.particles != null)
		{
			for (int i = 0; i < b.particles.count; i++)
			{
				Vector3 q = b.particles.x[i];
				lo = new Vector3(Mathf.Min(lo.x, q.x), Mathf.Min(lo.y, q.y), Mathf.Min(lo.z, q.z));
				hi = new Vector3(Mathf.Max(hi.x, q.x), Mathf.Max(hi.y, q.y), Mathf.Max(hi.z, q.z));
			}
		}
	}

	public static SuperBaker.Baked RunAndKeep(ComputeShader cs, SuperRodSpec rod, SuperPlateSpec plate, float spacing, float durationS, out Result r)
	{
		r = default(Result);
		return null;
	}

	static SuperShot()
	{
		Cfl = 0.4f;
		MarginM = 0.07f;
		GameSolverDomain = false;
		SabotageSkipSolve = false;
		SabotageSkipAbortCleanup = false;
	}
}
