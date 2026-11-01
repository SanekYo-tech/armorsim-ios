using System;
using UnityEngine;

public sealed class Flat2DTwoBodies : MonoBehaviour
{
	public sealed class ContactBResult
	{
		public bool ca1Close;

		public bool ca2Worked;

		public bool ca3Counterfactual;

		public bool caHeadline;

		public bool caCounterfactualRed;

		public bool cb;

		public bool cc;

		public bool cd;

		public bool alive;

		public bool aliveOff;

		public bool crossSeen;

		public bool hazardZero;

		public bool cbPrimeB;

		public bool cbPrimeMutant;

		public bool finite;

		public bool mutantBitIdenticalToB;

		public double gapOn;

		public double gapOff;

		public double overlapOn;

		public double overlapOff;

		public double maxAccC;

		public double wContact;

		public double wContactOff;

		public double pDriftOn;

		public double pDriftOff;

		public double a6On;

		public double a6OnAllPairs;

		public double a6Off;

		public double a6OffAllPairs;

		public uint maxCrossPairs;

		public uint hazard;

		public BPrimeRun primeB;

		public BPrimeRun primeWelded;

		public BPrimeRun primeMutant;

		public bool AllGreen => ca1Close && ca2Worked && ca3Counterfactual && caHeadline && caCounterfactualRed && cb && cc && cd && alive && aliveOff && crossSeen && hazardZero && cbPrimeB && cbPrimeMutant && finite;
	}

	public sealed class BPrimeRun
	{
		public string label;

		public bool separated;

		public bool finite;

		public int sStar;

		public int recontacts;

		public int samples;

		public double minFirst;

		public double minLast;

		public double maxPull;

		public double vA0;

		public double vA1;

		public double vB0;

		public double vB1;

		public ulong hash;

		public string alive;
	}

	public enum FlyerPair
	{
		Cu = 0,
		Wha = 1
	}

	public sealed class FlyerScene
	{
		public FlyerPair pair;

		public Flat2DScene.Body body;

		public uint[] bodyIds;

		public Flat2DMaterialBlock[] mats;

		public ContinuumMaterialId flyerId;

		public ContinuumMaterialId targetId;

		public Vector2 lo;

		public Vector2 hi;

		public float v;

		public float dx;

		public float dt;

		public int fnx;

		public int tnx;

		public int ny;

		public int steps;

		public float cFlyer;

		public double tauS;

		public bool[] rearFace;

		public int nRear;
	}

	public ComputeShader solverShader;

	private const string ShaderPath = "Assets/Scripts/Flat2D/Flat2DSolver.compute";

	public const float Dx = 0.0005f;

	public const double Ulp = 1.1920928955078125E-07;

	public const double TolRound = 64.0;

	private const int Nx = 16;

	private const int Ny = 40;

	private const float V = 600f;

	private static readonly float Gap0 = 0.003f;

	private static Flat2DMaterialBlock[] matsCache;

	private const int BPrimeSamples = 300;

	private const int BPrimeEvery = 2;

	public const int FlyerNx = 20;

	public const int TargetNx = 40;

	public const int FlyerNy = 120;

	public const int FlyerSteps = 420;

	public ContactBResult LastContactB { get; private set; }

	private static Flat2DMaterialBlock[] Mats => matsCache ?? (matsCache = Flat2DMaterials.Build(ContinuumMaterialId.OFHC_Copper, ContinuumMaterialId.OFHC_Copper));

	[ContextMenu("★ Два тела: T2 (контакт при раздельных телах)")]
	public void RunAll()
	{
		int pass = 0;
		int total = 0;
		ComputeShader cs = LoadShader();
		if (cs == null)
		{
			Debug.LogError("Flat2D: не найден Assets/Scripts/Flat2D/Flat2DSolver.compute");
			return;
		}
		Debug.Log("══════ ДВА ТЕЛА: T2 ══════");
		Debug.Log(string.Format("устройство: {0} ({1})", SystemInfo.deviceName, SystemInfo.graphicsDeviceType));
		CT.Run("T2", "C-a…C-d при SeparateBodies + контрфакт без контакта + C-b′ на шаге решётки", () => RunContactB(cs), ref pass, ref total);
		Debug.Log(string.Format("══════ ИТОГ ДВУХ ТЕЛ (T2): пройдено {0}, провалено {1} ══════", pass, total - pass));
	}

