public static class ShutdownTests
{
	private sealed class SlowJob : IUltraJob
	{
		public static int Allocated;

		public static int Released;

		public static int StepsRun;

		public static bool Finished;

		public int EstimateSteps()
		{
			return 0;
		}

		public bool Step()
		{
			return false;
		}

		public GeometryTimeline Finish()
		{
			return null;
		}

		public void Release()
		{
		}
	}

	public static bool Check()
	{
		return false;
	}
}
