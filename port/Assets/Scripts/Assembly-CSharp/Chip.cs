using UnityEngine;

public class Chip : MonoBehaviour
{
	private Vector3 vel;

	private Vector3 gravity;

	private float life;

	private float age;

	private Vector3 baseScale;

	private float spin;

	private Vector3 spinAxis;

	private float maxSpeed = float.PositiveInfinity;

	public void Init(Vector3 velWorld, Vector3 gravityWorld, float lifetime)
	{
		vel = velWorld;
		maxSpeed = Mathf.Max(velWorld.magnitude, 1e-06f);
		gravity = gravityWorld;
		life = Mathf.Max(0.1f, lifetime);
		age = 0f;
		baseScale = base.transform.localScale;
		spin = Random.Range(180f, 720f);
		spinAxis = Random.onUnitSphere;
		RenderedSpeedAudit.Register(base.transform, GetComponent<Renderer>(), "стружка");
	}

	private void Update()
	{
		float dt = Time.deltaTime;
		age += dt;
		vel += gravity * dt;
		float speed = vel.magnitude;
		if (speed > maxSpeed && speed > 1e-09f)
		{
			vel *= maxSpeed / speed;
		}
		base.transform.position += vel * dt;
		base.transform.Rotate(spinAxis, spin * dt, Space.Self);
		float t = age / life;
		if (t >= 1f)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		if (t > 0.7f)
		{
			float k = 1f - (t - 0.7f) / 0.3f;
			base.transform.localScale = baseScale * k;
		}
	}
}
