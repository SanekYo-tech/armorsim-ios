using UnityEngine;

public static class BodyAnatomy
{
	public readonly struct Organ
	{
		public readonly string Id;

		public readonly Vector3 CentreMm;

		public readonly Vector3 SemiAxesMm;

		public readonly float DensityKgM3;

		public Organ(string id, Vector3 centreMm, Vector3 semiAxesMm, float densityKgM3)
		{
			Id = id;
			CentreMm = centreMm;
			SemiAxesMm = semiAxesMm;
			DensityKgM3 = densityKgM3;
		}
	}

	public const float HeightMm = 600f;

	public const float OuterRadiusMm = 150f;

	public const float DepthRadiusMm = 110f;

	public const float DepthRatio = 11f / 15f;

	public const float DepthOffsetMm = 40f;

	public const float SkinMm = 2f;

	public const float FatDefaultMm = 20f;

	public const float FatMinMm = 5f;

	public const float FatMaxMm = 30f;

	public const float MuscleDefaultMm = 15f;

	public const float MuscleMinMm = 10f;

	public const float MuscleMaxMm = 40f;

	public const float RibBoneMm = 10f;

	public const float RibGapMm = 12f;

	public const float RibFirstYMm = 250f;

	public const int RibCount = 10;

	public const float RibCorticalMm = 2f;

	public const float RibOuterRadiusMm = 113f;

	public const float RibThicknessMm = 10f;

	public const float SternumThicknessMm = 15f;

	public const float SternumHalfWidthMm = 30f;

	public const float SternumHalfHeightMm = 90f;

	public static readonly Vector3 SternumCentreMm = new Vector3(0f, 380f, -108f);

	public const float SpineDiameterMm = 35f;

	public static readonly Vector3 SpineCentreMm = new Vector3(0f, 300f, 85f);

	public const float AortaDiameterMm = 25f;

	public static readonly Vector3 AortaCentreMm = new Vector3(0f, 300f, 55f);

	public static readonly Organ[] Organs = new Organ[8]
	{
		new Organ("heart", new Vector3(15f, 0f, 0f), new Vector3(0f, 55f, 40f), 1060f),
		new Organ("lung.left", new Vector3(55f, 0f, 0f), new Vector3(0f, 110f, 70f), 350f),
		new Organ("lung.right", new Vector3(-55f, 0f, 0f), new Vector3(0f, 110f, 70f), 350f),
		new Organ("liver", new Vector3(-60f, 0f, 0f), new Vector3(0f, 60f, 65f), 1060f),
		new Organ("spleen", new Vector3(75f, 0f, 0f), new Vector3(0f, 40f, 35f), 1060f),
		new Organ("stomach", new Vector3(45f, 0f, 0f), new Vector3(0f, 45f, 40f), 1010f),
		new Organ("kidney.left", new Vector3(60f, 0f, 0f), new Vector3(0f, 35f, 22f), 1050f),
		new Organ("kidney.right", new Vector3(-60f, 0f, 0f), new Vector3(0f, 35f, 22f), 1050f)
	};

	public const string Provenance = "ASSUMED_STYLISED";

	public static float RatioAtRadius(float r)
	{
		if (r > 40f)
		{
			return (r - 40f) / r;
		}
		return 1f;
	}

	public static float SoftTissueRadiusMm(float fatMm, float muscleMm)
	{
		return 148f - fatMm - muscleMm;
	}

	public static string Describe(TissueKind kind)
	{
		switch (kind)
		{
		case TissueKind.Unknown:
			return "не определена";
		case TissueKind.Skin:
			return "кожа";
		case TissueKind.Fat:
			return "жир";
		case TissueKind.Muscle:
			return "мышца";
		case TissueKind.BoneCortical:
			return "кость, кортикальный слой";
		case TissueKind.BoneCancellous:
			return "кость, губчатое вещество";
		case TissueKind.Organ:
			return "орган";
		case TissueKind.SoftTissue:
			return "мягкая ткань";
		default:
			Debug.LogWarning(string.Format("[BodyAnatomy] неизвестный класс ткани {0}; не назван.", kind));
			return null;
		}
	}
}
