public struct JetPenetrationOutcome
{
	public float totalPenetrationM;

	public bool perforated;

	public float cutoffVelocity;

	public int elementsConsumed;

	public int elementsTotal;

	public int elementsAfterBreakup;

	public float consumedJetLengthM;

	public float dispersionLossFraction;

	public float craterRadiusEntryM;

	public float craterRadiusMidsectionM;

	public float missedJetLengthM;

	public float geometricCutoffVelocity;

	public JetElement[] timeline;
}
