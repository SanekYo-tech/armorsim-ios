using System;
using System.Collections.Generic;
using UnityEngine;

public static class ScenePresets
{
	private class Stack
	{
		private readonly SceneFile file;

		private float frontZM;

		private float angleXDeg;

		private float angleYDeg;

		private float lastCentreZ;

		private float lastHalfZ;

		private float lastAngleX;

		private float lastAngleY;

		private bool hasLast;

		private float CosTheta => Mathf.Max(Mathf.Cos(angleXDeg * Mathf.Deg2Rad) * Mathf.Cos(angleYDeg * Mathf.Deg2Rad), 0.05f);

		public Stack(SceneFile f, float firstFrontZM)
		{
			file = f;
			frontZM = firstFrontZM;
		}

		public Stack Tilt(float horizontalDeg, float verticalDeg)
		{
			angleYDeg = horizontalDeg;
			angleXDeg = verticalDeg;
			return this;
		}

		public SavedObject Add(ArmorPlate.ObjectKind kind, ArmorPlate.ArmorType material, float thicknessMM, float gapAfterM, float widthM = 1f, float heightM = 1f)
		{
			float thicknessM = thicknessMM * 0.001f / CosTheta;
			float halfZ = HalfExtentZM(widthM, heightM, thicknessMM, angleXDeg, angleYDeg);
			float centreZ = frontZM + thicknessM * 0.5f;
			if (hasLast && Mathf.Approximately(lastAngleX, angleXDeg) && Mathf.Approximately(lastAngleY, angleYDeg) && lastCentreZ + lastHalfZ + halfZ > centreZ)
			{
				centreZ = lastCentreZ + lastHalfZ + halfZ;
				frontZM = centreZ - thicknessM * 0.5f;
			}
			SavedObject result = new SavedObject
			{
				kind = kind.ToString(),
				material = material.ToString(),
				posXM = 0f,
				posYM = 1f,
				posZM = centreZ,
				angleYDeg = angleYDeg,
				angleXDeg = angleXDeg,
				widthM = widthM,
				heightM = heightM,
				thicknessMM = thicknessMM
			};
			file.objects.Add(result);
			frontZM += thicknessM + gapAfterM / CosTheta;
			lastCentreZ = centreZ;
			lastHalfZ = halfZ;
			lastAngleX = angleXDeg;
			lastAngleY = angleYDeg;
			hasLast = true;
			return result;
		}

		private static float HalfExtentZM(float widthM, float heightM, float thicknessMM, float ax, float ay)
		{
			float cosAx = Mathf.Cos(ax * Mathf.Deg2Rad);
			float cosAy = Mathf.Cos(ay * Mathf.Deg2Rad);
			return (Mathf.Abs(Mathf.Tan(ay * Mathf.Deg2Rad) * cosAx) * widthM + Mathf.Abs(Mathf.Sin(ax * Mathf.Deg2Rad)) * heightM + Mathf.Abs(cosAx * cosAy) * (thicknessMM * 0.001f)) * 0.5f;
		}

		public void Gap(float millimetres)
		{
			frontZM += millimetres * 0.001f / CosTheta;
		}
	}

	public const string VehicleProvenance = "⚠ РЕКОНСТРУКЦИЯ, А НЕ СХЕМА МАШИНЫ. Состав брони серийных машин не публикуется; любая цифра в открытых источниках — оценка. Здесь собрано ПРАВДОПОДОБНОЕ разложение того же типа конструкции, чтобы было с чего начать опыт: порядок и толщины слоёв выбраны ради показа механизма и данными не являются. Что эта сборка держит — покажет выстрел по ней, и только он.";

	public const string ConstructionProvenance = "Учебная сборка: показывает один механизм модели. Толщины выбраны так, чтобы механизм было видно, и паспортом какой-либо машины не являются.";

	public const float ObliquePackVelocityMps = 1500f;

