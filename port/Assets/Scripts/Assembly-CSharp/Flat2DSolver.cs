using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class Flat2DSolver : IDisposable
{
	public enum TwoBodyMode
	{
		B = 0,
		APlus = 1
	}

	public enum ArtActivation
	{
		Tension = 0,
		Clumping = 1
	}

	public struct Diag
	{
		public double px;

		public double py;

		public double kinetic;

		public double internalE;

		public double hourglass;

		public double plasticWork;

		public double dt;

		public double time;

		public double vmax;

		public double cmax;

		public double hourglassAllPairs;

		public double Total => internalE + kinetic;

		public double Momentum => Math.Sqrt(py * py + px * px);
	}

	public const int MaxUavPerKernel = 8;

	public const float HOverDx = 1.3f;

	public float CFL;

	public float AlphaAV;

	public float BetaAV;

	public bool ArtificialViscosity;

	public bool Plasticity;

	public float TaylorQuinney;

	public bool MutIgnoreMaterialBuffer;

	public bool MutAsymmetricMomentum;

	public bool MutStiffnessX2;

	public bool MutMaterialIndexZero;

	public bool MutNoHeating;

	public bool DensitySummation;

	public float PinY;

	public float FixedTimeStep;

	public bool NoInteraction;

	public int QuadIntervals;

	public bool EnergyFirstOrder;

	public float MutRhoRateScale;

	public bool MutViscSignFlip;

	public bool MutDegenerateSplit;

	public float MutKickDtScale;

	public bool SplitPressureViscosity;

	public bool MutSplitDegenerate;

	public bool MutSplitMoveBeta;

	public bool MutSplitStoreLeak;

	public bool MutSplitDropEps;

	public bool MutSplitPlusStale;

	public bool InterfaceProbe;

	public bool MutIfDropEps;

	public bool MutIfAllCross;

	public bool MutIfFlipSign;

	public bool MutIfChanDropVisc;

	public bool MutIfNearOnly;

	public bool MutIfDropArt;

	private int ifSize;

	public bool SeparateBodies;

	public bool UnweldProbe;

	public bool MutUnweldKeepTension;

	public bool MutUnweldSkipRates;

	public bool MutUnweldLateRates;

	public bool MutUnweldDropEps;

	public bool MutUnweldDropArt;

	public bool MutUnweldNoContact;

	public bool MutContactLegacyStiffness;

	public float ContactStiffnessScale;

	public const float ContactStiffnessScaleMax = 3.4f;

	public TwoBodyMode SeparateBodiesMode;

	public float APlusTractionBand;

	public bool MutAPlusMaskNever;

	public bool MutAPlusMaskAlways;

	private int tbSize;

	private int uwSize;

	public bool Contact;

	public const float FrictionMu = 0f;

	public uint[] BodyIds;

	public bool[] AnchorMask;

	public bool Spall;

	public bool Damage;

	public bool Erosion;

	public float ErodeEpsP;

	public float ErodeShare;

	public uint ErodeSeed;

	public bool Fracture;

	public float[] Xi;

	public Vector2[] AsbPerMaterial;

	public bool Restraint;

	public float RestraintWidthM;

	public bool[] RestraintMask;

	public bool ArtificialStress;

	public bool MutArtStressOldForm;

	public float ArtStressEps;

	public float ArtStressN;

	public ArtActivation ArtStressActivation;

	public float ClumpTau;

	public bool ClumpForceAll;

	public bool MutClumpSkipCoincident;

	public bool AccumulateWork;

	public bool DeterministicOrder;

	private Flat2DMaterialBlock[] matsCPU;

	private readonly ComputeShader cs;

	private ComputeBuffer bPos;

	private ComputeBuffer bVelHalf;

	private ComputeBuffer bVelFull;

	private ComputeBuffer bAcc;

	private ComputeBuffer bMass;

	private ComputeBuffer bRho;

	private ComputeBuffer bRhoRate;

	private ComputeBuffer bEInt;

	private ComputeBuffer bTemp;

	private ComputeBuffer bEpsP;

	private ComputeBuffer bPlWork;

	private ComputeBuffer bEdotP;

	private ComputeBuffer bEdotS;

	private ComputeBuffer bEShear;

	private ComputeBuffer bEdotVisc;

	private ComputeBuffer bViscMin;

	private ComputeBuffer bWorkP;

	private ComputeBuffer bWorkS;

	private ComputeBuffer bAccPS;

	private ComputeBuffer bAccC;

	private ComputeBuffer bChanP;

	private ComputeBuffer bChanS;

	private ComputeBuffer bChanT;

	private ComputeBuffer bChanC;

	private ComputeBuffer bDamage;

	private ComputeBuffer bEDamage;

	private ComputeBuffer bEpsPPrev;

	private ComputeBuffer bAccA;

	private ComputeBuffer bChanA;

	private ComputeBuffer bSpallFail;

	private ComputeBuffer bPartialCon;

	private ComputeBuffer bBody;

	private ComputeBuffer bAnchor;

	private ComputeBuffer bStress;

	private ComputeBuffer bStressRate;

	private ComputeBuffer bVelGrad;

	private ComputeBuffer bMat;

	private ComputeBuffer bCellId;

	private ComputeBuffer bCellCount;

	private ComputeBuffer bCellStart;

	private ComputeBuffer bCellFill;

	private ComputeBuffer bBlockSums;

	private ComputeBuffer bOrder;

	private ComputeBuffer bMaterials;

	private ComputeBuffer bPartial;

	private ComputeBuffer bPartialC;

	private ComputeBuffer bPartial4;

	private ComputeBuffer bSim;

	private ComputeBuffer bProbe;

	private ComputeBuffer bClumpInfo;

	private ComputeBuffer bAccPV;

	private ComputeBuffer bPowPV;

	private ComputeBuffer bChanPr;

	private ComputeBuffer bChanV;

	private ComputeBuffer bHeatV;

	private ComputeBuffer bPowAccPr;

	private ComputeBuffer bPowAccV;

	private ComputeBuffer bHalfPr;

	private ComputeBuffer bHalfV;

	private ComputeBuffer bPowAccPrPlus;

	private ComputeBuffer bIfAcc;

	private ComputeBuffer bIfChanPS;

	private ComputeBuffer bIfChanVA;

	private ComputeBuffer bIfCount;

	private ComputeBuffer bIfColour;

	private ComputeBuffer bIfBand;

	private ComputeBuffer bIfProbe;

	private ComputeBuffer bCrossF;

	private ComputeBuffer bCrossV;

	private ComputeBuffer bTbProbe;

	private ComputeBuffer bUwMask;

	private ComputeBuffer bUwFA;

	private ComputeBuffer bUwFB;

	private ComputeBuffer bUwFE;

	private ComputeBuffer bUwFL1;

	private ComputeBuffer bUwFL1E;

	private ComputeBuffer bUwFSnap;

	private ComputeBuffer bUwVL;

	private ComputeBuffer bUwVRho;

	private ComputeBuffer bUwVL1;

	private ComputeBuffer bUwVS;

	private ComputeBuffer bUwVS2;

	private int kHash;

	private int kClear;

	private int kCount;

	private int kScanB;

	private int kScanS;

	private int kScanA;

	private int kScatter;

	private int kForces;

	private int kRates;

	private int kKick;

	private int kDrift;

	private int kState;

	private int kRedStep;

	private int kStep;

	private int kDiag;

	private int kHg;

	private int kPower;

	private int kRhoSum;

	private int kParts;

	private int kClosure;

	private int kStateE;

	private int kQuad;

	private int kPremise;

	private int kAccW;

	private int kRedW;

	private int kSortCells;

	private int kRedChan;

	private int kDamage;

	private int kRedDmg;

	private int kContact;

	private int kAnchor;

	private int kClump;

	private int kArtMasked;

	private int kSplitPV;

	private int kAccumPV;

	private int kInterface;

	private int kCrossFlagF;

	private int kUnweldF;

	private int kUnweldCloseF;

	private int kContactPair;

	private int kCrossFlagV;

	private int kUnweldV;

	private int kUnweldCloseV;

	private int kHgB;

	private int kStateEB;

	private int kMaskA;

	private int kCountE;

	private int kScatterE;

	private int kErodeMark;

	private int kErodeApply;

	private int kRedErode;

	private int kStateF;

	private int kDamageF;

	private int kRestrain;

	private int kRedRestrain;

	private ComputeBuffer bRestrainX0;

	private ComputeBuffer bRestrainWork;

	private ComputeBuffer bRestrainMask;

	private ComputeBuffer bRestrainProbe;

	private int rsSize;

	private bool rsAtInit;

	private ComputeBuffer bEroded;

	private ComputeBuffer bErodeLedger;

	private ComputeBuffer bErodeProbe;

	private ComputeBuffer bXi;

	private ComputeBuffer bAsb;

	private int erSize;

	private int frSize;

	private bool erAtInit;

	private bool frAtInit;

	private int nBlocks;

	private int cellBlocks;

	private int numCells;

	private int gridW;

	private int gridH;

	private float originX;

	private float originY;

	private float cellSize;

	private float cMax0;

	private static readonly Dictionary<string, int> uavCounts = new Dictionary<string, int>();

	private static Flat2DSolver boundOwner;

	private bool ClumpMode => ArtificialStress && ArtStressActivation == ArtActivation.Clumping;

	public int N { get; private set; }

	public float Dx { get; private set; }

	public float H { get; private set; }

	public static IReadOnlyDictionary<string, int> UavCounts => uavCounts;

	public int ErodedCount
	{
		get
		{
			if (!Erosion)
			{
				return 0;
			}
			uint[] arr = new uint[16];
			bErodeProbe.GetData(arr);
			return (int)arr[1];
		}
	}

	public ComputeBuffer PosBuffer => bPos;

	public ComputeBuffer VelBuffer => bVelFull;

	public ComputeBuffer StressBuffer => bStress;

	public ComputeBuffer DamageBuffer => bDamage;

	public ComputeBuffer TemperatureBuffer => bTemp;

	public ComputeBuffer FailBuffer => bSpallFail;

	public ComputeBuffer MatBuffer => bMat;

	public float SoundSpeedInitial => cMax0;

	public Flat2DSolver(ComputeShader shader)
	{
		CFL = 0.3f;
		AlphaAV = 1f;
		BetaAV = 2f;
		ArtificialViscosity = true;
		Plasticity = true;
		TaylorQuinney = 0.9f;
		PinY = float.NegativeInfinity;
		QuadIntervals = 64;
		MutRhoRateScale = 1f;
		MutKickDtScale = 1f;
		ContactStiffnessScale = 1f;
		APlusTractionBand = 1000000f;
		Spall = true;
		Damage = true;
		ErodeEpsP = 2f;
		ErodeShare = 0.8f;
		ArtStressEps = 0.2f;
		ArtStressN = 4f;
		ClumpTau = 0.84f;
		AccumulateWork = true;
		if (shader == null)
		{
			throw new ArgumentNullException("shader");
		}
		cs = shader;
		kHash = cs.FindKernel("KHash");
		kClear = cs.FindKernel("KClearCells");
		kCount = cs.FindKernel("KCount");
		kScanB = cs.FindKernel("KScanBlocks");
		kScanS = cs.FindKernel("KScanBlockSums");
		kScanA = cs.FindKernel("KScanApply");
		kScatter = cs.FindKernel("KScatterIdx");
		kSortCells = cs.FindKernel("KSortCells");
		kForces = cs.FindKernel("KForces");
		kRates = cs.FindKernel("KRates");
		kKick = cs.FindKernel("KKick");
		kDrift = cs.FindKernel("KDrift");
		kState = cs.FindKernel("KUpdateState");
		kRedStep = cs.FindKernel("KReduceStep");
		kStep = cs.FindKernel("KTimeStep");
		kDiag = cs.FindKernel("KReduceDiag");
		kHg = cs.FindKernel("KHourglass");
		kPower = cs.FindKernel("KReducePower");
		kRhoSum = cs.FindKernel("KDensitySum");
		kParts = cs.FindKernel("KReduceParts");
		kClosure = cs.FindKernel("KReduceClosure");
		kStateE = cs.FindKernel("KStateEnergy");
		kQuad = cs.FindKernel("KQuadCheck");
		kPremise = cs.FindKernel("KPremise");
		kAccW = cs.FindKernel("KAccumWork");
		kRedW = cs.FindKernel("KReduceWork");
		kRedChan = cs.FindKernel("KReduceChan");
		kDamage = cs.FindKernel("KDamage");
		kContact = cs.FindKernel("KContact");
		kAnchor = cs.FindKernel("KAnchor");
		kRedDmg = cs.FindKernel("KReduceDamage");
		kClump = cs.FindKernel("KClump");
		kArtMasked = cs.FindKernel("KArtMasked");
		kSplitPV = cs.FindKernel("KSplitPV");
		kAccumPV = cs.FindKernel("KAccumPV");
		kInterface = cs.FindKernel("KInterface");
		kCrossFlagF = cs.FindKernel("KCrossFlagF");
		kUnweldF = cs.FindKernel("KUnweldF");
		kUnweldCloseF = cs.FindKernel("KUnweldCloseF");
		kContactPair = cs.FindKernel("KContactPair");
		kCrossFlagV = cs.FindKernel("KCrossFlagV");
		kUnweldV = cs.FindKernel("KUnweldV");
		kUnweldCloseV = cs.FindKernel("KUnweldCloseV");
		kHgB = cs.FindKernel("KHourglassB");
		kStateEB = cs.FindKernel("KStateEnergyB");
		kMaskA = cs.FindKernel("KMaskA");
		kCountE = cs.FindKernel("KCountE");
		kScatterE = cs.FindKernel("KScatterIdxE");
		kErodeMark = cs.FindKernel("KErodeMark");
		kErodeApply = cs.FindKernel("KErodeApply");
		kRedErode = cs.FindKernel("KReduceErode");
		kStateF = cs.FindKernel("KUpdateStateF");
		kDamageF = cs.FindKernel("KDamageF");
		kRestrain = cs.FindKernel("KRestrain");
		kRedRestrain = cs.FindKernel("KReduceRestrain");
	}

	public void Init(Vector2[] pos, Vector2[] vel, uint[] mat, Flat2DMaterialBlock[] materials, float dx, Vector2 domainMin, Vector2 domainMax)
	{
		if (SplitPressureViscosity && MutDegenerateSplit)
		{
			throw new InvalidOperationException("Flat2D: SplitPressureViscosity несовместим с MutDegenerateSplit (прибор деления «давление | Π» не повторяет перенос девиатора в acc_p)");
		}
		GuardTwoBodies(pos.Length, afterInit: false);
		GuardErosionFracture(pos.Length, materials.Length, afterInit: false);
		Dispose();
		Dx = dx;
		N = pos.Length;
		matsCPU = materials;
		H = dx * HOverDx;
		originX = domainMin.x;
		originY = domainMin.y;
		cellSize = H + H;
		gridW = (int)Math.Ceiling((domainMax.x - domainMin.x) / cellSize);
		if (gridW < 1)
		{
			gridW = 1;
		}
		gridH = (int)Math.Ceiling((domainMax.y - domainMin.y) / cellSize);
		if (gridH < 1)
		{
			gridH = 1;
		}
		nBlocks = (N + 255) / 256;
		numCells = gridW * gridH;
		cellBlocks = (numCells + 255) / 256;
		bPos = new ComputeBuffer(N, 8);
		bPos.SetData(pos);
		bVelHalf = new ComputeBuffer(N, 8);
		bVelHalf.SetData(vel);
		bVelFull = new ComputeBuffer(N, 8);
		bVelFull.SetData(vel);
		bAcc = new ComputeBuffer(N, 8);
		bEdotP = new ComputeBuffer(N, 4);
		bEdotS = new ComputeBuffer(N, 4);
		bEShear = new ComputeBuffer(N, 4);
		bEdotVisc = new ComputeBuffer(N, 4);
		bViscMin = new ComputeBuffer(N, 4);
		bWorkP = new ComputeBuffer(N, 4);
		bAccPS = new ComputeBuffer(N, 16);
		bAccC = new ComputeBuffer(N, 8);
		bChanP = new ComputeBuffer(N, 4);
		bChanS = new ComputeBuffer(N, 4);
		bChanT = new ComputeBuffer(N, 4);
		bDamage = new ComputeBuffer(N, 4);
		bEDamage = new ComputeBuffer(N, 4);
		bEpsPPrev = new ComputeBuffer(N, 4);
		bAccA = new ComputeBuffer(N, 8);
		bChanA = new ComputeBuffer(N, 4);
		bSpallFail = new ComputeBuffer(N, 4);
		bChanC = new ComputeBuffer(N, 4);
		bBody = new ComputeBuffer(N, 4);
		bAnchor = new ComputeBuffer(N, 4);
		bPartialCon = new ComputeBuffer(nBlocks, 4);
		bWorkS = new ComputeBuffer(N, 4);
		bMass = new ComputeBuffer(N, 4);
		bRho = new ComputeBuffer(N, 4);
		bRhoRate = new ComputeBuffer(N, 4);
		bEInt = new ComputeBuffer(N, 4);
		bTemp = new ComputeBuffer(N, 4);
		bEpsP = new ComputeBuffer(N, 4);
		bPlWork = new ComputeBuffer(N, 4);
		bStress = new ComputeBuffer(N, 16);
		bStressRate = new ComputeBuffer(N, 16);
		bVelGrad = new ComputeBuffer(N, 16);
		bMat = new ComputeBuffer(N, 4);
		bMat.SetData(mat);
		bCellId = new ComputeBuffer(N, 4);
		bOrder = new ComputeBuffer(N, 4);
		bCellCount = new ComputeBuffer(numCells, 4);
		bCellStart = new ComputeBuffer(numCells, 4);
		bCellFill = new ComputeBuffer(numCells, 4);
		bBlockSums = new ComputeBuffer((cellBlocks > 16) ? cellBlocks : 16, 4);
		bMaterials = new ComputeBuffer(materials.Length, 96);
		bMaterials.SetData(materials);
		bPartial = new ComputeBuffer(nBlocks, 4);
		bPartialC = new ComputeBuffer(nBlocks, 4);
		bPartial4 = new ComputeBuffer(nBlocks, 16);
		bSim = new ComputeBuffer(8, 4);
		bSim.SetData(new float[8]);
		bProbe = new ComputeBuffer(8, 4);
		bProbe.SetData(new float[8]);
		bClumpInfo = new ComputeBuffer(N, 16);
		bClumpInfo.SetData(new Vector4[N]);
		bAccPV = new ComputeBuffer(N, 16);
		bAccPV.SetData(new Vector4[N]);
		bPowPV = new ComputeBuffer(N, 16);
		bPowPV.SetData(new Vector4[N]);
		bChanPr = new ComputeBuffer(N, 4);
		bChanPr.SetData(new float[N]);
		bChanV = new ComputeBuffer(N, 4);
		bChanV.SetData(new float[N]);
		bHeatV = new ComputeBuffer(N, 4);
		bHeatV.SetData(new float[N]);
		bPowAccPr = new ComputeBuffer(N, 4);
		bPowAccPr.SetData(new float[N]);
		bPowAccV = new ComputeBuffer(N, 4);
		bPowAccV.SetData(new float[N]);
		bHalfPr = new ComputeBuffer(N, 4);
		bHalfPr.SetData(new float[N]);
		bHalfV = new ComputeBuffer(N, 4);
		bHalfV.SetData(new float[N]);
		bPowAccPrPlus = new ComputeBuffer(N, 4);
		bPowAccPrPlus.SetData(new float[N]);
		ifSize = InterfaceProbe ? N : 1;
		bIfAcc = new ComputeBuffer(ifSize, 16);
		bIfAcc.SetData(new Vector4[ifSize]);
		bIfChanPS = new ComputeBuffer(ifSize, 16);
		bIfChanPS.SetData(new Vector4[ifSize]);
		bIfChanVA = new ComputeBuffer(ifSize, 16);
		bIfChanVA.SetData(new Vector4[ifSize]);
		bIfCount = new ComputeBuffer(ifSize, 16);
		bIfCount.SetData(new Vector4[ifSize]);
		bIfColour = new ComputeBuffer(ifSize, 16);
		bIfColour.SetData(new Vector4[ifSize]);
		bIfBand = new ComputeBuffer(ifSize, 16);
		bIfBand.SetData(new Vector4[ifSize]);
		bIfProbe = new ComputeBuffer(1, 4);
		bIfProbe.SetData(new float[1]);
		tbSize = SeparateBodies ? N : 1;
		uwSize = (SeparateBodies && UnweldProbe) ? N : 1;
		bCrossF = new ComputeBuffer(tbSize, 4);
		bCrossF.SetData(new uint[tbSize]);
		bCrossV = new ComputeBuffer(tbSize, 4);
		bCrossV.SetData(new uint[tbSize]);
		bTbProbe = new ComputeBuffer(16, 4);
		bTbProbe.SetData(new uint[16]);
		bUwMask = new ComputeBuffer(tbSize, 4);
		bUwMask.SetData(new uint[tbSize]);
		bUwFA = new ComputeBuffer(uwSize, 16);
		bUwFA.SetData(new Vector4[uwSize]);
		bUwFB = new ComputeBuffer(uwSize, 16);
		bUwFB.SetData(new Vector4[uwSize]);
		bUwFE = new ComputeBuffer(uwSize, 16);
		bUwFE.SetData(new Vector4[uwSize]);
		bUwFL1 = new ComputeBuffer(uwSize, 16);
		bUwFL1.SetData(new Vector4[uwSize]);
		bUwFL1E = new ComputeBuffer(uwSize, 16);
		bUwFL1E.SetData(new Vector4[uwSize]);
		bUwFSnap = new ComputeBuffer(uwSize, 16);
		bUwFSnap.SetData(new Vector4[uwSize]);
		bUwVL = new ComputeBuffer(uwSize, 16);
		bUwVL.SetData(new Vector4[uwSize]);
		bUwVRho = new ComputeBuffer(uwSize, 16);
		bUwVRho.SetData(new Vector4[uwSize]);
		bUwVL1 = new ComputeBuffer(uwSize, 16);
		bUwVL1.SetData(new Vector4[uwSize]);
		bUwVS = new ComputeBuffer(uwSize, 16);
		bUwVS.SetData(new Vector4[uwSize]);
		bUwVS2 = new ComputeBuffer(uwSize, 16);
		bUwVS2.SetData(new Vector4[uwSize]);
		erSize = Erosion ? N : 1;
		frSize = Fracture ? N : 1;
		erAtInit = Erosion;
		frAtInit = Fracture;
		bEroded = new ComputeBuffer(erSize, 4);
		bEroded.SetData(new uint[erSize]);
		bErodeLedger = new ComputeBuffer(erSize, 16);
		bErodeLedger.SetData(new Vector4[erSize]);
		bErodeProbe = new ComputeBuffer(16, 4);
		bErodeProbe.SetData(new uint[16]);
		float[] xiArr = new float[frSize];
		for (int i = 0; i < frSize; i++)
		{
			xiArr[i] = (Fracture && Xi != null && i < Xi.Length) ? Xi[i] : 1f;
		}
		bXi = new ComputeBuffer(frSize, 4);
		bXi.SetData(xiArr);
		Vector2[] asb = new Vector2[Math.Max(materials.Length, 1)];
		for (int i = 0; i < asb.Length; i++)
		{
			Vector2 v = (AsbPerMaterial != null && i < AsbPerMaterial.Length) ? AsbPerMaterial[i] : new Vector2(float.PositiveInfinity, 0f);
			if (float.IsInfinity(v.x) || float.IsNaN(v.x))
			{
				v.x = 1e+30f;
			}
			v.y = ((v.y > 0f) ? v.y : 0f);
			asb[i] = v;
		}
		bAsb = new ComputeBuffer(asb.Length, 8);
		bAsb.SetData(asb);
		rsSize = Restraint ? N : 1;
		rsAtInit = Restraint;
		Vector2[] x0 = new Vector2[rsSize];
		float[] maskArr = new float[rsSize];
		if (Restraint)
		{
			for (int i = 0; i < N; i++)
			{
				x0[i] = pos[i];
				maskArr[i] = (RestraintMask[i] ? 1f : 0f);
			}
		}
		bRestrainX0 = new ComputeBuffer(rsSize, 8);
		bRestrainX0.SetData(x0);
		bRestrainWork = new ComputeBuffer(rsSize, 4);
		bRestrainWork.SetData(new float[rsSize]);
		bRestrainMask = new ComputeBuffer(rsSize, 4);
		bRestrainMask.SetData(maskArr);
		bRestrainProbe = new ComputeBuffer(4, 4);
		bRestrainProbe.SetData(new uint[4]);
		float[] mass = new float[N];
		float[] rho = new float[N];
		float[] temp = new float[N];
		float[] zeros = new float[N];
		Vector4[] stress = new Vector4[N];
		cMax0 = 0f;
		for (int i = 0; i < N; i++)
		{
			Flat2DMaterialBlock block = materials[mat[i]];
			mass[i] = block.rho0 * dx * dx;
			rho[i] = block.rho0;
			temp[i] = block.tRoom;
			cMax0 = Mathf.Max(cMax0, block.LongitudinalWaveSpeed);
		}
		bMass.SetData(mass);
		bRho.SetData(rho);
		bTemp.SetData(temp);
		bEInt.SetData(zeros);
		bEpsP.SetData(zeros);
		bPlWork.SetData(zeros);
		bEShear.SetData(zeros);
		bEdotP.SetData(zeros);
		bEdotS.SetData(zeros);
		bEdotVisc.SetData(zeros);
		float[] viscMin = new float[N];
		for (int i = 0; i < N; i++)
		{
			viscMin[i] = 1e+30f;
		}
		bViscMin.SetData(viscMin);
		bWorkP.SetData(zeros);
		bWorkS.SetData(zeros);
		bChanP.SetData(zeros);
		bChanS.SetData(zeros);
		bChanT.SetData(zeros);
		bDamage.SetData(zeros);
		bEDamage.SetData(zeros);
		bEpsPPrev.SetData(zeros);
		bChanA.SetData(zeros);
		bSpallFail.SetData(zeros);
		bChanC.SetData(zeros);
		uint[] bodyArr = new uint[N];
		if (BodyIds != null)
		{
			Array.Copy(BodyIds, bodyArr, Math.Min(N, BodyIds.Length));
		}
		bBody.SetData(bodyArr);
		float[] anchorArr = new float[N];
		if (AnchorMask != null)
		{
			for (int i = 0; i < N; i++)
			{
				anchorArr[i] = (AnchorMask[i] ? 1f : 0f);
			}
		}
		bAnchor.SetData(anchorArr);
		Vector2[] accZero = new Vector2[N];
		bAccPS.SetData(new Vector4[N]);
		bAccA.SetData(accZero);
		bAccC.SetData(accZero);
		bStress.SetData(stress);
		bStressRate.SetData(stress);
		bVelGrad.SetData(stress);
		float dt;
		if (FixedTimeStep > 0f)
		{
			dt = FixedTimeStep;
		}
		else
		{
			dt = H * CFL / Mathf.Max(cMax0 + MaxSpeed(vel), 1e-06f);
		}
		float[] sim = new float[8];
		sim[0] = dt;
		sim[3] = cMax0;
		sim[4] = dt;
		bSim.SetData(sim);
		BindAll();
		PushUniforms();
		boundOwner = this;
		Sort();
		if (!NoInteraction)
		{
			cs.Dispatch(kForces, nBlocks, 1, 1);
			if (SeparateBodies)
			{
				cs.Dispatch(kCrossFlagF, nBlocks, 1, 1);
				if (SeparateBodiesMode == TwoBodyMode.APlus)
				{
					cs.Dispatch(kMaskA, nBlocks, 1, 1);
				}
				cs.Dispatch(kUnweldF, nBlocks, 1, 1);
				if (SeparateBodiesMode != TwoBodyMode.APlus && !MutUnweldNoContact)
				{
					cs.Dispatch(kContactPair, nBlocks, 1, 1);
				}
			}
			if (ArtificialStress && ArtStressActivation == ArtActivation.Clumping)
			{
				cs.Dispatch(kClump, nBlocks, 1, 1);
				cs.Dispatch(kArtMasked, nBlocks, 1, 1);
			}
		}
		else
		{
			bAcc.SetData(new Vector2[N]);
			bEdotP.SetData(new float[N]);
			bEdotS.SetData(new float[N]);
			bRhoRate.SetData(new float[N]);
			bStressRate.SetData(new Vector4[N]);
		}
		float[] simHalf = new float[8];
		simHalf[0] = dt * -0.5f;
		simHalf[3] = cMax0;
		simHalf[4] = dt * -0.5f;
		bSim.SetData(simHalf);
		cs.Dispatch(kKick, nBlocks, 1, 1);
		float[] simFull = new float[8];
		simFull[0] = dt;
		simFull[3] = cMax0;
		simFull[4] = dt;
		bSim.SetData(simFull);
	}

	private static float MaxSpeed(Vector2[] v)
	{
		float max = 0f;
		for (int i = 0; i < v.Length; i++)
		{
			max = Mathf.Max(max, (float)Math.Sqrt((double)(v[i].x * v[i].x + v[i].y * v[i].y)));
		}
		return max;
	}

	private void BindKernel(int kernel, string name, params (string slot, ComputeBuffer buf)[] binds)
	{
		HashSet<ComputeBuffer> seen = new HashSet<ComputeBuffer>();
		int count = 0;
		foreach (var bind in binds)
		{
			string slot = bind.slot;
			ComputeBuffer buf = bind.buf;
			bool counted = !slot.EndsWith("R") && slot != "_Materials";
			bool added = seen.Add(buf);
			if (counted && !added)
			{
				throw new InvalidOperationException(string.Concat("Flat2D: ядро ", name, " привязывает один буфер дважды (слот ", slot, ") — SRV и UAV на один ресурс в одном диспетче недопустимы"));
			}
			cs.SetBuffer(kernel, slot, buf);
			if (counted)
			{
				count++;
			}
		}
		uavCounts[name] = count;
		if (count > MaxUavPerKernel)
		{
			throw new InvalidOperationException(string.Format("Flat2D: ядро {0} требует {1} UAV при пределе {2} (D3D11.0). ", name, count, MaxUavPerKernel) + "Unity пропустит диспетч МОЛЧА — разделите ядро или переведите чтение в SRV.");
		}
	}

	private void BindAll()
	{
		BindKernel(kHash, "KHash", ("_PosR", bPos), ("_CellId", bCellId));
		BindKernel(kClear, "KClearCells", ("_CellCount", bCellCount), ("_CellFill", bCellFill));
		BindKernel(kCount, "KCount", ("_CellIdR", bCellId), ("_CellCount", bCellCount));
		BindKernel(kScanB, "KScanBlocks", ("_CellCountR", bCellCount), ("_CellStart", bCellStart), ("_BlockSums", bBlockSums));
		BindKernel(kScanS, "KScanBlockSums", ("_BlockSums", bBlockSums));
		BindKernel(kScanA, "KScanApply", ("_CellStart", bCellStart), ("_BlockSumsR", bBlockSums));
		BindKernel(kSortCells, "KSortCells", ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_Order", bOrder));
		BindKernel(kScatter, "KScatterIdx", ("_CellIdR", bCellId), ("_CellStartR", bCellStart), ("_CellFill", bCellFill), ("_Order", bOrder));
		BindKernel(kForces, "KForces", ("_PosR", bPos), ("_VelHalfR", bVelHalf), ("_MassR", bMass), ("_RhoR", bRho), ("_StressR", bStress), ("_EIntR", bEInt), ("_MatR", bMat), ("_OrderR", bOrder), ("_BodyR", bBody), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_Materials", bMaterials), ("_Acc", bAcc), ("_EdotP", bEdotP), ("_EdotS", bEdotS), ("_EdotVisc", bEdotVisc), ("_ViscMin", bViscMin), ("_AccPS", bAccPS), ("_AccA", bAccA), ("_AccC", bAccC));
		BindKernel(kRates, "KRates", ("_PosR", bPos), ("_VelHalfR", bVelHalf), ("_MassR", bMass), ("_RhoR", bRho), ("_StressR", bStress), ("_MatR", bMat), ("_OrderR", bOrder), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_Materials", bMaterials), ("_VelGrad", bVelGrad), ("_RhoRate", bRhoRate), ("_StressRate", bStressRate));
		BindKernel(kKick, "KKick", ("_AccR", bAcc), ("_SimR", bSim), ("_PosR", bPos), ("_VelHalf", bVelHalf), ("_VelFull", bVelFull), ("_Probe", bProbe));
		BindKernel(kAnchor, "KAnchor", ("_AnchorR", bAnchor), ("_VelHalf", bVelHalf), ("_VelFull", bVelFull));
		BindKernel(kDrift, "KDrift", ("_VelHalfR", bVelHalf), ("_SimR", bSim), ("_Pos", bPos));
		BindKernel(kState, "KUpdateState", ("_RhoRateR", bRhoRate), ("_StressRateR", bStressRate), ("_VelGradR", bVelGrad), ("_MatR", bMat), ("_Materials", bMaterials), ("_SimR", bSim), ("_EdotPR", bEdotP), ("_Rho", bRho), ("_Stress", bStress), ("_EInt", bEInt), ("_Temp", bTemp), ("_EpsP", bEpsP), ("_PlWork", bPlWork), ("_EShear", bEShear), ("_EdotSR", bEdotS), ("_Probe", bProbe), ("_EdotViscR", bEdotVisc));
		BindKernel(kRedStep, "KReduceStep", ("_VelHalfR", bVelHalf), ("_RhoR", bRho), ("_EIntR", bEInt), ("_MatR", bMat), ("_Materials", bMaterials), ("_Partial", bPartial), ("_PartialC", bPartialC));
		BindKernel(kStep, "KTimeStep", ("_PartialR", bPartial), ("_PartialCR", bPartialC), ("_Sim", bSim));
		BindKernel(kDiag, "KReduceDiag", ("_MassR", bMass), ("_VelFullR", bVelFull), ("_EIntR", bEInt), ("_RhoR", bRho), ("_StressR", bStress), ("_MatR", bMat), ("_Materials", bMaterials), ("_VelHalfR", bVelHalf), ("_EDamageR", bEDamage), ("_Partial4", bPartial4));
		BindKernel(kPower, "KReducePower", ("_MassR", bMass), ("_VelFullR", bVelFull), ("_VelHalfR", bVelHalf), ("_AccR", bAcc), ("_Partial4", bPartial4));
		BindKernel(kParts, "KReduceParts", ("_MassR", bMass), ("_RhoR", bRho), ("_EdotPR", bEdotP), ("_EdotSR", bEdotS), ("_EIntR", bEInt), ("_StressR", bStress), ("_MatR", bMat), ("_Materials", bMaterials), ("_Partial4", bPartial4));
		BindKernel(kClosure, "KReduceClosure", ("_MassR", bMass), ("_VelHalfR", bVelHalf), ("_VelFullR", bVelFull), ("_AccR", bAcc), ("_Partial4", bPartial4));
		BindKernel(kStateE, "KStateEnergy", ("_PosR", bPos), ("_MassR", bMass), ("_RhoR", bRho), ("_EIntR", bEInt), ("_MatR", bMat), ("_OrderR", bOrder), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_Materials", bMaterials), ("_Partial4", bPartial4));
		BindKernel(kQuad, "KQuadCheck", ("_Materials", bMaterials), ("_Probe", bProbe));
		BindKernel(kAccW, "KAccumWork", ("_EdotSR", bEdotS), ("_SimR", bSim), ("_RhoR", bRho), ("_RhoRateR", bRhoRate), ("_EIntR", bEInt), ("_MatR", bMat), ("_Materials", bMaterials), ("_VelFullR", bVelFull), ("_AccPSR", bAccPS), ("_AccR", bAcc), ("_AccAR", bAccA), ("_AccCR", bAccC), ("_WorkP", bWorkP), ("_WorkS", bWorkS), ("_WorkChanP", bChanP), ("_WorkChanS", bChanS), ("_WorkChanT", bChanT), ("_WorkChanA", bChanA), ("_WorkChanC", bChanC));
		BindKernel(kContact, "KContact", ("_PosR", bPos), ("_OrderR", bOrder), ("_BodyR", bBody), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_Acc", bAcc), ("_AccC", bAccC));
		BindKernel(kClump, "KClump", ("_PosR", bPos), ("_OrderR", bOrder), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_StressR", bStress), ("_ClumpInfo", bClumpInfo));
		BindKernel(kArtMasked, "KArtMasked", ("_PosR", bPos), ("_RhoR", bRho), ("_StressR", bStress), ("_MassR", bMass), ("_OrderR", bOrder), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_ClumpInfoR", bClumpInfo), ("_AccA", bAccA), ("_Acc", bAcc));
		BindKernel(kSplitPV, "KSplitPV", ("_PosR", bPos), ("_VelHalfR", bVelHalf), ("_MassR", bMass), ("_RhoR", bRho), ("_StressR", bStress), ("_EIntR", bEInt), ("_MatR", bMat), ("_OrderR", bOrder), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_Materials", bMaterials), ("_AccPV", bAccPV), ("_PowPV", bPowPV));
		BindKernel(kAccumPV, "KAccumPV", ("_SimR", bSim), ("_VelFullR", bVelFull), ("_AccPVR", bAccPV), ("_EdotViscR", bEdotVisc), ("_PowPVR", bPowPV), ("_AccR", bAcc), ("_MassR", bMass), ("_AnchorR", bAnchor), ("_WorkChanPr", bChanPr), ("_WorkChanV", bChanV), ("_HeatV", bHeatV), ("_PowAccPr", bPowAccPr), ("_PowAccV", bPowAccV), ("_HalfPr", bHalfPr), ("_HalfV", bHalfV), ("_VelHalfPlusR", bVelHalf), ("_PowAccPrPlus", bPowAccPrPlus));
		BindKernel(kInterface, "KInterface", ("_PosR", bPos), ("_VelHalfR", bVelHalf), ("_MassR", bMass), ("_RhoR", bRho), ("_StressR", bStress), ("_EIntR", bEInt), ("_MatR", bMat), ("_OrderR", bOrder), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_Materials", bMaterials), ("_IfAcc", bIfAcc), ("_IfChanPS", bIfChanPS), ("_IfChanVA", bIfChanVA), ("_IfCount", bIfCount), ("_IfColour", bIfColour), ("_IfBand", bIfBand), ("_IfProbe", bIfProbe));
		BindKernel(kDamage, "KDamage", ("_EpsPR", bEpsP), ("_VelGradR", bVelGrad), ("_TempR", bTemp), ("_RhoR", bRho), ("_MatR", bMat), ("_Materials", bMaterials), ("_Stress", bStress), ("_Damage", bDamage), ("_EDamage", bEDamage), ("_EpsPPrev", bEpsPPrev), ("_SpallFail", bSpallFail));
		BindKernel(kCountE, "KCountE", ("_CellIdR", bCellId), ("_ErodedR", bEroded), ("_CellCount", bCellCount));
		BindKernel(kScatterE, "KScatterIdxE", ("_CellIdR", bCellId), ("_CellStartR", bCellStart), ("_ErodedR", bEroded), ("_CellFill", bCellFill), ("_Order", bOrder));
		BindKernel(kStateF, "KUpdateStateF", ("_RhoRateR", bRhoRate), ("_StressRateR", bStressRate), ("_VelGradR", bVelGrad), ("_MatR", bMat), ("_Materials", bMaterials), ("_SimR", bSim), ("_EdotPR", bEdotP), ("_Rho", bRho), ("_Stress", bStress), ("_EInt", bEInt), ("_Temp", bTemp), ("_EpsP", bEpsP), ("_PlWork", bPlWork), ("_EShear", bEShear), ("_EdotSR", bEdotS), ("_Probe", bProbe), ("_EdotViscR", bEdotVisc), ("_AsbR", bAsb), ("_XiR", bXi), ("_SpallFailR", bSpallFail));
		BindKernel(kDamageF, "KDamageF", ("_EpsPR", bEpsP), ("_VelGradR", bVelGrad), ("_TempR", bTemp), ("_RhoR", bRho), ("_MatR", bMat), ("_Materials", bMaterials), ("_XiR", bXi), ("_AsbR", bAsb), ("_Stress", bStress), ("_Damage", bDamage), ("_EDamage", bEDamage), ("_EpsPPrev", bEpsPPrev), ("_SpallFail", bSpallFail));
		BindKernel(kErodeMark, "KErodeMark", ("_EpsPR", bEpsP), ("_AnchorR", bAnchor), ("_MatR", bMat), ("_Materials", bMaterials), ("_MassR", bMass), ("_VelHalfR", bVelHalf), ("_VelFullR", bVelFull), ("_StressR", bStress), ("_RhoR", bRho), ("_EIntR", bEInt), ("_Eroded", bEroded), ("_ErodeLedger", bErodeLedger), ("_ErodeProbe", bErodeProbe));
		BindKernel(kErodeApply, "KErodeApply", ("_MatR", bMat), ("_Materials", bMaterials), ("_Eroded", bEroded), ("_Pos", bPos), ("_VelHalf", bVelHalf), ("_VelFull", bVelFull), ("_Rho", bRho), ("_EInt", bEInt), ("_Stress", bStress), ("_ErodeProbe", bErodeProbe));
		BindKernel(kRedErode, "KReduceErode", ("_ErodedR", bEroded), ("_ErodeLedgerR", bErodeLedger), ("_Partial4", bPartial4));
		BindKernel(kRestrain, "KRestrain", ("_PosR", bPos), ("_RhoR", bRho), ("_EpsPR", bEpsP), ("_TempR", bTemp), ("_MassR", bMass), ("_MatR", bMat), ("_Materials", bMaterials), ("_SpallFailR", bSpallFail), ("_DamageR", bDamage), ("_RestrainMaskR", bRestrainMask), ("_ErodedR", bEroded), ("_Acc", bAcc), ("_RestrainX0", bRestrainX0), ("_RestrainWork", bRestrainWork), ("_RestrainProbe", bRestrainProbe));
		BindKernel(kRedRestrain, "KReduceRestrain", ("_RestrainWorkR", bRestrainWork), ("_Partial4", bPartial4));
		BindKernel(kRedDmg, "KReduceDamage", ("_MassR", bMass), ("_DamageR", bDamage), ("_EDamageR", bEDamage), ("_Partial4", bPartial4));
		BindKernel(kRedChan, "KReduceChan", ("_MassR", bMass), ("_WorkChanPR", bChanP), ("_WorkChanSR", bChanS), ("_WorkChanTR", bChanT), ("_WorkChanAR", bChanA), ("_WorkChanCR", bChanC), ("_Partial4", bPartial4), ("_PartialCon", bPartialCon));
		BindKernel(kRedW, "KReduceWork", ("_MassR", bMass), ("_WorkPR", bWorkP), ("_WorkSR", bWorkS), ("_MatR", bMat), ("_Materials", bMaterials), ("_StressR", bStress), ("_RhoR", bRho), ("_VelFullR", bVelFull), ("_Partial4", bPartial4));
		BindKernel(kPremise, "KPremise", ("_RhoR", bRho), ("_EIntR", bEInt), ("_MatR", bMat), ("_Materials", bMaterials), ("_Partial4", bPartial4));
		BindKernel(kRhoSum, "KDensitySum", ("_PosR", bPos), ("_MassR", bMass), ("_OrderR", bOrder), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_Rho", bRho));
		BindKernel(kHg, "KHourglass", ("_PosR", bPos), ("_VelHalfR", bVelHalf), ("_MassR", bMass), ("_RhoR", bRho), ("_VelGradR", bVelGrad), ("_PlWorkR", bPlWork), ("_OrderR", bOrder), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_Partial4", bPartial4));
		BindKernel(kCrossFlagF, "KCrossFlagF", ("_PosR", bPos), ("_BodyR", bBody), ("_OrderR", bOrder), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_CrossPairsF", bCrossF), ("_TbProbe", bTbProbe));
		BindKernel(kUnweldF, "KUnweldF", ("_PosR", bPos), ("_VelHalfR", bVelHalf), ("_MassR", bMass), ("_RhoR", bRho), ("_StressR", bStress), ("_EIntR", bEInt), ("_MatR", bMat), ("_OrderR", bOrder), ("_BodyR", bBody), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_CrossPairsFR", bCrossF), ("_Materials", bMaterials), ("_UwMaskR", bUwMask), ("_Acc", bAcc), ("_AccPS", bAccPS), ("_AccA", bAccA), ("_EdotP", bEdotP), ("_EdotS", bEdotS), ("_EdotVisc", bEdotVisc), ("_ViscMin", bViscMin), ("_TbProbe", bTbProbe));
		BindKernel(kUnweldCloseF, "KUnweldCloseF", ("_PosR", bPos), ("_VelHalfR", bVelHalf), ("_MassR", bMass), ("_RhoR", bRho), ("_StressR", bStress), ("_EIntR", bEInt), ("_MatR", bMat), ("_OrderR", bOrder), ("_BodyR", bBody), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_Materials", bMaterials), ("_AccR", bAcc), ("_UwMaskR", bUwMask), ("_UwFA", bUwFA), ("_UwFB", bUwFB), ("_UwFE", bUwFE), ("_UwFL1", bUwFL1), ("_UwFL1E", bUwFL1E), ("_UwFSnap", bUwFSnap), ("_TbProbe", bTbProbe));
		BindKernel(kContactPair, "KContactPair", ("_PosR", bPos), ("_MassR", bMass), ("_MatR", bMat), ("_BodyR", bBody), ("_OrderR", bOrder), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_CrossPairsFR", bCrossF), ("_Materials", bMaterials), ("_Acc", bAcc), ("_AccC", bAccC), ("_TbProbe", bTbProbe));
		BindKernel(kCrossFlagV, "KCrossFlagV", ("_PosR", bPos), ("_BodyR", bBody), ("_OrderR", bOrder), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_CrossPairsV", bCrossV), ("_TbProbe", bTbProbe));
		BindKernel(kUnweldV, "KUnweldV", ("_PosR", bPos), ("_VelHalfR", bVelHalf), ("_MassR", bMass), ("_RhoR", bRho), ("_StressR", bStress), ("_MatR", bMat), ("_OrderR", bOrder), ("_BodyR", bBody), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_CrossPairsVR", bCrossV), ("_Materials", bMaterials), ("_UwMaskR", bUwMask), ("_VelGrad", bVelGrad), ("_RhoRate", bRhoRate), ("_StressRate", bStressRate), ("_TbProbe", bTbProbe));
		BindKernel(kUnweldCloseV, "KUnweldCloseV", ("_PosR", bPos), ("_VelHalfR", bVelHalf), ("_MassR", bMass), ("_RhoR", bRho), ("_StressR", bStress), ("_EIntR", bEInt), ("_MatR", bMat), ("_OrderR", bOrder), ("_BodyR", bBody), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_Materials", bMaterials), ("_UwMaskR", bUwMask), ("_UwVL", bUwVL), ("_UwVRho", bUwVRho), ("_UwVL1", bUwVL1), ("_UwVS", bUwVS), ("_UwVS2", bUwVS2), ("_TbProbe", bTbProbe));
		BindKernel(kHgB, "KHourglassB", ("_PosR", bPos), ("_VelHalfR", bVelHalf), ("_MassR", bMass), ("_RhoR", bRho), ("_VelGradR", bVelGrad), ("_PlWorkR", bPlWork), ("_OrderR", bOrder), ("_BodyR", bBody), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_Partial4", bPartial4));
		BindKernel(kStateEB, "KStateEnergyB", ("_PosR", bPos), ("_MassR", bMass), ("_RhoR", bRho), ("_EIntR", bEInt), ("_MatR", bMat), ("_OrderR", bOrder), ("_BodyR", bBody), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_Materials", bMaterials), ("_Partial4", bPartial4));
		BindKernel(kMaskA, "KMaskA", ("_PosR", bPos), ("_VelHalfR", bVelHalf), ("_MassR", bMass), ("_RhoR", bRho), ("_StressR", bStress), ("_EIntR", bEInt), ("_MatR", bMat), ("_OrderR", bOrder), ("_BodyR", bBody), ("_CellStartR", bCellStart), ("_CellCountR", bCellCount), ("_CrossPairsFR", bCrossF), ("_Materials", bMaterials), ("_UwMask", bUwMask), ("_TbProbe", bTbProbe));
	}

	private void PushUniforms()
	{
		cs.SetInt("_N", N);
		cs.SetInt("_GridW", gridW);
		cs.SetInt("_GridH", gridH);
		cs.SetInt("_NumCells", numCells);
		cs.SetInt("_NumBlocks", cellBlocks);
		cs.SetInt("_NumGroups", nBlocks);
		cs.SetFloat("_CellSize", cellSize);
		cs.SetFloat("_OriginX", originX);
		cs.SetFloat("_OriginY", originY);
		cs.SetFloat("_H", H);
		cs.SetFloat("_AlphaAV", AlphaAV);
		cs.SetFloat("_BetaAV", BetaAV);
		cs.SetInt("_AVOn", ArtificialViscosity ? 1 : 0);
		cs.SetInt("_PlasticityOn", Plasticity ? 1 : 0);
		cs.SetFloat("_Beta", TaylorQuinney);
		cs.SetFloat("_CFL", CFL);
		cs.SetInt("_MutIgnoreMatBuf", MutIgnoreMaterialBuffer ? 1 : 0);
		cs.SetInt("_MutAsymMomentum", MutAsymmetricMomentum ? 1 : 0);
		cs.SetInt("_MutStiffX2", MutStiffnessX2 ? 1 : 0);
		cs.SetInt("_MutMatIndexZero", MutMaterialIndexZero ? 1 : 0);
		cs.SetInt("_MutNoHeating", MutNoHeating ? 1 : 0);
		cs.SetInt("_DensitySummation", DensitySummation ? 1 : 0);
		cs.SetFloat("_PinY", (PinY != float.NegativeInfinity) ? PinY : (-1e+30f));
		cs.SetFloat("_FixedDt", FixedTimeStep);
		cs.SetInt("_QuadN", QuadIntervals);
		cs.SetInt("_EFirstOrder", EnergyFirstOrder ? 1 : 0);
		cs.SetInt("_StateUseRhoSum", 0);
		cs.SetFloat("_RhoRateScale", MutRhoRateScale);
		cs.SetInt("_MutViscSign", MutViscSignFlip ? 1 : 0);
		cs.SetInt("_MutDegenSplit", MutDegenerateSplit ? 1 : 0);
		cs.SetFloat("_MutKickDt", MutKickDtScale);
		cs.SetInt("_ContactOn", Contact ? 1 : 0);
		cs.SetFloat("_ContactD0", Dx);
		float contactK = 0f;
		if (matsCPU != null && matsCPU.Length != 0)
		{
			contactK = matsCPU[0].K / (matsCPU[0].rho0 * Dx * Dx);
		}
		cs.SetFloat("_ContactK", contactK);
		cs.SetInt("_SpallOn", Spall ? 1 : 0);
		cs.SetInt("_DamageOn", Damage ? 1 : 0);
		cs.SetInt("_ArtStressOn", (ArtificialStress && ArtStressActivation != ArtActivation.Clumping) ? 1 : 0);
		cs.SetFloat("_ArtEps", ArtStressEps);
		cs.SetFloat("_ArtN", ArtStressN);
		cs.SetInt("_MutArtOldForm", MutArtStressOldForm ? 1 : 0);
		cs.SetFloat("_ClumpTau", ClumpTau);
		cs.SetInt("_ClumpForceAll", ClumpForceAll ? 1 : 0);
		cs.SetInt("_ClumpMutSkipCoincident", MutClumpSkipCoincident ? 1 : 0);
		if (SplitPressureViscosity && MutDegenerateSplit)
		{
			throw new InvalidOperationException("Flat2D: SplitPressureViscosity несовместим с MutDegenerateSplit (поставлен после Init)");
		}
		cs.SetInt("_MutSplitDegen", MutSplitDegenerate ? 1 : 0);
		cs.SetInt("_MutSplitMoveBeta", MutSplitMoveBeta ? 1 : 0);
		cs.SetInt("_MutStoreLeak", MutSplitStoreLeak ? 1 : 0);
		cs.SetInt("_MutSplitDropEps", MutSplitDropEps ? 1 : 0);
		cs.SetInt("_MutPlusStale", MutSplitPlusStale ? 1 : 0);
		cs.SetInt("_MutIfDropEps", MutIfDropEps ? 1 : 0);
		cs.SetInt("_MutIfAllCross", MutIfAllCross ? 1 : 0);
		cs.SetInt("_MutIfFlipSign", MutIfFlipSign ? 1 : 0);
		cs.SetInt("_MutIfChanDropVisc", MutIfChanDropVisc ? 1 : 0);
		cs.SetInt("_MutIfNearOnly", MutIfNearOnly ? 1 : 0);
		cs.SetInt("_MutIfDropArt", MutIfDropArt ? 1 : 0);
		if (InterfaceProbe && (Contact || (ArtificialStress && ArtStressActivation == ArtActivation.Clumping)))
		{
			throw new InvalidOperationException("Flat2D: InterfaceProbe несовместим с Contact и режимом скученности (_Acc содержит чужие ядра)");
		}
		if (InterfaceProbe && bIfAcc != null && ifSize != N)
		{
			throw new InvalidOperationException("Flat2D: InterfaceProbe включён после Init — буферы прибора не выделены");
		}
		cs.SetInt("_UwMutKeepTension", MutUnweldKeepTension ? 1 : 0);
		cs.SetInt("_UwMutDropEps", MutUnweldDropEps ? 1 : 0);
		cs.SetInt("_UwMutDropArt", MutUnweldDropArt ? 1 : 0);
		cs.SetInt("_UwMutLegacyK", MutContactLegacyStiffness ? 1 : 0);
		cs.SetFloat("_UwContactScale", ContactStiffnessScale);
		cs.SetInt("_UwMode", (SeparateBodiesMode == TwoBodyMode.APlus) ? 1 : 0);
		cs.SetFloat("_UwMaskBand", APlusTractionBand);
		cs.SetInt("_UwMutMaskNever", MutAPlusMaskNever ? 1 : 0);
		cs.SetInt("_UwMutMaskAlways", MutAPlusMaskAlways ? 1 : 0);
		GuardTwoBodies(N, afterInit: true);
		GuardErosionFracture(N, matsCPU != null ? matsCPU.Length : 0, afterInit: true);
		cs.SetFloat("_ErodeEpsP", ErodeEpsP);
		cs.SetInt("_ErodeShareQ24", (int)ShareQ24(ErodeShare));
		cs.SetInt("_ErodeSeed", (int)ErodeSeed);
		cs.SetVector("_ErodeGrave", new Vector4(originX - 1000f, originY - 1000f, 0f, 0f));
		cs.SetFloat("_RestrainD", RestraintWidthM);
		cs.SetInt("_RestrainUseEroded", Erosion ? 1 : 0);
	}

	private void GuardTwoBodies(int n, bool afterInit)
	{
		if (SeparateBodies)
		{
			if (BodyIds == null)
			{
				throw new InvalidOperationException("Flat2D: SeparateBodies без BodyIds — все частицы в теле 0, флаг был бы пуст");
			}
			if (BodyIds.Length != n)
			{
				throw new InvalidOperationException("Flat2D: SeparateBodies " + string.Format("при BodyIds длины {0} на {1} частиц — Init молча ", BodyIds.Length, n) + "дополнил бы нулями, и хвост частиц оказался бы в теле 0");
			}
			if (Contact)
			{
				throw new InvalidOperationException("Flat2D: SeparateBodies несовместим с Contact: два контактных ядра считали бы контакт дважды, а KContact к тому же не сохраняет импульс между веществами (D-1)");
			}
			if (ArtificialStress && ArtStressActivation == ArtActivation.Clumping)
			{
				throw new InvalidOperationException("Flat2D: SeparateBodies несовместим с режимом скученности (KClump/KArtMasked видят межтельные пары)");
			}
			if (InterfaceProbe)
			{
				throw new InvalidOperationException("Flat2D: SeparateBodies несовместим с InterfaceProbe: прибор интерфейса замыкается на _Acc из KForces (его наследник — UnweldProbe)");
			}
			if (SplitPressureViscosity)
			{
				throw new InvalidOperationException("Flat2D: SeparateBodies несовместим с SplitPressureViscosity: KSplitPV сверяется с _AccPS из KForces");
			}
			if (DensitySummation)
			{
				throw new InvalidOperationException("Flat2D: SeparateBodies несовместим с DensitySummation: плотность суммированием — диагностика K3, межтельные пары в ней не вычитаются");
			}
			if (MutViscSignFlip)
			{
				throw new InvalidOperationException("Flat2D: SeparateBodies несовместим с MutViscSignFlip: доказательство бегущего минимума _ViscMin требует ė_visc ≥ 0 по каждой паре (дизайн §3.6)");
			}
			if (MutIgnoreMaterialBuffer || MutMaterialIndexZero)
			{
				throw new InvalidOperationException("Flat2D: SeparateBodies несовместим с MutIgnoreMaterialBuffer/MutMaterialIndexZero: вещество в KContactPair и в копиях KForces разошлось бы по сторонам пары");
			}
			if (MutUnweldSkipRates && MutUnweldLateRates)
			{
				throw new InvalidOperationException("Flat2D: SeparateBodies с MutUnweldSkipRates И MutUnweldLateRates разом — контрфакты смешались бы, ни один не проверял бы своё");
			}
			if (float.IsNaN(ContactStiffnessScale) || ContactStiffnessScale <= 0f || ContactStiffnessScale > ContactStiffnessScaleMax)
			{
				throw new InvalidOperationException("Flat2D: SeparateBodies " + string.Format("при ContactStiffnessScale = {0}: допустимо ", ContactStiffnessScale) + string.Format("(0, {0}] — выше потолок устойчивости ", ContactStiffnessScaleMax) + "(ω·dt ≤ 1 при трёх контактах); упёрлись — отчёт, а не подстройка");
			}
			if (SeparateBodiesMode != TwoBodyMode.B && SeparateBodiesMode != TwoBodyMode.APlus)
			{
				throw new InvalidOperationException("Flat2D: SeparateBodies " + string.Format("с неизвестным режимом {0} (B = 0, APlus = 1)", (int)SeparateBodiesMode));
			}
			if (SeparateBodiesMode == TwoBodyMode.APlus)
			{
				if (MutContactLegacyStiffness)
				{
					throw new InvalidOperationException("Flat2D: SeparateBodies в режиме A⁺ с MutContactLegacyStiffness — контакта при A⁺ нет, мутация не кусает");
				}
				if (MutUnweldNoContact)
				{
					throw new InvalidOperationException("Flat2D: SeparateBodies в режиме A⁺ с MutUnweldNoContact — контакта при A⁺ нет и так (бессмысленно)");
				}
				if (MutUnweldKeepTension)
				{
					throw new InvalidOperationException("Flat2D: SeparateBodies в режиме A⁺ с MutUnweldKeepTension — контрфакт T7 для A⁺ — это MutAPlusMaskNever");
				}
				if (ContactStiffnessScale != 1f)
				{
					throw new InvalidOperationException("Flat2D: SeparateBodies в режиме A⁺ " + string.Format("при ContactStiffnessScale = {0} — жёсткость ", ContactStiffnessScale) + "контакта при A⁺ ничего не значит");
				}
				if (float.IsNaN(APlusTractionBand) || float.IsInfinity(APlusTractionBand) || APlusTractionBand < 0f)
				{
					throw new InvalidOperationException("Flat2D: SeparateBodies в режиме A⁺ " + string.Format("при APlusTractionBand = {0} Па — полоса гистерезиса ", APlusTractionBand) + "обязана быть конечной и ≥ 0");
				}
				if (MutAPlusMaskNever && MutAPlusMaskAlways)
				{
					throw new InvalidOperationException("Flat2D: SeparateBodies в режиме A⁺ с MutAPlusMaskNever И MutAPlusMaskAlways разом — контрфакты смешались бы, ни один не проверял бы своё");
				}
			}
			if (afterInit && bCrossF != null && tbSize != N)
			{
				throw new InvalidOperationException("Flat2D: SeparateBodies включён после Init — буферы двух тел не выделены");
			}
		}
		if (UnweldProbe && !SeparateBodies)
		{
			throw new InvalidOperationException("Flat2D: UnweldProbe без SeparateBodies — прибор замыкания мерил бы пустоту");
		}
		if (afterInit && UnweldProbe && bUwFA != null && uwSize != N)
		{
			throw new InvalidOperationException("Flat2D: UnweldProbe включён после Init — буферы прибора не выделены");
		}
		if (!SeparateBodies && (MutUnweldKeepTension || MutUnweldSkipRates || MutUnweldLateRates || MutUnweldDropEps || MutUnweldDropArt || MutUnweldNoContact || MutContactLegacyStiffness || ContactStiffnessScale != 1f))
		{
			throw new InvalidOperationException("Flat2D: контрфакт двух тел (или множитель жёсткости) без самих двух тел — он не может укусить");
		}
		if ((MutAPlusMaskNever || MutAPlusMaskAlways) && (!SeparateBodies || SeparateBodiesMode != TwoBodyMode.APlus))
		{
			throw new InvalidOperationException("Flat2D: MutAPlusMaskNever/MutAPlusMaskAlways без SeparateBodies в режиме A⁺ — маски нет, контрфакт не может укусить");
		}
	}

	private void GuardErosionFracture(int n, int nMats, bool afterInit)
	{
		if (Erosion && DensitySummation)
		{
			throw new InvalidOperationException("Flat2D: стачивание/трещины — Erosion несовместим с DensitySummation (KDensitySum сдвинул бы ρ стёртой частицы из покоя)");
		}
		if (Erosion && (float.IsNaN(ErodeEpsP) || ErodeEpsP < 0f))
		{
			throw new InvalidOperationException("Flat2D: стачивание/трещины " + string.Format("— порог ErodeEpsP = {0} обязан быть числом ≥ 0", ErodeEpsP));
		}
		if (Erosion && (float.IsNaN(ErodeShare) || ErodeShare < 0f || ErodeShare > 1f))
		{
			throw new InvalidOperationException("Flat2D: стачивание/трещины " + string.Format("— доля ErodeShare = {0} вне [0; 1]", ErodeShare));
		}
		if (Fracture && (Xi == null || Xi.Length != n))
		{
			throw new InvalidOperationException("Flat2D: стачивание/трещины " + string.Format("— Fracture требует Xi длиной {0} (дано {1}); ", n, Xi != null ? Xi.Length.ToString() : "null") + "ξ строит Flat2DFractureSetup.BuildXi");
		}
		if (Fracture && (AsbPerMaterial == null || AsbPerMaterial.Length != nMats))
		{
			throw new InvalidOperationException("Flat2D: стачивание/трещины " + string.Format("— Fracture требует AsbPerMaterial на каждый из {0} блоков веществ ", nMats) + "(дано " + (AsbPerMaterial != null ? AsbPerMaterial.Length.ToString() : "null") + "); строит Flat2DFractureSetup.AsbPerBlock");
		}
		if (afterInit)
		{
			if (bEroded != null && erAtInit != Erosion)
			{
				throw new InvalidOperationException("Flat2D: стачивание/трещины " + string.Format("— Erosion сменён после Init ({0} → {1}); задать до Init", erAtInit, Erosion));
			}
			if (bXi != null && frAtInit != Fracture)
			{
				throw new InvalidOperationException("Flat2D: стачивание/трещины " + string.Format("— Fracture сменён после Init ({0} → {1}); задать до Init", frAtInit, Fracture));
			}
		}
		if (Restraint && (RestraintWidthM <= 0f || float.IsInfinity(RestraintWidthM)))
		{
			throw new InvalidOperationException("Flat2D: стачивание/трещины " + string.Format("— Restraint требует ширину снаряда RestraintWidthM > 0 (дано {0})", RestraintWidthM));
		}
		if (Restraint && (RestraintMask == null || RestraintMask.Length != n))
		{
			throw new InvalidOperationException("Flat2D: стачивание/трещины " + string.Format("— Restraint требует RestraintMask длиной {0} (броня держится, снаряд нет)", n));
		}
		if (afterInit && bRestrainX0 != null && rsAtInit != Restraint)
		{
			throw new InvalidOperationException("Flat2D: стачивание/трещины " + string.Format("— Restraint сменён после Init ({0} → {1}); задать до Init", rsAtInit, Restraint));
		}
	}

	private static uint ShareQ24(float share)
	{
		return (uint)Math.Min(16777216.0, Math.Round((double)share * 16777216.0));
	}

	private void Sort()
	{
		cs.Dispatch(kHash, nBlocks, 1, 1);
		cs.Dispatch(kClear, cellBlocks, 1, 1);
		cs.Dispatch(Erosion ? kCountE : kCount, nBlocks, 1, 1);
		cs.Dispatch(kScanB, cellBlocks, 1, 1);
		cs.Dispatch(kScanS, 1, 1, 1);
		cs.Dispatch(kScanA, cellBlocks, 1, 1);
		cs.Dispatch(Erosion ? kScatterE : kScatter, nBlocks, 1, 1);
		if (DeterministicOrder)
		{
			cs.Dispatch(kSortCells, cellBlocks, 1, 1);
		}
	}

	public void Step()
	{
		EnsureBound();
		Sort();
		if (DensitySummation)
		{
			cs.Dispatch(kRhoSum, nBlocks, 1, 1);
		}
		if (!NoInteraction)
		{
			cs.Dispatch(kForces, nBlocks, 1, 1);
		}
		if (SeparateBodies && !NoInteraction)
		{
			cs.Dispatch(kCrossFlagF, nBlocks, 1, 1);
			if (SeparateBodiesMode == TwoBodyMode.APlus)
			{
				cs.Dispatch(kMaskA, nBlocks, 1, 1);
			}
			cs.Dispatch(kUnweldF, nBlocks, 1, 1);
			if (UnweldProbe)
			{
				cs.Dispatch(kUnweldCloseF, nBlocks, 1, 1);
			}
			if (SeparateBodiesMode != TwoBodyMode.APlus && !MutUnweldNoContact)
			{
				cs.Dispatch(kContactPair, nBlocks, 1, 1);
			}
		}
		if (Contact && !NoInteraction)
		{
			cs.Dispatch(kContact, nBlocks, 1, 1);
		}
		if (ArtificialStress && ArtStressActivation == ArtActivation.Clumping && !NoInteraction)
		{
			cs.Dispatch(kClump, nBlocks, 1, 1);
			cs.Dispatch(kArtMasked, nBlocks, 1, 1);
		}
		if (SplitPressureViscosity && !NoInteraction)
		{
			cs.Dispatch(kSplitPV, nBlocks, 1, 1);
		}
		if (InterfaceProbe && !NoInteraction)
		{
			cs.Dispatch(kInterface, nBlocks, 1, 1);
		}
		if (Restraint && !NoInteraction)
		{
			cs.Dispatch(kRestrain, nBlocks, 1, 1);
		}
		cs.Dispatch(kKick, nBlocks, 1, 1);
		if (AnchorMask != null)
		{
			cs.Dispatch(kAnchor, nBlocks, 1, 1);
		}
		cs.Dispatch(kDrift, nBlocks, 1, 1);
		if (!NoInteraction)
		{
			cs.Dispatch(kRates, nBlocks, 1, 1);
		}
		if (SeparateBodies && !NoInteraction && !MutUnweldSkipRates && !MutUnweldLateRates)
		{
			cs.Dispatch(kCrossFlagV, nBlocks, 1, 1);
			cs.Dispatch(kUnweldV, nBlocks, 1, 1);
		}
		if (UnweldProbe)
		{
			cs.Dispatch(kUnweldCloseV, nBlocks, 1, 1);
		}
		cs.Dispatch(Fracture ? kStateF : kState, nBlocks, 1, 1);
		if (SeparateBodies && !NoInteraction && MutUnweldLateRates)
		{
			cs.Dispatch(kCrossFlagV, nBlocks, 1, 1);
			cs.Dispatch(kUnweldV, nBlocks, 1, 1);
		}
		cs.Dispatch(Fracture ? kDamageF : kDamage, nBlocks, 1, 1);
		if (AccumulateWork)
		{
			cs.Dispatch(kAccW, nBlocks, 1, 1);
		}
		if (AccumulateWork && SplitPressureViscosity && !NoInteraction)
		{
			cs.Dispatch(kAccumPV, nBlocks, 1, 1);
		}
		if (Erosion)
		{
			cs.Dispatch(kErodeMark, nBlocks, 1, 1);
			cs.Dispatch(kErodeApply, nBlocks, 1, 1);
		}
		cs.Dispatch(kRedStep, nBlocks, 1, 1);
		cs.Dispatch(kStep, 1, 1, 1);
	}

	public void Steps(int n)
	{
		EnsureBound();
		PushUniforms();
		while (n > 0)
		{
			Step();
			n--;
		}
	}

	private void EnsureBound()
	{
		if (boundOwner != this && bPos != null)
		{
			BindAll();
			PushUniforms();
			boundOwner = this;
		}
	}

	public void SimState(out double dt, out double time, out double vmax, out double cmax)
	{
		float[] sim = new float[8];
		bSim.GetData(sim);
		dt = sim[0];
		time = sim[1];
		vmax = sim[2];
		cmax = sim[3];
	}

	public Diag Measure(bool withHourglass = true)
	{
		EnsureBound();
		Diag d = default(Diag);
		cs.Dispatch(kDiag, nBlocks, 1, 1);
		Vector4[] arr = new Vector4[nBlocks];
		bPartial4.GetData(arr);
		RadarKahan px = default(RadarKahan);
		RadarKahan py = default(RadarKahan);
		RadarKahan ke = default(RadarKahan);
		RadarKahan ie = default(RadarKahan);
		for (int i = 0; i < nBlocks; i++)
		{
			px.Add(arr[i].x);
			py.Add(arr[i].y);
			ke.Add(arr[i].z);
			ie.Add(arr[i].w);
		}
		d.px = px.Value;
		d.py = py.Value;
		d.kinetic = ke.Value;
		d.internalE = ie.Value;
		if (withHourglass)
		{
			cs.Dispatch(SeparateBodies ? kHgB : kHg, nBlocks, 1, 1);
			bPartial4.GetData(arr);
			RadarKahan hg = default(RadarKahan);
			RadarKahan pw = default(RadarKahan);
			for (int i = 0; i < nBlocks; i++)
			{
				hg.Add(arr[i].x);
				pw.Add(arr[i].y);
			}
			d.hourglass = hg.Value;
			d.plasticWork = pw.Value;
			d.hourglassAllPairs = d.hourglass;
			if (SeparateBodies)
			{
				cs.Dispatch(kHg, nBlocks, 1, 1);
				bPartial4.GetData(arr);
				RadarKahan all = default(RadarKahan);
				for (int i = 0; i < nBlocks; i++)
				{
					all.Add(arr[i].x);
				}
				d.hourglassAllPairs = all.Value;
			}
		}
		float[] sim = new float[8];
		bSim.GetData(sim);
		d.dt = sim[0];
		d.time = sim[1];
		d.vmax = sim[2];
		d.cmax = sim[3];
		return d;
	}

	public void MeasurePower(out double power, out double kineticHalf)
	{
		EnsureBound();
		cs.Dispatch(kPower, nBlocks, 1, 1);
		Vector4[] arr = new Vector4[nBlocks];
		bPartial4.GetData(arr);
		RadarKahan p = default(RadarKahan);
		RadarKahan k = default(RadarKahan);
		for (int i = 0; i < nBlocks; i++)
		{
			p.Add(arr[i].x);
			k.Add(arr[i].y);
		}
		power = p.Value;
		kineticHalf = k.Value;
	}

	public void ReadProbe(out int stateCalls, out double stateDt, out int kickCalls, out double kickDt)
	{
		float[] sim = new float[8];
		bProbe.GetData(sim);
		stateCalls = (int)Math.Round(sim[0]);
		stateDt = sim[1];
		kickCalls = (int)Math.Round(sim[2]);
		kickDt = sim[3];
	}

	public void ResetProbe()
	{
		bProbe.SetData(new float[8]);
	}

	public double ReadDpDe()
	{
		float[] sim = new float[8];
		bProbe.GetData(sim);
		return sim[4];
	}

	public void MeasureClosure(out double powHalf, out double sumMA2, out double powFull)
	{
		EnsureBound();
		cs.Dispatch(kClosure, nBlocks, 1, 1);
		Vector4[] arr = new Vector4[nBlocks];
		bPartial4.GetData(arr);
		RadarKahan a = default(RadarKahan);
		RadarKahan b = default(RadarKahan);
		RadarKahan c = default(RadarKahan);
		for (int i = 0; i < nBlocks; i++)
		{
			a.Add(arr[i].x);
			b.Add(arr[i].y);
			c.Add(arr[i].z);
		}
		powHalf = a.Value;
		sumMA2 = b.Value;
		powFull = c.Value;
	}

	public void MeasureParts(out double powerP, out double powerS, out double eInt, out double shear)
	{
		EnsureBound();
		cs.Dispatch(kParts, nBlocks, 1, 1);
		Vector4[] arr = new Vector4[nBlocks];
		bPartial4.GetData(arr);
		RadarKahan a = default(RadarKahan);
		RadarKahan b = default(RadarKahan);
		RadarKahan c = default(RadarKahan);
		RadarKahan d = default(RadarKahan);
		for (int i = 0; i < nBlocks; i++)
		{
			a.Add(arr[i].x);
			b.Add(arr[i].y);
			c.Add(arr[i].z);
			d.Add(arr[i].w);
		}
		powerP = a.Value;
		powerS = b.Value;
		eInt = c.Value;
		shear = d.Value;
	}

	public void MeasureStateEnergy(out double uInterior, out double uRest, out double ePath, out int interiorCount)
	{
		EnsureBound();
		cs.Dispatch(SeparateBodies ? kStateEB : kStateE, nBlocks, 1, 1);
		Vector4[] arr = new Vector4[nBlocks];
		bPartial4.GetData(arr);
		RadarKahan uInt = default(RadarKahan);
		RadarKahan uR = default(RadarKahan);
		RadarKahan eP = default(RadarKahan);
		double count = 0.0;
		for (int i = 0; i < nBlocks; i++)
		{
			uInt.Add(arr[i].x);
			uR.Add(arr[i].y);
			eP.Add(arr[i].z);
			count += arr[i].w;
		}
		uInterior = uInt.Value;
		uRest = uR.Value;
		ePath = eP.Value;
		interiorCount = (int)Math.Round((float)count);
	}

	public void CheckQuadrature(int matIndex, float rho, out double uAtRho0, out double uN, out double u2N)
	{
		EnsureBound();
		cs.SetInt("_QMat", matIndex);
		cs.SetFloat("_QRho", rho);
		cs.SetInt("_QuadN", QuadIntervals);
		cs.Dispatch(kQuad, 1, 1, 1);
		float[] arr = new float[8];
		bProbe.GetData(arr);
		uAtRho0 = arr[5];
		uN = arr[6];
		u2N = arr[7];
	}

	public void MeasurePremise(out double maxDpDe, out double maxDcDe, out double maxGamma, out double maxP)
	{
		EnsureBound();
		cs.Dispatch(kPremise, nBlocks, 1, 1);
		Vector4[] arr = new Vector4[nBlocks];
		bPartial4.GetData(arr);
		double a = 0.0;
		double b = 0.0;
		double c = 0.0;
		double d = 0.0;
		for (int i = 0; i < nBlocks; i++)
		{
			a = Math.Max(a, arr[i].x);
			b = Math.Max(b, arr[i].y);
			c = Math.Max(c, arr[i].z);
			d = Math.Max(d, arr[i].w);
		}
		maxDpDe = a;
		maxDcDe = b;
		maxGamma = c;
		maxP = d;
	}

	public double ReadViscMin()
	{
		float[] arr = new float[N];
		bViscMin.GetData(arr);
		float min = 0f;
		for (int i = 0; i < N; i++)
		{
			min = Math.Min(min, arr[i]);
		}
		return min;
	}

	public float[] ReadEdotVisc()
	{
		float[] arr = new float[N];
		bEdotVisc.GetData(arr);
		return arr;
	}

	public float[] ReadEdotP()
	{
		float[] arr = new float[N];
		bEdotP.GetData(arr);
		return arr;
	}

	public Vector4[] ReadAccPV()
	{
		Vector4[] arr = new Vector4[N];
		bAccPV.GetData(arr);
		return arr;
	}

	public Vector4[] ReadPowPV()
	{
		Vector4[] arr = new Vector4[N];
		bPowPV.GetData(arr);
		return arr;
	}

	public void MeasureSplitPV(out double wPressure, out double wViscosity, out double heatVisc)
	{
		MeasureSplitPV(out wPressure, out wViscosity, out heatVisc, out _, out _, out _, out _);
	}

	public void MeasureSplitPV(out double wPressure, out double wViscosity, out double heatVisc, out double powPressure, out double powViscosity, out double halfPressure, out double halfViscosity)
	{
		float[] mass = new float[N];
		bMass.GetData(mass);
		float[] chanPr = new float[N];
		float[] chanV = new float[N];
		float[] heatV = new float[N];
		float[] powAccPr = new float[N];
		float[] powAccV = new float[N];
		float[] halfPr = new float[N];
		float[] halfV = new float[N];
		bChanPr.GetData(chanPr);
		bChanV.GetData(chanV);
		bHeatV.GetData(heatV);
		bPowAccPr.GetData(powAccPr);
		bPowAccV.GetData(powAccV);
		bHalfPr.GetData(halfPr);
		bHalfV.GetData(halfV);
		RadarKahan wP = default(RadarKahan);
		RadarKahan wV = default(RadarKahan);
		RadarKahan hv = default(RadarKahan);
		RadarKahan pp = default(RadarKahan);
		RadarKahan pv = default(RadarKahan);
		RadarKahan hp = default(RadarKahan);
		RadarKahan hvl = default(RadarKahan);
		for (int i = 0; i < N; i++)
		{
			wP.Add(mass[i] * (double)chanPr[i]);
			wV.Add(mass[i] * (double)chanV[i]);
			hv.Add(mass[i] * (double)heatV[i]);
			pp.Add(powAccPr[i]);
			pv.Add(powAccV[i]);
			hp.Add(halfPr[i]);
			hvl.Add(halfV[i]);
		}
		wPressure = wP.Value;
		wViscosity = wV.Value;
		heatVisc = hv.Value;
		powPressure = pp.Value;
		powViscosity = pv.Value;
		halfPressure = hp.Value;
		halfViscosity = hvl.Value;
	}

	public double MeasurePressurePowerPlus()
	{
		float[] arr = new float[N];
		bPowAccPrPlus.GetData(arr);
		RadarKahan k = default(RadarKahan);
		for (int i = 0; i < N; i++)
		{
			k.Add(arr[i]);
		}
		return k.Value;
	}

	public bool ProbeAlive(int stepsSinceInit, out string detail)
	{
		float[] sim = new float[8];
		bProbe.GetData(sim);
		int stateCalls = (int)Math.Round(sim[0]);
		int kickCalls = (int)Math.Round(sim[2]);
		bool pinYIsNegInf = PinY == float.NegativeInfinity;
		bool alive = stateCalls == stepsSinceInit && (!pinYIsNegInf || kickCalls == stepsSinceInit + 1);
		detail = string.Format("живость: KUpdateState {0} (ждём {1}), KKick {2}", stateCalls, stepsSinceInit, kickCalls) + (pinYIsNegInf ? string.Format(" (ждём {0})", stepsSinceInit + 1) : " (не проверяется: PinY конечен)");
		return alive;
	}

	public float[] ReadEInt()
	{
		float[] arr = new float[N];
		bEInt.GetData(arr);
		return arr;
	}

	public void MeasureStateEnergySum(out double uInterior, out double uRest, out double ePath, out int interiorCount)
	{
		cs.SetInt("_StateUseRhoSum", 1);
		MeasureStateEnergy(out uInterior, out uRest, out ePath, out interiorCount);
		cs.SetInt("_StateUseRhoSum", 0);
	}

	public void MeasureWork(out double workP, out double workS, out double shearPot, out double ke)
	{
		EnsureBound();
		cs.Dispatch(kRedW, nBlocks, 1, 1);
		Vector4[] arr = new Vector4[nBlocks];
		bPartial4.GetData(arr);
		RadarKahan a = default(RadarKahan);
		RadarKahan b = default(RadarKahan);
		RadarKahan c = default(RadarKahan);
		RadarKahan d = default(RadarKahan);
		for (int i = 0; i < nBlocks; i++)
		{
			a.Add(arr[i].x);
			b.Add(arr[i].y);
			c.Add(arr[i].z);
			d.Add(arr[i].w);
		}
		workP = a.Value;
		workS = b.Value;
		shearPot = c.Value;
		ke = d.Value;
	}

	public void MeasureChannels(out double wP, out double wS, out double wTotal, out double wArt)
	{
		MeasureChannels(out wP, out wS, out wTotal, out wArt, out _);
	}

	public void MeasureChannels(out double wP, out double wS, out double wTotal, out double wArt, out double wContact)
	{
		EnsureBound();
		cs.Dispatch(kRedChan, nBlocks, 1, 1);
		Vector4[] arr = new Vector4[nBlocks];
		bPartial4.GetData(arr);
		float[] con = new float[nBlocks];
		bPartialCon.GetData(con);
		RadarKahan a = default(RadarKahan);
		RadarKahan b = default(RadarKahan);
		RadarKahan c = default(RadarKahan);
		RadarKahan d = default(RadarKahan);
		RadarKahan e = default(RadarKahan);
		for (int i = 0; i < nBlocks; i++)
		{
			a.Add(arr[i].x);
			b.Add(arr[i].y);
			c.Add(arr[i].z);
			d.Add(arr[i].w);
			e.Add(con[i]);
		}
		wP = a.Value;
		wS = b.Value;
		wTotal = c.Value;
		wArt = d.Value;
		wContact = e.Value;
	}

	public Vector2[] ReadAccP()
	{
		Vector4[] src = new Vector4[N];
		bAccPS.GetData(src);
		Vector2[] dst = new Vector2[N];
		for (int i = 0; i < N; i++)
		{
			dst[i] = new Vector2(src[i].x, src[i].y);
		}
		return dst;
	}

	public Vector2[] ReadAccS()
	{
		Vector4[] src = new Vector4[N];
		bAccPS.GetData(src);
		Vector2[] dst = new Vector2[N];
		for (int i = 0; i < N; i++)
		{
			dst[i] = new Vector2(src[i].z, src[i].w);
		}
		return dst;
	}

	public Vector2[] ReadAccC()
	{
		Vector2[] arr = new Vector2[N];
		bAccC.GetData(arr);
		return arr;
	}

	public void MeasureDamage(out double dissipation, out double meanD, out int failed, out double mass)
	{
		EnsureBound();
		cs.Dispatch(kRedDmg, nBlocks, 1, 1);
		Vector4[] arr = new Vector4[nBlocks];
		bPartial4.GetData(arr);
		RadarKahan a = default(RadarKahan);
		RadarKahan b = default(RadarKahan);
		RadarKahan c = default(RadarKahan);
		RadarKahan d = default(RadarKahan);
		for (int i = 0; i < nBlocks; i++)
		{
			a.Add(arr[i].x);
			b.Add(arr[i].y);
			c.Add(arr[i].z);
			d.Add(arr[i].w);
		}
		dissipation = a.Value;
		meanD = b.Value / Math.Max(N, 1);
		failed = (int)Math.Round((float)c.Value);
		mass = d.Value;
	}

	public void MeasureErosion(out double kinetic, out double internalE, out double px, out double py)
	{
		kinetic = 0.0;
		internalE = 0.0;
		px = 0.0;
		py = 0.0;
		if (Erosion && erSize == N)
		{
			EnsureBound();
			cs.Dispatch(kRedErode, nBlocks, 1, 1);
			Vector4[] arr = new Vector4[nBlocks];
			bPartial4.GetData(arr);
			RadarKahan a = default(RadarKahan);
			RadarKahan b = default(RadarKahan);
			RadarKahan c = default(RadarKahan);
			RadarKahan d = default(RadarKahan);
			for (int i = 0; i < nBlocks; i++)
			{
				a.Add(arr[i].x);
				b.Add(arr[i].y);
				c.Add(arr[i].z);
				d.Add(arr[i].w);
			}
			kinetic = a.Value;
			internalE = b.Value;
			px = c.Value;
			py = d.Value;
		}
	}

	public double MeasureRestraint()
	{
		RadarKahan k = default(RadarKahan);
		if (Restraint && rsSize == N)
		{
			EnsureBound();
			cs.Dispatch(kRedRestrain, nBlocks, 1, 1);
			Vector4[] arr = new Vector4[nBlocks];
			bPartial4.GetData(arr);
			for (int i = 0; i < nBlocks; i++)
			{
				k.Add(arr[i].x);
			}
		}
		return k.Value;
	}

	public uint[] ReadRestrainProbe()
	{
		uint[] arr = new uint[4];
		bRestrainProbe.GetData(arr);
		return arr;
	}

	public uint[] ReadEroded()
	{
		uint[] arr = new uint[N];
		if (Erosion && erSize == N)
		{
			bEroded.GetData(arr);
		}
		return arr;
	}

	public uint[] ReadErodeProbe()
	{
		uint[] arr = new uint[16];
		bErodeProbe.GetData(arr);
		return arr;
	}

	public float[] ReadDamage()
	{
		float[] arr = new float[N];
		bDamage.GetData(arr);
		return arr;
	}

	public float[] ReadSpallFail()
	{
		float[] arr = new float[N];
		bSpallFail.GetData(arr);
		return arr;
	}

	public Vector2[] ReadAccA()
	{
		Vector2[] arr = new Vector2[N];
		bAccA.GetData(arr);
		return arr;
	}

	public Vector4[] MeasureClump()
	{
		EnsureBound();
		PushUniforms();
		Sort();
		cs.Dispatch(kClump, nBlocks, 1, 1);
		Vector4[] arr = new Vector4[N];
		bClumpInfo.GetData(arr);
		return arr;
	}

	public ulong StateHash()
	{
		ulong h = 14695981039346656037L;
		void Bits(float f)
		{
			ulong b = (ulong)(uint)BitConverter.SingleToInt32Bits(f);
			h ^= b;
			h *= 1099511628211L;
			h ^= b >> 8;
			h *= 1099511628211L;
			h ^= b >> 16;
			h *= 1099511628211L;
			h ^= b >> 24;
			h *= 1099511628211L;
		}
		Vector2[] pos = new Vector2[N];
		bPos.GetData(pos);
		Vector2[] velHalf = new Vector2[N];
		bVelHalf.GetData(velHalf);
		Vector2[] velFull = new Vector2[N];
		bVelFull.GetData(velFull);
		float[] rho = new float[N];
		bRho.GetData(rho);
		Vector4[] stress = new Vector4[N];
		bStress.GetData(stress);
		float[] eInt = new float[N];
		bEInt.GetData(eInt);
		float[] temp = new float[N];
		bTemp.GetData(temp);
		float[] epsP = new float[N];
		bEpsP.GetData(epsP);
		for (int i = 0; i < N; i++)
		{
			Bits(pos[i].x);
			Bits(pos[i].y);
			Bits(velHalf[i].x);
			Bits(velHalf[i].y);
			Bits(velFull[i].x);
			Bits(velFull[i].y);
			Bits(rho[i]);
			Bits(eInt[i]);
			Bits(temp[i]);
			Bits(epsP[i]);
			Bits(stress[i].x);
			Bits(stress[i].y);
			Bits(stress[i].z);
			Bits(stress[i].w);
		}
		if (Erosion && erSize == N)
		{
			uint[] eroded = new uint[N];
			bEroded.GetData(eroded);
			for (int i = 0; i < N; i++)
			{
				Bits(eroded[i]);
			}
		}
		if (Restraint && rsSize == N)
		{
			Vector2[] x0 = new Vector2[N];
			bRestrainX0.GetData(x0);
			for (int i = 0; i < N; i++)
			{
				Bits(x0[i].x);
				Bits(x0[i].y);
			}
		}
		return h;
	}

	public uint[] ReadOrder()
	{
		uint[] arr = new uint[N];
		bOrder.GetData(arr);
		return arr;
	}

	public uint[] ReadMaterialIds()
	{
		uint[] arr = new uint[N];
		bMat.GetData(arr);
		return arr;
	}

	public void ReadGpuFlags(out bool avOn, out bool plasticity)
	{
		EnsureBound();
		cs.Dispatch(kPower, nBlocks, 1, 1);
		Vector4[] arr = new Vector4[nBlocks];
		bPartial4.GetData(arr);
		double av = 0.0;
		double pl = 0.0;
		for (int i = 0; i < nBlocks; i++)
		{
			av += arr[i].x;
			pl += arr[i].y;
		}
		avOn = av > 0.5;
		plasticity = pl > 0.5;
	}

	public Vector2[] ReadPositions()
	{
		Vector2[] arr = new Vector2[N];
		bPos.GetData(arr);
		return arr;
	}

	public Vector2[] ReadVelocities()
	{
		Vector2[] arr = new Vector2[N];
		bVelFull.GetData(arr);
		return arr;
	}

	public Vector2[] ReadVelHalf()
	{
		Vector2[] arr = new Vector2[N];
		bVelHalf.GetData(arr);
		return arr;
	}

	public float[] ReadDensity()
	{
		float[] arr = new float[N];
		bRho.GetData(arr);
		return arr;
	}

	public float[] ReadTemperature()
	{
		float[] arr = new float[N];
		bTemp.GetData(arr);
		return arr;
	}

	public float[] ReadPlasticStrain()
	{
		float[] arr = new float[N];
		bEpsP.GetData(arr);
		return arr;
	}

	public float[] ReadMass()
	{
		float[] arr = new float[N];
		bMass.GetData(arr);
		return arr;
	}

	public Vector4[] ReadStress()
	{
		Vector4[] arr = new Vector4[N];
		bStress.GetData(arr);
		return arr;
	}

	public Vector4[] ReadVelGrad()
	{
		Vector4[] arr = new Vector4[N];
		bVelGrad.GetData(arr);
		return arr;
	}

	public Vector2[] ReadAcc()
	{
		Vector2[] arr = new Vector2[N];
		bAcc.GetData(arr);
		return arr;
	}

	public Vector4[] ReadIfAcc()
	{
		Vector4[] arr = new Vector4[N];
		bIfAcc.GetData(arr);
		return arr;
	}

	public Vector4[] ReadIfChanPS()
	{
		Vector4[] arr = new Vector4[N];
		bIfChanPS.GetData(arr);
		return arr;
	}

	public Vector4[] ReadIfChanVA()
	{
		Vector4[] arr = new Vector4[N];
		bIfChanVA.GetData(arr);
		return arr;
	}

	public Vector4[] ReadIfCount()
	{
		Vector4[] arr = new Vector4[N];
		bIfCount.GetData(arr);
		return arr;
	}

	public Vector4[] ReadIfColour()
	{
		Vector4[] arr = new Vector4[N];
		bIfColour.GetData(arr);
		return arr;
	}

	public Vector4[] ReadIfBand()
	{
		Vector4[] arr = new Vector4[N];
		bIfBand.GetData(arr);
		return arr;
	}

	public int ReadIfProbe()
	{
		float[] arr = new float[1];
		bIfProbe.GetData(arr);
		return (int)arr[0];
	}

	private void NeedTb(string what)
	{
		if (bCrossF == null || tbSize != N)
		{
			throw new InvalidOperationException("Flat2D: " + what + " — буферы двух тел не выделены (SeparateBodies выкл. на Init)");
		}
	}

	private void NeedUw(string what)
	{
		if (bUwFA == null || uwSize != N)
		{
			throw new InvalidOperationException("Flat2D: " + what + " — буферы прибора CLOSE не выделены (UnweldProbe выкл. на Init)");
		}
	}

	public uint[] ReadCrossPairsF()
	{
		NeedTb("ReadCrossPairsF");
		uint[] arr = new uint[N];
		bCrossF.GetData(arr);
		return arr;
	}

	public uint[] ReadCrossPairsV()
	{
		NeedTb("ReadCrossPairsV");
		uint[] arr = new uint[N];
		bCrossV.GetData(arr);
		return arr;
	}

	public uint[] ReadTwoBodyProbe()
	{
		uint[] arr = new uint[16];
		bTbProbe.GetData(arr);
		return arr;
	}

	public uint[] ReadUwMask()
	{
		NeedTb("ReadUwMask");
		if (SeparateBodiesMode != TwoBodyMode.APlus)
		{
			throw new InvalidOperationException("Flat2D: ReadUwMask вне режима A⁺ — KMaskA не диспетчится, маски нет");
		}
		uint[] arr = new uint[N];
		bUwMask.GetData(arr);
		return arr;
	}

	public bool ProbeTwoBodiesAlive(int steps, out string detail)
	{
		return ProbeTwoBodiesAlive(steps, out detail, out uint hazard);
	}

	public bool ProbeTwoBodiesAlive(int steps, out string detail, out uint hazard)
	{
		uint[] tb = new uint[16];
		bTbProbe.GetData(tb);
		long[] expected = new long[9];
		if (SeparateBodies && !NoInteraction)
		{
			expected[0] = steps + 1;
			expected[1] = steps + 1;
			expected[2] = (SeparateBodiesMode != TwoBodyMode.APlus && !MutUnweldNoContact) ? (steps + 1) : 0;
			expected[4] = MutUnweldSkipRates ? 0 : steps;
			expected[3] = MutUnweldSkipRates ? 0 : steps;
			expected[6] = UnweldProbe ? steps : 0;
			expected[5] = UnweldProbe ? steps : 0;
			expected[8] = (SeparateBodiesMode == TwoBodyMode.APlus) ? (steps + 1) : 0;
		}
		string[] names = new string[9] { "KCrossFlagF", "KUnweldF", "KContactPair", "KCrossFlagV", "KUnweldV", "KUnweldCloseF", "KUnweldCloseV", "", "KMaskA" };
		StringBuilder sb = new StringBuilder("живость двух тел:");
		bool alive = true;
		for (int i = 0; i < 9; i++)
		{
			if (i == 7)
			{
				continue;
			}
			bool match = tb[i] == expected[i];
			alive &= match;
			sb.Append(string.Format(" {0} {1} (ждём {2}){3};", names[i], tb[i], expected[i], match ? "" : " ⛔"));
		}
		hazard = tb[7];
		sb.Append(string.Format(" | опасность ė_visc,межтел<0: {0} частице-шагов{1}", tb[7], tb[7] != 0 ? " ⚠️ (C.3: печатается, не решает живость)" : ""));
		detail = sb.ToString();
		return alive;
	}

	public void ReadUwF(out Vector4[] a, out Vector4[] b, out Vector4[] e, out Vector4[] l1, out Vector4[] l1e, out Vector4[] snap)
	{
		NeedUw("ReadUwF");
		a = new Vector4[N];
		bUwFA.GetData(a);
		b = new Vector4[N];
		bUwFB.GetData(b);
		e = new Vector4[N];
		bUwFE.GetData(e);
		l1 = new Vector4[N];
		bUwFL1.GetData(l1);
		l1e = new Vector4[N];
		bUwFL1E.GetData(l1e);
		snap = new Vector4[N];
		bUwFSnap.GetData(snap);
	}

	public void ReadUwV(out Vector4[] L, out Vector4[] rho, out Vector4[] l1, out Vector4[] s)
	{
		NeedUw("ReadUwV");
		L = new Vector4[N];
		bUwVL.GetData(L);
		rho = new Vector4[N];
		bUwVRho.GetData(rho);
		l1 = new Vector4[N];
		bUwVL1.GetData(l1);
		s = new Vector4[N];
		bUwVS.GetData(s);
	}

	public void ReadUwV(out Vector4[] L, out Vector4[] rho, out Vector4[] l1, out Vector4[] s, out Vector4[] s2)
	{
		NeedUw("ReadUwV");
		L = new Vector4[N];
		bUwVL.GetData(L);
		rho = new Vector4[N];
		bUwVRho.GetData(rho);
		l1 = new Vector4[N];
		bUwVL1.GetData(l1);
		s = new Vector4[N];
		bUwVS.GetData(s);
		s2 = new Vector4[N];
		bUwVS2.GetData(s2);
	}

	public float[] ReadRhoRate()
	{
		float[] arr = new float[N];
		bRhoRate.GetData(arr);
		return arr;
	}

	public float[] ReadEdotS()
	{
		float[] arr = new float[N];
		bEdotS.GetData(arr);
		return arr;
	}

	public Vector4[] ReadStressRate()
	{
		Vector4[] arr = new Vector4[N];
		bStressRate.GetData(arr);
		return arr;
	}

	public float[] ReadViscMinAll()
	{
		float[] arr = new float[N];
		bViscMin.GetData(arr);
		return arr;
	}

	public uint[] ReadBodyIds()
	{
		uint[] arr = new uint[N];
		bBody.GetData(arr);
		return arr;
	}

	public void Dispose()
	{
		ComputeBuffer[] core = new ComputeBuffer[30]
		{
			bPos, bVelHalf, bVelFull, bAcc, bMass, bRho, bRhoRate, bEInt, bTemp, bEpsP,
			bPlWork, bEdotP, bEdotS, bEShear, bStress, bStressRate, bVelGrad, bMat, bCellId, bCellCount,
			bCellStart, bCellFill, bBlockSums, bOrder, bMaterials, bPartial, bPartialC, bPartial4, bSim, bProbe
		};
		for (int i = 0; i < core.Length; i++)
		{
			if (core[i] != null)
			{
				core[i].Release();
			}
		}
		bEdotVisc?.Release();
		bViscMin?.Release();
		bWorkP?.Release();
		bWorkS?.Release();
		bAccPS?.Release();
		bAccC?.Release();
		bChanP?.Release();
		bChanS?.Release();
		bChanT?.Release();
		bChanC?.Release();
		bDamage?.Release();
		bEDamage?.Release();
		bEpsPPrev?.Release();
		bAccA?.Release();
		bChanA?.Release();
		bSpallFail?.Release();
		bBody?.Release();
		bAnchor?.Release();
		bPartialCon?.Release();
		bClumpInfo?.Release();
		bAccPV?.Release();
		bPowPV?.Release();
		bChanPr?.Release();
		bChanV?.Release();
		bHeatV?.Release();
		bPowAccPr?.Release();
		bPowAccV?.Release();
		bHalfPr?.Release();
		bHalfV?.Release();
		bPowAccPrPlus?.Release();
		bIfAcc?.Release();
		bIfChanPS?.Release();
		bIfChanVA?.Release();
		bIfCount?.Release();
		bIfColour?.Release();
		bIfBand?.Release();
		bIfProbe?.Release();
		bCrossF?.Release();
		bCrossV?.Release();
		bTbProbe?.Release();
		bUwMask?.Release();
		bUwFA?.Release();
		bUwFB?.Release();
		bUwFE?.Release();
		bUwFL1?.Release();
		bUwFL1E?.Release();
		bUwFSnap?.Release();
		bUwVL?.Release();
		bUwVRho?.Release();
		bUwVL1?.Release();
		bUwVS?.Release();
		bUwVS2?.Release();
		bEroded?.Release();
		bErodeLedger?.Release();
		bErodeProbe?.Release();
		bXi?.Release();
		bAsb?.Release();
		bRestrainX0?.Release();
		bRestrainWork?.Release();
		bRestrainMask?.Release();
		bRestrainProbe?.Release();
		bEdotVisc = null;
		bViscMin = null;
		bWorkP = null;
		bWorkS = null;
		bAccPS = null;
		bAccC = null;
		bChanP = null;
		bChanS = null;
		bChanT = null;
		bChanC = null;
		bDamage = null;
		bEDamage = null;
		bEpsPPrev = null;
		bAccA = null;
		bChanA = null;
		bSpallFail = null;
		bBody = null;
		bAnchor = null;
		bPartialCon = null;
		bClumpInfo = null;
		bAccPV = null;
		bPowPV = null;
		bChanPr = null;
		bChanV = null;
		bHeatV = null;
		bPowAccPr = null;
		bPowAccV = null;
		bHalfPr = null;
		bHalfV = null;
		bPowAccPrPlus = null;
		bIfAcc = null;
		bIfChanPS = null;
		bIfChanVA = null;
		bIfCount = null;
		bIfColour = null;
		bIfBand = null;
		bIfProbe = null;
		bCrossF = null;
		bCrossV = null;
		bTbProbe = null;
		bUwMask = null;
		bUwFA = null;
		bUwFB = null;
		bUwFE = null;
		bUwFL1 = null;
		bUwFL1E = null;
		bUwFSnap = null;
		bUwVL = null;
		bUwVRho = null;
		bUwVL1 = null;
		bUwVS = null;
		bUwVS2 = null;
		bEroded = null;
		bErodeLedger = null;
		bErodeProbe = null;
		bXi = null;
		bAsb = null;
		bRestrainX0 = null;
		bRestrainWork = null;
		bRestrainMask = null;
		bRestrainProbe = null;
		if (boundOwner == this)
		{
			boundOwner = null;
		}
		bPos = null;
		bVelHalf = null;
		bVelFull = null;
		bAcc = null;
		bMass = null;
		bRho = null;
		bRhoRate = null;
		bEInt = null;
		bTemp = null;
		bEpsP = null;
		bPlWork = null;
		bEdotP = null;
		bEdotS = null;
		bEShear = null;
		bStress = null;
		bStressRate = null;
		bVelGrad = null;
		bMat = null;
		bCellId = null;
		bCellCount = null;
		bCellStart = null;
		bCellFill = null;
		bOrder = null;
		bBlockSums = null;
		bMaterials = null;
		bPartial = null;
		bPartialC = null;
		bPartial4 = null;
		bSim = null;
		bProbe = null;
	}
}
