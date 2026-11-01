using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

public static class AeroSolver
{
	public const int DefaultResolution = 256;

	public const int MaxResolution = 1024;

	public static AeroResult Calculate(AeroMeshData data, Vector3 flow, int resolution = 256, AeroRayGrid? grid = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		cancellationToken.ThrowIfCancellationRequested();
		Vector3 vector = UnitFlow(flow);
		ValidateResolution(resolution);
		AeroRaycaster raycaster = new AeroRaycaster(data);
		AeroRayGrid grid2 = grid ?? CreateGrid(data, flow, resolution);
		ValidateGrid(grid2, vector);
		AeroResult aeroResult = new AeroResult();
		aeroResult.Cp = new float[data.TriangleCount];
		aeroResult.VisibleTriangles = new bool[data.TriangleCount];
		Vector3 vector2 = Vector3.Cross(grid2.StepU, grid2.StepV);
		double cellArea = vector2.magnitude;
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		for (int i = 0; i < grid2.Resolution; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			for (int j = 0; j < grid2.Resolution; j++)
			{
				Vector3 origin = grid2.Origin + (float)i * grid2.StepV + (float)j * grid2.StepU;
				if (raycaster.FirstHit(origin, vector, grid2.MaxDistance, out AeroRayHit hit))
				{
					aeroResult.HitCount++;
					aeroResult.VisibleTriangles[hit.TriangleIndex] = true;
					double num4 = (double)(0f - Vector3.Dot(vector, hit.Normal));
					if (num4 <= 0.0)
					{
						aeroResult.InvertedHitCount++;
					}
					else
					{
						aeroResult.Cp[hit.TriangleIndex] = (float)((num4 + num4) * num4);
						double num5 = (num4 + num4) * cellArea;
						num += (double)(0f - hit.Normal.x) * num5;
						num2 += (double)(0f - hit.Normal.y) * num5;
						num3 += (double)(0f - hit.Normal.z) * num5;
					}
				}
			}
		}
		double num6 = (double)aeroResult.HitCount * cellArea;
		aeroResult.MidshipArea = num6;
		if (aeroResult.HitCount == 0 || num6 <= 0.0)
		{
			throw new AeroException("aero.error.midship", Array.Empty<object>());
		}
		if (aeroResult.HitCount < aeroResult.InvertedHitCount * 5)
		{
			throw new AeroException("aero.error.normals", new object[2]
			{
				aeroResult.InvertedHitCount,
				aeroResult.HitCount
			});
		}
		double num7 = (double)vector.y * num2 + (double)vector.x * num + (double)vector.z * num3;
		aeroResult.Drag = num7;
		if (num7 < 0.0)
		{
			throw new AeroException("aero.error.drag", Array.Empty<object>());
		}
		double num8 = num - (double)vector.x * num7;
		double num9 = num2 - (double)vector.y * num7;
		double num10 = num3 - (double)vector.z * num7;
		if (num7 <= 0.0)
		{
			num7 = 0.0;
		}
		else
		{
			num7 = Math.Sqrt(num8 * num8 + num9 * num9 + num10 * num10) / aeroResult.Drag;
		}
		aeroResult.LateralRatio = num7;
		aeroResult.Force = new Vector3((float)num, (float)num2, (float)num3);
		if (!Finite(aeroResult.Drag) || !Finite(aeroResult.MidshipArea) || !Finite(aeroResult.LateralRatio) || !float.IsFinite(aeroResult.Force.x) || !float.IsFinite(aeroResult.Force.y) || !float.IsFinite(aeroResult.Force.z))
		{
			throw new AeroException("aero.error.calculation", Array.Empty<object>());
		}
		int num11 = 0;
		int num12 = 0;
		double num13 = 0.0;
		for (int k = 0; k < data.TriangleCount; k++)
		{
			if (Vector3.Dot(data.Vertices[k], vector) < 0f)
			{
				num11++;
				if (!aeroResult.VisibleTriangles[k])
				{
					num12++;
				}
			}
		}
		if (num11 > 0)
		{
			num13 = (double)num12 / (double)num11;
		}
		aeroResult.ShadowedFraction = num13;
		stopwatch.Stop();
		aeroResult.Milliseconds = stopwatch.Elapsed.TotalMilliseconds;
		return aeroResult;
	}

