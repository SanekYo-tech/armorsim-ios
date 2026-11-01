using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;

public sealed class SuperRunMeter : IDisposable
{
	public int moves;

	public double firstMoveMs;

	public double maxMoveMs;

	public double sumMoveMs;

	public int maxMoveIndex;

	public double wallMs;

	public double cpuMs;

	public double cpuUtil;

	public long peakWorkingSetBytes;

	public long peakManagedBytes;

	public long peakPrivateBytes;

	public long processPeakWorkingSetBytes;

	public readonly SuperPauseMeter pause;

	public readonly SuperUtilSeries util;

	public double cpuP10;

	public double cpuP50;

	public double cpuMin;

	public double occBuilding;

	public double occInFlight;

	public double occReadyUnwritten;

	private long _occSamples;

	private double _occBuildSum;

	private double _occFlightSum;

	private double _occReadySum;

	public string kdopChanges;

	private int _kSeen;

	private int _dopSeen;

	private readonly StringBuilder _kdop;

	private bool _stepLoop;

	private double _utilPrevCpuMs;

	private double _utilPrevWallMs;

	private int _tick;

	private readonly Stopwatch _wall;

	private double _cpu0Ms;

	private bool _cpuOk;

	private Thread _sampler;

	private bool _stop;

	public readonly int[] topIndex;

	public readonly double[] topMs;

	public readonly double[] topAtS;

	public readonly int[] topGcs;

	public int gcCount;

	private int _gc0;

	public SuperRunMeter()
	{
		pause = new SuperPauseMeter();
		util = new SuperUtilSeries();
		cpuP10 = -1.0;
		cpuP50 = -1.0;
		cpuMin = -1.0;
		occBuilding = -1.0;
		occInFlight = -1.0;
		occReadyUnwritten = -1.0;
		kdopChanges = "";
		_kSeen = -1;
		_dopSeen = -1;
		_kdop = new StringBuilder();
		_wall = new Stopwatch();
		topIndex = new int[5];
		topMs = new double[5];
		topAtS = new double[5];
		topGcs = new int[5];
	}

	public void MarkStepLoopBegin()
	{
		_utilPrevCpuMs = (SuperMachine.ProcessCpuMilliseconds(out double cpu) ? cpu : -1.0);
		_utilPrevWallMs = _wall.Elapsed.TotalMilliseconds;
		Volatile.Write(ref _stepLoop, true);
	}

	public void MarkStepLoopEnd()
	{
		Volatile.Write(ref _stepLoop, false);
	}

	public void Begin()
	{
		_cpuOk = SuperMachine.ProcessCpuMilliseconds(out _cpu0Ms);
		_gc0 = GC.CollectionCount(0);
		_wall.Start();
		Volatile.Write(ref _stop, false);
		_sampler = new Thread(Sample)
		{
			IsBackground = true,
			Name = "S25 mem sampler"
		};
		_sampler.Start();
		pause.Begin();
	}

	private void Sample()
	{
		while (!Volatile.Read(ref _stop))
		{
			if (SuperMachine.ProcessMemory(out long workingSet, out long processPeak, out long privateBytes))
			{
				if (workingSet > peakWorkingSetBytes)
				{
					peakWorkingSetBytes = workingSet;
				}
				if (privateBytes > peakPrivateBytes)
				{
					peakPrivateBytes = privateBytes;
				}
				if (processPeak > processPeakWorkingSetBytes)
				{
					processPeakWorkingSetBytes = processPeak;
				}
			}
			long managed = GC.GetTotalMemory(false);
			if (managed > peakManagedBytes)
			{
				peakManagedBytes = managed;
			}
			SampleS33();
			Thread.Sleep(50);
		}
	}

