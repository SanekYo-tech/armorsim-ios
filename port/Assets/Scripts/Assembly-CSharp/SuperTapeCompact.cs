using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;
using ThreadPriority = System.Threading.ThreadPriority;

public static class SuperTapeCompact
{
	public sealed class Result
	{
		public string path;

		public bool ok;

		public string refusalKey;

		public object[] args;

		public string detail;

		public long bytesBefore;

		public long bytesAfter;

		public int frames;

		public double seconds;

		public bool recovered;
	}

	public sealed class Job
	{
		private readonly List<string> paths;

		private bool cancel;

		public int tapeIndex;

		public int frame;

		public int frames;

		public readonly List<Result> results;

		public bool done;

		public string Current
		{
			get
			{
				int index = Volatile.Read(ref tapeIndex);
				return (index < paths.Count) ? paths[Volatile.Read(ref tapeIndex)] : null;
			}
		}

		public int Count => paths.Count;

		public Job(List<string> paths)
		{
			results = new List<Result>();
			this.paths = paths;
			Thread thread = new Thread(Run)
			{
				IsBackground = true,
				Name = "SuperTapeCompact",
				Priority = ThreadPriority.AboveNormal
			};
			thread.Start();
		}

		private void Run()
		{
			try
			{
				for (int i = 0; i < paths.Count && !Volatile.Read(ref cancel); i++)
				{
					Volatile.Write(ref tapeIndex, i);
					Volatile.Write(ref frame, 0);
					Volatile.Write(ref frames, 0);
					Result result = Compact(paths[i], SuperSolveJob.Running, SuperCheckpoint.ActiveRun, (int f, int n) =>
					{
						Volatile.Write(ref frame, f);
						Volatile.Write(ref frames, n);
						return !Volatile.Read(ref cancel);
					});
					lock (results)
					{
						results.Add(result);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
			finally
			{
				Volatile.Write(ref done, true);
			}
		}

		public void Cancel()
		{
			Volatile.Write(ref cancel, true);
		}

		public Result[] Snapshot()
		{
			lock (results)
			{
				return results.ToArray();
			}
		}
	}

	public const string TmpSuffix = ".v6.tmp";

	public const string KeyNotV5 = "super.tape.compact.refuse_not_v5";

	public const string KeyActive = "super.tape.compact.refuse_active";

	public const string KeyCheckpoint = "super.tape.compact.refuse_checkpoint";

	public const string KeyNoSpace = "super.tape.compact.refuse_space";

	public const string KeySourceBad = "super.tape.compact.refuse_source";

	public const string KeyVerify = "super.tape.compact.refuse_verify";

	public const string KeyIo = "super.tape.compact.refuse_io";

	public const string KeyCancelled = "super.tape.compact.cancelled";

	public static int SabotageAbortAfterFrames;

	public static bool SabotageCorruptTwin;

	public static long SabotageFreeBytes;

	static SuperTapeCompact()
	{
		SabotageAbortAfterFrames = -1;
		SabotageFreeBytes = -1L;
	}

	public static int VersionOf(string path)
	{
		try
		{
			using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				if (stream.Length < 8L)
				{
					return -1;
				}
				byte[] header = new byte[8];
				int read = 0;
				while (read < 8)
				{
					int n = stream.Read(header, read, 8 - read);
					if (n <= 0)
					{
						return -1;
					}
					read += n;
				}
				if (BitConverter.ToUInt32(header, 0) != SuperTape.Magic)
				{
					return -1;
				}
				return (int)BitConverter.ToUInt32(header, 4);
			}
		}
		catch
		{
			return -1;
		}
	}

	public static bool HasCheckpoint(string path)
	{
		try
		{
			string directory = Path.GetFullPath(Path.GetDirectoryName(path) ?? "");
			if (!string.Equals(directory, Path.GetFullPath(SuperTapeRecorder.TapeDir), StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			string run = SuperTapeFiles.RunNameOf(path);
			return File.Exists(SuperCheckpoint.SlotPath(run, 'A')) || File.Exists(SuperCheckpoint.SlotPath(run, 'B'));
		}
		catch
		{
			return false;
		}
	}

	public static long Estimate(SuperTape.Reader r, int samples = 6)
	{
		long stored = 0L;
		for (int i = 0; i < r.header.frameCount; i++)
		{
			stored += r.CanonicalLengthOf(i);
		}
		int bodyCount = r.header.bodyNames.Length;
		int fieldCount = r.header.fieldNames.Length;
		int sampleCount = Math.Max(1, Math.Min(samples, r.header.frameCount));
		long canonical = 0L;
		long encoded = 0L;
		for (int i = 0; i < sampleCount; i++)
		{
			int index = ((sampleCount == 1) ? 0 : ((r.header.frameCount - 1) * i / (sampleCount - 1)));
			byte[] bytes = r.ReadCanonical(index, out int len);
			canonical += len;
			encoded += SuperTape.EncodeCanonicalV6(bytes, len, bodyCount, fieldCount, SuperTape.WriteCodec).bytes.Length;
		}
		double ratio = ((canonical > 0L) ? ((double)encoded / canonical) : 1.0);
		return 512L + 8L * r.header.frameCount + (long)(stored * ratio);
	}

	public static long SpaceMargin(long estimate)
	{
		return estimate + estimate * 15L / 100L + 67108864L;
	}

	private static bool SameBytes(byte[] a, byte[] b)
	{
		if (a == null || b == null)
		{
			return a == b;
		}
		if (a.Length != b.Length)
		{
			return false;
		}
		for (int i = 0; i < a.Length; i++)
		{
			if (a[i] != b[i])
			{
				return false;
			}
		}
		return true;
	}

	public static long FreeBytes(string path)
	{
		if (SabotageFreeBytes >= 0L)
		{
			return SabotageFreeBytes;
		}
		try
		{
			return new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))).AvailableFreeSpace;
		}
		catch
		{
			return -1L;
		}
	}

