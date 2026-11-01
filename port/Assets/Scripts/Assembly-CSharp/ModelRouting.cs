using System.Collections.Generic;
using UnityEngine;

public static class ModelRouting
{
	public const string KeyAnalytic = "model.route.analytic";

	public const string KeyUltra = "model.route.ultra";

	public static bool SceneHasModel(Projectile proj)
	{
		if (proj == null)
		{
			return false;
		}
		Vector3 direction = proj.direction;
		Vector3 vector = ((direction.sqrMagnitude > 1E-09f) ? direction.normalized : Vector3.forward);
		List<SandboxModel> list = new List<SandboxModel>(Object.FindObjectsByType<SandboxModel>(FindObjectsSortMode.InstanceID));
		foreach (SandboxModel item in list)
		{
			if (item != null && Vector3.Dot(item.transform.position - proj.transform.position, vector) > 0f)
			{
				return true;
			}
		}
		return false;
	}

	public static string Refuses(SimMode mode)
	{
		if (mode == SimMode.Ultra)
		{
			return "model.route.ultra";
		}
		if (mode == SimMode.Super)
		{
			return null;
		}
		return "model.route.analytic";
	}
}
