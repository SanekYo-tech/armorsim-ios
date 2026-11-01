using System;
using UnityEngine;

public static class CrewVisualAsset
{
	public const string ResourcePath = "Models/Crew/SeatedCrew";

	public const string AssetPath = "Assets/Resources/Models/Crew/SeatedCrew.fbx";

	public const string ShellGroup = "Shell";

	public const string OrgansGroup = "Organs";

	public const string DetailsGroup = "Details";

	public const float BoundsToleranceM = 0.002f;

	public const string RepresentationNotice = "Детальная геометрия — визуальное представление (ASSUMED). Вход в тело определяется оболочкой, попадания в органы — упрощёнными объёмами. Рёбра и сосуды отдельно не рассчитываются. Красный цвет обозначает задетый орган, а не тяжесть травмы.";

	private static GameObject validatedAsset;

	public static Vector3 MeterToFigureScale => new Vector3(1.6666666f, 0.7692308f, 1.4285714f);

	public static GameObject LoadRequired()
	{
		GameObject gameObject = Resources.Load<GameObject>(ResourcePath);
		if (gameObject != null && gameObject == validatedAsset)
		{
			return gameObject;
		}
		string text;
		if (Validate(gameObject, out text))
		{
			validatedAsset = gameObject;
			return gameObject;
		}
		throw new InvalidOperationException("Crew visual asset: " + text);
	}

	public static int OrganIndex(Transform node, Transform organsRoot)
	{
		while (node != null && node != organsRoot)
		{
			for (int i = 0; i < CrewAnatomy.Organs.Length; i++)
			{
				if (node.name == CrewAnatomy.Organs[i].name)
				{
					return i;
				}
			}
			node = node.parent;
		}
		return -1;
	}

