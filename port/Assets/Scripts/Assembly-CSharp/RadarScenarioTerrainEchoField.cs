using System;
using UnityEngine;

public sealed class RadarScenarioTerrainEchoField : IDisposable
{
	public const int MaxResolution = 384;

	public const int AngularBins = 720;

	public const float PpiRadiusFraction = 0.45f;

	private readonly int size;

	private readonly byte[] kinds;

	private readonly byte[] strengths;

	private readonly float[] heights;

	private readonly float[] geometry;

	private readonly float[] lastSweep;

	private readonly ushort[] angleBins;

	private readonly bool[] valid;

	private readonly float[] fade;

	private readonly Color32[] pixels;

	private Texture2D texture;

	private bool dirty;

	private bool disposed;

	private float renderedNow;

	private float renderedGain;

	private float renderedClutter;

	private float renderedPersistence;

	private int lastRenderFrame;

	private Vector2 origin;

	private float worldPixelSize;

	private float minimumHeight01;

	public int Resolution => size;

	public int GeometryPixelCount { get; private set; }

	public int ActivePixelCount { get; private set; }

	public float MinimumHeight01
	{
		get
		{
			return minimumHeight01;
		}
		set
		{
			float num = RadarScenarioData.Finite(value) ? Mathf.Clamp01(value) : 0f;
			if (num != minimumHeight01)
			{
				minimumHeight01 = num;
				dirty = true;
			}
		}
	}

	public RadarScenarioTerrainEchoField(int resolution = 384)
	{
		lastSweep = new float[720];
		fade = new float[720];
		dirty = true;
		renderedNow = float.NaN;
		lastRenderFrame = -1;
		int num = Mathf.Clamp(resolution, 64, MaxResolution);
		size = num;
		kinds = new byte[num * num];
		strengths = new byte[num * num];
		heights = new float[num * num];
		geometry = new float[num * num];
		angleBins = new ushort[num * num];
		valid = new bool[num * num];
		pixels = new Color32[num * num];
		for (int i = 0; i < lastSweep.Length; i++)
		{
			lastSweep[i] = float.NegativeInfinity;
		}
	}

