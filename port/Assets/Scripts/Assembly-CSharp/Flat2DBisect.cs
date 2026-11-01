using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class Flat2DBisect : MonoBehaviour
{
	private sealed class Cfg
	{
		public string id;

		public string what;

		public bool noInteraction;

		public bool zeroG;

		public bool zeroGamma;

		public bool plasticity;

		public bool av;

		public float fixedDt;

		public float uniformV;

		public bool atRest;

		public bool eFirstOrder;
	}

	private struct Res
	{
		public double kin0;

		public double int0;

		public double kin1;

		public double int1;

		public double drift;

		public double absDelta;

		public double sumMA2;

		public double shear;

		public double dpde;

		public double plWork;

		public double hourglass;

		public double sumAbsS;

		public double maxEpsP;

		public double dtFirst;

		public double dtLast;

		public double time;

		public int steps;

		public bool avOn;

		public bool plOn;

		public double Total0 => int0 + kin0;

		public double Total1 => int1 + kin1;
	}

	public ComputeShader solverShader;

	private const string ShaderPath = "Assets/Scripts/Flat2D/Flat2DSolver.compute";

	private const int Warm = 300;

	private const int Window = 10000;

	private const int Nx = 120;

	private const int Ny = 30;

	private const float Dx = 0.0005f;

	private const float Push = 5f;

	[ContextMenu("★ Деление кода: B0 → B7")]
	public void RunAll()
	{
		ComputeShader cs = LoadShader();
		if (cs == null)
		{
			Debug.LogError("Flat2D: не найден Assets/Scripts/Flat2D/Flat2DSolver.compute");
			return;
		}
		Debug.Log("══════ ДЕЛЕНИЕ КОДА: ЛЕСТНИЦА B0 → B7 ══════");
		Debug.Log(string.Format("устройство: {0} ({1})", SystemInfo.graphicsDeviceName, SystemInfo.graphicsDeviceType));
		Debug.Log(string.Format("окно как у A3′: {0} шагов разгона + {1} шагов замера; ", Warm, Window) + string.Format("сцена «сжатие» {0}×{1}, dx {2:F2} мм, поршни {3} м/с", Nx, Ny, Dx * 1000f, Push));
		int pass = 0;
		int total = 0;
		int measured = 0;
		bool b0 = false;
		CT.Run("B0", "взаимодействия нет: дрейф обязан быть НУЛЁМ ПОБИТОВО", delegate
		{
			b0 = B0(cs);
			return b0;
		}, ref pass, ref total);
		if (!b0)
		{
			Debug.LogError("⛔ B0 ТЕЧЁТ. Лестница НЕ гоняется: против сломанного прибора измерять нечего. Недействительны K1, K2, K3, L1–L3, E1 и 526 %.");
		}
		else
		{
			CT.Measure("B1–B5", "лестница: давление → Γ₀ → шаг → девиатор → полная", delegate
			{
				Ladder(cs);
			}, ref measured);
			CT.Measure("B7", "плотность: неразрывность против суммирования, ТОЛЬКО ВНУТРИ", delegate
			{
				B7(cs);
			}, ref measured);
		}
		Debug.Log(string.Format("══════ ИТОГ ДЕЛЕНИЯ: пройдено {0}, провалено {1}, измерено {2} ══════", pass, total - pass, measured));
	}

	private bool B0(ComputeShader cs)
	{
		Res a = Run(cs, new Cfg
		{
			id = "B0a",
			noInteraction = true,
			plasticity = true,
			av = true,
			atRest = true
		});
		CT.Info(string.Format("B0a покой:  Σm|a|² = {0:E3}; кин {1:E6} → {2:E6} Дж; ", a.sumMA2, a.kin0, a.kin1) + string.Format("внутр {0:E6} → {1:E6} Дж; шагов {2}, dt {3:E6} с", a.int0, a.int1, a.steps, a.dtLast));
		bool t1 = CT.True("B0a: доказательство действия — Σm|a|² ровно 0", a.sumMA2 == 0.0);
		bool t2 = CT.True(string.Format("B0a: живость — KUpdateState {0} раз за окно (ждём {1})", a.steps, Window), a.steps == Window);
		bool t3 = CT.True(string.Format("B0a: кинетическая не сдвинулась ни на бит ({0:E3} Дж)", a.kin1 - a.kin0), a.kin1 == a.kin0);
		bool t4 = CT.True(string.Format("B0a: внутренняя не сдвинулась ни на бит ({0:E3} Дж)", a.int1 - a.int0), a.int1 == a.int0);
		Res b = Run(cs, new Cfg
		{
			id = "B0b",
			noInteraction = true,
			plasticity = true,
			av = true,
			fixedDt = 300f,
			atRest = true
		});
		CT.Info(string.Format("B0b полёт:  Σm|a|² = {0:E3}; кин {1:F9} → {2:F9} Дж; ", b.sumMA2, b.kin0, b.kin1) + string.Format("внутр {0:E6} → {1:E6} Дж; шагов {2}, dt {3:E6} с", b.int0, b.int1, b.steps, b.dtLast));
		CT.Info(string.Format("B0b дрейф {0:E3} % (абсолютно {1:E3} Дж)", b.drift * 100.0, b.absDelta));
		bool t5 = CT.True("B0b: доказательство действия — Σm|a|² ровно 0", b.sumMA2 == 0.0);
		bool t6 = CT.True(string.Format("B0b: живость — KUpdateState {0} раз за окно (ждём {1})", b.steps, Window), b.steps == Window);
		bool t7 = CT.True(string.Format("B0b: полная энергия не сдвинулась ни на бит ({0:E3} Дж)", b.absDelta), b.int1 + b.kin1 == b.int0 + b.kin0);
		return t1 & t2 & t3 & t4 & t5 & t6 & t7;
	}

	private void Ladder(ComputeShader cs)
	{
		float num = StiffestDt();
		CT.Info(string.Format("общий закреплённый шаг dt* = {0:E6} с (по самой жёсткой конфигурации)", num));
		Cfg[] array = new Cfg[5]
		{
			new Cfg
			{
				id = "B1",
				what = "только давление (G = 0, Γ₀ = 0, вязк. выкл., пласт. выкл.)",
				zeroG = true,
				zeroGamma = true,
				plasticity = false,
				av = false
			},
			new Cfg
			{
				id = "B1-1п",
				what = "U3: тот же B1, но ПЕРВЫЙ ПОРЯДОК вместо трапеции",
				zeroG = true,
				zeroGamma = true,
				plasticity = false,
				av = false,
				eFirstOrder = true
			},
			new Cfg
			{
				id = "B2",
				what = "B1 + Γ₀ (обратная связь энергия → давление)",
				zeroG = true,
				zeroGamma = false,
				plasticity = false,
				av = false
			},
			new Cfg
			{
				id = "B4",
				what = "B1 + девиатор (G восстановлен)",
				zeroG = false,
				zeroGamma = true,
				plasticity = false,
				av = false
			},
			new Cfg
			{
				id = "B5",
				what = "полная конфигурация (как A3′)",
				zeroG = false,
				zeroGamma = false,
				plasticity = true,
				av = false
			}
		};
		Dictionary<string, Res> byCourant = new Dictionary<string, Res>();
		Dictionary<string, Res> byPinned = new Dictionary<string, Res>();
		for (int i = 0; i < array.Length; i++)
		{
			Cfg c = array[i];
			Res a = Run(cs, c);
			Res b = Run(cs, new Cfg
			{
				id = c.id,
				what = c.what,
				zeroG = c.zeroG,
				zeroGamma = c.zeroGamma,
				plasticity = c.plasticity,
				av = c.av,
				fixedDt = num
			});
			byCourant[c.id] = a;
			byPinned[c.id] = b;
			CT.Info("── " + c.id + ": " + c.what);
			CT.Info(string.Format("   Курант:      дрейф {0:F4} %  ", a.drift * 100.0) + string.Format("({0:F4} → {1:F4} Дж, время {2:F2} мкс, dt {3:E4} с)", a.Total0, a.Total1, a.time * 1000000.0, a.dtLast));
			CT.Info(string.Format("   закреплён:   дрейф {0:F4} %  ", b.drift * 100.0) + string.Format("({0:F4} → {1:F4} Дж, время {2:F2} мкс)", b.Total0, b.Total1, b.time * 1000000.0));
			CT.Info(string.Format("   действие: Σ|S| {0:E4} Па (БЕЗ сторожа); потенциал {1:E3} Дж ", b.sumAbsS, b.shear) + string.Format("(со сторожем); ∂p/∂e {0:E4}; пласт. работа {1:E3} Дж; ", b.dpde, b.plWork) + string.Format("макс. ε_p {0:E3}; вязкость на GPU {1}, ", b.maxEpsP, b.avOn ? "вкл" : "выкл") + "пластичность " + (b.plOn ? "вкл" : "выкл"));
		}
		CT.Info(string.Format("── B3: B1 при закреплённом шаге — {0:F4} % против ", byPinned["B1"].drift * 100.0) + string.Format("{0:F4} % по Куранту ⇒ вклад режима шага ", byCourant["B1"].drift * 100.0) + string.Format("{0:F4} п.п.", (byPinned["B1"].drift - byCourant["B1"].drift) * 100.0));
		CT.Info("── доказательства действия (всё прочитано С GPU):");
		bool t1 = CT.True(string.Format("B1: девиатора нет — Σ|S| {0:E4} Па ровно 0 ", byPinned["B1"].sumAbsS) + "(сторож G > 10³ обойдён)", byPinned["B1"].sumAbsS == 0.0);
		bool t2 = CT.True(string.Format("B1: Γ₀ дошёл нулём — ∂p/∂e {0:E4} ровно 0", byPinned["B1"].dpde), byPinned["B1"].dpde == 0.0);
		bool t3 = CT.True(string.Format("B1: пластичности нет — работа {0:E3} Дж ровно 0", byPinned["B1"].plWork), byPinned["B1"].plWork == 0.0);
		bool t4 = CT.True(string.Format("B2: Γ₀ восстановлен — ∂p/∂e {0:E4} НЕ ноль", byPinned["B2"].dpde), Math.Abs(byPinned["B2"].dpde) > 0.0);
		bool t5 = CT.True(string.Format("B4: девиатор восстановлен — Σ|S| {0:E4} Па НЕ ноль", byPinned["B4"].sumAbsS), byPinned["B4"].sumAbsS > 0.0);
		string b4vs1 = string.Format("B4 против B1: шагов {0} = {1}, ", byPinned["B4"].steps, byPinned["B1"].steps) + string.Format("время {0:F4} = {1:F4} мкс (контроль F3)", byPinned["B4"].time * 1000000.0, byPinned["B1"].time * 1000000.0);
		bool t6 = CT.True(b4vs1, byPinned["B4"].steps == byPinned["B1"].steps && Math.Abs(byPinned["B4"].time - byPinned["B1"].time) < 1e-12);
		bool t7 = CT.True(string.Format("B5: пластичность ДЕЙСТВУЕТ — работа {0:E3} Дж или ", byPinned["B5"].plWork) + string.Format("макс. ε_p {0:E3} НЕ ноль", byPinned["B5"].maxEpsP), byPinned["B5"].plWork > 0.0 || byPinned["B5"].maxEpsP > 0.0);
		if (!(t1 & t2 & t3 & t4 & t5 & t6 & t7))
		{
			CT.Info("⚠️ хотя бы одно доказательство действия не сошлось — ступень ниже читать нельзя");
		}
		CT.Info("⚠️ сторож G > 10³ Па: на B1/B2/B3 сдвиговый потенциал зануляется ИМ, а не физикой; полная внутренняя энергия при G = 0 считается по другой ветке KReduceDiag. Девиатор доказан обходом — Σ|S|.");
		double b1 = byPinned["B1"].drift;
		double b2 = byPinned["B2"].drift - b1;
		double b4 = byPinned["B4"].drift - b1;
		double b5 = byPinned["B5"].drift - b1;
		double b3 = byCourant["B1"].drift - b1;
		double controlled = b2 + b4;
		double residual = b5 - controlled;
		double literal = b3 + b2 + b4;
		double literalResidual = b5 - literal;
		CT.Info("── B6, замыкание:");
		CT.Info(string.Format("   контролируемое (закреплённый шаг): (B2−B1) {0:F4} + ", b2 * 100.0) + string.Format("(B4−B1) {0:F4} = {1:F4} п.п.; ", b4 * 100.0, controlled * 100.0) + string.Format("целое (B5−B1) = {0:F4} п.п.; остаток {1:F4} п.п. ", b5 * 100.0, residual * 100.0) + string.Format("({0:F1} % целого)", Math.Abs(b5) <= 1e-12 ? double.NaN : residual / b5 * 100.0));
		CT.Info(string.Format("   буквальное (формула ТЗ, с членом B3): сумма {0:F4} п.п.; ", literal * 100.0) + string.Format("остаток {0:F4} п.п.", literalResidual * 100.0));
		CT.Info(Math.Abs(residual) > Math.Abs(b5) * 0.2 ? "⛔ лестница НЕ замыкается: остаток больше пятой части целого ⇒ приписывать течь одному члену НЕЛЬЗЯ. Отчёт даёт остаток, а не адрес." : "✔ лестница замыкается: сумма ступеней объясняет целое ⇒ адрес читаем");
	}

	private void B7(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, Push);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
		using Flat2DSolver s = new Flat2DSolver(cs);
		s.ArtificialViscosity = false;
		s.Init(body.pos, body.vel, body.mat, mats, body.dx, lo, hi);
		s.Steps(Warm);
		Compare(s, "старт");
		s.Steps(Window);
		Compare(s, "конец");
	}

	private static void Compare(Flat2DSolver s, string label)
	{
		Vector2[] pos = s.ReadPositions();
		float[] mass = s.ReadMass();
		float[] rho = s.ReadDensity();
		int n = pos.Length;
		double[] sum = new double[n];
		double[] sumOverRho = new double[n];
		for (int i = 0; i < n; i++)
		{
			double sw = 0.0;
			double swRho = 0.0;
			for (int j = 0; j < n; j++)
			{
				double dx = pos[j].x - pos[i].x;
				double dy = pos[j].y - pos[i].y;
				double w = Wk(Math.Sqrt(dx * dx + dy * dy), s.H);
				if (w != 0.0)
				{
					sw += mass[j] * w;
					swRho += mass[j] / rho[j] * w;
				}
			}
			sum[i] = sw;
			sumOverRho[i] = swRho;
		}
		List<double> deviations = new List<double>();
		for (int k = 0; k < n; k++)
		{
			if (sumOverRho[k] >= 0.9999)
			{
				deviations.Add(Math.Abs(sum[k] - rho[k]) / rho[k]);
			}
		}
		if (deviations.Count == 0)
		{
			CT.Info("B7 " + label + ": внутренних частиц НЕТ — сравнивать не на чем");
			return;
		}
		deviations.Sort();
		double median = deviations[deviations.Count / 2];
		int idx = Mathf.Clamp((int)Math.Floor((float)(deviations.Count - 1) * 0.95f), 0, deviations.Count - 1);
		double p95 = deviations[idx];
		double max = deviations[deviations.Count - 1];
		CT.Info(string.Format("B7 {0}: внутренних {1} из {2} ({3:F1} %); ", label, deviations.Count, n, (double)deviations.Count * 100.0 / n) + string.Format("|ρ_сумм − ρ_неразр|/ρ: медиана {0:F4} %, 95-й перцентиль {1:F4} %, ", median * 100.0, p95 * 100.0) + string.Format("максимум {0:F4} %", max * 100.0));
	}

	private static double Wk(double r, double h)
	{
		double q = r / h;
		double norm = 10.0 / (7.0 * Math.PI * h * h);
		if (q < 1.0)
		{
			return norm * (1.0 - 1.5 * q * q + 0.75 * q * q * q);
		}
		if (q < 2.0)
		{
			return norm * 0.25 * (2.0 - q) * (2.0 - q) * (2.0 - q);
		}
		return 0.0;
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

	private Res Run(ComputeShader cs, Cfg c)
	{
		Res res = default(Res);
		float pushSpeed = c.atRest ? 0f : Push;
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(Nx, Ny, Dx, pushSpeed);
		if (c.uniformV != 0f)
		{
			for (int i = 0; i < body.vel.Length; i++)
			{
				body.vel[i] = new Vector2(c.uniformV, 0f);
			}
		}
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] mats = Mats(c.zeroG, c.zeroGamma);
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = c.av;
			s.Plasticity = c.plasticity;
			s.NoInteraction = c.noInteraction;
			s.EnergyFirstOrder = c.eFirstOrder;
			s.FixedTimeStep = c.fixedDt;
			s.Init(body.pos, body.vel, body.mat, mats, body.dx, lo, hi);
			s.Steps(Warm);
			Flat2DSolver.Diag m0 = s.Measure();
			s.SimState(out double dt0, out double t0, out double vmax0, out double cmax0);
			s.ResetProbe();
			s.Steps(Window);
			Flat2DSolver.Diag m1 = s.Measure();
			s.SimState(out double dt1, out double t1, out double vmax1, out double cmax1);
			s.ReadProbe(out int stateCalls, out double stateDt, out int kickCalls, out double kickDt);
			s.MeasureClosure(out double powHalf, out double sumMA2, out double powFull);
			s.MeasureParts(out double powerP, out double powerS, out double eInt, out double shear);
			s.ReadGpuFlags(out bool avOn, out bool plOn);
			Vector4[] stress = s.ReadStress();
			double sumAbsS = 0.0;
			for (int j = 0; j < stress.Length; j++)
			{
				sumAbsS += Math.Abs(stress[j].x) + Math.Abs(stress[j].y) + Math.Abs(stress[j].z);
			}
			float[] epsP = s.ReadPlasticStrain();
			double maxEpsP = 0.0;
			for (int k = 0; k < epsP.Length; k++)
			{
				maxEpsP = Math.Max(maxEpsP, epsP[k]);
			}
			res.sumAbsS = sumAbsS;
			res.maxEpsP = maxEpsP;
			res.kin1 = m1.kinetic;
			res.int1 = m1.internalE;
			res.drift = m1.internalE + m1.kinetic - (m0.internalE + m0.kinetic);
			res.kin0 = m0.kinetic;
			res.int0 = m0.internalE;
			res.absDelta = Math.Abs(res.drift) / Math.Max(Math.Abs(m0.internalE + m0.kinetic), 1e-30);
			res.sumMA2 = sumMA2;
			res.shear = shear;
			res.dpde = s.ReadDpDe();
			res.plWork = m1.plasticWork;
			res.hourglass = m1.hourglass;
			res.dtFirst = stateDt;
			res.dtLast = dt1;
			res.time = t1 - t0;
			res.steps = stateCalls;
			res.avOn = avOn;
			res.plOn = plOn;
		}
		return res;
	}

	private ComputeShader LoadShader()
	{
		return solverShader != null ? solverShader : null;
	}
}
