public static class BodyCalibration
{
	public static void Run(IMeasuredAnchors anchors)
	{
	}

	public static GateVerdict VerdictOn(IAnchorSet anchors, out string reason)
	{
		reason = null;
		return GateVerdict.NotApplicable;
	}
}
