using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AeroMenuTests : MonoBehaviour
{
	internal sealed class Fixture : IDisposable
	{
		internal GameObject Host;

		internal MainMenuUI UI;

		internal Canvas Canvas;

		internal ScrollRect Scroll;

		internal RectTransform Start;

		internal EventSystem Events;

		internal StandaloneInputModule Module;

		internal AeroMenuTestInput Input;

		private readonly TMP_FontAsset savedFont;

		private readonly EventSystem savedEvents;

		private readonly CursorLockMode savedCursorLock;

		private readonly List<GraphicRaycaster> disabledRaycasters;

		internal Fixture(bool freezeFreeAccessClock = true)
		{
		}

		internal RectTransform Card(string name)
		{
			return null;
		}

		internal void Rebuild()
		{
		}

		internal bool CheckReference()
		{
			return false;
		}

		internal List<RaycastResult> Raycast(Vector2 point)
		{
			return null;
		}

		public void Dispose()
		{
		}
	}

	private static readonly string[] ExistingNames;

	private static readonly string[] VisibleNames;

	public static Action<ScrollRect> SceneMutation;

	private static Fixture preparedFixture;

	private static readonly AeroFacts[] StateCases;

	private static readonly string[] StateBadges;

	private static readonly string[] StateReasons;

	private static readonly string[][] StateButtons;

	public static bool CheckScroll()
	{
		return false;
	}

	public static bool CheckAR10()
	{
		return false;
	}

	public static bool CheckAR11()
	{
		return false;
	}

	public static bool CheckAR12()
	{
		return false;
	}

	public static bool CheckAR13()
	{
		return false;
	}

	public static bool CheckAR15()
	{
		return false;
	}

	public static void PrepareRenderedFixture()
	{
	}

	public static void DisposePreparedFixture()
	{
	}

	private static Fixture TakeFixture()
	{
		return null;
	}

	public static bool CheckAR1()
	{
		return false;
	}

	public static bool CheckAR2()
	{
		return false;
	}

	private static Button InstallLiveAero(Fixture fixture)
	{
		return null;
	}

	private static void InstallLiveSiblings(Fixture fixture)
	{
	}

	public static bool CheckAR3()
	{
		return false;
	}

	private static bool Selection(Fixture fixture, string wanted)
	{
		return false;
	}

	public static bool CheckAR4()
	{
		return false;
	}

	public static bool CheckAR5()
	{
		return false;
	}

	public static bool CheckAR6()
	{
		return false;
	}

	public static bool CheckAR7()
	{
		return false;
	}

	public static bool CheckAR8()
	{
		return false;
	}

	public static bool CheckAR9()
	{
		return false;
	}

	public static bool CheckAR14()
	{
		return false;
	}

	private static bool CheckOrder(Fixture fixture)
	{
		return false;
	}

	private static string ChildKey(Button card, string name)
	{
		return null;
	}

	private static bool AssertRow(string label, Button card, bool enterable, string badge, string reason, string[] buttons)
	{
		return false;
	}

	private static bool Close(string label, float actual, float expected)
	{
		return false;
	}

	private static bool Contains(Rect outer, Rect inner)
	{
		return false;
	}

	private static Rect Intersection(Rect a, Rect b)
	{
		return default;
	}

	private static object Field(MainMenuUI ui, string name)
	{
		return null;
	}

	private static void SetField(MainMenuUI ui, string name, object value)
	{
	}

	public static bool MeasureBaseline()
	{
		return false;
	}

	private static object Invoke(MainMenuUI ui, string method)
	{
		return null;
	}

	private static Transform Find(Transform root, string name)
	{
		return null;
	}

	private static Rect World(RectTransform transform)
	{
		return default;
	}

	private static string Show(Rect rect)
	{
		return null;
	}
}
