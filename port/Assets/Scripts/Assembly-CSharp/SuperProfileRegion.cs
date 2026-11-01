public struct SuperProfileRegion
{
	public string materialId;

	public string name;

	public int latSub;

	public float aFromM;

	public float aToM;

	public float rInnerM;

	public float rOuterM;

	public bool Matches(float a, float rho2)
	{
		if (a < aFromM || a >= aToM)
		{
			return false;
		}
		if (rInnerM > 0f && !(rho2 > rInnerM * rInnerM))
		{
			return false;
		}
		return rOuterM == float.PositiveInfinity || rho2 <= rOuterM * rOuterM;
	}
}
