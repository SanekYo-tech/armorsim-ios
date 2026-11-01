public interface IUltraJob
{
	int EstimateSteps();

	bool Step();

	GeometryTimeline Finish();

	void Release();
}