	private const string NeraThin = "nera_thin";

	private const string NeraNeoprene = "nera_neoprene";

	private const string NeraHeld = "nera_held";

	private const float NeraPlanM = 0.25f;

	private static float FirstFrontZM => SceneDefaults.ApproachM;

	private static SceneFile New(string name, string description, string provenance, string vehicleReference = "")
	{
		return new SceneFile
		{
			formatVersion = 5,
			name = name,
			description = description,
			provenanceNote = provenance,
			vehicleReference = vehicleReference,
			readOnly = true,
			savedUtc = "",
			objects = new List<SavedObject>(),
			projectile = null
		};
	}

	private static SavedProjectile Shot(ProjectileDefinition d)
	{
		return new SavedProjectile
		{
			catalogId = Catalog.ProjectileIdFor(d) ?? "",
			roundId = "",
			displayName = d.name,
			projectileClass = d.kind.ToString(),
			coreType = d.coreType.ToString(),
			rodLengthMM = d.rodLengthMM,
			diameterMM = d.diameterMM,
			velocityMps = d.velocityMps,
			coneDiameterMM = d.coneDiameterMM,
			fuzeStandoffMM = d.fuzeStandoffMM,
			isTandem = d.isTandem,
			precursorConeDiameterMM = d.precursorConeDiameterMM,
			nose = d.nose.ToString(),
			segments = d.segments,
			strands = d.strands,
			finBlades = d.finBlades,
			noFins = d.noFins,
			gunCalibreMM = d.gunCalibreMM,
			startZM = 1f
		};
	}

	private static SavedProjectile Rod()
	{
		return Shot(ProjectileLibrary.Default());
	}

	private static SavedProjectile Jet()
	{
		return Shot(ProjectileLibrary.ShapedCharge85());
	}

