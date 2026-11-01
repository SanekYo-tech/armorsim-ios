using Unity.Collections;
using UnityEngine;

public class ViewerTests : MonoBehaviour
{
	private struct Lathed
	{
		public LatheTopology topo;

		public NativeArray<Vector3> verts;

		public NativeArray<Vector3> normals;

		public void Dispose()
		{
		}
	}

	[ContextMenu("★ Прогнать проверки показа V1–V7")]
	public void RunAll()
	{
	}

	private static NativeArray<Vector2> CylinderLoop(float R, float z0, float z1, int perEdge)
	{
		return default;
	}

	private static NativeArray<Vector2> SphereLoop(float R, int arcPts, int axisPts)
	{
		return default;
	}

	private static Lathed Lathe(NativeArray<Vector2> loop, int nProf, int nSeg)
	{
		return default;
	}

	public static bool V1_AxisAndScale()
	{
		return false;
	}

	public static bool V2_AxisDegeneracy()
	{
		return false;
	}

	public static bool V3_Winding()
	{
		return false;
	}

	public static bool V4_NoOutrun()
	{
		return false;
	}

	private static float MaxTip(GeometryTimeline tl)
	{
		return 0f;
	}

	public static bool V5_TimeBase()
	{
		return false;
	}

	public static bool V6_Ownership()
	{
		return false;
	}

	public static bool V7_MeshAllocation()
	{
		return false;
	}
}
