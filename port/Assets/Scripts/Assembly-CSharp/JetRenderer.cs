using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class JetRenderer : MonoBehaviour
{
	public float jetTemperatureK;

	[Header("Геометрия")]
	public int radialDivisions;

	public float minVisualRadiusWU;

	private JetElement[] elements;

	private Vector3 origin;

	private Vector3 axis;

	private float breakupTimeS;

	private float dvMS;

	private float worldPerMeter;

	private int retiredFromTip;

	private bool active;

	private Mesh mesh;

	private MeshFilter mf;

	private MeshRenderer mr;

	private Vector3[] verts;

	private int[] tris;

	private int builtSegments;

	private int builtDiv;

	public float headLimitM;

	private float dispersionStartM;

	private float dispersionRampM;

	private float dispersionTargetRadiusM;

	private void Awake()
	{
		base.transform.SetParent(null, false);
		base.transform.position = Vector3.zero;
		base.transform.rotation = Quaternion.identity;
		base.transform.localScale = Vector3.one;
		mf = GetComponent<MeshFilter>();
		mr = GetComponent<MeshRenderer>();
		mesh = new Mesh();
		mesh.name = "JetTube";
		mesh.MarkDynamic();
		mf.sharedMesh = mesh;
		Shader shader = Shader.Find("Unlit/Color");
		if (shader == null)
		{
			shader = Shader.Find("Sprites/Default");
		}
		Material material = new Material(shader);
		material.color = BlackbodyColor(jetTemperatureK);
		mr.sharedMaterial = material;
		SectionView.MarkUnlit(mr.material);
		mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		mr.receiveShadows = false;
		if (mr != null)
		{
			mr.enabled = false;
		}
	}

	public void Configure(JetElement[] timeline, Vector3 detonationPoint, Vector3 direction, float tBreakS, float dv, float worldPerMeterScale)
	{
		elements = timeline;
		origin = detonationPoint;
		Vector3 vector = ((direction.magnitude > 1E-05f) ? (direction / direction.magnitude) : Vector3.zero);
		axis = vector;
		breakupTimeS = tBreakS;
		dvMS = dv;
		worldPerMeter = worldPerMeterScale;
		retiredFromTip = 0;
		active = timeline != null && timeline.Length > 0;
		if (mr != null)
		{
			mr.material.color = BlackbodyColor(jetTemperatureK);
			mr.enabled = false;
		}
	}

	public void RetireFromTip(int count)
	{
		retiredFromTip = Mathf.Min(retiredFromTip, count);
	}

	public void SetVisible(bool on)
	{
		if (mr != null)
		{
			mr.enabled = on && active;
		}
	}

	public void SetDispersion(float startM, float rampM, float targetRadiusM)
	{
		dispersionStartM = startM;
		dispersionRampM = Mathf.Max(rampM, 0.0001f);
		dispersionTargetRadiusM = Mathf.Max(targetRadiusM, 0f);
	}

	public void ClearDispersion()
	{
		dispersionStartM = -1f;
		dispersionTargetRadiusM = 0f;
	}

	public static float DispersedRadiusM(float ownRadiusM, float distM, float startM, float rampM, float targetRadiusM)
	{
		if (startM < 0f || distM >= targetRadiusM)
		{
			return ownRadiusM;
		}
		float num = Mathf.Clamp01((distM - startM) / Mathf.Max(rampM, 0.0001f));
		float num2 = Mathf.Clamp01((3f - 2f * num) * num * num);
		return ownRadiusM + (targetRadiusM - ownRadiusM) * num2;
	}

	private float DispersedRadiusM(float ownRadiusM, float distM)
	{
		return DispersedRadiusM(ownRadiusM, distM, dispersionStartM, dispersionRampM, dispersionTargetRadiusM);
	}

	public void UpdateAt(float t)
	{
		if (!active || t <= 0f)
		{
			if (mr != null)
			{
				mr.enabled = false;
			}
			return;
		}
		if (mr != null)
		{
			mr.enabled = active;
		}
		int num = Mathf.Clamp(retiredFromTip, 0, elements.Length);
		int num2 = elements.Length - num;
		if (num2 <= 0)
		{
			SetVisible(false);
			return;
		}
		EnsureBuffers(num2);
		Vector3 vector = ((Mathf.Abs(Vector3.Dot(axis, Vector3.right)) > 0.9f) ? Vector3.up : Vector3.right);
		Vector3 vector2 = Vector3.Cross(axis, vector).normalized;
		Vector3 vector3 = Vector3.Cross(vector2, axis).normalized;
		for (int i = 0; i < num2; i++)
		{
			JetElement jetElement = elements[num + i];
			float num3 = jetElement.velocity * t;
			if (headLimitM >= 0f && num3 > headLimitM)
			{
				num3 = headLimitM;
			}
			float num4 = num3 - Mathf.Min(t, breakupTimeS) * dvMS;
			float num5 = Mathf.Max(DispersedRadiusM(jetElement.radiusM, num3) * worldPerMeter, minVisualRadiusWU);
			Vector3 vector4 = origin + axis * (num3 * worldPerMeter);
			Vector3 vector5 = origin + axis * (num4 * worldPerMeter);
			for (int j = 0; j < radialDivisions; j++)
			{
				float f = (float)j / (float)radialDivisions * Mathf.PI;
				float num6 = Mathf.Cos(2f * f);
				float num7 = Mathf.Sin(2f * f);
				Vector3 vector6 = (vector2 * num6 + vector3 * num7) * num5;
				verts[i * radialDivisions * 2 + j] = vector5 + vector6;
				verts[i * radialDivisions * 2 + radialDivisions + j] = vector4 + vector6;
			}
		}
		mesh.Clear();
		mesh.vertices = verts;
		mesh.triangles = tris;
		mesh.RecalculateBounds();
	}

	private void EnsureBuffers(int segments)
	{
		int num = radialDivisions;
		if (builtSegments != segments || builtDiv != num)
		{
			verts = new Vector3[num * segments * 2];
			tris = new int[num * segments * 6];
			int num2 = 0;
			for (int i = 0; i < segments; i++)
			{
				for (int j = 0; j < num; j++)
				{
					int num3 = i * num * 2 + j;
					int num4 = num3 + num;
					int num5 = i * num * 2 + (j + 1) % num;
					int num6 = num5 + num;
					tris[num2] = num3;
					tris[num2 + 1] = num4;
					tris[num2 + 2] = num5;
					tris[num2 + 3] = num5;
					tris[num2 + 4] = num4;
					tris[num2 + 5] = num6;
					num2 += 6;
				}
			}
			builtSegments = segments;
			builtDiv = num;
		}
	}

	public static Color BlackbodyColor(float kelvin)
	{
		float num = Mathf.Clamp(kelvin, 1000f, 40000f) / 100f;
		float r = ((num <= 66f) ? 255f : (329.69873f * Mathf.Pow(num - 60f, -0.133204758f)));
		float g = ((num <= 66f) ? (99.4708023f * Mathf.Log(num) - 161.119568f) : (288.122162f * Mathf.Pow(num - 60f, -0.0755148456f)));
		float b = ((num >= 66f) ? 255f : ((num <= 19f) ? 0f : (138.517731f * Mathf.Log(num - 10f) - 305.0448f)));
		return new Color(Mathf.Clamp01(r / 255f), Mathf.Clamp01(g / 255f), Mathf.Clamp01(b / 255f), 1f);
	}
}
