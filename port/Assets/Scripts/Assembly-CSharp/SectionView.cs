using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SectionView : MonoBehaviour
{
	public const string ShaderName = "Hidden/ArmorSim/SectionClip";

	public const string ShaderResourcePath = "Shaders/SectionClip";

	public const string CapShaderName = "Hidden/ArmorSim/SectionCap";

	public const string CapShaderResourcePath = "Shaders/SectionCap";

	public const int CapPassMark = 0;

	public const int CapPassUnmark = 1;

	public const int CapPassFill = 2;

	public const int BodyRescanFrames = 30;

	public const string UnlitRenderType = "ArmorSimUnlit";

	[Header("Режим сечения")]
	public const bool DefaultOn = false;

	public bool sectionOn;

	[Header("Положение плоскости")]
	public float azimuthDeg;

	public float offsetM;

	public const float OffsetRangeM = 0.15f;

	[Header("Заливка среза")]
	public const float DefaultCapMul = 1f;

	public const float DefaultCapFloor = 0.035f;

	public float capMul;

	public float capFloor;

	[Header("Сверка длины (§6 ТЗ)")]
	public bool auditDrawnLength = true;

	private Shader sectionShader;

	private Shader capShader;

	private Vector3 anchorWorld;

	private Vector3 anchorAxis = Vector3.forward;

	private bool anchored;

	private bool appliedOn;

	private int appliedCameraCount = -1;

	private Material capMaterial;

	private CommandBuffer capBuffer;

	private MaterialPropertyBlock capBlock;

	private readonly List<Camera> capCameras = new List<Camera>();

	private readonly List<Renderer> capBodies = new List<Renderer>();

	private int lastBodyScanFrame = -1000;

	public const float BendGateDiameters = 3f;

	public const float DriftTolerance = 0.05f;

	private int auditSamples;

	private int auditSkipped;

	private int auditViolations;

	private float worstExcess;

	private float worstModelM;

	private float worstDrawnM;

	public static SectionView Instance { get; private set; }

	public static KeyCode DefaultToggleKey => ControlBindings.DefaultKey(ControlAction.SectionToggle);

	public static KeyCode DefaultRefaceKey => ControlBindings.DefaultKey(ControlAction.SectionReface);

	public static void Bootstrap()
	{
		if (Object.FindFirstObjectByType<SectionView>() == null)
		{
			GameObject gameObject = new GameObject("SectionView");
			gameObject.AddComponent<SectionView>();
		}
	}

	private void Awake()
	{
		Instance = this;
		sectionShader = Resources.Load<Shader>(ShaderResourcePath);
		if (sectionShader == null)
		{
			sectionShader = Shader.Find(ShaderName);
		}
		if (sectionShader == null)
		{
			Debug.LogError("[SectionView] Шейдер «Hidden/ArmorSim/SectionClip» не найден — режим сечения НЕДОСТУПЕН. Ожидается Assets/Resources/Shaders/SectionClip.shader");
		}
		else if (!sectionShader.isSupported)
		{
			Debug.LogError("[SectionView] Шейдер «Hidden/ArmorSim/SectionClip» не поддержан этой платформой или не собрался — режим сечения выключен. Смотри ошибки компиляции шейдера.");
			sectionShader = null;
		}
		SetupCap();
	}

	private void SetupCap()
	{
		capShader = Resources.Load<Shader>(CapShaderResourcePath);
		if (capShader == null)
		{
			capShader = Shader.Find(CapShaderName);
		}
		if (capShader != null && capShader.isSupported)
		{
			capMaterial = new Material(capShader);
			capMaterial.hideFlags = HideFlags.HideAndDontSave;
			capBlock = new MaterialPropertyBlock();
			capBuffer = new CommandBuffer();
			capBuffer.name = "ArmorSim/Крышка сечения";
		}
		else
		{
			Debug.LogError("[SectionView] Шейдер крышки «Hidden/ArmorSim/SectionCap» не найден или не собрался — плоскость реза останется НЕЗАЛИТОЙ (сквозь неё будет видно нутро полости). Сам режим сечения при этом работает.");
			capShader = null;
		}
	}

	private void OnDisable()
	{
		sectionOn = false;
		SyncReplacement(force: true);
		Shader.SetGlobalFloat("_SectionEnabled", 0f);
	}

	private void OnDestroy()
	{
		DetachCapBuffer();
		if (capMaterial != null)
		{
			Object.DestroyImmediate(capMaterial);
		}
		if (capBuffer != null)
		{
			capBuffer.Release();
			capBuffer = null;
		}
	}

	public static void MarkUnlit(Material m)
	{
		if (m != null)
		{
			m.SetOverrideTag("RenderType", UnlitRenderType);
		}
	}

	public void Toggle()
	{
		SetSection(!sectionOn);
	}

	public void SetSection(bool on)
	{
		if (on && sectionShader == null)
		{
			Debug.LogError("[SectionView] Включить сечение нечем: шейдер не загрузился (см. ошибку при старте).");
			return;
		}
		if (on && !sectionOn)
		{
			CaptureAnchor();
			RefaceToCamera();
		}
		sectionOn = on;
		if (on)
		{
			CollectBodies();
			PushGlobals();
			SyncReplacement(force: true);
			Debug.Log("[SectionView] Сечение ВКЛ: " + Describe());
			ResetAudit();
		}
		else
		{
			PushGlobals();
			SyncReplacement(force: true);
			Debug.Log("[SectionView] Сечение ВЫКЛ — подмена шейдера снята со всех камер.");
			ReportAudit();
		}
	}

	public void SetOffsetM(float m)
	{
		offsetM = Mathf.Clamp(m, 0f - OffsetRangeM, OffsetRangeM);
		PushGlobals();
		SyncReplacement(force: true);
	}

	public void RefaceToCamera()
	{
		Camera camera = Camera.main;
		if (camera == null)
		{
			camera = Camera.current;
		}
		if (camera == null)
		{
			return;
		}
		if (!anchored)
		{
			CaptureAnchor();
		}
		if (SectionPlaneMath.AzimuthAwayFromCamera(anchorAxis, anchorWorld, camera.transform.position, out var azimuthDeg))
		{
			this.azimuthDeg = azimuthDeg;
		}
		PushGlobals();
		SyncReplacement(force: true);
	}

	private void CaptureAnchor()
	{
		Projectile projectile = Object.FindFirstObjectByType<Projectile>();
		Vector3 vector = Vector3.forward;
		if (projectile != null && projectile.direction.sqrMagnitude > 1e-6f)
		{
			vector = projectile.direction.normalized;
		}
		anchorAxis = vector;
		if (PenetrationPlayer.Instance != null && PenetrationPlayer.Instance.TryGetImpactPoint(out var point))
		{
			anchorWorld = point;
		}
		else if (projectile == null)
		{
			anchorWorld = Vector3.zero;
		}
		else
		{
			anchorWorld = projectile.transform.position;
		}
		anchored = true;
	}

	public Vector4 CurrentPlane()
	{
		if (!anchored)
		{
			CaptureAnchor();
		}
		Vector3 vector = SectionPlaneMath.NormalFromAzimuth(anchorAxis, azimuthDeg);
		return new Vector4(vector.x, vector.y, vector.z, Vector3.Dot(anchorWorld, vector) + offsetM);
	}

	public string Describe()
	{
		Vector4 vector = CurrentPlane();
		return string.Format("нормаль ({0:F3}, {1:F3}, {2:F3}) ⊥ оси выстрела, азимут {3:F1}°, ", vector.x, vector.y, vector.z, azimuthDeg) + string.Format("смещение {0:F0} мм, якорь ({1:F2}, {2:F2}, {3:F2}).", offsetM * 1000f, anchorWorld.x, anchorWorld.y, anchorWorld.z);
	}

	private void Apply()
	{
		PushGlobals();
		SyncReplacement(force: true);
	}

	private void PushGlobals()
	{
		Shader.SetGlobalVector("_SectionPlane", CurrentPlane());
		float value = (sectionOn && sectionShader != null) ? 1 : 0;
		Shader.SetGlobalFloat("_SectionEnabled", value);
		Shader.SetGlobalFloat("_SectionCapMul", capMul);
		Shader.SetGlobalFloat("_SectionCapFloor", capFloor);
	}

	private void SyncReplacement(bool force)
	{
		bool flag = sectionOn && sectionShader != null;
		if (!force && flag == appliedOn)
		{
			if (!flag)
			{
				return;
			}
			if (Camera.allCamerasCount == appliedCameraCount)
			{
				return;
			}
		}
		Camera[] cameras = Camera.allCameras;
		for (int i = 0; i < cameras.Length; i++)
		{
			Camera camera = cameras[i];
			if (camera == null)
			{
				continue;
			}
			if (flag)
			{
				camera.SetReplacementShader(sectionShader, "RenderType");
			}
			else
			{
				camera.ResetReplacementShader();
			}
		}
		DetachCapBuffer();
		int num = -1;
		if (flag)
		{
			if (capBuffer != null)
			{
				for (int j = 0; j < cameras.Length; j++)
				{
					Camera camera2 = cameras[j];
					if (camera2 == null)
					{
						continue;
					}
					camera2.AddCommandBuffer(CameraEvent.AfterForwardOpaque, capBuffer);
					capCameras.Add(camera2);
				}
			}
			num = cameras.Length;
		}
		appliedCameraCount = num;
		appliedOn = flag;
	}

	private void AttachCapBuffer(Camera[] cams, bool want)
	{
		DetachCapBuffer();
		if (!want || capBuffer == null)
		{
			return;
		}
		for (int i = 0; i < cams.Length; i++)
		{
			Camera camera = cams[i];
			if (camera == null)
			{
				continue;
			}
			camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, capBuffer);
			capCameras.Add(camera);
		}
	}

	private void DetachCapBuffer()
	{
		if (capBuffer != null)
		{
			for (int i = 0; i < capCameras.Count; i++)
			{
				Camera camera = capCameras[i];
				if (camera != null)
				{
					camera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, capBuffer);
				}
			}
		}
		capCameras.Clear();
	}

	private void BuildCapBuffer()
	{
		if (capBuffer == null || capMaterial == null)
		{
			return;
		}
		capBuffer.Clear();
		if (!sectionOn)
		{
			return;
		}
		for (int i = 0; i < capBodies.Count; i++)
		{
			Renderer renderer = capBodies[i];
			if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
			{
				continue;
			}
			MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
			Mesh mesh = (meshFilter != null) ? meshFilter.sharedMesh : null;
			if (mesh == null || mesh.vertexCount == 0)
			{
				continue;
			}
			int num = Mathf.Max(mesh.subMeshCount, 1);
			Matrix4x4 localToWorldMatrix = renderer.localToWorldMatrix;
			capBlock.Clear();
			capBlock.SetColor("_Color", BodyColor(renderer));
			for (int j = 0; j < num; j++)
			{
				capBuffer.DrawMesh(mesh, localToWorldMatrix, capMaterial, j, CapPassMark, capBlock);
			}
			for (int k = 0; k < num; k++)
			{
				capBuffer.DrawMesh(mesh, localToWorldMatrix, capMaterial, k, CapPassUnmark, capBlock);
			}
			for (int l = 0; l < num; l++)
			{
				capBuffer.DrawMesh(mesh, localToWorldMatrix, capMaterial, l, CapPassFill, capBlock);
			}
		}
	}

	private static Color BodyColor(Renderer r)
	{
		Material[] sharedMaterials = r.sharedMaterials;
		if (sharedMaterials != null && sharedMaterials.Length != 0 && sharedMaterials[0] != null && sharedMaterials[0].HasProperty("_Color"))
		{
			return sharedMaterials[0].GetColor("_Color");
		}
		return new Color(0.5f, 0.5f, 0.5f, 1f);
	}

	private void CollectBodies()
	{
		lastBodyScanFrame = Time.frameCount;
		capBodies.Clear();
		Renderer[] array = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			Renderer renderer = array[i];
			if (renderer != null && renderer.GetComponent<Renderer>() != null)
			{
				capBodies.Add(renderer);
			}
		}
		if (PenetrationPlayer.Instance != null && PenetrationPlayer.Instance.DrawnRodRenderer != null)
		{
			capBodies.Add(PenetrationPlayer.Instance.DrawnRodRenderer);
		}
		NeraBlock.CollectCapBodies(capBodies);
	}

	private void ClearReplacement()
	{
		sectionOn = false;
		SyncReplacement(force: true);
	}

	private void Update()
	{
		if (!TypingInField())
		{
			if (Input.GetKeyDown(ControlBindings.Key(ControlAction.SectionToggle)))
			{
				SetSection(!sectionOn);
			}
			else if (sectionOn && Input.GetKeyDown(ControlBindings.Key(ControlAction.SectionReface)))
			{
				RefaceToCamera();
			}
		}
		PushGlobals();
		SyncReplacement(force: false);
	}

	private static bool TypingInField()
	{
		return NumberField.TypingNow;
	}

	public void ResetAudit()
	{
		auditSkipped = 0;
		auditSamples = 0;
		worstModelM = 0f;
		worstExcess = 0f;
	}

	private void LateUpdate()
	{
		if (sectionOn && auditDrawnLength)
		{
			AuditDrawnLength();
		}
		if (sectionOn && Time.frameCount - lastBodyScanFrame >= BodyRescanFrames)
		{
			CollectBodies();
		}
		BuildCapBuffer();
	}

	private void AuditDrawnLength()
	{
		PenetrationPlayer penetrationPlayer = PenetrationPlayer.Instance;
		if (penetrationPlayer == null)
		{
			return;
		}
		float currentModelRodLengthM = penetrationPlayer.CurrentModelRodLengthM;
		if (currentModelRodLengthM <= 1e-6f)
		{
			return;
		}
		Renderer drawnRodRenderer = penetrationPlayer.DrawnRodRenderer;
		if (drawnRodRenderer == null)
		{
			return;
		}
		Projectile projectile = Object.FindFirstObjectByType<Projectile>();
		float num = (projectile != null) ? (projectile.diameter / 1000f) : 0.03f;
		Vector3 axis = anchorAxis;
		Vector3 extents = drawnRodRenderer.bounds.extents;
		float num2 = BoxWidthAlong(extents, axis);
		SectionPlaneMath.AxisBasis(axis, out var right, out var up);
		float value = BoxWidthAlong(extents, right);
		float value2 = BoxWidthAlong(extents, up);
		float num3 = Mathf.Max(value, value2);
		if (num3 > num * BendGateDiameters)
		{
			auditSkipped++;
			return;
		}
		auditSamples++;
		float num4 = (num2 - currentModelRodLengthM) / Mathf.Max(currentModelRodLengthM, 1e-6f);
		if (num4 > DriftTolerance)
		{
			auditViolations++;
		}
		if (num4 > worstExcess)
		{
			worstExcess = num4;
			worstModelM = currentModelRodLengthM;
			worstDrawnM = num2;
		}
	}

	public static float BoxWidthAlong(Vector3 extents, Vector3 d)
	{
		return 2f * (Mathf.Abs(d.x) * extents.x + Mathf.Abs(d.y) * extents.y + Mathf.Abs(d.z) * extents.z);
	}

	public void ReportAudit()
	{
		if (auditSamples == 0 && auditSkipped == 0)
		{
			return;
		}
		string text = string.Format("[SectionView §6] Сверка нарисованной длины: замеров {0}, ", auditSamples) + string.Format("пропущено по изгибу {0}", auditSkipped);
		if (auditViolations != 0)
		{
			Debug.LogWarning(string.Format("{0} — ПРЕВЫШЕНИЙ {1}. Худшее: модель заявляет ", text, auditViolations) + string.Format("{0:F1} мм, нарисовано {1:F1} мм ", worstModelM * 1000f, worstDrawnM * 1000f) + string.Format("(+{0:F1} %). Это ровно та порода дефекта, ради которого ", worstExcess * 100f) + "сечение и заводилось: посчитано верно, нарисовано из другого.");
		}
		else
		{
			Debug.Log(text + " — превышений нет (нарисованное нигде не длиннее того, что " + string.Format("осталось после стачивания; худший запас {0:F1} % при допуске ", worstExcess * 100f) + string.Format("{0:F0} %).", 5f));
		}
		auditSamples = 0;
		worstExcess = 0f;
		auditSkipped = 0;
		worstModelM = 0f;
	}
}
