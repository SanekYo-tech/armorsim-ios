using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System.Text;

public sealed class SuperScenePlan
{
	public enum Per
	{
		Particle = 0,
		Node = 1,
		Fixed = 2,
		VBlock = 3,
		PoolBlock = 4,
		ReconParticle = 5,
		ReconCell = 6,
		ReconVertex = 7
	}

	public enum Layout
	{
		DenseA = 0,
		DenseB = 1,
		Sparse = 2
	}

	[Flags]
	public enum Where
	{
		DenseA = 1,
		DenseB = 2,
		Sparse = 4,
		Lazy = 8
	}

	public enum Phase
	{
		Particles = 0,
		Grid = 1
	}

	public struct Sizes
	{
		public long particles;

		public long nodes;

		public bool contact;

		public long vblocks;

		public long poolBlocks;

		public long reconOccCells;

		public long reconBricks;

		public long reconBodies;

		public long reconNodes;

		public long reconBlockCells;

		public long reconVerts;

		public Sizes(long particles, long nodes, bool contact)
		{
			this.particles = 0L;
			this.nodes = 0L;
			this.contact = false;
			vblocks = 0L;
			poolBlocks = 0L;
			reconOccCells = 0L;
			reconBricks = 0L;
			reconBodies = 0L;
			reconNodes = 0L;
			reconBlockCells = 0L;
			reconVerts = 0L;
			this.particles = particles;
			this.nodes = nodes;
			this.contact = contact;
		}

		public static Sizes Sparse(long particles, long poolBlocks, long vblocks, bool contact)
		{
			Sizes result = default(Sizes);
			result.particles = particles;
			result.nodes = poolBlocks << 9;
			result.vblocks = vblocks;
			result.poolBlocks = poolBlocks;
			result.contact = contact;
			return result;
		}
	}

	public enum CountRule
	{
		Particles = 0,
		ParticleBlocks = 1,
		Nodes = 2,
		NodesTimes4 = 3,
		NodeBlocks = 4,
		MaterialRows = 5,
		FailSlots = 6,
		Four = 7,
		ParticleBlocksTimes6 = 8,
		ParticleBlocks2Times6 = 9,
		Six = 10,
		Three = 11,
		NodesTimesGridInts = 12,
		NodesTimesGridV = 13,
		VBlocks = 14,
		VBlockGroups = 15,
		VBlockGroups2 = 16,
		PoolBlocks = 17,
		ReconParticlesTimesFields = 18,
		ReconOccCells = 19,
		ReconOccCellsPlus1 = 20,
		ReconScanL1 = 21,
		ReconScanL2 = 22,
		ReconBricks = 23,
		ReconBodies = 24,
		ReconBodiesTimes6 = 25,
		ReconNodes = 26,
		ReconBlockCells = 27,
		ReconBlockCellsTimesFields = 28,
		ReconVerts = 29,
		ReconVertsTimesFields = 30,
		ReconParticleBlocks2 = 31,
		Sixteen = 32,
		ProbeWords = 33,
		ReconBodiesPlus1 = 34,
		ObstacleWords = 35
	}

	public sealed class GpuBuffer
	{
		public readonly string name;

		public readonly Per per;

		public readonly CountRule rule;

		public readonly int stride;

		public readonly bool usedOnRouteA;

		public readonly Where where;

		public readonly Phase phase;

		public readonly bool indirect;

		public readonly string source;

		public readonly Func<bool> plannedWhen;

		private bool Planned => plannedWhen != null && plannedWhen();

		public bool Lazy
		{
			get
			{
				if (plannedWhen != null && plannedWhen())
				{
					return false;
				}
				return (where & Where.Lazy) != 0;
			}
		}

		public double BytesPerUnit => BytesPerUnitFor(SuperKernelGPU.TwoFieldContact);

		public string CountText
		{
			get
			{
				switch (rule)
				{
					case CountRule.Particles:
						return "N";
					case CountRule.ParticleBlocks:
						return "ceil(N/" + 256 + ")";
					case CountRule.Nodes:
						return "nodes";
					case CountRule.NodesTimes4:
						return "nodes×4";
					case CountRule.NodeBlocks:
						return "ceil(nodes/" + 256 + ")";
					case CountRule.MaterialRows:
						return MaterialRowCount.ToString();
					case CountRule.FailSlots:
						return 41 + "+3×" + 1024;
					case CountRule.ParticleBlocksTimes6:
						return "6×ceil(N/" + 256 + ")";
					case CountRule.ParticleBlocks2Times6:
						return "6×ceil(ceil(N/" + 256 + ")/" + 256 + ")";
					case CountRule.Six:
						return "6";
					case CountRule.Three:
						return "3";
					case CountRule.NodesTimesGridInts:
						return "nodes×" + SuperKernelGPU.GridIntsPerNodeFor(SuperKernelGPU.TwoFieldContact);
					case CountRule.NodesTimesGridV:
						if (!SuperKernelGPU.TwoFieldContact)
						{
							return "nodes";
						}
						return "nodes×2";
					case CountRule.VBlocks:
						return "vblocks";
					case CountRule.VBlockGroups:
						return "ceil(vblocks/" + 256 + ")";
					case CountRule.VBlockGroups2:
						return "ceil(ceil(vblocks/" + 256 + ")/" + 256 + ")";
					case CountRule.PoolBlocks:
						return "pool";
					case CountRule.ReconParticlesTimesFields:
						return "5×N";
					case CountRule.ReconOccCells:
						return "occ";
					case CountRule.ReconOccCellsPlus1:
						return "occ+1";
					case CountRule.ReconScanL1:
						return "ceil(occ/" + 256 + ")";
					case CountRule.ReconScanL2:
						return "ceil(ceil(occ/" + 256 + ")/" + 256 + ")";
					case CountRule.ReconBricks:
						return "bricks";
					case CountRule.ReconBodies:
						return "bodies";
					case CountRule.ReconBodiesTimes6:
						return "6×bodies";
					case CountRule.ReconNodes:
						return "nodes·rc";
					case CountRule.ReconBlockCells:
						return "cells·blk";
					case CountRule.ReconBlockCellsTimesFields:
						return "5×cells·blk";
					case CountRule.ReconVerts:
						return "verts";
					case CountRule.ReconVertsTimesFields:
						return "5×verts";
					case CountRule.ReconParticleBlocks2:
						return "ceil(ceil(N/" + 256 + ")/" + 256 + ")";
					case CountRule.Sixteen:
						return "16";
					case CountRule.ProbeWords:
						return "1024";
					case CountRule.ReconBodiesPlus1:
						return "bodies+1";
					case CountRule.ObstacleWords:
						return "ceil(vnodes/32)";
					default:
						return "4";
				}
			}
		}

		public GpuBuffer(string name, Per per, CountRule rule, int stride, bool usedOnRouteA, Where where, Phase phase, string source, bool indirect = false, Func<bool> plannedWhen = null)
		{
			this.name = name;
			this.per = per;
			this.rule = rule;
			this.stride = stride;
			this.usedOnRouteA = usedOnRouteA;
			this.where = where;
			this.phase = phase;
			this.indirect = indirect;
			this.source = source;
			this.plannedWhen = plannedWhen;
		}

		public bool In(Layout l)
		{
			if (plannedWhen != null && plannedWhen())
			{
				return true;
			}
			return (where & (l switch
			{
				Layout.DenseA => Where.DenseA,
				Layout.DenseB => Where.DenseB,
				_ => Where.Sparse
			})) != 0;
		}

