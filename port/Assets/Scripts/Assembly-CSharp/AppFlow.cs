using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AppFlow
{
	public static SaveEntry PendingLoad = null;

	public static ProjectedScenario PendingUltraBuild;

	public static float SelectedElementsPerDiameter = 16f;

	public static float SelectedDisplayWindowS = 0f;

	public static string SandboxSceneName = "";

	public static SimMode ActiveMode = SimMode.Normal;

	public static SimFamily ActiveFamily = SimFamily.Analytic3D;

	public static FlatQuality ActiveFlatQuality = FlatQuality.Coarse;

	public static FlatStage ActiveStage = FlatStage.Draw;

	public static TargetSection ActiveSection = TargetSection.Hull;

	public static string MenuSceneName = "MainMenu";

	public static bool SceneInBuild(string sceneName)
	{
		if (string.IsNullOrEmpty(sceneName))
		{
			return false;
		}
		for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
		{
			if (string.Equals(Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)), sceneName, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	public static void OpenSandbox(string sceneName, SimMode mode, SaveEntry entry)
	{
		OpenSandbox(sceneName, mode, TargetSection.Hull, entry);
	}

	public static void OpenSandbox(string sceneName, SimMode mode, TargetSection section, SaveEntry entry)
	{
		if (!GameSettings.IsUnlocked(mode))
		{
			mode = SimMode.Normal;
		}
		string reasonKey = null;
		bool ultraUnlocked = ContinuumRouting.IsUltraUnlocked(null, out reasonKey);
		if (UltraSceneRoute.Decide(mode, ultraUnlocked) == UltraEntryAction.BlockedWithReason)
		{
			Debug.LogWarning("[AppFlow] ULTRA не открыт: " + UltraHudText.ModeEntry(reasonKey));
			return;
		}
		string scene = UltraSceneRoute.SceneNameFor(mode, UltraStage.Edit, SandboxSceneName);
		if (SceneInBuild(scene))
		{
			ActiveMode = mode;
			if (!GameSettings.SupportsSection(ActiveMode))
			{
				section = TargetSection.Hull;
			}
			ActiveSection = section;
			PendingLoad = entry;
			SandboxSceneName = scene;
			SceneManager.LoadScene(scene);
		}
		else
		{
			Debug.LogError("[AppFlow] Сцена \"" + scene + "\" не найдена в Build Settings. File > Build Profiles > Scene List — добавь сцену и проверь поле sandboxSceneName в MainMenuUI. Для ULTRA нужна сцена \"UltraViewer\" (собирается через меню ULTRA > пересобрать сцену просмотрщика).");
		}
	}

	public static void OpenUltraView()
	{
		string scene = UltraSceneRoute.SceneNameFor(SimMode.Ultra, UltraStage.View, null);
		if (SceneInBuild(scene))
		{
			ActiveMode = SimMode.Ultra;
			SceneManager.LoadScene(scene);
		}
		else
		{
			Debug.LogError("[AppFlow] Сцена просмотрщика \"" + scene + "\" не в Build Settings. Собери её через меню ULTRA и добавь в список сцен.");
		}
	}

	public static void OpenFlat2D(SaveEntry entry, FlatQuality quality)
	{
		string scene = Flat2DRoute.SceneNameFor(SimFamily.Flat2D, FlatStage.Draw, null);
		if (scene != null && SceneInBuild(scene))
		{
			ActiveFamily = SimFamily.Flat2D;
			ActiveFlatQuality = quality;
			ActiveStage = FlatStage.Draw;
			PendingLoad = entry;
			SceneManager.LoadScene(scene);
		}
		else
		{
			Debug.LogError("[AppFlow] Сцена «Плоскость» \"" + scene + "\" не в Build Settings. Собери её через меню Tools > Flat2D и добавь в список сцен.");
		}
	}

	public static void OpenMenu()
	{
		if (SceneInBuild(MenuSceneName))
		{
			PendingLoad = null;
			ActiveFamily = SimFamily.Analytic3D;
			SceneManager.LoadScene(MenuSceneName);
		}
		else
		{
			Debug.LogError("[AppFlow] Сцена меню \"" + MenuSceneName + "\" не в Build Settings.");
		}
	}

	public static void Quit()
	{
		Application.Quit();
	}
}
