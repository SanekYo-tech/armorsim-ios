using System;
using System.Collections.Generic;
using UnityEngine;

public static class SuperReconstruct
{
	public sealed class Body
	{
		public string name;

		public Vector3[] vertices;

		public int[] triangles;

		public float[][] fields;

		public double meshVolumeM3;

		public double particleVolumeM3;

		public int rawVertexCount;

		public int failedExcluded;

		public long gridCells;

		public long bytesDensity;

		public long bytesDensityWork;

		public long bytesHash;

		public long bytesMesh;

		public long bytesVertexFields;

		public long bytesBlockFields;

		public long bytesIdx;

		public Vector3 blockLo;

		public float blockCell;

		public int bnx;

		public int bny;

		public int bnz;

		public float[][] blockFields;
	}

	private sealed class DensityPrep
	{
		public int[] bxs;

		public int[] bys;

		public int[] bzs;

		public int[] ord;

		public int[] start;

		public float[] w;

		public float[] amp;

		public long bytes;

		public List<int> idx;

		public Vector3[] pos;

		public Vector3 flo;

		public float cell;
	}

	public sealed class DensityProbe
	{
		private readonly ParticleHash hash;

		private readonly Vector3[] pos;

		private readonly float[] amp;

		private readonly float cell;

		public readonly int alive;

		public static float MaxCloudSpanM;

		public static int LastClippedOut;

		public static void ResetCloudMeter()
		{
			MaxCloudSpanM = 0f;
			LastClippedOut = 0;
		}

		public DensityProbe(Vector3[] pos, float[] v0, float spacingM, float[] damage, Bounds? clip = null)
		{
			cell = spacingM;
			this.pos = pos;
			List<int> idx = new List<int>(pos.Length);
			int clipped = 0;
			float minX = float.MaxValue;
			float minY = float.MaxValue;
			float minZ = float.MaxValue;
			float maxX = float.MinValue;
			float maxY = float.MinValue;
			float maxZ = float.MinValue;
			float loX = float.MaxValue;
			float loY = float.MaxValue;
			float loZ = float.MaxValue;
			for (int i = 0; i < pos.Length; i++)
			{
				if (damage != null && damage[i] >= 1f)
				{
					continue;
				}
				Vector3 p = pos[i];
				if (p.x <= minX)
				{
					minX = p.x;
				}
				if (p.y <= minY)
				{
					minY = p.y;
				}
				if (p.z <= minZ)
				{
					minZ = p.z;
				}
				if (maxX <= p.x)
				{
					maxX = p.x;
				}
				if (maxY <= p.y)
				{
					maxY = p.y;
				}
				if (maxZ <= p.z)
				{
					maxZ = p.z;
				}
				if (clip.HasValue && !clip.Value.Contains(p))
				{
					clipped++;
					continue;
				}
				idx.Add(i);
				if (p.x <= loX)
				{
					loX = p.x;
				}
				if (p.y <= loY)
				{
					loY = p.y;
				}
				if (p.z <= loZ)
				{
					loZ = p.z;
				}
			}
			LastClippedOut = clipped;
			if (minX < maxX)
			{
				float span = maxY - minY;
				if (span <= maxZ - minZ)
				{
					span = maxZ - minZ;
				}
				float sx = maxX - minX;
				if (sx <= span)
				{
					sx = span;
				}
				if (sx > MaxCloudSpanM)
				{
					MaxCloudSpanM = sx;
				}
			}
			alive = idx.Count;
			if (alive == 0)
			{
				hash = null;
				amp = null;
			}
			else
			{
				hash = new ParticleHash(pos, idx, new Vector3(loX - cell * PadCells, loY - cell * PadCells, loZ - cell * PadCells), cell);
				amp = new float[pos.Length];
				foreach (int i in idx)
				{
					amp[i] = v0[i] * (1f / (cell * cell * cell));
				}
			}
		}

		public float At(Vector3 x)
		{
			if (hash == null)
			{
				return 0f;
			}
			float sum = 0f;
			int i0 = SuperReconMath.CellCoord(x.x, hash.lo.x, cell, hash.nx);
			int j0 = SuperReconMath.CellCoord(x.y, hash.lo.y, cell, hash.ny);
			int k0 = SuperReconMath.CellCoord(x.z, hash.lo.z, cell, hash.nz);
			for (int dk = -2; dk <= 2; dk++)
			{
				for (int dj = -2; dj <= 2; dj++)
				{
					for (int di = -2; di <= 2; di++)
					{
						int i = i0 + di;
						int j = j0 + dj;
						int k = k0 + dk;
						if (i >= 0 && j >= 0 && k >= 0)
						{
							if (i < hash.nx && j < hash.ny && k < hash.nz)
							{
								int start;
								int end;
								hash.Range(i, j, k, out start, out end);
								for (int q = start; q < end; q++)
								{
									int pi = hash.order[q];
									Vector3 p = pos[pi];
									float w = B2((p.x - x.x) / cell) * B2((p.y - x.y) / cell) * B2((p.z - x.z) / cell);
									if (0f < w)
									{
										sum += w * amp[pi];
									}
								}
							}
						}
					}
				}
			}
			return sum;
		}
	}

	private sealed class ParticleHash
	{
		public readonly int[] order;

		public readonly Vector3 lo;

		public readonly float cell;

		public readonly int nx;

		public readonly int ny;

		public readonly int nz;

		public readonly bool sparse;

		public readonly long peakBytes;

		private readonly int[] cellStart;

		private readonly int[] brick;

		private readonly int[] slotStart;

		private readonly int[] slotCount;

		private readonly int bnx;

		private readonly int bny;

		private const int BrickShift = 3;

		private const int BrickCells = 512;

		private const int DenseCellsPerParticle = 4;

