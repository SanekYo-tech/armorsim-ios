using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;

public static class ModelFileImporter
{
	private sealed class PartBuilder
	{
		public string name;

		public readonly List<Vector3> verts = new List<Vector3>();

		public readonly List<int> tris = new List<int>();

		public readonly Dictionary<int, int> map = new Dictionary<int, int>();
	}

	private sealed class Welder
	{
		public readonly List<Vector3> verts = new List<Vector3>();

		public readonly List<int> tris = new List<int>();

		private readonly Dictionary<long, int> _map = new Dictionary<long, int>();

		private readonly Dictionary<long, List<int>> _coll = new Dictionary<long, List<int>>();

		public void Add(Vector3 v)
		{
			long num = (long)BitConverter.SingleToInt32Bits(v.z) * 83492791L ^ (long)BitConverter.SingleToInt32Bits(v.x) * 73856093L ^ (long)BitConverter.SingleToInt32Bits(v.y) * 19349663L;
			if (!_map.TryGetValue(num, out var value))
			{
				int num2 = verts.Count;
				verts.Add(v);
				_map.Add(num, num2);
				tris.Add(num2);
				return;
			}
			if (Same(verts[value], v))
			{
				tris.Add(value);
				return;
			}
			List<int> list = null;
			if (_coll.TryGetValue(num, out list))
			{
				foreach (int item in list)
				{
					if (Same(verts[item], v))
					{
						tris.Add(item);
						return;
					}
				}
			}
			int num3 = verts.Count;
			verts.Add(v);
			if (list == null)
			{
				list = new List<int>();
				_coll.Add(num, list);
			}
			list.Add(num3);
			tris.Add(num3);
		}

		private static bool Same(Vector3 a, Vector3 b)
		{
			return BitConverter.SingleToInt32Bits(a.x) == BitConverter.SingleToInt32Bits(b.x) && BitConverter.SingleToInt32Bits(a.y) == BitConverter.SingleToInt32Bits(b.y) && BitConverter.SingleToInt32Bits(a.z) == BitConverter.SingleToInt32Bits(b.z);
		}
	}

	public static long MaxFileBytes = int.MaxValue;

	public static int MaxTrianglesPerPart = 715827882;

	public static int MaxParts = 1000000;

	public const float MaxAbsCoord = 1000000f;

	public static bool SabotageRawThrow;

	public const string KeyFormat = "model.import.format";

	public const string KeyTooBig = "model.import.too_big";

	public const string KeyIo = "model.import.io";

	public const string KeyEmpty = "model.import.empty";

	public const string KeyPartsCap = "model.import.parts_cap";

	public const string KeyTrisCap = "model.import.tris_cap";

	public const string KeyParse = "model.import.parse";

	public const string KeyIndex = "model.import.index";

	public const string KeyFace = "model.import.face";

	public const string KeyNumber = "model.import.number";

	public const string KeyStlSize = "model.import.stl_size";

	public const string KeyStlAscii = "model.import.stl_ascii";

	public const string KeyDegenerate = "model.import.degenerate";

	public static readonly string[] AllKeys = new string[13]
	{
		KeyFormat, KeyTooBig, KeyIo, KeyEmpty, KeyPartsCap, KeyTrisCap, KeyParse, KeyIndex, KeyFace, KeyNumber, KeyStlSize, KeyStlAscii, KeyDegenerate
	};

	public static string ModelsFolder
	{
		get
		{
			string text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (string.IsNullOrEmpty(text))
			{
				text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents");
			}
			return Path.Combine(text, "ArmorSim", "Models");
		}
	}

	public static bool IsSupportedExtension(string path)
	{
		string text = (Path.GetExtension(path) ?? "").ToLowerInvariant();
		return text == ".obj" || text == ".stl";
	}

	private static ModelImportException Fail(string key, string prose, params object[] args)
	{
		return new ModelImportException(key, args ?? new object[0], prose);
	}

