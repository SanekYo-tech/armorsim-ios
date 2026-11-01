public static class RadarFreeAccess
{
	public const bool Permanent = true;

	public static RadarFacts ReadFacts()
	{
		RadarFacts facts = default(RadarFacts);
		facts.implemented = true;
		facts.ownership = Entitlements.State(ContentPack.Radar);
		facts.gateOpen = true;
		facts.gateReason = null;
		facts.demoAvailable = false;
		facts.freeAccess = true;
		return facts;
	}
}