		public ParticleHash(Vector3[] pos, List<int> idx, Vector3 flo, float cellM)
		{
			lo = flo;
			cell = cellM;
			Vector3 hi = flo;
			foreach (int q in idx)
			{
				Vector3 p = pos[q];
				if (hi.x <= p.x)
				{
					hi.x = p.x;
				}
				if (hi.y <= p.y)
				{
					hi.y = p.y;
				}
				if (hi.z <= p.z)
				{
					hi.z = p.z;
				}
			}
			SuperReconMath.HashDims(lo, hi, cell, out nx, out ny, out nz);
			long cells = (long)nz * ny * nx;
			int n = idx.Count;
			if (ForceHashForm == 2)
			{
				sparse = true;
			}
			else if (ForceHashForm == 1)
			{
				sparse = false;
			}
			else
			{
				sparse = (long)Math.Max(1, n) * DenseCellsPerParticle < cells;
			}
			order = new int[n];
			int start = SabotageHashCellOrder ? n - 1 : 0;
			int step = SabotageHashCellOrder ? -1 : 1;
			if (!sparse)
			{
				int[] counts = new int[cells];
				cellStart = new int[cells + 1];
				int[] cellIdx = new int[n];
				for (int i = 0; i < n; i++)
				{
					int q = idx[i];
					Vector3 p = pos[q];
					int ci = SuperReconMath.CellCoord(p.x, lo.x, cell, nx);
					int cj = SuperReconMath.CellCoord(p.y, lo.y, cell, ny);
					int ck = SuperReconMath.CellCoord(p.z, lo.z, cell, nz);
					int c = ci + (ck * ny + cj) * nx;
					cellIdx[i] = c;
					counts[c]++;
				}
				int acc = 0;
				for (int c = 0; c < cells; c++)
				{
					cellStart[c] = acc;
					acc += counts[c];
				}
				cellStart[cells] = acc;
				int[] slots = new int[cells];
				int si = start;
				for (int i = 0; i < n; i++)
				{
					int c = cellIdx[si];
					int s = slots[c];
					slots[c] = s + 1;
					order[cellStart[c] + s] = idx[si];
					si += step;
				}
				peakBytes = (long)(cellStart.Length + slots.Length + counts.Length + order.Length + cellIdx.Length) * 4L;
			}
			else
			{
				bnx = (nx + 7) >> 3;
				bny = (ny + 7) >> 3;
				long bricks = (long)((nz + 7) >> 3) * bny * bnx;
				if (0x3fffffff < bricks)
				{
					throw new ArgumentException(string.Format("хэш частиц: габарит облака {0:F1}×{1:F1}×{2:F1} м ", hi.x - flo.x, hi.y - flo.y, hi.z - flo.z) + string.Format("при шаге {0:F2} мм даёт решётку {1}×{2}×{3} и {4:F1} млрд кирпичей ", cell * 1000f, nx, ny, nz, bricks / 1e9) + string.Format("на {0} частиц — столько не бывает: облако растянуто улетевшими частицами", n));
				}
				brick = new int[bricks];
				int[] brickOf = new int[n];
				int[] slotOf = new int[n];
				for (int i = 0; i < n; i++)
				{
					int q = idx[i];
					Vector3 p = pos[q];
					int ci = SuperReconMath.CellCoord(p.x, lo.x, cell, nx);
					int cj = SuperReconMath.CellCoord(p.y, lo.y, cell, ny);
					int ck = SuperReconMath.CellCoord(p.z, lo.z, cell, nz);
					brickOf[i] = ((ck >> 3) * bny + (cj >> 3)) * bnx + (ci >> 3);
					slotOf[i] = ((ck & 7) << 3 | (cj & 7)) << 3 | (ci & 7);
					brick[brickOf[i]] = 1;
				}
				int used = 0;
				for (int b = 0; b < brick.Length; b++)
				{
					if (brick[b] != 0)
					{
						brick[b] = used;
						used++;
					}
					else
					{
						brick[b] = -1;
					}
				}
				slotCount = new int[used << 9];
				slotStart = new int[used << 9];
				for (int i = 0; i < n; i++)
				{
					int s = brick[brickOf[i]] << 9 | slotOf[i];
					slotCount[s]++;
				}
				int acc = 0;
				for (int s = 0; s < slotCount.Length; s++)
				{
					slotStart[s] = acc;
					acc += slotCount[s];
				}
				int si = start;
				for (int i = 0; i < n; i++)
				{
					int s = brick[brickOf[si]] << 9 | slotOf[si];
					order[slotStart[s]] = idx[si];
					slotStart[s]++;
					si += step;
				}
				for (int s = 0; s < slotStart.Length; s++)
				{
					slotStart[s] -= slotCount[s];
				}
				peakBytes = (long)(order.Length + slotOf.Length + brickOf.Length + slotCount.Length + slotStart.Length + brick.Length) * 4L;
			}
		}

		private void CellIJK(Vector3 p, out int i, out int j, out int k)
		{
			i = SuperReconMath.CellCoord(p.x, lo.x, cell, nx);
			j = SuperReconMath.CellCoord(p.y, lo.y, cell, ny);
			k = SuperReconMath.CellCoord(p.z, lo.z, cell, nz);
		}

		public int Cell(Vector3 p)
		{
			int i = SuperReconMath.CellCoord(p.x, lo.x, cell, nx);
			int j = SuperReconMath.CellCoord(p.y, lo.y, cell, ny);
			int k = SuperReconMath.CellCoord(p.z, lo.z, cell, nz);
			return i + (k * ny + j) * nx;
		}

		public void Range(int i, int j, int k, out int start, out int end)
		{
			if (sparse)
			{
				int bi = (i >> 3) + bnx * ((j >> 3) + (k >> 3) * bny);
				int b = brick[bi];
				if (b < 0)
				{
					start = 0;
					end = 0;
					return;
				}
				int slot = ((b << 3 | (k & 7)) << 3 | (j & 7)) << 3 | (i & 7);
				start = slotStart[slot];
				end = start + slotCount[slot];
			}
			else
			{
				int c = (k * ny + j) * nx + i;
				start = cellStart[c];
				end = cellStart[c + 1];
			}
		}
	}

	public const float Iso = 0.5f;

	public const int PadCells = 3;

	public const float FieldBlockCellM = 0.004f;

	public static bool SabotageConstantField;

	public const float SabotageValue = 1.234f;

	public static bool SabotageHealCracks;

	public static int ForceHashForm;

	public static bool SabotageHashCellOrder;

	public static bool WindowDensity = true;

	public static int WindowLayers = 32;

	public static long WindowCellThreshold = 2000000L;

	public static bool SabotageWindowGap;

	public static bool BlockSampleOnlyFilled = true;

	public static bool SabotageBlockDilateSpan;

	public static bool SpanSkip = true;

	private static int r_lastFailedExcluded;

	public static readonly string[] FieldNames = new string[5] { "eps_p", "|v|", "T", "s_vM", "D" };

	public static readonly string[] FieldUnits = new string[5] { "-", "м/с", "К", "ГПа", "-" };

	public const int FieldCount = 5;

	public const int FieldDamage = 4;

	public static bool CacheBsplineWeights;

	public static bool PoolDensity;

	public static int WindowedBodies;

	public static int FullFieldBodies;

	public static int WindowMaxNz;

	public static int WindowZUsed;

	private static readonly List<float[]> densityPool = new List<float[]>();

	private static readonly object densityPoolLock = new object();

	public static long DensityPoolHighWaterBytes;

	internal static Action<int, int, int, bool[]> TestFilledMask;

	internal static Action<int, int, int, int, bool[]> TestFilledMaskPass;

	public const float DecimateCellM = 0.003f;

	public const float DecimateConeDeg = 12f;

	public static int LastFailedExcluded => r_lastFailedExcluded;

	public static void NoteFailedExcluded(int count)
	{
		r_lastFailedExcluded = count;
	}

