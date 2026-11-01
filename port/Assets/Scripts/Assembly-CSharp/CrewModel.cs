using System.Text;
using UnityEngine;

public static class CrewModel
{
	public const float MaxHitPoints = 100f;

	public const float HpPerJoule = 1f;

	public const float HeadMultiplier = 3f;

	public const float TorsoMultiplier = 1f;

	public const float LimbMultiplier = 0.5f;

	public static float ZoneMultiplier(CrewZone z)
	{
		if (z == CrewZone.Head)
		{
			return HeadMultiplier;
		}
		if (z == CrewZone.Limbs)
		{
			return LimbMultiplier;
		}
		return TorsoMultiplier;
	}

	public static string ZoneName(CrewZone z)
	{
		if (z == CrewZone.Head)
		{
			return "голова";
		}
		if (z == CrewZone.Limbs)
		{
			return "конечности";
		}
		return "корпус";
	}

	public static CrewZone ZoneAtHeightFraction(float f)
	{
		if (f >= 0.82f)
		{
			return CrewZone.Head;
		}
		if (f >= 0.45f)
		{
			return CrewZone.Torso;
		}
		return CrewZone.Limbs;
	}

	public static float DamageFrom(float depositedJ, CrewZone zone)
	{
		return Mathf.Max(0f, depositedJ) * ZoneMultiplier(zone);
	}

	public static float DamageFromOrgan(float depositedJ, CrewOrgan organ, CrewTissue tissue)
	{
		return Mathf.Max(0f, depositedJ) * CrewAnatomy.Criticality(organ, tissue);
	}

	public static CrewState StateOf(float hitPoints)
	{
		if (hitPoints <= 0f)
		{
			return CrewState.Incapacitated;
		}
		if (hitPoints < 99.9999f)
		{
			return CrewState.Injured;
		}
		return CrewState.Unharmed;
	}

	public static string StateName(CrewState s)
	{
		if (s == CrewState.Injured)
		{
			return "ЕСТЬ ПОПАДАНИЙ";
		}
		if (s == CrewState.Incapacitated)
		{
			return "ВЫВЕДЕН ИЗ СТРОЯ";
		}
		return "БЕЗ ПОПАДАНИЙ";
	}

	public static Color StateColor(CrewState s)
	{
		if (s == CrewState.Injured)
		{
			return ResultPalette.Slide;
		}
		if (s == CrewState.Incapacitated)
		{
			return ResultPalette.Stopped;
		}
		return ResultPalette.Miss;
	}

	public static string Report(int hits, float depositedJ, float hitPoints)
	{
		string text = string.Format("Экипаж: попаданий {0}, поглощено {1:F0} Дж ⇒ запас {2:F0}/", hits, Mathf.Max(0f, depositedJ), Mathf.Max(0f, hitPoints));
		string text2 = string.Format("{0:F0} ⇒ «{1}». ", 100f, StateName(StateOf(hitPoints)));
		return text + text2 + "⚠ Модель РЕГИСТРИРУЕТ попадания и энергию по органам и НЕ предсказывает поражение: раневой баллистики в проекте нет, все пороги ASSUMED без источника.";
	}

	public static string OrganBreakdown(float[] organJ)
	{
		if (organJ == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 1; i < 17; i++)
		{
			if (0.05f >= organJ[i])
			{
				continue;
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(CrewAnatomy.OrganName((CrewOrgan)i)).Append(' ').Append(organJ[i].ToString("F0")).Append(" Дж");
		}
		return stringBuilder.ToString();
	}
}
