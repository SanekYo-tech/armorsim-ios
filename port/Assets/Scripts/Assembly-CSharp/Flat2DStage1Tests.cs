using UnityEngine;

public class Flat2DStage1Tests : MonoBehaviour
{
	public ComputeShader solverShader;

	private const string ShaderPath = "Assets/Scripts/Flat2D/Flat2DSolver.compute";

	private double momentumScale;

	private double lastHourglassFraction;

	[ContextMenu("★ Прогнать приёмку этапа 1 (A1–A6 + мутации)")]
	public void RunAll()
	{
	}

	private ComputeShader LoadShader()
	{
		return null;
	}

	private bool A0(ComputeShader cs)
	{
		return false;
	}

	private bool A1(ComputeShader cs)
	{
		return false;
	}

	private bool SoundSpeedCase(ComputeShader cs, ContinuumMaterialId id, double tol, bool mutBakeSteel, bool mutStiffX2)
	{
		return false;
	}

	private static int[] Gauge(Vector2[] pos, float x, float dx, float yLo, float yHi)
	{
		return null;
	}

	private static double MeanVx(Vector2[] v, int[] idx)
	{
		return 0.0;
	}

	private bool A2(ComputeShader cs)
	{
		return false;
	}

	private bool A3(ComputeShader cs)
	{
		return false;
	}

	private bool Collision(ComputeShader cs, int steps, bool avOn, bool asym, out Flat2DSolver.Diag d0, out Flat2DSolver.Diag d1)
	{
		d0 = default;
		d1 = default;
		return false;
	}

	private static bool IsFinite(double v)
	{
		return false;
	}

	private bool A4(ComputeShader cs)
	{
		return false;
	}

	private bool A5(ComputeShader cs)
	{
		return false;
	}

	private bool A6(ComputeShader cs)
	{
		return false;
	}

	private bool M1(ComputeShader cs)
	{
		return false;
	}

	private bool M2(ComputeShader cs)
	{
		return false;
	}

	private bool M3(ComputeShader cs)
	{
		return false;
	}
}
