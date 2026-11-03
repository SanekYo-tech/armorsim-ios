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
		float min = 1E+30f;
		for (int i = 0; i < elemDtCand.Length; i++)
		{
			if (elemAlive[i] != 0 && elemDtCand[i] < min)
			{
				min = elemDtCand[i];
			}
		}
		result[0] = min;
	}
}