	public void Rebuild(int seed, Vector2 station, float displayRange, float coverageRadius, float minimumHeight01 = 0f)
	{
		ThrowIfDisposed();
		Array.Clear(strengths, 0, strengths.Length);
		Array.Clear(geometry, 0, geometry.Length);
		Array.Clear(valid, 0, valid.Length);
		Array.Clear(pixels, 0, pixels.Length);
		for (int i = 0; i < lastSweep.Length; i++)
		{
			lastSweep[i] = float.NegativeInfinity;
		}
		GeometryPixelCount = 0;
		origin = station;
		worldPixelSize = 0f;
		float num = RadarScenarioData.Finite(minimumHeight01) ? Mathf.Clamp01(minimumHeight01) : 0f;
		if (num != this.minimumHeight01)
		{
			this.minimumHeight01 = num;
		}
		dirty = true;
		if (!RadarScenarioData.Finite(displayRange) || displayRange <= 0f || !RadarScenarioData.Finite(coverageRadius) || coverageRadius <= 0f || !RadarScenarioData.InsideMap(station))
		{
			return;
		}
		worldPixelSize = displayRange / ((float)size * PpiRadiusFraction);
		float num2 = Mathf.Min(displayRange, coverageRadius);
		for (int j = 0; j < size; j++)
		{
			for (int k = 0; k < size; k++)
			{
				float num3 = ((float)k + 0.5f - (float)size * 0.5f) * worldPixelSize;
				float num4 = ((float)j + 0.5f - (float)size * 0.5f) * worldPixelSize;
				if (num3 * num3 + num4 * num4 > num2 * num2)
				{
					continue;
				}
				Vector2 worldPosition = new Vector2(station.x + num3, station.y + num4);
				if (!RadarScenarioData.InsideMap(worldPosition))
				{
					continue;
				}
				RadarScenarioTerrainSample sample = RadarScenarioTerrain.Sample(worldPosition, seed);
				int num5 = j * size + k;
				valid[num5] = true;
				kinds[num5] = (byte)sample.Kind;
				heights[num5] = sample.Height01;
				float num6 = MathF.Atan2(num3, num4) * Mathf.Rad2Deg;
				num6 -= MathF.Floor(num6 / 360f) * 360f;
				num6 = Mathf.Clamp(num6, 0f, 360f);
				angleBins[num5] = (ushort)Mathf.Min(Mathf.FloorToInt(num6 * (float)AngularBins / 360f), 719);
			}
		}
		int num7 = 0;
		for (int l = 3; l < size; l += 7)
		{
			for (int m = (num7 % 2 + 1) * 3; m < size; m += 7)
			{
				uint num8 = Mix((uint)(m * 73846557) ^ (uint)(l * 19442591) ^ (uint)seed);
				Vector2 centre = new Vector2((float)m + (Unit(num8 & 0xFFFFu) - 0.5f) * 2.2f, (float)l + (Unit(Mix(num8) & 0xFFFFu) - 0.5f) * 2.2f);
				int num9 = Mathf.Clamp(Mathf.RoundToInt(centre.x), 0, size - 1);
				int num10 = Mathf.Clamp(Mathf.RoundToInt(centre.y), 0, size - 1);
				int num11 = num10 * size + num9;
				if (!valid[num11])
				{
					continue;
				}
				RadarScenarioTerrainKind kind = (RadarScenarioTerrainKind)kinds[num11];
				float num12 = heights[num11];
				float strength;
				float density;
				switch (kind)
				{
				case RadarScenarioTerrainKind.Forest:
					strength = 0.42f;
					density = 0.86f;
					break;
				case RadarScenarioTerrainKind.City:
					strength = 0.68f;
					density = 0.93f;
					break;
				case RadarScenarioTerrainKind.Mountain:
					density = 0.96f;
					strength = num12 * 0.36f + 0.46f;
					break;
				default:
					if (!NearTerrainBoundary(num9, num10, kind))
					{
						continue;
					}
					strength = 0.36f;
					density = 0.82f;
					break;
				}
				if (Unit(Mix(num8 ^ 0x96E34A1Fu) & 0xFFFFu) > density)
				{
					continue;
				}
				float num13 = HeightAt(num9 + 1, num10, num12) - HeightAt(num9 - 1, num10, num12);
				float num14 = HeightAt(num9, num10 + 1, num12) - HeightAt(num9, num10 - 1, num12);
				float angle;
				if (kind == RadarScenarioTerrainKind.Mountain && num13 * num13 + num14 * num14 > 1E-06f)
				{
					angle = MathF.Atan2(num14, num13) + 1.5707964f;
				}
				else
				{
					angle = MathF.Cos(centre.y * 0.027f + centre.x * 0.04f) * 0.4f + 0.7f;
				}
				float num15 = angle + (Unit(Mix(num8 ^ 0x8A41E2BDu) & 0xFFFFu) - 0.5f) * 0.6f;
				float num16 = Unit(Mix(num8 ^ 0x245F39ABu) & 0xFFFFu) * 1.8f + 3.8f;
				float num17 = Unit(Mix(num8 ^ 0x5924C0F7u) & 0xFFFFu) * 0.8f + 1.05f;
				if ((num8 & 1) != 0)
				{
					num17 = -num17;
				}
				DrawStroke(centre, num15, num16, num17, strength, kind);
			}
			num7++;
		}
		for (int n = 0; n < geometry.Length; n++)
		{
			if (!valid[n])
			{
				continue;
			}
			strengths[n] = (byte)Mathf.RoundToInt(Mathf.Clamp01(geometry[n]) * 255f);
			if (strengths[n] > 0)
			{
				GeometryPixelCount++;
			}
		}
	}

