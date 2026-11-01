using UnityEngine;

public sealed class AeroResult
{
	public Vector3 Force;

	public double Drag;

	public double MidshipArea;

	public double LateralRatio;

	public double ShadowedFraction;

	public float[] Cp;

	public bool[] VisibleTriangles;

	public int HitCount;

	public int InvertedHitCount;

	public double Milliseconds;
}
