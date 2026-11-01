using System.Collections.Generic;

public struct PenetrationOutcome
{
	public PenetrationResult result;

	public float residualVelocity;

	public float residualLengthM;

	public float erosionFraction;

	public float penetrationDepthMM;

	public float ballisticLimit;

	public float erosionOnsetMax;

	public float noseAreaOut;

	public List<TateStep> steps;
}
