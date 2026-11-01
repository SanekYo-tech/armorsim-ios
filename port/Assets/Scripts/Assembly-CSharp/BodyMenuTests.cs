using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class BodyMenuTests : MonoBehaviour
{
	private sealed class StubSource : IEntitlementSource
	{
		private readonly OwnershipState _answer;

		public string Name => null;

		public StubSource(OwnershipState answer)
		{
		}

		public OwnershipState Query(ContentPack pack)
		{
			return OwnershipState.Unknown;
		}
	}

	private sealed class PackSource : IEntitlementSource
	{
		private readonly ContentPack _pack;

		private readonly OwnershipState _yes;

		private readonly OwnershipState _no;

		public string Name => null;

		public PackSource(ContentPack pack, OwnershipState yes, OwnershipState no)
		{
		}

		public OwnershipState Query(ContentPack pack)
		{
			return OwnershipState.Unknown;
		}
	}

	private const string KeySoon = "mode.soon";

	private const string KeyBadge = "dlc.badge";

	private const string KeyNotOwnedHint = "dlc.not_owned_hint";

	private const string KeyUnverified = "dlc.unverified";

	private const string KeyStore = "dlc.open_store";

	private const string KeyDemo = "dlc.show_demo";

	private const string KeyTitle = "mode.body";

	private const string KeyDesc = "mode.body_desc";

	private const string GateProbe = "body.gate.probe";

	private const string ProbeFlatUrl = "probe://store/flat2d";

	private const string ProbeBodyUrl = "probe://store/body";

	private static readonly (SimMode mode, bool unlocked)[] UnlockIntent;

	public static bool SabotageUnlockTable;

	private const string MarkBegin = "НАЧАЛО КОДА КАРТОЧКИ BODY";

	private const string MarkEnd = "КОНЕЦ КОДА КАРТОЧКИ BODY";

	private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

	[ContextMenu("★ Прогнать все проверки меню BODY")]
	public void RunAll()
	{
	}

	public static bool CheckAll()
	{
		return false;
	}

	private static bool FiveStates()
	{
		return false;
	}

	private static bool FactsComeFromRealSources()
	{
		return false;
	}

	private static bool State(string title, MainMenuUI ui, Transform parent, MainMenuUI.BodyFacts f, bool enterable, string badge, string reason, string[] buttons)
	{
		return false;
	}

	private static bool OrderOfQuestions()
	{
		return false;
	}

	private static bool ExclusiveSelection()
	{
		return false;
	}

	private static Button ReplaceWithLive(MainMenuUI ui, Transform column, string field)
	{
		return null;
	}

	private static bool NoSectionButtons()
	{
		return false;
	}

	private static bool ShellParity()
	{
		return false;
	}

	private static bool SelectionColours(MainMenuUI ui, string title, SimFamily family, Button wantSelected, Button other1, Button other2)
	{
		return false;
	}

	private static bool UnlockTableAgrees()
	{
		return false;
	}

	private static bool ExistingColumnsUnmoved()
	{
		return false;
	}

	private static bool Row3D(Transform column, string cardName, string badge, bool wantSections)
	{
		return false;
	}

	private static bool Contrafactual()
	{
		return false;
	}

	private static bool NoQualityRungs()
	{
		return false;
	}

	private static bool TwoStoreUrls()
	{
		return false;
	}

	private static MainMenuUI.BodyFacts Facts(bool implemented, OwnershipState own, bool gateOpen, string gateReason, bool demo)
	{
		return default;
	}

	private static MainMenuUI.FlatFacts FlatFacts(bool implemented, OwnershipState own, bool gateOpen, string gateReason, bool demo)
	{
		return default;
	}

	private static MainMenuUI.BodyFacts[] AllStates()
	{
		return null;
	}

	private static MainMenuUI NewHost(out GameObject go)
	{
		go = null;
		return null;
	}

	private static MainMenuUI NewHostBuilt(out GameObject go)
	{
		go = null;
		return null;
	}

	private static void Kill(GameObject go)
	{
	}

	private static Transform FindDeep(Transform root, string name)
	{
		return null;
	}

	private static string KeyOfChild(Button card, string childName)
	{
		return null;
	}

	private static Button ChildButton(Button card, string childName)
	{
		return null;
	}

	private static List<string> ButtonKeys(Button card)
	{
		return null;
	}

	private static Color Bg(Button b)
	{
		return default;
	}

	private static Button Card3D(MainMenuUI ui, SimMode mode)
	{
		return null;
	}

	private static int CountSelected3D(MainMenuUI ui)
	{
		return 0;
	}

	private static bool SameSet(List<string> got, string[] want)
	{
		return false;
	}

	private static bool SameOrder(List<string> got, string[] want)
	{
		return false;
	}

	private static string Show(string s)
	{
		return null;
	}

	private static int Count(string text, string needle)
	{
		return 0;
	}

	private static string ProjectRoot()
	{
		return null;
	}

	private static object Invoke(Type t, object target, string name)
	{
		return null;
	}

	private static object Field(object target, string name)
	{
		return null;
	}

	private static void SetField(object target, string name, object value)
	{
	}

	private static void SetStatic(Type t, string name, object value)
	{
	}
}
