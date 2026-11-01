using UnityEngine;

public static class ScaleAudit
{
	public const float SpallLinerDefaultMM = 15f;

	public const float RodDefaultDiameterMM = 40f;

	public const float RodDefaultLengthM = 0.5f;

	public const float RefRodLengthMM = 600f;

	public const float RefRodDiameterMM = 25f;

	public const float RefPlateHeightM = 3f;

	public const float RefConeDiameterMM = 85f;

	public const float RefCrewHeightM = 1.3f;

	public static bool TryKindSize(ArmorPlate.ObjectKind kind, KindGeometryInput inp, out PlacedSize s)
	{
		s = default(PlacedSize);
		s.widthM = 1f;
		s.heightM = 1f;
		s.thicknessMM = 50f;
		switch (kind)
		{
		case ArmorPlate.ObjectKind.Screen:
			s.thicknessMM = 8f;
			s.overridesThickness = true;
			return true;
		case ArmorPlate.ObjectKind.EngineBlock:
			s.widthM = 1f;
			s.heightM = 0.8f;
			s.thicknessMM = 700f;
			s.overridesPlan = true;
			s.overridesThickness = true;
			return true;
		case ArmorPlate.ObjectKind.Era:
		{
			float num = Mathf.Max(inp.eraExtentMM / 1000f, 0.02f);
			s.widthM = Mathf.Max(0.25f, num);
			s.heightM = num;
			s.thicknessMM = inp.eraPlateThicknessMM * 2f + inp.eraGapMM;
			s.overridesPlan = true;
			s.overridesThickness = true;
			return true;
		}
		case ArmorPlate.ObjectKind.Nera:
		{
			float num = Mathf.Max(inp.neraExtentMM / 1000f, 0.02f);
			s.widthM = num;
			s.heightM = num;
			s.thicknessMM = inp.neraFrontMM + inp.neraRearMM + inp.neraInterlayerMM;
			s.overridesPlan = true;
			s.overridesThickness = true;
			return true;
		}
		case ArmorPlate.ObjectKind.SpallLiner:
			s.thicknessMM = SpallLinerDefaultMM;
			s.overridesThickness = true;
			return true;
		case ArmorPlate.ObjectKind.Crew:
			s.widthM = 0.6f;
			s.heightM = RefCrewHeightM;
			s.thicknessMM = 700f;
			s.overridesPlan = true;
			s.overridesThickness = true;
			return true;
		case ArmorPlate.ObjectKind.Rod:
			s.widthM = RodDefaultLengthM;
			s.heightM = RodDefaultDiameterMM / 1000f;
			s.thicknessMM = RodDefaultDiameterMM;
			s.overridesPlan = true;
			s.overridesThickness = true;
			return true;
		case ArmorPlate.ObjectKind.Plate:
		case ArmorPlate.ObjectKind.Slat:
		case ArmorPlate.ObjectKind.Model:
			return true;
		default:
			return false;
		}
	}

	public static Vector3 RenderedSizeOf(PlacedSize s)
	{
		return new Vector3(Mathf.Max(s.widthM, 0.0001f), Mathf.Max(s.heightM, 0.0001f), Mathf.Max(s.thicknessMM * 0.001f, 0.0001f));
	}

	public static Vector3 MismatchOf(PlacedSize s)
	{
		Vector3 vector = RenderedSizeOf(s);
		Vector3 physicalMeters = s.PhysicalMeters;
		return new Vector3(RelDiff(vector.x, physicalMeters.x), RelDiff(vector.y, physicalMeters.y), RelDiff(vector.z, physicalMeters.z));
	}

	public static float RelDiff(float a, float b)
	{
		return Mathf.Abs(a - b) / Mathf.Max(Mathf.Abs(b), 1e-09f);
	}

	public static Vector3 RodRenderedSize(float diameterMM, float rodLengthMM)
	{
		return new Vector3(diameterMM * 0.001f, diameterMM * 0.001f, rodLengthMM * 0.001f);
	}

	public static float RodFinnedCrossWorld(float diameterMM, float gunCalibreMM)
	{
		return RodGeometry.FinSpanM(diameterMM / 1000f, gunCalibreMM / 1000f);
	}

	public static float RodLengthOverPlateHeight()
	{
		return RefRodLengthMM / 1000f / RefPlateHeightM;
	}

	public static float RodDiameterOverCrewHeight()
	{
		return RefRodDiameterMM / 1000f / RefCrewHeightM;
	}

	public static float ConeDiameterOverPlateHeight()
	{
		return RefConeDiameterMM / 1000f / RefPlateHeightM;
	}

	public static string KindName(ArmorPlate.ObjectKind k)
	{
		return ArmorPlate.KindName(k);
	}
}
