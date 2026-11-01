using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ModelPanelUI : MonoBehaviour
{
	private const float PanelWidth = 300f;

	public const float LeftX = 252f;

	private const float TopGap = 20f;

	private const float BottomReserve = 370f;

	private const float RegionSliderMaxM = 10f;

	private GameObject _view;

	private GameObject panel;

	private TMP_Text statusLabel;

	private TMP_Text reportLabel;

	private TMP_Text regionLabel;

	private TMP_Text countLabel;

	private bool collapsed;

	private bool _listOpen;

	private float _scroll;

	private ModelImportSettings _settings;

	private Task<PreparedModel> _loadTask;

	private CancellationTokenSource _cts;

	private float _progress;

	private string _status;

	private string _lastReport;

	private int _modelsShown;

	private string _regionText;

	private float _regionAtT;

	private const float RegionEveryS = 0.5f;

	public RectTransform ViewRect
	{
		get
		{
			if (_view != null)
			{
				return _view.GetComponent<RectTransform>();
			}
			return null;
		}
	}

	public bool Collapsed => collapsed;

	public static void Bootstrap()
	{
		if (UnityEngine.Object.FindFirstObjectByType<ModelPanelUI>() != null)
		{
			return;
		}
		new GameObject("ModelPanelUI").AddComponent<ModelPanelUI>();
	}

	private void Start()
	{
		Build();
		Localization.OnLanguageChanged += Rebuild;
	}

	private void OnDestroy()
	{
		Localization.OnLanguageChanged -= Rebuild;
		if (_cts != null)
		{
			_cts.Cancel();
			_cts.Dispose();
		}
	}

	private void Rebuild()
	{
		if (_view != null)
		{
			ScrollRect component = _view.GetComponent<ScrollRect>();
			if (component != null && component.vertical)
			{
				_scroll = component.verticalNormalizedPosition;
			}
			_view.SetActive(false);
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(_view);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(_view);
			}
		}
		_view = null;
		panel = null;
		Build();
	}

	private static void Kill(UnityEngine.Object o)
	{
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(o);
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(o);
		}
	}

	public void SetCollapsed(bool c)
	{
		collapsed = c;
		Rebuild();
	}

	private void Build()
	{
		Canvas canvas = EnsureCanvas();
		statusLabel = null;
		reportLabel = null;
		regionLabel = null;
		countLabel = null;
		BuildView(canvas.transform);
		MakeButton(panel, Localization.T(!collapsed ? "model.ui.title_open" : "model.ui.title_closed"), new Vector2(0f, -8f), new Vector2(284f, 24f), delegate
		{
			collapsed = !collapsed;
			Rebuild();
		});
		if (collapsed)
		{
			Finish(canvas, -38f);
			return;
		}
		float num = -38f;
		TMP_Text tMP_Text = MakeLabel(panel, Localization.T("model.ui.disclaimer"), new Vector2(0f, num), 9f, TextAlignmentOptions.TopLeft, 280f, 40f);
		tMP_Text.color = new Color(1f, 0.72f, 0.38f, 1f);
		float num2 = Mathf.Max(24f, tMP_Text.GetPreferredValues(tMP_Text.text, 280f, 0f).y);
		tMP_Text.rectTransform.sizeDelta = new Vector2(280f, num2);
		num -= num2 + 6f;
		MakeButton(panel, Localization.T("model.ui.units", new object[1] { UnitsName(_settings.units) }), new Vector2(-96f, num), new Vector2(92f, 24f), delegate
		{
			_settings.units = (ModelUnits)(((int)_settings.units + 1) % 4);
			Rebuild();
		});
		MakeButton(panel, Localization.T("model.ui.up", new object[1] { (_settings.up != 0) ? "Z" : "Y" }), new Vector2(0f, num), new Vector2(92f, 24f), delegate
		{
			_settings.up = ((_settings.up == ModelUpAxis.Y) ? ModelUpAxis.Z : ModelUpAxis.Y);
			Rebuild();
		});
		MakeButton(panel, Localization.T(!_settings.mirrorX ? "model.ui.mirror_off" : "model.ui.mirror_on"), new Vector2(96f, num), new Vector2(96f, 24f), delegate
		{
			_settings.mirrorX = !_settings.mirrorX;
			Rebuild();
		});
		num -= 28f;
		MakeButton(panel, Localization.T("model.ui.load"), new Vector2(0f, num), new Vector2(284f, 26f), new UnityAction(PickFile));
		num -= 30f;
		if (_listOpen)
		{
			num = FileList(num);
		}
		statusLabel = MakeLabel(panel, "", new Vector2(0f, num), 10f, TextAlignmentOptions.TopLeft, 280f, 30f);
		statusLabel.color = new Color(1f, 0.82f, 0.45f, 1f);
		num -= 32f;
		reportLabel = MakeLabel(panel, _lastReport, new Vector2(0f, num), 9f, TextAlignmentOptions.TopLeft, 280f, 40f);
		reportLabel.color = new Color(0.75f, 0.78f, 0.82f, 1f);
		num -= 42f;
		_modelsShown = SandboxModel.All().Count;
		countLabel = MakeLabel(panel, Localization.T("model.ui.models_count", new object[1] { _modelsShown }), new Vector2(0f, num), 10f, TextAlignmentOptions.TopLeft, 280f, 42f);
		countLabel.color = new Color(0.62f, 0.86f, 1f, 1f);
		num -= 44f;
		List<ComputeRegion> list = new List<ComputeRegion>(UnityEngine.Object.FindObjectsByType<ComputeRegion>(FindObjectsSortMode.InstanceID));
		ComputeRegion computeRegion = ((list.Count > 0) ? list[0] : null);
		if (computeRegion == null)
		{
			MakeButton(panel, Localization.T("model.ui.region_add"), new Vector2(0f, num), new Vector2(284f, 22f), delegate
			{
				ComputeRegion.Create(DefaultRegionPoint());
				Rebuild();
			});
			num -= 28f;
		}
		else
		{
			MakeLabel(panel, Localization.T("model.ui.region_head"), new Vector2(0f, num), 11f, TextAlignmentOptions.Center, 280f, 18f);
			num -= 20f;
			num = AddSlider(num, "model.ui.region_w", 0.02f, 10f, (Func<float>)delegate
			{
				return computeRegion.widthM;
			}, (Action<float>)delegate(float v)
			{
				computeRegion.SetSize(v, computeRegion.heightM, computeRegion.depthM);
			}, "F2");
			num = AddSlider(num, "model.ui.region_h", 0.02f, 10f, (Func<float>)delegate
			{
				return computeRegion.heightM;
			}, (Action<float>)delegate(float v)
			{
				computeRegion.SetSize(computeRegion.widthM, v, computeRegion.depthM);
			}, "F2");
			num = AddSlider(num, "model.ui.region_d", 0.02f, 10f, (Func<float>)delegate
			{
				return computeRegion.depthM;
			}, (Action<float>)delegate(float v)
			{
				computeRegion.SetSize(computeRegion.widthM, computeRegion.heightM, v);
			}, "F2");
			num = PoseSliders(computeRegion.transform, num, delegate(float v)
			{
				computeRegion.SetAngles(v, computeRegion.angleY);
			}, delegate(float v)
			{
				computeRegion.SetAngles(computeRegion.angleX, v);
			}, computeRegion.angleX, computeRegion.angleY);
			regionLabel = MakeLabel(panel, _regionText, new Vector2(0f, num), 9f, TextAlignmentOptions.TopLeft, 280f, 40f);
			regionLabel.color = new Color(0.62f, 0.86f, 1f, 1f);
			num -= 42f;
			MakeButton(panel, Localization.T("model.ui.region_remove"), new Vector2(0f, num), new Vector2(284f, 22f), delegate
			{
				computeRegion.gameObject.SetActive(false);
				UnityEngine.Object.Destroy(computeRegion.gameObject);
				Rebuild();
			});
			num -= 28f;
		}
		Finish(canvas, num);
		Refresh();
	}

	private void BuildView(Transform canvas)
	{
		GameObject gameObject = new GameObject("ModelPanel_View", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect));
		_view = gameObject;
		gameObject.transform.SetParent(canvas, false);
		RectTransform component = (RectTransform)gameObject.transform;
		component.anchorMax = new Vector2(0f, 1f);
		component.anchorMin = new Vector2(0f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.anchoredPosition = new Vector2(252f, -20f);
		component.sizeDelta = new Vector2(300f, 40f);
		GameObject gameObject2 = new GameObject("ModelPanel", typeof(Image));
		panel = gameObject2;
		gameObject2.transform.SetParent(gameObject.transform, false);
		gameObject2.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.88f);
		RectTransform component2 = gameObject2.GetComponent<RectTransform>();
		component2.anchorMax = new Vector2(0f, 1f);
		component2.anchorMin = new Vector2(0f, 1f);
		component2.pivot = new Vector2(0f, 1f);
		component2.anchoredPosition = Vector2.zero;
		component2.sizeDelta = new Vector2(300f, 40f);
	}

	private void Finish(Canvas canvas, float y)
	{
		float num = -y + 8f;
		RectTransform rectTransform = canvas.rootCanvas.transform as RectTransform;
		float num2 = ((rectTransform != null) ? rectTransform.rect.height : 1080f) - 20f - 370f;
		float num3 = Math.Max(160f, num2);
		float num4 = Math.Min(num, num3);
		panel.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, num);
		RectTransform component = (RectTransform)_view.transform;
		component.sizeDelta = new Vector2(300f, num4);
		ScrollRect scrollRect = _view.GetComponent<ScrollRect>();
		scrollRect.content = panel.GetComponent<RectTransform>();
		scrollRect.viewport = component;
		scrollRect.horizontal = false;
		scrollRect.movementType = ScrollRect.MovementType.Clamped;
		scrollRect.scrollSensitivity = 30f;
		scrollRect.inertia = false;
		scrollRect.vertical = num > num4 + 0.5f;
		if (!scrollRect.vertical)
		{
			_scroll = 1f;
		}
		else
		{
			scrollRect.verticalNormalizedPosition = Mathf.Clamp(_scroll, 0f, 1f);
		}
		WheelZones.Register(component);
	}

	private float PoseSliders(Transform t, float y, Action<float> setAx, Action<float> setAy, float ax, float ay)
	{
		y = AddSlider(y, "model.ui.pos_x", -5f, 5f, (Func<float>)delegate
		{
			return t.localPosition.x;
		}, (Action<float>)delegate(float v)
		{
			Vector3 localPosition = t.localPosition;
			localPosition.x = v;
			t.localPosition = localPosition;
		}, "F3");
		y = AddSlider(y, "model.ui.pos_y", -5f, 5f, (Func<float>)delegate
		{
			return t.localPosition.y;
		}, (Action<float>)delegate(float v)
		{
			Vector3 localPosition2 = t.localPosition;
			localPosition2.y = v;
			t.localPosition = localPosition2;
		}, "F3");
		y = AddSlider(y, "model.ui.pos_z", -2f, 20f, (Func<float>)delegate
		{
			return t.localPosition.z;
		}, (Action<float>)delegate(float v)
		{
			Vector3 localPosition3 = t.localPosition;
			localPosition3.z = v;
			t.localPosition = localPosition3;
		}, "F3");
		y = AddSlider(y, "model.ui.angle_x", -90f, 90f, (Func<float>)delegate
		{
			return ax;
		}, (Action<float>)delegate(float v)
		{
			ax = v;
			setAx(v);
		}, "F0");
		return AddSlider(y, "model.ui.angle_y", -180f, 180f, (Func<float>)delegate
		{
			return ay;
		}, (Action<float>)delegate(float v)
		{
			ay = v;
			setAy(v);
		}, "F0");
	}

	private float AddSlider(float y, string key, float min, float max, Func<float> get, Action<float> set, string fmt)
	{
		TMP_Text lbl = MakeLabel(panel, Localization.T(key, new object[1] { get().ToString(fmt, Localization.Culture) }), new Vector2(0f, y), 10f, TextAlignmentOptions.Center, 280f, 14f);
		NumberField.Attach(lbl, get, (Action<float>)delegate(float v)
		{
			set(v);
			lbl.text = Localization.T(key, new object[1] { get().ToString(fmt, Localization.Culture) });
		}, fmt);
		y -= 15f;
		float value = Mathf.Clamp(get(), min, max);
		MakeSlider(panel, new Vector2(0f, y), 270f, min, max, value, delegate(float v)
		{
			set(v);
			lbl.text = Localization.T(key, new object[1] { get().ToString(fmt, Localization.Culture) });
		});
		return y - 19f;
	}

	private static string UnitsName(ModelUnits u)
	{
		return Localization.T("model.units." + u.ToString().ToLowerInvariant());
	}

	private static Vector3 DefaultRegionPoint()
	{
		Projectile projectile = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		Vector3 vector = ((projectile != null) ? projectile.transform.position : Vector3.zero);
		Vector3 vector2 = Vector3.right;
		if (projectile != null && projectile.direction.sqrMagnitude > 1e-9f)
		{
			float num = projectile.direction.magnitude;
			if (num > 1e-5f)
			{
				vector2 = projectile.direction / num;
			}
		}
		foreach (SandboxModel item in SandboxModel.All())
		{
			if (item == null)
			{
				continue;
			}
			float num2 = Vector3.Dot(item.transform.position - vector, vector2);
			if (num2 > 0f)
			{
				return vector + vector2 * num2;
			}
		}
		return vector + vector2;
	}

	private void Update()
	{
		if (_loadTask != null && _loadTask.IsCompleted)
		{
			Task<PreparedModel> task = _loadTask;
			_loadTask = null;
			if (task.IsFaulted)
			{
				_status = Explain(task.Exception.GetBaseException());
			}
			else if (!task.IsCanceled)
			{
				SandboxModel sandboxModel = Place(task.Result);
				if (sandboxModel != null && sandboxModel.Plate != null)
				{
					PlateSelector plateSelector = UnityEngine.Object.FindFirstObjectByType<PlateSelector>();
					if (plateSelector != null)
					{
						plateSelector.SetSelection(new ArmorPlate[1] { sandboxModel.Plate });
					}
				}
			}
			Rebuild();
		}
		int count = SandboxModel.All().Count;
		if (countLabel != null && count != _modelsShown)
		{
			_modelsShown = count;
			countLabel.text = Localization.T("model.ui.models_count", new object[1] { count });
		}
		Refresh();
	}

	private void Refresh()
	{
		if (statusLabel != null)
		{
			statusLabel.text = ((_loadTask != null) ? Localization.T("model.ui.working", new object[1] { (double)_progress }) : _status);
		}
		if (regionLabel == null)
		{
			return;
		}
		if (AppFlow.ActiveMode != SimMode.Super)
		{
			regionLabel.text = Localization.T("model.ui.region_super_only");
			return;
		}
		if (Input.GetMouseButton(0) || TouchInputHelper.TouchActive || Time.unscaledTime - _regionAtT < 0.5f)
		{
			regionLabel.text = _regionText;
			return;
		}
		_regionAtT = Time.unscaledTime;
		Projectile projectile = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		if (projectile == null)
		{
			return;
		}
		if (SuperShotRunner.ReadScene(projectile, out SuperRodSpec rod, out SuperJetSpec jet, out bool isJet, out List<SuperPlateSpec> plates, out SuperEraDrive drive, out string reasonKey, out object[] reasonArgs, quiet: true))
		{
			_regionText = Localization.T("model.ui.region_count", new object[1] { SuperBaker.EstimateParticles(0.0, plates, SuperPreset.SpacingM) });
		}
		else
		{
			_regionText = Localization.T(reasonKey, reasonArgs ?? new object[0]);
		}
		regionLabel.text = _regionText;
	}

	private static string Explain(Exception e)
	{
		if (e == null)
		{
			return Localization.T("model.ui.failed", new object[1] { "?" });
		}
		if (e is ModelImportException modelImportException)
		{
			return Localization.T(modelImportException.Key, modelImportException.Args);
		}
		return Localization.T("model.ui.failed", new object[1] { e.GetType().Name ?? "?" });
	}

	private void StartLoad(List<string> paths)
	{
		if (paths == null || paths.Count == 0)
		{
			_status = Localization.T("model.ui.none_picked");
			return;
		}
		if (_cts != null)
		{
			_cts.Cancel();
		}
		_cts = new CancellationTokenSource();
		CancellationToken cancellationToken = _cts.Token;
		ModelImportSettings modelImportSettings = _settings;
		_progress = 0f;
		_listOpen = false;
		_status = "";
		_loadTask = Task.Run(delegate
		{
			ImportedModel importedModel = ModelFileImporter.Load(paths, delegate(float x)
			{
				Thread.MemoryBarrier();
				_progress = x * 0.9f;
			}, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			PreparedModel preparedModel = ModelImport.Prepare(importedModel, modelImportSettings);
			Thread.MemoryBarrier();
			_progress = 1f;
			return preparedModel;
		}, cancellationToken);
		Rebuild();
	}

	private SandboxModel Place(PreparedModel pm)
	{
		Projectile projectile = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		Vector3 vector = ((projectile != null) ? projectile.transform.position : Vector3.zero);
		Vector3 vector2 = Vector3.right;
		if (projectile != null && projectile.direction.sqrMagnitude > 1e-9f)
		{
			float num = projectile.direction.magnitude;
			if (num > 1e-5f)
			{
				vector2 = projectile.direction / num;
			}
		}
		Vector3 vector3 = pm.bounds.extents * 2f;
		float num2 = 0.5f * (Mathf.Abs(vector2.y) * vector3.y + Mathf.Abs(vector2.x) * vector3.x + Mathf.Abs(vector2.z) * vector3.z);
		float num3 = num2 + 1f;
		bool flag = false;
		float num4 = float.NegativeInfinity;
		foreach (SandboxModel item in SandboxModel.All())
		{
			if (item == null)
			{
				continue;
			}
			Renderer[] componentsInChildren = item.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Renderer renderer = componentsInChildren[i];
				if (renderer == null || !renderer.enabled)
				{
					continue;
				}
				Bounds bounds = renderer.bounds;
				float num5 = Vector3.Dot(bounds.center - vector, vector2) + 0.5f * (Mathf.Abs(vector2.y) * bounds.size.y + Mathf.Abs(vector2.x) * bounds.size.x + Mathf.Abs(vector2.z) * bounds.size.z);
				if (num5 > num4)
				{
					num4 = num5;
				}
				flag = true;
			}
		}
		if (flag)
		{
			float num6 = num4 + 0.2f + num2;
			if (num3 <= num6)
			{
				num3 = num6;
			}
		}
		Vector3 worldPos = vector + vector2 * num3;
		SandboxModel sandboxModel = SandboxModel.Create(pm, worldPos);
		StringBuilder stringBuilder = new StringBuilder();
		if (pm.settings.mirrorX)
		{
			stringBuilder.AppendLine(Localization.T("model.report.mirror"));
		}
		int num7 = 0;
		int num8 = 0;
		foreach (ModelPart part in pm.parts)
		{
			num7 += part.triangles.Length / 3;
			if (!part.closed)
			{
				num8++;
			}
		}
		stringBuilder.AppendLine(Localization.T("model.report.loaded", new object[6]
		{
			sandboxModel.DisplayName,
			pm.parts.Count,
			num7,
			vector3.x * 1000f,
			vector3.y * 1000f,
			vector3.z * 1000f
		}));
		if (num8 > 0)
		{
			stringBuilder.AppendLine(Localization.T("model.report.open_parts", new object[1] { num8 }));
		}
		_status = Localization.T("model.ui.placed", new object[1] { pm.parts.Count });
		_lastReport = stringBuilder.ToString();
		return sandboxModel;
	}

	private static void SelectOnly(SandboxModel sm)
	{
		if (sm == null || sm.Plate == null)
		{
			return;
		}
		PlateSelector plateSelector = UnityEngine.Object.FindFirstObjectByType<PlateSelector>();
		if (plateSelector == null)
		{
			return;
		}
		plateSelector.SetSelection(new ArmorPlate[1] { sm.Plate });
	}

	private void LateUpdate()
	{
		if (reportLabel != null && reportLabel.text != _lastReport)
		{
			reportLabel.text = _lastReport;
		}
	}

	private void PickFile()
	{
		_listOpen = !_listOpen;
		Rebuild();
	}

	private float FileList(float y)
	{
		Directory.CreateDirectory(ModelFileImporter.ModelsFolder);
		MakeLabel(panel, Localization.T("model.ui.folder", new object[1] { ModelFileImporter.ModelsFolder }), new Vector2(0f, y), 9f, TextAlignmentOptions.Center, 280f, 28f);
		y -= 30f;
		int num = 0;
		foreach (string path in Directory.EnumerateFiles(ModelFileImporter.ModelsFolder))
		{
			if (!ModelFileImporter.IsSupportedExtension(path))
			{
				continue;
			}
			MakeButton(panel, Path.GetFileName(path), new Vector2(0f, y), new Vector2(284f, 20f), delegate
			{
				StartLoad(new List<string> { path });
			});
			y -= 22f;
			num++;
		}
		if (num == 0)
		{
			MakeLabel(panel, Localization.T("model.ui.folder_empty"), new Vector2(0f, y), 10f, TextAlignmentOptions.Center, 280f, 28f);
			y -= 30f;
		}
		MakeButton(panel, Localization.T("model.ui.close"), new Vector2(0f, y), new Vector2(284f, 22f), new UnityAction(PickFile));
		return y - 28f;
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
		CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
		canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
		canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		canvasScaler.matchWidthOrHeight = 0.5f;
		gameObject.AddComponent<GraphicRaycaster>();
		return canvas2;
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
		GameObject gameObject = new GameObject("Btn", typeof(Image), typeof(Button));
		gameObject.transform.SetParent(parent.transform, false);
		gameObject.GetComponent<Image>().color = new Color(0.2f, 0.21f, 0.24f, 1f);
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
		tMP_Text.fontSize = 11f;
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
		component.sizeDelta = new Vector2(width, 14f);
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
