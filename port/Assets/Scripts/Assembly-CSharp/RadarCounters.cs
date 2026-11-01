public sealed class RadarCounters
{
	public long LaunchedRays;

	public long HitRays;

	public long SurfaceHits;

	public long GrazingTerminations;

	public long SelfHitsSkipped;

	public long EdgeOnFacets;

	public long ShadowRays;

	public long VisibleFacetSubtriangles;

	public long MultiBounceRays;

	public long QualifiedPaths;

	public bool RayCapApplied;

	public bool ResolutionBelowOne;

	public double GrazingFraction => 0.0;
}
