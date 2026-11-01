public struct RoundSpec
{
	public string id;

	public string displayName;

	public string category;

	public int year;

	public float calibreMM;

	public float penetratorMassKg;

	public float muzzleVelocityMps;

	public float lengthOverDiameter;

	public Projectile.CoreType core;

	public float claimedRhaMM;

	public string limitation;

	public RoundGeometrySource geometrySource;

	public float massToleranceFrac;

	public RoundNumber coreLengthMM;

	public RoundNumber coreDiameterMM;

	public RoundName coreMaterialId;

	public RoundNumber coreDiameterFrac;

	public RoundNumber capLengthFrac;

	public RoundName capMaterialId;

	public RoundNose nose;

	public RoundNumber cavityFrac;

	public RoundNumber impactVelocityMps;

	public RoundNumber fuzeDelayS;

	public RoundNumber fragmentVelocityMps;

	public string passportId;
}
