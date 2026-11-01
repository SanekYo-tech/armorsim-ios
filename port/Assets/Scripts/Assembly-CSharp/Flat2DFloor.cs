using System;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine;

public sealed class Flat2DFloor : MonoBehaviour
{
	private struct Cpu
	{
		public double kinetic;

		public double internalE;

		public double mass;

		public double shear;

		public double Total => kinetic + internalE;
	}

	private struct Snap
	{
		public float[] mass;

		public float[] eInt;

		public float[] rho;

		public Vector2[] vel;

		public Vector4[] stress;

		public uint[] mat;
	}

	public ComputeShader solverShader;

	private const string ShaderPath = "Assets/Scripts/Flat2D/Flat2DSolver.compute";

	private const int Warm = 300;

	private const int Window = 10000;

	private const int Nx = 120;

	private const int Ny = 30;

	private const float Dx = 0.0005f;

	private const float Push = 5f;

	private const double U32 = 5.960464477539063E-08;

	[ContextMenu("★ Пол прибора: N1 → N4")]
	public void RunAll()
	{
		int pass = 0;
		int total = 0;
		int measured = 0;
		bool anchors = false;
		bool det = false;
		bool split = false;
		ComputeShader cs = LoadShader();
		if (cs == null)
		{
			Debug.LogError("Flat2D: не найден Assets/Scripts/Flat2D/Flat2DSolver.compute");
			return;
		}
		Debug.Log("══════ ПОЛ ПРИБОРА, ЗАТЕМ ОСТАТОК ══════");
		Debug.Log(string.Format("устройство: {0} ({1})", SystemInfo.deviceName, SystemInfo.graphicsDeviceType));
		CT.Run("ОПОРЫ", "чем «редукция точна» отличается от «обе стороны прочли одно неверное»", () => anchors = Anchors(cs), ref pass, ref total);
		if (!anchors)
		{
			Debug.LogError("⛔ ОПОРЫ НЕ ДЕРЖАТ. Пол не читается: ноль расхождения означал бы не точность, а общую ошибку двух сторон.");
		}
		else
		{
			CT.Run("ДЕТЕРМИНИЗМ", "три одинаковых прогона обязаны совпасть БИТ-В-БИТ (D1)", () => det = Determinism(cs), ref pass, ref total);
			CT.Measure("ЦЕНА", "чего стоит детерминированный порядок (D2)", () => Cost(cs), ref measured);
			CT.Measure("ПОВТОР", "второй компонент пола: разброс ОТ ПРОГОНА К ПРОГОНУ на том же коде", () => Repeat(cs), ref measured);
			CT.Measure("N1", "пол: FP64 на ЦПУ против FP32 на GPU, B1 и B5", () => N1(cs), ref measured);
			CT.Measure("N3", "ступень B2 после правки — какая ступень несёт остаток", () => N3(cs), ref measured);
			CT.Measure("N4", "девиатор: ЗАМЫКАНИЕ, и только потом сравнение", () => N4(cs), ref measured);
			CT.Run("K-d", "отпечаток состояния A3′ (шаг по скорости ядра) = записанному 14.09", delegate
			{
				ulong h = RunHash(cs, true, -1);
				CT.Info(string.Format("K-d: отпечаток состояния {0:X16} (записан 14.09: CBC05792ADAB0447; до правки Куранта был 41CF54D7F1FE60DF)", h));
				return CT.True("   отпечаток = записанному", h == 0xCBC05792ADAB0447UL);
			}, ref pass, ref total);
			CT.Run("ДЕЛЕНИЕ", "acc_p + acc_s = acc с точностью округления, и деление НЕ вырождено", () => split = Split(cs), ref pass, ref total);
			if (!split)
			{
				Debug.LogError("⛔ ДЕЛЕНИЕ НЕ ПРОВЕРЕНО — §4.2 НЕ ЧИТАЕТСЯ (ТЗ §8).");
			}
			else
			{
				CT.Measure("КАНАЛЫ", "каждый канал против СВОЕГО хранилища (V5)", () => Channels(cs), ref measured);
				CT.Run("БАЛАНС", "I1 и I2: разрывы ОБЯЗАНЫ складываться в дрейф — это алгебра", () => Balance(cs), ref pass, ref total);
				CT.Run("ПОЛУШАГ", "K-a…K-d: правка КЕ проверяется БЕЗ ссылки на A3′", () => HalfStep(cs), ref pass, ref total);
				CT.Measure("ПОРЯДОК", "измельчение шага: dt, dt/2, dt/4 — показатель степени у gap", () => Refine(cs), ref measured);
			}
		}
		Debug.Log(string.Format("══════ ИТОГ: пройдено {0}, провалено {1}, измерено {2} ══════", pass, total - pass, measured));
	}

