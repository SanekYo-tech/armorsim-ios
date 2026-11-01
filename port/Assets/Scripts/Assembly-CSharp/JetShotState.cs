public static class JetShotState
{
	public struct Stage
	{
		public float standoffM;

		public int elementsBefore;

		public int elementsRetired;

		public JetChannel channel;

		public float arrivalS;

		public bool stopsHere;
	}

	public static void At(Stage[] stages, float t, out int retiredFromTip, out float headLimitM)
	{
		retiredFromTip = default;
		headLimitM = default;
	}
}
