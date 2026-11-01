using System.Collections.Generic;

public static class Flat2DCatalogue
{
	public struct Row
	{
		public string id;

		public string display;

		public string category;

		public string tooltip;

		public bool available;

		public string reason;

		public ContinuumMaterialId mat;

		public bool isMaterial;

		public string round;

		public string nameKey;

		public bool passport;

		public CatalogEntry ToEntry()
		{
			return new CatalogEntry
			{
				id = id,
				displayName = Shown(this),
				category = Flat2DText.Tr(category),
				tooltip = available ? tooltip : Flat2DText.Tr(reason)
			};
		}
	}

	public const string WhyStage6 = "этап 6 — взрывчатка не реализована";

	public const string WhyJH2 = "нужна модель JH-2 (хрупкое разрушение керамики)";

	public const string WhyOrtho = "нужна ортотропная модель (композит не изотропен)";

	public const string WhyNoJC = "нет блока Джонсона–Кука вовсе — прочности нет";

	public const string WhyNoFrac = "нет блока разрушения";

	public const string WhyNoRate = "нет параметров разрушения и скоростной чувствительности";

	public const string WhyTestOnly = "только для проверок решателя — в песочнице не выбирается";

	public const string WhyNotAccepted = "строка в реестре есть, приёмка не пройдена — исход немонотонен по диапазону деформации разрушения";

	public const string WhyNoJet = "нет модели струи — этап 6";

	public const string WhyOtherMode = "строка в реестре есть (S31, для Super), в «Плоскости» приёмки не было; блока разрушения D1–D5 у урана нет";

	public static string Shown(Row r)
	{
		if (Flat2DText.ShowsRussian)
		{
			return r.display;
		}
		if (!string.IsNullOrEmpty(r.nameKey) && Localization.HasKey(r.nameKey))
		{
			return Localization.T(r.nameKey);
		}
		return Flat2DText.Tr(r.display);
	}

	public static string ShownRound(Flat2DRounds.Entry e)
	{
		if (Flat2DText.ShowsRussian)
		{
			return e.display;
		}
		if (!string.IsNullOrEmpty(e.nameKey) && Localization.HasKey(e.nameKey))
		{
			return Localization.T(e.nameKey);
		}
		return Flat2DText.Tr(e.display);
	}

	public static string TagOf(Row r)
	{
		return r.passport ? "ПАСПОРТ ОТКЛОНЁН" : Tag(r.reason);
	}

	public static List<Row> Projectiles()
	{
		List<Row> rows = new List<Row>();
		void Add(string id, string name, string cat, bool ok, string why, ContinuumMaterialId m, string tip = null)
		{
			rows.Add(new Row
			{
				id = id,
				display = name,
				category = cat,
				tooltip = Flat2DText.Tr(tip),
				available = ok,
				reason = why,
				mat = m
			});
		}
		Add("rod_wha", "стержень WHA", "НАСТРАИВАЕМЫЕ", true, null, ContinuumMaterialId.WHA, "91W-7Ni-2Fe: рисуется прямоугольником, скорость задаётся полем");
		Add("rod_du", "стержень DU", "НАСТРАИВАЕМЫЕ", false, WhyOtherMode, ContinuumMaterialId.DepletedUranium);
		Add("rod_ap", "бронебойный", "НАСТРАИВАЕМЫЕ", true, null, ContinuumMaterialId.Steel4340, "сталь 4340 — опорный отладочный материал проекта");
		Add("rod_hard", "твёрдый сердечник", "НАСТРАИВАЕМЫЕ", true, null, ContinuumMaterialId.HardCoreSteel_N12e, "N12e, >60 HRC (бриф 22)");
		foreach (Flat2DRounds.Entry e in Flat2DRounds.All)
		{
			rows.Add(new Row
			{
				id = e.id,
				display = e.display,
				category = e.category,
				tooltip = e.tooltip,
				available = e.available,
				reason = e.reason,
				mat = e.mat,
				isMaterial = false,
				round = e.id,
				nameKey = e.nameKey,
				passport = e.passport
			});
		}
		string[] heat = new string[] { "ПГ-7В", "ПГ-7ВР", "TOW-2A", "PARS 3 LR", "Javelin" };
		for (int i = 0; i < heat.Length; i++)
		{
			Add("heat_" + heat[i], heat[i], "КУМУЛЯТИВНЫЕ", false, WhyNoJet, ContinuumMaterialId.WHA);
		}
		return rows;
	}

