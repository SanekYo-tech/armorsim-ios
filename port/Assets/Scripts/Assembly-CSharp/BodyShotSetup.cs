using UnityEngine;

public readonly struct BodyShotSetup
{
	public readonly Vector3 OriginMm;

	public readonly Vector3 Direction;

	public readonly string ProjectileId;

	public BodyShotSetup(Vector3 originMm, Vector3 direction, string projectileId)
	{
		OriginMm = originMm;
		Direction = direction;
		ProjectileId = projectileId;
	}
}
