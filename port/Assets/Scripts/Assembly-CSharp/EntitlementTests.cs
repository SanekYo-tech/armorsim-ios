using UnityEngine;

public class EntitlementTests : MonoBehaviour
{
	private sealed class FixedSource : IEntitlementSource
	{
		private readonly OwnershipState _answer;

		public string Name => null;

		public FixedSource(OwnershipState answer)
		{
		}

		public OwnershipState Query(ContentPack pack)
		{
			return OwnershipState.Unknown;
		}
	}

	private sealed class CountingSource : IEntitlementSource
	{
		private readonly OwnershipState _answer;

		public int Calls;

		public string Name => null;

		public CountingSource(OwnershipState answer)
		{
		}

		public OwnershipState Query(ContentPack pack)
		{
			return OwnershipState.Unknown;
		}
	}

	private sealed class DefaultSource : IEntitlementSource
	{
		public string Name => null;

		public OwnershipState Query(ContentPack pack)
		{
			return OwnershipState.Unknown;
		}
	}

	private sealed class ThrowingSource : IEntitlementSource
	{
		public string Name => null;

		public OwnershipState Query(ContentPack pack)
		{
			return OwnershipState.Unknown;
		}
	}

	[ContextMenu("★ Прогнать все проверки владения")]
	public void RunAll()
	{
	}

	public static bool CheckAll()
	{
		return false;
	}

	private static bool Row(ContentPack pack, OwnershipState wantState, bool wantHas, string wantKey)
	{
		return false;
	}

	private static string Show(string s)
	{
		return null;
	}

	private static bool NeverInstalled()
	{
		return false;
	}

	private static bool ThrowingSourceFailsClosed()
	{
		return false;
	}

	private static bool NotOwnedIsRefused()
	{
		return false;
	}

	private static bool OwnedIsAllowed()
	{
		return false;
	}

	private static bool CacheAndRefresh()
	{
		return false;
	}

	private static bool ZeroMeansRefusal()
	{
		return false;
	}
}
