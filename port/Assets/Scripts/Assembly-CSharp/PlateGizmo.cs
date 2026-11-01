using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class PlateGizmo : MonoBehaviour
{
	public enum Axis
	{
		None = -1,
		X = 0,
		Y = 1,
		Z = 2
	}

	[Header("Габарит на экране")]
	public float pixelLength = 95f;

	public float pickRadiusPx = 14f;

	[Header("Привязка (Ctrl)")]
	public float snapStepM = 0.01f;

	private static readonly Color[] AxisColor = new Color[3]
	{
		new Color(0.92f, 0.28f, 0.3f, 1f),
		new Color(0.42f, 0.83f, 0.36f, 1f),
		new Color(0.32f, 0.54f, 1f, 1f)
	};

	private PlateSelector selector;

	private Camera cam;

	private Transform gizmoRoot;

	private readonly Transform[] axisRoot = new Transform[3];

	private readonly Material[] axisMat = new Material[3];

	private Axis dragAxis = Axis.None;

	private bool modal;

	private Axis modalAxis = Axis.None;

	private string typed = "";

	private readonly List<ArmorPlate> moved = new List<ArmorPlate>();

	private readonly List<Vector3> movedStart = new List<Vector3>();

	private Vector3 pivotStart;

	private float grabT;

	private float grabDepth;

	private Vector3 grabMouseWorld;

	private Vector3 appliedOffset;

	private float gizmoScale;

	private Vector3 gizmoOrigin;

	private TMP_Text hud;

	private static int clickConsumedFrame = -1;

	private static int escapeConsumedFrame = -1;

	public static PlateGizmo Instance { get; private set; }

	private static Color HotColor => MenuTheme.GizmoHot;

	public static bool IsBusy
	{
		get
		{
			if (Instance != null)
			{
				if (Instance.dragAxis != Axis.None)
				{
					return true;
				}
				if (Instance.modal)
				{
					return true;
				}
			}
			return Time.frameCount == clickConsumedFrame;
		}
	}

	public static bool PointerOverAxis
	{
		get
		{
			if (Instance != null && Instance.enabled)
			{
				return Instance.HoverAxis() != Axis.None;
			}
			return false;
		}
	}

	public static bool CancelOnEscape()
	{
		if (Time.frameCount == escapeConsumedFrame)
		{
			return true;
		}
		if (Instance != null && Instance.modal)
		{
			Instance.EndModal(apply: false);
			escapeConsumedFrame = Time.frameCount;
			return true;
		}
		return false;
	}

	public static void Bootstrap()
	{
		if (Object.FindFirstObjectByType<PlateGizmo>() != null)
		{
			return;
		}
		GameObject gameObject = new GameObject("PlateGizmo");
		gameObject.AddComponent<PlateGizmo>();
	}

	private void Awake()
	{
		Instance = this;
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void Start()
	{
		cam = Camera.main;
		selector = Object.FindFirstObjectByType<PlateSelector>();
		BuildArrows();
	}

	private void Update()
	{
		if (cam == null)
		{
			cam = Camera.main;
		}
		if (selector == null)
		{
			selector = Object.FindFirstObjectByType<PlateSelector>();
		}
		if (cam == null || selector == null)
		{
			return;
		}
		ArmorPlate anchor = selector.SelectedPlate;
		if (modal && anchor == null)
		{
			EndModal(apply: true);
		}
		if (anchor == null)
		{
			ShowArrows(false);
			ShowHud(null);
			return;
		}
		UpdateArrowTransform(anchor);
		HandleModal(anchor);
		if (!modal)
		{
			HandleArrowDrag(anchor);
		}
		Paint();
	}

	private void HandleArrowDrag(ArmorPlate anchor)
	{
		if (dragAxis != Axis.None)
		{
			if (!TouchInputHelper.PrimaryHeld)
			{
				dragAxis = Axis.None;
				clickConsumedFrame = Time.frameCount;
				ShowHud(null);
				ClearSnapshot();
				return;
			}
			if (AxisParam(dragAxis, pivotStart, out float t))
			{
				Vector3 off = AxisDir(dragAxis) * Snap(t - grabT);
				ApplyOffset(off);
				ShowHud(HudLine(dragAxis, off, numeric: false, hint: false));
			}
			return;
		}
		if (!TouchInputHelper.PrimaryDown || OverUI() || Time.frameCount == clickConsumedFrame)
		{
			return;
		}
		Axis hover = HoverAxis();
		if (hover != Axis.None)
		{
			dragAxis = hover;
			clickConsumedFrame = Time.frameCount;
			TakeSnapshot(anchor);
			AxisParam(dragAxis, pivotStart, out grabT);
		}
	}

	private void HandleModal(ArmorPlate anchor)
	{
		if (!modal)
		{
			if (dragAxis != Axis.None || NumberField.TypingNow || !Input.GetKeyDown(ControlBindings.Key(ControlAction.GizmoGrab)))
			{
				return;
			}
			TakeSnapshot(anchor);
			if (moved.Count == 0)
			{
				return;
			}
			modal = true;
			modalAxis = Axis.None;
			typed = "";
			grabDepth = cam.WorldToScreenPoint(pivotStart).z;
			Vector2 mousePosition = TouchInputHelper.PrimaryPosition;
			grabMouseWorld = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, grabDepth));
			return;
		}
		if (Input.GetKeyDown(ControlBindings.Key(ControlAction.GizmoAxisX)))
		{
			SetModalAxis(Axis.X);
		}
		else if (Input.GetKeyDown(ControlBindings.Key(ControlAction.GizmoAxisY)))
		{
			SetModalAxis(Axis.Y);
		}
		else if (Input.GetKeyDown(ControlBindings.Key(ControlAction.GizmoAxisZ)))
		{
			SetModalAxis(Axis.Z);
		}
		ReadTyping();
		Vector3 off = Vector3.zero;
		float num = 0f;
		bool flag = modalAxis != Axis.None && NumberField.TryParse(typed, out num);
		if (flag)
		{
			off = AxisDir(modalAxis) * SceneScale.FromM(num);
		}
		else if (modalAxis != Axis.None)
		{
			if (AxisParam(modalAxis, pivotStart, out float t))
			{
				off = AxisDir(modalAxis) * Snap(t - grabT);
			}
		}
		else
		{
			Vector3 world = cam.ScreenToWorldPoint(new Vector3(TouchInputHelper.PrimaryPosition.x, TouchInputHelper.PrimaryPosition.y, grabDepth));
			off = world - grabMouseWorld;
			if (SnapHeld())
			{
				off = new Vector3(SnapValue(off.x), SnapValue(off.y), SnapValue(off.z));
			}
		}
		ApplyOffset(off);
		ShowHud(HudLine(modalAxis, off, flag, hint: true));
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			modal = false;
			modalAxis = Axis.None;
			typed = "";
			ShowHud(null);
			ClearSnapshot();
			return;
		}
		if (TouchInputHelper.PrimaryDown)
		{
			clickConsumedFrame = Time.frameCount;
			EndModal(apply: true);
		}
		else if (ControlBindings.GizmoCancelOnRightButton && Input.GetMouseButtonDown(1))
		{
			clickConsumedFrame = Time.frameCount;
			EndModal(apply: false);
		}
	}

	private void BeginModal(ArmorPlate anchor)
	{
		TakeSnapshot(anchor);
		if (moved.Count == 0)
		{
			return;
		}
		modal = true;
		modalAxis = Axis.None;
		typed = "";
		grabDepth = cam.WorldToScreenPoint(pivotStart).z;
		Vector2 mousePosition = TouchInputHelper.PrimaryPosition;
		grabMouseWorld = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, grabDepth));
	}

	private void SetModalAxis(Axis a)
	{
		modalAxis = ((a == modalAxis) ? Axis.None : a);
		typed = "";
		if (modalAxis != Axis.None)
		{
			AxisParam(modalAxis, pivotStart, out grabT);
			return;
		}
		grabDepth = cam.WorldToScreenPoint(pivotStart).z;
		Vector2 mousePosition = TouchInputHelper.PrimaryPosition;
		grabMouseWorld = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, grabDepth));
	}

	private void EndModal(bool apply)
	{
		if (!apply)
		{
			ApplyOffset(Vector3.zero);
		}
		modal = false;
		modalAxis = Axis.None;
		typed = "";
		ShowHud(null);
		ClearSnapshot();
	}

	private void ReadTyping()
	{
		string input = Input.inputString;
		if (string.IsNullOrEmpty(input))
		{
			return;
		}
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (c == '\b')
			{
				if (typed.Length > 0)
				{
					typed = typed.Substring(0, typed.Length - 1);
				}
			}
			else if (c == '-')
			{
				typed = typed.StartsWith("-") ? typed.Substring(1) : ("-" + typed);
			}
			else if (char.IsDigit(c))
			{
				typed += c;
			}
			else if (c == ',' || c == '.')
			{
				if (!typed.Contains("."))
				{
					typed += ".";
				}
			}
		}
	}

	private void TakeSnapshot(ArmorPlate anchor)
	{
		ClearSnapshot();
		if (anchor == null)
		{
			return;
		}
		pivotStart = anchor.transform.position;
		foreach (ArmorPlate item in selector.SelectedPlates)
		{
			if (item == null)
			{
				continue;
			}
			moved.Add(item);
			movedStart.Add(item.transform.position);
		}
		if (moved.Count == 0)
		{
			moved.Add(anchor);
			movedStart.Add(anchor.transform.position);
		}
		appliedOffset = Vector3.zero;
	}

	private void ClearSnapshot()
	{
		moved.Clear();
		movedStart.Clear();
		appliedOffset = Vector3.zero;
	}

	private void ApplyOffset(Vector3 off)
	{
		for (int i = 0; i < moved.Count; i++)
		{
			ArmorPlate armorPlate = moved[i];
			if (armorPlate == null)
			{
				continue;
			}
			armorPlate.transform.position = movedStart[i] + off;
		}
		appliedOffset = off;
	}

	private bool SnapHeld()
	{
		return ControlBindings.Held(ControlModifier.GizmoSnap);
	}

	private float Snap(float v)
	{
		if (!SnapHeld())
		{
			return v;
		}
		return SnapValue(v);
	}

	private float SnapValue(float v)
	{
		float num = Mathf.Max(snapStepM, 0.0001f);
		return Mathf.Round(v / num) * num;
	}

	private static Vector3 AxisDir(Axis a)
	{
		if (a == Axis.X)
		{
			return Vector3.right;
		}
		if (a == Axis.Y)
		{
			return Vector3.up;
		}
		return Vector3.forward;
	}

	private bool AxisParam(Axis a, Vector3 linePoint, out float t)
	{
		t = 0f;
		if (cam == null)
		{
			return false;
		}
		Ray ray = cam.ScreenPointToRay(TouchInputHelper.PrimaryPosition);
		Vector3 d = ray.direction.normalized;
		Vector3 axis = AxisDir(a);
		Vector3 vector = linePoint - ray.origin;
		float num = Vector3.Dot(axis, d);
		float num2 = 1f - num * num;
		if (num2 < 1e-06f)
		{
			return false;
		}
		t = (Vector3.Dot(vector, d) * num - Vector3.Dot(vector, axis)) / num2;
		return true;
	}

	public Axis HoverAxis()
	{
		if (cam == null || gizmoRoot == null || !gizmoRoot.gameObject.activeSelf || OverUI())
		{
			return Axis.None;
		}
		Vector2 mouse = TouchInputHelper.PrimaryPosition;
		Axis best = Axis.None;
		float bestDist = pickRadiusPx;
		for (int i = 0; i < 3; i++)
		{
			Vector3 dir = AxisDir((Axis)i);
			Vector3 a = gizmoOrigin + dir * gizmoScale;
			Vector3 b = gizmoOrigin + dir * (gizmoScale * 0.15f);
			Vector3 screenA = cam.WorldToScreenPoint(a);
			Vector3 screenB = cam.WorldToScreenPoint(b);
			if (screenA.z <= 0f || screenB.z <= 0f)
			{
				continue;
			}
			Vector2 d = new Vector2(screenB.x - screenA.x, screenB.y - screenA.y);
			if (64f > d.sqrMagnitude)
			{
				continue;
			}
			float dist = PointSegmentDistance(mouse, screenA, screenB);
			if (bestDist > dist)
			{
				bestDist = dist;
				best = (Axis)i;
			}
		}
		return best;
	}

	private static float PointSegmentDistance(Vector2 p, Vector2 a, Vector2 b)
	{
		Vector2 d = b - a;
		float len2 = d.x * d.x + d.y * d.y;
		if (len2 < 1e-06f)
		{
			return (p - a).magnitude;
		}
		float t = ((p.x - a.x) * d.x + (p.y - a.y) * d.y) / len2;
		t = Mathf.Clamp01(t);
		Vector2 closest = a + d * t;
		return (p - closest).magnitude;
	}

	private Vector3 ScreenToWorld(Vector3 screenPos, float depth)
	{
		return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
	}

	private static bool OverUI()
	{
		return TouchInputHelper.PointerOverUI;
	}

	private void BuildArrows()
	{
		GameObject gameObject = new GameObject("GizmoArrows");
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		gizmoRoot = gameObject.transform;
		Mesh coneMesh = ConeMesh(18);
		for (int i = 0; i < 3; i++)
		{
			Axis axis = (Axis)i;
			GameObject axisObject = new GameObject("Axis_" + axis.ToString());
			axisObject.transform.SetParent(gizmoRoot, worldPositionStays: false);
			axisObject.transform.localRotation = Quaternion.FromToRotation(Vector3.up, AxisDir(axis));
			axisRoot[i] = axisObject.transform;
			axisMat[i] = MakeMaterial(AxisColor[i]);
			GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			shaft.name = "Shaft";
			StripCollider(shaft);
			shaft.transform.SetParent(axisObject.transform, worldPositionStays: false);
			shaft.transform.localScale = new Vector3(0.024f, 0.315f, 0.024f);
			shaft.transform.localPosition = new Vector3(0f, 0.465f, 0f);
			Dress(shaft, axisMat[i]);
			GameObject head = new GameObject("Head", new Type[2]
			{
				typeof(MeshFilter),
				typeof(MeshRenderer)
			});
			head.transform.SetParent(axisObject.transform, worldPositionStays: false);
			head.GetComponent<MeshFilter>().sharedMesh = coneMesh;
			head.transform.localScale = new Vector3(0.055f, 0.22f, 0.055f);
			head.transform.localPosition = new Vector3(0f, 0.78f, 0f);
			Dress(head, axisMat[i]);
		}
		ShowArrows(false);
	}

	private static void StripCollider(GameObject go)
	{
		Collider component = go.GetComponent<Collider>();
		if (component != null)
		{
			if (Application.isPlaying)
			{
				Object.Destroy(component);
			}
			else
			{
				Object.DestroyImmediate(component);
			}
		}
	}

	private static void Dress(GameObject go, Material m)
	{
		Renderer component = go.GetComponent<Renderer>();
		if (component != null)
		{
			component.sharedMaterial = m;
			component.shadowCastingMode = ShadowCastingMode.Off;
			component.receiveShadows = false;
		}
	}

	private static Material MakeMaterial(Color c)
	{
		Shader shader = Shader.Find("Unlit/Color");
		if (shader == null)
		{
			shader = Shader.Find("Standard");
		}
		Material material = new Material(shader);
		if (material.HasProperty("_Color"))
		{
			material.color = c;
		}
		if (shader != null && shader.name == "Unlit/Color")
		{
			SectionView.MarkUnlit(material);
		}
		return material;
	}

	private static Mesh ConeMesh(int seg)
	{
		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();
		for (int i = 0; i < seg; i++)
		{
			float a = (float)i / seg * Mathf.PI * 2f;
			float b = (float)(i + 1) / seg * Mathf.PI * 2f;
			int n = verts.Count;
			verts.Add(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
			verts.Add(new Vector3(0f, 1f, 0f));
			verts.Add(new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)));
			tris.Add(n);
			tris.Add(n + 1);
			tris.Add(n + 2);
			int m = verts.Count;
			verts.Add(Vector3.zero);
			verts.Add(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
			verts.Add(new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)));
			tris.Add(m);
			tris.Add(m + 1);
			tris.Add(m + 2);
		}
		Mesh mesh = new Mesh();
		mesh.name = "GizmoCone";
		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	private void UpdateArrowTransform(ArmorPlate anchor)
	{
		if (gizmoRoot == null)
		{
			return;
		}
		Vector3 pos = anchor.transform.position;
		float radius = ObjectRadius(anchor);
		Vector3 toCam = cam.transform.position - pos;
		float dist = toCam.magnitude;
		Vector3 origin = pos;
		if (dist >= 0.0001f)
		{
			float pull = Mathf.Min(radius + 0.02f, dist - cam.nearClipPlane * 4f);
			if (pull > 0f)
			{
				origin = pos + toCam / dist * pull;
			}
		}
		gizmoOrigin = origin;
		gizmoScale = WorldPerPixel(gizmoOrigin) * pixelLength;
		gizmoRoot.position = gizmoOrigin;
		gizmoRoot.rotation = Quaternion.identity;
		gizmoRoot.localScale = Vector3.one * gizmoScale;
		ShowArrows(true);
	}

	private Vector3 PullTowardCamera(Vector3 point, float radius)
	{
		Vector3 toCam = cam.transform.position - point;
		float dist = toCam.magnitude;
		if (dist >= 0.0001f)
		{
			float pull = Mathf.Min(radius + 0.02f, dist - cam.nearClipPlane * 4f);
			if (pull > 0f)
			{
				return point + toCam / dist * pull;
			}
		}
		return point;
	}

	private static float ObjectRadius(ArmorPlate p)
	{
		Renderer[] renderers = p.GetComponentsInChildren<Renderer>();
		if (renderers == null || renderers.Length == 0)
		{
			return 0f;
		}
		Bounds bounds = renderers[0].bounds;
		for (int i = 1; i < renderers.Length; i++)
		{
			Bounds bounds2 = renderers[i].bounds;
			Vector3 min = Vector3.Min(bounds.min, bounds2.min);
			Vector3 max = Vector3.Max(bounds.max, bounds2.max);
			Vector3 size = max - min;
			bounds = new Bounds(min + size * 0.5f, size);
		}
		return bounds.extents.magnitude;
	}

	private float WorldPerPixel(Vector3 worldPoint)
	{
		int num = Mathf.Max(Screen.height, 1);
		if (cam.orthographic)
		{
			return cam.orthographicSize * 2f / num;
		}
		Vector3 position = cam.transform.position;
		Vector3 forward = cam.transform.forward;
		float dist = Mathf.Max(cam.nearClipPlane, Vector3.Dot(worldPoint - position, forward));
		return Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * (2f * dist) / num;
	}

	private void ShowArrows(bool on)
	{
		if (gizmoRoot != null && gizmoRoot.gameObject.activeSelf != on)
		{
			gizmoRoot.gameObject.SetActive(on);
		}
	}

	private void Paint()
	{
		Axis axis = (dragAxis != Axis.None) ? dragAxis : (modal ? modalAxis : HoverAxis());
		for (int i = 0; i < 3; i++)
		{
			Material material = axisMat[i];
			if (material == null)
			{
				continue;
			}
			Color color = (i == (int)axis) ? HotColor : AxisColor[i];
			if (material.HasProperty("_Color"))
			{
				material.color = color;
			}
		}
	}

	private string HudLine(Axis a, Vector3 off, bool numeric, bool hint)
	{
		float num;
		string axisName;
		if (a == Axis.None)
		{
			axisName = Localization.T("gizmo.free");
			num = SceneScale.ToM(off.magnitude);
		}
		else
		{
			axisName = a.ToString();
			num = SceneScale.ToM(Vector3.Dot(off, AxisDir(a)));
		}
		string text = string.Concat(Localization.T("gizmo.move"), ": ", axisName, "   ", num.ToString("+0.000;-0.000;0.000"), " ", Localization.T("unit.m"));
		if (numeric)
		{
			text += "  ←";
		}
		if (hint)
		{
			text = text + "\n" + Localization.T("gizmo.hint", new object[4]
			{
				ControlBindings.KeyName(ControlBindings.Key(ControlAction.GizmoAxisX)),
				ControlBindings.KeyName(ControlBindings.Key(ControlAction.GizmoAxisY)),
				ControlBindings.KeyName(ControlBindings.Key(ControlAction.GizmoAxisZ)),
				ControlBindings.ModifierName(ControlBindings.Modifier(ControlModifier.GizmoSnap))
			});
		}
		return text;
	}

	private void ShowHud(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			if (hud != null)
			{
				hud.gameObject.SetActive(false);
			}
			return;
		}
		if (hud == null)
		{
			hud = BuildHud();
		}
		if (hud == null)
		{
			return;
		}
		hud.gameObject.SetActive(true);
		hud.text = text;
	}

	private static TMP_Text BuildHud()
	{
		Canvas canvas = Object.FindFirstObjectByType<Canvas>();
		if (canvas == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject("GizmoHud", new Type[1] { typeof(TextMeshProUGUI) });
		gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
		text.fontSize = 13f;
		text.alignment = TextAlignmentOptions.TopLeft;
		text.color = new Color(0.95f, 0.95f, 0.98f, 1f);
		text.textWrappingMode = TextWrappingModes.Normal;
		text.raycastTarget = false;
		RectTransform rectTransform = text.rectTransform;
		rectTransform.anchorMin = new Vector2(0f, 1f);
		rectTransform.anchorMax = new Vector2(0f, 1f);
		rectTransform.pivot = new Vector2(0f, 1f);
		rectTransform.sizeDelta = new Vector2(620f, 62f);
		rectTransform.anchoredPosition = new Vector2(20f, -20f);
		return text;
	}
}
