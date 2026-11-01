using System.Collections.Generic;
using UnityEngine;

public static class PenetrationCore
{
	private struct IntegrationResult
	{
		public bool penetrated;

		public float v;

		public float L;

		public float depthM;

		public float sMax;

		public float noseArea;

		public List<TateStep> steps;
	}

	private const float Dt = 2E-07f;

	private const int MaxIter = 200000;

	private const float MinL = 0.0001f;

	private const int StepSampleInterval = 1000;

	private const float MinVelocity = 20f;

	public const float PlugShearFromRt = 0.12820512f;

	private const float BisectLo = 50f;

	private const float BisectHi = 3000f;

	private const int BisectIterations = 18;

	private static IntegrationResult Integrate(TateModel.Penetrator p, TateModel.Target t, float v0, float L0, float tEffM, bool collectSteps, float kYaw = 1f, NoseShape nose = NoseShape.Ogive, bool ballisticCap = false)
	{
		List<TateStep> steps = collectSteps ? new List<TateStep>() : null;
		float minL = Mathf.Max(MinL, L0 * 0.02f);
		float v = v0;
		float L = L0;
		float depthM = 0f;
		float s = 0f;
		float sMax = 0f;
		float noseArea = 1f;
		int iter = 0;
		do
		{
			float lengthM = Mathf.Max(MinL, L);
			s = Mathf.Max(s, RigidPenetration.ErosionOnset(p, t, v, ballisticCap));
			sMax = Mathf.Max(sMax, s);
			noseArea = RigidPenetration.GrowNoseArea(p, t, noseArea, v, s, Dt);
			float u;
			float dLdt;
			float dvdt;
			if (s <= 0f)
			{
				u = v;
				dLdt = 0f;
				dvdt = RigidPenetration.RigidDeceleration(p, t, nose, lengthM, v, noseArea);
			}
			else
			{
				float targetInertia = 0.5f * t.density * v * v;
				if (p.Yp + 0.5f * p.density * v * v <= t.Rt)
				{
					u = 0f;
					dvdt = -p.Yp / (lengthM * p.density);
					dLdt = -v;
				}
				else if (p.Yp < t.Rt + targetInertia)
				{
					u = Mathf.Clamp(TateModel.PenetrationVelocity(p, t, v), 0f, v);
					dvdt = -p.Yp / (lengthM * p.density);
					dLdt = -(v - u);
				}
				else
				{
					u = v;
					dvdt = -(t.Rt + targetInertia) / (lengthM * p.density);
					dLdt = 0f;
				}
				if (s < 1f)
				{
					float dvRigid = RigidPenetration.RigidDeceleration(p, t, nose, lengthM, v, noseArea);
					RigidPenetration.MixTransition(s, v, u, dLdt, dvdt, dvRigid, out u, out dLdt, out dvdt);
				}
			}
			v = Mathf.Max(0f, v + dvdt * Dt);
			L += dLdt * Dt;
			depthM += u * kYaw * Dt;
			bool penetrated = depthM >= tEffM;
			bool stopped = v <= MinVelocity;
			bool eroded = L <= minL;
			if (collectSteps && (penetrated || stopped || eroded || iter % StepSampleInterval == 0))
			{
				steps.Add(new TateStep
				{
					t = (float)(iter + 1) * Dt,
					depthMM = depthM * 1000f,
					v = v,
					L = L
				});
			}
			if (penetrated)
			{
				return new IntegrationResult
				{
					penetrated = true,
					v = v,
					L = Mathf.Max(0f, L),
					depthM = tEffM,
					sMax = sMax,
					noseArea = noseArea,
					steps = steps
				};
			}
			if (stopped || eroded)
			{
				return new IntegrationResult
				{
					penetrated = false,
					v = v,
					L = Mathf.Max(0f, L),
					depthM = depthM,
					sMax = sMax,
					noseArea = noseArea,
					steps = steps
				};
			}
			iter++;
		}
		while (iter < MaxIter);
		Debug.LogWarning(string.Format("TateIntegrate: достигнут maxIter={0} без сходимости (v0={1:F0} м/с, L0={2:F1} мм) — прервано принудительно.", MaxIter, v0, L0 * 1000f));
		return new IntegrationResult
		{
			penetrated = false,
			v = v,
			L = Mathf.Max(0f, L),
			depthM = depthM,
			sMax = sMax,
			noseArea = noseArea,
			steps = steps
		};
	}

