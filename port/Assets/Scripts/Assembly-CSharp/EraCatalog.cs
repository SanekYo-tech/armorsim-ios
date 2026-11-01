using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public static class EraCatalog
{
	public const string TableNote = "⚠ Марка настоящая, числа — из ВТОРИЧНЫХ открытых источников: справочников и обзоров, не из протоколов испытаний. Заявленная стойкость («+N мм от кумулятивных») в паспорт НЕ ВХОДИТ и здесь не хранится вовсе: стойкость сборки — это то, что покажет выстрел по ней.";

	public const float PlaceholderLightVMS = 700f;

	public const float PlaceholderHeavyVMS = 500f;

	private static readonly EraPreset[] presets;

	public static IList<EraPreset> All { get; }

	public static void Apply(EraPreset p, ArmorPlate plate)
	{
		if (plate == null)
		{
			return;
		}
		plate.SetArmorType(p.armor);
		plate.SetEraPlateThickness(p.plateMM);
		plate.SetEraInterlayerGap(p.gapMM);
		plate.SetEraPlateVelocity(p.velocityMS);
		plate.SetEraCassetteExtent(p.extentMM);
		plate.presetId = p.id;
	}

	public static bool TryGet(string id, out EraPreset p)
	{
		for (int i = 0; i < presets.Length; i++)
		{
			if (presets[i].id == id)
			{
				p = presets[i];
				return true;
			}
		}
		p = default;
		return false;
	}

	public static string MatchingId(ArmorPlate plate)
	{
		if (plate == null || !plate.IsEraPlate)
		{
			return null;
		}
		if (!string.IsNullOrEmpty(plate.presetId))
		{
			for (int i = 0; i < presets.Length; i++)
			{
				if (presets[i].id == plate.presetId && MatchesNumbers(plate, presets[i]))
				{
					return plate.presetId;
				}
			}
		}
		for (int i = 0; i < presets.Length; i++)
		{
			if (MatchesNumbers(plate, presets[i]))
			{
				return presets[i].id;
			}
		}
		return null;
	}

	private static bool MatchesNumbers(ArmorPlate plate, EraPreset e)
	{
		return plate.armorType == e.armor && Same(plate.eraPlateThicknessMM, e.plateMM) && Same(plate.eraInterlayerGapMM, e.gapMM) && SameV(plate.eraPlateVelocityMS, e.velocityMS) && Same(plate.eraCassetteExtentMM, e.extentMM);
	}

	private static bool Same(float a, float b)
	{
		return Mathf.Abs(a - b) < 0.001f;
	}

	private static bool SameV(float a, float b)
	{
		return Mathf.Abs(a - b) < 0.5f;
	}

	public static string Tooltip(EraPreset p)
	{
		float timeMs = p.velocityMS > 0f ? p.extentMM / p.velocityMS : 0f;
		float impulse = p.plateMM / 1000f * 7850f * p.velocityMS / 1000f;
		string text = (p.armor == ArmorPlate.ArmorType.ERAHeavy ? "ТЯЖЁЛАЯ" : "лёгкая") + " кассета: " + string.Format("пластина {0:F1} мм, зазор (слой ВВ) {1:F1} мм, ", p.plateMM, p.gapMM) + string.Format("V_p = {0:F0} м/с, W = {1:F0} мм. ", p.velocityMS, p.extentMM) + string.Format("Набивка по линии визирования = 2·t_p + зазор = {0:F1} мм. ", 2f * p.plateMM + p.gapMM);
		string derived = string.Format("t = W/V_p = {0:F3} мс; удельный импульс ρ·t_p·V_p = ", timeMs) + string.Format("{0:F1} кН·с/м². ", impulse);
		return text + "⭐ ВЫВЕДЕНО из этих же чисел: пластина сходит со струи за " + derived + "ПРОВЕНАНС: " + p.provenance + " " + TableNote;
	}

	public static void Validate(IList<EraPreset> rows, List<string> problems)
	{
		foreach (EraPreset e in rows)
		{
			if (string.IsNullOrEmpty(e.id))
			{
				problems.Add("пресет ДЗ: пустой ключ");
				continue;
			}
			if (string.IsNullOrEmpty(e.displayName))
			{
				problems.Add("пресет ДЗ '" + e.id + "': пустая подпись");
			}
			if (string.IsNullOrEmpty(e.provenance))
			{
				problems.Add("пресет ДЗ '" + e.id + "': не указан провенанс чисел");
			}
			if (e.plateMM < 0.1f)
			{
				problems.Add(string.Format("пресет ДЗ '{0}': t_p = {1} мм — сеттер поднимет до 0.1", e.id, e.plateMM));
			}
			if (e.gapMM < 0.5f)
			{
				problems.Add(string.Format("пресет ДЗ '{0}': зазор = {1} мм — сеттер поднимет до 0.5", e.id, e.gapMM));
			}
			if (e.extentMM < 10f)
			{
				problems.Add(string.Format("пресет ДЗ '{0}': W = {1} мм — сеттер поднимет до 10", e.id, e.extentMM));
			}
			if (e.velocityMS < 0f)
			{
				problems.Add(string.Format("пресет ДЗ '{0}': V_p = {1} м/с — сеттер поднимет до 0", e.id, e.velocityMS));
			}
			if (e.armor != ArmorPlate.ArmorType.ERA && e.armor != ArmorPlate.ArmorType.ERAHeavy)
			{
				problems.Add(string.Format("пресет ДЗ '{0}': материал {1} не является кассетой ДЗ", e.id, e.armor));
			}
			if (e.id != "era_default")
			{
				bool heavy = e.armor == ArmorPlate.ArmorType.ERAHeavy;
				float plateLo = heavy ? 15f : 2f;
				float plateHi = heavy ? 20f : 6f;
				float gapLo = heavy ? 25f : 3f;
				float gapHi = heavy ? 45f : 7f;
				string kind = heavy ? "тяжёлая" : "лёгкая";
				if (e.plateMM < plateLo || e.plateMM > plateHi)
				{
					problems.Add(string.Format("пресет ДЗ '{0}': пластина {1:F1} мм вне конверта ", e.id, e.plateMM) + string.Format("настоящего железа [{0:F0}; {1:F0}] ({2}, бриф 24)", plateLo, plateHi, kind));
				}
				if (e.gapMM < gapLo || e.gapMM > gapHi)
				{
					problems.Add(string.Format("пресет ДЗ '{0}': слой ВВ {1:F1} мм вне конверта ", e.id, e.gapMM) + string.Format("[{0:F0}; {1:F0}] ({2}, бриф 24)", gapLo, gapHi, kind));
				}
			}
			if ((SameV(e.velocityMS, PlaceholderLightVMS) || SameV(e.velocityMS, PlaceholderHeavyVMS)) && !e.provenance.Contains("PLACEHOLDER"))
			{
				problems.Add("пресет ДЗ '" + e.id + "': V_p = заглушка, а провенанс о PLACEHOLDER молчит");
			}
		}
		for (int i = 0; i < rows.Count; i++)
		{
			for (int j = i + 1; j < rows.Count; j++)
			{
				if (rows[i].id == rows[j].id)
				{
					problems.Add("каталог ДЗ: ключ '" + rows[i].id + "' повторяется");
				}
			}
		}
	}

	public static void Validate(List<string> problems)
	{
		Validate(All, problems);
	}

	private static EraPreset E(string id, string name, ArmorPlate.ArmorType armor, float plateMM, float gapMM, float velocityMS, float extentMM, string provenance)
	{
		return new EraPreset
		{
			id = id,
			displayName = name,
			category = "Активная защита",
			armor = armor,
			plateMM = plateMM,
			gapMM = gapMM,
			velocityMS = velocityMS,
			extentMM = extentMM,
			provenance = provenance
		};
	}

	static EraCatalog()
	{
		presets = new EraPreset[]
		{
			E("era_kontakt1", "Контакт-1 (4С20)", ArmorPlate.ArmorType.ERA, 2.3f, 5.4f, 1156f, 130f, "t_p 2.3 мм и слой ВВ 5.4 мм — PUBLISHED (раскладка элемента 4С20; в других обзорах та же сборка описана округлённо как 2/7/2). W = 130 мм — PUBLISHED: элемент 252×130×10 мм, и W в модели — это КОРОТКАЯ сторона, путь пластины поперёк угрозы (см. ArmorPlate.EraSweepExtentMM). V_p = 1156 м/с — PUBLISHED, но это РАСЧЁТ источника (Герни по массе ВВ 260 г), а не замер. ⚠ СХОДИМОСТЬ, КОТОРУЮ СТОИТ ЗНАТЬ: удельный импульс ρ·t·V = 18.1 кН·с/м² здесь и 18.8 кН·с/м² у Blazer — расхождение 4 % при слое ВВ, различающемся почти вдвое. Совпадение это или закон — из двух точек не следует; проверять нечем, поэтому V_p у остальных строк НЕ выводится из него. ⭐ S11 (бриф 24, 02.09): бриф ПОДТВЕРЖДАЕТ оба числа и добавляет паспорт: ПВВ-5А 260 г, обшивка 2–3 мм, блок 314×148 мм из ДВУХ элементов 252×130, поставленных «домиком» под 9° друг к другу; штатный угол УСТАНОВКИ блока 68°. V-раскладка и угол установки моделью не воспроизводятся (кассета кладёт одну плоскую пластину; наклон — свойство СЦЕНЫ, не строки) — записаны паспортом."),
			E("era_blazer", "Blazer", ArmorPlate.ArmorType.ERA, 3f, 3f, 800f, 200f, "Сэндвич 3/3/3 мм и V_p ≈ 800 м/с — PUBLISHED, и это ЕДИНСТВЕННАЯ строка таблицы, у которой скорость пластины взята из источника, а не рассчитана и не принята нами. ⚠ W = 200 мм — ESTIMATED: габарит блока в найденных источниках дан только сравнением («блоки Контакта — около 40 % размера блоков Blazer»), собственного числа НЕ НАЙДЕНО. ⭐ S11 (бриф 24, 02.09): бриф НЕЗАВИСИМО подтверждает 3/3/3 и ≈800 м/с — единственную опубликованную скорость лёгкой кассеты; числа не тронуты."),
			E("era_erawa1", "ERAWA-1", ArmorPlate.ArmorType.ERA, 6f, 6f, 700f, 150f, "⭐ S11 (бриф 24, Wiśniewski 2019, первичное техническое): лицевая пластина 6 мм стали 500HB, кассета 26 мм, плитка 150×150 мм, 2.9 кг ⇒ t_p = 6.0 — PUBLISHED (до 02.09 стояло 3.0 ESTIMATED). W = 150 мм — PUBLISHED. ⚠ Слой ВВ — ESTIMATED (раскладки слоёв внутри кассеты источник не даёт). ⚠ V_p — PLACEHOLDER проекта."),
			E("era_erawa2", "ERAWA-2", ArmorPlate.ArmorType.ERA, 2.5f, 6f, 700f, 150f, "⭐ S11 (бриф 24, Wiśniewski 2019): кассета 46 мм — КЕРАМИКА 4–5 мм ПЕРЕД сталью 2–3 мм 500HB, два слоя ВВ ⇒ t_p = 2.5 — PUBLISHED: модель метает СТАЛЬ, и это её пластина (до 02.09 стояло 3.0 ESTIMATED). ⚠⚠ КЕРАМИЧЕСКИЙ СЛОЙ НЕ МОДЕЛИРУЕТСЯ ВОВСЕ — объявленное отсутствие, а не огрубление: у кассеты одна метаемая пластина. ⚠⚠ ВТОРОЙ РЕАКТИВНЫЙ ЭЛЕМЕНТ (ответ тандему) МОДЕЛЬЮ НЕ ВОСПРОИЗВОДИТСЯ: хочешь два слоя — ставь две кассеты подряд, они независимы по построению. ⚠ V_p — PLACEHOLDER проекта."),
			E("era_kontakt5", "Контакт-5 (4С22)", ArmorPlate.ArmorType.ERAHeavy, 15f, 35f, 1078f, 105f, "t_p = 15 мм — PUBLISHED (крышка блока 15–16 мм катаной брони; именно она и работает по стержню). W = 105 мм — PUBLISHED: блок 105×230×70 мм, 10.35 кг, короткая сторона в плане. ⚠ Слой 35 мм — ESTIMATED САМИМ ИСТОЧНИКОМ (раскладка «15 + 35 ВВ + 20» приведена там как оценка, а не как паспорт). ⭐ S11 (бриф 24, 02.09): V_p = 1078 м/с — PUBLISHED, НИЖНИЙ КРАЙ опубликованной полосы 1078–1205 м/с для элемента 4С22 (медленная пластина — консервативный выбор для защиты). ⚠⚠ До 02.09 здесь стояла заглушка проекта 500 м/с — В 2.2 РАЗА МЕДЛЕННЕЕ; импульс стержню растёт со скоростью пластины напрямую ⇒ ВСЕ числа тяжёлой ДЗ, снятые до этой даты (приёмка S5 целиком: выигрыши, половинки, денежный кадр; кассетная часть S6 §1.3), СНЯТЫ С УЧЁТА и перечислены в отчёте S11. Тихий перегон под старым именем запрещён."),
			E("era_relikt", "Реликт (4С23)", ArmorPlate.ArmorType.ERAHeavy, 15f, 30f, 500f, 125f, "t_p = 15 мм — PUBLISHED (крышка). W = 125 мм — PUBLISHED: элемент 250×125×10 мм. ⚠ Слой 30 мм — ESTIMATED: собственной раскладки короба 4С23 НЕ НАЙДЕНО, взято чуть меньше оценки для 4С22 — по единственному признаку, который в источниках есть (масса ВВ элемента 260 г против 280 г). Это ОЦЕНКА ПО КОСВЕННОМУ ПРИЗНАКУ. ⚠ V_p — PLACEHOLDER проекта."),
			E("era_kaktus", "Кактус (4С24)", ArmorPlate.ArmorType.ERAHeavy, 15f, 30f, 500f, 125f, "⚠⚠ ГЕОМЕТРИИ ЭЛЕМЕНТА 4С24 В ОТКРЫТЫХ ИСТОЧНИКАХ НЕ НАЙДЕНО — это результат поиска, а не пропуск в таблице. Взята геометрия 4С23: источники упоминают 4С23 и 4С24 как элементы одного семейства и одной раскладки, но ПОДТВЕРЖДЕНИЯ ЧИСЛАМИ этому нет. Строка отличается от «Реликта» ТОЛЬКО ИМЕНЕМ, и это сказано здесь, потому что молча разные подписи при одинаковых числах читались бы как разные изделия. ⚠ V_p — PLACEHOLDER проекта, как у Реликта (дописано S11: новый сторож валидатора поймал, что скорость-заглушка не была названа заглушкой именно у этой строки)."),
			E("era_default", "Кассета ДЗ (умолчание проекта)", ArmorPlate.ArmorType.ERA, 2.5f, 20f, 700f, 130f, "PLACEHOLDER целиком — все четыре числа. Изделием НЕ является: это прежнее умолчание проекта, сохранённое строкой, чтобы сборки, собранные до появления каталога, совпадали с ним точно, а не «примерно с Контактом-1».")
		};
		All = new ReadOnlyCollection<EraPreset>(presets);
	}
}
