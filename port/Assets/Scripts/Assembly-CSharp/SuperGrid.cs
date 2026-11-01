using System;
using Unity.Collections;
using UnityEngine;

public sealed class SuperGrid : IDisposable
{
	public const int Margin = 2;

	public float h;

	public Vector3 origin;

	public int nx;

	public int ny;

	public int nz;

	public NativeArray<float> mass;

	public NativeArray<Vector3> mom;

	private int capacity;

	public int NodeCount => nz * ny * nx;

	public static Vector3 OriginFor(Vector3 lo, float h)
	{
		return new Vector3(Mathf.Floor(lo.x / h) * h - Margin * h, Mathf.Floor(lo.y / h) * h - Margin * h, Mathf.Floor(lo.z / h) * h - Margin * h);
	}

	public static void Dims(Vector3 lo, Vector3 hi, float h, out int nx, out int ny, out int nz)
	{
		Vector3 origin = OriginFor(lo, h);
		nx = (int)Math.Ceiling((hi.x - origin.x) / h) + Margin + 1;
		ny = (int)Math.Ceiling((hi.y - origin.y) / h) + Margin + 1;
		nz = (int)Math.Ceiling((hi.z - origin.z) / h) + Margin + 1;
	}

	public static long NodeCountFor(Vector3 lo, Vector3 hi, float h)
	{
		Dims(lo, hi, h, out int nx, out int ny, out int nz);
		return (long)nx * ny * nz;
	}

	public SuperGrid(float cellSize)
	{
		h = cellSize;
		capacity = 0;
	}

	public int Index(int ix, int iy, int iz)
	{
		return (iz * ny + iy) * nx + ix;
	}

	public Vector3 NodePos(int ix, int iy, int iz)
	{
		return new Vector3(ix * h + origin.x, iy * h + origin.y, iz * h + origin.z);
	}

	public bool Reset(SuperParticles p)
	{
		float minX = float.MaxValue;
		float minY = float.MaxValue;
		float minZ = float.MaxValue;
		float maxX = -float.MaxValue;
		float maxY = -float.MaxValue;
		float maxZ = -float.MaxValue;
		for (int i = 0; i < p.count; i++)
		{
			Vector3 v = p.x[i];
			if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z))
			{
				return false;
			}
			if (v.x < minX)
			{
				minX = v.x;
			}
			if (v.x > maxX)
			{
				maxX = v.x;
			}
			if (v.y < minY)
			{
				minY = v.y;
			}
			if (v.y > maxY)
			{
				maxY = v.y;
			}
			if (v.z < minZ)
			{
				minZ = v.z;
			}
			if (v.z > maxZ)
			{
				maxZ = v.z;
			}
		}
		Vector3 lo = new Vector3(minX, minY, minZ);
		Vector3 hi = new Vector3(maxX, maxY, maxZ);
		origin = OriginFor(lo, h);
		Dims(lo, hi, h, out nx, out ny, out nz);
		int need = nz * ny * nx;
		if (need > capacity)
		{
			if (mass.IsCreated)
			{
				mass.Dispose();
			}
			if (mom.IsCreated)
			{
				mom.Dispose();
			}
			capacity = need + need / 4;
			mass = new NativeArray<float>(capacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
			mom = new NativeArray<Vector3>(capacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		}
		for (int i = 0; i < need; i++)
		{
			mass[i] = 0f;
			mom[i] = Vector3.zero;
		}
		return true;
	}

	public void Weights(Vector3 xp, out int bx, out int by, out int bz, out Vector3 w0, out Vector3 w1, out Vector3 w2)
	{
		float gx = (xp.x - origin.x) / h;
		float gy = (xp.y - origin.y) / h;
		float gz = (xp.z - origin.z) / h;
		bx = (int)Math.Floor(gx - 0.5f);
		by = (int)Math.Floor(gy - 0.5f);
		bz = (int)Math.Floor(gz - 0.5f);
		float dx = gx - bx;
		float dy = gy - by;
		float dz = gz - bz;
		w0 = new Vector3(0.5f * (1.5f - dx) * (1.5f - dx), 0.5f * (1.5f - dy) * (1.5f - dy), 0.5f * (1.5f - dz) * (1.5f - dz));
		w1 = new Vector3(0.75f - (dx - 1f) * (dx - 1f), 0.75f - (dy - 1f) * (dy - 1f), 0.75f - (dz - 1f) * (dz - 1f));
		w2 = new Vector3(0.5f * (dx - 0.5f) * (dx - 0.5f), 0.5f * (dy - 0.5f) * (dy - 0.5f), 0.5f * (dz - 0.5f) * (dz - 0.5f));
	}

	public void Dispose()
	{
		if (mass.IsCreated)
		{
			mass.Dispose();
		}
		if (mom.IsCreated)
		{
			mom.Dispose();
		}
	}
}
