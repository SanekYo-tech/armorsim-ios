using System;
using UnityEngine;

public class CatalogTests : MonoBehaviour
{
	[ContextMenu("★ Прогнать все тесты реестра")]
	public void RunAll()
	{
	}

	public bool RunAllCounted(out int pass, out int total)
	{
		pass = default;
		total = default;
		return false;
	}

	[ContextMenu("Тест 1 — полнота реестра")]
	public void Test1()
	{
	}

	[ContextMenu("Тест 2 — фальсифицируемость проверки полноты")]
	public void Test2()
	{
	}

	[ContextMenu("Тест 3 — эквивалентность выбора")]
	public void Test3()
	{
	}

	[ContextMenu("Тест 4 — категории и подсказки")]
	public void Test4()
	{
	}

	[ContextMenu("Тест 5 — выстрел в текстолит")]
	public void Test5()
	{
	}

	[ContextMenu("Тест 6 — варианты НДЗ")]
	public void Test6()
	{
	}

	[ContextMenu("Тест 7 — вариант доезжает до объекта")]
	public void Test7()
	{
	}

	[ContextMenu("Тест 8 — варианты ДЗ опознаются свой в свой")]
	public void Test8()
	{
	}

	private bool Test1Impl()
	{
		return false;
	}

	private bool Test2Impl()
	{
		return false;
	}

	private bool Test3Impl()
	{
		return false;
	}

	private bool Test4Impl()
	{
		return false;
	}

	private bool Test5Impl()
	{
		return false;
	}

	private static bool Test6Impl()
	{
		return false;
	}

	private bool Test7Impl()
	{
		return false;
	}

	private bool Test8Impl()
	{
		return false;
	}

	private static NeraSpec SpecOf(NeraPreset p)
	{
		return default;
	}

	private static bool PresetRatio(string id, float want)
	{
		return false;
	}

	private static int BitsOf(float v)
	{
		return 0;
	}

	private void RunOne(int n, string title, Func<bool> body, ref int pass, ref int total)
	{
	}

	private static bool Finite(string what, float value)
	{
		return false;
	}

	private static bool Check<T>(string what, T got, T want)
	{
		return false;
	}

	private static bool CheckNear(string what, float got, float want, float tol)
	{
		return false;
	}

	private static bool Contains(string[] arr, string want)
	{
		return false;
	}

	private static bool NonEmptyCategory(CatalogEntry[] entries, string category)
	{
		return false;
	}
}
