using UnityEngine;

public class Flat2DBoot : MonoBehaviour
{
	public ComputeShader solverShader;

	private Flat2DDrawScreen screen;

	private void Awake()
	{
		if (Camera.main == null)
		{
			GameObject gameObject = new GameObject("Flat2DCamera", typeof(Camera));
			gameObject.tag = "MainCamera";
			Camera camera = gameObject.GetComponent<Camera>();
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = new Color(0.94f, 0.94f, 0.93f, 1f);
			camera.orthographic = true;
		}
		if (solverShader == null)
		{
			Debug.LogError("[Flat2DBoot] В сцене «Плоскость» не проставлен вычислительный шейдер. Пересобери сцену: Tools > Flat2D > Пересобрать сцену «Плоскость».");
			return;
		}
		screen = Flat2DDrawScreen.Create(solverShader, null);
		screen.transform.SetParent(base.transform, false);
		SaveEntry saveEntry = AppFlow.PendingLoad;
		AppFlow.PendingLoad = null;
		screen.SetLatticeStep(Flat2DRoute.DxFor(AppFlow.ActiveFlatQuality));
		if (saveEntry == null || string.IsNullOrEmpty(saveEntry.id))
		{
			screen.ApplyNewSceneDefaults();
		}
		if (saveEntry != null && !string.IsNullOrEmpty(saveEntry.id))
		{
			screen.LoadFromEntry(saveEntry);
		}
	}

	public static void Prepare(Flat2DDrawScreen screen, FlatQuality quality, SaveEntry pending)
	{
		screen.SetLatticeStep(Flat2DRoute.DxFor(quality));
		if (pending == null || string.IsNullOrEmpty(pending.id))
		{
			screen.ApplyNewSceneDefaults();
		}
		if (pending != null && !string.IsNullOrEmpty(pending.id))
		{
			screen.LoadFromEntry(pending);
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape) && screen != null)
		{
			screen.ToggleEscapeMenu();
		}
	}

	public void Back()
	{
		if (screen != null && screen.HasShot)
		{
			screen.StopShot();
			return;
		}
		AppFlow.OpenMenu();
	}
}
