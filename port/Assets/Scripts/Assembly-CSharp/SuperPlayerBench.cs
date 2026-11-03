// PARTIAL: восстановлены HasArg, Boot, Line, Fnv, Hash, Smoke, Xfer, каркас итератора Start.
// НЕ восстановлены: тело итератора Start (MoveNext), RunT.
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
		double wallS = 0.0;
		double wallS2 = 0.0;
		ComputeShader computeShader = Resources.Load<ComputeShader>("SuperSolver");
		if (computeShader == null)
		{
			Line("SMOKE: Resources.Load<ComputeShader>(\"SuperSolver\") = null");
			return;
		}
		int sparseSelfChecksPassed = SuperKernelGPU.SparseSelfChecksPassed;
		SuperShot.Result r;
		ulong num = RunT(computeShader, SuperKernelGPU.GridPathMode.Sparse, out r, out wallS);
		int num2 = SuperKernelGPU.SparseSelfChecksPassed - sparseSelfChecksPassed;
		Line(string.Format("SMOKE T Sparse: fp={0:X16} ok={1} шагов {2}/{3} стена {4:F1} с «{5}»; самопроверок G-S3 прошло {6}", num, r.ok, r.stepsDone, r.steps, wallS, r.failure, num2));
		SuperShot.Result r2;
		ulong num3 = RunT(computeShader, SuperKernelGPU.GridPathMode.Region, out r2, out wallS2);
		Line(string.Format("SMOKE T Region: fp={0:X16} ok={1} шагов {2}/{3} стена {4:F1} с «{5}»", num3, r2.ok, r2.stepsDone, r2.steps, wallS2, r2.failure));
		bool flag = num2 == 1 && r.ok && num != 0 && num == num3;
		Line(string.Concat(new string[6]
		{
			"SMOKE ИТОГ: ",
			flag ? "ЗЕЛЕНО" : "КРАСНО",
			" — G-S3 ",
			(num2 == 1) ? "прошла" : "НЕ прошла",
			", отпечаток Sparse ",
			string.Format("{0} Region на {1}", (num == num3) ? "=" : "≠", SystemInfo.graphicsDeviceType)
		}));
	}

	private void Xfer()
	{
		int[] array = new int[2] { 64, 256 };
		for (int i = 0; i < array.Length; i++)
		{
			int num = array[i];
			int num2 = num << 18;
			Unity.Collections.NativeArray<float> nativeArray = new Unity.Collections.NativeArray<float>(num2, Unity.Collections.Allocator.Persistent, Unity.Collections.NativeArrayOptions.UninitializedMemory);
			float[] array2 = new float[num2];
			float[] array3 = new float[1];
			ComputeBuffer computeBuffer = new ComputeBuffer(num2, 4);
			for (int j = 0; j < num2; j++)
			{
				nativeArray[j] = (float)j * 0.5f;
			}
			computeBuffer.SetData(nativeArray);
			computeBuffer.GetData(array2, 0, 0, 1);
			computeBuffer.GetData(array2);
			UnityEngine.Rendering.AsyncGPUReadbackRequest asyncGPUReadbackRequest = UnityEngine.Rendering.AsyncGPUReadback.RequestIntoNativeArray(ref nativeArray, computeBuffer);
			asyncGPUReadbackRequest.WaitForCompletion();
			for (int k = 1; k < 6; k++)
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				computeBuffer.SetData(nativeArray);
				computeBuffer.GetData(array2, 0, 0, 1);
				double num3 = stopwatch.Elapsed.TotalSeconds;
				stopwatch.Restart();
				computeBuffer.GetData(array2);
				double num4 = stopwatch.Elapsed.TotalSeconds;
				stopwatch.Restart();
				asyncGPUReadbackRequest = UnityEngine.Rendering.AsyncGPUReadback.RequestIntoNativeArray(ref nativeArray, computeBuffer);
				asyncGPUReadbackRequest.WaitForCompletion();
				double num5 = stopwatch.Elapsed.TotalSeconds;
				Line(string.Format(System.Globalization.CultureInfo.InvariantCulture, "S34X size={0}MB rep={1} setMs={2:F2} setMBps={3:F0} getMs={4:F2} getMBps={5:F0} asyncMs={6:F2} asyncMBps={7:F0} asyncErr={8}", num, k, num3 * 1000.0, (double)num / num3, num4 * 1000.0, (double)num / num4, num5 * 1000.0, (double)num / num5, asyncGPUReadbackRequest.hasError));
			}
			float num6 = array2[num2 - 1];
			bool flag;
			if (Mathf.Abs(num6 - (float)(num2 - 1) * 0.5f) < 0.001f)
			{
				float num7 = nativeArray[num2 / 2];
				flag = Mathf.Abs(num7 - (float)(num2 / 2) * 0.5f) < 0.001f;
			}
			else
			{
				flag = false;
			}
			Line(string.Format("S34X size={0}MB данные доехали туда и обратно: {1}", num, flag ? "да" : "НЕТ"));
			computeBuffer.Dispose();
			nativeArray.Dispose();
		}
	}
}
