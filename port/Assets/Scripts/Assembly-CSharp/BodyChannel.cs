using System;
using System.Collections.Generic;

public static class BodyChannel
{
	public static ChannelReport Build(BodyTraceResult result, int placementIndex)
	{
		List<ChannelEntry> crossed = new List<ChannelEntry>();
		if (result == null || result.Branches == null || result.Branches.Count == 0)
		{
			return new ChannelReport(crossed, 0f, false);
		}
		TraceBranch branch = result.Branches[0];
		IReadOnlyList<TraceSegment> segments = branch.Segments;
		float depth = 0f;
		string tissueId = null;
		float enter = 0f;
		float exit = 0f;
		bool seenMaterial = false;
		for (int i = 0; i < segments.Count; i++)
		{
			TraceSegment seg = segments[i];
			if (seg.OwnerPlacementIndex != placementIndex)
			{
				continue;
			}
			if (seg.Kind != SegmentKind.Material)
			{
				if (seenMaterial)
				{
					depth += seg.LengthMm;
				}
				continue;
			}
			seenMaterial = true;
			string id = seg.Material?.Id;
			float start = depth;
			depth += seg.LengthMm;
			if (tissueId == null)
			{
				tissueId = id;
				enter = start;
				exit = depth;
			}
			else if (tissueId == id && Math.Abs(start - exit) < 0.0001f)
			{
				exit = depth;
			}
			else
			{
				crossed.Add(new ChannelEntry(tissueId, enter, exit, exit - enter));
				tissueId = id;
				enter = start;
				exit = depth;
			}
		}
		if (tissueId != null)
		{
			crossed.Add(new ChannelEntry(tissueId, enter, exit, exit - enter));
		}
		float totalDepthMm = 0f;
		bool exitedFar = false;
		if (crossed.Count > 0)
		{
			totalDepthMm = crossed[crossed.Count - 1].ExitDepthMm;
		}
		if (crossed.Count > 1)
		{
			exitedFar = crossed[crossed.Count - 1].TissueId == crossed[0].TissueId;
		}
		return new ChannelReport(crossed, totalDepthMm, exitedFar);
	}
}
