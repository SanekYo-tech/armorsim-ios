using System;

public static class ContinuumGate
{
	public const float AngleToleranceDeg = 0.5f;

	public const float LateralToleranceM = 0.002f;

	public const string KeyOk = "ultra.ok";

	public const string KeyEmpty = "ultra.reject.empty";

	public const string KeyAngle = "ultra.reject.angle";

	public const string KeyLateral = "ultra.reject.lateral";

	public const string KeyNonAxisym = "ultra.reject.nonaxisym";

	public const string KeyEraLateral = "ultra.reject.era_lateral";

	public const string KeyTargetMaterial = "ultra.reject.material_target";

	public const string KeyCoreMaterial = "ultra.reject.material_core";

	public const string KeyJetLiner = "ultra.reject.material_jet_liner";

	public const string KeyUnknownClass = "ultra.reject.class_unknown";

	public static bool IsUltraAvailable(SceneFile scene, out string reasonKey)
	{
		reasonKey = KeyOk;
		if (scene == null || scene.objects == null || scene.objects.Count == 0)
		{
			reasonKey = KeyEmpty;
			return false;
		}
		float num = 0f;
		float num2 = 0f;
		int num3 = 0;
		foreach (SavedObject savedObject in scene.objects)
		{
			if (Math.Abs(savedObject.angleXDeg) > AngleToleranceDeg || Math.Abs(savedObject.angleYDeg) > AngleToleranceDeg)
			{
				reasonKey = KeyAngle;
				return false;
			}
			if (!Enum.TryParse<ArmorPlate.ObjectKind>(savedObject.kind, false, out var objectKind))
			{
				reasonKey = KeyNonAxisym;
				return false;
			}
			if (objectKind == ArmorPlate.ObjectKind.Slat || objectKind == ArmorPlate.ObjectKind.Crew)
			{
				reasonKey = KeyNonAxisym;
				return false;
			}
			if (objectKind == ArmorPlate.ObjectKind.Nera && Math.Abs(savedObject.neraMountAngleDeg) > AngleToleranceDeg)
			{
				reasonKey = KeyEraLateral;
				return false;
			}
			if (!TargetMaterialSupported(savedObject, objectKind))
			{
				reasonKey = KeyTargetMaterial;
				return false;
			}
			num += savedObject.widthM;
			num2 += savedObject.heightM;
			num3++;
		}
		if (num3 > 0 && scene.projectile != null)
		{
			num /= (float)num3;
			if (Math.Abs(scene.projectile.startXM - num) > LateralToleranceM)
			{
				reasonKey = KeyLateral;
				return false;
			}
			num2 /= (float)num3;
			if (Math.Abs(scene.projectile.startYM - num2) > LateralToleranceM)
			{
				reasonKey = KeyLateral;
				return false;
			}
		}
		if (scene.projectile == null)
		{
			return true;
		}
		if (!Enum.TryParse<ProjectileClass>(scene.projectile.projectileClass, false, out var projectileClass))
		{
			reasonKey = KeyCoreMaterial;
			return false;
		}
		switch (projectileClass)
		{
		case ProjectileClass.LongRod:
		case ProjectileClass.ArmorPiercing:
		case ProjectileClass.Kinetic:
			if (!Enum.TryParse<Projectile.CoreType>(scene.projectile.coreType, false, out var coreType) || !TryMapCore(coreType, out var _))
			{
				reasonKey = KeyCoreMaterial;
				return false;
			}
			return true;
		case ProjectileClass.ShapedCharge:
			reasonKey = KeyJetLiner;
			return false;
		case ProjectileClass.HollowBody:
			reasonKey = "ultra.reject.no_hollow_body_model";
			return false;
		default:
			UnityEngine.Debug.LogError(string.Format("[ContinuumGate] класс боеприпаса {0} в воротах доступности " + "не назван — отказываю. Допиши строку сюда И в UltraProjection.CanRepresent/ClassRefusalKey.", projectileClass));
			reasonKey = KeyUnknownClass;
			return false;
		}
	}

	private static bool TargetMaterialSupported(SavedObject o, ArmorPlate.ObjectKind kind)
	{
		if (!MaterialNameSupported(o.material, out var _))
		{
			return false;
		}
		if (kind == ArmorPlate.ObjectKind.Nera && (!MaterialNameSupported(o.neraFrontMaterial, out var _) || !MaterialNameSupported(o.neraRearMaterial, out var _)))
		{
			return false;
		}
		if (kind == ArmorPlate.ObjectKind.EngineBlock && !string.IsNullOrEmpty(o.blockMaterial) && !MaterialNameSupported(o.blockMaterial, out var _))
		{
			return false;
		}
		if (kind == ArmorPlate.ObjectKind.SpallLiner && !string.IsNullOrEmpty(o.linerMaterial) && !MaterialNameSupported(o.linerMaterial, out var _))
		{
			return false;
		}
		return true;
	}

	private static bool MaterialNameSupported(string name, out ContinuumMaterialId id)
	{
		id = ContinuumMaterialId.RHA_MeyerKleponis;
		if (string.IsNullOrEmpty(name))
		{
			return true;
		}
		if (!Enum.TryParse<ArmorPlate.ArmorType>(name, false, out var armorType))
		{
			return false;
		}
		return TryMapTarget(armorType, out id);
	}

	public static bool TryMapTarget(ArmorPlate.ArmorType t, out ContinuumMaterialId id)
	{
		switch (t)
		{
		case ArmorPlate.ArmorType.RHA:
			id = ContinuumMaterials.ActiveRhaSet;
			return true;
		case ArmorPlate.ArmorType.Aluminum:
			id = ContinuumMaterialId.Al5083H116;
			return true;
		case ArmorPlate.ArmorType.Titanium:
			id = ContinuumMaterialId.Ti6Al4V;
			return true;
		default:
			id = ContinuumMaterialId.RHA_MeyerKleponis;
			return false;
		}
	}

	public static bool TryMapCore(Projectile.CoreType c, out ContinuumMaterialId id)
	{
		switch (c)
		{
		case Projectile.CoreType.WHA:
			id = ContinuumMaterialId.WHA;
			return true;
		case Projectile.CoreType.Steel:
			id = ContinuumMaterialId.Steel4340;
			return true;
		default:
			id = ContinuumMaterialId.WHA;
			return false;
		}
	}
}
