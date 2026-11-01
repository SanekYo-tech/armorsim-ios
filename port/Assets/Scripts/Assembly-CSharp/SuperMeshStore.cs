using System;
using System.Collections.Generic;
using System.IO;

public static class SuperMeshStore
{
	public const string KeyFileMissing = "super.s40.refuse.model_file";

	public const string KeyFileChanged = "super.s40.refuse.model_changed";

	private static readonly Dictionary<string, PreparedModel> _models = new Dictionary<string, PreparedModel>();

	public static void Register(PreparedModel m)
	{
		lock (_models)
		{
			_models[m.contentSha + "|" + m.settings.Key] = m;
		}
	}

	public static PreparedModel Get(SuperMeshSource src)
	{
		if (src == null)
		{
			throw new ArgumentNullException("src");
		}
		lock (_models)
		{
			if (_models.TryGetValue(src.contentSha + "|" + src.settings.Key, out PreparedModel value))
			{
				return value;
			}
		}
		List<string> list = new List<string>();
		foreach (ModelSourceFile item in src.files)
		{
			if (!File.Exists(item.path))
			{
				throw new ModelImportException(KeyFileMissing, new object[2] { item.path, Short(item.sha256) }, "Файл модели «" + item.path + "» не найден (ждали отпечаток " + item.sha256 + ").");
			}
			byte[] array;
			try
			{
				array = File.ReadAllBytes(item.path);
			}
			catch (Exception ex)
			{
				throw new ModelImportException(KeyFileMissing, new object[2] { item.path, Short(item.sha256) }, "Файл модели «" + item.path + "» не читается: " + ex.Message);
			}
			string text = ModelFileImporter.Sha256Hex(array);
			if (text != item.sha256)
			{
				throw new ModelImportException(KeyFileChanged, new object[3] { item.path, Short(item.sha256), Short(text) }, "Файл модели «" + item.path + "» сменился: ждали " + item.sha256 + ", на диске " + text + ".");
			}
			list.Add(item.path);
		}
		PreparedModel preparedModel = ModelImport.Prepare(ModelFileImporter.Load(list), src.settings);
		if (preparedModel.contentSha != src.contentSha)
		{
			throw new ModelImportException(KeyFileChanged, new object[3] { string.Join(";", list), Short(src.contentSha), Short(preparedModel.contentSha) }, "Отпечаток модели разошёлся: ждали " + src.contentSha + ", собрано " + preparedModel.contentSha + ".");
		}
		Register(preparedModel);
		return preparedModel;
	}

	public static string Short(string sha)
	{
		if (string.IsNullOrEmpty(sha))
		{
			return "—";
		}
		if (sha.Length > 12)
		{
			return sha.Substring(0, 12);
		}
		return sha;
	}

	public static void ForgetAll()
	{
		lock (_models)
		{
			_models.Clear();
		}
	}
}
