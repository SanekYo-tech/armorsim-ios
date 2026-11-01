using System;
using System.Collections.Generic;
using UnityEngine;

public static class ControlChecks
{
	private class Snap
	{
		public Dictionary<ControlAction, KeyCode> keys;

		public Dictionary<ControlModifier, ModifierKind> mods;

		public bool orbitRight;

		public bool invert3D;

		public bool invert2D;
	}

	private static Action<string> _log = delegate(string _)
	{
	};

	private static Action<string> _err = delegate(string _)
	{
	};

	private static readonly Language[] Required = new Language[4]
	{
		Language.RU,
		Language.EN,
		Language.DE,
		Language.PL
	};

	private const Language MustBeEmpty = Language.TT;

	private static void Log(string s)
	{
		_log(s);
	}

	private static void Err(string s)
	{
		_err(s);
	}

	public static bool RunAll(Action<string> log, Action<string> err, out int pass, out int total)
	{
		_log = log ?? delegate(string _)
		{
		};
		_err = err ?? delegate(string _)
		{
		};
		Log("=== Управление: самопроверка ===");
		Snap snap = Snapshot();
		pass = 0;
		total = 0;
		try
		{
			RunOne(1, "★ ТАБЛИЦА: каждое действие и каждая роль описаны ровно один раз", Test1Impl, ref pass, ref total);
			RunOne(2, "★ ОБЛАСТИ: только песочница и 2D; ULTRA и Super НЕ заявлены", Test2Impl, ref pass, ref total);
			RunOne(3, "умолчания = исторические клавиши и внутри области не повторяются", Test3Impl, ref pass, ref total);
			RunOne(4, "разрешённое и закреплённое не пересекаются; умолчания разрешены", Test4Impl, ref pass, ref total);
			RunOne(5, "★ СТОЛКНОВЕНИЕ клавиш: отказ именует занявшее действие", Test5Impl, ref pass, ref total);
			RunOne(6, "★ КОНТРФАКТ: свободная клавиша ПРИНИМАЕТСЯ (проверка умеет зеленеть)", Test6Impl, ref pass, ref total);
			RunOne(7, "закреплённые клавиши отклоняются своей причиной, а не общей", Test7Impl, ref pass, ref total);
			RunOne(8, "★ КАНАЛ модификаторов: колесо 2D сталкивается, камера и выбор — нет", Test8Impl, ref pass, ref total);
			RunOne(9, "★ ПРАВАЯ КНОПКА: один хозяин — камера ИЛИ отмена захвата", Test9Impl, ref pass, ref total);
			RunOne(10, "знак колеса и сброс области: соседняя область не задета", Test10Impl, ref pass, ref total);
			RunOne(11, "★ СТРОКИ: RU/EN/DE/PL заполнены, татарский пуст (указание владельца)", Test11Impl, ref pass, ref total);
			RunOne(12, "★ КОНТРФАКТ покрытия строк: подделанный ключ валит проверку 11", Test12Impl, ref pass, ref total);
			RunOne(13, "подсказки с клавишами берут аргументы, а не буквы в переводе", Test13Impl, ref pass, ref total);
		}
		finally
		{
			Restore(snap);
		}
		string summary = string.Format("=== Итог управления: {0}/{1} PASS ===", pass, total);
		if (pass == total)
		{
			Log(summary);
			return true;
		}
		Err(summary);
		return false;
	}

