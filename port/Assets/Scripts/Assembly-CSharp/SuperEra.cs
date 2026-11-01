using System;
using System.Collections.Generic;

public static class SuperEra
{
	public sealed class Cassette
	{
		public List<SuperPlateSpec> plates;

		public int frontBody;

		public int rearBody;

		public int armourBody;

		public float bakedPlateM;

		public float catalogPlateM;

		public float gapM;

		public float velocityMps;

		public float extentM;

		public float gapFlyersM;

		public float gapArmourM;

		public float frontFaceOnAxisM;

		public float rodTipZM;

		public float initiationDelayS;

		public float impulseErrorFraction;

		public float thicknessErrorFraction;

		public float energyErrorFraction;

		public string refuseKey;

		public string refuseDetail;

		public Cassette()
		{
			plates = new List<SuperPlateSpec>();
			frontBody = -1;
			rearBody = -1;
			armourBody = -1;
		}
	}

	public enum OffArm
	{
		InertCassette = 0,
		NoCassette = 1
	}

	public sealed class SceneCassette
	{
		public SuperPlateSpec front;

		public SuperPlateSpec rear;

		public float bakedPlateM;

		public float catalogPlateM;

		public float gapM;

		public float velocityMps;

		public float gapFlyersM;

		public float initiationDelayS;

		public float thicknessErrorFraction;

		public float energyErrorFraction;

		public float impulseErrorFraction;

		public string refuseKey;

		public string refuseDetail;

		public object[] refuseArgs;

		public SceneCassette()
		{
			refuseArgs = new object[0];
		}
	}

	public const float TriggerSpacings = 1f;

	public const float AssumedDetonationMps = 7000f;

	public const float RampS = 5E-06f;

	public const float MinGapCells = 3f;

	public const float ThicknessTolerance = 0.1f;

	public static bool CoherentFlyerPlates;

	public const string KeyGapUnresolved = "super.era.refuse.gap_unresolved";

	public const string KeyThicknessQuantised = "super.era.refuse.thickness_quantised";

	public const string KeyMaterial = "super.era.refuse.material";

	public const string FlyerMaterial = "STEEL4340";

	public const float CatalogPlateDensity = 7850f;

	public const float RegisterPlateDensity = 7830f;

	public const float ArmourStandoffM = 0.02f;

	static SuperEra()
	{
		CoherentFlyerPlates = true;
	}

	public static float InitiationDelayS(float plateM, float gapM, float rodSpeedMps)
	{
		return plateM / Math.Max(1f, rodSpeedMps) + gapM / 7000f;
	}

