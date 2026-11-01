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

	public double LoadFactor => 0.0;

	public double TurnRate => 0.0;
}
