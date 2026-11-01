using UnityEngine;

public static class TrajectoryModel
{
	public enum ObliqueEvent
	{
		Penetrate = 0,
		Ricochet = 1,
		CriticalRicochet = 2,
		Slide = 3
	}

	public struct ObliqueDecision
	{
		public ObliqueEvent evt;

		public bool thinPlate;

		public float tan2Beta;

		public float tan2Threshold;

		public float betaCritDeg;

		public float bernoulliU;

		public float slideVc;

		public float erosionOnset;
	}

	public static float RicochetSpeedFactor = 0.8f;

	public static float RicochetErosionOfL0 = 0.15f;

	public static float RicochetInducedYawDeg = 3f;

	public static float CriticalBandFraction = 0.1f;

	public static float CriticalRicochetSpeedFactor = 0.65f;

	public static float CriticalRicochetErosionOfL0 = 0.25f;

	public static float SlideMinAngleDeg = 30f;

	public static float SlideSpeedFactor = 0.7f;

	public static float SlideErosionOfL0 = 0.3f;

	public static float SlideGrooveDepthCalibers = 0.2f;

	public static float DeflectionCoeff = 1f;

	public static float DeflectionMaxDeg = 6f;

	public static bool DeflectTowardNormal = false;

	public static float TailBreakYawRatio = 4f;

	public static float TailBreakLengthCut = 0.3f;

	public const float BjerkeVMinKmS = 1.2f;

	public const float BjerkeVMaxKmS = 4.7f;

	public static ObliqueDecision Classify(TateModel.Penetrator p, TateModel.Target t, float V, float betaDeg, float tLosMM, float diameterMM, NoseShape nose = NoseShape.Ogive, bool ballisticCap = false, float rodLengthM = 0f)
	{
		ObliqueDecision d = new ObliqueDecision
		{
			betaCritDeg = 90f,
			slideVc = SlideCriticalVelocity(p, t)
		};
		d.erosionOnset = RigidPenetration.ErosionOnset(p, t, V, ballisticCap);
		bool thinPlate = t.Rt < 0.5f * p.density * V * V + p.Yp;
		float cosBeta = Mathf.Cos(betaDeg * Mathf.Deg2Rad);
		if (diameterMM > tLosMM * cosBeta)
		{
			d.thinPlate = true;
			return d;
		}
		if (d.erosionOnset > 0f)
		{
			if (!thinPlate)
			{
				float u = Mathf.Clamp(TateModel.PenetrationVelocity(p, t, V), 0f, V);
				d.bernoulliU = u;
				if (V - u <= 0.001f)
				{
					return d;
				}
				d.tan2Threshold = (u + V) / (V - u) * (p.density * V * V / t.Rt);
				if (d.erosionOnset < 1f)
				{
					float betaCrit = RigidPenetration.CriticalRicochetDeg(nose, rodLengthM, diameterMM / 1000f);
					float tan2Crit = Mathf.Tan(betaCrit * Mathf.Deg2Rad);
					tan2Crit *= tan2Crit;
					d.tan2Threshold = tan2Crit + (d.tan2Threshold - tan2Crit) * Mathf.Clamp01(d.erosionOnset);
				}
				d.betaCritDeg = Mathf.Atan(Mathf.Sqrt(d.tan2Threshold)) * Mathf.Rad2Deg;
				d.tan2Beta = Mathf.Tan(betaDeg * Mathf.Deg2Rad);
				d.tan2Beta *= d.tan2Beta;
			}
			else
			{
				if (betaDeg > SlideMinAngleDeg)
				{
					d.evt = ObliqueEvent.Slide;
				}
				return d;
			}
		}
		else
		{
			d.betaCritDeg = RigidPenetration.CriticalRicochetDeg(nose, rodLengthM, diameterMM / 1000f);
			d.tan2Threshold = Mathf.Tan(d.betaCritDeg * Mathf.Deg2Rad);
			d.tan2Threshold *= d.tan2Threshold;
			d.tan2Beta = Mathf.Tan(betaDeg * Mathf.Deg2Rad);
			d.tan2Beta *= d.tan2Beta;
		}
		if (d.tan2Beta > (1f + CriticalBandFraction) * d.tan2Threshold)
		{
			d.evt = ObliqueEvent.Ricochet;
		}
		else if (d.tan2Beta >= (1f - CriticalBandFraction) * d.tan2Threshold)
		{
			d.evt = ObliqueEvent.CriticalRicochet;
		}
		return d;
	}

	public static float SlideCriticalVelocity(TateModel.Penetrator p, TateModel.Target t)
	{
		if (p.Yp >= t.Rt)
		{
			return 0f;
		}
		return Mathf.Sqrt(2f * (t.Rt - p.Yp) / p.density);
	}

	public static float DeflectionDeg(TateModel.Penetrator p, TateModel.Target t, float V, float betaDeg, float tEffMM, float L0M)
	{
		if (betaDeg <= 0f || V <= 1f)
		{
			return 0f;
		}
		float result = tEffMM / 1000f / Mathf.Max(L0M, 0.0001f) * (betaDeg * DeflectionCoeff) * (t.Rt / (p.density * V * V));
		return Mathf.Clamp(result, 0f, DeflectionMaxDeg);
	}

	public static float CriticalYawDeg(float V, float rodLengthM, float diameterM)
	{
		float v = Mathf.Clamp(V / 1000f, BjerkeVMinKmS, BjerkeVMaxKmS);
		float length = Mathf.Max(rodLengthM, 0.0001f) * 2f;
		float x = ((0.1286f * v * v + (0.3388f * v + 1.1524f)) * diameterM - diameterM) / length;
		return Mathf.Acos(Mathf.Clamp01(x)) * Mathf.Rad2Deg;
	}

	public static float YawPenalty(float gammaDeg, float gammaCrDeg)
	{
		if (gammaCrDeg <= 0f || gammaDeg <= gammaCrDeg)
		{
			return 1f;
		}
		return Mathf.Clamp01(Mathf.Cos(gammaDeg / gammaCrDeg * 0.2f));
	}
}