	public static Cassette Build(EraPreset preset, SuperPlateSpec armour, float angleDeg, float spacing, float rodSpeedMps, float rodDiameterM)
	{
		Cassette c = new Cassette();
		c.catalogPlateM = preset.plateMM * 0.001f;
		c.gapM = preset.gapMM * 0.001f;
		c.velocityMps = preset.velocityMS;
		c.extentM = preset.extentMM * 0.001f;
		float required = 6f * spacing;
		if (required >= c.gapM)
		{
			c.refuseKey = "super.era.refuse.gap_unresolved";
			c.refuseDetail = string.Format("зазор ВВ {0:F1} мм при ячейке {1:F1} мм требует более ", preset.gapMM, required * 1000f) + string.Format("{0:F1} мм: пластины попали бы в один узел переноса и были бы ", required * 1000f) + string.Format("сварены с нулевого шага. Нужен шаг частиц мельче {0:F2} мм.", c.gapM / 3f * 0.5f * 1000f);
			return c;
		}
		int layers = Math.Max(1, UnityEngine.Mathf.CeilToInt(c.catalogPlateM / spacing));
		c.bakedPlateM = layers * spacing;
		c.thicknessErrorFraction = (c.bakedPlateM - c.catalogPlateM) / c.catalogPlateM;
		if (CoherentFlyerPlates)
		{
			c.impulseErrorFraction = 0f;
			c.energyErrorFraction = c.catalogPlateM / c.bakedPlateM - 1f;
			c.velocityMps = c.catalogPlateM / c.bakedPlateM * preset.velocityMS;
		}
		else
		{
			c.impulseErrorFraction = c.thicknessErrorFraction;
			if (Math.Abs(c.thicknessErrorFraction) > 0.1f)
			{
				c.refuseKey = "super.era.refuse.thickness_quantised";
				c.refuseDetail = string.Format("метаемая {0:F2} мм при шаге {1:F2} мм запекается в ", preset.plateMM, spacing * 1000f) + string.Format("{0} сл. = {1:F2} мм ⇒ удельный импульс ρ·t·V смещён на ", layers, c.bakedPlateM * 1000f) + string.Format("{0:P0} при допуске {1:P0}. ", c.impulseErrorFraction, 0.1f) + "Это ошибка ВХОДА механизма, а не погрешность счёта.";
				return c;
			}
		}
		float cosA = UnityEngine.Mathf.Cos(angleDeg * 0.0174532924f);
		if (cosA < 0.05f)
		{
			c.refuseKey = "super.era.refuse.gap_unresolved";
			c.refuseDetail = string.Format("наклон {0:F0}° — плиты почти параллельны выстрелу, ", angleDeg) + "раскладка по нормали вырождается.";
			return c;
		}
		SuperPlateSpec front = Plate(c.bakedPlateM, angleDeg, c.bakedPlateM, c.extentM, "STEEL4340", false, cosA);
		SuperPlateSpec rear = Plate(c.bakedPlateM, angleDeg, c.gapM + 2f * c.bakedPlateM, c.extentM, "STEEL4340", false, cosA);
		armour.alongAxisM = SuperBaker.AlongAxisForDepth(armour.thicknessM, c.gapM + 3f * c.bakedPlateM + 0.02f, angleDeg);
		c.plates.Add(front);
		c.plates.Add(rear);
		c.plates.Add(armour);
		c.gapFlyersM = Clear(c.plates[0], c.plates[1], cosA);
		c.gapArmourM = Clear(c.plates[1], c.plates[2], cosA);
		if (required >= c.gapFlyersM || required >= c.gapArmourM)
		{
			c.refuseKey = "super.era.refuse.gap_unresolved";
			c.refuseDetail = string.Format("при наклоне {0:F0}° просветы по нормали вышли ", angleDeg) + string.Format("{0:F1} мм (между метаемыми) и {1:F1} мм ", c.gapFlyersM * 1000f, c.gapArmourM * 1000f) + string.Format("(метаемая↔броня) при требуемых более {0:F1} мм.", required * 1000f);
			return c;
		}
		c.frontFaceOnAxisM = SuperBaker.FaceOnAxis(c.plates[0]);
		c.rodTipZM = SuperBaker.RodTipForClearance(c.plates[0], rodDiameterM, 0.002f);
		c.frontBody = 1;
		c.rearBody = 2;
		c.armourBody = 3;
		c.initiationDelayS = c.bakedPlateM / cosA / Math.Max(1f, rodSpeedMps) + c.gapM / 7000f;
		return c;
	}

	private static SuperPlateSpec Plate(float thicknessM, float angleDeg, float depthM, float sideM, string material, bool clamped, float cosA)
	{
		return new SuperPlateSpec
		{
			thicknessM = thicknessM,
			angleXDeg = angleDeg,
			sideM = sideM,
			material = material,
			clampedEdges = clamped,
			alongAxisM = SuperBaker.AlongAxisForDepth(thicknessM, depthM, angleDeg)
		};
	}

	private static float Clear(SuperPlateSpec a, SuperPlateSpec b, float cosA)
	{
		return (a.alongAxisM - a.thicknessM * 0.5f - (b.alongAxisM - b.thicknessM * 0.5f)) * cosA - a.thicknessM * 0.5f - b.thicknessM * 0.5f;
	}

	public static Cassette BuildOff(EraPreset preset, SuperPlateSpec armour, float angleDeg, float spacing, float rodSpeedMps, float rodDiameterM, OffArm arm)
	{
		Cassette c = Build(preset, armour, angleDeg, spacing, rodSpeedMps, rodDiameterM);
		if (c.refuseKey == null && arm != OffArm.InertCassette)
		{
			SuperPlateSpec savedArmour = c.plates[2];
			c.plates.Clear();
			c.plates.Add(savedArmour);
			c.frontBody = -1;
			c.rearBody = -1;
			c.armourBody = 1;
		}
		return c;
	}

	public static float SpacingCeilingForGap(float gapM)
	{
		return gapM / 6f;
	}

