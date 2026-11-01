using System.Collections.Generic;
using UnityEngine;

public class NeraBulge : MonoBehaviour
{
	public struct Node
	{
		public Vector2 dir;

		public float tMax;

		public float ring;

		public float zSide;
	}

	public const float InteractionSeconds = 7.1E-05f;

	public const float MinRiseSeconds = 0.3f;

	public const float VisualGain = 1f;

	private const float HoleStartFraction = 0.3f;

	private const float MaxObliquityStretch = 3f;

	private const int Sectors = 48;

	private const int Rings = 16;

	private const float RingBias = 1.7f;

	private Mesh mesh;

	private Node[] nodes;

	private Vector3[] work;

	private float ampFrontLocal;

	private float ampBackLocal;

	private float holeLocalFull;

	private float rimAmp;

	private float riseSeconds;

	private float elapsed;

	private Vector2 center;

	public static int SectorCount => 48;

	public static int RingCount => 16;

	public static void Begin(Renderer part, float dispFrontWorld, float dispBackWorld, Vector3 threatDirWorld, float timeDilation, Vector3? hitPoint, float holeRadiusWorld, float rimRaggedAmp = 0f)
	{
		if (part == null)
		{
			return;
		}
		NeraBulge bulge = part.gameObject.GetComponent<NeraBulge>();
		if (bulge == null)
		{
			bulge = part.gameObject.AddComponent<NeraBulge>();
		}
		bulge.Setup(part, dispFrontWorld, dispBackWorld, threatDirWorld, timeDilation, hitPoint, holeRadiusWorld, rimRaggedAmp);
	}

	private void Setup(Renderer part, float dispFrontWorld, float dispBackWorld, Vector3 threatDirWorld, float timeDilation, Vector3? hitPoint, float holeRadiusWorld, float rimRaggedAmp)
	{
		MeshFilter mf = part.GetComponent<MeshFilter>();
		if (mf == null)
		{
			enabled = false;
			return;
		}
		Vector3 s = part.transform.lossyScale;
		float sx = Mathf.Max(Mathf.Abs(s.x), 1e-06f);
		float sy = Mathf.Max(Mathf.Abs(s.y), 1e-06f);
		float sz = Mathf.Max(Mathf.Abs(s.z), 1e-06f);
		Vector3 dir = Quaternion.Inverse(part.transform.rotation) * threatDirWorld;
		if (dir.x * dir.x + dir.y * dir.y + dir.z * dir.z < 1e-12f)
		{
			dir = Vector3.forward;
		}
		dir = dir.normalized;
		float stretch = MaxObliquityStretch;
		if (Mathf.Abs(dir.z) > 0.0001f)
		{
			stretch = Mathf.Clamp(1f / Mathf.Abs(dir.z), 1f, MaxObliquityStretch);
		}
		Vector2 axis = new Vector2(dir.x, dir.y);
		if (axis.x * axis.x + axis.y * axis.y > 1e-10f)
		{
			axis = axis.normalized;
		}
		else
		{
			axis = Vector2.right;
		}
		center = Vector2.zero;
		if (hitPoint.HasValue)
		{
			Vector3 p = part.transform.InverseTransformPoint(hitPoint.Value);
			center = new Vector2(Mathf.Clamp(p.x, -0.5f, 0.5f), Mathf.Clamp(p.y, -0.5f, 0.5f));
		}
		rimAmp = Mathf.Clamp(rimRaggedAmp, 0f, 0.8f);
		holeLocalFull = Mathf.Clamp(holeRadiusWorld, 1e-05f, Mathf.Min(sx, sy) * 0.42f / ((rimAmp + 1f) * stretch));
		float ax = Mathf.Max(0.5f - holeLocalFull * stretch * (rimAmp + 1f) / sx - 0.02f, 0.02f);
		float ay = Mathf.Max(0.5f - holeLocalFull * stretch * (rimAmp + 1f) / sy - 0.02f, 0.02f);
		center = new Vector2(Mathf.Clamp(center.x, -ax, ax), Mathf.Clamp(center.y, -ay, ay));
		ampFrontLocal = dispFrontWorld / sz;
		ampBackLocal = dispBackWorld / sz;
		riseSeconds = Mathf.Max(Mathf.Max(timeDilation, 1f) * InteractionSeconds, MinRiseSeconds);
		elapsed = 0f;
		int[] tris;
		BuildTopology(center, sx, sy, axis, stretch, rimAmp, out nodes, out tris);
		work = new Vector3[nodes.Length];
		mesh = new Mesh();
		mesh.name = "NeraPartHoled";
		mesh.SetVertices(new List<Vector3>(new Vector3[nodes.Length]));
		mesh.SetTriangles(tris, 0);
		mf.mesh = mesh;
		enabled = true;
		Apply(0f);
	}

