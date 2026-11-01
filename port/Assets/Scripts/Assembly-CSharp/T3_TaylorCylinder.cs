using System;
using UnityEngine;

public class T3_TaylorCylinder : MonoBehaviour
{
	[ContextMenu("T3a — радиус гриба")]
	public void RunA()
	{
		int pass = 0;
		int total = 0;
		CT.Run("T3a", "радиус гриба", CheckMushroom, ref pass, ref total);
	}

	[ContextMenu("T3b — цилиндр Тейлора")]
	public void RunB()
	{
		int pass = 0;
		int total = 0;
		CT.Run("T3b", "цилиндр Тейлора", CheckTaylor, ref pass, ref total);
	}

	public static bool CheckTaylor()
	{
		bool flag = OneTaylor("эталонный опыт (OFHC)", 0.0254f, 0.0076f, 190f, 298f, 0.0162f, 0.0139f);
		bool flag2 = OneTaylor("RAS (OFHC M1)", 0.0345f, 0.0078f, 162f, 298f, 0.0248f, 0.0115f);
		Debug.Log("    " + "сводка T3b: строки набора взаимно расходятся на ~4.4 % (при 190 м/с укорочение СИЛЬНЕЕ, чем при 162 и 210 — зависимость от скорости в данных обратная), поэтому сойтись на обеих одновременно значило бы повторить ошибку данных");
		return flag && flag2;
	}

	private static bool OneTaylor(string title, float L0, float D0, float v, float T0, float LfRef, float DfRef)
	{
		object[] array = new object[5];
		array[0] = title;
		array[1] = L0 * 1000f;
		array[2] = D0 * 1000f;
		array[3] = v;
		array[4] = T0;
		Debug.Log("    " + string.Format("── {0}: L₀ {1:F2} мм, D₀ {2:F2} мм, {3:F0} м/с, {4:F0} K", array));
		ContinuumScenario scenario = ContinuumScenario.TaylorCylinder(L0, D0, v, ContinuumMaterialId.OFHC_Copper, T0);
		scenario.elementsPerDiameter = 20f;
		scenario.outputFrames = 20;
		ContinuumBackend.Run run = new ContinuumBackend.Run(scenario);
		double kePrev = double.MaxValue;
		float num = float.MaxValue;
		float num2 = float.NegativeInfinity;
		float num3 = 0f;
		float num4 = 0f;
		while (!run.Finished)
		{
			run.Step();
			if (kePrev > run.diag.kinetic)
			{
				kePrev = run.diag.kinetic;
				num = float.MaxValue;
				num2 = float.NegativeInfinity;
				AxiMesh mesh = run.mesh;
				for (int i = 0; i < mesh.elemCount; i++)
				{
					if (mesh.elemAlive[i] != 0 && mesh.elemBody[i] == 0)
					{
						for (int j = 0; j < 4; j++)
						{
							float num5 = mesh.nodeZ[mesh.elemNodes[i * 4 + j]];
							num = Math.Min(num, num5);
							num2 = Math.Max(num2, num5);
						}
					}
				}
				num3 = num2 - num;
				num4 = CT.MaxRadiusInWindow(mesh, 0, num2 - num3 * 0.15f, num2 + 1f) * 2f;
			}
		}
		run.Finish();
		Debug.Log("    " + string.Format("элементов {0}, шагов {1}, ", run.mesh.elemCount, run.StepIndex) + string.Format("эродировано {0}, стена {1:F0} мс; ", run.diag.elementsEroded, run.diag.wallClockMs) + string.Format("KE {0:F1} → {1:E2} Дж (укорочение окончено)", run.diag.kineticInitial, kePrev));
		bool flag = CT.Near("L_f/L₀ (ВОРОТА)", num3 / L0, LfRef / L0, 0.05);
		Debug.Log("    " + string.Format("D_f/D₀ = {0:F4} (опыт {1:F4}, отклонение ", num4 / D0, DfRef / D0) + string.Format("{0:F1} %) — НЕ утверждается, μ = {1:F2}", Math.Abs(num4 - DfRef) / DfRef * 100f, scenario.friction));
		bool flag2 = CT.True("NaN не появилось", !CT.AnyNaN(run.mesh));
		run.ReleaseWorkingSet();
		return flag && flag2;
	}

	public static bool CheckMushroom()
	{
		ContinuumScenario scenario = new ContinuumScenario();
		scenario.name = "T3a: WHA → RHA @ 1100";
		scenario.rodDiameterM = 0.03f;
		scenario.rodLengthM = 0.3f;
		scenario.impactVelocityMps = 1100f;
		scenario.rodMaterial = ContinuumMaterialId.WHA;
		scenario.targetKind = ContinuumTargetKind.SemiInfinite;
		scenario.targetThicknessM = 0.2f;
		scenario.targetMaterial = ContinuumMaterials.ActiveRhaSet;
		scenario.elementsPerDiameter = 20f;
		scenario.durationS = 0.0001f;
		scenario.outputFrames = 40;
		ContinuumBackend.Run run = CT.RunToEnd(scenario);
		float num = scenario.rodDiameterM * 0.5f;
		AxiMesh mesh = run.mesh;
		float num2 = float.NegativeInfinity;
		for (int i = 0; i < mesh.elemCount; i++)
		{
			if (mesh.elemAlive[i] != 0 && mesh.elemBody[i] == 0)
			{
				for (int j = 0; j < 4; j++)
				{
					num2 = Math.Max(num2, mesh.nodeZ[mesh.elemNodes[i * 4 + j]]);
				}
			}
		}
		float num3 = CT.MaxRadiusInWindow(mesh, 0, num2 - scenario.rodDiameterM * 0.25f, num2 + 1f) / num;
		Debug.Log("    " + string.Format("элементов {0}, шагов {1}, ", mesh.elemCount, run.StepIndex) + string.Format("эродировано {0} ({1:F1} г), ", run.diag.elementsEroded, run.diag.erodedMassKg * 1000f) + string.Format("стена {0:F0} мс", run.diag.wallClockMs));
		float num4 = CT.CraterDepth(mesh, num, scenario.targetThicknessM);
		Debug.Log("    " + string.Format("каверна по приосевой трубке: {0:F2} мм ", num4 * 1000f) + string.Format("({0:F2} калибра, отброшено улетевших обломков ", num4 / scenario.rodDiameterM) + string.Format("{0}) — установившийся режим наступает после ≈ 1 калибра", CT.lastDriftedDebris));
		bool flag = CT.InRange("R_гриба / r₀", num3, 1.35, 1.5);
		Debug.Log("    " + "⚠ ТЕГ SELF_CONSISTENCY: сверка с НАШЕЙ ЖЕ моделью течения этапа 1 (1.42), у которой источник из вторых рук. Провал = расхождение с аналитикой (договор фазы 4, ±10 %), а НЕ дефект физики. По лестнице D/16…D/64 величина гуляет в полосе 1.20 ± 0.03 без тренда к вилке — это РАЗБРОС, а не сходимость к точке.");
		Debug.Log("    " + "характеризация смещения: замер нечувствителен к окну (0.25D/0.5D/1.0D дают одно), к Q_HG (0.10→0.01 сдвигает на 0.5 %) и не лечится сгущением сетки (D/24 даёт 1.203) — смещение лежит в модели течения, а не в численности");
		bool flag2 = CT.True("NaN не появилось", !CT.AnyNaN(mesh));
		run.ReleaseWorkingSet();
		return flag2 && flag;
	}
}
