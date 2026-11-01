using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class ContinuumMaterials
{
	public const float TaylorQuinney = 0.9f;

	public const float DefaultEpsSat = 1f;

	public const float DefaultEpsASB = 0.5f;

	public const float DefaultFadeWidth = 0.3f;

	public const float DefaultGamma0 = 2f;

	public const float DefaultS = 1.45f;

	public static ContinuumMaterialId ActiveRhaSet;

	public const float CastDamageScale = 0.74f;

	public const float CastDamageScaleLow = 0.58f;

	public const float CastDamageScaleHigh = 0.89f;

	private static ContinuumProperties[] table;

	private static string[] sources;

	private static string[][] caveats;

	private static ContinuumProperties Make(ContinuumProperties p)
	{
		p = WithHugoniot(p, p.C0, p.S);
		if (p.epsSat <= 0f)
		{
			p.epsSat = DefaultEpsSat;
		}
		return p;
	}

	public static ContinuumProperties WithHugoniot(ContinuumProperties p, float c0, float s)
	{
		if (p.C0 > 0f && c0 != p.C0)
		{
			p.K *= (c0 / p.C0) * (c0 / p.C0);
		}
		p.C0 = c0;
		p.S = s;
		p.K1 = p.rho0 * c0 * c0;
		p.K2 = (2f * s - 1f) * p.K1;
		p.K3 = (3f * s * s - 4f * s + 1f) * p.K1;
		return p;
	}

	private static void Build()
	{
		if (table != null)
		{
			return;
		}
		int count = Enum.GetValues(typeof(ContinuumMaterialId)).Length;
		table = new ContinuumProperties[count];
		sources = new string[count];
		caveats = new string[count][];
		table[0] = Make(new ContinuumProperties
		{
			rho0 = 17300f,
			G = 1.6e+11f,
			K = 2.85999989e+11f,
			C0 = 4000f,
			S = 1.23000002f,
			Gamma0 = 1.53999996f,
			a_eos = 0f,
			K1 = 0f,
			K2 = 0f,
			K3 = 0f,
			JC_A = 1.09299994e+09f,
			JC_B = 1.27e+09f,
			JC_n = 0.419999987f,
			JC_C = 0.0187999997f,
			JC_m = 0.779999971f,
			epsDot0 = 10000f,
			epsSat = 1f,
			T_room = 293f,
			T_melt = 1850f,
			C_p = 134f,
			epsASB = 0.5f,
			dEpsFade = 0.300000012f,
			hasASB = 1,
			JC_D1 = 0f,
			JC_D2 = 0f,
			JC_D3 = 0f,
			JC_D4 = 0f,
			JC_D5 = 0f,
			hasDamage = 0,
			spallPa = -1.9e+09f,
			sigma1MaxPa = 0f,
			cavitationPa = 0f,
			provenance = ProvenanceTag.FITTED
		});
		sources[0] = "Прочность: Yadav & Ramesh (91W-7Ni-2Fe), FITTED; K = 286 ГПа DERIVED из E,ν; C0 = 4000 / S = 1.23 — FITTED, НЕ MEASURED: это почти наверняка подгонка по Fansteel-77 (4.008 км/с и 1.262 по 17 точкам симметричных соударений), то есть ПОДОГНАННЫЕ отрезок и наклон, а не измеренная пара; Γ0 = 1.54 — ASSUMED: первичного измерения коэффициента Грюнайзена НЕТ НИ ДЛЯ ОДНОГО вольфрамового сплава, всё найденное есть либо γ, ПРИНЯТОЕ автором ради сведения импедансов, либо один подогнанный параметр таблицы SESAME; ⚠ ПОДСТАНОВКА ЧЕРЕЗ СОСТАВ: набор течения (Yadav & Ramesh, 91W-7Ni-2Fe) и ударная адиабата (Fansteel-77) принадлежат РАЗНЫМ вольфрамовым сплавам, а у двухфазного композита отклик зависит от доли фаз — ТЕГ ПОДСТАНОВКУ НЕ ПЕРЕЖИВАЕТ; пол откола −1.9 ГПа MEASURED (откол кованого WHA) — эта пометка СВЕРКУ ВЫДЕРЖАЛА, подтверждена независимым вторым извлечением (с S19 живёт в spallPa, значение то же)";
		caveats[0] = new string[8]
		{
			"WHA: набор повреждаемости Джонсона–Кука отсутствует — ветка D выключена, разрушение идёт по ε_erode и по спаду прочности в полосе сдвига.",
			"WHA: пол откола −1.9 ГПа относится к КОВАНОМУ материалу. У отожжённого откольная прочность ниже 1.0 ГПа — при смене состояния поставки константа меняется вдвое.",
			"WHA: ε̇₀ = 10⁴ 1/с. Подстановка 1.0 завысила бы предел текучести примерно на 17 % при 10⁵ 1/с.",
			"WHA: полного набора JC-повреждаемости для класса 91W-7Ni-2Fe НЕ СУЩЕСТВУЕТ (бриф 20); опубликованные кандидаты расходятся на порядки (D3 от −1.004 до 17.434), все заимствованы или подогнаны под баллистику. Отрасль пользуется Кокрофтом–Латамом, порогом по максимальному главному напряжению (~2.0 ГПа) и критериями ПАС.",
			"WHA: наш пол −1.9 ГПа (FITTED, кованый) НИЖЕ измеренной полосы родственных марок (93W-Ni-Fe 2.1–2.5; 91W-Ni-Co катаный 2.8; 90W-7Ni-3Fe 3.8; чистый W ~0.4). Направление ошибки известно: наш лом откалывается охотнее реального. Число не меняется — подстановка чужой марки обменяла бы известную ошибку на неизвестную.",
			"WHA: порог по максимальному главному напряжению ОТОЗВАН 02.09 в день введения (sigma1MaxPa = 0, ветвь жива и ждёт брифа 27). Стояло 2.0 ГПа из брифа 20 — но это ЧИСЛЕННЫЙ ПРИЁМ лагранжевых кодов (удаление искажённых элементов, подбор под сетку), а не измеренное свойство; в MPM спасать нечего. Плюс замер: стержень штатно работает на 1.99 ГПа против порога 2.00 — критерий стоял на рабочем напряжении вещества. Плюс порядок: одноосный порог обязан лежать ВЫШЕ трёхосного откола (1.9), а не вровень.",
			"WHA: Кокрофт–Латам (который бриф 20 называет лучшим для «грибка» 91W/93W) НЕ реализован — критического значения W_c кампания не вернула. Бриф 27.",
			"WHA: пол откола −1.9 ГПа с S19 читает ТОЛЬКО ветвь разрушения Super. У L1 (ULTRA) тензильного пола НЕ ОСТАЛОСЬ ВОВСЕ: spallPa этот трек не читает нигде, а зажим растяжения в уравнении состояния удалён. Значит в ULTRA вольфрам тянется без порога — это ОБЪЯВЛЕННОЕ отсутствие механизма, а не свойство вещества, и замены в дереве нет. Разбор и замер сдвига — REPORT S19 (06.09).",
		};
		table[1] = Make(new ContinuumProperties
		{
			rho0 = 7830f,
			G = 7.69999995e+10f,
			K = 1.59000003e+11f,
			C0 = 4580f,
			S = 1.49000001f,
			Gamma0 = 2f,
			a_eos = 0f,
			K1 = 0f,
			K2 = 0f,
			K3 = 0f,
			JC_A = 792000000f,
			JC_B = 510000000f,
			JC_n = 0.25999999f,
			JC_C = 0.0140000004f,
			JC_m = 1.02999997f,
			epsDot0 = 1f,
			epsSat = 1f,
			T_room = 300f,
			T_melt = 1793f,
			C_p = 477f,
			epsASB = 0.5f,
			dEpsFade = 0.300000012f,
			hasASB = 1,
			JC_D1 = 0.0500000007f,
			JC_D2 = 3.44000006f,
			JC_D3 = -2.11999989f,
			JC_D4 = 0.00200000009f,
			JC_D5 = 0.610000014f,
			hasDamage = 1,
			spallPa = 0f,
			sigma1MaxPa = 0f,
			cavitationPa = 0f,
			provenance = ProvenanceTag.NOT_SUPPLIED_BY_BRIEF
		});
		sources[1] = "Течение: библиотека AutoDYN, FITTED. УС: C0/S MEASURED. Повреждаемость: канонический набор Johnson & Cook 1985 — ТЗ чисел не дало";
		caveats[1] = new string[2]
		{
			"4340: D1..D5 = (0.05, 3.44, −2.12, 0.002, 0.61) взяты из канонической публикации — ТЗ ULTRA этих чисел НЕ ПРИВОДИТ. Проверить перед калибровкой (фаза 4).",
			"4340: Γ₀ = 2.0 ASSUMED (в таблице ТЗ прочерк).",
		};
		table[2] = Make(new ContinuumProperties
		{
			rho0 = 7830f,
			G = 7.69999995e+10f,
			K = 1.59000003e+11f,
			C0 = 4580f,
			S = 1.49000001f,
			Gamma0 = 2f,
			a_eos = 0f,
			K1 = 0f,
			K2 = 0f,
			K3 = 0f,
			JC_A = 780000000f,
			JC_B = 780000000f,
			JC_n = 0.105999999f,
			JC_C = 0.00400000019f,
			JC_m = 0.899999976f,
			epsDot0 = 1f,
			epsSat = 1f,
			T_room = 293f,
			T_melt = 1793f,
			C_p = 477f,
			epsASB = 0.5f,
			dEpsFade = 0.300000012f,
			hasASB = 1,
			JC_D1 = 0.0500000007f,
			JC_D2 = 3.44000006f,
			JC_D3 = -2.11999989f,
			JC_D4 = 0.00200000009f,
			JC_D5 = 0.610000014f,
			hasDamage = 1,
			spallPa = 0f,
			sigma1MaxPa = 0f,
			cavitationPa = 0f,
			provenance = ProvenanceTag.ASSUMED_BY_SUBSTITUTION
		});
		sources[2] = "Meyer & Kleponis, ARL-TR-2528 (2001), табл. 5, набор 2 (рекомендованный для 2\" RHA): A/B/n/C — фит по опытным кривым (Benck 1976; Gray и др. 1994), проверен в CTH; m = 0.90 DERIVED (у источника 1.00); УС/C_p/ρ₀/G/K заимствованы у 4340";
		caveats[2] = new string[4]
		{
			"RHA: собственного набора повреждаемости Джонсона–Кука в открытой печати НЕТ — подставлен набор 4340 (ASSUMED_BY_SUBSTITUTION). Повреждаемость здесь ВТОРИЧНА: пробка образуется спадом прочности в полосе сдвига (§1.8b), а не по D ≥ 1.",
			"RHA: набор проверен авторами в 3D CTH при ячейке 2 мм (10 на ⌀ стержня) и одном пороге откола pfrac 2.5 ГПа — у нас откольного пола нет, повреждаемость от 4340.",
			"RHA: до S35 (24.09.2026) стоял C = 0.0891 — набор 6 того же отчёта, отвергнутый авторами.",
			"RHA: ε̇₀ = 1 с⁻¹ — нормировка фита источника (совпадает); C_p, ρ₀, G, K, C0, S — от 4340; T_melt у источника 1783 K (здесь 1793 K от 4340).",
		};
		table[3] = Make(new ContinuumProperties
		{
			rho0 = 7830f,
			G = 7.69999995e+10f,
			K = 1.59000003e+11f,
			C0 = 4580f,
			S = 1.49000001f,
			Gamma0 = 2f,
			a_eos = 0f,
			K1 = 0f,
			K2 = 0f,
			K3 = 0f,
			JC_A = 900000000f,
			JC_B = 1.30499994e+09f,
			JC_n = 0.899999976f,
			JC_C = 0.057500001f,
			JC_m = 0.899999976f,
			epsDot0 = 1f,
			epsSat = 1f,
			T_room = 293f,
			T_melt = 1793f,
			C_p = 477f,
			epsASB = 0.5f,
			dEpsFade = 0.300000012f,
			hasASB = 1,
			JC_D1 = 0.0500000007f,
			JC_D2 = 3.44000006f,
			JC_D3 = -2.11999989f,
			JC_D4 = 0.00200000009f,
			JC_D5 = 0.610000014f,
			hasDamage = 1,
			spallPa = 0f,
			sigma1MaxPa = 0f,
			cavitationPa = 0f,
			provenance = ProvenanceTag.ASSUMED_BY_SUBSTITUTION
		});
		sources[3] = "Grey et al., подгонка по SHPB; m заменено на 0.90 (DERIVED) вместо неатрибутированных 1.075";
		caveats[3] = new string[1]
		{
			"RHA набор B: n = 0.900 действителен лишь при ε_p < 0.35 (окно фита SHPB). При деформациях кратера 2…5 без потолка §1.8a даёт 3.3–6.5 ГПа — броня крепчала бы от ударов.",
		};
		table[4] = Make(new ContinuumProperties
		{
			rho0 = 2660f,
			G = 2.69000008e+10f,
			K = 5.82999982e+10f,
			C0 = 4682f,
			S = 1.45000005f,
			Gamma0 = 2f,
			a_eos = 0f,
			K1 = 0f,
			K2 = 0f,
			K3 = 0f,
			JC_A = 167000000f,
			JC_B = 596000000f,
			JC_n = 0.550999999f,
			JC_C = 0.00100000005f,
			JC_m = 0.859000027f,
			epsDot0 = 1f,
			epsSat = 1f,
			T_room = 293f,
			T_melt = 893f,
			C_p = 910f,
			epsASB = 0f,
			dEpsFade = 0f,
			hasASB = 0,
			JC_D1 = 0f,
			JC_D2 = 0f,
			JC_D3 = 0f,
			JC_D4 = 0f,
			JC_D5 = 0f,
			hasDamage = 0,
			spallPa = -810000000f,
			sigma1MaxPa = 0f,
			cavitationPa = 0f,
			provenance = ProvenanceTag.FITTED
		});
		sources[4] = "Библиотека AutoDYN, FITTED; C0 = 4682 и S = 1.45 DERIVED/ASSUMED по §1.6; откол spallPa = −0.81 ГПа — плоское соударение катаного 5083-H116, полоса 0.81–0.95 ГПа ПО ТОЛЩИНЕ (1.06 вдоль проката), HEL 0.43–0.44 ГПа; взят минимум по толщине — ориентация лобового удара, MEASURED (бриф 21 / ТЗ S9 §1.2)";
		caveats[4] = new string[5]
		{
			"Al 5083-H116: стандартного набора JC-повреждаемости не существует — у сплава отрицательная чувствительность к скорости деформации (Портевен–Ле Шателье), логарифмический член JC её описать не может. Разрушение идёт ТОЛЬКО отколом.",
			"Al 5083-H116: набор Clausen/Børvik — от МОДИФИЦИРОВАННОЙ JC, в эту строку не подставлять.",
			"Al 5083-H116: модель изотропна; измеренная анизотропия откола (0.81 по толщине против 1.06 вдоль проката) не воспроизводится, взят меньший (У9).",
			"Al 5083: полосы адиабатического сдвига выключены — измеренной опоры ε_ASB для этого сплава нет.",
			"Al 5083: C0 DERIVED по √(K/ρ₀), S = 1.45 ASSUMED, Γ₀ = 2.0 ASSUMED.",
		};
		table[5] = Make(new ContinuumProperties
		{
			rho0 = 4430f,
			G = 4.37999985e+10f,
			K = 1.11800001e+11f,
			C0 = 5130f,
			S = 1.028f,
			Gamma0 = 1.23000002f,
			a_eos = 0f,
			K1 = 0f,
			K2 = 0f,
			K3 = 0f,
			JC_A = 1.098e+09f,
			JC_B = 1.092e+09f,
			JC_n = 0.930000007f,
			JC_C = 0.0140000004f,
			JC_m = 1.10000002f,
			epsDot0 = 1f,
			epsSat = 1f,
			T_room = 293f,
			T_melt = 1878f,
			C_p = 560f,
			epsASB = 0.5f,
			dEpsFade = 0.300000012f,
			hasASB = 1,
			JC_D1 = -0.0900000036f,
			JC_D2 = 0.25f,
			JC_D3 = -0.5f,
			JC_D4 = 0.0140000004f,
			JC_D5 = 3.86999989f,
			hasDamage = 1,
			spallPa = 0f,
			sigma1MaxPa = 0f,
			cavitationPa = 0f,
			provenance = ProvenanceTag.FITTED
		});
		sources[5] = "LLNL / набор FAA по удержанию, FITTED — ЕДИНСТВЕННЫЙ РОДНОЙ набор повреждаемости в таблице; K = 111.8 ГПа DERIVED";
		caveats[5] = new string[3]
		{
			"Ti-6Al-4V: n = 0.93 аномально высок и получен узким фитом — кандидат на перефит, если калибровка фазы 4 будет с ним бороться.",
			"Ti-6Al-4V: Джонсон–Кук изотропен, сплав — нет. Пробка в титане есть результат сетки, а не предсказание анизотропии.",
			"Ti-6Al-4V: набор повреждаемости почти совпадает со строкой «Maraging 300» из задачи №6 — верна ТИТАНОВАЯ атрибуция, строка мартенситной стали загрязнена (записано, чтобы совпадение не приняли за независимое подтверждение).",
		};
		table[6] = Make(new ContinuumProperties
		{
			rho0 = 8930f,
			G = 4.77000008e+10f,
			K = 1.38625745e+11f,
			C0 = 3940f,
			S = 1.49000001f,
			Gamma0 = 1.99000001f,
			a_eos = 0f,
			K1 = 0f,
			K2 = 0f,
			K3 = 0f,
			JC_A = 90000000f,
			JC_B = 292000000f,
			JC_n = 0.310000002f,
			JC_C = 0.0250000004f,
			JC_m = 1.09000003f,
			epsDot0 = 1f,
			epsSat = 1f,
			T_room = 294f,
			T_melt = 1356f,
			C_p = 392.399994f,
			epsASB = 0f,
			dEpsFade = 0f,
			hasASB = 0,
			JC_D1 = 0.540000021f,
			JC_D2 = 4.88999987f,
			JC_D3 = -3.02999997f,
			JC_D4 = 0.0140000004f,
			JC_D5 = 1.12f,
			hasDamage = 1,
			spallPa = -1.31e+09f,
			sigma1MaxPa = 0f,
			cavitationPa = 0f,
			provenance = ProvenanceTag.NOT_SUPPLIED_BY_BRIEF
		});
		sources[6] = "Johnson & Cook 1983/85 (канонический набор течения и УС); K DERIVED = ρ₀C₀², G = 47.7 ГПа справочный — ТЗ модулей не дало; повреждаемость D1..D5 = 0.54/4.89/−3.03/0.014/1.12 — Johnson & Cook 1985, Eng. Fract. Mech. 21(1) 31–48, ПЕРВИЧНАЯ публикация, MEASURED; откол spallPa = −1.31 ГПа — Dennis-Koller et al. 2011, J. Appl. Phys., плоское соударение + VISAR, отожжённая OFHC 99.999 %, полоса 1.31–1.38 ГПа по зёрнам 30–200 мкм при ~10⁴ 1/с, взят минимум полосы, MEASURED. ⚠ Тег строки ОСТАЁТСЯ NOT_SUPPLIED_BY_BRIEF: G справочный, K DERIVED — слабейший тег правит строкой, MEASURED здесь только два входа отказа (ТЗ S9 §1.1)";
		caveats[6] = new string[6]
		{
			"Медь OFHC: только тестовая таблица и струя S6, в песочнице недоступна (§3.5, §5 ТЗ).",
			"Медь OFHC: G и K в таблице ТЗ отсутствуют. K выведен из C₀ (иначе УС противоречит себе), G взят справочный.",
			"Медь: набор D1..D5 подогнан до ~10³–10⁴ с⁻¹, применяется на 10⁶–10⁷ с⁻¹ (У7). Выше ~10⁴ с⁻¹ логарифмическое упрочнение сменяется вязким торможением дислокаций (Follansbee & Kocks 1988); отрасль в этой зоне JC не использует.",
			"Медь: пол откола измерен на ОТОЖЖЁННОЙ меди. В струе вещество уже растянуто до ε ≈ 10, и реальный пол выше (5–6 ГПа на 10⁷ с⁻¹). Наша струя распадается РАНЬШЕ реальной (У8).",
			"Медь: пол откола слеп ко ВРЕМЕНИ (У13): срабатывает по мгновенному давлению, а измеренная прочность меди растёт с укорочением импульса (1.31–1.38 ГПа на мкс против 5–6 ГПа на нс). В струе пол консервативен, в коротком импульсе переусерден. Тейлор 190 м/с заходит глубже 1.31 ГПа и НЕ откалывается (бриф 23) — потому пол есть вход ВЕТВИ РАЗРУШЕНИЯ Super, а не потолок в уравнении состояния: решение 02.09, слияние полей — S19 (06.09).",
			"Медь: набор, который в части литературы циркулирует как «медный», принадлежит стали (AISI 1045/4340) и совпадает со строкой Steel4340 этого реестра. Заимствование стального набора в медь — документированная чужая ошибка, повторять её запрещено.",
		};
		table[7] = Make(new ContinuumProperties
		{
			rho0 = 7850f,
			G = 8.13999964e+10f,
			K = 1.63946463e+11f,
			C0 = 4570f,
			S = 1.49000001f,
			Gamma0 = 2f,
			a_eos = 0f,
			K1 = 0f,
			K2 = 0f,
			K3 = 0f,
			JC_A = 1.58e+09f,
			JC_B = 2.90499994e+09f,
			JC_n = 0.116999999f,
			JC_C = 0.075000003f,
			JC_m = 1.16999996f,
			epsDot0 = 1f,
			epsSat = 1f,
			T_room = 300f,
			T_melt = 1793f,
			C_p = 477f,
			epsASB = 0f,
			dEpsFade = 0f,
			hasASB = 0,
			JC_D1 = 0.0359999985f,
			JC_D2 = 0.0829999968f,
			JC_D3 = -3f,
			JC_D4 = 0f,
			JC_D5 = 0f,
			hasDamage = 1,
			spallPa = -3.9e+09f,
			sigma1MaxPa = 0f,
			cavitationPa = 0f,
			provenance = ProvenanceTag.FITTED
		});
		sources[7] = "Течение: A. Morka et al., модель бронебойно-зажигательного Б-32 — статическое сжатие + модифицированный стержень Гопкинсона, MEASURED. Разрушение D1..D5: тот же источник, ИТЕРАТИВНАЯ ПОДГОНКА под баллистическую валидацию (глубина + рентген фрагментации), FITTED ⇒ тег строки FITTED. G = 81.4 ГПа DERIVED из E = 210 ГПа и ν = 0.29; K DERIVED = ρ₀C₀² по прецеденту OFHC (K₁ полинома обязан равняться K при μ = 0, иначе УС противоречит себе; перекрёстно: K из (E, ν) = 166.7 ГПа, расхождение 1.7 %). C0 = 4570 — объёмная скорость звука самого источника, MEASURED. S = 1.49 и Γ₀ = 2.0 — ПОДСТАНОВКА от строки Steel4340 (ASSUMED_BY_SUBSTITUTION, У10): ударная адиабата низколегированной стали к отпуску нечувствительна, но подстановка есть подстановка. spallPa = −3.9 ГПа — ASSUMED_BY_SUBSTITUTION (минимум полосы закалённого мартенсита, своего числа нет)";
		caveats[7] = new string[5]
		{
			"N12e: D1..D5 подогнаны итеративно под глубину пробития и рентген фрагментации, не измерены. Извлечь их прямым испытанием на растяжение на Гопкинсоне для стали 60 HRC невозможно — образцы хрупко разрушаются в захватах.",
			"N12e: ε̇₀ в источнике НЕ УКАЗАНА при C = 0.075. Приёмка S9 обязана прогнать развёртку ε̇₀ ∈ {10⁻³, 1.0} (ТЗ §5); в реестре оставлена 1.0 — как у всех строк таблицы.",
			"N12e: ПАС не моделируется (У12), хотя литература называет её доминирующим механизмом.",
			"N12e: наборы 4340@55HRC (A = 792 МПа) и 52100 (A = 688 МПа) получены обратной идентификацией из РЕЗАНИЯ; тепловое насыщение занижает предел текучести. Для сердечника они непригодны — именно поэтому эта строка существует. Строка Steel4340 этого реестра несёт A = 792 МПа и в роль сердечника НЕ ГОДИТСЯ.",
			"N12e: у «твёрдого сердечника B32» в литературе n = 0.89 — искусственная подпорка против численной эрозии в чужом гидрокоде, не измерение. Не заимствовать.",
		};
		table[8] = Make(new ContinuumProperties
		{
			rho0 = 1264f,
			G = 0f,
			K = 8.94355866e+09f,
			C0 = 2660f,
			S = 1.48000002f,
			Gamma0 = 1f,
			a_eos = 0f,
			K1 = 0f,
			K2 = 0f,
			K3 = 0f,
			JC_A = 0f,
			JC_B = 0f,
			JC_n = 1f,
			JC_C = 0f,
			JC_m = 1f,
			epsDot0 = 1f,
			epsSat = 1f,
			T_room = 300f,
			T_melt = 600f,
			C_p = 1800f,
			epsASB = 0f,
			dEpsFade = 0f,
			hasASB = 0,
			JC_D1 = 0f,
			JC_D2 = 0f,
			JC_D3 = 0f,
			JC_D4 = 0f,
			JC_D5 = 0f,
			hasDamage = 0,
			spallPa = 0f,
			sigma1MaxPa = 0f,
			cavitationPa = -50000000f,
			provenance = ProvenanceTag.ASSUMED
		});
		sources[8] = "ρ₀ = 1264, C₀ = 2660, S = 1.48 — Carter & Marsh, плотный полиуретан, MEASURED. G = 0 и A = 0 — МОДЕЛЬ, а не пробел: отрасль считает связанные эластомерные прослойки жидкостью с ударным УС (бриф 25); не «чинить». Γ₀ = 1.0, T_melt, C_p — ASSUMED (не поставлены) ⇒ тег строки ASSUMED. Неопрен с S30 — СВОЯ строка Neoprene_Interlayer (Millett et al. 2004, бриф 33). Натуральная резина отображается в ЭТУ строку с ASSUMED_BY_SUBSTITUTION: её C₀/S брифа 33 — вход чужой КЭ-модели композита, а не измерение (решение 21.09 §2). cavitationPa = −50 МПа — ASSUMED (полоса −10…−200 МПа; бриф 37 запрошен S37 23.09; значение не подбиралось под ворота; включается флагом SuperDamage.CavitationOn — ВКЛЮЧЁН по умолчанию словом владельца 24.09.2026 после приёмки S37, выключается доводом -s37cav 0; ⚠️ величина среза ограничена ЧИСЛЕННЫМ потолком SuperDamage.CavitationMaxFractionOfK·K (0.6 % K, MEASURED S37: 2 % K возвращает разлёт у обоих эластомеров) — у полиуретана −50 МПа = 0.56 % K проходит, у неопрена (K 2.48 ГПа) применяется −14.9 МПа).";
		caveats[8] = new string[4]
		{
			"Полиуретан: строка ТОЛЬКО для прослоек НДЗ в Super; бронёй не является и в песочнице как броня не выбирается.",
			"Полиуретан: G = 0 — состояние, которого не было ни у одной строки; звук √(K/ρ) = C₀, девиатора нет тождественно. Приёмка S11 §3.1 обязана прогнать слоёнку до сцен НДЗ.",
			"Полиуретан: Γ₀ = 1.0 ASSUMED, T_melt = 600 К и C_p = 1800 ASSUMED — брифом не поставлены.",
			"cavitationPa = −50 МПа — ASSUMED (полоса −10…−200 МПа; бриф 37 запрошен S37 23.09; значение не подбиралось под ворота; включается флагом SuperDamage.CavitationOn — ВКЛЮЧЁН по умолчанию словом владельца 24.09.2026 после приёмки S37, выключается доводом -s37cav 0; ⚠️ величина среза ограничена ЧИСЛЕННЫМ потолком SuperDamage.CavitationMaxFractionOfK·K (0.6 % K, MEASURED S37: 2 % K возвращает разлёт у обоих эластомеров) — у полиуретана −50 МПа = 0.56 % K проходит, у неопрена (K 2.48 ГПа) применяется −14.9 МПа).",
		};
		ContinuumProperties cast = table[2];
		cast.JC_D1 *= CastDamageScale;
		cast.JC_D2 *= CastDamageScale;
		cast.provenance = ProvenanceTag.ASSUMED_BY_SUBSTITUTION;
		table[9] = Make(cast);
		ContinuumProperties neoprene = table[8];
		neoprene.rho0 = 1230f;
		neoprene.K = 2480172032f;
		neoprene.C0 = 1420f;
		neoprene.S = 1.75f;
		neoprene.provenance = ProvenanceTag.ASSUMED;
		table[10] = Make(neoprene);
		sources[10] = "ρ₀ = 1230 ± 30, C₀ = 1420 ± 30, S = 1.75 ± 0.07 — Millett J.C.F., Bourne N.K., Akhavan J., «The shock response of the elastomer, polychloroprene», J. Appl. Phys. 95, 4722 (2004): коммерческий полихлоропрен без заметного сажевого наполнения; ρ₀ согласуется с LASL (Marsh 1980: Neoprene 1158 и G91 при 1.24). Помета чисел — ПОВТОР ИЗ ВОЗВРАТА БРИФА 33 до сверки с таблицей самой статьи (решение 21.09 §1.1); MEASURED ставится после сверки. K = ρ₀C₀² — DERIVED. G = 0 и A = 0 — МОДЕЛЬ жидкости, как у полиуретана (бриф 25); не «чинить». Γ₀ = 1.0, a_eos, T_melt = 600 К, C_p = 1800 — ПОДСТАНОВКА от строки полиуретана (ASSUMED_BY_SUBSTITUTION: брифом 33 не найдены), сами они у полиуретана ASSUMED ⇒ тег строки ASSUMED. cavitationPa = −50 МПа — ASSUMED (полоса −10…−200 МПа; бриф 37 запрошен S37 23.09; значение не подбиралось под ворота; включается флагом SuperDamage.CavitationOn — ВКЛЮЧЁН по умолчанию словом владельца 24.09.2026 после приёмки S37, выключается доводом -s37cav 0; ⚠️ величина среза ограничена ЧИСЛЕННЫМ потолком SuperDamage.CavitationMaxFractionOfK·K (0.6 % K, MEASURED S37: 2 % K возвращает разлёт у обоих эластомеров) — у полиуретана −50 МПа = 0.56 % K проходит, у неопрена (K 2.48 ГПа) применяется −14.9 МПа).";
		caveats[10] = new string[6]
		{
			"Неопрен: строка ТОЛЬКО для прослоек НДЗ и плиты-эластомера в Super; бронёй не является.",
			"Неопрен: фит адиабаты измерен на u_p = 0.125…2 км/с, то есть ДО ≈ 12 ГПа (P = ρ₀·U_s·u_p = 1230 × 4920 × 2000 ≈ 12.1 ГПа). Выше — ЭКСТРАПОЛЯЦИЯ. «20–25 ГПа» возврата — общая феноменология полимеров Картера–Марша, на неопрене не измерена.",
			"Неопрен: Γ₀, T_melt и C_p ПОДСТАВЛЕНЫ от строки полиуретана — у неопрена их не нашёл бриф 33, а у полиуретана они сами ASSUMED.",
			"Неопрен: аналитическая таблица эластомеров (NeraArmor) несёт ДРУГОЙ образец — ρ 1420, c₀ 1400, s 4.000 (иссл. №10 §A5). Две таблицы одного имени расходятся по давлению при u_p = 1 км/с почти вдвое (7.7 против 3.9 ГПа); аналитический трек S30 не трогал, расхождение — решение владельца.",
			"Неопрен: G = 0 — девиатора нет тождественно, звук √(K/ρ) = C₀ = 1420 м/с (у полиуретана 2660): импеданс ρ₀C₀ вдвое ниже полиуретанового, K — в 3.6 раза.",
			"cavitationPa = −50 МПа — ASSUMED (полоса −10…−200 МПа; бриф 37 запрошен S37 23.09; значение не подбиралось под ворота; включается флагом SuperDamage.CavitationOn — ВКЛЮЧЁН по умолчанию словом владельца 24.09.2026 после приёмки S37, выключается доводом -s37cav 0; ⚠️ величина среза ограничена ЧИСЛЕННЫМ потолком SuperDamage.CavitationMaxFractionOfK·K (0.6 % K, MEASURED S37: 2 % K возвращает разлёт у обоих эластомеров) — у полиуретана −50 МПа = 0.56 % K проходит, у неопрена (K 2.48 ГПа) применяется −14.9 МПа).",
		};
		table[11] = Make(new ContinuumProperties
		{
			rho0 = 18600f,
			G = 8.54000026e+10f,
			K = 1.20756855e+11f,
			C0 = 2548f,
			S = 1.45700002f,
			Gamma0 = 2f,
			a_eos = 0f,
			K1 = 0f,
			K2 = 0f,
			K3 = 0f,
			JC_A = 1.07900006e+09f,
			JC_B = 1.12e+09f,
			JC_n = 0.25f,
			JC_C = 0.00700000022f,
			JC_m = 1f,
			epsDot0 = 1f,
			epsSat = 1f,
			T_room = 293f,
			T_melt = 1473f,
			C_p = 117f,
			epsASB = 0f,
			dEpsFade = 0f,
			hasASB = 0,
			JC_D1 = 0f,
			JC_D2 = 0f,
			JC_D3 = 0f,
			JC_D4 = 0f,
			JC_D5 = 0f,
			hasDamage = 0,
			spallPa = -3.5e+09f,
			sigma1MaxPa = 0f,
			cavitationPa = 0f,
			provenance = ProvenanceTag.ASSUMED_BY_SUBSTITUTION
		});
		sources[11] = "ρ₀ = 18600, c_p = 117, T_melt = 1473 K — Johnson & Cook 1983, Proc. 7th Int. Symp. Ballistics, Таблица 1, строка DU-.75Ti (извлечение со скана, Docs/Research/ArmorSim_JohnsonCook_1983_Table1_EXTRACTED.md), MEASURED (справочные). A = 1079 МПа, B = 1120 МПа, n = 0.25, C = 0.007 при ε̇₀ = 1.0 с⁻¹ — FITTED (только кручение — слова авторов); m = 1.00 — ASSUMED САМИМИ АВТОРАМИ («линейное разупрочнение предполагается»). C₀ = 2548 м/с, S = 1.457 — ФИТ ПО 11 СЫРЫМ ТОЧКАМ α-урана LASL 1980 (Marsh (ed.), LASL Shock Hugoniot Data, p. 148; цитата — Docs/Research/brief-18-…-ANSWER.md §2.1), u_p 0.29…1.20 км/с, СКО 28 м/с, — ASSUMED_BY_SUBSTITUTION: ЭЛЕМЕНТ ВМЕСТО СПЛАВА, как велит сам ответ брифа 18 (U-0.75Ti там NOT FOUND). K = ρ₀C₀² = 120.76 ГПа — DERIVED (правило реестра: K едет вместе с C₀). G = 85.4 ГПа = ρ₀c_S² по LASL (c_S 2.12 км/с, ρ 19.0) — DERIVED, спорно (Isbell: 27.3). Γ₀ = 2.0 — DefaultGamma0, ASSUMED (соглашение реестра: Γ₀ не найден ни для одного урана). Откол spallPa = −3.5 ГПа — середина полосы −2.9…−4.1 ГПа (Zurek 1994/1999 через бриф 32, по тексту ТЗ S31 §1.2), ПОВТОР до сверки с первоисточником. D1–D5 — нет нигде ⇒ hasDamage = 0. ⚠ Тег строки ASSUMED_BY_SUBSTITUTION — слабейшая помета (адиабата элемента) правит строкой.";
		caveats[11] = new string[7]
		{
			"Уран: плотность СПЛАВА 18600 кг/м³ (JC 1983) при адиабате ЭЛЕМЕНТА, снятой на 18930–19000 (LASL 1980) — объявленное расхождение 1.8 %. Константы ходят наборами: плотность — от строки прочности, адиабата — от своих точек; свести их нечем (U-0.75Ti на адиабате NOT FOUND).",
			"Уран: САМОЗАТАЧИВАНИЯ НЕТ. m = 1.00 — линейное разупрочнение (ASSUMED самими авторами), а оно подавляет полосы адиабатического сдвига; механизма полосы в Super нет вовсе (оценка S28). Стержень урана в Super ГРИБИТСЯ, КАК ВОЛЬФРАМОВЫЙ — это объявлено на кнопке и в паспорте.",
			"Уран: модуль сдвига — ДВА ИСТОЧНИКА ВТРОЕ: LASL c_S = 2.12 км/с ⇒ G = 85.4 ГПа (ν 0.20) против Isbell c_S = 1.20 км/с ⇒ G = 27.3 ГПа (ν 0.40). Взят LASL; расхождение — в самих измеренных скоростях звука, третьего замера нет. Чувствительность выстрела к G меряет приёмка S31 (продольная скорость звука 3552 против 2907 м/с).",
			"Уран: адиабата — фит по 11 процитированным точкам LASL p. 148 (u_p 0.294…1.197 км/с, до ≈ 97 ГПа); у носа стержня (u_p 0.4–0.6) 23–38 ГПа — ВНУТРИ измеренного участка. Полной страницы LASL на диске нет. Фит Isbell (90–460 ГПа) в баллистическом диапазоне — экстраполяция, не взят. α-уран орторомбичен и анизотропен; изотропная формула объёмной скорости к нему применена LASL без оговорки (ответ брифа 18 §1.1).",
			"Уран: откол −3.5 ГПа — середина полосы −2.9…−4.1 ГПа (Zurek, после ПРЕДВАРИТЕЛЬНОГО нагружения), помета «повтор»: число взято из текста ТЗ S31, первоисточник не сверялся. Пол слеп ко времени (как у всех строк, У13).",
			"Уран: повреждаемости Джонсона–Кука НЕТ (D1–D5 не опубликованы; JC 1985 — медь, Armco, 4340) ⇒ hasDamage = 0: вещество рвётся ТОЛЬКО отколом. ПАС выключена (порога ε_ASB для U-0.75Ti в источниках нет). Порог σ₁ — 0, источника нет.",
			"Уран: T_melt = 1473 K (JC 1983), а не 1405 K элементного урана — константы JC подобраны под свою T_melt и ходят набором. T_room = 293 K — ASSUMED (в извлечении Таблицы 1 не приведена), Γ₀ = 2.0 — ASSUMED (не найден ни для одного урана).",
		};
		table[12] = Make(new ContinuumProperties
		{
			rho0 = 1060f,
			G = 1.34500004e+10f,
			K = 2.91499991e+10f,
			C0 = 5244f,
			S = 1.45000005f,
			Gamma0 = 2f,
			a_eos = 0f,
			K1 = 0f,
			K2 = 0f,
			K3 = 0f,
			JC_A = 83500000f,
			JC_B = 298000000f,
			JC_n = 0.550999999f,
			JC_C = 0.00100000005f,
			JC_m = 0.859000027f,
			epsDot0 = 1f,
			epsSat = 1f,
			T_room = 293f,
			T_melt = 893f,
			C_p = 910f,
			epsASB = 0f,
			dEpsFade = 0f,
			hasASB = 0,
			JC_D1 = 0f,
			JC_D2 = 0f,
			JC_D3 = 0f,
			JC_D4 = 0f,
			JC_D5 = 0f,
			hasDamage = 0,
			spallPa = -405000000f,
			sigma1MaxPa = 0f,
			cavitationPa = 0f,
			provenance = ProvenanceTag.ASSUMED
		});
		sources[12] = "Слово владельца 30.09.2026 («экипаж вдвое мягче алюминия»), ASSUMED: строка Al 5083-H116 (AutoDYN, FITTED) с G, K, JC A, JC B и spallPa × 0.5; ρ₀ = 1060 кг/м³ — CrewTissueModel (мышца/внутренний орган, ASSUMED-ESTIMATED); C₀ = √(K/ρ₀) DERIVED; прочее — как у Al 5083.";
		caveats[12] = new string[4]
		{
			"Экипаж: условная ткань по слову владельца, не раневая баллистика; ни одного замера за строкой нет.",
			"Экипаж: продольная волна ≈ 6.7 км/с — быстрее алюминия (модуль ×0.5 при плотности 1060).",
			"Экипаж: разрушение только отколом (−0.405 ГПа), повреждаемости Джонсона–Кука нет, ПАС выключена.",
			"Экипаж: органы манекена «Плоскости» — РАСКРАСКА одного вещества, свойства тканей не различаются.",
		};
		sources[9] = "ρ₀/G/K/C₀/S/Γ₀, течение A/B/n/C/m, ПАС и ε̇₀/C_p/T — ЦЕЛИКОМ строка RHA (набор A, Meyer & Kleponis), которая сама заимствует УС/C_p/ρ₀/G/K у стали 4340: ASSUMED_BY_SUBSTITUTION в два колена. Основание для заимствования — состав: подсказка каталога говорит «ρ = 7850 MEASURED — состав тот же, что у катаной», а отдельного набора Джонсона–Кука для литья в печати нет. " + string.Format("ПОВРЕЖДАЕМОСТЬ: набор RHA с D1 и D2, умноженными на {0:F2} — ", CastDamageScale) + "отношение удлинений при разрыве 12.8–19.6 % (литьё) к 22.0 % (прокат), исследование №10 §B1, MEASURED ⇒ сам множитель DERIVED. ⚠ Тег строки ОСТАЁТСЯ ASSUMED_BY_SUBSTITUTION: измеренное отношение наложено на ПОДСТАВЛЕННЫЙ набор (повреждаемость 4340), слабейший тег правит строкой.";
		caveats[9] = new string[7]
		{
			"У18 (объявленное упрощение): отношение квазистатических удлинений при разрыве переносится на всю поверхность ε_f без изменения формы. Это ПЕРЕНОС, а не измерение: ε_f Джонсона–Кука зависит от триаксиальности, скорости деформации и температуры, а удлинение при разрыве снято в одноосном растяжении на квазистатике. Перенос признанный, но огрублённый.",
			"Литьё: множитель — BAND 0.58…0.89 (12.8/22.0 и 19.6/22.0), в строке стоит " + string.Format("середина {0:F2} (16.2/22.0 = 0.7364, округление объявлено). Поля под ", CastDamageScale) + "разброс в строке НЕТ и заводить его нельзя; развёртка по трём точкам band'а — приёмка NightS27Cast, и качественно разный исход на краях означает, что строка НЕ ГОТОВА.",
			"Литьё: микропористость 0.10–0.27 % НЕ МОДЕЛИРУЕТСЯ — пор в модели нет вовсе, а весь эффект литья сведён к одному множителю при ε_f. Второй механизм того же источника остался невыраженным, и это объявленный пробел, а не недосмотр.",
			"Литьё: у источника есть НАБЛЮДАЕМАЯ ПРОВЕРКА, и она названа им самим — литьё обязано отколоться РАНЬШЕ и БОЛЕЕ КРУПНЫМИ кусками, чем прокат, в одном и том же выстреле. Не отличается — перенос неверен, и это надо НАПИСАТЬ, а не подкрутить множитель. Прибор проверки — NightS27Cast, и он же обязан сначала доказать, что прокат в выбранной сцене вообще что-то отделяет.",
			"Литьё: spallPa = 0, как у RHA — своего измеренного пола растяжения у этих строк нет. Значит «откол» этой строки выражается ТОЛЬКО ветвью повреждаемости Джонсона–Кука, и множитель — единственное, что его двигает.",
			"Литьё: ρ₀ = 7830 — ОТ RHA/4340, а НЕ измеренные 7850 из подсказки каталога, и это решение, а не описка. Заимствование ρ₀/G/K/C₀/S — ПАКЕТ: у строки собственный зазор K против ρ₀C₀² (159 против 164.2 ГПа, 3.2 %), объявленный реестром как принадлежащий строке. Подставить 7850 в одиночку значило бы разогнать зазор до 3.4 % — за объявленные 3 % — и сдвинуть шаг по времени этой строки одной. Разница 0.26 % по плотности при этом меньше самого зазора.",
			"Литьё: константы ПАС (epsASB 0.50, dEpsFade 0.30) у литья и проката ОДНИ И ТЕ ЖЕ — по ветви полос сдвига эти два вещества неразличимы по построению. Если частицы снимаются полосой раньше, чем накапливают D ⩾ 1, наблюдаемая проверка окажется слепой, и красный её исход будет говорить об этом, а не о самом переносе."
		};
	}

	public static ContinuumProperties Get(ContinuumMaterialId id)
	{
		Build();
		return table[(int)id];
	}

	public static string SourceRef(ContinuumMaterialId id)
	{
		Build();
		return sources[(int)id];
	}

	public static string[] Caveats(ContinuumMaterialId id)
	{
		Build();
		return caveats[(int)id] ?? new string[0];
	}

	public static float AsbDeathStrain(in ContinuumProperties p)
	{
		return p.hasASB == 0 ? float.NaN : p.epsASB + p.dEpsFade;
	}

	public static bool AsbOnsetIsSubstituted(ContinuumMaterialId id)
	{
		return id == ContinuumMaterialId.WHA || id == ContinuumMaterialId.Ti6Al4V;
	}

	public static string AsbOnsetTag(ContinuumMaterialId id, in ContinuumProperties p)
	{
		if (p.hasASB == 0)
		{
			return "— (ПАС не моделируется)";
		}
		if (AsbOnsetIsSubstituted(id))
		{
			return "ASSUMED — ПОДСТАВЛЕНО от RHA, своего источника в реестре нет";
		}
		if (p.epsASB == DefaultEpsASB)
		{
			return "DERIVED — замеренное начало ПАС в RHA/HHA 0.4–0.6";
		}
		return "ASSUMED — источника в реестре нет";
	}

	public static string AsbSubstitutionWarning()
	{
		Build();
		StringBuilder names = new StringBuilder();
		foreach (ContinuumMaterialId id in Enum.GetValues(typeof(ContinuumMaterialId)))
		{
			if (table[(int)id].hasASB == 0 || !AsbOnsetIsSubstituted(id))
			{
				continue;
			}
			if (names.Length > 0)
			{
				names.Append(", ");
			}
			names.Append(DisplayName(id));
		}
		if (names.Length == 0)
		{
			return null;
		}
		return "⚠⚠ НЕОБЪЯВЛЕННАЯ ПОДСТАНОВКА В ЕДИНСТВЕННОМ ПАРАМЕТРЕ ЭРОЗИИ: epsASB = " + string.Format("{0:F2} замерено на СТАЛИ и подставлено веществам [{1}]. ", DefaultEpsASB, names) + "У титана полосы начинаются раньше стальных, у вольфрама позже — подстановка стирает именно ту разницу, которой различаются WHA и обеднённый уран. " + string.Format("dEpsFade = {0:F2} — FITTED_BY_US, измеренной опоры нет вовсе.", DefaultFadeWidth);
	}

	public static bool ErodeEpsPReachable(in ContinuumProperties p, float erodeEpsP)
	{
		return p.hasASB == 0 || AsbDeathStrain(in p) >= erodeEpsP;
	}

	public static string ErosionParameterAudit(float erodeEpsP)
	{
		Build();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("── §1.1 ПАРАМЕТРЫ ЭРОЗИИ ПО ВЕЩЕСТВАМ (12c) ──");
		sb.AppendLine("  ⚠ После 12b это ЕДИНСТВЕННЫЕ параметры эрозии решателя: из шести бит");
		sb.AppendLine("    ElementKill во всех фикстурах срабатывал ровно один — полоса сдвига.");
		sb.AppendLine("  вещество          epsASB  тег/источник                    dEpsFade  тег/источник               hasASB  смерть ε_p  ε_erode");
		bool anyUnreachable = false;
		foreach (ContinuumMaterialId id in Enum.GetValues(typeof(ContinuumMaterialId)))
		{
			ContinuumProperties p = table[(int)id];
			string onsetTag = AsbOnsetTag(id, in p);
			string fadeTag;
			if (p.hasASB == 0)
			{
				fadeTag = "— (ПАС не моделируется)";
			}
			else if (p.dEpsFade == DefaultFadeWidth)
			{
				fadeTag = "FITTED_BY_US — измеренной опоры НЕТ, ОБЩЕЕ УМОЛЧАНИЕ";
			}
			else
			{
				fadeTag = "ASSUMED — источника в реестре нет";
			}
			float death = AsbDeathStrain(in p);
			bool reachable = ErodeEpsPReachable(in p, erodeEpsP);
			if (!reachable)
			{
				anyUnreachable = true;
			}
			string a = string.Format("  {0,-18} {1,5:F2}  {2,-58} {3,5:F2}  ", DisplayName(id), p.epsASB, onsetTag, p.dEpsFade);
			string b = string.Format("{0,-52} {1,3}  ", fadeTag, p.hasASB);
			string c = string.Format("{0,8}  ", float.IsNaN(death) ? "—" : death.ToString("F2"));
			string d = string.Format("{0:F2} {1}", erodeEpsP, reachable ? "ДОСТИЖИМ" : "НЕДОСТИЖИМ");
			sb.AppendLine(a + b + c + d);
		}
		sb.AppendLine();
		sb.AppendLine("── §1.2 ОБЩИЕ ИЛИ ПОМАТЕРИАЛЬНЫЕ ──");
		float eps = -1f;
		float fade = -1f;
		bool common = true;
		foreach (ContinuumMaterialId id in Enum.GetValues(typeof(ContinuumMaterialId)))
		{
			ContinuumProperties p = table[(int)id];
			if (p.hasASB == 0)
			{
				continue;
			}
			if (eps > 0f && (p.epsASB != eps || p.dEpsFade != fade))
			{
				common = false;
			}
			eps = p.epsASB;
			fade = p.dEpsFade;
		}
		if (!common)
		{
			sb.AppendLine("  поматериальные: значения различаются между веществами.");
		}
		else
		{
			sb.AppendLine(string.Format("  ⚠️⚠️ ОБЩИЕ. Все вещества с ПАС несут epsASB = {0:F2} и dEpsFade = {1:F2} — ", eps, fade) + "ОДНИ И ТЕ ЖЕ ЧИСЛА.");
			sb.AppendLine("  ⚠️⚠️ СЛЕДСТВИЕ ДЛЯ ИТЕРАЦИИ 13 (самозатачивание, DU против WHA), И ОНО");
			sb.AppendLine("    ОТРИЦАТЕЛЬНОЕ: два вещества с ПАС РАЗЛИЧИТЬ ПО ЭТОЙ ВЕТВИ НЕЛЬЗЯ —");
			sb.AppendLine(string.Format("    оба снимаются на ε_p = {0:F2}, до деформации ε = 1, на которой", eps + fade));
			sb.AppendLine("    предполагалось мерить термическое разупрочнение. Тест мерил бы");
			sb.AppendLine("    ВЫБРАННУЮ НАМИ КОНСТАНТУ, а не физику Джонсона–Кука.");
			sb.AppendLine("  ⚠️⚠️ И ОТДЕЛЬНО: СТРОКА УРАНА В РЕЕСТРЕ ЕСТЬ С S31 (DepletedUranium), НО ПАС У НЕЁ");
			sb.AppendLine("    ВЫКЛЮЧЕНА (порога ε_ASB для U-0.75Ti нет) — итерации 13 по-прежнему не из чего");
			sb.AppendLine("    строить вторую сторону опыта.");
		}
		sb.AppendLine();
		sb.AppendLine("── §1.3 ε_erode ──");
		if (!anyUnreachable)
		{
			sb.AppendLine(string.Format("  ε_erode = {0:F2} достижим у всех веществ реестра.", erodeEpsP));
		}
		else
		{
			sb.AppendLine(string.Format("  ⛔ ε_erode = {0:F2} НЕДОСТИЖИМ у веществ с ПАС: прочность обнуляется", erodeEpsP));
			sb.AppendLine(string.Format("     на epsASB + dEpsFade = {0:F2}, и элемент снимается там.", eps + fade));
			sb.AppendLine("  ⚠️ ПАРАМЕТР НЕ УДАЛЁН, И ЭТО ЗАМЕР, А НЕ СНИСХОЖДЕНИЕ: у веществ БЕЗ ПАС");
			sb.AppendLine("     (алюминий, медь) он ДОСТИЖИМ и является ЕДИНСТВЕННЫМ путём эрозии по");
			sb.AppendLine("     деформации. Удалить его значило бы оставить их вовсе без критерия —");
			sb.AppendLine("     то есть он мёртв не вообще, а на пяти строках реестра из семи.");
			sb.AppendLine("  ⚠️ ПОНИЖАТЬ ε_erode РАДИ ДОСТИЖИМОСТИ ЗАПРЕЩЕНО: это правка физики под");
			sb.AppendLine("     видом уборки, и место ей в развёртке, а не в этом коммите.");
		}
		sb.AppendLine();
		return sb.ToString();
	}

	public static string DisplayName(ContinuumMaterialId id)
	{
		switch (id)
		{
		case ContinuumMaterialId.WHA:
			return "WHA 91W-7Ni-2Fe";
		case ContinuumMaterialId.Steel4340:
			return "сталь 4340";
		case ContinuumMaterialId.RHA_MeyerKleponis:
			return "RHA (набор A, Meyer & Kleponis)";
		case ContinuumMaterialId.RHA_Grey:
			return "RHA (набор B, Grey et al.)";
		case ContinuumMaterialId.Al5083H116:
			return "Al 5083-H116";
		case ContinuumMaterialId.Ti6Al4V:
			return "Ti-6Al-4V";
		case ContinuumMaterialId.OFHC_Copper:
			return "медь OFHC (только проверки)";
		case ContinuumMaterialId.CastArmourSteel:
			return "литая броневая сталь";
		case ContinuumMaterialId.Neoprene_Interlayer:
			return "неопрен (прослойка НДЗ)";
		case ContinuumMaterialId.DepletedUranium:
			return "обеднённый уран DU-0.75Ti";
		case ContinuumMaterialId.CrewTissue:
			return "экипаж (условная ткань)";
		default:
			return id.ToString();
		}
	}

	public static List<string> NonMeasuredReport(params ContinuumMaterialId[] inUse)
	{
		Build();
		List<string> result = new List<string>();
		foreach (ContinuumMaterialId id in inUse)
		{
			result.Add(string.Format("{0}: {1} — {2}", DisplayName(id), table[(int)id].provenance, SourceRef(id)));
			string[] rows = Caveats(id);
			foreach (string row in rows)
			{
				result.Add("    ⚠ " + row);
			}
		}
		return result;
	}

	public static float Pressure(in ContinuumProperties m, float rho, float e)
	{
		return PressureFromMu(in m, rho / m.rho0 - 1f, e);
	}

	public static float PressureFromMu(in ContinuumProperties m, float mu, float e)
	{
		if (mu < 0f)
		{
			return m.rho0 * m.Gamma0 * e + m.K1 * mu;
		}
		return m.rho0 * (m.Gamma0 + m.a_eos * mu) * e + m.K1 * mu + m.K2 * mu * mu + m.K3 * mu * mu * mu;
	}

	public static float PlanarImpactPressure(in ContinuumProperties proj, in ContinuumProperties targ, float v0)
	{
		if (v0 <= 0f)
		{
			return 0f;
		}
		float a = proj.rho0 * proj.S - targ.rho0 * targ.S;
		float b = 2f * proj.rho0 * proj.S * v0 + proj.rho0 * proj.C0 + targ.rho0 * targ.C0;
		float c = proj.rho0 * proj.S * v0 * v0 + proj.rho0 * proj.C0 * v0;
		float u;
		if (Mathf.Abs(a) < Mathf.Abs(b) * 1E-06f)
		{
			u = c / b;
		}
		else
		{
			float discriminant = b * b - 4f * a * c;
			if (discriminant < 0f)
			{
				return 0f;
			}
			float root = Mathf.Sqrt(discriminant);
			float u1 = (b + root) / (2f * a);
			float u2 = (b - root) / (2f * a);
			u = u1 > 0f && u1 < v0 ? u1 : u2;
		}
		if (u <= 0f || u >= v0)
		{
			return 0f;
		}
		return targ.rho0 * (targ.C0 + targ.S * u) * u;
	}

	public static float FreeSurfaceVelocity(in ContinuumProperties m, float pressurePa)
	{
		if (pressurePa <= 0f)
		{
			return 0f;
		}
		float a = m.rho0 * m.S;
		float b = m.rho0 * m.C0;
		float u;
		if (a > 1E-12f)
		{
			u = (-b + Mathf.Sqrt(b * b + 4f * a * pressurePa)) / (2f * a);
		}
		else
		{
			u = pressurePa / Mathf.Max(b, 1E-12f);
		}
		return 2f * u;
	}

	public static float FlowStress(in ContinuumProperties m, float epsP, float epsDotP, float T)
	{
		float eps = Mathf.Min(epsP, m.epsSat);
		float hardening = m.JC_A + m.JC_B * Mathf.Pow(eps, m.JC_n);
		float rate = 1f + m.JC_C * Mathf.Log(Mathf.Max(1f, epsDotP / m.epsDot0));
		float tStar = Mathf.Clamp01((T - m.T_room) / (m.T_melt - m.T_room));
		float thermal = 1f - Mathf.Pow(tStar, m.JC_m);
		return Mathf.Max(0f, hardening * rate * thermal);
	}

	public static float AsbFade(in ContinuumProperties m, float epsP)
	{
		if (m.hasASB == 0 || epsP <= m.epsASB)
		{
			return 1f;
		}
		if (m.dEpsFade <= 0f)
		{
			return 0f;
		}
		return Mathf.Clamp01(1f - (epsP - m.epsASB) / m.dEpsFade);
	}

	public static float FailureStrain(in ContinuumProperties m, float P, float sigEq, float epsDotP, float T)
	{
		float triax = sigEq > 1f ? -P / sigEq : 0f;
		triax = Mathf.Clamp(triax, -1.5f, 1.5f);
		float rate = Mathf.Max(1f, epsDotP / m.epsDot0);
		float tStar = Mathf.Clamp01((T - m.T_room) / (m.T_melt - m.T_room));
		float result = (m.JC_D1 + m.JC_D2 * Mathf.Exp(m.JC_D3 * triax)) * (1f + m.JC_D4 * Mathf.Log(rate)) * (1f + m.JC_D5 * tStar);
		return Mathf.Max(0.001f, result);
	}

	static ContinuumMaterials()
	{
		ActiveRhaSet = ContinuumMaterialId.RHA_MeyerKleponis;
	}
}
