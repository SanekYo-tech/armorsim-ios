using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Makes the runtime-built uGUI screens comfortable on a phone.
///
/// Most screens are created in code with a 1920x1080 reference resolution,
/// which is tuned for a desktop monitor and looks tiny on a phone. This
/// helper lowers the reference resolution to 1024x576 and switches scalers to
/// Expand mode, so the whole layout gets ~1.5-1.9x bigger while still fitting
/// any screen aspect ratio (no vertical or horizontal clipping).
/// </summary>
public sealed class MobileUiScale : MonoBehaviour
{
	private const float ReferenceWidth = 1024f;

	private const float ReferenceHeight = 576f;

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
		Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		for (int i = 0; i < canvases.Length; i++)
		{
			Canvas canvas = canvases[i];
			if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
			{
				continue;
			}

			CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
			if (scaler == null)
			{
				scaler = canvas.gameObject.AddComponent<CanvasScaler>();
			}
			ApplyScaler(scaler);
		}

		CanvasScaler[] allScalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		for (int i = 0; i < allScalers.Length; i++)
		{
			ApplyScaler(allScalers[i]);
		}
	}

	private static void ApplyScaler(CanvasScaler scaler)
	{
		if (scaler == null)
		{
			return;
		}

		if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
		{
			Vector2 reference = scaler.referenceResolution;
			bool needsResize = reference.x <= 0f || reference.y <= 0f || reference.x > ReferenceWidth || reference.y > ReferenceHeight;
			if (needsResize)
			{
				scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
			}

			// Expand fits the whole reference area into the screen, so nothing
			// gets cut off on tall phone screens or on 4:3 tablets.
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
			return;
		}

		if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
		{
			float target = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
			scaler.scaleFactor = Mathf.Max(scaler.scaleFactor, Mathf.Clamp(target, 1f, 4f));
		}
	}
}