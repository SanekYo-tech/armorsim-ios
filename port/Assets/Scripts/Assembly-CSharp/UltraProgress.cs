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

	public bool EstimateExceeded => stepsEstimate > 0 && stepsDone >= stepsEstimate;

	public double EtaSeconds
	{
		get
		{
			if (stepsDone < 200 || stepsTotal <= stepsDone)
			{
				return -1.0;
			}
			return (double)(stepsTotal - stepsDone) * elapsedS / (double)stepsDone;
		}
	}

	public float Fraction
	{
		get
		{
			if (stepsTotal <= 0)
			{
				return 0f;
			}
			float num = (float)stepsDone / (float)stepsTotal;
			if (0f > num)
			{
				return 0f;
			}
			if (num > 1f)
			{
				return 1f;
			}
			return num;
		}
	}
}
