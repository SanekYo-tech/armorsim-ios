using System;
using UnityEngine;

public struct SuperCavity
{
	public const string KeyWall = "super.fail.shell_wall";

	public const string KeyCavity = "super.fail.shell_cavity";

	public const int MinWallLayers = 4;

	public const float MinRadiusInSpacings = 1.5f;

	public const int BisectionHalvings = 60;

	public const double DrivenSlack = 0.001;

	public bool present;

	public float requestedFraction;

	public float achievedFraction;

	public int wallLayers;

	public float wallM;

	public float radiusM;

	public float spanFromM;

	public float spanToM;

	public float spacingM;

	public int nz;

	public int nNose;

	public int firstLayer;

	public int lastLayer;

	public float SpanM => spanToM - spanFromM;

	public int LayerCount
	{
		get
		{
			if (!present)
			{
				return 0;
			}
			return lastLayer - firstLayer + 1;
		}
	}

	public double SolidFraction
	{
		get
		{
			if (!present)
			{
				return 1.0;
			}
			return 1.0 - achievedFraction;
		}
	}

	public static bool TryResolve(in SuperRodSpec rod, float spacingM, out SuperCavity cavity, out string failKey, out object[] failArgs, out string prose)
	{
		cavity = default(SuperCavity);
		failKey = null;
		failArgs = null;
		prose = null;
		if (rod.cavityFraction <= 0f)
		{
			return true;
		}
		if (spacingM <= 0.0 || rod.diameterM <= 0f || rod.lengthM <= 0f)
		{
			return Refuse(rod.cavityFraction, "геометрия стержня или шаг частиц нулевые: диаметр " + string.Format("{0:F2} мм, длина {1:F2} мм, ", rod.diameterM * 1000f, rod.lengthM * 1000f) + string.Format("шаг {0:F2} мм", spacingM * 1000f), out failKey, out failArgs, out prose);
		}
		if (rod.cavityFraction >= 1f)
		{
			return Refuse(rod.cavityFraction, "доля полости обязана быть строго меньше единицы: стержень целиком из пустоты не тело", out failKey, out failArgs, out prose);
		}
		double spacing = spacingM;
		double R = rod.diameterM * 0.5;
		float noseLen = RodGeometry.NoseLengthM(rod.diameterM, rod.lengthM);
		int nz = Math.Max(1, (int)Math.Round(rod.lengthM / spacingM));
		int nNose = Math.Max(0, (int)Math.Ceiling(noseLen / spacingM));
		double rodVol = SuperBaker.RodVolume(rod);
		if (rodVol <= 0.0)
		{
			return Refuse(rod.cavityFraction, "объём сплошного стержня вышел нулевым — считать долю не от чего", out failKey, out failArgs, out prose);
		}
		double latticeLen = nz * spacing;
		double cylLen = latticeLen - nNose * spacing;
		double budget = rod.cavityFraction * rodVol;
		double vol0 = CavityVolume(R, cylLen, 0.0);
		if (cylLen <= 0.0 || budget >= vol0)
		{
			return Refuse(rod.cavityFraction, string.Format("даже НУЛЕВАЯ стенка даёт только {0:F1} % ", vol0 / rodVol * 100.0) + string.Format("объёма: за носом остаётся {0:F1} мм цилиндра при ", cylLen * 1000.0) + string.Format("полной длине растра {0:F1} мм", latticeLen * 1000.0), out failKey, out failArgs, out prose);
		}
		double lo = 0.0;
		double hi = Math.Min(R, cylLen * 0.5);
		for (int i = 0; i < BisectionHalvings; i++)
		{
			double mid = (lo + hi) * 0.5;
			if (budget < CavityVolume(R, cylLen, mid))
			{
				lo = mid;
			}
			else
			{
				hi = mid;
			}
		}
		double w = (lo + hi) * 0.5;
		int wallLayers = (int)Math.Floor(w / spacing);
		if (wallLayers < MinWallLayers)
		{
			failKey = KeyWall;
			failArgs = new object[4] { w * 1000.0, spacing * 1000.0, wallLayers, MinWallLayers };
			prose = string.Format("Полый корпус отказан: доля {0:F1} % даёт стенку {1:F2} мм ", rod.cavityFraction * 100f, w * 1000.0) + string.Format("при шаге частиц {0:F2} мм — это {1} слоёв частиц, а нужно не ", spacing * 1000.0, wallLayers) + string.Format("меньше {0}. Слои считаются по СОБСТВЕННОМУ шагу тела (ячейка ", MinWallLayers) + "сетки — два шага), и берётся целое число слоёв, которые реально помещаются. Стенка тоньше четырёх слоёв распадается по растру, а не по веществу (урок S13). Мельчи шаг частиц или бери калибр крупнее; доля полости — константа строки каталога, ручки на неё у игрока нет.";
			return false;
		}
		int wl = Math.Max(MinWallLayers, (int)Math.Round((float)(w / spacing)));
		double wall = wl * spacing;
		double spanFrom = (nNose + wl) * spacing;
		double spanTo = (nz - wl) * spacing;
		if (MinRadiusInSpacings * spacing > R - wall)
		{
			return Refuse(rod.cavityFraction, string.Format("радиус полости вышел {0:F2} мм при стенке {1} слоёв ", (R - wall) * 1000.0, wl) + string.Format("({0:F2} мм) и шаге {1:F2} мм — меньше ", wall * 1000.0, spacing * 1000.0) + string.Format("{0:F1} шагов ({1:F2} мм): ", MinRadiusInSpacings, MinRadiusInSpacings * spacing * 1000.0) + "поперёк такой полости не помещается и трёх частиц, а показ нарисовал бы её сплошной", out failKey, out failArgs, out prose);
		}
		if (nz - wl - wl - nNose < 1)
		{
			return Refuse(rod.cavityFraction, "между носовой и донной стенками не остаётся ни одного слоя " + string.Format("частиц: длина растра {0} слоёв, нос {1}, стенка {2} слоёв ", nz, nNose, wl) + "с каждого конца", out failKey, out failArgs, out prose);
		}
		int nrDia = (int)Math.Ceiling(rod.diameterM / spacingM);
		if (nrDia >= 1 && nrDia % 2 == 0)
		{
			double radius = R - wall;
			if (spacing * 0.5 * spacing >= radius * radius)
			{
				return Refuse(rod.cavityFraction, "ни один узел решётки не попадает внутрь полости радиуса " + string.Format("{0:F2} мм: ближайший к оси узел стоит на ", radius * 1000.0) + string.Format("{0:F2} мм (частиц поперёк {1})", Math.Sqrt(spacing * 0.5 * spacing) * 1000.0, nrDia), out failKey, out failArgs, out prose);
			}
		}
		cavity.present = true;
		cavity.requestedFraction = rod.cavityFraction;
		cavity.achievedFraction = (float)(CavityVolume(R, cylLen, wall) / rodVol);
		cavity.wallLayers = wl;
		cavity.wallM = (float)wall;
		cavity.radiusM = (float)(R - wall);
		cavity.spanFromM = (float)spanFrom;
		cavity.spanToM = (float)spanTo;
		cavity.spacingM = spacingM;
		cavity.nz = nz;
		cavity.nNose = nNose;
		cavity.firstLayer = nNose + wl;
		cavity.lastLayer = nz - wl - 1;
		return true;
	}

