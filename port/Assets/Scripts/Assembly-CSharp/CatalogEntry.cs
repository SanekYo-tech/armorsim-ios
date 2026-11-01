public struct CatalogEntry
{
	public string id;

	public string displayName;

	public string category;

	public string tooltip;

	public string nameKey;

	public string tipKey;

	public string categoryKey;

	public string Name => Localization.HasKey(nameKey) ? Localization.T(nameKey) : displayName;

	public string Tip => Localization.HasKey(tipKey) ? Localization.T(tipKey) : tooltip;

	public string CategoryName => Localization.HasKey(categoryKey) ? Localization.T(categoryKey) : category;
}
