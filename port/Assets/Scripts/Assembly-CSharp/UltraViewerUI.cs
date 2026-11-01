using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ContinuumViewer))]
public class UltraViewerUI : MonoBehaviour
{
	[Tooltip("Разрешение сетки предпросмотра: D/это. Боевое 40 считается минуты.")]
	public float elementsPerDiameter;

	[Tooltip("Окно показа, мкс. Эталону хватает 200: удар, гриб, пробой, начало отлёта.")]
	public float windowMicroseconds;

	public bool showFire;

	private ContinuumViewer viewer;

	private UltraOrbitCamera orbitCam;

	private UltraHud hud;

	private UltraShotDriver driver;

	private UltraBuiltGhost ghost;

	private TMP_Text rungLabel;

	private TMP_Text presetLabel;

	private Slider frameSlider;

	private TMP_Text readout;

	private TMP_Text fireLabel;

	private TMP_Text playLabel;

	private Button playButton;

	private bool busy;

	private void Start()
	{
		viewer = GetComponent<ContinuumViewer>();
		orbitCam = UnityEngine.Object.FindFirstObjectByType<UltraOrbitCamera>();
		hud = UnityEngine.Object.FindFirstObjectByType<UltraHud>();
		driver = UnityEngine.Object.FindFirstObjectByType<UltraShotDriver>();
		ghost = UnityEngine.Object.FindFirstObjectByType<UltraBuiltGhost>();
		MusicPlayer.EnsureIn(null, MusicPolicy.GameProgram());
		Build();
	}

	private void Update()
	{
		if (viewer == null || readout == null)
		{
			return;
		}
		if (frameSlider != null && !busy)
		{
			if (viewer.playing)
			{
				frameSlider.value = viewer.frame;
			}
			frameSlider.maxValue = Math.Max(viewer.readFrameCount - 1, 0);
		}
		string text = (driver != null) ? UltraHudText.DeclarationCaption(driver.Current) : "";
		string text2 = (ghost != null && ghost.show && driver != null) ? UltraHudText.GhostLegend(driver.Current) : "";
		string[] array = new string[12];
		array[0] = Localization.T("ultra.readout.frame", viewer.frame, Math.Max(viewer.readFrameCount - 1, 0));
		array[1] = "    ";
		array[2] = Localization.T("ultra.readout.time", viewer.readTimeUs.ToString("F1"));
		array[3] = "    ";
		array[4] = Localization.T("ultra.readout.eroded", viewer.readErodedMassG.ToString("F1"));
		array[5] = "    ";
		array[6] = Localization.T("ultra.readout.tip", viewer.readTipVelocity.ToString("F0"));
		array[7] = "\n<size=11>";
		array[8] = Localization.T("ultra.readout.disclaimer");
		array[9] = "</size>";
		array[10] = (text.Length != 0) ? ("\n<size=12><color=#FAC759>" + text + "</color></size>") : "";
		array[11] = (text2.Length != 0) ? ("\n<size=12><color=#73D6FF>" + text2 + "</color></size>") : "";
		readout.text = string.Concat(array);
		busy = driver != null && driver.Solving;
		if (rungLabel != null)
		{
			rungLabel.text = RungCaption();
		}
		if (fireLabel != null)
		{
			fireLabel.text = Localization.T(busy ? "ultra.action.cancel" : "ultra.action.solve");
		}
		if (playLabel != null)
		{
			playLabel.text = Localization.T(viewer.playing ? "ultra.transport.pause" : "ultra.transport.play");
		}
	}

