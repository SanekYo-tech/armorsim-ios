using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MicroSpall : MonoBehaviour
{
	public static readonly bool EnabledInPlayback = false;

	public const int MaxLiveNormal = 2600;

	public const int MaxLiveAdvanced = 3000;

	public const float SizeFractionOfGrady = 0.405f;

	public const int PerGradyFragment = 1;

	public const float LifetimeS = 1f;

	public const float LifetimeJitterS = 0.15f;

	public const float FadeS = 0.3f;

	public const float ConeHalfDeg = 7f;

	private static int layer = -2;

	private static readonly List<GameObject> live = new List<GameObject>();

	private static bool cappedReported;

	private float life;

	private float age;

	private float realSpeedMps;

	private Vector3 baseScale;

	private Collider ownCol;

	private Collider ignoredSource;

	private MeshCollider ignoredSourceMesh;

	private DeformableMesh ignoredSourceDeform;

	private Vector3 tubePoint;

	private Vector3 tubeAxis;

	private float tubeRadiusWorld;

	public static int MaxLive
	{
		get
		{
			if (AppFlow.ActiveMode == SimMode.Advanced)
			{
				return MaxLiveAdvanced;
			}
			return MaxLiveNormal;
		}
	}

	public static int LiveCount => live.Count;

	private static int ResolveLayer()
	{
		if (layer != -2)
		{
			return layer;
		}
		layer = -1;
		for (int i = 8; i < 32; i++)
		{
			if (!string.IsNullOrEmpty(LayerMask.LayerToName(i)))
			{
				layer = i;
				break;
			}
		}
		if (layer < 0)
		{
			Debug.LogWarning("[МикроОсколки] Свободного слоя нет — категория ВЫКЛЮЧЕНА. Включать её без взаимного игнора нельзя: сотни перекрывающихся тел солвер разносит своей скоростью (см. RenderedSpeedAudit).");
		}
		else
		{
			Physics.IgnoreLayerCollision(layer, layer, true);
			Debug.Log(string.Format("[МикроОсколки] Взаимные контакты выключены на слое {0} ", layer) + "(с плитами и снарядом контакты ОСТАЮТСЯ).");
		}
		return layer;
	}

	public static bool AppliesTo(ArmorPlate.ArmorType t)
	{
		return CraterModel.RimClassOf(t) < CraterModel.RimClass.Composite;
	}

	public static void Burst(Vector3 origin, Vector3 dir, float speedVisual, float realSpeedMps, ArmorPlate plate, float channelRadiusM, int count, Color color)
	{
		if (!EnabledInPlayback || plate == null || count <= 0 || !AppliesTo(plate.armorType))
		{
			return;
		}
		int num = ResolveLayer();
		if (num < 0)
		{
			return;
		}
		Prune();
		int num2 = MaxLive - live.Count;
		if (num2 <= count)
		{
			count = num2;
			if (count < 1)
			{
				if (cappedReported)
				{
					return;
				}
				cappedReported = true;
				Debug.LogWarning(string.Format("[МикроОсколки] Потолок {0} живых достигнут — дальше не плодим ", MaxLive) + string.Format("(режим {0}). Это ЗАЩИТА КАДРА ", AppFlow.ActiveMode) + "(PLACEHOLDER): на расчёт, на глубину и на бюджет Грейди не влияет ничем.");
				return;
			}
		}
		Collider collider = plate.GetComponent<Collider>();
		channelRadiusM = Mathf.Max(channelRadiusM, 0f);
		GradyModel.FragMaterial material = GradyModel.ForArmor(plate.armorType);
		float num3 = Mathf.Max(GradyModel.CharacteristicSize(material, GradyModel.StrainRate) * SizeFractionOfGrady, 1e-4f);
		Vector3 vector = ((dir.y * dir.y + dir.x * dir.x + dir.z * dir.z <= 1e-9f) ? Vector3.forward : dir.normalized);
		Vector3 vector2 = Vector3.Cross(vector, Vector3.up);
		if (vector2.sqrMagnitude < 1e-6f)
		{
			vector2 = Vector3.Cross(vector, Vector3.right);
		}
		vector2 = vector2.normalized;
		Vector3 vector3 = Vector3.Cross(vector, vector2).normalized;
		float num4 = channelRadiusM - num3 * 0.5f;
		for (int i = 0; i < count; i++)
		{
			float num5 = Mathf.Max(num4, 0f);
			float num6 = Random.value * 2f * Mathf.PI;
			float num7 = Mathf.Sqrt(Random.value) * num5;
			Vector3 vector4 = origin + (vector2 * Mathf.Cos(num6) + vector3 * Mathf.Sin(num6)) * num7;
			GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			gameObject.name = "MicroSpall";
			gameObject.layer = num;
			gameObject.transform.position = vector4;
			gameObject.transform.localScale = Vector3.one * num3;
			MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = MaterialLibrary.Armor(plate.armorType);
			meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			SphereCollider sphereCollider = gameObject.GetComponent<SphereCollider>();
			sphereCollider.contactOffset = Fragment.ContactOffsetFor(num3 * 0.5f);
			Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
			rigidbody.mass = Mathf.Max(num3 * material.density * num3 * num3, 1e-6f);
			rigidbody.useGravity = false;
			rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			rigidbody.linearVelocity = RandomInCone(vector, ConeHalfDeg) * speedVisual;
			rigidbody.maxDepenetrationVelocity = Mathf.Max(rigidbody.linearVelocity.magnitude, 0.001f);
			if (collider != null)
			{
				Physics.IgnoreCollision(sphereCollider, collider, true);
			}
			MicroSpall microSpall = gameObject.AddComponent<MicroSpall>();
			microSpall.life = LifetimeS + Random.Range(-LifetimeJitterS, LifetimeJitterS);
			microSpall.realSpeedMps = realSpeedMps;
			microSpall.baseScale = gameObject.transform.localScale;
			microSpall.ownCol = sphereCollider;
			microSpall.ignoredSource = collider;
			microSpall.ignoredSourceMesh = collider as MeshCollider;
			microSpall.ignoredSourceDeform = ((collider != null) ? collider.GetComponent<DeformableMesh>() : null);
			microSpall.tubePoint = origin;
			microSpall.tubeAxis = vector;
			microSpall.tubeRadiusWorld = channelRadiusM;
			RenderedSpeedAudit.Register(gameObject.transform, meshRenderer, "микро-осколок");
			live.Add(gameObject);
		}
	}

	private static Vector3 RandomInCone(Vector3 axis, float halfDeg)
	{
		if (halfDeg <= 0f)
		{
			return axis;
		}
		float num = Random.value * Mathf.Tan(halfDeg * Mathf.Deg2Rad);
		Vector3 vector = Vector3.Cross(axis, Vector3.up);
		if (vector.sqrMagnitude < 1e-6f)
		{
			vector = Vector3.Cross(axis, Vector3.right);
		}
		vector = vector.normalized;
		Vector3 vector2 = Vector3.Cross(axis, vector).normalized;
		float num2 = Random.value * 2f * Mathf.PI;
		return (axis + (vector * Mathf.Cos(num2) + vector2 * Mathf.Sin(num2)) * num).normalized;
	}

	private static void Prune()
	{
		for (int num = live.Count - 1; num >= 0; num--)
		{
			if (live[num] == null)
			{
				live.RemoveAt(num);
			}
		}
	}

	public static void ClearAll()
	{
		for (int i = 0; i < live.Count; i++)
		{
			if (live[i] != null)
			{
				Object.Destroy(live[i]);
			}
		}
		live.Clear();
		cappedReported = false;
	}

	private void ReleaseSourceIgnore()
	{
		if (ignoredSource == null || ownCol == null)
		{
			return;
		}
		float num = Mathf.Max(baseScale.x, 1e-05f) * 0.5f;
		if (Fragment.SourceContactRestorable(ownCol, ignoredSource, ignoredSourceMesh, ignoredSourceDeform, base.transform.position, num, tubePoint, tubeAxis, tubeRadiusWorld))
		{
			Physics.IgnoreCollision(ownCol, ignoredSource, false);
			ignoredSource = null;
			ignoredSourceMesh = null;
			ignoredSourceDeform = null;
		}
	}

	private void Update()
	{
		age += Time.deltaTime;
		if (age >= life)
		{
			live.Remove(base.gameObject);
			Object.Destroy(base.gameObject);
			return;
		}
		ReleaseSourceIgnore();
		float num = Mathf.Clamp01((age - (life - FadeS)) / FadeS);
		base.transform.localScale = baseScale * (1f - num);
	}
}
