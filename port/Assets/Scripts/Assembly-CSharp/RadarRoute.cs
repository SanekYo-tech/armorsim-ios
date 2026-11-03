using UnityEngine;

public static class RadarRoute
{
	public static RadarAccessState State(RadarFacts facts)
	{
		if (!facts.implemented)
		{
			return RadarAccessState.NotImplemented;
		}
		switch (facts.ownership)
		{
		case OwnershipState.Unknown:
			if (!facts.freeAccess)
			{
				return RadarAccessState.Unverified;
			}
			break;
		case OwnershipState.Owned:
			break;
		case OwnershipState.NotOwned:
			if (!facts.freeAccess)
			{
				return RadarAccessState.NotOwned;
			}
			break;
		default:
			Debug.LogError(string.Format("[RadarRoute] Unknown ownership {0}; entry refused.", (int)facts.ownership));
			return RadarAccessState.Unverified;
		}
		return facts.gateOpen ? RadarAccessState.Open : RadarAccessState.GateClosed;
	}

	public static RadarEntryAction Decide(SimFamily family, RadarFacts facts)
	{
		if (family != SimFamily.Radar)
		{
			Debug.LogError(string.Format("[RadarRoute] Family {0} is not Radar; entry refused.", (int)family));
			return RadarEntryAction.BlockedWithReason;
		}
		RadarAccessState state = State(facts);
		switch (state)
		{
		case RadarAccessState.Open:
			return RadarEntryAction.OpenRadarBench;
		case RadarAccessState.NotOwned:
			if (facts.demoAvailable)
			{
				return RadarEntryAction.OpenRadarDemo;
			}
			return RadarEntryAction.BlockedWithReason;
		case RadarAccessState.Unverified:
		case RadarAccessState.GateClosed:
		case RadarAccessState.NotImplemented:
			return RadarEntryAction.BlockedWithReason;
		default:
			Debug.LogError(string.Format("[RadarRoute] Unknown access state {0}; entry refused.", (int)state));
			return RadarEntryAction.BlockedWithReason;
		}
	}
}