	public static int CleanStale(string dir)
	{
		int removed = 0;
		try
		{
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
			{
				return 0;
			}
			foreach (string path in Directory.GetFiles(dir, "*.v6.tmp"))
			{
				try
				{
					File.Delete(path);
					removed++;
					Debug.Log("[Super] Убран брошенный файл прерванного сжатия: " + Path.GetFileName(path));
				}
				catch (Exception ex)
				{
					Debug.LogWarning("[Super] Брошенный файл сжатия не убран: " + path + " — " + ex.Message);
				}
			}
			return removed;
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Super] Обход брошенных файлов сжатия: " + ex.Message);
			return removed;
		}
	}

	public static Result Compact(string path, Func<int, int, bool> progress = null)
	{
		return Compact(path, SuperSolveJob.Running, SuperCheckpoint.ActiveRun, progress);
	}

	public static Result Compact(string path, bool running, string activeRun, Func<int, int, bool> progress = null)
	{
		string fileName = Path.GetFileName(path);
		string temporary = path + ".v6.tmp";
		Stopwatch stopwatch = Stopwatch.StartNew();
		Result result = new Result
		{
			path = path,
			args = new object[0],
			detail = ""
		};
		Result Refuse(string key, string detail, params object[] more)
		{
			result.ok = false;
			result.refusalKey = key;
			List<object> args = new List<object> { fileName };
			if (more != null)
			{
				args.AddRange(more);
			}
			result.args = args.ToArray();
			result.detail = detail ?? "";
			if (File.Exists(temporary))
			{
				File.Delete(temporary);
			}
			result.seconds = stopwatch.Elapsed.TotalSeconds;
			Debug.LogWarning("[Super] Сжатие «" + fileName + "» отказано (" + key + "): " + result.detail);
			return result;
		}
		try
		{
			if (SuperTapeFiles.IsActiveRunTape(path, running, activeRun))
			{
				return Refuse("super.tape.compact.refuse_active", "лента идущего расчёта");
			}
			int sourceVersion = VersionOf(path);
			if (sourceVersion != 5)
			{
				return Refuse("super.tape.compact.refuse_not_v5", string.Format("версия ленты {0}", sourceVersion), sourceVersion);
			}
			if (HasCheckpoint(path))
			{
				return Refuse("super.tape.compact.refuse_checkpoint", "у ленты есть контрольная точка продолжения");
			}
			DateTime sourceWriteTime = File.GetLastWriteTimeUtc(path);
			result.bytesBefore = new FileInfo(path).Length;
			using (SuperTape.Reader source = SuperTape.Reader.OpenRecovering(path))
			{
				result.recovered = source.Recovered;
				result.frames = source.header.frameCount;
				long required = SpaceMargin(Estimate(source));
				long free = FreeBytes(path);
				if (free >= 0L && free < required)
				{
					return Refuse("super.tape.compact.refuse_space", string.Format("нужно ≈ {0} байт, свободно {1}", required, free), ((double)required / 1048576.0).ToString("F0"), ((double)free / 1048576.0).ToString("F0"));
				}
				if (File.Exists(temporary))
				{
					File.Delete(temporary);
				}
				int dop = Math.Max(1, Math.Min(Environment.ProcessorCount - 1, 8));
				int mismatch = -1;
				string mismatchDetail = null;
				bool cancelled = false;
				using (SuperTape.Writer writer = new SuperTape.Writer(temporary, source.HeaderFields(), source.header.bodyNames.Length, source.header.fieldNames.Length, SuperTape.WriteCodec, source.DecorBytes))
				{
					writer.LeaveHeaderOpen = source.Recovered;
					int frameCount = source.header.frameCount;
					for (int first = 0; first < frameCount && mismatch < 0 && !cancelled;)
					{
						int batch = Math.Min(dop, frameCount - first);
						byte[][] canonical = new byte[batch][];
						int[] lengths = new int[batch];
						ulong[] hashes = new ulong[batch];
						for (int i = 0; i < batch; i++)
						{
							canonical[i] = source.ReadCanonical(first + i, out lengths[i]);
							hashes[i] = source.CanonicalHashOf(first + i);
						}
						SuperTape.Encoded[] encoded = new SuperTape.Encoded[batch];
						Parallel.For(0, batch, new ParallelOptions
						{
							MaxDegreeOfParallelism = dop
						}, delegate(int i)
						{
							encoded[i] = SuperTape.EncodeCanonicalV6(canonical[i], lengths[i], source.header.bodyNames.Length, source.header.fieldNames.Length, SuperTape.WriteCodec);
						});
						for (int i = 0; i < batch; i++)
						{
							if (encoded[i].hash != hashes[i])
							{
								mismatch = first + i;
								mismatchDetail = string.Format("кадр {0}: хэш канонических байт 0x{1:X16}, у источника 0x{2:X16}", mismatch, encoded[i].hash, hashes[i]);
								break;
							}
							writer.WriteEncoded(encoded[i]);
							if (SabotageAbortAfterFrames >= 0 && first + i + 1 >= SabotageAbortAfterFrames)
							{
								throw new OperationCanceledException("ДИВЕРСИЯ G-T9: сжатие оборвано на кадре " + (first + i + 1) + "] (ждали «probe://store/flat2d»)");
							}
						}
						if (progress != null)
						{
							cancelled = !progress(Math.Min(frameCount, first + batch), frameCount);
						}
						first += batch;
					}
					if (mismatch >= 0 || cancelled)
					{
						writer.LeaveHeaderOpen = true;
					}
				}
				if (mismatch >= 0)
				{
					return Refuse("super.tape.compact.refuse_source", mismatchDetail, mismatch);
				}
				if (cancelled)
				{
					return Refuse("super.tape.compact.cancelled", "отменено игроком");
				}
				if (SabotageCorruptTwin)
				{
					using (FileStream stream = new FileStream(temporary, FileMode.Open, FileAccess.ReadWrite))
					{
						stream.Seek(stream.Length / 2L, SeekOrigin.Begin);
						int value = stream.ReadByte();
						stream.Seek(-1L, SeekOrigin.End);
						stream.WriteByte((byte)(value ^ 0x10));
					}
				}
				using (SuperTape.Reader twin = source.Recovered ? SuperTape.Reader.OpenRecovering(temporary) : new SuperTape.Reader(temporary))
				{
					if (twin.FormatVersion != 6 || twin.header.frameCount != source.header.frameCount || twin.Recovered != source.Recovered)
					{
						return Refuse("super.tape.compact.refuse_verify", string.Format("двойник: версия {0}, кадров {1} (у источника {2}), восстановлен {3}", twin.FormatVersion, twin.header.frameCount, source.header.frameCount, twin.Recovered), -1);
					}
					if (!SameBytes(twin.DecorBytes, source.DecorBytes))
					{
						return Refuse("super.tape.compact.refuse_verify", string.Format("двойник: секция декора {0} байт, у источника {1} — не та же", (twin.DecorBytes == null) ? 0 : twin.DecorBytes.Length, (source.DecorBytes == null) ? 0 : source.DecorBytes.Length), -1);
					}
					int bad = twin.VerifyCanonical();
					if (bad >= 0)
					{
						return Refuse("super.tape.compact.refuse_verify", twin.LastVerifyNote, bad);
					}
					for (int i = 0; i < source.header.frameCount; i++)
					{
						if (twin.CanonicalHashOf(i) != source.CanonicalHashOf(i))
						{
							return Refuse("super.tape.compact.refuse_verify", string.Format("кадр {0}: хэш двойника не равен хэшу кадра источника", i), i);
						}
					}
				}
			}
			File.Replace(temporary, path, null);
			File.SetLastWriteTimeUtc(path, sourceWriteTime);
			result.bytesAfter = new FileInfo(path).Length;
			result.ok = true;
			result.seconds = stopwatch.Elapsed.TotalSeconds;
			Debug.Log(string.Format("[Super] Сжато «{0}»: {1:F0} → {2:F0} МБ ", fileName, (double)result.bytesBefore / 1048576.0, (double)result.bytesAfter / 1048576.0) + string.Format("(×{0:F2}), кадров {1}, проверено покадрово, {2:F1} с", (double)result.bytesBefore / Math.Max(1L, result.bytesAfter), result.frames, result.seconds));
			return result;
		}
		catch (Exception ex)
		{
			return Refuse("super.tape.compact.refuse_io", ex.GetType().Name + ": " + ex.Message, path);
		}
	}
}
