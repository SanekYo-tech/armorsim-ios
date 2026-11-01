using System;

public sealed class RadarResult
{
	public bool Refused;

	public string RefusalKey;

	public bool BoundaryRegion;

	public bool FirstOrderOnly;

	public double ElapsedMilliseconds;

	public double WavelengthMeters;

	public double SizeInWavelengths;

	public double SurfaceOffsetMeters;

	public double SurfaceOffsetOverWavelength;

	public double EffectiveRaysPerWavelength;

	public bool ResolutionBelowOne;

	public int GridSide;

	public Complex64[,] S;

	public double[,] SigmaSquareMeters;

	public double[,] SigmaDbSquareMeters;

	public Complex64[,,] SByOrder;

	public double[] FacetAmplitude;

	public double[,,] FacetAmplitudeByPolarization;

	public int[] MaxBounceByFacet;

	public bool[] FacetIlluminated;

	public RadarPath[] Paths;

	public RadarCounters Counters;

	public double[] GetFacetAmplitude(int receivePolarization, int transmitPolarization)
	{
		if (FacetAmplitudeByPolarization == null)
		{
			return null;
		}
		if (receivePolarization > 1 || transmitPolarization > 1)
		{
			throw new ArgumentOutOfRangeException();
		}
		int n = FacetAmplitudeByPolarization.GetLength(2);
		double[] result = new double[n];
		for (int i = 0; i < n; i++)
		{
			result[i] = FacetAmplitudeByPolarization[receivePolarization, transmitPolarization, i];
		}
		return result;
	}
}
