public interface IEntitlementSource
{
	string Name { get; }

	OwnershipState Query(ContentPack pack);
}
