using System.Collections.Generic;
using UnityEngine;

public static class HollowBodyCatalog
{
	public const string HollowBodies = "Полые тела";

	private static readonly HollowBodySpec[] bodies =
	{
		H("hollow_placeholder", "Полое тело (заглушка проекта)", HollowBodies, 0, 76f, 300f, 800f, 0.2f, 1000f, 0.00018887f, "PLACEHOLDER ЦЕЛИКОМ — и механизм, и тело. Изделием НЕ является. Три числа механизма (доля полости, скорость разлёта стенки, время срабатывания) взяты ССЫЛКОЙ из SuperShellPreset и там же обоснованы: Конфигурация-заглушка S27 части А. Все три числа — PLACEHOLDER проекта, в том же смысле, в каком PLACEHOLDER'ом помечены скорости пластин в EraCatalog: V_разл = 1000 м/с (опубликованной скорости разлёта корпуса у проекта нет ни одной), доля полости 0.20 (выбрана под ворота толщины стенки, а не по снаряжению), задержка взрывателя 188.87 мкс (ВЫВЕДЕНА ИЗ ЗАМЕРА на эталонной сцене строки: эталонная сцена строки — приёмка S28 §4 (NightS28, набор S28FIRE): полое тело ⌀60 × 120 мм, встреча 800 м/с, мишень RHA 8 мм по нормали, шаг частиц 2 мм — там она даёт импульс в 47.83 мм ЗА тыльной плоскостью; настоящие донные взрыватели работают на миллисекундах, поэтому пометка PLACEHOLDER остаётся). Рампа заимствована у ДЗ (SuperEra.RampS = 5 мкс, ASSUMED_BY_SUBSTITUTION). Ни одно из этих чисел не является паспортом изделия: с S28 §1.3 они отгружаются игроку ОДНОЙ строкой каталога полых тел, и отгружаются С ЯРЛЫКОМ — и подпись строки, и её подсказка называют себя заглушкой проекта. Именованных полых тел по-прежнему нет. ⚠ ЧИСЛА ТЕЛА — СОБСТВЕННАЯ ПОДСТАВКА ПРОЕКТА, а не паспорт: калибр 76 мм взят потому, что ИМЕННО ПОД НЕГО выбрана доля полости 0.20 (при шаге частиц 2 мм стенка выходит около девяти слоёв при требуемых четырёх — см. SuperShellPreset); длина корпуса 300 мм — ESTIMATED по классу (около четырёх калибров); скорость встречи 800 м/с — порядок «снаряд средней пушки», своего источника не имеет; нос — оживало, вещество корпуса — сталь (ASSUMED_BY_SUBSTITUTION: строки «сталь корпуса снаряда» у реестра нет, сталь 4340 — опорная сталь проекта). Ни одно число тела в расчёт механизма не входит: полость задаётся ДОЛЕЙ, импульс — скоростью стенки.", "⭐ СЧИТАЕТСЯ ТОЛЬКО В SUPER. Обычный и продвинутый режимы ОТКАЗЫВАЮТ ПО ИМЕНИ: аналитическое ядро (Алексеевский–Тейт) знает сплошной стержень и полости не выражает, а посчитать полое тело ломом под его же именем — это тот самый подлог, ради запрета которого класс и заведён. Режим ULTRA отказывает по своей причине: полости и привода стенки у решателя сплошной среды нет. ⚠ ДЕТОНАЦИЯ, ВВ И ЦЕПЬ ВЗРЫВАТЕЛЯ НЕ СЧИТАЮТСЯ ВОВСЕ (упрощения У1 и У15): стенка получает РАДИАЛЬНЫЙ ИМПУЛЬС по наблюдаемой скорости разлёта, с конечной рампой, ровно как метаемая пластина динамической защиты. ⚠ ВРЕМЯ СРАБАТЫВАНИЯ — ПАРАМЕТР, А НЕ ПАСПОРТ ИЗДЕЛИЯ: 188.87 мкс ВЫВЕДЕНЫ ИЗ ЗАМЕРА на эталонной сцене строки (приёмка S28 §4: тело ⌀60 × 120 мм, 800 м/с, мишень RHA 8 мм, шаг 2 мм) — там они кладут импульс в 47.83 мм ЗА тыльной плоскостью. ⚠ НА СОБСТВЕННОМ ТЕЛЕ ЭТОЙ СТРОКИ (300 мм) центр полости отстоит от кончика вдвое дальше, чем на эталонной сцене, и это умолчание всё ещё кладёт импульс ПЕРЕД гранью: смотри живой расчёт в миллиметрах рядом с ручкой и поднимай время срабатывания вместе с длительностью события. У настоящих донных взрывателей задержка идёт на миллисекундах, то есть ДЛИННЕЕ всего прогона целиком. Ноль означает импульс на первом касании. ◈ ПОКАЗ РИСУЕТ ТЕЛО СТЕРЖНЕМ (своей модели корпуса у строки нет), и до импульса оно будет эродировать и грибить, как пенетратор, — заявка показа, которой механизм не делает."),
		H("hollow_instant", "Фугасное действие (заглушка проекта)", HollowBodies, 0, 76f, 300f, 800f, 0.2f, 1000f, 0f, "PLACEHOLDER ЦЕЛИКОМ, как и парная строка «Полое тело (заглушка проекта)». Изделием НЕ является. ⭐ ОТЛИЧАЕТСЯ ОТ ПАРНОЙ РОВНО ОДНИМ ЧИСЛОМ — временем срабатывания: здесь оно НОЛЬ, то есть импульс на первом касании (фугасное действие снаружи брони), у парной — 188.87 мкс, выведенные замером (каморное действие за бронёй). Тело, доля полости и скорость разлёта стенки — ТЕ ЖЕ и теми же ссылками: Конфигурация-заглушка S27 части А. Все три числа — PLACEHOLDER проекта, в том же смысле, в каком PLACEHOLDER'ом помечены скорости пластин в EraCatalog: V_разл = 1000 м/с (опубликованной скорости разлёта корпуса у проекта нет ни одной), доля полости 0.20 (выбрана под ворота толщины стенки, а не по снаряжению), задержка взрывателя 188.87 мкс (ВЫВЕДЕНА ИЗ ЗАМЕРА на эталонной сцене строки: эталонная сцена строки — приёмка S28 §4 (NightS28, набор S28FIRE): полое тело ⌀60 × 120 мм, встреча 800 м/с, мишень RHA 8 мм по нормали, шаг частиц 2 мм — там она даёт импульс в 47.83 мм ЗА тыльной плоскостью; настоящие донные взрыватели работают на миллисекундах, поэтому пометка PLACEHOLDER остаётся). Рампа заимствована у ДЗ (SuperEra.RampS = 5 мкс, ASSUMED_BY_SUBSTITUTION). Ни одно из этих чисел не является паспортом изделия: с S28 §1.3 они отгружаются игроку ОДНОЙ строкой каталога полых тел, и отгружаются С ЯРЛЫКОМ — и подпись строки, и её подсказка называют себя заглушкой проекта. Именованных полых тел по-прежнему нет. ⚠ ТОЛЩИНЫ СТЕНКИ И ПОЛОЖЕНИЯ КАМОРЫ ИЗ ПАСПОРТА У ПРОЕКТА НЕТ: возврат брифа 28 не пришёл, и стенка по-прежнему ВЫВОДИТСЯ из доли полости, а не объявляется.", "⭐ СЧИТАЕТСЯ ТОЛЬКО В SUPER — по той же причине, что и парная строка: аналитическое ядро полости не выражает, у решателя сплошной среды нет ни полости, ни привода стенки. ⚠ ДЕТОНАЦИЯ, ВВ И ЦЕПЬ ВЗРЫВАТЕЛЯ НЕ СЧИТАЮТСЯ ВОВСЕ (упрощения У1 и У15). ⭐ ВРЕМЯ СРАБАТЫВАНИЯ НОЛЬ — ЭТО ЗНАЧЕНИЕ МЕХАНИЗМА, А НЕ ПРОПУСК: импульс прикладывается на первом касании, то есть ПЕРЕД бронёй. Замер приёмки S28 §4 на эталонной сцене: центр полости в момент импульса — 75.09 мм ПЕРЕД лицевой гранью. ◈ ПОКАЗ РИСУЕТ ТЕЛО СТЕРЖНЕМ (своей модели корпуса у строки нет).")
	};

