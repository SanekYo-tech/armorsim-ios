using System.Collections.Generic;
using UnityEngine;

public class EraRodLatticeHarness : MonoBehaviour
{
	public struct CaseResult
	{
		public float obliquityDeg;

		public bool fractured;

		public int chunkCount;

		public int fiberBreaks;

		public float omegaLattice;

		public float omegaAnalytic;

		public float omegaCoupleOnly;

		public float deviationPct;

		public float yawDeg;

		public bool yawExtrapolated;

		public float maxBendDeg;

		public float longestStraightMM;

		public float craterClearanceMM;

		public float axisOutOfPlaneMM;

		public float timeToFractureUs;

		public bool exploded;

		public float forceN;

		public float bothPlatesTimeS;

		public float appliedLinear;

		public float appliedAngular;

		public float analyticAngularImpulse;

		public int doneSteps;

		public int totalSteps;

		public float prescribedImpulse;

		public float appliedImpulseMag;

		public float deliveredLinearX;

		public float deliveredAngularY;

		public float sweepLinear;

		public float sweepAngular;

		public float inertiaKgM2;

		public float omegaFromDelivered;

		public float offAxisAngular;

		public float solverDt;

		public float peakDevStrain;

		public float fractureStrain;
	}

	public const int SweepWindows = 256;

	[Header("Стержень (эталон шага 7)")]
	public Projectile.CoreType coreType;

	public float rodLengthMM;

	public float rodDiameterMM;

	public float rodVelocityMS;

	[Header("Кассета ДЗ (PLACEHOLDER)")]
	public float plateThicknessMM;

	public float plateVelocityMS;

	public float interlayerGapMM;

	public float standoffBehindM;

	[Header("Прогон")]
	public float simulateForS;

	public int saveEvery;

	[ContextMenu("★ Прогон всех углов")]
	public void RunAll()
	{
		Debug.Log("=== СТЕНД A: решётка под нагрузкой ДЗ ===");
		Debug.Log("   θ,°  разрушен  обломков  ω_реш,рад/с  ω_анал  расх.%  из ε_f,%  прямой,мм  t_разр,мкс");
		float[] angles = new float[5] { 0f, 30f, 45f, 60f, 75f };
		for (int i = 0; i < angles.Length; i++)
		{
			RunOne(angles[i]);
		}
		Debug.Log("ω_анал — ПОЛНЫЙ момент/I: пара F·Δs·T_both МИНУС фаза одиночной передней пластины.");
		Debug.Log("«из ε_f» — девиаторная пластическая деформация волокна от порога разрыва (ε_f = 0.20).");
		Debug.Log("⚠️ Столбец «макс.изгиб» УБРАН из вердикта намеренно (шаг 15 §2): угол между хордами");
		Debug.Log("   центров колец завышает деформацию волокна вдесятеро и вердикту о разрушении не годится.");
		Debug.Log("⚠️ Демпфер отклонений от ЦМ (dRef=0.9, tRef=1e-3) гасит и вращение тоже:");
		Debug.Log("   за 312.5 мкс полёта это ~3.2%.");
	}

	[ContextMenu("Прогон θ = 60°")]
	public void Run60()
	{
		RunOne(60f);
	}

	[ContextMenu("★ ПРИЁМКА: сохранение импульса на границе подачи")]
	public void RunConservationGate()
	{
		List<string> lines = ConservationGate(coreType, rodLengthMM, rodDiameterMM, rodVelocityMS, plateThicknessMM, plateVelocityMS, interlayerGapMM, standoffBehindM);
		foreach (string line in lines)
		{
			Debug.Log(line);
		}
	}

