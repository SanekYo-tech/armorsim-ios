using System.Collections.Generic;
using UnityEngine;

public class WarheadDetonation : MonoBehaviour
{
	public float casingRadialSpeedMS;

	[Header("Куски корпуса")]
	public bool spawnCasingPieces;

	public int casingPieces;

	public float pieceLifetimeS;

	public float pieceSpinDegS;

	public bool piecesHitEachOther;

	private static readonly List<GameObject> spawned;

	private static readonly List<Renderer> hiddenRenderers;

	private static readonly List<Collider> disabledColliders;

	private static float pieceSpinDegSStatic;

	private static bool mutualCollision;

	static WarheadDetonation()
	{
		spawned = new List<GameObject>();
		hiddenRenderers = new List<Renderer>();
		disabledColliders = new List<Collider>();
	}

	public static void RestoreBody()
	{
		foreach (GameObject item in spawned)
		{
			if (item != null)
			{
				item.SetActive(true);
			}
		}
		spawned.Clear();
		foreach (Renderer item2 in hiddenRenderers)
		{
			if (item2 != null)
			{
				item2.enabled = true;
			}
		}
		hiddenRenderers.Clear();
	}

	public static void Detonate(Transform body, Vector3 forward, float forwardVelocityMS, float bodyDiameterMM, float bodyLengthMM, float radialSpeedMS, int pieces, float lifetimeS, float worldPerMeter, float timeDilation, Color casingColor, bool spawnPieces)
	{
		if (body == null)
		{
			return;
		}
		Renderer[] componentsInChildren = body.GetComponentsInChildren<Renderer>();
		foreach (Renderer renderer in componentsInChildren)
		{
			if (renderer.enabled)
			{
				renderer.enabled = false;
				hiddenRenderers.Add(renderer);
			}
		}
		Collider[] componentsInChildren2 = body.GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren2)
		{
			if (collider.enabled)
			{
				collider.enabled = false;
				disabledColliders.Add(collider);
			}
		}
		if (!spawnPieces || pieces <= 0)
		{
			return;
		}
		float num = SceneScale.RodDiameterWorld(bodyDiameterMM) * 0.5f;
		float num2 = SceneScale.RodDiameterWorld(bodyLengthMM);
		Vector3 vector = ((Mathf.Abs(Vector3.Dot(Vector3.up, forward)) > 0.9f) ? Vector3.right : Vector3.up);
		Vector3 vector2 = Vector3.Cross(vector, forward).normalized;
		Vector3 vector3 = Vector3.Cross(vector2, forward).normalized;
		int num3 = Mathf.Max(1, pieces >> 2);
		int num4 = pieces / num3;
		if (num4 < 3)
		{
			num4 = 3;
		}
		List<Collider> list = new List<Collider>(num4 * num3);
		for (int i = 0; i < num3; i++)
		{
			float num5 = ((num3 == 1) ? 0.5f : ((float)i / (float)(num3 - 1)));
			num5 = (num5 - 0.5f) * num2;
			Vector3 vector4 = body.position + forward * num5;
			float num6 = Mathf.PI / (float)num4;
			float num7 = num2 / (float)num3 * 0.5f;
			for (int j = 0; j < num4; j++)
			{
				float f = ((float)j + 0.5f) / (float)num4 * Mathf.PI * 2f;
				Vector3 vector5 = (vector3 * Mathf.Sin(f) + vector2 * Mathf.Cos(f)).normalized;
				float num8 = Random.Range(0.1f, 0.16f) * num;
				float num9 = Random.Range(0.7f, 0.9f) * num6;
				float num10 = Random.Range(0.72f, 0.98f) * num7;
				int seed = Random.Range(1, 999999);
				Mesh mesh = MeshShapes.CasingPetal(num - num8, num8, num9, num10, 6, 5, seed);
				GameObject gameObject = new GameObject("CasingPiece", typeof(MeshFilter), typeof(MeshRenderer));
				gameObject.transform.position = vector4 + vector5 * (num - num8 * 0.5f);
				gameObject.transform.rotation = Quaternion.LookRotation(forward, vector5);
				gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
				MeshRenderer component = gameObject.GetComponent<MeshRenderer>();
				component.sharedMaterial = new Material(MaterialLibrary.Armor(ArmorPlate.ArmorType.RHA));
				component.material.color = casingColor;
				MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
				meshCollider.sharedMesh = mesh;
				meshCollider.convex = true;
				Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
				rigidbody.useGravity = false;
				rigidbody.collisionDetectionMode = (CollisionDetectionMode)2;
				rigidbody.linearVelocity = (vector5 * radialSpeedMS + forward * forwardVelocityMS) * timeDilation / Mathf.Max(worldPerMeter, 1E-06f);
				rigidbody.angularVelocity = Random.onUnitSphere * pieceSpinDegSStatic * Mathf.Deg2Rad;
				meshCollider.contactOffset = Fragment.ContactOffsetFor(num8 * 0.5f);
				rigidbody.maxDepenetrationVelocity = Mathf.Max(rigidbody.linearVelocity.magnitude, 0.001f);
				list.Add(meshCollider);
				Object.Destroy(gameObject, lifetimeS);
				spawned.Add(gameObject);
			}
		}
		if (!mutualCollision)
		{
			return;
		}
		for (int k = 0; k < list.Count; k++)
		{
			for (int l = k + 1; l < list.Count; l++)
			{
				Physics.IgnoreCollision(list[k], list[l], true);
			}
		}
	}

	public void DetonateFrom(Transform body, Vector3 forward, float forwardVelocityMS, float bodyDiameterMM, float bodyLengthMM, float worldPerMeter, float timeDilation, Color casingColor)
	{
		pieceSpinDegSStatic = pieceSpinDegS;
		mutualCollision = piecesHitEachOther;
		Detonate(body, forward, forwardVelocityMS, bodyDiameterMM, bodyLengthMM, casingRadialSpeedMS, casingPieces, pieceLifetimeS, worldPerMeter, timeDilation, casingColor, spawnCasingPieces);
	}

	public static void ClearAll()
	{
		foreach (GameObject item in spawned)
		{
			if (item != null)
			{
				Object.Destroy(item);
			}
		}
		spawned.Clear();
	}
}
