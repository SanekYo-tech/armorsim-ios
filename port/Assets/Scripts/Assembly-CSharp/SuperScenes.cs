using System;
using UnityEngine;

public static class SuperScenes
{
	public const int ParticlesPerCellAxis = 2;

	public static SuperParticles TaylorCylinder(float L0, float D0, float spacing, float velocity, float T0, ContinuumMaterialId material)
	{
		ContinuumProperties props = ContinuumMaterials.Get(material);
		float radius = D0 * 0.5f;
		float particleVolume = spacing * spacing * spacing;
		int nz = Math.Max(Mathf.CeilToInt(L0 / spacing), 1);
		int nr = Math.Max((int)Math.Ceiling(D0 / spacing), 1);
		int count = 0;
		for (int k = 0; k < nz; k++)
		{
			for (int iy = 0; iy < nr; iy++)
			{
				for (int ix = 0; ix < nr; ix++)
				{
					if (InsideDisc(ix, iy, nr, spacing, radius))
					{
						count++;
					}
				}
			}
		}
		SuperParticles p = new SuperParticles(count);
		int idx = 0;
		for (int k = 0; k < nz; k++)
		{
			for (int iy = 0; iy < nr; iy++)
			{
				for (int ix = 0; ix < nr; ix++)
				{
					if (InsideDisc(ix, iy, nr, spacing, radius))
					{
						p.x[idx] = new Vector3((ix - (nr - 1) * 0.5f) * spacing, (iy - (nr - 1) * 0.5f) * spacing, (k + 0.5f) * spacing);
						p.v[idx] = new Vector3(0f, 0f, -velocity);
						p.C[idx] = SuperMat3.Zero;
						p.F[idx] = SuperMat3.Identity;
						p.s[idx] = SuperMat3.Zero;
						p.epsP[idx] = 0f;
						p.T[idx] = T0;
						p.e[idx] = 0f;
						p.V0[idx] = particleVolume;
						p.mass[idx] = props.rho0 * particleVolume;
						p.matId[idx] = (byte)material;
						idx++;
					}
				}
			}
		}
		if (count > 0 && count * particleVolume > 0f)
		{
			float scale = radius * Mathf.PI * radius * (nz * spacing) / (count * particleVolume);
			for (int i = 0; i < count; i++)
			{
				p.V0[i] *= scale;
				p.mass[i] *= scale;
			}
		}
		return p;
	}

	public static SuperParticles Bar(float length, float side, float spacing, float strikerVelocity, float T0, ContinuumMaterialId material)
	{
		ContinuumProperties props = ContinuumMaterials.Get(material);
		float particleVolume = spacing * spacing * spacing;
		int nz = Math.Max(Mathf.CeilToInt(length / spacing), 2);
		int n = Math.Max(Mathf.CeilToInt(side / spacing), 1);
		SuperParticles p = new SuperParticles(n * n * nz);
		int idx = 0;
		for (int k = 0; k < nz; k++)
		{
			Vector3 velocity;
			if (k == 0)
			{
				velocity = new Vector3(0f, 0f, strikerVelocity);
			}
			else
			{
				velocity = Vector3.zero;
			}
			for (int j = 0; j < n; j++)
			{
				for (int i = 0; i < n; i++)
				{
					p.x[idx] = new Vector3((i - (n - 1) * 0.5f) * spacing, (j - (n - 1) * 0.5f) * spacing, (k + 0.5f) * spacing);
					p.v[idx] = velocity;
					p.C[idx] = SuperMat3.Zero;
					p.F[idx] = SuperMat3.Identity;
					p.s[idx] = SuperMat3.Zero;
					p.T[idx] = T0;
					p.V0[idx] = particleVolume;
					p.mass[idx] = props.rho0 * particleVolume;
					p.matId[idx] = (byte)material;
					idx++;
				}
			}
		}
		return p;
	}

	private static bool InsideDisc(int ix, int iy, int nr, float spacing, float radius)
	{
		float x = (ix - (nr - 1) * 0.5f) * spacing;
		float y = (iy - (nr - 1) * 0.5f) * spacing;
		return x * x + y * y <= radius * radius;
	}
}
