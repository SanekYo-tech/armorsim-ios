using System;
using System.Collections.Generic;
using Unity.Collections;

public sealed class AxiMesh : IDisposable
{
	private struct Patch
	{
		public float[] r;

		public float[] z;

		public int nCol;

		public int nRow;

		public byte body;

		public ContinuumMaterialId mat;

		public bool clampFar;

		public bool[] drop;

		public int NodeIndex(int i, int j)
		{
			return (nCol + 1) * j + i;
		}

		public int ElemIndex(int i, int j)
		{
			return i + j * nCol;
		}

		public bool Dropped(int i, int j)
		{
			if (drop == null)
			{
				return false;
			}
			return drop[ElemIndex(i, j)];
		}
	}

	public NativeArray<float> nodeR;

	public NativeArray<float> nodeZ;

	public NativeArray<float> nodeVr;

	public NativeArray<float> nodeVz;

	public NativeArray<float> nodeVrPrev;

	public NativeArray<float> nodeVzPrev;

	public NativeArray<float> nodeMass;

	public NativeArray<float> nodeFr;

	public NativeArray<float> nodeFz;

	public NativeArray<byte> nodeOnAxis;

	public NativeArray<byte> nodeFixed;

	public NativeArray<byte> nodeBody;

	public NativeArray<byte> nodeOrphan;

	public int nodeCount;

	public NativeArray<int> elemNodes;

	public NativeArray<byte> elemAlive;

	public NativeArray<byte> elemBody;

	public NativeArray<int> elemMat;

	public NativeArray<float> elemRho;

	public NativeArray<float> elemP;

	public NativeArray<float> elemSrr;

	public NativeArray<float> elemSzz;

	public NativeArray<float> elemStt;

	public NativeArray<float> elemSrz;

	public NativeArray<float> elemEnergy;

	public NativeArray<float> elemEpsP;

	public NativeArray<float> elemEpsRate;

	public NativeArray<float> elemTemp;

	public NativeArray<float> elemDamage;

	public NativeArray<float> elemQ;

	public NativeArray<float> elemVol0;

	public NativeArray<float> elemVol;

	public NativeArray<float> elemMass;

	public NativeArray<float> elemArea;

	public NativeArray<float> elemRc;

	public NativeArray<float> elemDtCand;

	public NativeArray<float> elemHgWork;

	public NativeArray<float> elemVisWork;

	public NativeArray<byte> elemKillFlag;

	public NativeArray<float> elemForce;

	public int elemCount;

	public NativeArray<int> nodeAdjStart;

	public NativeArray<int> nodeAdjElem;

	public NativeArray<byte> nodeAdjCorner;

	public float hCharacteristic;

	public float axisTol;

	public int projectileElemCount;

	public int targetElemCount;

	public const float AxisTolFactor = 1E-06f;

	private readonly List<Patch> patches;

	private AxiMesh()
	{
		patches = new List<Patch>();
	}

	public static AxiMesh Build(ContinuumScenario s, SolverDiagnostics diag)
	{
		AxiMesh mesh = new AxiMesh();
		float h = s.ElementSizeM;
		mesh.hCharacteristic = h;
		mesh.axisTol = h * AxisTolFactor;
		Patch rodPatch = ((s.noseMeshMode == NoseMeshMode.BodyFit) ? BuildRodPatchBodyFit(s, h) : BuildRodPatchTrimmed(s, h));
		mesh.patches.Add(rodPatch);
		if (s.targetKind == ContinuumTargetKind.Plate || s.targetKind == ContinuumTargetKind.SemiInfinite)
		{
			if (!s.HasLayerStack)
			{
				Patch targetPatch = BuildTargetPatch(s, h, 0f, s.targetThicknessM, s.targetMaterial, s.targetKind == ContinuumTargetKind.SemiInfinite);
				mesh.patches.Add(targetPatch);
			}
			else
			{
				float z = 0f;
				for (int i = 0; i < s.layers.Length; i++)
				{
					ContinuumLayer layer = s.layers[i];
					z += layer.gapBeforeM;
					Patch targetPatch = BuildTargetPatch(s, h, z, layer.thicknessM, layer.material, i == s.layers.Length - 1 && s.targetKind == ContinuumTargetKind.SemiInfinite);
					mesh.patches.Add(targetPatch);
					z += layer.thicknessM;
				}
			}
		}
		mesh.Assemble(s, diag);
		return mesh;
	}

