using UnityEngine;

public readonly struct LocalThickening
{
	public readonly string Name;

	public readonly float ExtraThicknessMm;

	public readonly IMaterialRef Material;

	public readonly Vector2 CenterMm;

	public readonly float RadiusMm;

	public readonly int AfterChildIndex;

	public LocalThickening(string name, float extraThicknessMm, IMaterialRef material, Vector2 centerMm, float radiusMm, int afterChildIndex)
	{
		Name = null;
		ExtraThicknessMm = 0f;
		Material = null;
		CenterMm = default;
		RadiusMm = 0f;
		AfterChildIndex = 0;
	}
}
