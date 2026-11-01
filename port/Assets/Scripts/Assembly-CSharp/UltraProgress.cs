public struct UltraProgress
{
	public UltraJobState state;

	public string key;

	public int stepsDone;

	public int stepsTotal;

	public int stepsEstimate;

	public double elapsedS;

	public string error;

	public const int MinStepsForEta = 200;

	public bool EstimateExceeded => false;

	public double EtaSeconds => 0.0;

	public float Fraction => 0f;
}
