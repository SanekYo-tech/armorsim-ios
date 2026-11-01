public static class WorkshopUrls
{
	public const uint AppId = 5159810u;

	public static string Hub()
	{
		return "https://steamcommunity.com/app/" + AppId.ToString() + "/workshop/";
	}

	public static string ItemPage(string publishedFileId)
	{
		return "https://steamcommunity.com/sharedfiles/filedetails/?id=" + publishedFileId;
	}

	public static string LegalAgreement()
	{
		return "https://steamcommunity.com/sharedfiles/workshoplegalagreement";
	}
}
