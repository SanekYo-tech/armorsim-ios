using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public static class RoundCatalog
{
	public const string US = "США (M829)";

	public const string DE = "Германия (DM)";

	public const string RU = "СССР/Россия (3BM)";

	public const string AC = "Автопушки 25–30 мм";

	public const float RangeVelocityDropMps = 120f;

	private const float SemiInfiniteMM = 4000f;

	private static readonly RoundSpec[] rounds;

	private const string AntiEra = "⚠ ПРОТИВО-ДЗ НАКОНЕЧНИК НЕ МОДЕЛИРУЕТСЯ: выстрел отработает как обычный моноблок. Механизма «сорвать кассету раньше основного стержня» в проекте нет, а стержневая ветвь ДЗ даёт эффект только выше ~60° обликвитета. Это ровно та черта, ради которой боеприпас и создавался.";

	private const string Composite = "⚠ СОСТАВНОЙ СЕРДЕЧНИК СЧИТАЕТСЯ МОНОБЛОКОМ доминирующего материала: у ядра Тейта одна пара (ρ_p, Y_p) на стержень. Вся масса приписана одному материалу, поэтому и геометрия, и глубина смещены — насколько, неизвестно.";

	public const float MassClosureTolFrac = 0.001f;

	public const float MinDiameterOverCalibre = 0.15f;

	public const float MaxDiameterOverCalibre = 0.35f;

	public const float MinLengthOverCalibre = 2f;

	public const float MaxLengthOverCalibre = 8f;

	private static readonly Dictionary<string, RoundComparison> cache;

	public static int ValidateCalls;

	public static int LastValidatedRowCount;

	public static IList<RoundSpec> All { get; }

	private static RoundSpec[] WithPassports(RoundSpec[] literal)
	{
		List<RoundSpec> result = new List<RoundSpec>(literal);
		foreach (RodPassport passport in RodPassports.Admitted)
		{
			result.Add(RodPassports.ToRoundSpec(passport));
		}
		return result.ToArray();
	}

	public static float DiameterM(RoundSpec r)
	{
		if (r.geometrySource == RoundGeometrySource.Dimensions && r.coreDiameterMM.present)
		{
			return r.coreDiameterMM.value * 0.001f;
		}
		float density = CorePenetrator(r.core).density;
		float ld = Math.Max(r.lengthOverDiameter, 0.001f);
		float denominator = (float)Math.PI * density * ld;
		if (denominator <= 0f)
		{
			return 0f;
		}
		return (float)Math.Pow(Math.Max(r.penetratorMassKg, 0f) * 4f / denominator, 1f / 3f);
	}

	public static float LengthM(RoundSpec r)
	{
		if (r.geometrySource == RoundGeometrySource.Dimensions && r.coreLengthMM.present)
		{
			return r.coreLengthMM.value * 0.001f;
		}
		return DiameterM(r) * r.lengthOverDiameter;
	}

	public static float MassFromGeometryKg(RoundSpec r)
	{
		float density = CorePenetrator(r.core).density;
		float d = DiameterM(r);
		return LengthM(r) * density * (float)Math.PI * 0.25f * d * d;
	}

	public static float MassMismatchFrac(RoundSpec r)
	{
		if (string.IsNullOrEmpty(r.passportId))
		{
			float published = Math.Max(r.penetratorMassKg, 0f);
			if (published <= 0f)
			{
				return 0f;
			}
			return (MassFromGeometryKg(r) - published) / published;
		}
		if (!RodPassports.TryFind(r.passportId, out RodPassport passport))
		{
			return float.NaN;
		}
		PassportVerdict verdict = RodPassports.Check(passport);
		return verdict.admitted ? 0f : verdict.mismatchFrac;
	}

	public static float MassToleranceFrac(RoundSpec r)
	{
		return r.geometrySource == RoundGeometrySource.Dimensions ? r.massToleranceFrac : MassClosureTolFrac;
	}

	public static string MassCheckNote(RoundSpec r)
	{
		if (r.geometrySource != RoundGeometrySource.Dimensions || !string.IsNullOrEmpty(r.passportId))
		{
			return null;
		}
		return string.Format("⚠ ГЕОМЕТРИЯ ИЗ РАЗМЕРОВ: масса ВЫВЕДЕНА ({0:F3} кг) против ", MassFromGeometryKg(r)) + string.Format("опубликованной ({0:F3} кг), расхождение {1:+0.00;-0.00;0.00} % ", r.penetratorMassKg, MassMismatchFrac(r) * 100f) + string.Format("при допуске {0:F2} %.", MassToleranceFrac(r) * 100f);
	}

	public static string PlausibilityNote(RoundSpec r)
	{
		if (r.calibreMM <= 0f)
		{
			return r.id + ": калибр не задан";
		}
		float diameter = DiameterM(r) * 1000f / r.calibreMM;
		float length = LengthM(r) * 1000f / r.calibreMM;
		if (diameter >= MinDiameterOverCalibre && diameter <= MaxDiameterOverCalibre && length >= MinLengthOverCalibre && length <= MaxLengthOverCalibre)
		{
			return null;
		}
		if (length < MinLengthOverCalibre || length > MaxLengthOverCalibre)
		{
			return string.Format("{0}: выведенная длина {1:F0} мм — это {2:F2} калибра ", r.id, LengthM(r) * 1000f, length) + string.Format("(правдоподобно {0:F1}–{1:F1}).", MinLengthOverCalibre, MaxLengthOverCalibre);
		}
		return string.Format("{0}: выведенный ⌀{1:F1} мм — это {2:F2} калибра ", r.id, DiameterM(r) * 1000f, diameter) + string.Format("(правдоподобно {0:F2}–{1:F2}). ", MinDiameterOverCalibre, MaxDiameterOverCalibre) + "Скорее всего, неверно L/D или масса в источнике.";
	}

	public static float SemiInfiniteRhaMM(TateModel.Penetrator p, float lengthM, float velocityMps)
	{
		return PenetrationCore.Calculate(p, TateModel.RHA(), velocityMps, 0f, SemiInfiniteMM, lengthM, lengthM, false).penetrationDepthMM;
	}

	public static RoundComparison Compare(RoundSpec r)
	{
		if (r.id != null && cache.TryGetValue(r.id, out RoundComparison cached))
		{
			return cached;
		}
		TateModel.Penetrator penetrator = CorePenetrator(r.core);
		float length = LengthM(r);
		float computed = SemiInfiniteRhaMM(penetrator, length, r.muzzleVelocityMps);
		float range = SemiInfiniteRhaMM(penetrator, length, Math.Max(1f, r.muzzleVelocityMps - RangeVelocityDropMps));
		RoundComparison result = new RoundComparison
		{
			diameterMM = DiameterM(r) * 1000f,
			lengthMM = length * 1000f,
			computedRhaMM = computed,
			claimedRhaMM = r.claimedRhaMM,
			atRangeRhaMM = range,
			hasClaim = r.claimedRhaMM > 0f
		};
		result.deltaPercent = result.hasClaim ? (computed - result.claimedRhaMM) / result.claimedRhaMM * 100f : 0f;
		if (r.id != null)
		{
			cache[r.id] = result;
		}
		return result;
	}

	public static ProjectileDefinition MakeDefinition(RoundSpec r)
	{
		return new ProjectileDefinition
		{
			name = r.displayName,
			kind = ProjectileClass.LongRod,
			coreType = r.core,
			rodLengthMM = LengthM(r) * 1000f,
			diameterMM = DiameterM(r) * 1000f,
			velocityMps = r.impactVelocityMps.present ? r.impactVelocityMps.value : r.muzzleVelocityMps,
			nose = r.nose.present ? r.nose.value : NoseShape.Ogive,
			coreDiameterFrac = r.coreDiameterFrac.present ? r.coreDiameterFrac.value : 0f,
			capLengthFrac = r.capLengthFrac.present ? r.capLengthFrac.value : 0f,
			coreMaterialId = r.coreMaterialId.present ? r.coreMaterialId.value : "",
			capMaterialId = r.capMaterialId.present ? r.capMaterialId.value : "",
			cavityFrac = r.cavityFrac.present ? r.cavityFrac.value : 0f,
			fuzeDelayS = r.fuzeDelayS.present ? r.fuzeDelayS.value : 0f,
			fragmentVelocityMps = r.fragmentVelocityMps.present ? r.fragmentVelocityMps.value : 0f,
			passportId = string.IsNullOrEmpty(r.passportId) ? "" : r.passportId,
			segments = 12,
			strands = 7,
			finBlades = (r.year >= 1990 ? 6 : 5),
			gunCalibreMM = r.calibreMM
		};
	}

	public static string Tooltip(RoundSpec r)
	{
		RoundComparison c = Compare(r);
		string head = r.geometrySource == RoundGeometrySource.Dimensions ? HeadFromDimensions(r, c) : string.Format("{0}, {1} г., калибр {2:F0} мм. ", r.displayName, r.year, r.calibreMM) + string.Format("{0}: m = {1:F2} кг, L/D = {2:F0} ", CorePenetrator(r.core).name, r.penetratorMassKg, r.lengthOverDiameter) + string.Format("⇒ ⌀{0:F1} × {1:F0} мм (ВЫВЕДЕНО), v₀ = {2:F0} м/с.", c.diameterMM, c.lengthMM, r.muzzleVelocityMps);
		string claim = c.hasClaim ? string.Format(" Расчёт модели: {0:F0} мм | Заявлено: {1:F0} мм (UNVERIFIED_CLAIM) | ", c.computedRhaMM, c.claimedRhaMM) + string.Format("Расхождение: {0:+0;-0;0} %.", c.deltaPercent) : string.Format(" Расчёт модели: {0:F0} мм; заявки нет.", c.computedRhaMM);
		string range = c.hasClaim ? " ⚠ заявка относится к 2000 м, расчёт — по ДУЛЬНОЙ скорости (наша цифра завышена; " + string.Format("на −{0:F0} м/с та же модель даёт {1:F0} мм).", RangeVelocityDropMps, c.atRangeRhaMM) : "";
		string notes = string.IsNullOrEmpty(r.limitation) ? "" : " " + r.limitation;
		string plausibility = PlausibilityNote(r);
		if (plausibility != null && !string.IsNullOrEmpty(r.id))
		{
			notes += " ⚠ ДАННЫЕ: " + plausibility;
		}
		string mass = MassCheckNote(r);
		if (mass != null)
		{
			notes += " " + mass;
		}
		return head + claim + range + notes;
	}

	private static string HeadFromDimensions(RoundSpec r, RoundComparison c)
	{
		string year = r.year > 0 ? string.Format(", {0} г.", r.year) : "";
		if (!string.IsNullOrEmpty(r.passportId))
		{
			string published = string.Format("m опубл. = {0:F3} кг", r.penetratorMassKg);
			return string.Format("{0}{1}, калибр {2:F0} мм. ", r.displayName, year, r.calibreMM) + string.Format("{0}: ⌀{1:F2} × {2:F0} мм (ПАСПОРТ), ", CorePenetrator(r.core).name, c.diameterMM, c.lengthMM) + string.Format("L/D = {0:F1}, {1} ", c.lengthMM / c.diameterMM, published) + string.Format("(масса по размерам ВЫВОДИТСЯ и сверяется — см. ниже), v₀ = {0:F0} м/с.", r.muzzleVelocityMps);
		}
		return string.Format("{0}{1}, калибр {2:F0} мм. ", r.displayName, year, r.calibreMM) + string.Format("{0}: ⌀{1:F2} × {2:F0} мм, ", CorePenetrator(r.core).name, c.diameterMM, c.lengthMM) + string.Format("m = {0:F1} г, v₀ = {1:F0} м/с.", r.penetratorMassKg * 1000f, r.muzzleVelocityMps);
	}

	public static TateModel.Penetrator CorePenetrator(Projectile.CoreType t)
	{
		switch (t)
		{
		case Projectile.CoreType.DU:
			return TateModel.DU();
		case Projectile.CoreType.Steel:
			return TateModel.Steel();
		case Projectile.CoreType.WC:
			return TateModel.WC();
		default:
			return TateModel.WHA();
		}
	}

	public static bool TryGet(string id, out RoundSpec spec)
	{
		foreach (RoundSpec row in All)
		{
			if (row.id == id)
			{
				spec = row;
				return true;
			}
		}
		spec = default;
		return false;
	}

	public static void Validate(IList<RoundSpec> rows, List<string> problems)
	{
		ValidateCalls++;
		LastValidatedRowCount = rows.Count;
		HashSet<string> ids = new HashSet<string>();
		Dictionary<string, string> names = new Dictionary<string, string>();
		foreach (RoundSpec r in rows)
		{
			if (string.IsNullOrEmpty(r.id)) problems.Add("выстрел каталога: пустой ключ");
			if (string.IsNullOrEmpty(r.displayName)) problems.Add("выстрел '" + r.id + "': пустая подпись");
			if (string.IsNullOrEmpty(r.category)) problems.Add("выстрел '" + r.id + "': пустая категория");
			if (r.calibreMM <= 0f) problems.Add(string.Format("выстрел '{0}': калибр {1} мм", r.id, r.calibreMM));
			if (r.penetratorMassKg <= 0f) problems.Add(string.Format("выстрел '{0}': масса сердечника {1} кг", r.id, r.penetratorMassKg));
			if (r.muzzleVelocityMps <= 0f) problems.Add(string.Format("выстрел '{0}': дульная скорость {1} м/с", r.id, r.muzzleVelocityMps));
			if (r.lengthOverDiameter <= 0f) problems.Add(string.Format("выстрел '{0}': L/D = {1}", r.id, r.lengthOverDiameter));
			Prov(r.id, "длина сердечника, мм", r.coreLengthMM.present, r.coreLengthMM.provenance, problems);
			Prov(r.id, "диаметр сердечника, мм", r.coreDiameterMM.present, r.coreDiameterMM.provenance, problems);
			Prov(r.id, "доля калибра под сердечником", r.coreDiameterFrac.present, r.coreDiameterFrac.provenance, problems);
			Prov(r.id, "доля длины под колпаком", r.capLengthFrac.present, r.capLengthFrac.provenance, problems);
			Prov(r.id, "доля объёма под полостью", r.cavityFrac.present, r.cavityFrac.provenance, problems);
			Prov(r.id, "скорость встречи, м/с", r.impactVelocityMps.present, r.impactVelocityMps.provenance, problems);
			Prov(r.id, "задержка взрывателя, с", r.fuzeDelayS.present, r.fuzeDelayS.provenance, problems);
			Prov(r.id, "скорость осколков, м/с", r.fragmentVelocityMps.present, r.fragmentVelocityMps.provenance, problems);
			Prov(r.id, "вещество сердечника", r.coreMaterialId.present, r.coreMaterialId.provenance, problems);
			Prov(r.id, "вещество колпака", r.capMaterialId.present, r.capMaterialId.provenance, problems);
			Prov(r.id, "форма головной части", r.nose.present, r.nose.provenance, problems);
			if (r.coreMaterialId.present && string.IsNullOrEmpty(r.coreMaterialId.value)) problems.Add("выстрел '" + r.id + "': вещество сердечника объявлено, а имя строки реестра пусто");
			if (r.capMaterialId.present && string.IsNullOrEmpty(r.capMaterialId.value)) problems.Add("выстрел '" + r.id + "': вещество колпака объявлено, а имя строки реестра пусто");
			if (r.coreDiameterFrac.present && (r.coreDiameterFrac.value <= 0f || r.coreDiameterFrac.value > 1f)) problems.Add(string.Format("выстрел '{0}': доля калибра под сердечником {1} вне (0; 1]", r.id, r.coreDiameterFrac.value));
			if (r.capLengthFrac.present && (r.capLengthFrac.value <= 0f || r.capLengthFrac.value >= 1f)) problems.Add(string.Format("выстрел '{0}': доля длины под колпаком {1} вне (0; 1)", r.id, r.capLengthFrac.value));
			if (r.cavityFrac.present && (r.cavityFrac.value < 0f || r.cavityFrac.value >= 1f)) problems.Add(string.Format("выстрел '{0}': доля объёма под полостью {1} вне [0; 1)", r.id, r.cavityFrac.value));
			if (r.impactVelocityMps.present && r.impactVelocityMps.value <= 0f) problems.Add(string.Format("выстрел '{0}': скорость встречи {1} м/с", r.id, r.impactVelocityMps.value));
			if (r.fuzeDelayS.present && r.fuzeDelayS.value < 0f) problems.Add(string.Format("выстрел '{0}': задержка взрывателя {1} с — отрицательная", r.id, r.fuzeDelayS.value));
			if (r.fragmentVelocityMps.present && r.fragmentVelocityMps.value < 0f) problems.Add(string.Format("выстрел '{0}': скорость осколков {1} м/с", r.id, r.fragmentVelocityMps.value));
			if (!string.IsNullOrEmpty(r.passportId))
			{
				if (!RodPassports.TryFind(r.passportId, out RodPassport pass))
				{
					problems.Add("выстрел '" + r.id + "': паспорт «" + r.passportId + "» в RodPassports не найден");
				}
				else if (!RodPassports.Check(pass).admitted)
				{
					problems.Add("выстрел '" + r.id + "': паспорт отклонён сверкой (" + r.passportId + ") — строкой каталога быть не может");
				}
			}
			if (r.geometrySource == RoundGeometrySource.Mass)
			{
				if (r.massToleranceFrac != 0f) problems.Add(string.Format("выстрел '{0}': допуск сверки масс {1} объявлен при ", r.id, r.massToleranceFrac) + "источнике МАССА — там сверка замкнута по построению, и допуск мёртв");
				if (r.coreDiameterMM.present || r.coreLengthMM.present) problems.Add("выстрел '" + r.id + "': источником объявлена МАССА, но строка несёт " + (r.coreDiameterMM.present ? "диаметр" : "длину") + " сердечника — у строки два независимых источника геометрии, чего этот файл не допускает");
			}
			else
			{
				if (r.massToleranceFrac == 0f) problems.Add("выстрел '" + r.id + "': источником объявлены РАЗМЕРЫ, а допуск сверки масс не объявлен — сверять было бы не с чем");
				if (!r.coreDiameterMM.present || !r.coreLengthMM.present) problems.Add(string.Format("выстрел '{0}': размеры ⌀{1} × {2} мм", r.id, r.coreDiameterMM.value, r.coreLengthMM.value) + "': источником объявлены РАЗМЕРЫ, а задан " + (r.coreDiameterMM.present ? "только диаметр" : r.coreLengthMM.present ? "только длина" : "ни один из них"));
				float mismatch = MassMismatchFrac(r);
				if (float.IsNaN(mismatch) || Math.Abs(mismatch) > r.massToleranceFrac) problems.Add(string.Format("выстрел '{0}': выведенная из размеров масса {1:F3} кг ", r.id, MassFromGeometryKg(r)) + string.Format("против опубликованной {0:F3} кг — расхождение ", r.penetratorMassKg) + string.Format("{0:+0.00;-0.00;0.00} % при объявленном допуске ", mismatch * 100f) + string.Format("{0:F2} %. Строка не принимается.", r.massToleranceFrac * 100f));
			}
			if (!ids.Add(r.id)) problems.Add("каталог выстрелов: ключ '" + r.id + "' повторяется");
			if (!string.IsNullOrEmpty(r.displayName))
			{
				if (names.TryGetValue(r.displayName, out string other)) problems.Add("каталог выстрелов: подпись '" + r.displayName + "' у строк '" + other + "' и '" + r.id + "' — опознание идёт по подписи, строки подменят друг друга");
				else names.Add(r.displayName, r.id);
			}
		}
	}

	public static void Validate(List<string> problems)
	{
		Validate(All, problems);
	}

	private static void Prov(string id, string field, bool present, string provenance, List<string> problems)
	{
		if (present && string.IsNullOrEmpty(provenance))
		{
			problems.Add("выстрел '" + id + "': поле паспорта «" + field + "» задано БЕЗ ПРОВЕНАНСА — молча подставленное число");
		}
	}

	public static RoundNumber Num(float value, string provenance)
	{
		return new RoundNumber { present = true, value = value, provenance = provenance };
	}

	public static RoundName Name(string value, string provenance)
	{
		return new RoundName { present = true, value = value, provenance = provenance };
	}

	public static RoundNose Nose(NoseShape value, string provenance)
	{
		return new RoundNose { present = true, value = value, provenance = provenance };
	}

	private static RoundSpec R(string id, string name, string category, int year, float calibreMM, float massKg, float velocity, float ld, Projectile.CoreType core, float claimMM, string limitation = "")
	{
		return new RoundSpec
		{
			id = id,
			displayName = name,
			category = category,
			year = year,
			calibreMM = calibreMM,
			penetratorMassKg = massKg,
			muzzleVelocityMps = velocity,
			lengthOverDiameter = ld,
			core = core,
			claimedRhaMM = claimMM,
			limitation = limitation,
			geometrySource = RoundGeometrySource.Mass
		};
	}

	public static RoundSpec RD(string id, string name, string category, int year, float calibreMM, float publishedMassKg, float velocity, RoundNumber coreDiameterMM, RoundNumber coreLengthMM, float massToleranceFrac, Projectile.CoreType core, float claimMM, string limitation = "")
	{
		return new RoundSpec
		{
			id = id,
			displayName = name,
			category = category,
			year = year,
			calibreMM = calibreMM,
			penetratorMassKg = publishedMassKg,
			muzzleVelocityMps = velocity,
			lengthOverDiameter = coreDiameterMM.value > 0f ? coreLengthMM.value / coreDiameterMM.value : 0f,
			core = core,
			claimedRhaMM = claimMM,
			limitation = limitation,
			geometrySource = RoundGeometrySource.Dimensions,
			massToleranceFrac = massToleranceFrac,
			coreDiameterMM = coreDiameterMM,
			coreLengthMM = coreLengthMM
		};
	}

	static RoundCatalog()
	{
		rounds = new RoundSpec[16]
		{
			R("m774", "M774", US, 1979, 105f, 3.4000001f, 1509f, 13f, Projectile.CoreType.Steel, 375f),
			R("m833", "M833", US, 1983, 105f, 3.70000005f, 1494f, 18f, Projectile.CoreType.Steel, 420f),
			R("m829", "M829", US, 1985, 120f, 4.5999999f, 1670f, 20f, Projectile.CoreType.DU, 540f),
			R("m829a1", "M829A1", US, 1988, 120f, 4.5999999f, 1575f, 24f, Projectile.CoreType.DU, 650f),
			R("m829a2", "M829A2", US, 1993, 120f, 4.5999999f, 1680f, 27f, Projectile.CoreType.DU, 700f),
			R("m829a3", "M829A3", US, 2002, 120f, 10f, 1555f, 30f, Projectile.CoreType.DU, 750f, AntiEra),
			R("dm13", "DM13", DE, 1979, 120f, 3.5999999f, 1650f, 12f, Projectile.CoreType.WHA, 350f),
			R("dm23", "DM23", DE, 1983, 120f, 3.79999995f, 1650f, 16f, Projectile.CoreType.WHA, 420f),
			R("dm33", "DM33", DE, 1987, 120f, 4.30000019f, 1650f, 20f, Projectile.CoreType.WHA, 470f),
			R("dm43", "DM43", DE, 1992, 120f, 4.80000019f, 1750f, 25f, Projectile.CoreType.WHA, 550f),
			R("dm53", "DM53", DE, 1999, 120f, 5f, 1750f, 34f, Projectile.CoreType.WHA, 600f, AntiEra),
			R("3bm22", "3BM22", RU, 1976, 125f, 4.48999977f, 1760f, 15f, Projectile.CoreType.Steel, 350f, Composite),
			R("3bm32", "3BM32", RU, 1985, 125f, 4.8499999f, 1700f, 19f, Projectile.CoreType.DU, 500f),
			R("3bm42", "3BM42", RU, 1986, 125f, 4.8499999f, 1700f, 20f, Projectile.CoreType.WHA, 470f, Composite),
			R("3bm46", "3BM46", RU, 1991, 125f, 4.8499999f, 1700f, 22f, Projectile.CoreType.DU, 620f),
			R("3bm60", "3BM60", RU, 2005, 125f, 5.4000001f, 1750f, 26f, Projectile.CoreType.WHA, 650f)
		};
		All = new ReadOnlyCollection<RoundSpec>(WithPassports(rounds));
		cache = new Dictionary<string, RoundComparison>();
	}
}
