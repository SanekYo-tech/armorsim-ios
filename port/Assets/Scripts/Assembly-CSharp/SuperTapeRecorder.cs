using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class SuperTapeRecorder
{
	internal sealed class FramePipeline
	{
		private sealed class Encoded
		{
			public SuperTape.Encoded enc;

			public FrameStats stats;

			public byte[] payload => enc.bytes;
		}

		private readonly RunData rd;

		private readonly SuperTape.Writer tape;

		private readonly Summary s;

		private readonly Action onFrame;

		private readonly Action onPump;

		private readonly Action<SuperShot.KeyframeContext> runCheckpoint;

		private readonly int bodyCount;

		private readonly int fieldCount;

		public int K;

		public int dop;

		public int workers;

		internal Action GpuPump;

		internal Action GpuPumpQuiet;

		private readonly object gate;

		private readonly object tapeLock;

		private readonly Stopwatch flushClock;

		private readonly Stack<Snapshot> pool;

		private readonly SortedDictionary<int, Encoded> ready;

		private readonly List<Task> tasks;

		private readonly HashSet<int> threadIds;

		private readonly int mainThreadId;

		private readonly int baseFrames;

		private readonly Queue<Snapshot> pending;

		private int queued;

		private int building;

		private long readyBytes;

		private int lookAhead;

		private long readyCap;

		private readonly long[] recent;

		private int recentAt;

		private int recentCount;

		private readonly Stopwatch kClock;

		private double lastKEvalS;

		private int kRefusedAt;

		private int created;

		private int enqueued;

		private int written;

		private int notified;

		private int nextWrite;

		private int builtOffMain;

		private int dropped;

		private int droppedAfter;

		private long measuredPerFrame;

		private long availAtStart;

		private long availRecent;

		private long modelPerFrame;

		private bool forced;

		private int dopMin;

		private int dopMax;

		private Exception error;

		private bool cancel;

		private bool stopWriter;

		private Thread writer;

		private bool shut;

		private int ckTarget;

		private SuperShot.KeyframeContext ckCtx;

		private Task prealloc;

		private int preallocPending;

		private int built;

		internal bool CancelRequested => cancel;

		internal int NextFrameIndex => enqueued;

		public int FramesBuilt => built + baseFrames;

		private int InFlight => enqueued - written - baseFrames;

		public bool Drained => enqueued - written == baseFrames;

		private FramePipeline(RunData rd, SuperTape.Writer tape, Summary s, Action onFrame, Action onPump, Action<SuperShot.KeyframeContext> runCheckpoint)
		{
			gate = new object();
			tapeLock = new object();
			flushClock = Stopwatch.StartNew();
			pool = new Stack<Snapshot>();
			ready = new SortedDictionary<int, Encoded>();
			tasks = new List<Task>();
			threadIds = new HashSet<int>();
			pending = new Queue<Snapshot>();
			recent = new long[8];
			kClock = Stopwatch.StartNew();
			kRefusedAt = -1;
			droppedAfter = -1;
			dopMin = int.MaxValue;
			this.rd = rd;
			this.tape = tape;
			this.s = s;
			this.onFrame = onFrame;
			this.onPump = onPump;
			this.runCheckpoint = runCheckpoint;
			bodyCount = tape.BodyCount;
			fieldCount = tape.FieldCount;
			mainThreadId = Thread.CurrentThread.GetHashCode();
			baseFrames = s.frames;
			enqueued = s.frames;
			nextWrite = s.frames;
		}

		public static FramePipeline Start(RunData rd, SuperTape.Writer tape, Summary s, Action onFrame, Action onPump, Action<SuperShot.KeyframeContext> runCheckpoint, SuperParticles p)
		{
			FramePipeline pipe = new FramePipeline(rd, tape, s, onFrame, onPump, runCheckpoint);
			pipe.workers = Math.Max(1, Environment.ProcessorCount - 2);
			long cells = 0L;
			for (int i = 0; i < rd.bodies.Length; i++)
			{
				SuperBaker.Body b = rd.bodies[i];
				if (b.count > 0)
				{
					Vector3 mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
					Vector3 mx = new Vector3(float.MinValue, float.MinValue, float.MinValue);
					for (int j = b.start; j < b.start + b.count; j++)
					{
						Vector3 v = p.x[j];
						if (v.x < mn.x)
						{
							mn.x = v.x;
						}
						if (v.y < mn.y)
						{
							mn.y = v.y;
						}
						if (v.z < mn.z)
						{
							mn.z = v.z;
						}
						if (v.x > mx.x)
						{
							mx.x = v.x;
						}
						if (v.y > mx.y)
						{
							mx.y = v.y;
						}
						if (v.z > mx.z)
						{
							mx.z = v.z;
						}
					}
					long nx = (long)((int)Math.Ceiling((double)((mx.x - mn.x) / rd.spacing))) + 8;
					long ny = (long)((int)Math.Ceiling((double)((mx.y - mn.y) / rd.spacing))) + 8;
					long nz = (long)((int)Math.Ceiling((double)((mx.z - mn.z) / rd.spacing))) + 8;
					cells += nx * ny * nz;
				}
			}
			long avail = SuperMachine.AvailablePhysicalBytes(out string src);
			long reconstructionBytes = (long)rd.n * 113 + cells * 16;
			long k;
			if (reconstructionBytes > 0)
			{
				k = (long)((double)avail * 0.5 / (double)reconstructionBytes);
			}
			else
			{
				k = pipe.workers;
			}
			pipe.K = (int)Math.Max(1, Math.Min(pipe.workers, k));
			if (ForceK > 0)
			{
				pipe.K = ForceK;
			}
			pipe.dop = Math.Max(1, pipe.workers / pipe.K);
			if (ForceDop > 0)
			{
				pipe.dop = ForceDop;
			}
			pipe.forced = ForceK > 0;
			pipe.availAtStart = avail;
			pipe.availRecent = avail;
			long snapshotBytes = (long)rd.n * 57;
			if (snapshotBytes > 0)
			{
				pipe.lookAhead = (int)Math.Max(0, Math.Min(2 * pipe.K, (long)((double)avail * 0.15 / (double)snapshotBytes)));
			}
			else
			{
				pipe.lookAhead = 0;
			}
			if (ForceQ >= 0)
			{
				pipe.lookAhead = ForceQ;
			}
			pipe.readyCap = (long)((double)avail * 0.1);
			_building = 0;
			PipelineInFlight = 0;
			PipelineReadyUnwritten = 0;
			PipelineQueued = 0;
			PipelineK = pipe.K;
			PipelineDop = pipe.dop;
			ThreadPool.GetMinThreads(out int minWorker, out int minIo);
			if (minWorker < pipe.workers + 2)
			{
				ThreadPool.SetMinThreads(pipe.workers + 2, minIo);
			}
			s.inFlightK = pipe.K;
			s.inFlightKStart = pipe.K;
			s.inFlightKMin = pipe.K;
			s.intraDop = pipe.dop;
			s.workers = pipe.workers;
			s.perFrameBytesEstimate = (double)reconstructionBytes;
			s.availablePhysMB = (double)avail / 1048576.0;
			s.baseFrames = pipe.baseFrames;
			string line1 = string.Format("[S25 конвейер] частиц {0}, клеток восстановления ≈{1}, кадр в полёте ≈{2:F1} МБ; ", rd.n, reconstructionBytes, (double)reconstructionBytes / 1048576.0);
			string line2 = string.Format("доступно {0:F0} МБ ({1}); рабочих {2}, K = {3}, внутри кадра {4}", (double)avail / 1048576.0, src, pipe.workers, pipe.K, pipe.dop);
			string forcedNote = ((ForceK > 0 || ForceDop > 0) ? string.Format(" (ПРИНУДИТЕЛЬНО: K {0}, dop {1})", ForceK, ForceDop) : "");
			string line3 = string.Format("; S33: забег снимков Q = {0} (снимок {1:F1} МБ), ", pipe.lookAhead, (double)snapshotBytes / 1048576.0);
			string line4 = string.Format("потолок готовых payload'ов R = {0:F0} МБ (обе доли ASSUMED)", (double)pipe.readyCap / 1048576.0);
			Debug.Log(string.Concat(new string[5] { line1, line2, forcedNote, line3, line4 }));
			Thread writer = new Thread(pipe.WriterLoop)
			{
				IsBackground = true,
				Name = "S25 tape writer"
			};
			pipe.writer = writer;
			writer.Start();
			FramePipeline pl = pipe;
			int ahead = pipe.K + pipe.lookAhead;
			int n = rd.n;
			if (ahead > 0 && !SabotageNoBackpressure)
			{
				pipe.preallocPending = ahead;
				pipe.prealloc = Task.Factory.StartNew(delegate
				{
					try
					{
						for (int l = 0; l < ahead; l++)
						{
							if (pl.cancel)
							{
								break;
							}
							Snapshot sn = new Snapshot(n);
							Interlocked.Increment(ref _liveSnapshots);
							lock (pl.gate)
							{
								pl.created++;
								pl.pool.Push(sn);
								pl.preallocPending--;
								Monitor.PulseAll(pl.gate);
							}
						}
					}
					finally
					{
						lock (pl.gate)
						{
							pl.preallocPending = 0;
							Monitor.PulseAll(pl.gate);
						}
					}
				}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			}
			return pipe;
		}

		public Snapshot Rent()
		{
			lock (gate)
			{
				while (pool.Count == 0 && preallocPending > 0)
				{
					Monitor.Wait(gate, 20);
				}
				if (pool.Count > 0)
				{
					return pool.Pop();
				}
				created++;
			}
			Interlocked.Increment(ref _liveSnapshots);
			return new Snapshot(rd.n);
		}

		public int Enqueue(Snapshot sn)
		{
			ThrowIfFailed();
			sn.index = enqueued;
			lock (gate)
			{
				pending.Enqueue(sn);
				PipelineQueued = ++queued;
				PipelineInFlight = enqueued - written - baseFrames;
			}
			TryDispatch();
			return sn.index + 1;
		}

		private int AllowedInFlight()
		{
			long maxRecent = 0L;
			for (int i = 0; i < recentCount; i++)
			{
				if (recent[i] > maxRecent)
				{
					maxRecent = recent[i];
				}
			}
			if (maxRecent <= 0)
			{
				return K;
			}
			return (int)Math.Max(1, Math.Min(K, (long)((double)availRecent * 0.5) / maxRecent));
		}

		private long RecentMaxLocked()
		{
			long num = 0L;
			for (int i = 0; i < recentCount; i++)
			{
				if (recent[i] > num)
				{
					num = recent[i];
				}
			}
			return num;
		}

		private void TryDispatch()
		{
			while (true)
			{
				Snapshot sn;
				int dopI;
				lock (gate)
				{
					if (cancel)
					{
						return;
					}
					if (pending.Count == 0 || building >= K)
					{
						return;
					}
					if (building > 0 && building >= AllowedInFlight())
					{
						return;
					}
					sn = pending.Dequeue();
					queued--;
					building++;
					dopI = ElasticDop(sn.index);
					if (dopI < dopMin)
					{
						dopMin = dopI;
					}
					if (dopI > dopMax)
					{
						dopMax = dopI;
					}
					PipelineQueued = queued;
					PipelineDop = dopI;
				}
				Interlocked.Increment(ref _liveTasks);
				Task task = Task.Factory.StartNew(delegate
				{
					Build(sn, dopI);
				}, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
				lock (gate)
				{
					tasks.RemoveAll((Task x) => x.IsCompleted);
					tasks.Add(task);
				}
			}
		}

		private int ElasticDop(int frameIndex)
		{
			if (ForceDopCycle != null && ForceDopCycle.Length != 0)
			{
				return Math.Max(1, ForceDopCycle[((frameIndex % ForceDopCycle.Length) + ForceDopCycle.Length) % ForceDopCycle.Length]);
			}
			if (ForceDop > 0)
			{
				return ForceDop;
			}
			return Math.Max(1, workers / Math.Max(1, Math.Min(K, queued + building)));
		}

		private void Build(Snapshot sn, int dop)
		{
			bool counted = false;
			try
			{
				if (cancel)
				{
					return;
				}
				Interlocked.Increment(ref _building);
				counted = true;
				int tid = Thread.CurrentThread.GetHashCode();
				if (tid != mainThreadId)
				{
					Interlocked.Increment(ref builtOffMain);
				}
				lock (threadIds)
				{
					threadIds.Add(tid);
				}
				long t0 = SuperHookStages.Now;
				SuperTape.Frame f = BuildFrame(rd, sn, dop, s.stages, out FrameStats stats);
				long t1 = SuperHookStages.Now;
				SuperTape.Encoded enc = tape.EncodeFrame(f);
				long payloadBytes = enc.canonicalLen;
				s.stages.FrameDone(s.stages.Lap(SuperHookStages.Stage.Encode, t1) - t0);
				long model = (long)rd.n * 57 + ((dop >= 3) ? ((long)rd.n * 56) : 0) + 16 * (stats.gridCells + 6 * stats.frameRawVerts) + payloadBytes;
				long honest = (long)rd.n * 57 + stats.bytesLattice + payloadBytes + stats.bytesMesh + stats.bytesFields + stats.bytesDebris;
				long seen;
				do
				{
					seen = Interlocked.Read(ref measuredPerFrame);
					if (honest <= seen)
					{
						break;
					}
				}
				while (Interlocked.CompareExchange(ref measuredPerFrame, honest, seen) != seen);
				do
				{
					seen = Interlocked.Read(ref modelPerFrame);
					if (model <= seen)
					{
						break;
					}
				}
				while (Interlocked.CompareExchange(ref modelPerFrame, model, seen) != seen);
				Interlocked.Increment(ref built);
				lock (gate)
				{
					s.bytesFramesCounted++;
					if (stats.bytesLattice > s.bytesLatticeMax)
					{
						s.bytesLatticeMax = stats.bytesLattice;
					}
					if (stats.bytesDensity > s.bytesDensityMax)
					{
						s.bytesDensityMax = stats.bytesDensity;
					}
					if (stats.bytesDensityWork > s.bytesDensityWorkMax)
					{
						s.bytesDensityWorkMax = stats.bytesDensityWork;
					}
					if (stats.bytesHash > s.bytesHashMax)
					{
						s.bytesHashMax = stats.bytesHash;
					}
					if (stats.bytesMesh > s.bytesMeshMax)
					{
						s.bytesMeshMax = stats.bytesMesh;
					}
					if (stats.bytesFields > s.bytesFieldsMax)
					{
						s.bytesFieldsMax = stats.bytesFields;
					}
					if (payloadBytes > s.bytesPayloadMax)
					{
						s.bytesPayloadMax = payloadBytes;
					}
					if (stats.bytesBodyMax > s.bytesBodyMaxSeen)
					{
						s.bytesBodyMaxSeen = stats.bytesBodyMax;
					}
					if (honest > s.bytesHonestMax)
					{
						s.bytesHonestMax = honest;
					}
					s.bytesLatticeMean += (double)stats.bytesLattice;
					s.bytesMeshMean += (double)stats.bytesMesh;
					s.bytesFieldsMean += (double)stats.bytesFields;
					s.bytesPayloadMean += (double)enc.canonicalLen;
					s.bytesHonestMean += (double)honest;
					recent[recentAt] = honest;
					recentAt = (recentAt + 1) % recent.Length;
					if (recentCount < recent.Length)
					{
						recentCount++;
					}
					Encoded e = new Encoded();
					e.enc = enc;
					e.stats = stats;
					ready[sn.index] = e;
					readyBytes += enc.bytes.Length;
					PipelineReadyUnwritten = ready.Count;
					Monitor.PulseAll(gate);
				}
			}
			finally
			{
				if (counted)
				{
					Interlocked.Decrement(ref _building);
				}
				SuperReconGpu.FrameHandle h = sn.recon;
				sn.recon = null;
				if (h != null)
				{
					h.ReleaseFromWorker();
				}
				lock (gate)
				{
					building--;
					pool.Push(sn);
					Monitor.PulseAll(gate);
				}
				Interlocked.Decrement(ref _liveTasks);
				if (!cancel)
				{
					TryDispatch();
				}
			}
		}

		private void Fail(Exception e)
		{
			lock (gate)
			{
				if (error == null)
				{
					error = e;
				}
				cancel = true;
				Monitor.PulseAll(gate);
			}
		}

		private void WriterLoop()
		{
			try
			{
				while (true)
				{
					Encoded e = null;
					int frame;
					lock (gate)
					{
						while (!stopWriter && !cancel && !ready.ContainsKey(nextWrite))
						{
							Monitor.Wait(gate);
						}
						if (cancel)
						{
							return;
						}
						if (!ready.TryGetValue(nextWrite, out e))
						{
							return;
						}
						ready.Remove(nextWrite);
						readyBytes -= e.enc.bytes.Length;
						PipelineReadyUnwritten = ready.Count;
						frame = nextWrite;
						nextWrite++;
					}
					if (AbortOnDegenerateFrame)
					{
						int degenerateFrame;
						lock (DegenerateLock)
						{
							degenerateFrame = DegenerateFrame;
						}
						if (degenerateFrame >= 0 && frame > degenerateFrame)
						{
							if (droppedAfter < 0)
							{
								droppedAfter = degenerateFrame;
							}
							Interlocked.Increment(ref dropped);
							Interlocked.Increment(ref written);
							PipelineInFlight = enqueued - written - baseFrames;
							lock (gate)
							{
								Monitor.PulseAll(gate);
							}
							continue;
						}
					}
					long t = SuperHookStages.Now;
					lock (tapeLock)
					{
						tape.WriteEncoded(e.enc);
						if (flushClock.Elapsed.TotalSeconds >= TapeFlushEveryS)
						{
							tape.FlushAndTell();
							flushClock.Restart();
						}
					}
					s.stages.Lap(SuperHookStages.Stage.Write, t);
					SuperReconstruct.NoteFailedExcluded(e.stats.lastFailedExcluded);
					if (e.stats.frameVerts > s.maxVertsFrame)
					{
						s.maxVertsFrame = e.stats.frameVerts;
					}
					if (e.stats.frameRawVerts > s.maxRawVertsFrame)
					{
						s.maxRawVertsFrame = e.stats.frameRawVerts;
					}
					Interlocked.Increment(ref s.frames);
					Interlocked.Increment(ref written);
					PipelineInFlight = enqueued - written - baseFrames;
					lock (gate)
					{
						Monitor.PulseAll(gate);
					}
				}
			}
			catch (Exception e2)
			{
				Fail(e2);
			}
		}

		private void ThrowIfFailed()
		{
			Exception e;
			lock (gate)
			{
				e = error;
			}
			if (e != null)
			{
				ExceptionDispatchInfo.Capture(e).Throw();
			}
		}

		public void WaitProgress(int ms)
		{
			lock (gate)
			{
				Monitor.Wait(gate, ms);
			}
		}

		public void RequestCheckpoint(int framesTarget, SuperShot.KeyframeContext ctx)
		{
			ckTarget = framesTarget;
			ckCtx = ctx;
		}

		public long FlushAndTell()
		{
			lock (tapeLock)
			{
				return tape.FlushAndTell();
			}
		}

		public long Tell()
		{
			lock (tapeLock)
			{
				return tape.Tell();
			}
		}

		public void FlushToDisk()
		{
			lock (tapeLock)
			{
				tape.FlushToDisk();
			}
		}

		public bool Ready()
		{
			ThrowIfFailed();
			if (ckTarget > 0)
			{
				if (AbortOnDegenerateFrame)
				{
					int degenerateFrame;
					lock (DegenerateLock)
					{
						degenerateFrame = DegenerateFrame;
					}
					if (degenerateFrame >= 0)
					{
						Debug.Log(string.Format("[Super] S37: контрольная точка после кадра {0} снята — ожидается останов ", ckTarget) + string.Format("по вырождению на кадре {0}", degenerateFrame));
						ckTarget = 0;
					}
				}
				if (ckTarget > 0)
				{
					if (written + baseFrames < ckTarget)
					{
						WaitProgress(2);
						ThrowIfFailed();
						if (written + baseFrames < ckTarget)
						{
							return false;
						}
					}
					ckTarget = 0;
					if (AbortOnDegenerateFrame)
					{
						int degenerateFrame;
						lock (DegenerateLock)
						{
							degenerateFrame = DegenerateFrame;
						}
						if (degenerateFrame >= 0)
						{
							Debug.Log(string.Format("[Super] S37: контрольная точка снята перед самой записью — вырождение на кадре {0}", degenerateFrame));
							return true;
						}
					}
					runCheckpoint(ckCtx);
				}
			}
			if (SabotageNoBackpressure)
			{
				return true;
			}
			ReevaluateK();
			if (Admits())
			{
				TryDispatch();
				return true;
			}
			WaitProgress(2);
			ThrowIfFailed();
			TryDispatch();
			return Admits();
		}

		private bool Admits()
		{
			lock (gate)
			{
				return queued + building < AllowedInFlight() + lookAhead && readyBytes < readyCap;
			}
		}

		private void ReevaluateK()
		{
			if (forced)
			{
				return;
			}
			if (kClock.Elapsed.TotalSeconds - lastKEvalS < 0.5)
			{
				return;
			}
			lastKEvalS = kClock.Elapsed.TotalSeconds;
			long avail = SuperMachine.AvailablePhysicalBytes(out string src);
			int building;
			long maxRecent;
			lock (gate)
			{
				maxRecent = RecentMaxLocked();
				building = this.building;
				availRecent = avail;
			}
			if (maxRecent <= 0)
			{
				return;
			}
			int oldK = K;
			int newK = (int)Math.Max(1, Math.Min(workers, (long)((double)avail * 0.5 / (double)maxRecent) + building));
			if (newK < oldK)
			{
				K = newK;
			}
			else if (newK > oldK)
			{
				if (KRaiseNeedsHeadroom)
				{
					if ((K + 1) * maxRecent > (long)((GC.GetTotalMemory(false) + avail) * 0.5))
					{
						if (K != kRefusedAt)
						{
							kRefusedAt = K;
							Debug.Log(string.Format("[S33 конвейер] подъём K {0} → {1} ОТКАЗАН по запасу: ", K, K + 1) + string.Format("{0} × {1:F0} МБ не влезает в половину ", K + 1, (double)maxRecent / 1048576.0) + string.Format("«свободно {0:F0} + куча {1:F0}» МБ", (double)avail / 1048576.0, (double)GC.GetTotalMemory(false) / 1048576.0));
						}
					}
					else
					{
						K++;
					}
				}
				else
				{
					K++;
				}
			}
			if (K == oldK)
			{
				return;
			}
			kRefusedAt = -1;
			s.inFlightK = K;
			PipelineK = K;
			if (K < s.inFlightKMin || s.inFlightKMin == 0)
			{
				s.inFlightKMin = K;
			}
			Debug.Log(string.Format("[S33 конвейер] K {0} → {1}: вес последних {2} кадров ⩽ {3:F1} МБ, ", oldK, K, recentCount, (double)maxRecent / 1048576.0) + string.Format("свободно {0:F0} МБ, строится {1}", (double)avail / 1048576.0, building));
		}

		public void Pump()
		{
			ThrowIfFailed();
			if (GpuPump != null)
			{
				GpuPump();
			}
			if (!cancel)
			{
				TryDispatch();
			}
			int target = written;
			while (notified < target)
			{
				notified++;
				if (onFrame != null)
				{
					onFrame();
				}
			}
			if (onPump != null)
			{
				onPump();
			}
		}

		public void Shutdown()
		{
			if (shut)
			{
				return;
			}
			shut = true;
			bool drained;
			lock (gate)
			{
				drained = (error == null && enqueued - written == baseFrames);
			}
			if (!drained)
			{
				cancel = true;
			}
			lock (gate)
			{
				while (pending.Count > 0)
				{
					Snapshot sn = pending.Dequeue();
					if (sn.recon != null)
					{
						sn.recon.ReleaseFromWorker();
					}
					sn.recon = null;
					pool.Push(sn);
					queued--;
				}
				PipelineQueued = 0;
			}
			Task[] arr = tasks.ToArray();
			if (GpuPumpQuiet != null)
			{
				while (!Task.WaitAll(arr, 50))
				{
					GpuPumpQuiet();
				}
			}
			else
			{
				Task.WaitAll(arr);
			}
			if (prealloc != null)
			{
				prealloc.Wait();
			}
			lock (gate)
			{
				stopWriter = true;
				Monitor.PulseAll(gate);
			}
			if (writer != null)
			{
				writer.Join();
			}
			lock (gate)
			{
				Interlocked.Add(ref _liveSnapshots, -created);
				pool.Clear();
				ready.Clear();
			}
			_building = 0;
			PipelineInFlight = 0;
			PipelineReadyUnwritten = 0;
			PipelineQueued = 0;
			PipelineK = 0;
			PipelineDop = 0;
			if (dropped > 0)
			{
				Debug.Log(string.Format("[лента] S37: после кадра {0} обрезано {1} построенных кадров (вырождение)", droppedAfter, dropped));
			}
			s.intraDopMin = ((dopMin == int.MaxValue) ? s.intraDop : dopMin);
			s.intraDopMax = ((dopMax == 0) ? s.intraDop : dopMax);
			s.lookAheadQ = lookAhead;
			s.readyCapMB = (double)readyCap / 1048576.0;
			s.poolHighWater = created;
			s.framesBuiltOffMain = builtOffMain;
			s.perFrameBytesMeasured = (double)measuredPerFrame;
			s.perFrameBytesModel = (double)modelPerFrame;
			s.bytesSnapshot = (long)rd.n * 57;
			if (s.bytesFramesCounted > 0)
			{
				s.bytesLatticeMean /= (double)s.bytesFramesCounted;
				s.bytesMeshMean /= (double)s.bytesFramesCounted;
				s.bytesFieldsMean /= (double)s.bytesFramesCounted;
				s.bytesPayloadMean /= (double)s.bytesFramesCounted;
				s.bytesHonestMean /= (double)s.bytesFramesCounted;
			}
			lock (threadIds)
			{
				s.observedWorkerThreads = threadIds.Count;
			}
		}

		public string Passport()
		{
			return string.Format(" [конвейер: рабочих {0}, K {1}→{2}→{3}, внутри кадра ", workers, s.inFlightKStart, s.inFlightKMin, K) + string.Format("{0}…{1} (забег Q {2}), пул {3}, построено вне главного {4}, ", s.intraDopMin, s.intraDopMax, lookAhead, created, builtOffMain) + string.Format("потоков {0}, досушка {1:F2} с]", s.observedWorkerThreads, s.drainS);
		}
	}

	public sealed class Summary
	{
		public string path;

		public int frames;

		public int particles;

		public long fileBytes;

		public double wallS;

		public SuperShot.Result shot;

		public int maxVertsFrame;

		public int maxRawVertsFrame;

		public bool ok;

		public string failure;

		public string failureKey;

		public object[] failureArgs;

		public SuperRunRecord.FailClass failClass;

		public SuperHookStages stages;

		public PipelineMode pipeline;

		public int workers;

		public int inFlightK;

		public int intraDop;

		public int poolHighWater;

		public int framesBuiltOffMain;

		public int observedWorkerThreads;

		public double hookWaitS;

		public double drainS;

		public double perFrameBytesEstimate;

		public double availablePhysMB;

		public int baseFrames;

		public int inFlightKStart;

		public int inFlightKMin;

		public int intraDopMin;

		public int intraDopMax;

		public int lookAheadQ;

		public double readyCapMB;

		public double perFrameBytesMeasured;

		public double perFrameBytesModel;

		public long bytesLatticeMax;

		public long bytesMeshMax;

		public long bytesFieldsMax;

		public long bytesPayloadMax;

		public long bytesSnapshot;

		public long bytesHonestMax;

		public long bytesDensityMax;

		public long bytesDensityWorkMax;

		public long bytesHashMax;

		public double bytesLatticeMean;

		public double bytesMeshMean;

		public double bytesFieldsMean;

		public double bytesPayloadMean;

		public double bytesHonestMean;

		public long bytesBodyMaxSeen;

		public int bytesFramesCounted;

		public SuperStepPacer pacer;

		public int stepNow;

		public int stepsTotal;

		public int framesBuilt;

		public string fieldsPath;

		public string fieldsPathWhy;

		public string reconReadback;

		public int reconSlots;

		public int reconSlotWaits;

		public int reconFramesGpu;

		public int reconVertexRequests;

		public double reconSlotWaitS;

		public double reconHookMsMean;

		public double reconHookMsMax;

		public double reconHookGpuMs;

		public double reconReadbackMsP95;

		public double reconVramMB;

		public long reconKeyMismatch;

		public long reconNodeMismatch;

		public long reconKeysChecked;

		public long reconAabbChecks;

		public long reconSignedZero;

		public int reconPipeWaits;

		public double reconPipeWaitS;
	}

	public enum PipelineMode
	{
		Serial = 0,
		Parallel = 1
	}

	internal enum Kind
	{
		Rod = 0,
		Shell = 1,
		Jet = 2
	}

	internal sealed class RecBox
	{
		public Summary s;
	}

	internal sealed class RunData
	{
		public int n;

		public SuperBaker.Body[] bodies;

		public Vector3 gridLo;

		public Vector3 gridHi;

		public float spacing;

		public float[] v0;

		public float[] mass;

		public Vector3[] plate0;

		public double rodKe0;

		public float rodDiameterM;

		public static RunData FromFreshBake(SuperShot.KeyframeContext ctx, float rodDiameterM)
		{
			SuperParticles particles = ctx.baked.particles;
			RunData rd = new RunData();
			rd.n = particles.count;
			rd.bodies = ctx.baked.bodies.ToArray();
			rd.gridLo = ctx.gridLo;
			rd.gridHi = ctx.gridHi;
			rd.spacing = ctx.baked.spacing;
			rd.rodDiameterM = rodDiameterM;
			rd.v0 = new float[rd.n];
			rd.mass = new float[rd.n];
			for (int i = 0; i < rd.n; i++)
			{
				rd.v0[i] = particles.V0[i];
				rd.mass[i] = particles.mass[i];
			}
			SuperBaker.Body plate = rd.bodies[rd.bodies.Length - 1];
			rd.plate0 = new Vector3[plate.count];
			for (int j = 0; j < plate.count; j++)
			{
				rd.plate0[j] = particles.x[plate.start + j];
			}
			double ke = 0.0;
			SuperBaker.Body rod = rd.bodies[0];
			for (int k = 0; k < rod.count; k++)
			{
				int idx = rod.start + k;
				Vector3 v = particles.v[idx];
				ke += 0.5 * particles.mass[idx] * ((double)v.x * v.x + (double)v.y * v.y + (double)v.z * v.z);
			}
			rd.rodKe0 = ke;
			return rd;
		}
	}

	internal sealed class Snapshot
	{
		public readonly Vector3[] pos;

		public readonly Vector3[] vel;

		public readonly float[] dmg;

		public readonly float[][] pf;

		public readonly bool[] inside;

		public int index;

		public float simTimeS;

		internal SuperReconGpu.FrameHandle recon;

		public const int BytesPerParticle = 49;

		public Snapshot(int n)
		{
			pos = new Vector3[n];
			vel = new Vector3[n];
			dmg = new float[n];
			pf = new float[5][];
			for (int i = 0; i < 5; i++)
			{
				pf[i] = new float[n];
			}
			inside = new bool[n];
		}
	}

	internal sealed class FieldSource
	{
		public readonly Vector4[] scal;

		public readonly SuperMat3[] dev;

		public const int BytesPerParticle = 52;

		public FieldSource(int n)
		{
			scal = new Vector4[n];
			dev = new SuperMat3[n];
		}
	}

	internal struct FrameStats
	{
		public int frameVerts;

		public int frameRawVerts;

		public int lastFailedExcluded;

		public long gridCells;

		public long bytesLattice;

		public long bytesDensity;

		public long bytesDensityWork;

		public long bytesHash;

		public long bytesDebris;

		public long bytesMesh;

		public long bytesFields;

		public long bytesBodyMax;
	}

	private sealed class FrameTail
	{
		public readonly double[] kineticJ;

		public readonly double[] massInDomainKg;

		public readonly Vector3[] momentum;

		public double escapedMassKg;

		public int debrisCount;

		public int verdictSoFar;

		public Vector3[] debrisPos;

		public float[][] debrisFields;

		public FrameTail(int nBodies)
		{
			kineticJ = new double[nBodies];
			massInDomainKg = new double[nBodies];
			momentum = new Vector3[nBodies];
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass65_0
	{
		public FramePipeline pipe;

		public SuperTape.Writer tape;

		public SuperCheckpoint.Scene ckScene;

		public SuperEraDrive drive;

		public Summary s;

		public double lastCkptS;

		public Stopwatch sw;

		public Kind kind;

		public SuperJetSpec jet;

		public bool composite;

		public SuperShellSpec shell;

		public SuperRodSpec rod;

		public SuperCheckpoint.Data resume;

		public float cadenceS;

		public IList<SuperPlateSpec> plates;

		public byte[] decor;

		public PipelineMode mode;

		public Action onFrame;

		public Action onPump;

		public bool reconDecided;

		public SuperReconGpu recon;

		public SuperStepPacer pacer;

		public int wMin0;

		public int ioMin0;

		public Action<long> _003C_003E9__6;

		internal void _003CRecordCore_003Eb__6(long ticks)
		{
			s.stages.Add(SuperHookStages.Stage.ReconWait, ticks);
		}

		internal void _003CRecordCore_003Eg__EnqueueHooked_007C3(Snapshot sn, SuperShot.KeyframeContext c, long t, long spentTicks)
		{
			int frame = pipe.Enqueue(sn);
			s.stages.HookDone(s.stages.Lap(SuperHookStages.Stage.Enqueue, t) - t + spentTicks);
			bool flag = false;
			if (ckScene != null && c.step < c.stepsTotal)
			{
				if (SuperCheckpoint.EveryFrames > 0)
				{
					flag = frame % SuperCheckpoint.EveryFrames == 0;
				}
				else
				{
					flag = sw.Elapsed.TotalSeconds - lastCkptS >= (double)SuperCheckpoint.IntervalS;
				}
			}
			if (flag)
			{
				pipe.RequestCheckpoint(frame, c);
			}
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass65_1
	{
		public Task ckWrite;

		public int probeOrdinal;

		public RunData rd;

		public Snapshot serialSnap;

		public FieldSource fieldSrc;

		public Action<SuperShot.KeyframeContext> runCheckpoint;

		public Snapshot halfSn;

		public SuperShot.KeyframeContext halfCtx;

		public bool halfYielded;

		public Snapshot hookSn;

		public SuperShot.KeyframeContext hookCtx;

		public int hookStage;

		public long hookT0;

		public long hookTFields;

		public Action finishHalf;

		public SuperReconGpu.FrameHandle hookHandle;

		public _003C_003Ec__DisplayClass65_0 CS_0024_003C_003E8__locals1;

		internal void _003CRecordCore_003Eb__0(SuperShot.KeyframeContext ctx)
		{
			_003C_003Ec__DisplayClass65_0 displayClass = CS_0024_003C_003E8__locals1;
			if (displayClass == null || displayClass.ckScene == null || displayClass.s == null)
			{
				return;
			}
			bool due;
			if (SuperCheckpoint.EveryFrames > 0)
			{
				due = displayClass.s.frames % SuperCheckpoint.EveryFrames == 0;
			}
			else
			{
				due = displayClass.sw.Elapsed.TotalSeconds - displayClass.lastCkptS >= (double)SuperCheckpoint.IntervalS;
			}
			if (!due)
			{
				return;
			}
			if (ckWrite != null)
			{
				if (!ckWrite.IsCompleted)
				{
					return;
				}
				ckWrite = null;
			}
			displayClass.lastCkptS = displayClass.sw.Elapsed.TotalSeconds;
			try
			{
				long tapeBytes = ((displayClass.tape != null) ? displayClass.tape.Tell() : 0L);
				SuperCheckpoint.Data data = SuperCheckpoint.Capture(ctx, displayClass.ckScene, displayClass.drive, tapeBytes, displayClass.s.frames, displayClass.s.maxVertsFrame, displayClass.s.maxRawVertsFrame);
				ckWrite = Task.Run(delegate
				{
					SuperCheckpoint.Write(data);
				});
			}
			catch (Exception exception)
			{
				Debug.LogWarning("[лента] контрольная точка не записана: " + exception.Message);
			}
		}

		internal void _003CRecordCore_003Eb__1(SuperShot.KeyframeContext ctx)
		{
			_003C_003Ec__DisplayClass65_0 displayClass = CS_0024_003C_003E8__locals1;
			if (displayClass == null || ctx.kernel == null || ctx.baked == null || ctx.baked.particles == null)
			{
				return;
			}
			if (rd == null)
			{
				float rodDia;
				if (displayClass.kind == Kind.Jet)
				{
					rodDia = displayClass.jet.diameterM;
				}
				else if (displayClass.kind == Kind.Shell)
				{
					rodDia = displayClass.shell.diameterM;
				}
				else
				{
					rodDia = displayClass.rod.diameterM;
				}
				rd = RunData.FromFreshBake(ctx, rodDia);
				serialSnap = new Snapshot(rd.n);
				fieldSrc = new FieldSource(rd.n);
				displayClass.s.particles = rd.n;
				if (displayClass.tape == null)
				{
					int bodyCount = ctx.baked.bodies.Count;
					string[] bodyNames = new string[bodyCount];
					for (int i = 0; i < bodyCount; i++)
					{
						bodyNames[i] = ctx.baked.bodies[i].name ?? "?";
					}
					string scene = Path.GetFileNameWithoutExtension(displayClass.s.path);
					displayClass.tape = new SuperTape.Writer(displayClass.s.path, displayClass.cadenceS, scene, bodyNames, SuperReconstruct.FieldNames, SuperReconstruct.FieldUnits, displayClass.decor);
				}
			}
			Snapshot snapshot = serialSnap;
			snapshot.index = displayClass.s.frames;
			snapshot.simTimeS = ctx.simTimeS;
			ctx.kernel.Download(snapshot.pos, snapshot.vel, fieldSrc.scal);
			ctx.kernel.DownloadDeviator(fieldSrc.dev);
			ctx.kernel.DownloadDamage(snapshot.dmg);
			SuperReconstruct.ParticleFields(rd.n, snapshot.vel, fieldSrc.scal, fieldSrc.dev, snapshot.dmg, 0, snapshot.pf);
			SuperTape.Frame frame = BuildFrame(rd, snapshot, 1, displayClass.s.stages, out FrameStats stats);
			ApplyStats(displayClass.s, stats);
			SuperTape.Encoded encoded = displayClass.tape.EncodeFrame(frame);
			displayClass.tape.WriteEncoded(encoded);
			displayClass.s.frames++;
			displayClass.s.bytesFramesCounted++;
			displayClass.s.bytesLatticeMean += (double)stats.bytesLattice;
			displayClass.s.bytesMeshMean += (double)stats.bytesMesh;
			displayClass.s.bytesFieldsMean += (double)stats.bytesFields;
			displayClass.s.bytesPayloadMean += (double)encoded.canonicalLen;
			displayClass.s.bytesHonestMean += (double)((long)rd.n * 57 + stats.bytesLattice + stats.bytesMesh + stats.bytesFields + stats.bytesDebris + encoded.canonicalLen);
			displayClass.s.fileBytes = displayClass.tape.Tell();
			if (stats.bytesLattice > displayClass.s.bytesLatticeMax)
			{
				displayClass.s.bytesLatticeMax = stats.bytesLattice;
			}
			if (stats.bytesMesh > displayClass.s.bytesMeshMax)
			{
				displayClass.s.bytesMeshMax = stats.bytesMesh;
			}
			if (stats.bytesFields > displayClass.s.bytesFieldsMax)
			{
				displayClass.s.bytesFieldsMax = stats.bytesFields;
			}
			if (encoded.canonicalLen > displayClass.s.bytesPayloadMax)
			{
				displayClass.s.bytesPayloadMax = encoded.canonicalLen;
			}
			if (displayClass.s.frames % 256 == 0)
			{
				displayClass.tape.FlushAndTell();
			}
			displayClass.onFrame?.Invoke();
			runCheckpoint?.Invoke(ctx);
		}

		internal void _003CRecordCore_003Eb__2()
		{
			Task task = ckWrite;
			if (task != null)
			{
				try
				{
					task.Wait();
				}
				catch (Exception exception)
				{
					Debug.LogWarning("[лента] запись контрольной точки завершилась ошибкой: " + exception.Message);
				}
				ckWrite = null;
			}
		}

		internal bool _003CRecordCore_003Eb__4()
		{
			return true;
		}
	}

	[CompilerGenerated]
	private sealed class _003CRecordCore_003Ed__65 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public SuperEraDrive drive;

		public Kind kind;

		public SuperJetSpec jet;

		public SuperShellSpec shell;

		public SuperRodSpec rod;

		public SuperCheckpoint.Data resume;

		public float cadenceS;

		public IList<SuperPlateSpec> plates;

		public byte[] decor;

		public Action onFrame;

		public Action onPump;

		public string dir;

		public string tapeName;

		public bool gameDomain;

		public PipelineMode? pipeline;

		public bool? paced;

		public RecBox box;

		public float marginM;

		public bool checkpoint;

		public float spacing;

		public float durationS;

		public ComputeShader cs;

		private _003C_003Ec__DisplayClass65_1 _003C_003E8__1;

		private _003C_003Ec__DisplayClass65_0 _003C_003E8__2;

		private bool _003CsavedGameDomain_003E5__2;

		private float _003CsavedMargin_003E5__3;

		private IEnumerator _003Cshot_003E5__4;

		private SuperShot.RunBox _003CshotBox_003E5__5;

		private Stopwatch _003CdrainSw_003E5__6;

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
		public _003CRecordCore_003Ed__65(int _003C_003E1__state)
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
		}

		private static readonly ConditionalWeakTable<_003CRecordCore_003Ed__65, IEnumerator> _003Cinners = new ConditionalWeakTable<_003CRecordCore_003Ed__65, IEnumerator>();

		private bool MoveNext()
		{
			if (_003C_003E1__state == -1)
			{
				return false;
			}
			if (!_003Cinners.TryGetValue(this, out IEnumerator value))
			{
				value = _003CRecordCoreBody(this);
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

		private static IEnumerator _003CRecordCoreBody(_003CRecordCore_003Ed__65 d)
		{
			_003C_003Ec__DisplayClass65_0 c0 = new _003C_003Ec__DisplayClass65_0();
			c0.drive = d.drive;
			c0.kind = d.kind;
			c0.jet = d.jet;
			c0.shell = d.shell;
			c0.rod = d.rod;
			c0.resume = d.resume;
			c0.cadenceS = d.cadenceS;
			c0.plates = d.plates;
			c0.decor = d.decor;
			c0.mode = (d.pipeline ?? (d.gameDomain ? GamePipeline : Pipeline));
			c0.onFrame = d.onFrame;
			c0.onPump = d.onPump;
			c0.composite = d.kind == Kind.Shell;
			c0.s = new Summary();
			c0.s.path = Path.GetFullPath(Path.Combine(d.dir ?? TapeDir, d.tapeName));
			c0.s.baseFrames = ((d.resume != null) ? d.resume.frames : 0);
			c0.s.frames = c0.s.baseFrames;
			c0.s.pipeline = c0.mode;
			c0.s.workers = WorkersOf(c0.mode);
			c0.s.stages = new SuperHookStages();
			c0.s.fieldsPath = "cpu";
			c0.s.fieldsPathWhy = "серийная запись кадров";
			c0.sw = Stopwatch.StartNew();
			c0.lastCkptS = 0.0;
			c0.wMin0 = 0;
			c0.ioMin0 = 0;
			if (d.paced ?? (d.gameDomain ? GamePaced : Paced))
			{
				c0.pacer = new SuperStepPacer();
			}
			c0.s.pacer = c0.pacer;
			if (d.checkpoint)
			{
				SuperCheckpoint.Scene scene = new SuperCheckpoint.Scene();
				scene.kind = (int)d.kind;
				scene.rod = d.rod;
				scene.jet = d.jet;
				scene.plates = new List<SuperPlateSpec>(d.plates);
				scene.spacing = d.spacing;
				scene.durationS = d.durationS;
				scene.marginM = d.marginM;
				scene.cadenceS = d.cadenceS;
				scene.tapeTmpPath = c0.s.path;
				scene.tapeDir = Path.GetDirectoryName(c0.s.path);
				scene.tapeName = Path.GetFileName(c0.s.path);
				scene.drives = new SuperCheckpoint.DriveSpec[0];
				scene.probeEveryS = SuperEraDrive.ProbeEveryS;
				c0.ckScene = scene;
			}
			d._003C_003E8__2 = c0;
			_003C_003Ec__DisplayClass65_1 c1 = new _003C_003Ec__DisplayClass65_1();
			c1.CS_0024_003C_003E8__locals1 = c0;
			c1.runCheckpoint = c1._003CRecordCore_003Eb__0;
			c1.finishHalf = c1._003CRecordCore_003Eb__2;
			d._003C_003E8__1 = c1;
			SuperShot.RunBox shotBox = new SuperShot.RunBox();
			shotBox.abortFrame = -1;
			shotBox.abortedAtStep = -1;
			d._003CshotBox_003E5__5 = shotBox;
			SuperShot.Resume resume = ((d.resume != null) ? SuperCheckpoint.ToResume(d.resume) : null);
			SuperDecor superDecor = ((d.decor != null) ? SuperDecor.Decode(d.decor) : null);
			d._003Cshot_003E5__4 = SuperShot.RunCore(d.cs, d.rod, d.shell, d.jet, (SuperShot.Proj)(int)d.kind, d.plates, d.spacing, d.durationS, SuperScatterRoute.FixedPointAtomics, 0, c1._003CRecordCore_003Eb__1, Mathf.Max(MinCadenceS, d.cadenceS), c0.drive, null, resume, shotBox, c1._003CRecordCore_003Eb__4, c0.pacer, superDecor);
			bool savedGameDomain = SuperShot.GameSolverDomain;
			SuperShot.GameSolverDomain = d.gameDomain;
			try
			{
				while (d._003Cshot_003E5__4.MoveNext())
				{
					c0.s.stepNow = shotBox.stepNow;
					c0.s.stepsTotal = shotBox.stepsTotal;
					d.onPump?.Invoke();
					yield return null;
				}
			}
			finally
			{
				SuperShot.GameSolverDomain = savedGameDomain;
			}
			c1.finishHalf?.Invoke();
			if (c0.tape != null)
			{
				c0.tape.FlushToDisk();
				c0.tape.Dispose();
			}
			c0.s.shot = shotBox.r;
			if (shotBox.r.particles > 0)
			{
				c0.s.particles = shotBox.r.particles;
			}
			c0.s.ok = shotBox.r.ok;
			c0.s.failure = shotBox.r.failure;
			c0.s.failureKey = shotBox.r.failureKey;
			c0.s.failureArgs = shotBox.r.failureArgs;
			c0.s.failClass = shotBox.r.failClass;
			c0.s.wallS = c0.sw.Elapsed.TotalSeconds;
			c0.s.stepsTotal = shotBox.stepsTotal;
			c0.s.stepNow = shotBox.stepNow;
			c0.s.framesBuilt = c0.s.frames - c0.s.baseFrames;
			if (c0.s.bytesFramesCounted > 0)
			{
				c0.s.bytesLatticeMean /= (double)c0.s.bytesFramesCounted;
				c0.s.bytesMeshMean /= (double)c0.s.bytesFramesCounted;
				c0.s.bytesFieldsMean /= (double)c0.s.bytesFramesCounted;
				c0.s.bytesPayloadMean /= (double)c0.s.bytesFramesCounted;
				c0.s.bytesHonestMean /= (double)c0.s.bytesFramesCounted;
			}
			try
			{
				c0.s.fileBytes = (File.Exists(c0.s.path) ? new FileInfo(c0.s.path).Length : 0L);
			}
			catch (Exception)
			{
				c0.s.fileBytes = 0L;
			}
			LastSummary = c0.s;
			d.box.s = c0.s;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			IEnumerator enumerator = _003Cshot_003E5__4;
			if (enumerator is IDisposable disposable)
			{
				disposable.Dispose();
			}
			_003C_003Ec__DisplayClass65_0 displayClass = _003C_003E8__2;
			if (displayClass?.tape != null)
			{
				try
				{
					displayClass.tape.FlushToDisk();
					displayClass.tape.Dispose();
				}
				catch (Exception)
				{
				}
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	public static int ForceK;

	public static int ForceDop;

	public static int[] ForceDopCycle;

	public static int ForceQ;

	public static bool KRaiseNeedsHeadroom;

	public static double TapeFlushEveryS;

	private static int _liveSnapshots;

	private static int _liveTasks;

	private static int _building;

	public static int PipelineInFlight;

	public static int PipelineReadyUnwritten;

	public static int PipelineQueued;

	public static int PipelineK;

	public static int PipelineDop;

	public static bool SabotageNoBackpressure;

	public const float CadenceS = 2E-06f;

	public const float MinCadenceS = 2E-07f;

	public const int DebrisCap = 4194303;

	public static float VelocityCeilingMps;

	public static int DegenerateFrame;

	public static string DegenerateWhat;

	internal static readonly object DegenerateLock;

	public static bool AbortOnDegenerateFrame;

	public static float SabotageAbortCeilingMps;

	public const string AbortKey = "super.fail.degenerate";

	public static int DegenerateParticle;

	public static int DegenerateBody;

	public static string DegenerateBodyName;

	public static float DegenerateSpeedMps;

	public static float DegenerateCeilingMps;

	public static Vector3 DegenerateWhere;

	public static Action<SuperShot.KeyframeContext, int> DebugKeyframeProbe;

	public static Action<SuperShot.FinalState> TestOnFinal;

	public static int SabotageThrowAtFrame;

	public static PipelineMode Pipeline;

	public static PipelineMode GamePipeline;

	public static bool Paced;

	public static bool GamePaced;

	static SuperTapeRecorder()
	{
		ForceQ = -1;
		KRaiseNeedsHeadroom = false;
		TapeFlushEveryS = 2.0;
		SabotageNoBackpressure = false;
		DegenerateFrame = -1;
		DegenerateLock = new object();
		AbortOnDegenerateFrame = true;
		SabotageAbortCeilingMps = 0f;
		DegenerateParticle = -1;
		DegenerateBody = -1;
		SabotageThrowAtFrame = -1;
		Pipeline = PipelineMode.Serial;
		GamePipeline = PipelineMode.Parallel;
		Paced = false;
		GamePaced = true;
	}

	public static int LiveSnapshots => _liveSnapshots;

	public static int LiveTasks => _liveTasks;

	public static int PipelineBuilding => _building;

	public static string TapeDir => Path.Combine(Application.persistentDataPath, "SuperTapes");

	public static string FixtureDir => Path.Combine(TapeDir, "fixtures");

	public static Summary LastSummary { get; private set; }

	public static long PerFrameBytes(int n, long reconstructionCells, long rawVerts, long payloadBytes, int dop)
	{
		return (long)n * 57 + (reconstructionCells + rawVerts * 6) * 16 + ((dop >= 3) ? ((long)n * 56) : 0) + payloadBytes;
	}

	public static int PipelineVersionOf(PipelineMode m, bool paced = false)
	{
		if (m == PipelineMode.Parallel)
		{
			return paced ? 3 : 1;
		}
		return 0;
	}

	public static int WorkersOf(PipelineMode m)
	{
		if (m == PipelineMode.Parallel)
		{
			return Math.Max(1, Environment.ProcessorCount - 2);
		}
		return 0;
	}

	public static Summary Record(ComputeShader cs, SuperRodSpec rod, SuperPlateSpec plate, float spacing, float durationS, string tapeName, float marginM = 0f, float cadenceS = 2E-06f, string dir = null)
	{
		return Record(cs, rod, default(SuperShellSpec), default(SuperJetSpec), Kind.Rod, new SuperPlateSpec[1] { plate }, spacing, durationS, tapeName, marginM, cadenceS, dir, null, null);
	}

	public static Summary Record(ComputeShader cs, SuperShellSpec shell, IList<SuperPlateSpec> plates, float spacing, float durationS, string tapeName, float marginM = 0f, float cadenceS = 2E-06f, string dir = null, Action onFrame = null, SuperEraDrive drive = null)
	{
		return Record(cs, default(SuperRodSpec), shell, default(SuperJetSpec), Kind.Shell, plates, spacing, durationS, tapeName, marginM, cadenceS, dir, onFrame, drive);
	}

	public static Summary Record(ComputeShader cs, SuperRodSpec rod, IList<SuperPlateSpec> plates, float spacing, float durationS, string tapeName, float marginM = 0f, float cadenceS = 2E-06f, string dir = null, Action onFrame = null, SuperEraDrive drive = null, bool checkpoint = false, SuperCheckpoint.Data resume = null, byte[] decor = null)
	{
		return Record(cs, rod, default(SuperShellSpec), default(SuperJetSpec), Kind.Rod, plates, spacing, durationS, tapeName, marginM, cadenceS, dir, onFrame, drive, checkpoint, resume, decor);
	}

	public static Summary Record(ComputeShader cs, SuperJetSpec jet, IList<SuperPlateSpec> plates, float spacing, float durationS, string tapeName, float marginM = 0f, float cadenceS = 2E-06f, string dir = null, Action onFrame = null, SuperEraDrive drive = null, bool checkpoint = false, SuperCheckpoint.Data resume = null)
	{
		return Record(cs, default(SuperRodSpec), default(SuperShellSpec), jet, Kind.Jet, plates, spacing, durationS, tapeName, marginM, cadenceS, dir, onFrame, drive, checkpoint, resume);
	}

	private static Summary Record(ComputeShader cs, SuperRodSpec rod, SuperShellSpec shell, SuperJetSpec jet, Kind kind, IList<SuperPlateSpec> plates, float spacing, float durationS, string tapeName, float marginM, float cadenceS, string dir, Action onFrame, SuperEraDrive drive, bool checkpoint = false, SuperCheckpoint.Data resume = null, byte[] decor = null)
	{
		if (resume != null)
		{
			string refusal = SuperSolveJob.ResumeRefusal(resume);
			if (refusal != null)
			{
				throw new InvalidOperationException(refusal);
			}
		}
		RecBox box = new RecBox();
		IEnumerator iterator = RecordCore(cs, rod, shell, jet, kind, plates, spacing, durationS, tapeName, marginM, cadenceS, dir, onFrame, drive, checkpoint, resume, box, null, null, null, gameDomain: false, decor);
		while (iterator.MoveNext())
		{
		}
		return box.s;
	}

	[IteratorStateMachine(typeof(_003CRecordCore_003Ed__65))]
	internal static IEnumerator RecordCore(ComputeShader cs, SuperRodSpec rod, SuperShellSpec shell, SuperJetSpec jet, Kind kind, IList<SuperPlateSpec> plates, float spacing, float durationS, string tapeName, float marginM, float cadenceS, string dir, Action onFrame, SuperEraDrive drive, bool checkpoint, SuperCheckpoint.Data resume, RecBox box, Action onPump = null, PipelineMode? pipeline = null, bool? paced = null, bool gameDomain = false, byte[] decor = null)
	{
		_003CRecordCore_003Ed__65 d = new _003CRecordCore_003Ed__65(0);
		d.cs = cs;
		d.rod = rod;
		d.shell = shell;
		d.jet = jet;
		d.kind = kind;
		d.plates = plates;
		d.spacing = spacing;
		d.durationS = durationS;
		d.tapeName = tapeName;
		d.marginM = marginM;
		d.cadenceS = cadenceS;
		d.dir = dir;
		d.onFrame = onFrame;
		d.drive = drive;
		d.checkpoint = checkpoint;
		d.resume = resume;
		d.box = box;
		d.onPump = onPump;
		d.pipeline = pipeline;
		d.paced = paced;
		d.gameDomain = gameDomain;
		d.decor = decor;
		return d;
	}

	private static string DescribePlates(IList<SuperPlateSpec> plates)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < plates.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(" + ");
			}
			SuperPlateSpec p = plates[i];
			stringBuilder.Append(string.Format("{0} {1:F0} мм β={2:F0}°", p.material, p.thicknessM * 1000f, p.angleXDeg));
			if (p.hasPose)
			{
				stringBuilder.Append(string.Format(" φ={0:F0}° ", SuperScenePose.CutAzimuthDeg(p.poseN)) + string.Format("Δ=({0:F0}, {1:F0}) мм", p.poseCentreM.x * 1000f, p.poseCentreM.y * 1000f));
			}
			stringBuilder.Append(p.clampedEdges ? " защ." : " своб.");
		}
		return stringBuilder.ToString();
	}

	private static void CopyReconStats(SuperReconGpu r, Summary s)
	{
		SuperReconGpu.ReconStats st = r.Stats;
		s.reconReadback = st.readback;
		s.reconSlots = st.slots;
		s.reconSlotWaits = st.slotWaits;
		s.reconFramesGpu = st.framesGpu;
		s.reconVertexRequests = st.vertexRequests;
		s.reconSlotWaitS = st.slotWaitS;
		s.reconHookMsMean = st.hookMsMean;
		s.reconHookMsMax = st.hookMsMax;
		s.reconHookGpuMs = st.hookGpuMsSum;
		s.reconReadbackMsP95 = st.readbackMsP95;
		s.reconVramMB = st.vramMB;
		s.reconKeyMismatch = st.keyMismatch;
		s.reconNodeMismatch = st.nodeMismatch;
		s.reconKeysChecked = st.keysChecked;
		s.reconAabbChecks = st.aabbChecks;
		s.reconSignedZero = st.signedZeroDiffs;
		s.reconPipeWaits = st.pipeWaits;
		s.reconPipeWaitS = st.pipeWaitS;
	}

	private static string ReconPassport(Summary s)
	{
		if (s.fieldsPath == "gpu")
		{
			return string.Format(" [поля: GPU, чтение {0}, слотов {1}, ждал слот {2} раз ({3:F1} с), стоял по K {4} раз ({5:F1} с), ", s.reconReadback, s.reconSlots, s.reconSlotWaits, s.reconSlotWaitS, s.reconPipeWaits, s.reconPipeWaitS) + string.Format("крюк {0:F0}/{1:F0} мс, чтение p95 {2:F0} мс, VRAM {3:F0} МБ, ", s.reconHookMsMean, s.reconHookMsMax, s.reconReadbackMsP95, s.reconVramMB) + string.Format("ключей мимо float-клетки {0} из {1}, узлов {2}]", s.reconKeyMismatch, s.reconKeysChecked, s.reconNodeMismatch);
		}
		return " [поля: ЦП — " + s.fieldsPathWhy + "]";
	}

	private static void ApplyStats(Summary s, FrameStats st)
	{
		SuperReconstruct.NoteFailedExcluded(st.lastFailedExcluded);
		if (st.frameVerts > s.maxVertsFrame)
		{
			s.maxVertsFrame = st.frameVerts;
		}
		if (st.frameRawVerts > s.maxRawVertsFrame)
		{
			s.maxRawVertsFrame = st.frameRawVerts;
		}
	}

	internal static SuperTape.Frame BuildFrame(RunData rd, Snapshot sn, int dop, SuperHookStages stages, out FrameStats stats)
	{
		if (SabotageThrowAtFrame >= 0 && sn.index == SabotageThrowAtFrame)
		{
			throw new InvalidOperationException("S25 диверсия: бросок при построении кадра " + sn.index);
		}
		long t0 = SuperHookStages.Now;
		int count = rd.n;
		Vector3[] pos = sn.pos;
		Vector3[] vel = sn.vel;
		float[] dmgArr = sn.dmg;
		Vector3 gridLo = rd.gridLo;
		Vector3 gridHi = rd.gridHi;
		float[][] pf = sn.pf;
		bool[] inside = sn.inside;
		int nBodies = rd.bodies.Length;
		SuperTape.Frame frame = new SuperTape.Frame();
		frame.index = sn.index;
		frame.simTimeS = sn.simTimeS;
		frame.bodies = new SuperTape.BodyFrame[nBodies];
		frame.telemetry = new SuperTape.BodyTelemetry[nBodies];
		frame.fieldMin = new float[5];
		frame.fieldMax = new float[5];
		frame.debrisCount = 0;
		int FC = 5;
		if (dop > 1)
		{
			int per = (count + dop * 4 - 1) / (dop * 4);
			float[] cmn = new float[per * FC];
			float[] cmx = new float[per * FC];
			SuperParallel.For(0, dop * 4, dop, delegate(int ch)
			{
				int from = Math.Min(count, ch * per);
				int to = Math.Min(count, (ch + 1) * per);
				InsideAndRange(from, to, pos, gridLo, gridHi, inside, pf, cmn, cmx, FC * ch);
			});
			for (int ch = 0; ch < FC; ch++)
			{
				float mn = float.MaxValue;
				float mx = float.MinValue;
				for (int j = 0; j < per; j++)
				{
					float v = cmn[j * FC + ch];
					if (v < mn)
					{
						mn = v;
					}
					v = cmx[j * FC + ch];
					if (v > mx)
					{
						mx = v;
					}
				}
				frame.fieldMin[ch] = mn;
				frame.fieldMax[ch] = mx;
			}
		}
		else
		{
			for (int i = 0; i < count; i++)
			{
				Vector3 v2 = pos[i];
				inside[i] = v2.x >= gridLo.x && v2.y >= gridLo.y && v2.z >= gridLo.z && gridHi.x >= v2.x && gridHi.y >= v2.y && gridHi.z >= v2.z;
			}
			for (int ch2 = 0; ch2 < 5; ch2++)
			{
				float mn2 = float.MaxValue;
				float mx2 = float.MinValue;
				for (int i2 = 0; i2 < count; i2++)
				{
					if (inside[i2])
					{
						float v3 = pf[ch2][i2];
						if (v3 < mn2)
						{
							mn2 = v3;
						}
						if (v3 > mx2)
						{
							mx2 = v3;
						}
					}
				}
				frame.fieldMin[ch2] = mn2;
				frame.fieldMax[ch2] = mx2;
			}
		}
		stages.Lap(SuperHookStages.Stage.Fields, t0);
		FrameTail tail = new FrameTail(nBodies);
		Task tailTask = ((dop >= 2) ? Task.Factory.StartNew(delegate
		{
			ComputeTail(rd, pos, vel, dmgArr, pf, inside, count, nBodies, sn.index, stages, tail);
		}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default) : null);
		SuperReconstruct.Body[] recs = new SuperReconstruct.Body[nBodies];
		if (sn.recon != null && dop < 1)
		{
			throw new InvalidOperationException("S39: путь полей GPU — только у конвейера (dop ⩾ 1)");
		}
		if (dop > 1)
		{
			SuperParallel.For(0, nBodies, dop, delegate(int b)
			{
				recs[b] = SuperReconstruct.Reconstruct(rd.bodies[b], pos, rd.v0, pf, gridLo, gridHi, rd.spacing, true, dmgArr, stages, dop, sn.recon, b);
			});
		}
		else
		{
			for (int b2 = 0; b2 < nBodies; b2++)
			{
				recs[b2] = SuperReconstruct.Reconstruct(rd.bodies[b2], pos, rd.v0, pf, gridLo, gridHi, rd.spacing, true, dmgArr, stages, dop, sn.recon, b2);
			}
		}
		long t1 = SuperHookStages.Now;
		if (tailTask == null)
		{
			ComputeTail(rd, pos, vel, dmgArr, pf, inside, count, nBodies, sn.index, stages, tail);
		}
		else
		{
			tailTask.Wait();
		}
		stats = default(FrameStats);
		for (int b3 = 0; b3 < nBodies; b3++)
		{
			SuperReconstruct.Body r = recs[b3];
			stats.frameVerts += r.vertices.Length;
			stats.frameRawVerts += r.rawVertexCount;
			stats.lastFailedExcluded += r.failedExcluded;
			stats.gridCells += r.gridCells;
			long lat = r.bytesIdx + r.bytesHash + r.bytesDensityWork + r.bytesDensity;
			stats.bytesLattice += lat;
			long mesh = r.bytesMesh + ((long)r.vertices.Length * 3 + r.triangles.Length) * 4;
			stats.bytesMesh += mesh;
			long flds = r.bytesBlockFields + r.bytesVertexFields;
			stats.bytesFields += flds;
			stats.bytesDensity += r.bytesDensity;
			stats.bytesDensityWork += r.bytesDensityWork;
			stats.bytesHash += r.bytesHash;
			if (flds + mesh + lat > stats.bytesBodyMax)
			{
				stats.bytesBodyMax = flds + mesh + lat;
			}
			Vector3 mn3 = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			Vector3 mx3 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			Vector3[] verts = r.vertices;
			for (int i3 = 0; i3 < verts.Length; i3++)
			{
				Vector3 v4 = verts[i3];
				if (v4.x <= mn3.x)
				{
					mn3.x = v4.x;
				}
				if (v4.y <= mn3.y)
				{
					mn3.y = v4.y;
				}
				if (v4.z <= mn3.z)
				{
					mn3.z = v4.z;
				}
				if (v4.x >= mx3.x)
				{
					mx3.x = v4.x;
				}
				if (v4.y >= mx3.y)
				{
					mx3.y = v4.y;
				}
				if (v4.z >= mx3.z)
				{
					mx3.z = v4.z;
				}
			}
			if (verts.Length == 0)
			{
				mn3 = Vector3.zero;
				mx3 = Vector3.zero;
			}
			SuperTape.BodyFrame bf = new SuperTape.BodyFrame();
			bf.aabbLo = mn3;
			bf.aabbHi = mx3;
			bf.vertices = r.vertices;
			bf.triangles = r.triangles;
			bf.fields = r.fields;
			bf.blockLo = r.blockLo;
			bf.blockCell = r.blockCell;
			bf.bnx = r.bnx;
			bf.bny = r.bny;
			bf.bnz = r.bnz;
			bf.blockFields = r.blockFields;
			frame.bodies[b3] = bf;
			SuperTape.BodyTelemetry tel = default(SuperTape.BodyTelemetry);
			tel.kineticJ = tail.kineticJ[b3];
			tel.momentum = tail.momentum[b3];
			tel.massInDomainKg = tail.massInDomainKg[b3];
			tel.particleVolumeM3 = r.particleVolumeM3;
			frame.telemetry[b3] = tel;
		}
		frame.escapedMassKg = tail.escapedMassKg;
		stages.Lap(SuperHookStages.Stage.Telemetry, t1);
		frame.debrisCount = tail.debrisCount;
		frame.debrisPos = tail.debrisPos;
		frame.debrisFields = tail.debrisFields;
		frame.verdictSoFar = tail.verdictSoFar;
		stats.bytesDebris = (long)frame.debrisCount * 64;
		return frame;
	}

	private static void ComputeTail(RunData rd, Vector3[] pos, Vector3[] vel, float[] dmgArr, float[][] pf, bool[] inside, int count, int nBodies, int frameIndex, SuperHookStages stages, FrameTail t)
	{
		long t0 = SuperHookStages.Now;
		KahanSum escapedMass = default(KahanSum);
		for (int b = 0; b < nBodies; b++)
		{
			SuperBaker.Body body = rd.bodies[b];
			KahanSum ke = default(KahanSum);
			KahanSum massDom = default(KahanSum);
			KahanSum mx = default(KahanSum);
			KahanSum my = default(KahanSum);
			KahanSum mz = default(KahanSum);
			for (int i = body.start; i < body.start + body.count; i++)
			{
				if (inside[i])
				{
					float m = rd.mass[i];
					Vector3 v = vel[i];
					massDom.Add(m);
					ke.Add(0.5 * (double)m * ((double)v.x * v.x + (double)v.y * v.y + (double)v.z * v.z));
					mx.Add((double)v.x * m);
					my.Add((double)v.y * m);
					mz.Add((double)v.z * m);
				}
				else
				{
					escapedMass.Add(rd.mass[i]);
				}
			}
			t.kineticJ[b] = ke.Value;
			t.momentum[b] = new Vector3((float)mx.Value, (float)my.Value, (float)mz.Value);
			t.massInDomainKg[b] = massDom.Value;
		}
		t.escapedMassKg = escapedMass.Value;
		long lapT = stages.Lap(SuperHookStages.Stage.Telemetry, t0);
		int escapedCount = 0;
		for (int j = 0; j < count; j++)
		{
			if (dmgArr[j] >= 1f && inside[j])
			{
				escapedCount++;
			}
		}
		int stride = 1;
		if (escapedCount > DebrisCap)
		{
			stride = (escapedCount + DebrisCap - 1) / DebrisCap;
		}
		int saved = 0;
		List<Vector3> debrisPosList = new List<Vector3>(Math.Min(escapedCount, DebrisCap));
		List<int> debrisIdx = new List<int>(Math.Min(escapedCount, DebrisCap));
		int k = 0;
		for (int l = 0; l < count; l++)
		{
			if (dmgArr[l] >= 1f && inside[l])
			{
				if (k % stride == 0)
				{
					debrisPosList.Add(pos[l]);
					debrisIdx.Add(l);
					saved++;
				}
				k++;
			}
		}
		t.debrisCount = saved;
		t.debrisPos = debrisPosList.ToArray();
		t.debrisFields = new float[5][];
		for (int f = 0; f < 5; f++)
		{
			t.debrisFields[f] = new float[saved];
		}
		for (int f2 = 0; f2 < 5; f2++)
		{
			for (int d = 0; d < saved; d++)
			{
				t.debrisFields[f2][d] = pf[f2][debrisIdx[d]];
			}
		}
		if (stride > 1 && frameIndex % 25 == 0)
		{
			Debug.Log(string.Concat(new string[9] { "[лента] кадр ", frameIndex.ToString(), ": обломков ", escapedCount.ToString(), ", сохранено ", saved.ToString(), " (шаг подвыборки ", stride.ToString(), ")" }));
		}
		lapT = stages.Lap(SuperHookStages.Stage.Debris, lapT);
		float ceiling = ((SabotageAbortCeilingMps > 0f) ? SabotageAbortCeilingMps : VelocityCeilingMps);
		if (ceiling > 0f)
		{
			float vmaxSq = 0f;
			int imax = -1;
			for (int i2 = 0; i2 < count; i2++)
			{
				Vector3 v2 = vel[i2];
				float sq = v2.x * v2.x + v2.y * v2.y + v2.z * v2.z;
				if (sq > vmaxSq)
				{
					vmaxSq = sq;
					imax = i2;
				}
			}
			if (vmaxSq > ceiling * ceiling)
			{
				int maxBody = -1;
				for (int b2 = 0; b2 < nBodies; b2++)
				{
					if (imax >= rd.bodies[b2].start && imax < rd.bodies[b2].start + rd.bodies[b2].count)
					{
						maxBody = b2;
						break;
					}
				}
				lock (DegenerateLock)
				{
					if (DegenerateFrame < 0 || frameIndex < DegenerateFrame)
					{
						DegenerateFrame = frameIndex;
						DegenerateWhat = string.Format("кадр {0}: частица {1} (тело {2} «{3}»), ", frameIndex, imax, maxBody, (maxBody >= 0) ? ((object)rd.bodies[maxBody].material) : "?") + string.Format("|v| = {0:F0} м/с при потолке {1:F0} м/с, положение {2}", Mathf.Sqrt(vmaxSq), ceiling, pos[imax]);
						DegenerateParticle = imax;
						DegenerateBody = maxBody;
						DegenerateBodyName = ((maxBody >= 0) ? rd.bodies[maxBody].material.ToString() : "?");
						DegenerateSpeedMps = Mathf.Sqrt(vmaxSq);
						DegenerateCeilingMps = ceiling;
						DegenerateWhere = pos[imax];
					}
				}
			}
		}
		t.verdictSoFar = (int)SuperOutcome.Classify(pos, vel, rd.mass, rd.v0, rd.bodies[0], rd.bodies[nBodies - 1], rd.rodDiameterM, rd.rodKe0, rd.plate0).verdict;
		stages.Lap(SuperHookStages.Stage.Classify, lapT);
	}

	private static void InsideAndRange(int from, int to, Vector3[] pos, Vector3 gridLo, Vector3 gridHi, bool[] inside, float[][] pf, float[] outMin, float[] outMax, int outBase)
	{
		for (int i = from; i < to; i++)
		{
			Vector3 v = pos[i];
			bool flag = v.x >= gridLo.x && v.y >= gridLo.y && v.z >= gridLo.z && gridHi.x >= v.x && gridHi.y >= v.y && gridHi.z >= v.z;
			inside[i] = flag;
		}
		for (int ch = 0; ch < 5; ch++)
		{
			float mn = float.MaxValue;
			float mx = float.MinValue;
			for (int i2 = from; i2 < to; i2++)
			{
				if (inside[i2])
				{
					float v2 = pf[ch][i2];
					if (v2 < mn)
					{
						mn = v2;
					}
					if (v2 > mx)
					{
						mx = v2;
					}
				}
			}
			outMin[outBase + ch] = mn;
			outMax[outBase + ch] = mx;
		}
	}
}
