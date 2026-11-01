using System;
using System.Diagnostics;
using UnityEngine;

public static class SuperHarness
{
	public const int MaxSteps = 200000;

	public static SuperResult RunTaylor(float L0, float D0, float spacing, float velocity, float T0, float durationS, ContinuumMaterialId material = ContinuumMaterialId.OFHC_Copper, SuperSabotage sabotage = SuperSabotage.None, float densityScale = 1f, bool anvil = true)
	{
		ContinuumProperties m = ContinuumMaterials.Get(material);
		m.rho0 *= densityScale;
		SuperParticles p = SuperScenes.TaylorCylinder(L0, D0, spacing, velocity, T0, material);
		if (densityScale != 1f)
		{
			for (int i = 0; i < p.count; i++)
			{
				p.mass[i] *= densityScale;
			}
		}
		SuperKernel k = new SuperKernel(p, spacing + spacing, m);
		k.anvil = anvil;
		k.sabotage = sabotage;
		return Drive(k, p, spacing, durationS);
	}

	public static float ElasticWaveArrival(float length, float side, float spacing, float impactVelocity, float T0, ContinuumMaterialId material, out float expectedS, out int steps, out float twoStationSpeed)
	{
		ContinuumProperties m = ContinuumMaterials.Get(material);
		SuperParticles p = SuperScenes.Bar(length, side, spacing, 0f, T0, material);
		int n = Math.Max(2, Mathf.RoundToInt(length / spacing));
		for (int i = 0; i < p.count; i++)
		{
			p.v[i] = new Vector3(0f, 0f, -impactVelocity);
		}
		SuperKernel k = new SuperKernel(p, spacing + spacing, m);
		k.anvil = true;
		k.sabotage = SuperSabotage.ElasticOnly;
		float s2 = (n - 0.5f) * spacing;
		float s1 = s2 * 0.25f;
		float threshold = impactVelocity * 0.05f;
		expectedS = s2 / m.LongitudinalWaveSpeed;
		float t1 = -1f;
		float t2 = -1f;
		steps = 0;
		float dt = k.TimeStep();
		while (k.time < 2f * expectedS)
		{
			if (steps >= MaxSteps)
			{
				break;
			}
			if (!k.Step(dt))
			{
				break;
			}
			steps++;
			for (int i = 0; i < p.count; i++)
			{
				if (threshold >= Mathf.Abs(impactVelocity + p.v[i].z))
				{
					continue;
				}
				float z = p.x[i].z;
				if (t1 < 0f && z >= s1 - spacing * 0.5f && z <= s1 + spacing * 0.5f)
				{
					t1 = k.time;
				}
				if (t2 < 0f && z >= s2 - spacing * 0.5f)
				{
					t2 = k.time;
				}
			}
			if (t2 > 0f)
			{
				break;
			}
			dt = k.TimeStep();
		}
		if (t1 > 0f && t2 > t1)
		{
			twoStationSpeed = (s2 - s1) / (t2 - t1);
		}
		else
		{
			twoStationSpeed = 0f;
		}
		k.Dispose();
		p.Dispose();
		return t2;
	}

