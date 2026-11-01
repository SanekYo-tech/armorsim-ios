using System.Collections.Generic;
using System.Text;

public static class SuperGatingSet
{
	public static readonly string[] Gating;

	public static readonly SuperGateEntry[] Exceptions;

	public static readonly SuperGateCoverage[] Coverage;

	public const string CoverageStatement = "Ворота S0 удостоверяют: перенос частица↔сетка, силовую пару «объём × напряжение», скорость упругой волны и то, что константы вещества доезжают до результата. Они НЕ удостоверяют остаточную длину цилиндра Тейлора на эталонной строке (A1 — объявленное исключение) и НЕ удостоверяют диаметр гриба: он принадлежит трению о наковальню и нигде не утверждается.";

	public static bool IsGating(string id)
	{
		for (int i = 0; i < Gating.Length; i++)
		{
			if (Gating[i] == id)
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryGetException(string id, out SuperGateEntry entry)
	{
		for (int i = 0; i < Exceptions.Length; i++)
		{
			if (Exceptions[i].id == id)
			{
				entry = Exceptions[i];
				return true;
			}
		}
		entry = default(SuperGateEntry);
		return false;
	}

	public static bool TryCoverage(string id, out SuperGateCoverage c)
	{
		for (int i = 0; i < Coverage.Length; i++)
		{
			if (Coverage[i].id == id)
			{
				c = Coverage[i];
				return true;
			}
		}
		c = default(SuperGateCoverage);
		return false;
	}

	public static bool WellFormed(out string problem)
	{
		problem = null;
		HashSet<string> seen = new HashSet<string>();
		for (int i = 0; i < Gating.Length; i++)
		{
			string id = Gating[i];
			if (string.IsNullOrEmpty(id))
			{
				problem = "пустой идентификатор в воротах";
				return false;
			}
			if (!seen.Add(id))
			{
				problem = "дубль в воротах: " + id;
				return false;
			}
		}
		for (int j = 0; j < Exceptions.Length; j++)
		{
			SuperGateEntry superGateEntry = Exceptions[j];
			if (string.IsNullOrEmpty(superGateEntry.id))
			{
				problem = "пустой идентификатор в исключениях";
				return false;
			}
			if (seen.Contains(superGateEntry.id))
			{
				problem = "проверка числится и воротами, и исключением: " + superGateEntry.id;
				return false;
			}
			if (!seen.Add(superGateEntry.id))
			{
				problem = "дубль в исключениях: " + superGateEntry.id;
				return false;
			}
			if (string.IsNullOrEmpty(superGateEntry.reason))
			{
				problem = "исключение без причины: " + superGateEntry.id;
				return false;
			}
			if (superGateEntry.status == SuperGateStatus.Gating)
			{
				problem = "исключение со статусом Gating: " + superGateEntry.id;
				return false;
			}
		}
		for (int k = 0; k < Gating.Length; k++)
		{
			string id2 = Gating[k];
			if (!TryCoverage(id2, out var c))
			{
				problem = "ворота без строки покрытия: " + id2;
				return false;
			}
			if (string.IsNullOrEmpty(c.scene))
			{
				problem = "покрытие без постановки: " + id2;
				return false;
			}
			if (string.IsNullOrEmpty(c.resolution))
			{
				problem = "покрытие без разрешения: " + id2;
				return false;
			}
		}
		return true;
	}

	public static string StatusToken(SuperGateStatus s)
	{
		switch (s)
		{
		case SuperGateStatus.WithinInterlabSpread:
			return "WITHIN_INTERLAB_SPREAD";
		case SuperGateStatus.SummationOrderOnly:
			return "SUMMATION_ORDER_ONLY";
		case SuperGateStatus.ReportOnly:
			return "REPORT_ONLY";
		case SuperGateStatus.QuantizationFloor:
			return "QUANTIZATION_FLOOR";
		case SuperGateStatus.VacuousWhileBaselineRed:
			return "VACUOUS_WHILE_BASELINE_RED";
		default:
			return "GATING";
		}
	}

	public static string Describe()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("НАБОР ВОРОТ SUPER S0 (");
		stringBuilder.Append(Gating.Length);
		stringBuilder.Append("), и что каждые гоняют:\n");
		for (int i = 0; i < Gating.Length; i++)
		{
			stringBuilder.Append("  * ");
			stringBuilder.Append(Gating[i]);
			stringBuilder.Append("  ");
			if (!TryCoverage(Gating[i], out var c))
			{
				stringBuilder.Append("ПОКРЫТИЕ НЕ ОПИСАНО");
			}
			else
			{
				stringBuilder.Append(c.scene);
				stringBuilder.Append("; ");
				stringBuilder.Append(c.resolution);
			}
			stringBuilder.Append('\n');
		}
		stringBuilder.Append("Объявленные исключения (");
		stringBuilder.Append(Exceptions.Length);
		stringBuilder.Append("):\n");
		for (int j = 0; j < Exceptions.Length; j++)
		{
			stringBuilder.Append("  * ");
			stringBuilder.Append(Exceptions[j].id);
			stringBuilder.Append("  ");
			stringBuilder.Append(StatusToken(Exceptions[j].status));
			stringBuilder.Append(" - ");
			stringBuilder.Append(Exceptions[j].reason);
			stringBuilder.Append('\n');
		}
		stringBuilder.Append(CoverageStatement);
		stringBuilder.Append('\n');
		stringBuilder.Append("Состав набора - решение владельца задачи, не вывод из кода.");
		return stringBuilder.ToString();
	}

	static SuperGatingSet()
	{
		Gating = new string[6] { "A2", "A3", "A6", "C1", "C2", "C3" };
		Exceptions = new SuperGateEntry[5]
		{
			new SuperGateEntry
			{
				id = "A1",
				status = SuperGateStatus.WithinInterlabSpread,
				reason = "расхождение +7.73 % при воротах ±5 %; L1 по той же строке даёт 6.82 % и стоит объявленным исключением; промах принадлежит конституционному набору меди, не методу (два метода, один пункт разницы); снимается только брифом по константам меди"
			},
			new SuperGateEntry
			{
				id = "A4",
				status = SuperGateStatus.SummationOrderOnly,
				reason = "Σwᵢ(xᵢ−x_p) ≡ 0 у квадратичного B-сплайна — покраснеть на физике не может; силовую пару сторожит A6"
			},
			new SuperGateEntry
			{
				id = "A5",
				status = SuperGateStatus.ReportOnly,
				reason = "лестница разрешения: ТЗ §2 прямо запрещает утверждать сходимость («тренд не есть сходимость», урок T3a), проверка печатает L_f, D_f и направление изменения и зеленеет от того, что обе ступени досчитались. ⚠️ В ТЗ S1-0 не названа ни воротами, ни исключением — классификация предложена сессией"
			},
			new SuperGateEntry
			{
				id = "C-F3",
				status = SuperGateStatus.VacuousWhileBaselineRed,
				reason = "первая половина («откол обязан исчезнуть под F3») сравнивает ноль с нулём, пока красен S4-2f: откола нет и со здоровым полом давления (замер S13 03.09: кусков 1, откол 0.0 г, max D = 1.00 — плоскость есть, куска нет). НЕ вакуумна вторая половина («механизм убит: max D = 0»): 1.00 → 0.00 под диверсией — живой различитель. Пометка снимается зеленью S4-2f, и только ею"
			},
			new SuperGateEntry
			{
				id = "S1-4d",
				status = SuperGateStatus.QuantizationFloor,
				reason = "маршрут A: сохранение массы 1.557e-5 при воротах 1e-5 — измеренное свойство int32-кодирования на регулярной решётке (REPORT_SUPER_gpu_spike_2026-08-29 §5, §7); физически 0.0016 % за событие, на порядки ниже принятых упрощений физики (§3 роадмапа); условия маршрута: сценический масштаб + сторож на накопителе. Маршрут B (4.7e-7) — эталонный GPU-маршрут для сверок."
			}
		};
		Coverage = new SuperGateCoverage[6]
		{
			new SuperGateCoverage
			{
				id = "A2",
				scene = "цилиндр Тейлора OFHC M1 34.50/7.80 мм @ 162 м/с, наковальня μ = 0.12",
				resolution = "шаг частиц D₀/16, сетка h = 2×шага (8 частиц на ячейку)"
			},
			new SuperGateCoverage
			{
				id = "A3",
				scene = "упругий брусок 50×12 мм о наковальню @ 5 м/с, пластичность выключена",
				resolution = "шаг частиц 0.35 мм"
			},
			new SuperGateCoverage
			{
				id = "A6",
				scene = "однородно сжатый блок 16³ частиц, F = J^⅓·I, один перенос P2G",
				resolution = "шаг частиц 0.5 мм, J = 0.80 / 0.90 / 0.95"
			},
			new SuperGateCoverage
			{
				id = "C1",
				scene = "контрфакт: цилиндр Тейлора A1 с удвоенной плотностью",
				resolution = "шаг частиц D₀/16"
			},
			new SuperGateCoverage
			{
				id = "C2",
				scene = "контрфакт: цилиндр Тейлора A1 без пластичности",
				resolution = "шаг частиц D₀/16"
			},
			new SuperGateCoverage
			{
				id = "C3",
				scene = "контрфакт: цилиндр Тейлора A1 без чтения скоростей с сетки",
				resolution = "шаг частиц D₀/16"
			}
		};
	}
}
