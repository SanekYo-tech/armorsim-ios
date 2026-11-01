using System;
using System.Collections.Generic;
using UnityEngine;

public class RodDynamicsSolver
{
	public class Frame
	{
		public float t;

		public Vector3[] ringCenter;

		public bool[] ringAlive;

		public float[] ringDrop;

		public float erodedM;
	}

	public class ChunkEvent
	{
		public float t;

		public float massKg;

		public float lengthM;

		public float avgRadiusM;

		public Vector3 vCm;

		public Vector3 omega;

		public Vector3 posCm;

		public int rings;
	}

	public struct ExternalLoad
	{
		public float tStart;

		public float tEnd;

		public Vector3 totalForce;

		public float zFromTipMin;

		public float zFromTipMax;
	}

	public int vertsPerRing;

	public int rings;

	public float densify;

	public float safety;

	public float dRef;

	public float tRef;

	public float zetaDash;

	public float kFlow;

	public float epsErode;

	public float cascadeShare;

	public float backboneAreaMult;

	public float mu;

	private Vector3[] pos;

	private Vector3[] vel;

	private Vector3[] force;

	private float[] mass;

	private float[] invMass;

	private bool[] dead;

	private bool[] driven;

	private float[] side0;

	private float[] partEp;

	private int[] partSpringCnt;

	private int[] calm;

	private int[] sA;

	private int[] sB;

	private byte[] sType;

	private int[] sLayer;

	private float[] sK;

	private float[] sL0;

	private float[] sL0init;

	private float[] sEp;

	private float[] sEpS;

	private bool[] sDead;

	private int[,] axialOf;

	private int[] backboneOf;

	private List<int>[] diagsOf;

	private int N;

	private int springs;

	private int vp;

	private int rn;

	private int shellCount;

	private float r0;

	private float L0;

	private float rho;

	private float E;

	private float Yp;

	private float epsY;

	private float epsF;

	private float areaRod;

	private float hMin;

	private float cSound;

	private float massTotal;

	private bool brittle;

	private float[] aRing0;

	private Vector3 planeN;

	private Vector3 planeP0;

	private float kContact;

	private bool contact;

	private bool staticPlane;

	private bool syntheticDrive;

	private bool freeFlight;

	private float freeFlightT;

	private float synthV0;

	private float synthVout;

	private float synthT;

	private float consumeRate;

	private float consumeAcc;

	private float freeRhoT;

	private float freeRt;

	private List<TateStep> steps;

	private TateModel.Penetrator pen;

	private TateModel.Target tgt;

	private float simTime;

	private float[] zCellLo;

	private float[] zCellHi;

	private float[] ringW;

	private float[] ringLiveMass;

	private float loadedSideSign;

	private Vector3 curPlaneP;

	private bool planeValid;

	public readonly List<Frame> frames;

	public readonly List<ChunkEvent> chunkEvents;

	private readonly List<ExternalLoad> extLoads;

	public float dt { get; private set; }

	public bool exploded { get; private set; }

	public float consumedMass { get; private set; }

	public int fiberBreaks { get; private set; }

	public int totalSteps { get; private set; }

	public int doneSteps { get; private set; }

	public float Progress
	{
		get
		{
			if (totalSteps <= 0)
			{
				return 0f;
			}
			float num = (float)doneSteps / totalSteps;
			if (num < 0f)
			{
				num = 0f;
			}
			if (num > 1f)
			{
				num = 1f;
			}
			return num;
		}
	}

	public IReadOnlyList<ExternalLoad> ExternalLoads => extLoads;

	public Vector3 AppliedLinearImpulse { get; private set; }

	public Vector3 AppliedAngularImpulse { get; private set; }

	public float PrescribedImpulseMagnitude { get; private set; }

	public float AppliedImpulseMagnitude { get; private set; }

	public float LastSampledErodedM { get; private set; }

	public float EventDuration
	{
		get
		{
			if (freeFlight)
			{
				return freeFlightT;
			}
			if (syntheticDrive)
			{
				return synthT;
			}
			if (steps != null && steps.Count > 0)
			{
				return steps[steps.Count - 1].t;
			}
			return 0f;
		}
	}

	public float FractureStrain => epsF;

	public float YieldStrain => epsY;

	public int RingCount => rn;

	public Vector3 PlaneNormal => planeN;

	public float RadiusM => r0;

	public float OriginToPlaneM => Vector3.Dot(-planeP0, planeN);

	public static float YoungsModulus(Projectile.CoreType t)
	{
		if (t == Projectile.CoreType.Steel)
		{
			return 2.05E+11f;
		}
		if (t == Projectile.CoreType.DU)
		{
			return 1.9E+11f;
		}
		if (t == Projectile.CoreType.WC)
		{
			return 6E+11f;
		}
		return 3.6E+11f;
	}

	public RodDynamicsSolver(Projectile.CoreType coreType, float diameterMM, float lengthMM)
	{
		vertsPerRing = 24;
		rings = 40;
		densify = 1.5f;
		safety = 0.25f;
		dRef = 0.9f;
		tRef = 0.001f;
		zetaDash = 0.05f;
		kFlow = 1f;
		epsErode = 0.3f;
		cascadeShare = 0.3f;
		backboneAreaMult = 6f;
		mu = 0.1f;
		planeN = Vector3.back;
		frames = new List<Frame>();
		chunkEvents = new List<ChunkEvent>();
		extLoads = new List<ExternalLoad>();
		pen = CoreOf(coreType);
		rho = pen.density;
		Yp = pen.Yp;
		E = YoungsModulus(coreType);
		brittle = Projectile.IsBrittleCore(coreType);
		epsY = Yp / E;
		epsF = Projectile.CoreFractureStrain(coreType);
		r0 = diameterMM * 0.5f / 1000f;
		if (r0 < 1E-04f)
		{
			r0 = 1E-04f;
		}
		L0 = lengthMM / 1000f;
		if (L0 < 0.001f)
		{
			L0 = 0.001f;
		}
		Build();
	}

