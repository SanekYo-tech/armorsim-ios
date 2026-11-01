using UnityEngine;

public static class BodyRoute
{
	public const string BodySceneName = "Body";

	public const string KeyNotImplemented = "mode.soon";

	public static BodyEntryAction Decide(SimFamily family, bool implemented, OwnershipState ownership, bool gateOpen, bool demoAvailable)
	{
		switch (family)
		{
		case SimFamily.Analytic3D:
			Debug.LogError("[BodyRoute] Decide спросили о семействе Analytic3D — это маршрут режима BODY. За 3D отвечает UltraSceneRoute; вход отклонён.");
			return BodyEntryAction.BlockedWithReason;
		case SimFamily.Flat2D:
			Debug.LogError("[BodyRoute] Decide спросили о семействе Flat2D — это маршрут режима BODY. За «Плоскость» отвечает Flat2DRoute; вход отклонён.");
			return BodyEntryAction.BlockedWithReason;
		case SimFamily.Body:
			return DecideBody(implemented, ownership, gateOpen, demoAvailable);
		case SimFamily.Aero:
			Debug.LogError("[BodyRoute] Decide спросили о семействе Aero — это маршрут режима BODY. За AeroSim отвечает AeroRoute; вход отклонён.");
			return BodyEntryAction.BlockedWithReason;
		case SimFamily.Radar:
			Debug.LogError("[BodyRoute] Decide спросили о семействе Radar — это маршрут режима BODY. За RADARSIM отвечает RadarRoute; вход отклонён.");
			return BodyEntryAction.BlockedWithReason;
		default:
			Debug.LogError(string.Format("[BodyRoute] Decide: неизвестное семейство {0}; вход ", family) + "отклонён. Значение перечисления завели и не обработали здесь.");
			return BodyEntryAction.BlockedWithReason;
		}
	}

	private static BodyEntryAction DecideBody(bool implemented, OwnershipState ownership, bool gateOpen, bool demoAvailable)
	{
		if (!implemented)
		{
			return BodyEntryAction.BlockedWithReason;
		}
		switch (ownership)
		{
		case OwnershipState.Unknown:
			return BodyEntryAction.BlockedWithReason;
		case OwnershipState.Owned:
			if (!gateOpen)
			{
				return BodyEntryAction.BlockedWithReason;
			}
			return BodyEntryAction.OpenBodyEditor;
		case OwnershipState.NotOwned:
			if (!demoAvailable)
			{
				return BodyEntryAction.BlockedWithReason;
			}
			return BodyEntryAction.OpenBodyDemo;
		default:
			Debug.LogError(string.Format("[BodyRoute] Decide: неизвестное состояние владения {0}; ", ownership) + "вход отклонён.");
			return BodyEntryAction.BlockedWithReason;
		}
	}

	public static string ReasonKeyFor(bool implemented, OwnershipState ownership, bool gateOpen, bool demoAvailable, string gateReasonKey)
	{
		if (!implemented)
		{
			return KeyNotImplemented;
		}
		switch (ownership)
		{
		case OwnershipState.Unknown:
			return "dlc.unverified";
		case OwnershipState.Owned:
			if (!gateOpen)
			{
				return gateReasonKey;
			}
			return null;
		case OwnershipState.NotOwned:
			if (!demoAvailable)
			{
				return "dlc.not_owned";
			}
			return null;
		default:
			Debug.LogError("[BodyRoute] ReasonKeyFor: неизвестное состояние владения " + string.Format("{0}; названо «не проверено».", ownership));
			return "dlc.unverified";
		}
	}

	public static string SceneNameFor(SimFamily family, BodyStage stage, string sandboxSceneName)
	{
		if (stage < BodyStage.Draw || stage > BodyStage.View)
		{
			Debug.LogError(string.Format("[BodyRoute] SceneNameFor: неизвестная стадия {0}; ", stage) + "сцена не названа.");
			return null;
		}
		switch (family)
		{
		case SimFamily.Analytic3D:
		case SimFamily.Flat2D:
		case SimFamily.Aero:
		case SimFamily.Radar:
			return null;
		case SimFamily.Body:
			return BodySceneName;
		default:
			Debug.LogError(string.Format("[BodyRoute] SceneNameFor: неизвестное семейство {0}; ", family) + "сцена не названа.");
			return null;
		}
	}
}
