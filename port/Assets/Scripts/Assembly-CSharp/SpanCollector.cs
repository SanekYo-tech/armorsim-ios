using System.Collections.Generic;

public sealed class SpanCollector : ISegmentSink
{
	public readonly List<RaySpan> Spans;

	public readonly List<TraceRejection> Rejections;

	public SpanCollector()
	{
		Spans = new List<RaySpan>();
		Rejections = new List<TraceRejection>();
	}

	public void AddSpan(float tEnterMm, float tExitMm, IMaterialRef material, float normalThicknessMm, float obliquityDeg, EntrySurface entry)
	{
		if (!(tExitMm > tEnterMm))
		{
			return;
		}
		Spans.Add(new RaySpan(tEnterMm, tExitMm, material, normalThicknessMm, obliquityDeg, entry));
	}

	public void Reject(BodyRejectCode code, string reason)
	{
		Rejections.Add(new TraceRejection(code, reason, -1));
	}

	public void Clear()
	{
		Spans.Clear();
		Rejections.Clear();
	}

	public void SortStable()
	{
		for (int i = 1; i < Spans.Count; i++)
		{
			RaySpan key = Spans[i];
			int j = i;
			while (j > 0 && Spans[j - 1].EnterMm > key.EnterMm)
			{
				Spans[j] = Spans[j - 1];
				j--;
			}
			Spans[j] = key;
		}
	}
}
