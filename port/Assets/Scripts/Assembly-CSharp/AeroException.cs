using System;

public sealed class AeroException : Exception
{
	public readonly string Key;

	public readonly object[] Args;

	public AeroException(string key, params object[] args)
	{
	}
}
