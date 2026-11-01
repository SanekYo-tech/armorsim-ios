using System.Collections.Generic;
using UnityEngine;

public static class RenderedSpeedRule
{
	public const float Tolerance = 1.05f;

	public static float MeasureSpeed(Vector3 prev, Vector3 cur, float dt)
	{
		if (dt <= 1e-9f)
		{
			return 0f;
		}
		return (cur - prev).magnitude / dt;
	}

	public static float Ratio(float measuredSpeed, float birthRodSpeed)
	{
		if (birthRodSpeed <= 1e-9f)
		{
			return 0f;
		}
		return measuredSpeed / birthRodSpeed;
	}

	public static bool Violates(float measuredSpeed, float birthRodSpeed)
	{
		if (birthRodSpeed <= 1e-9f)
		{
			return false;
		}
		return measuredSpeed / birthRodSpeed > Tolerance;
	}

	public static float GrowReference(float reference, float rodSpeedNow)
	{
		if (rodSpeedNow > reference)
		{
			return rodSpeedNow;
		}
		return reference;
	}

	public static float BirthReference(IList<float> recentRodSpeeds)
	{
		float num = 0f;
		if (recentRodSpeeds == null)
		{
			return 0f;
		}
		for (int i = 0; i < recentRodSpeeds.Count; i++)
		{
			float num2 = recentRodSpeeds[i];
			if (num < num2)
			{
				num = num2;
			}
		}
		return num;
	}
}
