using System;
using UnityEngine;

public static class SuperCostEstimate
{
	public const long CalStepsA = 2730L;

	public const long CalParticles = 583464L;

	public const long CalNodesLo = 2660850L;

	public const long CalNodesHi = 10446850L;

	public const double CalWallLo = 19.5;

	public const double CalWallHi = 25.4;

	public const string CalibrationSource = "S15 flip_1.00 (2.66 млн узлов, 19.5 с) и m150_flip_1.00 (10.45 млн узлов, 25.4 с), 583 464 частиц, 2730 шагов, RTX 3050 Laptop; до S18, на прогонах S18 не переучивается";

	public static double SecondsPerNodeStep => 2.77571559E-10;

	public static double SecondsPerParticleStep => 1.09763085E-08;

	public static int Steps(float spacing, float durationS, float cMax, float projectileSpeed)
	{
		float dt = 2f * spacing * SuperShot.Cfl / (Mathf.Abs(projectileSpeed) + cMax);
		return Mathf.CeilToInt(durationS / dt);
	}

	public static double WallSeconds(long particles, long nodes, int steps)
	{
		return ((double)nodes * SecondsPerNodeStep + (double)particles * SecondsPerParticleStep) * steps;
	}

	public static double HudMinutes(long particles, float windowS, float spacing)
	{
		return (double)(windowS / 0.0002f) * ((double)particles / 583464.0) * (double)(0.002f / (2f * spacing)) * 2.0;
	}

	public static string Line(long particles, long nodes, int steps, double actualWallS)
	{
		double predicted = WallSeconds(particles, nodes, steps);
		double error = 0.0;
		if (actualWallS > 0.0)
		{
			error = (predicted - actualWallS) / actualWallS;
		}
		return string.Format("цена: предсказано {0:F1} с, замерено {1:F1} с, ошибка {2:+0.0%;-0.0%} ", predicted, actualWallS, error) + string.Format("(частиц {0}, узлов {1}, шагов {2}; a = {3:F3} нс/частице-шаг, ", particles, nodes, steps, 10.9763085) + string.Format("b = {0:F3} нс/узел-шаг)", 0.277571559);
	}
}
