using UnityEngine;

public struct SuperJetSpec
{
	public float lengthM;

	public float diameterM;

	public float vTipMps;

	public float vTailMps;

	public float tipZM;

	public string material;

	public float spacingM;

	public float VelocityAt(float aM)
	{
		if (lengthM <= 1e-9f)
		{
			return vTipMps;
		}
		float t = Mathf.Clamp01(aM / lengthM);
		return Mathf.Lerp(vTipMps, vTailMps, t);
	}
}
