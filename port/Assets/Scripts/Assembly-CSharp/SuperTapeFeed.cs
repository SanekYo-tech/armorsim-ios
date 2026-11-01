using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public sealed class SuperTapeFeed : IDisposable
{
	public const int Ahead = 3;

	public const long BudgetBytes = 536870912L;

	private readonly SuperTape.Reader main;

	private SuperTape.Reader work;

	private Thread worker;

	private readonly object gate;

	private readonly Dictionary<int, SuperTape.Frame> ready;

	private readonly Dictionary<int, long> readyBytes;

	private long heldBytes;

	private int curField;

	private int wantFrom;

	private int wantStep;

	private int inFlight;

	private Exception failure;

	private bool stop;

	public SuperTapeVerifier Verifier;

	public int HitCount { get; private set; }

	public int MissCount { get; private set; }

	public bool Threaded => worker != null;

	public SuperTapeFeed(SuperTape.Reader tape)
	{
		gate = new object();
		ready = new Dictionary<int, SuperTape.Frame>();
		readyBytes = new Dictionary<int, long>();
		curField = -1;
		wantFrom = -1;
		wantStep = 1;
		inFlight = -1;
		if (tape == null)
		{
			throw new ArgumentNullException("tape");
		}
		main = tape;
		try
		{
			work = tape.SecondHandle();
			Thread thread = new Thread(Loop);
			thread.IsBackground = true;
			thread.Name = "SuperTapeFeed";
			worker = thread;
			worker.Start();
		}
		catch (Exception ex)
		{
			work?.Dispose();
			work = null;
			worker = null;
			Debug.LogWarning("[Super] Упреждающая раскодировка не завелась (" + ex.Message + ") — показ идёт синхронно, кадр за кадром.");
		}
	}

	public SuperTape.Frame Get(int index, int field)
	{
		if (Verifier != null && Verifier.IsBad(index))
		{
			throw new SuperTapeVerifier.CorruptFrameException(index);
		}
		lock (gate)
		{
			if (field != curField)
			{
				Forget();
				curField = field;
			}
			if (wantFrom >= 0 && index != wantFrom)
			{
				wantStep = ((index >= wantFrom) ? 1 : (-1));
			}
			wantFrom = index;
			Monitor.PulseAll(gate);
			while (true)
			{
				if (failure != null)
				{
					Throw();
				}
				if (ready.TryGetValue(index, out SuperTape.Frame value))
				{
					HitCount++;
					return value;
				}
				if (inFlight != index)
				{
					break;
				}
				Monitor.Wait(gate);
			}
		}
		MissCount++;
		SuperTape.Frame frame = main.ReadFrame(index, SuperTape.Detail.OneField, field);
		RefuseIfBad(index);
		lock (gate)
		{
			if (field == curField)
			{
				Put(index, frame);
			}
			Monitor.PulseAll(gate);
		}
		return frame;
	}

	private void Throw()
	{
		if (failure == null)
		{
			return;
		}
		Exception ex = failure;
		failure = null;
		throw new IOException("упреждающая раскодировка: " + ex.Message, ex);
	}

	private void Forget()
	{
		ready.Clear();
		readyBytes.Clear();
		heldBytes = 0L;
	}

	private void Put(int index, SuperTape.Frame f)
	{
		if (!ready.ContainsKey(index))
		{
			long num = Weigh(f);
			ready[index] = f;
			readyBytes[index] = num;
			heldBytes += num;
			Trim();
		}
	}

	private void Trim()
	{
		while (heldBytes > BudgetBytes && ready.Count > 1)
		{
			int num = -1;
			int num2 = -1;
			foreach (int key in ready.Keys)
			{
				if (key != wantFrom)
				{
					int num3 = Math.Abs(key - wantFrom);
					if (num3 > num2)
					{
						num2 = num3;
						num = key;
					}
				}
			}
			if (num < 0)
			{
				break;
			}
			heldBytes -= readyBytes[num];
			ready.Remove(num);
			readyBytes.Remove(num);
		}
	}

	private static long Weigh(SuperTape.Frame f)
	{
		long num = 0L;
		for (int i = 0; i < f.bodies.Length; i++)
		{
			SuperTape.BodyFrame bodyFrame = f.bodies[i];
			num += ((long)bodyFrame.vertices.Length * 3 + bodyFrame.triangles.Length) * 4;
			if (bodyFrame.fields != null)
			{
				for (int j = 0; j < bodyFrame.fields.Length; j++)
				{
					if (bodyFrame.fields[j] != null)
					{
						num += bodyFrame.fields[j].Length * 4;
					}
				}
			}
			if (bodyFrame.blockHalf != null)
			{
				for (int k = 0; k < bodyFrame.blockHalf.Length; k++)
				{
					if (bodyFrame.blockHalf[k] != null)
					{
						num += bodyFrame.blockHalf[k].Length;
					}
				}
			}
		}
		num += (long)f.debrisCount * 12;
		if (f.debrisFields != null)
		{
			for (int l = 0; l < f.debrisFields.Length; l++)
			{
				if (f.debrisFields[l] != null)
				{
					num += f.debrisFields[l].Length * 4;
				}
			}
		}
		return num;
	}

	private bool Pick(out int index, out int field)
	{
		index = -1;
		field = curField;
		if (wantFrom < 0 || curField < 0)
		{
			return false;
		}
		int frameCount = main.FrameCount;
		for (int i = 0; i <= Ahead; i++)
		{
			int num = ((wantStep >= 0) ? i : (-i));
			int num2 = wantFrom + num;
			if (num2 < 0 || num2 >= frameCount)
			{
				continue;
			}
			if (ready.ContainsKey(num2))
			{
				continue;
			}
			index = num2;
			return true;
		}
		return false;
	}

	private void Loop()
	{
		bool lockTaken = false;
		try
		{
			while (true)
			{
				Monitor.Enter(gate, ref lockTaken);
				while (true)
				{
					if (stop)
					{
						return;
					}
					int field = curField;
					bool flag = false;
					if (wantFrom >= 0 && field >= 0)
					{
						int frameCount = main.header.frameCount;
						for (int i = 0; i <= Ahead; i++)
						{
							int num = ((wantStep < 0) ? (-i) : i);
							int num2 = wantFrom + num;
							if (0 <= num2 && num2 < frameCount && !ready.ContainsKey(num2))
							{
								inFlight = num2;
								Monitor.Exit(gate);
								lockTaken = false;
								SuperTape.Frame frame = work.ReadFrame(num2, SuperTape.Detail.OneField, field);
								Monitor.Enter(gate, ref lockTaken);
								inFlight = -1;
								if (stop)
								{
									return;
								}
								if (curField == field)
								{
									Put(num2, frame);
								}
								Monitor.PulseAll(gate);
								flag = true;
								break;
							}
						}
					}
					if (flag)
					{
						break;
					}
					Monitor.Wait(gate);
				}
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(gate);
			}
		}
	}

	public void Dispose()
	{
		Thread thread;
		lock (gate)
		{
			stop = true;
			thread = worker;
			worker = null;
			Forget();
			Monitor.PulseAll(gate);
		}
		if (thread != null && !thread.Join(5000))
		{
			Debug.LogWarning("[Super] Поток упреждающей раскодировки не остановился за 5 с.");
		}
		work?.Dispose();
		work = null;
	}

	private void RefuseIfBad(int index)
	{
		if (Verifier != null && Verifier.IsBad(index))
		{
			throw new SuperTapeVerifier.CorruptFrameException(index);
		}
	}
}
