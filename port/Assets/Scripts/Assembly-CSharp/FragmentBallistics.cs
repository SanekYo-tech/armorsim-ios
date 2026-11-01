using System;
using UnityEngine;

public static class FragmentBallistics
{
	public static class ThorFragment
	{
		public const float FeetPerMetre = 3.28084f;

		public const float InchPerMetre = 39.37008f;

		public const float GrainsPerKg = 15432.358f;

		public const float SqInchPerSqMetre = 1550.003f;

		public const float DExponentAssumed = 1f;

		public const float FitMaxVelocityFps = 12000f;

		public const float FitMinMassGrains = 5f;

		public const float FitMaxMassGrains = 825f;

		public const float ShatterVelocityMS = 2000f;

		public const string FatepenNote = "Выше порога дробления THOR завышает остаточную массу и скорость; правильная модель для этого режима — FATEPEN, в проекте её НЕТ.";

		public const bool ConstantsVerified = false;

		public const string ConstantsAuditNote = "THOR-47: возвращены только a, b, c. Без множителя V_s^e потеря скорости не зависит от скорости встречи и составляет ~0.01 м/с на 6 мм стали ⇒ уравнение реализовано и проверяемо, но в конвейер НЕ подставлено (ConstantsVerified = false).";

		public static float MpsToFps(float mps)
		{
			return mps * FeetPerMetre;
		}

		public static float FpsToMps(float fps)
		{
			return fps / FeetPerMetre;
		}

		public static float MToInch(float m)
		{
			return m * InchPerMetre;
		}

		public static float KgToGrains(float kg)
		{
			return kg * GrainsPerKg;
		}

		public static float M2ToInch2(float m2)
		{
			return m2 * SqInchPerSqMetre;
		}

		public static void ConstantsMildSteel(bool slender, out float a, out float b, out float c)
		{
			if (slender)
			{
				a = 1.1f;
				b = 0.8f;
				c = 1.45f;
			}
			else
			{
				a = 1.12f;
				b = 0.52f;
				c = 1.29f;
			}
		}

		public static bool InValidRange(float velocityMS, float massKg)
		{
			float grains = KgToGrains(massKg);
			return velocityMS <= ShatterVelocityMS && MpsToFps(velocityMS) <= FitMaxVelocityFps && grains >= FitMinMassGrains && grains <= FitMaxMassGrains;
		}

		public static float ResidualVelocityMS(float velocityMS, float massKg, float areaM2, float plateThicknessM, float obliquityRad, bool slender)
		{
			ConstantsMildSteel(slender, out float a, out float b, out float c);
			float thicknessIn = MToInch(plateThicknessM);
			float velocityFps = MpsToFps(velocityMS);
			float areaIn2 = M2ToInch2(areaM2);
			float grains = KgToGrains(massKg);
			obliquityRad = Mathf.Clamp(obliquityRad, 0f, 1.5533431f);
			if (thicknessIn <= 0f || areaIn2 <= 0f || grains <= 0f)
			{
				return velocityMS;
			}
			float loss = Mathf.Pow(1f / Mathf.Max(Mathf.Cos(obliquityRad), 0.001f), DExponentAssumed) * Mathf.Pow(areaIn2 * thicknessIn, a) * Mathf.Pow(10f, c) * Mathf.Pow(grains, 0f - b);
			velocityFps -= loss;
			return velocityFps > 0f ? FpsToMps(velocityFps) : 0f;
		}
	}

	public const float ShearFromYield = 0.6f;

	public static bool HasYieldRow(ArmorPlate.ArmorType t)
	{
		switch (t)
		{
		case ArmorPlate.ArmorType.RHA:
		case ArmorPlate.ArmorType.HHA:
		case ArmorPlate.ArmorType.Aluminum:
		case ArmorPlate.ArmorType.Titanium:
		case ArmorPlate.ArmorType.Tungsten:
		case ArmorPlate.ArmorType.ERA:
		case ArmorPlate.ArmorType.ERAHeavy:
		case ArmorPlate.ArmorType.CastSteel:
			return true;
		default:
			return false;
		}
	}

	public static float YieldStrengthPa(ArmorPlate.ArmorType t)
	{
		switch (t)
		{
		case ArmorPlate.ArmorType.HHA:
			return 1.5E+09f;
		case ArmorPlate.ArmorType.Aluminum:
			return 3E+08f;
		case ArmorPlate.ArmorType.Titanium:
			return 9E+08f;
		default:
			return 1E+09f;
		}
	}

	public static float ShearStrengthPa(ArmorPlate.ArmorType t)
	{
		return YieldStrengthPa(t) * ShearFromYield;
	}

