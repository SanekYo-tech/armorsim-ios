using UnityEngine;

public static class UltraLifecycle
{
	private static bool installed;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void Install()
	{
	}

	private static void OnQuit()
	{
	}
}
