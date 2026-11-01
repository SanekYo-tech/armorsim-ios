using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UltraRungPanelUI : MonoBehaviour
{
	private const float PanelWidth = 300f;

	private GameObject panel;

	private readonly List<GameObject> buttons = new List<GameObject>();

	private readonly List<float> buttonValues = new List<float>();

	private readonly List<GameObject> windowButtons = new List<GameObject>();

	private readonly List<float> windowValues = new List<float>();

	private bool built;

	public static void Bootstrap()
	{
		if (AppFlow.ActiveMode != SimMode.Ultra)
		{
			return;
		}
		if (Object.FindFirstObjectByType<UltraRungPanelUI>() != null)
		{
			return;
		}
		new GameObject("UltraRungPanelUI").AddComponent<UltraRungPanelUI>();
	}

	private void Start()
	{
		Build();
		Localization.OnLanguageChanged += Rebuild;
	}

	private void OnDestroy()
	{
		Localization.OnLanguageChanged -= Rebuild;
	}

	private void Update()
	{
		if (built)
		{
			RefreshSelection();
		}
	}

	private void Rebuild()
	{
		if (panel != null)
		{
			Object.Destroy(panel);
		}
		buttons.Clear();
		buttonValues.Clear();
		windowButtons.Clear();
		windowValues.Clear();
		Build();
	}

	private void Build()
	{
	}

	private void Reconcile()
	{
		if (UltraQualityLadder.TrySelected(AppFlow.SelectedElementsPerDiameter, out var _))
		{
			return;
		}
		if (UltraQualityLadder.TryCoarsest(out UltraRung rung))
		{
			float selectedElementsPerDiameter = AppFlow.SelectedElementsPerDiameter;
			AppFlow.SelectedElementsPerDiameter = rung.elementsPerDiameter;
			Debug.Log(string.Format("ULTRA: ступени D/{0:F0} в лестнице нет — выбор переставлен на самую ", selectedElementsPerDiameter) + string.Format("грубую D/{0:F0}.", rung.elementsPerDiameter));
		}
		else
		{
			Debug.LogWarning("ULTRA: лестница качества ПУСТА — выбирать не из чего.");
		}
	}

	public void Select(float elementsPerDiameter)
	{
		AppFlow.SelectedElementsPerDiameter = elementsPerDiameter;
		Debug.Log(string.Format("ULTRA: выбрана ступень D/{0:F0} — считаться будет она.", elementsPerDiameter));
		RefreshSelection();
	}

	public void SelectWindow(float windowS)
	{
		windowS = Mathf.Clamp(windowS, 0f, 0.0012f);
		AppFlow.SelectedDisplayWindowS = windowS;
		if (AppFlow.SelectedDisplayWindowS > 0f)
		{
			Debug.Log(string.Format("ULTRA: окно показа {0:F0} мкс — ", AppFlow.SelectedDisplayWindowS * 1000000f) + "столько времени события будет посчитано и показано.");
		}
		else
		{
			Debug.Log("ULTRA: окно показа АВТО — длительность выведет сама постановка.");
		}
		Rebuild();
	}

	private void RefreshSelection()
	{
		for (int i = 0; i < buttons.Count; i++)
		{
			Paint(buttons[i], Mathf.Abs(buttonValues[i] - AppFlow.SelectedElementsPerDiameter) < 0.0001f);
		}
		for (int j = 0; j < windowButtons.Count; j++)
		{
			Paint(windowButtons[j], Mathf.Abs(windowValues[j] - AppFlow.SelectedDisplayWindowS) < 1E-06f);
		}
	}

	private static void Paint(GameObject go, bool on)
	{
		Image image = ((go != null) ? go.GetComponent<Image>() : null);
		if (image == null)
		{
			return;
		}
		Color color = (on ? new Color(0.25f, 0.42f, 0.58f) : new Color(0.2f, 0.21f, 0.24f));
		Color current = image.color;
		float dr = current.r - color.r;
		float dg = current.g - color.g;
		float db = current.b - color.b;
		float da = current.a - color.a;
		if (dr * dr + dg * dg + db * db + da * da < 1E-10f)
		{
			return;
		}
		image.color = color;
	}

	private static Canvas EnsureCanvas()
	{
		Canvas canvas = Object.FindFirstObjectByType<Canvas>();
		if (canvas != null)
		{
			return canvas;
		}
		GameObject gameObject = new GameObject("Canvas_Auto");
		Canvas canvas2 = gameObject.AddComponent<Canvas>();
		canvas2.renderMode = RenderMode.ScreenSpaceOverlay;
		CanvasScaler canvasScaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
		canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
		canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		canvasScaler.matchWidthOrHeight = 0.5f;
		gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
		return canvas2;
	}

	private static GameObject MakePanel(Transform parent, string name, Vector2 pos, Vector2 size, Vector2 anchor)
	{
		return null;
	}

	private static void MakeLabel(GameObject parent, string text, Vector2 pos, Vector2 size, float fontSize, Color color)
	{
	}

	private static GameObject MakeButton(GameObject parent, string text, Vector2 pos, Vector2 size, UnityAction onClick)
	{
		return null;
	}
}
