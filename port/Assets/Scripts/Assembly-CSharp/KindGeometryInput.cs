public struct KindGeometryInput
{
	public float eraExtentMM;

	public float eraPlateThicknessMM;

	public float eraGapMM;

	public float neraExtentMM;

	public float neraFrontMM;

	public float neraRearMM;

	public float neraInterlayerMM;

	public static KindGeometryInput Defaults => new KindGeometryInput
	{
		eraExtentMM = 130f,
		eraPlateThicknessMM = 2.5f,
		eraGapMM = 20f,
		neraExtentMM = 250f,
		neraFrontMM = 3f,
		neraRearMM = 5f,
		neraInterlayerMM = 6f
	};

	public static KindGeometryInput From(ArmorPlate p)
	{
		return new KindGeometryInput
		{
			eraExtentMM = p.eraCassetteExtentMM,
			eraPlateThicknessMM = p.eraPlateThicknessMM,
			eraGapMM = p.eraInterlayerGapMM,
			neraExtentMM = p.neraExtentMM,
			neraFrontMM = p.neraFrontThicknessMM,
			neraRearMM = p.neraRearThicknessMM,
			neraInterlayerMM = p.neraInterlayerMM
		};
	}
}