	public static bool Validate(GameObject asset, out string error)
	{
		if (asset == null)
		{
			return Fail("Missing Assets/Resources/Models/Crew/SeatedCrew.fbx. Re-export the Blender model; no primitive fallback is used.", out error);
		}
		Transform root = asset.transform;
		Vector3 localPosition = root.localPosition;
		if (1E-08f < localPosition.sqrMagnitude)
		{
			return Fail("FBX root must have identity transform (metres, +Y up, +Z facing).", out error);
		}
		Quaternion localRotation = root.localRotation;
		float num = Mathf.Abs(Quaternion.Dot(Quaternion.identity, localRotation));
		num = Mathf.Min(num, 1f);
		if (num <= 0.999999f && 0.01f < Mathf.Acos(num) * 2f * Mathf.Rad2Deg)
		{
			return Fail("FBX root must have identity transform (metres, +Y up, +Z facing).", out error);
		}
		Vector3 localScale = root.localScale;
		if (1E-08f < (localScale - Vector3.one).sqrMagnitude)
		{
			return Fail("FBX root must have identity transform (metres, +Y up, +Z facing).", out error);
		}
		Transform shell = root.Find(ShellGroup);
		Transform organs = root.Find(OrgansGroup);
		Transform details = root.Find(DetailsGroup);
		if (shell == null || organs == null || details == null)
		{
			return Fail("FBX needs direct children Shell, Organs and Details.", out error);
		}
		Collider[] colliders = asset.GetComponentsInChildren<Collider>(true);
		if (colliders.Length != 0)
		{
			return Fail("Visual meshes must not own colliders or rigidbodies; ArmorPlate owns the broadphase.", out error);
		}
		Rigidbody[] rigidbodies = asset.GetComponentsInChildren<Rigidbody>(true);
		if (rigidbodies.Length != 0)
		{
			return Fail("Visual meshes must not own colliders or rigidbodies; ArmorPlate owns the broadphase.", out error);
		}
		Renderer[] renderers = asset.GetComponentsInChildren<Renderer>(true);
		int num2 = 0;
		int num3 = 0;
		int[] array = new int[CrewAnatomy.Organs.Length];
		for (int i = 0; i < renderers.Length; i++)
		{
			Renderer renderer = renderers[i];
			MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
			if (!(renderer is MeshRenderer) || meshFilter == null)
			{
				return Fail(renderer.name + " must contain a nonempty static mesh.", out error);
			}
			if (meshFilter.sharedMesh == null || meshFilter.sharedMesh.vertexCount == 0)
			{
				return Fail(renderer.name + " must contain a nonempty static mesh.", out error);
			}
			if (renderer.sharedMaterials.Length != meshFilter.sharedMesh.subMeshCount)
			{
				return Fail(renderer.name + " has incomplete material slots.", out error);
			}
			Material[] materials = renderer.sharedMaterials;
			for (int j = 0; j < materials.Length; j++)
			{
				if (materials[j] == null)
				{
					return Fail(renderer.name + " has a missing material.", out error);
				}
			}
			Bounds bounds = BoundsInRoot(meshFilter, root);
			Bounds bounds2 = new Bounds(Vector3.zero, new Vector3(0.3f, 0.65f, 0.35f));
			if (!Contains(bounds2, bounds))
			{
				return Fail(renderer.name + " exceeds the 0.6 x 1.3 x 0.7 m crew broadphase. Check FBX axes/units.", out error);
			}
			Transform transform = renderer.transform;
			if (transform.IsChildOf(shell))
			{
				if (!meshFilter.sharedMesh.isReadable)
				{
					return Fail(renderer.name + " requires Read/Write for exact shell impact tracing.", out error);
				}
				num2++;
			}
			else if (transform.IsChildOf(details))
			{
				num3++;
			}
			else if (transform.IsChildOf(organs))
			{
				int num4 = OrganIndex(transform, organs);
				if (num4 < 0)
				{
					return Fail(renderer.name + " is in Organs but has no known Organ_* ancestor.", out error);
				}
				CrewOrganBox organ = CrewAnatomy.Organs[num4];
				Bounds bounds3 = new Bounds(new Vector3(organ.pos.x * 0.6f, organ.pos.y * 1.3f, organ.pos.z * 0.4f), new Vector3(organ.size.x * 0.6f * 0.5f, organ.size.y * 1.3f * 0.5f, organ.size.z * 0.4f * 0.5f));
				if (Contains(bounds3, bounds))
				{
					array[num4]++;
				}
				else
				{
					return Fail(renderer.name + " extends beyond its " + organ.name + " proxy; put extra vessels in Details.", out error);
				}
			}
			else
			{
				return Fail(renderer.name + " is outside Shell, Organs or Details.", out error);
			}
		}
		if (num2 != 0 && num3 != 0)
		{
			for (int k = 0; k < array.Length; k++)
			{
				if (array[k] == 0)
				{
					return Fail("Missing geometry for " + CrewAnatomy.Organs[k].name + ".", out error);
				}
			}
			error = null;
			return true;
		}
		return Fail("Shell and Details must both contain geometry.", out error);
	}

	private static Bounds BoundsInRoot(MeshFilter filter, Transform root)
	{
		Bounds localBounds = filter.sharedMesh.bounds;
		Matrix4x4 matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
		Vector3 vector = matrix.MultiplyPoint3x4(localBounds.center);
		Vector3 vector2 = vector;
		Vector3 vector3 = vector;
		for (int i = 0; i < 8; i++)
		{
			Vector3 vector4 = new Vector3(localBounds.center.x + (((i & 1) != 0) ? (-1f) : 1f) * localBounds.extents.x, localBounds.center.y + (((i & 2) != 0) ? (-1f) : 1f) * localBounds.extents.y, localBounds.center.z + (((i & 4) != 0) ? (-1f) : 1f) * localBounds.extents.z);
			Vector3 vector5 = matrix.MultiplyPoint3x4(vector4);
			vector2 = Vector3.Min(vector2, vector5);
			vector3 = Vector3.Max(vector3, vector5);
		}
		return new Bounds((vector2 + vector3) * 0.5f, (vector3 - vector2) * 0.5f);
	}

	private static bool Contains(Bounds outer, Bounds inner)
	{
		outer.Expand(BoundsToleranceM * 2f);
		return outer.Contains(inner.min) && outer.Contains(inner.max);
	}

	private static bool Fail(string message, out string error)
	{
		error = message;
		return false;
	}
}
