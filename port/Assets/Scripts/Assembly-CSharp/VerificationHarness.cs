using System;
using System.Collections.Generic;
using UnityEngine;

public class VerificationHarness : MonoBehaviour
{
	[Header("Ссылки (пусто — найдутся сами)")]
	public Projectile projectile;

	public ArmorBuilder builder;

	private Projectile Proj
	{
		get
		{
			if (projectile != null)
			{
				return projectile;
			}
			projectile = UnityEngine.Object.FindFirstObjectByType<Projectile>();
			return projectile;
		}
	}

	private ArmorBuilder Builder
	{
		get
		{
			if (builder != null)
			{
				return builder;
			}
			builder = UnityEngine.Object.FindFirstObjectByType<ArmorBuilder>();
			return builder;
		}
	}

	[ContextMenu("A1 — стержень WHA 25 мм @1600 по RHA 50 мм (вязкое раздавание + пробка)")]
	public void A1_RodDuctile()
	{
		ProjectileDefinition projectileDefinition = ProjectileLibrary.WCCore();
		projectileDefinition.coreType = Projectile.CoreType.WHA;
		projectileDefinition.diameterMM = 25f;
		projectileDefinition.rodLengthMM = 500f;
		projectileDefinition.velocityMps = 1600f;
		Setup(projectileDefinition, new(ArmorPlate.ArmorType, float)[1]
		{
			(ArmorPlate.ArmorType.RHA, 50f)
		});
		Expect("A1 — ВЯЗКОЕ РАЗДАВАНИЕ ОТВЕРСТИЯ",
			"стенки канала ГЛАДКИЕ, лепестков нет ни на входе, ни на выходе",
			string.Format("диаметр кратера ⌀{0:F1} мм ", CraterModel.BjerkeCraterRatio(1.6f) * 25f) + string.Format("(D_c/D_p = {0:F3}; прежний путь дал бы ⌀44.1 мм — шире на 14.7%)", CraterModel.BjerkeCraterRatio(1.6f)),
			string.Format("с тыла вылетает ПРОБКА ⌀{0:F1} × {1:F2} мм", CraterModel.PlugDiameterMM(25f), CraterModel.PlugThicknessMM(25f, 50f)),
			"полос адиабатического сдвига НЕТ (10-200 мкм — на экране невидимы)");
		Fire();
	}

	[ContextMenu("A2 — та же пара по RHA 150 мм (толщина пробки НЕ меняется)")]
	public void A2_PlugThicknessInvariance()
	{
		ProjectileDefinition projectileDefinition = ProjectileLibrary.WCCore();
		projectileDefinition.coreType = Projectile.CoreType.WHA;
		projectileDefinition.diameterMM = 25f;
		projectileDefinition.rodLengthMM = 500f;
		projectileDefinition.velocityMps = 1600f;
		Setup(projectileDefinition, new(ArmorPlate.ArmorType, float)[1]
		{
			(ArmorPlate.ArmorType.RHA, 150f)
		});
		Expect("A2 — ТОЛЩИНА ПРОБКИ НЕ ЗАВИСИТ ОТ ПЛИТЫ",
			string.Format("пробка та же: ⌀{0:F1} × {1:F2} мм", CraterModel.PlugDiameterMM(25f), CraterModel.PlugThicknessMM(25f, 150f)),
			"плита втрое толще, а пробка ТА ЖЕ — она последняя перемычка, а не «пробитая насквозь шайба»");
		Fire();
	}

	[ContextMenu("A3 — ГЕЙТ: лепестки на тонкой плите + жёсткий стержень (не мёртвый код)")]
	public void A3_PetalingGate()
	{
		ProjectileDefinition projectileDefinition = ProjectileLibrary.WCCore();
		projectileDefinition.coreType = Projectile.CoreType.WC;
		projectileDefinition.diameterMM = 25f;
		projectileDefinition.rodLengthMM = 120f;
		projectileDefinition.velocityMps = 900f;
		Setup(projectileDefinition, new(ArmorPlate.ArmorType, float)[1]
		{
			(ArmorPlate.ArmorType.Aluminum, 20f)
		});
		float num = CraterModel.ErosionIndicator(TateModel.WC(), TateModel.Aluminum(), 900f);
		Expect("A3 — ЛЕПЕСТКИ ДОЛЖНЫ ПОЯВИТЬСЯ (гейт жив)",
			string.Format("t/D = {0:F2} ≤ 1 И индикатор эрозии {1:F2} ≤ 0.05 ⇒ гейт открыт", 0.8f, num),
			"лепестки ТОЛЬКО на ТЫЛЬНОЙ грани, на входной их быть не должно",
			string.Format("граница эрозии WC→алюминий {0:F0} м/с, стреляем ниже неё", CraterModel.ErosionBoundaryMS(TateModel.WC(), TateModel.Aluminum())),
			"⚠ форма носа в гейт БОЛЬШЕ НЕ ВХОДИТ (шаг 21 §4.3): смена носа на Flat обязана оставить лепестки на месте — если они пропали, нос где-то снова читается расчётом");
		Fire();
	}

