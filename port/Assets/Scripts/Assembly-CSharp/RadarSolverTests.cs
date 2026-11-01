using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class RadarSolverTests : MonoBehaviour
{
	private sealed class MeshBuilder
	{
		private readonly List<double3> vertices;

		private readonly List<int> triangles;

		public void AddTriangle(double3 a, double3 b, double3 c)
		{
		}

		public void AddRect(double3 p0, double3 edgeA, double3 edgeB)
		{
		}

		public RadarModel Build()
		{
			return null;
		}
	}

	private const double Gigahertz = 1000000000.0;

	private static readonly Dictionary<string, RadarResult> Cached;

	private static readonly Dictionary<string, RadarScene> Scenes;

	[ContextMenu("Run RADAR R1 solver acceptance")]
	public void RunAll()
	{
	}

	public static bool CheckAll()
	{
		return false;
	}

	private static bool TubeFootprintQuadrature()
	{
		return false;
	}

	private static RadarScene Scene(string id, Func<RadarModel> create)
	{
		return null;
	}

	private static RadarResult Solve(string id, RadarScene scene, double hz, double az = 0.0, double el = 0.0, RadarQuality quality = RadarQuality.Normal)
	{
		return null;
	}

	private static bool HasRcs(RadarResult result)
	{
		return false;
	}

	private static double Db(RadarResult result, int receive = 1, int transmit = 1)
	{
		return 0.0;
	}

	private static double OrderSigma(RadarResult result, int group, int receive, int transmit)
	{
		return 0.0;
	}

	private static double OrderDb(RadarResult result, int group, int receive = 1, int transmit = 1)
	{
		return 0.0;
	}

	private static bool DbNear(string label, double got, double expected, double tolerance)
	{
		return false;
	}

	private static bool TriangleIntegral()
	{
		return false;
	}

	private static Complex64 DuffyGauss64(double3 a, double3 b, double3 c, double3 w)
	{
		return default;
	}

	private static bool PlateAngles()
	{
		return false;
	}

	private static bool Cylinder()
	{
		return false;
	}

	private static bool Sphere()
	{
		return false;
	}

	private static bool Dihedral()
	{
		return false;
	}

	private static bool PolarizationRotation()
	{
		return false;
	}

	private static bool Trihedrals()
	{
		return false;
	}

	private static bool NoDoubleCount()
	{
		return false;
	}

	private static bool PartialShadow()
	{
		return false;
	}

	private static bool Convergence()
	{
		return false;
	}

	private static bool RigidRotation()
	{
		return false;
	}

	private static bool Reciprocity()
	{
		return false;
	}

	private static bool FrequencyScaling()
	{
		return false;
	}

	private static bool Determinism()
	{
		return false;
	}

	private static bool SameBits(double a, double b)
	{
		return false;
	}

	private static bool RayGuards()
	{
		return false;
	}

	private static bool RealDefaultPreview()
	{
		return false;
	}

	private static bool RayRegimeRefusal()
	{
		return false;
	}

	private static bool Periscope()
	{
		return false;
	}

	private static Complex64 HigherOrderS(RadarResult result, int receive, int transmit)
	{
		return default;
	}

	private static double HigherOrderDb(RadarResult result, int receive, int transmit)
	{
		return 0.0;
	}

	private static bool HybridPhase()
	{
		return false;
	}

	private static RadarModel Plate(double width, double height, int nx, int ny)
	{
		return null;
	}

	private static RadarModel SingleTriangle()
	{
		return null;
	}

	private static RadarModel CylinderModel(double radius, double length, int sides, int divisions)
	{
		return null;
	}

	private static RadarModel DihedralModel(double alongFold, double outward)
	{
		return null;
	}

	private static RadarModel TrihedralSquare(double edge)
	{
		return null;
	}

	private static RadarModel TrihedralTriangle(double edge)
	{
		return null;
	}

	private static RadarModel PeriscopeModel(bool blocker)
	{
		return null;
	}

	private static void AddCenteredRect(MeshBuilder mesh, double3 center, double width, double height, double3 vertical)
	{
	}

	private static RadarModel ShadowPlate(double screenRightEdge, bool precut)
	{
		return null;
	}

	private static RadarModel RotateY(RadarModel original, double degrees)
	{
		return null;
	}

	private static RadarModel RotateZ(RadarModel original, double degrees)
	{
		return null;
	}

	private static RadarModel Icosphere(double radius, int level)
	{
		return null;
	}

	private static int Midpoint(int a, int b, double radius, List<double3> vertices, Dictionary<ulong, int> cache)
	{
		return 0;
	}

	private static void AddIndices(List<int> indices, int a, int b, int c)
	{
	}
}
