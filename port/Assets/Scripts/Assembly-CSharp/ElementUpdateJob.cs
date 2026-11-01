using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct ElementUpdateJob : IJobParallelFor
{
	[ReadOnly]
	public NativeArray<int> elemNodes;

	[ReadOnly]
	public NativeArray<int> elemMat;

	[ReadOnly]
	public NativeArray<byte> elemAlive;

	[ReadOnly]
	public NativeArray<float> elemMass;

	[ReadOnly]
	public NativeArray<float> elemVol0;

	[ReadOnly]
	public NativeArray<float> nodeR;

	[ReadOnly]
	public NativeArray<float> nodeZ;

	[ReadOnly]
	public NativeArray<float> nodeVr;

	[ReadOnly]
	public NativeArray<float> nodeVz;

	[ReadOnly]
	public NativeArray<ContinuumProperties> materials;

	public NativeArray<float> elemRho;

	public NativeArray<float> elemP;

	public NativeArray<float> elemQ;

	public NativeArray<float> elemSrr;

	public NativeArray<float> elemSzz;

	public NativeArray<float> elemStt;

	public NativeArray<float> elemSrz;

	public NativeArray<float> elemEnergy;

	public NativeArray<float> elemEpsP;

	public NativeArray<float> elemEpsRate;

	public NativeArray<float> elemTemp;

	public NativeArray<float> elemDamage;

	public NativeArray<float> elemVol;

	public NativeArray<float> elemArea;

	public NativeArray<float> elemRc;

	public NativeArray<float> elemDtCand;

	public NativeArray<float> elemHgWork;

	public NativeArray<float> elemVisWork;

	public NativeArray<byte> elemKillFlag;

	[NativeDisableParallelForRestriction]
	public NativeArray<float> elemForce;

	public float dt;

	public float axisTol;

	public float hourglassQ;

	public float viscQuad;

	public float viscLin;

	public float erodeEpsP;

	public float erodeVolFrac;

	public float dtFloor;

	public byte enableDamage;

	public byte enableAsb;

	public void Execute(int e)
	{
		if (elemAlive[e] == 0)
		{
			elemKillFlag[e] = 1;
			elemDtCand[e] = 1e+30f;
			elemHgWork[e] = 0f;
			elemVisWork[e] = 0f;
			elemForce[e * 8] = 0f;
			elemForce[e * 8 + 1] = 0f;
			elemForce[e * 8 + 2] = 0f;
			elemForce[e * 8 + 3] = 0f;
			elemForce[e * 8 + 4] = 0f;
			elemForce[e * 8 + 5] = 0f;
			elemForce[e * 8 + 6] = 0f;
			elemForce[e * 8 + 7] = 0f;
			return;
		}
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
		float rc = (r1 + r0 + r2 + r3) * 0.25f;
		float zc = (z1 + z0 + z2 + z3) * 0.25f;
		float x1 = r1 - rc;
		float y1 = z1 - zc;
		float x2 = r2 - rc;
		float y2 = z2 - zc;
		float x3 = r3 - rc;
		float y3 = z3 - zc;
		float x0 = r0 - rc;
		float y0 = z0 - zc;
		float a1 = y1 * x0 - x1 * y0;
		float a2 = y2 * x1 - x2 * y1;
		float a3 = y3 * x2 - x3 * y2;
		float a4 = x3 * y0 - y3 * x0;
		float area2 = a1 + a2 + a3 + a4;
		float areaH = area2 * 0.5f;
		if (0f >= areaH)
		{
			elemKillFlag[e] = 1;
			return;
		}
		rc = ((x2 + x1) * a2 + (x1 + x0) * a1 + (x3 + x2) * a3 + (x3 + x0) * a4) / (area2 * 3f) + rc;
		if (rc < 0f)
		{
			rc = 0f;
		}
		float vol = rc * 6.28318548f;
		vol *= areaH;
		elemArea[e] = areaH;
		elemRc[e] = rc;
		elemVol[e] = vol;
		ContinuumProperties m = materials[elemMat[e]];
		float rho = ((vol <= 1e-20f) ? 999.99994f : (elemVol0[e] / vol));
		float mu = rho - 1f;
		elemRho[e] = m.rho0 * rho;
		float vr0 = nodeVr[n0];
		float vr1 = nodeVr[n1];
		float vr2 = nodeVr[n2];
		float vr3 = nodeVr[n3];
		float vz0 = nodeVz[n0];
		float vz1 = nodeVz[n1];
		float vz2 = nodeVz[n2];
		float vz3 = nodeVz[n3];
		float cz31 = (z3 - z1) * 0.5f;
		float cz13 = (z1 - z3) * 0.5f;
		float cz02 = (z0 - z2) * 0.5f;
		float cz20 = (z2 - z0) * 0.5f;
		float cr31 = (r3 - r1) * 0.5f;
		float cr13 = (r1 - r3) * 0.5f;
		float cr02 = (r0 - r2) * 0.5f;
		float cr20 = (r2 - r0) * 0.5f;
		float invA = 1f / areaH;
		float drr = (vr1 * cr02 + vr0 * cr31 + vr2 * cr13 + vr3 * cr20) * invA;
		float dzz = (vz1 * cz20 + vz0 * cz13 + vz2 * cz31 + vz3 * cz02) * invA;
		float drz = (vr1 * cz20 + vr0 * cz13 + vr2 * cz31 + vz3 * cz02) * invA;
		float dzr = (vz1 * cr02 + vz0 * cr31 + vz2 * cr13 + vz3 * cr20) * invA;
		float mean = (dzz + drr) * 0.5f;
		float dev = (drr - dzz) * 0.5f;
		float dtt = ((axisTol <= rc) ? (((vr1 + vr0 + vr2 + vr3) * 0.25f) / rc) : drz);
		float div = dzr + drz + dtt;
		float d1 = Dist(r0, z0, r2, z2);
		float d2 = Dist(r1, z1, r3, z3);
		float diag = ((d1 <= d2) ? d2 : d1);
		float lenR = ((diag <= 1e-12f) ? 0f : (areaH / diag));
		float csDen = 1f;
		if (csDen < rho)
		{
			csDen = rho;
		}
		float cs2 = (m.G * 4f / 3f + m.K) / csDen;
		float cs = Mathf.Sqrt(cs2);
		float q = 0f;
		if (div < 0f)
		{
			q = (lenR * viscQuad * div * div - cs * viscLin * div) * lenR * rho;
		}
		elemQ[e] = q;
		float srr = elemSrr[e];
		float szz = elemSzz[e];
		float stt = elemStt[e];
		float srz = elemSrz[e];
		float p = elemP[e];
		float energy = elemEnergy[e];
		float power = szz * dzr + srr * drz + stt * dtt + (srz + srz) * mean;
		float pmax = ((rho <= 1f) ? 1f : rho);
		float invPmax = 1f / pmax;
		float newP = ContinuumMaterials.PressureFromMu(in m, mu, (power - (p + q) * div) * dt * invPmax + energy);
		float g2 = m.G + m.G;
		float third = div / 3f;
		stt = (dtt - third) * g2 * dt + stt;
		float srrN = (dev + dev) * srz * dt + srr + (drz - third) * g2 * dt;
		float szzN = dev * -2f * srz * dt + szz + (dzr - third) * g2 * dt;
		float srzN = (srr - szz) * (0f - dev) * dt + srz + g2 * mean * dt;
		float meanS = (szzN + srrN + stt) / 3f;
		szzN -= meanS;
		stt -= meanS;
		srrN -= meanS;
		float se = Mathf.Sqrt(szzN * szzN + stt * stt + srrN * srrN + (srzN + srzN) * srzN * 1.5f);
		float epsP = elemEpsP[e];
		float temp = elemTemp[e];
		float asbF = 1f;
		if (enableAsb != 0)
		{
			asbF = ContinuumMaterials.AsbFade(in m, epsP);
		}
		int iter = 0;
		float deps = 0f;
		float epsRate = elemEpsRate[e];
		float y = 0f;
		while (true)
		{
			y = ContinuumMaterials.FlowStress(in m, epsP, epsRate, temp) * asbF;
			if (se <= y)
			{
				deps = 0f;
				break;
			}
			iter++;
			deps = (se - y) / (m.G * 3f);
			epsRate = deps / dt;
			if (iter > 2)
			{
				break;
			}
		}
		if (deps <= 0f || se <= 1e-06f)
		{
			elemEpsRate[e] = 0f;
		}
		else
		{
			float num = y / se;
			szzN *= num;
			stt *= num;
			srrN *= num;
			srzN *= num;
			epsP += deps;
			elemEpsRate[e] = deps / dt;
			se = y;
		}
		elemSrr[e] = srrN;
		elemSzz[e] = szzN;
		elemStt[e] = stt;
		elemSrz[e] = srzN;
		elemEpsP[e] = epsP;
		if (0f < deps)
		{
			temp += (0.9f / (m.C_p * rho)) * y * deps;
			elemTemp[e] = temp;
		}
		byte killBit = 0;
		if (enableDamage != 0 && m.hasDamage != 0 && 0f < deps)
		{
			float failStrain = ContinuumMaterials.FailureStrain(in m, newP, se, epsRate, temp);
			float damage = deps / failStrain + elemDamage[e];
			bool flag = 1f <= damage;
			elemDamage[e] = damage;
			killBit = (byte)(flag ? 32 : 0);
		}
		energy = ((szzN * dzr + srrN * drz + dtt * stt + (srzN + srzN) * mean + power) * 0.5f - ((newP + p) * 0.5f + q) * div) * dt * invPmax + energy;
		elemEnergy[e] = energy;
		float pTot = ContinuumMaterials.PressureFromMu(in m, mu, energy) + q;
		elemP[e] = pTot - q;
		srrN -= pTot;
		szzN -= pTot;
		stt -= pTot;
		elemVisWork[e] = (0f - q) * div * vol * dt;
		float rc2pi = rc * 6.28318548f;
		float area2pi = areaH * 6.28318548f;
		float dxsum = (r0 - r1) + r2 - r3;
		float dzsum = (z0 - z1) + z2 - z3;
		float w0 = 1f - (dzsum * cr31 + dxsum * cz13) * invA;
		float w1 = -1f - (dzsum * cr02 + dxsum * cz20) * invA;
		float w3 = -1f - (dzsum * cr20 + dxsum * cz02) * invA;
		float w2 = 1f - (dzsum * cr13 + dxsum * cz31) * invA;
		float fr = w1 * vr1 + w0 * vr0 + w2 * vr2 + w3 * vr3;
		float fz = w1 * vz1 + w0 * vz0 + w2 * vz2 + w3 * vz3;
		float hgs = Mathf.Sqrt(areaH);
		float hg = ((hgs <= 1e-12f) ? 0f : (rho * hourglassQ * cs * (vol / hgs) * 0.25f));
		float hoopF = area2pi * (stt - pTot) * 0.25f;
		float hgr0 = (0f - hg) * fr * w0;
		float hgr1 = (0f - hg) * fr * w1;
		float hgr2 = (0f - hg) * fr * w2;
		float hgr3 = (0f - hg) * fr * w3;
		float hgz0 = (0f - hg) * fz * w0;
		float hgz1 = (0f - hg) * fz * w1;
		float hgz2 = (0f - hg) * fz * w2;
		float hgz3 = (0f - hg) * fz * w3;
		elemHgWork[e] = (0f - (hgr1 * vr1 + hgr0 * vr0 + hgr2 * vr2 + hgr3 * vr3 + hgz0 * vz0 + hgz1 * vz1 + hgz2 * vz2 + hgz3 * vz3)) * dt;
		elemForce[e * 8] = hgr0 - (srzN * cr31 + srrN * cz13) * rc2pi;
		elemForce[e * 8 + 1] = hgz0 - (srzN * cz13 + szzN * cr31) * rc2pi;
		elemForce[e * 8 + 2] = hgr1 - ((srzN * cr02 + srrN * cz20) * rc2pi + hoopF);
		elemForce[e * 8 + 3] = hgz1 - (srzN * cz20 + szzN * cr02) * rc2pi;
		elemForce[e * 8 + 4] = hgr2 - ((srzN * cr13 + srrN * cz31) * rc2pi + hoopF);
		elemForce[e * 8 + 5] = hgz2 - (srzN * cz31 + szzN * cr13) * rc2pi;
		elemForce[e * 8 + 6] = hgr3 - ((srzN * cr20 + srrN * cz02) * rc2pi + hoopF);
		elemForce[e * 8 + 7] = hgz3 - (srzN * cz02 + szzN * cr20) * rc2pi;
		float l1 = Mag(vr1, vz1);
		float l2 = Mag(vr0, vz0);
		float l3 = Mag(vr2, vz2);
		float l4 = Mag(vr3, vz3);
		float dtCand = lenR / ((l1 + l2 + l3 + l4) * 0.25f + cs);
		elemDtCand[e] = ((dtCand <= 0f) ? 1e+30f : dtCand);
		byte b = killBit;
		if (epsP > erodeEpsP)
		{
			b = (byte)(killBit | 2);
		}
		byte b2 = b;
		if (elemVol0[e] * erodeVolFrac > vol)
		{
			b2 = (byte)(b | 4);
		}
		byte b3 = b2;
		if (dtFloor > dtCand)
		{
			b3 = (byte)(b2 | 8);
		}
		if (enableAsb != 0 && asbF <= 0f && m.hasASB != 0)
		{
			b3 = (byte)(b3 | 16);
		}
		elemKillFlag[e] = b3;
	}

	private static float Dist(float ar, float az, float br, float bz)
	{
		float num = (br - ar) * (br - ar) + (bz - az) * (bz - az);
		if (0f <= num)
		{
			return Mathf.Sqrt(num);
		}
		return 0f;
	}

	private static float Mag(float a, float b)
	{
		float num = a * a + b * b;
		if (0f <= num)
		{
			return Mathf.Sqrt(num);
		}
		return 0f;
	}
}
