using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ScenePanelUI : MonoBehaviour
{
	private const float PanelWidth = 260f;

	private GameObject compactPanel;

	private TMP_Text sectionButtonLabel;

	private TMP_Text offsetLabel;

	private Slider offsetSlider;

	public static void Bootstrap()
	{
		if (Object.FindFirstObjectByType<ScenePanelUI>() != null)
		{
			return;
		}
		new GameObject("ScenePanelUI").AddComponent<ScenePanelUI>();
	}

	private void Start()
	{
		BuildCompactPanel();
		Localization.OnLanguageChanged += Rebuild;
	}

	private void OnDestroy()
	{
		Localization.OnLanguageChanged -= Rebuild;
	}

	private void Rebuild()
	{
		if (compactPanel != null)
		{
			Object.Destroy(compactPanel);
		}
		BuildCompactPanel();
	}

	private void Update()
	{
		if (SectionView.Instance != null && sectionButtonLabel != null)
		{
			sectionButtonLabel.text = Localization.T(SectionView.Instance.sectionOn ? "scene.section_on" : "scene.section_off");
		}
		if (SectionView.Instance != null && offsetSlider != null)
		{
			if (!Mathf.Approximately(offsetSlider.value, SectionView.Instance.offsetM))
			{
				offsetSlider.SetValueWithoutNotify(SectionView.Instance.offsetM);
				UpdateOffsetLabel(SectionView.Instance.offsetM);
			}
		}
	}

	private void BuildCompactPanel()
	{
		Canvas canvas = EnsureCanvas();
		GameObject gameObject = MakePanel(canvas.transform, "ScenePanel", new Vector2(-330f, 20f), new Vector2(260f, 150f), new Vector2(0.5f, 0f));
		compactPanel = gameObject;
		MakeLabel(gameObject, Localization.T("scene.panel_title"), new Vector2(0f, -12f), 14f, TextAlignmentOptions.Center, 240f, 20f);
		GameObject gameObject2 = MakeButton(gameObject, Localization.T("scene.section_off"), new Vector2(0f, -36f), new Vector2(236f, 26f), new Color(0.22f, 0.3f, 0.4f, 1f), ToggleSection);
		sectionButtonLabel = gameObject2.GetComponentInChildren<TMP_Text>();
		TMP_Text tMP_Text = MakeLabel(gameObject, Localization.T("scene.section_hint"), new Vector2(0f, -66f), 9f, TextAlignmentOptions.Center, 240f, 20f);
		tMP_Text.color = new Color(0.7f, 0.73f, 0.78f, 1f);
		offsetLabel = MakeLabel(gameObject, Localization.T("scene.plane_offset", "0"), new Vector2(0f, -84f), 10f, TextAlignmentOptions.Center, 240f, 20f);
		offsetSlider = MakeSlider(gameObject, new Vector2(0f, -100f), 230f, -0.15f, 0.15f, 0f, OnOffsetChanged);
		TMP_Text tMP_Text2 = MakeLabel(gameObject, Localization.T("scene.pause_hint"), new Vector2(0f, -126f), 9f, TextAlignmentOptions.Center, 240f, 20f);
		tMP_Text2.color = new Color(0.62f, 0.66f, 0.72f, 1f);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.sizeDelta = new Vector2(component.sizeDelta.x, 152f);
	}

	private void ToggleSection()
	{
		if (SectionView.Instance == null)
		{
			Debug.LogWarning("ScenePanelUI: SectionView не найден.");
			return;
		}
		SectionView.Instance.SetSection(!SectionView.Instance.sectionOn);
	}

	private void OnOffsetChanged(float v)
	{
		if (SectionView.Instance != null)
		{
			SectionView.Instance.SetOffsetM(v);
		}
		UpdateOffsetLabel(v);
	}

	private void UpdateOffsetLabel(float v)
	{
		if (offsetLabel != null)
		{
			offsetLabel.text = Localization.T("scene.plane_offset", string.Format("{0:F0}", v * 1000f));
		}
	}

	private static Canvas EnsureCanvas()
	{
		Canvas canvas = Object.FindFirstObjectByType<Canvas>();
		if (canvas == null)
		{
			GameObject gameObject = new GameObject("Canvas_Auto");
			canvas = gameObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
			canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
			canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			canvasScaler.matchWidthOrHeight = 0.5f;
			gameObject.AddComponent<GraphicRaycaster>();
		}
		return canvas;
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

	private static void SetPanelHeight(GameObject panel, float h)
	{
		RectTransform rectTransform = panel.GetComponent<RectTransform>();
		Vector2 sizeDelta = rectTransform.sizeDelta;
		rectTransform.sizeDelta = new Vector2(sizeDelta.x, Mathf.Max(h, 40f));
	}

	private static TMP_Text MakeLabel(GameObject parent, string text, Vector2 pos, float size, TextAlignmentOptions align, float width, float height = 20f)
	{
		GameObject gameObject = new GameObject("Label", typeof(TextMeshProUGUI));
		gameObject.transform.SetParent(parent.transform, false);
		TMP_Text tMP_Text = gameObject.GetComponent<TextMeshProUGUI>();
		tMP_Text.text = text;
		tMP_Text.fontSize = size;
		tMP_Text.alignment = align;
		tMP_Text.color = Color.white;
		tMP_Text.textWrappingMode = TextWrappingModes.Normal;
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMax = new Vector2(0.5f, 1f);
		component.anchorMin = new Vector2(0.5f, 1f);
		component.pivot = new Vector2(0.5f, 1f);
		component.sizeDelta = new Vector2(width, height);
		component.anchoredPosition = pos;
		return tMP_Text;
	}

	private static GameObject MakeButton(GameObject parent, string text, Vector2 pos, Vector2 size, Color color, UnityAction onClick, TextAlignmentOptions align = TextAlignmentOptions.Center)
	{
		GameObject gameObject = new GameObject("Btn_" + text, typeof(Image), typeof(Button));
		gameObject.transform.SetParent(parent.transform, false);
		gameObject.GetComponent<Image>().color = color;
		if (onClick != null)
		{
			gameObject.GetComponent<Button>().onClick.AddListener(onClick);
		}
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMax = new Vector2(0.5f, 1f);
		component.anchorMin = new Vector2(0.5f, 1f);
		component.pivot = new Vector2(0.5f, 1f);
		component.sizeDelta = size;
		component.anchoredPosition = pos;
		GameObject gameObject2 = new GameObject("Text", typeof(TextMeshProUGUI));
		gameObject2.transform.SetParent(gameObject.transform, false);
		TMP_Text tMP_Text = gameObject2.GetComponent<TextMeshProUGUI>();
		tMP_Text.text = text;
		tMP_Text.fontSize = 12f;
		tMP_Text.alignment = align;
		tMP_Text.color = Color.white;
		tMP_Text.textWrappingMode = TextWrappingModes.NoWrap;
		tMP_Text.overflowMode = TextOverflowModes.Ellipsis;
		tMP_Text.margin = new Vector4(6f, 0f, 6f, 0f);
		RectTransform component2 = gameObject2.GetComponent<RectTransform>();
		component2.anchorMin = Vector2.zero;
		component2.anchorMax = Vector2.one;
		component2.sizeDelta = Vector2.zero;
		component2.anchoredPosition = Vector2.zero;
		return gameObject;
	}

	private static Slider MakeSlider(GameObject parent, Vector2 pos, float width, float min, float max, float value, UnityAction<float> onChanged)
	{
		GameObject gameObject = new GameObject("Slider", typeof(Slider));
		gameObject.transform.SetParent(parent.transform, false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMax = new Vector2(0.5f, 1f);
		component.anchorMin = new Vector2(0.5f, 1f);
		component.pivot = new Vector2(0.5f, 1f);
		component.sizeDelta = new Vector2(width, 16f);
		component.anchoredPosition = pos;
		GameObject gameObject2 = new GameObject("BG", typeof(Image));
		gameObject2.transform.SetParent(gameObject.transform, false);
		gameObject2.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
		Stretch(gameObject2.GetComponent<RectTransform>());
		GameObject gameObject3 = new GameObject("Fill Area", typeof(RectTransform));
		gameObject3.transform.SetParent(gameObject.transform, false);
		Stretch(gameObject3.GetComponent<RectTransform>());
		GameObject gameObject4 = new GameObject("Fill", typeof(Image));
		gameObject4.transform.SetParent(gameObject3.transform, false);
		gameObject4.GetComponent<Image>().color = new Color(0.4f, 0.6f, 1f, 1f);
		Stretch(gameObject4.GetComponent<RectTransform>());
		GameObject gameObject5 = new GameObject("Handle Slide Area", typeof(RectTransform));
		gameObject5.transform.SetParent(gameObject.transform, false);
		Stretch(gameObject5.GetComponent<RectTransform>());
		GameObject gameObject6 = new GameObject("Handle", typeof(Image));
		gameObject6.transform.SetParent(gameObject5.transform, false);
		gameObject6.GetComponent<Image>().color = Color.white;
		RectTransform component2 = gameObject6.GetComponent<RectTransform>();
		component2.sizeDelta = new Vector2(12f, 0f);
		Slider component3 = gameObject.GetComponent<Slider>();
		component3.fillRect = gameObject4.GetComponent<RectTransform>();
		component3.handleRect = component2;
		component3.targetGraphic = gameObject6.GetComponent<Image>();
		component3.direction = Slider.Direction.LeftToRight;
		component3.minValue = min;
		component3.maxValue = max;
		component3.value = value;
		if (onChanged != null)
		{
			component3.onValueChanged.AddListener(onChanged);
		}
		return component3;
	}

	private static void Stretch(RectTransform rt)
	{
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.sizeDelta = Vector2.zero;
		rt.anchoredPosition = Vector2.zero;
	}
}
