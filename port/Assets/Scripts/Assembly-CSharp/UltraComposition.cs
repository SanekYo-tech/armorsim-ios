using System;
using Unity.Collections;
using UnityEngine;

public static class UltraComposition
{
	public struct FieldRange
	{
		public float min;

		public float max;

		public bool valid;

		public float Normalize(float v)
		{
			if (!valid)
			{
				return 0f;
			}
			if (1e-12f > max - min)
			{
				return 0f;
			}
			float num = (v - min) / (max - min);
			if (num < 0f)
			{
				num = 0f;
			}
			else if (num > 1f)
			{
				num = 1f;
			}
			return num;
		}
	}

	public const int ResampledPoints = 512;

	public const float ClosingSegmentMaxFraction = 0.1f;

	public static Vector2[] Resample(Vector2[] src, int n)
	{
		return ResampleCore(i => src[i], (src != null) ? src.Length : 0, new Vector2[n], n);
	}

	public static Vector2[] Resample(NativeArray<Vector2> src, int n)
	{
		return ResampleCore(i => src[i], src.Length, new Vector2[n], n);
	}

	private static Vector2[] ResampleCore(Func<int, Vector2> src, int m, Vector2[] dst, int n)
	{
		if (m == 1)
		{
			for (int i = 0; i < n; i++)
			{
				dst[i] = src(0);
			}
			return dst;
		}
		float[] array = new float[m + 1];
		for (int j = 0; j < m; j++)
		{
			Vector2 a = src(j);
			Vector2 b = src((j + 1) % m);
			float dx = a.x - b.x;
			float dy = a.y - b.y;
			array[j + 1] = array[j] + (float)Math.Sqrt(dy * dy + dx * dx);
		}
		float total = array[m];
		if (1e-12f >= total)
		{
			for (int k = 0; k < n; k++)
			{
				dst[k] = src(0);
			}
			return dst;
		}
		int idx = 0;
		for (int l = 0; l < n; l++)
		{
			float target = (float)l * total / n;
			while (idx < m - 1 && target > array[idx + 1])
			{
				idx++;
			}
			float seg = array[idx + 1] - array[idx];
			float u = 0f;
			if (seg > 1e-12f)
			{
				u = (target - array[idx]) / seg;
			}
			dst[l] = Vector2.Lerp(src(idx), src((idx + 1) % m), u);
		}
		return dst;
	}

	public static Vector2[] BuildDisplayContour(NativeArray<Vector2> raw, int n, int subdivision)
	{
		if (raw.Length == 0)
		{
			return new Vector2[n];
		}
		Vector2[] result;
		NativeArray<Vector2> smoothed = ProfileSmoothing.Smooth(raw, subdivision, Allocator.Persistent);
		try
		{
			result = ResampleCore(i => smoothed[i], smoothed.Length, new Vector2[n], n);
		}
		finally
		{
			smoothed.Dispose();
		}
		return result;
	}

	public static float MaxDeviation(Vector2[] display, NativeArray<Vector2> raw, out int atIndex)
	{
		atIndex = -1;
		if (display == null || display.Length == 0 || raw.Length == 0)
		{
			return 0f;
		}
		float best = 0f;
		for (int i = 0; i < display.Length; i++)
		{
			float minSqr = float.MaxValue;
			for (int j = 0; j < raw.Length; j++)
			{
				float d = PointSegmentSqr(display[i], raw[j], raw[(j + 1) % raw.Length]);
				if (minSqr > d)
				{
					minSqr = d;
				}
			}
			float dist = Mathf.Sqrt(minSqr);
			if (dist > best)
			{
				best = dist;
				atIndex = i;
			}
		}
		return best;
	}

	private static float PointSegmentSqr(Vector2 p, Vector2 a, Vector2 b)
	{
		float dx = b.x - a.x;
		float dy = b.y - a.y;
		float lenSqr = dy * dy + dx * dx;
		float t = 0f;
		if (lenSqr > 1e-20f)
		{
			t = (p.y - a.y) * dy + (p.x - a.x) * dx;
			t /= lenSqr;
		}
		if (0f > t)
		{
			t = 0f;
		}
		else if (t > 1f)
		{
			t = 1f;
		}
		float cx = t * dx + a.x;
		float cy = t * dy + a.y;
		float ex = cx - p.x;
		float ey = cy - p.y;
		return ex * ex + ey * ey;
	}

	public static float MaxZ(Vector2[] c)
	{
		float max = -float.MaxValue;
		if (c == null)
		{
			throw new NullReferenceException();
		}
		for (int i = 0; i < c.Length; i++)
		{
			if (c[i].y > max)
			{
				max = c[i].y;
			}
		}
		return max;
	}

	public static float MaxZ(NativeArray<Vector2> c)
	{
		float max = -float.MaxValue;
		for (int i = 0; i < c.Length; i++)
		{
			if (c[i].y > max)
			{
				max = c[i].y;
			}
		}
		return max;
	}

