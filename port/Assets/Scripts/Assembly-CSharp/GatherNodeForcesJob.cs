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
		float fr = 0f;
		float fz = 0f;
		int start = nodeAdjStart[n];
		int end = nodeAdjStart[n + 1];
		for (int i = start; i < end; i++)
		{
			int elem = nodeAdjElem[i];
			int corner = nodeAdjCorner[i];
			int index = (corner + elem * 4) * 2;
			fr += elemForce[index];
			fz += elemForce[index + 1];
		}
		nodeFr[n] = fr;
		nodeFz[n] = fz;
	}
}
