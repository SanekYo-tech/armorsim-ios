using UnityEngine;

public static class CraterModel
{
	public enum RimClass
	{
		DuctileMetal = 0,
		HighHardnessSteel = 1,
		Ceramic = 2,
		Composite = 3,
		Other = 4
	}

	public const float BjerkeVMinKmS = 1.2f;

	public const float BjerkeVMaxKmS = 4.7f;

	public const float ErosionBlendSpan = 0.35f;

	public const float PetalingMaxTOverD = 1f;

	public const float PetalingMaxErosion = 0.05f;

	public const float PreBreakoutLigamentCalibers = 1f;

	public const float RaggedRimMaxTOverD = 1f;

	public const float CompositeDelamHalfAngleDeg = 1.089f;

	public const float CompositeDelamRampExponent = 2f;

	public const float CompositeDownstreamFlare = 1.175f;

	public const float CompositeDownstreamFlareMax = 2f;

	public const float PlugDiameterFactor = 1.2f;

	public const float PlugThicknessFactor = 0.85f;

	public const float PlugVelocityFraction = 0.8f;

	public const float JetCleanExitMargin = 2f;

	public static float BjerkeCraterRatio(float velocityKmS)
	{
		velocityKmS = Mathf.Clamp(velocityKmS, BjerkeVMinKmS, BjerkeVMaxKmS);
		return 0.1286f * velocityKmS * velocityKmS + (0.3388f * velocityKmS + 1.1524f);
	}

	public static float WalkerCraterRatio(float velocityKmS)
	{
		return 1f + velocityKmS * 0.7f;
	}

	public static float ErosionBoundaryMS(TateModel.Penetrator p, TateModel.Target t)
	{
		if (t.Rt >= p.Yp || t.density <= 0f)
		{
			return 0f;
		}
		return Mathf.Sqrt(2f * (p.Yp - t.Rt) / t.density);
	}

	public static float ErosionIndicator(TateModel.Penetrator p, TateModel.Target t, float velocityMS)
	{
		float erosion = RigidPenetration.ErosionOnset(p, t, velocityMS);
		if (erosion <= 0f)
		{
			return 0f;
		}
		if (erosion >= 1f)
		{
			return 1f;
		}
		return erosion * erosion * (3f - 2f * erosion);
	}

	public static float CraterDiameterRatio(TateModel.Penetrator p, TateModel.Target t, float velocityMS)
	{
		return 1f + (BjerkeCraterRatio(velocityMS / 1000f) - 1f) * ErosionIndicator(p, t, velocityMS);
	}

	public static float CraterClearanceM(TateModel.Penetrator p, TateModel.Target t, float rodDiameterM, float velocityMS)
	{
		return Mathf.Max((CraterDiameterRatio(p, t, velocityMS) - 1f) * (rodDiameterM * 0.5f), 0f);
	}

	public static float EffectiveLengthByClearance(float[] arcFromNose, float[] lateral, float clearanceM)
	{
		if (arcFromNose == null || arcFromNose.Length < 2 || lateral == null || lateral.Length != arcFromNose.Length || clearanceM <= 0f)
		{
			return 0f;
		}
		float previousMax = 0f;
		for (int i = 2; i < arcFromNose.Length; i++)
		{
			float span = arcFromNose[i] - arcFromNose[0];
			if (span <= 1E-09f)
			{
				continue;
			}
			float slope = (lateral[i] - lateral[0]) / span;
			float maxDeviation = 0f;
			for (int j = 1; j < i; j++)
			{
				float expected = lateral[0] + (arcFromNose[j] - arcFromNose[0]) * slope;
				maxDeviation = Mathf.Max(maxDeviation, Mathf.Abs(lateral[j] - expected));
			}
			if (maxDeviation > clearanceM)
			{
				float fraction = 0f;
				float delta = maxDeviation - previousMax;
				if (delta > 1E-12f)
				{
					fraction = Mathf.Clamp01((clearanceM - previousMax) / delta);
				}
				return arcFromNose[i - 1] + (arcFromNose[i] - arcFromNose[i - 1]) * fraction - arcFromNose[0];
			}
			previousMax = maxDeviation;
		}
		return arcFromNose[arcFromNose.Length - 1] - arcFromNose[0];
	}

	public static bool PetalingApplies(float plateThicknessMM, float penetratorDiameterMM, float erosionIndicator)
	{
		return penetratorDiameterMM > 0f && plateThicknessMM / penetratorDiameterMM <= PetalingMaxTOverD && erosionIndicator <= PetalingMaxErosion;
	}

	public static float PreBreakoutLigamentMM(float penetratorDiameterMM)
	{
		return Mathf.Max(penetratorDiameterMM, 0f) * PreBreakoutLigamentCalibers;
	}

	public static bool PreBreakoutSpallApplies(float remainingLigamentMM, float penetratorDiameterMM)
	{
		if (penetratorDiameterMM <= 0f)
		{
			return false;
		}
		return remainingLigamentMM <= PreBreakoutLigamentMM(penetratorDiameterMM);
	}

	public static RimClass RimClassOf(ArmorPlate.ArmorType t)
	{
		switch (t)
		{
		case ArmorPlate.ArmorType.HHA:
			return RimClass.HighHardnessSteel;
		case ArmorPlate.ArmorType.Textolite:
			return RimClass.Composite;
		case ArmorPlate.ArmorType.Ceramic:
		case ArmorPlate.ArmorType.BoronCarbide:
		case ArmorPlate.ArmorType.TitaniumDiboride:
		case ArmorPlate.ArmorType.Alumina:
			return RimClass.Ceramic;
		case ArmorPlate.ArmorType.RHA:
		case ArmorPlate.ArmorType.Aluminum:
		case ArmorPlate.ArmorType.Titanium:
		case ArmorPlate.ArmorType.CastSteel:
			return RimClass.DuctileMetal;
		default:
			return RimClass.Other;
		}
	}

	public static bool RaggedRimApplies(ArmorPlate.ArmorType type, float plateThicknessMM, float penetratorDiameterMM)
	{
		if (penetratorDiameterMM <= 0f || plateThicknessMM / penetratorDiameterMM > RaggedRimMaxTOverD)
		{
			return false;
		}
		RimClass c = RimClassOf(type);
		return c == RimClass.DuctileMetal || c == RimClass.HighHardnessSteel;
	}

	public static float RaggedRimAmplitude(RimClass c)
	{
		if (c == RimClass.DuctileMetal)
		{
			return 0.3f;
		}
		if (c == RimClass.HighHardnessSteel)
		{
			return 0.18f;
		}
		return 0f;
	}

	public static float JetChannelFlareHalfAngleDeg(ArmorPlate.ArmorType t)
	{
		return RimClassOf(t) == RimClass.Composite ? CompositeDelamHalfAngleDeg : 0f;
	}

	public static float PlugDiameterMM(float penetratorDiameterMM)
	{
		return Mathf.Max(penetratorDiameterMM, 0f) * PlugDiameterFactor;
	}

	public static float PlugThicknessMM(float penetratorDiameterMM, float plateThicknessMM)
	{
		return Mathf.Min(Mathf.Max(penetratorDiameterMM, 0f) * PlugThicknessFactor, Mathf.Max(plateThicknessMM, 0f));
	}

	public static float JetExitBulge(float depthMargin)
	{
		if (depthMargin >= JetCleanExitMargin)
		{
			return 0f;
		}
		if (depthMargin <= 1f)
		{
			return 1f;
		}
		return JetCleanExitMargin - depthMargin;
	}
}
