using Unity.Collections;
using Unity.Jobs;

public struct NodeIntegratePositionJob : IJobParallelFor
{
	public NativeArray<float> nodeR;

	public NativeArray<float> nodeZ;

	[ReadOnly]
	public NativeArray<float> nodeVr;

	[ReadOnly]
	public NativeArray<float> nodeVz;

	[ReadOnly]
	public NativeArray<byte> nodeOrphan;

	public float dt;

	public void Execute(int n)
	{
		if (nodeOrphan[n] != 0)
		{
			return;
		}
		nodeR[n] += nodeVr[n] * dt;
		nodeZ[n] += nodeVz[n] * dt;
		if (nodeR[n] <= 0f && nodeR[n] != 0f)
		{
			nodeR[n] = 0f;
		}
	}
}