	public static bool RunSingle(int n, Action<string> log, Action<string> err)
	{
		_log = log ?? delegate(string _)
		{
		};
		_err = err ?? delegate(string _)
		{
		};
		Snap snap = Snapshot();
		int pass = 0;
		int total = 0;
		try
		{
			switch (n)
			{
			case 1:
				RunOne(1, "таблица действий и ролей", Test1Impl, ref pass, ref total);
				break;
			case 2:
				RunOne(2, "области", Test2Impl, ref pass, ref total);
				break;
			case 3:
				RunOne(3, "умолчания", Test3Impl, ref pass, ref total);
				break;
			case 4:
				RunOne(4, "разрешённое и закреплённое", Test4Impl, ref pass, ref total);
				break;
			case 5:
				RunOne(5, "столкновение клавиш", Test5Impl, ref pass, ref total);
				break;
			case 6:
				RunOne(6, "контрфакт столкновения", Test6Impl, ref pass, ref total);
				break;
			case 7:
				RunOne(7, "закреплённые клавиши", Test7Impl, ref pass, ref total);
				break;
			case 8:
				RunOne(8, "канал модификаторов", Test8Impl, ref pass, ref total);
				break;
			case 9:
				RunOne(9, "правая кнопка", Test9Impl, ref pass, ref total);
				break;
			case 10:
				RunOne(10, "знак колеса и сброс", Test10Impl, ref pass, ref total);
				break;
			case 11:
				RunOne(11, "строки раздела", Test11Impl, ref pass, ref total);
				break;
			case 12:
				RunOne(12, "контрфакт покрытия строк", Test12Impl, ref pass, ref total);
				break;
			case 13:
				RunOne(13, "подсказки с клавишами", Test13Impl, ref pass, ref total);
				break;
			default:
				Err(string.Format("нет проверки с номером {0}", n));
				return false;
			}
		}
		finally
		{
			Restore(snap);
		}
		return pass == total;
	}

	private static void RunOne(int n, string name, Func<bool> body, ref int pass, ref int total)
	{
		total++;
		Log(string.Format("── Тест {0}: {1}", n, name));
		try
		{
			if (body())
			{
				pass++;
				Log(string.Format("    PASS {0}", n));
			}
			else
			{
				Err(string.Format("    FAIL {0}", n));
			}
		}
		catch (Exception ex)
		{
			Err(string.Format("    ИСКЛЮЧЕНИЕ: {0}", ex));
			Err(string.Format("    FAIL {0}", n));
		}
	}

	private static bool Check(bool ok, string line)
	{
		Log("    " + (ok ? "ok  " : "FAIL") + " " + line);
		return ok;
	}

	private static Snap Snapshot()
	{
		Snap snap = new Snap();
		snap.keys = new Dictionary<ControlAction, KeyCode>();
		snap.mods = new Dictionary<ControlModifier, ModifierKind>();
		foreach (ControlBindings.KeyEntry entry in ControlBindings.Keys)
		{
			snap.keys[entry.action] = ControlBindings.Key(entry.action);
		}
		foreach (ControlBindings.ModEntry entry2 in ControlBindings.Modifiers)
		{
			snap.mods[entry2.id] = ControlBindings.Modifier(entry2.id);
		}
		snap.orbitRight = ControlBindings.OrbitOnRight;
		snap.invert3D = ControlBindings.InvertWheel(ControlScope.Analytic3D);
		snap.invert2D = ControlBindings.InvertWheel(ControlScope.Flat2D);
		return snap;
	}

	private static void Restore(Snap s)
	{
		HashSet<KeyCode> used = new HashSet<KeyCode>(s.keys.Values);
		List<KeyCode> free = new List<KeyCode>();
		foreach (KeyCode k in ControlBindings.Assignable)
		{
			if (!used.Contains(k))
			{
				free.Add(k);
			}
		}
		int i = 0;
		foreach (ControlBindings.KeyEntry e in ControlBindings.Keys)
		{
			if (i >= free.Count)
			{
				break;
			}
			ControlBindings.TryAssign(e.action, free[i], out string refusalKey, out ControlAction conflict);
			i++;
		}
		foreach (KeyValuePair<ControlAction, KeyCode> row in s.keys)
		{
			ControlBindings.TryAssign(row.Key, row.Value, out string refusalKey, out ControlAction conflict);
		}
		foreach (KeyValuePair<ControlModifier, ModifierKind> row2 in s.mods)
		{
			ControlBindings.TryAssignModifier(row2.Key, row2.Value, out string refusalKey, out ControlModifier conflict2);
		}
		ControlBindings.SetOrbitOnRight(s.orbitRight);
		ControlBindings.SetInvertWheel(ControlScope.Analytic3D, s.invert3D);
		ControlBindings.SetInvertWheel(ControlScope.Flat2D, s.invert2D);
		bool same = true;
		foreach (KeyValuePair<ControlAction, KeyCode> row3 in s.keys)
		{
			if (ControlBindings.Key(row3.Key) != row3.Value)
			{
				same = false;
			}
		}
		foreach (KeyValuePair<ControlModifier, ModifierKind> row4 in s.mods)
		{
			if (ControlBindings.Modifier(row4.Key) != row4.Value)
			{
				same = false;
			}
		}
		if (same)
		{
			Log("    (назначения игрока восстановлены)");
		}
		else
		{
			Err("    ⚠ СНИМОК НЕ ВОССТАНОВЛЕН — назначения игрока изменены прогоном!");
		}
	}