	private static double CavityVolume(double R, double cylLen, double w)
	{
		return Math.PI * (R - w) * (R - w) * (cylLen - 2.0 * w);
	}

	private static bool Refuse(float frac, string reason, out string failKey, out object[] failArgs, out string prose)
	{
		failKey = KeyCavity;
		failArgs = new object[2] { (double)frac, reason };
		prose = string.Format("Полая доля {0:F1} % не выражается на этой геометрии: {1}.", frac * 100f, reason);
		return false;
	}

	public static double BudgetVolume(in SuperRodSpec rod, float sceneSpacingM)
	{
		double num = SuperBaker.RodVolume(rod);
		if (rod.cavityFraction > 0f)
		{
			SuperCavity.TryResolve(rod, SuperBaker.ResolveSpacing(rod.spacingM, sceneSpacingM, "стержень"), out SuperCavity cavity, out string failKey, out object[] failArgs, out string prose);
			if (cavity.present)
			{
				num *= 1.0 - cavity.achievedFraction;
			}
		}
		return num;
	}

	public bool WallRole(float a, float rho, out float cRho, out float cAx)
	{
		cRho = 0f;
		cAx = 0f;
		if (!present)
		{
			return false;
		}
		double rr = Math.Max(0f, Math.Min(radiusM, rho));
		double aa = Mathf.Clamp(a, spanFromM, spanToM);
		double dx = (double)rho - rr;
		double da = (double)a - aa;
		double dist = Math.Sqrt(dx * dx + da * da);
		if (dist <= 0.0)
		{
			return false;
		}
		if (dist > (double)wallM * (1.0 + DrivenSlack))
		{
			return false;
		}
		cRho = (float)(dx / dist);
		cAx = (float)(da / dist);
		return true;
	}

	public bool WallRoleAt(Vector3 xBaked, float tipZM, out float cRho, out float cAx)
	{
		return WallRole(xBaked.z - tipZM, (float)Math.Sqrt((double)xBaked.x * xBaked.x + (double)xBaked.y * xBaked.y), out cRho, out cAx);
	}

	public bool IsSideWall(float a, float rho)
	{
		if (!present || a < spanFromM || spanToM < a || rho <= radiusM)
		{
			return false;
		}
		return (double)wallM * (1.0 + DrivenSlack) >= (double)(rho - radiusM);
	}

	public bool IsSideWallAt(Vector3 xBaked, float tipZM)
	{
		return IsSideWall(xBaked.z - tipZM, (float)Math.Sqrt((double)xBaked.x * xBaked.x + (double)xBaked.y * xBaked.y));
	}

	public string Describe(int removed, double measuredFraction)
	{
		if (!present)
		{
			return "полость: нет (сплошное тело)";
		}
		return string.Format("полость: запрошено {0:F2} %, расчётно {1:F2} %, ", requestedFraction * 100f, achievedFraction * 100f) + string.Format("измерено {0:F2} %; стенка {1:F2} мм = {2} слоёв ", measuredFraction * 100.0, wallM * 1000f, wallLayers) + string.Format("шага {0:F2} мм; радиус полости {1:F2} мм, длина {2:F2} мм ", spacingM * 1000f, radiusM * 1000f, (spanToM - spanFromM) * 1000f) + string.Format("({0} слоёв); удалено {1} частиц", LayerCount, removed);
	}
}
