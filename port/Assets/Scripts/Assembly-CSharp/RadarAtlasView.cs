using Object = UnityEngine.Object;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public sealed class RadarAtlasView : MonoBehaviour
{
	private enum Phase
	{
		Idle = 0,
		Estimating = 1,
		Confirm = 2,
		Baking = 3
	}

	public RadarBenchController Bench;

	public RadarAtlasLibrary Library;

	public double RangeDb;

	private RadarModel sceneModel;

	private Task<RadarScene> sceneTask;

	private RadarScene scene;

	private double atlasFrequency;

	private RadarCoating atlasCoating;

	private Texture2D texture;

	private double textureTop;

	private double textureRange;

	private RadarAtlas textureAtlas;

	private Phase phase;

	private RadarAtlasSpec pendingSpec;

	private Task<double> estimateTask;

	private RadarAtlasLibrary.Job job;

	private CancellationTokenSource estimateCancel;

	private GUISkin skin;

	private GUIStyle title;

	private GUIStyle text;

	private GUIStyle small;

	private GUIStyle bold;

	private readonly double[] slice;

	private const double FrontLimit = 30.0;

	private const double SideLow = 60.0;

	private const double SideHigh = 120.0;

	private const double RearLimit = 150.0;

	public RadarAtlas Atlas { get; private set; }

	public string StateLine { get; private set; }

	public double LastEstimateSeconds { get; private set; }

	public RadarAtlasBakeStats LastBakeStats { get; private set; }

	public double FrontMedianDb { get; private set; }

	public double SideMedianDb { get; private set; }

	public double RearMedianDb { get; private set; }

	private bool Builtin => Bench != null && Bench.referenceNameKey == "radar.model.yf23" && Bench.imported != null;

	private string ModelKey => Builtin ? "yf23" : null;

	private double SizeInWavelengths
	{
		get
		{
			if (scene == null)
			{
				return 0.0;
			}
			return scene.LongestSide / (299792458.0 / Bench.State.FrequencyHz);
		}
	}

	public bool Busy => phase == Phase.Estimating || phase == Phase.Baking;

	private static string L(string key)
	{
		return RadarScenarioText.Get(key);
	}

	private static string F(double v, string f)
	{
		return v.ToString(f, CultureInfo.InvariantCulture);
	}

	public RadarAtlasView()
	{
		Library = new RadarAtlasLibrary();
		RangeDb = 40.0;
		StateLine = "";
		LastEstimateSeconds = double.NaN;
		FrontMedianDb = double.NaN;
		SideMedianDb = double.NaN;
		RearMedianDb = double.NaN;
		atlasFrequency = double.NaN;
		slice = new double[180];
	}

	private void Update()
	{
		if (Bench == null || Bench.Model == null || Bench.State == null || Bench.ScenarioOpen)
		{
			return;
		}
		if (sceneModel != Bench.Model)
		{
			RadarModel model = Bench.Model;
			sceneModel = model;
			scene = null;
			Atlas = null;
			atlasFrequency = double.NaN;
			sceneTask = Task.Run(() => new RadarScene(model));
		}
		if (scene == null && sceneTask != null && sceneTask.IsCompleted)
		{
			if (sceneTask.Status == TaskStatus.RanToCompletion)
			{
				scene = sceneTask.Result;
			}
			sceneTask = null;
		}
		if (scene == null)
		{
			StateLine = L("map.atlas.state.none");
			return;
		}
		double frequencyHz = Bench.State.FrequencyHz;
		if ((atlasFrequency != frequencyHz || atlasCoating != Bench.BenchCoating) && phase != Phase.Baking)
		{
			atlasFrequency = frequencyHz;
			atlasCoating = Bench.BenchCoating;
			Atlas = Library.TryGet(ModelKey, scene, frequencyHz, RadarAtlasSpec.Standard, out string key, atlasCoating);
			if (Atlas == null)
			{
				Atlas = Library.TryGet(ModelKey, scene, frequencyHz, RadarAtlasSpec.Draft, out key, atlasCoating);
			}
		}
		PollJobs();
		RefreshState();
	}

	private RadarAtlas Lookup(double f)
	{
		return Library.TryGet(ModelKey, scene, f, RadarAtlasSpec.Standard, out string key, atlasCoating) ?? Library.TryGet(ModelKey, scene, f, RadarAtlasSpec.Draft, out key, atlasCoating);
	}

	private void RefreshState()
	{
		double value = 0.0;
		if (scene != null)
		{
			value = scene.LongestSide / (299792458.0 / Bench.State.FrequencyHz);
		}
		if (value < 3.0)
		{
			StateLine = string.Format(CultureInfo.InvariantCulture, L("map.atlas.state.refused"), F(value, "F2"));
			return;
		}
		switch (phase)
		{
		case Phase.Estimating:
			StateLine = string.Format(CultureInfo.InvariantCulture, L("map.atlas.eta"), "…");
			return;
		case Phase.Confirm:
			StateLine = string.Format(CultureInfo.InvariantCulture, L("map.atlas.state.slow"), RadarAtlasBaker.FormatDuration(LastEstimateSeconds));
			return;
		case Phase.Baking:
		{
			string text = string.Format(CultureInfo.InvariantCulture, L("map.atlas.state.baking"), job.Done.Value, job.Total, RadarAtlasBaker.DefaultThreads);
			if (!double.IsNaN(LastEstimateSeconds))
			{
				text = text + " · " + string.Format(CultureInfo.InvariantCulture, L("map.atlas.eta"), RadarAtlasBaker.FormatDuration(LastEstimateSeconds));
			}
			StateLine = text;
			return;
		}
		}
		if (Atlas == null)
		{
			StateLine = L("map.atlas.state.none");
		}
		else
		{
			string text2 = "";
			if (Atlas.Header != null && Atlas.Header.bakeSeconds > 0.0)
			{
				text2 = RadarAtlasBaker.FormatDuration(Atlas.Header.bakeSeconds) + " · " + Atlas.Header.threads.ToString() + " thr";
			}
			StateLine = string.Format(CultureInfo.InvariantCulture, L("map.atlas.state.ready"), Atlas.Spec, Atlas.CellCount, text2);
		}
		if (Library != null && Library.LastError != null)
		{
			StateLine = StateLine + " · " + Library.LastError;
		}
	}

	public void RequestBake(RadarAtlasSpec spec)
	{
		if (scene == null || scene.LongestSide / (299792458.0 / Bench.State.FrequencyHz) < 3.0)
		{
			return;
		}
		if (phase == Phase.Confirm && pendingSpec != null && pendingSpec.DeltaDeg == spec.DeltaDeg)
		{
			StartBake(spec);
			return;
		}
		if (phase != 0 && phase != Phase.Confirm)
		{
			return;
		}
		pendingSpec = spec;
		phase = Phase.Estimating;
		estimateCancel = new CancellationTokenSource();
		RadarScene s = scene;
		double f = Bench.State.FrequencyHz;
		CancellationToken token = estimateCancel.Token;
		RadarCoating coating = atlasCoating;
		estimateTask = Task.Run(() => RadarAtlasBaker.EstimateSeconds(s, f, spec.For(coating), RadarAtlasBaker.DefaultThreads, token, coating), token);
	}

	private void StartBake(RadarAtlasSpec spec)
	{
		RadarMapModelRecord model = (Builtin ? RadarMapModelRegistry.Yf23Record() : null);
		job = Library.StartBake(ModelKey, scene, Bench.State.FrequencyHz, spec, model, estimateFirst: false, atlasCoating);
		phase = Phase.Baking;
	}

	public void CancelBake()
	{
		if (estimateCancel != null)
		{
			estimateCancel.Cancel();
		}
		if (job != null)
		{
			job.Cancel.Cancel();
		}
		phase = Phase.Idle;
	}

	private void PollJobs()
	{
		if (phase == Phase.Estimating && estimateTask != null && estimateTask.IsCompleted)
		{
			if (estimateTask.Status == TaskStatus.RanToCompletion)
			{
				LastEstimateSeconds = estimateTask.Result;
				Debug.Log("[RADAR ATLAS] pilot estimate " + F(LastEstimateSeconds, "F1") + " s for " + pendingSpec);
				if (LastEstimateSeconds > 1800.0 && pendingSpec.DeltaDeg < 5.0)
				{
					phase = Phase.Confirm;
				}
				else
				{
					StartBake(pendingSpec);
				}
			}
			else
			{
				phase = Phase.Idle;
			}
			estimateTask = null;
		}
		if (phase != Phase.Baking || job == null || !job.Task.IsCompleted)
		{
			return;
		}
		if (job.Task.Status == TaskStatus.RanToCompletion)
		{
			Atlas = job.Task.Result;
			Library.Remember(Atlas);
			LastBakeStats = job.Stats;
			Debug.Log("[RADAR ATLAS] bench bake " + Atlas.Spec + " @ " + F(Atlas.FrequencyHz / 1e9, "F3") + " GHz: " + job.Stats);
		}
		else
		{
			Debug.LogWarning("[RADAR ATLAS] bench bake ended: " + job.Task.Status.ToString() + " " + job.Task.Exception?.GetBaseException().ToString());
		}
		job = null;
		phase = Phase.Idle;
	}

	private void EnsureStyles()
	{
		if (skin != null)
		{
			return;
		}
		skin = Object.Instantiate(GUI.skin);
		skin.font = Resources.Load<Font>("RadarScenarioSans");
		if (skin.font == null)
		{
			throw new InvalidOperationException("Missing RadarScenarioSans font");
		}
		title = new GUIStyle(skin.label);
		title.font = skin.font;
		title.fontSize = 26;
		title.fontStyle = FontStyle.Bold;
		title.normal.textColor = new Color(0.12f, 0.15f, 0.17f, 1f);
		text = new GUIStyle(skin.label);
		text.font = skin.font;
		text.fontSize = 18;
		text.wordWrap = true;
		text.normal.textColor = new Color(0.12f, 0.15f, 0.17f, 1f);
		small = new GUIStyle(text);
		small.fontSize = 15;
		bold = new GUIStyle(text);
		bold.fontStyle = FontStyle.Bold;
		skin.label.fontSize = 17;
	}

	private void OnGUI()
	{
		if (Bench == null || Bench.State == null || Bench.ScenarioOpen)
		{
			return;
		}
		EnsureStyles();
		Matrix4x4 matrix = GUI.matrix;
		GUISkin gUISkin = GUI.skin;
		Color color = GUI.color;
		try
		{
			float num = Mathf.Min((float)Screen.height / 1080f, (float)Screen.width / 1920f);
			float num2 = ((float)Screen.height - num * 1080f) * 0.5f;
			float num3 = ((float)Screen.width - num * 1920f) * 0.5f;
			GUI.matrix = Matrix4x4.TRS(new Vector3(num3, num2, 0f), Quaternion.identity, Vector3.one * num);
			GUI.skin = skin;
			DrawPanel(new Rect(18f, 88f, 1270f, 912f));
		}
		finally
		{
			GUI.matrix = matrix;
			GUI.skin = gUISkin;
			GUI.color = color;
		}
	}

	private void DrawPanel(Rect area)
	{
		RadarScenarioMapDraw.Fill(area, new Color(0.88f, 0.89f, 0.9f, 0.97f));
		RadarScenarioMapDraw.Outline(area, new Color(0.55f, 0.58f, 0.6f, 1f), 1f);
		string text = RadarBands.FormatFrequency(Bench.State.FrequencyHz, Localization.T("radar.freq.mhz"), Localization.T("radar.freq.ghz"));
		GUI.Label(new Rect(area.x + 22f, area.y + 8f, 1200f, 36f), L("map.atlas.title") + " · " + ((Bench.referenceNameKey != null) ? Localization.T(Bench.referenceNameKey) : Bench.currentName) + " · " + text, title);
		double num = ((Atlas != null) ? Atlas.Spec.DeltaDeg : 2.0);
		string text2 = string.Format(CultureInfo.InvariantCulture, L("map.atlas.note"), num.ToString("0.#", CultureInfo.InvariantCulture), RadarMapCoatings.Name(Bench.BenchCoating)) + ((Atlas != null) ? (" · M = " + Atlas.Spec.Samples) : "");
		double num2 = 0.0;
		if (scene != null)
		{
			num2 = scene.LongestSide / (299792458.0 / Bench.State.FrequencyHz);
		}
		if (3.0 <= num2 && num2 < 20.0)
		{
			text2 = text2 + " · " + string.Format(CultureInfo.InvariantCulture, L("map.atlas.boundary"), F(num2, "F1"));
		}
		GUI.Label(new Rect(area.x + 22f, area.y + 46f, 1204f, 28f), text2, bold);
		Rect map = new Rect(area.x + 70f, area.y + 90f, 1000f, 500f);
		RadarScenarioMapDraw.Fill(map, RadarAtlasPalette.Below);
		if (Atlas != null)
		{
			if (texture == null || textureAtlas != Atlas || textureRange != RangeDb)
			{
				if (texture != null)
				{
					Object.Destroy(texture);
				}
				texture = RadarAtlasPalette.Texture(Atlas, RangeDb, out textureTop);
				textureAtlas = Atlas;
				textureRange = RangeDb;
				ComputeSlicesAndMedians();
			}
			GUI.DrawTexture(map, texture);
		}
		RadarScenarioMapDraw.Outline(map, new Color(0.25f, 0.28f, 0.3f, 1f), 1.5f);
		Axes(map);
		ColorBar(new Rect(map.x + 1034f, map.y, 30f, 500f));
		Marker(map);
		Cursor(map);
		Slice(new Rect(map.x, map.y + 556f, 1000f, 130f));
		float num3 = map.y + 696f;
		if (Atlas != null)
		{
			GUI.Label(new Rect(area.x + 22f, num3, 1204f, 28f), string.Format(CultureInfo.InvariantCulture, L("map.atlas.sectors"), FrontMedianDb.ToString("F1", CultureInfo.InvariantCulture), SideMedianDb.ToString("F1", CultureInfo.InvariantCulture), RearMedianDb.ToString("F1", CultureInfo.InvariantCulture)));
		}
		num3 += 30f;
		GUI.enabled = num2 >= 3.0 && scene != null && phase != Phase.Estimating && phase != Phase.Baking;
		if (GUI.Button(new Rect(area.x + 22f, num3, 200f, 38f), L("map.atlas.bake")))
		{
			RequestBake(RadarAtlasSpec.Standard);
		}
		if (GUI.Button(new Rect(area.x + 232f, num3, 200f, 38f), L("map.atlas.draft")))
		{
			RequestBake(RadarAtlasSpec.Draft);
		}
		GUI.enabled = phase == Phase.Estimating || phase == Phase.Baking || phase == Phase.Confirm;
		if (GUI.Button(new Rect(area.x + 442f, num3, 160f, 38f), L("map.atlas.cancel")))
		{
			if (estimateCancel != null)
			{
				estimateCancel.Cancel();
			}
			if (job != null)
			{
				job.Cancel.Cancel();
			}
			phase = Phase.Idle;
		}
		GUI.enabled = true;
		double[] array = new double[2] { 40.0, 60.0 };
		foreach (double num4 in array)
		{
			Color backgroundColor = GUI.backgroundColor;
			if (RangeDb == num4)
			{
				GUI.backgroundColor = new Color(0.44f, 0.7f, 0.93f, 1f);
			}
			if (GUI.Button(new Rect(area.x + ((num4 == 40.0) ? 640 : 760), num3, 110f, 38f), string.Format(CultureInfo.InvariantCulture, L("map.atlas.range"), F(num4, "F0"))))
			{
				RangeDb = num4;
			}
			GUI.backgroundColor = backgroundColor;
		}
		GUI.Label(new Rect(area.x + 22f, num3 + 44f, 1204f, 48f), StateLine, small);
	}

	private void Axes(Rect map)
	{
		float num = map.y + map.height;
		for (int i = -180; i <= 180; i += 45)
		{
			float x = (float)(i + 180) / 360f * map.width + map.x;
			RadarScenarioMapDraw.Line(new Vector2(x, num), new Vector2(x, num + 6f), Color.black, 1.5f);
			GUIStyle gUIStyle = new GUIStyle(small)
			{
				alignment = TextAnchor.UpperCenter
			};
			GUI.Label(new Rect(x - 30f, num + 6f, 60f, 22f), i.ToString() + "°", gUIStyle);
		}
		for (int j = -90; j <= 90; j += 30)
		{
			float y = num - (float)(j + 90) / 180f * map.height;
			RadarScenarioMapDraw.Line(new Vector2(map.x - 6f, y), new Vector2(map.x, y), Color.black, 1.5f);
			GUIStyle gUIStyle2 = new GUIStyle(small)
			{
				alignment = TextAnchor.UpperRight
			};
			GUI.Label(new Rect(map.x - 62f, y - 11f, 54f, 22f), j.ToString() + "°", gUIStyle2);
		}
		GUIStyle gUIStyle3 = new GUIStyle(small)
		{
			alignment = TextAnchor.UpperCenter
		};
		GUI.Label(new Rect(map.x, num + 28f, map.width, 22f), L("map.atlas.axes") + " · " + L("map.atlas.marker"), gUIStyle3);
	}

	private void ColorBar(Rect bar)
	{
		GUI.Label(new Rect(bar.x - 20f, bar.y - 30f, 150f, 28f), L("map.atlas.scale"), bold);
		double num = ((Atlas != null) ? textureTop : 0.0);
		float num2 = bar.height / 11f;
		for (int i = 0; i <= 10; i++)
		{
			float num3 = bar.y + bar.height - (float)(i + 1) * num2;
			RadarScenarioMapDraw.Fill(new Rect(bar.x, num3, bar.width, num2), RadarAtlasPalette.Step((float)i / 10f));
			if (i == 1 && Atlas != null)
			{
				GUI.Label(new Rect(bar.x + bar.width + 6f, num3 + num2 * 0.5f - 11f, 80f, 22f), F(num - RangeDb + (double)i * RangeDb / 10.0, "F0"), small);
			}
		}
		RadarScenarioMapDraw.Fill(new Rect(bar.x, bar.y + bar.height + 8f, bar.width, 16f), RadarAtlasPalette.Below);
		if (Atlas != null)
		{
			GUI.Label(new Rect(bar.x + bar.width + 6f, bar.y + bar.height + 5f, 110f, 22f), "< " + (num - RangeDb).ToString("F0", CultureInfo.InvariantCulture), small);
		}
		RadarScenarioMapDraw.Outline(bar, Color.black, 1f);
	}

	private void Marker(Rect map)
	{
		Vector2 center = RadarAtlasPalette.Point(map, Bench.State.AzimuthDeg, Bench.State.ElevationDeg);
		RadarScenarioMapDraw.Line(new Vector2(center.x - 12f, center.y), new Vector2(center.x + 12f, center.y), Color.white, 4f);
		RadarScenarioMapDraw.Line(new Vector2(center.x, center.y - 12f), new Vector2(center.x, center.y + 12f), Color.white, 4f);
		RadarScenarioMapDraw.Line(new Vector2(center.x - 11f, center.y), new Vector2(center.x + 11f, center.y), Color.black, 2f);
		RadarScenarioMapDraw.Line(new Vector2(center.x, center.y - 11f), new Vector2(center.x, center.y + 11f), Color.black, 2f);
	}

	private void Cursor(Rect map)
	{
		if (Atlas == null || Event.current == null)
		{
			return;
		}
		if (!map.Contains(Event.current.mousePosition))
		{
			return;
		}
		Vector2 vector = Event.current.mousePosition;
		double num = (vector.x - map.x) / map.width * 360.0 - 180.0;
		double num2 = (map.y + map.height - vector.y) / map.height * 180.0 - 90.0;
		RadarAtlasCell radarAtlasCell = Atlas.CellAt(num, num2);
		string text = string.Format(CultureInfo.InvariantCulture, L("map.atlas.cursor"), F(num, "F1"), F(num2, "F1"), F(RadarAtlasPalette.Db(radarAtlasCell.SigmaHH), "F1"), F(radarAtlasCell.K2, "F2"));
		RadarScenarioMapDraw.Fill(new Rect(vector.x + 16f, vector.y + 12f, 210f, 30f), new Color(1f, 1f, 1f, 0.92f));
		GUI.Label(new Rect(vector.x + 16f + 6f, vector.y + 12f + 3f, 206f, 30f), text, small);
	}

	private void ComputeSlicesAndMedians()
	{
		List<double> list = new List<double>();
		List<double> list2 = new List<double>();
		List<double> list3 = new List<double>();
		double[] array = new double[9];
		int num = Atlas.Spec.AzimuthCells;
		for (int i = 0; i < num; i++)
		{
			double num2 = Atlas.Spec.CellAzimuthCentre(i);
			Atlas.Interpolate(num2, 0.0, array);
			double num3 = RadarAtlas.Sigma(array, default(RadarAtlasPair), 0.0);
			double num4 = Math.Abs(num2);
			if (num4 <= FrontLimit)
			{
				list.Add(num3);
			}
			else if (num4 >= SideLow && num4 <= SideHigh)
			{
				list2.Add(num3);
			}
			else if (num4 >= RearLimit)
			{
				list3.Add(num3);
			}
		}
		FrontMedianDb = RadarAtlasPalette.Db(Median(list));
		SideMedianDb = RadarAtlasPalette.Db(Median(list2));
		RearMedianDb = RadarAtlasPalette.Db(Median(list3));
	}

	private static double Median(List<double> values)
	{
		if (values.Count == 0)
		{
			return 0.0;
		}
		values.Sort();
		if (values.Count % 2 == 1)
		{
			return values[values.Count / 2];
		}
		return (values[values.Count / 2 - 1] + values[values.Count / 2]) * 0.5;
	}

	private void Slice(Rect plot)
	{
		RadarScenarioMapDraw.Fill(plot, new Color(0.96f, 0.96f, 0.96f, 1f));
		RadarScenarioMapDraw.Outline(plot, new Color(0.35f, 0.38f, 0.4f, 1f), 1f);
		GUI.Label(new Rect(plot.x, plot.y - 26f, plot.width, 24f), string.Format(CultureInfo.InvariantCulture, L("map.atlas.slice"), Bench.State.ElevationDeg.ToString("F1", CultureInfo.InvariantCulture)) + " · σ̄, dBsm", small);
		if (Atlas == null)
		{
			return;
		}
		double[] array = new double[9];
		double num = textureTop;
		double num2 = textureTop - RangeDb;
		Vector2 vector = Vector2.zero;
		int num3 = slice.Length;
		for (int i = 0; i < num3; i++)
		{
			double num4 = ((double)i + 0.5) * 360.0 / (double)num3 - 180.0;
			Atlas.Interpolate(num4, Bench.State.ElevationDeg, array);
			slice[i] = RadarAtlasPalette.Db(RadarAtlas.Sigma(array, default(RadarAtlasPair), 0.0));
			float num5 = (float)((Math.Max(num2, Math.Min(num, slice[i])) - num2) / RangeDb);
			float num6 = plot.y + plot.height - num5 * plot.height;
			float num7 = plot.x + ((float)i + 0.5f) / (float)num3 * plot.width;
			if (i > 0)
			{
				RadarScenarioMapDraw.Line(vector, new Vector2(num7, num6), new Color(0.1f, 0.22f, 0.55f, 1f), 2f);
			}
			vector = new Vector2(num7, num6);
		}
		GUI.Label(new Rect(plot.x + plot.width + 6f, plot.y - 4f, 80f, 22f), num.ToString("F0", CultureInfo.InvariantCulture), small);
		GUI.Label(new Rect(plot.x + plot.width + 6f, plot.y + plot.height - 18f, 80f, 22f), num2.ToString("F0", CultureInfo.InvariantCulture), small);
	}

	private void OnDestroy()
	{
		if (estimateCancel != null)
		{
			estimateCancel.Cancel();
		}
		if (job != null)
		{
			job.Cancel.Cancel();
		}
		phase = Phase.Idle;
		if (texture != null)
		{
			Object.Destroy(texture);
		}
		if (skin != null)
		{
			Object.Destroy(skin);
		}
	}
}
