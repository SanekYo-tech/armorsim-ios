using UnityEngine;
using UnityEngine.Rendering;

public class UltraBuiltGhost : MonoBehaviour
{
	public ContinuumViewer viewer;

	public UltraShotDriver driver;

	public bool show = true;

	[Tooltip("Цвет заливки и контура. Отличен и от стержня, и от преграды намеренно.")]
	public Color fillColor = new Color(0.35f, 0.8f, 1f, 0.16f);

	public Color outlineColor = new Color(0.45f, 0.88f, 1f, 1f);

	[Tooltip("Толщина линии контура в долях калибра.")]
	public float outlineWidthCalibres = 0.06f;

	public const string ShaderName = "ArmorSim/UltraGhost";

	private GameObject root;

	private ProjectedScenario built;

	private bool builtSection;

	private Material fillMat;

	private Material outlineMat;

	private bool shaderReported;

	public int GhostCount { get; private set; }

	private void OnDisable()
	{
		Clear();
		DestroyMat(ref fillMat);
		DestroyMat(ref outlineMat);
	}

	private static void DestroyMat(ref Material m)
	{
		if (m == null)
		{
			return;
		}
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(m);
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(m);
		}
		m = null;
	}

	private void Update()
	{
		if (viewer == null)
		{
			viewer = GetComponentInParent<ContinuumViewer>();
		}
		if (driver == null)
		{
			driver = Object.FindFirstObjectByType<UltraShotDriver>();
		}
		ProjectedScenario projectedScenario = null;
		if (show && driver != null && driver.Current != null && 0.0001f < driver.Current.maxTiltDeg)
		{
			projectedScenario = driver.Current;
		}
		bool section = viewer != null && viewer.sectionCut;
		if (projectedScenario == built && (projectedScenario == null || section == builtSection))
		{
			return;
		}
		Clear();
		built = projectedScenario;
		builtSection = section;
		if (projectedScenario != null)
		{
			BuildFor(projectedScenario, section);
		}
	}

	private void BuildFor(ProjectedScenario p, bool section)
	{
		ContinuumScenario s = p.scenario;
		if (s == null)
		{
			return;
		}
		float lineWidth = outlineWidthCalibres * s.rodDiameterM;
		if (lineWidth < 0.0001f)
		{
			lineWidth = 0.0001f;
		}
		float radiusM = s.outerRadiusCalibres * s.rodDiameterM;
		root = new GameObject("Призрак построенного");
		root.transform.SetParent(base.transform, false);
		EnsureMaterials();
		for (int i = 0; i < p.DiscCount; i++)
		{
			UltraProjection.ProjectedDisc disc = p.Disc(i);
			if (Mathf.Abs(disc.tiltDeg) <= 0.0001f)
			{
				continue;
			}
			float z0;
			float z1;
			if (!UltraGhostGeometry.TrySlabRange(s, i, out z0, out z1))
			{
				continue;
			}
			float rawM = disc.rawThicknessMM / 1000f;
			float zCentre = (z1 + z0) * 0.5f;
			float tiltDeg = Mathf.Abs(disc.tiltDeg) * ((disc.leanSign < 0f) ? 1f : (-1f));
			GameObject outline = new GameObject(string.Format("Контур слоя {0}", i + 1));
			outline.transform.SetParent(root.transform, false);
			LineRenderer line = outline.AddComponent<LineRenderer>();
			line.useWorldSpace = false;
			line.loop = false;
			line.positionCount = 5;
			line.SetPositions(UltraGhostGeometry.OutlineXZ(zCentre, rawM, tiltDeg, radiusM));
			line.endWidth = lineWidth;
			line.startWidth = lineWidth;
			line.numCornerVertices = 0;
			line.numCapVertices = 0;
			line.alignment = LineAlignment.View;
			line.shadowCastingMode = ShadowCastingMode.Off;
			line.receiveShadows = false;
			line.sharedMaterial = outlineMat;
			if (section)
			{
				continue;
			}
			GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			fill.name = string.Format("Заливка слоя {0}", i + 1);
			Collider collider = fill.GetComponent<Collider>();
			if (collider != null)
			{
				if (Application.isPlaying)
				{
					UnityEngine.Object.Destroy(collider);
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(collider);
				}
			}
			fill.transform.SetParent(root.transform, false);
			fill.transform.localRotation = Quaternion.Euler(0f, tiltDeg, 0f) * Quaternion.Euler(90f, 0f, 0f);
			fill.transform.localPosition = new Vector3(0f, 0f, zCentre);
			fill.transform.localScale = new Vector3(radiusM * 2f, rawM * 0.5f, radiusM * 2f);
			MeshRenderer fillRenderer = fill.GetComponent<MeshRenderer>();
			fillRenderer.sharedMaterial = fillMat;
			fillRenderer.shadowCastingMode = ShadowCastingMode.Off;
			fillRenderer.receiveShadows = false;
			GhostCount++;
		}
		if (!section)
		{
			return;
		}
		GhostCount = root.transform.childCount;
	}

	private void EnsureMaterials()
	{
		Shader shader = Shader.Find(ShaderName);
		if (shader == null && !shaderReported)
		{
			shaderReported = true;
			Debug.LogError("ULTRA призрак: шейдер «ArmorSim/UltraGhost» не найден (Assets/Resources/Shaders/UltraGhost.shader). Контур и заливка будут нарисованы откатным шейдером и могут выйти непрозрачными.");
		}
		if (shader == null)
		{
			shader = Shader.Find("Unlit/Color");
		}
		if (fillMat == null)
		{
			fillMat = new Material(shader);
			fillMat.name = "Призрак: заливка";
			fillMat.SetColor("_Color", fillColor);
			fillMat.SetFloat("_ZTest", 4f);
		}
		if (outlineMat == null)
		{
			outlineMat = new Material(shader);
			outlineMat.name = "Призрак: контур";
			outlineMat.SetColor("_Color", outlineColor);
			outlineMat.SetFloat("_ZTest", 8f);
		}
	}

	private void Clear()
	{
		built = null;
		GhostCount = 0;
		if (root != null)
		{
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(root);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
			root = null;
		}
	}
}