	private void Update()
	{
		if (mesh == null)
		{
			enabled = false;
		}
		else if (elapsed >= riseSeconds)
		{
			Apply(1f);
		}
		else
		{
			elapsed += Time.deltaTime;
			Apply(Mathf.Clamp(elapsed / riseSeconds, 0f, 1f));
		}
	}

	private void Apply(float k)
	{
		float s = 1f - (1f - k) * (1f - k);
		float grow = s * 0.7f + 0.3f;
		for (int i = 0; i < nodes.Length; i++)
		{
			Node n = nodes[i];
			float amp = ((n.zSide > 0f) ? ampFrontLocal : ampBackLocal);
			float u = Mathf.Pow(n.ring, RingBias);
			float t = (n.tMax - holeLocalFull * grow) * u + holeLocalFull * grow;
			float x = t * n.dir.x + center.x;
			float y = t * n.dir.y + center.y;
			float z = n.zSide + Profile(u) * amp * s;
			work[i] = new Vector3(x, y, z);
		}
		mesh.vertices = work;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	}

	private static float Profile(float u)
	{
		u = Mathf.Clamp(u, 0f, 1f);
		float c = Mathf.Cos(u * Mathf.PI * 0.5f);
		return c * c;
	}

	public static void BuildTopology(Vector2 center, float sx, float sy, Vector2 axis, float stretch, float rimAmp, out Node[] nodesOut, out int[] trisOut)
	{
		List<Node> nodes = new List<Node>(1824);
		List<int> tris = new List<int>();
		if (axis.x * axis.x + axis.y * axis.y <= 1e-10f)
		{
			axis = Vector2.right;
		}
		else
		{
			axis = axis.normalized;
		}
		float ax = axis.x;
		float ay = axis.y;
		Vector2[] dirs = new Vector2[48];
		float[] tMax = new float[48];
		for (int j = 0; j < 48; j++)
		{
			float a = (float)j * 6.28318548f / 48f;
			float c = Mathf.Cos(a);
			float s = Mathf.Sin(a);
			float rim = Mathf.Max(RimNoise(j) * rimAmp + 1f, 0.2f);
			dirs[j] = new Vector2(rim * (s * -ay + c * stretch * ax) / sx, rim * (s * ax + c * stretch * ay) / sy);
			tMax[j] = RayToSquare(center, dirs[j]);
		}
		int frontStart = nodes.Count;
		AddFaceNodes(nodes, dirs, tMax, 0.5f);
		int backStart = nodes.Count;
		AddFaceNodes(nodes, dirs, tMax, -0.5f);
		AddFaceTriangles(tris, frontStart, front: true);
		AddFaceTriangles(tris, backStart, front: false);
		int holeFrontStart = nodes.Count;
		CopyRing(nodes, dirs, tMax, 0f, 0.5f);
		int holeBackStart = nodes.Count;
		CopyRing(nodes, dirs, tMax, 0f, -0.5f);
		AddWall(tris, holeFrontStart, holeBackStart, outward: false);
		int rimFrontStart = nodes.Count;
		CopyRing(nodes, dirs, tMax, 1f, 0.5f);
		int rimBackStart = nodes.Count;
		CopyRing(nodes, dirs, tMax, 1f, -0.5f);
		AddWall(tris, rimFrontStart, rimBackStart, outward: true);
		nodesOut = nodes.ToArray();
		trisOut = tris.ToArray();
	}

