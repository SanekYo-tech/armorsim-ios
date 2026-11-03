using UnityEngine;
using UnityEngine.SceneManagement;

public static class AeroSceneEntry
{
	private const string SceneName = "AeroSim";

	public static void Open()
	{
		AeroAccessState state = AeroRoute.State(AeroFreeAccess.ReadFacts());
		if (state != AeroAccessState.Open)
		{
			if (state != AeroAccessState.NotImplemented && state != AeroAccessState.NotOwned && state != AeroAccessState.Unverified && state != AeroAccessState.GateClosed)
			{
				Debug.LogError(string.Format("[AeroRoute] Unknown access state {0}; entry refused.", (int)state));
			}
			Debug.Log("[AeroSceneEntry] AeroSim access unavailable; entry refused.");
			return;
		}
		if (!Application.CanStreamedLevelBeLoaded(SceneName))
		{
			Debug.LogError("[AeroSceneEntry] AeroSim is not registered in this build; entry refused.");
			return;
		}
		SceneManager.LoadScene(SceneName);
	}
}
