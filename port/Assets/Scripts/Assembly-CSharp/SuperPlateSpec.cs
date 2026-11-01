using UnityEngine;

public struct SuperPlateSpec
{
	public float thicknessM;

	public float angleXDeg;

	public float alongAxisM;

	public float sideM;

	public string material;

	public bool clampedEdges;

	public bool noFragmentation;

	public float spacingM;

	public float offsetUM;

	public float offsetVM;

	public float fineCoreRadiusM;

	public bool hasPose;

	public Vector3 poseCentreM;

	public Vector3 poseU;

	public Vector3 poseV;

	public Vector3 poseN;

	public float widthM;

	public float heightM;

	public SuperClip clip;

	public SuperMeshRef mesh;

	public bool roundU;
}