	public static List<Row> Materials()
	{
		List<Row> rows = new List<Row>();
		void Reg(string cat, ContinuumMaterialId id, string forced = null)
		{
			ContinuumProperties p = ContinuumMaterials.Get(id);
			if (forced == null && p.JC_A <= 0f)
			{
				forced = WhyNoJC;
			}
			else if (forced == null && (p.C0 <= 0f || p.S <= 0f))
			{
				forced = "нет ударной адиабаты (C0/S)";
			}
			rows.Add(new Row
			{
				id = "m_" + id,
				display = ContinuumMaterials.DisplayName(id),
				category = cat,
				tooltip = Flat2DText.F("плотность {0:F0} кг/м3, C0 {1:F0} м/с, J-C A {2:F0} МПа", new object[] { p.rho0, p.C0, p.JC_A / 1000000.0 }),
				available = forced == null,
				reason = forced,
				mat = id,
				isMaterial = true
			});
		}
		void Missing(string cat, string name, string why)
		{
			rows.Add(new Row
			{
				id = "x_" + name,
				display = name,
				category = cat,
				available = false,
				reason = why,
				isMaterial = true
			});
		}
		Reg("СТАЛИ", ContinuumMaterialId.RHA_MeyerKleponis);
		Reg("СТАЛИ", ContinuumMaterialId.RHA_Grey);
		Missing("СТАЛИ", "HHA / Armox 500", "нет параметров разрушения и скоростной чувствительности (в реестре нет)");
		Reg("СТАЛИ", ContinuumMaterialId.Steel4340);
		Missing("СТАЛИ", "мягкая сталь", "в реестре нет: набор J-C не добыт");
		Reg("СТАЛИ", ContinuumMaterialId.HardCoreSteel_N12e);
		Reg("ЛЁГКИЕ СПЛАВЫ", ContinuumMaterialId.Al5083H116);
		Missing("ЛЁГКИЕ СПЛАВЫ", "Al 7039-T64", "в реестре нет");
		Missing("ЛЁГКИЕ СПЛАВЫ", "Al 6061-T6", "в реестре нет");
		Reg("ЛЁГКИЕ СПЛАВЫ", ContinuumMaterialId.Ti6Al4V);
		Reg("ТЯЖЁЛЫЕ МЕТАЛЛЫ", ContinuumMaterialId.WHA);
		Reg("ТЯЖЁЛЫЕ МЕТАЛЛЫ", ContinuumMaterialId.DepletedUranium, WhyOtherMode);
		string[] ceramics = new string[] { "SiC", "B4C", "Al2O3" };
		for (int i = 0; i < ceramics.Length; i++)
		{
			Missing("КЕРАМИКА", ceramics[i], WhyJH2);
		}
		string[] composites = new string[] { "текстолит", "кевлар", "стеклопластик" };
		for (int i = 0; i < composites.Length; i++)
		{
			Missing("КОМПОЗИТЫ", composites[i], WhyOrtho);
		}
		Reg("ЭЛАСТОМЕРЫ", ContinuumMaterialId.Polyurethane_Interlayer);
		Reg("ЭЛАСТОМЕРЫ", ContinuumMaterialId.Neoprene_Interlayer);
		Reg("ЭКИПАЖ", ContinuumMaterialId.CrewTissue);
		Missing("ПРОЧЕЕ", "IRHA", "нет параметров разрушения и скоростной чувствительности (research_13)");
		Reg("ПРОЧЕЕ", ContinuumMaterialId.CastArmourSteel, WhyNotAccepted);
		Missing("ПРОЧЕЕ", "WC-Co", "нужна модель JH-2 (хрупкое разрушение керамики) (research_13)");
		Reg("ПРОЧЕЕ", ContinuumMaterialId.OFHC_Copper, WhyTestOnly);
		return rows;
	}

