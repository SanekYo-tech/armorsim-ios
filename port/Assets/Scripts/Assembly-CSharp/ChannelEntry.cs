public readonly struct ChannelEntry
{
	public readonly string TissueId;

	public readonly float EnterDepthMm;

	public readonly float ExitDepthMm;

	public readonly float PathLengthMm;

	public ChannelEntry(string tissueId, float enterDepthMm, float exitDepthMm, float pathLengthMm)
	{
		TissueId = tissueId;
		EnterDepthMm = enterDepthMm;
		ExitDepthMm = exitDepthMm;
		PathLengthMm = pathLengthMm;
	}

	public override string ToString()
	{
		return string.Format("{0}: {1:F1}…{2:F1} мм ({3:F1})", TissueId, EnterDepthMm, ExitDepthMm, PathLengthMm);
	}
}
