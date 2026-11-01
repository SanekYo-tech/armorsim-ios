using System;
using UnityEngine;

public static class SceneDefaults
{
	public const float CraterVisibleFraction = 0.06f;

	public static readonly float SandboxCraterDiameterM;

	public const float PlateM = 1f;

	public const float PlateSliderMinM = 0.2f;

	public const float PlateSliderMaxM = 4f;

	public const float PlateGapM = 0.1f;

	public const float ApproachPlates = 1.5f;

	public const float PlateFrameFill = 0.75f;

	public const float DefaultFovDeg = 60f;

	public const float DefaultPitchDeg = 20f;

	public const float CameraBackMargin = 1.15f;

	public const float CraterFrameFill = 0.8f;

	public const float NearClipFraction = 0.2f;

	public const int AssemblyPlates = 5;

	public const float AssumedFrameRate = 60f;

	public const int MinLayerFrames = 10;

	public static float ApproachM => ApproachPlates;

	public static float CameraDistanceM => Mathf.Max(2f / 3f / Mathf.Tan(30f * Mathf.Deg2Rad), 2.3f / Mathf.Cos(DefaultPitchDeg * Mathf.Deg2Rad));

	public static float CameraBackReachM => CameraDistanceM * Mathf.Cos(DefaultPitchDeg * Mathf.Deg2Rad);

	public static float AssemblyDepthM => AssemblyPlates * PlateM * 0.5f;

	public static float MaxDistanceM => 4f / Mathf.Tan(30f * Mathf.Deg2Rad);

	public static int GridHalfCells => Math.Max(2, (int)Math.Ceiling(AssemblyDepthM * 1.3f));

	public static float PlateWidthFromCraterM(float craterDiameterM)
	{
		return craterDiameterM / CraterVisibleFraction;
	}

	public static float CraterDiameterM(TateModel.Penetrator p, TateModel.Target t, float rodDiameterM, float velocityMS)
	{
		return CraterModel.CraterDiameterRatio(p, t, velocityMS) * rodDiameterM;
	}

	public static float FramingDistanceM(float sizeM, float fovDeg, float fill)
	{
		sizeM = Mathf.Max(sizeM, 0.0001f);
		fill = Mathf.Clamp(fill, 0.05f, 1f);
		fovDeg = Mathf.Max(fovDeg, 1f);
		return sizeM * 0.5f / fill / Mathf.Tan(fovDeg * 0.5f * Mathf.Deg2Rad);
	}

	public static float MinDistanceM(float craterDiameterM)
	{
		craterDiameterM = Mathf.Max(craterDiameterM, 0.0001f);
		return craterDiameterM * 0.5f / CraterFrameFill / Mathf.Tan(30f * Mathf.Deg2Rad);
	}

	public static bool LayerPhaseReadable(float minLayerSeconds)
	{
		return minLayerSeconds * AssumedFrameRate >= MinLayerFrames;
	}

	public static string Describe(float craterDiameterM)
	{
		string a = string.Format("Умолчания сцены (шаг 27): плита {0:F2} м (кратер ⌀{1:F0} мм = ", PlateM, craterDiameterM * 1000f);
		string b = string.Format("{0:F1} % грани), зазор {1:F0} мм, подлёт {2:F2} м, ", craterDiameterM * 100f, PlateGapM * 1000f, ApproachM);
		string c = string.Format("камера {0:F2} м (зум {1:F3}…{2:F1} м), ", CameraDistanceM, MinDistanceM(craterDiameterM), MaxDistanceM);
		string d = string.Format("сетка ±{0} м.", GridHalfCells);
		return string.Concat(a, b, c, d);
	}

	static SceneDefaults()
	{
		SandboxCraterDiameterM = CraterDiameterM(TateModel.WHA(), TateModel.RHA(), ProjectileLibrary.Default().diameterMM / 1000f, ProjectileLibrary.Default().velocityMps);
	}
}
