public readonly struct TraceRejection
{
	public readonly BodyRejectCode Code;

	public readonly string Reason;

	public readonly int PlacementIndex;

	public TraceRejection(BodyRejectCode code, string reason, int placementIndex)
	{
		Code = code;
		Reason = reason;
		PlacementIndex = placementIndex;
	}

	public override string ToString()
	{
		return string.Format("{0} @[{1}]: {2}", Code, PlacementIndex, Reason);
	}
}
