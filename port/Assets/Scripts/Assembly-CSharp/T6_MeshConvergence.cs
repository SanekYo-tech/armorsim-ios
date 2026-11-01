using UnityEngine;

public class T6_MeshConvergence : MonoBehaviour
{
	[ContextMenu("T6 — сходимость по сетке")]
	public void RunOne()
	{
		int pass = 0;
		int total = 0;
		CT.Run("T6", "сходимость по сетке", Check, ref pass, ref total);
	}

	public static bool Check()
	{
		return Check(ContinuumNose.Flat, 10f, 20f);
	}

	public static bool Check(ContinuumNose nose, float coarsePerDiameter, float finePerDiameter)
	{
		float depthCoarse = Depth(nose, coarsePerDiameter, out int elemsCoarse, out int stepsCoarse, out double msCoarse, out float craterCoarse);
		float depthFine = Depth(nose, finePerDiameter, out int elemsFine, out int stepsFine, out double msFine, out float craterFine);
		Debug.Log("    " + string.Format("нос {0}; пара (D/{1:F0}, D/{2:F0})", nose, coarsePerDiameter, finePerDiameter));
		string line = string.Format("грубая сетка h = D/{0:F0}: {1} элементов, {2} шагов, ", coarsePerDiameter, elemsCoarse, stepsCoarse);
		line += string.Format("{0:F0} мс ⇒ проникание {1:F3} мм (по полости {2:F3} мм)", msCoarse, depthCoarse * 1000f, craterCoarse * 1000f);
		Debug.Log("    " + line);
		string line2 = string.Format("мелкая сетка h = D/{0:F0}: {1} элементов, {2} шагов, ", finePerDiameter, elemsFine, stepsFine);
		line2 += string.Format("{0:F0} мс ⇒ проникание {1:F3} мм (по полости {2:F3} мм)", msFine, depthFine * 1000f, craterFine * 1000f);
		Debug.Log("    " + line2);
		if (depthCoarse > 0f && depthFine > 0f)
		{
			double rel = Mathf.Abs(depthFine - depthCoarse) / depthFine;
			return CT.True(string.Format("расхождение глубин {0:F2} % (порог 5 %)", rel * 100.0), rel <= 0.05);
		}
		Debug.LogError("    каверна нулевая хотя бы в одном прогоне — сравнивать нечего");
		return false;
	}

	private static float Depth(ContinuumNose nose, float perDiameter, out int elems, out int steps, out double ms, out float craterByDead)
	{
		ContinuumScenario s = new ContinuumScenario
		{
			name = string.Format("T6 h = D/{0:F0}, нос {1}", perDiameter, nose),
			rodDiameterM = 0.02f,
			rodLengthM = 0.06f,
			impactVelocityMps = 1400f,
			nose = nose,
			rodMaterial = ContinuumMaterialId.WHA,
			targetKind = ContinuumTargetKind.SemiInfinite,
			targetThicknessM = 0.14f,
			elementsPerDiameter = perDiameter,
			targetMaterial = ContinuumMaterials.ActiveRhaSet,
			outputFrames = 20
		};
		CT.ApplyCflOverride(s);
		ContinuumBackend.Run run = new ContinuumBackend.Run(s);
		float tipZ = 0f;
		while (!run.Finished)
		{
			run.Step();
			tipZ = CT.TipAdvance(run.mesh, tipZ);
		}
		run.Finish();
		elems = run.mesh.elemCount;
		steps = run.StepIndex;
		ms = run.diag.wallClockMs;
		craterByDead = CT.CraterDepth(run.mesh, s.rodDiameterM * 0.375f, s.targetThicknessM);
		run.ReleaseWorkingSet();
		return tipZ;
	}
}
