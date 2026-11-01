using System;
using UnityEngine;

public static class RadarScenarioGuiGeometry
{
	public static Matrix4x4 LineMatrix(Matrix4x4 parent, Vector2 start, Vector2 end)
	{
		float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
		return parent * Matrix4x4.TRS(new Vector3(start.x, start.y, 0f), Quaternion.Euler(0f, 0f, angle), Vector3.one);
	}

	public static void Line(Vector2 start, Vector2 end, Color color, float width)
	{
		Matrix4x4 matrix = GUI.matrix;
		Color color2 = GUI.color;
		try
		{
			float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
			GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(start.x, start.y, 0f), Quaternion.Euler(0f, 0f, angle), Vector3.one);
			GUI.color = color;
			float num = (float)Math.Sqrt((end.x - start.x) * (end.x - start.x) + (end.y - start.y) * (end.y - start.y));
			GUI.DrawTexture(new Rect(0f, -width * 0.5f, num, width), Texture2D.whiteTexture);
		}
		finally
		{
			GUI.matrix = matrix;
			GUI.color = color2;
		}
	}
}
