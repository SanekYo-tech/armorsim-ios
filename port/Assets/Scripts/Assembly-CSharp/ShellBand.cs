public readonly struct ShellBand
{
	public readonly float YMinMm;

	public readonly float YMaxMm;

	public readonly float ROuterAtYMinMm;

	public readonly float ROuterAtYMaxMm;

	public readonly float WallThicknessMm;

	public readonly float RInnerAtYMinMm;

	public readonly float RInnerAtYMaxMm;

	public bool HasExplicitInner => !float.IsNaN(RInnerAtYMinMm) && !float.IsNaN(RInnerAtYMaxMm);

	public ShellBand(float yMinMm, float yMaxMm, float rOuterAtYMinMm, float rOuterAtYMaxMm, float wallThicknessMm)
	{
		YMinMm = yMinMm;
		YMaxMm = yMaxMm;
		ROuterAtYMinMm = rOuterAtYMinMm;
		ROuterAtYMaxMm = rOuterAtYMaxMm;
		WallThicknessMm = wallThicknessMm;
		RInnerAtYMinMm = float.NaN;
		RInnerAtYMaxMm = float.NaN;
	}

	public ShellBand(float yMinMm, float yMaxMm, float rOuterAtYMinMm, float rOuterAtYMaxMm, float wallThicknessMm, float rInnerAtYMinMm, float rInnerAtYMaxMm)
	{
		YMinMm = yMinMm;
		YMaxMm = yMaxMm;
		ROuterAtYMinMm = rOuterAtYMinMm;
		ROuterAtYMaxMm = rOuterAtYMaxMm;
		WallThicknessMm = wallThicknessMm;
		RInnerAtYMinMm = rInnerAtYMinMm;
		RInnerAtYMaxMm = rInnerAtYMaxMm;
	}
}
