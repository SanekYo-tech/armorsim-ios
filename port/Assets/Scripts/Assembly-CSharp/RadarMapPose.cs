using System;
using Unity.Mathematics;

public struct RadarMapPose
{
	public double3 Position;

	public double Heading;

	public double Pitch;

	public double Bank;

	public double Speed;

	public double Time;

	public bool Finished;

	public double LoadFactor => 1.0 / Math.Cos(Bank);

	public double TurnRate => (Speed > 0.0) ? (Math.Tan(Bank) * 9.80665 / Speed) : 0.0;
}
