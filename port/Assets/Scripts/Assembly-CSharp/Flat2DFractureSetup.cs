using UnityEngine;

public static class Flat2DFractureSetup
{
	public const double XiWeibullM = 8.0;

	public const uint XiSalt = 1177699329u;

	public const float JitterA = 0.05f;

	public const uint JitterSalt = 1177699330u;

	public static float[] BuildXi(Vector2[] pos, uint[] blockOfParticle, ContinuumMaterialId[] blockIds, float dx)
	{
		float[] result = new float[pos.Length];
		Vector2 min = MinOf(pos);
		for (int i = 0; i < pos.Length; i++)
		{
			int ix = Mathf.CeilToInt((pos[i].x - min.x) / dx);
			int iy = Mathf.CeilToInt((pos[i].y - min.y) / dx);
			ulong key = SuperXi.PackLatKey(ix, iy, 0, 0);
			result[i] = SuperXi.For(XiSalt, blockIds[blockOfParticle[i]], 0, key, XiWeibullM);
		}
		return result;
	}

	public static Vector2[] AsbPerBlock(ContinuumMaterialId[] blockIds)
	{
		Vector2[] result = new Vector2[blockIds.Length];
		for (int i = 0; i < blockIds.Length; i++)
		{
			ContinuumProperties m = ContinuumMaterials.Get(blockIds[i]);
			Vector2 v = new Vector2(m.epsASB, m.dEpsFade);
			if (m.hasASB == 0 || ContinuumMaterials.AsbOnsetIsSubstituted(blockIds[i]))
			{
				v = new Vector2(float.PositiveInfinity, 0f);
			}
			result[i] = v;
		}
		return result;
	}

	public static Vector2[] Jitter(Vector2[] pos, float dx)
	{
		Vector2[] result = new Vector2[pos.Length];
		Vector2 min = MinOf(pos);
		for (int i = 0; i < pos.Length; i++)
		{
			int ix = Mathf.CeilToInt((pos[i].x - min.x) / dx);
			int iy = Mathf.CeilToInt((pos[i].y - min.y) / dx);
			ulong z = Mix((ulong)ix << 32 ^ (ulong)iy ^ (ulong)JitterSalt << 17);
			ulong w = Mix(z ^ 0x9E3779B97F4A7C15uL);
			double u1 = (double)(z >> 11) * 1.1102230246251565E-16;
			double u2 = (double)(w >> 11) * 1.1102230246251565E-16;
			result[i] = new Vector2(pos[i].x + (float)((2.0 * u1 - 1.0) * JitterA * dx), pos[i].y + (float)((2.0 * u2 - 1.0) * JitterA * dx));
		}
		return result;
	}

	private static Vector2 MinOf(Vector2[] pos)
	{
		Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
		for (int i = 0; i < pos.Length; i++)
		{
			if (pos[i].x < min.x)
			{
				min.x = pos[i].x;
			}
			if (pos[i].y < min.y)
			{
				min.y = pos[i].y;
			}
		}
		return min;
	}

	private static void LatticeIndex(Vector2 p, Vector2 min, float dx, out int ix, out int iy)
	{
		ix = Mathf.CeilToInt((p.x - min.x) / dx);
		iy = Mathf.CeilToInt((p.y - min.y) / dx);
	}

	public static float ProjectileWidthM(Flat2DDrawModel model)
	{
		float maxWidth = 0f;
		foreach (Flat2DDrawModel.Shape shape in model.shapes)
		{
			if (shape.on && !shape.erase && shape.role == Flat2DFill.Role.Projectile)
			{
				Vector2 dir;
				if (shape.velocity.sqrMagnitude <= 1E-06f)
				{
					dir = Vector2.right;
				}
				else
				{
					dir = new Vector2(shape.velocity.x, 0f - shape.velocity.y).normalized;
				}
				Vector2[] pts = ((shape.kind == 1) ? Flat2DDrawModel.EffectivePath(shape) : Flat2DDrawModel.EffectivePts(shape));
				if (pts != null && pts.Length != 0)
				{
					float min = float.MaxValue;
					float max = -float.MaxValue;
					for (int i = 0; i < pts.Length; i++)
					{
						float d = Vector2.Dot(dir, pts[i]);
						if (d < min)
						{
							min = d;
						}
						if (d > max)
						{
							max = d;
						}
					}
					float width = max - min + ((shape.kind == 1) ? shape.width : 0f);
					if (width > maxWidth)
					{
						maxWidth = width;
					}
				}
			}
		}
		return maxWidth;
	}

	public static bool[] RestraintMask(byte[] ownerRole)
	{
		bool[] mask = new bool[ownerRole.Length];
		for (int i = 0; i < mask.Length; i++)
		{
			mask[i] = ownerRole[i] == 0;
		}
		return mask;
	}

	private static ulong Mix(ulong z)
	{
		z += 11400714819323198485uL;
		z = (z ^ (z >> 30)) * 13787848793156543929uL;
		z = (z ^ (z >> 27)) * 10723151780598845931uL;
		return z ^ (z >> 31);
	}
}
