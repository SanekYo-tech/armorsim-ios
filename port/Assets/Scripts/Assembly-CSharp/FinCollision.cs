using UnityEngine;

public static class FinCollision
{
	public static float FootprintMajorAxisM(float diameterM, float effectiveAngleDeg)
	{
		float num = Mathf.Clamp(Mathf.Abs(effectiveAngleDeg), 0f, 89f) * Mathf.Deg2Rad;
		return diameterM / Mathf.Max(Mathf.Sin(num), 0.001f);
	}

	public static FinStrikeOutcome Resolve(FinEnvelope env, float holeDiameterM)
	{
		if (!env.present)
		{
			return FinStrikeOutcome.NoFins;
		}
		if (env.stripped)
		{
			return FinStrikeOutcome.AlreadyStripped;
		}
		if (Mathf.Max(holeDiameterM, 0f) < env.spanM)
		{
			return FinStrikeOutcome.Strikes;
		}
		return FinStrikeOutcome.PassesThrough;
	}

	public static float ArrivalDelayS(FinEnvelope env, float speedMps)
	{
		if (speedMps > 1e-06f)
		{
			return env.rootOffsetM / speedMps;
		}
		return 0f;
	}

	public static string OutcomeName(FinStrikeOutcome o)
	{
		switch (o)
		{
		case FinStrikeOutcome.NoFins:
			return "оперения нет";
		case FinStrikeOutcome.AlreadyStripped:
			return "оперение уже сорвано";
		case FinStrikeOutcome.PassesThrough:
			return "прошло сквозь отверстие";
		default:
			return "СРЫВ: бьёт в вещество преграды";
		}
	}
}
