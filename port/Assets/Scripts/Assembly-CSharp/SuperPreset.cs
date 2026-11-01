using System;
using UnityEngine;

public static class SuperPreset
{
	public const float TipGapM = 0.002f;

	public static float ParticleBudget;

	public const float DefaultPlateSideM = 0.2f;

	public const float FastSpacingM = 0.002f;

	public const float FastDurationS = 0.0002f;

	public const float FineSpacingM = 0.001f;

	public const float FineDurationS = 0.0003f;

	public static float SpacingM;

	public static float DurationS;

	public const float SpacingMinM = 0.0002f;

	public const float SpacingMaxM = 0.008f;

	public const float DurationMinS = 5E-05f;

	public const float DurationMaxS = 0.01f;

	public static float TriggerTimeS;

	public const float TriggerTimeMinS = 0f;

	public const float TriggerTimeMaxS = 0.01f;

	public const float MarginBaseM = 0.07f;

	public const float MarginMaxM = 0.22f;

	public static double BytesPerParticle => SuperMemory.BytesPerParticle(SuperScatterRoute.FixedPointAtomics);

	public static float VramShare => 0.8f;

	public static double ParticleCeiling
	{
		get
		{
			int memoryMB = SystemInfo.graphicsMemorySize;
			if (memoryMB < 1)
			{
				memoryMB = 2048;
			}
			return (double)memoryMB * 1024.0 * 1024.0 * 0.800000011920929 / BytesPerParticle;
		}
	}

	public static bool TriggerTimeSet => !float.IsNaN(TriggerTimeS) && !float.IsInfinity(TriggerTimeS) && TriggerTimeS >= 0f;

	public static double ParticlesFor(float sideM, float totalThicknessM, float spacingM, float rodDiaM = 0f, float rodLenM = 0f)
	{
		if (spacingM <= 1E-06f)
		{
			return 0.0;
		}
		double sideCells = sideM / spacingM;
		double thicknessCells = totalThicknessM / spacingM;
		double rodRadiusCells = (double)rodDiaM * 0.5 / spacingM;
		double rodLengthCells = rodLenM / spacingM;
		return Math.PI * rodRadiusCells * rodRadiusCells * rodLengthCells + thicknessCells * sideCells * sideCells;
	}

	public static float SpacingForBudget(float sideM, float totalThicknessM, float rodDiaM = 0f, float rodLenM = 0f)
	{
		if (sideM <= 0f || totalThicknessM <= 0f)
		{
			return SpacingM;
		}
		double budget = Math.Max(1f, ParticleBudget);
		float lo = SpacingMinM;
		float hi = SpacingMaxM;
		if (ParticlesFor(sideM, totalThicknessM, hi, rodDiaM, rodLenM) > budget)
		{
			return hi;
		}
		if (ParticlesFor(sideM, totalThicknessM, lo, rodDiaM, rodLenM) <= budget)
		{
			return lo;
		}
		for (int i = 0; i < 20; i++)
		{
			float mid = (hi + lo) * 0.5f;
			if (ParticlesFor(sideM, totalThicknessM, mid, rodDiaM, rodLenM) <= budget)
			{
				hi = mid;
			}
			else
			{
				lo = mid;
			}
		}
		return hi;
	}

	public static float CellsAcross(float thinnestM, float spacingM)
	{
		if (spacingM <= 1E-06f)
		{
			return 0f;
		}
		return thinnestM / spacingM;
	}

	public static void ApplyFast()
	{
		SpacingM = FastSpacingM;
		DurationS = FastDurationS;
	}

	public static void ApplyFine()
	{
		SpacingM = FineSpacingM;
		DurationS = FineDurationS;
	}

	public static float TriggerTimeFor(float rowValueS)
	{
		if (TriggerTimeSet)
		{
			return Mathf.Clamp(TriggerTimeS, TriggerTimeMinS, TriggerTimeMaxS);
		}
		return Mathf.Max(TriggerTimeMinS, rowValueS);
	}

	public static void TriggerTimeFromRow()
	{
		TriggerTimeS = float.NaN;
	}

	public static float MarginFor(float maxAbsAngleDeg, float velocityMps, float durationS)
	{
		return Mathf.Clamp(MarginBaseM + Mathf.Tan(Mathf.Abs(maxAbsAngleDeg) * Mathf.Deg2Rad) * velocityMps * 0.5f * durationS, MarginBaseM, MarginMaxM);
	}

	static SuperPreset()
	{
		ParticleBudget = 1000000f;
		SpacingM = FastSpacingM;
		DurationS = FastDurationS;
		TriggerTimeS = float.NaN;
	}
}
