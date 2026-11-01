public static class ErosionRegimeTest
{
	public static ErosionRegime Expected(ContinuumMaterialId id)
	{
		return ErosionRegime.Asb;
	}

	public static ErosionRegime Actual(in ContinuumProperties p, float erodeEpsP)
	{
		return ErosionRegime.Asb;
	}

	public static bool Check()
	{
		return false;
	}
}
