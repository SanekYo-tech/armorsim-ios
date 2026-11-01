public static class RadarRoute
{
	public static RadarAccessState State(RadarFacts facts)
	{
		return RadarAccessState.NotImplemented;
	}

	public static RadarEntryAction Decide(SimFamily family, RadarFacts facts)
	{
		return RadarEntryAction.BlockedWithReason;
	}
}
