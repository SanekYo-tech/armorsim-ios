using UnityEngine;

public class T4_HydrodynamicLimit : MonoBehaviour
{
	[ContextMenu("T4 — гидродинамический предел")]
	public void RunOne()
	{
		int pass = 0;
		int total = 0;
		CT.Run("T4", "гидродинамический предел", Check, ref pass, ref total);
	}

	public static bool Check()
	{
		ContinuumProperties[] table = CT.BaseTable();
		CT.MakeStrengthless(table, ContinuumMaterialId.WHA);
		CT.MakeStrengthless(table, ContinuumMaterials.ActiveRhaSet);
		ContinuumScenario s = new ContinuumScenario
		{
			name = "T4: гидродинамический предел",
			rodDiameterM = 0.01f,
			rodMaterial = ContinuumMaterialId.WHA,
			rodLengthM = 0.1f,
			impactVelocityMps = 3000f,
			targetKind = ContinuumTargetKind.SemiInfinite,
			targetThicknessM = 0.25f,
			targetMaterial = ContinuumMaterials.ActiveRhaSet,
			elementsPerDiameter = 8f,
			outputFrames = 40,
			materialOverride = table,
			enableJcDamage = false
		};
		float rhoRod = table[(int)ContinuumMaterialId.WHA].rho0;
		float rhoRha = table[(int)ContinuumMaterials.ActiveRhaSet].rho0;
		float etalon = Mathf.Sqrt(rhoRod / rhoRha);
		ContinuumBackend.Run run = new ContinuumBackend.Run(s);
		float nose = 0f;
		while (!run.Finished)
		{
			run.Step();
			float maxZ = float.MinValue;
			for (int i = 0; i < run.mesh.elemCount; i++)
			{
				if (run.mesh.elemAlive[i] == 0 || run.mesh.elemBody[i] != 0)
				{
					continue;
				}
				for (int k = 0; k < 4; k++)
				{
					float z = run.mesh.nodeZ[run.mesh.elemNodes[i * 4 + k]];
					if (z > maxZ)
					{
						maxZ = z;
					}
				}
			}
			if (maxZ > nose && maxZ < 1e+09f)
			{
				nose = maxZ;
			}
		}
		run.Finish();
		float craterDepth = CT.CraterDepth(run.mesh, 0.00375f, s.targetThicknessM);
		float num = nose / 0.1f;
		Debug.Log("    " + string.Format("элементов {0}, шагов {1}, ", run.mesh.elemCount, run.StepIndex) + string.Format("эродировано {0} из {1}, ", run.diag.elementsEroded, run.diag.elementsInitial) + string.Format("стена {0:F0} мс", run.diag.wallClockMs));
		Debug.Log("    " + string.Format("носок стержня ушёл на {0:F2} мм при длине стержня {1:F0} мм ", nose * 1000f, 100f) + string.Format("(замер по мёртвым элементам преграды дал бы {0:F2} мм — ", craterDepth * 1000f) + "недействителен при нулевой прочности, см. комментарий)");
		Debug.Log("    " + string.Format("эталон из таблицы: √(ρ_WHA/ρ_RHA) = √({0:F0}/{1:F0}) = {2:F4} ", rhoRod, rhoRha, etalon) + "(ТЗ называет 1.485 при ρ_t = 7850)");
		bool flag = CT.Near("P/L", num, etalon, 0.05);
		bool flag2 = CT.True("NaN не появилось", !CT.AnyNaN(run.mesh));
		run.ReleaseWorkingSet();
		return flag && flag2;
	}
}