	public static TateModel.Penetrator CoreOfPublic(Projectile.CoreType t)
	{
		if (t == Projectile.CoreType.DU)
		{
			return TateModel.DU();
		}
		if (t == Projectile.CoreType.Steel)
		{
			return TateModel.Steel();
		}
		if (t == Projectile.CoreType.WC)
		{
			return TateModel.WC();
		}
		return TateModel.WHA();
	}

	private static TateModel.Penetrator CoreOf(Projectile.CoreType t)
	{
		if (t == Projectile.CoreType.DU)
		{
			return TateModel.DU();
		}
		if (t == Projectile.CoreType.Steel)
		{
			return TateModel.Steel();
		}
		if (t == Projectile.CoreType.WC)
		{
			return TateModel.WC();
		}
		return TateModel.WHA();
	}

	private void Build()
	{
		vp = vertsPerRing;
		rn = rings;
		shellCount = rings * vertsPerRing;
		areaRod = r0 * Mathf.PI * r0;
		N = (vertsPerRing + 1) * rings;
		pos = new Vector3[N];
		vel = new Vector3[N];
		force = new Vector3[N];
		mass = new float[N];
		invMass = new float[N];
		dead = new bool[N];
		driven = new bool[N];
		side0 = new float[N];
		partEp = new float[N];
		partSpringCnt = new int[N];
		calm = new int[N];
		aRing0 = new float[rn];
		zCellLo = new float[rn];
		zCellHi = new float[rn];
		ringW = new float[rn];
		ringLiveMass = new float[rn];
		for (int i = 0; i < rn; i++)
		{
			aRing0[i] = Mathf.Pow((float)i / (rn - 1), densify) * L0;
			for (int j = 0; j < vp; j++)
			{
				float num = (float)j / vp * Mathf.PI * 2f;
				pos[i * vp + j] = new Vector3(Mathf.Cos(num) * r0, Mathf.Sin(num) * r0, 0f - aRing0[i]);
			}
			pos[shellCount + i] = new Vector3(0f, 0f, 0f - aRing0[i]);
		}
		for (int k = 0; k < N; k++)
		{
			side0[k] = ((pos[k].x < 0f) ? (-1f) : 1f);
		}
		for (int l = 0; l < rn; l++)
		{
			zCellLo[l] = ((l != 0) ? (aRing0[l - 1] + aRing0[l]) * 0.5f : 0f);
			zCellHi[l] = ((l != rn - 1) ? (aRing0[l] + aRing0[l + 1]) * 0.5f : L0);
		}
		for (int m = 0; m < rn; m++)
		{
			float num2 = (zCellHi[m] - zCellLo[m]) * (rho * areaRod);
			mass[shellCount + m] = num2 * 0.25f;
			for (int n = 0; n < vp; n++)
			{
				mass[m * vp + n] = num2 * 0.75f / vp;
			}
		}
		for (int num3 = 0; num3 < N; num3++)
		{
			invMass[num3] = 1f / mass[num3];
		}
		massTotal = 0f;
		for (int num4 = 0; num4 < N; num4++)
		{
			massTotal += mass[num4];
		}
		HashSet<long> seen = new HashSet<long>();
		List<int> la = new List<int>();
		List<int> lb = new List<int>();
		List<byte> lt = new List<byte>();
		List<int> lly = new List<int>();
		void Add(int p, int q, byte type, int layer = -1)
		{
			if (!seen.Add((long)Math.Min(p, q) * N + Math.Max(p, q)))
			{
				return;
			}
			la.Add(p);
			lb.Add(q);
			lt.Add(type);
			lly.Add(layer);
		}
		for (int num5 = 0; num5 < rn; num5++)
		{
			for (int num6 = 0; num6 < vp; num6++)
			{
				int num7 = num5 * vp + num6;
				int num8 = num5 * vp + (num6 + 1) % vp;
				Add(num7, num8, 0);
				if (num5 + 1 < rn)
				{
					int num9 = (num5 + 1) * vp + num6;
					Add(num7, num9, 1, num5);
					Add(num8, num9, 2, num5);
					Add(num7, (num5 + 1) * vp + (num6 + 1) % vp, 2, num5);
				}
				Add(shellCount + num5, num7, 3);
			}
		}
		for (int num10 = 0; num10 + 1 < rn; num10++)
		{
			Add(shellCount + num10, shellCount + num10 + 1, 4, num10);
		}
		springs = la.Count;
		sA = la.ToArray();
		sB = lb.ToArray();
		sType = lt.ToArray();
		sLayer = lly.ToArray();
		sK = new float[springs];
		sL0 = new float[springs];
		sL0init = new float[springs];
		sEp = new float[springs];
		sEpS = new float[springs];
		sDead = new bool[springs];
		hMin = float.MaxValue;
		for (int num11 = 0; num11 < springs; num11++)
		{
			Vector3 vector = pos[sA[num11]] - pos[sB[num11]];
			float magnitude = vector.magnitude;
			sL0[num11] = magnitude;
			sL0init[num11] = magnitude;
			float num12 = ((sType[num11] == 4) ? backboneAreaMult : 1f);
			sK[num11] = num12 * (areaRod / vp) * E / Mathf.Max(magnitude, 1E-06f);
			if (magnitude < hMin)
			{
				hMin = magnitude;
			}
		}
		cSound = Mathf.Sqrt(E / rho);
		dt = hMin * zetaDash / cSound;
		axialOf = new int[rn, vp];
		backboneOf = new int[rn];
		diagsOf = new List<int>[rn];
		for (int num13 = 0; num13 < rn; num13++)
		{
			diagsOf[num13] = new List<int>(vp * 2);
			for (int num14 = 0; num14 < vp; num14++)
			{
				axialOf[num13, num14] = -1;
			}
			backboneOf[num13] = -1;
		}
		for (int num15 = 0; num15 < springs; num15++)
		{
			if (sLayer[num15] < 0)
			{
				continue;
			}
			if (sType[num15] == 1)
			{
				axialOf[sLayer[num15], sA[num15] % vp] = num15;
			}
			else if (sType[num15] == 2)
			{
				diagsOf[sLayer[num15]].Add(num15);
			}
			else if (sType[num15] == 4)
			{
				backboneOf[sLayer[num15]] = num15;
			}
		}
		kContact = areaRod / vp * E / (hMin * 100f);
	}

