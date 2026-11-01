using System;

public struct ProjectileCatalogEntry
{
	public CatalogEntry meta;

	public Func<ProjectileDefinition> make;
}
