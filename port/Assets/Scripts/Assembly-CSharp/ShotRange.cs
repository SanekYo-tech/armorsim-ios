using System;
using UnityEngine;

public static class ShotRange
{
	public static bool SabotagePinBase;

	public static float MarginM => SceneDefaults.ApproachM;

	public static float FitWorld(Projectile pr)
	{
		if (SceneBounds.TryScene(pr, out Bounds bounds))
		{
			Vector3 size = bounds.size;
			return (float)Math.Sqrt(size.x * size.x + size.y * size.y + size.z * size.z) + MarginM;
		}
		return 0f;
	}

	public static float Derive(Projectile pr, float baseWorld)
	{
		if (SabotagePinBase)
		{
			return baseWorld;
		}
		return Mathf.Max(baseWorld, FitWorld(pr));
	}
}
