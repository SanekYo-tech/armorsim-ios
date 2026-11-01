using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public static class NeraCatalog
{
	private static readonly NeraPreset[] presets;

	public static IList<NeraPreset> All { get; }

	public static void Apply(NeraPreset p, ArmorPlate plate)
	{
		if (plate == null)
		{
			return;
		}
		plate.neraExtentMM = p.extentMM;
		plate.SetNeraFrontThickness(p.frontMM);
		plate.SetNeraRearThickness(p.rearMM);
		plate.SetNeraInterlayer(p.interlayerMM);
		plate.SetNeraMountAngle(p.mountAngleDeg);
		plate.SetNeraFrontMaterial(p.frontMaterial);
		plate.SetNeraRearMaterial(p.rearMaterial);
		plate.presetId = p.id;
		plate.neraInterlayer = p.interlayer;
		plate.neraBonding = p.bonding;
	}

	public static bool TryGet(string id, out NeraPreset p)
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
		if (plate == null || !plate.IsNeraPlate)
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

	private static bool MatchesNumbers(ArmorPlate plate, NeraPreset e)
	{
		return Same(plate.neraFrontThicknessMM, e.frontMM) && Same(plate.neraRearThicknessMM, e.rearMM) && Same(plate.neraInterlayerMM, e.interlayerMM) && Same(plate.neraExtentMM, e.extentMM) && Same(plate.neraMountAngleDeg, e.mountAngleDeg) && plate.neraFrontMaterial == e.frontMaterial && plate.neraRearMaterial == e.rearMaterial && plate.neraInterlayer == e.interlayer && plate.neraBonding == e.bonding;
	}

	private static bool Same(float a, float b)
	{
		return Mathf.Abs(a - b) < 0.001f;
	}

	public static string Tooltip(NeraPreset p)
	{
		float frontDensity = NeraArmor.PlateDensity(p.frontMaterial);
		float rearDensity = NeraArmor.PlateDensity(p.rearMaterial);
		float frontM = p.frontMM / 1000f;
		float rearM = p.rearMM / 1000f;
		float frontV = frontDensity > 0f && frontM > 0f ? 4710f / (frontM * frontDensity) : 0f;
		float rearV = rearDensity > 0f && rearM > 0f ? 4710f / (rearM * rearDensity) : 0f;
		float ratio = rearV > 0f ? frontV / rearV : 0f;
		string text = string.Format("Пакет {0:F1} / {1:F0} / {2:F1} мм ", p.frontMM, p.interlayerMM, p.rearMM) + "(" + ArmorPlate.DisplayName(p.frontMaterial) + " — " + InterlayerShort(p.interlayer) + " — " + string.Format("{0}), W = {1:F0} мм. ", ArmorPlate.DisplayName(p.rearMaterial), p.extentMM);
		string speeds = string.Format("⭐ СКОРОСТИ ВЫВЕДЕНЫ из одного импульса J = {0:F0} Н·с/м² (V = J/(ρ·t)): ", 4710f) + string.Format("передняя {0:F0} м/с, задняя {1:F0} м/с, отношение ×{2:F3}. ", frontV, rearV, ratio);
		string range;
		if (frontV >= 100f && frontV <= 250f && rearV >= 100f && rearV <= 250f)
		{
			range = string.Format("Обе внутри качественной полосы {0:F0}–{1:F0} м/с. ", 100f, 250f);
		}
		else
		{
			range = string.Format("⚠ ВНЕ качественной полосы {0:F0}–{1:F0} м/с — ", 100f, 250f) + "не отказ: полоса взята из качественного «сотни метров в секунду», а импульсная рентгенография Хельда даёт 200–500 м/с (MEASURED). ";
		}
		return text + speeds + range + "ГЕОМЕТРИЯ: " + p.provenance + " " + NeraArmor.InterlayerNote(p.interlayer);
	}

	private static string InterlayerShort(InterlayerMaterial m)
	{
		if (m == InterlayerMaterial.Neoprene)
		{
			return "неопрен";
		}
		return m == InterlayerMaterial.NaturalRubber ? "резина" : "полиуретан";
	}

	public static void Validate(IList<NeraPreset> rows, List<string> problems)
	{
		foreach (NeraPreset e in rows)
		{
			if (string.IsNullOrEmpty(e.id))
			{
				problems.Add("пресет НДЗ: пустой ключ");
				continue;
			}
			if (string.IsNullOrEmpty(e.displayName))
			{
				problems.Add("пресет НДЗ '" + e.id + "': пустая подпись");
			}
			if (string.IsNullOrEmpty(e.provenance))
			{
				problems.Add("пресет НДЗ '" + e.id + "': не указан провенанс геометрии");
			}
			InRange(e.id, "передняя пластина", e.frontMM, 1f, 12f, problems);
			InRange(e.id, "задняя пластина", e.rearMM, 1f, 12f, problems);
			InRange(e.id, "прослойка", e.interlayerMM, 1f, 40f, problems);
			InRange(e.id, "наклон пакета", e.mountAngleDeg, 0f, 60f, problems);
			InRange(e.id, "ширина W", e.extentMM, 20f, 500f, problems);
			if (!NeraArmor.IsAllowedPlateMaterial(e.frontMaterial))
			{
				problems.Add(string.Format("пресет НДЗ '{0}': материал передней пластины {1} не разрешён", e.id, e.frontMaterial));
			}
			if (!NeraArmor.IsAllowedPlateMaterial(e.rearMaterial))
			{
				problems.Add(string.Format("пресет НДЗ '{0}': материал задней пластины {1} не разрешён", e.id, e.rearMaterial));
			}
			if (!Enum.IsDefined(typeof(InterlayerMaterial), e.interlayer))
			{
				problems.Add(string.Format("пресет НДЗ '{0}': {1} не является значением InterlayerMaterial", e.id, (int)e.interlayer));
			}
			if (!Enum.IsDefined(typeof(NeraBonding), e.bonding))
			{
				problems.Add(string.Format("пресет НДЗ '{0}': {1} не является значением NeraBonding", e.id, (int)e.bonding));
			}
		}
		for (int i = 0; i < rows.Count; i++)
		{
			for (int j = i + 1; j < rows.Count; j++)
			{
				if (rows[i].id == rows[j].id)
				{
					problems.Add("каталог НДЗ: ключ '" + rows[i].id + "' повторяется");
				}
			}
		}
	}

	public static void Validate(List<string> problems)
	{
		Validate(All, problems);
	}

	private static void InRange(string id, string what, float v, float lo, float hi, List<string> problems)
	{
		if (v >= lo && v <= hi)
		{
			return;
		}
		problems.Add(string.Format("пресет НДЗ '{0}': {1} = {2:F1} вне ручки [{3:F0}; {4:F0}] — ", id, what, v, lo, hi) + "объект получит клампованное значение и разойдётся с пресетом");
	}

	private static NeraPreset P(string id, string name, float frontMM, float rearMM, float interlayerMM, InterlayerMaterial interlayer, string provenance)
	{
		return new NeraPreset
		{
			id = id,
			displayName = name,
			category = "Выпучивающаяся броня",
			frontMM = frontMM,
			rearMM = rearMM,
			interlayerMM = interlayerMM,
			frontMaterial = ArmorPlate.ArmorType.RHA,
			rearMaterial = ArmorPlate.ArmorType.RHA,
			interlayer = interlayer,
			extentMM = 250f,
			mountAngleDeg = 0f,
			provenance = provenance
		};
	}

	private static NeraPreset P2(string id, string name, float frontMM, float rearMM, float interlayerMM, InterlayerMaterial interlayer, NeraBonding bonding, float extentMM, float mountAngleDeg, string provenance)
	{
		NeraPreset p = P(id, name, frontMM, rearMM, interlayerMM, interlayer, provenance);
		p.bonding = bonding;
		p.extentMM = extentMM;
		p.mountAngleDeg = mountAngleDeg;
		return p;
	}

	static NeraCatalog()
	{
		presets = new NeraPreset[]
		{
			P("nera_held", "Пакет Хельда 2/20/4", 2f, 20f, 4f, InterlayerMaterial.Polyurethane, "MEASURED — тестовый пакет импульсной рентгенографии Хельда (иссл. №10 §A2), единственная полностью описанная сборка в отчёте. ⚠ «мягкая сталь» лицевой пластины записана строкой RHA: ρ совпадает, отдельной строки мягкой стали в таблице метаемых материалов нет и выдумывать её нельзя."),
			P("nera_thin", "Тонкий пакет 3/6/5", 3f, 6f, 5f, InterlayerMaterial.Polyurethane, "ASSUMED — текущее умолчание проекта, на котором откалиброван импульс эластомера (4710 Н·с/м² ⇒ 200/120 м/с, обе внутри качественной полосы 100–250). Измеренной сборкой НЕ является: это опорная точка модели, а не отстрел."),
			P("nera_neoprene", "Неопреновая прослойка 3/6/5", 3f, 6f, 5f, InterlayerMaterial.Neoprene, "Геометрия — та же ASSUMED, что у тонкого пакета. Строка существует РАДИ МАТЕРИАЛА. ⚠ В ОБЫЧНОМ РЕЖИМЕ ЧИСЛА ВЫСТРЕЛА ОТ ЭТОГО НЕ ЗАВИСЯТ (импульса по материалам нет); В SUPER прослойка считается своей строкой реестра — см. подсказку."),
			P2("nera_isl_5", "ISL/Fras 4/5/4 (не связан)", 4f, 4f, 5f, InterlayerMaterial.NaturalRubber, NeraBonding.Unbonded, 40f, 60f, "MEASURED (бриф 24, пакет ISL/Fras): пластины 4/4 мм RHA 380 HB (альтернативная сборка источника — 3/3 мм Domex Protect 300), натуральная резина 70 Shore A, прослойка 5 мм, купон 40×200 мм, установка 60° NATO, пластины НЕ СВЯЗАНЫ. ⚠ Резина записана материалом NaturalRubber → строка полиуретана (ASSUMED_BY_SUBSTITUTION: собственной адиабаты натуральной резины бриф не дал)."),
			P2("nera_isl_10", "ISL/Fras 4/10/4 (не связан)", 4f, 4f, 10f, InterlayerMaterial.NaturalRubber, NeraBonding.Unbonded, 40f, 60f, "MEASURED (бриф 24, пакет ISL/Fras): то же, прослойка 10 мм — середина серии 5/10/15."),
			P2("nera_isl_15", "ISL/Fras 4/15/4 (не связан)", 4f, 4f, 15f, InterlayerMaterial.NaturalRubber, NeraBonding.Unbonded, 40f, 60f, "MEASURED (бриф 24, пакет ISL/Fras): то же, прослойка 15 мм — верх серии 5/10/15."),
			P2("nera_kfrrca", "KFRRCA 3/5/3 (связан)", 3f, 3f, 5f, InterlayerMaterial.NaturalRubber, NeraBonding.Bonded, 150f, 60f, "MEASURED (бриф 24, пакет KFRRCA): 3/3 мм стали Q235 (записана RHA — огрубление Хельда), 5 мм кевлар-армированной резины (15 % волокна), купон 300×150 мм, СВЯЗАН; испытан на 30/45/60/68° (68° ручкой пакета невыразим — потолок 60°). Это ИСПЫТАННАЯ сборка; оптимум источника — отдельной строкой nera_kfrrca_opt."),
			P2("nera_kfrrca_opt", "KFRRCA 3/3/3 (оптимум источника)", 3f, 3f, 3f, InterlayerMaterial.NaturalRubber, NeraBonding.Bonded, 150f, 60f, "Геометрия пластин MEASURED (та же, что у nera_kfrrca), прослойка 3.0 мм — ЗАЯВЛЕННЫЙ ИСТОЧНИКОМ ОПТИМУМ против кумулятивной струи (полоса 3.0–3.5 мм, взят нижний край). ⚠ Это НЕ отстрелянная сборка, а оптимум ИЗ СТАТЬИ — испытанная есть отдельной строкой nera_kfrrca.")
		};
		All = new ReadOnlyCollection<NeraPreset>(presets);
	}
}
