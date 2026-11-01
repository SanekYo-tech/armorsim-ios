using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public static class SuperLimits
{
	public struct Row
	{
		public string name;

		public string value;

		public string bounds;

		public string measured;

		public string ratio;

		public ulong hits;

		public bool counted;

		public bool modelTerm;

		public bool validity;

		public bool perParticle;

		public double share;

		public string note;
	}

	public const int Slots = 41;

	public const int SampleStride = 64;

	public const int ProfileBins = 1024;

	public static bool ProfileOn;

	public const string TriaxDeclaration = "У17: у этой доли частице-шагов триаксиальность вне диапазона калибровки модели повреждения; там σ* зажата к ±1.5 и её предсказание ε_f смысла не имеет";

	public const int JWhole = 3;

	public const int PCapWhole = 4;

	public const int JDebris = 5;

	public const int PCapDebris = 6;

	public const int DebrisTension = 7;

	public const int CohTension = 8;

	public const int SoftFloor = 9;

	public const int Triax = 10;

	public const int EfFloor = 11;

	public const int EpsSat = 12;

	public const int RateFloor = 13;

	public const int TStar = 14;

	public const int RhoFloor = 15;

	public const int SigEqSkip = 16;

	public const int SteelAboveAlphaEps = 17;

	public const int MaxSteelPressure = 18;

	public const int CoherentAboveAlphaEps = 19;

	public const int MaxPressure = 20;

	public const int MaxTension = 21;

	public const int MaxJHi = 22;

	public const int MaxJLoInv = 23;

	public const int MaxTriax = 24;

	public const int MaxEpsP = 25;

	public const int MaxRate = 26;

	public const int MaxQ = 27;

	public const int MaxQOverP = 28;

	public const int MaxDmg = 29;

	public const int OverPCap = 30;

	public const int OverCoh = 31;

	public const int OverDebris = 32;

	public const int OverJHi = 33;

	public const int OverJLoInv = 34;

	public const int OverTriax = 35;

	public const int Cavitation = 36;

	public const int OverCavitation = 37;

	public const int MaxLiquidTension = 38;

	public const int BlockMapMiss = 39;

	public const int XiFail = 40;

	public static float Val(uint[] f, int slot)
	{
		if (f == null || slot >= f.Length || f[slot] == 0)
		{
			return 0f;
		}
		return BitConverter.ToSingle(BitConverter.GetBytes(f[slot]), 0);
	}

	public static uint Hits(uint[] f, int slot)
	{
		return f == null || slot >= f.Length ? 0u : f[slot];
	}

	private static string Gpa(double pa)
	{
		return (pa / 1000000000.0).ToString("F2", CultureInfo.InvariantCulture) + " ГПа";
	}

	private static string CoherentPart(uint[] f, int steps, int coherentSteelParticles)
	{
		uint hits = Hits(f, CoherentAboveAlphaEps);
		if (coherentSteelParticles < 0)
		{
			return "у тел У14 доля не считана: знаменатель не сообщён";
		}
		if (coherentSteelParticles == 0)
		{
			return hits == 0 ? "тел У14 в сцене нет ⇒ пол не применяется вовсе, вопрос годности не стоит" : string.Format("⛔ тел У14 нет, а счётчик У14-шагов выше перехода = {0} — прибор врёт, разобраться", hits);
		}
		double denominator = Math.Max(1.0, (double)steps * coherentSteelParticles);
		return string.Format("у тел У14 (метаемые, {0} частиц): {1} = ", coherentSteelParticles, hits) + ((double)hits / denominator).ToString("P4", CultureInfo.InvariantCulture) + " их частице-шагов вне области годности";
	}

	public static List<Row> Census(uint[] f, int particles, int steps, float capK, int steelParticles = -1, int coherentSteelParticles = -1)
	{
		List<Row> rows = new List<Row>();
		double denominator = Math.Max(1.0, (double)steps * particles);
		float maxPressure = Val(f, MaxPressure);
		float maxTension = Val(f, MaxTension);
		float maxJHi = Val(f, MaxJHi);
		float maxJLoInv = Val(f, MaxJLoInv);
		float minJ = maxJLoInv > 0f ? 1f / maxJLoInv : 0f;
		float maxTriax = Val(f, MaxTriax);
		float maxEps = Val(f, MaxEpsP);
		float maxRate = Val(f, MaxRate);
		float maxQ = Val(f, MaxQ);
		float maxQOverP = Val(f, MaxQOverP);
		float maxDmg = Val(f, MaxDmg);
		float capPressure = capK * SuperDamage.PressureCapFactorN;

		Action<string, string, string, string, string, ulong, double, string, bool, bool, bool, bool> add = delegate(string name, string value, string bounds, string measured, string ratio, ulong hits, double share, string note, bool counted, bool modelTerm, bool validity, bool perParticle)
		{
			rows.Add(new Row
			{
				name = name,
				value = value,
				bounds = bounds,
				measured = measured,
				ratio = ratio,
				hits = hits,
				counted = counted,
				modelTerm = modelTerm,
				validity = validity,
				perParticle = perParticle,
				share = share,
				note = note
			});
		};
		Action<string, string, string, string, string, ulong, double, string> simpleCustom = delegate(string name, string value, string bounds, string measured, string ratio, ulong hits, double share, string note)
		{
			add(name, value, bounds, measured, ratio, hits, share, note, true, false, false, false);
		};

		ulong capHits = (ulong)Hits(f, JWhole) + Hits(f, PCapWhole) + Hits(f, JDebris) + Hits(f, PCapDebris);
		string capMeasured = string.Format("достигнуто |p| = {0} (выборка 1/{1})", Gpa(maxPressure), SampleStride);
		if (Hits(f, OverPCap) != 0)
		{
			capMeasured += "; в момент среза " + Gpa(Val(f, OverPCap));
		}
		simpleCustom("потолок давления N·K", capK > 0f ? string.Format("N = {0:F0}, N·K = {1}", SuperDamage.PressureCapFactorN, Gpa(capPressure)) : string.Format("N = {0:F0} (K сцены не задан)", SuperDamage.PressureCapFactorN), "гидростатическое давление p₁ ЛЮБОЙ частицы", capMeasured, capK > 0f && maxPressure > 0f ? string.Format("предел / достигнутое = {0:F1}", capPressure / maxPressure) : "отношения нет: K сцены или замер нулевые", capHits, capHits / denominator, string.Format("из них на ЦЕЛОМ веществе {0}, на обломках {1} ", Hits(f, PCapWhole), Hits(f, PCapDebris)) + "(обломочная половина до S20 не считалась вовсе)");

		string jMeasured = string.Format("достигнуто J ∈ [{0:F4}; {1:F3}] (выборка 1/{2})", minJ, maxJHi, SampleStride);
		if (Hits(f, OverJHi) != 0)
		{
			jMeasured += string.Format("; сверху срезано при J = {0:F2}", Val(f, OverJHi));
		}
		if (Hits(f, OverJLoInv) != 0)
		{
			jMeasured += string.Format("; снизу при J = {0:F4}", 1f / Val(f, OverJLoInv));
		}
		string jRatio = maxJHi > 0f ? string.Format("верхний предел / достигнутое = {0:F1}; ", 20f / maxJHi) + string.Format("нижний: достигнутое / предел = {0:F1}", minJ / 0.05f) : "отношения нет: замер нулевой";
		ulong jHits = (ulong)Hits(f, JWhole) + Hits(f, JDebris);
		simpleCustom("зажим J = det F", string.Format("[{0}; {1}]", 0.05f, 20f), "объёмное отношение J; срабатывание ещё и СТИРАЕТ форму F (сброс к изотропному)", jMeasured, jRatio, jHits, jHits / denominator, string.Format("на ЦЕЛОМ {0} (потолок пригодности — SuperDamage.DegeneracyCeiling), ", Hits(f, JWhole)) + string.Format("на обломках {0}", Hits(f, JDebris)));

		string coherentMeasured = string.Format("достигнуто растяжение {0} (выборка 1/{1})", Gpa(maxTension), SampleStride);
		if (Hits(f, OverCoh) != 0)
		{
			coherentMeasured += "; в момент среза " + Gpa(Val(f, OverCoh));
		}
		ulong coherentHits = Hits(f, CohTension);
		simpleCustom("пол растяжения цельного тела (У14)", Gpa(SuperDamage.CoherentTensionFloorPa) + " (ASSUMED_BY_SUBSTITUTION)", "растяжение частицы тела с запретом дробления — метаемой пластины ДЗ/НДЗ", coherentMeasured, maxTension > 0f ? string.Format("предел / достигнутое = {0:F2}", Math.Abs(SuperDamage.CoherentTensionFloorPa) / maxTension) : "тел с запретом дробления в сцене нет", coherentHits, coherentHits / denominator, "S23: PLACEHOLDER снят — источник: армко-железо, откол 1.9 ГПа при HEL 0.9 ГПа (бриф 30, симметричный удар 0.2–0.4 км/с, ниже перехода α→ε); подстановка за мягкую сталь 4С20 метаемых пластин ДЗ; полоса измеренного семейства сталей ниже перехода 1.9…6.8 ГПа. ⚠️ ТА ЖЕ ФОРМА, ЧТО У СНЯТОГО ЗАЖИМА УС: тензильный зажим на ЦЕЛОМ веществе, при том что измеренное растяжение цельной метаемой −30 ГПа — на порядок ниже пола. ⚠️ Годность числа — НИЖЕ перехода α→ε; строка ниже меряет, соблюдён ли режим");

		uint steelHits = Hits(f, SteelAboveAlphaEps);
		float maxSteel = Val(f, MaxSteelPressure);
		double steelDenominator = steelParticles > 0 ? (double)steelParticles * steps : 0.0;
		string steelMeasured = maxSteel > 0f ? string.Format("достигнуто сжатие В СТАЛИ {0} (выборка 1/{1}, ЗАНИЖЕНО)", Gpa(maxSteel), SampleStride) : steelParticles == 0 ? "стали в сцене нет" : "сжатия в стали не замечено";
		string steelNote = steelParticles > 0 ? string.Format("знаменатель — СТАЛЬНЫЕ частице-шаги: {0} × {1} = {2:E3} ", steelParticles, steps, steelDenominator) + "(не все частице-шаги сцены)" : steelParticles < 0 ? "⚠️ знаменатель НЕ СООБЩЁН вызывающим (steelParticles < 0) ⇒ доля не считается" : "стальных частиц в сцене нет ⇒ доля не определена (это НЕ «ноль процентов»)";
		add("область годности пола откола: переход α(ОЦК) → ε(ГПУ) в стали", Gpa(SuperDamage.AlphaEpsilonTransitionPa) + " (порог режима, НЕ предел; выше него откольная прочность железа на 78–90 % больше — бриф 30)", "не ограничивает ничего: разделяет режимы, в одном из которых пол " + Gpa(SuperDamage.CoherentTensionFloorPa) + " годен, а в другом нет", steelMeasured, maxSteel > 0f ? string.Format("порог / достигнутое в стали = {0:F2}", SuperDamage.AlphaEpsilonTransitionPa / maxSteel) : "отношения нет: замер нулевой", steelHits, steelDenominator > 0.0 ? steelHits / steelDenominator : 0.0, steelNote + "; " + CoherentPart(f, steps, coherentSteelParticles), true, false, true, false);

		ContinuumProperties polyurethane = ContinuumMaterials.Get(ContinuumMaterialId.Polyurethane_Interlayer);
		ContinuumProperties neoprene = ContinuumMaterials.Get(ContinuumMaterialId.Neoprene_Interlayer);
		float effectivePu = SuperDamage.EffectiveCavitationPa(in polyurethane);
		float effectiveNeo = SuperDamage.EffectiveCavitationPa(in neoprene);
		Func<ContinuumProperties, float, string, string> rowText = delegate(ContinuumProperties m, float eff, string who)
		{
			string text = who + " " + (eff / 1000000f).ToString("F1", CultureInfo.InvariantCulture) + " МПа";
			if (SuperDamage.CavitationMaxFractionOfK <= 0f)
			{
				return text + " (ПЕРЕКРЫТИЕ СТЕНДА)";
			}
			if (SuperDamage.CavitationCeilingBinds(in m))
			{
				return text + " (строка " + (m.cavitationPa / 1000000f).ToString("F0", CultureInfo.InvariantCulture) + " МПа СРЕЗАНА потолком " + string.Format("{0:P1} K = {1} МПа, {2})", SuperDamage.CavitationMaxFractionOfK, (SuperDamage.CavitationCeilingPa(in m) / 1000000f).ToString("F1", CultureInfo.InvariantCulture), "MEASURED (S37 BAND/S31NERA 24.09)");
			}
			return text + string.Format(" (строка реестра, {0}; потолок {1:P1} K не связал)", "ASSUMED", SuperDamage.CavitationMaxFractionOfK);
		};
		float maxLiquid = Val(f, MaxLiquidTension);
		uint cavitationHits = Hits(f, Cavitation);
		string cavitationMeasured;
		if (maxLiquid > 0f)
		{
			cavitationMeasured = string.Format("достигнуто растяжение жидкости до среза {0} (выборка 1/{1}, ЗАНИЖЕНО)", Gpa(maxLiquid), SampleStride);
			if (cavitationHits != 0)
			{
				cavitationMeasured += "; в момент среза до " + Gpa(Val(f, OverCavitation));
			}
		}
		else
		{
			cavitationMeasured = !SuperDamage.CavitationOn ? "не меряется: при выключенном флаге cavitationPa на GPU = 0 у всех строк" : "растяжения жидкости не замечено (жидких строк в сцене нет или они не тянулись)";
		}
		simpleCustom("кавитационный предел жидкости (S37)", SuperDamage.CavitationOn ? rowText(polyurethane, effectivePu, "полиуретан") + "; " + rowText(neoprene, effectiveNeo, "неопрен") : "выключен (флаг SuperDamage.CavitationOn)", "гидростатическое растяжение p₁ частицы строки с G = 0 (объявленная жидкость)", cavitationMeasured, SuperDamage.CavitationOn && maxLiquid > 0f ? "|предел| / достигнутое = " + (Math.Abs(effectivePu) / maxLiquid).ToString("F3", CultureInfo.InvariantCulture) : "отношения нет: флаг выключен или замер нулевой", cavitationHits, cavitationHits / denominator, "источник: аналог *MAT_NULL «PC» LS-DYNA / гидравлического предела растяжения Autodyn для жидкости с ударным УС (прослойки НДЗ, бриф 25); −50 МПа внутри полосы −10…−200 МПа кавитационной прочности эластомеров — ASSUMED, бриф 37 запрошен S37 23.09, значение не подбиралось под ворота. ⚠️ Ноль срабатываний на сцене БЕЗ жидких строк (или при выключенном флаге) — ОЖИДАЕМОЕ значение, а НЕ «предел не проверен»: срез стоит на строке, и тянуть там нечего. Живость счётчика показывает сцена с прослойкой (PKG) при включённом флаге; живость самой правки — диверсия SuperDamage.SabotageNoCavitation (срез снят ⇒ вырождение обязано вернуться). Срез ОБРАТИМ — это не отказ и не обломки");

		uint debrisHits = Hits(f, DebrisTension);
		simpleCustom("обнуление растяжения обломка", "0 Па (жёсткий ноль)", "растяжение разрушенной частицы — убирается ВЕСЬ тензильный диапазон", debrisHits != 0 && Val(f, OverDebris) != 0f ? "наибольшее выброшенное растяжение " + Gpa(Val(f, OverDebris)) : "не срабатывало", "отношения НЕТ и быть не может: предел равен нулю, а не доле чего-либо. Читается только выброшенная величина (слева) и частота", debrisHits, debrisHits / denominator, "физически защитимо (грани трещины не тянут), но это самый жёсткий зажим файла по доле снятого диапазона");

		string triaxMeasured = string.Format("достигнуто |σ*| = {0:F2}", maxTriax);
		if (Hits(f, OverTriax) != 0)
		{
			triaxMeasured += string.Format("; в момент зажима {0:F2}", Val(f, OverTriax));
		}
		uint triaxHits = Hits(f, Triax);
		simpleCustom("зажим триаксиальности σ*", "[−1.5; 1.5]", "σ* = −P/σ_eq в показателе exp(D₃·σ*) деформации разрушения", triaxMeasured, maxTriax > 0f ? string.Format("предел / достигнутое = {0:F2}", 1.5f / maxTriax) : "ветвь повреждаемости в сцене не работала", triaxHits, triaxHits / denominator, "экспонента: зажим на 1.5 при D₃ порядка −3 меняет ε_f в разы, а не на проценты");

		rows.Add(Simple(f, EfFloor, denominator, "пол деформации разрушения ε_f", "1e-3", "ε_f — ЗНАМЕНАТЕЛЬ приращения повреждения ΔD = Δε_p/ε_f", "срабатывание = потолок ΔD в 1000·Δε_p за шаг; живость счётчика доказана диверсией _MutEfCollapse (S21, NightS21)"));
		rows.Add(Simple(f, SoftFloor, denominator, "пол размягчения", string.Format("{0:F2}", SuperDamage.SofteningFloor), "множитель прочности (1 − c·D) — предел уже В ФОРМЕ ОТНОШЕНИЯ (2 % от неповреждённой)", "отношение задано самой записью предела"));
		string epsMeasured = string.Format("достигнуто ε_p = {0:F2} (выборка 1/{1}) ⇒ отношение предел/достигнутое = ", maxEps, SampleStride) + (maxEps > 0f ? (1f / maxEps).ToString("F2", CultureInfo.InvariantCulture) : "—");
		rows.Add(Simple(f, EpsSat, denominator, "насыщение упрочнения ε_sat", "1.00", "накопленная пластическая деформация в члене A + B·ε^n", epsMeasured));

		uint rateHits = Hits(f, RateFloor);
		add("член скорости Джонсона–Кука ε̇* = max(ε̇_p/ε̇₀; 1) — МОДЕЛЬ, НЕ ПРЕДЕЛ", "ε̇* ⩾ 1 (соглашение J-C)", "логарифмические члены 1 + C·ln ε̇* (текучесть) и 1 + D₄·ln ε̇* (разрушение)", string.Format("достигнуто ratio = {0:E2} (выборка 1/{1})", maxRate, SampleStride), "отношения нет и быть не может: это форма члена модели — соглашение убирает скоростное РАЗУПРОЧНЕНИЕ и по построению применяется у всего покоящегося вещества", rateHits, rateHits / denominator, "", true, true, false, false);

		uint xiHits = Hits(f, XiFail);
		string xiValue;
		if (SuperDamage.FailureStrainScatterOn)
		{
			xiValue = "m = " + SuperDamage.FailureStrainScatterModulus.ToString("G4", CultureInfo.InvariantCulture) + ", " + string.Format("пол {0}, реализация 0x{1:X8}", 0.1f.ToString("F2", CultureInfo.InvariantCulture), SuperDamage.FailureStrainScatterSalt);
		}
		else
		{
			xiValue = SuperDamage.XiFieldOn ? "разброс ВЫКЛ (флаг SuperDamage.FailureStrainScatterOn)" : "ВЫКЛ (флаг SuperDamage.FailureStrainScatterOn)";
		}
		if (SuperDamage.S43ScalingOn)
		{
			xiValue += "; масштабирование S43: " + SuperDamage.S43StateText();
		}
		string xiNote;
		if (SuperDamage.XiFieldOn)
		{
			xiNote = SuperDamage.S43ScalingOn ? "живость разброса: счётчик обязан быть не нулём в каждом ВКЛ прогоне с разрывом; Вейбулл — предельное распределение слабейшего звена (минимум прочностей многих независимых дефектов в объёме частицы); единственный параметр формы m задаёт ширину (CoV ≈ 1.2/m); ASSUMED" : "S43 без разброса: счётчик считает отказы при ξ < 1 — при множителе ⩾ 1 везде законно 0, живость судится только при разбросе ВКЛ (D18); потолок 10 ASSUMED (S43 P1.3), только при масштабировании ВКЛ: множитель выше потолка — счётчик, не молчание";
		}
		else
		{
			xiNote = xiHits == 0 ? "0 ПО ПОСТРОЕНИЮ при ВЫКЛ: вариант шейдера SUPER_XI не собран в путь — это НЕ «не проверен»" : string.Format("⛔ флаг ВЫКЛ, а счётчик = {0} — ядро считало вариантом SUPER_XI; прибор или проводка врут, разобраться", xiHits);
		}
		add("разброс деформации разрушения ξ (S32) — МОДЕЛЬ, НЕ ПРЕДЕЛ", xiValue, "деформация разрушения частицы ε_f,i = ε_f·ξ_i в ΔD = Δε_p/(ε_f·ξ_i); ξ — поле запекания со средним 1", string.Format("частиц, ушедших в разрушение по повреждаемости (причина 1) при ξ < 1: {0}", xiHits), "отношения нет и быть не может: это член модели (разброс вещества), а не сторож", xiHits, xiHits / Math.Max(1.0, (double)particles), xiNote, true, true, false, true);

		rows.Add(Simple(f, TStar, denominator, "зажим гомологической температуры", "[0; 1]", "T* = (T−T_room)/(T_melt−T_room) в тепловом разупрочнении", "предел уже в форме отношения; срабатывание ниже нуля означает вещество холоднее комнатного"));
		rows.Add(Simple(f, RhoFloor, denominator, "пол плотности", string.Format("{0:F0} кг/м³", SuperDamage.RhoFloorKgM3), "ρ в множителе 1/ρ обновления внутренней энергии", "S21: НЕДОСТИЖИМ ПО ПОСТРОЕНИЮ при боевом значении — ρ = ρ₀/J при J ⩽ 20 даёт ρ ⩾ 63 кг/м³ у самой лёгкой строки реестра (прежняя запись «достижим через J = 20» была арифметически неверна); живость счётчика доказана диверсией _RhoFloor (NightS21)"));
		rows.Add(Simple(f, SigEqSkip, denominator, "знаменатель возврата σ_eq", string.Format("{0:E0} Па", SuperDamage.SigEqReturnFloorPa), "σ_eq в множителе радиального возврата s·(σ_y/σ_eq)", "1e-6 Па против пределов текучести порядка 1e9 Па — отношение 1e-15, вне физики по построению; живость счётчика доказана диверсией _SigEqFloor (S21, NightS21)"));

		add("искусственная вязкость q (НЕ предел)", string.Format("C_Q = {0}, C_L = {1}, потолка нет", SuperConstitutive.ViscosityQuadratic, SuperConstitutive.ViscosityLinear), "прибавляется к давлению в тензоре напряжений и в обновлении энергии", "достигнуто |q| = " + Gpa(maxQ) + "; против НАИБОЛЬШЕГО давления сцены это " + (maxPressure > 0f ? (maxQ / maxPressure).ToString("P2", CultureInfo.InvariantCulture) : "—") + string.Format(" (выборка 1/{0})", SampleStride), string.Format("⚠️ Поточечное |q|/max(|p|,1 Па) дало {0:E2} и ЭТО АРТЕФАКТ САМОГО ОТНОШЕНИЯ, ", maxQOverP) + "а не физики: у частиц с p ≈ 0 знаменатель прижат к полу в 1 Па. Читать надо левую колонку — |q| против пикового давления сцены" + "ПРЕДЕЛА НЕТ ВОВСЕ — строка включена именно поэтому: величина, свободно складываемая с давлением и нигде не ограниченная, в переписи пределов обязана быть видна. ", 0, 0.0, "первая же перепись показала, что отношение надо считать к ПИКУ сцены, а не поточечно", false, false, false, false);
		add("численные коэффициенты схемы (справка, НЕ пределы)", string.Format("C_Q = {0}, C_L = {1}, ", SuperConstitutive.ViscosityQuadratic, SuperConstitutive.ViscosityLinear) + string.Format("CFL = {0}, проходов возврата {1}, N = {2:F0}", SuperShot.Cfl, SuperDamage.YieldIterations, SuperDamage.PressureCapFactorN), "сама схема; что из отчитываемого от них зависит — реестр инвариантности S22", "—", "отношения нет: это не пределы, а ручки устойчивости/сходимости; их метка — ASSUMED", 0, 0.0, "", false, false, false, false);
		add("потолок повреждения D", string.Format("{0:F0}", SuperDamage.Cap), "накопленное повреждение D (D ⩾ 1 — уже обломок)", string.Format("достигнуто D = {0:F2} (выборка 1/{1})", maxDmg, SampleStride), string.Format("предел / порог разрушения = {0:F0} — отношение задано записью", SuperDamage.Cap), 0, 0.0, "физически инертен после D ⩾ 1: частица уже обломок и D больше не копит", false, false, false, false);
		return rows;
	}

	private static Row Simple(uint[] f, int slot, double denom, string name, string value, string bounds, string note)
	{
		uint hits = Hits(f, slot);
		return new Row
		{
			name = name,
			value = value,
			bounds = bounds,
			measured = hits != 0 ? "срабатывал (величина в момент срабатывания не снимается)" : "не срабатывал ни разу",
			ratio = note,
			hits = hits,
			counted = true,
			share = hits / denom,
			note = ""
		};
	}

	public static string OneLine(uint[] f, int particles, int steps, float capK, int steelParticles = -1, int coherentSteelParticles = -1)
	{
		if (f == null)
		{
			return "пределы: счётчики не прочитаны";
		}
		double denominator = Math.Max(1.0, (double)steps * particles);
		StringBuilder sb = new StringBuilder("пределы: ");
		int triggered = 0;
		Action<string, int> add = delegate(string name, int slot)
		{
			uint hits = Hits(f, slot);
			if (hits == 0)
			{
				return;
			}
			triggered++;
			sb.Append(name).Append(' ').Append(hits).Append(" (").Append(((double)hits / denominator).ToString("P4", CultureInfo.InvariantCulture)).Append("); ");
		};
		add("J-целое", JWhole);
		add("J-обломки", JDebris);
		add("потолок-целое", PCapWhole);
		add("потолок-обломки", PCapDebris);
		add("растяжение-обломка", DebrisTension);
		add("пол-цельного", CohTension);
		add("размягчение", SoftFloor);
		add("триакс", Triax);
		add("ε_f", EfFloor);
		add("ε_sat", EpsSat);
		add("T*", TStar);
		add("плотность", RhoFloor);
		add("σ_eq", SigEqSkip);
		add("кавитация-жидкости", Cavitation);
		if (triggered == 0)
		{
			sb.Append("не сработал ни один; ");
		}
		uint rateHits = Hits(f, RateFloor);
		if (rateHits != 0)
		{
			sb.Append("член ε̇*⩾1 применён ").Append(rateHits).Append(" (").Append(((double)rateHits / denominator).ToString("P4", CultureInfo.InvariantCulture)).Append(") — модель, не предел; ");
		}
		uint triaxHits = Hits(f, Triax);
		if (triaxHits != 0)
		{
			sb.Append("⚠️ ").Append(TriaxDeclaration).Append(" — ").Append(((double)triaxHits / denominator).ToString("P4", CultureInfo.InvariantCulture)).Append("; ");
		}
		float maxPressure = Val(f, MaxPressure);
		float capPressure = capK * SuperDamage.PressureCapFactorN;
		sb.Append("достигнуто |p| ").Append(Gpa(maxPressure));
		if (capK > 0f && maxPressure > 0f)
		{
			sb.Append(", потолок/достигнутое ").Append((capPressure / maxPressure).ToString("F1", CultureInfo.InvariantCulture));
			if (maxPressure > capPressure)
			{
				sb.Append(", потолком срезано ⩾").Append((1f - capPressure / maxPressure).ToString("P1", CultureInfo.InvariantCulture)).Append(" пика (условная действительность, решение 04.09)");
			}
		}
		sb.Append(" (максимумы по выборке 1/").Append(SampleStride).Append(')');
		uint steelHits = Hits(f, SteelAboveAlphaEps);
		float maxSteel = Val(f, MaxSteelPressure);
		if (steelParticles < 0)
		{
			sb.Append("; ⚠️ доля стали выше перехода α→ε не считана: знаменатель не сообщён");
		}
		else if (steelParticles == 0)
		{
			sb.Append("; стали в сцене нет");
		}
		else
		{
			double steelDenominator = Math.Max(1.0, (double)steelParticles * steps);
			sb.Append("; сталь выше перехода α→ε (").Append(Gpa(SuperDamage.AlphaEpsilonTransitionPa)).Append("): ").Append(steelHits).Append(" из ").Append(steelDenominator.ToString("E2", CultureInfo.InvariantCulture)).Append(" стальных частице-шагов = ").Append(((double)steelHits / steelDenominator).ToString("P4", CultureInfo.InvariantCulture)).Append(", пик в стали ").Append(Gpa(maxSteel));
			if (steelHits != 0 && coherentSteelParticles > 0)
			{
				sb.Append(" ⚠️ пол ").Append(Gpa(SuperDamage.CoherentTensionFloorPa)).Append(" вне области годности на этой доле шагов");
			}
		}
		sb.Append("; ").Append(CoherentPart(f, steps, coherentSteelParticles));
		uint xiHits = Hits(f, XiFail);
		if (xiHits != 0 || SuperDamage.XiFieldOn)
		{
			sb.Append("; ξ-разрушений (S32: причина 1 при ξ < 1) ").Append(xiHits).Append(" частиц (").Append(((double)xiHits / Math.Max(1.0, (double)particles)).ToString("P4", CultureInfo.InvariantCulture)).Append(')');
			if (SuperDamage.FailureStrainScatterOn)
			{
				sb.Append(", m = ").Append(SuperDamage.FailureStrainScatterModulus.ToString("G4", CultureInfo.InvariantCulture)).Append(" — модель, не предел");
			}
			if (SuperDamage.S43ScalingOn)
			{
				sb.Append(", S43: ").Append(SuperDamage.S43StateText()).Append(" — модель, не предел");
			}
			if (!SuperDamage.XiFieldOn && xiHits != 0)
			{
				sb.Append(" ⛔ флаг ξ ВЫКЛ, а счётчик не ноль — проводка врёт");
			}
		}
		return sb.ToString();
	}

	public static void Print(string scene, uint[] f, int particles, int steps, float capK, int steelParticles = -1, int coherentSteelParticles = -1)
	{
		Debug.Log("── ПЕРЕПИСЬ ПРЕДЕЛОВ · " + scene + " ──");
		double denominator = Math.Max(1.0, (double)steps * particles);
		Debug.Log(string.Format("    знаменатель: {0} частиц × {1} шагов = {2:E3} частице-шагов; ", particles, steps, denominator) + string.Format("максимумы сняты по выборке 1/{0} частиц (ЗАНИЖЕНЫ)", SampleStride));
		if (steelParticles >= 0)
		{
			Debug.Log(string.Format("    из них СТАЛЬ: {0} частиц ⇒ {1:E3} ", steelParticles, (double)steelParticles * steps) + "стальных частице-шагов — знаменатель строки области годности");
		}
		foreach (Row row in Census(f, particles, steps, capK, steelParticles, coherentSteelParticles))
		{
			string telemetry;
			if (!row.counted)
			{
				telemetry = "счётчика у этой строки НЕТ по построению — она включена ради отношения, а не частоты";
			}
			else if (row.validity)
			{
				telemetry = row.hits == 0 ? "вне области годности 0 частице-шагов — сцена ЦЕЛИКОМ в объявленном режиме" : string.Format("вне области годности {0} = {1:P4} СТАЛЬНЫХ частице-шагов ⚠️", row.hits, row.share);
			}
			else if (row.modelTerm)
			{
				telemetry = row.perParticle ? string.Format("член модели: {0} = {1:P4} ЧАСТИЦ сцены (единица — частицы, не частице-шаги) — ТЕЛЕМЕТРИЯ МОДЕЛИ, не риск", row.hits, row.share) : string.Format("член модели применён на {0} = {1:P4} частице-шагов — ТЕЛЕМЕТРИЯ МОДЕЛИ, не риск", row.hits, row.share);
			}
			else
			{
				telemetry = row.hits == 0 ? "срабатываний 0 — ⚠️ предел НЕ ПРОВЕРЕН (правило 1: сторож, ни разу не сработавший, не проверен)" : string.Format("срабатываний {0} = {1:P4} частице-шагов", row.hits, row.share);
			}
			Debug.Log("    • " + row.name + " = " + row.value);
			Debug.Log("        ограничивает: " + row.bounds);
			Debug.Log("        замер: " + row.measured);
			Debug.Log("        ОТНОШЕНИЕ: " + row.ratio);
			Debug.Log("        " + telemetry + (string.IsNullOrEmpty(row.note) ? "" : "; " + row.note));
		}
	}
}
