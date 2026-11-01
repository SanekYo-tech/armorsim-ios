public static class ContinuumRouting
{
	public const string KeyNoRungs = "ultra.reject.no_rungs";

	public const string KeyFingerprintMismatch = "ultra.reject.fingerprint";

	public const string KeyRoutingBroken = "ultra.reject.routing";

	public const string KeySyntheticFixture = "ultra.reject.synthetic";

	public const string SyntheticStamp = "SYNTHETIC";

	public static SolverBackendKind BackendFor(SimMode mode)
	{
		switch (mode)
		{
		case SimMode.Ultra:
			return SolverBackendKind.Continuum;
		case SimMode.Super:
			return SolverBackendKind.Super;
		default:
			return SolverBackendKind.Analytic;
		}
	}

	public static SolverBackendKind OutcomeAuthority(SimMode mode)
	{
		return SolverBackendKind.Analytic;
	}

	public static bool IsUltraUnlocked(string liveFingerprint, out string reasonKey)
	{
		reasonKey = "ultra.ok";
		if (!UltraQualityLadder.HasRungs)
		{
			reasonKey = "ultra.reject.no_rungs";
			return false;
		}
		if (liveFingerprint != null && liveFingerprint != "SYNTHETIC" && liveFingerprint != "R/r0=1.279217/1128/489/S19-single-floor")
		{
			reasonKey = "ultra.reject.fingerprint";
			return false;
		}
		return true;
	}

	public static bool ReceivesFinEnvelope(SimMode mode)
	{
		return mode != SimMode.Ultra;
	}
}
