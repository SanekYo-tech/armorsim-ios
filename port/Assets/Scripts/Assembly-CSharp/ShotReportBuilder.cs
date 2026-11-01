using System.Collections.Generic;
using UnityEngine;

public static class ShotReportBuilder
{
	public static ShotReport FromPlan(ShotPlan plan, string simModeName)
	{
		if (plan == null)
		{
			return null;
		}
		ShotReport shotReport = new ShotReport
		{
			RoundId = "",
			CoreTypeName = "",
			SimModeName = "",
			Layers = new List<LayerHit>()
		};
		shotReport.SimModeName = simModeName ?? "";
		shotReport.IsShapedCharge = plan.isShapedCharge;
		shotReport.FinalVerdict = Outcome(plan.verdict);
		shotReport.PenetratedAll = plan.verdict == ShotVerdict.PassedThrough;
		if (plan.projectile != null)
		{
			shotReport.MuzzleVelocity = plan.projectile.combatVelocity;
			shotReport.CoreTypeName = plan.projectile.coreType.ToString();
			if (plan.projectile.definition == null)
			{
				plan.projectile.definition = ProjectileLibrary.Default();
			}
			ProjectileDefinition projectileDefinition = plan.projectile.definition;
			if (projectileDefinition != null)
			{
				shotReport.CaliberMM = projectileDefinition.gunCalibreMM;
				shotReport.RoundId = RoundIdFor(projectileDefinition.name);
			}
		}
		for (int i = 0; i < plan.layers.Count; i++)
		{
			LayerPlaybackData layerPlaybackData = plan.layers[i];
			if (layerPlaybackData == null)
			{
				continue;
			}
			LayerHit item = default(LayerHit);
			item.ArmorTypeName = ((layerPlaybackData.plate != null) ? layerPlaybackData.plate.armorType.ToString() : "");
			item.ThicknessMM = ((layerPlaybackData.plate != null) ? layerPlaybackData.plate.thicknessMM : 0f);
			item.PathLengthMM = layerPlaybackData.tEffMM;
			item.EffectiveAngleDeg = float.NaN;
			item.ImpactVelocity = layerPlaybackData.entryVelocity;
			item.Verdict = Verdict(layerPlaybackData.eventKind);
			item.ResidualVelocity = layerPlaybackData.exitVelocity;
			item.PenetrationDepthMM = layerPlaybackData.outcome.penetrationDepthMM;
			item.ErosionFraction = layerPlaybackData.outcome.erosionFraction;
			shotReport.Layers.Add(item);
		}
		return shotReport;
	}

	public static ShotOutcome Outcome(ShotVerdict v)
	{
		switch (v)
		{
		case ShotVerdict.NoTarget:
			return ShotOutcome.NoTarget;
		case ShotVerdict.PassedThrough:
			return ShotOutcome.PassedThrough;
		case ShotVerdict.Stopped:
			return ShotOutcome.Stopped;
		case ShotVerdict.Ricochet:
			return ShotOutcome.Ricochet;
		case ShotVerdict.Slide:
			return ShotOutcome.Slide;
		case ShotVerdict.Exhausted:
			return ShotOutcome.Exhausted;
		case ShotVerdict.NoModel:
			return ShotOutcome.NoModel;
		default:
			Debug.LogError(string.Format("[ShotReportBuilder] неизвестный ShotVerdict {0} — отчёт получит ", (int)v) + "NoTarget. Добавь ветку сюда и в ShotOutcome: правило достижения иначе прочитает чужой исход.");
			return ShotOutcome.NoTarget;
		}
	}

	public static LayerVerdict Verdict(LayerEventKind k)
	{
		switch (k)
		{
		case LayerEventKind.Penetration:
			return LayerVerdict.Penetrated;
		case LayerEventKind.Stopped:
			return LayerVerdict.Stopped;
		case LayerEventKind.Ricochet:
			return LayerVerdict.Ricochet;
		case LayerEventKind.CriticalRicochet:
			return LayerVerdict.CriticalRicochet;
		case LayerEventKind.Slide:
			return LayerVerdict.Slide;
		default:
			Debug.LogError(string.Format("[ShotReportBuilder] неизвестный LayerEventKind {0} — слой получит ", (int)k) + "Stopped. Добавь ветку сюда и в LayerVerdict.");
			return LayerVerdict.Stopped;
		}
	}

	private static string RoundIdFor(string definitionName)
	{
		if (string.IsNullOrEmpty(definitionName))
		{
			return "";
		}
		for (int i = 0; i < RoundCatalog.All.Count; i++)
		{
			if (RoundCatalog.All[i].displayName == definitionName)
			{
				return RoundCatalog.All[i].id;
			}
		}
		return "";
	}

	public static float MaxCalibreMM()
	{
		float num = 0f;
		for (int i = 0; i < RoundCatalog.All.Count; i++)
		{
			if (num < RoundCatalog.All[i].calibreMM)
			{
				num = RoundCatalog.All[i].calibreMM;
			}
		}
		return num;
	}
}
