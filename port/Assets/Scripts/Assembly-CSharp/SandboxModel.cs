using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class SandboxModel : MonoBehaviour
{
	private sealed class SharedMeshes
	{
		public Mesh[] meshes;

		public int refs;
	}

	[NonSerialized]
	public PreparedModel prepared;

	[NonSerialized]
	public SuperMeshSource source;

	public readonly List<ArmorPlate.ArmorType> partMaterials = new List<ArmorPlate.ArmorType>();

	private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();

	private ArmorPlate _plate;

	private static readonly Dictionary<PreparedModel, SharedMeshes> _shared = new Dictionary<PreparedModel, SharedMeshes>();

	private PreparedModel _meshOwner;

	public ArmorPlate Plate
	{
		get
		{
			if (_plate == null)
			{
				_plate = GetComponent<ArmorPlate>();
			}
			return _plate;
		}
	}

	public float angleX
	{
		get
		{
			if (Plate != null)
			{
				return Plate.angleX;
			}
			return 0f;
		}
	}

	public float angleY
	{
		get
		{
			if (Plate != null)
			{
				return Plate.angleY;
			}
			return 0f;
		}
	}

	public float Scale
	{
		get
		{
			if (Plate != null && Plate.modelScale > 0f)
			{
				return Plate.modelScale;
			}
			return 1f;
		}
	}

	public string DisplayName
	{
		get
		{
			if (source != null && source.files.Count > 0)
			{
				string text = Path.GetFileName(source.files[0].path);
				string text2 = ((source.files.Count > 1) ? string.Format(" (+{0})", source.files.Count - 1) : "");
				return text + text2;
			}
			return base.name;
		}
	}

	public Matrix4x4 ModelToWorld => Matrix4x4.TRS(base.transform.position, base.transform.rotation, Vector3.one * Scale);

	public Vector3 SizeM
	{
		get
		{
			if (prepared == null)
			{
				return Vector3.zero;
			}
			return prepared.bounds.extents * 2f * Scale;
		}
	}

	public static List<SandboxModel> All()
	{
		return new List<SandboxModel>(Object.FindObjectsByType<SandboxModel>(FindObjectsSortMode.InstanceID));
	}

	public static SandboxModel Create(PreparedModel pm, Vector3 worldPos, ArmorPlate.ArmorType defaultMaterial = ArmorPlate.ArmorType.RHA)
	{
		string name = ((pm.sources.Count > 0) ? Path.GetFileNameWithoutExtension(pm.sources[0].path) : "model");
		GameObject gameObject = new GameObject("Model_" + name);
		gameObject.transform.SetPositionAndRotation(worldPos, SectionFrame.PlateBase());
		SandboxModel sandboxModel = gameObject.AddComponent<SandboxModel>();
		sandboxModel.prepared = pm;
		SuperMeshSource superMeshSource = new SuperMeshSource();
		superMeshSource.files = new List<ModelSourceFile>(pm.sources);
		superMeshSource.contentSha = pm.contentSha;
		superMeshSource.settings = pm.settings;
		superMeshSource.partCount = pm.parts.Count;
		sandboxModel.source = superMeshSource;
		for (int i = 0; i < pm.parts.Count; i++)
		{
			sandboxModel.partMaterials.Add(defaultMaterial);
		}
		BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
		boxCollider.center = pm.bounds.center;
		boxCollider.size = Vector3.Max(pm.bounds.extents * 2f, Vector3.one * 0.001f);
		ArmorPlate armorPlate = gameObject.AddComponent<ArmorPlate>();
		armorPlate.kind = ArmorPlate.ObjectKind.Model;
		armorPlate.armorType = defaultMaterial;
		armorPlate.SetModelScale(1f);
		sandboxModel._plate = armorPlate;
		sandboxModel.BuildParts();
		armorPlate.ResetColor();
		SuperMeshStore.Register(pm);
		return sandboxModel;
	}

	public static SandboxModel Duplicate(SandboxModel src, Vector3 worldOffset)
	{
		if (src == null || src.prepared == null)
		{
			return null;
		}
		ArmorPlate.ArmorType defaultMaterial = ((src.Plate != null) ? src.Plate.armorType : ArmorPlate.ArmorType.RHA);
		SandboxModel sandboxModel = Create(src.prepared, src.transform.position + worldOffset, defaultMaterial);
		if (src.source != null)
		{
			sandboxModel.source = src.source;
		}
		if (src.Plate != null)
		{
			sandboxModel.Plate.SetAngleX(src.Plate.angleX);
			sandboxModel.Plate.SetAngleY(src.Plate.angleY);
			sandboxModel.Plate.SetModelScale(src.Plate.modelScale);
			sandboxModel.Plate.edgeClamp = src.Plate.edgeClamp;
		}
		sandboxModel.transform.rotation = src.transform.rotation;
		return sandboxModel;
	}

	private static Mesh[] Acquire(PreparedModel pm)
	{
		if (!_shared.TryGetValue(pm, out SharedMeshes value) || value.meshes == null)
		{
			Mesh[] array = new Mesh[pm.parts.Count];
			for (int i = 0; i < pm.parts.Count; i++)
			{
				ModelPart modelPart = pm.parts[i];
				Mesh mesh = new Mesh();
				mesh.name = modelPart.name;
				if (modelPart.vertices.Length > 65000)
				{
					mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
				}
				mesh.vertices = modelPart.vertices;
				mesh.triangles = modelPart.triangles;
				mesh.RecalculateNormals();
				mesh.RecalculateBounds();
				array[i] = mesh;
			}
			value = new SharedMeshes();
			value.meshes = array;
			value.refs = 0;
			_shared[pm] = value;
		}
		value.refs++;
		return value.meshes;
	}

	private static void Release(PreparedModel pm)
	{
		if (_shared.TryGetValue(pm, out SharedMeshes value))
		{
			value.refs--;
			if (value.refs <= 0)
			{
				Mesh[] meshes = value.meshes;
				for (int i = 0; i < meshes.Length; i++)
				{
					if (meshes[i] != null)
					{
						Kill(meshes[i]);
					}
				}
				_shared.Remove(pm);
			}
		}
	}

	private void BuildParts()
	{
		_renderers.Clear();
		Mesh[] array = Acquire(prepared);
		_meshOwner = prepared;
		for (int i = 0; i < prepared.parts.Count; i++)
		{
			ModelPart modelPart = prepared.parts[i];
			GameObject gameObject = new GameObject("Part_" + i + "_" + modelPart.name);
			gameObject.transform.SetParent(base.transform, false);
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = array[i];
			MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = MaterialLibrary.Armor(MaterialOf(i));
			gameObject.AddComponent<BodyPaint>();
			_renderers.Add(meshRenderer);
		}
	}

	public ArmorPlate.ArmorType MaterialOf(int part)
	{
		if (Plate != null)
		{
			return Plate.armorType;
		}
		if (part >= 0 && part < partMaterials.Count)
		{
			return partMaterials[part];
		}
		return ArmorPlate.ArmorType.RHA;
	}

	public void SetPartMaterial(int part, ArmorPlate.ArmorType t)
	{
		if (part < 0 || part >= partMaterials.Count)
		{
			return;
		}
		partMaterials[part] = t;
		if (part == 0 && Plate != null)
		{
			Plate.SetArmorType(t);
		}
	}

	public void SyncPartMaterials()
	{
		ArmorPlate.ArmorType armorType = MaterialOf(0);
		for (int i = 0; i < partMaterials.Count; i++)
		{
			partMaterials[i] = armorType;
		}
	}

	public void SetAngles(float ax, float ay)
	{
		if (Plate == null)
		{
			return;
		}
		Plate.SetAngleX(ax);
		Plate.SetAngleY(ay);
	}

	private static void Kill(UnityEngine.Object o)
	{
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(o);
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(o);
		}
	}

	private void OnDestroy()
	{
		if (_meshOwner != null)
		{
			if (_shared.TryGetValue(_meshOwner, out SharedMeshes value))
			{
				value.refs--;
				if (value.refs <= 0)
				{
					Mesh[] meshes = value.meshes;
					for (int i = 0; i < meshes.Length; i++)
					{
						if (meshes[i] != null)
						{
							Kill(meshes[i]);
						}
					}
					_shared.Remove(_meshOwner);
				}
			}
		}
		_meshOwner = null;
	}
}
