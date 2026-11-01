using System;

public static class StandoffDefences
{
	public enum SlatOutcome
	{
		PassedBetween = 0,
		Dud = 1,
		DetonatedAtSlat = 2
	}

	public enum BlockMaterial
	{
		AluminiumAlloy = 0,
		Steel = 1
	}

	public const float ScreenThicknessMM = 8f;

	public const float ScreenDensity = 1200f;

	public const float ScreenRtPa = 50000000f;

	public const float ScreenAnchoredMinCD = 4f;

	public const float ScreenAnchoredMaxCD = 10f;

	private static float cachedBreakEvenCD;

	private static float cachedForConeMM;

	private static float cachedForFuzeMM;

	public const float SlatDudFractionDefault = 0.5f;

	public const float VoidFractionDefault = 0.55f;

	public const float BlockWidthM = 1f;

	public const float BlockHeightM = 0.8f;

	public const float BlockThicknessMM = 700f;

	public static TateModel.Target RubberScreen()
	{
		return new TateModel.Target
		{
			name = "Резинотканевый экран",
			density = ScreenDensity,
			Rt = ScreenRtPa,
			RtIsFitted = true
		};
	}

	public static float WorkingStandoffM(float screenToArmourM)
	{
		return Math.Max(0f, screenToArmourM);
	}

	public static float StandoffInCalibers(float standoffM, float coneDiameterM)
	{
		return (coneDiameterM > 1E-09f) ? (standoffM / coneDiameterM) : 0f;
	}

	public static bool ScreenStandoffIsAnchored(float standoffM, float coneDiameterM)
	{
		if (coneDiameterM <= 1E-09f)
		{
			return false;
		}
		float num = standoffM / coneDiameterM;
		return num >= ScreenAnchoredMinCD && num <= ScreenAnchoredMaxCD;
	}

	public static float ScreenBreakEvenCD(float coneDiameterMM, float fuzeStandoffMM, Func<float, float> penetrationAtStandoffCD)
	{
		if (Math.Abs(coneDiameterMM - cachedForConeMM) < 0.001f && Math.Abs(fuzeStandoffMM - cachedForFuzeMM) < 0.001f)
		{
			return cachedBreakEvenCD;
		}
		float num = ((coneDiameterMM > 1E-06f) ? (fuzeStandoffMM / coneDiameterMM) : 0f);
		float num2 = penetrationAtStandoffCD(num);
		float num3 = -1f;
		for (float num4 = 6f; num4 <= 40f; num4 += 0.25f)
		{
			if (num2 < penetrationAtStandoffCD(num4))
			{
				num3 = num4;
				break;
			}
		}
		cachedForConeMM = coneDiameterMM;
		cachedForFuzeMM = fuzeStandoffMM;
		cachedBreakEvenCD = num3;
		return num3;
	}

	public static float SlatHitProbability(float barWidthM, float warheadDiameterM, float pitchM)
	{
		if (pitchM <= 1E-09f)
		{
			return 1f;
		}
		return Math.Min(1f, Math.Max(0f, (Math.Max(0f, barWidthM) + Math.Max(0f, warheadDiameterM)) / pitchM));
	}

	public static SlatOutcome ResolveSlat(float pHit, float dudFraction, float roll1, float roll2)
	{
		pHit = Math.Min(1f, Math.Max(0f, pHit));
		if (roll1 >= pHit)
		{
			return SlatOutcome.PassedBetween;
		}
		dudFraction = Math.Min(1f, Math.Max(0f, dudFraction));
		return (roll2 < dudFraction) ? SlatOutcome.Dud : SlatOutcome.DetonatedAtSlat;
	}

	public static SlatOutcome ResolveSlatTandem(float pHit, float roll1)
	{
		pHit = Math.Min(1f, Math.Max(0f, pHit));
		return (roll1 < pHit) ? SlatOutcome.DetonatedAtSlat : SlatOutcome.PassedBetween;
	}

	public static float EffectiveThicknessMM(float thicknessMM, float voidFraction)
	{
		voidFraction = Math.Min(1f, Math.Max(0f, voidFraction));
		return Math.Max(0f, thicknessMM) * Math.Max(0f, 1f - voidFraction);
	}

	public static TateModel.Target BlockMaterialTarget(BlockMaterial m)
	{
		return (m == BlockMaterial.Steel) ? TateModel.RHA() : TateModel.Aluminum();
	}

	public static string BlockMaterialName(BlockMaterial m)
	{
		return (m == BlockMaterial.Steel) ? "Сталь" : "Алюминиевый сплав";
	}

	public static TateModel.Target EngineBlockTarget(BlockMaterial m)
	{
		TateModel.Target result = BlockMaterialTarget(m);
		result.name = "Моторный отсек (" + BlockMaterialName(m) + ")";
		return result;
	}

	static StandoffDefences()
	{
		cachedBreakEvenCD = -1f;
		cachedForConeMM = -1f;
		cachedForFuzeMM = -1f;
	}
}
