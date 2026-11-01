using UnityEngine;

public static class BodyTraceText
{
	public static string Describe(SegmentKind kind)
	{
		switch (kind)
		{
		case SegmentKind.Void:
			return "пустота";
		case SegmentKind.Material:
			return "вещество";
		default:
			Debug.LogError(string.Format("[BodyTraceText] неизвестный вид отрезка {0}; не назван.", kind));
			return null;
		}
	}

	public static string Describe(EntrySurface entry)
	{
		switch (entry)
		{
		case EntrySurface.Unknown:
			return "не определена";
		case EntrySurface.Front:
			return "лицевая";
		case EntrySurface.Rear:
			return "тыльная";
		case EntrySurface.Edge:
			return "кромка";
		case EntrySurface.Internal:
			return "внутренняя";
		default:
			Debug.LogError(string.Format("[BodyTraceText] неизвестная поверхность входа {0}; не названа.", entry));
			return null;
		}
	}

	public static string Describe(BodyRejectCode code)
	{
		switch (code)
		{
		case BodyRejectCode.None:
			return "отказа нет";
		case BodyRejectCode.BodyNotLast:
			return "тело не последнее в сцене";
		case BodyRejectCode.BodyMultiple:
			return "тел в сцене больше одного";
		case BodyRejectCode.PlacementOverlap:
			return "размещения пересекаются вдоль выстрела";
		case BodyRejectCode.NegativeStandoff:
			return "отрицательный зазор";
		case BodyRejectCode.DegenerateObject:
			return "вырожденный предмет";
		case BodyRejectCode.DirectionNotNormalised:
			return "направление не единичной длины";
		default:
			Debug.LogError(string.Format("[BodyTraceText] неизвестный код отказа {0}; не назван.", code));
			return null;
		}
	}
}
