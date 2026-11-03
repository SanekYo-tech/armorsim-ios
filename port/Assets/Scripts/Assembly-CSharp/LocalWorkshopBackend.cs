using System;
using System.IO;
using UnityEngine;

public sealed class LocalWorkshopBackend : IWorkshopBackend
{
	public static string Folder => Path.Combine(Application.persistentDataPath, "ArmorSim", "Workshop");

	public string Name => "местная папка (без Steam)";

	public bool CanPublish => false;

	public string AuthorName => "";

	public event Action ItemsChanged
	{
		add
		{
		}
		remove
		{
		}
	}

	public void ListInstalled(Action<WorkshopListResult> done)
	{
		WorkshopListResult workshopListResult = new WorkshopListResult();
		workshopListResult.ok = true;
		workshopListResult.where = Folder;
		workshopListResult.emptyHintKey = "workshop.empty_local";
		try
		{
			if (!Directory.Exists(Folder))
			{
				Directory.CreateDirectory(Folder);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Мастерская] папку " + Folder + " создать не удалось: " + ex.Message);
		}
		foreach (string item in WorkshopPackage.SubFolders(Folder))
		{
			WorkshopItem workshopItem = new WorkshopItem();
			workshopItem.publishedFileId = "";
			workshopItem.installFolder = "";
			workshopItem.detail = "";
			workshopItem.publishedFileId = Path.GetFileName(item);
			workshopItem.installFolder = item;
			workshopItem.installed = true;
			WorkshopReadResult workshopReadResult = WorkshopPackage.ReadFolder(item);
			if (workshopReadResult.status == WorkshopReadStatus.Ok)
			{
				workshopItem.manifest = workshopReadResult.manifest;
			}
			else
			{
				workshopItem.reasonKey = workshopReadResult.reasonKey;
				workshopItem.detail = workshopReadResult.detail;
			}
			workshopListResult.items.Add(workshopItem);
		}
		done?.Invoke(workshopListResult);
	}

	public void Publish(WorkshopPublishRequest request, Action<WorkshopPublishResult> done)
	{
		Debug.LogWarning("[Мастерская] выгрузка невозможна: приёмник — " + Name + ". Нужен Steam (символ сборки ARMORSIM_STEAM и запущенный клиент).");
		if (done != null)
		{
			WorkshopPublishResult obj = new WorkshopPublishResult
			{
				ok = false,
				reasonKey = "workshop.reject.no_steam",
				detail = "приёмник: " + Name
			};
			done(obj);
		}
	}

	public void OpenItemPage(string publishedFileId)
	{
		if (string.IsNullOrEmpty(publishedFileId))
		{
			return;
		}
		Application.OpenURL("https://steamcommunity.com/sharedfiles/filedetails/?id=" + publishedFileId);
	}

	public void OpenHub()
	{
		Application.OpenURL("https://steamcommunity.com/app/5163906/workshop/");
	}
}
