using UnityEngine;

public static class ResultDisplayFit
{
	public struct Fit
	{
		public float heightPx;

		public bool topAligned;

		public bool autoSize;
	}

	public const float BaseHeightPx = 60f;

	public const float PadPx = 12f;

	public const float FontMaxPt = 28f;

	public const float FontMinPt = 20f;

	public const float MaxScreenShare = 1f / 3f;

	public static Fit Decide(float textHeightPx, float canvasHeightPx)
	{
		Fit fit = default(Fit);
		float h = textHeightPx + PadPx * 2f;
		if (h <= BaseHeightPx)
		{
			fit.heightPx = BaseHeightPx;
			return fit;
		}
		float height = Mathf.Max(BaseHeightPx, canvasHeightPx * MaxScreenShare);
		fit.topAligned = true;
		fit.autoSize = h > height;
		if (!fit.autoSize)
		{
			height = h;
		}
		fit.heightPx = height;
		return fit;
	}
}
