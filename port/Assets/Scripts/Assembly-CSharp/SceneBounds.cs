using UnityEngine;

public static class SceneBounds
{
	public static bool TryArmour(out Bounds bounds)
	{
		bounds = default;
		return false;
	}

	public static bool TryScene(Projectile pr, out Bounds bounds)
	{
		bounds = default;
		return false;
	}
}
