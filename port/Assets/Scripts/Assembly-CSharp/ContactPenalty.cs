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
		cellStart = new NativeArray<int>(36865, Allocator.Persistent);
		cellCursor = new NativeArray<int>(36864, Allocator.Persistent);
		cellItems = new NativeArray<int>(Math.Max(1, segCapacity), Allocator.Persistent);
		work = new NativeArray<double>(4, Allocator.Persistent);
		rampDepth = new NativeArray<float>(Math.Max(1, nodeCount), Allocator.Persistent);
		rampOffset = new NativeArray<float>(Math.Max(1, nodeCount), Allocator.Persistent);
		nodeCFz = new NativeArray<float>(Math.Max(1, nodeCount), Allocator.Persistent);
		nodeCFr = new NativeArray<float>(Math.Max(1, nodeCount), Allocator.Persistent);
		nodeCFz = new NativeArray<float>(Math.Max(1, nodeCount), Allocator.Persistent);
	}

	public void Dispose()
	{
		cellStart.Dispose();
		cellCursor.Dispose();
		cellItems.Dispose();
		work.Dispose();
		rampDepth.Dispose();
		rampOffset.Dispose();
		nodeCFr.Dispose();
		nodeCFz.Dispose();
		nodeCFz.Dispose();
	}
}
