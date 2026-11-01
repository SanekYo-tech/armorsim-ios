using UnityEngine;

public static class SuperConstitutive
{
	public static float ViscosityQuadratic;

	public static float ViscosityLinear;

	static SuperConstitutive()
	{
		ViscosityQuadratic = 1.5f;
		ViscosityLinear = 0.06f;
	}

	public static SuperMat3 Update(in ContinuumProperties m, SuperMat3 L, float J, float dt, float hChar, ref SuperMat3 s, ref float epsP, ref float T, ref float e, ref float epsRate, out float pressure, out float qVisc, bool elasticOnly)
	{
		float damage = 0f;
		byte failureCause = 0;
		return Update(m, L, J, dt, hChar, ref s, ref epsP, ref T, ref e, ref epsRate, out pressure, out qVisc, elasticOnly, ref damage, ref failureCause);
	}

	public static SuperMat3 Update(in ContinuumProperties m, SuperMat3 L, float J, float dt, float hChar, ref SuperMat3 s, ref float epsP, ref float T, ref float e, ref float epsRate, out float pressure, out float qVisc, bool elasticOnly, ref float damage, float failureStrainScale = 1f)
	{
		byte failureCause = 0;
		return Update(m, L, J, dt, hChar, ref s, ref epsP, ref T, ref e, ref epsRate, out pressure, out qVisc, elasticOnly, ref damage, ref failureCause, noFragmentation: false, failureStrainScale);
	}

