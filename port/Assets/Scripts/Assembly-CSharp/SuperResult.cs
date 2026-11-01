using UnityEngine;

public struct SuperResult
{
	public bool ok;

	public string failure;

	public int particles;

	public int steps;

	public float simulatedTimeS;

	public float LfKeMinM;

	public float LfEndM;

	public float DfKeMinM;

	public double keInitialJ;

	public double keMinJ;

	public double keEndJ;

	public int stepAtKeMin;

	public float timeAtKeMinS;

	public bool keMinAtEnd;

	public Vector3 momentumInitial;

	public Vector3 momentumEnd;

	public double momentumDriftMax;

	public double momentumScale;

	public double wallClockMs;

	public double particleStepsPerSecond;

	public float maxEpsP;

	public float maxTempK;

	public float maxPressurePa;
}
