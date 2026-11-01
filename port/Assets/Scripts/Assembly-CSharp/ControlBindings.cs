using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class ControlBindings
{
	public sealed class KeyEntry
	{
		public readonly ControlAction action;

		public readonly ControlScope scope;

		public readonly KeyCode def;

		public readonly string titleKey;

		public readonly string hintKey;

		public KeyEntry(ControlAction a, ControlScope s, KeyCode d, string t, string h)
		{
			action = a;
			scope = s;
			def = d;
			titleKey = t;
			hintKey = h;
		}
	}

	public sealed class ModEntry
	{
		public readonly ControlModifier id;

		public readonly ControlScope scope;

		public readonly ModifierKind def;

		public readonly string channel;

		public readonly string titleKey;

		public readonly string hintKey;

		public ModEntry(ControlModifier i, ControlScope s, ModifierKind d, string c, string t, string h)
		{
			id = i;
			scope = s;
			def = d;
			channel = c;
			titleKey = t;
			hintKey = h;
		}
	}

	public sealed class GestureEntry
	{
		public readonly ControlScope scope;

		public readonly string titleKey;

		private readonly Func<string> value;

		public string Value => value == null ? string.Empty : value();

		public GestureEntry(ControlScope s, string t, Func<string> v)
		{
			scope = s;
			titleKey = t;
			value = v;
		}
	}

	public static readonly KeyEntry[] Keys = new KeyEntry[]
	{
		new KeyEntry(ControlAction.FrameCamera, ControlScope.Analytic3D, KeyCode.F, "controls.act.frame", "controls.act.frame_hint"),
		new KeyEntry(ControlAction.SectionToggle, ControlScope.Analytic3D, KeyCode.C, "controls.act.section", "controls.act.section_hint"),
		new KeyEntry(ControlAction.SectionReface, ControlScope.Analytic3D, KeyCode.V, "controls.act.reface", "controls.act.reface_hint"),
		new KeyEntry(ControlAction.CrewOrgans, ControlScope.Analytic3D, KeyCode.O, "controls.act.organs", "controls.act.organs_hint"),
		new KeyEntry(ControlAction.GizmoGrab, ControlScope.Analytic3D, KeyCode.G, "controls.act.grab", "controls.act.grab_hint"),
		new KeyEntry(ControlAction.GizmoAxisX, ControlScope.Analytic3D, KeyCode.X, "controls.act.axis_x", "controls.act.axis_hint"),
		new KeyEntry(ControlAction.GizmoAxisY, ControlScope.Analytic3D, KeyCode.Y, "controls.act.axis_y", "controls.act.axis_hint"),
		new KeyEntry(ControlAction.GizmoAxisZ, ControlScope.Analytic3D, KeyCode.Z, "controls.act.axis_z", "controls.act.axis_hint"),
		new KeyEntry(ControlAction.DuplicateSelection, ControlScope.Analytic3D, KeyCode.D, "controls.act.duplicate", "controls.act.duplicate_hint")
	};

	public static readonly ModEntry[] Modifiers = new ModEntry[]
	{
		new ModEntry(ControlModifier.CameraPan, ControlScope.Analytic3D, ModifierKind.Shift, "camera3d", "controls.mod.pan", "controls.mod.pan_hint"),
		new ModEntry(ControlModifier.SelectAdd, ControlScope.Analytic3D, ModifierKind.Shift, "select3d", "controls.mod.select_add", "controls.mod.select_add_hint"),
		new ModEntry(ControlModifier.GizmoSnap, ControlScope.Analytic3D, ModifierKind.Ctrl, "gizmo3d", "controls.mod.snap", "controls.mod.snap_hint"),
		new ModEntry(ControlModifier.FlatBrushSize, ControlScope.Flat2D, ModifierKind.Shift, "wheel2d", "controls.mod.brush", "controls.mod.brush_hint"),
		new ModEntry(ControlModifier.FlatPrecise, ControlScope.Flat2D, ModifierKind.Alt, "wheel2d", "controls.mod.precise", "controls.mod.precise_hint")
	};

	public static readonly GestureEntry[] Gestures = new GestureEntry[]
	{
		new GestureEntry(ControlScope.Analytic3D, "controls.g3.select", () => MouseName(0)),
		new GestureEntry(ControlScope.Analytic3D, "controls.g3.select_add", () => ModifierName(Modifier(ControlModifier.SelectAdd)) + " + " + MouseName(0)),
		new GestureEntry(ControlScope.Analytic3D, "controls.g3.drag", () => MouseName(0)),
		new GestureEntry(ControlScope.Analytic3D, "controls.g3.orbit", () => MouseName(OrbitMouseButton)),
		new GestureEntry(ControlScope.Analytic3D, "controls.g3.pan", () => ModifierName(Modifier(ControlModifier.CameraPan)) + " + " + MouseName(OrbitMouseButton)),
		new GestureEntry(ControlScope.Analytic3D, "controls.g3.zoom", WheelName),
		new GestureEntry(ControlScope.Analytic3D, "controls.g3.apply", () => KeyName(KeyCode.Return) + " / " + MouseName(0)),
		new GestureEntry(ControlScope.Analytic3D, "controls.g3.cancel", () => GizmoCancelOnRightButton ? KeyName(KeyCode.Escape) + " / " + MouseName(1) : KeyName(KeyCode.Escape)),
		new GestureEntry(ControlScope.Analytic3D, "controls.g3.typed", () => "0-9   -   " + KeyName(KeyCode.Backspace)),
		new GestureEntry(ControlScope.Analytic3D, "controls.g3.pause", () => KeyName(KeyCode.Escape)),
		new GestureEntry(ControlScope.Flat2D, "controls.g2.point", () => MouseName(0)),
		new GestureEntry(ControlScope.Flat2D, "controls.g2.paint", () => MouseName(0)),
		new GestureEntry(ControlScope.Flat2D, "controls.g2.pan", () => MouseName(1) + " / " + MouseName(2)),
		new GestureEntry(ControlScope.Flat2D, "controls.g2.zoom", WheelName),
		new GestureEntry(ControlScope.Flat2D, "controls.g2.rotate", WheelName),
		new GestureEntry(ControlScope.Flat2D, "controls.g2.menu", () => KeyName(KeyCode.Escape))
	};

	public static readonly ControlScope[] AllScopes = new ControlScope[]
	{
		ControlScope.Analytic3D,
		ControlScope.Flat2D
	};

	private const string KeyPrefix = "armorsim.input.key.";

	private const string ModPrefix = "armorsim.input.mod.";

	private const string OrbitKey = "armorsim.input.orbit_right";

	private const string WheelPrefix = "armorsim.input.invert_wheel.";

	[CompilerGenerated]
	private static Action m_OnChanged;

	private static readonly Dictionary<ControlAction, KeyCode> _keys = new Dictionary<ControlAction, KeyCode>();

	private static readonly Dictionary<ControlModifier, ModifierKind> _mods = new Dictionary<ControlModifier, ModifierKind>();

	private static bool _orbitRight;

	private static bool _invert3D;

	private static bool _invert2D;

	private static bool _loaded;

	private static KeyCode[] _assignable;

	private static HashSet<KeyCode> _assignableSet;

	private static readonly KeyCode[] ReservedKeys = new KeyCode[]
	{
		KeyCode.Escape,
		KeyCode.Return,
		KeyCode.KeypadEnter,
		KeyCode.Backspace,
		KeyCode.Delete,
		KeyCode.LeftShift,
		KeyCode.RightShift,
		KeyCode.LeftControl,
		KeyCode.RightControl,
		KeyCode.LeftAlt,
		KeyCode.RightAlt,
		KeyCode.Mouse0,
		KeyCode.Mouse1,
		KeyCode.Mouse2,
		KeyCode.Mouse3,
		KeyCode.Mouse4,
		KeyCode.Mouse5,
		KeyCode.Mouse6
	};

	private static KeyCode[] _capture;

	public static readonly ModifierKind[] AllModifierKinds = new ModifierKind[]
	{
		ModifierKind.Shift,
		ModifierKind.Ctrl,
		ModifierKind.Alt
	};

	public static bool OrbitOnRight
	{
		get
		{
			EnsureLoaded();
			return _orbitRight;
		}
	}

	public static int OrbitMouseButton
	{
		get
		{
			EnsureLoaded();
			return OrbitOnRight ? 1 : 2;
		}
	}

	public static bool GizmoCancelOnRightButton
	{
		get
		{
			EnsureLoaded();
			return !OrbitOnRight;
		}
	}

	public static KeyCode[] Assignable
	{
		get
		{
			if (_assignable != null)
			{
				return _assignable;
			}
			List<KeyCode> keys = new List<KeyCode>();
			for (KeyCode key = KeyCode.A; key <= KeyCode.Z; key++)
			{
				keys.Add(key);
			}
			for (KeyCode key = KeyCode.F1; key <= KeyCode.F12; key++)
			{
				keys.Add(key);
			}
			keys.Add(KeyCode.Space);
			keys.Add(KeyCode.Tab);
			keys.Add(KeyCode.Insert);
			keys.Add(KeyCode.Home);
			keys.Add(KeyCode.End);
			keys.Add(KeyCode.PageUp);
			keys.Add(KeyCode.PageDown);
			keys.Add(KeyCode.UpArrow);
			keys.Add(KeyCode.DownArrow);
			keys.Add(KeyCode.LeftArrow);
			keys.Add(KeyCode.RightArrow);
			keys.Add(KeyCode.BackQuote);
			keys.Add(KeyCode.LeftBracket);
			keys.Add(KeyCode.RightBracket);
			keys.Add(KeyCode.Semicolon);
			keys.Add(KeyCode.Quote);
			keys.Add(KeyCode.Comma);
			keys.Add(KeyCode.Slash);
			keys.Add(KeyCode.Backslash);
			_assignable = keys.ToArray();
			return _assignable;
		}
	}

	public static KeyCode[] CaptureCandidates
	{
		get
		{
			if (_capture == null)
			{
				List<KeyCode> keys = new List<KeyCode>(Assignable);
				keys.AddRange(ReservedKeys);
				_capture = keys.ToArray();
			}
			return _capture;
		}
	}

	public static event Action OnChanged
	{
		[CompilerGenerated]
		add
		{
			Action current = m_OnChanged;
			Action comparand;
			do
			{
				comparand = current;
				current = System.Threading.Interlocked.CompareExchange(ref m_OnChanged, (Action)Delegate.Combine(comparand, value), comparand);
			}
			while (current != comparand);
		}
		[CompilerGenerated]
		remove
		{
			Action current = m_OnChanged;
			Action comparand;
			do
			{
				comparand = current;
				current = System.Threading.Interlocked.CompareExchange(ref m_OnChanged, (Action)Delegate.Remove(comparand, value), comparand);
			}
			while (current != comparand);
		}
	}

	public static string TitleKey(ControlScope s)
	{
		return s == ControlScope.Flat2D ? "controls.scope.flat" : "controls.scope.sandbox";
	}

	public static string HintKey(ControlScope s)
	{
		return s == ControlScope.Flat2D ? "controls.scope.flat_hint" : "controls.scope.sandbox_hint";
	}

	private static void EnsureLoaded()
	{
		if (_loaded)
		{
			return;
		}
		_loaded = true;
		for (int i = 0; i < Keys.Length; i++)
		{
			KeyEntry entry = Keys[i];
			_keys[entry.action] = ReadKey(KeyPrefix + entry.action, entry.def);
		}
		for (int j = 0; j < Modifiers.Length; j++)
		{
			ModEntry entry = Modifiers[j];
			int kind = PlayerPrefs.GetInt(ModPrefix + entry.id, (int)entry.def);
			if (kind < 0)
			{
				kind = 0;
			}
			else if (kind > 2)
			{
				kind = 2;
			}
			_mods[entry.id] = (ModifierKind)kind;
		}
		_orbitRight = PlayerPrefs.GetInt(OrbitKey, 0) != 0;
		_invert3D = PlayerPrefs.GetInt(WheelPrefix + ControlScope.Analytic3D, 0) != 0;
		_invert2D = PlayerPrefs.GetInt(WheelPrefix + ControlScope.Flat2D, 0) != 0;
	}

	private static KeyCode ReadKey(string pref, KeyCode def)
	{
		KeyCode key = (KeyCode)PlayerPrefs.GetInt(pref, (int)def);
		return IsAssignable(key) ? key : def;
	}

	public static KeyCode Key(ControlAction a)
	{
		EnsureLoaded();
		KeyCode key;
		return _keys.TryGetValue(a, out key) ? key : DefaultKey(a);
	}

	public static KeyCode DefaultKey(ControlAction a)
	{
		for (int i = 0; i < Keys.Length; i++)
		{
			if (Keys[i].action == a)
			{
				return Keys[i].def;
			}
		}
		return KeyCode.None;
	}

	public static ModifierKind Modifier(ControlModifier m)
	{
		EnsureLoaded();
		ModifierKind kind;
		return _mods.TryGetValue(m, out kind) ? kind : DefaultModifier(m);
	}

	public static ModifierKind DefaultModifier(ControlModifier m)
	{
		for (int i = 0; i < Modifiers.Length; i++)
		{
			if (Modifiers[i].id == m)
			{
				return Modifiers[i].def;
			}
		}
		return ModifierKind.Shift;
	}

	public static bool Held(ControlModifier m)
	{
		return Held(Modifier(m));
	}

	public static bool Held(ModifierKind kind)
	{
		if (kind == ModifierKind.Ctrl)
		{
			return Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl);
		}
		if (kind == ModifierKind.Alt)
		{
			return Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.LeftAlt);
		}
		return Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift);
	}

	public static bool InvertWheel(ControlScope s)
	{
		EnsureLoaded();
		return s == ControlScope.Flat2D ? _invert2D : _invert3D;
	}

	public static float WheelSign(ControlScope s)
	{
		return InvertWheel(s) ? -1f : 1f;
	}

	public static bool TryAssign(ControlAction a, KeyCode k, out string refusalKey, out ControlAction conflict)
	{
		EnsureLoaded();
		refusalKey = null;
		conflict = ControlAction.None;
		if (a == ControlAction.None)
		{
			refusalKey = "controls.refuse.unknown";
			return false;
		}
		if (!IsAssignable(k))
		{
			refusalKey = (IsReserved(k) ? "controls.refuse.reserved" : "controls.refuse.unsupported");
			return false;
		}
		ControlScope scope = ScopeOf(a);
		for (int i = 0; i < Keys.Length; i++)
		{
			KeyEntry entry = Keys[i];
			if (entry.action != a && entry.scope == scope && Key(entry.action) == k)
			{
				conflict = entry.action;
				refusalKey = "controls.refuse.conflict";
				return false;
			}
		}
		KeyCode old;
		if (!_keys.TryGetValue(a, out old) || old != k)
		{
			_keys[a] = k;
			PlayerPrefs.SetInt(KeyPrefix + a, (int)k);
			PlayerPrefs.Save();
			m_OnChanged?.Invoke();
		}
		return true;
	}

	public static bool TryAssignModifier(ControlModifier m, ModifierKind kind, out string refusalKey, out ControlModifier conflict)
	{
		EnsureLoaded();
		refusalKey = null;
		conflict = ControlModifier.None;
		ModEntry target = EntryOf(m);
		if (target == null)
		{
			refusalKey = "controls.refuse.unknown";
			return false;
		}
		for (int i = 0; i < Modifiers.Length; i++)
		{
			ModEntry entry = Modifiers[i];
			if (entry.id != m && entry.scope == target.scope && entry.channel == target.channel && Modifier(entry.id) == kind)
			{
				conflict = entry.id;
				refusalKey = "controls.refuse.conflict";
				return false;
			}
		}
		ModifierKind old;
		if (!_mods.TryGetValue(m, out old) || old != kind)
		{
			_mods[m] = kind;
			PlayerPrefs.SetInt(ModPrefix + m, (int)kind);
			PlayerPrefs.Save();
			m_OnChanged?.Invoke();
		}
		return true;
	}

	public static void SetOrbitOnRight(bool value)
	{
		EnsureLoaded();
		if (_orbitRight == value)
		{
			return;
		}
		_orbitRight = value;
		PlayerPrefs.SetInt(OrbitKey, value ? 1 : 0);
		PlayerPrefs.Save();
		m_OnChanged?.Invoke();
	}

	public static void SetInvertWheel(ControlScope s, bool value)
	{
		EnsureLoaded();
		if (s == ControlScope.Flat2D)
		{
			if (_invert2D == value)
			{
				return;
			}
			_invert2D = value;
		}
		else
		{
			if (_invert3D == value)
			{
				return;
			}
			_invert3D = value;
		}
		PlayerPrefs.SetInt(WheelPrefix + s, value ? 1 : 0);
		PlayerPrefs.Save();
		m_OnChanged?.Invoke();
	}

	public static void ResetScope(ControlScope s)
	{
		EnsureLoaded();
		for (int i = 0; i < Keys.Length; i++)
		{
			KeyEntry entry = Keys[i];
			if (entry.scope == s)
			{
				_keys[entry.action] = entry.def;
				PlayerPrefs.DeleteKey(KeyPrefix + entry.action);
			}
		}
		for (int j = 0; j < Modifiers.Length; j++)
		{
			ModEntry entry = Modifiers[j];
			if (entry.scope == s)
			{
				_mods[entry.id] = entry.def;
				PlayerPrefs.DeleteKey(ModPrefix + entry.id);
			}
		}
		if (s == ControlScope.Analytic3D)
		{
			_orbitRight = false;
			PlayerPrefs.DeleteKey(OrbitKey);
			_invert3D = false;
		}
		else
		{
			_invert2D = false;
		}
		PlayerPrefs.DeleteKey(WheelPrefix + s);
		PlayerPrefs.Save();
		m_OnChanged?.Invoke();
	}

	public static ControlScope ScopeOf(ControlAction a)
	{
		for (int i = 0; i < Keys.Length; i++)
		{
			if (Keys[i].action == a)
			{
				return Keys[i].scope;
			}
		}
		return ControlScope.Analytic3D;
	}

	public static ModEntry EntryOf(ControlModifier m)
	{
		for (int i = 0; i < Modifiers.Length; i++)
		{
			if (Modifiers[i].id == m)
			{
				return Modifiers[i];
			}
		}
		return null;
	}

	public static KeyEntry EntryOf(ControlAction a)
	{
		for (int i = 0; i < Keys.Length; i++)
		{
			if (Keys[i].action == a)
			{
				return Keys[i];
			}
		}
		return null;
	}

	public static bool IsAssignable(KeyCode k)
	{
		if (_assignableSet == null)
		{
			_assignableSet = new HashSet<KeyCode>(Assignable);
		}
		return _assignableSet.Contains(k);
	}

	public static bool IsReserved(KeyCode k)
	{
		if ((uint)(k - KeyCode.Alpha0) <= 9u || (uint)(k - KeyCode.Keypad0) <= 9u)
		{
			return true;
		}
		for (int i = 0; i < ReservedKeys.Length; i++)
		{
			if (ReservedKeys[i] == k)
			{
				return true;
			}
		}
		return false;
	}

	public static string KeyName(KeyCode k)
	{
		switch (k)
		{
			case KeyCode.None:
			case KeyCode.Minus:
				return "-";
			case KeyCode.Backspace:
				return "Backspace";
			case KeyCode.Tab:
				return "Tab";
			case KeyCode.Return:
				return "Enter";
			case KeyCode.Escape:
				return "Escape";
			case KeyCode.Space:
				return "Space";
			case KeyCode.Quote:
				return "'";
			case KeyCode.Comma:
				return ",";
			case KeyCode.Period:
				return ".";
			case KeyCode.Slash:
				return "/";
			case KeyCode.Semicolon:
				return ";";
			case KeyCode.LeftBracket:
				return "[";
			case KeyCode.Backslash:
				return "\\";
			case KeyCode.RightBracket:
				return "]";
			case KeyCode.BackQuote:
				return "`";
			case KeyCode.Delete:
				return "Delete";
			case KeyCode.KeypadEnter:
				return "KP";
			case KeyCode.UpArrow:
				return "Up";
			case KeyCode.DownArrow:
				return "Down";
			case KeyCode.RightArrow:
				return "Right";
			case KeyCode.LeftArrow:
				return "Left";
			case KeyCode.Insert:
				return "Insert";
			case KeyCode.Home:
				return "Home";
			case KeyCode.End:
				return "End";
			case KeyCode.PageUp:
				return "PageUp";
			case KeyCode.PageDown:
				return "PageDown";
		}
		if (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9)
		{
			return ((char)k).ToString();
		}
		return k.ToString();
	}

	public static string ModifierName(ModifierKind k)
	{
		if (k == ModifierKind.Ctrl)
		{
			return "Ctrl";
		}
		return k == ModifierKind.Alt ? "Alt" : "Shift";
	}

	public static string MouseName(int button)
	{
		if (button == 1)
		{
			return Localization.T("controls.mouse.right");
		}
		if (button == 2)
		{
			return Localization.T("controls.mouse.middle");
		}
		return Localization.T("controls.mouse.left");
	}

	public static string WheelName()
	{
		return Localization.T("controls.mouse.wheel");
	}
}
