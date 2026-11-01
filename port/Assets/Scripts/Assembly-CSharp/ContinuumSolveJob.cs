public sealed class ContinuumSolveJob : IUltraJob
{
	private readonly ContinuumScenario scenario;

	private ContinuumBackend.Run run;

	public ContinuumSolveJob(ContinuumScenario s)
	{
		scenario = s;
		run = new ContinuumBackend.Run(s);
	}

	public int EstimateSteps()
	{
		return UltraSolveQueue.EstimateSteps(scenario);
	}

	public bool Step()
	{
		if (run.Finished)
		{
			return false;
		}
		run.Step();
		return true;
	}

	public GeometryTimeline Finish()
	{
		return run.Finish();
	}

	public void Release()
	{
		if (run != null)
		{
			run.ReleaseWorkingSet();
			run = null;
		}
	}
}
