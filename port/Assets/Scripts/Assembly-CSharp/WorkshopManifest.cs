using System;

[Serializable]
public class WorkshopManifest
{
	public int formatVersion;

	public string title;

	public string description;

	public string family;

	public string section;

	public int objectCount;

	public int payloadVersion;

	public string appVersion;

	public string publishedUtc;

	public string authorName;

	public string sourceSaveId;

	public SimFamily Family
	{
		get
		{
			if (Enum.TryParse<SimFamily>(family, out var result))
			{
				return result;
			}
			return SimFamily.Analytic3D;
		}
	}

	public TargetSection Section
	{
		get
		{
			if (Enum.TryParse<TargetSection>(section, out var result))
			{
				return result;
			}
			return TargetSection.Hull;
		}
	}
}
