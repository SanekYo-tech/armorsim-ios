using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public sealed class SuperTapeVerifier : IDisposable
{
	public sealed class CorruptFrameException : IOException
	{
		public readonly int Frame;

		public CorruptFrameException(int frame)
		{
			Frame = frame;
		}
	}

	private readonly SuperTape.Reader own;

	private readonly object gate;

	private readonly HashSet<int> bad;

	private int firstBad;

	private Thread worker;

	private bool stop;

	private int checkedCount;

	private bool done;

	private string failure;

	public int Total
	{
		get;
		private set;
	}

	public int Checked
	{
		get
		{
			lock (gate)
			{
				return checkedCount;
			}
		}
	}

	public bool Done
	{
		get
		{
			lock (gate)
			{
				return done;
			}
		}
	}

	public int FirstBad
	{
		get
		{
			lock (gate)
			{
				return firstBad;
			}
		}
	}

	public string Failure
	{
		get
		{
			lock (gate)
			{
				return failure;
			}
		}
	}

	public bool IsBad(int index)
	{
		lock (gate)
		{
			return bad.Contains(index);
		}
	}

	public SuperTapeVerifier(SuperTape.Reader tape)
	{
		gate = new object();
		bad = new HashSet<int>();
		firstBad = -1;
		Total = tape.header.frameCount;
		own = tape.SecondHandle();
		Thread t = new Thread(Loop);
		t.IsBackground = true;
		t.Name = "SuperTapeVerifier";
		t.Priority = ThreadPriority.BelowNormal;
		worker = t;
		t.Start();
	}

	private void Loop()
	{
		try
		{
			for (int i = 0; i < Total; i++)
			{
				if (stop)
				{
					break;
				}
				if (!own.FrameHashOk(i))
				{
					lock (gate)
					{
						bad.Add(i);
						if (firstBad < 0)
						{
							firstBad = i;
						}
					}
				}
				checkedCount++;
			}
		}
		catch (Exception ex)
		{
			lock (gate)
			{
				failure = ex.ToString();
			}
		}
		finally
		{
			done = true;
		}
	}

	public bool Join(int ms)
	{
		if (worker == null)
		{
			return true;
		}
		return worker.Join(ms);
	}

	public void Dispose()
	{
		stop = true;
		Thread t = worker;
		worker = null;
		if (t != null && !t.Join(5000))
		{
			Debug.LogWarning("[Super] Фоновая проверка ленты не остановилась за 5 с.");
		}
		own.Dispose();
	}
}
