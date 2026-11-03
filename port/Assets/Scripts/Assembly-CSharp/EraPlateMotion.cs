using UnityEngine;

public class EraPlateMotion : MonoBehaviour
{
	public EraPlateMotion()
	{
		lifetimeS = 6f;
		spinDegS = 90f;
		spinAxis = Vector3.right;
		launchDilation = 1f;
	}

	[Header("Показ")]
	public float lifetimeS;

	public float spinDegS;

	public bool dynamicBody;

	public Collider ownPlateCollider;

	private Vector3 velocityWU;

	private Vector3 spinAxis;

	private float aliveS;

	private bool running;

	private float launchDilation;

	public Vector3 VelocityWorld => velocityWU;

	public void Launch(Vector3 velocityWorldPerSecond, Vector3 dir)
	{
		velocityWU = velocityWorldPerSecond;
		launchDilation = 1f;
		if (PenetrationPlayer.Instance != null)
		{
			launchDilation = Mathf.Max(PenetrationPlayer.Instance.CurrentTimeDilation, 1E-06f);
		}
		Vector3 vector = Vector3.Cross(dir, Vector3.up);
		spinAxis = ((vector.sqrMagnitude > 1E-06f) ? vector.normalized : Vector3.right);
		aliveS = 0f;
		running = true;
		Collider component = GetComponent<Collider>();
		Rigidbody component2 = GetComponent<Rigidbody>();
		if (component != null && component2 == null)
		{
			component2 = base.gameObject.AddComponent<Rigidbody>();
			component2.useGravity = false;
			component2.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
		}
		if (component2 != null)
		{
			component2.isKinematic = !dynamicBody;
			if (dynamicBody)
			{
				component2.linearVelocity = velocityWU;
				component2.angularVelocity = spinAxis * (spinDegS * Mathf.Deg2Rad);
				float num = Mathf.Max(base.transform.lossyScale.x, base.transform.lossyScale.z) * 0.5f;
				component.contactOffset = Fragment.ContactOffsetFor(num);
				component2.maxDepenetrationVelocity = Mathf.Max(velocityWU.magnitude, 0.001f);
			}
		}
		if (component != null && ownPlateCollider != null)
		{
			Physics.IgnoreCollision(component, ownPlateCollider, true);
		}
	}

	private void SyncToShowTimeScale()
	{
		if (PenetrationPlayer.Instance == null)
		{
			return;
		}
		if (launchDilation <= 1E-09f)
		{
			return;
		}
		float num = Mathf.Max(PenetrationPlayer.Instance.CurrentTimeDilation, 1E-06f);
		if (Mathf.Abs(num - launchDilation) < 1E-09f)
		{
			return;
		}
		float num2 = Mathf.Clamp(launchDilation / num, 0.5f, 2f);
		velocityWU *= num2;
		launchDilation /= num2;
		Rigidbody component = GetComponent<Rigidbody>();
		if (component != null && !component.isKinematic)
		{
			component.linearVelocity *= num2;
		}
		spinDegS *= num2;
	}

	private void Update()
	{
		if (!running)
		{
			return;
		}
		SyncToShowTimeScale();
		if (!dynamicBody)
		{
			base.transform.position += velocityWU * Time.deltaTime;
			base.transform.Rotate(spinAxis, spinDegS * Time.deltaTime, Space.Self);
		}
		aliveS += Time.deltaTime;
	}
}