	private static bool Test1Impl()
	{
		bool ok = true;
		Dictionary<ControlAction, int> actionRows = new Dictionary<ControlAction, int>();
		foreach (ControlBindings.KeyEntry e in ControlBindings.Keys)
		{
			actionRows[e.action] = actionRows.TryGetValue(e.action, out int rows) ? (rows + 1) : 1;
		}
		foreach (ControlAction a in Enum.GetValues(typeof(ControlAction)))
		{
			if (a == ControlAction.None)
			{
				continue;
			}
			int rows2 = actionRows.TryGetValue(a, out int found) ? found : 0;
			ok = ok & Check(rows2 == 1, string.Format("действие {0}: строк в таблице {1} (ждём 1)", a, rows2));
		}
		Dictionary<ControlModifier, int> modRows = new Dictionary<ControlModifier, int>();
		foreach (ControlBindings.ModEntry e2 in ControlBindings.Modifiers)
		{
			modRows[e2.id] = modRows.TryGetValue(e2.id, out int rows3) ? (rows3 + 1) : 1;
		}
		foreach (ControlModifier m in Enum.GetValues(typeof(ControlModifier)))
		{
			if (m == ControlModifier.None)
			{
				continue;
			}
			int rows4 = modRows.TryGetValue(m, out int found2) ? found2 : 0;
			ok = ok & Check(rows4 == 1, string.Format("роль {0}: строк в таблице {1} (ждём 1)", m, rows4));
		}
		return ok;
	}

	private static bool Test2Impl()
	{
		bool ok = Check(ControlBindings.AllScopes.Length == 2, string.Format("областей заявлено {0} (ждём 2)", ControlBindings.AllScopes.Length));
		foreach (ControlScope s in ControlBindings.AllScopes)
		{
			ok = ok & Check(s == ControlScope.Analytic3D || s == ControlScope.Flat2D, string.Format("область {0} — из числа проведённых", s));
		}
		int sandboxKeys = 0;
		int flatKeys = 0;
		foreach (ControlBindings.KeyEntry e in ControlBindings.Keys)
		{
			if (e.scope == ControlScope.Analytic3D)
			{
				sandboxKeys++;
			}
			else
			{
				flatKeys++;
			}
		}
		int sandboxGestures = 0;
		int flatGestures = 0;
		foreach (ControlBindings.GestureEntry g in ControlBindings.Gestures)
		{
			if (g.scope == ControlScope.Analytic3D)
			{
				sandboxGestures++;
			}
			else
			{
				flatGestures++;
			}
		}
		ok = ok & Check(sandboxKeys > 0 && sandboxGestures > 0, string.Format("песочница: клавиш {0}, жестов {1} — не пусто", sandboxKeys, sandboxGestures));
		ok = ok & Check(flatKeys == 0, string.Format("«Плоскость»: переназначаемых клавиш {0} (ждём 0 — так и написано на экране)", flatKeys));
		return ok & Check(flatGestures > 0, string.Format("«Плоскость»: жестов {0} — не пусто", flatGestures));
	}

