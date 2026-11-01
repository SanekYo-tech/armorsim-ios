using System;
using UnityEngine;

public static class SuperTimingScene
{
	public const float BlockWidth = 0.2f;

	public const float BlockThick = 0.02f;

	public const float RodSide = 0.01f;

	public const float RodLength = 0.1f;

	public static double VolumeM3 => (double)BlockWidth * BlockWidth * BlockThick + (double)RodSide * RodSide * RodLength;

	public static SuperParticles Build(float spacing, float velocity, float T0, ContinuumMaterialId material)
	{
		ContinuumProperties p = ContinuumMaterials.Get(material);
		int nx = Math.Max(1, (int)Math.Round(BlockWidth / spacing));
		int nt = Math.Max(1, (int)Math.Round(BlockThick / spacing));
		int nr = Math.Max(1, (int)Math.Round(RodSide / spacing));
		int nl = Math.Max(1, (int)Math.Round(RodLength / spacing));
		SuperParticles pa = new SuperParticles(nl * nr * nr + nt * nx * nx);
		float cell = spacing * spacing * spacing;
		int k = 0;
		for (int iz = 0; iz < nt; iz++)
		{
			for (int iy = 0; iy < nx; iy++)
			{
				for (int ix = 0; ix < nx; ix++)
				{
					pa.x[k] = new Vector3((ix - (nx - 1) * 0.5f) * spacing, (iy - (nx - 1) * 0.5f) * spacing, (iz + 0.5f) * spacing);
					pa.v[k] = Vector3.zero;
					Init(pa, k, p, cell, T0, material);
					k++;
				}
			}
		}
		for (int iz2 = 0; iz2 < nl; iz2++)
		{
			for (int iy2 = 0; iy2 < nr; iy2++)
			{
				for (int ix2 = 0; ix2 < nr; ix2++)
				{
					pa.x[k] = new Vector3((ix2 - (nr - 1) * 0.5f) * spacing, (iy2 - (nr - 1) * 0.5f) * spacing, (iz2 + 0.5f) * spacing + BlockThick);
					pa.v[k] = new Vector3(0f, 0f, -velocity);
					Init(pa, k, p, cell, T0, material);
					k++;
				}
			}
		}
		return pa;
	}

	private static void Init(SuperParticles pa, int k, in ContinuumProperties p, float cell, float T0, ContinuumMaterialId material)
	{
		pa.C[k] = SuperMat3.Zero;
		pa.F[k] = SuperMat3.Identity;
		pa.s[k] = SuperMat3.Zero;
		pa.sigma[k] = SuperMat3.Zero;
		pa.epsP[k] = 0f;
		pa.epsRate[k] = 0f;
		pa.T[k] = T0;
		pa.e[k] = 0f;
		pa.V0[k] = cell;
		pa.mass[k] = cell * p.rho0;
		pa.matId[k] = (byte)material;
	}
}
