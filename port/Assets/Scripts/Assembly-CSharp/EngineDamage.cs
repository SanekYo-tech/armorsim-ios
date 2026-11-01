using UnityEngine;

public static class EngineDamage
{
	public const float ReferenceEnergyJ = 20000f;

	public const float DamagedThreshold = 0.25f;

	public const float DisabledThreshold = 1f;

	public static float DepositedEnergyJ(float energyInJ, float energyOutJ)
	{
		return Mathf.Max(0f, energyInJ - Mathf.Max(0f, energyOutJ));
	}

	public static float Accumulate(float damage, float depositedJ)
	{
		return Mathf.Max(0f, damage) + Mathf.Max(0f, depositedJ) / ReferenceEnergyJ;
	}

	public static EngineState StateOf(float damage)
	{
		if (damage >= DisabledThreshold)
		{
			return EngineState.Disabled;
		}
		return (damage >= DamagedThreshold) ? EngineState.Damaged : EngineState.Intact;
	}

	public static string StateName(EngineState s)
	{
		if (s == EngineState.Damaged)
		{
			return "ПОВРЕЖДЁН";
		}
		if (s == EngineState.Disabled)
		{
			return "ВЫВЕДЕН ИЗ СТРОЯ";
		}
		return "ЦЕЛ";
	}

	public static Color StateColor(EngineState s)
	{
		if (s == EngineState.Damaged)
		{
			return ResultPalette.Slide;
		}
		if (s == EngineState.Disabled)
		{
			return ResultPalette.Stopped;
		}
		return ResultPalette.Miss;
	}

	public static string Report(int hits, float depositedJ, float damage)
	{
		string[] array = new string[5];
		array[0] = string.Format("Моторный отсек: осколков вошло {0}, внесено {1:F1} кДж ⇒ ", hits, depositedJ / 1000f);
		array[1] = string.Format("повреждение {0:F2} (опора {1:F0} кДж, ASSUMED) ⇒ ", damage, ReferenceEnergyJ / 1000f);
		array[2] = "ВЕРОЯТНО «";
		array[3] = StateName(StateOf(damage));
		array[4] = "». ⚠ Внутреннее устройство не моделируется: куда именно попало — модель не знает и знать не может (однородное приближение).";
		return string.Concat(array);
	}
}
