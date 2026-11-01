using System;
using UnityEngine;

public static class SuperMemory
{
	public const float VramShareAllowed = 0.8f;

	public const string VramShareProvenance = "ASSUMED: Docs/DECISION_2026-09-19_the_memory_floor.md §3 (было 0.60 от graphicsMemorySize); 0.80 — слово владельца 26.09.2026 (S38b); бюджет D3D12 «App VRAM Budget» API не отдаёт";

	public static float BenchShareOverride;

	public const long ResourceCapFloorBytes = 134217728L;

	public const long ResourceCapCeilBytes = 2147483648L;

	public const double ResourceCapShare = 0.25;

	public static bool BenchIgnoreResourceCap;

	public static long DeviceBytes
	{
		get
		{
			int size = SystemInfo.graphicsMemorySize;
			if (size <= 0)
			{
				return 0L;
			}
			return (long)size << 20;
		}
	}

	public static long AllowedBytes
	{
		get
		{
			if (DeviceBytes <= 0)
			{
				return 0L;
			}
			float share = (BenchShareOverride > 0f) ? BenchShareOverride : VramShareAllowed;
			return (long)((float)DeviceBytes * share);
		}
	}

	public static long PerResourceCapBytes
	{
		get
		{
			if (BenchIgnoreResourceCap)
			{
				return long.MaxValue;
			}
			long device = DeviceBytes;
			return Math.Min(Math.Max((long)((double)device * ResourceCapShare), ResourceCapFloorBytes), ResourceCapCeilBytes);
		}
	}

	public static SuperScenePlan.Layout LayoutFor(SuperScatterRoute route)
	{
		if (route == SuperScatterRoute.CellSortGather)
		{
			return SuperScenePlan.Layout.DenseB;
		}
		if (SuperKernelGPU.GridPath != SuperKernelGPU.GridPathMode.Sparse)
		{
			return SuperScenePlan.Layout.DenseA;
		}
		if (!SuperKernelGPU.SabotageForce1D)
		{
			return SuperScenePlan.Layout.Sparse;
		}
		return SuperScenePlan.Layout.DenseA;
	}

	public static double BytesPerParticle(SuperScatterRoute route)
	{
		return SuperScenePlan.BytesPerUnit(LayoutFor(route), SuperScenePlan.Per.Particle, SuperKernelGPU.TwoFieldContact);
	}

	public static double BytesPerNode(SuperScatterRoute route)
	{
		return SuperScenePlan.BytesPerUnit(LayoutFor(route), SuperScenePlan.Per.Node, SuperKernelGPU.TwoFieldContact);
	}
}
