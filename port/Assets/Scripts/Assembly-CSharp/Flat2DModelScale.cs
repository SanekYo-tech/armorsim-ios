using System.Globalization;

public static class Flat2DModelScale
{
	public const float StandingOnCanvasM = 0.118f;

	private static float s = -1f;

	public static float S
	{
		get
		{
			if (s < 0f)
			{
				s = StandingOnCanvasM / Flat2DCrew.NaturalHeightM(standing: true);
			}
			return s;
		}
	}

	public static string Label => "1:" + (1f / S).ToString("F1", CultureInfo.InvariantCulture);
}