	public static Body Reconstruct(SuperBaker.Body body, Vector3[] pos, float[] v0, float[][] particleField, Vector3 gridLo, Vector3 gridHi, float particleSpacingM, bool decimate = true, float[] damage = null, SuperHookStages stages = null, int dop = 0, SuperReconGpu.FrameHandle gpu = null, int bodyIndex = -1)
	{
		long tLap = SuperHookStages.Now;
		if (particleSpacingM <= 0f)
		{
			throw new ArgumentException(string.Format("реконструкция: шаг частиц {0} — ", particleSpacingM) + "решётка восстановления не определена");
		}
		if (body.count < 1)
		{
			throw new ArgumentException("реконструкция: тело «" + body.name + "» пусто (0 частиц) — вниз по конвейеру ничего не передаётся, это отказ, а не пустая сетка");
		}
		bool skipDamaged = damage != null && !SabotageHealCracks;
		int failed = 0;
		int inDomain = 0;
		List<int> idx = new List<int>(body.count);
		Vector3 lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		for (int i = body.start; i < body.start + body.count; i++)
		{
			Vector3 p = pos[i];
			if (gridLo.x <= p.x && gridLo.y <= p.y && gridLo.z <= p.z && p.x <= gridHi.x && p.y <= gridHi.y && p.z <= gridHi.z)
			{
				inDomain++;
				if (skipDamaged && damage[i] >= 1f)
				{
					failed++;
					continue;
				}
				idx.Add(i);
				if (p.x <= lo.x)
				{
					lo.x = p.x;
				}
				if (p.y <= lo.y)
				{
					lo.y = p.y;
				}
				if (p.z <= lo.z)
				{
					lo.z = p.z;
				}
				if (hi.x <= p.x)
				{
					hi.x = p.x;
				}
				if (hi.y <= p.y)
				{
					hi.y = p.y;
				}
				if (hi.z <= p.z)
				{
					hi.z = p.z;
				}
			}
		}
		if (stages != null)
		{
			tLap = stages.Lap(SuperHookStages.Stage.Filter, tLap);
		}
		if (gpu != null)
		{
			gpu.CheckFilter(bodyIndex, idx.Count, lo, hi);
		}
		if (idx.Count == 0)
		{
			if (inDomain == 0)
			{
				throw new ArgumentException("реконструкция: у тела «" + body.name + "» ни одной частицы в габарите сетки — отказ по имени");
			}
			Body r = new Body();
			r.name = body.name;
			r.vertices = new Vector3[0];
			r.triangles = new int[0];
			r.meshVolumeM3 = 0.0;
			r.particleVolumeM3 = 0.0;
			r.rawVertexCount = 0;
			r.failedExcluded = failed;
			r.fields = new float[5][];
			r.blockLo = Vector3.zero;
			r.blockCell = particleSpacingM;
			r.bnx = 2;
			r.bny = 2;
			r.bnz = 2;
			r.blockFields = new float[5][];
			for (int c = 0; c < 5; c++)
			{
				r.fields[c] = new float[0];
				r.blockFields[c] = new float[8];
			}
			return r;
		}
		Body res = new Body();
		res.name = body.name;
		res.failedExcluded = failed;
		Vector3 flo;
		int nx;
		int ny;
		int nz;
		SuperReconMath.DensityGrid(lo, hi, particleSpacingM, out flo, out nx, out ny, out nz);
		res.gridCells = (long)nz * ny * nx;
		res.bytesIdx = (long)idx.Capacity * 4;
		float invCellVol = 1f / (particleSpacingM * particleSpacingM * particleSpacingM);
		RadarKahan vol = default(RadarKahan);
		SuperMarchingTets.Bytes bytes = new SuperMarchingTets.Bytes();
		SuperMarchingTets.RowSpans spans = null;
		if (SpanSkip && dop >= 1)
		{
			spans = new SuperMarchingTets.RowSpans(ny, nz);
		}
		if (dop >= 1 && WindowDensity && WindowCellThreshold < res.gridCells)
		{
			foreach (int q in idx)
			{
				vol.Add(v0[q]);
			}
			DensityPrep prep = MakeDensityPrep(idx, pos, v0, flo, particleSpacingM, invCellVol, nx, ny, nz, dop, spans);
			res.bytesDensityWork = prep.bytes;
			int nxy = ny * nx;
			int layers = Math.Max(2, Math.Min(nz, WindowLayers));
			System.Threading.Interlocked.Increment(ref WindowedBodies);
			if (WindowMaxNz < nz)
			{
				WindowMaxNz = nz;
				WindowZUsed = layers;
			}
			float[] density = RentDensity(layers * nxy);
			res.bytesDensity = (long)density.Length * 4;
			SuperMarchingTets.SlabMerge merge = new SuperMarchingTets.SlabMerge();
			int step = SabotageWindowGap ? layers : layers - 1;
			int baseOffset = 0;
			int stepNxy = step * nxy;
			for (int kFrom = 0; kFrom < nz - 1; kFrom += step)
			{
				int kTo = Math.Min(nz, kFrom + layers);
				Array.Clear(density, 0, (kTo - kFrom) * nxy);
				DensityScatterLayers(prep, kFrom, kTo, kFrom, nx, ny, density, dop);
				Action onSlab = ((stages != null) ? new Action(stages.Tick) : null);
				SuperMarchingTets.Slab[] slabs = SuperMarchingTets.BuildSlabs(density, baseOffset, kFrom, kTo - 1, nx, ny, flo, particleSpacingM, Iso, dop, onSlab, bytes, spans);
				merge.Append(slabs, kFrom, kTo - 1, nxy, bytes);
				baseOffset += stepNxy;
			}
			res.particleVolumeM3 = vol.Value;
			if (stages != null)
			{
				tLap = stages.Lap(SuperHookStages.Stage.Density, tLap);
			}
			SuperMarchingTets.Result mesh = merge.Finish();
			res.bytesMesh = bytes.peak;
			ReturnDensity(density);
			long lap = tLap;
			if (stages != null)
			{
				lap = stages.Lap(SuperHookStages.Stage.Polygonize, tLap);
			}
			return Finish(res, body, mesh, pos, particleField, flo, particleSpacingM, nx, ny, nz, idx, failed, decimate, dop, stages, lap, gpu, bodyIndex);
		}
		System.Threading.Interlocked.Increment(ref FullFieldBodies);
		float[] dense = RentDensity(nz * ny * nx);
		res.bytesDensity = (long)dense.Length * 4;
		if (dop < 3)
		{
			Span<float> wx = stackalloc float[3];
			Span<float> wy = stackalloc float[3];
			Span<float> wz = stackalloc float[3];
			foreach (int q in idx)
			{
				vol.Add(v0[q]);
				Vector3 p = pos[q];
				int bx;
				int by;
				int bz;
				Bspline((p.x - flo.x) / particleSpacingM, out bx, wx);
				Bspline((p.y - flo.y) / particleSpacingM, out by, wy);
				Bspline((p.z - flo.z) / particleSpacingM, out bz, wz);
				float amp = invCellVol * v0[q];
				if (spans != null)
				{
					for (int dz = 0; dz < 3; dz++)
					{
						for (int dy = 0; dy < 3; dy++)
						{
							spans.Touch(by + dy, bz + dz, bx, bx + 2);
						}
					}
				}
				for (int kk = 0; kk < 3; kk++)
				{
					for (int jj = 0; jj < 3; jj++)
					{
						for (int ii = 0; ii < 3; ii++)
						{
							int i = bx + ii;
							int j = by + jj;
							int k = bz + kk;
							if (i >= 0 && j >= 0 && k >= 0 && i < nx && j < ny && k < nz)
							{
								dense[(k * ny + j) * nx + i] += wx[ii] * amp * wy[jj] * wz[kk];
							}
						}
					}
				}
			}
		}
		else
		{
			foreach (int q in idx)
			{
				vol.Add(v0[q]);
			}
			DensityPrep prep = MakeDensityPrep(idx, pos, v0, flo, particleSpacingM, invCellVol, nx, ny, nz, dop, spans);
			DensityScatterLayers(prep, 0, nz, 0, nx, ny, dense, dop);
			res.bytesDensityWork = prep.bytes;
		}
		res.particleVolumeM3 = vol.Value;
		if (stages != null)
		{
			tLap = stages.Lap(SuperHookStages.Stage.Density, tLap);
		}
		SuperMarchingTets.Result mesh2;
		if (dop < 1)
		{
			mesh2 = SuperMarchingTets.Polygonize(dense, nx, ny, nz, flo, particleSpacingM, Iso);
		}
		else
		{
			Action onSlab = ((stages != null) ? new Action(stages.Tick) : null);
			mesh2 = SuperMarchingTets.PolygonizeSlabs(dense, nx, ny, nz, flo, particleSpacingM, Iso, dop, onSlab, bytes, spans);
		}
		res.bytesMesh = bytes.peak;
		ReturnDensity(dense);
		if (stages != null)
		{
			tLap = stages.Lap(SuperHookStages.Stage.Polygonize, tLap);
		}
		return Finish(res, body, mesh2, pos, particleField, flo, particleSpacingM, nx, ny, nz, idx, failed, decimate, dop, stages, tLap, gpu, bodyIndex);
	}

