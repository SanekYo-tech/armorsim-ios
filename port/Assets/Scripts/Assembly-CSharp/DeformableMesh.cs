using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class DeformableMesh : MonoBehaviour
{
	private struct FaceBlock
	{
		public int start;

		public int nu;

		public int nv;

		public int axis;

		public float sign;

		public int uAxis;

		public int vAxis;

		public int Count => (nu + 1) * (nv + 1);

		public Vector3 NormalLocal
		{
			get
			{
				Vector3 result = default(Vector3);
				result[axis] = sign;
				return result;
			}
		}
	}

	public struct ChannelProfile
	{
		public float[] depth;

		public float[] radius;

		public bool IsValid => depth != null && radius != null && depth.Length >= 2 && radius.Length == depth.Length;

		public float RadiusAt(float z)
		{
			if (!IsValid)
			{
				return 0f;
			}
			int num = depth.Length;
			if (z >= depth[0])
			{
				return radius[0];
			}
			if (z >= depth[num - 1])
			{
				return radius[num - 1];
			}
			int num2 = 1;
			while (num2 < num && z > depth[num2])
			{
				num2++;
			}
			if (num2 >= num)
			{
				return radius[num - 1];
			}
			float num3 = depth[num2] - depth[num2 - 1];
			float num4 = 1f;
			if (num3 > 1E-09f)
			{
				num4 = (z - depth[num2 - 1]) / num3;
			}
			if (num4 < 0f)
			{
				num4 = 0f;
			}
			else if (num4 > 1f)
			{
				num4 = 1f;
			}
			return radius[num2 - 1] + (radius[num2] - radius[num2 - 1]) * num4;
		}
	}

	[Header("Плотность сетки (качество вмятины)")]
	[Range(4f, 160f)]
	public int resolution;

	private const int MinResolutionForChannel = 96;

	private Mesh mesh;

	private Vector3[] baseVertices;

	private int[] baseTriangles;

	private Vector3[] workVertices;

	private FaceBlock[] faceBlocks;

	private const int ThinFaceResolution = 12;

	private Vector3[] pristineVertices;

	private int[] pristineTriangles;

	private float lastCarveDepthWorld;

	private bool channelThrough;

	private bool channelBuilt;

	private bool colliderChannelThrough;

	private Vector3[] channelVertices;

	private Vector3[] workChannelVertices;

	private const int ChannelRings = 12;

	private const int ChannelDivisions = 24;

	[Header("Диагностика")]
	public bool logChannelBuild;

	private const int AnnulusRings = 3;

	private const float ObliqueFaceLimit = 0.15f;

	private const float PatchCells = 2f;

	private const float MinPatchCells = 2.5f;

	public const float RimJitterDefault = 0.15f;

	public float CarvedDepthWorld
	{
		get
		{
			if (channelBuilt)
			{
				return lastCarveDepthWorld;
			}
			return -1f;
		}
	}

	public bool ColliderChannelThrough => colliderChannelThrough;

	public DeformableMesh()
	{
		resolution = 96;
		lastCarveDepthWorld = -1f;
		logChannelBuild = true;
	}

	private void Awake()
	{
		GenerateMesh();
	}

	private void GenerateMesh()
	{
		mesh = new Mesh();
		mesh.name = "DeformablePlate";
		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();
		if (resolution < 96)
		{
			resolution = 96;
		}
		int num = resolution;
		faceBlocks = new FaceBlock[6];
		faceBlocks[0] = AddFace(verts, tris, num, num, new Vector3(0f, 0f, 0.5f), new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f));
		faceBlocks[1] = AddFace(verts, tris, num, num, new Vector3(0f, 0f, -0.5f), new Vector3(-1f, 0f, 0f), new Vector3(0f, 1f, 0f));
		faceBlocks[2] = AddFace(verts, tris, 12, num, new Vector3(0.5f, 0f, 0f), new Vector3(0f, 0f, -1f), new Vector3(0f, 1f, 0f));
		faceBlocks[3] = AddFace(verts, tris, 12, num, new Vector3(-0.5f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(0f, 1f, 0f));
		faceBlocks[4] = AddFace(verts, tris, num, 12, new Vector3(0f, 0.5f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, -1f));
		faceBlocks[5] = AddFace(verts, tris, num, 12, new Vector3(0f, -0.5f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f));
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.SetVertices(verts);
		mesh.subMeshCount = 2;
		mesh.SetTriangles(tris, 0);
		mesh.SetTriangles(Array.Empty<int>(), 1);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		GetComponent<MeshFilter>().mesh = mesh;
		baseVertices = mesh.vertices;
		baseTriangles = mesh.triangles;
		workVertices = new Vector3[baseVertices.Length];
		pristineVertices = (Vector3[])baseVertices.Clone();
		pristineTriangles = (int[])baseTriangles.Clone();
		BoxCollider boxCollider = GetComponent<BoxCollider>();
		if (boxCollider != null)
		{
			UnityEngine.Object.Destroy(boxCollider);
		}
		MeshCollider meshCollider = GetComponent<MeshCollider>();
		if (meshCollider == null)
		{
			meshCollider = base.gameObject.AddComponent<MeshCollider>();
		}
		meshCollider.sharedMesh = mesh;
	}

	private FaceBlock AddFace(List<Vector3> verts, List<int> tris, int nu, int nv, Vector3 center, Vector3 uDir, Vector3 vDir)
	{
		FaceBlock result = default(FaceBlock);
		int count = verts.Count;
		for (int i = 0; i <= nv; i++)
		{
			for (int j = 0; j <= nu; j++)
			{
				float num = (float)j / (float)nu - 0.5f;
				float num2 = (float)i / (float)nv - 0.5f;
				verts.Add(new Vector3(center.x + uDir.x * num + vDir.x * num2, center.y + uDir.y * num + vDir.y * num2, center.z + uDir.z * num + vDir.z * num2));
			}
		}
		for (int k = 0; k < nv; k++)
		{
			for (int l = 0; l < nu; l++)
			{
				tris.Add(count + k * (nu + 1) + l);
				tris.Add(count + k * (nu + 1) + l + 1);
				tris.Add(count + (k + 1) * (nu + 1) + l);
				tris.Add(count + k * (nu + 1) + l + 1);
				tris.Add(count + (k + 1) * (nu + 1) + l + 1);
				tris.Add(count + (k + 1) * (nu + 1) + l);
			}
		}
		result.start = count;
		result.nu = nu;
		result.nv = nv;
		result.axis = MajorAxis(center);
		result.sign = ((center[result.axis] < 0f) ? (-1f) : 1f);
		result.uAxis = MajorAxis(uDir);
		result.vAxis = MajorAxis(vDir);
		return result;
	}

	private static int MajorAxis(Vector3 v)
	{
		if (Mathf.Abs(v.x) >= Mathf.Abs(v.y) && Mathf.Abs(v.x) >= Mathf.Abs(v.z))
		{
			return 0;
		}
		return 1 + ((Mathf.Abs(v.y) < Mathf.Abs(v.z)) ? 1 : 0);
	}

	private bool PickChannelFaces(Vector3 axisO, Vector3 axisD, out int entryFace, out int exitFace)
	{
		Transform transform = base.transform;
		Vector3 rhs = transform.TransformDirection(Vector3.forward);
		float num = axisD.x * rhs.x + axisD.y * rhs.y + axisD.z * rhs.z;
		entryFace = ((num >= 0f) ? 1 : 0);
		exitFace = ((num >= 0f) ? 0 : 1);
		if (faceBlocks == null || faceBlocks.Length < 6)
		{
			return false;
		}
		Vector3 lossyScale = base.transform.lossyScale;
		float num2 = (Mathf.Abs(lossyScale.x) + Mathf.Abs(lossyScale.y) + Mathf.Abs(lossyScale.z)) * 2f + 1f;
		Vector3 localA = base.transform.InverseTransformPoint(axisO - axisD * num2);
		Vector3 localB = base.transform.InverseTransformPoint(axisO - axisD * num2 + axisD);
		Vector3 vector = localB - localA;
		float num3 = float.NegativeInfinity;
		float num4 = float.PositiveInfinity;
		int num5 = -1;
		int num6 = -1;
		float num7 = 0f;
		float num8 = 0f;
		for (int i = 0; i < 3; i++)
		{
			if (1E-09f > Mathf.Abs(vector[i]))
			{
				if (localA[i] < -0.5f || localA[i] > 0.5f)
				{
					return false;
				}
				continue;
			}
			float num9 = (-0.5f - localA[i]) / vector[i];
			float num10 = (0.5f - localA[i]) / vector[i];
			float num11 = -1f;
			if (num9 > num10)
			{
				float num12 = num9;
				num9 = num10;
				num10 = num12;
				num11 = 1f;
			}
			if (num9 > num3)
			{
				num3 = num9;
				num5 = i;
				num7 = num11;
			}
			if (num4 > num10)
			{
				num8 = 0f - num11;
				num4 = num10;
				num6 = i;
			}
			if (num3 > num4)
			{
				return false;
			}
		}
		if (num5 < 0 || num6 < 0)
		{
			return false;
		}
		int num13 = FaceIndexOf(num5, num7);
		int num14 = FaceIndexOf(num6, num8);
		if (num13 < 0 || num14 < 0)
		{
			return false;
		}
		entryFace = num13;
		exitFace = num14;
		return true;
	}

	private int FaceIndexOf(int axis, float sign)
	{
		for (int i = 0; i < faceBlocks.Length; i++)
		{
			if (faceBlocks[i].axis == axis && faceBlocks[i].sign * sign > 0f)
			{
				return i;
			}
		}
		return -1;
	}

	private void FaceCells(FaceBlock f, out float cellMin, out float cellMax)
	{
		Vector3 lossyScale = base.transform.lossyScale;
		float num = Mathf.Abs(lossyScale[f.uAxis]) / (float)Math.Max(1, f.nu);
		float num2 = Mathf.Abs(lossyScale[f.vAxis]) / (float)Math.Max(1, f.nv);
		cellMin = Mathf.Min(num, num2);
		cellMax = Mathf.Max(num, num2);
	}

	private bool BuffersMatchMesh()
	{
		if (mesh == null || baseVertices == null || workVertices == null)
		{
			return false;
		}
		if (baseVertices.Length == mesh.vertexCount && workVertices.Length == mesh.vertexCount)
		{
			return true;
		}
		MeshFilter meshFilter = GetComponent<MeshFilter>();
		if (meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh != mesh)
		{
			mesh = meshFilter.sharedMesh;
		}
		if (pristineVertices != null)
		{
			ResetDeformation();
		}
		return baseVertices.Length == mesh.vertexCount;
	}

	private Vector3[] DentSource(out Vector3[] scratch)
	{
		if (channelBuilt && channelVertices != null && mesh != null && channelVertices.Length == mesh.vertexCount)
		{
			if (workChannelVertices == null || workChannelVertices.Length != channelVertices.Length)
			{
				workChannelVertices = new Vector3[channelVertices.Length];
			}
			scratch = workChannelVertices;
			return channelVertices;
		}
		if (BuffersMatchMesh())
		{
			scratch = workVertices;
			return baseVertices;
		}
		scratch = null;
		return null;
	}

	public void Dent(Vector3 worldPoint, Vector3 worldDir, float radius, float depth, bool updateCollider = true)
	{
		Vector3[] array = DentSource(out var array2);
		if (array == null)
		{
			return;
		}
		Array.Copy(array, array2, array.Length);
		Vector3 localPoint = base.transform.InverseTransformPoint(worldPoint);
		Vector3 vector = base.transform.InverseTransformDirection(worldDir).normalized;
		Vector3 localScale = base.transform.localScale;
		float num = radius / ((localScale.x + localScale.y) * 0.5f);
		for (int i = 0; i < array2.Length; i++)
		{
			Vector3 vector2 = array2[i];
			float num2 = Mathf.Sqrt((vector2.x - localPoint.x) * (vector2.x - localPoint.x) + (vector2.y - localPoint.y) * (vector2.y - localPoint.y) + (vector2.z - localPoint.z) * (vector2.z - localPoint.z));
			if (num > num2)
			{
				float num3 = depth / localScale.z;
				float num4 = 1f - num2 / num;
				num4 *= num4;
				if (num3 > 0.4f)
				{
					num3 = 0.4f;
				}
				array2[i] = vector2 + vector * num3 * num4;
			}
		}
		mesh.vertices = array2;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		if (updateCollider)
		{
			SyncCollider();
		}
	}

	public void DentPath(Vector3 worldA, Vector3 worldB, Vector3 worldDir, float radius, float depth, bool updateCollider = true)
	{
		Vector3[] array = DentSource(out var array2);
		if (array == null)
		{
			return;
		}
		Array.Copy(array, array2, array.Length);
		Vector3 localA = base.transform.InverseTransformPoint(worldA);
		Vector3 localB = base.transform.InverseTransformPoint(worldB);
		Vector3 vector = base.transform.InverseTransformDirection(worldDir).normalized;
		Vector3 localScale = base.transform.localScale;
		Vector3 vector2 = localB - localA;
		float num = vector2.x * vector2.x + vector2.y * vector2.y + vector2.z * vector2.z;
		float num2 = radius / ((localScale.x + localScale.y) * 0.5f);
		for (int i = 0; i < array2.Length; i++)
		{
			Vector3 vector3 = array2[i];
			float num3 = 0f;
			if (num > 1E-12f)
			{
				num3 = Mathf.Clamp01(((vector3.x - localA.x) * vector2.x + (vector3.y - localA.y) * vector2.y + (vector3.z - localA.z) * vector2.z) / num);
			}
			Vector3 vector4 = localA + vector2 * num3;
			float num4 = Mathf.Sqrt((vector3.x - vector4.x) * (vector3.x - vector4.x) + (vector3.y - vector4.y) * (vector3.y - vector4.y) + (vector3.z - vector4.z) * (vector3.z - vector4.z));
			if (num2 > num4)
			{
				float num5 = depth / localScale.z;
				float num6 = 1f - num4 / num2;
				num6 *= num6;
				if (num5 > 0.4f)
				{
					num5 = 0.4f;
				}
				array2[i] = vector3 + vector * num5 * num6;
			}
		}
		mesh.vertices = array2;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		if (updateCollider)
		{
			SyncCollider();
		}
	}

	public void SyncCollider()
	{
		MeshCollider meshCollider = GetComponent<MeshCollider>();
		if (meshCollider == null)
		{
			return;
		}
		meshCollider.sharedMesh = null;
		meshCollider.sharedMesh = mesh;
		colliderChannelThrough = channelThrough;
	}

	public void ResetDeformation()
	{
		if (mesh == null || pristineVertices == null)
		{
			return;
		}
		lastCarveDepthWorld = -1f;
		channelVertices = null;
		channelThrough = false;
		channelBuilt = false;
		baseVertices = (Vector3[])pristineVertices.Clone();
		baseTriangles = (int[])pristineTriangles.Clone();
		if (workVertices == null || workVertices.Length != baseVertices.Length)
		{
			workVertices = new Vector3[baseVertices.Length];
		}
		mesh.triangles = Array.Empty<int>();
		mesh.subMeshCount = 1;
		mesh.vertices = (Vector3[])pristineVertices.Clone();
		mesh.subMeshCount = 2;
		mesh.SetTriangles((int[])pristineTriangles.Clone(), 0);
		mesh.SetTriangles(Array.Empty<int>(), 1);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		SyncCollider();
	}

	public void PunchHole(Vector3 impactPoint, Vector3 direction, float holeRadius, bool isCeramic, bool allowPetaling, bool extraThinBonus, bool updateCollider = true)
	{
		if (mesh == null || baseTriangles == null)
		{
			return;
		}
		BuildChannel(impactPoint, direction, holeRadius, 0f, through: true, isCeramic, allowPetaling, extraThinBonus, default(ChannelProfile), 0f, 0f, 0f);
		channelThrough = true;
		channelBuilt = true;
		if (updateCollider)
		{
			SyncCollider();
		}
	}

	public void UpdateJetChannel(Vector3 entry, Vector3 dir, ChannelProfile prof, float depthWorld, float fullThicknessWorld, bool through, bool updateCollider = false, float exitBulge = 0f)
	{
		if (mesh == null || baseTriangles == null || !prof.IsValid)
		{
			return;
		}
		if (channelBuilt && through == channelThrough && (base.transform.lossyScale.x + base.transform.lossyScale.y) * 0.5f / (float)Math.Max(1, resolution) * 0.5f > Mathf.Abs(depthWorld - lastCarveDepthWorld))
		{
			return;
		}
		float num = (through ? fullThicknessWorld : Mathf.Min(depthWorld, fullThicknessWorld * 0.95f));
		BuildChannel(entry, dir, prof.RadiusAt(0f), num, through, isCeramic: false, allowPetaling: false, extraThinBonus: false, prof, fullThicknessWorld, exitBulge, 0f);
		lastCarveDepthWorld = depthWorld;
		channelThrough = through;
		channelBuilt = true;
		SyncCollider();
	}

	public void UpdateChannel(Vector3 entry, Vector3 dir, float holeRadius, float depthWorld, float fullThicknessWorld, bool through, bool isCeramic, bool allowPetaling, bool extraThinBonus, bool updateCollider = false, float raggedRimAmplitude = 0f)
	{
		if (mesh == null || baseTriangles == null)
		{
			return;
		}
		if (channelBuilt && through == channelThrough && (base.transform.lossyScale.x + base.transform.lossyScale.y) * 0.5f / (float)Math.Max(1, resolution) * 0.5f > Mathf.Abs(depthWorld - lastCarveDepthWorld))
		{
			return;
		}
		float num = (through ? fullThicknessWorld : Mathf.Min(depthWorld, fullThicknessWorld * 0.95f));
		BuildChannel(entry, dir, holeRadius, num, through, isCeramic, allowPetaling, extraThinBonus, default(ChannelProfile), 0f, 0f, raggedRimAmplitude);
		lastCarveDepthWorld = depthWorld;
		channelThrough = through;
		channelBuilt = true;
		SyncCollider();
	}

	private void BuildChannel(Vector3 axisO, Vector3 axisD, float holeRadius, float depthWorld, bool through, bool isCeramic, bool allowPetaling, bool extraThinBonus, ChannelProfile prof = default(ChannelProfile), float fullThicknessWorld = 0f, float exitBulge = 0f, float raggedRimAmplitude = 0f)
	{
		if (faceBlocks == null || faceBlocks.Length < 6)
		{
			return;
		}
		Vector3[] world = new Vector3[baseVertices.Length];
		for (int i = 0; i < baseVertices.Length; i++)
		{
			world[i] = base.transform.TransformPoint(baseVertices[i]);
		}
		Vector3 vector = ((axisD.sqrMagnitude <= 1E-12f) ? base.transform.forward : axisD.normalized);
		PickChannelFaces(axisO, vector, out var entryFace, out var exitFace);
		FaceBlock faceBlock = faceBlocks[entryFace];
		FaceBlock faceBlock2 = faceBlocks[exitFace];
		FaceCells(faceBlock, out var cellMin, out var cellMax);
		FaceCells(faceBlock2, out var cellMin2, out var cellMax2);
		Vector3 vector2 = Vector3.zero;
		vector2[faceBlock.axis] = faceBlock.sign;
		Vector3 vector3 = (base.transform.rotation * vector2).normalized;
		Vector3 vector4 = Vector3.zero;
		vector4[faceBlock2.axis] = faceBlock2.sign;
		Vector3 vector5 = (base.transform.rotation * vector4).normalized;
		bool flag = false;
		if (through)
		{
			flag = Mathf.Abs(Vector3.Dot(vector5, vector)) >= 0.15f;
			if (!flag)
			{
				Debug.Log(string.Concat("  ▣ Косой прорыв кромки у ", base.gameObject.name, ": выход под ", string.Format("{0:F0}° к нормали грани — отверстие выхода ", Vector3.Dot(vector, -vector5)), "не строится, канал показан трубой (см. разбор в BuildChannel)."));
			}
		}
		if (holeRadius <= 1E-05f)
		{
			holeRadius = 1E-05f;
		}
		bool flag2 = false;
		float num = holeRadius;
		if (prof.IsValid)
		{
			flag2 = prof.radius.Length == prof.depth.Length;
			num = holeRadius;
			if (flag2)
			{
				num = prof.RadiusAt(prof.radius.Length);
				if (num <= 1E-05f)
				{
					num = 1E-05f;
				}
				if (num > holeRadius)
				{
					num = holeRadius;
				}
			}
		}
		float num2 = cellMin + cellMin + holeRadius;
		if (num2 <= cellMax * 2.5f)
		{
			num2 = cellMax * 2.5f;
		}
		float num3 = cellMin2 + cellMin2 + num;
		if (num3 <= cellMax2 * 2.5f)
		{
			num3 = cellMax2 * 2.5f;
		}
		int num4 = faceBlock.start;
		int num5 = (faceBlock.nv + 1) * (faceBlock.nu + 1) + faceBlock.start;
		int num6 = faceBlock2.start;
		int num7 = (faceBlock2.nv + 1) * (faceBlock2.nu + 1) + faceBlock2.start;
		bool flag3 = through || depthWorld > 1E-04f;
		List<int> newTris = new List<int>(baseTriangles.Length);
		HashSet<int> hashSet = new HashSet<int>();
		HashSet<int> hashSet2 = new HashSet<int>();
		HashSet<int> hashSet3 = new HashSet<int>();
		HashSet<int> hashSet4 = new HashSet<int>();
		for (int j = 0; j < baseTriangles.Length; j += 3)
		{
			int num8 = baseTriangles[j];
			int num9 = baseTriangles[j + 1];
			int num10 = baseTriangles[j + 2];
			bool flag4 = num8 >= num4 && num8 < num5 && num9 >= num4 && num9 < num5 && num10 >= num4 && num10 < num5;
			bool flag5 = num8 >= num6 && num8 < num7 && num9 >= num6 && num9 < num7 && num10 >= num6 && num10 < num7;
			if (!flag3 || !flag4)
			{
				if (!flag || !flag5)
				{
					newTris.Add(num8);
					newTris.Add(num9);
					newTris.Add(num10);
					if (flag4)
					{
						hashSet2.Add(num8);
						hashSet2.Add(num9);
						hashSet2.Add(num10);
					}
					else if (flag5)
					{
						hashSet4.Add(num8);
						hashSet4.Add(num9);
						hashSet4.Add(num10);
					}
					continue;
				}
			}
			Vector3 vector6 = (world[num8] + world[num9] + world[num10]) / 3f;
			float num11 = DistToAxis(vector6, axisO, vector);
			float num12 = (flag4 ? num2 : num3);
			if (num12 > num11)
			{
				HashSet<int> hashSet5 = (flag4 ? hashSet : hashSet3);
				hashSet5.Add(num8);
				hashSet5.Add(num9);
				hashSet5.Add(num10);
			}
			else
			{
				newTris.Add(num8);
				newTris.Add(num9);
				newTris.Add(num10);
				if (flag4)
				{
					hashSet2.Add(num8);
					hashSet2.Add(num9);
					hashSet2.Add(num10);
				}
				else if (flag5)
				{
					hashSet4.Add(num8);
					hashSet4.Add(num9);
					hashSet4.Add(num10);
				}
			}
		}
		List<Vector3> vertsList = new List<Vector3>(baseVertices);
		List<int> list = RimVerts(hashSet, hashSet2);
		List<int> list2 = RimVerts(hashSet3, hashSet4);
		Func<int, Vector3> func = (int i) => ((i < world.Length) ? world[i] : base.transform.TransformPoint(vertsList[i]));
		List<int> list3 = new List<int>();
		List<int> list4 = new List<int>();
		int num13 = newTris.Count;
		int num14 = num13;
		List<int> list5 = null;
		if (flag3 && list.Count > 2)
		{
			list5 = BuildFaceAperture(vertsList, newTris, func, axisO, vector, holeRadius, list, world[num4], vector3, list3);
			num14 = newTris.Count;
		}
		List<int> list6 = null;
		int num15 = num14;
		if (flag && list2.Count > 2)
		{
			list6 = BuildFaceAperture(vertsList, newTris, func, axisO, vector, num, list2, world[num6], vector5, list4);
			num15 = newTris.Count;
		}
		if (!flag2)
		{
			float num16 = ((isCeramic || allowPetaling) ? RimJitterDefault : 0f);
			if (num16 < raggedRimAmplitude)
			{
				num16 = raggedRimAmplitude;
			}
			if (0f < num16)
			{
				if (list5 != null)
				{
					JitterRim(vertsList, list5, axisO, vector, holeRadius, num16);
				}
				if (list6 != null)
				{
					JitterRim(vertsList, list6, axisO, vector, num, num16);
				}
			}
			if (through && !isCeramic)
			{
				ApplyLipRing(vertsList, num4, num5, list3, axisO, vector, holeRadius, holeRadius * 0.1f, -1f);
				if (list6 != null)
				{
					ApplyLipRing(vertsList, num6, num7, list4, axisO, vector, num, num * 0.25f, 1f);
				}
			}
		}
		else if (flag && list6 != null && 0f < exitBulge)
		{
			exitBulge *= 0.6f;
			ApplyLipRing(vertsList, num6, num7, list4, axisO, vector, num, num * exitBulge, 1f);
		}
		Func<int, Vector3> func2 = (int i) => base.transform.TransformPoint(vertsList[i]);
		AlignFaceWinding(newTris, func2, num13, num14, vector3);
		AlignFaceWinding(newTris, func2, num14, num15, vector5);
		int num17 = newTris.Count;
		if (!flag2)
		{
			if (!through || list5 == null || list6 == null)
			{
				if (flag3 && list5 != null)
				{
					if (through && !flag)
					{
						depthWorld *= 0.98f;
					}
					BuildBlindTube(vertsList, newTris, axisO, vector, depthWorld, list5);
				}
			}
			else
			{
				StitchRingsBothSides(vertsList, newTris, list5, list6);
			}
		}
		else if (list5 != null)
		{
			List<int> list7 = null;
			if (!through)
			{
				fullThicknessWorld = depthWorld;
			}
			else
			{
				list7 = list6;
			}
			BuildTaperedWall(vertsList, newTris, axisO, vector, prof, fullThicknessWorld, list5, list7);
		}
		int num18 = newTris.Count;
		bool flag6 = false;
		if (through && !flag2)
		{
			flag6 = !isCeramic;
		}
		if (allowPetaling && flag6 && list6 != null)
		{
			AddPetals(vertsList, newTris, list6, axisO, vector, num, extraThinBonus);
		}
		if (logChannelBuild && (!channelBuilt || through != channelThrough))
		{
			Debug.Log(string.Concat("◆ МЕШ [", base.gameObject.name, "]: канал ", through ? "СКВОЗНОЙ" : "слепой", " | ", string.Format("радиус вход {0:F1} мм → выход {1:F1} мм ", holeRadius * 1000f, num * 1000f), string.Format("(клетка {0:F1} мм, патч {1:F1} мм) | ", cellMax * 1000f, num2 * 1000f), string.Format("снято треуг.: вход {0} узлов, выход {1} | ", hashSet.Count, hashSet3.Count), string.Format("кромки: вход {0}, выход {1} | ", list5?.Count ?? 0, list6?.Count ?? 0), string.Format("вершин {0} → {1}, треуг. {2} → {3}", baseVertices.Length, vertsList.Count, baseTriangles.Length / 3, newTris.Count / 3)));
		}
		List<int> list8 = new List<int>(newTris.Count);
		int num19 = num18 - num17;
		if (num19 < 1)
		{
			num19 = 0;
		}
		List<int> list9 = new List<int>(num19);
		for (int k = 0; k < newTris.Count; k++)
		{
			if (k < num17 || num18 <= k)
			{
				list8.Add(newTris[k]);
			}
			else
			{
				list9.Add(newTris[k]);
			}
		}
		mesh.triangles = Array.Empty<int>();
		mesh.subMeshCount = 1;
		mesh.vertices = vertsList.ToArray();
		mesh.subMeshCount = 2;
		mesh.SetTriangles(list8, 0);
		mesh.SetTriangles(list9, 1);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		channelVertices = vertsList.ToArray();
	}

	private List<int> BuildFaceAperture(List<Vector3> vertsList, List<int> newTris, Func<int, Vector3> pos, Vector3 axisO, Vector3 axisD, float R, List<int> patchRim, Vector3 facePoint, Vector3 faceOutNormal, List<int> annulusOut)
	{
		Vector3 u = default(Vector3);
		Vector3 vAx = default(Vector3);
		BuildPerpBasis(axisD, out u, out vAx);
		float num = Vector3.Dot(faceOutNormal, axisD);
		if (Mathf.Abs(num) < 1E-04f)
		{
			return null;
		}
		float num2 = Vector3.Dot(facePoint - axisO, faceOutNormal);
		float num3 = float.MaxValue;
		foreach (int item in patchRim)
		{
			float num4 = DistToAxis(pos(item), axisO, axisD);
			if (num4 < num3)
			{
				num3 = num4;
			}
		}
		float num5 = num3 * 0.92f;
		if (num5 <= R * 1.05f)
		{
			num5 = R * 1.05f;
		}
		if (num5 <= R)
		{
			num5 = R * 1.05f;
		}
		List<int[]> list = new List<int[]>(3);
		for (int i = 0; i < 3; i++)
		{
			float num6 = Mathf.Clamp01((float)i * 0.5f) * (num5 - R) + R;
			int[] array = new int[24];
			for (int j = 0; j < 24; j++)
			{
				float num7 = ((float)j / 24f + (float)j / 24f) * Mathf.PI;
				Vector3 vector = Mathf.Cos(num7) * u + Mathf.Sin(num7) * vAx;
				float num8 = (num2 - Vector3.Dot(faceOutNormal, vector) * num6) / num;
				Vector3 vector2 = axisD * num8 + axisO + vector * num6;
				vertsList.Add(base.transform.InverseTransformPoint(vector2));
				array[j] = vertsList.Count - 1;
				if (i > 0)
				{
					annulusOut.Add(array[j]);
				}
			}
			list.Add(array);
		}
		if (list.Count != 0)
		{
			foreach (int item2 in list[0])
			{
				annulusOut.Add(item2);
			}
			bool flag = Vector3.Dot(axisD, faceOutNormal) > 0f;
			for (int k = 0; k + 1 < 3; k++)
			{
				int[] array2 = list[k];
				int[] array3 = list[k + 1];
				for (int l = 0; l < array2.Length; l++)
				{
					int num9 = (l + 1) % array2.Length;
					if (flag)
					{
						newTris.Add(array2[num9]);
						newTris.Add(array2[l]);
						newTris.Add(array3[l]);
						newTris.Add(array3[num9]);
						newTris.Add(array2[num9]);
						newTris.Add(array3[l]);
					}
					else
					{
						newTris.Add(array2[l]);
						newTris.Add(array2[num9]);
						newTris.Add(array3[l]);
						newTris.Add(array2[num9]);
						newTris.Add(array3[num9]);
						newTris.Add(array3[l]);
					}
				}
			}
			if (list.Count > 2)
			{
				List<int> list2 = new List<int>(list[2]);
				List<int> list3 = new List<int>(patchRim);
				if (flag)
				{
					StitchWalls(newTris, pos, axisO, axisD, list3, list2);
				}
				else
				{
					StitchWalls(newTris, pos, axisO, axisD, list2, list3);
				}
				return new List<int>(list[0]);
			}
		}
		return null;
	}

	private static void AlignFaceWinding(List<int> tris, Func<int, Vector3> pos, int from, int to, Vector3 faceOutNormal)
	{
		for (int i = from; i + 2 < to; i += 3)
		{
			Vector3 a = pos(tris[i]);
			Vector3 b = pos(tris[i + 1]);
			Vector3 c = pos(tris[i + 2]);
			Vector3 vector = Vector3.Cross(b - a, c - a);
			if (vector.x * vector.x + vector.y * vector.y + vector.z * vector.z < 1E-16f)
			{
				continue;
			}
			if (faceOutNormal.x * vector.x + faceOutNormal.y * vector.y + faceOutNormal.z * vector.z >= 0f)
			{
				continue;
			}
			int num = tris[i + 1];
			tris[i + 1] = tris[i + 2];
			tris[i + 2] = num;
		}
	}

	private static void StitchRingsParallel(List<int> tris, int[] A, int[] B, bool reverse)
	{
		int num = A.Length;
		for (int i = 0; i < num; i++)
		{
			int num2 = (i + 1) % num;
			if (!reverse)
			{
				tris.Add(A[i]);
				tris.Add(A[num2]);
				tris.Add(B[i]);
				tris.Add(A[num2]);
				tris.Add(B[num2]);
				tris.Add(B[i]);
			}
			else
			{
				tris.Add(A[num2]);
				tris.Add(A[i]);
				tris.Add(B[i]);
				tris.Add(B[num2]);
				tris.Add(A[num2]);
				tris.Add(B[i]);
			}
		}
	}

	private void StitchRingsBothSides(List<Vector3> vertsList, List<int> newTris, List<int> rimFront, List<int> rimBack)
	{
		int[] array = rimFront.ToArray();
		int[] array2 = rimBack.ToArray();
		int[] array3 = DuplicateRing(vertsList, array);
		int[] array4 = DuplicateRing(vertsList, array2);
		StitchRingsParallel(newTris, array, array2, reverse: false);
		StitchRingsParallel(newTris, array4, array3, reverse: false);
	}

	private static int[] DuplicateRing(List<Vector3> vertsList, int[] ring)
	{
		int[] array = new int[ring.Length];
		for (int i = 0; i < ring.Length; i++)
		{
			vertsList.Add(vertsList[ring[i]]);
			array[i] = vertsList.Count - 1;
		}
		return array;
	}

	private void BuildTaperedWall(List<Vector3> vertsList, List<int> newTris, Vector3 axisO, Vector3 axisD, ChannelProfile prof, float depthWorld, List<int> rimFront, List<int> rimBack)
	{
		if (depthWorld <= 1E-05f)
		{
			return;
		}
		Vector3 u = default(Vector3);
		Vector3 vAx = default(Vector3);
		BuildPerpBasis(axisD, out u, out vAx);
		bool flag = rimBack == null || rimBack.Count != 24;
		List<int[]> list = new List<int[]>();
		for (int i = 1; i < 12 + (flag ? 1 : 0); i++)
		{
			float num = (float)i * depthWorld / 12f;
			float num2 = Mathf.Max(prof.RadiusAt(num), 1E-05f);
			int[] array = new int[24];
			for (int j = 0; j < 24; j++)
			{
				float num3 = ((float)j / 24f + (float)j / 24f) * Mathf.PI;
				Vector3 vector = (Mathf.Cos(num3) * u + Mathf.Sin(num3) * vAx) * num2 + axisD * num + axisO;
				vertsList.Add(base.transform.InverseTransformPoint(vector));
				array[j] = vertsList.Count - 1;
			}
			list.Add(array);
		}
		List<int[]> list2 = new List<int[]>();
		list2.Add(rimFront.ToArray());
		list2.AddRange(list);
		int num4 = -1;
		int num5 = -1;
		if (flag)
		{
			Vector3 vector2 = base.transform.InverseTransformPoint(axisD * depthWorld + axisO);
			vertsList.Add(vector2);
			num4 = vertsList.Count - 1;
			vertsList.Add(vector2);
			num5 = vertsList.Count - 1;
		}
		else
		{
			list2.Add(rimBack.ToArray());
		}
		List<int[]> list3 = new List<int[]>();
		foreach (int[] item in list2)
		{
			list3.Add(DuplicateRing(vertsList, item));
		}
		for (int k = 0; k + 1 < list2.Count; k++)
		{
			int[] array2 = list2[k];
			int[] array3 = list2[k + 1];
			for (int l = 0; l < array2.Length; l++)
			{
				int num6 = (l + 1) % array2.Length;
				newTris.Add(array2[num6]);
				newTris.Add(array2[l]);
				newTris.Add(array3[l]);
				newTris.Add(array3[num6]);
				newTris.Add(array2[num6]);
				newTris.Add(array3[l]);
			}
			int[] array4 = list3[k + 1];
			int[] array5 = list3[k];
			for (int m = 0; m < array4.Length; m++)
			{
				int num7 = (m + 1) % array4.Length;
				newTris.Add(array4[num7]);
				newTris.Add(array4[m]);
				newTris.Add(array5[m]);
				newTris.Add(array5[num7]);
				newTris.Add(array4[num7]);
				newTris.Add(array5[m]);
			}
		}
		if (num4 > -1)
		{
			int[] array6 = list2[list2.Count - 1];
			int[] array7 = list3[list3.Count - 1];
			for (int n = 0; n < 24; n++)
			{
				int num8 = (n + 1) % 24;
				newTris.Add(array6[n]);
				newTris.Add(array6[num8]);
				newTris.Add(num4);
				newTris.Add(num5);
				newTris.Add(array7[num8]);
				newTris.Add(array7[n]);
			}
		}
	}

	private void BuildBlindTube(List<Vector3> vertsList, List<int> newTris, Vector3 axisO, Vector3 axisD, float depthWorld, List<int> rimFront)
	{
		int[] array = rimFront.ToArray();
		int[] array2 = new int[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			Vector3 vector = base.transform.TransformPoint(vertsList[array[i]]);
			vertsList.Add(base.transform.InverseTransformPoint(axisD * depthWorld + vector));
			array2[i] = vertsList.Count - 1;
		}
		int[] array3 = DuplicateRing(vertsList, array);
		int[] array4 = DuplicateRing(vertsList, array2);
		for (int j = 0; j < array.Length; j++)
		{
			int num = (j + 1) % array.Length;
			newTris.Add(array[num]);
			newTris.Add(array[j]);
			newTris.Add(array2[j]);
			newTris.Add(array2[num]);
			newTris.Add(array[num]);
			newTris.Add(array2[j]);
		}
		for (int k = 0; k < array4.Length; k++)
		{
			int num2 = (k + 1) % array4.Length;
			newTris.Add(array4[num2]);
			newTris.Add(array4[k]);
			newTris.Add(array3[k]);
			newTris.Add(array3[num2]);
			newTris.Add(array4[num2]);
			newTris.Add(array3[k]);
		}
		vertsList.Add(base.transform.InverseTransformPoint(axisD * depthWorld + axisO));
		int num3 = vertsList.Count - 1;
		vertsList.Add(base.transform.InverseTransformPoint(axisD * depthWorld + axisO));
		int num4 = vertsList.Count - 1;
		for (int l = 0; l < array.Length; l++)
		{
			int num5 = (l + 1) % array.Length;
			newTris.Add(array2[l]);
			newTris.Add(array2[num5]);
			newTris.Add(num3);
			newTris.Add(num4);
			newTris.Add(array4[num5]);
			newTris.Add(array4[l]);
		}
	}

	private void JitterRim(List<Vector3> vertsList, List<int> rimIdx, Vector3 axisO, Vector3 axisD, float R, float amp)
	{
		if (amp <= 0f)
		{
			return;
		}
		float num = amp * 0.6666667f;
		foreach (int item in rimIdx)
		{
			Vector3 vector = base.transform.TransformPoint(vertsList[item]);
			Vector3 vector2 = RadialDir(vector, axisO, axisD);
			float num2 = Random.Range(0f - amp, amp) * R;
			float num3 = Random.Range(0f - num, num) * R;
			Vector3 vector3 = new Vector3(vector.x + vector2.x * num2 + axisD.x * num3, vector.y + vector2.y * num2 + axisD.y * num3, vector.z + vector2.z * num2 + axisD.z * num3);
			vertsList[item] = base.transform.InverseTransformPoint(vector3);
		}
	}

	private void ApplyLipRing(List<Vector3> vertsList, int faceStart, int faceEnd, List<int> extra, Vector3 axisO, Vector3 axisD, float R, float h, float sign)
	{
		float num = R * 0.5f;
		float num2 = num * 3f + R;
		for (int i = faceStart; i < faceEnd; i++)
		{
			LipVertex(vertsList, i, axisO, axisD, R, num2, num, h, sign);
		}
		if (extra == null)
		{
			return;
		}
		foreach (int item in extra)
		{
			LipVertex(vertsList, item, axisO, axisD, R, num2, num, h, sign);
		}
	}

	private void LipVertex(List<Vector3> vertsList, int idx, Vector3 axisO, Vector3 axisD, float R, float outer, float w, float h, float sign)
	{
		Vector3 vector = base.transform.TransformPoint(vertsList[idx]);
		float num = DistToAxis(vector, axisO, axisD);
		if (num < R || num > outer)
		{
			return;
		}
		float num2 = Mathf.Exp(0f - (num - R) * (num - R) / (2f * w * w)) * h * sign;
		vertsList[idx] = base.transform.InverseTransformPoint(vector + axisD * num2);
	}

	private void AddPetals(List<Vector3> vertsList, List<int> newTris, List<int> rimBack, Vector3 axisO, Vector3 axisD, float R, bool extraThinBonus)
	{
		int num = PickPetalCount(extraThinBonus);
		if (num * 2 > rimBack.Count)
		{
			return;
		}
		Vector3 u = default(Vector3);
		Vector3 vAx = default(Vector3);
		BuildPerpBasis(axisD, out u, out vAx);
		List<int> list = new List<int>(rimBack);
		list.Sort((p, q) => AngleAround(vertsList[p], axisO, u, vAx).CompareTo(AngleAround(vertsList[q], axisO, u, vAx)));
		float num2 = 6.28318548f / num;
		float num3 = Random.Range(0f, 6.28318548f);
		float[] array = new float[num + 1];
		for (int i = 0; i <= num; i++)
		{
			array[i] = (float)i * num2 + num3 + Random.Range(0f - 10f, 10f) * 0.0174532924f;
		}
		for (int j = 0; j < num; j++)
		{
			float num4 = (array[j] + array[j + 1]) * 0.5f;
			List<int> list2 = new List<int>();
			foreach (int item in list)
			{
				if (InSector(AngleAround(vertsList[item], axisO, u, vAx), array[j], array[j + 1]))
				{
					list2.Add(item);
				}
			}
			if (list2.Count > 1)
			{
				Vector3 vector = (Mathf.Cos(num4) * u + Mathf.Sin(num4) * vAx).normalized;
				float num5 = Random.Range(40f, 70f) * 0.0174532924f;
				Vector3 vector2 = (Mathf.Cos(num5) * vector + Mathf.Sin(num5) * axisD).normalized;
				float num6 = Random.Range(1f, 1.4f) * R;
				int[] array2 = new int[list2.Count];
				for (int k = 0; k < list2.Count; k++)
				{
					Vector3 vector3 = base.transform.TransformPoint(vertsList[list2[k]]);
					vertsList.Add(base.transform.InverseTransformPoint(vector2 * num6 + vector3));
					array2[k] = vertsList.Count - 1;
				}
				int[] array3 = new int[list2.Count];
				int[] array4 = new int[list2.Count];
				for (int l = 0; l < list2.Count; l++)
				{
					vertsList.Add(vertsList[list2[l]]);
					array3[l] = vertsList.Count - 1;
					vertsList.Add(vertsList[array2[l]]);
					array4[l] = vertsList.Count - 1;
				}
				for (int m = 0; m < list2.Count - 1; m++)
				{
					newTris.Add(list2[m]);
					newTris.Add(list2[m + 1]);
					newTris.Add(array2[m]);
					newTris.Add(list2[m + 1]);
					newTris.Add(array2[m + 1]);
					newTris.Add(array2[m]);
					newTris.Add(array3[m + 1]);
					newTris.Add(array3[m]);
					newTris.Add(array4[m]);
					newTris.Add(array4[m + 1]);
					newTris.Add(array3[m + 1]);
					newTris.Add(array4[m]);
				}
			}
		}
	}

	private static int PickPetalCount(bool extraThinBonus)
	{
		float value = Random.value;
		int num = 3;
		if (value >= 0.2f)
		{
			if (value >= 0.6f)
			{
				num = 5 + ((value >= 0.85f) ? 1 : 0);
			}
			else
			{
				num = 4;
			}
		}
		if (extraThinBonus)
		{
			num++;
		}
		if (num < 3)
		{
			num = 3;
		}
		if (num > 6)
		{
			num = 6;
		}
		return num;
	}

	private static Vector3 RadialDir(Vector3 p, Vector3 o, Vector3 d)
	{
		Vector3 vector = p - o;
		float num = d.x * d.x + d.y * d.y + d.z * d.z;
		if (num >= Mathf.Epsilon)
		{
			float num2 = (vector.x * d.x + vector.y * d.y + vector.z * d.z) / num;
			vector.x -= d.x * num2;
			vector.y -= d.y * num2;
			vector.z -= d.z * num2;
		}
		if (vector.x * vector.x + vector.y * vector.y + vector.z * vector.z <= 1E-12f)
		{
			return Vector3.zero;
		}
		float magnitude = vector.magnitude;
		if (magnitude <= 1E-05f)
		{
			return Vector3.zero;
		}
		return vector / magnitude;
	}

	private static void BuildPerpBasis(Vector3 axisD, out Vector3 u, out Vector3 vAx)
	{
		u = Vector3.Cross(axisD, Vector3.up);
		if (u.sqrMagnitude < 1E-06f)
		{
			u = Vector3.Cross(axisD, Vector3.right);
		}
		u = u.normalized;
		vAx = Vector3.Cross(axisD, u).normalized;
	}

	private float AngleAround(Vector3 localVert, Vector3 axisO, Vector3 u, Vector3 vAx)
	{
		Vector3 vector = base.transform.TransformPoint(localVert) - axisO;
		return Mathf.Atan2(vector.x * vAx.x + vector.y * vAx.y + vector.z * vAx.z, vector.x * u.x + vector.y * u.y + vector.z * u.z);
	}

	private static bool InSector(float a, float lo, float hi)
	{
		if (a >= lo && a < hi)
		{
			return true;
		}
		if (a + 6.28318548f >= lo && a + 6.28318548f < hi)
		{
			return true;
		}
		return a - 6.28318548f >= lo && a - 6.28318548f < hi;
	}

	private static float DistToAxis(Vector3 p, Vector3 o, Vector3 d)
	{
		Vector3 vector = p - o;
		float num = d.x * d.x + d.y * d.y + d.z * d.z;
		if (num >= Mathf.Epsilon)
		{
			float num2 = (vector.x * d.x + vector.y * d.y + vector.z * d.z) / num;
			vector.x -= d.x * num2;
			vector.y -= d.y * num2;
			vector.z -= d.z * num2;
		}
		return vector.magnitude;
	}

	private static List<int> RimVerts(HashSet<int> removed, HashSet<int> kept)
	{
		List<int> list = new List<int>();
		foreach (int item in removed)
		{
			if (!kept.Contains(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	private void StitchWalls(List<int> tris, Vector3[] world, Vector3 axisO, Vector3 axisD, List<int> rimA, List<int> rimB)
	{
		StitchWalls(tris, (Func<int, Vector3>)((int i) => world[i]), axisO, axisD, rimA, rimB);
	}

	private void StitchWalls(List<int> tris, Func<int, Vector3> world, Vector3 axisO, Vector3 axisD, List<int> rimA, List<int> rimB)
	{
		Vector3 u = default(Vector3);
		Vector3 vAx = default(Vector3);
		BuildPerpBasis(axisD, out u, out vAx);
		rimA.Sort((p, q) => Angle(world(p), axisO, u, vAx).CompareTo(Angle(world(q), axisO, u, vAx)));
		rimB.Sort((p, q) => Angle(world(p), axisO, u, vAx).CompareTo(Angle(world(q), axisO, u, vAx)));
		int nA = rimA.Count;
		int nB = rimB.Count;
		float a0 = Angle(world(rimA[0]), axisO, u, vAx);
		int bOff = 0;
		float num = float.MaxValue;
		for (int i = 0; i < nB; i++)
		{
			float num2 = Wrap(Angle(world(rimB[i]), axisO, u, vAx) - a0);
			if (num > num2)
			{
				num = num2;
				bOff = i;
			}
		}
		int j = 0;
		int k = 0;
		while (j < nA || k < nB)
		{
			int num3 = rimA[j % nA];
			int num4 = rimA[(j + 1) % nA];
			int num5 = rimB[(bOff + k) % nB];
			int num6 = rimB[(bOff + k + 1) % nB];
			if (j < nA && (k >= nB || RelB(k + 1) >= RelA(j + 1)))
			{
				tris.Add(num3);
				tris.Add(num4);
				tris.Add(num5);
				j++;
			}
			else
			{
				tris.Add(num5);
				tris.Add(num3);
				tris.Add(num6);
				k++;
			}
		}
		float RelA(int index)
		{
			if (index >= nA)
			{
				return 6.28318548f;
			}
			return Wrap(Angle(world(rimA[index]), axisO, u, vAx) - a0);
		}
		float RelB(int index)
		{
			if (index >= nB)
			{
				return 6.28318548f;
			}
			return Wrap(Angle(world(rimB[(bOff + index) % nB]), axisO, u, vAx) - a0);
		}
	}

	private static float Angle(Vector3 p, Vector3 o, Vector3 u, Vector3 vAx)
	{
		Vector3 vector = p - o;
		return Mathf.Atan2(vector.x * vAx.x + vector.y * vAx.y + vector.z * vAx.z, vector.x * u.x + vector.y * u.y + vector.z * u.z);
	}

	private static float Wrap(float a)
	{
		float num = a % 6.28318548f;
		if (num < 0f)
		{
			num += 6.28318548f;
		}
		return num;
	}

	public Mesh GetMesh()
	{
		return mesh;
	}

	public Vector3[] GetBaseVertices()
	{
		return baseVertices;
	}
}
