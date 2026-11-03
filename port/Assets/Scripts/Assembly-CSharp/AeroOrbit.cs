using UnityEngine;
using UnityEngine.EventSystems;

public sealed class AeroOrbit : MonoBehaviour
{
	public AeroOrbit()
	{
		yaw = -32f;
		pitch = 20f;
		yawGoal = -32f;
		pitchGoal = 20f;
		distance = 9f;
		distanceGoal = 9f;
	}

	private Vector3 target;

	private Vector3 targetGoal;

	private float yaw;

	private float pitch;

	private float yawGoal;

	private float pitchGoal;

	private float distance;

	private float distanceGoal;

	public void Frame()
	{
		targetGoal = Vector3.zero;
		distanceGoal = 9f;
	}

	private void LateUpdate()
	{
		if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
		{
			if (Input.GetMouseButton(2))
			{
				float axis = Input.GetAxis("Mouse X");
				float axis2 = Input.GetAxis("Mouse Y");
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					Vector3 right = base.transform.right;
					Vector3 up = base.transform.up;
					targetGoal -= (up * axis2 + right * axis) * distance * 0.06f;
				}
				else
				{
					yawGoal += axis * 4f;
					pitchGoal = Mathf.Clamp(pitchGoal - axis2 * 4f, -80f, 80f);
				}
			}
			distanceGoal = Mathf.Clamp(distanceGoal * Mathf.Exp((0f - WheelZones.SceneWheel(Input.GetAxis("Mouse ScrollWheel"))) * 3f), 2.5f, 25f);
			if (Input.GetKeyDown(KeyCode.F))
			{
				Frame();
			}
		}
		float t = 1f - Mathf.Exp((0f - Time.unscaledDeltaTime) / 0.1f);
		yaw = Mathf.LerpAngle(yaw, yawGoal, t);
		pitch = Mathf.Lerp(pitch, pitchGoal, t);
		target = Vector3.Lerp(target, targetGoal, t);
		distance = Mathf.Lerp(distance, distanceGoal, t);
		Quaternion quaternion = Quaternion.Euler(pitch, yaw, 0f);
		base.transform.position = target + quaternion * new Vector3(0f, 0f, 0f - distance);
		base.transform.rotation = quaternion;
	}
}