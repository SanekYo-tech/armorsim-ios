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
		double sum = 0.0;
		for (int i = 0; i < nodeCFr.Length; i++)
		{
			sum += (double)(nodeVrPrev[i] + nodeVr[i]) * 0.5 * (double)nodeCFr[i] + (double)(nodeVzPrev[i] + nodeVz[i]) * 0.5 * (double)nodeCFz[i];
		}
		result[0] = -(dt * sum);
	}
}
