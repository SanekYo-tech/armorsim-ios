using System.Collections.Generic;

public sealed class BodyTraceResult
{
	public IReadOnlyList<TraceBranch> Branches { get; }

	public IReadOnlyList<TraceInterface> Interfaces { get; }

	public IReadOnlyList<TraceRejection> Rejections { get; }

	public ulong Fingerprint { get; }

	public bool Rejected => Rejections != null && Rejections.Count > 0;

	public BodyTraceResult(IReadOnlyList<TraceBranch> branches, IReadOnlyList<TraceInterface> interfaces, IReadOnlyList<TraceRejection> rejections, ulong fingerprint)
	{
		Branches = branches;
		Interfaces = interfaces;
		Rejections = rejections;
		Fingerprint = fingerprint;
	}
}
