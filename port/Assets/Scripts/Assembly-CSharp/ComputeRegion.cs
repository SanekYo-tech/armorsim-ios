using System.Collections.Generic;
using UnityEngine;

public sealed class ComputeRegion : MonoBehaviour
{
	public const float DefaultSizeM = 0.4f;

	public const float MinSizeM = 0.02f;

	public const float MaxSizeM = 100f;

	public float widthM = DefaultSizeM;

	public float heightM = DefaultSizeM;

	public float depthM = DefaultSizeM;

	public float angleX;

	public float angleY;

	private Mesh _wire;

	public static List<ComputeRegion> All()
	{
		return new List<ComputeRegion>(Object.FindObjectsByType<ComputeRegion>(FindObjectsSortMode.InstanceID));
	}

	public static ComputeRegion Create(Vector3 worldPos)
	{
		GameObject gameObject = new GameObject("ComputeRegion");
		ComputeRegion computeRegion = gameObject.AddComponent<ComputeRegion>();
		gameObject.transform.position = worldPos;
		computeRegion.ApplyRotation();
		computeRegion.Rebuild();
		return computeRegion;
	}

	public void SetSize(float w, float h, float d)
	{
		widthM = Mathf.Clamp(w, MinSizeM, MaxSizeM);
		heightM = Mathf.Clamp(h, MinSizeM, MaxSizeM);
		depthM = Mathf.Clamp(d, MinSizeM, MaxSizeM);
		Rebuild();
	}

	public void SetAngles(float ax, float ay)
	{
		angleX = ax;
		angleY = ay;
		ApplyRotation();
	}

	private void ApplyRotation()
	{
		base.transform.rotation = SectionFrame.PlateBase() * Quaternion.Euler(angleX, angleY, 0f);
	}

	public SuperClip ClipIn(SuperScenePose.BakerFrame f)
	{
		RegionBox regionBox = RegionBox.Of(this);
		Vector3 centre = f.Point(regionBox.centre);
		Vector3 u = f.Dir(regionBox.right);
		Vector3 v = f.Dir(regionBox.up);
		Vector3 n = f.Dir(regionBox.forward);
		if (n.z < 0f)
		{
			u = -u;
			n = -n;
		}
		return SuperClip.Box(centre, u, v, n, regionBox.widthM, regionBox.heightM, regionBox.depthM);
	}

	public void WorldAxes(SuperScenePose.BakerFrame f, out Vector3 u, out Vector3 v, out Vector3 n)
	{
		RegionBox regionBox = RegionBox.Of(this);
		u = regionBox.right;
		v = regionBox.up;
		n = regionBox.forward;
		if (f.Dir(regionBox.forward).z < 0f)
		{
			u = -u;
			n = -n;
		}
	}

	private void Rebuild()
	{
		MeshFilter meshFilter = GetComponent<MeshFilter>();
		if (meshFilter == null)
		{
			meshFilter = base.gameObject.AddComponent<MeshFilter>();
		}
		MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
		if (meshRenderer == null)
		{
			meshRenderer = base.gameObject.AddComponent<MeshRenderer>();
		}
		if (_wire == null)
		{
			_wire = new Mesh();
			_wire.name = "ComputeRegionWire";
		}
		Vector3[] array = new Vector3[8];
		Vector3 vector = new Vector3(widthM * 0.5f, heightM * 0.5f, depthM * 0.5f);
		for (int i = 0; i < 8; i++)
		{
			array[i] = new Vector3(((i & 1) == 0) ? (0f - vector.x) : vector.x, ((i & 2) == 0) ? (0f - vector.y) : vector.y, ((i & 4) == 0) ? (0f - vector.z) : vector.z);
		}
		int[] array2 = new int[24]
		{
			0, 1, 2, 3, 4, 5, 6, 7,
			0, 2, 1, 3, 4, 6, 5, 7,
			0, 4, 1, 5, 2, 6, 3, 7
		};
		_wire.Clear();
		_wire.vertices = array;
		_wire.SetIndices(array2, MeshTopology.Lines, 0);
		_wire.RecalculateBounds();
		meshFilter.sharedMesh = _wire;
		if (meshRenderer.sharedMaterial == null)
		{
			Shader shader = Shader.Find("Unlit/Color");
			if (shader == null)
			{
				shader = Shader.Find("Standard");
			}
			Material material = new Material(shader);
			material.color = new Color(0.1f, 0.55f, 0.95f, 1f);
			meshRenderer.sharedMaterial = material;
		}
	}

	private static void Kill(Object o)
	{
		if (Application.isPlaying)
		{
			Object.Destroy(o);
		}
		else
		{
			Object.DestroyImmediate(o);
		}
	}

	private void OnDestroy()
	{
		if (_wire != null)
		{
			Kill(_wire);
		}
		MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
		if (meshRenderer != null && meshRenderer.sharedMaterial != null)
		{
			Kill(meshRenderer.sharedMaterial);
		}
	}
}
