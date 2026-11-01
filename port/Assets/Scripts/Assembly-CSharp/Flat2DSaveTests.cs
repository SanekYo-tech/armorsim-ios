using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Flat2DSaveTests : MonoBehaviour
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

	private const string KeySaveLocked = "dlc.save_locked";

	private const string KeyUnverified = "dlc.unverified";

	private const string KeySoon = "mode.soon";

	private static int _errors;

	[ContextMenu("★ Прогнать все проверки сохранений 2D")]
	public void RunAll()
	{
	}

	public static bool CheckAll()
	{
		return false;
	}

	private static bool OldFileStillLoads()
	{
		return false;
	}

	private static bool RealFilesOnDisk()
	{
		return false;
	}

	private static bool NewFileRoundTrips()
	{
		return false;
	}

	private static bool RowTable()
	{
		return false;
	}

	private static bool Row(MainMenuUI ui, GameObject host, string title, SimFamily family, bool implemented, OwnershipState own, bool gateOpen, string wantReason)
	{
		return false;
	}

	private static bool ReasonFollowsSource()
	{
		return false;
	}

	private static bool OneSource(MainMenuUI ui, GameObject host, OwnershipState install, string wantReason)
	{
		return false;
	}

	private static bool DeleteNeverDisabled()
	{
		return false;
	}

	private static bool Contrafactual()
	{
		return false;
	}

	private static bool FamilySurvivesCreate()
	{
		return false;
	}

	private static bool CreateHasNoWayToSkipFamily()
	{
		return false;
	}

	private static bool CreatePair(List<string> made, SimFamily family, TargetSection section)
	{
		return false;
	}

	private static bool NoSectionChipOnFlatRow()
	{
		return false;
	}

	private static bool LoadUsesFileNotSettings()
	{
		return false;
	}

	private static MainMenuUI.FlatFacts Facts(bool implemented, OwnershipState own, bool gateOpen, string gateReason)
	{
		return default;
	}

	private static SaveEntry Entry(SimFamily family, string name)
	{
		return null;
	}

	private static SceneFile MinimalFile()
	{
		return null;
	}

	private static string StripMember(string json, string member)
	{
		return null;
	}

	private static MainMenuUI NewHost(out GameObject go)
	{
		go = null;
		return null;
	}

	private static void Kill(GameObject go)
	{
	}

	private static string KeyOfChild(Transform row, string childName)
	{
		return null;
	}

	private static string RawText(Transform row, string childName)
	{
		return null;
	}

	private static Button ChildButton(Transform row, string name)
	{
		return null;
	}

	private static string Show(string s)
	{
		return null;
	}

	private static void OnLog(string condition, string stack, LogType type)
	{
	}

	private static void BeginCount()
	{
	}

	private static int EndCount()
	{
		return 0;
	}
}
