using System;
using System.Diagnostics;

public sealed class SuperStepPacer
{
	public const double DefaultBudgetMs = 100.0;

	public const int CalibrationBatch = 8;

	public const int MaxStepsPerFrame = 4096;

	public double budgetMs;

	public int yields;

	public int calibrations;

	public double maxBatchMs;

	public double sumBatchMs;

	public int minStepsPerFrame;

	public int maxStepsPerFrame;

	private int sinceYield;

	private int calibLeft;

	private int calibSize;

	private long calibT0;

	private bool first;

	public int StepsPerFrame { get; private set; }

	public double MsPerStep { get; private set; }

	public void QueueIsEmpty()
	{
		calibSize = first ? 1 : Math.Min(CalibrationBatch, Math.Max(1, StepsPerFrame));
		calibLeft = calibSize;
		calibT0 = Stopwatch.GetTimestamp();
		sinceYield = 0;
	}

	public bool AfterStep(Action sync)
	{
		sinceYield++;
		if (calibLeft > 0)
		{
			calibLeft--;
			if (calibLeft == 0)
			{
				sync();
				double batchMs = (Stopwatch.GetTimestamp() - calibT0) * 1000.0 / Stopwatch.Frequency;
				calibrations++;
				MsPerStep = batchMs / calibSize;
				sumBatchMs += batchMs;
				if (batchMs > maxBatchMs)
				{
					maxBatchMs = batchMs;
				}
				int num = ((MsPerStep > 1E-06) ? ((int)Math.Floor(budgetMs / MsPerStep)) : MaxStepsPerFrame);
				num = Math.Max(1, Math.Min(MaxStepsPerFrame, num));
				StepsPerFrame = num;
				if (num < minStepsPerFrame)
				{
					minStepsPerFrame = num;
				}
				if (num > maxStepsPerFrame)
				{
					maxStepsPerFrame = num;
				}
				if (first)
				{
					first = false;
					calibSize = Math.Min(CalibrationBatch, num);
					calibLeft = calibSize;
					calibT0 = Stopwatch.GetTimestamp();
					yields++;
					sinceYield = 0;
					return true;
				}
			}
			if (calibLeft > 0)
			{
				return false;
			}
		}
		if (sinceYield >= StepsPerFrame)
		{
			yields++;
			sinceYield = 0;
			return true;
		}
		return false;
	}

	public void Yielded()
	{
		sinceYield = 0;
	}

	public string Passport()
	{
		return string.Format("темп шагов: бюджет {0:F0} мс/кадр, шаг ≈ {1:F2} мс ⇒ {2} шаг./кадр ", budgetMs, MsPerStep, StepsPerFrame) + string.Format("(за прогон {0}…{1}), отдач {2}, ", minStepsPerFrame == int.MaxValue ? 0 : minStepsPerFrame, maxStepsPerFrame, yields) + string.Format("калибровок {0}, наибольшая пачка {1:F1} мс", calibrations, maxBatchMs);
	}
}