	public static SceneCassette BuildInScene(EraPreset preset, float sideM, float angleDeg, float frontFaceZ, float spacing, float rodSpeedMps)
	{
		SceneCassette c = new SceneCassette();
		c.catalogPlateM = preset.plateMM * 0.001f;
		c.gapM = preset.gapMM * 0.001f;
		c.velocityMps = preset.velocityMS;
		string presetName = SuperShotRunner.PresetName(preset.id, preset.displayName);
		float required = 6f * spacing;
		float spacingCeilingMm = SpacingCeilingForGap(c.gapM) * 1000f;
		if (required >= c.gapM)
		{
			c.refuseKey = "super.era.refuse.gap_unresolved";
			c.refuseArgs = new object[3]
			{
				presetName,
				preset.gapMM.ToString("F1", Localization.Culture),
				spacingCeilingMm.ToString("F2", Localization.Culture)
			};
			c.refuseDetail = string.Format("зазор ВВ {0:F1} мм при ячейке {1:F2} мм требует более ", preset.gapMM, 2f * spacing * 1000f) + string.Format("{0:F2} мм — нужен шаг частиц мельче {1:F2} мм.", required * 1000f, spacingCeilingMm);
			return c;
		}
		int layers = Math.Max(1, UnityEngine.Mathf.CeilToInt(c.catalogPlateM / spacing));
		c.bakedPlateM = layers * spacing;
		c.thicknessErrorFraction = (c.bakedPlateM - c.catalogPlateM) / c.catalogPlateM;
		if (CoherentFlyerPlates)
		{
			c.impulseErrorFraction = 0f;
			c.energyErrorFraction = c.catalogPlateM / c.bakedPlateM - 1f;
			c.velocityMps = c.catalogPlateM / c.bakedPlateM * preset.velocityMS;
		}
		else
		{
			c.impulseErrorFraction = c.thicknessErrorFraction;
			if (Math.Abs(c.thicknessErrorFraction) > 0.1f)
			{
				c.refuseKey = "super.era.refuse.thickness_quantised";
				c.refuseArgs = new object[4]
				{
					preset.plateMM.ToString("F2", Localization.Culture),
					(spacing * 1000f).ToString("F2", Localization.Culture),
					(c.bakedPlateM * 1000f).ToString("F2", Localization.Culture),
					c.thicknessErrorFraction.ToString("P0", Localization.Culture)
				};
				c.refuseDetail = string.Format("метаемая {0:F2} мм при шаге {1:F2} мм ", preset.plateMM, spacing * 1000f) + string.Format("запекается в {0:F2} мм ⇒ импульс смещён на ", c.bakedPlateM * 1000f) + string.Format("{0:P0} при допуске {1:P0}.", c.thicknessErrorFraction, 0.1f);
				return c;
			}
		}
		float cosA = UnityEngine.Mathf.Cos(angleDeg * 0.0174532924f);
		if (cosA < 0.05f)
		{
			c.refuseKey = "super.era.refuse.tilt";
			c.refuseArgs = new object[1] { angleDeg.ToString("F0") };
			c.refuseDetail = string.Format("наклон {0:F0}° — плиты почти параллельны выстрелу.", angleDeg);
			return c;
		}
		c.front = new SuperPlateSpec
		{
			thicknessM = c.bakedPlateM,
			angleXDeg = angleDeg,
			sideM = sideM,
			material = "STEEL4340",
			clampedEdges = false,
			noFragmentation = CoherentFlyerPlates,
			alongAxisM = SuperBaker.AlongAxisForFaceOnAxis(c.bakedPlateM, frontFaceZ, angleDeg)
		};
		c.rear = new SuperPlateSpec
		{
			thicknessM = c.bakedPlateM,
			angleXDeg = angleDeg,
			sideM = sideM,
			material = "STEEL4340",
			clampedEdges = false,
			noFragmentation = CoherentFlyerPlates,
			alongAxisM = SuperBaker.AlongAxisForFaceOnAxis(c.bakedPlateM, frontFaceZ - (c.gapM + c.bakedPlateM) / cosA, angleDeg)
		};
		c.gapFlyersM = Clear(c.front, c.rear, cosA);
		c.initiationDelayS = c.bakedPlateM / cosA / Math.Max(1f, rodSpeedMps) + c.gapM / 7000f;
		return c;
	}
}
