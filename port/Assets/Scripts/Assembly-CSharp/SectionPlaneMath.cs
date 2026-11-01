using UnityEngine;

public static class SectionPlaneMath
{
	public const float DegenerateSin = 0.02f;

	public static void AxisBasis(Vector3 axis, out Vector3 right, out Vector3 up)
	{
		Vector3 n = (axis.sqrMagnitude > 1e-12f) ? axis.normalized : Vector3.right;
		Vector3 reference = (Mathf.Abs(Vector3.Dot(n, Vector3.forward)) > 0.99f) ? Vector3.right : Vector3.forward;
		right = Vector3.Cross(reference, n).normalized;
		up = Vector3.Cross(n, right).normalized;
	}

	public static Vector3 NormalFromAzimuth(Vector3 axis, float azimuthDeg)
	{
		AxisBasis(axis, out Vector3 right, out Vector3 up);
		float num = azimuthDeg * Mathf.Deg2Rad;
		return (up * Mathf.Sin(num) + right * Mathf.Cos(num)).normalized;
	}

	public static bool AzimuthAwayFromCamera(Vector3 axis, Vector3 axisPoint, Vector3 cameraPos, out float azimuthDeg)
	{
		azimuthDeg = 0f;
		Vector3 n = (axis.sqrMagnitude > 1e-12f) ? axis.normalized : Vector3.right;
		Vector3 toCam = cameraPos - axisPoint;
		float dot = Vector3.Dot(n, toCam);
		Vector3 proj = toCam - n * dot;
		float projMag = proj.magnitude;
		float dist = Mathf.Max(toCam.magnitude, 1e-06f);
		if (projMag < DegenerateSin * dist)
		{
			return false;
		}
		Vector3 away = -proj / projMag;
		AxisBasis(n, out Vector3 right, out Vector3 up);
		azimuthDeg = Mathf.Atan2(Vector3.Dot(up, away), Vector3.Dot(right, away)) * Mathf.Rad2Deg;
		return true;
	}

	public static Vector4 Plane(Vector3 axis, Vector3 axisPoint, float azimuthDeg, float offsetM)
	{
		Vector3 normal = NormalFromAzimuth(axis, azimuthDeg);
		return new Vector4(normal.x, normal.y, normal.z, normal.x * axisPoint.x + normal.y * axisPoint.y + normal.z * axisPoint.z + offsetM);
	}

	public static bool Keeps(Vector4 plane, Vector3 p)
	{
		return plane.x * p.x + plane.y * p.y + plane.z * p.z - plane.w >= 0f;
	}

	public static float SignedDistanceM(Vector4 plane, Vector3 p)
	{
		return plane.x * p.x + plane.y * p.y + plane.z * p.z - plane.w;
	}
}