	public static bool HydrostaticPatch(float J, float spacing, out double got, out double want)
	{
		ContinuumProperties m = ContinuumMaterials.Get(ContinuumMaterialId.OFHC_Copper);
		SuperParticles p = new SuperParticles(4096);
		float side = spacing * 16f;
		float cellVol = spacing * spacing * spacing / J;
		float scale = Mathf.Pow(J, 1f / 3f);
		SuperMat3 F = SuperMat3.Identity;
		F.m00 = scale;
		F.m11 = scale;
		F.m22 = scale;
		float p0 = ContinuumMaterials.PressureFromMu(in m, 1f / J - 1f, 0f);
		SuperMat3 sigma = SuperMat3.Zero;
		sigma.m00 = -p0;
		sigma.m11 = -p0;
		sigma.m22 = -p0;
		int n = 0;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					p.x[n] = new Vector3((k + 0.5f) * spacing, (j + 0.5f) * spacing, (i + 0.5f) * spacing);
					p.v[n] = Vector3.zero;
					p.C[n] = SuperMat3.Zero;
					p.F[n] = F;
					p.s[n] = SuperMat3.Zero;
					p.sigma[n] = sigma;
					p.V0[n] = cellVol;
					p.mass[n] = m.rho0 * cellVol;
					p.matId[n] = (byte)ContinuumMaterialId.OFHC_Copper;
					n++;
				}
			}
		}
		SuperKernel kk = new SuperKernel(p, spacing + spacing, m);
		kk.anvil = false;
		float dt = 1e-09f;
		bool ok = kk.ProbeParticleToGrid(dt);
		RadarKahan sum = default(RadarKahan);
		float half = side * 0.5f;
		for (int iz = 0; iz < kk.grid.nz; iz++)
		{
			for (int iy = 0; iy < kk.grid.ny; iy++)
			{
				for (int ix = 0; ix < kk.grid.nx; ix++)
				{
					if (half > (float)iz * kk.grid.h + kk.grid.origin.z)
					{
						continue;
					}
					sum.Add(kk.grid.mom[kk.grid.Index(ix, iy, iz)].z);
				}
			}
		}
		got = sum.Value / dt;
		want = side * side * p0;
		kk.Dispose();
		p.Dispose();
		return ok;
	}

	private static SuperResult Drive(SuperKernel k, SuperParticles p, float spacing, float durationS)
	{
		SuperResult result = default(SuperResult);
		result.ok = true;
		result.particles = p.count;
		Vector3 momentum0 = Momentum(p);
		result.momentumInitial = momentum0;
		result.keInitialJ = KineticEnergy(p);
		result.momentumScale = ((momentum0.magnitude > 1e-09f) ? ((double)momentum0.magnitude) : MomentumScale(p));
		float minZ = float.MaxValue;
		float maxZ = -float.MaxValue;
		float r2 = 0f;
		for (int i = 0; i < p.count; i++)
		{
			Vector3 pos = p.x[i];
			if (minZ > pos.z)
			{
				minZ = pos.z;
			}
			if (pos.z > maxZ)
			{
				maxZ = pos.z;
			}
			float rr = pos.y * pos.y + pos.x * pos.x;
			if (rr > r2)
			{
				r2 = rr;
			}
		}
		float rMax = Mathf.Sqrt(r2);
		float diameterKeMin = rMax + rMax + spacing;
		float lengthKeMin = maxZ - minZ + spacing;
		Stopwatch sw = Stopwatch.StartNew();
		double keMin = double.MaxValue;
		float dt = k.TimeStep();
		while (k.time < durationS)
		{
			if (k.stepIndex >= MaxSteps)
			{
				break;
			}
			if (!k.Step(dt))
			{
				result.ok = false;
				result.failure = k.failure;
				break;
			}
			double ke = KineticEnergy(p);
			if (ke < keMin)
			{
				keMin = ke;
				lengthKeMin = Measure(p, spacing, out diameterKeMin);
				result.stepAtKeMin = k.stepIndex;
				result.timeAtKeMinS = k.time;
			}
			Vector3 momentum = Momentum(p);
			Vector3 drift = momentum - momentum0;
			double driftMag = drift.magnitude;
			if (driftMag > result.momentumDriftMax)
			{
				result.momentumDriftMax = driftMag;
			}
			dt = k.TimeStep();
		}
		sw.Stop();
		if (k.stepIndex >= MaxSteps)
		{
			result.ok = false;
			result.failure = string.Format("предохранитель {0} шагов", MaxSteps);
		}
		result.steps = k.stepIndex;
		result.simulatedTimeS = k.time;
		if (keMin == double.MaxValue)
		{
			keMin = 0.0;
		}
		result.keMinJ = keMin;
		result.keEndJ = KineticEnergy(p);
		result.keMinAtEnd = result.stepAtKeMin >= k.stepIndex - 1 && k.stepIndex > 0;
		result.LfKeMinM = lengthKeMin;
		result.DfKeMinM = diameterKeMin;
		result.LfEndM = Measure(p, spacing, out _);
		result.momentumEnd = Momentum(p);
		result.wallClockMs = sw.Elapsed.TotalMilliseconds;
		if (result.wallClockMs > 0.0)
		{
			result.particleStepsPerSecond = (double)result.steps * p.count / (result.wallClockMs / 1000.0);
		}
		for (int i = 0; i < p.count; i++)
		{
			if (p.epsP[i] > result.maxEpsP)
			{
				result.maxEpsP = p.epsP[i];
			}
			if (p.T[i] > result.maxTempK)
			{
				result.maxTempK = p.T[i];
			}
		}
		k.Dispose();
		p.Dispose();
		return result;
	}

	private static float Measure(SuperParticles p, float spacing, out float diameter)
	{
		float minZ = float.MaxValue;
		float maxZ = -float.MaxValue;
		float r2 = 0f;
		for (int i = 0; i < p.count; i++)
		{
			Vector3 pos = p.x[i];
			if (minZ > pos.z)
			{
				minZ = pos.z;
			}
			if (pos.z > maxZ)
			{
				maxZ = pos.z;
			}
			float rr = pos.y * pos.y + pos.x * pos.x;
			if (rr > r2)
			{
				r2 = rr;
			}
		}
		float rMax = Mathf.Sqrt(r2);
		diameter = rMax + rMax + spacing;
		return maxZ - minZ + spacing;
	}

	private static double KineticEnergy(SuperParticles p)
	{
		RadarKahan sum = new RadarKahan();
		for (int i = 0; i < p.count; i++)
		{
			Vector3 vel = p.v[i];
			sum.Add(0.5 * p.mass[i] * ((double)vel.x * vel.x + (double)vel.y * vel.y + (double)vel.z * vel.z));
		}
		return sum.Value;
	}

	private static Vector3 Momentum(SuperParticles p)
	{
		RadarKahan mx = new RadarKahan();
		RadarKahan my = new RadarKahan();
		RadarKahan mz = new RadarKahan();
		for (int i = 0; i < p.count; i++)
		{
			float m = p.mass[i];
			Vector3 vel = p.v[i];
			mx.Add((double)vel.x * m);
			my.Add((double)vel.y * m);
			mz.Add((double)vel.z * m);
		}
		return new Vector3((float)mx.Value, (float)my.Value, (float)mz.Value);
	}

	private static double MomentumScale(SuperParticles p)
	{
		RadarKahan sum = new RadarKahan();
		for (int i = 0; i < p.count; i++)
		{
			Vector3 vel = p.v[i];
			float speed = (float)Math.Sqrt(vel.x * vel.x + vel.y * vel.y + vel.z * vel.z);
			sum.Add(speed * (double)p.mass[i]);
		}
		if (sum.Value > 1e-12)
		{
			return sum.Value;
		}
		return 1.0;
	}
}
