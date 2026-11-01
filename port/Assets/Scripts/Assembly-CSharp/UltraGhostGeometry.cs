using UnityEngine;

public static class UltraGhostGeometry
{
	public const float MinTiltDeg = 0.0001f;

	public static float DrawTiltDeg(float tiltDeg, float leanSign)
	{
		return Mathf.Abs(tiltDeg) * Mathf.Sign(leanSign);
	}

	public static bool TrySlabRange(ContinuumScenario s, int i, out float z0M, out float z1M)
	{
		z0M = 0f;
		z1M = 0f;
		if (s == null)
		{
			return false;
		}
		if (s.layers == null || s.layers.Length == 0)
		{
			if (i != 0)
			{
				return false;
			}
			z1M = s.targetThicknessM;
			return s.targetThicknessM > 0f;
		}
		if (i < 0 || i >= s.layers.Length)
		{
			return false;
		}
		float num = 0f;
		for (int j = 0; j < i; j++)
		{
			num += s.layers[j].thicknessM + s.layers[j].gapBeforeM;
		}
		z0M = num + s.layers[i].gapBeforeM;
		z1M = z0M + s.layers[i].thicknessM;
		return s.layers[i].thicknessM > 0f;
	}

	public static float AxialExtentM(float rawM, float tiltDeg)
	{
		float num = Mathf.Cos(Mathf.Abs(tiltDeg) * Mathf.Deg2Rad);
		return rawM / Mathf.Max(num, 0.001f);
	}

	public static Vector3 Normal(float tiltDeg)
	{
		float num = tiltDeg * Mathf.Deg2Rad;
		return new Vector3(Mathf.Sin(num), 0f, Mathf.Cos(num));
	}

	public static Vector3 InPlane(float tiltDeg)
	{
		float num = tiltDeg * Mathf.Deg2Rad;
		return new Vector3(Mathf.Cos(num), 0f, -Mathf.Sin(num));
	}

	public static Vector3[] OutlineXZ(float zCentreM, float rawM, float tiltDeg, float radiusM)
	{
		Vector3 n = Normal(tiltDeg);
		Vector3 b = n * (rawM * 0.5f);
		Vector3 u = InPlane(tiltDeg);
		Vector3 a = u * radiusM;
		Vector3 c = new Vector3(0f, 0f, zCentreM);
		return new Vector3[5]
		{
			c + a + b,
			c - a + b,
			c - a - b,
			c + a - b,
			c + a + b
		};
	}
}
