using System;
using System.Collections.Generic;
using UnityEngine;

public static class Entitlements
{
	public const string KeyNotOwned = "dlc.not_owned";

	public const string KeyUnverified = "dlc.unverified";

	private static readonly Dictionary<ContentPack, OwnershipState> _cache;

	private static IEntitlementSource _source;

	private static string _sourceName;

	static Entitlements()
	{
		_cache = new Dictionary<ContentPack, OwnershipState>();
		_source = new NullEntitlementSource();
		_sourceName = "none";
	}

	public static string SourceName => _sourceName;

	public static void Install(IEntitlementSource source)
	{
		if (source == null)
		{
			source = new NullEntitlementSource();
		}
		_source = source;
		try
		{
			_sourceName = _source.Name ?? "?";
		}
		catch (Exception ex)
		{
			_sourceName = "?";
			Debug.LogError(string.Format("[Entitlements] источник бросил исключение на Name: {0}", ex));
		}
		_cache.Clear();
	}

	public static OwnershipState State(ContentPack pack)
	{
		if (_cache.TryGetValue(pack, out OwnershipState state))
		{
			return state;
		}
		try
		{
			state = _source.Query(pack);
		}
		catch (Exception ex)
		{
			state = OwnershipState.Unknown;
			Debug.LogError("[Entitlements] источник «" + _sourceName + "» бросил исключение на " + string.Format("Query({0}); владение считается непроверенным: {1}", pack, ex));
		}
		_cache[pack] = state;
		return state;
	}

	public static bool Has(ContentPack pack)
	{
		return State(pack) == OwnershipState.Owned;
	}

	public static void Refresh()
	{
		_cache.Clear();
	}

	public static string ReasonKey(ContentPack pack)
	{
		OwnershipState ownershipState = State(pack);
		switch (ownershipState)
		{
		case OwnershipState.Owned:
			return null;
		case OwnershipState.NotOwned:
			return KeyNotOwned;
		case OwnershipState.Unknown:
			return KeyUnverified;
		default:
			Debug.LogError(string.Format("[Entitlements] неизвестное состояние владения {0} для {1}; ", ownershipState, pack) + "считается непроверенным.");
			return KeyUnverified;
		}
	}
}