	public static SuperMat3 Update(in ContinuumProperties m, SuperMat3 L, float J, float dt, float hChar, ref SuperMat3 s, ref float epsP, ref float T, ref float e, ref float epsRate, out float pressure, out float qVisc, bool elasticOnly, ref float damage, ref byte failureCause, bool noFragmentation = false, float failureStrainScale = 1f)
	{
		bool cracked = SuperDamage.Enabled && damage >= 1f && (!noFragmentation || failureCause == 4);
		SuperMat3 sym = L.Sym;
		SuperMat3 skew = L.Skew;
		float trL = sym.Trace;
		float rhoRaw = m.rho0 / J;
		float c = Mathf.Sqrt((m.G * 4f / 3f + m.K) / rhoRaw);
		float mu = 1f / J - 1f;
		float q = 0f;
		if (trL < 0f)
		{
			q = (hChar * ViscosityQuadratic * trL * trL - c * ViscosityLinear * trL) * (rhoRaw * hChar);
		}
		float rho = (rhoRaw > SuperDamage.RhoFloorKgM3) ? rhoRaw : SuperDamage.RhoFloorKgM3;
		float invRho = 1f / rho;
		float dd1 = SuperMat3.DoubleDot(s, sym);
		float cavFloor = 0f;
		if (SuperDamage.CavitationOn && m.G == 0f && m.cavitationPa < 0f)
		{
			if (SuperDamage.CavitationOverridePa < 0f)
			{
				cavFloor = SuperDamage.CavitationOverridePa;
			}
			else
			{
				float ceiling = SuperDamage.CavitationCeilingPa(m);
				cavFloor = (ceiling > m.cavitationPa) ? ceiling : m.cavitationPa;
			}
		}
		bool cavApplies = cavFloor < 0f && !SuperDamage.SabotageNoCavitation;
		bool zeroTension = !SuperDamage.SabotageDebrisTension && cracked && SuperDamage.DebrisWorkApplied;
		bool coherentFloor = noFragmentation && !cracked && SuperDamage.CoherentTensionFloorPa < 0f && SuperDamage.DebrisWorkApplied;
		float p0 = ContinuumMaterials.PressureFromMu(m, mu, e);
		if (cavApplies && p0 < cavFloor)
		{
			p0 = cavFloor;
		}
		if (zeroTension && p0 < 0f)
		{
			p0 = 0f;
		}
		if (coherentFloor && p0 < SuperDamage.CoherentTensionFloorPa)
		{
			p0 = SuperDamage.CoherentTensionFloorPa;
		}
		float eTrial = (dd1 - (p0 + q) * trL) * dt * invRho + e;
		float p2 = ContinuumMaterials.PressureFromMu(m, mu, eTrial);
		if (cavApplies && p2 < cavFloor)
		{
			p2 = cavFloor;
		}
		if (zeroTension && p2 < 0f)
		{
			p2 = 0f;
		}
		if (coherentFloor && p2 < SuperDamage.CoherentTensionFloorPa)
		{
			p2 = SuperDamage.CoherentTensionFloorPa;
		}
		SuperMat3 rot = skew * s - s * skew;
		SuperMat3 dev = sym;
		float t3 = trL / 3f;
		dev.m00 -= t3;
		dev.m11 -= t3;
		dev.m22 -= t3;
		s = s + rot * dt + dev * ((m.G + m.G) * dt);
		float trS = s.Trace / 3f;
		s.m00 -= trS;
		s.m11 -= trS;
		s.m22 -= trS;
		if (!SuperDamage.SabotageDebrisTension && cracked)
		{
			s = SuperMat3.Zero;
		}
		float sigEq = SuperMat3.VonMises(s);
		float dEpsP = 0f;
		float flowStress = 0f;
		if (elasticOnly || (!SuperDamage.SabotageDebrisTension && cracked))
		{
			epsRate = 0f;
		}
		else
		{
			float rate = epsRate;
			float scale;
			if (!SuperDamage.Enabled)
			{
				scale = 1f;
			}
			else
			{
				float dmg1 = (1f > damage) ? damage : 1f;
				scale = 1f - dmg1 * SuperDamage.SofteningCoefficient;
				scale = (scale > SuperDamage.SofteningFloor) ? scale : SuperDamage.SofteningFloor;
				if (SuperDamage.AsbErosionOn)
				{
					scale = ContinuumMaterials.AsbFade(m, epsP) * (1f - dmg1 * SuperDamage.SofteningCoefficient);
					if (scale < SuperDamage.SofteningFloor)
					{
						scale = SuperDamage.SofteningFloor;
					}
				}
			}
			int it = 0;
			while (true)
			{
				if (it >= SuperDamage.YieldIterations)
				{
					if (dEpsP > 0f && sigEq > SuperDamage.SigEqReturnFloorPa)
					{
						s = s * (flowStress / sigEq);
						epsP += dEpsP;
						epsRate = dEpsP / dt;
						T = 0.9f / (rhoRaw * m.C_p) * flowStress * dEpsP + T;
					}
					else
					{
						epsRate = 0f;
					}
					break;
				}
				flowStress = ContinuumMaterials.FlowStress(m, epsP, rate, T) * scale;
				if (flowStress >= sigEq)
				{
					dEpsP = 0f;
					epsRate = 0f;
					break;
				}
				dEpsP = (sigEq - flowStress) / (m.G * 3f);
				rate = dEpsP / dt;
				it++;
			}
		}
		float dd2 = SuperMat3.DoubleDot(s, sym);
		e = ((dd2 + dd1) * 0.5f - ((p2 + p0) * 0.5f + q) * trL) * dt * invRho + e;
		float p = ContinuumMaterials.PressureFromMu(m, mu, e);
		if (cavApplies && p < cavFloor)
		{
			p = cavFloor;
		}
		bool zeroStress = false;
		if (SuperDamage.Enabled && !cracked)
		{
			if (m.hasDamage != 0 && dEpsP > 0f)
			{
				float ef = ContinuumMaterials.FailureStrain(m, p2, flowStress, epsRate, T);
				if (SuperDamage.SabotageNoFailure)
				{
					ef *= 1e+09f;
				}
				if (SuperDamage.SabotageEfCollapse)
				{
					ef *= 1e-09f;
					ef = (ef > 0.001f) ? ef : 0.001f;
				}
				if (failureStrainScale != 1f)
				{
					ef *= failureStrainScale;
				}
				float dNew = dEpsP / ef + damage;
				damage = (10f > dNew) ? dNew : 10f;
				if (failureCause == 0 && damage >= 1f)
				{
					failureCause = 1;
				}
			}
			float spallThreshold = m.spallPa;
			if (SuperDamage.InterlayerSpallOn && m.G == 0f && m.spallPa == 0f && m.cavitationPa < 0f)
			{
				spallThreshold = (SuperDamage.CavitationOverridePa < 0f) ? SuperDamage.CavitationOverridePa : m.cavitationPa;
			}
			if (!SuperDamage.SabotageNoSpall && spallThreshold < 0f && p <= spallThreshold)
			{
				damage = (damage > 1f) ? damage : 1f;
				if (failureCause == 0)
				{
					failureCause = 2;
				}
			}
			if (!SuperDamage.SabotageNoShear && m.sigma1MaxPa > 0f)
			{
				float sigEqArg = (dEpsP > 0f && sigEq > SuperDamage.SigEqReturnFloorPa) ? flowStress : sigEq;
				float sigma1 = SuperMat3.MaxPrincipal(s, sigEqArg, p + q, m.G * 1e-06f);
				if (sigma1 >= m.sigma1MaxPa)
				{
					damage = (damage > 1f) ? damage : 1f;
					if (failureCause == 0)
					{
						failureCause = 3;
					}
				}
			}
			if (SuperDamage.AsbErosionOn && m.hasASB != 0)
			{
				float fade = (m.dEpsFade > 0f) ? m.dEpsFade : 0f;
				if (epsP >= fade + m.epsASB)
				{
					damage = (damage > 1f) ? damage : 1f;
					if (failureCause == 0)
					{
						failureCause = (byte)SuperDamage.CauseAsbErosion;
					}
				}
			}
			if (damage >= 1f && !noFragmentation && !SuperDamage.SabotageDebrisTension)
			{
				s = SuperMat3.Zero;
				zeroStress = true;
			}
		}
		if (zeroStress || (!SuperDamage.SabotageDebrisTension && cracked))
		{
			if (p < 0f)
			{
				p = 0f;
			}
		}
		if (noFragmentation && SuperDamage.CoherentTensionFloorPa < 0f && p < SuperDamage.CoherentTensionFloorPa)
		{
			p = SuperDamage.CoherentTensionFloorPa;
		}
		float cap = m.K * SuperDamage.PressureCapFactorN;
		if (p > cap)
		{
			p = cap;
		}
		pressure = p;
		qVisc = q;
		SuperMat3 result = s;
		float diag = -(p + q);
		result.m00 += diag;
		result.m11 += diag;
		result.m22 += diag;
		return result;
	}
}
