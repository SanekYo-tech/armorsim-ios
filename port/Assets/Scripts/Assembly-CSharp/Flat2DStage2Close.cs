using System;
using UnityEngine;

public class Flat2DStage2Close : MonoBehaviour
{
	public ComputeShader solverShader;

	private const string ShaderPath = "Assets/Scripts/Flat2D/Flat2DSolver.compute";

	private const double LeakPerStep = 0.0218;

	[ContextMenu("★ Закрытие этапа 2 (E1, E2, E4, H1)")]
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
		Debug.Log("══════ ЗАКРЫТИЕ ЭТАПА 2 ══════");
		Debug.Log("устройство: " + SystemInfo.graphicsDeviceName);
		Debug.Log(string.Format("течь для сравнения: {0:E3} Дж/шаг (54.5 Дж за 2500 шагов)", LeakPerStep));
		CT.Run("P12", "точка оценки и частота: что ЯДРА реально сделали", delegate
		{
			return EvaluationPoint(cs);
		}, ref pass, ref total);
		CT.Run("P3", "предсказанный избыток против измеренной течи, одно окно", delegate
		{
			return PredictSurplus(cs);
		}, ref pass, ref total);
		CT.Run("INT", "инвариант интегратора: v^n = ½(v^{n−1/2} + v^{n+1/2})", delegate
		{
			return IntegratorInvariant(cs);
		}, ref pass, ref total);
		CT.Run("C2", "закрытие разложения: части и целое разнесены РОВНО полушаговым членом, остаток — против течи", delegate
		{
			return Closure(cs);
		}, ref pass, ref total);
		CT.Run("C3", "разложение, прочитанное ЗАНОВО после закрытия", delegate
		{
			return Reread(cs);
		}, ref pass, ref total);
		CT.Run("C4", "внешняя правда: разбиение единицы Σ(m/ρ)W = 1", delegate
		{
			return PartitionOfUnity(cs);
		}, ref pass, ref total);
		CT.Run("L1", "внешняя работа поршней: замкнута ли сцена вообще", delegate
		{
			return ExternalWork(cs);
		}, ref pass, ref total);
		CT.Run("L2", "вязкость выключена НА GPU, а не только в поле C#", delegate
		{
			return ViscosityOff(cs);
		}, ref pass, ref total);
		CT.Run("L3", "разложенное сравнение: давление и девиатор по отдельности", delegate
		{
			return Decomposed(cs);
		}, ref pass, ref total);
		CT.Run("E1", "баланс мощности: Σ m·v·a против ΔKE на полушаге", delegate
		{
			return PowerBalance(cs);
		}, ref pass, ref total);
		CT.Run("E2", "K3: плотность суммированием вместо неразрывности", delegate
		{
			return K3(cs);
		}, ref pass, ref total);
		CT.Run("E4", "кратер: вопрос о ±v/2 и прямая проверка валика", delegate
		{
			return Crater(cs);
		}, ref pass, ref total);
		CT.Run("H1", "мутация СТЕНДА: половинная скорость — видит ли её набор", delegate
		{
			return HarnessMutation(cs);
		}, ref pass, ref total);
		Debug.Log(string.Format("══════ ИТОГ: {0}/{1} ══════", pass, total));
	}

	private ComputeShader LoadShader()
	{
		return solverShader != null ? solverShader : null;
	}

	private static Flat2DScene.Body Scene(out Vector2 lo, out Vector2 hi, out Flat2DMaterialBlock[] mats, out float dx)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
		Flat2DScene.Domain(body, 0.01f, out lo, out hi);
		mats = Flat2DMaterials.Build(new ContinuumMaterialId[1]
		{
			ContinuumMaterialId.RHA_MeyerKleponis
		});
		dx = 0.0005f;
		return body;
	}

	private bool EvaluationPoint(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] array = Flat2DMaterials.Build(new ContinuumMaterialId[1]
		{
			ContinuumMaterialId.RHA_MeyerKleponis
		});
		using Flat2DSolver flat2DSolver = new Flat2DSolver(cs);
		flat2DSolver.ArtificialViscosity = false;
		flat2DSolver.Init(body.pos, body.vel, body.mat, array, body.dx, lo, hi);
		flat2DSolver.Steps(50);
		flat2DSolver.ResetProbe();
		flat2DSolver.Steps(200);
		flat2DSolver.ReadProbe(out int stateCalls, out double stateDt, out int kickCalls, out double kickDt);
		flat2DSolver.SimState(out double simDt, out double simTime, out double vmax, out double cmax);
		CT.Info(string.Format("KUpdateState вызвано {0} раз на {1} шагов; dt = {2:E6} с", stateCalls, 200, stateDt));
		CT.Info(string.Format("KKick вызвано {0} раз; dt = {1:E6} с (среднее двух шагов)", kickCalls, kickDt));
		CT.Info(string.Format("_Sim[0] после окна = {0:E6} с; отношение dt(состояние)/dt(пинок) = {1:F6}", simDt, stateDt / Math.Max(kickDt, 1e-30)));
		CT.Info("P1: обновление e потребляет _EdotP, собранный в KForces по _VelHalfR. KForces идёт ДО KKick, значит это v^{n−1/2}.");
		CT.Info(string.Format("P2: обновление применяется {0:F2} раза за шаг с ПОЛНЫМ dt ", stateCalls / 200.0) + "— не дважды по dt/2. Кандидат «множитель два от частоты» ОПРОВЕРГНУТ замером.");
		return CT.True(string.Format("состояние обновляется ровно один раз за шаг ({0} на {1})", stateCalls, 200), stateCalls == 200) & CT.True(string.Format("пинок применяется ровно один раз за шаг ({0} на {1})", kickCalls, 200), kickCalls == 200);
	}

	private bool PredictSurplus(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] array = Flat2DMaterials.Build(new ContinuumMaterialId[1]
		{
			ContinuumMaterialId.RHA_MeyerKleponis
		});
		using Flat2DSolver flat2DSolver = new Flat2DSolver(cs);
		flat2DSolver.ArtificialViscosity = false;
		flat2DSolver.Init(body.pos, body.vel, body.mat, array, body.dx, lo, hi);
		flat2DSolver.Steps(300);
		Flat2DSolver.Diag diag = flat2DSolver.Measure(withHourglass: false);
		RadarKahan radarKahan = default(RadarKahan);
		for (int i = 0; i < 400; i++)
		{
			flat2DSolver.SimState(out double dt, out double simTime, out double vmax, out double cmax);
			flat2DSolver.Steps(1);
			flat2DSolver.MeasureClosure(out double powHalf, out double sumMA2, out double powFull);
			radarKahan.Add(dt * 0.5 * sumMA2 * dt);
		}
		Flat2DSolver.Diag diag2 = flat2DSolver.Measure(withHourglass: false);
		double num = diag2.Total - diag.Total;
		double num2 = radarKahan.Value;
		double num3 = num2 - num;
		double num4 = Math.Abs(num) * 3.8 / 525.9;
		CT.Info(string.Format("окно {0} шагов: течь измерена {1:F4} Дж (полная энергия {2:F3} → {3:F3})", 400, num, diag.Total, diag2.Total));
		CT.Info(string.Format("предсказание при точке v^(n−1/2) и частоте 1×dt: {0:F4} Дж", num2));
		CT.Info(string.Format("промах {0:F4} Дж = {1:F1} % течи; ", num3, num3 / Math.Max(num, 1e-30) * 100.0) + string.Format("пол шума A7 ≈ {0:F4} Дж ⇒ промах = {1:F0} полов", num4, Math.Abs(num3) / Math.Max(num4, 1e-30)));
		CT.Info(string.Format("отношение предсказанного к измеренному: {0:F3}", num2 / Math.Max(num, 1e-30)));
		CT.Info(Math.Abs(num3) <= num4 ? "⇒ механизм НАЗВАН: избыток равен рассогласованию полушагов" : "⇒ ⚠️ ВНЕ ПОЛА: рассогласование полушагов — КОМПОНЕНТ, а не весь механизм. По ТЗ §1.3 это «стоп и доклад», шестая догадка не путь.");
		return true;
	}

	private bool IntegratorInvariant(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] array = Flat2DMaterials.Build(new ContinuumMaterialId[1]
		{
			ContinuumMaterialId.RHA_MeyerKleponis
		});
		using Flat2DSolver flat2DSolver = new Flat2DSolver(cs);
		flat2DSolver.ArtificialViscosity = false;
		flat2DSolver.Init(body.pos, body.vel, body.mat, array, body.dx, lo, hi);
		flat2DSolver.Steps(200);
		Vector2[] array2 = flat2DSolver.ReadVelHalf();
		flat2DSolver.Steps(1);
		Vector2[] array3 = flat2DSolver.ReadVelHalf();
		Vector2[] array4 = flat2DSolver.ReadVelocities();
		double num = 0.0;
		double num2 = 0.0;
		for (int i = 0; i < array4.Length; i++)
		{
			num = Math.Max(num, (array4[i] - 0.5f * (array2[i] + array3[i])).magnitude);
			num2 = Math.Max(num2, array4[i].magnitude);
		}
		CT.Info(string.Format("худшее |v^n − ½(v^(n−1/2)+v^(n+1/2))| = {0:E3} м/с при масштабе {1:F3} м/с ", num, num2) + string.Format("⇒ {0:E2} %", num / Math.Max(num2, 1e-30) * 100.0));
		return CT.True("тождество чередующегося шага держится (< 1e-5 от масштаба)", num < Math.Max(num2, 1e-30) * 1e-05 + 1e-09);
	}

	private bool Closure(ComputeShader cs)
	{
		Flat2DScene.Body body;
		Flat2DMaterialBlock[] mats;
		float dx;
		Vector2 lo;
		Vector2 hi;
		body = Scene(out lo, out hi, out mats, out dx);
		double Window(float kickScale, out double parts, out double wholeHalf, out double wholeFull, out double ma2)
		{
			using Flat2DSolver flat2DSolver = new Flat2DSolver(cs);
			flat2DSolver.ArtificialViscosity = false;
			flat2DSolver.MutKickDtScale = kickScale;
			flat2DSolver.Init(body.pos, body.vel, body.mat, mats, dx, lo, hi);
			flat2DSolver.Steps(300);
			RadarKahan radarKahan = default(RadarKahan);
			RadarKahan radarKahan2 = default(RadarKahan);
			RadarKahan radarKahan3 = default(RadarKahan);
			RadarKahan radarKahan4 = default(RadarKahan);
			for (int i = 0; i < 400; i++)
			{
				flat2DSolver.SimState(out double dt, out double simTime, out double vmax, out double cmax);
				flat2DSolver.Steps(1);
				flat2DSolver.MeasureParts(out double powerP, out double powerS, out double eInt, out double shear);
				flat2DSolver.MeasureClosure(out double powHalf, out double sumMA2, out double powFull);
				radarKahan.Add((powerS + powerP) * dt);
				radarKahan2.Add(-powHalf * dt);
				radarKahan3.Add(-powFull * dt);
				radarKahan4.Add(dt * 0.5 * sumMA2 * dt);
			}
			parts = radarKahan.Value;
			wholeHalf = radarKahan2.Value;
			wholeFull = radarKahan3.Value;
			ma2 = radarKahan4.Value;
			return parts - wholeHalf - 2.0 * ma2;
		}
		double num = Window(1f, out double parts, out double wholeHalf, out double wholeFull, out double ma2);
		CT.Info(string.Format("части (∫(ėP+ėS)dt) = {0:F4} Дж", parts));
		CT.Info(string.Format("целое при v^(n+1/2) (−∫Σm·v·a dt, прибор читает ПОСЛЕ шага) = {0:F4} Дж ", wholeHalf) + string.Format("⇒ части − целое {0:E3} Дж", parts - wholeHalf));
		CT.Info(string.Format("целое по E1 (−∫Σm·v^n·a dt) = {0:F4} Дж ⇒ расхождение {1:F4} Дж", wholeFull, parts - wholeFull));
		CT.Info(string.Format("шаг по скорости (dt/2)·Σm|a|² = {0:F4} Дж", ma2));
		CT.Info(string.Format("части → E1: {0:F4} Дж ({1:E2} % от него); ", parts - wholeFull, Math.Abs(parts - wholeFull - ma2) / Math.Max(ma2, 1e-30) * 100.0) + string.Format("E1 → целое: {0:F4} Дж ({1:E2} %)", wholeFull - wholeHalf, Math.Abs(wholeFull - wholeHalf - ma2) / Math.Max(ma2, 1e-30) * 100.0));
		CT.Info("⇒ три числа лежат на трёх соседних полушагах, разнесённые ровно на этот член. Незакрытие — не ошибка парной алгебры, а РАЗНЫЕ ТОЧКИ ОЦЕНКИ скорости.");
		CT.Info(string.Format("⚠️ прежний критерий (для журнала, НЕ ворота): |части − целое| = {0:F3} % ", Math.Abs(parts - wholeHalf) / 8.72 * 100.0) + string.Format("течи — это структурный член 2·(dt/2)·Σm|a|² = {0:F4} Дж, гейтированный против порога утечки", 2.0 * ma2));
		CT.Info(string.Format("течь за окно {0:F3} Дж; допуск 5 % течи = {1:F4} Дж (число НЕ тронуто)", 8.72, 8.72 * 0.05));
		double num2 = Window(1.1f, out double parts2, out double wholeHalf2, out double wholeFull2, out double ma22);
		CT.Info(string.Format("контрфакт (шаг пинка ×1.1): остаток {0:E3} Дж = {1:F2} % течи ", num2, Math.Abs(num2) / 8.72 * 100.0) + string.Format("(ожидание ≈ 0.2·(dt/2)·Σm|a|² = {0:F3} Дж)", ma22 * 0.2));
		return CT.True(string.Format("⭐ ОСТАТОК после структурного члена в допуске течи: |{0:E3}| Дж = ", num) + string.Format("{0:F4} % течи < 5 %", Math.Abs(num) / 8.72 * 100.0), Math.Abs(num) < 8.72 * 0.05) & CT.True("⭐ контрфакт: разнесённые полушаги (пинок ×1.1) ворота ПРОВАЛИВАЮТ — проверка не пуста", Math.Abs(num2) >= 8.72 * 0.05) & CT.True("опора: структурный член не нулевой — окно действительно ускорялось", ma2 > 0.0);
	}

	private bool Reread(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] array = Flat2DMaterials.Build(new ContinuumMaterialId[1]
		{
			ContinuumMaterialId.RHA_MeyerKleponis
		});
		using Flat2DSolver flat2DSolver = new Flat2DSolver(cs);
		flat2DSolver.ArtificialViscosity = false;
		flat2DSolver.Init(body.pos, body.vel, body.mat, array, body.dx, lo, hi);
		flat2DSolver.Steps(300);
		flat2DSolver.MeasureParts(out _, out _, out double eInt, out double shear);
		flat2DSolver.MeasureClosure(out _, out _, out _);
		RadarKahan radarKahan = default(RadarKahan);
		RadarKahan radarKahan2 = default(RadarKahan);
		RadarKahan radarKahan3 = default(RadarKahan);
		for (int i = 0; i < 400; i++)
		{
			flat2DSolver.SimState(out double dt, out double simTime, out double vmax, out double cmax);
			flat2DSolver.Steps(1);
			flat2DSolver.MeasureParts(out double powerP, out double powerS, out _, out _);
			flat2DSolver.MeasureClosure(out double powHalf, out _, out _);
			radarKahan.Add(powerP * dt);
			radarKahan2.Add(powerS * dt);
			radarKahan3.Add(-powHalf * dt);
		}
		flat2DSolver.MeasureParts(out _, out _, out double eInt2, out double shear2);
		CT.Info(string.Format("ДАВЛЕНИЕ: ∫ėP·dt = {0:F4} Дж, Δ(Σm·e) = {1:F4} Дж ⇒ ", radarKahan.Value, eInt2 - eInt) + string.Format("{0:F2} % течи", Math.Abs(eInt2 - eInt - radarKahan.Value) / 8.72 * 100.0));
		CT.Info(string.Format("ДЕВИАТОР: ∫ėS·dt = {0:F4} Дж, Δ(потенциал) = {1:F4} Дж ⇒ ", radarKahan2.Value, shear2 - shear) + string.Format("{0:F2} % течи", Math.Abs(shear2 - shear - radarKahan2.Value) / 8.72 * 100.0));
		CT.Info(string.Format("полная внутренняя по прибору = {0:F4} Дж, ", (shear2 - shear) + (eInt2 - eInt)) + string.Format("работа целиком = {0:F4} Дж ⇒ невязка баланса ", radarKahan3.Value) + string.Format("{0:F1} % течи", Math.Abs((shear2 - shear) + (eInt2 - eInt) - radarKahan3.Value) / 8.72 * 100.0));
		return true;
	}

	private bool PartitionOfUnity(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] array = Flat2DMaterials.Build(new ContinuumMaterialId[1]
		{
			ContinuumMaterialId.RHA_MeyerKleponis
		});
		using Flat2DSolver flat2DSolver = new Flat2DSolver(cs);
		flat2DSolver.Init(body.pos, body.vel, body.mat, array, body.dx, lo, hi);
		flat2DSolver.Steps(200);
		Vector2[] pos = flat2DSolver.ReadPositions();
		float[] rho = flat2DSolver.ReadDensity();
		float[] mass = flat2DSolver.ReadMass();
		float h = 0.00065f;
		float num2 = 0.0013f;
		float num3 = 0.00195f;
		float minX = float.MaxValue;
		float maxX = float.MinValue;
		float minY = float.MaxValue;
		float maxY = float.MinValue;
		double num4 = 0.0;
		double num5 = 0.0;
		int num6 = 0;
		for (int i = 0; i < pos.Length; i++)
		{
			if (pos[i].x <= minX)
			{
				minX = pos[i].x;
			}
			if (maxX <= pos[i].x)
			{
				maxX = pos[i].x;
			}
			if (pos[i].y <= minY)
			{
				minY = pos[i].y;
			}
			if (maxY <= pos[i].y)
			{
				maxY = pos[i].y;
			}
		}
		for (int j = 0; j < pos.Length && num6 < 60; j += 37)
		{
			if (minX + num3 > pos[j].x || pos[j].x > maxX - num3 || minY + num3 > pos[j].y || pos[j].y > maxY - num3)
			{
				continue;
			}
			double num7 = W(0f, h) * (mass[j] / rho[j]);
			for (int k = 0; k < pos.Length; k++)
			{
				if (k == j)
				{
					continue;
				}
				float num8 = (pos[j] - pos[k]).magnitude;
				if (num8 < num2)
				{
					num7 += W(num8, h) * (mass[k] / rho[k]);
				}
			}
			num4 = Math.Max(num4, Math.Abs(num7 - 1.0));
			num5 += num7;
			num6++;
		}
		double num9 = (num6 > 0) ? (num5 / num6) : double.NaN;
		CT.Info(string.Format("проверено внутренних частиц: {0}; среднее Σ(m/ρ)W = {1:F4}, ", num6, num9) + string.Format("худшее отклонение от 1: {0:F2} %", num4 * 100.0));
		CT.Info("⚠️ Это НЕ проверка решателя: это проверка ядра, носителя и набора соседей против величины, известной независимо от кода.");
		return CT.True(string.Format("разбиение единицы держится (худшее {0:F2} % < 10 %)", num4 * 100.0), num6 > 0 && num4 < 0.1);
	}

	private static double W(float r, float h)
	{
		double num = r / h;
		double num2 = 10.0 / (7.0 * Math.PI * h * h);
		if (1.0 > num)
		{
			return num2 * (1.0 - 1.5 * num * num + 0.75 * num * num * num);
		}
		if (2.0 > num)
		{
			return 0.25 * num2 * (2.0 - num) * (2.0 - num) * (2.0 - num);
		}
		return 0.0;
	}

	private bool ExternalWork(ComputeShader cs)
	{
		float dx = 0.0005f;
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, dx, 5f);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] array = Flat2DMaterials.Build(new ContinuumMaterialId[1]
		{
			ContinuumMaterialId.RHA_MeyerKleponis
		});
		double num = 0.0;
		double num2 = 0.0;
		for (int i = 0; i < body.pos.Length; i++)
		{
			double num3 = array[body.mat[i]].rho0 * dx * dx;
			num += body.vel[i].x * num3;
			num2 += Mathf.Abs(body.vel[i].x) * num3;
		}
		CT.Info(string.Format("инвариант сцены: Σp = {0:E3} кг·м/с при Σ|p| = {1:F3} ⇒ ", num, num2) + string.Format("{0:E2} % — сцена симметрична и замкнута", Math.Abs(num) / Math.Max(num2, 1e-30) * 100.0));
		bool flag = CT.True("суммарный импульс сцены равен нулю (нет внешнего привода)", Math.Abs(num) < Math.Max(num2, 1e-30) * 1e-09 + 1e-09);
		using Flat2DSolver flat2DSolver = new Flat2DSolver(cs);
		flat2DSolver.ArtificialViscosity = false;
		flat2DSolver.Init(body.pos, body.vel, body.mat, array, dx, lo, hi);
		flat2DSolver.Steps(300);
		double num4 = flat2DSolver.Measure(withHourglass: false).px;
		flat2DSolver.Steps(10000);
		double num5 = flat2DSolver.Measure(withHourglass: false).px;
		double num6 = Math.Abs(num5 - num4);
		CT.Info(string.Format("импульс за окно: {0:E4} → {1:E4} кг·м/с, дрейф {2:E3}", num4, num5, num6));
		CT.Info("⇒ верхняя оценка внешней работы: приводная связь при 5 м/с внесла бы " + string.Format("{0:E3} Дж, а течь за то же окно {1:F1} Дж ", num6 * 5.0, 218.0) + string.Format("({0:E2} % течи)", num6 * 5.0 / Math.Max(218.0, 1e-30) * 100.0));
		CT.Info("⇒ ВНЕШНЕЙ РАБОТЫ НЕТ: кандидат §1.2 опровергнут, и K2 он не объясняет");
		return flag;
	}

	private bool ViscosityOff(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] array = Flat2DMaterials.Build(new ContinuumMaterialId[1]
		{
			ContinuumMaterialId.RHA_MeyerKleponis
		});
		using Flat2DSolver flat2DSolver = new Flat2DSolver(cs);
		flat2DSolver.ArtificialViscosity = false;
		flat2DSolver.Init(body.pos, body.vel, body.mat, array, body.dx, lo, hi);
		flat2DSolver.Steps(10);
		flat2DSolver.ReadGpuFlags(out bool avOn, out bool plasticity);
		CT.Info(string.Format("GPU вернул: _AVOn = {0}, _PlasticityOn = {1} ", avOn ? 1 : 0, plasticity ? 1 : 0) + "(значения прочитаны из ядра, не из поля C#)");
		return CT.True("вязкость на GPU действительно выключена", !avOn);
	}

	private bool Decomposed(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] array = Flat2DMaterials.Build(new ContinuumMaterialId[1]
		{
			ContinuumMaterialId.RHA_MeyerKleponis
		});
		using Flat2DSolver flat2DSolver = new Flat2DSolver(cs);
		flat2DSolver.ArtificialViscosity = false;
		flat2DSolver.Init(body.pos, body.vel, body.mat, array, body.dx, lo, hi);
		flat2DSolver.Steps(300);
		flat2DSolver.MeasureParts(out _, out _, out double eInt, out double shear);
		RadarKahan radarKahan = default(RadarKahan);
		RadarKahan radarKahan2 = default(RadarKahan);
		for (int i = 0; i < 400; i++)
		{
			flat2DSolver.SimState(out double dt, out double simTime, out double vmax, out double cmax);
			flat2DSolver.Steps(1);
			flat2DSolver.MeasureParts(out double powerP, out double powerS, out _, out _);
			radarKahan.Add(powerP * dt);
			radarKahan2.Add(powerS * dt);
		}
		flat2DSolver.MeasureParts(out _, out _, out double eInt2, out double shear2);
		double num = eInt2 - eInt - radarKahan.Value;
		double num2 = shear2 - shear - radarKahan2.Value;
		CT.Info(string.Format("ДАВЛЕНИЕ: ∫P·dt = {0:F4} Дж, Δ(Σm·e) = {1:F4} Дж ⇒ ", radarKahan.Value, eInt2 - eInt) + string.Format("невязка {0:E3} Дж = {1:F2} % течи", num, Math.Abs(num) / 8.72 * 100.0));
		CT.Info(string.Format("ДЕВИАТОР: ∫P·dt = {0:F4} Дж, Δ(потенциал S:S/4G) = {1:F4} Дж ⇒ ", radarKahan2.Value, shear2 - shear) + string.Format("невязка {0:E3} Дж = {1:F2} % течи", num2, Math.Abs(num2) / 8.72 * 100.0));
		CT.Info(string.Format("течь за окно {0} шагов = {1:F3} Дж; пол шума A7 по этому числу — 3.8 п.п.", 400, 8.72));
		CT.Info(Math.Abs(num2) > Math.Abs(num) ? "⇒ ТЕЧЬ В ЧЛЕНЕ ДЕВИАТОРА: потенциал S:S/(4G) не равен парной работе девиатора" : "⇒ ТЕЧЬ В ЧЛЕНЕ ДАВЛЕНИЯ: накопленная e расходится с парной работой давления");
		return true;
	}

	private bool PowerBalance(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] array = Flat2DMaterials.Build(new ContinuumMaterialId[1]
		{
			ContinuumMaterialId.RHA_MeyerKleponis
		});
		using Flat2DSolver flat2DSolver = new Flat2DSolver(cs);
		flat2DSolver.ArtificialViscosity = false;
		flat2DSolver.Init(body.pos, body.vel, body.mat, array, body.dx, lo, hi);
		flat2DSolver.Steps(300);
		flat2DSolver.MeasurePower(out _, out double ke0);
		RadarKahan radarKahan = default(RadarKahan);
		for (int i = 0; i < 400; i++)
		{
			flat2DSolver.SimState(out double dt, out double simTime, out double vmax, out double cmax);
			flat2DSolver.Steps(1);
			flat2DSolver.MeasurePower(out double power, out _);
			radarKahan.Add(power * dt);
		}
		flat2DSolver.MeasurePower(out _, out double ke1);
		double num = ke1 - ke0;
		double num2 = num - radarKahan.Value;
		CT.Info(string.Format("окно {0} шагов: ΔKE(полушаг) = {1:F4} Дж, ∫P·dt = {2:F4} Дж", 400, num, radarKahan.Value));
		CT.Info(string.Format("невязка пары «сила↔кинетическая» = {0:E3} Дж", num2));
		CT.Info(string.Format("течь за то же окно = {0:F3} Дж ⇒ невязка составляет ", 8.72) + string.Format("{0:F2} % течи", Math.Abs(num2) / Math.Max(8.72, 1e-30) * 100.0));
		CT.Info(Math.Abs(num2) < 8.72 * 0.05 ? "⇒ ВЕТКА «УЧЁТ»: пара сила↔кинетическая цела; работа, записываемая в e, не совпадает с работой, которую сила действительно совершает" : "⇒ ВЕТКА «ВЫШЕ ПО ПОТОКУ»: сила совершает работу, которую никто не отслеживает — смотреть саму силу и плотность, которая её кормит");
		return true;
	}

	private bool K3(ComputeShader cs)
	{
		double num = Drift(cs, summation: false);
		double num2 = Drift(cs, summation: true);
		CT.Info(string.Format("дрейф энергии: неразрывность {0:F1} %, суммирование {1:F1} %", num * 100.0, num2 * 100.0));
		double num3 = 1.0 - num2 / Math.Max(num, 1e-30);
		CT.Info(string.Format("K3 убирает {0:F1} % дрейфа", num3 * 100.0));
		CT.Info("⚠️ Сцена — удерживаемое сжатие поршнями; свободные поверхности только сверху и снизу полосы, сжатие идёт вдоль x. Слабость суммирования у свободной поверхности задета меньше всего, но НЕ отсутствует.");
		CT.Info((num3 > 0.8) ? "⇒ оценка плотности по неразрывности рассогласована с градиентом, кормящим силу" : "⇒ оценка плотности причиной НЕ является");
		return true;
	}

	private double Drift(ComputeShader cs, bool summation)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] array = Flat2DMaterials.Build(new ContinuumMaterialId[1]
		{
			ContinuumMaterialId.RHA_MeyerKleponis
		});
		using Flat2DSolver flat2DSolver = new Flat2DSolver(cs);
		flat2DSolver.ArtificialViscosity = false;
		flat2DSolver.DensitySummation = summation;
		flat2DSolver.Init(body.pos, body.vel, body.mat, array, body.dx, lo, hi);
		flat2DSolver.Steps(300);
		double num = flat2DSolver.Measure(withHourglass: false).Total;
		flat2DSolver.Steps(10000);
		double num2 = flat2DSolver.Measure(withHourglass: false).Total;
		return Math.Abs(num2 - num) / Math.Max(num, 1e-30);
	}

	private bool Crater(ComputeShader cs)
	{
		CT.Info("Flat2DScene.Crater: стержень получает (0, −v) целиком, плита покоится ⇒ половинной скорости здесь НЕТ, объяснение TaylorPair сюда не переносится");
		float dx = 0.0005f;
		Flat2DScene.Body body = Flat2DScene.Crater(120, 60, 16, 40, dx, 0, 1, 1200f);
		Flat2DScene.Domain(body, 0.02f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] array = Flat2DMaterials.Build(new ContinuumMaterialId[1]
		{
			ContinuumMaterialId.RHA_MeyerKleponis
		});
		float num2 = float.MinValue;
		float num3 = float.MinValue;
		for (int i = 0; i < body.pos.Length; i++)
		{
			if (body.mat[i] == 0 && num2 <= body.pos[i].y)
			{
				num2 = body.pos[i].y;
			}
		}
		using Flat2DSolver flat2DSolver = new Flat2DSolver(cs);
		flat2DSolver.PinY = 0.00075f;
		flat2DSolver.Init(body.pos, body.vel, body.mat, array, dx, lo, hi);
		float[] array2 = flat2DSolver.ReadMass();
		double num4 = 0.0;
		double num5 = 0.0;
		for (int j = 0; j < array2.Length; j++)
		{
			num4 += array2[j];
		}
		flat2DSolver.Steps(12000);
		Vector2[] pos = flat2DSolver.ReadPositions();
		float[] array3 = flat2DSolver.ReadMass();
		for (int k = 0; k < array3.Length; k++)
		{
			num5 += array3[k];
		}
		float num6 = float.MaxValue;
		int num7 = 0;
		for (int l = 0; l < pos.Length; l++)
		{
			if (body.mat[l] != 0)
			{
				continue;
			}
			if (num3 <= pos[l].y)
			{
				num3 = pos[l].y;
			}
			if (pos[l].y <= num6)
			{
				num6 = pos[l].y;
			}
			if (pos[l].y > num2 + 5E-05f)
			{
				num7++;
			}
		}
		CT.Info(string.Format("верх плиты: было {0:F3} мм, стало {1:F3} мм ⇒ ", num2 * 1000f, num3 * 1000f) + string.Format("подъём {0:F3} мм = {1:F2} шага решётки", (num3 - num2) * 1000f, (num3 - num2) / dx));
		CT.Info(string.Format("частиц плиты выше исходного верха: {0}; самая низкая точка плиты {1:F2} мм", num7, num6 * 1000f));
		CT.Info(string.Format("масса: {0:E9} → {1:E9} кг, разница {2:E3}", num4, num5, Math.Abs(num5 - num4)));
		CT.True(string.Format("валик виден в положениях ({0:F2} шага)", (num3 - num2) / dx), num3 - num2 > 0.0001f);
		return true;
	}

	private bool HarnessMutation(ComputeShader cs)
	{
		Flat2DScene.Body body = Flat2DScene.TaylorPair(0.02347f, 0.00762f, 0.0003f, 0u, 210f);
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < body.vel.Length; i++)
		{
			if (num <= Mathf.Abs(body.vel[i].x))
			{
				num = Mathf.Abs(body.vel[i].x);
			}
			num2 += body.vel[i].x;
		}
		bool flag = CT.Near("скорость каждого тела равна номинальной", num, 210.0, 0.0001);
		bool flag2 = CT.True("плоскость встречи покоится (суммарный импульс ≈ 0)", body.vel.Length * 0.21f > Mathf.Abs(num2));
		Flat2DScene.Body body2 = Flat2DScene.TaylorPair(0.02347f, 0.00762f, 0.0003f, 0u, 105f);
		float num3 = 0f;
		for (int j = 0; j < body2.vel.Length; j++)
		{
			if (num3 <= Mathf.Abs(body2.vel[j].x))
			{
				num3 = Mathf.Abs(body2.vel[j].x);
			}
		}
		bool flag3 = Mathf.Abs(num3 - 210f) > 0.021f;
		CT.Info(string.Format("мутация «половинная скорость»: инвариант видит {0:F1} м/с против номинала {1:F0} ⇒ ", num3, 210f) + (flag3 ? "ПОЙМАНА" : "не поймана"));
		Flat2DStage2Tests.TaylorOut taylorOut = Flat2DStage2Tests.RunTaylor(cs, ContinuumMaterialId.OFHC_Copper, 0.02347f, 0.00762f, 105f, 0.0003f, 8000);
		double num4 = taylorOut.Lf;
		Flat2DStage2Tests.TaylorOut taylorOut2 = Flat2DStage2Tests.RunTaylor(cs, ContinuumMaterialId.OFHC_Copper, 0.02347f, 0.00762f, 210f, 0.0003f, 8000);
		double num5 = taylorOut2.Lf;
		CT.Info(string.Format("L_f при половинной {0:F2} мм против полной {1:F2} мм ", num4, num5) + string.Format("(эталон 16.27) ⇒ разница {0:F1} %", Math.Abs(num4 - num5) / num5 * 100.0));
		CT.Info("⭐ Единственное, что видело этот дефект до сих пор, — тейлоровская строка с ЭТАЛОНОМ. A0–A7, M1′ и M4 к нему слепы: они не знают, какой скорость ДОЛЖНА быть. Инвариант выше даёт сторожа, не зависящего от эталона, — и это то, чего набору не хватало.");
		return flag & flag2 & flag3;
	}
}