	public static int ValidateCalls;

	public static int LastValidatedRowCount;

	public static IList<HollowBodySpec> All => bodies;

	public static ProjectileDefinition MakeDefinition(HollowBodySpec h)
	{
		ProjectileDefinition projectileDefinition = new ProjectileDefinition();
		projectileDefinition.name = h.displayName;
		projectileDefinition.kind = ProjectileClass.HollowBody;
		projectileDefinition.coreType = Projectile.CoreType.Steel;
		projectileDefinition.rodLengthMM = h.bodyLengthMM;
		projectileDefinition.diameterMM = h.calibreMM;
		projectileDefinition.velocityMps = h.velocityMps;
		projectileDefinition.nose = NoseShape.Ogive;
		projectileDefinition.cavityFrac = h.cavityFraction;
		projectileDefinition.fuzeDelayS = h.triggerTimeS;
		projectileDefinition.fragmentVelocityMps = h.wallVelocityMps;
		projectileDefinition.segments = 10;
		projectileDefinition.strands = 7;
		projectileDefinition.noFins = true;
		return projectileDefinition;
	}

	public static string Tooltip(HollowBodySpec h)
	{
		string text = string.Concat(h.displayName, (h.year > 0) ? string.Format(", {0} г.", h.year) : "", ". ", string.Format("Корпус {0:F0} x {1:F0} мм, скорость встречи ", h.bodyLengthMM, h.calibreMM), string.Format("{0:F0} м/с. ", h.velocityMps));
		string text2 = string.Concat(string.Format("МЕХАНИЗМ: полое тело, доля полости {0:F2} ", h.cavityFraction), string.Format("({0:F0} % объёма), радиальный импульс по стенке ", h.cavityFraction * 100f), string.Format("{0:F0} м/с с конечной рампой, время срабатывания ", h.wallVelocityMps), (h.triggerTimeS > 0f) ? string.Format("{0:F0} мкс от первого касания. ", h.triggerTimeS * 1000000f) : "0 — импульс на первом касании. ");
		return string.Concat(text, text2, "⭐ ОДНА СТРОКА, ДВА ПОВЕДЕНИЯ, И РАЗЛИЧАЕТ ИХ ОДНО ЧИСЛО — время срабатывания: ноль даёт импульс на касании, ненулевое — импульс после того, как тело успело пройти вглубь. ", "⚠ РАСЧЁТА ПРОБИТИЯ У ЭТОЙ СТРОКИ НЕТ И В ПОДСКАЗКЕ НЕ БУДЕТ: аналитической модели полого тела в проекте нет вовсе. ", string.IsNullOrEmpty(h.limitation) ? "" : (h.limitation + " "), "ПРОВЕНАНС: ", h.provenance);
	}

