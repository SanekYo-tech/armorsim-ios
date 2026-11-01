#if !UNITY_IOS && UNITY_STANDALONE_WIN
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

public sealed class SteamAchievementBackend : IAchievementBackend
{
	private readonly List<string> _pendingUnlocks = new List<string>();

	private readonly Dictionary<string, int> _pendingStats = new Dictionary<string, int>();

	private bool _statsReceived;

	private Callback<UserStatsReceived_t> _statsCallback;

	public SteamAchievementBackend()
	{
		if (SteamBoot.Initialized)
		{
			_statsCallback = Callback<UserStatsReceived_t>.Create(new Callback<UserStatsReceived_t>.DispatchDelegate(OnUserStatsReceived));
		}
		else
		{
			Debug.LogWarning("[Достижения] Steam API не поднялся (клиент не запущен, нет steam_appid.txt или чужой AppID). Достижения не уедут; локальные счётчики этим приёмником не ведутся.");
		}
	}

	public string Name => "Steam";

	public bool Ready => SteamBoot.Initialized;

	private void OnUserStatsReceived(UserStatsReceived_t cb)
	{
		if (cb.m_nGameID != new CGameID(SteamUtils.GetAppID()).m_GameID)
		{
			return;
		}
		if (cb.m_eResult != EResult.k_EResultOK)
		{
			Debug.LogError(string.Format("[Достижения] статистика не пришла: {0}.", cb.m_eResult));
			return;
		}
		bool flag = _statsReceived;
		_statsReceived = true;
		Debug.Log("[Достижения] статистика Steam получена (" + (flag ? "повторно" : "впервые") + "); " + string.Format("в очереди было {0} разблокировок и {1} статов", _pendingUnlocks.Count, _pendingStats.Count));
		Flush();
	}

	private void Flush()
	{
		if (!SteamBoot.Initialized)
		{
			return;
		}
		foreach (KeyValuePair<string, int> pendingStat in _pendingStats)
		{
			WriteStat(pendingStat.Key, pendingStat.Value);
		}
		_pendingStats.Clear();
		for (int i = 0; i < _pendingUnlocks.Count; i++)
		{
			WriteUnlock(_pendingUnlocks[i]);
		}
		_pendingUnlocks.Clear();
		SteamUserStats.StoreStats();
	}

	public bool IsUnlocked(string achievementId)
	{
		if (string.IsNullOrEmpty(achievementId))
		{
			return false;
		}
		if (!SteamBoot.Initialized)
		{
			return _pendingUnlocks.Contains(achievementId);
		}
		bool unlocked = false;
		if (!SteamUserStats.GetAchievement(achievementId, out unlocked))
		{
			Debug.LogError("[Достижения] GetAchievement(«" + achievementId + "») вернул false: имя не заведено в App Admin или конфигурация не опубликована.");
			return false;
		}
		return unlocked;
	}

	public int GetStat(string statName)
	{
		if (string.IsNullOrEmpty(statName))
		{
			return 0;
		}
		int value = 0;
		if (_pendingStats.TryGetValue(statName, out value))
		{
			return value;
		}
		if (!SteamBoot.Initialized)
		{
			return 0;
		}
		int result = 0;
		if (!SteamUserStats.GetStat(statName, out result))
		{
			Debug.LogError("[Достижения] GetStat(«" + statName + "») вернул false: стат не заведён в App Admin или имеет другой тип (нужен INT).");
			return 0;
		}
		return result;
	}

	public void Unlock(string achievementId)
	{
		if (string.IsNullOrEmpty(achievementId))
		{
			return;
		}
		if (SteamBoot.Initialized)
		{
			WriteUnlock(achievementId);
			return;
		}
		if (!_pendingUnlocks.Contains(achievementId))
		{
			_pendingUnlocks.Add(achievementId);
		}
		Debug.Log("[Достижения] " + achievementId + " отложено до прихода статистики Steam");
	}

	private void WriteUnlock(string achievementId)
	{
		if (SteamUserStats.SetAchievement(achievementId))
		{
			Debug.Log("[Достижения] ★ ПОЛУЧЕНО: " + achievementId + " (Steam)");
		}
		else
		{
			Debug.LogError("[Достижения] SetAchievement(«" + achievementId + "») вернул false — имя не заведено в App Admin или конфигурация не опубликована.");
		}
	}

	public void SetStat(string statName, int value)
	{
		if (string.IsNullOrEmpty(statName))
		{
			return;
		}
		if (SteamBoot.Initialized)
		{
			WriteStat(statName, value);
		}
		else
		{
			_pendingStats[statName] = value;
		}
	}

	private void WriteStat(string statName, int value)
	{
		if (!SteamUserStats.SetStat(statName, value))
		{
			Debug.LogError(string.Format("[Достижения] SetStat(«{0}», {1}) вернул false — ", statName, value) + "стат не заведён в App Admin или имеет другой тип.");
		}
	}

	public void IndicateProgress(string achievementId, int current, int max)
	{
		if (!SteamBoot.Initialized)
		{
			return;
		}
		if (string.IsNullOrEmpty(achievementId))
		{
			return;
		}
		if (!IsUnlocked(achievementId))
		{
			SteamUserStats.IndicateAchievementProgress(achievementId, (uint)current, (uint)max);
		}
	}

	public void Store()
	{
		if (!SteamBoot.Initialized)
		{
			return;
		}
		if (!SteamUserStats.StoreStats())
		{
			Debug.LogWarning("[Достижения] StoreStats вернул false — накопленное осталось локальным и уедет со следующей попыткой.");
		}
	}
}
#endif
