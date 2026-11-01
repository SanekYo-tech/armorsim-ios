using Unity.Collections;
using Unity.Jobs;

public struct ContactWorkJob : IJob
{
	[ReadOnly]
	public NativeArray<float> nodeCFr;

	[ReadOnly]
	public NativeArray<float> nodeCFz;

	[ReadOnly]
	public NativeArray<float> nodeVr;

	[ReadOnly]
	public NativeArray<float> nodeVz;

	[ReadOnly]
	public NativeArray<float> nodeVrPrev;

	[ReadOnly]
	public NativeArray<float> nodeVzPrev;

	public NativeArray<double> result;

	public float dt;

	public void Execute()
	{
	}
}
