using UnityEngine;

public static class AeroWake
{
	public static Vector3 Velocity(Vector3 position, float time, float strength, Vector3 bodyCenter, Vector3 bodySize)
	{
		if (!Finite(position) || !Finite(bodyCenter) || !Finite(bodySize) || !Finite(time) || !Finite(strength) || strength <= 0f)
		{
			return Vector3.zero;
		}
		float num = Mathf.Clamp(Mathf.Abs(bodySize.x) * 0.5f, 0.12f, 8f);
		float num2 = Mathf.Clamp(Mathf.Abs(bodySize.y) * 0.5f, 0.12f, 8f);
		float num3 = Mathf.Max(num, num2);
		float num4 = Mathf.Clamp(Mathf.Min(num, num2) * 0.36f, 0.25f, 0.8f);
		Vector3 vector = position - bodyCenter;
		if (!Finite(vector))
		{
			return Vector3.zero;
		}
		float num5 = vector.z - Mathf.Abs(bodySize.z) * 0.5f;
		if (num5 <= 0f || num5 > num3 * 8f)
		{
			return Vector3.zero;
		}
		if (Mathf.Abs(vector.x) > num4 * 6f + num || Mathf.Abs(vector.y) > num4 * 6f + num2)
		{
			return Vector3.zero;
		}
		float num6 = (Mathf.Repeat(time, 1.8f) - num5 / 1.3f) * (2f * Mathf.PI / 1.8f);
		float num7 = Mathf.Sin(num6);
		float num8 = Mathf.Cos(num6);
		float num9 = Mathf.SmoothStep(0f, 1f, num5 / (num4 * 0.4f));
		float num10 = Mathf.Exp(-(num5 * num5) / (18f * num3 * num3));
		float num11 = (1f - Mathf.SmoothStep(0f, 1f, (num5 - num3 * 6f) / (num3 * 2f))) * num10;
		Vector3 vector2 = Patch(vector, num * 0.92f, num4 * 0.12f * num7, num4, 0.72f + 0.28f * num7)
			+ Patch(vector, -num * 0.92f, -num4 * 0.12f * num7, num4, 0.28f * num7 - 0.72f)
			+ Patch(vector, num4 * 0.12f * num8, num2 * 0.92f, num4, -0.72f - 0.28f * num8)
			+ Patch(vector, -num4 * 0.12f * num8, -num2 * 0.92f, num4, 0.72f - 0.28f * num8);
		vector2 = Vector3.ClampMagnitude(vector2, 4f);
		vector2 *= Mathf.Clamp(strength, 0f, 2f) * num9 * num11;
		return vector2;
	}

	private static Vector3 Patch(Vector3 p, float x, float y, float core, float circulation)
	{
		float num = (p.y - y) / core;
		float num2 = (p.x - x) / core;
		float num3 = Mathf.Exp(0.5f - 0.5f * (num * num + num2 * num2)) * (circulation * 2.8f);
		return new Vector3(-num * num3, num3 * num2, 0f);
	}

	private static bool Finite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}

	private static bool Finite(Vector3 value)
	{
		return Finite(value.x) && Finite(value.y) && Finite(value.z);
	}
}
