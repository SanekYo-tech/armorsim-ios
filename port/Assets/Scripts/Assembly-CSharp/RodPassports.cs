using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public static class RodPassports
{
	public const float MassTolFrac = 0.1f;

	public const string KeyMassDerived = "catalog.passport.mass_derived";

	public const string KeyMassMismatch = "catalog.passport.mass_mismatch";

	public const string KeyProvenanceUnnamed = "catalog.passport.provenance_unnamed";

	public const bool ResolutionMeasured = true;

	public const bool ResolutionFloorFound = false;

	public const float ResolutionFloorLayers = float.NaN;

	public const float ResolutionMaxLayersTried = 16.6f;

	public const float ResolutionLastDeltaPct = 19.2f;

	public const string ResolutionFloorSource = "S44, лестница шага 27–28.09 (s44ladder1, s44ladderN)";

	public const string KeyResolutionNote = "super.note.rod_resolution";

	public const float ResolutionFineStepMM = 1f;

	public const string WaiverNoteOwner2809 = "слово владельца 28.09: завести без сверки массы и без предупреждений";

	private static readonly RodPassport[] table;

	private static readonly Dictionary<string, PassportVerdict> verdicts;

	public static IList<RodPassport> All { get; }

	public static IList<RodPassport> Admitted { get; }

	public static IList<RodPassport> Refused { get; }

	private static Dictionary<string, PassportVerdict> BuildVerdicts()
	{
		Dictionary<string, PassportVerdict> result = new Dictionary<string, PassportVerdict>();
		foreach (RodPassport passport in table)
		{
			result.Add(passport.id, Check(passport));
		}
		return result;
	}

	private static IList<RodPassport> Split(bool admitted)
	{
		List<RodPassport> result = new List<RodPassport>();
		foreach (RodPassport passport in table)
		{
			if (verdicts[passport.id].admitted == admitted)
			{
				result.Add(passport);
			}
		}
		return new ReadOnlyCollection<RodPassport>(result);
	}

	public static float ProfileMassKg(float dM, float lM, NoseShape nose, float densityKgM3)
	{
		SuperRodSpec rod = new SuperRodSpec
		{
			diameterM = dM,
			lengthM = lM,
			nose = nose
		};
		return (float)(SuperBaker.RodVolume(rod) * densityKgM3);
	}

	public static float ClosingDiameterMM(RodPassport p)
	{
		float lo = 0.06f;
		float hi = 0.001f;
		for (int i = 0; i < 60; i++)
		{
			float mid = (lo + hi) * 0.5f;
			if (p.coreMass.nominal >= ProfileMassKg(mid, p.coreL.nominal * 0.001f, p.nose, p.density.nominal))
			{
				lo = mid;
			}
			else
			{
				hi = mid;
			}
		}
		return (lo + hi) * 0.5f * 1000f;
	}

	public static float CheckDiameterMM(RodPassport p)
	{
		if (p.coreD.tag == PassportTag.AuthorCalcFromOtherQuantity && p.hasLOverD)
		{
			return p.coreL.nominal / p.lOverD.nominal;
		}
		return p.coreD.nominal;
	}

	public static PassportVerdict Check(RodPassport p)
	{
		float checkD = CheckDiameterMM(p);
		float profileMass = ProfileMassKg(checkD * 0.001f, p.coreL.nominal * 0.001f, p.nose, p.density.nominal);
		float published = p.coreMass.nominal;
		float mismatch = published > 0f ? (profileMass - published) / published : float.NaN;
		float closing = ClosingDiameterMM(p);
		object[] args = new object[8]
		{
			p.id,
			published * 1000f,
			profileMass * 1000f,
			mismatch * 100f,
			MassTolFrac * 100f,
			closing,
			p.coreD.lo,
			p.coreD.hi
		};
		PassportVerdict result = new PassportVerdict
		{
			profileMassKg = profileMass,
			publishedMassKg = published,
			mismatchFrac = mismatch,
			closingDiameterMM = closing,
			checkDiameterMM = checkD,
			publishedLoKg = p.coreMass.lo,
			publishedHiKg = p.coreMass.hi,
			args = args
		};
		if (p.massCheckWaived)
		{
			result.admitted = true;
			result.key = null;
			result.args = null;
		}
		else if (p.coreD.tag == PassportTag.AuthorCalcFromSameDims)
		{
			result.key = KeyMassDerived;
		}
		else if (published > 0f && !float.IsNaN(mismatch) && Math.Abs(mismatch) <= MassTolFrac)
		{
			result.admitted = true;
			result.key = null;
			result.args = null;
		}
		else if (p.provenanceUnnamed)
		{
			result.key = KeyProvenanceUnnamed;
		}
		else
		{
			result.key = KeyMassMismatch;
		}
		return result;
	}

	public static bool TryFind(string id, out RodPassport p)
	{
		foreach (RodPassport row in table)
		{
			if (row.id == id)
			{
				p = row;
				return true;
			}
		}
		p = default;
		return false;
	}

	public static bool Refusal(ProjectileDefinition d, out string key, out object[] args)
	{
		key = null;
		args = null;
		if (d == null || string.IsNullOrEmpty(d.passportId) || !TryFind(d.passportId, out RodPassport passport))
		{
			return false;
		}
		if (!verdicts.TryGetValue(passport.id, out PassportVerdict verdict))
		{
			verdict = Check(passport);
		}
		if (verdict.admitted)
		{
			return false;
		}
		key = verdict.key;
		args = verdict.args;
		return true;
	}

	public static RoundSpec ToRoundSpec(RodPassport p)
	{
		return new RoundSpec
		{
			id = p.id,
			displayName = p.displayName,
			category = RoundCatalog.AC,
			calibreMM = p.calibreMM,
			penetratorMassKg = p.coreMass.nominal,
			muzzleVelocityMps = p.velocity.nominal,
			lengthOverDiameter = p.hasLOverD ? p.lOverD.nominal : p.coreL.nominal / p.coreD.nominal,
			core = p.core,
			limitation = Limitation(p),
			geometrySource = RoundGeometrySource.Dimensions,
			massToleranceFrac = MassTolFrac,
			coreLengthMM = RoundCatalog.Num(p.coreL.nominal, string.Format("бриф 38 §B: {0} [{1}; разброс {2:F0}–{3:F0} мм]", p.coreL.source, TagRu(p.coreL.tag), p.coreL.lo, p.coreL.hi)),
			coreDiameterMM = RoundCatalog.Num(p.coreD.nominal, string.Format("бриф 38 §B: {0} [{1}; разброс {2:F2}–{3:F2} мм]", p.coreD.source, TagRu(p.coreD.tag), p.coreD.lo, p.coreD.hi) + string.Format("; масса строки DERIVED — профиль запекателя при ρ ядра {0:F0} кг/м³ ", p.density.nominal) + string.Format("(опубликовано {0:F0}–{1:F0} г, в строку не идёт); {2}", p.coreMass.lo * 1000f, p.coreMass.hi * 1000f, p.note)),
			coreMaterialId = string.IsNullOrEmpty(SuperShotRunner.MapCore(p.core)) ? default : RoundCatalog.Name(SuperShotRunner.MapCore(p.core), SubstitutionProvenance(p)),
			nose = RoundCatalog.Nose(p.nose, p.noseSource),
			passportId = p.id
		};
	}

	private static string Limitation(RodPassport p)
	{
		return SubstitutionRu(p) + " " + ResolutionLineRu(p.coreD.nominal);
	}

	private static string SubstitutionRu(RodPassport p)
	{
		TateModel.Penetrator penetrator = RoundCatalog.CorePenetrator(p.core);
		if (p.core == Projectile.CoreType.WC)
		{
			return string.Format("Сердечник считается карбидом вольфрама (TateModel.WC, ρ {0:F0} кг/м³); ", penetrator.density) + "в режиме Super модели карбида нет.";
		}
		if (p.core == Projectile.CoreType.DU)
		{
			return string.Format("Сердечник считается нашей строкой урана {0} (U-0.75Ti, ρ {1:F0} кг/м³).", "DU_075TI", penetrator.density);
		}
		if (p.core == Projectile.CoreType.WHA)
		{
			return string.Format("Сердечник считается нашей строкой {0} (91W-7Ni-2Fe, ρ {1:F0} кг/м³).", SuperShotRunner.MapCore(p.core) ?? "WHA", penetrator.density);
		}
		return string.Format("Сердечник считается материалом «{0}» (ρ {1:F0} кг/м³).", penetrator.name, penetrator.density);
	}

	private static string SubstitutionProvenance(RodPassport p)
	{
		TateModel.Penetrator penetrator = RoundCatalog.CorePenetrator(p.core);
		string ours;
		if (p.core == Projectile.CoreType.WHA)
		{
			ours = string.Format("ядро — TateModel.WHA, ρ {0:F0} кг/м³ (состав не объявлен); Super — строка реестра ", penetrator.density) + "«" + (SuperShotRunner.MapCore(p.core) ?? "—") + "» = 91W-7Ni-2Fe (MaterialModels), ρ 17300 кг/м³";
		}
		else
		{
			ours = string.Format("«{0}», ρ {1:F0} кг/м³ (TateModel; строка реестра Super «{2}»)", penetrator.name, penetrator.density, SuperShotRunner.MapCore(p.core) ?? "нет");
		}
		return "ASSUMED_BY_SUBSTITUTION: источник даёт " + p.materialText + ", ρ " + RhoRangeRu(p) + "; у нас " + ours + ".";
	}

	private static string RhoRangeRu(RodPassport p)
	{
		return string.Format("{0:F2}–{1:F2}", p.density.lo * 0.001f, p.density.hi * 0.001f) + " г/см³";
	}

	public static ProjectileDefinition MakeDefinition(RodPassport p)
	{
		return RoundCatalog.MakeDefinition(ToRoundSpec(p));
	}

	public static string Tooltip(RodPassport p)
	{
		if (!verdicts.TryGetValue(p.id, out PassportVerdict v))
		{
			v = Check(p);
		}
		string reason;
		if (v.key == KeyMassDerived)
		{
			reason = "масса сердечника в источнике не измерена, а посчитана автором по тем же размерам — независимой массы нет, сверять не с чем";
		}
		else if (v.key == KeyProvenanceUnnamed)
		{
			reason = "масса с размерами сходится, но числа паспорта даны без автора и документа: " + p.provenanceNote;
		}
		else
		{
			reason = "паспорт не сходится сам с собой";
		}
		string head = p.displayName + " (" + p.calibreText + "), калибр " + p.calibreMM + " мм; год источником не назван. " + string.Format("{0}: ⌀{1:F2} × {2:F0} мм ", RoundCatalog.CorePenetrator(p.core).name, v.checkDiameterMM, p.coreL.nominal) + string.Format("({0}), ρ {1:F2} г/см³, v₀ = {2:F0} м/с.", NoseRu(p.nose), p.density.nominal * 0.001f, p.velocity.nominal);
		string tolerance = v.key == KeyMassDerived ? "" : string.Format(" при допуске {0:F0} %", MassTolFrac * 100f);
		string checkDiameter = Math.Abs(v.checkDiameterMM - p.coreD.nominal) > 0.0001f ? string.Format(" при ⌀ сверки {0:F2} мм", v.checkDiameterMM) : "";
		string refusal = string.Format(" ПАСПОРТ ОТКЛОНЁН: {0}. Масса сердечника опубликована {1:F0}–", reason, v.publishedLoKg * 1000f) + string.Format("{0:F0} г (середина {1:F1} г; ", v.publishedHiKg * 1000f, v.publishedMassKg * 1000f) + string.Format("{0}: {1}), тело по профилю {2:F1} г{3}, ", p.coreMass.source, TagRu(p.coreMass.tag), v.profileMassKg * 1000f, checkDiameter) + string.Format("расхождение {0:+0.0;-0.0} %{1}; ", v.mismatchFrac * 100f, tolerance) + string.Format("масса сошлась бы при ⌀{0:F2} мм. В каталог выстрелом не заводится — ", v.closingDiameterMM) + "плитка отказывает по имени на всех трёх путях: аналитика, Super и ULTRA (ULTRA передаёт выстрел аналитике, та отказывает).";
		string note = string.IsNullOrEmpty(p.note) ? "" : " " + p.note;
		return head + refusal + note + " " + ResolutionLineRu(p.coreD.nominal);
	}

	public static string ResolutionSentenceRu(float dMM)
	{
		return string.Format("пол не найден: глубина стержня не сошлась по шагу и при {0:F1} слоя ", ResolutionMaxLayersTried) + string.Format("(последний шаг лестницы ещё двигал её на {0:F0} %) — на меньшем числе слоёв ", ResolutionLastDeltaPct) + "она занижена (S44, лестница шага 27–28.09 (s44ladder1, s44ladderN))";
	}

	public static bool ResolutionNoteArgs(ProjectileDefinition d, float spacingM, out object[] args)
	{
		args = null;
		if (d == null || string.IsNullOrEmpty(d.passportId) || !TryFind(d.passportId, out RodPassport passport))
		{
			return false;
		}
		if (!verdicts.TryGetValue(passport.id, out PassportVerdict verdict))
		{
			verdict = Check(passport);
		}
		if (!verdict.admitted || spacingM <= 0f)
		{
			return false;
		}
		args = new object[4]
		{
			d.diameterMM / (spacingM * 1000f),
			spacingM * 1000f,
			ResolutionMaxLayersTried,
			ResolutionLastDeltaPct
		};
		return true;
	}

	public static string ResolutionLineRu(float dMM)
	{
		return string.Format("⚠ РАЗРЕШЕНИЕ: сердечник ⌀{0:F2} мм — {1:F1} слоя частиц при быстром шаге {2:F0} мм, ", dMM, dMM * 0.5f, 2f) + string.Format("{0:F1} при {1:F0} мм; {2}", dMM, ResolutionFineStepMM, ResolutionSentenceRu(dMM));
	}

	public static string TagRu(PassportTag t)
	{
		switch (t)
		{
		case PassportTag.Primary:
			return "ПЕРВИЧНОЕ";
		case PassportTag.Repeat:
			return "повтор";
		case PassportTag.AuthorCalcFromSameDims:
			return "расчёт автора из тех же размеров";
		case PassportTag.AuthorCalcFromOtherQuantity:
			return "расчёт автора из другой величины";
		case PassportTag.AuthorAssumption:
			return "допущение автора";
		case PassportTag.OursMapping:
			return "наша привязка";
		case PassportTag.AuthorCalcOrWeighing:
			return "расчёт или взвешивание автора";
		default:
			return t.ToString();
		}
	}

	private static string NoseRu(NoseShape n)
	{
		switch (n)
		{
		case NoseShape.Flat:
			return "плоский нос";
		case NoseShape.Conical:
			return "конический нос";
		case NoseShape.Ogive:
			return "оживальный нос";
		case NoseShape.BluntOgive:
			return "притупленное оживало";
		default:
			return n.ToString();
		}
	}

	private static PassportValue V(float lo, float hi, float nominal, PassportTag tag, string source)
	{
		return new PassportValue { lo = lo, hi = hi, nominal = nominal, tag = tag, source = source };
	}

	private static PassportValue V1(float value, PassportTag tag, string source)
	{
		return V(value, value, value, tag, source);
	}

	private static PassportValue Rho(float loGcc, float hiGcc, PassportTag tag, string source)
	{
		return V(loGcc * 1000f, hiGcc * 1000f, (loGcc + hiGcc) * 500f, tag, source);
	}

	private static PassportValue Mass(float loG, float hiG, float nominalG, PassportTag tag, string source)
	{
		return V(loG * 0.001f, hiG * 0.001f, nominalG * 0.001f, tag, source);
	}

	static RodPassports()
	{
		table = new RodPassport[5]
		{
		new RodPassport
		{
			id = "pmc287",
			designation = "PMC287",
			displayName = "PMC287",
			calibreText = "30 × 173",
			calibreMM = 30f,
			coreD = V(8.60000038f, 8.89999962f, 8.64999962f, (PassportTag)3, "выведено автором из объёма V≈9,37 см³ при 165 г и 17,6; цилиндрическая часть 8,65"),
			coreL = V(170f, 173f, 170f, (PassportTag)1, "рентгенографический замер EMI"),
			density = V(17600f, 17750f, 17675f, (PassportTag)1, "технические стандарты группы Oerlikon"),
			coreMass = V(0.160000011f, 0.165000007f, 0.162500009f, (PassportTag)0, "официальный бюллетень Oerlikon Pyrotec (PMC287 Design Parameters: Long Rod Mass = 165 g); 160 г — «данные деривативных испытаний»; номинал сверки — середина разброса 160–165"),
			velocity = V(1405f, 1405f, 1405f, (PassportTag)0, "каталог Oerlikon / Rheinmetall Pyrotec"),
			lOverD = V(19.5f, 20f, 19.75f, (PassportTag)0, "презентация Oerlikon Contraves Pyrotec «New Ammunition for Vehicle Armament»"),
			hasLOverD = true,
			nose = (NoseShape)1,
			noseSource = "ПЕРВИЧНОЕ (сборочный чертёж Oerlikon PMC287): «Остроконечная коническая геометрия с микропритуплением вершины» → Conical",
			core = (Projectile.CoreType)0,
			materialText = "WHA (вольфрам-никель-железо-кобальт), W не ниже 95 % — металлургическая спецификация Rheinmetall WHA",
			note = "Локальное утолщение узла соединения — до 8,85 мм (текст источника); в геометрию строки не входит.",
		},
		new RodPassport
		{
			id = "m919",
			designation = "M919",
			displayName = "M919",
			calibreText = "25 × 137",
			calibreMM = 25f,
			coreD = V(8.27299976f, 8.27299976f, 8.27299976f, (PassportTag)0, "замер ARL на ведущем пояске (0,325 in)"),
			coreL = V(82.8000031f, 82.8000031f, 82.8000031f, (PassportTag)0, "теневой оптический замер ARL (раздел 2.2, отчёт дисперсионных тестов M242) — без номера отчёта"),
			density = V(18600f, 18900f, 18750f, (PassportTag)1, "Benét Laboratories, закалённый U-0.75Ti"),
			coreMass = V(0.0680000037f, 0.0730000064f, 0.0705000013f, (PassportTag)2, "вычислено автором по объёму цилиндра сердечника ⌀8,27 × 82,8 мм при плотности 18,65 г/см³ (помета «Расчёт автора»)"),
			velocity = V(1345f, 1420f, 1385f, (PassportTag)0, "патент; таблицы M242; бюллетень ARL"),
			nose = (NoseShape)0,
			noseSource = "OursMapping: «ступенчато-притупленную форму вершины с резьбовым соединением под обтекатель» (ПЕРВИЧНОЕ, документация ARL) → Flat",
			core = (Projectile.CoreType)1,
			materialText = "«Обеднённый уран, легированный титаном (Staballoy, сплав U-0.75% Ti)» — MIL-HDBK-799(AR)",
			note = "Диаметр 8,636 мм (0,340 in) по патенту относится к корню стабилизатора, не к стержню. Вещество U-0.75Ti (MIL-HDBK-799) — наша строка урана DU_075TI (S31).",
			massCheckWaived = true,
			waiverNote = WaiverNoteOwner2809
		},
		new RodPassport
		{
			id = "m791",
			designation = "M791",
			displayName = "M791",
			calibreText = "25 × 137",
			calibreMM = 25f,
			coreD = V(13.5f, 13.5f, 13.5f, (PassportTag)0, "формуляр завода-изготовителя / технический чертёж TopWar (решение §2: вторичный)"),
			coreL = V(70f, 70f, 70f, (PassportTag)0, "формуляр завода-изготовителя / технический чертёж TopWar (решение §2: вторичный)"),
			density = V(17500f, 17700f, 17600f, (PassportTag)1, "MIL-T-21014 Class 3"),
			coreMass = V(0.105000004f, 0.110000007f, 0.107500002f, (PassportTag)2, "интеграл объёма оживально-цилиндрического тела ⌀13,5 × 70 мм при плотности 17,5 г/см³ (помета «Расчёт автора»)"),
			velocity = V(1345f, 1345f, 1345f, (PassportTag)0, "официальные баллистические таблицы пушки M242"),
			nose = (NoseShape)2,
			noseSource = "ПЕРВИЧНОЕ (заводская сборочная схема M791): «Оживальная форма вершины вольфрамового сердечника» → Ogive",
			core = (Projectile.CoreType)0,
			materialText = "«Вольфрамовый тяжёлый сплав (W-Ni-Fe, содержание W ~93%)» — GICHD; MIL-T-21014",
			note = "Снаряд в сборе с поддоном 134 г (TM 43-0001-27) — легче сердечника этих размеров (≈157 г по профилю при ρ 17,6 — DERIVED нами); при 134 г и L 70 мм цилиндр не толще 11,80 мм (ρ 17,50 — нижний край; DERIVED нами).",
			provenanceUnnamed = true,
			provenanceNote = "длина и диаметр — «Формуляр завода-изготовителя / технический чертёж TopWar»: документ не назван, TopWar — вторичный источник (решение §2)",
			massCheckWaived = true,
			waiverNote = WaiverNoteOwner2809
		},
		new RodPassport
		{
			id = "3ubr8",
			designation = "3UBR8",
			displayName = "3УБР8",
			calibreText = "30 × 165",
			calibreMM = 30f,
			coreD = V(8f, 8.39999962f, 8.19999981f, (PassportTag)0, "«Непосредственные микрометрические замеры извлечённых стержней различных производственных серий» — без автора и документа (решение §2)"),
			coreL = V(132f, 136f, 134f, (PassportTag)0, "«Инструментальный замер препарированного сердечника 3УБР8» — без автора и документа (решение §2)"),
			density = V(17000f, 17500f, 17250f, (PassportTag)1, "ГОСТ на тяжёлые спечённые сплавы ВНЖ"),
			coreMass = V(0.175000012f, 0.182000011f, 0.178500012f, (PassportTag)3, "вычитание стабилизатора, наконечника и трассера из 222 г полётной части — их массы не названы"),
			velocity = V(1120f, 1120f, 1120f, (PassportTag)0, "таблицы стрельбы 2А42 (ствол 2400 мм)"),
			nose = (NoseShape)1,
			noseSource = "OursMapping: «усечённо-коническую форму с притуплённым плоским торцом» (ПЕРВИЧНОЕ, описание патента) → Conical",
			core = (Projectile.CoreType)0,
			materialText = "«Спечённый вольфрамовый тяжёлый сплав (ВНЖ-90 или ВНЖ-95: 90–95% W, остальное Ni, Fe)» — ТУ завода «Прибор»",
			note = "Провенанс длины и диаметра не назван (решение §2 — принимается как повтор). Вещество ВНЖ-90/95 → наша строка WHA (подстановка).",
			provenanceUnnamed = true,
			provenanceNote = "длина («Инструментальный замер препарированного сердечника 3УБР8») и диаметр («Непосредственные микрометрические замеры извлечённых стержней…») даны без автора и документа (решение §2)",
			massCheckWaived = true,
			waiverNote = WaiverNoteOwner2809
		},
		new RodPassport
		{
			id = "3ubr6",
			designation = "3UBR6",
			displayName = "3УБР6",
			calibreText = "30 × 165",
			calibreMM = 30f,
			coreD = V(14f, 14f, 14f, (PassportTag)0, "замер цилиндрической части сердечника"),
			coreL = V(52f, 54f, 53f, (PassportTag)0, "чертёжный замер сердечника 3УБР6"),
			density = V(14800f, 15000f, 14900f, (PassportTag)1, "ГОСТ 3882-74, сплавы ВК"),
			coreMass = V(0.170000002f, 0.175000012f, 0.172500014f, (PassportTag)6, "«Лабораторное взвешивание извлечённого сердечника» — лаборатория не названа"),
			velocity = V(970f, 970f, 970f, (PassportTag)0, "официальные таблицы стрельбы пушки 2А42"),
			nose = (NoseShape)2,
			noseSource = "OursMapping: «Оживально-коническая форма вершины монолитного сердечника с плоской торцевой микрофаской» (ПЕРВИЧНОЕ, чертёж 3УБР6) → Ogive",
			core = (Projectile.CoreType)3,
			materialText = "«Металлокерамический спечённый твёрдый сплав карбида вольфрама на кобальтовой связке (марки ВК6 / ВК8: WC 92–94%, Co 6–8%)» — ТУ ГНПП «Прибор»",
			note = "Сердечник WC-Co (ВК6/ВК8, WC 92–94 %, Co 6–8 %): в Super класс заперт отсутствием модели Джонсона–Хольмквиста — вторая причина отказа по имени.",
			provenanceUnnamed = true,
			provenanceNote = "масса — «Лабораторное взвешивание извлечённого сердечника»: лаборатория и протокол не названы",
			massCheckWaived = true,
			waiverNote = WaiverNoteOwner2809
		},
		};
		All = new ReadOnlyCollection<RodPassport>(table);
		verdicts = BuildVerdicts();
		Admitted = Split(true);
		Refused = Split(false);
	}
}