	[ContextMenu("E1 — ОПЕРЕНИЕ: срез о входную грань (смотреть на хвост, а не на нос)")]
	public void E1_FinShear()
	{
		ProjectileDefinition projectileDefinition = ProjectileLibrary.Default();
		projectileDefinition.diameterMM = 30f;
		projectileDefinition.rodLengthMM = 300f;
		projectileDefinition.velocityMps = 1600f;
		Setup(projectileDefinition, new(ArmorPlate.ArmorType, float)[1]
		{
			(ArmorPlate.ArmorType.RHA, 50f)
		});
		float num = RodGeometry.FinSpanM(0.03f) * 1000f;
		float num2 = CraterModel.CraterDiameterRatio(TateModel.WHA(), TateModel.RHA(), 1600f) * 30f;
		Expect("E1 — СРЕЗ ОПЕРЕНИЯ",
			string.Format("размах оперения {0:F1} мм против отверстия ⌀{1:F1} мм ⇒ перья в дыру не проходят", num, num2),
			"перья уходят ПО ОДНОМУ, пока хвост протаскивает сборку сквозь входную грань",
			"обломки — ПЛОСКИЕ пластины, остаются у входной грани и не летят за стержнем",
			"⚠ смотреть надо ПОСЛЕ пробоя: на плите 50 мм оперение отстаёт от носика на ~250 мм,",
			"   то есть доходит до брони, когда носик уже вышел с тыла (см. журнал ✂)");
		Fire();
	}

	[ContextMenu("E2 — НОС: форма видна, но на числа не влияет")]
	public void E2_NoseIsVisualOnly()
	{
		ProjectileDefinition projectileDefinition = ProjectileLibrary.Default();
		projectileDefinition.nose = NoseShape.Ogive;
		Setup(projectileDefinition, new(ArmorPlate.ArmorType, float)[1]
		{
			(ArmorPlate.ArmorType.RHA, 50f)
		});
		Expect("E2 — НОС ТОЛЬКО ВИЗУАЛЬНЫЙ",
			"у заряженного лома видно оживальное остриё; по мере эрозии торец РАСТЁТ от",
			"острия до полного калибра — это следствие геометрии, а не отдельная анимация",
			"⚠ выстрелить трижды (Flat / Conical / Ogive) и сверить ЧИСЛА в консоли:",
			"   v_res, износ и предел обязаны совпасть до последнего знака (тест 85)",
			"⚠ ЭТО ВЕРНО ТОЛЬКО ДЛЯ ЭТОГО СНАРЯДА: у WHA-стержня s = 1 с первого кадра.",
			"   У стального бронебойного (жёсткая ветка) нос МЕНЯЕТ глубину — см. блок B");
		Fire();
	}

	[ContextMenu("B1 — КОНТРАСТ: стержень по RHA 100 мм")]
	public void B1_ContrastRod()
	{
		ProjectileDefinition projectileDefinition = ProjectileLibrary.WCCore();
		projectileDefinition.coreType = Projectile.CoreType.WHA;
		projectileDefinition.diameterMM = 25f;
		projectileDefinition.rodLengthMM = 500f;
		projectileDefinition.velocityMps = 1600f;
		Setup(projectileDefinition, new(ArmorPlate.ArmorType, float)[1]
		{
			(ArmorPlate.ArmorType.RHA, 100f)
		});
		Expect("B1 — СТЕРЖЕНЬ (снять кадр и сравнить с B2)",
			string.Format("канал ⌀{0:F1} мм, ПОЧТИ ПОСТОЯННЫЙ по длине", CraterModel.BjerkeCraterRatio(1.6f) * 25f),
			"стенки гладкие (вязкое раздавание), с тыла пробка",
			"осколков МНОГО — объёмное правило от широкого канала");
		Fire();
	}

