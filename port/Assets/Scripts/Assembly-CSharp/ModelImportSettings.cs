using System;
using UnityEngine;

[Serializable]
public struct ModelImportSettings
{
	public ModelUnits units;

	public ModelUpAxis up;

	public bool mirrorX;

	public float scale;

	public const float MinScale = 0.001f;

	public const float MaxScale = 1000f;

	public static ModelImportSettings Default => new ModelImportSettings
	{
		mirrorX = true,
		scale = 1f
	};

	public float Scale
	{
		get
		{
			if (scale > 0f && !float.IsNaN(scale) && !float.IsInfinity(scale))
			{
				return scale;
			}
			return 1f;
		}
	}

	public float MetresPerFileUnit => Scale * MetresPerUnit;

	public float MetresPerUnit
	{
		get
		{
			switch (units)
			{
			case ModelUnits.Millimetre:
				return 0.001f;
			case ModelUnits.Centimetre:
				return 0.01f;
			case ModelUnits.Metre:
				return 1f;
			case ModelUnits.Inch:
				return 0.0254f;
			default:
				throw new ArgumentOutOfRangeException("units", units, "неизвестные единицы модели");
			}
		}
	}

	public int DetSign => (mirrorX ? (-1) : 1);

	public string Key
	{
		get
		{
			string text = string.Format("{0}|{1}|{2}", units, up, mirrorX);
			string text2 = "";
			if (Scale != 1f)
			{
				text2 = "|x" + BitConverter.SingleToInt32Bits(Scale).ToString("X8");
			}
			return text + text2;
		}
	}

	public Vector3 Apply(Vector3 f)
	{
		float num = Scale * MetresPerUnit;
		Vector3 vector = ((up == ModelUpAxis.Z) ? new Vector3(f.x, f.z, -f.y) : f);
		if (mirrorX)
		{
			vector.x = -vector.x;
		}
		vector *= num;
		return vector;
	}
}
