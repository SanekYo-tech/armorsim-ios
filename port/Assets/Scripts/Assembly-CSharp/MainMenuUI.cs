using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
	private class KeyRow
	{
		public ControlAction action;

		public Button btn;
	}

	private class ModRow
	{
		public ControlModifier id;

		public Button[] btns;
	}

	private class PickRow
	{
		public Func<int> current;

		public Button[] btns;

		public Func<int, string> caption;
	}

	private class GestureRow
	{
		public ControlBindings.GestureEntry entry;

		public TextMeshProUGUI value;
	}

	private class ToggleRow
	{
		public Button on;

		public Button off;

		public Func<bool> getter;
	}

	public struct FlatFacts
	{
		public bool implemented;

		public OwnershipState ownership;

		public bool gateOpen;

		public string gateReason;

		public bool demoAvailable;

		public bool freeAccess;
	}

	public struct BodyFacts
	{
		public bool implemented;

		public OwnershipState ownership;

		public bool gateOpen;

		public string gateReason;

		public bool demoAvailable;
	}

	public struct FlatCardShape
	{
		public bool enterable;

		public string badgeKey;

		public string reasonKey;

		public bool storeButton;

		public bool demoButton;

		public bool qualityButtons;
	}

	public enum FlatState
	{
		NotImplemented = 0,
		NotOwned = 1,
		Unverified = 2,
		GateClosed = 3,
		Open = 4
	}

	public enum BodyState
	{
		NotImplemented = 0,
		NotOwned = 1,
		Unverified = 2,
		GateClosed = 3,
		Open = 4
	}

	public struct BodyCardShape
	{
		public bool enterable;

		public string badgeKey;

		public string reasonKey;

		public bool storeButton;

		public bool demoButton;
	}

	public enum SaveLoadAction
	{
		Refuse = 0,
		OpenSandbox3D = 1,
		EnterFlat2D = 2
	}

	private readonly List<KeyRow> _keyRows = new List<KeyRow>();

	private readonly List<ModRow> _modRows = new List<ModRow>();

	private readonly List<PickRow> _pickRows = new List<PickRow>();

	private readonly List<GestureRow> _gestureRows = new List<GestureRow>();

	private readonly Dictionary<ControlScope, GameObject> _controlPanels = new Dictionary<ControlScope, GameObject>();

	private readonly Dictionary<ControlScope, TextMeshProUGUI> _controlStatus = new Dictionary<ControlScope, TextMeshProUGUI>();

	private ControlAction _awaiting;

	private ControlScope _awaitingScope;

	private const float RowHeight = 62f;

	private const float RowValueWidth = 220f;

	[Header("Сцены")]
	[Tooltip("Имя сцены песочницы. Обязана быть в Build Settings.")]
	public string sandboxSceneName = "SampleScene";

	[Tooltip("Имя сцены меню — сюда возвращает Esc из песочницы.")]
	public string menuSceneName = "MainMenu";

	[Header("Логотип")]
	[Tooltip("Можно перетащить спрайт сюда напрямую.")]
	public Sprite logoSprite;

	[Tooltip("Иначе логотип грузится отсюда: Assets/Resources/<путь>.png (Texture Type = Sprite).")]
	public string logoResourcePath = "UI/logo_armour_simulator";

	[Header("Шрифт")]
	[Tooltip("TMP-шрифт с кириллицей, татарскими (ә ө ү җ ң һ), немецкими (ä ö ü ß) и польскими (ą ć ę ł ń ó ś ź ż) глифами. Без него не-ASCII символы будут квадратами.")]
	public TMP_FontAsset uiFont;

	[Header("Ссылки")]
	public string telegramUrl = "https://t.me/GalimovDynamics";

	public string discordUrl = "https://discord.gg/qp9wFhHkr";

	[Tooltip("Страница дополнения «Плоскость» в магазине. ⚠\ufe0f ПУСТО — И ЭТО НЕ ЗАБЫТО: AppID ещё нет (задача 5). OpenUrl на пустой строке выходит сразу, поэтому кнопка инертна, а не сломана. Оверлей Steam (ActivateGameOverlayToStore) — тоже задача 5.")]
	public string storeUrl = string.Empty;

	[Tooltip("Страница дополнения BODY в магазине. ⚠\ufe0f⚠\ufe0f ОТДЕЛЬНОЕ ПОЛЕ, А НЕ ОБЩЕЕ С «Плоскостью»: BODY — отдельная покупка, значит и страница у него своя. Одно поле на два дополнения увело бы покупателя BODY на чужую карточку в магазине — молча и правдоподобно. Сторожит это тест D9. ⚠\ufe0f ПУСТО и это не забыто: AppID ещё нет.")]
	public string bodyStoreUrl = string.Empty;

	[Tooltip("AeroSim store page: a separate purchase. Empty until assigned.")]
	public string aeroStoreUrl = string.Empty;

	[Tooltip("RadarSim store page: a separate purchase. Empty until assigned.")]
	public string radarStoreUrl = string.Empty;

	[Header("Прочее")]
	[Tooltip("Пусто — показывается настоящая версия сборки (PlayerSettings → Version). Заполнять только ради особой пометки: она ПЕРЕКРОЕТ настоящую версию.")]
	public string versionLabel = string.Empty;

	public string studioName = "GALIMOV DYNAMICS";

	public string leadName = "Тимерлан Галимов";

	private RectTransform _root;

	private GameObject _home;

	private GameObject _panelNew;

	private GameObject _panelSaves;

	private GameObject _panelAbout;

	private GameObject _panelSettings;

	private GameObject _panelWorkshop;

	private GameObject _panelFlatAlpha;

	private GameObject _flatAlphaReturn;

	private SaveEntry _flatAlphaPending;

	private readonly List<GameObject> _panels = new List<GameObject>();

	private RectTransform _savesContent;

	private Button _tabUser;

	private Button _tabPresets;

	private bool _showPresets;

	private RectTransform _workshopContent;

	private TextMeshProUGUI _workshopStatus;

	private TextMeshProUGUI _savesStatus;

	private readonly List<WorkshopItem> _workshopItems = new List<WorkshopItem>();

	private string _workshopStatusKey;

	private string _workshopStatusArg;

	private string _savesStatusKey;

	private string _savesStatusArg;

	private bool _workshopListOk;

	private string _workshopEmptyKey = "workshop.empty";

	private string _workshopWhere = string.Empty;

	private SimMode _pickedMode;

	private TargetSection _pickedSection;

	private SimFamily _pickedFamily;

	private Button _bodyCard;

	private Button _aeroCard;

	private Button _radarCard;

	private FlatQuality _pickedQuality;

	private Button _flatCard;

	private Button _flatCoarse;

	private Button _flatFine;

	private readonly Dictionary<SimMode, Button> _modeCards = new Dictionary<SimMode, Button>();

	private readonly Dictionary<SimMode, Button> _hullButtons = new Dictionary<SimMode, Button>();

	private readonly Dictionary<SimMode, Button> _roofButtons = new Dictionary<SimMode, Button>();

	private readonly Dictionary<SimMode, Button> _defaultModeButtons = new Dictionary<SimMode, Button>();

	private readonly Dictionary<Language, Button> _langButtons = new Dictionary<Language, Button>();

	private readonly List<ToggleRow> _toggleRows = new List<ToggleRow>();

	internal static Action<string> UrlSink;

	private bool _ultraRevealed;

	private int _logoTaps;

	private float _logoTapAt = float.NegativeInfinity;

	public const int UltraTapsToReveal = 5;

	public const float UltraTapWindowS = 5f;

	private const bool FlatDemoAvailable = false;

	public string VersionText
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(versionLabel))
			{
				return versionLabel.Trim();
			}
			string version = Application.version;
			return string.IsNullOrWhiteSpace(version) ? "?" : version;
		}
	}

	public static bool OverlayOpen { get; private set; }

	private bool UltraVisible => _ultraRevealed || GameSettings.DefaultMode == SimMode.Ultra;

	private void ShowControls(ControlScope scope)
	{
		CancelCapture(true);
		SetControlStatus(scope, null);
		RefreshControlRows();
		GameObject panel;
		if (_controlPanels.TryGetValue(scope, out panel))
		{
			ShowPanel(panel);
		}
	}

	private void BuildControlsPanels()
	{
		_controlPanels.Clear();
		_controlStatus.Clear();
		_keyRows.Clear();
		_modRows.Clear();
		_pickRows.Clear();
		_gestureRows.Clear();
		for (int i = 0; i < ControlBindings.AllScopes.Length; i++)
		{
			BuildControlsPanel(ControlBindings.AllScopes[i]);
		}
		RefreshControlRows();
	}

	private void BuildControlsPanel(ControlScope scope)
	{
		RectTransform body;
		GameObject panel = MakeOverlayPanel("Panel_Controls_" + scope, "controls.title", out body, ShowSettings);
		_controlPanels[scope] = panel;
		TextMeshProUGUI subtitle = MenuTheme.MakeLabel(body, "Subtitle", ControlBindings.TitleKey(scope), 20f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(900f, 28f));
		subtitle.characterSpacing = 4f;
		TextMeshProUGUI hint = MenuTheme.MakeLabel(body, "Hint", ControlBindings.HintKey(scope), 16f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -32f), new Vector2(1100f, 24f));
		TextMeshProUGUI status = MenuTheme.MakeLabelRaw(body, "Status", string.Empty, 16f, FontStyles.Bold, MenuTheme.Refused, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(status.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -62f), new Vector2(1100f, 24f));
		_controlStatus[scope] = status;
		Button reset = MenuTheme.MakeButton(body, "Reset", "controls.reset", 46f, delegate
		{
			ControlBindings.ResetScope(scope);
			CancelCapture(true);
			SetControlStatus(scope, Localization.T("controls.reset_done"));
			RefreshControlRows();
		}, 16f, TextAlignmentOptions.Center, 10f);
		MenuTheme.Place(reset.transform as RectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(320f, 46f));
		RectTransform content;
		ScrollRect scroll = MenuTheme.MakeScrollList(body, "ControlsScroll", out content);
		MenuTheme.Stretch(scroll.transform as RectTransform, new Vector2(0f, 58f), new Vector2(0f, -96f));
		BuildKeySection(content, scope);
		BuildModifierSection(content, scope);
		BuildMouseSection(content, scope);
		BuildGestureSection(content, scope);
	}

	private void BuildKeySection(RectTransform column, ControlScope scope)
	{
		List<ControlBindings.KeyEntry> entries = new List<ControlBindings.KeyEntry>();
		for (int i = 0; i < ControlBindings.Keys.Length; i++)
		{
			ControlBindings.KeyEntry entry = ControlBindings.Keys[i];
			if (entry.scope == scope)
			{
				entries.Add(entry);
			}
		}
		MakeSectionLabel(column, "controls.keys");
		if (entries.Count == 0)
		{
			MakeNoteRow(column, "controls.no_keys");
			return;
		}
		foreach (ControlBindings.KeyEntry entry in entries)
		{
			ControlAction captured = entry.action;
			RectTransform row = MakeRow(column, entry.titleKey, entry.hintKey);
			Button btn = MenuTheme.MakeButtonRaw(row, "Key_" + captured, string.Empty, 46f, delegate
			{
				BeginCapture(captured, scope);
			}, 17f, TextAlignmentOptions.Center, 10f);
			MenuTheme.Place(btn.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(RowValueWidth, 46f));
			_keyRows.Add(new KeyRow { action = captured, btn = btn });
		}
	}

	private void BuildModifierSection(RectTransform column, ControlScope scope)
	{
		List<ControlBindings.ModEntry> entries = new List<ControlBindings.ModEntry>();
		for (int i = 0; i < ControlBindings.Modifiers.Length; i++)
		{
			ControlBindings.ModEntry entry = ControlBindings.Modifiers[i];
			if (entry.scope == scope)
			{
				entries.Add(entry);
			}
		}
		if (entries.Count == 0)
		{
			return;
		}
		MakeSectionLabel(column, "controls.mods");
		foreach (ControlBindings.ModEntry entry in entries)
		{
			ControlModifier captured = entry.id;
			RectTransform row = MakeRow(column, entry.titleKey, entry.hintKey);
			Button[] btns = new Button[ControlBindings.AllModifierKinds.Length];
			for (int i = 0; i < ControlBindings.AllModifierKinds.Length; i++)
			{
				ModifierKind kind = ControlBindings.AllModifierKinds[i];
				Button btn = MenuTheme.MakeButtonRaw(row, "Mod_" + captured + "_" + kind, ControlBindings.ModifierName(kind), 46f, delegate
				{
					AssignModifier(captured, kind, scope);
				}, 16f, TextAlignmentOptions.Center, 10f);
				MenuTheme.Place(btn.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2((i - ControlBindings.AllModifierKinds.Length + 1) * 106f, 0f), new Vector2(100f, 46f));
				btns[i] = btn;
			}
			_modRows.Add(new ModRow { id = captured, btns = btns });
		}
	}

	private void BuildMouseSection(RectTransform column, ControlScope scope)
	{
		MakeSectionLabel(column, "controls.mouse");
		if (scope == ControlScope.Analytic3D)
		{
			RectTransform row = MakeRow(column, "controls.orbit_button", "controls.orbit_button_hint");
			int[] buttons = new int[] { 2, 1 };
			Button[] btns = new Button[buttons.Length];
			for (int i = 0; i < buttons.Length; i++)
			{
				int button = buttons[i];
				Button btn = MenuTheme.MakeButtonRaw(row, "Orbit_" + button, ControlBindings.MouseName(button), 46f, delegate
				{
					ControlBindings.SetOrbitOnRight(button == 1);
					SetControlStatus(scope, null);
					RefreshControlRows();
				}, 15f, TextAlignmentOptions.Center, 8f);
				MenuTheme.Place(btn.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2((i - buttons.Length + 1) * 186f, 0f), new Vector2(180f, 46f));
				btns[i] = btn;
			}
			_pickRows.Add(new PickRow
			{
				current = () => ControlBindings.OrbitOnRight ? 1 : 0,
				btns = btns,
				caption = i => ControlBindings.MouseName(buttons[i])
			});
		}
		RectTransform invertRow = MakeRow(column, "controls.invert_wheel", "controls.invert_wheel_hint");
		Button on = MenuTheme.MakeButton(invertRow, "Invert_On", "settings.on", 46f, delegate
		{
			ControlBindings.SetInvertWheel(scope, true);
			SetControlStatus(scope, null);
			RefreshControlRows();
		}, 16f, TextAlignmentOptions.Center, 10f);
		MenuTheme.Place(on.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-146f, 0f), new Vector2(140f, 46f));
		Button off = MenuTheme.MakeButton(invertRow, "Invert_Off", "settings.off", 46f, delegate
		{
			ControlBindings.SetInvertWheel(scope, false);
			SetControlStatus(scope, null);
			RefreshControlRows();
		}, 16f, TextAlignmentOptions.Center, 10f);
		MenuTheme.Place(off.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(140f, 46f));
		ControlScope capturedScope = scope;
		_pickRows.Add(new PickRow
		{
			current = () => !ControlBindings.InvertWheel(capturedScope) ? 1 : 0,
			btns = new Button[] { on, off }
		});
	}

	private void BuildGestureSection(RectTransform column, ControlScope scope)
	{
		List<ControlBindings.GestureEntry> entries = new List<ControlBindings.GestureEntry>();
		for (int i = 0; i < ControlBindings.Gestures.Length; i++)
		{
			ControlBindings.GestureEntry entry = ControlBindings.Gestures[i];
			if (entry.scope == scope)
			{
				entries.Add(entry);
			}
		}
		if (entries.Count == 0)
		{
			return;
		}
		MakeSectionLabel(column, "controls.fixed");
		MakeNoteRow(column, "controls.fixed_hint");
		foreach (ControlBindings.GestureEntry entry in entries)
		{
			RectTransform row = MakeRow(column, entry.titleKey, null);
			TextMeshProUGUI value = MenuTheme.MakeLabelRaw(row, "Value", string.Empty, 16f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Right);
			MenuTheme.Place(value.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(320f, 40f));
			_gestureRows.Add(new GestureRow { entry = entry, value = value });
		}
	}

	private void MakeSectionLabel(RectTransform column, string key)
	{
		TextMeshProUGUI label = MenuTheme.MakeLabel(column, "Section_" + key, key, 18f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.BottomLeft);
		label.characterSpacing = 4f;
		LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = 40f;
		layout.preferredHeight = 40f;
	}

	private void MakeNoteRow(RectTransform column, string key)
	{
		TextMeshProUGUI label = MenuTheme.MakeLabel(column, "Note_" + key, key, 14f, FontStyles.Italic, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = 40f;
		layout.preferredHeight = 40f;
	}

	private RectTransform MakeRow(RectTransform column, string titleKey, string hintKey)
	{
		RectTransform row = MenuTheme.MakeRect(column, "Row_" + titleKey);
		LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = RowHeight;
		layout.preferredHeight = RowHeight;
		TextMeshProUGUI title = MenuTheme.MakeLabel(row, "Title", titleKey, 17f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -4f), new Vector2(760f, 24f));
		if (!string.IsNullOrEmpty(hintKey))
		{
			TextMeshProUGUI hint = MenuTheme.MakeLabel(row, "Hint", hintKey, 13f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
			MenuTheme.Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -28f), new Vector2(760f, 30f));
		}
		MenuTheme.MakeSeparator(column);
		return row;
	}

	private void BeginCapture(ControlAction action, ControlScope scope)
	{
		_awaiting = action;
		_awaitingScope = scope;
		ControlBindings.KeyEntry entry = ControlBindings.EntryOf(action);
		SetControlStatus(scope, Localization.T("controls.press_hint", entry == null ? action.ToString() : Localization.T(entry.titleKey)));
		RefreshControlRows();
	}

	private void CancelCapture(bool silent)
	{
		if (_awaiting == ControlAction.None)
		{
			return;
		}
		_awaiting = ControlAction.None;
		if (!silent)
		{
			SetControlStatus(_awaitingScope, Localization.T("controls.cancelled"));
		}
		RefreshControlRows();
	}

	private void Update()
	{
		if (_awaiting == ControlAction.None)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			CancelCapture(false);
			return;
		}
		KeyCode[] candidates = ControlBindings.CaptureCandidates;
		for (int i = 0; i < candidates.Length; i++)
		{
			KeyCode key = candidates[i];
			if (Input.GetKeyDown(key))
			{
				AssignKey(_awaiting, key, _awaitingScope);
				return;
			}
		}
	}

	private void AssignKey(ControlAction action, KeyCode key, ControlScope scope)
	{
		string refusalKey;
		ControlAction conflict;
		bool assigned = ControlBindings.TryAssign(action, key, out refusalKey, out conflict);
		_awaiting = ControlAction.None;
		if (!assigned)
		{
			ControlBindings.KeyEntry conflictEntry = ControlBindings.EntryOf(conflict);
			SetControlStatus(scope, Localization.T(refusalKey, ControlBindings.KeyName(key), conflictEntry == null ? string.Empty : Localization.T(conflictEntry.titleKey)));
		}
		else
		{
			ControlBindings.KeyEntry entry = ControlBindings.EntryOf(action);
			SetControlStatus(scope, Localization.T("controls.assigned", entry == null ? action.ToString() : Localization.T(entry.titleKey), ControlBindings.KeyName(key)));
		}
		RefreshControlRows();
	}

	private void AssignModifier(ControlModifier id, ModifierKind kind, ControlScope scope)
	{
		string refusalKey;
		ControlModifier conflict;
		if (!ControlBindings.TryAssignModifier(id, kind, out refusalKey, out conflict))
		{
			ControlBindings.ModEntry conflictEntry = ControlBindings.EntryOf(conflict);
			SetControlStatus(scope, Localization.T(refusalKey, ControlBindings.ModifierName(kind), conflictEntry == null ? string.Empty : Localization.T(conflictEntry.titleKey)));
		}
		else
		{
			SetControlStatus(scope, null);
		}
		RefreshControlRows();
	}

	private void ClearControlStatuses()
	{
		foreach (TextMeshProUGUI status in _controlStatus.Values)
		{
			if (status != null)
			{
				status.text = string.Empty;
			}
		}
	}

	private void SetControlStatus(ControlScope scope, string text)
	{
		TextMeshProUGUI status;
		if (_controlStatus.TryGetValue(scope, out status) && status != null)
		{
			status.text = text ?? string.Empty;
		}
	}

	private void RefreshControlRows()
	{
		foreach (KeyRow row in _keyRows)
		{
			if (row.btn == null)
			{
				continue;
			}
			TextMeshProUGUI label = MenuTheme.LabelOf(row.btn);
			if (label == null)
			{
				continue;
			}
			bool waiting = _awaiting == row.action;
			label.text = waiting ? Localization.T("controls.press") : ControlBindings.KeyName(ControlBindings.Key(row.action));
			MenuTheme.SetSelectedVisual(row.btn, waiting);
		}
		foreach (ModRow row in _modRows)
		{
			ModifierKind current = ControlBindings.Modifier(row.id);
			for (int i = 0; i < row.btns.Length && i < ControlBindings.AllModifierKinds.Length; i++)
			{
				MenuTheme.SetSelectedVisual(row.btns[i], ControlBindings.AllModifierKinds[i] == current);
			}
		}
		foreach (PickRow row in _pickRows)
		{
			int current = row.current();
			for (int i = 0; i < row.btns.Length; i++)
			{
				MenuTheme.SetSelectedVisual(row.btns[i], i == current);
				if (row.caption != null)
				{
					TextMeshProUGUI label = MenuTheme.LabelOf(row.btns[i]);
					if (label != null)
					{
						label.text = row.caption(i);
					}
				}
			}
		}
		foreach (GestureRow row in _gestureRows)
		{
			if (row.value != null)
			{
				row.value.text = row.entry.Value;
			}
		}
	}

	private void Awake()
	{
		MenuTheme.Font = uiFont;
		AppFlow.MenuSceneName = menuSceneName;
		_pickedMode = GameSettings.DefaultMode;
		EnsureEventSystem();
		MusicPlayer.EnsureIn(transform, MusicPolicy.MenuProgram(MusicSession.WasInGame));
		BuildCanvas();
		BuildHome();
		BuildNewSimulationPanel();
		BuildFlatAlphaPanel();
		BuildSavesPanel();
		BuildWorkshopPanel();
		BuildAboutPanel();
		BuildSettingsPanel();
		BuildControlsPanels();
		ShowPanel(null);
		Localization.OnLanguageChanged += OnLanguageChanged;
		WorkshopService.Backend.ItemsChanged += OnWorkshopItemsChanged;
	}

	private void OnDestroy()
	{
		Localization.OnLanguageChanged -= OnLanguageChanged;
		WorkshopService.Backend.ItemsChanged -= OnWorkshopItemsChanged;
	}

	private void OnLanguageChanged()
	{
		RefreshSavesList();
		RefreshWorkshopRows();
		ApplyWorkshopStatus();
		ApplySavesStatus();
		RefreshLanguageButtons();
		ClearControlStatuses();
		RefreshControlRows();
	}

	private void EnsureEventSystem()
	{
		if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
		{
			return;
		}
		GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
		eventSystem.transform.SetParent(transform, false);
	}

	private void BuildCanvas()
	{
		GameObject canvasObject = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvasObject.transform.SetParent(transform, false);
		canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
		CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		scaler.matchWidthOrHeight = 0.5f;
		_root = canvasObject.transform as RectTransform;
		RectTransform background = MenuTheme.MakeRect(_root, "Background");
		MenuTheme.Stretch(background);
		MenuTheme.AddImage(background, MenuTheme.Background);
	}

	private void BuildHome()
	{
		_home = MenuTheme.MakeRect(_root, "Home").gameObject;
		MenuTheme.Stretch(_home.transform as RectTransform);
		Transform parent = _home.transform;
		Sprite sprite = logoSprite;
		if (sprite == null && !string.IsNullOrEmpty(logoResourcePath))
		{
			sprite = Resources.Load<Sprite>(logoResourcePath);
		}
		float cursor;
		if (sprite != null)
		{
			float aspect = sprite.rect.height <= 0.001f ? 1f : sprite.rect.width / sprite.rect.height;
			aspect = Mathf.Max(0.001f, aspect);
			float logoWidth = 640f;
			float logoHeight = 640f / aspect;
			if (logoHeight > 400f)
			{
				logoWidth = aspect * 400f;
				logoHeight = 400f;
			}
			RectTransform logo = MenuTheme.MakeRect(parent, "Logo");
			Image image = logo.gameObject.AddComponent<Image>();
			image.sprite = sprite;
			image.preserveAspect = true;
			image.raycastTarget = false;
			MenuTheme.Place(logo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(logoWidth, logoHeight));
			cursor = -30f - logoHeight;
			Debug.Log(string.Format("[MainMenuUI] Логотип {0:F0}x{1:F0} ", sprite.rect.width, sprite.rect.height) + string.Format("(пропорции {0:F2}) вписан как {1:F0}x{2:F0}.", aspect, logoWidth, logoHeight));
		}
		else
		{
			Debug.LogWarning("[MainMenuUI] Логотип не найден по пути Resources/" + logoResourcePath + " — показываю текстовую заглушку.");
			TextMeshProUGUI armour = MenuTheme.MakeLabelRaw(parent, "LogoText", "ARMOUR", 88f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Center);
			MenuTheme.Place(armour.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 100f));
			armour.characterSpacing = 12f;
			TextMeshProUGUI simulator = MenuTheme.MakeLabelRaw(parent, "LogoText2", "SIMULATOR", 52f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Center);
			MenuTheme.Place(simulator.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -215f), new Vector2(900f, 70f));
			simulator.characterSpacing = 18f;
			cursor = -370f;
		}
		RectTransform tapTarget = MenuTheme.MakeRect(parent, "LogoTapTarget");
		MenuTheme.Place(tapTarget, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(640f, Mathf.Max(60f, -cursor - 30f)));
		Image tapImage = tapTarget.gameObject.AddComponent<Image>();
		tapImage.color = Color.clear;
		tapImage.raycastTarget = true;
		Button tapButton = tapTarget.gameObject.AddComponent<Button>();
		tapButton.targetGraphic = tapImage;
		tapButton.transition = Selectable.Transition.None;
		tapButton.onClick.AddListener(TapLogo);
		TextMeshProUGUI tagline = MenuTheme.MakeLabel(parent, "Tagline", "menu.tagline", 20f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Center);
		MenuTheme.Place(tagline.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, cursor - 15f), new Vector2(900f, 28f));
		tagline.characterSpacing = 3f;
		RectTransform nav = MenuTheme.MakeColumn(parent, "Nav", 10f);
		MenuTheme.Place(nav, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, cursor - 80f), new Vector2(480f, 374f));
		MenuTheme.MakeButton(nav, "BtnNew", "menu.new_simulation", 54f, ShowNewSimulation, 22f, TextAlignmentOptions.Left, 24f);
		MenuTheme.MakeButton(nav, "BtnSaves", "menu.saves", 54f, ShowSaves, 22f, TextAlignmentOptions.Left, 24f);
		MenuTheme.MakeButton(nav, "BtnWorkshop", "menu.workshop", 54f, ShowWorkshop, 22f, TextAlignmentOptions.Left, 24f);
		MenuTheme.MakeButton(nav, "BtnAbout", "menu.about", 54f, ShowAbout, 22f, TextAlignmentOptions.Left, 24f);
		MenuTheme.MakeButton(nav, "BtnSettings", "menu.settings", 54f, ShowSettings, 22f, TextAlignmentOptions.Left, 24f);
		MenuTheme.MakeButton(nav, "BtnQuit", "menu.quit", 54f, AppFlow.Quit, 22f, TextAlignmentOptions.Left, 24f);
		TextMeshProUGUI socialLabel = MenuTheme.MakeLabel(parent, "SocialLabel", "menu.social", 16f, FontStyles.Bold, MenuTheme.TextMuted, TextAlignmentOptions.Center);
		MenuTheme.Place(socialLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 118f), new Vector2(600f, 24f));
		socialLabel.characterSpacing = 6f;
		RectTransform social = MenuTheme.MakeRow(parent, "SocialRow", 16f);
		MenuTheme.Place(social, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(520f, 52f));
		Button telegram = MenuTheme.MakeButtonRaw(social, "BtnTelegram", "TELEGRAM", 52f, delegate { OpenUrl(telegramUrl); }, 20f, TextAlignmentOptions.Center, 12f);
		telegram.gameObject.AddComponent<LayoutElement>().preferredWidth = 240f;
		Button discord = MenuTheme.MakeButtonRaw(social, "BtnDiscord", "DISCORD", 52f, delegate { OpenUrl(discordUrl); }, 20f, TextAlignmentOptions.Center, 12f);
		discord.gameObject.AddComponent<LayoutElement>().preferredWidth = 240f;
		string versionText = VersionText;
		TextMeshProUGUI version = MenuTheme.MakeLabelRaw(parent, "Version", Localization.T("common.version") + " " + versionText, 15f, FontStyles.Normal, MenuTheme.TextLocked, TextAlignmentOptions.Left);
		MenuTheme.Place(version.rectTransform, Vector2.zero, Vector2.zero, new Vector2(28f, 36f), new Vector2(400f, 22f));
		LocalizedLabel localized = version.gameObject.AddComponent<LocalizedLabel>();
		localized.key = "common.version";
		localized.suffix = " " + versionText;
		localized.Apply();
	}

	private void OpenUrl(string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return;
		}
		if (UrlSink == null)
		{
			Application.OpenURL(url);
		}
		else
		{
			UrlSink(url);
		}
	}

	private GameObject MakeOverlayPanel(string name, string titleKey, out RectTransform body, UnityAction onBack = null)
	{
		RectTransform panel = MenuTheme.MakeRect(_root, name);
		MenuTheme.Stretch(panel, new Vector2(200f, 80f), new Vector2(-200f, -80f));
		Image image = panel.gameObject.AddComponent<Image>();
		image.color = MenuTheme.Surface;
		image.raycastTarget = true;
		TextMeshProUGUI title = MenuTheme.MakeLabel(panel, "Title", titleKey, 32f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		MenuTheme.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(900f, 44f));
		title.characterSpacing = 5f;
		Button back = MenuTheme.MakeButton(panel, "BtnBack", "menu.back", 44f, onBack ?? ShowHome, 18f, TextAlignmentOptions.Center, 12f);
		MenuTheme.Place(back.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -28f), new Vector2(180f, 44f));
		RectTransform line = MenuTheme.MakeRect(panel, "HeaderLine");
		line.anchorMin = new Vector2(0f, 1f);
		line.anchorMax = new Vector2(1f, 1f);
		line.pivot = new Vector2(0.5f, 1f);
		line.anchoredPosition = new Vector2(0f, -96f);
		line.sizeDelta = new Vector2(-80f, 1f);
		MenuTheme.AddImage(line, MenuTheme.Line);
		body = MenuTheme.MakeRect(panel, "Body");
		MenuTheme.Stretch(body, new Vector2(40f, 40f), new Vector2(-40f, -116f));
		panel.gameObject.SetActive(false);
		_panels.Add(panel.gameObject);
		return panel.gameObject;
	}

	private void ShowPanel(GameObject panel)
	{
		CancelCapture(true);
		OverlayOpen = panel != null;
		if (_home != null)
		{
			_home.SetActive(panel == null);
		}
		foreach (GameObject item in _panels)
		{
			if (item != null)
			{
				item.SetActive(item == panel);
			}
		}
	}

	private void ShowHome()
	{
		ShowPanel(null);
	}

	private void ShowNewSimulation()
	{
		GameEvents.RaiseNewSimulationRequested();
		_pickedFamily = SimFamily.Analytic3D;
		_pickedMode = GameSettings.DefaultMode;
		RefreshModeCards();
		ShowPanel(_panelNew);
	}

	private void ShowSaves()
	{
		_savesStatusKey = null;
		_savesStatusArg = null;
		ApplySavesStatus();
		RefreshSavesList();
		ShowPanel(_panelSaves);
	}

	private void ShowWorkshop()
	{
		RequestWorkshopList();
		ShowPanel(_panelWorkshop);
	}

	private void ShowAbout()
	{
		ShowPanel(_panelAbout);
	}

	private void ShowSettings()
	{
		RefreshLanguageButtons();
		RefreshDefaultModeButtons();
		RefreshToggleRows();
		ShowPanel(_panelSettings);
	}

	private void BuildNewSimulationPanel()
	{
		RectTransform body;
		_panelNew = MakeOverlayPanel("Panel_New", "new.title", out body);
		TextMeshProUGUI subtitle = MenuTheme.MakeLabel(body, "Subtitle", "new.pick_mode", 20f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(900f, 28f));
		subtitle.characterSpacing = 4f;
		TextMeshProUGUI hint = MenuTheme.MakeLabel(body, "Hint", "new.hint", 16f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -34f), new Vector2(1100f, 44f));
		RectTransform modes;
		ScrollRect scroll = MenuTheme.MakeScrollList(body, "ModesScroll", out modes);
		MenuTheme.Stretch(scroll.transform as RectTransform, new Vector2(0f, 68f), new Vector2(14f, -92f));
		scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
		modes.name = "Modes";
		modes.GetComponent<VerticalLayoutGroup>().spacing = 12f;
		AeroMenuScroll aeroScroll = scroll.gameObject.AddComponent<AeroMenuScroll>();
		_modeCards.Clear();
		_hullButtons.Clear();
		_roofButtons.Clear();
		for (int i = 0; i < GameSettings.AllModes.Length; i++)
		{
			SimMode mode = GameSettings.AllModes[i];
			_modeCards[mode] = MakeModeCard(modes, mode);
		}
		_flatCard = MakeFlatCard(modes);
		_bodyCard = MakeBodyCard(modes);
		TextMeshProUGUI aeroSection = MenuTheme.MakeLabel(modes, "AeroSection", "aero.section", 20f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		LayoutElement sectionLayout = aeroSection.gameObject.AddComponent<LayoutElement>();
		sectionLayout.minHeight = 28f;
		sectionLayout.preferredHeight = 28f;
		_aeroCard = MakeAeroCard(modes);
		_radarCard = MakeRadarCard(modes);
		aeroScroll.Configure(scroll);
		ApplyUltraVisibility();
		Button ultra;
		if (_modeCards.TryGetValue(SimMode.Ultra, out ultra) && !ultra.gameObject.activeSelf)
		{
			RectTransform rect = ultra.transform as RectTransform;
			rect.anchoredPosition += new Vector2(0f, 58f);
		}
		Button start = MenuTheme.MakeButton(body, "BtnStart", "new.start", 56f, StartSimulation, 22f, TextAlignmentOptions.Center, 12f);
		MenuTheme.Place(start.transform as RectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(320f, 56f));
		RefreshModeCards();
	}

	private static bool UltraGateOpen(out string reasonKey)
	{
		return ContinuumRouting.IsUltraUnlocked(null, out reasonKey);
	}

	private void TapLogo()
	{
		float now = Time.unscaledTime;
		_logoTaps = now - _logoTapAt > UltraTapWindowS ? 1 : _logoTaps + 1;
		_logoTapAt = now;
		if (_logoTaps >= UltraTapsToReveal && !_ultraRevealed)
		{
			_ultraRevealed = true;
			_logoTaps = 0;
			Debug.Log(string.Format("[MainMenuUI] Пасхалка: ULTRA открыт ({0} нажатий по логотипу). ", UltraTapsToReveal) + "Режим заброшен; маршрут и сохранения не менялись.");
			ApplyUltraVisibility();
		}
	}

	private void ApplyUltraVisibility()
	{
		bool visible = UltraVisible;
		Button button;
		if (_modeCards.TryGetValue(SimMode.Ultra, out button) && button != null)
		{
			button.gameObject.SetActive(visible);
		}
		if (_defaultModeButtons.TryGetValue(SimMode.Ultra, out button) && button != null)
		{
			button.gameObject.SetActive(visible);
		}
		RefreshModeCards();
	}

	private Button MakeModeCard(Transform parent, SimMode mode)
	{
		bool unlocked = GameSettings.IsUnlocked(mode);
		string reasonKey = null;
		string gateReason = null;
		if (unlocked && mode == SimMode.Ultra && !UltraGateOpen(out reasonKey))
		{
			unlocked = false;
			gateReason = reasonKey;
		}
		SimMode captured = mode;
		RectTransform card = MenuTheme.MakeRect(parent, "Mode_" + captured);
		Image background = card.gameObject.AddComponent<Image>();
		background.color = MenuTheme.SurfaceAlt;
		background.raycastTarget = true;
		Button button = card.gameObject.AddComponent<Button>();
		button.targetGraphic = background;
		MenuTheme.ApplyColors(button);
		button.interactable = unlocked;
		button.onClick.AddListener(delegate { SelectMode(captured); });
		LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = 104f;
		layout.preferredHeight = 104f;
		RectTransform accent = MenuTheme.MakeRect(card, "Accent");
		accent.anchorMin = Vector2.zero;
		accent.anchorMax = new Vector2(0f, 1f);
		accent.pivot = new Vector2(0f, 0.5f);
		accent.offsetMin = Vector2.zero;
		accent.offsetMax = new Vector2(4f, 0f);
		MenuTheme.AddImage(accent, MenuTheme.Line);
		TextMeshProUGUI title = MenuTheme.MakeLabel(card, "Title", GameSettings.TitleKey(mode), 24f, FontStyles.Bold, unlocked ? MenuTheme.TextPrimary : MenuTheme.TextLocked, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -18f), new Vector2(700f, 30f));
		title.characterSpacing = 4f;
		TextMeshProUGUI desc = MenuTheme.MakeLabel(card, "Desc", GameSettings.DescKey(mode), 16f, FontStyles.Normal, unlocked ? MenuTheme.TextMuted : MenuTheme.TextLocked, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -54f), new Vector2(1000f, 44f));
		if (!GameSettings.IsUnlocked(mode))
		{
			TextMeshProUGUI badge = MenuTheme.MakeLabel(card, "Badge", "mode.soon", 15f, FontStyles.Bold, MenuTheme.TextMuted, TextAlignmentOptions.Right);
			MenuTheme.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(200f, 24f));
			badge.characterSpacing = 6f;
		}
		else if (gateReason == null)
		{
			if (mode == SimMode.Ultra)
			{
				TextMeshProUGUI note = MenuTheme.MakeLabel(card, "AnglesNote", "mode.ultra_abandoned", 14f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
				MenuTheme.Place(note.rectTransform, Vector2.zero, Vector2.zero, new Vector2(26f, 14f), new Vector2(1000f, 20f));
			}
		}
		else
		{
			TextMeshProUGUI reason = MenuTheme.MakeLabelRaw(card, "GateReason", UltraHudText.ModeEntry(gateReason), 14f, FontStyles.Normal, MenuTheme.TextLocked, TextAlignmentOptions.TopLeft);
			MenuTheme.Place(reason.rectTransform, Vector2.zero, Vector2.zero, new Vector2(26f, 14f), new Vector2(1000f, 20f));
		}
		if (unlocked && GameSettings.SupportsSection(mode))
		{
			_hullButtons[mode] = MakeSectionButton(card, mode, TargetSection.Hull, -236f);
			_roofButtons[mode] = MakeSectionButton(card, mode, TargetSection.Roof, -24f);
		}
		return button;
	}

	private Button MakeSectionButton(RectTransform card, SimMode mode, TargetSection section, float rightX)
	{
		Button button = MenuTheme.MakeButton(card, "Section_" + mode + "_" + section, GameSettings.SectionKey(section), 52f, delegate { SelectSection(mode, section); }, 18f, TextAlignmentOptions.Center, 10f);
		MenuTheme.Place(button.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(rightX, 0f), new Vector2(200f, 52f));
		return button;
	}

	private static BodyFacts ReadBodyFacts()
	{
		return new BodyFacts
		{
			implemented = false,
			ownership = Entitlements.State(ContentPack.Body),
			gateOpen = true,
			gateReason = null,
			demoAvailable = false
		};
	}

	private static FlatFacts ReadFlatFacts()
	{
		return new FlatFacts
		{
			implemented = true,
			ownership = Entitlements.State(ContentPack.Flat2D),
			gateOpen = Flat2DGate.IsAvailable(out string gateReason),
			gateReason = gateReason,
			demoAvailable = false,
			freeAccess = true
		};
	}

	internal static FlatState DescribeFlatState(FlatFacts f)
	{
		if (!f.implemented)
		{
			return FlatState.NotImplemented;
		}
		if (f.freeAccess && f.ownership != OwnershipState.Owned)
		{
			return f.gateOpen ? FlatState.Open : FlatState.GateClosed;
		}
		switch (f.ownership)
		{
			case OwnershipState.Owned:
				return f.gateOpen ? FlatState.Open : FlatState.GateClosed;
			case OwnershipState.NotOwned:
				return FlatState.NotOwned;
			case OwnershipState.Unknown:
				return FlatState.Unverified;
			default:
				Debug.LogError(string.Format("[MainMenuUI] неизвестное состояние владения {0}; ", f.ownership) + "«Плоскость» закрыта как непроверенная.");
				return FlatState.Unverified;
		}
	}

	internal static FlatCardShape DescribeFlatCard(FlatFacts f)
	{
		FlatState state = DescribeFlatState(f);
		switch (state)
		{
			case FlatState.NotImplemented:
				return new FlatCardShape { badgeKey = "mode.soon" };
			case FlatState.NotOwned:
				return new FlatCardShape { badgeKey = "dlc.badge", reasonKey = "dlc.not_owned_hint", storeButton = true, demoButton = f.demoAvailable };
			case FlatState.Unverified:
				return new FlatCardShape { badgeKey = "dlc.badge", reasonKey = "dlc.unverified", storeButton = true };
			case FlatState.GateClosed:
				return new FlatCardShape { reasonKey = f.gateReason };
			case FlatState.Open:
				return new FlatCardShape { enterable = true, qualityButtons = true };
			default:
				Debug.LogError("[MainMenuUI] неизвестное положение «Плоскости»; карточка закрыта.");
				return new FlatCardShape { badgeKey = "dlc.badge", reasonKey = "dlc.unverified" };
		}
	}

	internal static string FlatSaveRefusalKey(FlatState state, string gateReason)
	{
		switch (state)
		{
			case FlatState.NotImplemented:
				return "mode.soon";
			case FlatState.NotOwned:
				return "dlc.save_locked";
			case FlatState.Unverified:
				return "dlc.unverified";
			case FlatState.GateClosed:
				return gateReason;
			case FlatState.Open:
				return null;
			default:
				Debug.LogError(string.Format("[MainMenuUI] неизвестное положение «Плоскости» ({0}) в строке ", state) + "сохранения; загрузка запрещена.");
				return "dlc.unverified";
		}
	}

	private Button MakeFlatCard(Transform parent)
	{
		return MakeFlatCard(parent, ReadFlatFacts());
	}

	internal Button MakeFlatCard(Transform parent, FlatFacts facts)
	{
		FlatCardShape shape = DescribeFlatCard(facts);
		RectTransform card = MenuTheme.MakeRect(parent, "Mode_Flat2D");
		Image background = card.gameObject.AddComponent<Image>();
		background.color = MenuTheme.SurfaceAlt;
		background.raycastTarget = true;
		Button button = card.gameObject.AddComponent<Button>();
		button.targetGraphic = background;
		MenuTheme.ApplyColors(button);
		button.interactable = shape.enterable;
		button.onClick.AddListener(SelectFlat);
		LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = 104f;
		layout.preferredHeight = 104f;
		RectTransform accent = MenuTheme.MakeRect(card, "Accent");
		accent.anchorMin = Vector2.zero;
		accent.anchorMax = new Vector2(0f, 1f);
		accent.pivot = new Vector2(0f, 0.5f);
		accent.offsetMin = Vector2.zero;
		accent.offsetMax = new Vector2(4f, 0f);
		MenuTheme.AddImage(accent, MenuTheme.Line);
		TextMeshProUGUI title = MenuTheme.MakeLabel(card, "Title", "mode.flat", 24f, FontStyles.Bold, shape.enterable ? MenuTheme.TextPrimary : MenuTheme.TextLocked, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -18f), new Vector2(700f, 30f));
		title.characterSpacing = 4f;
		TextMeshProUGUI desc = MenuTheme.MakeLabel(card, "Desc", "mode.flat_desc", 16f, FontStyles.Normal, shape.enterable ? MenuTheme.TextMuted : MenuTheme.TextLocked, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -54f), new Vector2(1000f, 44f));
		if (shape.badgeKey != null)
		{
			TextMeshProUGUI badge = MenuTheme.MakeLabel(card, "Badge", shape.badgeKey, 15f, FontStyles.Bold, MenuTheme.TextMuted, TextAlignmentOptions.Right);
			MenuTheme.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(200f, 24f));
			badge.characterSpacing = 6f;
		}
		if (shape.reasonKey != null)
		{
			TextMeshProUGUI reason = MenuTheme.MakeLabel(card, "Reason", shape.reasonKey, 14f, FontStyles.Normal, MenuTheme.TextLocked, TextAlignmentOptions.TopLeft);
			MenuTheme.Place(reason.rectTransform, Vector2.zero, Vector2.zero, new Vector2(26f, 14f), new Vector2(1000f, 20f));
		}
		if (shape.storeButton)
		{
			Button store = MakeFlatButton(card, "Flat_Store", "dlc.open_store", -24f, delegate { OpenUrl(storeUrl); });
			if (string.IsNullOrEmpty(storeUrl))
			{
				store.interactable = false;
				TextMeshProUGUI label = MenuTheme.LabelOf(store);
				label.color = MenuTheme.TextLocked;
				label.text = Localization.T("dlc.store_unavailable") + " — " + Localization.T("dlc.store_no_appid");
				label.fontSize = 13f;
			}
		}
		if (shape.demoButton)
		{
			MakeFlatButton(card, "Flat_Demo", "dlc.show_demo", -236f, null);
		}
		if (shape.qualityButtons)
		{
			_flatCoarse = MakeFlatButton(card, "Flat_Coarse", "flat.quality.coarse", -24f, delegate { SelectFlatQuality(FlatQuality.Coarse); });
			_flatFine = MakeFlatButton(card, "Flat_Fine", "flat.quality.fine", -236f, delegate { SelectFlatQuality(FlatQuality.Fine); });
		}
		return button;
	}

	private Button MakeFlatButton(RectTransform card, string name, string key, float rightX, UnityAction onClick)
	{
		Button button = MenuTheme.MakeButton(card, name, key, 52f, null, 18f, TextAlignmentOptions.Center, 10f);
		if (onClick != null)
		{
			button.onClick.AddListener(onClick);
		}
		MenuTheme.Place(button.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(rightX, 0f), new Vector2(200f, 52f));
		return button;
	}

	internal static BodyState DescribeBodyState(BodyFacts f)
	{
		if (!f.implemented)
		{
			return BodyState.NotImplemented;
		}
		switch (f.ownership)
		{
			case OwnershipState.Owned:
				return f.gateOpen ? BodyState.Open : BodyState.GateClosed;
			case OwnershipState.NotOwned:
				return BodyState.NotOwned;
			case OwnershipState.Unknown:
				return BodyState.Unverified;
			default:
				Debug.LogError(string.Format("[MainMenuUI] неизвестное состояние владения {0}; ", f.ownership) + "BODY закрыт как непроверенный.");
				return BodyState.Unverified;
		}
	}

	internal static BodyCardShape DescribeBodyCard(BodyFacts f)
	{
		if (!f.implemented)
		{
			return new BodyCardShape { badgeKey = "mode.soon" };
		}
		switch (f.ownership)
		{
			case OwnershipState.Owned:
				if (f.gateOpen)
				{
					return new BodyCardShape { enterable = true };
				}
				return new BodyCardShape { reasonKey = f.gateReason };
			case OwnershipState.NotOwned:
				return new BodyCardShape { badgeKey = "dlc.badge", reasonKey = "dlc.not_owned_hint", storeButton = true, demoButton = f.demoAvailable };
			case OwnershipState.Unknown:
				return new BodyCardShape { badgeKey = "dlc.badge", reasonKey = "dlc.unverified", storeButton = true };
			default:
				Debug.LogError(string.Format("[MainMenuUI] неизвестное состояние владения {0}; ", f.ownership) + "BODY закрыт как непроверенный.");
				return new BodyCardShape { badgeKey = "dlc.badge", reasonKey = "dlc.unverified", storeButton = true };
		}
	}

	private Button MakeBodyCard(Transform parent)
	{
		return MakeBodyCard(parent, ReadBodyFacts());
	}

	internal Button MakeBodyCard(Transform parent, BodyFacts facts)
	{
		BodyCardShape shape = DescribeBodyCard(facts);
		RectTransform card = MenuTheme.MakeRect(parent, "Mode_Body");
		Image background = card.gameObject.AddComponent<Image>();
		background.color = MenuTheme.SurfaceAlt;
		background.raycastTarget = true;
		Button button = card.gameObject.AddComponent<Button>();
		button.targetGraphic = background;
		MenuTheme.ApplyColors(button);
		button.interactable = shape.enterable;
		button.onClick.AddListener(SelectBody);
		LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = 104f;
		layout.preferredHeight = 104f;
		RectTransform accent = MenuTheme.MakeRect(card, "Accent");
		accent.anchorMin = Vector2.zero;
		accent.anchorMax = new Vector2(0f, 1f);
		accent.pivot = new Vector2(0f, 0.5f);
		accent.offsetMin = Vector2.zero;
		accent.offsetMax = new Vector2(4f, 0f);
		MenuTheme.AddImage(accent, MenuTheme.Line);
		TextMeshProUGUI title = MenuTheme.MakeLabel(card, "Title", "mode.body", 24f, FontStyles.Bold, shape.enterable ? MenuTheme.TextPrimary : MenuTheme.TextLocked, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -18f), new Vector2(700f, 30f));
		title.characterSpacing = 4f;
		TextMeshProUGUI desc = MenuTheme.MakeLabel(card, "Desc", "mode.body_desc", 16f, FontStyles.Normal, shape.enterable ? MenuTheme.TextMuted : MenuTheme.TextLocked, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -54f), new Vector2(1000f, 44f));
		if (shape.badgeKey != null)
		{
			TextMeshProUGUI badge = MenuTheme.MakeLabel(card, "Badge", shape.badgeKey, 15f, FontStyles.Bold, MenuTheme.TextMuted, TextAlignmentOptions.Right);
			MenuTheme.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(200f, 24f));
			badge.characterSpacing = 6f;
		}
		if (shape.reasonKey != null)
		{
			TextMeshProUGUI reason = MenuTheme.MakeLabel(card, "Reason", shape.reasonKey, 14f, FontStyles.Normal, MenuTheme.TextLocked, TextAlignmentOptions.TopLeft);
			MenuTheme.Place(reason.rectTransform, Vector2.zero, Vector2.zero, new Vector2(26f, 14f), new Vector2(1000f, 20f));
		}
		if (shape.storeButton)
		{
			MakeBodyButton(card, "Body_Store", "dlc.open_store", -24f, delegate { OpenUrl(bodyStoreUrl); });
		}
		if (shape.demoButton)
		{
			MakeBodyButton(card, "Body_Demo", "dlc.show_demo", -236f, null);
		}
		return button;
	}

	private static AeroFacts ReadAeroFacts()
	{
		return AeroFreeAccess.ReadFacts();
	}

	private Button MakeAeroCard(Transform parent)
	{
		AeroFacts facts = ReadAeroFacts();
		Button card = MakeAeroCard(parent, facts);
		card.gameObject.AddComponent<AeroAccessRefresh>().Configure(facts.freeAccess, RefreshAeroAccess);
		return card;
	}

	private void RefreshAeroAccess()
	{
		if (_aeroCard == null)
		{
			return;
		}
		Button old = _aeroCard;
		Transform parent = old.transform.parent;
		int sibling = old.transform.GetSiblingIndex();
		old.gameObject.SetActive(false);
		old.transform.SetParent(null, false);
		_aeroCard = MakeAeroCard(parent);
		_aeroCard.transform.SetSiblingIndex(sibling);
		if (!Application.isPlaying)
		{
			UnityEngine.Object.DestroyImmediate(old.gameObject);
		}
		else
		{
			UnityEngine.Object.Destroy(old.gameObject);
		}
		ScrollRect scroll = parent.GetComponentInParent<ScrollRect>();
		if (scroll != null)
		{
			scroll.GetComponent<AeroMenuScroll>().Configure(scroll);
		}
		RefreshModeCards();
	}

	internal Button MakeAeroCard(Transform parent, AeroFacts facts)
	{
		return AeroCard.Build(parent, facts, SelectAero, delegate { OpenUrl(aeroStoreUrl); }, delegate { Debug.LogWarning("[MainMenuUI] Aero demo is not available in this build."); });
	}

	private void SelectAero()
	{
		if (_aeroCard != null && _aeroCard.interactable)
		{
			_pickedFamily = SimFamily.Aero;
			RefreshModeCards();
		}
	}

	private void StartAero()
	{
		AeroFacts facts = ReadAeroFacts();
		AeroEntryAction action = AeroRoute.Decide(SimFamily.Aero, facts);
		if (action == AeroEntryAction.BlockedWithReason)
		{
			AeroCardShape shape = AeroCard.Describe(facts);
			Debug.Log("[MainMenuUI] AeroSim: " + Localization.T(shape.reasonKey ?? shape.badgeKey));
			return;
		}
		if (action == AeroEntryAction.OpenAeroEditor)
		{
			AeroSceneEntry.Open();
			return;
		}
		if (action == AeroEntryAction.OpenAeroDemo)
		{
			Debug.LogWarning("[MainMenuUI] Aero demo is not available in this build.");
			return;
		}
		Debug.LogError(string.Format("[MainMenuUI] Unknown Aero entry action {0}; entry refused.", action));
	}

	private static RadarFacts ReadRadarFacts()
	{
		return RadarFreeAccess.ReadFacts();
	}

	private Button MakeRadarCard(Transform parent)
	{
		return MakeRadarCard(parent, ReadRadarFacts());
	}

	internal Button MakeRadarCard(Transform parent, RadarFacts facts)
	{
		return RadarCard.Build(parent, facts, SelectRadar, delegate { OpenUrl(radarStoreUrl); }, delegate { Debug.LogWarning("[MainMenuUI] Radar demo is not available in this build."); });
	}

	private void SelectRadar()
	{
		if (_radarCard != null && _radarCard.interactable)
		{
			_pickedFamily = SimFamily.Radar;
			RefreshModeCards();
		}
	}

	private void StartRadar()
	{
		RadarFacts facts = ReadRadarFacts();
		RadarEntryAction action = RadarRoute.Decide(SimFamily.Radar, facts);
		if (action == RadarEntryAction.BlockedWithReason)
		{
			RadarCardShape shape = RadarCard.Describe(facts);
			Debug.Log("[MainMenuUI] RadarSim: " + Localization.T(shape.reasonKey ?? shape.badgeKey));
			return;
		}
		if (action == RadarEntryAction.OpenRadarBench)
		{
			RadarSceneEntry.Open();
			return;
		}
		if (action == RadarEntryAction.OpenRadarDemo)
		{
			Debug.LogWarning("[MainMenuUI] Radar demo is not available in this build.");
			return;
		}
		Debug.LogError(string.Format("[MainMenuUI] Unknown Radar entry action {0}; entry refused.", action));
	}

	private Button MakeBodyButton(RectTransform card, string name, string key, float rightX, UnityAction onClick)
	{
		Button button = MenuTheme.MakeButton(card, name, key, 52f, null, 18f, TextAlignmentOptions.Center, 10f);
		if (onClick != null)
		{
			button.onClick.AddListener(onClick);
		}
		MenuTheme.Place(button.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(rightX, 0f), new Vector2(200f, 52f));
		return button;
	}

	private void SelectBody()
	{
		if (_bodyCard != null && _bodyCard.interactable)
		{
			_pickedFamily = SimFamily.Body;
			RefreshModeCards();
		}
	}

	private void SelectFlat()
	{
		if (_flatCard != null && _flatCard.interactable)
		{
			_pickedFamily = SimFamily.Flat2D;
			RefreshModeCards();
		}
	}

	private void SelectFlatQuality(FlatQuality quality)
	{
		if (_flatCard != null && _flatCard.interactable)
		{
			_pickedFamily = SimFamily.Flat2D;
			_pickedQuality = quality;
			RefreshModeCards();
		}
	}

	private void SelectMode(SimMode mode)
	{
		if (!GameSettings.IsUnlocked(mode))
		{
			return;
		}
		string reasonKey;
		if (mode == SimMode.Ultra && !ContinuumRouting.IsUltraUnlocked(null, out reasonKey))
		{
			return;
		}
		_pickedFamily = SimFamily.Analytic3D;
		_pickedMode = mode;
		RefreshModeCards();
	}

	private void SelectSection(SimMode mode, TargetSection section)
	{
		if (!GameSettings.IsUnlocked(mode))
		{
			return;
		}
		_pickedFamily = SimFamily.Analytic3D;
		_pickedMode = mode;
		_pickedSection = section;
		RefreshModeCards();
	}

	private void RefreshModeCards()
	{
		SimFamily family = _pickedFamily;
		foreach (KeyValuePair<SimMode, Button> pair in _modeCards)
		{
			bool selected = family == SimFamily.Analytic3D && pair.Key == _pickedMode && GameSettings.IsUnlocked(pair.Key);
			Image image = pair.Value.GetComponent<Image>();
			if (image != null)
			{
				image.color = selected ? MenuTheme.Selected : MenuTheme.SurfaceAlt;
			}
			Image accent = MenuTheme.AccentOf(pair.Value);
			if (accent != null)
			{
				accent.color = selected ? MenuTheme.TextPrimary : (GameSettings.IsUnlocked(pair.Key) ? MenuTheme.Line : MenuTheme.SurfaceAlt);
			}
		}
		foreach (KeyValuePair<SimMode, Button> pair in _hullButtons)
		{
			MenuTheme.SetSelectedVisual(pair.Value, family == SimFamily.Analytic3D && pair.Key == _pickedMode && _pickedSection == TargetSection.Hull);
		}
		foreach (KeyValuePair<SimMode, Button> pair in _roofButtons)
		{
			MenuTheme.SetSelectedVisual(pair.Value, family == SimFamily.Analytic3D && pair.Key == _pickedMode && _pickedSection == TargetSection.Roof);
		}
		family = _pickedFamily;
		if (_flatCard != null)
		{
			Image image = _flatCard.GetComponent<Image>();
			if (image != null)
			{
				image.color = family == SimFamily.Flat2D ? MenuTheme.Selected : MenuTheme.SurfaceAlt;
			}
			Image accent = MenuTheme.AccentOf(_flatCard);
			if (accent != null)
			{
				accent.color = family == SimFamily.Flat2D ? MenuTheme.TextPrimary : (_flatCard.interactable ? MenuTheme.Line : MenuTheme.SurfaceAlt);
			}
		}
		if (_flatCoarse != null)
		{
			MenuTheme.SetSelectedVisual(_flatCoarse, family == SimFamily.Flat2D && _pickedQuality == FlatQuality.Coarse);
		}
		if (_flatFine != null)
		{
			MenuTheme.SetSelectedVisual(_flatFine, family == SimFamily.Flat2D && _pickedQuality == FlatQuality.Fine);
		}
		family = _pickedFamily;
		if (_bodyCard != null)
		{
			Image image = _bodyCard.GetComponent<Image>();
			if (image != null)
			{
				image.color = family == SimFamily.Body ? MenuTheme.Selected : MenuTheme.SurfaceAlt;
			}
			Image accent = MenuTheme.AccentOf(_bodyCard);
			if (accent != null)
			{
				accent.color = family == SimFamily.Body ? MenuTheme.TextPrimary : (_bodyCard.interactable ? MenuTheme.Line : MenuTheme.SurfaceAlt);
			}
		}
		family = _pickedFamily;
		if (_aeroCard != null)
		{
			Image image = _aeroCard.GetComponent<Image>();
			if (image != null)
			{
				image.color = family == SimFamily.Aero ? MenuTheme.Selected : MenuTheme.SurfaceAlt;
			}
			Image accent = MenuTheme.AccentOf(_aeroCard);
			if (accent != null)
			{
				accent.color = family == SimFamily.Aero ? MenuTheme.TextPrimary : (_aeroCard.interactable ? MenuTheme.Line : MenuTheme.SurfaceAlt);
			}
		}
		family = _pickedFamily;
		if (_radarCard != null)
		{
			Image image = _radarCard.GetComponent<Image>();
			if (image != null)
			{
				image.color = family == SimFamily.Radar ? MenuTheme.Selected : MenuTheme.SurfaceAlt;
			}
			Image accent = MenuTheme.AccentOf(_radarCard);
			if (accent != null)
			{
				accent.color = family == SimFamily.Radar ? MenuTheme.TextPrimary : (_radarCard.interactable ? MenuTheme.Line : MenuTheme.SurfaceAlt);
			}
		}
	}

	private void StartSimulation()
	{
		switch (FamilyEntryRoute.Decide(_pickedFamily))
		{
			case FamilyEntry.Refuse:
				return;
			case FamilyEntry.OpenSandbox3D:
				AppFlow.OpenSandbox(sandboxSceneName, _pickedMode, _pickedSection, null);
				return;
			case FamilyEntry.EnterFlat2D:
				StartFlat2D();
				return;
			case FamilyEntry.EnterBody:
				StartBody();
				return;
			case FamilyEntry.EnterAero:
				StartAero();
				return;
			case FamilyEntry.EnterRadar:
				StartRadar();
				return;
			default:
				Debug.LogError("[MainMenuUI] неизвестное решение о входе; вход отклонён.");
				return;
		}
	}

	private void StartBody()
	{
		BodyFacts facts = ReadBodyFacts();
		BodyEntryAction action = BodyRoute.Decide(SimFamily.Body, facts.implemented, facts.ownership, facts.gateOpen, facts.demoAvailable);
		if (action == BodyEntryAction.BlockedWithReason)
		{
			string reasonKey = BodyRoute.ReasonKeyFor(facts.implemented, facts.ownership, facts.gateOpen, facts.demoAvailable, facts.gateReason);
			Debug.Log("[MainMenuUI] BODY недоступен: " + (reasonKey == null ? "причина не названа" : Localization.T(reasonKey)));
			return;
		}
		if (action == BodyEntryAction.OpenBodyEditor)
		{
			Debug.LogWarning("[MainMenuUI] BODY: маршрут пускает, но сцены и решателя в этой сборке нет — открывать нечего.");
			return;
		}
		if (action == BodyEntryAction.OpenBodyDemo)
		{
			Debug.LogWarning("[MainMenuUI] BODY: запись пробного среза ещё не сделана.");
			return;
		}
		Debug.LogError(string.Format("[MainMenuUI] неизвестное решение маршрута BODY ({0}); ", action) + "вход отклонён.");
	}

	private void StartFlat2D()
	{
		StartFlat2D(null);
	}

	private void BuildFlatAlphaPanel()
	{
		RectTransform body;
		_panelFlatAlpha = MakeOverlayPanel("Panel_FlatAlpha", "flat.alpha.title", out body);
		TextMeshProUGUI text = MenuTheme.MakeLabel(body, "AlphaBody", "flat.alpha.body", 19f, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1180f, 260f));
		text.textWrappingMode = TextWrappingModes.Normal;
		text.lineSpacing = 6f;
		Button enter = MenuTheme.MakeButton(body, "BtnAlphaEnter", "flat.alpha.enter", 56f, ConfirmFlatAlpha, 20f, TextAlignmentOptions.Center, 16f);
		MenuTheme.Place(enter.transform as RectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, 24f), new Vector2(420f, 56f));
		Transform backTransform = _panelFlatAlpha.transform.Find("BtnBack");
		Button back = backTransform != null ? backTransform.GetComponent<Button>() : null;
		if (back == null)
		{
			Debug.LogError("[MainMenuUI] у панели предупреждения 2D не нашлась кнопка BtnBack — «назад» уведёт на главный экран вместо возврата к списку.");
			return;
		}
		back.onClick.RemoveAllListeners();
		back.onClick.AddListener(CancelFlatAlpha);
	}

	private void ShowFlatAlpha(SaveEntry entry)
	{
		_flatAlphaPending = entry;
		_flatAlphaReturn = null;
		foreach (GameObject panel in _panels)
		{
			if (panel != null && panel.activeSelf)
			{
				_flatAlphaReturn = panel;
				break;
			}
		}
		ShowPanel(_panelFlatAlpha);
	}

	private void CancelFlatAlpha()
	{
		_flatAlphaPending = null;
		ShowPanel(_flatAlphaReturn);
		_flatAlphaReturn = null;
	}

	private void ConfirmFlatAlpha()
	{
		SaveEntry entry = _flatAlphaPending;
		_flatAlphaPending = null;
		_flatAlphaReturn = null;
		FlatFacts facts = ReadFlatFacts();
		FlatEntryAction action = Flat2DRoute.Decide(SimFamily.Flat2D, facts.implemented, facts.ownership, facts.gateOpen, facts.demoAvailable, facts.freeAccess);
		if (action == FlatEntryAction.OpenFlatEditor)
		{
			AppFlow.OpenFlat2D(entry, _pickedQuality);
			return;
		}
		Debug.LogWarning("[MainMenuUI] вход в «Плоскость» отменён после предупреждения: маршрут больше не пускает (" + action + ").");
		ShowPanel(null);
	}

	private void StartFlat2D(SaveEntry entry)
	{
		FlatFacts facts = ReadFlatFacts();
		string suffix = entry == null ? string.Empty : " (сохранение «" + entry.displayName + "»)";
		FlatEntryAction action = Flat2DRoute.Decide(SimFamily.Flat2D, facts.implemented, facts.ownership, facts.gateOpen, facts.demoAvailable, facts.freeAccess);
		if (action == FlatEntryAction.BlockedWithReason)
		{
			FlatCardShape shape = DescribeFlatCard(facts);
			string reasonKey = shape.reasonKey ?? shape.badgeKey;
			string reason = reasonKey == null ? "причина не названа" : Localization.T(reasonKey);
			Debug.Log("[MainMenuUI] «Плоскость» недоступна: " + reason + suffix);
			return;
		}
		if (action == FlatEntryAction.OpenFlatEditor)
		{
			ShowFlatAlpha(entry);
			return;
		}
		if (action == FlatEntryAction.OpenFlatDemo)
		{
			Debug.LogWarning("[MainMenuUI] «Плоскость»: запись пробного среза ещё не сделана." + suffix);
			return;
		}
		Debug.LogError(string.Format("[MainMenuUI] неизвестное решение маршрута 2D ({0}); вход отклонён.", action));
	}

	private void BuildSavesPanel()
	{
		RectTransform body;
		_panelSaves = MakeOverlayPanel("Panel_Saves", "saves.title", out body);
		RectTransform tabs = MenuTheme.MakeRow(body, "Tabs", 10f, TextAnchor.MiddleLeft);
		tabs.anchorMin = new Vector2(0f, 1f);
		tabs.anchorMax = new Vector2(0f, 1f);
		tabs.pivot = new Vector2(0f, 1f);
		tabs.anchoredPosition = Vector2.zero;
		tabs.sizeDelta = new Vector2(720f, 46f);
		_tabUser = MenuTheme.MakeButton(tabs, "TabUser", "saves.tab_user", 46f, delegate { _showPresets = false; RefreshSavesList(); }, 18f, TextAlignmentOptions.Center, 12f);
		_tabUser.gameObject.AddComponent<LayoutElement>().preferredWidth = 300f;
		_tabPresets = MenuTheme.MakeButton(tabs, "TabPresets", "saves.tab_presets", 46f, delegate { _showPresets = true; RefreshSavesList(); }, 18f, TextAlignmentOptions.Center, 12f);
		_tabPresets.gameObject.AddComponent<LayoutElement>().preferredWidth = 300f;
		ScrollRect scroll = MenuTheme.MakeScrollList(body, "SavesScroll", out _savesContent);
		MenuTheme.Stretch(scroll.transform as RectTransform, new Vector2(0f, 48f), new Vector2(0f, -60f));
		_savesStatus = MenuTheme.MakeLabelRaw(body, "SavesStatus", string.Empty, 15f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(_savesStatus.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(1400f, 44f));
		_savesStatus.textWrappingMode = TextWrappingModes.Normal;
	}

	private void SetSavesStatus(string key, string arg)
	{
		_savesStatusKey = key;
		_savesStatusArg = arg;
		ApplySavesStatus();
	}

	private void ApplySavesStatus()
	{
		if (_savesStatus == null)
		{
			return;
		}
		_savesStatus.text = string.IsNullOrEmpty(_savesStatusKey) ? string.Empty : (_savesStatusArg == null ? Localization.T(_savesStatusKey) : Localization.T(_savesStatusKey, _savesStatusArg));
	}

	private void RefreshSavesList()
	{
		if (_savesContent == null)
		{
			return;
		}
		for (int i = _savesContent.childCount - 1; i >= 0; i--)
		{
			UnityEngine.Object.Destroy(_savesContent.GetChild(i).gameObject);
		}
		MenuTheme.SetSelectedVisual(_tabUser, !_showPresets);
		MenuTheme.SetSelectedVisual(_tabPresets, _showPresets);
		List<SaveEntry> saves = _showPresets ? SaveSystem.LoadPresets() : SaveSystem.LoadUserSaves();
		if (saves.Count == 0)
		{
			TextMeshProUGUI empty = MenuTheme.MakeLabel(_savesContent, "Empty", _showPresets ? "saves.empty_presets" : "saves.empty_user", 18f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
			LayoutElement layout = empty.gameObject.AddComponent<LayoutElement>();
			layout.minHeight = 60f;
			layout.preferredHeight = 60f;
			return;
		}
		foreach (SaveEntry entry in saves)
		{
			MakeSaveRow(entry);
		}
	}

	internal static SaveLoadAction DecideSaveLoad(SimFamily family, FlatFacts facts, out string reasonKey)
	{
		reasonKey = null;
		if (family == SimFamily.Analytic3D)
		{
			return SaveLoadAction.OpenSandbox3D;
		}
		if (family != SimFamily.Flat2D)
		{
			Debug.LogError(string.Format("[MainMenuUI] неизвестное семейство {0} в строке сохранения; ", family) + "загрузка отклонена.");
			reasonKey = "dlc.unverified";
			return SaveLoadAction.Refuse;
		}
		reasonKey = FlatSaveRefusalKey(DescribeFlatState(facts), facts.gateReason);
		return reasonKey == null ? SaveLoadAction.EnterFlat2D : SaveLoadAction.Refuse;
	}

	private void PerformSaveLoad(SaveEntry entry)
	{
		if (entry == null)
		{
			return;
		}
		string reasonKey;
		SaveLoadAction action = DecideSaveLoad(entry.Family, ReadFlatFacts(), out reasonKey);
		if (action == SaveLoadAction.Refuse)
		{
			Debug.Log("[MainMenuUI] сохранение «" + entry.displayName + "» не открывается: " + (reasonKey == null ? "причина не названа" : Localization.T(reasonKey)));
			return;
		}
		if (action == SaveLoadAction.OpenSandbox3D)
		{
			AppFlow.OpenSandbox(sandboxSceneName, GameSettings.DefaultMode, entry.Section, entry);
			return;
		}
		if (action == SaveLoadAction.EnterFlat2D)
		{
			StartFlat2D(entry);
			return;
		}
		Debug.LogError(string.Format("[MainMenuUI] неизвестное решение загрузки ({0}); отклонено.", action));
	}

	internal static string FamilyMetaText(SimFamily family, TargetSection section)
	{
		if (family == SimFamily.Analytic3D)
		{
			return "   ·   " + Localization.T(GameSettings.SectionKey(section));
		}
		if (family == SimFamily.Flat2D)
		{
			return "   ·   " + Localization.T("mode.flat");
		}
		if (family != SimFamily.Body)
		{
			Debug.LogError(string.Format("[MainMenuUI] неизвестное семейство {0} в мете строки ", family) + "сохранения; место оставлено пустым.");
		}
		return string.Empty;
	}

	private void MakeSaveRow(SaveEntry entry)
	{
		MakeSaveRow(_savesContent, entry, ReadFlatFacts());
	}

	internal RectTransform MakeSaveRow(Transform parent, SaveEntry entry, FlatFacts facts)
	{
		SimFamily family = entry.Family;
		string reasonKey;
		SaveLoadAction action = DecideSaveLoad(family, facts, out reasonKey);
		RectTransform row = MenuTheme.MakeRect(parent, "Row_" + entry.id);
		MenuTheme.AddImage(row, MenuTheme.SurfaceAlt);
		LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = reasonKey == null ? 78f : 98f;
		layout.preferredHeight = layout.minHeight;
		TextMeshProUGUI name = MenuTheme.MakeLabelRaw(row, "Name", entry.displayName, 21f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		MenuTheme.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -14f), new Vector2(640f, 26f));
		name.overflowMode = TextOverflowModes.Ellipsis;
		DateTime created = entry.CreatedLocal();
		string meta = created != default(DateTime) ? created.ToString("dd.MM.yyyy  HH:mm") + "   ·   " : string.Empty;
		meta += Localization.T("saves.objects", entry.objectCount) + FamilyMetaText(family, entry.Section);
		TextMeshProUGUI metaLabel = MenuTheme.MakeLabelRaw(row, "Meta", meta, 15f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		MenuTheme.Place(metaLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -44f), new Vector2(640f, 22f));
		SaveEntry captured = entry;
		Button load = MenuTheme.MakeButton(row, "BtnLoad", "saves.load", 40f, delegate { PerformSaveLoad(captured); }, 16f, TextAlignmentOptions.Center, 10f);
		load.interactable = action != SaveLoadAction.Refuse;
		MenuTheme.Place(load.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(entry.isPreset ? -20f : -170f, 0f), new Vector2(150f, 40f));
		if (reasonKey != null)
		{
			TextMeshProUGUI reason = MenuTheme.MakeLabel(row, "LoadReason", reasonKey, 14f, FontStyles.Normal, MenuTheme.TextLocked, TextAlignmentOptions.Left);
			MenuTheme.Place(reason.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -70f), new Vector2(640f, 22f));
		}
		if (entry.isPreset)
		{
			return row;
		}
		Button delete = MenuTheme.MakeButton(row, "BtnDelete", "saves.delete", 40f, delegate
		{
			SaveSystem.Delete(captured.id);
			RefreshSavesList();
		}, 16f, TextAlignmentOptions.Center, 10f);
		MenuTheme.Place(delete.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(140f, 40f));
		Button publish = MenuTheme.MakeButton(row, "BtnPublish", "workshop.publish", 40f, delegate { PerformPublish(captured); }, 16f, TextAlignmentOptions.Center, 10f);
		MenuTheme.Place(publish.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-330f, 0f), new Vector2(170f, 40f));
		return row;
	}

	private void PerformPublish(SaveEntry entry)
	{
		if (entry == null)
		{
			return;
		}
		bool wasLinked = !string.IsNullOrEmpty(WorkshopLinks.ItemFor(entry.id));
		SetSavesStatus("workshop.publishing", entry.displayName);
		string preview = SavePreview.ForPublish(entry.id);
		WorkshopService.Publish(entry, entry.displayName, string.Empty, preview, delegate(WorkshopPublishResult result)
		{
			if (result == null)
			{
				SetSavesStatus("workshop.reject.failed", null);
				return;
			}
			if (!result.ok)
			{
				if (string.IsNullOrEmpty(result.reasonKey))
				{
					Debug.LogError("[MainMenuUI] приёмник мастерской отказал БЕЗ названной причины: " + result.detail);
				}
				SetSavesStatus(string.IsNullOrEmpty(result.reasonKey) ? "workshop.reject.failed" : result.reasonKey, null);
				return;
			}
			if (result.needsLegalAgreement)
			{
				SetSavesStatus("workshop.published_hidden", null);
				return;
			}
			SetSavesStatus(wasLinked ? "workshop.updated" : "workshop.published", entry.displayName);
			if (!wasLinked)
			{
				WorkshopService.Backend.OpenItemPage(result.publishedFileId);
			}
		});
	}

	private void BuildWorkshopPanel()
	{
		RectTransform body;
		_panelWorkshop = MakeOverlayPanel("Panel_Workshop", "workshop.title", out body);
		TextMeshProUGUI hint = MenuTheme.MakeLabel(body, "Hint", "workshop.hint", 16f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1400f, 48f));
		hint.textWrappingMode = TextWrappingModes.Normal;
		RectTransform buttons = MenuTheme.MakeRow(body, "WorkshopButtons", 12f, TextAnchor.MiddleLeft);
		MenuTheme.Place(buttons, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -56f), new Vector2(760f, 46f));
		Button hub = MenuTheme.MakeButton(buttons, "BtnHub", "workshop.open_hub", 46f, delegate { WorkshopService.Backend.OpenHub(); }, 18f, TextAlignmentOptions.Center, 12f);
		hub.gameObject.AddComponent<LayoutElement>().preferredWidth = 320f;
		Button refresh = MenuTheme.MakeButton(buttons, "BtnRefresh", "workshop.refresh", 46f, RequestWorkshopList, 18f, TextAlignmentOptions.Center, 12f);
		refresh.gameObject.AddComponent<LayoutElement>().preferredWidth = 240f;
		ScrollRect scroll = MenuTheme.MakeScrollList(body, "WorkshopScroll", out _workshopContent);
		MenuTheme.Stretch(scroll.transform as RectTransform, new Vector2(0f, 48f), new Vector2(0f, -114f));
		_workshopStatus = MenuTheme.MakeLabelRaw(body, "WorkshopStatus", string.Empty, 15f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(_workshopStatus.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(1400f, 44f));
		_workshopStatus.textWrappingMode = TextWrappingModes.Normal;
	}

	private void SetWorkshopStatus(string key, string arg)
	{
		_workshopStatusKey = key;
		_workshopStatusArg = arg;
		ApplyWorkshopStatus();
	}

	private void ApplyWorkshopStatus()
	{
		if (_workshopStatus == null)
		{
			return;
		}
		_workshopStatus.text = string.IsNullOrEmpty(_workshopStatusKey) ? string.Empty : (_workshopStatusArg == null ? Localization.T(_workshopStatusKey) : Localization.T(_workshopStatusKey, _workshopStatusArg));
	}

	private void RequestWorkshopList()
	{
		WorkshopService.Refresh(OnWorkshopList);
	}

	private void OnWorkshopList(WorkshopListResult result)
	{
		_workshopItems.Clear();
		_workshopListOk = false;
		if (result == null)
		{
			Debug.LogError("[MainMenuUI] приёмник мастерской вернул пустой ответ на список.");
			_workshopStatusKey = "workshop.reject.failed";
			_workshopStatusArg = null;
		}
		else
		{
			_workshopEmptyKey = string.IsNullOrEmpty(result.emptyHintKey) ? "workshop.empty" : result.emptyHintKey;
			_workshopWhere = result.where ?? string.Empty;
			if (result.ok)
			{
				_workshopListOk = true;
				int skipped = 0;
				if (result.items != null)
				{
					foreach (WorkshopItem item in result.items)
					{
						if (item == null)
						{
							skipped++;
						}
						else
						{
							_workshopItems.Add(item);
						}
					}
				}
				if (skipped > 0)
				{
					Debug.LogWarning(string.Format("[MainMenuUI] приёмник мастерской вернул {0} пустых записей — ", skipped) + "они отброшены, список неполон.");
				}
				_workshopStatusKey = "workshop.where";
				_workshopStatusArg = result.where;
			}
			else
			{
				_workshopStatusKey = string.IsNullOrEmpty(result.reasonKey) ? "workshop.reject.failed" : result.reasonKey;
				_workshopStatusArg = null;
			}
		}
		ApplyWorkshopStatus();
		RefreshWorkshopRows();
	}

	private void OnWorkshopItemsChanged()
	{
		if (_panelWorkshop != null && _panelWorkshop.activeSelf)
		{
			RequestWorkshopList();
		}
	}

	private void RefreshWorkshopRows()
	{
		if (_workshopContent == null)
		{
			return;
		}
		for (int i = _workshopContent.childCount - 1; i >= 0; i--)
		{
			UnityEngine.Object.Destroy(_workshopContent.GetChild(i).gameObject);
		}
		if (_workshopItems.Count == 0)
		{
			if (!_workshopListOk)
			{
				return;
			}
			TextMeshProUGUI empty = MenuTheme.MakeLabelRaw(_workshopContent, "Empty", Localization.T(_workshopEmptyKey, _workshopWhere), 18f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
			empty.textWrappingMode = TextWrappingModes.Normal;
			LayoutElement layout = empty.gameObject.AddComponent<LayoutElement>();
			layout.minHeight = 80f;
			layout.preferredHeight = 80f;
			return;
		}
		FlatFacts facts = ReadFlatFacts();
		foreach (WorkshopItem item in _workshopItems)
		{
			MakeWorkshopRow(_workshopContent, item, facts);
		}
	}

	internal RectTransform MakeWorkshopRow(Transform parent, WorkshopItem item, FlatFacts facts)
	{
		string importReason;
		WorkshopImportAction importAction = WorkshopService.DecideImport(item, out importReason);
		SaveLoadAction loadAction = SaveLoadAction.Refuse;
		string loadReason = null;
		if (importAction == WorkshopImportAction.Import)
		{
			loadAction = DecideSaveLoad(item.manifest.Family, facts, out loadReason);
		}
		string reasonKey = importReason ?? loadReason;
		RectTransform row = MenuTheme.MakeRect(parent, "Item_" + item.publishedFileId);
		MenuTheme.AddImage(row, MenuTheme.SurfaceAlt);
		LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = reasonKey == null ? 78f : 98f;
		layout.preferredHeight = layout.minHeight;
		TextMeshProUGUI name = MenuTheme.MakeLabelRaw(row, "Name", item.Title, 21f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		MenuTheme.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -14f), new Vector2(640f, 26f));
		name.overflowMode = TextOverflowModes.Ellipsis;
		string meta;
		if (item.manifest == null)
		{
			meta = item.downloading ? Localization.T("workshop.state.downloading") : string.Empty;
		}
		else
		{
			meta = Localization.T("saves.objects", item.manifest.objectCount) + FamilyMetaText(item.manifest.Family, item.manifest.Section);
			if (!string.IsNullOrEmpty(item.manifest.authorName))
			{
				meta += "   ·   " + Localization.T("workshop.author", item.manifest.authorName);
			}
		}
		TextMeshProUGUI metaLabel = MenuTheme.MakeLabelRaw(row, "Meta", meta, 15f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		MenuTheme.Place(metaLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -44f), new Vector2(640f, 22f));
		if (reasonKey != null)
		{
			TextMeshProUGUI reason = MenuTheme.MakeLabel(row, "ItemReason", reasonKey, 14f, FontStyles.Normal, MenuTheme.TextLocked, TextAlignmentOptions.Left);
			MenuTheme.Place(reason.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -70f), new Vector2(640f, 22f));
		}
		WorkshopItem captured = item;
		Button load = MenuTheme.MakeButton(row, "BtnLoad", "saves.load", 40f, delegate { PerformWorkshopLoad(captured); }, 16f, TextAlignmentOptions.Center, 10f);
		load.interactable = importAction == WorkshopImportAction.Import && loadAction != SaveLoadAction.Refuse;
		MenuTheme.Place(load.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-410f, 0f), new Vector2(150f, 40f));
		Button import = MenuTheme.MakeButton(row, "BtnImport", "workshop.import", 40f, delegate { PerformWorkshopImport(captured); }, 16f, TextAlignmentOptions.Center, 10f);
		import.interactable = importAction == WorkshopImportAction.Import;
		MenuTheme.Place(import.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-170f, 0f), new Vector2(230f, 40f));
		Button page = MenuTheme.MakeButton(row, "BtnPage", "workshop.page", 40f, delegate { WorkshopService.Backend.OpenItemPage(captured.publishedFileId); }, 16f, TextAlignmentOptions.Center, 10f);
		page.interactable = !string.IsNullOrEmpty(item.publishedFileId);
		MenuTheme.Place(page.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(140f, 40f));
		return row;
	}

	private void PerformWorkshopLoad(WorkshopItem item)
	{
		string reasonKey;
		string detail;
		SaveEntry entry = WorkshopService.ReadEntry(item, out reasonKey, out detail);
		if (entry != null)
		{
			PerformSaveLoad(entry);
			return;
		}
		SetWorkshopStatus(string.IsNullOrEmpty(reasonKey) ? "workshop.reject.malformed" : reasonKey, null);
		RequestWorkshopList();
	}

	private void PerformWorkshopImport(WorkshopItem item)
	{
		string reasonKey;
		string detail;
		SaveEntry entry = WorkshopService.Import(item, out reasonKey, out detail);
		if (entry == null)
		{
			SetWorkshopStatus(string.IsNullOrEmpty(reasonKey) ? "workshop.reject.malformed" : reasonKey, null);
			return;
		}
		SetWorkshopStatus("workshop.imported", entry.displayName);
	}

	private void BuildAboutPanel()
	{
		RectTransform body;
		_panelAbout = MakeOverlayPanel("Panel_About", "about.title", out body);
		TextMeshProUGUI studio = MenuTheme.MakeLabelRaw(body, "Studio", studioName, 30f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(studio.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(900f, 40f));
		studio.characterSpacing = 8f;
		TextMeshProUGUI lead = MenuTheme.MakeLabelRaw(body, "Lead", leadName, 22f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(lead.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -50f), new Vector2(900f, 30f));
		TextMeshProUGUI role = MenuTheme.MakeLabel(body, "Role", "about.role_lead", 16f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(role.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -82f), new Vector2(900f, 24f));
		RectTransform content;
		ScrollRect scroll = MenuTheme.MakeScrollList(body, "AboutScroll", out content);
		MenuTheme.Place(scroll.transform as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -126f), new Vector2(1190f, 300f));
		TextMeshProUGUI text = MenuTheme.MakeLabel(content, "Body", "about.body", 18f, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		text.lineSpacing = 6f;
		text.textWrappingMode = TextWrappingModes.Normal;
		text.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		TextMeshProUGUI contacts = MenuTheme.MakeLabel(body, "ContactsLabel", "about.contacts", 16f, FontStyles.Bold, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(contacts.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, 74f), new Vector2(400f, 24f));
		contacts.characterSpacing = 6f;
		RectTransform social = MenuTheme.MakeRow(body, "AboutSocial", 14f, TextAnchor.MiddleLeft);
		MenuTheme.Place(social, Vector2.zero, Vector2.zero, new Vector2(0f, 8f), new Vector2(600f, 48f));
		Button telegram = MenuTheme.MakeButtonRaw(social, "AboutTg", "TELEGRAM", 48f, delegate { OpenUrl(telegramUrl); }, 18f, TextAlignmentOptions.Center, 10f);
		telegram.gameObject.AddComponent<LayoutElement>().preferredWidth = 220f;
		Button discord = MenuTheme.MakeButtonRaw(social, "AboutDc", "DISCORD", 48f, delegate { OpenUrl(discordUrl); }, 18f, TextAlignmentOptions.Center, 10f);
		discord.gameObject.AddComponent<LayoutElement>().preferredWidth = 220f;
	}

	private void BuildSettingsPanel()
	{
		RectTransform body;
		_panelSettings = MakeOverlayPanel("Panel_Settings", "settings.title", out body);
		RectTransform content;
		ScrollRect scroll = MenuTheme.MakeScrollList(body, "SettingsScroll", out content);
		MenuTheme.Stretch(scroll.transform as RectTransform);
		RectTransform rows = MenuTheme.MakeRect(content, "SettingsRows");
		LayoutElement rowsLayout = rows.gameObject.AddComponent<LayoutElement>();
		rowsLayout.minHeight = 1060f;
		rowsLayout.preferredHeight = 1060f;
		TextMeshProUGUI langLabel = MenuTheme.MakeLabel(rows, "LangLabel", "settings.language", 20f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(langLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(900f, 28f));
		langLabel.characterSpacing = 4f;
		RectTransform langRow = MenuTheme.MakeRow(rows, "LangRow", 10f, TextAnchor.MiddleLeft);
		langRow.anchorMin = new Vector2(0f, 1f);
		langRow.anchorMax = new Vector2(1f, 1f);
		langRow.pivot = new Vector2(0f, 1f);
		langRow.anchoredPosition = new Vector2(0f, -42f);
		langRow.sizeDelta = new Vector2(0f, 54f);
		_langButtons.Clear();
		for (int i = 0; i < Localization.All.Length; i++)
		{
			Language captured = Localization.All[i];
			string nativeName;
			switch (captured)
			{
				case Language.RU:
					nativeName = "Русский";
					break;
				case Language.EN:
					nativeName = "English";
					break;
				case Language.TT:
					nativeName = "Татарча";
					break;
				case Language.DE:
					nativeName = "Deutsch";
					break;
				case Language.PL:
					nativeName = "Polski";
					break;
				default:
					nativeName = captured.ToString();
					break;
			}
			Button button = MenuTheme.MakeButtonRaw(langRow, "Lang_" + captured, nativeName, 54f, delegate { Localization.SetLanguage(captured); }, 19f, TextAlignmentOptions.Center, 10f);
			button.gameObject.AddComponent<LayoutElement>().preferredWidth = 200f;
			_langButtons[captured] = button;
		}
		TextMeshProUGUI modeLabel = MenuTheme.MakeLabel(rows, "ModeLabel", "settings.default_mode", 20f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(modeLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -130f), new Vector2(900f, 28f));
		modeLabel.characterSpacing = 4f;
		TextMeshProUGUI modeHint = MenuTheme.MakeLabel(rows, "ModeHint", "settings.default_mode_hint", 16f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(modeHint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -162f), new Vector2(1100f, 24f));
		RectTransform modeRow = MenuTheme.MakeRow(rows, "DefaultModeRow", 10f, TextAnchor.MiddleLeft);
		modeRow.anchorMin = new Vector2(0f, 1f);
		modeRow.anchorMax = new Vector2(1f, 1f);
		modeRow.pivot = new Vector2(0f, 1f);
		modeRow.anchoredPosition = new Vector2(0f, -196f);
		modeRow.sizeDelta = new Vector2(0f, 52f);
		_defaultModeButtons.Clear();
		for (int i = 0; i < GameSettings.AllModes.Length; i++)
		{
			SimMode captured = GameSettings.AllModes[i];
			Button button = MenuTheme.MakeButton(modeRow, "Default_" + captured, GameSettings.TitleKey(captured), 52f, delegate
			{
				GameSettings.SetDefaultMode(captured);
				RefreshDefaultModeButtons();
			}, 17f, TextAlignmentOptions.Center, 10f);
			button.gameObject.AddComponent<LayoutElement>().preferredWidth = 260f;
			if (!GameSettings.IsUnlocked(captured))
			{
				MenuTheme.SetLocked(button, true, null);
			}
			_defaultModeButtons[captured] = button;
		}
		ApplyUltraVisibility();
		TextMeshProUGUI displayLabel = MenuTheme.MakeLabel(rows, "DisplayLabel", "settings.display", 20f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(displayLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -276f), new Vector2(900f, 28f));
		displayLabel.characterSpacing = 4f;
		TextMeshProUGUI displayHint = MenuTheme.MakeLabel(rows, "DisplayHint", "settings.display_hint", 16f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(displayHint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -308f), new Vector2(1100f, 24f));
		_toggleRows.Clear();
		MakeToggleRow(rows, "ResultTint", "settings.result_tint", "settings.result_tint_hint", -344f, () => GameSettings.ResultTint, GameSettings.SetResultTint);
		MakeToggleRow(rows, "TracePath", "settings.trace_path", "settings.trace_path_hint", -408f, () => GameSettings.TracePath, GameSettings.SetTracePath);
		MakeToggleRow(rows, "Textures", "settings.textures", "settings.textures_hint", -472f, () => GameSettings.Textures, GameSettings.SetTextures);
		MakeToggleRow(rows, "CrewOrgans", "organs", "organs_hint", -536f, () => GameSettings.CrewOrgans, GameSettings.SetCrewOrgans, true);
		MakeToggleRow(rows, "CrewDamageHighlight", "highlight_title", "highlight_hint", -600f, () => GameSettings.CrewDamageHighlight, GameSettings.SetCrewDamageHighlight, true);
		TextMeshProUGUI limitsLabel = MenuTheme.MakeLabel(rows, "LimitsLabel", "settings.limits", 20f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(limitsLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -664f), new Vector2(900f, 28f));
		limitsLabel.characterSpacing = 4f;
		MakeToggleRow(rows, "ExtremeTilt", "settings.extreme_tilt", "settings.extreme_tilt_hint", -706f, () => GameSettings.AllowExtremeTilt, GameSettings.SetAllowExtremeTilt);
		TextMeshProUGUI soundLabel = MenuTheme.MakeLabel(rows, "SoundLabel", "settings.sound", 20f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(soundLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -788f), new Vector2(900f, 28f));
		soundLabel.characterSpacing = 4f;
		MakeToggleRow(rows, "Music", "settings.music", "settings.music_hint", -830f, () => GameSettings.Music, GameSettings.SetMusic);
		TextMeshProUGUI controlsLabel = MenuTheme.MakeLabel(rows, "ControlsLabel", "settings.controls", 20f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(controlsLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -910f), new Vector2(900f, 28f));
		controlsLabel.characterSpacing = 4f;
		TextMeshProUGUI controlsHint = MenuTheme.MakeLabel(rows, "ControlsHint", "settings.controls_hint", 16f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(controlsHint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -942f), new Vector2(1100f, 24f));
		RectTransform controlsRow = MenuTheme.MakeRow(rows, "ControlsRow", 10f, TextAnchor.MiddleLeft);
		controlsRow.anchorMin = new Vector2(0f, 1f);
		controlsRow.anchorMax = new Vector2(1f, 1f);
		controlsRow.pivot = new Vector2(0f, 1f);
		controlsRow.anchoredPosition = new Vector2(0f, -976f);
		controlsRow.sizeDelta = new Vector2(0f, 52f);
		for (int i = 0; i < ControlBindings.AllScopes.Length; i++)
		{
			ControlScope captured = ControlBindings.AllScopes[i];
			Button button = MenuTheme.MakeButton(controlsRow, "Controls_" + captured, ControlBindings.TitleKey(captured), 52f, delegate { ShowControls(captured); }, 17f, TextAlignmentOptions.Center, 10f);
			button.gameObject.AddComponent<LayoutElement>().preferredWidth = 360f;
		}
		RefreshLanguageButtons();
		RefreshDefaultModeButtons();
		RefreshToggleRows();
	}

	private void MakeToggleRow(RectTransform body, string name, string titleKey, string hintKey, float y, Func<bool> getter, Action<bool> setter, bool crewText = false)
	{
		TextMeshProUGUI title;
		if (crewText)
		{
			title = MenuTheme.MakeLabelRaw(body, name + "Label", CrewPanelText.T(titleKey), 18f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
			CrewPanelText.Bind(title, titleKey);
		}
		else
		{
			title = MenuTheme.MakeLabel(body, name + "Label", titleKey, 18f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		}
		MenuTheme.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(700f, 24f));
		TextMeshProUGUI hint;
		if (crewText)
		{
			hint = MenuTheme.MakeLabelRaw(body, name + "Hint", CrewPanelText.T(hintKey), 14f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
			CrewPanelText.Bind(hint, hintKey);
		}
		else
		{
			hint = MenuTheme.MakeLabel(body, name + "Hint", hintKey, 14f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		}
		MenuTheme.Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y - 24f), new Vector2(900f, 22f));
		ToggleRow row = new ToggleRow { getter = getter };
		row.on = MenuTheme.MakeButton(body, name + "_On", "settings.on", 46f, delegate
		{
			setter(true);
			RefreshToggleRows();
		}, 17f, TextAlignmentOptions.Center, 10f);
		MenuTheme.Place(row.on.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, y), new Vector2(140f, 46f));
		row.off = MenuTheme.MakeButton(body, name + "_Off", "settings.off", 46f, delegate
		{
			setter(false);
			RefreshToggleRows();
		}, 17f, TextAlignmentOptions.Center, 10f);
		MenuTheme.Place(row.off.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(140f, 46f));
		_toggleRows.Add(row);
	}

	private void RefreshToggleRows()
	{
		foreach (ToggleRow row in _toggleRows)
		{
			bool value = row.getter();
			MenuTheme.SetSelectedVisual(row.on, value);
			MenuTheme.SetSelectedVisual(row.off, !value);
		}
	}

	private void RefreshLanguageButtons()
	{
		foreach (KeyValuePair<Language, Button> pair in _langButtons)
		{
			MenuTheme.SetSelectedVisual(pair.Value, pair.Key == Localization.Current);
		}
	}

	private void RefreshDefaultModeButtons()
	{
		foreach (KeyValuePair<SimMode, Button> pair in _defaultModeButtons)
		{
			MenuTheme.SetSelectedVisual(pair.Value, pair.Key == GameSettings.DefaultMode);
		}
	}
}
