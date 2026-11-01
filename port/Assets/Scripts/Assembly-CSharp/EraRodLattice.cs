using System.Collections.Generic;
using UnityEngine;

public static class EraRodLattice
{
	private const int SweepWindows = 256;

	public static RodPostEraState Run(ReactiveArmorRodSpec era, float obliquityRad, Projectile.CoreType coreType, float rodLengthM, float rodDiameterM, float rodDensity, float rodVelocityMS)
	{
		RodEraOutcome o = ReactiveArmor.ApplyToRod(era, obliquityRad, rodLengthM, rodDiameterM, rodDensity, rodVelocityMS);
		if (!era.enabled || o.forceN <= 0f)
		{
			return ReactiveArmor.NeutralPostEraState(rodLengthM);
		}
		ReactiveArmor.RodLoadSchedule s = ReactiveArmor.BuildRodLoadSchedule(era, obliquityRad, rodLengthM, rodDiameterM, rodDensity, rodVelocityMS);
		RodDynamicsSolver solver = new RodDynamicsSolver(coreType, rodDiameterM * 1000f, rodLengthM * 1000f);
		solver.ConfigureFreeFlight(rodVelocityMS, rodLengthM / rodVelocityMS);
		float dt = s.engagementEndS / SweepWindows;
		for (int k = 0; k < SweepWindows; k++)
		{
			float t0 = k * dt;
			float t1 = t0 + dt;
			float front0 = ReactiveArmor.FrontStationAt(s, t0);
			float front1 = ReactiveArmor.FrontStationAt(s, t1);
			if (front0 < rodLengthM)
			{
				solver.AddExternalLoad(t0, t1, Vector3.right * s.forceN, Mathf.Clamp(front0, 0f, rodLengthM), Mathf.Clamp(front1, 0f, rodLengthM));
			}
			float rear0 = ReactiveArmor.RearStationAt(s, t0);
			float rear1 = ReactiveArmor.RearStationAt(s, t1);
			if (rear1 > 0f && rear0 < rodLengthM)
			{
				solver.AddExternalLoad(t0, t1, Vector3.right * (0f - s.forceN), Mathf.Clamp(rear0, 0f, rodLengthM), Mathf.Clamp(rear1, 0f, rodLengthM));
			}
		}
		int guard = 0;
		while (solver.doneSteps < solver.totalSteps && !solver.exploded && guard < 1000)
		{
			int done = solver.doneSteps;
			int n = solver.totalSteps - done;
			if (n > 4000)
			{
				n = 4000;
			}
			solver.RunBatch(n, 400);
			if (solver.doneSteps == done)
			{
				break;
			}
			guard++;
		}
		Vector3[] centers = new Vector3[solver.RingCount];
		bool[] alive = new bool[solver.RingCount];
		bool ok = solver.SampleFrame(solver.EventDuration, centers, alive);
		RodPostEraState r = default(RodPostEraState);
		r.fragmentCount = solver.frames.Count + 1;
		r.fractured = solver.frames.Count > 0 || solver.fiberBreaks > 0;
		r.timeToFractureS = ((solver.frames.Count > 0) ? solver.frames[0].t : float.NaN);
		r.fromLattice = true;
		r.craterClearanceM = CraterModel.CraterClearanceM(RoundCatalog.CorePenetrator(coreType), TateModel.RHA(), rodDiameterM, rodVelocityMS);
		float omegaY = 0f;
		if (ok)
		{
			r.effectiveLengthM = EffectiveLengthM(centers, alive, r.craterClearanceM, rodLengthM, out r.axisOutOfPlaneM);
			r.maxLocalAngleRad = MaxBendRad(centers, alive);
			r.peakDevPlasticStrain = solver.MaxDeviatoricPlasticStrain();
			r.fractureStrain = solver.FractureStrain;
			omegaY = solver.RigidOmegaY();
			if (!solver.exploded)
			{
				r.rigidYawRad = Mathf.Abs(omegaY) * (era.standoffBehindM / rodVelocityMS);
			}
			else
			{
				r.rigidYawRad = o.yawAngleRad;
				r.fromLattice = false;
			}
		}
		else
		{
			r.effectiveLengthM = rodLengthM;
			r.maxLocalAngleRad = 0f;
			r.peakDevPlasticStrain = solver.MaxDeviatoricPlasticStrain();
			r.fractureStrain = solver.FractureStrain;
			r.rigidYawRad = o.yawAngleRad;
			r.fromLattice = false;
		}
		Debug.Log(string.Format("    [решётка ДЗ] ω={0:F1} рад/с (аналит. {1:F1}), ", omegaY, o.omegaRadS)
			+ string.Format("ψ={0:F2}°, L_эфф={1:F0} мм ", r.rigidYawRad * Mathf.Rad2Deg, r.effectiveLengthM * 1000f)
			+ string.Format("из {0:F0}, обломков {1}, изгиб {2:F1}°", rodLengthM * 1000f, r.fragmentCount, r.maxLocalAngleRad * Mathf.Rad2Deg)
			+ (solver.exploded ? " ⚠РАЗНОС — взята аналитика" : ""));
		if (r.fragmentCount > 1)
		{
			Debug.Log("    [решётка ДЗ] ⚠ обломки НЕ суммируются: пробивает ТОЛЬКО ГОЛОВНОЙ (заявленное допущение)");
		}
		return r;
	}

