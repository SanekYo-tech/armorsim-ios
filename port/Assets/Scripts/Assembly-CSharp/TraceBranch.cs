using System.Collections.Generic;

public sealed class TraceBranch
{
	public int BranchId { get; }

	public int ParentBranchId { get; }

	public int SpawnedAtInterfaceIndex { get; }

	public IReadOnlyList<TraceSegment> Segments { get; }

	public TraceBranch(int branchId, int parentBranchId, int spawnedAtInterfaceIndex, IReadOnlyList<TraceSegment> segments)
	{
		BranchId = branchId;
		ParentBranchId = parentBranchId;
		SpawnedAtInterfaceIndex = spawnedAtInterfaceIndex;
		Segments = segments;
	}
}
