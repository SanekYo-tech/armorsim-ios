using System.Collections.Generic;

public static class BodyFingerprint
{
	private const ulong Offset = 14695981039346656037uL;

	private const ulong Prime = 1099511628211uL;

	public const float Quantum = 0.0001f;

	private static ulong Mix(ulong h, ulong v)
	{
		return 0uL;
	}

	private static ulong Mix(ulong h, string s)
	{
		return 0uL;
	}

	private static ulong Q(float v)
	{
		return 0uL;
	}

	public static ulong Of(IReadOnlyList<TraceSegment> segments)
	{
		return 0uL;
	}
}
