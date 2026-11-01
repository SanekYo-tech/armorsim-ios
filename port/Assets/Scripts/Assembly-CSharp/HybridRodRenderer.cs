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

	public Renderer DrawnRenderer => null;

	public void SetActive(bool on)
	{
	}

	public void Destroy()
	{
	}

	private void Ensure(Transform parent, Material mat)
	{
	}

	public void Render(Transform parent, Material mat, Vector3 anchor, Quaternion rot, float scale, Vector3[] centersM, bool[] alive, float[] restCoordM, Func<float, float> radiusAtW, Vector3 planePointM, Vector3 planeNormalM, float[] ringDropM, float rodRadiusM, float erodedM)
	{
	}

	private void Push()
	{
	}
}
