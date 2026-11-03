using UnityEngine;

public sealed class AeroOrbit : MonoBehaviour
{
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
	}
}
