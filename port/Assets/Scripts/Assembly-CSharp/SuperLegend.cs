using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class SuperLegend
{
	public static readonly Color32 Ink;

	public const int BarW = 34;

	public const int BarRight = 185;

	public const int BarTop = 110;

	public const int BarBottom = 130;

	public const int TickGap = 10;

	public const int TickLen = 6;

	public const int TitleDx = 6;

	public const int TitleDy = 40;

	public const int StampX = 24;

	public const int StampY = 22;

	public const int StampScale = 3;

	public const int CounterY = 52;

	public const int CounterScale = 2;

	public const int LabelScale = 2;

	public static readonly Color32 DeadInk;

	private static readonly byte[] Unknown;

	private static readonly Dictionary<char, byte[]> Glyphs;

	static SuperLegend()
	{
		Ink = new Color32(40, 40, 46, 255);
		DeadInk = new Color32(200, 30, 30, 255);
		Unknown = new byte[7] { 31, 31, 31, 31, 31, 31, 31 };
		Glyphs = new Dictionary<char, byte[]>();
		Glyphs['0'] = new byte[7] { 14, 17, 19, 21, 25, 17, 14 };
		Glyphs['1'] = new byte[7] { 4, 12, 4, 4, 4, 4, 14 };
		Glyphs['2'] = new byte[7] { 14, 17, 1, 2, 4, 8, 31 };
		Glyphs['3'] = new byte[7] { 31, 2, 4, 2, 1, 17, 14 };
		Glyphs['4'] = new byte[7] { 2, 6, 10, 18, 31, 2, 2 };
		Glyphs['5'] = new byte[7] { 31, 16, 30, 1, 1, 17, 14 };
		Glyphs['6'] = new byte[7] { 6, 8, 16, 30, 17, 17, 14 };
		Glyphs['7'] = new byte[7] { 31, 1, 2, 4, 8, 8, 8 };
		Glyphs['8'] = new byte[7] { 14, 17, 17, 14, 17, 17, 14 };
		Glyphs['9'] = new byte[7] { 14, 17, 17, 15, 1, 2, 12 };
		Glyphs['.'] = new byte[7] { 0, 0, 0, 0, 0, 12, 12 };
		Glyphs[','] = new byte[7] { 0, 0, 0, 0, 12, 4, 8 };
		Glyphs['-'] = new byte[7] { 0, 0, 0, 31, 0, 0, 0 };
		Glyphs['+'] = new byte[7] { 0, 4, 4, 31, 4, 4, 0 };
		Glyphs['='] = new byte[7] { 0, 0, 31, 0, 31, 0, 0 };
		Glyphs['/'] = new byte[7] { 1, 1, 2, 4, 8, 16, 16 };
		Glyphs['['] = new byte[7] { 14, 8, 8, 8, 8, 8, 14 };
		Glyphs[']'] = new byte[7] { 14, 2, 2, 2, 2, 2, 14 };
		Glyphs['|'] = new byte[7] { 4, 4, 4, 4, 4, 4, 4 };
		Glyphs['('] = new byte[7] { 2, 4, 8, 8, 8, 4, 2 };
		Glyphs[')'] = new byte[7] { 8, 4, 2, 2, 2, 4, 8 };
		Glyphs['_'] = new byte[7] { 0, 0, 0, 0, 0, 0, 31 };
		Glyphs['%'] = new byte[7] { 25, 26, 2, 4, 8, 11, 19 };
		Glyphs['e'] = new byte[7] { 0, 0, 14, 17, 31, 16, 14 };
		Glyphs['i'] = new byte[7] { 4, 0, 12, 4, 4, 4, 14 };
		Glyphs['n'] = new byte[7] { 0, 0, 22, 25, 17, 17, 17 };
		Glyphs['m'] = new byte[7] { 0, 0, 26, 21, 21, 21, 21 };
		Glyphs['a'] = new byte[7] { 0, 0, 14, 1, 15, 17, 15 };
		Glyphs['x'] = new byte[7] { 0, 0, 17, 10, 4, 10, 17 };
		Glyphs['p'] = new byte[7] { 0, 0, 30, 17, 30, 16, 16 };
		Glyphs['s'] = new byte[7] { 0, 0, 15, 16, 14, 1, 30 };
		Glyphs['v'] = new byte[7] { 0, 0, 17, 17, 17, 10, 4 };
		Glyphs['t'] = new byte[7] { 8, 8, 28, 8, 8, 9, 6 };
		Glyphs['M'] = new byte[7] { 17, 27, 21, 21, 17, 17, 17 };
		Glyphs['T'] = new byte[7] { 31, 4, 4, 4, 4, 4, 4 };
		Glyphs['D'] = new byte[7] { 30, 17, 17, 17, 17, 17, 30 };
		Glyphs['м'] = new byte[7] { 0, 0, 17, 27, 21, 17, 17 };
		Glyphs['с'] = new byte[7] { 0, 0, 14, 17, 16, 17, 14 };
		Glyphs['к'] = new byte[7] { 0, 0, 18, 20, 24, 20, 18 };
		Glyphs['а'] = new byte[7] { 0, 0, 14, 1, 15, 17, 15 };
		Glyphs['д'] = new byte[7] { 0, 6, 10, 10, 10, 31, 17 };
		Glyphs['р'] = new byte[7] { 0, 0, 30, 17, 30, 16, 16 };
		Glyphs['Г'] = new byte[7] { 31, 16, 16, 16, 16, 16, 16 };
		Glyphs['П'] = new byte[7] { 31, 17, 17, 17, 17, 17, 17 };
		Glyphs['К'] = new byte[7] { 17, 18, 20, 24, 20, 18, 17 };
	}

	public static bool RangeAlive(SuperTape.Frame f, int fieldIdx)
	{
		return f.fieldMax[fieldIdx] > f.fieldMin[fieldIdx];
	}

	public static string Stamp(float simTimeS)
	{
		return "t = " + StampValue(simTimeS) + " мкс";
	}

	public static string StampValue(float simTimeS)
	{
		return (simTimeS * 1000000f).ToString("F1").Replace(',', '.');
	}

	public static string Counter(int frameIdx, int frameCount)
	{
		return string.Format("кадр {0}/{1}", frameIdx, frameCount);
	}

	public static string Title(string fieldName, string unit)
	{
		return fieldName + " [" + unit + "]";
	}

	public static void Compose(Color32[] px, int w, int h, string fieldName, string unit, float mn, float mx, int bands, float simTimeS, int frameIdx, int frameCount)
	{
		int num = w - BarRight;
		int num2 = h - BarBottom - BarTop;
		for (int i = BarTop; i < h - BarBottom; i++)
		{
			float num3 = (float)(i - BarTop) / (float)num2;
			int num4 = Mathf.Min((int)((1f - num3) * (float)bands), bands - 1);
			Color32 c = EncodeDisplay(SuperTapeRender.Rainbow((float)num4 / (float)(bands - 1)));
			for (int j = num; j < num + BarW; j++)
			{
				Set(px, w, h, j, i, c);
			}
		}
		Frame(px, w, h, num - 1, BarTop - 1, BarW + 2, num2 + 2, Ink);
		bool flag = !(mx > mn);
		for (int k = 0; k <= bands; k++)
		{
			int num5 = (h - BarBottom) - Mathf.RoundToInt((float)k / (float)bands * (float)num2);
			for (int l = num + BarW; l < num + BarW + TickLen; l++)
			{
				Set(px, w, h, l, num5, Ink);
			}
			Text(px, w, h, num + BarW + TickGap, num5 - 5, Fmt(mn + (float)k / (float)bands * (mx - mn)), LabelScale, Ink);
		}
		Text(px, w, h, num - TitleDx, BarTop - TitleDy, Title(fieldName, unit), LabelScale, Ink);
		if (flag)
		{
			Text(px, w, h, num - 60, 46, "min = max", LabelScale, DeadInk);
		}
		Text(px, w, h, StampX, StampY, Stamp(simTimeS), StampScale, Ink);
		Text(px, w, h, StampX, CounterY, Counter(frameIdx, frameCount - 1), CounterScale, Ink);
	}

	private static Color32 EncodeDisplay(Color c)
	{
		if (QualitySettings.activeColorSpace == ColorSpace.Linear)
		{
			return new Color(Mathf.LinearToGammaSpace(c.r), Mathf.LinearToGammaSpace(c.g), Mathf.LinearToGammaSpace(c.b), c.a);
		}
		return c;
	}

	public static string Fmt(float v)
	{
		v = Mathf.Abs(v);
		if (v >= 10000f || (v > 0f && v < 0.01f))
		{
			return v.ToString("0.00e+00", CultureInfo.InvariantCulture);
		}
		if (v >= 100f)
		{
			return v.ToString("0", CultureInfo.InvariantCulture);
		}
		if (v >= 10f)
		{
			return v.ToString("0.0", CultureInfo.InvariantCulture);
		}
		return v.ToString("0.00", CultureInfo.InvariantCulture);
	}

	private static void Set(Color32[] px, int w, int h, int x, int y, Color32 c)
	{
		if (x < 0 || y < 0 || x >= w || y >= h)
		{
			return;
		}
		px[(h - y - 1) * w + x] = c;
	}

	private static void Frame(Color32[] px, int w, int h, int x, int y, int fw, int fh, Color32 c)
	{
		for (int i = 0; i < fw; i++)
		{
			Set(px, w, h, x + i, y, c);
			Set(px, w, h, x + i, y + fh - 1, c);
		}
		for (int j = 0; j < fh; j++)
		{
			Set(px, w, h, x, y + j, c);
			Set(px, w, h, x + fw - 1, y + j, c);
		}
	}

	public static void Text(Color32[] px, int w, int h, int x, int y, string s, int scale, Color32 c)
	{
		for (int i = 0; i < s.Length; i++)
		{
			char c2 = s[i];
			if (c2 != ' ')
			{
				byte[] array;
				if (!Glyphs.TryGetValue(c2, out array))
				{
					array = Unknown;
				}
				int num = y;
				for (int j = 0; j < 7; j++)
				{
					int num2 = x;
					for (int k = 0; k < 5; k++)
					{
						if ((array[j] & (16 >> k)) != 0)
						{
							for (int l = 0; l < scale; l++)
							{
								for (int m = 0; m < scale; m++)
								{
									Set(px, w, h, num2 + m, num + l, c);
								}
							}
						}
						num2 += scale;
					}
					num += scale;
				}
			}
			x += 6 * scale;
		}
	}
}
