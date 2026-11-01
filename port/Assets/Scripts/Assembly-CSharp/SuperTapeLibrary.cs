using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

public static class SuperTapeLibrary
{
	public sealed class Entry
	{
		public string path;

		public string name;

		public string scene;

		public DateTime modified;

		public long bytes;

		public int frames;

		public float durationS;

		public bool partial;

		public bool unreadable;

		public bool active;

		public int version;

		public string note;

		public Entry()
		{
			version = -1;
			note = "";
		}
	}

	[CompilerGenerated]
	private sealed class _003CDisplayKeys_003Ed__14 : IEnumerable<string>, IEnumerable, IEnumerator<string>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private string _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private (string raw, string key)[] _003C_003E7__wrap1;

		private int _003C_003E7__wrap2;

		private Dictionary<string, string>.ValueCollection.Enumerator _003C_003E7__wrap3;

		string IEnumerator<string>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CDisplayKeys_003Ed__14(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int state = _003C_003E1__state;
			if (state == 2 || state == -3)
			{
				_003C_003Em__Finally1();
			}
			if (state == 3 || state == -4)
			{
				_003C_003Em__Finally2();
			}
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
				case 0:
					_003C_003E7__wrap1 = SceneTokens;
					_003C_003E7__wrap2 = 0;
					goto case 1;
				case 1:
					if (_003C_003E7__wrap2 < _003C_003E7__wrap1.Length)
					{
						_003C_003E2__current = _003C_003E7__wrap1[_003C_003E7__wrap2++].key;
						_003C_003E1__state = 1;
						return true;
					}
					_003C_003E7__wrap1 = null;
					_003C_003E7__wrap3 = FieldNameKeys.Values.GetEnumerator();
					_003C_003E1__state = -3;
					goto case 2;
				case 2:
					if (_003C_003E7__wrap3.MoveNext())
					{
						_003C_003E2__current = _003C_003E7__wrap3.Current;
						_003C_003E1__state = 2;
						return true;
					}
					_003C_003Em__Finally1();
					_003C_003E7__wrap3 = UnitKeys.Values.GetEnumerator();
					_003C_003E1__state = -4;
					goto case 3;
				case 3:
					if (_003C_003E7__wrap3.MoveNext())
					{
						_003C_003E2__current = _003C_003E7__wrap3.Current;
						_003C_003E1__state = 3;
						return true;
					}
					_003C_003Em__Finally2();
					_003C_003E2__current = "super.library.mb";
					_003C_003E1__state = 4;
					return true;
				case 4:
					_003C_003E2__current = "super.library.frames_duration";
					_003C_003E1__state = 5;
					return true;
				case 5:
					_003C_003E2__current = "super.library.active_tag";
					_003C_003E1__state = 6;
					return true;
				case 6:
					_003C_003E2__current = "super.library.unknown_scene";
					_003C_003E1__state = 7;
					return true;
				case 7:
					_003C_003E2__current = "super.viewer.partial_tag";
					_003C_003E1__state = 8;
					return true;
				case 8:
					_003C_003E2__current = "super.viewer.unreadable_tag";
					_003C_003E1__state = 9;
					return true;
				case 9:
					_003C_003E2__current = "super.viewer.frames_on_open";
					_003C_003E1__state = 10;
					return true;
				default:
					return false;
			}
		}

		bool IEnumerator.MoveNext()
		{
			return MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			((IDisposable)_003C_003E7__wrap3).Dispose();
		}

		private void _003C_003Em__Finally2()
		{
			_003C_003E1__state = -1;
			((IDisposable)_003C_003E7__wrap3).Dispose();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				return this;
			}
			return new _003CDisplayKeys_003Ed__14(0);
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<string>)this).GetEnumerator();
		}
	}

	private static readonly (string raw, string key)[] SceneTokens;

	private static readonly Dictionary<string, string> FieldNameKeys;

	private static readonly Dictionary<string, string> UnitKeys;

	static SuperTapeLibrary()
	{
		SceneTokens = new(string, string)[6]
		{
			("м/с", "super.library.tok.mps"),
			("мм", "super.library.tok.mm"),
			("защ.", "super.library.tok.clamped"),
			("своб.", "super.library.tok.free"),
			("струя", "super.library.tok.jet"),
			("тел", "super.library.tok.bodies")
		};
		FieldNameKeys = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			{ "eps_p", "super.viewer.fieldname.eps_p" },
			{ "|v|", "super.viewer.fieldname.speed" },
			{ "T", "super.viewer.fieldname.temperature" },
			{ "s_vM", "super.viewer.fieldname.von_mises" },
			{ "D", "super.viewer.fieldname.damage" }
		};
		UnitKeys = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			{ "м/с", "super.library.tok.mps" },
			{ "К", "super.viewer.unit.kelvin" },
			{ "ГПа", "super.viewer.unit.gpa" }
		};
	}

	public static bool LooksLikeTape(string path)
	{
		try
		{
			using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			{
				if (stream.Length < 8L)
				{
					return false;
				}
				byte[] magic = new byte[4];
				int read = 0;
				while (read < 4)
				{
					int n = stream.Read(magic, read, 4 - read);
					if (n <= 0)
					{
						return false;
					}
					read += n;
				}
				return BitConverter.ToUInt32(magic, 0) == SuperTape.Magic;
			}
		}
		catch
		{
			return false;
		}
	}

	public static List<Entry> Scan(string dir = null)
	{
		List<Entry> result = new List<Entry>();
		if (dir == null)
		{
			dir = SuperTapeRecorder.TapeDir;
		}
		if (!Directory.Exists(dir))
		{
			return result;
		}
		foreach (string path in Directory.GetFiles(dir))
		{
			if (path.EndsWith(".v6.tmp", StringComparison.OrdinalIgnoreCase) || !LooksLikeTape(path))
			{
				continue;
			}
			Entry entry = new Entry
			{
				path = Path.GetFullPath(path),
				name = Path.GetFileName(path),
				modified = File.GetLastWriteTime(path),
				bytes = new FileInfo(path).Length
			};
			try
			{
				using (SuperTape.Reader reader = new SuperTape.Reader(path))
				{
					entry.scene = reader.header.scene;
					entry.version = reader.FormatVersion;
					entry.frames = reader.FrameCount;
					entry.durationS = ((reader.FrameCount > 0) ? reader.FrameTime(reader.FrameCount - 1) : 0f);
				}
				entry.partial = SuperTapeFiles.IsPartialByName(path);
			}
			catch (Exception ex)
			{
				entry.partial = true;
				entry.frames = -1;
				entry.note = ex.Message;
				entry.version = SuperTapeCompact.VersionOf(path);
				if (!TryHeader(path, out entry.scene))
				{
					entry.unreadable = true;
				}
			}
			entry.active = SuperTapeFiles.IsActiveRunTape(entry.path);
			result.Add(entry);
		}
		result.Sort((Entry a, Entry b) => a.modified.CompareTo(b.modified));
		return result;
	}

	public static bool TryHeader(string path, out string scene)
	{
		scene = "";
		try
		{
			using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			{
				BinaryReader reader = new BinaryReader(stream);
				if (reader.ReadUInt32() != SuperTape.Magic || !SuperTape.Readable(reader.ReadInt32()))
				{
					return false;
				}
				reader.ReadSingle();
				int length = reader.ReadInt32();
				if (length > 65536)
				{
					return false;
				}
				scene = Encoding.UTF8.GetString(reader.ReadBytes(length));
				return true;
			}
		}
		catch
		{
			return false;
		}
	}

	public static string Caption(Entry e)
	{
		return e.name + "  ·  " + CaptionTitle(e, 70) + "  ·  " + CaptionMeta(e, false);
	}

	public static string CaptionTitle(Entry e, int maxChars)
	{
		string title = (string.IsNullOrEmpty(e.scene) ? Localization.T("super.library.unknown_scene") : DisplayScene(e.scene));
		if (maxChars > 3 && title.Length > maxChars)
		{
			title = title.Substring(0, maxChars - 3) + "…";
		}
		return title;
	}

	public static string CaptionMeta(Entry e, bool withName)
	{
		string tags;
		if (e.unreadable)
		{
			tags = "  [" + Localization.T("super.viewer.unreadable_tag") + "]";
		}
		else if (e.partial)
		{
			tags = "  [" + Localization.T("super.viewer.partial_tag") + "]";
		}
		else
		{
			tags = "";
		}
		if (e.active)
		{
			tags = tags + "  [" + Localization.T("super.library.active_tag") + "]";
		}
		string frames;
		if (e.unreadable)
		{
			frames = "—";
		}
		else if (e.frames < 0)
		{
			frames = Localization.T("super.viewer.frames_on_open");
		}
		else
		{
			frames = Localization.T("super.library.frames_duration", e.frames, (e.durationS * 1000000f).ToString("F0"));
		}
		string name = (withName ? (e.name + "  ·  ") : "");
		return name + string.Format("{0:yyyy-MM-dd HH:mm}  ·  {1}  ·  {2}{3}", e.modified, frames, Megabytes(e.bytes), tags);
	}

	public static string Megabytes(long bytes)
	{
		return Localization.T("super.library.mb", ((double)bytes / 1048576.0).ToString("F0"));
	}

	public static string DisplayScene(string raw)
	{
		if (string.IsNullOrEmpty(raw))
		{
			return raw ?? "";
		}
		StringBuilder sb = new StringBuilder(raw.Length + 16);
		int position = 0;
		while (position < raw.Length)
		{
			bool matched = false;
			for (int i = 0; i < SceneTokens.Length; i++)
			{
				(string token, string key) = SceneTokens[i];
				if (string.CompareOrdinal(raw, position, token, 0, token.Length) != 0)
				{
					continue;
				}
				bool left = position == 0 || !char.IsLetter(raw[position - 1]);
				int end = position + token.Length;
				bool right = end >= raw.Length || !char.IsLetter(raw[end]) || !char.IsLetter(token[token.Length - 1]);
				if (left && right)
				{
					sb.Append(Localization.T(key));
					position = end;
					matched = true;
					break;
				}
			}
			if (!matched)
			{
				sb.Append(raw[position++]);
			}
		}
		return FontSubstitutions.Apply(sb.ToString());
	}

	public static string DisplayFieldName(string raw)
	{
		if (raw != null && FieldNameKeys.TryGetValue(raw, out string key))
		{
			return Localization.T(key);
		}
		return FontSubstitutions.Apply(raw ?? "");
	}

	public static string DisplayUnit(string raw)
	{
		if (raw != null && UnitKeys.TryGetValue(raw, out string key))
		{
			return Localization.T(key);
		}
		return FontSubstitutions.Apply(raw ?? "");
	}

	[IteratorStateMachine(typeof(_003CDisplayKeys_003Ed__14))]
	public static IEnumerable<string> DisplayKeys()
	{
		return new _003CDisplayKeys_003Ed__14(-2);
	}
}
