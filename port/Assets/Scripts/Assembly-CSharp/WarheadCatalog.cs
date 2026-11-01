using System.Collections.Generic;
using System.Collections.ObjectModel;

public static class WarheadCatalog
{
	public const float TandemDelayS = 0.0004f;

	public const float FuzeStandoffCD = 2f;

	private static readonly WarheadSpec[] warheads;

	public const string Atgm = "ПТУР и гранатомёты";

	public static IList<WarheadSpec> All { get; }

	private static float Standoff(float coneMM)
	{
		return coneMM * 2f;
	}

	public static ProjectileDefinition MakeDefinition(WarheadSpec w)
	{
		ProjectileDefinition projectileDefinition = new ProjectileDefinition();
		projectileDefinition.name = w.displayName;
		projectileDefinition.rodLengthMM = w.bodyLengthMM;
		projectileDefinition.diameterMM = w.bodyDiameterMM;
		projectileDefinition.velocityMps = w.velocityMps;
		projectileDefinition.coneDiameterMM = w.coneDiameterMM;
		projectileDefinition.fuzeStandoffMM = w.fuzeStandoffMM;
		projectileDefinition.isTandem = w.isTandem;
		projectileDefinition.precursorConeDiameterMM = w.precursorConeDiameterMM;
		projectileDefinition.kind = ProjectileClass.ShapedCharge;
		projectileDefinition.coreType = Projectile.CoreType.Steel;
		projectileDefinition.precursorInNoseProbe = w.precursorInNoseProbe;
		projectileDefinition.interChargeDelayS = TandemDelayS;
		projectileDefinition.nose = NoseShape.Ogive;
		projectileDefinition.segments = 10;
		projectileDefinition.strands = 7;
		projectileDefinition.noFins = true;
		projectileDefinition.bodyModelResource = w.bodyModelResource;
		projectileDefinition.roofBodyModelResource = w.roofBodyModelResource;
		projectileDefinition.roofBodyLengthMM = w.roofBodyLengthMM;
		return projectileDefinition;
	}

	public static string Tooltip(WarheadSpec w)
	{
		string[] array = new string[6]
		{
			w.displayName,
			(w.year < 1) ? "" : string.Format(", {0} г.", w.year),
			". ",
			string.Format("Воронка ⌀{0:F0} мм, взрывательный стандоф {1:F0} мм ", w.coneDiameterMM, w.fuzeStandoffMM),
			string.Format("(= {0:F1} CD), скорость {1:F0} м/с. ", 2f, w.velocityMps),
			(!w.isTandem)
				? "Моноблок. "
				: (string.Format("ТАНДЕМ: предзаряд ⌀{0:F0} мм снимает кассету ДЗ, ", w.precursorConeDiameterMM) + "основной заряд идёт по уже инертной. Предзаряд НЕ считается отдельным пробитием. " + ((!w.precursorInNoseProbe)
					? "Предзаряд сидит в теле, заряды идут кучно. "
					: ("Предзаряд вынесен в ЗОНД и срабатывает ПРИ КАСАНИИ: его струя рождается " + string.Format("в {0:F0} мм от брони, основная — в ", w.precursorConeDiameterMM) + string.Format("{0:F0} мм, то есть заметно дальше. ", w.fuzeStandoffMM))))
		};
		string text = string.Concat(array);
		string text2 = (w.claimedRhaMM <= 0f)
			? "Заявки о пробитии в найденных источниках нет. "
			: ((!w.claimIsBehindEra)
				? (string.Format("⚠ ЗАЯВЛЕНО: {0:F0} мм RHA (UNVERIFIED_CLAIM — без условий отстрела: ", w.claimedRhaMM) + "какая мишень, какой угол, кто мерил). В расчёт НЕ входит. ")
				: (string.Format("⚠ ЗАЯВЛЕНО: {0:F0} мм ПОСЛЕ ДЗ (UNVERIFIED_CLAIM). Это НЕ пробитие ", w.claimedRhaMM) + "однородной брони: величина описывает связку «боеприпас + чья-то кассета», и сравнивать её с расчётом по плите нельзя. Что пробьёт ЭТОТ выстрел по однородной плите — решает расчёт, а не заявка."));
		string text3 = string.IsNullOrEmpty(w.bodyModelResource)
			? "◈ Своей 3D-модели у выстрела нет — до подрыва рисуется куб-заглушка по габариту. "
			: ("◈ Модель: Resources/" + w.bodyModelResource + ". " + (string.IsNullOrEmpty(w.roofBodyModelResource)
				? ""
				: ("В разделе «Крыша» — Resources/" + w.roofBodyModelResource + ": другой носитель того же заряда. Меняется ТОЛЬКО показ, ни одно число строки не зависит от раздела. ")));
		string text4 = string.IsNullOrEmpty(w.limitation) ? "" : (w.limitation + " ");
		string[] array2 = new string[6] { text, text2, text3, text4, "ПРОВЕНАНС: ", w.provenance };
		return string.Concat(array2);
	}

