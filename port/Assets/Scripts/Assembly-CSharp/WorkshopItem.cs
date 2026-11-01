public class WorkshopItem
{
	public string publishedFileId;

	public string installFolder;

	public ulong sizeBytes;

	public bool installed;

	public bool downloading;

	public WorkshopManifest manifest;

	public string reasonKey;

	public string detail;

	public string Title => null;
}
