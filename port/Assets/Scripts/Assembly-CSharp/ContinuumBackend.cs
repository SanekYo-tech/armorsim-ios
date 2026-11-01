using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public sealed class ContinuumBackend : ISolverBackend
{
	public sealed class Run
	{
		public readonly ContinuumScenario scenario;

		public readonly AxiMesh mesh;

		public readonly SolverDiagnostics diag;

		private readonly FreeSurfaceExtractor surface;

		private readonly ContactPenalty contact;

		private readonly GeometryTimeline timeline;

		private NativeArray<ContinuumProperties> materials;

		private NativeArray<float> dtResult;

		private NativeArray<double> auditResult;

		private NativeArray<double> contactWorkResult;

		private readonly List<ContinuumFragmentSeed> fragmentSeeds;

		private float[] elemMinPSeen;

		private float[] elemMinPureP;

		private float[] elemMaxPureP;

		private readonly float hourglassQEff;

		public float time;

		private bool cancelled;

		private float nextOutputTime;

		private readonly float duration;

		private readonly Stopwatch clock;

		private float[] fieldScratch;

		private byte[] bodyScratch;

		private float crossedQuarterT;

		private float crossedThreeQuarterT;

		public float[] ElemMinPureP => elemMinPureP;

		public float[] ElemMaxPureP => elemMaxPureP;

		public int StepIndex { get; private set; }

		public bool Finished { get; private set; }

		public float Progress
		{
			get
			{
				if (duration <= 0f)
				{
					return 1f;
				}
				float num = time / duration;
				if (num < 0f)
				{
					return 0f;
				}
				if (num > 1f)
				{
					return 1f;
				}
				return num;
			}
		}

		public FreeSurfaceExtractor Surface => surface;

		public NativeArray<double> ContactStats => contact.work;

		public ContactPenalty Contact => contact;

		public GeometryTimeline Timeline => timeline;

		public Run(ContinuumScenario s)
			: this(s, null)
		{
		}

		public Run(ContinuumScenario s, AxiMesh prebuilt)
		{
			diag = new SolverDiagnostics();
			timeline = new GeometryTimeline();
			fragmentSeeds = new List<ContinuumFragmentSeed>();
			clock = new Stopwatch();
			crossedQuarterT = -1f;
			crossedThreeQuarterT = -1f;
			scenario = s;
			hourglassQEff = (float.IsNaN(ContinuumScenario.HourglassQOverride) ? s.hourglassQ : ContinuumScenario.HourglassQOverride);
			duration = s.DefaultDuration();
			diag.LogMaterialProvenance(MaterialsInUse(s));
			if (prebuilt == null)
			{
				prebuilt = AxiMesh.Build(s, diag);
			}
			mesh = prebuilt;
			surface = new FreeSurfaceExtractor(mesh);
			contact = new ContactPenalty(mesh.elemCount * 4, mesh.nodeCount);
			int count = Enum.GetValues(typeof(ContinuumMaterialId)).Length;
			materials = new NativeArray<ContinuumProperties>(count, Allocator.Persistent);
			for (int i = 0; i < count; i++)
			{
				if (s.materialOverride != null && i < s.materialOverride.Length)
				{
					materials[i] = s.materialOverride[i];
				}
				else
				{
					materials[i] = ContinuumMaterials.Get((ContinuumMaterialId)i);
				}
			}
			dtResult = new NativeArray<float>(1, Allocator.Persistent);
			auditResult = new NativeArray<double>(4, Allocator.Persistent);
			contactWorkResult = new NativeArray<double>(1, Allocator.Persistent);
			timeline.dtOutput = duration / Math.Max(1, s.outputFrames);
			float minZ = float.MaxValue;
			float maxZ = float.MinValue;
			for (int j = 0; j < mesh.nodeCount; j++)
			{
				if (mesh.nodeZ[j] < minZ)
				{
					minZ = mesh.nodeZ[j];
				}
				if (mesh.nodeZ[j] > maxZ)
				{
					maxZ = mesh.nodeZ[j];
				}
			}
			diag.tensionZMinM = minZ;
			diag.tensionZBinM = Mathf.Max(1e-06f, (maxZ - minZ) / 40f);
			elemMinPSeen = new float[mesh.elemCount];
			elemMinPureP = new float[mesh.elemCount];
			elemMaxPureP = new float[mesh.elemCount];
			if (s.targetKind == ContinuumTargetKind.Plate || s.targetKind == ContinuumTargetKind.SemiInfinite)
			{
				diag.impactPressurePa = ContinuumMaterials.PlanarImpactPressure(materials[(int)s.rodMaterial], materials[(int)s.targetMaterial], Mathf.Abs(s.impactVelocityMps));
			}
			surface.Extract(mesh, diag);
			ErosionPass.RetireOrphans(mesh, diag, 0f, fragmentSeeds);
			PrimeTimeStep();
			MeasureMeshQuality(s);
			for (int k = 0; k < mesh.nodeCount; k++)
			{
				mesh.nodeVrPrev[k] = mesh.nodeVr[k];
				mesh.nodeVzPrev[k] = mesh.nodeVz[k];
			}
			MeasureEnergy();
			diag.kineticInitial = diag.kinetic;
			diag.internalInitial = diag.internalEnergy;
			EmitFrame();
			nextOutputTime = timeline.dtOutput;
			clock.Start();
		}

		private static ContinuumMaterialId[] MaterialsInUse(ContinuumScenario s)
		{
			if ((s.targetKind == ContinuumTargetKind.Plate || s.targetKind == ContinuumTargetKind.SemiInfinite) && s.rodMaterial != s.targetMaterial)
			{
				return new ContinuumMaterialId[2] { s.rodMaterial, s.targetMaterial };
			}
			return new ContinuumMaterialId[1] { s.rodMaterial };
		}

		private void PrimeTimeStep()
		{
			for (int e = 0; e < mesh.elemCount; e++)
			{
				ContinuumProperties props = materials[mesh.elemMat[e]];
				float area = mesh.elemArea[e];
				if (area > 0f)
				{
					int n0 = mesh.elemNodes[e * 4];
					int n1 = mesh.elemNodes[e * 4 + 1];
					int n2 = mesh.elemNodes[e * 4 + 2];
					int n3 = mesh.elemNodes[e * 4 + 3];
					float diag = Vector2.Distance(new Vector2(mesh.nodeR[n0], mesh.nodeZ[n0]), new Vector2(mesh.nodeR[n2], mesh.nodeZ[n2]));
					float diag2 = Vector2.Distance(new Vector2(mesh.nodeR[n1], mesh.nodeZ[n1]), new Vector2(mesh.nodeR[n3], mesh.nodeZ[n3]));
					if (diag2 > diag)
					{
						diag = diag2;
					}
					if (diag < 1e-12f)
					{
						diag = 1e-12f;
					}
					float c = props.LongitudinalWaveSpeed;
					mesh.elemDtCand[e] = area / diag / (Mathf.Abs(mesh.nodeVz[n0]) + c);
				}
			}
		}

		private void MeasureMeshQuality(ContinuumScenario s)
		{
			float minDt = float.MaxValue;
			for (int e = 0; e < mesh.elemCount; e++)
			{
				if (mesh.elemAlive[e] == 0)
				{
					continue;
				}
				int n0 = mesh.elemNodes[e * 4];
				int n1 = mesh.elemNodes[e * 4 + 1];
				int n2 = mesh.elemNodes[e * 4 + 2];
				int n3 = mesh.elemNodes[e * 4 + 3];
				float e12 = Len(mesh.nodeR[n2] - mesh.nodeR[n1], mesh.nodeZ[n2] - mesh.nodeZ[n1]);
				float e23 = Len(mesh.nodeR[n3] - mesh.nodeR[n2], mesh.nodeZ[n3] - mesh.nodeZ[n2]);
				float e30 = Len(mesh.nodeR[n0] - mesh.nodeR[n3], mesh.nodeZ[n0] - mesh.nodeZ[n3]);
				float e01 = Len(mesh.nodeR[n1] - mesh.nodeR[n0], mesh.nodeZ[n1] - mesh.nodeZ[n0]);
				float minEdge = e01;
				if (e12 < minEdge)
				{
					minEdge = e12;
				}
				if (e23 < minEdge)
				{
					minEdge = e23;
				}
				if (e30 < minEdge)
				{
					minEdge = e30;
				}
				float maxEdge = e01;
				if (e12 > maxEdge)
				{
					maxEdge = e12;
				}
				if (e23 > maxEdge)
				{
					maxEdge = e23;
				}
				if (e30 > maxEdge)
				{
					maxEdge = e30;
				}
				if (minEdge > 1e-12f)
				{
					float aspect = maxEdge / minEdge;
					if (diag.meshAspectMin > aspect)
					{
						diag.meshAspectMin = aspect;
					}
					if (aspect > diag.meshAspectMax)
					{
						diag.meshAspectMax = aspect;
					}
				}
				float skew = Corner(mesh.nodeR[n3], mesh.nodeZ[n3], mesh.nodeR[n0], mesh.nodeZ[n0], mesh.nodeR[n1], mesh.nodeZ[n1]);
				if (skew > diag.meshSkewMaxDeg)
				{
					diag.meshSkewMaxDeg = skew;
				}
				skew = Corner(mesh.nodeR[n0], mesh.nodeZ[n0], mesh.nodeR[n1], mesh.nodeZ[n1], mesh.nodeR[n2], mesh.nodeZ[n2]);
				if (skew > diag.meshSkewMaxDeg)
				{
					diag.meshSkewMaxDeg = skew;
				}
				skew = Corner(mesh.nodeR[n1], mesh.nodeZ[n1], mesh.nodeR[n2], mesh.nodeZ[n2], mesh.nodeR[n3], mesh.nodeZ[n3]);
				if (skew > diag.meshSkewMaxDeg)
				{
					diag.meshSkewMaxDeg = skew;
				}
				skew = Corner(mesh.nodeR[n2], mesh.nodeZ[n2], mesh.nodeR[n3], mesh.nodeZ[n3], mesh.nodeR[n0], mesh.nodeZ[n0]);
				if (skew > diag.meshSkewMaxDeg)
				{
					diag.meshSkewMaxDeg = skew;
				}
				float dt = mesh.elemDtCand[e];
				if (dt > 0f && dt < minDt)
				{
					minDt = dt;
				}
			}
			if (minDt == float.MaxValue)
			{
				minDt = 0f;
			}
			diag.meshMinDtAtStartS = minDt;
			if (diag.meshAspectMin == float.MaxValue)
			{
				diag.meshAspectMin = 0f;
			}
			if (s.nose != ContinuumNose.Flat)
			{
				float h = s.hOverrideM;
				if (h <= 0f)
				{
					h = s.rodDiameterM / Mathf.Max(1f, s.elementsPerDiameter);
				}
				diag.tipFlatMinAdmissibleOverD = AxiMesh.MinTipFlatOverD(h, s.rodDiameterM);
			}
		}

		private static float Len(float dr, float dz)
		{
			return MathF.Sqrt(dr * dr + dz * dz);
		}

		private static float Corner(float ar, float az, float br, float bz, float cr, float cz)
		{
			float abr = ar - br;
			float abz = az - bz;
			float cbr = cr - br;
			float cbz = cz - bz;
			float lab = MathF.Sqrt(abr * abr + abz * abz);
			if (lab < 1e-12f)
			{
				return 0f;
			}
			float lcb = MathF.Sqrt(cbr * cbr + cbz * cbz);
			if (lcb < 1e-12f)
			{
				return 0f;
			}
			float cross = abr * cbz - abz * cbr;
			float dot = abr * cbr + abz * cbz;
			return MathF.Abs(MathF.Atan2(MathF.Abs(cross), dot) * Mathf.Rad2Deg - 90f);
		}

		public void Step()
		{
			if (Finished)
			{
				return;
			}
			ReduceTimeStepJob reduceTimeStepJob = default(ReduceTimeStepJob);
			reduceTimeStepJob.elemDtCand = mesh.elemDtCand;
			reduceTimeStepJob.elemAlive = mesh.elemAlive;
			reduceTimeStepJob.result = dtResult;
			reduceTimeStepJob.Schedule().Complete();
			float dt = dtResult[0] * scenario.cflSafety;
			if (dt <= 0f || float.IsNaN(dt) || dt >= 1e+30f)
			{
				diag.Warn("шаг по времени выродился — прогон остановлен");
				Finished = true;
				return;
			}
			if (dt < scenario.dtFloorS)
			{
				diag.dtFloorHits++;
			}
			if (dt < scenario.dtFloorS)
			{
				diag.WarnOnce("dtFloor", string.Format("Δt упал ниже пола {0:E2} с на шаге {1} — ", scenario.dtFloorS, StepIndex) + "элемент схлопывается, эрозия должна была его снять раньше");
			}
			if (time + dt > duration)
			{
				dt = duration - time;
			}
			if (dt <= 0f)
			{
				Finished = true;
				return;
			}
			diag.lastTimeStepS = dt;
			if (diag.minTimeStepS > dt)
			{
				diag.minTimeStepS = dt;
			}
			NodeStashVelocityJob nodeStashVelocityJob = default(NodeStashVelocityJob);
			nodeStashVelocityJob.nodeVr = mesh.nodeVr;
			nodeStashVelocityJob.nodeVz = mesh.nodeVz;
			nodeStashVelocityJob.nodeVrPrev = mesh.nodeVrPrev;
			nodeStashVelocityJob.nodeVzPrev = mesh.nodeVzPrev;
			nodeStashVelocityJob.Schedule(mesh.nodeCount, 256).Complete();
			NodeIntegrateVelocityJob nodeIntegrateVelocityJob = default(NodeIntegrateVelocityJob);
			nodeIntegrateVelocityJob.nodeVr = mesh.nodeVr;
			nodeIntegrateVelocityJob.nodeVz = mesh.nodeVz;
			nodeIntegrateVelocityJob.nodeFr = mesh.nodeFr;
			nodeIntegrateVelocityJob.nodeFz = mesh.nodeFz;
			nodeIntegrateVelocityJob.nodeMass = mesh.nodeMass;
			nodeIntegrateVelocityJob.nodeOnAxis = mesh.nodeOnAxis;
			nodeIntegrateVelocityJob.nodeFixed = mesh.nodeFixed;
			nodeIntegrateVelocityJob.nodeOrphan = mesh.nodeOrphan;
			nodeIntegrateVelocityJob.dt = dt;
			nodeIntegrateVelocityJob.Schedule(mesh.nodeCount, 256).Complete();
			ContactWorkJob contactWorkJob = default(ContactWorkJob);
			contactWorkJob.nodeCFr = contact.nodeCFr;
			contactWorkJob.nodeCFz = contact.nodeCFz;
			contactWorkJob.nodeVr = mesh.nodeVr;
			contactWorkJob.nodeVz = mesh.nodeVz;
			contactWorkJob.nodeVrPrev = mesh.nodeVrPrev;
			contactWorkJob.nodeVzPrev = mesh.nodeVzPrev;
			contactWorkJob.result = contactWorkResult;
			contactWorkJob.dt = dt;
			contactWorkJob.Schedule().Complete();
			diag.contactWork += contactWorkResult[0];
			NodeIntegratePositionJob nodeIntegratePositionJob = default(NodeIntegratePositionJob);
			nodeIntegratePositionJob.nodeR = mesh.nodeR;
			nodeIntegratePositionJob.nodeZ = mesh.nodeZ;
			nodeIntegratePositionJob.nodeVr = mesh.nodeVr;
			nodeIntegratePositionJob.nodeVz = mesh.nodeVz;
			nodeIntegratePositionJob.nodeOrphan = mesh.nodeOrphan;
			nodeIntegratePositionJob.dt = dt;
			nodeIntegratePositionJob.Schedule(mesh.nodeCount, 256).Complete();
			ElementUpdateJob elementUpdateJob = default(ElementUpdateJob);
			elementUpdateJob.elemNodes = mesh.elemNodes;
			elementUpdateJob.elemMat = mesh.elemMat;
			elementUpdateJob.elemAlive = mesh.elemAlive;
			elementUpdateJob.elemMass = mesh.elemMass;
			elementUpdateJob.elemVol0 = mesh.elemVol0;
			elementUpdateJob.nodeR = mesh.nodeR;
			elementUpdateJob.nodeZ = mesh.nodeZ;
			elementUpdateJob.nodeVr = mesh.nodeVr;
			elementUpdateJob.nodeVz = mesh.nodeVz;
			elementUpdateJob.materials = materials;
			elementUpdateJob.elemRho = mesh.elemRho;
			elementUpdateJob.elemP = mesh.elemP;
			elementUpdateJob.elemQ = mesh.elemQ;
			elementUpdateJob.elemSrr = mesh.elemSrr;
			elementUpdateJob.elemSzz = mesh.elemSzz;
			elementUpdateJob.elemStt = mesh.elemStt;
			elementUpdateJob.elemSrz = mesh.elemSrz;
			elementUpdateJob.elemEnergy = mesh.elemEnergy;
			elementUpdateJob.elemEpsP = mesh.elemEpsP;
			elementUpdateJob.elemEpsRate = mesh.elemEpsRate;
			elementUpdateJob.elemTemp = mesh.elemTemp;
			elementUpdateJob.elemDamage = mesh.elemDamage;
			elementUpdateJob.elemVol = mesh.elemVol;
			elementUpdateJob.elemArea = mesh.elemArea;
			elementUpdateJob.elemRc = mesh.elemRc;
			elementUpdateJob.elemDtCand = mesh.elemDtCand;
			elementUpdateJob.elemHgWork = mesh.elemHgWork;
			elementUpdateJob.elemVisWork = mesh.elemVisWork;
			elementUpdateJob.elemKillFlag = mesh.elemKillFlag;
			elementUpdateJob.elemForce = mesh.elemForce;
			elementUpdateJob.dt = dt;
			elementUpdateJob.axisTol = mesh.axisTol;
			elementUpdateJob.hourglassQ = hourglassQEff;
			elementUpdateJob.viscQuad = scenario.viscosityQuadratic;
			elementUpdateJob.viscLin = scenario.viscosityLinear;
			elementUpdateJob.erodeEpsP = scenario.erodeEpsP;
			elementUpdateJob.erodeVolFrac = scenario.erodeVolumeFraction;
			elementUpdateJob.dtFloor = scenario.dtFloorS;
			elementUpdateJob.enableDamage = (byte)(scenario.enableJcDamage ? 1 : 0);
			elementUpdateJob.enableAsb = (byte)(scenario.enableAsbFade ? 1 : 0);
			elementUpdateJob.Schedule(mesh.elemCount, 64).Complete();
			GatherNodeForcesJob gatherNodeForcesJob = default(GatherNodeForcesJob);
			gatherNodeForcesJob.nodeAdjStart = mesh.nodeAdjStart;
			gatherNodeForcesJob.nodeAdjElem = mesh.nodeAdjElem;
			gatherNodeForcesJob.nodeAdjCorner = mesh.nodeAdjCorner;
			gatherNodeForcesJob.elemForce = mesh.elemForce;
			gatherNodeForcesJob.nodeFr = mesh.nodeFr;
			gatherNodeForcesJob.nodeFz = mesh.nodeFz;
			gatherNodeForcesJob.Schedule(mesh.nodeCount, 256).Complete();
			if (scenario.targetKind == ContinuumTargetKind.None)
			{
				for (int n = 0; n < mesh.nodeCount; n++)
				{
					contact.nodeCFr[n] = 0f;
					contact.nodeCFz[n] = 0f;
				}
			}
			ContactPenaltyJob contactPenaltyJob = default(ContactPenaltyJob);
			contactPenaltyJob.nodeR = mesh.nodeR;
			contactPenaltyJob.nodeZ = mesh.nodeZ;
			contactPenaltyJob.nodeVr = mesh.nodeVr;
			contactPenaltyJob.nodeVz = mesh.nodeVz;
			contactPenaltyJob.nodeMass = mesh.nodeMass;
			contactPenaltyJob.segA = surface.segA;
			contactPenaltyJob.segB = surface.segB;
			contactPenaltyJob.surfNodes = surface.surfNodes;
			contactPenaltyJob.nodeFr = mesh.nodeFr;
			contactPenaltyJob.nodeFz = mesh.nodeFz;
			contactPenaltyJob.work = contact.work;
			contactPenaltyJob.projSegStart = surface.projSegStart;
			contactPenaltyJob.projSegEnd = surface.projSegEnd;
			contactPenaltyJob.targSegStart = surface.targSegStart;
			contactPenaltyJob.targSegEnd = surface.targSegEnd;
			contactPenaltyJob.projNodeStart = surface.projNodeStart;
			contactPenaltyJob.projNodeEnd = surface.projNodeEnd;
			contactPenaltyJob.targNodeStart = surface.targNodeStart;
			contactPenaltyJob.targNodeEnd = surface.targNodeEnd;
			contactPenaltyJob.cellStart = contact.cellStart;
			contactPenaltyJob.cellCursor = contact.cellCursor;
			contactPenaltyJob.cellItems = contact.cellItems;
			contactPenaltyJob.gridMaxSide = ContactPenalty.GridMaxSide;
			contactPenaltyJob.rampDepth = contact.rampDepth;
			contactPenaltyJob.rampOffset = contact.rampOffset;
			contactPenaltyJob.nodeCFr = contact.nodeCFr;
			contactPenaltyJob.nodeCFz = contact.nodeCFz;
			contactPenaltyJob.dt = dt;
			contactPenaltyJob.penaltyScale = scenario.contactScale;
			contactPenaltyJob.dampingRatio = scenario.contactDampingRatio;
			contactPenaltyJob.friction = scenario.friction;
			contactPenaltyJob.hChar = mesh.hCharacteristic;
			if (scenario.contactPushSpeedMps > 0f)
			{
				contactPenaltyJob.pushSpeed = scenario.contactPushSpeedMps;
			}
			else
			{
				contactPenaltyJob.pushSpeed = Mathf.Abs(scenario.impactVelocityMps) * 0.1f + 1f;
			}
			contactPenaltyJob.anvilEnabled = (byte)(scenario.targetKind == ContinuumTargetKind.RigidAnvil ? 1 : 0);
			contactPenaltyJob.anvilZ = 0f;
			contactPenaltyJob.Schedule().Complete();
			NodeApplyForceBcJob nodeApplyForceBcJob = default(NodeApplyForceBcJob);
			nodeApplyForceBcJob.nodeFr = mesh.nodeFr;
			nodeApplyForceBcJob.nodeFz = mesh.nodeFz;
			nodeApplyForceBcJob.nodeOnAxis = mesh.nodeOnAxis;
			nodeApplyForceBcJob.nodeFixed = mesh.nodeFixed;
			nodeApplyForceBcJob.Schedule(mesh.nodeCount, 256).Complete();
			bool ledgerJump = false;
			double before = 0.0;
			for (int e = 0; e < mesh.elemCount; e++)
			{
				if (mesh.elemAlive[e] != 0 && mesh.elemKillFlag[e] != 0)
				{
					ledgerJump = true;
					before = PeekLedgerTotal();
					break;
				}
			}
			bool eroded = ErosionPass.Apply(mesh, diag, scenario.asbDeletionPressureGate, scenario.deleteMaxPressureFraction, time + dt, fragmentSeeds);
			if (eroded)
			{
				surface.Extract(mesh, diag);
			}
			TrackCraterFront();
			if (ledgerJump)
			{
				diag.erosionLedgerJump += PeekLedgerTotal() - before;
			}
			TrackPressureExtremes();
			MeasureEnergy();
			StepIndex++;
			time += dt;
			diag.steps = StepIndex;
			diag.simulatedTimeS = time;
			if (time >= nextOutputTime - 1e-12f)
			{
				EmitFrame();
				nextOutputTime += timeline.dtOutput;
				if (time >= nextOutputTime)
				{
					nextOutputTime = timeline.dtOutput + time;
				}
			}
			if (time >= duration - 1e-12f)
			{
				Finished = true;
			}
			if (StepIndex >= scenario.maxSteps)
			{
				diag.Warn(string.Format("достигнут потолок шагов ({0}) при t = {1:F2} мкс ", scenario.maxSteps, time * 1000000f) + string.Format("из {0:F2} — прогон оборван", duration * 1000000f));
				Finished = true;
			}
		}

		private void TrackPressureExtremes()
		{
			for (int e = 0; e < mesh.elemCount; e++)
			{
				if (mesh.elemAlive[e] == 0)
				{
					continue;
				}
				float p = mesh.elemQ[e] + mesh.elemP[e];
				float pureP = mesh.elemP[e];
				if (p < elemMinPSeen[e])
				{
					elemMinPSeen[e] = p;
				}
				if (pureP < elemMinPureP[e])
				{
					elemMinPureP[e] = pureP;
				}
				if (pureP > elemMaxPureP[e])
				{
					elemMaxPureP[e] = pureP;
				}
				if (p < 0f)
				{
					float tension = 0f - p;
					if (tension > diag.peakTensionPa)
					{
						diag.peakTensionPa = tension;
						diag.peakTensionElem = e;
						diag.peakTensionR = mesh.elemRc[e];
						diag.peakTensionZ = ElemZ(e);
						diag.peakTensionTimeS = time;
					}
				}
				if (p > diag.peakCompressionPa)
				{
					diag.peakCompressionPa = p;
					diag.peakCompressionElem = e;
					diag.peakCompressionR = mesh.elemRc[e];
					diag.peakCompressionZ = ElemZ(e);
					diag.peakCompressionTimeS = time;
				}
				if (mesh.elemBody[e] == 0 && p > diag.peakRodCompressionPa)
				{
					diag.peakRodCompressionPa = p;
				}
			}
		}

		private float ElemZ(int e)
		{
			float z = 0f;
			for (int k = 0; k < 4; k++)
			{
				z += mesh.nodeZ[mesh.elemNodes[e * 4 + k]];
			}
			return z * 0.25f;
		}

		private double PeekLedgerTotal()
		{
			EnergyAuditJob energyAuditJob = default(EnergyAuditJob);
			energyAuditJob.nodeMass = mesh.nodeMass;
			energyAuditJob.nodeVr = mesh.nodeVr;
			energyAuditJob.nodeVz = mesh.nodeVz;
			energyAuditJob.nodeVrPrev = mesh.nodeVrPrev;
			energyAuditJob.nodeVzPrev = mesh.nodeVzPrev;
			energyAuditJob.nodeOrphan = mesh.nodeOrphan;
			energyAuditJob.elemEnergy = mesh.elemEnergy;
			energyAuditJob.elemMass = mesh.elemMass;
			energyAuditJob.elemHgWork = mesh.elemHgWork;
			energyAuditJob.elemVisWork = mesh.elemVisWork;
			energyAuditJob.elemAlive = mesh.elemAlive;
			energyAuditJob.result = auditResult;
			energyAuditJob.Schedule().Complete();
			return auditResult[1] + auditResult[0] + diag.erodedInternal + diag.debrisKinetic + diag.hourglassWork + diag.contactWork;
		}

		private void MeasureEnergy()
		{
			EnergyAuditJob energyAuditJob = default(EnergyAuditJob);
			energyAuditJob.nodeMass = mesh.nodeMass;
			energyAuditJob.nodeVr = mesh.nodeVr;
			energyAuditJob.nodeVz = mesh.nodeVz;
			energyAuditJob.nodeVrPrev = mesh.nodeVrPrev;
			energyAuditJob.nodeVzPrev = mesh.nodeVzPrev;
			energyAuditJob.nodeOrphan = mesh.nodeOrphan;
			energyAuditJob.elemEnergy = mesh.elemEnergy;
			energyAuditJob.elemMass = mesh.elemMass;
			energyAuditJob.elemHgWork = mesh.elemHgWork;
			energyAuditJob.elemVisWork = mesh.elemVisWork;
			energyAuditJob.elemAlive = mesh.elemAlive;
			energyAuditJob.result = auditResult;
			energyAuditJob.Schedule().Complete();
			diag.kinetic = auditResult[0];
			diag.internalEnergy = auditResult[1];
			diag.hourglassWork += auditResult[2];
			diag.viscosityWork += auditResult[3];
		}

		private void EmitFrame()
		{
			if (!timeline.HasField)
			{
				timeline.fieldGridR = 64;
				timeline.fieldGridZ = 192;
				timeline.fieldBounds = UltraFieldGrid.DomainOf(mesh);
			}
			GeometryFrame frame = default(GeometryFrame);
			frame.time = time;
			frame.projectileProfile = surface.BuildProfile(mesh, 0, out int projLoops, allShells: false);
			bool hasOverride = scenario.materialOverride != null && scenario.materialOverride.Length > 0;
			frame.targetProfile = surface.BuildProfile(mesh, 1, out int targLoops, hasOverride);
			frame.erodedMassKg = diag.erodedMassKg;
			float maxZ = float.MinValue;
			float tipVz = 0f;
			for (int i = surface.projNodeStart; i < surface.projNodeEnd; i++)
			{
				int n = surface.surfNodes[i];
				if (mesh.nodeOrphan[n] != 0)
				{
					continue;
				}
				if (mesh.nodeZ[n] > maxZ)
				{
					maxZ = mesh.nodeZ[n];
					tipVz = mesh.nodeVz[n];
				}
			}
			frame.tipAxialVelocity = tipVz;
			frame.fragmentSeeds = SnapshotSeeds();
			if (timeline.HasField)
			{
				int cells = timeline.fieldGridR * timeline.fieldGridZ;
				if (fieldScratch == null || fieldScratch.Length != cells)
				{
					fieldScratch = new float[cells];
				}
				if (bodyScratch == null || bodyScratch.Length != cells)
				{
					bodyScratch = new byte[cells];
				}
				UltraFieldGrid.Resample(mesh, mesh.elemEpsP, timeline.fieldBounds, timeline.fieldGridR, timeline.fieldGridZ, fieldScratch, bodyScratch);
				frame.fieldPlastic = new NativeArray<float>(cells, Allocator.Persistent);
				frame.fieldPlastic.CopyFrom(fieldScratch);
				frame.fieldBody = new NativeArray<byte>(cells, Allocator.Persistent);
				frame.fieldBody.CopyFrom(bodyScratch);
			}
			frame.projectileLoopCount = projLoops;
			frame.targetLoopCount = targLoops;
			timeline.frames.Add(frame);
		}

		private NativeArray<ContinuumFragmentSeed> SnapshotSeeds()
		{
			NativeArray<ContinuumFragmentSeed> result = new NativeArray<ContinuumFragmentSeed>(fragmentSeeds.Count, Allocator.Persistent);
			for (int i = 0; i < fragmentSeeds.Count; i++)
			{
				result[i] = fragmentSeeds[i];
			}
			return result;
		}

		private void TrackCraterFront()
		{
			if (scenario.targetKind != ContinuumTargetKind.Plate && scenario.targetKind != ContinuumTargetKind.SemiInfinite)
			{
				return;
			}
			float zBack;
			if (scenario.layers == null || scenario.layers.Length == 0)
			{
				zBack = scenario.targetThicknessM;
			}
			else
			{
				zBack = 0f;
				for (int i = 0; i < scenario.layers.Length; i++)
				{
					zBack += scenario.layers[i].thicknessM + scenario.layers[i].gapBeforeM;
				}
			}
			float rLimit = scenario.rodDiameterM * 0.375f;
			float minZ = float.MaxValue;
			for (int e = 0; e < mesh.elemCount; e++)
			{
				if (mesh.elemBody[e] != 1 || mesh.elemAlive[e] == 0 || mesh.elemRc[e] > rLimit)
				{
					continue;
				}
				float zc = ElemZ(e);
				if (zc >= 0f && zc <= zBack && zc < minZ)
				{
					minZ = zc;
				}
			}
			if (minZ == float.MaxValue)
			{
				return;
			}
			if (crossedQuarterT < 0f && minZ >= zBack * 0.25f)
			{
				crossedQuarterT = time;
			}
			if (crossedThreeQuarterT < 0f && minZ >= zBack * 0.75f)
			{
				crossedThreeQuarterT = time;
			}
		}

		private void ComputeEffectiveResistance()
		{
			if (scenario.targetKind != ContinuumTargetKind.Plate && scenario.targetKind != ContinuumTargetKind.SemiInfinite)
			{
				diag.resistanceNotApplicable = "нет преграды в постановке";
				return;
			}
			if (scenario.layers != null && scenario.layers.Length > 0)
			{
				diag.resistanceNotApplicable = string.Format("пакет из {0} слоёв: обратный счёт по Алексеевскому–Тейту ", scenario.layers.Length) + "требует ОДНОЙ сплошной преграды, а между 1/4 и 3/4 пути здесь зазоры и границы слоёв";
				return;
			}
			if (crossedQuarterT < 0f)
			{
				diag.resistanceNotApplicable = string.Format("фронт не прошёл 1/4 толщины ({0:F0} мм)", scenario.targetThicknessM * 0.25f * 1000f);
				return;
			}
			if (crossedQuarterT >= crossedThreeQuarterT)
			{
				diag.resistanceNotApplicable = string.Format("фронт не прошёл 3/4 толщины ({0:F0} мм) — ", scenario.targetThicknessM * 0.75f * 1000f) + "обратный счёт по Алексеевскому–Тейту требует установившегося участка между 1/4 и 3/4";
				return;
			}
			ContinuumProperties rodProps = materials[(int)scenario.rodMaterial];
			ContinuumProperties targProps = materials[(int)scenario.targetMaterial];
			float u = scenario.targetThicknessM * 0.5f / (crossedThreeQuarterT - crossedQuarterT);
			diag.interfaceVelocityMps = u;
			diag.effectiveTargetResistancePa = (scenario.impactVelocityMps - u) * (0.5f * rodProps.rho0) * (scenario.impactVelocityMps - u) + 2e+09f - (0.5f * targProps.rho0) * u * u;
		}

		private float TipAxialVelocity()
		{
			float maxZ = float.MinValue;
			float vz = 0f;
			for (int i = surface.projNodeStart; i < surface.projNodeEnd; i++)
			{
				int n = surface.surfNodes[i];
				if (mesh.nodeOrphan[n] != 0)
				{
					continue;
				}
				if (mesh.nodeZ[n] > maxZ)
				{
					maxZ = mesh.nodeZ[n];
					vz = mesh.nodeVz[n];
				}
			}
			return vz;
		}

		public void Cancel()
		{
			cancelled = true;
			Finished = true;
		}

		public GeometryTimeline Finish()
		{
			clock.Stop();
			diag.wallClockMs = clock.Elapsed.TotalMilliseconds;
			diag.elementsAlive = CountAlive();
			MeasureHourglassDistortion();
			ComputeEffectiveResistance();
			if (scenario.targetKind == ContinuumTargetKind.SemiInfinite)
			{
				float crater = CraterDepthOnAxis();
				float zBack;
				if (scenario.layers == null || scenario.layers.Length == 0)
				{
					zBack = scenario.targetThicknessM;
				}
				else
				{
					zBack = 0f;
					for (int i = 0; i < scenario.layers.Length; i++)
					{
						zBack += scenario.layers[i].thicknessM + scenario.layers[i].gapBeforeM;
					}
				}
				if (crater > zBack * 0.75f)
				{
					diag.semiInfiniteViolated = string.Format("каверна {0:F1} мм превысила {1:P0} ", crater * 1000f, 0.75f) + string.Format("толщины блока ({0:F0} мм) — приближение ", scenario.targetThicknessM * 1000f) + "полубесконечности нарушено, величины прогона НЕДЕЙСТВИТЕЛЬНЫ";
				}
			}
			ContinuumProperties rodProps = ContinuumMaterials.Get(scenario.rodMaterial);
			diag.targetTensionProbePa = rodProps.spallPa;
			if (rodProps.spallPa < 0f && elemMinPureP != null)
			{
				for (int e = 0; e < mesh.elemCount; e++)
				{
					if (mesh.elemBody[e] != 1)
					{
						continue;
					}
					if (elemMinPureP[e] < rodProps.spallPa)
					{
						diag.targetElemsBelowRodFloor++;
						diag.targetMassBelowRodFloorKg += mesh.elemMass[e];
					}
				}
			}
			for (int e2 = 0; e2 < mesh.elemCount; e2++)
			{
				for (int j = 0; j < SolverDiagnostics.TensionThresholdsPa.Length; j++)
				{
					if (elemMinPSeen[e2] < SolverDiagnostics.TensionThresholdsPa[j])
					{
						diag.deletionTensionBins[j]++;
					}
				}
			}
			if (diag.EnergyImbalanceFraction > 0.02)
			{
				diag.Warn(string.Format("невязка баланса энергии {0:F2} % ", diag.EnergyImbalanceFraction * 100.0) + "превышает порог T5 (2 %)");
			}
			if (diag.HourglassFraction > 0.05)
			{
				diag.Warn(string.Format("справочно: песочные часы {0:F2} % ", diag.HourglassFraction * 100.0) + string.Format("внутренней энергии (числитель {0:F0} Дж) — ", diag.hourglassWork) + "потолок 5 % ПРИОСТАНОВЛЕН как ворота, см. итерацию 9");
			}
			timeline.diagnostics = diag;
			timeline.Completed = !cancelled;
			timeline.Cancelled = cancelled;
			return timeline;
		}

		private void MeasureHourglassDistortion()
		{
			double mean = 0.0;
			double max = 0.0;
			int distorted = 0;
			int count = 0;
			float h = ((mesh.hCharacteristic > 0f) ? mesh.hCharacteristic : 1f);
			for (int e = 0; e < mesh.elemCount; e++)
			{
				if (mesh.elemAlive[e] == 0)
				{
					continue;
				}
				int n0 = mesh.elemNodes[e * 4];
				int n1 = mesh.elemNodes[e * 4 + 1];
				int n2 = mesh.elemNodes[e * 4 + 2];
				int n3 = mesh.elemNodes[e * 4 + 3];
				float dz = mesh.nodeZ[n0] - mesh.nodeZ[n1] + mesh.nodeZ[n2] - mesh.nodeZ[n3];
				float dr = mesh.nodeR[n0] - mesh.nodeR[n1] + mesh.nodeR[n2] - mesh.nodeR[n3];
				double d = MathF.Sqrt(dz * dz + dr * dr) / h;
				mean += d;
				if (d > max)
				{
					max = d;
				}
				if (d > 0.5)
				{
					distorted++;
				}
				count++;
			}
			if (count > 0)
			{
				mean /= count;
			}
			else
			{
				mean = 0.0;
			}
			diag.hourglassDistortionMean = mean;
			diag.hourglassDistortionMax = max;
			diag.hourglassDistortedElements = distorted;
			if (diag.hourglassDistortionMean > SolverDiagnostics.CheckerboardMean)
			{
				diag.Warn(string.Format("сетка ушла в «шахматку»: среднее искажение {0:F3} ", diag.hourglassDistortionMean) + string.Format("выше порога {0:F2} — контроль песочных часов СЛАБ, ", SolverDiagnostics.CheckerboardMean) + "поднимать Q_HG или переходить на жёсткостную форму");
			}
		}

		private float CraterDepthOnAxis()
		{
			float rLimit = scenario.rodDiameterM * 0.375f;
			float depth = 0f;
			for (int e = 0; e < mesh.elemCount; e++)
			{
				if (mesh.elemBody[e] != 1)
				{
					continue;
				}
				if (mesh.elemAlive[e] != 0)
				{
					continue;
				}
				if (mesh.elemRc[e] > rLimit)
				{
					continue;
				}
				float z = ElemZ(e);
				if (z <= depth)
				{
					continue;
				}
				if (z > scenario.TargetSpanM)
				{
					continue;
				}
				depth = z;
			}
			return depth;
		}

		private int CountAlive()
		{
			int alive = 0;
			for (int e = 0; e < mesh.elemCount; e++)
			{
				if (mesh.elemAlive[e] != 0)
				{
					alive++;
				}
			}
			return alive;
		}

		public void ReleaseWorkingSet()
		{
			materials.Dispose();
			dtResult.Dispose();
			auditResult.Dispose();
			contactWorkResult.Dispose();
			surface.Dispose();
			contact.Dispose();
			mesh.Dispose();
		}
	}

	public int stepsPerYield;

	public ContinuumBackend()
	{
		stepsPerYield = 24;
	}

	public GeometryTimeline Solve(ContinuumScenario scenario, IProgress<float> progress, CancellationToken ct)
	{
		Run run = new Run(scenario);
		try
		{
			while (!run.Finished)
			{
				if (ct.IsCancellationRequested)
				{
					run.Cancel();
					break;
				}
				run.Step();
				if (progress != null && (run.StepIndex & 63) == 0)
				{
					progress.Report(run.Progress);
				}
			}
			if (progress != null)
			{
				progress.Report(1f);
			}
			return run.Finish();
		}
		finally
		{
			run.ReleaseWorkingSet();
		}
	}

	public IEnumerator SolveCoroutine(ContinuumScenario scenario, IProgress<float> progress, CancellationToken ct, Action<GeometryTimeline> onDone)
	{
		Run run = new Run(scenario);
		GeometryTimeline timeline = null;
		try
		{
			while (!run.Finished)
			{
				if (ct.IsCancellationRequested)
				{
					run.Cancel();
					break;
				}
				for (int i = 0; i < stepsPerYield; i++)
				{
					if (run.Finished)
					{
						break;
					}
					run.Step();
				}
				if (progress != null)
				{
					progress.Report(run.Progress);
				}
				yield return null;
			}
			if (progress != null)
			{
				progress.Report(1f);
			}
			timeline = run.Finish();
		}
		finally
		{
			run.ReleaseWorkingSet();
		}
		if (onDone != null)
		{
			onDone(timeline);
		}
	}
}