	private static SceneFile ObliquePack3030()
	{
		SceneFile f = New("Пакет 30+30 косой — 30 мм под 60°, зазор 100 мм, 30 мм прямо", "Рабочая постановка блока Super: наклонный лист, воздушный промежуток, прямой лист. Механизм — наклон удлиняет путь по ходу вдвое и уводит стержень вбок, а промежуток даёт обломкам разойтись до второго листа. На ней сняты приёмки S4v и S5.", "Учебная сборка: показывает один механизм модели. Толщины выбраны так, чтобы механизм было видно, и паспортом какой-либо машины не являются. ⚠ Скорость стержня 1500 м/с — подмена реестрового значения 1100 м/с: постановка снята именно на 1500, геометрия стержня взята из реестра без правок.");
		Stack s = new Stack(f, FirstFrontZM).Tilt(60f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 30f, 0.1f);
		s.Tilt(0f, 0f).Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 30f, 0f);
		f.projectile = Rod();
		f.projectile.velocityMps = ObliquePackVelocityMps;
		return f;
	}

	private static SceneFile Monolith(float thicknessMM)
	{
		SceneFile f = New(string.Format("Монолит RHA {0:F0} мм", thicknessMM), "Опорный случай: одна сплошная плита катаной брони. С ней сравнивается всё остальное — разнесение, керамика, метаемые пластины. Ничего, кроме сплошного металла, здесь не работает.", ConstructionProvenance);
		new Stack(f, FirstFrontZM).Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, thicknessMM, 0f);
		f.projectile = Rod();
		return f;
	}

	private static SceneFile Spaced()
	{
		SceneFile f = New("Разнесённая преграда — три листа с зазором", "Три тонких листа вместо одного толстого. Механизм — стандоф: струя проходит зазор, успевает разойтись и работает по следующему листу уже растянутой. ⚠ Выигрыш НЕ безусловен: по расчёту модели близкое разнесение может оказаться хуже монолита той же суммарной толщины — на том и опыт.", ConstructionProvenance);
		Stack s = new Stack(f, FirstFrontZM);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 30f, 0.15f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 30f, 0.15f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 30f, 0f);
		f.projectile = Jet();
		return f;
	}

	private static SceneFile SandwichCeramic()
	{
		SceneFile f = New("Композитный пакет — сталь / керамика / сталь", "Керамическая плитка между стальными листами. Механизм — высокое сопротивление внедрению у керамики при её же хрупкости: передний лист держит плитку, задний ловит осколки. Смени керамику на карбид бора или диборид титана — и увидишь разницу в поведении по глубине.", ConstructionProvenance);
		Stack s = new Stack(f, FirstFrontZM);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 20f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Ceramic, 40f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 40f, 0f);
		f.projectile = Rod();
		return f;
	}

	private static SceneFile SandwichTextolite()
	{
		SceneFile f = New("Композитный пакет — сталь / текстолит / сталь", "Стеклотекстолит между стальными листами — лёгкая прослойка вместо керамики. ⚠ У этого материала в модели самый слабый провенанс: сопротивление внедрению для него не измерено, а оценено. Сравнивай с керамическим пакетом той же толщины, а не с числом.", ConstructionProvenance);
		Stack s = new Stack(f, FirstFrontZM);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 20f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Textolite, 60f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 40f, 0f);
		f.projectile = Rod();
		return f;
	}

	private static SceneFile NeraArray()
	{
		SceneFile f = New("Пакет НДЗ — метаемые пластины", "Два блока НДЗ перед основой. Механизм тот же, что у динамической защиты — подача свежего металла в струю, — но привод не взрывной, а упругий: эластомер разгоняет обе пластины ОДНИМ импульсом, поэтому тонкая уходит быстрее толстой. ⚠ Сам импульс эластомера в модели предположен: измеренных скоростей пластин в открытой литературе нет.", ConstructionProvenance);
		Stack s = new Stack(f, FirstFrontZM);
		float thickness = NeraStackMM(NeraThin);
		SavedObject a = s.Add(ArmorPlate.ObjectKind.Nera, ArmorPlate.ArmorType.RHA, thickness, 0.05f, NeraPlanM, NeraPlanM);
		SavedObject b = s.Add(ArmorPlate.ObjectKind.Nera, ArmorPlate.ArmorType.RHA, thickness, 0.05f, NeraPlanM, NeraPlanM);
		SetNera(a, NeraThin);
		SetNera(b, NeraThin);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 60f, 0f);
		f.projectile = Jet();
		return f;
	}

	private static SceneFile EraOverComposite()
	{
		SceneFile f = New("ДЗ поверх композита", "Кассеты динамической защиты на композитном пакете. Механизм — метаемая пластина идёт ПОПЕРЁК струи и подаёт в неё свежий металл всё время своего хода. ⚠ Толщина пластины, её скорость и зазор в модели предположены, но в расчёт входят: правь их и смотри, что меняется. Одиночный заряд ДЗ гасит — ответ на неё тандемный.", ConstructionProvenance);
		Stack s = new Stack(f, FirstFrontZM);
		SavedObject era = s.Add(ArmorPlate.ObjectKind.Era, ArmorPlate.ArmorType.ERA, 25f, 0.05f, NeraPlanM, NeraPlanM);
		SetEra(era, 2.5f, 750f, 10f, 130f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 20f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Ceramic, 40f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 40f, 0f);
		f.projectile = Jet();
		return f;
	}

	private static SceneFile SlatOverThin()
	{
		SceneFile f = New("Решётка над тонкой основой", "Решётчатый экран перед тонким листом. Механизм чисто геометрический: вероятность задеть перекладину выводится из ширины прутка, шага решётки и калибра боевой части. ⚠ Что именно происходит при задеве, модель разделяет предположением — измеренного разделения исходов нет.", ConstructionProvenance);
		Stack s = new Stack(f, FirstFrontZM);
		SavedObject slat = s.Add(ArmorPlate.ObjectKind.Slat, ArmorPlate.ArmorType.RHA, 12f, 0.4f);
		slat.slatBarWidthMM = 12f;
		slat.slatPitchMM = 100f;
		slat.slatDudFraction = 0.5f;
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 20f, 0f);
		f.projectile = Jet();
		return f;
	}

	private static SceneFile ReconWesternComposite()
	{
		SceneFile f = New("Композитный пакет — тип «Абрамс» (реконструкция)", "Многослойный пакет из чередующихся стальных и лёгких слоёв — тип конструкции, применяемый на западных основных танках с конца 1970-х. Собран как отправная точка для опыта: меняй слой и смотри, что изменится.", VehicleProvenance, "M1 Abrams");
		Stack s = new Stack(f, FirstFrontZM);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 30f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Ceramic, 50f, 0.02f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 20f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Textolite, 50f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 40f, 0f);
		f.projectile = Rod();
		return f;
	}

	private static SceneFile ReconEasternEraOverComposite()
	{
		SceneFile f = New("ДЗ поверх композита — тип «Контакт-5» (реконструкция)", "Кассеты динамической защиты, установленные поверх многослойной основы, — тип компоновки, применяемый на восточных основных танках с середины 1980-х. Отправная точка для опыта, а не схема конкретной машины.", VehicleProvenance, "Т-72Б / Т-80У");
		Stack s = new Stack(f, FirstFrontZM);
		SavedObject era = s.Add(ArmorPlate.ObjectKind.Era, ArmorPlate.ArmorType.ERAHeavy, 25f, 0.03f, NeraPlanM, NeraPlanM);
		SetEra(era, 2.5f, 750f, 10f, 130f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Textolite, 60f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 100f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Ceramic, 50f, 0f);
		f.projectile = Rod();
		return f;
	}

	private static SceneFile ReconCastTurretWithNera()
	{
		SceneFile f = New("Литая башня с двумя пакетами НДЗ — тип «Т-90» (реконструкция)", "Литая башня с двумя отражающими пакетами между наружной и внутренней стенками. Механизм — подача свежего металла выпучивающимися пластинами: первый пакет с полиуретаном, второй с неопреном, между ними разнесение. Литьё взято намеренно: сварная башня из катаной брони появилась позже. ⚠ Литая сталь в модели держит столько же, сколько катаная, — ходовой множитель «литьё слабее проката» проверен и оказался инженерным фольклором; отличается она разрушением, а не сопротивлением.", VehicleProvenance, "Т-90 (ранний, литая башня)");
		Stack s = new Stack(f, FirstFrontZM).Tilt(40f, 35f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.CastSteel, 130f, 0f);
		SetNera(s.Add(ArmorPlate.ObjectKind.Nera, ArmorPlate.ArmorType.RHA, NeraStackMM(NeraThin), 0f, NeraPlanM, NeraPlanM), NeraThin);
		s.Gap(20f);
		SetNera(s.Add(ArmorPlate.ObjectKind.Nera, ArmorPlate.ArmorType.RHA, NeraStackMM(NeraNeoprene), 0f, NeraPlanM, NeraPlanM), NeraNeoprene);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.CastSteel, 180f, 0f);
		Liner(s, 20f);
		f.projectile = Rod();
		return f;
	}

	private static SceneFile ReconUraniumComposite()
	{
		SceneFile f = New("Пакет с ураном и глинозёмом — тип «M1A2» (реконструкция)", "Два отражающих пакета геометрии Хельда перед тяжёлым сердечником из обеднённого урана и керамики. Механизм двойной: пакеты подают свежий металл, уран работает ПЛОТНОСТЬЮ — инерционным членом, а не высоким сопротивлением внедрению. ⚠ Пластины пакетов остались стальными: метаемых материалов в модели три (катаная, высокотвёрдая, титан), урана среди них нет и выдумывать его нельзя.", VehicleProvenance, "M1A2 Abrams");
		Stack s = new Stack(f, FirstFrontZM).Tilt(45f, 20f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 50f, 0f);
		SetNera(s.Add(ArmorPlate.ObjectKind.Nera, ArmorPlate.ArmorType.RHA, NeraStackMM(NeraHeld), 0f, NeraPlanM, NeraPlanM), NeraHeld);
		s.Gap(10f);
		SetNera(s.Add(ArmorPlate.ObjectKind.Nera, ArmorPlate.ArmorType.RHA, NeraStackMM(NeraHeld), 0f, NeraPlanM, NeraPlanM), NeraHeld);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.DepletedUraniumArmour, 50f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Alumina, 120f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 100f, 0f);
		Liner(s, 20f);
		f.projectile = Rod();
		return f;
	}

	private static SceneFile ReconWedgeAndBox()
	{
		SceneFile f = New("Клин и короб — тип «Leopard 2A7» (реконструкция)", "Две конструкции подряд, и у каждой свой наклон. Сперва навесной клин: тонкая высокотвёрдая щека под большим углом, за ней пустота клина, за ней его внутренняя стенка. Дальше — основной короб с отражающим пакетом, керамикой, вольфрамом и титаном. ⚠ Клин работает УГЛОМ: путь по линии визирования растёт как t/cosθ, и на 45°/60° это уже втрое к собственной толщине листа.", VehicleProvenance, "Leopard 2A7");
		Stack s = new Stack(f, FirstFrontZM).Tilt(45f, 60f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.HHA, 20f, 0f);
		s.Gap(150f);
		s.Tilt(35f, 10f).Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.HHA, 50f, 0f);
		SetNera(s.Add(ArmorPlate.ObjectKind.Nera, ArmorPlate.ArmorType.RHA, NeraStackMM(NeraThin), 0f, NeraPlanM, NeraPlanM), NeraThin);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Ceramic, 150f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Tungsten, 40f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Titanium, 30f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 80f, 0f);
		Liner(s, 15f);
		f.projectile = Rod();
		return f;
	}

	private static SceneFile ReconCeramicBehindNera()
	{
		SceneFile f = New("Керамика за двумя пакетами — тип «Challenger 2» (реконструкция)", "Два разных отражающих пакета подряд — геометрии Хельда и тонкий с неопреном, — за ними толстая керамика и титан. Механизм показателен именно порядком: пакеты работают по головной части угрозы, керамика по тому, что от неё осталось. Смени порядок слоёв и посмотри, меняется ли исход.", VehicleProvenance, "Challenger 2");
		Stack s = new Stack(f, FirstFrontZM).Tilt(45f, 10f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 70f, 0f);
		SetNera(s.Add(ArmorPlate.ObjectKind.Nera, ArmorPlate.ArmorType.RHA, NeraStackMM(NeraHeld), 0f, NeraPlanM, NeraPlanM), NeraHeld);
		SetNera(s.Add(ArmorPlate.ObjectKind.Nera, ArmorPlate.ArmorType.RHA, NeraStackMM(NeraNeoprene), 0f, NeraPlanM, NeraPlanM), NeraNeoprene);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Ceramic, 150f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Titanium, 20f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 120f, 0f);
		Liner(s, 25f);
		f.projectile = Rod();
		return f;
	}

	private static SceneFile ReconLightMultilayer()
	{
		SceneFile f = New("Лёгкий пакет с разнесением — тип «Ariete» (реконструкция)", "Сборка полегче остальных: высокотвёрдая щека, отражающий пакет, разнесение, второй пакет и алюминий за ними. Механизм — не толщина, а порядок и пустота: тот же приём, что в разнесённой преграде, только с метаемыми пластинами между листами. ⚠ Прослойка первого пакета взята неопреновой: полиуретан и неопрен различаются ударной адиабатой, но чисел выстрела этот выбор в модели НЕ меняет — импульса по материалам прослойки в литературе нет вовсе.", VehicleProvenance, "Ariete (C1)");
		Stack s = new Stack(f, FirstFrontZM).Tilt(35f, 20f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.HHA, 40f, 0f);
		SetNera(s.Add(ArmorPlate.ObjectKind.Nera, ArmorPlate.ArmorType.RHA, NeraStackMM(NeraNeoprene), 0f, NeraPlanM, NeraPlanM), NeraNeoprene);
		s.Gap(50f);
		SetNera(s.Add(ArmorPlate.ObjectKind.Nera, ArmorPlate.ArmorType.RHA, NeraStackMM(NeraThin), 0f, NeraPlanM, NeraPlanM), NeraThin);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.Aluminum, 40f, 0f);
		s.Add(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, 80f, 0f);
		Liner(s, 15f);
		f.projectile = Rod();
		return f;
	}

	private static void Liner(Stack s, float thicknessMM)
	{
		SavedObject o = s.Add(ArmorPlate.ObjectKind.SpallLiner, ArmorPlate.ArmorType.RHA, thicknessMM, 0f);
		o.linerMaterial = SpallLinerMaterial.Aramid.ToString();
	}

	private static void SetEra(SavedObject o, float tpMM, float vpMS, float gapMM, float extentMM)
	{
		o.eraCassetteExtentMM = extentMM;
		o.eraPlateThicknessMM = tpMM;
		o.eraPlateVelocityMS = vpMS;
		o.eraInterlayerGapMM = gapMM;
	}

	private static void SetNera(SavedObject o, string presetId)
	{
		if (!NeraCatalog.TryGet(presetId, out NeraPreset p))
		{
			Debug.LogError("ScenePresets: варианта НДЗ «" + presetId + "» нет в NeraCatalog — пакет собран не будет. Проверь ключ.");
			return;
		}
		o.neraFrontThicknessMM = p.frontMM;
		o.neraRearThicknessMM = p.rearMM;
		o.neraInterlayerMM = p.interlayerMM;
		o.neraMountAngleDeg = p.mountAngleDeg;
		o.neraExtentMM = p.extentMM;
		o.neraImpulsePerArea = 4900f;
		o.neraFrontMaterial = p.frontMaterial.ToString();
		o.neraRearMaterial = p.rearMaterial.ToString();
		o.neraInterlayer = p.interlayer.ToString();
	}

	private static float NeraStackMM(string presetId)
	{
		return NeraCatalog.TryGet(presetId, out NeraPreset p) ? p.frontMM + p.interlayerMM + p.rearMM : 0f;
	}

	public static List<SceneFile> All()
	{
		List<SceneFile> result = new List<SceneFile>
		{
			Monolith(50f),
			Monolith(100f),
			Monolith(200f),
			Spaced(),
			SandwichCeramic(),
			SandwichTextolite(),
			NeraArray(),
			EraOverComposite(),
			ReconWesternComposite(),
			ReconEasternEraOverComposite(),
			ReconCastTurretWithNera(),
			ReconUraniumComposite(),
			ReconWedgeAndBox(),
			ReconCeramicBehindNera(),
			ReconLightMultilayer(),
			ObliquePack3030()
		};
		if (Catalog.IsKindAvailable(ArmorPlate.ObjectKind.Slat))
		{
			result.Insert(8, SlatOverThin());
		}
		return result;
	}

	public static List<SceneFile> AllIncludingWithdrawn()
	{
		List<SceneFile> result = All();
		if (!Catalog.IsKindAvailable(ArmorPlate.ObjectKind.Slat))
		{
			result.Add(SlatOverThin());
		}
		return result;
	}

	public static bool TryGet(string presetName, out SceneFile f)
	{
		foreach (SceneFile row in AllIncludingWithdrawn())
		{
			if (row.name == presetName)
			{
				f = row;
				return true;
			}
		}
		f = null;
		return false;
	}
}
