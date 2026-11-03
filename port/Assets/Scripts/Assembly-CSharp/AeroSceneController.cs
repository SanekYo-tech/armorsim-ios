using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class AeroSceneController : MonoBehaviour
{
	private sealed class Work
	{
		public int Revision;

		public AeroComparison Comparison;

		public AeroRaycaster Raycaster;
	}

	private AeroMeshData data;

	private AeroComparison current;

	private Task<AeroMeshData> import;

	private Task<Work> calculation;

	private Mesh surface;

	private Material material;

	private Material gridMaterial;

	private Transform model;

	private CancellationTokenSource cancellation;

	private int calculationRevision;

	private AeroReadout yawCaption;

	private AeroReadout pitchCaption;

	private string currentName;

	private readonly Dictionary<int, Button> gridButtons;

	private readonly Dictionary<int, Button> densityButtons;

	private AeroReadout densityCaption;

	private int lineDensity;

	private bool wakeVortices;

	private AeroStreamlines paths;

	private AeroOrbit orbit;

	private RectTransform canvas;

	private RectTransform filePanel;

	private AeroReadout status;

	private AeroReadout ratio;

	private AeroReadout stats;

	private AeroReadout source;

	private AeroReadout modelName;

	private Slider yaw;

	private Slider pitch;

	private Button flip;

	private int revision;

	private int resolution;

	private float due;

	private float nextAccessCheck;

	private bool showPaths;

	private bool showClouds;

	private string pendingName;

	public bool HasResult => current != null;

	public string LastErrorKey { get; private set; }

	public AeroComparison Result => current;

	public int SurfaceContacts
	{
		get
		{
			if (paths == null)
			{
				return 0;
			}
			return paths.SurfaceContacts;
		}
	}

	public bool CloudsShown => paths != null && paths.CloudsVisible;

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

	private void Start()
	{
		if (!CheckAccess())
		{
			return;
		}
		showClouds = PlayerPrefs.GetInt("armorsim.aero.clouds", 0) == 1;
		try
		{
			BuildView();
			BuildUI();
			MusicPlayer.EnsureIn(null, MusicPolicy.GameProgram());
			Directory.CreateDirectory(ModelsFolder);
		}
		catch (Exception ex)
		{
			Error(new AeroException("aero.error.io", new object[1] { ex.Message }));
		}
		try
		{
			ShowSphere();
		}
		catch (Exception ex2)
		{
			Debug.LogException(ex2);
			if (status != null)
			{
				Error(ex2);
			}
		}
	}

	private void BuildView()
	{
		Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
		if (camera == null)
		{
			camera = new GameObject("Aero Camera", typeof(Camera)).GetComponent<Camera>();
		}
		Behaviour[] behaviours = UnityEngine.Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None);
		bool flag = false;
		for (int i = 0; i < behaviours.Length; i++)
		{
			if (behaviours[i].isActiveAndEnabled)
			{
				flag = true;
				break;
			}
		}
		if (!flag && camera.GetComponent<AudioListener>() == null)
		{
			camera.gameObject.AddComponent<AudioListener>().enabled = true;
		}
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = ResultPalette.Background;
		camera.rect = new Rect(0f, 0f, 0.68f, 1f);
		camera.nearClipPlane = 0.025f;
		camera.farClipPlane = 100f;
		orbit = camera.gameObject.AddComponent<AeroOrbit>();
		model = new GameObject("ImportedModel", typeof(MeshFilter), typeof(MeshRenderer)).transform;
		model.SetParent(base.transform, worldPositionStays: false);
		material = AeroDisplay.Material();
		model.GetComponent<MeshRenderer>().sharedMaterial = material;
		GameObject gameObject = new GameObject("Qualitative paths");
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		paths = gameObject.AddComponent<AeroStreamlines>();
		paths.enabled = false;
		paths.SetDensity(lineDensity);
		paths.SetWakeVortices(wakeVortices);
		ApplyFlowVisibility();
		GameObject gameObject2 = new GameObject("Reference grid", typeof(MeshFilter), typeof(MeshRenderer));
		gameObject2.transform.SetParent(base.transform, worldPositionStays: false);
		List<Vector3> list = new List<Vector3>();
		for (int j = -3; j <= 3; j++)
		{
			list.Add(new Vector3(j, -2.15f, -3f));
			list.Add(new Vector3(j, -2.15f, 4f));
			list.Add(new Vector3(-3f, -2.15f, j));
			list.Add(new Vector3(3f, -2.15f, j));
		}
		list.Add(new Vector3(-2.8f, -2.1f, -2.5f));
		list.Add(new Vector3(-2.8f, -2.1f, 2.5f));
		list.Add(new Vector3(-2.8f, -2.1f, 2.5f));
		list.Add(new Vector3(-3f, -2.1f, 2.1f));
		list.Add(new Vector3(-2.8f, -2.1f, 2.5f));
		list.Add(new Vector3(-2.6f, -2.1f, 2.1f));
		int[] array = new int[list.Count];
		Color[] array2 = new Color[list.Count];
		for (int k = 0; k < list.Count; k++)
		{
			array[k] = k;
			array2[k] = ResultPalette.Miss;
		}
		Mesh mesh = new Mesh();
		mesh.name = "Aero reference grid";
		mesh.SetVertices(list);
		mesh.colors = array2;
		mesh.SetIndices(array, MeshTopology.LineStrip, 0);
		gameObject2.GetComponent<MeshFilter>().sharedMesh = mesh;
		gridMaterial = AeroDisplay.Material();
		gameObject2.GetComponent<MeshRenderer>().sharedMaterial = gridMaterial;
	}

	private void BuildUI()
	{
		if (EventSystem.current == null)
		{
			new GameObject("Aero EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(base.transform, worldPositionStays: false);
		}
		canvas = MenuTheme.MakeRect(base.transform, "AeroCanvas");
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
		Label(content, "Title", "aero.title", 32f, 40f);
		RectTransform rect2 = Row(content, 40f);
		MenuTheme.MakeButton(rect2, "Import", "aero.import", 40f, PickFile, 18f);
		MenuTheme.MakeButton(rect2, "Sphere", "aero.sphere", 40f, ShowSphere, 18f);
		modelName = AeroReadout.Attach(Label(content, "ModelName", "aero.sphere", 18f, 28f), "aero.sphere");
		source = AeroReadout.Attach(Label(content, "SourceSize", "aero.source", 17f, 54f), "aero.source");
		status = AeroReadout.Attach(Label(content, "Status", "aero.ready", 18f, 64f), "aero.ready");
		ratio = AeroReadout.Attach(Label(content, "RelativeDrag", "aero.pending", 23f, 56f), "aero.pending");
		stats = AeroReadout.Attach(Label(content, "Measurements", "aero.pending", 18f, 110f), "aero.pending");
		yaw = Angle(content, "Yaw", "aero.yaw", -180f, 180f);
		pitch = Angle(content, "Pitch", "aero.pitch", -90f, 90f);
		Label(content, "Rays", "aero.rays", 18f, 24f);
		RectTransform rect3 = Row(content, 34f);
		foreach (int n in new int[3] { 128, 256, 512 })
		{
			Button button = MenuTheme.MakeButton(rect3, "Grid" + n, "aero.grid." + n, 34f, delegate
			{
				resolution = n;
				RefreshGridSelection();
				Changed();
			}, 17f);
			MenuTheme.LabelOf(button).characterSpacing = 0f;
			gridButtons.Add(n, button);
		}
		RefreshGridSelection();
		Label(content, "FlowSettings", "aero.flow.settings", 18f, 28f);
		Button cloudButton = MenuTheme.MakeButton(content, "CloudDisplay", showClouds ? "aero.clouds.on" : "aero.clouds.off", 36f, null, 17f);
		MenuTheme.SetSelectedVisual(cloudButton, showClouds);
		cloudButton.onClick.AddListener(delegate
		{
			showClouds = !showClouds;
			PlayerPrefs.SetInt("armorsim.aero.clouds", showClouds ? 1 : 0);
			PlayerPrefs.Save();
			LocalizedLabel component = cloudButton.GetComponent<LocalizedLabel>();
			component.SetKey(showClouds ? "aero.clouds.on" : "aero.clouds.off");
			MenuTheme.SetSelectedVisual(cloudButton, showClouds);
			ApplyFlowVisibility();
		});
		densityCaption = AeroReadout.Attach(Label(content, "DensityLabel", "aero.density", 18f, 26f), "aero.density");
		RectTransform rect4 = Row(content, 34f);
		foreach (int n2 in new int[4] { 80, 320, 1280, 2560 })
		{
			Button button2 = MenuTheme.MakeButton(rect4, "Density" + n2, "aero.density." + n2, 34f, delegate
			{
				lineDensity = n2;
				if (paths != null)
				{
					paths.SetDensity(n2);
				}
				RefreshDensitySelection();
			}, 17f);
			MenuTheme.LabelOf(button2).characterSpacing = 0f;
			densityButtons.Add(n2, button2);
		}
		RefreshDensitySelection();
		RectTransform rect5 = Row(content, 36f);
		Button streamButton = MenuTheme.MakeButton(rect5, "Paths", "aero.paths.on", 36f, null, 17f);
		MenuTheme.SetSelectedVisual(streamButton, showPaths);
		streamButton.onClick.AddListener(delegate
		{
			showPaths = !showPaths;
			LocalizedLabel component = streamButton.GetComponent<LocalizedLabel>();
			component.SetKey(showPaths ? "aero.paths.on" : "aero.paths.off");
			MenuTheme.SetSelectedVisual(streamButton, showPaths);
			ApplyFlowVisibility();
		});
		MenuTheme.MakeButton(rect5, "Frame", "aero.frame", 36f, delegate
		{
			orbit.Frame();
		}, 17f);
		Button wakeButton = MenuTheme.MakeButton(content, "WakeVortices", "aero.wake.on", 36f, null, 17f);
		MenuTheme.SetSelectedVisual(wakeButton, wakeVortices);
		wakeButton.onClick.AddListener(delegate
		{
			wakeVortices = !wakeVortices;
			if (paths != null)
			{
				paths.SetWakeVortices(wakeVortices);
			}
			LocalizedLabel component = wakeButton.GetComponent<LocalizedLabel>();
			component.SetKey(wakeVortices ? "aero.wake.on" : "aero.wake.off");
			MenuTheme.SetSelectedVisual(wakeButton, wakeVortices);
		});
		flip = MenuTheme.MakeButton(content, "FlipNormals", "aero.flip", 40f, FlipNormals, 18f);
		flip.gameObject.SetActive(false);
		MenuTheme.MakeButton(content, "Back", "aero.menu", 38f, AppFlow.OpenMenu, 18f);
		TextMeshProUGUI textMeshProUGUI = Label(canvas, "StreamlineCaveat", "aero.clouds.disclaimer", 17f, 60f);
		textMeshProUGUI.color = ResultPalette.Miss;
		MenuTheme.Place(textMeshProUGUI.rectTransform, Vector2.zero, Vector2.zero, new Vector2(28f, 18f), new Vector2(1220f, 58f));
		TextMeshProUGUI textMeshProUGUI2 = Label(canvas, "CameraHelp", "aero.camera", 18f, 46f);
		textMeshProUGUI2.color = ResultPalette.Miss;
		MenuTheme.Place(textMeshProUGUI2.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -22f), new Vector2(1220f, 46f));
		RectTransform rect6 = MenuTheme.MakeRect(canvas, "CpLegend");
		MenuTheme.Place(rect6, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -88f), new Vector2(350f, 80f));
		TextMeshProUGUI textMeshProUGUI3 = Label(rect6, "CpTitle", "aero.cp", 18f, 24f);
		textMeshProUGUI3.color = ResultPalette.Miss;
		MenuTheme.Place(textMeshProUGUI3.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(320f, 24f));
		for (int j = 0; j < 40; j++)
		{
			RectTransform rect7 = MenuTheme.MakeRect(rect6, "CpColor" + j);
			MenuTheme.Place(rect7, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2((float)j * 8f, -28f), new Vector2(8f, 16f));
			MenuTheme.AddImage(rect7, AeroDisplay.PressureColor(((float)j + (float)j) / 39f));
		}
		TextMeshProUGUI textMeshProUGUI4 = Label(rect6, "CpTicks", "aero.cp.ticks", 17f, 24f);
		textMeshProUGUI4.color = ResultPalette.Miss;
		MenuTheme.Place(textMeshProUGUI4.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -50f), new Vector2(320f, 24f));
	}

	private static void Height(RectTransform rect, float height)
	{
		LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
		layoutElement.preferredHeight = height;
		layoutElement.minHeight = height;
	}

	private static TextMeshProUGUI Label(Transform parent, string name, string key, float size, float height)
	{
		TextMeshProUGUI textMeshProUGUI = MenuTheme.MakeLabel(parent, name, key, size, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.TopLeft);
		textMeshProUGUI.overflowMode = TextOverflowModes.Ellipsis;
		Height(textMeshProUGUI.rectTransform, height);
		return textMeshProUGUI;
	}

	private static AeroReadout Readout(Transform parent, string name, string key, float size, float height)
	{
		return AeroReadout.Attach(Label(parent, name, key, size, height), key);
	}

	private static RectTransform Row(Transform parent, float height)
	{
		RectTransform rect = MenuTheme.MakeRow(parent, "Actions", 8f, forceExpandWidth: true);
		Height(rect, height);
		return rect;
	}

	private Slider Angle(Transform parent, string name, string key, float min, float max)
	{
		AeroReadout caption = AeroReadout.Attach(Label(parent, name + "Label", key, 18f, 25f), key);
		caption.Set(key, new object[1] { 0f });
		if (name == "Yaw")
		{
			yawCaption = caption;
		}
		else
		{
			pitchCaption = caption;
		}
		RectTransform rect = MenuTheme.MakeRect(parent, name);
		Height(rect, 28f);
		Slider slider = rect.gameObject.AddComponent<Slider>();
		slider.minValue = min;
		slider.maxValue = max;
		RectTransform rect2 = MenuTheme.MakeRect(rect, "Track");
		MenuTheme.Stretch(rect2, new Vector2(8f, 10f), new Vector2(-8f, -10f));
		Image image = MenuTheme.AddImage(rect2, MenuTheme.SliderTrack);
		image.raycastTarget = true;
		RectTransform rect3 = MenuTheme.MakeRect(rect, "HandleArea");
		MenuTheme.Stretch(rect3, new Vector2(8f, 0f), new Vector2(-8f, 0f));
		RectTransform rect4 = MenuTheme.MakeRect(rect3, "Handle");
		rect4.sizeDelta = new Vector2(16f, 0f);
		slider.targetGraphic = MenuTheme.AddImage(rect4, MenuTheme.TextPrimary);
		slider.handleRect = rect4;
		slider.value = 0f;
		slider.onValueChanged.AddListener(delegate(float v)
		{
			caption.Set(key, new object[1] { v.ToString("F0") });
			Changed();
		});
		return slider;
	}

	private void RefreshGridSelection()
	{
		foreach (KeyValuePair<int, Button> gridButton in gridButtons)
		{
			MenuTheme.SetSelectedVisual(gridButton.Value, gridButton.Key == resolution);
		}
	}

	private void RefreshDensitySelection()
	{
		densityCaption.Set("aero.density", new object[1] { lineDensity });
		foreach (KeyValuePair<int, Button> densityButton in densityButtons)
		{
			MenuTheme.SetSelectedVisual(densityButton.Value, densityButton.Key == lineDensity);
		}
	}

	private void SelectDensity(int count)
	{
		lineDensity = count;
		if (paths != null)
		{
			paths.SetDensity(count);
		}
		RefreshDensitySelection();
	}

	private void ApplyFlowVisibility()
	{
		if (paths == null)
		{
			return;
		}
		paths.SetLinesVisible(showPaths);
		paths.SetCloudsVisible(showClouds);
		paths.gameObject.SetActive(current != null && (showPaths || showClouds));
	}

	public void ShowSphere()
	{
		if (import == null)
		{
			AeroMeshData aeroMeshData = AeroPrimitives.Sphere();
			aeroMeshData.OriginalSize = Vector3.one * 4f;
			SetModel(aeroMeshData, Localization.T("aero.sphere"), true);
		}
	}

	public void LoadPath(string path)
	{
		if (import == null)
		{
			pendingName = Path.GetFileName(path);
			Invalidate();
			status.Set("aero.loading", Array.Empty<object>());
			import = Task.Run(delegate
			{
				return AeroImporter.Load(path);
			});
		}
	}

	private void SetModel(AeroMeshData meshData, string name, bool resetAngles = true)
	{
		data = meshData;
		currentName = name;
		source.Set("aero.filename", new object[1] { name });
		if (surface != null)
		{
			UnityEngine.Object.Destroy(surface);
		}
		surface = AeroDisplay.Mesh(data);
		model.GetComponent<MeshFilter>().sharedMesh = surface;
		stats.Set("aero.source", new object[3]
		{
			meshData.OriginalSize.x.ToString("G5"),
			meshData.OriginalSize.y.ToString("G5"),
			meshData.OriginalSize.z.ToString("G5")
		});
		if (resetAngles)
		{
			yaw.value = 0f;
			pitch.value = 0f;
			yawCaption.Set("aero.yaw", new object[1] { 0f });
			pitchCaption.Set("aero.pitch", new object[1] { 0f });
			model.rotation = Quaternion.identity;
		}
		Changed();
	}

	public void FlipNormals()
	{
		if (data != null)
		{
			SetModel(data.Flipped(), currentName, false);
		}
	}

	private void Invalidate()
	{
		revision++;
		current = null;
		LastErrorKey = null;
		if (cancellation != null)
		{
			cancellation.Cancel();
		}
		if (material != null)
		{
			material.SetFloat("_ShowPressure", 0f);
		}
		if (paths != null)
		{
			paths.Clear();
			ApplyFlowVisibility();
		}
		ratio.Set("aero.pending", Array.Empty<object>());
		stats.Set("aero.pending", Array.Empty<object>());
		if (flip != null)
		{
			flip.gameObject.SetActive(false);
		}
	}

	private void Changed()
	{
		Invalidate();
		due = Time.unscaledTime + 0.2f;
		if (model != null && yaw != null && pitch != null)
		{
			model.rotation = Quaternion.Euler(pitch.value, yaw.value, 0f);
		}
		if (status != null)
		{
			status.GetComponent<Image>().color = MenuTheme.TextPrimary;
			status.Set("aero.calculating", new object[2] { resolution, resolution });
		}
	}

	private bool CheckAccess()
	{
		AeroFacts facts = new AeroFacts
		{
			implemented = true,
			ownership = Entitlements.State(ContentPack.Aero),
			gateOpen = true,
			freeAccess = AeroFreeAccess.IsActiveNow
		};
		AeroAccessState state = AeroRoute.State(facts);
		switch (state)
		{
		case AeroAccessState.Open:
			return true;
		case AeroAccessState.NotImplemented:
		case AeroAccessState.NotOwned:
		case AeroAccessState.Unverified:
		case AeroAccessState.GateClosed:
			break;
		default:
			Debug.LogError(string.Format("[AeroRoute] Unknown access state {0}; entry refused.", (int)state));
			break;
		}
		Invalidate();
		due = float.PositiveInfinity;
		enabled = false;
		Debug.Log("[AeroSim] Access unavailable or free period ended; returning to menu.");
		AppFlow.OpenMenu();
		return false;
	}

	private void Update()
	{
		if (Time.unscaledTime >= nextAccessCheck)
		{
			nextAccessCheck = Time.unscaledTime + 0.25f;
			if (!CheckAccess())
			{
				return;
			}
		}
		if (import != null && import.IsCompleted)
		{
			Task<AeroMeshData> task = import;
			import = null;
			if (!task.IsFaulted)
			{
				SetModel(task.Result, pendingName, true);
			}
			else
			{
				Error(task.Exception.InnerException);
			}
		}
		if (calculation != null && calculation.IsCompleted)
		{
			Task<Work> task2 = calculation;
			calculation = null;
			if (task2.IsFaulted)
			{
				if (calculationRevision == revision)
				{
					Error(task2.Exception.InnerException);
				}
			}
			else if (!task2.IsCanceled)
			{
				Work work = task2.Result;
				if (work.Revision == revision)
				{
					current = work.Comparison;
					AeroDisplay.Paint(surface, current.Model.Cp);
					material.SetFloat("_ShowPressure", 1f);
					ratio.Set("aero.ratio", new object[1] { current.Ratio.ToString("F3") });
					stats.Set("aero.stats", new object[7]
					{
						current.Model.MidshipArea.ToString("F4"),
						(current.Model.ShadowedFraction * 100.0).ToString("F1"),
						data.Triangles.Length / 2,
						data.DegenerateTriangles,
						current.Milliseconds.ToString("F1"),
						resolution,
						resolution
					});
					status.GetComponent<Image>().color = MenuTheme.TextPrimary;
					status.Set("aero.ready", Array.Empty<object>());
					paths.SetDensity(lineDensity);
					paths.SetWakeVortices(wakeVortices);
					paths.Configure(work.Raycaster);
					ApplyFlowVisibility();
				}
			}
		}
		if (import == null && calculation == null && data != null && Time.unscaledTime >= due)
		{
			due = float.PositiveInfinity;
			AeroMeshData sourceData = data;
			int version = revision;
			int cells = resolution;
			Quaternion rotation = model.rotation;
			calculationRevision = revision;
			if (cancellation != null)
			{
				cancellation.Dispose();
			}
			cancellation = new CancellationTokenSource();
			CancellationToken token = cancellation.Token;
			calculation = Task.Run(delegate
			{
				AeroMeshData rotated = sourceData.Rotated(rotation);
				return new Work
				{
					Revision = version,
					Comparison = AeroSolver.CompareToSphere(rotated, Vector3.forward, cells, token),
					Raycaster = new AeroRaycaster(rotated)
				};
			});
		}
	}

	private void Error(Exception error)
	{
		Invalidate();
		due = float.PositiveInfinity;
		AeroException ex = error as AeroException;
		LastErrorKey = ((ex != null) ? ex.Key : "aero.error.unexpected");
		if (ex != null)
		{
			status.Set(LastErrorKey, ex.Args);
		}
		else
		{
			status.Set(LastErrorKey, new object[1] { error.Message });
		}
		status.GetComponent<Image>().color = MenuTheme.Failure;
		if (flip != null)
		{
			flip.gameObject.SetActive(LastErrorKey == "aero.error.normals");
		}
		Debug.LogWarning("[AeroSim] " + error.Message);
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
			MenuTheme.MakeScrollList(filePanel, "FilesScroll", out content);
			MenuTheme.Stretch(content.transform as RectTransform, new Vector2(24f, 24f), new Vector2(-24f, -24f));
			MenuTheme.MakeButton(content, "CloseFiles", "aero.close", 40f, delegate
			{
				if (filePanel != null)
				{
					UnityEngine.Object.Destroy(filePanel.gameObject);
				}
			});
			AeroReadout aeroReadout = AeroReadout.Attach(Label(content, "Folder", "aero.folder", 18f, 76f), "aero.folder");
			aeroReadout.Set("aero.folder", new object[1] { ModelsFolder });
			int num = 0;
			foreach (string path in Directory.EnumerateFiles(ModelsFolder))
			{
				string extension = Path.GetExtension(path);
				if (extension.Equals(".obj", StringComparison.OrdinalIgnoreCase) || extension.Equals(".stl", StringComparison.OrdinalIgnoreCase))
				{
					Button button = MenuTheme.MakeButton(content, "ModelFile", "aero.filename", 42f, delegate
					{
						if (filePanel != null)
						{
							UnityEngine.Object.Destroy(filePanel.gameObject);
						}
						LoadPath(path);
					}, 18f);
					AeroReadout aeroReadout2 = AeroReadout.Attach(MenuTheme.LabelOf(button), "aero.filename");
					aeroReadout2.Set("aero.filename", new object[1] { Path.GetFileName(path) });
					num++;
				}
			}
			if (num == 0)
			{
				Label(content, "EmptyFolder", "aero.folder.empty", 20f, 80f);
			}
		}
		catch (Exception ex2)
		{
			Error(new AeroException("aero.error.io", new object[1] { ex2.Message }));
		}
	}

	private void OnDestroy()
	{
		if (cancellation != null)
		{
			cancellation.Cancel();
			cancellation.Dispose();
		}
		if (import != null)
		{
			import.ContinueWith(delegate(Task<AeroMeshData> t)
			{
				_ = t.Exception;
			}, TaskContinuationOptions.OnlyOnFaulted);
		}
		if (calculation != null)
		{
			calculation.ContinueWith(delegate(Task<Work> t)
			{
				_ = t.Exception;
			}, TaskContinuationOptions.OnlyOnFaulted);
		}
		if (surface != null)
		{
			UnityEngine.Object.Destroy(surface);
		}
		if (material != null)
		{
			UnityEngine.Object.Destroy(material);
		}
		if (gridMaterial != null)
		{
			UnityEngine.Object.Destroy(gridMaterial);
		}
		MeshFilter[] components = base.GetComponentsInChildren<MeshFilter>();
		foreach (MeshFilter meshFilter in components)
		{
			if (meshFilter.sharedMesh != null && meshFilter.sharedMesh != surface && meshFilter.gameObject.name == "Reference grid")
			{
				UnityEngine.Object.Destroy(meshFilter.sharedMesh);
			}
		}
	}
}
