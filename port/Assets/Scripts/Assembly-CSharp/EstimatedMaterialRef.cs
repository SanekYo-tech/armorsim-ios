public readonly struct EstimatedMaterialRef : IMaterialRef
{
	private readonly string _id;

	public MaterialTier Tier => MaterialTier.Estimated;

	public string Id => _id;

	public EstimatedMaterialRef(string id)
	{
		_id = id;
	}

	public override string ToString()
	{
		return "estimated:" + (_id ?? "<null>");
	}
}
