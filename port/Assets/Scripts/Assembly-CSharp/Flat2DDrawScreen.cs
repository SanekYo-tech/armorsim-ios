using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class Flat2DDrawScreen : MonoBehaviour
{
	private class Section
	{
		public string id;

		public Button header;

		public string title;

		public RectTransform body;

		public CatalogGrid grid;

		public bool open;
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass135_0
	{
		public int idx;

		public Flat2DDrawScreen _003C_003E4__this;

		internal void _003CBuildColourBlock_003Eb__2()
		{
			_003C_003E4__this.PickPaint(idx);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass166_0
	{
		public Flat2DDrawModel.Decal captured;

		public Flat2DDrawScreen _003C_003E4__this;

		internal void _003CBuildDecalBlock_003Eb__2()
		{
			_003C_003E4__this.PickDecal(captured.id);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass166_1
	{
		public Flat2DDrawModel.Preset captured;

		public Flat2DDrawScreen _003C_003E4__this;

		internal void _003CBuildDecalBlock_003Eb__3()
		{
			_003C_003E4__this.LoadPreset(captured.id);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass217_0
	{
		public SaveEntry e;

		public Flat2DDrawScreen _003C_003E4__this;

		internal void _003CEscLoad_003Eb__0()
		{
			_003C_003E4__this.LoadFromEntry(e);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass153_0
	{
		public Flat2DDrawScreen _003C_003E4__this;

		public Flat2DDrawModel.Tool t;

		internal void _003CAddToolBtn_003Eb__0()
		{
			_003C_003E4__this.PickTool(t);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass115_0
	{
		public Flat2DDrawScreen _003C_003E4__this;

		public string id;

		internal void _003CAddSection_003Eb__0()
		{
			_003C_003E4__this.ToggleSection(id);
		}

		internal void _003CAddSection_003Eb__1(string id)
		{
			_003C_003E4__this.PickCatalogue(id);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass126_0
	{
		public int mode;

		public Flat2DDrawScreen _003C_003E4__this;

		internal void _003CBuildFireRow_003Eb__5()
		{
			_003C_003E4__this.SetField(mode);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass289_0
	{
		public float v;

		internal Vector2 _003CSetVelocity_003Eb__0(Vector2 old)
		{
			Vector2 vector = ((old.sqrMagnitude > 1e-12f) ? Vector2.Normalize(old) : Vector2.down);
			return vector * Mathf.Clamp(v, 0f, 8000f);
		}
	}

	[CompilerGenerated]
	private sealed class _003Cget_SectionIds_003Ed__266 : IEnumerable<string>, IEnumerable, IEnumerator<string>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private string _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public Flat2DDrawScreen _003C_003E4__this;

		private List<Section>.Enumerator _003C_003E7__wrap1;

		string IEnumerator<string>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003Cget_SectionIds_003Ed__266(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			if (_003C_003E1__state == -3 || _003C_003E1__state == 1)
			{
				_003C_003Em__Finally1();
			}
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003C_003E7__wrap1 = _003C_003E4__this.sections.GetEnumerator();
				_003C_003E1__state = -3;
				goto IL_0013;
			case 1:
				_003C_003E1__state = -3;
				goto IL_0013;
			}
			IL_0013:
			while (_003C_003E7__wrap1.MoveNext())
			{
				_003C_003E2__current = _003C_003E7__wrap1.Current.id;
				_003C_003E1__state = 1;
				return true;
			}
			_003C_003Em__Finally1();
			_003C_003E7__wrap1 = default(List<Section>.Enumerator);
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			_003C_003E7__wrap1.Dispose();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			if (_003C_003El__initialThreadId == Environment.CurrentManagedThreadId && _003C_003E1__state == -2)
			{
				_003C_003E1__state = 0;
				return this;
			}
			return new _003Cget_SectionIds_003Ed__266(0)
			{
				_003C_003E4__this = _003C_003E4__this
			};
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<string>)this).GetEnumerator();
		}
	}

	public readonly Flat2DDrawModel model;

	public Flat2DRuntimeView view;

	public Flat2DSolver solver;

	public ComputeShader solverShader;

	public const float LeftW = 320f;

	public const float RightW = 360f;

	public const float TopH = 34f;

	public const float ScaleH = 20f;

	public const float RowAH = 46f;

	public const float RowBH = 36f;

	private const float FireH = 120f;

	private ContinuumMaterialId pickedMat;

	private Flat2DFill.Role pickedRole;

	private Flat2DDrawModel.Tool tool;

	private Flat2DDrawModel.Snap snap;

	private bool mirror;

	private Vector2 projectileVel;

	private float brush;

	private int brushShape;

	private Vector2? rubberStart;

	private Vector2? rulerA;

	private Flat2DDrawModel.Shape selectedRef;

	private string heldDecal;

	private float decalAngle;

	private float decalT;

	private float decalH;

	public const float DecalTMin = 0.0005f;

	public const float DecalTMax = 0.1f;

	public const float DecalHMin = 0.002f;

	public const float DecalHMax = 0.4f;

	private Color32 pickedPaint;

	private ContinuumMaterialId matBeforeRound;

	private Flat2DFill.Role roleBeforeRound;

	private string heldRoundName;

	private bool strokeActive;

	private bool suppressNextClick;

	private int touchStrokeFinger = -1;

	private int touchIgnoreFinger = -1;

	private float touchPinchDist = -1f;

	private Vector2 touchPanMid;

	private float lastRotateAt;

	private Flat2DDrawModel.Shape lastRotatedRef;

	private const int SitesMinZoom = 6;

	private bool running;

	private int stepsPerFrame;

	private int totalSteps;

	private int plannedSteps;

	private float fixedDt;

	private double etaSeconds;

	private string etaText;

	public const int PlannedStepsAtCoarse = 4000;

	private int previewVersion;

	private int listVersion;

	public float confirmSeconds;

	public bool MutProbeKeepTrace;

	private int bodiesOnCanvas;

	private const int BodiesCountCeiling = 250000;

	private Vector2 viewCentre;

	private int zoomK;

	private float zoomAccum;

	private const int ZoomMin = 1;

	private const int ZoomMax = 64;

	private Canvas canvas;

	private Camera uiCam;

	private TextMeshProUGUI hud;

	private TextMeshProUGUI hint;

	private TextMeshProUGUI statusLabel;

	private TextMeshProUGUI reasonLabel;

	private TextMeshProUGUI sceneLabel;

	private TextMeshProUGUI brushLabel;

	private TextMeshProUGUI etaLabel;

	private TextMeshProUGUI shapesHint;

	private TextMeshProUGUI rulerLabel;

	private TextMeshProUGUI anchorHint;

	private RectTransform surfaceRect;

	private RectTransform scaleBarRt;

	private TextMeshProUGUI scaleBarLabel;

	private Image countBar;

	private Slider velSlider;

	private Slider brushSlider;

	private TMP_InputField velField;

	private Button runBtn;

	private Button fireBtn;

	private readonly List<Button> fieldButtons;

	private readonly List<Button> toolButtons;

	private readonly List<Button> snapButtons;

	private readonly List<Button> roleButtons;

	private readonly List<Section> sections;

	private readonly Dictionary<string, Flat2DCatalogue.Row> rowsById;

	private GameObject escapeRoot;

	private Button exitBtn;

	private bool exitArmed;

	private TMP_InputField saveNameField;

	private RectTransform loadContent;

	private TextMeshProUGUI escStatus;

	private GameObject confirmRoot;

	private TextMeshProUGUI confirmLabel;

	private readonly List<Button> brushShapeButtons;

	private readonly List<Button> styleButtons;

	private string lastPickedRowId;

	public static readonly Color32[] Palette;

	private Button colourHeader;

	private Image colourChip;

	private RectTransform colourBody;

	private bool colourOpen;

	private readonly List<Outline> swatchMarks;

	private Button mirrorBtn;

	private TextMeshProUGUI decalLabel;

	private Slider decalTSlider;

	private Slider decalHSlider;

	private bool showPinned;

	private Button stabBtn;

	private Button bodiesBtn;

	private Button erosionBtn;

	private Button fractureBtn;

	private Button restraintBtn;

	private float restraintWidthShown;

	private int erodedShown;

	private const int ErodedEveryFrames = 30;

	private Button glowBtn;

	private TextMeshProUGUI glowHint;

	private string glowRefusalShown;

	private RectTransform escNav;

	private RectTransform loadPanel;

	private bool rulerArmed;

	private bool probeWasAlive;

	private bool lastHashAlive;

	private string probeDetail;

	private string lastHashDetail;

	private bool previewFillOk;

	static Flat2DDrawScreen()
	{
		Palette = new Color32[28]
		{
			new Color32(0, 0, 0, byte.MaxValue),
			new Color32(128, 128, 128, byte.MaxValue),
			new Color32(128, 0, 0, byte.MaxValue),
			new Color32(128, 128, 0, byte.MaxValue),
			new Color32(0, 128, 0, byte.MaxValue),
			new Color32(0, 128, 128, byte.MaxValue),
			new Color32(0, 0, 128, byte.MaxValue),
			new Color32(128, 0, 128, byte.MaxValue),
			new Color32(128, 128, 64, byte.MaxValue),
			new Color32(0, 64, 64, byte.MaxValue),
			new Color32(0, 128, 255, byte.MaxValue),
			new Color32(0, 64, 128, byte.MaxValue),
			new Color32(128, 0, 255, byte.MaxValue),
			new Color32(128, 64, 0, byte.MaxValue),
			new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
			new Color32(192, 192, 192, byte.MaxValue),
			new Color32(255, 0, 0, byte.MaxValue),
			new Color32(255, 255, 0, byte.MaxValue),
			new Color32(0, 255, 0, byte.MaxValue),
			new Color32(0, 255, 255, byte.MaxValue),
			new Color32(0, 0, 255, byte.MaxValue),
			new Color32(255, 0, 255, byte.MaxValue),
			new Color32(255, 255, 128, byte.MaxValue),
			new Color32(0, 255, 128, byte.MaxValue),
			new Color32(128, 255, 255, byte.MaxValue),
			new Color32(128, 128, 255, byte.MaxValue),
			new Color32(255, 0, 128, byte.MaxValue),
			new Color32(255, 128, 64, byte.MaxValue)
		};
	}

	public Flat2DDrawScreen()
	{
		model = new Flat2DDrawModel();
		pickedMat = ContinuumMaterialId.RHA_MeyerKleponis;
		snap = Flat2DDrawModel.Snap.Free;
		projectileVel = new Vector2(300f, 0f);
		brush = 0.0025f;
		decalT = 0.02f;
		decalH = 0.1f;
		stepsPerFrame = 4;
		plannedSteps = 4000;
		etaText = "";
		previewVersion = -1;
		listVersion = -2;
		confirmSeconds = 30f;
		bodiesOnCanvas = -1;
		viewCentre = new Vector2(0.04f, 0.03f);
		zoomK = 4;
		zoomAccum = 4f;
		fieldButtons = new List<Button>();
		toolButtons = new List<Button>();
		snapButtons = new List<Button>();
		roleButtons = new List<Button>();
		sections = new List<Section>();
		rowsById = new Dictionary<string, Flat2DCatalogue.Row>();
		brushShapeButtons = new List<Button>();
		styleButtons = new List<Button>();
		swatchMarks = new List<Outline>();
		showPinned = true;
		erodedShown = -1;
		probeDetail = "";
		lastHashDetail = "";
		previewFillOk = true;
	}

	private int SelectedIndex => (selectedRef != null) ? model.shapes.IndexOf(selectedRef) : (-1);

	private bool HoldingRound => Flat2DRounds.IsRoundId(heldDecal);

	public Color32 PickedPaint => pickedPaint;

	public bool ColourPanelOpen => colourOpen;

	public Flat2DFill.Role PickedRole => pickedRole;

	public ContinuumMaterialId PickedMaterial => pickedMat;

	public float DecalThicknessM => decalT;

	public float DecalLengthM => decalH;

	public bool Stabiliser => model.stabiliser;

	public bool BodiesSeparate => model.bodiesSeparate;

	public float RestraintWidthShown => restraintWidthShown;

	public bool Glow => view != null && view.Glow;

	public string StatusText => (statusLabel != null) ? statusLabel.text : "";

	public TextMeshProUGUI HudLabel => hud;

	public bool ConfirmOpen => confirmRoot != null && confirmRoot.activeSelf;

	public Transform ConfirmRoot => (confirmRoot != null) ? confirmRoot.transform : null;

	public bool LoadListOpen => loadPanel != null;

	public Transform LoadPanelRoot => loadPanel;

	public bool EscapeMenuOpen => escapeRoot != null;

	public Transform EscapeRoot => (escapeRoot != null) ? escapeRoot.transform : null;

	public string HeldDecal => heldDecal;

	public float DecalAngle => decalAngle;

	public int BrushShapeNow => brushShape;

	public int BodiesOnCanvas => bodiesOnCanvas;

	public Vector2 ProjectileVelocity => projectileVel;

	public int TotalSteps => totalSteps;

	public int ZoomPxPerDx => zoomK;

	public IEnumerable<string> SectionIds
	{
		[IteratorStateMachine(typeof(_003Cget_SectionIds_003Ed__266))]
		get
		{
			_003Cget_SectionIds_003Ed__266 _003Cget_SectionIds_003Ed__ = new _003Cget_SectionIds_003Ed__266(-2);
			_003Cget_SectionIds_003Ed__._003C_003E4__this = this;
			return _003Cget_SectionIds_003Ed__;
		}
	}

	public float BrushSizeM => brush;

	public double EtaSeconds => etaSeconds;

	public bool ProbeWasAlive => probeWasAlive;

	public string ProbeDetail => probeDetail;

	public bool LastHashAlive => lastHashAlive;

	public string LastHashDetail => lastHashDetail;

	public bool Running => running;

	public Flat2DDrawModel.Tool ActiveTool => tool;

	public bool HasShot => solver != null;

	public float ShownDx => (solver != null) ? solver.Dx : model.dx;

	private static string BrushModName => ControlBindings.ModifierName(ControlBindings.Modifier(ControlModifier.FlatBrushSize));

	private static string PreciseModName => ControlBindings.ModifierName(ControlBindings.Modifier(ControlModifier.FlatPrecise));

	public Rect RenderRectPx => (view != null) ? view.ViewportPx : default(Rect);

	public Flat2DSolver ActiveSolver => solver;

	public Canvas CanvasOf => canvas;

	public static int PlannedStepsFor(float dx)
	{
		return Math.Max(1, Convert.ToInt32(0.0005f / dx * 4000f));
	}

	private static string Tr(string ru)
	{
		return Flat2DText.Tr(ru);
	}

	private static string F(string ru, params object[] a)
	{
		return Flat2DText.F(ru, a);
	}

	private static string RowName(Flat2DCatalogue.Row r)
	{
		return Flat2DCatalogue.Shown(r);
	}

	private static string MatName(ContinuumMaterialId id)
	{
		return Flat2DText.Tr(ContinuumMaterials.DisplayName(id));
	}

	private static string DecalName(string id)
	{
		if (Flat2DRounds.TryGet(id, out Flat2DRounds.Entry e))
		{
			return Flat2DCatalogue.ShownRound(e);
		}
		Flat2DDrawModel.Decal[] decals = Flat2DDrawModel.Decals;
		for (int i = 0; i < decals.Length; i++)
		{
			if (decals[i].id == id)
			{
				return Tr(decals[i].display);
			}
		}
		return id;
	}

	private static string PresetName(string id)
	{
		Flat2DDrawModel.Preset[] presets = Flat2DDrawModel.Presets;
		for (int i = 0; i < presets.Length; i++)
		{
			if (presets[i].id == id)
			{
				return Tr(presets[i].display);
			}
		}
		return id;
	}

	private static string ToolName(Flat2DDrawModel.Tool t)
	{
		string ru;
		switch (t)
		{
		case Flat2DDrawModel.Tool.Polygon:
			ru = "ПОЛИГОН";
			break;
		case Flat2DDrawModel.Tool.Rect:
			ru = "ПРЯМОУГ.";
			break;
		case Flat2DDrawModel.Tool.Circle:
			return Flat2DText.Tr("КРУГ");
		case Flat2DDrawModel.Tool.Brush:
			return Flat2DText.Tr("КИСТЬ");
		default:
			return Flat2DText.Tr("ЛАСТИК");
		}
		return Flat2DText.Tr(ru);
	}

	public static Flat2DDrawScreen Create(ComputeShader shader, Camera cam = null)
	{
		GameObject gameObject = new GameObject("Flat2DDrawScreen");
		Flat2DDrawScreen flat2DDrawScreen = gameObject.AddComponent<Flat2DDrawScreen>();
		flat2DDrawScreen.solverShader = shader;
		flat2DDrawScreen.uiCam = ((cam != null) ? cam : Camera.main);
		flat2DDrawScreen.BuildUi();
		return flat2DDrawScreen;
	}

	private void BuildUi()
	{
		view = gameObject.AddComponent<Flat2DRuntimeView>();
		view.Dx = model.dx;
		view.RenderStyle = ((Shader.Find("Flat2D/SurfaceScreen") != null) ? Flat2DRuntimeView.Style.Surface : Flat2DRuntimeView.Style.Izmerenie);
		view.ShowGrid = true;
		view.ShowDomain = true;
		view.DomainLo = new Vector2(-0.08f, -0.04f);
		view.DomainHi = new Vector2(0.24f, 0.16f);
		if (uiCam == null)
		{
			GameObject go = new GameObject("Flat2DUiCamera", typeof(Camera));
			go.transform.SetParent(base.transform, worldPositionStays: false);
			uiCam = go.GetComponent<Camera>();
			uiCam.clearFlags = CameraClearFlags.SolidColor;
			uiCam.backgroundColor = new Color(0.94f, 0.94f, 0.93f, 1f);
			uiCam.orthographic = true;
		}
		GameObject canvasGo = new GameObject("DrawCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
		canvasGo.transform.SetParent(base.transform, worldPositionStays: false);
		canvas = canvasGo.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceCamera;
		canvas.worldCamera = uiCam;
		canvas.planeDistance = 1f;
		canvas.sortingOrder = 40;
		CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;
		if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
		{
			new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
		}
		BuildLeft(canvasGo.transform);
		BuildCentre(canvasGo.transform);
		BuildRight(canvasGo.transform);
		BuildEscapeMenu(canvasGo.transform);
		SyncSlidersFromState();
		Refresh(Localization.T("flat.hint.draw"));
	}

	private static TextMeshProUGUI Fit(TextMeshProUGUI t, bool wrap = false, float autoMin = 0f)
	{
		t.textWrappingMode = (wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap);
		t.overflowMode = (wrap ? TextOverflowModes.Truncate : TextOverflowModes.Ellipsis);
		if (autoMin > 0f)
		{
			t.enableAutoSizing = true;
			t.fontSizeMin = autoMin;
			t.fontSizeMax = t.fontSize;
		}
		return t;
	}

	private static Button Compact(Button b, float fontSize = 15f)
	{
		TextMeshProUGUI label = MenuTheme.LabelOf(b);
		if (label != null)
		{
			label.fontSize = fontSize;
			label.characterSpacing = 0f;
			label.enableAutoSizing = true;
			label.fontSizeMin = fontSize * 0.6f;
			label.fontSizeMax = fontSize;
			label.textWrappingMode = TextWrappingModes.NoWrap;
			label.overflowMode = TextOverflowModes.Ellipsis;
		}
		return b;
	}

	private static RectTransform Panel(Transform parent, string name, Color c)
	{
		RectTransform rect = MenuTheme.MakeRect(parent, name);
		Image image = rect.gameObject.AddComponent<Image>();
		image.color = c;
		image.raycastTarget = true;
		rect.gameObject.AddComponent<Outline>();
		return rect;
	}

	private static TextMeshProUGUI BlockTitle(Transform parent, string text)
	{
		TextMeshProUGUI label = MenuTheme.MakeLabelRaw(parent, "T_" + text, Flat2DText.Tr(text).ToUpperInvariant(), 13f, FontStyles.Bold, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		label.characterSpacing = 6f;
		LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = 18f;
		layout.preferredHeight = 18f;
		label.textWrappingMode = TextWrappingModes.NoWrap;
		label.overflowMode = TextOverflowModes.Ellipsis;
		return label;
	}

	private void BuildLeft(Transform parent)
	{
		RectTransform left = Panel(parent, "LeftPanel", MenuTheme.Background);
		left.anchorMin = new Vector2(0f, 0f);
		left.anchorMax = new Vector2(0f, 1f);
		left.pivot = new Vector2(0f, 0.5f);
		left.sizeDelta = new Vector2(320f, 0f);
		left.anchoredPosition = Vector2.zero;
		TextMeshProUGUI leftTitle = MenuTheme.MakeLabelRaw(left, "LeftTitle", Tr("ЧТО СТРЕЛЯЕТ И ВО ЧТО"), 16f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		MenuTheme.Place(leftTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -8f), new Vector2(300f, 22f));
		leftTitle.characterSpacing = 4f;
		Fit(leftTitle, false, 11f);
		reasonLabel = MenuTheme.MakeLabelRaw(left, "LeftReason", "", 13f, FontStyles.Normal, MenuTheme.Refused, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(reasonLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 6f), new Vector2(300f, 76f));
		Fit(reasonLabel, wrap: true);
		ScrollRect scrollRect = MenuTheme.MakeScrollList(left, "LeftScroll", out RectTransform content);
		MenuTheme.Stretch((RectTransform)scrollRect.transform, new Vector2(4f, 88f), new Vector2(-4f, -34f));
		AddSection(content, "proj", Tr("СНАРЯДЫ"), Flat2DCatalogue.Projectiles());
		AddSection(content, "mat", Tr("МАТЕРИАЛЫ"), Flat2DCatalogue.Materials());
		AddSection(content, "era", Tr("ДЗ И НЕРА"), Flat2DCatalogue.Reactive());
		OpenSection("mat");
	}

	private void AddSection(Transform parent, string id, string title, List<Flat2DCatalogue.Row> rows)
	{
		_003C_003Ec__DisplayClass115_0 _003C_003Ec__DisplayClass115_ = new _003C_003Ec__DisplayClass115_0();
		_003C_003Ec__DisplayClass115_._003C_003E4__this = this;
		_003C_003Ec__DisplayClass115_.id = id;
		RectTransform rect = MenuTheme.MakeColumn(parent, "Sec_" + id, 2f);
		Section section = new Section();
		section.id = id;
		section.title = title;
		section.header = Compact(MenuTheme.MakeButtonRaw(rect, "SecHdr_" + id, "", 30f, new UnityAction(_003C_003Ec__DisplayClass115_._003CAddSection_003Eb__0), 16f, TextAlignmentOptions.Left, 12f), 16f);
		RectTransform rect2 = MenuTheme.MakeRect(rect, "SecBody_" + id);
		List<CatalogEntry> list = new List<CatalogEntry>();
		foreach (Flat2DCatalogue.Row row in rows)
		{
			list.Add(row.ToEntry());
			rowsById[row.id] = row;
		}
		section.grid = CatalogGrid.Build(rect2.gameObject, list.ToArray(), 0f, 312f, 10f, new Action<string>(_003C_003Ec__DisplayClass115_._003CAddSection_003Eb__1), new Action<string>(ShowReason), 2, 34f);
		LayoutElement layout = rect2.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = 0f - section.grid.EndY;
		layout.preferredHeight = 0f - section.grid.EndY;
		foreach (Flat2DCatalogue.Row row2 in rows)
		{
			if (!row2.available)
			{
				section.grid.SetRefused(row2.id, Tr(row2.reason));
				RectTransform rect3 = section.grid.ButtonRect(row2.id);
				TMP_Text val = section.grid.ButtonLabel(row2.id);
				if (rect3 == null)
				{
					continue;
				}
				if (val != null)
				{
					val.rectTransform.offsetMin = new Vector2(2f, 12f);
					val.rectTransform.offsetMax = new Vector2(-2f, -1f);
					val.fontSizeMax = 11f;
				}
				TextMeshProUGUI val2 = MenuTheme.MakeLabelRaw(rect3, "Tag", Tr(Flat2DCatalogue.TagOf(row2)), 9f, FontStyles.Bold, MenuTheme.RefusedText, TextAlignmentOptions.Center);
				val2.rectTransform.anchorMin = Vector2.zero;
				val2.rectTransform.anchorMax = new Vector2(1f, 0f);
				val2.rectTransform.pivot = new Vector2(0.5f, 0f);
				val2.rectTransform.offsetMin = new Vector2(2f, 1f);
				val2.rectTransform.offsetMax = new Vector2(-2f, 12f);
				Fit(val2, false, 6f);
			}
		}
		section.body = rect2;
		sections.Add(section);
		SetSectionOpen(section, open: false);
	}

	private void ToggleSection(string id)
	{
		foreach (Section s in sections)
		{
			if (s.id == id)
			{
				SetSectionOpen(s, !s.open);
			}
			else
			{
				SetSectionOpen(s, false);
			}
		}
		Refresh(null);
	}

	private void OpenSection(string id)
	{
		foreach (Section s in sections)
		{
			SetSectionOpen(s, s.id == id);
		}
	}

	private void SetSectionOpen(Section s, bool open)
	{
		s.open = open;
		s.body.gameObject.SetActive(open);
		TextMeshProUGUI label = MenuTheme.LabelOf(s.header);
		if (label != null)
		{
			label.text = (open ? "[ - ]  " : "[ + ]  ") + s.title;
		}
		MenuTheme.SetSelectedVisual(s.header, open);
	}

	public string PickCatalogue(string id)
	{
		if (!rowsById.TryGetValue(id, out var row))
		{
			return "строки каталога с таким ключом нет";
		}
		if (!row.available)
		{
			ShowReason(Tr(row.reason));
			Refresh(Flat2DCatalogue.Shown(row) + ": " + Tr(row.reason));
			return row.reason;
		}
		if (row.round != null)
		{
			return PickRound(row);
		}
		ReleaseRound();
		pickedMat = row.mat;
		pickedRole = (row.isMaterial ? Flat2DFill.Role.Armour : Flat2DFill.Role.Projectile);
		foreach (Section section in sections)
		{
			section.grid.SetSelected(id);
		}
		lastPickedRowId = id;
		ShowReason("");
		if (selectedRef != null)
		{
			int num = model.shapes.IndexOf(selectedRef);
			if (num > -1)
			{
				model.Checkpoint();
				model.SetShapeMaterial(num, row.mat);
				listVersion = -2;
			}
		}
		Refresh(F("выбрано: {0} — роль: {1}", RowName(row), Flat2DDrawModel.RoleName(pickedRole)));
		return null;
	}

	private string PickRound(Flat2DCatalogue.Row row)
	{
		if (heldDecal == row.round)
		{
			ReleaseRound();
			Refresh(F("{0}: убран из руки", RowName(row)));
			return null;
		}
		ReleaseRound();
		matBeforeRound = pickedMat;
		roleBeforeRound = pickedRole;
		heldDecal = row.round;
		heldRoundName = RowName(row);
		pickedMat = row.mat;
		pickedRole = Flat2DFill.Role.Projectile;
		foreach (Section section in sections)
		{
			section.grid.SetSelected(row.id);
		}
		if (reasonLabel != null)
		{
			reasonLabel.text = row.tooltip ?? "";
		}
		Refresh(F("{0} в руке — роль: {1}; превью у курсора, колесо вертит ({2} — точно), клик ставит; повторный выбор — убрать", RowName(row), Flat2DDrawModel.RoleName(pickedRole), ControlBindings.ModifierName(ControlBindings.Modifier(ControlModifier.FlatPrecise))));
		return null;
	}

	private void ReleaseRound()
	{
		if (!Flat2DRounds.IsRoundId(heldDecal))
		{
			return;
		}
		heldDecal = null;
		heldRoundName = null;
		pickedMat = matBeforeRound;
		pickedRole = roleBeforeRound;
		foreach (Section section in sections)
		{
			section.grid.SetSelected(lastPickedRowId);
		}
		if (view != null)
		{
			view.ClearCursor();
		}
	}

	private string PlaceHeldRound(Vector2 w)
	{
		if (!Flat2DRounds.TryGet(heldDecal, out var e) || !e.available)
		{
			string text = heldRoundName ?? heldDecal;
			ReleaseRound();
			Refresh(F("{0}: снаряд недоступен — {1}", text, Tr(e.reason ?? "строки нет в реестре силуэтов")));
			return null;
		}
		Vector2 velocity = Flat2DDrawModel.Rotate(new Vector2(e.velocityMps, 0f), Vector2.zero, decalAngle);
		if (model.PlaceDecal(heldDecal, w, decalT, decalH, decalAngle, e.mat, Flat2DFill.Role.Projectile, velocity, e.display, pickedPaint) == 0)
		{
			Refresh(F("{0}: силуэт не построился", Flat2DCatalogue.ShownRound(e)));
			return null;
		}
		projectileVel = velocity;
		SyncSlidersFromState();
		float num = decalAngle;
		listVersion = -2;
		ReleaseRound();
		string text2 = model.CeilingWarning();
		if (text2 != null)
		{
			Refresh(text2);
			return text2;
		}
		float num2 = Flat2DModelScale.S;
		float num3 = e.diameterM * num2;
		Refresh(F("поставлен {0} в масштабе {1}: Ø{2:F1} × {3:F1} мм (натура Ø{4:F1} × {5:F0} мм), v0 {6:F0} м/с, {7}", Flat2DCatalogue.ShownRound(e), Flat2DModelScale.Label, num3 * 1000f, e.lengthM * num2 * 1000f, e.diameterM * 1000f, e.lengthM * 1000f, e.velocityMps, Tr(ContinuumMaterials.DisplayName(e.mat))) + ((num == 0f) ? "" : F(", угол {0:F0}°", num)) + ((num3 < model.dx * 3f) ? F(" — Ø в модели {0:F1} Δx: сердечник почти не разрешён решёткой", num3 / model.dx) : "") + Tr(" — выбор снят"));
		return null;
	}

	private void ShowReason(string s)
	{
		if (reasonLabel != null)
		{
			reasonLabel.text = (s ?? "");
		}
	}

	private void BuildCentre(Transform parent)
	{
		RectTransform centre = MenuTheme.MakeRect(parent, "Centre");
		centre.anchorMin = Vector2.zero;
		centre.anchorMax = Vector2.one;
		centre.offsetMin = new Vector2(320f, 0f);
		centre.offsetMax = new Vector2(-360f, 0f);
		RectTransform topBar = MenuTheme.MakeRect(centre, "TopBar");
		topBar.anchorMin = new Vector2(0f, 1f);
		topBar.anchorMax = new Vector2(1f, 1f);
		topBar.pivot = new Vector2(0.5f, 1f);
		topBar.offsetMin = new Vector2(6f, -40f);
		topBar.offsetMax = new Vector2(-6f, -6f);
		MenuTheme.AddImage(topBar, MenuTheme.Surface);
		hud = MenuTheme.MakeLabelRaw(topBar, "Hud", "", 14f, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		MenuTheme.Stretch(hud.rectTransform, new Vector2(10f, 2f), new Vector2(-560f, -2f));
		Fit(hud);
		hint = MenuTheme.MakeLabelRaw(topBar, "Hint", Localization.T("flat.hint.draw"), 14f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopRight);
		MenuTheme.Stretch(hint.rectTransform, new Vector2(-550f, 2f), new Vector2(-10f, -2f));
		hint.rectTransform.anchorMin = new Vector2(1f, 0f);
		hint.rectTransform.anchorMax = new Vector2(1f, 1f);
		hint.rectTransform.pivot = new Vector2(1f, 0.5f);
		hint.rectTransform.sizeDelta = new Vector2(540f, -4f);
		hint.rectTransform.anchoredPosition = new Vector2(-10f, 0f);
		Fit(hint);
		RectTransform surface = MenuTheme.MakeRect(centre, "Surface");
		surface.anchorMin = Vector2.zero;
		surface.anchorMax = Vector2.one;
		surface.offsetMin = new Vector2(6f, 126f);
		surface.offsetMax = new Vector2(-6f, -44f);
		surface.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.02f);
		surface.gameObject.AddComponent<DrawSurface>().screen = this;
		surfaceRect = surface;
		RectTransform scaleStrip = MenuTheme.MakeRect(centre, "ScaleStrip");
		scaleStrip.anchorMin = new Vector2(0f, 0f);
		scaleStrip.anchorMax = new Vector2(1f, 0f);
		scaleStrip.pivot = new Vector2(0.5f, 0f);
		scaleStrip.offsetMin = new Vector2(6f, 100f);
		scaleStrip.offsetMax = new Vector2(-6f, 120f);
		scaleStrip.sizeDelta = new Vector2(-12f, 20f);
		scaleStrip.anchoredPosition = new Vector2(0f, 100f);
		MenuTheme.AddImage(scaleStrip, MenuTheme.Surface);
		scaleBarRt = MenuTheme.MakeRect(scaleStrip, "ScaleBar");
		MenuTheme.Place(scaleBarRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(100f, 6f));
		MenuTheme.AddImage(scaleBarRt, MenuTheme.TextPrimary);
		scaleBarLabel = MenuTheme.MakeLabelRaw(scaleStrip, "ScaleLabel", "", 13f, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		MenuTheme.Place(scaleBarLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(112f, 0f), new Vector2(300f, 20f));
		Fit(scaleBarLabel);
		rulerLabel = MenuTheme.MakeLabelRaw(scaleStrip, "RulerLabel", "", 13f, FontStyles.Normal, MenuTheme.GizmoHot, TextAlignmentOptions.TopRight);
		MenuTheme.Place(rulerLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(420f, 20f));
		Fit(rulerLabel);
		BuildFireRow(centre);
	}

	private void BuildFireRow(Transform centre)
	{
		RectTransform fireBar = MenuTheme.MakeRect(centre, "FireBar");
		fireBar.anchorMin = new Vector2(0f, 0f);
		fireBar.anchorMax = new Vector2(1f, 0f);
		fireBar.pivot = new Vector2(0.5f, 0f);
		fireBar.sizeDelta = new Vector2(-12f, 96f);
		fireBar.anchoredPosition = new Vector2(0f, 4f);
		MenuTheme.AddImage(fireBar, MenuTheme.Surface);
		RectTransform rowA = MenuTheme.MakeRow(fireBar, "RowA", 8f, TextAnchor.MiddleLeft);
		rowA.anchorMin = new Vector2(0f, 1f);
		rowA.anchorMax = new Vector2(1f, 1f);
		rowA.pivot = new Vector2(0.5f, 1f);
		rowA.sizeDelta = new Vector2(-16f, 46f);
		rowA.anchoredPosition = new Vector2(0f, -6f);
		fireBtn = Compact(MenuTheme.MakeButtonRaw(rowA, "BtnFire", Localization.T("flat.fire"), 42f, new UnityAction(_003CBuildFireRow_003Eb__126_0), 20f, TextAlignmentOptions.Center, 16f), 20f);
		Width(fireBtn, 190f);
		TextMeshProUGUI vLabel = MenuTheme.MakeLabelRaw(rowA, "VLabel", Tr("скорость, м/с"), 14f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopRight);
		Width(vLabel, 118f);
		Fit(vLabel);
		velField = MakeInput(rowA, 92f, 40f, "300");
		velField.onEndEdit.AddListener(new UnityAction<string>(_003CBuildFireRow_003Eb__126_1));
		velSlider = MakeSlider(rowA, "VelSlider", 0f, 2500f, 300f, 300f, new UnityAction<float>(_003CBuildFireRow_003Eb__126_2));
		etaLabel = MenuTheme.MakeLabelRaw(rowA, "EtaLabel", "", 14f, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		LayoutElement layout = etaLabel.gameObject.AddComponent<LayoutElement>();
		layout.flexibleWidth = 1f;
		layout.minWidth = 200f;
		Fit(etaLabel);
		RectTransform rowB = MenuTheme.MakeRow(fireBar, "RowB", 6f, TextAnchor.MiddleLeft);
		rowB.anchorMin = new Vector2(0f, 0f);
		rowB.anchorMax = new Vector2(1f, 0f);
		rowB.pivot = new Vector2(0.5f, 0f);
		rowB.sizeDelta = new Vector2(-16f, 36f);
		rowB.anchoredPosition = new Vector2(0f, 6f);
		runBtn = Compact(MenuTheme.MakeButtonRaw(rowB, "BtnRun", Localization.T("flat.pause"), 32f, new UnityAction(TogglePlay), 14f, TextAlignmentOptions.Center, 16f), 14f);
		Width(runBtn, 120f);
		Button stepBtn = Compact(MenuTheme.MakeButtonRaw(rowB, "BtnStep", Localization.T("flat.step"), 32f, new UnityAction(StepOnce), 14f, TextAlignmentOptions.Center, 16f), 14f);
		Width(stepBtn, 84f);
		Button stopBtn = Compact(MenuTheme.MakeButtonRaw(rowB, "BtnStop", Tr("ОСТАНОВИТЬ"), 32f, new UnityAction(_003CBuildFireRow_003Eb__126_4), 14f, TextAlignmentOptions.Center, 16f), 14f);
		Width(stopBtn, 118f);
		TextMeshProUGUI fieldsLabel = MenuTheme.MakeLabelRaw(rowB, "FieldsLabel", Tr("ПОЛЕ:"), 12f, FontStyles.Bold, MenuTheme.TextMuted, TextAlignmentOptions.TopRight);
		Width(fieldsLabel, 52f);
		Fit(fieldsLabel);
		string[] array = new string[7]
		{
			Localization.T("flat.field.material"),
			Localization.T("flat.field.pressure"),
			Localization.T("flat.field.damage"),
			Localization.T("flat.field.speed"),
			Localization.T("flat.field.failed"),
			Localization.T("flat.field.bodies"),
			Tr("ТЕМПЕРАТУРА")
		};
		for (int i = 0; i < array.Length; i++)
		{
			_003C_003Ec__DisplayClass126_0 _003C_003Ec__DisplayClass126_ = new _003C_003Ec__DisplayClass126_0();
			_003C_003Ec__DisplayClass126_._003C_003E4__this = this;
			_003C_003Ec__DisplayClass126_.mode = i;
			Button button = Compact(MenuTheme.MakeButtonRaw(rowB, "F" + i, array[i], 32f, new UnityAction(_003C_003Ec__DisplayClass126_._003CBuildFireRow_003Eb__5), 13f, TextAlignmentOptions.Center, 16f), 13f);
			LayoutElement layout2 = button.gameObject.AddComponent<LayoutElement>();
			layout2.flexibleWidth = 1f;
			layout2.minWidth = 96f;
			fieldButtons.Add(button);
		}
	}

	[CompilerGenerated]
	private void _003CBuildFireRow_003Eb__126_0()
	{
		Fire();
	}

	[CompilerGenerated]
	private void _003CBuildFireRow_003Eb__126_1(string txt)
	{
		float result;
		if (float.TryParse(txt, out result))
		{
			SetVelocity(result);
		}
		else
		{
			Refresh(Tr("скорость: не число — оставлено прежнее значение"));
		}
	}

	[CompilerGenerated]
	private void _003CBuildFireRow_003Eb__126_2(float v)
	{
		SetVelocity(v);
	}

	[CompilerGenerated]
	private void _003CBuildFireRow_003Eb__126_3()
	{
		StepSolver(1);
	}

	[CompilerGenerated]
	private void _003CBuildFireRow_003Eb__126_4()
	{
		StopShot();
	}

	private static void Width(Component c, float w)
	{
		LayoutElement layout = c.gameObject.GetComponent<LayoutElement>();
		if (layout == null)
		{
			layout = c.gameObject.AddComponent<LayoutElement>();
		}
		layout.minWidth = w;
		layout.preferredWidth = w;
	}

	private void BuildRight(Transform parent)
	{
		RectTransform panel = Panel(parent, "RightPanel", MenuTheme.Background);
		panel.anchorMin = new Vector2(1f, 0f);
		panel.anchorMax = new Vector2(1f, 1f);
		panel.pivot = new Vector2(1f, 0.5f);
		panel.sizeDelta = new Vector2(360f, 0f);
		panel.anchoredPosition = Vector2.zero;
		TextMeshProUGUI rightTitle = MenuTheme.MakeLabelRaw(panel, "RightTitle", Tr("КАК РИСУЕМ"), 16f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		MenuTheme.Place(rightTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -8f), new Vector2(340f, 22f));
		rightTitle.characterSpacing = 4f;
		rightTitle.textWrappingMode = TextWrappingModes.NoWrap;
		rightTitle.overflowMode = TextOverflowModes.Ellipsis;
		rightTitle.enableAutoSizing = true;
		rightTitle.fontSizeMin = 11f;
		rightTitle.fontSizeMax = rightTitle.fontSize;
		statusLabel = MenuTheme.MakeLabelRaw(panel, "Status", "", 13f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(statusLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 6f), new Vector2(340f, 54f));
		statusLabel.textWrappingMode = TextWrappingModes.Normal;
		statusLabel.overflowMode = TextOverflowModes.Truncate;
		ScrollRect rightScroll = MenuTheme.MakeScrollList(panel, "RightScroll", out RectTransform col);
		MenuTheme.Stretch((RectTransform)rightScroll.transform, new Vector2(6f, 64f), new Vector2(-4f, -34f));
		VerticalLayoutGroup layout = col.GetComponent<VerticalLayoutGroup>();
		if (layout != null)
		{
			layout.spacing = 6f;
		}
		BuildColourBlock(col);
		BuildToolBlock(col);
		BuildBrushBlock(col);
		BuildDecalBlock(col);
		BuildSnapBlock(col);
		BuildShapesBlock(col);
		BuildSceneBlock(col);
	}

	private void BuildColourBlock(Transform col)
	{
		colourHeader = Compact(MenuTheme.MakeButtonRaw(col, "ColourHeader", "", 28f, new UnityAction(_003CBuildColourBlock_003Eb__135_0), 14f, TextAlignmentOptions.Left, 10f), 14f);
		RectTransform chipRect = MenuTheme.MakeRect(colourHeader.transform, "ColourChip");
		MenuTheme.Place(chipRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(40f, 16f));
		colourChip = MenuTheme.AddImage(chipRect, default(Color));
		colourBody = MenuTheme.MakeColumn(col, "ColourBody", 3f);
		swatchMarks.Clear();
		for (int i = 0; i < 2; i++)
		{
			RectTransform swatchesRow = MenuTheme.MakeRow(colourBody, "Swatches" + i, 14f, TextAnchor.MiddleCenter);
			for (int j = 0; j < 14; j++)
			{
				_003C_003Ec__DisplayClass135_0 displayClass = new _003C_003Ec__DisplayClass135_0();
				displayClass._003C_003E4__this = this;
				displayClass.idx = i * 14 + j;
				swatchMarks.Add(MakeSwatch(swatchesRow, "Swatch_" + displayClass.idx, Palette[displayClass.idx], new UnityAction(displayClass._003CBuildColourBlock_003Eb__2)));
			}
		}
		RectTransform colourReset = MenuTheme.MakeRow(colourBody, "ColourReset", 4f, TextAnchor.MiddleCenter, true);
		Button byMaterial = Compact(MenuTheme.MakeButtonRaw(colourReset, "PaintByMaterial", Tr("ПО ВЕЩЕСТВУ"), 24f, new UnityAction(_003CBuildColourBlock_003Eb__135_1), 12f, TextAlignmentOptions.Center, 16f), 12f);
		TextMeshProUGUI hint = MenuTheme.MakeLabelRaw(colourBody, "ColourHint", Tr("цвет — только показ (поле ВЕЩЕСТВО): вещество, роль и скорость он не меняет; перекрасить — кистью того же вещества"), 11f, FontStyles.Italic, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		LayoutElement contentSizeFitter = hint.gameObject.AddComponent<LayoutElement>();
		contentSizeFitter.minHeight = 30f;
		contentSizeFitter.preferredHeight = 30f;
		hint.textWrappingMode = TextWrappingModes.Normal;
		hint.overflowMode = TextOverflowModes.Truncate;
		SetColourOpen(false);
	}

	private void _003CBuildColourBlock_003Eb__135_0()
	{
		SetColourOpen(!colourOpen);
	}

	private void _003CBuildColourBlock_003Eb__135_1()
	{
		PickPaint(-1);
	}

	private static Outline MakeSwatch(Transform row, string name, Color32 c, UnityAction act)
	{
		RectTransform rect = MenuTheme.MakeRect(row, name);
		LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
		layout.minWidth = 21f;
		layout.preferredWidth = 21f;
		layout.minHeight = 21f;
		layout.preferredHeight = 21f;
		Image image = rect.gameObject.AddComponent<Image>();
		image.color = c;
		image.raycastTarget = true;
		Button button = rect.gameObject.AddComponent<Button>();
		button.targetGraphic = image;
		button.onClick.AddListener(act);
		Outline outline = rect.gameObject.AddComponent<Outline>();
		outline.effectColor = new Color(0.95f, 0.75f, 0.1f, 1f);
		outline.effectDistance = new Vector2(2f, -2f);
		outline.enabled = false;
		return outline;
	}

	private void SetColourOpen(bool open)
	{
		colourOpen = open;
		if (colourBody != null)
		{
			colourBody.gameObject.SetActive(open);
		}
		UpdateColourHeader();
	}

	private void UpdateColourHeader()
	{
		if (colourHeader == null)
		{
			return;
		}
		TextMeshProUGUI label = MenuTheme.LabelOf(colourHeader);
		if (label != null)
		{
			string text = (colourOpen ? "[ - ]  " : "[ + ]  ") + F("ЦВЕТ: {0}", PaintName(pickedPaint));
			label.text = text;
		}
		if (colourChip != null)
		{
			colourChip.color = ((pickedPaint.a != 0) ? pickedPaint : default(Color));
		}
		for (int i = 0; i < swatchMarks.Count; i++)
		{
			swatchMarks[i].enabled = pickedPaint.a != 0 && Same(pickedPaint, Palette[i]);
		}
	}

	private static bool Same(Color32 a, Color32 b)
	{
		return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
	}

	private static string PaintName(Color32 c)
	{
		if (c.a == 0)
		{
			return Tr("по веществу");
		}
		return string.Format("#{0:X2}{1:X2}{2:X2}", c.r, c.g, c.b);
	}

	public void PickPaint(int swatch)
	{
		pickedPaint = ((swatch >= 0 && swatch < Palette.Length) ? Palette[swatch] : default(Color32));
		UpdateColourHeader();
		if (pickedPaint.a != 0)
		{
			Refresh(F("цвет: {0} — новые мазки, фигуры и декали пластин рисуются им", PaintName(pickedPaint)));
		}
		else
		{
			Refresh(Tr("цвет: по веществу — новые мазки и фигуры красятся цветом вещества"));
		}
	}

	public void SetColourPanelOpen(bool open)
	{
		SetColourOpen(open);
	}

	private void BuildToolBlock(Transform col)
	{
		BlockTitle(col, "Инструмент");
		RectTransform toolsRow1 = MenuTheme.MakeRow(col, "Tools1", 4f, TextAnchor.MiddleCenter, true);
		AddToolBtn(toolsRow1, "TPoly", Tr("ПОЛИГОН"), Flat2DDrawModel.Tool.Polygon);
		AddToolBtn(toolsRow1, "TRect", Tr("ПРЯМОУГ."), Flat2DDrawModel.Tool.Rect);
		AddToolBtn(toolsRow1, "TCircle", Tr("КРУГ"), Flat2DDrawModel.Tool.Circle);
		RectTransform toolsRow2 = MenuTheme.MakeRow(col, "Tools2", 4f, TextAnchor.MiddleCenter, true);
		AddToolBtn(toolsRow2, "TBrush", Tr("КИСТЬ"), Flat2DDrawModel.Tool.Brush);
		AddToolBtn(toolsRow2, "TEraser", Tr("ЛАСТИК"), Flat2DDrawModel.Tool.Eraser);
		Button ruler = Compact(MenuTheme.MakeButtonRaw(toolsRow2, "TRuler", Tr("ЛИНЕЙКА"), 26f, new UnityAction(StartRuler), 13f, TextAlignmentOptions.Center, 16f), 13f);
		RectTransform editRow1 = MenuTheme.MakeRow(col, "Edit1", 4f, TextAnchor.MiddleCenter, true);
		Button close = Compact(MenuTheme.MakeButtonRaw(editRow1, "BtnClose", Localization.T("flat.close_shape"), 26f, new UnityAction(_003CBuildToolBlock_003Eb__151_0), 13f, TextAlignmentOptions.Center, 16f), 13f);
		Button undo = Compact(MenuTheme.MakeButtonRaw(editRow1, "BtnUndo", Localization.T("flat.undo"), 26f, new UnityAction(UndoPressed), 13f, TextAlignmentOptions.Center, 16f), 13f);
		RectTransform editRow2 = MenuTheme.MakeRow(col, "Edit2", 4f, TextAnchor.MiddleCenter, true);
		Button redo = Compact(MenuTheme.MakeButtonRaw(editRow2, "BtnRedo", Tr("ВЕРНУТЬ"), 26f, new UnityAction(_003CBuildToolBlock_003Eb__151_1), 13f, TextAlignmentOptions.Center, 16f), 13f);
		Button clear = Compact(MenuTheme.MakeButtonRaw(editRow2, "BtnClear", Localization.T("flat.clear"), 26f, new UnityAction(_003CBuildToolBlock_003Eb__151_2), 13f, TextAlignmentOptions.Center, 16f), 13f);
		mirrorBtn = Compact(MenuTheme.MakeButtonRaw(editRow2, "BtnMirror", Tr("СИММЕТРИЯ"), 26f, new UnityAction(_003CBuildToolBlock_003Eb__151_3), 13f, TextAlignmentOptions.Center, 16f), 13f);
		RectTransform rolesRow = MenuTheme.MakeRow(col, "Roles", 4f, TextAnchor.MiddleCenter, true);
		roleButtons.Clear();
		roleButtons.Add(Compact(MenuTheme.MakeButtonRaw(rolesRow, "RArm", Localization.T("flat.role.armour"), 26f, new UnityAction(_003CBuildToolBlock_003Eb__151_4), 13f, TextAlignmentOptions.Center, 16f), 13f));
		roleButtons.Add(Compact(MenuTheme.MakeButtonRaw(rolesRow, "RProj", Localization.T("flat.role.projectile"), 26f, new UnityAction(_003CBuildToolBlock_003Eb__151_5), 13f, TextAlignmentOptions.Center, 16f), 13f));
		roleButtons.Add(Compact(MenuTheme.MakeButtonRaw(rolesRow, "RAnch", Tr("ЯКОРЬ"), 26f, new UnityAction(_003CBuildToolBlock_003Eb__151_6), 13f, TextAlignmentOptions.Center, 16f), 13f));
		anchorHint = MenuTheme.MakeLabelRaw(col, "AnchorHint", Localization.T("flat.role.anchor"), 12f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		LayoutElement contentSizeFitter = anchorHint.gameObject.AddComponent<LayoutElement>();
		contentSizeFitter.minHeight = 34f;
		contentSizeFitter.preferredHeight = 34f;
		anchorHint.textWrappingMode = TextWrappingModes.Normal;
		anchorHint.overflowMode = TextOverflowModes.Truncate;
	}

	private void _003CBuildToolBlock_003Eb__151_0()
	{
		model.Checkpoint();
		if (model.CloseShape())
		{
			Refresh(Tr("фигура замкнута"));
		}
	}

	private void _003CBuildToolBlock_003Eb__151_1()
	{
		if (model.RedoStep())
		{
			listVersion = -2;
			Refresh(Tr("возвращено"));
		}
		else
		{
			Refresh(Tr("возвращать нечего"));
		}
	}

	private void _003CBuildToolBlock_003Eb__151_2()
	{
		model.Checkpoint();
		model.Clear();
		selectedRef = null;
		listVersion = -2;
		Refresh(Tr("чисто"));
	}

	private void _003CBuildToolBlock_003Eb__151_3()
	{
		mirror = !mirror;
		Refresh(Tr(mirror ? "симметрия ВКЛ: отражаются контурные фигуры (мазки кисти и декали — нет)" : "симметрия ВЫКЛ"));
	}

	private void _003CBuildToolBlock_003Eb__151_4()
	{
		PickRole(Flat2DFill.Role.Armour);
	}

	private void _003CBuildToolBlock_003Eb__151_5()
	{
		PickRole(Flat2DFill.Role.Projectile);
	}

	private void _003CBuildToolBlock_003Eb__151_6()
	{
		PickRole(Flat2DFill.Role.Anchor);
	}

	private void AddToolBtn(Transform row, string name, string text, Flat2DDrawModel.Tool t)
	{
		_003C_003Ec__DisplayClass153_0 displayClass = new _003C_003Ec__DisplayClass153_0();
		displayClass._003C_003E4__this = this;
		displayClass.t = t;
		Button button = Compact(MenuTheme.MakeButtonRaw(row, name, text, 32f, new UnityAction(displayClass._003CAddToolBtn_003Eb__0), 13f, TextAlignmentOptions.Center, 16f), 13f);
		toolButtons.Add(button);
	}

	private void BuildBrushBlock(Transform col)
	{
		BlockTitle(col, "Кисть");
		brushLabel = MenuTheme.MakeLabelRaw(col, "BrushLabel", "", 13f, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		LayoutElement contentSizeFitter = brushLabel.gameObject.AddComponent<LayoutElement>();
		contentSizeFitter.minHeight = 32f;
		contentSizeFitter.preferredHeight = 32f;
		brushLabel.textWrappingMode = TextWrappingModes.Normal;
		brushLabel.overflowMode = TextOverflowModes.Truncate;
		RectTransform brushRow = MenuTheme.MakeRow(col, "BrushRow", 6f, TextAnchor.MiddleCenter, true);
		brushSlider = MakeSlider(brushRow, "BrushSlider", 0.0002f, 0.02f, brush, 210f, new UnityAction<float>(_003CBuildBrushBlock_003Eb__154_0));
		RectTransform shapesRow = MenuTheme.MakeRow(col, "BrushShapes", 4f, TextAnchor.MiddleCenter, true);
		brushShapeButtons.Clear();
		brushShapeButtons.Add(Compact(MenuTheme.MakeButtonRaw(shapesRow, "BShapeCircle", Tr("КРУГЛАЯ"), 26f, new UnityAction(_003CBuildBrushBlock_003Eb__154_1), 13f, TextAlignmentOptions.Center, 16f), 13f));
		brushShapeButtons.Add(Compact(MenuTheme.MakeButtonRaw(shapesRow, "BShapeSquare", Tr("КВАДРАТНАЯ"), 26f, new UnityAction(_003CBuildBrushBlock_003Eb__154_2), 13f, TextAlignmentOptions.Center, 16f), 13f));
		TextMeshProUGUI hint = MenuTheme.MakeLabelRaw(col, "BrushHint", F("кисть: зажать и вести — мазок; {0} + колесо над холстом — размер, не отрывая руки", ControlBindings.ModifierName(ControlBindings.Modifier(ControlModifier.FlatBrushSize))), 12f, FontStyles.Italic, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		LayoutElement contentSizeFitter2 = hint.gameObject.AddComponent<LayoutElement>();
		contentSizeFitter2.minHeight = 30f;
		contentSizeFitter2.preferredHeight = 30f;
		hint.textWrappingMode = TextWrappingModes.Normal;
		hint.overflowMode = TextOverflowModes.Truncate;
	}

	private void _003CBuildBrushBlock_003Eb__154_0(float v)
	{
		brush = v;
		Refresh(null);
	}

	private void _003CBuildBrushBlock_003Eb__154_1()
	{
		PickBrushShape(0);
	}

	private void _003CBuildBrushBlock_003Eb__154_2()
	{
		PickBrushShape(1);
	}

	private void PickBrushShape(int s)
	{
		brushShape = s;
		Refresh(Tr((s != 0) ? "кисть: КВАДРАТНАЯ — курсор показывает квадрат отпечатка" : "кисть: КРУГЛАЯ — курсор показывает круг отпечатка"));
	}

	private void SyncDecalSliders()
	{
		if (decalTSlider != null)
		{
			decalTSlider.value = decalT;
		}
		if (decalHSlider != null)
		{
			decalHSlider.value = decalH;
		}
	}

	private void RefreshHeldDecal()
	{
		if (heldDecal != null && view != null && surfaceRect != null)
		{
			Vector2 screen = TouchInputHelper.PrimaryPosition;
			Camera cam = ((canvas != null) ? canvas.worldCamera : null);
			PointCursorAt(ScreenToWorld(screen, cam));
		}
		Refresh(null);
	}

	public void SetDecalSizeForAcceptance(float thicknessM, float lengthM)
	{
		decalT = Mathf.Clamp(thicknessM, 0.0005f, 0.1f);
		decalH = Mathf.Clamp(lengthM, 0.002f, 0.4f);
		SyncDecalSliders();
		Refresh(null);
	}

	private void BuildDecalBlock(Transform col)
	{
		BlockTitle(col, "Декали");
		RectTransform row = MenuTheme.MakeRow(col, "Decals", 4f, TextAnchor.MiddleCenter, true);
		Flat2DDrawModel.Decal[] decals = Flat2DDrawModel.Decals;
		for (int i = 0; i < decals.Length; i++)
		{
			_003C_003Ec__DisplayClass166_0 displayClass = new _003C_003Ec__DisplayClass166_0();
			displayClass._003C_003E4__this = this;
			displayClass.captured = decals[i];
			if (i == 3)
			{
				row = MenuTheme.MakeRow(col, "Decals2", 4f, TextAnchor.MiddleCenter, true);
			}
			else if (i == 5)
			{
				row = MenuTheme.MakeRow(col, "Decals3", 4f, TextAnchor.MiddleCenter, true);
			}
			Button button = Compact(MenuTheme.MakeButtonRaw(row, "D_" + displayClass.captured.id, Tr(displayClass.captured.display), 26f, new UnityAction(displayClass._003CBuildDecalBlock_003Eb__2), 12f, TextAlignmentOptions.Center, 16f), 12f);
		}
		decalLabel = MenuTheme.MakeLabelRaw(col, "DecalLabel", "", 13f, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		LayoutElement labelFitter = decalLabel.gameObject.AddComponent<LayoutElement>();
		labelFitter.minHeight = 32f;
		labelFitter.preferredHeight = 32f;
		decalLabel.textWrappingMode = TextWrappingModes.Normal;
		decalLabel.overflowMode = TextOverflowModes.Truncate;
		RectTransform thickRow = MenuTheme.MakeRow(col, "DecalThickRow", 6f, TextAnchor.MiddleLeft);
		TextMeshProUGUI thickCaption = MenuTheme.MakeLabelRaw(thickRow, "DecalThickCaption", Tr("толщина"), 13f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopRight);
		thickCaption.textWrappingMode = TextWrappingModes.NoWrap;
		thickCaption.overflowMode = TextOverflowModes.Ellipsis;
		Width(thickCaption, 84f);
		decalTSlider = MakeSlider(thickRow, "DecalThickSlider", 0.0005f, 0.1f, decalT, 230f, new UnityAction<float>(_003CBuildDecalBlock_003Eb__166_0));
		RectTransform lengthRow = MenuTheme.MakeRow(col, "DecalLengthRow", 6f, TextAnchor.MiddleLeft);
		TextMeshProUGUI lengthCaption = MenuTheme.MakeLabelRaw(lengthRow, "DecalLengthCaption", Tr("длина"), 13f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopRight);
		lengthCaption.textWrappingMode = TextWrappingModes.NoWrap;
		lengthCaption.overflowMode = TextOverflowModes.Ellipsis;
		Width(lengthCaption, 84f);
		decalHSlider = MakeSlider(lengthRow, "DecalLengthSlider", 0.002f, 0.4f, decalH, 230f, new UnityAction<float>(_003CBuildDecalBlock_003Eb__166_1));
		TextMeshProUGUI hint = MenuTheme.MakeLabelRaw(col, "DecalHint", F("декаль берётся В РУКУ: превью следует за курсором, колесо вертит ({0} — точно, {1} — размер), клик ставит; убрать — повторное нажатие кнопки", ControlBindings.ModifierName(ControlBindings.Modifier(ControlModifier.FlatPrecise)), ControlBindings.ModifierName(ControlBindings.Modifier(ControlModifier.FlatBrushSize))), 12f, FontStyles.Italic, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		LayoutElement hintFitter = hint.gameObject.AddComponent<LayoutElement>();
		hintFitter.minHeight = 40f;
		hintFitter.preferredHeight = 40f;
		hint.textWrappingMode = TextWrappingModes.Normal;
		hint.overflowMode = TextOverflowModes.Truncate;
		BlockTitle(col, "Пресеты сцен");
		RectTransform presetRow = MenuTheme.MakeRow(col, "Presets", 4f, TextAnchor.MiddleCenter, true);
		Flat2DDrawModel.Preset[] presets = Flat2DDrawModel.Presets;
		for (int j = 0; j < presets.Length; j++)
		{
			_003C_003Ec__DisplayClass166_1 displayClass2 = new _003C_003Ec__DisplayClass166_1();
			displayClass2._003C_003E4__this = this;
			displayClass2.captured = presets[j];
			Button preset = Compact(MenuTheme.MakeButtonRaw(presetRow, "P_" + displayClass2.captured.id, Tr(displayClass2.captured.display), 30f, new UnityAction(displayClass2._003CBuildDecalBlock_003Eb__3), 11f, TextAlignmentOptions.Center, 16f), 11f);
		}
	}

	private void _003CBuildDecalBlock_003Eb__166_0(float v)
	{
		decalT = v;
		RefreshHeldDecal();
	}

	private void _003CBuildDecalBlock_003Eb__166_1(float v)
	{
		decalH = v;
		RefreshHeldDecal();
	}

	private void BuildSnapBlock(Transform col)
	{
		BlockTitle(col, "Привязка");
		RectTransform row = MenuTheme.MakeRow(col, "Snap", 4f, TextAnchor.MiddleCenter, true);
		snapButtons.Add(Compact(MenuTheme.MakeButtonRaw(row, "SnapGrid", Tr("К СЕТКЕ Δx"), 26f, new UnityAction(_003CBuildSnapBlock_003Eb__167_0), 12f, TextAlignmentOptions.Center, 16f), 12f));
		snapButtons.Add(Compact(MenuTheme.MakeButtonRaw(row, "SnapAng", Tr("К УГЛУ 15°"), 26f, new UnityAction(_003CBuildSnapBlock_003Eb__167_1), 12f, TextAlignmentOptions.Center, 16f), 12f));
		snapButtons.Add(Compact(MenuTheme.MakeButtonRaw(row, "SnapFree", Tr("СВОБОДНО"), 26f, new UnityAction(_003CBuildSnapBlock_003Eb__167_2), 12f, TextAlignmentOptions.Center, 16f), 12f));
	}

	private void _003CBuildSnapBlock_003Eb__167_0()
	{
		PickSnap(Flat2DDrawModel.Snap.Grid);
	}

	private void _003CBuildSnapBlock_003Eb__167_1()
	{
		PickSnap(Flat2DDrawModel.Snap.Angle15);
	}

	private void _003CBuildSnapBlock_003Eb__167_2()
	{
		PickSnap(Flat2DDrawModel.Snap.Free);
	}

	private void BuildShapesBlock(Transform col)
	{
		BlockTitle(col, "Рисунок");
		shapesHint = MenuTheme.MakeLabelRaw(col, "ShapesHint", "", 12f, FontStyles.Italic, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		LayoutElement contentSizeFitter = shapesHint.gameObject.AddComponent<LayoutElement>();
		contentSizeFitter.minHeight = 58f;
		contentSizeFitter.preferredHeight = 58f;
		shapesHint.textWrappingMode = TextWrappingModes.Normal;
		shapesHint.overflowMode = TextOverflowModes.Truncate;
	}

	private void BuildSceneBlock(Transform col)
	{
		BlockTitle(col, "Сцена");
		sceneLabel = MenuTheme.MakeLabelRaw(col, "SceneLabel", "", 12f, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		LayoutElement sceneFitter = sceneLabel.gameObject.AddComponent<LayoutElement>();
		sceneFitter.minHeight = 46f;
		sceneFitter.preferredHeight = 46f;
		sceneLabel.textWrappingMode = TextWrappingModes.Normal;
		sceneLabel.overflowMode = TextOverflowModes.Truncate;
		RectTransform countTrack = MenuTheme.MakeRect(col, "CountTrack");
		LayoutElement trackFitter = countTrack.gameObject.AddComponent<LayoutElement>();
		trackFitter.minHeight = 14f;
		trackFitter.preferredHeight = 14f;
		MenuTheme.AddImage(countTrack, MenuTheme.SurfaceAlt);
		RectTransform countFill = MenuTheme.MakeRect(countTrack, "CountFill");
		countFill.anchorMin = Vector2.zero;
		countFill.anchorMax = new Vector2(0f, 1f);
		countFill.pivot = new Vector2(0f, 0.5f);
		countFill.offsetMin = Vector2.zero;
		countFill.offsetMax = Vector2.zero;
		countBar = MenuTheme.AddImage(countFill, MenuTheme.SliderFill);
		RectTransform styleRow = MenuTheme.MakeRow(col, "ViewStyleRow", 4f, TextAnchor.MiddleCenter, true);
		styleButtons.Clear();
		styleButtons.Add(Compact(MenuTheme.MakeButtonRaw(styleRow, "VStyleMeasure", Tr("ИЗМЕРЕНИЕ"), 26f, new UnityAction(_003CBuildSceneBlock_003Eb__170_0), 11f, TextAlignmentOptions.Center, 16f), 11f));
		styleButtons.Add(Compact(MenuTheme.MakeButtonRaw(styleRow, "VStyleDots", Tr("ТОЧКИ"), 26f, new UnityAction(_003CBuildSceneBlock_003Eb__170_1), 11f, TextAlignmentOptions.Center, 16f), 11f));
		styleButtons.Add(Compact(MenuTheme.MakeButtonRaw(styleRow, "VStyleSurface", Tr("ПОВЕРХН."), 26f, new UnityAction(_003CBuildSceneBlock_003Eb__170_2), 11f, TextAlignmentOptions.Center, 16f), 11f));
		RectTransform glowRow = MenuTheme.MakeRow(col, "GlowRow", 4f, TextAnchor.MiddleCenter, true);
		glowBtn = Compact(MenuTheme.MakeButtonRaw(glowRow, "BtnGlow", Tr("СВЕЧЕНИЕ: ВКЛ"), 26f, new UnityAction(_003CBuildSceneBlock_003Eb__170_3), 12f, TextAlignmentOptions.Center, 16f), 12f);
		glowHint = MenuTheme.MakeLabelRaw(glowRow, "GlowHint", Tr("остывание показано ускоренно"), 11f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Center);
		LayoutElement glowFitter = glowHint.gameObject.AddComponent<LayoutElement>();
		glowFitter.minHeight = 28f;
		glowFitter.preferredHeight = 28f;
		glowHint.textWrappingMode = TextWrappingModes.Normal;
		glowHint.overflowMode = TextOverflowModes.Truncate;
		RectTransform stabRow = MenuTheme.MakeRow(col, "StabRow", 4f, TextAnchor.MiddleCenter, true);
		stabBtn = Compact(MenuTheme.MakeButtonRaw(stabRow, "BtnStabiliser", Tr("СТАБИЛИЗАТОР: ВЫКЛ"), 26f, new UnityAction(_003CBuildSceneBlock_003Eb__170_4), 12f, TextAlignmentOptions.Center, 16f), 12f);
		RectTransform bodiesRow = MenuTheme.MakeRow(col, "BodiesRow", 4f, TextAnchor.MiddleCenter, true);
		bodiesBtn = Compact(MenuTheme.MakeButtonRaw(bodiesRow, "BtnBodiesSeparate", Tr("ТЕЛА РАЗДЕЛЬНЫ: ВЫКЛ"), 26f, new UnityAction(_003CBuildSceneBlock_003Eb__170_5), 12f, TextAlignmentOptions.Center, 16f), 12f);
		RectTransform erosionRow = MenuTheme.MakeRow(col, "ErosionRow", 4f, TextAnchor.MiddleCenter, true);
		erosionBtn = Compact(MenuTheme.MakeButtonRaw(erosionRow, "BtnErosion", Tr("СТАЧИВАНИЕ: ВЫКЛ"), 26f, new UnityAction(_003CBuildSceneBlock_003Eb__170_6), 12f, TextAlignmentOptions.Center, 16f), 12f);
		RectTransform fractureRow = MenuTheme.MakeRow(col, "FractureRow", 4f, TextAnchor.MiddleCenter, true);
		fractureBtn = Compact(MenuTheme.MakeButtonRaw(fractureRow, "BtnFracture", Tr("ТРЕЩИНЫ: ВЫКЛ"), 26f, new UnityAction(_003CBuildSceneBlock_003Eb__170_7), 12f, TextAlignmentOptions.Center, 16f), 12f);
		RectTransform restraintRow = MenuTheme.MakeRow(col, "RestraintRow", 4f, TextAnchor.MiddleCenter, true);
		restraintBtn = Compact(MenuTheme.MakeButtonRaw(restraintRow, "BtnRestraint", Tr("УДЕРЖАНИЕ: ВЫКЛ"), 26f, new UnityAction(_003CBuildSceneBlock_003Eb__170_8), 12f, TextAlignmentOptions.Center, 16f), 12f);
		RectTransform sceneRow = MenuTheme.MakeRow(col, "SceneRow", 4f, TextAnchor.MiddleCenter, true);
		Button fit = Compact(MenuTheme.MakeButtonRaw(sceneRow, "BtnFit", Tr("ВПИСАТЬ"), 26f, new UnityAction(_003CBuildSceneBlock_003Eb__170_9), 12f, TextAlignmentOptions.Center, 16f), 12f);
		Button grid = Compact(MenuTheme.MakeButtonRaw(sceneRow, "BtnGrid", Tr("СЕТКА"), 26f, new UnityAction(_003CBuildSceneBlock_003Eb__170_10), 12f, TextAlignmentOptions.Center, 16f), 12f);
		Button anchors = Compact(MenuTheme.MakeButtonRaw(sceneRow, "AnchorShow", Tr("УЗЛЫ ЯКОРЯ"), 26f, new UnityAction(_003CBuildSceneBlock_003Eb__170_11), 12f, TextAlignmentOptions.Center, 16f), 12f);
		Button menu = Compact(MenuTheme.MakeButtonRaw(sceneRow, "BtnMenu", Tr("МЕНЮ (ESC)"), 26f, new UnityAction(ToggleEscapeMenu), 12f, TextAlignmentOptions.Center, 16f), 12f);
	}

	private void _003CBuildSceneBlock_003Eb__170_0()
	{
		PickStyle(Flat2DRuntimeView.Style.Izmerenie);
	}

	private void _003CBuildSceneBlock_003Eb__170_1()
	{
		PickStyle(Flat2DRuntimeView.Style.Dots);
	}

	private void _003CBuildSceneBlock_003Eb__170_2()
	{
		PickStyle(Flat2DRuntimeView.Style.Surface);
	}

	private void _003CBuildSceneBlock_003Eb__170_3()
	{
		SetGlow(!view.Glow);
	}

	private void _003CBuildSceneBlock_003Eb__170_4()
	{
		SetStabiliser(!model.stabiliser);
	}

	private void _003CBuildSceneBlock_003Eb__170_5()
	{
		SetBodiesSeparate(!model.bodiesSeparate);
	}

	private void _003CBuildSceneBlock_003Eb__170_6()
	{
		SetErosion(!model.erosion);
	}

	private void _003CBuildSceneBlock_003Eb__170_7()
	{
		SetFracture(!model.fracture);
	}

	private void _003CBuildSceneBlock_003Eb__170_8()
	{
		SetRestraint(!model.restraint);
	}

	private void _003CBuildSceneBlock_003Eb__170_9()
	{
		FrameAll();
		Refresh(Tr("вид вписан"));
	}

	private void _003CBuildSceneBlock_003Eb__170_10()
	{
		view.ShowGrid = !view.ShowGrid;
		Refresh(null);
	}

	private void _003CBuildSceneBlock_003Eb__170_11()
	{
		showPinned = !showPinned;
		previewVersion = -1;
		Refresh(Tr(showPinned ? "закреплённые узлы показаны" : "показ узлов выключен"));
	}

	public string SetRestraint(bool on)
	{
		if (solver != null)
		{
			string refusal = Tr("удержание меняется до «ОГОНЬ»: сначала ОСТАНОВИТЬ выстрел");
			Refresh(refusal);
			return refusal;
		}
		model.restraint = on;
		string message = Tr(on ? "удержание ВКЛ — броню у отверстия держит недостающее третье измерение; со следующего «ОГОНЬ»" : "удержание ВЫКЛ — плита как 2D-лезвие: гнётся целиком (прежняя физика)");
		Refresh(message);
		return message;
	}

	public string SetErosion(bool on)
	{
		if (solver != null)
		{
			string refusal = Tr("стачивание меняется до «ОГОНЬ»: сначала ОСТАНОВИТЬ выстрел");
			Refresh(refusal);
			return refusal;
		}
		model.erosion = on;
		string message = Tr(on ? "стачивание ВКЛ — 80 % сильно продеформированных частиц уходит из счёта; со следующего «ОГОНЬ»" : "стачивание ВЫКЛ — частицы не исчезают (прежняя физика)");
		Refresh(message);
		return message;
	}

	public string SetFracture(bool on)
	{
		if (solver != null)
		{
			string refusal = Tr("трещины меняются до «ОГОНЬ»: сначала ОСТАНОВИТЬ выстрел");
			Refresh(refusal);
			return refusal;
		}
		model.fracture = on;
		string message = Tr(on ? "трещины ВКЛ — полосы сдвига, разрушенное не держит растяжения, разброс прочности; со следующего «ОГОНЬ»" : "трещины ВЫКЛ — прежняя физика разрушения");
		Refresh(message);
		return message;
	}

	public string SetStabiliser(bool on)
	{
		if (solver != null)
		{
			string refusal = Tr("стабилизатор меняется до «ОГОНЬ»: сначала ОСТАНОВИТЬ выстрел");
			Refresh(refusal);
			return refusal;
		}
		model.stabiliser = on;
		string message = Tr(on ? "стабилизатор ВКЛ (искусственное напряжение, по растяжению) — действует со следующего «ОГОНЬ»" : "стабилизатор ВЫКЛ — как было по умолчанию");
		Refresh(message);
		return message;
	}

	public string SetBodiesSeparate(bool on)
	{
		if (solver != null)
		{
			string refusal = Tr("тела раздельны меняются до «ОГОНЬ»: сначала ОСТАНОВИТЬ выстрел");
			Refresh(refusal);
			return refusal;
		}
		model.bodiesSeparate = on;
		string message = Tr(on ? "тела РАЗДЕЛЬНЫ — толкают друг друга, но не тянут; действует со следующего «ОГОНЬ»" : "тела СЦЕПЛЕНЫ (одно сплошное тело) — как было по умолчанию");
		Refresh(message);
		return message;
	}

	public string SetGlow(bool on)
	{
		if (view == null)
		{
			return Tr("свечение: вида нет");
		}
		view.Glow = on;
		string message;
		if (on)
		{
			if (view.RenderStyle == Flat2DRuntimeView.Style.Izmerenie)
			{
				message = Tr("свечение ВКЛ, но к виду ИЗМЕРЕНИЕ оно не применяется: это прибор, его кадры не меняются");
			}
			else
			{
				message = Tr("свечение ВКЛ — остывание показано ускоренно; температуру решателя смотрите полем ТЕМПЕРАТУРА");
			}
		}
		else
		{
			message = Tr("свечение ВЫКЛ — частицы красятся только выбранным полем");
		}
		Refresh(message);
		return message;
	}

	public void PickStyle(Flat2DRuntimeView.Style s)
	{
		if (s == Flat2DRuntimeView.Style.Surface && Shader.Find("Flat2D/SurfaceScreen") == null)
		{
			Refresh(Tr("ПОВЕРХНОСТЬ недоступна: шейдер Flat2D/SurfaceScreen не найден в сборке — вид не переключён"));
			return;
		}
		view.RenderStyle = s;
		Refresh(Tr(((int)s != 0) ? (((int)s != 1) ? "вид: ПОВЕРХНОСТЬ — сплошное тело; замеры читайте в «ИЗМЕРЕНИЕ»" : "вид: ТОЧКИ — мягкие диски; зазор в шаг читается зазором") : "вид: ИЗМЕРЕНИЕ — точные квадраты 0.5 Δx, зазор в шаг читается зазором"));
	}

	private void BuildEscapeMenu(Transform parent)
	{
		GameObject canvasGo = new GameObject("EscapeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvasGo.transform.SetParent(parent, false);
		Canvas canvas = canvasGo.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceCamera;
		canvas.worldCamera = uiCam;
		canvas.planeDistance = 0.9f;
		canvas.sortingOrder = 500;
		CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;
		RectTransform menu = MenuTheme.MakeRect(canvasGo.transform, "EscapeMenu");
		MenuTheme.Stretch(menu);
		Image dim = MenuTheme.AddImage(menu, new Color(0.06f, 0.06f, 0.07f, 0.88f));
		dim.raycastTarget = true;
		TextMeshProUGUI title = MenuTheme.MakeLabel(menu, "Title", "pause.title", 32f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Center);
		MenuTheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(900f, 44f));
		title.textWrappingMode = TextWrappingModes.NoWrap;
		title.overflowMode = TextOverflowModes.Ellipsis;
		escNav = MenuTheme.MakeColumn(menu, "EscNav", 10f);
		MenuTheme.Place(escNav, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(460f, 340f));
		Button resume = MenuTheme.MakeButton(escNav, "EmResume", "pause.resume", 52f, new UnityAction(_003CBuildEscapeMenu_003Eb__201_0));
		TextMeshProUGUI saveNameCaption = MenuTheme.MakeLabelRaw(escNav, "SaveNameCaption", Tr("имя сохранения:"), 14f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		LayoutElement captionFitter = saveNameCaption.gameObject.AddComponent<LayoutElement>();
		captionFitter.minHeight = 20f;
		captionFitter.preferredHeight = 20f;
		saveNameCaption.textWrappingMode = TextWrappingModes.NoWrap;
		saveNameCaption.overflowMode = TextOverflowModes.Ellipsis;
		saveNameField = MakeInput(escNav, 460f, 40f, Tr("Flat2D рисунок"));
		Button save = MenuTheme.MakeButtonRaw(escNav, "EmSave", Localization.T("pause.save"), 52f, new UnityAction(EscSave), 22f, TextAlignmentOptions.Center, 16f);
		Button load = MenuTheme.MakeButtonRaw(escNav, "EmLoad", Localization.T("pause.load"), 52f, new UnityAction(EscLoad), 22f, TextAlignmentOptions.Center, 16f);
		exitBtn = MenuTheme.MakeButtonRaw(escNav, "EmQuit", Localization.T("sandbox.to_menu"), 52f, new UnityAction(EscExit), 22f, TextAlignmentOptions.Center, 16f);
		escStatus = MenuTheme.MakeLabelRaw(menu, "EscStatus", "", 15f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		MenuTheme.Place(escStatus.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1100f, 50f));
		escStatus.textWrappingMode = TextWrappingModes.Normal;
		escStatus.overflowMode = TextOverflowModes.Truncate;
		BuildLoadPanel(menu);
		escapeRoot = menu.gameObject;
		escapeRoot.SetActive(false);
	}

	private void _003CBuildEscapeMenu_003Eb__201_0()
	{
		SetEscapeOpen(false);
	}

	private void BuildConfirm(Transform escCanvas)
	{
		RectTransform rect = MenuTheme.MakeRect(escCanvas, "ConfirmFire");
		MenuTheme.Stretch(rect);
		Image image = MenuTheme.AddImage(rect, new Color(0.06f, 0.06f, 0.07f, 0.88f));
		image.raycastTarget = true;
		confirmLabel = MenuTheme.MakeLabelRaw(rect, "ConfirmText", "", 22f, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.Center);
		MenuTheme.Place(confirmLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1100f, 120f));
		confirmLabel.textWrappingMode = TextWrappingModes.Normal;
		confirmLabel.overflowMode = TextOverflowModes.Truncate;
		RectTransform row = MenuTheme.MakeRow(rect, "ConfirmRow", 16f, TextAnchor.MiddleCenter);
		MenuTheme.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(560f, 56f));
		Button fireConfirm = Compact(MenuTheme.MakeButtonRaw(row, "BtnConfirmFire", Tr("СТРЕЛЯТЬ"), 52f, new UnityAction(_003CBuildConfirm_003Eb__202_0), 18f, TextAlignmentOptions.Center, 16f), 18f);
		Width(fireConfirm, 260f);
		Button cancel = Compact(MenuTheme.MakeButtonRaw(row, "BtnCancelFire", Tr("ОТМЕНА"), 52f, new UnityAction(_003CBuildConfirm_003Eb__202_1), 18f, TextAlignmentOptions.Center, 16f), 18f);
		Width(cancel, 260f);
		confirmRoot = rect.gameObject;
		confirmRoot.SetActive(false);
	}

	private void _003CBuildConfirm_003Eb__202_0()
	{
		SetConfirmOpen(false);
		if (solver != null)
		{
			running = true;
			Refresh(Tr("счёт идёт"));
		}
	}

	private void _003CBuildConfirm_003Eb__202_1()
	{
		SetConfirmOpen(false);
		StopShot();
	}

	private void SetConfirmOpen(bool v)
	{
		if (confirmRoot != null)
		{
			confirmRoot.SetActive(v);
			SyncViewEnabled();
			if (v && confirmLabel != null)
			{
				confirmLabel.text = F("Оценка счёта: {0}.\nЭто дольше названного порога {1:F0} с. Начать выстрел?", etaText, confirmSeconds);
			}
		}
	}

	private void EscSave()
	{
		string text = ((saveNameField != null && !string.IsNullOrEmpty(saveNameField.text)) ? saveNameField.text : Tr("Flat2D рисунок"));
		SaveEntry saveEntry = model.Save(text);
		if (saveEntry == null)
		{
			EscStatus(Tr("не сохранилось"));
		}
		else
		{
			EscStatus(F("сохранено: {0}", saveEntry.displayName));
		}
	}

	private void BuildLoadPanel(RectTransform root)
	{
		loadPanel = MenuTheme.MakeRect(root, "EscLoadPanel");
		MenuTheme.Place(loadPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(640f, 520f));
		MenuTheme.AddImage(loadPanel, MenuTheme.Surface);
		TextMeshProUGUI title = MenuTheme.MakeLabelRaw(loadPanel, "EscLoadTitle", Tr("ВЫБЕРИТЕ СОХРАНЕНИЕ"), 20f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Center);
		MenuTheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(600f, 30f));
		title.textWrappingMode = TextWrappingModes.NoWrap;
		title.overflowMode = TextOverflowModes.Ellipsis;
		ScrollRect scroll = MenuTheme.MakeScrollList(loadPanel, "EscLoadScroll", out loadContent);
		MenuTheme.Stretch((RectTransform)scroll.transform, new Vector2(12f, 70f), new Vector2(-12f, -54f));
		Button back = MenuTheme.MakeButtonRaw(loadPanel, "EscLoadBack", Localization.T("menu.back"), 44f, new UnityAction(_003CBuildLoadPanel_003Eb__211_0), 22f, TextAlignmentOptions.Center, 16f);
		MenuTheme.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(260f, 44f));
		loadPanel.gameObject.SetActive(false);
	}

	private void _003CBuildLoadPanel_003Eb__211_0()
	{
		SetLoadOpen(false);
	}

	private void SetLoadOpen(bool open)
	{
		if (loadPanel != null)
		{
			loadPanel.gameObject.SetActive(open);
		}
		if (escNav != null)
		{
			escNav.gameObject.SetActive(!open);
		}
	}

	private void EscLoad()
	{
		List<SaveEntry> list = new List<SaveEntry>();
		foreach (SaveEntry item in SaveSystem.LoadUserSaves())
		{
			if (item.Family == SimFamily.Flat2D)
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			EscStatus(Tr("сохранений семьи Flat2D нет"));
			return;
		}
		int childCount = loadContent.childCount;
		for (int i = childCount - 1; i >= 0; i--)
		{
			UnityEngine.Object.DestroyImmediate(loadContent.GetChild(i).gameObject);
		}
		for (int j = 0; j < list.Count; j++)
		{
			_003C_003Ec__DisplayClass217_0 displayClass = new _003C_003Ec__DisplayClass217_0();
			displayClass._003C_003E4__this = this;
			displayClass.e = list[j];
			string date = displayClass.e.CreatedLocal().ToString("dd.MM.yyyy HH:mm", Localization.Culture);
			Button row = Compact(MenuTheme.MakeButtonRaw(loadContent, "EscLoadRow_" + j, displayClass.e.displayName + "   ·   " + date, 40f, new UnityAction(displayClass._003CEscLoad_003Eb__0), 15f, TextAlignmentOptions.Center, 16f), 15f);
		}
		SetLoadOpen(true);
		EscStatus(F("сохранений 2D: {0} — выберите, что загрузить", list.Count));
	}

	private void EscExit()
	{
		if (exitArmed)
		{
			if (solver != null)
			{
				StopShot();
			}
			AppFlow.OpenMenu();
			return;
		}
		exitArmed = true;
		TextMeshProUGUI label = MenuTheme.LabelOf(exitBtn);
		if (label != null)
		{
			label.text = Localization.T("pause.confirm_exit");
		}
		EscStatus(Localization.T("pause.unsaved_warning"));
	}

	private void EscStatus(string s)
	{
		if (escStatus != null)
		{
			escStatus.text = (s ?? "");
		}
	}

	public void OpenEscapeMenu()
	{
		SetEscapeOpen(true);
	}

	public void ToggleEscapeMenu()
	{
		SetEscapeOpen(!EscapeMenuOpen);
	}

	private void SetEscapeOpen(bool v)
	{
		if (escapeRoot != null)
		{
			escapeRoot.SetActive(v);
			SyncViewEnabled();
			if (v)
			{
				model.CancelStroke();
				strokeActive = false;
				SetLoadOpen(false);
				exitArmed = false;
				TextMeshProUGUI label = MenuTheme.LabelOf(exitBtn);
				if (label != null)
				{
					label.text = Localization.T("sandbox.to_menu");
				}
			}
			EscStatus("");
		}
	}

	private void SyncViewEnabled()
	{
		if (view != null)
		{
			view.enabled = !EscapeMenuOpen && !ConfirmOpen;
		}
	}

	private static TMP_InputField MakeInput(Transform parent, float width, float height, string initial)
	{
		RectTransform rect = MenuTheme.MakeRect(parent, "Input");
		LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
		layout.minWidth = width;
		layout.preferredWidth = width;
		layout.minHeight = height;
		layout.preferredHeight = height;
		return FillInput(rect, initial);
	}

	private static TMP_InputField MakeInputAt(Transform parent, Vector2 pos, Vector2 size, string initial)
	{
		RectTransform rect = MenuTheme.MakeRect(parent, "Input");
		MenuTheme.Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
		return FillInput(rect, initial);
	}

	private static TMP_InputField FillInput(RectTransform rt, string initial)
	{
		Image image = rt.gameObject.AddComponent<Image>();
		image.color = MenuTheme.SurfaceAlt;
		RectTransform area = MenuTheme.MakeRect(rt, "Text Area");
		area.anchorMin = Vector2.zero;
		area.anchorMax = Vector2.one;
		area.offsetMin = new Vector2(8f, 4f);
		area.offsetMax = new Vector2(-8f, -4f);
		TextMeshProUGUI label = MenuTheme.MakeLabelRaw(area, "Text", initial, 15f, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		MenuTheme.Stretch(label.rectTransform);
		label.textWrappingMode = TextWrappingModes.NoWrap;
		label.overflowMode = TextOverflowModes.Ellipsis;
		TMP_InputField field = rt.gameObject.AddComponent<TMP_InputField>();
		field.textViewport = area;
		field.textComponent = label;
		field.lineType = TMP_InputField.LineType.SingleLine;
		field.text = initial;
		return field;
	}

	private static Slider MakeSlider(Transform parent, string name, float min, float max, float value, float width, UnityAction<float> onChange)
	{
		RectTransform rect = MenuTheme.MakeRect(parent, name);
		LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
		layout.minWidth = width;
		layout.preferredWidth = width;
		layout.minHeight = 24f;
		layout.preferredHeight = 24f;
		Image image = MenuTheme.AddImage(rect, new Color(0f, 0f, 0f, 0f));
		image.raycastTarget = true;
		RectTransform track = MenuTheme.MakeRect(rect, "Track");
		track.anchorMin = new Vector2(0f, 0.5f);
		track.anchorMax = new Vector2(1f, 0.5f);
		track.pivot = new Vector2(0.5f, 0.5f);
		track.offsetMin = new Vector2(0f, -3f);
		track.offsetMax = new Vector2(0f, 3f);
		MenuTheme.AddImage(track, MenuTheme.SliderTrack);
		RectTransform fillArea = MenuTheme.MakeRect(rect, "Fill Area");
		fillArea.anchorMin = new Vector2(0f, 0.5f);
		fillArea.anchorMax = new Vector2(1f, 0.5f);
		fillArea.offsetMin = new Vector2(0f, -3f);
		fillArea.offsetMax = new Vector2(0f, 3f);
		RectTransform fill = MenuTheme.MakeRect(fillArea, "Fill");
		fill.anchorMin = Vector2.zero;
		fill.anchorMax = new Vector2(0f, 1f);
		fill.offsetMin = Vector2.zero;
		fill.offsetMax = Vector2.zero;
		MenuTheme.AddImage(fill, MenuTheme.SliderFill);
		RectTransform handleArea = MenuTheme.MakeRect(rect, "Handle Slide Area");
		MenuTheme.Stretch(handleArea);
		RectTransform handle = MenuTheme.MakeRect(handleArea, "Handle");
		handle.sizeDelta = new Vector2(16f, 22f);
		Image handleImage = MenuTheme.AddImage(handle, MenuTheme.TextPrimary);
		handleImage.raycastTarget = true;
		Slider slider = rect.gameObject.AddComponent<Slider>();
		slider.fillRect = fill;
		slider.handleRect = handle;
		slider.targetGraphic = image;
		slider.direction = Slider.Direction.LeftToRight;
		slider.minValue = min;
		slider.maxValue = max;
		slider.value = value;
		slider.onValueChanged.AddListener(onChange);
		return slider;
	}

	public void PickToolPublic(Flat2DDrawModel.Tool t)
	{
		PickTool(t);
	}

	public void SetBrushForAcceptance(float sizeM, int shape)
	{
		brush = Mathf.Clamp(sizeM, 0.0002f, 0.02f);
		if (brushSlider != null)
		{
			brushSlider.value = brush;
		}
		PickBrushShape(shape);
	}

	public void SelectShapeForAcceptance(int idx)
	{
		selectedRef = ((idx >= 0 && model.shapes != null && idx < model.shapes.Count) ? model.shapes[idx] : null);
		listVersion = -2;
		if (selectedRef != null)
		{
			Refresh(F("фигура {0} выбрана", idx + 1));
		}
		else
		{
			Refresh(Tr("выбор снят"));
		}
	}

	private void PickTool(Flat2DDrawModel.Tool t)
	{
		tool = t;
		rubberStart = null;
		rulerA = null;
		ReleaseRound();
		heldDecal = null;
		model.CancelStroke();
		strokeActive = false;
		if (view != null)
		{
			view.ClearCursor();
		}
		Refresh(F("инструмент: {0}", ToolName(t)));
	}

	private void PickSnap(Flat2DDrawModel.Snap s)
	{
		snap = s;
		string mode = (((int)s != 0) ? (((int)s != 1) ? "свободно" : "к углу 15°") : "к сетке Δx");
		Refresh(F("привязка: {0}", Tr(mode)));
	}

	private void PickDecal(string id)
	{
		ReleaseRound();
		heldDecal = ((heldDecal != id) ? id : null);
		if (heldDecal == null)
		{
			if (view != null)
			{
				view.ClearCursor();
			}
			Refresh(Tr("декаль убрана из руки"));
			return;
		}
		string text;
		if (Flat2DCrew.IsCrewDecal(heldDecal))
		{
			text = F("{0} в руке: вещество «экипаж», органы раскрашены; масштаб модели {1} (стоя {2:F0} мм); клик ставит", DecalName(heldDecal), Flat2DModelScale.Label, Flat2DModelScale.StandingOnCanvasM * 1000f);
		}
		else
		{
			text = F("декаль {0} в руке: превью у курсора, колесо вертит ({1} — точно, {2} — размер), клик ставит", DecalName(heldDecal), ControlBindings.ModifierName(ControlBindings.Modifier(ControlModifier.FlatPrecise)), ControlBindings.ModifierName(ControlBindings.Modifier(ControlModifier.FlatBrushSize)));
		}
		Refresh(text);
	}

	public void HoldDecal(string id)
	{
		PickDecal(id);
	}

	private void StartRuler()
	{
		tool = Flat2DDrawModel.Tool.Polygon;
		rulerA = null;
		rulerArmed = true;
		Refresh(Tr("линейка: два клика по холсту — расстояние в мм"));
	}

	private void PickRole(Flat2DFill.Role r)
	{
		ReleaseRound();
		pickedRole = r;
		if (r == Flat2DFill.Role.Anchor)
		{
			Refresh(Tr("роль: ЯКОРЬ — область закрепления, частиц не даёт"));
		}
		else
		{
			Refresh(F("роль: {0}", Flat2DDrawModel.RoleName(r)));
		}
	}

	private void UndoPressed()
	{
		if (model.open != null && model.open.pts.Count > 0)
		{
			model.Undo();
			Refresh(Tr("вершина снята"));
		}
		else if (model.UndoStep())
		{
			listVersion = -2;
			Refresh(F("отменено (в стопке {0})", model.UndoDepthNow));
		}
		else
		{
			Refresh(Tr("отменять нечего"));
		}
	}

	public void ClickWorld(Vector2 world)
	{
		ApplyToolAt(world);
	}

	public string ClickWorldReturningWarning(Vector2 world)
	{
		return ApplyToolAt(world);
	}

	private string ApplyToolAt(Vector2 raw)
	{
		if (solver != null)
		{
			return null;
		}
		Vector2? nullable = null;
		if (model.open != null && model.open.pts.Count > 0)
		{
			nullable = model.open.pts[model.open.pts.Count - 1];
		}
		Vector2 w = Flat2DDrawModel.SnapPoint(raw, snap, model.dx, nullable);
		if (rulerArmed)
		{
			if (!rulerA.HasValue)
			{
				rulerA = w;
				Refresh(Tr("линейка: вторая точка"));
				return null;
			}
			Vector2 a = rulerA.Value;
			float mm = (w - a).magnitude * 1000f;
			if (rulerLabel != null)
			{
				rulerLabel.text = F("линейка: {0:F2} мм = {1:F1} Δx", mm, mm / (model.dx * 1000f));
			}
			rulerA = null;
			rulerArmed = false;
			Refresh(F("линейка: {0:F2} мм", mm));
			return null;
		}
		if (Flat2DRounds.IsRoundId(heldDecal))
		{
			return PlaceHeldRound(w);
		}
		if (heldDecal != null)
		{
			Vector2 velocity = ((pickedRole == Flat2DFill.Role.Projectile) ? projectileVel : Vector2.zero);
			if (model.PlaceDecal(heldDecal, w, decalT, decalH, decalAngle, pickedMat, pickedRole, velocity, "декаль " + heldDecal, pickedPaint) == 0)
			{
				Refresh(Tr("декали с таким именем нет"));
				return null;
			}
			listVersion = -2;
			string warning = model.CeilingWarning();
			string text = warning;
			if (warning == null)
			{
				object[] array = new object[2]
				{
					DecalName(heldDecal),
					((decalAngle == 0f) ? Tr("без поворота") : string.Format("{0:F0}°", decalAngle))
				};
				text = F("декаль {0} поставлена ({1}); она всё ещё в руке", array);
			}
			Refresh(text);
			return warning;
		}
		switch (tool)
		{
		case Flat2DDrawModel.Tool.Polygon:
			return AddVertexAt(w);
		case Flat2DDrawModel.Tool.Rect:
		{
			if (!rubberStart.HasValue)
			{
				rubberStart = w;
				Refresh(Tr("вторая точка"));
				return null;
			}
			Vector2 a2 = rubberStart.Value;
			rubberStart = null;
			return Commit(Flat2DDrawModel.RectPts(a2, w), "прямоугольник");
		}
		case Flat2DDrawModel.Tool.Circle:
		{
			if (!rubberStart.HasValue)
			{
				rubberStart = w;
				Refresh(Tr("радиус: второй клик"));
				return null;
			}
			Vector2 centre = rubberStart.Value;
			rubberStart = null;
			float radius = (w - centre).magnitude;
			return Commit(Flat2DDrawModel.CirclePts(centre, radius, 24), "круг");
		}
		case Flat2DDrawModel.Tool.Brush:
		case Flat2DDrawModel.Tool.Eraser:
		{
			Vector2 velocity2 = ((pickedRole == Flat2DFill.Role.Projectile) ? projectileVel : Vector2.zero);
			model.BeginStroke(pickedMat, pickedRole, w, brush, brushShape, tool == Flat2DDrawModel.Tool.Eraser, velocity2);
			model.openStroke.paint = pickedPaint;
			Flat2DDrawModel.Shape shape = model.EndStroke();
			listVersion = -2;
			string warning2 = model.CeilingWarning();
			string text2 = warning2;
			if (warning2 == null)
			{
				text2 = Tr((shape != null && shape.erase) ? "ластик: штамп стёр узлы под собой" : "кисть: штамп поставлен");
			}
			Refresh(text2);
			return warning2;
		}
		default:
			return null;
		}
	}

	private string Commit(Vector2[] pts, string what)
	{
		model.Checkpoint();
		Vector2 velocity = ((pickedRole == Flat2DFill.Role.Projectile) ? projectileVel : Vector2.zero);
		model.AddShape(pts, pickedMat, pickedRole, velocity, what, pickedPaint);
		if (mirror)
		{
			model.AddShape(Flat2DDrawModel.MirrorX(pts, (view.Lo.x + view.Hi.x) * 0.5f), pickedMat, pickedRole, velocity, what + " (зеркало)", pickedPaint);
		}
		listVersion = -2;
		string warning = model.CeilingWarning();
		string text = warning;
		if (warning == null)
		{
			text = F("{0}: фигура добавлена", Tr(what));
		}
		Refresh(text);
		return warning;
	}

	private string AddVertexAt(Vector2 w)
	{
		if (model.open == null)
		{
			model.BeginShape(pickedMat, pickedRole);
			model.open.paint = pickedPaint;
		}
		if (model.open.role == Flat2DFill.Role.Projectile)
		{
			model.open.velocity = projectileVel;
		}
		string text = model.AddVertex(w);
		if (text != null)
		{
			Refresh(text);
			return text;
		}
		Refresh(F("вершина {0}: ({1:F1}, {2:F1}) мм", model.open.pts.Count, w.x * 1000f, w.y * 1000f));
		return null;
	}

	public string PickMaterialByName(string display)
	{
		foreach (Flat2DCatalogue.Row row in rowsById.Values)
		{
			if (row.display == display)
			{
				return PickCatalogue(row.id);
			}
		}
		foreach (Flat2DDrawModel.MaterialSlot slot in Flat2DDrawModel.MaterialSlots())
		{
			if (slot.display == display)
			{
				if (!slot.usable)
				{
					ShowReason(Tr(slot.display) + ": " + Tr(slot.reason));
					return slot.reason;
				}
				pickedMat = slot.id;
				ShowReason("");
				pickedRole = Flat2DFill.Role.Armour;
				Refresh(F("вещество: {0}", Tr(slot.display)));
				return null;
			}
		}
		return "вещества с такой подписью в пикере нет";
	}

	public void LoadPreset(string id)
	{
		model.Checkpoint();
		model.LoadPreset(id);
		Vector2? nullable = model.LastProjectileVelocity();
		if (nullable.HasValue)
		{
			projectileVel = nullable.Value;
		}
		SyncSlidersFromState();
		selectedRef = null;
		listVersion = -2;
		Tick();
		FrameAll();
		Tick();
		Refresh(F("пресет загружен: {0}", PresetName(id)));
	}

	public void OpenSectionPublic(string id)
	{
		OpenSection(id);
		Refresh(null);
	}

	public void SetField(int mode)
	{
		view.FieldMode = mode;
		Refresh(Tr((mode != 6) ? "поле: " : "поле: ТЕМПЕРАТУРА — температура решателя как есть, шкала 300 K … T_melt сцены (свечение — это показ, прибор — это поле)") + mode);
	}

	public void StepOnce()
	{
		StepSolver(1);
	}

	public void SetVelocity(float v)
	{
		_003C_003Ec__DisplayClass289_0 _003C_003Ec__DisplayClass289_ = new _003C_003Ec__DisplayClass289_0();
		_003C_003Ec__DisplayClass289_.v = v;
		EditVelocity(new Func<Vector2, Vector2>(_003C_003Ec__DisplayClass289_._003CSetVelocity_003Eb__0));
		SyncSlidersFromState();
	}

	public ulong FireAndHash(int steps)
	{
		Fire();
		if (solver == null)
		{
			return 0uL;
		}
		solver.Steps(steps);
		int num = (MutProbeKeepTrace ? (steps + 40) : steps);
		lastHashAlive = solver.ProbeAlive(num, out lastHashDetail);
		return solver.StateHash();
	}

	public void TogglePlay()
	{
		if (solver == null)
		{
			Refresh(Tr("сначала «ОГОНЬ» — он строит сцену и запускает счёт"));
			return;
		}
		running = !running;
		if (!running && solver.Erosion)
		{
			erodedShown = solver.ErodedCount;
		}
		Refresh(Tr(running ? "счёт идёт" : "пауза"));
	}

	public void Play()
	{
		if (solver == null)
		{
			return;
		}
		running = true;
		Refresh(Tr("счёт идёт"));
	}

	private void StepSolver(int n)
	{
		if (solver == null)
		{
			Refresh(Tr("выстрела нет"));
			return;
		}
		solver.Steps(n);
		totalSteps += n;
		view.NotifyStepped(n);
		if (solver.Erosion && (erodedShown < 0 || totalSteps / Math.Max(1, n) % ErodedEveryFrames == 0))
		{
			erodedShown = solver.ErodedCount;
		}
		Refresh(null);
	}

	private void Update()
	{
		if (running && solver != null)
		{
			StepSolver(stepsPerFrame);
		}
		Tick();
		TrackPointer();
		HandleTouchInput();
	}

	private void HandleTouchInput()
	{
		if (!TouchInputHelper.IsTouchDevice)
		{
			return;
		}
		if (Input.touchCount == 0)
		{
			if (strokeActive)
			{
				SurfaceRelease();
			}
			touchStrokeFinger = -1;
			touchIgnoreFinger = -1;
			touchPinchDist = -1f;
			return;
		}
		if (EscapeMenuOpen || ConfirmOpen)
		{
			if (strokeActive)
			{
				SurfaceRelease();
			}
			touchStrokeFinger = -1;
			touchPinchDist = -1f;
			return;
		}
		Camera cam = ((canvas != null) ? canvas.worldCamera : null);
		Rect rect = SurfaceScreenRect();
		if (Input.touchCount >= 2)
		{
			if (strokeActive)
			{
				SurfaceRelease();
			}
			touchStrokeFinger = -1;
			Touch t0 = Input.GetTouch(0);
			Touch t1 = Input.GetTouch(1);
			Vector2 mid = (t0.position + t1.position) * 0.5f;
			float dist = Vector2.Distance(t0.position, t1.position);
			if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began || touchPinchDist < 0f)
			{
				touchPinchDist = dist;
				touchPanMid = mid;
				touchIgnoreFinger = t0.fingerId;
				return;
			}
			float pinch = dist - touchPinchDist;
			if (Mathf.Abs(pinch) > 0.5f)
			{
				SurfaceZoom(pinch / TouchInputHelper.PinchPixelsPerNotch, mid, cam);
			}
			Vector2 pan = mid - touchPanMid;
			if (pan.sqrMagnitude > 4f)
			{
				SurfacePan(pan);
			}
			touchPinchDist = dist;
			touchPanMid = mid;
			return;
		}
		Touch t = Input.GetTouch(0);
		if (touchStrokeFinger == -1)
		{
			if (t.phase == TouchPhase.Began && t.fingerId != touchIgnoreFinger && rect.Contains(t.position))
			{
				SurfacePress(t.position, cam);
				touchStrokeFinger = t.fingerId;
			}
			else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
			{
				touchIgnoreFinger = -1;
				touchPinchDist = -1f;
			}
			return;
		}
		if (t.fingerId != touchStrokeFinger)
		{
			touchStrokeFinger = -1;
			return;
		}
		if (t.phase == TouchPhase.Moved)
		{
			SurfaceDragPaint(t.position, cam);
		}
		else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
		{
			if (strokeActive)
			{
				SurfaceRelease();
				if (rect.Contains(t.position))
				{
					SurfaceClick(t.position, cam);
				}
				else
				{
					suppressNextClick = false;
				}
			}
			else if (rect.Contains(t.position))
			{
				SurfaceClick(t.position, cam);
			}
			touchStrokeFinger = -1;
			touchIgnoreFinger = -1;
			touchPinchDist = -1f;
		}
	}

	private void TrackPointer()
	{
		if (view != null)
		{
			if (solver == null && !EscapeMenuOpen && !ConfirmOpen && (heldDecal != null || tool == Flat2DDrawModel.Tool.Brush || tool == Flat2DDrawModel.Tool.Eraser))
			{
				Vector2 screen = TouchInputHelper.PrimaryPosition;
				Rect rect = SurfaceScreenRect();
				if (rect.x <= screen.x && screen.x < rect.x + rect.width && rect.y <= screen.y && screen.y < rect.y + rect.height)
				{
					PointCursorAt(ScreenToWorld(screen, (canvas != null) ? canvas.worldCamera : null));
					return;
				}
			}
			view.ClearCursor();
		}
	}

	public void PointCursorAt(Vector2 w)
	{
		if (view == null)
		{
			return;
		}
		if (heldDecal != null)
		{
			BuildDecalCursor(w);
			return;
		}
		if (tool == Flat2DDrawModel.Tool.Brush || tool == Flat2DDrawModel.Tool.Eraser)
		{
			BuildBrushCursor(w);
			return;
		}
		view.ClearCursor();
	}

	public void Tick()
	{
		if (view != null && surfaceRect != null)
		{
			view.ViewportPx = SurfaceScreenRect();
			UpdateFrame();
		}
	}

	public void StopShot()
	{
		if (ConfirmOpen)
		{
			SetConfirmOpen(false);
		}
		if (view != null && solver == null)
		{
			view.Solver = null;
		}
		if (solver == null)
		{
			Refresh(Tr("выстрела нет — останавливать нечего"));
			return;
		}
		running = false;
		view.Solver = null;
		view.SetLivePaint(null);
		view.ResetGlow();
		solver.Dispose();
		solver = null;
		totalSteps = 0;
		previewVersion = -1;
		Refresh(Tr("выстрел остановлен — можно править фигуры и стрелять снова"));
	}

	public void EditVelocity(Func<Vector2, Vector2> f)
	{
		projectileVel = f(projectileVel);
		if (model.open != null && model.open.role == Flat2DFill.Role.Projectile)
		{
			model.open.velocity = projectileVel;
		}
		else if (model.openStroke != null && model.openStroke.role == Flat2DFill.Role.Projectile)
		{
			model.openStroke.velocity = projectileVel;
		}
		else
		{
			model.SetLastProjectileVelocity(projectileVel);
		}
		Refresh(null);
	}

	private void SyncSlidersFromState()
	{
		if (velSlider != null)
		{
			velSlider.value = Mathf.Clamp(projectileVel.magnitude, velSlider.minValue, velSlider.maxValue);
		}
		if (velField != null)
		{
			velField.SetTextWithoutNotify(string.Format("{0:F0}", projectileVel.magnitude));
		}
		if (brushSlider != null)
		{
			brushSlider.value = brush;
		}
	}

	public void LoadFromEntry(SaveEntry entry)
	{
		model.FromPayload(entry.payload);
		Vector2? nullable = model.LastProjectileVelocity();
		if (nullable.HasValue)
		{
			projectileVel = nullable.Value;
		}
		selectedRef = null;
		listVersion = -2;
		SyncSlidersFromState();
		FrameAll();
		Refresh(F("загружено: {0}", entry.displayName));
	}

	public bool SetLatticeStep(float dx)
	{
		if (dx <= 0f || float.IsInfinity(dx))
		{
			UnityEngine.Debug.LogError(string.Format("[Flat2DDrawScreen] SetLatticeStep: шаг {0} м отклонён; остаётся ", dx) + string.Format("{0:F4} мм.", model.dx * 1000f));
			return false;
		}
		if (solver != null)
		{
			UnityEngine.Debug.LogError("[Flat2DDrawScreen] SetLatticeStep: идёт выстрел — шаг решётки под ним не меняется; " + string.Format("остаётся {0:F4} мм.", model.dx * 1000f));
			return false;
		}
		float num = model.dx;
		if (dx != num)
		{
			model.SetDx(dx);
			zoomAccum = Mathf.Clamp(dx * zoomAccum / num, 1f, 64f);
			int num2 = Convert.ToInt32(zoomAccum);
			if (num2 < 1)
			{
				num2 = 1;
			}
			else if (num2 > 64)
			{
				num2 = 64;
			}
			zoomK = num2;
			Refresh(null);
		}
		return true;
	}

	public void ApplyNewSceneDefaults()
	{
		model.erosion = true;
		model.fracture = true;
		model.restraint = true;
		Refresh(null);
	}

	public void SurfaceClick(Vector2 screenPos, Camera cam)
	{
		if (suppressNextClick)
		{
			suppressNextClick = false;
			return;
		}
		ApplyToolAt(ScreenToWorld(screenPos, cam));
	}

	public void SurfacePress(Vector2 screenPos, Camera cam)
	{
		suppressNextClick = false;
		if (solver != null || EscapeMenuOpen || ConfirmOpen || heldDecal != null || (tool != Flat2DDrawModel.Tool.Brush && tool != Flat2DDrawModel.Tool.Eraser))
		{
			return;
		}
		Vector2 w = ScreenToWorld(screenPos, cam);
		Vector2 velocity = ((pickedRole == Flat2DFill.Role.Projectile) ? projectileVel : Vector2.zero);
		model.BeginStroke(pickedMat, pickedRole, w, brush, brushShape, tool == Flat2DDrawModel.Tool.Eraser, velocity);
		model.openStroke.paint = pickedPaint;
		strokeActive = true;
		Refresh(Tr((tool != Flat2DDrawModel.Tool.Eraser) ? "кисть: ведите — след красит" : "ластик: ведите — след стирает"));
	}

	public bool SurfaceDragPaint(Vector2 screenPos, Camera cam)
	{
		if (strokeActive && model != null && model.openStroke != null)
		{
			Vector2 w = ScreenToWorld(screenPos, cam);
			string message = model.ExtendStroke(w);
			PointCursorAt(w);
			Refresh(message);
			return true;
		}
		return false;
	}

	public void SurfaceRelease()
	{
		if (!strokeActive)
		{
			return;
		}
		strokeActive = false;
		suppressNextClick = true;
		Flat2DDrawModel.Shape shape = model.EndStroke();
		listVersion = -2;
		string text = model.CeilingWarning();
		if (text == null && shape != null)
		{
			text = Tr(shape.erase ? "ластик: мазок закончен — узлы под ним пусты" : "кисть: мазок закончен");
		}
		Refresh(text);
	}

	public void SurfacePan(Vector2 deltaPx)
	{
		float dx = ((solver != null) ? solver.Dx : model.dx);
		float num = (float)Math.Max(1, zoomK);
		viewCentre.x -= dx / num * deltaPx.x;
		viewCentre.y -= dx / num * deltaPx.y;
		Refresh(null);
	}

	public void SurfaceZoom(float scroll, Vector2 screenPos, Camera cam)
	{
		ScrollSurface(scroll, ControlBindings.Held(ControlModifier.FlatBrushSize), ControlBindings.Held(ControlModifier.FlatPrecise), screenPos, cam);
	}

	public static float WheelNotches(float delta)
	{
		InputSystemUIInputModule inputSystemUIInputModule = EventSystem.current?.currentInputModule as InputSystemUIInputModule;
		if (inputSystemUIInputModule != null)
		{
			float num = Math.Abs(inputSystemUIInputModule.scrollDeltaPerTick);
			if (num < 1E-05f)
			{
				return 0f;
			}
			delta /= num;
		}
		return delta;
	}

	public void ScrollSurface(float scroll, bool shift, Vector2 screenPos, Camera cam)
	{
		ScrollSurface(scroll, shift, ControlBindings.Held(ControlModifier.FlatPrecise), screenPos, cam);
	}

	public void ScrollSurface(float scroll, bool shift, bool alt, Vector2 screenPos, Camera cam)
	{
		if (Math.Abs(scroll) < 1e-06f)
		{
			return;
		}
		float num = ((scroll < 0f) ? (-1f) : 1f);
		if (heldDecal != null && !shift)
		{
			if (!alt)
			{
				decalAngle = (float)Math.Round((num * 15f + decalAngle) / 15f) * 15f;
			}
			else
			{
				decalAngle += num;
			}
			decalAngle %= 360f;
			if (decalAngle > 180f)
			{
				decalAngle -= 360f;
			}
			if (decalAngle <= -180f)
			{
				decalAngle += 360f;
			}
			PointCursorAt(ScreenToWorld(screenPos, cam));
			if (Flat2DRounds.IsRoundId(heldDecal))
			{
				Refresh(F("{0}: угол {1:F0}° (скорость — вдоль острия)", heldRoundName, decalAngle));
				return;
			}
			Refresh(F("декаль {0}: угол {1:F0}°", DecalName(heldDecal), decalAngle));
			return;
		}
		if (shift)
		{
			if (heldDecal == null || Flat2DRounds.IsRoundId(heldDecal))
			{
				brush = Mathf.Clamp(Mathf.Pow(1.2f, scroll) * brush, 0.0002f, 0.02f);
				if (brushSlider != null)
				{
					brushSlider.value = brush;
				}
				if (heldDecal != null || tool == Flat2DDrawModel.Tool.Brush || tool == Flat2DDrawModel.Tool.Eraser)
				{
					PointCursorAt(ScreenToWorld(screenPos, cam));
				}
			}
			else
			{
				float num2 = Mathf.Pow(1.2f, scroll);
				decalT = Mathf.Clamp(num2 * decalT, 0.0005f, 0.1f);
				decalH = Mathf.Clamp(num2 * decalH, 0.002f, 0.4f);
				SyncDecalSliders();
				PointCursorAt(ScreenToWorld(screenPos, cam));
			}
			Refresh(null);
			return;
		}
		if (solver == null && SelectedIndex > -1)
		{
			RotateSelected(num, alt);
			return;
		}
		Vector2 w = ScreenToWorld(screenPos, cam);
		zoomAccum = Mathf.Pow(1.2f, ControlBindings.WheelSign(ControlScope.Flat2D) * scroll) * zoomAccum;
		if (zoomAccum < 1f)
		{
			zoomAccum = 1f;
		}
		else if (zoomAccum > 64f)
		{
			zoomAccum = 64f;
		}
		int num3 = Convert.ToInt32(zoomAccum);
		if (num3 < 1)
		{
			num3 = 1;
		}
		else if (num3 > 64)
		{
			num3 = 64;
		}
		if (num3 != zoomK)
		{
			zoomK = num3;
			UpdateFrame();
			Vector2 w2 = ScreenToWorld(screenPos, cam);
			viewCentre.x += w.x - w2.x;
			viewCentre.y += w.y - w2.y;
			UpdateFrame();
		}
		Refresh(null);
	}

	private static float NextAngle(float angle, float dir, bool alt)
	{
		float num;
		if (alt)
		{
			num = angle + dir;
		}
		else
		{
			num = (float)Math.Round((angle + dir * 15f) / 15f) * 15f;
		}
		num %= 360f;
		if (num > 180f)
		{
			num += -360f;
		}
		if (num <= -180f)
		{
			num += 360f;
		}
		return num;
	}

	private void RotateSelected(float dir, bool alt)
	{
		if (selectedRef == null)
		{
			return;
		}
		int num = model.shapes.IndexOf(selectedRef);
		if (num < 0)
		{
			return;
		}
		Flat2DDrawModel.Shape shape = model.shapes[num];
		if (shape.locked)
		{
			Refresh(Tr("фигура под замком — снимите замок, чтобы вертеть"));
			return;
		}
		float time = Time.realtimeSinceStartup;
		if (lastRotatedRef != shape || time - lastRotateAt > 1.5f)
		{
			model.Checkpoint();
		}
		lastRotatedRef = shape;
		lastRotateAt = time;
		float num2 = (alt ? 1f : 15f);
		model.RotateShape(num, num2 * dir, !alt);
		listVersion = -2;
		string text = F("фигура {0}: угол {1:F0}°", num + 1, shape.angleDeg);
		text += Tr(alt ? " (точный шаг 1°)" : " (шаг 15°)");
		Refresh(text);
	}

	private static bool ShiftHeld()
	{
		return ControlBindings.Held(ControlModifier.FlatBrushSize);
	}

	private static bool AltHeld()
	{
		return ControlBindings.Held(ControlModifier.FlatPrecise);
	}

	private Vector2 ScreenToWorld(Vector2 sp, Camera cam)
	{
		if (surfaceRect == null)
		{
			return Vector2.zero;
		}
		Camera camera = ((cam != null) ? cam : ((canvas != null) ? canvas.worldCamera : null));
		RectTransformUtility.ScreenPointToLocalPointInRectangle(surfaceRect, sp, camera, out Vector2 localPoint);
		Rect rect = surfaceRect.rect;
		float num = localPoint.x - rect.x;
		float num2 = localPoint.y - rect.y;
		num /= Mathf.Max(rect.width, 1E-06f);
		num2 /= Mathf.Max(rect.height, 1E-06f);
		return new Vector2((view.Hi.x - view.Lo.x) * num + view.Lo.x, (view.Hi.y - view.Lo.y) * num2 + view.Lo.y);
	}

	public Vector2 WorldToRenderPx(Vector2 w)
	{
		Rect viewportPx = view.ViewportPx;
		float num = (w.x - view.Lo.x) / Mathf.Max(view.Hi.x - view.Lo.x, 1E-09f);
		float num2 = (w.y - view.Lo.y) / Mathf.Max(view.Hi.y - view.Lo.y, 1E-09f);
		return new Vector2(num * viewportPx.width + viewportPx.x, num2 * viewportPx.height + viewportPx.y);
	}

	public Vector2 ScreenToWorldPublic(Vector2 sp)
	{
		return ScreenToWorld(sp, (canvas != null) ? canvas.worldCamera : null);
	}

	public Rect SurfaceScreenRect()
	{
		if (surfaceRect == null)
		{
			return default(Rect);
		}
		Vector3[] array = new Vector3[4];
		surfaceRect.GetWorldCorners(array);
		Camera camera = (canvas != null) ? canvas.worldCamera : null;
		if (camera == null)
		{
			return new Rect(array[0].x, array[0].y, array[2].x - array[0].x, array[2].y - array[0].y);
		}
		Vector3 vector = camera.WorldToScreenPoint(array[0]);
		Vector3 vector2 = camera.WorldToScreenPoint(array[2]);
		return new Rect(vector.x, vector.y, vector2.x - vector.x, vector2.y - vector.y);
	}

	public void FrameAll()
	{
		List<Flat2DFill.Poly> polys = model.Polys(includeOpenProvisional: true);
		if (polys.Count == 0)
		{
			return;
		}
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = float.MinValue;
		float num4 = float.MinValue;
		bool flag = false;
		foreach (Flat2DFill.Poly poly in polys)
		{
			if (poly.path != null && poly.path.Length >= 1)
			{
				for (int i = 0; i < poly.path.Length; i++)
				{
					num = Mathf.Min(num, poly.path[i].x - poly.halfWidth);
					num2 = Mathf.Min(num2, poly.path[i].y - poly.halfWidth);
					num3 = Mathf.Max(num3, poly.path[i].x + poly.halfWidth);
					num4 = Mathf.Max(num4, poly.path[i].y + poly.halfWidth);
					flag = true;
				}
			}
			else if (poly.pts != null)
			{
				for (int j = 0; j < poly.pts.Length; j++)
				{
					num = Mathf.Min(num, poly.pts[j].x);
					num2 = Mathf.Min(num2, poly.pts[j].y);
					num3 = Mathf.Max(num3, poly.pts[j].x);
					num4 = Mathf.Max(num4, poly.pts[j].y);
					flag = true;
				}
			}
		}
		if (!flag)
		{
			return;
		}
		viewCentre = new Vector2((num3 + num) * 0.5f, (num4 + num2) * 0.5f);
		if (view.ViewportPx.width >= 1f)
		{
			float dx = ShownDx;
			float num5 = (num3 - num) * 1.25f;
			float num6 = (num4 - num2) * 1.25f;
			num5 = Mathf.Max(num5, dx);
			num6 = Mathf.Max(num6, dx);
			float num7 = view.ViewportPx.width * dx / num5;
			float num8 = view.ViewportPx.height * dx / num6;
			if (num8 <= num7)
			{
				num7 = num8;
			}
			zoomAccum = Mathf.Clamp(num7, 1f, 64f);
			zoomK = Mathf.Clamp(Convert.ToInt32(zoomAccum), 1, 64);
			UpdateFrame();
		}
	}

	private void UpdateFrame()
	{
		float dx = ((solver != null) ? solver.Dx : model.dx);
		if (view.Dx != dx)
		{
			view.Dx = dx;
		}
		Rect viewportPx = view.ViewportPx;
		if (viewportPx.width < 1f || viewportPx.height < 1f)
		{
			return;
		}
		dx /= (float)Math.Max(1, zoomK);
		view.Lo = new Vector2(viewCentre.x - viewportPx.width * dx * 0.5f, viewCentre.y - viewportPx.height * dx * 0.5f);
		view.Hi = new Vector2(viewCentre.x + viewportPx.width * dx * 0.5f, viewCentre.y + viewportPx.height * dx * 0.5f);
	}

	private Flat2DSolver BuildShotSolver(out Flat2DFill.Result fill, out Flat2DMaterialBlock[] mats, out Vector2 lo, out Vector2 hi)
	{
		fill = model.BuildFireFill(out mats);
		lo = Vector2.zero;
		hi = Vector2.zero;
		if (!fill.ok)
		{
			return null;
		}
		Flat2DSolver flat2DSolver = new Flat2DSolver(solverShader);
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < fill.vel.Length; i++)
		{
			num = Math.Max(num, (float)Math.Sqrt(fill.vel[i].x * fill.vel[i].x + fill.vel[i].y * fill.vel[i].y));
		}
		for (int j = 0; j < mats.Length; j++)
		{
			num2 = Math.Max(num2, mats[j].LongitudinalWaveSpeed);
		}
		num = Math.Max(num, 1f);
		fixedDt = model.dx * 1.3f * 0.3f / (num + num2);
		plannedSteps = PlannedStepsFor(model.dx);
		flat2DSolver.FixedTimeStep = fixedDt;
		flat2DSolver.AnchorMask = fill.pinned;
		flat2DSolver.ArtificialStress = model.stabiliser;
		flat2DSolver.ArtStressActivation = Flat2DSolver.ArtActivation.Tension;
		flat2DSolver.SeparateBodies = model.bodiesSeparate;
		if (model.bodiesSeparate)
		{
			flat2DSolver.BodyIds = Flat2DBodies.Assign(fill.pos, fill.ownerRole, model.dx);
		}
		flat2DSolver.Erosion = model.erosion;
		flat2DSolver.Fracture = model.fracture;
		if (model.fracture)
		{
			ContinuumMaterialId[] array = model.FireMaterialIds();
			flat2DSolver.Xi = Flat2DFractureSetup.BuildXi(fill.pos, fill.mat, array, model.dx);
			flat2DSolver.AsbPerMaterial = Flat2DFractureSetup.AsbPerBlock(array);
			if (!model.bodiesSeparate)
			{
				fill.pos = Flat2DFractureSetup.Jitter(fill.pos, model.dx);
			}
		}
		restraintWidthShown = 0f;
		if (model.restraint)
		{
			float projectileWidthM = Flat2DFractureSetup.ProjectileWidthM(model);
			if (projectileWidthM <= 0f || fill.ownerRole == null)
			{
				UnityEngine.Debug.Log("[Flat2DDrawScreen] удержание из плоскости: в сцене нет снаряда — ширина D не определена, не действует.");
			}
			else
			{
				flat2DSolver.RestraintWidthM = projectileWidthM;
				flat2DSolver.Restraint = true;
				flat2DSolver.RestraintMask = Flat2DFractureSetup.RestraintMask(fill.ownerRole);
				restraintWidthShown = projectileWidthM;
			}
		}
		Flat2DScene.Domain(new Flat2DScene.Body
		{
			pos = fill.pos,
			vel = fill.vel,
			mat = fill.mat,
			dx = model.dx
		}, 0.05f, out lo, out hi);
		flat2DSolver.Init(fill.pos, fill.vel, fill.mat, mats, model.dx, lo, hi);
		return flat2DSolver;
	}

	public string Fire()
	{
		Flat2DSolver flat2DSolver = BuildShotSolver(out var fill, out var mats, out var lo, out var hi);
		if (flat2DSolver == null)
		{
			Refresh(null);
			return null;
		}
		running = false;
		if (view != null)
		{
			view.Solver = null;
		}
		solver?.Dispose();
		solver = flat2DSolver;
		totalSteps = 0;
		erodedShown = -1;
		solver.Steps(20);
		solver.SimState(out _, out _, out _, out _);
		Stopwatch stopwatch = Stopwatch.StartNew();
		solver.Steps(20);
		solver.SimState(out _, out _, out _, out _);
		stopwatch.Stop();
		etaText = Flat2DDrawModel.EstimateShotTime(plannedSteps, 20, stopwatch.Elapsed.TotalSeconds, out etaSeconds);
		probeWasAlive = solver.ProbeAlive(40, out probeDetail);
		if (!MutProbeKeepTrace)
		{
			solver.Init(fill.pos, fill.vel, fill.mat, mats, model.dx, lo, hi);
		}
		view.Solver = solver;
		view.SetLivePaint(fill.paint);
		view.ResetBodyCache();
		view.ResetGlow();
		view.MaterialBlocks = mats;
		view.PlannedDurationSeconds = (double)plannedSteps * fixedDt;
		if ((double)confirmSeconds < etaSeconds)
		{
			running = false;
			SetConfirmOpen(true);
			return F("выстрел готов: {0} частиц; оценка {1} — дольше {2:F0} с, нужно подтверждение", fill.count, etaText, confirmSeconds);
		}
		running = true;
		return F("выстрел пошёл: {0} частиц; оценка {1}", fill.count, etaText);
	}

	public ulong NeverFiredHash()
	{
		Flat2DSolver flat2DSolver = BuildShotSolver(out var _, out var _, out var _, out var _);
		if (flat2DSolver == null)
		{
			return 0uL;
		}
		ulong result = flat2DSolver.StateHash();
		flat2DSolver.Dispose();
		return result;
	}

	private Flat2DFill.Poly StampAt(Vector2 w)
	{
		return new Flat2DFill.Poly
		{
			path = new Vector2[1] { w },
			halfWidth = brush * 0.5f,
			square = brushShape == 1
		};
	}

	private void BuildBrushCursor(Vector2 w)
	{
		List<Flat2DFill.Poly> extra = new List<Flat2DFill.Poly>();
		extra.Add(StampAt(w));
		Vector2[] sites = SitesUnder(extra);
		List<Vector3[]> lines = new List<Vector3[]>();
		List<bool> open = new List<bool>();
		lines.Add((brushShape == 1) ? SquareRing(w, brush * 0.5f) : CircleRing(w, brush * 0.5f));
		List<Color> colors = new List<Color>();
		Color color;
		bool flag;
		if (sites == null || sites.Length != 0)
		{
			if (brush / model.dx >= 3f)
			{
				if (tool == Flat2DDrawModel.Tool.Eraser)
				{
					color = MenuTheme.Refused;
				}
				else if (pickedRole != Flat2DFill.Role.Anchor)
				{
					color = MenuTheme.TextPrimary;
				}
				else
				{
					color = MenuTheme.Refused;
				}
			}
			else
			{
				color = MenuTheme.GizmoHot;
			}
			flag = false;
		}
		else
		{
			color = MenuTheme.Refused;
			flag = true;
		}
		colors.Add(color);
		open.Add(false);
		if (flag)
		{
			lines.Add(new Vector3[2]
			{
				new Vector3(w.x - brush * 0.5f, w.y - brush * 0.5f, 0f),
				new Vector3(w.x + brush * 0.5f, w.y + brush * 0.5f, 0f)
			});
			colors.Add(MenuTheme.Refused);
			open.Add(true);
			lines.Add(new Vector3[2]
			{
				new Vector3(w.x - brush * 0.5f, w.y + brush * 0.5f, 0f),
				new Vector3(w.x + brush * 0.5f, w.y - brush * 0.5f, 0f)
			});
			colors.Add(MenuTheme.Refused);
			open.Add(true);
		}
		if (view != null)
		{
			view.SetCursor(lines, colors, open, sites, color);
		}
	}

	private void BuildDecalCursor(Vector2 wRaw)
	{
		Vector2 vector = Flat2DDrawModel.SnapPoint(wRaw, snap, model.dx, null);
		List<Vector2[]> list = Flat2DDrawModel.DecalPolysCentred(heldDecal, vector, decalT, decalH);
		if (list.Count == 0)
		{
			if (view != null)
			{
				view.ClearCursor();
			}
			return;
		}
		List<Flat2DFill.Poly> extra = new List<Flat2DFill.Poly>();
		List<Vector3[]> lines = new List<Vector3[]>();
		List<Color> colors = new List<Color>();
		List<bool> open = new List<bool>();
		foreach (Vector2[] outline in list)
		{
			Vector2[] array = new Vector2[outline.Length];
			for (int i = 0; i < outline.Length; i++)
			{
				array[i] = Flat2DDrawModel.Rotate(outline[i], vector, decalAngle);
			}
			extra.Add(new Flat2DFill.Poly
			{
				pts = array
			});
			Vector3[] array2 = new Vector3[array.Length];
			for (int j = 0; j < array.Length; j++)
			{
				array2[j] = new Vector3(array[j].x, array[j].y, 0f);
			}
			lines.Add(array2);
			open.Add(false);
		}
		Vector2[] sites = SitesUnder(extra);
		bool flag = sites?.Length == 0;
		Color color = (flag ? MenuTheme.Refused : MenuTheme.Accent);
		for (int k = 0; k < lines.Count; k++)
		{
			colors.Add(color);
		}
		if (flag)
		{
			float num = brush * 0.5f;
			lines.Add(new Vector3[2]
			{
				new Vector3(vector.x - num, vector.y - num, 0f),
				new Vector3(vector.x + num, vector.y + num, 0f)
			});
			colors.Add(MenuTheme.Refused);
			open.Add(true);
			lines.Add(new Vector3[2]
			{
				new Vector3(vector.x - num, vector.y + num, 0f),
				new Vector3(vector.x + num, vector.y - num, 0f)
			});
			colors.Add(MenuTheme.Refused);
			open.Add(true);
		}
		if (view != null)
		{
			view.SetCursor(lines, colors, open, sites, color);
		}
	}

	private Vector2[] SitesUnder(List<Flat2DFill.Poly> extra)
	{
		if (zoomK < SitesMinZoom)
		{
			return null;
		}
		List<Flat2DFill.Poly> list = model.Polys(includeOpenProvisional: true);
		list.AddRange(extra);
		if (!Flat2DFill.LatticeOf(list, model.dx, out var lo, out var _, out var _))
		{
			return Array.Empty<Vector2>();
		}
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = 0f - float.MaxValue;
		float num4 = 0f - float.MaxValue;
		foreach (Flat2DFill.Poly poly in extra)
		{
			if (poly.path == null)
			{
				Vector2[] pts = poly.pts;
				if (pts != null)
				{
					for (int i = 0; i < pts.Length; i++)
					{
						num = Mathf.Min(num, pts[i].x);
						num2 = Mathf.Min(num2, pts[i].y);
						num3 = Mathf.Max(num3, pts[i].x);
						num4 = Mathf.Max(num4, pts[i].y);
					}
				}
			}
			else
			{
				Vector2[] path = poly.path;
				for (int j = 0; j < path.Length; j++)
				{
					num = Mathf.Min(num, path[j].x - poly.halfWidth);
					num2 = Mathf.Min(num2, path[j].y - poly.halfWidth);
					num3 = Mathf.Max(num3, path[j].x + poly.halfWidth);
					num4 = Mathf.Max(num4, path[j].y + poly.halfWidth);
				}
			}
		}
		num = Mathf.Max(num, view.Lo.x);
		num2 = Mathf.Max(num2, view.Lo.y);
		num3 = Mathf.Min(num3, view.Hi.x);
		num4 = Mathf.Min(num4, view.Hi.y);
		if (num3 < num || num4 < num2)
		{
			return Array.Empty<Vector2>();
		}
		int num5 = Mathf.FloorToInt((num - lo.x) / model.dx);
		int num6 = Mathf.CeilToInt((num3 - lo.x) / model.dx);
		int num7 = Mathf.FloorToInt((num2 - lo.y) / model.dx);
		int num8 = Mathf.CeilToInt((num4 - lo.y) / model.dx);
		List<Vector2> list2 = new List<Vector2>();
		for (int k = num7; k <= num8; k++)
		{
			for (int l = num5; l <= num6; l++)
			{
				Vector2 p = new Vector2(((float)l + 0.5f) * model.dx + lo.x, ((float)k + 0.5f) * model.dx + lo.y);
				foreach (Flat2DFill.Poly poly2 in extra)
				{
					if (Flat2DFill.Covers(poly2, p))
					{
						list2.Add(p);
						break;
					}
				}
			}
		}
		return list2.ToArray();
	}

	private static Vector3[] CircleRing(Vector2 c, float r, int seg = 32)
	{
		Vector3[] array = new Vector3[seg];
		for (int i = 0; i < seg; i++)
		{
			float f = (float)i * (Mathf.PI * 2f) / (float)seg;
			array[i] = new Vector3(c.x + Mathf.Cos(f) * r, c.y + Mathf.Sin(f) * r, 0f);
		}
		return array;
	}

	private static Vector3[] SquareRing(Vector2 c, float half)
	{
		return new Vector3[4]
		{
			new Vector3(c.x - half, c.y - half, 0f),
			new Vector3(c.x + half, c.y - half, 0f),
			new Vector3(c.x + half, c.y + half, 0f),
			new Vector3(c.x - half, c.y + half, 0f)
		};
	}

	public void Refresh(string message)
	{
		if (message == null)
		{
			if (view != null && view.GlowRefusal != glowRefusalShown)
			{
				glowRefusalShown = view.GlowRefusal;
				if (glowRefusalShown != null && statusLabel != null)
				{
					statusLabel.text = Tr(glowRefusalShown);
				}
			}
		}
		else if (statusLabel != null)
		{
			statusLabel.text = message;
		}
		int num = model.LiveCount();
		string text = model.CeilingWarning();
		if (hud != null)
		{
			string text2 = "";
			if (solver != null)
			{
				text2 = F(" | t = {0:F2} мкс, шаг {1}", (double)totalSteps * fixedDt * 1000000.0, totalSteps);
				if (view.FieldMode == 5 && view.LastBodyCount >= 0)
				{
					text2 += F(", тел {0}", view.LastBodyCount) + (solver.SeparateBodies ? Tr(" (тела: по решателю)") : "");
				}
			}
			string text3;
			if (heldDecal != null)
			{
				text3 = F("колесо: ПОВОРОТ {0} {1:F0}° · {2} точно", Tr(Flat2DRounds.IsRoundId(heldDecal) ? "снаряда" : "декали"), decalAngle, ControlBindings.ModifierName(ControlBindings.Modifier(ControlModifier.FlatPrecise)));
			}
			else if (SelectedIndex >= 0 && solver == null)
			{
				text3 = F("колесо: ПОВОРОТ фигуры {0} ({1:F0}°) · ВЫБР снять — зум", SelectedIndex + 1, model.shapes[SelectedIndex].angleDeg);
			}
			else
			{
				text3 = F("колесо: зум · {0} — кисть", BrushModName);
			}
			string text4 = Tr((view.RenderStyle == Flat2DRuntimeView.Style.Izmerenie) ? "ИЗМЕРЕНИЕ" : ((view.RenderStyle == Flat2DRuntimeView.Style.Dots) ? "ТОЧКИ" : "ПОВЕРХНОСТЬ"));
			string text5;
			if (view.Glow)
			{
				if (view.RenderStyle == Flat2DRuntimeView.Style.Izmerenie)
				{
					text5 = Tr(" (свеч. не для прибора)");
				}
				else if (view.GlowSuppressedByField)
				{
					text5 = Tr(" (свеч. не для поля ТЕМПЕРАТУРА)");
				}
				else if (view.GlowRefusal != null)
				{
					text5 = Tr(" (свеч. отказано)");
				}
				else
				{
					text5 = "";
				}
			}
			else
			{
				text5 = Tr(" (свеч. ВЫКЛ)");
			}
			float dx = ((solver != null) ? solver.Dx : model.dx);
			string text6 = F("стаб. {0} | тела {1}{2} | вид {3}{4} | {5} | сетка {6} Δx | Δx {7:F2} мм | {8:F0} пикс/Δx | частиц {9}", Tr(model.stabiliser ? "ВКЛ" : "ВЫКЛ"), Tr(model.bodiesSeparate ? "СЦЕПЛ." : "РАЗД."), text2, text4, text5, text3, Flat2DRuntimeView.GridStepFor(zoomK), dx * 1000f, (double)zoomK, num);
			if (solver != null && solver.Erosion && erodedShown >= 0)
			{
				text6 += F(" | стёрто {0}", erodedShown);
			}
			hud.text = text6;
		}
		if (sceneLabel != null)
		{
			string text7 = F("Δx {0:F2} мм · мир {1:F0}×{2:F0} мм\nчастиц {3} из {4} ({5:F1} %)", model.dx * 1000f, (view.DomainHi.x - view.DomainLo.x) * 1000f, (view.DomainHi.y - view.DomainLo.y) * 1000f, num, Flat2DFill.Ceiling, num * 100.0 / Flat2DFill.Ceiling);
			if (text != null)
			{
				text7 = text7 + "\n" + text;
			}
			sceneLabel.text = text7;
			sceneLabel.color = ((text != null) ? MenuTheme.Refused : MenuTheme.TextPrimary);
		}
		if (countBar != null)
		{
			float num2 = Mathf.Clamp(num / 1000000f, 0f, 1f);
			((RectTransform)countBar.transform).anchorMax = new Vector2(Mathf.Max(num2, 0.001f), 1f);
			countBar.color = ((text != null) ? MenuTheme.Refused : ((num2 > 0.75f) ? MenuTheme.GizmoHot : MenuTheme.SliderFill));
		}
		if (decalLabel != null)
		{
			decalLabel.text = F("толщина {0:F1} мм · длина {1:F0} мм · масштаб модели {2}", decalT * 1000f, decalH * 1000f, Flat2DModelScale.Label);
		}
		if (brushLabel != null)
		{
			float num3 = brush / model.dx;
			string text8 = F("{0} {1:F2} мм = {2:F1} Δx", Tr((brushShape == 1) ? "квадрат" : "круг"), brush * 1000f, num3);
			text8 += ((num3 < 1f) ? Tr("   тоньше 1 Δx — МОЖЕТ НЕ ДАТЬ НИ УЗЛА") : ((num3 < 3f) ? Tr("   тоньше 3 Δx — НЕ ИЗГИБАЕТСЯ") : ""));
			brushLabel.text = text8;
			brushLabel.color = ((num3 < 3f) ? MenuTheme.Refused : MenuTheme.TextPrimary);
		}
		if (etaLabel != null)
		{
			etaLabel.text = ((solver == null) ? Tr("оценка появится по нажатию «ОГОНЬ»") : (Tr("оценка: ") + etaText));
		}
		if (runBtn != null)
		{
			TextMeshProUGUI label = MenuTheme.LabelOf(runBtn);
			if (label != null)
			{
				label.text = Tr(running ? "ПАУЗА" : "ПРОДОЛЖИТЬ");
			}
			runBtn.interactable = solver != null;
		}
		if (fireBtn != null)
		{
			MenuTheme.SetSelectedVisual(fireBtn, solver != null);
		}
		for (int i = 0; i < brushShapeButtons.Count; i++)
		{
			MenuTheme.SetSelectedVisual(brushShapeButtons[i], i == brushShape);
		}
		for (int j = 0; j < styleButtons.Count; j++)
		{
			MenuTheme.SetSelectedVisual(styleButtons[j], j == (int)view.RenderStyle);
		}
		for (int k = 0; k < fieldButtons.Count; k++)
		{
			MenuTheme.SetSelectedVisual(fieldButtons[k], k == view.FieldMode);
		}
		for (int l = 0; l < toolButtons.Count; l++)
		{
			MenuTheme.SetSelectedVisual(toolButtons[l], l == (int)tool);
		}
		for (int m = 0; m < snapButtons.Count; m++)
		{
			MenuTheme.SetSelectedVisual(snapButtons[m], m == (int)snap);
		}
		for (int n = 0; n < roleButtons.Count; n++)
		{
			MenuTheme.SetSelectedVisual(roleButtons[n], n == (int)pickedRole);
		}
		if (mirrorBtn != null)
		{
			MenuTheme.SetSelectedVisual(mirrorBtn, mirror);
		}
		if (stabBtn != null)
		{
			MenuTheme.SetSelectedVisual(stabBtn, model.stabiliser);
			TextMeshProUGUI label2 = MenuTheme.LabelOf(stabBtn);
			if (label2 != null)
			{
				label2.text = Tr(model.stabiliser ? "СТАБИЛИЗАТОР: ВКЛ" : "СТАБИЛИЗАТОР: ВЫКЛ");
			}
		}
		if (bodiesBtn != null)
		{
			MenuTheme.SetSelectedVisual(bodiesBtn, model.bodiesSeparate);
			TextMeshProUGUI label3 = MenuTheme.LabelOf(bodiesBtn);
			if (label3 != null)
			{
				label3.text = Tr(model.bodiesSeparate ? "ТЕЛА РАЗДЕЛЬНЫ: ВКЛ" : "ТЕЛА РАЗДЕЛЬНЫ: ВЫКЛ");
			}
		}
		if (erosionBtn != null)
		{
			MenuTheme.SetSelectedVisual(erosionBtn, model.erosion);
			TextMeshProUGUI label4 = MenuTheme.LabelOf(erosionBtn);
			if (label4 != null)
			{
				label4.text = Tr(model.erosion ? "СТАЧИВАНИЕ: ВКЛ" : "СТАЧИВАНИЕ: ВЫКЛ");
			}
		}
		if (fractureBtn != null)
		{
			MenuTheme.SetSelectedVisual(fractureBtn, model.fracture);
			TextMeshProUGUI label5 = MenuTheme.LabelOf(fractureBtn);
			if (label5 != null)
			{
				label5.text = Tr(model.fracture ? "ТРЕЩИНЫ: ВКЛ" : "ТРЕЩИНЫ: ВЫКЛ");
			}
		}
		if (restraintBtn != null)
		{
			MenuTheme.SetSelectedVisual(restraintBtn, model.restraint);
			TextMeshProUGUI label6 = MenuTheme.LabelOf(restraintBtn);
			if (label6 != null)
			{
				label6.text = Tr(model.restraint ? "УДЕРЖАНИЕ: ВКЛ" : "УДЕРЖАНИЕ: ВЫКЛ");
			}
		}
		if (glowBtn != null)
		{
			MenuTheme.SetSelectedVisual(glowBtn, view.Glow && view.RenderStyle != Flat2DRuntimeView.Style.Izmerenie);
			TextMeshProUGUI label7 = MenuTheme.LabelOf(glowBtn);
			if (label7 != null)
			{
				label7.text = Tr(view.Glow ? "СВЕЧЕНИЕ: ВКЛ" : "СВЕЧЕНИЕ: ВЫКЛ");
			}
		}
		if (glowHint != null)
		{
			glowHint.color = ((view.Glow && view.RenderStyle != 0) ? MenuTheme.TextPrimary : MenuTheme.TextMuted);
		}
		UpdateScaleBar();
		UpdateOutlines();
		if (view != null && solver == null && previewVersion != model.Version)
		{
			previewVersion = model.Version;
			Flat2DFill.Result result = model.PreviewFill();
			if (model.openStroke == null && model.open == null)
			{
				if (result.ok && result.count <= BodiesCountCeiling)
				{
					double linkR = (double)model.dx * 2.6f;
					if (!model.bodiesSeparate || result.ownerRole == null)
					{
						bodiesOnCanvas = Flat2DBodies.Count(result.pos, linkR).Item1;
					}
					else
					{
						bodiesOnCanvas = Flat2DBodies.Label(result.pos, linkR, out var _, Flat2DBodies.Assign(result.pos, result.ownerRole, model.dx));
					}
				}
				else
				{
					bodiesOnCanvas = -1;
				}
				listVersion = -2;
			}
			previewFillOk = result.ok || model.shapes.Count == 0;
			if (!result.ok)
			{
				view.SetStatic(new Vector2[0], new uint[0]);
				view.SetStaticPaint(null, 0);
				view.PinnedPreview = null;
			}
			else
			{
				view.SetStatic(result.pos, result.mat);
				view.SetStaticPaint(result.paint, result.count);
				if (showPinned && result.pinned != null)
				{
					List<Vector2> list = new List<Vector2>();
					for (int num4 = 0; num4 < result.count; num4++)
					{
						if (result.pinned[num4])
						{
							list.Add(result.pos[num4]);
						}
					}
					view.PinnedPreview = list.ToArray();
				}
				else
				{
					view.PinnedPreview = null;
				}
			}
		}
		if (listVersion != model.Version && model.openStroke == null)
		{
			listVersion = model.Version;
			RebuildShapeList();
		}
	}

	private void UpdateScaleBar()
	{
		if (scaleBarRt == null || view == null)
		{
			return;
		}
		float width = view.ViewportPx.width;
		if (width < 2f)
		{
			return;
		}
		float dx = ((solver != null) ? solver.Dx : model.dx);
		int num = ((zoomK > 1) ? zoomK : 1);
		float num2 = width * 0.25f * (dx / (float)num) * 1000f;
		float num3 = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(Mathf.Max(num2, 1E-06f))));
		float num4 = num2 / num3;
		float num5 = ((num4 >= 5f) ? 5f : ((num4 >= 2f) ? 2f : 1f));
		float num6 = num5 * num3;
		float num7 = num6 * 0.001f / (dx / (float)num);
		float num8 = ((canvas != null) ? canvas.scaleFactor : 1f);
		scaleBarRt.sizeDelta = new Vector2(num7 / Mathf.Max(num8, 0.001f), 6f);
		if (scaleBarLabel == null)
		{
			return;
		}
		scaleBarLabel.rectTransform.anchoredPosition = new Vector2(num7 / Mathf.Max(num8, 0.001f) + 12f, 0f);
		if (num6 < 1f)
		{
			scaleBarLabel.text = F("{0:F2} мм", num6);
		}
		else
		{
			scaleBarLabel.text = F("{0:F0} мм = {1:F0} Δx", num6, num6 * 0.001f / dx);
		}
	}

	private void UpdateOutlines()
	{
		List<Vector3[]> lines = new List<Vector3[]>();
		List<Color> colors = new List<Color>();
		List<bool> open = new List<bool>();
		for (int i = 0; i < model.shapes.Count; i++)
		{
			Flat2DDrawModel.Shape shape = model.shapes[i];
			if (shape.on && (shape == selectedRef || (shape.role == Flat2DFill.Role.Anchor && !shape.erase) || (!previewFillOk && !shape.erase)))
			{
				Vector2[] array = ((shape.kind == 1) ? Flat2DDrawModel.EffectivePath(shape) : Flat2DDrawModel.EffectivePts(shape));
				if (array.Length > 1)
				{
					Vector3[] array2 = new Vector3[array.Length];
					for (int j = 0; j < array.Length; j++)
					{
						array2[j] = new Vector3(array[j].x, array[j].y, 0f);
					}
					lines.Add(array2);
					open.Add(shape.kind == 1);
					Color color;
					if (shape == selectedRef)
					{
						color = MenuTheme.GizmoHot;
					}
					else if (shape.role == Flat2DFill.Role.Anchor && !shape.erase)
					{
						color = MenuTheme.Refused;
					}
					else
					{
						color = MenuTheme.TextPrimary;
					}
					colors.Add(color);
				}
			}
		}
		if (model.open != null && model.open.pts.Count > 1)
		{
			Vector3[] array3 = new Vector3[model.open.pts.Count];
			for (int k = 0; k < model.open.pts.Count; k++)
			{
				array3[k] = new Vector3(model.open.pts[k].x, model.open.pts[k].y, 0f);
			}
			lines.Add(array3);
			colors.Add(MenuTheme.GizmoHot);
			open.Add(false);
		}
		if (model.openStroke != null && model.openStroke.role == Flat2DFill.Role.Anchor && !model.openStroke.erase)
		{
			List<Vector2> path = model.openStroke.path;
			if (path.Count > 1)
			{
				Vector3[] array4 = new Vector3[path.Count];
				for (int l = 0; l < path.Count; l++)
				{
					array4[l] = new Vector3(path[l].x, path[l].y, 0f);
				}
				lines.Add(array4);
				colors.Add(MenuTheme.Refused);
				open.Add(true);
			}
		}
		if (view != null)
		{
			view.SetOutlines(lines, colors, open);
		}
	}

	private void RebuildShapeList()
	{
		if (shapesHint == null)
		{
			return;
		}
		string text;
		if (model.shapes.Count == 0)
		{
			text = Tr("холст пуст — рисуйте кистью, контуром или поставьте декаль");
		}
		else
		{
			text = ((bodiesOnCanvas < 0) ? "" : (F("тел на холсте: {0}", bodiesOnCanvas) + (model.bodiesSeparate ? Tr(" (по решателю)") : "") + "\n"));
			text += Tr("рисунок цельный: снаряд из нескольких веществ, если его куски касаются, летит одним телом с одной скоростью");
		}
		shapesHint.text = text;
	}

	private void OnDestroy()
	{
		if (solver != null)
		{
			solver.Dispose();
		}
	}
}
