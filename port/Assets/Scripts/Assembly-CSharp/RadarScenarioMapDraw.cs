using System;
using UnityEngine;

public static class RadarScenarioMapDraw
{
	internal static Rect? ClipBounds;

	public static void Label(Rect rect, string value, GUIStyle style)
	{
		if (!ClipBounds.HasValue)
		{
			GUI.Label(rect, value, style);
			return;
		}
		Rect clip = ClipBounds.Value;
		GUI.BeginGroup(clip);
		GUI.Label(new Rect(rect.x - clip.x, rect.y - clip.y, rect.width, rect.height), value, style);
		GUI.EndGroup();
	}

	public static void Fill(Rect rect, Color color)
	{
		if (ClipBounds.HasValue)
		{
			Rect clip = ClipBounds.Value;
			float x = Math.Max(rect.x, clip.x);
			float y = Math.Max(rect.y, clip.y);
			float num = Math.Max(rect.x + rect.width, clip.x + clip.width);
			float num2 = Math.Max(rect.y + rect.height, clip.y + clip.height);
			rect = new Rect(x, y, num - x, num2 - y);
			if (rect.width <= 0f || rect.height <= 0f)
			{
				return;
			}
		}
		Color color2 = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(rect, Texture2D.whiteTexture);
		GUI.color = color2;
	}

	public static void Line(Vector2 a, Vector2 b, Color color, float width = 1f)
	{
		if (Event.current.type != EventType.Repaint)
		{
			return;
		}
		if ((b - a).sqrMagnitude < 0.0001f)
		{
			return;
		}
		if (ClipBounds.HasValue)
		{
			Rect clip = ClipBounds.Value;
			Rect clip2 = new Rect(clip.x + width * 0.5f, clip.y + width * 0.5f, clip.width - width, clip.height - width);
			if (!ClipLine(clip2, ref a, ref b))
			{
				return;
			}
		}
		RadarScenarioGuiGeometry.Line(a, b, color, width);
	}

	public static bool ClipLine(Rect clip, ref Vector2 start, ref Vector2 end)
	{
		float num = end.x - start.x;
		float num2 = end.y - start.y;
		float enter = 0f;
		float exit = 1f;
		if (!ClipEdge(-num, start.x - clip.x, ref enter, ref exit))
		{
			return false;
		}
		if (!ClipEdge(num, clip.x + clip.width - start.x, ref enter, ref exit))
		{
			return false;
		}
		if (!ClipEdge(-num2, start.y - clip.y, ref enter, ref exit))
		{
			return false;
		}
		if (!ClipEdge(num2, clip.y + clip.height - start.y, ref enter, ref exit))
		{
			return false;
		}
		end = new Vector2(start.x + num * exit, start.y + num2 * exit);
		start = new Vector2(start.x + num * enter, start.y + num2 * enter);
		return true;
	}

	private static bool ClipEdge(float p, float q, ref float enter, ref float exit)
	{
		if (Math.Abs(p) < 1E-06f)
		{
			return q >= 0f;
		}
		float num = q / p;
		if (p < 0f)
		{
			if (num > exit)
			{
				return false;
			}
			if (num < enter)
			{
				enter = num;
			}
			return true;
		}
		if (enter > num)
		{
			return false;
		}
		if (exit > num)
		{
			exit = num;
		}
		return true;
	}

	public static void DashedLine(Vector2 a, Vector2 b, Color color, float width = 1f)
	{
		float num = Vector2.Distance(a, b);
		if (num <= 0.01f)
		{
			return;
		}
		float num2 = (b.x - a.x) / num;
		float num3 = (b.y - a.y) / num;
		for (float num4 = 0f; num4 < num; num4 += 13f)
		{
			float num5 = Math.Min(num4 + 7f, num);
			Line(new Vector2(a.x + num2 * num4, a.y + num3 * num4), new Vector2(a.x + num2 * num5, a.y + num3 * num5), color, width);
		}
	}

	public static void Circle(Vector2 centre, float radius, Color color, float width = 1f)
	{
		Vector2 vector = centre + Vector2.right * radius;
		for (int i = 1; i <= 96; i++)
		{
			float f = (float)i * Mathf.PI * 2f / 96f;
			Vector2 vector2 = new Vector2(centre.x + Mathf.Cos(f) * radius, centre.y + Mathf.Sin(f) * radius);
			Line(vector, vector2, color, width);
			vector = vector2;
		}
	}

	public static void Outline(Rect rect, Color color, float width = 1f)
	{
		Fill(new Rect(rect.x, rect.y, rect.width, width), color);
		Fill(new Rect(rect.x, rect.y + rect.height - width, rect.width, width), color);
		Fill(new Rect(rect.x, rect.y, width, rect.height), color);
		Fill(new Rect(rect.x + rect.width - width, rect.y, width, rect.height), color);
	}
}