	public static List<string> ConservationGate(Projectile.CoreType coreType, float rodLengthMM, float rodDiameterMM, float rodVelocityMS, float plateThicknessMM, float plateVelocityMS, float interlayerGapMM, float standoffBehindM)
	{
		List<string> results = new List<string>();
		results.Add("=== ПРИЁМКА: сохранение импульса на ГРАНИЦЕ подачи в решётку ===");
		results.Add(string.Format("{0,4} {1,22} {2,22} {3,22} {4,18}", "θ°", "A: потреб.→доставл.", "B: линейный", "B: угловой", "C: ω против L/I"));
		int failures = 0;
		foreach (float thetaDeg in new float[4] { 30f, 45f, 60f, 75f })
		{
			CaseResult r = RunCase(thetaDeg, coreType, rodLengthMM, rodDiameterMM, rodVelocityMS, plateThicknessMM, plateVelocityMS, interlayerGapMM, standoffBehindM, 200);
			float rodLengthM = rodLengthMM * 0.001f;
			ReactiveArmorRodSpec era = ReactiveArmor.DemoRodPlaceholder();
			era.plateThicknessM = plateThicknessMM * 0.001f;
			era.plateVelocityMS = plateVelocityMS;
			era.interlayerGapM = interlayerGapMM * 0.001f;
			era.standoffBehindM = standoffBehindM;
			TateModel.Penetrator wha = TateModel.WHA();
			ReactiveArmor.RodLoadSchedule schedule = ReactiveArmor.BuildRodLoadSchedule(era, thetaDeg * Mathf.Deg2Rad, rodLengthM, rodDiameterMM * 0.001f, wha.density, rodVelocityMS);
			ExpectedFromWindows(schedule, rodLengthM, r.solverDt, r.doneSteps, out float linear, out float angular);
			float errA = (r.prescribedImpulse > 0f) ? Mathf.Abs(r.appliedImpulseMag - r.prescribedImpulse) / r.prescribedImpulse : 0f;
			float errBLin = (Mathf.Abs(linear) > 1e-09f) ? Mathf.Abs(r.deliveredLinearX - linear) / Mathf.Abs(linear) : 0f;
			float errBAng = (Mathf.Abs(angular) > 1e-09f) ? Mathf.Abs(r.deliveredAngularY - angular) / Mathf.Abs(angular) : 0f;
			float errC = (Mathf.Abs(r.omegaFromDelivered) > 0.001f) ? Mathf.Abs(r.omegaLattice - r.omegaFromDelivered) / Mathf.Abs(r.omegaFromDelivered) : 0f;
			bool okA = errA <= 1e-05f;
			bool okB = errBLin <= 0.03f && errBAng <= 0.03f;
			bool signOk = r.deliveredAngularY < 0f && r.omegaLattice < 0f;
			if (!okA || !okB || errC > 0.1f || !signOk)
			{
				failures++;
			}
			results.Add(string.Format("{0,4:F0} {1,9:F6} {2,3}", thetaDeg, r.appliedImpulseMag / Mathf.Max(r.prescribedImpulse, 1e-09f), okA ? "ok" : "✘") + string.Format("{0,10}{1,10:F3}/{2,8:F3} {3,3}", "", r.deliveredLinearX, linear, okB ? "ok" : "✘") + string.Format("{0,11:F3}/{1,8:F3} {2,3}", r.deliveredAngularY, angular, okB ? "ok" : "✘") + string.Format("{0,12:F2}% {1,3}", errC * 100f, (errC <= 0.1f && signOk) ? "ok" : "✘"));
		}
		results.Add("");
		results.Add("эффективная длина по зазору в кратере (шаг 16), на прогонах решётки:");
		results.Add(string.Format("{0,4} {1,10} {2,10} {3,18} {4,10} {5,9}", "θ°", "зазор,мм", "L_эфф,мм", "вне плоскости,мм", "ε_дев", "из ε_f,%"));
		float prevLen = float.MaxValue;
		foreach (float thetaDeg in new float[5] { 0f, 30f, 45f, 60f, 75f })
		{
			CaseResult r = RunCase(thetaDeg, coreType, rodLengthMM, rodDiameterMM, rodVelocityMS, plateThicknessMM, plateVelocityMS, interlayerGapMM, standoffBehindM, 200);
			results.Add(string.Format("{0,4:F0} {1,10:F3} {2,10:F1} ", thetaDeg, r.craterClearanceMM, r.longestStraightMM) + string.Format("{0,18:F5} {1,10:F5} ", r.axisOutOfPlaneMM, r.peakDevStrain) + string.Format("{0,9:F2}", (r.fractureStrain > 0f) ? r.peakDevStrain / r.fractureStrain * 100f : 0f));
			if (r.longestStraightMM > prevLen + 0.001f)
			{
				results.Add(string.Format("     ✘ длина ВЫРОСЛА при большем обликвитете на θ = {0:F0}°", thetaDeg));
				failures++;
			}
			prevLen = r.longestStraightMM;
		}
		results.Add("«вне плоскости» — остаток оси поперёк плоскости изгиба. Нагрузка планарна, и это");
		results.Add("число показывает, насколько приведение оси к плоскости оправдано, а не постулирует его.");
		results.Add("");
		results.Add("A — потребовано расписанием против ушедшего в узлы (до шага 14 было ≈0.56: окна, в");
		results.Add("    которые не попало ни одного кольца, выбрасывались целиком).");
		results.Add("B — против аналитики ПО ТЕМ ЖЕ окнам: остаток — живая геометрия против покоящейся.");
		results.Add("C — превратил ли решатель полученный момент во вращение (допуск 10%, из них ~3.2%");
		results.Add("    съедает демпфер отклонений от ЦМ).");
		results.Add((failures == 0) ? "✔ ПРИЁМКА ПРОЙДЕНА: импульс через границу подачи сохраняется" : string.Format("✘ ПРИЁМКА ПРОВАЛЕНА по {0} углам", failures));
		return results;
	}

