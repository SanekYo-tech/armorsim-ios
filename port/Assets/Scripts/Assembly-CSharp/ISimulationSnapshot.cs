public interface ISimulationSnapshot
{
	int ObjectCount { get; }

	string CaptureJson();

	void RestoreJson(string json);
}
