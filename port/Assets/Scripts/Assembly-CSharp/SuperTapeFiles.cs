using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SuperTapeFiles
{
	public const string WritingSuffix = ".writing";

	public const string KeyRefuseActive = "super.library.refuse_active";

	public const string KeyRefuseIo = "super.library.refuse_io";

	public const string TapesArg = "-superTapes";

	public const string TapesEnv = "ARMORSIM_SUPER_TAPES";

	public static bool IsWritingPath(string p)
	{
		return !string.IsNullOrEmpty(p) && p.EndsWith(WritingSuffix, StringComparison.OrdinalIgnoreCase);
	}

	public static string FinalPathOf(string p)
	{
		if (IsWritingPath(p))
		{
			return p.Substring(0, p.Length - WritingSuffix.Length);
		}
		return p;
	}

	public static bool IsPartialByName(string p)
	{
		return !string.IsNullOrEmpty(p) && p.EndsWith(WritingSuffix, StringComparison.OrdinalIgnoreCase);
	}

	public static string RunNameOf(string tapePath)
	{
		if (string.IsNullOrEmpty(tapePath))
		{
			return "";
		}
		return Path.GetFileNameWithoutExtension(Path.GetFileName(FinalPathOf(tapePath)));
	}

	public static bool IsActiveRunTape(string p)
	{
		return IsActiveRunTape(p, SuperSolveJob.Running, SuperCheckpoint.ActiveRun);
	}

	public static bool IsActiveRunTape(string p, bool running, string activeRun)
	{
		if (!running || string.IsNullOrEmpty(p))
		{
			return false;
		}
		if (string.IsNullOrEmpty(activeRun))
		{
			return IsWritingPath(p);
		}
		return string.Equals(RunNameOf(p), activeRun, StringComparison.OrdinalIgnoreCase);
	}

	public static bool TryDelete(string tapePath, out string refusalKey)
	{
		bool exportLeft;
		return TryDeleteCore(tapePath, SuperSolveJob.Running, SuperCheckpoint.ActiveRun, SuperTapeRecorder.TapeDir, SuperCheckpoint.Dir, out refusalKey, out exportLeft);
	}

	public static bool TryDelete(string tapePath, out string refusalKey, out bool exportLeft)
	{
		return TryDeleteCore(tapePath, SuperSolveJob.Running, SuperCheckpoint.ActiveRun, SuperTapeRecorder.TapeDir, SuperCheckpoint.Dir, out refusalKey, out exportLeft);
	}

	public static bool TryDeleteCore(string tapePath, bool running, string activeRun, string tapeDir, string checkpointDir, out string refusalKey, out bool exportLeft)
	{
		refusalKey = null;
		exportLeft = false;
		if (string.IsNullOrEmpty(tapePath))
		{
			throw new ArgumentException("SuperTapeFiles.TryDelete: пустой путь ленты", "tapePath");
		}
		string run = null;
		try
		{
			if (IsActiveRunTape(tapePath, running, activeRun))
			{
				refusalKey = KeyRefuseActive;
				Debug.LogWarning("[Super] Удаление отказано: " + tapePath + " — лента идущего прогона «" + (activeRun ?? "(имя ещё не взведено)") + "».");
				return false;
			}
			tapePath = Path.GetFullPath(tapePath);
			run = RunNameOf(tapePath);
			string sidecar = SuperRunRecord.SidecarPathFor(tapePath);
			string twin = (IsWritingPath(tapePath) ? FinalPathOf(tapePath) : tapePath + WritingSuffix);
			bool exists = File.Exists(twin);
			if (exists)
			{
				Debug.LogWarning("[Super] Удаление " + tapePath + ": рядом лежит " + twin + " с тем же именем прогона — спутник и точки продолжения оставлены ей.");
			}
			if (!DeleteLoud(tapePath, "лента", out refusalKey))
			{
				return false;
			}
			if (!exists)
			{
				if (!DeleteLoud(sidecar, "спутник ленты", out refusalKey))
				{
					return false;
				}
				if (!DeleteLoud(sidecar + ".part", "недописанный спутник", out refusalKey))
				{
					return false;
				}
			}
			string text = SafeFull(tapeDir);
			string text2 = SafeFull(Path.GetDirectoryName(tapePath));
			if (!exists && !string.IsNullOrEmpty(run) && text != null && text2 != null && string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
			{
				string text3 = SafeFull(SuperCheckpoint.Dir);
				string text4 = SafeFull(checkpointDir);
				if (text4 != null && text3 != null && string.Equals(text4, text3, StringComparison.OrdinalIgnoreCase))
				{
					SuperCheckpoint.DiscardAll(run);
				}
				char[] array = new char[2] { 'A', 'B' };
				for (int i = 0; i < array.Length; i++)
				{
					char slot = array[i];
					string text5 = Path.Combine(checkpointDir ?? "", run + "." + slot + ".ckpt");
					if (!DeleteLoud(text5, "контрольная точка", out refusalKey))
					{
						Debug.LogError("[Super] Удаление: лента " + tapePath + " удалена, но контрольная точка " + text5 + " осталась (файл занят?) — предложение продолжить укажет на ленту, которой нет.");
						SuperSolveJob.ForgetTape(tapePath);
						SuperHud.RefreshResumeProbe();
						return false;
					}
					if (!DeleteLoud(text5 + ".part", "недописанная контрольная точка", out refusalKey))
					{
						Debug.LogError("[Super] Удаление: лента " + tapePath + " удалена, но контрольная точка " + text5 + " осталась (файл занят?) — предложение продолжить укажет на ленту, которой нет.");
						SuperSolveJob.ForgetTape(tapePath);
						SuperHud.RefreshResumeProbe();
						return false;
					}
				}
			}
			SuperSolveJob.ForgetTape(tapePath);
			SuperSolveJob.ForgetTape(FinalPathOf(tapePath));
			SuperHud.RefreshResumeProbe();
			string text6 = ExportDirFor(tapeDir, tapePath);
			exportLeft = Directory.Exists(text6 ?? "");
			return true;
		}
		catch (Exception ex)
		{
			refusalKey = KeyRefuseIo;
			Debug.LogError("[Super] Удаление: путь ленты не разобран («" + run + "»): " + ex.Message);
			return false;
		}
	}

	public static string ExportDirFor(string tapeDir, string tapePath)
	{
		return Path.Combine(tapeDir ?? "", "export", Path.GetFileNameWithoutExtension(tapePath ?? ""));
	}

	public static string CheckpointSlotPath(string checkpointDir, string run, char slot)
	{
		return Path.Combine(checkpointDir ?? "", run + "." + slot + ".ckpt");
	}

	private static bool DeleteLoud(string path, string what, out string refusalKey)
	{
		refusalKey = null;
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
				if (File.Exists(path))
				{
					throw new IOException("имя осталось после удаления — файл держит другой дескриптор");
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			refusalKey = KeyRefuseIo;
			Debug.LogError("[Super] Не удалось удалить " + what + " " + path + ": " + ex.GetType().FullName + ": " + ex.Message);
			return false;
		}
	}

	private static string SafeFull(string p)
	{
		if (string.IsNullOrEmpty(p))
		{
			return null;
		}
		return Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}

	public static IReadOnlyList<string> GalleryRoots()
	{
		List<string> roots = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddRoot(roots, seen, SuperTapeRecorder.TapeDir);
		AddRoot(roots, seen, Path.Combine(Application.dataPath, "..", "..", "..", "Library", "SuperTapes"));
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			if (string.Equals(commandLineArgs[i], TapesArg, StringComparison.OrdinalIgnoreCase) && i + 1 < commandLineArgs.Length)
			{
				AddRoot(roots, seen, commandLineArgs[i + 1]);
			}
		}
		string environmentVariable = Environment.GetEnvironmentVariable(TapesEnv);
		if (!string.IsNullOrEmpty(environmentVariable))
		{
			string[] array = environmentVariable.Split(';');
			for (int j = 0; j < array.Length; j++)
			{
				string text = SafeFull(array[j].Trim().Trim('"'));
				if (text != null && Directory.Exists(text) && seen.Add(text))
				{
					roots.Add(text);
				}
			}
		}
		return roots.AsReadOnly();
	}

	private static void AddRoot(List<string> roots, HashSet<string> seen, string dir)
	{
		string text = SafeFull(dir);
		if (text != null && Directory.Exists(text) && seen.Add(text))
		{
			roots.Add(text);
		}
	}

	public static string FindTape(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
		{
			return null;
		}
		foreach (string root in GalleryRoots())
		{
			string text = Path.Combine(root, fileName);
			if (File.Exists(text))
			{
				return Path.GetFullPath(text);
			}
		}
		return null;
	}
}