	public static List<Row> Reactive()
	{
		List<Row> rows = new List<Row>();
		string[] era = new string[] { "Контакт-1", "Контакт-5", "Реликт", "Кактус", "ERAWA-1", "ERAWA-2", "Blazer" };
		for (int i = 0; i < era.Length; i++)
		{
			rows.Add(new Row
			{
				id = "era_" + era[i],
				display = era[i],
				category = "ДИНАМИЧЕСКАЯ ЗАЩИТА",
				available = false,
				reason = WhyStage6
			});
		}
		string[] nera = new string[] { "НЕРА-пакет 3 слоя", "НЕРА-пакет 5 слоёв", "НЕРА с прослойкой" };
		for (int i = 0; i < nera.Length; i++)
		{
			rows.Add(new Row
			{
				id = "nera_" + nera[i],
				display = nera[i],
				category = "НЕРА",
				available = false,
				reason = "нужен контакт между слоями и метаемые пластины — этап 6"
			});
		}
		return rows;
	}

	public static string Tag(string reason)
	{
		if (string.IsNullOrEmpty(reason))
		{
			return "";
		}
		if (reason.Contains("приёмка не пройдена"))
		{
			return "ПРИЁМКА НЕ ПРОЙДЕНА";
		}
		if (reason.Contains("паспорт"))
		{
			return "ПАСПОРТ ОТКЛОНЁН";
		}
		if (reason.Contains("колпачок"))
		{
			return "ДВА ТЕЛА";
		}
		if (reason.Contains("этап 6"))
		{
			return "ЭТАП 6";
		}
		if (reason.Contains("JH-2"))
		{
			return "НУЖНА JH-2";
		}
		if (reason.Contains("ортотроп"))
		{
			return "ОРТОТРОПИЯ";
		}
		if (reason.Contains("Джонсона–Кука"))
		{
			return "НЕТ J-C";
		}
		if (reason.Contains("разрушения"))
		{
			return "НЕТ РАЗРУШЕНИЯ";
		}
		if (reason.Contains("адиабат"))
		{
			return "НЕТ АДИАБАТЫ";
		}
		if (reason.Contains("нет размеров"))
		{
			return "НЕТ РАЗМЕРОВ";
		}
		if (reason.Contains("только для проверок"))
		{
			return "ТОЛЬКО ТЕСТЫ";
		}
		if (reason.Contains("в реестре нет") || reason.Contains("не добыт") || reason.Contains("нет строки"))
		{
			return "НЕТ В РЕЕСТРЕ";
		}
		return "НЕДОСТУПНО";
	}

	public static List<Row> Refused()
	{
		List<Row> refused = new List<Row>();
		List<Row>[] groups = new List<Row>[] { Projectiles(), Materials(), Reactive() };
		for (int i = 0; i < groups.Length; i++)
		{
			foreach (Row row in groups[i])
			{
				if (!row.available)
				{
					refused.Add(row);
				}
			}
		}
		return refused;
	}

	public static string Summary()
	{
		List<Row>[] groups = new List<Row>[] { Projectiles(), Materials(), Reactive() };
		int total = 0;
		int refused = 0;
		for (int i = 0; i < groups.Length; i++)
		{
			foreach (Row row in groups[i])
			{
				total++;
				if (!row.available)
				{
					refused++;
				}
			}
		}
		return string.Format("каталог: {0} строк, доступно {1}, отказано с причиной {2}", total, total - refused, refused);
	}
}
