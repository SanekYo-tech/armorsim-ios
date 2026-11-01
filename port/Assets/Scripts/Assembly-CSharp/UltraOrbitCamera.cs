using UnityEngine;

public class UltraOrbitCamera : MonoBehaviour
{
	public enum Framing
	{
		Static = 0,
		Follow = 1
	}

	public enum Projection
	{
		Ortho2DSection = 0,
		Perspective3D = 1
	}

	[Header("Режимы")]
	public UltraViewMode viewMode;

	public Framing framing;

	public Projection projection;

	public float orthoHalfHeightM;

	public float minOrthoHalfHeightM;

	public float maxOrthoHalfHeightM;

	public Camera cam;

	[Header("Орбита")]
	public float azimuthDeg;

	public float elevationDeg;

	public float distanceM;

	[Header("Пределы")]
	public float minDistanceM;

	public float maxDistanceM;

	public float minElevationDeg;

	public float maxElevationDeg;

	[Header("Цель")]
	public Vector3 pivotM;

	public Transform cameraArm;

	public const float FrameMargin = 1.7f;

	[Header("Исходное кадрирование")]
	public Vector3 homePivotM;

	public float homeDistanceM;

	public float homeOrthoHalfHeightM;

	public float homeAzimuthDeg;

	public float homeElevationDeg;

	[HideInInspector]
	public float craterFrontZM;

	[Header("Чувствительность ввода")]
	public float panSensitivity;

	public float orbitSensitivityDeg;

	public float zoomPerNotch;

	public const float HeadlightIntensity = 0.9f;

	private Light headlight;

	public string PresetKey
	{
		get
		{
			if (projection == Projection.Ortho2DSection)
			{
				return "ultra.view.2d";
			}
			return "ultra.view.3d";
		}
	}

	public static float FramePivotZ(float plateFaceZM, float plateThicknessM)
	{
		return plateFaceZM + plateThicknessM * 0.5f;
	}

	public static float FrameHalfHeight(float plateThicknessM, float rodDiameterM)
	{
		float num = Mathf.Max(plateThicknessM * 0.5f * FrameMargin, rodDiameterM * FrameMargin);
		return Mathf.Max(0.005f, num);
	}

	public void FrameOn(float plateFaceZM, float plateThicknessM, float rodDiameterM)
	{
		homePivotM = new Vector3(0f, 0f, FramePivotZ(plateFaceZM, plateThicknessM));
		homeDistanceM = Mathf.Max(homeDistanceM, 0.45f);
		homeOrthoHalfHeightM = FrameHalfHeight(plateThicknessM, rodDiameterM);
		pivotM = homePivotM;
		azimuthDeg = homeAzimuthDeg;
		elevationDeg = homeElevationDeg;
		distanceM = homeDistanceM;
		orthoHalfHeightM = homeOrthoHalfHeightM;
		Apply();
	}

	private void Update()
	{
		TouchUpdate();
		float num = WheelZones.SceneWheel(Input.mouseScrollDelta.y);
		if (Mathf.Abs(num) > 0.01f)
		{
			float num2 = ((num > 0f) ? zoomPerNotch : (1f / zoomPerNotch));
			num2 = Mathf.Max(0.01f, num2);
			if (projection == Projection.Ortho2DSection)
			{
				orthoHalfHeightM *= num2;
			}
			else
			{
				distanceM *= num2;
			}
			Apply();
		}
		float num3 = Input.GetAxisRaw("Mouse X");
		float num4 = Input.GetAxisRaw("Mouse Y");
		if (Mathf.Abs(num3) >= 0.0001f || Mathf.Abs(num4) >= 0.0001f)
		{
			if (Input.GetMouseButton(2))
			{
				Pan(-num3 * panSensitivity * 0.02f, -num4 * panSensitivity * 0.02f);
			}
			else
			{
				if (!Input.GetMouseButton(1))
				{
					return;
				}
				if (projection == Projection.Ortho2DSection)
				{
					return;
				}
				elevationDeg += -num4 * orbitSensitivityDeg * 0.01f;
				azimuthDeg += num3 * orbitSensitivityDeg * 0.01f;
			}
			Apply();
		}
	}

	private void TouchUpdate()
	{
		if (!TouchInputHelper.IsTouchDevice)
		{
			return;
		}
		if (Input.touchCount == 2)
		{
			float pinchNotches = TouchInputHelper.PinchNotches;
			if (Mathf.Abs(pinchNotches) > 0.01f)
			{
				float num = ((pinchNotches > 0f) ? zoomPerNotch : (1f / zoomPerNotch));
				num = Mathf.Max(0.01f, num);
				if (projection == Projection.Ortho2DSection)
				{
					orthoHalfHeightM *= num;
				}
				else
				{
					distanceM *= num;
				}
				Apply();
			}
			Vector2 pan = TouchInputHelper.TwoFingerDelta;
			if (pan.sqrMagnitude > 1f)
			{
				Pan(0f - pan.x * panSensitivity * 0.02f, 0f - pan.y * panSensitivity * 0.02f);
			}
		}
		else if (Input.touchCount == 1)
		{
			Touch touch = Input.GetTouch(0);
			if (touch.phase != TouchPhase.Moved)
			{
				return;
			}
			if (projection == Projection.Ortho2DSection)
			{
				Pan(0f - touch.deltaPosition.x * panSensitivity * 0.02f, 0f - touch.deltaPosition.y * panSensitivity * 0.02f);
			}
			else
			{
				elevationDeg += -touch.deltaPosition.y * orbitSensitivityDeg * 0.01f;
				azimuthDeg += touch.deltaPosition.x * orbitSensitivityDeg * 0.01f;
				Apply();
			}
		}
	}

	private void LateUpdate()
	{
		Apply();
	}

