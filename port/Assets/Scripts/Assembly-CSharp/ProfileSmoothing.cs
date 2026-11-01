using System;
using Unity.Collections;
using UnityEngine;

public static class ProfileSmoothing
{
	public const int DefaultSubdivision = 3;

	public static NativeArray<Vector2> Smooth(NativeArray<Vector2> src, int subdivision, Allocator alloc)
	{
		int num = src.IsCreated ? src.Length : 0;
		if (num < 4 || subdivision < 2)
		{
			NativeArray<Vector2> result = new NativeArray<Vector2>(Math.Max(num, 0), alloc, NativeArrayOptions.ClearMemory);
			for (int i = 0; i < num; i++)
			{
				result[i] = src[i];
			}
			return result;
		}
		NativeArray<Vector2> result2 = new NativeArray<Vector2>(num * subdivision, alloc, NativeArrayOptions.ClearMemory);
		for (int j = 0; j < num; j++)
		{
			Vector2 prev = src[(j + num - 1) % num];
			Vector2 cur = src[j];
			Vector2 next = src[(j + 1) % num];
			Vector2 next2 = src[(j + 2) % num];
			Vector2 m = Monotone(prev, cur, next);
			Vector2 m2 = Monotone(cur, next, next2);
			for (int k = 0; k < subdivision; k++)
			{
				result2[j * subdivision + k] = Hermite(cur, next, m, m2, (float)k / (float)subdivision);
			}
		}
		return result2;
	}

	private static Vector2 Monotone(Vector2 prev, Vector2 cur, Vector2 next)
	{
		return new Vector2(Component(prev.x, cur.x, next.x), Component(prev.y, cur.y, next.y));
	}

	private static float Component(float a, float b, float c)
	{
		float num = c - b;
		float num2 = b - a;
		if (num * num2 <= 0f)
		{
			return 0f;
		}
		float num3 = (num + num2) * 0.5f;
		float num4 = 3f * Mathf.Min(Mathf.Abs(num2), Mathf.Abs(num));
		return Mathf.Clamp(num3, -num4, num4);
	}

	private static Vector2 Hermite(Vector2 p1, Vector2 p2, Vector2 m1, Vector2 m2, float t)
	{
		float num = t * t;
		float num2 = num * t;
		float num3 = 2f * num2 - 3f * num + 1f;
		float num4 = num2 - 2f * num + t;
		float num5 = num2 * -2f + 3f * num;
		float num6 = num2 - num;
		return new Vector2(num3 * p1.x + num4 * m1.x + num5 * p2.x + num6 * m2.x, num3 * p1.y + num4 * m1.y + num5 * p2.y + num6 * m2.y);
	}
}
