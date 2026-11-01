internal struct RadarKahan
{
	private double sum;

	private double correction;

	public double Value => sum;

	public void Add(double value)
	{
		double adjusted = value - correction;
		double temp = sum + adjusted;
		correction = (temp - sum) - adjusted;
		sum = temp;
	}
}
