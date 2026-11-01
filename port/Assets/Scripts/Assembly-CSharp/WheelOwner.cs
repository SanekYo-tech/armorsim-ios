using System.Collections.Generic;

public static class WheelOwner
{
	public enum Owner
	{
		Scene = 0,
		Panel = 1
	}

	public static bool SabotageIgnorePanels;

	public static bool Inside(WheelZone z, float x, float y)
	{
		return z.xMax > z.xMin && z.yMax > z.yMin && x >= z.xMin && x <= z.xMax && y >= z.yMin && y <= z.yMax;
	}

	public static bool AnyInside(IList<WheelZone> zones, float x, float y)
	{
		if (zones == null)
		{
			return false;
		}
		for (int i = 0; i < zones.Count; i++)
		{
			if (Inside(zones[i], x, y))
			{
				return true;
			}
		}
		return false;
	}

	public static Owner Decide(bool cursorOverPanel)
	{
		if (!cursorOverPanel)
		{
			return Owner.Scene;
		}
		return SabotageIgnorePanels ? Owner.Scene : Owner.Panel;
	}

	public static float ForScene(float raw, bool cursorOverPanel)
	{
		if (!cursorOverPanel || SabotageIgnorePanels)
		{
			return raw;
		}
		return 0f;
	}

	public static float ForPanel(float raw, bool cursorOverPanel)
	{
		if (!cursorOverPanel || SabotageIgnorePanels)
		{
			return 0f;
		}
		return raw;
	}

	public static bool Exclusive(float raw, bool cursorOverPanel)
	{
		float num = ForScene(raw, cursorOverPanel);
		float num2 = ForPanel(raw, cursorOverPanel);
		if (num == 0f)
		{
			if (num2.Equals(raw))
			{
				return true;
			}
		}
		if (num2 == 0f)
		{
			return num.Equals(raw);
		}
		return false;
	}
}
