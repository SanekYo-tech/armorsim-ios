using System;
using System.Threading;
using Unity.Collections;
using UnityEngine;

public sealed class SuperParticles : IDisposable
{
	public int count;

	public NativeArray<Vector3> x;

	public NativeArray<Vector3> v;

	public NativeArray<SuperMat3> C;

	public NativeArray<SuperMat3> F;

	public NativeArray<SuperMat3> s;

	public NativeArray<SuperMat3> sigma;

	public NativeArray<float> epsP;

	public NativeArray<float> epsRate;

	public NativeArray<float> T;

	public NativeArray<float> e;

	public NativeArray<float> mass;

	public NativeArray<float> V0;

	public NativeArray<byte> matId;

	public NativeArray<byte> pinned;

	public NativeArray<float> damage;

	public NativeArray<byte> noFrag;

	public NativeArray<float> xi;

	private static int _live;

	private bool _disposed;

	public static int Live => Volatile.Read(ref _live);

	public SuperParticles(int n)
	{
		count = n;
		x = new NativeArray<Vector3>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		v = new NativeArray<Vector3>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		C = new NativeArray<SuperMat3>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		F = new NativeArray<SuperMat3>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		s = new NativeArray<SuperMat3>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		sigma = new NativeArray<SuperMat3>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		epsP = new NativeArray<float>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		epsRate = new NativeArray<float>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		T = new NativeArray<float>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		e = new NativeArray<float>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		mass = new NativeArray<float>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		V0 = new NativeArray<float>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		matId = new NativeArray<byte>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		pinned = new NativeArray<byte>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		damage = new NativeArray<float>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		noFrag = new NativeArray<byte>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		xi = new NativeArray<float>(n, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		for (int i = 0; i < n; i++)
		{
			F[i] = SuperMat3.Identity;
		}
		Interlocked.Increment(ref _live);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			Interlocked.Decrement(ref _live);
		}
		if (x.IsCreated)
		{
			x.Dispose();
		}
		if (v.IsCreated)
		{
			v.Dispose();
		}
		if (C.IsCreated)
		{
			C.Dispose();
		}
		if (F.IsCreated)
		{
			F.Dispose();
		}
		if (s.IsCreated)
		{
			s.Dispose();
		}
		if (sigma.IsCreated)
		{
			sigma.Dispose();
		}
		if (epsP.IsCreated)
		{
			epsP.Dispose();
		}
		if (epsRate.IsCreated)
		{
			epsRate.Dispose();
		}
		if (T.IsCreated)
		{
			T.Dispose();
		}
		if (e.IsCreated)
		{
			e.Dispose();
		}
		if (mass.IsCreated)
		{
			mass.Dispose();
		}
		if (V0.IsCreated)
		{
			V0.Dispose();
		}
		if (matId.IsCreated)
		{
			matId.Dispose();
		}
		if (pinned.IsCreated)
		{
			pinned.Dispose();
		}
		if (damage.IsCreated)
		{
			damage.Dispose();
		}
		if (noFrag.IsCreated)
		{
			noFrag.Dispose();
		}
		if (xi.IsCreated)
		{
			xi.Dispose();
		}
	}

	public int PinnedCount()
	{
		int num = 0;
		if (pinned.IsCreated)
		{
			for (int i = 0; i < count; i++)
			{
				if (pinned[i] != 0)
				{
					num++;
				}
			}
		}
		return num;
	}
}