	private static Patch BuildRodPatch(ContinuumScenario s, float h)
	{
		Patch patch = ((s.noseMeshMode == NoseMeshMode.BodyFit) ? BuildRodPatchBodyFit(s, h) : BuildRodPatchTrimmed(s, h));
		return patch;
	}

	private static Patch BuildRodPatchTrimmed(ContinuumScenario s, float h)
	{
		Patch patch = default(Patch);
		float r0 = s.rodDiameterM * 0.5f;
		float noseLen = NoseLength(s, r0);
		float rTip = TipFlatRadius(s, h);
		int nCol = UnityEngine.Mathf.RoundToInt(r0 / h);
		if (nCol < 2)
		{
			nCol = 2;
		}
		int nRow = UnityEngine.Mathf.RoundToInt(s.rodLengthM / h);
		if (nRow < 1)
		{
			nRow = 1;
		}
		patch.nCol = nCol;
		patch.nRow = nRow;
		patch.body = 0;
		patch.mat = s.rodMaterial;
		patch.clampFar = false;
		patch.r = new float[(nCol + 1) * (nRow + 1)];
		patch.z = new float[(nCol + 1) * (nRow + 1)];
		float zTail = -s.initialGapM - s.rodLengthM;
		float zNose = -s.initialGapM;
		for (int i = 0; i <= nCol; i++)
		{
			float r = (float)i * r0 / nCol;
			for (int j = 0; j <= nRow; j++)
			{
				float t = (float)j / nRow;
				if (t < 0f)
				{
					t = 0f;
				}
				else if (t > 1f)
				{
					t = 1f;
				}
				int idx = patch.NodeIndex(i, j);
				patch.r[idx] = r;
				patch.z[idx] = zTail + (zNose - zTail) * t;
			}
		}
		if (noseLen > 1E-09f)
		{
			patch.drop = new bool[nCol * nRow];
			for (int i = 0; i < nCol; i++)
			{
				for (int j = 0; j < nRow; j++)
				{
					float rAvg = (patch.r[patch.NodeIndex(i, j)] + patch.r[patch.NodeIndex(i + 1, j)] + patch.r[patch.NodeIndex(i + 1, j + 1)] + patch.r[patch.NodeIndex(i, j + 1)]) * 0.25f;
					float zAvg = (patch.z[patch.NodeIndex(i, j)] + patch.z[patch.NodeIndex(i + 1, j)] + patch.z[patch.NodeIndex(i + 1, j + 1)] + patch.z[patch.NodeIndex(i, j + 1)]) * 0.25f;
					if (NoseFrontZ(s, rAvg, r0, noseLen, rTip) + zNose < zAvg)
					{
						patch.drop[patch.ElemIndex(i, j)] = true;
					}
				}
			}
		}
		return patch;
	}

	private static Patch BuildRodPatchBodyFit(ContinuumScenario s, float h)
	{
		Patch patch = default(Patch);
		float r0 = s.rodDiameterM * 0.5f;
		float noseLen = NoseLength(s, r0);
		float rTip = TipFlatRadius(s, h);
		float noseExt = 0f;
		if (noseLen > 1E-09f)
		{
			noseExt = noseLen * 0.5f;
			if (noseExt < h)
			{
				noseExt = h;
			}
		}
		float noseLenExt = noseLen + noseExt;
		float bodyLen = s.rodLengthM - noseLenExt;
		if (bodyLen < 0f)
		{
			bodyLen = 0f;
		}
		int nCol = UnityEngine.Mathf.RoundToInt(r0 / h);
		if (nCol < 2)
		{
			nCol = 2;
		}
		int nRowBody = UnityEngine.Mathf.RoundToInt(bodyLen / h);
		if (nRowBody < 1)
		{
			nRowBody = 1;
		}
		int nRowNose = 0;
		if (noseLenExt > 1E-09f)
		{
			nRowNose = UnityEngine.Mathf.RoundToInt(noseLenExt / h);
			if (nRowNose < 1)
			{
				nRowNose = 1;
			}
		}
		int nRow = nRowBody + nRowNose;
		patch.nCol = nCol;
		patch.nRow = nRow;
		patch.body = 0;
		patch.mat = s.rodMaterial;
		patch.clampFar = false;
		patch.r = new float[(nCol + 1) * (nRow + 1)];
		patch.z = new float[(nCol + 1) * (nRow + 1)];
		float zTail = -s.initialGapM - s.rodLengthM;
		float zNoseBase = -s.initialGapM - noseLenExt;
		for (int i = 0; i <= nCol; i++)
		{
			float r = (float)i * r0 / nCol;
			float zFront = NoseFrontZ(s, r, r0, noseLen, rTip) - s.initialGapM;
			for (int j = 0; j <= nRow; j++)
			{
				float t;
				if (j <= nRowBody)
				{
					t = ((nRowBody != 0) ? ((float)j / nRowBody) : 1f);
					if (t < 0f)
					{
						t = 0f;
					}
					else if (t > 1f)
					{
						t = 1f;
					}
					patch.z[patch.NodeIndex(i, j)] = zTail + (zNoseBase - zTail) * t;
				}
				else
				{
					t = (float)(j - nRowBody) / nRowNose;
					if (t < 0f)
					{
						t = 0f;
					}
					else if (t > 1f)
					{
						t = 1f;
					}
					patch.z[patch.NodeIndex(i, j)] = zNoseBase + (zFront - zNoseBase) * t;
				}
				patch.r[patch.NodeIndex(i, j)] = r;
			}
		}
		return patch;
	}

