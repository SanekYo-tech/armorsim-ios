using UnityEngine;
using UnityEngine.UI;

public class WorkshopTests : MonoBehaviour
{
	private static readonly string[] Keys;

	[ContextMenu("★ Прогнать все проверки мастерской")]
	public void RunAll()
	{
	}

	public static bool CheckAll()
	{
		return false;
	}

	private static bool PackageVersion()
	{
		return false;
	}

	private static bool FamilyAgreement()
	{
		return false;
	}

	private static bool PayloadVersion()
	{
		return false;
	}

	private static bool DiskRoundTrip()
	{
		return false;
	}

	private static bool ImportStates()
	{
		return false;
	}

	private static bool PublishDecision()
	{
		return false;
	}

	private static bool Strings()
	{
		return false;
	}

	private static bool SameGateAsSaves()
	{
		return false;
	}

	private static bool LocalBackendRefuses()
	{
		return false;
	}

	private static bool PreviewPath()
	{
		return false;
	}

	private static bool Refused(string what, WorkshopReadResult r, WorkshopReadStatus want)
	{
		return false;
	}

	private static bool Import(string what, WorkshopItem item, WorkshopImportAction want, string wantReason)
	{
		return false;
	}

	private static bool Publish(string what, SaveEntry entry, bool canPublish, string link, WorkshopPublishAction want, string wantReason)
	{
		return false;
	}

	private static SaveEntry Entry(SimFamily family, string name, int objects)
	{
		return null;
	}

	private static WorkshopManifest Manifest(SimFamily family, int formatVersion)
	{
		return null;
	}

	private static WorkshopItem ReadyItem(SimFamily family)
	{
		return null;
	}

	private static void Kill(GameObject go)
	{
	}

	private static string KeyOfChild(Transform row, string childName)
	{
		return null;
	}

	private static Button ChildButton(Transform row, string name)
	{
		return null;
	}

	private static string Show(string s)
	{
		return null;
	}
}
