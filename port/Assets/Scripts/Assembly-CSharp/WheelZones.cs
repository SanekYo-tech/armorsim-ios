using System;
using System.Collections.Generic;
using UnityEngine;

public static class WheelZones
{
	private static readonly List<RectTransform> _zones;

	private static readonly List<WheelZone> _buf;

	private static readonly Vector3[] _corners;

	public static Func<Vector2> CursorSource;

	public static Func<float> WheelSource;

	static WheelZones()
	{
		_zones = new List<RectTransform>();
		_buf = new List<WheelZone>();
		_corners = new Vector3[4];
	}

	public static int Count => _zones.Count;

	public static void Register(RectTransform zone)
	{
		if (zone == null)
		{
			return;
		}
		for (int i = 0; i < _zones.Count; i++)
		{
			if (_zones[i] == zone)
			{
				return;
			}
		}
		_zones.Add(zone);
	}

	public static void Clear()
	{
		_zones.Clear();
	}

	public static IList<WheelZone> Zones()
	{
		_buf.Clear();
		for (int num = _zones.Count - 1; num >= 0; num--)
		{
			RectTransform rectTransform = _zones[num];
			if (rectTransform == null)
			{
				_zones.RemoveAt(num);
			}
			else if (rectTransform.gameObject.activeInHierarchy && TryZone(rectTransform, out WheelZone zone))
			{
				_buf.Add(zone);
			}
		}
		return _buf;
	}

	private static bool TryZone(RectTransform rt, out WheelZone zone)
	{
		zone = default(WheelZone);
		Canvas canvas = rt.GetComponentInParent<Canvas>();
		if (canvas == null)
		{
			return false;
		}
		if (!canvas.isActiveAndEnabled)
		{
			return false;
		}
		Canvas rootCanvas = canvas.rootCanvas;
		Camera camera = null;
		if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
		{
			camera = rootCanvas.worldCamera;
		}
		rt.GetWorldCorners(_corners);
		float num = float.MaxValue;
		float num2 = float.MinValue;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		for (int i = 0; i < 4; i++)
		{
			Vector2 vector = RectTransformUtility.WorldToScreenPoint(camera, _corners[i]);
			if (vector.x < num)
			{
				num = vector.x;
			}
			if (vector.x > num2)
			{
				num2 = vector.x;
			}
			if (vector.y < num3)
			{
				num3 = vector.y;
			}
			if (vector.y > num4)
			{
				num4 = vector.y;
			}
		}
		zone.xMin = num;
		zone.yMin = num3;
		zone.xMax = num2;
		zone.yMax = num4;
		return num < num2 && num3 < num4;
	}

	public static Vector2 Cursor()
	{
		if (CursorSource != null)
		{
			return CursorSource();
		}
		return Input.mousePosition;
	}

	public static bool CursorOverPanel()
	{
		Vector2 vector = Cursor();
		return WheelOwner.AnyInside(Zones(), vector.x, vector.y);
	}

	public static float SceneWheel(float raw)
	{
		if (WheelSource != null)
		{
			raw = WheelSource();
		}
		if (CursorOverPanel() && !WheelOwner.SabotageIgnorePanels)
		{
			raw = 0f;
		}
		return raw;
	}
}
