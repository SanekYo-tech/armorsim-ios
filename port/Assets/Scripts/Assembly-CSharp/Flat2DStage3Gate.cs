using System;
using UnityEngine;

public class Flat2DStage3Gate : MonoBehaviour
{
	private struct DriftOut
	{
		public double drift;

		public double[] energy;
	}

	public ComputeShader solverShader;

	private const string ShaderPath = "Assets/Scripts/Flat2D/Flat2DSolver.compute";

	private const double FourOverPi = 4.0 / Math.PI;

	[ContextMenu("★ Ворота этапа 3 (§1 4/π, §2 бисекция A3′)")]
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
		Debug.Log("══════ ВОРОТА ЭТАПА 3 ══════");
		Debug.Log(string.Format("устройство: {0} ({1})", SystemInfo.deviceName, SystemInfo.graphicsDeviceType));
		Debug.Log(string.Format("4/π = {0:F5}; предсказание §1 записано ДО прогона: отношение в ±0.5 %", FourOverPi));
		CT.Run("§1", "третий и четвёртый тейлоровские случаи против 4/π", () => FourOverPiTest(cs), ref pass, ref total);
		CT.Run("§2", "бисекция A3′: нокауты Γ₀=0 и G=0 плюс форма роста", () => Bisect(cs), ref pass, ref total);
		Debug.Log(string.Format("══════ ИТОГ ВОРОТ: {0}/{1} ══════", pass, total));
	}

	private ComputeShader LoadShader()
	{
		return solverShader != null ? solverShader : null;
	}

	private bool FourOverPiTest(ComputeShader cs)
	{
		bool a = OneCase(cs, "медь, 23.47×7.62 @210 (эталон из №13, ПЕРЕСКАЗ)", ContinuumMaterialId.OFHC_Copper, 0.02347f, 0.00762f, 210f, 16.27, 0.00015f, false);
		bool b = OneCase(cs, "4340, 25.40×7.62 @208 (эталон из №13, ПЕРЕСКАЗ)", ContinuumMaterialId.Steel4340, 0.0254f, 0.00762f, 208f, 19.3, 0.00015f, false);
		bool c = OneCase(cs, "медь, 25.40×7.60 @190 (T3b «эталонный опыт», ИЗМЕРЕНО)", ContinuumMaterialId.OFHC_Copper, 0.0254f, 0.0076f, 190f, 16.2, 0.00015f, true);
		bool d = OneCase(cs, "медь, 34.50×7.80 @162 (T3b «RAS (OFHC M1)», ИЗМЕРЕНО)", ContinuumMaterialId.OFHC_Copper, 0.0345f, 0.0078f, 162f, 24.8, 0.00015f, true);
		return d && c && b && a;
	}

	private bool OneCase(ComputeShader cs, string title, ContinuumMaterialId id, float L0, float D0, float v, double refLf, float dx, bool isPrediction)
	{
		Flat2DStage2Tests.TaylorOut r = Flat2DStage2Tests.RunTaylor(cs, id, L0, D0, v, dx, 14000, 0.3f);
		double ratio = r.Lf / refLf;
		double predicted = r.Lf / (refLf * FourOverPi);
		CT.Info(title ?? "");
		CT.Info(string.Format("    L_f = {0:F3} мм; эталон {1:F2} мм; L0 = {2:F2}, укорочение ", r.Lf, refLf, L0 * 1000f) + string.Format("замера {0:F1} %, эталона {1:F1} %", (1.0 - r.Lf / (double)(L0 * 1000f)) * 100.0, (1.0 - refLf / (double)(L0 * 1000f)) * 100.0));
		CT.Info(string.Format("    отношение к эталону {0:F4}   (4/π = {1:F5})", ratio, FourOverPi));
		CT.Info(string.Format("    к эталону, домноженному на 4/π: {0:F4} ⇒ промах {1:+0.00;-0.00} %", predicted, (predicted - 1.0) * 100.0) + (r.tooShort ? "   ⚠️⚠️ минимум кинетической на последних шагах — прогон короток" : ""));
		if (!isPrediction)
		{
			return true;
		}
		bool ok = Math.Abs(predicted - 1.0) <= 0.005;
		return CT.True("    предсказание ±0.5 % " + (ok ? "подтверждено" : "НЕ подтверждено"), ok);
	}

	private bool Bisect(ComputeShader cs)
	{
		double baseline = Drift(cs, "эталон (как в A3′)", null).drift;
		double k1 = Drift(cs, "K1: Γ₀ = 0 (петля Ми–Грюнайзена убрана)", delegate(Flat2DMaterialBlock m)
		{
			m.gamma0 = 0f;
			return m;
		}).drift;
		double k2 = Drift(cs, "K2: G = 0 (девиатор убран, чистая гидродинамика)", delegate(Flat2DMaterialBlock m)
		{
			m.G = 0f;
			return m;
		}).drift;
		CT.Info("");
		CT.Info(string.Format("дрейф: эталон {0:F1} %, K1 {1:F1} %, K2 {2:F1} %", baseline * 100.0, k1 * 100.0, k2 * 100.0));
		double drop1 = 1.0 - k1 / Math.Max(baseline, 1E-30);
		double drop2 = 1.0 - k2 / Math.Max(baseline, 1E-30);
		CT.Info(string.Format("обрушение: K1 убирает {0:F1} % дрейфа, K2 — {1:F1} %", drop1 * 100.0, drop2 * 100.0));
		string verdict;
		if (drop1 > 0.8 && drop2 > 0.8)
		{
			verdict = "ОБА нокаута обрушают — ветки не разделены, нужен третий признак (форма роста)";
		}
		else if (drop1 > 0.8)
		{
			verdict = "ВЕТКА K1: источник — петля уравнения состояния";
		}
		else if (drop2 > 0.8)
		{
			verdict = "ВЕТКА K2: источник — работа девиатора";
		}
		else
		{
			verdict = "⚠️⚠️ НИ ОДИН НОКАУТ НЕ ОБРУШИЛ ДРЕЙФ — ни один из двух кандидатов не является причиной";
		}
		CT.Info("⇒ " + verdict);
		if (drop1 <= 0.8 && drop2 <= 0.8)
		{
			CT.Info("   по ТЗ §2 это «стоп и доклад»: смотреть плотность по неразрывности и оценку градиента скорости, формулировку не править");
		}
		return true;
	}

	private DriftOut Drift(ComputeShader cs, string title, Func<Flat2DMaterialBlock, Flat2DMaterialBlock> tweak)
	{
		Flat2DScene.Body body = Flat2DStage2Tests.Squeeze(120, 30, 0.0005f, 5f);
		Flat2DScene.Domain(body, 0.01f, out Vector2 lo, out Vector2 hi);
		Flat2DMaterialBlock[] mats = Flat2DMaterials.Build(ContinuumMaterialId.RHA_MeyerKleponis, ContinuumMaterialId.WHA);
		if (tweak != null)
		{
			for (int i = 0; i < mats.Length; i++)
			{
				mats[i] = tweak(mats[i]);
			}
		}
		DriftOut result = new DriftOut
		{
			energy = new double[5]
		};
		using (Flat2DSolver s = new Flat2DSolver(cs))
		{
			s.ArtificialViscosity = false;
			s.Init(body.pos, body.vel, body.mat, mats, 0.0005f, lo, hi);
			s.Steps(300);
			result.energy[0] = s.Measure(false).Total;
			for (int i = 1; i < 5; i++)
			{
				s.Steps(2500);
				result.energy[i] = s.Measure(false).Total;
			}
			result.drift = Math.Abs(result.energy[4] - result.energy[0]) / Math.Max(result.energy[0], 1E-30);
		}
		CT.Info("── " + title + " ──");
		CT.Info("    энергия по отсчётам (0, 2500, 5000, 7500, 10000 шагов): " + string.Join(" → ", Array.ConvertAll(result.energy, (double e) => e.ToString("F2"))));
		double[] differences = new double[4];
		double[] ratios = new double[4];
		for (int i = 0; i < 4; i++)
		{
			differences[i] = result.energy[i + 1] - result.energy[i];
			ratios[i] = result.energy[i + 1] / Math.Max(result.energy[i], 1E-30);
		}
		CT.Info("    разности: " + string.Join(" ", Array.ConvertAll(differences, (double x) => x.ToString("F1"))) + string.Format("   (разброс {0:F0} %)", Spread(differences) * 100.0));
		CT.Info("    отношения: " + string.Join(" ", Array.ConvertAll(ratios, (double x) => x.ToString("F3"))) + string.Format("   (разброс {0:F0} %)", Spread(ratios) * 100.0));
		CT.Info("    ⇒ рост " + (Spread(ratios) <= Spread(differences) ? "ЛИНЕЙНЫЙ (неучтённая работа)" : "ЭКСПОНЕНЦИАЛЬНЫЙ (петля с усилением)"));
		return result;
	}

	private static double Spread(double[] a)
	{
		double min = double.MaxValue;
		double max = double.MinValue;
		double sum = 0.0;
		for (int i = 0; i < a.Length; i++)
		{
			min = Math.Min(min, a[i]);
			max = Math.Max(max, a[i]);
			sum += a[i];
		}
		double mean = Math.Abs(sum / (double)a.Length);
		return mean > 1E-30 ? (max - min) / mean : 0.0;
	}
}
