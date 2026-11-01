using UnityEngine;

public static class Flat2DGate
{
	public const string KeyOk = null;

	public const string KeyNoScene = "flat.reject.no_scene";

	public const string KeyNoCompute = "flat.reject.no_compute";

	public static bool IsAvailable(out string reasonKey)
	{
		return IsAvailable(SystemInfo.supportsComputeShaders, AppFlow.SceneInBuild("Flat2D"), out reasonKey);
	}

	internal static bool IsAvailable(bool computeSupported, bool sceneInBuild, out string reasonKey)
	{
		if (!computeSupported)
		{
			reasonKey = KeyNoCompute;
			return false;
		}
		if (!sceneInBuild)
		{
			reasonKey = KeyNoScene;
			return false;
		}
		reasonKey = KeyOk;
		return true;
	}
}