	public static float TipFlatRadius(ContinuumScenario s, float h)
	{
		if (s.nose == ContinuumNose.Flat)
		{
			return 0f;
		}
		float d = s.rodDiameterM;
		float minOverD = 0f;
		if (d > 1E-09f)
		{
			minOverD = (h + h) / d;
		}
		if (minOverD - 1E-06f <= s.tipFlatOverD)
		{
			return s.tipFlatOverD * 0.5f * d;
		}
		throw new InvalidOperationException(string.Format("ULTRA / сетка стержня: притупление носка tipFlatOverD = {0:F4} ", s.tipFlatOverD) + string.Format("меньше допустимого. При h = {0:F3} мм и D = {1:F2} мм минимум ", h * 1000f, d * 1000f) + string.Format("равен 2·h/D = {0:F4} (две ячейки поперёк площадки). ", minOverD) + "Острый носок вырожден В ИСХОДНОЙ сетке: Δt_e схлопывается, а эрозия такой элемент не снимет — он не повреждён. Поднимите tipFlatOverD или сгустите сетку.");
	}

	public static float MinTipFlatOverD(float h, float diameterM)
	{
		if (diameterM > 1E-09f)
		{
			return (h + h) / diameterM;
		}
		return 0f;
	}

	private static float NoseLength(ContinuumScenario s, float r0)
	{
		if (s.nose == ContinuumNose.Flat)
		{
			return 0f;
		}
		if (s.nose == ContinuumNose.Hemispherical)
		{
			return r0;
		}
		float noseLen = (s.EffectiveNoseLengthCalibres + s.EffectiveNoseLengthCalibres) * r0;
		if (noseLen < 0f)
		{
			return 0f;
		}
		return noseLen;
	}

	private static float NoseFrontZ(ContinuumScenario s, float r, float r0, float noseLen, float rTip)
	{
		if (noseLen > 1E-09f && rTip < r)
		{
			float denom = 1E-09f;
			if (r0 - rTip > denom)
			{
				denom = r0 - rTip;
			}
			float t = (r - rTip) / denom;
			if (t < 0f)
			{
				t = 0f;
			}
			else if (t > 1f)
			{
				t = 1f;
			}
			if (s.nose == ContinuumNose.Conical)
			{
				return -noseLen * t;
			}
			if (s.nose == ContinuumNose.Hemispherical)
			{
				float tr = t * r0;
				float disc = r0 * r0 - tr * tr;
				if (disc < 0f)
				{
					disc = 0f;
				}
				return UnityEngine.Mathf.Sqrt(disc) - r0;
			}
			if (s.nose == ContinuumNose.Ogive)
			{
				float rr = (r0 * r0 + noseLen * noseLen) / (r0 + r0);
				float disc = rr * rr - (t * r0 + rr - r0) * (t * r0 + rr - r0);
				if (disc < 0f)
				{
					disc = 0f;
				}
				return UnityEngine.Mathf.Sqrt(disc) - noseLen;
			}
		}
		return 0f;
	}

