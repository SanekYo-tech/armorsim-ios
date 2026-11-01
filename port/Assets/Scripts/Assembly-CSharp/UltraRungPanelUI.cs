using System.Collections.Generic;
using TMPro;
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
		Canvas canvas = EnsureCanvas();
		if (canvas == null)
		{
			return;
		}
		UltraRung[] rungs = UltraQualityLadder.Available();
		if (!UltraQualityLadder.Benchmarked)
		{
			UltraQualityLadder.Benchmark();
		}
		Reconcile();
		panel = MakePanel(canvas.transform, "UltraRungPanel", new Vector2(330f, 20f), new Vector2(300f, 200f), new Vector2(0.5f, 0f));
		float num = -12f;
		for (int i = 0; i < rungs.Length; i++)
		{
			float elementsPerDiameter = rungs[i].elementsPerDiameter;
			object[] args = new object[2]
			{
				elementsPerDiameter.ToString("F0"),
				null
			};
			float num2 = rungs[i].refWallSeconds * UltraQualityLadder.MachineFactor / 60f;
			float num3 = ((AppFlow.SelectedDisplayWindowS > 0f && UltraQualityLadder.ReferenceWindowS > 0f) ? (AppFlow.SelectedDisplayWindowS / UltraQualityLadder.ReferenceWindowS) : 1f);
			args[1] = (num2 * num3).ToString("F1");
			GameObject gameObject = MakeButton(panel, Localization.T("ultra.rung.picker", args), new Vector2(0f, num), new Vector2(276f, 26f), delegate
			{
				Select(elementsPerDiameter);
			});
			buttons.Add(gameObject);
			buttonValues.Add(elementsPerDiameter);
			num -= 30f;
		}
		num -= 8f;
		MakeLabel(panel, Localization.T("ultra.window.title"), new Vector2(0f, num), new Vector2(276f, 16f), 12f, new Color(0.78f, 0.82f, 0.88f, 1f));
		num -= 20f;
		float[] offeredWindowsS = UltraProjection.OfferedWindowsS;
		float num4 = (276f - 4f * (offeredWindowsS.Length - 1)) / (float)offeredWindowsS.Length;
		float num5 = num4 * 0.5f - 138f;
		for (int j = 0; j < offeredWindowsS.Length; j++)
		{
			float windowS = offeredWindowsS[j];
			string text = ((windowS > 0f) ? (windowS * 1000000f).ToString("F0") : Localization.T("ultra.window.auto"));
			GameObject gameObject2 = MakeButton(panel, text, new Vector2(num5 + (num4 + 4f) * j, num), new Vector2(num4, 24f), delegate
			{
				SelectWindow(windowS);
			});
			windowButtons.Add(gameObject2);
			windowValues.Add(windowS);
		}
		num -= 28f;
		if (AppFlow.SelectedDisplayWindowS > 0f)
		{
			MakeLabel(panel, Localization.T("ultra.window.scaled"), new Vector2(0f, num), new Vector2(276f, 14f), 10f, new Color(0.62f, 0.66f, 0.72f, 1f));
			num -= 18f;
		}
		panel.GetComponent<RectTransform>().sizeDelta = new Vector2(PanelWidth, Mathf.Max(40f, 12f - num));
		built = true;
		RefreshSelection();
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
		GameObject gameObject = new GameObject(name, typeof(Image));
		gameObject.transform.SetParent(parent, false);
		gameObject.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.88f);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMax = anchor;
		component.anchorMin = anchor;
		component.pivot = anchor;
		component.sizeDelta = size;
		component.anchoredPosition = pos;
		return gameObject;
	}

	private static void MakeLabel(GameObject parent, string text, Vector2 pos, Vector2 size, float fontSize, Color color)
	{
		GameObject gameObject = new GameObject("Lbl_" + text, typeof(TextMeshProUGUI));
		gameObject.transform.SetParent(parent.transform, false);
		TextMeshProUGUI component = gameObject.GetComponent<TextMeshProUGUI>();
		component.text = text;
		component.fontSize = fontSize;
		component.alignment = TextAlignmentOptions.Center;
		component.color = color;
		component.textWrappingMode = TextWrappingModes.NoWrap;
		component.overflowMode = TextOverflowModes.Ellipsis;
		RectTransform component2 = gameObject.GetComponent<RectTransform>();
		component2.anchorMax = new Vector2(0.5f, 1f);
		component2.anchorMin = new Vector2(0.5f, 1f);
		component2.pivot = new Vector2(0.5f, 1f);
		component2.sizeDelta = size;
		component2.anchoredPosition = pos;
	}

	private static GameObject MakeButton(GameObject parent, string text, Vector2 pos, Vector2 size, UnityAction onClick)
	{
		GameObject gameObject = new GameObject("Btn_" + text, typeof(Image), typeof(Button));
		gameObject.transform.SetParent(parent.transform, false);
		gameObject.GetComponent<Image>().color = new Color(0.2f, 0.21f, 0.24f, 1f);
		Button component = gameObject.GetComponent<Button>();
		if (onClick != null)
		{
			component.onClick.AddListener(onClick);
		}
		RectTransform component2 = gameObject.GetComponent<RectTransform>();
		component2.anchorMax = new Vector2(0.5f, 1f);
		component2.anchorMin = new Vector2(0.5f, 1f);
		component2.pivot = new Vector2(0.5f, 1f);
		component2.sizeDelta = size;
		component2.anchoredPosition = pos;
		GameObject gameObject2 = new GameObject("Text", typeof(TextMeshProUGUI));
		gameObject2.transform.SetParent(gameObject.transform, false);
		TextMeshProUGUI component3 = gameObject2.GetComponent<TextMeshProUGUI>();
		component3.text = text;
		component3.fontSize = 12f;
		component3.alignment = TextAlignmentOptions.Center;
		component3.color = Color.white;
		component3.textWrappingMode = TextWrappingModes.NoWrap;
		component3.overflowMode = TextOverflowModes.Ellipsis;
		component3.margin = new Vector4(6f, 0f, 6f, 0f);
		RectTransform component4 = gameObject2.GetComponent<RectTransform>();
		component4.anchorMin = Vector2.zero;
		component4.anchorMax = Vector2.one;
		component4.sizeDelta = Vector2.zero;
		component4.anchoredPosition = Vector2.zero;
		return gameObject;
	}
}
