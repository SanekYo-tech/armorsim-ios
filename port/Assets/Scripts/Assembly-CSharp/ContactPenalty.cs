using System;
using Unity.Collections;

public sealed class ContactPenalty : IDisposable
{
	public const int GridMaxSide = 192;

	public NativeArray<int> cellStart;

	public NativeArray<int> cellCursor;

	public NativeArray<int> cellItems;

	public NativeArray<double> work;

	public NativeArray<float> rampDepth;

	public NativeArray<float> rampOffset;

	public NativeArray<float> nodeCFr;

	public NativeArray<float> nodeCFz;

	public ContactPenalty(int segCapacity, int nodeCount)
	{
	}

	public void Dispose()
	{
	}
}
