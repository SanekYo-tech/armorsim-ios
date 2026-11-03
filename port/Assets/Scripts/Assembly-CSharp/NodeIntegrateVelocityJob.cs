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
		if (nodeOrphan[n] != 0)
		{
			nodeFr[n] = 0f;
			nodeFz[n] = 0f;
			return;
		}
		if (nodeFixed[n] != 0)
		{
			nodeVr[n] = 0f;
			nodeVz[n] = 0f;
			nodeFr[n] = 0f;
			nodeFz[n] = 0f;
			return;
		}
		if (nodeOnAxis[n] != 0)
		{
			nodeFr[n] = 0f;
			nodeVr[n] = 0f;
		}
		float invMass = 1f / nodeMass[n];
		nodeVr[n] = invMass * nodeFr[n] * dt + nodeVr[n];
		nodeVz[n] = invMass * nodeFz[n] * dt + nodeVz[n];
	}
}
