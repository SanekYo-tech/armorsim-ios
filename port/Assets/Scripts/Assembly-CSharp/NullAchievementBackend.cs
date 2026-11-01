using UnityEngine;

public sealed class NullAchievementBackend : IAchievementBackend
{
	private const string AchPrefix = "armorsim.ach.";

	private const string StatPrefix = "armorsim.stat.";

	public string Name => "локальный (без Steam)";

	public bool Ready => true;

	public bool IsUnlocked(string achievementId)
	{
		if (string.IsNullOrEmpty(achievementId))
		{
			return false;
		}
		return PlayerPrefs.GetInt(AchPrefix + achievementId, 0) != 0;
	}

	public void Unlock(string achievementId)
	{
		if (string.IsNullOrEmpty(achievementId) || IsUnlocked(achievementId))
		{
			return;
		}
		PlayerPrefs.SetInt(AchPrefix + achievementId, 1);
		PlayerPrefs.Save();
		Debug.Log(string.Concat("[Достижения] ★ ПОЛУЧЕНО: ", achievementId, " (приёмник: ", Name, ")"));
	}

	public int GetStat(string statName)
	{
		if (string.IsNullOrEmpty(statName))
		{
			return 0;
		}
		return PlayerPrefs.GetInt(StatPrefix + statName, 0);
	}

	public void SetStat(string statName, int value)
	{
		if (string.IsNullOrEmpty(statName))
		{
			return;
		}
		PlayerPrefs.SetInt(StatPrefix + statName, value);
	}

	public void IndicateProgress(string achievementId, int current, int max)
	{
		Debug.Log(string.Format("[Достижения] прогресс {0}: {1} из {2}", achievementId, current, max));
	}

	public void Store()
	{
		PlayerPrefs.Save();
	}

	public static void ResetLocalProgress()
	{
		PlayerPrefs.DeleteKey(AchPrefix + AchievementIds.Engineer);
		PlayerPrefs.DeleteKey(AchPrefix + AchievementIds.Restless);
		PlayerPrefs.DeleteKey(AchPrefix + AchievementIds.RicochetBoing);
		PlayerPrefs.DeleteKey(AchPrefix + AchievementIds.Overkill);
		PlayerPrefs.DeleteKey(AchPrefix + AchievementIds.TitaniumStop);
		PlayerPrefs.DeleteKey(AchPrefix + AchievementIds.HundredSims);
		PlayerPrefs.DeleteKey(AchPrefix + AchievementIds.StuckDead);
		PlayerPrefs.DeleteKey(StatPrefix + AchievementIds.StatNewSimClicks);
		PlayerPrefs.DeleteKey(StatPrefix + AchievementIds.StatSimCount);
		PlayerPrefs.Save();
		Debug.Log("[Достижения] локальный прогресс стёрт (только приёмник без Steam)");
	}
}
