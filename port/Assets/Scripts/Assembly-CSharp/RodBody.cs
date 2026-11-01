using System.Collections.Generic;
using UnityEngine;

public sealed class RodBody : MonoBehaviour
{
	private const string ChildName = "RodBody";

	public const int Segments = 32;

	[SerializeField]
	private GameObject _visual;

	[SerializeField]
	private CapsuleCollider _capsule;

	[SerializeField]
	private List<Collider> _disabled = new List<Collider>();

	private static Mesh _unitCylinder;

	public static void Refresh(ArmorPlate plate)
	{
		if (plate == null)
		{
			return;
		}
		RodBody rodBody = plate.GetComponent<RodBody>();
		if (plate.IsRod)
		{
			if (rodBody == null)
			{
				rodBody = plate.gameObject.AddComponent<RodBody>();
			}
			rodBody.Build(plate);
		}
		else if (rodBody != null)
		{
			rodBody.Clear(plate);
		}
	}

	private void Build(ArmorPlate plate)
	{
		if (_visual == null)
		{
			Transform transform = base.transform.Find(ChildName);
			if (transform != null)
			{
				_visual = transform.gameObject;
			}
		}
		if (_visual == null)
		{
			GameObject gameObject = new GameObject(ChildName, typeof(MeshFilter), typeof(MeshRenderer), typeof(BodyPaint));
			_visual = gameObject;
			gameObject.transform.SetParent(base.transform, false);
		}
		_visual.transform.localPosition = Vector3.zero;
		_visual.transform.localRotation = Quaternion.identity;
		_visual.transform.localScale = Vector3.one;
		_visual.GetComponent<MeshFilter>().sharedMesh = UnitCylinder();
		_visual.GetComponent<MeshRenderer>().sharedMaterial = new Material(MaterialLibrary.Armor(plate.armorType));
		Renderer renderer = GetComponent<Renderer>();
		if (renderer != null)
		{
			renderer.enabled = false;
		}
		Collider[] components = GetComponents<Collider>();
		for (int i = 0; i < components.Length; i++)
		{
			Collider collider = components[i];
			if (collider == null || collider == _capsule || !collider.enabled)
			{
				continue;
			}
			collider.enabled = false;
			if (!_disabled.Contains(collider))
			{
				_disabled.Add(collider);
			}
		}
		if (_capsule == null)
		{
			_capsule = base.gameObject.AddComponent<CapsuleCollider>();
		}
		_capsule.direction = 0;
		_capsule.radius = 0.5f;
		_capsule.height = 1f;
		_capsule.center = Vector3.zero;
		_capsule.enabled = true;
	}

	private void Clear(ArmorPlate plate)
	{
		if (_visual != null)
		{
			Kill(_visual);
			_visual = null;
		}
		if (_capsule != null)
		{
			Kill(_capsule);
			_capsule = null;
		}
		foreach (Collider collider in _disabled)
		{
			if (collider != null)
			{
				collider.enabled = true;
			}
		}
		_disabled.Clear();
		bool skip = plate.IsEraPlate || plate.IsNeraPlate || plate.IsCrew || plate.IsModel;
		Renderer renderer = GetComponent<Renderer>();
		if (renderer != null && !skip)
		{
			renderer.enabled = true;
		}
		if (Application.isPlaying)
		{
			Object.Destroy(this);
		}
		else
		{
			Object.DestroyImmediate(this);
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

	public static Mesh UnitCylinder()
	{
		if (_unitCylinder != null)
		{
			return _unitCylinder;
		}
		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();
		for (int i = 0; i <= Segments; i++)
		{
			float a = (float)i * Mathf.PI * 2f / (float)Segments;
			verts.Add(new Vector3(-0.5f, 0.5f * Mathf.Cos(a), 0.5f * Mathf.Sin(a)));
			verts.Add(new Vector3(0.5f, 0.5f * Mathf.Cos(a), 0.5f * Mathf.Sin(a)));
		}
		for (int i = 0; i < Segments; i++)
		{
			tris.Add(2 * i);
			tris.Add(2 * i + 2);
			tris.Add(2 * i + 1);
			tris.Add(2 * i + 1);
			tris.Add(2 * i + 2);
			tris.Add(2 * i + 3);
		}
		for (int cap = 0; cap < 2; cap++)
		{
			float x = (cap == 0) ? (-0.5f) : 0.5f;
			int c = verts.Count;
			verts.Add(new Vector3(x, 0f, 0f));
			for (int j = 0; j <= Segments; j++)
			{
				float a = (float)j * Mathf.PI * 2f / (float)Segments;
				verts.Add(new Vector3(x, 0.5f * Mathf.Cos(a), 0.5f * Mathf.Sin(a)));
			}
			for (int j = 0; j < Segments; j++)
			{
				tris.Add(c);
				if (cap == 0)
				{
					tris.Add(c + j + 2);
					tris.Add(c + j + 1);
				}
				else
				{
					tris.Add(c + j + 1);
					tris.Add(c + j + 2);
				}
			}
		}
		Mesh mesh = new Mesh();
		mesh.name = "RodUnitCylinder";
		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0, false);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		_unitCylinder = mesh;
		return mesh;
	}
}