	public static Vector3 NodePosition(Node n, Vector2 center, float hole, float amp)
	{
		float u = Mathf.Pow(n.ring, RingBias);
		float t = (n.tMax - hole) * u + hole;
		return new Vector3(t * n.dir.x + center.x, t * n.dir.y + center.y, n.zSide + ProfileAt(u) * amp);
	}

	public static float ProfileAt(float u)
	{
		u = Mathf.Clamp(u, 0f, 1f);
		float c = Mathf.Cos(u * Mathf.PI * 0.5f);
		return c * c;
	}

	public static float RimNoise(int j)
	{
		float a = (float)j * 6.28318548f / 48f;
		return Mathf.Sin(a * 9f + 4.2f) * 0.3f + Mathf.Sin(a * 5f + 1.7f) * 0.6f + Mathf.Sin(a * 17f + 2.9f) * 0.1f;
	}

	private static void AddFaceNodes(List<Node> list, Vector2[] dirs, float[] tMax, float z)
	{
		for (int i = 0; i <= 16; i++)
		{
			for (int j = 0; j < 48; j++)
			{
				Node node = default(Node);
				node.dir = dirs[j];
				node.tMax = tMax[j];
				node.ring = (float)i * 0.0625f;
				node.zSide = z;
				list.Add(node);
			}
		}
	}

	private static void CopyRing(List<Node> list, Vector2[] dirs, float[] tMax, float ring, float z)
	{
		for (int j = 0; j < 48; j++)
		{
			Node node = default(Node);
			node.dir = dirs[j];
			node.tMax = tMax[j];
			node.ring = ring;
			node.zSide = z;
			list.Add(node);
		}
	}

	private static void AddFaceTriangles(List<int> tris, int start, bool front)
	{
		for (int r = 0; r < 16; r++)
		{
			for (int j = 0; j < 48; j++)
			{
				int a = start + 48 * r + j;
				int b = a + 48;
				int c = start + 48 * r + (j + 1) % 48;
				int d = c + 48;
				if (front)
				{
					tris.Add(a);
					tris.Add(b);
					tris.Add(d);
					tris.Add(a);
					tris.Add(d);
					tris.Add(c);
				}
				else
				{
					tris.Add(a);
					tris.Add(d);
					tris.Add(b);
					tris.Add(a);
					tris.Add(c);
					tris.Add(d);
				}
			}
		}
	}

	private static void AddWall(List<int> tris, int frontStart, int backStart, bool outward)
	{
		for (int j = 0; j < 48; j++)
		{
			int a = frontStart + j;
			int b = backStart + j;
			int c = frontStart + (j + 1) % 48;
			int d = backStart + (j + 1) % 48;
			if (outward)
			{
				tris.Add(a);
				tris.Add(b);
				tris.Add(d);
				tris.Add(a);
				tris.Add(d);
				tris.Add(c);
			}
			else
			{
				tris.Add(a);
				tris.Add(d);
				tris.Add(b);
				tris.Add(a);
				tris.Add(c);
				tris.Add(d);
			}
		}
	}

	private static float RayToSquare(Vector2 c, Vector2 d)
	{
		float tx;
		if (d.x > 1e-09f)
		{
			tx = (0.5f - c.x) / d.x;
		}
		else if (d.x < -1e-09f)
		{
			tx = (-0.5f - c.x) / d.x;
		}
		else
		{
			tx = float.MaxValue;
		}
		float ty;
		if (d.y > 1e-09f)
		{
			ty = (0.5f - c.y) / d.y;
		}
		else if (d.y < -1e-09f)
		{
			ty = (-0.5f - c.y) / d.y;
		}
		else
		{
			ty = float.MaxValue;
		}
		return Mathf.Max(Mathf.Min(tx, ty), 0.0001f);
	}
}
