using System.Diagnostics;

public static class RadarMapRouteHooks
{
	[Conditional("UNITY_EDITOR")]
	[Conditional("DEVELOPMENT_BUILD")]
	public static void Physical()
	{
	}

	[Conditional("UNITY_EDITOR")]
	[Conditional("DEVELOPMENT_BUILD")]
	public static void ArcadeVisibility()
	{
	}
}
