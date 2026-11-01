using UnityEngine;

public static class BodyComplianceGate
{
	public const string NoAnchorsReason = "body.gate.no_measured_anchors";

	public static GateVerdict Evaluate(CertifiedMaterialRef material, StandardLevel level)
	{
		return GateVerdict.NotApplicable;
	}

	public static string ReasonFor(GateVerdict verdict)
	{
		if (verdict != 0)
		{
			if (verdict == GateVerdict.Fail || verdict == GateVerdict.Pass)
			{
				return null;
			}
			Debug.LogError(string.Format("[BodyComplianceGate] неизвестный приговор {0}; ", verdict) + "причина не названа.");
		}
		return NoAnchorsReason;
	}
}
