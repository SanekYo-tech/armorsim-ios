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
		EnterMm = enterMm;
		ExitMm = exitMm;
		Material = material;
		NormalThicknessMm = normalThicknessMm;
		ObliquityDeg = obliquityDeg;
		Entry = entry;
	}
}
