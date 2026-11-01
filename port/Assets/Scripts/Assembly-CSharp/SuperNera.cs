using System;
using System.Collections.Generic;

public static class SuperNera
{
	public sealed class Pack
	{
		public List<SuperPlateSpec> plates;

		public float frontBakedM;

		public float interBakedM;

		public float rearBakedM;

		public float frontErr;

		public float interErr;

		public float rearErr;

		public int frontLayers;

		public int interLayers;

		public int rearLayers;

		public string frontMaterial;

		public string rearMaterial;

		public string interlayerMaterial;

		public bool bondingUnknown;

		public string passport;

		public string passportShown;

		public string refuseKey;

		public string refuseDetail;

		public object[] refuseArgs;

		public Pack()
		{
			plates = new List<SuperPlateSpec>();
			passport = "";
			passportShown = "";
			refuseArgs = new object[0];
		}
	}

	public const string KeyUnbonded = "super.nera.refuse.unbonded";

	public const string KeyQuantised = "super.nera.refuse.quantised";

	public const string KeyThin = "super.nera.refuse.thin";

	public const string InterlayerRegistryName = "POLY_INTERLAYER";

	public const string NeopreneRegistryName = "NEOPRENE_INTERLAYER";

	public static Func<InterlayerMaterial, string> NightRowOverride;

	public const float ThicknessTolerance = 0.1f;

	public const int MinPlateLayers = 4;

	public static string RegistryNameFor(InterlayerMaterial m)
	{
		if (NightRowOverride != null)
		{
			string overridden = NightRowOverride(m);
			if (overridden != null)
			{
				return overridden;
			}
		}
		return (m == InterlayerMaterial.Neoprene) ? "NEOPRENE_INTERLAYER" : "POLY_INTERLAYER";
	}

	public static bool Quantise(float tM, float spacing, out int layers, out float bakedM, out float err)
	{
		layers = Math.Max(1, UnityEngine.Mathf.CeilToInt(tM / spacing));
		bakedM = layers * spacing;
		err = (bakedM - tM) / Math.Max(tM, 1E-09f);
		return Math.Abs(err) <= 0.1f;
	}

	public static float SpacingCeilingFor(float frontM, float interM, float rearM, float fromM)
	{
		float spacing = UnityEngine.Mathf.Floor(Math.Max(fromM, 0.008f) * 100000f) / 100000f;
		const float minSpacing = 0.00019999899f;
		while (spacing >= minSpacing)
		{
			bool frontOk = Quantise(frontM, spacing, out int frontLayers, out _, out _);
			bool interOk = Quantise(interM, spacing, out _, out _, out _);
			bool rearOk = Quantise(rearM, spacing, out int rearLayers, out _, out _);
			if (frontOk && interOk && rearOk && frontLayers >= 4 && rearLayers >= 4)
			{
				return spacing;
			}
			spacing += -1E-05f;
		}
		return -1f;
	}

	public static string InterlayerNote(InterlayerMaterial m)
	{
		if (m == InterlayerMaterial.Polyurethane)
		{
			return "прослойка: полиуретан — своя строка реестра (Carter & Marsh, MEASURED; Γ₀/T_melt/C_p ASSUMED)";
		}
		if (m == InterlayerMaterial.Neoprene)
		{
			return "прослойка: НЕОПРЕН — своя строка реестра (Millett et al. 2004; Γ₀ и C_p подставлены от полиуретана — ASSUMED_BY_SUBSTITUTION)";
		}
		return "прослойка: НАТУРАЛЬНАЯ РЕЗИНА считается строкой полиуретана — ASSUMED_BY_SUBSTITUTION, подстановка объявлена реестром";
	}

	public static string InterlayerNoteKey(InterlayerMaterial m)
	{
		if (m == InterlayerMaterial.Polyurethane)
		{
			return "super.passport.interlayer_pu";
		}
		if (m == InterlayerMaterial.Neoprene)
		{
			return "super.passport.interlayer_neoprene";
		}
		return "super.passport.interlayer_rubber";
	}

