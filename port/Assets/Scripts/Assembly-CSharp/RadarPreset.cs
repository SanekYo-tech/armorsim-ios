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
		Id = id;
		NameKey = "radar.preset." + id;
		BandLetter = bandLetter;
		DefaultFrequencyHz = defaultFrequencyHz;
		MinimumHz = minimumHz;
		MaximumHz = maximumHz;
		RoleKey = "radar.role." + id;
		ExamplesKey = "radar.example." + id;
		Provenance = "ASSUMED";
		Source = "RADAR-2 Appendix A; public band/example context: Docs/RADAR_primer_2026-09-23.md";
	}
}
