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

	public SimFamily Family => SimFamily.Analytic3D;

	public TargetSection Section => TargetSection.Hull;
}
