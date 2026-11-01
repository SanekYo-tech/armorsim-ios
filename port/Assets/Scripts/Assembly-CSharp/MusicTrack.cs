public struct MusicTrack
{
	public string Name;

	public float Percent;

	public bool IsCommon => Percent < 0f;

	public static MusicTrack Common(string name)
	{
		MusicTrack result = default(MusicTrack);
		result.Name = name;
		result.Percent = -1f;
		return result;
	}

	public static MusicTrack Rare(string name, float percent)
	{
		MusicTrack result = default(MusicTrack);
		result.Name = name;
		result.Percent = percent;
		return result;
	}
}
