using System;
using System.Runtime.CompilerServices;
using System.Threading;

public sealed class RadarBenchState
{
	private double azimuthDeg;

	private double elevationDeg;

	private double frequencyHz;

	private string presetId;

	private RadarPolarization polarization;

	private bool viewFromRadar;

	[CompilerGenerated]
	private Action m_Changed;

	public double AzimuthDeg => azimuthDeg;

	public double ElevationDeg => elevationDeg;

	public double FrequencyHz => frequencyHz;

	public string PresetId => presetId;

	public RadarPolarization Polarization => polarization;

	public bool ViewFromRadar => viewFromRadar;

	public RadarDirectionBasis Basis => RadarDirection.BasisDegrees(azimuthDeg, elevationDeg);

	public string BandLetter => RadarBands.Letter(frequencyHz);

	public double WavelengthM => RadarBands.WavelengthMeters(frequencyHz);

	public string WaveClassKey => RadarBands.WaveClassKey(frequencyHz);

	public double FrequencySlider01 => RadarBands.FrequencyToSlider(frequencyHz);

	public event Action Changed
	{
		[CompilerGenerated]
		add
		{
			Action action = m_Changed;
			Action action2;
			do
			{
				action2 = action;
				Action value2 = (Action)Delegate.Combine(action2, value);
				action = Interlocked.CompareExchange(ref m_Changed, value2, action2);
			}
			while (action != action2);
		}
		[CompilerGenerated]
		remove
		{
			Action action = m_Changed;
			Action action2;
			do
			{
				action2 = action;
				Action value2 = (Action)Delegate.Remove(action2, value);
				action = Interlocked.CompareExchange(ref m_Changed, value2, action2);
			}
			while (action != action2);
		}
	}

	public RadarBenchState()
	{
		frequencyHz = RadarCatalog.Default.DefaultFrequencyHz;
		presetId = "x_air";
		polarization = RadarPolarization.VV;
	}

	public void SetDirection(double azimuth, double elevation)
	{
		azimuth = RadarDirection.WrapAzimuth(azimuth);
		elevation = RadarDirection.ClampElevation(elevation);
		if (azimuth != azimuthDeg || elevation != elevationDeg)
		{
			azimuthDeg = azimuth;
			elevationDeg = elevation;
			m_Changed?.Invoke();
		}
	}

	public void SetAzimuth(double azimuth)
	{
		SetDirection(azimuth, elevationDeg);
	}

	public void SetElevation(double elevation)
	{
		SetDirection(azimuthDeg, elevation);
	}

	public void NudgeDirection(double azimuthDeltaDeg, double elevationDeltaDeg)
	{
		SetDirection(azimuthDeg + azimuthDeltaDeg, elevationDeg + elevationDeltaDeg);
	}

	public void SetFrequency(double newFrequencyHz)
	{
		RadarBands.RequireUserFrequency(newFrequencyHz);
		string text = presetId;
		if (RadarCatalog.TryGet(presetId, out RadarPreset preset) && (newFrequencyHz < preset.MinimumHz || newFrequencyHz > preset.MaximumHz || RadarBands.Letter(newFrequencyHz) != preset.BandLetter))
		{
			text = "custom";
		}
		if (newFrequencyHz != frequencyHz || !string.Equals(text, presetId, StringComparison.Ordinal))
		{
			frequencyHz = newFrequencyHz;
			presetId = text;
			m_Changed?.Invoke();
		}
	}

	public void SetFrequencySlider(double slider01)
	{
		if (Math.Abs(slider01 - FrequencySlider01) > 1E-07)
		{
			SetFrequency(RadarBands.SliderToFrequency(slider01));
		}
	}

	public bool TrySetFrequencyText(string input, out string errorKey)
	{
		if (RadarBands.TryParseFrequency(input, out double num, out errorKey))
		{
			SetFrequency(num);
			return true;
		}
		return false;
	}

	public void SelectPreset(string id)
	{
		if (!string.Equals(id, "custom", StringComparison.Ordinal))
		{
			if (!RadarCatalog.TryGet(id, out RadarPreset preset))
			{
				throw new ArgumentException("radar.error.preset", "id");
			}
			if (!string.Equals(presetId, id, StringComparison.Ordinal) || frequencyHz != preset.DefaultFrequencyHz)
			{
				presetId = id;
				frequencyHz = preset.DefaultFrequencyHz;
				m_Changed?.Invoke();
			}
		}
		else if (string.Equals(presetId, id, StringComparison.Ordinal))
		{
			return;
		}
		else
		{
			presetId = id;
			m_Changed?.Invoke();
		}
	}

	public void SetPolarization(RadarPolarization selected)
	{
		if ((uint)selected > 3u)
		{
			throw new ArgumentOutOfRangeException("selected", selected, "radar.error.polarization");
		}
		if (selected != polarization)
		{
			polarization = selected;
			m_Changed?.Invoke();
		}
	}

	public void SetViewFromRadar(bool enabled)
	{
		if (enabled != viewFromRadar)
		{
			viewFromRadar = enabled;
			m_Changed?.Invoke();
		}
	}
}