	public static Pack Build(NeraPreset preset, float sideM, float angleDeg, float frontFaceZ, float spacing, ArmorPlate.ArmorType frontMat, ArmorPlate.ArmorType rearMat, NeraBonding bonding, InterlayerMaterial interlayer)
	{
		Pack p = new Pack();
		string presetName = SuperShotRunner.PresetName(preset.id, preset.displayName);
		float frontM = preset.frontMM * 0.001f;
		float interM = preset.interlayerMM * 0.001f;
		float rearM = preset.rearMM * 0.001f;
		p.frontMaterial = SuperShotRunner.MapMaterial(frontMat);
		p.rearMaterial = SuperShotRunner.MapMaterial(rearMat);
		p.interlayerMaterial = RegistryNameFor(interlayer);
		if (p.frontMaterial == null || p.rearMaterial == null)
		{
			ArmorPlate.ArmorType bad = ((p.frontMaterial == null) ? frontMat : rearMat);
			p.refuseKey = SuperShotRunner.MaterialRefusalKey(bad);
			p.refuseArgs = new object[1] { Catalog.MaterialName(bad) };
			p.refuseDetail = "пластина из " + ArmorPlate.DisplayName(bad) + ": " + p.refuseKey + ".";
			return p;
		}
		if (bonding == NeraBonding.Unbonded)
		{
			p.refuseKey = "super.nera.refuse.unbonded";
			p.refuseArgs = new object[1] { presetName };
			p.refuseDetail = "пластины не связаны с прослойкой — у сеточного контакта нет скольжения.";
			return p;
		}
		p.bondingUnknown = bonding == NeraBonding.Unknown;
		bool frontOk = Quantise(frontM, spacing, out p.frontLayers, out p.frontBakedM, out p.frontErr);
		bool interOk = Quantise(interM, spacing, out p.interLayers, out p.interBakedM, out p.interErr);
		bool rearOk = Quantise(rearM, spacing, out p.rearLayers, out p.rearBakedM, out p.rearErr);
		float ceiling = SpacingCeilingFor(frontM, interM, rearM, spacing);
		string ceilingDetail = ((ceiling > 0f) ? (ceiling * 1000f).ToString("F2") : "—");
		string ceilingShown = ((ceiling > 0f) ? (ceiling * 1000f).ToString("F2", Localization.Culture) : "—");
		int minLayers = Math.Min(p.frontLayers, p.rearLayers);
		if (minLayers < 4)
		{
			float thinnest = Math.Min(frontM, rearM);
			p.refuseKey = "super.nera.refuse.thin";
			p.refuseArgs = new object[4]
			{
				(thinnest * 1000f).ToString("F1", Localization.Culture),
				(spacing * 1000f).ToString("F2", Localization.Culture),
				minLayers,
				ceilingShown
			};
			p.refuseDetail = string.Format("пластина {0:F1} мм при шаге {1:F2} мм — ", thinnest * 1000f, spacing * 1000f) + string.Format("{0} сл. частиц при нужных {1}; нужен шаг ⩽ {2} мм.", minLayers, 4, ceilingShown);
			return p;
		}
		if (!frontOk || !interOk || !rearOk)
		{
			p.refuseKey = "super.nera.refuse.quantised";
			p.refuseArgs = new object[4]
			{
				presetName,
				string.Format("{0:F1}/{1:F1}/{2:F1}", preset.frontMM, preset.interlayerMM, preset.rearMM),
				(spacing * 1000f).ToString("F2", Localization.Culture),
				ceilingShown
			};
			p.refuseDetail = string.Format("слои {0:F1}/{1:F1}/{2:F1} мм при шаге ", preset.frontMM, preset.interlayerMM, preset.rearMM) + string.Format("{0:F2} мм: ошибки {1:P0}/{2:P0}/{3:P0} ", spacing * 1000f, p.frontErr, p.interErr, p.rearErr) + string.Format("при допуске {0:P0}; нужен шаг ⩽ {1} мм.", 0.1f, ceilingDetail);
			return p;
		}
		float cosA = UnityEngine.Mathf.Cos(angleDeg * 0.0174532924f);
		if (cosA < 0.05f)
		{
			p.refuseKey = "super.nera.refuse.tilt";
			p.refuseArgs = new object[1] { angleDeg.ToString("F0") };
			p.refuseDetail = string.Format("наклон {0:F0}° — пакет почти вдоль выстрела.", angleDeg);
			return p;
		}
		float interFaceZ = frontFaceZ - p.frontBakedM / cosA;
		float rearFaceZ = interFaceZ - p.interBakedM / cosA;
		p.plates.Add(Layer(p.frontBakedM, angleDeg, sideM, p.frontMaterial, frontFaceZ, SuperEra.CoherentFlyerPlates));
		p.plates.Add(Layer(p.interBakedM, angleDeg, sideM, p.interlayerMaterial, interFaceZ, false));
		p.plates.Add(Layer(p.rearBakedM, angleDeg, sideM, p.rearMaterial, rearFaceZ, SuperEra.CoherentFlyerPlates));
		p.passport = string.Format("пакет НДЗ «{0}»: {1}/{2}/{3} сл. ", presetName, p.frontLayers, p.interLayers, p.rearLayers) + string.Format("({0:F2}/{1:F2}/{2:F2} мм, ", p.frontBakedM * 1000f, p.interBakedM * 1000f, p.rearBakedM * 1000f) + string.Format("ошибки {0:P1}/{1:P1}/{2:P1}); ", p.frontErr, p.interErr, p.rearErr) + (p.bondingUnknown ? "связанность в источнике НЕ НАЗВАНА — считается связанным (единственный контакт сетки); " : "связанный; ") + InterlayerNote(interlayer) + "; кромки свободны; пластины цельные (У14).";
		p.passportShown = Localization.T("super.passport.nera_pack", presetName, p.frontLayers, p.interLayers, p.rearLayers, p.frontBakedM * 1000f, p.interBakedM * 1000f, p.rearBakedM * 1000f, p.frontErr, p.interErr, p.rearErr) + "; " + Localization.T(p.bondingUnknown ? "super.passport.nera_bonding_unknown" : "super.passport.nera_bonded") + "; " + Localization.T(InterlayerNoteKey(interlayer)) + "; " + Localization.T("super.passport.nera_tail") + ".";
		return p;
	}

	private static SuperPlateSpec Layer(float thicknessM, float angleDeg, float sideM, string material, float faceZ, bool coherent)
	{
		return new SuperPlateSpec
		{
			thicknessM = thicknessM,
			angleXDeg = angleDeg,
			sideM = sideM,
			material = material,
			clampedEdges = false,
			noFragmentation = coherent,
			alongAxisM = SuperBaker.AlongAxisForFaceOnAxis(thicknessM, faceZ, angleDeg)
		};
	}
}
