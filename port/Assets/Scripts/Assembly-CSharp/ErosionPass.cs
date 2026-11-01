using System;
using System.Collections.Generic;
using UnityEngine;

public static class ErosionPass
{
	public static readonly float[] PressureBinsPa = new float[7]
	{
		1E+08f, 5E+08f, 1E+09f, 2E+09f, 5E+09f, 1E+10f, 2E+10f
	};

	public static bool Apply(AxiMesh mesh, SolverDiagnostics diag, AsbDeletionGate gate, float deleteMaxPressureFraction, float timeS, List<ContinuumFragmentSeed> seeds)
	{
		bool flag = false;
		for (int i = 0; i < mesh.elemCount; i++)
		{
			if (mesh.elemAlive[i] == 0)
			{
				continue;
			}
			byte kill = mesh.elemKillFlag[i];
			mesh.elemKillFlag[i] = 0;
			if (kill == 0)
			{
				continue;
			}
			if (kill > 15 && kill <= 48)
			{
				continue;
			}
			if (gate != 0 && deleteMaxPressureFraction > 0f)
			{
				ContinuumProperties props = ContinuumMaterials.Get((ContinuumMaterialId)mesh.elemMat[i]);
				float num2 = ((gate == AsbDeletionGate.SignedPPlusQ) ? (mesh.elemQ[i] + mesh.elemP[i]) : mesh.elemP[i]);
				if (num2 >= props.K * deleteMaxPressureFraction)
				{
					diag.strengthlessHeld++;
					continue;
				}
			}
			float num = mesh.elemQ[i] + mesh.elemP[i];
			float zc = ElemCentroidZ(mesh, i);
			RecordDeletion(diag, num, kill, zc, mesh.elemBody[i]);
			diag.LogRemoval(timeS, zc, mesh.elemRc[i], mesh.elemMass[i], num, mesh.elemEpsP[i], kill, mesh.elemBody[i]);
			mesh.elemAlive[i] = 0;
			diag.erodedInternal += (double)mesh.elemMass[i] * (double)mesh.elemEnergy[i];
			diag.erodedMassKg += mesh.elemMass[i];
			if (mesh.elemBody[i] != 0)
			{
				diag.erodedMassTargetKg += mesh.elemMass[i];
			}
			else
			{
				diag.erodedMassRodKg += mesh.elemMass[i];
			}
			diag.elementsEroded++;
			mesh.elemSrr[i] = 0f;
			mesh.elemSzz[i] = 0f;
			mesh.elemStt[i] = 0f;
			mesh.elemSrz[i] = 0f;
			mesh.elemP[i] = 0f;
			mesh.elemQ[i] = 0f;
			mesh.elemEnergy[i] = 0f;
			mesh.elemForce[i * 2] = 0f;
			mesh.elemForce[i * 2 + 1] = 0f;
			flag = true;
		}
		if (flag)
		{
			int num3 = 0;
			for (int j = 0; j < mesh.elemCount; j++)
			{
				if (mesh.elemAlive[j] != 0)
				{
					num3++;
				}
			}
			diag.elementsAlive = num3;
			RetireOrphans(mesh, diag, timeS, seeds);
		}
		return flag;
	}

	private static float ElemCentroidZ(AxiMesh mesh, int e)
	{
		float num = 0f;
		for (int i = 0; i < 4; i++)
		{
			num += mesh.nodeZ[mesh.elemNodes[e * 4 + i]];
		}
		return num * 0.25f;
	}

