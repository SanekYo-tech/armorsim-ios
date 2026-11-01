public static class UltraProvenance
{
	public static bool IsSynthetic(string stamp)
	{
		return stamp == "SYNTHETIC";
	}

	public static bool IsEmpty(string stamp)
	{
		return string.IsNullOrEmpty(stamp);
	}

	public static string Describe(string stamp)
	{
		if (string.IsNullOrEmpty(stamp))
		{
			return "показ пуст";
		}
		if (stamp == "SYNTHETIC")
		{
			return "SYNTHETIC — похоже на нынешний вывод решателя, не физическая истина";
		}
		if (stamp == "R/r0=1.279217/1128/489/S19-single-floor")
		{
			return "РЕШЕНИЕ решателя, отпечаток РАЗОШЁЛСЯ со сборкой — запись устарела";
		}
		return "РЕШЕНИЕ решателя, отпечаток совпадает со сборкой";
	}
}
