public interface IAchievementBackend
{
	string Name { get; }

	bool Ready { get; }

	bool IsUnlocked(string achievementId);

	void Unlock(string achievementId);

	int GetStat(string statName);

	void SetStat(string statName, int value);

	void IndicateProgress(string achievementId, int current, int max);

	void Store();
}
