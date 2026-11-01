using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class SuperReconGpu : IDisposable
{
	public enum ReadbackMode
	{
		Unknown = 0,
		None = 1,
		Sync = 2,
		Async = 3
	}

	public sealed class ReconFailure : Exception
	{
		public readonly string Key;

		public ReconFailure(string key, string text)
			: base(key + ": " + text)
		{
			Key = key;
		}
	}

	internal struct HookInputs
	{
		public Vector3[] pos;

		public float[] dmg;

		public SuperBaker.Body[] bodies;

		public Vector3 gridLo;

		public Vector3 gridHi;

		public float spacing;

		public int frameIndex;

		public int dop;
	}

	internal struct ReconStats
	{
		public string readback;

		public int slots;

		public int slotWaits;

		public int framesGpu;

		public int vertexRequests;

		public double slotWaitS;

		public double hookMsMean;

		public double hookMsMax;

		public double hookGpuMsSum;

		public double readbackMsP95;

		public double vramMB;

		public int pipeWaits;

		public double pipeWaitS;

		public long keyMismatch;

		public long nodeMismatch;

		public long keysChecked;

		public long aabbChecks;

		public long signedZeroDiffs;

		public long filledCells;

		public long dilatedCells;

		public double reserveMB;

		public double ownMB;

		public double allowedMB;

		public double reserveMBMax;

		public double ownMBMax;
	}

	internal struct BodyRec
	{
		public Vector3 flo;

		public float cell;

		public int hnx;

		public int hny;

		public int hnz;

		public int brickBase;

		public int hbx;

		public int hby;

		public int hbz;

		public int blockBase;

		public int bnx;

		public int bny;

		public int bnz;

		public int nodeBase;

		public int start;

		public int count;

		public int accepted;

		public int flags;
	}

	internal struct NodeEntry
	{
		public uint cell;

		public float coord;
	}

	internal struct BoundsRec
	{
		public Vector3 lo;

		public Vector3 hi;

		public uint cnt;
	}

	private enum SlotState
	{
		Free = 0,
		InUse = 1
	}

	private sealed class VertexRequest
	{
		public FrameHandle handle;

		public int body;

		public Vector3[] verts;

		public float[][] result;

		public long t0;

		public long tSubmit;

		public int ring = -1;

		public bool done;

		public Exception error;

		public AsyncGPUReadbackRequest gpuReq;

		public bool gpuPending;
	}

	private sealed class PendingBlock
	{
		public FrameHandle h;

		public AsyncGPUReadbackRequest req;

		public int bt;

		public int[] bodyBase;

		public int[] bodyCells;

		public long t0;
	}

	internal sealed class ReconProfileData
	{
		public enum Slot
		{
			EmptyReadback = 0,
			Snap = 1,
			Bounds = 2,
			Hash = 3,
			BlockSample = 4,
			Dilate = 5,
			VertexFields = 6
		}

		public static readonly int Slots;

		public readonly long[] ticks;

		public readonly long[] counts;

		public double TotalComputeMs
		{
			get
			{
				double total = 0.0;
				foreach (Slot s in Enum.GetValues(typeof(Slot)))
				{
					total += (double)counts[(int)s] * MeanMs(s);
				}
				return total;
			}
		}

		static ReconProfileData()
		{
			Slots = Enum.GetValues(typeof(Slot)).Length;
		}

		public ReconProfileData()
		{
			ticks = new long[Slots];
			counts = new long[Slots];
		}

		public void Reset()
		{
			Array.Clear(ticks, 0, Slots);
			Array.Clear(counts, 0, Slots);
		}

		public void Add(Slot s, long t)
		{
			ticks[(int)s] += t;
			counts[(int)s]++;
		}

		public double RawMeanMs(Slot s)
		{
			if (counts[(int)s] <= 0)
			{
				return 0.0;
			}
			return (double)ticks[(int)s] * 1000.0 / (double)Stopwatch.Frequency / (double)counts[(int)s];
		}

		public double MeanMs(Slot s)
		{
			if (s == Slot.EmptyReadback)
			{
				return RawMeanMs(s);
			}
			return Math.Max(0.0, RawMeanMs(s) - RawMeanMs(Slot.EmptyReadback));
		}

		public long Calls(Slot s)
		{
			return counts[(int)s];
		}
	}

	public sealed class FrameHandle
	{
		internal readonly SuperReconGpu drv;

		internal readonly int slot;

		internal readonly int frame;

		internal readonly int bodies;

		internal readonly int[] accepted;

		internal readonly int[] nx;

		internal readonly int[] ny;

		internal readonly int[] nz;

		internal readonly int[] bnx;

		internal readonly int[] bny;

		internal readonly int[] bnz;

		internal readonly int[] blockBase;

		internal readonly Vector3[] gpuLo;

		internal readonly Vector3[] gpuHi;

		internal readonly Vector3[] flo;

		private readonly bool[] done;

		internal int remaining;

		internal int queued;

		internal bool closed;

		private readonly object blockSync;

		private bool blockReady;

		private float[][][] block;

		private Exception blockError;

		internal uint[] keysCopy;

		internal int occSlots;

		internal int[] extentAfterSample;

		internal uint[][] maskPass;

		internal FrameHandle(SuperReconGpu drv, int slot, int frame, int bodies, int[] accepted, Vector3[] gpuLo, Vector3[] gpuHi, Vector3[] flo, int[] nx, int[] ny, int[] nz, int[] bnx, int[] bny, int[] bnz, int[] blockBase)
		{
			blockSync = new object();
			this.drv = drv;
			this.slot = slot;
			this.frame = frame;
			this.bodies = bodies;
			this.accepted = accepted;
			this.gpuLo = gpuLo;
			this.gpuHi = gpuHi;
			this.flo = flo;
			this.nx = nx;
			this.ny = ny;
			this.nz = nz;
			this.bnx = bnx;
			this.bny = bny;
			this.bnz = bnz;
			this.blockBase = blockBase;
			done = new bool[bodies];
			for (int i = 0; i < bodies; i++)
			{
				if (accepted[i] > 0)
				{
					remaining++;
				}
			}
		}

		private void CheckBody(int b)
		{
			if (b >= 0 && b < bodies)
			{
				return;
			}
			throw new ArgumentOutOfRangeException("b", string.Format("тело {0} при {1} в кадре {2}", b, bodies, frame));
		}

		internal void CheckFilter(int b, int acceptedCpu, Vector3 lo, Vector3 hi)
		{
			CheckBody(b);
			Interlocked.Increment(ref drv.aabbChecks);
			if (acceptedCpu != accepted[b])
			{
				throw new ReconFailure(KeyAabb, string.Format("тело {0} кадра {1}: ЦП отобрал {2} частиц, GPU {3}", b, frame, acceptedCpu, accepted[b]));
			}
			if (acceptedCpu <= 0)
			{
				return;
			}
			Vector3 loGpu = gpuLo[b];
			Vector3 hiGpu = gpuHi[b];
			if (lo.x != loGpu.x || lo.y != loGpu.y || lo.z != loGpu.z || hi.x != hiGpu.x || hi.y != hiGpu.y || hi.z != hiGpu.z)
			{
				throw new ReconFailure(KeyAabb, string.Format("тело {0} кадра {1}: габарит ЦП ({2:R}, {3:R}, {4:R})…({5:R}, {6:R}, {7:R}) ≠ GPU ", b, frame, lo.x, lo.y, lo.z, hi.x, hi.y, hi.z) + string.Format("({0:R}, {1:R}, {2:R})…({3:R}, {4:R}, {5:R}) — биты {6} {7} / {8} {9}", loGpu.x, loGpu.y, loGpu.z, hiGpu.x, hiGpu.y, hiGpu.z, Hex(lo), Hex(hi), Hex(loGpu), Hex(hiGpu)));
			}
			if (Bits(lo.x) != Bits(loGpu.x) || Bits(lo.y) != Bits(loGpu.y) || Bits(lo.z) != Bits(loGpu.z) || Bits(hi.x) != Bits(hiGpu.x) || Bits(hi.y) != Bits(hiGpu.y) || Bits(hi.z) != Bits(hiGpu.z))
			{
				Interlocked.Increment(ref drv.signedZeroDiffs);
			}
		}

		private static int Bits(float f)
		{
			return BitConverter.SingleToInt32Bits(f);
		}

		private static string Hex(Vector3 v)
		{
			return string.Format("{0:X8}/{1:X8}/{2:X8}", Bits(v.x), Bits(v.y), Bits(v.z));
		}

		internal void CheckGrid(int b, Vector3 floCpu, int nxCpu, int nyCpu, int nzCpu)
		{
			CheckBody(b);
			if (floCpu.x != flo[b].x || floCpu.y != flo[b].y || floCpu.z != flo[b].z || nxCpu != nx[b] || nyCpu != ny[b] || nzCpu != nz[b])
			{
				throw new ReconFailure(KeyDims, string.Format("тело {0} кадра {1}: решётка рабочего (({2:R}, {3:R}, {4:R}), {5}×{6}×{7}) ≠ крюка ", b, frame, floCpu.x, floCpu.y, floCpu.z, nxCpu, nyCpu, nzCpu) + string.Format("(({0:R}, {1:R}, {2:R}), {3}×{4}×{5})", flo[b].x, flo[b].y, flo[b].z, nx[b], ny[b], nz[b]));
			}
		}

		internal float[][] RequestVertexFields(int b, Vector3[] verts)
		{
			if (verts == null)
			{
				throw new ArgumentNullException("verts");
			}
			CheckBody(b);
			float[][] result = new float[FC][];
			for (int f = 0; f < FC; f++)
			{
				result[f] = new float[verts.Length];
			}
			if (verts.Length != 0)
			{
				if (Thread.CurrentThread.GetHashCode() == drv.mainThreadId)
				{
					drv.ServeInline(this, b, verts, result);
				}
				else
				{
					VertexRequest req = new VertexRequest();
					req.handle = this;
					req.body = b;
					req.verts = verts;
					req.result = result;
					req.t0 = Now;
					lock (drv.queueLock)
					{
						if (closed || drv.Abandoned)
						{
							throw new ReconFailure(KeyAbandoned, string.Format("прогон отменён/оборван — ожидание полей вершин тела {0} кадра {1} снято", b, frame));
						}
						drv.queue.Enqueue(req);
						queued++;
					}
					Wait(req, () => req.done, "полей вершин", b);
					if (req.error != null)
					{
						throw req.error;
					}
				}
			}
			BodyDone(b);
			return result;
		}

		internal float[][] BlockFields(int b, int bnxCpu, int bnyCpu, int bnzCpu)
		{
			CheckBody(b);
			if (accepted[b] <= 0)
			{
				return null;
			}
			if (bnxCpu != bnx[b] || bnyCpu != bny[b] || bnzCpu != bnz[b])
			{
				throw new ReconFailure(KeyDims, string.Format("тело {0} кадра {1}: блок {2}×{3}×{4} ≠ крюка {5}×{6}×{7}", b, frame, bnxCpu, bnyCpu, bnzCpu, bnx[b], bny[b], bnz[b]));
			}
			if (Thread.CurrentThread.GetHashCode() == drv.mainThreadId)
			{
				drv.CompletePendingBlock(wait: true);
			}
			Wait(blockSync, () => blockReady || blockError != null, "блока полей", b);
			lock (blockSync)
			{
				if (blockError != null)
				{
					throw blockError;
				}
				float[][] result = block?[b];
				if (block != null)
				{
					block[b] = null;
				}
				return result;
			}
		}

		private void Wait(object o, Func<bool> ready, string what, int b)
		{
			long t0 = Now;
			lock (o)
			{
				while (!ready())
				{
					if (drv.Abandoned || drv.CancelRequested)
					{
						throw new ReconFailure(KeyAbandoned, string.Format("прогон отменён/оборван — ожидание {0} тела {1} кадра {2} снято", what, b, frame));
					}
					if (Ms(Now - t0) / 1000.0 > WaitTimeoutS)
					{
						throw new ReconFailure(KeyTimeout, string.Format("рабочий ждал {0} тела {1} кадра {2} {3:F0} с", what, b, frame, Ms(Now - t0) / 1000.0));
					}
					Monitor.Wait(o, WaitSliceMs);
				}
			}
		}

		internal void BodyDone(int b)
		{
			CheckBody(b);
			lock (drv.queueLock)
			{
				if (done[b])
				{
					return;
				}
				if (accepted[b] <= 0)
				{
					return;
				}
				done[b] = true;
				remaining--;
			}
		}

		internal void ReleaseFromWorker()
		{
			lock (drv.queueLock)
			{
				closed = true;
			}
		}

		internal void SetBlock(float[][][] perBody)
		{
			lock (blockSync)
			{
				block = perBody;
				blockReady = true;
				Monitor.PulseAll(blockSync);
			}
		}

		internal void FailBlock(Exception e)
		{
			lock (blockSync)
			{
				blockError = e;
				Monitor.PulseAll(blockSync);
			}
		}
	}

	public static bool Enabled;

	public static bool ProfileRecon;

	public const int MaxRetainedSlots = 4;

	public const int MinRetainedSlots = 2;

	public const int VertexRing = 4;

	public const int SlotCellsPerParticle = 4;

	public const float SlotShareOfBudget = 0.5f;

	public const double ProbeTimeoutMs = 2000.0;

	public const double WaitTimeoutS = 600.0;

	public const int BodyRecStride = 80;

	public const int NodeEntryStride = 8;

	public const int BoundsRecStride = 28;

	public const int SyncWords = 16;

	public const uint NoKey = 4294967295u;

	public const string KeyAabb = "super.fail.recon_aabb";

	public const string KeyDims = "super.fail.recon_dims";

	public const string KeyVram = "super.fail.recon_vram";

	public const string KeyReadback = "super.fail.recon_readback";

	public const string KeyTimeout = "super.fail.recon_timeout";

	public const string KeyAbandoned = "super.fail.recon_abandoned";

	public const string KeyHash = "super.fail.recon_hash";

	public const string KeyBind = "super.fail.recon_bind";

	public const string KeyAlloc = "super.fail.recon_alloc";

	public const string KeySabotage = "super.fail.recon_sabotage";

	private const int WaitSliceMs = 50;

	private const long MiB = 1048576L;

	private const int FC = 5;

	private const int BrickCells = 512;

	private static int _liveDrivers;

	private static int _liveSlots;

	private static int _livePending;

	private static long _liveBytes;

	public static bool SabotageSkipRelease;

	public static bool SabotageBoundsPredicate;

	public static bool SabotageBoundsNudge;

	public static bool SabotageTouchSolver;

	public static bool SabotageUavOnSolver;

	public static bool SabotageAllocExtra;

	public static bool SabotageProbeCorrupt;

	public static bool SabotageUnboundedReserve;

	public static int SabotageThrowAtFrame;

	public static int SabotageTapeMutant;

	internal static Action<SuperReconGpu, FrameHandle> TestOnHook;

	internal static SuperReconGpu LastLeaked;

	internal int MutantField;

	internal float MutantScale = 1.001f;

	internal float MutantAdd = 0.001f;

	internal bool MutantTap;

	internal bool MutantDilate;

	internal bool CaptureMask0AfterSample;

	internal bool CaptureMasksPerPass;

	internal static bool KeepKeysForTest;

	internal static readonly string[] KernelNames;

	private static readonly string[] SingleThreadKernels;
	private static ReadbackMode _probeMode;

	private static string _probeReport;

	internal static readonly ReconProfileData Profile;

	static SuperReconGpu()
	{
		Enabled = SuperDamage.ArmFromCommandLine("-s39gpu", "ВОССТАНОВЛЕНИЕ ПОЛЕЙ КАДРА НА GPU (S39: поля на вершинах + блок полей)", false, "S39", "по слову владельца после приёмки S39");
		SabotageThrowAtFrame = -1;
		KernelNames = new string[21]
		{
			"KReconProbe",
			"KReconSnap",
			"KReconBoundsP",
			"KReconBoundsLevel",
			"KReconBoundsFinal",
			"KReconSlotClear",
			"KReconSlotCount",
			"KReconScanBlocks",
			"KReconScanBlocksIP",
			"KReconScanSeq",
			"KReconScanApply",
			"KReconScatter",
			"KReconSortCells",
			"KReconKeyDiag",
			"KReconNodeDiag",
			"KReconBlockClear",
			"KReconExtentClear",
			"KReconBlockSample",
			"KReconBlockDilate",
			"KReconVertexFields",
			"KReconSabTouch"
		};
		SingleThreadKernels = new string[3] { "KReconScanSeq", "KReconBoundsFinal", "KReconSabTouch" };
		_probeMode = ReadbackMode.Unknown;
		_probeReport = "";
		Profile = new ReconProfileData();
	}

	private readonly ComputeShader cs;

	private readonly int n;

	private readonly int nb;

	private readonly int slots;

	private readonly ReadbackMode mode;

	private readonly bool paced;

	private readonly Action<long> bookMain;

	private readonly int mainThreadId;

	private SuperKernelGPU kernelRef;

	private long reserveBytes;

	private Func<bool> cancelProbe;

	private bool abandoned;

	private bool disposed;

	private bool released;

	private readonly int kProbe;

	private readonly int kSnap;

	private readonly int kBoundsP;

	private readonly int kBoundsLevel;

	private readonly int kBoundsFinal;

	private readonly int kSlotClear;

	private readonly int kSlotCount;

	private readonly int kScanBlocks;

	private readonly int kScanBlocksIP;

	private readonly int kScanSeq;

	private readonly int kScanApply;

	private readonly int kScatter;

	private readonly int kSortCells;

	private readonly int kKeyDiag;

	private readonly int kNodeDiag;

	private readonly int kBlockClear;

	private readonly int kExtentClear;

	private readonly int kBlockSample;

	private readonly int kBlockDilate;

	private readonly int kVertexFields;

	private readonly int kSabTouch;

	private readonly SlotState[] slotState;

	private readonly FrameHandle[] slotOwner;

	private readonly ComputeBuffer[] rcPos;

	private readonly ComputeBuffer[] rcPf;

	private readonly ComputeBuffer[] rcOrder;

	private readonly ComputeBuffer[] rcSlotStart;

	private readonly ComputeBuffer[] rcBrick;

	private readonly ComputeBuffer[] rcBody;

	private readonly long[] capOccSlot;

	private readonly long[] capBricksSlot;

	private ComputeBuffer rcKeys;

	private ComputeBuffer rcSlotCnt;

	private ComputeBuffer rcScanL1;

	private ComputeBuffer rcScanL2;

	private ComputeBuffer rcBoundsA;

	private ComputeBuffer rcBoundsB;

	private ComputeBuffer rcBoundsOut;

	private ComputeBuffer rcNode;

	private ComputeBuffer rcBlock;

	private ComputeBuffer rcMask0;

	private ComputeBuffer rcMask1;

	private ComputeBuffer rcExtent;

	private ComputeBuffer rcSync;

	private ComputeBuffer rcGuard;

	private ComputeBuffer rcBlockBase;

	private ComputeBuffer rcStart;

	private long capSlotCnt;

	private long capScanL1;

	private long capScanL2;

	private long capNodes;

	private long capBlock;

	private long capMask0;

	private long capMask1;

	private readonly ComputeBuffer[] rcVerts;

	private readonly ComputeBuffer[] rcVField;

	private readonly long[] capVerts;

	private readonly long[] capVField;

	private readonly VertexRequest[] ringReq;

	private readonly Dictionary<string, SuperScenePlan.Sizes> sizesUsed = new Dictionary<string, SuperScenePlan.Sizes>();

	private long ownBytes;

	private long peakOwnBytes;

	private readonly int[] gbid;

	private readonly ushort[] local;

	private readonly uint[] keys;

	private byte[] mark = new byte[0];

	private int[] brickTable = new int[0];

	private NodeEntry[] nodes = new NodeEntry[0];

	private readonly BodyRec[] bodyRecs;

	private readonly BoundsRec[] boundsBack;

	private readonly uint[] syncBack = new uint[SyncWords];

	private readonly uint[] blockBaseArr;

	private readonly uint[] startArr;

	private readonly uint[] guardBack = new uint[4];

	private int[] chunkCounts = new int[0];

	private PendingBlock pendingBlock;

	private uint[] mask0Captured;

	private bool bodiesChecked;

	private readonly object queueLock = new object();

	private readonly Queue<VertexRequest> queue = new Queue<VertexRequest>();

	private FrameHandle hookHandle;

	private bool suppressSubmit;

	private bool skipSubmitOnce;

	private int depth;

	private long hookBeginTicks;

	private long tSnapDispatch;

	private long tSync1Return;

	private long tHashFirstDispatch;

	private long curTotalAccepted;

	private long curNodes;

	private long curBricks;

	private int curOcc;

	private int curBT;

	private int curSlot;

	private bool curSkipped;

	private SuperKernelGPU.ReconInputs curInputs;

	private bool haveInputs;

	private int slotWaits;

	private int framesGpu;

	private int vertexRequests;

	private int hookCount;

	private double slotWaitS;

	private double hookGpuMsSum;

	private double reserveMBMax;

	private double ownMBMax;

	private long hookTicksSum;

	private long hookTicksMax;

	private long slotWaitStart;

	private bool slotWaitOpen;

	private bool pipeWaitOpen;

	private int pipeWaits;

	private double pipeWaitS;

	private long pipeWaitStart;

	private readonly List<double> readbackMs = new List<double>();

	private long keyMismatch;

	private long nodeMismatch;

	private long keysChecked;

	private long aabbChecks;

	private long signedZeroDiffs;

	private long filledCells;

	private long dilatedCells;

	private readonly List<string> bindReport = new List<string>();

	private bool bindReportDone;

	public static int LiveDrivers => _liveDrivers;

	public static long LiveBytes => Interlocked.Read(ref _liveBytes);

	public static int LiveSlotsInUse => _liveSlots;

	public static int LivePendingReadbacks => _livePending;

	internal static ReadbackMode LastProbeMode => _probeMode;

	internal long LastHookTicks { get; private set; }

	private static long Now => Stopwatch.GetTimestamp();

	internal long AllocatedBytesOwn => ownBytes;

	internal IReadOnlyList<string> BindReport => bindReport;

	internal int Slots => slots;

	internal ReadbackMode Mode => mode;

	internal bool Abandoned => abandoned;

	internal bool CancelRequested => cancelProbe != null && cancelProbe();

	internal ReconStats Stats
	{
		get
		{
			double p95 = -1.0;
			if (readbackMs.Count > 0)
			{
				List<double> sorted = new List<double>(readbackMs);
				sorted.Sort();
				p95 = sorted[Math.Max(0, (int)Math.Floor((double)sorted.Count * 0.95) - 1)];
			}
			ReconStats s = default(ReconStats);
			s.readback = ((mode != ReadbackMode.Sync) ? "async" : "sync");
			s.slots = slots;
			s.slotWaits = slotWaits;
			s.framesGpu = framesGpu;
			s.vertexRequests = vertexRequests;
			s.slotWaitS = slotWaitS;
			s.pipeWaits = pipeWaits;
			s.pipeWaitS = pipeWaitS;
			s.hookMsMean = ((hookCount > 0) ? (Ms(hookTicksSum) / (double)hookCount) : 0.0);
			s.hookMsMax = Ms(hookTicksMax);
			s.hookGpuMsSum = hookGpuMsSum;
			s.readbackMsP95 = p95;
			s.vramMB = (double)peakOwnBytes * 1e-06;
			s.keyMismatch = keyMismatch;
			s.nodeMismatch = nodeMismatch;
			s.keysChecked = keysChecked;
			s.aabbChecks = Interlocked.Read(ref aabbChecks);
			s.signedZeroDiffs = Interlocked.Read(ref signedZeroDiffs);
			s.filledCells = filledCells;
			s.dilatedCells = dilatedCells;
			s.reserveMB = (double)reserveBytes * 1e-06;
			s.ownMB = (double)ownBytes * 1e-06;
			s.allowedMB = (double)SuperMemory.AllowedBytes * 1e-06;
			s.reserveMBMax = reserveMBMax;
			s.ownMBMax = ownMBMax;
			return s;
		}
	}

	private static double Ms(long ticks)
	{
		return (double)ticks * 1000.0 / (double)Stopwatch.Frequency;
	}

	internal static string PlannedPathFor(SuperTapeRecorder.PipelineMode m)
	{
		if (Enabled && m == SuperTapeRecorder.PipelineMode.Parallel && SystemInfo.supportsComputeShaders)
		{
			return "gpu";
		}
		return "cpu";
	}

	internal static void AssertLayouts()
	{
		if (Marshal.SizeOf(typeof(BodyRec)) != 80)
		{
			throw new InvalidOperationException(string.Format("SuperReconGpu: BodyRec {0} Б при ожидаемых {1} — раскладка разошлась с RcBody", Marshal.SizeOf(typeof(BodyRec)), 80));
		}
		if (Marshal.SizeOf(typeof(NodeEntry)) != 8)
		{
			throw new InvalidOperationException(string.Format("SuperReconGpu: NodeEntry {0} Б при ожидаемых {1} — раскладка разошлась с RcNode", Marshal.SizeOf(typeof(NodeEntry)), 8));
		}
		if (Marshal.SizeOf(typeof(BoundsRec)) != 28)
		{
			throw new InvalidOperationException(string.Format("SuperReconGpu: BoundsRec {0} Б при ожидаемых {1} — раскладка разошлась с RcBounds", Marshal.SizeOf(typeof(BoundsRec)), 28));
		}
	}

	internal static SuperReconGpu TryStart(SuperKernelGPU kernel, int particles, int bodyCount, bool paced, Action<long> bookMainTicks, out string path, out string why)
	{
		path = "cpu";
		why = null;
		if (!Enabled)
		{
			why = "флаг -s39gpu ВЫКЛ";
			return null;
		}
		AssertLayouts();
		if (!SystemInfo.supportsComputeShaders)
		{
			why = "устройство без compute-шейдеров";
			return Cpu(why);
		}
		ComputeShader shader = LoadShader();
		if (shader == null)
		{
			why = "шейдер SuperRecon не найден (Resources, AssetDatabase)";
			return Cpu(why);
		}
		ReadbackMode probe;
		string report;
		string note = "";
		try
		{
			AssertKernels(shader);
			probe = ProbeReadback(shader, force: false, out report);
			if (probe == ReadbackMode.None)
			{
				why = "проба чтения GPU→ЦП мертва: " + report;
				return Cpu(why);
			}
			if (probe == ReadbackMode.Async && Application.isBatchMode)
			{
				probe = ReadbackMode.Sync;
				note = "; batchmode без кадров ⇒ Sync";
			}
		}
		catch (Exception ex)
		{
			why = "шейдер SuperRecon не скомпилирован: " + ex.Message;
			return Cpu(why);
		}
		long kernelBytes = KernelReserveBytes(kernel);
		long allowed = SuperMemory.AllowedBytes;
		long budget = (allowed > 0) ? (allowed - kernelBytes) : long.MaxValue;
		long slotBytes = SlotBytesEstimate(particles, bodyCount);
		int slots = MinRetainedSlots;
		if (SuperMemory.DeviceBytes > 0)
		{
			slots = (int)Math.Min(MaxRetainedSlots, Math.Max(0L, (long)((double)budget * SlotShareOfBudget)) / Math.Max(1L, slotBytes));
			if (slots < MinRetainedSlots)
			{
				why = string.Format("видеопамяти на 2 слота нет: допущено {0:F0} МиБ, решатель с запасом роста пула держит ", (double)allowed * 1e-06) + string.Format("{0:F0} МиБ, слот ≈ {1:F0} МиБ", (double)kernelBytes * 1e-06, (double)slotBytes * 1e-06);
				return Cpu(why);
			}
		}
		else
		{
			slots = MinRetainedSlots;
		}
		SuperReconGpu gpu = null;
		try
		{
			gpu = new SuperReconGpu(shader, particles, bodyCount, slots, probe, paced, bookMainTicks);
			gpu.kernelRef = kernel;
			gpu.reserveBytes = kernelBytes;
			gpu.AllocateFixed();
		}
		catch (Exception ex2)
		{
			if (gpu != null)
			{
				gpu.Dispose();
			}
			if (ex2 is ReconFailure reconFailure && (reconFailure.Key == KeyVram || reconFailure.Key == KeyAlloc))
			{
				path = "cpu";
				why = reconFailure.Message;
				return Cpu(why);
			}
			throw;
		}
		path = "gpu";
		why = string.Format("слотов {0}, чтение {1}{2}, бюджет {3:F0} МиБ, слот ≈ {4:F0} МиБ", slots, probe, note, (double)budget * 1e-06, (double)slotBytes * 1e-06);
		UnityEngine.Debug.Log("[S39 recon] путь полей кадра: GPU — " + why);
		return gpu;
	}

	private static SuperReconGpu Cpu(string why)
	{
		UnityEngine.Debug.LogWarning("[S39 recon] путь полей кадра: ЦП — " + why);
		return null;
	}

	internal static SuperReconGpu CreateForTest(ComputeShader shader, int particles, int bodyCount, int slots, ReadbackMode mode, bool paced)
	{
		AssertLayouts();
		if (shader == null)
		{
			throw new ArgumentNullException("shader");
		}
		if (mode != ReadbackMode.Sync && mode != ReadbackMode.Async)
		{
			throw new ArgumentException(string.Format("SuperReconGpu.CreateForTest: режим чтения {0} не годится (Async|Sync)", mode));
		}
		SuperReconGpu gpu = new SuperReconGpu(shader, particles, bodyCount, slots, mode, paced, null);
		gpu.AllocateFixed();
		return gpu;
	}

	internal static ComputeShader LoadShader()
	{
		return Resources.Load<ComputeShader>("SuperRecon");
	}

	internal static void AssertKernels(ComputeShader shader, string extraName = null)
	{
		if (shader == null)
		{
			throw new ArgumentNullException("shader");
		}
		List<string> names = new List<string>(KernelNames);
		if (extraName != null)
		{
			names.Add(extraName);
		}
		List<string> missing = new List<string>();
		foreach (string name in names)
		{
			if (!shader.HasKernel(name))
			{
				missing.Add(name + " (нет имени)");
				continue;
			}
			int kernelIndex = shader.FindKernel(name);
			shader.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
			uint expectX = (uint)((Array.IndexOf(SingleThreadKernels, name) >> 31 & 0xFF) + 1);
			if (x != expectX)
			{
				missing.Add(string.Format("{0} (numthreads {1}, ждали {2})", name, x, expectX));
			}
		}
		if (missing.Count > 0)
		{
			throw new InvalidOperationException(string.Format("SUPER S39: ШЕЙДЕР SuperRecon НЕ СКОМПИЛИРОВАН — непригодны {0} ядер из {1}: ", missing.Count, names.Count) + string.Join(", ", missing) + ". ⚠️ Почти всегда это синтаксическая ошибка в SuperRecon.compute; строка ошибки шейдера — выше в консоли Unity (typecheck читает только C#, HasKernel переживает провал компиляции).");
		}
	}

	internal static ReadbackMode ProbeReadback(ComputeShader shader, bool force, out string report)
	{
		if (!force && _probeMode != ReadbackMode.Unknown)
		{
			report = _probeReport;
			return _probeMode;
		}
		if (!SystemInfo.supportsComputeShaders)
		{
			report = "устройство без compute-шейдеров";
			_probeMode = ReadbackMode.None;
			_probeReport = report;
			return _probeMode;
		}
		uint[] probe = new uint[1024];
		uint h = 0u;
		for (int i = 0; i < 1024; i++)
		{
			probe[i] = h ^ 0x5A5A0000u;
			h -= 0x61C8864Fu;
		}
		if (SabotageProbeCorrupt)
		{
			probe[0] ^= 1u;
		}
		ComputeBuffer probeBuf = new ComputeBuffer(1024, 4);
		Interlocked.Add(ref _liveBytes, 4096L);
		bool supportsAsync = SystemInfo.supportsAsyncGPUReadback;
		bool asyncOk = false;
		bool timedOut = false;
		int polls = 0;
		double waitMs = 0.0;
		bool hasError = false;
		int asyncMatched = 0;
		int syncMatched = 0;
		AsyncGPUReadbackRequest req = default(AsyncGPUReadbackRequest);
		try
		{
			int kProbe = shader.FindKernel("KReconProbe");
			shader.SetBuffer(kProbe, "_RcProbe", probeBuf);
			shader.SetInt("_RcProbeSalt", 0x5A5A0000);
			SuperKernelGPU.GroupLayout(1024, out int gx, out int gy);
			shader.SetInt("_DispStrideX", gx << 8);
			shader.SetInt("_DispGroupsX", gx);
			shader.SetInt("_DispOffset", 0);
			shader.SetInt("_DispEnd", 1024);
			shader.Dispatch(kProbe, gx, gy, 1);
			if (supportsAsync)
			{
				req = AsyncGPUReadback.Request(probeBuf, 4096, 0, null);
				Interlocked.Increment(ref _livePending);
				try
				{
					long t0 = Stopwatch.GetTimestamp();
					while (!req.done)
					{
						if (ProbeTimeoutMs <= Ms(Stopwatch.GetTimestamp() - t0))
						{
							break;
						}
						req.Update();
						polls++;
						if (req.done)
						{
							break;
						}
						Thread.Sleep(1);
					}
					timedOut = !req.done;
					if (!req.done)
					{
						req.WaitForCompletion();
					}
					waitMs = Ms(Stopwatch.GetTimestamp() - t0);
					hasError = req.hasError;
				}
				finally
				{
					Interlocked.Decrement(ref _livePending);
				}
				if (!hasError)
				{
					NativeArray<uint> data = req.GetData<uint>(0);
					for (int i = 0; i < 1024; i++)
					{
						if (data[i] == probe[i])
						{
							asyncMatched++;
						}
					}
					if (!timedOut)
					{
						asyncOk = asyncMatched == 1024;
					}
				}
			}
			uint[] back = new uint[1024];
			probeBuf.GetData(back);
			syncMatched = 0;
			for (int i = 0; i < 1024; i++)
			{
				if (back[i] == probe[i])
				{
					syncMatched++;
				}
			}
		}
		finally
		{
			probeBuf.Release();
			Interlocked.Add(ref _liveBytes, -4096L);
		}
		ReadbackMode mode = asyncOk ? ReadbackMode.Async : ((syncMatched == 1024) ? ReadbackMode.Sync : ReadbackMode.None);
		string problem;
		if (!supportsAsync)
		{
			problem = "не поддержан";
		}
		else if (asyncOk)
		{
			problem = string.Format("готов за {0:F1} мс, опросов Update {1}", waitMs, polls);
		}
		else
		{
			string why;
			if (hasError)
			{
				why = "hasError";
			}
			else if (timedOut)
			{
				why = string.Format("не завершился за {0:F0} мс", ProbeTimeoutMs);
			}
			else
			{
				why = "узор разошёлся";
			}
			problem = "не годен: " + why;
		}
		report = string.Format("AsyncGPUReadback {0}, узор {1}/{2} ПОБИТОВО; GetData: узор {3}/{4} ⇒ режим {5}", problem, asyncMatched, 1024, syncMatched, 1024, mode) + (SabotageProbeCorrupt ? " (⛔ диверсия SabotageProbeCorrupt)" : "");
		UnityEngine.Debug.Log("[S39 проба] " + report);
		_probeMode = mode;
		_probeReport = report;
		return mode;
	}

	internal static long KernelReserveBytes(SuperKernelGPU k)
	{
		if (k == null)
		{
			throw new ArgumentNullException("k");
		}
		long unbounded = 0L;
		if (k.Sparse)
		{
			SuperScenePlan.Sizes worst = SuperScenePlan.Sizes.Sparse(k.SizesNow.particles, k.SizesNow.poolBlocks * 2, k.SizesNow.vblocks, k.SizesNow.contact);
			unbounded = Math.Max(0L, SuperScenePlan.GpuBytesTotal(k.Layout, worst) - SuperScenePlan.GpuBytesTotal(k.Layout, k.SizesNow));
		}
		long headroom = k.PoolHeadroomBytes();
		if (SabotageUnboundedReserve)
		{
			headroom = unbounded;
		}
		return k.VramBytes + headroom;
	}

	internal static long ReserveBytesFrom(long kernelVramBytes, long boundedHeadroomBytes, long unboundedHeadroomBytes)
	{
		return kernelVramBytes + (SabotageUnboundedReserve ? unboundedHeadroomBytes : boundedHeadroomBytes);
	}

	internal static long SlotBytesEstimate(long particles, int bodyCount)
	{
		SuperScenePlan.Sizes sz = new SuperScenePlan.Sizes(particles, 0, SuperKernelGPU.TwoFieldContact);
		sz.reconOccCells = particles * SlotCellsPerParticle;
		sz.reconBricks = 1;
		sz.reconBodies = bodyCount;
		long bytes = 0L;
		foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.ReconBuffers)
		{
			if (Array.IndexOf(SuperScenePlan.ReconSlotRows, row.name) < 0)
			{
				continue;
			}
			bytes += row.Bytes(in sz);
		}
		return bytes;
	}

	private SuperReconGpu(ComputeShader cs, int particles, int bodyCount, int slots, ReadbackMode mode, bool paced, Action<long> bookMain)
	{
		if (particles <= 0)
		{
			throw new ArgumentException(string.Format("SuperReconGpu: частиц {0}", particles));
		}
		if (bodyCount <= 0)
		{
			throw new ArgumentException(string.Format("SuperReconGpu: тел {0}", bodyCount));
		}
		if (slots < 1 || slots > MaxRetainedSlots)
		{
			throw new ArgumentException(string.Format("SuperReconGpu: слотов {0} (1…{1})", slots, MaxRetainedSlots));
		}
		this.cs = cs;
		this.mode = mode;
		this.paced = paced;
		n = particles;
		nb = bodyCount;
		this.slots = slots;
		this.bookMain = bookMain;
		mainThreadId = Thread.CurrentThread.GetHashCode();
		kProbe = cs.FindKernel("KReconProbe");
		kSnap = cs.FindKernel("KReconSnap");
		kBoundsP = cs.FindKernel("KReconBoundsP");
		kBoundsLevel = cs.FindKernel("KReconBoundsLevel");
		kBoundsFinal = cs.FindKernel("KReconBoundsFinal");
		kSlotClear = cs.FindKernel("KReconSlotClear");
		kSlotCount = cs.FindKernel("KReconSlotCount");
		kScanBlocks = cs.FindKernel("KReconScanBlocks");
		kScanBlocksIP = cs.FindKernel("KReconScanBlocksIP");
		kScanSeq = cs.FindKernel("KReconScanSeq");
		kScanApply = cs.FindKernel("KReconScanApply");
		kScatter = cs.FindKernel("KReconScatter");
		kSortCells = cs.FindKernel("KReconSortCells");
		kKeyDiag = cs.FindKernel("KReconKeyDiag");
		kNodeDiag = cs.FindKernel("KReconNodeDiag");
		kBlockClear = cs.FindKernel("KReconBlockClear");
		kExtentClear = cs.FindKernel("KReconExtentClear");
		kBlockSample = cs.FindKernel("KReconBlockSample");
		kBlockDilate = cs.FindKernel("KReconBlockDilate");
		kVertexFields = cs.FindKernel("KReconVertexFields");
		kSabTouch = cs.FindKernel("KReconSabTouch");
		slotState = new SlotState[slots];
		slotOwner = new FrameHandle[slots];
		rcPos = new ComputeBuffer[slots];
		rcPf = new ComputeBuffer[slots];
		rcOrder = new ComputeBuffer[slots];
		rcSlotStart = new ComputeBuffer[slots];
		rcBrick = new ComputeBuffer[slots];
		rcBody = new ComputeBuffer[slots];
		capOccSlot = new long[slots];
		capBricksSlot = new long[slots];
		rcVerts = new ComputeBuffer[VertexRing];
		rcVField = new ComputeBuffer[VertexRing];
		capVerts = new long[VertexRing];
		capVField = new long[VertexRing];
		ringReq = new VertexRequest[VertexRing];
		gbid = new int[particles];
		local = new ushort[particles];
		keys = new uint[particles];
		for (int i = 0; i < particles; i++)
		{
			gbid[i] = -1;
		}
		bodyRecs = new BodyRec[bodyCount];
		boundsBack = new BoundsRec[bodyCount];
		blockBaseArr = new uint[bodyCount + 1];
		startArr = new uint[bodyCount + 1];
		Interlocked.Increment(ref _liveDrivers);
	}

	private SuperScenePlan.Sizes Sz()
	{
		SuperScenePlan.Sizes sz = new SuperScenePlan.Sizes(n, 0, SuperKernelGPU.TwoFieldContact);
		sz.reconBodies = nb;
		return sz;
	}

	private void AllocateFixed()
	{
		for (int s = 0; s < slots; s++)
		{
			rcPos[s] = Make(string.Format("RcPos#{0}", s), Sz());
			rcPf[s] = Make(string.Format("RcPf#{0}", s), Sz());
			rcOrder[s] = Make(string.Format("RcOrder#{0}", s), Sz());
			rcBody[s] = Make(string.Format("RcBody#{0}", s), Sz());
		}
		rcKeys = Make("RcKeys", Sz());
		rcBoundsA = Make("RcBoundsA", Sz());
		rcBoundsB = Make("RcBoundsB", Sz());
		rcBoundsOut = Make("RcBoundsOut", Sz());
		rcExtent = Make("RcExtent", Sz());
		rcSync = Make("RcSync", Sz());
		rcGuard = Make("RcGuard", Sz());
		rcBlockBase = Make("RcBlockBase", Sz());
		rcStart = Make("RcStart", Sz());
		if (SuperAllocAudit.AfterAllocation(this) != 0)
		{
			throw new ReconFailure(KeyAlloc, "буфер восстановления: проба известными значениями не сошлась (" + SuperAllocAudit.LastAuditReport.Trim() + ")");
		}
	}

	private string WhyNotFits(string auditName, long bytes, long oldBytes)
	{
		if (SuperMemory.DeviceBytes > 0 && bytes > SuperMemory.PerResourceCapBytes)
		{
			return string.Format("буфер {0} на {1:F0} МиБ: одним ресурсом выше потолка {2:F0} МиБ", auditName, (double)bytes * 1e-06, (double)SuperMemory.PerResourceCapBytes * 1e-06);
		}
		long allowed = SuperMemory.AllowedBytes;
		if (allowed <= 0)
		{
			return null;
		}
		long budget = allowed - reserveBytes;
		long total = ownBytes - oldBytes + bytes;
		if (total <= budget)
		{
			return null;
		}
		return string.Format("буфер {0} на {1:F0} МиБ: всего восстановлению {2:F0} МиБ при допущенных ", auditName, (double)bytes * 1e-06, (double)total * 1e-06) + string.Format("{0:F0} МиБ (допущено {1:F0}, резерв ядра {2:F0})", (double)budget * 1e-06, (double)allowed * 1e-06, (double)reserveBytes * 1e-06);
	}

	private bool Fits(string auditName, in SuperScenePlan.Sizes sz, ComputeBuffer old)
	{
		SuperScenePlan.GpuBuffer row = SuperScenePlan.ReconRow(auditName);
		long count = row.Count(in sz);
		if (count < 1 || count > int.MaxValue)
		{
			return false;
		}
		long oldBytes = ((old != null) ? ((long)old.count * old.stride) : 0);
		return WhyNotFits(auditName, row.stride * count, oldBytes) == null;
	}

	private ComputeBuffer Make(string auditName, SuperScenePlan.Sizes sz)
	{
		SuperScenePlan.GpuBuffer row = SuperScenePlan.ReconRow(auditName);
		long count = row.Count(in sz);
		if (count < 1 || count > int.MaxValue)
		{
			throw new ReconFailure("super.fail.recon_vram", string.Format("буфер {0}: число элементов {1} вне (0, int.MaxValue]", auditName, count));
		}
		string why = WhyNotFits(auditName, row.stride * count, 0L);
		if (why != null)
		{
			throw new ReconFailure("super.fail.recon_vram", why);
		}
		if (SabotageAllocExtra && auditName == "RcKeys")
		{
			count++;
		}
		ComputeBuffer buffer = new ComputeBuffer((int)count, row.stride);
		long bytes = (long)row.stride * count;
		Interlocked.Add(ref _liveBytes, bytes);
		ownBytes += bytes;
		if (ownBytes > peakOwnBytes)
		{
			peakOwnBytes = ownBytes;
		}
		ownMBMax = Math.Max(ownMBMax, (double)ownBytes * 1e-06);
		if (sizesUsed != null)
		{
			sizesUsed[auditName] = sz;
		}
		if (kernelRef != null)
		{
			kernelRef.ExternalVramBytes = ownBytes;
		}
		return buffer;
	}

	private void Free(ref ComputeBuffer b, string auditName)
	{
		if (b == null)
		{
			return;
		}
		long bytes = (long)b.count * b.stride;
		b.Release();
		b = null;
		ownBytes -= bytes;
		Interlocked.Add(ref _liveBytes, -bytes);
	}

	private void EnsureCap(ref ComputeBuffer b, ref long cap, string auditName, long need, Func<long, SuperScenePlan.Sizes> sizesFor)
	{
		if (b == null || cap < need)
		{
			long want = ((cap == 0) ? need : Math.Max(need, cap * 2));
			if (!Fits(auditName, sizesFor(want), b))
			{
				want = need;
			}
			if (!Fits(auditName, sizesFor(want), b))
			{
				SuperScenePlan.GpuBuffer row = SuperScenePlan.ReconRow(auditName);
				SuperScenePlan.Sizes sz = sizesFor(want);
				long count = row.Count(in sz);
				string why;
				if (count >= 1 && count <= int.MaxValue)
				{
					long oldBytes = ((b != null) ? ((long)b.count * b.stride) : 0L);
					why = WhyNotFits(auditName, row.stride * count, oldBytes);
				}
				else
				{
					why = string.Format("буфер {0}: число элементов {1} вне (0, int.MaxValue]", auditName, count);
				}
				throw new ReconFailure(KeyVram, why ?? ("буфер " + auditName + ": не влезает"));
			}
			Free(ref b, auditName);
			b = Make(auditName, sizesFor(want));
			cap = want;
			if (SuperAllocAudit.AfterAllocation(this, auditName) != 0)
			{
				throw new ReconFailure(KeyAlloc, "буфер " + auditName + ": проба известными значениями не сошлась (" + SuperAllocAudit.LastAuditReport.Trim() + ")");
			}
		}
	}

	private ref ComputeBuffer FieldOf(string auditName)
	{
		int sharp = auditName.IndexOf('#');
		string baseName = auditName;
		int idx = -1;
		if (sharp >= 0)
		{
			baseName = auditName.Substring(0, sharp);
			idx = int.Parse(auditName.Substring(sharp + 1));
		}
		switch (baseName)
		{
		case "RcKeys":
			return ref rcKeys;
		case "RcSlotCnt":
			return ref rcSlotCnt;
		case "RcScanL1":
			return ref rcScanL1;
		case "RcScanL2":
			return ref rcScanL2;
		case "RcBoundsA":
			return ref rcBoundsA;
		case "RcBoundsB":
			return ref rcBoundsB;
		case "RcBoundsOut":
			return ref rcBoundsOut;
		case "RcNode":
			return ref rcNode;
		case "RcBlock":
			return ref rcBlock;
		case "RcMask0":
			return ref rcMask0;
		case "RcMask1":
			return ref rcMask1;
		case "RcExtent":
			return ref rcExtent;
		case "RcSync":
			return ref rcSync;
		case "RcGuard":
			return ref rcGuard;
		case "RcBlockBase":
			return ref rcBlockBase;
		case "RcStart":
			return ref rcStart;
		case "RcPos":
			return ref rcPos[idx];
		case "RcPf":
			return ref rcPf[idx];
		case "RcOrder":
			return ref rcOrder[idx];
		case "RcSlotStart":
			return ref rcSlotStart[idx];
		case "RcBrick":
			return ref rcBrick[idx];
		case "RcBody":
			return ref rcBody[idx];
		case "RcVerts":
			return ref rcVerts[idx];
		case "RcVField":
			return ref rcVField[idx];
		}
		throw new InvalidOperationException("SuperReconGpu: буфера «" + auditName + "» у водителя нет — таблица ReconBuffers и водитель разошлись");
	}

	internal List<KeyValuePair<string, ComputeBuffer>> AuditBuffers()
	{
		List<KeyValuePair<string, ComputeBuffer>> list = new List<KeyValuePair<string, ComputeBuffer>>();
		foreach (SuperScenePlan.GpuBuffer row in SuperScenePlan.ReconBuffers)
		{
			ref ComputeBuffer field = ref FieldOf(row.name);
			list.Add(new KeyValuePair<string, ComputeBuffer>(row.name, field));
		}
		return list;
	}

	internal SuperScenePlan.Sizes SizesOf(string auditName)
	{
		if (sizesUsed == null)
		{
			return default(SuperScenePlan.Sizes);
		}
		if (!sizesUsed.TryGetValue(auditName, out SuperScenePlan.Sizes sz))
		{
			throw new InvalidOperationException("SuperReconGpu: буфер «" + auditName + "» не выделялся — размеров нет");
		}
		return sz;
	}

	internal void BindCancel(Func<bool> cancelRequested)
	{
		cancelProbe = cancelRequested;
	}

	internal void RaiseThreadPoolMin(int workers)
	{
		ThreadPool.GetMinThreads(out int worker, out int completion);
		int want = workers + 2 + slots * nb;
		if (worker < want)
		{
			ThreadPool.SetMinThreads(want, completion);
		}
	}

	private void Go(int kernel, long count)
	{
		if (count <= 0)
		{
			return;
		}
		if (count > int.MaxValue)
		{
			throw new ReconFailure("super.fail.recon_vram", string.Format("диспетч на {0} элементов переполняет int", count));
		}
		SuperKernelGPU.GroupLayout((int)count, out int gx, out int gy);
		if (gx > 65535 || gy > 65535)
		{
			throw new InvalidOperationException(string.Format("SuperReconGpu: диспетч {0} элементов не помещается в раскладку {1}×{2}", count, gx, gy));
		}
		cs.SetInt("_DispStrideX", gx << 8);
		cs.SetInt("_DispGroupsX", gx);
		cs.SetInt("_DispOffset", 0);
		cs.SetInt("_DispEnd", (int)count);
		cs.Dispatch(kernel, gx, gy, 1);
	}

	private void GoOne(int kernel)
	{
		cs.Dispatch(kernel, 1, 1, 1);
	}

	private static long Groups(long c)
	{
		return Math.Max(1L, (c + 255) / 256);
	}

	private void SetCommon()
	{
		cs.SetInt("_RcN", n);
		cs.SetInt("_RcNumBodies", nb);
		cs.SetInt("_RcOcc", curOcc);
		cs.SetInt("_RcTotal", (int)curTotalAccepted);
		cs.SetInt("_RcBlockTotal", curBT);
		cs.SetInt("_RcNodes", (int)curNodes);
	}

	private bool IsSolverBuffer(ComputeBuffer b)
	{
		if (!haveInputs || b == null)
		{
			return false;
		}
		if (b == curInputs.pos || b == curInputs.vel || b == curInputs.scal || b == curInputs.s)
		{
			return true;
		}
		return b == curInputs.dmg;
	}

	private void BindRc(int kernel, string name, params SuperGpuBind.Bind[] binds)
	{
		foreach (SuperGpuBind.Bind bind in binds)
		{
			if (IsSolverBuffer(bind.buf) && bind.kind != SuperGpuBind.Kind.Srv)
			{
				throw new ReconFailure(KeyBind, string.Concat("ядро ", name, " привязывает буфер решателя «", bind.slot, "» как UAV — S39 разрешает решателю только SRV"));
			}
			if (!bindReportDone)
			{
				bindReport.Add(string.Concat(name, "|", bind.slot, "|", (bind.kind == SuperGpuBind.Kind.Uav) ? "U" : "R", "|", IsSolverBuffer(bind.buf) ? "решатель" : ""));
			}
		}
		SuperGpuBind.Checked(cs, kernel, name, binds);
	}

	private int TakeFreeSlot()
	{
		for (int s = 0; s < slots; s++)
		{
			if (slotState[s] == SlotState.Free)
			{
				slotState[s] = SlotState.InUse;
				Interlocked.Increment(ref _liveSlots);
				return s;
			}
		}
		throw new InvalidOperationException("SuperReconGpu.HookBegin без свободного слота — hookReady обязан был держать решатель");
	}

	private void ReleaseSlot(int s)
	{
		if (slotState[s] != SlotState.InUse)
		{
			return;
		}
		slotState[s] = SlotState.Free;
		slotOwner[s] = null;
		Interlocked.Decrement(ref _liveSlots);
	}

	private void ProfLap(ReconProfileData.Slot slot, ref long t)
	{
		if (!ProfileRecon)
		{
			return;
		}
		rcGuard.GetData(guardBack);
		long now = Stopwatch.GetTimestamp();
		Profile.Add(slot, now - t);
		t = now;
	}

	internal FrameHandle HookBegin(SuperKernelGPU kernel, in HookInputs inp)
	{
		long t0 = Stopwatch.GetTimestamp();
		depth++;
		int s = -1;
		try
		{
			if (disposed)
			{
				throw new InvalidOperationException("SuperReconGpu.HookBegin после Dispose");
			}
			if (hookHandle != null)
			{
				throw new InvalidOperationException("SuperReconGpu.HookBegin: предыдущий HookEnd не вызван");
			}
			if (kernel == null)
			{
				throw new ArgumentNullException("kernel");
			}
			if (SabotageThrowAtFrame >= 0 && inp.frameIndex == SabotageThrowAtFrame)
			{
				throw new ReconFailure(KeySabotage, string.Format("диверсия SabotageThrowAtFrame на кадре {0}", inp.frameIndex));
			}
			if (inp.bodies == null || inp.bodies.Length != nb)
			{
				throw new ReconFailure(KeyDims, string.Format("кадр {0}: тел {1} при {2} у водителя", inp.frameIndex, (inp.bodies == null) ? 0 : inp.bodies.Length, nb));
			}
			if (inp.pos == null || inp.pos.Length < n)
			{
				throw new ReconFailure(KeyDims, string.Format("кадр {0}: положений {1} при {2} частицах", inp.frameIndex, (inp.pos == null) ? 0 : inp.pos.Length, n));
			}
			if (inp.spacing <= 0f)
			{
				throw new ReconFailure(KeyDims, string.Format("кадр {0}: шаг {1} неположителен", inp.frameIndex, inp.spacing));
			}
			CheckBodiesOnce(inp.bodies);
			kernelRef = kernel;
			reserveBytes = KernelReserveBytes(kernel);
			reserveMBMax = Math.Max(reserveMBMax, (double)reserveBytes * 1e-06);
			CompletePendingBlock(wait: true);
			s = TakeFreeSlot();
			curSlot = s;
			framesGpu++;
			bool excludeFailed = inp.dmg != null && !SuperReconstruct.SabotageHealCracks;
			rcSync.SetData(new uint[SyncWords]);
			curInputs = kernel.ReconInputsNow;
			haveInputs = true;
			if (curInputs.particles != n)
			{
				throw new ReconFailure(KeyDims, string.Format("кадр {0}: у ядра {1} частиц, у водителя {2}", inp.frameIndex, curInputs.particles, n));
			}
			curOcc = 0;
			curTotalAccepted = 0L;
			curNodes = 0L;
			curBricks = 0L;
			curBT = 0;
			curSkipped = false;
			mask0Captured = null;
			long t = Now;
			ProfLap(ReconProfileData.Slot.EmptyReadback, ref t);
			SetCommon();
			cs.SetInt("_RcConstField", SuperReconstruct.SabotageConstantField ? 1 : 0);
			cs.SetFloat("_RcConstValue", 1.234f);
			cs.SetInt("_RcMutField", (SabotageTapeMutant > 0) ? SabotageTapeMutant : MutantField);
			cs.SetFloat("_RcMutScale", (SabotageTapeMutant > 0) ? 1.1f : MutantScale);
			cs.SetFloat("_RcMutAdd", (SabotageTapeMutant > 0) ? 0f : MutantAdd);
			SuperGpuBind.Bind[] snapBinds = new SuperGpuBind.Bind[7];
			snapBinds[0] = SuperGpuBind.Bind.U("_RcPos", rcPos[s]);
			snapBinds[1] = SuperGpuBind.Bind.U("_RcPf", rcPf[s]);
			snapBinds[2] = (SabotageUavOnSolver ? SuperGpuBind.Bind.U("_SolPosR", curInputs.pos) : SuperGpuBind.Bind.R("_SolPosR", curInputs.pos));
			snapBinds[3] = SuperGpuBind.Bind.R("_SolVelR", curInputs.vel);
			snapBinds[4] = SuperGpuBind.Bind.R("_SolScalR", curInputs.scal);
			snapBinds[5] = SuperGpuBind.Bind.R("_SolSR", curInputs.s);
			snapBinds[6] = SuperGpuBind.Bind.R("_SolDmgR", curInputs.dmg);
			BindRc(kSnap, "KReconSnap", snapBinds);
			tSnapDispatch = Now;
			Go(kSnap, n);
			if (SabotageTouchSolver)
			{
				cs.SetBuffer(kSabTouch, "_SabVel", curInputs.vel);
				GoOne(kSabTouch);
			}
			ProfLap(ReconProfileData.Slot.Snap, ref t);
			cs.SetInt("_RcExclude", excludeFailed ? 1 : 0);
			cs.SetInt("_RcSabPred", SabotageBoundsPredicate ? 1 : 0);
			cs.SetInt("_RcSabNudge", SabotageBoundsNudge ? 1 : 0);
			cs.SetVector("_RcGridLo", inp.gridLo);
			cs.SetVector("_RcGridHi", inp.gridHi);
			for (int b = 0; b < nb; b++)
			{
				if (inp.bodies[b].count <= 0)
				{
					continue;
				}
				cs.SetInt("_RcStart", inp.bodies[b].start);
				cs.SetInt("_RcCount", inp.bodies[b].count);
				SuperGpuBind.Bind[] pBinds = new SuperGpuBind.Bind[3];
				pBinds[0] = SuperGpuBind.Bind.U("_RcBoundsOut", rcBoundsA);
				pBinds[1] = SuperGpuBind.Bind.R("_RcPosR", rcPos[s]);
				pBinds[2] = SuperGpuBind.Bind.R("_SolDmgR", curInputs.dmg);
				BindRc(kBoundsP, "KReconBoundsP", pBinds);
				Go(kBoundsP, inp.bodies[b].count);
				long groups = Groups(inp.bodies[b].count);
				ComputeBuffer boundsIn = rcBoundsA;
				ComputeBuffer boundsOut = rcBoundsB;
				while (groups > 1)
				{
					SuperGpuBind.Bind[] levelBinds = new SuperGpuBind.Bind[2];
					levelBinds[0] = SuperGpuBind.Bind.U("_RcBoundsOut", boundsOut);
					levelBinds[1] = SuperGpuBind.Bind.R("_RcBoundsInR", boundsIn);
					BindRc(kBoundsLevel, "KReconBoundsLevel", levelBinds);
					cs.SetInt("_RcCount", (int)groups);
					Go(kBoundsLevel, groups);
					groups = Groups(groups);
					ComputeBuffer tmp = boundsIn;
					boundsIn = boundsOut;
					boundsOut = tmp;
				}
				SuperGpuBind.Bind[] finalBinds = new SuperGpuBind.Bind[2];
				finalBinds[0] = SuperGpuBind.Bind.U("_RcBoundsOut", rcBoundsOut);
				finalBinds[1] = SuperGpuBind.Bind.R("_RcBoundsInR", boundsIn);
				BindRc(kBoundsFinal, "KReconBoundsFinal", finalBinds);
				cs.SetInt("_RcBody", b);
				GoOne(kBoundsFinal);
			}
			rcBoundsOut.GetData(boundsBack, 0, 0, nb);
			tSync1Return = Now;
			ProfLap(ReconProfileData.Slot.Bounds, ref t);
			FrameHandle h = BuildTables(inp, excludeFailed, s);
			h.occSlots = curOcc;
			if (KeepKeysForTest)
			{
				h.keysCopy = new uint[n];
				Array.Copy(keys, h.keysCopy, n);
			}
			slotOwner[s] = h;
			if (curTotalAccepted != 0)
			{
				EnsureCap(ref rcSlotStart[s], ref capOccSlot[s], string.Format("RcSlotStart#{0}", s), curOcc, v => new SuperScenePlan.Sizes(n, 0, SuperKernelGPU.TwoFieldContact)
				{
					reconBodies = nb,
					reconOccCells = v
				});
				EnsureCap(ref rcBrick[s], ref capBricksSlot[s], string.Format("RcBrick#{0}", s), curBricks, v => new SuperScenePlan.Sizes(n, 0, SuperKernelGPU.TwoFieldContact)
				{
					reconBodies = nb,
					reconBricks = v
				});
				EnsureCap(ref rcSlotCnt, ref capSlotCnt, "RcSlotCnt", curOcc, v => new SuperScenePlan.Sizes(n, 0, SuperKernelGPU.TwoFieldContact)
				{
					reconBodies = nb,
					reconOccCells = v
				});
				EnsureCap(ref rcScanL1, ref capScanL1, "RcScanL1", curOcc, v => new SuperScenePlan.Sizes(n, 0, SuperKernelGPU.TwoFieldContact)
				{
					reconBodies = nb,
					reconOccCells = v
				});
				EnsureCap(ref rcScanL2, ref capScanL2, "RcScanL2", curOcc, v => new SuperScenePlan.Sizes(n, 0, SuperKernelGPU.TwoFieldContact)
				{
					reconBodies = nb,
					reconOccCells = v
				});
				EnsureCap(ref rcNode, ref capNodes, "RcNode", curNodes, v => new SuperScenePlan.Sizes(n, 0, SuperKernelGPU.TwoFieldContact)
				{
					reconBodies = nb,
					reconNodes = v
				});
				EnsureCap(ref rcBlock, ref capBlock, "RcBlock", curBT, v => new SuperScenePlan.Sizes(n, 0, SuperKernelGPU.TwoFieldContact)
				{
					reconBodies = nb,
					reconBlockCells = v
				});
				EnsureCap(ref rcMask0, ref capMask0, "RcMask0", curBT, v => new SuperScenePlan.Sizes(n, 0, SuperKernelGPU.TwoFieldContact)
				{
					reconBodies = nb,
					reconBlockCells = v
				});
				EnsureCap(ref rcMask1, ref capMask1, "RcMask1", curBT, v => new SuperScenePlan.Sizes(n, 0, SuperKernelGPU.TwoFieldContact)
				{
					reconBodies = nb,
					reconBlockCells = v
				});
				rcKeys.SetData(keys, 0, 0, n);
				rcBrick[s].SetData(brickTable, 0, 0, (int)curBricks);
				rcNode.SetData(nodes, 0, 0, (int)curNodes);
				rcBody[s].SetData(bodyRecs, 0, 0, nb);
				rcBlockBase.SetData(blockBaseArr);
				rcStart.SetData(startArr);
				long scanL1Count = Groups(curOcc);
				long scanL2Count = Groups(scanL1Count);
				SetCommon();
				tHashFirstDispatch = Now;
				SuperGpuBind.Bind[] clearBinds = new SuperGpuBind.Bind[1];
				clearBinds[0] = SuperGpuBind.Bind.U("_RcSlotCnt", rcSlotCnt);
				BindRc(kSlotClear, "KReconSlotClear", clearBinds);
				Go(kSlotClear, curOcc);
				SuperGpuBind.Bind[] countBinds = new SuperGpuBind.Bind[3];
				countBinds[0] = SuperGpuBind.Bind.U("_RcSlotCnt", rcSlotCnt);
				countBinds[1] = SuperGpuBind.Bind.U("_RcSync", rcSync);
				countBinds[2] = SuperGpuBind.Bind.R("_RcKeysR", rcKeys);
				BindRc(kSlotCount, "KReconSlotCount", countBinds);
				Go(kSlotCount, n);
				cs.SetInt("_ScanN", curOcc);
				SuperGpuBind.Bind[] scanBlocksBinds = new SuperGpuBind.Bind[3];
				scanBlocksBinds[0] = SuperGpuBind.Bind.U("_ScanOut", rcSlotStart[s]);
				scanBlocksBinds[1] = SuperGpuBind.Bind.U("_ScanSums", rcScanL1);
				scanBlocksBinds[2] = SuperGpuBind.Bind.R("_ScanInR", rcSlotCnt);
				BindRc(kScanBlocks, "KReconScanBlocks", scanBlocksBinds);
				Go(kScanBlocks, curOcc);
				cs.SetInt("_ScanN", (int)scanL1Count);
				SuperGpuBind.Bind[] scanIpBinds = new SuperGpuBind.Bind[2];
				scanIpBinds[0] = SuperGpuBind.Bind.U("_ScanIO", rcScanL1);
				scanIpBinds[1] = SuperGpuBind.Bind.U("_ScanSums", rcScanL2);
				BindRc(kScanBlocksIP, "KReconScanBlocksIP", scanIpBinds);
				Go(kScanBlocksIP, scanL1Count);
				cs.SetInt("_ScanN", (int)scanL2Count);
				cs.SetInt("_ScanTotalAt", curOcc);
				SuperGpuBind.Bind[] seqBinds = new SuperGpuBind.Bind[3];
				seqBinds[0] = SuperGpuBind.Bind.U("_ScanIO", rcScanL2);
				seqBinds[1] = SuperGpuBind.Bind.U("_ScanTotalOut", rcSlotStart[s]);
				seqBinds[2] = SuperGpuBind.Bind.U("_RcSync", rcSync);
				BindRc(kScanSeq, "KReconScanSeq", seqBinds);
				GoOne(kScanSeq);
				cs.SetInt("_ScanN", (int)scanL1Count);
				SuperGpuBind.Bind[] apply1Binds = new SuperGpuBind.Bind[2];
				apply1Binds[0] = SuperGpuBind.Bind.U("_ScanIO", rcScanL1);
				apply1Binds[1] = SuperGpuBind.Bind.R("_ScanAddR", rcScanL2);
				BindRc(kScanApply, "KReconScanApply", apply1Binds);
				Go(kScanApply, scanL1Count);
				cs.SetInt("_ScanN", curOcc);
				SuperGpuBind.Bind[] apply2Binds = new SuperGpuBind.Bind[2];
				apply2Binds[0] = SuperGpuBind.Bind.U("_ScanIO", rcSlotStart[s]);
				apply2Binds[1] = SuperGpuBind.Bind.R("_ScanAddR", rcScanL1);
				BindRc(kScanApply, "KReconScanApply", apply2Binds);
				Go(kScanApply, curOcc);
				SuperGpuBind.Bind[] scatterBinds = new SuperGpuBind.Bind[5];
				scatterBinds[0] = SuperGpuBind.Bind.U("_RcSlotCnt", rcSlotCnt);
				scatterBinds[1] = SuperGpuBind.Bind.U("_RcOrder", rcOrder[s]);
				scatterBinds[2] = SuperGpuBind.Bind.U("_RcSync", rcSync);
				scatterBinds[3] = SuperGpuBind.Bind.R("_RcKeysR", rcKeys);
				scatterBinds[4] = SuperGpuBind.Bind.R("_RcSlotStartR", rcSlotStart[s]);
				BindRc(kScatter, "KReconScatter", scatterBinds);
				Go(kScatter, n);
				cs.SetInt("_RcSortDesc", SuperReconstruct.SabotageHashCellOrder ? 1 : 0);
				SuperGpuBind.Bind[] sortBinds = new SuperGpuBind.Bind[2];
				sortBinds[0] = SuperGpuBind.Bind.U("_RcOrder", rcOrder[s]);
				sortBinds[1] = SuperGpuBind.Bind.R("_RcSlotStartR", rcSlotStart[s]);
				BindRc(kSortCells, "KReconSortCells", sortBinds);
				Go(kSortCells, curOcc);
				SuperGpuBind.Bind[] keyDiagBinds = new SuperGpuBind.Bind[6];
				keyDiagBinds[0] = SuperGpuBind.Bind.U("_RcSync", rcSync);
				keyDiagBinds[1] = SuperGpuBind.Bind.R("_RcKeysR", rcKeys);
				keyDiagBinds[2] = SuperGpuBind.Bind.R("_RcPosR", rcPos[s]);
				keyDiagBinds[3] = SuperGpuBind.Bind.R("_RcBrickR", rcBrick[s]);
				keyDiagBinds[4] = SuperGpuBind.Bind.R("_RcBodyR", rcBody[s]);
				keyDiagBinds[5] = SuperGpuBind.Bind.R("_RcStartR", rcStart);
				BindRc(kKeyDiag, "KReconKeyDiag", keyDiagBinds);
				Go(kKeyDiag, n);
				SuperGpuBind.Bind[] nodeDiagBinds = new SuperGpuBind.Bind[3];
				nodeDiagBinds[0] = SuperGpuBind.Bind.U("_RcSync", rcSync);
				nodeDiagBinds[1] = SuperGpuBind.Bind.R("_RcNodeR", rcNode);
				nodeDiagBinds[2] = SuperGpuBind.Bind.R("_RcBodyR", rcBody[s]);
				BindRc(kNodeDiag, "KReconNodeDiag", nodeDiagBinds);
				Go(kNodeDiag, curNodes);
				ProfLap(ReconProfileData.Slot.Hash, ref t);
				SetCommon();
				SuperGpuBind.Bind[] blockClearBinds = new SuperGpuBind.Bind[3];
				blockClearBinds[0] = SuperGpuBind.Bind.U("_RcBlock", rcBlock);
				blockClearBinds[1] = SuperGpuBind.Bind.U("_RcMaskOut", rcMask0);
				blockClearBinds[2] = SuperGpuBind.Bind.U("_RcMaskOut1", rcMask1);
				BindRc(kBlockClear, "KReconBlockClear", blockClearBinds);
				Go(kBlockClear, curBT);
				SuperGpuBind.Bind[] extentClearBinds = new SuperGpuBind.Bind[1];
				extentClearBinds[0] = SuperGpuBind.Bind.U("_RcExtent", rcExtent);
				BindRc(kExtentClear, "KReconExtentClear", extentClearBinds);
				Go(kExtentClear, (long)nb * 6);
				cs.SetInt("_RcMutTap", MutantTap ? 1 : 0);
				SuperGpuBind.Bind[] sampleBinds = new SuperGpuBind.Bind[12];
				sampleBinds[0] = SuperGpuBind.Bind.U("_RcBlock", rcBlock);
				sampleBinds[1] = SuperGpuBind.Bind.U("_RcMaskOut", rcMask0);
				sampleBinds[2] = SuperGpuBind.Bind.U("_RcExtent", rcExtent);
				sampleBinds[3] = SuperGpuBind.Bind.U("_RcSync", rcSync);
				sampleBinds[4] = SuperGpuBind.Bind.R("_RcBodyR", rcBody[s]);
				sampleBinds[5] = SuperGpuBind.Bind.R("_RcNodeR", rcNode);
				sampleBinds[6] = SuperGpuBind.Bind.R("_RcBrickR", rcBrick[s]);
				sampleBinds[7] = SuperGpuBind.Bind.R("_RcSlotStartR", rcSlotStart[s]);
				sampleBinds[8] = SuperGpuBind.Bind.R("_RcOrderR", rcOrder[s]);
				sampleBinds[9] = SuperGpuBind.Bind.R("_RcPosR", rcPos[s]);
				sampleBinds[10] = SuperGpuBind.Bind.R("_RcPfR", rcPf[s]);
				sampleBinds[11] = SuperGpuBind.Bind.R("_RcBlockBaseR", rcBlockBase);
				BindRc(kBlockSample, "KReconBlockSample", sampleBinds);
				Go(kBlockSample, curBT);
				ProfLap(ReconProfileData.Slot.BlockSample, ref t);
				if (CaptureMask0AfterSample)
				{
					mask0Captured = new uint[curBT];
					rcMask0.GetData(mask0Captured, 0, 0, curBT);
				}
				if (CaptureMasksPerPass)
				{
					h.extentAfterSample = new int[nb * 6];
					rcExtent.GetData(h.extentAfterSample, 0, 0, nb * 6);
					h.maskPass = new uint[3][];
				}
				cs.SetInt("_RcDilateSpanSab", SuperReconstruct.SabotageBlockDilateSpan ? 1 : 0);
				cs.SetInt("_RcWholeWindow", SuperReconstruct.BlockSampleOnlyFilled ? 0 : 1);
				for (int pass = 0; pass < 3; pass++)
				{
					if (pass == 2 && MutantDilate)
					{
						continue;
					}
					ComputeBuffer maskPrev = (pass == 1) ? rcMask0 : rcMask1;
					ComputeBuffer maskOut = (pass == 1) ? rcMask1 : rcMask0;
					cs.SetInt("_RcPass", pass);
					SuperGpuBind.Bind[] dilateBinds = new SuperGpuBind.Bind[7];
					dilateBinds[0] = SuperGpuBind.Bind.U("_RcBlock", rcBlock);
					dilateBinds[1] = SuperGpuBind.Bind.U("_RcMaskOut", maskOut);
					dilateBinds[2] = SuperGpuBind.Bind.U("_RcSync", rcSync);
					dilateBinds[3] = SuperGpuBind.Bind.R("_RcMaskPrevR", maskPrev);
					dilateBinds[4] = SuperGpuBind.Bind.R("_RcExtentR", rcExtent);
					dilateBinds[5] = SuperGpuBind.Bind.R("_RcBodyR", rcBody[s]);
					dilateBinds[6] = SuperGpuBind.Bind.R("_RcBlockBaseR", rcBlockBase);
					BindRc(kBlockDilate, "KReconBlockDilate", dilateBinds);
					Go(kBlockDilate, curBT);
					if (CaptureMasksPerPass)
					{
						h.maskPass[pass] = new uint[curBT];
						maskOut.GetData(h.maskPass[pass], 0, 0, curBT);
					}
				}
				ProfLap(ReconProfileData.Slot.Dilate, ref t);
				bindReportDone = true;
			}
			else
			{
				curSkipped = true;
			}
			hookHandle = h;
			suppressSubmit = true;
			return h;
		}
		catch
		{
			if (s >= 0)
			{
				ReleaseSlot(s);
			}
			throw;
		}
		finally
		{
			depth--;
			hookBeginTicks = Stopwatch.GetTimestamp() - t0;
			if (depth == 0 && bookMain != null)
			{
				bookMain(hookBeginTicks);
			}
		}
	}

	private void CheckBodiesOnce(SuperBaker.Body[] bodies)
	{
		if (bodiesChecked)
		{
			return;
		}
		int expected = 0;
		for (int i = 0; i < nb; i++)
		{
			if (bodies[i].count < 0 || bodies[i].start < expected)
			{
				throw new ReconFailure(KeyDims, string.Format("тело {0} «{1}»: диапазон [{2}, +{3}) не идёт по возрастанию за концом предыдущего {4}", i, bodies[i].name, bodies[i].start, bodies[i].count, expected));
			}
			expected = bodies[i].start + bodies[i].count;
		}
		if (n < expected)
		{
			throw new ReconFailure(KeyDims, string.Format("тела кончаются на {0} при {1} частицах", expected, n));
		}
		bodiesChecked = true;
	}

	private FrameHandle BuildTables(in HookInputs inp, bool excludeFailed, int s)
	{
		int dop = Math.Max(1, inp.dop);
		int[] accepted = new int[nb];
		Vector3[] gpuLo = new Vector3[nb];
		Vector3[] gpuHi = new Vector3[nb];
		Vector3[] flo = new Vector3[nb];
		int[] nx = new int[nb];
		int[] ny = new int[nb];
		int[] nz = new int[nb];
		int[] bnx = new int[nb];
		int[] bny = new int[nb];
		int[] bnz = new int[nb];
		int[] blockBase = new int[nb];
		float cell = inp.spacing;
		long bricksTotal = 0L;
		long blocksTotal = 0L;
		long nodesTotal = 0L;
		long acceptedTotal = 0L;
		for (int b = 0; b < nb; b++)
		{
			int start = inp.bodies[b].start;
			int count = inp.bodies[b].count;
			startArr[b] = (uint)start;
			uint cnt = (count < 1) ? 0u : boundsBack[b].cnt;
			blockBase[b] = (int)bricksTotal;
			blockBaseArr[b] = (uint)bricksTotal;
			if (cnt == 0)
			{
				BodyRec rec = default(BodyRec);
				rec.start = start;
				rec.count = count;
				bodyRecs[b] = rec;
				accepted[b] = 0;
				continue;
			}
			SuperReconMath.DensityGrid(boundsBack[b].lo, boundsBack[b].hi, cell, out Vector3 floB, out int wnx, out int wny, out int wnz);
			SuperReconMath.HashDims(floB, boundsBack[b].hi, cell, out int hnx, out int hny, out int hnz);
			SuperReconMath.BlockDims(new Vector3(wnx * cell, wny * cell, wnz * cell), out int bbnx, out int bbny, out int bbnz);
			int hbx = (hnx + 7) >> 3;
			int hby = (hny + 7) >> 3;
			int hbz = (hnz + 7) >> 3;
			long bricks = (long)hbz * hby * hbx;
			if (bricks > 0x3FFFFFFF)
			{
				throw new ArgumentException(string.Format("хэш частиц: габарит облака {0:F1}×{1:F1}×{2:F1} м ", boundsBack[b].hi.x - floB.x, boundsBack[b].hi.y - floB.y, boundsBack[b].hi.z - floB.z) + string.Format("при шаге {0:F2} мм даёт решётку {1}×{2}×{3} и {4:F1} млрд кирпичей ", cell * 1000f, hnx, hny, hnz, (double)bricks / 1e9) + string.Format("на {0} частиц — столько не бывает: облако растянуто улетевшими частицами", cnt));
			}
			if (bricksTotal + bricks > int.MaxValue)
			{
				throw new ReconFailure(KeyVram, string.Format("кадр {0}: кирпичей хэша по телам {1} переполняет int", inp.frameIndex, bricksTotal + bricks));
			}
			long blocks = (long)bbnx * bbny * bbnz;
			if (blocksTotal + blocks > int.MaxValue)
			{
				throw new ReconFailure(KeyVram, string.Format("кадр {0}: ячеек блока по телам {1} переполняет int", inp.frameIndex, blocksTotal + blocks));
			}
			BodyRec rec2 = default(BodyRec);
			rec2.flo = floB;
			rec2.cell = cell;
			rec2.hnx = hnx;
			rec2.hny = hny;
			rec2.hnz = hnz;
			rec2.brickBase = (int)bricksTotal;
			rec2.hbx = hbx;
			rec2.hby = hby;
			rec2.hbz = hbz;
			rec2.blockBase = (int)blocksTotal;
			rec2.bnx = bbnx;
			rec2.bny = bbny;
			rec2.bnz = bbnz;
			rec2.nodeBase = (int)nodesTotal;
			rec2.start = start;
			rec2.count = count;
			rec2.accepted = (int)cnt;
			rec2.flags = 1;
			bodyRecs[b] = rec2;
			gpuLo[b] = boundsBack[b].lo;
			gpuHi[b] = boundsBack[b].hi;
			flo[b] = floB;
			nx[b] = wnx;
			ny[b] = wny;
			nz[b] = wnz;
			bnx[b] = bbnx;
			bny[b] = bbny;
			bnz[b] = bbnz;
			bricksTotal += bricks;
			blocksTotal += blocks;
			nodesTotal += bbnx + bbny + bbnz;
			acceptedTotal += cnt;
			accepted[b] = (int)cnt;
		}
		blockBaseArr[nb] = (uint)bricksTotal;
		startArr[nb] = (nb <= 0) ? 0u : ((uint)(inp.bodies[nb - 1].start + inp.bodies[nb - 1].count));
		for (int i = 1; i <= nb; i++)
		{
			if (blockBaseArr[i] < blockBaseArr[i - 1] || startArr[i] < startArr[i - 1])
			{
				throw new ReconFailure(KeyDims, string.Format("кадр {0}: начала тел не монотонны у тела {1} — двоичный поиск GPU не годится", inp.frameIndex, i));
			}
		}
		curBricks = bricksTotal;
		curBT = (int)blocksTotal;
		curNodes = nodesTotal;
		curTotalAccepted = acceptedTotal;
		if (mark.Length < curBricks)
		{
			long want = Math.Max(curBricks, (long)mark.Length * 2);
			mark = new byte[want];
			brickTable = new int[want];
		}
		if (nodes.Length < curNodes)
		{
			nodes = new NodeEntry[Math.Max(curNodes, (long)nodes.Length * 2)];
		}
		Vector3[] pos = inp.pos;
		float[] dmg = inp.dmg;
		Vector3 gridLo = inp.gridLo;
		Vector3 gridHi = inp.gridHi;
		int chunks = dop * 4;
		if (chunkCounts.Length < chunks)
		{
			chunkCounts = new int[chunks];
		}
		for (int b = 0; b < nb; b++)
		{
			SuperBaker.Body body = inp.bodies[b];
			if (body.count <= 0)
			{
				continue;
			}
			if (bodyRecs[b].flags == 0)
			{
				for (int p = body.start; p < body.start + body.count; p++)
				{
					gbid[p] = -1;
				}
				continue;
			}
			Vector3 bodyFlo = bodyRecs[b].flo;
			int bodyHnx = bodyRecs[b].hnx;
			int bodyHny = bodyRecs[b].hny;
			int bodyHnz = bodyRecs[b].hnz;
			int bodyHbx = bodyRecs[b].hbx;
			int bodyHby = bodyRecs[b].hby;
			int bodyBrickBase = bodyRecs[b].brickBase;
			int per = (body.count - 1 + chunks) / chunks;
			Array.Clear(chunkCounts, 0, chunks);
			SuperParallel.For(0, chunks, dop, delegate(int ch)
			{
				int from = ch * per + body.start;
				int to = Math.Min(body.start + body.count, from + per);
				int taken = 0;
				for (int p = from; p < to; p++)
				{
					Vector3 v = pos[p];
					if (v.x < gridLo.x || v.y < gridLo.y || v.z < gridLo.z || v.x > gridHi.x || v.y > gridHi.y || v.z > gridHi.z)
					{
						gbid[p] = -1;
					}
					else if (excludeFailed && dmg[p] >= 1f)
					{
						gbid[p] = -1;
					}
					else
					{
						int cx = SuperReconMath.CellCoord(v.x, bodyFlo.x, cell, bodyHnx);
						int cy = SuperReconMath.CellCoord(v.y, bodyFlo.y, cell, bodyHny);
						int cz = SuperReconMath.CellCoord(v.z, bodyFlo.z, cell, bodyHnz);
						int bi = ((cz >> 3) * bodyHby + (cy >> 3)) * bodyHbx + (cx >> 3);
						gbid[p] = bodyBrickBase + bi;
						local[p] = (ushort)((((cz & 7) << 3) | (cy & 7)) << 3 | (cx & 7));
						mark[bodyBrickBase + bi] = 1;
						taken++;
					}
				}
				chunkCounts[ch] = taken;
			});
			long sum = 0L;
			for (int ch = 0; ch < chunks; ch++)
			{
				sum += chunkCounts[ch];
			}
			if (sum != boundsBack[b].cnt)
			{
				throw new ReconFailure(KeyAabb, string.Format("тело {0} кадра {1}: ЦП отобрал {2} частиц, GPU {3}", b, inp.frameIndex, sum, boundsBack[b].cnt));
			}
		}
		int usedBricks = 0;
		for (int i = 0; i < curBricks; i++)
		{
			if (mark[i] != 0)
			{
				brickTable[i] = usedBricks;
				usedBricks++;
				mark[i] = 0;
			}
			else
			{
				brickTable[i] = -1;
			}
		}
		long occ = (long)usedBricks * BrickCells;
		if (occ >= int.MaxValue)
		{
			throw new ReconFailure(KeyVram, string.Format("кадр {0}: занятых клеток хэша {1} переполняет int", inp.frameIndex, occ));
		}
		curOcc = (int)occ;
		int perKeys = (n - 1 + chunks) / chunks;
		SuperParallel.For(0, chunks, dop, delegate(int ch)
		{
			int from = ch * perKeys;
			int to = Math.Min(n, from + perKeys);
			for (int p = from; p < to; p++)
			{
				int k = (gbid[p] < 0) ? (-1) : (brickTable[gbid[p]] * BrickCells + local[p]);
				keys[p] = (uint)k;
			}
		});
		for (int b = 0; b < nb; b++)
		{
			if (bodyRecs[b].flags == 0)
			{
				continue;
			}
			for (int i = 0; i < bodyRecs[b].bnx; i++)
			{
				Vector3 node = SuperReconMath.BlockNode(bodyRecs[b].flo, 0.004f, i, 0, 0);
				nodes[bodyRecs[b].nodeBase + i] = new NodeEntry
				{
					cell = (uint)SuperReconMath.CellCoord(node.x, bodyRecs[b].flo.x, cell, bodyRecs[b].hnx),
					coord = node.x
				};
			}
			for (int j = 0; j < bodyRecs[b].bny; j++)
			{
				Vector3 node = SuperReconMath.BlockNode(bodyRecs[b].flo, 0.004f, 0, j, 0);
				nodes[bodyRecs[b].nodeBase + bodyRecs[b].bnx + j] = new NodeEntry
				{
					cell = (uint)SuperReconMath.CellCoord(node.y, bodyRecs[b].flo.y, cell, bodyRecs[b].hny),
					coord = node.y
				};
			}
			for (int k = 0; k < bodyRecs[b].bnz; k++)
			{
				Vector3 node = SuperReconMath.BlockNode(bodyRecs[b].flo, 0.004f, 0, 0, k);
				nodes[bodyRecs[b].nodeBase + bodyRecs[b].bnx + bodyRecs[b].bny + k] = new NodeEntry
				{
					cell = (uint)SuperReconMath.CellCoord(node.z, bodyRecs[b].flo.z, cell, bodyRecs[b].hnz),
					coord = node.z
				};
			}
		}
		return new FrameHandle(this, s, inp.frameIndex, nb, accepted, gpuLo, gpuHi, flo, nx, ny, nz, bnx, bny, bnz, blockBase);
	}

	internal void HookEnd(FrameHandle h)
	{
		long t0 = Stopwatch.GetTimestamp();
		depth++;
		try
		{
			if (disposed)
			{
				throw new InvalidOperationException("SuperReconGpu.HookEnd после Dispose");
			}
			if (h == null || h != hookHandle)
			{
				throw new InvalidOperationException("SuperReconGpu.HookEnd: описатель не от последнего HookBegin");
			}
			if (!curSkipped)
			{
				rcSync.GetData(syncBack);
				long now = Now;
				if (syncBack[0] != 0 || syncBack[1] != curTotalAccepted)
				{
					throw new ReconFailure(KeyHash, string.Format("кадр {0}: итог суммы клеток {1} ≠ принятых частиц {2}; слова отказа 0x{3:X}", h.frame, syncBack[1], curTotalAccepted, syncBack[0]));
				}
				keyMismatch += syncBack[2];
				nodeMismatch += syncBack[3];
				filledCells += syncBack[4];
				dilatedCells += syncBack[5];
				keysChecked += curTotalAccepted;
				hookGpuMsSum += Ms(tSync1Return - tSnapDispatch) + Ms(now - tHashFirstDispatch);
			}
			if (h.remaining != 0)
			{
				if (mode == ReadbackMode.Async)
				{
					PendingBlock pb = new PendingBlock();
					pb.h = h;
					pb.bt = curBT;
					pb.bodyBase = new int[nb];
					pb.bodyCells = new int[nb];
					pb.t0 = Now;
					for (int b = 0; b < nb; b++)
					{
						pb.bodyBase[b] = h.blockBase[b];
						if (h.accepted[b] > 0)
						{
							pb.bodyCells[b] = h.bny[b] * h.bnz[b] * h.bnx[b];
						}
					}
					pb.req = AsyncGPUReadback.Request(rcBlock, curBT * 5 * 4, 0, null);
					Interlocked.Increment(ref _livePending);
					pendingBlock = pb;
					if (!paced)
					{
						CompletePendingBlock(wait: true);
					}
				}
				else
				{
					long blockT0 = Now;
					float[][][] block = new float[nb][][];
					for (int b = 0; b < nb; b++)
					{
						if (h.accepted[b] <= 0)
						{
							continue;
						}
						int cells = h.bny[b] * h.bnz[b] * h.bnx[b];
						block[b] = new float[FC][];
						for (int f = 0; f < FC; f++)
						{
							block[b][f] = new float[cells];
							rcBlock.GetData(block[b][f], 0, curBT * f + h.blockBase[b], cells);
						}
					}
					h.SetBlock(block);
					double blockMs = Ms(Now - blockT0);
					readbackMs.Add(blockMs);
				}
			}
			else
			{
				lock (queueLock)
				{
					if (h.queued == 0)
					{
						ReleaseSlot(h.slot);
					}
				}
				h.SetBlock(new float[nb][][]);
			}
			TestOnHook?.Invoke(this, h);
		}
		finally
		{
			hookHandle = null;
			suppressSubmit = false;
			skipSubmitOnce = true;
			depth--;
			long dt = Stopwatch.GetTimestamp() - t0;
			LastHookTicks = hookBeginTicks + dt;
			hookCount++;
			hookTicksSum += LastHookTicks;
			hookTicksMax = Math.Max(hookTicksMax, LastHookTicks);
			if (depth == 0 && bookMain != null)
			{
				bookMain(dt);
			}
		}
	}

	private void CompletePendingBlock(bool wait)
	{
		PendingBlock pb = pendingBlock;
		if (pb == null)
		{
			return;
		}
		try
		{
			if (!pb.req.done)
			{
				if (wait)
				{
					pb.req.WaitForCompletion();
				}
				else
				{
					pb.req.Update();
					if (!pb.req.done)
					{
						return;
					}
				}
			}
			if (pb.req.hasError)
			{
				abandoned = true;
				ReconFailure e = new ReconFailure(KeyReadback, string.Format("чтение блока полей кадра {0} вернуло ошибку AsyncGPUReadback", pb.h.frame));
				pb.h.FailBlock(e);
				throw e;
			}
			NativeArray<float> data = pb.req.GetData<float>(0);
			float[][][] block = new float[nb][][];
			for (int b = 0; b < nb; b++)
			{
				int cells = pb.bodyCells[b];
				if (cells <= 0)
				{
					continue;
				}
				block[b] = new float[FC][];
				for (int f = 0; f < FC; f++)
				{
					block[b][f] = new float[cells];
					NativeArray<float>.Copy(data, pb.bt * f + pb.bodyBase[b], block[b][f], 0, cells);
				}
			}
			pb.h.SetBlock(block);
			readbackMs.Add(Ms(Now - pb.t0));
		}
		finally
		{
			pendingBlock = null;
			Interlocked.Decrement(ref _livePending);
		}
	}

	private int FreeRing()
	{
		for (int r = 0; r < VertexRing; r++)
		{
			if (ringReq[r] == null)
			{
				return r;
			}
		}
		return -1;
	}

	internal void Pump(bool drainNow)
	{
		if (disposed)
		{
			return;
		}
		long t0 = Stopwatch.GetTimestamp();
		depth++;
		try
		{
			bool cancel = abandoned || CancelRequested;
			CompletePendingBlock(drainNow);
			for (int r = 0; r < VertexRing; r++)
			{
				VertexRequest req = ringReq[r];
				if (req == null)
				{
					continue;
				}
				if (mode == ReadbackMode.Async && !req.gpuReq.done)
				{
					if (drainNow)
					{
						req.gpuReq.WaitForCompletion();
					}
					else
					{
						req.gpuReq.Update();
						if (!req.gpuReq.done)
						{
							continue;
						}
					}
				}
				CompleteVertex(r);
			}
			ReapSlots();
			DropStale(cancel);
			bool submit = false;
			if (!suppressSubmit && !cancel && (drainNow || skipSubmitOnce))
			{
				submit = mode != ReadbackMode.Sync || !paced || drainNow || slotWaitOpen || pipeWaitOpen;
			}
			if (drainNow || skipSubmitOnce)
			{
				skipSubmitOnce = false;
			}
			if (!submit)
			{
				return;
			}
			SubmitQueued();
			if (!drainNow)
			{
				return;
			}
			while (true)
			{
				bool any = false;
				for (int r = 0; r < VertexRing; r++)
				{
					VertexRequest req = ringReq[r];
					if (req == null)
					{
						continue;
					}
					any = true;
					if (mode == ReadbackMode.Async && !req.gpuReq.done)
					{
						req.gpuReq.WaitForCompletion();
					}
					CompleteVertex(r);
				}
				ReapSlots();
				DropStale(abandoned || CancelRequested);
				int queued;
				lock (queueLock)
				{
					queued = queue.Count;
				}
				if (!any && queued == 0)
				{
					break;
				}
				if (abandoned || CancelRequested)
				{
					break;
				}
				SubmitQueued();
			}
		}
		finally
		{
			depth--;
			if (depth == 0 && bookMain != null)
			{
				bookMain(Stopwatch.GetTimestamp() - t0);
			}
		}
	}

	internal void PumpQuiet()
	{
		try
		{
			Pump(drainNow: false);
		}
		catch (Exception ex)
		{
			abandoned = true;
			UnityEngine.Debug.LogWarning("[S39 recon] насос при останове: " + ex.Message);
		}
	}

	internal bool HasFreeSlot()
	{
		long t0 = Now;
		ReapSlots();
		bool free = false;
		for (int s = 0; s < slots; s++)
		{
			if (slotState[s] == SlotState.Free)
			{
				free = true;
				break;
			}
		}
		if (free)
		{
			if (slotWaitOpen)
			{
				slotWaitOpen = false;
				slotWaitS += Ms(Now - slotWaitStart) / 1000.0;
			}
		}
		else if (!slotWaitOpen)
		{
			slotWaits++;
			slotWaitOpen = true;
			slotWaitStart = Now;
		}
		if (bookMain != null)
		{
			bookMain(Now - t0);
		}
		return free;
	}

	internal void NoteBackpressure(bool waiting)
	{
		if (waiting)
		{
			if (!pipeWaitOpen)
			{
				pipeWaits++;
				pipeWaitOpen = true;
				pipeWaitStart = Now;
			}
		}
		else if (pipeWaitOpen)
		{
			pipeWaitOpen = false;
			pipeWaitS += Ms(Now - pipeWaitStart) / 1000.0;
		}
	}

	private void ReapSlots()
	{
		lock (queueLock)
		{
			for (int s = 0; s < slots; s++)
			{
				if (slotState[s] != SlotState.InUse)
				{
					continue;
				}
				FrameHandle owner = slotOwner[s];
				if (owner == null || owner == hookHandle || owner.remaining != 0 || !owner.closed || owner.queued != 0)
				{
					continue;
				}
				ReleaseSlot(s);
			}
		}
	}

	private void DropStale(bool all)
	{
		List<VertexRequest> stale = null;
		lock (queueLock)
		{
			if (queue.Count != 0)
			{
				int count = queue.Count;
				for (int i = 0; i < count; i++)
				{
					VertexRequest req = queue.Dequeue();
					if (!all && !req.handle.closed)
					{
						queue.Enqueue(req);
						continue;
					}
					req.handle.queued--;
					if (stale == null)
					{
						stale = new List<VertexRequest>();
					}
					stale.Add(req);
				}
			}
		}
		if (stale != null)
		{
			foreach (VertexRequest req in stale)
			{
				Signal(req, new ReconFailure(KeyAbandoned, string.Format("прогон отменён/оборван — ожидание полей вершин тела {0} кадра {1} снято", req.body, req.handle.frame)));
			}
		}
	}

	private void SubmitQueued()
	{
		while (true)
		{
			VertexRequest req = null;
			lock (queueLock)
			{
				if (queue.Count > 0)
				{
					int ring = FreeRing();
					if (ring >= 0)
					{
						req = queue.Dequeue();
					}
				}
			}
			if (req == null)
			{
				break;
			}
			Submit(req);
		}
	}

	private static void Signal(VertexRequest rq, Exception error)
	{
		lock (rq)
		{
			rq.error = error;
			rq.done = true;
			Monitor.PulseAll(rq);
		}
	}

	private void Submit(VertexRequest req)
	{
		int count = req.verts.Length;
		int slot = req.handle.slot;
		long t0 = Stopwatch.GetTimestamp();
		int ring = FreeRing();
		if (ring < 0)
		{
			throw new InvalidOperationException("SuperReconGpu.Submit без свободного места кольца");
		}
		EnsureCap(ref rcVerts[ring], ref capVerts[ring], string.Format("RcVerts#{0}", ring), count, v => new SuperScenePlan.Sizes(n, 0, SuperKernelGPU.TwoFieldContact)
		{
			reconBodies = nb,
			reconVerts = v
		});
		EnsureCap(ref rcVField[ring], ref capVField[ring], string.Format("RcVField#{0}", ring), count, v => new SuperScenePlan.Sizes(n, 0, SuperKernelGPU.TwoFieldContact)
		{
			reconBodies = nb,
			reconVerts = v
		});
		rcVerts[ring].SetData(req.verts, 0, 0, count);
		cs.SetInt("_RcN", n);
		cs.SetInt("_RcNumBodies", nb);
		cs.SetInt("_RcBody", req.body);
		cs.SetInt("_RcVertCount", count);
		cs.SetInt("_RcMutTap", MutantTap ? 1 : 0);
		SuperGpuBind.Bind[] binds = new SuperGpuBind.Bind[8];
		binds[0] = SuperGpuBind.Bind.U("_RcVField", rcVField[ring]);
		binds[1] = SuperGpuBind.Bind.R("_RcVertsR", rcVerts[ring]);
		binds[2] = SuperGpuBind.Bind.R("_RcBodyR", rcBody[slot]);
		binds[3] = SuperGpuBind.Bind.R("_RcBrickR", rcBrick[slot]);
		binds[4] = SuperGpuBind.Bind.R("_RcSlotStartR", rcSlotStart[slot]);
		binds[5] = SuperGpuBind.Bind.R("_RcOrderR", rcOrder[slot]);
		binds[6] = SuperGpuBind.Bind.R("_RcPosR", rcPos[slot]);
		binds[7] = SuperGpuBind.Bind.R("_RcPfR", rcPf[slot]);
		BindRc(kVertexFields, "KReconVertexFields", binds);
		Go(kVertexFields, count);
		req.ring = ring;
		req.tSubmit = Stopwatch.GetTimestamp();
		ringReq[ring] = req;
		vertexRequests++;
		lock (queueLock)
		{
			req.handle.queued--;
		}
		if (mode == ReadbackMode.Async)
		{
			req.gpuReq = AsyncGPUReadback.Request(rcVField[ring], count * FC * sizeof(float), 0, null);
			req.gpuPending = true;
			Interlocked.Increment(ref _livePending);
			if (ProfileRecon)
			{
				req.gpuReq.WaitForCompletion();
				Profile.Add(ReconProfileData.Slot.VertexFields, Stopwatch.GetTimestamp() - t0);
			}
		}
		else
		{
			for (int f = 0; f < FC; f++)
			{
				rcVField[ring].GetData(req.result[f], 0, count * f, count);
			}
			if (ProfileRecon)
			{
				Profile.Add(ReconProfileData.Slot.VertexFields, Stopwatch.GetTimestamp() - t0);
			}
			CompleteVertex(ring);
		}
	}

	private void CompleteVertex(int r)
	{
		VertexRequest req = ringReq[r];
		if (req == null)
		{
			return;
		}
		try
		{
			if (mode == ReadbackMode.Async && req.gpuReq.hasError)
			{
				abandoned = true;
				ReconFailure e = new ReconFailure(KeyReadback, string.Format("чтение полей вершин тела {0} кадра {1} вернуло ошибку AsyncGPUReadback", req.body, req.handle.frame));
				Signal(req, e);
				throw e;
			}
			if (mode == ReadbackMode.Async)
			{
				int count = req.verts.Length;
				NativeArray<float> data = req.gpuReq.GetData<float>(0);
				for (int f = 0; f < FC; f++)
				{
					NativeArray<float>.Copy(data, f * count, req.result[f], 0, count);
				}
			}
			readbackMs.Add(Ms(Stopwatch.GetTimestamp() - req.tSubmit));
			Signal(req, null);
		}
		finally
		{
			ringReq[r] = null;
		}
	}

	private void ServeInline(FrameHandle h, int b, Vector3[] verts, float[][] result)
	{
		if (FreeRing() < 0)
		{
			Pump(drainNow: true);
		}
		if (FreeRing() < 0)
		{
			throw new InvalidOperationException("SuperReconGpu.ServeInline: кольцо не освободилось");
		}
		VertexRequest req = new VertexRequest();
		req.handle = h;
		req.body = b;
		req.verts = verts;
		req.result = result;
		req.t0 = Stopwatch.GetTimestamp();
		lock (queueLock)
		{
			h.queued++;
		}
		Submit(req);
		if (mode == ReadbackMode.Async)
		{
			if (!req.gpuReq.done)
			{
				req.gpuReq.WaitForCompletion();
			}
			CompleteVertex(req.ring);
		}
		if (req.error != null)
		{
			throw req.error;
		}
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}
		disposed = true;
		abandoned = true;
		if (SabotageSkipRelease)
		{
			LastLeaked = this;
			return;
		}
		ReleaseAll();
	}

	private void ReleaseAll()
	{
		if (released)
		{
			return;
		}
		released = true;
		abandoned = true;
		ReconFailure abort = new ReconFailure("super.fail.recon_abandoned", "прогон отменён/оборван — водитель освобождён");
		PendingBlock pb = pendingBlock;
		if (pb != null)
		{
			try
			{
				if (!pb.req.done)
				{
					pb.req.WaitForCompletion();
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogWarning("[S39 recon] ожидание блока при освобождении: " + ex.ToString());
			}
			finally
			{
				pendingBlock = null;
				Interlocked.Decrement(ref _livePending);
			}
			pb.h.FailBlock(abort);
		}
		for (int r = 0; r < VertexRing; r++)
		{
			VertexRequest req = ringReq[r];
			if (req == null)
			{
				continue;
			}
			try
			{
				if (req.gpuPending && !req.gpuReq.done)
				{
					req.gpuReq.WaitForCompletion();
				}
			}
			catch (Exception ex2)
			{
				UnityEngine.Debug.LogWarning("[S39 recon] ожидание кольца при освобождении: " + ex2.ToString());
			}
			finally
			{
				ringReq[r] = null;
			}
			Signal(req, abort);
		}
		DropStale(all: true);
		lock (queueLock)
		{
			for (int s = 0; s < slots; s++)
			{
				FrameHandle owner = slotOwner[s];
				if (owner != null)
				{
					owner.FailBlock(abort);
				}
			}
			if (hookHandle != null)
			{
				hookHandle.FailBlock(abort);
			}
			hookHandle = null;
		}
		foreach (KeyValuePair<string, ComputeBuffer> kv in AuditBuffers())
		{
			if (kv.Value != null)
			{
				Free(ref FieldOf(kv.Key), kv.Key);
			}
		}
		int inUse = 0;
		for (int s = 0; s < slots; s++)
		{
			inUse += (slotState[s] == SlotState.InUse) ? 1 : 0;
			slotState[s] = SlotState.Free;
			slotOwner[s] = null;
		}
		if (inUse > 0)
		{
			Interlocked.Add(ref _liveSlots, -inUse);
		}
		if (kernelRef != null)
		{
			kernelRef.ExternalVramBytes = 0L;
		}
		Interlocked.Decrement(ref _liveDrivers);
	}

	internal static void DisposeLeakedForGate()
	{
		SuperReconGpu leaked = LastLeaked;
		LastLeaked = null;
		if (leaked != null)
		{
			leaked.ReleaseAll();
		}
	}

	internal void ReadbackPf(FrameHandle h, float[][] into)
	{
		if (h == null)
		{
			throw new ArgumentNullException("h");
		}
		if (slotOwner[h.slot] == h)
		{
			float[] all = new float[n * FC];
			rcPf[h.slot].GetData(all);
			for (int f = 0; f < FC; f++)
			{
				if (into[f] == null || into[f].Length < n)
				{
					into[f] = new float[n];
				}
				Array.Copy(all, n * f, into[f], 0L, n);
			}
		}
	}

	internal uint[] ReadbackMask0(FrameHandle h, int body)
	{
		if (h == null)
		{
			throw new ArgumentNullException("h");
		}
		if (body < 0 || body >= nb)
		{
			throw new ArgumentOutOfRangeException("body");
		}
		if (h.accepted[body] <= 0)
		{
			return new uint[0];
		}
		long words = (long)h.bnx[body] * h.bny[body] * h.bnz[body];
		uint[] mask = new uint[words];
		if (words != 0)
		{
			if (mask0Captured != null)
			{
				Array.Copy(mask0Captured, h.blockBase[body], mask, 0L, words);
			}
			else
			{
				rcMask0.GetData(mask, 0, h.blockBase[body], (int)words);
			}
		}
		return mask;
	}

	internal void ReadbackHash(FrameHandle h, out uint[] slotStart, out uint[] order)
	{
		if (h == null)
		{
			throw new ArgumentNullException("h");
		}
		if (Thread.CurrentThread.GetHashCode() != mainThreadId)
		{
			throw new InvalidOperationException("SuperReconGpu.ReadbackHash: только главный поток");
		}
		if (hookHandle == h)
		{
			throw new InvalidOperationException(string.Format("SuperReconGpu.ReadbackHash: HookEnd кадра {0} ещё не вызван", h.frame));
		}
		long occSlots = h.occSlots;
		if (occSlots == 0)
		{
			slotStart = new uint[1];
			order = new uint[0];
			return;
		}
		if (slotOwner[h.slot] != h)
		{
			throw new InvalidOperationException(string.Format("SuperReconGpu.ReadbackHash: слот {0} уже не у кадра {1}", h.slot, h.frame));
		}
		if (rcSlotStart[h.slot] == null || rcOrder[h.slot] == null)
		{
			throw new InvalidOperationException(string.Format("SuperReconGpu.ReadbackHash: буферы хэша слота {0} не выделены при {1} клетках", h.slot, h.occSlots));
		}
		if (occSlots + 1 > rcSlotStart[h.slot].count)
		{
			throw new InvalidOperationException(string.Format("SuperReconGpu.ReadbackHash: RcSlotStart#{0} на {1} слов при {2} клетках", h.slot, rcSlotStart[h.slot].count, h.occSlots));
		}
		slotStart = new uint[occSlots + 1];
		rcSlotStart[h.slot].GetData(slotStart, 0, 0, (int)(occSlots + 1));
		long prefix = slotStart[occSlots];
		if (prefix > n || prefix > rcOrder[h.slot].count)
		{
			throw new InvalidOperationException(string.Format("SuperReconGpu.ReadbackHash: итог префикса {0} при {1} частицах (RcOrder#{2} на {3} слов)", prefix, n, h.slot, rcOrder[h.slot].count));
		}
		order = new uint[prefix];
		if (prefix != 0)
		{
			rcOrder[h.slot].GetData(order, 0, 0, (int)prefix);
		}
	}
}
