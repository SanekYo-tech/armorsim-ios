using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class GameSettings
{
	private const string DefaultModeKey = "armorsim.default_mode";

	[CompilerGenerated]
	private static Action m_OnDefaultModeChanged;

	private const string ResultTintKey = "armorsim.result_tint";

	private const string TracePathKey = "armorsim.trace_path";

	private const string TexturesKey = "armorsim.textures";

	private const string ExtremeTiltKey = "armorsim.extreme_tilt";

	private const string MusicKey = "armorsim.music";

	private const string CrewOrgansKey = "armorsim.crew_organs";

	private const string CrewDamageHighlightKey = "armorsim.crew_damage_highlight";

	[CompilerGenerated]
	private static Action m_OnDisplayChanged;

	private static bool _resultTint;

	private static bool _tracePath;

	private static bool _textures;

	private static bool _extremeTilt;

	private static bool _music;

	private static bool _crewOrgans;

	private static bool _crewDamageHighlight;

	private static bool _displayLoaded;

	public const float TiltLimitSafeDeg = 75f;

	public const float TiltLimitExtremeDeg = 90f;

	[CompilerGenerated]
	private static Action m_OnLimitsChanged;

	public static readonly SimMode[] AllModes;

	private static SimMode _defaultMode;

	private static bool _loaded;

	public static bool CrewOrgans
	{
		get
		{
			LoadDisplay();
			return _crewOrgans;
		}
	}

	public static bool CrewDamageHighlight
	{
		get
		{
			LoadDisplay();
			return _crewDamageHighlight;
		}
	}

	public static bool Music
	{
		get
		{
			LoadDisplay();
			return _music;
		}
	}

	public static bool AllowExtremeTilt
	{
		get
		{
			LoadDisplay();
			return _extremeTilt;
		}
	}

	public static float TiltLimitDeg => AllowExtremeTilt ? TiltLimitExtremeDeg : TiltLimitSafeDeg;

	public static bool ResultTint
	{
		get
		{
			LoadDisplay();
			return _resultTint;
		}
	}

	public static bool TracePath
	{
		get
		{
			LoadDisplay();
			return _tracePath;
		}
	}

	public static bool Textures
	{
		get
		{
			LoadDisplay();
			return _textures;
		}
	}

	public static SimMode DefaultMode
	{
		get
		{
			if (!_loaded)
			{
				_loaded = true;
				int mode = PlayerPrefs.GetInt(DefaultModeKey, 0);
				if (mode < 0 || mode > 3)
				{
					mode = 0;
				}
				_defaultMode = (SimMode)mode;
			}
			return _defaultMode;
		}
	}

	public static event Action OnDefaultModeChanged
	{
		[CompilerGenerated]
		add
		{
			m_OnDefaultModeChanged = (Action)Delegate.Combine(m_OnDefaultModeChanged, value);
		}
		[CompilerGenerated]
		remove
		{
			m_OnDefaultModeChanged = (Action)Delegate.Remove(m_OnDefaultModeChanged, value);
		}
	}

	public static event Action OnDisplayChanged
	{
		[CompilerGenerated]
		add
		{
			m_OnDisplayChanged = (Action)Delegate.Combine(m_OnDisplayChanged, value);
		}
		[CompilerGenerated]
		remove
		{
			m_OnDisplayChanged = (Action)Delegate.Remove(m_OnDisplayChanged, value);
		}
	}

	public static event Action OnLimitsChanged
	{
		[CompilerGenerated]
		add
		{
			m_OnLimitsChanged = (Action)Delegate.Combine(m_OnLimitsChanged, value);
		}
		[CompilerGenerated]
		remove
		{
			m_OnLimitsChanged = (Action)Delegate.Remove(m_OnLimitsChanged, value);
		}
	}

	private static void LoadDisplay()
	{
		if (_displayLoaded)
		{
			return;
		}
		_displayLoaded = true;
		_resultTint = PlayerPrefs.GetInt(ResultTintKey, 1) != 0;
		_tracePath = PlayerPrefs.GetInt(TracePathKey, 1) != 0;
		_textures = PlayerPrefs.GetInt(TexturesKey, 1) != 0;
		_extremeTilt = PlayerPrefs.GetInt(ExtremeTiltKey, 0) != 0;
		_music = PlayerPrefs.GetInt(MusicKey, 1) != 0;
		_crewOrgans = PlayerPrefs.GetInt(CrewOrgansKey, 0) != 0;
		_crewDamageHighlight = PlayerPrefs.GetInt(CrewDamageHighlightKey, 1) != 0;
	}

	public static void SetCrewOrgans(bool v)
	{
		SetFlag(CrewOrgansKey, v, ref _crewOrgans);
	}

	public static void SetCrewDamageHighlight(bool v)
	{
		SetFlag(CrewDamageHighlightKey, v, ref _crewDamageHighlight);
	}

	public static void SetMusic(bool value)
	{
		LoadDisplay();
		if (_music == value)
		{
			return;
		}
		_music = value;
		PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
		PlayerPrefs.Save();
	}

	public static void SetAllowExtremeTilt(bool value)
	{
		LoadDisplay();
		if (_extremeTilt == value)
		{
			return;
		}
		_extremeTilt = value;
		PlayerPrefs.SetInt(ExtremeTiltKey, value ? 1 : 0);
		PlayerPrefs.Save();
		m_OnLimitsChanged?.Invoke();
	}

	public static void SetResultTint(bool v)
	{
		SetFlag(ResultTintKey, v, ref _resultTint);
	}

	public static void SetTracePath(bool v)
	{
		SetFlag(TracePathKey, v, ref _tracePath);
	}

	public static void SetTextures(bool v)
	{
		SetFlag(TexturesKey, v, ref _textures);
	}

	private static void SetFlag(string key, bool value, ref bool field)
	{
		LoadDisplay();
		if (field == value)
		{
			return;
		}
		field = value;
		PlayerPrefs.SetInt(key, value ? 1 : 0);
		PlayerPrefs.Save();
		m_OnDisplayChanged?.Invoke();
	}

	public static void SetDefaultMode(SimMode mode)
	{
		if (mode < SimMode.Normal || mode > SimMode.Super)
		{
			return;
		}
		SimMode old = DefaultMode;
		if (old == mode)
		{
			return;
		}
		_defaultMode = mode;
		PlayerPrefs.SetInt(DefaultModeKey, (int)mode);
		PlayerPrefs.Save();
		m_OnDefaultModeChanged?.Invoke();
	}

	public static bool IsUnlocked(SimMode mode)
	{
		return mode >= SimMode.Normal && mode <= SimMode.Super;
	}

	public static bool SupportsSection(SimMode mode)
	{
		return mode == SimMode.Normal || mode == SimMode.Advanced;
	}

	public static string SectionKey(TargetSection section)
	{
		return section == TargetSection.Roof ? "section.roof" : "section.hull";
	}

	public static string TitleKey(SimMode mode)
	{
		switch (mode)
		{
		case SimMode.Advanced:
			return "mode.advanced";
		case SimMode.Ultra:
			return "mode.ultra";
		case SimMode.Super:
			return "mode.super";
		default:
			return "mode.normal";
		}
	}

	public static string DescKey(SimMode mode)
	{
		switch (mode)
		{
		case SimMode.Advanced:
			return "mode.advanced_desc";
		case SimMode.Ultra:
			return "mode.ultra_desc";
		case SimMode.Super:
			return "mode.super_desc";
		default:
			return "mode.normal_desc";
		}
	}

	static GameSettings()
	{
		_resultTint = true;
		_tracePath = true;
		_textures = true;
		_music = true;
		_crewDamageHighlight = true;
		AllModes = new SimMode[4]
		{
			SimMode.Normal,
			SimMode.Advanced,
			SimMode.Ultra,
			SimMode.Super
		};
		_defaultMode = SimMode.Normal;
	}
}
