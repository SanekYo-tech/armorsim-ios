using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public sealed class UltraTailFins : IDisposable
{
	public static int MeshesAllocated;

	public int finCount;

	public float rootChordOverD;

	public float tipChordFraction;

	public float spanOverD;

	public float sweepAngleDeg;

	public float thicknessM;

	public float cantAngleDeg;

	private Mesh mesh;

	private Transform anchor;

	private float rodD;

	public int[] destroyFrame;

	private bool[] lastAlive;

	public float TailSpeed { get; private set; }

	public Mesh Mesh => mesh;

	public float SpanM => spanOverD * rodD;

	public float TipRadiusM => spanOverD * rodD + rodD * 0.5f;

	public UltraTailFins(Transform parent, float rodDiameterM, Material material = null)
	{
		finCount = 6;
		rootChordOverD = 1.2f;
		tipChordFraction = 0.25f;
		spanOverD = 0.8f;
		sweepAngleDeg = 35f;
		thicknessM = 0.002f;
		rodD = rodDiameterM;
		if (parent == null)
		{
			return;
		}
		MeshesAllocated++;
		mesh = new Mesh();
		mesh.name = "ULTRA/оперение";
		mesh.MarkDynamic();
		GameObject gameObject = new GameObject("UltraTailFins");
		anchor = gameObject.transform;
		anchor.SetParent(parent, false);
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		if (material != null)
		{
			meshRenderer.sharedMaterial = material;
		}
		else
		{
			Debug.LogWarning("ULTRA оперение: материал не передан — рендерер останется пурпурным. Это дефект вызывающего, а не отсутствие меша.");
		}
		Rebuild(null);
	}

	public void Rebuild(bool[] alive)
	{
		if (mesh == null)
		{
			return;
		}
		float spanM = rodD * spanOverD;
		float rootChordM = rodD * rootChordOverD;
		float r = rodD * 0.5f;
		float tipChordM = Mathf.Max(0f, tipChordFraction) * rootChordM;
		float sweepDeg = Mathf.Clamp(sweepAngleDeg, 0f, 80f);
		float sweepBack = Mathf.Tan(sweepDeg * Mathf.Deg2Rad) * spanM;
		int n = Mathf.Max(1, finCount);
		float cantRad = cantAngleDeg * Mathf.Deg2Rad;
		float halfT = thicknessM * 0.5f;
		List<Vector3> vertices = new List<Vector3>(n * 8);
		List<int> triangles = new List<int>(n * 36);
		for (int i = 0; i < n; i++)
		{
			if (alive != null && i < alive.Length && !alive[i])
			{
				continue;
			}
			float angle = (float)i * 6.28318548f / (float)n + cantRad;
			float ca = Mathf.Cos(angle);
			float sa = Mathf.Sin(angle);
			Vector3 radial = new Vector3(-sa, ca, 0f) * halfT;
			float tipZ = sweepBack + tipChordM;
			int b = vertices.Count;
			float tipRadius = spanM + r;
			Vector3 rootPos = new Vector3(ca, sa, 0f) * r;
			Vector3 tipPos = new Vector3(ca, sa, 0f) * tipRadius;
			for (int side = 0; side < 2; side++)
			{
				Vector3 off = radial;
				if (side != 0)
				{
					off = -off;
				}
				vertices.Add(rootPos + off);
				vertices.Add(rootPos + off + new Vector3(0f, 0f, rootChordM));
				vertices.Add(tipPos + off + new Vector3(0f, 0f, tipZ));
				vertices.Add(tipPos + off + new Vector3(0f, 0f, sweepBack));
			}
			int[] tri = new int[36]
			{
				b + 0, b + 1, b + 2, b + 0, b + 2, b + 3, b + 6, b + 5, b + 4, b + 7,
				b + 6, b + 4, b + 0, b + 4, b + 5, b + 0, b + 5, b + 1, b + 1, b + 5,
				b + 6, b + 1, b + 6, b + 2, b + 2, b + 6, b + 7, b + 2, b + 7, b + 3,
				b + 3, b + 7, b + 4, b + 3, b + 4, b + 0
			};
			for (int j = 0; j < tri.Length; j += 3)
			{
				Vector3 p0 = vertices[tri[j]];
				Vector3 p1 = vertices[tri[j + 1]];
				Vector3 p2 = vertices[tri[j + 2]];
				Vector3 cross = Vector3.Cross(p1 - p0, p2 - p0);
				if (cross.sqrMagnitude < 1e-18f)
				{
					continue;
				}
				triangles.Add(tri[j]);
				triangles.Add(tri[j + 1]);
				triangles.Add(tri[j + 2]);
			}
		}
		mesh.Clear();
		if (vertices.Count > 0)
		{
			mesh.SetVertices(vertices);
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
		}
	}

	public void Bake(GeometryTimeline tl, float plateFaceZ)
	{
		int n = Mathf.Max(1, finCount);
		destroyFrame = new int[n];
		for (int i = 0; i < n; i++)
		{
			destroyFrame[i] = -1;
		}
		for (int f = 0; f < tl.frames.Count; f++)
		{
			float tailZ = TailZ(tl.frames[f].projectileProfile);
			if (float.IsNaN(tailZ) || plateFaceZ > tailZ + rodD * rootChordOverD)
			{
				continue;
			}
			if (HoleRadiusAt(tl.frames[f].targetProfile, plateFaceZ) >= rodD * spanOverD + rodD * 0.5f)
			{
				continue;
			}
			for (int k = 0; k < n; k++)
			{
				if (destroyFrame[k] < 0)
				{
					destroyFrame[k] = f;
				}
			}
			return;
		}
	}

	public static float HoleRadiusAt(NativeArray<Vector2> targetProfile, float faceZ)
	{
		if (!targetProfile.IsCreated || targetProfile.Length == 0)
		{
			return 0f;
		}
		Vector2 best = new Vector2(float.MaxValue, float.MaxValue);
		for (int i = 0; i < targetProfile.Length; i++)
		{
			Vector2 p = targetProfile[i];
			if (Mathf.Abs(p.y - faceZ) < 0.002f && best.x > p.x)
			{
				best = p;
			}
		}
		if (best.x != float.MaxValue)
		{
			return best.x;
		}
		return 0f;
	}

	public void Apply(GeometryFrame frame, GeometryFrame previous, float dtOutput, int frameIndex)
	{
		if (anchor == null)
		{
			return;
		}
		float tailZ = TailZ(frame.projectileProfile);
		if (float.IsNaN(tailZ))
		{
			return;
		}
		anchor.localPosition = new Vector3(0f, 0f, tailZ);
		if (dtOutput > 1e-09f)
		{
			float prevTailZ = TailZ(previous.projectileProfile);
			if (!float.IsNaN(prevTailZ))
			{
				TailSpeed = (tailZ - prevTailZ) / dtOutput;
			}
		}
		if (destroyFrame == null)
		{
			return;
		}
		bool[] alive = new bool[destroyFrame.Length];
		bool changed = false;
		bool anyAlive = false;
		for (int i = 0; i < destroyFrame.Length; i++)
		{
			alive[i] = destroyFrame[i] < 0 || frameIndex < destroyFrame[i];
			anyAlive |= alive[i];
			bool last = lastAlive == null || i >= lastAlive.Length || lastAlive[i];
			if (alive[i] != last)
			{
				changed = true;
			}
		}
		if (changed)
		{
			Rebuild(anyAlive ? alive : new bool[destroyFrame.Length]);
			lastAlive = alive;
		}
	}

	private static float TailZ(NativeArray<Vector2> profile)
	{
		if (!profile.IsCreated || profile.Length == 0)
		{
			return float.NaN;
		}
		float num = float.MaxValue;
		for (int i = 0; i < profile.Length; i++)
		{
			Vector2 vector = profile[i];
			if (num > vector.y)
			{
				num = vector.y;
			}
		}
		return num;
	}

	public void Dispose()
	{
		if (mesh != null)
		{
			UnityEngine.Object.Destroy(mesh);
			mesh = null;
		}
		if (anchor != null)
		{
			UnityEngine.Object.Destroy(anchor.gameObject);
			anchor = null;
		}
	}
}
