using Unity.Collections;
using Unity.Jobs;

public struct EnergyAuditJob : IJob
{
	[ReadOnly]
	public NativeArray<float> nodeMass;

	[ReadOnly]
	public NativeArray<float> nodeVr;

	[ReadOnly]
	public NativeArray<float> nodeVz;

	[ReadOnly]
	public NativeArray<float> nodeVrPrev;

	[ReadOnly]
	public NativeArray<float> nodeVzPrev;

	[ReadOnly]
	public NativeArray<byte> nodeOrphan;

	[ReadOnly]
	public NativeArray<float> elemEnergy;

	[ReadOnly]
	public NativeArray<float> elemMass;

	[ReadOnly]
	public NativeArray<float> elemHgWork;

	[ReadOnly]
	public NativeArray<float> elemVisWork;

	[ReadOnly]
	public NativeArray<byte> elemAlive;

	public NativeArray<double> result;

	public void Execute()
	{
	}
}
