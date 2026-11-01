using UnityEngine;

public struct Flat2DMaterialBlock
{
	public float rho0;

	public float K;

	public float G;

	public float spall;

	public float jcA;

	public float jcB;

	public float jcN;

	public float jcC;

	public float jcM;

	public float epsDot0;

	public float tRoom;

	public float tMelt;

	public float cP;

	public float c0;

	public float s;

	public float gamma0;

	public float d1;

	public float d2;

	public float d3;

	public float d4;

	public float d5;

	public float aEos;

	public float pad1;

	public float pad2;

	public const int SizeBytes = 96;

	public float LongitudinalWaveSpeed => KernelSoundSpeed(rho0, 0f);

	public float ElasticWaveSpeedK => Mathf.Sqrt((K + G * 4f / 3f) / rho0);

	public float KernelPressure(float rho, float e)
	{
		float mu = rho / rho0 - 1f;
		float thermal = (gamma0 + aEos * mu) * rho0 * e;
		if (mu < 0f)
		{
			return rho0 * c0 * c0 * mu + thermal;
		}
		float denom = 1f - (s - 1f) * mu;
		if (denom < 0.1f)
		{
			denom = 0.1f;
		}
		float compression = (1f - gamma0 * 0.5f) * mu - aEos * 0.5f * mu * mu + 1f;
		return compression * (rho0 * c0 * c0 * mu) / (denom * denom) + thermal;
	}

	public float KernelSoundSpeed(float rho, float e)
	{
		float drho = rho0 * 0.001f;
		float p0 = KernelPressure(rho, e);
		float p1 = KernelPressure(rho + drho, e);
		float bulk = (p1 - p0) / drho;
		float shear = G * 4f / (rho * 3f);
		if (bulk < 0f)
		{
			bulk = 0f;
		}
		float c2 = bulk + shear;
		if (c2 < 1f)
		{
			c2 = 1f;
		}
		return Mathf.Sqrt(c2);
	}
}
