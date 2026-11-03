public struct SuperShellSpec
{
	public float diameterM;

	public float lengthM;

	public float velocityMps;

	public NoseShape nose;

	public NoseShape coreNose;

	public float tipZM;

	public string coreMaterial;

	public float coreDiameterFrac;

	public string jacketMaterial;

	public float capLengthFrac;

	public string capMaterial;

	public bool HasJacket => coreDiameterFrac >= 0.999f && !string.IsNullOrEmpty(jacketMaterial);

	public bool HasCap => capLengthFrac > 0.0001f && !string.IsNullOrEmpty(capMaterial);

	public int BodyCount => 1 + (HasJacket ? 1 : 0) + (HasCap ? 1 : 0);
}
