public static class SuperShell
{
	public static float CoreDiameterFraction;

	public static float CapLengthFraction;

	public const string SoftSteel = "STEEL4340";

	public const string HardCore = "HARDCORE_N12E";

	public static bool FromDefinition(ProjectileDefinition d, bool jacketed, out SuperShellSpec shell, out string reason)
	{
		shell = default;
		reason = "";
		if (d == null)
		{
			reason = "определения снаряда нет";
			return false;
		}
		return Build(d, d.diameterMM, d.rodLengthMM, d.velocityMps, null, jacketed, out shell, out reason, out _);
	}

	public static bool FromDefinition(ProjectileDefinition d, bool jacketed, out SuperShellSpec shell, out string reason, out string passport)
	{
		shell = default;
		reason = "";
		passport = "";
		if (d == null)
		{
			reason = "определения снаряда нет";
			return false;
		}
		return Build(d, d.diameterMM, d.rodLengthMM, d.velocityMps, null, jacketed, out shell, out reason, out passport);
	}

	public static bool FromLive(ProjectileDefinition d, float diameterMM, float rodLengthMM, float velocityMps, string coreMaterial, bool jacketed, out SuperShellSpec shell, out string reason, out string passport)
	{
		shell = default;
		reason = "";
		passport = "";
		if (d == null)
		{
			reason = "определения снаряда нет";
			return false;
		}
		return Build(d, diameterMM, rodLengthMM, velocityMps, coreMaterial, jacketed, out shell, out reason, out passport);
	}

	private static bool Build(ProjectileDefinition d, float diameterMM, float rodLengthMM, float velocityMps, string coreMaterialFromScene, bool jacketed, out SuperShellSpec shell, out string reason, out string passport)
	{
		shell = default;
		reason = "";
		passport = "";
		return false;
	}

	private static string Passport(ProjectileDefinition d, bool jacketed, bool coreFracFromRow, bool capFracFromRow, bool capMatFromRow, bool coreMatFromRow)
	{
		System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
		if (jacketed)
		{
			stringBuilder.Append(string.Format("доля калибра под сердечником {0:F3} — ", coreFracFromRow ? d.coreDiameterFrac : CoreDiameterFraction)).Append(coreFracFromRow ? "ИЗ СТРОКИ КАТАЛОГА. " : "ЗАПАСНАЯ КОНСТАНТА (ASSUMED 01.09, источника нет). ");
		}
		if (d.ballisticCap)
		{
			stringBuilder.Append(string.Format("доля длины под колпаком {0:F3} — ", capFracFromRow ? d.capLengthFrac : CapLengthFraction)).Append(capFracFromRow ? "ИЗ СТРОКИ КАТАЛОГА. " : "ЗАПАСНАЯ КОНСТАНТА (ASSUMED 01.09, источника нет). ").Append("вещество колпака " + (capMatFromRow ? d.capMaterialId : SoftSteel) + " — ").Append(capMatFromRow ? "ИЗ СТРОКИ КАТАЛОГА. " : "ЗАПАСНОЕ ВЕЩЕСТВО (выбор по реестру, S8). ");
		}
		if (d.kind != 0)
		{
			stringBuilder.Append("вещество сердечника " + (coreMatFromRow ? d.coreMaterialId : HardCore) + " — ").Append(coreMatFromRow ? "ИЗ СТРОКИ КАТАЛОГА. " : "ЗАПАСНОЕ ВЕЩЕСТВО (выбор по реестру, S9/S12). ");
		}
		return stringBuilder.ToString().TrimEnd();
	}
}
