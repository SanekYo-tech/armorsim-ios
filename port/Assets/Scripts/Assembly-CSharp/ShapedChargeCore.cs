using System;
using System.Collections.Generic;
using UnityEngine;

public static class ShapedChargeCore
{
	private const float DegenerateDensityFraction = 0.001f;

	public const float RefJetFlowStressPa = 300000000f;

	public const float RefInitialStrainRate = 100000f;

	public const float RefConeDiameterM = 0.085f;

	public const float RefR0OverCD = 0.0294118f;

	public static readonly float RefLinerRadiusM = RefConeDiameterM * RefR0OverCD;

	public const float RtSofteningULo = 1350f;

	public const float RtSofteningUHi = 4100f;

	public const float RtSofteningRatio = 0.5660377f;

	public const float RtIterationTolerancePa = 1000000f;

	public const int RtMaxIterations = 5;

	public const bool KbIsFitted = true;

	public const float KbFitToTheoryLimit = 3f;

	public static JetMaterial Copper()
	{
		return new JetMaterial
		{
			name = "Медь",
			density = 8960f
		};
	}

	public static float ChouCarleoneBreakupTime(float jetFlowStressPa, float jetDensity, float initialRadiusM, float initialStrainRate)
	{
		if (jetFlowStressPa > 0f && jetDensity > 0f && initialRadiusM > 0f && initialStrainRate > 0f)
		{
			float wave = Mathf.Sqrt(jetFlowStressPa / jetDensity);
			float x = initialRadiusM * initialStrainRate / wave;
			return (3.75f - x * 0.125f + 1f / x) * initialRadiusM / wave;
		}
		Debug.LogError("ShapedChargeCore.ChouCarleoneBreakupTime: некорректные входные данные — " + string.Format("Yj={0:G9}, rhoJ={1:G9}, r0={2:G9}, ", jetFlowStressPa, jetDensity, initialRadiusM) + string.Format("strainRate={0:G9}", initialStrainRate));
		return float.PositiveInfinity;
	}

	public static float TheoreticalBreakupScaleFactor()
	{
		return ChouCarleoneBreakupTime(RefJetFlowStressPa, Copper().density, RefLinerRadiusM, RefInitialStrainRate) / RefConeDiameterM;
	}

	public static float BreakupTime(ShapedChargeSpec spec)
	{
		if (spec.breakupTimeOverrideS > 0f)
		{
			return spec.breakupTimeOverrideS;
		}
		float value = spec.coneDiameterM * spec.breakupScaleFactor;
		return value > 0f ? value : float.PositiveInfinity;
	}

	public static float JetMass(ShapedChargeSpec spec)
	{
		if (spec.jetMassOverrideKg > 0f)
		{
			return spec.jetMassOverrideKg;
		}
		float value = spec.jetMassScaleFactor * spec.coneDiameterM * spec.coneDiameterM * spec.coneDiameterM;
		return value > 0f ? value : 0f;
	}

	public static float DispersionTime(ShapedChargeSpec spec)
	{
		if (spec.dispersionTimeOverrideS > 0f)
		{
			return spec.dispersionTimeOverrideS;
		}
		float value = spec.coneDiameterM * spec.dispersionScaleFactor;
		return value > 0f ? value : float.PositiveInfinity;
	}

	public static float SzendreiHeldCraterRadius(float jetRadiusM, float velocity, float targetStrengthPa, float jetDensity)
	{
		if (jetRadiusM <= 0f || velocity <= 0f || targetStrengthPa <= 0f || jetDensity <= 0f)
		{
			return 0f;
		}
		return jetRadiusM * Mathf.Sqrt(1f + jetDensity * velocity * velocity / (2f * targetStrengthPa));
	}

	public static float ReamedRadius(float currentRadiusM, float jetRadiusM, float axialExtentRatio = 1f)
	{
		if (jetRadiusM <= 0f || currentRadiusM < 0f)
		{
			return currentRadiusM;
		}
		if (axialExtentRatio <= 0f)
		{
			axialExtentRatio = 1f;
		}
		return Mathf.Sqrt(currentRadiusM * currentRadiusM + jetRadiusM * jetRadiusM / axialExtentRatio);
	}

	public static float DispersionTimeFromCrater(float acceptanceRadiusM, float sigmaTransverseMS)
	{
		if (acceptanceRadiusM <= 0f || sigmaTransverseMS <= 0f)
		{
			return float.PositiveInfinity;
		}
		return acceptanceRadiusM / (Mathf.Sqrt(2f) * sigmaTransverseMS);
	}

