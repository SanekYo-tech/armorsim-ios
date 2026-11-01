using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ContinuumMeshBinder : IDisposable
{
	private sealed class ShellSet
	{
		public LatheKind[] kinds;

		public LatheTopology[] full;

		public HalfLatheTopology[] half;

		public int[] offset;

		public int vertexTotal;

		public int[] triangles;
	}

	public static int MeshesAllocated;

	public readonly int profilePoints;

	public readonly int segments;

	public bool flatShaded;

	private Mesh projectileMesh;

	private Mesh targetMesh;

	private bool lastHalfTurn;

	public bool halfTurn;

	private readonly Vector2[] arc;

	private NativeArray<Vector3> verts;

	private NativeArray<Vector3> normals;

	public UltraFieldGrid.Bounds fieldBounds;

	private NativeArray<Vector2> uvs;

	private Vector3[] capVerts = new Vector3[0];

	private readonly List<int> capPoly = new List<int>();

	private readonly List<int> capTris = new List<int>();

	private int vertexCapacity;

	public static int CapRetries;

	public static int CapSavedByRaw;

	public static int ShellSplitMismatches;

	private readonly ShellSet projShells = new ShellSet();

	private readonly ShellSet targShells = new ShellSet();

	private readonly List<int> shellBounds = new List<int>();

	private Vector2[][] arcs = new Vector2[0][];

	private LatheKind[] kindBuf = new LatheKind[0];

	public bool smoothProfile = true;

	public int smoothSubdivision = 3;

	public Mesh ProjectileMesh => projectileMesh;

	public Mesh TargetMesh => targetMesh;

	public LatheTopology ProjectileTopology
	{
		get
		{
			if (projShells.full.Length == 0)
			{
				return null;
			}
			return projShells.full[0];
		}
	}

	public LatheTopology TargetTopology
	{
		get
		{
			if (targShells.full.Length == 0)
			{
				return null;
			}
			return targShells.full[0];
		}
	}

	public ContinuumMeshBinder(int profilePoints = 256, int segments = 48)
	{
		this.profilePoints = profilePoints;
		this.segments = segments;
		arc = new Vector2[profilePoints];
		vertexCapacity = (segments + 3) * profilePoints;
		verts = new NativeArray<Vector3>(vertexCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		normals = new NativeArray<Vector3>(vertexCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		uvs = new NativeArray<Vector2>(vertexCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		projectileMesh = NewMesh("ULTRA/стержень");
		targetMesh = NewMesh("ULTRA/преграда");
	}

	private static Mesh NewMesh(string name)
	{
		MeshesAllocated++;
		Mesh mesh = new Mesh();
		mesh.name = name;
		mesh.MarkDynamic();
		mesh.indexFormat = IndexFormat.UInt32;
		return mesh;
	}

	public void Apply(GeometryFrame frame)
	{
		if (lastHalfTurn != halfTurn)
		{
			lastHalfTurn = halfTurn;
			Reset(projShells);
			Reset(targShells);
		}
		int num;
		if (!halfTurn)
		{
			num = ApplyBodyFull(frame.projectileProfile, projectileMesh, projShells);
			CheckShells(num, frame.projectileLoopCount, "стержень", frame.time);
			num = ApplyBodyFull(frame.targetProfile, targetMesh, targShells);
		}
		else
		{
			num = ApplyBodyHalf(frame.projectileProfile, projectileMesh, projShells);
			CheckShells(num, frame.projectileLoopCount, "стержень", frame.time);
			num = ApplyBodyHalf(frame.targetProfile, targetMesh, targShells);
		}
		CheckShells(num, frame.targetLoopCount, "преграда", frame.time);
	}

	private static void CheckShells(int shells, int loops, string body, float timeS)
	{
		if (shells == loops || shells == 1)
		{
			return;
		}
		ShellSplitMismatches++;
		if (ShellSplitMismatches > 1)
		{
			return;
		}
		Debug.LogError("ULTRA показ: РАЗРЕЗ ОБОЛОЧЕК РАСХОДИТСЯ С ИЗВЛЕЧЕНИЕМ — тело «" + body + "», " + string.Format("кадр t = {0:F2} мкс: петель {1}, оболочек после разреза {2}. ", timeS * 1000000f, loops, shells) + "Правило разреза (петля возвращается в свою первую точку) сработало не там — скорее всего обход вернулся в стартовую точку раньше конца петли (узел-перешеек, оказавшийся стартовым). Меш этого кадра построен по лишней оболочке, и как он выглядит, ни о чём не говорит. Дальнейшие такие кадры считаются, но не печатаются: их число — в сводке показа.");
	}

	private static void Reset(ShellSet s)
	{
		s.kinds = new LatheKind[0];
		s.full = new LatheTopology[0];
		s.half = new HalfLatheTopology[0];
		s.offset = new int[0];
		s.triangles = new int[0];
		s.vertexTotal = 0;
	}

	private void EnsureArcs(int shells)
	{
		if (kindBuf.Length < shells)
		{
			kindBuf = new LatheKind[shells];
		}
		if (arcs.Length < shells)
		{
			Vector2[][] array = new Vector2[shells][];
			for (int i = 0; i < shells; i++)
			{
				array[i] = ((i < arcs.Length) ? arcs[i] : new Vector2[profilePoints]);
			}
			arcs = array;
		}
	}

	private void EnsureCapacity(int need)
	{
		if (need <= vertexCapacity)
		{
			return;
		}
		if (verts.IsCreated)
		{
			verts.Dispose();
		}
		if (normals.IsCreated)
		{
			normals.Dispose();
		}
		if (uvs.IsCreated)
		{
			uvs.Dispose();
		}
		vertexCapacity = need;
		verts = new NativeArray<Vector3>(need, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		normals = new NativeArray<Vector3>(need, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		uvs = new NativeArray<Vector2>(need, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
	}

	private int ExtractShells(NativeArray<Vector2> profile, bool smooth)
	{
		int shells = ProfileLathe.SplitShells(profile, shellBounds);
		if (shells == 0)
		{
			return 0;
		}
		EnsureArcs(shells);
		for (int i = 0; i < shells; i++)
		{
			int num = shellBounds[i];
			int num2 = shellBounds[i + 1];
			NativeArray<Vector2> nativeArray = new NativeArray<Vector2>(num2 - num, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			for (int j = 0; j < num2 - num; j++)
			{
				nativeArray[j] = profile[num + j];
			}
			NativeArray<Vector2> nativeArray2 = nativeArray;
			bool flag = false;
			if (smooth && num2 - num >= 4)
			{
				nativeArray2 = ProfileSmoothing.Smooth(nativeArray, smoothSubdivision, Allocator.Temp);
				flag = true;
			}
			kindBuf[i] = ProfileLathe.ExtractOuterArc(nativeArray2, arcs[i]);
			if (flag && nativeArray2.IsCreated)
			{
				nativeArray2.Dispose();
			}
			nativeArray.Dispose();
		}
		return shells;
	}

	private int ApplyBodyFull(NativeArray<Vector2> profile, Mesh mesh, ShellSet sh)
	{
		int shells = ExtractShells(profile, smoothProfile && profile.IsCreated);
		if (shells == 0)
		{
			mesh.Clear();
			Reset(sh);
			return 0;
		}
		bool flag = sh.full.Length != shells || sh.kinds.Length != shells;
		if (!flag)
		{
			for (int i = 0; i < shells; i++)
			{
				if (sh.kinds[i] != kindBuf[i] || sh.full[i] == null || sh.full[i].profilePoints != profilePoints || sh.full[i].segments != segments)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			RebuildFull(sh, shells);
		}
		if (sh.vertexTotal == 0)
		{
			mesh.Clear();
			return shells;
		}
		for (int j = 0; j < shells; j++)
		{
			ProfileLathe.BuildVertices(arcs[j], sh.full[j], verts, normals, flatShaded, sh.offset[j]);
		}
		WriteUVs(sh.vertexTotal);
		if (flag)
		{
			mesh.Clear();
		}
		mesh.SetVertices(verts, 0, sh.vertexTotal);
		mesh.SetNormals(normals, 0, sh.vertexTotal);
		if (fieldBounds.Valid)
		{
			mesh.SetUVs(0, uvs, 0, sh.vertexTotal);
		}
		if (flag)
		{
			mesh.subMeshCount = 1;
			mesh.SetTriangles(sh.triangles, 0, true);
		}
		return shells;
	}

	private void RebuildFull(ShellSet sh, int shells)
	{
		sh.kinds = new LatheKind[shells];
		sh.full = new LatheTopology[shells];
		sh.half = new HalfLatheTopology[0];
		sh.offset = new int[shells];
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < shells; i++)
		{
			sh.kinds[i] = kindBuf[i];
			sh.full[i] = ProfileLathe.MakeTopology(kindBuf[i], profilePoints, segments);
			sh.offset[i] = num;
			num += sh.full[i].vertexCount;
			num2 += sh.full[i].triangles.Length;
		}
		sh.vertexTotal = num;
		sh.triangles = new int[num2];
		int num3 = 0;
		for (int j = 0; j < shells; j++)
		{
			int[] array = sh.full[j].triangles;
			int num4 = sh.offset[j];
			for (int k = 0; k < array.Length; k++)
			{
				sh.triangles[num3++] = array[k] + num4;
			}
		}
		EnsureCapacity(sh.vertexTotal);
	}

	private int ApplyBodyHalf(NativeArray<Vector2> profile, Mesh mesh, ShellSet sh)
	{
		bool smooth = smoothProfile && profile.IsCreated;
		int failures = MeridianCap.Failures;
		int num = BuildHalf(profile, mesh, sh, smooth);
		if (smooth && MeridianCap.Failures != failures)
		{
			CapRetries++;
			int failures2 = MeridianCap.Failures;
			num = BuildHalf(profile, mesh, sh, smooth: false);
			if (MeridianCap.Failures == failures2)
			{
				CapSavedByRaw++;
			}
		}
		return num;
	}

	private int BuildHalf(NativeArray<Vector2> profile, Mesh mesh, ShellSet sh, bool smooth)
	{
		int shells = ExtractShells(profile, smooth);
		if (shells == 0)
		{
			mesh.Clear();
			Reset(sh);
			return 0;
		}
		bool flag = sh.half.Length != shells || sh.kinds.Length != shells;
		if (!flag)
		{
			for (int i = 0; i < shells; i++)
			{
				if (sh.kinds[i] != kindBuf[i] || sh.half[i] == null || sh.half[i].profilePoints != profilePoints || sh.half[i].segments != segments)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			RebuildHalf(sh, shells);
		}
		if (sh.vertexTotal == 0)
		{
			mesh.Clear();
			return shells;
		}
		for (int j = 0; j < shells; j++)
		{
			HalfLathe.BuildVertices(arcs[j], sh.half[j], verts, normals, sh.offset[j]);
		}
		WriteUVs(sh.vertexTotal);
		if (flag)
		{
			mesh.Clear();
		}
		mesh.SetVertices(verts, 0, sh.vertexTotal);
		mesh.SetNormals(normals, 0, sh.vertexTotal);
		if (fieldBounds.Valid)
		{
			mesh.SetUVs(0, uvs, 0, sh.vertexTotal);
		}
		if (flag)
		{
			mesh.subMeshCount = 2;
			mesh.SetTriangles(sh.triangles, 0, true);
		}
		BuildCaps(mesh, sh, shells);
		return shells;
	}

	private void RebuildHalf(ShellSet sh, int shells)
	{
		sh.kinds = new LatheKind[shells];
		sh.half = new HalfLatheTopology[shells];
		sh.full = new LatheTopology[0];
		sh.offset = new int[shells];
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < shells; i++)
		{
			sh.kinds[i] = kindBuf[i];
			sh.half[i] = HalfLathe.MakeTopology(kindBuf[i], profilePoints, segments);
			sh.offset[i] = num;
			num += sh.half[i].VertexCount;
			num2 += sh.half[i].sideTriangles.Length;
		}
		sh.vertexTotal = num;
		sh.triangles = new int[num2];
		int num3 = 0;
		for (int j = 0; j < shells; j++)
		{
			int[] array = sh.half[j].sideTriangles;
			int num4 = sh.offset[j];
			for (int k = 0; k < array.Length; k++)
			{
				sh.triangles[num3++] = array[k] + num4;
			}
		}
		EnsureCapacity(sh.vertexTotal);
	}

	private void BuildCaps(Mesh mesh, ShellSet sh, int shells)
	{
		capTris.Clear();
		for (int i = 0; i < shells; i++)
		{
			HalfLatheTopology halfLatheTopology = sh.half[i];
			int num = sh.offset[i] + halfLatheTopology.sideVertexCount;
			if (halfLatheTopology.capVertexCount > 0)
			{
				if (capVerts.Length != halfLatheTopology.capVertexCount)
				{
					capVerts = new Vector3[halfLatheTopology.capVertexCount];
				}
				for (int j = 0; j < halfLatheTopology.capVertexCount; j++)
				{
					capVerts[j] = verts[num + j];
				}
				int num2 = capTris.Count;
				bool flag;
				if (halfLatheTopology.kind == LatheKind.Tube)
				{
					flag = Ring(0, halfLatheTopology.segments);
					if (flag)
					{
						flag = Ring(1, halfLatheTopology.segments);
					}
				}
				else
				{
					capPoly.Clear();
					for (int k = 0; k < halfLatheTopology.segments; k++)
					{
						capPoly.Add(k);
					}
					for (int num3 = halfLatheTopology.segments - 2; num3 >= 1; num3--)
					{
						capPoly.Add(num3);
					}
					flag = MeridianCap.Triangulate(capVerts, capPoly, capTris);
				}
				if (flag)
				{
					while (num2 < capTris.Count)
					{
						capTris[num2] += num;
						num2++;
					}
				}
				else if (capTris.Count > num2)
				{
					capTris.RemoveRange(num2, capTris.Count - num2);
				}
			}
			AppendBand(halfLatheTopology, sh.offset[i]);
		}
		mesh.SetTriangles(capTris, 1, true);
	}

	private void AppendBand(HalfLatheTopology t, int offset)
	{
		int[] capTriangles = t.capTriangles;
		for (int i = 0; i < capTriangles.Length; i++)
		{
			capTris.Add(capTriangles[i] + offset);
		}
	}

	private bool Ring(int side, int N)
	{
		capPoly.Clear();
		for (int i = 0; i < N; i++)
		{
			capPoly.Add(side * N + i);
		}
		return MeridianCap.Triangulate(capVerts, capPoly, capTris);
	}

	private void WriteUVs(int count)
	{
		if (!fieldBounds.Valid)
		{
			return;
		}
		float num = 1f / fieldBounds.rMaxM;
		float num2 = 1f / (fieldBounds.zMaxM - fieldBounds.zMinM);
		for (int i = 0; i < count; i++)
		{
			Vector3 vector = verts[i];
			uvs[i] = new Vector2(Math.Clamp(Mathf.Sqrt(vector.x * vector.x + vector.y * vector.y) * num, 0f, 1f), Math.Clamp((vector.z - fieldBounds.zMinM) * num2, 0f, 1f));
		}
	}

	public void Dispose()
	{
		if (verts.IsCreated)
		{
			verts.Dispose();
		}
		if (normals.IsCreated)
		{
			normals.Dispose();
		}
		if (uvs.IsCreated)
		{
			uvs.Dispose();
		}
	}
}
