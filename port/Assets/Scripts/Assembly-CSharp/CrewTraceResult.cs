using UnityEngine;

public struct CrewTraceResult
{
	public bool hitBody;

	public bool stopped;

	public float exitSpeedMps;

	public float depositedJ;

	public float damageHp;

	public float pathMm;

	public Vector3 endLocal;

	public Vector3 entryLocal;

	public bool[] organTouched;

	public CrewOrgan stopOrgan;

	public float[] organJ;

	public string trail;
}
