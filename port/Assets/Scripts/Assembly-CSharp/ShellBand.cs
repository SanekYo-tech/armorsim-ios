public readonly struct ShellBand
{
	public readonly float YMinMm;

	public readonly float YMaxMm;

	public readonly float ROuterAtYMinMm;

	public readonly float ROuterAtYMaxMm;

	public readonly float WallThicknessMm;

	public readonly float RInnerAtYMinMm;

	public readonly float RInnerAtYMaxMm;

	public bool HasExplicitInner => false;

	public ShellBand(float yMinMm, float yMaxMm, float rOuterAtYMinMm, float rOuterAtYMaxMm, float wallThicknessMm)
	{
		YMinMm = 0f;
		YMaxMm = 0f;
		ROuterAtYMinMm = 0f;
		ROuterAtYMaxMm = 0f;
		WallThicknessMm = 0f;
		RInnerAtYMinMm = 0f;
		RInnerAtYMaxMm = 0f;
	}

	public ShellBand(float yMinMm, float yMaxMm, float rOuterAtYMinMm, float rOuterAtYMaxMm, float wallThicknessMm, float rInnerAtYMinMm, float rInnerAtYMaxMm)
	{
		YMinMm = 0f;
		YMaxMm = 0f;
		ROuterAtYMinMm = 0f;
		ROuterAtYMaxMm = 0f;
		WallThicknessMm = 0f;
		RInnerAtYMinMm = 0f;
		RInnerAtYMaxMm = 0f;
	}
}
