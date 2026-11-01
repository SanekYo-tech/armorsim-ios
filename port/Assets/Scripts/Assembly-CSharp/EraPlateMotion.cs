using UnityEngine;

public class EraPlateMotion : MonoBehaviour
{
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

	public Vector3 VelocityWorld => default;

	public void Launch(Vector3 velocityWorldPerSecond, Vector3 dir)
	{
	}

	private void SyncToShowTimeScale()
	{
	}

	private void Update()
	{
	}
}
