public interface IAnchorSet
{
	AnchorProvenance Provenance { get; }

	float Get(AnchorId id);
}
