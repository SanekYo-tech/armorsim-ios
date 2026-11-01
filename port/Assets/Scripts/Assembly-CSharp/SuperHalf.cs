using System;
using UnityEngine;

public static class SuperHalf
{
	private static class EngineNan
	{
		public static readonly uint QuietBit;

		static EngineNan()
		{
			QuietBit = Probe();
		}

		private static uint Probe()
		{
			ushort num = Mathf.FloatToHalf(BitConverter.Int32BitsToSingle(0x7F800001));
			if (num == 0x7F00)
			{
				return 0x200u;
			}
			if (num == 0x7D00)
			{
				return 0u;
			}
			throw new InvalidOperationException(string.Format("Mathf.FloatToHalf(сигнальный NaN 0x7F800001) = 0x{0:X4}, а замер S36 знает 0x7F00 (Mono) и 0x7D00 (IL2CPP) — ", num) + "правило движка сменилось, управляемый перевод half отказан (ворота G-T7)");
		}
	}

	private static readonly float[] Table;

	public static bool ManagedEncode;

	static SuperHalf()
	{
		Table = Build();
		ManagedEncode = SuperTape.ArgInt("-s36halfmanaged", 1) != 0;
	}

	public static float ToFloat(ushort h)
	{
		return Table[h];
	}

	public static ushort FromFloat(float f)
	{
		uint num = (uint)BitConverter.SingleToInt32Bits(f);
		uint num2 = (num >> 16) & 0x8000u;
		uint num3 = num & 0x7FFFFFFFu;
		if (num3 > 0x7F800000u)
		{
			return (ushort)(0x2FFu & (num3 >> 13) | EngineNan.QuietBit | num2 | 0x7D00u);
		}
		if (num3 >= 0x477FF000u)
		{
			return (ushort)(num2 | 0x7C00u);
		}
		if (num3 >= 0x38800000u)
		{
			return (ushort)((num3 - 0x37FFF000u) >> 13 | num2);
		}
		if (num3 < 0x33000000u)
		{
			return (ushort)num2;
		}
		int num4 = (int)(num3 >> 23);
		uint num5 = (num3 & 0x7FFFFFu) | 0x800000u;
		int num6 = 126 - num4;
		return (ushort)((num5 + (1u << (num6 - 1))) >> num6 | num2);
	}

	public static ushort Encode(float f)
	{
		if (ManagedEncode)
		{
			return FromFloat(f);
		}
		return Mathf.FloatToHalf(f);
	}

	public static float ToFloat(byte[] b, int at)
	{
		return Table[b[at] | (b[at + 1] << 8)];
	}

	private static float[] Build()
	{
		float[] array = new float[65536];
		for (int i = 0; i < 65536; i++)
		{
			array[i] = Decode((ushort)i);
		}
		return array;
	}

	private static float Decode(ushort h)
	{
		uint num = (uint)(h & 0x8000) << 16;
		uint num2 = (uint)(h & 0x3FF);
		uint num3 = (uint)(h >> 10) & 0x1Fu;
		if (num3 == 0)
		{
			if (num2 == 0)
			{
				return BitConverter.Int32BitsToSingle((int)num);
			}
			int num4 = -1;
			do
			{
				num2 <<= 1;
				num4++;
			}
			while ((num2 & 0x400u) == 0u);
			return BitConverter.Int32BitsToSingle((int)(0x38000000u - ((uint)num4 << 23) | ((num2 & 0x3FFu) << 13) | num));
		}
		if (num3 == 31)
		{
			return BitConverter.Int32BitsToSingle((int)(((num2 | 0x3FC00u) << 13) | num));
		}
		return BitConverter.Int32BitsToSingle((int)((num3 << 23) + 0x38000000u | (num2 << 13) | num));
	}
}
