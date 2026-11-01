public readonly struct TraceSegment
{
	public readonly SegmentKind Kind;

	public readonly IMaterialRef Material;

	public readonly float LengthMm;

	public readonly float NormalThicknessMm;

	public readonly float ObliquityDeg;

	public readonly int OwnerPlacementIndex;

	public readonly string OwnerObjectId;

	public readonly EntrySurface Entry;

	public TraceSegment(SegmentKind kind, IMaterialRef material, float lengthMm, float normalThicknessMm, float obliquityDeg, int ownerPlacementIndex, string ownerObjectId, EntrySurface entry)
	{
		Kind = kind;
		Material = material;
		LengthMm = lengthMm;
		NormalThicknessMm = normalThicknessMm;
		ObliquityDeg = obliquityDeg;
		OwnerPlacementIndex = ownerPlacementIndex;
		OwnerObjectId = ownerObjectId;
		Entry = entry;
	}

	public static TraceSegment MakeVoid(float lengthMm, int ownerPlacementIndex, string ownerObjectId)
	{
		return new TraceSegment(SegmentKind.Void, null, lengthMm, 0f, 0f, ownerPlacementIndex, ownerObjectId, EntrySurface.Unknown);
	}
}
