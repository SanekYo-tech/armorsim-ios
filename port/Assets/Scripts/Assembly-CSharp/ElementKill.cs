public static class ElementKill
{
	public const byte Inverted = 1;

	public const byte PlasticStrain = 2;

	public const byte Volume = 4;

	public const byte DtFloor = 8;

	public const byte Asb = 16;

	public const byte Damage = 32;

	public const byte StrengthLoss = 48;

	public const byte Geometric = 15;

	public const int CauseCount = 6;

	public static string CauseName(int bitIndex)
	{
		switch (bitIndex)
		{
		case 0:
			return "выворот";
		case 1:
			return "ε_p > ε_erode";
		case 2:
			return "V/V₀";
		case 3:
			return "пол Δt";
		case 4:
			return "полоса сдвига";
		case 5:
			return "повреждаемость";
		default:
			return "?";
		}
	}
}
