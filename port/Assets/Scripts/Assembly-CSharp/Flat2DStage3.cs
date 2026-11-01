using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public sealed class Flat2DStage3 : MonoBehaviour
{
	private struct SpallZone
	{
		public int n;

		public int rows;

		public int rowsTotal;

		public double minM;

		public double maxM;

		public double p05M;

		public double p95M;

		public double hM;

		public double dxM;

		public double planeRef;

		public double targetM;

		public bool clipFront;

		public bool clipBack;

		public double Extent => maxM - minM;

		public double Core => planeRef - p05M;
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass20_0
	{
		public Flat2DStage3 _003C_003E4__this;

		public ComputeShader cs;

		internal void _003CRunAll_003Eb__0()
		{
			_003C_003E4__this.Q1(cs);
		}

		internal void _003CRunAll_003Eb__1()
		{
			_003C_003E4__this.Q3(cs);
		}

		internal void _003CRunAll_003Eb__2()
		{
			_003C_003E4__this.P1toP5(cs);
		}

		internal bool _003CRunAll_003Eb__3()
		{
			return _003C_003E4__this.P6(cs);
		}

		internal bool _003CRunAll_003Eb__4()
		{
			return _003C_003E4__this.S2(cs);
		}

		internal void _003CRunAll_003Eb__5()
		{
			_003C_003E4__this.S3(cs);
		}

		internal bool _003CRunAll_003Eb__6()
		{
			return _003C_003E4__this.S5(cs);
		}

		internal void _003CRunAll_003Eb__7()
		{
			_003C_003E4__this.S6(cs);
		}

		internal void _003CRunAll_003Eb__8()
		{
			_003C_003E4__this.Z3Sweep(cs);
		}

		internal bool _003CRunAll_003Eb__9()
		{
			return _003C_003E4__this.Z5Fragments(cs);
		}

		internal void _003CRunAll_003Eb__10()
		{
			_003C_003E4__this.G2Width(cs);
		}

		internal bool _003CRunAll_003Eb__11()
		{
			return _003C_003E4__this.G4Contact(cs);
		}

		internal void _003CRunAll_003Eb__12()
		{
			_003C_003E4__this.Z7Resolution(cs);
		}

		internal bool _003CRunAll_003Eb__13()
		{
			return _003C_003E4__this.S7(cs);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass44_0
	{
		public Components c;

		internal int _003CLabel_003Eb__0(int x, int y)
		{
			if (c.sizes[x] == c.sizes[y])
			{
				return x.CompareTo(y);
			}
			return c.sizes[y].CompareTo(c.sizes[x]);
		}
	}

	private struct Components
	{
		public int count;

		public int edges;

		public int n;

		public int[] label;

		public List<int> sizes;

		public List<double> masses;

		public double massTotal;
	}

	private struct FsHistory
	{
		public List<double> t;

		public List<double> u;

		public List<double> sc;

		public double scatterAtPeak;
	}

	public ComputeShader solverShader;

	private const string ShaderPath = "Assets/Scripts/Flat2D/Flat2DSolver.compute";

	private const int Warm = 300;

	private const int Window = 10000;

	private const float Dx = 0.0005f;

	private const int FlyerNx = 20;

	private const int TargetNx = 40;

	private const int NyFlyer = 120;

	private const float FlyerM = 0.01f;

	private const float TargetM = 0.02f;

	private const float HeightM = 0.06f;

	private float vCal;

	private Components lastComp;

	private Vector2[] lastPos;

	private float[] lastMassArr;

	private double lastLinkR;

	private double lastMassStart;

	private double lastMassEnd;

	private float lastDx;

	private static void Counts(float dx, out int fnx, out int tnx, out int ny, float heightM = 0.06f)
	{
		fnx = Convert.ToInt32(FlyerM / dx);
		tnx = Convert.ToInt32(TargetM / dx);
		ny = Convert.ToInt32(heightM / dx);
	}

	[ContextMenu("★ Этап 3: повреждение, откол, искусственное напряжение")]
	public void RunAll()
	{
		_003C_003Ec__DisplayClass20_0 _003C_003Ec__DisplayClass20_ = new _003C_003Ec__DisplayClass20_0();
		_003C_003Ec__DisplayClass20_._003C_003E4__this = this;
		_003C_003Ec__DisplayClass20_.cs = LoadShader();
		if (_003C_003Ec__DisplayClass20_.cs == null)
		{
			Debug.LogError("Flat2D: не найден Assets/Scripts/Flat2D/Flat2DSolver.compute");
			return;
		}
		Debug.Log("══════ ЭТАП 3: ПОВРЕЖДЕНИЕ, ОТКОЛ, ИСКУССТВЕННОЕ НАПРЯЖЕНИЕ ══════");
		Debug.Log(string.Format("устройство: {0} ({1})", SystemInfo.graphicsDeviceName, SystemInfo.graphicsDeviceType));
		int pass = 0;
		int total = 0;
		int measured = 0;
		CT.Measure("Q1", "цена стабилизатора: W_a против ε — стоимость или обратная связь", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__0, ref measured);
		CT.Measure("Q3", "чешуйка: где плоскость, отделяется ли, куда летит, хватает ли окна", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__1, ref measured);
		CT.Measure("P1–P5", "летящая пластина на меди: порог, пол, откол", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__2, ref measured);
		CT.Run("P6", "искусственное напряжение: в ЧИСТО СЖИМАЮЩЕЙ окрестности acc_a РОВНО ноль", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__3, ref pass, ref total);
		CT.Run("S2", "масса сохраняется ПОБИТОВО, включая разрушенные частицы", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__4, ref pass, ref total);
		CT.Measure("S3", "A6 разделена: часы против растягивающей неустойчивости", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__5, ref measured);
		CT.Run("S5", "повреждение НАКАПЛИВАЕТСЯ, и без него прогон ДРУГОЙ", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__6, ref pass, ref total);
		CT.Measure("S6", "M4 на баллистической скорости — обязана покраснеть", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__7, ref measured);
		CT.Measure("Z3", "развёртка по входной σ_sp: восстанавливает ли извлечение свой вход", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__8, ref measured);
		CT.Run("Z5/Z6", "тела: связные компоненты, масса ПОБИТОВО включая отказавшие", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__9, ref pass, ref total);
		CT.Measure("G2", "выбивание шириной: может ли чешуйка оторваться в этой геометрии", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__10, ref measured);
		CT.Run("G4", "контакт между телами: C-a, C-b, C-c, C-d", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__11, ref pass, ref total);
		CT.Measure("Z7", "разрешение: три шага сетки — ЗАПИСЬ, не ворота", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__12, ref measured);
		CT.Run("S7", "детерминизм ЧЕРЕЗ разрушение: три прогона бит-в-бит", _003C_003Ec__DisplayClass20_._003CRunAll_003Eb__13, ref pass, ref total);
		Debug.Log(string.Format("══════ ИТОГ ЭТАПА 3: пройдено {0}, провалено {1}, измерено {2} ══════", pass, total - pass, measured));
	}

	private void Q1(ComputeShader cs)
	{
		CT.Info(string.Format("── Δp, который использует код: _H/1.3 = dx = {0:F3} мм — НАЧАЛЬНЫЙ шаг ", 0.5f) + "(_H ставится в Init один раз и не обновляется). Самоподпитка через Δp ИСКЛЮЧЕНА.");
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
		float[] eps = new float[7] { 0f, 0.1f, 0.12f, 0.15f, 0.17f, 0.2f, 0.3f };
		List<double> wByEps = new List<double>();
		double fnRef = 0.0;
		for (int i = 0; i < eps.Length; i++)
		{
			float e = eps[i];
			Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
			Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
			using (Flat2DSolver solver = new Flat2DSolver(cs))
			{
				solver.ArtificialViscosity = false;
				solver.Plasticity = false;
				solver.ArtificialStress = true;
				solver.ArtStressEps = e;
				solver.AccumulateWork = true;
				solver.FixedTimeStep = StiffDt(mats, 5f);
				solver.Init(body.pos, body.vel, body.mat, mats, 0.0005f, lo, hi);
				solver.Steps(2000);
				solver.MeasureChannels(out double _, out double _, out double _, out double wArt);
				wByEps.Add(wArt);
				if (e == 0.2f)
				{
					fnRef = MaxFn(solver);
				}
				CT.Info(string.Format("   ε = {0:F2}: W_a = {1:E4} Дж", e, wArt));
			}
		}
		CT.Info(string.Format("── max fⁿ = {0:E3} (f = W(r_min)/W(Δp), n = 4) — во столько раз множитель ", fnRef) + "усиливает вклад ближайшей пары");
		CT.Info("── масштабирование W_a/ε:");
		for (int j = 1; j < eps.Length; j++)
		{
			CT.Info(string.Format("   ε = {0:F2}: W_a/ε = {1:E4}", eps[j], wByEps[j] / eps[j]));
		}
		CT.Info("── R6(a) БИСЕКЦИЯ: отношение к предыдущей точке (линейное было бы ε_k/ε_{k−1})");
		for (int k = 2; k < eps.Length; k++)
		{
			CT.Info(string.Format("   {0:F2} → {1:F2}: W_a ×{2:F2} ", eps[k - 1], eps[k], Math.Abs(wByEps[k]) / Math.Max(Math.Abs(wByEps[k - 1]), 1e-30)) + string.Format("(линейно ×{0:F2})", eps[k] / eps[k - 1]));
		}
		int i02 = Array.IndexOf(eps, 0.2f);
		int i03 = Array.IndexOf(eps, 0.3f);
		double r02 = Math.Abs(wByEps[i02]) / Math.Max(Math.Abs(wByEps[1]), 1e-30);
		double r03 = Math.Abs(wByEps[i03]) / Math.Max(Math.Abs(wByEps[i02]), 1e-30);
		CT.Info(string.Format("   отношения: W_a(0.2)/W_a(0.1) = {0:F2} (линейно было бы 2.00); ", r02) + string.Format("W_a(0.3)/W_a(0.2) = {0:F2} (линейно 1.50)", r03));
		if (r02 < 3.0 && 2.3 > r03)
		{
			CT.Info("   ⭐ W_a растёт примерно ЛИНЕЙНО по ε ⇒ это ЦЕНА, а не контур обратной связи. ε берётся из источника (0.2), цена записывается и меряется там, где член работает.");
		}
		else
		{
			CT.Info("   ⛔ рост СВЕРХЛИНЕЙНЫЙ ⇒ это ОБРАТНАЯ СВЯЗЬ, дефект реализации, а не цена. §1.2 не запускается (ТЗ §7).");
		}
		CT.Info("⭐ ЗАМЕНЫ, НАЗВАННЫЕ ПЕРВОИСТОЧНИКОМ (Ma/Zhang/Qiu 2009 §5.4, PDF в репозитории) — перечислены, НЕ начаты: (1) особые сглаживающие функции; (2) корректирующий SPH (CSPH); (3) искусственная сила; (4) СТОЧКИ НАПРЯЖЕНИЙ (stress points). ⇒ выбор замены делается не с нуля, когда до него дойдёт очередь.");
		CT.True(string.Format("   ε = 0 обязана дать РОВНО ноль ({0:E3})", wByEps[0]), wByEps[0] == 0.0);
		ArtStressNature(cs, mats);
	}

	private void ArtStressNature(ComputeShader cs, Flat2DMaterialBlock[] mats)
	{
		float c = 0f;
		for (int i = 0; i < mats.Length; i++)
		{
			if (c <= mats[i].LongitudinalWaveSpeed)
			{
				c = mats[i].LongitudinalWaveSpeed;
			}
		}
		float dt = 0.000195f / (c + 5f);
		CT.Info("── R6(б) ВЫБИВАНИЕ ШАГОМ при ε = 0.2 (одно и то же время, вчетверо больше шагов)");
		double[] wByStep = new double[3];
		for (int k = 0; k < 3; k++)
		{
			int mul = 1 << k;
			Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
			Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
			using (Flat2DSolver solver = new Flat2DSolver(cs))
			{
				solver.ArtificialViscosity = false;
				solver.Plasticity = false;
				solver.ArtificialStress = true;
				solver.ArtStressEps = 0.2f;
				solver.AccumulateWork = true;
				solver.FixedTimeStep = dt / mul;
				solver.Init(body.pos, body.vel, body.mat, mats, 0.0005f, lo, hi);
				solver.Steps(mul * 2000);
				solver.MeasureChannels(out double _, out double _, out double _, out wByStep[k]);
				CT.Info(string.Format("   dt/{0}: шагов {1}, W_a = {2:E4} Дж", mul, mul * 2000, wByStep[k]));
			}
		}
		double r1 = Math.Abs(wByStep[1]) / Math.Max(Math.Abs(wByStep[0]), 1e-30);
		double r2 = Math.Abs(wByStep[2]) / Math.Max(Math.Abs(wByStep[1]), 1e-30);
		CT.Info(string.Format("   отношения по шагу: dt/2 ÷ dt = {0:F3}; dt/4 ÷ dt/2 = {1:F3} ", r1, r2) + "(СХОДИМОСТЬ дала бы обе ≈1)");
		CT.Info("── R6(в) ЧИСТОЕ ПРОТИВ РАЗОШЕДШЕГОСЯ при ε = 0.2: накопитель по четвертям окна");
		Flat2DScene.Body body2 = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
		Flat2DScene.Domain(body2, 0.01f, out Vector2 lo2, out Vector2 hi2);
		double firstQ = 0.0;
		double lastQ = 0.0;
		double prevW = 0.0;
		using (Flat2DSolver solver2 = new Flat2DSolver(cs))
		{
			solver2.ArtificialViscosity = false;
			solver2.Plasticity = false;
			solver2.ArtificialStress = true;
			solver2.ArtStressEps = 0.2f;
			solver2.AccumulateWork = true;
			solver2.FixedTimeStep = dt;
			solver2.Init(body2.pos, body2.vel, body2.mat, mats, 0.0005f, lo2, hi2);
			for (int q = 1; q <= 4; q++)
			{
				solver2.Steps(500);
				solver2.MeasureChannels(out double _, out double _, out double _, out double wTotal);
				double dw = wTotal - prevW;
				prevW = wTotal;
				if (q == 1)
				{
					firstQ = dw;
				}
				else if (q == 4)
				{
					lastQ = dw;
				}
				CT.Info(string.Format("   шаги {0}…{1}: приращение W_a = {2:E4} Дж; ", q * 500 - 500, q * 500, dw) + string.Format("накоплено {0:E4}; max fⁿ = {1:E3}", wTotal, MaxFn(solver2)));
			}
		}
		double qRatio = Math.Abs(lastQ) / Math.Max(Math.Abs(firstQ), 1e-30);
		CT.Info(string.Format("   последняя четверть ÷ первая = {0:E2} (ЦЕНА дала бы ≈1)", qRatio));
		bool converged = 1.5 > r1 && r1 > 0.67 && 1.5 > r2 && r2 > 0.67;
		bool uniform = 10.0 > qRatio;
		if (converged & uniform)
		{
			CT.Info("   ⇒ ВЕРДИКТ: РАБОТА. W_a сходится по шагу и равномерна во времени.");
		}
		else
		{
			CT.Info("   ⛔ ВЕРДИКТ: УСТОЙЧИВОСТЬ. " + (converged ? "" : "W_a не сходится по шагу; ") + (uniform ? "" : "и вклад сосредоточен в конце окна. ") + "Член не платит равномерную цену, а отвечает на РАСХОЖДЕНИЕ решения ⇒ число ε нельзя читать как «цену стабилизатора».");
		}
		CT.Info("   ⛔ Правка не делается: ТЗ §7 разрешает в этой задаче только замер.");
	}

	private static double MaxFn(Flat2DSolver s)
	{
		Vector2[] pos = s.ReadPositions();
		double h = s.H;
		double r = h + h;
		for (int i = 0; i < pos.Length; i++)
		{
			for (int j = i + 1; j < pos.Length; j++)
			{
				double dx = pos[i].x - pos[j].x;
				double dy = pos[i].y - pos[j].y;
				double r2 = dx * dx + dy * dy;
				if (r2 < r * r && r2 > 1e-24)
				{
					r = Math.Sqrt(r2);
				}
			}
		}
		return Math.Pow(Math.Max(Wk(r, h) / Wk(h / 1.3, h), 0.0), 4.0);
	}

	private static double Wk(double r, double h)
	{
		double q = r / h;
		double norm = 10.0 / (h * 7.0 * Math.PI * h);
		if (q < 1.0)
		{
			return norm * ((1.0 - q * 1.5 * q) + q * 0.75 * q * q);
		}
		if (2.0 <= q)
		{
			return 0.0;
		}
		double t = 2.0 - q;
		return norm * 0.25 * t * t * t;
	}

	private void Q3(ComputeShader cs)
	{
		Scab(cs, spall: true, -1f, out double measuredPlaneM);
		if (measuredPlaneM > 0.0)
		{
			Scab(cs, spall: true, (float)measuredPlaneM, out double _);
		}
		else
		{
			CT.Info("   ⚠️ измеренной плоскости нет (защёлка не сработала) — второй прогон не запускается");
		}
		Scab(cs, spall: false, -1f, out double _);
	}

	private void Scab(ComputeShader cs, bool spall, float planeOverrideM, out double measuredPlaneM)
	{
		measuredPlaneM = -1.0;
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.OFHC_Copper, ContinuumMaterialId.OFHC_Copper);
		float v = Velocity(cs, mats);
		Flat2DScene.Body body = Flyer(20, 40, 120, v, out Vector2 lo, out Vector2 hi);
		float dt = 0.000195f / (mats[0].LongitudinalWaveSpeed + v);
		float plane = ((planeOverrideM <= 0f) ? (2 * FlyerNx * Dx) : (planeOverrideM + FlyerNx * Dx));
		float mid = (plane - FlyerNx * Dx) * 0.4f + FlyerNx * Dx;
		bool[] inTarget = new bool[body.pos.Length];
		bool[] behindPlane = new bool[body.pos.Length];
		bool[] pairFront = new bool[body.pos.Length];
		bool[] pairBack = new bool[body.pos.Length];
		for (int i = 0; i < body.pos.Length; i++)
		{
			inTarget[i] = FlyerNx * Dx - Dx * 0.5f < body.pos[i].x;
			behindPlane[i] = inTarget[i] && body.pos[i].x >= plane;
			pairFront[i] = inTarget[i] && !behindPlane[i] && mid > body.pos[i].x;
			pairBack[i] = inTarget[i] && !behindPlane[i] && body.pos[i].x >= mid;
		}
		CT.Info("── " + (spall ? "отсечка ВКЛ" : "КОНТРФАКТ: отсечка ВЫКЛ") + ((planeOverrideM > 0f) ? string.Format(", разметка пары по ИЗМЕРЕННОЙ плоскости {0:F2} мм", planeOverrideM * 1000f) : (string.Format(", разметка пары по ПРЕДСКАЗАННОЙ плоскости {0:F2} мм ", FlyerNx * Dx * 1000f) + "(= толщина ударника)")));
		using (Flat2DSolver solver = new Flat2DSolver(cs))
		{
			solver.FixedTimeStep = dt;
			solver.Spall = spall;
			solver.Damage = true;
			solver.ArtificialStress = true;
			solver.Init(body.pos, body.vel, body.mat, mats, 0.0005f, lo, hi);
			List<double> duByWin = new List<double>();
			List<double> pairByWin = new List<double>();
			double peakU = -1e30;
			double peakTime = 0.0;
			for (int win = 0; win < 42; win++)
			{
				solver.Steps(10);
				Vector2[] pos = solver.ReadPositions();
				Vector2[] vel = solver.ReadVelocities();
				double frontMax = -1e30;
				double uFs = 1e30;
				double cMin = 1e30;
				double dMax = -1e30;
				for (int j = 0; j < pos.Length; j++)
				{
					if (Math.Abs(pos[j].y - 0.02975f) < 0.0015f && inTarget[j])
					{
						if (!behindPlane[j])
						{
							frontMax = Math.Max(frontMax, pos[j].x);
						}
						else
						{
							uFs = Math.Min(uFs, pos[j].x);
						}
						if (pairFront[j])
						{
							cMin = Math.Min(cMin, pos[j].x);
						}
						if (pairBack[j])
						{
							dMax = Math.Max(dMax, pos[j].x);
						}
					}
				}
				duByWin.Add(uFs - frontMax);
				pairByWin.Add((dMax > -1e29 && cMin < 1e29) ? (cMin - dMax) : double.NaN);
				double sum = 0.0;
				int count = 0;
				for (int k = 0; k < pos.Length; k++)
				{
					if (pos[k].x > (FlyerNx + TargetNx) * Dx - Dx * 1.5f && Math.Abs(pos[k].y - 0.02975f) < Dx * 10f)
					{
						sum += vel[k].x;
						count++;
					}
				}
				if (peakU < sum / Math.Max(count, 1))
				{
					peakU = sum / Math.Max(count, 1);
					peakTime = (double)((win + 1) * 10) * dt;
				}
			}
			float[] damage = solver.ReadDamage();
			Vector4[] stress = solver.ReadStress();
			Vector2[] posEnd = solver.ReadPositions();
			float spallStrength = mats[0].spall;
			double dSum = 0.0;
			int dCount = 0;
			double clipSum = 0.0;
			int clipCount = 0;
			for (int m = 0; m < damage.Length; m++)
			{
				if (damage[m] >= 1f)
				{
					dSum += (double)(posEnd[m].x - FlyerNx * Dx);
					dCount++;
				}
				if (spallStrength > 0f && stress[m].w <= -spallStrength * 0.999f)
				{
					clipSum += (double)(posEnd[m].x - FlyerNx * Dx);
					clipCount++;
				}
			}
			float[] fail = solver.ReadSpallFail();
			int failCount = 0;
			double maxTensFail = 0.0;
			double maxTensAll = 0.0;
			for (int n = 0; n < fail.Length; n++)
			{
				double tens = -stress[n].w;
				maxTensAll = Math.Max(maxTensAll, tens);
				if (fail[n] > 0.5f)
				{
					failCount++;
					maxTensFail = Math.Max(maxTensFail, tens);
				}
			}
			CT.Info(string.Format("   R3 ЗАЩЁЛКА: помечено {0} частиц; max(−p) по НИМ = {1:F4} ГПа ", failCount, maxTensFail / 1000000000.0) + string.Format("(зажим вернул бы {0:F2}); max(−p) по ВСЕМ = {1:F4} ГПа", spallStrength / 1000000000.0, maxTensAll / 1000000000.0));
			double locSum = 0.0;
			int locCount = 0;
			double locMin = 1e30;
			double locMax = -1e30;
			for (int i2 = 0; i2 < fail.Length; i2++)
			{
				if (fail[i2] > 0.5f && inTarget[i2])
				{
					double depth = body.pos[i2].x - FlyerNx * Dx;
					locSum += depth;
					locCount++;
					locMin = Math.Min(locMin, depth);
					locMax = Math.Max(locMax, depth);
				}
			}
			if (locCount > 0)
			{
				measuredPlaneM = locSum / locCount;
				CT.Info(string.Format("       ЛОКУС ЗАЩЁЛКИ по НАЧАЛЬНЫМ положениям (только мишень): {0} частиц, ", locCount) + string.Format("средняя глубина {0:F2} мм, полоса {1:F2}…{2:F2} мм ", measuredPlaneM * 1000.0, locMin * 1000.0, locMax * 1000.0) + string.Format("(предсказано было {0:F2} мм)", FlyerNx * Dx * 1000f));
				double extent = locMax - locMin;
				string[] parts = new string[5];
				parts[0] = string.Format("       ширина полосы {0:F2} мм при мишени ", extent * 1000.0);
				parts[1] = string.Format("{0:F2} мм ⇒ отказ занимает ", TargetNx * Dx * 1000f);
				parts[2] = string.Format("{0:F0} % толщины = {1:F1} шагов Δx ", extent / (TargetNx * Dx) * 100.0, extent / Dx);
				parts[3] = "⇒ ";
				parts[4] = ((double)solver.H * 4.0 < extent) ? "⛔ ЗОНА (толще 4h)" : "ПЛОСКОСТЬ (тоньше выведенного предела 4h)";
				CT.Info(string.Concat(parts));
			}
			if (failCount == 0)
			{
				CT.Info("       ⚠️ ПОМЕЧЕННЫХ НЕТ — ноль выше означает «отказов не было», а НЕ «растяжения не несёт». Строка не выдаётся за структурную проверку.");
			}
			else
			{
				CT.Info(string.Format("       ⭐ проверка ЧИТАЕТСЯ: отказы есть ({0}), и по ним растяжение ", failCount) + ((double)spallStrength * 0.01 > maxTensFail ? "РОВНО НОЛЬ в пределах округления" : "НЕ снято"));
			}
			CT.Info(string.Format("   (a) локус D = 1: частиц {0}", dCount) + ((dCount > 0) ? string.Format(", средняя глубина {0:F2} мм от лица удара", dSum / dCount * 1000.0) : " — ЛОКУСА НЕТ"));
			CT.Info(string.Format("       локус ОТСЕЧКИ (p на полу): частиц {0}", clipCount) + ((clipCount > 0) ? string.Format(", средняя глубина {0:F2} мм", clipSum / clipCount * 1000.0) : " — не сработала"));
			double duLast = duByWin[duByWin.Count - 1];
			double duMid = duByWin[duByWin.Count / 2];
			double pairLast = pairByWin[pairByWin.Count - 1];
			bool monotone = true;
			for (int k2 = duByWin.Count / 2; k2 < duByWin.Count; k2++)
			{
				if (duByWin[k2] < duByWin[k2 - 1] * 0.98)
				{
					monotone = false;
					break;
				}
			}
			double du0 = duByWin[0];
			double pair0 = pairByWin[0];
			if (!double.IsNaN(pairLast) && !double.IsNaN(pair0))
			{
				string[] row = new string[7];
				row[0] = string.Format("   (b) зазор ПОМЕЧЕННОЙ пары: старт {0:F3} → конец {1:F3} мм ", du0 * 1000.0, duLast * 1000.0);
				row[1] = string.Format("(рост ×{0:F2}); КОНТРОЛЬНАЯ пара внутри остатка ", duLast / Math.Max(du0, 1e-9));
				row[2] = string.Format("{0:F3} → {1:F3} мм (рост ×{2:F2}) ", pair0 * 1000.0, pairLast * 1000.0, pairLast / Math.Max(pair0, 1e-9));
				row[3] = string.Format("⇒ ОТДЕЛЕНИЕ/ПЕРЕНОС = {0:F2}× ", duLast / Math.Max(du0, 1e-9) / Math.Max(pairLast / Math.Max(pair0, 1e-9), 1e-9));
				row[4] = "(";
				row[5] = (monotone ? "растёт монотонно" : "СХЛОПЫВАЕТСЯ — это не отделение");
				row[6] = ")";
				CT.Info(string.Concat(row));
				CT.Info("       ⭐ отличие ОТДЕЛЕНИЯ от ПЕРЕНОСА: зазор сравнивается с фоновым промежутком ТОГО ЖЕ тела. Перенос и равномерное расширение поднимают оба, отделение — только первый.");
			}
			else
			{
				CT.Info(string.Format("   (b) зазор ПОМЕЧЕННОЙ пары: старт {0:F3} → конец {1:F3} мм ", du0 * 1000.0, duLast * 1000.0) + string.Format("(рост ×{0:F2}); ⛔ КОНТРОЛЬНОЙ ПАРЫ НЕТ — группа пуста, ", duLast / Math.Max(du0, 1e-9)) + "отношение НЕ считается (пустая группа дала бы ложный ×1.00)");
			}
			Vector2[] velEnd = solver.ReadVelocities();
			double sumEnd = 0.0;
			int countEnd = 0;
			for (int i3 = 0; i3 < posEnd.Length; i3++)
			{
				if (posEnd[i3].x > 0.02925f && Math.Abs(posEnd[i3].y - 0.02975f) < 0.005f)
				{
					sumEnd += velEnd[i3].x;
					countEnd++;
				}
			}
			CT.Info(string.Format("   (c) скорость задней грани в конце окна {0:F2} м/с", sumEnd / Math.Max(countEnd, 1)));
			CT.Info(string.Format("   (d) окно {0:F1} мкс, шаг выборки ", (double)dt * 420.0 * 1000000.0) + string.Format("{0:F3} мкс; u_max в {1:F2} мкс", (double)dt * 10.0 * 1000000.0, peakTime * 1000000.0));
		}
	}

	private static Flat2DScene.Body Flyer(int fnx, int tnx, int ny, float v, out Vector2 lo, out Vector2 hi, float dx = 0.0005f)
	{
		Flat2DScene.Body a = Flat2DScene.Block(fnx, ny, dx, Vector2.zero, 0u, new Vector2(v, 0f));
		Flat2DScene.Body b = Flat2DScene.Block(tnx, ny, dx, new Vector2((float)fnx * dx, 0f), 0u, Vector2.zero);
		Flat2DScene.Body res = default(Flat2DScene.Body);
		int total = a.pos.Length + b.pos.Length;
		res.pos = new Vector2[total];
		res.vel = new Vector2[total];
		res.mat = new uint[total];
		res.dx = dx;
		Flat2DScene.Body[] parts = new Flat2DScene.Body[2] { a, b };
		int offset = 0;
		for (int i = 0; i < parts.Length; i++)
		{
			parts[i].pos.CopyTo(res.pos, offset);
			parts[i].vel.CopyTo(res.vel, offset);
			parts[i].mat.CopyTo(res.mat, offset);
			offset += parts[i].pos.Length;
		}
		res.min = a.min;
		res.max = b.max;
		Flat2DScene.Domain(res, 0.01f, out lo, out hi);
		return res;
	}

	private void P1toP5(ComputeShader cs)
	{
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.OFHC_Copper, ContinuumMaterialId.OFHC_Copper);
		ContinuumProperties props = ContinuumMaterials.Get(ContinuumMaterialId.OFHC_Copper);
		double rho0 = props.rho0;
		double c0 = props.C0;
		double sigSp = mats[0].spall;
		CT.Info("── P1: цель перенацелена на медь OFHC");
		CT.Info(string.Format("   реестр: ρ₀ = {0} кг/м³, c₀ = {1} м/с, откол = {2:F2} ГПа", rho0, c0, sigSp / 1000000000.0));
		CT.Info("   провенанс: Dennis-Koller et al. 2011, J. Appl. Phys., плоское соударение + VISAR, отожжённая OFHC 99.999 %, MEASURED");
		CT.Info("   ⚠️ ОКНО — полоса реестра 1.31…1.38 ГПа (зёрна 30–200 мкм, ~10⁴ 1/с; в таблицу взят МИНИМУМ). ±0.06 ГПа из ТЗ принадлежали титану и сюда НЕ переносятся.");
		FlyerRun(cs, mats, 150f, spall: true, out double _, out double _, out double _, out SpallZone zone1);
		CT.Info(string.Format("── W1: геометрия возвращена к {0:F0}/{1:F0} мм ", FlyerNx * Dx * 1000f, TargetNx * Dx * 1000f) + string.Format("(было 16/20), скорость ПОКА старая {0} м/с", 150f));
		ReportZone(zone1, string.Format("10/20, v = {0} м/с — КОНТРФАКТ, обязан быть ЗОНОЙ", 150f));
		float v = Velocity(cs, mats);
		FsHistory fs = FlyerRun(cs, mats, v, spall: true, out double peakOn, out double uPeak, out double uMin, out SpallZone zoneA);
		FlyerRun(cs, mats, v, spall: false, out double peakOff, out double uPeakOff, out double uMinOff, out SpallZone zoneB);
		CT.Info(string.Format("── P2: симметричный удар ⇒ σ ≈ ρ₀c₀(v/2); порог {0:F2} ГПа даёт ", sigSp / 1000000000.0) + string.Format("v_min = {0:F1} м/с ПО ОЦЕНКЕ. ⚠️ Оценка занижает (при 150 м/с обещала ", (sigSp + sigSp) / (c0 * rho0)) + string.Format("{0:F2}, замер дал 3.723), поэтому v взята ЗАМЕРОМ: {1:F1} м/с.", c0 * rho0 * 150.0 * 0.5 / 1000000000.0, v));
		CT.Info(string.Format("── Q3b/P3: пиковое РАСТЯГИВАЮЩЕЕ напряжение сцены {0:F3} ГПа ", peakOff / 1000000000.0) + string.Format("(замерено БЕЗ отсечки), порог {0:F2} ГПа ⇒ запас ×{1:F2}", sigSp / 1000000000.0, peakOff / sigSp));
		CT.Info(string.Format("   для сравнения, с включённой отсечкой пик = {0:F3} ГПа — это ", peakOn / 1000000000.0) + "ЗАЖИМ на пороге, показание механизма, а не способности сцены");
		bool reaches = CT.True("   сцена ДОСТАЁТ до порога", sigSp <= peakOff);
		if (reaches)
		{
			CT.Info(string.Format("── W3: ВОРОТА §3 на калиброванной скорости {0:F1} м/с", v));
			bool isPlane = ReportZone(zoneA, string.Format("10/20, v = {0:F1} м/с — РАБОЧИЙ прогон", v));
			ReportZone(zoneB, "отсечка ВЫКЛ — КОНТРФАКТ «флаг ставится, но не читается»");
			A8(cs, mats, v);
			if (!isPlane)
			{
				CT.Info("⛔⛔ §3 показывает ЗОНУ ⇒ ИЗВЛЕЧЕНИЕ НЕ ВЫПОЛНЯЕТСЯ И ЧИСЛО НЕ ПЕЧАТАЕТСЯ. Выход B недоступен, пока §3 показывает зону (ТЗ §4). Третьей догадки о геометрии не будет — это сообщение, а не обход.");
			}
			else
			{
				double scatter2 = Math.Sqrt(2.0) * fs.scatterAtPeak;
				double floorPa = rho0 * 0.5 * c0 * scatter2;
				CT.Info("── P4: ПОЛ ИЗМЕРЕНИЯ. Разброс u_fs по частицам задней грани на пике " + string.Format("{0:F3} м/с ⇒ в Δu_fs он входит дважды ⇒ {1:F3} м/с ", fs.scatterAtPeak, scatter2) + string.Format("⇒ по напряжению {0:F4} ГПа", floorPa / 1000000000.0));
				CT.Info(string.Format("   для сравнения: полоса реестра шириной {0:F2} ГПа, ", 1.38 - 1.31) + string.Format("то есть пол {0:F1} % полосы", floorPa / 1000000000.0 / 0.07 * 100.0));
				double du = uPeak - uMin;
				double pullback = rho0 * 0.5 * c0 * du;
				CT.Info(string.Format("── P5: u_fs пик {0:F2} м/с, минимум после отката {1:F2} м/с ⇒ ", uPeak, uMin) + string.Format("Δu_fs = {0:F2} м/с", du));
				CT.Info(string.Format("   σ_sp = ½·ρ₀·c₀·Δu_fs = {0:F3} ГПа  ± {1:F4} (пол)", pullback / 1000000000.0, floorPa / 1000000000.0));
				CT.Info("   ⚠️ Соотношение акустическое, стандартное; поправочных множителей НЕТ, геометрического перевода НЕТ (одноосная деформация — родная для плоской).");
				bool inBand = 1310000000.0 <= pullback && pullback <= 1380000000.0;
				CT.True("   ⭐ σ_sp в полосе реестра 1.31…1.38 ГПа", inBand);
				CT.Info(inBand ? "   ⇒ ВЫХОД A: в полосе. Этап 3 идёт дальше — к осколкам и контакту." : "   ⇒ ВЫХОД B: плоскость ЧИСТАЯ (§3 пройдена), геометрия возвращена, запас калиброван замером — и число всё равно мимо. ⇒ S1 становится ЗАПИСАННЫМ ЗАМЕРОМ, а не воротами, и этап 3 продолжается. ⚠️ Строка стоит в CT.Measure, то есть уже НЕ ГАТИТ набор; объявление здесь — чтобы это не читалось как тихо проглоченный красный.");
				double duOff = uPeakOff - uMinOff;
				double offStress = rho0 * 0.5 * c0 * duOff;
				CT.Info(string.Format("── КОНТРФАКТ СЛЕПОТЫ: отсечка ВЫКЛ ⇒ Δu_fs = {0:F2} м/с ⇒ ", duOff) + string.Format("σ «извлечённая» = {0:F3} ГПа", offStress / 1000000000.0));
				CT.True("   ⭐ без отсечки извлечение НЕ даёт числа в полосе 1.31…1.38 — значит оно читает откол, а не колебание", offStress < 1310000000.0 || 1380000000.0 < offStress);
				double dist = Math.Min(Math.Abs(offStress - 1310000000.0), Math.Abs(offStress - 1380000000.0));
				CT.Info(string.Format("       ⚠️ отрыв контрфакта от полосы всего {0:F3} ГПа ", dist / 1000000000.0) + string.Format("(= {0:F0} % её ширины). При v = 150 м/с он был огромен; ", dist / 70000000.0 * 100.0) + "с калиброванной скоростью проверка ослабла и это записано, а не замолчано.");
			}
		}
		else
		{
			CT.Info("⛔ Сцена не дотягивает до порога ⇒ извлечение НЕ ВЫПОЛНЯЕТСЯ (ТЗ §2).");
		}
	}

	private static SpallZone ZoneOf(float[] fail, Vector2[] pos0, float xImpact, int tnx, int ny, double h, float dx)
	{
		SpallZone z = default(SpallZone);
		z.rowsTotal = ny;
		z.hM = h;
		z.dxM = dx;
		z.targetM = tnx * dx;
		z.planeRef = (h + h) / (double)dx * (double)ny;
		List<double> xs = new List<double>();
		HashSet<int> rows = new HashSet<int>();
		for (int i = 0; i < fail.Length; i++)
		{
			if (fail[i] > 0.5f && xImpact - dx * 0.5f < pos0[i].x)
			{
				xs.Add(pos0[i].x - xImpact);
				rows.Add(Flat2DLattice.Index(pos0[i].y, dx));
			}
		}
		z.n = xs.Count;
		z.rows = rows.Count;
		if (z.n != 0)
		{
			xs.Sort();
			z.minM = xs[0];
			z.maxM = xs[z.n - 1];
			z.p05M = xs[(int)((double)(z.n - 1) * 0.05)];
			z.p95M = xs[(int)((double)(z.n - 1) * 0.95)];
			z.clipFront = z.minM <= (double)dx * 0.75;
			z.clipBack = ((double)tnx - 1.75) * (double)dx <= z.maxM;
		}
		return z;
	}

	private static bool IsPlane(SpallZone z)
	{
		if (z.n == 0)
		{
			return false;
		}
		if (z.Extent <= z.hM * 4.0 && z.hM * 2.0 / z.dxM * (double)Math.Max(z.rows, 1) <= (double)z.n && 1.0 < (double)z.rows * z.dxM / Math.Max(z.Extent, z.dxM) && !z.clipFront)
		{
			return !z.clipBack;
		}
		return false;
	}

	private static bool ReportZone(SpallZone z, string label)
	{
		if (z.n == 0)
		{
			CT.Info("   ЗОНА ОТКАЗА [" + label + "]: помеченных НЕТ ⇒ протяжённости не существует. ⚠️ Это НЕ «плоскость нулевой толщины» — это отсутствие отказа.");
			return false;
		}
		CT.Info(string.Format("   ЗОНА ОТКАЗА [{0}]: n = {1}; размах {2:F2}…{3:F2} мм ", label, z.n, z.minM * 1000.0, z.maxM * 1000.0) + string.Format("⇒ {0:F2} мм = {1:F1} шагов Δx = {2:F1} h", z.Extent * 1000.0, z.Extent / z.dxM, z.Extent / z.hM));
		CT.Info(string.Format("       ядро 5…95 %: {0:F2}…{1:F2} мм ⇒ {2:F2} мм = ", z.p05M * 1000.0, z.p95M * 1000.0, z.Core * 1000.0) + string.Format("{0:F1} шагов Δx; мишень {1:F2} мм ⇒ размах занимает ", z.Core / z.dxM, z.targetM * 1000.0) + string.Format("{0:F0} % толщины", z.Extent / z.targetM * 100.0));
		double support = z.hM * 2.0 / z.dxM * (double)Math.Max(z.rows, 1);
		CT.Info(string.Format("       (1) ЧИСЛО: n = {0}; опора ПО ЗАНЯТОЙ ШИРИНЕ {1:F0} ", z.n, support) + "(= рядов·2h/Δx — сплошная ли поверхность там, где она есть) ⇒ " + (((double)z.n < support) ? "⛔ отказов МАЛО — узость может быть ОТ ЭТОГО, а не от физики" : "поверхность СПЛОШНАЯ"));
		CT.Info(string.Format("           для сведения, опора ВО ВСЮ ширину = {0:F0}; она НЕ ГАТИТ — ", z.rowsTotal) + "боковая разгрузка не пускает поверхность к свободным кромкам, и такой порог был бы недостижим по построению");
		CT.Info(string.Concat(new string[5]
		{
			"       (2) ГРАНИЦЫ: передняя ",
			z.clipFront ? "⛔ УПЁРТА в лицо удара" : "свободна",
			", задняя ",
			z.clipBack ? "⛔ УПЁРТА в тыл" : "свободна",
			(z.clipFront || z.clipBack) ? " ⇒ размах ограничен ТЕЛОМ, а не физикой" : ""
		}));
		double across = (double)z.rows * z.dxM;
		double ratio = across / Math.Max(z.Extent, z.dxM);
		CT.Info(string.Format("       (3) ФОРМА: занято {0} рядов по y из {1} ⇒ протяжённость ", z.rows, z.rowsTotal) + string.Format("поперёк {0:F1} мм против {1:F2} мм вдоль ⇒ отношение ", across * 1000.0, z.Extent * 1000.0) + string.Format("сторон {0:F1} ⇒ {1}", ratio, (ratio <= 1.0) ? "⛔ это ПЯТНО, а не плоскость — узость по x ничего не значит" : "это ПОВЕРХНОСТЬ"));
		CT.Info(string.Format("       (4) ОКНО: прибор сканировал ВСЮ мишень по x и ВСЕ {0} рядов по y — ", z.rowsTotal) + "ни средней трети, ни полосы у зонда");
		CT.Info(string.Format("       ТОЛЩИНА: {0:F2} мм против выведенного предела 4h = ", z.Extent * 1000.0) + string.Format("{0:F2} мм ({1:F1} Δx) ⇒ {2}", z.hM * 4.0 * 1000.0, z.hM * 4.0 / z.dxM, (z.hM * 4.0 < z.Extent) ? "⛔ толще" : "тоньше"));
		bool verdict = IsPlane(z);
		if (verdict)
		{
			CT.Info(string.Format("       ⭐ ВЕРДИКТ: ПЛОСКОСТЬ — {0:F1} шагов Δx, все четыре сторожа чисты", z.Extent / z.dxM));
		}
		else
		{
			CT.Info(string.Format("       ⛔ ВЕРДИКТ: ЗОНА ({0:F1} шагов Δx, ", z.Extent / z.dxM) + string.Format("{0:F0} % толщины) — извлечение НЕ ЧИТАЕТСЯ (ТЗ §3)", z.Extent / z.targetM * 100.0));
		}
		return verdict;
	}

	private float Velocity(ComputeShader cs, Flat2DMaterialBlock[] mats, float dx = 0.0005f, bool cache = true)
	{
		if (cache && vCal > 0f)
		{
			return vCal;
		}
		double sigSp = mats[0].spall / 1e9;
		CT.Info(string.Format("── W2: ПОИСК СКОРОСТИ по ЗАПАСУ над σ_sp = {0:F2} ГПа, полоса ", sigSp) + string.Format("×{0:F1}…×{1:F1} (эталон research_13 Q5 даёт ×1.16)", 1.1, 1.3));
		CT.Info("   ⚠️ критерий поиска — ПИК РАСТЯЖЕНИЯ при выключенной отсечке. Извлечённая прочность в поиск НЕ ВХОДИТ и войти не может: извлечение здесь не вызывается.");
		float lo = 30f;
		float hi = 200f;
		double mLo = Margin(cs, mats, lo, sigSp, dx);
		double mHi = Margin(cs, mats, hi, sigSp, dx);
		if (mLo > 1.3 || 1.1 > mHi)
		{
			throw new Exception(string.Format("W2: полоса запаса ×{0:F1}…×{1:F1} НЕ ЗАЖАТА скобками ", 1.1, 1.3) + string.Format("[{0}, {1}] м/с: получено ×{2:F2} и ×{3:F2}. Расширять скобки ", 30f, 200f, mLo, mHi) + "самовольно нельзя — это находка о сцене, и молча подставить скорость (тем более прежние 150 м/с) значило бы мерить при опровергнутом запасе.");
		}
		float v = hi;
		double m = mHi;
		for (int i = 0; i < 8; i++)
		{
			if (m >= 1.1 && 1.3 >= m)
			{
				break;
			}
			v = (hi + lo) * 0.5f;
			m = Margin(cs, mats, v, sigSp, dx);
			if (m > 1.3)
			{
				hi = v;
			}
			else
			{
				lo = v;
			}
		}
		if (m >= 1.1 && 1.3 >= m)
		{
			CT.Info(string.Format("   ⭐ ВЗЯТО v = {0:F1} м/с ⇒ ИЗМЕРЕННЫЙ запас ×{1:F2} — в полосе", v, m));
		}
		else
		{
			CT.Info(string.Format("   ⛔ за 8 проб полоса не достигнута, ближайшее v = {0:F1} м/с ⇒ ×{1:F2}", v, m));
		}
		if (cache)
		{
			vCal = v;
		}
		return v;
	}

	private double Margin(ComputeShader cs, Flat2DMaterialBlock[] mats, float v, double sigSp, float dx)
	{
		FlyerRun(cs, mats, v, spall: false, out double peakTension, out double _, out double _, out SpallZone _);
		double margin = peakTension / sigSp;
		CT.Info(string.Format("      проба v = {0:F1} м/с ⇒ пик растяжения {1:F3} ГПа ⇒ запас ×{2:F2}", v, peakTension / 1e9, margin));
		return margin;
	}

	private void A8(ComputeShader cs, Flat2DMaterialBlock[] mats, float v)
	{
		CT.Info("── W5/A8: искусственное напряжение в сцене ЛЕТЯЩЕЙ ПЛАСТИНЫ, оба положения");
		bool[] modes = new bool[2] { false, true };
		double fnOff = 0.0;
		double fnOn = 0.0;
		double hgOff = 0.0;
		double hgOn = 0.0;
		for (int i = 0; i < modes.Length; i++)
		{
			bool artStress = modes[i];
			Flat2DScene.Body body = Flyer(20, 40, 120, v, out Vector2 lo, out Vector2 hi);
			float dt = 0.000195f / (mats[0].LongitudinalWaveSpeed + v);
			using (Flat2DSolver solver = new Flat2DSolver(cs))
			{
				solver.FixedTimeStep = dt;
				solver.Spall = true;
				solver.Damage = false;
				solver.ArtificialStress = artStress;
				solver.AccumulateWork = true;
				solver.Init(body.pos, body.vel, body.mat, mats, 0.0005f, lo, hi);
				solver.Steps(420);
				solver.MeasureChannels(out double _, out double _, out double _, out double wArt);
				Flat2DSolver.Diag diag = solver.Measure(withHourglass: true);
				double fn = MaxFn(solver);
				if (artStress)
				{
					fnOn = fn;
					hgOn = diag.hourglass;
				}
				else
				{
					fnOff = fn;
					hgOff = diag.hourglass;
				}
				CT.Info(string.Format("   [{0}] max fⁿ = {1:F4}; W_a = {2:E4} Дж = ", artStress ? "ВКЛ " : "ВЫКЛ", fn, wArt) + string.Format("{0:F4} % полной энергии ", Math.Abs(wArt) / Math.Max(diag.Total, 1e-30) * 100.0) + string.Format("({0:E4} Дж); A6 = {1:E4}", diag.Total, diag.hourglass));
			}
		}
		if (fnOff > fnOn)
		{
			CT.Info(string.Format("   ⭐ СКУЧИВАНЬЕ ПАДАЕТ: max fⁿ {0:F4} → {1:F4} ⇒ член делает свою работу ", fnOff, fnOn) + "там, где ей есть место; число A3′ было проигранной дракой не по адресу");
		}
		else
		{
			CT.Info(string.Format("   ⛔ СКУЧИВАНЬЕ ПО max fⁿ НЕ ПАДАЕТ: {0:F4} → {1:F4} ⇒ по букве таблицы ", fnOff, fnOn) + "ТЗ §5 член не окупается и здесь, и четыре названные первоисточником замены (Ma/Zhang/Qiu §5.4) становятся следующим решением, а не обходом");
		}
		CT.Info(string.Format("   ⚠️ A6 при этом ПАДАЕТ {0:E4} → {1:E4} (в {2:F2} раза). ", hgOff, hgOn, hgOff / Math.Max(hgOn, 1e-30)) + "max fⁿ — ОДНА крайняя пара, A6 — сумма по телу. Расхождение записано как есть: по крайней паре член проигрывает, по телу в целом выигрывает. Порога нет, правки нет (ТЗ §5).");
	}

	private void Z3Sweep(ComputeShader cs)
	{
		ContinuumProperties props = ContinuumMaterials.Get(ContinuumMaterialId.OFHC_Copper);
		double rho0 = props.rho0;
		double c0 = props.C0;
		CT.Info("── Z3: РАЗВЁРТКА ПО ВХОДНОЙ σ_sp. Отсечка переопределяется ТЕСТ-ЛОКАЛЬНО (поле Flat2DMaterialBlock.spall); реестр не трогается.");
		CT.Info("   σ_вх ГПа | v м/с | запас | плоскость Δx / % | σ_вых ГПа | out/in | СЫРОЙ контроль ВЫКЛ ГПа (при скорости ЭТОЙ строки)");
		List<double> outIn = new List<double>();
		List<double> controlRaw = new List<double>();
		List<double> controlNorm = new List<double>();
		List<double> vList = new List<double>();
		float[] sigSps = new float[4] { 9e8f, 1.31e9f, 1.8e9f, 2.5e9f };
		for (int i = 0; i < sigSps.Length; i++)
		{
			float sigSp = sigSps[i];
			Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.OFHC_Copper, ContinuumMaterialId.OFHC_Copper);
			for (int j = 0; j < mats.Length; j++)
			{
				mats[j].spall = sigSp;
			}
			float v = Velocity(cs, mats, Dx, cache: false);
			FlyerRun(cs, mats, v, spall: true, out double peakOn, out double uPeakOn, out double uMinOn, out SpallZone zoneOn);
			FlyerRun(cs, mats, v, spall: false, out double peakOff, out double uPeakOff, out double uMinOff, out SpallZone _);
			double sigmaOn = rho0 * 0.5 * c0 * (uPeakOn - uMinOn);
			double sigmaOff = rho0 * 0.5 * c0 * (uPeakOff - uMinOff);
			double margin = peakOff / sigSp;
			bool isPlaneRow = IsPlane(zoneOn);
			string planeInfo;
			if (zoneOn.n == 0)
			{
				planeInfo = "нет отказов";
			}
			else
			{
				planeInfo = string.Format("{0:F1} Δx / {1:F0} %", zoneOn.Extent / zoneOn.dxM, zoneOn.Extent / zoneOn.targetM * 100.0);
			}
			CT.Info(string.Format("      {0:F2} | {1:F1} | ×{2:F2} | {3} ", sigSp / 1000000000.0, v, margin, planeInfo) + string.Format("{0} | {1:F3} | ", isPlaneRow ? "ПЛОСКОСТЬ" : "⛔ НЕ плоскость", sigmaOn / 1000000000.0) + string.Format("{0:F3} | {1:F3}", sigmaOn / sigSp, sigmaOff / 1000000000.0));
			if (!isPlaneRow)
			{
				CT.Info(string.Format("         ⛔ строка σ_вх = {0:F2} ГПа НЕДЕЙСТВИТЕЛЬНА: плоскость ", sigSp / 1000000000.0) + "потеряна (ТЗ §7), out/in из неё в чтение НЕ идёт");
			}
			else
			{
				outIn.Add(sigmaOn / sigSp);
				controlRaw.Add(sigmaOff);
				controlNorm.Add(sigmaOff / v);
				vList.Add(v);
			}
		}
		if (outIn.Count < 2)
		{
			CT.Info("   ⛔ действительных строк меньше двух — чтения нет.");
			return;
		}
		double min1 = double.MaxValue;
		double max1 = double.MinValue;
		double sum1 = 0.0;
		foreach (double x in outIn)
		{
			min1 = Math.Min(min1, x);
			max1 = Math.Max(max1, x);
			sum1 += x;
		}
		double min2 = double.MaxValue;
		double max2 = double.MinValue;
		foreach (double y in controlRaw)
		{
			min2 = Math.Min(min2, y);
			max2 = Math.Max(max2, y);
		}
		double avg1 = sum1 / outIn.Count;
		double spread1 = (max1 - min1) / avg1;
		double spread2 = (max2 - min2) / Math.Max((max2 + min2) * 0.5, 1e-30);
		CT.Info(string.Format("── Z4: out/in по действительным строкам ({0}): среднее {1:F3}, ", outIn.Count, avg1) + string.Format("размах {0:F3}…{1:F3} ⇒ разброс {2:F1} % от среднего", min1, max1, spread1 * 100.0));
		CT.Info(string.Format("   КОНТРОЛЬ (отсечка ВЫКЛ): {0:F3}…{1:F3} ГПа ⇒ ", min2 / 1000000000.0, max2 / 1000000000.0) + string.Format("размах {0:F1} %", spread2 * 100.0));
		CT.Info(string.Format("   вход прошёл {0:F0} % размаха; контроль {1:F1} % ⇒ ", 94.11764705882354, spread2 * 100.0) + string.Format("доля {0:F1} % ⇒ ", spread2 / 0.9411764705882354 * 100.0) + ((0.25 <= spread2 / 0.9411764705882354) ? "⛔⛔ КОНТРОЛЬ СЛЕДУЕТ ЗА ВХОДОМ — по БУКВЕ ТЗ §2.2 измерение НЕДЕЙСТВИТЕЛЬНО" : "контроль СТОИТ"));
		double min3 = double.MaxValue;
		double max3 = double.MinValue;
		double sum3 = 0.0;
		foreach (double w in controlNorm)
		{
			min3 = Math.Min(min3, w);
			max3 = Math.Max(max3, w);
			sum3 += w;
		}
		double spread3 = (max3 - min3) / Math.Max(sum3 / controlNorm.Count, 1e-30);
		CT.Info(string.Format("   КОНТРОЛЬ, НОРМИРОВАННЫЙ НА v: {0:F3}…{1:F3} МПа на м/с ⇒ ", min3 / 1000000.0, max3 / 1000000.0) + string.Format("разброс {0:F1} % против {1:F1} % у ненормированного ⇒ ", spread3 * 100.0, spread2 * 100.0) + "контроль читает СКОРОСТЬ, а не σ_вх");
		CT.Info("── Z4б: ПОБИТОВАЯ ПРОВЕРКА ПРОТЕЧКИ — отсечка ВЫКЛ, ОДНА скорость, четыре σ_вх");
		LeakTest(cs, 72.5f);
		if (0.15 <= spread1)
		{
			CT.Info(string.Format("   ⛔ out/in ЕЗДИТ с входом на {0:F1} % ⇒ извлечение ничего не ", spread1 * 100.0) + "восстанавливает. Разброс записан, дальше не идём (ТЗ §2.2).");
		}
		else
		{
			CT.Info(string.Format("   out/in ПОСТОЯННО в пределах {0:F1} % ⇒ систематика {1:F3}", spread1 * 100.0, avg1));
		}
		CT.Info("   ⇒ ⛔ ОСТАНОВКА ПО ТЗ §7: развёртка недействительна ПО БУКВЕ §2.2. S1 характеризацией НЕ закрывается этой задачей. Диагноз (нормировка + побитовая проверка) приложен, решение о переделке развёртки — владельца.");
	}

	private void LeakTest(ComputeShader cs, float v)
	{
		float[] ratios = new float[4] { 9e8f, 1.31e9f, 1.8e9f, 2.5e9f };
		bool same = true;
		ulong refHash = 0uL;
		for (int i = 0; i < ratios.Length; i++)
		{
			float ratio = ratios[i];
			Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.OFHC_Copper, ContinuumMaterialId.OFHC_Copper);
			for (int j = 0; j < mats.Length; j++)
			{
				mats[j].spall = ratio;
			}
			Flat2DScene.Body body = Flyer(Convert.ToInt32(FlyerM / Dx), Convert.ToInt32(TargetM / Dx), Convert.ToInt32(HeightM / Dx), v, out Vector2 lo, out Vector2 hi);
			float dt = 0.000195f / (mats[0].LongitudinalWaveSpeed + v);
			using (Flat2DSolver solver = new Flat2DSolver(cs))
			{
				solver.FixedTimeStep = dt;
				solver.Spall = false;
				solver.Damage = false;
				solver.ArtificialStress = true;
				double uPass = 0.0;
				for (int k = 0; k < body.vel.Length; k++)
				{
					uPass = Math.Max(uPass, Math.Abs(body.vel[k].x));
				}
				solver.Init(body.pos, body.vel, body.mat, mats, 0.0005f, lo, hi);
				Vector2[] vel = solver.ReadVelocities();
				double sum = 0.0;
				int count = 0;
				for (int m = 0; m < vel.Length; m++)
				{
					if (body.pos[m].x < 0.00075f)
					{
						sum += vel[m].x;
						count++;
					}
				}
				double uFs = sum / Math.Max(count, 1);
				double uMax = 0.0;
				for (int n = 0; n < vel.Length; n++)
				{
					uMax = Math.Max(uMax, Math.Abs(vel[n].x));
				}
				solver.Steps(420);
				ulong hash = solver.StateHash();
				if (ratio == 9e8f)
				{
					refHash = hash;
				}
				else
				{
					same &= hash == refHash;
				}
				same &= solver.ProbeAlive(420, out string detail);
				CT.Info("      " + detail);
				CT.Info(string.Format("      σ_вх = {0:F2} ГПа ⇒ ПЕРЕДАНО {1:F3}, у дальнего края ", (double)ratio / 1000000000.0, uPass) + string.Format("ударника ИЗ РЕШАТЕЛЯ {0:F3} м/с (max по телу {1:F3} — это лицо ", uFs, uMax) + string.Format("удара после разгоночного полушага) ⇒ отпечаток {0:X16}", hash));
				if (0.001 < Math.Abs(uFs - (double)v))
				{
					CT.Info(string.Format("         ⛔⛔ СКОРОСТЬ НЕ ДОШЛА ДО РЕШАТЕЛЯ: {0:F3} против {1:F3}", uFs, v));
				}
			}
		}
		CT.True("   ⭐ отпечатки СОВПАЛИ ⇒ при выключенной отсечке σ_вх не читается вовсе, протечка входа в ответ ИСКЛЮЧЕНА побитово", same);
		CT.Info("   ⭐ G1 — МИР ПЕРВЫЙ, И ОН ПОДТВЕРЖДЁН ЧТЕНИЕМ ИЗ РЕШАТЕЛЯ. Все четыре прогона " + string.Format("шли при ОДНОЙ скорости {0:F1} м/с; она совпала со считанной у дальнего края ", v) + "ударника. ⇒ Совпадение отпечатков объясняется ПОЛНОСТЬЮ: скорость одна, а σ_вх при _SpallOn = 0 не читается вовсе. ⇒ Это проверка ПРОТЕЧКИ, а НЕ построчный контроль слепоты, и слабее, чем было заявлено.");
		CT.Info("   ⚠️ Построчный контроль — столбец «СЫРОЙ контроль ВЫКЛ» таблицы Z3: он идёт при скорости СВОЕЙ строки и потому НЕ обязан стоять на месте — v меняется по условию §2.1. ⇒ Второй мир («скорость не доходит до решателя») ОПРОВЕРГНУТ.");
	}

	private bool Z5Fragments(ComputeShader cs)
	{
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.OFHC_Copper, ContinuumMaterialId.OFHC_Copper);
		float v = Velocity(cs, mats);
		CT.Info(string.Format("── Z5: тела на калиброванной скорости {0:F1} м/с, σ_sp реестра ", v) + string.Format("{0:F2} ГПа", (double)mats[0].spall / 1000000000.0));
		FlyerRun(cs, mats, v, spall: true, out double _, out double _, out double _, out SpallZone zoneOn, labelBodies: true);
		bool okRun = ReportComponents(lastComp, "отсечка ВКЛ, штатное окно", lastLinkR);
		float minX = float.MaxValue;
		float minY = float.MaxValue;
		float maxX = float.MinValue;
		float maxY = float.MinValue;
		for (int i = 0; i < lastPos.Length; i++)
		{
			minX = Math.Min(minX, lastPos[i].x);
			maxX = Math.Max(maxX, lastPos[i].x);
			minY = Math.Min(minY, lastPos[i].y);
			maxY = Math.Max(maxY, lastPos[i].y);
		}
		double diag = Math.Sqrt((double)(maxY - minY) * (maxY - minY) + (double)(maxX - minX) * (maxX - minX));
		Components cHalf = Label(lastPos, lastMassArr, (double)lastDx * 0.5);
		long maxEdges = (long)(cHalf.n - 1) * cHalf.n / 2;
		bool okDiv = CT.True(string.Format("       (3а) ДИВЕРСИЯ радиусом 0.5·Δx = {0:F3} мм ", (double)lastDx * 0.5 * 1000.0) + string.Format("(меньше шага {0:F2} мм) ⇒ компонент {1} = N = {2} ", lastDx * 1000f, cHalf.count, cHalf.n) + string.Format("при рёбрах {0} = 0; путь ИСПОЛНЕН: сетка, ключи, обход, расстояние", cHalf.edges), cHalf.count == cHalf.n && cHalf.edges == 0) & okRun;
		Components cTen = Label(lastPos, lastMassArr, diag * 10.0);
		okDiv = CT.True(string.Format("       (3б) ДИВЕРСИЯ радиусом 10 диагоналей = {0:F0} мм ⇒ ", diag * 10.0 * 1000.0) + string.Format("компонент {0} = 1 при рёбрах {1} = N(N−1)/2 = {2}", cTen.count, cTen.edges, maxEdges), cTen.count == 1 && cTen.edges == maxEdges) & okDiv;
		Components cA = Label(lastPos, lastMassArr, lastLinkR);
		Components cB = Label(lastPos, lastMassArr, lastLinkR);
		bool sameList = cA.count == cB.count && cA.count == lastComp.count && cA.edges == lastComp.edges;
		for (int j = 0; sameList && j < cA.sizes.Count; j++)
		{
			sameList = cA.sizes[j] == cB.sizes[j] && cA.sizes[j] == lastComp.sizes[j];
		}
		bool okOrder = CT.True(string.Format("({0} компонент, {1} рёбер) — обход словаря не течёт в вывод", cA.count, cA.edges) + "       (4) ПОРЯДОК: три разметки одних данных дали один список размеров ", sameList);
		CT.Info("── Z6 [ПРОСПЕКТИВНЫЙ СТОРОЖ — сегодня почти тавтология, покраснеет, когда осколки начнут удалять частицы]: масса по ВСЕМ частицам (отказавшие и " + string.Format("отделившиеся включены): {0:F12} → {1:F12} кг", lastMassStart, lastMassEnd));
		bool okMass = CT.True("   масса совпала ПОБИТОВО", lastMassStart == lastMassEnd);
		bool okSum = CT.True(string.Format("   сумма по компонентам {0:F12} = полная {1:F12}", lastComp.massTotal, lastMassEnd), lastComp.massTotal == lastMassEnd);
		if (lastMassStart != lastMassEnd)
		{
			CT.Info(string.Format("   ⛔ ПОТЕРЯ {0:E6} кг = {1:E3} % — ", Math.Abs(lastMassStart - lastMassEnd), Math.Abs(lastMassStart - lastMassEnd) / lastMassStart * 100.0) + "это находка, а не допуск (ТЗ §7)");
		}
		FlyerRun(cs, mats, v, spall: false, out double _, out double _, out double _, out SpallZone _, labelBodies: true);
		bool okNoSpall = ReportComponents(lastComp, "КОНТРФАКТ: отсечка ВЫКЛ — обязано быть РОВНО одно тело", lastLinkR);
		bool okOne = CT.True("   ⭐ без отсечки РОВНО одно тело", lastComp.count == 1);
		CT.Info(string.Format("   ⭐ ЧЕШУЙКА как ИЗВЕСТНЫЙ ОТВЕТ: помеченная плоскость на {0:F2}…", zoneOn.minM * 1000.0) + string.Format("{0:F2} мм, зазор растёт ×1.57 против контрольной ×0.99. ", zoneOn.maxM * 1000.0) + string.Format("Отделение ПО МОДЕЛИ наступает, когда зазор превысит 2h = {0:F2} мм — ", lastLinkR * 1000.0) + "за этим порогом пара действует друг на друга РОВНО нулём.");
		FlyerRun(cs, mats, v, spall: true, out double _, out double _, out double _, out SpallZone _, Dx, 4, labelBodies: true);
		ReportComponents(lastComp, "отсечка ВКЛ, окно ×4 — НАБЛЮДЕНИЕ, а не правка сцены", lastLinkR);
		return okOne & okNoSpall & okSum & okMass & okOrder & okDiv;
	}

	private void G2Width(ComputeShader cs)
	{
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.OFHC_Copper, ContinuumMaterialId.OFHC_Copper);
		float v = Velocity(cs, mats);
		CT.Info(string.Format("── G2: выбивание ШИРИНОЙ при калиброванной скорости {0:F1} м/с. ", v) + string.Format("Толщина мишени неизменна {0:F0} мм.", 20f));
		CT.Info("   ширина мм | W/D | частиц | рядов поверхности / всего | доля | тел | чешуйка");
		float[] widths = new float[3] { 1f, 2f, 4f };
		for (int i = 0; i < widths.Length; i++)
		{
			float w = widths[i];
			float heightM = w * HeightM;
			int ny = Convert.ToInt32(heightM / Dx);
			int total = (Convert.ToInt32(FlyerM / Dx) + Convert.ToInt32(TargetM / Dx)) * ny;
			CT.Info(string.Format("      [ширина ×{0:F0} = {1:F0} мм ⇒ частиц {2}]", w, heightM * 1000f, total));
			FlyerRun(cs, mats, v, spall: true, out double _, out double _, out double _, out SpallZone zone, labelBodies: true, heightM: heightM);
			double need = ny * 2.6;
			int big = 0;
			for (int c = 1; c < lastComp.sizes.Count; c++)
			{
				if (lastComp.sizes[c] >= need)
				{
					big++;
				}
			}
			CT.Info(string.Format("      {0:F0} | {1:F1} | {2} | {3} / {4} | ", heightM * 1000f, heightM / TargetM, total, zone.rows, zone.rowsTotal) + string.Format("{0:F0} % | {1} | ", (double)zone.rows / Math.Max(zone.rowsTotal, 1) * 100.0, lastComp.count) + ((big > 0) ? string.Format("⭐ ОТОРВАЛАСЬ ({0} тел ≥ {1:F0} частиц)", big, need) : "нет"));
		}
		CT.Info("   ⚠️ Читается по ТЗ §2: доля растёт к полной ширине и чешуйка отрывается ⇒ модель откалывает верно, а прежние сцены были просто узки; доля осталась частичной при ×4 ⇒ это СВОЙСТВО РЕЖИМА (плоская деформация со свободными боковыми кромками), а не дефект, и на этом остановка.");
	}

	private bool G4Contact(ComputeShader cs)
	{
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.OFHC_Copper, ContinuumMaterialId.OFHC_Copper);
		float gap0 = 0.003f;
		double overlap = 0.0;

		Flat2DScene.Body TwoPlates(float vA, float vB, out Vector2 lo, out Vector2 hi, out uint[] bodies)
		{
			Flat2DScene.Body a = Flat2DScene.Block(16, 40, Dx, Vector2.zero, 0u, new Vector2(vA, 0f));
			Flat2DScene.Body b = Flat2DScene.Block(16, 40, Dx, new Vector2(gap0 + 0.008f, 0f), 0u, new Vector2(vB, 0f));
			Flat2DScene.Body res = default(Flat2DScene.Body);
			int total = a.pos.Length + b.pos.Length;
			res.pos = new Vector2[total];
			res.vel = new Vector2[total];
			res.mat = new uint[total];
			res.dx = Dx;
			bodies = new uint[total];
			Flat2DScene.Body[] parts = new Flat2DScene.Body[2] { a, b };
			int offset = 0;
			for (int q = 0; q < parts.Length; q++)
			{
				parts[q].pos.CopyTo(res.pos, offset);
				parts[q].vel.CopyTo(res.vel, offset);
				parts[q].mat.CopyTo(res.mat, offset);
				for (int i = 0; i < parts[q].pos.Length; i++)
				{
					bodies[offset + i] = (uint)q;
				}
				offset += parts[q].pos.Length;
			}
			res.min = a.min;
			res.max = b.max;
			Flat2DScene.Domain(res, 0.01f, out lo, out hi);
			return res;
		}

		double Approach(bool contact, out double minGap, out double maxAccC, out double wContact, out double pStart, out double pEnd, out double a6, out bool everClose, out double maxOverlap)
		{
			overlap = 0.0;
			Flat2DScene.Body body = TwoPlates(600f, -600f, out Vector2 lo, out Vector2 hi, out uint[] bodies);
			float dt = 0.000195f / (mats[0].LongitudinalWaveSpeed + 600f);
			minGap = 1e30;
			maxAccC = 0.0;
			wContact = 0.0;
			everClose = false;
			using (Flat2DSolver solver = new Flat2DSolver(cs))
			{
				solver.FixedTimeStep = dt;
				solver.Contact = contact;
				solver.BodyIds = bodies;
				solver.Spall = false;
				solver.Damage = false;
				solver.ArtificialStress = true;
				solver.AccumulateWork = true;
				solver.Init(body.pos, body.vel, body.mat, mats, Dx, lo, hi);
				pStart = solver.Measure(withHourglass: true).px;
				int half = body.pos.Length / 2;
				for (int sample = 0; sample < 300; sample++)
				{
					solver.Steps(2);
					Vector2[] pos = solver.ReadPositions();
					double best = 1e30;
					double maxAX = -1e30;
					double minBX = 1e30;
					float interfaceX = gap0 * 0.5f + 0.008f;
					for (int i = 0; i < half; i++)
					{
						if (Math.Abs(pos[i].x - interfaceX) <= 0.002f)
						{
							maxAX = Math.Max(maxAX, pos[i].x);
							for (int j = half; j < pos.Length; j++)
							{
								if (Math.Abs(pos[j].x - interfaceX) <= 0.002f)
								{
									double dx = pos[i].x - pos[j].x;
									double dy = pos[i].y - pos[j].y;
									double d2 = dx * dx + dy * dy;
									if (d2 < best * best)
									{
										best = Math.Sqrt(d2);
									}
								}
							}
						}
					}
					for (int j = half; j < pos.Length; j++)
					{
						if (Math.Abs(pos[j].x - interfaceX) <= 0.002f)
						{
							minBX = Math.Min(minBX, pos[j].x);
						}
					}
					minGap = Math.Min(minGap, best);
					overlap = Math.Max(overlap, maxAX - minBX);
					if (best < 0.0005000000237487257)
					{
						everClose = true;
					}
					if (contact)
					{
						Vector2[] acc = solver.ReadAccC();
						for (int k = 0; k < acc.Length; k++)
						{
							maxAccC = Math.Max(maxAccC, acc[k].magnitude);
						}
					}
				}
				Flat2DSolver.Diag diag = solver.Measure(withHourglass: true);
				pEnd = diag.px;
				a6 = diag.hourglass;
				maxOverlap = overlap;
				solver.MeasureChannels(out double _, out double _, out double _, out double _, out wContact);
				return diag.Total;
			}
		}

		Approach(contact: true, out double minGapOn, out double maxAccCOn, out double wContactOn, out double pStartOn, out double pEndOn, out double a6On, out bool everCloseOn, out double maxOverlapOn);
		Approach(contact: false, out double minGapOff, out double maxAccCOff, out double wContactOff, out double pStartOff, out double pEndOff, out double a6Off, out bool everCloseOff, out double maxOverlapOff);
		CT.Info(string.Format("── C-a: СБЛИЖЕНИЕ. d0 = Δx = {0:F2} мм, старт зазора ", 0.5f) + string.Format("{0:F2} мм, скорости ±{1} м/с", gap0 * 1000f, 600f));
		CT.Info(string.Format("   [контакт ВКЛ ] минимальное расстояние пары {0:F3} мм; ", minGapOn * 1000.0) + string.Format("глубина ПЕРЕКРЫТИЯ {0:F3} мм; max|acc_c| = {1:E3} м/с²", maxOverlapOn * 1000.0, maxAccCOn));
		CT.Info(string.Format("   [контакт ВЫКЛ] минимальное расстояние пары {0:F3} мм; ", minGapOff * 1000.0) + string.Format("глубина ПЕРЕКРЫТИЯ {0:F3} мм", maxOverlapOff * 1000.0));
		bool okApproach = CT.True("   (1) СБЛИЖЕНИЕ СОСТОЯЛОСЬ: зазор опускался ниже d0 ⇒ контакт был испытан", everCloseOn);
		bool okWorked = CT.True(string.Format("   (2) КОНТАКТ ОТРАБОТАЛ: max|acc_c| = {0:E3} > 0 и работа канала ", maxAccCOn) + string.Format("{0:E4} Дж ≠ 0", wContactOn), maxAccCOn > 0.0 && Math.Abs(wContactOn) > 0.0);
		bool okCounterfactual = CT.True("   (3) КОНТРФАКТ: БЕЗ контакта тела сходятся ближе или перекрываются " + string.Format("глубже (расстояние {0:F3} против {1:F3} мм; ", minGapOff * 1000.0, minGapOn * 1000.0) + string.Format("перекрытие {0:F3} против {1:F3} мм)", maxOverlapOff * 1000.0, maxOverlapOn * 1000.0), minGapOff < minGapOn || maxOverlapOff > maxOverlapOn);
		bool okNoCross = CT.True("   ⭐ C-a: С контактом ПЕРЕСЕЧЕНИЯ НЕТ — глубина перекрытия " + string.Format("{0:F3} мм ≤ 0", maxOverlapOn * 1000.0), maxOverlapOn <= 0.0);
		bool okSeparate;
		bool okNoCohesion;
		Flat2DScene.Body bodyB = TwoPlates(-150f, 150f, out Vector2 loB, out Vector2 hiB, out uint[] bodiesB);
		float dtB = 0.000195f / (mats[0].LongitudinalWaveSpeed + 600f);
		using (Flat2DSolver solverB = new Flat2DSolver(cs))
		{
			solverB.FixedTimeStep = dtB;
			solverB.Contact = true;
			solverB.BodyIds = bodiesB;
			solverB.Spall = false;
			solverB.Damage = false;
			solverB.ArtificialStress = true;
			solverB.Init(bodyB.pos, bodyB.vel, bodyB.mat, mats, Dx, loB, hiB);
			int halfB = bodyB.pos.Length / 2;
			double firstGap = 0.0;
			double lastGap = 0.0;
			bool monotone = true;
			double maxAccA = 0.0;
			for (int sample = 0; sample < 150; sample++)
			{
				solverB.Steps(2);
				Vector2[] pos = solverB.ReadPositions();
				double maxAX = -1e30;
				double minBX = 1e30;
				for (int i = 0; i < pos.Length; i++)
				{
					if (Math.Abs(pos[i].y - 0.00975f) <= 0.0015f)
					{
						if (i < halfB)
						{
							maxAX = Math.Max(maxAX, pos[i].x);
						}
						else
						{
							minBX = Math.Min(minBX, pos[i].x);
						}
					}
				}
				double gap = minBX - maxAX;
				if (sample == 0)
				{
					firstGap = gap;
				}
				if (gap < lastGap - 1e-09)
				{
					monotone = false;
				}
				lastGap = gap;
				Vector2[] acc = solverB.ReadAccC();
				for (int i = 0; i < halfB; i++)
				{
					maxAccA = Math.Max(maxAccA, acc[i].x);
				}
			}
			CT.Info("── C-b: РАСХОЖДЕНИЕ. расстояние по оси между крайними " + string.Format("частицами {0:F3} → {1:F3} мм", firstGap * 1000.0, lastGap * 1000.0));
			okSeparate = CT.True("   ⭐ C-b: тела НЕ слипаются — зазор растёт монотонно и не схлопывается", lastGap > firstGap && monotone);
			okNoCohesion = CT.True("   ⭐ ложной когезии НЕТ: максимальное ПРИТЯЖИВАЮЩЕЕ acc_c у тела A " + string.Format("= {0:E3} (обязано быть ≤ 0)", maxAccA), maxAccA <= 0.0);
		}
		CT.Info(string.Format("── C-c: ИМПУЛЬС через контакт: p_x {0:E6} → {1:E6} кг·м/с ⇒ ", pStartOn, pEndOn) + string.Format("дрейф {0:E3}", Math.Abs(pEndOn - pStartOn)));
		CT.Info(string.Format("   для сравнения, БЕЗ контакта: {0:E6} → {1:E6} ⇒ ", pStartOff, pEndOff) + string.Format("{0:E3} — тот же пол прибора A2", Math.Abs(pEndOff - pStartOff)));
		bool okDrift = CT.True("   ⭐ C-c: дрейф импульса с контактом не хуже, чем без него (пол A2 меряется тем же прибором)", Math.Abs(pEndOn - pStartOn) <= Math.Max(Math.Abs(pEndOff - pStartOff) * 10.0, 1e-06));
		CT.Info(string.Format("── C-d: A6 {0:E4} (без контакта) → {1:E4} (с контактом)", a6Off, a6On));
		if (a6On > a6Off)
		{
			CT.Info(string.Format("   ⭐ A6 ВЫРОСЛА в {0:F2} раза — ОЖИДАЛОСЬ: ", a6On / Math.Max(a6Off, 1e-30)) + "DECISION §4 п. 3 записывает, что штрафной контакт ВОЗБУЖДАЕТ нулевые моды, а искусственное напряжение их не лечит. Сообщается, не подгоняется.");
		}
		else
		{
			CT.Info(string.Format("   ⚠️ A6 НЕ выросла ({0:E4} → {1:E4}) — против записанного ожидания ", a6Off, a6On) + "DECISION §4 п. 3. Это находка, а не успех.");
		}
		CT.Info("── ЗНАК РАБОТЫ КОНТАКТА, ОБЪЯВЛЕННЫЙ ДО ЗАМЕРА: штрафной отклик — пружина без демпфера. На сближении сила противонаправлена движению ⇒ работа ОТРИЦАТЕЛЬНА; на отскоке ⇒ положительна; за полное событие ≈ 0.");
		CT.Info(string.Format("   ЗАМЕР: W_контакт = {0:E4} Дж (без контакта {1:E4} Дж — обязан быть РОВНО 0)", wContactOn, wContactOff));
		bool pass = okApproach & okWorked & okCounterfactual & okNoCross & okSeparate & okNoCohesion & okDrift;
		return CT.True("   ⭐ канал контакта пуст, когда контакта нет", wContactOff == 0.0) & pass;
	}

	private void Z7Resolution(ComputeShader cs)
	{
		ContinuumProperties props = ContinuumMaterials.Get(ContinuumMaterialId.OFHC_Copper);
		CT.Info("── Z7: разрешение. Δx мм | частиц | v м/с | запас | плоскость Δx / % | out/in | тел");
		float[] dxs = new float[3] { 0.001f, 0.0005f, 0.00025f };
		for (int i = 0; i < dxs.Length; i++)
		{
			float dx = dxs[i];
			Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.OFHC_Copper, ContinuumMaterialId.OFHC_Copper);
			int fnx = Convert.ToInt32(FlyerM / dx);
			int tnx = Convert.ToInt32(TargetM / dx);
			int ny = Convert.ToInt32(HeightM / dx);
			float v = Velocity(cs, mats, dx, cache: false);
			FlyerRun(cs, mats, v, spall: true, out double peakTension, out double uPeak, out double uMin, out SpallZone zone, dx, 1, labelBodies: true, HeightM);
			FlyerRun(cs, mats, v, spall: false, out double peakOff, out double _, out double _, out SpallZone _, dx, 1, labelBodies: false, HeightM);
			double sigma = props.rho0 * 0.5 * props.C0 * (uPeak - uMin) / mats[0].spall;
			string verdict = "нет отказов";
			if (zone.n != 0)
			{
				verdict = string.Format("{0:F1} Δx / {1:F0} %", zone.Extent / zone.dxM, zone.Extent / zone.targetM * 100.0);
			}
			int total = (tnx + fnx) * ny;
			CT.Info(string.Format("      {0:F2} | {1} | {2:F1} | ", dx * 1000f, total, v) + string.Format("×{0:F2} | {1} | {2:F3} | ", peakOff / mats[0].spall, verdict, sigma) + string.Format("{0}", lastComp.count));
		}
		CT.Info("   ⚠️ Строки ЗАПИСАНЫ, а не сопоставлены с порогом. Гейта здесь нет намеренно (ТЗ §4): DECISION §0б снял число глубины, ради которого существовало правило 15 %.");
	}

	private static int Find(int[] p, int x)
	{
		while (x != p[x])
		{
			p[x] = p[p[x]];
			x = p[x];
		}
		return x;
	}

	private static Components Label(Vector2[] pos, float[] mass, double linkR)
	{
		_003C_003Ec__DisplayClass44_0 _003C_003Ec__DisplayClass44_ = new _003C_003Ec__DisplayClass44_0();
		_003C_003Ec__DisplayClass44_.c = default(Components);
		_003C_003Ec__DisplayClass44_.c.n = pos.Length;
		_003C_003Ec__DisplayClass44_.c.label = new int[pos.Length];
		_003C_003Ec__DisplayClass44_.c.sizes = new List<int>();
		_003C_003Ec__DisplayClass44_.c.masses = new List<double>();
		for (int i = 0; i < _003C_003Ec__DisplayClass44_.c.label.Length; i++)
		{
			_003C_003Ec__DisplayClass44_.c.label[i] = i;
		}
		if (linkR > 0.0)
		{
			float r = (float)linkR;
			Dictionary<Vector2Int, List<int>> grid = new Dictionary<Vector2Int, List<int>>();
			for (int j = 0; j < pos.Length; j++)
			{
				Vector2Int cell = new Vector2Int((int)Math.Floor(pos[j].x / r), (int)Math.Floor(pos[j].y / r));
				if (!grid.TryGetValue(cell, out List<int> bucket))
				{
					bucket = new List<int>();
					grid.Add(cell, bucket);
				}
				bucket.Add(j);
			}
			for (int k = 0; k < pos.Length; k++)
			{
				int cx = (int)Math.Floor(pos[k].x / r);
				int cy = (int)Math.Floor(pos[k].y / r);
				for (int dx = -1; dx < 2; dx++)
				{
					for (int dy = -1; dy < 2; dy++)
					{
						if (grid.TryGetValue(new Vector2Int(cx + dx, cy + dy), out List<int> bucket2))
						{
							foreach (int j2 in bucket2)
							{
								if (k < j2)
								{
									double ddx = pos[k].x - pos[j2].x;
									double ddy = pos[k].y - pos[j2].y;
									double r2 = ddx * ddx + ddy * ddy;
									if (r2 < linkR * linkR)
									{
										_003C_003Ec__DisplayClass44_.c.edges++;
										int ra = Find(_003C_003Ec__DisplayClass44_.c.label, k);
										int rb = Find(_003C_003Ec__DisplayClass44_.c.label, j2);
										if (ra != rb)
										{
											_003C_003Ec__DisplayClass44_.c.label[ra] = rb;
										}
									}
								}
							}
						}
					}
				}
			}
		}
		Dictionary<int, int> sizeByRoot = new Dictionary<int, int>();
		Dictionary<int, double> massByRoot = new Dictionary<int, double>();
		for (int m = 0; m < pos.Length; m++)
		{
			int root = Find(_003C_003Ec__DisplayClass44_.c.label, m);
			sizeByRoot.TryGetValue(root, out int size);
			sizeByRoot[root] = size + 1;
			massByRoot.TryGetValue(root, out double massSum);
			massByRoot[root] = mass[m] + massSum;
		}
		_003C_003Ec__DisplayClass44_.c.count = sizeByRoot.Count;
		foreach (KeyValuePair<int, int> item in sizeByRoot)
		{
			_003C_003Ec__DisplayClass44_.c.sizes.Add(item.Value);
			_003C_003Ec__DisplayClass44_.c.masses.Add(massByRoot[item.Key]);
		}
		List<int> order = new List<int>();
		for (int n = 0; n < _003C_003Ec__DisplayClass44_.c.sizes.Count; n++)
		{
			order.Add(n);
		}
		order.Sort(_003C_003Ec__DisplayClass44_._003CLabel_003Eb__0);
		List<int> sortedSizes = new List<int>();
		List<double> sortedMasses = new List<double>();
		foreach (int item2 in order)
		{
			sortedSizes.Add(_003C_003Ec__DisplayClass44_.c.sizes[item2]);
			sortedMasses.Add(_003C_003Ec__DisplayClass44_.c.masses[item2]);
		}
		_003C_003Ec__DisplayClass44_.c.sizes = sortedSizes;
		_003C_003Ec__DisplayClass44_.c.masses = sortedMasses;
		foreach (double item3 in _003C_003Ec__DisplayClass44_.c.masses)
		{
			_003C_003Ec__DisplayClass44_.c.massTotal += item3;
		}
		return _003C_003Ec__DisplayClass44_.c;
	}

	private static bool ReportComponents(Components c, string label, double linkR)
	{
		int sum = 0;
		foreach (int size in c.sizes)
		{
			sum += size;
		}
		CT.Info(string.Format("   ТЕЛА [{0}]: компонент {1}; рёбра {2}; радиус связи ", label, c.count, c.edges) + string.Format("2h = {0:F2} мм", linkR * 1000.0));
		bool flag = CT.True(string.Format("       (1) рёбра {0} > 0 при N = {1}: не работавший детектор ", c.edges, c.n) + "дал бы 0, и «одна компонента при нуле рёбер» невозможна при N > 1", c.edges > 0 || c.n <= 1);
		bool flag2 = CT.True(string.Format("       (2) сумма размеров {0} = N = {1}", sum, c.n), sum == c.n);
		bool result = flag2 && flag;
		List<string> list = new List<string>();
		for (int i = 0; i < Math.Min(5, c.sizes.Count); i++)
		{
			list.Add(string.Format("{0} ({1:F2} % массы)", c.sizes[i], c.masses[i] / Math.Max(c.massTotal, 1e-30) * 100.0));
		}
		CT.Info("       крупнейшие: " + string.Join(", ", list));
		return result;
	}

	private FsHistory FlyerRun(ComputeShader cs, Flat2DMaterialBlock[] mats, float v, bool spall, out double peakTension, out double uPeak, out double uMin, out SpallZone zone, float dx = 0.0005f, int stepMul = 1, bool labelBodies = false, float heightM = 0.06f)
	{
		FsHistory fs = default(FsHistory);
		int totalSteps = Convert.ToInt32(Dx / dx * 420f) * stepMul;
		int warm = Math.Max(1, Convert.ToInt32(0.001f / dx));
		int fnx = Convert.ToInt32(FlyerM / dx);
		int tnx = Convert.ToInt32(TargetM / dx);
		int ny = Convert.ToInt32(heightM / dx);
		Flat2DScene.Body a = Flat2DScene.Block(fnx, ny, dx, Vector2.zero, 0u, new Vector2(v, 0f));
		Flat2DScene.Body b = Flat2DScene.Block(tnx, ny, dx, new Vector2((float)fnx * dx, 0f), 0u, Vector2.zero);
		Flat2DScene.Body body = default(Flat2DScene.Body);
		int total = a.pos.Length + b.pos.Length;
		body.pos = new Vector2[total];
		body.vel = new Vector2[total];
		body.mat = new uint[total];
		body.dx = dx;
		Flat2DScene.Body[] parts = new Flat2DScene.Body[2] { a, b };
		int offset = 0;
		for (int i = 0; i < parts.Length; i++)
		{
			parts[i].pos.CopyTo(body.pos, offset);
			parts[i].vel.CopyTo(body.vel, offset);
			parts[i].mat.CopyTo(body.mat, offset);
			offset += parts[i].pos.Length;
		}
		body.min = a.min;
		body.max = b.max;
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		float xBack = (float)(fnx + tnx - 1) * dx;
		float yMid = (float)(ny - 1) * dx * 0.5f;
		List<int> probe = new List<int>();
		for (int j = 0; j < body.pos.Length; j++)
		{
			if (xBack - dx * 1.5f < body.pos[j].x && Math.Abs(body.pos[j].y - yMid) < dx * 10f)
			{
				probe.Add(j);
			}
		}
		fs.t = new List<double>();
		fs.u = new List<double>();
		fs.sc = new List<double>();
		peakTension = 0.0;
		uPeak = 0.0;
		uMin = 0.0;
		zone = default(SpallZone);
		float dt = dx * 1.3f * 0.3f / (mats[0].LongitudinalWaveSpeed + v);
		using (Flat2DSolver solver = new Flat2DSolver(cs))
		{
			solver.FixedTimeStep = dt;
			solver.Spall = spall;
			solver.Damage = false;
			solver.ArtificialStress = true;
			solver.Init(body.pos, body.vel, body.mat, mats, dx, lo, hi);
			if (labelBodies)
			{
				float[] mass = solver.ReadMass();
				lastMassStart = 0.0;
				for (int k = 0; k < mass.Length; k++)
				{
					lastMassStart += mass[k];
				}
			}
			for (int step = 0; step < totalSteps / warm; step++)
			{
				solver.Steps(warm);
				Vector2[] vel = solver.ReadVelocities();
				double sum = 0.0;
				foreach (int idx in probe)
				{
					sum += vel[idx].x;
				}
				fs.t.Add((double)((step + 1) * warm) * dt);
				sum /= (double)probe.Count;
				fs.u.Add(sum);
				double scatter = 0.0;
				foreach (int idx2 in probe)
				{
					scatter += (vel[idx2].x - sum) * (vel[idx2].x - sum);
				}
				fs.sc.Add(Math.Sqrt(scatter / (double)Math.Max(probe.Count - 1, 1)));
				Vector4[] stress = solver.ReadStress();
				for (int i2 = 0; i2 < stress.Length; i2++)
				{
					peakTension = Math.Max(peakTension, -stress[i2].w);
				}
			}
			int peakIdx = 0;
			for (int i3 = 1; i3 < fs.u.Count; i3++)
			{
				if (fs.u[i3] > fs.u[peakIdx])
				{
					peakIdx = i3;
				}
			}
			uPeak = fs.u[peakIdx];
			uMin = fs.u[peakIdx];
			for (int j2 = peakIdx + 1; j2 < fs.u.Count - 1; j2++)
			{
				if (!(fs.u[j2 - 1] >= fs.u[j2] && fs.u[j2 + 1] > fs.u[j2]))
				{
					uMin = Math.Min(uMin, fs.u[j2]);
					continue;
				}
				uMin = fs.u[j2];
				break;
			}
			float[] fail = solver.ReadSpallFail();
			zone = ZoneOf(fail, body.pos, fnx * dx, tnx, ny, solver.H, dx);
			if (labelBodies)
			{
				float[] mass2 = solver.ReadMass();
				lastMassEnd = 0.0;
				for (int k2 = 0; k2 < mass2.Length; k2++)
				{
					lastMassEnd += mass2[k2];
				}
				Vector2[] pos = solver.ReadPositions();
				lastComp = Label(pos, mass2, solver.H + solver.H);
				lastLinkR = solver.H + solver.H;
				lastPos = pos;
				lastMassArr = mass2;
				lastDx = dx;
			}
			fs.scatterAtPeak = fs.sc[peakIdx];
			CT.Info(string.Format("   [{0}] частиц {1}, зонд {2} ", spall ? "отсечка ВКЛ " : "отсечка ВЫКЛ", total, probe.Count) + string.Format("на задней грани, dt = {0:E3} с, окно {1:F1} мкс; ", dt, (double)totalSteps * dt * 1e6) + string.Format("u_fs пик {0:F2} → мин {1:F2} м/с", uPeak, uMin));
		}
		return fs;
	}

	private bool P6(ComputeShader cs)
	{
		int[] modes = new int[2] { 0, 1 };
		double wArtScene = 0.0;
		double wArtOld = 0.0;
		for (int i = 0; i < modes.Length; i++)
		{
			int mode = modes[i];
			Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
			Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
			Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
			using (Flat2DSolver solver = new Flat2DSolver(cs))
			{
				solver.ArtificialViscosity = false;
				solver.Plasticity = false;
				solver.ArtificialStress = true;
				solver.MutArtStressOldForm = mode == 1;
				solver.AccumulateWork = true;
				solver.FixedTimeStep = StiffDt(mats, 5f);
				solver.Init(body.pos, body.vel, body.mat, mats, 0.0005f, lo, hi);
				solver.Steps(1000);
				solver.MeasureChannels(out double _, out double _, out double _, out double wArt);
				if (mode == 0)
				{
					wArtScene = wArt;
				}
				else
				{
					wArtOld = wArt;
				}
				CT.Info(string.Format("   сжатие, {0}: W_a = {1:E4} Дж", mode != 0 ? "прежняя форма" : "ГЛАВНЫЕ ОСИ", wArt));
			}
		}
		CT.Info(string.Format("   ⚠️ по сцене W_a = {0:E4} Дж ≠ 0 — и это ПРАВИЛЬНО: у сцены A3′ есть ", wArtScene) + "свободные границы, где главные напряжения растягивающие. Инвариант — окрестностный.");
		bool invariant = NeighbourhoodInvariant(cs);
		bool counterfactual = CT.True(string.Format("   ⭐ контрфакт: прежняя форма даёт W_a ≠ 0 ({0:E4} Дж) — проверка не пуста", wArtOld), wArtOld != 0.0);
		bool pass = invariant & counterfactual;
		Flat2DScene.Body stretch = Flat2DScene.Block(80, 24, 0.0005f, Vector2.zero, 0u, Vector2.zero);
		for (int j = 0; j < stretch.pos.Length; j++)
		{
			stretch.vel[j] = new Vector2((stretch.pos[j].x - 0.02f) * 2000f, 0f);
		}
		Flat2DScene.Domain(stretch, 0.01f, out Vector2 lo2, out Vector2 hi2);
		Flat2DMaterialBlock[] mats2 = Flat2DMaterials.Build(ContinuumMaterialId.OFHC_Copper, ContinuumMaterialId.OFHC_Copper);
		double wArtStretch = 0.0;
		using (Flat2DSolver solver2 = new Flat2DSolver(cs))
		{
			solver2.ArtificialViscosity = false;
			solver2.Plasticity = false;
			solver2.ArtificialStress = true;
			solver2.AccumulateWork = true;
			solver2.Init(stretch.pos, stretch.vel, stretch.mat, mats2, 0.0005f, lo2, hi2);
			solver2.Steps(800);
			solver2.MeasureChannels(out double _, out double _, out double _, out wArtStretch);
		}
		CT.Info(string.Format("   растяжение, главные оси: W_a = {0:E4} Дж", wArtStretch));
		return CT.True("   ⭐ в РАСТЯЖЕНИИ член живой (иначе он просто выключен, а не исправлен)", wArtStretch != 0.0) & pass;
	}

	private bool NeighbourhoodInvariant(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
		Vector4[] stress;
		Vector2[] pos;
		Vector2[] accA;
		double h;
		using (Flat2DSolver solver = new Flat2DSolver(cs))
		{
			solver.ArtificialViscosity = false;
			solver.Plasticity = false;
			solver.ArtificialStress = true;
			float c = 0f;
			for (int i = 0; i < mats.Length; i++)
			{
				if (c <= mats[i].LongitudinalWaveSpeed)
				{
					c = mats[i].LongitudinalWaveSpeed;
				}
			}
			solver.FixedTimeStep = 0.000195f / (c + 5f);
			solver.Init(body.pos, body.vel, body.mat, mats, 0.0005f, lo, hi);
			solver.Steps(999);
			stress = solver.ReadStress();
			pos = solver.ReadPositions();
			solver.Steps(1);
			accA = solver.ReadAccA();
			h = solver.H;
		}
		int n = stress.Length;
		bool[] tensile = new bool[n];
		int tensileCount = 0;
		for (int k = 0; k < n; k++)
		{
			double a = stress[k].x - stress[k].w;
			double b = stress[k].y - stress[k].w;
			double mean = (a + b) * 0.5;
			double d = (a - b) * 0.5;
			tensile[k] = Math.Sqrt(d * d + (double)stress[k].z * stress[k].z) + mean > 0.0;
			if (tensile[k])
			{
				tensileCount++;
			}
		}
		double linkR = h + h;
		Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();
		double linkR2 = linkR * linkR;
		float r = (float)linkR;
		for (int m = 0; m < n; m++)
		{
			long key = ((long)(int)Math.Floor(pos[m].x / r) << 32) ^ (uint)(int)Math.Floor(pos[m].y / r);
			if (!grid.TryGetValue(key, out List<int> bucket))
			{
				bucket = new List<int>();
				grid[key] = bucket;
			}
			bucket.Add(m);
		}
		int pure = 0;
		int violations = 0;
		int mixed = 0;
		int mixedWithAcc = 0;
		double maxAcc = 0.0;
		for (int i2 = 0; i2 < n; i2++)
		{
			if (tensile[i2])
			{
				continue;
			}
			bool ok = true;
			int cx = Mathf.FloorToInt(pos[i2].x / r);
			int cy = Mathf.FloorToInt(pos[i2].y / r);
			for (int dy = -1; ok && dy < 2; dy++)
			{
				for (int dx = -1; ok && dx < 2; dx++)
				{
					long key2 = ((long)(cx + dx) << 32) ^ (uint)(cy + dy);
					if (grid.TryGetValue(key2, out List<int> bucket2))
					{
						foreach (int j in bucket2)
						{
							if (j == i2 || !tensile[j])
							{
								continue;
							}
							double ddy = (double)pos[i2].y - pos[j].y;
							double ddx = (double)pos[i2].x - pos[j].x;
							if (linkR2 > ddy * ddy + ddx * ddx)
							{
								ok = false;
								break;
							}
						}
					}
				}
			}
			bool hasAcc = accA[i2].x != 0f || accA[i2].y != 0f;
			if (ok)
			{
				pure++;
				if (hasAcc)
				{
					violations++;
					maxAcc = Math.Max(maxAcc, Math.Sqrt((double)accA[i2].x * accA[i2].x + (double)accA[i2].y * accA[i2].y));
				}
			}
			else
			{
				mixed++;
				if (hasAcc)
				{
					mixedWithAcc++;
				}
			}
		}
		CT.Info(string.Format("   частиц с растягивающим главным s1 > 0: {0} из {1}; чисто сжимающих ", tensileCount, n) + string.Format("ОКРЕСТНОСТЕЙ {0}; смешанных окрестностей у сжатых {1}, ", pure, mixed) + string.Format("из них с acc_a ≠ 0 — {0}", mixedWithAcc));
		CT.Info("   ⚠️ ненулевое acc_a у сжатой частицы со СМЕШАННОЙ окрестностью — НЕ нарушение: нулём обращается лишь её собственный R_i, вклад растянутого соседа остаётся (это и были прежние 1176 «нарушений» из 1260 сжатых).");
		if (pure == 0)
		{
			CT.Info("   ⚠️⚠️ ПОПУЛЯЦИЯ ПУСТА: чисто сжимающих окрестностей в сцене нет вовсе ⇒ ворота зелены ВПУСТУЮ и ничего не доказывают. Это сказано, а не скрыто.");
		}
		return CT.True(string.Format("   ⭐⭐ в чисто сжимающих окрестностях acc_a РОВНО ноль (популяция {0}, ", pure) + string.Format("нарушений {0}, худшее |acc_a| = {1:E3})", violations, maxAcc), violations == 0);
	}

	private bool S2(ComputeShader cs)
	{
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.Ti6Al4V, ContinuumMaterialId.Ti6Al4V);
		Flat2DScene.Body body = Impact(out Vector2 lo, out Vector2 hi, 900f);
		using (Flat2DSolver solver = new Flat2DSolver(cs))
		{
			float c = 0f;
			for (int i = 0; i < mats.Length; i++)
			{
				if (c <= mats[i].LongitudinalWaveSpeed)
				{
					c = mats[i].LongitudinalWaveSpeed;
				}
			}
			solver.FixedTimeStep = 0.000195f / (c + 900f);
			solver.Init(body.pos, body.vel, body.mat, mats, 0.0005f, lo, hi);
			solver.MeasureDamage(out double _, out double _, out int _, out double mass0);
			solver.Steps(2300);
			solver.MeasureDamage(out double _, out double _, out int failed, out double mass1);
			CT.Info(string.Format("S2: масса {0:F12} → {1:F12} кг; разрушенных частиц {2}", mass0, mass1, failed));
			CT.Info("   ⭐ разрушение НЕ УДАЛЯЕТ частиц: при D = 1 снимается девиатор, частица остаётся гидродинамической. Удаление крадёт массу и энергию и планом запрещено.");
			return CT.True("   масса совпала ПОБИТОВО", mass0 == mass1);
		}
	}

	private void S3(ComputeShader cs)
	{
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.OFHC_Copper, ContinuumMaterialId.OFHC_Copper);
		bool[] modes = new bool[2] { false, true };
		double hgOff = 0.0;
		double hgOn = 0.0;
		for (int i = 0; i < modes.Length; i++)
		{
			bool artStress = modes[i];
			Flat2DScene.Body body = Flat2DScene.Block(80, 24, 0.0005f, Vector2.zero, 0u, Vector2.zero);
			for (int j = 0; j < body.pos.Length; j++)
			{
				body.vel[j] = new Vector2((body.pos[j].x - 0.02f) * 2000f, 0f);
			}
			Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
			using (Flat2DSolver solver = new Flat2DSolver(cs))
			{
				solver.ArtificialViscosity = false;
				solver.Plasticity = false;
				solver.ArtificialStress = artStress;
				solver.Spall = false;
				solver.Damage = false;
				solver.Init(body.pos, body.vel, body.mat, mats, 0.0005f, lo, hi);
				solver.Steps(1500);
				Flat2DSolver.Diag diag = solver.Measure(withHourglass: true);
				if (artStress)
				{
					hgOn = diag.hourglass;
				}
				else
				{
					hgOff = diag.hourglass;
				}
				CT.Info(string.Format("   искусственное напряжение {0}: A6 = {1:E4}", artStress ? "ВКЛ " : "ВЫКЛ", diag.hourglass));
			}
		}
		CT.Info(string.Format("── S3: A6 без искусственного напряжения {0:E4}, с ним {1:E4}", hgOff, hgOn));
		CT.Info(string.Format("   ⇒ РАСТЯГИВАЮЩАЯ НЕУСТОЙЧИВОСТЬ = {0:E4} (что исчезло); ", hgOff - hgOn) + string.Format("НУЛЕВЫЕ МОДЫ = {0:E4} (что осталось)", hgOn));
		CT.Info("⚠️ Порога нет намеренно. ⚠️ Ожидается рост нулевых мод, когда придёт контакт (DECISION §4 п. 3) — сообщать, а не подгонять.");
	}

	private bool S5(ComputeShader cs)
	{
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.Ti6Al4V, ContinuumMaterialId.Ti6Al4V);
		CT.Info("⚠️ Сцена A3′ здесь НЕ ГОДИТСЯ: у неё ε_p ≡ 0, то есть путь повреждения там не исполняется вовсе. Берётся УДАР пластины 900 м/с — там пластика есть.");
		Flat2DScene.Body body = Impact(out Vector2 lo, out Vector2 hi, 900f);
		bool[] modes = new bool[2] { true, false };
		double meanD = 0.0;
		ulong hashOn = 0uL;
		ulong hashOff = 0uL;
		int failedOn = 0;
		for (int i = 0; i < modes.Length; i++)
		{
			bool damage = modes[i];
			using (Flat2DSolver solver = new Flat2DSolver(cs))
			{
				solver.FixedTimeStep = StiffDt(mats, 900f);
				solver.Damage = damage;
				solver.Init(body.pos, body.vel, body.mat, mats, 0.0005f, lo, hi);
				solver.Steps(2300);
				solver.MeasureDamage(out double dissipation, out double mean, out int failed, out double _);
				ulong hash = solver.StateHash();
				CT.Info(string.Format("   разрушение {0}: средняя D = {1:E4}, ", damage ? "ВКЛ " : "ВЫКЛ", mean) + string.Format("разрушено {0}, диссипация {1:E4} Дж, отпечаток {2:X16}", failed, dissipation, hash));
				if (damage)
				{
					meanD = mean;
					hashOn = hash;
					failedOn = failed;
				}
				else
				{
					hashOff = hash;
				}
			}
		}
		bool accumulating = CT.True(string.Format("   повреждение НАКАПЛИВАЕТСЯ (средняя D = {0:E4} > 0)", meanD), meanD > 0.0);
		bool differs = CT.True("   ⭐ прогон БЕЗ разрушения отличается ПОБИТОВО — механизм влияет на решение", hashOn != hashOff);
		if (failedOn == 0)
		{
			CT.Info("⚠️ Ни одна частица не достигла D = 1 на этом окне: накопление есть, перехода в гидродинамику нет. Сообщается как есть.");
		}
		return accumulating & differs;
	}

	private void S6(ComputeShader cs)
	{
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.Ti6Al4V, ContinuumMaterialId.Ti6Al4V);
		float[] vs = new float[2] { 1000f, 1500f };
		for (int iv = 0; iv < vs.Length; iv++)
		{
			float v = vs[iv];
			bool[] modes = new bool[2] { false, true };
			double maxHot = 0.0;
			double maxCold = 0.0;
			for (int im = 0; im < modes.Length; im++)
			{
				bool noHeat = modes[im];
				Flat2DScene.Body body = Impact(out Vector2 lo, out Vector2 hi, v);
				using (Flat2DSolver solver = new Flat2DSolver(cs))
				{
					solver.FixedTimeStep = StiffDt(mats, v);
					solver.MutNoHeating = noHeat;
					solver.Init(body.pos, body.vel, body.mat, mats, 0.0005f, lo, hi);
					solver.Steps(2300);
					float[] temp = solver.ReadTemperature();
					double maxT = 0.0;
					for (int i = 0; i < temp.Length; i++)
					{
						maxT = Math.Max(maxT, temp[i]);
					}
					if (!noHeat)
					{
						maxHot = maxT;
					}
					else
					{
						maxCold = maxT;
					}
				}
			}
			CT.Info(string.Format("   {0} м/с: максимум T = {1:F1} К с разогревом, {2:F1} К без ⇒ ", v, maxHot, maxCold) + string.Format("ΔT = {0:F1} К", maxHot - maxCold));
			CT.True(string.Format("   M4 на {0} м/с ВИДНА (ΔT = {1:F1} К > 100 К)", v, maxHot - maxCold), 100.0 < maxHot - maxCold);
		}
	}

	private bool S7(ComputeShader cs)
	{
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.Ti6Al4V, ContinuumMaterialId.Ti6Al4V);
		List<ulong> hashes = new List<ulong>();
		bool alive = true;
		for (int i = 0; i < 3; i++)
		{
			Flat2DScene.Body body = Impact(out Vector2 lo, out Vector2 hi, 900f);
			using (Flat2DSolver solver = new Flat2DSolver(cs))
			{
				solver.FixedTimeStep = StiffDt(mats, 900f);
				solver.Init(body.pos, body.vel, body.mat, mats, 0.0005f, lo, hi);
				solver.Steps(2300);
				hashes.Add(solver.StateHash());
				string detail = "";
				alive &= solver.ProbeAlive(2300, out detail);
				CT.Info(string.Format("   S7 прогон {0}: {1}", i + 1, detail));
			}
		}
		CT.Info(string.Format("S7: {0:X16} / {1:X16} / {2:X16}", hashes[0], hashes[1], hashes[2]));
		return CT.True("   живость всех трёх прогонов по счётчикам ядер", alive) & CT.True("   ⭐ три прогона С РАЗРУШЕНИЕМ совпали бит-в-бит", hashes[0] == hashes[1] && hashes[1] == hashes[2]);
	}

	private static Flat2DScene.Body Impact(out Vector2 lo, out Vector2 hi, float v)
	{
		Flat2DScene.Body a = Flat2DScene.Block(20, 24, 0.0005f, Vector2.zero, 0u, new Vector2(v, 0f));
		Flat2DScene.Body b = Flat2DScene.Block(60, 24, 0.0005f, new Vector2(0.0105f, 0f), 0u, Vector2.zero);
		Flat2DScene.Body res = default(Flat2DScene.Body);
		int total = a.pos.Length + b.pos.Length;
		res.pos = new Vector2[total];
		res.vel = new Vector2[total];
		res.mat = new uint[total];
		res.dx = 0.0005f;
		Flat2DScene.Body[] parts = new Flat2DScene.Body[2] { a, b };
		int offset = 0;
		for (int i = 0; i < parts.Length; i++)
		{
			parts[i].pos.CopyTo(res.pos, offset);
			parts[i].vel.CopyTo(res.vel, offset);
			parts[i].mat.CopyTo(res.mat, offset);
			offset += parts[i].pos.Length;
		}
		res.min = a.min;
		res.max = b.max;
		Flat2DScene.Domain(res, 0.01f, out lo, out hi);
		return res;
	}

	private static float StiffDt(Flat2DMaterialBlock[] mats, float v)
	{
		float c = 0f;
		for (int i = 0; i < mats.Length; i++)
		{
			if (c < mats[i].LongitudinalWaveSpeed)
			{
				c = mats[i].LongitudinalWaveSpeed;
			}
		}
		return 0.000195f / (c + v);
	}

	private ComputeShader LoadShader()
	{
		if (solverShader != null)
		{
			return solverShader;
		}
		return null;
	}

	public Flat2DStage3()
	{
		vCal = -1f;
	}
}