	public bool RunContactB()
	{
		return RunContactB(LoadShader());
	}

	public bool RunContactB(ComputeShader cs)
	{
		if (cs == null)
		{
			throw new InvalidOperationException("Flat2DTwoBodies: нет шейдера Assets/Scripts/Flat2D/Flat2DSolver.compute");
		}
		ContactBResult r = new ContactBResult();
		LastContactB = r;
		Approach(cs, false, out r.gapOn, out r.maxAccC, out r.wContact, out double pStartOn, out double pEndOn, out r.a6On, out r.a6OnAllPairs, out bool everClose, out r.overlapOn, out bool aliveOn, out string aliveDetailOn, out uint crossOn, out uint hazardOn);
		Approach(cs, true, out r.gapOff, out double maxAccOff, out r.wContactOff, out double pStartOff, out double pEndOff, out r.a6Off, out r.a6OffAllPairs, out bool _, out r.overlapOff, out bool aliveOffFlag, out string aliveDetailOff, out uint crossOff, out uint hazardOff);
		r.pDriftOn = Math.Abs(pEndOn - pStartOn);
		r.pDriftOff = Math.Abs(pEndOff - pStartOff);
		r.maxCrossPairs = Math.Max(crossOn, crossOff);
		r.hazard = hazardOn + hazardOff;

		CT.Info(string.Format("── C-a (B): СБЛИЖЕНИЕ. d0 = Δx = {0:F2} мм, старт зазора {1:F2} мм, ", 0.5f, Gap0 * 1000f) + string.Format("скорости ±{0} м/с; тела раздельны, суммы видят только своё тело, контакт — KContactPair", 600f));
		CT.Info(string.Format("   [B, контакт ВКЛ ] минимальное расстояние пары {0:F3} мм; глубина ПЕРЕКРЫТИЯ ", r.gapOn * 1000.0) + string.Format("{0:F3} мм; max|acc_c| = {1:E3} м/с²; межтельных пар (max суммы за замер) {2}", r.overlapOn * 1000.0, r.maxAccC, crossOn));
		CT.Info(string.Format("   [B, контакт ВЫКЛ] минимальное расстояние пары {0:F3} мм; глубина ПЕРЕКРЫТИЯ ", r.gapOff * 1000.0) + string.Format("{0:F3} мм; max|acc_c| = {1:E3} м/с² (обязан быть 0)", r.overlapOff * 1000.0, maxAccOff));
		r.ca1Close = CT.True("   (1) СБЛИЖЕНИЕ СОСТОЯЛОСЬ: зазор опускался ниже d0 ⇒ контакт был испытан", everClose);
		r.ca2Worked = CT.True(string.Format("   (2) КОНТАКТ ОТРАБОТАЛ: max|acc_c| = {0:E3} > 0 и работа канала {1:E4} Дж ≠ 0", r.maxAccC, r.wContact), r.maxAccC > 0.0 && Math.Abs(r.wContact) > 0.0);
		bool counter = CT.True("   (3) КОНТРФАКТ: БЕЗ контакта тела сходятся ближе или перекрываются глубже " + string.Format("(расстояние {0:F3} против {1:F3} мм; перекрытие ", r.gapOff * 1000.0, r.gapOn * 1000.0) + string.Format("{0:F3} против {1:F3} мм)", r.overlapOff * 1000.0, r.overlapOn * 1000.0), r.gapOff < r.gapOn || r.overlapOff < r.overlapOn);
		bool counterZero = CT.True(string.Format("   (4) КОНТРФАКТ ЧЕСТЕН: max|acc_c| без контакта = {0:E3} = 0 ровно", maxAccOff), maxAccOff == 0.0);
		r.ca3Counterfactual = counter && counterZero;
		r.caHeadline = CT.True(string.Format("   ⭐ C-a: С контактом ПЕРЕСЕЧЕНИЯ НЕТ — глубина перекрытия {0:F3} мм ≤ 0", r.overlapOn * 1000.0), r.overlapOn <= 0.0);
		r.caCounterfactualRed = CT.True("   ⭐ КОНТРФАКТ КРАСНЫЙ: без KContactPair заголовок C-a ПРОВАЛЕН — перекрытие " + string.Format("{0:F3} мм > 0 (тела проходят друг сквозь друга)", r.overlapOff * 1000.0), r.overlapOff > 0.0);
		CT.Info("   живость (контакт ВКЛ):  " + aliveDetailOn);
		CT.Info("   живость (контакт ВЫКЛ): " + aliveDetailOff);
		r.alive = CT.True("   ⭐ девять ядер НЕ пропущены (контакт ВКЛ; счётчики = шагам + разгон)", aliveOn);
		r.aliveOff = CT.True("   ⭐ контрфакт честен: KContactPair НЕ диспетчился ни разу, остальные — да", aliveOffFlag);
		r.crossSeen = CT.True(string.Format("   ⭐ тела действительно стали СОСЕДЯМИ: межтельных пар в 2h (max) {0} > 0", r.maxCrossPairs), r.maxCrossPairs != 0u);
		r.hazardZero = CT.True(string.Format("   ⭐ посылка §3.6: межтельная Π-работа ни разу не вышла < 0 (счётчик [7] = {0}); ", r.hazard) + "не ноль — отчёт, НЕ зажим", r.hazard == 0u);

		Flat2DScene.Body body = TwoPlates(-150f, 150f, Gap0, out Vector2 lo, out Vector2 hi, out uint[] bodyIds);
		double startGap;
		double endGap;
		double maxPull;
		bool monotonic;
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.FixedTimeStep = PlatesDt();
			s.SeparateBodies = true;
			s.BodyIds = bodyIds;
			s.Spall = false;
			s.Damage = false;
			s.ArtificialStress = true;
			s.Init(body.pos, body.vel, body.mat, Mats, Dx, lo, hi);
			int half = body.pos.Length / 2;
			startGap = 0.0;
			endGap = 0.0;
			maxPull = 0.0;
			monotonic = true;
			for (int sample = 0; sample < 150; sample++)
			{
				s.Steps(2);
				Vector2[] pos = s.ReadPositions();
				double maxX = -1E+30;
				double minX = 1E+30;
				for (int i = 0; i < pos.Length; i++)
				{
					if (Mathf.Abs(pos[i].y - 0.00975f) <= 0.0015f)
					{
						if (i < half)
						{
							maxX = Math.Max(maxX, pos[i].x);
						}
						else
						{
							minX = Math.Min(minX, pos[i].x);
						}
					}
				}
				double gap = minX - maxX;
				if (sample == 0)
				{
					startGap = gap;
				}
				if (gap < endGap - 1E-09)
				{
					monotonic = false;
				}
				Vector2[] acc = s.ReadAccC();
				for (int i = 0; i < half; i++)
				{
					maxPull = Math.Max(maxPull, acc[i].x);
				}
				endGap = gap;
			}
			CT.Info(string.Format("── C-b (B): РАСХОЖДЕНИЕ. ⚠️ ПУСТА ПО ПОСТРОЕНИЮ (AMENDMENTS M3): старт зазора ", Gap0 * 1000f) + string.Format("{0:F2} мм > 2h = {1:F2} мм, тела не взаимодействуют ", Gap0 * 1000f, 1.3f) + string.Format("ни сцепленными, ни раздельными. Расстояние {0:F3} → {1:F3} мм", startGap * 1000.0, endGap * 1000.0));
			bool growing = CT.True("   C-b [пустая: нет взаимодействия]: зазор растёт монотонно", startGap < endGap && monotonic);
			bool noPull = CT.True(string.Format("   C-b [пустая]: max притягивающего acc_c у тела A = {0:E3} ≤ 0", maxPull), maxPull <= 0.0);
			r.cb = growing && noPull;
		}

