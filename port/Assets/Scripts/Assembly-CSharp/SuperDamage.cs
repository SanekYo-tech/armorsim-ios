using System;
using System.Globalization;
using UnityEngine;

public static class SuperDamage
{
	public static bool Enabled;

	public const float Cap = 10f;

	public static float SofteningCoefficient;

	public const float SofteningFloor = 0.02f;

	public static bool AsbErosionOn;

	public const int CauseAsbErosion = 5;

	public const float DebrisJMin = 0.05f;

	public const float DebrisJMax = 20f;

	public static float CoherentTensionFloorPa;

	public const string CoherentTensionFloorTag = "ASSUMED_BY_SUBSTITUTION";

	public const string CoherentTensionFloorSource = "армко-железо, откол 1.9 ГПа при HEL 0.9 ГПа (бриф 30, симметричный удар 0.2–0.4 км/с, ниже перехода α→ε); подстановка за мягкую сталь 4С20 метаемых пластин ДЗ; полоса измеренного семейства сталей ниже перехода 1.9…6.8 ГПа";

	public static float AlphaEpsilonTransitionPa;

	public static readonly float[] CoherentTensionBandPa;

	[Obsolete("04.09: потолок выводится из вещества — PressureCapFor(K). Число 50 ГПа стояло внутри рабочего диапазона стержневого удара.")]
	public const float DebrisPressureCapPa = 5E+10f;

	public static float PressureCapFactorN;

	public const double DegeneracyCeiling = 0.01;

	public static bool SabotageNoFailure;

	public static bool SabotageDebrisTension;

	public static bool SabotageNoSpall;

	public static bool SabotageNoShear;

	public static int YieldIterations;

	public static float RhoFloorKgM3;

	public static float SigEqReturnFloorPa;

	public static bool SabotageEfCollapse;

	public static bool SabotagePressureBlowup;

	public static bool CavitationOn;

	public static float LiquidCflFactor;

	public static bool InterlayerSpallOn;

	public static bool SabotageNoCavitation;

	public static bool DebrisWorkApplied;

	public static bool DebrisIsotropicF;

	public static bool WholeIsotropicF;

	public const float WholeShapeResetRatio = 4f;

	public static bool CohesiveDegenerateExit;

	public static bool FailureStrainScatterOn;

	public static float FailureStrainScatterModulus;

	public static uint FailureStrainScatterSalt;

	public static bool SabotageXiUnity;

	public static bool SabotageXiShuffle;

	public static double S43Alpha;

	public static double S43Beta;

	public static double S43VRefMm3;

	public static double S43HRefMm;

	public static float CavitationOverridePa;

	public static readonly float[] CavitationBandPa;

	public const string CavitationTag = "ASSUMED";

	public const string CavitationSource = "аналог *MAT_NULL «PC» LS-DYNA / гидравлического предела растяжения Autodyn для жидкости с ударным УС (прослойки НДЗ, бриф 25); −50 МПа внутри полосы −10…−200 МПа кавитационной прочности эластомеров — ASSUMED, бриф 37 запрошен S37 23.09, значение не подбиралось под ворота";

	public static float CavitationMaxFractionOfK;

	public const string CavitationCeilingTag = "MEASURED (S37 BAND/S31NERA 24.09)";

