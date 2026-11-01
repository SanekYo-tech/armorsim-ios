using UnityEngine;

public class AeroRoutingTests : MonoBehaviour
{
	private sealed class PackProbe : IEntitlementSource
	{
		public OwnershipState AeroState;

		public int AeroQueries;

		public int OtherQueries;

		public string Name => null;

		public OwnershipState Query(ContentPack pack)
		{
			return OwnershipState.Unknown;
		}
	}

	public static bool CheckAll()
	{
		return false;
	}

	private static bool Routes()
	{
		return false;
	}

	private static bool Sources()
	{
		return false;
	}

	private static bool FreeWindow()
	{
		return false;
	}

	private static bool FreeRoutes()
	{
		return false;
	}

	private static bool Languages()
	{
		return false;
	}
}
