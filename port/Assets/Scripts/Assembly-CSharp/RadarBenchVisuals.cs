using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public sealed class RadarBenchVisuals : MonoBehaviour
{
	private const int RingSegments = 72;

	private const int MeridianSegments = 36;

	private const int MarkerSegments = 24;

	private static readonly Color GuideColor;

	private static readonly Color IncidentColor;

	private static readonly Color XColor;

	private static readonly Color YColor;

	private static readonly Color ZColor;

	private readonly List<Mesh> ownedMeshes;

	private readonly List<Material> ownedMaterials;

	private Transform marker;

	private Mesh ringMesh;

	private Mesh meridianMesh;

	private Mesh wavesMesh;

	private Mesh axesMesh;

	private Mesh frontMesh;

	private bool built;

	private GameObject simpleWaves;

	private GameObject simpleFront;

	public Transform RadarMarkerTransform => marker;

	public float MarkerRadius { get; private set; }

	public RadarBenchVisuals()
	{
		ownedMeshes = new List<Mesh>();
		ownedMaterials = new List<Material>();
	}

	static RadarBenchVisuals()
	{
		GuideColor = new Color(0.31f, 0.35f, 0.39f, 1f);
		IncidentColor = new Color(0.22f, 0.36f, 0.51f, 1f);
		XColor = new Color(0.61f, 0.19f, 0.19f, 1f);
		YColor = new Color(0.16f, 0.47f, 0.25f, 1f);
		ZColor = new Color(0.18f, 0.32f, 0.64f, 1f);
	}

	public void SetSimpleGuidesVisible(bool visible)
	{
		EnsureBuilt();
		simpleWaves.SetActive(visible);
		simpleFront.SetActive(visible);
	}

	public void Configure(Vector3 center, float rbs, float azDegrees, float elDegrees)
	{
		Quaternion quaternion = Quaternion.Euler(-elDegrees, azDegrees, 0f);
		Configure(center, rbs, quaternion * Vector3.forward, quaternion * Vector3.right, quaternion * Vector3.up);
	}

	public void Configure(Vector3 center, float rbs, Vector3 u, Vector3 h, Vector3 v)
	{
		if (rbs <= 0f || float.IsInfinity(rbs))
		{
			throw new ArgumentOutOfRangeException("rbs", rbs, "Radar bounding-sphere radius must be positive and finite.");
		}
		EnsureBuilt();
		MarkerRadius = rbs * 1.6f;
		u.Normalize();
		h.Normalize();
		v.Normalize();
		marker.position = center + u * MarkerRadius;
		marker.rotation = Quaternion.LookRotation(-u, v);
		marker.localScale = Vector3.one * rbs;
		BuildDirectionSphere(center, MarkerRadius, u, h);
		BuildIncomingArrows(center, rbs, u, h, v);
		List<Vector3> lines = new List<Vector3>(30);
		List<Color> colors = new List<Color>(30);
		AddAxis(lines, colors, center, Vector3.right, XColor, rbs);
		AddAxis(lines, colors, center, Vector3.up, YColor, rbs);
		AddAxis(lines, colors, center, Vector3.forward, ZColor, rbs);
		SetLines(axesMesh, lines, colors);
		BuildWavefront(center, rbs, u, h, v);
	}

	private void EnsureBuilt()
	{
		if (built)
		{
			return;
		}
		Material material = Resources.Load<Material>("RadarSurface");
		if (material == null)
		{
			throw new InvalidOperationException(Localization.T("radar.error.shader"));
		}
		Material material2 = new Material(material);
		material2.name = "Radar bench guides";
		Material material3 = Own(material2);
		material3.SetFloat("_ShowMap", 1f);
		Material material4 = new Material(material);
		material4.name = "Radar marker metal";
		Material material5 = Own(material4);
		material5.SetFloat("_ShowMap", 1f);
		Material material6 = new Material(material);
		material6.name = "Radar static wavefront";
		Material material7 = Own(material6);
		material7.SetFloat("_ShowMap", 1f);
		material7.SetFloat("_Alpha", 0.22f);
		material7.SetFloat("_ZWrite", 0f);
		material7.renderQueue = 3000;
		ringMesh = MakeMesh("Radar azimuth ring", "Radar Azimuth Ring", material3);
		meridianMesh = MakeMesh("Radar meridian", "Radar Meridian", material3);
		wavesMesh = MakeMesh("Radar incoming arrows", "Radar Incoming Waves", material3);
		axesMesh = MakeMesh("Radar model axes", "Radar Model Axes", material3);
		frontMesh = MakeMesh("Radar static wavefront", "Radar Wavefront", material7);
		simpleWaves = base.transform.Find("Radar Incoming Waves").gameObject;
		simpleFront = base.transform.Find("Radar Wavefront").gameObject;
		GameObject gameObject = new GameObject("Radar Marker");
		marker = gameObject.transform;
		marker.SetParent(base.transform, false);
		MakeMarkerPart("Antenna Cone", BuildCone(), material5);
		MakeMarkerPart("Antenna Disc", BuildDisc(), material5);
		built = true;
	}

	private Material Own(Material material)
	{
		ownedMaterials.Add(material);
		return material;
	}

	private Mesh MakeMesh(string meshName, string objectName, Material material)
	{
		Mesh mesh = new Mesh();
		mesh.name = meshName;
		mesh.indexFormat = IndexFormat.UInt16;
		mesh.MarkDynamic();
		ownedMeshes.Add(mesh);
		GameObject gameObject = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
		gameObject.transform.SetParent(base.transform, false);
		gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
		return mesh;
	}

	private void MakeMarkerPart(string name, Mesh mesh, Material material)
	{
		ownedMeshes.Add(mesh);
		GameObject gameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
		gameObject.transform.SetParent(marker, false);
		gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
	}

	private void BuildDirectionSphere(Vector3 center, float markerRadius, Vector3 u, Vector3 h)
	{
		List<Vector3> lines = new List<Vector3>(RingSegments * 2);
		List<Color> colors = new List<Color>(RingSegments * 2);
		float num = Mathf.Sqrt(Mathf.Max(0f, 1f - u.y * u.y)) * markerRadius;
		float num2 = (center + u * markerRadius).y - center.y;
		for (int i = 0; i < RingSegments; i++)
		{
			float num3 = Mathf.PI * 2f * i / (float)RingSegments;
			float num4 = Mathf.PI * 2f * (i + 1) / (float)RingSegments;
			Vector3 vector = center + new Vector3(num * Mathf.Cos(num3), num2, num * Mathf.Sin(num3));
			Vector3 vector2 = center + new Vector3(num * Mathf.Cos(num4), num2, num * Mathf.Sin(num4));
			AddLine(lines, colors, vector, vector2, GuideColor);
		}
		SetLines(ringMesh, lines, colors);
		List<Vector3> lines2 = new List<Vector3>(MeridianSegments * 2);
		List<Color> colors2 = new List<Color>(MeridianSegments * 2);
		Vector3 vector3 = new Vector3(0f - h.z, 0f, h.x);
		for (int j = 0; j < MeridianSegments; j++)
		{
			float num5 = Mathf.PI * j / (float)MeridianSegments - Mathf.PI / 2f;
			float num6 = Mathf.PI * (j + 1) / (float)MeridianSegments - Mathf.PI / 2f;
			Vector3 vector4 = center + markerRadius * (Vector3.up * Mathf.Cos(num5) + vector3 * Mathf.Sin(num5));
			Vector3 vector5 = center + markerRadius * (Vector3.up * Mathf.Cos(num6) + vector3 * Mathf.Sin(num6));
			AddLine(lines2, colors2, vector4, vector5, GuideColor);
		}
		SetLines(meridianMesh, lines2, colors2);
	}

	private void BuildIncomingArrows(Vector3 center, float rbs, Vector3 u, Vector3 h, Vector3 v)
	{
		List<Vector3> lines = new List<Vector3>(50);
		List<Color> colors = new List<Color>(50);
		Vector3[] array = new Vector3[5]
		{
			Vector3.zero,
			h * (rbs * 0.5f),
			h * (rbs * -0.5f),
			v * (rbs * 0.5f),
			v * (rbs * -0.5f)
		};
		Vector3 vector = center + u * (rbs * 1.6f);
		Vector3 vector2 = center + u * (rbs * 0.38f);
		for (int i = 0; i < array.Length; i++)
		{
			AddArrow(lines, colors, vector + array[i], vector2 + array[i], h, v, rbs * 0.16f, rbs * 0.065f, IncidentColor);
		}
		SetLines(wavesMesh, lines, colors);
	}

	private void BuildModelAxes(Vector3 center, float rbs)
	{
		List<Vector3> lines = new List<Vector3>(30);
		List<Color> colors = new List<Color>(30);
		AddAxis(lines, colors, center, Vector3.right, XColor, rbs);
		AddAxis(lines, colors, center, Vector3.up, YColor, rbs);
		AddAxis(lines, colors, center, Vector3.forward, ZColor, rbs);
		SetLines(axesMesh, lines, colors);
	}

	private static void AddAxis(List<Vector3> lines, List<Color> colors, Vector3 center, Vector3 direction, Color color, float rbs)
	{
		Vector3 vector = (Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.9f) ? Vector3.forward : Vector3.up;
		Vector3 vector2 = Vector3.Cross(direction, vector);
		vector2.Normalize();
		Vector3 vector3 = Vector3.Cross(direction, vector2);
		AddArrow(lines, colors, center, center + direction * (rbs * 0.6f), vector2, vector3, rbs * 0.12f, rbs * 0.045f, color);
	}

	private void BuildWavefront(Vector3 center, float rbs, Vector3 u, Vector3 h, Vector3 v)
	{
		Vector3 vector = center + u * (rbs * 1.3f);
		float num = rbs * 1.1f;
		Vector3[] array = new Vector3[4]
		{
			vector - h * num - v * num,
			vector + h * num - v * num,
			vector + h * num + v * num,
			vector - h * num + v * num
		};
		frontMesh.Clear();
		frontMesh.vertices = array;
		Color[] array2 = new Color[4] { IncidentColor, IncidentColor, IncidentColor, IncidentColor };
		frontMesh.colors = array2;
		Vector3[] array3 = new Vector3[4] { u, u, u, u };
		frontMesh.normals = array3;
		frontMesh.triangles = new int[6] { 0, 1, 2, 0, 2, 3 };
		frontMesh.RecalculateBounds();
	}

	private static void AddArrow(List<Vector3> lines, List<Color> colors, Vector3 origin, Vector3 tip, Vector3 side1, Vector3 side2, float headLength, float headWidth, Color color)
	{
		Vector3 vector = tip - origin;
		vector.Normalize();
		vector *= headLength;
		AddLine(lines, colors, origin, tip, color);
		AddLine(lines, colors, tip, tip + vector + side1 * headWidth, color);
		AddLine(lines, colors, tip, tip + vector - side1 * headWidth, color);
		AddLine(lines, colors, tip, tip + vector + side2 * headWidth, color);
		AddLine(lines, colors, tip, tip + vector - side2 * headWidth, color);
	}

	private static void AddLine(List<Vector3> lines, List<Color> colors, Vector3 a, Vector3 b, Color color)
	{
		lines.Add(a);
		lines.Add(b);
		colors.Add(color);
		colors.Add(color);
	}

	private static void SetLines(Mesh mesh, List<Vector3> vertices, List<Color> colors)
	{
		mesh.Clear();
		mesh.SetVertices(vertices);
		mesh.SetColors(colors);
		int[] array = new int[vertices.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = i;
		}
		mesh.SetIndices(array, MeshTopology.Lines, 0);
		mesh.RecalculateBounds();
	}

	private static Mesh BuildCone()
	{
		Vector3[] array = new Vector3[MarkerSegments + 2];
		Color[] array2 = new Color[array.Length];
		int[] array3 = new int[MarkerSegments * 3];
		array[0] = new Vector3(0f, 0f, 0.22f);
		array2[0] = GuideColor;
		for (int i = 0; i <= MarkerSegments; i++)
		{
			float num = Mathf.PI * 2f * i / (float)MarkerSegments;
			array[i + 1] = new Vector3(Mathf.Sin(num) * 0.075f, Mathf.Cos(num) * 0.075f, -0.06f);
			array2[i + 1] = GuideColor;
			if (i < MarkerSegments)
			{
				array3[i * 3] = 0;
				array3[i * 3 + 1] = i + 1;
				array3[i * 3 + 2] = i + 2;
			}
		}
		Mesh mesh = new Mesh();
		mesh.name = "Radar antenna cone";
		mesh.vertices = array;
		mesh.colors = array2;
		mesh.triangles = array3;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	private static Mesh BuildDisc()
	{
		Vector3[] array = new Vector3[MarkerSegments + 2];
		Color[] array2 = new Color[array.Length];
		int[] array3 = new int[MarkerSegments * 3];
		array[0] = new Vector3(0f, 0f, -0.08f);
		array2[0] = GuideColor;
		for (int i = 0; i <= MarkerSegments; i++)
		{
			float num = Mathf.PI * 2f * i / (float)MarkerSegments;
			array[i + 1] = new Vector3(Mathf.Sin(num) * 0.15f, Mathf.Cos(num) * 0.15f, -0.08f);
			array2[i + 1] = GuideColor;
			if (i < MarkerSegments)
			{
				array3[i * 3] = 0;
				array3[i * 3 + 1] = i + 1;
				array3[i * 3 + 2] = i + 2;
			}
		}
		Mesh mesh = new Mesh();
		mesh.name = "Radar antenna disc";
		mesh.vertices = array;
		mesh.colors = array2;
		mesh.triangles = array3;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	private void OnDestroy()
	{
		foreach (Mesh item in ownedMeshes)
		{
			Release(item);
		}
		foreach (Material item in ownedMaterials)
		{
			Release(item);
		}
	}

	private static void Release(UnityEngine.Object resource)
	{
		if (resource == null)
		{
			return;
		}
		if (Application.isPlaying)
		{
			Object.Destroy(resource);
		}
		else
		{
			Object.DestroyImmediate(resource);
		}
	}
}
