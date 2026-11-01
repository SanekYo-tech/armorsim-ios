using UnityEngine;

public class Flat2DStage2Tests : MonoBehaviour
{
	public struct TaylorOut
	{
		public double Lf;

		public double Df;

		public double minKE;

		public double tMin;

		public double maxP;

		public double dtStart;

		public double dtMin;

		public bool tooShort;
	}

	private struct RingOut
	{
		public double drift;

		public double pdrift;

		public double hg;

		public double plw;

		public double cmax;
	}

	private struct HeatOut
	{
		public double Lf;

		public double maxT;
	}

	public ComputeShader solverShader;

	private const string ShaderPath = "Assets/Scripts/Flat2D/Flat2DSolver.compute";

	private double loadedFractionLast;

	[ContextMenu("★ Прогнать приёмку этапа 2 (Тейлор, A3′, A4′, A7, M1′)")]
	public void RunAll()
	{
	}

	private ComputeShader LoadShader()
	{
		return null;
	}

	public static TaylorOut RunTaylor(ComputeShader cs, ContinuumMaterialId id, float L0, float D0, float v, float dx, int maxSteps, float cfl = 0.3f)
	{
		return default;
	}

	private bool Taylor(ComputeShader cs, ContinuumMaterialId id, float L0, float D0, float v, double expectL, double expectD)
	{
		return false;
	}

	private bool Crater(ComputeShader cs)
	{
		return false;
	}

	public static Flat2DScene.Body Squeeze(int targetNx, int ny, float dx, float pushSpeed)
	{
		return default;
	}

	private static Flat2DMaterialBlock[] SqueezeMats()
	{
		return null;
	}

	private bool A3prime(ComputeShader cs)
	{
		return false;
	}

	private static float FixedStepFor(Flat2DMaterialBlock[] mats)
	{
		return 0f;
	}

	private double A3primeRun(ComputeShader cs, Flat2DScene.Body body, Flat2DMaterialBlock[] mats, float dx, Vector2 lo, Vector2 hi, float fixedDt, out Flat2DSolver.Diag d0, out Flat2DSolver.Diag d1, int stepMul = 1)
	{
		d0 = default;
		d1 = default;
		return 0.0;
	}

	private bool A4prime(ComputeShader cs)
	{
		return false;
	}

	private bool A7(ComputeShader cs)
	{
		return false;
	}

	private RingOut Ring(ComputeShader cs, float cfl)
	{
		return default;
	}

	private static double Rel(double a, double b)
	{
		return 0.0;
	}

	private bool M1prime(ComputeShader cs)
	{
		return false;
	}

	private double Deform(ComputeShader cs, Flat2DScene.Body body, Flat2DMaterialBlock[] mats, float dx, Vector2 lo, Vector2 hi, bool mutIndexZero)
	{
		return 0.0;
	}

	private bool M4(ComputeShader cs)
	{
		return false;
	}

	private HeatOut RunTaylorHeat(ComputeShader cs, bool noHeating)
	{
		return default;
	}

	private static bool IsFinite(double v)
	{
		return false;
	}
}
