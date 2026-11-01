using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class RadarLargeStlAdapter
{
	public const int MaxRadarTriangles = 1000000;

	public static AeroMeshData Load(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return AeroImporter.Load(path);
		}
		using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			using (BinaryReader binaryReader = new BinaryReader(fileStream))
			{
				if (fileStream.Length < 84 || fileStream.Length > 134217728)
				{
					return AeroImporter.Load(path);
				}
				fileStream.Seek(80L, SeekOrigin.Begin);
				uint num = binaryReader.ReadUInt32();
				if ((long)num * 50 + 84 != fileStream.Length)
				{
					return AeroImporter.Load(path);
				}
				if (num <= 300000)
				{
					return AeroImporter.Load(path);
				}
				if (num > MaxRadarTriangles)
				{
					throw new AeroException("aero.error.triangles", num, 1000000);
				}
				fileStream.Seek(0L, SeekOrigin.Begin);
				byte[] array = binaryReader.ReadBytes((int)fileStream.Length);
				if (array.Length != fileStream.Length)
				{
					throw new AeroException("aero.error.format", "file changed while reading");
				}
				return ParseLargeBinary(array);
			}
		}
	}

	public static AeroMeshData ParseLargeBinary(byte[] bytes)
	{
		if (bytes == null || bytes.Length < 84)
		{
			throw new AeroException("aero.error.format", "binary STL envelope");
		}
		uint num = BitConverter.ToUInt32(bytes, 80);
		if ((long)num * 50 + 84 != bytes.Length)
		{
			throw new AeroException("aero.error.format", "binary STL envelope");
		}
		if (num > MaxRadarTriangles)
		{
			throw new AeroException("aero.error.triangles", num, 1000000);
		}
		if (num <= 300000)
		{
			return AeroImporter.Parse(bytes, ".stl");
		}
		List<AeroMeshData> list = new List<AeroMeshData>();
		List<Vector3> list2 = new List<Vector3>();
		List<Vector3> list3 = new List<Vector3>();
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < num; i += 300000)
		{
			int num5 = Math.Min((int)(num - i), 300000);
			byte[] array = new byte[num5 * 50 + 84];
			Buffer.BlockCopy(bytes, 0, array, 0, 80);
			Buffer.BlockCopy(BitConverter.GetBytes(num5), 0, array, 80, 4);
			Buffer.BlockCopy(bytes, i * 50 + 84, array, 84, num5 * 50);
			AeroMeshData item = AeroImporter.Parse(array, ".stl");
			Vector3 vector = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			Vector3 vector2 = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
			for (int j = 0; j < num5; j++)
			{
				int num6 = 96 + j * 50;
				Vector3 a = new Vector3(BitConverter.ToSingle(bytes, num6), BitConverter.ToSingle(bytes, num6 + 4), BitConverter.ToSingle(bytes, num6 + 8));
				Vector3 b = new Vector3(BitConverter.ToSingle(bytes, num6 + 12), BitConverter.ToSingle(bytes, num6 + 16), BitConverter.ToSingle(bytes, num6 + 20));
				Vector3 c = new Vector3(BitConverter.ToSingle(bytes, num6 + 24), BitConverter.ToSingle(bytes, num6 + 28), BitConverter.ToSingle(bytes, num6 + 32));
				Vector3 vector3 = AeroMeshData.GeometricNormal(a, b, c);
				if (vector3.sqrMagnitude < 1e-05f * 1e-05f)
				{
					continue;
				}
				float x = ((b.x < c.x) ? b.x : c.x);
				x = ((x < a.x) ? x : a.x);
				float y = ((b.y < c.y) ? b.y : c.y);
				y = ((y < a.y) ? y : a.y);
				float z = ((b.z < c.z) ? b.z : c.z);
				z = ((z < a.z) ? z : a.z);
				vector.x = ((x < vector.x) ? x : vector.x);
				vector.y = ((y < vector.y) ? y : vector.y);
				vector.z = ((z < vector.z) ? z : vector.z);
				float x2 = ((b.x > c.x) ? b.x : c.x);
				x2 = ((x2 > a.x) ? x2 : a.x);
				float y2 = ((b.y > c.y) ? b.y : c.y);
				y2 = ((y2 > a.y) ? y2 : a.y);
				float z2 = ((b.z > c.z) ? b.z : c.z);
				z2 = ((z2 > a.z) ? z2 : a.z);
				vector2.x = ((x2 > vector2.x) ? x2 : vector2.x);
				vector2.y = ((y2 > vector2.y) ? y2 : vector2.y);
				vector2.z = ((z2 > vector2.z) ? z2 : vector2.z);
			}
			list.Add(item);
			list2.Add(vector);
			list3.Add(vector2);
			num2 += item.Vertices.Length;
			num3 += item.Triangles.Length;
			num4 += item.DegenerateTriangles;
		}
		if (list.Count == 0)
		{
			throw new AeroException("aero.error.empty");
		}
		Vector3 vector4 = list2[0];
		Vector3 vector5 = list3[0];
		for (int k = 1; k < list2.Count; k++)
		{
			vector4 = Vector3.Min(vector4, list2[k]);
			vector5 = Vector3.Max(vector5, list3[k]);
		}
		double dWidth = (double)vector5.x - vector4.x;
		double num7 = (double)vector5.y - vector4.y;
		double num8 = (double)vector5.z - vector4.z;
		double num9 = Math.Max(Math.Max(num7, num8), dWidth);
		double num10 = ((double)vector5.x + vector4.x) * 0.5;
		double num11 = ((double)vector5.y + vector4.y) * 0.5;
		double num12 = ((double)vector5.z + vector4.z) * 0.5;
		Vector3[] array2 = new Vector3[num2];
		int[] array3 = new int[num3];
		int num13 = 0;
		int num14 = 0;
		for (int l = 0; l < list.Count; l++)
		{
			AeroMeshData aeroMeshData = list[l];
			Vector3 vector6 = list2[l];
			Vector3 vector7 = list3[l];
			double num15 = Math.Max(Math.Max((double)vector7.z - vector6.z, (double)vector7.y - vector6.y), (double)vector7.x - vector6.x) / RadarModelImport.MeasuredNormalizedLongestSide(aeroMeshData);
			double num16 = ((double)vector7.x + vector6.x) * 0.5 - num10;
			double num17 = ((double)vector7.y + vector6.y) * 0.5 - num11;
			double num18 = ((double)vector7.z + vector6.z) * 0.5 - num12;
			for (int m = 0; m < aeroMeshData.Vertices.Length; m++)
			{
				array2[num13 + m] = new Vector3((float)(((double)aeroMeshData.Vertices[m].x * num15 + num16) / num9), (float)(((double)aeroMeshData.Vertices[m].y * num15 + num17) / num9), (float)(((double)aeroMeshData.Vertices[m].z * num15 + num18) / num9));
			}
			for (int n = 0; n < aeroMeshData.Triangles.Length; n++)
			{
				array3[num14 + n] = aeroMeshData.Triangles[n] + num13;
			}
			num13 += aeroMeshData.Vertices.Length;
			num14 += aeroMeshData.Triangles.Length;
		}
		AeroMeshData result = new AeroMeshData(array2, array3);
		result.OriginalSize = new Vector3((float)dWidth, (float)num7, (float)num8);
		result.DegenerateTriangles = num4;
		return result;
	}

	private static Vector3 ReadCoordinate(byte[] bytes, int offset)
	{
		return new Vector3(BitConverter.ToSingle(bytes, offset), BitConverter.ToSingle(bytes, offset + 4), BitConverter.ToSingle(bytes, offset + 8));
	}
}