	public static float TransverseSigmaAt(ShapedChargeSpec spec, float velocity)
	{
		if (spec.sigmaVelocityExponent == 0f)
		{
			return spec.sigmaTransverseMS;
		}
		float reference = spec.sigmaRefVelocityMS > 0f ? spec.sigmaRefVelocityMS : spec.vTip;
		if (reference > 0f && velocity > 0f)
		{
			return spec.sigmaTransverseMS * Mathf.Pow(reference / velocity, spec.sigmaVelocityExponent);
		}
		return spec.sigmaTransverseMS;
	}

	public static float DispersionEfficiencyFromBound(float vPerpMaxMS, float sigmaTransverseMS)
	{
		if (sigmaTransverseMS <= 0f || vPerpMaxMS == float.PositiveInfinity)
		{
			return 1f;
		}
		if (vPerpMaxMS <= 0f)
		{
			return 0f;
		}
		return Erf(vPerpMaxMS / (Mathf.Sqrt(2f) * sigmaTransverseMS));
	}

	public static float TargetResistanceAtU(float u, float rHiPa)
	{
		if (rHiPa <= 0f)
		{
			return 0f;
		}
		float rLoPa = rHiPa * RtSofteningRatio;
		if (u <= RtSofteningULo)
		{
			return rHiPa;
		}
		if (u >= RtSofteningUHi)
		{
			return rLoPa;
		}
		return rHiPa - (u - RtSofteningULo) / (RtSofteningUHi - RtSofteningULo) * (rHiPa - rLoPa);
	}

	public static float InterfaceVelocitySoftened(float v, float rhoJ, float rhoT, float targetRtPa, out float resolvedRtPa, out int iterations)
	{
		float rt = targetRtPa;
		iterations = 0;
		bool again;
		do
		{
			float u = InterfaceVelocity(v, rhoJ, rhoT, rt);
			float next = targetRtPa > 0f ? TargetResistanceAtU(u, targetRtPa) : 0f;
			again = Mathf.Abs(next - rt) > RtIterationTolerancePa;
			rt = next;
			iterations++;
		}
		while (again && iterations < RtMaxIterations);
		resolvedRtPa = rt;
		return InterfaceVelocity(v, rhoJ, rhoT, rt);
	}

	public static float ElementRadius(float massKg, float lengthM, float density)
	{
		if (massKg <= 0f || lengthM <= 0f || density <= 0f)
		{
			return 0f;
		}
		return Mathf.Sqrt(massKg / (density * Mathf.PI * lengthM));
	}

	public static ShapedChargeSpec DemoPlaceholder()
	{
		return new ShapedChargeSpec
		{
			name = "DEMO_PLACEHOLDER",
			coneDiameterM = RefConeDiameterM,
			vTip = 9000f,
			vTail = 2500f,
			jetMassScaleFactor = 21.225254f,
			breakupScaleFactor = 0.00114885136f,
			dispersionAngleRad = 0.00390211912f,
			sigmaTransverseMS = 247.479416f,
			dispersionScaleFactor = 0.000647202018f,
			material = Copper(),
			provenance = "PLACEHOLDER"
		};
	}

	public static int ElementsSpentOn(JetElement[] timeline, float targetThicknessM)
	{
		if (timeline == null || timeline.Length == 0)
		{
			return 0;
		}
		for (int i = 0; i < timeline.Length; i++)
		{
			if (timeline[i].depthAfter >= targetThicknessM)
			{
				return i + 1;
			}
		}
		return timeline.Length;
	}

	public static ShapedChargeSpec RetireFromTip(ShapedChargeSpec jet, int elements, int retired, out int survivingElements)
	{
		elements = Mathf.Max(elements, 1);
		retired = Mathf.Clamp(retired, 1, elements - 1);
		int surviving = elements - retired;
		jet.vTail += (jet.vTip - jet.vTail) / elements * surviving;
		jet.jetMassOverrideKg = JetMass(jet) / elements * surviving;
		survivingElements = Mathf.Max(surviving, 1);
		return jet;
	}

	public static float CutoffVelocity(float rhoJ, float Rt)
	{
		if (rhoJ <= 0f || Rt <= 0f)
		{
			return 0f;
		}
		return Mathf.Sqrt(2f * Rt / rhoJ);
	}

	public static float HydrodynamicInterfaceVelocity(float v, float rhoJ, float rhoT)
	{
		if (v <= 0f || rhoJ <= 0f || rhoT <= 0f)
		{
			return 0f;
		}
		return Mathf.Sqrt(rhoJ) * v / (Mathf.Sqrt(rhoT) + Mathf.Sqrt(rhoJ));
	}