	public static AeroComparison CompareToSphere(AeroMeshData data, Vector3 flow, int resolution = 256, CancellationToken cancellationToken = default(CancellationToken))
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		AeroResult aeroResult = Calculate(data, flow, resolution, null, cancellationToken);
		AeroMeshData sphere = AeroPrimitives.Sphere();
		AeroResult aeroResult2 = Calculate(sphere, flow, resolution, null, cancellationToken);
		double num = Math.Sqrt(aeroResult.MidshipArea / aeroResult2.MidshipArea);
		if (!Finite(num) || num <= 0.0 || (double)float.MaxValue < num)
		{
			throw new AeroException("aero.error.calculation", Array.Empty<object>());
		}
		Vector3[] array = new Vector3[sphere.Vertices.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = sphere.Vertices[i] * (float)num;
		}
		AeroMeshData mesh = new AeroMeshData(array, sphere.Triangles, sphere.Normals);
		AeroResult aeroResult3 = Calculate(mesh, flow, resolution, null, cancellationToken);
		if (aeroResult3.Drag <= 0.0)
		{
			throw new AeroException("aero.error.calculation", Array.Empty<object>());
		}
		double num2 = aeroResult.Drag / aeroResult3.Drag;
		if (!Finite(num2) || num2 < 0.0)
		{
			throw new AeroException("aero.error.calculation", Array.Empty<object>());
		}
		stopwatch.Stop();
		AeroComparison aeroComparison = new AeroComparison();
		aeroComparison.Model = aeroResult;
		aeroComparison.Reference = aeroResult3;
		aeroComparison.Ratio = num2;
		aeroComparison.Milliseconds = stopwatch.Elapsed.TotalMilliseconds;
		return aeroComparison;
	}

	public static AeroRayGrid CreateGrid(AeroMeshData data, Vector3 flow, int resolution = 256)
	{
		ValidateResolution(resolution);
		if (data == null || data.TriangleCount == 0)
		{
			throw new AeroException("aero.error.midship", Array.Empty<object>());
		}
		Vector3 vector = UnitFlow(flow);
		bool flag = false;
		Vector3 vector2 = Vector3.zero;
		int num = 0;
		while (num < data.TriangleCount && !flag)
		{
			for (int i = 0; i < 3; i++)
			{
				Vector3 a = data.Vertices[data.Triangles[num * 3 + i]];
				Vector3 b = data.Vertices[data.Triangles[num * 3 + (i + 1) % 3]];
				double num2 = (double)b.x - (double)a.x;
				double num3 = (double)b.y - (double)a.y;
				double num4 = (double)b.z - (double)a.z;
				double num5 = (double)vector.x * num2 + (double)vector.y * num3 + (double)vector.z * num4;
				double num6 = num2 - (double)vector.x * num5;
				double num7 = num3 - (double)vector.y * num5;
				double num8 = num4 - (double)vector.z * num5;
				double num9 = num6 * num6 + num7 * num7 + num8 * num8;
				if ((num2 * num2 + num3 * num3 + num4 * num4) * 1E-08 < num9)
				{
					flag = true;
					double num10 = 1.0 / Math.Sqrt(num9);
					vector2 = new Vector3((float)(num10 * num6), (float)(num10 * num7), (float)(num10 * num8));
					break;
				}
			}
			num++;
		}
		if (!flag)
		{
			throw new AeroException("aero.error.midship", Array.Empty<object>());
		}
		Vector3 vector3 = Vector3.Cross(vector, vector2);
		float magnitude = vector3.magnitude;
		vector3 = ((magnitude <= 1E-05f) ? Vector3.zero : (vector3 / magnitude));
		Vector3 vector4 = Vector3.Cross(vector3, vector);
		float magnitude2 = vector4.magnitude;
		vector4 = ((magnitude2 <= 1E-05f) ? Vector3.zero : (vector4 / magnitude2));
		Vector3 vector5 = data.Vertices[data.Triangles[0]];
		double num11 = double.PositiveInfinity;
		double num12 = double.NegativeInfinity;
		double num13 = double.PositiveInfinity;
		double num14 = double.NegativeInfinity;
		double num15 = double.PositiveInfinity;
		double num16 = double.NegativeInfinity;
		double num17 = 0.0;
		for (int j = 0; j < data.Triangles.Length; j++)
		{
			Vector3 vertex = data.Vertices[data.Triangles[j]];
			double num18 = (double)vertex.x - (double)vector5.x;
			double num19 = (double)vertex.y - (double)vector5.y;
			double num20 = (double)vertex.z - (double)vector5.z;
			double num21 = (double)vector4.x * num18 + (double)vector4.y * num19 + (double)vector4.z * num20;
			double num22 = (double)vector3.x * num18 + (double)vector3.y * num19 + (double)vector3.z * num20;
			double num23 = (double)vector.x * num18 + (double)vector.y * num19 + (double)vector.z * num20;
			num11 = Math.Min(num11, num21);
			num12 = Math.Max(num12, num21);
			num13 = Math.Min(num13, num22);
			num14 = Math.Max(num14, num22);
			num15 = Math.Min(num15, num23);
			num16 = Math.Max(num16, num23);
			num17 = Math.Max(num17, num18 * num18 + num19 * num19 + num20 * num20);
		}
		double num24 = Math.Sqrt(num17);
		if (!Finite(num24) || !(0.0 < num24))
		{
			throw new AeroException("aero.error.midship", Array.Empty<object>());
		}
		double num25 = Math.Max((num12 - num11) * 0.002, num24 * 1E-06);
		double num26 = Math.Max((num14 - num13) * 0.002, num24 * 1E-06);
		double num27 = num24 * 0.002;
		float num28 = (float)((num25 + num25 + (num12 - num11)) / (double)resolution);
		float num29 = (float)((num26 + num26 + (num14 - num13)) / (double)resolution);
		AeroRayGrid result = default(AeroRayGrid);
		result.Origin = vector5 + vector4 * (float)(num11 - num25) + vector3 * (float)(num13 - num26) + vector * (float)(num15 - num27) + (vector4 * num28 + vector3 * num29) * 0.5f;
		result.StepU = vector4 * num28;
		result.StepV = vector3 * num29;
		result.Direction = vector;
		result.Resolution = resolution;
		result.MaxDistance = (float)(num27 + num27 + (num16 - num15));
		return result;
	}

	private static Vector3 UnitFlow(Vector3 flow)
	{
		if (!float.IsFinite(flow.x) || !float.IsFinite(flow.y) || !float.IsFinite(flow.z) || flow.sqrMagnitude < 1E-20f)
		{
			throw new AeroException("aero.error.calculation", Array.Empty<object>());
		}
		float magnitude = flow.magnitude;
		Vector3 vector = ((magnitude <= 1E-05f) ? Vector3.zero : (flow / magnitude));
		if (!float.IsFinite(vector.x) || !float.IsFinite(vector.y) || !float.IsFinite(vector.z) || vector.sqrMagnitude < 0.99f)
		{
			throw new AeroException("aero.error.calculation", Array.Empty<object>());
		}
		return vector;
	}

	private static void ValidateResolution(int resolution)
	{
		if (resolution < 16 || resolution > 1024)
		{
			throw new AeroException("aero.error.resolution", new object[3]
			{
				resolution,
				16,
				1024
			});
		}
	}

	private static void ValidateGrid(AeroRayGrid grid, Vector3 direction)
	{
		ValidateResolution(grid.Resolution);
		double cellArea = grid.CellArea;
		if (AeroRaycaster.Finite(grid.Origin) && AeroRaycaster.Finite(grid.StepU) && AeroRaycaster.Finite(grid.StepV) && AeroRaycaster.Finite(grid.Direction) && 0f < grid.MaxDistance && Finite(cellArea) && 0.0 < cellArea)
		{
			Vector3 vector = UnitFlow(grid.Direction);
			if (0.999f <= Vector3.Dot(direction, vector) && Mathf.Abs(Vector3.Dot(direction, grid.StepU.normalized)) <= 0.0001f && Mathf.Abs(Vector3.Dot(direction, grid.StepV.normalized)) <= 0.0001f)
			{
				return;
			}
		}
		throw new AeroException("aero.error.calculation", Array.Empty<object>());
	}

	private static bool Finite(double value)
	{
		return !double.IsNaN(value) && !double.IsInfinity(value);
	}
}
