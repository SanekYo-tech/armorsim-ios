using UnityEngine;

public struct AeroRayGrid
{
	public Vector3 Origin;

	public Vector3 StepU;

	public Vector3 StepV;

	public Vector3 Direction;

	public int Resolution;

	public float MaxDistance;

	public double CellArea => Vector3.Cross(StepU, StepV).magnitude;
}
