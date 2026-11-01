using System.Collections.Generic;

public sealed class ShotReport
{
	public string RoundId;

	public string CoreTypeName;

	public float CaliberMM;

	public float MassKg;

	public float MuzzleVelocity;

	public bool IsShapedCharge;

	public string SimModeName;

	public List<LayerHit> Layers;

	public ShotOutcome FinalVerdict;

	public bool PenetratedAll;

	public float SolveMilliseconds;
}