	private static Body Finish(Body r, SuperBaker.Body body, SuperMarchingTets.Result mesh, Vector3[] pos, float[][] particleField, Vector3 flo, float cell, int nx, int ny, int nz, List<int> idx, int failedInDomain, bool decimate, int dop, SuperHookStages stages, long tLap, SuperReconGpu.FrameHandle gpu, int bodyIndex)
	{
		r.rawVertexCount = mesh.vertices.Count;
		if (mesh.vertices.Count == 0)
		{
			if (failedInDomain == 0)
			{
				throw new ArgumentException(string.Format("реконструкция: у тела «{0}» изоповерхность пуста при {1} ", body.name, idx.Count) + "целых частицах и НИ ОДНОЙ разрушенной — поле плотности не дотянуло до изоуровня, это дефект, а не «нет поверхности»");
			}
			r.vertices = new Vector3[0];
			r.triangles = new int[0];
			r.meshVolumeM3 = 0.0;
			r.fields = new float[5][];
			for (int c = 0; c < 5; c++)
			{
				r.fields[c] = new float[0];
			}
			if (gpu != null)
			{
				return FinishGpu(r, gpu, bodyIndex, null, flo, cell, nx, ny, nz, stages, tLap);
			}
			ParticleHash hash = new ParticleHash(pos, idx, flo, cell);
			r.bytesHash = hash.peakBytes;
			if (stages != null)
			{
				tLap = stages.Lap(SuperHookStages.Stage.ParticleHash, tLap);
			}
			BuildFieldBlock(r, hash, pos, particleField, flo, cell, new Vector3(nx * cell, ny * cell, nz * cell), dop, stages);
			if (stages != null)
			{
				stages.Lap(SuperHookStages.Stage.FieldBlock, tLap);
			}
			return r;
		}
		Vector3[] verts;
		int[] tris;
		if (decimate)
		{
			Decimate(mesh.vertices, mesh.triangles, out verts, out tris);
		}
		else
		{
			verts = mesh.vertices.ToArray();
			tris = mesh.triangles.ToArray();
		}
		if (stages != null)
		{
			tLap = stages.Lap(SuperHookStages.Stage.Decimate, tLap);
		}
		r.vertices = verts;
		r.triangles = tris;
		r.meshVolumeM3 = SuperMarchingTets.SignedVolume(verts, tris);
		if (stages != null)
		{
			tLap = stages.Lap(SuperHookStages.Stage.SignedVolume, tLap);
		}
		if (gpu != null)
		{
			return FinishGpu(r, gpu, bodyIndex, verts, flo, cell, nx, ny, nz, stages, tLap);
		}
		ParticleHash hash2 = new ParticleHash(pos, idx, flo, cell);
		r.bytesHash = hash2.peakBytes;
		if (stages != null)
		{
			tLap = stages.Lap(SuperHookStages.Stage.ParticleHash, tLap);
		}
		r.fields = new float[5][];
		for (int c = 0; c < 5; c++)
		{
			r.fields[c] = new float[verts.Length];
		}
		r.bytesVertexFields = (long)verts.Length * 20L;
		if (dop < 2)
		{
			float[] acc = new float[5];
			for (int vi = 0; vi < verts.Length; vi++)
			{
				SampleVertex(vi, r.fields, hash2, pos, particleField, verts, cell, acc);
				if ((vi & 0x7ff) == 0x7ff && stages != null)
				{
					stages.Tick();
				}
			}
		}
		else
		{
			float[][] outFields = r.fields;
			SuperParallel.For(0, verts.Length, dop, () => new float[5], delegate(int vi, float[] acc)
			{
				SampleVertex(vi, outFields, hash2, pos, particleField, verts, cell, acc);
				if ((vi & 0x7ff) == 0x7ff && stages != null)
				{
					stages.Tick();
				}
			});
		}
		if (stages != null)
		{
			tLap = stages.Lap(SuperHookStages.Stage.VertexFields, tLap);
		}
		BuildFieldBlock(r, hash2, pos, particleField, flo, cell, new Vector3(nx * cell, ny * cell, nz * cell), dop, stages);
		if (stages != null)
		{
			stages.Lap(SuperHookStages.Stage.FieldBlock, tLap);
		}
		return r;
	}