	private void Awake()
	{
		EnsureHeadlight();
	}

	private void EnsureHeadlight()
	{
		if (headlight != null)
		{
			return;
		}
		Transform transform = ((cam != null) ? cam.transform : cameraArm);
		if (transform == null)
		{
			return;
		}
		GameObject gameObject = new GameObject("Фонарь сечения");
		gameObject.transform.SetParent(transform, false);
		gameObject.transform.localRotation = Quaternion.Euler(22f, -26f, 0f);
		headlight = gameObject.AddComponent<Light>();
		headlight.type = LightType.Directional;
		headlight.intensity = HeadlightIntensity;
		headlight.color = new Color(0.96f, 0.97f, 1f);
		headlight.shadows = LightShadows.None;
	}

	public static Vector3 SectionCameraOffset(float distanceM)
	{
		return new Vector3(0f, -Mathf.Abs(distanceM), 0f);
	}

	public static Quaternion SectionCameraRotation()
	{
		return Quaternion.LookRotation(Vector3.up, Vector3.right);
	}

	public void Apply()
	{
		distanceM = Mathf.Clamp(distanceM, minDistanceM, maxDistanceM);
		orthoHalfHeightM = Mathf.Clamp(orthoHalfHeightM, minOrthoHalfHeightM, maxOrthoHalfHeightM);
		EnsureHeadlight();
		if (headlight != null)
		{
			headlight.enabled = projection == Projection.Ortho2DSection || viewMode == UltraViewMode.Section;
		}
		if (projection == Projection.Ortho2DSection)
		{
			transform.position = new Vector3(pivotM.x, pivotM.y, (framing == Framing.Follow) ? craterFrontZM : pivotM.z);
			if (cam != null)
			{
				cam.orthographic = true;
				cam.orthographicSize = orthoHalfHeightM;
			}
			if (cameraArm != null)
			{
				cameraArm.localPosition = SectionCameraOffset(distanceM);
				cameraArm.localRotation = SectionCameraRotation();
			}
			return;
		}
		if (cam != null)
		{
			cam.orthographic = false;
		}
		azimuthDeg = HalfLathe.ClampOrbitAzimuth(azimuthDeg, viewMode);
		elevationDeg = Mathf.Clamp(elevationDeg, minElevationDeg, maxElevationDeg);
		transform.position = new Vector3(pivotM.x, pivotM.y, (framing == Framing.Follow) ? craterFrontZM : pivotM.z);
		if (cameraArm != null)
		{
			float num = elevationDeg * Mathf.Deg2Rad;
			float num2 = azimuthDeg * Mathf.Deg2Rad;
			cameraArm.localPosition = new Vector3(Mathf.Sin(num) * Mathf.Sin(num2) * distanceM, Mathf.Cos(num) * distanceM, Mathf.Sin(num) * Mathf.Cos(num2) * distanceM);
			cameraArm.LookAt(transform.position, Vector3.up);
		}
	}

	public void Orbit(float dAzimuth, float dElevation)
	{
		if (projection == Projection.Ortho2DSection)
		{
			return;
		}
		azimuthDeg = dAzimuth + azimuthDeg;
		elevationDeg = dElevation + elevationDeg;
		Apply();
	}

	public void Pan(float dRightFrac, float dUpFrac)
	{
		float num = ((projection == Projection.Ortho2DSection) ? orthoHalfHeightM : distanceM);
		pivotM = new Vector3(pivotM.x + num * dUpFrac, pivotM.y + 0f, pivotM.z + num * dRightFrac);
		Apply();
	}

	public void ResetFraming()
	{
		pivotM = homePivotM;
		distanceM = homeDistanceM;
		orthoHalfHeightM = homeOrthoHalfHeightM;
		azimuthDeg = homeAzimuthDeg;
		elevationDeg = homeElevationDeg;
		Apply();
	}

	public void TogglePreset()
	{
		Projection projection = this.projection;
		this.projection = ((projection != Projection.Ortho2DSection) ? Projection.Ortho2DSection : Projection.Perspective3D);
		if (projection == Projection.Ortho2DSection)
		{
			azimuthDeg = HalfLathe.ClampOrbitAzimuth(azimuthDeg, viewMode);
		}
		Apply();
	}

	public void Zoom(float factor)
	{
		factor = Mathf.Max(0.01f, factor);
		if (projection == Projection.Ortho2DSection)
		{
			orthoHalfHeightM *= factor;
		}
		else
		{
			distanceM *= factor;
		}
		Apply();
	}

	public void SetProjection(Projection p)
	{
		projection = p;
		if (p == Projection.Perspective3D)
		{
			azimuthDeg = HalfLathe.ClampOrbitAzimuth(azimuthDeg, viewMode);
		}
		Apply();
	}

	public void SetViewMode(UltraViewMode m)
	{
		viewMode = m;
		azimuthDeg = HalfLathe.ClampOrbitAzimuth(azimuthDeg, m);
		Apply();
	}

	public UltraOrbitCamera()
	{
		orthoHalfHeightM = 0.12f;
		minOrthoHalfHeightM = 0.005f;
		maxOrthoHalfHeightM = 1.5f;
		azimuthDeg = 250f;
		elevationDeg = 14f;
		distanceM = 0.45f;
		minDistanceM = 0.05f;
		maxDistanceM = 3f;
		minElevationDeg = -80f;
		maxElevationDeg = 80f;
		pivotM = Vector3.forward;
		homePivotM = Vector3.forward;
		homeDistanceM = 0.45f;
		homeOrthoHalfHeightM = 0.12f;
		homeAzimuthDeg = 250f;
		homeElevationDeg = 14f;
		panSensitivity = 1.6f;
		orbitSensitivityDeg = 220f;
		zoomPerNotch = 0.88f;
	}
}
