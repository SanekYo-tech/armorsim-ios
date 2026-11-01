using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
			return SuperOutcome.Classify(pos, vel, cMass, cV0, cRod, cPlate, cDia, cKe0, plate0, cDmg);
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
			if (_003Cinners.TryGetValue(this, out IEnumerator value))
			{
				_003Cinners.Remove(this);
				(value as IDisposable)?.Dispose();
			}
			if (_003C_003E1__state == -3 || (uint)(_003C_003E1__state - 1) <= 7u)
			{
				_003C_003Em__Finally1();
			}
		}

		private static readonly ConditionalWeakTable<_003CRunCore_003Ed__33, IEnumerator> _003Cinners = new ConditionalWeakTable<_003CRunCore_003Ed__33, IEnumerator>();

		private bool MoveNext()
		{
			if (_003C_003E1__state == -1)
			{
				return false;
			}
			if (!_003Cinners.TryGetValue(this, out IEnumerator value))
			{
				value = _003CRunCoreBody(this);
				_003Cinners.Add(this, value);
			}
			if (value.MoveNext())
			{
				_003C_003E2__current = null;
				return true;
			}
			_003Cinners.Remove(this);
			(value as IDisposable)?.Dispose();
			_003C_003E1__state = -1;
			return false;
		}

		private static IEnumerator _003CRunCoreBody(_003CRunCore_003Ed__33 d)
		{
			Result r = default(Result);
			r.ok = true;
			long t0 = Stopwatch.GetTimestamp();
			long afterBake = t0;
			SuperBaker.Baked baked = null;
			SuperKernelGPU g = null;
			LogWatch logs = null;
			SuperCavity cavity = default(SuperCavity);
			bool isShell = d.kind == Proj.Shell;
			try
			{
				if (d.kind == Proj.Rod && d.rod.cavityFraction > 0f)
				{
					float rodSpacing = SuperBaker.ResolveSpacing(d.rod.spacingM, d.spacing, "стержень");
					if (!SuperCavity.TryResolve(in d.rod, rodSpacing, out cavity, out string failKey, out object[] failArgs, out string prose))
					{
						r.ok = false;
						r.failClass = SuperRunRecord.FailClass.Resource;
						r.failure = prose;
						r.failureKey = failKey;
						r.failureArgs = failArgs;
						d.box.r = r;
						yield break;
					}
				}
				double rodVolume = 0.0;
				if (d.kind == Proj.Rod)
				{
					rodVolume = SuperBaker.RodVolume(d.rod);
				}
				else if (d.kind == Proj.Shell)
				{
					SuperRodSpec probe = default(SuperRodSpec);
					probe.diameterM = d.shell.diameterM;
					probe.lengthM = d.shell.lengthM;
					probe.nose = d.shell.nose;
					rodVolume = SuperBaker.RodVolume(probe);
				}
				long estimate = SuperBaker.EstimateParticles(rodVolume, d.plates, d.spacing);
				if (!SuperBaker.ParticleBudgetOk(estimate, out string why))
				{
					r.ok = false;
					r.failClass = SuperRunRecord.FailClass.Resource;
					r.failure = why;
					r.failureKey = "super.fail.ram";
					r.failureArgs = new object[4]
					{
						(double)estimate / 1000000.0,
						(double)(estimate * 209) * (1.0 / 1048576.0),
						(double)SystemInfo.systemMemorySize,
						0.35f
					};
					d.box.r = r;
					yield break;
				}
				if (d.kind == Proj.Shell)
				{
					baked = SuperBaker.Bake(d.shell, d.plates, d.spacing);
				}
				else if (d.kind == Proj.Jet)
				{
					baked = SuperBaker.Bake(d.jet, d.plates, d.spacing);
				}
				else
				{
					baked = SuperBaker.Bake(d.rod, d.plates, d.spacing);
				}
				afterBake = Stopwatch.GetTimestamp();
				if (baked.cavity.present)
				{
					Debug.Log("[Super] " + baked.cavity.Describe(baked.cavityRemoved, baked.cavityMeasuredFraction));
				}
				SuperParticles p = baked.particles;
				r.particles = p.count;
				SuperBaker.Body rodBody = baked.bodies[0];
				SuperBaker.Body plateBody = baked.bodies[baked.bodies.Count - 1];
				double ke0 = 0.0;
				for (int i = rodBody.start; i < rodBody.start + rodBody.count; i++)
				{
					Vector3 v = p.v[i];
					ke0 += 0.5 * (double)p.mass[i] * ((double)v.x * v.x + (double)v.y * v.y + (double)v.z * v.z);
				}
				Vector3 rodCentre0 = BodyCentre(p, rodBody);
				Vector3[] plate0 = new Vector3[plateBody.count];
				for (int j = 0; j < plateBody.count; j++)
				{
					plate0[j] = p.x[plateBody.start + j];
				}
				float rodV;
				float rodDia;
				if (!isShell)
				{
					if (d.kind == Proj.Jet)
					{
						rodV = d.jet.vTipMps;
						rodDia = d.jet.diameterM;
					}
					else
					{
						rodV = d.rod.velocityMps;
						rodDia = d.rod.diameterM;
					}
				}
				else
				{
					rodV = d.shell.velocityMps;
					rodDia = d.shell.diameterM;
				}
				SuperEnsemble.Perturb(p, d.ensembleMember);
				float maxWave = 0f;
				foreach (SuperBaker.Body body in baked.bodies)
				{
					float wave = ContinuumMaterials.Get(body.material).LongitudinalWaveSpeed;
					if (wave > maxWave)
					{
						maxWave = wave;
					}
				}
				float h = d.spacing * 2f;
				float cfl = SuperDamage.CflFor(baked, Cfl);
				float dt = cfl * h / (Mathf.Abs(rodV) + maxWave);
				if (cfl != Cfl)
				{
					Debug.Log(string.Format("[S37] F-d: в сцене есть строка жидкости — Cfl {0:G9} → {1:G9} (LiquidCflFactor ", Cfl, cfl) + string.Format("{0:G9}), dt = {1:E4} с", SuperDamage.LiquidCflFactor, dt));
				}
				r.dtS = dt;
				r.steps = Math.Max(1, (int)Math.Ceiling(d.durationS / dt));
				int stepsToRun = (SabotageSkipSolve ? 0 : r.steps);
				ContinuumProperties plateMaterial = ContinuumMaterials.Get(plateBody.material);
				g = new SuperKernelGPU(d.cs, p, h, in plateMaterial);
				g.route = d.route;
				g.anvil = false;
				g.velocityCeiling = Mathf.Abs(rodV) * 10f;
				g.projectileParticleEnd = ProjectileParticleEnd(baked);
				SuperTapeRecorder.VelocityCeilingMps = g.velocityCeiling;
				Bounds(baked, out Vector3 lo, out Vector3 hi);
				r.displayMarginM = MarginM;
				r.solverMarginM = SolverMarginFor(GameSolverDomain, lo, hi, h, MarginM, Mathf.Abs(rodV), d.durationS, out long vblocks);
				r.solverVBlocks = vblocks;
				if (r.solverMarginM != MarginM)
				{
					float pad = r.solverMarginM - MarginM;
					lo = new Vector3(lo.x - pad, lo.y - pad, lo.z - pad);
					hi = new Vector3(hi.x + pad, hi.y + pad, hi.z + pad);
					double tableMiB = (double)vblocks * SuperScenePlan.BytesPerUnit(SuperScenePlan.Layout.Sparse, SuperScenePlan.Per.VBlock, SuperKernelGPU.TwoFieldContact) / 1048576.0;
					Debug.Log(string.Format("[S34 P3] область решателя шире ящика показа: запас {0:F0} мм против габарита ", r.solverMarginM * 1000f) + string.Format("{0:F0} мм (v·T = {1:F0} мм); виртуальных блоков {2}, ", MarginM * 1000f, d.durationS * Mathf.Abs(rodV) * 1000f, vblocks) + string.Format("таблицы ≈ {0:F1} МиБ ", tableMiB) + string.Format("при бюджете {0} МиБ (ASSUMED)", 64));
				}
				r.gridNodes = SuperGrid.NodeCountFor(lo, hi, h);
				if (!GridBudgetOk(lo, hi, h, p.count, d.route, p, out string budgetWhy, out long wantBytes, out long allowed, out bool intOverflow, out string bigBuffer, out long bigBytes))
				{
					r.ok = false;
					r.failClass = SuperRunRecord.FailClass.Resource;
					r.failure = budgetWhy;
					if (bigBuffer != null)
					{
						r.failureKey = "super.fail.buffer";
						r.failureArgs = new object[3]
						{
							bigBuffer,
							(double)bigBytes / 1048576.0,
							(double)SuperMemory.PerResourceCapBytes / 1048576.0
						};
					}
					else
					{
						r.failureKey = "super.fail.vram";
						SuperGrid.Dims(lo, hi, h, out int nx, out int ny, out int nz);
						r.failureArgs = new object[7] { nx, ny, nz, (double)r.gridNodes / 1000000.0, (double)wantBytes / 1048576.0, (double)SuperMemory.DeviceBytes / 1048576.0, (double)allowed / 1048576.0 };
					}
					d.box.r = r;
					yield break;
				}
				if (d.obstacles != null && SuperDecor.Collision)
				{
					g.SetObstacles(d.obstacles, (float)SuperClip.Band * d.spacing);
				}
				g.RebuildFixed(lo, hi);
				r.obstacleNodes = g.ObstacleNodes;
				if (g.ObstacleOn)
				{
					Debug.Log(string.Format("[Super] Преграда декора: помечено узлов {0} (сетка {1}), маска {2:F0} мс — ", g.ObstacleNodes, r.gridNodes, g.ObstacleBuildMs) + "осколки останавливаются у декора, урона декору нет.");
				}
				g.Upload(p);
				d.drive?.Prepare(baked);
				logs = ((d.resume == null) ? new LogWatch() : new LogWatch(d.resume.unityErrors, d.resume.unityFirst));
				g.VerifyUpload(p);
				int s0 = 0;
				int nextKf = 1;
				if (d.resume != null)
				{
					if (d.resume.dtS > 0f && d.resume.dtS != dt)
					{
						r.ok = false;
						r.failClass = SuperRunRecord.FailClass.SolverRefused;
						r.failure = string.Format("шаг по времени точки {0:E7} с не совпал с пересчитанным ", d.resume.dtS) + string.Format("{0:E7} с — конфигурация (CFL/шаг частиц) изменилась; ", dt) + "продолжение было бы другим прогоном под тем же именем";
						d.box.r = r;
						yield break;
					}
					g.UploadFull(d.resume.state);
					g.VerifyUploadAgainst(d.resume.state.pos);
					s0 = d.resume.step;
					nextKf = d.resume.nextKf;
					if (d.resume.drives != null)
					{
						SuperEraDrive eraDrive = d.drive;
						int k = 0;
						while (eraDrive != null && k < d.resume.drives.Length)
						{
							eraDrive.Restore(in d.resume.drives[k]);
							eraDrive = eraDrive.next;
							k++;
						}
					}
				}
				int stepsTotal = stepsToRun;
				r.uploadMs = (double)(Stopwatch.GetTimestamp() - afterBake) * 1000.0 / (double)Stopwatch.Frequency;
				r.bakeMs = (double)(afterBake - t0) * 1000.0 / (double)Stopwatch.Frequency;
				Stopwatch sw = Stopwatch.StartNew();
				Stopwatch stepSw = new Stopwatch();
				Stopwatch waitSw = new Stopwatch();
				Stopwatch yieldSw = new Stopwatch();
				Stopwatch paceSw = new Stopwatch();
				Stopwatch hookSw = new Stopwatch();
				stepSw.Start();
				Action syncQueue = null;
				if (d.keyframe != null)
				{
					if (d.keyframeEveryS <= 0f)
					{
						throw new ArgumentException("крюк кадров задан, а шаг кадров нет — молча писать каждый шаг было бы сюрпризом на гигабайты");
					}
					KeyframeContext ctx = default(KeyframeContext);
					if (d.resume == null)
					{
						stepSw.Stop();
						ctx = new KeyframeContext
						{
							kernel = g,
							baked = baked,
							step = 0,
							simTimeS = 0f,
							gridLo = lo,
							gridHi = hi,
							dtS = dt,
							nextKf = nextKf,
							stepsTotal = r.steps,
							unityErrors = logs.Errors,
							unityFirst = logs.First
						};
						hookSw.Start();
						d.keyframe(ctx);
						hookSw.Stop();
						waitSw.Start();
						while (d.hookReady != null && !d.hookReady())
						{
							yield return null;
						}
						waitSw.Stop();
						yieldSw.Start();
						yield return null;
						yieldSw.Stop();
						stepSw.Start();
					}
					if (d.pacer != null)
					{
						syncQueue = g.SyncQueue;
						d.pacer.QueueIsEmpty();
					}
					d.box.stepsTotal = r.steps;
					d.box.stepNow = s0;
					int s = s0;
					float t = (float)s0 * dt;
					while (s < stepsTotal)
					{
						if (d.box.abortRequested)
						{
							d.box.abortedAtStep = s;
							break;
						}
						g.Step(dt);
						d.box.stepNow = s + 1;
						t = (float)(s + 1) * dt;
						if (d.drive != null)
						{
							long profileT0 = (SuperKernelGPU.ProfileKernels ? g.ProfileMark() : 0L);
							d.drive.AfterStep(g, baked, t, dt);
							if (SuperKernelGPU.ProfileKernels)
							{
								g.ProfileDriveDone(profileT0);
							}
						}
						if ((float)nextKf * d.keyframeEveryS <= t + dt * 0.5f || s == stepsTotal - 1)
						{
							if (d.pacer != null)
							{
								g.SyncQueue();
								stepSw.Stop();
								paceSw.Start();
								yield return null;
								paceSw.Stop();
							}
							stepSw.Stop();
							nextKf = (int)Math.Floor((double)((t + dt * 0.5f) / d.keyframeEveryS)) + 1;
							ctx.step = s + 1;
							ctx.simTimeS = t;
							ctx.nextKf = nextKf;
							ctx.unityErrors = logs.Errors;
							ctx.unityFirst = logs.First;
							hookSw.Start();
							d.keyframe(ctx);
							hookSw.Stop();
							waitSw.Start();
							while (d.hookReady != null && !d.hookReady())
							{
								yield return null;
							}
							waitSw.Stop();
							yieldSw.Start();
							yield return null;
							yieldSw.Stop();
							d.pacer?.QueueIsEmpty();
							stepSw.Start();
						}
						if (d.pacer != null && d.pacer.AfterStep(syncQueue))
						{
							stepSw.Stop();
							paceSw.Start();
							yield return null;
							paceSw.Stop();
							stepSw.Start();
						}
						s++;
					}
				}
				else if (d.drive != null)
				{
					for (int s2 = s0; s2 < stepsTotal; s2++)
					{
						g.Step(dt);
						long profileT0 = (SuperKernelGPU.ProfileKernels ? g.ProfileMark() : 0L);
						d.drive.AfterStep(g, baked, (float)(s2 + 1) * dt, dt);
						if (SuperKernelGPU.ProfileKernels)
						{
							g.ProfileDriveDone(profileT0);
						}
					}
				}
				else
				{
					for (int s3 = s0; s3 < stepsTotal; s3++)
					{
						g.Step(dt);
					}
				}
				int effective = ((d.box.abortedAtStep >= 0) ? d.box.abortedAtStep : r.steps);
				r.stepsDone = effective;
				r.abortedEarly = d.box.abortedAtStep >= 0;
				r.abortedAtStep = d.box.abortedAtStep;
				r.simulatedTimeS = (float)effective * dt;
				r.wallClockMs = sw.Elapsed.TotalMilliseconds;
				r.keyframeHookMs = hookSw.Elapsed.TotalMilliseconds;
				r.stepLoopMs = stepSw.Elapsed.TotalMilliseconds;
				r.hookWaitMs = waitSw.Elapsed.TotalMilliseconds;
				r.yieldMs = yieldSw.Elapsed.TotalMilliseconds;
				r.paceYieldMs = paceSw.Elapsed.TotalMilliseconds;
				r.chunkDispatches = g.ChunkDispatches;
				g.RegionFractions(out double firstFraction, out double lastFraction);
				r.regionFirstFraction = firstFraction;
				r.regionLastFraction = lastFraction;
				if (g.Sparse)
				{
					r.sparse = g.ReadSparseStats();
				}
				g.RunGuard();
				SuperGpuFail fail = g.ReadFail(out int pass, out int particle);
				g.ReadWholeGuardHits(out uint jClampHits, out uint pressureCapHits);
				r.wholeJClampHits = jClampHits;
				r.wholePressureCapHits = pressureCapHits;
				r.limitCounters = g.ReadLimitCounters();
				r.limitCapK = ContinuumMaterials.Get(baked.bodies[0].material).K;
				r.steelParticles = SuperKernelGPU.CountSteelParticles(p, out int coherentSteel);
				r.coherentSteelParticles = coherentSteel;
				r.escapedFraction = (double)g.EscapedParticleSteps() / ((double)p.count * (double)Math.Max(1, effective));
				Debug.Log("    " + SuperLimits.OneLine(r.limitCounters, p.count, effective, r.limitCapK, r.steelParticles, r.coherentSteelParticles));
				if (fail != SuperGpuFail.None)
				{
					r.ok = false;
					r.failClass = SuperRunRecord.FailClass.QualityGuard;
					r.failure = string.Format("GPU отказ {0} на проходе {1}, частица {2}", fail, pass, particle);
					if (fail == SuperGpuFail.GridPool)
					{
						r.failure = r.failure + " — " + (g.SparsePoolFailure ?? "пул разреженной сетки переполнен");
						r.failClass = SuperRunRecord.FailClass.Resource;
						r.failureKey = "super.fail.gridpool";
					}
				}
				if (r.escapedFraction > 0.001)
				{
					r.ok = false;
					r.failClass = SuperRunRecord.FailClass.QualityGuard;
					r.failure = string.Format("вне сетки {0:P3} частице-шагов при потолке ", r.escapedFraction) + string.Format("{0:P3} — габарит мал для этого события", 0.001);
					r.failureKey = "super.fail.escaped";
					r.failureArgs = new object[2] { r.escapedFraction * 100.0, 0.1 };
				}
				r.degeneracyFraction = (double)jClampHits / Math.Max(1.0, (double)effective * (double)p.count);
				if (r.ok && r.degeneracyFraction > 0.01)
				{
					r.ok = false;
					r.failClass = SuperRunRecord.FailClass.QualityGuard;
					r.failure = string.Format("ЦЕЛЬНОЕ ВЕЩЕСТВО УХОДИТ В ВЫРОЖДЕНИЕ: {0:P3} частице-шагов ", r.degeneracyFraction) + string.Format("при потолке {0:P3} ({1} срабатываний). ", 0.01, jClampHits) + "⚠️ Это не отказ решателя, а приговор ДИСКРЕТИЗАЦИИ: тело, теряющее такую долю вещества на аварийный выход, ею не представлено, и числа прогона недействительны, хотя он и дошёл до конца.";
				}
				if (d.pacer != null)
				{
					yield return null;
				}
				Vector3[] pos = new Vector3[p.count];
				Vector3[] vel = new Vector3[p.count];
				g.Download(pos, vel, null);
				p.x.CopyFrom(pos);
				p.v.CopyFrom(vel);
				KahanSum totalMass = default(KahanSum);
				KahanSum escapedMass = default(KahanSum);
				for (int m = rodBody.start; m < rodBody.start + rodBody.count; m++)
				{
					totalMass.Add(p.mass[m]);
					Vector3 q = p.x[m];
					if (q.x < lo.x || q.y < lo.y || q.z < lo.z || hi.x < q.x || hi.y < q.y || hi.z < q.z)
					{
						escapedMass.Add(p.mass[m]);
					}
				}
				r.escapedMassFractionEnd = ((totalMass.Value > 1E-30) ? (escapedMass.Value / totalMass.Value) : 0.0);
				KahanSum plateDisplacement = default(KahanSum);
				for (int n = 0; n < plateBody.count; n++)
				{
					Vector3 delta = p.x[plateBody.start + n] - plate0[n];
					plateDisplacement.Add(Vector3.Dot(delta, plateBody.normal));
				}
				r.plateMeanDisplacementM = ((plateBody.count > 0) ? ((float)(plateDisplacement.Value / (double)plateBody.count)) : 0f);
				float[] dmgFinal = null;
				if (SuperDamage.Enabled)
				{
					dmgFinal = new float[p.count];
					g.DownloadDamage(dmgFinal);
				}
				if (d.pacer != null)
				{
					float[] cMass = new float[p.count];
					p.mass.CopyTo(cMass);
					float[] cV0 = new float[p.count];
					p.V0.CopyTo(cV0);
					SuperBaker.Body cRod = rodBody;
					SuperBaker.Body cPlate = plateBody;
					float cDia = rodDia;
					double cKe0 = ke0;
					float[] cDmg = dmgFinal;
					Vector3[] plateInitial = plate0;
					Task<SuperOutcomeResult> judge = Task.Run(() => SuperOutcome.Classify(pos, vel, cMass, cV0, cRod, cPlate, cDia, cKe0, plateInitial, cDmg));
					while (!judge.IsCompleted)
					{
						yield return null;
					}
					r.outcome = judge.Result;
				}
				else
				{
					r.outcome = SuperOutcome.Classify(p, rodBody, plateBody, rodDia, ke0, plate0, dmgFinal);
				}
				r.rodCentreTravelM = Vector3.Dot(BodyCentre(p, rodBody) - rodCentre0, rodBody.normal);
				if (Mathf.Abs(rodV) < 1E-06f)
				{
					throw new InvalidOperationException("SUPER: скорость снаряда ноль — сторож живости выродился бы в тождество. Сцена без движущегося снаряда через SuperShot.Run не считается.");
				}
				float oneStepTravel = Mathf.Abs(rodV) * 2f * dt;
				float livenessFloor = (float)(r.steps - s0) * dt * Mathf.Abs(rodV) * LivenessFloorFraction;
				bool moved = r.rodCentreTravelM >= oneStepTravel;
				bool dead = false;
				if (r.rodCentreTravelM <= livenessFloor && r.outcome.craterReading.status == RulerStatus.Agreed && r.escapedFraction <= 0.0)
				{
					dead = Mathf.Abs(r.plateMeanDisplacementM) <= 0f;
				}
				if (!dead && moved)
				{
					if (d.onFinal != null)
					{
						d.onFinal(new FinalState
						{
							baked = baked,
							pos = pos,
							vel = vel,
							damage = dmgFinal,
							spacing = d.spacing
						});
					}
					d.box.r = r;
					yield break;
				}
				r.ok = false;
				r.failClass = SuperRunRecord.FailClass.QualityGuard;
				string[] failureParts = new string[7]
				{
					"РЕШАТЕЛЬ НИЧЕГО НЕ ПОСЧИТАЛ: центр масс снаряда прошёл вдоль выстрела ",
					string.Format("{0:F4} мм при {1} поле ", r.rodCentreTravelM * 1000f, moved ? "мягком" : "жёстком"),
					string.Format("{0:F3} мм ", (moved ? livenessFloor : oneStepTravel) * 1000f),
					string.Format("(свободный пролёт за окно — {0:F0} мм). ", r.simulatedTimeS * Mathf.Abs(rodV) * 1000f),
					"⚠️ ЭТО НЕ РЕЗУЛЬТАТ, А МЁРТВЫЙ ПРОГОН. Прочие приборы при этом показывают ",
					string.Format("безупречные числа — утечка {0:P3} — именно потому, что ничего ", r.escapedFraction),
					"не двигалось и потому ничего не могло пойти не так. Причина: счёт не состоялся (пропущенный диспетч, неудачное выделение, непривязанный буфер). ⚠️⚠️ ПРОВЕРЬ ЛОГ НА ОШИБКИ UNITY: в разборе 01.09 мёртвый прогон сопровождался 22 154 строками «Thread group count is above the maximum allowed limit», и их никто не прочитал. Числа НЕДЕЙСТВИТЕЛЬНЫ."
				};
				r.failure = string.Concat(failureParts);
				r.failureArgs = failureParts;
				d.box.r = r;
				yield break;
			}
			finally
			{
				logs?.Dispose();
				if (SabotageSkipAbortCleanup)
				{
					LastBakedForAudit = baked;
				}
				else
				{
					g?.Dispose();
					baked?.Dispose();
				}
			}
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
		Quiet result = default(Quiet);
		result.ok = true;
		SuperBaker.Baked baked = SuperBaker.BakePlates(new SuperPlateSpec[1] { plate }, spacing);
		SuperParticles particles = baked.particles;
		result.particles = particles.count;
		result.pinnedCount = particles.PinnedCount();
		Vector3[] array = new Vector3[particles.count];
		for (int i = 0; i < particles.count; i++)
		{
			array[i] = particles.x[i];
		}
		SuperBaker.Body body = baked.bodies[0];
		float num = ContinuumMaterials.Get(body.material).LongitudinalWaveSpeed;
		float num2 = 2f * spacing * Cfl / num;
		int num3 = Math.Max(1, (int)Math.Ceiling(durationS / num2));
		result.steps = num3;
		ContinuumProperties material = ContinuumMaterials.Get(body.material);
		SuperKernelGPU superKernelGPU = new SuperKernelGPU(cs, particles, spacing * 2f, in material);
		superKernelGPU.route = SuperScatterRoute.FixedPointAtomics;
		superKernelGPU.clampInjects = clampInjects;
		superKernelGPU.anvil = false;
		Bounds(baked, out var lo, out var hi);
		superKernelGPU.RebuildFixed(lo, hi);
		superKernelGPU.Upload(particles);
		for (int j = 0; j < num3; j++)
		{
			superKernelGPU.Step(num2);
		}
		superKernelGPU.RunGuard();
		SuperGpuFail superGpuFail = superKernelGPU.ReadFail(out int pass, out int particle);
		if (superGpuFail != SuperGpuFail.None)
		{
			result.ok = false;
			result.failure = string.Format("GPU отказ {0} на проходе {1}, частица {2}", superGpuFail, pass, particle);
		}
		else
		{
			Vector3[] array2 = new Vector3[particles.count];
			Vector3[] array3 = new Vector3[particles.count];
			superKernelGPU.Download(array2, array3, null);
			KahanSum kahanSum = default(KahanSum);
			KahanSum kahanSum2 = default(KahanSum);
			KahanSum kahanSum3 = default(KahanSum);
			KahanSum kahanSum4 = default(KahanSum);
			for (int k = 0; k < particles.count; k++)
			{
				Vector3 vector = array3[k];
				float num4 = (float)Math.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
				if (num4 > result.maxSpeedMps)
				{
					result.maxSpeedMps = num4;
				}
				float num5 = (array2[k] - array[k]).magnitude;
				if (num5 > result.maxDisplacementM)
				{
					result.maxDisplacementM = num5;
				}
				kahanSum.Add(array2[k].x - array[k].x);
				kahanSum2.Add(array2[k].y - array[k].y);
				kahanSum3.Add(array2[k].z - array[k].z);
				kahanSum4.Add(0.5 * (double)particles.mass[k] * (double)num4 * (double)num4);
			}
			result.centroidDriftM = new Vector3((float)(kahanSum.Value / (double)particles.count), (float)(kahanSum2.Value / (double)particles.count), (float)(kahanSum3.Value / (double)particles.count)).magnitude;
			result.keJ = kahanSum4.Value;
		}
		superKernelGPU.Dispose();
		baked.Dispose();
		return result;
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
		SuperBaker.Baked baked = SuperBaker.Bake(rod, new SuperPlateSpec[1] { plate }, spacing);
		r.particles = baked.particles.count;
		float num = 0f;
		foreach (SuperBaker.Body body2 in baked.bodies)
		{
			float longitudinalWaveSpeed = ContinuumMaterials.Get(body2.material).LongitudinalWaveSpeed;
			if (longitudinalWaveSpeed > num)
			{
				num = longitudinalWaveSpeed;
			}
		}
		float num2 = SuperDamage.CflFor(baked, Cfl) * (2f * spacing) / (num + Mathf.Abs(rod.velocityMps));
		r.dtS = num2;
		r.steps = Math.Max(1, (int)Math.Ceiling(durationS / num2));
		SuperBaker.Body body = baked.bodies[1];
		ContinuumProperties material = ContinuumMaterials.Get(body.material);
		SuperKernelGPU superKernelGPU = new SuperKernelGPU(cs, baked.particles, spacing * 2f, in material);
		superKernelGPU.route = SuperScatterRoute.FixedPointAtomics;
		superKernelGPU.anvil = false;
		superKernelGPU.projectileParticleEnd = ProjectileParticleEnd(baked);
		Bounds(baked, out var lo, out var hi);
		superKernelGPU.RebuildFixed(lo, hi);
		superKernelGPU.Upload(baked.particles);
		for (int i = 0; i < r.steps; i++)
		{
			superKernelGPU.Step(r.dtS);
		}
		superKernelGPU.RunGuard();
		SuperGpuFail superGpuFail = superKernelGPU.ReadFail(out int pass, out int particle);
		if (superGpuFail != SuperGpuFail.None)
		{
			r.ok = false;
			r.failure = string.Format("GPU отказ {0} (проход {1})", superGpuFail, pass);
		}
		Vector3[] array = new Vector3[baked.particles.count];
		Vector3[] array2 = new Vector3[baked.particles.count];
		superKernelGPU.Download(array, array2, null);
		for (int j = 0; j < baked.particles.count; j++)
		{
			baked.particles.x[j] = array[j];
			baked.particles.v[j] = array2[j];
		}
		superKernelGPU.Dispose();
		return baked;
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
