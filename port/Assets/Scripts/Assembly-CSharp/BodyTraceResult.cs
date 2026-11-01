using System.Collections.Generic;

public sealed class BodyTraceResult
{
	public IReadOnlyList<TraceBranch> Branches { get; }

	public IReadOnlyList<TraceInterface> Interfaces { get; }

	public IReadOnlyList<TraceRejection> Rejections { get; }

	public ulong Fingerprint { get; }

	public bool Rejected => false;

	public BodyTraceResult(IReadOnlyList<TraceBranch> branches, IReadOnlyList<TraceInterface> interfaces, IReadOnlyList<TraceRejection> rejections, ulong fingerprint)
	{
	}
}