	private static void ExpectedFromWindows(ReactiveArmor.RodLoadSchedule s, float rodLengthM, float dt, int totalSteps, out float linear, out float angular)
	{
		linear = 0f;
		angular = 0f;
		if (s.forceN <= 0f)
		{
			return;
		}
		float window = s.engagementEndS * 0.00390625f;
		if (totalSteps <= 0)
		{
			return;
		}
		float t = 0f;
		for (int step = 0; step < totalSteps; step++)
		{
			t += dt;
			for (int w = 0; w < 256; w++)
			{
				float a = (float)w * window;
				float b = a + window;
				if (a > t || t > b)
				{
					continue;
				}
				float frontLo = ReactiveArmor.FrontStationAt(s, a);
				float frontHi = ReactiveArmor.FrontStationAt(s, b);
				if (rodLengthM > frontLo)
				{
					float fa = Mathf.Clamp(frontLo, 0f, rodLengthM);
					float fb = Mathf.Clamp(frontHi, 0f, rodLengthM);
					linear += s.forceN * dt;
					angular += (rodLengthM * 0.5f - (fa + fb) * 0.5f) * (s.forceN * dt);
				}
				float rearLo = ReactiveArmor.RearStationAt(s, a);
				float rearHi = ReactiveArmor.RearStationAt(s, b);
				if (rearHi > 0f && rodLengthM > rearLo)
				{
					float ra = Mathf.Clamp(rearLo, 0f, rodLengthM);
					float rb = Mathf.Clamp(rearHi, 0f, rodLengthM);
					linear -= s.forceN * dt;
					angular -= (rodLengthM * 0.5f - (ra + rb) * 0.5f) * (s.forceN * dt);
				}
			}
		}
	}

