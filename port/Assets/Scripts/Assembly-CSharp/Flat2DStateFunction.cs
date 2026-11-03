using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class Flat2DStateFunction : MonoBehaviour
{
	private struct Sample
	{
		public int step;

		public double ke;

		public double ePath;

		public double uInt;

		public double uRest;

		public int interior;

		public double EPath => ePath / (uInt + uRest);

		public double EState => uInt / (uInt + uRest);
	}

	public ComputeShader solverShader;

	public bool sabotagePremise;

	private const string ShaderPath = "Assets/Scripts/Flat2D/Flat2DSolver.compute";

	private const int Warm = 300;

	private const int Chunk = 500;

	private const int Chunks = 20;

	private const int Nx = 120;

	private const int Ny = 30;

	private const float Dx = 0.0005f;

	private const float Push = 5f;

	public static int LastFailures { get; private set; }

	[ContextMenu("★ Функция состояния: S1 → S4")]
	public void RunAll()
	{
		ComputeShader cs = (solverShader != null) ? solverShader : null;
		if (cs == null)
		{
			Debug.LogError("Flat2D: не найден Assets/Scripts/Flat2D/Flat2DSolver.compute");
			return;
		}
		Debug.Log("══════ ФУНКЦИЯ СОСТОЯНИЯ: ТЕЧЬ В `e` ИЛИ В ДИНАМИКЕ? ══════");
		Debug.Log(string.Format("устройство: {0} ({1})", SystemInfo.graphicsDeviceName, SystemInfo.graphicsDeviceType));
		bool s1ok = false;
		bool premiseok = false;
		int pass = 0;
		int total = 0;
		int measured = 0;
		CT.Run("S1", "квадратура u(ρ): три проверки ДО первого чтения", delegate
		{
			s1ok = S1(cs);
			return s1ok;
		}, ref pass, ref total);
		if (!s1ok)
		{
			Debug.LogError("⛔ КВАДРАТУРА НЕ ПРОШЛА ПРОВЕРКУ. Дальше не идём: непроверенный прибор — это ровно то, что произвело L3.");
			Debug.Log(string.Format("══════ ИТОГ: пройдено {0}, провалено {1}, измерено {2} ══════", pass, total - pass, measured));
			return;
		}
		CT.Run("ПОСЫЛКА", "p зависит только от ρ? — и видит ли сторож порчу", delegate
		{
			premiseok = Premise(cs);
			return premiseok;
		}, ref pass, ref total);
		if (!premiseok)
		{
			Debug.LogError("⛔ ПОСЫЛКА НАРУШЕНА: p зависит не только от ρ ⇒ u(ρ) НЕ функция состояния, и E_сост ничего не значит. Вердикт §2.2 НЕ ВЫНОСИТСЯ.");
			Debug.Log(string.Format("══════ ИТОГ: пройдено {0}, провалено {1}, измерено {2} ══════", pass, total - pass, measured));
			LastFailures = total - pass;
			return;
		}
		CT.Measure("ОБЩАЯ МОДА", "e и u(ρ) делят ρ и УРС — ловит ли независимый канал их СОГЛАСОВАННУЮ порчу", delegate
		{
			CommonMode(cs);
		}, ref measured);
		CT.Measure("ВЯЗКОСТЬ", "ė_visc ≥ 0 у каждой частицы на каждом шаге (замер, не гейт)", delegate
		{
			Viscous(cs);
		}, ref measured);
		CT.Measure("S2–S4", "E_путь против E_сост на B1, с разделением по границе", delegate
		{
			Curves(cs);
		}, ref measured);
		Debug.Log(string.Format("══════ ИТОГ: пройдено {0}, провалено {1}, измерено {2} ══════", pass, total - pass, measured));
		LastFailures = total - pass;
	}

	private bool S1(ComputeShader cs)
	{
		bool ok = true;
		Flat2DMaterialBlock[] mats = Mats(zeroG: false, zeroGamma: false);
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(8, 4, Dx, 0f);
		Flat2DScene.Domain(body, 0.01f, out Vector2 min, out Vector2 max);
		Flat2DSolver s = new Flat2DSolver(cs);
		s.Init(body.pos, body.vel, body.mat, mats, Dx, min, max);
		s.CheckQuadrature(0, mats[0].rho0 * 1.002f, out double uAtRho0, out double uN, out double u2N);
		double rel = Math.Abs(u2N - uN) / Math.Max(Math.Abs(uN), 1.0);
		CT.Info(string.Format("проверка 1: u(ρ₀) = {0:E17} Дж/кг", uAtRho0));
		ok &= CT.True("u(ρ₀) РОВНО ноль — аддитивной постоянной в квадратуру не затесалось", uAtRho0 == 0.0);
		CT.Info("   ⚠️ проверка структурна: при b == a шаг обращается в нуль и сумма даёт точный нуль сама. Она подтверждает отсутствие постоянной, а не сходимость.");
		CT.Info(string.Format("проверка 2: N = {0} ⇒ u = {1:E9}; ", s.QuadIntervals, uN) + string.Format("2N = {0} ⇒ u = {1:E9} Дж/кг", s.QuadIntervals * 2, u2N));
		ok &= CT.True(string.Format("удвоение числа интервалов сдвигает на {0:E3} < 1e-6", rel), 1e-06 > rel);
		Flat2DMaterialBlock[] clean = Mats(zeroG: true, zeroGamma: true);
		clean[0].s = 1f;
		Flat2DSolver s2 = new Flat2DSolver(cs);
		s2.Init(body.pos, body.vel, body.mat, clean, Dx, min, max);
		double[] rs = new double[2] { 1.1, 1.0025 };
		for (int j = 0; j < rs.Length; j++)
		{
			float r = (float)((double)clean[0].rho0 * rs[j]);
			s2.CheckQuadrature(0, r, out double _, out double uQuad, out double _);
			double mu = (double)r / (double)clean[0].rho0 - 1.0;
			double manual = (double)clean[0].c0 * (double)clean[0].c0 * (mu - Math.Log((double)r / (double)clean[0].rho0));
			double quadRel = Math.Abs(uQuad - manual) / Math.Abs(manual);
			double kappa = Math.Abs(mu) / Math.Abs(mu - Math.Log((double)r / (double)clean[0].rho0));
			double fpLimit = kappa * 1.1920929e-07;
			CT.Info(string.Format("проверка 3 при r = {0:F5} (κ = {1:F1}, предел FP32 = {2:E3}): ", mu, kappa, fpLimit) + string.Format("квадратура {0:E9}, вручную {1:E9} Дж/кг, расхождение {2:E3}", uQuad, manual, quadRel));
			ok &= CT.True(string.Format("   расхождение {0:E3} ≤ выведенного предела {1:E3}", quadRel, fpLimit), fpLimit >= quadRel);
		}
		CT.Info("⚠️⚠️ НАХОДКА О САМОЙ ПРОВЕРКЕ 2: удвоение интервалов дало РОВНО НОЛЬ изменения, а проверка 3 показала реальную ошибку ~2e-5. Значит проверка 2 ограничивает ошибку УСЕЧЕНИЯ и СЛЕПА к ошибке ОКРУГЛЕНИЯ. Одна она объявила бы квадратуру точной.");
		s.Dispose();
		s2.Dispose();
		return ok;
	}

	private bool Premise(ComputeShader cs)
	{
		bool ok = true;
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out Vector2 min, out Vector2 max);
		Flat2DSolver s = new Flat2DSolver(cs);
		s.ArtificialViscosity = true;
		s.Plasticity = true;
		s.FixedTimeStep = StiffestDt();
		Flat2DMaterialBlock[] mats = Mats(zeroG: false, zeroGamma: false);
		if (sabotagePremise)
		{
			for (int i = 0; i < mats.Length; i++)
			{
				mats[i].aEos = 0.5f;
			}
			CT.Info("⚠️ ДИВЕРСИЯ ВКЛЮЧЕНА: a := 0.5 на здоровой стороне. Ворота обязаны остановить прогон до вердикта §2.2.");
		}
		s.Init(body.pos, body.vel, body.mat, mats, Dx, min, max);
		s.Steps(Warm);
		s.MeasurePremise(out double dpDeA, out double dcDeA, out double gammaA, out double pA);
		s.Steps(Chunks * Chunk);
		s.MeasurePremise(out double dpDeB, out double dcDeB, out double gammaB, out double pB);
		CT.Info(string.Format("B1 старт: max|∂p/∂e| {0:E3}, max|∂c/∂e| {1:E3}, max|Γ₀+aμ| {2:E3}, ", dpDeA, dcDeA, gammaA) + string.Format("max|p| {0:F3} ГПа", pA / 1e+09));
		CT.Info(string.Format("B1 конец: max|∂p/∂e| {0:E3}, max|∂c/∂e| {1:E3}, max|Γ₀+aμ| {2:E3}, ", dpDeB, dcDeB, gammaB) + string.Format("max|p| {0:F3} ГПа", pB / 1e+09));
		ok &= CT.True("посылка держится: p — функция ОДНОЙ ρ по всем частицам и на обоих концах", dpDeA == 0.0 && dpDeB == 0.0 && dcDeA == 0.0 && dcDeB == 0.0);
		if (pB > 1.3e+10)
		{
			CT.Info("⚠️ прогон перешёл 13 ГПа — там ударная адиабата стали ломается (α→ε), а кусочной сегментации в Pressure() НЕТ");
		}
		s.Dispose();
		Flat2DMaterialBlock[] counterMats = Mats(zeroG: true, zeroGamma: true);
		for (int j = 0; j < counterMats.Length; j++)
		{
			counterMats[j].aEos = 0.5f;
		}
		Flat2DSolver c = new Flat2DSolver(cs);
		c.ArtificialViscosity = false;
		c.Plasticity = false;
		c.FixedTimeStep = StiffestDt();
		c.Init(body.pos, body.vel, body.mat, counterMats, Dx, min, max);
		c.Steps(Warm);
		c.MeasurePremise(out double dpDeC, out double dcDeC, out double gammaC, out double pC);
		CT.Info(string.Format("контрфакт (Γ₀ = 0, a = 0.5): max|∂p/∂e| {0:E3}, max|∂c/∂e| {1:E3}, ", dpDeC, dcDeC) + string.Format("max|Γ₀+aμ| {0:E3}", gammaC));
		ok &= CT.True("сторож ВИДИТ порчу, которую поле «gamma0 = 0» пропускает", dpDeC > 0.0 || dcDeC > 0.0);
		c.Dispose();
		return ok;
	}

	private void Curves(ComputeShader cs)
	{
		float num = StiffestDt();
		CT.Info(string.Format("ступень B1 при ЗАКРЕПЛЁННОМ шаге dt* = {0:E6} с ", num) + "(§1: гейт A3′ отныне здесь; шаг Куранта — цена схемы, не дефект)");
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out Vector2 min, out Vector2 max);
		List<Sample> list = new List<Sample>();
		Flat2DSolver s = new Flat2DSolver(cs);
		s.ArtificialViscosity = false;
		s.Plasticity = false;
		s.FixedTimeStep = num;
		Flat2DMaterialBlock[] mats = Mats(zeroG: true, zeroGamma: true);
		s.Init(body.pos, body.vel, body.mat, mats, Dx, min, max);
		s.Steps(Warm);
		list.Add(Snap(s, 0));
		for (int i = 0; i < Chunks; i++)
		{
			s.Steps(Chunk);
			list.Add(Snap(s, (i + 1) * Chunk));
		}
		s.Dispose();
		Sample sample = list[0];
		Sample sample2 = list[list.Count - 1];
		CT.Info("── S2: обе величины сняты С ОДНОГО состояния, одной выборкой");
		CT.Info("шаг      E_путь, Дж     E_сост, Дж    Σmu внутр    Σmu гран.   внутр.");
		foreach (Sample item in list)
		{
			CT.Info(string.Format("{0,6}  {1,12:F4}  {2,12:F4}  {3,10:F4}  ", item.step, item.ePath + item.ke, item.uInt + item.ke + item.uRest, item.uInt) + string.Format("{0,10:F4}  {1,5} ({2:F1} %)", item.uRest, item.interior, (double)item.interior * 100.0 / 5040.0));
		}
		CT.Info(string.Format("── S2 итог по ВСЕМУ окну ({0} шагов): ", Chunks * Chunk) + string.Format("E_путь {0:F4} → {1:F4} Дж, дрейф {2:F4} %; ", sample.ePath + sample.ke, sample2.ePath + sample2.ke, Math.Abs(sample2.ePath + sample2.ke - (sample.ePath + sample.ke)) / Math.Abs(sample.ePath + sample.ke) * 100.0) + string.Format("E_сост {0:F4} → {1:F4} Дж, дрейф {2:F4} %", sample.uInt + sample.ke + sample.uRest, sample2.uInt + sample2.ke + sample2.uRest, Math.Abs(sample2.uInt + sample2.ke + sample2.uRest - (sample.uInt + sample.ke + sample.uRest)) / Math.Abs(sample.uInt + sample.ke + sample.uRest) * 100.0));
		CT.Info(string.Format("── S3, разделение: внутренние {0} → {1} из 5040; ", sample.interior, sample2.interior) + string.Format("Σm·u внутри {0:F4} → {1:F4} Дж; на границе {2:F4} → {3:F4} Дж", sample.uInt, sample2.uInt, sample.uRest, sample2.uRest));
		int num2 = 0;
		for (int j = 0; j < list.Count; j++)
		{
			if ((double)list[j].interior >= (double)sample.interior * 0.9)
			{
				num2 = j;
			}
		}
		Sample sample3 = list[num2];
		double num3 = sample3.step - sample.step;
		double num4 = (double)num * num3;
		if (num3 > 0.0)
		{
			double num5 = (sample3.ke + sample3.ePath - (sample.ePath + sample.ke)) / num4;
			double num6 = (sample3.uInt + sample3.ke + sample3.uRest - (sample.uInt + sample.ke + sample.uRest)) / num4;
			CT.Info(string.Format("── S4, окно с ЦЕЛОЙ посылкой: шаги {0}…{1} ", sample.step, sample3.step) + string.Format("({0} шагов, {1:F2} мкс), внутренних ", num3, num4 * 1000000.0) + string.Format("{0} → {1} (≥ 90 % от старта)", sample.interior, sample3.interior));
			CT.Info(string.Format("   E_путь: {0:F4} → {1:F4} Дж ⇒ {2:E4} Вт ", sample.ePath + sample.ke, sample3.ke + sample3.ePath, num5) + string.Format("({0:F4} % за окно)", (sample3.ke + sample3.ePath - (sample.ePath + sample.ke)) / (sample.ePath + sample.ke) * 100.0));
			CT.Info(string.Format("   E_сост: {0:F4} → {1:F4} Дж ⇒ {2:E4} Вт ", sample.uInt + sample.ke + sample.uRest, sample3.uInt + sample3.ke + sample3.uRest, num6) + string.Format("({0:F4} % за окно)", (sample3.uInt + sample3.ke + sample3.uRest - (sample.uInt + sample.ke + sample.uRest)) / (sample.uInt + sample.ke + sample.uRest) * 100.0));
		}
		else
		{
			CT.Info("── S4: окно с целой посылкой пусто — доля внутренних падает ниже 90 % уже на первом отсчёте. Скорость на нём не меряется.");
		}
		double num7 = (sample3.ke + sample3.ePath - (sample.ePath + sample.ke)) / (sample.ePath + sample.ke);
		double num8 = (sample3.uInt + sample3.ke + sample3.uRest - (sample.uInt + sample.ke + sample.uRest)) / (sample.uInt + sample.ke + sample.uRest);
		CT.Info("── S3, ЧТЕНИЕ ВЕТВИ:");
		if (Math.Abs(num7) < 0.01 && Math.Abs(num8) < 0.01)
		{
			CT.Info(string.Format("   ⭐⭐ ОБЕ ОГРАНИЧЕНЫ: E_путь {0:F4} %, E_сост ", num7 * 100.0) + string.Format("{0:F4} % ⇒ накопитель `e` согласован с траекторией ρ. ", num8 * 100.0) + "⚠️ Их СОГЛАСИЕ ожидаемо по построению (§3 делает Σm·e квадратурой того же интеграла); доказательна их МАЛОСТЬ, а не их близость.");
		}
		else if (Math.Abs(num8) < Math.Abs(num7) * 0.1 && Math.Abs(num7) > 1.0)
		{
			CT.Info(string.Format("   ⭐⭐ E_сост ограничена ({0:F2} %), E_путь растёт ", num8 * 100.0) + string.Format("({0:F2} %) ⇒ течь ЦЕЛИКОМ в интегрировании `e`, ", num7 * 100.0) + "динамика цела.");
		}
		else if (Math.Abs(num8) > Math.Abs(num7) * 0.5)
		{
			CT.Info(string.Format("   ⛔ РАСТУТ ОБЕ ({0:F2} % против {1:F2} %) ⇒ ", num8 * 100.0, num7 * 100.0) + "течь в паре «сила–плотность», `e` невиновна.");
		}
		else
		{
			CT.Info(string.Format("   ❓ расходятся третьим образом ({0:F2} % против ", num8 * 100.0) + string.Format("{0:F2} %) ⇒ обе кривые в отчёт, приписывания НЕТ.", num7 * 100.0));
		}
	}

	private void CommonMode(ComputeShader cs)
	{
		float[] rhoScales = new float[2] { 1f, 1.02f };
		for (int i = 0; i < rhoScales.Length; i++)
		{
			float scale = rhoScales[i];
			Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, Dx, Push);
			Flat2DScene.Domain(body, 0.01f, out Vector2 min, out Vector2 max);
			Flat2DSolver s = new Flat2DSolver(cs);
			s.ArtificialViscosity = false;
			s.Plasticity = false;
			s.FixedTimeStep = StiffestDt();
			s.MutRhoRateScale = scale;
			Flat2DMaterialBlock[] mats = Mats(zeroG: true, zeroGamma: true);
			s.Init(body.pos, body.vel, body.mat, mats, Dx, min, max);
			s.Steps(Warm + Chunks * Chunk);
			Flat2DSolver.Diag d = s.Measure();
			s.MeasureStateEnergy(out double uInt, out double uRest, out double ePath, out int interior);
			s.MeasureStateEnergySum(out double uIntSum, out double uRestSum, out double ePathSum, out int interiorSum);
			CT.Info(string.Format("КОНТРФАКТ ρ̇ ×{0}", scale) + ": " + string.Format("{0}: KE + Σm·e = {1:F4}; KE + Σm·u(ρ_неразр) = {2:F4} Дж; ", "как есть", d.kinetic + ePath, d.kinetic + uInt + uRest) + string.Format("внутренних {0}", interior));
			CT.Info(string.Format("   e против u(ρ_неразр): {0:E3} % — ОБЩАЯ МОДА, согласие ", Math.Abs(uInt - uRest) / Math.Max(Math.Abs(uInt), 1.0)));
			CT.Info(string.Format("   Σm·u ВНУТРИ: неразрывность {0:F5} против суммирования {1:F5} Дж ", uInt, uIntSum) + string.Format("⇒ {0:F3} % ⇐ НЕЗАВИСИМЫЙ канал (ρ_сумм из ПОЛОЖЕНИЙ)", Math.Abs(uInt - uIntSum) / Math.Max(Math.Abs(uInt), 1.0)));
			CT.Info("⚠️ Читать надо ПОСЛЕДНЮЮ величину: первая мала по построению и ничего не доказывает.");
			CT.Info("⚠️ Канал УСИЛИВАЕТ ошибку плотности: при малом μ имеем u ≈ c₀²μ²/2, значит δu/u ≈ 2·δμ/μ, а μ ≈ 0.002 ⇒ 0.01 % по ρ превращается в ~10 % по u. Поэтому здоровое расхождение здесь НЕ нуль, и порог ему не назначается.");
			s.Dispose();
		}
	}

	private void Viscous(ComputeShader cs)
	{
		bool[] modes = new bool[2] { false, true };
		for (int i = 0; i < modes.Length; i++)
		{
			bool signFlip = modes[i];
			Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, Dx, Push);
			Flat2DScene.Domain(body, 0.01f, out Vector2 min, out Vector2 max);
			Flat2DSolver s = new Flat2DSolver(cs);
			s.ArtificialViscosity = true;
			s.Plasticity = false;
			s.FixedTimeStep = StiffestDt();
			s.MutViscSignFlip = signFlip;
			Flat2DMaterialBlock[] mats = Mats(zeroG: false, zeroGamma: false);
			s.Init(body.pos, body.vel, body.mat, mats, Dx, min, max);
			s.Steps(Warm + Chunks * Chunk);
			double viscMin = s.ReadViscMin();
			float[] edot = s.ReadEdotVisc();
			double maxEdot = 0.0;
			double sum = 0.0;
			for (int j = 0; j < edot.Length; j++)
			{
				maxEdot = Math.Max(maxEdot, edot[j]);
				sum += edot[j];
			}
			Flat2DSolver.Diag d = s.Measure();
			CT.Info((signFlip ? "КОНТРФАКТ (знак перевёрнут)" : "как есть") + ": " + string.Format("min ė_visc по всем частицам за все шаги = {0:E4} Вт/кг; ", viscMin) + string.Format("max на последнем шаге {0:E4}; Σ = {1:E4}; ", maxEdot, sum) + string.Format("внутренняя {0:F4} Дж", d.internalE));
			CT.True(string.Format("   ė_visc ≥ 0 везде и всегда ({0:E4})", viscMin), viscMin >= 0.0);
			s.Dispose();
		}
		CT.Info("⚠️ Порога НЕТ намеренно: ТЗ §4 требует замерить, а не гатить.");
	}

	private static Sample Snap(Flat2DSolver s, int step)
	{
		Sample sample = default(Sample);
		Flat2DSolver.Diag d = s.Measure();
		s.MeasureStateEnergy(out double uInt, out double uRest, out double ePath, out int interior);
		sample.step = step;
		sample.ke = d.kinetic;
		sample.ePath = ePath;
		sample.uInt = uInt;
		sample.uRest = uRest;
		sample.interior = interior;
		return sample;
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
		float cmax = 0f;
		for (int i = 0; i < mats.Length; i++)
		{
			cmax = Mathf.Max(cmax, mats[i].LongitudinalWaveSpeed);
		}
		return 0.000195000015f / (cmax + 5f);
	}

	private ComputeShader LoadShader()
	{
		if (solverShader != null)
		{
			return solverShader;
		}
		return null;
	}
}