	public static string Validate(Vector2[] contour, int expectedPoints, float hM)
	{
		if (contour == null)
		{
			return "контура нет (null)";
		}
		if (contour.Length != expectedPoints)
		{
			return string.Format("точек {0}, ожидалось {1} — топология строится ", contour.Length, expectedPoints) + "ОДИН раз на постоянное N; частичный меш рисовать нельзя";
		}
		for (int i = 0; i < contour.Length; i++)
		{
			Vector2 p = contour[i];
			if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.x) || float.IsInfinity(p.y))
			{
				return string.Format("точка {0} не является числом (NaN/Inf)", i);
			}
			if (-1e-06f > p.x)
			{
				return string.Format("точка {0} имеет отрицательный радиус {1:G6} — в ", i, p.x) + "осесимметричной постановке это невыразимо";
			}
		}
		float perimeter = 0f;
		for (int j = 0; j < contour.Length; j++)
		{
			Vector2 a = contour[j];
			Vector2 b = contour[(j + 1) % contour.Length];
			float dx = a.x - b.x;
			float dy = a.y - b.y;
			perimeter += (float)Math.Sqrt(dy * dy + dx * dx);
		}
		float closing = Vector2.Distance(contour[contour.Length - 1], contour[0]);
		if (perimeter > 1e-09f && closing / perimeter > ClosingSegmentMaxFraction)
		{
			return string.Format("контур НЕ ЗАМКНУТ: замыкающее звено {0:F2} мм = ", closing * 1000f) + string.Format("{0:F1} % периметра (порог ", closing / perimeter * 100f) + string.Format("{0:F0} %) — это открытый профиль, а не петля", 10f);
		}
		return null;
	}

	public static string ValidateField(float[] values, string fieldName, int frame)
	{
		if (values == null)
		{
			return string.Format("поле «{0}», кадр {1}: массива нет", fieldName, frame);
		}
		for (int i = 0; i < values.Length; i++)
		{
			float v = values[i];
			if (float.IsNaN(v) || float.IsInfinity(v))
			{
				return string.Format("поле «{0}», кадр {1}, элемент {2}: значение ", fieldName, frame, i) + (float.IsInfinity(v) ? "бесконечность" : "NaN");
			}
		}
		return null;
	}

	public static string ValidateNoOutrun(Vector2[] display, Vector2[] raw, float hM)
	{
		if (display == null || raw == null || display.Length == 0 || raw.Length == 0)
		{
			return null;
		}
		float maxZ = -float.MaxValue;
		for (int i = 0; i < raw.Length; i++)
		{
			if (raw[i].y > maxZ)
			{
				maxZ = raw[i].y;
			}
		}
		for (int j = 0; j < display.Length; j++)
		{
			if (display[j].y > maxZ + 1e-06f)
			{
				return string.Format("точка {0} показного контура вынесена за носок сырого: ", j) + string.Format("{0:F3} против {1:F3} мм", display[j].y * 1000f, maxZ * 1000f);
			}
		}
		return null;
	}

	public static FieldRange RangeOf(AxiMesh m, UltraField field)
	{
		FieldRange r = default(FieldRange);
		r.min = float.MaxValue;
		r.max = -float.MaxValue;
		r.valid = false;
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemAlive[e] == 0)
			{
				continue;
			}
			float v = Sample(m, e, field);
			if (v < r.min)
			{
				r.min = v;
			}
			if (v > r.max)
			{
				r.max = v;
			}
			r.valid = true;
		}
		return r;
	}

	public static float Sample(AxiMesh m, int e, UltraField field)
	{
		if (field == UltraField.Temperature)
		{
			return m.elemTemp[e];
		}
		if (field == UltraField.Pressure)
		{
			return m.elemP[e];
		}
		return m.elemEpsP[e];
	}

	public static Color RampTarget(float t)
	{
		float num = Mathf.Clamp(t, 0f, 1f);
		float u;
		if (num < 0.5f)
		{
			u = Mathf.Clamp(num * 2f, 0f, 1f);
			return new Color(u * 0.52f + 0.2f, u * 0.22f + 0.14f, u * 0.02f + 0.1f, 1f);
		}
		u = Mathf.Clamp((num - 0.5f) * 2f, 0f, 1f);
		return new Color(u * 0.28f + 0.72f, u * 0.5f + 0.36f, u * 0.43f + 0.12f, 1f);
	}

	public static Color Ramp(float t)
	{
		float num = Mathf.Clamp(t, 0f, 1f);
		float u;
		if (num < 0.5f)
		{
			u = Mathf.Clamp(num * 2f, 0f, 1f);
			return new Color(u * 0.62f + 0.16f, u * 0.26f + 0.19f, u * -0.12f + 0.3f, 1f);
		}
		u = Mathf.Clamp((num - 0.5f) * 2f, 0f, 1f);
		return new Color(u * 0.22f + 0.78f, u * 0.48f + 0.45f, u * 0.68f + 0.18f, 1f);
	}
}
