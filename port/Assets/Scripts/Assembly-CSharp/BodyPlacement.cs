using UnityEngine;

public sealed class BodyPlacement
{
	public IShotObject Object { get; }

	public float StandoffMm { get; }

	public Quaternion Orientation { get; }

	public Vector2 AimOffsetMm { get; }

	public BodyPlacement(IShotObject obj, float standoffMm, Quaternion orientation, Vector2 aimOffsetMm)
	{
	}

	public BodyPlacement(IShotObject obj, float standoffMm)
	{
	}
}
