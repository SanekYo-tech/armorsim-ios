using System;
using Unity.Mathematics;
using UnityEngine;

public static class RadarModelImport
{
	public static RadarModel FromAero(AeroMeshData data, RadarLengthUnit unit)
	{
		return FromAero(data, unit, 0.0, RadarOrientation.Identity);
	}

	public static RadarModel FromAero(AeroMeshData data, RadarLengthUnit unit, RadarOrientation orientation)
	{
		return FromAero(data, unit, 0.0, orientation);
	}

	public static RadarModel FromAero(AeroMeshData data, RadarLengthUnit unit, double customLongestSideMeters)
	{
		return FromAero(data, unit, customLongestSideMeters, RadarOrientation.Identity);
	}

	public static RadarModel FromAero(AeroMeshData data, RadarLengthUnit unit, double customLongestSideMeters, RadarOrientation orientation)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		double num = MeasuredNormalizedLongestSide(data);
		double num2;
		if (unit == RadarLengthUnit.CustomLength)
		{
			num2 = customLongestSideMeters;
		}
		else
		{
			float num3 = Math.Max(data.OriginalSize.x, Math.Max(data.OriginalSize.y, data.OriginalSize.z));
			if (!Finite(num3) || num3 <= 0f)
			{
				throw new AeroException("aero.error.dimensions");
			}
			num2 = MetersPerFileUnit(unit) * num3;
		}
		if (!Finite(num2) || num2 <= 0.0)
		{
			throw new AeroException("aero.error.dimensions");
		}
		double num4 = num2 / num;
		if (!Finite(num4) || num4 <= 0.0)
		{
			throw new AeroException("aero.error.dimensions");
		}
		double3[] array = new double3[data.Vertices.Length];
		for (int i = 0; i < array.Length; i++)
		{
			Vector3 vector = data.Vertices[i];
			array[i] = new double3(vector.x * num4, vector.y * num4, vector.z * num4);
		}
		return RadarModel.Build(array, data.Triangles, orientation);
	}

	public static double MeasuredNormalizedLongestSide(AeroMeshData data)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (data.Triangles.Length == 0)
		{
			throw new AeroException("aero.error.empty");
		}
		Vector3 vector = data.Vertices[data.Triangles[0]];
		float num = vector.x;
		float num2 = vector.y;
		float num3 = vector.z;
		float num4 = vector.x;
		float num5 = vector.y;
		float num6 = vector.z;
		for (int i = 0; i < data.Triangles.Length; i++)
		{
			Vector3 vector2 = data.Vertices[data.Triangles[i]];
			num = Math.Min(num, vector2.x);
			num2 = Math.Min(num2, vector2.y);
			num3 = Math.Min(num3, vector2.z);
			num4 = Math.Max(num4, vector2.x);
			num5 = Math.Max(num5, vector2.y);
			num6 = Math.Max(num6, vector2.z);
		}
		double num7 = Math.Max((double)num4 - (double)num, Math.Max((double)num5 - (double)num2, (double)num6 - (double)num3));
		if (!Finite(num7) || num7 <= 0.0)
		{
			throw new AeroException("aero.error.dimensions");
		}
		return num7;
	}

	public static double MetersPerFileUnit(RadarLengthUnit unit)
	{
		switch (unit)
		{
		case RadarLengthUnit.Meter:
			return 1.0;
		case RadarLengthUnit.Centimeter:
			return 0.01;
		case RadarLengthUnit.Millimeter:
			return 0.001;
		case RadarLengthUnit.Inch:
			return 0.0254;
		default:
			throw new ArgumentOutOfRangeException("unit", unit, "Select a file unit or custom length.");
		}
	}

	private static bool Finite(double value)
	{
		return !double.IsNaN(value) && !double.IsInfinity(value);
	}
}