	public static float BallisticLimit(TateModel.Penetrator p, TateModel.Target t, float lengthIn, float tEffM, float kYaw = 1f, NoseShape nose = NoseShape.Ogive, bool ballisticCap = false)
	{
		if (!Integrate(p, t, BisectHi, lengthIn, tEffM, false, kYaw, nose, ballisticCap).penetrated)
		{
			Debug.Log(string.Format("  Баллистический предел: >{0:F0} м/с (непробиваемо)", BisectHi));
			return float.PositiveInfinity;
		}
		if (Integrate(p, t, BisectLo, lengthIn, tEffM, false, kYaw, nose, ballisticCap).penetrated)
		{
			return BisectLo;
		}
		float lo = BisectLo;
		float hi = BisectHi;
		for (int i = 0; i < BisectIterations; i++)
		{
			float mid = (lo + hi) * 0.5f;
			if (Integrate(p, t, mid, lengthIn, tEffM, false, kYaw, nose, ballisticCap).penetrated)
			{
				hi = mid;
			}
			else
			{
				lo = mid;
			}
		}
		return (lo + hi) * 0.5f;
	}

	public static PenetrationOutcome Calculate(TateModel.Penetrator core, TateModel.Target armor, float velocity, float angleDeg, float thicknessMM, float rodLengthIn, float rodLengthOriginal, bool collectSteps = true, float kYaw = 1f, NoseShape nose = NoseShape.Ogive, bool ballisticCap = false, float diameterMM = 0f)
	{
		PenetrationOutcome outcome = default;
		float tEffM = thicknessMM / Mathf.Cos(angleDeg * Mathf.Deg2Rad) / 1000f;
		outcome.ballisticLimit = BallisticLimit(core, armor, rodLengthIn, tEffM, kYaw, nose, ballisticCap);
		IntegrationResult integration = Integrate(core, armor, velocity, rodLengthIn, tEffM, collectSteps, kYaw, nose, ballisticCap);
		outcome.steps = integration.steps;
		outcome.residualVelocity = integration.v;
		outcome.residualLengthM = integration.L;
		outcome.penetrationDepthMM = integration.depthM * 1000f;
		outcome.erosionOnsetMax = integration.sMax;
		outcome.noseAreaOut = integration.noseArea;
		outcome.erosionFraction = Mathf.Clamp01((rodLengthIn - integration.L) / Mathf.Max(MinL, rodLengthOriginal));
		if (!integration.penetrated)
		{
			if (diameterMM > 0f && thicknessMM < diameterMM)
			{
				float radiusM = diameterMM * 0.5f / 1000f;
				float area = Mathf.PI * radiusM * radiusM;
				float coreMass = area * core.density * Mathf.Max(MinL, rodLengthIn);
				float plugMass = area * armor.density * tEffM;
				float shear = armor.Rt * PlugShearFromRt * Mathf.PI * (diameterMM / 1000f) * tEffM * tEffM;
				float plugLimit = Mathf.Sqrt(2f * shear / Mathf.Max(1E-06f, coreMass));
				if (velocity > plugLimit)
				{
					float residual = Mathf.Sqrt(velocity * velocity - plugLimit * plugLimit) * (coreMass / (plugMass + coreMass));
					Debug.Log(string.Format("  ⊙ ПРОБКА: плита {0:F1} мм тоньше калибра {1:F0} мм — ", thicknessMM, diameterMM) + string.Format("расширение полости неприменимо, срез по периметру | v_bl = {0:F0} м/с, ", plugLimit) + string.Format("пробка {0:F1} г, v → {1:F0} м/с", plugMass * 1000f, residual));
					outcome.erosionFraction = 0f;
					outcome.residualVelocity = residual;
					outcome.residualLengthM = rodLengthIn;
					outcome.penetrationDepthMM = tEffM * 1000f;
					outcome.ballisticLimit = plugLimit;
					outcome.erosionOnsetMax = 0f;
					outcome.noseAreaOut = 1f;
					return outcome;
				}
			}
			outcome.residualVelocity = 0f;
			outcome.result = PenetrationResult.Stopped;
		}
		return outcome;
	}
}
