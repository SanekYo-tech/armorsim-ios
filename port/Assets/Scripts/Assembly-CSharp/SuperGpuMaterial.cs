public struct SuperGpuMaterial
{
	public float rho0;

	public float G;

	public float K;

	public float K1;

	public float K2;

	public float K3;

	public float Gamma0;

	public float a_eos;

	public float JC_A;

	public float JC_B;

	public float JC_n;

	public float JC_C;

	public float JC_m;

	public float epsDot0;

	public float epsSat;

	public float T_room;

	public float T_melt;

	public float C_p;

	public float TaylorQuinney;

	public float viscQuad;

	public float viscLin;

	public float JC_D1;

	public float JC_D2;

	public float JC_D3;

	public float JC_D4;

	public float JC_D5;

	public float hasDamage;

	public float spallPa;

	public float sigma1MaxPa;

	public float cavitationPa;

	public float hasASB;

	public float epsASB;

	public float dEpsFade;

	public const int Stride = 132;

	public static SuperGpuMaterial From(in ContinuumProperties p)
	{
		return new SuperGpuMaterial
		{
			rho0 = p.rho0,
			G = p.G,
			K = p.K,
			K1 = p.K1,
			K2 = p.K2,
			K3 = p.K3,
			Gamma0 = p.Gamma0,
			a_eos = p.a_eos,
			JC_A = p.JC_A,
			JC_B = p.JC_B,
			JC_n = p.JC_n,
			JC_C = p.JC_C,
			JC_m = p.JC_m,
			epsDot0 = p.epsDot0,
			epsSat = p.epsSat,
			T_room = p.T_room,
			T_melt = p.T_melt,
			C_p = p.C_p,
			TaylorQuinney = 0.9f,
			viscQuad = SuperConstitutive.ViscosityQuadratic,
			viscLin = SuperConstitutive.ViscosityLinear,
			JC_D1 = p.JC_D1,
			JC_D2 = p.JC_D2,
			JC_D3 = p.JC_D3,
			JC_D4 = p.JC_D4,
			JC_D5 = p.JC_D5,
			hasDamage = ((p.hasDamage != 0) ? 1f : 0f),
			spallPa = SuperDamage.EffectiveSpallPa(p),
			sigma1MaxPa = p.sigma1MaxPa,
			cavitationPa = SuperDamage.EffectiveCavitationPa(p),
			hasASB = ((p.hasASB != 0) ? 1f : 0f),
			epsASB = p.epsASB,
			dEpsFade = p.dEpsFade
		};
	}
}
