using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class AeroImporter
{
	private struct Corner
	{
		public int Vertex;

		public int Normal;
	}

	private sealed class Tokens
	{
		private readonly string line;

		private int position;

		public Tokens(string line)
		{
			this.line = line;
		}

		public string Next()
		{
			while (position < line.Length && char.IsWhiteSpace(line[position]))
			{
				position++;
			}
			if (position >= line.Length)
			{
				return null;
			}
			int start = position;
			while (position < line.Length && !char.IsWhiteSpace(line[position]))
			{
				position++;
			}
			return line.Substring(start, position - start);
		}

		public int RemainingCount()
		{
			int count = 0;
			while (position < line.Length)
			{
				while (position < line.Length && char.IsWhiteSpace(line[position]))
				{
					position++;
				}
				if (position >= line.Length)
				{
					break;
				}
				count++;
				while (position < line.Length && !char.IsWhiteSpace(line[position]))
				{
					position++;
				}
			}
			return count;
		}
	}

	private sealed class Builder
	{
		public readonly List<Vector3> Vertices;

		private readonly List<int> triangles;

		private readonly List<Vector3> normals;

		private int discarded;

		public long InputTriangles { get; private set; }

		public Builder()
		{
			Vertices = new List<Vector3>();
			triangles = new List<int>();
			normals = new List<Vector3>();
		}

		public int AddVertex(Vector3 value)
		{
			AeroMeshData.RequireFinite(value);
			if (Vertices.Count >= MaxVertices)
			{
				throw new AeroException("aero.error.vertices", new object[2]
				{
					Vertices.Count + 1L,
					MaxVertices
				});
			}
			Vertices.Add(value);
			return Vertices.Count - 1;
		}

		public void AddTriangle(int a, int b, int c, Vector3 suppliedNormal)
		{
			InputTriangles++;
			CheckTriangleCount(InputTriangles);
			AeroMeshData.RequireFinite(suppliedNormal);
			Vector3 geometric = AeroMeshData.GeometricNormal(Vertices[a], Vertices[b], Vertices[c]);
			Vector3 d = geometric - Vector3.zero;
			if (d.x * d.x + d.y * d.y + d.z * d.z < 1e-10f)
			{
				discarded++;
				return;
			}
			Vector3 supplied = AeroMeshData.Unit(suppliedNormal);
			Vector3 d2 = supplied - Vector3.zero;
			if (d2.x * d2.x + d2.y * d2.y + d2.z * d2.z < 1e-10f)
			{
				supplied = geometric;
			}
			triangles.Add(a);
			triangles.Add(b);
			triangles.Add(c);
			normals.Add(supplied);
		}

		public AeroMeshData Finish()
		{
			if (triangles.Count == 0)
			{
				throw new AeroException("aero.error.empty", Array.Empty<object>());
			}
			Vector3 first = Vertices[triangles[0]];
			Vector3 min = first;
			Vector3 max = first;
			foreach (int index in triangles)
			{
				Vector3 v = Vertices[index];
				if (v.x <= min.x)
				{
					min.x = v.x;
				}
				if (v.y <= min.y)
				{
					min.y = v.y;
				}
				if (v.z <= min.z)
				{
					min.z = v.z;
				}
				if (max.x <= v.x)
				{
					max.x = v.x;
				}
				if (max.y <= v.y)
				{
					max.y = v.y;
				}
				if (max.z <= v.z)
				{
					max.z = v.z;
				}
			}
			double dx = (double)max.x - (double)min.x;
			double dy = (double)max.y - (double)min.y;
			double dz = (double)max.z - (double)min.z;
			Vector3 size = new Vector3((float)dx, (float)dy, (float)dz);
			AeroMeshData.RequireFinite(size);
			double maxDim = Math.Max(dx, Math.Max(dy, dz));
			if (maxDim <= 0.0)
			{
				throw new AeroException("aero.error.dimensions", Array.Empty<object>());
			}
			double cx = ((double)max.x + (double)min.x) * 0.5;
			double cy = ((double)max.y + (double)min.y) * 0.5;
			double cz = ((double)max.z + (double)min.z) * 0.5;
			double inv = 4.0 / maxDim;
			Dictionary<int, int> map = new Dictionary<int, int>();
			List<Vector3> newVertices = new List<Vector3>();
			List<int> outTriangles = new List<int>(triangles.Count);
			List<Vector3> outNormals = new List<Vector3>(normals.Count);
			int[] tri = new int[3];
			int t = 0;
			for (int i = 0; i < triangles.Count; i += 3)
			{
				for (int corner = 0; corner < 3; corner++)
				{
					int index = triangles[i + corner];
					if (!map.TryGetValue(index, out var mapped))
					{
						Vector3 v2 = Vertices[index];
						Vector3 nv = new Vector3((float)((v2.x - cx) * inv), (float)((v2.y - cy) * inv), (float)((v2.z - cz) * inv));
						AeroMeshData.RequireFinite(nv);
						mapped = newVertices.Count;
						newVertices.Add(nv);
						map.Add(index, mapped);
					}
					tri[corner] = mapped;
				}
				Vector3 normal = AeroMeshData.GeometricNormal(newVertices[tri[0]], newVertices[tri[1]], newVertices[tri[2]]);
				Vector3 d3 = normal - Vector3.zero;
				if (d3.x * d3.x + d3.y * d3.y + d3.z * d3.z < 1e-10f)
				{
					discarded++;
				}
				else
				{
					outTriangles.Add(tri[0]);
					outTriangles.Add(tri[1]);
					outTriangles.Add(tri[2]);
					outNormals.Add(normals[t]);
				}
				t++;
			}
			if (outTriangles.Count == 0)
			{
				throw new AeroException("aero.error.empty", Array.Empty<object>());
			}
			int[] remap = new int[newVertices.Count];
			for (int i = 0; i < remap.Length; i++)
			{
				remap[i] = -1;
			}
			List<Vector3> kept = new List<Vector3>(newVertices.Count);
			for (int i = 0; i < outTriangles.Count; i++)
			{
				int index = outTriangles[i];
				int target = remap[index];
				if (target < 0)
				{
					target = kept.Count;
					kept.Add(newVertices[index]);
					remap[index] = target;
				}
				outTriangles[i] = target;
			}
			AeroMeshData result = new AeroMeshData(kept.ToArray(), outTriangles.ToArray(), outNormals.ToArray());
			result.OriginalSize = size;
			result.DegenerateTriangles = discarded;
			return result;
		}
	}

	public const int MaxTriangles = 300000;

	public const int MaxVertices = 1200000;

	public const long MaxFileBytes = 134217728L;

	private const int MaxLineCharacters = 16777216;

	private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

	public static AeroMeshData Load(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new AeroException("aero.error.empty", Array.Empty<object>());
		}
		try
		{
			using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			CheckFileSize(fs.Length);
			byte[] bytes = new byte[fs.Length];
			int offset = 0;
			while (offset < bytes.Length)
			{
				int read = fs.Read(bytes, offset, bytes.Length - offset);
				if (read == 0)
				{
					throw new AeroException("aero.error.format", new object[1] { "file changed while reading" });
				}
				offset += read;
			}
			return Parse(bytes, Path.GetExtension(path));
		}
		catch (IOException ex)
		{
			throw new AeroException("aero.error.io", new object[1] { ex.Message });
		}
	}

	public static AeroMeshData Parse(byte[] bytes, string extension)
	{
		if (bytes == null)
		{
			throw new AeroException("aero.error.empty", Array.Empty<object>());
		}
		CheckFileSize(bytes.Length);
		if (bytes.Length >= 84)
		{
			uint count = (uint)(bytes[83] << 24 | bytes[82] << 16 | bytes[81] << 8 | bytes[80]);
			if (count * 50 + 84 == bytes.Length)
			{
				return ParseBinaryStl(bytes, count);
			}
		}
		string text;
		try
		{
			text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
		}
		catch (DecoderFallbackException)
		{
			throw new AeroException("aero.error.format", new object[1] { "invalid UTF-8 / binary STL length" });
		}
		if (text.Length > 0 && text[0] == '\ufeff')
		{
			text = text.Substring(1);
		}
		extension = extension ?? "";
		string ext = extension.Trim().TrimStart('.').ToLowerInvariant();
		if (ext == "obj")
		{
			return ParseObj(text);
		}
		if (ext == "stl")
		{
			return ParseAsciiStl(text);
		}
		throw new AeroException("aero.error.format", new object[1] { "supported files: OBJ, STL" });
	}

	private static void CheckFileSize(long length)
	{
		if (length == 0)
		{
			throw new AeroException("aero.error.empty", Array.Empty<object>());
		}
		if (length > MaxFileBytes)
		{
			throw new AeroException("aero.error.file_size", new object[2] { length, MaxFileBytes });
		}
	}

	private static AeroMeshData ParseBinaryStl(byte[] bytes, uint count)
	{
		CheckTriangleCount(count);
		Builder builder = new Builder();
		using BinaryReader reader = new BinaryReader(new MemoryStream(bytes));
		reader.BaseStream.Position = 84L;
		for (uint i = 0; i < count; i++)
		{
			Vector3 normal = ReadVector(reader);
			int a = builder.AddVertex(ReadVector(reader));
			int b = builder.AddVertex(ReadVector(reader));
			int c = builder.AddVertex(ReadVector(reader));
			reader.ReadUInt16();
			builder.AddTriangle(a, b, c, normal);
		}
		return builder.Finish();
	}

	private static Vector3 ReadVector(BinaryReader reader)
	{
		Vector3 v = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		AeroMeshData.RequireFinite(v);
		return v;
	}

	private static AeroMeshData ParseObj(string source)
	{
		Builder builder = new Builder();
		List<Vector3> normals = new List<Vector3>();
		int line = 0;
		using StringReader reader = new StringReader(source);
		string text;
		while ((text = reader.ReadLine()) != null)
		{
			line++;
			if (text.Length > MaxLineCharacters)
			{
				throw Format(line, "line too long");
			}
			int comment = text.IndexOf('#');
			if (comment >= 0)
			{
				text = text.Substring(0, comment);
			}
			Tokens cursor = new Tokens(text);
			string tag = cursor.Next();
			if (tag == null)
			{
				continue;
			}
			if (tag == "v" || tag == "vn")
			{
				float x = Number(cursor.Next(), line);
				float y = Number(cursor.Next(), line);
				float z = Number(cursor.Next(), line);
				string extra = cursor.Next();
				if (extra != null)
				{
					if (tag != "v")
					{
						throw Format(line, "normal needs three coordinates");
					}
					float w = Number(extra, line);
					if (w == 0f)
					{
						throw Format(line, "zero homogeneous coordinate");
					}
					x = (float)(x / (double)w);
					y = (float)(y / (double)w);
					z = (float)(z / (double)w);
					if (cursor.Next() != null)
					{
						throw Format(line, "vertex needs three coordinates and optional weight");
					}
				}
				Vector3 v = new Vector3(x, y, z);
				AeroMeshData.RequireFinite(v);
				if (tag == "v")
				{
					builder.AddVertex(v);
				}
				else
				{
					if (normals.Count >= MaxVertices)
					{
						throw new AeroException("aero.error.vertices", new object[2]
						{
							normals.Count + 1L,
							MaxVertices
						});
					}
					normals.Add(AeroMeshData.Unit(v));
				}
			}
			else
			{
				if (tag != "f")
				{
					continue;
				}
				int remaining = cursor.RemainingCount();
				if (remaining < 3)
				{
					throw Format(line, "face needs at least three vertices");
				}
				CheckTriangleCount(remaining - 2 + builder.InputTriangles);
				Corner first = ReadCorner(cursor.Next(), builder.Vertices.Count, normals.Count, line);
				Corner second = ReadCorner(cursor.Next(), builder.Vertices.Count, normals.Count, line);
				for (int i = 2; i < remaining; i++)
				{
					Corner corner = ReadCorner(cursor.Next(), builder.Vertices.Count, normals.Count, line);
					Vector3 normal = Vector3.zero;
					if (first.Normal >= 0 && second.Normal >= 0 && corner.Normal >= 0)
					{
						Vector3 sum = normals[first.Normal] + normals[second.Normal] + normals[corner.Normal];
						normal = AeroMeshData.Unit(sum);
						Vector3 d = normal - Vector3.zero;
						if (d.x * d.x + d.y * d.y + d.z * d.z < 1e-10f)
						{
							Vector3 d1 = normals[first.Normal] - Vector3.zero;
							Vector3 d2 = normals[second.Normal] - Vector3.zero;
							Vector3 d3 = normals[corner.Normal] - Vector3.zero;
							if (d1.x * d1.x + d1.y * d1.y + d1.z * d1.z >= 1e-10f || d2.x * d2.x + d2.y * d2.y + d2.z * d2.z >= 1e-10f || d3.x * d3.x + d3.y * d3.y + d3.z * d3.z >= 1e-10f)
							{
								throw new AeroException("aero.error.normal", Array.Empty<object>());
							}
						}
					}
					builder.AddTriangle(first.Vertex, second.Vertex, corner.Vertex, normal);
					second = corner;
				}
			}
		}
		return builder.Finish();
	}

	private static Corner ReadCorner(string token, int vertexCount, int normalCount, int line)
	{
		if (token == null)
		{
			throw Format(line, "missing face corner");
		}
		string[] parts = token.Split('/');
		if (parts.Length > 3 || parts[0].Length == 0)
		{
			throw Format(line, "invalid face corner");
		}
		int vertex = Index(parts[0], vertexCount);
		int normal = -1;
		if (parts.Length > 1)
		{
			if (parts[1].Length > 0 && (!int.TryParse(parts[1], NumberStyles.Integer, Invariant, out var texture) || texture == 0))
			{
				throw new AeroException("aero.error.indices", Array.Empty<object>());
			}
			if (parts.Length == 2 && parts[1].Length == 0)
			{
				throw Format(line, "missing normal index");
			}
		}
		if (parts.Length == 3)
		{
			if (parts[2].Length == 0)
			{
				throw Format(line, "missing normal index");
			}
			normal = Index(parts[2], normalCount);
		}
		Corner result = default(Corner);
		result.Vertex = vertex;
		result.Normal = normal;
		return result;
	}

	private static int Index(string token, int count)
	{
		if (!long.TryParse(token, NumberStyles.Integer, Invariant, out var value) || value == 0)
		{
			throw new AeroException("aero.error.indices", Array.Empty<object>());
		}
		long index = ((value < 0) ? (value + count) : (value - 1));
		if (index < 0 || index >= count)
		{
			throw new AeroException("aero.error.indices", Array.Empty<object>());
		}
		return (int)index;
	}

	private static AeroMeshData ParseAsciiStl(string source)
	{
		Builder builder = new Builder();
		int line = 0;
		bool inSolid = false;
		int state = 0;
		Vector3 normal = Vector3.zero;
		int v1 = -1;
		int v2 = -1;
		int v3 = -1;
		using StringReader reader = new StringReader(source);
		string text;
		while ((text = reader.ReadLine()) != null)
		{
			line++;
			if (text.Length > MaxLineCharacters)
			{
				throw Format(line, "line too long");
			}
			Tokens cursor = new Tokens(text);
			string token = cursor.Next();
			if (token == null)
			{
				continue;
			}
			string keyword = token.ToLowerInvariant();
			if (state == 0)
			{
				if (keyword == "solid" && !inSolid)
				{
					inSolid = true;
					continue;
				}
				if (keyword == "endsolid" && inSolid)
				{
					inSolid = false;
					continue;
				}
				if (!inSolid)
				{
					throw Format(line, "expected solid");
				}
				if (keyword != "facet")
				{
					throw Format(line, "unexpected STL record " + keyword);
				}
				CheckTriangleCount(builder.InputTriangles + 1);
				Expect(cursor.Next(), "normal", line);
				normal = ReadTextVector(cursor, line);
				state = 1;
			}
			else if (state == 1)
			{
				if (keyword != "outer")
				{
					throw Format(line, "unexpected STL record " + keyword);
				}
				Expect(cursor.Next(), "loop", line);
				RequireEnd(cursor, line);
				state = 2;
			}
			else if (state >= 2 && state <= 4)
			{
				if (keyword != "vertex")
				{
					throw Format(line, "unexpected STL record " + keyword);
				}
				int index = builder.AddVertex(ReadTextVector(cursor, line));
				if (state == 2)
				{
					v1 = index;
				}
				else if (state == 3)
				{
					v2 = index;
				}
				else
				{
					v3 = index;
				}
				state++;
			}
			else if (state == 5)
			{
				if (keyword != "endloop")
				{
					throw Format(line, "unexpected STL record " + keyword);
				}
				RequireEnd(cursor, line);
				state = 6;
			}
			else
			{
				if (state != 6)
				{
					throw Format(line, "unexpected STL record " + keyword);
				}
				if (keyword != "endfacet")
				{
					throw Format(line, "unexpected STL record " + keyword);
				}
				RequireEnd(cursor, line);
				builder.AddTriangle(v1, v2, v3, normal);
				state = 0;
			}
		}
		if (inSolid || state != 0)
		{
			throw Format(line, "truncated STL");
		}
		return builder.Finish();
	}

	private static Vector3 ReadTextVector(Tokens cursor, int line)
	{
		float x = Number(cursor.Next(), line);
		float y = Number(cursor.Next(), line);
		float z = Number(cursor.Next(), line);
		if (cursor.Next() != null)
		{
			throw Format(line, "unexpected extra values");
		}
		return new Vector3(x, y, z);
	}

	private static void RequireEnd(Tokens cursor, int line)
	{
		if (cursor.Next() != null)
		{
			throw Format(line, "unexpected extra values");
		}
	}

	private static void Expect(string actual, string expected, int line)
	{
		if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
		{
			throw Format(line, "expected " + expected);
		}
	}

	private static float Number(string token, int line)
	{
		if (token == null)
		{
			throw Format(line, "missing coordinate");
		}
		string text = token.ToLowerInvariant();
		if (text == "nan" || text == "+nan" || text == "-nan" || text.Contains("inf"))
		{
			throw new AeroException("aero.error.finite", Array.Empty<object>());
		}
		if (!double.TryParse(token, NumberStyles.Float, Invariant, out var value))
		{
			throw Format(line, "invalid coordinate");
		}
		float result = (float)value;
		if (float.IsNaN(result) || float.IsInfinity(result))
		{
			throw new AeroException("aero.error.finite", Array.Empty<object>());
		}
		return result;
	}

	private static AeroException Format(int line, string detail)
	{
		object[] array = new object[1];
		array[0] = "line " + line.ToString() + ": " + detail;
		return new AeroException("aero.error.format", array);
	}

	private static void CheckTriangleCount(long count)
	{
		if (count > MaxTriangles)
		{
			throw new AeroException("aero.error.triangles", new object[2] { count, MaxTriangles });
		}
	}
}
