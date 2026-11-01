using Unity.Collections;
using Unity.Jobs;

public struct NodeIntegrateVelocityJob : IJobParallelFor
{
	public NativeArray<float> nodeVr;

	public NativeArray<float> nodeVz;

	public NativeArray<float> nodeFr;

	public NativeArray<float> nodeFz;

	[ReadOnly]
	public NativeArray<float> nodeMass;

	[ReadOnly]
	public NativeArray<byte> nodeOnAxis;

	[ReadOnly]
	public NativeArray<byte> nodeFixed;

	[ReadOnly]
	public NativeArray<byte> nodeOrphan;

	public float dt;

	public void Execute(int n)
	{
	}
}
