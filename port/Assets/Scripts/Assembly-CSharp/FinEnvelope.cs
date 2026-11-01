using System;
using UnityEngine;

public struct FinEnvelope
{
	public float spanM;

	public float rootOffsetM;

	public int blades;

	public bool present;

	public bool stripped;

	public float OuterRadiusM => Mathf.Max(spanM, 0f) * 0.5f;

	public static FinEnvelope For(float rodDiameterM, float gunCalibreM, float rodLengthM, int blades, bool hasFins)
	{
		return new FinEnvelope
		{
			spanM = RodGeometry.FinSpanM(rodDiameterM, gunCalibreM),
			rootOffsetM = Mathf.Max(rodLengthM, 0f),
			blades = Math.Max(blades, 1),
			present = hasFins,
			stripped = false
		};
	}
}
