using System.Collections.Generic;
using UnityEngine;

public static class BodyFingerprint
{
	private const ulong Offset = 14695981039346656037uL;

	private const ulong Prime = 1099511628211uL;

	public const float Quantum = 0.0001f;

	private static ulong Mix(ulong h, ulong v)
	{
		h ^= v;
		h *= Prime;
		h ^= v >> 8;
		h *= Prime;
		h ^= v >> 16;
		h *= Prime;
		h ^= v >> 24;
		h *= Prime;
		h ^= v >> 32;
		h *= Prime;
		h ^= v >> 40;
		h *= Prime;
		h ^= v >> 48;
		h *= Prime;
		h ^= v >> 56;
		h *= Prime;
		return h;
	}

	private static ulong Mix(ulong h, string s)
	{
		if (s == null)
		{
			return h * 0x1EFAC7090AEF4A21uL;
		}
		for (int i = 0; i < s.Length; i++)
		{
			h ^= s[i];
			h *= Prime;
		}
		ulong length = (ulong)s.Length;
		for (int j = 0; j < 8; j++)
		{
			h ^= length >> (j * 8);
			h *= Prime;
		}
		return h;
	}

	private static ulong Q(float v)
	{
		return (ulong)Mathf.Round(v / Quantum);
	}

	public static ulong Of(IReadOnlyList<TraceSegment> segments)
	{
		ulong h = Offset;
		if (segments == null)
		{
			return h;
		}
		for (int i = 0; i < segments.Count; i++)
		{
			TraceSegment segment = segments[i];
			h = Mix(h, (ulong)segment.Kind);
			h = Mix(h, segment.Material != null ? segment.Material.Id : "<void>");
			h = Mix(h, Q(segment.LengthMm));
			h = Mix(h, Q(segment.ObliquityDeg));
			h = Mix(h, (ulong)segment.OwnerPlacementIndex);
		}
		return h;
	}
}
