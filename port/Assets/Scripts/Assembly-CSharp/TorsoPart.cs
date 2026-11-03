using UnityEngine;

public readonly struct TorsoPart
{
	public readonly string TissueId;

	public readonly TissueKind Kind;

	public readonly Vector3 CentreMm;

	public readonly IShotObject Object;

	public TorsoPart(string tissueId, TissueKind kind, Vector3 centreMm, IShotObject obj)
	{
		TissueId = tissueId;
		Kind = kind;
		CentreMm = centreMm;
		Object = obj;
	}
}
