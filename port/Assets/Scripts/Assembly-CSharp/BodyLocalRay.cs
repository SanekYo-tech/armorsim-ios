using UnityEngine;

public readonly struct BodyLocalRay
{
	public readonly Vector3 OriginMm;

	public readonly Vector3 Direction;

	public BodyLocalRay(Vector3 originMm, Vector3 direction)
	{
		OriginMm = originMm;
		Direction = direction;
	}

	public Vector3 At(float t)
	{
		return OriginMm + Direction * t;
	}
}
