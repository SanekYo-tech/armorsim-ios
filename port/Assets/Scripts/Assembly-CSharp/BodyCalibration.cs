using System;
using UnityEngine;

public static class BodyCalibration
{
	public static void Run(IMeasuredAnchors anchors)
	{
		throw new NotImplementedException("Калибровка BODY не реализована: опор нет, бриф не вернулся. Подпись существует, чтобы калибровка по PlaceholderAnchors не собиралась.");
	}

	public static GateVerdict VerdictOn(IAnchorSet anchors, out string reason)
	{
		if (anchors == null)
		{
			reason = "body.anchors.absent";
			return GateVerdict.NotApplicable;
		}
		AnchorProvenance provenance = anchors.Provenance;
		if (provenance == AnchorProvenance.PlaceholderNotMeasured)
		{
			reason = "body.anchors.placeholder_not_measured";
			return GateVerdict.NotApplicable;
		}
		if (provenance == AnchorProvenance.Measured || provenance == AnchorProvenance.Derived || provenance == AnchorProvenance.Fitted)
		{
			reason = "body.anchors.no_gate_yet";
			return GateVerdict.NotApplicable;
		}
		Debug.LogError("[BodyCalibration] неизвестный провенанс " + string.Format("{0}; приговор не вынесен.", (int)provenance));
		reason = "body.anchors.unknown_provenance";
		return GateVerdict.NotApplicable;
	}
}
