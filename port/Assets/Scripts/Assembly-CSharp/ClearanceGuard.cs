public sealed class ClearanceGuard
{
	public const float RequiredClearanceInDcRef = 2f;

	public float axialM;

	public float radialM;

	public float dcRefM;

	public float axialInDcRef;

	public float radialInDcRef;

	public bool tripped;

	public string reason;

	public bool radialFromMaxRadius;

	public static ClearanceGuard Evaluate(ContinuumScenario s, CraterProfile p, float dcRefM, double dcMeasuredM)
	{
		ClearanceGuard clearanceGuard = new ClearanceGuard();
		clearanceGuard.dcRefM = dcRefM;
		float num;
		if (double.IsNaN(dcMeasuredM))
		{
			num = p.maxRadiusM;
			clearanceGuard.radialFromMaxRadius = true;
		}
		else
		{
			num = (float)(dcMeasuredM * 0.5);
		}
		float num2 = s.targetThicknessM - p.floorDepthM;
		float num3 = s.outerRadiusCalibres * s.rodDiameterM - num;
		clearanceGuard.radialM = num3;
		clearanceGuard.axialM = num2;
		clearanceGuard.radialInDcRef = num3 / dcRefM;
		clearanceGuard.axialInDcRef = num2 / dcRefM;
		bool flag = RequiredClearanceInDcRef > clearanceGuard.axialInDcRef;
		bool flag2 = RequiredClearanceInDcRef > clearanceGuard.radialInDcRef;
		if (flag | flag2)
		{
			clearanceGuard.tripped = true;
			clearanceGuard.reason = (flag ? string.Format("осевой запас {0:F2} мм = {1:F2}·D_c_ref", clearanceGuard.axialM * 1000f, clearanceGuard.axialInDcRef) : "") + ((flag && flag2) ? "; " : "") + (flag2 ? string.Format("радиальный запас {0:F2} мм = {1:F2}·D_c_ref", clearanceGuard.radialM * 1000f, clearanceGuard.radialInDcRef) : "") + string.Format(" — требуется не менее {0:F0}·D_c_ref ", RequiredClearanceInDcRef) + string.Format("({0:F2} мм)", RequiredClearanceInDcRef * dcRefM * 1000f);
		}
		return clearanceGuard;
	}
}
