using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class SyntheticTimeline
{
	public const string Stamp = "SYNTHETIC";

	public const int Frames = 90;

	public const int Points = 512;

	public const float RodRadiusM = 0.015f;

	public const float RodLengthM = 0.3f;

	public const float PlateThickM = 0.05f;

	public const float DurationS = 0.0002f;

	public const int DetachedFrame = 60;

	public static Vector2[] ProjectileContour(int frame)
	{
		float num = Mathf.Clamp01((float)frame / (float)(Frames - 1));
		float num2 = Mathf.Max(0f, (num - 0.25f) / 0.75f) * 0.55f;
		float num3 = (1f - num2) * RodLengthM;
		float num4;
		if (0.25f <= num)
		{
			num4 = (num - 0.25f) * 0.09f / 0.75f;
		}
		else
		{
			num4 = (1f - num * 4f) * -0.04f;
		}
		float num5 = Mathf.Clamp01((num - 0.25f) / 0.35f);
		num5 = (num5 * 0.35f + 1f) * RodRadiusM;
		List<Vector2> corners = new List<Vector2>();
		corners.Add(new Vector2(RodRadiusM, num4 - num3));
		corners.Add(new Vector2(RodRadiusM, num4 - num3 * 0.25f));
		corners.Add(new Vector2(num5, num4 - num3 * 0.06f));
		corners.Add(new Vector2(num5 * 0.55f, num4));
		corners.Add(new Vector2(0f, num4));
		corners.Add(new Vector2(0f, num4 - num3));
		return WalkClosed(corners, Points);
	}

	private static Vector2[] WalkClosed(List<Vector2> corners, int n)
	{
		float[] array = new float[corners.Count];
		float num = 0f;
		for (int i = 0; i < corners.Count; i++)
		{
			array[i] = Vector2.Distance(corners[i], corners[(i + 1) % corners.Count]);
			num += array[i];
		}
		Vector2[] array2 = new Vector2[n];
		if (num <= 1E-12f)
		{
			for (int j = 0; j < n; j++)
			{
				array2[j] = corners[0];
			}
			return array2;
		}
		float num2 = 0f;
		int num3 = 0;
		for (int k = 0; k < n; k++)
		{
			float num4 = (float)k * num / (float)n;
			while (num3 < corners.Count - 1 && num4 > num2 + array[num3])
			{
				num2 += array[num3];
				num3++;
			}
			float num5 = array[num3];
			num5 = ((!(num5 > 1E-12f)) ? 0f : ((num4 - num2) / num5));
			Vector2 a = corners[num3];
			Vector2 b = corners[(num3 + 1) % corners.Count];
			array2[k] = Vector2.Lerp(a, b, Mathf.Clamp01(num5));
		}
		return array2;
	}

	public static Vector2[] TargetContour(int frame)
	{
		float num = Mathf.Clamp01((float)frame / (float)(Frames - 1));
		float num2 = Mathf.Clamp01((num - 0.25f) / 0.55f) * PlateThickM;
		float num3 = Mathf.Clamp01((num - 0.25f) / 0.2f) * 0.004f;
		float num4 = Mathf.Clamp01((num - 0.7f) / 0.3f) * 0.006f;
		List<Vector2> corners = new List<Vector2>();
		corners.Add(new Vector2(0.12f, 0f));
		corners.Add(new Vector2(0.0241f, -num3 * 0.4f));
		corners.Add(new Vector2(0.0201f, -num3));
		corners.Add(new Vector2(0.0201f, num2));
		if (PlateThickM <= num2)
		{
			corners.Add(new Vector2(0.0201f, num4 + PlateThickM));
		}
		else
		{
			corners.Add(new Vector2(0f, num2));
			corners.Add(new Vector2(0f, PlateThickM));
		}
		corners.Add(new Vector2(0.12f, num4 + PlateThickM));
		corners.Add(new Vector2(0.12f, PlateThickM));
		return WalkClosed(corners, Points);
	}

	public static float Field(UltraField f, float r, float z, float t)
	{
		float num = Mathf.Exp(-40f * Mathf.Abs(z));
		if (f == UltraField.Temperature)
		{
			return num * 1200f * t + 300f;
		}
		if (f == UltraField.Pressure)
		{
			return ((t + 0.5f) * num - 0.1f) * 1e+09f;
		}
		return r * 0.05f / 0.015f + num * 1.2f * t;
	}

	public static bool DetachedAt(int frame, out UltraDetachedPiece piece)
	{
		piece = default(UltraDetachedPiece);
		if (frame != DetachedFrame)
		{
			return false;
		}
		piece.elementCount = 7;
		piece.massKg = 0.0125f;
		piece.velocityRZ = new Vector2(0f, 240f);
		piece.radiusM = 0.0068f;
		return true;
	}

	public static GeometryTimeline Build()
	{
		GeometryTimeline geometryTimeline = new GeometryTimeline();
		geometryTimeline.dtOutput = DurationS / Frames;
		for (int i = 0; i < Frames; i++)
		{
			GeometryFrame geometryFrame = default(GeometryFrame);
			geometryFrame.time = (float)i * DurationS / Frames;
			geometryFrame.projectileProfile = ToNative(ProjectileContour(i));
			geometryFrame.targetProfile = ToNative(TargetContour(i));
			geometryFrame.erodedMassKg = (1f - (float)i * 0.25f / Frames) * 1100f;
			geometryTimeline.frames.Add(geometryFrame);
		}
		geometryTimeline.Completed = true;
		return geometryTimeline;
	}

	private static NativeArray<Vector2> ToNative(Vector2[] src)
	{
		NativeArray<Vector2> result = new NativeArray<Vector2>(src.Length, Allocator.Persistent);
		for (int i = 0; i < src.Length; i++)
		{
			result[i] = src[i];
		}
		return result;
	}
}
