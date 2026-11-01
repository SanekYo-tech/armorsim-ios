public static class ShotRouting
{
	public static bool Resists(ArmorPlate.ObjectKind kind)
	{
		return kind != ArmorPlate.ObjectKind.Slat && kind != ArmorPlate.ObjectKind.SpallLiner && kind != ArmorPlate.ObjectKind.Crew;
	}

	public static bool Registers(ArmorPlate.ObjectKind kind)
	{
		return kind != ArmorPlate.ObjectKind.Slat;
	}

	public static bool IsVisibleToShot(ArmorPlate.ObjectKind kind)
	{
		return true;
	}

	public static bool IsReactive(ArmorPlate.ObjectKind kind)
	{
		return kind == ArmorPlate.ObjectKind.Era || kind == ArmorPlate.ObjectKind.Nera;
	}

	public static bool FinEnvelopeApplies(ArmorPlate.ObjectKind kind)
	{
		return kind != ArmorPlate.ObjectKind.Slat && kind != ArmorPlate.ObjectKind.SpallLiner && kind != ArmorPlate.ObjectKind.Crew;
	}

	public static bool Fires(ShotBranch branch, ArmorPlate.ObjectKind kind, ThreatClass threat)
	{
		switch (branch)
		{
		case ShotBranch.EraJet:
			return (threat == ThreatClass.ShapedCharge || threat == ThreatClass.TandemShapedCharge) && kind == ArmorPlate.ObjectKind.Era;
		case ShotBranch.EraRod:
			return kind == ArmorPlate.ObjectKind.Era && threat == ThreatClass.KineticRod;
		case ShotBranch.NeraJet:
			return (threat == ThreatClass.ShapedCharge || threat == ThreatClass.TandemShapedCharge) && kind == ArmorPlate.ObjectKind.Nera;
		case ShotBranch.NeraRod:
			return kind == ArmorPlate.ObjectKind.Nera && threat == ThreatClass.KineticRod;
		case ShotBranch.PlainLayer:
			return kind != ArmorPlate.ObjectKind.Slat && kind != ArmorPlate.ObjectKind.SpallLiner && kind != ArmorPlate.ObjectKind.Crew && kind != ArmorPlate.ObjectKind.Era && threat != ThreatClass.Fragment && kind != ArmorPlate.ObjectKind.Nera;
		case ShotBranch.SpallLinerCatch:
			return kind == ArmorPlate.ObjectKind.SpallLiner && threat == ThreatClass.Fragment;
		case ShotBranch.LinerRegistration:
			return kind == ArmorPlate.ObjectKind.SpallLiner && threat != ThreatClass.Fragment;
		case ShotBranch.EngineDamage:
			return kind == ArmorPlate.ObjectKind.EngineBlock && threat == ThreatClass.Fragment;
		case ShotBranch.CrewRegistration:
			return kind == ArmorPlate.ObjectKind.Crew;
		case ShotBranch.SlatResolution:
			return (threat == ThreatClass.ShapedCharge || threat == ThreatClass.TandemShapedCharge) && kind == ArmorPlate.ObjectKind.Slat;
		case ShotBranch.ScreenFuze:
			return (threat == ThreatClass.ShapedCharge || threat == ThreatClass.TandemShapedCharge) && kind == ArmorPlate.ObjectKind.Screen;
		case ShotBranch.PrecursorArms:
			return kind == ArmorPlate.ObjectKind.Era && threat == ThreatClass.TandemShapedCharge;
		default:
			return false;
		}
	}

	public static ThreatClass ClassOf(ProjectileDefinition def)
	{
		if (def == null || !def.IsShapedCharge)
		{
			return ThreatClass.KineticRod;
		}
		return def.isTandem ? ThreatClass.TandemShapedCharge : ThreatClass.ShapedCharge;
	}

	public static string BranchName(ShotBranch b)
	{
		switch (b)
		{
		case ShotBranch.EraJet:
			return "ДЗ против струи";
		case ShotBranch.EraRod:
			return "ДЗ против стержня";
		case ShotBranch.NeraJet:
			return "НДЗ против струи";
		case ShotBranch.NeraRod:
			return "НДЗ против стержня";
		case ShotBranch.PlainLayer:
			return "обычный слой";
		case ShotBranch.SpallLinerCatch:
			return "подбой ловит осколок";
		case ShotBranch.LinerRegistration:
			return "регистрация у подбоя";
		case ShotBranch.EngineDamage:
			return "повреждение отсека";
		case ShotBranch.CrewRegistration:
			return "регистрация у экипажа";
		case ShotBranch.SlatResolution:
			return "разрешение решётки";
		case ShotBranch.ScreenFuze:
			return "подрыв на экране";
		case ShotBranch.PrecursorArms:
			return "предзаряд снимает кассету";
		default:
			return b.ToString();
		}
	}

	public static string ThreatName(ThreatClass t)
	{
		switch (t)
		{
		case ThreatClass.KineticRod:
			return "стержень";
		case ThreatClass.ShapedCharge:
			return "струя";
		case ThreatClass.TandemShapedCharge:
			return "тандем";
		default:
			return "осколок";
		}
	}
}