		public long Count(in Sizes s)
		{
			switch (rule)
			{
				case CountRule.Particles:
					return s.particles;
				case CountRule.ParticleBlocks:
					return Math.Max(1L, (s.particles + 255) / 256);
				case CountRule.Nodes:
					return s.nodes;
				case CountRule.NodesTimes4:
					return s.nodes * 4;
				case CountRule.NodeBlocks:
					return Math.Max(1L, (s.nodes + 255) / 256);
				case CountRule.MaterialRows:
					return MaterialRowCount;
				case CountRule.FailSlots:
					return 3113L;
				case CountRule.Four:
					return 4L;
				case CountRule.ParticleBlocksTimes6:
					return 6L * Math.Max(1L, (s.particles + 255) / 256);
				case CountRule.ParticleBlocks2Times6:
					return 6L * Math.Max(1L, (Math.Max(1L, (s.particles + 255) / 256) + 255) / 256);
				case CountRule.Six:
					return 6L;
				case CountRule.Three:
					return 3L;
				case CountRule.NodesTimesGridInts:
					return s.nodes * SuperKernelGPU.GridIntsPerNodeFor(s.contact);
				case CountRule.NodesTimesGridV:
					return s.nodes * SuperKernelGPU.GridVPerNodeFor(s.contact);
				case CountRule.VBlocks:
					return s.vblocks;
				case CountRule.VBlockGroups:
					return Math.Max(1L, (s.vblocks + 255) / 256);
				case CountRule.VBlockGroups2:
					return Math.Max(1L, (Math.Max(1L, (s.vblocks + 255) / 256) + 255) / 256);
				case CountRule.PoolBlocks:
					return s.poolBlocks;
				case CountRule.ReconParticlesTimesFields:
					return s.particles * 5;
				case CountRule.ReconOccCells:
					return Math.Max(1L, s.reconOccCells);
				case CountRule.ReconOccCellsPlus1:
					return s.reconOccCells + 1;
				case CountRule.ReconScanL1:
					return Math.Max(1L, (s.reconOccCells + 255) / 256);
				case CountRule.ReconScanL2:
					return Math.Max(1L, (Math.Max(1L, (s.reconOccCells + 255) / 256) + 255) / 256);
				case CountRule.ReconBricks:
					return Math.Max(1L, s.reconBricks);
				case CountRule.ReconBodies:
					return Math.Max(1L, s.reconBodies);
				case CountRule.ReconBodiesTimes6:
					return 6L * Math.Max(1L, s.reconBodies);
				case CountRule.ReconNodes:
					return Math.Max(1L, s.reconNodes);
				case CountRule.ReconBlockCells:
					return Math.Max(1L, s.reconBlockCells);
				case CountRule.ReconBlockCellsTimesFields:
					return Math.Max(1L, s.reconBlockCells) * 5;
				case CountRule.ReconVerts:
					return Math.Max(1L, s.reconVerts);
				case CountRule.ReconVertsTimesFields:
					return Math.Max(1L, s.reconVerts) * 5;
				case CountRule.ReconParticleBlocks2:
					return Math.Max(1L, (Math.Max(1L, (s.particles + 255) / 256) + 255) / 256);
				case CountRule.Sixteen:
					return 16L;
				case CountRule.ProbeWords:
					return 1024L;
				case CountRule.ReconBodiesPlus1:
					return Math.Max(1L, s.reconBodies) + 1;
				case CountRule.ObstacleWords:
				{
					long active = s.nodes;
					if (s.vblocks > 0)
					{
						active = s.vblocks << 9;
					}
					return Math.Max(1L, (active + 31) / 32);
				}
				default:
					throw new InvalidOperationException("SuperScenePlan: правило счёта " + rule.ToString() + " не описано — таблица и код разошлись");
			}
		}

		public long Count(long particleCount, long nodeCount)
		{
			return Count(new Sizes(particleCount, nodeCount, SuperKernelGPU.TwoFieldContact));
		}

		public long Bytes(in Sizes s)
		{
			return Count(in s) * stride;
		}

		public long Bytes(long particleCount, long nodeCount)
		{
			return Count(particleCount, nodeCount) * stride;
		}

		public double BytesPerUnitFor(bool contact)
		{
			switch (rule)
			{
				case CountRule.Particles:
				case CountRule.Nodes:
				case CountRule.VBlocks:
				case CountRule.PoolBlocks:
					return stride;
				case CountRule.ParticleBlocks:
				case CountRule.NodeBlocks:
				case CountRule.VBlockGroups:
					return (double)stride / 256.0;
				case CountRule.NodesTimes4:
					return (double)stride * 4.0;
				case CountRule.ParticleBlocksTimes6:
					return (double)stride * 6.0 / 256.0;
				case CountRule.ParticleBlocks2Times6:
					return (double)stride * 6.0 / 65536.0;
				case CountRule.NodesTimesGridInts:
					return (double)stride * (double)SuperKernelGPU.GridIntsPerNodeFor(contact);
				case CountRule.NodesTimesGridV:
					return (double)stride * (double)SuperKernelGPU.GridVPerNodeFor(contact);
				case CountRule.VBlockGroups2:
					return (double)stride / 65536.0;
				default:
					if (rule == CountRule.ObstacleWords)
					{
						return (double)stride / 32.0;
					}
					return (double)(stride * Count(new Sizes(0L, 0L, contact)));
			}
		}
	}

	public enum Life
	{
		Run = 0,
		Recording = 1,
		Bake = 2,
		Upload = 3,
		Checkpoint = 4,
		Frame = 5
	}

	public sealed class CpuItem
	{
		public readonly string name;

		public readonly int bytesPerParticle;

		public readonly Life life;

		public readonly string source;

		public CpuItem(string name, int bytesPerParticle, Life life, string source)
		{
			this.name = name;
			this.bytesPerParticle = bytesPerParticle;
			this.life = life;
			this.source = source;
		}
	}

	private struct Box
	{
		public Vector3 lo;

		public Vector3 hi;

		public bool empty;

		public Box(bool _)
		{
			lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			empty = true;
		}

		public void Add(Vector3 q)
		{
			lo = new Vector3(Mathf.Min(lo.x, q.x), Mathf.Min(lo.y, q.y), Mathf.Min(lo.z, q.z));
			hi = new Vector3(Mathf.Max(hi.x, q.x), Mathf.Max(hi.y, q.y), Mathf.Max(hi.z, q.z));
			empty = false;
		}

		public void Add(Box b)
		{
			if (b.empty)
			{
				return;
			}
			lo = new Vector3(Mathf.Min(lo.x, b.lo.x), Mathf.Min(lo.y, b.lo.y), Mathf.Min(lo.z, b.lo.z));
			hi = new Vector3(Mathf.Max(hi.x, b.hi.x), Mathf.Max(hi.y, b.hi.y), Mathf.Max(hi.z, b.hi.z));
			empty = false;
		}
	}

	public string kind;

	public float spacingM;

	public float durationS;

	public float marginM;

	public float cellM;

	public long particles;

	public int nx;

	public int ny;

	public int nz;

	public long gridNodes;

	public long gridNodesTight;

	public bool gridCountOverflowsInt;

	public long numVBlocks;

	public long poolBlocksEstimate;

	public long poolBlocksPlanned;

	public float solverMarginM;

	public long gpuBytesGrid;

	public long gpuBytesParticles;

	public long gpuBytesFixed;

	public long gpuBytesTotal;

	public long gpuBytesPeakInferred;

	public long cpuBytesSteady;

	public long cpuBytesPeakEstimate;

	public int steps;

	public float cMax;

	public double solveSecondsPredicted;

	public int vramMB;

	public bool vramKnown;

	public long vramCeilingBytes;

	public bool fitsVram;

	public long gpuBytesRecon;

	public bool fitsVramWithRecon;

	public Layout layout;

	public string largestBuffer;

	public long largestBufferBytes;

	public long resourceCapBytes;

	public bool fitsResourceCap;

	public string note;

	public const int NodesPerBlock = 512;

	public const int BlockEdge = 8;

	public static readonly int MaterialRowCount;

	private const Where All = Where.DenseA | Where.DenseB | Where.Sparse;

	private const Where Dense = Where.DenseA | Where.DenseB;

	public static readonly ReadOnlyCollection<GpuBuffer> GpuBuffers;

	public const string ReconSource = "SuperReconGpu (S39)";

	public static readonly ReadOnlyCollection<GpuBuffer> ReconBuffers;

	public static readonly string[] ReconSlotRows;

	public static readonly string[] ReconRingRows;

	public static readonly string[] ReconTransientRows;

	private static readonly ReadOnlyCollection<CpuItem> CpuItemsBase;

	private static readonly ReadOnlyCollection<CpuItem> CpuItemsWithXi;

	public const int CpuBytesPerLastPlateParticle = 24;

	public const int CpuBytesPerReconCell = 16;

	public const int CpuBytesPerReconParticle = 12;

	public const double S18ErrMean = 0.084;

	public const double S18ErrMin = -0.108;