	private static Patch BuildTargetPatch(ContinuumScenario s, float h, float z0, float thicknessM, ContinuumMaterialId mat, bool clampFar)
	{
		Patch patch = default(Patch);
		float rMax = s.rodDiameterM * s.outerRadiusCalibres;
		float rUniform = s.rodDiameterM * s.uniformRadiusCalibres;
		List<float> radii = new List<float>();
		int nUniform = UnityEngine.Mathf.RoundToInt(rUniform / h);
		if (nUniform < 1)
		{
			nUniform = 1;
		}
		for (int i = 0; i <= nUniform; i++)
		{
			radii.Add((float)i * rUniform / nUniform);
		}
		float hCur = h;
		if (rMax - 1E-09f > rUniform)
		{
			for (int k = 0; k < 10000; k++)
			{
				hCur *= s.gradingRatio;
				float r = rUniform + hCur;
				if (r > rMax)
				{
					r = rMax;
				}
				radii.Add(r);
				if (rMax - 1E-09f <= r)
				{
					break;
				}
			}
		}
		if (rMax > radii[radii.Count - 1])
		{
			radii.Add(rMax);
		}
		int nCol = radii.Count - 1;
		int nRow = UnityEngine.Mathf.RoundToInt(thicknessM / h);
		if (nRow < 1)
		{
			nRow = 1;
		}
		patch.nCol = nCol;
		patch.nRow = nRow;
		patch.body = 1;
		patch.mat = mat;
		patch.clampFar = clampFar;
		patch.drop = null;
		patch.r = new float[(nRow + 1) * (nCol + 1)];
		patch.z = new float[(nRow + 1) * (nCol + 1)];
		for (int i = 0; i <= nCol; i++)
		{
			float r = radii[i];
			for (int j = 0; j <= nRow; j++)
			{
				int idx = patch.NodeIndex(i, j);
				patch.r[idx] = r;
				patch.z[idx] = (float)j * thicknessM / nRow + z0;
			}
		}
		return patch;
	}

	private void Assemble(ContinuumScenario s, SolverDiagnostics diag)
	{
		int[] nodeBase = new int[patches.Count];
		int nodeTotal = 0;
		for (int k = 0; k < patches.Count; k++)
		{
			nodeBase[k] = nodeTotal;
			nodeTotal += (patches[k].nCol + 1) * (patches[k].nRow + 1);
		}
		bool[] nodeUsed = new bool[nodeTotal];
		int elemTotal = 0;
		for (int k = 0; k < patches.Count; k++)
		{
			Patch p = patches[k];
			for (int i = 0; i < p.nCol; i++)
			{
				for (int j = 0; j < p.nRow; j++)
				{
					if (!p.Dropped(i, j))
					{
						elemTotal++;
						nodeUsed[nodeBase[k] + p.NodeIndex(i, j)] = true;
						nodeUsed[nodeBase[k] + p.NodeIndex(i + 1, j)] = true;
						nodeUsed[nodeBase[k] + p.NodeIndex(i + 1, j + 1)] = true;
						nodeUsed[nodeBase[k] + p.NodeIndex(i, j + 1)] = true;
					}
				}
			}
		}
		int[] newIndex = new int[nodeTotal];
		int nodeCount = 0;
		for (int u = 0; u < nodeTotal; u++)
		{
			newIndex[u] = (nodeUsed[u] ? nodeCount++ : (-1));
		}
		this.nodeCount = nodeCount;
		elemCount = elemTotal;
		AllocateNodes(nodeCount);
		AllocateElements(elemTotal);
		float outerR = s.outerRadiusCalibres * s.rodDiameterM;
		for (int k = 0; k < patches.Count; k++)
		{
			Patch p = patches[k];
			int nodeBaseOffset = nodeBase[k];
			for (int i = 0; i <= p.nCol; i++)
			{
				for (int j = 0; j <= p.nRow; j++)
				{
					int g = newIndex[nodeBaseOffset + p.NodeIndex(i, j)];
					if (g >= 0)
					{
						nodeR[g] = p.r[p.NodeIndex(i, j)];
						nodeZ[g] = p.z[p.NodeIndex(i, j)];
						nodeBody[g] = p.body;
						nodeOnAxis[g] = (byte)((nodeR[g] < axisTol) ? 1 : 0);
						nodeFixed[g] = (byte)(((p.body == 1 && nodeR[g] >= outerR - 1E-09f) || (p.clampFar && j == p.nRow)) ? 1 : 0);
						nodeVr[g] = 0f;
						nodeVz[g] = ((p.body != 0) ? 0f : s.impactVelocityMps);
					}
				}
			}
			int e = 0;
			for (int i = 0; i < p.nCol; i++)
			{
				for (int j = 0; j < p.nRow; j++)
				{
					if (!p.Dropped(i, j))
					{
						elemNodes[e * 4] = newIndex[nodeBaseOffset + p.NodeIndex(i, j)];
						elemNodes[e * 4 + 1] = newIndex[nodeBaseOffset + p.NodeIndex(i + 1, j)];
						elemNodes[e * 4 + 2] = newIndex[nodeBaseOffset + p.NodeIndex(i + 1, j + 1)];
						elemNodes[e * 4 + 3] = newIndex[nodeBaseOffset + p.NodeIndex(i, j + 1)];
						elemAlive[e] = 1;
						elemBody[e] = p.body;
						elemMat[e] = (int)p.mat;
						e++;
					}
				}
			}
			if (p.body == 0)
			{
				projectileElemCount = e;
				if (diag != null)
				{
					diag.elementsTrimmed += p.nRow * p.nCol - e;
				}
			}
			else
			{
				targetElemCount += e;
			}
		}
		InitElementState(s, diag);
		BuildAdjacency();
		if (nodeCount > 0)
		{
			LumpNodalMass();
		}
		ValidateGeometry(diag);
	}

