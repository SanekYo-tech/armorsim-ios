using System;

[Serializable]
public sealed class RadarMapModelRecord
{
	public string Key;

	public string SourcePath;

	public string FileSha256;

	public string Unit;

	public double CustomLengthM;

	public int[] Orientation;

	public string DisplayName;

	public double LongestSideM;

	public RadarMapModelRecord()
	{
		Orientation = new int[9];
	}
}
