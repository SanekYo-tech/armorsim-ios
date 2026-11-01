public struct CrewSpan
{
	public float enterMm;

	public float exitMm;

	public CrewTissue tissue;

	public CrewOrgan organ;

	public float LengthMm => exitMm - enterMm;
}
