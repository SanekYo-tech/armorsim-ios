using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class UltraSolveQueue
{
	public enum Submission
	{
		StartedNew = 0,
		AlreadyRunningSameKey = 1,
		ReplacedRunning = 2,
		NotCacheable = 3
	}

	private static UltraSolveQueue instance;

	public Func<ContinuumScenario, IUltraJob> jobFactory;

	private readonly object gate;

	private Thread worker;

	private bool cancelRequested;

	private UltraProgress progress;

	private ContinuumScenario pendingScenario;

	private string runningKey;

	private GeometryTimeline finished;

	private string finishedKey;

	public IUltraTimelineSink sink;

	public static UltraSolveQueue Instance
	{
		get
		{
			if (instance == null)
			{
				instance = new UltraSolveQueue();
			}
			return instance;
		}
	}

	public UltraProgress Progress
	{
		get
		{
			lock (gate)
			{
				return progress;
			}
		}
	}

	public UltraSolveQueue()
	{
		gate = new object();
	}

	public Submission Submit(ContinuumScenario s, out string key)
	{
		key = UltraCacheKey.Compute(s);
		if (key == null)
		{
			return Submission.NotCacheable;
		}
		lock (gate)
		{
			if (worker != null && worker.IsAlive)
			{
				if (runningKey == key)
				{
					return Submission.AlreadyRunningSameKey;
				}
				Monitor.PulseAll(gate);
				Volatile.Write(ref cancelRequested, true);
			}
			pendingScenario = s.Clone();
		}
		JoinWorker();
		StartWorker(key);
		return Submission.ReplacedRunning;
	}

	private void StartWorker(string key)
	{
		lock (gate)
		{
			Volatile.Write(ref cancelRequested, false);
			runningKey = key;
			progress = new UltraProgress
			{
				state = UltraJobState.Running,
				key = key
			};
			finished = null;
			finishedKey = null;
		}
		Thread thread = new Thread(WorkerBody);
		thread.IsBackground = true;
		thread.Name = "ULTRA solve";
		worker = thread;
		worker.Start();
	}

	private void WorkerBody()
	{
		ContinuumScenario continuumScenario;
		string text;
		lock (gate)
		{
			continuumScenario = pendingScenario;
			text = runningKey;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		IUltraJob ultraJob = null;
		try
		{
			ultraJob = ((jobFactory != null) ? jobFactory(continuumScenario) : new ContinuumSolveJob(continuumScenario));
			int num = ultraJob.EstimateSteps();
			lock (gate)
			{
				progress.stepsEstimate = num;
			}
			int num2 = 0;
			while (!Volatile.Read(ref cancelRequested))
			{
				if (!ultraJob.Step())
				{
					GeometryTimeline geometryTimeline = ultraJob.Finish();
					lock (gate)
					{
						finished = geometryTimeline;
						finishedKey = text;
						progress.state = UltraJobState.Done;
						progress.elapsedS = stopwatch.Elapsed.TotalSeconds;
						progress.stepsDone = num2;
						progress.stepsTotal = num2;
					}
					return;
				}
				num2++;
				lock (gate)
				{
					progress.stepsDone = num2;
					progress.stepsTotal = Blend(num, num2, progress.stepsTotal);
					progress.elapsedS = stopwatch.Elapsed.TotalSeconds;
				}
			}
			lock (gate)
			{
				progress.state = UltraJobState.Cancelled;
				progress.elapsedS = stopwatch.Elapsed.TotalSeconds;
			}
		}
		catch (Exception ex)
		{
			lock (gate)
			{
				progress.state = UltraJobState.Failed;
				progress.error = ex.GetType().Name + ": " + ex.Message;
			}
		}
		finally
		{
			ultraJob?.Release();
		}
	}

	public static int Blend(int estimate, int done, int prevTotal)
	{
		if (done < 200)
		{
			return Math.Max(estimate, done + 1);
		}
		float num = MathF.Min(1f, MathF.Max(0f, (float)done / Math.Max(1, estimate)));
		return Math.Max(done + 1, (int)Math.Round((float)prevTotal + ((float)done - prevTotal) * num, MidpointRounding.AwayFromZero));
	}

	public static int EstimateSteps(ContinuumScenario s)
	{
		float num = MathF.Max(ContinuumMaterials.Get(s.rodMaterial).LongitudinalWaveSpeed, ContinuumMaterials.Get(s.targetMaterial).LongitudinalWaveSpeed) + MathF.Abs(s.impactVelocityMps);
		float num2 = s.ElementSizeM * s.cflSafety / MathF.Max(1f, num);
		float num3 = ((s.durationS > 0f) ? s.durationS : s.DefaultDuration()) / MathF.Max(num2, 1E-12f);
		return Math.Max(1, (int)Math.Ceiling(num3));
	}

	public void Pump()
	{
		GeometryTimeline geometryTimeline = null;
		string text = null;
		UltraJobState ultraJobState;
		string text2;
		lock (gate)
		{
			ultraJobState = progress.state;
			text2 = progress.error;
			if (finished != null && finishedKey != null)
			{
				geometryTimeline = finished;
				text = finishedKey;
				finished = null;
				finishedKey = null;
			}
		}
		if (ultraJobState == UltraJobState.Failed && text2 != null)
		{
			Debug.LogError("ULTRA: решение не удалось — " + text2);
			lock (gate)
			{
				progress.error = null;
			}
		}
		if (geometryTimeline != null && sink != null)
		{
			sink.Accept(text, geometryTimeline);
		}
	}

	public void RequestCancel()
	{
		Volatile.Write(ref cancelRequested, true);
	}

	public void Shutdown()
	{
		Volatile.Write(ref cancelRequested, true);
		JoinWorker();
		lock (gate)
		{
			finished = null;
			finishedKey = null;
		}
	}

	private void JoinWorker()
	{
		if (worker != null)
		{
			if (!worker.Join(10000))
			{
				Debug.LogError("ULTRA: рабочий поток не завершился за 10 с — буферы могли утечь");
			}
			worker = null;
		}
	}
}
