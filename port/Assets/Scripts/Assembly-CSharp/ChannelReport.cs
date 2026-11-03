using System.Collections.Generic;

public sealed class ChannelReport
{
	public IReadOnlyList<ChannelEntry> Crossed { get; }

	public float TotalDepthMm { get; }

	public bool ExitedFar { get; }

	public ChannelReport(IReadOnlyList<ChannelEntry> crossed, float totalDepthMm, bool exitedFar)
	{
		Crossed = crossed;
		TotalDepthMm = totalDepthMm;
		ExitedFar = exitedFar;
	}
}