	public static float InterfaceVelocity(float v, float rhoJ, float rhoT, float Rt)
	{
		if (v <= 0f || rhoJ <= 0f || rhoT <= 0f)
		{
			return 0f;
		}
		float c = v * rhoJ * v - 2f * Rt;
		if (c <= 0f)
		{
			return 0f;
		}
		float a = rhoJ - rhoT;
		float b = -2f * rhoJ * v;
		if (Mathf.Abs(a) < rhoT * DegenerateDensityFraction)
		{
			return 0f - c / b;
		}
		float disc = b * b - 4f * a * c;
		if (disc < 0f)
		{
			return 0f;
		}
		float root = Mathf.Sqrt(disc);
		float u1 = (root - b) * 0.5f / a;
		float u2 = c / ((root - b) * 0.5f);
		bool valid1 = u1 > 0f && v > u1;
		bool valid2 = u2 > 0f && v > u2;
		if (valid1 && !valid2)
		{
			return u1;
		}
		if (valid2 && !valid1)
		{
			return u2;
		}
		int count = (valid1 ? 1 : 0) + (valid2 ? 1 : 0);
		Debug.LogError("ShapedChargeCore.InterfaceVelocity: неоднозначный выбор корня " + string.Format("(подходящих корней: {0}). ", count) + string.Format("v={0:G9}, rhoJ={1:G9}, rhoT={2:G9}, Rt={3:G9}; ", v, rhoJ, rhoT, Rt) + string.Format("a={0:G9}, b={1:G9}, c={2:G9}, disc={3:G9}, u1={4:G9}, u2={5:G9}", a, b, c, disc, u1, u2));
		return valid2 ? u2 : 0f;
	}

	public static float Erf(float x)
	{
		if (float.IsNaN(x) || x > 6f)
		{
			return 1f;
		}
		if (x <= 0f)
		{
			return 0f;
		}
		double xd = x;
		double t = 1.0 / (1.0 + 0.3275911 * xd);
		double p = t;
		p = p * 1.061405429 - 1.453152027;
		p = p * t + 1.421413741;
		p = p * t - 0.284496736;
		p = p * t + 0.254829592;
		p *= t;
		return (float)(1.0 - Math.Exp(0.0 - xd * xd) * p);
	}

	public static float DispersionEfficiency(float dtAfterBreakup, float tauD)
	{
		if (dtAfterBreakup <= 0f || tauD <= 0f)
		{
			return 1f;
		}
		return Erf(tauD / dtAfterBreakup);
	}

	public static float GetMassFraction(int i, int N)
	{
		return N > 0 ? 1f / N : 0f;
	}

