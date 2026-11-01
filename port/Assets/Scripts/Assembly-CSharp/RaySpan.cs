public readonly struct RaySpan
{
	public readonly float EnterMm;

	public readonly float ExitMm;

	public readonly IMaterialRef Material;

	public readonly float NormalThicknessMm;

	public readonly float ObliquityDeg;

	public readonly EntrySurface Entry;

	public RaySpan(float enterMm, float exitMm, IMaterialRef material, float normalThicknessMm, float obliquityDeg, EntrySurface entry)
	{
		EnterMm = 0f;
		ExitMm = 0f;
		Material = null;
		NormalThicknessMm = 0f;
		ObliquityDeg = 0f;
		Entry = EntrySurface.Unknown;
	}
}
