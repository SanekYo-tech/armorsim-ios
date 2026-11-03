using UnityEngine;

public readonly struct TraceInterface
{
	public readonly int SegmentIndexAfter;

	public readonly IMaterialRef Before;

	public readonly IMaterialRef After;

	public readonly float ObliquityDeg;

	public readonly Vector3 PointMm;

	public TraceInterface(int segmentIndexAfter, IMaterialRef before, IMaterialRef after, float obliquityDeg, Vector3 pointMm)
	{
		SegmentIndexAfter = segmentIndexAfter;
		Before = before;
		After = after;
		ObliquityDeg = obliquityDeg;
		PointMm = pointMm;
	}
}
