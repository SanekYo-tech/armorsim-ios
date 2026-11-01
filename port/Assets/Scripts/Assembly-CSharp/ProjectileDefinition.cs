using System;
using UnityEngine;

[Serializable]
public class ProjectileDefinition
{
	[Header("Идентификация")]
	public string name = "Длинный стержень";

	public ProjectileClass kind;

	[Header("Материал сердечника")]
	public Projectile.CoreType coreType;

	[Header("Геометрия по умолчанию (мм) — применяется при выборе типа")]
	public float rodLengthMM = 300f;

	public float diameterMM = 30f;

	public float velocityMps = 1100f;

	[Header("Кумулятивный заряд (только при kind == ShapedCharge)")]
	public float coneDiameterMM = 85f;

	public float fuzeStandoffMM = 170f;

	public bool isTandem;

	public float precursorConeDiameterMM = 30f;

	public bool precursorInNoseProbe;

	public float interChargeDelayS = 0.0004f;

	[Header("Визуал")]
	public string bodyModelResource = "";

	public string roofBodyModelResource = "";

	public float roofBodyLengthMM;

	public NoseShape nose = NoseShape.Conical;

	public bool ballisticCap;

	public float coreDiameterFrac;

	public float capLengthFrac;

	public string coreMaterialId = "";

	public string capMaterialId = "";

	public float cavityFrac;

	public float fuzeDelayS;

	public float fragmentVelocityMps;

	public string passportId = "";

	public int segments = 12;

	public int strands = 7;

	public int finBlades = 6;

	public bool noFins;

	public float gunCalibreMM;

	public bool IsShapedCharge => kind == ProjectileClass.ShapedCharge;

	public bool IsHollowBody => kind == ProjectileClass.HollowBody;

	public string BodyModelFor(TargetSection section)
	{
		if (section == TargetSection.Roof && !string.IsNullOrEmpty(roofBodyModelResource))
		{
			return roofBodyModelResource;
		}
		return bodyModelResource;
	}

	public float BodyLengthMMFor(TargetSection section)
	{
		if (section == TargetSection.Roof && roofBodyLengthMM > 0f)
		{
			return roofBodyLengthMM;
		}
		return rodLengthMM;
	}

	public ShapedChargeSpec GetShapedChargeSpec()
	{
		ShapedChargeSpec spec = ShapedChargeCore.DemoPlaceholder();
		spec.coneDiameterM = coneDiameterMM / 1000f;
		return spec;
	}

	public ProjectileDefinition Clone()
	{
		ProjectileDefinition p = new ProjectileDefinition();
		p.name = name;
		p.kind = kind;
		p.coreType = coreType;
		p.rodLengthMM = rodLengthMM;
		p.diameterMM = diameterMM;
		p.velocityMps = velocityMps;
		p.coneDiameterMM = coneDiameterMM;
		p.fuzeStandoffMM = fuzeStandoffMM;
		p.isTandem = isTandem;
		p.precursorConeDiameterMM = precursorConeDiameterMM;
		p.precursorInNoseProbe = precursorInNoseProbe;
		p.interChargeDelayS = interChargeDelayS;
		p.nose = nose;
		p.ballisticCap = ballisticCap;
		p.bodyModelResource = bodyModelResource;
		p.roofBodyModelResource = roofBodyModelResource;
		p.roofBodyLengthMM = roofBodyLengthMM;
		p.segments = segments;
		p.strands = strands;
		p.finBlades = finBlades;
		p.gunCalibreMM = gunCalibreMM;
		p.noFins = noFins;
		p.coreDiameterFrac = coreDiameterFrac;
		p.capLengthFrac = capLengthFrac;
		p.coreMaterialId = coreMaterialId;
		p.capMaterialId = capMaterialId;
		p.cavityFrac = cavityFrac;
		p.fuzeDelayS = fuzeDelayS;
		p.fragmentVelocityMps = fragmentVelocityMps;
		p.passportId = passportId;
		return p;
	}
}
