using Steamworks;
using UnityEngine;

public sealed class SteamPump : MonoBehaviour
{
	private void Update()
	{
		SteamAPI.RunCallbacks();
	}

	private void OnApplicationQuit()
	{
		SteamBoot.Down();
	}

	private void OnDestroy()
	{
		SteamBoot.Down();
	}
}