	public void SetInitialVelocity(float vMps)
	{
		for (int i = 0; i < N; i++)
		{
			vel[i] = new Vector3(0f, 0f, vMps);
		}
	}

	private void DriveTail()
	{
		for (int i = rn - 3; i < rn; i++)
		{
			for (int j = 0; j < vp; j++)
			{
				driven[i * vp + j] = true;
			}
			driven[shellCount + i] = true;
		}
	}

	private void SetPlane(float betaDeg)
	{
		float num = betaDeg * 0.0174532924f;
		float num2 = Mathf.Sin(num);
		float num3 = 0f - Mathf.Cos(num);
		planeN = new Vector3(num2, 0f, num3);
		planeP0 = Vector3.zero;
		float num4 = float.MaxValue;
		float num5 = 0f;
		for (int i = 0; i < N; i++)
		{
			float num6 = (pos[i].x - planeP0.x) * planeN.x + (pos[i].y - planeP0.y) * planeN.y + (pos[i].z - planeP0.z) * planeN.z;
			if (num6 < num4)
			{
				num4 = num6;
				num5 = ((pos[i].x < 0f) ? (-1f) : 1f);
			}
		}
		planeP0 += planeN * num4;
		if (0.01f >= betaDeg)
		{
			num5 = 0f;
		}
		loadedSideSign = num5;
	}

	public int ConfigureSteps(List<TateStep> coreSteps, TateModel.Target target, float betaDeg, float entryVel)
	{
		steps = coreSteps;
		tgt = target;
		contact = true;
		staticPlane = false;
		syntheticDrive = false;
		for (int i = 0; i < N; i++)
		{
			vel[i] = new Vector3(0f, 0f, entryVel);
		}
		DriveTail();
		SetPlane(betaDeg);
		float num = ((coreSteps != null && coreSteps.Count > 0) ? coreSteps[coreSteps.Count - 1].t : 1E-04f);
		totalSteps = Mathf.Max(1, (int)(num / dt));
		return totalSteps;
	}

	public int ConfigureFreeFlight(float velocityMS, float durationS)
	{
		contact = false;
		staticPlane = false;
		syntheticDrive = false;
		freeFlight = true;
		steps = null;
		consumeRate = 0f;
		planeValid = false;
		for (int i = 0; i < N; i++)
		{
			driven[i] = false;
		}
		for (int j = 0; j < N; j++)
		{
			vel[j] = new Vector3(0f, 0f, velocityMS);
		}
		freeFlightT = Mathf.Max(durationS, dt);
		totalSteps = Mathf.Max(1, (int)(freeFlightT / dt));
		return totalSteps;
	}

	public int ConfigureSynthetic(float betaDeg, float vIn, float vOut, float erodeFrac, TateModel.Target target, bool ricochet)
	{
		tgt = target;
		freeRhoT = target.density;
		freeRt = target.Rt;
		synthV0 = vIn;
		synthVout = vOut;
		contact = true;
		staticPlane = true;
		syntheticDrive = true;
		synthT = (ricochet ? (r0 * 2f * 3f) : L0) / vIn;
		for (int i = 0; i < N; i++)
		{
			vel[i] = new Vector3(0f, 0f, vIn);
		}
		DriveTail();
		SetPlane(betaDeg);
		float num = 0f;
		for (int j = 0; j < N; j++)
		{
			num += mass[j];
		}
		consumeRate = num * erodeFrac / Mathf.Max(synthT, 1E-09f);
		totalSteps = Mathf.Max(1, (int)(synthT / dt));
		return totalSteps;
	}

	public void RunBatch(int nSteps, int saveEvery)
	{
		for (int i = 0; i < nSteps; i++)
		{
			if (doneSteps >= totalSteps || exploded)
			{
				break;
			}
			Step();
			doneSteps++;
			if (doneSteps % saveEvery == 0 || doneSteps == totalSteps)
			{
				SaveFrame();
			}
		}
	}

	private void SaveFrame()
	{
		Frame frame = new Frame();
		frame.t = simTime;
		frame.ringCenter = new Vector3[rn];
		frame.ringAlive = new bool[rn];
		frame.ringDrop = new float[rn];
		frame.erodedM = ErodedFront();
		for (int i = 0; i < rn; i++)
		{
			Vector3 vector = Vector3.zero;
			int num = 0;
			for (int j = i * vp; j < (i + 1) * vp; j++)
			{
				if (!dead[j])
				{
					vector += pos[j];
					num++;
				}
			}
			if (num < vp / 3)
			{
				continue;
			}
			Vector3 vector2 = vector / num;
			frame.ringCenter[i] = vector2;
			frame.ringAlive[i] = true;
			float num2 = 0f;
			for (int k = i * vp; k < (i + 1) * vp; k++)
			{
				if (!dead[k])
				{
					float num3 = Vector3.Dot(vector2 - pos[k], planeN);
					if (num3 > num2)
					{
						num2 = num3;
					}
				}
			}
			frame.ringDrop[i] = num2;
		}
		frames.Add(frame);
	}

