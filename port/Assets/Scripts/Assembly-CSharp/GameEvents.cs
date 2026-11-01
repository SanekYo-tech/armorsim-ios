using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

public static class GameEvents
{
	[CompilerGenerated]
	private static Action<ShotReport> m_ShotResolved;

	[CompilerGenerated]
	private static Action m_NewSimulationRequested;

	public static event Action<ShotReport> ShotResolved
	{
		[CompilerGenerated]
		add
		{
			Action<ShotReport> obj = m_ShotResolved;
			Action<ShotReport> action2;
			do
			{
				action2 = obj;
				Action<ShotReport> action3 = (Action<ShotReport>)Delegate.Combine(action2, value);
				obj = Interlocked.CompareExchange(ref m_ShotResolved, action3, action2);
			}
			while (obj != action2);
		}
		[CompilerGenerated]
		remove
		{
			Action<ShotReport> obj = m_ShotResolved;
			Action<ShotReport> action2;
			do
			{
				action2 = obj;
				Action<ShotReport> action3 = (Action<ShotReport>)Delegate.Remove(action2, value);
				obj = Interlocked.CompareExchange(ref m_ShotResolved, action3, action2);
			}
			while (obj != action2);
		}
	}

	public static event Action NewSimulationRequested
	{
		[CompilerGenerated]
		add
		{
			Action obj = m_NewSimulationRequested;
			Action action2;
			do
			{
				action2 = obj;
				Action action3 = (Action)Delegate.Combine(action2, value);
				obj = Interlocked.CompareExchange(ref m_NewSimulationRequested, action3, action2);
			}
			while (obj != action2);
		}
		[CompilerGenerated]
		remove
		{
			Action obj = m_NewSimulationRequested;
			Action action2;
			do
			{
				action2 = obj;
				Action action3 = (Action)Delegate.Remove(action2, value);
				obj = Interlocked.CompareExchange(ref m_NewSimulationRequested, action3, action2);
			}
			while (obj != action2);
		}
	}

	public static void RaiseShotResolved(ShotReport report)
	{
		if (report == null)
		{
			return;
		}
		try
		{
			m_ShotResolved?.Invoke(report);
		}
		catch (Exception value)
		{
			Debug.LogError(string.Format("[GameEvents] подписчик ShotResolved бросил: {0}", value));
		}
	}

	public static void RaiseNewSimulationRequested()
	{
		try
		{
			m_NewSimulationRequested?.Invoke();
		}
		catch (Exception value)
		{
			Debug.LogError(string.Format("[GameEvents] подписчик NewSimulationRequested бросил: {0}", value));
		}
	}

	public static void ResetForTests()
	{
		m_ShotResolved = null;
		m_NewSimulationRequested = null;
	}
}
