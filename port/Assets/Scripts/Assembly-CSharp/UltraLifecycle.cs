using UnityEngine;

public static class UltraLifecycle
{
	private static bool installed;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void Install()
	{
		if (installed)
		{
			return;
		}
		installed = true;
		Application.quitting += OnQuit;
	}

	private static void OnQuit()
	{
		UltraSolveQueue.Instance.Shutdown();
	}
}
