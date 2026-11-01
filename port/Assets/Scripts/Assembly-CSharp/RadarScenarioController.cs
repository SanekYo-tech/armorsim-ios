using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public sealed class RadarScenarioController : MonoBehaviour
{
	public Action Closed;

	private GUISkin skin;

	private GUIStyle footerStyle;

	private bool importPanel;

	private string importPath;

	private readonly Dictionary<string, List<RadarScenarioAircraft>> importTargets;

	private readonly Dictionary<RadarScenarioAircraft, string> desiredModels;

	private Vector2 filesScroll;

	private string[] files;

	private int completedRoutes;

	private RadarMapSession physical;

	private RadarMapObserverAtlas observerAtlas;

	private readonly List<RadarMapScopeStation> scopeStations;

	private readonly List<RadarMapScopeLauncher> scopeLaunchers;

	public const float MaxPhysicalSpeed = 16f;

	public const double ContactFreshScans = 1.5;

	public RadarScenarioData Data { get; private set; }

	public RadarScenarioMapEditor Map { get; private set; }

	public RadarScenarioRadarScreen Radar { get; private set; }

	public RadarScenarioModelLibrary Models { get; private set; }

	public RadarScenarioLaunchGame Launches { get; private set; }

	public bool IsOperator { get; private set; }

	public bool Paused { get; set; }

	public float Elapsed { get; private set; }

	public float PlaybackSpeed { get; set; }

	public string Status { get; private set; }

	public RadarMapSession PhysicalSession => physical;

	public RadarMapPhysicalView PhysicalView { get; private set; }

	public RadarMapObserver Observer { get; private set; }

	public RadarMapModelRegistry Registry { get; set; }

	public List<string> PhysicalRefusals { get; }

	public RadarAtlasLibrary Atlases { get; set; }

	public List<RadarMapSession.MissingAtlas> MissingAtlases { get; }

	public RadarAtlasLibrary.Job BakeJob { get; private set; }

	public RadarMapObserverAtlas ObserverAtlas => observerAtlas;

	public RadarMapScope Scope { get; private set; }

	public RadarScenarioLaunchGame PhysicalLaunches { get; private set; }

	public RadarScenarioController()
	{
		importTargets = new Dictionary<string, List<RadarScenarioAircraft>>();
		desiredModels = new Dictionary<RadarScenarioAircraft, string>();
		PhysicalRefusals = new List<string>();
		MissingAtlases = new List<RadarMapSession.MissingAtlas>();
		scopeStations = new List<RadarMapScopeStation>();
		scopeLaunchers = new List<RadarMapScopeLauncher>();
	}

	private void Awake()
	{
		Data = RadarScenarioData.CreateDefault();
		Map = new RadarScenarioMapEditor();
		Models = new RadarScenarioModelLibrary();
		Radar = new RadarScenarioRadarScreen((string key) => Models.Find(key)?.ShapeProfile);
		PhysicalView = new RadarMapPhysicalView();
		Observer = new RadarMapObserver();
		Scope = new RadarMapScope();
		Launches = new RadarScenarioLaunchGame();
		Launches.ResolveTarget = ResolvePhysicalTarget;
		Scope.SetEmission = delegate(int i, bool on)
		{
			physical.Radars[i].Emitting = on;
		};
		Scope.SetPfa = delegate(int i, double pfa)
		{
			physical.Radars[i].Pfa = pfa;
		};
		Scope.SetIntegration = delegate(int i, int m)
		{
			physical.Radars[i].Scanner.SetIntegration(m, physical.Time);
		};
		Scope.SetSector = delegate(int i, double c, double w)
		{
			physical.Radars[i].Scanner.SetSector(c, w, physical.Time);
		};
		Scope.SetLowAltitude = delegate(int i, bool on)
		{
			physical.Radars[i].LowAltitude = on;
		};
		Scope.Launch = LaunchAtContact;
		Registry = RadarMapModelRegistry.Default;
		Atlases = new RadarAtlasLibrary();
		observerAtlas = new RadarMapObserverAtlas();
		Observer.ExtraSection = observerAtlas.Draw;
		Observer.ExtraHeight = 240f;
		PhysicalLaunches = new RadarScenarioLaunchGame();
		Radar.Launches = PhysicalLaunches;
		PhysicalLaunches.ResolveTarget = Radar.ResolveTrackedTarget;
		Models.Completed = Imported;
		Models.Failed = ImportFailed;
		Map.ImportRequested = OpenImportPanel;
		Map.ModelSelectionChanged = delegate(RadarScenarioAircraft plane)
		{
			desiredModels.Remove(plane);
		};
		Map.ModelTexture = (string key) => Models.GetTexture(key) ?? Models.FindByRegistryKey(key)?.Texture;
	}

	public void ShowEditor()
	{
		importPanel = false;
		completedRoutes = 0;
		Status = RadarScenarioText.Get("shell.editorHint");
		IsOperator = false;
		RefreshPhysicalChoices();
	}

	public void RefreshPhysicalChoices()
	{
		List<RadarMapModelRecord> choices = Registry.Choices();
		string[] array = new string[choices.Count];
		string[] array2 = new string[choices.Count];
		for (int i = 0; i < choices.Count; i++)
		{
			array[i] = choices[i].Key;
			array2[i] = choices[i].DisplayName;
		}
		Map.PhysicalModelKeys = array;
		Map.PhysicalModelNames = array2;
	}

	public bool StartExercise()
	{
		if (Data.Physical)
		{
			return StartPhysical();
		}
		if (desiredModels.Count > 0)
		{
			Status = RadarScenarioText.Get("shell.loading");
			return false;
		}
		if (!Data.Validate(out string reason))
		{
			Status = RadarScenarioText.Get("shell.invalid") + " " + reason;
			return false;
		}
		foreach (RadarScenarioAircraft plane in Data.Aircraft)
		{
			RadarScenarioModelLibrary.Entry entry = Models.Find(plane.ModelKey);
			if (entry == null || entry.ShapeProfile == null)
			{
				Status = RadarScenarioText.Get("shell.missingModel") + " " + plane.ModelName;
				return false;
			}
		}
		Radar.Reset();
		Launches.Reset();
		Elapsed = 0f;
		completedRoutes = 0;
		IsOperator = true;
		Status = RadarScenarioText.Get("shell.operatorHint");
		return true;
	}

	private bool StartPhysical()
	{
		PhysicalRefusals.Clear();
		MissingAtlases.Clear();
		if (BakeJob != null)
		{
			Status = string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.atlas.state.baking"), MissingAtlases.Count, Atlases.CacheFolder, RadarAtlasBaker.DefaultThreads);
			return false;
		}
		physical = RadarMapSession.Create(Data, Registry, Atlases, PhysicalRefusals, MissingAtlases);
		if (physical == null)
		{
			Status = RadarScenarioText.Get("map.phys.refuse.title") + string.Join("; ", PhysicalRefusals) + " ";
			Debug.LogWarning("[RADAR MAP] " + Status);
			return false;
		}
		IsOperator = true;
		completedRoutes = 0;
		PhysicalLaunches.Reset();
		Scope.Contact.StationId = -1;
		Scope.Contact.PlotIds.Clear();
		Scope.Contact.ManualHeightM = double.NaN;
		Scope.Message = "";
		physical.IsDestroyed = PhysicalLaunches.IsDestroyed;
		if (1f > PlaybackSpeed || PlaybackSpeed > MaxPhysicalSpeed)
		{
			PlaybackSpeed = 1f;
		}
		Status = physical.GuardLine();
		Debug.Log(string.Concat("[RADAR MAP] physical start: planes=", physical.Planes.Count.ToString(), " radars=", physical.Radars.Count.ToString(), "; guards: ", Status));
		return true;
	}

	public void BakeMissingAtlases()
	{
		if (BakeJob == null && MissingAtlases.Count > 0)
		{
			RadarMapSession.MissingAtlas missing = MissingAtlases[0];
			RadarMapModelRecord model = missing.Record;
			BakeJob = Atlases.StartBake(missing.ModelKey, missing.Scene, missing.FrequencyHz, RadarAtlasSpec.Standard, model, estimateFirst: true, missing.Coating);
		}
	}

	private void PollBake()
	{
		if (BakeJob == null || !BakeJob.Task.IsCompleted)
		{
			return;
		}
		RadarAtlasLibrary.Job job = BakeJob;
		BakeJob = null;
		try
		{
			Atlases.Remember(job.Task.GetAwaiter().GetResult());
			if (MissingAtlases.Count > 0)
			{
				MissingAtlases.RemoveAt(0);
			}
			Debug.Log(string.Concat("[RADAR ATLAS] baked ", job.ModelKey, " @ ", job.FrequencyHz.ToString(), " Hz; estimate ", job.EstimateSeconds.ToString(), " s; ", job.Stats.ToString()));
			if (MissingAtlases.Count > 0)
			{
				BakeMissingAtlases();
			}
			else
			{
				Status = RadarScenarioText.Get("shell.editorHint");
			}
		}
		catch (Exception ex)
		{
			MissingAtlases.Clear();
			Status = string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.atlas.state.failed"), ex.ToString());
		}
	}

	private void UpdatePhysical()
	{
		if (!Paused)
		{
			float speed = PlaybackSpeed;
			if (1f > speed)
			{
				speed = 1f;
			}
			else if (speed > MaxPhysicalSpeed)
			{
				speed = MaxPhysicalSpeed;
			}
			float num = Time.unscaledDeltaTime;
			if (num > 0.1f)
			{
				num = 0.1f;
			}
			double seconds = num * speed;
			physical.Advance(seconds);
			PhysicalLaunches.Tick((float)physical.Time);
			if (physical.AllFinished() && PhysicalLaunches.ActiveCount == 0)
			{
				Paused = true;
				Status = RadarScenarioText.Get("shell.finished");
			}
		}
		PhysicalView.Session = physical;
		if (Observer.ExtraSection != null)
		{
			Observer.RenderInset(physical);
		}
	}

	public void SaveDraft(string path)
	{
		try
		{
			RadarScenarioStore.Save(path, Data);
			Status = RadarScenarioText.Get("shell.saved") + " " + path;
		}
		catch (Exception ex)
		{
			Status = RadarScenarioText.Get("shell.saveFailed") + " " + ex.ToString();
		}
	}

	public void LoadDraft(string path)
	{
		Data = RadarScenarioStore.Load(path);
		ShowEditor();
		Map.ResetView();
		importTargets.Clear();
		desiredModels.Clear();
		Status = RadarScenarioText.Get("shell.loaded");
		HashSet<string> hashSet = new HashSet<string>();
		foreach (RadarScenarioAircraft plane in Data.Aircraft)
		{
			if (plane.ModelKey == "yf23" || !hashSet.Add(plane.ModelKey) || Models.Find(plane.ModelKey) != null)
			{
				continue;
			}
			if (File.Exists(plane.ModelKey))
			{
				Models.Import(plane.ModelKey);
				continue;
			}
			Status = RadarScenarioText.Get("shell.missingModel") + " " + plane.ModelName;
		}
	}

	public void ImportModel(string path)
	{
		path = Path.GetFullPath(path);
		if (Map.SelectedAircraftIndex >= 0 && Map.SelectedAircraftIndex < Data.Aircraft.Count)
		{
			if (!importTargets.TryGetValue(path, out List<RadarScenarioAircraft> targets))
			{
				targets = new List<RadarScenarioAircraft>();
				importTargets.Add(path, targets);
			}
			RadarScenarioAircraft plane = Data.Aircraft[Map.SelectedAircraftIndex];
			targets.Add(plane);
			desiredModels[plane] = path;
		}
		completedRoutes = 0;
		Status = RadarScenarioText.Get("shell.loading");
		try
		{
			Models.Import(path);
		}
		catch (Exception ex)
		{
			ImportFailed(path, ex.ToString());
		}
	}

	private void ImportFailed(string key, string message)
	{
		if (key != null && importTargets.TryGetValue(key, out List<RadarScenarioAircraft> targets))
		{
			foreach (RadarScenarioAircraft plane in targets)
			{
				if (desiredModels.TryGetValue(plane, out string value) && value == key)
				{
					desiredModels.Remove(plane);
				}
			}
			importTargets.Remove(key);
		}
		Status = RadarScenarioText.Get("shell.importFailed") + " " + message;
	}

	private void Imported(RadarScenarioModelLibrary.Entry entry)
	{
		string[] array = new string[Models.Entries.Count];
		string[] array2 = new string[Models.Entries.Count];
		for (int i = 0; i < Models.Entries.Count; i++)
		{
			array[i] = Models.Entries[i].Key;
			array2[i] = Models.Entries[i].Name;
		}
		Map.AvailableModelKeys = array;
		Map.AvailableModelNames = array2;
		if (entry != null && importTargets.TryGetValue(entry.Key, out List<RadarScenarioAircraft> targets))
		{
			foreach (RadarScenarioAircraft plane in targets)
			{
				if (!Data.Aircraft.Contains(plane))
				{
					continue;
				}
				if (desiredModels.TryGetValue(plane, out string value) && value == entry.Key)
				{
					plane.ModelKey = entry.Key;
					plane.ModelName = entry.Name;
					desiredModels.Remove(plane);
					if (Map.SelectedAircraftIndex >= 0 && Map.SelectedAircraftIndex < Data.Aircraft.Count && Data.Aircraft[Map.SelectedAircraftIndex] == plane)
					{
						Map.SetSelectedAircraftModel(Data, entry.Key, entry.Name);
					}
				}
			}
			importTargets.Remove(entry.Key);
		}
		Status = RadarScenarioText.Get("shell.imported") + " " + entry.Name + " (" + entry.Triangles.ToString() + ")";
	}

	private void Update()
	{
		Models.Poll();
		PollBake();
		if (!IsOperator)
		{
			return;
		}
		if (physical != null)
		{
			UpdatePhysical();
			return;
		}
		if (Paused)
		{
			Radar.Tick(Data, Elapsed, 0f);
			return;
		}
		float num = Time.unscaledDeltaTime;
		if (num > 0.1f)
		{
			num = 0.1f;
		}
		float speed = PlaybackSpeed;
		if (speed < 0.25f || speed > 4f)
		{
			speed = 4f;
		}
		Elapsed += speed * num;
		Radar.Tick(Data, Elapsed, speed * num);
		Launches.Tick(Elapsed);
		Radar.RemoveDestroyedEchoes();
		completedRoutes = 0;
		foreach (RadarScenarioAircraft plane in Data.Aircraft)
		{
			if (Launches.IsDestroyed(plane.Id) || RadarScenarioGameModel.Pose(plane, Elapsed).Finished)
			{
				completedRoutes++;
			}
		}
		if (completedRoutes == Data.Aircraft.Count && Launches.ActiveCount == 0)
		{
			Paused = true;
			Status = RadarScenarioText.Get("shell.finished");
		}
	}

	private void OpenImportPanel()
	{
		importPanel = true;
		Directory.CreateDirectory(RadarBenchController.ModelsFolder);
		List<string> list = new List<string>();
		string[] array = Directory.GetFiles(RadarBenchController.ModelsFolder);
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (Path.GetExtension(text).Equals(".stl") || Path.GetExtension(text).Equals(".obj"))
			{
				list.Add(text);
			}
		}
		files = list.ToArray();
	}

	private void EnsureSkin()
	{
		if (skin != null)
		{
			return;
		}
		skin = UnityEngine.Object.Instantiate(GUI.skin);
		skin.font = Resources.Load<Font>("RadarScenarioSans");
		if (skin.font == null)
		{
			throw new InvalidOperationException("Missing RadarScenarioSans font");
		}
		GUIStyle[] array = new GUIStyle[6] { skin.label, skin.button, skin.box, skin.textField, skin.textArea, skin.toggle };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].fontSize = 20;
			array[i].richText = false;
			array[i].wordWrap = true;
		}
		skin.label.normal.textColor = new Color(0.91f, 0.91f, 0.91f, 1f);
		skin.button.alignment = (TextAnchor)4;
		skin.textField.padding = new RectOffset(8, 8, 5, 5);
		skin.horizontalSlider.fixedHeight = 22f;
		skin.horizontalSliderThumb.fixedHeight = 22f;
		skin.horizontalSliderThumb.fixedWidth = 16f;
		footerStyle = new GUIStyle(skin.label)
		{
			alignment = (TextAnchor)0,
			contentOffset = new Vector2(2f, 2f),
			clipping = (TextClipping)1,
			padding = new RectOffset(0, 0, 0, 0)
		};
	}

	private void OnGUI()
	{
		if (Data == null)
		{
			return;
		}
		Matrix4x4 matrix = GUI.matrix;
		GUISkin gUISkin = GUI.skin;
		Color color = GUI.color;
		GUI.depth = -50;
		GUI.color = new Color(0.065f, 0.079f, 0.087f, 1f);
		GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
		GUI.color = Color.white;
		float x = Mathf.Min((float)Screen.width / 1920f, (float)Screen.height / 1080f);
		float y = x;
		GUI.matrix = matrix * Matrix4x4.TRS(new Vector3((Screen.width - 1920f * x) * 0.5f, (Screen.height - 1080f * y) * 0.5f, 0f), Quaternion.identity, new Vector3(x, y, 1f));
		EnsureSkin();
		GUI.skin = skin;
		if (skin != null)
		{
			GUIStyle gUIStyle = new GUIStyle(skin.label);
			gUIStyle.fontSize = 30;
			gUIStyle.fontStyle = FontStyle.Bold;
			GUI.Label(new Rect(24f, 14f, 1200f, 42f), RadarScenarioText.Get(IsOperator ? "shell.operatorTitle" : "shell.editorTitle"), gUIStyle);
			GUIStyle gUIStyle2 = new GUIStyle(skin.label);
			gUIStyle2.fontSize = 19;
			gUIStyle2.fontStyle = FontStyle.Bold;
			gUIStyle2.alignment = TextAnchor.UpperRight;
			GUI.Label(new Rect(700f, 22f, 880f, 34f), RadarScenarioText.Get((Data.DetectionModel == RadarDetectionModel.Physical) ? "map.model.physical" : "map.model.arcade"), gUIStyle2);
			if (Data.DetectionModel != RadarDetectionModel.Physical)
			{
				GUI.Label(new Rect(24f, 60f, 1865f, 36f), RadarScenarioText.Get("shell.fictional"));
			}
			GUI.enabled = !importPanel;
			if (GUI.Button(new Rect(1600f, 14f, 295f, 40f), RadarScenarioText.Get("shell.backBench")))
			{
				Closed?.Invoke();
			}
			else
			{
				if (IsOperator)
				{
					if (physical == null)
					{
						OperatorToolbar();
					}
					else
					{
						PhysicalToolbar();
					}
				}
				else
				{
					EditorToolbar();
				}
				GUI.enabled = !importPanel;
				if (IsOperator)
				{
					if (physical == null)
					{
						Radar.LaunchEnabled = !Paused;
						Radar.Draw(new Rect(24f, 158f, 1872f, 826f), Data, Elapsed);
					}
					else
					{
						Scope.Draw(new Rect(24f, 158f, 1872f, 826f), ScopeStations(), ScopeLaunchers(), physical.Time);
						Rect rect = PhysicalView.Draw(new Rect(24f, 158f, 1872f, 826f), physical.Time);
						Observer.Open = PhysicalView.ObserverOpen;
						Observer.Draw(rect, physical);
					}
				}
				else
				{
					Map.Draw(new Rect(24f, 158f, 1872f, 826f), Data);
				}
			}
			GUI.matrix = matrix;
			GUI.skin = gUISkin;
			GUI.color = color;
			GUI.enabled = true;
			GUI.BeginGroup(new Rect(24f, 1000f, 1872f, 68f));
			string text = (Models.Busy ? RadarScenarioText.Get("shell.loading") : Status);
			GUI.Label(new Rect(0f, 0f, 1872f, 68f), text, footerStyle);
			GUI.EndGroup();
			if (importPanel)
			{
				DrawImportPanel();
			}
		}
	}

	private void EditorToolbar()
	{
		if (GUI.Button(new Rect(24f, 107f, 230f, 40f), RadarScenarioText.Get("shell.ready")))
		{
			StartExercise();
		}
		if (GUI.Button(new Rect(270f, 107f, 230f, 40f), RadarScenarioText.Get("shell.save")))
		{
			SaveDraft(RadarScenarioStore.DefaultPath);
		}
		if (GUI.Button(new Rect(516f, 107f, 230f, 40f), RadarScenarioText.Get("shell.load")))
		{
			LoadDraft(RadarScenarioStore.DefaultPath);
		}
		if (BakeJob == null)
		{
			if (MissingAtlases.Count > 0 && Data != null && Data.DetectionModel == RadarDetectionModel.Physical && GUI.Button(new Rect(775f, 107f, 420f, 40f), RadarScenarioText.Get("map.phys.bake.run")))
			{
				BakeMissingAtlases();
				return;
			}
			GUI.Label(new Rect(775f, 109f, 1100f, 38f), RadarScenarioText.Get("shell.workflow"));
			return;
		}
		if (GUI.Button(new Rect(775f, 107f, 200f, 40f), RadarScenarioText.Get("map.atlas.cancel")))
		{
			BakeJob.Cancel.Cancel();
			MissingAtlases.Clear();
		}
		GUI.Label(new Rect(990f, 109f, 900f, 38f), string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.phys.bake.progress"), 1, MissingAtlases.Count, BakeJob.Done.Value, BakeJob.Total));
	}

	private void OperatorToolbar()
	{
		if (GUI.Button(new Rect(24f, 107f, 170f, 40f), RadarScenarioText.Get(Paused ? "shell.resume" : "shell.pause")))
		{
			Paused = !Paused;
		}
		if (GUI.Button(new Rect(210f, 107f, 190f, 40f), RadarScenarioText.Get("shell.restart")))
		{
			StartExercise();
		}
		if (GUI.Button(new Rect(416f, 107f, 225f, 40f), RadarScenarioText.Get("shell.edit")))
		{
			ShowEditor();
		}
		GUI.Label(new Rect(670f, 109f, 115f, 36f), RadarScenarioText.Get("shell.time"));
		float num = 1f;
		for (float num2 = 790f; num2 < 1024f; num2 += 78f)
		{
			if (GUI.Button(new Rect(num2, 107f, 70f, 40f), "x" + num + ((PlaybackSpeed == num) ? " *" : "")))
			{
				PlaybackSpeed = num;
			}
			num *= 2f;
		}
		string text = string.Concat(Elapsed.ToString("F1"), " s   |   ", RadarScenarioText.Get("shell.completed"), " ", completedRoutes.ToString(), " / ", Data.Aircraft.Count.ToString(), "   |   ", RadarScenarioText.Get("track.hits"), " ", Launches.DestroyedCount.ToString());
		GUI.Label(new Rect(1055f, 109f, 840f, 40f), text);
	}

	private List<RadarMapScopeStation> ScopeStations()
	{
		while (scopeStations.Count < physical.Radars.Count)
		{
			scopeStations.Add(new RadarMapScopeStation());
		}
		while (scopeStations.Count > physical.Radars.Count)
		{
			scopeStations.RemoveAt(scopeStations.Count - 1);
		}
		for (int i = 0; i < physical.Radars.Count; i++)
		{
			RadarMapSession.Radar radar = physical.Radars[i];
			RadarMapScopeStation station = scopeStations[i];
			station.StationId = radar.Source.Id;
			station.Spec = radar.Spec;
			station.Scanner = radar.Scanner;
			station.Emitting = radar.Emitting;
			station.Pfa = radar.Pfa;
			station.LowAltitude = radar.LowAltitude;
			station.AntennaHeightM = radar.Antenna.z;
			station.MapPosition = radar.Source.Position;
			station.Plots = radar.Plots;
			station.Name = RadarScenarioText.Get("map.radar") + " " + radar.Source.Id.ToString() + " · " + RadarMapEditorPanel.StationName(radar.Spec);
		}
		return scopeStations;
	}

	private List<RadarMapScopeLauncher> ScopeLaunchers()
	{
		scopeLaunchers.Clear();
		foreach (RadarScenarioStation station in Data.Stations)
		{
			if (station.IsDefencePost)
			{
				scopeLaunchers.Add(new RadarMapScopeLauncher
				{
					Id = station.Id,
					MapPosition = station.Position,
					Ammo = PhysicalLaunches.AmmoRemaining(station.Id),
					Cooldown = PhysicalLaunches.CooldownRemaining(station.Id, (float)physical.Time)
				});
			}
		}
		return scopeLaunchers;
	}

	private int RadarIndexOf(int stationId)
	{
		for (int i = 0; i < physical.Radars.Count; i++)
		{
			if (physical.Radars[i].Source.Id == stationId)
			{
				return i;
			}
		}
		return -1;
	}

	private bool ContactFresh(RadarMapScopeContact contact, out int radar)
	{
		radar = -1;
		if (contact == null || contact.PlotIds.Count <= 0)
		{
			return false;
		}
		int stationId = contact.StationId;
		for (int i = 0; i < physical.Radars.Count; i++)
		{
			if (physical.Radars[i].Source.Id == stationId)
			{
				radar = i;
				break;
			}
		}
		if (radar < 0)
		{
			return false;
		}
		double age = physical.Time - contact.Last.Time;
		return (double)physical.Radars[radar].Scanner.Revisit * ContactFreshScans >= age;
	}

	private RadarScenarioLaunchGame.TrackedTarget ResolvePhysicalTarget(int id)
	{
		RadarMapScopeContact contact = Scope.Contact;
		if (contact == null || physical == null)
		{
			return default(RadarScenarioLaunchGame.TrackedTarget);
		}
		if (!ContactFresh(contact, out int radar) || physical.JudgeAircraft(radar, contact.Last.PlotId) != id)
		{
			return default(RadarScenarioLaunchGame.TrackedTarget);
		}
		RadarScenarioLaunchGame.TrackedTarget result = default(RadarScenarioLaunchGame.TrackedTarget);
		result.Available = true;
		result.ObservedPosition = contact.LastMapPosition;
		return result;
	}

	private bool LaunchAtContact(RadarMapScopeContact contact, int launcherId)
	{
		if (physical == null || Paused)
		{
			return false;
		}
		if (!ContactFresh(contact, out int radar))
		{
			return false;
		}
		int num = physical.JudgeAircraft(radar, contact.Last.PlotId);
		int contactNumber = 0;
		if (num > 0)
		{
			contactNumber = contact.PlotIds[0];
		}
		bool launched = PhysicalLaunches.TryLaunchTracked(Data, launcherId, contact.LastMapPosition, contactNumber, num, (float)physical.Time, out RadarScenarioLaunchGame.LaunchOutcome outcome);
		Scope.LastLaunchOutcomeCode = (int)outcome;
		return launched;
	}

	private void PhysicalToolbar()
	{
		if (GUI.Button(new Rect(24f, 107f, 170f, 40f), RadarScenarioText.Get(Paused ? "shell.resume" : "shell.pause")))
		{
			Paused = !Paused;
		}
		if (GUI.Button(new Rect(210f, 107f, 190f, 40f), RadarScenarioText.Get("shell.restart")))
		{
			physical.Time = 0.0;
			physical.ResetDetection();
			PhysicalLaunches.Reset();
			Scope.Contact.StationId = -1;
			Scope.Contact.PlotIds.Clear();
			Scope.Contact.ManualHeightM = double.NaN;
			Paused = false;
		}
		if (GUI.Button(new Rect(416f, 107f, 225f, 40f), RadarScenarioText.Get("shell.edit")))
		{
			ShowEditor();
		}
		GUI.Label(new Rect(670f, 109f, 115f, 36f), RadarScenarioText.Get("shell.time"));
		float num = 1f;
		for (float num2 = 790f; num2 < 1180f; num2 += 78f)
		{
			if (GUI.Button(new Rect(num2, 107f, 70f, 40f), "x" + num + ((PlaybackSpeed == num) ? " *" : "")))
			{
				PlaybackSpeed = num;
			}
			num *= 2f;
		}
		string text = string.Concat(physical.Time.ToString("F1", CultureInfo.InvariantCulture), " s   |   ", RadarScenarioText.Get("track.hits"), " ", PhysicalLaunches.DestroyedCount.ToString(), "   |   ", RadarScenarioText.Get("track.misses"), " ", PhysicalLaunches.MissedCount.ToString());
		GUI.Label(new Rect(1210f, 109f, 685f, 40f), text);
	}

	private void DrawImportPanel()
	{
		GUI.color = new Color(0f, 0f, 0f, 0.78f);
		GUI.DrawTexture(new Rect(0f, 0f, 1920f, 1080f), Texture2D.whiteTexture);
		GUI.color = Color.white;
		GUI.Box(new Rect(405f, 235f, 1110f, 610f), "");
		GUI.Label(new Rect(430f, 257f, 1060f, 42f), RadarScenarioText.Get("shell.importTitle"));
		GUI.Label(new Rect(430f, 305f, 1060f, 64f), RadarScenarioText.Get("shell.importHint"));
		importPath = GUI.TextField(new Rect(430f, 375f, 820f, 42f), importPath);
		if (GUI.Button(new Rect(1264f, 375f, 225f, 42f), RadarScenarioText.Get("shell.import")))
		{
			ImportModel(importPath);
		}
		GUI.Label(new Rect(430f, 432f, 1060f, 64f), RadarBenchController.ModelsFolder);
		float num = Mathf.Max(files.Length * 45, 220);
		filesScroll = GUI.BeginScrollView(new Rect(430f, 500f, 1060f, 225f), filesScroll, new Rect(0f, 0f, 1000f, num));
		for (int i = 0; i < files.Length; i++)
		{
			string path = files[i];
			if (GUI.Button(new Rect(0f, i * 45, 1000f, 40f), Path.GetFileName(path)))
			{
				ImportModel(path);
			}
		}
		GUI.EndScrollView();
		if (GUI.Button(new Rect(1239f, 774f, 250f, 40f), RadarScenarioText.Get("shell.close")))
		{
			importPanel = false;
		}
	}

	private void OnDestroy()
	{
		Models.Dispose();
		Map.Dispose();
		Radar.Dispose();
		Observer.Dispose();
		observerAtlas.Dispose();
		if (BakeJob != null)
		{
			BakeJob.Cancel.Cancel();
			BakeJob.Cancel.Dispose();
		}
	}
}