	private void SampleS33()
	{
		if (!Volatile.Read(ref _stepLoop))
		{
			return;
		}
		_occSamples++;
		_occBuildSum += SuperTapeRecorder.PipelineBuilding;
		_occFlightSum += SuperTapeRecorder.PipelineInFlight;
		_occReadySum += SuperTapeRecorder.PipelineReadyUnwritten;
		int k = SuperTapeRecorder.PipelineK;
		int dop = SuperTapeRecorder.PipelineDop;
		if (k != _kSeen || dop != _dopSeen)
		{
			if (_kdop.Length > 0)
			{
				_kdop.Append("; ");
			}
			_kdop.Append(string.Format("{0:F1} с: K {1} × dop {2}", _wall.Elapsed.TotalSeconds, k, dop));
			_kSeen = k;
			_dopSeen = dop;
		}
		_tick++;
		if (_tick % 10 != 0 || !SuperMachine.ProcessCpuMilliseconds(out double cpu) || _utilPrevCpuMs < 0.0)
		{
			return;
		}
		double wall = _wall.Elapsed.TotalMilliseconds;
		double wallDelta = wall - _utilPrevWallMs;
		double cpuDelta = cpu - _utilPrevCpuMs;
		_utilPrevWallMs = wall;
		_utilPrevCpuMs = cpu;
		if (wallDelta > 1.0)
		{
			double value = cpuDelta / wallDelta / Math.Max(1, Environment.ProcessorCount);
			if (value >= 0.0)
			{
				util.Add(value);
			}
		}
	}

	public bool Move(IEnumerator it)
	{
		int gc0 = GC.CollectionCount(0);
		long t0 = Stopwatch.GetTimestamp();
		bool moved = it.MoveNext();
		double ms = (double)(Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
		int gcs = GC.CollectionCount(0) - gc0;
		if (moves == 0)
		{
			firstMoveMs = ms;
		}
		else if (ms > maxMoveMs)
		{
			maxMoveIndex = moves;
			maxMoveMs = ms;
		}
		for (int i = 0; i < 5; i++)
		{
			if (ms > topMs[i])
			{
				for (int j = 4; j > i; j--)
				{
					topMs[j] = topMs[j - 1];
					topIndex[j] = topIndex[j - 1];
					topAtS[j] = topAtS[j - 1];
					topGcs[j] = topGcs[j - 1];
				}
				topMs[i] = ms;
				topIndex[i] = moves;
				topAtS[i] = _wall.Elapsed.TotalSeconds;
				topGcs[i] = gcs;
				break;
			}
		}
		sumMoveMs += ms;
		moves++;
		return moved;
	}

	public string TopStalls()
	{
		StringBuilder sb = new StringBuilder();
		int n = 0;
		for (int i = 0; i < 5; i++)
		{
			if (topMs[i] > 0.0)
			{
				sb.Append((n > 0) ? "; " : "");
				sb.Append(string.Format("№{0} {1:F0} мс на {2:F1} с (сборок мусора внутри: {3})", topIndex[i], topMs[i], topAtS[i], topGcs[i]));
				n++;
			}
		}
		return (sb.Length == 0) ? "нет" : sb.ToString();
	}

	public void End()
	{
		if (!_wall.IsRunning)
		{
			return;
		}
		_wall.Stop();
		Volatile.Write(ref _stop, true);
		Volatile.Write(ref _stepLoop, false);
		if (_sampler != null)
		{
			_sampler.Join(500);
		}
		pause.End();
		cpuP10 = util.Quantile(0.1);
		cpuP50 = util.Quantile(0.5);
		cpuMin = ((util.Count == 0) ? -1.0 : util.Quantile(0.0));
		if (_occSamples > 0L)
		{
			occBuilding = _occBuildSum / _occSamples;
			occInFlight = _occFlightSum / _occSamples;
			occReadyUnwritten = _occReadySum / _occSamples;
		}
		kdopChanges = _kdop.ToString();
		wallMs = _wall.Elapsed.TotalMilliseconds;
		gcCount = GC.CollectionCount(0) - _gc0;
		cpuMs = ((_cpuOk && SuperMachine.ProcessCpuMilliseconds(out double cpu)) ? (cpu - _cpu0Ms) : -1.0);
		if (SuperMachine.ProcessMemory(out long workingSet, out long processPeak, out long privateBytes))
		{
			if (workingSet > peakWorkingSetBytes)
			{
				peakWorkingSetBytes = workingSet;
			}
			if (privateBytes > peakPrivateBytes)
			{
				peakPrivateBytes = privateBytes;
			}
			if (processPeak > processPeakWorkingSetBytes)
			{
				processPeakWorkingSetBytes = processPeak;
			}
		}
		cpuUtil = -1.0;
		if (cpuMs >= 0.0 && wallMs > 0.0)
		{
			cpuUtil = cpuMs / wallMs / Math.Max(1, Environment.ProcessorCount);
		}
	}

	public void Dispose()
	{
		End();
	}
}