	public static bool TryGet(string id, out WarheadSpec w)
	{
		for (int i = 0; i < warheads.Length; i++)
		{
			if (warheads[i].id == id)
			{
				w = warheads[i];
				return true;
			}
		}
		w = default(WarheadSpec);
		return false;
	}

	public static void Validate(IList<WarheadSpec> rows, List<string> problems)
	{
		foreach (WarheadSpec w in rows)
		{
			if (string.IsNullOrEmpty(w.id))
			{
				problems.Add("кумулятивный выстрел: пустой ключ");
				continue;
			}
			if (string.IsNullOrEmpty(w.displayName))
			{
				problems.Add("выстрел '" + w.id + "': пустая подпись");
			}
			if (string.IsNullOrEmpty(w.provenance))
			{
				problems.Add("выстрел '" + w.id + "': не указан провенанс чисел");
			}
			if (w.coneDiameterMM <= 0f)
			{
				problems.Add(string.Format("выстрел '{0}': воронка {1} мм — струи не будет вовсе", w.id, w.coneDiameterMM));
			}
			if (w.velocityMps <= 0f)
			{
				problems.Add(string.Format("выстрел '{0}': скорость {1} м/с", w.id, w.velocityMps));
			}
			if (w.bodyLengthMM <= 0f || w.bodyDiameterMM <= 0f)
			{
				problems.Add(string.Format("выстрел '{0}': габарит тела {1}×{2} мм", w.id, w.bodyLengthMM, w.bodyDiameterMM));
			}
			if (w.fuzeStandoffMM <= 0f)
			{
				problems.Add(string.Format("выстрел '{0}': стандоф {1} мм", w.id, w.fuzeStandoffMM));
			}
			if (!string.IsNullOrEmpty(w.roofBodyModelResource) && string.IsNullOrEmpty(w.bodyModelResource))
			{
				problems.Add("выстрел '" + w.id + "': модель для крыши задана, а основной нет — по корпусу останется куб-заглушка");
			}
			if (0f < w.roofBodyLengthMM && string.IsNullOrEmpty(w.roofBodyModelResource))
			{
				problems.Add("выстрел '" + w.id + "': длина показа для крыши задана, а модель крыши — нет; число исказило бы ту же модель именно на крыше");
			}
			if (w.isTandem)
			{
				if (w.precursorConeDiameterMM <= 0f)
				{
					problems.Add("выстрел '" + w.id + "': тандем без предзаряда");
				}
				if (w.coneDiameterMM <= w.precursorConeDiameterMM)
				{
					problems.Add(string.Format("выстрел '{0}': предзаряд ⌀{1} не меньше ", w.id, w.precursorConeDiameterMM) + string.Format("основного ⌀{0} — это не тандем", w.coneDiameterMM));
				}
			}
			else
			{
				if (0f < w.precursorConeDiameterMM)
				{
					problems.Add("выстрел '" + w.id + "': предзаряд задан, но isTandem = false — поле мёртвое");
				}
				if (w.precursorInNoseProbe)
				{
					problems.Add("выстрел '" + w.id + "': выносной зонд задан, но isTandem = false — подрывать в зонде нечего, поле мёртвое");
				}
			}
			if (w.precursorInNoseProbe && w.fuzeStandoffMM <= w.precursorConeDiameterMM)
			{
				problems.Add(string.Format("выстрел '{0}': зонд — предзаряд ⌀{1} мм ", w.id, w.precursorConeDiameterMM) + string.Format("не меньше взрывательного стандофа {0:F0} мм: ", w.fuzeStandoffMM) + "предзаряд оказался бы ПОЗАДИ основного заряда");
			}
		}
		for (int i = 0; i < rows.Count; i++)
		{
			for (int j = i + 1; j < rows.Count; j++)
			{
				if (rows[i].id == rows[j].id)
				{
					problems.Add("каталог кумулятивных: ключ '" + rows[i].id + "' повторяется");
				}
			}
		}
	}

