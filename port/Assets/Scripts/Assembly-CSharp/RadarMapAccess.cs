public static class RadarMapAccess
{
	public static bool EditorEntry => false;

	public static bool SwitchVisibleNow => false;

	public static bool InUnityEditor => false;

	public static bool CoatingChoiceVisibleNow => false;

	public static bool SwitchVisible(RadarPhysicalMapState state, bool editorEntry)
	{
		if (editorEntry)
		{
			return true;
		}
		return state >= RadarPhysicalMapState.Beta;
	}

	public static bool BetaLabel(RadarPhysicalMapState state, bool editorEntry)
	{
		if (editorEntry)
		{
			return false;
		}
		return state == RadarPhysicalMapState.Beta;
	}

	public static bool CoatingChoiceVisible(RadarPhysicalMapState state, bool unityEditor)
	{
		if (unityEditor)
		{
			return true;
		}
		return state >= RadarPhysicalMapState.Beta;
	}
}
