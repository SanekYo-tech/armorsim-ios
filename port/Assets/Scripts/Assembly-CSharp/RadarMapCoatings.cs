using System;
using System.Globalization;

public static class RadarMapCoatings
{
	public const double EpsReMin = -10.0;

	public const double EpsReMax = 20.0;

	public const double MuReMin = -5.0;

	public const double MuReMax = 5.0;

	public const double LossMax = 20.0;

	public const double ThicknessMinM = 0.0001;

	public const double ThicknessMaxM = 0.05;

	public const double F0MinHz = 100000000.0;

	public const double F0MaxHz = 40000000000.0;

	public static readonly float[] DefaultCustom = new float[5] { 10f, 1f, 2f, 1.5f, 0.002f };

	public static bool CustomInRange(float[] layer, out string reason)
	{
		reason = null;
		if (layer == null || layer.Length != 5)
		{
			reason = "5 numbers";
			return false;
		}
		for (int i = 0; i < layer.Length; i++)
		{
			if (!float.IsFinite(layer[i]))
			{
				reason = "finite";
				return false;
			}
		}
		if (layer[0] < EpsReMin || layer[0] > EpsReMax)
		{
			reason = "ε′ [−10, 20]";
			return false;
		}
		if (layer[1] < 0.0 || layer[1] > LossMax)
		{
			reason = "ε″ [0, 20]";
			return false;
		}
		if (layer[2] < MuReMin || layer[2] > MuReMax)
		{
			reason = "μ′ [−5, 5]";
			return false;
		}
		if (layer[3] < 0.0 || layer[3] > LossMax)
		{
			reason = "μ″ [0, 20]";
			return false;
		}
		if (layer[4] < ThicknessMinM || layer[4] > ThicknessMaxM)
		{
			reason = "d [0.1, 50] mm";
			return false;
		}
		return true;
	}

	public static bool F0InRange(double f0)
	{
		return f0 >= F0MinHz && f0 <= F0MaxHz;
	}

	public static RadarCoating FromAircraft(RadarScenarioAircraft aircraft, out string refusalReason)
	{
		refusalReason = null;
		if (aircraft.PhysicalCoatingId == "metal")
		{
			return RadarCoating.Metal;
		}
		if (aircraft.PhysicalCoatingId == "salisbury" || aircraft.PhysicalCoatingId == "jaumann")
		{
			if (!F0InRange(aircraft.CoatingF0Hz))
			{
				refusalReason = "f0 [0.1, 40] GHz";
				return null;
			}
			if (aircraft.PhysicalCoatingId == "salisbury")
			{
				return RadarCoating.Salisbury(aircraft.CoatingF0Hz);
			}
			return RadarCoating.Jaumann(aircraft.CoatingF0Hz);
		}
		if (aircraft.PhysicalCoatingId != "custom")
		{
			refusalReason = "unknown coating " + aircraft.PhysicalCoatingId;
			return null;
		}
		if (!CustomInRange(aircraft.CustomLayer, out refusalReason))
		{
			return null;
		}
		return RadarCoating.Custom(aircraft.CustomLayer[0], aircraft.CustomLayer[1], aircraft.CustomLayer[2], aircraft.CustomLayer[3], aircraft.CustomLayer[4]);
	}

	public static string Name(RadarCoating coating)
	{
		if (coating == null || coating.IsMetal)
		{
			return RadarScenarioText.Get("map.coat.metal");
		}
		double num = coating.F0Hz / 1000000000.0;
		string text = num.ToString("0.###", CultureInfo.InvariantCulture);
		if (coating.Id == RadarCoating.Kind.Salisbury)
		{
			return string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.coat.salisbury"), text);
		}
		if (coating.Id == RadarCoating.Kind.Jaumann)
		{
			return string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.coat.jaumann"), text);
		}
		return RadarScenarioText.Get("map.coat.custom");
	}

	public static double NormalDb(RadarCoating coating, double frequencyHz)
	{
		coating.Gamma(frequencyHz * 6.2831853071795862 / 299792458.0, 1.0, out Complex64 te, out Complex64 tm);
		double magnitudeSquared = te.MagnitudeSquared;
		if (magnitudeSquared > 0.0)
		{
			return Math.Log10(magnitudeSquared) * 10.0;
		}
		return -400.0;
	}
}
