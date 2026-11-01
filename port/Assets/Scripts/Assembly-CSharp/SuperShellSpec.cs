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

	public bool HasJacket => false;

	public bool HasCap => false;

	public int BodyCount => 0;
}