	public static CaseResult RunCase(float obliquityDeg, Projectile.CoreType coreType, float rodLengthMM, float rodDiameterMM, float rodVelocityMS, float plateThicknessMM, float plateVelocityMS, float interlayerGapMM, float standoffBehindM, int saveEvery = 200)
	{
		CaseResult r = default(CaseResult);
		float rodLengthM = rodLengthMM * 0.001f;
		float rodDiameterM = rodDiameterMM * 0.001f;
		float plateThicknessM = plateThicknessMM * 0.001f;
		float interlayerGapM = interlayerGapMM * 0.001f;
		ReactiveArmorRodSpec era = ReactiveArmor.DemoRodPlaceholder();
		era.plateThicknessM = plateThicknessM;
		era.plateVelocityMS = plateVelocityMS;
		era.interlayerGapM = interlayerGapM;
		era.standoffBehindM = standoffBehindM;
		TateModel.Penetrator wha = TateModel.WHA();
		RodEraOutcome outcome = ReactiveArmor.ApplyToRod(era, obliquityDeg * Mathf.Deg2Rad, rodLengthM, rodDiameterM, wha.density, rodVelocityMS);
		ReactiveArmor.RodLoadSchedule schedule = ReactiveArmor.BuildRodLoadSchedule(era, obliquityDeg * Mathf.Deg2Rad, rodLengthM, rodDiameterM, wha.density, rodVelocityMS);
		RodDynamicsSolver dyn = new RodDynamicsSolver(coreType, rodDiameterMM, rodLengthMM);
		dyn.ConfigureFreeFlight(rodVelocityMS, rodLengthM / rodVelocityMS);
		if (schedule.forceN > 0f)
		{
			float window = schedule.engagementEndS * 0.00390625f;
			for (int w = 0; w < 256; w++)
			{
				float tStart = (float)w * window;
				float tEnd = tStart + window;
				float frontLo = ReactiveArmor.FrontStationAt(schedule, tStart);
				float frontHi = ReactiveArmor.FrontStationAt(schedule, tEnd);
				if (rodLengthM > frontLo)
				{
					float fa = Mathf.Clamp(frontLo, 0f, rodLengthM);
					float fb = Mathf.Clamp(frontHi, 0f, rodLengthM);
					dyn.AddExternalLoad(tStart, tEnd, new Vector3(schedule.forceN, 0f, 0f), fa, fb);
				}
				float rearLo = ReactiveArmor.RearStationAt(schedule, tStart);
				float rearHi = ReactiveArmor.RearStationAt(schedule, tEnd);
				if (rearHi > 0f && rodLengthM > rearLo)
				{
					float ra = Mathf.Clamp(rearLo, 0f, rodLengthM);
					float rb = Mathf.Clamp(rearHi, 0f, rodLengthM);
					dyn.AddExternalLoad(tStart, tEnd, new Vector3(-schedule.forceN, 0f, 0f), ra, rb);
				}
			}
		}
		int guard = 0;
		while (dyn.totalSteps > dyn.doneSteps && !dyn.exploded && guard < 1000)
		{
			int doneBefore = dyn.doneSteps;
			dyn.RunBatch(Mathf.Min(dyn.totalSteps - dyn.doneSteps, 4000), saveEvery);
			if (dyn.doneSteps == doneBefore)
			{
				break;
			}
			guard++;
		}
		float omegaLattice = dyn.RigidOmegaY();
		float deviationPct = ((outcome.omegaRadS > 1e-06f) ? (Mathf.Abs(omegaLattice - outcome.omegaRadS) / outcome.omegaRadS * 100f) : 0f);
		Vector3 L = dyn.RigidAngularMomentum();
		TateModel.Penetrator p = RoundCatalog.CorePenetrator(coreType);
		TateModel.Target target = TateModel.RHA();
		float clearanceM = CraterModel.CraterClearanceM(p, target, rodDiameterM, rodVelocityMS);
		Vector3[] centers = new Vector3[dyn.RingCount];
		bool[] alive = new bool[dyn.RingCount];
		float effectiveLengthMM = 0f;
		float outOfPlaneM = 0f;
		if (dyn.SampleFrame(dyn.EventDuration, centers, alive))
		{
			float effectiveLengthM = EraRodLattice.EffectiveLengthM(centers, alive, clearanceM, rodLengthM, out outOfPlaneM);
			effectiveLengthMM = effectiveLengthM * 1000f;
		}
		r.obliquityDeg = obliquityDeg;
		r.prescribedImpulse = dyn.PrescribedImpulseMagnitude;
		r.appliedImpulseMag = dyn.AppliedImpulseMagnitude;
		r.deliveredLinearX = dyn.AppliedLinearImpulse.x;
		r.deliveredAngularY = dyn.AppliedAngularImpulse.y;
		r.sweepLinear = schedule.sweepLinearImpulse;
		r.sweepAngular = schedule.sweepAngularImpulse;
		r.inertiaKgM2 = outcome.rodInertiaKgM2;
		r.omegaFromDelivered = ((outcome.rodInertiaKgM2 > 1e-12f) ? (dyn.AppliedAngularImpulse.y / outcome.rodInertiaKgM2) : 0f);
		r.offAxisAngular = Mathf.Abs(L.x) + Mathf.Abs(L.z);
		r.solverDt = dyn.dt;
		r.peakDevStrain = dyn.MaxDeviatoricPlasticStrain();
		r.fractureStrain = dyn.FractureStrain;
		r.omegaCoupleOnly = ((outcome.rodInertiaKgM2 > 1e-12f) ? (outcome.coupleAngularImpulse / outcome.rodInertiaKgM2) : 0f);
		r.yawDeg = Mathf.Abs(omegaLattice) * (standoffBehindM / rodVelocityMS) * 57.2957802f;
		r.yawExtrapolated = r.yawDeg > 5f;
		r.fractured = dyn.chunkEvents.Count > 0 || dyn.fiberBreaks > 0;
		r.chunkCount = dyn.chunkEvents.Count;
		r.fiberBreaks = dyn.fiberBreaks;
		r.omegaAnalytic = outcome.omegaRadS;
		r.omegaLattice = omegaLattice;
		r.deviationPct = deviationPct;
		r.maxBendDeg = MeasureMaxBendDeg(dyn);
		r.craterClearanceMM = clearanceM * 1000f;
		r.axisOutOfPlaneMM = outOfPlaneM * 1000f;
		r.longestStraightMM = effectiveLengthMM;
		r.timeToFractureUs = ((dyn.chunkEvents.Count > 0) ? (dyn.chunkEvents[0].t * 1000000f) : (-1f));
		r.exploded = dyn.exploded;
		r.forceN = schedule.forceN;
		r.bothPlatesTimeS = schedule.bothPlatesTimeS;
		r.appliedLinear = dyn.AppliedLinearImpulse.magnitude;
		r.appliedAngular = dyn.AppliedAngularImpulse.magnitude;
		r.analyticAngularImpulse = outcome.angularImpulse;
		r.doneSteps = dyn.doneSteps;
		r.totalSteps = dyn.totalSteps;
		return r;
	}

