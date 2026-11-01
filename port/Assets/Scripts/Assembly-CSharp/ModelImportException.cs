using System;

public sealed class ModelImportException : Exception
{
	public readonly string Key;

	public readonly object[] Args;

	public ModelImportException(string key, object[] args, string prose)
	{
	}
}
