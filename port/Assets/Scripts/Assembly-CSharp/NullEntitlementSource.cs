public sealed class NullEntitlementSource : IEntitlementSource
{
	public string Name => "none";

	public OwnershipState Query(ContentPack pack)
	{
		return OwnershipState.Unknown;
	}
}
