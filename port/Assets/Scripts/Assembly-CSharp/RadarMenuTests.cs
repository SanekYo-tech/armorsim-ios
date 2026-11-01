using System;
using UnityEngine;
using UnityEngine.UI;

public class RadarMenuTests : MonoBehaviour
{
	private sealed class StateRow
	{
		internal readonly string Name;

		internal readonly RadarFacts Facts;

		internal readonly bool Enterable;

		internal readonly bool Free;

		internal readonly string Badge;

		internal readonly string Reason;

		internal readonly string[] Buttons;

		internal StateRow(string name, RadarFacts facts, bool enterable, string badge, string reason, bool free, params string[] buttons)
		{
		}
	}

	private struct BaselineRect
	{
		internal string Name;

		internal float Left;

		internal float Right;

		internal float Top;

		internal float Bottom;

		internal float Height;

		internal float Width;

		internal BaselineRect(string name, float left, float right, float top, float bottom, float height, float width)
		{
			Name = null;
			Left = 0f;
			Right = 0f;
			Top = 0f;
			Bottom = 0f;
			Height = 0f;
			Width = 0f;
		}
	}

	private static AeroMenuTests.Fixture preparedFixture;

	private static readonly StateRow[] StateRows;

	private static readonly BaselineRect[] Baseline;

	[ContextMenu("★ Прогнать все проверки меню RADARSIM")]
	public void RunAll()
	{
	}

	public static bool CheckAll()
	{
		return false;
	}

	public static void PrepareRenderedFixture()
	{
	}

	public static void DisposePreparedFixture()
	{
	}

	private static AeroMenuTests.Fixture TakeFixture()
	{
		return null;
	}

	private static RadarFacts Facts(bool implemented, OwnershipState ownership, bool gateOpen, bool freeAccess = false, bool demoAvailable = false, string gateReason = null)
	{
		return default;
	}

	public static bool CheckRM1()
	{
		return false;
	}

	public static bool CheckRM2()
	{
		return false;
	}

	private static Button Replace(AeroMenuTests.Fixture fixture, string name, string field, Func<Button> build)
	{
		return null;
	}

	private static Button InstallLiveCards(AeroMenuTests.Fixture fixture)
	{
		return null;
	}

	public static bool CheckRM3()
	{
		return false;
	}

	private static bool Selection(AeroMenuTests.Fixture fixture, string wanted, SimFamily family)
	{
		return false;
	}

	public static bool CheckRM4()
	{
		return false;
	}

	public static bool CheckRM5()
	{
		return false;
	}

	public static bool CheckRM6()
	{
		return false;
	}

	public static bool CheckRM6b()
	{
		return false;
	}

	public static bool CheckRM7()
	{
		return false;
	}

	public static bool CheckRM8()
	{
		return false;
	}

	public static bool CheckRM9()
	{
		return false;
	}

	public static bool CheckRM10()
	{
		return false;
	}

	private static string ChildKey(Button card, string name)
	{
		return null;
	}

	private static bool AssertRow(string label, Button card, bool enterable, string badge, string reason, bool free, params string[] buttons)
	{
		return false;
	}

	private static object Field(MainMenuUI ui, string name)
	{
		return null;
	}

	private static void SetField(MainMenuUI ui, string name, object value)
	{
	}

	private static void Invoke(MainMenuUI ui, string method)
	{
	}

	private static Rect World(RectTransform rect)
	{
		return default;
	}

	private static bool Close(string name, float actual, float expected)
	{
		return false;
	}

	private static string Show(Rect rect)
	{
		return null;
	}
}
