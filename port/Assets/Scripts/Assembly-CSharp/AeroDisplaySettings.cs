using UnityEngine;

public static class AeroDisplaySettings
{
	public const string Key = "armorsim.aero.clouds";

	public static bool Clouds
	{
		get
		{
			return PlayerPrefs.GetInt(Key, 0) == 1;
		}
		set
		{
			PlayerPrefs.SetInt(Key, value ? 1 : 0);
			PlayerPrefs.Save();
		}
	}
}
