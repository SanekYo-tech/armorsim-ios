using Unity.Collections;
using Unity.Jobs;

public struct NodeStashVelocityJob : IJobParallelFor
{
	[ReadOnly]
	public NativeArray<float> nodeVr;

	[ReadOnly]
	public NativeArray<float> nodeVz;

	public NativeArray<float> nodeVrPrev;

	public NativeArray<float> nodeVzPrev;

	public void Execute(int n)
	{
	}
}