	private void InitElementState(ContinuumScenario s, SolverDiagnostics diag)
	{
		for (int e = 0; e < elemCount; e++)
		{
			ContinuumProperties props;
			if (s.materialOverride != null && elemMat[e] < s.materialOverride.Length)
			{
				props = s.materialOverride[elemMat[e]];
			}
			else
			{
				props = ContinuumMaterials.Get((ContinuumMaterialId)elemMat[e]);
			}
			GeometryOf(e, out float area, out float rc);
			elemArea[e] = area;
			elemRc[e] = rc;
			float vol = rc * 6.28318548f * area;
			elemVol0[e] = vol;
			elemVol[e] = vol;
			elemRho[e] = props.rho0;
			elemMass[e] = vol * props.rho0;
			elemTemp[e] = ((s.initialTemperatureK > 0f) ? s.initialTemperatureK : props.T_room);
			elemEnergy[e] = 0f;
			elemEpsRate[e] = props.epsDot0;
			if (area <= 0f)
			{
				diag.WarnOnce("negArea", string.Format("элемент {0}: неположительная площадь при построении — ", e) + "проверить порядок обхода узлов");
			}
		}
	}

	public void GeometryOf(int e, out float area, out float rc)
	{
		int n0 = elemNodes[e * 4];
		int n1 = elemNodes[e * 4 + 1];
		int n2 = elemNodes[e * 4 + 2];
		int n3 = elemNodes[e * 4 + 3];
		float r0 = nodeR[n0];
		float z0 = nodeZ[n0];
		float r1 = nodeR[n1];
		float z1 = nodeZ[n1];
		float r2 = nodeR[n2];
		float z2 = nodeZ[n2];
		float r3 = nodeR[n3];
		float z3 = nodeZ[n3];
		float rAvg = (r0 + r1 + r2 + r3) * 0.25f;
		float zAvg = (z0 + z1 + z2 + z3) * 0.25f;
		float r0c = r0 - rAvg;
		float z0c = z0 - zAvg;
		float r1c = r1 - rAvg;
		float z1c = z1 - zAvg;
		float r2c = r2 - rAvg;
		float z2c = z2 - zAvg;
		float r3c = r3 - rAvg;
		float z3c = z3 - zAvg;
		float c01 = z1c * r0c - r1c * z0c;
		float c12 = z2c * r1c - r2c * z1c;
		float c23 = z3c * r2c - r3c * z2c;
		float c30 = r3c * z0c - z3c * r0c;
		float sum = c12 + c01 + c23 + c30;
		area = sum * 0.5f;
		if (1E-20f > Math.Abs(sum))
		{
			rc = rAvg;
			return;
		}
		rc = (r2c + r1c) * c12 + (r1c + r0c) * c01 + (r3c + r2c) * c23 + (r3c + r0c) * c30;
		rc = rc / (sum * 3f) + rAvg;
		if (rc < 0f)
		{
			rc = 0f;
		}
	}

