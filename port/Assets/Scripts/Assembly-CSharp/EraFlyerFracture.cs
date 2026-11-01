using System.Collections.Generic;
using UnityEngine;

public static class EraFlyerFracture
{
	public class Grid
	{
		public int nx;

		public int ny;

		public float[] damage;

		public bool[] bondRight;

		public bool[] bondUp;

		public float threshold;

		public int[] parent;

		public int Count => ny * nx;

		public int Index(int x, int y)
		{
			return x + y * nx;
		}
	}

	public static readonly bool EnabledInPlayback;

	public const string KnownGap = "⏸ ПРОБЕЛ: разрушение метаемой пластины ПОКАЗАНО, но в расчёт НЕ ВХОДИТ — скважность (ReactiveArmor) по-прежнему считает пластину целой. Связать их = правка валидированной модели, и она требует своего шага со своей приёмкой.";

	public const int CellsX = 16;

	public const int CellsY = 12;

	public const float BondThresholdRHA = 1f;

	public const float NeighbourShare = 0.45f;

	public const float EdgeWeakenMax = 2.5f;

	public static float ToughnessRatio(ArmorPlate.ArmorType type)
	{
		float kic = GradyModel.ForArmor(type).Kic;
		float rhaKic = GradyModel.RHA().Kic;
		if (rhaKic <= 1e-06f)
		{
			return 1f;
		}
		float ratio = kic / rhaKic;
		if (0.05f > ratio)
		{
			return 0.05f;
		}
		if (ratio > 4f)
		{
			return 4f;
		}
		return ratio;
	}

	public static float BondThreshold(ArmorPlate.ArmorType type)
	{
		float kic = GradyModel.ForArmor(type).Kic;
		float rhaKic = GradyModel.RHA().Kic;
		if (rhaKic <= 1e-06f)
		{
			return 1f;
		}
		float ratio = kic / rhaKic;
		if (0.05f > ratio)
		{
			return 0.05f;
		}
		if (ratio > 4f)
		{
			return 4f;
		}
		return ratio;
	}

	public static Grid Create(int nx, int ny, ArmorPlate.ArmorType type)
	{
		Grid grid = new Grid();
		grid.nx = ((nx < 1) ? 1 : nx);
		grid.ny = ((ny < 1) ? 1 : ny);
		grid.threshold = BondThreshold(type);
		int count = grid.ny * grid.nx;
		grid.damage = new float[count];
		grid.bondRight = new bool[count];
		grid.bondUp = new bool[count];
		grid.parent = new int[count];
		for (int y = 0; y < grid.ny; y++)
		{
			for (int x = 0; x < grid.nx; x++)
			{
				int i = y * grid.nx + x;
				grid.bondRight[i] = x < grid.nx - 1;
				grid.bondUp[i] = y < grid.ny - 1;
			}
		}
		return grid;
	}

	public static void Accumulate(Grid g, float hitU, float hitV, float dt, float threatLoad, float driveLoad)
	{
		if (g == null)
		{
			return;
		}
		float ax = (g.nx - 1) * hitU;
		float ay = (g.ny - 1) * hitV;
		for (int y = 0; y < g.ny; y++)
		{
			for (int x = 0; x < g.nx; x++)
			{
				int i = y * g.nx + x;
				float dx = x - ax;
				float dy = y - ay;
				float w = Mathf.Exp(-(dx * dx + dy * dy) * 0.5f);
				g.damage[i] = (w * threatLoad + driveLoad) * dt + g.damage[i];
			}
		}
	}

	public static void ApplyImpact(Grid g, float u, float v, float amount)
	{
		if (g == null || amount <= 0f)
		{
			return;
		}
		int cx = Mathf.RoundToInt((g.nx - 1) * u);
		if (cx < 0)
		{
			cx = 0;
		}
		else if (cx > g.nx - 1)
		{
			cx = g.nx - 1;
		}
		int cy = Mathf.RoundToInt((g.ny - 1) * v);
		if (cy < 0)
		{
			cy = 0;
		}
		else if (cy > g.ny - 1)
		{
			cy = g.ny - 1;
		}
		int i = cy * g.nx + cx;
		g.damage[i] = amount + g.damage[i];
		AddAt(g, cx - 1, cy, amount * NeighbourShare);
		AddAt(g, cx + 1, cy, amount * NeighbourShare);
		AddAt(g, cx, cy - 1, amount * NeighbourShare);
		AddAt(g, cx, cy + 1, amount * NeighbourShare);
	}

	private static void AddAt(Grid g, int x, int y, float amount)
	{
		if (x >= 0 && y >= 0 && x < g.nx && y < g.ny)
		{
			int i = y * g.nx + x;
			g.damage[i] = amount + g.damage[i];
		}
	}

	public static float EdgeWeaken(Grid g, int x, int y)
	{
		int edges = ((x == 0) ? 1 : 0) + ((x == g.nx - 1) ? 1 : 0) + ((y == 0) ? 1 : 0) + ((y == g.ny - 1) ? 1 : 0);
		return (float)edges * 0.25f * EdgeWeakenMax + 1f;
	}

