using UnityEngine;

public sealed class ModelPart
{
	public string name;

	public Vector3[] vertices;

	public int[] triangles;

	public bool closed;

	public int boundaryEdges;

	public int nonManifoldEdges;

	public double volumeM3;

	public Bounds bounds;
}
