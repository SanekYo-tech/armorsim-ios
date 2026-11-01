using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class AnchorReport
{
	public sealed class DcResult
	{
		public bool exists;

		public double meanM;

		public double sdM;

		public int bands;

		public int skipped;

		public float zLoM;

		public float zHiM;

		public float widthM;

		public string naReason;
	}

	public const string DcRefTag = "FITTED (внешняя, корреляция Walker/Anderson, провенанс не установлен)";

	public static readonly float[] RefV0;

	public static readonly double[] RefRate;

	public const double EtalonWearContract = 0.28119177;

	public const double EtalonWearToleranceRel = 0.1;

	public const double RefDcSlopePercent = 16.8;

	public const double RefPLSlopePercent = 95.1;

	public static double RefSlopePercent => (RefRate[1] - RefRate[0]) / RefRate[0] * 100.0;

	public static float DcRefM(ContinuumScenario s)
	{
		return (s.impactVelocityMps * 0.7f * 0.001f + 1f) * s.rodDiameterM;
	}

	public static void Invariants()
	{
		Debug.Log("отпечаток решателя  : " + CT.SolverFingerprint() + "   [ПЕРЕНЕСЁН: источник — CT.SolverFingerprint()]");
		Debug.Log("контрольная сумма   : " + ViewerFixtures.Checksum(ViewerFixtures.EtalonTimeline()) + "   [ПЕРЕНЕСЁН: источник — ViewerFixtures.EtalonTimeline()]");
	}

	public static void Print(string fixtureName, AnchorRun.Result r, string fingerprint, string checksum)
	{
		ContinuumScenario s = r.scenario;
		float h = s.ElementSizeM;
		float dcRefM = DcRefM(s);
		DcResult dc = ComputeDc(r, dcRefM);
		r.dcMeasuredM = dc.exists ? dc.meanM : double.NaN;
		if (s.targetKind == ContinuumTargetKind.SemiInfinite)
		{
			r.guard = ClearanceGuard.Evaluate(s, r.crater, dcRefM, r.dcMeasuredM);
			r.blockTooSmall = r.guard.tripped;
		}
		Debug.Log("");
		Debug.Log("╔═══════════════════════════════════════════════════════════════════════════");
		Debug.Log("║ ОТЧЁТ 12b — " + fixtureName);
		Debug.Log("╚═══════════════════════════════════════════════════════════════════════════");
		Debug.Log("отпечаток решателя  : " + fingerprint + "   [ПЕРЕНЕСЁН, не из этого прогона]");
		Debug.Log("контрольная сумма   : " + checksum + "   [ПЕРЕНЕСЁН, не из этого прогона]");
		Debug.Log("постановка          : " + s.name);
		Debug.Log(string.Format("                      V₀ = {0:F0} м/с, стержень {1} ", s.impactVelocityMps, s.rodMaterial) + string.Format("L {0:F0} мм × D {1:F0} мм (L/D = ", s.rodLengthM * 1000f, s.rodDiameterM * 1000f) + string.Format("{0:F1}), нос {1}", s.rodLengthM / s.rodDiameterM, s.nose));
		Debug.Log(string.Format("                      преграда {0}, {1}, глубина ", s.targetMaterial, s.targetKind) + string.Format("{0:F0} мм, внешний радиус ", s.targetThicknessM * 1000f) + string.Format("{0:F0} мм", s.outerRadiusCalibres * s.rodDiameterM * 1000f));
		Debug.Log(string.Format("                      h = D/{0:F0} = {1:F4} мм, ", s.elementsPerDiameter, h * 1000f) + string.Format("элементов {0}, шагов {1}, ", r.elementsInitial, r.steps) + string.Format("модельное время {0:F1} мкс", r.finalTimeS * 1000000f));
		Debug.Log(string.Format("                      ε_erode = {0:F2}, Q_HG = {1:F4}, ", s.erodeEpsP, s.hourglassQ) + string.Format("ворота удаления {0}", s.asbDeletionPressureGate));
		Debug.Log(string.Format("время работы        : {0:F1} с", r.wallSeconds));
		Debug.Log(string.Format("D_c_ref             : {0:F2} мм — {1}", dcRefM * 1000f, DcRefTag));
		Debug.Log("                      = (1 + 0.7·V₀[км/с])·D_p = (1 + 0.7·" + string.Format("{0:F3})·{1:F1} мм", s.impactVelocityMps * 0.001f, s.rodDiameterM * 1000f));
		string warning = ContinuumMaterials.AsbSubstitutionWarning();
		if (warning != null)
		{
			Debug.LogWarning(warning);
		}
		Debug.Log(ContinuumMaterials.ErosionParameterAudit(s.erodeEpsP));
		PrintPMin(s);
		PrintClearances(r, dcRefM);
		PrintReflection(r, dcRefM);
		PrintMorphology(r, dcRefM);
		PrintDc(dc, r, dcRefM);
		PrintErosion(r, h, out int wi0, out int wi1);
		PrintDeletionDecomposition(r, wi0, wi1);
		PrintAmplitudeAndRam(r);
	}

	private static void PrintEpsPHistogram(AnchorRun.Result r)
	{
		List<SolverDiagnostics.ElementRemoval> log = r.diag.removals;
		if (log.Count == 0)
		{
			return;
		}
		float maxEps = 0f;
		for (int i = 0; i < log.Count; i++)
		{
			maxEps = Mathf.Max(maxEps, log[i].epsPAtDeath);
		}
		int bins = Math.Max(1, (int)Math.Ceiling(maxEps / 0.05f) + 1);
		int[] rod = new int[bins];
		int[] target = new int[bins];
		for (int i = 0; i < log.Count; i++)
		{
			int bin = Mathf.Clamp((int)Math.Floor(log[i].epsPAtDeath / 0.05f), 0, bins - 1);
			if (log[i].body == 0)
			{
				rod[bin]++;
			}
			else
			{
				target[bin]++;
			}
		}
		Debug.Log(string.Format("    §1.2 ε_p В МОМЕНТ СМЕРТИ (корзина {0:F2}); порог ПАС: стержень ", 0.05f) + string.Format("{0:F2}, преграда ", ContinuumMaterials.AsbDeathStrain(ContinuumMaterials.Get(r.scenario.rodMaterial))) + string.Format("{0:F2}", ContinuumMaterials.AsbDeathStrain(ContinuumMaterials.Get(r.scenario.targetMaterial))));
		Debug.Log("            ε_p        стержень   преграда");
		int peakBin = 0;
		int peakCount = 0;
		int nonempty = 0;
		for (int i = 0; i < bins; i++)
		{
			if (rod[i] > peakCount)
			{
				peakCount = rod[i];
				peakBin = i;
			}
			if (rod[i] == 0 && target[i] == 0)
			{
				continue;
			}
			Debug.Log(string.Format("        {0,5:F2}–{1,-5:F2} {2,9} {3,10}", (float)i * 0.05f, (float)(i + 1) * 0.05f, rod[i], target[i]));
			nonempty++;
		}
		Debug.Log(string.Format("    ⭐ у стержня пик в корзине {0:F2}–{1:F2}: ", (float)peakBin * 0.05f, (float)(peakBin + 1) * 0.05f) + string.Format("{0} из {1} = {2:F1} %, заполненных корзин {3}", peakCount, CountBody(log, 0), a_safe(peakCount, r) * 100.0, nonempty));
		if (a_safe(peakCount, r) > 0.9)
		{
			Debug.LogWarning("    ⚠⚠ ЭТО ПИК, А НЕ РАСПРЕДЕЛЕНИЕ: вещество снимается ПОРОГОМ, и порог и есть физика эрозии. Всё, что модель течения делает выше этой деформации, не проверяется ничем — до неё просто не доживают.");
		}
	}

	private static void PrintRadialMap(AnchorRun.Result r)
	{
		List<SolverDiagnostics.ElementRemoval> log = r.diag.removals;
		if (log.Count == 0)
		{
			return;
		}
		float r0 = r.scenario.rodDiameterM * 0.5f;
		float max = 0f;
		for (int i = 0; i < log.Count; i++)
		{
			max = Mathf.Max(max, log[i].rM / r0);
		}
		int bins = Math.Max(1, (int)Math.Ceiling(max / 0.1f) + 1);
		int[] rodCount = new int[bins];
		double[] rodMass = new double[bins];
		int[] targetCount = new int[bins];
		double[] targetMass = new double[bins];
		for (int i = 0; i < log.Count; i++)
		{
			int bin = Mathf.Clamp((int)Math.Floor(log[i].rM / r0 / 0.1f), 0, bins - 1);
			if (log[i].body == 0)
			{
				rodCount[bin]++;
				rodMass[bin] += log[i].massKg;
			}
			else
			{
				targetCount[bin]++;
				targetMass[bin] += log[i].massKg;
			}
		}
		Debug.Log(string.Format("    §B РАДИАЛЬНАЯ КАРТА УДАЛЕНИЙ (r/r₀, корзина {0:F1}; r₀ = {1:F1} мм — ", 0.1f, r0 * 1000f) + "радиус СТЕРЖНЯ для обоих тел)");
		Debug.Log("          r/r₀        стержень шт / г         преграда шт / г");
		for (int i = 0; i < bins; i++)
		{
			if (rodCount[i] == 0 && targetCount[i] == 0)
			{
				continue;
			}
			Debug.Log(string.Format("        {0,4:F1}–{1,-4:F1} {2,9} / {3,9:F2}", (float)i * 0.1f, (float)(i + 1) * 0.1f, rodCount[i], rodMass[i] * 1000.0) + string.Format("      {0,9} / {1,9:F2}", targetCount[i], targetMass[i] * 1000.0));
		}
		double rodTotal = 0.0;
		double rodFar = 0.0;
		double targetTotal = 0.0;
		double targetFar = 0.0;
		for (int i = 0; i < log.Count; i++)
		{
			bool far = log[i].rM / r0 > 0.7f;
			if (log[i].body == 0)
			{
				rodTotal += log[i].massKg;
				if (far)
				{
					rodFar += log[i].massKg;
				}
			}
			else
			{
				targetTotal += log[i].massKg;
				if (far)
				{
					targetFar += log[i].massKg;
				}
			}
		}
		Debug.Log(string.Format("    ⭐ масса при r/r₀ > 0.7 — стержень: {0:F2} г из {1:F2} г = ", rodFar * 1000.0, rodTotal * 1000.0) + string.Format("{0:F2} %;  ", rodTotal > 0.0 ? rodFar / rodTotal * 100.0 : 0.0) + string.Format("преграда: {0:F2} г из {1:F2} г = ", targetFar * 1000.0, targetTotal * 1000.0) + string.Format("{0:F2} %", targetTotal > 0.0 ? targetFar / targetTotal * 100.0 : 0.0));
		if (rodTotal > 0.0 && rodFar / rodTotal < 0.05)
		{
			Debug.LogWarning("    ⚠⚠ ГИБЕЛЬ СТЕРЖНЯ ПРАКТИЧЕСКИ ЦЕЛИКОМ ПРИОСЕВАЯ: периферийного съедания у плеча нет. Морфология самозатачивания в такой картине НЕ ВОСПРОИЗВОДИМА ни при каком epsASB — итерации 13 нужна переспецификация, а не константы урана.");
		}
	}

	private static double a_safe(int peak, AnchorRun.Result r)
	{
		int count = CountBody(r.diag.removals, 1);
		return count < 1 ? 0.0 : (double)peak / count;
	}

	private static int CountBody(List<SolverDiagnostics.ElementRemoval> log, byte body)
	{
		int count = 0;
		for (int i = 0; i < log.Count; i++)
		{
			if (log[i].body == body)
			{
				count++;
			}
		}
		return count;
	}

	private static void PrintAmplitudeAndRam(AnchorRun.Result r)
	{
		Debug.Log("");
		Debug.Log("── §2.3 РАЗМАХ ДАВЛЕНИЯ, ТАРАН, СКОРОСТЬ ──");
		if (r.fastestNode < 0)
		{
			Debug.Log("    быстрейший узел не найден");
			return;
		}
		Debug.Log(string.Format("    быстрейший узел стержня    : {0:F1} м/с на ", r.fastestSpeed) + string.Format("{0:F1} мкс, (r = {1:F2} мм, ", r.fastestTimeS * 1000000f, r.fastestR * 1000f) + string.Format("z = {0:F2} мм)", r.fastestZ * 1000f));
		Debug.Log(string.Format("    v_max / v₀                 : {0:F4}  ", r.fastestSpeed / r.scenario.impactVelocityMps) + string.Format("(числитель {0:F1} м/с, знаменатель {1:F0} м/с)", r.fastestSpeed, r.scenario.impactVelocityMps));
		Debug.Log(string.Format("    размах у быстрейшего узла  : {0:F3} ГПа ", r.amplitudePa * 1E-09f) + string.Format("(max {0:F3}, min {1:F3} ГПа) ", r.ampMaxPa * 1E-09f, r.ampMinPa * 1E-09f) + string.Format("по {0} элементам", r.ampElements));
		Debug.LogWarning("    ⚠⚠ ЭТА СТРОКА НЕ КОНТРОЛИРУЕМА и между конфигурациями НЕ СРАВНИВАЕТСЯ: место пробы выбирает сам прогон. В 12b «быстрейший узел» оказался разными элементами (z 80.88 мм @105.6 мкс против 99.66 мм @125.5 мкс). Сравнивать — по двум строкам ниже.");
		Debug.Log(string.Format("    §3.2 размах на ЗАКРЕПЛЁННОМ элементе #{0} ", r.fixedElem) + "(приосевой, четверть длины от носка, выбран ДО первого шага): " + string.Format("{0:F3} ГПа ", r.fixedAmplitudePa * 1E-09f) + string.Format("(max {0:F3}, min {1:F3} ГПа)", r.fixedMaxPa * 1E-09f, r.fixedMinPa * 1E-09f));
		Debug.Log(string.Format("    §3.2 99-й процентиль размаха по {0} живым элементам стержня: ", r.amplitudeSamples) + string.Format("{0:F3} ГПа", r.amplitudeP99Pa * 1E-09f));
		Debug.Log("    ⚠ СУДИТЬ ПО РАЗМАХУ, А НЕ ПО ПИКУ: пол, срезавший пик и оставивший колебание прежним, проблему спрятал, а не решил.");
		Debug.Log("    ⚠ Закреплённый элемент сравним между конфигурациями С ТОЙ ЖЕ СЕТКОЙ И ТЕМ ЖЕ НОСОМ. У конуса нумерация другая (часть элементов не создана обрезкой), поэтому конус с тупым по этой строке НЕ сверяется — только по процентилю.");
		Debug.Log(string.Format("    ram = max(P+q)/P_удара     : {0:F3}  ", r.diag.RamRatio) + string.Format("(числитель {0:F2} ГПа, знаменатель ", r.diag.peakCompressionPa * 1E-09f) + string.Format("{0:F2} ГПа)", r.diag.impactPressurePa * 1E-09f));
		if (r.diag.targetTensionProbePa >= 0f)
		{
			return;
		}
		Debug.Log(string.Format("    §2.2 преграда ниже {0:F2} ГПа: ", r.diag.targetTensionProbePa * 1E-09f) + string.Format("{0} элементов, ", r.diag.targetElemsBelowRodFloor) + string.Format("{0:F2} г — столько вещества ", r.diag.targetMassBelowRodFloorKg * 1000f) + "преграды затронула бы недостающая константа, если бы её нашли");
	}

	private static void PrintPMin(ContinuumScenario s)
	{
		ContinuumProperties rod = ContinuumMaterials.Get(s.rodMaterial);
		Debug.Log("пол откола: с S19 это ВХОД МОДЕЛИ РАЗРУШЕНИЯ Super. В L1 он не действует — зажим растяжения в уравнении состояния удалён, порога у этого трека нет.");
		string line;
		if (rod.spallPa < 0f)
		{
			line = string.Format("пол ({0})       : {1:F2} ГПа — MEASURED ", s.rodMaterial, rod.spallPa * 1E-09f) + "(откольная прочность КОВАНОГО WHA 1.9 ± 0.4 ГПа)";
			Debug.Log(line);
			Debug.LogWarning("                      ⚠ РАЗБРОС ДВУКРАТНЫЙ И ОН ЗДЕСЬ, А НЕ В СНОСКЕ: ОТОЖЖЁННЫЙ WHA даёт ниже 1.0 ГПа. Стержень эталона задан КОВАНЫМ; если это условие когда-нибудь ослабят, константа меняется вдвое.");
			ContinuumProperties target = ContinuumMaterials.Get(s.targetMaterial);
			if (target.spallPa < 0f)
			{
				line = string.Format("пол ({0}) : {1:F2} ГПа — у преграды пол ", s.targetMaterial, target.spallPa * 1E-09f) + "ЕСТЬ и он свой; §2.2 всё равно меряет по полу СТЕРЖНЯ, и это надо читать как сравнение с чужим порогом, а не как свойство преграды.";
			}
			else
			{
				line = string.Format("пол ({0}) : НЕ ЗАДАН СОЗНАТЕЛЬНО — источника по откольной ", s.targetMaterial) + "прочности RHA в реестре проекта нет. Заимствовать число стержня ЗАПРЕЩЕНО; вместо этого §2.2 меряет, сколько вещества преграды ушло ниже пола стержня.";
			}
		}
		else
		{
			line = string.Format("пол ({0})       : не задан — источника нет", s.rodMaterial);
		}
		Debug.Log(line);
	}

	private static void PrintClearances(AnchorRun.Result r, float dcRefM)
	{
		if (r.scenario.targetKind != ContinuumTargetKind.SemiInfinite)
		{
			Debug.Log("запасы до границ    : не применимо — у плиты закреплён только внешний радиус, тыльная грань СВОБОДНА по построению");
			return;
		}
		ClearanceGuard g = r.guard;
		Debug.Log(string.Format("запас осевой        : {0:F2} мм = {1:F2}·D_c_ref ", g.axialM * 1000f, g.axialInDcRef) + string.Format("(глубина блока {0:F0} мм − P_f ", r.scenario.targetThicknessM * 1000f) + string.Format("{0:F2} мм)", r.crater.floorDepthM * 1000f));
		Debug.Log(string.Format("запас радиальный    : {0:F2} мм = {1:F2}·D_c_ref", g.radialM * 1000f, g.radialInDcRef) + (g.radialFromMaxRadius ? " ⚠ по НАИБОЛЬШЕМУ радиусу кратера: D_c к этой постановке не применима" : ""));
		Debug.Log(string.Format("                      требуется не менее {0:F0}", 2f) + "·D_c_ref по обоим");
		if (g.tripped)
		{
			Debug.LogError("⛔⛔ BLOCK_TOO_SMALL — ВЕЛИЧИНЫ ПРОГОНА НЕДЕЙСТВИТЕЛЬНЫ.");
			Debug.LogError("    " + g.reason);
		}
	}

	private static void PrintReflection(AnchorRun.Result r, float dcRefM)
	{
		ContinuumScenario s = r.scenario;
		if (s.targetKind != ContinuumTargetKind.SemiInfinite)
		{
			Debug.Log("t_отражения         : не применимо — тыльная грань плиты свободна");
			return;
		}
		float waveSpeed = ContinuumMaterials.Get(s.targetMaterial).LongitudinalWaveSpeed;
		float reflectionS = 2f * s.targetThicknessM / waveSpeed;
		Debug.Log(string.Format("t_отражения         : {0:F1} мкс = 2·{1:F0} мм / ", reflectionS * 1000000f, s.targetThicknessM * 1000f) + string.Format("{0:F0} м/с (c_L преграды)", waveSpeed));
		int erosionIndex = r.lengths.FirstAtOrBelow(0.3f);
		float craterFloor = r.crater.floorDepthM;
		float dcTime = -1f;
		for (int i = 0; i < r.lengths.tipZM.Count; i++)
		{
			if (r.lengths.tipZM[i] >= craterFloor - dcRefM)
			{
				dcTime = r.lengths.timeS[i];
				break;
			}
		}
		double overlap = -1.0;
		string window = "";
		if (erosionIndex >= 0)
		{
			float time = r.lengths.timeS[erosionIndex];
			if (time > reflectionS)
			{
				overlap = time - reflectionS;
				window = "эрозии";
			}
		}
		if (dcTime > 0f && overlap < dcTime - reflectionS)
		{
			overlap = dcTime - reflectionS;
			window = "D_c";
		}
		if (overlap > 0.0)
		{
			Debug.LogWarning("                      ⚠ REFLECTION_OVERLAP: окно " + window + " перекрывает " + string.Format("приход отражения на {0:F1} мкс — ТРЕБОВАНИЕ ВИДИМОСТИ. ", overlap * 1000000.0) + "⚠ 12a замерила, что плоскостность стенки за перекрытием НЕ следует (1250 на 350 мм: перекрытие больше, плоскостность лучше), поэтому в стеночных величинах отражение пока не обвиняется.");
		}
		else
		{
			Debug.Log("                      перекрытия нет: оба окна закрываются до прихода отражения");
		}
	}

	private static void PrintMorphology(AnchorRun.Result r, float dcRefM)
	{
		CraterProfile crater = r.crater;
		float h = r.scenario.ElementSizeM;
		Debug.Log("");
		Debug.Log("── §1.4 МОРФОЛОГИЯ (печатается безусловно) ──");
		if (crater.radius.Length == 0)
		{
			Debug.Log("    живого вещества преграды не осталось — профиль пуст");
			return;
		}
		int last = Math.Min(crater.radius.Length - 1, (int)Math.Floor(crater.floorDepthM / h + 0.5f) - crater.kMin + 3);
		Debug.Log(string.Format("    профиль r_кратера(z), шаг полосы h = {0:F4} мм; глубже дна ", h * 1000f) + string.Format("r_кратера ≡ 0 по построению, {0} полос не печатаются", Math.Max(0, crater.radius.Length - last - 1)));
		Debug.Log("         z, мм    r_кратера, мм   D_c(z), мм");
		for (int i = 0; i <= last; i++)
		{
			Debug.Log(string.Format("    {0,10:F2} {1,14:F3} {2,12:F3}", (crater.kMin + i) * crater.h * 1000f, crater.radius[i] * 1000f, 2f * crater.radius[i] * 1000f) + (crater.hasMaterial[i] ? "" : "   вещества нет"));
		}
		Debug.Log(string.Format("    P_f (дно кратера)          : {0:F2} мм", crater.floorDepthM * 1000f));
		Debug.Log(string.Format("    P_f / D_c_ref              : {0:F4}  ", crater.floorDepthM / dcRefM) + string.Format("(числитель P_f = {0:F2} мм, знаменатель D_c_ref = {1:F2} мм)", crater.floorDepthM * 1000f, dcRefM * 1000f));
		Debug.Log(string.Format("    наибольший радиус при z>0  : {0:F3} мм на z = ", crater.maxRadiusM * 1000f) + string.Format("{0:F2} мм", crater.maxRadiusAtZM * 1000f));
		Debug.Log(string.Format("    навал над лицевой гранью   : {0:F3} мм ", Mathf.Max(0f, -crater.frontMostZM) * 1000f) + string.Format("(самая передняя точка преграды z = {0:F3} мм)", crater.frontMostZM * 1000f));
		Debug.Log(string.Format("    связных кусков преграды    : {0}", crater.componentCount));
		Debug.Log(string.Format("    вне граничной компоненты   : {0} элементов, ", crater.detachedElements) + string.Format("{0:F2} г — это ВЫБРОС И ОТКОЛ, морфология, ничего не гейтит", crater.detachedMassKg * 1000f));
		Debug.Log("    ⚠ r_кратера считается ТОЛЬКО по компоненте, связанной с внешней границей блока: оторвавшийся кусок исключается точно. Пометка SUSPECT итерации 12a ОТОЗВАНА — её порог совпадал с типичным значением и не значил ничего.");
		if (crater.floorSaturated)
		{
			Debug.LogWarning("    ⚠ P_f НАСЫЩЕНА ГЕОМЕТРИЕЙ: дно совпало с последней полосой с веществом — преграда пройдена насквозь. Величину задаёт толщина преграды, а не решатель.");
		}
		if (r.diag.semiInfiniteViolated != null)
		{
			Debug.LogWarning("    ⛔ " + r.diag.semiInfiniteViolated + "  [второй, независимый от §1.3 сторож — оставлен намеренно]");
		}
	}

	private static DcResult ComputeDc(AnchorRun.Result r, float dcRefM)
	{
		DcResult d = new DcResult();
		CraterProfile crater = r.crater;
		float h = r.scenario.ElementSizeM;
		d.zLoM = dcRefM * 1.5f;
		d.zHiM = crater.floorDepthM - dcRefM;
		d.widthM = d.zHiM - d.zLoM;
		if (d.widthM < dcRefM * 0.5f)
		{
			d.naReason = string.Format("установившегося участка нет — P_f = {0:F2} мм, ", crater.floorDepthM * 1000f) + string.Format("D_c_ref = {0:F2} мм, P_f/D_c_ref = {1:F4}, ", dcRefM * 1000f, crater.floorDepthM / dcRefM) + string.Format("окно = {0:F4} · D_c_ref (требуется не менее 0.5)", d.widthM / dcRefM);
			return d;
		}
		int i0 = Math.Max(0, (int)Math.Floor(d.zLoM / h + 0.5f) - crater.kMin);
		int i1 = Math.Min(crater.radius.Length - 1, (int)Math.Floor(d.zHiM / h + 0.5f) - crater.kMin);
		double sum = 0.0;
		for (int i = i0; i <= i1; i++)
		{
			if (!crater.hasMaterial[i] || crater.radius[i] <= 0f)
			{
				d.skipped++;
				continue;
			}
			d.bands++;
			sum += 2.0 * crater.radius[i];
		}
		if (d.bands > 1)
		{
			d.meanM = sum / d.bands;
			double variance = 0.0;
			for (int i = i0; i <= i1; i++)
			{
				if (crater.hasMaterial[i] && crater.radius[i] > 0f)
				{
					double delta = 2.0 * crater.radius[i] - d.meanM;
					variance += delta * delta;
				}
			}
			d.sdM = Math.Sqrt(variance / d.bands);
			d.exists = true;
			return d;
		}
		d.naReason = string.Format("в окне осталось {0} годных полос (отброшено {1}) — ", d.bands, d.skipped) + "среднее по одной полосе не есть замер";
		return d;
	}

	private static void PrintDc(DcResult d, AnchorRun.Result r, float dcRefM)
	{
		Debug.Log("");
		Debug.Log("── §1.5 ШИРИНА КАНАЛА D_c ──");
		Debug.Log(string.Format("    окно: z_lo = 1.5·D_c_ref = {0:F2} мм, ", d.zLoM * 1000f) + string.Format("z_hi = P_f − 1.0·D_c_ref = {0:F2} мм, ширина {1:F2} мм", d.zHiM * 1000f, d.widthM * 1000f));
		if (!d.exists)
		{
			Debug.Log("    D_c = N/A");
			Debug.Log("    причина: " + d.naReason);
			Debug.Log("    ⚠ окно НЕ расширяется, подмена на максимум радиуса или среднее по всему каналу НЕ делается: прибор, к постановке не применимый, печатает N/A с причиной.");
			return;
		}
		Debug.Log(string.Format("    D_c                        : {0:F3} мм (полос в окне {1}, ", d.meanM * 1000.0, d.bands) + string.Format("отброшено {0})", d.skipped));
		Debug.Log(string.Format("    D_c / D_p                  : {0:F4}  ", d.meanM / r.scenario.rodDiameterM) + string.Format("(числитель D_c = {0:F3} мм, знаменатель D_p = ", d.meanM * 1000.0) + string.Format("{0:F1} мм)", r.scenario.rodDiameterM * 1000f));
		Debug.Log(string.Format("    плоскостность стенки       : коэффициент вариации {0:F2} % ", d.sdM / d.meanM * 100.0) + string.Format("при абсолютном СКО {0:F3} мм", d.sdM * 1000.0));
		Debug.Log("    ⚠ плоскостность ПЕЧАТАЕТСЯ, а не судит.");
	}

	private static void PrintErosion(AnchorRun.Result r, float h, out int wi0, out int wi1)
	{
		wi0 = -1;
		wi1 = -1;
		RodLengthSeries lengths = r.lengths;
		ContinuumScenario s = r.scenario;
		float minLength = float.MaxValue;
		for (int i = 0; i < lengths.lengthM.Count; i++)
		{
			minLength = Mathf.Min(minLength, lengths.lengthM[i]);
		}
		double minFrac = lengths.L0M > 1E-09f ? minLength / lengths.L0M : 0.0;
		Debug.Log("");
		Debug.Log("── §2 СКОРОСТЬ СРАБАТЫВАНИЯ СТЕРЖНЯ dL/dt ──");
		Debug.Log(string.Format("    отсчётов L_R (раз в 1 мкс): {0}, L₀ (замеренная) = ", lengths.timeS.Count) + string.Format("{0:F2} мм при заявленной {1:F1} мм", lengths.L0M * 1000f, s.rodLengthM * 1000f));
		PrintWear(r, minFrac);
		if (minFrac > 0.95)
		{
			Debug.Log("    |dL/dt|/V₀ = N/A");
			Debug.Log(string.Format("{0:F4}·L₀ выше 0.95·L₀. Наклон не печатается ВООБЩЕ: подгонка по ", minFrac) + "почти горизонтальному ряду мерила бы шум.");
			Debug.Log("    причина: срабатывание стержня ниже порога прибора — минимум ");
			return;
		}
		if (minFrac > 0.7)
		{
			int minIndex = lengths.IndexOfMin();
			Debug.Log("    |dL/dt|/V₀ = N/A");
			Debug.Log(string.Format("    причина: срабатывание стержня ниже окна — минимум {0:F4}·L₀ = ", minFrac) + string.Format("{0:F2} мм на {1:F1} мкс, окно §2.2 ", lengths.lengthM[minIndex] * 1000f, lengths.timeS[minIndex] * 1000000f) + "требует 0.30·L₀ и не закрывается");
			int i0 = lengths.FirstAtOrBelow(0.95f);
			int i1 = lengths.FirstAtOrBelow(0.85f);
			if (i1 < 0)
			{
				Debug.Log(string.Format("    скобка расхода [0.95; 0.85]·L₀: НЕ ЗАКРЫЛАСЬ (минимум {0:F4}·L₀) ", minFrac) + "⇒ наклон не печатается вовсе");
				return;
			}
			if (!lengths.Fit(i0, i1, out double slope, out double residual, out int n))
			{
				return;
			}
			Debug.Log(string.Format("    СКОБКА РАСХОДА [0.95; 0.85]·L₀: наклон {0:F2} м/с, ", slope) + string.Format("|dL/dt|/V₀ = {0:F4}, отсчётов {1}, ", Math.Abs(slope) / s.impactVelocityMps, n) + string.Format("окно [{0:F1}; {1:F1}] мкс, ", lengths.timeS[i0] * 1000000f, lengths.timeS[i1] * 1000000f) + string.Format("СКО невязки {0:F3} мм", residual * 1000.0));
			Debug.Log("    ⚠ С ОПОРОЙ §2.3 НЕ СВЕРЯЕТСЯ: опора описывает участок 0.90→0.30, до которого плита не доходит. Величина сравнима ТОЛЬКО с такой же скобкой.");
			wi0 = i0;
			wi1 = i1;
			return;
		}
		int mainI0 = lengths.FirstAtOrBelow(0.9f);
		int mainI1 = lengths.FirstAtOrBelow(0.3f);
		if (lengths.Fit(mainI0, mainI1, out double mainSlope, out double mainResidual, out int mainN))
		{
			wi0 = mainI0;
			wi1 = mainI1;
			Debug.Log(string.Format("    окно §2.2                  : [{0:F1}; {1:F1}] мкс, ", lengths.timeS[mainI0] * 1000000f, lengths.timeS[mainI1] * 1000000f) + string.Format("отсчётов {0}", mainN));
			Debug.Log(string.Format("    dL/dt                      : {0:F2} м/с, СКО невязки подгонки ", mainSlope) + string.Format("{0:F4} мм", mainResidual * 1000.0));
			double ratio = Math.Abs(mainSlope) / s.impactVelocityMps;
			Debug.Log(string.Format("    |dL/dt| / V₀               : {0:F4}  ", ratio) + string.Format("(числитель |dL/dt| = {0:F2} м/с, знаменатель V₀ = {1:F0} м/с)", mainSlope, s.impactVelocityMps));
			if (mainResidual > h)
			{
				Debug.LogWarning(string.Format("    ⚠ СКО невязки {0:F3} мм превышает h = {1:F3} мм: ", mainResidual * 1000.0, h * 1000f) + "срабатывание на окне НЕ линейно, один наклон — неверная сводка");
			}
			PrintReferenceNotes(s.impactVelocityMps, ratio);
			return;
		}
		Debug.Log("    |dL/dt|/V₀ = N/A");
		Debug.Log(string.Format("    причина: в окне меньше трёх отсчётов ({0})", mainI1 - mainI0 + 1));
	}

	private static void PrintWear(AnchorRun.Result r, double minFrac)
	{
		if (r.scenario.targetKind != ContinuumTargetKind.Plate)
		{
			return;
		}
		int minIndex = r.lengths.IndexOfMin();
		double wear = 1.0 - minFrac;
		double relative = (wear - EtalonWearContract) / EtalonWearContract;
		bool inside = Math.Abs(relative) <= EtalonWearToleranceRel;
		Debug.Log(string.Format("    §1.6 износ = 1 − min(L_R)/L₀ = {0:F2} %  ", wear * 100.0) + string.Format("(min L_R = {0:F2} мм, L₀ = {1:F2} мм)", r.lengths.lengthM[minIndex] * 1000f, r.lengths.L0M * 1000f));
		Debug.Log(string.Format("         договор эталона        : {0:F2} % — ПЕРЕНЕСЁН из ", EtalonWearContract * 100.0) + "аналитического ядра (Алексеевский–Тейт, блок А), здесь НЕ замеряется");
		Debug.Log(string.Format("         отклонение             : {0:F1} % относительных — ", relative * 100.0) + (inside ? "внутри ±10 %" : "ВНЕ ±10 %"));
		Debug.Log("         ⚠ характеризация, НЕ ворота: фаза 4 (калибровка) не начата.");
	}

	private static void PrintReferenceNotes(float v0, double got)
	{
		Debug.Log("    опора §2.3 (рентгенограммы остаточной длины, WHA L/D = 12.5 в ВЫСОКОТВЁРДУЮ сталь, MEASURED из одного столбца L_R):");
		for (int i = 0; i < RefV0.Length; i++)
		{
			Debug.Log(string.Format("        V₀ = {0:F2} км/с → |dL/dt|/V₀ = {1:F3}", RefV0[i] * 0.001f, RefRate[i]));
		}
		Debug.Log("    ⚠ СВЕРКА ОДНОСТОРОННЯЯ: опорная преграда высокотвёрдая, наша RHA — МЯГЧЕ, значит наше значение обязано лежать НИЖЕ. Выше — отказ; ниже — согласуется, но НЕ подтверждает.");
		Debug.Log("    ⚠ L/D РАЗЛИЧАЕТСЯ: опора 12.5 против нашей 10. Записано, не исправлено.");
		Debug.Log("    ⚠ К 1100 м/с опора НЕ ЭКСТРАПОЛИРУЕТСЯ: две точки тренда не образуют.");
		for (int i = 0; i < RefV0.Length; i++)
		{
			if (Mathf.Abs(v0 - RefV0[i]) < 0.001f)
			{
				string line = string.Format("    ⭐ на этой скорости опора {0:F3}, получено {1:F4} — ", RefRate[i], got);
				if (RefRate[i] <= got)
				{
					line += string.Format("ВЫШЕ опоры на {0:F1} % — ОТКАЗ", (got - RefRate[i]) / RefRate[i] * 100.0);
				}
				else
				{
					line += string.Format("НИЖЕ опоры на {0:F1} %, согласуется", (RefRate[i] - got) / RefRate[i] * 100.0);
				}
				Debug.Log(line);
			}
		}
	}

	private static void PrintDeletionDecomposition(AnchorRun.Result r, int wi0, int wi1)
	{
		DeletionAccounting a = DeletionAccounting.Build(r, wi0, wi1);
		float rodDiameter = r.scenario.rodDiameterM;
		Debug.Log("");
		Debug.Log("── §1.1 РАЗЛОЖЕНИЕ УДАЛЕНИЙ ПО КРИТЕРИЯМ ──");
		Debug.Log(string.Format("    записей в журнале {0}", a.removalsLogged) + (a.removalsDropped > 0 ? string.Format(", ПРОПУЩЕНО ПО ПОТОЛКУ {0}", a.removalsDropped) : "") + string.Format("; удалений с более чем одним сработавшим критерием: {0}", a.multiCause));
		Debug.Log("    ⚠ «ЭРОЗИЯ С КОНТАКТНОЙ ГРАНИ» ОТДЕЛЬНЫМ КРИТЕРИЕМ В ЭТОМ РЕШАТЕЛЕ НЕ СУЩЕСТВУЕТ. Удаление идёт ровно через шесть бит ElementKill; контакт элементов не снимает, он лишь создаёт условия у грани. Поэтому «контактная» доля ниже разделена ПО МЕСТУ (договор замера, ASSUMED), а разбивка по шести настоящим критериям — вот она, и она ни от какого договора не зависит.");
		int[] present = new int[6];
		for (int i = 0; i < r.diag.removals.Count; i++)
		{
			byte mask = r.diag.removals[i].causeMask;
			for (int cause = 0; cause < 6; cause++)
			{
				if ((mask & (1 << cause)) != 0)
				{
					present[cause]++;
				}
			}
		}
		List<string> absent = new List<string>();
		for (int cause = 0; cause < 6; cause++)
		{
			if (present[cause] == 0)
			{
				absent.Add(ElementKill.CauseName(cause));
			}
		}
		Debug.Log("    §1.3 биты, не сработавшие НИ РАЗУ: " + (absent.Count == 0 ? "нет таких" : string.Join(", ", absent)));
		Debug.Log("        критерий              стержень: шт / г        преграда: шт / г");
		for (int cause = 0; cause < 6; cause++)
		{
			DeletionAccounting.Bucket rod = a.byCause[cause, 0];
			DeletionAccounting.Bucket target = a.byCause[cause, 1];
			if (rod.count == 0 && target.count == 0)
			{
				continue;
			}
			Debug.Log(string.Format("        {0,-20} {1,7} / {2,10:F2}", ElementKill.CauseName(cause), rod.count, rod.massKg * 1000.0) + string.Format("     {0,7} / {1,10:F2}", target.count, target.massKg * 1000.0));
		}
		Debug.Log(string.Format("        {0,-20} {1,7} / {2,10:F2}", "ИТОГО", a.byBody[0].count, a.byBody[0].massKg * 1000.0) + string.Format("     {0,7} / {1,10:F2}", a.byBody[1].count, a.byBody[1].massKg * 1000.0));
		PrintEpsPHistogram(r);
		PrintRadialMap(r);
		if (!a.windowValid)
		{
			Debug.Log("    доля по месту: N/A — окна подгонки §2.2 нет, а вне его доля мерила бы другой участок, чем наклон");
		}
		else
		{
			double share = a.CriterionShare;
			Debug.Log(string.Format("    окно §2.2: [{0:F1}; {1:F1}] мкс; ", a.windowT0 * 1000000f, a.windowT1 * 1000000f) + string.Format("правило грани: z_смерти ≥ z_носок(t) − {0:F1}·D_p", DeletionAccounting.FaceWindowCalibres));
			Debug.Log(string.Format("        с рабочей грани   : {0} шт, {1:F2} г", a.rodFaceInWindow.count, a.rodFaceInWindow.massKg * 1000.0));
			Debug.Log(string.Format("        вне рабочей грани : {0} шт, {1:F2} г", a.rodElsewhereInWindow.count, a.rodElsewhereInWindow.massKg * 1000.0));
			Debug.Log(string.Format("    ⭐ ДОЛЯ КРИТЕРИЕВ (вне грани) : {0:F2} %  ", share * 100.0) + string.Format("(числитель {0:F2} г, знаменатель {1:F2} г)", a.rodElsewhereInWindow.massKg * 1000.0, (a.rodElsewhereInWindow.massKg + a.rodFaceInWindow.massKg) * 1000.0));
			if (r.lengths.Fit(wi0, wi1, out double slope, out double residual, out int n))
			{
				slope = Math.Abs(slope);
				Debug.Log(string.Format("        |dL/dt|_итого     : {0:F2} м/с (подгонка §2.2, определение 12a)", slope));
				Debug.Log(string.Format("        |dL/dt|_грань     : {0:F2} м/с", (1.0 - share) * slope));
				Debug.Log(string.Format("        |dL/dt|_критерии  : {0:F2} м/с", share * slope));
				double rate = DeletionAccounting.RateFromMass(r, a, ContinuumMaterials.Get(r.scenario.rodMaterial).rho0);
				if (!double.IsNaN(rate))
				{
					float meanRadius = 0f;
					int radiusSamples = 0;
					for (int i = 0; i < r.lengths.timeS.Count; i++)
					{
						if (r.lengths.timeS[i] >= a.windowT0 && r.lengths.timeS[i] <= a.windowT1)
						{
							meanRadius += r.lengths.headRadiusM[i];
							radiusSamples++;
						}
					}
					if (radiusSamples > 0)
					{
						meanRadius /= radiusSamples;
					}
					Debug.Log(string.Format("        сверка пересчёта  : масса/(ρ₀·A_грани) = {0:F2} м/с при ", rate) + string.Format("среднем радиусе головы {0:F2} мм ", meanRadius * 1000f) + string.Format("(A = {0:F1} мм²)", (double)meanRadius * Math.PI * meanRadius * 1000000.0));
					Debug.Log("                            расхождение с подгонкой " + string.Format("{0:F1} % — ⚠ это НЕ ошибка наклона, а ", (rate - slope) / slope * 100.0) + "нормировка: L_R мерит осевой габарит, а масса уходит и вбок");
				}
			}
		}
		Debug.Log("");
		Debug.Log("── §1.2 КАРТА УДАЛЕНИЙ СТЕРЖНЯ ОТНОСИТЕЛЬНО ХВОСТА ──");
		if (a.rodRemovalsTotal == 0)
		{
			Debug.Log("    удалений стержня нет");
			return;
		}
		Debug.Log(string.Format("    корзина = h = {0:F3} мм; отсчёт от ХВОСТА живого стержня ", a.tailMapBinM * 1000f) + "в момент смерти (вставка между отсчётами 1 мкс, ошибка до ~0.9·h)");
		Debug.Log("        (z − хвост)/D_p     шт        г");
		int group = Math.Max(1, (int)Math.Round(rodDiameter / a.tailMapBinM));
		for (int start = 0; start < a.tailMapCounts.Length; start += group)
		{
			int count = 0;
			double mass = 0.0;
			int end = Math.Min(start + group, a.tailMapCounts.Length);
			for (int i = start; i < end; i++)
			{
				count += a.tailMapCounts[i];
				mass += a.tailMapMass[i];
			}
			if (count != 0)
			{
				Debug.Log(string.Format("        {0,6:F1} – {1,-6:F1}", start * a.tailMapBinM / rodDiameter, (start + group) * a.tailMapBinM / rodDiameter) + string.Format(" {0,8} {1,10:F2}", count, mass * 1000.0));
			}
		}
		double nearShare = a.rodMassTotal > 0.0 ? a.rodMassNearTail / a.rodMassTotal : 0.0;
		Debug.Log(string.Format("    ⭐ в пределах {0:F0}·D_p от хвоста: ", 2f) + string.Format("{0} шт из {1}, ", a.rodRemovalsNearTail, a.rodRemovalsTotal) + string.Format("{0:F2} г из {1:F2} г = ", a.rodMassNearTail * 1000.0, a.rodMassTotal * 1000.0) + string.Format("{0:F2} % массы", nearShare * 100.0));
		Debug.LogWarning("    ⚠⚠ ЭТА КАРТА ПЛОСКАЯ ПО ПОСТРОЕНИЮ И СВОЙ ВОПРОС НЕ РЕШАЕТ. Стержень срабатывается с носка, поэтому расстояние умирающего элемента до хвоста равно текущей длине L_R(t); пока длина едет 10·D_p → 0, величина ровно пробегает весь диапазон ПРИ ЛЮБОМ механизме. «Доля в пределах 2·D_p от хвоста» есть попросту «две корзины из десяти» ≈ 20 %. Различает не она, а карта от НОСКА ниже.");
		Debug.Log("");
		Debug.Log("── §1.2-бис КАРТА ОТ РАБОЧЕЙ ГРАНИ (носка) — НЕВЫРОЖДЕННЫЙ ВАРИАНТ ──");
		Debug.Log("        (носок − z)/D_p     шт        г");
		for (int start = 0; start < a.tipMapCounts.Length; start += group)
		{
			int count = 0;
			double mass = 0.0;
			int end = Math.Min(start + group, a.tipMapCounts.Length);
			for (int i = start; i < end; i++)
			{
				count += a.tipMapCounts[i];
				mass += a.tipMapMass[i];
			}
			if (count != 0)
			{
				Debug.Log(string.Format("        {0,6:F1} – {1,-6:F1}", start * a.tailMapBinM / rodDiameter, (start + group) * a.tailMapBinM / rodDiameter) + string.Format(" {0,8} {1,10:F2}", count, mass * 1000.0));
			}
		}
		Debug.Log("    ⚠ ЧТО ЭТО РАЗЛИЧАЕТ: удаления У ХВОСТА, растущие со скоростью, указывали бы на механизм, ведомый волной разгрузки (растяжение) — он масштабируется с давлением удара. Удаления, собранные у рабочей грани, указывают на контакт и ε_erode. Новой физики для этого различения не требуется.");
	}

	static AnchorReport()
	{
		RefV0 = new float[] { 1250f, 1700f };
		RefRate = new double[] { 0.605, 0.498 };
	}
}
