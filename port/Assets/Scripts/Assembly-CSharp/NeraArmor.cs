using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public static class NeraArmor
{
	public const float ReferenceFrontThicknessM = 0.003f;

	public const float ReferenceRearThicknessM = 0.005f;

	public const float DefaultElastomerImpulse = 4710f;

	public const float ElastomerImpulseUpperBound = 18840f;

	public const float PlateSpeedBandMinMS = 100f;

	public const float PlateSpeedBandMaxMS = 250f;

	public static readonly ArmorPlate.ArmorType[] AllowedPlateMaterials = new ArmorPlate.ArmorType[]
	{
		ArmorPlate.ArmorType.RHA,
		ArmorPlate.ArmorType.HHA,
		ArmorPlate.ArmorType.Titanium
	};

	public const float PolyurethanePhaseIIThresholdPa = 2.1E+10f;

	public const float NeoprenePublishedLimitPa = 5E+09f;

	private static readonly InterlayerShock[] polyurethanePhases = new InterlayerShock[]
	{
		new InterlayerShock
		{
			name = "Полиуретан, фаза I",
			density = 1265f,
			c0 = 2486f,
			s = 1.577f,
			upperLimitPa = PolyurethanePhaseIIThresholdPa,
			note = "до фазового перехода при 21 ГПа"
		},
		new InterlayerShock
		{
			name = "Полиуретан, фаза II",
			density = 1265f,
			c0 = 4040f,
			s = 0.97f,
			upperLimitPa = float.PositiveInfinity,
			note = "после фазового перехода при 21 ГПа"
		}
	};

	private static readonly InterlayerShock[] neoprenePhases = new InterlayerShock[]
	{
		new InterlayerShock
		{
			name = "Полихлоропрен (неопрен)",
			density = 1420f,
			c0 = 1400f,
			s = 4f,
			upperLimitPa = NeoprenePublishedLimitPa,
			note = "выше 5 ГПа данных НЕ НАЙДЕНО — не путать с «ведёт себя так же»"
		}
	};

	public static bool IsAllowedPlateMaterial(ArmorPlate.ArmorType t)
	{
		for (int i = 0; i < AllowedPlateMaterials.Length; i++)
		{
			if (AllowedPlateMaterials[i] == t)
			{
				return true;
			}
		}
		return false;
	}

	public static ArmorPlate.ArmorType ClampPlateMaterial(ArmorPlate.ArmorType t)
	{
		return IsAllowedPlateMaterial(t) ? t : ArmorPlate.ArmorType.RHA;
	}

	public static TateModel.Target PlateTarget(ArmorPlate.ArmorType t)
	{
		t = ClampPlateMaterial(t);
		if (t == ArmorPlate.ArmorType.HHA)
		{
			return TateModel.HHA();
		}
		if (t == ArmorPlate.ArmorType.Titanium)
		{
			return TateModel.Titanium();
		}
		return TateModel.RHA();
	}

	public static float PlateDensity(ArmorPlate.ArmorType t)
	{
		return PlateTarget(t).density;
	}

	public static string PlateMaterialNote(ArmorPlate.ArmorType t)
	{
		t = ClampPlateMaterial(t);
		if (t == ArmorPlate.ArmorType.HHA)
		{
			return "⚠ HHA: трескается вместо выпучивания — В МОДЕЛИ НЕ УЧТЕНО (оценка завышена)";
		}
		if (t == ArmorPlate.ArmorType.Titanium)
		{
			return "легче стали ⇒ БЫСТРЕЕ при той же толщине (×1,77 к RHA)";
		}
		return "вязкая, хорошо выпучивается — обычный выбор";
	}

	private static InterlayerShock[] PhasesOf(InterlayerMaterial m)
	{
		return m == InterlayerMaterial.Neoprene ? neoprenePhases : polyurethanePhases;
	}

	public static ContinuumMaterialId RowFor(InterlayerMaterial m)
	{
		return m == InterlayerMaterial.Neoprene ? ContinuumMaterialId.Neoprene_Interlayer : ContinuumMaterialId.Polyurethane_Interlayer;
	}

	public static IList<InterlayerShock> ShockPhases(InterlayerMaterial m)
	{
		return new ReadOnlyCollection<InterlayerShock>(PhasesOf(m));
	}

	public static InterlayerShock ShockAt(InterlayerMaterial m, float pressurePa)
	{
		InterlayerShock[] phases = PhasesOf(m);
		for (int i = 0; i < phases.Length; i++)
		{
			if (phases[i].upperLimitPa >= pressurePa)
			{
				return phases[i];
			}
		}
		return phases[phases.Length - 1];
	}

	public static bool IsWithinRange(InterlayerShock row, float pressurePa)
	{
		return row.upperLimitPa >= pressurePa;
	}

	public static float ShockVelocityMS(InterlayerShock row, float particleVelocityMS)
	{
		return row.c0 + row.s * particleVelocityMS;
	}

	public static string InterlayerName(InterlayerMaterial m)
	{
		if (m == InterlayerMaterial.Neoprene)
		{
			return "Полихлоропрен (неопрен)";
		}
		if (m == InterlayerMaterial.NaturalRubber)
		{
			return "Натуральная резина (адиабата — подстановка полиуретана)";
		}
		return "Полиуретан";
	}

	public static float InterlayerDensity(InterlayerMaterial m)
	{
		return PhasesOf(m)[0].density;
	}

	public static string InterlayerNote(InterlayerMaterial m)
	{
		return InterlayerPassport(m) + " " + InterlayerModeNote(m);
	}

	private static string InterlayerPassport(InterlayerMaterial m)
	{
		ContinuumProperties p = ContinuumMaterials.Get(RowFor(m));
		string source = m == InterlayerMaterial.Neoprene ? "Millett et al. 2004; повтор до сверки со статьёй" : "Carter & Marsh, плотный полиуретан, MEASURED; Grüneisen, T_melt и C_p — ASSUMED";
		return string.Format("{0}: ударная адиабата — строка реестра Super: ρ0 = {1:F0} кг/м³, ", InterlayerName(m), p.rho0) + string.Format("C0 = {0:F0} м/с, S = {1:F2} ({2}). ", p.C0, p.S, source) + "Таблица эластомеров аналитики (иссл. №10 §A5) — УТОЧНЯЕТСЯ: источника на диске нет, её числа не показываются, в счёт выстрела она не входит.";
	}

	private static string InterlayerModeNote(InterlayerMaterial m)
	{
		string registry = SuperNera.RegistryNameFor(m);
		string mode;
		if (registry == "POLY_INTERLAYER" && m != InterlayerMaterial.Polyurethane)
		{
			mode = "В SUPER прослойка считается строкой полиуретана «" + registry + "» — объявленная подстановка, от полиуретана не отличается.";
		}
		else
		{
			mode = "В SUPER прослойка считается СВОЕЙ строкой реестра «" + registry + "», и там материал числа выстрела МЕНЯЕТ.";
		}
		return "⚠ В ОБЫЧНОМ РЕЖИМЕ ЧИСЕЛ ВЫСТРЕЛА ЭТОТ ВЫБОР НЕ МЕНЯЕТ: импульса прослойки по материалам в литературе НЕТ (NOT FOUND), а вывести его из адиабаты можно только гидрокодом — записано как свойство, а не подставлено как эффект. " + mode;
	}

	public static float PlateVelocityMS(float impulsePerArea, float density, float thicknessM)
	{
		if (density <= 0f || thicknessM <= 0f)
		{
			return 0f;
		}
		return Mathf.Max(impulsePerArea, 0f) / (density * thicknessM);
	}

	public static float FrontVelocityMS(NeraSpec n)
	{
		return n.blockExpended ? 0f : PlateVelocityMS(n.elastomerImpulsePerArea, n.frontDensity, n.frontThicknessM);
	}

	public static float RearVelocityMS(NeraSpec n)
	{
		return n.blockExpended ? 0f : PlateVelocityMS(n.elastomerImpulsePerArea, n.rearDensity, n.rearThicknessM);
	}

	public static float VelocityRatio(NeraSpec n)
	{
		float frontArealDensity = n.frontThicknessM * n.frontDensity;
		return frontArealDensity > 0f ? n.rearThicknessM * n.rearDensity / frontArealDensity : 0f;
	}

	public static NeraSpec DemoReference()
	{
		return new NeraSpec
		{
			name = "НДЗ 3/5 RHA (опорная)",
			enabled = true,
			frontThicknessM = ReferenceFrontThicknessM,
			rearThicknessM = ReferenceRearThicknessM,
			frontDensity = 7850f,
			rearDensity = 7850f,
			interlayerThicknessM = 0.006f,
			plateExtentM = 0.25f,
			elastomerImpulsePerArea = DefaultElastomerImpulse
		};
	}

	public static ReactiveArmorSpec PlateJetSpec(NeraSpec n, bool front)
	{
		return new ReactiveArmorSpec
		{
			name = (n.name ?? "НДЗ") + (front ? " / передняя" : " / задняя"),
			enabled = n.enabled,
			plateThicknessM = front ? n.frontThicknessM : n.rearThicknessM,
			plateVelocityMS = front ? FrontVelocityMS(n) : RearVelocityMS(n),
			plateExtentM = n.plateExtentM,
			plateDensity = front ? n.frontDensity : n.rearDensity,
			plateCount = 1,
			cassetteExpended = false,
			interChargeDelayS = 0f
		};
	}

	public static float EffectiveObliquityRad(NeraSpec n, float blockObliquityRad)
	{
		return Mathf.Clamp(blockObliquityRad + n.mountAngleRad, 0f, 1.5620697f);
	}

	public static NeraOutcome Apply(ShapedChargeSpec jet, NeraSpec n, float blockObliquityRad, float standoffToBlockM, int elementCount = 300)
	{
		elementCount = Mathf.Max(elementCount, 1);
		NeraOutcome o = new NeraOutcome
		{
			frontVelocityMS = FrontVelocityMS(n),
			rearVelocityMS = RearVelocityMS(n),
			velocityRatio = VelocityRatio(n),
			effectiveObliquityRad = EffectiveObliquityRad(n, blockObliquityRad),
			survivingJet = jet,
			survivingElementCount = elementCount
		};
		if (!n.enabled)
		{
			return o;
		}
		o.front = ReactiveArmor.Apply(jet, PlateJetSpec(n, true), o.effectiveObliquityRad, standoffToBlockM, elementCount);
		o.consumedLengthM = o.front.eraConsumedLengthM;
		o.elementsRetired = o.front.eraElementsRetired;
		o.survivingJet = o.front.survivingJet;
		o.survivingElementCount = o.front.survivingElementCount;
		if (o.front.eraFullyDefeated || o.front.survivingElementCount <= 0)
		{
			o.fullyDefeated = true;
			return o;
		}
		float cos = Mathf.Cos(o.effectiveObliquityRad);
		float gap = cos > 0.0001f ? (n.interlayerThicknessM + n.frontThicknessM) / cos : 0f;
		o.rear = ReactiveArmor.Apply(o.front.survivingJet, PlateJetSpec(n, false), o.effectiveObliquityRad, standoffToBlockM + gap, elementCount);
		o.consumedLengthM += o.rear.eraConsumedLengthM;
		o.elementsRetired += o.rear.eraElementsRetired;
		o.survivingJet = o.rear.survivingJet;
		o.survivingElementCount = o.rear.survivingElementCount;
		o.fullyDefeated = o.rear.eraFullyDefeated || o.rear.survivingElementCount <= 0;
		return o;
	}

	public static ReactiveArmorRodSpec PlateRodSpec(NeraSpec n, bool front, float standoffBehindM)
	{
		ReactiveArmorRodSpec spec = ReactiveArmor.DemoRodPlaceholder();
		spec.name = (n.name ?? "НДЗ") + (front ? " / передняя" : " / задняя");
		spec.enabled = n.enabled;
		spec.plateThicknessM = front ? n.frontThicknessM : n.rearThicknessM;
		spec.plateVelocityMS = front ? FrontVelocityMS(n) : RearVelocityMS(n);
		spec.interlayerGapM = n.interlayerThicknessM;
		spec.plateExtentM = n.plateExtentM;
		spec.standoffBehindM = standoffBehindM;
		spec.plateDensity = front ? n.frontDensity : n.rearDensity;
		spec.cassetteExpended = false;
		return spec;
	}

	public static NeraRodOutcome ApplyToRod(NeraSpec n, float blockObliquityRad, float rodLengthM, float rodDiameterM, float rodDensity, float rodVelocityMS, float standoffBehindM = 0.5f)
	{
		float obliquity = EffectiveObliquityRad(n, blockObliquityRad);
		NeraRodOutcome o = new NeraRodOutcome
		{
			frontVelocityMS = FrontVelocityMS(n),
			rearVelocityMS = RearVelocityMS(n)
		};
		o.front = ReactiveArmor.ApplyToRod(PlateRodSpec(n, true, standoffBehindM), obliquity, rodLengthM, rodDiameterM, rodDensity, rodVelocityMS);
		o.rear = ReactiveArmor.ApplyToRod(PlateRodSpec(n, false, standoffBehindM), obliquity, rodLengthM, rodDiameterM, rodDensity, rodVelocityMS);
		RodEraOutcome baseline = ReactiveArmor.ApplyToRod(ReactiveArmor.DemoRodPlaceholder(), obliquity, rodLengthM, rodDiameterM, rodDensity, rodVelocityMS);
		o.forceRatioToEra = baseline.forceN > 0f ? o.front.forceN / baseline.forceN : 0f;
		return o;
	}
}
