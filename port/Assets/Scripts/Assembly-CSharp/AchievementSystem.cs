using UnityEngine;

public sealed class AchievementSystem : MonoBehaviour
{
	private static AchievementSystem _instance;

	private static IAchievementBackend _backend = new NullAchievementBackend();

	private bool _engineerPending;

	public static IAchievementBackend Backend => _backend;

	public static void Install(IAchievementBackend backend)
	{
		if (backend == null)
		{
			backend = new NullAchievementBackend();
		}
		_backend = backend;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		if (_instance != null)
		{
			return;
		}
		GameObject gameObject = new GameObject("AchievementSystem");
		gameObject.hideFlags = HideFlags.HideAndDontSave;
		DontDestroyOnLoad(gameObject);
		_instance = gameObject.AddComponent<AchievementSystem>();
	}

	private void Awake()
	{
#if !UNITY_IOS && UNITY_STANDALONE_WIN
		Install(new SteamAchievementBackend());
#else
		Install(new NullAchievementBackend());
#endif
		GameEvents.ShotResolved += OnShotResolved;
		GameEvents.NewSimulationRequested += OnNewSimulationRequested;
		Debug.Log("[Достижения] система поднята, приёмник: " + Backend.Name);
		_engineerPending = true;
	}

	private void OnDisable()
	{
		GameEvents.ShotResolved -= OnShotResolved;
		GameEvents.NewSimulationRequested -= OnNewSimulationRequested;
	}

	private void OnDestroy()
	{
		GameEvents.ShotResolved -= OnShotResolved;
		GameEvents.NewSimulationRequested -= OnNewSimulationRequested;
		if (this == _instance)
		{
			_instance = null;
		}
	}

	private void Update()
	{
		if (!_engineerPending)
		{
			return;
		}
		if (!Backend.Ready)
		{
			return;
		}
		_engineerPending = false;
		Backend.Unlock(AchievementIds.Engineer);
		Backend.Store();
	}

	private void OnNewSimulationRequested()
	{
		if (!Backend.Ready)
		{
			return;
		}
		if (Backend.IsUnlocked(AchievementIds.Restless))
		{
			return;
		}
		int num = Backend.GetStat(AchievementIds.StatNewSimClicks) + 1;
		Backend.SetStat(AchievementIds.StatNewSimClicks, num);
		if (AchievementRules.Restless(num))
		{
			Backend.Unlock(AchievementIds.Restless);
		}
		Backend.Store();
	}

	private void OnShotResolved(ShotReport report)
	{
		if (report == null)
		{
			return;
		}
		if (!Backend.Ready)
		{
			return;
		}
		if (report.FinalVerdict != ShotOutcome.NoModel && !Backend.IsUnlocked(AchievementIds.HundredSims))
		{
			int num = Backend.GetStat(AchievementIds.StatSimCount);
			Backend.SetStat(AchievementIds.StatSimCount, num + 1);
			if (AchievementRules.HundredSims(num + 1))
			{
				Backend.Unlock(AchievementIds.HundredSims);
			}
			else
			{
				int num2 = AchievementRules.CrossedMilestone(num, num + 1, AchievementIds.SimCountMilestones);
				if (num2 > 0)
				{
					Backend.IndicateProgress(AchievementIds.HundredSims, num + 1, AchievementIds.HundredSimsGoal);
				}
			}
		}
		TryUnlock(AchievementIds.RicochetBoing, AchievementRules.RicochetBoing(report));
		TryUnlock(AchievementIds.Overkill, AchievementRules.Overkill(report, ShotReportBuilder.MaxCalibreMM()));
		TryUnlock(AchievementIds.TitaniumStop, AchievementRules.TitaniumStop(report));
		TryUnlock(AchievementIds.StuckDead, AchievementRules.StuckMidPlate(report));
		Backend.Store();
	}

	private void TryUnlock(string id, bool fired)
	{
		if (!fired)
		{
			return;
		}
		if (!Backend.IsUnlocked(id))
		{
			Backend.Unlock(id);
		}
	}
}
