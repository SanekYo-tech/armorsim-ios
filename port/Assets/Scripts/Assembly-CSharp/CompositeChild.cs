public readonly struct CompositeChild
{
	public readonly IShotObject Child;

	public readonly float GapBeforeMm;

	public CompositeChild(IShotObject child, float gapBeforeMm)
	{
		Child = child;
		GapBeforeMm = gapBeforeMm;
	}
}
