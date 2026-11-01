using System;
using System.Collections.Generic;
using UnityEngine;

public static class ModelImport
{
	public static PreparedModel Prepare(ImportedModel m, ModelImportSettings st)
	{
		if (m == null)
		{
			throw new ArgumentNullException("m");
		}
		PreparedModel result = new PreparedModel();
		result.sources = new List<ModelSourceFile>(m.sources);
		result.contentSha = m.ContentSha;
		result.settings = st;
		Bounds bounds = default(Bounds);
		bool flag = false;
		foreach (ImportedPart part in m.parts)
		{
			Vector3[] array = new Vector3[part.vertices.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = st.Apply(part.vertices[i]);
			}
			int[] array2 = (int[])part.triangles.Clone();
			if (st.mirrorX)
			{
				for (int j = 0; j < array2.Length; j += 3)
				{
					int num = array2[j + 1];
					array2[j + 1] = array2[j + 2];
					array2[j + 2] = num;
				}
			}
			ModelPart modelPart = new ModelPart();
			modelPart.name = part.name;
			modelPart.vertices = array;
			modelPart.triangles = array2;
			modelPart.bounds = BoundsOf(array);
			if (flag)
			{
				bounds.Encapsulate(modelPart.bounds);
			}
			else
			{
				bounds = modelPart.bounds;
				flag = true;
			}
			result.parts.Add(modelPart);
		}
		result.centreShiftM = bounds.center;
		foreach (ModelPart part2 in result.parts)
		{
			for (int k = 0; k < part2.vertices.Length; k++)
			{
				part2.vertices[k] -= result.centreShiftM;
			}
			part2.bounds = BoundsOf(part2.vertices);
			Topology(part2);
			part2.volumeM3 = SignedVolume(part2.vertices, part2.triangles);
		}
		result.bounds = new Bounds(Vector3.zero, bounds.size);
		return result;
	}

	public static Bounds BoundsOf(Vector3[] v)
	{
		if (v == null || v.Length == 0)
		{
			return new Bounds(Vector3.zero * 0.5f, Vector3.zero);
		}
		Vector3 min = v[0];
		Vector3 max = v[0];
		for (int i = 1; i < v.Length; i++)
		{
			min = Vector3.Min(min, v[i]);
			max = Vector3.Max(max, v[i]);
		}
		Vector3 vector = max - min;
		return new Bounds(min + vector * 0.5f, vector);
	}

	private static void Topology(ModelPart p)
	{
		Dictionary<long, int> dictionary = new Dictionary<long, int>();
		int[] triangles = p.triangles;
		for (int i = 0; i < triangles.Length; i += 3)
		{
			for (int j = 0; j < 3; j++)
			{
				int num = triangles[i + j];
				int num2 = triangles[i + (j + 1) % 3];
				if (num == num2)
				{
					continue;
				}
				long key = ((num < num2) ? (((long)num << 32) | (uint)num2) : (((long)num2 << 32) | (uint)num));
				dictionary.TryGetValue(key, out int count);
				dictionary[key] = count + 1;
			}
		}
		int num3 = 0;
		int num4 = 0;
		foreach (int value in dictionary.Values)
		{
			if (value == 1)
			{
				num3++;
			}
			else if (value > 2)
			{
				num4++;
			}
		}
		bool flag = false;
		if (num3 == 0 && num4 == 0)
		{
			flag = dictionary.Count > 0;
		}
		p.boundaryEdges = num3;
		p.nonManifoldEdges = num4;
		p.closed = flag;
	}

	public static double SignedVolume(Vector3[] v, int[] t)
	{
		double num = 0.0;
		for (int i = 0; i < t.Length; i += 3)
		{
			Vector3 vector = v[t[i]];
			Vector3 vector2 = v[t[i + 1]];
			Vector3 vector3 = v[t[i + 2]];
			num += (double)vector.x * ((double)vector2.y * vector3.z - (double)vector2.z * vector3.y) - (double)vector.y * ((double)vector2.x * vector3.z - (double)vector2.z * vector3.x) + (double)vector.z * ((double)vector2.x * vector3.y - (double)vector2.y * vector3.x);
		}
		return num / 6.0;
	}
}
