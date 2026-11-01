using System;
using UnityEngine;
using Unity.Mathematics;

public static class RadarDirection
{
	public const double DegreesToRadians = Math.PI / 180.0;

	public static RadarDirectionBasis BasisDegrees(double azimuthDeg, double elevationDeg)
	{
		RequireFinite(azimuthDeg, "azimuthDeg");
		RequireFinite(elevationDeg, "elevationDeg");
		double azimuthRad = azimuthDeg * DegreesToRadians;
		double elevationRad = elevationDeg * DegreesToRadians;
		return BasisRadians(azimuthRad, elevationRad);
	}

	public static RadarDirectionBasis BasisRadians(double azimuthRad, double elevationRad)
	{
		RequireFinite(azimuthRad, "azimuthRad");
		RequireFinite(elevationRad, "elevationRad");
		double sinAz = Math.Sin(azimuthRad);
		double cosAz = Math.Cos(azimuthRad);
		double cosEl = Math.Cos(elevationRad);
		double sinEl = Math.Sin(elevationRad);
		double3 toRadar = new double3(cosEl * sinAz, sinEl, cosEl * cosAz);
		double3 horizontal = new double3(cosAz, 0.0, -sinAz);
		return new RadarDirectionBasis(toRadar, horizontal, math.cross(toRadar, horizontal));
	}

	public static double WrapAzimuth(double azimuthDeg)
	{
		RequireFinite(azimuthDeg, "azimuthDeg");
		double value = Math.IEEERemainder(azimuthDeg, 360.0);
		if (value <= -180.0)
		{
			value += 360.0;
		}
		if (value > 180.0)
		{
			value -= 360.0;
		}
		return value == 0.0 ? 0.0 : value;
	}

	public static double ClampElevation(double elevationDeg)
	{
		RequireFinite(elevationDeg, "elevationDeg");
		return Math.Max(-90.0, Math.Min(90.0, elevationDeg));
	}

	public static Quaternion UnityQuaternion(double azimuthDeg, double elevationDeg)
	{
		RequireFinite(azimuthDeg, "azimuthDeg");
		RequireFinite(elevationDeg, "elevationDeg");
		return Quaternion.Euler(new Vector3(-(float)elevationDeg, (float)azimuthDeg, 0f));
	}

	private static void RequireFinite(double value, string parameter)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
		{
			throw new ArgumentOutOfRangeException(parameter, value, "radar.error.direction");
		}
	}
}
