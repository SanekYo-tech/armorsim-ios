#if !UNITY_IOS && UNITY_STANDALONE_WIN
using System;
using Steamworks;
using UnityEngine;

public static class SteamBoot
{
	private static bool _tried;

	private static bool _ok;

	public static string FailureReason { get; private set; }

	public static bool Initialized
	{
		get
		{
			Ensure();
			return _ok;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		Ensure();
	}

	private static void Ensure()
	{
		if (_tried)
		{
			return;
		}
		_tried = true;
		FailureReason = string.Empty;
		if (!Application.isPlaying)
		{
			FailureReason = "не режим игры";
			return;
		}
		try
		{
			if (SteamAPI.RestartAppIfNecessary(new AppId_t(5159810u)))
			{
				FailureReason = "перезапуск через клиент Steam";
				Application.Quit();
				return;
			}
		}
		catch (Exception ex)
		{
			_ok = false;
			FailureReason = "замок отгрузки: нативная библиотека не загрузилась: " + ex.Message;
			Debug.LogError("[Steam] " + FailureReason);
			Application.Quit();
			return;
		}
		if (!Packsize.Test())
		{
			_ok = false;
			FailureReason = "Packsize.Test провален: разрядность обёртки и steam_api не совпали";
			Debug.LogError("[Steam] " + FailureReason);
			return;
		}
		ESteamAPIInitResult eSteamAPIInitResult;
		string error;
		try
		{
			eSteamAPIInitResult = SteamAPI.InitEx(out error);
		}
		catch (Exception ex2)
		{
			_ok = false;
			FailureReason = "нативная библиотека не загрузилась: " + ex2.Message;
			Debug.LogError("[Steam] " + FailureReason);
			return;
		}
		if (eSteamAPIInitResult != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
		{
			_ok = false;
			FailureReason = eSteamAPIInitResult.ToString() + ": " + error;
			Debug.LogWarning("[Steam] API не поднялся — " + FailureReason + ". Достижения и мастерская работать не будут. Частые причины: клиент Steam не запущен; нет steam_appid.txt в корне проекта (для редактора); в файле чужой AppID.");
			return;
		}
		_ok = true;
		GameObject gameObject = new GameObject("SteamBoot");
		if (gameObject != null)
		{
			gameObject.hideFlags = HideFlags.HideAndDontSave;
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			gameObject.AddComponent<SteamPump>();
			Debug.Log(string.Concat("[Steam] API поднят, AppID ", SteamUtils.GetAppID().ToString(), ", игрок «", SteamFriends.GetPersonaName(), "»"));
		}
	}

	internal static void Down()
	{
		if (_ok)
		{
			_ok = false;
			SteamAPI.Shutdown();
			Debug.Log("[Steam] API опущен.");
		}
	}
}
#endif
