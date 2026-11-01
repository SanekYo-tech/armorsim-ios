using UnityEngine;

public static class RadarScenarioTerrain
{
	public static RadarScenarioTerrainSample Sample(Vector2 position, int seed)
	{
		float num = Mathf.Clamp(position.x, 0f, 900f) * 0.111111112f;
		float num2 = Mathf.Clamp(position.y, 0f, 450f) * 0.222222224f;
		float phase = CoastPhase(seed);
		float num3 = Mathf.Sin(num * 0.41f + phase) * 0.25f + 0.5f + Mathf.Cos(num2 * 0.33f - phase) * 0.25f;
		float num4 = Mathf.Sin(num2 * 0.065f + phase * 0.2f) * 5f + 47f;
		float num5 = Mathf.Sin((num2 - 30f) * 0.065f + phase * 0.08f) * 7f + 72f;
		float num6 = Mathf.Clamp01(1f - Mathf.Abs(num - num5) / 18f);
		float num7 = Mathf.Clamp01((num2 - 43f) / 23f) * num6;
		float num8 = (num3 * 0.32f + 0.68f) * num7;
		float num9 = (num - 28f) / 12f;
		float num10 = (num2 - 57f) / 10f;
		bool flag = num9 * num9 + num10 * num10 < 1f && Mathf.Abs(Mathf.Sin(num * 0.85f)) > 0.12f && Mathf.Abs(Mathf.Sin(num2 * 0.85f)) > 0.12f;
		RadarScenarioTerrainSample result = default(RadarScenarioTerrainSample);
		result.Kind = RadarScenarioTerrainKind.Plain;
		result.Height01 = num3 * 0.05f + 0.05f;
		result.Clutter01 = num3 * 0.06f + 0.13f;
		if (Mathf.Abs(num - num4) < 1.8f + 0.35f * Mathf.Sin(num2 * 0.23f))
		{
			result.Kind = RadarScenarioTerrainKind.Water;
			result.Height01 = 0.02f;
			result.Clutter01 = 0.05f;
		}
		else if (num8 > 0.18f)
		{
			result.Kind = RadarScenarioTerrainKind.Mountain;
			result.Height01 = num8 * 0.82f + 0.18f;
			result.Clutter01 = num8 * 0.3f + 0.54f;
		}
		else if (flag)
		{
			result.Kind = RadarScenarioTerrainKind.City;
			result.Clutter01 = 0.88f;
			result.Height01 = num3 * 0.13f + 0.13f;
		}
		else if (Mathf.Min((num - 21f) * (num - 21f) / 440f + (num2 - 80f) * (num2 - 80f) / 250f, (num - 76f) * (num - 76f) / 610f + (num2 - 25f) * (num2 - 25f) / 310f) < num3 * 0.24f + 0.78f)
		{
			result.Kind = RadarScenarioTerrainKind.Forest;
			result.Height01 += 0.08f;
			result.Clutter01 = num3 * 0.12f + 0.47f;
		}
		return result;
	}

	public static RadarScenarioTerrainSample Sample(Vector2 position, int seed, bool coast)
	{
		RadarScenarioTerrainSample sample = Sample(position, seed);
		if (coast)
		{
			float num = Mathf.Clamp(position.x, 0f, 900f);
			float num2 = Mathf.Clamp(position.y, 0f, 450f);
			if (num2 < Mathf.Sin(CoastPhase(seed) + num / 140f) * 25f + 60f)
			{
				sample.Kind = RadarScenarioTerrainKind.Sea;
				sample.Height01 = 0f;
				sample.Clutter01 = 0.05f;
			}
		}
		return sample;
	}

	private static float CoastPhase(int seed)
	{
		uint num = (uint)seed * 0x2C9277B5u + 0xAC564B05u;
		num = (num ^ (num >> (int)((num >> 28) + 4u))) * 0x108EF2D9u;
		return (float)((num ^ (num >> 22)) & 0xFFFu) * 0.00153398083f;
	}

	public static Color MapColor(RadarScenarioTerrainSample sample)
	{
		float num;
		switch (sample.Kind)
		{
		case RadarScenarioTerrainKind.Forest:
			num = Mathf.Clamp01(sample.Height01 * 3f);
			return new Color(num * 0.15f + 0.19f, num * 0.13f + 0.34f, num * 0.049999997f + 0.23f);
		case RadarScenarioTerrainKind.City:
			return new Color(0.58f, 0.57f, 0.53f);
		case RadarScenarioTerrainKind.Mountain:
			num = Mathf.Clamp01(sample.Height01);
			return new Color(num * 0.41f + 0.42f, num * 0.42000002f + 0.39f, num * 0.42f + 0.33f);
		case RadarScenarioTerrainKind.Water:
			return new Color(0.21f, 0.39f, 0.49f);
		case RadarScenarioTerrainKind.Sea:
			return new Color(0.13f, 0.27f, 0.42f);
		default:
			num = Mathf.Clamp01(sample.Height01 * 5f);
			return new Color(num * 0.110000014f + 0.53f, num * 0.079999983f + 0.57f, num * 0.080000013f + 0.39f);
		}
	}
}
