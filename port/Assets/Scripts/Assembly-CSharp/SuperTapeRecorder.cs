using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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
			return null;
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
		}
	}

	internal sealed class FieldSource
	{
		public readonly Vector4[] scal;

		public readonly SuperMat3[] dev;

		public const int BytesPerParticle = 52;

		public FieldSource(int n)
		{
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
		}

		internal void _003CRecordCore_003Eb__1(SuperShot.KeyframeContext ctx)
		{
		}

		internal void _003CRecordCore_003Eb__2()
		{
		}

		internal bool _003CRecordCore_003Eb__4()
		{
			return false;
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
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CRecordCore_003Ed__65(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
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
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
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

	public static int LiveSnapshots => 0;

	public static int LiveTasks => 0;

	public static int PipelineBuilding => 0;

	public static string TapeDir => null;

	public static string FixtureDir => null;

	public static Summary LastSummary { get; private set; }

	public static long PerFrameBytes(int n, long reconstructionCells, long rawVerts, long payloadBytes, int dop)
	{
		return 0L;
	}

	public static int PipelineVersionOf(PipelineMode m, bool paced = false)
	{
		return 0;
	}

	public static int WorkersOf(PipelineMode m)
	{
		return 0;
	}

	public static Summary Record(ComputeShader cs, SuperRodSpec rod, SuperPlateSpec plate, float spacing, float durationS, string tapeName, float marginM = 0f, float cadenceS = 2E-06f, string dir = null)
	{
		return null;
	}

	public static Summary Record(ComputeShader cs, SuperShellSpec shell, IList<SuperPlateSpec> plates, float spacing, float durationS, string tapeName, float marginM = 0f, float cadenceS = 2E-06f, string dir = null, Action onFrame = null, SuperEraDrive drive = null)
	{
		return null;
	}

	public static Summary Record(ComputeShader cs, SuperRodSpec rod, IList<SuperPlateSpec> plates, float spacing, float durationS, string tapeName, float marginM = 0f, float cadenceS = 2E-06f, string dir = null, Action onFrame = null, SuperEraDrive drive = null, bool checkpoint = false, SuperCheckpoint.Data resume = null, byte[] decor = null)
	{
		return null;
	}

	public static Summary Record(ComputeShader cs, SuperJetSpec jet, IList<SuperPlateSpec> plates, float spacing, float durationS, string tapeName, float marginM = 0f, float cadenceS = 2E-06f, string dir = null, Action onFrame = null, SuperEraDrive drive = null, bool checkpoint = false, SuperCheckpoint.Data resume = null)
	{
		return null;
	}

	private static Summary Record(ComputeShader cs, SuperRodSpec rod, SuperShellSpec shell, SuperJetSpec jet, Kind kind, IList<SuperPlateSpec> plates, float spacing, float durationS, string tapeName, float marginM, float cadenceS, string dir, Action onFrame, SuperEraDrive drive, bool checkpoint = false, SuperCheckpoint.Data resume = null, byte[] decor = null)
	{
		return null;
	}

	[IteratorStateMachine(typeof(_003CRecordCore_003Ed__65))]
	internal static IEnumerator RecordCore(ComputeShader cs, SuperRodSpec rod, SuperShellSpec shell, SuperJetSpec jet, Kind kind, IList<SuperPlateSpec> plates, float spacing, float durationS, string tapeName, float marginM, float cadenceS, string dir, Action onFrame, SuperEraDrive drive, bool checkpoint, SuperCheckpoint.Data resume, RecBox box, Action onPump = null, PipelineMode? pipeline = null, bool? paced = null, bool gameDomain = false, byte[] decor = null)
	{
		return null;
	}

	private static string DescribePlates(IList<SuperPlateSpec> plates)
	{
		return null;
	}

	private static void CopyReconStats(SuperReconGpu r, Summary s)
	{
	}

	private static string ReconPassport(Summary s)
	{
		return null;
	}

	private static void ApplyStats(Summary s, FrameStats st)
	{
	}

	internal static SuperTape.Frame BuildFrame(RunData rd, Snapshot sn, int dop, SuperHookStages stages, out FrameStats stats)
	{
		stats = default;
		return null;
	}

	private static void ComputeTail(RunData rd, Vector3[] pos, Vector3[] vel, float[] dmgArr, float[][] pf, bool[] inside, int count, int nBodies, int frameIndex, SuperHookStages stages, FrameTail t)
	{
	}

	private static void InsideAndRange(int from, int to, Vector3[] pos, Vector3 gridLo, Vector3 gridHi, bool[] inside, float[][] pf, float[] outMin, float[] outMax, int outBase)
	{
	}
}
