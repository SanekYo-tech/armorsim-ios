using System.Collections.Generic;
using UnityEngine;

public static class UltraEditorState
{
	public static bool FromScene(Projectile proj, out UltraShotEdit shot, out List<UltraPlateEdit> plates, out string reasonKey)
	{
		shot = default;
		plates = new List<UltraPlateEdit>();
		reasonKey = null;
		if (proj == null)
		{
			reasonKey = "ultra.reject.no_plates";
			return false;
		}
		ProjectileDefinition definition = proj.GetDefinition();
		shot.kind = definition.kind;
		shot.core = definition.coreType;
		shot.calibreMM = Mathf.Max(0.001f, definition.diameterMM);
		shot.lengthOverDiameter = Mathf.Max(0.1f, definition.rodLengthMM / shot.calibreMM);
		shot.nose = ((definition.nose != NoseShape.Flat) ? ((definition.nose != NoseShape.Ogive) ? ContinuumNose.Conical : ContinuumNose.Ogive) : ContinuumNose.Flat);
		shot.noseHalfAngleDeg = 0f;
		shot.velocityMps = Mathf.Max(1f, definition.velocityMps);
		shot.yawDeg = 0f;
		shot.analyticNose = definition.nose;
		shot.ballisticCap = definition.ballisticCap;
		Vector3 projPos = proj.transform.position;
		Vector3 shotDir;
		if (proj.direction.sqrMagnitude <= 1e-9f)
		{
			shotDir = Vector3.forward;
		}
		else
		{
			shotDir = proj.direction.normalized;
		}
		ArmorPlate[] array = Object.FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			ArmorPlate plate = array[i];
			if (plate.kind != ArmorPlate.ObjectKind.Plate)
			{
				reasonKey = "ultra.reject.nonaxisym";
				return false;
			}
			Vector3 delta = plate.transform.position - projPos;
			float alongAxis = Vector3.Dot(shotDir, delta);
			if (alongAxis > 0f)
			{
				Vector3 lateral = delta - shotDir * alongAxis;
				plates.Add(new UltraPlateEdit
				{
					alongAxisM = alongAxis,
					thicknessMM = plate.thicknessMM,
					material = plate.armorType,
					tiltDeg = plate.GetEffectiveAngle(),
					leanSign = LeanSign(plate.transform.forward, shotDir),
					lateralOffsetM = lateral.magnitude
				});
			}
		}
		if (plates.Count == 0)
		{
			reasonKey = "ultra.reject.no_plates";
			return false;
		}
		return true;
	}

	public static float LeanSign(Vector3 plateNormal, Vector3 shotDir)
	{
		if (plateNormal.sqrMagnitude < 1e-9f || shotDir.sqrMagnitude < 1e-9f)
		{
			return 0f;
		}
		Vector3 n = plateNormal.normalized;
		Vector3 d = shotDir.normalized;
		Vector3 side = Vector3.ProjectOnPlane(Vector3.up, n);
		if (side.sqrMagnitude < 1e-6f)
		{
			return 0f;
		}
		side = side.normalized;
		float dot = Vector3.Dot(side, d);
		if (Mathf.Abs(dot) < 1e-4f)
		{
			return 0f;
		}
		if (dot > 0f)
		{
			return 1f;
		}
		return -1f;
	}
}
