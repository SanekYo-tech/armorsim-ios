using System;

public interface IWorkshopBackend
{
	string Name { get; }

	bool CanPublish { get; }

	string AuthorName { get; }

	event Action ItemsChanged;

	void ListInstalled(Action<WorkshopListResult> done);

	void Publish(WorkshopPublishRequest request, Action<WorkshopPublishResult> done);

	void OpenItemPage(string publishedFileId);

	void OpenHub();
}