	private static float[] RentDensity(int n)
	{
		if (!PoolDensity)
		{
			return new float[n];
		}
		float[] result;
		lock (densityPoolLock)
		{
			int best = -1;
			for (int i = 0; i < densityPool.Count; i++)
			{
				if (densityPool[i].Length < n)
				{
					continue;
				}
				if (best < 0 || densityPool[i].Length < densityPool[best].Length)
				{
					best = i;
				}
			}
			if (best < 0)
			{
				result = new float[n];
			}
			else
			{
				result = densityPool[best];
				densityPool.RemoveAt(best);
				Array.Clear(result, 0, n);
			}
		}
		return result;
	}

	private static void ReturnDensity(float[] a)
	{
		if (!PoolDensity || a == null)
		{
			return;
		}
		lock (densityPoolLock)
		{
			densityPool.Add(a);
			long bytes = 0L;
			for (int i = 0; i < densityPool.Count; i++)
			{
				bytes += (long)densityPool[i].Length * 4L;
			}
			if (bytes > DensityPoolHighWaterBytes)
			{
				DensityPoolHighWaterBytes = bytes;
			}
		}
	}

	public static void BeginRun()
	{
		lock (densityPoolLock)
		{
			densityPool.Clear();
			DensityPoolHighWaterBytes = 0L;
		}
		WindowedBodies = 0;
		FullFieldBodies = 0;
		WindowMaxNz = 0;
		WindowZUsed = 0;
	}

	public static void EndRun()
	{
		lock (densityPoolLock)
		{
			densityPool.Clear();
			DensityPoolHighWaterBytes = 0L;
		}
	}

	private static long DensityLayered(List<int> idx, Vector3[] pos, float[] v0, Vector3 flo, float cell, float invCellVol, int nx, int ny, int nz, float[] density, int dop, SuperMarchingTets.RowSpans spans = null)
	{
		DensityPrep prep = MakeDensityPrep(idx, pos, v0, flo, cell, invCellVol, nx, ny, nz, dop, spans);
		DensityScatterLayers(prep, 0, nz, 0, nx, ny, density, dop);
		return prep.bytes;
	}

	private static DensityPrep MakeDensityPrep(List<int> idx, Vector3[] pos, float[] v0, Vector3 flo, float cell, float invCellVol, int nx, int ny, int nz, int dop, SuperMarchingTets.RowSpans spans = null)
	{
		bool cacheW = CacheBsplineWeights;
		int n = idx.Count;
		int[] bxs = cacheW ? new int[n] : null;
		int[] bys = cacheW ? new int[n] : null;
		int[] bzs = new int[n];
		float[] w = cacheW ? new float[n * 9] : null;
		float[] amp = new float[n];
		SuperParallel.For(0, n, dop, delegate(int q)
		{
			int g = idx[q];
			Vector3 p = pos[g];
			Span<float> wx = stackalloc float[3];
			Span<float> wy = stackalloc float[3];
			Span<float> wz = stackalloc float[3];
			int bx;
			int by;
			int bz;
			Bspline((p.x - flo.x) / cell, out bx, wx);
			Bspline((p.y - flo.y) / cell, out by, wy);
			Bspline((p.z - flo.z) / cell, out bz, wz);
			bzs[q] = bz;
			if (cacheW)
			{
				bxs[q] = bx;
				bys[q] = by;
				w[q * 9 + 0] = wx[0];
				w[q * 9 + 1] = wx[1];
				w[q * 9 + 2] = wx[2];
				w[q * 9 + 3] = wy[0];
				w[q * 9 + 4] = wy[1];
				w[q * 9 + 5] = wy[2];
				w[q * 9 + 6] = wz[0];
				w[q * 9 + 7] = wz[1];
				w[q * 9 + 8] = wz[2];
			}
			amp[q] = v0[g] * invCellVol;
		});
		int[] counts = new int[nz + 3];
		for (int i = 0; i < n; i++)
		{
			int b = bzs[i];
			if (b + 2 >= 0 && b + 2 < nz + 2)
			{
				counts[b + 3]++;
			}
		}
		for (int c = 1; c <= nz + 2; c++)
		{
			counts[c] += counts[c - 1];
		}
		int[] scratch = new int[nz + 2];
		int[] ord = new int[counts[nz + 2]];
		for (int i = 0; i < n; i++)
		{
			int b = bzs[i];
			if (b + 2 >= 0 && b + 2 < nz + 2)
			{
				ord[counts[b + 2] + scratch[b + 2]] = i;
				scratch[b + 2]++;
			}
		}
		if (spans != null)
		{
			for (int i = 0; i < n; i++)
			{
				int k = bzs[i];
				int bx;
				int by;
				if (cacheW)
				{
					bx = bxs[i];
					by = bys[i];
				}
				else
				{
					Vector3 p = pos[idx[i]];
					Span<float> wx = stackalloc float[3];
					Span<float> wy = stackalloc float[3];
					Bspline((p.x - flo.x) / cell, out bx, wx);
					Bspline((p.y - flo.y) / cell, out by, wy);
				}
				for (int jj = 0; jj < 3; jj++)
				{
					for (int ii = 0; ii < 3; ii++)
					{
						spans.Touch(by + ii, k + jj, bx, bx + 2);
					}
				}
			}
		}
		if (!cacheW)
		{
			bzs = null;
		}
		DensityPrep prep = new DensityPrep();
		prep.bxs = bxs;
		prep.bys = bys;
		prep.bzs = bzs;
		prep.ord = ord;
		prep.start = counts;
		prep.w = w;
		prep.amp = amp;
		prep.idx = idx;
		prep.pos = pos;
		prep.flo = flo;
		prep.cell = cell;
		prep.bytes = (cacheW ? 56L : 12L) * n + (counts.Length + scratch.Length) * 4L + ((spans != null) ? spans.Bytes : 0);
		return prep;
	}

