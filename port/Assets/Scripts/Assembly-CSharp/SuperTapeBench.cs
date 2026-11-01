using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Random = System.Random;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public sealed class SuperTapeBench : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CStart_003Ed__7 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public SuperTapeBench _003C_003E4__this;

		private IEnumerator<string> _003C_003E7__wrap1;

		private string[] _003C_003E7__wrap2;

		private int _003C_003E7__wrap3;

		object IEnumerator<object>.Current
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
		public _003CStart_003Ed__7(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if (num <= -3)
			{
				if (num == -4)
				{
					_003C_003Em__Finally2();
					return;
				}
				if (num == -3)
				{
					_003C_003Em__Finally1();
					return;
				}
			}
			else
			{
				if (num == 7)
				{
					_003C_003Em__Finally1();
					return;
				}
				if (num == 9)
				{
					_003C_003Em__Finally2();
				}
			}
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003C_003E4__this._out = Path.Combine(Application.persistentDataPath, "s36_player_bench.txt");
				_003C_003E4__this.Line(string.Format("## S36 {0} — {1:yyyy-MM-dd HH:mm:ss}; ", Application.isEditor ? "редактор" : "плеер", DateTime.Now) + string.Format("{0}, {1} логических, ОЗУ {2} МиБ; Unity {3}; ", SystemInfo.processorType, SystemInfo.processorCount, SystemInfo.systemMemorySize, Application.unityVersion) + "скриптовый бэкенд " + Backend());
				_003C_003E2__current = null;
				_003C_003E1__state = 1;
				return true;
			case 1:
				_003C_003E1__state = -1;
				_003C_003E2__current = null;
				_003C_003E1__state = 2;
				return true;
			case 2:
				_003C_003E1__state = -1;
				_003C_003E4__this.Try("deflate", _003C_003E4__this.Deflate);
				_003C_003E2__current = null;
				_003C_003E1__state = 3;
				return true;
			case 3:
			{
				_003C_003E1__state = -1;
				if (Arg("-s36half", "1") != "0")
				{
					_003C_003E4__this.Try("half", _003C_003E4__this.Half);
				}
				_003C_003E2__current = null;
				_003C_003E1__state = 4;
				return true;
			}
			case 4:
				_003C_003E1__state = -1;
				_003C_003E4__this.Try("encode", _003C_003E4__this.Encode);
				_003C_003E2__current = null;
				_003C_003E1__state = 5;
				return true;
			case 5:
				_003C_003E1__state = -1;
				_003C_003E4__this.Try("compact", _003C_003E4__this.CompactTiny);
				_003C_003E2__current = null;
				_003C_003E1__state = 6;
				return true;
			case 6:
				_003C_003E1__state = -1;
				_003C_003E7__wrap1 = Twins().GetEnumerator();
				_003C_003E1__state = -3;
				goto IL_0186;
			case 7:
				_003C_003E1__state = -3;
				goto IL_0186;
			case 8:
				_003C_003E1__state = -1;
				_003C_003E7__wrap3++;
				goto IL_021D;
			case 9:
				_003C_003E1__state = -4;
				goto IL_028E;
			}
			IL_0186:
			while (_003C_003E7__wrap1.MoveNext())
			{
				string p = _003C_003E7__wrap1.Current;
				_003C_003E4__this.Try("feed " + Path.GetFileName(p), delegate
				{
					_003C_003E4__this.Feed(p);
				});
				_003C_003E2__current = null;
				_003C_003E1__state = 7;
				return true;
			}
			_003C_003E1__state = -1;
			if (_003C_003E7__wrap1 != null)
			{
				_003C_003E7__wrap1.Dispose();
			}
			_003C_003E7__wrap1 = null;
			_003C_003E7__wrap2 = Arg("-s36src", "").Split(';');
			_003C_003E7__wrap3 = 0;
			goto IL_021D;
			IL_021D:
			while (_003C_003E7__wrap3 < _003C_003E7__wrap2.Length)
			{
				string p2 = _003C_003E7__wrap2[_003C_003E7__wrap3].Trim();
				if (p2.Length != 0 && File.Exists(p2))
				{
					_003C_003E4__this.Try("feed v5 " + Path.GetFileName(p2), delegate
					{
						_003C_003E4__this.Feed(p2);
					});
					_003C_003E4__this.Try("open v5 " + Path.GetFileName(p2), delegate
					{
						_003C_003E4__this.Open(p2);
					});
					_003C_003E2__current = null;
					_003C_003E1__state = 8;
					return true;
				}
				_003C_003E7__wrap3++;
			}
			_003C_003E7__wrap2 = null;
			_003C_003E7__wrap1 = Twins().GetEnumerator();
			_003C_003E1__state = -4;
			goto IL_028E;
			IL_028E:
			while (_003C_003E7__wrap1.MoveNext())
			{
				string p3 = _003C_003E7__wrap1.Current;
				_003C_003E4__this.Try("open " + Path.GetFileName(p3), delegate
				{
					_003C_003E4__this.Open(p3);
				});
				_003C_003E2__current = null;
				_003C_003E1__state = 9;
				return true;
			}
			_003C_003E1__state = -1;
			if (_003C_003E7__wrap1 != null)
			{
				_003C_003E7__wrap1.Dispose();
			}
			_003C_003E7__wrap1 = null;
			_003C_003E4__this.Line(string.Format("ИТОГ: стенд окончен, провалов {0}", _003C_003E4__this._fail));
			Application.Quit((_003C_003E4__this._fail != 0) ? 1 : 0);
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			if (_003C_003E7__wrap1 != null)
			{
				_003C_003E7__wrap1.Dispose();
			}
		}

		private void _003C_003Em__Finally2()
		{
			_003C_003E1__state = -1;
			if (_003C_003E7__wrap1 != null)
			{
				_003C_003E7__wrap1.Dispose();
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	[CompilerGenerated]
	private sealed class _003CTwins_003Ed__10 : IEnumerable<string>, IEnumerable, IEnumerator<string>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private string _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private List<string>.Enumerator _003C_003E7__wrap1;

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
		public _003CTwins_003Ed__10(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if (num == -3 || num == 1)
			{
				_003C_003Em__Finally1();
			}
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				goto IL_001D;
			case 1:
				_003C_003E1__state = -3;
				break;
			}
			goto IL_0069;
			IL_001D:
			{
				string text = Arg("-s36twins", "");
				if (text.Length == 0 || !Directory.Exists(text))
				{
					return false;
				}
				List<string> list = new List<string>(Directory.GetFiles(text, "*.tape"));
				list.Sort(StringComparer.OrdinalIgnoreCase);
				_003C_003E7__wrap1 = list.GetEnumerator();
				_003C_003E1__state = -3;
			}
			IL_0069:
			while (_003C_003E7__wrap1.MoveNext())
			{
				_003C_003E2__current = _003C_003E7__wrap1.Current;
				_003C_003E1__state = 1;
				return true;
			}
			_003C_003E1__state = -1;
			_003C_003E7__wrap1.Dispose();
			_003C_003E7__wrap1 = default(List<string>.Enumerator);
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			_003C_003E7__wrap1.Dispose();
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
			return new _003CTwins_003Ed__10(0);
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				return this;
			}
			return new _003CTwins_003Ed__10(0);
		}
	}

	private string _out;

	private int _fail;

	private static string Arg(string key, string def)
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length - 1; i++)
		{
			if (commandLineArgs[i] == key)
			{
				return commandLineArgs[i + 1];
			}
		}
		return def;
	}

	private static bool Has(string key)
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			if (commandLineArgs[i] == key)
			{
				return true;
			}
		}
		return false;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		if (!Has("-s36bench"))
		{
			return;
		}
		GameObject gameObject = new GameObject("S36 TapeBench");
		UnityEngine.Object.DontDestroyOnLoad(gameObject);
		gameObject.AddComponent<SuperTapeBench>();
	}

	private void Line(string s)
	{
		Debug.Log("[S36 плеер] " + s);
		File.AppendAllText(_out, s + "\n", new UTF8Encoding(false));
	}

	private void Ck(string what, bool ok, string detail)
	{
		if (!ok)
		{
			_fail++;
		}
		Line((ok ? "[ОК]   " : "[ПРОВАЛ] ") + what + " — " + detail);
	}

	[IteratorStateMachine(typeof(_003CStart_003Ed__7))]
	private IEnumerator Start()
	{
		return new _003CStart_003Ed__7(0);
	}

	private static string Backend()
	{
		if (Application.isEditor)
		{
			return "Mono (редактор)";
		}
		string text = Path.GetDirectoryName(Application.dataPath) ?? "";
		return File.Exists(Path.Combine(text, "GameAssembly.dll")) ? "IL2CPP" : "Mono";
	}

	private void Try(string what, Action a)
	{
		try
		{
			a();
		}
		catch (Exception ex)
		{
			Ck(what + ": исключение", false, ex.Message);
		}
	}

	[IteratorStateMachine(typeof(_003CTwins_003Ed__10))]
	private static IEnumerable<string> Twins()
	{
		return new _003CTwins_003Ed__10(-2);
	}

	private void Deflate()
	{
		Random random = new Random(64);
		byte[] array = new byte[67108864];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (byte)(random.Next(4) ^ i / 7);
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		MemoryStream memoryStream = new MemoryStream();
		using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionLevel.Fastest, leaveOpen: true))
		{
			deflateStream.Write(array, 0, array.Length);
		}
		byte[] array2 = memoryStream.ToArray();
		double totalMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
		stopwatch.Restart();
		byte[] array3 = new byte[array.Length];
		MemoryStream stream = new MemoryStream(array2);
		using (DeflateStream deflateStream2 = new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: false))
		{
			int num = 0;
			while (num < array3.Length)
			{
				int num2 = deflateStream2.Read(array3, num, array3.Length - num);
				if (num2 <= 0)
				{
					break;
				}
				num += num2;
			}
		}
		double totalMilliseconds2 = stopwatch.Elapsed.TotalMilliseconds;
		bool flag = true;
		for (int j = 0; j < array.Length && flag; j++)
		{
			flag = array[j] == array3[j];
		}
		Ck("P0: DeflateStream (Fastest) — круг 64 МБ", flag, string.Format("сжато {0:F1} МБ за {1:F0} мс ({2:F0} МБ/с), распаковано за {3:F0} мс ({4:F0} МБ/с)", (double)array2.Length / 1048576.0, totalMilliseconds, 64.0 / (totalMilliseconds / 1000.0), totalMilliseconds2, 64.0 / (totalMilliseconds2 / 1000.0)));
	}

	private void Half()
	{
		long[] mis = new long[64];
		uint[] first = new uint[64];
		for (int i = 0; i < first.Length; i++)
		{
			first[i] = uint.MaxValue;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		Parallel.For(0, 64, delegate(int ch)
		{
			for (long num = (long)ch << 26; num < (long)(ch + 1) << 26; num++)
			{
				float f = BitConverter.Int32BitsToSingle((int)num);
				if (SuperHalf.FromFloat(f) != Mathf.FloatToHalf(f) && mis[ch]++ == 0L)
				{
					first[ch] = (uint)num;
				}
			}
		});
		long num2 = 0L;
		uint num3 = uint.MaxValue;
		for (int j = 0; j < mis.Length; j++)
		{
			num2 += mis[j];
			if (num3 == uint.MaxValue && first[j] != uint.MaxValue)
			{
				num3 = first[j];
			}
		}
		string text = string.Format("расхождений {0}", num2);
		string text2;
		bool flag;
		if (num2 > 0)
		{
			text2 = string.Format(", первое 0x{0:X8}", num3);
			flag = false;
		}
		else
		{
			text2 = "";
			flag = num2 == 0;
		}
		Ck("G-T7: SuperHalf.FromFloat = Mathf.FloatToHalf на всех 2³² образцах (плеер)", flag, text + text2 + string.Format("; перебор {0:F1} с", stopwatch.Elapsed.TotalSeconds));
		long[] mis2 = new long[64];
		long[] oth = new long[64];
		uint[] othF = new uint[64];
		for (int k = 0; k < othF.Length; k++)
		{
			othF[k] = uint.MaxValue;
		}
		Parallel.For(0, 64, delegate(int ch)
		{
			for (long num4 = (long)ch << 26; num4 < (long)(ch + 1) << 26; num4++)
			{
				float f2 = BitConverter.Int32BitsToSingle((int)num4);
				if (SuperHalf.FromFloat(f2) != Mathf.FloatToHalf(f2))
				{
					uint num5 = (uint)num4 & 0x7FFFFFFFu;
					if (num5 > 2139095040u && (num5 & 0x400000u) == 0)
					{
						mis2[ch]++;
					}
					else if (oth[ch]++ == 0L)
					{
						othF[ch] = (uint)num4;
					}
				}
			}
		});
		long num6 = 0L;
		long num7 = 0L;
		uint num8 = uint.MaxValue;
		for (int l = 0; l < mis2.Length; l++)
		{
			num6 += mis2[l];
			num7 += oth[l];
			if (num8 == uint.MaxValue && othF[l] != uint.MaxValue)
			{
				num8 = othF[l];
			}
		}
		string text3 = string.Format("    [ЗАМЕР] G-T7 разбор: сигнальные NaN {0} (всего таких 8388606), прочие {1}", num6, num7);
		Line(text3 + ((num7 > 0) ? string.Format(", первое 0x{0:X8}", num8) : ""));
		StringBuilder stringBuilder = new StringBuilder("    [ЗАМЕР] Mathf.FloatToHalf плеера на NaN:");
		uint[] array = new uint[12]
		{
			2139095041u, 2139095042u, 2139103232u, 2140143616u, 2141192192u, 2143289343u,
			2143289344u, 2143289345u, 2147483647u, 4286578689u, 4290772991u, 2139095040u
		};
		foreach (uint num9 in array)
		{
			stringBuilder.Append(string.Format(" {0:X8}→{1:X4}/упр {2:X4}/биты {3:X8};", num9, Mathf.FloatToHalf(BitConverter.Int32BitsToSingle((int)num9)), SuperHalf.FromFloat(BitConverter.Int32BitsToSingle((int)num9)), (int)num9));
		}
		Line(stringBuilder.ToString());
	}

	private void Encode()
	{
		foreach (string text in Arg("-s36src", "").Split(';'))
		{
			string text2 = text.Trim();
			if (text2.Length == 0 || !File.Exists(text2))
			{
				continue;
			}
			SuperTape.Codec[] array = new SuperTape.Codec[2]
			{
				SuperTape.Codec.Lz4,
				SuperTape.Codec.Deflate
			};
			foreach (SuperTape.Codec codec in array)
			{
				long num = 0L;
				double num2 = 0.0;
				long num3 = 0L;
				using (SuperTape.Reader reader = SuperTape.Reader.OpenRecovering(text2))
				{
					int num4 = reader.header.bodyNames.Length;
					int num5 = reader.header.fieldNames.Length;
					int num6 = Math.Min(8, reader.header.frameCount);
					for (int i = 0; i < num6; i++)
					{
						int index = (num6 == 1) ? 0 : ((reader.header.frameCount - 1) * i / (num6 - 1));
						byte[] canonical = reader.ReadCanonical(index, out var len);
						Stopwatch stopwatch = Stopwatch.StartNew();
						SuperTape.Encoded encoded = SuperTape.EncodeCanonicalV6(canonical, len, num4, num5, codec);
						num2 += stopwatch.Elapsed.TotalSeconds;
						num += len;
						num3 += encoded.bytes.Length;
					}
				}
				Line(string.Format("    [ЗАМЕР] кодирование {0} {1}: {2:F0} МБ/с на поток, ×{3:F2} на выборке", Path.GetFileName(text2), codec, (double)num / 1048576.0 / Math.Max(1E-09, num2), (double)num / Math.Max(1L, num3)));
			}
		}
	}

	private void CompactTiny()
	{
		string text = Arg("-s36tiny", "");
		if (text.Length == 0 || !File.Exists(text))
		{
			Line("    сжатие малой ленты — пропущено (нет -s36tiny)");
			return;
		}
		string text2 = Path.Combine(Application.persistentDataPath, "s36bench");
		Directory.CreateDirectory(text2);
		string text3 = Path.Combine(text2, "tiny.tape");
		File.Copy(text, text3, true);
		string a = SuperTape.CanonicalSha256(text3);
		SuperTapeCompact.Result result = SuperTapeCompact.Compact(text3, false, null, null);
		bool flag = result.ok && SuperTape.CanonicalSha256(text3) == a;
		string text4 = "P4: сжатие малой ленты в плеере (оценка места, запись, проверка, замена)";
		string text5;
		if (result.ok)
		{
			text5 = string.Format("{0} → {1} Б, свободно {2:F1} ГБ", result.bytesBefore, result.bytesAfter, (double)SuperTapeCompact.FreeBytes(text3) / 1073741824.0);
		}
		else
		{
			text5 = result.refusalKey + ": " + result.detail;
		}
		Ck(text4, flag, text5);
		File.Delete(text3);
	}

	private void Feed(string p)
	{
		List<double> list = new List<double>();
		using (SuperTape.Reader reader = new SuperTape.Reader(p))
		{
			for (int i = 0; i < reader.header.frameCount; i++)
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				reader.ReadFrame(i, SuperTape.Detail.OneField, 1);
				list.Add(stopwatch.Elapsed.TotalMilliseconds);
			}
			list.Sort();
			Line(string.Format("    [ЗАМЕР] раскодировка OneField({0}) {1} v{2}: p50 {3:F1} / p95 {4:F1} / max {5:F1} мс, кадров {6}", 1, Path.GetFileName(p), reader.FormatVersion, Pct(list, 0.5), Pct(list, 0.95), list[list.Count - 1], list.Count));
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			double num = 5000.0 / (double)reader.header.frameCount;
			List<double> list2 = new List<double>();
			int num3;
			using (SuperTapeFeed superTapeFeed = new SuperTapeFeed(reader))
			{
				superTapeFeed.Get(0, 1);
				int num2 = superTapeFeed.MissCount;
				stopwatch2.Restart();
				for (int j = 1; j < reader.header.frameCount; j++)
				{
					while ((double)j * num > stopwatch2.Elapsed.TotalMilliseconds)
					{
						Thread.Sleep(1);
					}
					Stopwatch stopwatch3 = Stopwatch.StartNew();
					superTapeFeed.Get(j, 1);
					list2.Add(stopwatch3.Elapsed.TotalMilliseconds);
				}
				num3 = superTapeFeed.MissCount - num2;
			}
			list2.Sort();
			Line(string.Format("    [ЗАМЕР] проигрывание 1× {0}: кадр раз в {1:F1} мс, промахов подачи после первого кадра {2} из {3}; ", Path.GetFileName(p), num, num3, reader.header.frameCount - 1) + string.Format("ожидание кадра p50 {0:F2} / p95 {1:F2} / max {2:F2} мс; ", Pct(list2, 0.5), Pct(list2, 0.95), (list2.Count > 0) ? list2[list2.Count - 1] : 0.0) + string.Format("стена {0:F2} с", stopwatch2.Elapsed.TotalSeconds));
		}
	}

	private void Open(string p)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		using (SuperTape.Reader reader = new SuperTape.Reader(p))
		{
			double totalMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
			stopwatch.Restart();
			SuperTapeRender.FitTape(reader, out var centreZ, out var halfY, out var halfZ);
			double totalMilliseconds2 = stopwatch.Elapsed.TotalMilliseconds;
			stopwatch.Restart();
			int num = -1;
			for (int i = 0; i < reader.header.frameCount; i++)
			{
				if (!reader.FrameHashOk(i))
				{
					num = i;
					break;
				}
			}
			Line(string.Format("    [ЗАМЕР] открытие {0} v{1}: до первого кадра {2:F2} с ", Path.GetFileName(p), reader.FormatVersion, (totalMilliseconds2 + totalMilliseconds) / 1000.0) + string.Format("(таблица {0:F0} мс + кадрирование {1:F0} мс); проверка всех кадров {2:F2} с (итог {3})", totalMilliseconds, totalMilliseconds2, stopwatch.Elapsed.TotalSeconds, num));
		}
	}

	private static double Pct(List<double> s, double q)
	{
		if (s.Count == 0)
		{
			return 0.0;
		}
		return s[Math.Min(s.Count - 1, (int)((double)(s.Count - 1) * q + 0.5))];
	}
}
