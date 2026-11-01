using System;

public class CorruptFrameException : Exception
{
	public int Frame { get; }

	public CorruptFrameException(string message, int frame)
		: base(message)
	{
		Frame = frame;
	}
}
