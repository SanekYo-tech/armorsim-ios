using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class T2b_AxisRegularity : MonoBehaviour
{
	[ContextMenu("T2b — регулярность на оси")]
	public void RunOne()
	{
		int pass = 0;
		int total = 0;
		CT.Run("T2b", "регулярность на оси", Check, ref pass, ref total);
	}

	public static bool Check()
	{
		ContinuumProperties[] table = CT.BaseTable();
		ContinuumScenario s = new ContinuumScenario
		{
			name = "T2b",
			rodMaterial = ContinuumMaterialId.Steel4340,
			impactVelocityMps = 0f,
			targetKind = ContinuumTargetKind.None,
			hOverrideM = 0.001f,
			materialOverride = table,
			enableJcDamage = false,
			erodeEpsP = 1e+09f
		};
		SolverDiagnostics diag = new SolverDiagnostics();
		AxiMesh mesh = AxiMesh.BuildBlock(s, 0.02f, 0.004f, 20, 4, diag);
		ImposeExpansion(mesh, 10000f);
		StepElements(mesh, table, s, 2e-09f, 1);
		float srrMin = float.MaxValue;
		float srrMax = float.MinValue;
		float sttMin = float.MaxValue;
		float sttMax = float.MinValue;
		float pMin = float.MaxValue;
		float pMax = float.MinValue;
		int axisElem = -1;
		for (int i = 0; i < mesh.elemCount; i++)
		{
			srrMin = Mathf.Min(srrMin, mesh.elemSrr[i]);
			srrMax = Mathf.Max(srrMax, mesh.elemSrr[i]);
			sttMin = Mathf.Min(sttMin, mesh.elemStt[i]);
			sttMax = Mathf.Max(sttMax, mesh.elemStt[i]);
			pMin = Mathf.Min(pMin, mesh.elemP[i]);
			pMax = Mathf.Max(pMax, mesh.elemP[i]);
			if (mesh.elemRc[i] < 0.001f)
			{
				axisElem = i;
			}
		}
		float scale = Mathf.Max(Mathf.Abs(srrMax), 1f);
		Debug.Log("    " + string.Format("диск {0}×{1} элементов; приосевой элемент #{2}, r_c = {3:F4} мм, ", 20, 4, axisElem, mesh.elemRc[axisElem] * 1000f) + string.Format("порог оси = {0:E2} мм (элемент ВЫШЕ порога — идёт обычная ветка)", mesh.axisTol * 1000f));
		Debug.Log("    " + string.Format("s_rr = {0:F6}…{1:F6} МПа, ", srrMin / 1000000f, srrMax / 1000000f) + string.Format("s_θθ = {0:F6}…{1:F6} МПа, P = {2:F6}…{3:F6} МПа", sttMin / 1000000f, sttMax / 1000000f, pMin / 1000000f, pMax / 1000000f));
		bool srrOk = CT.True("s_rr одинаково во всех элементах (разброс < 1e-4)", 1e-04f > (srrMax - srrMin) / scale);
		bool sttOk = CT.True("s_θθ одинаково во всех элементах", 1e-04f > (sttMax - sttMin) / scale);
		ContinuumProperties props = ContinuumMaterials.Get(ContinuumMaterialId.Steel4340);
		bool pressureOk = CT.True("давление одинаково с точностью до представимости УС " + string.Format("(разброс {0:F1} Па, порог K·1e-6 = {1:F0} Па)", pMax - pMin, props.K * 1e-06f), props.K * 1e-06f > pMax - pMin);
		bool nanOk = CT.True("NaN не появилось", !CT.AnyNaN(mesh));
		ContinuumProperties props2 = ContinuumMaterials.Get(ContinuumMaterialId.Steel4340);
		float expected = 2f * props2.G * (10000f / 3f) * 2e-09f;
		bool analyticOk = CT.Near("s_rr против аналитики 2G·(α/3)·Δt, МПа", mesh.elemSrr[axisElem] / 1000000f, expected / 1000000f, 0.0001);
		mesh.Dispose();
		ContinuumScenario s2 = new ContinuumScenario
		{
			name = "T2b-tol",
			rodMaterial = ContinuumMaterialId.Steel4340,
			impactVelocityMps = 0f,
			targetKind = ContinuumTargetKind.None,
			hOverrideM = 0.001f,
			materialOverride = table,
			enableJcDamage = false,
			erodeEpsP = 1e+09f
		};
		SolverDiagnostics diag2 = new SolverDiagnostics();
		AxiMesh mesh2 = AxiMesh.BuildBlock(s2, 1e-10f, 0.001f, 1, 1, diag2);
		Debug.Log("    " + string.Format("вырожденный элемент: r_c = {0:E3} м при пороге {1:E3} м — ", mesh2.elemRc[0], mesh2.axisTol) + "особая ветка " + ((mesh2.axisTol > mesh2.elemRc[0]) ? "СРАБАТЫВАЕТ" : "не срабатывает"));
		bool degenerateOk = CT.True("вырожденный элемент действительно ниже порога оси", mesh2.axisTol > mesh2.elemRc[0]);
		ImposeExpansion(mesh2, 10000f);
		StepElements(mesh2, table, s2, 2e-09f, 1);
		bool degenerateNanOk = CT.True("NaN на вырожденном элементе не появилось", !CT.AnyNaN(mesh2));
		bool smoothOk = CT.Near("предел гладкий: s_rr на оси = s_rr в диске, МПа", mesh2.elemSrr[0] / 1000000f, mesh.elemSrr[axisElem] / 1000000f, 0.0001);
		mesh2.Dispose();
		ContinuumScenario s3 = new ContinuumScenario
		{
			name = "T2b-free",
			rodMaterial = ContinuumMaterialId.Steel4340,
			impactVelocityMps = 0f,
			targetKind = ContinuumTargetKind.None,
			hOverrideM = 0.001f,
			materialOverride = table,
			durationS = 2e-06f,
			outputFrames = 2,
			enableJcDamage = false,
			erodeEpsP = 1e+09f
		};
		SolverDiagnostics diag3 = new SolverDiagnostics();
		AxiMesh mesh3 = AxiMesh.BuildBlock(s3, 0.02f, 0.004f, 20, 4, diag3);
		ImposeExpansion(mesh3, 10000f);
		ContinuumBackend.Run run = new ContinuumBackend.Run(s3, mesh3);
		while (!run.Finished)
		{
			run.Step();
		}
		run.Finish();
		float maxAxis = 0f;
		float maxOff = 0f;
		for (int j = 0; j < mesh3.nodeCount; j++)
		{
			float num = Mathf.Abs(mesh3.nodeVz[j]);
			if (mesh3.nodeR[j] < 0.0005f)
			{
				if (num > maxAxis)
				{
					maxAxis = num;
				}
			}
			else if (mesh3.nodeR[j] > 0.005f)
			{
				if (num > maxOff)
				{
					maxOff = num;
				}
			}
		}
		Debug.Log("    " + string.Format("после {0} шагов свободного расширения: max|v_z| на оси {1:F4} м/с, ", run.StepIndex, maxAxis) + string.Format("вне оси {0:F4} м/с", maxOff));
		bool freeNanOk = CT.True("NaN за прогон не появилось", !CT.AnyNaN(mesh3));
		bool noJet = CT.True("ложной осевой струи нет (|v_z| на оси не превышает 1.2× от остального тела)", maxOff * 1.2f + 1e-06f >= maxAxis);
		run.ReleaseWorkingSet();
		return srrOk && sttOk && pressureOk && nanOk && analyticOk && degenerateOk && degenerateNanOk && smoothOk && freeNanOk && noJet;
	}

	private static void ImposeExpansion(AxiMesh m, float alpha)
	{
		for (int i = 0; i < m.nodeCount; i++)
		{
			m.nodeVr[i] = alpha * m.nodeR[i];
			m.nodeVz[i] = 0f;
		}
	}

	private static void StepElements(AxiMesh mesh, ContinuumProperties[] table, ContinuumScenario s, float dt, int steps)
	{
		int materialCount = Enum.GetValues(typeof(ContinuumMaterialId)).Length;
		NativeArray<ContinuumProperties> materials = new NativeArray<ContinuumProperties>(materialCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		for (int i = 0; i < materialCount; i++)
		{
			materials[i] = table[i];
		}
		ElementUpdateJob job = default(ElementUpdateJob);
		job.elemNodes = mesh.elemNodes;
		job.elemMat = mesh.elemMat;
		job.elemAlive = mesh.elemAlive;
		job.elemMass = mesh.elemMass;
		job.elemVol0 = mesh.elemVol0;
		job.nodeR = mesh.nodeR;
		job.nodeZ = mesh.nodeZ;
		job.nodeVr = mesh.nodeVr;
		job.nodeVz = mesh.nodeVz;
		job.materials = materials;
		job.elemRho = mesh.elemRho;
		job.elemP = mesh.elemP;
		job.elemQ = mesh.elemQ;
		job.elemSrr = mesh.elemSrr;
		job.elemSzz = mesh.elemSzz;
		job.elemStt = mesh.elemStt;
		job.elemSrz = mesh.elemSrz;
		job.elemEnergy = mesh.elemEnergy;
		job.elemEpsP = mesh.elemEpsP;
		job.elemEpsRate = mesh.elemEpsRate;
		job.elemTemp = mesh.elemTemp;
		job.elemDamage = mesh.elemDamage;
		job.elemVol = mesh.elemVol;
		job.elemArea = mesh.elemArea;
		job.elemRc = mesh.elemRc;
		job.elemDtCand = mesh.elemDtCand;
		job.elemHgWork = mesh.elemHgWork;
		job.elemVisWork = mesh.elemVisWork;
		job.elemKillFlag = mesh.elemKillFlag;
		job.elemForce = mesh.elemForce;
		job.dt = dt;
		job.axisTol = mesh.axisTol;
		job.hourglassQ = s.hourglassQ;
		job.viscQuad = s.viscosityQuadratic;
		job.viscLin = s.viscosityLinear;
		job.erodeEpsP = 1e+09f;
		for (int j = 0; j < steps; j++)
		{
			job.Schedule(mesh.elemCount, 8).Complete();
		}
		materials.Dispose();
	}
}