	public const double S18ErrMax = 0.134;

	public const int S18BandPoints = 301;

	public const string S18BandSource = "MEASURED: Docs/REPORT_SUPER_the_angle_band_2026-09-13.md:214 (301 точка без коротких пакетов, средняя +8.4 %, −10.8…+13.4 %; только счёт, без записи ленты)";

	public const int PoolGrowthReserve = 2;

	public static ReadOnlyCollection<CpuItem> CpuItems => SuperDamage.XiFieldOn ? CpuItemsWithXi : CpuItemsBase;

	public static int VBlocksAxis(int nodesAxis)
	{
		return (nodesAxis + 7) / 8;
	}

	public static GpuBuffer ReconRow(string name)
	{
		string key = name;
		int cut = name.IndexOf('#');
		if (cut >= 0)
		{
			key = name.Substring(0, cut);
		}
		foreach (GpuBuffer b in ReconBuffers)
		{
			if (b.name == key)
			{
				return b;
			}
		}
		throw new InvalidOperationException("SuperScenePlan: строки «" + key + "» в таблице буферов восстановления нет — водитель S39 и таблица разошлись.");
	}

	public static long ReconBytes(in Sizes s, int slots, int ring)
	{
		long total = 0L;
		foreach (GpuBuffer b in ReconBuffers)
		{
			if (Array.IndexOf(ReconTransientRows, b.name) >= 0)
			{
				continue;
			}
			long bytes = b.Bytes(in s);
			if (Array.IndexOf(ReconSlotRows, b.name) >= 0)
			{
				total += slots * bytes;
			}
			else if (Array.IndexOf(ReconRingRows, b.name) >= 0)
			{
				total += ring * bytes;
			}
			else
			{
				total += bytes;
			}
		}
		return total;
	}

	public static bool IsPoolRow(GpuBuffer b)
	{
		if (b.per == Per.PoolBlock)
		{
			return true;
		}
		if (b.per != Per.Node)
		{
			return false;
		}
		return b.In(Layout.Sparse);
	}

	public static GpuBuffer Row(string name)
	{
		foreach (GpuBuffer b in GpuBuffers)
		{
			if (b.name == name)
			{
				return b;
			}
		}
		throw new InvalidOperationException("SuperScenePlan: строки «" + name + "» в таблице буферов нет — ядро и таблица разошлись.");
	}

	public static void GpuBytes(Layout layout, in Sizes s, out long gridBytes, out long particleBytes, out long fixedBytes)
	{
		gridBytes = 0L;
		particleBytes = 0L;
		fixedBytes = 0L;
		foreach (GpuBuffer b in GpuBuffers)
		{
			if (!b.In(layout))
			{
				continue;
			}
			long bytes = b.Bytes(in s);
			switch (b.per)
			{
				case Per.Particle:
					particleBytes += bytes;
					break;
				case Per.Node:
				case Per.VBlock:
				case Per.PoolBlock:
					gridBytes += bytes;
					break;
				default:
					fixedBytes += bytes;
					break;
			}
		}
	}

	public static long GpuBytesTotal(Layout layout, in Sizes s)
	{
		GpuBytes(layout, in s, out long gridBytes, out long particleBytes, out long fixedBytes);
		return fixedBytes + particleBytes + gridBytes;
	}

	public static void GpuBytes(long particleCount, long nodeCount, out long gridBytes, out long particleBytes, out long fixedBytes)
	{
		Sizes s = new Sizes(particleCount, nodeCount, SuperKernelGPU.TwoFieldContact);
		GpuBytes(Layout.DenseA, in s, out gridBytes, out particleBytes, out fixedBytes);
	}

	public static double BytesPerUnit(Layout layout, Per per, bool contact, bool withLazy = false)
	{
		double sum = 0.0;
		foreach (GpuBuffer b in GpuBuffers)
		{
			if (b.per != per)
			{
				continue;
			}
			if (!b.In(layout) && !(withLazy && b.Lazy))
			{
				continue;
			}
			sum += b.BytesPerUnitFor(contact);
		}
		return sum;
	}

	public static double GpuBytesPerParticle(bool routeAOnly)
	{
		if (routeAOnly)
		{
			return BytesPerUnit(Layout.DenseA, Per.Particle, SuperKernelGPU.TwoFieldContact);
		}
		return BytesPerUnit(Layout.DenseB, Per.Particle, SuperKernelGPU.TwoFieldContact, withLazy: true);
	}

	public static double GpuBytesPerNode(bool routeAOnly)
	{
		if (routeAOnly)
		{
			return BytesPerUnit(Layout.DenseA, Per.Node, SuperKernelGPU.TwoFieldContact);
		}
		return BytesPerUnit(Layout.DenseB, Per.Node, SuperKernelGPU.TwoFieldContact, withLazy: true);
	}

	public static string LargestBuffer(Layout layout, in Sizes s, out long bytes)
	{
		bytes = 0L;
		string name = null;
		foreach (GpuBuffer b in GpuBuffers)
		{
			if (!b.In(layout))
			{
				continue;
			}
			long bBytes = b.Bytes(in s);
			if (bBytes > bytes)
			{
				bytes = bBytes;
				name = b.name;
			}
		}
		return name;
	}

	private static long CpuSum(Life life)
	{
		long sum = 0L;
		foreach (CpuItem c in CpuItems)
		{
			if (c.life == life)
			{
				sum += c.bytesPerParticle;
			}
		}
		return sum;
	}

	public static void SolveBand(double predictedS, out double lowS, out double highS)
	{
		lowS = predictedS / 1.134;
		highS = predictedS / 0.892;
	}

	public static void BandFromErrors(double predictedS, double errMin, double errMax, out double lowS, out double highS)
	{
		if (errMax + 1.0 > 0.0)
		{
			lowS = predictedS / (errMax + 1.0);
		}
		else
		{
			lowS = 0.0;
		}
		if (errMin + 1.0 > 0.0)
		{
			highS = predictedS / (errMin + 1.0);
		}
		else
		{
			highS = double.PositiveInfinity;
		}
	}

	public static float GameMarginFor(IList<SuperPlateSpec> plates, float threatSpeedMps, float durationS)
	{
		return SuperPreset.MarginFor(MaxObliquityDeg(plates), threatSpeedMps, durationS);
	}

	public static float MaxObliquityDeg(IList<SuperPlateSpec> plates)
	{
		float max = 0f;
		if (plates == null)
		{
			return max;
		}
		foreach (SuperPlateSpec p in plates)
		{
			float o = (p.hasPose ? SuperScenePose.ObliquityDeg(p.poseN) : Mathf.Abs(p.angleXDeg));
			max = Mathf.Max(max, o);
		}
		return max;
	}

	public static SuperScenePlan For(in SuperRodSpec rod, IList<SuperPlateSpec> plates, float spacingM, float durationS, float marginM, bool gameDomain = false)
	{
		Box projBox = new Box(true);
		float s = SuperBaker.ResolveSpacing(rod.spacingM, spacingM, "стержень");
		RodLattice(rod.diameterM, rod.lengthM, rod.tipZM, s, profiled: true, rod.nose, ref projBox);
		double projVol = SuperCavity.BudgetVolume(in rod, s);
		return Core("rod", projBox, projVol, rod.velocityMps, rod.material, plates, spacingM, durationS, marginM, gameDomain);
	}

	public static SuperScenePlan For(in SuperJetSpec jet, IList<SuperPlateSpec> plates, float spacingM, float durationS, float marginM, bool gameDomain = false)
	{
		Box projBox = new Box(true);
		float s = SuperBaker.ResolveSpacing(jet.spacingM, spacingM, "струя");
		RodLattice(jet.diameterM, jet.lengthM, jet.tipZM, s, profiled: false, NoseShape.Flat, ref projBox);
		double projVol = (double)jet.diameterM * 0.785398185f * jet.diameterM * jet.lengthM;
		return Core("jet", projBox, projVol, jet.vTipMps, jet.material, plates, spacingM, durationS, marginM, gameDomain);
	}

