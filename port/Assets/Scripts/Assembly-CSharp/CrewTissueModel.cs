public static class CrewTissueModel
{
	public const float DragCoefficient = 1f;

	public static float DensityKgM3(CrewTissue t)
	{
		switch (t)
		{
		case CrewTissue.Skin:
			return 1100f;
		case CrewTissue.Fat:
			return 920f;
		case CrewTissue.Muscle:
			return 1060f;
		case CrewTissue.Bone:
			return 1600f;
		case CrewTissue.Viscera:
			return 1060f;
		case CrewTissue.Lung:
			return 300f;
		case CrewTissue.Brain:
			return 1040f;
		}
		return 1060f;
	}

	public static float StrengthPa(CrewTissue t)
	{
		switch (t)
		{
		case CrewTissue.Skin:
			return 22000000f;
		case CrewTissue.Fat:
			return 250000f;
		case CrewTissue.Muscle:
			return 1000000f;
		case CrewTissue.Bone:
			return 90000000f;
		case CrewTissue.Viscera:
			return 600000f;
		case CrewTissue.Lung:
			return 200000f;
		case CrewTissue.Brain:
			return 150000f;
		}
		return 1000000f;
	}

	public static string Name(CrewTissue t)
	{
		switch (t)
		{
		case CrewTissue.Skin:
			return "кожа";
		case CrewTissue.Fat:
			return "жир";
		case CrewTissue.Muscle:
			return "мышца";
		case CrewTissue.Bone:
			return "кость";
		case CrewTissue.Viscera:
			return "внутренний орган";
		case CrewTissue.Lung:
			return "лёгкое";
		case CrewTissue.Brain:
			return "мозг";
		}
		return "ткань";
	}
}
