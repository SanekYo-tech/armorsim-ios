public static class UltraSceneRoute
{
	public const string UltraSceneName = "UltraViewer";

	public const string SuperSceneName = "SuperViewer";

	public static string SceneNameFor(SimMode mode, UltraStage stage, string sandboxSceneName)
	{
		switch (mode)
		{
		case SimMode.Ultra:
			return stage == UltraStage.View ? UltraSceneName : sandboxSceneName;
		case SimMode.Super:
			return stage == UltraStage.View ? SuperSceneName : sandboxSceneName;
		default:
			return sandboxSceneName;
		}
	}

	public static bool HasViewerStage(SimMode mode)
	{
		return mode == SimMode.Ultra || mode == SimMode.Super;
	}

	public static UltraEntryAction Decide(SimMode mode, bool ultraUnlocked)
	{
		if (mode == SimMode.Ultra || mode == SimMode.Super)
		{
			return ultraUnlocked ? UltraEntryAction.EnterUltra : UltraEntryAction.BlockedWithReason;
		}
		return UltraEntryAction.OpenSandbox;
	}
}
