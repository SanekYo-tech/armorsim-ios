using System;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class AeroCloudDisplay : MonoBehaviour
{
	public const int MaximumPuffs = 4096;

	private const int MaximumSampledPaths = 512;

	private const float MinimumSegmentSquared = 1E-08f;

	private readonly Vector3[] centers;

	private readonly Vector3[] tangents;

	private readonly float[] radii;

	private readonly Vector3[] quadVertices;

	private readonly Color32[] quadColors;

	private readonly Vector2[] quadUV;

	private readonly int[] triangles;

	private Mesh mesh;

	private Material material;

	private MeshRenderer cloudRenderer;

	private Camera view;

	private bool visible;

	private bool dirty;

	private int uploadedPuffs;

	private Vector3 previousCameraPosition;

	private Quaternion previousCameraRotation;

	private Bounds cloudBounds;

	public bool IsVisible => visible;

	public int PuffCount { get; private set; }

	public AeroCloudDisplay()
	{
		centers = new Vector3[MaximumPuffs];
		tangents = new Vector3[MaximumPuffs];
		radii = new float[MaximumPuffs];
		quadVertices = new Vector3[MaximumPuffs * 4];
		quadColors = new Color32[MaximumPuffs * 4];
		quadUV = new Vector2[MaximumPuffs * 4];
		triangles = new int[MaximumPuffs * 6];
	}

	public void SetVisible(bool value)
	{
		if (visible == value)
		{
			return;
		}
		visible = value;
		Clear();
	}

	public void Clear()
	{
		uploadedPuffs = 0;
		PuffCount = 0;
		dirty = false;
		if (cloudRenderer != null)
		{
			cloudRenderer.enabled = false;
		}
		if (mesh != null)
		{
			mesh.Clear(false);
		}
	}

	public void Publish(Vector3[] trailVertices, Color32[] lineColors, int count, int trailLength)
	{
		if (!visible)
		{
			return;
		}
		if (trailVertices == null || lineColors == null || count < 0 || trailLength < 2 || trailVertices.Length < (long)count * trailLength || lineColors.Length < (long)count * trailLength)
		{
			throw new ArgumentException("Invalid Aero cloud trajectory buffers.");
		}
		if (count == 0)
		{
			Clear();
			return;
		}
		EnsureMesh();
		PuffCount = 0;
		count = Mathf.Min(count, MaximumSampledPaths);
		int num = Mathf.Min(MaximumPuffs / count, 16);
		Vector3 min = Vector3.positiveInfinity;
		Vector3 max = Vector3.negativeInfinity;
		Vector3 vector = Vector3.positiveInfinity;
		for (int i = 0; i < count; i++)
		{
			int num2 = (int)((i * 2 + 1) * (long)count / (count * 2));
			int num3 = num2 * trailLength;
			float num4 = (float)num2 * 0.618034f + 0.37f;
			num4 -= Mathf.Floor(num4);
			num4 = Mathf.Clamp01(num4);
			for (int j = 0; j < num; j++)
			{
				float num5 = ((float)j + num4) * ((float)trailLength - 1f) / (float)num;
				int num6 = Mathf.Min((int)num5, trailLength - 2);
				Vector3 a = trailVertices[num3 + num6];
				Vector3 b = trailVertices[num3 + num6 + 1];
				Vector3 vector2 = b - a;
				if (vector2.sqrMagnitude > MinimumSegmentSquared && Finite(a) && Finite(vector2))
				{
					float num7 = num5 - num6;
					Vector3 vector3 = new Vector3(a.x + vector2.x * num7, a.y + vector2.y * num7, a.z + vector2.z * num7);
					if ((vector3 - vector).sqrMagnitude > 0.012f)
					{
						vector = vector3;
						float num8 = lineColors[num3 + num6].a / 255f;
						num7 = Mathf.Clamp01(num7);
						float num9 = ((lineColors[num3 + num6 + 1].a / 255f - num8) * num7 + num8) * 0.18f;
						if (1E-04f < num9)
						{
							int num10 = PuffCount;
							PuffCount = num10 + 1;
							centers[num10] = vector3;
							float magnitude = vector2.magnitude;
							if (magnitude <= 1E-05f)
							{
								tangents[num10] = Vector3.zero;
							}
							else
							{
								tangents[num10] = vector2 / magnitude;
							}
							radii[num10] = Mathf.Repeat((float)j * 0.5698403f + (float)num2 * 0.7548777f, 1f) * 0.045f + 0.22f;
							Color32 color = new Color(0.29f, 0.4f, 0.44f, num9);
							quadColors[num10 * 4] = color;
							quadColors[num10 * 4 + 1] = color;
							quadColors[num10 * 4 + 2] = color;
							quadColors[num10 * 4 + 3] = color;
							min = Vector3.Min(min, vector3);
							max = Vector3.Max(max, vector3);
						}
					}
				}
			}
		}
		if (PuffCount != 0)
		{
			cloudBounds = new Bounds((min + max) * 0.5f, (max - min + Vector3.one) * 0.5f);
			dirty = true;
			return;
		}
		Clear();
	}

	private void EnsureMesh()
	{
		if (mesh != null)
		{
			return;
		}
		Shader shader = Resources.Load<Shader>("AeroCloud");
		if (shader == null)
		{
			throw new AeroException("aero.error.shader", Array.Empty<object>());
		}
		material = new Material(shader);
		material.name = "Aero qualitative cloud";
		mesh = new Mesh();
		mesh.name = "Aero sampled cloud paths";
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.MarkDynamic();
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		cloudRenderer = gameObject.AddComponent<MeshRenderer>();
		cloudRenderer.sharedMaterial = material;
		cloudRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		cloudRenderer.receiveShadows = false;
		cloudRenderer.enabled = false;
		for (int i = 0; i < MaximumPuffs; i++)
		{
			quadUV[i * 4] = new Vector2(0f, 0f);
			quadUV[i * 4 + 1] = new Vector2(1f, 0f);
			quadUV[i * 4 + 2] = new Vector2(1f, 1f);
			quadUV[i * 4 + 3] = new Vector2(0f, 1f);
			int num = i * 6;
			int num2 = i * 4;
			triangles[num] = num2;
			triangles[num + 1] = num2 + 1;
			triangles[num + 2] = num2 + 2;
			triangles[num + 3] = num2;
			triangles[num + 4] = num2 + 2;
			triangles[num + 5] = num2 + 3;
		}
	}

	private void LateUpdate()
	{
		if (!visible || PuffCount == 0)
		{
			return;
		}
		if (mesh == null)
		{
			return;
		}
		if (view == null)
		{
			view = Object.FindFirstObjectByType<Camera>();
		}
		if (view == null)
		{
			return;
		}
		Transform transform = view.transform;
		if (!dirty)
		{
			Vector3 position = transform.position;
			if ((position - previousCameraPosition).sqrMagnitude < 1E-10f && 0.999999f < Quaternion.Dot(transform.rotation, previousCameraRotation))
			{
				return;
			}
		}
		Transform transform2 = base.transform;
		Vector3 vector = transform2.InverseTransformDirection(transform.forward);
		Vector3 vector2 = transform2.InverseTransformDirection(transform.up);
		for (int i = 0; i < PuffCount; i++)
		{
			Vector3 tangent = tangents[i];
			float num = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
			if (num >= Mathf.Epsilon)
			{
				float num2 = (tangent.x * vector.x + tangent.y * vector.y + tangent.z * vector.z) / num;
				tangent = new Vector3(tangent.x - vector.x * num2, tangent.y - vector.y * num2, tangent.z - vector.z * num2);
			}
			if (tangent.sqrMagnitude <= 1E-08f)
			{
				tangent = vector2;
			}
			else
			{
				float magnitude = tangent.magnitude;
				if (magnitude <= 1E-05f)
				{
					tangent = Vector3.zero;
				}
				else
				{
					tangent /= magnitude;
				}
			}
			Vector3 vector3 = Vector3.Cross(tangent, vector);
			float magnitude2 = vector3.magnitude;
			if (magnitude2 <= 1E-05f)
			{
				vector3 = Vector3.zero;
			}
			else
			{
				vector3 /= magnitude2;
			}
			float num3 = radii[i];
			Vector3 vector4 = vector3 * num3;
			Vector3 vector5 = tangent * (num3 * 1.35f);
			Vector3 vector6 = centers[i];
			quadVertices[i * 4] = vector6 - vector4 - vector5;
			quadVertices[i * 4 + 1] = vector6 + vector4 - vector5;
			quadVertices[i * 4 + 2] = vector6 + vector4 + vector5;
			quadVertices[i * 4 + 3] = vector6 - vector4 + vector5;
		}
		int num4 = PuffCount;
		int num5 = uploadedPuffs;
		if (num5 != num4)
		{
			mesh.Clear(false);
		}
		mesh.SetVertices(quadVertices, 0, num4 * 4, UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
		if (num5 != num4 || dirty)
		{
			mesh.SetColors(quadColors, 0, num4 * 4);
			mesh.SetUVs(0, quadUV, 0, num4 * 4);
			if (num5 != num4)
			{
				mesh.SetIndices(triangles, 0, num4 * 6, MeshTopology.Triangles, 0, false);
			}
			mesh.bounds = cloudBounds;
		}
		uploadedPuffs = num4;
		previousCameraPosition = transform.position;
		previousCameraRotation = transform.rotation;
		dirty = false;
		if (cloudRenderer != null)
		{
			cloudRenderer.enabled = true;
		}
	}

	private static bool Finite(Vector3 p)
	{
		return !float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z);
	}

	private void OnDestroy()
	{
		if (mesh != null)
		{
			Object.Destroy(mesh);
		}
		if (material != null)
		{
			Object.Destroy(material);
		}
	}
}