	private static bool Test3Impl()
	{
		Dictionary<ControlAction, KeyCode> hist = new Dictionary<ControlAction, KeyCode>();
		hist.Add(ControlAction.FrameCamera, KeyCode.F);
		hist.Add(ControlAction.SectionToggle, KeyCode.C);
		hist.Add(ControlAction.SectionReface, KeyCode.V);
		hist.Add(ControlAction.CrewOrgans, KeyCode.O);
		hist.Add(ControlAction.GizmoGrab, KeyCode.G);
		hist.Add(ControlAction.GizmoAxisX, KeyCode.X);
		hist.Add(ControlAction.GizmoAxisY, KeyCode.Y);
		hist.Add(ControlAction.GizmoAxisZ, KeyCode.Z);
		hist.Add(ControlAction.DuplicateSelection, KeyCode.D);
		bool ok = true;
		foreach (KeyValuePair<ControlAction, KeyCode> row in hist)
		{
			KeyCode def = ControlBindings.DefaultKey(row.Key);
			ok = ok & Check(def == row.Value, string.Format("{0}: умолчание {1} (ждём {2})", row.Key, def, row.Value));
		}
		foreach (ControlScope s in ControlBindings.AllScopes)
		{
			Dictionary<KeyCode, ControlAction> byKey = new Dictionary<KeyCode, ControlAction>();
			bool scopeOk = true;
			foreach (ControlBindings.KeyEntry e in ControlBindings.Keys)
			{
				if (e.scope != s)
				{
					continue;
				}
				if (byKey.TryGetValue(e.def, out ControlAction first))
				{
					Log(string.Format("    FAIL {0}: {1} у {2} и у {3}", s, e.def, e.action, first));
					scopeOk = false;
				}
				else
				{
					byKey[e.def] = e.action;
				}
			}
			ok = ok & Check(scopeOk, string.Format("{0}: умолчания внутри области не повторяются ({1} клавиш)", s, byKey.Count));
		}
		Dictionary<string, ModifierKind> byChannel = new Dictionary<string, ModifierKind>();
		bool modsOk = true;
		foreach (ControlBindings.ModEntry e2 in ControlBindings.Modifiers)
		{
			string channel = e2.scope.ToString() + "/" + e2.channel + "/" + e2.def.ToString();
			if (byChannel.ContainsKey(channel))
			{
				Log("    FAIL канал " + channel + " занят дважды");
				modsOk = false;
			}
			else
			{
				byChannel[channel] = e2.def;
			}
		}
		return ok & Check(modsOk, "умолчания модификаторов внутри канала не повторяются");
	}

	private static bool Test4Impl()
	{
		bool ok = true;
		foreach (ControlBindings.KeyEntry e in ControlBindings.Keys)
		{
			ok = ok & Check(ControlBindings.IsAssignable(e.def), string.Format("{0}: умолчание {1} входит в разрешённые", e.action, ControlBindings.KeyName(e.def)));
		}
		int pinned = 0;
		foreach (KeyCode k in ControlBindings.Assignable)
		{
			if (ControlBindings.IsReserved(k))
			{
				pinned++;
			}
		}
		ok = ok & Check(pinned == 0, string.Format("разрешённых, которые одновременно закреплены: {0} (ждём 0)", pinned));
		foreach (KeyCode k2 in new KeyCode[15]
		{
			KeyCode.Escape,
			KeyCode.Return,
			KeyCode.KeypadEnter,
			KeyCode.Backspace,
			KeyCode.Alpha0,
			KeyCode.Alpha5,
			KeyCode.Keypad7,
			KeyCode.Minus,
			KeyCode.Period,
			KeyCode.LeftShift,
			KeyCode.LeftControl,
			KeyCode.LeftAlt,
			KeyCode.Mouse0,
			KeyCode.Mouse1,
			KeyCode.Mouse2
		})
		{
			ok = ok & Check(ControlBindings.IsReserved(k2) && !ControlBindings.IsAssignable(k2), ControlBindings.KeyName(k2) + " закреплена и не назначается");
		}
		bool sawEsc = false;
		bool sawEnter = false;
		foreach (KeyCode k3 in ControlBindings.CaptureCandidates)
		{
			if (k3 == KeyCode.Escape)
			{
				sawEsc = true;
			}
			else if (k3 == KeyCode.Return)
			{
				sawEnter = true;
			}
		}
		return ok & Check(sawEsc && sawEnter, "перехват опрашивает и закреплённые клавиши (Esc, Enter)");
	}

	private static bool Test5Impl()
	{
		ControlBindings.ResetScope(ControlScope.Analytic3D);
		bool refused = !ControlBindings.TryAssign(ControlAction.SectionToggle, KeyCode.F, out string refusalKey, out ControlAction conflict) && refusalKey == "controls.refuse.conflict" && conflict == ControlAction.FrameCamera;
		bool stillC = ControlBindings.Key(ControlAction.SectionToggle) == KeyCode.C;
		bool ok = Check(refused, string.Format("F на «разрез» отклонена: причина {0}, занял {1}", refusalKey ?? "нет", conflict));
		return ok & Check(stillC, "после отказа «разрез» остался на " + ControlBindings.KeyName(ControlBindings.Key(ControlAction.SectionToggle)));
	}

