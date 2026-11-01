using System;

public struct ObjectCatalogEntry
{
	public CatalogEntry meta;

	public ArmorPlate.ObjectKind kind;

	public Action<ArmorPlate> apply;
}
