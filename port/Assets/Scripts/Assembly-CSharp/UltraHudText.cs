using System.Text;

public static class UltraHudText
{
	public const string EtaUnknown = "—";

	public static string Fraction(UltraProgress p)
	{
		if (p.stepsTotal <= 0)
		{
			return "0 %";
		}
		if (p.stepsEstimate > 0 && p.stepsDone >= p.stepsEstimate)
		{
			return string.Format("шагов {0} — оценка ({1}) превышена, ", p.stepsDone, p.stepsEstimate) + "сколько осталось, неизвестно";
		}
		float num = 0f;
		if (p.stepsTotal > 0)
		{
			num = (float)p.stepsDone / p.stepsTotal;
			if (num < 0f)
			{
				num = 0f;
			}
			else if (num > 1f)
			{
				num = 1f;
			}
		}
		return string.Format("{0:F0} %  ({1} / {2})", num * 100f, p.stepsDone, p.stepsTotal);
	}

	public static string Elapsed(UltraProgress p)
	{
		return string.Format("{0:F0} с", p.elapsedS);
	}

	public static string Eta(UltraProgress p)
	{
		if (p.stepsDone >= 200 && p.stepsTotal > p.stepsDone)
		{
			double num = (double)(p.stepsTotal - p.stepsDone) * p.elapsedS / p.stepsDone;
			if (num >= 0.0)
			{
				if (num < 90.0)
				{
					return string.Format("около {0:F0} с", num);
				}
				return string.Format("около {0:F0} мин", num / 60.0);
			}
		}
		return "—";
	}

	public static string EtaTooltip()
	{
		return string.Format("Оценка появляется после {0} шагов: раньше темп ", 200) + "нестабилен, и число прыгало бы на порядки.";
	}

	public static string CacheSize(long bytes, int entries)
	{
		return string.Format("Кэш: {0:F1} МБ, записей {1}", (double)bytes * 9.5367431640625E-07, entries);
	}

	public static string RungEstimate(UltraRung r)
	{
		return string.Format("D/{0:F0} — оценка около {1:F0} мин", r.elementsPerDiameter, r.refWallSeconds * UltraQualityLadder.MachineFactor / 60f) + (UltraQualityLadder.Benchmarked ? "" : " (замер машины не делался)");
	}

	public static string Rungs()
	{
		UltraRung[] array = UltraQualityLadder.Available();
		if (array.Length == 0)
		{
			return "Ступеней НЕТ — лестница пуста";
		}
		StringBuilder stringBuilder = new StringBuilder("Ступени: ");
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.Append((i > 0) ? ", " : "");
			stringBuilder.Append(string.Format("D/{0:F0}", array[i].elementsPerDiameter));
		}
		return stringBuilder.ToString();
	}

	public static string GateSet()
	{
		return UltraGatingSet.Describe();
	}

	public static string DeclarationCaption(ProjectedScenario p)
	{
		if (p == null || (!p.isLossy && p.layerCount <= 1))
		{
			return "";
		}
		if (p.layerCount <= 1)
		{
			return Localization.T("ultra.declaration.caption.tilt", p.maxTiltDeg.ToString("F0"), p.rawThicknessMM.ToString("F0"), p.losThicknessMM.ToString("F0"));
		}
		if (p.isLossy)
		{
			return Localization.T("ultra.declaration.caption.stack_tilt", p.layerCount, p.maxTiltDeg.ToString("F0"), p.rawThicknessMM.ToString("F0"), p.losThicknessMM.ToString("F0"));
		}
		return Localization.T("ultra.declaration.caption.stack", p.layerCount, p.rawThicknessMM.ToString("F0"));
	}

	public static string DeclarationPanel(ProjectedScenario p)
	{
		if (p == null || (!p.isLossy && p.layerCount <= 1))
		{
			return "";
		}
		return DeclarationCaption(p) + "\n\n" + Localization.T(p.declarationKey);
	}

	public static bool NeedsGhost(ProjectedScenario p)
	{
		return p != null && p.maxTiltDeg > 0.0001f;
	}

	public static string GhostLegend(ProjectedScenario p)
	{
		if (!NeedsGhost(p))
		{
			return "";
		}
		return Localization.T("ultra.ghost.legend", p.rawThicknessMM.ToString("F0"), p.maxTiltDeg.ToString("F0"), p.losThicknessMM.ToString("F0"));
	}

	public static string GhostSwitch(ProjectedScenario p, bool on, string keyName)
	{
		if (!NeedsGhost(p))
		{
			return "";
		}
		return "\n\n" + Localization.T(on ? "ultra.ghost.on" : "ultra.ghost.off", keyName);
	}

	public static string ModeEntry(string reasonKey)
	{
		if (reasonKey == "ultra.ok")
		{
			return "УЛЬТРА: доступна";
		}
		if (reasonKey == "ultra.reject.no_rungs")
		{
			return "УЛЬТРА недоступна: в лестнице качества нет ни одной ступени";
		}
		if (reasonKey == "ultra.reject.fingerprint")
		{
			return "УЛЬТРА недоступна: отпечаток решателя разошёлся со сборкой — прежние записи показа недействительны";
		}
		if (reasonKey == "ultra.reject.synthetic")
		{
			return "УЛЬТРА недоступна: загружена СИНТЕТИЧЕСКАЯ оснастка, а не решение — это дефект сборки, а не устаревание";
		}
		if (reasonKey == "ultra.reject.routing")
		{
			return "УЛЬТРА недоступна: маршрут бэкендов нарушен";
		}
		return "УЛЬТРА недоступна: " + reasonKey;
	}

	public static string FieldName(UltraField f)
	{
		if (f == UltraField.Temperature)
		{
			return "температура";
		}
		if (f == UltraField.Pressure)
		{
			return "давление";
		}
		return "пластическая деформация";
	}

	public static string FieldLegend(UltraField f)
	{
		return FieldName(f) + " — относительная шкала, диапазон этого кадра (от меньшего к большему)";
	}

	public static bool LegendHasNoNumbers(string legend)
	{
		for (int i = 0; i < legend.Length; i++)
		{
			if (legend[i] >= '0' && legend[i] <= '9')
			{
				return false;
			}
		}
		return true;
	}
}