	private static float MaxBendRad(Vector3[] c, bool[] alive)
	{
		float maxBend = 0f;
		int k1 = -1;
		int k2 = -1;
		for (int i = 0; i < c.Length; i++)
		{
			if (alive[i])
			{
				if (k2 >= 0 && k1 >= 0)
				{
					Vector3 a = c[k1] - c[k2];
					Vector3 b = c[i] - c[k1];
					if (a.sqrMagnitude > 1e-12f && b.sqrMagnitude > 1e-12f)
					{
						maxBend = Mathf.Max(maxBend, Vector3.Angle(a, b) * Mathf.Deg2Rad);
					}
				}
				k2 = k1;
				k1 = i;
			}
		}
		return maxBend;
	}

	public static float EffectiveLengthM(Vector3[] c, bool[] alive, float clearanceM, float rodLengthM, out float outOfPlaneM)
	{
		outOfPlaneM = 0f;
		List<int> idx = new List<int>(c.Length);
		for (int i = 0; i < c.Length; i++)
		{
			if (alive[i])
			{
				idx.Add(i);
			}
		}
		int n = idx.Count;
		if (n < 3)
		{
			return 0f;
		}
		float[] arcFromNose = new float[n];
		for (int j = 1; j < n; j++)
		{
			arcFromNose[j] = arcFromNose[j - 1] + (c[idx[j]] - c[idx[j - 1]]).magnitude;
		}
		Vector3 axis = c[idx[n - 1]] - c[idx[0]];
		float axisLen = axis.magnitude;
		if (axisLen < 1e-9f)
		{
			return rodLengthM > arcFromNose[n - 1] ? arcFromNose[n - 1] : rodLengthM;
		}
		Vector3 u = axis / axisLen;
		Vector3 dMax = Vector3.zero;
		float w = 0f;
		for (int i = 1; i < n; i++)
		{
			Vector3 d = c[idx[i]] - c[idx[0]];
			Vector3 r = d - u * Vector3.Dot(u, d);
			float len = r.magnitude;
			if (len > w)
			{
				w = len;
				dMax = r;
			}
		}
		if (w < 1e-12f)
		{
			return rodLengthM > arcFromNose[n - 1] ? arcFromNose[n - 1] : rodLengthM;
		}
		Vector3 dn = dMax / w;
		float[] lateral = new float[n];
		for (int i = 0; i < n; i++)
		{
			Vector3 d = c[idx[i]] - c[idx[0]];
			Vector3 r = d - u * Vector3.Dot(u, d);
			lateral[i] = Vector3.Dot(r, dn);
			Vector3 q = r - dn * Vector3.Dot(r, dn);
			outOfPlaneM = Mathf.Max(outOfPlaneM, q.magnitude);
		}
		float eff = CraterModel.EffectiveLengthByClearance(arcFromNose, lateral, clearanceM);
		if (rodLengthM > eff)
		{
			return eff;
		}
		return rodLengthM;
	}
}
