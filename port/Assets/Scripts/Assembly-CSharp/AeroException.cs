using System;

public sealed class AeroException : Exception
{
	public readonly string Key;

	public readonly object[] Args;

	public AeroException(string key, params object[] args)
		: base(key + ((args != null && args.Length != 0) ? (": " + string.Join(", ", args)) : ""))
	{
		Key = key;
		Args = args ?? Array.Empty<object>();
	}
}