	private static void DensityScatterLayers(DensityPrep p, int kFrom, int kTo, int layerBase, int nx, int ny, float[] density, int dop)
	{
		int[] bxs = p.bxs;
		int[] bys = p.bys;
		int[] ord = p.ord;
		int[] start = p.start;
		float[] w = p.w;
		float[] amp = p.amp;
		Vector3[] ppos = p.pos;
		List<int> pidx = p.idx;
		Vector3 pflo = p.flo;
		float pcell = p.cell;
		int nxy = ny * nx;
		SuperParallel.For(kFrom, kTo, dop, delegate(int gk)
		{
			int i0 = start[gk];
			int i1 = start[gk + 1];
			int i2 = start[gk + 2];
			int e0 = start[gk + 1];
			int e1 = start[gk + 2];
			int e2 = start[gk + 3];
			Span<float> wx = stackalloc float[3];
			Span<float> wy = stackalloc float[3];
			Span<float> wz = stackalloc float[3];
			while (true)
			{
				int a = (i0 < e0) ? ord[i0] : int.MaxValue;
				int b = (i1 < e1) ? ord[i1] : int.MaxValue;
				int c = (i2 < e2) ? ord[i2] : int.MaxValue;
				int q;
				int ch;
				if (b < a || c < a)
				{
					if (c < b)
					{
						i2++;
						ch = 0;
						q = c;
					}
					else
					{
						i1++;
						ch = 1;
						q = b;
					}
				}
				else
				{
					if (a == int.MaxValue)
					{
						return;
					}
					i0++;
					ch = 2;
					q = a;
				}
				float amp1 = amp[q];
				int bx;
				int by;
				float wzv;
				if (w == null)
				{
					Vector3 pp = ppos[pidx[q]];
					float ux = (pp.x - pflo.x) / pcell;
					Bspline(ux, out bx, wx);
					float uy = (pp.y - pflo.y) / pcell;
					Bspline(uy, out by, wy);
					float uz = (pp.z - pflo.z) / pcell;
					int bz;
					Bspline(uz, out bz, wz);
					wzv = wz[ch];
				}
				else
				{
					bx = bxs[q];
					by = bys[q];
					wzv = w[q * 9 + 6 + ch];
				}
				for (int jj = 0; jj < 3; jj++)
				{
					for (int ii = 0; ii < 3; ii++)
					{
						int i = bx + ii;
						int j = by + jj;
						if (i < 0 || j < 0 || i >= nx || j >= ny)
						{
							continue;
						}
						float wgt = (w == null) ? wx[ii] * wy[jj] : w[q * 9 + ii] * w[q * 9 + 3 + jj];
						density[(gk - layerBase) * nxy + j * nx + i] += amp1 * wgt * wzv;
					}
				}
			}
		});
	}

	private static void Bspline(float u, out int b, Span<float> w)
	{
		float t = u - 0.5f;
		b = (int)Math.Floor((double)t);
		float f = u - b;
		w[0] = (1.5f - f) * 0.5f * (1.5f - f);
		w[1] = 0.75f - (f - 1f) * (f - 1f);
		w[2] = (f - 0.5f) * 0.5f * (f - 0.5f);
	}

	private static float B2(float t)
	{
		float x = Mathf.Abs(t);
		if (0.5f > x)
		{
			return 0.75f - x * x;
		}
		if (1.5f > x)
		{
			return 0.5f * (1.5f - x) * (1.5f - x);
		}
		return 0f;
	}

	private static void SampleFields(ParticleHash h, Vector3[] pos, float[][] pf, Vector3 x, float cell, float[] outAcc)
	{
		for (int c = 0; c < 5; c++)
		{
			outAcc[c] = 0f;
		}
		float sum = 0f;
		int i0 = SuperReconMath.CellCoord(x.x, h.lo.x, cell, h.nx);
		int j0 = SuperReconMath.CellCoord(x.y, h.lo.y, cell, h.ny);
		int k0 = SuperReconMath.CellCoord(x.z, h.lo.z, cell, h.nz);
		for (int dk = -2; dk <= 2; dk++)
		{
			for (int dj = -2; dj <= 2; dj++)
			{
				for (int di = -2; di <= 2; di++)
				{
					int i = i0 + di;
					int j = j0 + dj;
					int k = k0 + dk;
					if (i < 0 || j < 0 || k < 0 || i >= h.nx || j >= h.ny || k >= h.nz)
					{
						continue;
					}
					int start;
					int end;
					h.Range(i, j, k, out start, out end);
					for (int q = start; q < end; q++)
					{
						int pi = h.order[q];
						Vector3 p = pos[pi];
						float w = B2((p.x - x.x) / cell) * B2((p.y - x.y) / cell) * B2((p.z - x.z) / cell);
						if (0f < w)
						{
							sum += w;
							for (int c = 0; c < 5; c++)
							{
								outAcc[c] += w * pf[c][pi];
							}
						}
					}
				}
			}
		}
		if (1e-20f < sum)
		{
			for (int c = 0; c < 5; c++)
			{
				outAcc[c] /= sum;
			}
		}
	}

	private static void BuildFieldBlock(Body r, ParticleHash h, Vector3[] pos, float[][] pf, Vector3 flo, float cell, Vector3 size, int dop = 0, SuperHookStages stages = null)
	{
		int bnx;
		int bny;
		int bnz;
		SuperReconMath.BlockDims(size, out bnx, out bny, out bnz);
		float bc = FieldBlockCellM;
		r.blockLo = flo;
		r.blockCell = bc;
		r.bnx = bnx;
		r.bny = bny;
		r.bnz = bnz;
		int total = bnz * bny * bnx;
		r.blockFields = new float[5][];
		for (int c = 0; c < 5; c++)
		{
			r.blockFields[c] = new float[total];
		}
		bool[] filled = new bool[total];
		r.bytesBlockFields = (long)total * 22L;
		if (dop < 2)
		{
			float[] acc = new float[5];
			for (int k = 0; k < bnz; k++)
			{
				SampleBlockLayer(k, r, h, pos, pf, flo, cell, bc, bnx, bny, filled, acc);
				if (stages != null)
				{
					stages.Tick();
				}
			}
		}
		else
		{
			SuperParallel.For(0, bnz, dop, () => new float[5], delegate(int k, float[] acc)
			{
				SampleBlockLayer(k, r, h, pos, pf, flo, cell, bc, bnx, bny, filled, acc);
				if (stages != null)
				{
					stages.Tick();
				}
			});
		}
		if (TestFilledMask != null)
		{
			TestFilledMask(bnx, bny, bnz, filled);
		}
		int iMin = int.MaxValue;
		int iMax = -1;
		int jMin = int.MaxValue;
		int jMax = -1;
		int kMin = int.MaxValue;
		int kMax = -1;
		for (int k2 = 0; k2 < bnz; k2++)
		{
			for (int j = 0; j < bny; j++)
			{
				for (int i = 0; i < bnx; i++)
				{
					if (filled[(k2 * bny + j) * bnx + i])
					{
						if (i < iMin)
						{
							iMin = i;
						}
						if (iMax < i)
						{
							iMax = i;
						}
						if (j < jMin)
						{
							jMin = j;
						}
						if (jMax < j)
						{
							jMax = j;
						}
						if (k2 < kMin)
						{
							kMin = k2;
						}
						if (kMax < k2)
						{
							kMax = k2;
						}
					}
				}
			}
		}
		int pass = 0;
		while (true)
		{
			if (iMax < 0)
			{
				return;
			}
			int pad = SabotageBlockDilateSpan ? pass : pass + 1;
			int kFrom = Math.Max(0, kMin - pad);
			int kTo = Math.Min(bnz - 1, kMax + pad);
			int j0 = Math.Max(0, jMin - pad);
			int j1 = Math.Min(bny - 1, jMax + pad);
			int i0 = Math.Max(0, iMin - pad);
			int i1 = Math.Min(bnx - 1, pad + iMax);
			if (!BlockSampleOnlyFilled)
			{
				kFrom = 0;
				kTo = bnz - 1;
				j0 = 0;
				j1 = bny - 1;
				i0 = 0;
				i1 = bnx - 1;
			}
			bool[] prev = filled;
			bool[] nextFilled = (bool[])filled.Clone();
			if (dop < 2)
			{
				float[] acc = new float[5];
				for (int k = kFrom; k <= kTo; k++)
				{
					DilateBlockLayer(k, r, prev, nextFilled, bnx, bny, bnz, acc, i0, i1, j0, j1);
				}
			}
			else
			{
				SuperParallel.For(kFrom, kTo + 1, dop, () => new float[5], delegate(int k, float[] acc)
				{
					DilateBlockLayer(k, r, prev, nextFilled, bnx, bny, bnz, acc, i0, i1, j0, j1);
				});
			}
			filled = nextFilled;
			if (TestFilledMaskPass != null)
			{
				TestFilledMaskPass(pass, bnx, bny, bnz, nextFilled);
			}
			pass++;
			if (2 < pass)
			{
				return;
			}
		}
	}

