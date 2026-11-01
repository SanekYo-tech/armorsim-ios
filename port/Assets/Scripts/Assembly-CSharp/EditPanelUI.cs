using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class EditPanelUI : MonoBehaviour
{
	private class SliderBinding
	{
		public Slider slider;

		public TMP_Text label;

		public string title;

		public string unit;

		public string format;

		public string titleKey;

		public string unitKey;

		public float min;

		public float max;

		public Func<ArmorPlate, float> getter;

		public Action<ArmorPlate, float> setter;

		public bool captured;

		public float grabSliderValue;

		public readonly Dictionary<ArmorPlate, float> grabValues = new Dictionary<ArmorPlate, float>();

		public TMP_InputField input;

		public float typedMin = float.NaN;

		public float typedMax = float.NaN;

		public readonly List<ArmorPlate> typedTargets = new List<ArmorPlate>();

		public Func<ArmorPlate, bool> appliesTo;

		public bool always;

		public RectTransform row;

		public float rowHeight;

		public Func<ArmorPlate, float, string> extraLabel;

		public Func<ArmorPlate, bool> hideFor;

		public Func<float, float> shown;

		public Func<float, float> unshown;
	}

	private class SliderPointerEvents : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
	{
		public Action onDown;

		public Action onUp;

		public void OnPointerDown(PointerEventData eventData)
		{
			onDown?.Invoke();
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			onUp?.Invoke();
		}
	}

	public PlateSelector selector;

	private const float PanelWidth = 220f;

	private const float SidePad = 15f;

	private const float TooltipMinH = 42f;

	private Canvas canvas;

	private GameObject panelRoot;

	private ArmorPlate current;

	private TMP_Text titleLabel;

	private TMP_Text tooltipLabel;

	private CatalogGrid materialGrid;

	private CatalogGrid objectGrid;

	private Button sectionButton;

	private Button crewOrgansButton;

	private Button crewHighlightButton;

	private TMP_Text crewReportLabel;

	private TMP_Text neraPackLabel;

	private string crewReportSignature;

	private readonly List<SliderBinding> bindings = new List<SliderBinding>();

	private float rowsTopY;

	private static float MaxTiltDeg => GameSettings.TiltLimitDeg;

	private static int NeraBondingCount => Enum.GetValues(typeof(NeraBonding)).Length;

	private static int InterlayerMaterialCount => Enum.GetValues(typeof(InterlayerMaterial)).Length;

	public static string EraCrackingNote => Localization.T("sandbox.era_cracking");

	private void Start()
	{
		if (selector == null)
		{
			selector = UnityEngine.Object.FindFirstObjectByType<PlateSelector>();
		}
		if (selector == null)
		{
			Debug.LogWarning("EditPanelUI: не найден PlateSelector в сцене!");
			return;
		}
		BuildPanel();
		RefreshPanel();
		Localization.OnLanguageChanged += Rebuild;
		GameSettings.OnLimitsChanged += Rebuild;
	}

	private void OnDestroy()
	{
		Localization.OnLanguageChanged -= Rebuild;
		GameSettings.OnLimitsChanged -= Rebuild;
	}

	private void Rebuild()
	{
		if (panelRoot != null)
		{
			SidePanelScroll.DestroyPanel(panelRoot);
		}
		bindings.Clear();
		BuildPanel();
		RefreshPanel();
	}

	private void Update()
	{
		if (selector == null)
		{
			return;
		}
		ArmorPlate selected = selector.SelectedPlate;
		if (selected != current)
		{
			current = selected;
			RefreshPanel();
		}
		RefreshCrewControls();
		if (RefreshCrewReport())
		{
			RelayoutRows(current != null, false);
		}
		bool ctrl = Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl);
		bool shift = Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift);
		if (ctrl && shift && !NumberField.TypingNow && Input.GetKeyDown(ControlBindings.Key(ControlAction.DuplicateSelection)))
		{
			DuplicateSelection();
		}
	}

	private void BuildPanel()
	{
		canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
		if (canvas == null)
		{
			GameObject canvasObject = new GameObject("Canvas_Auto", typeof(Canvas));
			canvas = canvasObject.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
			canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
			canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			canvasScaler.matchWidthOrHeight = 0.5f;
			canvasObject.AddComponent<GraphicRaycaster>();
		}
		Catalog.AssertComplete();
		panelRoot = MakePanel("EditPanel", new Vector2(-20f, -20f), new Vector2(560f, 220f), Vector2.one);
		float y = -15f;
		titleLabel = MakeLabel(panelRoot, Localization.T("sandbox.no_selection"), new Vector2(0f, y), 16f, TextAlignmentOptions.Top, 200f, 40f);
		y -= 46f;
		MakeActionButton(panelRoot, Localization.T("sandbox.duplicate", ControlBindings.KeyName(ControlBindings.Key(ControlAction.DuplicateSelection))), new Vector2(0f, y), new Vector2(190f, 28f), new Color(0.2f, 0.4f, 0.3f, 1f), DuplicateSelection);
		y -= 38f;
		sectionButton = MakeActionButton(panelRoot, string.Empty, new Vector2(0f, y), new Vector2(190f, 28f), MenuTheme.SurfaceAlt, delegate
		{
			if (SectionView.Instance != null)
			{
				SectionView.Instance.Toggle();
			}
		});
		sectionButton.name = "CrewSectionToggle";
		y -= 34f;
		crewOrgansButton = MakeActionButton(panelRoot, string.Empty, new Vector2(0f, y), new Vector2(190f, 28f), MenuTheme.SurfaceAlt, delegate
		{
			GameSettings.SetCrewOrgans(!GameSettings.CrewOrgans);
		});
		crewOrgansButton.name = "CrewOrgansToggle";
		y -= 34f;
		crewHighlightButton = MakeActionButton(panelRoot, string.Empty, new Vector2(0f, y), new Vector2(190f, 28f), MenuTheme.SurfaceAlt, delegate
		{
			GameSettings.SetCrewDamageHighlight(!GameSettings.CrewDamageHighlight);
		});
		crewHighlightButton.name = "CrewDamageHighlightToggle";
		y -= 38f;
		RefreshCrewControls();
		objectGrid = CatalogGrid.Build(panelRoot, Catalog.ObjectMeta(), y, 220f, 15f, PickObjectKind, SetTooltip);
		y = objectGrid.EndY - 2f;
		materialGrid = CatalogGrid.Build(panelRoot, Catalog.MaterialMeta(), y, 220f, 15f, PickMaterial, SetTooltip);
		y = materialGrid.EndY - 2f;
		tooltipLabel = MakeLabel(panelRoot, string.Empty, new Vector2(0f, y), 10f, TextAlignmentOptions.TopLeft, 190f, 42f);
		tooltipLabel.color = new Color(0.75f, 0.78f, 0.82f, 1f);
		tooltipLabel.textWrappingMode = TextWrappingModes.Normal;
		rowsTopY = y;
		crewReportLabel = MakeLabel(panelRoot, string.Empty, new Vector2(0f, y), 12f, TextAlignmentOptions.TopLeft, 190f, 32f);
		crewReportLabel.name = "CrewOrganHitReport";
		crewReportLabel.textWrappingMode = TextWrappingModes.Normal;
		crewReportSignature = null;
		neraPackLabel = MakeLabel(panelRoot, string.Empty, new Vector2(0f, y), 12f, TextAlignmentOptions.TopLeft, 190f, 32f);
		neraPackLabel.name = "NeraPackReport";
		neraPackLabel.textWrappingMode = TextWrappingModes.Normal;

		AddSlider("sandbox.thickness", ref y, 0f, 300f, "unit.mm", "F0", true, p => p.thicknessMM, delegate(ArmorPlate p, float v) { p.SetThickness(v); }, p => p.IsModel || p.IsRod, null, true, 0.1f, 2000f);
		AddSlider("sandbox.angle_x", ref y, -MaxTiltDeg, MaxTiltDeg, "unit.deg", "F0", true, p => p.angleX, delegate(ArmorPlate p, float v) { p.SetAngleX(v); }, null, null, true);
		AddSlider("sandbox.angle_y", ref y, -MaxTiltDeg, MaxTiltDeg, "unit.deg", "F0", true, p => p.angleY, delegate(ArmorPlate p, float v) { p.SetAngleY(v); }, null, null, true);
		AddSlider("sandbox.width", ref y, 0.2f, 4f, "unit.m", "F2", false, p => p.widthM, delegate(ArmorPlate p, float v) { p.SetWidth(v); }, null, null, true, 0.02f, 20f);
		AddSlider("sandbox.height", ref y, 0.2f, 4f, "unit.m", "F2", false, p => p.heightM, delegate(ArmorPlate p, float v) { p.SetHeight(v); }, null, null, true, 0.02f, 20f);
		AddSlider("sandbox.model_scale", ref y, -2f, 2f, "unit.none", "G3", false, p => Mathf.Log10(Mathf.Max(0.001f, p.modelScale)), delegate(ArmorPlate p, float v)
		{
			if (p.IsModel)
			{
				p.SetModelScale(Mathf.Pow(10f, v));
			}
		}, p => p.IsModel, (p, v) => ModelSizeReadout(p, Mathf.Pow(10f, v)), false, -3f, 3f, null, v => Mathf.Pow(10f, v), s => Mathf.Log10(s));
		AddSlider("sandbox.rod_armor_d", ref y, 0f, 2000f, "unit.mm", "F0", true, p => p.thicknessMM, delegate(ArmorPlate p, float v)
		{
			if (p.IsRod)
			{
				p.SetThickness(v);
			}
		}, p => p.IsRod, null, true, 0.5f, 2000f);
		AddSlider("sandbox.rod_armor_len", ref y, 0.01f, 3f, "unit.m", "F2", false, p => p.widthM, delegate(ArmorPlate p, float v)
		{
			if (p.IsRod)
			{
				p.SetWidth(v);
			}
		}, p => p.IsRod, null, false, 0.01f, 20f);
		AddSlider("sandbox.edge_clamp", ref y, 0f, 1f, "unit.none", "F0", true, p => p.edgeClamp ? 1f : 0f, delegate(ArmorPlate p, float v) { p.edgeClamp = v > 0.5f; }, p => p.kind == ArmorPlate.ObjectKind.Plate || p.IsRod, (p, v) => Localization.T(v > 0.5f ? "sandbox.edge_clamp_on" : "sandbox.edge_clamp_off"), true);
		if (Catalog.IsKindAvailable(ArmorPlate.ObjectKind.Screen))
		{
			AddSlider("sandbox.screen_standoff", ref y, 100f, 1200f, "unit.mm", "F0", true, p => Mathf.Max(100f, p.ScreenStandoffMM(ShotDir())), delegate(ArmorPlate p, float v) { p.SetScreenStandoffMM(v, ShotDir()); }, p => p.IsScreen, (p, v) => ScreenStandoffReadout(v), true);
		}
		if (Catalog.IsKindAvailable(ArmorPlate.ObjectKind.Slat))
		{
			AddSlider("sandbox.slat_bar", ref y, 0.5f, 60f, "unit.mm", "F0", true, p => p.slatBarWidthMM, delegate(ArmorPlate p, float v) { p.slatBarWidthMM = v; }, p => p.IsSlat, (p, v) => SlatReadout(p), true);
			AddSlider("sandbox.slat_pitch", ref y, 1f, 400f, "unit.mm", "F0", true, p => p.slatPitchMM, delegate(ArmorPlate p, float v) { p.slatPitchMM = v; }, p => p.IsSlat, (p, v) => SlatReadout(p), true);
			AddSlider("sandbox.slat_dud", ref y, 0f, 1f, "unit.none", "F2", false, p => p.slatDudFraction, delegate(ArmorPlate p, float v) { p.slatDudFraction = v; }, p => p.IsSlat, (p, v) => Localization.T("sandbox.assumed_bare"), false);
		}
		AddSlider("sandbox.block_void", ref y, 0f, 0.9f, "unit.none", "F2", false, p => p.voidFraction, delegate(ArmorPlate p, float v) { p.voidFraction = v; }, p => p.IsEngineBlock, (p, v) => Localization.T("sandbox.block_effective", string.Format("{0:F0}", StandoffDefences.EffectiveThicknessMM(p.thicknessMM, v))), false);
		AddSlider("sandbox.era_plate_t", ref y, 0.5f, 25f, "unit.mm", "F1", false, p => p.eraPlateThicknessMM, delegate(ArmorPlate p, float v) { p.SetEraPlateThickness(v); }, p => p.IsEraPlate, (p, v) => Localization.T("sandbox.era_tp_note") + EraCrackingNote, false);
		AddSlider("sandbox.era_plate_v", ref y, 0f, 1200f, "unit.mps", "F0", true, p => p.eraPlateVelocityMS, delegate(ArmorPlate p, float v) { p.SetEraPlateVelocity(v); }, p => p.IsEraPlate, (p, v) => Localization.T(v <= 0.5f ? "sandbox.era_vp_inert" : "sandbox.era_vp_note"), true);
		AddSlider("sandbox.era_gap", ref y, 0f, 80f, "unit.mm", "F0", true, p => p.eraInterlayerGapMM, delegate(ArmorPlate p, float v) { p.SetEraInterlayerGap(v); }, p => p.IsEraPlate, (p, v) => Localization.T("sandbox.era_gap_note"), true);
		AddSlider("sandbox.era_extent", ref y, 0f, 400f, "unit.mm", "F0", true, p => p.eraCassetteExtentMM, delegate(ArmorPlate p, float v) { p.SetEraCassetteExtent(v); }, p => p.IsEraPlate, (p, v) => Localization.T(v > 80f ? "sandbox.era_w_sat" : "sandbox.era_w_note"), true);
		AddSlider("sandbox.nera_front", ref y, 0f, 12f, "unit.mm", "F1", false, p => p.neraFrontThicknessMM, delegate(ArmorPlate p, float v) { p.SetNeraFrontThickness(v); }, p => p.IsNeraPlate, (p, v) => NeraVelocityReadout(p), false);
		AddSlider("sandbox.nera_rear", ref y, 0f, 12f, "unit.mm", "F1", false, p => p.neraRearThicknessMM, delegate(ArmorPlate p, float v) { p.SetNeraRearThickness(v); }, p => p.IsNeraPlate, (p, v) => NeraVelocityReadout(p), false);
		AddSlider("sandbox.nera_elastomer", ref y, 0f, 40f, "unit.mm", "F0", true, p => p.neraInterlayerMM, delegate(ArmorPlate p, float v) { p.SetNeraInterlayer(v); }, p => p.IsNeraPlate, (p, v) => Localization.T("sandbox.nera_elast_note"), true);
		AddSlider("sandbox.nera_mount", ref y, 0f, 60f, "unit.deg", "F0", true, p => p.neraMountAngleDeg, delegate(ArmorPlate p, float v) { p.SetNeraMountAngle(v); }, p => p.IsNeraPlate, (p, v) => Localization.T("sandbox.nera_mount_note", string.Format("{0:F0}", v)), true);
		AddSlider("sandbox.nera_mat_front", ref y, 0f, NeraArmor.AllowedPlateMaterials.Length - 1, "unit.none", "F0", true, p => NeraMaterialIndex(p.neraFrontMaterial), delegate(ArmorPlate p, float v) { p.SetNeraFrontMaterial(NeraMaterialAt(v)); }, p => p.IsNeraPlate, (p, v) => NeraMaterialReadout(NeraMaterialAt(v)), true);
		AddSlider("sandbox.nera_mat_rear", ref y, 0f, NeraArmor.AllowedPlateMaterials.Length - 1, "unit.none", "F0", true, p => NeraMaterialIndex(p.neraRearMaterial), delegate(ArmorPlate p, float v) { p.SetNeraRearMaterial(NeraMaterialAt(v)); }, p => p.IsNeraPlate, (p, v) => NeraMaterialReadout(NeraMaterialAt(v)), true);
		AddSlider("sandbox.nera_interlayer_mat", ref y, 0f, InterlayerMaterialCount - 1, "unit.none", "F0", true, p => (float)p.neraInterlayer, delegate(ArmorPlate p, float v) { p.neraInterlayer = InterlayerMaterialAt(v); }, p => p.IsNeraPlate, (p, v) => NeraInterlayerReadout(InterlayerMaterialAt(v)), true);
		AddSlider("sandbox.nera_extent", ref y, 20f, 500f, "unit.mm", "F0", true, p => p.neraExtentMM, delegate(ArmorPlate p, float v) { p.SetNeraExtent(v); }, p => p.IsNeraPlate, (p, v) => Localization.T("sandbox.nera_extent_note"), true);
		AddSlider("sandbox.nera_bonding", ref y, 0f, NeraBondingCount - 1, "unit.none", "F0", true, p => (float)p.neraBonding, delegate(ArmorPlate p, float v) { p.SetNeraBonding(NeraBondingAt(v)); }, p => p.IsNeraPlate, (p, v) => NeraBondingReadout(NeraBondingAt(v)), true);
		AddSlider("sandbox.liner_fabric", ref y, 0f, 1f, "unit.none", "F0", true, p => p.linerMaterial == SpallLinerMaterial.UHMWPE ? 1f : 0f, delegate(ArmorPlate p, float v) { p.linerMaterial = (SpallLinerMaterial)(v > 0.5f ? 1 : 0); }, p => p.IsSpallLiner, (p, v) => LinerReadout(p, (SpallLinerMaterial)(v > 0.5f ? 1 : 0)), true);
		AddSlider("sandbox.block_material", ref y, 0f, 1f, "unit.none", "F0", true, p => (float)p.blockMaterial, delegate(ArmorPlate p, float v) { p.blockMaterial = (StandoffDefences.BlockMaterial)(v > 0.5f ? 1 : 0); }, p => p.IsEngineBlock, (p, v) => StandoffDefences.BlockMaterialName((StandoffDefences.BlockMaterial)(v > 0.5f ? 1 : 0)), true);
	}

	private void PickMaterial(string id)
	{
		ArmorPlate.ArmorType type;
		if (Catalog.TryMaterialType(id, out type))
		{
			ApplyMaterial(type);
			return;
		}
		Debug.LogError("EditPanelUI: в реестре нет материала с ключом '" + id + "'");
	}

	private void SetTooltip(string text)
	{
		if (tooltipLabel != null)
		{
			tooltipLabel.text = text;
		}
	}

	private void DuplicateSelection()
	{
		if (selector == null)
		{
			return;
		}
		List<ArmorPlate> srcs = new List<ArmorPlate>(selector.SelectedPlates);
		if (srcs.Count == 0)
		{
			return;
		}
		ArmorBuilder builder = UnityEngine.Object.FindFirstObjectByType<ArmorBuilder>();
		if (builder == null)
		{
			Debug.LogWarning("EditPanelUI: не найден ArmorBuilder — копирование недоступно.");
			return;
		}
		Vector3 offset = DuplicateOffset(srcs);
		List<ArmorPlate> copies = new List<ArmorPlate>();
		foreach (ArmorPlate src in srcs)
		{
			ArmorPlate copy = builder.DuplicatePlate(src, offset);
			if (copy != null)
			{
				copies.Add(copy);
			}
		}
		selector.SetSelection(copies);
	}

	private Vector3 DuplicateOffset(List<ArmorPlate> srcs)
	{
		float diameterMM = current != null ? current.thicknessMM : 50f;
		float gap = Mathf.Max(0.0001f, SceneScale.RodDiameterWorld(diameterMM));
		float maxBack = float.NegativeInfinity;
		ArmorPlate[] plates = UnityEngine.Object.FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None);
		for (int i = 0; i < plates.Length; i++)
		{
			if (plates[i] == null)
			{
				continue;
			}
			DepthSpan(plates[i], out float front, out float back);
			if (back > maxBack)
			{
				maxBack = back;
			}
		}
		float minFront = float.PositiveInfinity;
		foreach (ArmorPlate plate in srcs)
		{
			if (plate == null)
			{
				continue;
			}
			DepthSpan(plate, out float front, out float back);
			if (front < minFront)
			{
				minFront = front;
			}
		}
		Vector3 axis = SectionFrame.StackAxis();
		if (float.IsInfinity(maxBack) || float.IsInfinity(minFront))
		{
			return axis * gap;
		}
		return axis * (maxBack + gap - minFront);
	}

	private static void DepthSpan(ArmorPlate p, out float front, out float back)
	{
		Vector3 axis = SectionFrame.StackAxis();
		bool hasBounds = false;
		Bounds bounds = default(Bounds);
		Renderer[] renderers = p.GetComponentsInChildren<Renderer>();
		for (int i = 0; i < renderers.Length; i++)
		{
			Renderer renderer = renderers[i];
			if (renderer == null)
			{
				continue;
			}
			Bounds b = renderer.bounds;
			if (b.size.sqrMagnitude < 1e-12f)
			{
				continue;
			}
			if (!hasBounds)
			{
				hasBounds = true;
				bounds = b;
			}
			else
			{
				bounds.Encapsulate(b);
			}
		}
		if (hasBounds)
		{
			float centre = Vector3.Dot(bounds.center, axis);
			float half = (Mathf.Abs(axis.x) * bounds.size.x + Mathf.Abs(axis.y) * bounds.size.y + Mathf.Abs(axis.z) * bounds.size.z) * 0.5f;
			front = centre - half;
			back = centre + half;
			return;
		}
		Vector3 scale = SceneScale.PlateLocalScale(p.widthM, p.heightM, p.thicknessMM);
		Transform transform = p.transform;
		float halfLocal = (Mathf.Abs(Vector3.Dot(transform.right, axis)) * scale.x + Mathf.Abs(Vector3.Dot(transform.up, axis)) * scale.y + Mathf.Abs(Vector3.Dot(transform.forward, axis)) * scale.z) * 0.5f;
		float centreLocal = Vector3.Dot(transform.position, axis);
		front = centreLocal - halfLocal;
		back = centreLocal + halfLocal;
	}

	private void RefreshPanel()
	{
		if (panelRoot != null)
		{
			panelRoot.SetActive(true);
		}
		bool hasSelection = current != null;
		if (titleLabel != null)
		{
			if (!hasSelection)
			{
				titleLabel.text = Localization.T("sandbox.no_selection");
			}
			else
			{
				string name = current.name;
				string extra = selector.SelectedPlates.Count < 2 ? string.Empty : string.Format("  (+{0})", selector.SelectedPlates.Count - 1);
				titleLabel.text = name + extra + "\n" + Catalog.MaterialName(current.armorType);
			}
		}
		if (materialGrid != null)
		{
			materialGrid.SetSelected(hasSelection ? Catalog.MaterialIdFor(current.armorType) : null);
			materialGrid.SetInteractable(hasSelection);
		}
		if (objectGrid != null)
		{
			objectGrid.SetSelected(hasSelection ? Catalog.ObjectIdFor(current) : null);
			objectGrid.SetInteractable(hasSelection && !current.IsModel);
		}
		RefreshCrewReport();
		RelayoutRows(hasSelection);
	}

	private void RefreshCrewControls()
	{
		SectionView section = SectionView.Instance;
		bool available = section != null;
		SetViewButton(sectionButton, "section", available && section.sectionOn, available);
		SetViewButton(crewOrgansButton, "organs", GameSettings.CrewOrgans, true);
		SetViewButton(crewHighlightButton, "highlight", GameSettings.CrewDamageHighlight, true);
	}

	private static void SetViewButton(Button button, string key, bool on, bool available)
	{
		if (button == null)
		{
			return;
		}
		button.interactable = available;
		TMP_Text label = button.GetComponentInChildren<TMP_Text>();
		label.text = CrewPanelText.T(key) + ": " + Localization.T(on ? "settings.on" : "settings.off");
		label.enableAutoSizing = true;
		label.fontSizeMin = 10f;
		label.fontSizeMax = 13f;
		button.GetComponent<Image>().color = on ? new Color(0.2f, 0.4f, 0.3f, 1f) : MenuTheme.SurfaceAlt;
	}

	private bool RefreshCrewReport()
	{
		if (crewReportLabel == null)
		{
			return false;
		}
		bool crew = current != null && current.IsCrew;
		string signature = string.Empty;
		if (crew)
		{
			signature = string.Join(string.Empty, new string[]
			{
				current.crewFragmentHits.ToString(), ":", current.crewDepositedJ.ToString(), ":", current.crewDirectHits.ToString(), ":", current.crewOrganHits.ToString()
			});
		}
		if (signature == crewReportSignature)
		{
			return false;
		}
		crewReportSignature = signature;
		crewReportLabel.gameObject.SetActive(crew);
		if (crew)
		{
			string breakdown = CrewModel.OrganBreakdown(current.crewOrganJ);
			if (string.IsNullOrEmpty(breakdown))
			{
				breakdown = CrewPanelText.T("none");
			}
			crewReportLabel.text = CrewPanelText.T("organ_hits") + ":" + breakdown;
		}
		return true;
	}

	private void RelayoutRows(bool hasSelection, bool refreshValues = true)
	{
		float y = rowsTopY;
		if (tooltipLabel != null)
		{
			Vector2 preferred = tooltipLabel.GetPreferredValues(tooltipLabel.text, 190f, 0f);
			float height = Mathf.Max(42f, preferred.y);
			tooltipLabel.rectTransform.anchoredPosition = new Vector2(0f, y);
			tooltipLabel.rectTransform.sizeDelta = new Vector2(190f, height);
			y -= height + 6f;
		}
		if (crewReportLabel != null && crewReportLabel.gameObject.activeSelf)
		{
			Vector2 preferred = crewReportLabel.GetPreferredValues(crewReportLabel.text, 190f, 0f);
			float height = Mathf.Max(36f, preferred.y);
			crewReportLabel.rectTransform.anchoredPosition = new Vector2(0f, y);
			crewReportLabel.rectTransform.sizeDelta = new Vector2(190f, height);
			y -= height + 12f;
		}
		if (neraPackLabel != null)
		{
			bool show = hasSelection && current.IsNeraPlate;
			neraPackLabel.gameObject.SetActive(show);
			if (show)
			{
				neraPackLabel.text = NeraPackReadout(current);
				Vector2 preferred = neraPackLabel.GetPreferredValues(neraPackLabel.text, 190f, 0f);
				float height = Mathf.Max(36f, preferred.y);
				neraPackLabel.rectTransform.anchoredPosition = new Vector2(0f, y);
				neraPackLabel.rectTransform.sizeDelta = new Vector2(190f, height);
				y -= height + 12f;
			}
		}
		foreach (SliderBinding b in bindings)
		{
			if (refreshValues && b.input != null)
			{
				NumberField.Close(b.input);
			}
			bool interactable;
			bool visible;
			if (!hasSelection)
			{
				interactable = false;
				visible = b.always && !hasSelection;
			}
			else if (b.hideFor != null && b.hideFor(current))
			{
				interactable = false;
				visible = b.always && !hasSelection;
			}
			else if (!b.always && b.appliesTo != null)
			{
				interactable = b.appliesTo(current);
				visible = interactable || (b.always && !hasSelection);
			}
			else
			{
				interactable = true;
				visible = true;
			}
			if (b.row != null)
			{
				b.row.gameObject.SetActive(visible);
			}
			if (!visible)
			{
				continue;
			}
			b.row.anchoredPosition = new Vector2(0f, y);
			y -= b.rowHeight;
			if (b.slider != null)
			{
				b.slider.interactable = interactable;
			}
			if (!refreshValues)
			{
				continue;
			}
			if (!hasSelection)
			{
				if (b.label != null)
				{
					b.label.text = b.title + ": —";
				}
			}
			else
			{
				float value = b.getter(current);
				if (b.slider != null)
				{
					b.slider.value = value;
				}
				UpdateLabel(b, value);
			}
		}
		if (panelRoot != null)
		{
			RectTransform rect = panelRoot.GetComponent<RectTransform>();
			rect.sizeDelta = new Vector2(220f, -y + 15f);
			SidePanelScroll.Fit(rect, 20f);
		}
	}

	private void PickObjectKind(string id)
	{
		ArmorPlate.ObjectKind kind;
		if (!Catalog.TryObjectKind(id, out kind))
		{
			Debug.LogError("EditPanelUI: в реестре нет вида объекта с ключом '" + id + "'");
			return;
		}
		if (current == null)
		{
			return;
		}
		foreach (ArmorPlate plate in selector.SelectedPlates)
		{
			if (plate != null && !plate.IsModel)
			{
				plate.SetKind(kind);
				Catalog.ApplyObjectPreset(id, plate);
			}
		}
		RefreshPanel();
	}

	private static string ModelSizeReadout(ArmorPlate p, float k)
	{
		SandboxModel model = p != null ? p.GetComponent<SandboxModel>() : null;
		if (model == null || model.prepared == null)
		{
			return string.Empty;
		}
		Vector3 size = model.prepared.bounds.size * (k * 1000f);
		return Localization.T("sandbox.model_size", size.x.ToString("F0", Localization.Culture), size.y.ToString("F0", Localization.Culture), size.z.ToString("F0", Localization.Culture));
	}

	private static Vector3 ShotDir()
	{
		Projectile projectile = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		return projectile != null ? projectile.direction : Vector3.forward;
	}

	private static float ConeDiameterMM()
	{
		Projectile projectile = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		if (projectile != null)
		{
			ProjectileDefinition definition = projectile.GetDefinition();
			if (definition != null && definition.IsShapedCharge)
			{
				return definition.coneDiameterMM;
			}
		}
		return 0f;
	}

	private static string ScreenStandoffReadout(float mm)
	{
		Projectile projectile = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		ProjectileDefinition definition = projectile != null ? projectile.GetDefinition() : null;
		if (definition == null || !definition.IsShapedCharge)
		{
			return Localization.T("sandbox.not_shaped");
		}
		float cd = mm / definition.coneDiameterMM;
		ShapedChargeSpec spec = definition.GetShapedChargeSpec();
		var rha = TateModel.RHA();
		float breakEven = StandoffDefences.ScreenBreakEvenCD(definition.coneDiameterMM, definition.fuzeStandoffMM, delegate(float sCD)
		{
			return ShapedChargeCore.Compute(spec, spec.coneDiameterM * sCD, 1f, rha.density, rha.Rt, 300, true, true).totalPenetrationM;
		});
		string note;
		if (breakEven >= 0f)
		{
			note = Localization.T(cd < breakEven ? "sandbox.screen_before_be" : "sandbox.screen_past_be", string.Format("{0:F1}", breakEven));
		}
		else
		{
			note = Localization.T("sandbox.screen_no_gain");
		}
		bool unanchored = cd < 4f || cd > 10f;
		return Localization.T("sandbox.screen_cd", string.Format("{0:F2}", cd)) + note + "  " + Localization.T(unanchored ? "sandbox.screen_unanchored" : "sandbox.screen_anchored");
	}

	private static string NeraInterlayerReadout(InterlayerMaterial m)
	{
		return Localization.T("sandbox.nera_interlayer_j") + "  " + Localization.T(SuperNera.InterlayerNoteKey(m));
	}

	private static NeraBonding NeraBondingAt(float v)
	{
		int index = Mathf.RoundToInt(v);
		int last = NeraBondingCount - 1;
		if (index < 0)
		{
			return (NeraBonding)0;
		}
		return (NeraBonding)(index <= last ? index : last);
	}

	private static string NeraBondingReadout(NeraBonding b)
	{
		if (b == NeraBonding.Bonded)
		{
			return Localization.T("sandbox.nera_bond.bonded");
		}
		if (b == NeraBonding.Unbonded)
		{
			return Localization.T("sandbox.nera_bond.unbonded");
		}
		return Localization.T("sandbox.nera_bond.unknown");
	}

	private static string NeraPackReadout(ArmorPlate p)
	{
		if (p == null || !p.IsNeraPlate)
		{
			return string.Empty;
		}
		string id = NeraCatalog.MatchingId(p);
		string text;
		if (id == null)
		{
			text = Localization.T("sandbox.nera_state_custom");
		}
		else
		{
			NeraPreset preset;
			bool found = NeraCatalog.TryGet(id, out preset);
			text = Localization.T("sandbox.nera_state_row", SuperShotRunner.PresetName(id, found ? preset.displayName : id));
		}
		NeraPreset currentPreset = SuperShotRunner.NeraPresetOf(p);
		SuperNera.Pack pack = SuperNera.Build(currentPreset, p.neraExtentMM / 1000f, p.neraMountAngleDeg, 0f, SuperPreset.SpacingM, p.neraFrontMaterial, p.neraRearMaterial, p.neraBonding, p.neraInterlayer);
		string details;
		if (pack.refuseKey != null)
		{
			details = Localization.T("super.avail.prefix") + (pack.refuseArgs != null && pack.refuseArgs.Length != 0 ? Localization.T(pack.refuseKey, pack.refuseArgs) : Localization.T(pack.refuseKey));
		}
		else
		{
			details = Localization.T("sandbox.nera_layers", pack.frontLayers, pack.interLayers, pack.rearLayers, pack.frontBakedM * 1000f, pack.interBakedM * 1000f, pack.rearBakedM * 1000f, pack.frontErr, pack.interErr, pack.rearErr, SuperPreset.SpacingM * 1000f);
		}
		return text + "  " + details;
	}

	private static string NeraVelocityReadout(ArmorPlate p)
	{
		if (p == null || !p.IsNeraPlate)
		{
			return string.Empty;
		}
		NeraSpec spec = p.GetNeraSpec();
		float front = NeraArmor.FrontVelocityMS(spec);
		float rear = NeraArmor.RearVelocityMS(spec);
		bool inBand = front >= 100f && front <= 250f && rear >= 100f && rear <= 250f;
		return Localization.T("sandbox.nera_speeds", string.Format("{0:F0}", front), string.Format("{0:F0}", rear), string.Format("{0:F2}", NeraArmor.VelocityRatio(spec))) + Localization.T(inBand ? "sandbox.nera_band_in" : "sandbox.nera_band_out");
	}

	private static string LinerReadout(ArmorPlate p, SpallLinerMaterial m)
	{
		if (p == null)
		{
			return string.Empty;
		}
		float thickness = p.LinerThicknessM;
		float density = SpallLinerModel.ArealDensityKgM2(m, thickness);
		float velocity = SpallLinerModel.CatchVelocityMS(m, thickness, 0.001f, SpallLinerModel.ReferenceFragmentDiameterM);
		return Localization.T("sandbox.liner_readout", LinerName(m), string.Format("{0:F1}", density), string.Format("{0:F0}", velocity));
	}

	private static string LinerName(SpallLinerMaterial m)
	{
		if (m == SpallLinerMaterial.Aramid)
		{
			return Localization.T("material.liner.aramid");
		}
		if (m == SpallLinerMaterial.UHMWPE)
		{
			return Localization.T("material.liner.uhmwpe");
		}
		return SpallLinerModel.Name(m);
	}

	private static string NeraMaterialNote(ArmorPlate.ArmorType t)
	{
		ArmorPlate.ArmorType clamped = NeraArmor.ClampPlateMaterial(t);
		if (clamped == ArmorPlate.ArmorType.HHA)
		{
			return Localization.T("sandbox.nera_note.hha");
		}
		if (clamped == ArmorPlate.ArmorType.Titanium)
		{
			return Localization.T("sandbox.nera_note.titanium");
		}
		return Localization.T("sandbox.nera_note.ductile");
	}

	private static int NeraMaterialIndex(ArmorPlate.ArmorType t)
	{
		for (int i = 0; i < NeraArmor.AllowedPlateMaterials.Length; i++)
		{
			if (NeraArmor.AllowedPlateMaterials[i] == t)
			{
				return i;
			}
		}
		return 0;
	}

	private static ArmorPlate.ArmorType NeraMaterialAt(float v)
	{
		int index = Mathf.RoundToInt(v);
		if (index < 0)
		{
			index = 0;
		}
		else if (index > NeraArmor.AllowedPlateMaterials.Length - 1)
		{
			index = NeraArmor.AllowedPlateMaterials.Length - 1;
		}
		return NeraArmor.AllowedPlateMaterials[index];
	}

	private static InterlayerMaterial InterlayerMaterialAt(float v)
	{
		int index = Mathf.RoundToInt(v);
		int last = InterlayerMaterialCount - 1;
		if (index < 0)
		{
			return (InterlayerMaterial)0;
		}
		return (InterlayerMaterial)(index <= last ? index : last);
	}

	private static string NeraMaterialReadout(ArmorPlate.ArmorType t)
	{
		var target = NeraArmor.PlateTarget(t);
		return Localization.T("sandbox.nera_mat_readout", Catalog.MaterialName(t), string.Format("{0:F0}", target.density), string.Format("{0:F1}", target.Rt / 1000000000f), NeraMaterialNote(t)) + " " + EraCrackingNote;
	}

	private static string SlatReadout(ArmorPlate p)
	{
		Projectile projectile = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		ProjectileDefinition definition = projectile != null ? projectile.GetDefinition() : null;
		if (definition != null && definition.IsShapedCharge && definition.coneDiameterMM > 0.001f)
		{
			float probability = StandoffDefences.SlatHitProbability(p.slatBarWidthMM / 1000f, definition.coneDiameterMM / 1000f, p.slatPitchMM / 1000f);
			return Localization.T("sandbox.slat_prob", string.Format("{0:F3}", probability));
		}
		return Localization.T("sandbox.not_shaped");
	}

	private void ApplyMaterial(ArmorPlate.ArmorType type)
	{
		if (current == null)
		{
			return;
		}
		foreach (ArmorPlate plate in selector.SelectedPlates)
		{
			if (plate != null)
			{
				plate.SetArmorType(type);
			}
		}
		RefreshPanel();
	}

	private void AddSlider(string titleKey, ref float y, float min, float max, string unitKey, string format, bool wholeNumbers, Func<ArmorPlate, float> getter, Action<ArmorPlate, float> setter, Func<ArmorPlate, bool> appliesTo = null, Func<ArmorPlate, float, string> extraLabel = null, bool always = false, float typedMin = 0f / 0f, float typedMax = 0f / 0f, Func<ArmorPlate, bool> hideFor = null, Func<float, float> shown = null, Func<float, float> unshown = null)
	{
		SliderBinding b = new SliderBinding();
		b.titleKey = titleKey;
		b.unitKey = unitKey;
		b.title = Localization.T(titleKey);
		b.unit = Localization.T(unitKey);
		b.format = format;
		b.getter = getter;
		b.min = min;
		b.max = max;
		b.setter = setter;
		b.appliesTo = appliesTo;
		b.extraLabel = extraLabel;
		b.typedMin = typedMin;
		b.typedMax = typedMax;
		b.always = always;
		b.hideFor = hideFor;
		b.shown = shown;
		b.unshown = unshown;
		float top = y;
		GameObject rowObject = new GameObject("Row_" + titleKey, typeof(RectTransform));
		rowObject.transform.SetParent(panelRoot.transform, false);
		RectTransform row = (RectTransform)rowObject.transform;
		row.anchorMax = new Vector2(0.5f, 1f);
		row.anchorMin = new Vector2(0.5f, 1f);
		row.pivot = new Vector2(0.5f, 1f);
		row.sizeDelta = new Vector2(220f, 0f);
		row.anchoredPosition = new Vector2(0f, top);
		MakeEditSliderRow(rowObject, b.title, ref y, min, max, min, b.unit, out b.slider, out b.label, v => OnSliderChanged(b, v), wholeNumbers);
		b.row = row;
		b.rowHeight = -y;
		row.sizeDelta = new Vector2(220f, b.rowHeight);
		y = top - b.rowHeight;
		b.input = NumberField.Attach(b.label, delegate
		{
			SnapshotTypedTargets(b);
			return CurrentValue(b);
		}, v => ApplyTyped(b, v), b.format);
		bindings.Add(b);
		if (b.slider != null)
		{
			SliderPointerEvents events = b.slider.gameObject.AddComponent<SliderPointerEvents>();
			events.onDown = () => BeginCapture(b);
			events.onUp = () => EndCapture(b);
		}
	}

	private float CurrentValue(SliderBinding b)
	{
		float value = current != null ? b.getter(current) : (b.slider != null ? b.slider.value : b.min);
		if (b.shown != null)
		{
			value = b.shown(value);
		}
		return value;
	}

	private void SnapshotTypedTargets(SliderBinding b)
	{
		b.typedTargets.Clear();
		if (selector == null)
		{
			return;
		}
		foreach (ArmorPlate plate in selector.SelectedPlates)
		{
			if (plate != null)
			{
				b.typedTargets.Add(plate);
			}
		}
	}

	private void ApplyTyped(SliderBinding b, float value)
	{
		if (b.unshown != null)
		{
			if (value <= 0f)
			{
				return;
			}
			value = b.unshown(value);
		}
		float min = float.IsNaN(b.typedMin) ? b.min : b.typedMin;
		float max = float.IsNaN(b.typedMax) ? b.max : b.typedMax;
		value = Mathf.Clamp(value, min, max);
		if (b.slider != null && b.slider.wholeNumbers)
		{
			value = Mathf.Round(value);
		}
		if (b.typedTargets.Count == 0 && current != null)
		{
			b.typedTargets.Add(current);
		}
		foreach (ArmorPlate plate in b.typedTargets)
		{
			if (plate != null && (b.hideFor == null || !b.hideFor(plate)))
			{
				b.setter(plate, value);
			}
		}
		if (b.slider != null)
		{
			b.slider.value = Mathf.Clamp(value, b.min, b.max);
		}
		UpdateLabel(b, value);
		b.typedTargets.Clear();
	}

	private void BeginCapture(SliderBinding b)
	{
		if (selector == null)
		{
			return;
		}
		b.captured = true;
		b.grabSliderValue = b.slider.value;
		b.grabValues.Clear();
		foreach (ArmorPlate plate in selector.SelectedPlates)
		{
			if (plate != null)
			{
				b.grabValues[plate] = b.getter(plate);
			}
		}
	}

	private void EndCapture(SliderBinding b)
	{
		b.captured = false;
		b.grabValues.Clear();
	}

	private void OnSliderChanged(SliderBinding b, float newSliderValue)
	{
		if (current == null)
		{
			return;
		}
		if (!b.captured)
		{
			BeginCapture(b);
		}
		float grab = b.grabSliderValue;
		foreach (KeyValuePair<ArmorPlate, float> pair in b.grabValues)
		{
			ArmorPlate plate = pair.Key;
			if (plate == null || (b.hideFor != null && b.hideFor(plate)))
			{
				continue;
			}
			float value = Mathf.Clamp(pair.Value + (newSliderValue - grab), b.min, b.max);
			b.setter(plate, value);
		}
		UpdateLabel(b, newSliderValue);
	}

	private void UpdateLabel(SliderBinding b, float value)
	{
		if (b.label == null)
		{
			return;
		}
		string extra = null;
		if (b.extraLabel != null && current != null)
		{
			extra = b.extraLabel(current, value);
		}
		if (b.shown != null)
		{
			value = b.shown(value);
		}
		b.label.text = b.title + ": " + value.ToString(b.format) + " " + b.unit + (string.IsNullOrEmpty(extra) ? string.Empty : "   " + extra);
	}

	private GameObject MakePanel(string name, Vector2 anchoredPos, Vector2 size, Vector2 anchor)
	{
		GameObject panel = new GameObject(name, typeof(Image));
		panel.transform.SetParent(canvas.transform, false);
		panel.GetComponent<Image>().color = MenuTheme.PanelBackground;
		RectTransform rect = panel.GetComponent<RectTransform>();
		rect.anchorMin = anchor;
		rect.anchorMax = anchor;
		rect.pivot = anchor;
		rect.sizeDelta = size;
		rect.anchoredPosition = anchoredPos;
		return panel;
	}

	private TMP_Text MakeLabel(GameObject parent, string text, Vector2 pos, float size, TextAlignmentOptions align, float width, float height = 24f)
	{
		GameObject labelObject = new GameObject("Label", typeof(TextMeshProUGUI));
		labelObject.transform.SetParent(parent.transform, false);
		TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
		label.text = text;
		label.fontSize = size;
		label.alignment = align;
		label.color = Color.white;
		label.textWrappingMode = TextWrappingModes.NoWrap;
		RectTransform rect = labelObject.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.sizeDelta = new Vector2(width, height);
		rect.anchoredPosition = pos;
		return label;
	}

	private Button MakeActionButton(GameObject parent, string text, Vector2 pos, Vector2 size, Color color, UnityAction onClick)
	{
		GameObject buttonObject = new GameObject("ActionBtn_" + text, typeof(Image), typeof(Button));
		buttonObject.transform.SetParent(parent.transform, false);
		buttonObject.GetComponent<Image>().color = color;
		Button button = buttonObject.GetComponent<Button>();
		button.onClick.AddListener(onClick);
		RectTransform rect = buttonObject.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.sizeDelta = size;
		rect.anchoredPosition = pos;
		TMP_Text label = MakeLabel(buttonObject, text, Vector2.zero, 13f, TextAlignmentOptions.Center, size.x);
		RectTransform labelRect = label.GetComponent<RectTransform>();
		labelRect.anchorMin = Vector2.zero;
		labelRect.anchorMax = Vector2.one;
		labelRect.sizeDelta = Vector2.zero;
		labelRect.anchoredPosition = Vector2.zero;
		return button;
	}

	private void MakeEditSliderRow(GameObject parent, string name, ref float y, float min, float max, float initial, string unit, out Slider slider, out TMP_Text label, Action<float> onChange, bool wholeNumbers = false)
	{
		label = MakeLabel(parent, string.Format("{0}: {1:F0} {2}", name, initial, unit), new Vector2(0f, y), 12f, TextAlignmentOptions.Left, 180f);
		y -= 20f;
		GameObject sliderObject = new GameObject("Slider", typeof(Slider));
		sliderObject.transform.SetParent(parent.transform, false);
		slider = sliderObject.GetComponent<Slider>();
		RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
		sliderRect.anchorMin = new Vector2(0.5f, 1f);
		sliderRect.anchorMax = new Vector2(0.5f, 1f);
		sliderRect.pivot = new Vector2(0.5f, 1f);
		sliderRect.sizeDelta = new Vector2(180f, 18f);
		sliderRect.anchoredPosition = new Vector2(0f, y);
		GameObject bg = new GameObject("BG", typeof(Image));
		bg.transform.SetParent(sliderObject.transform, false);
		bg.GetComponent<Image>().color = MenuTheme.SliderTrack;
		StretchFull(bg.GetComponent<RectTransform>());
		GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
		fillArea.transform.SetParent(sliderObject.transform, false);
		StretchFull(fillArea.GetComponent<RectTransform>());
		GameObject fill = new GameObject("Fill", typeof(Image));
		fill.transform.SetParent(fillArea.transform, false);
		fill.GetComponent<Image>().color = MenuTheme.SliderFill;
		StretchFull(fill.GetComponent<RectTransform>());
		GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
		handleArea.transform.SetParent(sliderObject.transform, false);
		StretchFull(handleArea.GetComponent<RectTransform>());
		GameObject handle = new GameObject("Handle", typeof(Image));
		handle.transform.SetParent(handleArea.transform, false);
		Image handleImage = handle.GetComponent<Image>();
		handleImage.color = Color.white;
		handleImage.rectTransform.sizeDelta = new Vector2(10f, 18f);
		slider.fillRect = fill.GetComponent<RectTransform>();
		slider.handleRect = handleImage.rectTransform;
		slider.targetGraphic = handleImage;
		slider.wholeNumbers = wholeNumbers;
		slider.minValue = min;
		slider.maxValue = max;
		slider.value = initial;
		slider.onValueChanged.AddListener(v => onChange(v));
		y -= 30f;
	}

	private void StretchFull(RectTransform rt)
	{
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.sizeDelta = Vector2.zero;
		rt.anchoredPosition = Vector2.zero;
	}
}