	private static void RodLattice(float diameterM, float lengthM, float tipZM, float s, bool profiled, NoseShape nose, ref Box box)
	{
		float r = diameterM * 0.5f;
		float noseLen = (profiled ? RodGeometry.NoseLengthM(diameterM, lengthM) : 0f);
		int nLen = Math.Max(1, Mathf.RoundToInt(lengthM / s));
		int nDia = Math.Max(1, (int)Math.Ceiling(diameterM / s));
		float xMin = float.MaxValue;
		for (int i = 0; i < nDia; i++)
		{
			xMin = Mathf.Min(xMin, Mathf.Abs((i - (nDia - 1) * 0.5f) * s));
		}
		int first = -1;
		int last = -1;
		float rMax = 0f;
		if (profiled)
		{
			for (int i = 0; i < nLen; i++)
			{
				float rr = RodGeometry.NoseRadiusFactorAt(nose, (i + 0.5f) * s, noseLen, r);
				if (!(2f * xMin * xMin > rr * rr))
				{
					if (first < 0)
					{
						first = i;
					}
					last = i;
					rMax = Mathf.Max(rMax, rr);
				}
			}
		}
		else
		{
			for (int i = 0; i < nLen; i++)
			{
				if (!(2f * xMin * xMin > r * r))
				{
					if (first < 0)
					{
						first = i;
					}
					last = i;
					rMax = Mathf.Max(rMax, r);
				}
			}
		}
		if (first < 0)
		{
			return;
		}
		float rMaxSq = rMax * rMax;
		float xMinSq = xMin * xMin;
		float edge = 0f;
		for (int i = 0; i < nDia; i++)
		{
			float x = (i - (nDia - 1) * 0.5f) * s;
			if (x * x + xMinSq <= rMaxSq)
			{
				edge = Mathf.Max(edge, Mathf.Abs(x));
			}
		}
		box.Add(new Vector3(-edge, -edge, (first + 0.5f) * s + tipZM));
		box.Add(new Vector3(edge, edge, (last + 0.5f) * s + tipZM));
	}

	private static void PlateLattice(SuperPlateSpec pl, float s, ref Box box)
	{
		if (pl.clip.present)
		{
			SuperPlateSpec plain = pl;
			plain.clip = default(SuperClip);
			plain.mesh = null;
			Box plateBox = new Box(true);
			PlateLattice(plain, s, ref plateBox);
			float band = SuperClip.Band * s;
			Box clipBox = new Box(true);
			for (int iu = 0; iu < 2; iu++)
			{
				float eu = (iu == 0) ? (0f - (pl.clip.hu + band)) : (pl.clip.hu + band);
				for (int iv = 0; iv < 2; iv++)
				{
					float ev = (iv == 0) ? (0f - (pl.clip.hv + band)) : (pl.clip.hv + band);
					for (int ik = 0; ik < 2; ik++)
					{
						float ek = (ik == 0) ? (0f - (pl.clip.hn + band)) : (pl.clip.hn + band);
						clipBox.Add(pl.clip.centre + pl.clip.u * eu + pl.clip.v * ev + pl.clip.n * ek);
					}
				}
			}
			if (!plateBox.empty && !clipBox.empty)
			{
				float loX = Mathf.Max(plateBox.lo.x, clipBox.lo.x);
				float loY = Mathf.Max(plateBox.lo.y, clipBox.lo.y);
				float loZ = Mathf.Max(plateBox.lo.z, clipBox.lo.z);
				float hiX = Mathf.Min(plateBox.hi.x, clipBox.hi.x);
				float hiY = Mathf.Min(plateBox.hi.y, clipBox.hi.y);
				float hiZ = Mathf.Min(plateBox.hi.z, clipBox.hi.z);
				if (!(loX > hiX || loY > hiY || loZ > hiZ))
				{
					box.Add(new Vector3(loX, loY, loZ));
					box.Add(new Vector3(hiX, hiY, hiZ));
				}
			}
		}
		else if (pl.hasPose)
		{
			SuperBaker.PlateFrame(in pl, out Vector3 centre, out Vector3 u, out Vector3 v, out Vector3 n, out float halfU, out float halfV);
			int nT = Math.Max(1, Mathf.RoundToInt(pl.thicknessM / s));
			int nW = Math.Max(1, Mathf.RoundToInt(pl.widthM / s));
			int nH = Math.Max(1, Mathf.RoundToInt(pl.heightM / s));
			float extra = (pl.fineCoreRadiusM > 0f) ? (s * 0.25f) : 0f;
			float halfT = pl.thicknessM * 0.5f;
			float t0 = s * 0.5f - halfT - extra;
			float t1 = (nT - 0.5f) * s - halfT + extra;
			float euMax = (nW - 1) * 0.5f * s + extra;
			float evMax = (nH - 1) * 0.5f * s + extra;
			for (int i = 0; i < 2; i++)
			{
				float eu = (i == 0) ? (0f - euMax) : euMax;
				for (int j = 0; j < 2; j++)
				{
					float ev = (j == 0) ? (0f - evMax) : evMax;
					for (int k = 0; k < 2; k++)
					{
						float et = (k == 0) ? t0 : t1;
						box.Add(centre + u * eu + v * ev + n * et);
					}
				}
			}
		}
		else
		{
			int nT2 = Math.Max(1, Mathf.RoundToInt(pl.thicknessM / s));
			int nSide = Math.Max(1, Mathf.RoundToInt(pl.sideM / s));
			float extra2 = (pl.fineCoreRadiusM > 0f) ? (s * 0.25f) : 0f;
			float a = pl.angleXDeg * 0.0174532924f;
			float sinA = Mathf.Sin(a);
			float cosA = Mathf.Cos(a);
			Vector3 c = SuperBaker.PlateCentre(pl);
			float halfT2 = pl.thicknessM * 0.5f;
			float t02 = s * 0.5f - halfT2 - extra2;
			float t12 = (nT2 - 0.5f) * s - halfT2 + extra2;
			float inPlane = (nSide - 1) * 0.5f * s + extra2;
			for (int i2 = 0; i2 < 2; i2++)
			{
				float ex = (i2 == 0) ? (0f - inPlane) : inPlane;
				for (int j2 = 0; j2 < 2; j2++)
				{
					float ev2 = (j2 == 0) ? (0f - inPlane) : inPlane;
					for (int k2 = 0; k2 < 2; k2++)
					{
						float et2 = (k2 == 0) ? t02 : t12;
						box.Add(new Vector3(c.x + ex, c.y + ev2 * sinA - et2 * cosA, c.z + et2 * sinA + ev2 * cosA));
					}
				}
			}
		}
	}

	private static long BlocksTouchedBy(Box b, Vector3 origin, float h, int nx, int ny, int nz)
	{
		if (b.empty)
		{
			return 0L;
		}
		return AxisBlocks(b.lo.x, b.hi.x, origin.x, nx) * AxisBlocks(b.lo.y, b.hi.y, origin.y, ny) * AxisBlocks(b.lo.z, b.hi.z, origin.z, nz);

		long AxisBlocks(float lo, float hi, float o, int n)
		{
			int i0 = (int)Math.Floor((lo - o) / h - 0.5f);
			int i1 = (int)Math.Floor((hi - o) / h - 0.5f);
			i0 = Mathf.Clamp(i0, 0, n - 1);
			int t = Mathf.Clamp(i1 + 2, 0, n - 1);
			return t / 8 - i0 / 8 + 1;
		}
	}

	private static Sizes ReconPlanSizes(long particles, List<Box> boxes, float spacing)
	{
		Sizes s = new Sizes(particles, 0L, SuperKernelGPU.TwoFieldContact);
		s.reconBodies = boxes.Count;
		s.reconOccCells = particles * 4;
		long reconBricks = 0L;
		long reconNodes = 0L;
		long reconBlockCells = 0L;
		long reconVerts = 0L;
		foreach (Box b in boxes)
		{
			if (b.empty)
			{
				continue;
			}
			float dx = b.hi.x - b.lo.x;
			float dy = b.hi.y - b.lo.y;
			float dz = b.hi.z - b.lo.z;
			int cX = Mathf.CeilToInt(dx / spacing) + 7;
			int cY = Mathf.CeilToInt(dy / spacing) + 7;
			int cZ = Mathf.CeilToInt(dz / spacing) + 7;
			reconBricks += ((cX + 7) >> 3) * ((cY + 7) >> 3) * ((cZ + 7) >> 3);
			SuperReconMath.BlockDims(new Vector3((float)cX * spacing, (float)cY * spacing, (float)cZ * spacing), out int bnx, out int bny, out int bnz);
			reconBlockCells += (long)bnz * bny * bnx;
			reconNodes += bnz + bny + bnx;
			reconVerts = Math.Max(reconVerts, (long)(2.0 * ((double)dx * dy + (double)dy * dz + (double)dz * dx) / ((double)SuperReconstruct.DecimateCellM * SuperReconstruct.DecimateCellM)));
		}
		s.reconBricks = reconBricks;
		s.reconBlockCells = reconBlockCells;
		s.reconNodes = reconNodes;
		s.reconVerts = Math.Max(1L, reconVerts);
		return s;
	}