	public static JetPenetrationOutcome Compute(ShapedChargeSpec spec, float standoffM, float targetThicknessM, float targetDensity, float targetRt, int elementCount = 300, bool craterFeedback = true, bool velocityDependentRt = true)
	{
		JetPenetrationOutcome outcome = new JetPenetrationOutcome
		{
			timeline = new JetElement[0]
		};
		int count = Mathf.Max(elementCount, 1);
		if (spec.material.density <= 0f || targetDensity <= 0f || spec.vTip <= 0f || spec.vTail <= 0f || spec.vTail >= spec.vTip || standoffM < 0f)
		{
			Debug.LogError("ShapedChargeCore.Compute: некорректные входные данные — " + string.Format("rhoJ={0:G9}, rhoT={1:G9}, vTip={2:G9}, ", spec.material.density, targetDensity, spec.vTip) + string.Format("vTail={0:G9}, standoff={1:G9}", spec.vTail, standoffM));
			return outcome;
		}
		float cutoff = spec.cutoffVelocityOverride > 0f ? spec.cutoffVelocityOverride : CutoffVelocity(spec.material.density, targetRt);
		float breakup = BreakupTime(spec);
		bool spatialSigma = spec.dispersionTimeOverrideS <= 0f && spec.dispersionAngleRad <= 0f && spec.sigmaTransverseMS > 0f;
		float tauD = spatialSigma ? float.PositiveInfinity : DispersionTime(spec);
		bool angularDispersion = spec.dispersionAngleRad > 0f;
		float tanAngle = angularDispersion ? Mathf.Tan(spec.dispersionAngleRad) : 0f;
		float totalMass = JetMass(spec);
		float dv = (spec.vTip - spec.vTail) / count;
		List<JetElement> timeline = new List<JetElement>(count);
		float depth = 0f;
		float maxCraterRadius = 0f;
		float acceptanceRadius = float.PositiveInfinity;
		float geometricCutoff = 0f;
		float consumedLength = 0f;
		float basePenetration = 0f;
		float actualPenetration = 0f;
		float rejectedMass = 0f;
		float rejectedMassRadius = 0f;
		float missedLength = 0f;
		int consumed = 0;
		int afterBreakup = 0;
		for (int i = count - 1; i >= 0; i--)
		{
			float velocity = spec.vTail + (i + 0.5f) * dv;
			float mass = totalMass * (1f / count);
			float arrival = (standoffM + (craterFeedback ? depth : 0f)) / velocity;
			float length = Mathf.Min(breakup, arrival) * dv;
			float radius = ElementRadius(mass, length, spec.material.density);
			JetElement element = new JetElement
			{
				velocity = velocity,
				mass = mass,
				arrivalTime = arrival,
				lengthAtArrival = length,
				radiusM = radius
			};
			if (cutoff >= velocity)
			{
				element.targetResistancePa = targetRt;
				element.belowCutoff = true;
				timeline.Add(element);
				break;
			}
			float resolvedRt;
			float u;
			if (velocityDependentRt)
			{
				u = InterfaceVelocitySoftened(velocity, spec.material.density, targetDensity, targetRt, out resolvedRt, out _);
			}
			else
			{
				resolvedRt = targetRt;
				u = InterfaceVelocity(velocity, spec.material.density, targetDensity, resolvedRt);
			}
			if (u <= 0f || u >= velocity)
			{
				element.targetResistancePa = targetRt;
				element.belowCutoff = true;
				timeline.Add(element);
				break;
			}
			float craterRadius = SzendreiHeldCraterRadius(radius, velocity, resolvedRt, spec.material.density);
			maxCraterRadius = Mathf.Max(maxCraterRadius, craterRadius);
			float efficiency;
			if (angularDispersion)
			{
				element.lateralOffsetM = (depth + standoffM) * tanAngle;
				element.acceptanceRadiusM = acceptanceRadius;
				if (acceptanceRadius >= element.lateralOffsetM)
				{
					efficiency = 1f;
					acceptanceRadius = craterRadius;
					geometricCutoff = velocity;
				}
				else
				{
					efficiency = 0f;
					element.geometricallyRejected = true;
					if (spec.channelReaming)
					{
						acceptanceRadius = ReamedRadius(acceptanceRadius, radius, spec.reamingAxialExtentRatio);
					}
				}
			}
			else if (!spatialSigma)
			{
				efficiency = DispersionEfficiency(arrival - breakup, tauD);
			}
			else
			{
				float sigma = TransverseSigmaAt(spec, velocity);
				float after = arrival - breakup;
				float bound = after > 0f ? craterRadius / after : float.PositiveInfinity;
				if (spec.angularCriterion && depth > 0f)
				{
					bound = Mathf.Min(velocity * maxCraterRadius / depth, bound);
				}
				efficiency = DispersionEfficiencyFromBound(bound, sigma);
			}
			float baseDelta = length * u / (velocity - u);
			float deltaP = baseDelta * efficiency;
			depth += deltaP;
			consumedLength += length;
			if (efficiency > 0f)
			{
				consumed++;
			}
			if (arrival > breakup)
			{
				afterBreakup++;
			}
			basePenetration += baseDelta;
			actualPenetration += deltaP;
			if (efficiency <= 0f)
			{
				rejectedMass += mass;
				rejectedMassRadius += mass * craterRadius;
			}
			missedLength += (1f - efficiency) * length;
			element.craterRadiusM = craterRadius;
			element.interfaceVelocity = u;
			element.targetResistancePa = resolvedRt;
			element.dispersionEfficiency = efficiency;
			element.deltaP = deltaP;
			element.depthAfter = depth;
			timeline.Add(element);
		}
		outcome.totalPenetrationM = depth;
		outcome.perforated = targetThicknessM > 0f && depth >= targetThicknessM;
		outcome.cutoffVelocity = cutoff;
		outcome.elementsConsumed = consumed;
		outcome.elementsTotal = count;
		outcome.elementsAfterBreakup = afterBreakup;
		outcome.consumedJetLengthM = consumedLength;
		outcome.dispersionLossFraction = basePenetration > 0f ? 1f - actualPenetration / basePenetration : 0f;
		outcome.craterRadiusEntryM = maxCraterRadius;
		outcome.craterRadiusMidsectionM = rejectedMass > 0f ? rejectedMassRadius / rejectedMass : 0f;
		outcome.missedJetLengthM = missedLength;
		outcome.geometricCutoffVelocity = geometricCutoff;
		outcome.timeline = timeline.ToArray();
		return outcome;
	}
}