	private static void RecordDeletion(SolverDiagnostics diag, float pressure, byte kill, float zc, byte body)
	{
		if (pressure >= 0f)
		{
			int num = 0;
			while (num < PressureBinsPa.Length && PressureBinsPa[num] <= pressure)
			{
				num++;
			}
			diag.deletionPressureBins[num]++;
			if (pressure > diag.deletionPressureMaxPa)
			{
				diag.deletionPressureMaxPa = pressure;
			}
			diag.deletionPressureSumPa += pressure;
			return;
		}
		diag.deletionsInTension++;
		diag.deletionTensionSumPa += -pressure;
		if (-pressure > diag.deletionTensionMaxPa)
		{
			diag.deletionTensionMaxPa = -pressure;
		}
		int num2 = 0;
		while (num2 < PressureBinsPa.Length && PressureBinsPa[num2] <= -pressure)
		{
			num2++;
		}
		diag.deletionTensionBins[num2]++;
		if (diag.tensionZBinM > 0f)
		{
			int num3 = (int)Math.Round((zc - diag.tensionZMinM) / diag.tensionZBinM);
			if (num3 < 0)
			{
				num3 = 0;
			}
			if (num3 >= SolverDiagnostics.TensionZBins)
			{
				num3 = SolverDiagnostics.TensionZBins - 1;
			}
			if (body == 0)
			{
				diag.tensionZRod[num3]++;
			}
			else
			{
				diag.tensionZTarget[num3]++;
			}
		}
		for (int i = 0; i < 6; i++)
		{
			if ((kill & (1 << i)) == 0)
			{
				continue;
			}
			diag.deletionsByCause[i]++;
			if (pressure > diag.deletionCauseMaxPa[i])
			{
				diag.deletionCauseMaxPa[i] = pressure;
			}
			if (diag.deletionCauseMinPa[i] > pressure)
			{
				diag.deletionCauseMinPa[i] = pressure;
			}
		}
	}

	public static void RetireOrphans(AxiMesh mesh, SolverDiagnostics diag, float timeS, List<ContinuumFragmentSeed> seeds)
	{
		for (int n = 0; n < mesh.nodeCount; n++)
		{
			if (mesh.nodeOrphan[n] != 0)
			{
				continue;
			}
			bool flag = true;
			for (int i = mesh.nodeAdjStart[n]; i < mesh.nodeAdjStart[n + 1]; i++)
			{
				if (mesh.elemAlive[mesh.nodeAdjElem[i]] != 0)
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				continue;
			}
			mesh.nodeOrphan[n] = 1;
			double num = (double)mesh.nodeMass[n] * 0.5;
			double num2 = (double)(mesh.nodeVr[n] + mesh.nodeVrPrev[n]) * 0.5;
			double num3 = (double)(mesh.nodeVz[n] + mesh.nodeVzPrev[n]) * 0.5;
			diag.debrisKinetic += (num2 * num2 + num3 * num3) * num;
			diag.debrisMomentumZ += num3 * (double)mesh.nodeMass[n];
			diag.debrisMomentumR += num2 * (double)mesh.nodeMass[n];
			diag.debrisMassKg += mesh.nodeMass[n];
			diag.debrisNodes++;
			mesh.nodeFr[n] = 0f;
			mesh.nodeFz[n] = 0f;
			float num4 = Mathf.Sqrt(mesh.nodeVz[n] * mesh.nodeVz[n] + mesh.nodeVr[n] * mesh.nodeVr[n]);
			if (num4 > diag.orphanMaxSpeed)
			{
				diag.orphanMaxSpeed = num4;
			}
			if (seeds != null)
			{
				seeds.Add(new ContinuumFragmentSeed
				{
					r = mesh.nodeR[n],
					z = mesh.nodeZ[n],
					vr = mesh.nodeVr[n],
					vz = mesh.nodeVz[n],
					massKg = mesh.nodeMass[n],
					timeS = timeS,
					body = mesh.nodeBody[n]
				});
			}
		}
		diag.orphanNodes = diag.debrisNodes;
	}

	public static int CountOrphanNodes(AxiMesh mesh)
	{
		int num = 0;
		for (int i = 0; i < mesh.nodeCount; i++)
		{
			bool flag = true;
			for (int j = mesh.nodeAdjStart[i]; j < mesh.nodeAdjStart[i + 1]; j++)
			{
				if (mesh.elemAlive[mesh.nodeAdjElem[j]] != 0)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				num++;
			}
		}
		return num;
	}
}
