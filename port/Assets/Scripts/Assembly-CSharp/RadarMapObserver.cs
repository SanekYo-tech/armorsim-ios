using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class RadarMapObserver : IDisposable
{
	public const int InsetLayer = 29;

	public const int InsetSize = 256;

	private static readonly Vector3 Stage = new Vector3(0f, -50000f, 0f);

	private static readonly Color Background = new Color(0.86f, 0.87f, 0.88f, 1f);

	private static readonly Color XColor = new Color(0.61f, 0.19f, 0.19f, 1f);

	private static readonly Color YColor = new Color(0.16f, 0.47f, 0.25f, 1f);

	private static readonly Color ZColor = new Color(0.18f, 0.32f, 0.64f, 1f);

	public bool Open;

	public int PlaneIndex;

	public int RadarIndex;

	public Action<Rect, RadarMapSession, RadarMapObserver> ExtraSection;

	public float ExtraHeight;

	private GameObject root;

	private GameObject model;

	private GameObject axes;

	private Camera camera;

	private RenderTexture texture;

	private Material modelMaterial;

	private Material axesMaterial;

	private Mesh modelMesh;

	private Mesh axesMesh;

	private string meshKey;

	private float modelRadius;

	private GUIStyle title;

	private GUIStyle truth;

	private GUIStyle label;

	private GUIStyle value;

	private GUIStyle small;

	private GUIStyle button;

	private GUIStyle aspectStyle;

	public RadarMapObserver()
	{
		modelRadius = 1f;
	}

	public RadarMapPose LastPose { get; private set; }

	public RadarAspect LastAspect { get; private set; }

	public Vector3 LastCameraForward { get; private set; }

	public Quaternion LastModelRotation { get; private set; }

	public int InsetRenders { get; private set; }

	public Texture InsetTexture => texture;

	public Camera InsetCamera => camera;

	private static string L(string key)
	{
		return RadarScenarioText.Get(key);
	}

	private static string F(double v, string format)
	{
		return v.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
	}

	private static string Deg(double radians)
	{
		return (radians * 57.29577951308232).ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "°";
	}

	private void EnsureStyles()
	{
		if (title != null)
		{
			return;
		}
		title = new GUIStyle(GUI.skin.label);
		title.fontSize = 24;
		title.fontStyle = FontStyle.Bold;
		title.normal.textColor = new Color(0.13f, 0.16f, 0.18f, 1f);
		truth = new GUIStyle(GUI.skin.label);
		truth.fontSize = 19;
		truth.fontStyle = FontStyle.Bold;
		truth.normal.textColor = new Color(0.62f, 0.16f, 0.1f, 1f);
		label = new GUIStyle(GUI.skin.label);
		label.fontSize = 17;
		label.wordWrap = false;
		label.normal.textColor = new Color(0.2f, 0.24f, 0.26f, 1f);
		value = new GUIStyle(label);
		value.fontStyle = FontStyle.Bold;
		value.alignment = TextAnchor.UpperRight;
		value.normal.textColor = new Color(0.08f, 0.1f, 0.12f, 1f);
		small = new GUIStyle(label);
		small.fontSize = 15;
		small.wordWrap = true;
		button = new GUIStyle(GUI.skin.button);
		button.fontSize = 17;
	}

	public void Draw(Rect area, RadarMapSession session)
	{
		if (!Open || session == null)
		{
			return;
		}
		EnsureStyles();
		GUI.color = new Color(1f, 1f, 1f, 1f);
		RadarScenarioMapDraw.Fill(area, new Color(0.93f, 0.94f, 0.94f, 0.98f));
		RadarScenarioMapDraw.Outline(area, new Color(0.62f, 0.16f, 0.1f, 1f), 3f);
		float num = area.x + 16f;
		float num2 = area.y + 10f;
		float num3 = area.width - 32f;
		GUI.Label(new Rect(num, num2, num3, 30f), L("map.obs.title"), title);
		num2 += 32f;
		GUI.Label(new Rect(num, num2, num3, 26f), L("map.obs.truth"), truth);
		num2 += 32f;
		if (session.Planes.Count == 0 || session.Radars.Count == 0)
		{
			return;
		}
		if (PlaneIndex < 0)
		{
			PlaneIndex = 0;
		}
		else if (PlaneIndex > session.Planes.Count - 1)
		{
			PlaneIndex = session.Planes.Count - 1;
		}
		if (RadarIndex < 0)
		{
			RadarIndex = 0;
		}
		else if (RadarIndex > session.Radars.Count - 1)
		{
			RadarIndex = session.Radars.Count - 1;
		}
		RadarMapSession.Plane plane = session.Planes[PlaneIndex];
		Selector(new Rect(num, num2, num3, 34f), L("map.obs.aircraft") + ": " + plane.Source.Name, ref PlaneIndex, session.Planes.Count);
		num2 += 38f;
		RadarMapSession.Radar radar = session.Radars[RadarIndex];
		Selector(new Rect(num, num2, num3, 34f), L("map.obs.radar") + ": " + radar.Source.Id.ToString() + " · " + radar.Spec.Id, ref RadarIndex, session.Radars.Count);
		num2 += 42f;
		RadarMapPose radarMapPose = session.Pose(PlaneIndex);
		RadarAspect radarAspect = session.Aspect(PlaneIndex, RadarIndex);
		float num4 = num2;
		float num5 = area.x + area.width - 16f - 256f;
		GUI.Label(new Rect(num5, num2, 256f, 24f), L("map.obs.view"), label);
		float num6 = num2 + 26f;
		RadarScenarioMapDraw.Fill(new Rect(num5, num6, 256f, 256f), Background);
		if (texture != null)
		{
			GUI.DrawTexture(new Rect(num5, num6, 256f, 256f), texture, ScaleMode.StretchToFill, alphaBlend: false);
		}
		RadarScenarioMapDraw.Outline(new Rect(num5, num6, 256f, 256f), new Color(0.35f, 0.38f, 0.4f, 1f), 1f);
		GUI.Label(new Rect(num5, num6 + 256f + 4f, 256f, 70f), L("map.obs.axes"), small);
		float num7 = area.width - 304f;
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		list.Add(Row("map.obs.position", F(radarMapPose.Position.x / 1000.0, "F2") + " / " + F(radarMapPose.Position.y / 1000.0, "F2") + " " + L("map.phys.km")));
		list.Add(Row("map.obs.altitude", F(radarMapPose.Position.z, "F0") + " " + L("map.phys.m")));
		list.Add(Row("map.obs.speed", F(radarMapPose.Speed, "F0") + " " + L("map.phys.mps")));
		list.Add(Row("map.obs.heading", Deg(radarMapPose.Heading)));
		list.Add(Row("map.obs.pitch", Deg(radarMapPose.Pitch)));
		list.Add(Row("map.obs.bank", Deg(radarMapPose.Bank)));
		list.Add(Row("map.obs.load", F(1.0 / Math.Cos(radarMapPose.Bank), "F2")));
		list.Add(Row("map.obs.ground", F(radarAspect.GroundRange / 1000.0, "F2") + " " + L("map.phys.km")));
		list.Add(Row("map.obs.slant", F(radarAspect.SlantRange / 1000.0, "F3") + " " + L("map.phys.km")));
		list.Add(Row("map.obs.elevRadar", F(radarAspect.ElevationAtRadar * 57.29577951308232, "F3") + "°"));
		list.Add(Row("map.obs.elevTarget", F(radarAspect.ElevationAtTarget * 57.29577951308232, "F3") + "°"));
		list.Add(Row("map.obs.bearing", Deg(radarAspect.BearingToRadar)));
		list.Add(Row("map.obs.horizon", F((radarAspect.Horizon - radarAspect.GroundRange) / 1000.0, "F1") + " " + L("map.phys.km")));
		foreach (KeyValuePair<string, string> item in list)
		{
			GUI.Label(new Rect(num, num2, num7 - 130f, 23f), item.Key, label);
			GUI.Label(new Rect(num + num7 - 130f, num2, 130f, 23f), item.Value, value);
			num2 += 23f;
		}
		num2 += 4f;
		GUI.Label(new Rect(num, num2, num7, 24f), L("map.obs.aspect"), label);
		num2 += 24f;
		if (aspectStyle == null)
		{
			aspectStyle = new GUIStyle(value);
			aspectStyle.alignment = TextAnchor.UpperLeft;
			aspectStyle.fontSize = 19;
		}
		GUI.Label(new Rect(num, num2, num7 + 40f, 26f), "az " + Deg(radarAspect.Azimuth) + "  el " + Deg(radarAspect.Elevation) + "  ξ " + Deg(radarAspect.Xi), aspectStyle);
		num2 += 30f;
		num2 = Mathf.Max(num2, num4 + 26f + 256f + 74f);
		if (ExtraSection != null && ExtraHeight > 0f)
		{
			float num8 = area.y + area.height - num2 - 8f;
			if (num8 > ExtraHeight)
			{
				num8 = ExtraHeight;
			}
			ExtraSection(new Rect(area.x + 16f, num2, area.width - 32f, num8), session, this);
		}
	}

	private KeyValuePair<string, string> Row(string key, string text)
	{
		return new KeyValuePair<string, string>(L(key), text);
	}

	private void Selector(Rect rect, string text, ref int index, int count)
	{
		if (GUI.Button(new Rect(rect.x, rect.y, 40f, rect.height), "<", button))
		{
			index = (index - 1 + count) % count;
		}
		GUI.Label(new Rect(rect.x + 48f, rect.y + 4f, rect.width - 96f, rect.height), text, label);
		if (GUI.Button(new Rect(rect.x + rect.width - 40f, rect.y, 40f, rect.height), ">", button))
		{
			index = (index + 1) % count;
		}
	}

	public void RenderInset(RadarMapSession session)
	{
		if (!Open || session == null)
		{
			return;
		}
		if (session.Planes.Count == 0 || session.Radars.Count == 0)
		{
			return;
		}
		if (PlaneIndex < 0)
		{
			PlaneIndex = 0;
		}
		else if (PlaneIndex > session.Planes.Count - 1)
		{
			PlaneIndex = session.Planes.Count - 1;
		}
		if (RadarIndex < 0)
		{
			RadarIndex = 0;
		}
		else if (RadarIndex > session.Radars.Count - 1)
		{
			RadarIndex = session.Radars.Count - 1;
		}
		RadarMapSession.Plane plane = session.Planes[PlaneIndex];
		RadarMapPose radarMapPose = session.Pose(PlaneIndex);
		RadarAspect radarAspect = session.Aspect(PlaneIndex, RadarIndex);
		LastPose = radarMapPose;
		LastAspect = radarAspect;
		EnsureStage();
		if (meshKey != plane.ModelKey)
		{
			SetModel(plane.ModelKey, plane.Model);
		}
		RadarMapFrames.Axes(radarMapPose.Heading, radarMapPose.Pitch, radarMapPose.Bank, out double3 right, out double3 up, out double3 nose);
		Vector3 forward = Unity(nose);
		Vector3 upwards = Unity(up);
		Quaternion quaternion = Quaternion.LookRotation(forward, upwards);
		model.transform.SetPositionAndRotation(Stage, quaternion);
		LastModelRotation = quaternion;
		Vector3 vector = new Vector3((float)(Math.Sin(radarAspect.BearingToRadar) * Math.Cos(radarAspect.ElevationAtTarget)), (float)Math.Sin(radarAspect.ElevationAtTarget), (float)(Math.Cos(radarAspect.BearingToRadar) * Math.Cos(radarAspect.ElevationAtTarget))).normalized;
		Vector3 vector2 = Vector3.up;
		if (Mathf.Abs(Vector3.Dot(vector, vector2)) > 0.999f)
		{
			vector2 = forward;
		}
		camera.transform.position = Stage + vector * (modelRadius * 3f);
		camera.transform.rotation = Quaternion.LookRotation(-vector, vector2);
		camera.orthographicSize = modelRadius * 1.1f;
		camera.nearClipPlane = modelRadius * 0.01f;
		camera.farClipPlane = modelRadius * 10f;
		LastCameraForward = camera.transform.forward;
		camera.Render();
		InsetRenders++;
	}

	private static Vector3 Unity(double3 enu)
	{
		return new Vector3((float)enu.x, (float)enu.z, (float)enu.y);
	}

	private void EnsureStage()
	{
		if (root != null)
		{
			return;
		}
		root = new GameObject("RadarMapObserverStage");
		root.hideFlags = HideFlags.DontSave;
		texture = new RenderTexture(InsetSize, InsetSize, 24, RenderTextureFormat.ARGB32);
		texture.name = "RadarMapObserverInset";
		texture.antiAliasing = 4;
		texture.Create();
		GameObject gameObject = new GameObject("RadarMapObserverCamera");
		gameObject.transform.SetParent(root.transform, worldPositionStays: false);
		camera = gameObject.AddComponent<Camera>();
		camera.enabled = false;
		camera.orthographic = true;
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = Background;
		camera.cullingMask = 1 << InsetLayer;
		camera.targetTexture = texture;
		camera.allowHDR = false;
		camera.allowMSAA = true;
		Shader shader = Resources.Load<Shader>("RadarSurface");
		if (shader == null)
		{
			throw new InvalidOperationException(Localization.T("radar.error.shader"));
		}
		modelMaterial = new Material(shader);
		modelMaterial.SetFloat("_ShowMap", 0f);
		axesMaterial = new Material(shader);
		axesMaterial.SetFloat("_ShowMap", 1f);
		model = new GameObject("RadarMapObserverModel", typeof(MeshFilter), typeof(MeshRenderer));
		model.layer = InsetLayer;
		model.transform.SetParent(root.transform, worldPositionStays: false);
		model.GetComponent<MeshRenderer>().sharedMaterial = modelMaterial;
		axes = new GameObject("RadarMapObserverAxes", typeof(MeshFilter), typeof(MeshRenderer));
		axes.layer = InsetLayer;
		axes.transform.SetParent(model.transform, worldPositionStays: false);
		axes.GetComponent<MeshRenderer>().sharedMaterial = axesMaterial;
	}

	private void SetModel(string key, RadarModel source)
	{
		if (modelMesh != null)
		{
			UnityEngine.Object.Destroy(modelMesh);
		}
		if (axesMesh != null)
		{
			UnityEngine.Object.Destroy(axesMesh);
		}
		meshKey = key;
		modelRadius = Mathf.Max((float)source.BoundingSphereRadius, 0.01f);
		Vector3[] array = new Vector3[source.Vertices.Length];
		Color[] array2 = new Color[array.Length];
		Color color = new Color(0.76f, 0.78f, 0.8f, 1f);
		for (int i = 0; i < array.Length; i++)
		{
			double3 b = source.Vertices[i];
			array[i] = new Vector3((float)(b.x - source.Center.x), (float)(b.y - source.Center.y), (float)(b.z - source.Center.z));
			array2[i] = color;
		}
		modelMesh = new Mesh();
		modelMesh.name = "Radar map observer model";
		modelMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		modelMesh.vertices = array;
		modelMesh.triangles = (int[])source.Triangles.Clone();
		modelMesh.colors = array2;
		modelMesh.RecalculateNormals();
		modelMesh.RecalculateBounds();
		model.GetComponent<MeshFilter>().sharedMesh = modelMesh;
		axesMesh = AxesMesh(modelRadius * 0.6f, modelRadius * 0.012f);
		axes.GetComponent<MeshFilter>().sharedMesh = axesMesh;
	}

	private static Mesh AxesMesh(float length, float thickness)
	{
		List<Vector3> list = new List<Vector3>();
		List<Color> list2 = new List<Color>();
		List<int> list3 = new List<int>();
		Arrow(Vector3.right, Vector3.up, XColor, length, thickness, list, list2, list3);
		Arrow(Vector3.up, Vector3.forward, YColor, length, thickness, list, list2, list3);
		Arrow(Vector3.forward, Vector3.up, ZColor, length, thickness, list, list2, list3);
		Mesh mesh = new Mesh();
		mesh.name = "Radar map observer axes";
		mesh.SetVertices(list);
		mesh.SetColors(list2);
		mesh.SetTriangles(list3, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	private static void Arrow(Vector3 axis, Vector3 side, Color color, float length, float thickness, List<Vector3> vertices, List<Color> colors, List<int> triangles)
	{
		Vector3 vector = Vector3.Cross(axis, side).normalized * thickness;
		Vector3 vector2 = side.normalized * thickness;
		Box(Vector3.zero, axis * (length * 0.82f), vector, vector2, color, vertices, colors, triangles);
		Vector3 vector3 = axis * (length * 0.82f);
		Vector3[] array = new Vector3[4];
		array[0] = vector3 + (vector + vector2) * (thickness * 3f);
		array[1] = vector3 + (vector - vector2) * (thickness * 3f);
		array[2] = vector3 - (vector + vector2) * (thickness * 3f);
		array[3] = vector3 + (vector2 - vector) * (thickness * 3f);
		int num = vertices.Count;
		for (int i = 0; i < array.Length; i++)
		{
			vertices.Add(array[i]);
			colors.Add(color);
		}
		vertices.Add(axis * length);
		colors.Add(color);
		for (int j = 0; j < 4; j++)
		{
			triangles.Add(num + j);
			triangles.Add(num + (j + 1) % 4);
			triangles.Add(num + 4);
		}
		triangles.Add(num);
		triangles.Add(num + 2);
		triangles.Add(num + 1);
		triangles.Add(num);
		triangles.Add(num + 3);
		triangles.Add(num + 2);
	}

	private static void Box(Vector3 from, Vector3 to, Vector3 a, Vector3 b, Color color, List<Vector3> vertices, List<Color> colors, List<int> triangles)
	{
		int num = vertices.Count;
		Vector3[] array = new Vector3[2] { from, to };
		for (int i = 0; i < array.Length; i++)
		{
			Vector3 vector = array[i];
			vertices.Add(vector + a + b);
			vertices.Add(vector + a - b);
			vertices.Add(vector - a - b);
			vertices.Add(vector - a + b);
			for (int j = 0; j < 4; j++)
			{
				colors.Add(color);
			}
		}
		for (int k = 0; k < 4; k++)
		{
			triangles.Add(num + k);
			triangles.Add(num + (k + 1) % 4);
			triangles.Add(num + 4 + (k + 1) % 4);
			triangles.Add(num + k);
			triangles.Add(num + 4 + (k + 1) % 4);
			triangles.Add(num + 4 + k);
		}
	}

	public void Dispose()
	{
		if (texture != null)
		{
			texture.Release();
			UnityEngine.Object.Destroy(texture);
		}
		if (modelMesh != null)
		{
			UnityEngine.Object.Destroy(modelMesh);
		}
		if (axesMesh != null)
		{
			UnityEngine.Object.Destroy(axesMesh);
		}
		if (modelMaterial != null)
		{
			UnityEngine.Object.Destroy(modelMaterial);
		}
		if (axesMaterial != null)
		{
			UnityEngine.Object.Destroy(axesMaterial);
		}
		if (root != null)
		{
			UnityEngine.Object.Destroy(root);
		}
		texture = null;
		root = null;
		meshKey = null;
	}
}
