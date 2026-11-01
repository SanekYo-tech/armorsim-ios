using System;
using System.Collections.Generic;
using UnityEngine;

public static class UltraBuildAction
{
	public static UltraBuildResult Run(UltraShotEdit shot, IList<UltraPlateEdit> plates, UltraRung rung, float windowOverrideS = 0f)
	{
		UltraBuildResult result = default(UltraBuildResult);
		result = UltraSceneBuild.Prepare(shot, plates, rung, new Func<string, bool>(UltraTimelineCache.Shared.Has), windowOverrideS);
		if (result.status == UltraBuildStatus.Refused)
		{
			Debug.LogWarning("ULTRA сборка отказана: " + Localization.T(result.reasonKey));
			return result;
		}
		AppFlow.PendingUltraBuild = result.projected;
		if (result.status == UltraBuildStatus.Enqueued)
		{
			string key;
			UltraSolveQueue.Instance.Submit(result.projected.scenario, out key);
			Debug.Log("ULTRA сборка: поставлено в очередь [" + key + "], ступень "
				+ string.Format("D/{0:F0}, окно показа ", result.projected.scenario.elementsPerDiameter)
				+ string.Format("{0:F0} мкс, переход в просмотрщик", result.projected.scenario.durationS * 1000000f));
		}
		else if (UltraSceneBuild.NeedsEquivalenceNote(result))
		{
			Debug.Log("ULTRA: " + Localization.T(UltraSceneBuild.KeyEquivalent) + "  " + result.equivalenceLine);
		}
		else
		{
			Debug.Log("ULTRA сборка: эта постановка уже посчитана, берём из кэша");
		}
		if (UltraSceneBuild.NeedsDeclaration(result))
		{
			Debug.Log("ULTRA объявление: " + Localization.T(result.declarationKey) + "  " + result.equivalenceLine);
		}
		AppFlow.OpenUltraView();
		return result;
	}
}
