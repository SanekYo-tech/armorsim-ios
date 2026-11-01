using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class UltraQualityLadder
{
	public static readonly UltraRung[] Rungs = new UltraRung[]
	{
		new UltraRung { elementsPerDiameter = 16f, refWallSeconds = 54f, refElements = 3467, note = "самая дешёвая; на ней сняты отпечаток решателя и все замеры проекта" },
		new UltraRung { elementsPerDiameter = 24f, refWallSeconds = 331f, refElements = 13000, note = "" },
		new UltraRung { elementsPerDiameter = 32f, refWallSeconds = 416f, refElements = 23000, note = "" },
		new UltraRung { elementsPerDiameter = 40f, refWallSeconds = 767f, refElements = 35000, note = "" },
		new UltraRung { elementsPerDiameter = 48f, refWallSeconds = 1267f, refElements = 48000, note = "самая подробная; на машине разработки около 21 минуты" }
	};

	public static float MachineFactor = 1f;

	public static bool Benchmarked;

	public const int BenchmarkSteps = 300;

	public const float BenchmarkReferenceSeconds = 1.44f;

	private static float referenceWindowS;

	public static bool HasRungs => Rungs.Length > 0;

	public static float ReferenceWindowS
	{
		get
		{
			if (referenceWindowS <= 0f)
			{
				referenceWindowS = UltraProjection.DisplayWindowS(ContinuumScenario.Etalon());
			}
			return referenceWindowS;
		}
	}

	public static UltraRung[] Available()
	{
		var copy = new UltraRung[Rungs.Length];
		Array.Copy(Rungs, copy, Rungs.Length);
		return copy;
	}

	public static UltraRung RowFor(float elementsPerDiameter)
	{
		for (int i = 0; i < Rungs.Length; i++)
		{
			if (Mathf.Abs(Rungs[i].elementsPerDiameter - elementsPerDiameter) < 0.0001f)
			{
				return Rungs[i];
			}
		}
		Debug.LogWarning(string.Format("ULTRA: выбранной ступени D/{0:F0} в лестнице НЕТ — ", elementsPerDiameter) + string.Format("беру D/{0:F0}. Выбор относится к таблице, ", Rungs[0].elementsPerDiameter) + "которой больше нет.");
		return Rungs[0];
	}

	public static bool TrySelected(float elementsPerDiameter, out UltraRung rung)
	{
		for (int i = 0; i < Rungs.Length; i++)
		{
			if (Mathf.Abs(Rungs[i].elementsPerDiameter - elementsPerDiameter) < 0.0001f)
			{
				rung = Rungs[i];
				return true;
			}
		}
		rung = default;
		return false;
	}

	public static bool TryCoarsest(out UltraRung rung)
	{
		if (Rungs.Length == 0)
		{
			rung = default;
			return false;
		}
		rung = Rungs[0];
		for (int i = 1; i < Rungs.Length; i++)
		{
			if (rung.elementsPerDiameter > Rungs[i].elementsPerDiameter)
			{
				rung = Rungs[i];
			}
		}
		return true;
	}

	public static float Benchmark()
	{
		var scenario = ContinuumScenario.Etalon();
		scenario.elementsPerDiameter = 16f;
		scenario.outputFrames = 1;
		var watch = Stopwatch.StartNew();
		var run = new ContinuumBackend.Run(scenario);
		int steps = 0;
		while (!run.Finished && steps < 300)
		{
			run.Step();
			steps++;
		}
		watch.Stop();
		var timeline = run.Finish();
		timeline.Dispose();
		run.ReleaseWorkingSet();
		float secondsPerStep = (float)(watch.Elapsed.TotalSeconds / Math.Max(steps, 1));
		MachineFactor = Mathf.Clamp(secondsPerStep / 0.0048f, 0.05f, 50f);
		Benchmarked = true;
		Debug.Log(string.Format("ULTRA: замер машины — {0} шагов за {1:F2} с ", steps, watch.Elapsed.TotalSeconds) + string.Format("({0:F2} мс/шаг против эталонных {1:F2}) ⇒ ", secondsPerStep * 1000f, 4.8f) + string.Format("коэффициент ×{0:F2}", MachineFactor));
		return MachineFactor;
	}

	public static float EstimatedMinutes(UltraRung r)
	{
		return r.refWallSeconds * MachineFactor / 60f;
	}

	public static float EstimatedMinutes(UltraRung r, float windowS)
	{
		float minutes = r.refWallSeconds * MachineFactor / 60f;
		float scale = (windowS > 0f && ReferenceWindowS > 0f) ? windowS / ReferenceWindowS : 1f;
		return minutes * scale;
	}

	public static bool NeedsRebenchmark(UltraRung r, double actualSeconds)
	{
		double expected = r.refWallSeconds * MachineFactor;
		if (expected <= 0.0 || actualSeconds <= 0.0)
		{
			return false;
		}
		return actualSeconds > expected ? actualSeconds / expected > 2.0 : expected / actualSeconds > 2.0;
	}
}
