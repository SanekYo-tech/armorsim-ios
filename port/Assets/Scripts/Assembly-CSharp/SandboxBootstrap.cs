using UnityEngine;
using UnityEngine.SceneManagement;

public static class SandboxBootstrap
{
	public static bool IsSandboxScene()
	{
		return Object.FindFirstObjectByType<Projectile>() != null || Object.FindFirstObjectByType<ArmorBuilder>() != null;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Install()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
		Run();
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Run();
	}

	public static void Run()
	{
		if (!IsSandboxScene())
		{
			return;
		}
		SceneSerializer.FindOrCreate();
		if (Object.FindFirstObjectByType<SandboxSessionController>() == null)
		{
			GameObject gameObject = new GameObject("SandboxSession");
			gameObject.AddComponent<SandboxSessionController>();
		}
		SceneVisuals.Bootstrap();
		CinemaCamera.Bootstrap();
		ScaleGrid.Bootstrap();
		SectionView.Bootstrap();
		RenderedSpeedAudit.Bootstrap();
		ScenePanelUI.Bootstrap();
		if (SandboxPauseMenu.Instance == null && Object.FindFirstObjectByType<SandboxPauseMenu>() == null)
		{
			GameObject gameObject2 = new GameObject("SandboxPauseMenu");
			gameObject2.AddComponent<SandboxPauseMenu>();
		}
		PlateGizmo.Bootstrap();
		if (SectionFrame.IsRoof && Object.FindFirstObjectByType<RoofShotPanelUI>() == null)
		{
			GameObject gameObject3 = new GameObject("RoofShotPanelUI");
			gameObject3.AddComponent<RoofShotPanelUI>();
		}
		ModelPanelUI.Bootstrap();
		HullAimPanelUI.Bootstrap();
		UltraRungPanelUI.Bootstrap();
		MusicPlayer.EnsureIn(null, MusicPolicy.GameProgram());
		SectionLayout.Apply();
		AimShot.OnSceneBoot();
		SceneFrame.Bootstrap();
	}
}
