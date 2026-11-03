public struct SceneLoadResult
{
	public SceneLoadStatus status;

	public SceneFile file;

	public string message;

	public bool Loaded => status == SceneLoadStatus.Ok || status == SceneLoadStatus.Migrated;
}
