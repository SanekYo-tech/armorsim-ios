public enum SceneLoadStatus
{
	Ok = 0,
	Migrated = 1,
	RefusedNoVersion = 2,
	RefusedNewer = 3,
	RefusedNoMigration = 4,
	RefusedMalformed = 5
}
