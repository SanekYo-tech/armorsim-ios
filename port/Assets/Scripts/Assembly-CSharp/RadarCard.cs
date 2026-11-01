using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class RadarCard
{
	public static RadarCardShape Describe(RadarFacts facts)
	{
		RadarCardShape result = default(RadarCardShape);
		switch (RadarRoute.State(facts))
		{
		case RadarAccessState.Open:
			result.enterable = true;
			return result;
		case RadarAccessState.GateClosed:
			result.reasonKey = facts.gateReason ?? "radar.gate.closed";
			return result;
		case RadarAccessState.Unverified:
			result.badgeKey = "dlc.badge";
			result.reasonKey = "dlc.unverified";
			result.storeButton = true;
			return result;
		case RadarAccessState.NotOwned:
			result.badgeKey = "dlc.badge";
			result.reasonKey = "dlc.not_owned_hint";
			result.demoButton = facts.demoAvailable;
			result.storeButton = true;
			return result;
		case RadarAccessState.NotImplemented:
			result.badgeKey = "mode.soon";
			return result;
		default:
			Debug.LogError("[RadarCard] Unknown access state; card refused.");
			result.badgeKey = "dlc.badge";
			result.reasonKey = "dlc.unverified";
			return result;
		}
	}

	public static Button Build(Transform parent, RadarFacts facts, UnityAction select, UnityAction store, UnityAction demo)
	{
		return null;
	}

	private static void AddButton(RectTransform card, string name, string key, float right, UnityAction action)
	{
	}
}