	private static Body FinishGpu(Body r, SuperReconGpu.FrameHandle gpu, int bodyIndex, Vector3[] verts, Vector3 flo, float cell, int nx, int ny, int nz, SuperHookStages stages, long tLap)
	{
		gpu.CheckGrid(bodyIndex, flo, nx, ny, nz);
		r.bytesHash = 0L;
		if (verts == null)
		{
			int bnx;
			int bny;
			int bnz;
			SuperReconMath.BlockDims(new Vector3(nx * cell, ny * cell, nz * cell), out bnx, out bny, out bnz);
			float[][] bf = gpu.BlockFields(bodyIndex, bnx, bny, bnz);
			long lap = (stages != null) ? stages.Lap(SuperHookStages.Stage.ReconWait, tLap) : tLap;
			r.blockLo = flo;
			r.bnx = bnx;
			r.bny = bny;
			r.blockCell = FieldBlockCellM;
			r.bnz = bnz;
			r.blockFields = bf;
			r.bytesBlockFields = (long)(bnz * bny * bnx) * 22L;
			if (stages != null)
			{
				stages.Tick();
				stages.Lap(SuperHookStages.Stage.FieldBlock, lap);
			}
			gpu.BodyDone(bodyIndex);
		}
		else
		{
			r.bytesVertexFields = (long)verts.Length * 20L;
			r.fields = gpu.RequestVertexFields(bodyIndex, verts);
			long lap = tLap;
			if (stages != null)
			{
				lap = stages.Lap(SuperHookStages.Stage.ReconWait, tLap);
				stages.Tick();
				lap = stages.Lap(SuperHookStages.Stage.VertexFields, lap);
			}
			int bnx;
			int bny;
			int bnz;
			SuperReconMath.BlockDims(new Vector3(nx * cell, ny * cell, nz * cell), out bnx, out bny, out bnz);
			float[][] bf = gpu.BlockFields(bodyIndex, bnx, bny, bnz);
			if (stages != null)
			{
				lap = stages.Lap(SuperHookStages.Stage.ReconWait, lap);
			}
			r.blockLo = flo;
			r.bnx = bnx;
			r.bny = bny;
			r.blockCell = FieldBlockCellM;
			r.bnz = bnz;
			r.blockFields = bf;
			r.bytesBlockFields = (long)(bnz * bny * bnx) * 22L;
			if (stages != null)
			{
				stages.Tick();
				stages.Lap(SuperHookStages.Stage.FieldBlock, lap);
			}
		}
		return r;
	}

	private static void GpuBlock(Body r, SuperReconGpu.FrameHandle gpu, int b, Vector3 flo, float cell, int nx, int ny, int nz, SuperHookStages stages, ref long tLap)
	{
		int bnx;
		int bny;
		int bnz;
		SuperReconMath.BlockDims(new Vector3(nx * cell, ny * cell, nz * cell), out bnx, out bny, out bnz);
		float[][] bf = gpu.BlockFields(b, bnx, bny, bnz);
		if (stages != null)
		{
			tLap = stages.Lap(SuperHookStages.Stage.ReconWait, tLap);
		}
		r.blockLo = flo;
		r.blockCell = FieldBlockCellM;
		r.bnx = bnx;
		r.bny = bny;
		r.bnz = bnz;
		r.blockFields = bf;
		r.bytesBlockFields = (long)(bnx * bny * bnz) * 22L;
		if (stages != null)
		{
			stages.Tick();
			tLap = stages.Lap(SuperHookStages.Stage.FieldBlock, tLap);
		}
	}

	private static void SampleVertex(int vi, float[][] fields, ParticleHash hash, Vector3[] pos, float[][] pf, Vector3[] verts, float cell, float[] acc)
	{
		Vector3 x = verts[vi];
		SampleFields(hash, pos, pf, x, cell, acc);
		for (int c = 0; c < 5; c++)
		{
			fields[c][vi] = acc[c];
		}
	}

	private static void SampleBlockLayer(int k, Body r, ParticleHash h, Vector3[] pos, float[][] pf, Vector3 flo, float cell, float bc, int bnx, int bny, bool[] filled, float[] acc)
	{
		for (int j = 0; j < bny; j++)
		{
			for (int i = 0; i < bnx; i++)
			{
				int idx = (k * bny + j) * bnx + i;
				Vector3 node = SuperReconMath.BlockNode(flo, bc, i, j, k);
				if (!BlockSampleOnlyFilled)
				{
					SampleFields(h, pos, pf, node, cell, acc);
					bool sup = HasSupport(h, node, cell);
					filled[idx] = sup;
					if (sup)
					{
						for (int c = 0; c < 5; c++)
						{
							r.blockFields[c][idx] = acc[c];
						}
					}
				}
				else
				{
					bool sup = HasSupport(h, node, cell);
					filled[idx] = sup;
					if (sup)
					{
						SampleFields(h, pos, pf, node, cell, acc);
						for (int c = 0; c < 5; c++)
						{
							r.blockFields[c][idx] = acc[c];
						}
					}
				}
			}
		}
	}