	private float HeightAt(int x, int y, float fallback)
	{
		if (x >= 0 && x < size && y >= 0 && y < size)
		{
			int num = y * size + x;
			if (valid[num])
			{
				return heights[num];
			}
		}
		return fallback;
	}

	private bool NearTerrainBoundary(int x, int y, RadarScenarioTerrainKind kind)
	{
		for (int i = y - 2; i <= y + 2; i += 2)
		{
			for (int j = x - 2; j <= x + 2; j += 2)
			{
				if (j >= 0 && j < size && i >= 0 && i < size)
				{
					int num = i * size + j;
					if (valid[num] && (RadarScenarioTerrainKind)kinds[num] == kind)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private void DrawStroke(Vector2 centre, float angle, float length, float bend, float strength, RadarScenarioTerrainKind kind)
	{
		float num = MathF.Sin(angle);
		float num2 = MathF.Cos(angle);
		for (int i = 0; i <= 12; i++)
		{
			float num3 = (float)i / 12f;
			float num4 = (num3 - 0.5f) * length;
			float num5 = 4f * num3 * (1f - num3) * bend;
			float num6 = centre.x + num4 * num - num5 * num2;
			float num7 = centre.y + num4 * num2 + num5 * num;
			int num8 = (int)Math.Floor(num6 - 1f);
			int num9 = (int)Math.Floor(num7 - 1f);
			for (int j = num9; j <= num9 + 2; j++)
			{
				for (int k = num8; k <= num8 + 2; k++)
				{
					if (k < 0 || k >= size || j < 0 || j >= size)
					{
						continue;
					}
					int num10 = j * size + k;
					if (!valid[num10] || (RadarScenarioTerrainKind)kinds[num10] != kind)
					{
						continue;
					}
					float num11 = Mathf.Clamp01((1.02f - Vector2.Distance(new Vector2(k, j), new Vector2(num6, num7))) / 0.65f) * strength;
					geometry[num10] = Mathf.Max(geometry[num10], num11);
				}
			}
		}
	}

	private static uint Mix(uint value)
	{
		value ^= value >> 16;
		value *= 0x7FEB352Du;
		value ^= value >> 15;
		value *= 0x846CA68Bu;
		value ^= value >> 16;
		return value;
	}

	private static float Unit(uint value)
	{
		return value / 65535f;
	}

	public void Advance(float now, float startDegrees, float advanceDegrees)
	{
		ThrowIfDisposed();
		if (!RadarScenarioData.Finite(now) || now < 0f || !RadarScenarioData.Finite(startDegrees) || !RadarScenarioData.Finite(advanceDegrees) || advanceDegrees <= 0f)
		{
			return;
		}
		for (int i = 0; i < 720; i++)
		{
			float num = (float)i + 0.5f;
			num = num * 360f / 720f;
			num -= startDegrees;
			num -= MathF.Floor(num / 360f) * 360f;
			num = Mathf.Clamp(num, 0f, 360f);
			if (advanceDegrees >= 360f || (num > 0f && num <= advanceDegrees))
			{
				lastSweep[i] = now;
			}
		}
		dirty = true;
	}

	public Texture2D Render(float now, float gain, float clutter, float persistence)
	{
		ThrowIfDisposed();
		if (texture == null)
		{
			texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
			texture.name = "RadarScenarioTerrainStrokes";
			texture.hideFlags = (HideFlags)61;
			texture.filterMode = FilterMode.Bilinear;
			texture.wrapMode = TextureWrapMode.Clamp;
		}
		now = (RadarScenarioData.Finite(now) ? Mathf.Max(now, 0f) : 0f);
		gain = (RadarScenarioData.Finite(gain) ? Mathf.Clamp(gain, 0f, 4f) : 0f);
		clutter = (RadarScenarioData.Finite(clutter) ? Mathf.Clamp(clutter, 0f, 1f) : 0f);
		persistence = (RadarScenarioData.Finite(persistence) ? Mathf.Max(persistence, 0.05f) : 0.05f);
		int num = (int)Math.Floor(now * 30f);
		if (!dirty && num == lastRenderFrame && gain == renderedGain && clutter == renderedClutter && persistence == renderedPersistence && now >= renderedNow)
		{
			return texture;
		}
		renderedNow = now;
		renderedGain = gain;
		renderedClutter = clutter;
		renderedPersistence = persistence;
		lastRenderFrame = num;
		dirty = false;
		for (int i = 0; i < 720; i++)
		{
			float num2 = now - lastSweep[i];
			float num3;
			if (lastSweep[i] != float.NegativeInfinity && num2 >= 0f && num2 < persistence * 3f)
			{
				num3 = MathF.Exp(-num2 / persistence) * gain * clutter;
			}
			else
			{
				num3 = 0f;
			}
			fade[i] = num3;
		}
		ActivePixelCount = 0;
		for (int j = 0; j < pixels.Length; j++)
		{
			int num4 = 0;
			Color32 color = default(Color32);
			if (minimumHeight01 <= heights[j])
			{
				float num5 = (float)(int)strengths[j] / 255f * fade[angleBins[j]];
				num5 = Mathf.Clamp01(num5) * 255f;
				num4 = Mathf.RoundToInt(num5);
				if (num4 != 0)
				{
					color = new Color32(22, byte.MaxValue, 48, (byte)num4);
				}
			}
			pixels[j] = color;
			if (num4 >= 8)
			{
				ActivePixelCount++;
			}
		}
		texture.SetPixels32(pixels);
		texture.Apply(false, false);
		return texture;
	}

	public float VisibilityAt(Vector2 worldPosition, float now, float gain, float clutter, float persistence)
	{
		if (disposed || worldPixelSize <= 0f || !RadarScenarioData.InsideMap(worldPosition) || !RadarScenarioData.Finite(now) || !RadarScenarioData.Finite(gain) || !RadarScenarioData.Finite(clutter) || !RadarScenarioData.Finite(persistence))
		{
			return 0f;
		}
		float num = (worldPosition.x - origin.x) / worldPixelSize;
		float num2 = (worldPosition.y - origin.y) / worldPixelSize;
		Vector2 vector = new Vector2(num, num2) + Vector2.one * ((float)size * 0.5f);
		int num3 = Mathf.FloorToInt(vector.x);
		int num4 = Mathf.FloorToInt(vector.y);
		if (num3 < 0 || num4 < 0 || num3 >= size || num4 >= size)
		{
			return 0f;
		}
		int num5 = num4 * size + num3;
		float num6 = lastSweep[angleBins[num5]];
		float num7 = (0.05f >= persistence) ? 0.05f : persistence;
		float num8 = now - num6;
		if (!valid[num5] || minimumHeight01 > heights[num5] || num6 == float.NegativeInfinity || num8 < 0f || num8 >= num7 * 3f)
		{
			return 0f;
		}
		float num9 = MathF.Exp(-num8 / num7);
		gain = Mathf.Clamp(gain, 0f, 4f);
		clutter = Mathf.Clamp(clutter, 0f, 1f);
		return Mathf.Clamp01((float)(int)strengths[num5] / 255f * num9 * gain * clutter);
	}

	private void ClearSweep()
	{
		for (int i = 0; i < lastSweep.Length; i++)
		{
			lastSweep[i] = float.NegativeInfinity;
		}
	}

	private void ThrowIfDisposed()
	{
		if (disposed)
		{
			throw new ObjectDisposedException("RadarScenarioTerrainEchoField");
		}
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}
		Texture2D texture = this.texture;
		disposed = true;
		if (texture != null)
		{
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(texture);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(texture);
			}
			this.texture = null;
		}
	}
}