	private static long ReconCells(Box b, float sceneSpacing)
	{
		if (b.empty)
		{
			return 0L;
		}
		return (long)((int)Math.Ceiling((b.hi.z - b.lo.z) / sceneSpacing) + 7) * ((long)((int)Math.Ceiling((b.hi.y - b.lo.y) / sceneSpacing) + 7)) * ((long)((int)Math.Ceiling((b.hi.x - b.lo.x) / sceneSpacing) + 7));
	}

	private static SuperScenePlan Core(string kind, Box projBox, double projVol, float projSpeed, string projMaterial, IList<SuperPlateSpec> plates, float spacingM, float durationS, float marginM, bool gameDomain = false)
	{
		if (spacingM <= 0f)
		{
			throw new InvalidOperationException("SuperScenePlan: шаг частиц " + spacingM + " — неположительный шаг сцены не имеет плана.");
		}
		SuperScenePlan plan = new SuperScenePlan();
		plan.kind = kind;
		plan.spacingM = spacingM;
		plan.durationS = durationS;
		StringBuilder note = new StringBuilder();
		if (!(marginM > 0f))
		{
			marginM = SuperShot.MarginM;
		}
		plan.marginM = marginM;
		plan.cellM = spacingM * 2f;
		Box scene = new Box(true);
		scene.Add(projBox);
		long particles = SuperBaker.EstimateParticles(projVol, plates, spacingM);
		List<Box> boxes = new List<Box>();
		boxes.Add(projBox);
		long reconCpu = ReconCells(projBox, spacingM) * CpuBytesPerReconCell + particles * CpuBytesPerReconParticle;
		long lastPlateParticles = 0L;
		ContinuumProperties projMat = ContinuumMaterials.Get(SuperBaker.Material(projMaterial));
		float cMax = projMat.LongitudinalWaveSpeed;
		int n = plates?.Count ?? 0;
		for (int i = 0; i < n; i++)
		{
			SuperPlateSpec pl = plates[i];
			if (pl.hasPose)
			{
				SuperBaker.ValidatePose(in pl, "плита " + (i + 1));
			}
			float s = SuperBaker.ResolveSpacing(pl.spacingM, spacingM, "плита " + (i + 1));
			Box pb = new Box(true);
			PlateLattice(pl, s, ref pb);
			scene.Add(pb);
			boxes.Add(pb);
			long pCount = SuperBaker.EstimateParticles(0.0, new SuperPlateSpec[1] { pl }, spacingM);
			reconCpu = Math.Max(reconCpu, ReconCells(pb, spacingM) * CpuBytesPerReconCell + pCount * CpuBytesPerReconParticle);
			if (i == n - 1)
			{
				lastPlateParticles = pCount;
			}
			ContinuumProperties m = ContinuumMaterials.Get(SuperBaker.Material(pl.material));
			cMax = Mathf.Max(cMax, m.LongitudinalWaveSpeed);
		}
		if (scene.empty)
		{
			throw new InvalidOperationException("SuperScenePlan: у сцены нет ни одной частицы по раскладке запекателя — плана у пустой сцены нет.");
		}
		if (n == 0)
		{
			note.Append("плит нет (ReadScene такую сцену отказывает); ");
		}
		float mrg = plan.marginM;
		Vector3 lo = new Vector3(scene.lo.x - mrg, scene.lo.y - mrg, scene.lo.z - mrg);
		Vector3 hi = new Vector3(scene.hi.x + mrg, scene.hi.y + mrg, scene.hi.z + mrg);
		plan.solverMarginM = SuperShot.SolverMarginFor(gameDomain, lo, hi, plan.cellM, plan.marginM, projSpeed, durationS, out long vblocks);
		if (plan.solverMarginM != plan.marginM)
		{
			float dm = plan.solverMarginM - plan.marginM;
			lo = new Vector3(lo.x - dm, lo.y - dm, lo.z - dm);
			hi = new Vector3(hi.x + dm, hi.y + dm, hi.z + dm);
			note.Append(string.Format("область решателя шире ящика показа: запас {0:F0} мм против габарита ", plan.solverMarginM * 1000f) + string.Format("{0:F0} мм (S34 P3); ", plan.marginM * 1000f));
		}
		SuperGrid.Dims(lo, hi, plan.cellM, out plan.nx, out plan.ny, out plan.nz);
		plan.gridNodes = SuperGrid.NodeCountFor(lo, hi, plan.cellM);
		plan.gridNodesTight = SuperGrid.NodeCountFor(scene.lo, scene.hi, plan.cellM);
		int gridInts = SuperKernelGPU.GridIntsPerNodeFor(SuperKernelGPU.TwoFieldContact);
		plan.particles = SuperBaker.EstimateParticles(projVol, plates, spacingM);
		plan.layout = SuperMemory.LayoutFor(SuperScatterRoute.FixedPointAtomics);
		Sizes full;
		Sizes tight;
		if (plan.layout == Layout.Sparse)
		{
			Vector3 gridOrigin = SuperGrid.OriginFor(lo, plan.cellM);
			long touched = 0L;
			foreach (Box pb2 in boxes)
			{
				if (!pb2.empty)
				{
					touched += BlocksTouchedBy(pb2, gridOrigin, plan.cellM, plan.nx, plan.ny, plan.nz);
				}
			}
			plan.poolBlocksEstimate = SuperKernelGPU.InitialPoolBlocks(touched);
			plan.poolBlocksPlanned = plan.poolBlocksEstimate * PoolGrowthReserve;
			plan.numVBlocks = (long)VBlocksAxis(plan.nz) * VBlocksAxis(plan.ny) * VBlocksAxis(plan.nx);
			SuperGrid.Dims(scene.lo, scene.hi, plan.cellM, out int nxT, out int nyT, out int nzT);
			full = Sizes.Sparse(plan.particles, plan.poolBlocksPlanned, plan.numVBlocks, SuperKernelGPU.TwoFieldContact);
			tight = Sizes.Sparse(0L, 0L, (long)VBlocksAxis(nzT) * VBlocksAxis(nyT) * VBlocksAxis(nxT), SuperKernelGPU.TwoFieldContact);
			plan.gridCountOverflowsInt = (long)gridInts * plan.poolBlocksPlanned * 512 >= int.MaxValue || plan.numVBlocks > int.MaxValue;
			if (plan.gridCountOverflowsInt)
			{
				note.Append(string.Format("разреженная сетка: пул {0} блоков × {1}×{2} int или ", plan.poolBlocksPlanned, 4, gridInts) + string.Format("{0} виртуальных блоков переполняет int — решатель откажет до памяти; ", plan.numVBlocks));
			}
			note.Append(string.Format("разреженная сетка: {0} виртуальных блоков 8³ ({1}×{2}×{3} узлов), ", plan.numVBlocks, plan.nx, plan.ny, plan.nz) + string.Format("пул по оценке {0} блоков × запас {1} (ASSUMED) = ", plan.poolBlocksEstimate, PoolGrowthReserve) + string.Format("{0}; ", plan.poolBlocksPlanned));
		}
		else
		{
			full = new Sizes(plan.particles, plan.gridNodes, SuperKernelGPU.TwoFieldContact);
			tight = new Sizes(0L, plan.gridNodesTight, SuperKernelGPU.TwoFieldContact);
			long nodesTimesGridInts = (long)gridInts * plan.gridNodes;
			plan.gridCountOverflowsInt = nodesTimesGridInts > int.MaxValue;
			if (nodesTimesGridInts > int.MaxValue)
			{
				note.Append(string.Format("сетка {0}×{1}×{2}: nodes×{3} переполняет int — решатель откажет до памяти; ", plan.nx, plan.ny, plan.nz, gridInts));
			}
		}
		GpuBytes(plan.layout, in full, out plan.gpuBytesGrid, out plan.gpuBytesParticles, out plan.gpuBytesFixed);
		plan.gpuBytesTotal = plan.gpuBytesFixed + plan.gpuBytesParticles + plan.gpuBytesGrid;
		GpuBytes(plan.layout, in tight, out long tightGrid, out long tightParticles, out long tightFixed);
		plan.gpuBytesPeakInferred = plan.gpuBytesTotal + plan.particles * 36 + tightGrid;
		plan.largestBuffer = LargestBuffer(plan.layout, in full, out plan.largestBufferBytes);
		plan.resourceCapBytes = SuperMemory.PerResourceCapBytes;
		if (SuperReconGpu.Enabled)
		{
			Sizes reconSizes = ReconPlanSizes(plan.particles, boxes, spacingM);
			plan.gpuBytesRecon = ReconBytes(in reconSizes, 4, 4);
			note.Append(string.Format("S39: восстановление полей на GPU ≈ {0:F0} МиБ при {1} слотах ", plan.gpuBytesRecon / 1048576.0, 4) + "(оценка до запекания, ASSUMED; в отказ сцены не входит — при нехватке путь полей ЦП); ");
		}
		plan.cMax = cMax;
		int steps = SuperCostEstimate.Steps(spacingM, durationS, cMax, projSpeed);
		if (steps < 1)
		{
			steps = 1;
		}
		plan.steps = steps;
		plan.solveSecondsPredicted = SuperCostEstimate.WallSeconds(plan.particles, plan.layout == Layout.Sparse ? (plan.poolBlocksEstimate << 9) : plan.gridNodes, steps);
		plan.vramMB = SystemInfo.graphicsMemorySize;
		plan.vramKnown = SuperMemory.DeviceBytes > 0;
		plan.vramCeilingBytes = SuperMemory.AllowedBytes;
		if (!plan.vramKnown)
		{
			plan.fitsVram = true;
		}
		else
		{
			plan.fitsVram = plan.gpuBytesTotal <= plan.vramCeilingBytes;
		}
		if (!plan.vramKnown)
		{
			plan.fitsVramWithRecon = true;
		}
		else
		{
			plan.fitsVramWithRecon = plan.gpuBytesRecon + plan.gpuBytesTotal <= plan.vramCeilingBytes;
		}
		if (!plan.vramKnown)
		{
			plan.fitsResourceCap = true;
		}
		else
		{
			plan.fitsResourceCap = plan.largestBufferBytes <= plan.resourceCapBytes;
		}
		if (!plan.fitsResourceCap)
		{
			note.Append(string.Format("буфер {0} = {1:F0} МиБ выше потолка одного ресурса ", plan.largestBuffer, plan.largestBufferBytes / 1048576.0) + string.Format("{0:F0} МиБ — решатель откажет по имени буфера; ", plan.resourceCapBytes / 1048576.0));
		}
		if (!plan.vramKnown)
		{
			note.Append("graphicsMemorySize = 0: устройство не сообщило объём — сторож решателя в этом случае пропускает, план тоже; ");
		}
		long runBytes = CpuSum(Life.Run);
		plan.cpuBytesSteady = (CpuSum(Life.Recording) + runBytes) * plan.particles + lastPlateParticles * CpuBytesPerLastPlateParticle;
		long bakeBytes = (CpuSum(Life.Bake) + runBytes) * plan.particles;
		long uploadBytes = (runBytes + CpuSum(Life.Upload)) * plan.particles + lastPlateParticles * CpuBytesPerReconParticle;
		long checkpointBytes = (CpuSum(Life.Checkpoint) + CpuSum(Life.Frame)) * plan.particles + plan.cpuBytesSteady + reconCpu;
		plan.cpuBytesPeakEstimate = Math.Max(bakeBytes, Math.Max(uploadBytes, checkpointBytes));
		bool hasFineCore = false;
		for (int i2 = 0; i2 < n; i2++)
		{
			SuperPlateSpec pl2 = plates[i2];
			if (pl2.fineCoreRadiusM > 0f)
			{
				hasFineCore = true;
			}
		}
		if (hasFineCore)
		{
			note.Append("мелкое ядро S18: габарит — верхняя оценка (до s/4 на сторону); ");
		}
		note.Append("габарит по раскладке запекателя, допуск ±1 узел на ось; раскладка " + plan.layout + ": " + string.Format("{0:0.###} Б/узел + ", BytesPerUnit(plan.layout, Per.Node, SuperKernelGPU.TwoFieldContact)) + string.Format("{0:0.###} Б/частицу, допущено ", BytesPerUnit(plan.layout, Per.Particle, SuperKernelGPU.TwoFieldContact)) + string.Format("{0:P0} (ASSUMED); ", 0.8f) + "пик GPU и ОЗУ — INFERRED; бюджет D3D12 игрока (Player.log «App VRAM Budget») API не отдаёт.");
		plan.note = note.ToString();
		return plan;
	}

