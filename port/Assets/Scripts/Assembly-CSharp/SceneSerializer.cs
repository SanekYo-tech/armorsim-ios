using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class SceneSerializer : MonoBehaviour, ISimulationSnapshot
{
	[Tooltip("Имя, которое уйдёт в поле SceneFile.name при снимке. Кнопка сохранения в песочнице ставит его перед вызовом CaptureJson().")]
	public string sceneName = "";

	public string LastStatus { get; private set; } = "";

	public int ObjectCount => Object.FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None).Length;

	public static SceneSerializer FindOrCreate()
	{
		SceneSerializer sceneSerializer = Object.FindFirstObjectByType<SceneSerializer>();
		if (sceneSerializer == null)
		{
			sceneSerializer = new GameObject("SceneSerializer").AddComponent<SceneSerializer>();
		}
		return sceneSerializer;
	}

	public string CaptureJson()
	{
		SceneFile sceneFile = SceneSave.Capture(sceneName);
		LastStatus = string.Format("Снимок собран: объектов {0}, формат {1}.", sceneFile.objects.Count, sceneFile.formatVersion);
		return JsonUtility.ToJson(sceneFile, true);
	}

	public void RestoreJson(string json)
	{
		SceneLoadResult sceneLoadResult = SceneSave.Parse(json);
		if (sceneLoadResult.status > SceneLoadStatus.Migrated)
		{
			LastStatus = string.Format("ОТКАЗ ({0}): {1}", sceneLoadResult.status, sceneLoadResult.message);
			Debug.LogError("[SceneSerializer] " + LastStatus);
			return;
		}
		TargetSection targetSection = AppFlow.ActiveSection;
		TargetSection targetSection2 = TargetSection.Hull;
		if (sceneLoadResult.file != null && Enum.TryParse(sceneLoadResult.file.section, out TargetSection result))
		{
			targetSection2 = result;
		}
		if (targetSection2 != targetSection)
		{
			LastStatus = string.Format("ОТКАЗ (раздел): сборка «{0}» собрана для раздела «{1}», ", sceneLoadResult.file.name, targetSection2) + string.Format("а сессия идёт в разделе «{0}». Открой её из меню — она запустится ", targetSection) + "в своём разделе.";
			Debug.LogError("[SceneSerializer] " + LastStatus);
			return;
		}
		if (!SceneSave.ModelsAvailable(sceneLoadResult.file, out string why))
		{
			LastStatus = "ОТКАЗ (модель): " + why;
			Debug.LogError("[SceneSerializer] " + LastStatus);
			return;
		}
		ResetPreviousShot();
		int num = SceneSave.ApplyToScene(sceneLoadResult.file);
		Projectile projectile = Object.FindFirstObjectByType<Projectile>();
		if (projectile != null)
		{
			projectile.AdoptCurrentTransformAsStart();
		}
		if (!SectionFrame.IsRoof)
		{
			AimShot.OnSceneLoaded();
			SceneFrame.RequestFrame();
		}
		else
		{
			RoofShot.ApplyToScene();
		}
		if (sceneLoadResult.status == SceneLoadStatus.Migrated)
		{
			LastStatus = string.Format("Загружено «{0}»: объектов {1}. {2}", sceneLoadResult.file.name, num, sceneLoadResult.message);
		}
		else
		{
			LastStatus = string.Format("Загружено «{0}»: объектов {1}, формат {2}.", sceneLoadResult.file.name, num, sceneLoadResult.file.formatVersion);
		}
		Debug.Log("[SceneSerializer] " + LastStatus);
	}

	private void ResetPreviousShot()
	{
		PenetrationPlayer penetrationPlayer = PenetrationPlayer.FindOrCreate();
		if (penetrationPlayer != null)
		{
			penetrationPlayer.StopPlayback();
		}
		WarheadDetonation.RestoreBody();
		WarheadDetonation.ClearAll();
		EraCassette.ClearAll();
		NeraBlock.ClearAll();
		EraFlyerFragments.ClearAll();
		MicroSpall.ClearAll();
		if (FragmentSpawner.Instance != null)
		{
			FragmentSpawner.Instance.ClearAll();
		}
		Projectile projectile = Object.FindFirstObjectByType<Projectile>();
		if (projectile != null)
		{
			projectile.ClearTrace();
			projectile.ResetTransformToStart();
		}
		if (ResultDisplay.Instance != null)
		{
			ResultDisplay.Instance.Hide();
		}
	}

	private void AdoptLoadedProjectileStart()
	{
		Projectile projectile = Object.FindFirstObjectByType<Projectile>();
		if (projectile != null)
		{
			projectile.AdoptCurrentTransformAsStart();
		}
	}
}