	private static void DilateBlockLayer(int k, Body r, bool[] filled, bool[] nextFilled, int bnx, int bny, int bnz, float[] acc, int i0, int i1, int j0, int j1)
	{
		for (int j = j0; j <= j1; j++)
		{
			for (int i = i0; i <= i1; i++)
			{
				int idx = (k * bny + j) * bnx + i;
				if (filled[idx])
				{
					continue;
				}
				for (int c = 0; c < 5; c++)
				{
					acc[c] = 0f;
				}
				int near = 0;
				for (int d = 0; d < 6; d++)
				{
					int ni = i;
					int nj = j;
					int nk = k;
					switch (d)
					{
					case 0:
						ni++;
						break;
					case 1:
						ni--;
						break;
					case 2:
						nj++;
						break;
					case 3:
						nj--;
						break;
					case 4:
						nk++;
						break;
					default:
						nk--;
						break;
					}
					if (ni < 0 || nj < 0 || nk < 0 || ni >= bnx || nj >= bny || nk >= bnz)
					{
						continue;
					}
					int nidx = (nk * bny + nj) * bnx + ni;
					if (!filled[nidx])
					{
						continue;
					}
					near++;
					for (int c = 0; c < 5; c++)
					{
						acc[c] += r.blockFields[c][nidx];
					}
				}
				if (near != 0)
				{
					nextFilled[idx] = true;
					for (int c = 0; c < 5; c++)
					{
						r.blockFields[c][idx] = acc[c] / near;
					}
				}
			}
		}
	}

	private static bool HasSupport(ParticleHash h, Vector3 x, float cell)
	{
		int i0 = SuperReconMath.CellCoord(x.x, h.lo.x, cell, h.nx);
		int j0 = SuperReconMath.CellCoord(x.y, h.lo.y, cell, h.ny);
		int k0 = SuperReconMath.CellCoord(x.z, h.lo.z, cell, h.nz);
		for (int dk = -2; dk <= 2; dk++)
		{
			for (int dj = -2; dj <= 2; dj++)
			{
				for (int di = -2; di <= 2; di++)
				{
					int i = i0 + di;
					int j = j0 + dj;
					int k = k0 + dk;
					if (i < 0 || j < 0 || k < 0 || i >= h.nx || j >= h.ny || k >= h.nz)
					{
						continue;
					}
					int start;
					int end;
					h.Range(i, j, k, out start, out end);
					if (end > start)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private static void Decimate(List<Vector3> v, List<int> t, out Vector3[] outV, out int[] outT)
	{
		int n = v.Count;
		Vector3[] areaAcc = new Vector3[n];
		for (int ti = 0; ti < t.Count; ti += 3)
		{
			int a = t[ti];
			int b = t[ti + 1];
			int c = t[ti + 2];
			Vector3 cross = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
			areaAcc[a] += cross;
			areaAcc[b] += cross;
			areaAcc[c] += cross;
		}
		float minX = float.MaxValue;
		float minY = float.MaxValue;
		float minZ = float.MaxValue;
		for (int i = 0; i < n; i++)
		{
			Vector3 p = v[i];
			if (p.x <= minX)
			{
				minX = p.x;
			}
			if (p.y <= minY)
			{
				minY = p.y;
			}
			if (p.z <= minZ)
			{
				minZ = p.z;
			}
		}
		float coneCos = Mathf.Cos(DecimateConeDeg * Mathf.Deg2Rad);
		for (int i = 0; i < n; i++)
		{
			areaAcc[i] = SafeDir(areaAcc[i]);
		}
		Dictionary<long, List<int>> cells = new Dictionary<long, List<int>>();
		for (int i = 0; i < n; i++)
		{
			Vector3 p = v[i];
			long key = (((long)((p.x - minX) / DecimateCellM) << 21) | (long)((p.y - minY) / DecimateCellM)) << 21 | (long)((p.z - minZ) / DecimateCellM);
			List<int> cell;
			if (!cells.TryGetValue(key, out cell))
			{
				cell = new List<int>();
				cells.Add(key, cell);
			}
			cell.Add(i);
		}
		int[] remap = new int[n];
		for (int i = 0; i < n; i++)
		{
			remap[i] = -1;
		}
		List<Vector3> nv = new List<Vector3>();
		foreach (List<int> bucket in cells.Values)
		{
			Vector3 sum = Vector3.zero;
			foreach (int q in bucket)
			{
				sum += areaAcc[q];
			}
			Vector3 dir = SafeDir(sum);
			if (1e-10f <= dir.sqrMagnitude)
			{
				bool allInCone = true;
				foreach (int q in bucket)
				{
					if (Vector3.Dot(dir, areaAcc[q]) < coneCos)
					{
						allInCone = false;
						break;
					}
				}
				if (allInCone && bucket.Count > 1)
				{
					Vector3 avg = Vector3.zero;
					foreach (int q in bucket)
					{
						avg += v[q];
					}
					int nn = nv.Count;
					nv.Add(avg / bucket.Count);
					foreach (int q in bucket)
					{
						remap[q] = nn;
					}
					continue;
				}
			}
			foreach (int q in bucket)
			{
				remap[q] = nv.Count;
				nv.Add(v[q]);
			}
		}
		List<int> nt = new List<int>(t.Count);
		for (int i = 0; i < t.Count; i += 3)
		{
			int a = remap[t[i]];
			int b = remap[t[i + 1]];
			int c = remap[t[i + 2]];
			if (a != b && b != c && a != c)
			{
				nt.Add(a);
				nt.Add(b);
				nt.Add(c);
			}
		}
		outV = nv.ToArray();
		outT = nt.ToArray();
	}

	private static Vector3 SafeDir(Vector3 v)
	{
		float len = v.magnitude;
		if (1e-20f < len)
		{
			return v / len;
		}
		return Vector3.zero;
	}

	public static float[][] ParticleFields(int count, Vector3[] vel, Vector4[] scal, SuperMat3[] dev, float[] dmg = null, int dop = 0, float[][] into = null)
	{
		float[][] f = into;
		if (f == null)
		{
			f = new float[5][];
			for (int ch = 0; ch < 5; ch++)
			{
				f[ch] = new float[count];
			}
		}
		if (SabotageConstantField)
		{
			for (int ch = 0; ch < 5; ch++)
			{
				for (int i = 0; i < count; i++)
				{
					f[ch][i] = SabotageValue;
				}
			}
		}
		else if (dop < 2)
		{
			FieldsRange(0, count, f, vel, scal, dev, dmg);
		}
		else
		{
			int per = (count + dop * 4 - 1) / (dop * 4);
			SuperParallel.For(0, dop * 4, dop, delegate(int ch)
			{
				FieldsRange(Math.Min(count, per * ch), Math.Min(count, (ch + 1) * per), f, vel, scal, dev, dmg);
			});
		}
		return f;
	}

	private static void FieldsRange(int from, int to, float[][] f, Vector3[] vel, Vector4[] scal, SuperMat3[] dev, float[] dmg)
	{
		for (int i = from; i < to; i++)
		{
			f[0][i] = scal[i].x;
			Vector3 v = vel[i];
			f[1][i] = (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
			f[2][i] = scal[i].y;
			f[3][i] = SuperMat3.VonMises(dev[i]) * 1e-9f;
			f[4][i] = (dmg != null) ? dmg[i] : 0f;
		}
	}
}
