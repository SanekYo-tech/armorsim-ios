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
		double kinetic = 0.0;
		for (int i = 0; i < nodeMass.Length; i++)
		{
			if (nodeOrphan[i] == 0)
			{
				double vr = (double)(nodeVrPrev[i] + nodeVr[i]) * 0.5;
				double vz = (double)(nodeVzPrev[i] + nodeVz[i]) * 0.5;
				kinetic += (vr * vr + vz * vz) * (double)nodeMass[i] * 0.5;
			}
		}
		double energy = 0.0;
		double hgWork = 0.0;
		double visWork = 0.0;
		for (int j = 0; j < elemMass.Length; j++)
		{
			hgWork += (double)elemHgWork[j];
			visWork += (double)elemVisWork[j];
			if (elemAlive[j] != 0)
			{
				energy += (double)elemEnergy[j] * (double)elemMass[j];
			}
		}
		result[0] = kinetic;
		result[1] = energy;
		result[2] = hgWork;
		result[3] = visWork;
	}
}
