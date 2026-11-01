using System;
using UnityEngine;

public static class SectionFrame
{
	public const float RoofAngleMinDeg = 30f;

	public const float RoofAngleMaxDeg = 180f;

	public const float RoofAngleDefaultDeg = 65f;

	public const float RoofAimSinkMM = 1f;

	public static TargetSection Active => AppFlow.ActiveSection;

	public static bool IsRoof => AppFlow.ActiveSection == TargetSection.Roof;

	public static float RoofLineYM => 1f;

	public static Vector3 RoofAimPoint => new Vector3(0f, RoofLineYM - SceneScale.RodDiameterWorld(RoofAimSinkMM), 0f);

	public static Vector3 AtgmDirection => Vector3.down;

	public static Vector3 StackAxis(TargetSection s)
	{
		if (s == TargetSection.Roof)
		{
			return Vector3.down;
		}
		return Vector3.forward;
	}

	public static Vector3 StackAxis()
	{
		return StackAxis(Active);
	}

	public static Quaternion PlateBase(TargetSection s)
	{
		if (s == TargetSection.Roof)
		{
			return Quaternion.Euler(-90f, 0f, 0f);
		}
		return Quaternion.identity;
	}

	public static Quaternion PlateBase()
	{
		return PlateBase(Active);
	}

	public static Vector3 FirstPlateCentre(TargetSection s, float thicknessMM)
	{
		if (s == TargetSection.Roof)
		{
			return new Vector3(0f, RoofLineYM - Mathf.Max(1E-04f, SceneScale.RodDiameterWorld(thicknessMM)) * 0.5f, 0f);
		}
		return new Vector3(0f, RoofLineYM, SceneDefaults.ApproachM);
	}

	public static Vector3 FirstPlateCentre(float thicknessMM)
	{
		return FirstPlateCentre(Active, thicknessMM);
	}

	public static float ClampRoofAngle(float deg)
	{
		return Mathf.Clamp(deg, RoofAngleMinDeg, RoofAngleMaxDeg);
	}

	public static Vector3 AngledDirection(float alphaDeg)
	{
		float num = ClampRoofAngle(alphaDeg) * Mathf.Deg2Rad;
		Vector3 vector = new Vector3(0f, -MathF.Sin(num), MathF.Cos(num));
		if (vector.sqrMagnitude <= 1E-12f)
		{
			return Vector3.down;
		}
		return vector.normalized;
	}

	public static Vector3 AngledOrigin(float alphaDeg)
	{
		return RoofAimPoint - AngledDirection(alphaDeg) * SceneDefaults.ApproachM;
	}

	public static float AtgmClearanceM(Projectile pr)
	{
		float num = 0f;
		if (pr != null)
		{
			ProjectileDefinition projectileDefinition = pr.GetDefinition();
			if (projectileDefinition != null && projectileDefinition.fuzeStandoffMM > 0f)
			{
				num = projectileDefinition.fuzeStandoffMM / 1000f;
			}
		}
		return Mathf.Max(0.1f, num);
	}

	public static Vector3 AtgmOrigin(Projectile pr)
	{
		return new Vector3(0f, AtgmClearanceM(pr) + RoofLineYM, 0f);
	}

	public static Vector3 AtgmApproachFrom(Projectile pr)
	{
		return AtgmOrigin(pr) - Vector3.forward * SceneDefaults.ApproachM;
	}

	public static string Describe(TargetSection s)
	{
		if (s == TargetSection.Roof)
		{
			return string.Format("Раздел: КРЫША. Плоскость крыши y = {0:F2} м, пакет растёт вниз, ", RoofLineYM) + "плиты горизонтальны (нормаль +Y).";
		}
		return string.Format("Раздел: КОРПУС. Линия выстрела y = {0:F2} м, пакет растёт по +Z, ", RoofLineYM) + "плиты вертикальны (нормаль +Z).";
	}
}
