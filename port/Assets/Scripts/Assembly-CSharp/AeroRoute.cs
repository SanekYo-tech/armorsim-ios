using UnityEngine;

public static class AeroRoute
{
	public const string AeroSceneName = "AeroSim";

	public static AeroAccessState State(AeroFacts facts)
	{
		if (!facts.implemented)
		{
			return AeroAccessState.NotImplemented;
		}
		switch (facts.ownership)
		{
		case OwnershipState.Unknown:
			if (!facts.freeAccess)
			{
				return AeroAccessState.Unverified;
			}
			break;
		case OwnershipState.Owned:
			break;
		case OwnershipState.NotOwned:
			if (!facts.freeAccess)
			{
				return AeroAccessState.NotOwned;
			}
			break;
		default:
			Debug.LogError(string.Format("[AeroRoute] Unknown ownership {0}; entry refused.", (int)facts.ownership));
			return AeroAccessState.Unverified;
		}
		return facts.gateOpen ? AeroAccessState.Open : AeroAccessState.GateClosed;
	}

	public static AeroEntryAction Decide(SimFamily family, bool implemented, OwnershipState ownership, bool gateOpen, bool demoAvailable, bool freeAccess = false)
	{
		return Decide(family, new AeroFacts
		{
			implemented = implemented,
			ownership = ownership,
			gateOpen = gateOpen,
			demoAvailable = demoAvailable,
			freeAccess = freeAccess
		});
	}

	public static AeroEntryAction Decide(SimFamily family, AeroFacts facts)
	{
		if (family != SimFamily.Aero)
		{
			Debug.LogError(string.Format("[AeroRoute] Family {0} is not Aero; entry refused.", (int)family));
			return AeroEntryAction.BlockedWithReason;
		}
		AeroAccessState state = State(facts);
		switch (state)
		{
		case AeroAccessState.Open:
			return AeroEntryAction.OpenAeroEditor;
		case AeroAccessState.NotOwned:
			if (facts.demoAvailable)
			{
				return AeroEntryAction.OpenAeroDemo;
			}
			return AeroEntryAction.BlockedWithReason;
		default:
			Debug.LogError(string.Format("[AeroRoute] Unknown access state {0}; entry refused.", (int)state));
			return AeroEntryAction.BlockedWithReason;
		case AeroAccessState.Unverified:
		case AeroAccessState.GateClosed:
		case AeroAccessState.NotImplemented:
			return AeroEntryAction.BlockedWithReason;
		}
	}
}
