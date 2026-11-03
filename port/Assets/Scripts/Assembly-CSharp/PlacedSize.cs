using UnityEngine;

public struct PlacedSize
{
	public float widthM;

	public float heightM;

	public float thicknessMM;

	public bool overridesPlan;

	public bool overridesThickness;

	public Vector3 PhysicalMeters => new Vector3(widthM, heightM, thicknessMM / 1000f);
}
