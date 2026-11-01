using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Keeps the runtime-built uGUI screens fully visible on a phone.
///
/// The game was authored for a 1920x1080 canvas and uses absolute offsets
/// (e.g. menu rows at -976 units from the top), so the canvas must keep at
/// least the full 1080-unit design height. ScaleWithScreenSize's default
/// width matching gives a shorter canvas on tall phone screens and clips the
/// lower part of the UI; "Expand" matches the smaller axis instead, which
/// keeps the whole 1920x1080 design area visible on any aspect ratio.
///
/// This helper deliberately does not change reference resolutions or the
/// scale factor: shrinking the reference canvas is what made the layout
/// overflow.
/// </summary>
public sealed class MobileUiScale : MonoBehaviour
{
	private static MobileUiScale instance;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		if (instance != null)
		{
			return;
		}
		GameObject host = new GameObject("MobileUiScale");
		Object.DontDestroyOnLoad(host);
		instance = host.AddComponent<MobileUiScale>();
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
		Apply();
		StartCoroutine(ApplyForAWhile());
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		StopAllCoroutines();
		StartCoroutine(ApplyForAWhile());
	}

	private IEnumerator ApplyForAWhile()
	{
		// Screens are built in Start()/coroutines, so keep re-applying for a
		// short while as new canvases appear.
		for (int i = 0; i < 30; i++)
		{
			Apply();
			yield return new WaitForSecondsRealtime(0.5f);
		}
	}

	public static void Apply()
	{
		CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		for (int i = 0; i < scalers.Length; i++)
		{
			CanvasScaler scaler = scalers[i];
			if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
			{
				continue;
			}

			// Expand uses the smaller screen axis, so the full reference area
			// (1920x1080 and whatever each screen defines) always fits.
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
		}
	}
}