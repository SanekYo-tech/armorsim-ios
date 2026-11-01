using Unity.Collections;
using Unity.Jobs;

public struct ReduceTimeStepJob : IJob
{
	[ReadOnly]
	public NativeArray<float> elemDtCand;

	[ReadOnly]
	public NativeArray<byte> elemAlive;

	public NativeArray<float> result;

	public void Execute()
	{
	}
}