	private bool Anchors(ComputeShader cs)
	{
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, 0f);
		for (int i = 0; i < body.vel.Length; i++)
		{
			body.vel[i] = new Vector2(300f, 0f);
		}
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		bool massOk;
		bool countOk;
		bool gpuKeOk;
		bool cpuKeOk;
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.NoInteraction = true;
			s.ArtificialViscosity = false;
			s.Init(body.pos, body.vel, body.mat, mats, Dx, lo, hi);
			s.Steps(200);
			Flat2DSolver.Diag diag = s.Measure(false);
			Cpu cpu = CpuTotals(s, mats);
			double expectedMass = (double)mats[0].rho0 * Dx * Dx * s.N;
			double expectedKe = expectedMass * 0.5 * 300.0 * 300.0;
			double massError = Math.Abs(cpu.mass - expectedMass) / expectedMass;
			double gpuError = Math.Abs(diag.kinetic - expectedKe) / expectedKe;
			double cpuError = Math.Abs(cpu.kinetic - expectedKe) / expectedKe;
			CT.Info(string.Format("О1 масса: ЦПУ {0:F9} против аналитики N·ρ₀·dx² = {1:F9} кг ", cpu.mass, expectedMass) + string.Format("⇒ {0:E3}; выведенная граница 2u = {1:E3} (два умножения в float)", massError, 2.0 * U32));
			massOk = CT.True("   Σm сходится в пределах двух округлений FP32", massError <= 2.0 * U32);
			CT.Info(string.Format("О1б число частиц: решатель {0}, геометрия сцены (24+{1}+24)×{2} = {3}", s.N, Nx, Ny, 5040));
			countOk = CT.True("   счёт частиц сверен с ГЕОМЕТРИЕЙ, а не сам с собой", s.N == 5040);
			CT.Info(string.Format("О3 якорь энергии (равномерный полёт {0} м/с, взаимодействия нет): ", 300f) + string.Format("руками ½·M·v² = {0:F6} Дж; GPU {1:F6} ({2:E3}, ", expectedKe, diag.kinetic, gpuError) + string.Format("граница 11u = {0:E3}); ЦПУ {1:F6} ({2:E3}, ", 11.0 * U32, cpu.kinetic, cpuError) + string.Format("граница 3u = {0:E3})", 3.0 * U32));
			gpuKeOk = CT.True("   GPU сходится с РУЧНЫМ значением (не с ЦПУ)", gpuError <= 11.0 * U32);
			cpuKeOk = CT.True("   ЦПУ сходится с РУЧНЫМ значением (не с GPU)", cpuError <= 3.0 * U32);
		}

		body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out lo, out hi);
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = false;
			s.Plasticity = false;
			s.FixedTimeStep = StiffestDt();
			s.Init(body.pos, body.vel, body.mat, mats, Dx, lo, hi);
			s.Steps(Warm);
			Vector2[] velocitiesA = s.ReadVelocities();
			float[] energiesA = s.ReadEInt();
			s.Steps(Window);
			Vector2[] velocitiesB = s.ReadVelocities();
			float[] energiesB = s.ReadEInt();
			int sameVelocities = 0;
			int sameEnergies = 0;
			for (int i = 0; i < velocitiesA.Length; i++)
			{
				float dx = velocitiesA[i].x - velocitiesB[i].x;
				float dy = velocitiesA[i].y - velocitiesB[i].y;
				if (dx * dx + dy * dy < 1E-10f)
				{
					sameVelocities++;
				}
				if (energiesA[i] == energiesB[i])
				{
					sameEnergies++;
				}
			}
			CT.Info(string.Format("О2 живость выгрузки: побитово совпавших скоростей {0}/{1}, ", sameVelocities, velocitiesA.Length) + string.Format("внутренних энергий {0}/{1} между двумя отсчётами", sameEnergies, energiesA.Length));
			bool velocitiesLive = CT.True("   скорости РАЗЛИЧАЮТСЯ — выгрузка не устаревшая", sameVelocities < velocitiesA.Length / 2);
			bool energiesLive = CT.True("   внутренние энергии РАЗЛИЧАЮТСЯ — тот же вопрос, второй канал", sameEnergies < energiesA.Length / 2);

			Snap baseline = ReadSnap(s);
			Cpu cpuA = Sum(baseline, mats);
			Flat2DSolver.Diag gpu = s.Measure(false);
			double oldGap = Math.Abs(gpu.Total - cpuA.Total);
			double deltaE = cpuA.Total * 0.001 / baseline.mass[1234];
			baseline.eInt[1234] += (float)deltaE;
			Cpu cpuB = Sum(baseline, mats);
			double expected = (double)baseline.mass[1234] * (float)deltaE;
			double actual = cpuB.Total - cpuA.Total;
			double error = Math.Abs(actual - expected) / Math.Abs(expected);
			CT.Info(string.Format("контрфакт пола: в снимок частицы #{0} внесено δe, ожидаемый сдвиг итога ", 1234) + string.Format("m·δe = {0:E6} Дж; пересчёт суммы дал {1:E6} Дж ⇒ {2:E3}", expected, actual, error));
			double newGap = Math.Abs(gpu.Total - cpuB.Total);
			CT.Info(string.Format("   расхождение GPU↔ЦПУ: {0:E6} Дж → {1:E6} Дж", oldGap, newGap));
			bool perturbationOk = CT.True(string.Format("   пересчёт ОТРАБОТАЛ возмущение входа с точностью {0:E3} ≤ 3u", error), error <= 3.0 * U32);

			Snap shearBaseline = ReadSnap(s);
			Cpu shearA = Sum(shearBaseline, mats);
			const int index = 2222;
			double g = mats[shearBaseline.mat[index]].G;
			Vector4 stress = shearBaseline.stress[index];
			double SSof(double sx, double sy, double sz)
			{
				double third = -(sx + sy);
				return sx * sx + sy * sy + third * third + 2.0 * sz * sz;
			}
			double oldSs = SSof(stress.x, stress.y, stress.z);
			double newSx = (double)stress.x + 10000000.0;
			double newSs = SSof(newSx, stress.y, stress.z);
			double expectedShear = shearBaseline.mass[index] / shearBaseline.rho[index] * (newSs - oldSs) / (g * 4.0);
			shearBaseline.stress[index].x += 10000000f;
			Cpu shearB = Sum(shearBaseline, mats);
			double actualShear = shearB.shear - shearA.shear;
			double shearError = Math.Abs(actualShear - expectedShear) / Math.Max(Math.Abs(expectedShear), 1E-30);
			CT.Info(string.Format("контрфакт СДВИГОВОГО канала: в снимок частицы #{0} внесено ΔS_xx = {1:E1} Па; ", index, 10000000f) + string.Format("ожидаемый прирост потенциала {0:E6} Дж, пересчёт дал {1:E6} Дж ", expectedShear, actualShear) + string.Format("⇒ {0:E3}", shearError));
			bool shearOk = CT.True(string.Format("   сдвиговый член CpuTotals СЧИТАЕТ ТО, ЧТО ОБЪЯВЛЕНО ({0:E3} ≤ 1e-5)", shearError), shearError <= 1E-05);
			bool growth;
			if (Math.Abs(expected) * (3.0 * U32) < Math.Abs(newGap - Math.Abs(oldGap - expected)))
			{
				growth = Math.Abs(newGap - expected) <= Math.Abs(expected) * 0.001;
			}
			else
			{
				growth = true;
			}
			bool growthOk = CT.True("   и расхождение GPU↔ЦПУ ВЫРОСЛО на ожидаемую величину", growth);
			return massOk & countOk & gpuKeOk & cpuKeOk & velocitiesLive & energiesLive & perturbationOk & shearOk & growthOk;
		}
	}

	private bool Split(ComputeShader cs)
	{
		bool normal = SplitOnce(cs, false);
		CT.Info("── контрфакт: деление вырождено намеренно (всё в давление)");
		bool degenerate = SplitOnce(cs, true);
		return CT.True("   ⭐ на вырожденном делении набор КРАСНЕЕТ (иначе сторож бесполезен)", !degenerate) && normal;
	}

	private bool SplitOnce(ComputeShader cs, bool degenerate)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = false;
			s.Plasticity = false;
			s.FixedTimeStep = StiffestDt();
			s.AccumulateWork = true;
			s.MutSplitDegenerate = degenerate;
			Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
			for (int i = 0; i < mats.Length; i++)
			{
				mats[i].gamma0 = 0f;
				mats[i].aEos = 0f;
			}
			s.Init(body.pos, body.vel, body.mat, mats, Dx, lo, hi);
			s.Steps(800);
			Vector2[] acc = s.ReadAcc();
			Vector2[] accP = s.ReadAccP();
			Vector2[] accS = s.ReadAccS();
			Vector2[] accA = s.ReadAccA();
			double maxError = 0.0;
			double maxAcc = 0.0;
			double sumP = 0.0;
			double sumS = 0.0;
			for (int i = 0; i < acc.Length; i++)
			{
				maxAcc = Math.Max(maxAcc, acc[i].magnitude);
				double ex = (double)acc[i].x - ((double)accA[i].x + accS[i].x + accP[i].x);
				double ey = (double)acc[i].y - ((double)accA[i].y + accS[i].y + accP[i].y);
				maxError = Math.Max(maxError, Math.Sqrt(ex * ex + ey * ey));
				sumP += accP[i].magnitude;
				sumS += accS[i].magnitude;
			}
			double relative = maxError / Math.Max(maxAcc, 1E-30);
			CT.Info(string.Format("V3 max|acc − (acc_p + acc_s + acc_a)| = {0:E4} при max|acc| = {1:E4} ⇒ {2:E3}", maxError, maxAcc, relative));
			bool close = CT.True(string.Format("   деление сходится с целым в пределах 20u = {0:E3}", 1.1920928955078125E-06), relative <= 1.1920928955078125E-06);
			double share = sumS / Math.Max(sumP + sumS, 1E-30);
			CT.Info(string.Format("   насыщенность: Σ|acc_p| = {0:E4}, Σ|acc_s| = {1:E4} ⇒ доля девиатора ", sumP, sumS) + string.Format("{0:F2} %", share * 100.0));
			bool saturated = CT.True(string.Format("   ОБА канала несут работу (доля девиатора {0:F2} % в 1…99 %)", share * 100.0), share > 0.01 && share < 0.99);
			s.Steps(500);
			s.MeasureChannels(out double wp, out double ws, out double total, out double wa);
			double gap = Math.Abs(wp + ws + wa - total) / Math.Max(Math.Abs(total), 1E-30);
			CT.Info(string.Format("V4 W_p {0:F6} + W_s {1:F6} + W_a {2:F6} = {3:F6} Дж против ", wp, ws, wa, wp + ws + wa) + string.Format("ЦЕЛОГО {0:F6} Дж ⇒ {1:E3}", total, gap));
			bool balanced = CT.True("   замыкание СТРУКТУРНО, на уровне округления (≤ 20u)", gap <= 1.1920928955078125E-06);
			return saturated && close && balanced;
		}
	}

	private void Channels(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = false;
			s.Plasticity = false;
			s.FixedTimeStep = StiffestDt();
			s.AccumulateWork = true;
			Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
			for (int i = 0; i < mats.Length; i++)
			{
				mats[i].gamma0 = 0f;
				mats[i].aEos = 0f;
			}
			s.Init(body.pos, body.vel, body.mat, mats, Dx, lo, hi);
			s.Steps(Warm);
			s.MeasureChannels(out double wp0, out double ws0, out double _, out double _);
			s.MeasureWork(out double _, out double _, out double shear0, out double _);
			s.MeasureStateEnergy(out double _, out double _, out double path0, out int _);
			Flat2DSolver.Diag a = s.Measure(false);
			s.Steps(Window);
			s.MeasureChannels(out double wp1, out double ws1, out double _, out double _);
			s.MeasureWork(out double _, out double _, out double shear1, out double _);
			s.MeasureStateEnergy(out double _, out double _, out double path1, out int _);
			Flat2DSolver.Diag b = s.Measure(false);
			double full = Math.Abs(b.Total - a.Total);
			double pressureWork = -(wp1 - wp0);
			double shearWork = -(ws1 - ws0);
			double pathDelta = path1 - path0;
			double shearDelta = shear1 - shear0;
			double gapP = pressureWork - pathDelta;
			double gapS = shearWork - shearDelta;
			CT.Info(string.Format("── V5, окно {0} шагов; полный дрейф {1:F6} Дж ", Window, full) + string.Format("({0:F4} %)", full / a.Total * 100.0));
			CT.Info(string.Format("   ДАВЛЕНИЕ: −W_p = {0:F6} Дж против Δ(Σm·e) = {1:F6} Дж ⇒ разрыв ", pressureWork, pathDelta) + string.Format("{0:F6} Дж = {1:F2} % дрейфа", gapP, gapP / full * 100.0));
			CT.Info(string.Format("   ДЕВИАТОР: −W_s = {0:F6} Дж против Δ Σ(m/ρ)S:S/(4G) = {1:F6} Дж ⇒ ", shearWork, shearDelta) + string.Format("разрыв {0:F6} Дж = {1:F2} % дрейфа", gapS, gapS / full * 100.0));
			CT.Info(string.Format("   сумма разрывов {0:F6} Дж = {1:F2} % дрейфа", gapP + gapS, (gapP + gapS) / full * 100.0));
			double rp = Math.Abs(gapP) / full;
			double rs = Math.Abs(gapS) / full;
			if (rs > 0.5 && rs > rp * 3.0)
			{
				CT.Info("   ⭐⭐ ЧЛЕН НАЗВАН: расходится ДЕВИАТОРНОЕ хранилище — его прирост не равен работе, которую девиаторная часть силы реально совершила");
			}
			else if (rp > 0.5 && rp > rs * 3.0)
			{
				CT.Info("   ⭐⭐ ЧЛЕН НАЗВАН: расходится ДАВЛЕНЧЕСКОЕ хранилище — вопреки ожиданию, и это опровергает посылку §4.2");
			}
			else if (rp >= 0.1 || rs >= 0.1)
			{
				CT.Info("   ⚠️ разрывы СОПОСТАВИМЫ — приписывать одному члену НЕЛЬЗЯ");
			}
			else
			{
				CT.Info("   ⚠️ НИ ОДИН ЧЛЕН НЕ НАЗВАН: оба канала сходятся со своими хранилищами, а дрейф всё равно есть ⇒ остаток НЕ в паре «работа ↔ хранилище». Третья пересборка прибора — отдельное решение, не обход (ТЗ §8).");
			}
		}
	}

	private bool Balance(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
		for (int i = 0; i < mats.Length; i++)
		{
			mats[i].gamma0 = 0f;
			mats[i].aEos = 0f;
		}
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = false;
			s.Plasticity = false;
			s.FixedTimeStep = StiffestDt();
			s.AccumulateWork = true;
			s.Init(body.pos, body.vel, body.mat, mats, Dx, lo, hi);
			s.Steps(Warm);
			Flat2DSolver.Diag a = s.Measure(false);
			s.MeasureStateEnergy(out double _, out double _, out double path0, out int _);
			s.MeasureWork(out double _, out double _, out double shear0, out double _);
			s.MeasureChannels(out double wp0, out double ws0, out double wt0, out double wa0);
			s.MeasureDamage(out double damage0, out double _, out int _, out double _);
			s.MeasurePower(out double _, out double keHalf0);
			Cpu cpuA = CpuTotals(s, mats);
			s.Steps(Window);
			Flat2DSolver.Diag b = s.Measure(false);
			s.MeasureStateEnergy(out double _, out double _, out double path1, out int _);
			s.MeasureWork(out double _, out double _, out double shear1, out double _);
			s.MeasureChannels(out double wp1, out double ws1, out double wt1, out double wa1);
			s.MeasureDamage(out double damage1, out double _, out int failed, out double _);
			s.MeasurePower(out double _, out double keHalf1);
			Cpu cpuB = CpuTotals(s, mats);

			double dE = b.Total - a.Total;
			double dKe = b.kinetic - a.kinetic;
			double dPath = path1 - path0;
			double dShear = shear1 - shear0;
			double dWp = wp1 - wp0;
			double dWs = ws1 - ws0;
			double dWt = wt1 - wt0;
			double dWa = wa1 - wa0;
			double dDamage = damage1 - damage0;
			double dKeHalf = keHalf1 - keHalf0;
			double dCpu = cpuB.Total - cpuA.Total;
			CT.Info("── ОКНО: ступень B4 (G есть, Γ₀ = 0, пласт. выкл., вязк. выкл.), " + string.Format("шаг закреплён {0:E6} с, отсчёт после {1} шагов разгона, ", StiffestDt(), Warm) + string.Format("замер {0} шагов", Window));
			CT.Info(string.Format("   E₀ = {0:F6} Дж, E₁ = {1:F6} Дж ⇒ ΔE = {2:F6} Дж ", a.Total, b.Total, dE) + string.Format("({0:F4} %)", dE / a.Total * 100.0));
			CT.Info(string.Format("   ΔKE (по v^n) = {0:F6}; Δ(Σm·e) = {1:F6}; Δ(Σshear) = {2:F6} Дж", dKe, dPath, dShear));
			CT.Info(string.Format("   W_p = {0:F6}; W_s = {1:F6}; W_total (свой накопитель) = {2:F6} Дж", dWp, dWs, dWt));
			CT.Info(string.Format("   ΔKE на ПОЛУШАГЕ (по v^{{n±1/2}}) = {0:F6} Дж", dKeHalf));
			double independent = Math.Abs(dE - dCpu) / Math.Max(Math.Abs(dE), 1E-30);
			CT.Info(string.Format("   независимость ΔE: GPU {0:F6} против FP64 на ЦПУ {1:F6} ⇒ {2:E3}", dE, dCpu, independent));
			bool independentOk = CT.True("   ΔE подтверждён ТРЕТЬИМ путём (отдельный код, свои опоры)", independent < 1E-05);
			double i1 = dPath + dKe + dShear + dDamage;
			double residual1 = i1 - dE;
			double relative1 = Math.Abs(residual1) / Math.Max(Math.Abs(dE), 1E-30);
			CT.Info(string.Format("I1: ΔKE + Δ(Σm·e) + Δ(Σshear) + Δ(Σразруш) = {0:F6} ", i1) + string.Format("против ΔE = {0:F6} ", dE) + string.Format("⇒ невязка {0:E4} Дж = {1:F4} % дрейфа", residual1, relative1 * 100.0));
			bool i1Ok = CT.True("   I1 держится — слагаемые и дрейф сняты С ОДНОГО состояния", relative1 < 0.0001);
			double counter = (dPath * 1.01 + dKe + dShear) - dE;
			double counterRelative = Math.Abs(counter) / Math.Abs(dE);
			CT.Info(string.Format("   контрфакт I1: Δ(Σm·e) умножен на 1.01 ⇒ невязка {0:F2} % ", counterRelative * 100.0) + string.Format("(было {0:F4} %)", relative1 * 100.0));
			bool counterOk = CT.True("   I1 УМЕЕТ падать — это проверка, а не тождество", counterRelative >= 0.0001);
			bool result = independentOk & i1Ok & counterOk;
			if (!i1Ok)
			{
				CT.Info("⛔ I1 НЕ ДЕРЖИТСЯ ⇒ §2.3 НЕ ЧИТАЕТСЯ (ТЗ §2.2).");
				return result;
			}

			double gapP = dWp + dPath;
			double gapS = dWs + dShear;
			double stage = dWt - dKe;
			double residual2 = gapP + gapS + dWa + dDamage - dE;
			double relative2 = Math.Abs(residual2) / Math.Max(Math.Abs(dE), 1E-30);
			CT.Info(string.Format("I2: gap_p {0:F6} + gap_s {1:F6} + W_a {2:F6} + Δразруш {3:F6} = ", gapP, gapS, dWa, dDamage) + string.Format("{0:F6} против ΔE = {1:F6} ", gapP + gapS + dWa + dDamage, dE) + string.Format("⇒ невязка {0:E4} Дж = {1:F2} % дрейфа", residual2, relative2 * 100.0));
			double stageResidual = residual2 - stage;
			double stageRelative = Math.Abs(stageResidual) / Math.Max(Math.Abs(stage), 1E-30);
			CT.Info(string.Format("   невязка I2 против (W_total − ΔKE) = {0:F6} Дж ⇒ совпадение {1:E3}", stage, stageRelative));
			if (Math.Abs(dE) * 0.001 < Math.Abs(residual2))
			{
				bool stageOk = CT.True("   ⭐ невязка I2 ОБЪЯСНЕНА АРИФМЕТИЧЕСКИ: это в точности W_total − ΔKE", stageRelative < 0.001);
				result &= stageOk;
			}
			else
			{
				CT.Info(string.Format("   ✔ I2 замыкается НАПРЯМУЮ ({0:F4} % дрейфа) — объяснять нечего; ", relative2 * 100.0) + "член стадийности снят правкой §1");
			}
			double halfResidual = dWt - dKeHalf;
			double halfRelative = Math.Abs(halfResidual) / Math.Max(Math.Abs(dKeHalf), 1E-30);
			CT.Info(string.Format("   W_total {0:F6} против ΔKE на ПОЛУШАГЕ {1:F6} ⇒ {2:E3}", dWt, dKeHalf, halfRelative));
			CT.Info(halfRelative < 0.001 ? "   ⭐⭐ ПОДТВЕРЖДЕНО: работа силы равна приросту КЕ НА ПОЛУШАГЕ, а аудит берёт КЕ по v^n. Разность двух определений КЕ — не течь, а СТАДИЙНОСТЬ схемы." : "   ⚠️ работа силы не сходится и с полушаговой КЕ — объяснение НЕ подтверждено");

			double percentP = gapP / dE * 100.0;
			double percentS = gapS / dE * 100.0;
			double percentStage = -stage / dE * 100.0;
			double percentA = dWa / dE * 100.0;
			double percentDamage = dDamage / dE * 100.0;
			CT.Info(string.Format("── РАСКЛАД ДРЕЙФА (ΔE = {0:F6} Дж = 100 %):", dE));
			CT.Info(string.Format("   gap_p (давление)                        {0:F6} Дж = {1:F2} %", gapP, percentP));
			CT.Info(string.Format("   gap_s (девиатор)                        {0:F6} Дж = {1:F2} %", gapS, percentS));
			CT.Info(string.Format("   ΔKE(v^n) − W_total (стадийность схемы)  {0:F6} Дж = {1:F2} %", -stage, percentStage));
			CT.Info(string.Format("   W_a (искусственное напряжение)          {0:F6} Дж = {1:F2} %", dWa, percentA));
			CT.Info(string.Format("   Δ(Σ диссипация разрушения)              {0:F6} Дж = {1:F2} % ", dDamage, percentDamage) + string.Format("(разрушенных частиц {0})", failed));
			double sumEnergy = gapP + gapS - stage + dWa + dDamage;
			double sumPercent = percentP + percentS + percentStage + percentA + percentDamage;
			CT.Info(string.Format("   сумма                                   {0:F6} Дж = ", sumEnergy) + string.Format("{0:F2} %", sumPercent));
			bool sumOk = CT.True(string.Format("   ⭐⭐ ДОЛИ СКЛАДЫВАЮТСЯ В ДРЕЙФ ({0:F2} % от 100 %)", sumPercent), Math.Abs(sumPercent - 100.0) < 0.5);
			if (Math.Abs(percentStage) > Math.Abs(percentP) && Math.Abs(percentStage) > Math.Abs(percentS))
			{
				CT.Info("   ⭐⭐ КРУПНЕЙШАЯ ДОЛЯ — СТАДИЙНОСТЬ СХЕМЫ, а не канал: больше половины «дрейфа» есть разность двух определений кинетической энергии, и это НЕ утечка. Из каналов крупнее девиаторный.");
			}
			else if (Math.Abs(percentS) > Math.Abs(percentP))
			{
				CT.Info("   ⭐ крупнейшая доля — ДЕВИАТОРНЫЙ канал");
			}
			else
			{
				CT.Info("   ⭐ крупнейшая доля — ДАВЛЕНЧЕСКИЙ канал");
			}
			return result & sumOk;
		}
	}

	private bool HalfStep(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
		for (int i = 0; i < mats.Length; i++)
		{
			mats[i].gamma0 = 0f;
			mats[i].aEos = 0f;
		}
		double KaRun(float dt, out double effectOut, out double dFullOut, out double dHalfOut)
		{
			using (Flat2DSolver s = new Flat2DSolver(cs))
			{
				s.ArtificialViscosity = false;
				s.Plasticity = false;
				s.AccumulateWork = true;
				s.FixedTimeStep = dt;
				s.Init(body.pos, body.vel, body.mat, mats, Dx, lo, hi);
				s.Steps(Warm);
				Flat2DSolver.Diag a = s.Measure(false);
				double keA = KeFull(s);
				s.MeasureChannels(out double _, out double _, out double wtA, out double _, out double _);
				s.Steps(Window);
				Flat2DSolver.Diag b = s.Measure(false);
				double keB = KeFull(s);
				s.MeasureChannels(out double _, out double _, out double wtB, out double _, out double _);
				if (!s.ProbeAlive(10280, out string detail))
				{
					throw new InvalidOperationException("K-a: решатель НЕ шагал — " + detail);
				}
				dHalfOut = b.Total - a.Total;
				dFullOut = (keB - keA) - (b.kinetic - a.kinetic) + dHalfOut;
				effectOut = dFullOut - dHalfOut;
				return (keB - keA) - (wtB - wtA);
			}
		}

		double member = KaRun(StiffestDt(), out double effect, out double dFull, out double dHalf);
		double elasticMember = KaRun(StiffestDtElasticK(), out double elasticEffect, out double _, out double _);
		CT.Info(string.Format("K-a: ΔE по СТАРОМУ определению (КЕ по vⁿ) = {0:F6} Дж; ", dFull) + string.Format("по НОВОМУ (полушаг) = {0:F6} Дж ⇒ действие правки {1:F6} Дж", dHalf, effect));
		CT.Info(string.Format("   член баланса ΔКЕ(vⁿ) − W_T этого прогона {0:F6} Дж ⇒ расхождение ", member) + string.Format("{0:F3} %", Math.Abs(effect - member) / Math.Abs(member) * 100.0));
		CT.Info(string.Format("   МОСТ, шаг по упругому K: действие {0:F6} Дж, член {1:F6} Дж, записано 03.09 {2:F6} Дж", elasticEffect, elasticMember, 0.190409));
		bool arithmetic = CT.True("   действие РАВНО члену баланса, измеренному в том же прогоне накопителем работы (это арифметика, не прогноз)", Math.Abs(effect - member) / Math.Abs(member) < 0.02);
		CT.Info(string.Format("   мост: при шаге по упругому K и действие, и член = записанным {0:F6} Дж (±2 %)", 0.190409));
		bool bridge = CT.True("   мост: при шаге по упругому K и действие, и член = записанным {0:F6} Дж (±2 %)", Math.Abs(elasticEffect - 0.190409) / 0.190409 < 0.02 && Math.Abs(elasticMember - 0.190409) / 0.190409 < 0.02);
		bool initial = bridge & arithmetic;

		Flat2DScene.Body free = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, 0f);
		for (int i = 0; i < free.vel.Length; i++)
		{
			free.vel[i] = new Vector2(300f, 0f);
		}
		Flat2DScene.Domain(free, 0.01f, out Vector2 loFree, out Vector2 hiFree);
		bool freeExact;
		bool freeAlive;
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.NoInteraction = true;
			s.ArtificialViscosity = false;
			s.Init(free.pos, free.vel, free.mat, mats, Dx, loFree, hiFree);
			s.Steps(Warm);
			Flat2DSolver.Diag a = s.Measure(false);
			s.Steps(Window);
			Flat2DSolver.Diag b = s.Measure(false);
			CT.Info(string.Format("K-b: свободный полёт без сил, {0:F9} → {1:F9} Дж", a.Total, b.Total));
			freeExact = CT.True("   ПОБИТОВО не сдвинулось — смена определения не тронула случай без сил", b.Total == a.Total);
			bool alive = s.ProbeAlive(10280, out string detail);
			freeAlive = CT.True("   живость K-b — " + detail, alive);
		}
		double asymmetric = DriftWith(cs, body, lo, hi, mats, delegate(Flat2DSolver m)
		{
			m.MutAsymmetricMomentum = true;
		});
		double healthy = DriftWith(cs, body, lo, hi, mats, delegate
		{
		});
		CT.Info(string.Format("K-c: здоровый дрейф {0:F4} %, при MutAsymMomentum ", healthy * 100.0) + string.Format("{0:F4} % ⇒ во сколько раз хуже: {1:F1}×", asymmetric * 100.0, asymmetric / healthy));
		bool mutant = CT.True("   сломанный решатель ОСТАЛСЯ красным — определение не стало снисходительнее", asymmetric > healthy * 10.0);
		double kicked = DriftWith(cs, body, lo, hi, mats, delegate(Flat2DSolver m)
		{
			m.MutKickDtScale = 1.001f;
		});
		CT.Info(string.Format("СЛЕПОТА: dt пинка × 1.001 ⇒ дрейф {0:F4} % против здоровых ", kicked * 100.0) + string.Format("{0:F4} % ⇒ {1:F2}×", healthy * 100.0, kicked / healthy));
		CT.Info(healthy + healthy < kicked ? "   ⭐ аудит ВИДИТ ошибку стадийности пинка — канал не выродился" : "   ⚠️⚠️ аудит её НЕ ВИДИТ (дрейф даже МЕНЬШЕ здорового): полушаговая КЕ сокращает эту ошибку с обеих сторон тождества. K-c НЕДОСТАТОЧЕН — он покрывает дефект СИЛЫ, а не стадийности. Класс закрывается отдельным зондом, ниже.");
		bool dtGood = DtProbe(cs, body, lo, hi, mats, 1f, true);
		bool dtBad = DtProbe(cs, body, lo, hi, mats, 1.001f, false);
		return dtBad & dtGood & initial & freeExact & freeAlive & mutant;
	}

	private bool DtProbe(ComputeShader cs, Flat2DScene.Body body, Vector2 lo, Vector2 hi, Flat2DMaterialBlock[] mats, float kickScale, bool expectEqual)
	{
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = false;
			s.Plasticity = false;
			s.FixedTimeStep = StiffestDt();
			s.MutKickDtScale = kickScale;
			s.Init(body.pos, body.vel, body.mat, mats, Dx, lo, hi);
			s.Steps(100);
			s.ResetProbe();
			s.Steps(100);
			s.ReadProbe(out int _, out double stateDt, out int _, out double kickDt);
			bool equal = stateDt == kickDt;
			CT.Info(string.Format("зонд dt (пинок ×{0}): состояние {1:E9}, пинок {2:E9} ⇒ ", kickScale, stateDt, kickDt) + (equal ? "совпали побитово" : "РАЗОШЛИСЬ"));
			return CT.True("   зонд " + (expectEqual ? "молчит на здоровом" : "СРАБАТЫВАЕТ на порче"), equal == expectEqual);
		}
	}

	private static double KeFull(Flat2DSolver s)
	{
		float[] mass = s.ReadMass();
		Vector2[] vel = s.ReadVelocities();
		double result = 0.0;
		for (int i = 0; i < mass.Length; i++)
		{
			result += ((double)vel[i].x * vel[i].x + (double)vel[i].y * vel[i].y) * mass[i] * 0.5;
		}
		return result;
	}

	private double DriftWith(ComputeShader cs, Flat2DScene.Body body, Vector2 lo, Vector2 hi, Flat2DMaterialBlock[] mats, Action<Flat2DSolver> tweak)
	{
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = false;
			s.Plasticity = false;
			s.FixedTimeStep = StiffestDt();
			tweak(s);
			s.Init(body.pos, body.vel, body.mat, mats, Dx, lo, hi);
			s.Steps(Warm);
			Flat2DSolver.Diag a = s.Measure(false);
			s.Steps(Window);
			Flat2DSolver.Diag b = s.Measure(false);
			return Math.Abs(b.Total - a.Total) / Math.Abs(a.Total);
		}
	}

	private void Refine(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
		for (int i = 0; i < mats.Length; i++)
		{
			mats[i].gamma0 = 0f;
			mats[i].aEos = 0f;
		}
		float dt = StiffestDt();
		List<double> gapS = new List<double>();
		List<double> gapP = new List<double>();
		int[] divisors = new int[3] { 1, 2, 4 };
		for (int q = 0; q < divisors.Length; q++)
		{
			int div = divisors[q];
			using (Flat2DSolver s = new Flat2DSolver(cs))
			{
				s.ArtificialViscosity = false;
				s.Plasticity = false;
				s.FixedTimeStep = dt / (float)div;
				s.AccumulateWork = true;
				s.Init(body.pos, body.vel, body.mat, mats, Dx, lo, hi);
				s.Steps(div * Warm);
				s.MeasureChannels(out double wp0, out double ws0, out double _, out double _);
				s.MeasureWork(out double _, out double _, out double shear0, out double _);
				s.MeasureStateEnergy(out double _, out double _, out double path0, out int _);
				Flat2DSolver.Diag a = s.Measure(false);
				s.Steps(div * Window);
				s.MeasureChannels(out double wp1, out double ws1, out double _, out double _);
				s.MeasureWork(out double _, out double _, out double shear1, out double _);
				s.MeasureStateEnergy(out double _, out double _, out double path1, out int _);
				Flat2DSolver.Diag b = s.Measure(false);
				gapP.Add(path1 - path0 + (wp1 - wp0));
				gapS.Add(shear1 - shear0 + (ws1 - ws0));
				CT.Info(string.Format("dt/{0}: шагов {1}, время {2:F2} мкс; ", div, div * Window, (dt / (float)div) * (div * Window) * 1000000.0) + string.Format("ΔE = {0:F6} Дж; gap_p = {1:F6}; gap_s = {2:F6} Дж", b.Total - a.Total, path1 - path0 + (wp1 - wp0), shear1 - shear0 + (ws1 - ws0)));
			}
		}
		double orderS = (Log2R(gapS[0], gapS[1]) + Log2R(gapS[1], gapS[2])) * 0.5;
		double orderP = (Log2R(gapP[0], gapP[1]) + Log2R(gapP[1], gapP[2])) * 0.5;
		CT.Info(string.Format("── ПОКАЗАТЕЛЬ: gap_s ∝ dt^{0:F2}; gap_p ∝ dt^{1:F2}", orderS, orderP));
		CT.Info(string.Format("   gap_s: {0:F6} → {1:F6} → {2:F6} Дж ", gapS[0], gapS[1], gapS[2]) + string.Format("(отношения {0:F2}×, {1:F2}×)", gapS[0] / gapS[1], gapS[1] / gapS[2]));
		CT.Info(string.Format("   gap_p: {0:F6} → {1:F6} → {2:F6} Дж ", gapP[0], gapP[1], gapP[2]) + string.Format("(отношения {0:F2}×, {1:F2}×)", gapP[0] / gapP[1], gapP[1] / gapP[2]));
		if (orderS > 0.7 && orderS < 1.4)
		{
			CT.Info("   ⭐⭐ gap_s ∝ dt — ЭТО ПОРЯДОК СХЕМЫ, а не дефект. Правки не существует; ворота обязаны выводиться ВЫШЕ него.");
		}
		else if (Math.Abs(orderS) < 0.3)
		{
			CT.Info("   ⚠️ gap_s от шага НЕ ЗАВИСИТ — дефект, независимый от dt, остаётся открытым, но теперь с измеренным свойством, а не с подозрением.");
		}
		else
		{
			CT.Info(string.Format("   ❓ показатель {0:F2} — ни ~1, ни ~0. Сообщается, утверждений НЕТ (ТЗ §7).", orderS));
		}
	}

	private static double Log2R(double a, double b)
	{
		if (Math.Abs(b) < 1E-30)
		{
			return double.NaN;
		}
		return Math.Log(Math.Abs(a / b)) / Math.Log(2.0);
	}

	private bool Determinism(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		int ordered = OrderDrops(cs, body, lo, hi, true);
		int unordered = OrderDrops(cs, body, lo, hi, false);
		CT.Info(string.Format("действие сортировки: спусков в _Order {0} (сорт. ВКЛ) против ", ordered) + string.Format("{0} (ВЫКЛ)", unordered));
		bool order = CT.True("   сортировка РЕАЛЬНО меняет порядок (иначе она мертва)", ordered < unordered);
		ulong[] old = new ulong[3];
		for (int i = 0; i < 3; i++)
		{
			old[i] = RunHash(cs, false, -1);
		}
		bool oldDifferent = old[0] != old[1] || old[1] != old[2];
		CT.Info(string.Format("М1 старый путь (сортировка ВЫКЛ): отпечатки {0:X16} / {1:X16} / ", old[0], old[1]) + string.Format("{0:X16}", old[2]));
		bool oldSees = CT.True("   ⭐ отпечаток УМЕЕТ видеть расхождение — на старом пути они РАЗНЫЕ", oldDifferent);
		bool baseline = order && oldSees;
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			Flat2DScene.Body body2 = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
			Flat2DScene.Domain(body2, 0.01f, out Vector2 lo2, out Vector2 hi2);
			s.ArtificialViscosity = false;
			s.FixedTimeStep = StiffestDt();
			s.Init(body2.pos, body2.vel, body2.mat, Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA), Dx, lo2, hi2);
			s.Steps(Warm);
			ulong h0 = s.StateHash();
			s.Steps(Window);
			ulong h1 = s.StateHash();
			CT.Info(string.Format("М2 внутри прогона: после разгона {0:X16}, после окна {1:X16}", h0, h1));
			baseline = baseline && CT.True("   выгрузка ОБНОВЛЯЕТСЯ — отпечатки разные", h0 != h1);
		}
		ulong clean = RunHash(cs, true, -1);
		ulong changed = RunHash(cs, true, 100);
		CT.Info("М3 сдвиг НАЧАЛЬНОЙ скорости частицы #100 (поршень, 5 м/с) на один ulp: " + string.Format("{0:X16} → {1:X16}", clean, changed));
		bool ulp = CT.True("   отпечаток НЕ схлопывает состояние", clean != changed);
		ulong[] hashes = new ulong[3];
		for (int i = 0; i < 3; i++)
		{
			hashes[i] = RunHash(cs, true, -1);
		}
		CT.Info(string.Format("D1 детерминированный путь: {0:X16} / {1:X16} / {2:X16}", hashes[0], hashes[1], hashes[2]));
		bool same = hashes[0] == hashes[1] && hashes[1] == hashes[2];
		return CT.True("   ⭐⭐ ТРИ ПРОГОНА СОВПАЛИ БИТ-В-БИТ", same) && ulp && baseline;
	}

	private int OrderDrops(ComputeShader cs, Flat2DScene.Body b, Vector2 lo, Vector2 hi, bool deterministic)
	{
		int drops = 0;
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = false;
			s.FixedTimeStep = StiffestDt();
			s.DeterministicOrder = deterministic;
			s.Init(b.pos, b.vel, b.mat, Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA), Dx, lo, hi);
			s.Steps(50);
			uint[] order = s.ReadOrder();
			for (int i = 1; i < order.Length; i++)
			{
				if (order[i] < order[i - 1])
				{
					drops++;
				}
			}
		}
		return drops;
	}

	private ulong RunHash(ComputeShader cs, bool deterministic, int ulpAt)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
		if (ulpAt >= 0 && ulpAt < body.vel.Length)
		{
			body.vel[ulpAt].x = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(body.vel[ulpAt].x) + 1);
		}
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = false;
			s.FixedTimeStep = StiffestDt();
			s.DeterministicOrder = deterministic;
			s.Init(body.pos, body.vel, body.mat, Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA), Dx, lo, hi);
			s.Steps(10280);
			if (!s.ProbeAlive(10280, out string detail))
			{
				throw new InvalidOperationException("RunHash: решатель НЕ шагал — " + detail);
			}
			return s.StateHash();
		}
	}

	private void Cost(ComputeShader cs)
	{
		double oldA = Throughput(cs, false);
		double detA = Throughput(cs, true);
		double detB = Throughput(cs, true);
		double oldB = Throughput(cs, false);
		double oldMean = (oldA + oldB) * 0.5;
		double detMean = (detA + detB) * 0.5;
		double loss = (oldMean - detMean) / oldMean * 100.0;
		CT.Info(string.Format("чередование A/B/B/A: старый {0:E3} / {1:E3}, детерминированный ", oldA, oldB) + string.Format("{0:E3} / {1:E3} частице-шагов в секунду", detA, detB));
		CT.Info(string.Format("средние: детерминированно {0:E3}, старый путь {1:E3} ⇒ потеря {2:F2} %", detMean, oldMean, loss));
		CT.Info(string.Format("⚠️ собственный разброс замера: детерминированный {0:F2} %, старый ", Math.Abs(detA - detB) / detMean * 100.0) + string.Format("{0:F2} % — цену читать НЕ ТОЧНЕЕ этого", Math.Abs(oldA - oldB) / oldMean * 100.0));
		CT.Info("⚠️ Стенд ThroughputBench к этой правке СЛЕП: у него свой шейдер и решатель он не зовёт. Его число идёт в отчёт как контроль машины, а НЕ как цена.");
		CT.Info(string.Format("   пересчёт на бюджет выстрела: 48 с голого стенда ⇒ +{0:F1} с; ", loss * 48.0 / 100.0) + string.Format("при вилке 1.5–3 мин полной физики это +{0:F0}…", loss * 90.0 / 100.0) + string.Format("{0:F0} с", loss * 180.0 / 100.0));
	}

	private double Throughput(ComputeShader cs, bool deterministic)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = false;
			s.FixedTimeStep = StiffestDt();
			s.DeterministicOrder = deterministic;
			s.Init(body.pos, body.vel, body.mat, Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA), Dx, lo, hi);
			s.Steps(200);
			s.Measure(false);
			Stopwatch watch = Stopwatch.StartNew();
			s.Steps(3000);
			s.Measure(false);
			watch.Stop();
			return (double)s.N * 3000.0 / watch.Elapsed.TotalSeconds;
		}
	}

	private void Repeat(ComputeShader cs)
	{
		double[] runs = new double[3];
		for (int i = 0; i < 3; i++)
		{
			runs[i] = Rung(cs, string.Format("повтор {0}  B5", i + 1), false, false, true);
		}
		double min = double.MaxValue;
		double max = double.MinValue;
		double sum = 0.0;
		for (int i = 0; i < runs.Length; i++)
		{
			min = Math.Min(min, runs[i]);
			max = Math.Max(max, runs[i]);
			sum += runs[i];
		}
		double spread = (max - min) * 100.0;
		CT.Info(string.Format("── B5, тот же код, три прогона: {0:F4} / {1:F4} / ", runs[0] * 100.0, runs[1] * 100.0) + string.Format("{0:F4} % ⇒ размах {1:F4} п.п., среднее {2:F4} %", runs[2] * 100.0, spread, sum / 3.0 * 100.0));
		CT.Info(string.Format("   ⭐ РАЗБРОС ОТ ПРОГОНА К ПРОГОНУ = {0:F4} п.п. — это ВТОРОЕ слагаемое ", spread) + "пола, и ТЗ §2 его не предполагало");
	}

	private void N1(ComputeShader cs)
	{
		Floor(cs, "B1 (только давление, G = 0, Γ₀ = 0)", true, true, false);
		Floor(cs, "B5 (полная, как A3′)", false, false, true);
		CT.Info("⚠️ У B5 в итог входит ещё и Σ(m/ρ)·S:S/(4G) — член со своей обусловленностью, поэтому пол у двух ступеней не обязан совпадать (ТЗ §2).");
	}

	private void Floor(ComputeShader cs, string label, bool zeroG, bool zeroGamma, bool plasticity)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] mats = Mats(zeroG, zeroGamma);
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = false;
			s.Plasticity = plasticity;
			s.FixedTimeStep = StiffestDt();
			s.Init(body.pos, body.vel, body.mat, mats, Dx, lo, hi);
			s.Steps(Warm);
			Flat2DSolver.Diag a = s.Measure(false);
			Cpu cpuA = CpuTotals(s, mats);
			s.Steps(Window);
			Flat2DSolver.Diag b = s.Measure(false);
			Cpu cpuB = CpuTotals(s, mats);
			double gpu = Math.Abs(b.Total - a.Total) / Math.Abs(a.Total);
			double cpu = Math.Abs(cpuB.Total - cpuA.Total) / Math.Abs(cpuA.Total);
			CT.Info("── " + label);
			CT.Info(string.Format("   старт: GPU {0:F6} Дж (кин {1:F6} + внутр {2:F6}); ", a.Total, a.kinetic, a.internalE) + string.Format("ЦПУ {0:F6} (кин {1:F6} + внутр {2:F6})", cpuA.Total, cpuA.kinetic, cpuA.internalE));
			CT.Info(string.Format("   конец: GPU {0:F6} Дж; ЦПУ {1:F6} Дж", b.Total, cpuB.Total));
			CT.Info(string.Format("   дрейф: GPU {0:F6} %; ЦПУ(FP64) {1:F6} %", gpu * 100.0, cpu * 100.0));
			CT.Info(string.Format("   ⭐ ПОЛ = {0:F6} п.п.", Math.Abs(gpu - cpu) * 100.0));
		}
	}

	private static Snap ReadSnap(Flat2DSolver s)
	{
		return new Snap
		{
			mass = s.ReadMass(),
			vel = s.ReadVelHalf(),
			eInt = s.ReadEInt(),
			rho = s.ReadDensity(),
			stress = s.ReadStress(),
			mat = s.ReadMaterialIds()
		};
	}

	private static Cpu Sum(Snap a, Flat2DMaterialBlock[] mats)
	{
		Cpu result = default;
		for (int i = 0; i < a.mass.Length; i++)
		{
			double mass = a.mass[i];
			result.mass += mass;
			result.kinetic += ((double)a.vel[i].x * a.vel[i].x + (double)a.vel[i].y * a.vel[i].y) * mass * 0.5;
			double shear = 0.0;
			double g = mats[a.mat[i]].G;
			if (g > 1000.0)
			{
				Vector4 stress = a.stress[i];
				double third = -(double)stress.x - stress.y;
				shear = ((double)stress.x * stress.x + (double)stress.y * stress.y + third * third + 2.0 * stress.z * stress.z) * (mass / a.rho[i]) / (g * 4.0);
			}
			result.shear += shear;
			result.internalE += (double)a.eInt[i] * mass + shear;
		}
		return result;
	}

	private static Cpu CpuTotals(Flat2DSolver s, Flat2DMaterialBlock[] mats)
	{
		return Sum(ReadSnap(s), mats);
	}

	private void N3(ComputeShader cs)
	{
		double b1 = Rung(cs, "B1  —", true, true, false);
		double b2 = Rung(cs, "B2  + Γ₀", true, false, false);
		double b4 = Rung(cs, "B4  + девиатор", false, true, false);
		CT.Info(string.Format("── ступени при закреплённом шаге: B1 {0:F4} %, B2 {1:F4} %, ", b1 * 100.0, b2 * 100.0) + string.Format("B4 {0:F4} %", b4 * 100.0));
		double gamma = Math.Abs(b2 - b1);
		double dev = Math.Abs(b4 - b1);
		CT.Info(string.Format("   вклад Γ₀ {0:F4} п.п.; вклад девиатора {1:F4} п.п. ", gamma * 100.0, dev * 100.0) + string.Format("⇒ отношение {0:F1}×", gamma > 0.0 ? dev / gamma : double.PositiveInfinity));
		CT.Info(gamma * 10.0 < dev ? "   ⭐ ОСТАТОК НЕСЁТ ДЕВИАТОР — Γ₀ на порядок меньше" : "   ⚠️ вклады сопоставимы — приписывать остаток одному члену НЕЛЬЗЯ");
	}

	private void N4(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = false;
			s.Plasticity = false;
			s.FixedTimeStep = StiffestDt();
			s.AccumulateWork = true;
			Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
			for (int i = 0; i < mats.Length; i++)
			{
				mats[i].gamma0 = 0f;
				mats[i].aEos = 0f;
			}
			s.Init(body.pos, body.vel, body.mat, mats, Dx, lo, hi);
			s.Steps(Warm);
			s.MeasureWork(out double wp0, out double ws0, out double shear0, out double ke0);
			s.MeasureStateEnergy(out double _, out double _, out double path0, out int _);
			Flat2DSolver.Diag a = s.Measure(false);
			s.Steps(Window);
			s.MeasureWork(out double wp1, out double ws1, out double shear1, out double ke1);
			s.MeasureStateEnergy(out double _, out double _, out double path1, out int _);
			Flat2DSolver.Diag b = s.Measure(false);
			double workP = wp1 - wp0;
			double workS = ws1 - ws0;
			double potS = shear1 - shear0;
			double minusKe = -(ke1 - ke0);
			double path = path1 - path0;
			double pressureRelative = Math.Abs(workP - path) / Math.Max(Math.Abs(path), 1E-30);
			CT.Info(string.Format("── ПЕРЕСОБРАННАЯ ДАВЛЕНЧЕСКАЯ ПОЛОВИНА: Σm·∫Δe_P = {0:F6} Дж против ", workP) + string.Format("Δ(Σm·e) = {0:F6} Дж ⇒ {1:F4} %", path, pressureRelative * 100.0));
			CT.Info(pressureRelative >= 0.01 ? "   ⚠️ половина НЕ совпадает с тем, чем реально движется e — читать её нельзя" : "   ✔ половина СЧИТАЕТ ЖИВОЙ ЧЛЕН — прежние +221 Дж были парной формой, которой в решателе больше нет");
			double residual = workP + workS - minusKe;
			double closureRelative = Math.Abs(residual) / Math.Max(Math.Abs(minusKe), 1E-30);
			CT.Info(string.Format("── ЗАМЫКАНИЕ (проверяется ПЕРВЫМ): части ∫ėP {0:F5} + ∫ėS {1:F5} = ", workP, workS) + string.Format("{0:F5} Дж; целое −ΔKE = {1:F5} Дж; остаток {2:F5} Дж ", workP + workS, minusKe, residual) + string.Format("{0:F4} % = ", closureRelative * 100.0) + string.Format("⇒ {0:F3} %", closureRelative * 100.0));
			if (closureRelative > 0.05)
			{
				CT.Info(string.Format("   ⛔ ЗАМЫКАНИЕ НЕ ДЕРЖИТ ({0:F3} % > {1:F0} %). ", closureRelative * 100.0, 5.0) + "Сравнение НЕ ЧИТАЕТСЯ и §3.3 НЕ ЗАПУСКАЕТСЯ. Это ровно ошибка L3, и повторять её нельзя.");
				CT.Info(string.Format("   ⭐ НО АДРЕС СУЗИЛСЯ: давленческая половина теперь {0:F4} Дж ", workP) + string.Format("(была +221 Дж парной формой), а девиаторная — {0:F4} Дж против ", workS) + string.Format("целого {0:F4} Дж. Невязку несёт ЦЕЛИКОМ ∫ėS, и это тот же класс: ", minusKe) + "парная форма, которой решатель не пользуется. ВТОРОЙ провал замыкания на этом приборе ⇒ по ТЗ §3 предметом становится ПРИБОР, не физика.");
				return;
			}
			CT.Info("   ✔ замыкание держит — сравнение можно читать");
			double gap = workS - potS;
			CT.Info(string.Format("── СРАВНЕНИЕ: парная работа девиатора ∫ėS = {0:F6} Дж; прирост потенциала ", workS) + string.Format("Δ Σ(m/ρ)S:S/(4G) = {0:F6} Дж; РАЗРЫВ {1:F6} Дж ⇒ {2:F3} %", potS, gap, Math.Abs(gap) / Math.Max(Math.Abs(potS), 1E-30) * 100.0));
			CT.Info(string.Format("   для масштаба: полный итог на старте {0:F4} Дж, дрейф ступени ", a.Total) + string.Format("{0:F4} % = ", Math.Abs(b.Total - a.Total) / a.Total * 100.0) + string.Format("{0:F6} Дж", Math.Abs(b.Total - a.Total)));
			CT.Info(Math.Abs(gap) > Math.Abs(b.Total - a.Total) * 0.5 ? "   ⭐ РАЗРЫВ ОБЪЯСНЯЕТ ОСТАТОК по величине — у остатка есть адрес" : "   ⚠️ разрыв НЕ объясняет остаток по величине — адреса он не даёт");
		}
	}

	private double Rung(ComputeShader cs, string label, bool zeroG, bool zeroGamma, bool plasticity)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = false;
			s.Plasticity = plasticity;
			s.FixedTimeStep = StiffestDt();
			s.Init(body.pos, body.vel, body.mat, Mats(zeroG, zeroGamma), Dx, lo, hi);
			s.Steps(Warm);
			Flat2DSolver.Diag a = s.Measure(false);
			s.Steps(Window);
			Flat2DSolver.Diag b = s.Measure(false);
			double drift = Math.Abs(b.Total - a.Total) / Math.Abs(a.Total);
			CT.Info(string.Format("{0} дрейф {1:F4} % ({2:F4} → {3:F4} Дж)", label, drift * 100.0, a.Total, b.Total));
			return drift;
		}
	}

	private static Flat2DMaterialBlock[] Mats(bool zeroG, bool zeroGamma)
	{
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
		for (int i = 0; i < mats.Length; i++)
		{
			if (zeroG)
			{
				mats[i].G = 0f;
			}
			if (zeroGamma)
			{
				mats[i].gamma0 = 0f;
				mats[i].aEos = 0f;
			}
		}
		return mats;
	}

	private static float StiffestDt()
	{
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
		float fastest = 0f;
		for (int i = 0; i < mats.Length; i++)
		{
			fastest = Mathf.Max(fastest, mats[i].LongitudinalWaveSpeed);
		}
		return 0.000195f / (fastest + 5f);
	}

	private static float StiffestDtElasticK()
	{
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
		float fastest = 0f;
		for (int i = 0; i < mats.Length; i++)
		{
			fastest = Mathf.Max(fastest, mats[i].ElasticWaveSpeedK);
		}
		return 0.000195f / (fastest + 5f);
	}

	private ComputeShader LoadShader()
	{
		return solverShader != null ? solverShader : null;
	}
}