	static SuperDamage()
	{
		Enabled = true;
		SofteningCoefficient = 1f;
		AsbErosionOn = true;
		CoherentTensionFloorPa = -1900000000f;
		AlphaEpsilonTransitionPa = 13000000000f;
		CoherentTensionBandPa = new float[4] { -1500000000f, -2500000000f, -3500000000f, -5000000000f };
		PressureCapFactorN = 32f;
		SabotageNoFailure = false;
		SabotageDebrisTension = false;
		SabotageNoSpall = false;
		SabotageNoShear = false;
		YieldIterations = 3;
		RhoFloorKgM3 = 1f;
		SigEqReturnFloorPa = 1E-06f;
		SabotageEfCollapse = false;
		SabotagePressureBlowup = false;
		CavitationOn = ArmFromCommandLine("-s37cav", "КАВИТАЦИОННЫЙ ПРЕДЕЛ ЖИДКОСТИ", true);
		LiquidCflFactor = FactorFromCommandLine("-s37liqcfl", "МНОЖИТЕЛЬ КУРАНТА ЖИДКОСТИ (F-d)", 1f);
		InterlayerSpallOn = ArmFromCommandLine("-s37spall", "ОТКОЛ ЖИДКОЙ ПРОСЛОЙКИ (F-a)", false);
		SabotageNoCavitation = false;
		DebrisWorkApplied = ArmFromCommandLine("-s38debriswork", "РАБОТА ОБЛОМКА ПО ДЕЙСТВУЮЩЕМУ ДАВЛЕНИЮ", true, "S38", "слово владельца 25.09.2026");
		DebrisIsotropicF = ArmFromCommandLine("-s38debrisf", "ИЗОТРОПНЫЙ F ОБЛОМКА", true, "S38", "слово владельца 25.09.2026");
		WholeIsotropicF = ArmFromCommandLine("-s38fwhole", "ИЗОТРОПНЫЙ F ИСКАЖЁННОГО ЦЕЛОГО ВЕЩЕСТВА", true, "S38", "слово владельца 26.09.2026");
		CohesiveDegenerateExit = ArmFromCommandLine("-s38coh4", "ВЫРОЖДЕНИЕ ЦЕЛЬНОГО ТЕЛА — ОБЛОМОК НАСОВСЕМ", true, "S38", "слово владельца 26.09.2026");
		FailureStrainScatterOn = ArmFromCommandLine("-s32xi", "РАЗБРОС ДЕФОРМАЦИИ РАЗРУШЕНИЯ ξ (S32)", false, "S32", "фаза S32: включение по умолчанию — решение владельца ПОСЛЕ фазы");
		FailureStrainScatterModulus = PositiveFromCommandLine("-s32m", "МОДУЛЬ ВЕЙБУЛЛА ξ (S32)", 0f);
		FailureStrainScatterSalt = 1395785729u;
		SabotageXiUnity = false;
		SabotageXiShuffle = false;
		S43Alpha = NonNegativeDoubleFromCommandLine("-s43a", "ПОКАЗАТЕЛЬ ОБЪЁМА α (S43)", 0.0);
		S43Beta = NonNegativeDoubleFromCommandLine("-s43b", "ПОКАЗАТЕЛЬ ШАГА β (S43)", 0.0);
		S43VRefMm3 = NonNegativeDoubleFromCommandLine("-s43vref", "ЯКОРЬ ОБЪЁМА V_ref, мм³ (S43)", 0.0);
		S43HRefMm = NonNegativeDoubleFromCommandLine("-s43href", "ЯКОРЬ ШАГА h_ref, мм (S43)", 0.0);
		CavitationOverridePa = 0f;
		CavitationBandPa = new float[3] { -10000000f, -50000000f, -200000000f };
		CavitationMaxFractionOfK = 0.006f;
	}

	public static bool S43ScalingOn => S43Alpha != 0.0 || S43Beta != 0.0;

	public static bool XiFieldOn => FailureStrainScatterOn || S43ScalingOn;

	public static float StrengthFactor(float damage)
	{
		float d = 1f;
		if (1f > damage)
		{
			d = damage;
		}
		float result = 1f - d * SofteningCoefficient;
		if (!(result > 0.02f))
		{
			result = 0.02f;
		}
		return result;
	}

	public static float PressureCapFor(float bulkModulusK)
	{
		return bulkModulusK * PressureCapFactorN;
	}

	public static bool SceneHasLiquid(SuperBaker.Baked baked)
	{
		foreach (SuperBaker.Body body in baked.bodies)
		{
			if (ContinuumMaterials.Get(body.material).G == 0f)
			{
				return true;
			}
		}
		return false;
	}

	public static float CflFor(SuperBaker.Baked baked, float baseCfl)
	{
		if (LiquidCflFactor < 1f && SceneHasLiquid(baked))
		{
			baseCfl *= LiquidCflFactor;
		}
		return baseCfl;
	}

