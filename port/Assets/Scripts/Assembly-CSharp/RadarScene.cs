using System;
using System.Threading;
using Unity.Mathematics;

public sealed class RadarScene
{
	internal readonly double3[] Vertices;

	internal readonly int[] Triangles;

	internal readonly double3[] Normals;

	internal readonly double[] Areas;

	internal readonly double3[] Centroids;

	public readonly double3 Center;

	public readonly double BoundingSphereRadius;

	public readonly double LongestSide;

	public readonly RadarBvh Bvh;

	public int TriangleCount => Triangles.Length / 3;

	public RadarScene(RadarModel model)
		: this(model, default(CancellationToken))
	{
	}

	public RadarScene(RadarModel model, CancellationToken cancellationToken)
	{
		if (model == null)
		{
			throw new ArgumentNullException("model");
		}
		Vertices = (double3[])model.Vertices.Clone();
		Triangles = (int[])model.Triangles.Clone();
		Normals = (double3[])model.Normals.Clone();
		Areas = (double[])model.Areas.Clone();
		Center = model.Center;
		BoundingSphereRadius = model.BoundingSphereRadius;
		LongestSide = model.LongestSide;
		Centroids = new double3[TriangleCount];
		for (int i = 0; i < TriangleCount; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			GetTriangle(i, out var a, out var b, out var c);
			Centroids[i] = new double3((a.x + b.x + c.x) / 3.0, (a.y + b.y + c.y) / 3.0, (a.z + b.z + c.z) / 3.0);
		}
		Bvh = new RadarBvh(this, cancellationToken);
	}

	public void GetTriangle(int face, out double3 a, out double3 b, out double3 c)
	{
		if (face < 0 || face >= TriangleCount)
		{
			throw new ArgumentOutOfRangeException("face");
		}
		a = Vertices[Triangles[face * 3]];
		b = Vertices[Triangles[face * 3 + 1]];
		c = Vertices[Triangles[face * 3 + 2]];
	}
}
