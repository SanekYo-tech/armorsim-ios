using System.Collections.Generic;

public static class ProjectileLibrary
{
	public static ProjectileDefinition WHALongRod()
	{
		return new ProjectileDefinition
		{
			name = "WHA длинный стержень",
			kind = ProjectileClass.LongRod,
			rodLengthMM = 300f,
			diameterMM = 30f,
			velocityMps = 1100f,
			nose = NoseShape.Conical,
			segments = 12,
			strands = 7
		};
	}

	public static ProjectileDefinition DULongRod()
	{
		return new ProjectileDefinition
		{
			name = "DU длинный стержень",
			kind = ProjectileClass.LongRod,
			coreType = Projectile.CoreType.DU,
			rodLengthMM = 450f,
			diameterMM = 30f,
			velocityMps = 1200f,
			nose = NoseShape.Ogive,
			segments = 14,
			strands = 7
		};
	}

	public static ProjectileDefinition SteelAP()
	{
		return new ProjectileDefinition
		{
			name = "Стальной бронебойный",
			kind = ProjectileClass.ArmorPiercing,
			coreType = Projectile.CoreType.Steel,
			rodLengthMM = 150f,
			diameterMM = 40f,
			velocityMps = 900f,
			nose = NoseShape.BluntOgive,
			segments = 8,
			strands = 7,
			noFins = true
		};
	}

	public static ProjectileDefinition SteelAPC()
	{
		return new ProjectileDefinition
		{
			name = "Бронебойный с колпачком",
			kind = ProjectileClass.ArmorPiercing,
			coreType = Projectile.CoreType.Steel,
			rodLengthMM = 150f,
			diameterMM = 40f,
			velocityMps = 900f,
			nose = NoseShape.BluntOgive,
			ballisticCap = true,
			segments = 8,
			strands = 7,
			noFins = true
		};
	}

	public static ProjectileDefinition WCCore()
	{
		return new ProjectileDefinition
		{
			name = "Карбид-W сердечник",
			kind = ProjectileClass.Kinetic,
			coreType = Projectile.CoreType.WC,
			rodLengthMM = 120f,
			diameterMM = 25f,
			velocityMps = 1000f,
			nose = NoseShape.Conical,
			segments = 8,
			strands = 5
		};
	}

	public static ProjectileDefinition ShapedCharge85()
	{
		return new ProjectileDefinition
		{
			name = "Кумулятивный 85 мм",
			kind = ProjectileClass.ShapedCharge,
			coreType = Projectile.CoreType.Steel,
			rodLengthMM = 300f,
			diameterMM = 85f,
			velocityMps = 900f,
			coneDiameterMM = 85f,
			fuzeStandoffMM = 170f,
			bodyModelResource = "Models/defaulttled",
			nose = NoseShape.Ogive,
			segments = 10,
			strands = 7,
			noFins = true
		};
	}

	public static ProjectileDefinition TandemShapedCharge()
	{
		return new ProjectileDefinition
		{
			name = "Тандемный кумулятивный",
			kind = ProjectileClass.ShapedCharge,
			coreType = Projectile.CoreType.Steel,
			rodLengthMM = 380f,
			diameterMM = 85f,
			velocityMps = 900f,
			coneDiameterMM = 85f,
			fuzeStandoffMM = 170f,
			bodyModelResource = "Models/defaulttled",
			isTandem = true,
			precursorConeDiameterMM = 30f,
			interChargeDelayS = 0.0004f,
			nose = NoseShape.Ogive,
			segments = 10,
			strands = 7,
			noFins = true
		};
	}

	public static ProjectileDefinition EtalonFlatRod()
	{
		return new ProjectileDefinition
		{
			name = "Эталонный стержень (плоский торец)",
			kind = ProjectileClass.LongRod,
			rodLengthMM = 300f,
			diameterMM = 30f,
			velocityMps = 1100f,
			nose = NoseShape.Flat,
			segments = 12,
			strands = 7
		};
	}

	public static ProjectileDefinition Default()
	{
		return WHALongRod();
	}

	public static ProjectileDefinition[] All()
	{
		IList<ProjectileCatalogEntry> projectiles = Catalog.Projectiles;
		ProjectileDefinition[] array = new ProjectileDefinition[projectiles.Count];
		for (int i = 0; i < projectiles.Count; i++)
		{
			array[i] = projectiles[i].make();
		}
		return array;
	}
}
