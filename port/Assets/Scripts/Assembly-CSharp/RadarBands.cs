using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System;

public static class RadarBands
{
	public const double SpeedOfLightMetersPerSecond = 299792458.0;

	public const double MinimumFrequencyHz = 3000000.0;

	public const double MaximumFrequencyHz = 100000000000.0;

	private static readonly Regex FrequencyPattern;

	static RadarBands()
	{
		FrequencyPattern = new Regex("^\\s*([+-]?(?:\\d+(?:[.,]\\d*)?|[.,]\\d+)(?:[eE][+-]?\\d+)?)\\s*(Hz|kHz|MHz|Гц|кГц|МГц|ГГц)?\\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
	}

	public static string Letter(double frequencyHz)
	{
		if (!IsFinite(frequencyHz) || frequencyHz < 3000000.0 || frequencyHz >= 110000000000.0)
		{
			throw new ArgumentOutOfRangeException("frequencyHz", frequencyHz, "radar.error.band");
		}
		if (frequencyHz < 30000000.0)
		{
			return "HF";
		}
		if (frequencyHz < 300000000.0)
		{
			return "VHF";
		}
		if (frequencyHz < 1000000000.0)
		{
			return "UHF";
		}
		if (frequencyHz < 2000000000.0)
		{
			return "L";
		}
		if (frequencyHz < 4000000000.0)
		{
			return "S";
		}
		if (frequencyHz < 8000000000.0)
		{
			return "C";
		}
		if (frequencyHz < 12000000000.0)
		{
			return "X";
		}
		if (frequencyHz < 18000000000.0)
		{
			return "Ku";
		}
		if (frequencyHz < 27000000000.0)
		{
			return "K";
		}
		if (frequencyHz < 40000000000.0)
		{
			return "Ka";
		}
		if (frequencyHz < 75000000000.0)
		{
			return "V";
		}
		return "W";
	}

	public static bool TryLetter(double frequencyHz, out string letter)
	{
		letter = null;
		if (!IsFinite(frequencyHz) || frequencyHz < 3000000.0 || frequencyHz >= 110000000000.0)
		{
			return false;
		}
		letter = Letter(frequencyHz);
		return true;
	}

	public static double WavelengthMeters(double frequencyHz)
	{
		if (!IsFinite(frequencyHz) || frequencyHz <= 0.0)
		{
			throw new ArgumentOutOfRangeException("frequencyHz", frequencyHz, "radar.error.frequency");
		}
		return SpeedOfLightMetersPerSecond / frequencyHz;
	}

	public static string WaveClassKey(double frequencyHz)
	{
		double num = WavelengthMeters(frequencyHz);
		if (num >= 10.0)
		{
			return "radar.wave.decametric";
		}
		if (num >= 1.0)
		{
			return "radar.wave.metric";
		}
		if (num >= 0.1)
		{
			return "radar.wave.decimetric";
		}
		if (num >= 0.01)
		{
			return "radar.wave.centimetric";
		}
		if (num < 0.001)
		{
			throw new ArgumentOutOfRangeException("frequencyHz", frequencyHz, "radar.error.band");
		}
		return "radar.wave.millimetric";
	}

	public static double SliderToFrequency(double v)
	{
		if (!IsFinite(v))
		{
			throw new ArgumentOutOfRangeException("v", v, "radar.error.frequency");
		}
		if (v <= 0.0)
		{
			return MinimumFrequencyHz;
		}
		if (v >= 1.0)
		{
			return MaximumFrequencyHz;
		}
		return MinimumFrequencyHz * Math.Pow(MaximumFrequencyHz / MinimumFrequencyHz, v);
	}

	public static double FrequencyToSlider(double frequencyHz)
	{
		RequireUserFrequency(frequencyHz);
		return Math.Log(frequencyHz / MinimumFrequencyHz) / Math.Log(MaximumFrequencyHz / MinimumFrequencyHz);
	}

	public static void RequireUserFrequency(double frequencyHz)
	{
		if (!IsFinite(frequencyHz) || frequencyHz < MinimumFrequencyHz || frequencyHz > MaximumFrequencyHz)
		{
			throw new ArgumentOutOfRangeException("frequencyHz", frequencyHz, "radar.error.frequency");
		}
	}

	public static bool TryParseFrequency(string input, out double frequencyHz, out string errorKey)
	{
		frequencyHz = 0.0;
		errorKey = "radar.error.frequency";
		Match match = FrequencyPattern.Match(input ?? string.Empty);
		if (!match.Success)
		{
			return false;
		}
		if (!double.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
		{
			return false;
		}
		string unit = match.Groups[2].Value.ToLowerInvariant();
		double multiplier;
		switch (unit)
		{
		case "мгц":
		case "mhz":
			multiplier = 1000000.0;
			break;
		case "ггц":
		case "ghz":
			multiplier = 1000000000.0;
			break;
		case "кгц":
		case "khz":
			multiplier = 1000.0;
			break;
		case "":
		case "hz":
		case "гц":
			multiplier = 1.0;
			break;
		default:
			return false;
		}
		double num = multiplier * number;
		if (!IsFinite(num) || num < MinimumFrequencyHz || num > MaximumFrequencyHz)
		{
			return false;
		}
		frequencyHz = num;
		errorKey = null;
		return true;
	}

	public static string FormatFrequency(double frequencyHz, string megahertzUnit = "MHz", string gigahertzUnit = "GHz")
	{
		RequireUserFrequency(frequencyHz);
		bool flag = frequencyHz >= 1000000000.0;
		double num = frequencyHz / (flag ? 1000000000.0 : 1000000.0);
		string text = flag ? gigahertzUnit : megahertzUnit;
		num = Math.Round(num, Math.Max(0, 2 - (int)Math.Round(Math.Log10(num))), MidpointRounding.AwayFromZero);
		if (!flag && num >= 1000.0)
		{
			num /= 1000.0;
			text = gigahertzUnit;
		}
		int num2 = Math.Max(0, 2 - (int)Math.Round(Math.Log10(num)));
		return num.ToString("F" + num2, CultureInfo.InvariantCulture) + " " + text;
	}

	private static bool IsFinite(double value)
	{
		return !double.IsNaN(value) && !double.IsInfinity(value);
	}
}