	public static string FormatCase(CaseResult r)
	{
		return string.Format("  {0,5:F0}  {1,8}  {2,8}  ", r.obliquityDeg, r.fractured ? "ДА" : "нет", r.chunkCount) + string.Format("{0,11:F2}  {1,7:F2}  {2,6:F1}  ", r.omegaLattice, r.omegaAnalytic, r.deviationPct) + string.Format("{0,12:F2}  ", (r.fractureStrain > 0f) ? (r.peakDevStrain / r.fractureStrain * 100f) : 0f) + string.Format("{0,9:F1}  {1,10}", r.longestStraightMM, (r.timeToFractureUs >= 0f) ? r.timeToFractureUs.ToString("F1") : "—") + (r.exploded ? "  ⚠РАЗНОС" : "");
	}

	public static string FormatCaseDetail(CaseResult r)
	{
		return string.Format("        подано в решётку: F = {0:F3} МН, T_both = {1:F1} мкс, ", r.forceN / 1000000f, r.bothPlatesTimeS * 1000000f) + string.Format("линейный итог {0:G4}, угловой итог {1:G4} ", r.appliedLinear, r.appliedAngular) + string.Format("(аналитика ПОЛНАЯ = {0:F4}); ψ = {1:F2}°", r.analyticAngularImpulse, r.yawDeg) + (r.yawExtrapolated ? " ⚠ЭКСТРАПОЛЯЦИЯ за диапазон Бьерке" : "") + string.Format("; изгиб (геометрия, не прочность) {0:F2}°, ε_дев {1:F5} из {2:F2}; ", r.maxBendDeg, r.peakDevStrain, r.fractureStrain) + string.Format("разрывов волокон {0}, шагов {1}/{2}", r.fiberBreaks, r.doneSteps, r.totalSteps);
	}

