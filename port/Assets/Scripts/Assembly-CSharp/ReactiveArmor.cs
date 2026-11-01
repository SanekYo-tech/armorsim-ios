using UnityEngine;

public static class ReactiveArmor
{
	public struct RodLoadSchedule
	{
		public float forceN;

		public float stationTimeS;

		public float betweenDelayS;

		public float separationM;

		public float contactWidthM;

		public float engagementEndS;

		public float bothPlatesTimeS;

		public float rodVelocityMS;

		public float appliedLinearImpulse;

		public float appliedAngularImpulse;

		public float sweepLinearImpulse;

		public float sweepAngularImpulse;
	}

	public const float PlateDensityKgM3 = 7850f;

	public const float YawVerifiedLimitDeg = 5f;

	public static ReactiveArmorSpec DemoPlaceholder()
	{
		return new ReactiveArmorSpec
		{
			name = "ERA_DEMO_PLACEHOLDER",
			enabled = true,
			plateThicknessM = 0.0025f,
			plateVelocityMS = 700f,
			plateExtentM = 0.15f,
			plateDensity = PlateDensityKgM3,
			plateCount = 2
		};
	}

	public static float DutyCycle(float jetDiameterM, float interfaceVelocityMS, float obliquityRad, float plateVelocityMS, float plateThicknessM)
	{
		if (jetDiameterM <= 0f || interfaceVelocityMS <= 0f || plateVelocityMS <= 0f || plateThicknessM <= 0f)
		{
			return 0f;
		}
		float sin = Mathf.Sin(obliquityRad);
		float cos = Mathf.Cos(obliquityRad);
		if (sin <= 0f)
		{
			return 0f;
		}
		if (cos <= 0f)
		{
			return 1f;
		}
		float penetrationTime = plateThicknessM / cos / interfaceVelocityMS;
		float sweepTime = jetDiameterM / (sin * plateVelocityMS);
		return penetrationTime / (penetrationTime + sweepTime);
	}

	public static float StaticBoreLength(float plateThicknessM, float obliquityRad, float plateDensity, float jetDensity)
	{
		if (plateThicknessM <= 0f || plateDensity <= 0f || jetDensity <= 0f)
		{
			return 0f;
		}
		float cos = Mathf.Cos(obliquityRad);
		if (cos <= 0.0001f)
		{
			return 0f;
		}
		return Mathf.Sqrt(plateDensity / jetDensity) * (plateThicknessM / cos);
	}

	public static ReactiveArmorRodSpec DemoRodPlaceholder()
	{
		return new ReactiveArmorRodSpec
		{
			name = "ERA_ROD_DEMO_PLACEHOLDER",
			enabled = true,
			plateThicknessM = 0.015f,
			plateVelocityMS = 500f,
			interlayerGapM = 0.02f,
			plateExtentM = 0.15f,
			standoffBehindM = 0.5f,
			plateDensity = PlateDensityKgM3,
			holeDiameterFactor = 1.2f,
			couplingFactor = 1f
		};
	}

	public static float TransverseImpulse(ReactiveArmorRodSpec era, float rodDiameterM, float obliquityRad)
	{
		if (rodDiameterM <= 0f || era.plateThicknessM <= 0f || era.plateDensity <= 0f)
		{
			return 0f;
		}
		float cos = Mathf.Cos(obliquityRad);
		if (cos <= 0.0001f)
		{
			return 0f;
		}
		float holeDiameter = era.holeDiameterFactor * rodDiameterM;
		float lateralVelocity = Mathf.Max(Mathf.Tan(obliquityRad) * era.plateVelocityMS, 0f);
		float force = era.plateDensity * Mathf.PI * (holeDiameter * 0.5f) * (holeDiameter * 0.5f) * (era.plateThicknessM / cos) * era.couplingFactor;
		return lateralVelocity * force;
	}

