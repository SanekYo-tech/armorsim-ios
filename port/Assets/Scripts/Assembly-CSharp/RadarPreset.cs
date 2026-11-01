public readonly struct RadarPreset
{
	public readonly string Id;

	public readonly string NameKey;

	public readonly string BandLetter;

	public readonly double DefaultFrequencyHz;

	public readonly double MinimumHz;

	public readonly double MaximumHz;

	public readonly string RoleKey;

	public readonly string ExamplesKey;

	public readonly string Provenance;

	public readonly string Source;

	public RadarPreset(string id, string bandLetter, double defaultFrequencyHz, double minimumHz, double maximumHz)
	{
		Id = null;
		NameKey = null;
		BandLetter = null;
		DefaultFrequencyHz = 0.0;
		MinimumHz = 0.0;
		MaximumHz = 0.0;
		RoleKey = null;
		ExamplesKey = null;
		Provenance = null;
		Source = null;
	}
}
