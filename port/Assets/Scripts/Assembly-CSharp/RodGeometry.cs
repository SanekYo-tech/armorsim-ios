using UnityEngine;

public static class RodGeometry
{
	public const float NoseLengthCalibers = 1.5f;

	public const float NoseTipRadiusFactor = 0.08f;

	public const float BluntTipRadiusFactor = 0.55f;

	public const float BluntNoseLengthScale = 0.6f;

	public const float FinSpanBoreFraction = 0.5f;

	public const float FinSpanCalibers = 2.5f;

	public const float MinSpanCalibers = 1.3f;

	public const float FinChordCalibers = 2f;

	public const float FinThicknessCalibers = 0.06f;

	public const float BladeSweep = 0.55f;

	public const float BladeTipChordFrac = 0.18f;

	public const float BladeTipThickFrac = 0.4f;

	public const float BladeTearJitter = 0.06f;

	public const int DefaultBladeCount = 6;

	public const int BladePieces = 3;

	public static float NoseLengthM(float diameterM, float rodLengthM)
	{
		return Mathf.Min(Mathf.Max(diameterM, 0f) * NoseLengthCalibers, Mathf.Max(rodLengthM, 1E-06f) * 0.25f);
	}

	public static float NoseRadiusFactor(NoseShape shape, float aOverR, float noseLenOverR)
	{
		if (shape == NoseShape.Flat || noseLenOverR <= 1E-09f)
		{
			return 1f;
		}
		if (shape == NoseShape.BluntOgive)
		{
			noseLenOverR *= BluntNoseLengthScale;
		}
		if (noseLenOverR <= 1E-09f)
		{
			return 1f;
		}
		float a = Mathf.Clamp(aOverR, 0f, noseLenOverR);
		if (a >= noseLenOverR)
		{
			return 1f;
		}
		float factor;
		if (shape == NoseShape.Conical)
		{
			factor = a / noseLenOverR;
		}
		else
		{
			float c = (noseLenOverR * noseLenOverR + 1f) * 0.5f;
			float d = noseLenOverR - a;
			float discriminant = c * c - d * d;
			factor = 1f + (discriminant > 0f ? Mathf.Sqrt(discriminant) : 0f) - c;
			if (shape == NoseShape.BluntOgive)
			{
				factor = BluntTipRadiusFactor + Mathf.Clamp(factor, 0f, 1f) * 0.45f;
			}
		}
		return Mathf.Clamp(factor, NoseTipRadiusFactor, 1f);
	}

	public static float NoseRadiusFactorAt(NoseShape shape, float aFromNose, float noseLen, float rodRadius)
	{
		if (rodRadius <= 1E-09f)
		{
			return 1f;
		}
		return NoseRadiusFactor(shape, aFromNose / rodRadius, noseLen / rodRadius);
	}

	public static float FinSpanM(float diameterM)
	{
		return Mathf.Max(diameterM, 0f) * FinSpanCalibers;
	}

	public static float FinSpanM(float diameterM, float gunCalibreM)
	{
		diameterM = Mathf.Max(diameterM, 0f);
		if (gunCalibreM > 0f)
		{
			return Mathf.Max(gunCalibreM * FinSpanBoreFraction, diameterM * MinSpanCalibers);
		}
		return FinSpanM(diameterM);
	}

	public static float FinChordM(float diameterM, float rodLengthM)
	{
		return Mathf.Min(Mathf.Max(diameterM, 0f) * FinChordCalibers, Mathf.Max(rodLengthM, 1E-06f) * 0.4f);
	}

	public static float FinThicknessM(float diameterM)
	{
		return Mathf.Max(diameterM, 0f) * FinThicknessCalibers;
	}

	public static float BladeHeightM(float diameterM)
	{
		return Mathf.Max((FinSpanM(diameterM) - Mathf.Max(diameterM, 0f)) * 0.5f, 0f);
	}

	public static float BladeHeightM(float diameterM, float gunCalibreM)
	{
		return Mathf.Max((FinSpanM(diameterM, gunCalibreM) - Mathf.Max(diameterM, 0f)) * 0.5f, 0f);
	}

	public static float PieceShearStationM(int blade, int bladeCount, int piece, int pieceCount, float diameterM, float rodLengthM)
	{
		bladeCount = Mathf.Max(bladeCount, 1);
		pieceCount = Mathf.Max(pieceCount, 1);
		blade = Mathf.Clamp(blade, 0, bladeCount - 1);
		piece = Mathf.Clamp(piece, 0, pieceCount - 1);
		int total = bladeCount * pieceCount;
		return FinChordM(diameterM, rodLengthM) * (total - bladeCount * piece - blade) / total;
	}

	public static float BladeShearStationM(int blade, int bladeCount, float diameterM, float rodLengthM)
	{
		bladeCount = Mathf.Max(bladeCount, 1);
		blade = Mathf.Clamp(blade, 0, bladeCount - 1);
		return FinChordM(diameterM, rodLengthM) * (bladeCount - blade) / bladeCount;
	}

	public static float PieceShearDepthMM(int blade, int bladeCount, int piece, int pieceCount, float diameterM, float rodLengthM, float currentLengthM)
	{
		return (currentLengthM - PieceShearStationM(blade, bladeCount, piece, pieceCount, diameterM, rodLengthM)) * 1000f;
	}

	public static bool PieceSheared(int blade, int bladeCount, int piece, int pieceCount, float diameterM, float rodLengthM, float depthMM, float currentLengthM)
	{
		return depthMM >= (currentLengthM - PieceShearStationM(blade, bladeCount, piece, pieceCount, diameterM, rodLengthM)) * 1000f;
	}

	public static float BladeShearDepthMM(int blade, int bladeCount, float diameterM, float rodLengthM, float currentLengthM)
	{
		return (currentLengthM - BladeShearStationM(blade, bladeCount, diameterM, rodLengthM)) * 1000f;
	}

	public static bool BladeSheared(int blade, int bladeCount, float diameterM, float rodLengthM, float depthMM, float currentLengthM)
	{
		return depthMM >= (currentLengthM - BladeShearStationM(blade, bladeCount, diameterM, rodLengthM)) * 1000f;
	}

	public static bool FinsPassThroughCrater(float diameterM, float craterDiameterM)
	{
		return craterDiameterM >= FinSpanM(diameterM);
	}

	public static bool FinsPassThroughCrater(float diameterM, float gunCalibreM, float craterDiameterM)
	{
		return craterDiameterM >= FinSpanM(diameterM, gunCalibreM);
	}
}
