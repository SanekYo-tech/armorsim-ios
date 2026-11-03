public static class FamilyEntryRoute
{
	public static FamilyEntry Decide(SimFamily family)
	{
		switch (family)
		{
		case SimFamily.Analytic3D:
			return FamilyEntry.OpenSandbox3D;
		case SimFamily.Body:
			return FamilyEntry.EnterBody;
		case SimFamily.Flat2D:
			return FamilyEntry.EnterFlat2D;
		case SimFamily.Aero:
			return FamilyEntry.EnterAero;
		case SimFamily.Radar:
			return FamilyEntry.EnterRadar;
		default:
			return FamilyEntry.Refuse;
		}
	}
}