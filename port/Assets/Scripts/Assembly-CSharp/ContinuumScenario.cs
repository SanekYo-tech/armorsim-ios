using System;
using UnityEngine;

public sealed class ContinuumScenario
{
	public string name;

	public float rodDiameterM;

	public float rodLengthM;

	public float impactVelocityMps;

	public ContinuumNose nose;

	public float noseLengthCalibres;

	public float noseHalfAngleDeg;

	public float tipFlatOverD;

	public NoseMeshMode noseMeshMode;

	public ContinuumMaterialId rodMaterial;

	public ContinuumTargetKind targetKind;

	public float targetThicknessM;

	public ContinuumMaterialId targetMaterial;

	public ContinuumLayer[] layers;

	public float initialGapM;

	public float elementsPerDiameter;

	public float uniformRadiusCalibres;

	public float outerRadiusCalibres;

	public float gradingRatio;

	public float cflSafety;

	public float dtFloorS;

	public float hourglassQ;

	public static float HourglassQOverride;

	public float viscosityQuadratic;

	public float viscosityLinear;

	public float contactScale;

	public float contactDampingRatio;

	public float contactPushSpeedMps;

	public float friction;

	public float erodeEpsP;

	public float erodeVolumeFraction;

	public AsbDeletionGate asbDeletionPressureGate;

	public float deleteMaxPressureFraction;

	public bool enableJcDamage;

	public bool enableAsbFade;

	public float durationS;

	public int outputFrames;

	public int maxSteps;

	public float hOverrideM;

	public float initialTemperatureK;

	public ContinuumProperties[] materialOverride;

	public float EffectiveNoseLengthCalibres => noseHalfAngleDeg > 0f ? 0.5f / Mathf.Max(1E-06f, Mathf.Tan(Mathf.Clamp(noseHalfAngleDeg, 1f, 89f) * Mathf.Deg2Rad)) : noseLengthCalibres;

	public float ElementSizeM => hOverrideM > 0f ? hOverrideM : rodDiameterM / Mathf.Max(1f, elementsPerDiameter);

	public bool HasLayerStack => layers != null && layers.Length != 0;

	public float TargetSpanM
	{
		get
		{
			if (layers == null || layers.Length == 0)
			{
				return targetThicknessM;
			}
			float span = 0f;
			for (int i = 0; i < layers.Length; i++)
			{
				span += layers[i].gapBeforeM + layers[i].thicknessM;
			}
			return span;
		}
	}

	public float TargetThicknessTotalM
	{
		get
		{
			if (layers == null || layers.Length == 0)
			{
				return targetThicknessM;
			}
			float thickness = 0f;
			for (int i = 0; i < layers.Length; i++)
			{
				thickness += layers[i].thicknessM;
			}
			return thickness;
		}
	}

	public float DefaultDuration()
	{
		if (durationS > 0f)
		{
			return durationS;
		}
		float rhoRod = ContinuumMaterials.Get(rodMaterial).rho0;
		float rhoTarget = ContinuumMaterials.Get(targetMaterial).rho0;
		float densityRatio = Mathf.Max(0.1f, Mathf.Sqrt(rhoRod / Mathf.Max(1f, rhoTarget)));
		float v = impactVelocityMps;
		v -= v / (1f / densityRatio + 1f);
		v = Mathf.Max(1f, v);
		return rodLengthM / v * 1.6f + 5E-06f;
	}

	public ContinuumScenario Clone()
	{
		return (ContinuumScenario)MemberwiseClone();
	}

	public static ContinuumScenario Etalon()
	{
		return new ContinuumScenario
		{
			name = "эталон: WHA L/D 10 @ 1100 → RHA 50 мм",
			rodDiameterM = 0.03f,
			rodLengthM = 0.3f,
			impactVelocityMps = 1100f,
			rodMaterial = ContinuumMaterialId.WHA,
			targetThicknessM = 0.05f,
			targetMaterial = ContinuumMaterials.ActiveRhaSet
		};
	}