	public static float BondLimit(Grid g, int x1, int y1, int x2, int y2)
	{
		float scale = (EdgeWeaken(g, x2, y2) + EdgeWeaken(g, x1, y1)) * 0.5f;
		if (scale <= 1e-06f)
		{
			scale = 1e-06f;
		}
		return g.threshold / scale;
	}

	public static int BreakBonds(Grid g)
	{
		if (g == null)
		{
			return 0;
		}
		int broken = 0;
		for (int y = 0; y < g.ny; y++)
		{
			for (int x = 0; x < g.nx; x++)
			{
				int i = y * g.nx + x;
				if (g.bondRight[i] && x < g.nx - 1)
				{
					if (BondLimit(g, x, y, x + 1, y) <= (g.damage[i] + g.damage[i + 1]) * 0.5f)
					{
						broken++;
						g.bondRight[i] = false;
					}
				}
				if (g.bondUp[i] && y < g.ny - 1)
				{
					int up = (y + 1) * g.nx + x;
					if (BondLimit(g, x, y, x, y + 1) <= (g.damage[up] + g.damage[i]) * 0.5f)
					{
						broken++;
						g.bondUp[i] = false;
					}
				}
			}
		}
		return broken;
	}

	private static int Find(int[] p, int a)
	{
		while (a != p[a])
		{
			p[a] = p[p[a]];
			a = p[a];
		}
		return a;
	}

	public static void BuildComponents(Grid g)
	{
		if (g == null)
		{
			return;
		}
		for (int i = 0; i < g.ny * g.nx; i++)
		{
			g.parent[i] = i;
		}
		for (int y = 0; y < g.ny; y++)
		{
			for (int x = 0; x < g.nx; x++)
			{
				int i = y * g.nx + x;
				if (g.bondRight[i] && x < g.nx - 1)
				{
					int ra = Find(g.parent, i);
					int rb = Find(g.parent, i + 1);
					if (ra != rb)
					{
						g.parent[ra] = rb;
					}
				}
				if (g.bondUp[i] && y < g.ny - 1)
				{
					int ra2 = Find(g.parent, i);
					int rb2 = Find(g.parent, (y + 1) * g.nx + x);
					if (ra2 != rb2)
					{
						g.parent[ra2] = rb2;
					}
				}
			}
		}
	}

	public static int Root(Grid g, int cell)
	{
		return Find(g.parent, cell);
	}

	public static float StrainOf(Grid g, int x, int y)
	{
		int total = 0;
		int broken = 0;
		if (x < g.nx - 1)
		{
			total++;
			if (!g.bondRight[y * g.nx + x])
			{
				broken++;
			}
		}
		if (x > 0)
		{
			total++;
			if (!g.bondRight[y * g.nx + x - 1])
			{
				broken++;
			}
		}
		if (y < g.ny - 1)
		{
			total++;
			if (!g.bondUp[y * g.nx + x])
			{
				broken++;
			}
		}
		if (y > 0)
		{
			total++;
			if (!g.bondUp[(y - 1) * g.nx + x])
			{
				broken++;
			}
		}
		if (total == 0)
		{
			return 0f;
		}
		return (float)broken / (float)total;
	}

	public static Vector2 PullDirection(Grid g, int x, int y)
	{
		Vector2 sum = Vector2.zero;
		if (x < g.nx - 1 && !g.bondRight[y * g.nx + x])
		{
			sum += Vector2.right;
		}
		if (x > 0 && !g.bondRight[y * g.nx + x - 1])
		{
			sum += Vector2.left;
		}
		if (y < g.ny - 1 && !g.bondUp[y * g.nx + x])
		{
			sum += Vector2.up;
		}
		if (y > 0 && !g.bondUp[(y - 1) * g.nx + x])
		{
			sum += Vector2.down;
		}
		if (sum.x * sum.x + sum.y * sum.y <= 1e-09f)
		{
			return Vector2.zero;
		}
		return sum.normalized;
	}

	public static int MainBodyRoot(Grid g)
	{
		Dictionary<int, int> sizes = new Dictionary<int, int>();
		for (int i = 0; i < g.Count; i++)
		{
			int root = Find(g.parent, i);
			int size = 1;
			if (sizes.TryGetValue(root, out var existing))
			{
				size = existing + 1;
			}
			sizes[root] = size;
		}
		int bestRoot = -1;
		int bestSize = -1;
		foreach (KeyValuePair<int, int> pair in sizes)
		{
			if (bestSize < pair.Value)
			{
				bestSize = pair.Value;
				bestRoot = pair.Key;
			}
		}
		return bestRoot;
	}

	public static float DetachedFraction(Grid g)
	{
		if (g == null || g.ny * g.nx == 0)
		{
			return 0f;
		}
		BuildComponents(g);
		int main = MainBodyRoot(g);
		int detached = 0;
		for (int i = 0; i < g.ny * g.nx; i++)
		{
			if (Root(g, i) != main)
			{
				detached++;
			}
		}
		return (float)detached / (float)(g.ny * g.nx);
	}
}
