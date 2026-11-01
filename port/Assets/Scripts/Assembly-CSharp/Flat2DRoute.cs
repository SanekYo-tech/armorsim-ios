using UnityEngine;

public static class Flat2DRoute
{
	public const string FlatSceneName = "Flat2D";

	public const float CoarseDxM = 0.0005f;

	public const float FineDxM = 0.00028867513f;

	public static FlatEntryAction Decide(SimFamily family, bool implemented, OwnershipState ownership, bool gateOpen, bool demoAvailable, bool freeAccess = false)
	{
		switch (family)
		{
		case SimFamily.Analytic3D:
			Debug.LogError("[Flat2DRoute] Decide спросили о семействе Analytic3D — это маршрут режима «Плоскость». За 3D отвечает UltraSceneRoute; вход отклонён.");
			return FlatEntryAction.BlockedWithReason;
		case SimFamily.Flat2D:
			return DecideFlat(implemented, ownership, gateOpen, demoAvailable, freeAccess);
		case SimFamily.Aero:
			Debug.LogError("[Flat2DRoute] Decide спросили о семействе Aero — это маршрут режима «Плоскость». За AeroSim отвечает AeroRoute; вход отклонён.");
			return FlatEntryAction.BlockedWithReason;
		case SimFamily.Radar:
			Debug.LogError("[Flat2DRoute] Decide спросили о семействе Radar — это маршрут режима «Плоскость». За RADARSIM отвечает RadarRoute; вход отклонён.");
			return FlatEntryAction.BlockedWithReason;
		default:
			Debug.LogError(string.Format("[Flat2DRoute] Decide: неизвестное семейство {0}; вход ", family) + "отклонён. Значение перечисления завели и не обработали здесь.");
			return FlatEntryAction.BlockedWithReason;
		}
	}

	private static FlatEntryAction DecideFlat(bool implemented, OwnershipState ownership, bool gateOpen, bool demoAvailable, bool freeAccess)
	{
		if (!implemented)
		{
			return FlatEntryAction.BlockedWithReason;
		}
		if (freeAccess && ownership != OwnershipState.Owned)
		{
			return gateOpen ? FlatEntryAction.OpenFlatEditor : FlatEntryAction.BlockedWithReason;
		}
		switch (ownership)
		{
		case OwnershipState.Unknown:
			return FlatEntryAction.BlockedWithReason;
		case OwnershipState.Owned:
			return gateOpen ? FlatEntryAction.OpenFlatEditor : FlatEntryAction.BlockedWithReason;
		case OwnershipState.NotOwned:
			return demoAvailable ? FlatEntryAction.OpenFlatDemo : FlatEntryAction.BlockedWithReason;
		default:
			Debug.LogError(string.Format("[Flat2DRoute] Decide: неизвестное состояние владения {0}; ", ownership) + "вход отклонён.");
			return FlatEntryAction.BlockedWithReason;
		}
	}

	public static string SceneNameFor(SimFamily family, FlatStage stage, string sandboxSceneName)
	{
		if (family == SimFamily.Flat2D)
		{
			return FlatSceneName;
		}
		if (family != SimFamily.Analytic3D && family != SimFamily.Aero && family != SimFamily.Radar)
		{
			Debug.LogError(string.Format("[Flat2DRoute] SceneNameFor: неизвестное семейство {0}; ", family) + "сцена не названа.");
		}
		return null;
	}

	public static float DxFor(FlatQuality quality)
	{
		switch (quality)
		{
		case FlatQuality.Coarse:
			return CoarseDxM;
		case FlatQuality.Fine:
			return FineDxM;
		default:
			Debug.LogError(string.Format("[Flat2DRoute] DxFor: неизвестная ступень {0}; шаг не назван. ", quality) + "Значение перечисления завели и не обработали здесь.");
			return 0f;
		}
	}
}
