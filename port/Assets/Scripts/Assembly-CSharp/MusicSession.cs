using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MusicSession
{
	public static bool WasInGame { get; private set; }

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Hook()
	{
		WasInGame = false;
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		NoteSceneLoaded(scene.name);
	}

	public static void NoteSceneLoaded(string sceneName)
	{
		if (!IsMenuScene(sceneName))
		{
			WasInGame = true;
		}
	}

	public static void ResetSession()
	{
		WasInGame = false;
	}

	private static bool IsMenuScene(string sceneName)
	{
		return string.Equals(sceneName, AppFlow.MenuSceneName, StringComparison.OrdinalIgnoreCase);
	}
}