	private static float FactorFromCommandLine(string flag, string what, float def)
	{
		string[] args = Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] != flag)
			{
				continue;
			}
			if (float.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && value > 0f && value <= 1f)
			{
				Debug.Log(string.Format("[S37] {0} = {1:G9} доводом командной строки {2}", what, value, flag));
				return value;
			}
			Debug.LogWarning(string.Format("[S37] довод {0} «{1}» не число в (0; 1] — умолчание {2:G9}", flag, args[i + 1], def));
			break;
		}
		Debug.Log(string.Format("[S37] {0}: {1:G9} по умолчанию{2}", what, def, (def >= 1f) ? " (выключен)" : ""));
		return def;
	}

	public static void S43Anchors(out double vRefM3, out double hRefM)
	{
		if (S43Alpha != 0.0 && S43VRefMm3 <= 0.0)
		{
			throw new InvalidOperationException(Localization.T("super.s43.refuse.anchor", "α", "-s43vref"));
		}
		if (S43Beta != 0.0 && S43HRefMm <= 0.0)
		{
			throw new InvalidOperationException(Localization.T("super.s43.refuse.anchor", "β", "-s43href"));
		}
		vRefM3 = S43VRefMm3 * 1E-09;
		hRefM = S43HRefMm * 0.001;
	}

	public static string S43StateText()
	{
		if (!S43ScalingOn)
		{
			return "ВЫКЛ";
		}
		return string.Format(CultureInfo.InvariantCulture, "α = {0:G6}, β = {1:G6}, V_ref = {2:G6} мм³, h_ref = {3:G6} мм", S43Alpha, S43Beta, S43VRefMm3, S43HRefMm);
	}

	private static double NonNegativeDoubleFromCommandLine(string flag, string what, double def, string tag = "S43")
	{
		CultureInfo culture = CultureInfo.InvariantCulture;
		string[] args = Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] != flag)
			{
				continue;
			}
			if (double.TryParse(args[i + 1], NumberStyles.Float, culture, out double value) && value >= 0.0 && !double.IsNaN(value) && !double.IsInfinity(value))
			{
				Debug.Log("[" + tag + "] " + what + " = " + value.ToString("R", culture) + " доводом командной строки " + flag);
				return value;
			}
			Debug.LogWarning("[" + tag + "] довод " + flag + " «" + args[i + 1] + "» не конечное число ⩾ 0 — умолчание " + def.ToString("R", culture));
			return def;
		}
		Debug.Log("[" + tag + "] " + what + ": " + def.ToString("R", culture) + " по умолчанию");
		return def;
	}

	private static float PositiveFromCommandLine(string flag, string what, float def, string tag = "S32")
	{
		string[] args = Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] != flag)
			{
				continue;
			}
			if (float.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && value > 0f && !float.IsInfinity(value))
			{
				Debug.Log("[" + tag + "] " + what + " = " + value.ToString("G9", CultureInfo.InvariantCulture) + " доводом командной строки " + flag);
				return value;
			}
			Debug.LogWarning("[" + tag + "] довод " + flag + " «" + args[i + 1] + "» не конечное число > 0 — умолчание " + def.ToString("G9", CultureInfo.InvariantCulture));
			return def;
		}
		Debug.Log("[" + tag + "] " + what + ": " + def.ToString("G9", CultureInfo.InvariantCulture) + " по умолчанию" + ((def <= 0f) ? " (не выбран: при ВКЛ флаге запекание откажет)" : ""));
		return def;
	}

	public static float CavitationCeilingPa(in ContinuumProperties m)
	{
		return -CavitationMaxFractionOfK * m.K;
	}

	public static bool CavitationCeilingBinds(in ContinuumProperties m)
	{
		if (!CavitationOn || m.G != 0f || m.cavitationPa >= 0f || CavitationOverridePa < 0f)
		{
			return false;
		}
		return CavitationCeilingPa(in m) > m.cavitationPa;
	}

	public static float EffectiveCavitationPa(in ContinuumProperties m)
	{
		if (!CavitationOn || m.G != 0f || m.cavitationPa >= 0f)
		{
			return 0f;
		}
		if (CavitationOverridePa < 0f)
		{
			return CavitationOverridePa;
		}
		return Math.Max(CavitationCeilingPa(in m), m.cavitationPa);
	}

	public static float EffectiveSpallPa(in ContinuumProperties m)
	{
		if (!InterlayerSpallOn || m.G != 0f || m.spallPa != 0f || m.cavitationPa >= 0f)
		{
			return m.spallPa;
		}
		if (CavitationOverridePa < 0f)
		{
			return CavitationOverridePa;
		}
		return m.cavitationPa;
	}

	private static bool ArmFromCommandLine(string flag, string what, bool defaultOn)
	{
		return ArmFromCommandLine(flag, what, defaultOn, "S37", "слово владельца 24.09.2026");
	}

	internal static bool ArmFromCommandLine(string flag, string what, bool defaultOn, string tag, string defaultWhy)
	{
		string[] args = Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] != flag)
			{
				continue;
			}
			if (args[i + 1] == "1")
			{
				Debug.Log("[" + tag + "] " + what + " ВООРУЖЁН доводом командной строки " + flag + " 1");
				return true;
			}
			if (args[i + 1] == "0")
			{
				Debug.Log("[" + tag + "] " + what + " ВЫКЛЮЧЕН доводом командной строки " + flag + " 0");
				return false;
			}
		}
		Debug.Log("[" + tag + "] " + what + ": " + (defaultOn ? "ВКЛ" : "ВЫКЛ") + " по умолчанию" + (defaultOn ? (" (" + defaultWhy + ")") : ""));
		return defaultOn;
	}
}
