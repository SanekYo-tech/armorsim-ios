using Unity.Mathematics;

public sealed class RadarPatternResult
{
	public readonly double3[] Directions;

	public readonly double[] RelativePower;

	public readonly double[] SigmaSquareMeters;

	public readonly int AzimuthSamples;

	public readonly int ElevationSamples;

	public readonly bool FirstOrderOnly;

	public readonly bool Refused;

	public readonly string RefusalKey;

	public readonly double PeakSigma;

	public readonly double ElapsedMilliseconds;

	internal RadarPatternResult(double3[] directions, double[] relativePower, double[] sigma, int azimuthSamples, int elevationSamples, bool refused, string refusalKey, double peakSigma, double elapsedMilliseconds)
	{
	}
}
