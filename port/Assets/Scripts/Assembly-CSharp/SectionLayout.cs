using System.Collections.Generic;
using UnityEngine;

public static class SectionLayout
{
	public static void Apply()
	{
		Debug.Log("[SectionLayout] " + SectionFrame.Describe(AppFlow.ActiveSection));
		if (!SectionFrame.IsRoof)
		{
			return;
		}
		RestackAsRoof();
		RoofShot.ApplyToScene();
		FrameForRoof();
		MoveGridBelowRoof();
		Debug.Log("[SectionLayout] " + RoofShot.Describe(Object.FindFirstObjectByType<Projectile>()));
	}

	private static void RestackAsRoof()
	{
		List<ArmorPlate> list = new List<ArmorPlate>(Object.FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None));
		list.Sort((ArmorPlate a, ArmorPlate b) => a.transform.position.z.CompareTo(b.transform.position.z));
		ArmorBuilder armorBuilder = Object.FindFirstObjectByType<ArmorBuilder>();
		float num = ((armorBuilder != null) ? armorBuilder.gapM : SceneDefaults.PlateGapM);
		float num2 = SectionFrame.RoofLineYM;
		for (int i = 0; i < list.Count; i++)
		{
			ArmorPlate armorPlate = list[i];
			if (armorPlate == null)
			{
				continue;
			}
			float num3 = Mathf.Max(SceneScale.MinRenderWorld, armorPlate.thicknessMM * SceneScale.WorldPerMM);
			armorPlate.transform.position = new Vector3(0f, num2 - num3 * 0.5f, 0f);
			armorPlate.transform.rotation = SectionFrame.PlateBase(TargetSection.Roof) * Quaternion.Euler(armorPlate.angleX, armorPlate.angleY, 0f);
			num2 -= num3 + num;
		}
		Debug.Log(string.Format("[SectionLayout] Пакет уложен крышей: преград {0}, ", list.Count) + string.Format("верхняя грань y = {0:F2} м, зазор {1:F0} мм, рост вниз.", SectionFrame.RoofLineYM, num * 1000f));
	}

	private static void FrameForRoof()
	{
		if (OrbitCamera.Instance == null)
		{
			return;
		}
		Vector3 vector = SectionFrame.RoofAimPoint;
		if (OrbitCamera.Instance.target == null)
		{
			GameObject gameObject = new GameObject("CameraTarget_Roof");
			OrbitCamera.Instance.target = gameObject.transform;
		}
		OrbitCamera.Instance.target.position = vector;
		OrbitCamera.Instance.FocusOn(vector);
		OrbitCamera.Instance.SetOrbit(OrbitCamera.Instance.YawDeg, 40f);
	}

	private static void MoveGridBelowRoof()
	{
		if (ScaleGrid.Instance == null)
		{
			return;
		}
		ScaleGrid.Instance.groundY = SectionFrame.RoofLineYM - SceneDefaults.AssemblyDepthM;
	}
}
