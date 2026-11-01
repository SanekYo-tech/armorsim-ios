using System.Collections.Generic;
using UnityEngine;

public sealed class PlaceholderAnchors : IAnchorSet
{
	private static readonly Dictionary<AnchorId, float> Values;

	public AnchorProvenance Provenance => AnchorProvenance.PlaceholderNotMeasured;

	public float Get(AnchorId id)
	{
		if (Values.TryGetValue(id, out float value))
		{
			return value;
		}
		Debug.LogError(string.Format("[PlaceholderAnchors] опора {0} ({1}) не заведена ", id, (int)id) + "даже заглушкой; возвращён -1.");
		return -1f;
	}

	static PlaceholderAnchors()
	{
		Values = new Dictionary<AnchorId, float>();
		Values.Add(AnchorId.GelatinDepthMm, -1f);
		Values.Add(AnchorId.DistanceToFirstYawMm, -1f);
		Values.Add(AnchorId.SkinPerforationMps, -1f);
	}
}
