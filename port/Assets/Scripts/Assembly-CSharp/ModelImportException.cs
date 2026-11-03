using System;

public sealed class ModelImportException : Exception
{
	public readonly string Key;

	public readonly object[] Args;

	public ModelImportException(string key, object[] args, string prose)
		: base(prose)
	{
		Key = key;
		Args = args ?? Array.Empty<object>();
	}
}
