using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class SandboxPauseMenu : MonoBehaviour
{
	private enum Screen
	{
		Root = 0,
		Save = 1,
		Load = 2
	}

	public static SandboxPauseMenu Instance;

	private Canvas canvas;

	private GameObject root;

	private GameObject savePanel;

	private GameObject loadPanel;

	private RectTransform loadContent;

	private TMP_InputField nameField;

	private TMP_Text statusLabel;

	private TMP_Text modeLabel;

	private Button confirmNewBtn;

	private Button confirmExitBtn;

	private bool open;

	private bool showPresets;

	private bool armedNew;

	private bool armedExit;

	private Button tabUser;

	private Button tabPresets;

	private Button touchMenuBtn;

	public static SandboxPauseMenu FindOrCreate()
	{
		if (Instance != null)
		{
			return Instance;
		}
		SandboxPauseMenu sandboxPauseMenu = Object.FindFirstObjectByType<SandboxPauseMenu>();
		if (sandboxPauseMenu == null)
		{
			sandboxPauseMenu = new GameObject("SandboxPauseMenu").AddComponent<SandboxPauseMenu>();
		}
		return sandboxPauseMenu;
	}

	private void Awake()
	{
		Instance = this;
		EnsureEventSystem();
		Build();
		open = false;
		if (root != null)
		{
			root.SetActive(false);
		}
		Time.timeScale = 1f;
	}

	private void OnDisable()
	{
		Time.timeScale = 1f;
	}

	private void Update()
	{
		bool touchMenuVisible = TouchInputHelper.IsTouchDevice && !open;
		if (touchMenuBtn != null && touchMenuBtn.gameObject.activeSelf != touchMenuVisible)
		{
			touchMenuBtn.gameObject.SetActive(touchMenuVisible);
		}
		if (Input.GetKeyDown(KeyCode.Escape) && !PlateGizmo.CancelOnEscape())
		{
			if (!open)
			{
				SetOpen(value: true);
			}
			else if (savePanel.activeSelf || loadPanel.activeSelf)
			{
				savePanel.SetActive(false);
				loadPanel.SetActive(false);
				armedNew = false;
				armedExit = false;
				RefreshConfirmLabels();
			}
			else
			{
				SetOpen(value: false);
			}
		}
	}

	private void SetOpen(bool value)
	{
		open = value;
		if (root != null)
		{
			root.SetActive(value);
		}
		if (value)
		{
			Time.timeScale = 0f;
			armedNew = false;
			armedExit = false;
			savePanel.SetActive(false);
			loadPanel.SetActive(false);
			RefreshConfirmLabels();
			RefreshMode();
			Status("");
		}
		else
		{
			Time.timeScale = 1f;
		}
	}

	private void ShowScreen(Screen s)
	{
		savePanel.SetActive(s == Screen.Save);
		loadPanel.SetActive(s == Screen.Load);
		armedNew = false;
		armedExit = false;
		RefreshConfirmLabels();
		if (s == Screen.Load)
		{
			RefreshLoadList();
			return;
		}
		if (s == Screen.Save && nameField != null)
		{
			nameField.Select();
		}
	}

	private static void EnsureEventSystem()
	{
		if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
		{
			return;
		}
		new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
	}

	private void Build()
	{
		GameObject gameObject = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		gameObject.transform.SetParent(base.transform, false);
		canvas = gameObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 500;
		CanvasScaler component = gameObject.GetComponent<CanvasScaler>();
		component.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		component.referenceResolution = new Vector2(1920f, 1080f);
		component.matchWidthOrHeight = 0.5f;
		RectTransform rectTransform = MenuTheme.MakeRect(gameObject.transform, "PauseRoot");
		root = rectTransform.gameObject;
		MenuTheme.Stretch((RectTransform)root.transform);
		MenuTheme.AddImage((RectTransform)root.transform, new Color(0.06f, 0.06f, 0.07f, 0.88f)).raycastTarget = true;
		BuildRoot(root.transform);
		BuildSavePanel(root.transform);
		BuildLoadPanel(root.transform);
		BuildTouchMenuButton(gameObject.transform);
	}

	private void BuildTouchMenuButton(Transform parent)
	{
		touchMenuBtn = MenuTheme.MakeButtonRaw(parent, "BtnTouchMenu", "МЕНЮ", 56f, OnTouchMenuPressed, 26f, TextAlignmentOptions.Center, 10f);
		MenuTheme.Place((RectTransform)touchMenuBtn.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -26f), new Vector2(210f, 56f));
		touchMenuBtn.gameObject.SetActive(false);
	}

	private void OnTouchMenuPressed()
	{
		SetOpen(!open);
	}

	private void BuildRoot(Transform parent)
	{
		TextMeshProUGUI textMeshProUGUI = MenuTheme.MakeLabel(parent, "Title", "pause.title", 34f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Center);
		MenuTheme.Place(textMeshProUGUI.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 46f));
		textMeshProUGUI.characterSpacing = 8f;
		modeLabel = MenuTheme.MakeLabelRaw(parent, "Mode", "", 17f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Center);
		MenuTheme.Place(modeLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 26f));
		RectTransform rectTransform = MenuTheme.MakeColumn(parent, "PauseNav", 12f);
		MenuTheme.Place(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(460f, 340f));
		MenuTheme.MakeButton(rectTransform, "BtnResume", "pause.resume", 56f, delegate
		{
			SetOpen(value: false);
		});
		MenuTheme.MakeButton(rectTransform, "BtnSave", "pause.save", 56f, delegate
		{
			savePanel.SetActive(true);
			loadPanel.SetActive(false);
			armedNew = false;
			armedExit = false;
			RefreshConfirmLabels();
			nameField.Select();
		});
		MenuTheme.MakeButton(rectTransform, "BtnLoad", "pause.load", 56f, delegate
		{
			savePanel.SetActive(false);
			loadPanel.SetActive(true);
			armedNew = false;
			armedExit = false;
			RefreshConfirmLabels();
			RefreshLoadList();
		});
		confirmNewBtn = MenuTheme.MakeButton(rectTransform, "BtnNew", "pause.new_sim", 56f, OnNewSimulation);
		confirmExitBtn = MenuTheme.MakeButton(rectTransform, "BtnExit", "sandbox.to_menu", 56f, OnExitToMenu);
		statusLabel = MenuTheme.MakeLabelRaw(parent, "Status", "", 15f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Center);
		MenuTheme.Place(statusLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1200f, 60f));
		statusLabel.textWrappingMode = TextWrappingModes.Normal;
	}

	private void BuildSavePanel(Transform parent)
	{
		RectTransform rectTransform = MenuTheme.MakeRect(parent, "SavePanel");
		MenuTheme.Stretch(rectTransform, new Vector2(300f, 200f), new Vector2(-300f, -200f));
		MenuTheme.AddImage(rectTransform, MenuTheme.Surface).raycastTarget = true;
		TextMeshProUGUI textMeshProUGUI = MenuTheme.MakeLabel(rectTransform, "Title", "pause.save", 26f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		MenuTheme.Place(textMeshProUGUI.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -28f), new Vector2(700f, 36f));
		textMeshProUGUI.characterSpacing = 5f;
		nameField = MakeInput(rectTransform, new Vector2(0f, -110f), new Vector2(600f, 44f), "scene.name_placeholder");
		Button button = MenuTheme.MakeButton(rectTransform, "BtnDoSave", "scene.save", 48f, OnSave, 20f, TextAlignmentOptions.Center, 12f);
		MenuTheme.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -172f), new Vector2(300f, 48f));
		Button button2 = MenuTheme.MakeButton(rectTransform, "BtnBack", "menu.back", 44f, delegate
		{
			savePanel.SetActive(false);
			loadPanel.SetActive(false);
			armedNew = false;
			armedExit = false;
			RefreshConfirmLabels();
		}, 18f, TextAlignmentOptions.Center, 12f);
		MenuTheme.Place((RectTransform)button2.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -26f), new Vector2(180f, 44f));
		rectTransform.gameObject.SetActive(false);
		savePanel = rectTransform.gameObject;
	}

	private void BuildLoadPanel(Transform parent)
	{
		RectTransform rectTransform = MenuTheme.MakeRect(parent, "LoadPanel");
		MenuTheme.Stretch(rectTransform, new Vector2(220f, 90f), new Vector2(-220f, -90f));
		MenuTheme.AddImage(rectTransform, MenuTheme.Surface).raycastTarget = true;
		TextMeshProUGUI textMeshProUGUI = MenuTheme.MakeLabel(rectTransform, "Title", "pause.load", 26f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		MenuTheme.Place(textMeshProUGUI.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -26f), new Vector2(700f, 36f));
		textMeshProUGUI.characterSpacing = 5f;
		Button button = MenuTheme.MakeButton(rectTransform, "BtnBack", "menu.back", 44f, delegate
		{
			savePanel.SetActive(false);
			loadPanel.SetActive(false);
			armedNew = false;
			armedExit = false;
			RefreshConfirmLabels();
		}, 18f, TextAlignmentOptions.Center, 12f);
		MenuTheme.Place((RectTransform)button.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -24f), new Vector2(180f, 44f));
		RectTransform rectTransform2 = MenuTheme.MakeRow(rectTransform, "Tabs", 10f, TextAnchor.MiddleLeft);
		MenuTheme.Place(rectTransform2, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -78f), new Vector2(640f, 44f));
		tabUser = MenuTheme.MakeButton(rectTransform2, "TabUser", "saves.tab_user", 44f, delegate
		{
			showPresets = false;
			RefreshLoadList();
		}, 17f, TextAlignmentOptions.Center, 10f);
		tabUser.gameObject.AddComponent<LayoutElement>().preferredWidth = 300f;
		tabPresets = MenuTheme.MakeButton(rectTransform2, "TabPresets", "saves.tab_presets", 44f, delegate
		{
			showPresets = true;
			RefreshLoadList();
		}, 17f, TextAlignmentOptions.Center, 10f);
		tabPresets.gameObject.AddComponent<LayoutElement>().preferredWidth = 300f;
		ScrollRect scrollRect = MenuTheme.MakeScrollList(rectTransform, "LoadScroll", out loadContent);
		MenuTheme.Stretch((RectTransform)scrollRect.transform, new Vector2(40f, 30f), new Vector2(-40f, -136f));
		rectTransform.gameObject.SetActive(false);
		loadPanel = rectTransform.gameObject;
	}

	private void RefreshLoadList()
	{
		if (loadContent == null)
		{
			return;
		}
		for (int num = loadContent.childCount - 1; num >= 0; num--)
		{
			Object.Destroy(loadContent.GetChild(num).gameObject);
		}
		MenuTheme.SetSelectedVisual(tabUser, !showPresets);
		MenuTheme.SetSelectedVisual(tabPresets, showPresets);
		List<SaveEntry> list = (showPresets ? SaveSystem.LoadPresets() : SaveSystem.LoadUserSaves());
		List<SaveEntry> list2 = new List<SaveEntry>();
		foreach (SaveEntry item in list)
		{
			if (item.Section == SectionFrame.Active)
			{
				list2.Add(item);
			}
		}
		if (list2.Count == 0)
		{
			TextMeshProUGUI textMeshProUGUI = MenuTheme.MakeLabel(loadContent, "Empty", showPresets ? "saves.empty_presets" : "saves.empty_user", 17f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
			LayoutElement layoutElement = textMeshProUGUI.gameObject.AddComponent<LayoutElement>();
			layoutElement.minHeight = 56f;
			layoutElement.preferredHeight = 56f;
			return;
		}
		foreach (SaveEntry item2 in list2)
		{
			MakeLoadRow(item2);
		}
	}

	private void MakeLoadRow(SaveEntry entry)
	{
		RectTransform rectTransform = MenuTheme.MakeRect(loadContent, "Row_" + entry.displayName);
		MenuTheme.AddImage(rectTransform, MenuTheme.SurfaceAlt);
		LayoutElement layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
		layoutElement.minHeight = 70f;
		layoutElement.preferredHeight = 70f;
		TextMeshProUGUI textMeshProUGUI = MenuTheme.MakeLabelRaw(rectTransform, "Name", entry.displayName, 19f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		MenuTheme.Place(textMeshProUGUI.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -12f), new Vector2(620f, 24f));
		textMeshProUGUI.overflowMode = TextOverflowModes.Ellipsis;
		string text = ((entry.CreatedLocal() == DateTime.MinValue) ? "" : (entry.CreatedLocal().ToString("dd.MM.yyyy  HH:mm") + "   ·   "));
		object[] array = new object[1];
		array[0] = entry.objectCount;
		string text2 = text + Localization.T("saves.objects", array);
		TextMeshProUGUI textMeshProUGUI2 = MenuTheme.MakeLabelRaw(rectTransform, "Meta", text2, 14f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		MenuTheme.Place(textMeshProUGUI2.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -40f), new Vector2(620f, 20f));
		Button button = MenuTheme.MakeButton(rectTransform, "BtnLoad", "saves.load", 38f, delegate
		{
			OnLoad(entry);
		}, 16f, TextAlignmentOptions.Center, 10f);
		MenuTheme.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(entry.isPreset ? (-18f) : (-160f), 0f), new Vector2(140f, 38f));
		if (!entry.isPreset)
		{
			Button button2 = MenuTheme.MakeButton(rectTransform, "BtnDelete", "saves.delete", 38f, delegate
			{
				SaveSystem.Delete(entry.id);
				RefreshLoadList();
			}, 16f, TextAlignmentOptions.Center, 10f);
			MenuTheme.Place((RectTransform)button2.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(130f, 38f));
		}
	}

	private static SandboxSessionController Session()
	{
		SandboxSessionController sandboxSessionController = Object.FindFirstObjectByType<SandboxSessionController>();
		if (sandboxSessionController == null)
		{
			sandboxSessionController = new GameObject("SandboxSession").AddComponent<SandboxSessionController>();
		}
		return sandboxSessionController;
	}

	private void OnSave()
	{
		string text = ((nameField != null) ? nameField.text : "");
		if (string.IsNullOrEmpty(text))
		{
			Status(Localization.T("scene.need_name"));
			return;
		}
		SceneSerializer.FindOrCreate().sceneName = text;
		SaveEntry saveEntry = Session().SaveCurrent(text);
		if (saveEntry == null)
		{
			Status(Localization.T("scene.save_to_menu_failed"));
			return;
		}
		object[] array = new object[2];
		array[0] = saveEntry.displayName;
		array[1] = saveEntry.objectCount;
		Status(Localization.T("pause.saved", array));
		savePanel.SetActive(false);
		loadPanel.SetActive(false);
		armedNew = false;
		armedExit = false;
		RefreshConfirmLabels();
	}

	private void OnLoad(SaveEntry entry)
	{
		string message = null;
		SandboxSessionController sandboxSessionController = Session();
		bool flag = sandboxSessionController.LoadEntry(entry, out message);
		if (statusLabel != null)
		{
			statusLabel.text = message ?? "";
		}
		if (flag)
		{
			savePanel.SetActive(false);
			loadPanel.SetActive(false);
			armedNew = false;
			armedExit = false;
			RefreshConfirmLabels();
		}
	}

	private void OnNewSimulation()
	{
		if (!armedNew)
		{
			armedNew = true;
			RefreshConfirmLabels();
			return;
		}
		Time.timeScale = 1f;
		string text = SceneManager.GetActiveScene().name;
		if (string.Equals(text, "UltraViewer", StringComparison.OrdinalIgnoreCase))
		{
			string text2 = AppFlow.SandboxSceneName;
			if (!AppFlow.SceneInBuild(text2))
			{
				Debug.LogWarning("[Пауза] «Новая симуляция» из просмотрщика ULTRA: сцена сборщика " + (string.IsNullOrEmpty(text2) ? "не запомнена" : ("«" + text2 + "» не в Build Settings")) + " — перезагружаю текущую. Собирать в ней нечем.");
			}
			else
			{
				text = text2;
			}
		}
		AppFlow.OpenSandbox(text, AppFlow.ActiveMode, AppFlow.ActiveSection, null);
	}

	private void OnExitToMenu()
	{
		if (!armedExit)
		{
			armedNew = false;
			armedExit = true;
			RefreshConfirmLabels();
			return;
		}
		Time.timeScale = 1f;
		Session().ReturnToMenu();
	}

	private void RefreshConfirmLabels()
	{
		SetLabel(confirmNewBtn, armedNew ? "pause.confirm_new" : "pause.new_sim");
		SetLabel(confirmExitBtn, armedExit ? "pause.confirm_exit" : "sandbox.to_menu");
		Status((armedNew || armedExit) ? Localization.T("pause.unsaved_warning") : "");
	}

	private static void SetLabel(Button btn, string key)
	{
		if (btn == null)
		{
			return;
		}
		LocalizedLabel localizedLabel = btn.GetComponentInChildren<LocalizedLabel>();
		if (localizedLabel != null)
		{
			localizedLabel.SetKey(key);
		}
	}

	private void RefreshMode()
	{
		if (modeLabel == null)
		{
			return;
		}
		object[] array = new object[2];
		array[0] = Localization.T(GameSettings.TitleKey(AppFlow.ActiveMode));
		array[1] = MicroSpall.MaxLive;
		modeLabel.text = Localization.T("pause.mode", array);
	}

	private void Status(string s)
	{
		if (statusLabel != null)
		{
			statusLabel.text = s ?? "";
		}
	}

	private static TMP_InputField MakeInput(Transform parent, Vector2 pos, Vector2 size, string placeholderKey)
	{
		RectTransform rectTransform = MenuTheme.MakeRect(parent, "Input");
		MenuTheme.Place(rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, size);
		rectTransform.gameObject.AddComponent<Image>().color = MenuTheme.SurfaceAlt;
		RectTransform rectTransform2 = MenuTheme.MakeRect(rectTransform, "Text Area");
		rectTransform2.anchorMin = Vector2.zero;
		rectTransform2.anchorMax = Vector2.one;
		rectTransform2.offsetMin = new Vector2(14f, 6f);
		rectTransform2.offsetMax = new Vector2(-14f, -6f);
		// Text area has no Graphic: use RectMask2D so the input text is actually clipped.
		rectTransform2.gameObject.AddComponent<RectMask2D>();
		TextMeshProUGUI textMeshProUGUI = MenuTheme.MakeLabel(rectTransform2, "Placeholder", placeholderKey, 18f, FontStyles.Italic, MenuTheme.TextLocked, TextAlignmentOptions.Left);
		MenuTheme.Stretch(textMeshProUGUI.rectTransform);
		TextMeshProUGUI textMeshProUGUI2 = MenuTheme.MakeLabelRaw(rectTransform2, "Text", "", 18f, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		MenuTheme.Stretch(textMeshProUGUI2.rectTransform);
		TMP_InputField tMP_InputField = rectTransform.gameObject.AddComponent<TMP_InputField>();
		tMP_InputField.textViewport = rectTransform2;
		tMP_InputField.textComponent = textMeshProUGUI2;
		tMP_InputField.placeholder = textMeshProUGUI;
		tMP_InputField.lineType = TMP_InputField.LineType.SingleLine;
		return tMP_InputField;
	}
}
