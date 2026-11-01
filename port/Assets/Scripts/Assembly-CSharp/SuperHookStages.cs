using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

public sealed class SuperHookStages
{
	public enum Stage
	{
		Download = 0,
		Fields = 1,
		Filter = 2,
		Density = 3,
		Polygonize = 4,
		Decimate = 5,
		SignedVolume = 6,
		ParticleHash = 7,
		VertexFields = 8,
		FieldBlock = 9,
		Telemetry = 10,
		Debris = 11,
		Classify = 12,
		Encode = 13,
		Write = 14,
		Checkpoint = 15,
		Enqueue = 16,
		ReconDispatch = 17,
		ReconWait = 18
	}

	public static readonly int Count;

	private readonly long[] _ticks;

	private long _work;

	private readonly object _frameLock;

	private long _frameCount;

	private long _frameTicksSum;

	private long _frameTicksMax;

	private long _hookCount;

	private long _hookTicksSum;

	private long _hookTicksMax;

	public static long Now => Stopwatch.GetTimestamp();

	public long WorkTicks => Interlocked.Read(ref _work);

	public double FrameMeanMs
	{
		get
		{
			lock (_frameLock)
			{
				if (_frameCount <= 0L)
				{
					return 0.0;
				}
				return (double)_frameTicksSum * 1000.0 / Stopwatch.Frequency / _frameCount;
			}
		}
	}

	public double FrameMaxMs
	{
		get
		{
			lock (_frameLock)
			{
				return (double)_frameTicksMax * 1000.0 / Stopwatch.Frequency;
			}
		}
	}

	public double HookMeanMs
	{
		get
		{
			lock (_frameLock)
			{
				if (_hookCount <= 0L)
				{
					return 0.0;
				}
				return (double)_hookTicksSum * 1000.0 / Stopwatch.Frequency / _hookCount;
			}
		}
	}

	public double HookMaxMs
	{
		get
		{
			lock (_frameLock)
			{
				return (double)_hookTicksMax * 1000.0 / Stopwatch.Frequency;
			}
		}
	}

	public double SumMs
	{
		get
		{
			double sum = 0.0;
			for (int i = 0; i < Count; i++)
			{
				if (i != 18)
				{
					sum += Ms((Stage)i);
				}
			}
			return sum;
		}
	}

	public SuperHookStages()
	{
		_ticks = new long[Count];
		_frameLock = new object();
	}

	static SuperHookStages()
	{
		Count = Enum.GetValues(typeof(Stage)).Length;
	}

	public long Lap(Stage s, long t0)
	{
		long now = Stopwatch.GetTimestamp();
		Interlocked.Add(ref _ticks[(int)s], now - t0);
		Interlocked.Increment(ref _work);
		return now;
	}

	public void Tick()
	{
		Interlocked.Increment(ref _work);
	}

	public void Add(Stage s, long ticks)
	{
		Interlocked.Add(ref _ticks[(int)s], ticks);
	}

	public void FrameDone(long ticks)
	{
		lock (_frameLock)
		{
			_frameCount++;
			_frameTicksSum += ticks;
			if (ticks > _frameTicksMax)
			{
				_frameTicksMax = ticks;
			}
		}
	}

	public void HookDone(long ticks)
	{
		lock (_frameLock)
		{
			_hookCount++;
			_hookTicksSum += ticks;
			if (ticks > _hookTicksMax)
			{
				_hookTicksMax = ticks;
			}
		}
	}

	public double Ms(Stage s)
	{
		return (double)Interlocked.Read(ref _ticks[(int)s]) * 1000.0 / Stopwatch.Frequency;
	}

	public string OneLine(int frames)
	{
		int[] order = new int[Count];
		for (int i = 0; i < Count; i++)
		{
			order[i] = i;
		}
		Array.Sort(order, (int a, int b) => Ms((Stage)b).CompareTo(Ms((Stage)a)));
		double sum = Math.Max(1E-09, SumMs);
		StringBuilder sb = new StringBuilder();
		sb.Append(string.Format("[S25 стадии крюка] кадров {0}, сумма {1:F2} с ЦПУ-времени стадий: ", frames, sum / 1000.0));
		for (int i = 0; i < Count; i++)
		{
			Stage stage = (Stage)order[i];
			if (stage == Stage.ReconWait)
			{
				continue;
			}
			double ms = Ms(stage);
			if (ms > 0.0)
			{
				sb.Append(string.Format("{0} {1:F2} с ({2:P1}); ", stage, ms / 1000.0, ms / sum));
			}
		}
		double reconWait = Ms(Stage.ReconWait);
		if (reconWait > 0.0)
		{
			sb.Append(string.Format("ReconWait {0:F2} с (ожидание GPU рабочими, вне суммы); ", reconWait / 1000.0));
		}
		return sb.ToString();
	}

	public string Top3()
	{
		int[] order = new int[Count - 1];
		int n = 0;
		for (int i = 0; i < Count; i++)
		{
			if (i != 18)
			{
				order[n++] = i;
			}
		}
		Array.Sort(order, (int a, int b) => Ms((Stage)b).CompareTo(Ms((Stage)a)));
		double sum = Math.Max(1E-09, SumMs);
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < 3; i++)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}
			Stage stage = (Stage)order[i];
			sb.Append(string.Format("{0} {1:P0}", stage, Ms(stage) / sum));
		}
		return sb.ToString();
	}

	public void FillRecord(SuperRunRecord.HookStages h)
	{
		h.downloadMs = Ms(Stage.Download);
		h.fieldsMs = Ms(Stage.Fields);
		h.filterMs = Ms(Stage.Filter);
		h.densityMs = Ms(Stage.Density);
		h.polygonizeMs = Ms(Stage.Polygonize);
		h.decimateMs = Ms(Stage.Decimate);
		h.signedVolumeMs = Ms(Stage.SignedVolume);
		h.particleHashMs = Ms(Stage.ParticleHash);
		h.vertexFieldsMs = Ms(Stage.VertexFields);
		h.fieldBlockMs = Ms(Stage.FieldBlock);
		h.telemetryMs = Ms(Stage.Telemetry);
		h.debrisMs = Ms(Stage.Debris);
		h.classifyMs = Ms(Stage.Classify);
		h.encodeMs = Ms(Stage.Encode);
		h.writeMs = Ms(Stage.Write);
		h.checkpointMs = Ms(Stage.Checkpoint);
		h.enqueueMs = Ms(Stage.Enqueue);
		h.reconDispatchMs = Ms(Stage.ReconDispatch);
		h.reconWaitMs = Ms(Stage.ReconWait);
		h.sumMs = SumMs;
		h.keyframeMeanMs = FrameMeanMs;
		h.keyframeMaxMs = FrameMaxMs;
		h.hookMainMeanMs = HookMeanMs;
		h.hookMainMaxMs = HookMaxMs;
	}
}
