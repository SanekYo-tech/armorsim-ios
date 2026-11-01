using System;
using UnityEngine;

public static class WorkshopService
{
	private static IWorkshopBackend _backend;

	public static IWorkshopBackend Backend
	{
		get
		{
			if (_backend == null)
			{
				Install(MakeDefaultBackend());
			}
			return _backend;
		}
	}

	public static void Install(IWorkshopBackend backend)
	{
		if (backend == null)
		{
			backend = MakeDefaultBackend();
		}
		_backend = backend;
		Debug.Log("[Мастерская] приёмник: " + _backend.Name);
	}

	private static IWorkshopBackend MakeDefaultBackend()
	{
#if !UNITY_IOS && UNITY_STANDALONE_WIN
		return new SteamWorkshopBackend();
#else
		return new LocalWorkshopBackend();
#endif
	}

	public static WorkshopImportAction DecideImport(WorkshopItem item, out string reasonKey)
	{
		reasonKey = null;
		if (item == null)
		{
			reasonKey = "workshop.reject.malformed";
			return WorkshopImportAction.Refuse;
		}
		if (item.downloading)
		{
			reasonKey = "workshop.state.downloading";
			return WorkshopImportAction.Refuse;
		}
		if (!item.installed)
		{
			reasonKey = "workshop.reject.no_files";
			return WorkshopImportAction.Refuse;
		}
		if (item.reasonKey != null)
		{
			reasonKey = item.reasonKey;
			return WorkshopImportAction.Refuse;
		}
		if (item.manifest == null)
		{
			reasonKey = "workshop.reject.no_manifest";
			return WorkshopImportAction.Refuse;
		}
		return WorkshopImportAction.Import;
	}

	public static WorkshopPublishAction DecidePublish(SaveEntry entry, bool canPublish, string linkedItemId, out string reasonKey)
	{
		reasonKey = null;
		if (entry == null || string.IsNullOrEmpty(entry.payload))
		{
			reasonKey = "workshop.reject.nothing";
			return WorkshopPublishAction.Refuse;
		}
		if (entry.isPreset)
		{
			reasonKey = "workshop.reject.preset";
			return WorkshopPublishAction.Refuse;
		}
		if (entry.objectCount <= 0)
		{
			reasonKey = "workshop.reject.empty";
			return WorkshopPublishAction.Refuse;
		}
		if (!canPublish)
		{
			reasonKey = "workshop.reject.no_steam";
			return WorkshopPublishAction.Refuse;
		}
		return string.IsNullOrEmpty(linkedItemId) ? WorkshopPublishAction.Create : WorkshopPublishAction.Update;
	}

	public static void Refresh(Action<WorkshopListResult> done)
	{
		Backend.ListInstalled(done ?? delegate
		{
		});
	}

	public static SaveEntry ReadEntry(WorkshopItem item, out string reasonKey, out string detail)
	{
		reasonKey = null;
		detail = "";
		if (item == null)
		{
			reasonKey = "workshop.reject.malformed";
			detail = "предмета нет";
			return null;
		}
		if (DecideImport(item, out reasonKey) != WorkshopImportAction.Import)
		{
			detail = item.detail;
			return null;
		}
		WorkshopReadResult workshopReadResult = WorkshopPackage.ReadFolder(item.installFolder);
		if (workshopReadResult.Ok)
		{
			return workshopReadResult.entry;
		}
		reasonKey = workshopReadResult.reasonKey;
		detail = workshopReadResult.detail;
		Debug.LogWarning("[Мастерская] предмет " + item.publishedFileId + " не читается: " + detail);
		return null;
	}

	public static SaveEntry Import(WorkshopItem item, out string reasonKey, out string detail)
	{
		SaveEntry saveEntry = ReadEntry(item, out reasonKey, out detail);
		if (saveEntry == null)
		{
			return null;
		}
		SaveEntry saveEntry2 = SaveSystem.Create(saveEntry.displayName, saveEntry.payload, saveEntry.objectCount, saveEntry.Section, saveEntry.Family);
		if (saveEntry2 == null)
		{
			reasonKey = "workshop.reject.write_failed";
			detail = "SaveSystem.Create вернул null (подробность — в журнале выше)";
			return null;
		}
		Debug.Log("[Мастерская] предмет «" + saveEntry.displayName + "» (" + item.publishedFileId + ") скопирован в сохранения как " + saveEntry2.id);
		return saveEntry2;
	}

	public static void Publish(SaveEntry entry, string title, string description, string previewImagePath, Action<WorkshopPublishResult> done)
	{
		done = done ?? delegate
		{
		};
		IWorkshopBackend backend = Backend;
		string linkedItemId = WorkshopLinks.ItemFor(entry?.id);
		string reasonKey;
		WorkshopPublishAction workshopPublishAction = DecidePublish(entry, backend.CanPublish, linkedItemId, out reasonKey);
		if (workshopPublishAction == WorkshopPublishAction.Refuse)
		{
			done(new WorkshopPublishResult
			{
				ok = false,
				reasonKey = reasonKey,
				detail = "решение: отказ"
			});
			return;
		}
		WorkshopPublishRequest workshopPublishRequest = new WorkshopPublishRequest();
		workshopPublishRequest.entry = entry;
		workshopPublishRequest.title = string.IsNullOrEmpty(title) ? entry.displayName : title;
		workshopPublishRequest.description = description ?? "";
		workshopPublishRequest.previewImagePath = previewImagePath ?? "";
		if (workshopPublishAction == WorkshopPublishAction.Update)
		{
			workshopPublishRequest.publishedFileId = linkedItemId;
			workshopPublishRequest.changeNote = "ArmorSim " + Application.version;
		}
		else
		{
			workshopPublishRequest.publishedFileId = "";
			workshopPublishRequest.changeNote = "";
		}
		backend.Publish(workshopPublishRequest, delegate(WorkshopPublishResult result)
		{
			if (result != null && !string.IsNullOrEmpty(result.publishedFileId))
			{
				WorkshopLinks.Remember(entry.id, result.publishedFileId);
			}
			else if (result == null)
			{
				result = new WorkshopPublishResult
				{
					ok = false,
					reasonKey = "workshop.reject.failed",
					detail = "приёмник вернул пустой ответ"
				};
			}
			done?.Invoke(result);
		});
	}
}
