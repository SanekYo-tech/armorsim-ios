using Steamworks;
using UnityEngine;

public static class SteamBoot
{
	private static bool _tried;

	private static bool _ok;

	public static string FailureReason { get; private set; }

	public static bool Initialized
	{
		get
		{
			Ensure();
			return _ok;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		Ensure();
	}

	private static void Ensure()
	{
	}

	internal static void Down()
	{
		if (_ok)
		{
			_ok = false;
			SteamAPI.Shutdown();
			Debug.Log("[Steam] API опущен.");
		}
	}
}