	private static bool Test6Impl()
	{
		ControlBindings.ResetScope(ControlScope.Analytic3D);
		bool taken = ControlBindings.TryAssign(ControlAction.SectionToggle, KeyCode.J, out string refusalKey, out ControlAction conflict);
		bool readsJ = ControlBindings.Key(ControlAction.SectionToggle) == KeyCode.J;
		bool ok = Check(taken && refusalKey == null, "свободная J на «разрез» ПРИНЯТА");
		ok = ok & Check(readsJ, "чтение вернуло " + ControlBindings.KeyName(ControlBindings.Key(ControlAction.SectionToggle)));
		return ok & Check(ControlBindings.TryAssign(ControlAction.SectionToggle, KeyCode.C, out string refusalKey2, out ControlAction conflict2) && ControlBindings.Key(ControlAction.SectionToggle) == KeyCode.C, "возврат на C принят — занятость снялась вместе с назначением");
	}

	private static bool Test7Impl()
	{
		ControlBindings.ResetScope(ControlScope.Analytic3D);
		bool ok = true;
		foreach (KeyCode k in new KeyCode[4]
		{
			KeyCode.Escape,
			KeyCode.Return,
			KeyCode.Alpha3,
			KeyCode.LeftShift
		})
		{
			bool accepted = ControlBindings.TryAssign(ControlAction.FrameCamera, k, out string refusalKey, out ControlAction conflict);
			ok = ok & Check(!accepted && refusalKey == "controls.refuse.reserved", ControlBindings.KeyName(k) + ": отказ «" + (refusalKey ?? "нет") + "»");
		}
		return ok & Check(ControlBindings.Key(ControlAction.FrameCamera) == KeyCode.F, "после четырёх отказов «кадрировать» всё ещё на F");
	}

	private static bool Test8Impl()
	{
		ControlBindings.ResetScope(ControlScope.Flat2D);
		ControlBindings.ResetScope(ControlScope.Analytic3D);
		bool ok = Check(ControlBindings.Modifier(ControlModifier.FlatBrushSize) == ModifierKind.Shift && ControlBindings.Modifier(ControlModifier.FlatPrecise) == ModifierKind.Alt, "умолчания 2D: кисть Shift, точный шаг Alt");
		bool shiftTaken = !ControlBindings.TryAssignModifier(ControlModifier.FlatPrecise, ModifierKind.Shift, out string refusalKey, out ControlModifier conflict) && refusalKey == "controls.refuse.conflict" && conflict == ControlModifier.FlatBrushSize;
		ok = ok & Check(shiftTaken, string.Format("точный шаг на Shift отклонён: занял {0}", conflict));
		ok = ok & Check(ControlBindings.TryAssignModifier(ControlModifier.SelectAdd, ModifierKind.Shift, out string refusalKey2, out ControlModifier conflict2) && conflict2 == ControlModifier.None, "выбор и сдвиг камеры уживаются на одном Shift — каналы разные");
		ok = ok & Check(ControlBindings.TryAssignModifier(ControlModifier.FlatPrecise, ModifierKind.Ctrl, out string refusalKey3, out ControlModifier conflict3) && ControlBindings.Modifier(ControlModifier.FlatPrecise) == ModifierKind.Ctrl, "точный шаг переехал на Ctrl");
		ControlBindings.ResetScope(ControlScope.Flat2D);
		return ok;
	}

	private static bool Test9Impl()
	{
		ControlBindings.SetOrbitOnRight(false);
		bool ok = Check(ControlBindings.OrbitMouseButton == 2 && ControlBindings.GizmoCancelOnRightButton, "камера на средней — правая отменяет захват");
		ControlBindings.SetOrbitOnRight(true);
		ok = ok & Check(ControlBindings.OrbitMouseButton == 1 && !ControlBindings.GizmoCancelOnRightButton, "камера на правой — правая БОЛЬШЕ не отменяет захват");
		string cancelText = null;
		foreach (ControlBindings.GestureEntry g in ControlBindings.Gestures)
		{
			if (g.titleKey == "controls.g3.cancel")
			{
				cancelText = g.Value;
			}
		}
		ControlBindings.SetOrbitOnRight(false);
		string cancelText2 = null;
		foreach (ControlBindings.GestureEntry g2 in ControlBindings.Gestures)
		{
			if (g2.titleKey == "controls.g3.cancel")
			{
				cancelText2 = g2.Value;
			}
		}
		string right = Localization.T("controls.mouse.right");
		ok = ok & Check(cancelText != null && !cancelText.Contains(right), "справка при камере на правой: «" + cancelText + "» — без правой кнопки");
		ok = ok & Check(cancelText2 != null && cancelText2.Contains(right), "справка при камере на средней: «" + cancelText2 + "» — правая названа");
		ControlBindings.SetOrbitOnRight(true);
		string orbitText = null;
		foreach (ControlBindings.GestureEntry g3 in ControlBindings.Gestures)
		{
			if (g3.titleKey == "controls.g3.orbit")
			{
				orbitText = g3.Value;
			}
		}
		ok = ok & Check(orbitText == right, "строка «вращать камеру» печатает «" + orbitText + "»");
		ControlBindings.SetOrbitOnRight(false);
		return ok;
	}

