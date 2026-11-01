using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RoofShotPanelUI : MonoBehaviour
{
	private const float PanelWidth = 260f;

	private GameObject panel;

	private GameObject btnAngled;

	private GameObject btnAtgm;

	private TMP_Text angleLabel;

	private TMP_Text hintLabel;

	private Slider angleSlider;

	private TMP_InputField angleField;

	public static void Bootstrap()
	{
		if (!SectionFrame.IsRoof)
		{
			return;
		}
		if (UnityEngine.Object.FindFirstObjectByType<RoofShotPanelUI>() != null)
		{
			return;
		}
		new GameObject("RoofShotPanelUI").AddComponent<RoofShotPanelUI>();
	}

	private void Start()
	{
		Build();
		Localization.OnLanguageChanged += Rebuild;
		RoofShot.OnChanged += Refresh;
	}

	private void OnDestroy()
	{
		Localization.OnLanguageChanged -= Rebuild;
		RoofShot.OnChanged -= Refresh;
	}

	private void Rebuild()
	{
		if (panel != null)
		{
			UnityEngine.Object.Destroy(panel);
		}
		Build();
	}

	private void Build()
	{
		Canvas canvas = EnsureCanvas();
		if (canvas == null)
		{
			return;
		}
		panel = MakePanel(canvas.transform, "RoofShotPanel", new Vector2(-330f, 180f), new Vector2(PanelWidth, 176f), new Vector2(0.5f, 0f));
		MakeLabel(panel, Localization.T("roof.panel_title"), new Vector2(0f, -12f), 14f, TextAlignmentOptions.Center, 240f, 20f);
		btnAngled = MakeButton(panel, Localization.T("roof.spawn_angled"), new Vector2(0f, -36f), new Vector2(236f, 26f), delegate
		{
			RoofShot.SetKind(RoofSpawnKind.Angled);
		});
		btnAtgm = MakeButton(panel, Localization.T("roof.spawn_atgm"), new Vector2(0f, -66f), new Vector2(236f, 26f), delegate
		{
			RoofShot.SetKind(RoofSpawnKind.Atgm);
		});
		angleLabel = MakeLabel(panel, "", new Vector2(0f, -98f), 11f, TextAlignmentOptions.Center, 240f, 20f);
		angleField = NumberField.Attach(angleLabel, () => RoofShot.AngleDeg, delegate(float v)
		{
			RoofShot.SetAngle(v);
		}, "F0");
		angleSlider = MakeSlider(panel, new Vector2(0f, -116f), 230f, 30f, 180f, RoofShot.AngleDeg, delegate(float v)
		{
			RoofShot.SetAngle(v);
		});
		hintLabel = MakeLabel(panel, "", new Vector2(0f, -142f), 9f, TextAlignmentOptions.Center, 240f, 44f);
		hintLabel.color = new Color(0.7f, 0.73f, 0.78f, 1f);
		Refresh();
	}

	private void Refresh()
	{
		RoofSpawnKind kind = RoofShot.Kind;
		Tint(btnAngled, kind == RoofSpawnKind.Angled);
		Tint(btnAtgm, kind == RoofSpawnKind.Atgm);
		if (kind == RoofSpawnKind.Atgm && angleField != null)
		{
			NumberField.Close(angleField);
		}
		if (angleSlider != null)
		{
			angleSlider.interactable = kind == RoofSpawnKind.Angled;
			if (!Mathf.Approximately(angleSlider.value, RoofShot.AngleDeg))
			{
				angleSlider.value = RoofShot.AngleDeg;
			}
		}
		if (angleLabel != null)
		{
			angleLabel.text = Localization.T("roof.angle", new object[1] { Mathf.RoundToInt(RoofShot.AngleDeg) });
		}
		if (hintLabel != null)
		{
			Projectile projectile = UnityEngine.Object.FindFirstObjectByType<Projectile>();
			hintLabel.text = ((kind == RoofSpawnKind.Angled) ? Localization.T("roof.angle_hint") : Localization.T("roof.atgm_hint", new object[1] { Mathf.RoundToInt(SectionFrame.AtgmClearanceM(projectile) * 1000f) }));
		}
	}

	private static void Tint(GameObject btn, bool on)
	{
		if (btn == null)
		{
			return;
		}
		Image image = btn.GetComponent<Image>();
		if (image != null)
		{
			image.color = (on ? new Color(0.25f, 0.42f, 0.58f, 1f) : new Color(0.20f, 0.21f, 0.24f, 1f));
		}
	}

	private static Canvas EnsureCanvas()
	{
		Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
		if (canvas != null)
		{
			return canvas;
		}
		GameObject gameObject = new GameObject("Canvas_Auto");
		Canvas canvas2 = gameObject.AddComponent<Canvas>();
		canvas2.renderMode = RenderMode.ScreenSpaceOverlay;
		gameObject.AddComponent<CanvasScaler>();
		gameObject.AddComponent<GraphicRaycaster>();
		return canvas2;
	}

	private static GameObject MakePanel(Transform parent, string name, Vector2 pos, Vector2 size, Vector2 anchor)
	{
		GameObject panel = new GameObject(name, typeof(Image));
		panel.transform.SetParent(parent, false);
		panel.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.12f, 0.88f);
		RectTransform rectTransform = panel.GetComponent<RectTransform>();
		rectTransform.anchorMax = anchor;
		rectTransform.anchorMin = anchor;
		rectTransform.pivot = anchor;
		rectTransform.sizeDelta = size;
		rectTransform.anchoredPosition = pos;
		return panel;
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

	private static GameObject MakeButton(GameObject parent, string text, Vector2 pos, Vector2 size, UnityAction onClick)
	{
		GameObject gameObject = new GameObject("Btn_" + text, typeof(Image), typeof(Button));
		gameObject.transform.SetParent(parent.transform, false);
		gameObject.GetComponent<Image>().color = new Color(0.20f, 0.21f, 0.24f, 1f);
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
		tMP_Text.alignment = TextAlignmentOptions.Center;
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
		gameObject6.GetComponent<RectTransform>().sizeDelta = new Vector2(12f, 0f);
		Slider component2 = gameObject.GetComponent<Slider>();
		component2.fillRect = gameObject4.GetComponent<RectTransform>();
		component2.handleRect = gameObject6.GetComponent<RectTransform>();
		component2.targetGraphic = gameObject6.GetComponent<Image>();
		component2.direction = Slider.Direction.LeftToRight;
		component2.minValue = min;
		component2.maxValue = max;
		component2.value = value;
		if (onChanged != null)
		{
			component2.onValueChanged.AddListener(onChanged);
		}
		return component2;
	}

	private static void Stretch(RectTransform rt)
	{
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.sizeDelta = Vector2.zero;
		rt.anchoredPosition = Vector2.zero;
	}
}
