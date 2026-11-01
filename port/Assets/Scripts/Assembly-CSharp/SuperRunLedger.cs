using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class SuperRunLedger
{
	public struct Version
	{
		public int pipelineVersion;

		public int workers;

		public string gpuPath;

		public string fieldsPath;

		public static Version Game => new Version
		{
			pipelineVersion = SuperTapeRecorder.PipelineVersionOf(SuperTapeRecorder.GamePipeline, SuperTapeRecorder.GamePaced),
			workers = SuperTapeRecorder.WorkersOf(SuperTapeRecorder.GamePipeline),
			gpuPath = SuperKernelGPU.GameGpuPath,
			fieldsPath = SuperReconGpu.PlannedPathFor(SuperTapeRecorder.GamePipeline)
		};
	}

	public const string FileName = "super_cost_ledger.jsonl";

	private static readonly UTF8Encoding Utf8NoBom;

	public static string LedgerPath => Path.Combine(Application.persistentDataPath, "super_cost_ledger.jsonl");

	static SuperRunLedger()
	{
		Utf8NoBom = new UTF8Encoding(false);
	}

	public static void Append(SuperRunRecord r)
	{
		Append(LedgerPath, r);
	}

	public static void Append(string path, SuperRunRecord r)
	{
		if (r == null)
		{
			throw new ArgumentNullException("r");
		}
		string json = JsonUtility.ToJson(r).Replace("\r", "\\r").Replace("\n", "\\n");
		string directory = Path.GetDirectoryName(Path.GetFullPath(path));
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}
		using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
		{
			if (stream.Length > 0L)
			{
				stream.Seek(-1L, SeekOrigin.End);
				if (stream.ReadByte() != 10)
				{
					stream.Seek(0L, SeekOrigin.End);
					stream.WriteByte(10);
				}
			}
			stream.Seek(0L, SeekOrigin.End);
			byte[] bytes = Utf8NoBom.GetBytes(json + "\n");
			stream.Write(bytes, 0, bytes.Length);
			stream.Flush(true);
		}
	}

	public static List<SuperRunRecord> ReadAll()
	{
		return ReadAll(LedgerPath);
	}

	public static List<SuperRunRecord> ReadAll(string path)
	{
		List<SuperRunRecord> rows = new List<SuperRunRecord>();
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return rows;
		}
		int bad = 0;
		int otherVersion = 0;
		string firstError = null;
		string text;
		using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
		{
			using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
			{
				text = reader.ReadToEnd();
			}
		}
		string[] lines = text.Split('\n');
		bool trailingNewline = text.Length > 0 && text[text.Length - 1] == '\n';
		for (int i = 0; i < lines.Length; i++)
		{
			string line = lines[i].TrimEnd('\r');
			if (line.Trim().Length == 0)
			{
				continue;
			}
			try
			{
				SuperRunRecord r = JsonUtility.FromJson<SuperRunRecord>(line);
				if (r == null)
				{
					throw new ArgumentException("пустая запись");
				}
				if (r.v == 1)
				{
					r.NormaliseMissing();
					rows.Add(r);
				}
				else
				{
					otherVersion++;
				}
			}
			catch (Exception ex)
			{
				bad++;
				if (firstError == null)
				{
					firstError = "строка " + (i + 1) + ": " + ex.Message;
				}
			}
		}
		if (bad > 0 || otherVersion > 0)
		{
			Debug.LogWarning(string.Format("[Super] Реестр {0}: нечитаемых строк {1}, чужой версии {2} — пропущены", path, bad, otherVersion) + ((firstError != null) ? (" (первая — " + firstError + ").") : "."));
		}
		return rows;
	}

	public static bool TapeCoefficient(string device, out double medianSecondsPerParticleFrame, out double minK, out double maxK, out int n)
	{
		return TapeCoefficient(ReadAll(), device, null, Version.Game, out medianSecondsPerParticleFrame, out minK, out maxK, out n);
	}

	private static bool SameTapeVersion(SuperRunRecord r, Version v)
	{
		return r.pipelineVersion == v.pipelineVersion && r.workers == v.workers && (string.IsNullOrEmpty(r.fieldsPath) ? "cpu" : r.fieldsPath) == (string.IsNullOrEmpty(v.fieldsPath) ? "cpu" : v.fieldsPath);
	}

	private static bool SameSolveVersion(SuperRunRecord r, Version v)
	{
		return r.pipelineVersion == v.pipelineVersion && (string.IsNullOrEmpty(r.gpuPath) ? "full" : r.gpuPath) == (string.IsNullOrEmpty(v.gpuPath) ? "full" : v.gpuPath) && (string.IsNullOrEmpty(r.fieldsPath) ? "cpu" : r.fieldsPath) == (string.IsNullOrEmpty(v.fieldsPath) ? "cpu" : v.fieldsPath);
	}

	public static bool TapeCoefficient(IList<SuperRunRecord> rows, string device, string api, Version? version, out double medianSecondsPerParticleFrame, out double minK, out double maxK, out int n)
	{
		List<double> xs = new List<double>();
		if (rows != null)
		{
			foreach (SuperRunRecord r in rows)
			{
				if (!Matches(r, device, api) || (version.HasValue && !SameTapeVersion(r, version.Value)))
				{
					continue;
				}
				if (r.actual != null && r.actual.tapeS > 0.0 && r.actual.particles > 0L && r.frames > 0)
				{
					xs.Add(r.actual.tapeS / ((double)r.frames * r.actual.particles));
				}
			}
		}
		return Summarise(xs, out medianSecondsPerParticleFrame, out minK, out maxK, out n);
	}

	public static bool TotalErrorBand(string device, out double minErr, out double maxErr, out int n)
	{
		return TotalErrorBand(ReadAll(), device, null, false, Version.Game, out minErr, out maxErr, out n);
	}

	public static bool TotalErrorBand(IList<SuperRunRecord> rows, string device, string api, bool onlyWithTapePrediction, Version? version, out double minErr, out double maxErr, out int n)
	{
		minErr = 0.0;
		maxErr = 0.0;
		n = 0;
		if (rows != null)
		{
			foreach (SuperRunRecord r in rows)
			{
				if (!Matches(r, device, api) || (version.HasValue && !SameSolveVersion(r, version.Value)))
				{
					continue;
				}
				if (r.predicted == null || r.predicted.solveS <= 0.0 || (onlyWithTapePrediction && r.predicted.tapeS <= 0.0) || r.actual == null || r.actual.solveS <= 0.0 || r.actual.tapeS <= 0.0)
				{
					continue;
				}
				double actual = r.actual.solveS + r.actual.tapeS;
				if (actual <= 0.0)
				{
					continue;
				}
				double error = (r.predicted.solveS + Math.Max(0.0, r.predicted.tapeS) - actual) / actual;
				if (n == 0)
				{
					minErr = error;
					maxErr = error;
				}
				else
				{
					minErr = Math.Min(minErr, error);
					maxErr = Math.Max(maxErr, error);
				}
				n++;
			}
		}
		return n > 0;
	}

	private static bool Matches(SuperRunRecord r, string device, string api)
	{
		return r != null && r.v == 1 && r.completed && r.ok && r.allocFailures == 0 && r.nativeLogFailures == 0 && string.IsNullOrEmpty(r.invalidReason) && r.resumedFromStep == 0 && r.predicted != null && r.actual != null && string.Equals(r.device ?? "", device ?? "", StringComparison.Ordinal) && (api == null || string.Equals(r.api ?? "", api, StringComparison.Ordinal));
	}

	private static bool Summarise(List<double> xs, out double median, out double min, out double max, out int n)
	{
		n = xs.Count;
		median = 0.0;
		min = 0.0;
		max = 0.0;
		if (n == 0)
		{
			return false;
		}
		xs.Sort();
		min = xs[0];
		max = xs[n - 1];
		median = ((n % 2 == 1) ? xs[n / 2] : ((xs[n / 2 - 1] + xs[n / 2]) * 0.5));
		return true;
	}
}
