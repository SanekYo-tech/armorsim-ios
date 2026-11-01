using System;
using UnityEngine;

public sealed class SuperKernel : IDisposable
{
	private const float NodeMassFloor = 1E-12f;

	public readonly SuperParticles particles;

	public readonly SuperGrid grid;

	public ContinuumProperties material;

	public bool anvil = true;

	public float anvilFriction = 0.12f;

	public float cfl = 0.4f;

	public SuperSabotage sabotage;

	public int stepIndex;

	public float time;

	public float lastDt;

	public string failure;

	public SuperKernel(SuperParticles p, float cellSize, ContinuumProperties m)
	{
		particles = p;
		grid = new SuperGrid(cellSize);
		material = m;
	}

	public float TimeStep()
	{
		float maxSpeed = 0f;
		float maxWave = 0f;
		for (int i = 0; i < particles.count; i++)
		{
			Vector3 v = particles.v[i];
			float speed = (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
			if (speed > maxSpeed)
			{
				maxSpeed = speed;
			}
			float det = particles.F[i].Det;
			if (det < 1E-06f)
			{
				det = 1E-06f;
			}
			float c = MathF.Sqrt((material.K + material.G * 4f / 3f) / (material.rho0 / det));
			if (c > maxWave)
			{
				maxWave = c;
			}
		}
		return grid.h * cfl / (maxWave + maxSpeed);
	}

	public bool Step(float dt)
	{
		lastDt = dt;
		if (!grid.Reset(particles))
		{
			failure = "положение частицы стало NaN";
			return false;
		}
		ParticleToGrid(dt);
		GridUpdate();
		GridToParticle(dt);
		if (!ParticleUpdate(dt))
		{
			return false;
		}
		stepIndex++;
		time += dt;
		return true;
	}

	public bool ProbeParticleToGrid(float dt)
	{
		if (!grid.Reset(particles))
		{
			failure = "положение частицы стало NaN";
			return false;
		}
		ParticleToGrid(dt);
		return true;
	}

	private void ParticleToGrid(float dt)
	{
		float k = 4f / (grid.h * grid.h);
		for (int i = 0; i < particles.count; i++)
		{
			Vector3 xp = particles.x[i];
			grid.Weights(xp, out int bx, out int by, out int bz, out Vector3 w0, out Vector3 w1, out Vector3 w2);
			float m = particles.mass[i];
			float c = particles.F[i].Det * particles.V0[i] * (k * dt);
			SuperMat3 d = particles.C[i] * m - particles.sigma[i] * c;
			Vector3 v = particles.v[i];
			for (int a = 0; a < 3; a++)
			{
				Vector3 wa = ((a == 0) ? w0 : ((a == 1) ? w1 : w2));
				for (int b = 0; b < 3; b++)
				{
					Vector3 wb = ((b == 0) ? w0 : ((b == 1) ? w1 : w2));
					for (int c2 = 0; c2 < 3; c2++)
					{
						Vector3 wc = ((c2 == 0) ? w0 : ((c2 == 1) ? w1 : w2));
						float w = wc.x * wb.y * wa.z;
						int idx = grid.Index(bx + c2, by + b, bz + a);
						Vector3 node = grid.NodePos(bx + c2, by + b, bz + a);
						grid.mass[idx] += w * m;
						grid.mom[idx] += (d.MulVec(node - xp) + v * m) * w;
					}
				}
			}
		}
	}

	private void GridUpdate()
	{
		int nodes = grid.NodeCount;
		for (int i = 0; i < nodes; i++)
		{
			float m = grid.mass[i];
			if (m <= NodeMassFloor)
			{
				grid.mom[i] = Vector3.zero;
			}
			else
			{
				Vector3 v = grid.mom[i] / m;
				if (anvil)
				{
					float z = i / (grid.ny * grid.nx) * grid.h + grid.origin.z;
					if (z <= 0f)
					{
						v = ApplyAnvil(v, anvilFriction);
					}
				}
				grid.mom[i] = v;
			}
		}
	}

	private static Vector3 ApplyAnvil(Vector3 v, float mu)
	{
		if (v.z >= 0f)
		{
			return v;
		}
		Vector3 h = new Vector3(v.x, v.y, 0f);
		float speed = h.magnitude;
		if (speed <= -v.z * mu)
		{
			return Vector3.zero;
		}
		float k = 1f - (-v.z * mu) / speed;
		return new Vector3(k * v.x, k * v.y, 0f);
	}

	private void GridToParticle(float dt)
	{
		float k = 4f / (grid.h * grid.h);
		for (int i = 0; i < particles.count; i++)
		{
			Vector3 xp = particles.x[i];
			grid.Weights(xp, out int bx, out int by, out int bz, out Vector3 w0, out Vector3 w1, out Vector3 w2);
			Vector3 v = Vector3.zero;
			SuperMat3 C = default(SuperMat3);
			for (int a = 0; a < 3; a++)
			{
				Vector3 wa = ((a == 0) ? w0 : ((a == 1) ? w1 : w2));
				for (int b = 0; b < 3; b++)
				{
					Vector3 wb = ((b == 0) ? w0 : ((b == 1) ? w1 : w2));
					for (int c = 0; c < 3; c++)
					{
						Vector3 wc = ((c == 0) ? w0 : ((c == 1) ? w1 : w2));
						float w = wc.x * wb.y * wa.z;
						int idx = grid.Index(bx + c, by + b, bz + a);
						Vector3 vel = grid.mom[idx];
						Vector3 node = grid.NodePos(bx + c, by + b, bz + a);
						Vector3 r = node - xp;
						Vector3 vw = vel * w;
						v += vw;
						C += SuperMat3.Outer(vw, r);
					}
				}
			}
			if ((sabotage & SuperSabotage.SkipGridVelocity) == 0)
			{
				particles.C[i] = C * k;
				particles.v[i] = v;
				particles.x[i] = v * dt + xp;
			}
			else
			{
				particles.C[i] = default(SuperMat3);
				particles.x[i] = particles.v[i] * dt + xp;
			}
		}
	}

	private bool ParticleUpdate(float dt)
	{
		bool elasticOnly = (sabotage & SuperSabotage.ElasticOnly) != 0;
		for (int i = 0; i < particles.count; i++)
		{
			SuperMat3 L = particles.C[i];
			SuperMat3 newF = (SuperMat3.Identity + L * dt) * particles.F[i];
			float damage = particles.damage.IsCreated ? particles.damage[i] : 0f;
			bool dead = SuperDamage.Enabled && damage >= 1f;
			float J = newF.Det;
			if (!float.IsFinite(J))
			{
				failure = string.Format("det F стал NaN у частицы {0} на шаге {1}", i, stepIndex);
				return false;
			}
			if (J <= 1E-04f && !SuperDamage.Enabled)
			{
				failure = string.Format("det F = {0:E3} у частицы {1} на шаге {2} — вещество вывернулось", J, i, stepIndex);
				return false;
			}
			if (J < SuperDamage.DebrisJMin || J > SuperDamage.DebrisJMax)
			{
				if (SuperDamage.Enabled && !dead)
				{
					damage = Mathf.Max(damage, 1f);
				}
				J = Mathf.Clamp(J, SuperDamage.DebrisJMin, SuperDamage.DebrisJMax);
				newF = SuperMat3.Identity * Mathf.Pow(J, 1f / 3f);
			}
			else if (dead && !SuperDamage.SabotageDebrisTension && SuperDamage.DebrisIsotropicF)
			{
				newF = SuperMat3.Identity * Mathf.Pow(Mathf.Max(J, SuperDamage.DebrisJMin), 1f / 3f);
			}
			else if (SuperDamage.WholeIsotropicF)
			{
				float r = Mathf.Pow(Mathf.Max(J, SuperDamage.DebrisJMin), 1f / 3f);
				if (SuperMat3.DoubleDot(newF, newF) > SuperDamage.WholeShapeResetRatio * 3f * r * r)
				{
					newF = SuperMat3.Identity * r;
				}
			}
			particles.F[i] = newF;
			SuperMat3 s = particles.s[i];
			float epsP = particles.epsP[i];
			float T = particles.T[i];
			float e = particles.e[i];
			float epsRate = particles.epsRate[i];
			float failureStrainScale = (particles.xi.IsCreated ? particles.xi[i] : 1f);
			particles.sigma[i] = SuperConstitutive.Update(in material, L, J, dt, grid.h, ref s, ref epsP, ref T, ref e, ref epsRate, out var pressure, out var qVisc, elasticOnly, ref damage, failureStrainScale);
			particles.s[i] = s;
			particles.epsP[i] = epsP;
			particles.T[i] = T;
			particles.e[i] = e;
			particles.epsRate[i] = epsRate;
			if (particles.damage.IsCreated)
			{
				particles.damage[i] = damage;
			}
		}
		return true;
	}

	public void Dispose()
	{
		grid.Dispose();
	}
}