	public static float SphereDiameterM(float massKg, float densityKgM3)
	{
		if (massKg <= 0f || densityKgM3 <= 0f)
		{
			return 0f;
		}
		return Mathf.Pow(massKg / densityKgM3 * 6f / Mathf.PI, 0.33333334f);
	}

	public static float DiameterFromAreaM(float areaM2)
	{
		if (areaM2 <= 0f)
		{
			return 0f;
		}
		return Mathf.Sqrt(areaM2 * 4f / Mathf.PI);
	}

	public static FragmentPenetration PlugShear(float massKg, float velocityMS, float fragDiameterM, float plateThicknessM, float yieldPa, float obliquityRad = 0f, bool outOfDomain = false)
	{
		FragmentPenetration r = new FragmentPenetration
		{
			energyJ = 0.5f * Mathf.Max(massKg, 0f) * velocityMS * velocityMS,
			shearStrengthPa = Mathf.Max(yieldPa, 0f) * ShearFromYield,
			outOfDomain = outOfDomain
		};
		obliquityRad = Mathf.Clamp(obliquityRad, 0f, 1.5533431f);
		r.losThicknessM = Mathf.Max(plateThicknessM, 0f) / Mathf.Max(Mathf.Cos(obliquityRad), 0.001f);
		if (massKg > 0f && velocityMS > 0f && fragDiameterM > 0f && r.losThicknessM > 0f)
		{
			r.workJ = r.shearStrengthPa * Mathf.PI * fragDiameterM * r.losThicknessM * r.losThicknessM;
			if (r.workJ < r.energyJ)
			{
				r.penetrates = true;
				r.residualVelocityMS = Mathf.Sqrt(Mathf.Max(velocityMS * velocityMS - 2f * r.workJ / massKg, 0f));
			}
			else
			{
				r.penetrates = false;
				r.residualVelocityMS = 0f;
			}
		}
		else
		{
			r.penetrates = r.losThicknessM >= 0f && massKg > 0f && velocityMS > 0f;
			r.residualVelocityMS = r.penetrates ? velocityMS : 0f;
		}
		return r;
	}

	public static bool ThorHasConstants(ArmorPlate.ArmorType t)
	{
		return t == ArmorPlate.ArmorType.RHA || t == ArmorPlate.ArmorType.HHA || t == ArmorPlate.ArmorType.ERA || t == ArmorPlate.ArmorType.ERAHeavy || t == ArmorPlate.ArmorType.CastSteel;
	}

	public static FragmentPenetration AgainstPlate(ArmorPlate.ArmorType type, float plateThicknessM, float massKg, float velocityMS, float fragDiameterM, float obliquityRad = 0f, bool slenderFragment = false)
	{
		FragmentPenetration r = PlugShear(massKg, velocityMS, fragDiameterM, plateThicknessM, YieldStrengthPa(type), obliquityRad, !HasYieldRow(type));
		r.model = FragmentModel.PlugShearAssumed;
		r.thorOutOfRange = ThorHasConstants(type);
		if (r.thorOutOfRange)
		{
			ThorFragment.InValidRange(velocityMS, massKg);
		}
		return r;
	}

	public static string Describe(FragmentPenetration r)
	{
		if (r.model == FragmentModel.Thor)
		{
			return string.Format("THOR: E = {0:F1} Дж, снято {1:F1} Дж (путь {2:F1} мм) ⇒ ", r.energyJ, r.workJ, r.losThicknessM * 1000f) + (r.penetrates ? string.Format("ПРОБИЛ, v_ост = {0:F0} м/с", r.residualVelocityMS) : "ОСТАНОВЛЕН") + "  [THOR-47, мягкая сталь, FITTED]";
		}
		return string.Format("E = {0:F1} Дж против W_проб = {1:F1} Дж (τ = {2:F0} МПа, ", r.energyJ, r.workJ, r.shearStrengthPa / 1000000f) + string.Format("путь {0:F1} мм) ⇒ ", r.losThicknessM * 1000f) + (r.penetrates ? string.Format("ПРОБИЛ, v_ост = {0:F0} м/с", r.residualVelocityMS) : "ОСТАНОВЛЕН") + (r.outOfDomain ? "  ⚠ КРИТЕРИЙ ВНЕ ОБЛАСТИ: у этой мишени нет предела текучести (перенос строки RHA)" : "") + (r.thorOutOfRange ? "  ⚠ СТАЛЬ, НО ВНЕ ОБЛАСТИ ФИТА THOR (скорость/масса) — запасной путь" : "") + "  [ASSUMED, срез пробки]";
	}
}
