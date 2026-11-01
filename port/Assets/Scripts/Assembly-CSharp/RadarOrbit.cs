using System;
using UnityEngine;

public sealed class RadarOrbit : MonoBehaviour
{
	private struct OrbitSnapshot
	{
		public Vector3 target;

		public Vector3 targetGoal;

		public float yaw;

		public float pitch;

		public float yawGoal;

		public float pitchGoal;

		public float distance;

		public float distanceGoal;
	}

	private const float SmoothingSeconds = 0.1f;

	private const float MinimumDistanceRadii = 0.4f;

	private const float MaximumDistanceRadii = 40f;

	private const float DefaultDistanceRadii = 2.6f;

	private Vector3 center;

	private Vector3 target;

	private Vector3 targetGoal;

	private float rbs;

	private float yaw;

	private float pitch;

	private float yawGoal;

	private float pitchGoal;

	private float distance;

	private float distanceGoal;

	private bool configured;

	private bool radarView;

	private bool orbitWasOrthographic;

	private Vector3 radarDirection;

	private Vector3 radarUp;

	private Camera viewCamera;

	private OrbitSnapshot savedOrbit;

	public bool IsRadarView => radarView;

	public Camera ViewCamera
	{
		get
		{
			if (viewCamera != null)
			{
				return viewCamera;
			}
			viewCamera = GetComponent<Camera>();
			return viewCamera;
		}
	}

	public RadarOrbit()
	{
		rbs = 1f;
		yaw = -32f;
		pitch = 20f;
		yawGoal = -32f;
		pitchGoal = 20f;
		distance = 2.6f;
		distanceGoal = 2.6f;
		radarDirection = Vector3.forward;
		radarUp = Vector3.up;
	}

	private static float RequireRadius(float radius)
	{
		if (radius > 0f && !float.IsInfinity(radius))
		{
			return radius;
		}
		throw new ArgumentOutOfRangeException("radius", radius, "Radar bounding-sphere radius must be positive and finite.");
	}

	public void Configure(Vector3 modelCenter, float boundingSphereRadius)
	{
		rbs = RequireRadius(boundingSphereRadius);
		center = modelCenter;
		targetGoal = modelCenter;
		target = modelCenter;
		distanceGoal = rbs * DefaultDistanceRadii;
		distance = rbs * DefaultDistanceRadii;
		if (!configured)
		{
			yawGoal = -32f;
			yaw = -32f;
			pitchGoal = 20f;
			pitch = 20f;
			configured = true;
		}
		Camera viewCamera = ViewCamera;
		viewCamera.nearClipPlane = rbs * 0.01f;
		viewCamera.farClipPlane = rbs * 100f;
		if (!radarView)
		{
			ApplyOrbitPose();
			return;
		}
		savedOrbit.target = center;
		savedOrbit.targetGoal = center;
		savedOrbit.distance = rbs * DefaultDistanceRadii;
		savedOrbit.distanceGoal = rbs * DefaultDistanceRadii;
		ApplyRadarPose();
	}

	public void Frame()
	{
		if (radarView)
		{
			return;
		}
		targetGoal = center;
		distanceGoal = rbs * DefaultDistanceRadii;
	}

	public void SetRadarView(bool enabled, Vector3 modelCenter, float boundingSphereRadius, Vector3 directionToRadar, Vector3 verticalPolarization)
	{
		rbs = RequireRadius(boundingSphereRadius);
		center = modelCenter;
		radarDirection = directionToRadar.normalized;
		radarUp = verticalPolarization.normalized;
		if (enabled)
		{
			if (!radarView)
			{
				savedOrbit.target = target;
				savedOrbit.targetGoal = targetGoal;
				savedOrbit.yaw = yaw;
				savedOrbit.pitch = pitch;
				savedOrbit.yawGoal = yawGoal;
				savedOrbit.pitchGoal = pitchGoal;
				savedOrbit.distance = distance;
				savedOrbit.distanceGoal = distanceGoal;
				orbitWasOrthographic = ViewCamera.orthographic;
				radarView = true;
			}
			ApplyRadarPose();
		}
		else if (radarView)
		{
			radarView = false;
			target = savedOrbit.target;
			targetGoal = savedOrbit.targetGoal;
			yaw = savedOrbit.yaw;
			pitch = savedOrbit.pitch;
			yawGoal = savedOrbit.yawGoal;
			pitchGoal = savedOrbit.pitchGoal;
			distance = savedOrbit.distance;
			distanceGoal = savedOrbit.distanceGoal;
			ViewCamera.orthographic = orbitWasOrthographic;
			ApplyOrbitPose();
		}
	}

