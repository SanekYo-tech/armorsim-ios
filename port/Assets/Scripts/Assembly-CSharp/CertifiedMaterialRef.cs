public readonly struct CertifiedMaterialRef : IMaterialRef
{
	private readonly string _id;

	public MaterialTier Tier => MaterialTier.Certified;

	public string Id => _id;

	public CertifiedMaterialRef(string id)
	{
		_id = id;
	}

	public override string ToString()
	{
		return "certified:" + (_id ?? "<null>");
	}
}