	public static RodEraOutcome ApplyToRod(ReactiveArmorRodSpec era, float obliquityRad, float rodLengthM, float rodDiameterM, float rodDensity, float rodVelocityMS)
	{
		RodEraOutcome o = new RodEraOutcome
		{
			kYaw = 1f
		};
		if (!era.enabled || rodLengthM <= 0f || rodDiameterM <= 0f || rodDensity <= 0f || rodVelocityMS <= 0f)
		{
			return o;
		}
		float cos = Mathf.Cos(obliquityRad);
		if (cos <= 0.0001f)
		{
			return o;
		}
		float massPerLength = (rodDiameterM * 0.5f) * Mathf.PI * (rodDiameterM * 0.5f) * rodDensity;
		o.rodInertiaKgM2 = massPerLength * rodLengthM * rodLengthM * rodLengthM / 12f;
		float plateLosThickness = era.plateThicknessM / cos;
		float holeDiameter = era.holeDiameterFactor * rodDiameterM;
		float plateVelocity = era.cassetteExpended ? 0f : era.plateVelocityMS;
		o.lateralVelocityMS = Mathf.Tan(obliquityRad) * plateVelocity;
		o.displacedMassRateKgS = era.plateDensity * holeDiameter * Mathf.Max(o.lateralVelocityMS, 0f) * plateLosThickness;
		o.forceN = o.displacedMassRateKgS * era.couplingFactor * Mathf.Max(o.lateralVelocityMS, 0f);
		o.plateSeparationM = (era.interlayerGapM + era.plateThicknessM) / cos;
		float separationTime = o.plateSeparationM / rodVelocityMS;
		float plateEngagement = plateVelocity > 0f ? era.plateExtentM / plateVelocity : float.PositiveInfinity;
		o.engagementEndS = Mathf.Min(rodLengthM / rodVelocityMS, plateEngagement);
		o.bothPlatesTimeS = Mathf.Max(o.engagementEndS - separationTime, 0f);
		o.transverseImpulse = o.engagementEndS * o.forceN;
		o.coupleAngularImpulse = o.plateSeparationM * o.forceN * o.bothPlatesTimeS;
		float singlePlateTime = Mathf.Min(separationTime, o.engagementEndS);
		o.singlePlateAngularImpulse = (rodLengthM - singlePlateTime * rodVelocityMS) * (singlePlateTime * o.forceN) * 0.5f;
		o.singlePlateLinearImpulse = singlePlateTime * o.forceN;
		o.angularImpulse = o.coupleAngularImpulse - o.singlePlateAngularImpulse;
		o.omegaRadS = o.rodInertiaKgM2 > 0f ? o.angularImpulse / o.rodInertiaKgM2 : 0f;
		o.singlePlateDominates = o.angularImpulse < 0f;
		o.yawAngleRad = Mathf.Abs(o.omegaRadS) * (era.standoffBehindM / rodVelocityMS);
		o.yawAngleDeg = o.yawAngleRad * Mathf.Rad2Deg;
		o.yawBeyondVerifiedRange = o.yawAngleDeg > YawVerifiedLimitDeg;
		o.stationTimeS = holeDiameter / rodVelocityMS;
		o.stationImpulse = o.stationTimeS * o.forceN;
		o.stationMassKg = plateLosThickness * massPerLength;
		o.stationVelocityMS = o.stationMassKg > 0f ? o.stationImpulse / o.stationMassKg : 0f;
		o.localDeflectionRad = Mathf.Atan(o.stationVelocityMS / rodVelocityMS);
		o.linearMomentumResidual = o.singlePlateLinearImpulse;
		o.angularMomentumResidual = o.omegaRadS * o.rodInertiaKgM2 - o.angularImpulse;
		o.criticalYawDeg = TrajectoryModel.CriticalYawDeg(rodVelocityMS, rodLengthM, rodDiameterM);
		o.kYaw = TrajectoryModel.YawPenalty(o.yawAngleDeg, o.criticalYawDeg);
		return o;
	}

	public static float FrontStationAt(RodLoadSchedule s, float t)
	{
		return t * s.rodVelocityMS;
	}

	public static float RearStationAt(RodLoadSchedule s, float t)
	{
		return t * s.rodVelocityMS - s.separationM;
	}

	public static float TorqueAt(RodLoadSchedule s, float t, float rodLengthM)
	{
		float frontLever = t * s.rodVelocityMS - rodLengthM * 0.5f;
		float rear = t * s.rodVelocityMS - s.separationM;
		if (rear >= 0f)
		{
			return s.forceN * frontLever - s.forceN * (rear - rodLengthM * 0.5f);
		}
		return s.forceN * frontLever;
	}

	public static RodLoadSchedule BuildRodLoadSchedule(ReactiveArmorRodSpec era, float obliquityRad, float rodLengthM, float rodDiameterM, float rodDensity, float rodVelocityMS)
	{
		RodLoadSchedule s = new RodLoadSchedule();
		RodEraOutcome o = ApplyToRod(era, obliquityRad, rodLengthM, rodDiameterM, rodDensity, rodVelocityMS);
		if (o.forceN > 0f && rodVelocityMS > 0f)
		{
			s.forceN = o.forceN;
			s.stationTimeS = o.stationTimeS;
			s.rodVelocityMS = rodVelocityMS;
			s.separationM = o.plateSeparationM;
			s.betweenDelayS = o.plateSeparationM / rodVelocityMS;
			s.contactWidthM = era.holeDiameterFactor * rodDiameterM;
			s.engagementEndS = o.engagementEndS;
			s.bothPlatesTimeS = o.bothPlatesTimeS;
			s.appliedLinearImpulse = o.singlePlateLinearImpulse;
			s.appliedAngularImpulse = o.angularImpulse;
			s.sweepLinearImpulse = o.singlePlateLinearImpulse;
			s.sweepAngularImpulse = 0f - o.angularImpulse;
		}
		return s;
	}

