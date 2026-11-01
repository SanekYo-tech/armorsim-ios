public static class UltraProjectionFields
{
	public static readonly string[] PlateProjected = new string[4]
	{
		"thicknessMM", "material", "tiltDeg", "alongAxisM"
	};

	public static readonly string[] PlateCosmetic = new string[3]
	{
		"lateralOffsetM", "curved", "leanSign"
	};

	public static readonly string[] ShotProjected = new string[6]
	{
		"core", "calibreMM", "lengthOverDiameter", "nose", "noseHalfAngleDeg", "velocityMps"
	};

	public static readonly string[] ShotCosmetic = new string[4]
	{
		"yawDeg", "kind", "analyticNose", "ballisticCap"
	};

	public static bool PlateFeedsProjection(string field)
	{
		return Has(PlateProjected, field);
	}

	public static bool ShotFeedsProjection(string field)
	{
		return Has(ShotProjected, field);
	}

	public static bool PlateDeclared(string f)
	{
		return Has(PlateProjected, f) || Has(PlateCosmetic, f);
	}

	public static bool ShotDeclared(string f)
	{
		return Has(ShotProjected, f) || Has(ShotCosmetic, f);
	}

	private static bool Has(string[] a, string s)
	{
		for (int i = 0; i < a.Length; i++)
		{
			if (a[i] == s)
			{
				return true;
			}
		}
		return false;
	}
}