	private float ErodedFront()
	{
		if (steps != null && steps.Count > 0)
		{
			int num = 0;
			for (int i = 1; i < steps.Count; i++)
			{
				if (simTime <= steps[i].t)
				{
					break;
				}
				num++;
			}
			int num2 = ((num + 1 < steps.Count - 1) ? (num + 1) : (steps.Count - 1));
			float num3 = 1f;
			if (num2 > num)
			{
				num3 = ((steps[num2].t != steps[num].t) ? Mathf.Clamp01((simTime - steps[num].t) / (steps[num2].t - steps[num].t)) : 0f);
			}
			float num4 = L0 - (steps[num].L + (steps[num2].L - steps[num].L) * num3);
			if (num4 < 0f)
			{
				return 0f;
			}
			if (num4 > L0)
			{
				return L0;
			}
			return num4;
		}
		if (massTotal > 1E-12f)
		{
			float num5 = consumedMass / massTotal * L0;
			if (num5 < 0f)
			{
				return 0f;
			}
			if (num5 > L0)
			{
				return L0;
			}
			return num5;
		}
		return 0f;
	}

	public void AddExternalLoad(float tStart, float tEnd, Vector3 totalForce, float zFromTipMin, float zFromTipMax)
	{
		extLoads.Add(new ExternalLoad
		{
			tStart = tStart,
			tEnd = tEnd,
			totalForce = totalForce,
			zFromTipMin = zFromTipMin,
			zFromTipMax = zFromTipMax
		});
	}

	private float CellOverlap(int ring, float zMin, float zMax)
	{
		float num = ((zCellLo[ring] >= zMin) ? zCellLo[ring] : zMin);
		float num2 = ((zCellHi[ring] <= zMax) ? zCellHi[ring] : zMax);
		if (num2 <= num)
		{
			return 0f;
		}
		return num2 - num;
	}

