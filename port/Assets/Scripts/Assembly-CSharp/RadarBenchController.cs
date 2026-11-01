using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RadarBenchController : MonoBehaviour
{
	private sealed class ImportWork
	{
		public int Revision;

		public string Name;

		public Task<AeroMeshData> Task;

		public bool BuiltinYf23;

		public string Path;
	}

	private RadarCoating benchCoating;

	private string coatingKind;

	private double coatingF0Hz;

	private readonly float[] customLayer;

	private RectTransform coatingRoot;

	private RectTransform coatingF0Row;

	private RectTransform coatingCustomRows;

	private readonly Dictionary<string, Button> coatingButtons;

	private TextMeshProUGUI coatingTitle;

	private TextMeshProUGUI coatingCaption;

	private TextMeshProUGUI coatingReason;

	private TextMeshProUGUI coatingF0Label;

	private readonly TextMeshProUGUI[] customLabels;

	private readonly TMP_InputField[] customFields;

	private TMP_InputField coatingF0Field;

	private string coatingReasonText;

	private TextMeshProUGUI overlayCoating;

	private readonly List<ImportWork> imports;

	private int revision;

	private float nextAccessCheck;

	public AeroMeshData imported;

	private RadarModel referenceBase;

	private RadarLengthUnit unit;

	private double customLengthMeters;

	private RadarOrientation orientation;

	private bool isReference;

	public string currentName;

	public string referenceNameKey;

	private RadarBenchState state;

	private RadarModel currentModel;

	private Camera benchCamera;

	private RadarOrbit orbit;

	private RadarBenchVisuals visuals;

	private Transform modelObject;

	private Mesh displayMesh;

	private Material displayMaterial;

	private RectTransform canvas;

	private RectTransform filePanel;

	private bool radarDragging;

	private Vector3 lastPointer;

	private string currentPath;

	private Button toMapButton;

	private TextMeshProUGUI toMapStatus;

	private string toMapMessage;

	private readonly Dictionary<RadarLengthUnit, Button> unitButtons;

	private readonly Dictionary<RadarPolarization, Button> polButtons;

	private readonly Dictionary<string, Button> presetButtons;

	private AeroReadout modelNameReadout;

	private AeroReadout fileSizeReadout;

	private AeroReadout sizeReadout;

	private AeroReadout triangleReadout;

	private AeroReadout hintReadout;

	private AeroReadout bandReadout;

	private AeroReadout lambdaReadout;

	private AeroReadout azReadout;

	private AeroReadout elReadout;

	private AeroReadout esizeReadout;

	private AeroReadout statusReadout;

	private AeroReadout roleReadout;

	private AeroReadout examplesReadout;

	private Button presetButton;

	private Button radarViewButton;

	private RectTransform unitRow;

	private RectTransform presetList;

	private Slider frequencySlider;

	private Slider azimuthSlider;

	private Slider elevationSlider;

	private TMP_InputField frequencyField;

	private TMP_InputField azimuthField;

	private TMP_InputField elevationField;

	private TMP_InputField customLengthField;

	private string statusKey;

	private object[] statusArgs;

	private RectTransform referencesList;

	private Task<RadarPatternResult> patternTask;

	private CancellationTokenSource patternCancel;

	private int patternRevision;

	private int patternProgress;

	private int patternTotal;

	private int patternGeneration;

	private int runningPatternGeneration;

	private bool patternRequested;

	private bool patternAuto;

	private Button patternButton;

	private AeroReadout patternCaption;

	private RadarDisplayMode displayMode;

	private RadarPropagationVisuals propagation;

	private RadarScene solverScene;

	private RadarResult radarResult;

	private Task<RadarResult> solveTask;

	private Task<RadarScene> sceneTask;

	private CancellationTokenSource solveCancel;

	private RadarModel sceneModel;

	private RadarModel runningModel;

	private int solveRevision;

	private int runningRevision;

	private bool modesRunning;

	private bool solvePending;

	private bool heatmap;

	private bool importScaleConfirmed;

	private float solveDue;

	private double lastFrequency;

	private double lastAzimuth;

	private double lastElevation;

	private RadarPolarization lastPatternPolarization;

	private RadarQuality nextQuality;

	private RectTransform modeChoices;

	private RectTransform propagationControls;

	private RectTransform propagationOverlay;

	private Button modeButton;

	private Button simpleModeButton;

	private Button propagationModeButton;

	private Button wavefrontModeButton;

	private Button heatButton;

	private Button rayButton;

	private Button waveButton;

	private AeroReadout modeDisclaimer;

	private AeroReadout resultReadout;

	private AeroReadout counterReadout;

	private AeroReadout propagationStatus;

	private AeroReadout scaleReadout;

	private AeroReadout detailReadout;

	private AeroReadout waveReadout;

	private AeroReadout waveModeHint;

	private readonly Dictionary<int, Button> densityButtons;

	private int pathDensity;

	private Button atlasModeButton;

	private RadarAtlasView atlasView;

	private RadarScenarioController scenarioController;

	private bool propagationWasEnabled;

	public RadarCoating BenchCoating => benchCoating;

	public bool CoatingChoiceVisible => Application.isEditor;

	public RadarBenchState State => state;

	public RadarModel Model => currentModel;

	public Camera BenchCamera => benchCamera;

	public Transform RadarMarkerTransform
	{
		get
		{
			if (visuals == null)
			{
				return null;
			}
			return visuals.RadarMarkerTransform;
		}
	}

	public bool HasPanel => canvas != null;

	public string LastErrorKey { get; private set; }

	public static string ModelsFolder
	{
		get
		{
			string text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (string.IsNullOrEmpty(text))
			{
				text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents");
			}
			return Path.Combine(text, "ArmorSim", "Models");
		}
	}

	public RadarMapModelRegistry MapRegistry { get; set; }

	public string CurrentSourcePath => currentPath;

	public bool IsBuiltinYf23
	{
		get
		{
			if (referenceNameKey != "radar.model.yf23")
			{
				return false;
			}
			return imported != null;
		}
	}

	public string ModelDisplayName
	{
		get
		{
			if (referenceNameKey == null)
			{
				return currentName;
			}
			return Localization.T(referenceNameKey);
		}
	}

	public bool CanSendToMap
	{
		get
		{
			if (!isReference && imported != null && importScaleConfirmed && currentModel != null)
			{
				return !string.IsNullOrEmpty(currentPath);
			}
			return false;
		}
	}

	public bool PatternReady
	{
		get
		{
			if (patternTask == null && patternRequested && propagation != null)
			{
				return propagation.ShowPattern;
			}
			return false;
		}
	}

	public RadarDisplayMode DisplayMode => displayMode;

	public RadarResult PropagationResult => radarResult;

	public bool IsCalculating => solvePending || solveTask != null;

	private bool HasCalculatedDisplay => displayMode == RadarDisplayMode.Propagation || displayMode == RadarDisplayMode.Wavefronts;

	public RadarAtlasView AtlasView => atlasView;

	public bool ScenarioOpen
	{
		get
		{
			if (scenarioController == null)
			{
				return false;
			}
			return scenarioController.gameObject.activeSelf;
		}
	}

	public RadarScenarioController Scenario => scenarioController;

	public RadarBenchController()
	{
		benchCoating = RadarCoating.Metal;
		coatingKind = "metal";
		coatingF0Hz = 10e9;
		customLayer = (float[])RadarMapCoatings.DefaultCustom.Clone();
		coatingButtons = new Dictionary<string, Button>();
		customLabels = new TextMeshProUGUI[5];
		customFields = new TMP_InputField[5];
		coatingReasonText = "";
		imports = new List<ImportWork>();
		customLengthMeters = 1.0;
		orientation = RadarOrientation.Identity;
		isReference = true;
		unitButtons = new Dictionary<RadarLengthUnit, Button>();
		polButtons = new Dictionary<RadarPolarization, Button>();
		presetButtons = new Dictionary<string, Button>();
		statusKey = "radar.solver.pending";
		statusArgs = Array.Empty<object>();
		lastFrequency = double.NaN;
		heatmap = true;
		importScaleConfirmed = true;
		densityButtons = new Dictionary<int, Button>();
		pathDensity = 3000;
	}

	private static TextMeshProUGUI PlainLabel(Transform parent, string name, float size, float height)
	{
		TextMeshProUGUI textMeshProUGUI = Label(parent, name, "radar.input.empty", size, height);
		LocalizedLabel localized = textMeshProUGUI.GetComponent<LocalizedLabel>();
		if (localized != null)
		{
			localized.enabled = false;
		}
		return textMeshProUGUI;
	}

	private void BuildCoatingUI(Transform column)
	{
		coatingRoot = MenuTheme.MakeColumn(column, "CoatingControls", 5f);
		coatingTitle = PlainLabel(coatingRoot, "CoatingTitle", 18f, 26f);
		RectTransform row = Row(coatingRoot, "CoatingPresets", 38f);
		string[] array = new string[4] { "metal", "salisbury", "jaumann", "custom" };
		foreach (string kind in array)
		{
			Button button = Button(row, "Coating_" + kind, "radar.input.empty", 36f, delegate
			{
				SelectCoatingKind(kind);
			}, 14f);
			LocalizedLabel[] componentsInChildren = button.GetComponentsInChildren<LocalizedLabel>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = false;
			}
			coatingButtons[kind] = button;
		}
		coatingF0Row = Row(coatingRoot, "CoatingF0", 38f);
		coatingF0Label = PlainLabel(coatingF0Row, "CoatingF0Label", 16f, 34f);
		coatingF0Field = MakeField(coatingF0Row, "CoatingF0Input", 120f, "10", delegate(string text)
		{
			if (TryNumber(text, out var value) && RadarMapCoatings.F0InRange(value * 1000000000.0))
			{
				coatingF0Hz = value * 1000000000.0;
				coatingReasonText = "";
				ApplyCoating();
			}
			else
			{
				coatingReasonText = string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.coat.range"), "f0 [0.1, 40] GHz");
			}
			RefreshCoatingUI();
		});
		coatingCustomRows = MenuTheme.MakeColumn(coatingRoot, "CoatingCustom", 4f);
		string[] array2 = new string[5] { "ε′", "ε″", "μ′", "μ″", "d, mm" };
		for (int j = 0; j < 5; j++)
		{
			RectTransform row2 = MenuTheme.MakeRow(coatingCustomRows, "CustomLayer" + j, 8f, forceExpandWidth: true);
			Height(row2, 36f);
			customLabels[j] = PlainLabel(row2, "CustomLabel" + j, 16f, 32f);
			customLabels[j].text = array2[j];
			int index = j;
			customFields[j] = MakeField(row2, "CustomInput" + j, 120f, CustomText(j), delegate(string text)
			{
				ApplyCustomNumber(index, text);
			});
		}
		coatingReason = PlainLabel(coatingRoot, "CoatingReason", 14f, 36f);
		coatingReason.color = ResultPalette.Miss;
		coatingCaption = PlainLabel(coatingRoot, "CoatingCaption", 14f, 52f);
		RefreshCoatingUI();
	}

	private string CustomText(int i)
	{
		double num;
		if (i == 4)
		{
			num = customLayer[4] * 1000.0;
		}
		else
		{
			num = customLayer[i];
		}
		return num.ToString("G4", CultureInfo.InvariantCulture);
	}

	private void ApplyCustomNumber(int index, string text)
	{
		if (!TryNumber(text, out double value))
		{
			coatingReasonText = string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.coat.range"), text);
			RefreshCoatingUI();
			return;
		}
		float[] array = (float[])customLayer.Clone();
		if (index == 4)
		{
			value /= 1000.0;
		}
		array[index] = (float)value;
		string reason;
		if (RadarMapCoatings.CustomInRange(array, out reason))
		{
			Array.Copy(array, customLayer, 5);
			coatingReasonText = "";
			ApplyCoating();
		}
		else
		{
			coatingReasonText = string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.coat.range"), reason);
		}
		RefreshCoatingUI();
	}

	public void SelectCoatingKind(string kind)
	{
		if (Array.IndexOf(RadarScenarioData.PhysicalCoatingIds, kind) < 0)
		{
			throw new ArgumentOutOfRangeException("kind");
		}
		coatingKind = kind;
		coatingReasonText = "";
		ApplyCoating();
		RefreshCoatingUI();
	}

	public void SetCoatingF0(double hz)
	{
		if (!RadarMapCoatings.F0InRange(hz))
		{
			throw new ArgumentOutOfRangeException("hz");
		}
		coatingF0Hz = hz;
		ApplyCoating();
		RefreshCoatingUI();
	}

	private void ApplyCoating()
	{
		RadarCoating radarCoating;
		if (coatingKind == "salisbury")
		{
			radarCoating = RadarCoating.Salisbury(coatingF0Hz);
		}
		else if (coatingKind == "jaumann")
		{
			radarCoating = RadarCoating.Jaumann(coatingF0Hz);
		}
		else if (coatingKind == "custom")
		{
			radarCoating = RadarCoating.Custom(customLayer[0], customLayer[1], customLayer[2], customLayer[3], customLayer[4]);
		}
		else
		{
			radarCoating = RadarCoating.Metal;
		}
		if (!Application.isEditor)
		{
			radarCoating = RadarCoating.Metal;
		}
		benchCoating = radarCoating;
		patternGeneration++;
		patternCancel?.Cancel();
		patternRequested = false;
		if (propagation != null)
		{
			propagation.ClearPattern();
		}
		InvalidatePropagation(geometry: false, force: true);
	}

	private void RefreshCoatingUI()
	{
		if (coatingRoot == null)
		{
			return;
		}
		bool isEditor = Application.isEditor;
		coatingRoot.gameObject.SetActive(isEditor);
		if (!isEditor)
		{
			return;
		}
		coatingTitle.text = RadarScenarioText.Get("map.coat.title");
		string text = (coatingF0Hz / 1000000000.0).ToString("0.###", CultureInfo.InvariantCulture);
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("metal", RadarScenarioText.Get("map.coat.metal"));
		dictionary.Add("salisbury", string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.coat.salisbury"), text));
		dictionary.Add("jaumann", string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.coat.jaumann"), text));
		dictionary.Add("custom", RadarScenarioText.Get("map.coat.custom"));
		foreach (KeyValuePair<string, Button> item in coatingButtons)
		{
			TextMeshProUGUI label = MenuTheme.LabelOf(item.Value);
			label.text = dictionary[item.Key];
			MenuTheme.SetSelectedVisual(item.Value, item.Key == coatingKind);
		}
		coatingF0Row.gameObject.SetActive(coatingKind == "salisbury" || coatingKind == "jaumann");
		coatingF0Label.text = RadarScenarioText.Get("map.coat.f0");
		SyncField(coatingF0Field, text);
		coatingCustomRows.gameObject.SetActive(coatingKind == "custom");
		for (int i = 0; i < 5; i++)
		{
			SyncField(customFields[i], CustomText(i));
		}
		coatingReason.text = coatingReasonText;
		string text2 = RadarScenarioText.Get("map.coat.title") + ": " + RadarMapCoatings.Name(benchCoating);
		if (!benchCoating.IsMetal)
		{
			text2 += " · " + RadarScenarioText.Get("map.coat.note");
			if (benchCoating.Id == RadarCoating.Kind.Custom)
			{
				text2 += " · " + RadarScenarioText.Get("map.coat.assumed");
			}
			if (state != null)
			{
				text2 += " · " + string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.coat.band"), Frequency(state.FrequencyHz), RadarMapCoatings.NormalDb(benchCoating, state.FrequencyHz).ToString("F1", CultureInfo.InvariantCulture));
			}
		}
		coatingCaption.text = text2;
		if (propagationOverlay != null && overlayCoating == null)
		{
			overlayCoating = PlainLabel(propagationOverlay, "OverlayCoating", 15f, 40f);
			overlayCoating.color = new Color(0.16f, 0.18f, 0.2f, 1f);
			overlayCoating.raycastTarget = false;
		}
		if (overlayCoating != null)
		{
			overlayCoating.text = text2;
		}
	}

	private void Start()
	{
		if (CheckAccess())
		{
			try
			{
				state = new RadarBenchState();
				state.Changed += OnStateChanged;
				BuildView();
				BuildUI();
				modesRunning = true;
				MusicPlayer.EnsureIn(null, MusicPolicy.GameProgram());
				Directory.CreateDirectory(ModelsFolder);
			}
			catch (IOException ex)
			{
				Report(new AeroException("aero.error.io", ex.Message));
			}
			catch (Exception ex2)
			{
				Debug.LogException(ex2);
				Report(ex2);
			}
			SelectReference("sphere");
		}
	}

	private bool CheckAccess()
	{
		if (RadarRoute.Decide(SimFamily.Radar, RadarFreeAccess.ReadFacts()) != RadarEntryAction.OpenRadarBench)
		{
			enabled = false;
			Debug.Log("[RADARSIM] Bench access unavailable; returning to menu.");
			AppFlow.OpenMenu();
			return false;
		}
		return true;
	}

	private void BuildView()
	{
		benchCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
		if (benchCamera == null)
		{
			benchCamera = new GameObject("Radar Camera", typeof(Camera)).GetComponent<Camera>();
		}
		AudioListener audioListener = null;
		AudioListener[] array = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].isActiveAndEnabled)
			{
				audioListener = array[i];
				break;
			}
		}
		if (audioListener == null)
		{
			audioListener = benchCamera.GetComponent<AudioListener>();
			if (audioListener == null)
			{
				audioListener = benchCamera.gameObject.AddComponent<AudioListener>();
			}
			audioListener.enabled = true;
		}
		benchCamera.clearFlags = CameraClearFlags.SolidColor;
		benchCamera.backgroundColor = ResultPalette.Background;
		benchCamera.rect = new Rect(0f, 0f, 0.68f, 1f);
		orbit = benchCamera.GetComponent<RadarOrbit>();
		if (orbit == null)
		{
			orbit = benchCamera.gameObject.AddComponent<RadarOrbit>();
		}
		modelObject = new GameObject("RadarModel", typeof(MeshFilter), typeof(MeshRenderer)).transform;
		modelObject.SetParent(base.transform, worldPositionStays: false);
		Shader shader = Resources.Load<Shader>("RadarSurface");
		if (shader == null)
		{
			throw new InvalidOperationException(Localization.T("radar.error.shader"));
		}
		displayMaterial = new Material(shader);
		displayMaterial.SetFloat("_ShowMap", 0f);
		modelObject.GetComponent<MeshRenderer>().sharedMaterial = displayMaterial;
		GameObject gameObject = new GameObject("RadarDirectionVisuals");
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		visuals = gameObject.AddComponent<RadarBenchVisuals>();
	}

	public void ShowSphere()
	{
		SelectReference("sphere");
	}

	public void SelectReference(string id)
	{
		RadarModel radarModel;
		if (id == "sphere")
		{
			radarModel = RadarPrimitives.Sphere();
		}
		else if (id == "plate")
		{
			radarModel = RadarPrimitives.Plate();
		}
		else if (id == "cylinder")
		{
			radarModel = RadarPrimitives.Cylinder();
		}
		else if (id == "dihedral")
		{
			radarModel = RadarPrimitives.Dihedral();
		}
		else if (id == "trihedral_sq")
		{
			radarModel = RadarPrimitives.SquareTrihedral();
		}
		else if (id == "trihedral_tri")
		{
			radarModel = RadarPrimitives.TriangularTrihedral();
		}
		else
		{
			throw new ArgumentOutOfRangeException("id", id, "Unknown Radar reference");
		}
		revision++;
		referenceBase = radarModel;
		imported = null;
		isReference = true;
		orientation = RadarOrientation.Identity;
		referenceNameKey = "radar.ref." + id;
		currentName = null;
		currentPath = null;
		ApplyGeometry();
	}

	public void LoadPath(string path)
	{
		int num = revision + 1;
		LastErrorKey = null;
		revision = num;
		string name = Path.GetFileName(path);
		Task<AeroMeshData> task = Task.Run(delegate
		{
			return RadarLargeStlAdapter.Load(path);
		});
		task.ContinueWith(delegate (Task<AeroMeshData> failed)
		{
			_ = failed.Exception;
		}, TaskContinuationOptions.OnlyOnFaulted);
		ImportWork importWork = new ImportWork();
		importWork.Revision = num;
		importWork.Name = name;
		importWork.Task = task;
		importWork.Path = path;
		imports.Add(importWork);
		SetStatus("radar.model.loading", Array.Empty<object>());
	}

	public void SelectYf23()
	{
		try
		{
			byte[] bytes = RadarBuiltinModels.ReadYf23Resource();
			int num = revision + 1;
			revision = num;
			LastErrorKey = null;
			Task<AeroMeshData> task = Task.Run(delegate
			{
				return RadarBuiltinModels.ParseYf23(bytes);
			});
			task.ContinueWith(delegate (Task<AeroMeshData> t)
			{
				_ = t.Exception;
			}, TaskContinuationOptions.OnlyOnFaulted);
			ImportWork importWork = new ImportWork();
			importWork.Revision = num;
			importWork.Name = "Northrop YF-23";
			importWork.Task = task;
			importWork.BuiltinYf23 = true;
			imports.Add(importWork);
			SetStatus("radar.model.loading", Array.Empty<object>());
		}
		catch (Exception error)
		{
			Report(error);
		}
	}

	private void ReceiveImports()
	{
		for (int num = imports.Count - 1; num >= 0; num--)
		{
			ImportWork importWork = imports[num];
			if (importWork.Task.IsCompleted)
			{
				imports.RemoveAt(num);
				try
				{
					AeroMeshData aeroMeshData = importWork.Task.GetAwaiter().GetResult();
					if (importWork.Revision == revision)
					{
						imported = aeroMeshData;
						referenceBase = null;
						isReference = false;
						importScaleConfirmed = importWork.BuiltinYf23;
						unit = (importWork.BuiltinYf23 ? RadarLengthUnit.CustomLength : RadarLengthUnit.Meter);
						if (importWork.BuiltinYf23)
						{
							customLengthMeters = RadarBuiltinModels.Yf23LengthMeters;
						}
						orientation = RadarOrientation.Identity;
						currentName = importWork.Name;
						currentPath = (importWork.BuiltinYf23 ? null : importWork.Path);
						referenceNameKey = (importWork.BuiltinYf23 ? "radar.model.yf23" : null);
						ApplyGeometry();
					}
				}
				catch (Exception error)
				{
					if (importWork.Revision == revision)
					{
						Report(error);
					}
				}
			}
		}
	}

	public void SetUnit(RadarLengthUnit value)
	{
		if (!isReference)
		{
			unit = value;
			importScaleConfirmed = value != RadarLengthUnit.CustomLength;
			ApplyGeometry();
		}
	}

	public void SetCustomLength(double lengthMeters)
	{
		if (lengthMeters > 0.0 && double.IsFinite(lengthMeters))
		{
			customLengthMeters = lengthMeters;
			importScaleConfirmed = true;
			SetStatus("radar.solver.pending", Array.Empty<object>());
			if (!isReference && unit == RadarLengthUnit.CustomLength)
			{
				ApplyGeometry();
			}
		}
		else
		{
			SetStatus("radar.error.length", Array.Empty<object>());
		}
	}

	public void RotateX()
	{
		orientation = orientation.RotateX();
		ApplyGeometry();
	}

	public void RotateY()
	{
		orientation = orientation.RotateY();
		ApplyGeometry();
	}

	public void RotateZ()
	{
		orientation = orientation.RotateZ();
		ApplyGeometry();
	}

	public void ResetOrientation()
	{
		orientation = RadarOrientation.Identity;
		ApplyGeometry();
	}

	private void ApplyGeometry()
	{
		RadarModel radarModel;
		if (imported == null)
		{
			if (referenceBase == null)
			{
				return;
			}
			radarModel = RadarModel.Build(referenceBase.Vertices, referenceBase.Triangles, orientation);
		}
		else
		{
			radarModel = RadarModelImport.FromAero(imported, unit, customLengthMeters, orientation);
		}
		currentModel = radarModel;
		LastErrorKey = null;
		SetStatus("radar.solver.pending", Array.Empty<object>());
		if (displayMesh != null)
		{
			UnityEngine.Object.Destroy(displayMesh);
		}
		displayMesh = ToDisplayMesh(currentModel);
		modelObject.GetComponent<MeshFilter>().sharedMesh = displayMesh;
		float num = DisplayRadius(currentModel);
		benchCamera.nearClipPlane = num * 0.01f;
		benchCamera.farClipPlane = num * 100f;
		orbit.Configure(V(currentModel.Center), num);
		RefreshDirection();
		RefreshUI();
		InvalidatePropagation(geometry: true);
	}

	private static Mesh ToDisplayMesh(RadarModel model)
	{
		Vector3[] array = new Vector3[model.Vertices.Length];
		Color[] array2 = new Color[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = V(model.Vertices[i]);
			array2[i] = new Color(0.76f, 0.78f, 0.8f, 1f);
		}
		Mesh mesh = new Mesh();
		mesh.name = "Radar model, metres";
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.vertices = array;
		mesh.triangles = (int[])model.Triangles.Clone();
		mesh.colors = array2;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	private void OnStateChanged()
	{
		if (statusKey != null && statusKey.StartsWith("radar.error.", StringComparison.Ordinal) && LastErrorKey == null)
		{
			SetStatus("radar.solver.pending", Array.Empty<object>());
		}
		RefreshDirection();
		RefreshUI();
		InvalidatePropagation(geometry: false);
	}

	private void RefreshDirection()
	{
		if (currentModel == null || state == null || visuals == null)
		{
			return;
		}
		Vector3 center = V(currentModel.Center);
		float num = DisplayRadius(currentModel);
		RadarDirectionBasis basis = state.Basis;
		Vector3 u = V(basis.ToRadar);
		Vector3 h = V(basis.Horizontal);
		Vector3 v = V(basis.Vertical);
		visuals.Configure(center, num, u, h, v);
		orbit.SetRadarView(state.ViewFromRadar, center, num, u, v);
		if (state.ViewFromRadar)
		{
			orbit.UpdateRadarPose(center, num, u, v);
		}
	}

	public void SetDirection(double azimuthDeg, double elevationDeg)
	{
		state.SetDirection(azimuthDeg, elevationDeg);
	}

	public void BackToMenu()
	{
		AppFlow.OpenMenu();
	}

	private static float DisplayRadius(RadarModel model)
	{
		float num = (float)model.BoundingSphereRadius;
		if (num > 0f && !float.IsInfinity(num))
		{
			return num;
		}
		throw new InvalidOperationException(Localization.T("radar.error.length"));
	}

	public void ApplyDirectionKeys(int horizontal, int vertical, bool shift)
	{
		if (state == null || NumberField.TypingNow)
		{
			return;
		}
		double num = shift ? 10.0 : 1.0;
		if (horizontal != 0 || vertical != 0)
		{
			state.NudgeDirection((double)horizontal * num, (double)vertical * num);
		}
	}

	private void Update()
	{
		if (Time.unscaledTime >= nextAccessCheck)
		{
			nextAccessCheck = Time.unscaledTime + 0.25f;
			if (CheckAccess() && !ScenarioOpen)
			{
				ReceiveImports();
				HandleDirectionInput();
				UpdatePropagation();
			}
		}
	}

	private void HandleDirectionInput()
	{
		if (state == null)
		{
			return;
		}
		Vector2 pointerPos = TouchInputHelper.PrimaryPosition;
		bool num = pointerPos.x >= 0f && pointerPos.x < (float)Screen.width * 0.68f && pointerPos.y >= 0f && pointerPos.y <= (float)Screen.height;
		bool flag = TouchInputHelper.PointerOverUI;
		if (TouchInputHelper.PrimaryDown)
		{
			radarDragging = num && !flag && displayMode != RadarDisplayMode.Atlas;
			lastPointer = pointerPos;
		}
		if (TouchInputHelper.PrimaryUp)
		{
			radarDragging = false;
		}
		if (radarDragging && TouchInputHelper.PrimaryHeld)
		{
			float num2 = pointerPos.x - lastPointer.x;
			float num3 = pointerPos.y - lastPointer.y;
			lastPointer = pointerPos;
			if (num2 != 0f || num3 != 0f)
			{
				state.SetDirection((double)num2 * 0.3 + state.AzimuthDeg, (double)num3 * 0.3 + state.ElevationDeg);
			}
		}
		int num4 = (Input.GetKeyDown(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKeyDown(KeyCode.LeftArrow) ? 1 : 0);
		int num5 = (Input.GetKeyDown(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKeyDown(KeyCode.DownArrow) ? 1 : 0);
		bool flag2 = Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift);
		if (state == null || NumberField.TypingNow)
		{
			return;
		}
		double num6 = flag2 ? 10.0 : 1.0;
		if (num4 != 0 || num5 != 0)
		{
			state.NudgeDirection((double)num4 * num6, (double)num5 * num6);
		}
	}

	private void Report(Exception error)
	{
		AeroException ex = error as AeroException;
		string key = (ex != null) ? ex.Key : "radar.error.unexpected";
		LastErrorKey = key;
		Debug.LogWarning("[RADARSIM] " + error.Message);
		object[] array = (ex != null) ? ex.Args : new object[1] { error.Message };
		SetStatus(key, array);
	}

	private void OnDestroy()
	{
		modesRunning = false;
		solveCancel?.Cancel();
		patternGeneration++;
		patternCancel?.Cancel();
		patternRequested = false;
		if (state != null)
		{
			state.Changed -= OnStateChanged;
		}
		if (displayMesh != null)
		{
			UnityEngine.Object.Destroy(displayMesh);
		}
		if (displayMaterial != null)
		{
			UnityEngine.Object.Destroy(displayMaterial);
		}
		foreach (ImportWork import in imports)
		{
			if (import.Task.IsFaulted)
			{
				_ = import.Task.Exception;
			}
		}
	}

	private void BuildToMap(Transform column)
	{
		toMapButton = Button(column, "ToMap", "radar.input.empty", 40f, delegate
		{
			SendToMap();
		}, 17f);
		LocalizedLabel[] componentsInChildren = toMapButton.GetComponentsInChildren<LocalizedLabel>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		toMapStatus = Label(column, "ToMapStatus", "radar.input.empty", 15f, 40f);
		LocalizedLabel localized = toMapStatus.GetComponent<LocalizedLabel>();
		if (localized != null)
		{
			localized.enabled = false;
		}
		toMapStatus.text = "";
	}

	public RadarMapModelRecord SendToMap()
	{
		if (isReference || imported == null || !importScaleConfirmed || currentModel == null || string.IsNullOrEmpty(currentPath))
		{
			toMapMessage = RadarScenarioText.Get("map.phys.toMap.need");
			RefreshToMap();
			return null;
		}
		try
		{
			RadarMapModelRegistry radarMapModelRegistry = MapRegistry ?? RadarMapModelRegistry.Default;
			string text = Path.GetFileNameWithoutExtension(currentName ?? currentPath);
			RadarMapModelRecord radarMapModelRecord = radarMapModelRegistry.Register(currentPath, unit, customLengthMeters, orientation, text, currentModel.LongestSide);
			toMapMessage = string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.phys.toMap.done"), radarMapModelRecord.DisplayName, radarMapModelRecord.LongestSideM.ToString("F2", CultureInfo.InvariantCulture));
			Debug.Log("[RADAR MAP] bench record " + radarMapModelRecord.Key + " unit=" + radarMapModelRecord.Unit + " length=" + radarMapModelRecord.LongestSideM.ToString());
			RefreshToMap();
			return radarMapModelRecord;
		}
		catch (Exception error)
		{
			toMapMessage = string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.phys.toMap.failed"), error.Message);
			Debug.LogWarning("[RADAR MAP] " + toMapMessage);
			RefreshToMap();
		}
		return null;
	}

	private void RefreshToMap()
	{
		if (toMapButton == null)
		{
			return;
		}
		toMapButton.interactable = CanSendToMap;
		TextMeshProUGUI label = MenuTheme.LabelOf(toMapButton);
		if (label != null)
		{
			label.text = RadarScenarioText.Get("map.phys.toMap");
		}
		if (toMapStatus != null)
		{
			toMapStatus.text = toMapMessage ?? "";
		}
	}

	private static void Height(RectTransform rect, float value)
	{
		LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
		if (layoutElement == null)
		{
			layoutElement = rect.gameObject.AddComponent<LayoutElement>();
		}
		layoutElement.minHeight = value;
		layoutElement.preferredHeight = value;
	}

	private static TextMeshProUGUI Label(Transform parent, string name, string key, float size, float height)
	{
		TextMeshProUGUI textMeshProUGUI = MenuTheme.MakeLabel(parent, name, key, size, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		textMeshProUGUI.overflowMode = TextOverflowModes.Ellipsis;
		Height(textMeshProUGUI.rectTransform, height);
		return textMeshProUGUI;
	}

	private static AeroReadout Live(Transform parent, string name, string key, float size, float height)
	{
		return AeroReadout.Attach(Label(parent, name, key, size, height), key);
	}

	private static RectTransform Row(Transform parent, string name, float height)
	{
		RectTransform rect = MenuTheme.MakeRow(parent, name, 8f, TextAnchor.MiddleCenter, forceExpandWidth: true);
		Height(rect, height);
		return rect;
	}

	private static Button Button(Transform parent, string name, string key, float height, Action click, float font = 16f)
	{
		return MenuTheme.MakeButton(parent, name, key, height, delegate
		{
			click();
		}, font);
	}

	private static Slider MakeSlider(Transform parent, string name, float min, float max, float initial, Action<float> change)
	{
		RectTransform rect = MenuTheme.MakeRect(parent, name);
		LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
		layoutElement.minHeight = 30f;
		layoutElement.preferredHeight = 30f;
		layoutElement.flexibleWidth = 1f;
		Slider slider = rect.gameObject.AddComponent<Slider>();
		slider.minValue = min;
		slider.maxValue = max;
		RectTransform rectTransform = MenuTheme.MakeRect(rect, "Track");
		MenuTheme.Stretch(rectTransform, new Vector2(8f, 11f), new Vector2(-8f, -11f));
		Image image = MenuTheme.AddImage(rectTransform, MenuTheme.SliderTrack);
		image.raycastTarget = true;
		RectTransform rectTransform2 = MenuTheme.MakeRect(rect, "HandleArea");
		MenuTheme.Stretch(rectTransform2, new Vector2(8f, 0f), new Vector2(-8f, 0f));
		RectTransform handle = MenuTheme.MakeRect(rectTransform2, "Handle");
		handle.sizeDelta = new Vector2(16f, 0f);
		slider.targetGraphic = MenuTheme.AddImage(handle, MenuTheme.TextPrimary);
		slider.handleRect = handle;
		slider.value = initial;
		slider.onValueChanged.AddListener(delegate(float value)
		{
			change(value);
		});
		return slider;
	}

	private static TMP_InputField MakeField(Transform parent, string name, float width, string initial, Action<string> submit)
	{
		RectTransform rect = MenuTheme.MakeRect(parent, name);
		LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
		layoutElement.minWidth = width;
		layoutElement.preferredWidth = width;
		layoutElement.minHeight = 34f;
		layoutElement.preferredHeight = 34f;
		Image image = MenuTheme.AddImage(rect, MenuTheme.Surface);
		image.raycastTarget = true;
		RectTransform textArea = MenuTheme.MakeRect(rect, "Text Area");
		MenuTheme.Stretch(textArea, new Vector2(8f, 2f), new Vector2(-8f, -2f));
		TMP_InputField field = textArea.gameObject.AddComponent<TMP_InputField>();
		TextMeshProUGUI textMeshProUGUI = Label(textArea, "Text", "radar.input.empty", 17f, 30f);
		LocalizedLabel localized = textMeshProUGUI.GetComponent<LocalizedLabel>();
		if (localized != null)
		{
			localized.enabled = false;
		}
		textMeshProUGUI.text = initial;
		textMeshProUGUI.alignment = TextAlignmentOptions.MidlineLeft;
		textMeshProUGUI.textWrappingMode = TextWrappingModes.NoWrap;
		MenuTheme.Stretch(textMeshProUGUI.rectTransform);
		field.targetGraphic = image;
		field.textViewport = textArea;
		field.textComponent = textMeshProUGUI;
		field.lineType = TMP_InputField.LineType.SingleLine;
		field.richText = false;
		field.onFocusSelectAll = true;
		field.restoreOriginalTextOnEscape = true;
		field.SetTextWithoutNotify(initial);
		field.onSubmit.AddListener(delegate(string value)
		{
			if (!field.isFocused)
			{
				submit(value);
			}
		});
		return field;
	}

	private void BuildUI()
	{
		if (EventSystem.current == null)
		{
			new GameObject("Radar EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(base.transform, worldPositionStays: false);
		}
		canvas = MenuTheme.MakeRect(base.transform, "RadarCanvas");
		canvas.gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.gameObject.AddComponent<GraphicRaycaster>();
		CanvasScaler canvasScaler = canvas.gameObject.AddComponent<CanvasScaler>();
		canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
		canvasScaler.matchWidthOrHeight = 0.5f;
		RectTransform rect = MenuTheme.MakeRect(canvas, "Controls");
		rect.anchorMin = new Vector2(0.68f, 0f);
		rect.anchorMax = Vector2.one;
		rect.offsetMin = new Vector2(8f, 16f);
		rect.offsetMax = new Vector2(-16f, -16f);
		MenuTheme.AddImage(rect, MenuTheme.SurfaceAlt).raycastTarget = true;
		RectTransform content;
		MenuTheme.Stretch((RectTransform)MenuTheme.MakeScrollList(rect, "ControlsScroll", out content).transform, new Vector2(20f, 20f), new Vector2(-20f, -20f));
		content.GetComponent<VerticalLayoutGroup>().spacing = 8f;
		Label(content, "Title", "radar.title", 32f, 42f);
		BuildModeUI(content);
		RectTransform rect2 = Row(content, "ModelActions", 42f);
		Button(rect2, "Import", "radar.import", 40f, PickFile, 18f);
		Button(rect2, "References", "radar.references", 40f, delegate
		{
			referencesList.gameObject.SetActive(!referencesList.gameObject.activeSelf);
		}, 18f);
		BuildReferences(content);
		Button(rect2, "BuiltinYf23", "radar.model.yf23_load", 40f, SelectYf23, 17f);
		modelNameReadout = AeroReadout.Attach(Label(content, "ModelName", "radar.model.name", 18f, 28f), "radar.model.name");
		fileSizeReadout = AeroReadout.Attach(Label(content, "FileSize", "radar.model.file_size", 17f, 32f), "radar.model.file_size");
		unitRow = Row(content, "Units", 38f);
		AddUnit(unitRow, RadarLengthUnit.Meter, "radar.unit.m");
		AddUnit(unitRow, RadarLengthUnit.Centimeter, "radar.unit.cm");
		AddUnit(unitRow, RadarLengthUnit.Millimeter, "radar.unit.mm");
		AddUnit(unitRow, RadarLengthUnit.Inch, "radar.unit.in");
		AddUnit(unitRow, RadarLengthUnit.CustomLength, "radar.unit.length");
		RectTransform rect3 = Row(content, "CustomLength", 38f);
		Label(rect3, "CustomLengthLabel", "radar.unit.length_m", 17f, 34f);
		customLengthField = MakeField(rect3, "CustomLengthMeters", 150f, "1", delegate(string text)
		{
			if (!TryNumber(text, out var value))
			{
				SetStatus("radar.error.length", Array.Empty<object>());
			}
			else if (value > 0.0 && double.IsFinite(value))
			{
				customLengthMeters = value;
				importScaleConfirmed = true;
				SetStatus("radar.solver.pending", Array.Empty<object>());
				if (!isReference && unit == RadarLengthUnit.CustomLength)
				{
					ApplyGeometry();
				}
			}
			else
			{
				SetStatus("radar.error.length", Array.Empty<object>());
			}
			RefreshUI();
		});
		customLengthField.onEndEdit.AddListener(delegate(string _)
		{
			customLengthField.SetTextWithoutNotify(customLengthMeters.ToString("G4", CultureInfo.InvariantCulture));
			RefreshUI();
		});
		sizeReadout = AeroReadout.Attach(Label(content, "SizeMetres", "radar.model.size_m", 17f, 32f), "radar.model.size_m");
		triangleReadout = AeroReadout.Attach(Label(content, "TriangleCount", "radar.model.tris", 17f, 32f), "radar.model.tris");
		hintReadout = AeroReadout.Attach(Label(content, "UnitHint", "radar.unit.hint_mm", 16f, 42f), "radar.unit.hint_mm");
		RectTransform rect4 = Row(content, "Axes", 38f);
		Button(rect4, "RotateX", "radar.axes.x90", 36f, RotateX, 16f);
		Button(rect4, "RotateY", "radar.axes.y90", 36f, RotateY, 16f);
		Button(rect4, "RotateZ", "radar.axes.z90", 36f, RotateZ, 16f);
		Button(rect4, "AxesReset", "radar.axes.reset", 36f, ResetOrientation, 16f);
		Label(content, "AxesHint", "radar.axes.hint", 15f, 46f);
		BuildToMap(content);
		Label(content, "RadarSection", "radar.section", 22f, 30f);
		presetButton = Button(content, "CurrentPreset", "radar.preset.current", 46f, delegate
		{
			presetList.gameObject.SetActive(!presetList.gameObject.activeSelf);
		}, 17f);
		presetList = MenuTheme.MakeRect(content, "PresetList");
		VerticalLayoutGroup verticalLayoutGroup = presetList.gameObject.AddComponent<VerticalLayoutGroup>();
		verticalLayoutGroup.spacing = 5f;
		verticalLayoutGroup.childControlHeight = true;
		verticalLayoutGroup.childForceExpandHeight = false;
		presetList.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		foreach (RadarPreset preset in RadarCatalog.Presets)
		{
			string id = preset.Id;
			Button button = Button(presetList, "Preset_" + id, preset.NameKey, 38f, delegate
			{
				state.SelectPreset(id);
				presetList.gameObject.SetActive(false);
			}, 16f);
			presetButtons.Add(id, button);
		}
		Button(presetList, "Preset_custom", "radar.preset.custom", 38f, delegate
		{
			state.SelectPreset("custom");
			presetList.gameObject.SetActive(false);
		}, 16f);
		presetList.gameObject.SetActive(false);
		roleReadout = AeroReadout.Attach(Label(content, "PresetRole", "radar.role.x_air", 15f, 38f), "radar.role.x_air");
		examplesReadout = AeroReadout.Attach(Label(content, "PresetExamples", "radar.example.x_air", 15f, 46f), "radar.example.x_air");
		Label(content, "FrequencyTitle", "radar.freq.title", 18f, 26f);
		RectTransform rect5 = MenuTheme.MakeRow(content, "FrequencyRow", 8f, forceExpandWidth: true);
		Height(rect5, 38f);
		frequencySlider = MakeSlider(rect5, "FrequencySlider", 0f, 1f, 0f, delegate(float value)
		{
			state.SetFrequencySlider(value);
		});
		frequencyField = MakeField(rect5, "FrequencyInput", 142f, "10 GHz", delegate(string value)
		{
			if (state.TrySetFrequencyText(value, out var error))
			{
				SetStatus("radar.solver.pending", Array.Empty<object>());
			}
			else
			{
				SetStatus(error, Array.Empty<object>());
			}
			RefreshUI();
		});
		frequencyField.onEndEdit.AddListener(delegate(string _)
		{
			frequencyField.SetTextWithoutNotify(Frequency(state.FrequencyHz));
			RefreshUI();
		});
		bandReadout = AeroReadout.Attach(Label(content, "Band", "radar.band", 17f, 28f), "radar.band");
		lambdaReadout = AeroReadout.Attach(Label(content, "Wavelength", "radar.lambda", 17f, 28f), "radar.lambda");
		Label(content, "PolarizationTitle", "radar.pol", 17f, 28f);
		RectTransform rect6 = MenuTheme.MakeRow(content, "Polarizations", 8f, forceExpandWidth: true);
		Height(rect6, 38f);
		AddPol(rect6, RadarPolarization.HH, "radar.pol.hh");
		AddPol(rect6, RadarPolarization.VV, "radar.pol.vv");
		AddPol(rect6, RadarPolarization.HV, "radar.pol.hv");
		AddPol(rect6, RadarPolarization.VH, "radar.pol.vh");
		BuildCoatingUI(content);
		Label(content, "DirectionTitle", "radar.direction", 22f, 30f);
		azReadout = AeroReadout.Attach(Label(content, "Azimuth", "radar.az", 18f, 26f), "radar.az");
		RectTransform rect7 = MenuTheme.MakeRow(content, "AzimuthControls", 8f, forceExpandWidth: true);
		Height(rect7, 38f);
		azimuthSlider = MakeSlider(rect7, "AzimuthSlider", -180f, 180f, 0f, delegate(float value)
		{
			state.SetAzimuth(value);
		});
		azimuthField = MakeField(rect7, "AzimuthInput", 90f, "0", delegate(string text)
		{
			if (!TryNumber(text, out var value))
			{
				SetStatus("radar.error.angle", Array.Empty<object>());
			}
			else
			{
				SetStatus("radar.solver.pending", Array.Empty<object>());
				state.SetAzimuth(value);
			}
			RefreshUI();
		});
		azimuthField.onEndEdit.AddListener(delegate(string _)
		{
			azimuthField.SetTextWithoutNotify(state.AzimuthDeg.ToString("F1", CultureInfo.InvariantCulture));
			RefreshUI();
		});
		elReadout = AeroReadout.Attach(Label(content, "Elevation", "radar.el", 18f, 26f), "radar.el");
		RectTransform rect8 = MenuTheme.MakeRow(content, "ElevationControls", 8f, forceExpandWidth: true);
		Height(rect8, 38f);
		elevationSlider = MakeSlider(rect8, "ElevationSlider", -90f, 90f, 0f, delegate(float value)
		{
			state.SetElevation(value);
		});
		elevationField = MakeField(rect8, "ElevationInput", 90f, "0", delegate(string text)
		{
			if (!TryNumber(text, out var value))
			{
				SetStatus("radar.error.angle", Array.Empty<object>());
			}
			else
			{
				SetStatus("radar.solver.pending", Array.Empty<object>());
				state.SetElevation(value);
			}
			RefreshUI();
		});
		elevationField.onEndEdit.AddListener(delegate(string _)
		{
			elevationField.SetTextWithoutNotify(state.ElevationDeg.ToString("F1", CultureInfo.InvariantCulture));
			RefreshUI();
		});
		RectTransform rect9 = MenuTheme.MakeRow(content, "DirectionShortcuts", 8f, forceExpandWidth: true);
		Height(rect9, 38f);
		AddDirection(rect9, "Front", "radar.dir.front", 0.0, 0.0);
		AddDirection(rect9, "Right", "radar.dir.right", 90.0, 0.0);
		AddDirection(rect9, "Rear", "radar.dir.rear", 180.0, 0.0);
		AddDirection(rect9, "Top", "radar.dir.top", 0.0, 90.0);
		AddDirection(rect9, "Bottom", "radar.dir.bottom", 0.0, -90.0);
		radarViewButton = Button(content, "RadarView", "radar.view_from_radar", 38f, delegate
		{
			state.SetViewFromRadar(!state.ViewFromRadar);
		}, 16f);
		Label(content, "DirectionHint", "radar.dir.hint", 15f, 48f);
		Label(content, "DirectionKeys", "radar.keys", 15f, 48f);
		esizeReadout = AeroReadout.Attach(Label(content, "ElectricalSize", "radar.esize", 18f, 30f), "radar.esize");
		statusReadout = AeroReadout.Attach(Label(content, "SolverStatus", "radar.solver.pending", 17f, 56f), "radar.solver.pending");
		Button(content, "Back", "radar.menu", 40f, BackToMenu, 18f);
		TextMeshProUGUI textMeshProUGUI = Label(canvas, "CameraHelp", "radar.camera", 18f, 55f);
		textMeshProUGUI.color = ResultPalette.Miss;
		textMeshProUGUI.raycastTarget = false;
		MenuTheme.Place(textMeshProUGUI.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -22f), new Vector2(1220f, 55f));
		TextMeshProUGUI disclaimerLabel = Label(canvas, "BenchDisclaimer", "radar.disclaimer.bench", 17f, 58f);
		modeDisclaimer = AeroReadout.Attach(disclaimerLabel, "radar.disclaimer.bench");
		disclaimerLabel.color = ResultPalette.Miss;
		disclaimerLabel.raycastTarget = false;
		MenuTheme.Place(disclaimerLabel.rectTransform, Vector2.zero, Vector2.zero, new Vector2(28f, 18f), new Vector2(1220f, 58f));
		BuildPropagationOverlay();
		RefreshUI();
	}

	private void BuildReferences(Transform column)
	{
		referencesList = MenuTheme.MakeRect(column, "ReferenceList");
		VerticalLayoutGroup verticalLayoutGroup = referencesList.gameObject.AddComponent<VerticalLayoutGroup>();
		verticalLayoutGroup.spacing = 5f;
		verticalLayoutGroup.childControlHeight = true;
		verticalLayoutGroup.childForceExpandHeight = false;
		ContentSizeFitter contentSizeFitter = referencesList.gameObject.AddComponent<ContentSizeFitter>();
		contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		string[] array = new string[6] { "sphere", "plate", "cylinder", "dihedral", "trihedral_sq", "trihedral_tri" };
		for (int i = 0; i < array.Length; i++)
		{
			string picked = array[i];
			Button(referencesList, "Reference_" + picked, "radar.ref." + picked, 38f, delegate
			{
				SelectReference(picked);
			}, 16f);
		}
		referencesList.gameObject.SetActive(false);
	}

	private void AddUnit(Transform row, RadarLengthUnit value, string key)
	{
		Button button = Button(row, "Unit_" + value.ToString(), key, 36f, delegate
		{
			SetUnit(value);
		}, 16f);
		unitButtons[value] = button;
	}

	private void AddPol(Transform row, RadarPolarization value, string key)
	{
		Button button = Button(row, "Polarization_" + value.ToString(), key, 36f, delegate
		{
			state.SetPolarization(value);
		}, 16f);
		polButtons[value] = button;
	}

	private void AddDirection(Transform row, string name, string key, double az, double el)
	{
		Button(row, name, key, 36f, delegate
		{
			state.SetDirection(az, el);
		}, 14f);
	}

	private static bool TryNumber(string text, out double value)
	{
		if (double.TryParse((text ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
		{
			return double.IsFinite(value);
		}
		return false;
	}

	private static void SyncField(TMP_InputField field, string text)
	{
		if (field != null && (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != field.gameObject))
		{
			field.SetTextWithoutNotify(text);
		}
	}

	private static string Number(double value)
	{
		return value.ToString("G4", CultureInfo.InvariantCulture);
	}

	private static string Frequency(double hz)
	{
		return RadarBands.FormatFrequency(hz, Localization.T("radar.freq.mhz"), Localization.T("radar.freq.ghz"));
	}

	private static string Length(double metres)
	{
		double num = metres;
		if (metres < 1.0)
		{
			num = ((metres < 0.01) ? (metres * 1000.0) : (metres * 100.0));
		}
		string key = (metres >= 1.0) ? "radar.unit.m" : ((metres < 0.01) ? "radar.unit.mm" : "radar.unit.cm");
		int num2 = Math.Max(0, 2 - (int)Math.Floor(Math.Log10(num)));
		return num.ToString("F" + num2, CultureInfo.InvariantCulture) + " " + Localization.T(key);
	}

	private void RefreshUI()
	{
		if (state == null || canvas == null)
		{
			return;
		}
		if (modelNameReadout != null)
		{
			modelNameReadout.Set("radar.model.name", (referenceNameKey != null) ? Localization.T(referenceNameKey) : (currentName ?? ""));
		}
		if (fileSizeReadout != null)
		{
			Vector3 vector = ((imported != null) ? imported.OriginalSize : ((currentModel != null) ? V(currentModel.Size) : Vector3.one));
			fileSizeReadout.Set("radar.model.file_size", Number(vector.x), Number(vector.y), Number(vector.z));
		}
		if (sizeReadout != null && currentModel != null)
		{
			sizeReadout.Set("radar.model.size_m", Number(V(currentModel.Size).x), Number(V(currentModel.Size).y), Number(V(currentModel.Size).z));
		}
		if (triangleReadout != null && currentModel != null)
		{
			triangleReadout.Set("radar.model.tris", currentModel.TriangleCount, currentModel.DegenerateTriangles + (imported?.DegenerateTriangles ?? 0));
		}
		if (unitRow != null)
		{
			unitRow.gameObject.SetActive(!isReference);
		}
		if (customLengthField != null)
		{
			customLengthField.transform.parent.gameObject.SetActive(!isReference && unit == RadarLengthUnit.CustomLength);
			SyncField(customLengthField, Number(customLengthMeters));
		}
		foreach (KeyValuePair<RadarLengthUnit, Button> item in unitButtons)
		{
			MenuTheme.SetSelectedVisual(item.Value, item.Key == unit);
		}
		if (hintReadout != null)
		{
			float num = 0f;
			if (imported != null)
			{
				num = Math.Max(imported.OriginalSize.x, Math.Max(imported.OriginalSize.y, imported.OriginalSize.z));
			}
			bool flag = !isReference && ((double)num > 1000.0 || 0.5 > (double)num);
			hintReadout.gameObject.SetActive(flag);
			if (flag)
			{
				hintReadout.Set((!(num <= 1000f)) ? "radar.unit.hint_mm" : "radar.unit.hint_small", Array.Empty<object>());
			}
		}
		string text = ((state.PresetId != "custom") ? (RadarCatalog.TryGet(state.PresetId, out var preset) ? Localization.T(preset.NameKey) : state.PresetId) : Localization.T("radar.preset.custom"));
		if (presetButton != null)
		{
			TextMeshProUGUI label = MenuTheme.LabelOf(presetButton);
			LocalizedLabel component = label.GetComponent<LocalizedLabel>();
			if (component != null)
			{
				component.enabled = false;
			}
			label.text = Localization.T("radar.preset.current", text, state.BandLetter, Frequency(state.FrequencyHz));
		}
		foreach (KeyValuePair<string, Button> item2 in presetButtons)
		{
			MenuTheme.SetSelectedVisual(item2.Value, item2.Key == state.PresetId);
		}
		bool flag2 = RadarCatalog.TryGet(state.PresetId, out var preset2);
		if (roleReadout != null)
		{
			roleReadout.gameObject.SetActive(flag2);
			if (flag2)
			{
				roleReadout.Set(preset2.RoleKey, Array.Empty<object>());
			}
		}
		if (examplesReadout != null)
		{
			examplesReadout.gameObject.SetActive(flag2);
			if (flag2)
			{
				examplesReadout.Set(preset2.ExamplesKey, Array.Empty<object>());
			}
		}
		if (frequencySlider != null)
		{
			frequencySlider.value = (float)state.FrequencySlider01;
		}
		SyncField(frequencyField, Frequency(state.FrequencyHz));
		if (bandReadout != null)
		{
			bandReadout.Set("radar.band", state.BandLetter + " · " + Localization.T(state.WaveClassKey));
		}
		if (lambdaReadout != null)
		{
			lambdaReadout.Set("radar.lambda", Length(state.WavelengthM));
		}
		foreach (KeyValuePair<RadarPolarization, Button> item3 in polButtons)
		{
			MenuTheme.SetSelectedVisual(item3.Value, item3.Key == state.Polarization);
		}
		if (azReadout != null)
		{
			azReadout.Set("radar.az", state.AzimuthDeg.ToString("F1", CultureInfo.InvariantCulture));
		}
		if (elReadout != null)
		{
			elReadout.Set("radar.el", state.ElevationDeg.ToString("F1", CultureInfo.InvariantCulture));
		}
		if (azimuthSlider != null)
		{
			azimuthSlider.value = (float)state.AzimuthDeg;
		}
		if (elevationSlider != null)
		{
			elevationSlider.value = (float)state.ElevationDeg;
		}
		SyncField(azimuthField, state.AzimuthDeg.ToString("F1", CultureInfo.InvariantCulture));
		SyncField(elevationField, state.ElevationDeg.ToString("F1", CultureInfo.InvariantCulture));
		if (radarViewButton != null)
		{
			MenuTheme.SetSelectedVisual(radarViewButton, state.ViewFromRadar);
		}
		if (esizeReadout != null && currentModel != null)
		{
			esizeReadout.Set("radar.esize", (currentModel.LongestSide / state.WavelengthM).ToString("G4", CultureInfo.InvariantCulture));
		}
		if (statusReadout != null)
		{
			statusReadout.Set(statusKey, statusArgs);
		}
		RefreshModeUI();
		RefreshToMap();
		RefreshCoatingUI();
	}

	private void SetStatus(string key, params object[] args)
	{
		statusKey = key;
		statusArgs = ((args != null) ? args : Array.Empty<object>());
		if (statusReadout != null)
		{
			statusReadout.Set(statusKey, statusArgs);
		}
	}

	private void PickFile()
	{
		ShowFileList();
	}

	public void ShowFileList()
	{
		Directory.CreateDirectory(ModelsFolder);
		try
		{
			if (filePanel != null)
			{
				UnityEngine.Object.Destroy(filePanel.gameObject);
			}
			filePanel = MenuTheme.MakeRect(canvas, "ModelFiles");
			filePanel.anchorMin = new Vector2(0.1f, 0.1f);
			filePanel.anchorMax = new Vector2(0.9f, 0.9f);
			filePanel.offsetMax = Vector2.zero;
			filePanel.offsetMin = Vector2.zero;
			MenuTheme.AddImage(filePanel, MenuTheme.Surface).raycastTarget = true;
			RectTransform content;
			MenuTheme.Stretch((RectTransform)MenuTheme.MakeScrollList(filePanel, "FilesScroll", out content).transform, new Vector2(24f, 24f), new Vector2(-24f, -24f));
			Button(content, "CloseFiles", "radar.close", 40f, delegate
			{
				if (filePanel != null)
				{
					UnityEngine.Object.Destroy(filePanel.gameObject);
				}
			}, 16f);
			AeroReadout aeroReadout = AeroReadout.Attach(Label(content, "Folder", "radar.models.folder", 17f, 62f), "radar.models.folder");
			aeroReadout.Set("radar.models.folder", ModelsFolder);
			int num = 0;
			foreach (string path in Directory.EnumerateFiles(ModelsFolder))
			{
				string extension = Path.GetExtension(path);
				if (extension.Equals(".obj", StringComparison.OrdinalIgnoreCase) || extension.Equals(".stl", StringComparison.OrdinalIgnoreCase))
				{
					Button button = Button(content, "ModelFile", "radar.model.name", 42f, delegate
					{
						if (filePanel != null)
						{
							UnityEngine.Object.Destroy(filePanel.gameObject);
						}
						LoadPath(path);
					}, 16f);
					AeroReadout aeroReadout2 = AeroReadout.Attach(MenuTheme.LabelOf(button), "radar.model.name");
					aeroReadout2.Set("radar.model.name", Path.GetFileName(path));
					num++;
				}
			}
			if (num == 0)
			{
				Label(content, "EmptyFolder", "radar.models.empty", 18f, 80f);
			}
		}
		catch (Exception error)
		{
			Report(new AeroException("aero.error.io", new object[1] { error.Message }));
		}
	}

	private void OnEnable()
	{
		Localization.OnLanguageChanged += RefreshUI;
	}

	private void OnDisable()
	{
		Localization.OnLanguageChanged -= RefreshUI;
	}

	private void BuildPatternUI(Transform parent)
	{
		patternButton = Button(parent, "Pattern3D", "radar.pattern.calculate", 38f, RequestPattern, 16f);
		patternCaption = AeroReadout.Attach(Label(parent, "PatternCaption", "radar.pattern.caption", 15f, 64f), "radar.pattern.caption");
	}

	public void RequestPattern()
	{
		if (patternTask != null)
		{
			return;
		}
		if (displayMode != RadarDisplayMode.Propagation && displayMode != RadarDisplayMode.Wavefronts)
		{
			return;
		}
		if (currentModel == null)
		{
			return;
		}
		if (!isReference && !importScaleConfirmed)
		{
			return;
		}
		EnsurePropagationView();
		if (propagation.ShowPattern && patternRequested)
		{
			propagation.ShowPattern = false;
			patternRequested = false;
			orbit.Configure(V(currentModel.Center), DisplayRadius(currentModel));
			RefreshPatternUI();
			return;
		}
		patternRevision = solveRevision;
		runningPatternGeneration = patternGeneration;
		patternRequested = true;
		patternAuto = true;
		RadarModel model = currentModel;
		RadarScene existing = ((sceneModel == model) ? solverScene : null);
		RadarQuery query = new RadarQuery(state.FrequencyHz, state.AzimuthDeg, state.ElevationDeg);
		PolIndices(out var rx, out var tx);
		RadarCoating coating = benchCoating;
		patternCancel = new CancellationTokenSource();
		CancellationToken token = patternCancel.Token;
		patternProgress = 0;
		patternTotal = 312;
		patternTask = Task.Run(delegate
		{
			RadarScene radarScene = existing;
			if (radarScene == null)
			{
				radarScene = new RadarScene(model, token);
			}
			return RadarSolver.SolvePattern(radarScene, query, rx, tx, coating, token, delegate(int done, int total)
			{
				patternProgress = done;
				patternTotal = total;
			});
		});
		patternTask.ContinueWith(delegate(Task<RadarPatternResult> t)
		{
			_ = t.Exception;
		}, TaskContinuationOptions.OnlyOnFaulted);
		RefreshPatternUI();
	}

	private void UpdatePattern()
	{
		if (patternTask == null)
		{
			if (patternAuto && !patternRequested && !solvePending && solveTask == null && radarResult != null && !radarResult.Refused && (displayMode == RadarDisplayMode.Propagation || displayMode == RadarDisplayMode.Wavefronts))
			{
				RequestPattern();
			}
			return;
		}
		RefreshPatternUI();
		if (!patternTask.IsCompleted)
		{
			return;
		}
		Task<RadarPatternResult> task = patternTask;
		patternTask = null;
		patternCancel.Dispose();
		patternCancel = null;
		try
		{
			RadarPatternResult radarPatternResult = task.GetAwaiter().GetResult();
			if (patternRevision != solveRevision || runningPatternGeneration != patternGeneration || (displayMode != RadarDisplayMode.Propagation && displayMode != RadarDisplayMode.Wavefronts))
			{
				patternRequested = false;
				return;
			}
			if (radarPatternResult.Refused)
			{
				propagation.ClearPattern();
				patternRequested = false;
				return;
			}
			Vector3[] array = new Vector3[radarPatternResult.Directions.Length];
			float[] array2 = new float[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				double3 directions = radarPatternResult.Directions[i];
				array[i] = new Vector3((float)directions.x, (float)directions.y, (float)directions.z);
				array2[i] = (float)radarPatternResult.RelativePower[i];
			}
			propagation.SetPattern(array, array2, radarPatternResult.AzimuthSamples, radarPatternResult.ElevationSamples);
			propagation.ShowPattern = true;
			float num = DisplayRadius(currentModel);
			RadarDirectionBasis basis = state.Basis;
			Vector3 vector = V(currentModel.Center) + V(basis.Horizontal) * (num * 1.4f);
			orbit.Configure(vector, num * 2.2f);
		}
		catch (OperationCanceledException)
		{
			RefreshPatternUI();
			return;
		}
		catch (Exception error)
		{
			if (patternRevision == solveRevision)
			{
				Report(error);
			}
		}
		RefreshPatternUI();
	}

	private void RefreshPatternUI()
	{
		if (patternButton == null)
		{
			return;
		}
		object[] args;
		string key;
		if (patternTask == null)
		{
			key = ((!patternRequested || propagation == null || !propagation.ShowPattern) ? "radar.pattern.calculate" : "radar.pattern.hide");
			args = Array.Empty<object>();
		}
		else
		{
			args = new object[2] { patternProgress, patternTotal };
			key = "radar.pattern.progress";
		}
		AeroReadoutText(patternButton, key, args);
		patternCaption?.Set("radar.pattern.caption", Array.Empty<object>());
	}

	private void CancelPattern()
	{
		patternGeneration++;
		patternCancel?.Cancel();
		patternRequested = false;
	}

	private void BuildModeUI(Transform column)
	{
		modeButton = Button(column, "RadarMode", "radar.mode.select", 42f, delegate
		{
			modeChoices.gameObject.SetActive(!modeChoices.gameObject.activeSelf);
		}, 18f);
		modeChoices = MenuTheme.MakeColumn(column, "RadarModes", 6f);
		propagationModeButton = Button(modeChoices, "ModePropagation", "radar.mode.propagation", 40f, delegate
		{
			SelectDisplayMode(RadarDisplayMode.Propagation);
		}, 16f);
		wavefrontModeButton = Button(modeChoices, "ModeWavefronts", "radar.mode.wavefronts", 40f, delegate
		{
			SelectDisplayMode(RadarDisplayMode.Wavefronts);
		}, 16f);
		simpleModeButton = Button(modeChoices, "ModeSimple", "radar.mode.simple", 40f, delegate
		{
			SelectDisplayMode(RadarDisplayMode.SimpleBench);
		}, 16f);
		atlasModeButton = Button(modeChoices, "ModeAtlas", "radar.input.empty", 40f, delegate
		{
			SelectDisplayMode(RadarDisplayMode.Atlas);
		}, 16f);
		LocalizedLabel[] componentsInChildren = atlasModeButton.GetComponentsInChildren<LocalizedLabel>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		modeChoices.gameObject.SetActive(false);
		BuildScenarioEntry(column);
		propagationControls = MenuTheme.MakeColumn(column, "PropagationControls", 6f);
		RectTransform rect = MenuTheme.MakeRow(propagationControls, "SolveActions", 8f, forceExpandWidth: true);
		Height(rect, 38f);
		Button(rect, "SolveNormal", "radar.solve.normal", 36f, delegate
		{
			RequestCalculation(RadarQuality.Normal);
		}, 16f);
		Button(rect, "SolveAccurate", "radar.solve.accurate", 36f, delegate
		{
			RequestCalculation(RadarQuality.Accurate);
		}, 16f);
		Button(rect, "CancelSolve", "radar.solve.cancel", 36f, StopCalculation, 16f);
		RectTransform rect2 = MenuTheme.MakeRow(propagationControls, "PropagationViews", 8f, forceExpandWidth: true);
		Height(rect2, 38f);
		rayButton = Button(rect2, "ShowRays", "radar.show.rays", 36f, delegate
		{
			EnsurePropagationView();
			propagation.ShowRays = !propagation.ShowRays;
			RefreshModeUI();
		}, 16f);
		waveButton = Button(rect2, "ShowWaves", "radar.show.waves", 36f, delegate
		{
			EnsurePropagationView();
			propagation.ShowWavefronts = !propagation.ShowWavefronts;
			RefreshModeUI();
		}, 16f);
		heatButton = Button(rect2, "ShowHeatmap", "radar.show.heat", 36f, delegate
		{
			heatmap = !heatmap;
			ApplyHeatmap();
			RefreshModeUI();
		}, 16f);
		waveModeHint = AeroReadout.Attach(Label(propagationControls, "WavefrontModeHint", "radar.wavefronts.caption", 15f, 58f), "radar.wavefronts.caption");
		RectTransform rect3 = MenuTheme.MakeRow(propagationControls, "SignalDensity", 8f, forceExpandWidth: true);
		Height(rect3, 38f);
		Label(rect3, "DensityLabel", "radar.signal.density", 15f, 34f);
		int[] array = new int[3] { 1500, 3000, 8000 };
		foreach (int count in array)
		{
			Button button = Button(rect3, "Density_" + count, "radar.density.value", 36f, delegate
			{
				SetPathDensity(count);
			}, 15f);
			AeroReadoutText(button, "radar.density.value", count);
			densityButtons.Add(count, button);
		}
		BuildPatternUI(propagationControls);
		detailReadout = AeroReadout.Attach(Label(propagationControls, "SolverDetails", "radar.input.empty", 14f, 42f), "radar.input.empty");
		waveReadout = AeroReadout.Attach(Label(propagationControls, "WaveSpacing", "radar.input.empty", 14f, 44f), "radar.input.empty");
	}

	public void SetPathDensity(int count)
	{
		if (count != 1500 && count != 3000 && count != 8000)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		pathDensity = count;
		EnsurePropagationView();
		propagation.MaxVisiblePaths = count;
		RefreshModeUI();
	}

	private void BuildPropagationOverlay()
	{
		propagationOverlay = MenuTheme.MakeColumn(canvas, "PropagationOverlay", 5f);
		MenuTheme.Place(propagationOverlay, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -88f), new Vector2(820f, 185f));
		resultReadout = AeroReadout.Attach(Label(propagationOverlay, "RcsResult", "radar.solve.wait", 23f, 34f), "radar.solve.wait");
		counterReadout = AeroReadout.Attach(Label(propagationOverlay, "RayCounters", "radar.input.empty", 15f, 42f), "radar.input.empty");
		propagationStatus = AeroReadout.Attach(Label(propagationOverlay, "PropagationStatus", "radar.solve.wait", 16f, 45f), "radar.solve.wait");
		scaleReadout = AeroReadout.Attach(Label(propagationOverlay, "HeatScale", "radar.heat.scale", 16f, 28f), "radar.heat.scale");
		RectTransform rect = Row(propagationOverlay, "HeatLegend", 8f);
		for (int i = 0; i <= 10; i++)
		{
			RectTransform rect2 = MenuTheme.MakeRect(rect, "dB_" + (i * 4 - 40));
			LayoutElement layoutElement = rect2.gameObject.AddComponent<LayoutElement>();
			layoutElement.minWidth = 8f;
			layoutElement.flexibleWidth = 1f;
			MenuTheme.AddImage(rect2, ContributionColor(i / 10f)).raycastTarget = false;
		}
		Graphic[] componentsInChildren = propagationOverlay.GetComponentsInChildren<Graphic>();
		for (int j = 0; j < componentsInChildren.Length; j++)
		{
			componentsInChildren[j].raycastTarget = false;
		}
		TMP_Text[] componentsInChildren2 = propagationOverlay.GetComponentsInChildren<TMP_Text>();
		for (int k = 0; k < componentsInChildren2.Length; k++)
		{
			componentsInChildren2[k].color = new Color(0.16f, 0.18f, 0.2f, 1f);
		}
	}

	public void SelectDisplayMode(RadarDisplayMode value)
	{
		if (value > RadarDisplayMode.Wavefronts && value != RadarDisplayMode.Atlas)
		{
			throw new ArgumentOutOfRangeException("value");
		}
		if (value == RadarDisplayMode.Atlas && atlasView == null)
		{
			atlasView = gameObject.AddComponent<RadarAtlasView>();
			atlasView.Bench = this;
		}
		if (atlasView != null)
		{
			atlasView.enabled = value == RadarDisplayMode.Atlas;
		}
		bool flag = displayMode == RadarDisplayMode.Propagation || displayMode == RadarDisplayMode.Wavefronts;
		displayMode = value;
		if (modeChoices != null)
		{
			modeChoices.gameObject.SetActive(false);
		}
		if (displayMode == RadarDisplayMode.Propagation || displayMode == RadarDisplayMode.Wavefronts)
		{
			EnsurePropagationView();
		}
		if (propagation != null)
		{
			propagation.gameObject.SetActive(displayMode == RadarDisplayMode.Propagation || displayMode == RadarDisplayMode.Wavefronts);
			propagation.WavefrontOnlyPresentation = value == RadarDisplayMode.Wavefronts;
			if (value == RadarDisplayMode.Wavefronts)
			{
				propagation.ShowWavefronts = true;
			}
		}
		if (!flag || (displayMode != RadarDisplayMode.Propagation && displayMode != RadarDisplayMode.Wavefronts))
		{
			if (propagation != null && propagation.ShowPattern && currentModel != null)
			{
				orbit.Configure(V(currentModel.Center), DisplayRadius(currentModel));
			}
			solveRevision++;
			solveCancel?.Cancel();
			patternGeneration++;
			patternCancel?.Cancel();
			solvePending = false;
			patternRequested = false;
			if (propagation != null)
			{
				propagation.ClearPattern();
			}
			if (visuals != null)
			{
				visuals.gameObject.SetActive(true);
				visuals.SetSimpleGuidesVisible(value == RadarDisplayMode.SimpleBench);
			}
		}
		displayMaterial?.SetFloat("_ShowMap", ((displayMode == RadarDisplayMode.Propagation || displayMode == RadarDisplayMode.Wavefronts) && heatmap && radarResult != null) ? 1f : 0f);
		if (displayMode == RadarDisplayMode.Propagation || displayMode == RadarDisplayMode.Wavefronts)
		{
			if (radarResult != null)
			{
				ConfigurePropagationView();
				ApplyHeatmap();
			}
			else
			{
				InvalidatePropagation(geometry: false, force: true);
			}
		}
		RefreshModeUI();
	}

	private void EnsurePropagationView()
	{
		if (propagation != null)
		{
			return;
		}
		GameObject gameObject = new GameObject("RadarPropagation");
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		propagation = gameObject.AddComponent<RadarPropagationVisuals>();
		propagation.MaxVisiblePaths = pathDensity;
		propagation.WavefrontOnlyPresentation = displayMode == RadarDisplayMode.Wavefronts;
	}

	private void InvalidatePropagation(bool geometry, bool force = false)
	{
		if (geometry)
		{
			solverScene = null;
			sceneModel = null;
		}
		if (!modesRunning || currentModel == null || state == null)
		{
			return;
		}
		if (geometry || force || state.FrequencyHz != lastFrequency || state.AzimuthDeg != lastAzimuth || state.ElevationDeg != lastElevation)
		{
			lastPatternPolarization = state.Polarization;
			lastFrequency = state.FrequencyHz;
			lastAzimuth = state.AzimuthDeg;
			lastElevation = state.ElevationDeg;
			solveRevision++;
			solveCancel?.Cancel();
			patternGeneration++;
			patternCancel?.Cancel();
			solvePending = false;
			patternRequested = false;
			radarResult = null;
			if (displayMaterial != null)
			{
				displayMaterial.SetFloat("_ShowMap", 0f);
			}
			if (propagation != null)
			{
				propagation.ClearPaths();
				propagation.ClearPattern();
			}
			if (displayMode == RadarDisplayMode.Propagation || displayMode == RadarDisplayMode.Wavefronts)
			{
				EnsurePropagationView();
				ConfigurePropagationView();
				RequestCalculation(RadarQuality.Preview);
			}
			ApplyHeatmap();
			RefreshModeUI();
			return;
		}
		RadarPolarization polarization = lastPatternPolarization;
		lastPatternPolarization = state.Polarization;
		if (polarization != state.Polarization)
		{
			patternGeneration++;
			patternCancel?.Cancel();
			patternRequested = false;
			if (propagation != null)
			{
				propagation.ClearPattern();
			}
		}
		ApplyHeatmap();
		RefreshModeUI();
	}

	private void ConfigurePropagationView()
	{
		RadarDirectionBasis basis = state.Basis;
		propagation.SetGeometry(V(currentModel.Center), DisplayRadius(currentModel), V(basis.ToRadar), V(basis.Horizontal), V(basis.Vertical), (float)state.WavelengthM);
		propagation.gameObject.SetActive(displayMode == RadarDisplayMode.Propagation || displayMode == RadarDisplayMode.Wavefronts);
		visuals.gameObject.SetActive(true);
		visuals.SetSimpleGuidesVisible(displayMode == RadarDisplayMode.SimpleBench);
	}

	public void RequestCalculation(RadarQuality quality)
	{
		if (displayMode != RadarDisplayMode.Propagation && displayMode != RadarDisplayMode.Wavefronts)
		{
			return;
		}
		if (currentModel == null)
		{
			return;
		}
		if (!isReference && !importScaleConfirmed)
		{
			RefreshModeUI();
			return;
		}
		LastErrorKey = null;
		solveRevision++;
		solveCancel?.Cancel();
		patternGeneration++;
		patternCancel?.Cancel();
		patternRequested = false;
		nextQuality = quality;
		solvePending = true;
		solveDue = Time.unscaledTime + ((quality != RadarQuality.Preview) ? 0f : 0.25f);
		RefreshModeUI();
	}

	public void StopCalculation()
	{
		solveRevision++;
		solveCancel?.Cancel();
		patternGeneration++;
		patternCancel?.Cancel();
		solvePending = false;
		patternAuto = false;
		patternRequested = false;
		propagationStatus?.Set("radar.solve.cancelled", Array.Empty<object>());
	}

	private void UpdatePropagation()
	{
		UpdatePattern();
		if (solveTask != null && solveTask.IsCompleted)
		{
			Task<RadarResult> task = solveTask;
			solveTask = null;
			solveCancel.Dispose();
			solveCancel = null;
			if (sceneTask != null && sceneTask.Status == TaskStatus.RanToCompletion && currentModel == runningModel)
			{
				solverScene = sceneTask.Result;
				sceneModel = runningModel;
			}
			try
			{
				RadarResult result = task.GetAwaiter().GetResult();
				if (runningRevision == solveRevision && (displayMode == RadarDisplayMode.Propagation || displayMode == RadarDisplayMode.Wavefronts))
				{
					radarResult = result;
					PublishPropagation();
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception error)
			{
				if (runningRevision == solveRevision)
				{
					Report(error);
				}
			}
			RefreshModeUI();
		}
		if (modesRunning && solvePending && solveTask == null && Time.unscaledTime >= solveDue)
		{
			solvePending = false;
			if ((displayMode == RadarDisplayMode.Propagation || displayMode == RadarDisplayMode.Wavefronts) && currentModel != null && (isReference || importScaleConfirmed))
			{
				runningRevision = solveRevision;
				RadarModel model = currentModel;
				runningModel = model;
				RadarScene existing = ((sceneModel == model) ? solverScene : null);
				RadarQuery query = new RadarQuery(state.FrequencyHz, state.AzimuthDeg, state.ElevationDeg);
				RadarQuality quality = nextQuality;
				RadarCoating coating = benchCoating;
				solveCancel = new CancellationTokenSource();
				CancellationToken token = solveCancel.Token;
				sceneTask = Task.Run(delegate
				{
					RadarScene radarScene = existing;
					if (radarScene == null)
					{
						radarScene = new RadarScene(model, token);
					}
					return radarScene;
				});
				solveTask = Task.Run(async delegate
				{
					RadarScene radarScene2 = await sceneTask.ConfigureAwait(continueOnCapturedContext: false);
					token.ThrowIfCancellationRequested();
					return RadarSolver.Solve(radarScene2, query, coating, quality, token);
				});
				solveTask.ContinueWith(delegate(Task<RadarResult> t)
				{
					_ = t.Exception;
				}, TaskContinuationOptions.OnlyOnFaulted);
			}
			RefreshModeUI();
		}
	}

	private void PublishPropagation()
	{
		EnsurePropagationView();
		ConfigurePropagationView();
		if (radarResult.Refused)
		{
			propagation.ClearPaths();
		}
		else
		{
			Vector3[][] array = new Vector3[radarResult.Paths.Length][];
			float[] array2 = new float[radarResult.Paths.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new Vector3[radarResult.Paths[i].Points.Length];
				for (int j = 0; j < array[i].Length; j++)
				{
					double3 points = radarResult.Paths[i].Points[j];
					array[i][j] = new Vector3((float)points.x, (float)points.y, (float)points.z);
				}
				array2[i] = 1f;
			}
			propagation.SetPaths(array, array2, V(currentModel.Center), DisplayRadius(currentModel), (float)state.WavelengthM);
		}
		ApplyHeatmap();
	}

	private void ApplyHeatmap()
	{
		if (displayMaterial == null || displayMesh == null)
		{
			return;
		}
		bool flag = (displayMode == RadarDisplayMode.Propagation || displayMode == RadarDisplayMode.Wavefronts) && heatmap && radarResult != null && !radarResult.Refused;
		displayMaterial.SetFloat("_ShowMap", flag ? 1f : 0f);
		if (!flag)
		{
			return;
		}
		int num = ((state.Polarization == RadarPolarization.VV || state.Polarization == RadarPolarization.VH) ? 1 : 0);
		int num2 = ((state.Polarization == RadarPolarization.VV || state.Polarization == RadarPolarization.HV) ? 1 : 0);
		double[] array = radarResult.GetFacetAmplitude(num2, num);
		double num3 = 0.0;
		for (int i = 0; i < array.Length; i++)
		{
			num3 = Math.Max(num3, array[i]);
		}
		double[] array2 = new double[currentModel.Triangles.Length];
		double[] array3 = new double[currentModel.Triangles.Length];
		for (int j = 0; j < currentModel.TriangleCount; j++)
		{
			for (int k = 0; k < 3; k++)
			{
				int num4 = currentModel.Triangles[3 * j + k];
				array2[num4] += currentModel.Areas[j] * array[j];
				array3[num4] += currentModel.Areas[j];
			}
		}
		Color[] array4 = new Color[array2.Length];
		for (int l = 0; l < array4.Length; l++)
		{
			double num5 = ((array3[l] > 0.0) ? (array2[l] / array3[l]) : 0.0);
			double num6 = ((num3 > 0.0 && num5 > 0.0) ? (20.0 * Math.Log10(num5 / num3)) : double.NegativeInfinity);
			array4[l] = ((num6 >= -40.0) ? ContributionColor((float)((num6 + 40.0) / 40.0)) : new Color(0.48f, 0.5f, 0.52f, 1f));
		}
		displayMesh.colors = array4;
	}

	private static Color ContributionColor(float fraction)
	{
		float num = fraction;
		if (num < 0f)
		{
			num = 0f;
		}
		else if (num > 1f)
		{
			num = 1f;
		}
		float level = Mathf.Round(num * 10f);
		float value = 1f - level / 10f;
		return Color.HSVToRGB((value + value) / 3f, 0.9f, 0.95f, hdr: true);
	}

	private void RefreshModeUI()
	{
		bool flag = displayMode == RadarDisplayMode.Propagation || displayMode == RadarDisplayMode.Wavefronts;
		if (modeButton != null)
		{
			AeroReadoutText(modeButton, "radar.mode.current", (displayMode == RadarDisplayMode.Atlas) ? RadarScenarioText.Get("map.atlas.title") : ((displayMode != RadarDisplayMode.Wavefronts) ? Localization.T(flag ? "radar.mode.propagation" : "radar.mode.simple") : Localization.T("radar.mode.wavefronts")));
		}
		if (atlasModeButton != null)
		{
			MenuTheme.LabelOf(atlasModeButton).text = RadarScenarioText.Get("map.atlas.title");
			MenuTheme.SetSelectedVisual(atlasModeButton, displayMode == RadarDisplayMode.Atlas);
		}
		if (propagationControls != null)
		{
			propagationControls.gameObject.SetActive(flag);
		}
		if (propagationOverlay != null)
		{
			propagationOverlay.gameObject.SetActive(flag);
		}
		if (simpleModeButton != null)
		{
			MenuTheme.SetSelectedVisual(simpleModeButton, displayMode == RadarDisplayMode.SimpleBench);
		}
		if (propagationModeButton != null)
		{
			MenuTheme.SetSelectedVisual(propagationModeButton, displayMode == RadarDisplayMode.Propagation);
		}
		if (wavefrontModeButton != null)
		{
			MenuTheme.SetSelectedVisual(wavefrontModeButton, displayMode == RadarDisplayMode.Wavefronts);
		}
		if (waveModeHint != null)
		{
			waveModeHint.gameObject.SetActive(displayMode == RadarDisplayMode.Wavefronts);
		}
		if (heatButton != null)
		{
			MenuTheme.SetSelectedVisual(heatButton, heatmap);
		}
		if (rayButton != null)
		{
			rayButton.interactable = displayMode != RadarDisplayMode.Wavefronts;
			MenuTheme.SetSelectedVisual(rayButton, displayMode != RadarDisplayMode.Wavefronts && (propagation == null || propagation.ShowRays));
		}
		if (waveButton != null)
		{
			MenuTheme.SetSelectedVisual(waveButton, propagation == null || propagation.ShowWavefronts);
		}
		foreach (KeyValuePair<int, Button> item in densityButtons)
		{
			MenuTheme.SetSelectedVisual(item.Value, item.Key == pathDensity);
		}
		if (modeDisclaimer != null)
		{
			modeDisclaimer.Set(flag ? "radar.propagation.limitations" : "radar.disclaimer.bench", Array.Empty<object>());
		}
		if (flag && resultReadout != null)
		{
			if (isReference || importScaleConfirmed)
			{
				if (radarResult == null)
				{
					resultReadout.Set("radar.solve.wait", Array.Empty<object>());
					counterReadout.Set("radar.input.empty", Array.Empty<object>());
					propagationStatus.Set("radar.input.empty", Array.Empty<object>());
					if (detailReadout != null)
					{
						detailReadout.Set("radar.input.empty", Array.Empty<object>());
					}
					if (waveReadout != null)
					{
						waveReadout.Set("radar.input.empty", Array.Empty<object>());
					}
				}
				else if (radarResult.Refused)
				{
					resultReadout.Set("radar.solve.refused", Array.Empty<object>());
					counterReadout.Set("radar.input.empty", Array.Empty<object>());
					propagationStatus.Set("radar.input.empty", Array.Empty<object>());
					if (detailReadout != null)
					{
						detailReadout.Set("radar.input.empty", Array.Empty<object>());
					}
					if (waveReadout != null)
					{
						waveReadout.Set("radar.input.empty", Array.Empty<object>());
					}
				}
				else
				{
					int rx = ((state.Polarization == RadarPolarization.VV || state.Polarization == RadarPolarization.HV) ? 1 : 0);
					int tx = ((state.Polarization == RadarPolarization.VV || state.Polarization == RadarPolarization.VH) ? 1 : 0);
					double sigmaDbSquareMeters = radarResult.SigmaDbSquareMeters[rx, tx];
					resultReadout.Set("radar.solve.result", Number(sigmaDbSquareMeters), (sigmaDbSquareMeters > 0.0) ? (10.0 * Math.Log10(sigmaDbSquareMeters)).ToString("F1", CultureInfo.InvariantCulture) : "−∞");
					counterReadout.Set("radar.solve.dense_counters", (long)radarResult.GridSide * radarResult.GridSide, Number(radarResult.EffectiveRaysPerWavelength), (propagation != null) ? propagation.DisplayedPathCount : 1, radarResult.Paths.Length);
					detailReadout.Set("radar.solve.detail", radarResult.Counters.SurfaceHits, Number(radarResult.Counters.GrazingFraction * 100.0), Number(radarResult.SurfaceOffsetOverWavelength), Number(radarResult.ElapsedMilliseconds), radarResult.Counters.RayCapApplied ? "*" : "—");
					if (waveReadout != null && propagation != null)
					{
						waveReadout.Set("radar.wave.spacing", propagation.EffectiveWavefrontPeriodStride);
					}
				}
			}
			else
			{
				resultReadout.Set("radar.import.units_required", Array.Empty<object>());
				counterReadout.Set("radar.input.empty", Array.Empty<object>());
				propagationStatus.Set("radar.input.empty", Array.Empty<object>());
				if (detailReadout != null)
				{
					detailReadout.Set("radar.input.empty", Array.Empty<object>());
				}
				if (waveReadout != null)
				{
					waveReadout.Set("radar.input.empty", Array.Empty<object>());
				}
			}
		}
		if (propagationStatus != null)
		{
			if (solvePending || solveTask != null)
			{
				propagationStatus.Set("radar.solve.running", Array.Empty<object>());
			}
			else if (radarResult == null)
			{
				propagationStatus.Set("radar.solve.wait", Array.Empty<object>());
			}
			else if (radarResult.Refused)
			{
				propagationStatus.Set("radar.solve.too_small", Array.Empty<object>());
			}
			else if (radarResult.ResolutionBelowOne)
			{
				propagationStatus.Set("radar.solve.low_density", Array.Empty<object>());
			}
			else
			{
				double num = currentModel.LongestSide / state.WavelengthM;
				string key = ((20.0 > num) ? "radar.solve.boundary" : "radar.pulse.caption");
				propagationStatus.Set(key, Number(899377374.0 / (currentModel.BoundingSphereRadius * 4.0)));
			}
		}
		bool flag2 = statusKey != null && statusKey.StartsWith("radar.error.", StringComparison.Ordinal);
		if (statusReadout != null && LastErrorKey == null && !flag2)
		{
			statusReadout.Set((solvePending || solveTask != null) ? "radar.solve.running" : "radar.propagation.method", Array.Empty<object>());
		}
		if (flag2 || LastErrorKey != null)
		{
			propagationStatus.Set(statusKey, statusArgs);
		}
		if (scaleReadout != null)
		{
			scaleReadout.Set("radar.heat.scale", Array.Empty<object>());
		}
		RefreshPatternUI();
	}

	private static void AeroReadoutText(Button button, string key, params object[] args)
	{
		TextMeshProUGUI textMeshProUGUI = MenuTheme.LabelOf(button);
		AeroReadout aeroReadout = textMeshProUGUI.GetComponent<AeroReadout>();
		if (aeroReadout == null)
		{
			aeroReadout = AeroReadout.Attach(textMeshProUGUI, key);
		}
		aeroReadout.Set(key, args);
	}

	private void PolIndices(out int rx, out int tx)
	{
		tx = ((state.Polarization == RadarPolarization.VV || state.Polarization == RadarPolarization.VH) ? 1 : 0);
		rx = ((state.Polarization == RadarPolarization.VV || state.Polarization == RadarPolarization.HV) ? 1 : 0);
	}

	private static Vector3 V(double3 d)
	{
		return new Vector3((float)d.x, (float)d.y, (float)d.z);
	}

	private void CancelPropagation()
	{
		modesRunning = false;
		solveCancel?.Cancel();
		patternGeneration++;
		patternCancel?.Cancel();
		patternRequested = false;
	}

	private void BuildScenarioEntry(Transform parent)
	{
		Button button = Button(parent, "ModeScenario", "radar.input.empty", 44f, OpenScenario, 18f);
		TMP_Text label = button.GetComponentInChildren<TMP_Text>();
		LocalizedLabel[] componentsInChildren = button.GetComponentsInChildren<LocalizedLabel>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		RadarScenarioEntryCaption radarScenarioEntryCaption = button.gameObject.AddComponent<RadarScenarioEntryCaption>();
		radarScenarioEntryCaption.Label = label;
		radarScenarioEntryCaption.Refresh();
	}

	public void OpenScenario()
	{
		if (ScenarioOpen)
		{
			return;
		}
		solveRevision++;
		solveCancel?.Cancel();
		patternGeneration++;
		patternCancel?.Cancel();
		solvePending = false;
		patternAuto = false;
		patternRequested = false;
		propagationStatus?.Set("radar.solve.cancelled", Array.Empty<object>());
		propagationWasEnabled = propagation != null && propagation.enabled;
		if (propagation != null)
		{
			propagation.enabled = false;
		}
		radarDragging = false;
		if (scenarioController == null)
		{
			GameObject gameObject = new GameObject("RadarScenario");
			gameObject.transform.SetParent(base.transform, worldPositionStays: false);
			scenarioController = gameObject.AddComponent<RadarScenarioController>();
			scenarioController.Closed = CloseScenario;
		}
		canvas.gameObject.SetActive(false);
		benchCamera.enabled = false;
		orbit.enabled = false;
		scenarioController.gameObject.SetActive(true);
		scenarioController.ShowEditor();
	}

	public void CloseScenario()
	{
		if (scenarioController != null)
		{
			scenarioController.gameObject.SetActive(false);
			canvas.gameObject.SetActive(true);
			benchCamera.enabled = true;
			orbit.enabled = true;
			if (propagation != null)
			{
				propagation.enabled = propagationWasEnabled;
			}
		}
		RefreshUI();
	}
}