	public static ContinuumScenario SemiInf1250()
	{
		return SemiInfiniteAnchor(1250f, 0.2044f, 0.35f);
	}

	public static ContinuumScenario SemiInf1700()
	{
		return SemiInfiniteAnchor(1700f, 0.318f, 0.5f);
	}

	public static ContinuumScenario SemiInfiniteAnchor(float v0Mps, float referenceDepthM, float blockDepthM)
	{
		float minOuterRadius = Mathf.Max(0.15f, referenceDepthM * 0.5f);
		float minBlockDepth = referenceDepthM * 1.5f;
		if (blockDepthM < minBlockDepth)
		{
			throw new ArgumentException(string.Format("блок {0:F0} мм мельче требуемых 1.5·P_f_ожид = {1:F1} мм", blockDepthM * 1000f, minBlockDepth * 1000f));
		}
		ContinuumScenario s = new ContinuumScenario
		{
			name = string.Format("опора 12a: WHA L/D 10 @ {0:F0} → RHA полубесконечная {1:F0} мм", v0Mps, blockDepthM * 1000f),
			impactVelocityMps = v0Mps,
			nose = ContinuumNose.Flat,
			rodMaterial = ContinuumMaterialId.WHA,
			targetThicknessM = blockDepthM,
			rodDiameterM = 0.03f,
			rodLengthM = 0.3f,
			targetKind = ContinuumTargetKind.SemiInfinite,
			targetMaterial = ContinuumMaterials.ActiveRhaSet,
			elementsPerDiameter = 16f
		};
		if (minOuterRadius > s.outerRadiusCalibres * 0.03f)
		{
			throw new ArgumentException(string.Format("радиус блока {0:F0} мм меньше требуемых ", s.outerRadiusCalibres * 0.03f * 1000f) + string.Format("{0:F1} мм", minOuterRadius * 1000f));
		}
		return s;
	}

	public static ContinuumScenario TaylorCylinder(float lengthM, float diameterM, float vMps, ContinuumMaterialId mat, float t0K)
	{
		return new ContinuumScenario
		{
			name = string.Format("Тейлор: L₀ {0:F2} мм, D₀ {1:F2} мм, {2:F0} м/с", lengthM * 1000f, diameterM * 1000f, vMps),
			rodDiameterM = diameterM,
			rodLengthM = lengthM,
			impactVelocityMps = vMps,
			initialTemperatureK = t0K,
			nose = ContinuumNose.Flat,
			rodMaterial = mat,
			targetKind = ContinuumTargetKind.RigidAnvil,
			friction = 0.12f,
			enableJcDamage = false,
			enableAsbFade = false,
			durationS = 0.0001f,
			elementsPerDiameter = 20f
		};
	}

	public ContinuumScenario()
	{
		name = "";
		rodDiameterM = 0.03f;
		rodLengthM = 0.3f;
		impactVelocityMps = 1100f;
		noseLengthCalibres = 1f;
		tipFlatOverD = 0.15f;
		targetThicknessM = 0.05f;
		targetMaterial = ContinuumMaterialId.RHA_MeyerKleponis;
		elementsPerDiameter = 40f;
		uniformRadiusCalibres = 3f;
		outerRadiusCalibres = 8f;
		gradingRatio = 1.05f;
		cflSafety = 0.67f;
		dtFloorS = 1E-11f;
		hourglassQ = 0.01f;
		viscosityQuadratic = 1.5f;
		viscosityLinear = 0.06f;
		contactScale = 0.1f;
		contactDampingRatio = 0.1f;
		friction = 0.1f;
		erodeEpsP = 2f;
		erodeVolumeFraction = 0.05f;
		deleteMaxPressureFraction = 0.01f;
		enableJcDamage = true;
		enableAsbFade = true;
		outputFrames = 200;
		maxSteps = 500000;
	}

	static ContinuumScenario()
	{
		HourglassQOverride = float.NaN;
	}
}
