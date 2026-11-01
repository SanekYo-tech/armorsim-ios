using System;
using System.Collections.Generic;

public static class UltraSceneBuild
{
	public const string KeyEquivalent = "ultra.cache.equivalent";

	public static UltraBuildResult Prepare(UltraShotEdit shot, IList<UltraPlateEdit> plates, UltraRung rung, Func<string, bool> isCached, float windowOverrideS = 0f)
	{
		UltraBuildResult result = default(UltraBuildResult);
		result.layout = UltraProjection.Layout(shot, plates, out string reason);
		ProjectedScenario projected = UltraProjection.Project(shot, plates, rung, out string reason2, windowOverrideS);
		if (projected == null)
		{
			result.status = UltraBuildStatus.Refused;
			result.reasonKey = reason2 ?? reason;
			return result;
		}
		result.projected = projected;
		result.declarationKey = projected.declarationKey;
		result.equivalenceLine = UltraProjection.EquivalenceLine(projected);
		result.cacheKey = UltraCacheKey.Compute(projected.scenario);
		if (result.cacheKey != null && isCached != null && isCached(result.cacheKey))
		{
			result.status = (projected.isLossy ? UltraBuildStatus.CacheHitByEquivalence : UltraBuildStatus.CacheHit);
		}
		return result;
	}

	public static bool NeedsEquivalenceNote(UltraBuildResult r)
	{
		return r.status == UltraBuildStatus.CacheHitByEquivalence;
	}

	public static bool NeedsDeclaration(UltraBuildResult r)
	{
		if (r.projected == null)
		{
			return false;
		}
		if (r.projected.isLossy)
		{
			return true;
		}
		return r.projected.layerCount > 1;
	}

	public static bool NeedsDeclaration(ProjectedScenario p)
	{
		if (p == null)
		{
			return false;
		}
		if (p.isLossy)
		{
			return true;
		}
		return p.layerCount > 1;
	}
}
