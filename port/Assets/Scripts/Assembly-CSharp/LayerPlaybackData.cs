using UnityEngine;

public class LayerPlaybackData
{
	public ArmorPlate plate;

	public Vector3 entryPoint;

	public Vector3 direction;

	public LayerEventKind eventKind;

	public PenetrationOutcome outcome;

	public float tEffMM;

	public float entryVelocity;

	public float entryLengthM;

	public Vector3 exitDirection;

	public Vector3 exitPoint;

	public Vector3 surfaceNormal;

	public float exitVelocity;

	public float exitLengthM;

	public float grooveDepthMM;

	public float slideTrackWorld;

	public float yawDeg;

	public float kYaw;

	public bool tailBrokeFromYaw;

	public float tailBreakLengthM;

	public JetElement[] jetTimeline;

	public int jetElementsBefore;

	public int jetElementsRetired;

	public float jetDepthMargin;
}