		CT.Info(string.Format("── C-c (B): ИМПУЛЬС через контакт: p_x {0:E6} → {1:E6} кг·м/с ⇒ дрейф {2:E3}", pStartOn, pEndOn, r.pDriftOn));
		CT.Info(string.Format("   без контакта: {0:E6} → {1:E6} ⇒ {2:E3} — пол прибора A2", pStartOff, pEndOff, r.pDriftOff));
		r.cc = CT.True("   ⭐ C-c: дрейф импульса с контактом не хуже, чем без него (10× пол, не меньше 1e-6)", r.pDriftOn <= Math.Max(r.pDriftOff * 10.0, 1E-06));
		CT.Info(string.Format("── C-d (B): A6 ТЕЛЕСНАЯ (KHourglassB) {0:E4} (без контакта) → {1:E4} (с контактом)", r.a6Off, r.a6On));
		CT.Info(string.Format("   A6 ПО ВСЕМ ПАРАМ (KHourglass) {0:E4} → {1:E4} — через скользящий интерфейс ", r.a6OffAllPairs, r.a6OnAllPairs) + "прибор всех пар читает скачок скорости между телами как ошибку часов; печатается рядом, не гейтится");
		CT.Info("   сравнение со сцепленным G4Contact — по журналу RunStage3 того же дерева (раннер)");
		CT.Info(string.Format("   ЗАМЕР: W_контакт = {0:E4} Дж (без контакта {1:E4} Дж — обязан быть РОВНО 0)", r.wContact, r.wContactOff));
		r.cd = CT.True("   ⭐ канал контакта пуст, когда контакта нет", r.wContactOff == 0.0);