	public static ImportedModel Load(IList<string> paths, Action<float> progress = null, CancellationToken ct = default(CancellationToken))
	{
		if (paths == null || paths.Count == 0)
		{
			throw Fail(KeyEmpty, "Не выбрано ни одного файла модели.", "—");
		}
		ImportedModel importedModel = new ImportedModel();
		for (int i = 0; i < paths.Count; i++)
		{
			ct.ThrowIfCancellationRequested();
			string path = paths[i];
			string fileName = Path.GetFileName(path ?? "");
			if (!IsSupportedExtension(path))
			{
				throw Fail(KeyFormat, "Файл «" + fileName + "»: формат не поддерживается — только .obj и .stl.", fileName);
			}
			if (paths.Count > 1 && (Path.GetExtension(path) ?? "").ToLowerInvariant() != ".stl")
			{
				throw Fail(KeyFormat, "Файл «" + fileName + "»: несколько файлов собираются в модель только из STL (у OBJ части — его группы).", fileName);
			}
			FileInfo fileInfo = new FileInfo(path);
			if (!fileInfo.Exists)
			{
				throw Fail(KeyIo, "Файл «" + fileName + "» не найден.", fileName, "not found");
			}
			if (fileInfo.Length > MaxFileBytes)
			{
				throw Fail(KeyTooBig, string.Format("Файл «{0}» {1:F1} МБ больше потолка {2} МБ.", fileName, fileInfo.Length / 1048576.0, MaxFileBytes >> 20), fileName, fileInfo.Length / 1048576.0, MaxFileBytes >> 20);
			}
			byte[] array;
			try
			{
				array = File.ReadAllBytes(path);
			}
			catch (Exception ex)
			{
				throw Fail(KeyIo, "Файл «" + fileName + "» не читается: " + ex.Message, fileName, ex.GetType().Name);
			}
			ImportedModel importedModel2 = Parse(fileName, array, (progress != null) ? (Action<float>)((float x) => progress((i + x) / paths.Count)) : null, ct);
			foreach (ImportedPart part in importedModel2.parts)
			{
				if (importedModel.parts.Count >= MaxParts)
				{
					throw Fail(KeyPartsCap, string.Format("В модели больше {0} частей.", MaxParts), importedModel.parts.Count + 1, MaxParts);
				}
				importedModel.parts.Add(part);
			}
			importedModel.sources.Add(new ModelSourceFile
			{
				path = path,
				sha256 = Sha256Hex(array),
				bytes = array.Length
			});
		}
		importedModel.ContentSha = CombinedSha(importedModel.sources);
		return importedModel;
	}

	public static string Sha256Hex(byte[] data)
	{
		using (SHA256 sHA = SHA256.Create())
		{
			return BitConverter.ToString(sHA.ComputeHash(data ?? new byte[0])).Replace("-", "");
		}
	}

