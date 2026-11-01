public static class Flat2DMaterials
{
	public static Flat2DMaterialBlock From(ContinuumMaterialId id)
	{
		ContinuumProperties p = ContinuumMaterials.Get(id);
		return new Flat2DMaterialBlock
		{
			rho0 = p.rho0,
			K = p.K,
			G = p.G,
			spall = UnityEngine.Mathf.Abs(p.spallPa),
			jcA = p.JC_A,
			jcB = p.JC_B,
			jcN = p.JC_n,
			jcC = p.JC_C,
			jcM = p.JC_m,
			epsDot0 = p.epsDot0,
			tRoom = p.T_room,
			tMelt = p.T_melt,
			cP = p.C_p,
			c0 = p.C0,
			s = p.S,
			gamma0 = p.Gamma0,
			d1 = p.JC_D1,
			d2 = p.JC_D2,
			d3 = p.JC_D3,
			d4 = p.JC_D4,
			d5 = p.JC_D5,
			aEos = p.a_eos
		};
	}

	public static Flat2DMaterialBlock[] Build(params ContinuumMaterialId[] ids)
	{
		Flat2DMaterialBlock[] result = new Flat2DMaterialBlock[ids.Length];
		for (int i = 0; i < ids.Length; i++)
		{
			result[i] = From(ids[i]);
		}
		return result;
	}
}
