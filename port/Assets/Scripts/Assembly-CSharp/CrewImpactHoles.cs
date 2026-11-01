using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CrewImpactHoles : MonoBehaviour
{
	public struct BodyInterval
	{
		public float enterMm;

		public float exitMm;

		public Vector3 normal;

		public CrewRegion region;
	}

	private struct Triangle
	{
		public Vector3 a;

		public Vector3 b;

		public Vector3 c;

		public Triangle(Vector3 a, Vector3 b, Vector3 c)
		{
			this.a = a;
			this.b = b;
			this.c = c;
		}
	}

	private struct Crossing
	{
		public float t;

		public Vector3 normal;
	}

	public const int MaxHoles = 128;

	private readonly List<Triangle> triangles;

	private readonly List<Crossing> crossings;

	private readonly Vector4[] starts;

	private readonly Vector4[] ends;

	private Renderer[] renderers;

	private MaterialPropertyBlock block;

	private int count;

	public CrewImpactHoles()
	{
		triangles = new List<Triangle>();
		crossings = new List<Crossing>(64);
		starts = new Vector4[MaxHoles];
		ends = new Vector4[MaxHoles];
		renderers = Array.Empty<Renderer>();
	}

	private MaterialPropertyBlock Block
	{
		get
		{
			if (block == null)
			{
				block = new MaterialPropertyBlock();
			}
			return block;
		}
	}

	public int HoleCount => count;

	public bool HasSurface => triangles.Count > 0;

	public void Bind(Renderer[] shellRenderers)
	{
		if (shellRenderers == null)
		{
			shellRenderers = Array.Empty<Renderer>();
		}
		renderers = shellRenderers;
		triangles.Clear();
		count = 0;
		Shader shader = null;
		if (renderers.Length != 0)
		{
			shader = Resources.Load<Shader>("Shaders/CrewShellClip");
			if (shader == null)
			{
				throw new InvalidOperationException("Missing Shaders/CrewShellClip; crew holes require this shader.");
			}
		}
		for (int i = 0; i < renderers.Length; i++)
		{
			Renderer renderer = renderers[i];
			MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
			if (meshFilter == null || meshFilter.sharedMesh == null || !meshFilter.sharedMesh.isReadable)
			{
				throw new InvalidOperationException("Crew shell must have a readable static mesh: " + renderer.name);
			}
			Vector3[] array = meshFilter.sharedMesh.vertices;
			int[] array2 = meshFilter.sharedMesh.triangles;
			Matrix4x4 matrix = transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
			for (int j = 0; j < array.Length; j++)
			{
				array[j] = matrix.MultiplyPoint3x4(array[j]);
			}
			for (int k = 0; k < array2.Length; k += 3)
			{
				triangles.Add(new Triangle(array[array2[k]], array[array2[k + 1]], array[array2[k + 2]]));
			}
			Material[] sharedMaterials = renderer.sharedMaterials;
			for (int m = 0; m < sharedMaterials.Length; m++)
			{
				sharedMaterials[m].shader = shader;
			}
		}
		Push();
	}

	private static Vector3 ToMm(Vector3 local, Vector3 units)
	{
		return new Vector3(-local.x * units.x, local.y * units.y, -local.z * units.z);
	}

	public void BodyIntervals(Vector3 originMm, Vector3 directionMm, Vector3 dimensionsMm, List<BodyInterval> output)
	{
		output.Clear();
		crossings.Clear();
		foreach (Triangle triangle in triangles)
		{
			Vector3 vector = ToMm(triangle.a, dimensionsMm);
			Vector3 vector2 = ToMm(triangle.b, dimensionsMm);
			Vector3 vector3 = ToMm(triangle.c, dimensionsMm);
			Vector3 lhs = vector2 - vector;
			Vector3 rhs = vector3 - vector;
			Vector3 lhs2 = Vector3.Cross(directionMm, rhs);
			float num = Vector3.Dot(lhs, lhs2);
			if (Mathf.Abs(num) < 1E-08f)
			{
				continue;
			}
			float num2 = 1f / num;
			Vector3 lhs3 = originMm - vector;
			float num3 = Vector3.Dot(lhs3, lhs2) * num2;
			if (num3 < -1E-06f || num3 > 1.000001f)
			{
				continue;
			}
			Vector3 vector4 = Vector3.Cross(lhs3, lhs);
			float num4 = Vector3.Dot(directionMm, vector4) * num2;
			if (num4 < -1E-06f || num3 + num4 > 1.000001f)
			{
				continue;
			}
			float num5 = Vector3.Dot(rhs, vector4) * num2;
			Vector3 normalized = Vector3.Cross(lhs, rhs).normalized;
			crossings.Add(new Crossing
			{
				t = num5,
				normal = normalized
			});
		}
		crossings.Sort((Crossing a, Crossing b) => a.t.CompareTo(b.t));
		int num6 = 0;
		for (int i = 0; i < crossings.Count; i++)
		{
			if (num6 > 0 && Mathf.Abs(crossings[i].t - crossings[num6 - 1].t) <= 0.001f)
			{
				continue;
			}
			crossings[num6] = crossings[i];
			num6++;
		}
		for (int j = 0; j + 1 < num6; j += 2)
		{
			float num7 = Mathf.Max(0f, crossings[j].t);
			float num8 = crossings[j + 1].t;
			if (num7 + 1E-06f >= num8)
			{
				continue;
			}
			Vector3 pointMm = originMm + num7 * directionMm;
			output.Add(new BodyInterval
			{
				enterMm = num7,
				exitMm = num8,
				normal = crossings[j].normal,
				region = RegionAt(pointMm, dimensionsMm)
			});
		}
	}

	private static CrewRegion RegionAt(Vector3 pointMm, Vector3 dimensionsMm)
	{
		CrewRegion region = CrewRegion.Limb;
		float num = float.PositiveInfinity;
		for (int i = 0; i < CrewFigure.Parts.Length; i++)
		{
			CrewFigure.FigurePart figurePart = CrewFigure.NormalizedPart(i);
			float num2 = (pointMm.x - figurePart.pos.x * dimensionsMm.x) / (figurePart.scale.x * 0.5f * dimensionsMm.x);
			float num3 = (pointMm.y - figurePart.pos.y * dimensionsMm.y) / (figurePart.scale.y * 0.5f * dimensionsMm.y);
			float num4 = (pointMm.z - figurePart.pos.z * dimensionsMm.z) / (figurePart.scale.z * 0.5f * dimensionsMm.z);
			float num5 = num2 * num2 + num3 * num3 + num4 * num4;
			if (num > num5)
			{
				num = num5;
				region = figurePart.region;
			}
		}
		return region;
	}

	public void AddChannel(Vector3 entryLocal, Vector3 endLocal, float radiusM)
	{
		if (triangles.Count <= 0)
		{
			return;
		}
		if (radiusM <= 0f)
		{
			return;
		}
		Vector3 vector = endLocal - entryLocal;
		if (vector.sqrMagnitude < 1E-14f)
		{
			return;
		}
		if (count >= MaxHoles)
		{
			return;
		}
		ArmorPlate armorPlate = GetComponent<ArmorPlate>();
		Vector3 vector2 = new Vector3(entryLocal.x * armorPlate.widthM, entryLocal.y * armorPlate.heightM, entryLocal.z * (armorPlate.thicknessMM / 1000f));
		Vector3 vector3 = new Vector3(endLocal.x * armorPlate.widthM, endLocal.y * armorPlate.heightM, endLocal.z * (armorPlate.thicknessMM / 1000f));
		Vector3 vector4 = (vector3 - vector2).normalized;
		float num = 5E-05f;
		Vector4 vector5 = new Vector4(vector2.x - vector4.x * num, vector2.y - vector4.y * num, vector2.z - vector4.z * num, radiusM);
		Vector4 vector6 = new Vector4(vector3.x + vector4.x * num, vector3.y + vector4.y * num, vector3.z + vector4.z * num, 0f);
		starts[count] = vector5;
		ends[count] = vector6;
		count++;
		Push();
	}

	public void ResetHoles()
	{
		count = 0;
		Push();
	}

	private void LateUpdate()
	{
		if (count > 0 && transform.hasChanged)
		{
			Push();
		}
	}

	private void Push()
	{
		ArmorPlate armorPlate = GetComponent<ArmorPlate>();
		if (armorPlate == null)
		{
			return;
		}
		Matrix4x4 lhs = default(Matrix4x4);
		lhs.m00 = armorPlate.widthM;
		lhs.m11 = armorPlate.heightM;
		lhs.m22 = armorPlate.thicknessMM / 1000f;
		lhs.m33 = 1f;
		Matrix4x4 matrix = lhs * transform.worldToLocalMatrix;
		for (int i = 0; i < renderers.Length; i++)
		{
			Renderer renderer = renderers[i];
			if (renderer == null)
			{
				continue;
			}
			MaterialPropertyBlock materialPropertyBlock = Block;
			renderer.GetPropertyBlock(materialPropertyBlock);
			materialPropertyBlock.SetFloat("_CrewHoleCount", count);
			materialPropertyBlock.SetVectorArray("_CrewHoleStart", starts);
			materialPropertyBlock.SetVectorArray("_CrewHoleEnd", ends);
			materialPropertyBlock.SetMatrix("_CrewWorldToMeters", matrix);
			renderer.SetPropertyBlock(materialPropertyBlock);
		}
	}
}