	private int NearestLiveRing(float z)
	{
		float num = float.MaxValue;
		int result = -1;
		for (int i = 0; i < rn; i++)
		{
			if (1E-12f >= ringLiveMass[i])
			{
				continue;
			}
			float num2 = Mathf.Abs(aRing0[i] - z);
			if (num > num2)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	private void ApplyExternalLoads()
	{
		if (extLoads.Count == 0)
		{
			return;
		}
		bool flag = false;
		int num = 0;
		while (num < extLoads.Count && !flag)
		{
			flag = simTime >= extLoads[num].tStart && simTime <= extLoads[num].tEnd;
			num++;
		}
		if (!flag)
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		float num2 = 0f;
		for (int i = 0; i < rn; i++)
		{
			ringLiveMass[i] = 0f;
		}
		for (int j = 0; j < N; j++)
		{
			if (!dead[j])
			{
				vector += pos[j] * mass[j];
				num2 += mass[j];
				int num3 = ((j < shellCount) ? (j / vp) : (j - shellCount));
				ringLiveMass[num3] += mass[j];
			}
		}
		if (num2 <= 1E-12f)
		{
			return;
		}
		Vector3 vector2 = vector / num2;
		for (int k = 0; k < extLoads.Count; k++)
		{
			ExternalLoad externalLoad = extLoads[k];
			if (externalLoad.tStart > simTime || simTime > externalLoad.tEnd)
			{
				continue;
			}
			PrescribedImpulseMagnitude += externalLoad.totalForce.magnitude * dt;
			float num4 = 0f;
			for (int l = 0; l < rn; l++)
			{
				float num5 = ((ringLiveMass[l] > 1E-12f) ? CellOverlap(l, externalLoad.zFromTipMin, externalLoad.zFromTipMax) : 0f);
				ringW[l] = num5;
				num4 += num5;
			}
			if (num4 <= 1E-12f)
			{
				int num6 = NearestLiveRing((externalLoad.zFromTipMin + externalLoad.zFromTipMax) * 0.5f);
				if (num6 < 0)
				{
					continue;
				}
				for (int m = 0; m < rn; m++)
				{
					ringW[m] = 0f;
				}
				ringW[num6] = 1f;
				num4 = 1f;
			}
			Vector3 vector3 = Vector3.zero;
			Vector3 vector4 = Vector3.zero;
			for (int n = 0; n < rn; n++)
			{
				if (ringW[n] <= 0f)
				{
					continue;
				}
				float num7 = ringW[n] / num4 / ringLiveMass[n];
				for (int num8 = 0; num8 <= vp; num8++)
				{
					int num9 = ((num8 < vp) ? (n * vp + num8) : (shellCount + n));
					if (dead[num9])
					{
						continue;
					}
					Vector3 vector5 = externalLoad.totalForce * (num7 * mass[num9]);
					force[num9] += vector5;
					vector3 += vector5 * dt;
					vector4 += Vector3.Cross(pos[num9] - vector2, vector5 * dt);
				}
			}
			AppliedLinearImpulse += vector3;
			AppliedAngularImpulse += vector4;
			AppliedImpulseMagnitude += vector3.magnitude;
		}
	}

	private void Step()
	{
		Array.Clear(force, 0, N);
		simTime += dt;
		ApplyExternalLoads();
		for (int i = 0; i < springs; i++)
		{
			if (sDead[i])
			{
				continue;
			}
			int num = sA[i];
			int num2 = sB[i];
			Vector3 vector = pos[num] - pos[num2];
			float magnitude = vector.magnitude;
			if (magnitude < 1E-09f)
			{
				continue;
			}
			Vector3 vector2 = vector / magnitude;
			float num3 = magnitude - sL0[i];
			float num4 = num3 * sK[i];
			if (zetaDash > 0f)
			{
				float num5 = 1f / (invMass[num] + invMass[num2]) * sK[i];
				float num6 = Mathf.Sqrt(num5);
				num4 += num6 * 2f * zetaDash * Vector3.Dot(vel[num] - vel[num2], vector2);
			}
			Vector3 vector3 = vector2 * num4;
			force[num] -= vector3;
			force[num2] += vector3;
			if (!brittle)
			{
				float num7 = num3 / sL0init[i];
				if (Mathf.Abs(num7) > epsY)
				{
					float num8 = ((num7 < 0f) ? (-1f) : 1f);
					float num9 = (magnitude - (num8 * epsY + 1f) * sL0[i]) * kFlow;
					sL0[i] += num9;
					float num10 = Mathf.Abs(num9) / sL0init[i];
					sEp[i] += num10;
					sEpS[i] += num9 / sL0init[i];
					partEp[num] += num10;
					partEp[num2] += num10;
				}
			}
		}
		if (contact)
		{
			Contact();
		}
		if (syntheticDrive && planeValid)
		{
			ConsumeBudget();
		}
		if (CheckFracture())
		{
			SplitChunks();
		}
		float num11 = Mathf.Pow(dRef, dt / tRef);
		float num12 = 0f;
		if (contact && !staticPlane && steps != null)
		{
			num12 = StepsV(simTime);
		}
		if (syntheticDrive)
		{
			num12 = (synthVout - synthV0) * Mathf.Clamp01(simTime / synthT) + synthV0;
		}
		Vector3 vector4 = Vector3.zero;
		float num13 = 0f;
		for (int j = 0; j < N; j++)
		{
			if (!dead[j])
			{
				vector4 += vel[j] * mass[j];
				num13 += mass[j];
			}
		}
		num13 = Mathf.Max(num13, 1E-09f);
		Vector3 vector5 = vector4 / num13;
		for (int k = 0; k < N; k++)
		{
			if (dead[k])
			{
				pos[k] += vel[k] * dt;
				continue;
			}
			if (driven[k])
			{
				vel[k] += force[k] * (invMass[k] * dt);
				vel[k] = (vel[k] - vector5) * num11 + vector5;
				vel[k].z = num12;
				pos[k] += vel[k] * dt;
				continue;
			}
			vel[k] += force[k] * (invMass[k] * dt);
			vel[k] = (vel[k] - vector5) * num11 + vector5;
			if (calm[k] > 0)
			{
				vel[k] = (vel[k] - vector5) * 0.85f + vector5;
				calm[k]--;
			}
			pos[k] += vel[k] * dt;
			if (float.IsNaN(pos[k].x) || vel[k].magnitude > 5000f)
			{
				exploded = true;
			}
		}
	}

	private float StepsV(float t)
	{
		if (steps == null || steps.Count == 0)
		{
			return 0f;
		}
		if (t <= steps[0].t)
		{
			return steps[0].v;
		}
		if (t >= steps[steps.Count - 1].t)
		{
			return steps[steps.Count - 1].v;
		}
		int num = 0;
		for (int i = 1; i < steps.Count; i++)
		{
			if (t <= steps[i].t)
			{
				break;
			}
			num++;
		}
		float num2 = ((steps[num + 1].t != steps[num].t) ? Mathf.Clamp01((t - steps[num].t) / (steps[num + 1].t - steps[num].t)) : 0f);
		return steps[num].v + (steps[num + 1].v - steps[num].v) * num2;
	}

	private (float, float, float) StepsState(float t)
	{
		if (steps == null || steps.Count <= 0)
		{
			return (0f, 0f, 0f);
		}
		int num = 0;
		for (int i = 1; i < steps.Count; i++)
		{
			if (t <= steps[i].t)
			{
				break;
			}
			num++;
		}
		int num2 = ((num + 1 < steps.Count - 1) ? (num + 1) : (steps.Count - 1));
		float num3 = 1f;
		if (num2 > num)
		{
			num3 = ((steps[num2].t != steps[num].t) ? Mathf.Clamp01((t - steps[num].t) / (steps[num2].t - steps[num].t)) : 0f);
		}
		float num4 = steps[num].v + (steps[num2].v - steps[num].v) * num3;
		float num5 = steps[num].depthMM + (steps[num2].depthMM - steps[num].depthMM) * num3;
		float num6 = TateModel.PenetrationVelocity(pen, tgt, num4);
		if (num6 < 0f)
		{
			num6 = 0f;
		}
		else if (num6 > num4)
		{
			num6 = num4;
		}
		return (num5 / 1000f, num6, num4);
	}

	private void Contact()
	{
		Vector3 vector;
		Vector3 vector2;
		float num;
		if (!staticPlane)
		{
			(float, float, float) tuple = StepsState(simTime);
			vector = planeP0 + new Vector3(0f, 0f, tuple.Item1);
			vector2 = new Vector3(0f, 0f, tuple.Item2);
			num = tgt.density * 0.5f * tuple.Item2 * tuple.Item2 + tgt.Rt;
		}
		else
		{
			vector = planeP0;
			vector2 = Vector3.zero;
			Vector3 vector3 = Vector3.zero;
			float num2 = 0f;
			for (int i = 0; i < N; i++)
			{
				if (!dead[i])
				{
					vector3 += vel[i] * mass[i];
					num2 += mass[i];
				}
			}
			num2 = Mathf.Max(num2, 1E-09f);
			float num3 = Mathf.Max(Vector3.Dot(-planeN, vector3 / num2), 0f);
			num = freeRhoT * 0.5f * num3 * num3 + freeRt;
		}
		float num4 = 0f;
		float num5 = r0;
		for (int j = 0; j < N; j++)
		{
			if (!dead[j] && !driven[j])
			{
				float num6 = -Vector3.Dot(pos[j] - vector, planeN);
				if (num6 > 0f)
				{
					float num7 = Mathf.Min(num6 * kContact, areaRod / (vp + 1) * num);
					num4 += num7;
					float num8 = Mathf.Sqrt(pos[j].x * pos[j].x + pos[j].y * pos[j].y);
					if (num8 > num5)
					{
						num5 = num8;
					}
				}
			}
		}
		float num9 = num * Mathf.PI * num5 * num5;
		float num10 = 1f;
		if (num4 > num9 && num4 > 1E-09f)
		{
			num10 = num9 / num4;
		}
		curPlaneP = vector;
		planeValid = true;
		for (int k = 0; k < N; k++)
		{
			if (dead[k] || driven[k])
			{
				continue;
			}
			float num11 = -Vector3.Dot(pos[k] - vector, planeN);
			if (num11 <= 0f)
			{
				continue;
			}
			float num12 = Mathf.Min(num11 * kContact, areaRod / (vp + 1) * num) * num10;
			Vector3 vector4 = vel[k] - vector2;
			float num13 = -Vector3.Dot(vector4, planeN);
			if (num13 > 0f)
			{
				num12 += Mathf.Sqrt(mass[k] * kContact) * 1.4f * num13;
			}
			force[k] += planeN * num12;
			Vector3 vector5 = vector4 - planeN * Vector3.Dot(vector4, planeN);
			float magnitude = vector5.magnitude;
			if (magnitude > 1E-06f)
			{
				force[k] -= vector5 / magnitude * (num12 * mu);
			}
			int num14 = SpringCountOf(k);
			float num15 = ((num14 >= 1) ? (partEp[k] / num14) : 0f);
			if (!brittle && num15 > epsErode)
			{
				Detach(k);
			}
		}
	}

	private void ConsumeBudget()
	{
		if (consumeRate <= 0f)
		{
			return;
		}
		consumeAcc += consumeRate * dt;
		while (consumeAcc > 0f)
		{
			int num = -1;
			float num2 = float.MaxValue;
			for (int i = 0; i < N; i++)
			{
				if (!dead[i] && !driven[i])
				{
					float num3 = Vector3.Dot(pos[i] - curPlaneP, planeN);
					if (num2 > num3)
					{
						num2 = num3;
						num = i;
					}
				}
			}
			if (num < 0 || num2 > hMin * 2f)
			{
				break;
			}
			consumeAcc -= mass[num];
			consumedMass += mass[num];
			Detach(num);
		}
	}

	private int SpringCountOf(int i)
	{
		if (partSpringCnt[i] == 0)
		{
			int num = 0;
			for (int j = 0; j < springs; j++)
			{
				if (sA[j] == i || sB[j] == i)
				{
					num++;
				}
			}
			partSpringCnt[i] = ((num <= 1) ? 1 : num);
		}
		return partSpringCnt[i];
	}

	private void Detach(int i)
	{
		if (dead[i])
		{
			return;
		}
		dead[i] = true;
		for (int j = 0; j < springs; j++)
		{
			if (!sDead[j] && (sA[j] == i || sB[j] == i))
			{
				sDead[j] = true;
			}
		}
	}

	private bool CheckFracture()
	{
		bool result = false;
		if (brittle)
		{
			for (int i = 0; i < springs; i++)
			{
				if (sDead[i])
				{
					continue;
				}
				Vector3 vector = pos[sA[i]] - pos[sB[i]];
				float magnitude = vector.magnitude;
				if (Mathf.Abs(magnitude - sL0[i]) / sL0init[i] > epsY * 1.5f)
				{
					sDead[i] = true;
					result = true;
					fiberBreaks++;
					calm[sA[i]] = 10;
					calm[sB[i]] = 10;
				}
			}
			return result;
		}
		if (epsF <= 0f)
		{
			return false;
		}
		for (int j = 0; j < rn - 1; j++)
		{
			float num = 0f;
			int num2 = 0;
			int num3 = 0;
			for (int k = 0; k < vp; k++)
			{
				int num4 = axialOf[j, k];
				if (num4 < 0)
				{
					continue;
				}
				if (sDead[num4])
				{
					num3++;
					continue;
				}
				num += sEpS[num4];
				num2++;
			}
			if (num2 == 0)
			{
				continue;
			}
			float num5 = num / num2;
			for (int l = 0; l < vp; l++)
			{
				int num6 = axialOf[j, l];
				if (num6 < 0 || sDead[num6])
				{
					continue;
				}
				if (sEpS[num6] - num5 > epsF)
				{
					RipFiber(j, l, num6);
					num3++;
					result = true;
				}
			}
			int num7 = backboneOf[j];
			if (num7 >= 0 && !sDead[num7] && (float)num3 / vp >= 0.6f)
			{
				sDead[num7] = true;
				result = true;
			}
		}
		return result;
	}

	private void RipFiber(int gap, int j, int s)
	{
		sDead[s] = true;
		fiberBreaks++;
		calm[sA[s]] = 10;
		calm[sB[s]] = 10;
		int num = sA[s];
		int num2 = sB[s];
		foreach (int num3 in diagsOf[gap])
		{
			if (sDead[num3])
			{
				continue;
			}
			if (sA[num3] == num || sB[num3] == num || sA[num3] == num2 || sB[num3] == num2)
			{
				sDead[num3] = true;
			}
		}
		int[] array = new int[2] { (j + 1) % vp, (vp - 1 + j) % vp };
		for (int m = 0; m < array.Length; m++)
		{
			int num4 = axialOf[gap, array[m]];
			if (num4 < 0 || sDead[num4])
			{
				continue;
			}
			sEpS[num4] += epsF * cascadeShare;
			sEp[num4] += epsF * cascadeShare;
		}
	}

	private void SplitChunks()
	{
		int[] array = new int[N];
		for (int i = 0; i < N; i++)
		{
			array[i] = (dead[i] ? (-1) : 0);
		}
		List<int>[] array2 = new List<int>[N];
		for (int j = 0; j < N; j++)
		{
			array2[j] = new List<int>(10);
		}
		for (int k = 0; k < springs; k++)
		{
			if (!sDead[k])
			{
				array2[sA[k]].Add(sB[k]);
				array2[sB[k]].Add(sA[k]);
			}
		}
		int num = 0;
		Stack<int> stack = new Stack<int>();
		for (int l = 0; l < N; l++)
		{
			if (array[l] == 0)
			{
				num++;
				array[l] = num;
				stack.Push(l);
				while (stack.Count > 0)
				{
					int num2 = stack.Pop();
					foreach (int num3 in array2[num2])
					{
						if (array[num3] == 0)
						{
							array[num3] = num;
							stack.Push(num3);
						}
					}
				}
			}
		}
		int num4 = -1;
		for (int m = 0; m < N; m++)
		{
			if (driven[m] && array[m] > 0)
			{
				num4 = array[m];
				break;
			}
		}
		if (num4 < 0)
		{
			int num5 = shellCount + rn - 1;
			if (array[num5] > 0)
			{
				num4 = array[num5];
			}
		}
		for (int num6 = 1; num6 <= num; num6++)
		{
			if (num6 == num4)
			{
				continue;
			}
			List<int> list = new List<int>();
			for (int n = 0; n < N; n++)
			{
				if (array[n] == num6)
				{
					list.Add(n);
				}
			}
			if (list.Count == 0)
			{
				continue;
			}
			if (list.Count < 5)
			{
				foreach (int num7 in list)
				{
					dead[num7] = true;
				}
				continue;
			}
			Vector3 vector = Vector3.zero;
			Vector3 vector2 = Vector3.zero;
			float num8 = 0f;
			float num9 = 0f;
			float num10 = float.MaxValue;
			float num11 = float.MinValue;
			foreach (int num12 in list)
			{
				vector += pos[num12] * mass[num12];
				vector2 += vel[num12] * mass[num12];
				num8 += mass[num12];
				if (pos[num12].z < num10)
				{
					num10 = pos[num12].z;
				}
				if (pos[num12].z > num11)
				{
					num11 = pos[num12].z;
				}
				num9 += Mathf.Sqrt(pos[num12].x * pos[num12].x + pos[num12].y * pos[num12].y);
			}
			Vector3 vector3 = vector / num8;
			Vector3 vector4 = vector2 / num8;
			Vector3 vector5 = Vector3.zero;
			float num13 = 0f;
			foreach (int num14 in list)
			{
				Vector3 lhs = pos[num14] - vector3;
				Vector3 rhs = vel[num14] - vector4;
				vector5 += Vector3.Cross(lhs, rhs) * mass[num14];
				num13 += (lhs.x * lhs.x + lhs.y * lhs.y + lhs.z * lhs.z) * mass[num14];
			}
			HashSet<int> hashSet = new HashSet<int>();
			foreach (int num15 in list)
			{
				if (num15 < shellCount)
				{
					hashSet.Add(num15 / vp);
				}
			}
			ChunkEvent chunkEvent = new ChunkEvent();
			chunkEvent.t = simTime;
			chunkEvent.massKg = num8;
			chunkEvent.lengthM = Mathf.Max(num11 - num10, 0.005f);
			chunkEvent.avgRadiusM = Mathf.Max(num9 / list.Count, r0 * 0.3f);
			chunkEvent.vCm = vector4;
			chunkEvent.omega = ((num13 > 1E-12f) ? (vector5 / num13) : Vector3.zero);
			chunkEvent.posCm = vector3;
			chunkEvent.rings = hashSet.Count;
			chunkEvents.Add(chunkEvent);
			foreach (int num16 in list)
			{
				dead[num16] = true;
				for (int num17 = 0; num17 < springs; num17++)
				{
					if (!sDead[num17] && (sA[num17] == num16 || sB[num17] == num16))
					{
						sDead[num17] = true;
					}
				}
			}
		}
	}

	public bool SampleFrame(float t, Vector3[] centersOut, bool[] aliveOut, float[] dropOut = null)
	{
		if (frames.Count == 0)
		{
			return false;
		}
		int num = 0;
		while (num < frames.Count && t > frames[num].t)
		{
			num++;
		}
		int num2 = Mathf.Clamp(num - 1, 0, frames.Count - 1);
		int num3 = Mathf.Clamp(num, 0, frames.Count - 1);
		Frame frame = frames[num2];
		Frame frame2 = frames[num3];
		float num4 = 1f;
		if (frame2.t - frame.t > 1E-12f)
		{
			num4 = Mathf.Clamp01((t - frame.t) / (frame2.t - frame.t));
		}
		for (int i = 0; i < rn; i++)
		{
			bool flag = frame.ringAlive[i] && frame2.ringAlive[i];
			aliveOut[i] = flag;
			if (flag)
			{
				centersOut[i] = Vector3.Lerp(frame.ringCenter[i], frame2.ringCenter[i], num4);
			}
			else
			{
				centersOut[i] = (frame.ringAlive[i] ? frame.ringCenter[i] : frame2.ringCenter[i]);
			}
			if (dropOut != null)
			{
				if (flag)
				{
					dropOut[i] = Mathf.Lerp(frame.ringDrop[i], frame2.ringDrop[i], num4);
				}
				else
				{
					dropOut[i] = (frame.ringAlive[i] ? frame.ringDrop[i] : frame2.ringDrop[i]);
				}
			}
		}
		LastSampledErodedM = Mathf.Lerp(frame.erodedM, frame2.erodedM, num4);
		return true;
	}

	public float TotalEnergyJ()
	{
		float num = 0f;
		for (int i = 0; i < N; i++)
		{
			if (!dead[i])
			{
				num += mass[i] * 0.5f * vel[i].sqrMagnitude;
			}
		}
		for (int j = 0; j < springs; j++)
		{
			if (!sDead[j])
			{
				Vector3 vector = pos[sB[j]] - pos[sA[j]];
				float num2 = vector.magnitude - sL0[j];
				num += sK[j] * 0.5f * num2 * num2;
			}
		}
		return num;
	}

	public float MaxDeviatoricPlasticStrain()
	{
		if (brittle)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < rn - 1; i++)
		{
			float num2 = 0f;
			int num3 = 0;
			for (int j = 0; j < vp; j++)
			{
				int num4 = axialOf[i, j];
				if (num4 >= 0 && !sDead[num4])
				{
					num2 += sEpS[num4];
					num3++;
				}
			}
			if (num3 == 0)
			{
				continue;
			}
			float num5 = num2 / num3;
			for (int k = 0; k < vp; k++)
			{
				int num6 = axialOf[i, k];
				if (num6 >= 0 && !sDead[num6])
				{
					float num7 = sEpS[num6] - num5;
					if (num7 > num)
					{
						num = num7;
					}
				}
			}
		}
		return num;
	}

	public float MaxTotalFiberStrain()
	{
		float num = 0f;
		for (int i = 0; i < springs; i++)
		{
			if (sDead[i] || sType[i] != 1)
			{
				continue;
			}
			Vector3 vector = pos[sB[i]] - pos[sA[i]];
			float num2 = Mathf.Abs(vector.magnitude - sL0init[i]) / sL0init[i];
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public Vector3 RigidAngularMomentum()
	{
		Vector3 vector = Vector3.zero;
		Vector3 vector2 = Vector3.zero;
		float num = 0f;
		for (int i = 0; i < N; i++)
		{
			if (!dead[i])
			{
				vector += pos[i] * mass[i];
				vector2 += vel[i] * mass[i];
				num += mass[i];
			}
		}
		if (num <= 1E-12f)
		{
			return Vector3.zero;
		}
		Vector3 vector3 = vector / num;
		Vector3 vector4 = vector2 / num;
		Vector3 vector5 = Vector3.zero;
		for (int j = 0; j < N; j++)
		{
			if (!dead[j])
			{
				Vector3 lhs = pos[j] - vector3;
				Vector3 rhs = (vel[j] - vector4) * mass[j];
				vector5 += Vector3.Cross(lhs, rhs);
			}
		}
		return vector5;
	}

	public float RigidOmegaY()
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		for (int i = 0; i < N; i++)
		{
			if (!dead[i])
			{
				num += pos[i].x * mass[i];
				num2 += pos[i].z * mass[i];
				num3 += mass[i];
			}
		}
		if (num3 <= 1E-12f)
		{
			return 0f;
		}
		float num4 = num / num3;
		float num5 = num2 / num3;
		float num6 = 0f;
		for (int j = 0; j < N; j++)
		{
			if (!dead[j])
			{
				float num7 = pos[j].x - num4;
				float num8 = pos[j].z - num5;
				num6 += (num7 * num7 + num8 * num8) * mass[j];
			}
		}
		if (num6 <= 1E-12f)
		{
			return 0f;
		}
		return RigidAngularMomentum().y / num6;
	}

	public float MaxLateralDeviationM()
	{
		Vector3[] array = new Vector3[rn];
		bool[] array2 = new bool[rn];
		if (!SampleFrame(EventDuration, array, array2))
		{
			return 0f;
		}
		int num = -1;
		for (int i = 0; i < rn; i++)
		{
			if (array2[i])
			{
				num = i;
				break;
			}
		}
		int num2 = -1;
		for (int num3 = rn - 1; num3 >= 0; num3--)
		{
			if (array2[num3])
			{
				num2 = num3;
				break;
			}
		}
		if (num < 0 || num2 < 0 || num == num2)
		{
			return 0f;
		}
		Vector3 a = array[num];
		Vector3 vector = array[num2] - a;
		float magnitude = vector.magnitude;
		if (magnitude < 1E-09f)
		{
			return 0f;
		}
		Vector3 vector2 = vector / magnitude;
		float num4 = 0f;
		for (int j = num; j <= num2; j++)
		{
			if (!array2[j])
			{
				continue;
			}
			Vector3 vector3 = array[j] - a;
			float num5 = Vector3.Dot(vector3, vector2);
			Vector3 vector4 = vector3 - vector2 * num5;
			float magnitude2 = vector4.magnitude;
			if (num4 < magnitude2)
			{
				num4 = magnitude2;
			}
		}
		return num4;
	}

	public int AliveRingCount()
	{
		Vector3[] array = new Vector3[rn];
		bool[] array2 = new bool[rn];
		if (SampleFrame(EventDuration, array, array2))
		{
			int num = 0;
			for (int i = 0; i < array2.Length; i++)
			{
				if (array2[i])
				{
					num++;
				}
			}
			return num;
		}
		return 0;
	}

	public float RingRestCoord(int i)
	{
		return aRing0[i];
	}

	public Vector3 PlanePointAt(float t)
	{
		if (freeFlight && steps != null && steps.Count > 0)
		{
			float item = StepsState(t).Item1;
			return new Vector3(planeP0.x, planeP0.y, planeP0.z + item);
		}
		return planeP0;
	}
}