	public static string CombinedSha(IList<ModelSourceFile> sources)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ModelSourceFile source in sources)
		{
			stringBuilder.Append(source.sha256);
			stringBuilder.Append(';');
		}
		return Sha256Hex(Encoding.ASCII.GetBytes(stringBuilder.ToString()));
	}

	public static ImportedModel Parse(string name, byte[] data, Action<float> progress = null, CancellationToken ct = default(CancellationToken))
	{
		if (data == null)
		{
			throw Fail(KeyIo, "Пустые данные.", name, "null");
		}
		if (data.Length > MaxFileBytes)
		{
			throw Fail(KeyTooBig, "«" + name + "» больше потолка.", name, data.Length / 1048576.0, MaxFileBytes >> 20);
		}
		string text = (Path.GetExtension(name ?? "") ?? "").ToLowerInvariant();
		ImportedModel importedModel;
		if (text == ".stl")
		{
			if (LooksBinaryStl(data))
			{
				importedModel = ParseStlBinary(name, data, progress, ct);
			}
			else if (StartsWithSolid(data))
			{
				importedModel = ParseStlAscii(name, data, progress, ct);
			}
			else if (data.Length < 84)
			{
				throw Fail(KeyStlSize, string.Format("«{0}»: файл STL короче заголовка ({1} Б < 84).", name, data.Length), name, 0L, (long)data.Length);
			}
			else
			{
				uint num = BitConverter.ToUInt32(data, 80);
				throw Fail(KeyStlSize, string.Format("«{0}»: двоичный STL объявляет {1} треугольников, а длина файла {2} Б ", name, num, data.Length) + string.Format("(нужно {0} Б); текстовым он тоже не начинается («solid»).", num * 50 + 84), name, (long)num, (long)data.Length);
			}
		}
		else if (text == ".obj")
		{
			importedModel = ParseObj(name, data, progress, ct);
		}
		else
		{
			throw Fail(KeyFormat, "«" + name + "»: формат не поддерживается — только .obj и .stl.", name);
		}
		if (importedModel.parts.Count == 0)
		{
			throw Fail(KeyEmpty, "«" + name + "»: в файле нет ни одного треугольника.", name);
		}
		foreach (ImportedPart part in importedModel.parts)
		{
			CheckPart(name, part);
		}
		importedModel.sources.Add(new ModelSourceFile
		{
			path = name,
			sha256 = Sha256Hex(data),
			bytes = data.Length
		});
		importedModel.ContentSha = CombinedSha(importedModel.sources);
		return importedModel;
	}

	private static void CheckPart(string file, ImportedPart p)
	{
		for (int i = 0; i < p.vertices.Length; i++)
		{
			Vector3 vector = p.vertices[i];
			if (!(Math.Abs(vector.x) <= MaxAbsCoord) || !(Math.Abs(vector.y) <= MaxAbsCoord) || !(Math.Abs(vector.z) <= MaxAbsCoord))
			{
				throw Fail(KeyNumber, string.Format("«{0}», часть «{1}»: координата {2} вне ±{3:G} единиц файла.", file, p.name, vector.x, MaxAbsCoord), file, p.name, 0);
			}
		}
		bool flag = false;
		for (int j = 0; j < p.triangles.Length; j += 3)
		{
			Vector3 a = p.vertices[p.triangles[j]];
			Vector3 b = p.vertices[p.triangles[j + 1]];
			Vector3 c = p.vertices[p.triangles[j + 2]];
			Vector3 vector2 = Vector3.Cross(b - a, c - a);
			if (vector2.sqrMagnitude > 0f)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			throw Fail(KeyDegenerate, string.Format("«{0}», часть «{1}»: все {2} треугольников вырождены (нулевая площадь).", file, p.name, p.TriangleCount), p.name, p.TriangleCount);
		}
	}

	private static ImportedModel ParseObj(string name, byte[] data, Action<float> progress, CancellationToken ct)
	{
		string text;
		try
		{
			text = new UTF8Encoding(false, false).GetString(data);
		}
		catch (DecoderFallbackException ex)
		{
			throw Fail(KeyParse, "«" + name + "»: текст не читается как UTF-8 (" + ex.Message + ")", 0, "utf8");
		}
		List<Vector3> verts = new List<Vector3>();
		List<PartBuilder> parts = new List<PartBuilder>();
		Dictionary<string, PartBuilder> map = new Dictionary<string, PartBuilder>(StringComparer.Ordinal);
		string text2 = Path.GetFileNameWithoutExtension(name ?? "model");
		List<int> face = new List<int>(8);
		PartBuilder part = null;
		int line = 0;
		int pos = 0;
		while (pos < text.Length)
		{
			int num = text.IndexOf('\n', pos);
			if (num < 0)
			{
				num = text.Length;
			}
			line++;
			string lineText = text.Substring(pos, num - pos).Trim();
			pos = num + 1;
			if (line % 16384 == 0)
			{
				ct.ThrowIfCancellationRequested();
				if (progress != null)
				{
					progress(pos * 0.9f / Math.Max(1, text.Length));
				}
			}
			if (lineText.Length == 0 || lineText[0] == '#')
			{
				continue;
			}
			string[] array = lineText.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			if (array[0] == "v")
			{
				if (array.Length < 4)
				{
					throw Fail(KeyParse, string.Format("«{0}», строка {1}: у вершины меньше трёх координат.", name, line), line, "v");
				}
				verts.Add(new Vector3(Num(array[1], name, line), Num(array[2], name, line), Num(array[3], name, line)));
			}
			else if (array[0] == "o" || array[0] == "g")
			{
				string text3 = ((array.Length > 1) ? string.Join(" ", array, 1, array.Length - 1) : text2);
				if (!map.TryGetValue(text3, out part))
				{
					part = new PartBuilder();
					part.name = text3;
					map[text3] = part;
					parts.Add(part);
				}
			}
			else if (array[0] == "f")
			{
				if (array.Length < 4)
				{
					throw Fail(KeyFace, string.Format("«{0}», строка {1}: у грани {2} вершин (нужно ⩾ 3).", name, line, array.Length - 1), line, array.Length - 1);
				}
				if (part == null)
				{
					if (!map.TryGetValue(text2, out part))
					{
						part = new PartBuilder();
						part.name = text2;
						map[text2] = part;
						parts.Add(part);
					}
				}
				face.Clear();
				for (int i = 1; i < array.Length; i++)
				{
					string text4 = array[i];
					int num2 = text4.IndexOf('/');
					if (num2 >= 0)
					{
						text4 = text4.Substring(0, num2);
					}
					int num3;
					if (!int.TryParse(text4, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out num3) || num3 == 0)
					{
						throw Fail(KeyParse, string.Format("«{0}», строка {1}: индекс вершины «{2}» не целое ненулевое число.", name, line, text4), line, text4);
					}
					int num4 = ((num3 > 0) ? (num3 - 1) : (verts.Count + num3));
					if (num4 < 0 || num4 >= verts.Count)
					{
						if (SabotageRawThrow)
						{
							throw new IndexOutOfRangeException("диверсия: сырое исключение вместо отказа по имени");
						}
						throw Fail(KeyIndex, string.Format("«{0}», строка {1}: индекс вершины {2} вне 1…{3}.", name, line, num3, verts.Count), line, num3, verts.Count);
					}
					if (!part.map.TryGetValue(num4, out var value))
					{
						value = part.verts.Count;
						part.verts.Add(verts[num4]);
						part.map[num4] = value;
					}
					face.Add(value);
				}
				if (part.tris.Count / 3 + (face.Count - 2) > MaxTrianglesPerPart)
				{
					throw Fail(KeyTrisCap, string.Format("«{0}»: в части «{1}» больше {2} треугольников.", name, part.name, MaxTrianglesPerPart), part.name, part.tris.Count / 3 + (face.Count - 2), MaxTrianglesPerPart);
				}
				for (int j = 2; j < face.Count; j++)
				{
					part.tris.Add(face[0]);
					part.tris.Add(face[j - 1]);
					part.tris.Add(face[j]);
				}
			}
			if (parts.Count > MaxParts)
			{
				throw Fail(KeyPartsCap, string.Format("«{0}»: частей больше {1}.", name, MaxParts), parts.Count, MaxParts);
			}
		}
		ImportedModel importedModel = new ImportedModel();
		foreach (PartBuilder item in parts)
		{
			if (item.tris.Count == 0)
			{
				continue;
			}
			ImportedPart importedPart = new ImportedPart();
			importedPart.name = item.name;
			importedPart.vertices = item.verts.ToArray();
			importedPart.triangles = item.tris.ToArray();
			importedModel.parts.Add(importedPart);
		}
		if (progress != null)
		{
			progress(1f);
		}
		return importedModel;
	}

	private static float Num(string s, string file, int line)
	{
		if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && !float.IsNaN(result) && !float.IsInfinity(result))
		{
			return result;
		}
		throw Fail(KeyNumber, string.Format("«{0}», строка {1}: «{2}» — не конечное число.", file, line, s), file, line, s);
	}

	private static bool LooksBinaryStl(byte[] d)
	{
		if (d == null || d.Length < 84)
		{
			return false;
		}
		return BitConverter.ToUInt32(d, 80) * 50 + 84 == d.Length;
	}

	private static bool StartsWithSolid(byte[] d)
	{
		int i = 0;
		while (i < d.Length && i < 256)
		{
			byte b = d[i];
			if (b != 32 && b != 9 && b != 13 && b != 10 && b != 239 && b != 187 && b != 191)
			{
				break;
			}
			i++;
		}
		return d.Length - i >= 5 && d[i] == 115 && d[i + 1] == 111 && d[i + 2] == 108 && d[i + 3] == 105 && d[i + 4] == 100;
	}

	private static ImportedModel ParseStlBinary(string name, byte[] d, Action<float> progress, CancellationToken ct)
	{
		uint num = BitConverter.ToUInt32(d, 80);
		if (num > MaxTrianglesPerPart)
		{
			throw Fail(KeyTrisCap, string.Format("«{0}»: в двоичном STL {1} треугольников при потолке {2}.", name, num, MaxTrianglesPerPart), Path.GetFileNameWithoutExtension(name ?? ""), Math.Min((int)num, int.MaxValue), MaxTrianglesPerPart);
		}
		Welder welder = new Welder();
		int num2 = 96;
		for (uint num3 = 0u; num3 < num; num3++)
		{
			if (num3 % 16384u == 0u)
			{
				ct.ThrowIfCancellationRequested();
				if (progress != null)
				{
					progress(num3 * 0.9f / Math.Max(1, num));
				}
			}
			for (int i = 0; i < 3; i++)
			{
				float x = BitConverter.ToSingle(d, num2);
				float y = BitConverter.ToSingle(d, num2 + 4);
				float z = BitConverter.ToSingle(d, num2 + 8);
				num2 += 12;
				if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z) || float.IsInfinity(x) || float.IsInfinity(y) || float.IsInfinity(z))
				{
					throw Fail(KeyNumber, string.Format("«{0}»: треугольник {1}, вершина {2} — не конечное число.", name, num3 + 1u, i + 1), name, (int)(num3 + 1u), "NaN/Inf");
				}
				welder.Add(new Vector3(x, y, z));
			}
			num2 += 14;
		}
		ImportedModel importedModel = new ImportedModel();
		if (welder.tris.Count > 0)
		{
			ImportedPart importedPart = new ImportedPart();
			importedPart.name = Path.GetFileNameWithoutExtension(name ?? "stl");
			importedPart.vertices = welder.verts.ToArray();
			importedPart.triangles = welder.tris.ToArray();
			importedModel.parts.Add(importedPart);
		}
		if (progress != null)
		{
			progress(1f);
		}
		return importedModel;
	}

	private static ImportedModel ParseStlAscii(string name, byte[] data, Action<float> progress, CancellationToken ct)
	{
		string text = new UTF8Encoding(false, false).GetString(data);
		ImportedModel importedModel = new ImportedModel();
		string text2 = Path.GetFileNameWithoutExtension(name ?? "stl");
		Welder welder = null;
		string text3 = null;
		int state = 0;
		int loopVerts = 0;
		int line = 0;
		int pos = 0;
		while (pos < text.Length)
		{
			int num = text.IndexOf('\n', pos);
			if (num < 0)
			{
				num = text.Length;
			}
			line++;
			string lineText = text.Substring(pos, num - pos).Trim();
			pos = num + 1;
			if (line % 16384 == 0)
			{
				ct.ThrowIfCancellationRequested();
				if (progress != null)
				{
					progress(pos * 0.9f / Math.Max(1, text.Length));
				}
			}
			if (lineText.Length == 0)
			{
				continue;
			}
			string[] array = lineText.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			switch (array[0].ToLowerInvariant())
			{
			case "outer":
				if (state != 2)
				{
					throw Fail(KeyStlAscii, string.Format("«{0}», строка {1}: «outer loop» вне грани.", name, line), line, "outer");
				}
				state = 3;
				loopVerts = 0;
				break;
			case "solid":
				if (state != 0)
				{
					throw Fail(KeyStlAscii, string.Format("«{0}», строка {1}: «solid» внутри тела.", name, line), line, "solid");
				}
				state = 1;
				welder = new Welder();
				text3 = ((array.Length > 1) ? string.Join(" ", array, 1, array.Length - 1) : text2);
				break;
			case "endfacet":
				if (state != 2)
				{
					throw Fail(KeyStlAscii, string.Format("«{0}», строка {1}: «endfacet» не на месте.", name, line), line, "endfacet");
				}
				state = 1;
				break;
			case "vertex":
				if (state != 3)
				{
					throw Fail(KeyStlAscii, string.Format("«{0}», строка {1}: «vertex» вне петли.", name, line), line, "vertex");
				}
				if (array.Length < 4)
				{
					throw Fail(KeyStlAscii, string.Format("«{0}», строка {1}: у вершины меньше трёх чисел.", name, line), line, "vertex");
				}
				loopVerts++;
				if (loopVerts > 3)
				{
					throw Fail(KeyStlAscii, string.Format("«{0}», строка {1}: в петле больше трёх вершин.", name, line), line, "vertex");
				}
				welder.Add(new Vector3(Num(array[1], name, line), Num(array[2], name, line), Num(array[3], name, line)));
				break;
			case "endloop":
				if (state != 3 || loopVerts != 3)
				{
					throw Fail(KeyStlAscii, string.Format("«{0}», строка {1}: «endloop» после {2} вершин (нужно ровно 3).", name, line, loopVerts), line, "endloop");
				}
				state = 2;
				if (welder.tris.Count / 3 > MaxTrianglesPerPart)
				{
					throw Fail(KeyTrisCap, string.Format("«{0}»: в теле «{1}» больше {2} треугольников.", name, text3, MaxTrianglesPerPart), text3, welder.tris.Count / 3, MaxTrianglesPerPart);
				}
				break;
			case "facet":
				if (state != 1)
				{
					throw Fail(KeyStlAscii, string.Format("«{0}», строка {1}: «facet» вне тела.", name, line), line, "facet");
				}
				state = 2;
				break;
			case "endsolid":
				if (state != 1)
				{
					throw Fail(KeyStlAscii, string.Format("«{0}», строка {1}: «endsolid» внутри грани.", name, line), line, "endsolid");
				}
				state = 0;
				if (welder.tris.Count > 0)
				{
					if (importedModel.parts.Count >= MaxParts)
					{
						throw Fail(KeyPartsCap, string.Format("«{0}»: тел больше {1}.", name, MaxParts), importedModel.parts.Count + 1, MaxParts);
					}
					ImportedPart importedPart = new ImportedPart();
					importedPart.name = text3;
					importedPart.vertices = welder.verts.ToArray();
					importedPart.triangles = welder.tris.ToArray();
					importedModel.parts.Add(importedPart);
				}
				welder = null;
				break;
			default:
				throw Fail(KeyStlAscii, string.Format("«{0}», строка {1}: неизвестное слово «{2}».", name, line, Short(array[0])), line, Short(array[0]));
			}
		}
		if (state != 0)
		{
			throw Fail(KeyStlAscii, "«" + name + "»: файл оборван внутри тела (нет «endsolid»).", line, "EOF");
		}
		if (progress != null)
		{
			progress(1f);
		}
		return importedModel;
	}

	private static string Short(string s)
	{
		if (s == null)
		{
			return "";
		}
		if (s.Length > 24)
		{
			return s.Substring(0, 24) + "…";
		}
		return s;
	}
}