	public static bool TryGet(string id, out HollowBodySpec h)
	{
		for (int i = 0; i < bodies.Length; i++)
		{
			if (bodies[i].id == id)
			{
				h = bodies[i];
				return true;
			}
		}
		h = default(HollowBodySpec);
		return false;
	}

	public static void Validate(IList<HollowBodySpec> rows, List<string> problems)
	{
		ValidateCalls++;
		LastValidatedRowCount = rows.Count;
		foreach (HollowBodySpec row in rows)
		{
			HollowBodySpec h = row;
			if (string.IsNullOrEmpty(h.id))
			{
				problems.Add("полое тело: пустой ключ");
				continue;
			}
			if (string.IsNullOrEmpty(h.displayName))
			{
				problems.Add("полое тело '" + h.id + "': пустая подпись");
				continue;
			}
			if (string.IsNullOrEmpty(h.provenance))
			{
				problems.Add("полое тело '" + h.id + "': не указан провенанс чисел");
			}
			if (h.calibreMM <= 0f || h.bodyLengthMM <= 0f)
			{
				problems.Add(string.Format("полое тело '{0}': габарит корпуса {1}x{2} мм", h.id, h.bodyLengthMM, h.calibreMM));
			}
			if (h.velocityMps <= 0f)
			{
				problems.Add(string.Format("полое тело '{0}': скорость встречи {1} м/с", h.id, h.velocityMps));
			}
			if (h.cavityFraction <= 0f || h.cavityFraction >= 1f)
			{
				problems.Add(string.Format("полое тело '{0}': доля полости {1} вне (0; 1) — ", h.id, h.cavityFraction) + "ноль это сплошное тело, единица это отсутствие стенки");
			}
			if (h.wallVelocityMps <= 0f)
			{
				problems.Add(string.Format("полое тело '{0}': скорость разлёта стенки {1} м/с — ", h.id, h.wallVelocityMps) + "радиального импульса не будет вовсе");
			}
			if (h.triggerTimeS < 0f)
			{
				problems.Add(string.Format("полое тело '{0}': время срабатывания {1} с отрицательно", h.id, h.triggerTimeS));
			}
			if ((Mathf.Abs(h.wallVelocityMps - 1000f) < 0.5f || Mathf.Abs(h.cavityFraction - 0.2f) < 0.0001f || Mathf.Abs(h.triggerTimeS - 0.00018887f) < 1e-09f) && (h.limitation == null || !h.limitation.Contains("PLACEHOLDER")))
			{
				problems.Add("полое тело '" + h.id + "': число механизма — заглушка, а провенанс о PLACEHOLDER молчит");
			}
		}
		for (int i = 0; i < rows.Count; i++)
		{
			for (int j = i + 1; j < rows.Count; j++)
			{
				if (rows[i].id == rows[j].id)
				{
					problems.Add("каталог полых тел: ключ '" + rows[i].id + "' повторяется");
				}
				if (rows[i].displayName == rows[j].displayName)
				{
					problems.Add("каталог полых тел: подпись '" + rows[i].displayName + "' повторяется — опознание строки идёт ПО ИМЕНИ, и сохранение подменило бы строку молча");
				}
			}
		}
	}

	public static void Validate(List<string> problems)
	{
		Validate(bodies, problems);
	}

	private static HollowBodySpec H(string id, string name, string category, int year, float calibreMM, float bodyLen, float velocity, float cavity, float wallV, float triggerTime, string provenance, string limitation = "")
	{
		return new HollowBodySpec
		{
			id = id,
			displayName = name,
			category = category,
			year = year,
			calibreMM = calibreMM,
			bodyLengthMM = bodyLen,
			velocityMps = velocity,
			cavityFraction = cavity,
			wallVelocityMps = wallV,
			triggerTimeS = triggerTime,
			provenance = provenance,
			limitation = limitation
		};
	}
}
