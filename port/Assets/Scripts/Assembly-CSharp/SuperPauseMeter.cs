using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

public sealed class SuperPauseMeter : IDisposable
{
	public const double ThresholdMs = 10.0;

	public int gaps;

	public double gapSumMs;

	public double gapMaxMs;

	public long iterations;

	public double wallMs;

	public int gapGcs;

	public double meanStepMs;

	private Thread _t;

	private bool _stop;

	private readonly Stopwatch _sw = new Stopwatch();

	private double _stepSumMs;

	private bool _periodTaken;

	private bool _periodTakenEver;

	public double ShareOfWall
	{
		get
		{
			if (wallMs > 0.0)
			{
				return gapSumMs / wallMs;
			}
			return -1.0;
		}
	}

#if !UNITY_IOS && !UNITY_ANDROID
	[DllImport("winmm.dll")]
	[PreserveSig]
	private static extern uint TimeBeginPeriod(uint ms);

	[DllImport("winmm.dll")]
	[PreserveSig]
	private static extern uint TimeEndPeriod(uint ms);
#endif

	private void TakePeriod()
	{
#if !UNITY_IOS && !UNITY_ANDROID
		_periodTaken = TimeBeginPeriod(1) == 0;
		_periodTakenEver = _periodTaken;
#endif
	}

	private void ReleasePeriod()
	{
#if !UNITY_IOS && !UNITY_ANDROID
		if (_periodTaken)
		{
			TimeEndPeriod(1);
			_periodTaken = false;
		}
#endif
	}

	public void Begin()
	{
		if (_t == null)
		{
			Volatile.Write(ref _stop, false);
			TakePeriod();
			_sw.Restart();
			Thread thread = new Thread(Loop);
			thread.IsBackground = true;
			thread.Name = "S33 pause meter";
			thread.Priority = ThreadPriority.Highest;
			_t = thread;
			_t.Start();
		}
	}

	public void End()
	{
		if (_t != null)
		{
			Volatile.Write(ref _stop, true);
			_t.Join(200);
			_t = null;
			_sw.Stop();
			ReleasePeriod();
			wallMs = _sw.Elapsed.TotalMilliseconds;
			if (iterations > 0)
			{
				meanStepMs = _stepSumMs / (double)iterations;
			}
			else
			{
				meanStepMs = -1.0;
			}
		}
	}

	private void Loop()
	{
		double num = 1000.0 / (double)Stopwatch.Frequency;
		long num2 = Stopwatch.GetTimestamp();
		int num3 = GC.CollectionCount(0);
		while (!Volatile.Read(ref _stop))
		{
			Thread.Sleep(1);
			long num4 = Stopwatch.GetTimestamp();
			int num5 = GC.CollectionCount(0);
			iterations++;
			double num6 = (double)(num4 - num2) * num;
			_stepSumMs += num6;
			if (num6 > ThresholdMs)
			{
				gaps++;
				gapSumMs += num6;
				if (num6 > gapMaxMs)
				{
					gapMaxMs = num6;
				}
				gapGcs += num5 - num3;
			}
			num2 = num4;
			num3 = num5;
		}
	}

	public static SuperPauseMeter MeasureNoiseFloor(int ms)
	{
		SuperPauseMeter superPauseMeter = new SuperPauseMeter();
		superPauseMeter.Begin();
		Thread.Sleep(Math.Max(50, ms));
		superPauseMeter.End();
		return superPauseMeter;
	}

	public string OneLine()
	{
		return string.Format("пауз > {0:F0} мс: {1} (сумма {2:F2} с = {3:P2} стены, INFERRED; ", ThresholdMs, gaps, gapSumMs / 1000.0, ShareOfWall) + string.Format("наибольшая {0:F0} мс; сборок внутри {1}; итераций {2}, шаг цикла {3:F2} мс", gapMaxMs, gapGcs, iterations, meanStepMs) + (_periodTakenEver ? ", разрешение таймера поднято до 1 мс" : ", разрешение таймера НЕ поднято") + ")";
	}

	public void Dispose()
	{
		End();
	}
}
