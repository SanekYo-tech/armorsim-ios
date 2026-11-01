using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class SuperMachine
{
	[StructLayout((LayoutKind)0, CharSet = CharSet.Auto)]
	private sealed class MemoryStatusEx
	{
		public uint dwLength;

		public uint dwMemoryLoad;

		public ulong ullTotalPhys;

		public ulong ullAvailPhys;

		public ulong ullTotalPageFile;

		public ulong ullAvailPageFile;

		public ulong ullTotalVirtual;

		public ulong ullAvailVirtual;

		public ulong ullAvailExtendedVirtual;
	}

	private struct ProcessMemoryCountersEx
	{
		public uint cb;

		public uint PageFaultCount;

		public UIntPtr PeakWorkingSetSize;

		public UIntPtr WorkingSetSize;

		public UIntPtr QuotaPeakPagedPoolUsage;

		public UIntPtr QuotaPagedPoolUsage;

		public UIntPtr QuotaPeakNonPagedPoolUsage;

		public UIntPtr QuotaNonPagedPoolUsage;

		public UIntPtr PagefileUsage;

		public UIntPtr PeakPagefileUsage;

		public UIntPtr PrivateUsage;
	}

#if !UNITY_IOS && !UNITY_ANDROID
	[PreserveSig]
	[DllImport("kernel32.dll")]
	private static extern bool GlobalMemoryStatusEx([In][Out] MemoryStatusEx buffer);

	[PreserveSig]
	[DllImport("kernel32.dll")]
	private static extern IntPtr GetCurrentProcess();

	[PreserveSig]
	[DllImport("kernel32.dll")]
	private static extern bool GetProcessMemoryInfo(IntPtr process, out ProcessMemoryCountersEx counters, uint size);

	[PreserveSig]
	[DllImport("kernel32.dll")]
	private static extern bool GetProcessTimes(IntPtr process, out long creation, out long exit, out long kernel, out long user);
#endif

	public static bool ProcessCpuMilliseconds(out double ms)
	{
		ms = 0.0;
#if !UNITY_IOS && !UNITY_ANDROID
		long creation = 0L;
		long exit = 0L;
		long kernel = 0L;
		long user = 0L;
		if (GetProcessTimes(GetCurrentProcess(), out creation, out exit, out kernel, out user))
		{
			ms = (double)(kernel + user) / 10000.0;
			return true;
		}
#endif
		return false;
	}

	public static bool ProcessMemory(out long workingSet, out long peakWorkingSet, out long privateBytes)
	{
		workingSet = 0L;
		peakWorkingSet = 0L;
		privateBytes = 0L;
#if !UNITY_IOS && !UNITY_ANDROID
		ProcessMemoryCountersEx counters = new ProcessMemoryCountersEx
		{
			cb = (uint)Marshal.SizeOf(typeof(ProcessMemoryCountersEx))
		};
		if (GetProcessMemoryInfo(GetCurrentProcess(), out counters, counters.cb))
		{
			workingSet = (long)counters.WorkingSetSize.ToUInt64();
			peakWorkingSet = (long)counters.PeakWorkingSetSize.ToUInt64();
			privateBytes = (long)counters.PrivateUsage.ToUInt64();
			return true;
		}
#endif
		return false;
	}

	public static long AvailablePhysicalBytes(out string source)
	{
#if !UNITY_IOS && !UNITY_ANDROID
		MemoryStatusEx status = new MemoryStatusEx
		{
			dwLength = (uint)Marshal.SizeOf(typeof(MemoryStatusEx))
		};
		if (GlobalMemoryStatusEx(status))
		{
			source = "GlobalMemoryStatusEx.ullAvailPhys";
			return (long)status.ullAvailPhys;
		}
#endif
		source = "0.30·SystemInfo.systemMemorySize (ASSUMED)";
		return (long)((double)SystemInfo.systemMemorySize * 0.3 * 1024.0 * 1024.0);
	}
}
