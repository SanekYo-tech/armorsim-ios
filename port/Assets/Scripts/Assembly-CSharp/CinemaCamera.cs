using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class CinemaCamera : MonoBehaviour
{
	[Header("Кино-камера (тумблер)")]
	public bool cinematic;

	[Header("Слежение")]
	public float followLag;

	public float backOffset;

	public float sideOffset;

	public float heightOffset;

	private Camera cam;

	private OrbitCamera orbit;

	private Projectile projectile;

	private Vector3 vel;

	public static void Bootstrap()
	{
		Camera camera = Camera.main;
		if (camera == null)
		{
			camera = Object.FindFirstObjectByType<Camera>();
		}
		if (camera != null && camera.GetComponent<CinemaCamera>() == null)
		{
			camera.gameObject.AddComponent<CinemaCamera>();
		}
	}

	private void Awake()
	{
		cam = GetComponent<Camera>();
		orbit = GetComponent<OrbitCamera>();
	}

	private void LateUpdate()
	{
		if (orbit != null && orbit.enabled == cinematic)
		{
			orbit.enabled = !cinematic;
		}
		if (!cinematic)
		{
			return;
		}
		if (projectile == null)
		{
			projectile = Object.FindFirstObjectByType<Projectile>();
		}
		if (projectile == null)
		{
			return;
		}
		Vector3 forward = projectile.transform.forward;
		if (forward.sqrMagnitude < 1e-06f)
		{
			forward = Vector3.zero;
		}
		Vector3 vector = projectile.transform.position;
		Vector3 right = Vector3.Cross(forward, Vector3.up);
		right = ((right.magnitude > 1e-05f) ? (right / right.magnitude) : Vector3.forward);
		Vector3 vector2 = vector - forward * backOffset + right * sideOffset + Vector3.up * heightOffset;
		transform.position = Vector3.SmoothDamp(transform.position, vector2, ref vel, followLag);
		Vector3 vector3 = vector - transform.position;
		vector3 = ((vector3.magnitude > 1e-05f) ? (vector3 / vector3.magnitude) : Vector3.forward);
		Quaternion quaternion = Quaternion.LookRotation(vector3, Vector3.up);
		float num = Mathf.Max(followLag, 0.001f);
		transform.rotation = Quaternion.Slerp(transform.rotation, quaternion, 1f - MathF.Exp(-Time.deltaTime / num));
	}

	public CinemaCamera()
	{
		followLag = 0.25f;
		backOffset = SceneScale.FromM(0.35f);
		sideOffset = SceneScale.FromM(0.22f);
		heightOffset = SceneScale.FromM(0.14f);
	}
}
