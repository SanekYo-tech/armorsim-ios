using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class SuperReconMath
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int CellCoord(float p, float lo, float cell, int n)
	{
		int c = (int)((p - lo) / cell);
		if (c < 0)
		{
			return 0;
		}
		if (c > n - 1)
		{
			return n - 1;
		}
		return c;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static Vector3 BlockNode(Vector3 flo, float bc, int i, int j, int k)
	{
		return new Vector3(flo.x + i * bc, flo.y + j * bc, flo.z + k * bc);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void DensityGrid(Vector3 lo, Vector3 hi, float cell, out Vector3 flo, out int nx, out int ny, out int nz)
	{
		flo = new Vector3(lo.x - cell * 3f, lo.y - cell * 3f, lo.z - cell * 3f);
		nx = (int)Math.Ceiling((double)((hi.x - flo.x) / cell)) + 4;
		ny = (int)Math.Ceiling((double)((hi.y - flo.y) / cell)) + 4;
		nz = (int)Math.Ceiling((double)((hi.z - flo.z) / cell)) + 4;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void HashDims(Vector3 flo, Vector3 hi, float cell, out int nx, out int ny, out int nz)
	{
		nx = (int)Math.Ceiling((double)((hi.x - flo.x) / cell)) + 2;
		ny = (int)Math.Ceiling((double)((hi.y - flo.y) / cell)) + 2;
		nz = (int)Math.Ceiling((double)((hi.z - flo.z) / cell)) + 2;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void BlockDims(Vector3 size, out int bnx, out int bny, out int bnz)
	{
		bnx = Math.Max((int)Math.Ceiling((double)(size.x / 0.004f)) + 1, 2);
		bny = Math.Max((int)Math.Ceiling((double)(size.y / 0.004f)) + 1, 2);
		bnz = Math.Max((int)Math.Ceiling((double)(size.z / 0.004f)) + 1, 2);
	}
}
