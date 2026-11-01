using System.Collections.Generic;
using UnityEngine;

public class Flat2DRoutingTests : MonoBehaviour
{
	private const string SandboxProbe = "SampleScene";

	private static readonly string[] Table;

	private static int _errors;

	private static bool _counting;

	[ContextMenu("★ Прогнать все проверки маршрута 2D")]
	public void RunAll()
	{
	}

	public static bool CheckAll()
	{
		return false;
	}

	private static bool FullTable()
	{
		return false;
	}

	private static bool SceneIsolation()
	{
		return false;
	}

	private static bool WrongRouterRefused()
	{
		return false;
	}

	private static bool OnlyOwnedOpensEditor()
	{
		return false;
	}

	private static bool Contrafactual()
	{
		return false;
	}

	private static bool LoudDefaultFires()
	{
		return false;
	}

	private static void OnLog(string condition, string stackTrace, LogType type)
	{
	}

	private static void BeginCountingErrors()
	{
	}

	private static int ErrorsSoFar()
	{
		return 0;
	}

	private static int EndCountingErrors()
	{
		return 0;
	}

	private static Dictionary<string, FlatEntryAction> ParseTable()
	{
		return null;
	}

	private static string Key(SimFamily f, bool impl, OwnershipState own, bool gate, bool demo)
	{
		return null;
	}

	private static string Show(string s)
	{
		return null;
	}
}
