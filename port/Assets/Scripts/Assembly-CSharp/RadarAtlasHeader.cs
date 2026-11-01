using System;

[Serializable]
public sealed class RadarAtlasHeader
{
	public int format;

	public string key;

	public string modelKey;

	public string modelSha256;

	public string modelUnit;

	public double modelLengthM;

	public int[] modelOrientation;

	public double frequencyHz;

	public double deltaDeg;

	public int samplesPerSide;

	public string seed;

	public string solverTag;

	public int faces;

	public double sizeInWavelengths;

	public bool boundaryRegion;

	public string date;

	public double bakeSeconds;

	public double prepareSeconds;

	public double integralSeconds;

	public int threads;

	public double cellsPerSecond;

	public double minDb;

	public double medianDb;

	public double maxDb;

	public double reciprocityMaxRatio;

	public int sharedVariants;

	public long edgeOnFacets;

	public long shadowRays;

	public string payloadSha256;

	public RadarAtlasCoatingInfo coating;
}
