using System;
using System.Threading;

public interface ISolverBackend
{
	GeometryTimeline Solve(ContinuumScenario scenario, IProgress<float> progress, CancellationToken ct);
}
