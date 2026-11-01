using System;

[Serializable]
public class SavedObject
{
	public string kind;

	public string material;

	public float posXM;

	public float posYM;

	public float posZM;

	public float angleXDeg;

	public float angleYDeg;

	public float widthM;

	public float heightM;

	public float thicknessMM;

	public float eraPlateThicknessMM;

	public float eraPlateVelocityMS;

	public float eraInterlayerGapMM;

	public float eraCassetteExtentMM;

	public float neraFrontThicknessMM;

	public float neraRearThicknessMM;

	public float neraInterlayerMM;

	public float neraMountAngleDeg;

	public float neraExtentMM;

	public float neraImpulsePerArea;

	public string neraFrontMaterial;

	public string neraRearMaterial;

	public string neraInterlayer;

	public string neraBonding;

	public float slatBarWidthMM;

	public float slatPitchMM;

	public float slatDudFraction;

	public string blockMaterial;

	public float voidFraction;

	public string linerMaterial;

	public string presetId;

	public bool edgeClamp;

	public SavedObject()
	{
		kind = "Plate";
		material = "RHA";
		heightM = 1f;
		thicknessMM = 1f;
		eraPlateThicknessMM = 50f;
		neraFrontMaterial = "RHA";
		neraRearMaterial = "RHA";
		neraInterlayer = "Polyurethane";
		neraBonding = "Unknown";
		blockMaterial = "";
		linerMaterial = "";
		presetId = "";
		edgeClamp = true;
	}
}