		r.primeB = BPrime(cs, "B", true, false);
		r.primeWelded = BPrime(cs, "сцеплено", false, false);
		r.primeMutant = BPrime(cs, "B + MutUnweldKeepTension", true, true);
		CT.Info(string.Format("── C-b′: ПЛАСТИНЫ НА ШАГЕ РЕШЁТКИ (узлы через Δx = {0:F2} мм), разлёт ∓{1} м/с, ", 0.5f, 150f) + string.Format("{0} замеров × {1} шага; «разошлись» ⇔ истинное двумерное min расстояние > 2h = ", 300, 2) + string.Format("{0:F2} мм на КАЖДОМ замере до конца", 1.3));
		BPrimeRun[] runs = new BPrimeRun[3] { r.primeB, r.primeWelded, r.primeMutant };
		for (int i = 0; i < runs.Length; i++)
		{
			BPrimeRun b = runs[i];
			CT.Info(string.Format("   [{0}] разошлись: {1}; s* = {2}; повторных касаний ", b.label, b.separated ? "ДА" : "нет", b.sStar) + string.Format("{0}; min расстояние {1:F3} → {2:F3} мм; ", b.recontacts, b.minFirst * 1000.0, b.minLast * 1000.0) + string.Format("max притягивающего acc_c по нормали {0:E3}; v_x ЦМ пластины A {1:F3} → {2:F3} м/с, ", b.maxPull, b.vA0, b.vA1) + string.Format("B {0:F3} → {1:F3} м/с; конечно: {2}; отпечаток {3:X16}", b.vB0, b.vB1, b.finite, b.hash) + (string.IsNullOrEmpty(b.alive) ? "" : "; " + b.alive));
		}
		r.finite = CT.True("   ⭐ C.15: состояние конечно во всех трёх прогонах C-b′ (NaN не читается как «сцеплены»)", r.primeB.finite && r.primeWelded.finite && r.primeMutant.finite);
		r.cbPrimeB = CT.True(string.Format("   ⭐ C-b′ [B]: разошлись до конца (s* = {0}) и max притягивающего acc_c ", r.primeB.sStar) + string.Format("по нормали {0:E3} ≤ 0", r.primeB.maxPull), r.primeB.separated && r.primeB.maxPull <= 0.0);
		r.mutantBitIdenticalToB = r.primeMutant.hash == r.primeB.hash;
		double mutantDelta = r.primeMutant.vA1 - r.primeMutant.vA0;
		r.cbPrimeMutant = CT.True(string.Format("   ⭐ C-b′ [мутант KeepTension]: НЕ разошёлся или тянет (ЦМ A {0:+0.000;-0.000} м/с ", mutantDelta) + "навстречу; порог 1 % разлёта) — клей обязан вернуться", !r.primeMutant.separated || mutantDelta > 1.5);
		if (r.mutantBitIdenticalToB)
		{
			CT.Info("   ⚠️⚠️ ДИАГНОЗ: мутант ПОБИТОВО равен B. Старт без напряжений (p(ρ₀, e = 0) = 0, S = 0) ⇒ r̂·σ̃·r̂ = 0 на каждой межтельной паре ⇒ мутация (оставлять только tn > 0) не срабатывает ни разу, а однородный разлёт каждого тела не рождает напряжения, которое её включило бы. C-b′ слеп к клею ПО ПОСТРОЕНИЮ — это находка о сцене, не повод подстраивать мутацию (дизайн §4.3).");
		}
		CT.Info("   [сцеплено] — запись, не ворота (M3): ожидалось «не расходится или тянет»; ЦМ A " + string.Format("{0:+0.000;-0.000} м/с навстречу", r.primeWelded.vA1 - r.primeWelded.vA0));
		return r.AllGreen;
	}

	private static float PlatesDt()
	{
		return 0.000195f / (Mats[0].LongitudinalWaveSpeed + 600f);
	}

	public static Flat2DScene.Body TwoPlates(float vA, float vB, float gap, out Vector2 lo, out Vector2 hi, out uint[] bodies)
	{
		Flat2DScene.Body a = Flat2DScene.Block(Nx, Ny, Dx, Vector2.zero, 0u, new Vector2(vA, 0f));
		Flat2DScene.Body b = Flat2DScene.Block(Nx, Ny, Dx, new Vector2(gap + 0.008f, 0f), 0u, new Vector2(vB, 0f));
		int count = a.pos.Length + b.pos.Length;
		Flat2DScene.Body body = new Flat2DScene.Body
		{
			pos = new Vector2[count],
			vel = new Vector2[count],
			mat = new uint[count],
			dx = Dx,
			min = new Vector2(a.min.x, b.min.y),
			max = new Vector2(b.max.x, a.max.y)
		};
		bodies = new uint[count];
		int offset = 0;
		Flat2DScene.Body[] parts = new Flat2DScene.Body[2] { a, b };
		for (int q = 0; q < parts.Length; q++)
		{
			Array.Copy(parts[q].pos, 0, body.pos, offset, parts[q].pos.Length);
			Array.Copy(parts[q].vel, 0, body.vel, offset, parts[q].vel.Length);
			Array.Copy(parts[q].mat, 0, body.mat, offset, parts[q].mat.Length);
			for (int i = 0; i < parts[q].pos.Length; i++)
			{
				bodies[offset + i] = (uint)q;
			}
			offset += parts[q].pos.Length;
		}
		Flat2DScene.Domain(body, 0.01f, out lo, out hi);
		return body;
	}

	private void Approach(ComputeShader cs, bool noContact, out double minGap, out double maxAccC, out double wContact, out double pStart, out double pEnd, out double a6, out double a6All, out bool everClose, out double maxOverlap, out bool alive, out string aliveDetail, out uint maxCross, out uint hazard)
	{
		Flat2DScene.Body body = TwoPlates(600f, -600f, Gap0, out Vector2 lo, out Vector2 hi, out uint[] bodies);
		float dt = PlatesDt();
		minGap = 1E+30;
		maxAccC = 0.0;
		wContact = 0.0;
		everClose = false;
		maxOverlap = 0.0;
		maxCross = 0u;
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.FixedTimeStep = dt;
			s.SeparateBodies = true;
			s.BodyIds = bodies;
			s.MutUnweldNoContact = noContact;
			s.Spall = false;
			s.Damage = false;
			s.ArtificialStress = true;
			s.AccumulateWork = true;
			s.Init(body.pos, body.vel, body.mat, Mats, Dx, lo, hi);
			pStart = s.Measure(true).Momentum;
			int half = body.pos.Length / 2;
			for (int sample = 0; sample < 300; sample++)
			{
				s.Steps(2);
				Vector2[] pos = s.ReadPositions();
				double best = 1E+30;
				double maxAX = -1E+30;
				double minBX = 1E+30;
				float interfaceX = Gap0 * 0.5f + 0.008f;
				for (int i = 0; i < half; i++)
				{
					if (Mathf.Abs(pos[i].x - interfaceX) <= 0.002f)
					{
						maxAX = Math.Max(maxAX, pos[i].x);
						for (int j = half; j < pos.Length; j++)
						{
							if (Mathf.Abs(pos[j].x - interfaceX) <= 0.002f)
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
					if (Mathf.Abs(pos[j].x - interfaceX) <= 0.002f)
					{
						minBX = Math.Min(minBX, pos[j].x);
					}
				}
				minGap = Math.Min(minGap, best);
				maxOverlap = Math.Max(maxOverlap, maxAX - minBX);
				if (best < 0.0005000000237487257)
				{
					everClose = true;
				}
				Vector2[] acc = s.ReadAccC();
				for (int i = 0; i < acc.Length; i++)
				{
					maxAccC = Math.Max(maxAccC, (double)acc[i].magnitude);
				}
				uint[] pairs = s.ReadCrossPairsF();
				uint sum = 0u;
				for (int i = 0; i < pairs.Length; i++)
				{
					sum += pairs[i];
				}
				maxCross = Math.Max(maxCross, sum);
			}
			Flat2DSolver.Diag diag = s.Measure(true);
			pEnd = diag.Momentum;
			a6 = diag.hourglass;
			a6All = diag.hourglassAllPairs;
			s.MeasureChannels(out double _, out double _, out double _, out double _, out wContact);
			alive = s.ProbeTwoBodiesAlive(600, out aliveDetail, out hazard);
		}
	}

	private BPrimeRun BPrime(ComputeShader cs, string label, bool separate, bool keepTension)
	{
		BPrimeRun r = new BPrimeRun
		{
			label = label,
			sStar = -1,
			alive = ""
		};
		Flat2DScene.Body body = TwoPlates(-150f, 150f, 0f, out Vector2 lo, out Vector2 hi, out uint[] bodies);
		float dt = PlatesDt();
		int half = body.pos.Length / 2;
		int lastClose = -1;
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.FixedTimeStep = dt;
			s.Spall = false;
			s.Damage = false;
			s.ArtificialStress = true;
			if (separate)
			{
				s.SeparateBodies = true;
				s.BodyIds = bodies;
				s.MutUnweldKeepTension = keepTension;
			}
			s.Init(body.pos, body.vel, body.mat, Mats, Dx, lo, hi);
			ComVx(s, half, out r.vA0, out r.vB0);
			bool finite = true;
			bool wasSeparated = false;
			for (int sample = 0; sample < BPrimeSamples; sample++)
			{
				s.Steps(BPrimeEvery);
				Vector2[] pos = s.ReadPositions();
				double closest2 = double.PositiveInfinity;
				for (int i = 0; i < half; i++)
				{
					if (!IsFinite(pos[i].x) || !IsFinite(pos[i].y))
					{
						finite = false;
						continue;
					}
					for (int j = half; j < pos.Length; j++)
					{
						double dx = pos[i].x - pos[j].x;
						double dy = pos[i].y - pos[j].y;
						closest2 = Math.Min(closest2, dx * dx + dy * dy);
					}
				}
				double closest = Math.Sqrt(closest2);
				if (!IsFinite(closest))
				{
					finite = false;
				}
				if (sample == 0)
				{
					r.minFirst = closest;
				}
				r.minLast = closest;
				if (closest <= 0.0013000000140629686)
				{
					if (wasSeparated)
					{
						r.recontacts++;
					}
					wasSeparated = false;
					lastClose = sample;
				}
				else
				{
					wasSeparated = true;
				}
				Vector2[] acc = s.ReadAccC();
				for (int i = 0; i < half; i++)
				{
					r.maxPull = Math.Max(r.maxPull, acc[i].x);
				}
				for (int i = half; i < acc.Length; i++)
				{
					r.maxPull = Math.Max(r.maxPull, -acc[i].x);
				}
			}
			r.samples = BPrimeSamples;
			r.sStar = lastClose + 1;
			r.separated = lastClose < BPrimeSamples - 1 && r.minLast > 0.0013000000140629686;
			ComVx(s, half, out r.vA1, out r.vB1);
			Vector2[] vel = s.ReadVelocities();
			for (int i = 0; i < vel.Length; i++)
			{
				if (!IsFinite(vel[i].x) || !IsFinite(vel[i].y))
				{
					finite = false;
				}
			}
			r.finite = finite;
			r.hash = s.StateHash();
			if (separate)
			{
				bool ok = s.ProbeTwoBodiesAlive(600, out string detail, out uint _);
				r.alive = (ok ? "" : "⛔ ") + detail;
			}
		}
		return r;
	}

	private static void ComVx(Flat2DSolver s, int half, out double vA, out double vB)
	{
		Vector2[] vel = s.ReadVelocities();
		float[] mass = s.ReadMass();
		double momentumA = 0.0;
		double momentumB = 0.0;
		double massA = 0.0;
		double massB = 0.0;
		for (int i = 0; i < vel.Length; i++)
		{
			if (i < half)
			{
				momentumA += (double)mass[i] * vel[i].x;
				massA += mass[i];
			}
			else
			{
				momentumB += (double)mass[i] * vel[i].x;
				massB += mass[i];
			}
		}
		vA = momentumA / Math.Max(massA, 1E-30);
		vB = momentumB / Math.Max(massB, 1E-30);
	}

	private static bool IsFinite(double x)
	{
		return !double.IsNaN(x) && !double.IsInfinity(x);
	}

	public static FlyerScene BuildFlyer(FlyerPair pair, float v = 200f, float dx = 0.0005f)
	{
		FlyerScene sc = new FlyerScene
		{
			pair = pair,
			v = v,
			dx = dx,
			fnx = FlyerNx,
			tnx = TargetNx,
			ny = FlyerNy,
			steps = FlyerSteps,
			flyerId = pair == FlyerPair.Cu ? ContinuumMaterialId.OFHC_Copper : ContinuumMaterialId.WHA,
			targetId = pair == FlyerPair.Cu ? ContinuumMaterialId.OFHC_Copper : ContinuumMaterialId.RHA_MeyerKleponis
		};
		sc.mats = Flat2DMaterials.Build(sc.flyerId, sc.targetId);
		Flat2DScene.Body a = Flat2DScene.Block(sc.fnx, sc.ny, dx, Vector2.zero, 0u, new Vector2(v, 0f));
		Flat2DScene.Body b = Flat2DScene.Block(sc.tnx, sc.ny, dx, new Vector2((float)sc.fnx * dx, 0f), 1u, Vector2.zero);
		int count = a.pos.Length + b.pos.Length;
		sc.body = new Flat2DScene.Body
		{
			pos = new Vector2[count],
			vel = new Vector2[count],
			mat = new uint[count],
			dx = dx,
			min = new Vector2(a.min.x, b.min.y),
			max = new Vector2(b.max.x, a.max.y)
		};
		sc.bodyIds = new uint[count];
		int offset = 0;
		Flat2DScene.Body[] parts = new Flat2DScene.Body[2] { a, b };
		for (int q = 0; q < parts.Length; q++)
		{
			Array.Copy(parts[q].pos, 0, sc.body.pos, offset, parts[q].pos.Length);
			Array.Copy(parts[q].vel, 0, sc.body.vel, offset, parts[q].vel.Length);
			Array.Copy(parts[q].mat, 0, sc.body.mat, offset, parts[q].mat.Length);
			for (int i = 0; i < parts[q].pos.Length; i++)
			{
				sc.bodyIds[offset + i] = (uint)q;
			}
			offset += parts[q].pos.Length;
		}
		Flat2DScene.Domain(sc.body, 0.01f, out sc.lo, out sc.hi);
		float fastest = 0f;
		for (int i = 0; i < sc.mats.Length; i++)
		{
			fastest = Mathf.Max(fastest, sc.mats[i].LongitudinalWaveSpeed);
		}
		sc.dt = dx * 1.3f * 0.3f / (fastest + v);
		sc.cFlyer = sc.mats[0].LongitudinalWaveSpeed;
		sc.tauS = (2.0 * sc.fnx * dx) / sc.cFlyer;
		sc.rearFace = new bool[count];
		float centreY = (float)(sc.ny - 1) * dx * 0.5f;
		for (int i = 0; i < count; i++)
		{
			sc.rearFace[i] = sc.body.pos[i].x >= ((float)(sc.fnx + sc.tnx) - 1.5f) * dx && Mathf.Abs(sc.body.pos[i].y - centreY) < dx * 10f;
			if (sc.rearFace[i])
			{
				sc.nRear++;
			}
		}
		return sc;
	}

	public static Flat2DSolver MakeFlyerSolver(ComputeShader cs, FlyerScene sc, bool twoBodies, float dtScale = 1f, bool unweldProbe = false)
	{
		Flat2DSolver s = new Flat2DSolver(cs);
		s.FixedTimeStep = sc.dt * dtScale;
		s.ArtificialStress = true;
		s.AccumulateWork = true;
		if (twoBodies)
		{
			s.SeparateBodies = true;
			s.BodyIds = sc.bodyIds;
			s.UnweldProbe = unweldProbe;
		}
		else if (unweldProbe)
		{
			throw new InvalidOperationException("Flat2DTwoBodies: UnweldProbe без двух тел решатель отвергнет");
		}
		s.Init(sc.body.pos, sc.body.vel, sc.body.mat, sc.mats, sc.dx, sc.lo, sc.hi);
		return s;
	}

	public static double RearFaceVx(Flat2DSolver s, FlyerScene sc)
	{
		if (sc.nRear == 0)
		{
			throw new InvalidOperationException("Flat2DTwoBodies: пустой селектор тыльной грани");
		}
		Vector2[] vel = s.ReadVelocities();
		double sum = 0.0;
		for (int i = 0; i < vel.Length; i++)
		{
			if (sc.rearFace[i])
			{
				sum += vel[i].x;
			}
		}
		return sum / sc.nRear;
	}

	public static double ImpedanceMatchUfs(ContinuumMaterialId flyer, ContinuumMaterialId target, double v)
	{
		ContinuumProperties f = ContinuumMaterials.Get(flyer);
		ContinuumProperties t = ContinuumMaterials.Get(target);
		double lo = 0.0;
		double hi = v;
		for (int i = 0; i < 200; i++)
		{
			double u = (lo + hi) * 0.5;
			double pf = ((double)f.S * (v - u) + f.C0) * f.rho0 * (v - u);
			double pt = ((double)t.S * u + t.C0) * t.rho0 * u;
			if (pf <= pt)
			{
				hi = u;
			}
			else
			{
				lo = u;
			}
		}
		return lo + hi;
	}

	public static bool CheckViscMinExact(Flat2DSolver s, int steps, out string report)
	{
		if (!s.SeparateBodies || !s.UnweldProbe)
		{
			throw new InvalidOperationException("Flat2DTwoBodies.CheckViscMinExact: нужен решатель с SeparateBodies и UnweldProbe (l1EV берётся из прибора CLOSE)");
		}
		float[] previous = s.ReadViscMinAll();
		uint lastCross = s.ReadTwoBodyProbe()[7];
		int firstStep = -1;
		int firstParticle = -1;
		int worstStep = -1;
		int worstParticle = -1;
		int dangerMismatch = 0;
		int safeSteps = 0;
		int cleanMismatch = 0;
		double minA = double.PositiveInfinity;
		double maxRatio = 0.0;
		for (int step = 0; step < steps; step++)
		{
			s.Steps(1);
			float[] edot = s.ReadEdotVisc();
			float[] current = s.ReadViscMinAll();
			uint cross = s.ReadTwoBodyProbe()[7];
			s.ReadUwF(out Vector4[] _, out Vector4[] _, out Vector4[] _, out Vector4[] _, out Vector4[] l1e, out Vector4[] _);
			if (cross <= lastCross)
			{
				safeSteps++;
			}
			for (int i = 0; i < current.Length; i++)
			{
				float expected = Math.Min(previous[i], edot[i]);
				if (current[i] != expected)
				{
					if (cross > lastCross)
					{
						dangerMismatch++;
					}
					else
					{
						cleanMismatch++;
						if (firstStep < 0)
						{
							firstStep = step + 1;
							firstParticle = i;
						}
					}
				}
				minA = Math.Min(minA, edot[i]);
				if (edot[i] < 0f)
				{
					double denom = (double)l1e[i].z * 7.62939453125E-06;
					double ratio = denom <= 0.0 ? double.PositiveInfinity : -(double)edot[i] / denom;
					if (ratio > maxRatio)
					{
						worstStep = step + 1;
						worstParticle = i;
						maxRatio = ratio;
					}
				}
			}
			previous = current;
			lastCross = cross;
		}
		bool clean = cleanMismatch == 0;
		bool roundoff = maxRatio <= 1.0;
		report = string.Concat(new string[9]
		{
			string.Format("T-VM: {0} шагов; расхождений бегущего минимума на ЧИСТЫХ шагах {1}", steps, cleanMismatch),
			firstStep < 1 ? "" : string.Format(" (первое: шаг {0}, частица {1})", firstStep, firstParticle),
			string.Format(", на шагах опасности {0}; шагов опасности {1}, счётчик [7] = {2}; ", dangerMismatch, safeSteps, lastCross),
			string.Format("min a′ = {0:E3}; худшее a′ < 0 против 64 ulp·l1EV: {1:F3}", minA, maxRatio),
			worstStep < 1 ? "" : string.Format(" (шаг {0}, частица {1})", worstStep, worstParticle),
			" ⇒ точность ",
			clean ? "ДА" : "НЕТ ⛔",
			", отрицательность ",
			roundoff ? "в пределах округления" : "ГЛУБЖЕ округления ⛔"
		});
		return clean && roundoff;
	}

	private ComputeShader LoadShader()
	{
		return solverShader != null ? solverShader : null;
	}
}
