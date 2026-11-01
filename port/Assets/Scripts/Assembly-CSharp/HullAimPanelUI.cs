using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HullAimPanelUI : MonoBehaviour
{
	private const float PanelWidth = 260f;

	private const float PanelHeight = 330f;

	private const float InsidePollS = 0.5f;

	private GameObject panel;

	private GameObject btnLine;

	private GameObject btnFree;

	private GameObject btnPick;

	private GameObject btnCentre;

	private GameObject btnCamera;

	private TMP_Text azLabel;

	private TMP_Text elLabel;

	private TMP_Text pointLabel;

	private TMP_Text warnLabel;

	private TMP_Text hintLabel;

	private TMP_Text pickText;

	private Slider azSlider;

	private Slider elSlider;

	private TMP_InputField azField;

	private TMP_InputField elField;

	private float nextInsidePoll;

	public static void Bootstrap()
	{
		AimPick.Bootstrap();
		if (!AimShot.Available)
		{
			return;
		}
		if (UnityEngine.Object.FindFirstObjectByType<HullAimPanelUI>() != null)
		{
			return;
		}
		new GameObject("HullAimPanelUI").AddComponent<HullAimPanelUI>();
	}

	private void Start()
	{
		Build();
		Localization.OnLanguageChanged += Rebuild;
		AimShot.OnChanged += Refresh;
		AimPick.OnArmedChanged += Refresh;
	}

	private void OnDestroy()
	{
		Localization.OnLanguageChanged -= Rebuild;
		AimShot.OnChanged -= Refresh;
		AimPick.OnArmedChanged -= Refresh;
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
		panel = MakePanel(canvas.transform, "HullAimPanel", new Vector2(-330f, 180f), new Vector2(PanelWidth, PanelHeight), new Vector2(0.5f, 0f));
		MakeLabel(panel, Localization.T("aim.panel_title"), new Vector2(0f, -12f), 14f, TextAlignmentOptions.Center, 240f, 20f);
		btnLine = MakeButton(panel, Localization.T("aim.mode_line"), new Vector2(-60.5f, -36f), new Vector2(115f, 26f), delegate
		{
			AimShot.SetMode(AimMode.Line);
		});
		btnFree = MakeButton(panel, Localization.T("aim.mode_free"), new Vector2(60.5f, -36f), new Vector2(115f, 26f), delegate
		{
			AimShot.SetMode(AimMode.Free);
		});
		azLabel = MakeLabel(panel, "", new Vector2(0f, -70f), 11f, TextAlignmentOptions.Center, 240f, 20f);
		azField = NumberField.Attach(azLabel, () => (AimShot.Mode == AimMode.Free) ? AimShot.AzimuthDeg : 0f, delegate(float v)
		{
			AimShot.SetAzimuth(v);
		}, "F1");
		azSlider = MakeSlider(panel, new Vector2(0f, -88f), 230f, -180f, 180f, AimShot.AzimuthDeg, delegate(float v)
		{
			AimShot.SetAzimuth(v);
		});
		elLabel = MakeLabel(panel, "", new Vector2(0f, -114f), 11f, TextAlignmentOptions.Center, 240f, 20f);
		elField = NumberField.Attach(elLabel, () => (AimShot.Mode == AimMode.Free) ? AimShot.ElevationDeg : 0f, delegate(float v)
		{
			AimShot.SetElevation(v);
		}, "F1");
		elSlider = MakeSlider(panel, new Vector2(0f, -132f), 230f, -15f, 90f, AimShot.ElevationDeg, delegate(float v)
		{
			AimShot.SetElevation(v);
		});
		pointLabel = MakeLabel(panel, "", new Vector2(0f, -158f), 10f, TextAlignmentOptions.Center, 240f, 20f);
		btnPick = MakeButton(panel, Localization.T("aim.pick"), new Vector2(0f, -180f), new Vector2(236f, 26f), new UnityAction(AimPick.Toggle));
		pickText = btnPick.GetComponentInChildren<TMP_Text>();
		btnCentre = MakeButton(panel, Localization.T("aim.centre"), new Vector2(-60.5f, -210f), new Vector2(115f, 26f), delegate
		{
			AimShot.AimAtSelectionCentre();
		});
		btnCamera = MakeButton(panel, Localization.T("aim.from_camera"), new Vector2(60.5f, -210f), new Vector2(115f, 26f), new UnityAction(AimFromCamera));
		warnLabel = MakeLabel(panel, "", new Vector2(0f, -242f), 10f, TextAlignmentOptions.Center, 240f, 30f);
		warnLabel.color = new Color(1f, 0.45f, 0.4f, 1f);
		hintLabel = MakeLabel(panel, "", new Vector2(0f, -272f), 9f, TextAlignmentOptions.Center, 240f, 48f);
		hintLabel.color = new Color(0.7f, 0.73f, 0.78f, 1f);
		Refresh();
	}

	private static void AimFromCamera()
	{
		Camera camera = Camera.main;
		if (camera != null)
		{
			AimShot.SetDirectionFromView(camera.transform.forward);
		}
	}

	private void Refresh()
	{
		if (panel == null)
		{
			return;
		}
		AimMode mode = AimShot.Mode;
		Tint(btnLine, mode != AimMode.Free);
		Tint(btnFree, mode == AimMode.Free);
		Tint(btnPick, AimPick.IsArmed);
		float num = 0f;
		float num2 = 0f;
		if (mode == AimMode.Free)
		{
			num = AimShot.AzimuthDeg;
			num2 = AimShot.ElevationDeg;
		}
		else
		{
			if (azField != null)
			{
				NumberField.Close(azField);
			}
			if (elField != null)
			{
				NumberField.Close(elField);
			}
		}
		SetSlider(azSlider, num, mode == AimMode.Free);
		SetSlider(elSlider, num2, mode == AimMode.Free);
		if (azLabel != null)
		{
			azLabel.text = Localization.T("aim.azimuth", new object[1] { num.ToString("F1", Localization.Culture) });
		}
		if (elLabel != null)
		{
			elLabel.text = Localization.T("aim.elevation", new object[1] { num2.ToString("F1", Localization.Culture) });
		}
		if (pointLabel != null)
		{
			pointLabel.text = ((mode != AimMode.Free) ? "" : Localization.T("aim.point", new object[3]
			{
				AimShot.AimPointM.x.ToString("F2", Localization.Culture),
				AimShot.AimPointM.y.ToString("F2", Localization.Culture),
				AimShot.AimPointM.z.ToString("F2", Localization.Culture)
			}));
		}
		if (pickText != null)
		{
			pickText.text = Localization.T(AimPick.IsArmed ? "aim.pick_armed" : "aim.pick");
		}
		PlateSelector plateSelector = UnityEngine.Object.FindFirstObjectByType<PlateSelector>();
		SetInteractable(btnCentre, plateSelector != null && plateSelector.SelectedPlates.Count > 0);
		if (hintLabel != null)
		{
			hintLabel.text = ((mode == AimMode.Free) ? Localization.T("aim.hint_free", new object[1] { SceneDefaults.ApproachM.ToString("F1", Localization.Culture) }) : Localization.T("aim.hint_line"));
		}
		RefreshWarning();
	}

	private void RefreshWarning()
	{
		if (warnLabel == null)
		{
			return;
		}
		string text = "";
		if (AimShot.IsFree)
		{
			string text2 = AimShot.StartInsideObjectName(UnityEngine.Object.FindFirstObjectByType<Projectile>());
			if (text2 != null)
			{
				text = Localization.T("aim.warn_inside", new object[1] { text2 });
			}
		}
		warnLabel.text = text;
	}

	private void Update()
	{
		if (nextInsidePoll > Time.unscaledTime)
		{
			return;
		}
		nextInsidePoll = Time.unscaledTime + InsidePollS;
		PlateSelector plateSelector = UnityEngine.Object.FindFirstObjectByType<PlateSelector>();
		SetInteractable(btnCentre, plateSelector != null && plateSelector.SelectedPlates.Count > 0);
		RefreshWarning();
	}

	private static void SetSlider(Slider s, float v, bool interactable)
	{
		if (s == null)
		{
			return;
		}
		s.interactable = interactable;
		if (!Mathf.Approximately(s.value, v))
		{
			s.value = v;
		}
	}

	private static void SetInteractable(GameObject btn, bool on)
	{
		if (btn == null)
		{
			return;
		}
		Button button = btn.GetComponent<Button>();
		if (button != null && button.interactable != on)
		{
			button.interactable = on;
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
