public static class RadarMapRng
{
	public const ulong Gamma = 11400714819323198485uL;

	private const ulong Mix1 = 13787848793156543929uL;

	private const ulong Mix2 = 10723151780598845931uL;

	public const ulong IndexMultiplier = 1099511628211uL;

	public const double Unit53 = 1.1102230246251565E-16;

	public static ulong Next(ref ulong state)
	{
		state += Gamma;
		ulong num = state;
		num ^= num >> 30;
		num *= Mix1;
		num ^= num >> 27;
		num *= Mix2;
		num ^= num >> 31;
		return num;
	}

	public static double NextUnit(ref ulong state)
	{
		return (double)(Next(ref state) >> 11) * Unit53;
	}

	public static ulong AtlasState(ulong seed, long cell, int sample)
	{
		return seed ^ IndexMultiplier * (ulong)((cell << 16) + sample);
	}

	public static ulong Key(ulong seed, params long[] parts)
	{
		ulong num = seed;
		if (parts == null)
		{
			return num;
		}
		for (int i = 0; i < parts.Length; i++)
		{
			ulong num2 = (ulong)parts[i] * IndexMultiplier ^ num;
			num2 += Gamma;
			num2 ^= num2 >> 30;
			num2 *= Mix1;
			num2 ^= num2 >> 27;
			num2 *= Mix2;
			num2 ^= num2 >> 31;
			num = num2;
		}
		return num;
	}

	public static long Part(string text)
	{
		ulong num = 14695981039346656037uL;
		text = (text ?? string.Empty);
		for (int i = 0; i < text.Length; i++)
		{
			num ^= text[i];
			num *= IndexMultiplier;
		}
		return (long)num;
	}
}
