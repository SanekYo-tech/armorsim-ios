using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public sealed class RadarAtlasLibrary
{
	public sealed class Job
	{
		public string ModelKey;

		public string Key;

		public double FrequencyHz;

		public RadarAtlasSpec Spec;

		public RadarCoating Coating;

		public readonly RadarAtlasCounter Done = new RadarAtlasCounter();

		public int Total;

		public Task<RadarAtlas> Task;

		public CancellationTokenSource Cancel;

		public double EstimateSeconds = double.NaN;

		public RadarAtlasBakeStats Stats;
	}

	public static readonly double[] BuiltinFrequencies = new double[6] { 150000000.0, 1300000000.0, 3000000000.0, 5500000000.0, 9500000000.0, 10000000000.0 };

	public const double BuiltinSalisburyF0 = 10000000000.0;

	public const string ResourceFolder = "Atlas";

	private readonly Dictionary<string, RadarAtlas> loaded;

	public string CacheFolder;

	public string LastError { get; private set; }

	public RadarAtlasLibrary(string cacheFolder = null)
	{
		loaded = new Dictionary<string, RadarAtlas>(StringComparer.Ordinal);
		CacheFolder = cacheFolder ?? RadarAtlasStore.CacheFolder;
	}

	public static string ResourceName(double frequencyHz, RadarCoating coating = null)
	{
		string text = ((coating == null || coating.IsMetal) ? "/yf23_" : "/yf23_sal10_");
		return "Atlas" + text + Math.Round(frequencyHz / 1000000000.0).ToString("0", CultureInfo.InvariantCulture);
	}

	public static bool IsBuiltin(string modelKey, double frequencyHz, RadarAtlasSpec spec, RadarCoating coating = null)
	{
		bool flag = coating == null || coating.IsMetal || (coating.Id == RadarCoating.Kind.Salisbury && coating.F0Hz == 10000000000.0);
		if (modelKey != "yf23" || spec.DeltaDeg != 2.0 || spec.SamplesPerSide != 6 || spec.Seed != 5927093631938940209uL || spec.SolverTag != (flag ? "PO1-frozen-v1" : "PO1-frozen-coat-v1"))
		{
			return false;
		}
		for (int i = 0; i < BuiltinFrequencies.Length; i++)
		{
			if (BuiltinFrequencies[i] == frequencyHz)
			{
				return true;
			}
		}
		return false;
	}

	public RadarAtlas TryGet(string modelKey, RadarScene scene, double frequencyHz, RadarAtlasSpec spec, out string key, RadarCoating coating = null)
	{
		LastError = null;
		key = null;
		try
		{
			spec = spec.For(coating);
			key = RadarAtlasKey.Of(scene, frequencyHz, spec, coating);
			if (loaded.TryGetValue(key, out RadarAtlas value))
			{
				return value;
			}
			if (IsBuiltin(modelKey, frequencyHz, spec, coating))
			{
				TextAsset textAsset = Resources.Load<TextAsset>(ResourceName(frequencyHz, coating));
				if (textAsset != null)
				{
					value = RadarAtlasStore.Deserialize(textAsset.bytes, key);
					Resources.UnloadAsset(textAsset);
					loaded[key] = value;
					return value;
				}
			}
			string path = Path.Combine(CacheFolder, key + ".atlas");
			if (File.Exists(path))
			{
				value = RadarAtlasStore.Deserialize(File.ReadAllBytes(path), key);
				loaded[key] = value;
				return value;
			}
			return null;
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			Debug.LogError(string.Concat(new object[8]
			{
				"[RADAR ATLAS] ",
				scene,
				" @ ",
				frequencyHz.ToString(),
				" Hz (",
				coating?.Describe() ?? "metal",
				"): ",
				ex.Message
			}));
			return null;
		}
	}

	public void Remember(RadarAtlas atlas)
	{
		if (atlas != null && atlas.Header != null && atlas.Header.key != null)
		{
			loaded[atlas.Header.key] = atlas;
		}
	}

	public Job StartBake(string modelKey, RadarScene scene, double frequencyHz, RadarAtlasSpec spec, RadarMapModelRecord model, bool estimateFirst, RadarCoating coating = null)
	{
		spec = spec.For(coating);
		Job job = new Job
		{
			ModelKey = modelKey,
			FrequencyHz = frequencyHz,
			Spec = spec,
			Coating = coating,
			Total = spec.CellCount,
			Key = RadarAtlasKey.Of(scene, frequencyHz, spec, coating),
			Cancel = new CancellationTokenSource()
		};
		CancellationToken token = job.Cancel.Token;
		string folder = CacheFolder;
		int threads = Math.Max(1, Environment.ProcessorCount - 1);
		job.Task = Task.Run(() =>
		{
			if (estimateFirst)
			{
				job.EstimateSeconds = RadarAtlasBaker.EstimateSeconds(scene, frequencyHz, spec, threads, token, coating);
			}
			RadarAtlasBakeStats[] array;
			RadarAtlas radarAtlas = RadarAtlasBaker.BakeMany(scene, spec, new(double, RadarCoating)[1] { (frequencyHz, coating) }, threads, token, job.Done, out array)[0];
			job.Stats = array[0];
			FillModel(radarAtlas.Header, model);
			string path = Path.Combine(folder, radarAtlas.Header.key + ".atlas");
			byte[] bytes = RadarAtlasStore.Serialize(radarAtlas);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			string text = path + ".tmp";
			File.WriteAllBytes(text, bytes);
			if (File.Exists(path))
			{
				File.Replace(text, path, null);
			}
			else
			{
				File.Move(text, path);
			}
			return radarAtlas;
		}, token);
		job.Task.ContinueWith(delegate(Task<RadarAtlas> t)
		{
			_ = t.Exception;
		}, TaskContinuationOptions.OnlyOnFaulted);
		return job;
	}

	public static void FillModel(RadarAtlasHeader header, RadarMapModelRecord model)
	{
		if (model != null)
		{
			header.modelKey = model.Key;
			header.modelSha256 = model.FileSha256;
			header.modelUnit = model.Unit;
			header.modelLengthM = model.LongestSideM;
			header.modelOrientation = (int[])model.Orientation.Clone();
		}
	}

	public static string Note(RadarAtlas atlas)
	{
		return string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.atlas.note"), atlas.Spec.DeltaDeg.ToString("0.#", CultureInfo.InvariantCulture), CoatingName(atlas.Header?.coating));
	}

	public static string CoatingName(RadarAtlasCoatingInfo info)
	{
		if (info == null || info.id == "metal" || string.IsNullOrEmpty(info.id))
		{
			return RadarScenarioText.Get("map.atlas.metal");
		}
		string text = (info.f0Hz / 1000000000.0).ToString("0.###", CultureInfo.InvariantCulture);
		if (info.id == "salisbury")
		{
			return string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.coat.salisbury"), text);
		}
		if (info.id == "jaumann")
		{
			return string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.coat.jaumann"), text);
		}
		return RadarScenarioText.Get("map.coat.custom");
	}
}
