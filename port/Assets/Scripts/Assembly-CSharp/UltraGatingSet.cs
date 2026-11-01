using System.Collections.Generic;
using System.Text;

public static class UltraGatingSet
{
	public static readonly UltraGateCoverage[] Coverage = new UltraGateCoverage[8]
	{
		new UltraGateCoverage
		{
			id = "T1",
			geometry = "снаряда нет: брусок AxiMesh.BuildBlock, преграды нет (targetKind = None)",
			resolution = "hOverrideM = 0.25 мм"
		},
		new UltraGateCoverage
		{
			id = "T2",
			geometry = "снаряда нет: один элемент BuildBlock, преграды нет",
			resolution = "hOverrideM = 10 мм"
		},
		new UltraGateCoverage
		{
			id = "T2b",
			geometry = "снаряда нет: три блока BuildBlock, преграды нет",
			resolution = "hOverrideM = 1 мм (и вырожденный элемент 1e-10 м)"
		},
		new UltraGateCoverage
		{
			id = "T4",
			geometry = "ПЛОСКИЙ нос, WHA L/D 10 в полубесконечную RHA, прочность СНЯТА",
			resolution = "D/8"
		},
		new UltraGateCoverage
		{
			id = "T5",
			geometry = "ПЛОСКИЙ нос, сталь 4340 L/D 5 в плиту 4340 30 мм",
			resolution = "D/16"
		},
		new UltraGateCoverage
		{
			id = "T6",
			geometry = "ПЛОСКИЙ нос, WHA L/D 3 в полубесконечную RHA",
			resolution = "своя развёртка D/10 против D/20"
		},
		new UltraGateCoverage
		{
			id = "T7",
			geometry = "сетки нет вовсе: перебор таблицы маршрутизации",
			resolution = "—"
		},
		new UltraGateCoverage
		{
			id = "T8",
			geometry = "ПЛОСКИЙ нос — ПОСЫЛКА проверки: с острым пойдёт вязкое расширение, а не выбивание пробки; WHA в плиту RHA t/D = 0.5",
			resolution = "D/20"
		}
	};

	public static readonly string[] Gating = new string[8] { "T1", "T2", "T2b", "T4", "T5", "T6", "T7", "T8" };

	public static readonly UltraGateEntry[] Exceptions = new UltraGateEntry[3]
	{
		new UltraGateEntry
		{
			id = "T3a",
			status = UltraGateStatus.SelfConsistency,
			reason = "SELF_CONSISTENCY - меряет согласие с моделью этапа 1, а не физику; первичного источника у вилки нет, воротами быть не может"
		},
		new UltraGateEntry
		{
			id = "T3b",
			status = UltraGateStatus.WithinInterlabSpread,
			reason = "расхождение 6.82 % против межлабораторного разброса 4.4 % - тот же порядок, отдельным отказом не является"
		},
		new UltraGateEntry
		{
			id = "V4",
			status = UltraGateStatus.RepresentationDefect,
			reason = "замеренный и охарактеризованный дефект ПОКАЗА (обломок впереди носа), решателя не касается"
		}
	};

	public const string CoverageKey = "ultra.gates.coverage";

	public const string CoverageStatement = "Набор ворот удостоверяет РЕШАТЕЛЬ на перечисленных выше постановках. Форму носа и разрешение сетки принимают извне только T5 и T6; остальные шесть снаряда не строят вовсе (T1, T2, T2b, T7), снимают прочность целиком и потому к форме носа безразличны (T4) или держат тупой нос своей ПОСЫЛКОЙ (T8). Выбранные вами разрешение сетки и форма носа отдельно НЕ удостоверены и у T5 с T6: набор регистрирует их собственный тупоносый прогон на их собственных разрешениях, а не ваш.";

	public static bool TryCoverage(string id, out UltraGateCoverage c)
	{
		for (int i = 0; i < Coverage.Length; i++)
		{
			if (Coverage[i].id == id)
			{
				c = Coverage[i];
				return true;
			}
		}
		c = default;
		return false;
	}

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

	public static bool TryGetException(string id, out UltraGateEntry entry)
	{
		for (int i = 0; i < Exceptions.Length; i++)
		{
			if (Exceptions[i].id == id)
			{
				entry = Exceptions[i];
				return true;
			}
		}
		entry = default;
		return false;
	}

	public static bool WellFormed(out string problem)
	{
		HashSet<string> set = new HashSet<string>();
		for (int i = 0; i < Gating.Length; i++)
		{
			if (string.IsNullOrEmpty(Gating[i]))
			{
				problem = "пустой идентификатор в воротах";
				return false;
			}
			if (!set.Add(Gating[i]))
			{
				problem = "дубль в воротах: " + Gating[i];
				return false;
			}
		}
		for (int j = 0; j < Exceptions.Length; j++)
		{
			if (string.IsNullOrEmpty(Exceptions[j].id))
			{
				problem = "пустой идентификатор в исключениях";
				return false;
			}
			if (set.Contains(Exceptions[j].id))
			{
				problem = "проверка числится и воротами, и исключением: " + Exceptions[j].id;
				return false;
			}
			if (!set.Add(Exceptions[j].id))
			{
				problem = "дубль в исключениях: " + Exceptions[j].id;
				return false;
			}
			if (string.IsNullOrEmpty(Exceptions[j].reason))
			{
				problem = "исключение без причины: " + Exceptions[j].id;
				return false;
			}
			if (Exceptions[j].status == UltraGateStatus.Gating)
			{
				problem = "исключение со статусом Gating: " + Exceptions[j].id;
				return false;
			}
		}
		for (int k = 0; k < Gating.Length; k++)
		{
			if (!TryCoverage(Gating[k], out var coverage))
			{
				problem = "ворота без строки покрытия: " + Gating[k];
				return false;
			}
			if (string.IsNullOrEmpty(coverage.geometry))
			{
				problem = "покрытие без геометрии: " + Gating[k];
				return false;
			}
			if (string.IsNullOrEmpty(coverage.resolution))
			{
				problem = "покрытие без разрешения: " + Gating[k];
				return false;
			}
		}
		problem = null;
		return true;
	}

	public static string StatusToken(UltraGateStatus s)
	{
		switch (s)
		{
		case UltraGateStatus.SelfConsistency:
			return "SELF_CONSISTENCY";
		case UltraGateStatus.WithinInterlabSpread:
			return "WITHIN_INTERLAB_SPREAD";
		case UltraGateStatus.RepresentationDefect:
			return "REPRESENTATION_DEFECT";
		default:
			return "GATING";
		}
	}

	public static string Describe()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("НАБОР ВОРОТ РЕШАТЕЛЯ (");
		stringBuilder.Append(Gating.Length);
		stringBuilder.Append("), и что каждые гоняют:\n");
		for (int i = 0; i < Gating.Length; i++)
		{
			stringBuilder.Append("  * ");
			stringBuilder.Append(Gating[i]);
			stringBuilder.Append("  ");
			if (TryCoverage(Gating[i], out var coverage))
			{
				stringBuilder.Append(coverage.geometry);
				stringBuilder.Append("; сетка: ");
				stringBuilder.Append(coverage.resolution);
			}
			else
			{
				stringBuilder.Append("ПОКРЫТИЕ НЕ ОПИСАНО");
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
}