	public string LogLine()
	{
		SolveBand(solveSecondsPredicted, out double lowS, out double highS);
		return string.Format("[Super] План ({0}): частиц {1}, сетка {2}×{3}×{4} = {5} узлов ", kind, particles, nx, ny, nz, gridNodes) + string.Format("(ячейка {0:F2} мм, габарит {1:F0} мм); видеопамять ", cellM * 1000f, marginM * 1000f) + string.Format("{0:F1} МиБ (сетка {1:F1}, частицы ", gpuBytesTotal / 1048576.0, gpuBytesGrid / 1048576.0) + string.Format("{0:F1}, постоянные {1:F1} КиБ) против потолка ", gpuBytesParticles / 1048576.0, gpuBytesFixed / 1024.0) + string.Format("{0:F1} МиБ из {1} МиБ ⇒ {2}; ", gpuBytesPeakInferred / 1048576.0, vramMB, fitsVram ? "влезает" : "НЕ ВЛЕЗАЕТ") + string.Format("ОЗУ {0:F0}…{1:F0} МиБ; шагов {2}, ", cpuBytesSteady / 1048576.0, cpuBytesPeakEstimate / 1048576.0, steps) + string.Format("счёт {0:F0}…{1:F0} с (S18, без ленты). {2}", lowS, highS, note);
	}

