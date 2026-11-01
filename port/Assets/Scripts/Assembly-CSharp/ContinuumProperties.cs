public struct ContinuumProperties
{
	public float rho0;

	public float G;

	public float K;

	public float C0;

	public float S;

	public float Gamma0;

	public float a_eos;

	public float K1;

	public float K2;

	public float K3;

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

	public float epsASB;

	public float dEpsFade;

	public byte hasASB;

	public float JC_D1;

	public float JC_D2;

	public float JC_D3;

	public float JC_D4;

	public float JC_D5;

	public byte hasDamage;

	public float spallPa;

	public float sigma1MaxPa;

	public float cavitationPa;

	public ProvenanceTag provenance;

	public float LongitudinalWaveSpeed => UnityEngine.Mathf.Sqrt((K + 4f * G / 3f) / rho0);
}