	private static bool Test10Impl()
	{
		ControlBindings.SetInvertWheel(ControlScope.Analytic3D, false);
		ControlBindings.SetInvertWheel(ControlScope.Flat2D, false);
		bool ok = Check(Mathf.Approximately(ControlBindings.WheelSign(ControlScope.Analytic3D), 1f) && Mathf.Approximately(ControlBindings.WheelSign(ControlScope.Flat2D), 1f), "без инверсии знак колеса +1 в обеих областях");
		ControlBindings.SetInvertWheel(ControlScope.Flat2D, true);
		ok = ok & Check(Mathf.Approximately(ControlBindings.WheelSign(ControlScope.Flat2D), -1f) && Mathf.Approximately(ControlBindings.WheelSign(ControlScope.Analytic3D), 1f), "инверсия 2D не задела песочницу");
		ControlBindings.TryAssign(ControlAction.FrameCamera, KeyCode.J, out string refusalKey, out ControlAction conflict);
		ControlBindings.ResetScope(ControlScope.Flat2D);
		ok = ok & Check(ControlBindings.Key(ControlAction.FrameCamera) == KeyCode.J, "сброс «Плоскости» не вернул клавишу песочницы");
		ok = ok & Check(!ControlBindings.InvertWheel(ControlScope.Flat2D), "сброс «Плоскости» снял её инверсию");
		ControlBindings.ResetScope(ControlScope.Analytic3D);
		return ok & Check(ControlBindings.Key(ControlAction.FrameCamera) == KeyCode.F, "сброс песочницы вернул F");
	}

	private static List<string> ControlKeys()
	{
		HashSet<string> set = new HashSet<string>();
		foreach (ControlBindings.KeyEntry e in ControlBindings.Keys)
		{
			Add(e.titleKey);
			Add(e.hintKey);
		}
		foreach (ControlBindings.ModEntry e2 in ControlBindings.Modifiers)
		{
			Add(e2.titleKey);
			Add(e2.hintKey);
		}
		foreach (ControlBindings.GestureEntry g in ControlBindings.Gestures)
		{
			Add(g.titleKey);
		}
		foreach (ControlScope s in ControlBindings.AllScopes)
		{
			Add(ControlBindings.TitleKey(s));
			Add(ControlBindings.HintKey(s));
		}
		foreach (string k in new string[27]
		{
			"settings.controls", "settings.controls_hint", "controls.title", "controls.keys", "controls.mods", "controls.mouse", "controls.fixed", "controls.fixed_hint", "controls.reset", "controls.press",
			"controls.press_hint", "controls.assigned", "controls.cancelled", "controls.reset_done", "controls.no_keys", "controls.refuse.reserved", "controls.refuse.unsupported", "controls.refuse.conflict", "controls.refuse.unknown", "controls.orbit_button",
			"controls.orbit_button_hint", "controls.invert_wheel", "controls.invert_wheel_hint", "controls.mouse.left", "controls.mouse.middle", "controls.mouse.right", "controls.mouse.wheel"
		})
		{
			Add(k);
		}
		List<string> list = new List<string>(set);
		list.Sort(StringComparer.Ordinal);
		return list;

		void Add(string k)
		{
			if (!string.IsNullOrEmpty(k))
			{
				set.Add(k);
			}
		}
	}

