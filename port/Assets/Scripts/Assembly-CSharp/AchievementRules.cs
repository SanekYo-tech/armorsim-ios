using System.Collections.Generic;

public static class AchievementRules
{
	public const float ThinArmourMM = 10f;

	public const float CaliberEpsMM = 0.01f;

	public const string TitaniumName = "Titanium";

	public const float StuckLowFraction = 0.4f;

	public const float StuckHighFraction = 0.6f;

	public static bool RicochetBoing(ShotReport r)
	{
		return r != null && r.FinalVerdict == ShotOutcome.Ricochet;
	}

	public static bool Overkill(ShotReport r, float maxCaliberMM)
	{
		if (r == null || r.Layers == null || maxCaliberMM <= 0f || r.CaliberMM < maxCaliberMM - CaliberEpsMM)
		{
			return false;
		}
		foreach (LayerHit layer in r.Layers)
		{
			if (layer.Verdict != LayerVerdict.Penetrated && layer.ThicknessMM < ThinArmourMM)
			{
				return true;
			}
		}
		return false;
	}

	public static bool TitaniumStop(ShotReport r)
	{
		if (r == null || r.Layers == null || r.PenetratedAll)
		{
			return false;
		}
		foreach (LayerHit layer in r.Layers)
		{
			if (layer.Verdict == LayerVerdict.Stopped && layer.ArmorTypeName == TitaniumName)
			{
				return true;
			}
		}
		return false;
	}

	public static float StuckFraction(LayerHit h)
	{
		return (h.PathLengthMM > 0f) ? (h.PenetrationDepthMM / h.PathLengthMM) : (-1f);
	}

	public static bool StuckMidPlate(ShotReport r)
	{
		if (r == null || r.Layers == null)
		{
			return false;
		}
		foreach (LayerHit layer in r.Layers)
		{
			if (layer.Verdict == LayerVerdict.Stopped && layer.PathLengthMM > 0f)
			{
				float num = layer.PenetrationDepthMM / layer.PathLengthMM;
				if (num >= StuckLowFraction && num <= StuckHighFraction)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool Restless(int clicks)
	{
		return clicks >= 43;
	}

	public static bool HundredSims(int sims)
	{
		return sims >= 100;
	}

	public static int CrossedMilestone(int before, int after, IList<int> milestones)
	{
		if (milestones == null)
		{
			return 0;
		}
		for (int i = 0; i < milestones.Count; i++)
		{
			int num = milestones[i];
			if (before < num && after >= num)
			{
				return num;
			}
		}
		return 0;
	}
}
