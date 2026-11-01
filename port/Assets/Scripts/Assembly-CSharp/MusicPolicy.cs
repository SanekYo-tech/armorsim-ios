using System;
using System.Collections.Generic;

public static class MusicPolicy
{
	public const float TotalPercent = 100f;

	public const float NormalVolume = 1f;

	public const float ReturnVolumeScale = 0.5f;

	public const float ReturnDelaySeconds = 16f;

	public const float UltraStartVolumeScale = 0.5f;

	public const float UltraRampSeconds = 30f;

	public static readonly MusicTrack[] MenuTracks;

	public static readonly MusicTrack[] GameTracks;

	public static MusicProgram MenuProgram(bool returnedFromGame)
	{
		return new MusicProgram
		{
			Name = "меню",
			Tracks = MenuTracks,
			Plan = (returnedFromGame ? Steady(ReturnDelaySeconds, ReturnVolumeScale) : Steady(0f, NormalVolume))
		};
	}

	public static MusicProgram GameProgram()
	{
		return new MusicProgram
		{
			Name = "игра",
			Tracks = GameTracks,
			Plan = new MusicPlan
			{
				DelaySeconds = 0f,
				Volume = UltraStartVolumeScale,
				TargetVolume = NormalVolume,
				RampSeconds = UltraRampSeconds
			},
			SurvivesSceneChange = true
		};
	}

	public static bool GameKeepsPlaying(string sceneName, string menuSceneName)
	{
		return !string.Equals(sceneName, menuSceneName, StringComparison.OrdinalIgnoreCase);
	}

	public static MusicPlan Steady(float delaySeconds, float volume)
	{
		return new MusicPlan
		{
			DelaySeconds = delaySeconds,
			Volume = volume,
			TargetVolume = volume,
			RampSeconds = 0f
		};
	}

	public static float VolumeAt(MusicPlan plan, float secondsSincePlaying)
	{
		if (plan.RampSeconds <= 0f)
		{
			return plan.Volume;
		}
		float num = Math.Min(1f, Math.Max(0f, secondsSincePlaying / plan.RampSeconds));
		return plan.Volume + (plan.TargetVolume - plan.Volume) * num;
	}

	public static float[] Percents(MusicTrack[] tracks)
	{
		if (tracks == null || tracks.Length == 0)
		{
			return new float[0];
		}
		int num = 0;
		float num2 = 0f;
		foreach (MusicTrack musicTrack in tracks)
		{
			if (musicTrack.Percent < 0f)
			{
				num++;
			}
			else
			{
				num2 += musicTrack.Percent;
			}
		}
		float num3 = ((num > 0) ? ((TotalPercent - num2) / (float)num) : 0f);
		float[] array = new float[tracks.Length];
		for (int i = 0; i < tracks.Length; i++)
		{
			array[i] = ((tracks[i].Percent < 0f) ? num3 : tracks[i].Percent);
		}
		return array;
	}

	public static int Pick(float[] percents, float roll)
	{
		if (percents == null || percents.Length == 0)
		{
			return -1;
		}
		float num = 0f;
		foreach (float num2 in percents)
		{
			if (num2 > 0f)
			{
				num += num2;
			}
		}
		if (num <= 0f)
		{
			return -1;
		}
		float num3 = num * roll;
		float num4 = 0f;
		for (int i = 0; i < percents.Length; i++)
		{
			if (percents[i] > 0f)
			{
				num4 += percents[i];
				if (num4 > num3)
				{
					return i;
				}
			}
		}
		for (int j = percents.Length - 1; j >= 0; j--)
		{
			if (percents[j] > 0f)
			{
				return j;
			}
		}
		return -1;
	}

	public static List<string> Validate(MusicTrack[] tracks)
	{
		List<string> list = new List<string>();
		if (tracks == null || tracks.Length == 0)
		{
			list.Add("список треков пуст");
			return list;
		}
		HashSet<string> hashSet = new HashSet<string>();
		float num = 0f;
		int num2 = 0;
		foreach (MusicTrack musicTrack in tracks)
		{
			if (string.IsNullOrEmpty(musicTrack.Name))
			{
				list.Add("трек без имени");
			}
			else if (!hashSet.Add(musicTrack.Name))
			{
				list.Add("имя '" + musicTrack.Name + "' встречается дважды");
			}
			if (musicTrack.Percent < 0f)
			{
				num2++;
				continue;
			}
			num += musicTrack.Percent;
			if (musicTrack.Percent <= 0f || musicTrack.Percent > TotalPercent)
			{
				list.Add(string.Format("'{0}': шанс {1} вне (0, {2}]", musicTrack.Name, musicTrack.Percent, TotalPercent));
			}
		}
		if (num2 > 0)
		{
			if (num >= TotalPercent)
			{
				list.Add(string.Format("назначено {0} % при {1} обычных треках — остатка им не остаётся", num, num2));
			}
		}
		else if (Math.Abs(num - TotalPercent) > 0.001f)
		{
			list.Add(string.Format("обычных треков нет, а сумма назначенных {0} %, а не {1}", num, TotalPercent));
		}
		float[] array = Percents(tracks);
		float num3 = 0f;
		foreach (float num4 in array)
		{
			num3 += num4;
		}
		if (Math.Abs(num3 - TotalPercent) > 0.001f)
		{
			list.Add(string.Format("сумма шансов {0} %, а не {1}", num3, TotalPercent));
		}
		return list;
	}

	static MusicPolicy()
	{
		MenuTracks = new MusicTrack[11]
		{
			MusicTrack.Common("Beneath_The_Permafrost"),
			MusicTrack.Common("Beneath_the_Winter_Stone"),
			MusicTrack.Common("Subterranean_Pressure"),
			MusicTrack.Common("Temperature_Of_Stone"),
			MusicTrack.Common("The_Last_Ten_Meters"),
			MusicTrack.Common("March_at_Daybreak"),
			MusicTrack.Common("The_Final_Bearing"),
			MusicTrack.Rare("Sunlight_on_the_Bank", 9f),
			MusicTrack.Rare("Morning_at_the_Hearth", 9f),
			MusicTrack.Rare("Symphony_of_Suns", 1f),
			MusicTrack.Rare("One_Heart_for_the_Stars", 0.01f)
		};
		GameTracks = new MusicTrack[3]
		{
			MusicTrack.Common("Arctic_Lab_Protocol"),
			MusicTrack.Common("Surface_of_the_Deep"),
			MusicTrack.Common("The_Geometry_of_Rest")
		};
	}
}
