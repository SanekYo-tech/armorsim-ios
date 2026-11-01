public struct WheelZone
{
	public float xMin;

	public float yMin;

	public float xMax;

	public float yMax;

	public bool Valid => false;

	public WheelZone(float xMin, float yMin, float xMax, float yMax)
	{
		this.xMin = 0f;
		this.yMin = 0f;
		this.xMax = 0f;
		this.yMax = 0f;
	}
}
