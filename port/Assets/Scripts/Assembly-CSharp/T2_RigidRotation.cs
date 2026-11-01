using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class T2_RigidRotation : MonoBehaviour
{
	[ContextMenu("T2 — жёсткий поворот")]
	public void RunOne()
	{
		int pass = 0;
		int total = 0;
		CT.Run("T2", "жёсткий поворот", Check, ref pass, ref total);
	}

	public static bool Check()
	{
		bool shortRun = OneRun("короткий прогон (ворота инварианта)", 200, 6e-05f, 1e-06, 0.001, true);
		bool longRun = OneRun("длинный прогон (ворота знака)", 200000, 1e-06f, 0.0003, 0.002, false);
		return shortRun && longRun;
	}

	private static bool OneRun(string title, int steps, float omegaDt, double misesTol, double rotTol, bool misesIsGate)
	{
		Debug.Log("    " + string.Format("── {0}: N = {1}, Ω·Δt = {2:E2}, θ = {3:F4} рад ──", title, steps, omegaDt, steps * omegaDt));
		float dt = 1e-09f;
		float omega = omegaDt / dt;
		ContinuumProperties[] table = CT.BaseTable();
		CT.MakeRigidPlastic(table, ContinuumMaterialId.Steel4340);
		ContinuumScenario s = new ContinuumScenario
		{
			name = "T2",
			rodMaterial = ContinuumMaterialId.Steel4340,
			impactVelocityMps = 0f,
			targetKind = ContinuumTargetKind.None,
			hOverrideM = 0.01f,
			materialOverride = table,
			enableJcDamage = false
		};
		SolverDiagnostics diag = new SolverDiagnostics();
		AxiMesh mesh = AxiMesh.BuildBlock(s, 0.01f, 0.01f, 1, 1, diag);
		for (int i = 0; i < mesh.nodeCount; i++)
		{
			mesh.nodeZ[i] -= 0.005f;
		}
		for (int i = 0; i < mesh.nodeCount; i++)
		{
			mesh.nodeVr[i] = -omega * mesh.nodeZ[i];
			mesh.nodeVz[i] = omega * mesh.nodeR[i];
		}
		mesh.elemSrr[0] = 1e+08f;
		mesh.elemSzz[0] = -40000000f;
		mesh.elemStt[0] = -60000000f;
		mesh.elemSrz[0] = 30000000f;
		float mises0 = Mises(mesh.elemSrr[0], mesh.elemSzz[0], mesh.elemStt[0], mesh.elemSrz[0]);
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
		float maxDev = 0f;
		for (int i = 0; i < steps; i++)
		{
			job.Schedule(mesh.elemCount, 1).Complete();
			float mises = Mises(mesh.elemSrr[0], mesh.elemSzz[0], mesh.elemStt[0], mesh.elemSrz[0]);
			float dev = Mathf.Abs(mises - mises0) / mises0;
			if (dev > maxDev)
			{
				maxDev = dev;
			}
		}
		float theta = omega * dt * steps;
		float aExpected = 70000000f * Mathf.Cos(2f * theta) - 30000000f * Mathf.Sin(2f * theta);
		float bExpected = 70000000f * Mathf.Sin(2f * theta) + 30000000f * Mathf.Cos(2f * theta);
		float a = (mesh.elemSrr[0] - mesh.elemSzz[0]) * 0.5f;
		float mean = (mesh.elemSrr[0] + mesh.elemSzz[0]) * 0.5f;
		float b = mesh.elemSrz[0];
		bool sigmaEqOk = true;
		if (misesIsGate)
		{
			sigmaEqOk = CT.Near("σ_eq (наибольшее отклонение за прогон)", (1.0 + maxDev) * mises0, mises0, misesTol);
		}
		else
		{
			Debug.Log("    " + string.Format("σ_eq: наибольшее расхождение {0:F5} % — это накопление ", maxDev * 100f) + string.Format("округления float32 за {0} шагов (вклад самой схемы 0.68·θ²/N = ", steps) + string.Format("{0:E2} %), справка, не ворота", 0.68f * theta * theta / steps * 100f));
		}
		bool aOk = CT.Near("девиаторная амплитуда a = (s_rr−s_zz)/2, МПа", a / 1000000f, aExpected / 1000000f, rotTol);
		bool bOk = CT.Near("сдвиг b = s_rz, МПа", b / 1000000f, bExpected / 1000000f, rotTol);
		bool meanOk = CT.Near("среднее (s_rr+s_zz)/2, МПа", mean / 1000000f, 30.0, Math.Max(misesTol, 1e-05));
		bool sttOk = CT.Near("s_θθ не изменилось, МПа", mesh.elemStt[0] / 1000000f, -60.0, Math.Max(misesTol, 1e-05));
		bool epsOk = CT.True("пластической деформации не появилось", mesh.elemEpsP[0] == 0f);
		float bMirror = 30000000f * Mathf.Cos(2f * theta) - 70000000f * Mathf.Sin(2f * theta);
		float sep = Mathf.Abs(bExpected - bMirror) / Mathf.Abs(bExpected);
		Debug.Log("    " + string.Format("фальсифицируемость: при перевёрнутом знаке ω было бы b = {0:F3} МПа ", bMirror / 1000000f) + string.Format("вместо {0:F3} — расхождение {1:F1} % при допуске {2:F2} %", bExpected / 1000000f, sep * 100f, rotTol * 100.0));
		bool signDistinct = CT.True("сверка компонент РАЗЛИЧАЕТ знак (разделение много больше допуска)", sep > rotTol * 10.0);
		materials.Dispose();
		mesh.Dispose();
		return sigmaEqOk && aOk && bOk && meanOk && sttOk && epsOk && signDistinct;
	}

	private static float Mises(float srr, float szz, float stt, float srz)
	{
		return Mathf.Sqrt(1.5f * (srr * srr + szz * szz + stt * stt + 2f * srz * srz));
	}
}
