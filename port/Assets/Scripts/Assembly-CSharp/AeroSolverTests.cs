using System;
using UnityEngine;

public class AeroSolverTests : MonoBehaviour
{
	[ContextMenu("Run Aero CPU acceptance")]
	public void RunAll()
	{
	}

	public static bool CheckAll()
	{
		return false;
	}

	private static bool PlateSphere()
	{
		return false;
	}

	private static bool SymmetricBody()
	{
		return false;
	}

	private static bool ReverseFlow()
	{
		return false;
	}

	private static bool CoordinateRotation()
	{
		return false;
	}

	private static bool GridConvergence()
	{
		return false;
	}

	private static bool OccludedPlates()
	{
		return false;
	}

	private static bool GrazingPlate()
	{
		return false;
	}

	private static bool InvertedNormals()
	{
		return false;
	}

	public static bool CheckNegativeForceMutation()
	{
		return false;
	}

	private static bool TriangleLimit()
	{
		return false;
	}

	private static bool BinarySolid()
	{
		return false;
	}

	private static bool NonfiniteCoordinates()
	{
		return false;
	}

	private static bool MissedGrid()
	{
		return false;
	}

	private static bool ObjFan()
	{
		return false;
	}

	private static bool TextFormats()
	{
		return false;
	}

	private static bool InvalidFiles()
	{
		return false;
	}

	private static AeroMeshData Combine(AeroMeshData a, AeroMeshData b)
	{
		return null;
	}

	private static byte[] BinaryStl(int triangles, bool solidHeader)
	{
		return null;
	}

	private static bool ExpectError(string key, Action body)
	{
		return false;
	}

	private static bool ExpectError(string key, Action body, out AeroException error)
	{
		error = null;
		return false;
	}

	private static void Log(string label, AeroResult result)
	{
	}

	private static Bounds BoundsOf(AeroMeshData model)
	{
		return default;
	}

	private static bool Finite(double value)
	{
		return false;
	}
}
