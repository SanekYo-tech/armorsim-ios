using UnityEngine;

public struct SuperOutcomeResult
{
	public SuperVerdict verdict;

	public float throughFraction;

	public float bypassFraction;

	public Vector3 residualVelocity;

	public Vector3 rodMeanVelocity;

	public Vector3 plateMeanVelocity;

	public float departureSpeed;

	public float rodMassFractionRemaining;

	public double rodKineticJ;

	public float craterDepthM;

	public RulerReading craterReading;

	public float craterLateralOffsetM;

	public float craterOffsetSymmetricAxisM;

	public int rodParticles;

	public int plateParticles;
}
