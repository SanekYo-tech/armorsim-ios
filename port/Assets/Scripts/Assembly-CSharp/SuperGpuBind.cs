using System;
using System.Collections.Generic;
using UnityEngine;

public static class SuperGpuBind
{
	public enum Kind
	{
		Uav = 0,
		Srv = 1
	}

	public struct Bind
	{
		public string slot;

		public ComputeBuffer buf;

		public Kind kind;

		public static Bind U(string s, ComputeBuffer b)
		{
			Bind result = default(Bind);
			result.slot = s;
			result.buf = b;
			result.kind = Kind.Uav;
			return result;
		}

		public static Bind R(string s, ComputeBuffer b)
		{
			Bind result = default(Bind);
			result.slot = s;
			result.buf = b;
			result.kind = Kind.Srv;
			return result;
		}
	}

	internal static Action<ComputeShader, int, string, Bind[]> Trace;

	internal static void Checked(ComputeShader cs, int kernel, string name, params Bind[] binds)
	{
		Trace?.Invoke(cs, kernel, name, binds);
		HashSet<ComputeBuffer> uav = new HashSet<ComputeBuffer>();
		HashSet<ComputeBuffer> srv = new HashSet<ComputeBuffer>();
		int uavCount = 0;
		for (int i = 0; i < binds.Length; i++)
		{
			Bind bind = binds[i];
			if (bind.buf == null)
			{
				throw new InvalidOperationException("SUPER: ядро " + name + ", слот " + bind.slot + " — буфер null");
			}
			if (bind.kind == Kind.Uav)
			{
				uavCount++;
				uav.Add(bind.buf);
			}
			else
			{
				srv.Add(bind.buf);
			}
			cs.SetBuffer(kernel, bind.slot, bind.buf);
		}
		foreach (ComputeBuffer b in uav)
		{
			if (srv.Contains(b))
			{
				throw new InvalidOperationException("SUPER: ядро " + name + " привязывает один буфер и как SRV, и как UAV — D3D снимет одну привязку, и диспетч посчитает не то");
			}
		}
		if (uavCount > 8)
		{
			throw new InvalidOperationException(string.Format("SUPER: ядро {0} требует {1} UAV при пределе {2} (D3D11.0). ", name, uavCount, 8) + "Unity пропустит диспетч МОЛЧА — разделите ядро или переведите чтение в SRV.");
		}
	}
}
