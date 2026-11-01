using UnityEngine;

public static class SteamBoot
{
	private static bool _tried;

	private static bool _ok;

	public static string FailureReason { get; private set; }

	public static bool Initialized => false;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
	}

	private static void Ensure()
	{
	}

	internal static void Down()
	{
	}
}