	public static void Validate(List<string> problems)
	{
		Validate(All, problems);
	}

	private static WarheadSpec W(string id, string name, string category, int year, float coneMM, float velocity, float bodyLen, float bodyDia, string model, float claimMM, bool behindEra, string provenance, bool tandem = false, float precursorMM = 0f, string limitation = "", string roofModel = "", float roofBodyLen = 0f, bool noseProbe = false)
	{
		return new WarheadSpec
		{
			id = id,
			displayName = name,
			category = category,
			year = year,
			coneDiameterMM = coneMM,
			fuzeStandoffMM = Standoff(coneMM),
			velocityMps = velocity,
			bodyLengthMM = bodyLen,
			bodyDiameterMM = bodyDia,
			isTandem = tandem,
			precursorConeDiameterMM = precursorMM,
			precursorInNoseProbe = noseProbe,
			claimedRhaMM = claimMM,
			claimIsBehindEra = behindEra,
			bodyModelResource = model,
			roofBodyModelResource = roofModel,
			roofBodyLengthMM = roofBodyLen,
			provenance = provenance,
			limitation = limitation
		};
	}

	static WarheadCatalog()
	{
		warheads = new WarheadSpec[6]
		{
			W("3bk9", "3БК9 (115 мм)", "Кумулятивные", 1962, 100f, 900f, 700f, 115f, "Models/HeatRound", 460f, behindEra: false, "Калибр орудия 115 мм и дульная скорость 900 м/с — PUBLISHED. ⚠ ВОРОНКА 100 мм — ESTIMATED: паспортного диаметра облицовки в открытых источниках НЕ НАЙДЕНО, принято 0.87 калибра — типовая доля для калиберного снаряда, у которого воронка сидит внутри корпуса со стенкой. Это ЕДИНСТВЕННОЕ число строки, входящее в расчёт, и оно оценочное — расхождение с заявкой читать в первую очередь через него. ⚠ Габарит тела (700×115 мм) — ESTIMATED, на расчёт не влияет: это растяжка модели.", tandem: false, precursorMM: 0f, limitation: "", roofModel: "", roofBodyLen: 0f, noseProbe: false),
			W("pg7v", "РПГ-7 (ПГ-7В)", "ПТУР и гранатомёты", 1961, 85f, 300f, 708f, 85f, "Models/RGP-7ed", 260f, behindEra: false, "Надкалиберная БЧ ⌀85 мм, длина выстрела 925 мм, маршевая скорость ≈300 м/с — PUBLISHED. ⚠ Заявка 260 мм — UNVERIFIED_CLAIM без условий отстрела. ⚠ Модель корпуса — авторская (Resources/Models/RGP-7ed) и в расчёт НЕ ВХОДИТ: габарит показа задают bodyLen×bodyDia, глубину — струя. bodyLen = 708 мм — это СОБСТВЕННЫЕ пропорции модели (L/D 8.33 при ⌀85), а не паспортные 925 мм: натяжка до 925 растягивала меш на +30 % (см. код выше). Длина тела вне расчёта.", tandem: false, precursorMM: 0f, limitation: "⭐ ЭТА БЧ — ТИПОВАЯ УГРОЗА ПО КРЫШЕ ПРИ СБРОСЕ С ДРОНА. В разделе «Крыша» её и стоит брать для углового подлёта: скорость подхода при сбросе задаётся падением, а не маршевым двигателем, и от паспортных 300 м/с отличается в разы. Модель этой разницы НЕ ЗНАЕТ: скорость носителя в пробитие струи не входит вовсе — глубину задаёт струя. Практически это значит, что сброшенная и выстреленная БЧ дадут ОДИН И ТОТ ЖЕ ответ, и различать их сегодня нечем. ◈ РАЗЛИЧАЕТСЯ ТОЛЬКО ПОКАЗ: по крыше рисуется вариант сброса с дрона (без пусковой трубы и маршевой ступени), по корпусу — выстрел из гранатомёта. Это смена МОДЕЛИ, а не второй боеприпас: ни одно число строки от раздела не зависит.", roofModel: "Models/RGP-7eDRONEd", roofBodyLen: 0f, noseProbe: false),
			W("pg7vr", "РПГ-7 (ПГ-7ВР)", "ПТУР и гранатомёты", 1988, 105f, 120f, 874f, 105f, "Models/RGP-7ed", 600f, behindEra: true, "Тандем: предзаряд ⌀64 мм + основной ⌀105 мм, длина выстрела 1120 мм, маршевая скорость ≈120 м/с — PUBLISHED. ⚠ Заявка ≈600 мм дана ПОСЛЕ ДЗ — это свойство связки «боеприпас + чья-то кассета», а не брони; сравнивать её с нашим расчётом по однородной плите нельзя. ⚠ Задержка между зарядами — PLACEHOLDER проекта. ⚠ Модель корпуса ОБЩАЯ с ПГ-7В (Resources/Models/RGP-7ed): выстрел тот же, отличается боевая часть, и своей модели у неё нет — тандемной головы на экране НЕ ВИДНО. bodyLen = 874 мм — пропорции модели (L/D 8.33 при ⌀105), не паспортные 1120 мм: натяжка до 1120 растягивала меш на +28 %. Длина тела вне расчёта.", tandem: true, precursorMM: 64f, limitation: "◈ ПО КРЫШЕ РИСУЕТСЯ ВАРИАНТ СБРОСА С ДРОНА — то же правило, что у ПГ-7В: меняется показ, а не расчёт.", roofModel: "Models/RGP-7eDRONEd", roofBodyLen: 0f, noseProbe: false),
			W("tow2a", "TOW-2A (BGM-71E)", "ПТУР и гранатомёты", 1987, 127f, 188f, 1534f, 152f, "Models/Tow-2A", 900f, behindEra: true, "Диаметр ракеты 152 мм, длина 1170 мм со сложенным зондом, скорость полёта 188 м/с — PUBLISHED. Воронка основного заряда 127 мм — PUBLISHED (вторичный источник). ⚠ Предзаряд 40 мм — ESTIMATED: он сидит в выносном зонде, паспортного калибра НЕ НАЙДЕНО. ⚠ Заявка 900 мм дана ПОСЛЕ ДЗ, и сам источник рядом отмечает, что рассекреченная оценка ЦРУ по вертикальной мишени существенно НИЖЕ заявленной. Число хранится как есть и в расчёт не входит. ⚠ Модель корпуса — авторская (Resources/Models/Tow-2A) и в расчёт НЕ ВХОДИТ. bodyLen = 1534 мм — СОБСТВЕННЫЕ пропорции модели с разложенным зондом (L/D 10.09 при ⌀152), а не паспортные 1170 мм со сложенным: длина тела вне расчёта, а ужимать модель на 24 % незачем. ⚠ ДЛИНА ЗОНДА НЕ ОТДЕЛЬНОЕ ЧИСЛО: перед броней боеприпас встаёт по своему взрывательному стандофу (2.0 CD = 254 мм), и зонд на модели ему примерно и равен (≈280 мм по замеру файла) — совпадение приятное, но паспортом не является.", tandem: true, precursorMM: 40f, limitation: "⭐ ПРЕДЗАРЯД В ВЫНОСНОМ ЗОНДЕ, И НА ЭКРАНЕ ЭТО ВИДНО КАК ДВЕ РАЗНЕСЁННЫЕ СТРУИ. Зонд касается брони — там и подрывается предзаряд (40 мм от преграды, 1.0 своего CD: слабая короткая струя, её дело снять кассету ДЗ, а не пробить). Основной заряд остаётся на своём стандофе 254 мм — его струя рождается ЗАМЕТНО ДАЛЬШЕ от брони и успевает вытянуться. ⚠ Расстояние между зарядами при этом НЕ измерено: и стандоф основного (2.0 CD), и секция зонда перед воронкой предзаряда (1.0 его CD) — ASSUMED.", roofModel: "", roofBodyLen: 0f, noseProbe: true),
			W("pars3", "PARS 3 LR", "ПТУР и гранатомёты", 2012, 150f, 290f, 1600f, 152f, "Models/PARS_3_LR", 1000f, behindEra: true, "⚠⚠ САМАЯ СЛАБАЯ СТРОКА ТАБЛИЦЫ ПО ДАННЫМ. Заявка «1000+ мм после ДЗ» — PUBLISHED, и это ПРАКТИЧЕСКИ ВСЁ, что нашлось: ни диаметра ракеты, ни длины, ни калибра воронки в найденных источниках НЕТ (NOT FOUND). Воронка 150 мм и габарит 1600×152 мм — ESTIMATED по классу боеприпаса; предзаряд 45 мм — ESTIMATED. Держать строку в таблице стоит ровно потому, что тандем ~150 мм — это ВЕРХНЯЯ граница того, что вообще прилетает в крышу, и увидеть её полезно; принимать её числа за паспорт — нельзя. ⚠ Модель корпуса — авторская (Resources/Models/PARS_3_LR), в расчёт НЕ ВХОДИТ. Габарит 1600×152 оставлен прежним НАМЕРЕННО: собственное L/D модели 10.05 против требуемого 10.53 — растяжка +4.7 %, ниже порога, за которым показ вообще об этом говорит. Менять оценочное число ради 4.7 % значило бы двигать паспорт под меш.", tandem: true, precursorMM: 45f, limitation: "⚠ ПРЕДЗАРЯД СЧИТАЕТСЯ СИДЯЩИМ В ТЕЛЕ, А НЕ В ВЫНОСНОМ ЗОНДЕ (в отличие от TOW-2A). Это НЕ находка, а ОТСУТСТВИЕ находки: компоновки тандема этой ракеты в найденных источниках нет, и приписывать ей зонд по аналогии мы не станем. Практически это значит, что оба её заряда подрываются кучно: предзаряд в 255 мм от брони, основной в 300 мм (стандоф 2.0 CD минус секция предзаряда в один его калибр).", roofModel: "", roofBodyLen: 0f, noseProbe: false),
			W("javelin", "Javelin (FGM-148)", "ПТУР и гранатомёты", 1996, 127f, 290f, 1100f, 127f, "Models/javelineNEitled", 760f, behindEra: false, "Калибр ракеты 127 мм и заявка «свыше 760 мм RHA» — PUBLISHED. Воронка основного заряда принята равной калибру ракеты — ESTIMATED (собственного числа НЕ НАЙДЕНО; в реальности облицовка уже корпуса на толщину стенки, то есть наша оценка ЗАВЫШЕНА, и направление ошибки известно). ⚠ Предзаряд 40 мм — ESTIMATED. ⚠ Скорость 290 м/с — ESTIMATED по времени полёта и дальности. ⚠ Модель корпуса — авторская (Resources/Models/javelineNEitled), в расчёт НЕ ВХОДИТ.", tandem: true, precursorMM: 40f, limitation: "⭐ БОЕПРИПАС ВЕРХНЕЙ АТАКИ: в разделе «Крыша» ему соответствует УГЛОВОЙ ПОДЛЁТ, а не горизонтальный пролёт ПТУР. ⚠ Сама траектория пикирования моделью не считается — угол задаётся игроком (см. RoofShot); паспортного угла пикирования в открытых источниках нет, и приписывать его боеприпасу мы не станем.", roofModel: "", roofBodyLen: 0f, noseProbe: false)
		};
		All = new ReadOnlyCollection<WarheadSpec>(warheads);
	}
}
