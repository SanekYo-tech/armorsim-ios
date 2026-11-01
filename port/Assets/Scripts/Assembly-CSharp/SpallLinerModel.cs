using UnityEngine;

public static class SpallLinerModel
{
	public const float AramidDensity = 1440f;

	public const float UhmwpeDensity = 970f;

	public const float AramidAbsorptionJPerKg = 205000f;

	public const float UhmwpeAbsorptionJPerKg = 307500f;

	public const float ReferenceFragmentMassKg = 0.001f;

	public static float ReferenceFragmentDiameterM => FragmentBallistics.SphereDiameterM(ReferenceFragmentMassKg, 7850f);

	public static float Density(SpallLinerMaterial m)
	{
		if (m == SpallLinerMaterial.UHMWPE)
		{
			return UhmwpeDensity;
		}
		return AramidDensity;
	}

	public static string Name(SpallLinerMaterial m)
	{
		if (m == SpallLinerMaterial.UHMWPE)
		{
			return "СВМПЭ";
		}
		return "Арамид (кевлар)";
	}

	public static float ArealDensityKgM2(SpallLinerMaterial m, float thicknessM)
	{
		if (m == SpallLinerMaterial.UHMWPE)
		{
			return Mathf.Max(thicknessM, 0f) * UhmwpeDensity;
		}
		return Mathf.Max(thicknessM, 0f) * AramidDensity;
	}

	public static float AbsorptionJPerKg(SpallLinerMaterial m)
	{
		if (m == SpallLinerMaterial.UHMWPE)
		{
			return UhmwpeAbsorptionJPerKg;
		}
		return AramidAbsorptionJPerKg;
	}

	public static SpallLinerCatch Resolve(SpallLinerMaterial material, float thicknessM, float massKg, float velocityMS, float fragDiameterM, float obliquityRad = 0f)
	{
		SpallLinerCatch result = default(SpallLinerCatch);
		result.energyJ = 0.5f * Mathf.Max(massKg, 0f) * velocityMS * velocityMS;
		float obliquity = Mathf.Clamp(obliquityRad, 0f, 1.55334306f);
		float cos = Mathf.Max(Mathf.Cos(obliquity), 0.001f);
		result.arealDensityKgM2 = ArealDensityKgM2(material, Mathf.Max(thicknessM, 0f) / cos);
		result.presentedAreaM2 = ((fragDiameterM > 0f) ? (fragDiameterM * Mathf.PI * fragDiameterM * 0.25f) : 0f);
		result.capacityJ = AbsorptionJPerKg(material) * result.arealDensityKgM2 * result.presentedAreaM2;
		if (massKg <= 0f || velocityMS <= 0f || result.presentedAreaM2 <= 0f || result.arealDensityKgM2 <= 0f)
		{
			result.residualVelocityMS = velocityMS;
			return result;
		}
		if (result.energyJ <= result.capacityJ)
		{
			result.caught = true;
			result.residualVelocityMS = 0f;
			return result;
		}
		result.residualVelocityMS = Mathf.Sqrt(Mathf.Max(0f, velocityMS * velocityMS - 2f * result.capacityJ / massKg));
		return result;
	}

	public static float CatchVelocityMS(SpallLinerMaterial material, float thicknessM, float massKg, float fragDiameterM)
	{
		if (massKg <= 0f)
		{
			return 0f;
		}
		float num = AbsorptionJPerKg(material) * ArealDensityKgM2(material, thicknessM) * ((fragDiameterM > 0f) ? (fragDiameterM * Mathf.PI * fragDiameterM * 0.25f) : 0f);
		return Mathf.Sqrt(Mathf.Max(0f, 2f * num / massKg));
	}
}