	public static string MemoryTable()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(Localization.T("super.mem.title"));
		sb.AppendLine();
		sb.AppendLine(Localization.T("super.mem.gpu_header"));
		foreach (GpuBuffer b in GpuBuffers)
		{
			string perText;
			switch (b.per)
			{
				case Per.Particle:
					perText = Localization.T("super.mem.per.particle");
					break;
				case Per.Node:
					perText = Localization.T("super.mem.per.node");
					break;
				case Per.VBlock:
					perText = Localization.T("super.mem.per.vblock");
					break;
				case Per.PoolBlock:
					perText = Localization.T("super.mem.per.poolblock");
					break;
				default:
					perText = Localization.T("super.mem.per.fixed");
					break;
			}
			string sizeText;
			if (b.per == Per.Fixed)
			{
				sizeText = b.Bytes(0L, 0L).ToString();
			}
			else
			{
				sizeText = b.BytesPerUnit.ToString("0.###");
			}
			sb.Append("  ");
			sb.Append(Localization.T("super.mem.row", b.name, b.CountText, b.stride, sizeText, perText));
			sb.Append(" · ");
			if (b.Lazy)
			{
				sb.Append(Localization.T("super.mem.lazy"));
			}
			else if (!b.usedOnRouteA)
			{
				sb.Append(Localization.T("super.mem.route_a_unused"));
			}
			else if (!b.In(Layout.Sparse))
			{
				sb.Append(Localization.T("super.mem.dense_only"));
			}
			else if (!b.In(Layout.DenseA))
			{
				sb.Append(Localization.T("super.mem.sparse_only"));
			}
			sb.AppendLine();
		}
		Layout layout = SuperMemory.LayoutFor(SuperScatterRoute.FixedPointAtomics);
		Sizes sizes = new Sizes(0L, 0L, SuperKernelGPU.TwoFieldContact);
		GpuBytes(layout, in sizes, out long gridBytes, out long particleBytes, out long fixedBytes);
		sb.AppendLine(Localization.T("super.mem.total_particle", GpuBytesPerParticle(routeAOnly: false).ToString("0.###"), BytesPerUnit(layout, Per.Particle, SuperKernelGPU.TwoFieldContact).ToString("0.###")));
		sb.AppendLine(Localization.T("super.mem.total_node", GpuBytesPerNode(routeAOnly: false).ToString("0.###"), BytesPerUnit(layout, Per.Node, SuperKernelGPU.TwoFieldContact).ToString("0.###")));
		sb.AppendLine(Localization.T("super.mem.total_sparse", (BytesPerUnit(Layout.Sparse, Per.Node, SuperKernelGPU.TwoFieldContact) * 512.0 + BytesPerUnit(Layout.Sparse, Per.PoolBlock, SuperKernelGPU.TwoFieldContact)).ToString("0.###"), BytesPerUnit(Layout.Sparse, Per.VBlock, SuperKernelGPU.TwoFieldContact).ToString("0.###"), layout.ToString()));
		sb.AppendLine(Localization.T("super.mem.total_fixed", fixedBytes));
		sb.AppendLine();
		sb.AppendLine(Localization.T("super.mem.cpu_header"));
		foreach (CpuItem c in CpuItems)
		{
			sb.Append("  ");
			sb.AppendLine(Localization.T("super.mem.cpu_row", c.name, c.bytesPerParticle, LifeText(c.life)));
		}
		sb.Append("  ");
		sb.AppendLine(Localization.T("super.mem.cpu_plate", CpuBytesPerLastPlateParticle));
		sb.Append("  ");
		sb.AppendLine(Localization.T("super.mem.cpu_recon", CpuBytesPerReconCell, CpuBytesPerReconParticle));
		sb.AppendLine();
		sb.AppendLine(Localization.T("super.mem.growth_header"));
		sb.Append("  ");
		sb.AppendLine(Localization.T("super.mem.growth_grid", 2, 2));
		sb.Append("  ");
		sb.AppendLine(Localization.T("super.mem.growth_spacing"));
		sb.Append("  ");
		sb.AppendLine(Localization.T("super.mem.growth_extent"));
		sb.Append("  ");
		sb.AppendLine(Localization.T("super.mem.ceiling", 80f.ToString("0")));
		return sb.ToString();
	}

	private static string LifeText(Life l)
	{
		switch (l)
		{
			case Life.Recording:
				return Localization.T("super.mem.life.recording");
			case Life.Bake:
				return Localization.T("super.mem.life.bake");
			case Life.Upload:
				return Localization.T("super.mem.life.upload");
			case Life.Checkpoint:
				return Localization.T("super.mem.life.checkpoint");
			case Life.Frame:
				return Localization.T("super.mem.life.frame");
			default:
				return Localization.T("super.mem.life.run");
		}
	}

	static SuperScenePlan()
	{
		MaterialRowCount = Enum.GetValues(typeof(ContinuumMaterialId)).Length;
		GpuBuffers = Array.AsReadOnly(new GpuBuffer[42]
		{
			new GpuBuffer("Pos", Per.Particle, CountRule.Particles, 12, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("Vel", Per.Particle, CountRule.Particles, 12, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("C", Per.Particle, CountRule.Particles, 36, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("F", Per.Particle, CountRule.Particles, 36, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("S", Per.Particle, CountRule.Particles, 36, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("Sigma", Per.Particle, CountRule.Particles, 36, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("Scal", Per.Particle, CountRule.Particles, 16, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("Mass", Per.Particle, CountRule.Particles, 4, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("V0", Per.Particle, CountRule.Particles, 4, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("Mat", Per.Fixed, CountRule.MaterialRows, 132, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("MatId", Per.Particle, CountRule.Particles, 4, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("Pinned", Per.Particle, CountRule.Particles, 4, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("NoFrag", Per.Particle, CountRule.Particles, 4, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("Dmg", Per.Particle, CountRule.Particles, 4, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("Cause", Per.Particle, CountRule.Particles, 4, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("Fail", Per.Fixed, CountRule.FailSlots, 4, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("Guard", Per.Fixed, CountRule.Four, 4, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("Escaped", Per.Fixed, CountRule.Four, 4, usedOnRouteA: true, All, Phase.Particles, "SuperKernelGPU.AllocateParticles"),
			new GpuBuffer("Eval", Per.Particle, CountRule.Particles, 8, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperKernelGPU.EnsureEval (лениво)"),
			new GpuBuffer("Partial", Per.Particle, CountRule.ParticleBlocks, 4, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperKernelGPU.EnsurePartial (лениво)"),
			new GpuBuffer("Xi", Per.Particle, CountRule.Particles, 4, usedOnRouteA: true, Where.Lazy, Phase.Particles, "SuperKernelGPU.AllocateParticles (S32: только при ξ ВКЛ; S43: плановая при поле ξ ВКЛ)", plannedWhen: () => SuperDamage.XiFieldOn),
			new GpuBuffer("BoundsA", Per.Particle, CountRule.ParticleBlocksTimes6, 4, usedOnRouteA: true, Dense, Phase.Particles, "SuperKernelGPU.AllocateParticles (S25 P3.2)"),
			new GpuBuffer("BoundsB", Per.Particle, CountRule.ParticleBlocks2Times6, 4, usedOnRouteA: true, Dense, Phase.Particles, "SuperKernelGPU.AllocateParticles (S25 P3.2)"),
			new GpuBuffer("Region", Per.Fixed, CountRule.Six, 4, usedOnRouteA: true, Dense, Phase.Particles, "SuperKernelGPU.AllocateParticles (S25 P3.2)"),
			new GpuBuffer("RegionArgs", Per.Fixed, CountRule.Three, 4, usedOnRouteA: true, Dense, Phase.Particles, "SuperKernelGPU.AllocateParticles (S25 P3.2)", indirect: true),
			new GpuBuffer("GridI", Per.Node, CountRule.NodesTimesGridInts, 4, usedOnRouteA: true, All, Phase.Grid, "SuperKernelGPU.Build (S35: ×4 / ×12)"),
			new GpuBuffer("GridF", Per.Node, CountRule.Nodes, 16, usedOnRouteA: false, Where.DenseB, Phase.Grid, "SuperKernelGPU.Build"),
			new GpuBuffer("GridV", Per.Node, CountRule.NodesTimesGridV, 16, usedOnRouteA: true, All, Phase.Grid, "SuperKernelGPU.Build (S35: ×1 / ×2)"),
			new GpuBuffer("CellId", Per.Particle, CountRule.Particles, 4, usedOnRouteA: false, Where.DenseB, Phase.Grid, "SuperKernelGPU.Build"),
			new GpuBuffer("CellCount", Per.Node, CountRule.Nodes, 4, usedOnRouteA: false, Where.DenseB, Phase.Grid, "SuperKernelGPU.Build"),
			new GpuBuffer("CellStart", Per.Node, CountRule.Nodes, 4, usedOnRouteA: false, Where.DenseB, Phase.Grid, "SuperKernelGPU.Build"),
			new GpuBuffer("CellFill", Per.Node, CountRule.Nodes, 4, usedOnRouteA: false, Where.DenseB, Phase.Grid, "SuperKernelGPU.Build"),
			new GpuBuffer("Order", Per.Particle, CountRule.Particles, 4, usedOnRouteA: false, Where.DenseB, Phase.Grid, "SuperKernelGPU.Build"),
			new GpuBuffer("BlockSums", Per.Node, CountRule.NodeBlocks, 4, usedOnRouteA: false, Where.DenseB, Phase.Grid, "SuperKernelGPU.Build"),
			new GpuBuffer("BlockMark", Per.VBlock, CountRule.VBlocks, 4, usedOnRouteA: true, Where.Sparse, Phase.Grid, "SuperKernelGPU.Build (S34 P2)"),
			new GpuBuffer("BlockSlot", Per.VBlock, CountRule.VBlocks, 4, usedOnRouteA: true, Where.Sparse, Phase.Grid, "SuperKernelGPU.Build (S34 P2)"),
			new GpuBuffer("VSums1", Per.VBlock, CountRule.VBlockGroups, 4, usedOnRouteA: true, Where.Sparse, Phase.Grid, "SuperKernelGPU.Build (S34 P2)"),
			new GpuBuffer("VSums1Scan", Per.VBlock, CountRule.VBlockGroups, 4, usedOnRouteA: true, Where.Sparse, Phase.Grid, "SuperKernelGPU.Build (S34 P2)"),
			new GpuBuffer("VSums2", Per.VBlock, CountRule.VBlockGroups2, 4, usedOnRouteA: true, Where.Sparse, Phase.Grid, "SuperKernelGPU.Build (S34 P2)"),
			new GpuBuffer("ActiveList", Per.PoolBlock, CountRule.PoolBlocks, 4, usedOnRouteA: true, Where.Sparse, Phase.Grid, "SuperKernelGPU.Build / рост пула (S34 P2)"),
			new GpuBuffer("BlockArgs", Per.Fixed, CountRule.Three, 4, usedOnRouteA: true, Where.Sparse, Phase.Grid, "SuperKernelGPU.Build (S34 P2)", indirect: true),
			new GpuBuffer("Obst", Per.Node, CountRule.ObstacleWords, 4, usedOnRouteA: true, Where.Lazy, Phase.Grid, "SuperKernelGPU.Build (S40c: лениво — сцена с декором)")
		});
		ReconBuffers = Array.AsReadOnly(new GpuBuffer[25]
		{
			new GpuBuffer("RcPos", Per.ReconParticle, CountRule.Particles, 12, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperReconGpu (S39): слот — положения кадра"),
			new GpuBuffer("RcPf", Per.ReconParticle, CountRule.ReconParticlesTimesFields, 4, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperReconGpu (S39): слот — 5 полей по каналам [c·N+p]"),
			new GpuBuffer("RcOrder", Per.ReconParticle, CountRule.Particles, 4, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperReconGpu (S39): слот — порядок частиц хэша"),
			new GpuBuffer("RcSlotStart", Per.ReconCell, CountRule.ReconOccCellsPlus1, 4, usedOnRouteA: false, Where.Lazy, Phase.Grid, "SuperReconGpu (S39): слот — начала клеток (+1 страж)"),
			new GpuBuffer("RcBrick", Per.ReconCell, CountRule.ReconBricks, 4, usedOnRouteA: false, Where.Lazy, Phase.Grid, "SuperReconGpu (S39): слот — ранг кирпича 8³ (−1 пусто)"),
			new GpuBuffer("RcBody", Per.Fixed, CountRule.ReconBodies, 80, usedOnRouteA: false, Where.Lazy, Phase.Grid, "SuperReconGpu (S39): слот — таблица тел"),
			new GpuBuffer("RcKeys", Per.ReconParticle, CountRule.Particles, 4, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperReconGpu (S39): ключи ЦП (глобальная клетка)"),
			new GpuBuffer("RcSlotCnt", Per.ReconCell, CountRule.ReconOccCells, 4, usedOnRouteA: false, Where.Lazy, Phase.Grid, "SuperReconGpu (S39): счёт клеток"),
			new GpuBuffer("RcScanL1", Per.ReconCell, CountRule.ReconScanL1, 4, usedOnRouteA: false, Where.Lazy, Phase.Grid, "SuperReconGpu (S39): суммы групп, уровень 1"),
			new GpuBuffer("RcScanL2", Per.ReconCell, CountRule.ReconScanL2, 4, usedOnRouteA: false, Where.Lazy, Phase.Grid, "SuperReconGpu (S39): суммы групп, уровень 2"),
			new GpuBuffer("RcBoundsA", Per.ReconParticle, CountRule.ParticleBlocks, 28, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperReconGpu (S39): редукция габарита, уровень 1"),
			new GpuBuffer("RcBoundsB", Per.ReconParticle, CountRule.ReconParticleBlocks2, 28, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperReconGpu (S39): редукция габарита, уровень 2"),
			new GpuBuffer("RcBoundsOut", Per.Fixed, CountRule.ReconBodies, 28, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperReconGpu (S39): габарит и число частиц по телам"),
			new GpuBuffer("RcNode", Per.ReconCell, CountRule.ReconNodes, 8, usedOnRouteA: false, Where.Lazy, Phase.Grid, "SuperReconGpu (S39): таблицы узлов блока (клетка, координата)"),
			new GpuBuffer("RcBlock", Per.ReconCell, CountRule.ReconBlockCellsTimesFields, 4, usedOnRouteA: false, Where.Lazy, Phase.Grid, "SuperReconGpu (S39): блок полей [c·BT+ячейка]"),
			new GpuBuffer("RcMask0", Per.ReconCell, CountRule.ReconBlockCells, 4, usedOnRouteA: false, Where.Lazy, Phase.Grid, "SuperReconGpu (S39): заполненность, чётные проходы"),
			new GpuBuffer("RcMask1", Per.ReconCell, CountRule.ReconBlockCells, 4, usedOnRouteA: false, Where.Lazy, Phase.Grid, "SuperReconGpu (S39): заполненность, нечётные проходы"),
			new GpuBuffer("RcExtent", Per.Fixed, CountRule.ReconBodiesTimes6, 4, usedOnRouteA: false, Where.Lazy, Phase.Grid, "SuperReconGpu (S39): габарит заполненных ячеек по телам"),
			new GpuBuffer("RcSync", Per.Fixed, CountRule.Sixteen, 4, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperReconGpu (S39): слова отказа и приборов"),
			new GpuBuffer("RcGuard", Per.Fixed, CountRule.Four, 4, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperReconGpu (S39): пустое чтение профиля"),
			new GpuBuffer("RcBlockBase", Per.Fixed, CountRule.ReconBodiesPlus1, 4, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperReconGpu (S39): начала блоков тел + страж (двоичный поиск, P1)"),
			new GpuBuffer("RcStart", Per.Fixed, CountRule.ReconBodiesPlus1, 4, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperReconGpu (S39): начала частиц тел + страж (двоичный поиск, P1)"),
			new GpuBuffer("RcVerts", Per.ReconVertex, CountRule.ReconVerts, 12, usedOnRouteA: false, Where.Lazy, Phase.Grid, "SuperReconGpu (S39): вершины запроса"),
			new GpuBuffer("RcVField", Per.ReconVertex, CountRule.ReconVertsTimesFields, 4, usedOnRouteA: false, Where.Lazy, Phase.Grid, "SuperReconGpu (S39): поля вершин [c·V+v]"),
			new GpuBuffer("RcProbe", Per.Fixed, CountRule.ProbeWords, 4, usedOnRouteA: false, Where.Lazy, Phase.Particles, "SuperReconGpu (S39): проба AsyncGPUReadback")
		});
		ReconSlotRows = new string[6] { "RcPos", "RcPf", "RcOrder", "RcSlotStart", "RcBrick", "RcBody" };
		ReconRingRows = new string[2] { "RcVerts", "RcVField" };
		ReconTransientRows = new string[1] { "RcProbe" };
		CpuItemsBase = Array.AsReadOnly(new CpuItem[7]
		{
			new CpuItem("SuperParticles", 199, Life.Run, "SuperParticles.cs:32-120"),
			new CpuItem("Baked.cellVol", 4, Life.Run, "SuperBaker.cs:558"),
			new CpuItem("Recorder pos/vel/scal/dev/dmg/v0", 84, Life.Recording, "SuperTapeRecorder.cs:199-203"),
			new CpuItem("Bake lists (×2 List growth)", 76, Life.Bake, "SuperBaker.cs:413-424"),
			new CpuItem("Upload ×2 + Cause + VerifyUpload", 432, Life.Upload, "SuperKernelGPU.cs:554,581-604,198; SuperShot.cs:567"),
			new CpuItem("Checkpoint FullState", 192, Life.Checkpoint, "SuperKernelGPU.cs:637"),
			new CpuItem("Frame fields + inside", 21, Life.Frame, "SuperReconstruct.cs:618-622; SuperTapeRecorder.cs:337-346")
		});
		CpuItemsWithXi = Array.AsReadOnly(new List<CpuItem>(CpuItemsBase)
		{
			new CpuItem("SuperParticles.xi (S43: поле ξ ВКЛ)", 4, Life.Run, "SuperBaker.cs AssignXi")
		}.ToArray());
	}
}
