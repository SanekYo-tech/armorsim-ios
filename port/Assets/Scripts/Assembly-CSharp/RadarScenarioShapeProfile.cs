using System;
using UnityEngine;

public sealed class RadarScenarioShapeProfile
{
	public const int Resolution = 64;

	public const float MinScore = 0.35f;

	public const float MaxScore = 1.8f;

	public readonly float Front;

	public readonly float Side;

	public readonly float Top;

	private readonly float mean;

	public static readonly RadarScenarioShapeProfile Uniform;

	static RadarScenarioShapeProfile()
	{
		Uniform = new RadarScenarioShapeProfile(1f, 1f, 1f);
	}

	public RadarScenarioShapeProfile(float front, float side, float top)
	{
		if (!Finite(front) || !Finite(side) || !Finite(top) || front < 0f || side < 0f || top < 0f)
		{
			throw new ArgumentException("shape silhouettes must be finite and non-negative");
		}
		Front = front;
		Side = side;
		Top = top;
		mean = (front + side + top) / 3f;
		if (mean <= 0f)
		{
			throw new ArgumentException("shape silhouettes are all empty");
		}
	}

	public float Score(float relativeHeadingDeg, float elevationHint)
	{
		if (Front == Side && Side == Top)
		{
			return 1f;
		}
		float num = Finite(relativeHeadingDeg) ? (relativeHeadingDeg * 0.0174532924f) : 0f;
		float num2 = Finite(elevationHint) ? Mathf.Clamp01(elevationHint) : 0f;
		float num3 = MathF.Abs(MathF.Sin(num));
		float num4 = MathF.Abs(MathF.Cos(num));
		float num5 = (num4 * Side + num3 * Front) / Mathf.Max(num4 + num3, 1E-06f);
		return Mathf.Clamp((Top - num5) * num2 + num5 / mean, MinScore, MaxScore);
	}

	public static RadarScenarioShapeProfile FromMesh(AeroMeshData mesh)
	{
		if (mesh == null || mesh.Vertices.Length == 0 || mesh.Triangles.Length < 3)
		{
			throw new ArgumentException("shape profile needs a non-empty mesh");
		}
		Vector3 vector = Vector3.one * float.MaxValue;
		Vector3 vector2 = -(Vector3.one * float.MaxValue);
		for (int i = 0; i < mesh.Vertices.Length; i++)
		{
			Vector3 vertex = mesh.Vertices[i];
			vector = Vector3.Min(vector, vertex);
			vector2 = Vector3.Max(vector2, vertex);
		}
		Vector3 size = vector2 - vector;
		int num = Largest(size, -1, -1);
		int num2 = Largest(size, num, -1);
		int num3 = 3 - num2 - num;
		return new RadarScenarioShapeProfile(Silhouette(mesh, num2, num3), Silhouette(mesh, num, num3), Silhouette(mesh, num, num2));
	}

	private static int Largest(Vector3 size, int skipA, int skipB)
	{
		int num = -1;
		for (int i = 0; i < 3; i++)
		{
			if (i == skipA || i == skipB)
			{
				continue;
			}
			if (num < 0 || size[i] > size[num])
			{
				num = i;
			}
		}
		return num;
	}

	private static float Silhouette(AeroMeshData mesh, int ax, int ay)
	{
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = float.MinValue;
		float num4 = float.MinValue;
		for (int i = 0; i < mesh.Vertices.Length; i++)
		{
			Vector3 vertex = mesh.Vertices[i];
			float num5 = vertex[ax];
			float num6 = vertex[ay];
			if (num5 <= num)
			{
				num = num5;
			}
			if (num6 <= num2)
			{
				num2 = num6;
			}
			if (num5 >= num3)
			{
				num3 = num5;
			}
			if (num6 >= num4)
			{
				num4 = num6;
			}
		}
		float num7 = num3 - num;
		float num8 = num4 - num2;
		float num9 = num7;
		if (num9 < num8)
		{
			num9 = num8;
		}
		if (num9 <= 0f)
		{
			return 0f;
		}
		float num10 = 64f / num9;
		bool[] array = new bool[4096];
		Vector2 min = new Vector2(num, num2);
		int num11 = 0;
		for (int j = 0; j + 2 < mesh.Triangles.Length; j += 3)
		{
			Vector2 a = Project(mesh.Vertices[mesh.Triangles[j]], ax, ay, min, num10);
			Vector2 b = Project(mesh.Vertices[mesh.Triangles[j + 1]], ax, ay, min, num10);
			Vector2 c = Project(mesh.Vertices[mesh.Triangles[j + 2]], ax, ay, min, num10);
			float num12 = Edge(a, b, c);
			if (MathF.Abs(num12) >= 1E-09f)
			{
				int num13 = Mathf.Clamp(Mathf.FloorToInt(MathF.Min(MathF.Min(b.x, c.x), a.x)), 0, 63);
				int num14 = Mathf.Clamp(Mathf.CeilToInt(MathF.Max(MathF.Max(b.x, c.x), a.x)), 0, 63);
				int num15 = Mathf.Clamp(Mathf.FloorToInt(MathF.Min(MathF.Min(b.y, c.y), a.y)), 0, 63);
				int num16 = Mathf.Clamp(Mathf.CeilToInt(MathF.Max(MathF.Max(b.y, c.y), a.y)), 0, 63);
				for (int k = num15; k <= num16; k++)
				{
					for (int l = num13; l <= num14; l++)
					{
						int num17 = k * 64 + l;
						if (array[num17])
						{
							continue;
						}
						Vector2 vector = new Vector2(l + 0.5f, k + 0.5f);
						if (Edge(a, b, vector) / num12 >= 0f && Edge(b, c, vector) / num12 >= 0f && Edge(c, a, vector) / num12 >= 0f)
						{
							array[num17] = true;
							num11++;
						}
					}
				}
			}
		}
		float num18 = num9 / 64f;
		return num11 * num18 * num18;
	}

	private static Vector2 Project(Vector3 v, int ax, int ay, Vector2 min, float scale)
	{
		return new Vector2((v[ax] - min.x) * scale, (v[ay] - min.y) * scale);
	}

	private static float Edge(Vector2 a, Vector2 b, Vector2 p)
	{
		return (p.y - a.y) * (b.x - a.x) - (b.y - a.y) * (p.x - a.x);
	}

	private static bool Finite(float f)
	{
		return float.IsFinite(f);
	}
}
