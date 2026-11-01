using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public static class SuperAllocAudit
{
	public struct BufferRow
	{
		public string name;

		public bool expected;

		public bool allocated;

		public long allocatedBytes;

		public long tableBytes;

		public bool Match => expected == allocated && allocatedBytes == tableBytes;
	}

	public static bool TestCorruptAudit;

	public static string LastAuditReport { get; private set; }

	public static string LastAliveReport { get; private set; }

	public static string LastNativeLogNote { get; private set; }

	static SuperAllocAudit()
	{
		LastAuditReport = "";
		LastAliveReport = "";
		LastNativeLogNote = "";
	}

	public static int AfterAllocation(SuperKernelGPU k, SuperParticles p)
	{
		if (k == null)
		{
			throw new ArgumentNullException("k");
		}
		if (p == null)
		{
			throw new ArgumentNullException("p");
		}
		if (p.count != k.ParticleCount)
		{
			throw new ArgumentOutOfRangeException("p", string.Format(CultureInfo.InvariantCulture, "SuperAllocAudit: набор на {0} частиц, ядро на {1} — аудит сверял бы чужое запекание.", p.count, k.ParticleCount));
		}
		StringBuilder report = new StringBuilder();
		List<KeyValuePair<string, ComputeBuffer>> audit = k.AuditBuffers();
		SuperGpuMaterial[] table = k.AuditMaterialTable();
		if (table == null)
		{
			throw new InvalidOperationException("SuperAllocAudit: у ядра нет таблицы веществ — конструктор не дошёл до bMat.SetData.");
		}
		int failures = 0;
		int bufIndex = 0;
		foreach (KeyValuePair<string, ComputeBuffer> row in audit)
		{
			string name = row.Key;
			ComputeBuffer buffer = row.Value;
			SuperScenePlan.GpuBuffer plan = SuperScenePlan.Row(name);
			if (name == "Xi")
			{
				if (plan.Lazy != plan.In(k.Layout))
				{
					throw XiFieldMismatch(k);
				}
			}
			else if (plan.Lazy && plan.In(k.Layout))
			{
				throw XiFieldMismatch(k);
			}
			if (!buffer.IsValid())
			{
				throw new InvalidOperationException("SuperAllocAudit: буфер «" + name + "» освобождён — аудит зовут до Dispose ядра.");
			}
			bool flag = ProbeKernel(buffer, name, bufIndex, p, table, report);
			if (!flag)
			{
				failures++;
			}
			bufIndex++;
		}
		LastAuditReport = report.ToString();
		if (failures > 0)
		{
			LastAuditReport = string.Format(CultureInfo.InvariantCulture, "[S24 аудит] ⛔ {0} из {1} буферов не прошли проверку известными значениями ", failures, audit.Count) + string.Format(CultureInfo.InvariantCulture, "({0} частиц, {1} узлов, {2} МиБ на устройстве)", k.ParticleCount, k.NodeCount, SystemInfo.graphicsMemorySize) + (TestCorruptAudit ? " — ДИВЕРСИЯ TestCorruptAudit" : "") + ":\n" + report.ToString();
		}
		return failures;
	}

	private static InvalidOperationException XiFieldMismatch(SuperKernelGPU k)
	{
		return new InvalidOperationException("SuperAllocAudit: буфер «Xi» раскладки " + k.Layout + " не создан — аудит зовут ПОСЛЕ построения сетки (RebuildFixed).");
	}

	private static bool ProbeKernel(ComputeBuffer b, string name, int bufIndex, SuperParticles p, SuperGpuMaterial[] table, StringBuilder report)
	{
		switch (name)
		{
		case "Pos":
		case "BlockSums":
		case "BlockArgs":
			return Probe(b, name, bufIndex, (Func<int, Vector3>)((int i) => p.x[i]), report);
		case "Vel":
			return Probe(b, name, bufIndex, (Func<int, Vector3>)((int i) => p.v[i]), report);
		case "C":
			return Probe(b, name, bufIndex, (Func<int, SuperMat3>)((int i) => p.C[i]), report);
		case "F":
			return Probe(b, name, bufIndex, (Func<int, SuperMat3>)((int i) => p.F[i]), report);
		case "S":
		case "CellFill":
		case "RegionArgs":
		case "GridV":
			return Probe(b, name, bufIndex, (Func<int, SuperMat3>)((int i) => p.s[i]), report);
		case "Sigma":
		case "CellCount":
		case "BoundsB":
			return Probe(b, name, bufIndex, (Func<int, SuperMat3>)((int i) => p.sigma[i]), report);
		case "Scal":
			return Probe(b, name, bufIndex, (Func<int, Vector4>)((int i) => new Vector4(p.epsP[i], p.epsRate[i], p.T[i], p.e[i])), report);
		case "Mass":
		case "Region":
		case "BlockMark":
			return Probe(b, name, bufIndex, (Func<int, float>)((int i) => p.mass[i]), report);
		case "V0":
		case "Fail":
		case "BlockSlot":
		case "ActiveList":
			return Probe(b, name, bufIndex, (Func<int, float>)((int i) => p.V0[i]), report);
		case "Mat":
			return Probe(b, name, bufIndex, (Func<int, SuperGpuMaterial>)((int i) => table[i]), report);
		case "MatId":
		case "CellStart":
			return Probe(b, name, bufIndex, (Func<int, uint>)((int i) => p.matId[i]), report);
		case "Pinned":
			return Probe(b, name, bufIndex, (Func<int, uint>)((int i) => (p.pinned.IsCreated ? ((uint)p.pinned[i]) : 0u)), report);
		case "NoFrag":
		case "VSums1":
		case "VSums2":
			return Probe(b, name, bufIndex, (Func<int, uint>)((int i) => (p.noFrag.IsCreated ? ((uint)p.noFrag[i]) : 0u)), report);
		case "Dmg":
			return Probe(b, name, bufIndex, (Func<int, float>)((int i) => (p.damage.IsCreated ? p.damage[i] : 0f)), report);
		case "Xi":
			return Probe(b, name, bufIndex, (Func<int, float>)((int i) => (p.xi.IsCreated ? p.xi[i] : 0f)), report);
		default:
			return Probe(b, name, bufIndex, (Func<int, uint>)((Func<int, uint>)((int i) => 0u)), report);
		}
	}

	private static bool Probe<T>(ComputeBuffer b, string name, int bufIndex, Func<int, T> expected, StringBuilder report) where T : struct
	{
		int sizeofT = Marshal.SizeOf(typeof(T));
		if (b.stride != sizeofT)
		{
			return false;
		}
		int count = b.count;
		T[] gpu = new T[count];
		b.GetData(gpu);
		T[] host = new T[count];
		for (int i = 0; i < count; i++)
		{
			host[i] = expected(i);
		}
		bool readable = SameBits(gpu, host);
		if (!readable)
		{
			int[] samples = new int[3] { count / 2, count - 1, bufIndex % Math.Max(count, 1) };
			for (int j = 0; j < samples.Length; j++)
			{
				T[] pattern = Pattern<T>(bufIndex, j);
				FlipLowestBit(pattern);
				b.SetData(pattern);
				T[] echoed = new T[pattern.Length];
				b.GetData(echoed);
				if (!SameBits(pattern, echoed))
				{
					readable = true;
					break;
				}
			}
			b.SetData(gpu);
		}
		if (!readable)
		{
			report.Append("    буфер «").Append(name).Append("» вернул мусор: ");
			report.Append(Hex(gpu)).Append(" / ").AppendLine(Hex(host));
		}
		return readable;
	}

	private static T[] Pattern<T>(int bufIndex, int slot) where T : struct
	{
		uint num = (uint)(bufIndex * 16777619 + slot * 31 + 7);
		int count = 16;
		T[] array = new T[count];
		for (int i = 0; i < count; i++)
		{
			num = num * 16777619u ^ (uint)i;
			array[i] = default(T);
		}
		return array;
	}

	private static void FlipLowestBit<T>(T[] a) where T : struct
	{
		long[] array = new long[a.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = 0L;
		}
		array[0] = 1L;
		Buffer.BlockCopy(array, 0, a, 0, Math.Min(a.Length * Marshal.SizeOf(typeof(T)), array.Length * 8));
	}

	private static bool SameBits<T>(T[] a, T[] b) where T : struct
	{
		if (a.Length != b.Length)
		{
			return false;
		}
		byte[] array = new byte[a.Length * Marshal.SizeOf(typeof(T))];
		byte[] array2 = new byte[b.Length * Marshal.SizeOf(typeof(T))];
		Buffer.BlockCopy(a, 0, array, 0, array.Length);
		Buffer.BlockCopy(b, 0, array2, 0, array2.Length);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != array2[i])
			{
				return false;
			}
		}
		return true;
	}

	private static string Hex<T>(T[] a) where T : struct
	{
		byte[] array = new byte[a.Length * Marshal.SizeOf(typeof(T))];
		Buffer.BlockCopy(a, 0, array, 0, array.Length);
		StringBuilder stringBuilder = new StringBuilder(array.Length * 2);
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.Append(array[i].ToString("x2"));
		}
		return stringBuilder.ToString();
	}

	public static int AliveCheck(SuperKernelGPU k, SuperParticles p)
	{
		if (k == null)
		{
			throw new ArgumentNullException("k");
		}
		if (p == null)
		{
			throw new ArgumentNullException("p");
		}
		if (p.count != k.ParticleCount)
		{
			LastAliveReport = string.Format(CultureInfo.InvariantCulture, "SuperAllocAudit: набор на {0} частиц, ядро на {1} — аудит сверял бы чужое запекание.", p.count, k.ParticleCount);
			return 0;
		}
		List<KeyValuePair<string, ComputeBuffer>> audit = k.AuditBuffers();
		SuperGpuMaterial[] table = k.AuditMaterialTable();
		if (table == null)
		{
			LastAliveReport = "SuperAllocAudit: у ядра нет таблицы веществ — конструктор не дошёл до bMat.SetData.";
			return 0;
		}
		StringBuilder report = new StringBuilder();
		int failures = 0;
		int bufIndex = 0;
		foreach (KeyValuePair<string, ComputeBuffer> row in audit)
		{
			string name = row.Key;
			ComputeBuffer buffer = row.Value;
			SuperScenePlan.GpuBuffer plan = SuperScenePlan.Row(name);
			if (plan.Lazy != plan.In(k.Layout))
			{
				LastAliveReport = "SuperAllocAudit: буфер «Xi» раскладки " + k.Layout + " не создан — аудит зовут ПОСЛЕ построения сетки (RebuildFixed).";
				return 0;
			}
			if (!buffer.IsValid())
			{
				report.Append("SuperAllocAudit: буфер «").Append(name).Append("» освобождён — аудит зовут до Dispose ядра.\n");
				failures++;
				bufIndex++;
				continue;
			}
			if (!ProbeKernel(buffer, name, bufIndex, p, table, report))
			{
				failures++;
			}
			bufIndex++;
		}
		LastAliveReport = report.ToString();
		return failures;
	}

	public static long NativeLogMark()
	{
		string path = LogPath();
		if (path == null)
		{
			return -1L;
		}
		LastNativeLogNote = "";
		try
		{
			using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			return fileStream.Length;
		}
		catch (Exception e)
		{
			if (!IsIo(e))
			{
				throw;
			}
			LastNativeLogNote = "журнал «" + path + "» не открылся: " + e.GetType().FullName + ": " + e;
			return -1L;
		}
	}

	public static int NativeLogFailuresSince(long mark)
	{
		return NativeLogFailuresSince(mark, out int _);
	}

	public static int NativeLogFailuresSince(long mark, out int bufferLines)
	{
		string text = LogPath();
		if (text == null)
		{
			bufferLines = 0;
			return 0;
		}
		return CountNativeFailureLines(text, mark, out bufferLines);
	}

	public static int CountNativeFailureLines(string path, long fromOffset, out int bufferLines)
	{
		bufferLines = 0;
		int num = 0;
		try
		{
			using (StreamReader streamReader = new StreamReader(path))
			{
				streamReader.BaseStream.Position = fromOffset;
				string line;
				while ((line = streamReader.ReadLine()) != null)
				{
					if (IsNativeFailure(line, out var isBuffer))
					{
						num++;
						if (isBuffer)
						{
							bufferLines++;
						}
					}
				}
			}
			LastNativeLogNote = "";
		}
		catch (Exception e)
		{
			if (!IsIo(e))
			{
				throw;
			}
			LastNativeLogNote = "журнал «" + path + "» не читается: " + e.GetType().FullName + ": " + e;
		}
		return num;
	}

	private static bool IsNativeFailure(string line, out bool isBuffer)
	{
		isBuffer = false;
		if (line.Contains("d3d11:"))
		{
			isBuffer = true;
			return true;
		}
		if (line.Contains("d3d12:"))
		{
			isBuffer = true;
			return true;
		}
		if (line.Contains("failed to create buffer"))
		{
			isBuffer = true;
			return true;
		}
		if (line.Contains("failed to create"))
		{
			return true;
		}
		return false;
	}

	private static string LogPath()
	{
		try
		{
			string consoleLogPath = Application.consoleLogPath;
			if (!string.IsNullOrEmpty(consoleLogPath))
			{
				return consoleLogPath;
			}
			LastNativeLogNote = "журнал не пишется в файл (Application.consoleLogPath пуст: -nolog или -logFile -)";
			return null;
		}
		catch (Exception e)
		{
			if (!IsIo(e))
			{
				throw;
			}
			LastNativeLogNote = "журнал не получен: " + e.GetType().FullName + ": " + e;
			return null;
		}
	}

	private static bool IsIo(Exception e)
	{
		return e is IOException || e is UnauthorizedAccessException || e is NotSupportedException;
	}

	public static long AllocatedBytes(SuperKernelGPU k)
	{
		long num = 0L;
		if (k == null)
		{
			return 0L;
		}
		List<KeyValuePair<string, ComputeBuffer>> audit = k.AuditBuffers();
		foreach (KeyValuePair<string, ComputeBuffer> row in audit)
		{
			ComputeBuffer value = row.Value;
			if (value != null && value.IsValid())
			{
				num += (long)value.count * (long)value.stride;
			}
		}
		return num;
	}

	public static List<BufferRow> PerBuffer(SuperKernelGPU k)
	{
		List<BufferRow> list = new List<BufferRow>();
		if (k == null)
		{
			return list;
		}
		SuperScenePlan.Sizes sizes = k.SizesNow;
		List<KeyValuePair<string, ComputeBuffer>> audit = k.AuditBuffers();
		foreach (KeyValuePair<string, ComputeBuffer> row in audit)
		{
			string name = row.Key;
			ComputeBuffer value = row.Value;
			SuperScenePlan.GpuBuffer plan = SuperScenePlan.Row(name);
			bool allocated = value != null && value.IsValid();
			long num = (allocated ? ((long)value.count * value.stride) : 0);
			long tableBytes = plan.Bytes(in sizes);
			list.Add(new BufferRow
			{
				name = name,
				expected = plan.In(k.Layout),
				allocated = allocated,
				allocatedBytes = num,
				tableBytes = tableBytes
			});
		}
		return list;
	}

	internal static int AfterAllocation(SuperReconGpu r, string onlyName = null)
	{
		if (r == null)
		{
			throw new ArgumentNullException("r");
		}
		StringBuilder report = new StringBuilder();
		List<KeyValuePair<string, ComputeBuffer>> audit = r.AuditBuffers();
		int failures = 0;
		int bufIndex = 0;
		foreach (KeyValuePair<string, ComputeBuffer> row in audit)
		{
			string name = row.Key;
			ComputeBuffer buffer = row.Value;
			if (onlyName != null && name != onlyName)
			{
				bufIndex++;
				continue;
			}
			if (!buffer.IsValid())
			{
				throw new InvalidOperationException("SuperAllocAudit: буфер «" + name + "» освобождён — аудит зовут до Dispose ядра.");
			}
			if (!Probe(buffer, name, bufIndex, (Func<int, uint>)((Func<int, uint>)((int i) => (uint)i)), report))
			{
				failures++;
			}
			bufIndex++;
		}
		LastAuditReport = report.ToString();
		return failures;
	}

	internal static long AllocatedBytes(SuperReconGpu r)
	{
		long num = 0L;
		if (r == null)
		{
			return 0L;
		}
		List<KeyValuePair<string, ComputeBuffer>> audit = r.AuditBuffers();
		foreach (KeyValuePair<string, ComputeBuffer> row in audit)
		{
			ComputeBuffer value = row.Value;
			if (value != null && value.IsValid())
			{
				num += (long)value.count * (long)value.stride;
			}
		}
		return num;
	}

	internal static List<BufferRow> PerBuffer(SuperReconGpu r)
	{
		List<BufferRow> list = new List<BufferRow>();
		if (r == null)
		{
			return list;
		}
		List<KeyValuePair<string, ComputeBuffer>> audit = r.AuditBuffers();
		foreach (KeyValuePair<string, ComputeBuffer> row in audit)
		{
			string name = row.Key;
			ComputeBuffer value = row.Value;
			SuperScenePlan.GpuBuffer reconRow = SuperScenePlan.ReconRow(name);
			bool allocated = value != null && value.IsValid();
			long num = (allocated ? ((long)value.count * value.stride) : 0);
			long tableBytes = reconRow.Bytes(r.SizesOf(name));
			list.Add(new BufferRow
			{
				name = name,
				expected = true,
				allocated = allocated,
				allocatedBytes = num,
				tableBytes = tableBytes
			});
		}
		return list;
	}

	public static int PerBufferMismatches(List<BufferRow> rows)
	{
		int num = 0;
		foreach (BufferRow row in rows)
		{
			if (!row.Match)
			{
				num++;
			}
		}
		return num;
	}

	public static string PerBufferText(List<BufferRow> rows)
	{
		StringBuilder stringBuilder = new StringBuilder();
		long num = 0L;
		long num2 = 0L;
		foreach (BufferRow row in rows)
		{
			num += row.allocatedBytes;
			num2 += row.tableBytes;
			string text = ((row.allocatedBytes == row.tableBytes && row.expected == row.allocated) ? "=" : (row.expected ? "⛔ РАСХОДИТСЯ" : "— (нет в раскладке)"));
			stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "    {0,-11} выделено {1,14} Б  по таблице {2,14} Б  ", row.name, row.allocatedBytes, row.tableBytes) + text);
		}
		stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "    ИТОГО       выделено {0,14} Б  по таблице {1,14} Б  {2}", num, num2, (num != num2) ? "⛔ РАСХОДИТСЯ" : "="));
		return stringBuilder.ToString();
	}
}
