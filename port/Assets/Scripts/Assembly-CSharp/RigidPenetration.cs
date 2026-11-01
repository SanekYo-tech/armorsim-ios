using UnityEngine;

public static class RigidPenetration
{
	public const float ConfinementFactor = 2.5f;

	public const float TransitionBand = 0.5f;

	public const float BallisticCapFactor = 1.35f;

	public const float RicochetLDCoeff = 1.5f;

	public const float RicochetLDReference = 4f;

	public const float InitialNoseArea = 1f;

	public static float NoseFactorN1(NoseShape shape)
	{
		switch (shape)
		{
		case NoseShape.Flat:
			return 1f;
		case NoseShape.Conical:
			return 0.1f;
		case NoseShape.Ogive:
			return 0.106f;
		default:
			return 0.295f;
		}
	}

	public static float RigidResistancePa(TateModel.Target t)
	{
		return t.Rrigid > 0f ? t.Rrigid : t.Rt;
	}

	public static bool HasRigidBranch(TateModel.Target t)
	{
		return t.Rrigid > 0f;
	}

	private static float ConfinedStrength(TateModel.Penetrator p, bool ballisticCap)
	{
		float confinement = p.confinement > 0f ? p.confinement : ConfinementFactor;
		if (ballisticCap)
		{
			confinement *= BallisticCapFactor;
		}
		return p.Yp * confinement;
	}

	public static float ErosionOnset(TateModel.Penetrator p, TateModel.Target t, float velocityMS, bool ballisticCap = false)
	{
		if (!HasRigidBranch(t))
		{
			return 1f;
		}
		float pressure = RigidResistancePa(t) + 0.5f * t.density * velocityMS * velocityMS;
		float strength = ConfinedStrength(p, ballisticCap);
		if (strength <= 0f)
		{
			return 1f;
		}
		if (strength >= pressure)
		{
			return 0f;
		}
		return Mathf.Clamp01((pressure - strength) / (strength * 0.5f));
	}

	public static bool IsRigid(TateModel.Penetrator p, TateModel.Target t, float velocityMS, bool ballisticCap = false)
	{
		return ErosionOnset(p, t, velocityMS, ballisticCap) <= 0f;
	}

	public static float RigidLimitVelocity(TateModel.Penetrator p, TateModel.Target t, bool ballisticCap = false)
	{
		if (!HasRigidBranch(t) || t.density <= 0f)
		{
			return 0f;
		}
		float delta = ConfinedStrength(p, ballisticCap) - RigidResistancePa(t);
		if (delta <= 0f)
		{
			return 0f;
		}
		return Mathf.Sqrt(delta / (0.5f * t.density));
	}

	private static float BaseRicochetAngleDeg(NoseShape shape)
	{
		switch (shape)
		{
		case NoseShape.Flat:
			return 75f;
		case NoseShape.Conical:
			return 65f;
		case NoseShape.Ogive:
			return 62f;
		case NoseShape.BluntOgive:
			return 72f;
		default:
			return 70f;
		}
	}

	public static float CriticalRicochetDeg(NoseShape shape, float rodLengthM = 0f, float diameterM = 0f)
	{
		float ld = rodLengthM > 0f && diameterM > 1E-06f ? rodLengthM / diameterM : RicochetLDReference;
		return Mathf.Clamp(BaseRicochetAngleDeg(shape) + (ld - RicochetLDReference) * RicochetLDCoeff, 45f, 85f);
	}

	public static float SemiInfiniteDepthM(TateModel.Penetrator p, TateModel.Target t, NoseShape shape, float rodLengthM, float velocityMS)
	{
		float n1 = NoseFactorN1(shape);
		float resistance = RigidResistancePa(t);
		if (resistance <= 0f || n1 <= 0f || t.density <= 0f)
		{
			return 0f;
		}
		return Mathf.Log(1f + n1 * t.density * velocityMS * velocityMS / resistance) * (p.density * rodLengthM / (2f * n1 * t.density));
	}

	public static float RigidDeceleration(TateModel.Penetrator p, TateModel.Target t, NoseShape shape, float lengthM, float velocityMS, float noseArea = 1f)
	{
		float n1 = EffectiveNoseFactorN1(shape, noseArea);
		float result = -(n1 * t.density * velocityMS * velocityMS + RigidResistancePa(t)) / (Mathf.Max(lengthM, 0.0001f) * p.density);
		if (noseArea > 1f)
		{
			result *= noseArea;
		}
		return result;
	}

	public static float GrowNoseArea(TateModel.Penetrator p, TateModel.Target t, float noseArea, float velocityMS, float s, float dt)
	{
		return noseArea;
	}

	public static float EffectiveNoseFactorN1(NoseShape shape, float noseArea)
	{
		float n1 = NoseFactorN1(shape);
		if (noseArea <= 1f)
		{
			return n1;
		}
		return n1 + (noseArea - 1f) / noseArea * (1f - n1);
	}

	public static void MixTransition(float s, float v, float uTate, float dLTate, float dvTate, float dvRigid, out float u, out float dLdt, out float dvdt)
	{
		s = Mathf.Clamp01(s);
		u = v + (uTate - v) * s;
		dLdt = dLTate * s;
		dvdt = dvRigid + (dvTate - dvRigid) * s;
	}
}
