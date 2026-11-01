using System;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class SuperMeshRef
{
	public SuperMeshSource source;

	public float[] modelToRegion = new float[16];

	public float spacing;

	public float hu;

	public float hv;

	public float hn;

	public int part;

	public string partName = "";

	public Matrix4x4 ModelToRegion
	{
		get
		{
			Matrix4x4 result = default(Matrix4x4);
			for (int i = 0; i < 4; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					result[i, j] = modelToRegion[i * 4 + j];
				}
			}
			return result;
		}
		set
		{
			for (int i = 0; i < 4; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					modelToRegion[i * 4 + j] = value[i, j];
				}
			}
		}
	}

	public string VoxelKey
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder(source.contentSha + "|" + source.settings.Key);
			float[] array = modelToRegion;
			for (int i = 0; i < array.Length; i++)
			{
				float num = array[i];
				stringBuilder.Append('|').Append(BitConverter.SingleToInt32Bits(num).ToString("X8"));
			}
			stringBuilder.Append('|').Append(BitConverter.SingleToInt32Bits(spacing).ToString("X8"));
			stringBuilder.Append('|').Append(BitConverter.SingleToInt32Bits(hu).ToString("X8"));
			stringBuilder.Append('|').Append(BitConverter.SingleToInt32Bits(hv).ToString("X8"));
			stringBuilder.Append('|').Append(BitConverter.SingleToInt32Bits(hn).ToString("X8"));
			stringBuilder.Append('|').Append(SuperClip.Band);
			return stringBuilder.ToString();
		}
	}

	public SuperMeshRef CloneForPart(int p, string name)
	{
		SuperMeshRef superMeshRef = new SuperMeshRef();
		superMeshRef.source = source;
		superMeshRef.modelToRegion = (float[])modelToRegion.Clone();
		superMeshRef.spacing = spacing;
		superMeshRef.hu = hu;
		superMeshRef.hv = hv;
		superMeshRef.hn = hn;
		superMeshRef.part = p;
		superMeshRef.partName = name ?? "";
		return superMeshRef;
	}
}
