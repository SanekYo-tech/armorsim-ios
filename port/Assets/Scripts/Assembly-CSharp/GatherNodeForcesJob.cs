using Unity.Collections;
using Unity.Jobs;

public struct GatherNodeForcesJob : IJobParallelFor
{
	[ReadOnly]
	public NativeArray<int> nodeAdjStart;

	[ReadOnly]
	public NativeArray<int> nodeAdjElem;

	[ReadOnly]
	public NativeArray<byte> nodeAdjCorner;

	[ReadOnly]
	public NativeArray<float> elemForce;

	public NativeArray<float> nodeFr;

	public NativeArray<float> nodeFz;

	public void Execute(int n)
	{
	}
}