	[ContextMenu("B2 — КОНТРАСТ: кумулятивный по той же RHA 100 мм")]
	public void B2_ContrastJet()
	{
		Setup(ProjectileLibrary.ShapedCharge85(), new(ArmorPlate.ArmorType, float)[1]
		{
			(ArmorPlate.ArmorType.RHA, 100f)
		});
		Expect("B2 — СТРУЯ (тот же кадр, что B1)",
			"канал СУЖАЕТСЯ: устье ≈CD/2, дно ≈CD/6 на этой плите — конус, а не цилиндр",
			"стенки гладкие, лепестков нет, пробки нет — чистое отверстие",
			"осколков в РАЗЫ меньше, чем у стержня, и это следствие узкого канала, а не другого числа в коде",
			"ОБЕ модели выведены отдельно и никто не подгонял их «чтобы отличались»");
		Fire();
	}

	[ContextMenu("C1 — частикуляция: стандоф 482 мм (промежутки в струе видны)")]
	public void C1_Particulation()
	{
		ProjectileDefinition projectileDefinition = ProjectileLibrary.ShapedCharge85();
		projectileDefinition.fuzeStandoffMM = 482f;
		Setup(projectileDefinition, new(ArmorPlate.ArmorType, float)[1]
		{
			(ArmorPlate.ArmorType.RHA, 100f)
		});
		Expect("C1 — СТУПЕНЬКИ КАНАЛА И ПРОМЕЖУТКИ В СТРУЕ — ОДНО СОБЫТИЕ",
			"стандоф 482 мм = 5.7 CD: струя приходит УЖЕ разорванной на частицы",
			"канал углубляется ступеньками, и каждая ступенька совпадает с промежутком в струе",
			"канал ОСТАНАВЛИВАЕТСЯ на последнем вносящем элементе и дальше не растёт",
			"⚠️ рост канала виден в КОНЦЕ фазы подлёта: струя идёт 9000 м/с, корпус 900 —",
			"   она режет плиту задолго до того, как до плиты доедет тело снаряда");
		Fire();
	}

	[ContextMenu("C2 — ДЗ: кассета разбирается (короб + две пластины + прослойка)")]
	public void C2_EraAssembly()
	{
		Setup(ProjectileLibrary.ShapedCharge85(), new(ArmorPlate.ArmorType, float)[2]
		{
			(ArmorPlate.ArmorType.ERA, 30f),
			(ArmorPlate.ArmorType.RHA, 100f)
		});
		Expect("C2 — ДЗ КАК СБОРКА",
			"до выстрела внутри короба видны ДВЕ пластины и прослойка между ними",
			"при срабатывании: короб разлетается кусками, прослойка гаснет,",
			"пластины расходятся в ПРОТИВОПОЛОЖНЫЕ стороны вдоль ±нормали",
			"ни вспышки, ни дыма, ни ударной волны — только движущаяся геометрия",
			"⚠️ повторный «Огонь» обязан вернуть кассету собранной (сброс cassetteExpended)");
		Fire();
	}

	[ContextMenu("D1 — подрыв БЧ и контраст скоростей")]
	public void D1_Detonation()
	{
		ProjectileDefinition projectileDefinition = ProjectileLibrary.ShapedCharge85();
		Setup(projectileDefinition, new(ArmorPlate.ArmorType, float)[1]
		{
			(ArmorPlate.ArmorType.RHA, 100f)
		});
		Expect("D1 — ПОДРЫВ",
			string.Format("подрыв на {0:F0} мм от брони: целое тело ГАСНЕТ, вперёд уходит струя", projectileDefinition.fuzeStandoffMM),
			"⚠️ кусков корпуса быть НЕ ДОЛЖНО (spawnCasingPieces выключен — процедурные лепестки",
			"   выглядели плохо); увидели куски — значит галочку кто-то вернул",
			"⚠️ и НИЧЕГО вместо них: ни вспышки, ни спрайта, ни дыма — пустое место у точки подрыва",
			"цвет струи — чернотельный при 900 K (раскалённый металл), не «свечение»",
			"⚠️ повторный «Огонь» обязан вернуть снаряд видимым (RestoreBody)");
		Fire();
	}

