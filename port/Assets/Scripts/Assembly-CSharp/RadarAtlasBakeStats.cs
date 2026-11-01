using System.Globalization;

public sealed class RadarAtlasBakeStats
{
	public double WallSeconds;

	public double PrepareSeconds;

	public double IntegralSeconds;

	public int Threads;

	public int Cells;

	public int SharedVariants = 1;

	public long ShadowRays;

	public long EdgeOnFacets;

	public double ReciprocityMaxRatio;

	public double CellsPerSecond;

	public override string ToString()
	{
		return string.Format(CultureInfo.InvariantCulture, "variants sharing the pass {9}, cells {0}, threads {1}, wall {2:F1} s, Prepare {3:F1} s (thread-sum), integrals {4:F1} s (thread-sum), {5:F1} cells/s, shadow rays {6}, edge-on facets {7}, reciprocity max |S_HV−S_VH|/max|S| {8:E2}", Cells, Threads, WallSeconds, PrepareSeconds, IntegralSeconds, CellsPerSecond, ShadowRays, EdgeOnFacets, ReciprocityMaxRatio, SharedVariants);
	}
}
