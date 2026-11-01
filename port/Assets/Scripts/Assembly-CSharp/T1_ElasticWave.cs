using UnityEngine;

public class T1_ElasticWave : MonoBehaviour
{
	[ContextMenu("T1 — скорость упругой волны")]
	public void RunOne()
	{
		int pass = 0;
		int total = 0;
		CT.Run("T1", "скорость упругой волны", Check, ref pass, ref total);
	}

	public static bool Check()
	{
		ContinuumProperties[] table = CT.BaseTable();
		CT.MakeRigidPlastic(table, ContinuumMaterialId.Steel4340);
		ContinuumScenario s = new ContinuumScenario
		{
			name = "T1",
			rodMaterial = ContinuumMaterialId.Steel4340,
			targetKind = ContinuumTargetKind.None,
			impactVelocityMps = -20f,
			hOverrideM = 0.00025f,
			durationS = 1.2e-05f,
			outputFrames = 2,
			materialOverride = table,
			enableJcDamage = false,
			enableAsbFade = false,
			erodeEpsP = 1e9f
		};
		SolverDiagnostics diag = new SolverDiagnostics();
		AxiMesh mesh = AxiMesh.BuildBlock(s, 0.0005f, 0.05f, 2, 200, diag);
		for (int i = 0; i < mesh.nodeCount; i++)
		{
			mesh.nodeOnAxis[i] = 1;
		}
		int fixedCount = 0;
		for (int i = 0; i < mesh.nodeCount; i++)
		{
			if (mesh.nodeZ[i] < 0.000125f)
			{
				fixedCount++;
				mesh.nodeFixed[i] = 1;
				mesh.nodeVz[i] = 0f;
			}
		}
		int sensor = 0;
		float best = float.MaxValue;
		for (int i = 0; i < mesh.nodeCount; i++)
		{
			if (mesh.nodeR[i] <= 1e-9f)
			{
				float dz = Mathf.Abs(mesh.nodeZ[i] - 0.045f);
				if (dz < best)
				{
					best = dz;
					sensor = i;
				}
			}
		}
		float sensorZ = mesh.nodeZ[sensor];
		ContinuumBackend.Run run = new ContinuumBackend.Run(s, mesh);
		string line = string.Format("брусок {0:F1} × {1:F1} мм, элементов {2}, ", 50f, 0.5f, mesh.elemCount);
		line += string.Format("закреплённых узлов {0}, датчик на z = {1:F1} мм", fixedCount, sensorZ * 1000f);
		Debug.Log("    " + line);
		float arrivalTime = -1f;
		while (!run.Finished)
		{
			run.Step();
			if (arrivalTime < 0f && Mathf.Abs(mesh.nodeVz[sensor] + 20f) > 10f)
			{
				arrivalTime = run.time;
			}
		}
		run.Finish();
		ContinuumProperties props = ContinuumMaterials.Get(ContinuumMaterialId.Steel4340);
		float waveSpeed = props.LongitudinalWaveSpeed;
		bool speedOk;
		if (arrivalTime > 0f)
		{
			Debug.Log("    " + string.Format("фронт пришёл за {0:F3} мкс, шагов {1}", arrivalTime * 1000000f, run.StepIndex));
			speedOk = CT.Near("скорость продольной волны, м/с", sensorZ / arrivalTime, waveSpeed, 0.02);
			string note = "справочно: ТЗ называет ≈ 5860 м/с, формула по таблице §3.5 даёт " + string.Format("{0:F0} м/с (расхождение {1:F2} %)", waveSpeed, Mathf.Abs(waveSpeed - 5860f) / 5860f * 100f);
			Debug.Log("    " + note);
		}
		else
		{
			Debug.LogError("    фронт до датчика не дошёл — измерять нечего");
			speedOk = false;
		}
		bool nanOk = CT.True("NaN не появилось", !CT.AnyNaN(mesh));
		bool erodedOk = CT.True("элементы не эродировали (задача упругая)", run.diag.elementsEroded == 0);
		run.ReleaseWorkingSet();
		return speedOk && nanOk && erodedOk;
	}
}