	private static bool KeyCovered(string key, out string why)
	{
		why = null;
		if (!Localization.HasKey(key))
		{
			why = "ключа нет в таблице";
			return false;
		}
		foreach (Language lang in Required)
		{
			if (Localization.CellEmpty(key, lang))
			{
				why = "пусто на " + Localization.ShortCode(lang);
				return false;
			}
		}
		if (!Localization.CellEmpty(key, MustBeEmpty))
		{
			why = Localization.ShortCode(MustBeEmpty) + " заполнен, а по заданию обязан быть пуст";
			return false;
		}
		return true;
	}

	private static bool Test11Impl()
	{
		List<string> keys = ControlKeys();
		bool ok = Check(keys.Count > 40, string.Format("ключей раздела собрано {0}", keys.Count));
		int broken = 0;
		foreach (string k in keys)
		{
			if (!KeyCovered(k, out string why))
			{
				Log("    FAIL " + k + ": " + why);
				broken++;
			}
		}
		ok = ok & Check(broken == 0, string.Format("ключей с изъяном: {0} из {1}", broken, keys.Count));
		Language saved = Localization.Current;
		Localization.SetLanguage(Language.TT);
		string tt = Localization.T("controls.title");
		Localization.SetLanguage(Language.EN);
		string en = Localization.T("controls.title");
		Localization.SetLanguage(saved);
		return ok & Check(tt == en && !tt.StartsWith("#"), "татарский показывает английское «" + tt + "», а не «#ключ»");
	}

	private static bool Test12Impl()
	{
		bool ok = Check(!KeyCovered("menu.settings", out string why), "menu.settings отвергнут проверкой: " + (why ?? "НЕ ОТВЕРГНУТ"));
		ok = ok & Check(!KeyCovered("flat.fire", out string why2), "flat.fire отвергнут проверкой: " + (why2 ?? "НЕ ОТВЕРГНУТ"));
		ok = ok & Check(!KeyCovered("controls.this.key.does.not.exist", out string why3), "несуществующий ключ отвергнут: " + (why3 ?? "НЕ ОТВЕРГНУТ"));
		return ok & Check(KeyCovered("controls.title", out string why4), "и при этом настоящий ключ раздела ПРИНЯТ (" + (why4 ?? "без изъяна") + ")");
	}

	private static bool Test13Impl()
	{
		ControlBindings.ResetScope(ControlScope.Analytic3D);
		string hint = Localization.T("gizmo.hint", ControlBindings.KeyName(ControlBindings.Key(ControlAction.GizmoGrab)), ControlBindings.KeyName(ControlBindings.Key(ControlAction.GizmoAxisY)), ControlBindings.KeyName(ControlBindings.Key(ControlAction.GizmoAxisZ)), ControlBindings.ModifierName(ControlBindings.Modifier(ControlModifier.GizmoSnap)));
		bool ok = Check(hint.Contains("X") && hint.Contains("Ctrl") && !hint.Contains("{0}"), "подсказка захвата на умолчаниях: «" + hint + "»");
		ControlBindings.TryAssign(ControlAction.GizmoGrab, KeyCode.J, out string refusalKey, out ControlAction conflict);
		ControlBindings.TryAssignModifier(ControlModifier.GizmoSnap, ModifierKind.Alt, out string refusalKey2, out ControlModifier conflict2);
		string hint2 = Localization.T("gizmo.hint", ControlBindings.KeyName(ControlBindings.Key(ControlAction.GizmoGrab)), ControlBindings.KeyName(ControlBindings.Key(ControlAction.GizmoAxisY)), ControlBindings.KeyName(ControlBindings.Key(ControlAction.GizmoAxisZ)), ControlBindings.ModifierName(ControlBindings.Modifier(ControlModifier.GizmoSnap)));
		ok = ok & Check(hint2.Contains("J") && hint2.Contains("Alt"), "после переназначения: «" + hint2 + "»");
		ok = ok & Check(hint2 != hint, "строка действительно изменилась (а не совпала случайно)");
		string duplicate = Localization.T("sandbox.duplicate", ControlBindings.KeyName(ControlBindings.Key(ControlAction.DuplicateSelection)));
		ok = ok & Check(!duplicate.Contains("{0}") && duplicate.Contains("D"), "подпись кнопки копирования: «" + duplicate + "»");
		ControlBindings.ResetScope(ControlScope.Analytic3D);
		return ok;
	}
}
