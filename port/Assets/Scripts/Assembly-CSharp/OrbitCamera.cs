using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Object = UnityEngine.Object;

public class OrbitCamera : MonoBehaviour
{
	[Header("Цель вращения")]
	public Transform target;

	[Header("Настройки")]
	public float distance;

	public float minDistance;

	public float maxDistance;

	public float rotateSpeed;

	public float panSpeed;

	public float zoomSpeed;

	[Header("Плавность (постоянная времени, с)")]
	[Range(0.01f, 0.5f)]
	public float rotateSmoothing;

	[Range(0.01f, 0.5f)]
	public float zoomSmoothing;

	[Range(0.01f, 0.5f)]
	public float panSmoothing;

	private float yaw;

	private float pitch;

	private float yawTarget;

	private float pitchTarget;

	private float distanceTarget;

	private Vector3 pivotTarget;

	private bool pivotInit;

	private float maxDistanceBase;

	public static OrbitCamera Instance { get; private set; }

	public float DistanceTarget => distanceTarget;

	public float YawDeg => yaw;

	public float PitchDeg => pitch;

	public Vector3 PivotTarget => pivotTarget;

	public float YawTarget => yawTarget;

	public float PitchTarget => pitchTarget;

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		if (target == null)
		{
			GameObject gameObject = new GameObject("CameraTarget_Auto");
			gameObject.transform.position = new Vector3(0f, 1f, 6.5f);
			target = gameObject.transform;
		}
		distanceTarget = distance;
		pivotTarget = target.position;
		pivotInit = true;
		Camera camera = GetComponent<Camera>();
		if (camera == null)
		{
			camera = Camera.main;
		}
		float num = Mathf.Max(minDistance * 0.2f, 0.001f);
		if (camera != null && camera.nearClipPlane > num)
		{
			camera.nearClipPlane = num;
		}
	}

	public void SetOrbit(float yawDeg, float pitchDeg)
	{
		yaw = yawTarget = yawDeg;
		pitch = pitchTarget = Mathf.Clamp(pitchDeg, -85f, 85f);
	}

	public void FocusOn(Vector3 worldPoint, float distanceM = -1f)
	{
		if (target == null)
		{
			return;
		}
		pivotTarget = worldPoint;
		pivotInit = true;
		if (distanceM > 0f)
		{
			distanceTarget = Mathf.Clamp(SceneScale.FromM(distanceM), minDistance, maxDistance);
		}
	}

	private void FrameInterestingThing()
	{
		PenetrationPlayer penetrationPlayer = PenetrationPlayer.Instance;
		if (penetrationPlayer != null && penetrationPlayer.plan != null && penetrationPlayer.plan.layers != null && penetrationPlayer.plan.layers.Count != 0)
		{
			int num = 0;
			if (penetrationPlayer.segments != null && penetrationPlayer.segIndex >= 0 && penetrationPlayer.segIndex < penetrationPlayer.segments.Count)
			{
				num = Mathf.Max(0, penetrationPlayer.segments[penetrationPlayer.segIndex].layerIndex);
			}
			LayerPlaybackData layerPlaybackData = penetrationPlayer.plan.layers[Mathf.Clamp(num, 0, penetrationPlayer.plan.layers.Count - 1)];
			Vector3 vector = layerPlaybackData.entryPoint;
			Projectile projectile = Object.FindFirstObjectByType<Projectile>();
			float num2 = ((projectile == null) ? 0.03f : (Mathf.Max(projectile.diameter, 1f) / 1000f));
			if (target != null)
			{
				pivotTarget = vector;
				pivotInit = true;
				if (num2 * 12f > 0f)
				{
					distanceTarget = Mathf.Clamp(SceneScale.FromM(num2 * 12f), minDistance, maxDistance);
				}
			}
			Debug.Log(string.Format("[Камера] Кадр по точке удара {0} с {1:F3} м ", vector, num2 * 12f) + string.Format("(калибр {0:F0} мм). {1}", num2 * 1000f, SceneScale.Describe()));
			return;
		}
		PlateSelector plateSelector = Object.FindFirstObjectByType<PlateSelector>();
		ArmorPlate armorPlate = ((plateSelector != null) ? plateSelector.SelectedPlate : null);
		Renderer renderer = ((armorPlate != null) ? armorPlate.GetComponentInChildren<Renderer>() : null);
		if (renderer != null)
		{
			Bounds bounds = renderer.bounds;
			if (target != null)
			{
				Camera camera = GetComponent<Camera>();
				if (camera == null)
				{
					camera = Camera.main;
				}
				float num3 = ((camera != null) ? camera.fieldOfView : 60f);
				float num4 = Mathf.Max(bounds.extents.magnitude, SceneScale.MinRenderWorld);
				pivotTarget = bounds.center;
				pivotInit = true;
				distanceTarget = Mathf.Clamp(num4 / MathF.Max(MathF.Sin(num3 * Mathf.Deg2Rad * 0.5f), 0.001f), minDistance, maxDistance);
			}
			Debug.Log(string.Format("[Камера] Кадр по объекту {0}: габарит {1} мир.ед.", armorPlate.name, renderer.bounds.size));
			return;
		}
		if (!SceneBounds.TryScene(Object.FindFirstObjectByType<Projectile>(), out Bounds bounds2))
		{
			Debug.Log("[Камера] Кадрировать нечего: показа нет, объект не выделен, брони в сцене нет.");
			return;
		}
		if (target != null)
		{
			Camera camera2 = GetComponent<Camera>();
			if (camera2 == null)
			{
				camera2 = Camera.main;
			}
			float num5 = SceneFrame.FitDistance(bounds2, camera2);
			if (maxDistanceBase < 0f)
			{
				maxDistanceBase = maxDistance;
			}
			float num6 = Mathf.Max(Mathf.Max(maxDistanceBase, num5 * 2f), distanceTarget);
			if (num6 != maxDistance)
			{
				maxDistance = num6;
			}
			pivotTarget = bounds2.center;
			pivotInit = true;
			distanceTarget = Mathf.Clamp(num5, minDistance, maxDistance);
		}
		Debug.Log(string.Format("[Камера] Кадр по сцене: габарит {0} мир.ед., центр {1}.", bounds2.size, bounds2.center));
	}

	public Pose SteadyPose()
	{
		Quaternion quaternion = Quaternion.Euler(pitchTarget, yawTarget, 0f);
		Vector3 vector = pivotTarget + quaternion * new Vector3(0f, 0f, -distanceTarget);
		Vector3 forward = pivotTarget - vector;
		Quaternion rotation = ((forward.sqrMagnitude > 1e-12f) ? Quaternion.LookRotation(forward, Vector3.up) : Quaternion.identity);
		return new Pose(vector, rotation);
	}

	public void EnsureMaxDistance(float d)
	{
		if (maxDistanceBase < 0f)
		{
			maxDistanceBase = maxDistance;
		}
		float num = Mathf.Max(Mathf.Max(maxDistanceBase, d), distanceTarget);
		if (num != maxDistance)
		{
			maxDistance = num;
		}
	}

	public void FrameSceneBounds(Bounds b)
	{
		if (target == null)
		{
			return;
		}
		Camera camera = GetComponent<Camera>();
		if (camera == null)
		{
			camera = Camera.main;
		}
		float num = SceneFrame.FitDistance(b, camera);
		if (maxDistanceBase < 0f)
		{
			maxDistanceBase = maxDistance;
		}
		float num2 = Mathf.Max(Mathf.Max(maxDistanceBase, num * 2f), distanceTarget);
		if (num2 != maxDistance)
		{
			maxDistance = num2;
		}
		pivotTarget = b.center;
		pivotInit = true;
		distanceTarget = Mathf.Clamp(num, minDistance, maxDistance);
	}

	public void FrameBounds(Bounds b)
	{
		if (target == null)
		{
			return;
		}
		Camera camera = GetComponent<Camera>();
		if (camera == null)
		{
			camera = Camera.main;
		}
		float num = ((camera != null) ? camera.fieldOfView : 60f);
		float num2 = Mathf.Max(b.extents.magnitude, SceneScale.MinRenderWorld);
		pivotTarget = b.center;
		pivotInit = true;
		distanceTarget = Mathf.Clamp(num2 / MathF.Max(MathF.Sin(num * Mathf.Deg2Rad * 0.5f), 0.001f), minDistance, maxDistance);
	}

	private static float Damp(float tau, float dt)
	{
		if (tau <= 0.0001f)
		{
			return 1f;
		}
		return 1f - MathF.Exp(-dt / tau);
	}

	private void TouchCameraUpdate()
	{
		if (!TouchInputHelper.IsTouchDevice)
		{
			return;
		}
		if (Input.touchCount == 1)
		{
			Touch touch = Input.GetTouch(0);
			if (touch.phase != TouchPhase.Moved)
			{
				return;
			}
			Vector2 axisDelta = touch.deltaPosition / TouchInputHelper.PixelsPerAxisUnit;
			yawTarget += axisDelta.x * rotateSpeed;
			pitchTarget = Mathf.Clamp(pitchTarget - axisDelta.y * rotateSpeed, -85f, 85f);
			return;
		}
		if (Input.touchCount != 2)
		{
			return;
		}
		float pinchNotches = TouchInputHelper.PinchNotches;
		if (Mathf.Abs(pinchNotches) > 0.001f)
		{
			distanceTarget = Mathf.Clamp(MathF.Exp(-pinchNotches * zoomSpeed) * distanceTarget, minDistance, maxDistance);
		}
		Vector2 axisPan = TouchInputHelper.TwoFingerAxisDelta;
		if (axisPan.sqrMagnitude > 1e-06f)
		{
			float panScale = distance * 0.125f;
			pivotTarget -= transform.right * axisPan.x * panSpeed * panScale;
			pivotTarget -= transform.up * axisPan.y * panSpeed * panScale;
		}
	}

	private void LateUpdate()
	{
		if (target == null)
		{
			return;
		}
		if (!pivotInit)
		{
			pivotTarget = target.position;
			pivotInit = true;
		}
		float num = Time.unscaledDeltaTime;
		TouchCameraUpdate();
		bool flag = Input.GetMouseButton(ControlBindings.OrbitMouseButton);
		bool flag2 = ControlBindings.Held(ControlModifier.CameraPan);
		float num2 = Input.GetAxis("Mouse X");
		float num3 = Input.GetAxis("Mouse Y");
		if (flag && flag2)
		{
			float num4 = distance * 0.125f;
			pivotTarget -= transform.right * num2 * panSpeed * num4;
			pivotTarget -= transform.up * num3 * panSpeed * num4;
		}
		else if (flag)
		{
			yawTarget += num2 * rotateSpeed;
			pitchTarget = Mathf.Clamp(pitchTarget - num3 * rotateSpeed, -85f, 85f);
		}
		if (Input.GetKeyDown(ControlBindings.Key(ControlAction.FrameCamera)))
		{
			GameObject gameObject = ((EventSystem.current != null) ? EventSystem.current.currentSelectedGameObject : null);
			if (gameObject == null || (gameObject.GetComponent<TMP_InputField>() == null))
			{
				FrameInterestingThing();
			}
		}
		float num5 = ControlBindings.WheelSign(ControlScope.Analytic3D) * WheelZones.SceneWheel(Input.GetAxis("Mouse ScrollWheel"));
		if (MathF.Abs(num5) > 1e-05f)
		{
			distanceTarget = Mathf.Clamp(MathF.Exp(-num5 * zoomSpeed) * distanceTarget, minDistance, maxDistance);
		}
		float num6 = Damp(rotateSmoothing, num);
		yaw = Mathf.LerpAngle(yaw, yawTarget, num6);
		pitch = Mathf.Lerp(pitch, pitchTarget, num6);
		distance = Mathf.Clamp(Mathf.Lerp(distance, distanceTarget, Damp(zoomSmoothing, num)), minDistance, maxDistance);
		target.position = Vector3.Lerp(target.position, pivotTarget, Damp(panSmoothing, num));
		Quaternion quaternion = Quaternion.Euler(pitch, yaw, 0f);
		transform.position = target.position + quaternion * new Vector3(0f, 0f, -distance);
		transform.LookAt(target.position);
	}

	public OrbitCamera()
	{
		distance = SceneDefaults.CameraDistanceM;
		minDistance = SceneDefaults.MinDistanceM(SceneDefaults.SandboxCraterDiameterM);
		maxDistance = SceneDefaults.MaxDistanceM;
		rotateSpeed = 4f;
		panSpeed = 0.5f;
		zoomSpeed = 3f;
		rotateSmoothing = 0.1f;
		zoomSmoothing = 0.14f;
		panSmoothing = 0.1f;
		pitch = 20f;
		pitchTarget = 20f;
		distanceTarget = 11f;
		maxDistanceBase = -1f;
	}
}
