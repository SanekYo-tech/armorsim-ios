using System;

public struct ContinuumFragmentSeed
{
	public float r;

	public float z;

	public float vr;

	public float vz;

	public float massKg;

	public float timeS;

	public int body;

	public float Speed => MathF.Sqrt(vr * vr + vz * vz);
}
