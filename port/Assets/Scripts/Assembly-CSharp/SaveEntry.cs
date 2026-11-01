using System;
using System.Globalization;

[Serializable]
public class SaveEntry
{
	public string id;

	public string displayName;

	public string createdUtc;

	public int objectCount;

	public int payloadVersion;

	public string payload;

	public bool isPreset = true;

	public string section = "Hull";

	public string family = "Analytic3D";

	public TargetSection Section
	{
		get
		{
			if (!Enum.TryParse(section, false, out TargetSection value))
			{
				return TargetSection.Hull;
			}
			return value;
		}
	}

	public SimFamily Family
	{
		get
		{
			if (!Enum.TryParse(family, false, out SimFamily value))
			{
				return SimFamily.Analytic3D;
			}
			return value;
		}
	}

	public DateTime CreatedLocal()
	{
		if (!DateTime.TryParse(createdUtc, null, DateTimeStyles.RoundtripKind, out DateTime parsed))
		{
			return DateTime.MinValue;
		}
		return parsed.ToLocalTime();
	}
}
