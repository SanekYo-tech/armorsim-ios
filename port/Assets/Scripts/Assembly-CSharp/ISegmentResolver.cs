using System.Collections.Generic;

public interface ISegmentResolver
{
	SegmentOutcome Resolve(in TraceSegment segment, in ProjectileState incoming, IReadOnlyList<TransferEvent> inbound);
}
