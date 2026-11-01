using System.Collections.Generic;
using UnityEngine;

public static class UltraDetachedPieces
{
	public static List<UltraDetachedPiece> Collect(AxiMesh m, int[] componentOf, int boundaryRoot)
	{
		Dictionary<int, UltraDetachedPiece> dictionary = new Dictionary<int, UltraDetachedPiece>();
		for (int i = 0; i < m.elemCount; i++)
		{
			if (m.elemAlive[i] == 0)
			{
				continue;
			}
			if (m.elemBody[i] != 1)
			{
				continue;
			}
			int key = componentOf[i];
			if (key == boundaryRoot)
			{
				continue;
			}
			dictionary.TryGetValue(key, out var value);
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			for (int j = 0; j < 4; j++)
			{
				int num5 = m.elemNodes[i * 4 + j];
				num += m.nodeR[num5];
				num2 += m.nodeZ[num5];
				num3 += m.nodeVr[num5];
				num4 += m.nodeVz[num5];
			}
			num *= 0.25f;
			num2 *= 0.25f;
			num3 *= 0.25f;
			num4 *= 0.25f;
			float num6 = m.elemMass[i];
			float num7 = num6 + value.massKg;
			if (num7 > 1E-12f)
			{
				value.centroidRZ = new Vector2((value.centroidRZ.x * value.massKg + num * num6) / num7, (value.centroidRZ.y * value.massKg + num2 * num6) / num7);
				value.velocityRZ = new Vector2((value.velocityRZ.x * value.massKg + num3 * num6) / num7, (value.velocityRZ.y * value.massKg + num4 * num6) / num7);
			}
			else
			{
				value.centroidRZ = new Vector2(num, num2);
				value.velocityRZ = new Vector2(num3, num4);
			}
			value.massKg = num7;
			value.elementCount++;
			dictionary[key] = value;
		}
		List<UltraDetachedPiece> list = new List<UltraDetachedPiece>(dictionary.Count);
		foreach (KeyValuePair<int, UltraDetachedPiece> item in dictionary)
		{
			UltraDetachedPiece value2 = item.Value;
			ContinuumProperties continuumProperties = ContinuumMaterials.Get(ContinuumMaterials.ActiveRhaSet);
			float num8 = Mathf.Max(1f, continuumProperties.rho0);
			value2.radiusM = Mathf.Pow(value2.massKg / num8 * 3f / (4f * Mathf.PI), 1f / 3f);
			list.Add(value2);
		}
		return list;
	}
}