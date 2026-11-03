public struct KahanSum
{
	private double sum;

	private double c;

	public double Value => sum;

	public void Add(double v)
	{
		double y = v - c;
		double t = sum + y;
		c = (t - sum) - y;
		sum = t;
	}
}
