using System.Collections.Generic;

public static class UltraProjectionTests
{
	public static UltraRung R16 => default;

	public static UltraShotEdit Shot(float v = 1600f, float yaw = 0f, ContinuumNose nose = ContinuumNose.Flat, NoseShape analyticNose = NoseShape.Ogive, float lOverD = 20f)
	{
		return default;
	}

	public static UltraPlateEdit Plate(float tMM, float tilt, float offset = 0f, bool curved = false, ArmorPlate.ArmorType mat = ArmorPlate.ArmorType.RHA)
	{
		return default;
	}

	public static List<UltraPlateEdit> One(UltraPlateEdit p)
	{
		return null;
	}

	public static List<UltraPlateEdit> Flush(float t1MM, float t2MM, float gapMM = 0f)
	{
		return null;
	}

	public static bool ModelCheck()
	{
		return false;
	}

	private static bool LayoutCheck()
	{
		return false;
	}

	private static bool BuildCheck()
	{
		return false;
	}

	private static bool WindowCheck()
	{
		return false;
	}

	private static bool LineOfSightAgreesWithCore()
	{
		return false;
	}

	private static bool AngleSurvivesCheck()
	{
		return false;
	}

	private static bool FieldsDeclared()
	{
		return false;
	}

	public static bool NoseInRefusalCheck()
	{
		return false;
	}

	public static bool ContrafactualCheck()
	{
		return false;
	}

	private static bool Refuses(string what, UltraShotEdit s, IList<UltraPlateEdit> p, string expect)
	{
		return false;
	}

	private static bool Accepts(string what, UltraShotEdit s, IList<UltraPlateEdit> p)
	{
		return false;
	}
}
