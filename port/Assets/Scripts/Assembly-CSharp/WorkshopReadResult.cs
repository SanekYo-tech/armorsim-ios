public struct WorkshopReadResult
{
	public WorkshopReadStatus status;

	public WorkshopManifest manifest;

	public SaveEntry entry;

	public string reasonKey;

	public string detail;

	public bool Ok => false;
}
