using System;

[Serializable]
public class SavedProjectile
{
	public string catalogId;

	public string roundId;

	public string displayName;

	public string projectileClass;

	public string coreType;

	public float rodLengthMM;

	public float diameterMM;

	public float velocityMps;

	public float coneDiameterMM;

	public float fuzeStandoffMM;

	public bool isTandem;

	public float precursorConeDiameterMM;

	public string nose;

	public int segments;

	public int strands;

	public int finBlades;

	public bool noFins;

	public float gunCalibreMM;

	public float startXM;

	public float startYM;

	public float startZM;
}
