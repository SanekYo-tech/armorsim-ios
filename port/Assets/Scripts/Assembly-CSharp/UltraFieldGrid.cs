using Unity.Collections;
using UnityEngine;

public static class UltraFieldGrid
{
	public struct Bounds
	{
		public float rMaxM;

		public float zMinM;

		public float zMaxM;

		public bool Valid => rMaxM > 0f && zMaxM > zMinM;
	}

	public const int DefaultR = 64;

	public const int DefaultZ = 192;

	public const float Empty = -1f;

	public const byte BodyEmpty = 255;

	public static long BytesPerFrame(int gridR, int gridZ)
	{
		return (long)gridR * gridZ * 4;
	}

	public static Bounds BoundsOf(AxiMesh m, float marginFraction = 0.1f)
	{
		float rMax = 0f;
		float zMin = float.MaxValue;
		float zMax = float.MinValue;
		if (m == null || m.elemCount <= 0)
		{
			return new Bounds
			{
				rMaxM = rMax,
				zMinM = zMin,
				zMaxM = zMax
			};
		}
		for (int i = 0; i < m.elemCount; i++)
		{
			if (m.elemAlive[i] == 0)
			{
				continue;
			}
			for (int j = 0; j < 4; j++)
			{
				int num = m.elemNodes[i * 4 + j];
				float num2 = m.nodeR[num];
				float num3 = m.nodeZ[num];
				if (num2 > rMax)
				{
					rMax = num2;
				}
				if (num3 < zMin)
				{
					zMin = num3;
				}
				if (num3 > zMax)
				{
					zMax = num3;
				}
			}
		}
		if (zMin >= zMax)
		{
			return default(Bounds);
		}
		float num4 = (zMax - zMin) * marginFraction;
		return new Bounds
		{
			rMaxM = rMax * (1f + marginFraction),
			zMinM = zMin - num4,
			zMaxM = zMax + num4
		};
	}

	public static Bounds DomainOf(AxiMesh m, float marginFraction = 0.35f)
	{
		float rMax = 0f;
		float zMin = float.MaxValue;
		float zMax = float.MinValue;
		if (m == null || m.nodeR.Length == 0)
		{
			return new Bounds
			{
				rMaxM = rMax,
				zMinM = zMin,
				zMaxM = zMax
			};
		}
		for (int i = 0; i < m.nodeR.Length; i++)
		{
			float num = m.nodeR[i];
			float num2 = m.nodeZ[i];
			if (num > rMax)
			{
				rMax = num;
			}
			if (num2 < zMin)
			{
				zMin = num2;
			}
			if (num2 > zMax)
			{
				zMax = num2;
			}
		}
		if (zMin >= zMax || rMax <= 0f)
		{
			return default(Bounds);
		}
		float num3 = (zMax - zMin) * marginFraction;
		return new Bounds
		{
			rMaxM = rMax * (1f + marginFraction),
			zMinM = zMin - num3,
			zMaxM = zMax + num3
		};
	}

	public static void Resample(AxiMesh m, NativeArray<float> src, Bounds b, int gridR, int gridZ, float[] into, byte[] bodyInto = null)
	{
		if (into == null || gridR <= 0 || gridZ <= 0)
		{
			return;
		}
		for (int i = 0; i < gridR * gridZ; i++)
		{
			into[i] = Empty;
		}
		if (bodyInto != null)
		{
			for (int i = 0; i < bodyInto.Length; i++)
			{
				bodyInto[i] = BodyEmpty;
			}
		}
		if (m == null || !b.Valid)
		{
			return;
		}
		float num = b.rMaxM / gridR;
		if (num <= 0f)
		{
			return;
		}
		float num2 = (b.zMaxM - b.zMinM) / gridZ;
		if (num2 <= 0f)
		{
			return;
		}
		float num3 = b.zMinM;
		for (int j = 0; j < m.elemCount; j++)
		{
			if (m.elemAlive[j] == 0)
			{
				continue;
			}
			float num4 = float.MaxValue;
			float num5 = float.MinValue;
			float num6 = float.MaxValue;
			float num7 = float.MinValue;
			for (int k = 0; k < 4; k++)
			{
				int num8 = m.elemNodes[j * 4 + k];
				float num9 = m.nodeR[num8];
				float num10 = m.nodeZ[num8];
				if (num9 > num4)
				{
					num4 = num9;
				}
				if (num9 > num5)
				{
					num5 = num9;
				}
				if (num10 < num6)
				{
					num6 = num10;
				}
				if (num10 > num7)
				{
					num7 = num10;
				}
			}
			int num11 = Mathf.Clamp(Mathf.FloorToInt(num4 / num), 0, gridR - 1);
			int num12 = Mathf.Clamp(Mathf.FloorToInt(num5 / num), 0, gridR - 1);
			int num13 = Mathf.Clamp(Mathf.FloorToInt((num6 - num3) / num2), 0, gridZ - 1);
			int num14 = Mathf.Clamp(Mathf.FloorToInt((num7 - num3) / num2), 0, gridZ - 1);
			float num15 = src[j];
			for (int num16 = num13; num16 <= num14; num16++)
			{
				for (int num17 = num11; num17 <= num12; num17++)
				{
					int num18 = num16 * gridR + num17;
					if (into[num18] == Empty || num15 > into[num18])
					{
						into[num18] = num15;
						if (bodyInto != null)
						{
							bodyInto[num18] = m.elemBody[j];
						}
					}
				}
			}
		}
	}

	public static bool RangeOf(NativeArray<float> grid, out float min, out float max)
	{
		min = float.MaxValue;
		max = float.MinValue;
		if (!grid.IsCreated)
		{
			return false;
		}
		for (int i = 0; i < grid.Length; i++)
		{
			float num = grid[i];
			if (num <= Empty)
			{
				continue;
			}
			if (num < min)
			{
				min = num;
			}
			if (num > max)
			{
				max = num;
			}
		}
		return max >= min;
	}

	public static float Normalise(float v, float min, float max)
	{
		return Mathf.Sqrt(Mathf.Clamp01((v - min) / Mathf.Max(1e-09f, max - min)));
	}

	public static int IndexAt(Bounds b, int gridR, int gridZ, float rM, float zM)
	{
		if (!b.Valid || rM < 0f || rM > b.rMaxM || zM < b.zMinM || zM > b.zMaxM)
		{
			return -1;
		}
		int num = Mathf.Clamp(Mathf.FloorToInt(rM / (b.rMaxM / gridR)), 0, gridR - 1);
		int num2 = Mathf.Clamp(Mathf.FloorToInt((zM - b.zMinM) / ((b.zMaxM - b.zMinM) / gridZ)), 0, gridZ - 1);
		return num2 * gridR + num;
	}
}
