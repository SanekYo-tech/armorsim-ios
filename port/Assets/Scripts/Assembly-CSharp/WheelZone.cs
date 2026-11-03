public struct WheelZone
{
	public float xMin;

	public float yMin;

	public float xMax;

	public float yMax;

	public bool Valid => xMax > xMin && yMax > yMin;

	public WheelZone(float xMin, float yMin, float xMax, float yMax)
	{
		this.xMin = xMin;
		this.yMin = yMin;
		this.xMax = xMax;
		this.yMax = yMax;
	}
}
