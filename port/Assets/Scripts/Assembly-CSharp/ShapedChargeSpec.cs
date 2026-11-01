public struct ShapedChargeSpec
{
	public string name;

	public float coneDiameterM;

	public float vTip;

	public float vTail;

	public float jetMassScaleFactor;

	public float jetMassOverrideKg;

	public float breakupScaleFactor;

	public float breakupTimeOverrideS;

	public float dispersionAngleRad;

	public bool channelReaming;

	public float reamingAxialExtentRatio;

	public float sigmaVelocityExponent;

	public float sigmaRefVelocityMS;

	public bool angularCriterion;

	public float sigmaTransverseMS;

	public float dispersionScaleFactor;

	public float dispersionTimeOverrideS;

	public float cutoffVelocityOverride;

	public JetMaterial material;

	public string provenance;
}
