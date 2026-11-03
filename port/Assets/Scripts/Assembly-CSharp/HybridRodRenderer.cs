using System;
using System.Collections.Generic;
using UnityEngine;

public class HybridRodRenderer
{
	private GameObject go;

	private Mesh mesh;

	private MeshRenderer mr;

	private readonly List<Vector3> verts;

	private readonly List<int> tris;

	private const int SegPerRing = 16;

	public Renderer DrawnRenderer => (mr != null && mr.enabled && go != null && go.activeInHierarchy) ? mr : null;

	public HybridRodRenderer()
	{
		verts = new List<Vector3>(1024);
		tris = new List<int>(4096);
	}

	public void SetActive(bool on)
	{
		if (go != null)
		{
			go.SetActive(on);
		}
	}

	public void Destroy()
	{
		if (go != null)
		{
			UnityEngine.Object.Destroy(go);
		}
		go = null;
	}

	private void Ensure(Transform parent, Material mat)
	{
		if (go == null)
		{
			go = new GameObject("HybridRodMesh", typeof(MeshFilter), typeof(MeshRenderer));
			go.transform.SetParent(parent, false);
			mesh = new Mesh();
			mesh.name = "HybridRod";
			go.GetComponent<MeshFilter>().sharedMesh = mesh;
			mr = go.GetComponent<MeshRenderer>();
			mr.sharedMaterial = mat;
		}
	}

	public void Render(Transform parent, Material mat, Vector3 anchor, Quaternion rot, float scale, Vector3[] centersM, bool[] alive, float[] restCoordM, Func<float, float> radiusAtW, Vector3 planePointM, Vector3 planeNormalM, float[] ringDropM, float rodRadiusM, float erodedM)
	{
		Ensure(parent, mat);
		if (!go.activeSelf)
		{
			go.SetActive(true);
		}
		if (mr != null && mr.sharedMaterial != mat)
		{
			mr.sharedMaterial = mat;
		}
		HybridRodLoft.Build(anchor, rot, scale, centersM, alive, restCoordM, radiusAtW, planePointM, planeNormalM, ringDropM, rodRadiusM, erodedM, verts, tris);
		mesh.Clear();
		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		if (verts.Count > 2)
		{
			MeshNormals.RecalculateSmooth(mesh, 55f, weld: false);
		}
		mesh.RecalculateBounds();
	}

	private void Push()
	{
		mesh.Clear();
		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		if (verts.Count > 2)
		{
			MeshNormals.RecalculateSmooth(mesh, 55f, weld: false);
		}
		mesh.RecalculateBounds();
	}
}
