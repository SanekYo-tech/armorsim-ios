using Unity.Collections;
using UnityEngine;

public struct GeometryFrame
{
	public float time;

	public NativeArray<Vector2> projectileProfile;

	public NativeArray<Vector2> targetProfile;

	public float erodedMassKg;

	public float tipAxialVelocity;

	public NativeArray<ContinuumFragmentSeed> fragmentSeeds;

	public NativeArray<float> fieldPlastic;

	public NativeArray<byte> fieldBody;

	public int projectileLoopCount;

	public int targetLoopCount;

	public void Dispose()
	{
	}
}