	public static RodPostEraState AnalyticPostEraState(RodEraOutcome o, float rodLengthM)
	{
		return new RodPostEraState
		{
			effectiveLengthM = rodLengthM,
			fragmentCount = 1,
			rigidYawRad = o.yawAngleRad,
			timeToFractureS = float.NaN,
			maxLocalAngleRad = o.localDeflectionRad
		};
	}

	public static RodPostEraState NeutralPostEraState(float rodLengthM)
	{
		return new RodPostEraState
		{
			effectiveLengthM = rodLengthM,
			fragmentCount = 1,
			timeToFractureS = float.NaN
		};
	}

	public static EraOutcome Apply(ShapedChargeSpec jet, ReactiveArmorSpec era, float obliquityRad, float standoffToEraM, int elementCount = 300)
	{
		elementCount = Mathf.Max(elementCount, 1);
		EraOutcome o = new EraOutcome
		{
			survivingJet = jet,
			survivingElementCount = elementCount
		};
		if (!era.enabled || era.cassetteExpended || era.plateCount <= 0 || jet.material.density <= 0f || standoffToEraM <= 0f || jet.vTail >= jet.vTip || era.plateThicknessM <= 0f)
		{
			return o;
		}
		float dv = (jet.vTip - jet.vTail) / elementCount;
		float breakup = ShapedChargeCore.BreakupTime(jet);
		float elementMass = ShapedChargeCore.JetMass(jet) / elementCount;
		float tipTime = standoffToEraM / jet.vTip;
		float tailTime = standoffToEraM / jet.vTail;
		float radius = ShapedChargeCore.ElementRadius(elementMass, Mathf.Min(breakup, tipTime) * dv, jet.material.density);
		float interfaceVelocity = ShapedChargeCore.InterfaceVelocitySoftened(jet.vTip, jet.material.density, era.plateDensity, TateModel.RHA().Rt, out _, out _);
		float plateVelocity = era.cassetteExpended ? 0f : era.plateVelocityMS;
		float duty = DutyCycle(radius * 2f, interfaceVelocity, obliquityRad, plateVelocity, era.plateThicknessM);
		float engagement = Mathf.Min(tailTime - tipTime, plateVelocity > 0f ? era.plateExtentM / plateVelocity : float.PositiveInfinity);
		float staticBore = StaticBoreLength(era.plateThicknessM, obliquityRad, era.plateDensity, jet.material.density);
		float sweep = Mathf.Max(jet.vTip - interfaceVelocity, 0f) * duty * engagement;
		o.engagementTimeS = engagement;
		o.interfaceVelocityMS = interfaceVelocity;
		o.eraDutyCycle = duty;
		o.staticBoreLengthM = era.plateCount * staticBore;
		o.sweepLengthM = era.plateCount * sweep;
		float eraLength = era.plateCount * (staticBore + sweep);
		float jetLength = 0f;
		for (int i = elementCount - 1; i >= 0; i--)
		{
			float arrival = standoffToEraM / (jet.vTail + (i + 0.5f) * dv);
			jetLength += Mathf.Min(breakup, arrival) * dv;
		}
		if (eraLength >= jetLength)
		{
			o.eraConsumedLengthM = jetLength;
			o.eraElementsRetired = elementCount;
			o.survivingElementCount = 0;
			o.eraFullyDefeated = true;
			return o;
		}
		float consumed = 0f;
		int retired = 0;
		for (int i = elementCount - 1; i >= 0 && consumed < eraLength; i--)
		{
			float arrival = standoffToEraM / (jet.vTail + (i + 0.5f) * dv);
			consumed += Mathf.Min(breakup, arrival) * dv;
			retired++;
		}
		int surviving = elementCount - retired;
		o.eraConsumedLengthM = consumed;
		o.eraElementsRetired = retired;
		o.survivingElementCount = surviving;
		jet.vTail += surviving * dv;
		jet.jetMassOverrideKg = surviving * elementMass;
		o.survivingJet = jet;
		return o;
	}
}