	[ContextMenu("F1 — материалы на светлом поле (керамика/текстолит рядом)")]
	public void F1_Materials()
	{
		Setup(ProjectileLibrary.Default(), new(ArmorPlate.ArmorType, float)[3]
		{
			(ArmorPlate.ArmorType.Ceramic, 50f),
			(ArmorPlate.ArmorType.Textolite, 50f),
			(ArmorPlate.ArmorType.RHA, 50f)
		});
		Expect("F1 — МАТЕРИАЛЫ НА ФОНЕ #C8C8C8 (не стрелять, просто смотреть)",
			"керамика SiC — помеченный случай: читается ли она затенением после правки рига?",
			"текстолит — обязан читаться как ЯВНО НЕ МЕТАЛЛ (матовый, без блика)",
			"RHA рядом — для сравнения металл/не металл",
			"выемки и кратеры от прошлых выстрелов: тёмная полость обязана читаться на светлом поле");
		Debug.Log("[Стенд] F1: выстрел НЕ производится — это статичный кадр материалов.");
	}

	[ContextMenu("F2 — цвета результата: пробитие / остановка / рикошет")]
	public void F2_ResultColours()
	{
		Debug.Log("=== F2 — ЦВЕТА РЕЗУЛЬТАТА НА СВЕТЛОМ ПОЛЕ ===");
		Debug.Log("  фон #" + ColorUtility.ToHtmlStringRGB(ResultPalette.Background));
		Debug.Log("  пробито    #" + ColorUtility.ToHtmlStringRGB(ResultPalette.Perforated) + " (контраст к фону 3.60)");
		Debug.Log("  остановлено#" + ColorUtility.ToHtmlStringRGB(ResultPalette.Stopped) + " (4.05)");
		Debug.Log("  рикошет    #" + ColorUtility.ToHtmlStringRGB(ResultPalette.Ricochet) + " (3.09)");
		Debug.Log("  скольжение #" + ColorUtility.ToHtmlStringRGB(ResultPalette.Slide) + " (3.09)");
		Debug.Log("  выдохся    #" + ColorUtility.ToHtmlStringRGB(ResultPalette.Exhausted) + " (3.40)");
		Debug.Log("  промах     #" + ColorUtility.ToHtmlStringRGB(ResultPalette.Miss) + " (5.94)");
		Debug.Log("  Все шесть выше порога WCAG AA 3.0 для крупного текста. Вердикт — 28pt bold");
		Debug.Log("  БЕЗ подложки, поверх сцены, поэтому порог применим буквально.");
		Debug.Log("  Смотреть: читается ли КАЖДЫЙ, и различимы ли рикошет и скольжение между собой");
		Debug.Log("  (у них СПЕЦИАЛЬНО близкая яркость и разный тон).");
	}

	private void Setup(ProjectileDefinition def, (ArmorPlate.ArmorType type, float thicknessMM)[] plates)
	{
		if (Proj == null)
		{
			Debug.LogError("[Стенд] Projectile в сцене не найден.");
			return;
		}
		if (Builder == null)
		{
			Debug.LogError("[Стенд] ArmorBuilder в сцене не найден.");
			return;
		}
		Builder.ClearAll();
		for (int i = 0; i < plates.Length; i++)
		{
			Builder.AddPlate();
		}
		List<ArmorPlate> list = new List<ArmorPlate>(UnityEngine.Object.FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None));
		list.Sort(delegate(ArmorPlate a, ArmorPlate b)
		{
			return a.transform.position.z.CompareTo(b.transform.position.z);
		});
		for (int j = 0; j < plates.Length && j < list.Count; j++)
		{
			list[j].SetArmorType(plates[j].type);
			list[j].SetThickness(plates[j].thicknessMM);
		}
		Proj.ApplyDefinition(def);
		string text = string.Format("[Стенд] снаряд: {0}, D = {1:F0} мм, v = {2:F0} м/с", def.name, def.diameterMM, def.velocityMps);
		string text2 = (def.IsShapedCharge ? string.Format(", CD = {0:F0} мм, стандоф {1:F0} мм", def.coneDiameterMM, def.fuzeStandoffMM) : "");
		Debug.Log(text + text2 + string.Format("; плит {0}", plates.Length));
	}

	private static void Expect(string title, params string[] lines)
	{
		Debug.Log("=== " + title + " — ЧТО ДОЛЖНО БЫТЬ ВИДНО ===");
		for (int i = 0; i < lines.Length; i++)
		{
			Debug.Log("  • " + lines[i]);
		}
	}

	private void Fire()
	{
		if (Proj == null)
		{
			return;
		}
		if (!Application.isPlaying)
		{
			Debug.LogWarning("[Стенд] Нужен режим Play — сцена настроена, нажмите Play и «Огонь».");
			return;
		}
		Proj.Fire();
	}
}
