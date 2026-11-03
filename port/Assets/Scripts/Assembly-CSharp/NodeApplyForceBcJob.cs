using Unity.Collections;
using Unity.Jobs;

public struct NodeApplyForceBcJob : IJobParallelFor
{
	public NativeArray<float> nodeFr;

	public NativeArray<float> nodeFz;

	[ReadOnly]
	public NativeArray<byte> nodeOnAxis;

	[ReadOnly]
	public NativeArray<byte> nodeFixed;

	public void Execute(int n)
	{
		if (nodeFixed[n] != 0)
		{
			nodeFr[n] = 0f;
			nodeFz[n] = 0f;
		}
		else if (nodeOnAxis[n] != 0)
		{
			nodeFr[n] = 0f;
		}
	}
}
