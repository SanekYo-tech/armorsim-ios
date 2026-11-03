using UnityEngine;

public sealed class ImportedPart
{
	public string name;

	public Vector3[] vertices;

	public int[] triangles;

	public int TriangleCount => (triangles != null) ? (triangles.Length / 3) : 0;
}
