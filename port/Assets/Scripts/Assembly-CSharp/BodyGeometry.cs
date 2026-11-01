using UnityEngine;

public static class BodyGeometry
{
	public const float Eps = 1E-07f;

	public static bool Slab(float origin, float dir, float half, ref float t0, ref float t1)
	{
		if (Eps > Mathf.Abs(dir))
		{
			return half >= Mathf.Abs(origin);
		}
		float num = (half - origin) / dir;
		float num2 = (-half - origin) / dir;
		if (num < num2)
		{
			float num3 = num;
			num = num2;
			num2 = num3;
		}
		if (num2 > t0)
		{
			t0 = num2;
		}
		if (t1 > num)
		{
			t1 = num;
		}
		return t0 <= t1;
	}

	public static bool Band(float origin, float dir, float lo, float hi, ref float t0, ref float t1)
	{
		if (Eps > Mathf.Abs(dir))
		{
			if (origin < lo)
			{
				return false;
			}
			return hi >= origin;
		}
		float num = (lo - origin) / dir;
		float num2 = (hi - origin) / dir;
		if (num > num2)
		{
			float num3 = num;
			num = num2;
			num2 = num3;
		}
		if (num > t0)
		{
			t0 = num;
		}
		if (t1 > num2)
		{
			t1 = num2;
		}
		return t0 <= t1;
	}

	public static bool Box(in BodyLocalRay ray, Vector3 half, out float t0, out float t1, out int enterAxis)
	{
		t0 = float.NegativeInfinity;
		t1 = float.PositiveInfinity;
		enterAxis = -1;
		for (int axis = 0; axis < 3; axis++)
		{
			float num = ray.OriginMm[axis];
			float num2 = ray.Direction[axis];
			float num3 = half[axis];
			if (num3 == float.PositiveInfinity)
			{
				continue;
			}
			if (Eps > Mathf.Abs(num2))
			{
				if (Mathf.Abs(num) > num3)
				{
					return false;
				}
				continue;
			}
			float num4 = (num3 - num) / num2;
			float num5 = (-num3 - num) / num2;
			if (num4 < num5)
			{
				float num6 = num4;
				num4 = num5;
				num5 = num6;
			}
			if (num5 > t0)
			{
				t0 = num5;
				enterAxis = axis;
			}
			if (t1 > num4)
			{
				t1 = num4;
			}
			if (t0 > t1)
			{
				return false;
			}
		}
		return t0 <= t1 && t0 != float.NegativeInfinity && t1 != float.PositiveInfinity;
	}

	public static float BoxHalfExtentAlong(Vector3 half, Vector3 dir)
	{
		return Mathf.Abs(dir.x) * half.x + Mathf.Abs(dir.y) * half.y + Mathf.Abs(dir.z) * half.z;
	}

	public static bool Revolved(in BodyLocalRay ray, float a, float b, float yLo, float yHi, out float t0, out float t1, out bool unsupported)
	{
		t0 = 0f;
		t1 = 0f;
		unsupported = false;
		float dx = ray.Direction.x;
		float dy = ray.Direction.y;
		float dz = ray.Direction.z;
		float ox = ray.OriginMm.x;
		float oy = ray.OriginMm.y;
		float oz = ray.OriginMm.z;
		float num = dx * dx + dz * dz - b * b * dy * dy;
		if (Eps >= num)
		{
			if (Eps <= Mathf.Abs(b) || Eps <= Mathf.Abs(num))
			{
				unsupported = true;
				return false;
			}
			float num2 = b * oy + a;
			float num3 = Mathf.Sqrt(ox * ox + oz * oz);
			if ((num3 + num2) * (num3 - num2) > 0f)
			{
				return false;
			}
			t0 = float.NegativeInfinity;
			t1 = float.PositiveInfinity;
		}
		else
		{
			float num4 = ox * dx + oz * dz - (b * oy + a) * (b * dy);
			float num5 = -(num4 + num4) / (num + num);
			float num6 = ox + dx * num5;
			float num7 = oz + dz * num5;
			float num8 = oy + dy * num5;
			float num9 = b * num8 + a;
			float num10 = Mathf.Sqrt(num6 * num6 + num7 * num7);
			float num11 = -((num10 - num9) * (num10 + num9)) / num;
			if (num11 < 0f)
			{
				return false;
			}
			float num12 = Mathf.Sqrt(num11);
			t0 = num5 - num12;
			t1 = num5 + num12;
		}
		return Band(oy, dy, yLo, yHi, ref t0, ref t1);
	}

	private static float OutsideAt(Vector3 p, float a, float b)
	{
		float num = Mathf.Sqrt(p.x * p.x + p.z * p.z);
		float num2 = b * p.y + a;
		return (num - num2) * (num + num2);
	}

	public static Vector3 RevolvedNormal(Vector3 p, float a, float b)
	{
		return RevolvedNormal(p, a, b, 1f);
	}

	public static Vector3 RevolvedNormal(Vector3 p, float a, float b, float depthRatio)
	{
		if (Eps > depthRatio)
		{
			depthRatio = 1f;
		}
		Vector3 vector = new Vector3(p.x + p.x, (b * p.y + a) * (-2f * b), (p.z + p.z) / (depthRatio * depthRatio));
		float num = vector.magnitude;
		if (Eps > num)
		{
			return Vector3.zero;
		}
		return vector / num;
	}

	public static BodyLocalRay ToCircularSection(in BodyLocalRay ray, float depthRatio)
	{
		if (Eps > depthRatio || Eps > Mathf.Abs(depthRatio - 1f))
		{
			return ray;
		}
		float num = 1f / depthRatio;
		return new BodyLocalRay(new Vector3(ray.OriginMm.x, ray.OriginMm.y, ray.OriginMm.z * num), new Vector3(ray.Direction.x, ray.Direction.y, ray.Direction.z * num));
	}

	public static float ObliquityDeg(Vector3 dir, Vector3 normal)
	{
		Vector3 lhs = dir.normalized;
		Vector3 rhs = normal.normalized;
		return Mathf.Acos(Mathf.Clamp01(Mathf.Abs(Vector3.Dot(lhs, rhs)))) * Mathf.Rad2Deg;
	}
}
