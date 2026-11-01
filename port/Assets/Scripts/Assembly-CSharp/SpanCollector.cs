using System.Collections.Generic;

public sealed class SpanCollector : ISegmentSink
{
	public readonly List<RaySpan> Spans;

	public readonly List<TraceRejection> Rejections;

	public void AddSpan(float tEnterMm, float tExitMm, IMaterialRef material, float normalThicknessMm, float obliquityDeg, EntrySurface entry)
	{
	}

	public void Reject(BodyRejectCode code, string reason)
	{
	}

	public void Clear()
	{
	}

	public void SortStable()
	{
	}
}
