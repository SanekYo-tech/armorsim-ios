using UnityEngine;
using UnityEngine.SceneManagement;

public static class RadarSceneEntry
{
	public const string SceneName = "RadarSim";

	public static void Open()
	{
		RadarFacts facts = default(RadarFacts);
		facts.implemented = true;
		facts.ownership = Entitlements.State(ContentPack.Radar);
		facts.gateOpen = true;
		Open(SceneName, facts);
	}

	internal static bool Open(string sceneName, RadarFacts facts)
	{
		RadarAccessState radarAccessState = RadarRoute.State(facts);
		if (radarAccessState != RadarAccessState.Open)
		{
			if (radarAccessState != RadarAccessState.NotImplemented && radarAccessState != RadarAccessState.NotOwned && radarAccessState != RadarAccessState.Unverified && radarAccessState != RadarAccessState.GateClosed)
			{
				Debug.LogError(string.Format("[RadarRoute] Unknown access state {0}; entry refused.", radarAccessState));
			}
			Debug.Log("[RadarSceneEntry] RadarSim access unavailable; entry refused.");
			return false;
		}
		if (!Application.CanStreamedLevelBeLoaded(sceneName))
		{
			Debug.LogError("[RadarSceneEntry] RadarSim is not registered in this build; entry refused.");
			return false;
		}
		SceneManager.LoadScene(sceneName);
		return true;
	}
}
