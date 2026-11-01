// PARTIAL: восстановлены HasArg, Boot, Line, Fnv, Hash, каркас итератора Start.
// НЕ восстановлены: тело итератора Start (MoveNext), RunT, Smoke, Xfer.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

public sealed class SuperPlayerBench : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CStart_003Ed__6 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public SuperPlayerBench _003C_003E4__this;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CStart_003Ed__6(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			// PARTIAL: тело итератора не восстановлено (SuperPlayerBench_NestedType__Start_d__6, 0x1804B94B0)
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	private bool _smoke;

	private bool _xfer;

	private string _out;

	private static bool HasArg(string a)
	{
		string[] args = Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i] == a)
			{
				return true;
			}
		}
		return false;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		bool smoke = HasArg("-s34smoke");
		bool xfer = HasArg("-s34xfer");
		if (smoke || xfer)
		{
			GameObject go = new GameObject("S34 PlayerBench");
			UnityEngine.Object.DontDestroyOnLoad(go);
			SuperPlayerBench bench = go.AddComponent<SuperPlayerBench>();
			bench._smoke = smoke;
			bench._xfer = xfer;
		}
	}

	private void Line(string s)
	{
		UnityEngine.Debug.Log("[S34 плеер] " + s);
		File.AppendAllText(_out, s + "\n", new UTF8Encoding(false));
	}

	[IteratorStateMachine(typeof(_003CStart_003Ed__6))]
	private IEnumerator Start()
	{
		return new _003CStart_003Ed__6(0)
		{
			_003C_003E4__this = this
		};
	}

	private static ulong Fnv(ulong h, int bits)
	{
		h = (h ^ (ulong)(uint)(bits & 0xFF)) * 1099511628211L;
		h = (h ^ (ulong)(uint)((bits >> 8) & 0xFF)) * 1099511628211L;
		h = (h ^ (ulong)(uint)((bits >> 16) & 0xFF)) * 1099511628211L;
		h = (h ^ (ulong)(uint)((bits >> 24) & 0xFF)) * 1099511628211L;
		return h;
	}

	private static ulong Hash(SuperShot.FinalState fs)
	{
		ulong h = 14695981039346656037L;
		for (int i = 0; i < fs.pos.Length; i++)
		{
			h = Fnv(h, BitConverter.SingleToInt32Bits(fs.pos[i].x));
			h = Fnv(h, BitConverter.SingleToInt32Bits(fs.pos[i].y));
			h = Fnv(h, BitConverter.SingleToInt32Bits(fs.pos[i].z));
			h = Fnv(h, BitConverter.SingleToInt32Bits(fs.vel[i].x));
			h = Fnv(h, BitConverter.SingleToInt32Bits(fs.vel[i].y));
			h = Fnv(h, BitConverter.SingleToInt32Bits(fs.vel[i].z));
			h = Fnv(h, BitConverter.SingleToInt32Bits(fs.damage[i]));
		}
		return h;
	}

	private ulong RunT(ComputeShader cs, SuperKernelGPU.GridPathMode path, out SuperShot.Result r, out double wallS)
	{
		// PARTIAL: тело не восстановлено (0x180708130)
		r = default(SuperShot.Result);
		wallS = 0.0;
		return 0uL;
	}

	private void Smoke()
	{
		// PARTIAL: тело не восстановлено (0x1807098A0)
	}

	private void Xfer()
	{
		// PARTIAL: тело не восстановлено (0x180709910)
	}
}
