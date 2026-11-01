using System;
using UnityEngine;

public static class RadarAtlasPalette
{
	public static readonly Color Below = new Color(0.48f, 0.5f, 0.52f, 1f);

	public static Color Step(float fraction)
	{
		float num = Mathf.Clamp01(fraction);
		num = Mathf.Round(num * 10f) / 10f;
		return Color.HSVToRGB((1f - num) * 2f / 3f, 0.9f, 0.95f, true);
	}

	public static double Db(double sigma)
	{
		if (sigma > 0.0)
		{
			return 10.0 * Math.Log10(sigma);
		}
		return 0.0;
	}

	public static double MaxDb(RadarAtlas atlas)
	{
		double num = 0.0;
		for (int i = 0; i < atlas.CellCount; i++)
		{
			num = Math.Max(num, Db(atlas.CellSigmaHH(i)));
		}
		return num;
	}

	public static Color ColorOf(double db, double topDb, double rangeDb)
	{
		if (db < topDb - rangeDb)
		{
			return Below;
		}
		return Step((float)((db - (topDb - rangeDb)) / rangeDb));
	}

	public static Texture2D Texture(RadarAtlas atlas, double rangeDb, out double topDb)
	{
		int num = (int)Math.Round(360.0 / atlas.Spec.DeltaDeg);
		int num2 = (int)Math.Round(180.0 / atlas.Spec.DeltaDeg);
		topDb = Math.Ceiling(MaxDb(atlas));
		Texture2D texture2D = new Texture2D(num, num2, TextureFormat.RGBA32, false);
		texture2D.name = "RadarAtlasMap";
		texture2D.filterMode = FilterMode.Point;
		texture2D.wrapMode = TextureWrapMode.Clamp;
		Color32[] array = new Color32[num * num2];
		for (int j = 0; j < num2; j++)
		{
			for (int i = 0; i < num; i++)
			{
				array[j * num + i] = ColorOf(Db(atlas.CellSigmaHH(atlas.Spec.CellIndex(i, j))), topDb, rangeDb);
			}
		}
		texture2D.SetPixels32(array);
		texture2D.Apply();
		return texture2D;
	}

	public static Vector2 Point(Rect map, double azDeg, double elDeg)
	{
		return new Vector2(map.x + (float)((azDeg + 180.0) / 360.0) * map.width, map.y + map.height - (float)((elDeg + 90.0) / 180.0) * map.height);
	}
}
