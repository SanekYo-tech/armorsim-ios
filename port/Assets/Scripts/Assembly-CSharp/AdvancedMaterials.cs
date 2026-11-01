using System;
using UnityEngine;

public static class AdvancedMaterials
{
	public const float B4CDensity = 2520f;

	public const float B4CFullRtPa = 6E+09f;

	public const float B4CThresholdPa = 4E+10f;

	public const float B4CHelLoPa = 1.4E+10f;

	public const float B4CHelHiPa = 1.9E+10f;

	public const float B4CDegradedRtPa = 1.2E+09f;

	public const float TiB2Density = 4500f;

	public const float TiB2InitialRtPa = 1.07E+10f;

	public const float TiB2ComminutedRtPa = 5.8E+09f;

	public const float TiB2PathAverageRtPa = 8.4E+09f;

	public const float TiB2DecayLengthM = 0.04f;

	public const int DefaultDepthSlices = 24;

	public const string TiB2ToughnessNote = "Вязкость разрушения TiB₂ в R_t НЕ входит: она управляет скоростью спада, а не уровнем. Многократное попадание вне области одновыстрельной модели.";

	public const string NegativeResultDuRt = "1) R_t ОБЕДНЁННОГО УРАНА КАК МИШЕНИ: измерений в открытой литературе НЕТ. Наше значение 5.8 ГПа — DERIVED по расширению полости (Hill 1950 / Tate 1986) с поверкой метода на RHA (+5%). Выводить его из ПЛОТНОСТИ нельзя: она уже работает в инерционном члене ½·ρ_t·u², и это был бы двойной счёт.";

	public const string NegativeResultNeraImpulse = "2) СКОРОСТИ ПЛАСТИН НДЗ И ИМПУЛЬС ЭЛАСТОМЕРА: измерений НЕТ. Сохранение импульса с ОДНИМ подогнанным импульсом остаётся единственной пригодной трактовкой; импедансный механизм замкнутой формы, отделимой от УРС в гидрокоде, не имеет.";

	public const string NegativeResultThorConstants = "3) КОНСТАНТЫ THOR ДЛЯ АЛЮМИНИЯ И ТИТАНА: исторически вычислены, таблицы в открытых источниках ОТСУТСТВУЮТ. Экстраполировать стальные нельзя — это размерные регрессионные коэффициенты, а не физические параметры.";

	public const string JetWiringGap = "B₄C против струи: закон обвала есть и проверен, но ядро струи принимает R_t одним числом на весь заряд ⇒ поэлементная деградация НЕ ПРОВЕДЕНА. Расчёт идёт с полным R_t (оценка СВЕРХУ по защите). Для проводки нужен поэлементный R_t в ShapedChargeCore.";

	public static float StagnationPressurePa(float penetratorDensity, float vMS, float uMS, float ypPa)
	{
		return Mathf.Max(penetratorDensity, 0f) * 0.5f * (vMS - uMS) * (vMS - uMS) + Mathf.Max(ypPa, 0f);
	}

	public static float B4CResistancePa(float stagnationPressurePa)
	{
		return stagnationPressurePa >= B4CThresholdPa ? B4CDegradedRtPa : B4CFullRtPa;
	}

	public static bool B4CTriggersCollapse(TateModel.Penetrator p, float vMS, out float pressurePa, out float uMS)
	{
		TateModel.Target b4c = TateModel.BoronCarbide();
		uMS = Mathf.Clamp(TateModel.PenetrationVelocity(p, b4c, vMS), 0f, vMS);
		pressurePa = StagnationPressurePa(p.density, vMS, uMS, p.Yp);
		return pressurePa >= B4CThresholdPa;
	}

	public static bool B4CTriggersCollapseForJet(float jetDensity, float vMS, out float pressurePa, out float uMS)
	{
		TateModel.Target b4c = TateModel.BoronCarbide();
		uMS = ShapedChargeCore.InterfaceVelocity(vMS, jetDensity, b4c.density, b4c.Rt);
		pressurePa = StagnationPressurePa(jetDensity, vMS, uMS, 0f);
		return pressurePa >= B4CThresholdPa;
	}

	public static float B4CJetThresholdVelocityMS(float jetDensity, float loMS = 500f, float hiMS = 20000f)
	{
		if (!B4CTriggersCollapseForJet(jetDensity, hiMS, out float _, out float _))
		{
			return float.NaN;
		}
		if (B4CTriggersCollapseForJet(jetDensity, loMS, out float _, out float _))
		{
			return loMS;
		}
		for (int i = 0; i < 60; i++)
		{
			float mid = (loMS + hiMS) * 0.5f;
			if (B4CTriggersCollapseForJet(jetDensity, mid, out float _, out float _))
			{
				hiMS = mid;
			}
			else
			{
				loMS = mid;
			}
		}
		return (loMS + hiMS) * 0.5f;
	}