	private void BuildAdjacency()
	{
		int[] counts = new int[nodeCount];
		for (int e = 0; e < elemCount; e++)
		{
			for (int c = 0; c < 4; c++)
			{
				counts[elemNodes[e * 4 + c]]++;
			}
		}
		nodeAdjStart = new NativeArray<int>(nodeCount + 1, Allocator.Persistent);
		int total = 0;
		for (int n = 0; n < nodeCount; n++)
		{
			nodeAdjStart[n] = total;
			total += counts[n];
		}
		nodeAdjStart[nodeCount] = total;
		nodeAdjElem = new NativeArray<int>(total, Allocator.Persistent);
		nodeAdjCorner = new NativeArray<byte>(total, Allocator.Persistent);
		int[] next = new int[nodeCount];
		for (int n = 0; n < nodeCount; n++)
		{
			next[n] = nodeAdjStart[n];
		}
		for (int e = 0; e < elemCount; e++)
		{
			for (int c = 0; c < 4; c++)
			{
				int n = elemNodes[e * 4 + c];
				nodeAdjElem[next[n]] = e;
				nodeAdjCorner[next[n]] = (byte)c;
				next[n]++;
			}
		}
	}

	private void LumpNodalMass()
	{
		float minMass = 1E-18f;
		float quarter = 0.25f;
		for (int n = 0; n < nodeCount; n++)
		{
			float m = 0f;
			for (int k = nodeAdjStart[n]; k < nodeAdjStart[n + 1]; k++)
			{
				m += elemMass[nodeAdjElem[k]] * quarter;
			}
			if (m <= minMass)
			{
				m = minMass;
			}
			nodeMass[n] = m;
		}
	}

	private void ValidateGeometry(SolverDiagnostics diag)
	{
		int bad = 0;
		for (int e = 0; e < elemCount; e++)
		{
			if (elemArea[e] <= 0f)
			{
				bad++;
			}
		}
		if (bad > 0)
		{
			diag.Warn(string.Format("вырожденных элементов при построении: {0}", bad));
		}
		diag.elementsInitial = elemCount;
		diag.nodesInitial = nodeCount;
		diag.elementsAlive = elemCount;
		float totalMass = 0f;
		float rodMass = 0f;
		float targetMass = 0f;
		for (int e = 0; e < elemCount; e++)
		{
			totalMass += elemMass[e];
			if (elemBody[e] == 0)
			{
				rodMass += elemMass[e];
			}
			else
			{
				targetMass += elemMass[e];
			}
		}
		diag.initialMassKg = totalMass;
		diag.initialMassRodKg = rodMass;
		diag.initialMassTargetKg = targetMass;
	}

	public static AxiMesh BuildBlock(ContinuumScenario s, float R, float Z, int nr, int nz, SolverDiagnostics diag)
	{
		float h;
		if (s.hOverrideM > 0f)
		{
			h = s.hOverrideM;
		}
		else
		{
			h = R / nr;
			if (Z / nz < h)
			{
				h = Z / nz;
			}
		}
		AxiMesh mesh = new AxiMesh();
		mesh.hCharacteristic = h;
		mesh.axisTol = h * AxisTolFactor;
		Patch patch = default(Patch);
		patch.nCol = nr;
		patch.nRow = nz;
		patch.body = 0;
		patch.mat = s.rodMaterial;
		patch.clampFar = false;
		patch.r = new float[(nr + 1) * (nz + 1)];
		patch.z = new float[(nr + 1) * (nz + 1)];
		for (int i = 0; i <= nr; i++)
		{
			for (int j = 0; j <= nz; j++)
			{
				int idx = patch.NodeIndex(i, j);
				patch.r[idx] = (float)i * R / nr;
				patch.z[idx] = (float)j * Z / nz;
			}
		}
		mesh.patches.Add(patch);
		mesh.Assemble(s, diag);
		return mesh;
	}

	public int BlockNode(int nr, int i, int j)
	{
		return (nr + 1) * j + i;
	}

	private void AllocateNodes(int n)
	{
		nodeR = new NativeArray<float>(n, Allocator.Persistent);
		nodeZ = new NativeArray<float>(n, Allocator.Persistent);
		nodeVr = new NativeArray<float>(n, Allocator.Persistent);
		nodeVz = new NativeArray<float>(n, Allocator.Persistent);
		nodeVrPrev = new NativeArray<float>(n, Allocator.Persistent);
		nodeVzPrev = new NativeArray<float>(n, Allocator.Persistent);
		nodeMass = new NativeArray<float>(n, Allocator.Persistent);
		nodeFr = new NativeArray<float>(n, Allocator.Persistent);
		nodeFz = new NativeArray<float>(n, Allocator.Persistent);
		nodeOnAxis = new NativeArray<byte>(n, Allocator.Persistent);
		nodeFixed = new NativeArray<byte>(n, Allocator.Persistent);
		nodeBody = new NativeArray<byte>(n, Allocator.Persistent);
		nodeOrphan = new NativeArray<byte>(n, Allocator.Persistent);
	}

