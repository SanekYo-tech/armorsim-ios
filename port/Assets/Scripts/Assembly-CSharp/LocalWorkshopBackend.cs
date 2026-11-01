using System;

public sealed class LocalWorkshopBackend : IWorkshopBackend
{
	public static string Folder => null;

	public string Name => null;

	public bool CanPublish => false;

	public string AuthorName => null;

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
	}

	public void Publish(WorkshopPublishRequest request, Action<WorkshopPublishResult> done)
	{
	}

	public void OpenItemPage(string publishedFileId)
	{
	}

	public void OpenHub()
	{
	}
}
