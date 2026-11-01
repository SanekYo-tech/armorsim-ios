using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct ContactPenaltyJob : IJob
{
	[ReadOnly]
	public NativeArray<float> nodeR;

	[ReadOnly]
	public NativeArray<float> nodeZ;

	[ReadOnly]
	public NativeArray<float> nodeVr;

	[ReadOnly]
	public NativeArray<float> nodeVz;

	[ReadOnly]
	public NativeArray<float> nodeMass;

	[ReadOnly]
	public NativeArray<int> segA;

	[ReadOnly]
	public NativeArray<int> segB;

	[ReadOnly]
	public NativeArray<int> surfNodes;

	public NativeArray<float> nodeFr;

	public NativeArray<float> nodeFz;

	public NativeArray<double> work;

	public int projSegStart;

	public int projSegEnd;

	public int targSegStart;

	public int targSegEnd;

	public int projNodeStart;

	public int projNodeEnd;

	public int targNodeStart;

	public int targNodeEnd;

	public NativeArray<int> cellStart;

	public NativeArray<int> cellCursor;

	public NativeArray<int> cellItems;

	public int gridMaxSide;

	public NativeArray<float> rampDepth;

	public NativeArray<float> rampOffset;

	public NativeArray<float> nodeCFr;

	public NativeArray<float> nodeCFz;

	public float dt;

	public float penaltyScale;

	public float dampingRatio;

	public float friction;

	public float hChar;

	public float pushSpeed;

	public byte anvilEnabled;

	public float anvilZ;

	private int gNr;

	private int gNz;

	private float gCell;

	private float gR0;

	private float gZ0;

	public void Execute()
	{
		work[0] = 0.0;
		work[1] = 0.0;
		work[2] = 0.0;
		work[3] = 0.0;
		if (anvilEnabled != 0)
		{
			for (int i = projNodeStart; i < projNodeEnd; i++)
			{
				AnvilContact(surfNodes[i]);
			}
			return;
		}
		BuildGrid(targSegStart, targSegEnd);
		for (int j = projNodeStart; j < projNodeEnd; j++)
		{
			SlaveAgainstMaster(surfNodes[j], targSegStart, targSegEnd);
		}
		BuildGrid(projSegStart, projSegEnd);
		for (int k = targNodeStart; k < targNodeEnd; k++)
		{
			SlaveAgainstMaster(surfNodes[k], projSegStart, projSegEnd);
		}
	}

	private void AnvilContact(int s)
	{
		float gap = nodeZ[s] - anvilZ;
		if (0f >= gap)
		{
			rampDepth[s] = 0f;
			rampOffset[s] = 0f;
			return;
		}
		float m = nodeMass[s];
		float kp = m * penaltyScale / (dt * dt);
		float cc = Mathf.Sqrt(kp * m);
		float depth = rampDepth[s] + (Mathf.Abs(nodeVz[s]) + pushSpeed) * dt;
		if (depth > gap)
		{
			depth = gap;
		}
		rampDepth[s] = depth;
		float vz = nodeVz[s];
		if (vz <= 0f)
		{
			vz = 0f;
		}
		float fn = 2f * dampingRatio * cc * vz + kp * depth;
		if (0f >= fn)
		{
			return;
		}
		float ft = TangentialForce(nodeVr[s], fn);
		work[1] += 1.0;
		if ((double)gap > work[2])
		{
			work[2] = gap;
		}
		work[3] += (double)(fn * dt);
		nodeFr[s] += ft;
		nodeFz[s] -= fn;
		nodeCFr[s] += ft;
		nodeCFz[s] -= fn;
	}

	private float TangentialForce(float vt, float fn)
	{
		float a = ((0f > vt) ? (-vt) : vt);
		if (1e-09f > a)
		{
			return 0f;
		}
		float num = a / (a + 1f) * (friction * fn);
		if (vt > 0f)
		{
			return -num;
		}
		return num;
	}

	private void SlaveAgainstMaster(int s, int from, int to)
	{
		int cr = CellOf(nodeR[s], gR0);
		int cz = CellOf(nodeZ[s], gZ0);
		float bestPen = hChar;
		float bestT = 0f;
		float bestNx = 0f;
		float bestNz = 0f;
		int bestSeg = -1;
		for (int dzOff = -1; dzOff <= 1; dzOff++)
		{
			for (int drOff = 0; drOff < 3; drOff++)
			{
				int dr = cr - 1 + drOff;
				int dz = cz - 1 + dzOff;
				if (dr < 0 || dz < 0 || dr >= gNr || dz >= gNz)
				{
					continue;
				}
				int cell = dz * gNr + dr;
				int start = cellStart[cell];
				int end = cellStart[cell + 1];
				for (int k = start; k < end; k++)
				{
					int seg = cellItems[k];
					if (seg < from || seg >= to)
					{
						continue;
					}
					int a = segA[seg];
					int b = segB[seg];
					if (a == s || b == s)
					{
						continue;
					}
					float er = nodeR[b] - nodeR[a];
					float ez = nodeZ[b] - nodeZ[a];
					float lenSqr = ez * ez + er * er;
					if (1e-20f > lenSqr)
					{
						continue;
					}
					float t = ((nodeZ[s] - nodeZ[a]) * ez + (nodeR[s] - nodeR[a]) * er) / lenSqr;
					if (t < -0.1f || t > 1.1f)
					{
						continue;
					}
					if (0f > t)
					{
						t = 0f;
					}
					else if (t > 1f)
					{
						t = 1f;
					}
					float len = Mathf.Sqrt(lenSqr);
					float nx = ez / len;
					float nz = -er / len;
					float pen = -((nodeR[s] - (t * er + nodeR[a])) * nx + (nodeZ[s] - (t * ez + nodeZ[a])) * nz);
					if (pen <= 0f || bestPen <= pen)
					{
						continue;
					}
					bestPen = pen;
					bestNx = nx;
					bestNz = nz;
					bestSeg = seg;
					bestT = t;
				}
			}
		}
		if (bestSeg < 0)
		{
			rampDepth[s] = 0f;
			rampOffset[s] = 0f;
			return;
		}
		int ma = segA[bestSeg];
		int mb = segB[bestSeg];
		float oneMinusT = 1f - bestT;
		float ms = nodeMass[s];
		float invMeff = bestT * bestT / nodeMass[mb] + oneMinusT * oneMinusT / nodeMass[ma];
		float meff = ((!(invMeff <= 1.49e-09f)) ? (1f / invMeff) : ms);
		float mred = meff * ms / (meff + ms);
		float kp = mred * penaltyScale / (dt * dt);
		float cc = Mathf.Sqrt(kp * mred);
		float vRelR = nodeVr[s] - (oneMinusT * nodeVr[ma] + bestT * nodeVr[mb]);
		float vRelZ = nodeVz[s] - (oneMinusT * nodeVz[ma] + bestT * nodeVz[mb]);
		float vn = vRelZ * bestNz + vRelR * bestNx;
		float rampDep = rampDepth[s];
		float rampOff = rampOffset[s];
		float pen2 = bestPen - rampOff;
		float allow = (Mathf.Abs(vn) + pushSpeed) * dt;
		if (pen2 < 0f)
		{
			pen2 = 0f;
			rampOff = bestPen;
		}
		if (pen2 > allow + rampDep)
		{
			pen2 = allow + rampDep;
			rampOff = bestPen - pen2;
		}
		if (rampOff > hChar)
		{
			rampOff = hChar;
			pen2 = bestPen - rampOff;
		}
		if (0f > pen2)
		{
			pen2 = 0f;
		}
		rampOffset[s] = rampOff;
		rampDepth[s] = pen2;
		float fn = pen2 * kp - vn * cc * 2f * dampingRatio;
		if (0f >= fn)
		{
			return;
		}
		float vtR = vRelR - vn * bestNx;
		float vtZ = vRelZ - vn * bestNz;
		float ftR = fn * bestNx;
		float ftZ = fn * bestNz;
		float vtLen = Mathf.Sqrt(vtZ * vtZ + vtR * vtR);
		if (vtLen > 1e-09f)
		{
			float fric = vtLen / (vtLen + 1f) * (friction * fn);
			ftZ -= fric * vtZ / vtLen;
			ftR -= fric * vtR / vtLen;
		}
		work[1] += 1.0;
		if ((double)bestPen > work[2])
		{
			work[2] = bestPen;
		}
		work[3] += (double)(fn * dt);
		nodeFr[s] += ftR;
		nodeFz[s] += ftZ;
		nodeFr[ma] -= ftR * oneMinusT;
		nodeFz[ma] -= ftZ * oneMinusT;
		nodeFr[mb] -= ftR * bestT;
		nodeFz[mb] -= ftZ * bestT;
		nodeCFr[s] += ftR;
		nodeCFz[s] += ftZ;
		nodeCFr[ma] -= ftR * oneMinusT;
		nodeCFz[ma] -= ftZ * oneMinusT;
		nodeCFr[mb] -= ftR * bestT;
		nodeCFz[mb] -= ftZ * bestT;
	}

	private int CellOf(float x, float origin)
	{
		return (int)((x - origin) / gCell);
	}

	private void BuildGrid(int from, int to)
	{
		gCell = ((hChar > 0f) ? (hChar + hChar) : 1f);
		gNr = 1;
		gNz = 1;
		gR0 = 0f;
		cellStart[0] = 0;
		if (to <= from)
		{
			return;
		}
		float rMin = float.MaxValue;
		float rMax = 0f - float.MaxValue;
		float zMin = float.MaxValue;
		float zMax = 0f - float.MaxValue;
		for (int i = from; i < to; i++)
		{
			Extend(segA[i], ref rMin, ref rMax, ref zMin, ref zMax);
			Extend(segB[i], ref rMin, ref rMax, ref zMin, ref zMax);
		}
		gR0 = rMin - gCell;
		gZ0 = zMin - gCell;
		float num = 2f * gCell + (rMax - rMin);
		float num2 = 2f * gCell + (zMax - zMin);
		gNr = Mathf.Clamp((int)Math.Ceiling(num / gCell), 1, gridMaxSide);
		gNz = Mathf.Clamp((int)Math.Ceiling(num2 / gCell), 1, gridMaxSide);
		float num3 = (2f * gCell + (rMax - rMin)) / gNr;
		float num4 = (2f * gCell + (zMax - zMin)) / gNz;
		if (num3 <= num4)
		{
			num3 = num4;
		}
		if (num3 > gCell)
		{
			gCell = num3;
			gR0 = rMin - gCell;
			gZ0 = zMin - gCell;
			gNr = Mathf.Clamp((int)Math.Ceiling((2f * gCell + (rMax - rMin)) / gCell), 1, gridMaxSide);
			gNz = Mathf.Clamp((int)Math.Ceiling((2f * gCell + (zMax - zMin)) / gCell), 1, gridMaxSide);
		}
		int num5 = gNz * gNr;
		for (int j = 0; j <= num5; j++)
		{
			cellStart[j] = 0;
		}
		for (int k = 0; k < num5; k++)
		{
			cellCursor[k] = 0;
		}
		for (int l = from; l < to; l++)
		{
			int num6 = CellOfSegment(l);
			if (num6 >= 0)
			{
				cellStart[num6 + 1]++;
			}
		}
		for (int m = 1; m <= num5; m++)
		{
			cellStart[m] += cellStart[m - 1];
		}
		for (int n = from; n < to; n++)
		{
			int num7 = CellOfSegment(n);
			if (num7 >= 0)
			{
				int num8 = cellStart[num7] + cellCursor[num7];
				cellItems[num8] = n;
				cellCursor[num7]++;
			}
		}
	}

	private int CellOfSegment(int i)
	{
		int a = segA[i];
		int b = segB[i];
		int rCell = CellOf((nodeR[b] + nodeR[a]) * 0.5f, gR0);
		int zCell = CellOf((nodeZ[b] + nodeZ[a]) * 0.5f, gZ0);
		if (rCell < 0 || zCell < 0 || rCell >= gNr || zCell >= gNz)
		{
			return -1;
		}
		return zCell * gNr + rCell;
	}

	private void Extend(int n, ref float rMin, ref float rMax, ref float zMin, ref float zMax)
	{
		float r = nodeR[n];
		float z = nodeZ[n];
		if (rMin > r)
		{
			rMin = r;
		}
		if (r > rMax)
		{
			rMax = r;
		}
		if (zMin > z)
		{
			zMin = z;
		}
		if (z > zMax)
		{
			zMax = z;
		}
	}
}