	private void AllocateElements(int n)
	{
		elemNodes = new NativeArray<int>(n * 4, Allocator.Persistent);
		elemAlive = new NativeArray<byte>(n, Allocator.Persistent);
		elemBody = new NativeArray<byte>(n, Allocator.Persistent);
		elemMat = new NativeArray<int>(n, Allocator.Persistent);
		elemRho = new NativeArray<float>(n, Allocator.Persistent);
		elemP = new NativeArray<float>(n, Allocator.Persistent);
		elemSrr = new NativeArray<float>(n, Allocator.Persistent);
		elemSzz = new NativeArray<float>(n, Allocator.Persistent);
		elemStt = new NativeArray<float>(n, Allocator.Persistent);
		elemSrz = new NativeArray<float>(n, Allocator.Persistent);
		elemEnergy = new NativeArray<float>(n, Allocator.Persistent);
		elemEpsP = new NativeArray<float>(n, Allocator.Persistent);
		elemEpsRate = new NativeArray<float>(n, Allocator.Persistent);
		elemTemp = new NativeArray<float>(n, Allocator.Persistent);
		elemDamage = new NativeArray<float>(n, Allocator.Persistent);
		elemQ = new NativeArray<float>(n, Allocator.Persistent);
		elemVol0 = new NativeArray<float>(n, Allocator.Persistent);
		elemVol = new NativeArray<float>(n, Allocator.Persistent);
		elemMass = new NativeArray<float>(n, Allocator.Persistent);
		elemArea = new NativeArray<float>(n, Allocator.Persistent);
		elemRc = new NativeArray<float>(n, Allocator.Persistent);
		elemDtCand = new NativeArray<float>(n, Allocator.Persistent);
		elemHgWork = new NativeArray<float>(n, Allocator.Persistent);
		elemVisWork = new NativeArray<float>(n, Allocator.Persistent);
		elemKillFlag = new NativeArray<byte>(n, Allocator.Persistent);
		elemForce = new NativeArray<float>(n * 8, Allocator.Persistent);
	}

	public void Dispose()
	{
		Free(ref nodeR);
		Free(ref nodeZ);
		Free(ref nodeVr);
		Free(ref nodeVz);
		Free(ref nodeVrPrev);
		Free(ref nodeVzPrev);
		Free(ref nodeMass);
		Free(ref nodeFr);
		Free(ref nodeFz);
		FreeB(ref nodeOnAxis);
		FreeB(ref nodeFixed);
		FreeB(ref nodeBody);
		FreeB(ref nodeOrphan);
		FreeI(ref elemNodes);
		FreeB(ref elemAlive);
		FreeB(ref elemBody);
		FreeI(ref elemMat);
		Free(ref elemRho);
		Free(ref elemP);
		Free(ref elemSrr);
		Free(ref elemSzz);
		Free(ref elemStt);
		Free(ref elemSrz);
		Free(ref elemEnergy);
		Free(ref elemEpsP);
		Free(ref elemEpsRate);
		Free(ref elemTemp);
		Free(ref elemDamage);
		Free(ref elemQ);
		Free(ref elemVol0);
		Free(ref elemVol);
		Free(ref elemMass);
		Free(ref elemArea);
		Free(ref elemRc);
		Free(ref elemDtCand);
		Free(ref elemHgWork);
		Free(ref elemVisWork);
		FreeB(ref elemKillFlag);
		Free(ref elemForce);
		FreeI(ref nodeAdjStart);
		FreeI(ref nodeAdjElem);
		FreeB(ref nodeAdjCorner);
	}

	private static void Free(ref NativeArray<float> a)
	{
		if (a.IsCreated)
		{
			a.Dispose();
		}
	}

	private static void FreeI(ref NativeArray<int> a)
	{
		if (a.IsCreated)
		{
			a.Dispose();
		}
	}

	private static void FreeB(ref NativeArray<byte> a)
	{
		if (a.IsCreated)
		{
			a.Dispose();
		}
	}
}