	public static float B4CDegradedElementFraction(ShapedChargeSpec jet, int elementCount = 300)
	{
		if (jet.vTail >= jet.vTip)
		{
			return 0f;
		}
		float threshold = B4CJetThresholdVelocityMS(jet.material.density);
		if (float.IsNaN(threshold) || threshold >= jet.vTip)
		{
			return 0f;
		}
		if (jet.vTail >= threshold)
		{
			return 1f;
		}
		int count = Math.Max(1, elementCount);
		float dv = (jet.vTip - jet.vTail) / count;
		int degraded = 0;
		for (int i = 0; i < count; i++)
		{
			float velocity = jet.vTail + (i + 0.5f) * dv;
			if (velocity >= threshold)
			{
				degraded++;
			}
		}
		return (float)degraded / count;
	}

	public static TateModel.Target ResolveForImpact(ArmorPlate.ArmorType type, TateModel.Target row, TateModel.Penetrator p, float velocityMS, out bool degraded, out float pressurePa)
	{
		degraded = false;
		pressurePa = 0f;
		if (type == ArmorPlate.ArmorType.BoronCarbide && B4CTriggersCollapse(p, velocityMS, out pressurePa, out float _))
		{
			degraded = true;
			row.Rt = B4CDegradedRtPa;
			row.name += " (аморфизован)";
		}
		return row;
	}

	public static float TiB2ResistancePa(float depthM, float decayLengthM = 0.04f)
	{
		decayLengthM = Mathf.Max(decayLengthM, 1E-06f);
		depthM = Mathf.Max(depthM, 0f);
		return TiB2ComminutedRtPa + (TiB2InitialRtPa - TiB2ComminutedRtPa) * Mathf.Exp(-depthM / decayLengthM);
	}

	public static PenetrationOutcome CalculateWithDepthDecay(TateModel.Penetrator core, TateModel.Target row, Func<float, float> rtAtDepth, float velocity, float angleDeg, float thicknessMM, float rodLengthIn, float rodLengthOriginal, float kYaw = 1f, int slices = 24)
	{
		slices = Math.Max(1, slices);
		float cos = Mathf.Max(Mathf.Cos(angleDeg * Mathf.Deg2Rad), 0.0001f);
		float effectiveThicknessMM = thicknessMM / cos;
		float sliceMM = effectiveThicknessMM / slices;
		float currentVelocity = velocity;
		float currentLength = rodLengthIn;
		float depthMM = 0f;
		PenetrationOutcome outcome = new PenetrationOutcome
		{
			result = PenetrationResult.Penetrated,
			residualVelocity = velocity,
			residualLengthM = rodLengthIn,
			noseAreaOut = 1f
		};
		for (int i = 0; i < slices; i++)
		{
			TateModel.Target sliceRow = row;
			sliceRow.Rt = rtAtDepth((depthMM + sliceMM * 0.5f) / 1000f);
			PenetrationOutcome step = PenetrationCore.Calculate(core, sliceRow, currentVelocity, angleDeg, sliceMM * cos, currentLength, rodLengthOriginal, false, kYaw);
			outcome.erosionOnsetMax = Mathf.Max(outcome.erosionOnsetMax, step.erosionOnsetMax);
			outcome.noseAreaOut = step.noseAreaOut;
			currentVelocity = step.residualVelocity;
			currentLength = step.residualLengthM;
			if (step.result == PenetrationResult.Stopped)
			{
				outcome.result = PenetrationResult.Stopped;
				outcome.residualLengthM = step.residualLengthM;
				outcome.penetrationDepthMM = depthMM + step.penetrationDepthMM;
				break;
			}
			depthMM += sliceMM;
			if (i == slices - 1)
			{
				outcome.result = PenetrationResult.Penetrated;
				outcome.residualVelocity = currentVelocity;
				outcome.residualLengthM = currentLength;
				outcome.penetrationDepthMM = effectiveThicknessMM;
			}
		}
		outcome.erosionFraction = Mathf.Clamp01((rodLengthIn - currentLength) / Mathf.Max(rodLengthOriginal, 0.0001f));
		return outcome;
	}

	public static bool HasDepthDecay(ArmorPlate.ArmorType t)
	{
		return t == ArmorPlate.ArmorType.TitaniumDiboride;
	}

	public static Func<float, float> DepthLaw(ArmorPlate.ArmorType t)
	{
		if (t != ArmorPlate.ArmorType.TitaniumDiboride)
		{
			return null;
		}
		return depth => TiB2ResistancePa(depth);
	}
}
