using System.Collections.Generic;

public readonly struct SegmentOutcome
{
	public readonly ProjectileState State;

	public readonly IReadOnlyList<TransferEvent> Transfers;

	public SegmentOutcome(ProjectileState state, IReadOnlyList<TransferEvent> transfers)
	{
		State = default;
		Transfers = null;
	}
}
