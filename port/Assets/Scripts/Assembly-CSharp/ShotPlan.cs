using System.Collections.Generic;
using UnityEngine;

public class ShotPlan
{
	public List<LayerPlaybackData> layers;

	public List<Vector3> tracePoints;

	public ShotVerdict verdict;

	public int verdictLayerIndex;

	public float finalVelocity;

	public string verdictMessage;

	public Color verdictColor;

	public Vector3 lineEnd;

	public Color lineColor;

	public Vector3 origin;

	public Vector3 direction;

	public bool hasApproach;

	public Vector3 approachFrom;

	public float L0M;

	public float diameterMM;

	public float rayDistance;

	public Projectile projectile;

	public bool isShapedCharge;

	public Vector3 detonationPoint;

	public float coneDiameterM;

	public float breakupTimeS;

	public float elementDvMS;

	public JetElement[] jetTimeline;

	public int jetElementsAtDetonation;

	public int tandemPrecursorArmed;

	public readonly List<ArmorPlate> precursorArmedPlates;

	public float jetTipVelocityMS;

	public PrecursorCharge precursor;

	public float MainChargeDelayS => (precursor != null) ? precursor.delayS : 0f;
}