	public void UpdateRadarPose(Vector3 modelCenter, float boundingSphereRadius, Vector3 directionToRadar, Vector3 verticalPolarization)
	{
		rbs = RequireRadius(boundingSphereRadius);
		center = modelCenter;
		radarDirection = directionToRadar.normalized;
		radarUp = verticalPolarization.normalized;
		if (radarView)
		{
			ApplyRadarPose();
		}
	}

	private void LateUpdate()
	{
		if (!configured)
		{
			return;
		}
		if (radarView)
		{
			ApplyRadarPose();
			return;
		}
		Rect pixelRect = ViewCamera.pixelRect;
		bool inside = pixelRect.Contains(WheelZones.Cursor());
		bool cursorOverPanel = WheelZones.CursorOverPanel();
		if (inside)
		{
			if (!cursorOverPanel && Input.GetMouseButton(2))
			{
				float axis = Input.GetAxis("Mouse X");
				float axis2 = Input.GetAxis("Mouse Y");
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					targetGoal -= (base.transform.right * axis + base.transform.up * axis2) * distance * 0.06f;
				}
				else
				{
					yawGoal += axis * 4f;
					pitchGoal = Mathf.Clamp(pitchGoal - axis2 * 4f, -80f, 80f);
				}
			}
			float num = WheelZones.SceneWheel(Input.GetAxis("Mouse ScrollWheel")) * ControlBindings.WheelSign(ControlScope.Analytic3D);
			distanceGoal = Mathf.Clamp(distanceGoal * Mathf.Exp(-num * 3f), rbs * MinimumDistanceRadii, rbs * MaximumDistanceRadii);
		}
		TouchOrbitUpdate(pixelRect);
		if (!NumberField.TypingNow && Input.GetKeyDown(KeyCode.F))
		{
			Frame();
		}
		float num2 = 1f - Mathf.Exp(-Time.unscaledDeltaTime / SmoothingSeconds);
		float num3 = Mathf.Clamp(Mathf.Repeat(yawGoal - yaw, 360f), 0f, 360f);
		if (num3 > 180f)
		{
			num3 -= 360f;
		}
		yaw = Mathf.Lerp(yaw, yaw + num3, num2);
		pitch = Mathf.Lerp(pitch, pitchGoal, num2);
		target = Vector3.Lerp(target, targetGoal, num2);
		distance = Mathf.Lerp(distance, distanceGoal, num2);
		ApplyOrbitPose();
	}

	private void TouchOrbitUpdate(Rect pixelRect)
	{
		if (!TouchInputHelper.IsTouchDevice || Input.touchCount == 0)
		{
			return;
		}
		if (Input.touchCount == 1)
		{
			Touch touch = Input.GetTouch(0);
			if (touch.phase != TouchPhase.Moved || !pixelRect.Contains(touch.position) || WheelZones.CursorOverPanel())
			{
				return;
			}
			Vector2 axisDelta = touch.deltaPosition / TouchInputHelper.PixelsPerAxisUnit;
			yawGoal += axisDelta.x * 4f;
			pitchGoal = Mathf.Clamp(pitchGoal - axisDelta.y * 4f, -80f, 80f);
			return;
		}
		Touch t0 = Input.GetTouch(0);
		Touch t1 = Input.GetTouch(1);
		if (!pixelRect.Contains(t0.position) && !pixelRect.Contains(t1.position))
		{
			return;
		}
		float pinchNotches = TouchInputHelper.PinchNotches;
		if (Mathf.Abs(pinchNotches) > 0.001f)
		{
			distanceGoal = Mathf.Clamp(distanceGoal * Mathf.Exp(-pinchNotches * 3f), rbs * MinimumDistanceRadii, rbs * MaximumDistanceRadii);
		}
		Vector2 axisPan = TouchInputHelper.TwoFingerAxisDelta;
		if (axisPan.sqrMagnitude > 1e-06f)
		{
			targetGoal -= (base.transform.right * axisPan.x + base.transform.up * axisPan.y) * distance * 0.06f;
		}
	}

	private void ApplyOrbitPose()
	{
		Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
		Transform transform = base.transform;
		transform.position = target + rotation * new Vector3(0f, 0f, -distance);
		base.transform.rotation = rotation;
	}

	private void ApplyRadarPose()
	{
		Camera viewCamera = ViewCamera;
		viewCamera.orthographic = true;
		viewCamera.orthographicSize = rbs * 1.1f;
		viewCamera.nearClipPlane = rbs * 0.01f;
		viewCamera.farClipPlane = rbs * 100f;
		Transform transform = base.transform;
		transform.position = center + radarDirection * (rbs * 3f);
		base.transform.rotation = Quaternion.LookRotation(-radarDirection, radarUp);
	}
}