	private void Build()
	{
		if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
		{
			new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
		}
		GameObject gameObject = new GameObject("ULTRA UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		Canvas canvas = gameObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		CanvasScaler canvasScaler = gameObject.GetComponent<CanvasScaler>();
		canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
		GameObject gameObject2 = new GameObject("Полоса", typeof(Image));
		gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		gameObject2.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.88f);
		RectTransform rect = gameObject2.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0f, 0f);
		rect.anchorMax = new Vector2(1f, 0f);
		rect.pivot = new Vector2(0.5f, 0f);
		rect.sizeDelta = new Vector2(0f, 176f);
		rect.anchoredPosition = Vector2.zero;
		float num = 16f;
		float num2 = 32f;
		float num3 = 56f;
		float num4 = num;
		fireLabel = null;
		if (showFire)
		{
			MakeButton(gameObject2, Localization.T("ultra.action.solve"), new Vector2(num4, num2), new Vector2(190f, num3), new Color(0.78f, 0.28f, 0.16f, 1f), OnSolveOrCancel, out fireLabel);
			num4 = 214f;
		}
		MakeReadonlyLabel(gameObject2, RungCaption(), new Vector2(num4, num2), new Vector2(240f, num3), out rungLabel);
		num4 += 248f;
		MakeButton(gameObject2, Localization.T("ultra.transport.play"), new Vector2(num4, num2), new Vector2(150f, num3), new Color(0.22f, 0.34f, 0.46f, 1f), OnPlayPause, out playLabel);
		num4 += 158f;
		MakeButton(gameObject2, Localization.T("ultra.transport.start"), new Vector2(num4, num2), new Vector2(150f, num3), new Color(0.2f, 0.24f, 0.3f, 1f), delegate
		{
			JumpTo(0);
		}, out _);
		num4 += 158f;
		MakeButton(gameObject2, Localization.T("ultra.transport.end"), new Vector2(num4, num2), new Vector2(150f, num3), new Color(0.2f, 0.24f, 0.3f, 1f), delegate
		{
			JumpTo(int.MaxValue);
		}, out _);
		num4 += 158f;
		MakeButton(gameObject2, Localization.T(orbitCam != null ? orbitCam.PresetKey : "ultra.view.2d"), new Vector2(num4, num2), new Vector2(230f, num3), new Color(0.26f, 0.3f, 0.24f, 1f), OnTogglePreset, out presetLabel);
		num4 += 238f;
		MakeButton(gameObject2, Localization.T("ultra.view.reset"), new Vector2(num4, num2), new Vector2(190f, num3), new Color(0.26f, 0.3f, 0.24f, 1f), OnResetFraming, out _);
		num4 += 198f;
		MakeButton(gameObject2, Localization.T("ultra.panel.toggle"), new Vector2(num4, num2), new Vector2(220f, num3), new Color(0.24f, 0.24f, 0.3f, 1f), OnTogglePanel, out _);
		GameObject gameObject3 = new GameObject("Кадры", typeof(Slider), typeof(Image));
		gameObject3.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		gameObject3.GetComponent<Image>().color = new Color(0.16f, 0.18f, 0.22f, 1f);
		RectTransform rect2 = gameObject3.GetComponent<RectTransform>();
		rect2.anchorMin = new Vector2(0f, 1f);
		rect2.anchorMax = new Vector2(1f, 1f);
		rect2.pivot = new Vector2(0.5f, 1f);
		rect2.offsetMin = new Vector2(16f, -102f);
		rect2.offsetMax = new Vector2(-16f, -78f);
		frameSlider = gameObject3.GetComponent<Slider>();
		frameSlider.wholeNumbers = true;
		frameSlider.minValue = 0f;
		frameSlider.maxValue = 0f;
		GameObject gameObject4 = new GameObject("Заливка", typeof(Image));
		gameObject4.transform.SetParent(gameObject3.transform, worldPositionStays: false);
		gameObject4.GetComponent<Image>().color = new Color(0.45f, 0.68f, 0.85f, 1f);
		RectTransform rect3 = gameObject4.GetComponent<RectTransform>();
		rect3.anchorMin = Vector2.zero;
		rect3.anchorMax = Vector2.one;
		rect3.offsetMin = Vector2.zero;
		rect3.offsetMax = Vector2.zero;
		frameSlider.fillRect = rect3;
		frameSlider.onValueChanged.AddListener(delegate(float v)
		{
			if (busy)
			{
				return;
			}
			viewer.playing = false;
			viewer.ShowFrame(Mathf.RoundToInt(v));
		});
		GameObject gameObject5 = new GameObject("Показания", typeof(TextMeshProUGUI));
		gameObject5.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		readout = gameObject5.GetComponent<TextMeshProUGUI>();
		readout.fontSize = 16f;
		readout.alignment = TextAlignmentOptions.Left;
		readout.color = new Color(0.85f, 0.88f, 0.92f, 1f);
		readout.richText = true;
		RectTransform rect4 = gameObject5.GetComponent<RectTransform>();
		rect4.anchorMin = new Vector2(0f, 1f);
		rect4.anchorMax = new Vector2(1f, 1f);
		rect4.pivot = new Vector2(0f, 1f);
		rect4.offsetMin = new Vector2(24f, -76f);
		rect4.offsetMax = new Vector2(-24f, -6f);
		readout.raycastTarget = false;
	}

	private Button MakeButton(GameObject parent, string caption, Vector2 pos, Vector2 size, Color color, UnityAction onClick, out TMP_Text label)
	{
		GameObject gameObject = new GameObject("Кнопка " + caption, typeof(Image), typeof(Button));
		gameObject.transform.SetParent(parent.transform, worldPositionStays: false);
		gameObject.GetComponent<Image>().color = color;
		gameObject.GetComponent<Button>().onClick.AddListener(onClick);
		RectTransform rect = gameObject.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0f, 0f);
		rect.anchorMax = new Vector2(0f, 0f);
		rect.pivot = new Vector2(0f, 0.5f);
		rect.sizeDelta = size;
		rect.anchoredPosition = pos;
		GameObject gameObject2 = new GameObject("Подпись", typeof(TextMeshProUGUI));
		gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		label = gameObject2.GetComponent<TextMeshProUGUI>();
		label.text = caption;
		label.fontSize = 20f;
		label.alignment = TextAlignmentOptions.Center;
		label.color = new Color(1f, 1f, 1f, 1f);
		label.enableAutoSizing = true;
		label.fontSizeMin = 10f;
		RectTransform rect2 = gameObject2.GetComponent<RectTransform>();
		rect2.anchorMin = Vector2.zero;
		rect2.anchorMax = Vector2.one;
		rect2.offsetMin = new Vector2(6f, 4f);
		rect2.offsetMax = new Vector2(-6f, -4f);
		return gameObject.GetComponent<Button>();
	}

	private void MakeReadonlyLabel(GameObject parent, string caption, Vector2 pos, Vector2 size, out TMP_Text label)
	{
		GameObject gameObject = new GameObject("Метка " + caption, typeof(TextMeshProUGUI));
		gameObject.transform.SetParent(parent.transform, worldPositionStays: false);
		label = gameObject.GetComponent<TextMeshProUGUI>();
		label.text = caption;
		label.fontSize = 18f;
		label.alignment = TextAlignmentOptions.Center;
		label.color = new Color(0.78f, 0.78f, 0.78f, 1f);
		label.enableAutoSizing = true;
		label.fontSizeMin = 10f;
		label.raycastTarget = false;
		RectTransform rect = gameObject.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0f, 0f);
		rect.anchorMax = new Vector2(0f, 0f);
		rect.pivot = new Vector2(0f, 0.5f);
		rect.sizeDelta = size;
		rect.anchoredPosition = pos;
	}

	private void OnSolveOrCancel()
	{
		if (driver == null)
		{
			Debug.LogError("ULTRA: драйвер выстрела не найден в сцене — расчёт запускать нечем.");
			return;
		}
		if (driver.Solving)
		{
			driver.Cancel();
		}
		else
		{
			driver.Solve();
		}
	}

	private void OnTogglePreset()
	{
		if (orbitCam == null)
		{
			return;
		}
		orbitCam.TogglePreset();
		if (presetLabel != null)
		{
			presetLabel.text = Localization.T(orbitCam.PresetKey);
		}
	}

	private void OnResetFraming()
	{
		if (orbitCam != null)
		{
			orbitCam.ResetFraming();
		}
	}

	private void OnTogglePanel()
	{
		if (hud != null)
		{
			hud.TogglePanel();
		}
	}

	private string RungCaption()
	{
		if (driver == null)
		{
			return "";
		}
		return Localization.T("ultra.rung.label", new object[1] { driver.ElementsPerDiameter.ToString("F0") });
	}

	private void OnPlayPause()
	{
		if (busy || viewer == null)
		{
			return;
		}
		viewer.playing = !viewer.playing;
	}

	private void JumpTo(int target)
	{
		if (busy || viewer == null || viewer.readFrameCount == 0)
		{
			return;
		}
		viewer.playing = false;
		if (target < 0)
		{
			target = 0;
		}
		else if (target > viewer.readFrameCount - 1)
		{
			target = viewer.readFrameCount - 1;
		}
		viewer.ShowFrame(target);
		if (frameSlider != null)
		{
			frameSlider.value = viewer.frame;
		}
	}

	private void StepFrame(int d)
	{
		if (busy || viewer == null || viewer.readFrameCount == 0)
		{
			return;
		}
		viewer.playing = false;
		int num = viewer.frame + d;
		if (num < 0)
		{
			num = 0;
		}
		else if (num > viewer.readFrameCount - 1)
		{
			num = viewer.readFrameCount - 1;
		}
		viewer.ShowFrame(num);
		if (frameSlider != null)
		{
			frameSlider.value = viewer.frame;
		}
	}
}