	public static string FormatCaseConservation(CaseResult r)
	{
		return string.Format("        граница: потребовано {0:G8}, доставлено {1:G8} ", r.prescribedImpulse, r.appliedImpulseMag) + string.Format("(расхождение {0:+0.0000;-0.0000;0}%)\n", (r.prescribedImpulse > 0f) ? ((r.appliedImpulseMag - r.prescribedImpulse) / r.prescribedImpulse * 100f) : 0f) + string.Format("        свип: линейный {0:G6} против {1:G6}, ", r.deliveredLinearX, r.sweepLinear) + string.Format("угловой {0:G6} против {1:G6} ", r.deliveredAngularY, r.sweepAngular) + string.Format("(вне плоскости {0:G3})", r.offAxisAngular);
	}

	private void RunOne(float obliquityDeg)
	{
		CaseResult r = RunCase(obliquityDeg, coreType, rodLengthMM, rodDiameterMM, rodVelocityMS, plateThicknessMM, plateVelocityMS, interlayerGapMM, standoffBehindM, saveEvery);
		Debug.Log(FormatCase(r));
		Debug.Log(FormatCaseDetail(r));
		Debug.Log(FormatCaseConservation(r));
	}

	private static float MeasureRigidOmega(RodDynamicsSolver dyn)
	{
		return dyn.RigidOmegaY();
	}

	private static float AxisAngle(Vector3[] c, bool[] alive)
	{
		int first = -1;
		for (int i = 0; i < c.Length; i++)
		{
			if (alive[i])
			{
				first = i;
				break;
			}
		}
		int last = -1;
		for (int num = c.Length - 1; num >= 0; num--)
		{
			if (alive[num])
			{
				last = num;
				break;
			}
		}
		if (first < 0 || last < 0 || first == last)
		{
			return 0f;
		}
		float dx = c[first].x - c[last].x;
		float dz = Mathf.Abs(c[first].z - c[last].z);
		return Mathf.Atan2(dx, (dz <= 1e-09f) ? 1e-09f : dz);
	}

	private static float MeasureMaxBendDeg(RodDynamicsSolver dyn)
	{
		Vector3[] c = new Vector3[dyn.RingCount];
		bool[] alive = new bool[dyn.RingCount];
		if (!dyn.SampleFrame(dyn.EventDuration, c, alive))
		{
			return 0f;
		}
		List<int> rings = new List<int>();
		for (int i = 0; i < c.Length; i++)
		{
			if (alive[i])
			{
				rings.Add(i);
			}
		}
		float maxBend = 0f;
		for (int j = 1; j + 1 < rings.Count; j++)
		{
			Vector3 a = c[rings[j]] - c[rings[j - 1]];
			Vector3 b = c[rings[j + 1]] - c[rings[j]];
			if (a.sqrMagnitude >= 1e-12f && b.sqrMagnitude >= 1e-12f)
			{
				float bend = Vector3.Angle(a, b);
				if (bend > maxBend)
				{
					maxBend = bend;
				}
			}
		}
		return maxBend;
	}

	private static float MeasureEffectiveLengthMM(RodDynamicsSolver dyn, Projectile.CoreType coreType, float rodDiameterM, float rodLengthM, float rodVelocityMS, out float clearanceM, out float outOfPlaneM)
	{
		TateModel.Penetrator p = RoundCatalog.CorePenetrator(coreType);
		TateModel.Target target = TateModel.RHA();
		clearanceM = CraterModel.CraterClearanceM(p, target, rodDiameterM, rodVelocityMS);
		outOfPlaneM = 0f;
		Vector3[] c = new Vector3[dyn.RingCount];
		bool[] alive = new bool[dyn.RingCount];
		if (!dyn.SampleFrame(dyn.EventDuration, c, alive))
		{
			return 0f;
		}
		float lengthM = EraRodLattice.EffectiveLengthM(c, alive, clearanceM, rodLengthM, out outOfPlaneM);
		return lengthM * 1000f;
	}

	public EraRodLatticeHarness()
	{
		rodLengthMM = 500f;
		rodDiameterMM = 25f;
		rodVelocityMS = 1600f;
		plateThicknessMM = 15f;
		plateVelocityMS = 500f;
		interlayerGapMM = 20f;
		standoffBehindM = 0.5f;
		simulateForS = 0.0004f;
		saveEvery = 200;
	}
}
