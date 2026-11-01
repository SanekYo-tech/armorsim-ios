using System;
using UnityEngine;

public static class RadarScenarioWideMapTests
{
	public static void RunAll()
	{
	}

	private static void Check(string name, Action run, ref int passed)
	{
	}

	private static void Require(bool test, string message)
	{
	}

	private static void Near(float actual, float expected, string message)
	{
	}

	private static void Near(Vector2 actual, Vector2 expected, string message)
	{
	}

	private static void Bounds()
	{
	}

	private static void Fit()
	{
	}

	private static void Projection()
	{
	}

	private static void Terrain()
	{
	}

	private static RadarScenarioData Legacy(float size)
	{
		return null;
	}

	private static void Draft(Action<string> action)
	{
	}

	private static void Write(string path, int version, RadarScenarioData data)
	{
	}

	private static void PreservedFields(RadarScenarioData data)
	{
	}

	private static void VersionOne()
	{
	}

	private static void VersionTwo()
	{
	}

	private static void VersionThree()
	{
	}

	private static void RejectMalformed()
	{
	}

	private static void MustReject(string path, string message)
	{
	}
}
