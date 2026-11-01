using System;
using UnityEngine;

public sealed class RadarScenarioMapEditor : IDisposable
{
	private enum Tool
	{
		Select = 0,
		Route = 1,
		Aircraft = 2,
		Radar = 3,
		Post = 4
	}

	private enum Drag
	{
		None = 0,
		DrawRoute = 1,
		Waypoint = 2,
		Station = 3,
		Pan = 4
	}

	public Action ImportRequested;

	public Action<RadarScenarioAircraft> ModelSelectionChanged;

	public Func<string, Texture> ModelTexture;

	public string SelectedModelKey = "yf23";

	public string SelectedModelName = "YF-23";

	public string[] AvailableModelKeys = new string[1] { "yf23" };

	public string[] AvailableModelNames = new string[1] { "YF-23" };

	public string[] PhysicalModelKeys = new string[1] { "yf23" };

	public string[] PhysicalModelNames = new string[1] { "YF-23" };

	private readonly RadarMapEditorPanel physicalPanel = new RadarMapEditorPanel();

	private Tool tool;

	private Drag drag;

	private Vector2 scroll;

	private Vector2 centre = RadarScenarioData.MapCentre;

	private float zoom = 1f;

	private int terrainSeed = int.MinValue;

	private Texture2D terrain;

	private GUIStyle label;

	private GUIStyle small;

	private GUIStyle heading;

	private GUIStyle button;

	private GUIStyle routeButton;

	private GUIStyle field;

	private GUIStyle toggle;

	private static readonly Color Ink = new Color(0.11f, 0.15f, 0.17f);

	private static readonly Color[] AircraftColors = new Color[6]
	{
		new Color(0.06f, 0.3f, 0.7f),
		new Color(0.7f, 0.22f, 0.08f),
		new Color(0.34f, 0.14f, 0.63f),
		new Color(0f, 0.44f, 0.4f),
		new Color(0.67f, 0.14f, 0.37f),
		new Color(0.4f, 0.36f, 0.02f)
	};

	private bool terrainCoast;

	public int SelectedAircraftIndex { get; set; }

	public int SelectedWaypointIndex { get; set; }

	public int SelectedStationIndex { get; set; }

	public string StatusKey { get; private set; }

	public float DefaultAltitude { get; set; } = 1200f;

	public Rect LastMapRect { get; private set; }

	private static bool Physical(RadarScenarioData data)
	{
		return data != null && data.DetectionModel == RadarDetectionModel.Physical;
	}

	private static string L(string key)
	{
		return RadarScenarioText.Get(key);
	}

	public void Draw(Rect area, RadarScenarioData data)
	{
		if (data == null)
		{
			return;
		}
		if (area.width < 500f || area.height < 250f)
		{
			return;
		}
		EnsureStyles();
		ClampSelection(data);
		EnsureTerrain(data.Seed, data.DetectionModel == RadarDetectionModel.Physical && data.CoastMode);
		GUI.BeginGroup(area);
		RadarScenarioMapDraw.Fill(new Rect(0f, 0f, area.width, area.height), new Color(0.82f, 0.84f, 0.84f));
		float sideW = Mathf.Clamp(area.width * 0.27f, 320f, 390f);
		float sideX = area.width - sideW - 10f;
		float sideH = area.height - 20f;
		float mapW = sideX - 22f;
		float mapH = area.height - 112f;
		float z = Mathf.Max(0f, Mathf.Min(mapH / 450f, mapW / 900f));
		float zw = z * 900f;
		float zh = z * 450f;
		float cx = mapW * 0.5f + 12f - zw * 0.5f;
		float cy = mapH * 0.5f + 42f - zh * 0.5f;
		LastMapRect = new Rect(cx + area.x, cy + area.y, zw, zh);
		GUI.Label(new Rect(22f, 6f, mapW, 32f), L("map.title") + "   " + 900f.ToString() + " x " + 450f.ToString() + " " + L(Physical(data) ? "map.phys.units" : "map.units"), heading);
		DrawSidebar(new Rect(sideX, 10f, sideW, sideH), data);
		GUI.EndGroup();
		DrawMap(LastMapRect, data);
		GUI.BeginGroup(area);
		DrawLegend(new Rect(cx, cy + zh + 5f, zw, 25f));
		GUI.Label(new Rect(22f, cy + zh + 34f, mapW, 40f), L(HintKey()), small);
		if (GUI.enabled)
		{
			HandleMapInput(new Rect(cx, cy, zw, zh), data);
		}
		GUI.EndGroup();
	}

	private void EnsureStyles()
	{
		if (label != null)
		{
			return;
		}
		label = new GUIStyle(GUI.skin.label);
		label.fontSize = 19;
		label.wordWrap = true;
		label.normal.textColor = Ink;
		small = new GUIStyle(label);
		small.fontSize = 16;
		heading = new GUIStyle(label);
		heading.fontSize = 21;
		heading.fontStyle = FontStyle.Bold;
		button = new GUIStyle(GUI.skin.button);
		button.fontSize = 18;
		button.wordWrap = true;
		button.fixedHeight = 35f;
		routeButton = new GUIStyle(button);
		routeButton.fixedHeight = 54f;
		field = new GUIStyle(GUI.skin.textField);
		field.fontSize = 19;
		field.fixedHeight = 31f;
		toggle = new GUIStyle(GUI.skin.toggle);
		toggle.fontSize = 19;
		toggle.wordWrap = true;
		toggle.normal.textColor = Ink;
		toggle.onNormal.textColor = Ink;
		toggle.onHover.textColor = Ink;
		toggle.onActive.textColor = Ink;
		toggle.hover.textColor = Ink;
		toggle.active.textColor = Ink;
	}

	private void DrawSidebar(Rect side, RadarScenarioData data)
	{
		bool enabled = GUI.enabled;
		RadarScenarioMapDraw.Fill(side, new Color(0.91f, 0.92f, 0.92f, 1f));
		GUILayout.BeginArea(new Rect(side.x + 10f, side.y + 8f, side.width - 20f, side.height - 16f));
		scroll = GUILayout.BeginScrollView(scroll, alwaysShowHorizontal: false, alwaysShowVertical: true);
		RadarMapEditorPanel.ScenarioSwitch(data, heading, small, button);
		if (data.DetectionModel == RadarDetectionModel.Physical)
		{
			RadarMapEditorPanel.WorldFields(data, label, small, button, toggle);
		}
		GUILayout.Label(L("map.aircraft") + "  " + data.Aircraft.Count + "/" + 12, heading);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button(L("map.add"), button))
		{
			tool = Tool.Aircraft;
			StatusKey = null;
		}
		GUI.enabled = enabled && data.Aircraft.Count > 0;
		if (GUILayout.Button(L("map.remove"), button))
		{
			data.Aircraft.RemoveAt(SelectedAircraftIndex);
			SelectedAircraftIndex = -1;
			ClampSelection(data);
		}
		GUI.enabled = enabled;
		GUILayout.EndHorizontal();
		RadarScenarioAircraft aircraft = SelectedAircraft(data);
		if (aircraft == null)
		{
			GUILayout.Label(L("map.no_plane"), small);
			DrawModelSelector(data, null);
		}
		else
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("<", button, GUILayout.Width(39f)))
			{
				SelectAircraft(data, (SelectedAircraftIndex - 1 + data.Aircraft.Count) % data.Aircraft.Count);
			}
			GUILayout.Label(SelectedAircraftIndex + 1 + "/" + data.Aircraft.Count + "  " + aircraft.Name, label, GUILayout.MinHeight(35f));
			if (GUILayout.Button(">", button, GUILayout.Width(39f)))
			{
				SelectAircraft(data, (SelectedAircraftIndex + 1) % data.Aircraft.Count);
			}
			GUILayout.EndHorizontal();
			aircraft = SelectedAircraft(data);
			GUILayout.Label(L("map.name"), small);
			aircraft.Name = GUILayout.TextField(aircraft.Name ?? "", 48, field);
			DrawModelSelector(data, aircraft);
			GUILayout.Space(7f);
			if (data.DetectionModel == RadarDetectionModel.Physical)
			{
				physicalPanel.AircraftFields(aircraft, label, small);
			}
			else
			{
				GUILayout.Label(L("map.coating"), small);
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("<", button, GUILayout.Width(39f)))
				{
					aircraft.Coating = (aircraft.Coating + 3) % 4;
				}
				GUILayout.Label(L("map.coat" + aircraft.Coating), label, GUILayout.MinHeight(35f));
				if (GUILayout.Button(">", button, GUILayout.Width(39f)))
				{
					aircraft.Coating = (aircraft.Coating + 1) % 4;
				}
				GUILayout.EndHorizontal();
				int coating = Mathf.Clamp(aircraft.Coating, 0, RadarScenarioGameModel.CoatingVisibility.Length - 1);
				GUILayout.Label(L("map.visibility") + ": " + RadarScenarioGameModel.CoatingVisibility[coating].ToString("F2"), small);
				GUILayout.Label(L("map.speed") + ": " + aircraft.Speed.ToString("F2"), small);
				aircraft.Speed = GUILayout.HorizontalSlider(aircraft.Speed, 0.25f, 20f);
			}
			aircraft.Loop = GUILayout.Toggle(aircraft.Loop, L("map.loop"), toggle);
			GUILayout.Space(9f);
			GUILayout.Label(L("map.route") + "  " + aircraft.Route.Count + "/" + 256, heading);
			GUILayout.BeginHorizontal();
			ToolButton("map.select", Tool.Select);
			ToolButton("map.draw", Tool.Route);
			GUILayout.EndHorizontal();
			GUILayout.Label(L("map.default_altitude") + ": " + DefaultAltitude.ToString("F0"), small);
			DefaultAltitude = Mathf.Round(GUILayout.HorizontalSlider(DefaultAltitude, 0f, 12000f) / 25f) * 25f;
			if (SelectedWaypointIndex >= 0)
			{
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("<", button, GUILayout.Width(39f)))
				{
					SelectedWaypointIndex = (SelectedWaypointIndex - 1 + aircraft.Route.Count) % aircraft.Route.Count;
				}
				GUILayout.Label(L("map.waypoint") + " " + (SelectedWaypointIndex + 1) + "/" + aircraft.Route.Count, label, GUILayout.MinHeight(35f));
				if (GUILayout.Button(">", button, GUILayout.Width(39f)))
				{
					SelectedWaypointIndex = (SelectedWaypointIndex + 1) % aircraft.Route.Count;
				}
				GUILayout.EndHorizontal();
				RadarScenarioWaypoint waypoint = aircraft.Route[SelectedWaypointIndex];
				GUILayout.Label(L("map.altitude") + ": " + waypoint.Altitude.ToString("F0"), small);
				waypoint.Altitude = Mathf.Round(GUILayout.HorizontalSlider(waypoint.Altitude, 0f, 12000f) / 25f) * 25f;
				if (GUILayout.Button(L("map.delete_point"), button))
				{
					aircraft.Route.RemoveAt(SelectedWaypointIndex);
					SelectedWaypointIndex = Mathf.Min(SelectedWaypointIndex, aircraft.Route.Count - 1);
				}
			}
			else
			{
				GUILayout.Label(L("map.no_point"), small);
			}
			if (GUILayout.Button(L("map.clear"), button))
			{
				RadarScenarioAircraft aircraft2 = SelectedAircraft(data);
				if (aircraft2 != null)
				{
					if (aircraft2.Route.Count > 1)
					{
						aircraft2.Route.RemoveRange(1, aircraft2.Route.Count - 1);
					}
					SelectedWaypointIndex = ((aircraft2.Route.Count > 0) ? 0 : (-1));
					tool = Tool.Route;
				}
			}
			if (aircraft.Route.Count < 2)
			{
				GUILayout.Label(L("map.no_route"), small);
			}
		}
		GUILayout.Space(14f);
		GUILayout.Label(L("map.stations"), heading);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button(L("map.add_radar"), button))
		{
			tool = Tool.Radar;
			StatusKey = null;
		}
		if (GUILayout.Button(L("map.add_post"), button))
		{
			tool = Tool.Post;
			StatusKey = null;
		}
		GUILayout.EndHorizontal();
		int radars = 0;
		int posts = 0;
		for (int i = 0; i < data.Stations.Count; i++)
		{
			if (data.Stations[i].IsDefencePost)
			{
				posts++;
			}
			else
			{
				radars++;
			}
		}
		GUILayout.Label(L("map.radar") + " " + radars + "/2  |  " + L("map.post") + " " + posts + "/4", small);
		if (data.Stations.Count > 0)
		{
			if (SelectedStationIndex < 0)
			{
				SelectedStationIndex = 0;
			}
			RadarScenarioStation station = data.Stations[SelectedStationIndex];
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("<", button, GUILayout.Width(39f)))
			{
				SelectedStationIndex = (SelectedStationIndex - 1 + data.Stations.Count) % data.Stations.Count;
			}
			GUILayout.Label(StationName(station), label, GUILayout.MinHeight(35f));
			if (GUILayout.Button(">", button, GUILayout.Width(39f)))
			{
				SelectedStationIndex = (SelectedStationIndex + 1) % data.Stations.Count;
			}
			GUILayout.EndHorizontal();
			station = data.Stations[SelectedStationIndex];
			if (station.IsDefencePost)
			{
				GUILayout.Label(L("map.launcher_hint"), small);
			}
			else if (data.DetectionModel == RadarDetectionModel.Physical)
			{
				RadarMapEditorPanel.StationFields(station, RadarMapWorld.Of(data), label, small, button, toggle);
			}
			else
			{
				GUILayout.Label(L("map.preset"), small);
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("<", button, GUILayout.Width(39f)))
				{
					station.PresetIndex = RadarScenarioGroundCatalog.Next(station.PresetIndex, -1);
				}
				GUILayout.Label(Localization.T(RadarCatalog.Presets[Mathf.Clamp(station.PresetIndex, 0, RadarCatalog.Presets.Count - 1)].NameKey), small, GUILayout.MinHeight(43f));
				if (GUILayout.Button(">", button, GUILayout.Width(39f)))
				{
					station.PresetIndex = RadarScenarioGroundCatalog.Next(station.PresetIndex, 1);
				}
				GUILayout.EndHorizontal();
				GUILayout.Label(L("map.coverage") + ": " + RadarScenarioGroundCatalog.CoverageRadius(station.PresetIndex).ToString("0") + " " + L("map.units"), small);
				GUILayout.Label(L("map.station_hint"), small);
			}
			if (GUILayout.Button(L("map.remove") + " — " + StationName(station), button))
			{
				data.Stations.RemoveAt(SelectedStationIndex);
				SelectedStationIndex = Mathf.Min(SelectedStationIndex, data.Stations.Count - 1);
			}
		}
		else
		{
			GUILayout.Label(L("map.no_station"), small);
		}
		GUILayout.Space(12f);
		if (GUILayout.Button(L("map.fit"), button))
		{
			centre = RadarScenarioData.MapCentre;
			zoom = 1f;
		}
		if (!string.IsNullOrEmpty(StatusKey))
		{
			GUILayout.Label(L(StatusKey), small);
		}
		GUILayout.Space(10f);
		GUILayout.EndScrollView();
		GUILayout.EndArea();
	}

	private void DrawModelSelector(RadarScenarioData data, RadarScenarioAircraft aircraft)
	{
		GUILayout.Space(7f);
		GUILayout.Label(L("map.model"), label);
		string key = ((aircraft != null) ? aircraft.ModelKey : SelectedModelKey);
		string name = ((aircraft != null) ? aircraft.ModelName : SelectedModelName);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("<", button, GUILayout.Width(39f)))
		{
			CycleModel(data, key, -1);
		}
		GUILayout.Label(name ?? "YF-23", label, GUILayout.MinHeight(35f));
		if (GUILayout.Button(">", button, GUILayout.Width(39f)))
		{
			CycleModel(data, key, 1);
		}
		GUILayout.EndHorizontal();
		if (data.DetectionModel == RadarDetectionModel.Physical)
		{
			GUILayout.Label(L("map.phys.modelHint"), small);
			return;
		}
		Texture texture = ModelTexture?.Invoke(key);
		if (texture != null)
		{
			Rect rect = GUILayoutUtility.GetRect(10f, 90f, GUILayout.ExpandWidth(true));
			RadarScenarioMapDraw.Fill(rect, new Color(0.79f, 0.82f, 0.83f, 1f));
			GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, alphaBlend: true);
		}
		GUILayout.Label(L("map.model_hint"), small);
		if (GUILayout.Button(L("map.import"), button))
		{
			ImportRequested?.Invoke();
		}
	}

	private void CycleModel(RadarScenarioData data, string current, int direction)
	{
		string[] keys = Physical(data) ? PhysicalModelKeys : AvailableModelKeys;
		string[] names = Physical(data) ? PhysicalModelNames : AvailableModelNames;
		int n = Math.Min(keys?.Length ?? 0, names?.Length ?? 0);
		if (n == 0)
		{
			return;
		}
		int i = Array.IndexOf(keys, current);
		if (i < 0)
		{
			i = 0;
		}
		int next = (i + direction + n) % n;
		string key = keys[next];
		string name = names[next];
		SelectedModelKey = key;
		SelectedModelName = name;
		if (data.Aircraft.Count > 0 && SelectedAircraftIndex >= 0 && SelectedAircraftIndex < data.Aircraft.Count)
		{
			RadarScenarioAircraft aircraft = data.Aircraft[SelectedAircraftIndex];
			if (aircraft != null)
			{
				aircraft.ModelKey = key;
				aircraft.ModelName = name;
				ModelSelectionChanged?.Invoke(aircraft);
			}
		}
	}

	private void ToolButton(string key, Tool value)
	{
		Color backgroundColor = GUI.backgroundColor;
		if (tool == value)
		{
			GUI.backgroundColor = new Color(0.44f, 0.7f, 0.93f, 1f);
		}
		if (GUILayout.Button(L(key), routeButton, GUILayout.Width(167f)))
		{
			tool = value;
			StatusKey = null;
		}
		GUI.backgroundColor = backgroundColor;
	}

	public void SelectAircraft(RadarScenarioData data, int index)
	{
		int i = Mathf.Clamp(index, 0, data.Aircraft.Count - 1);
		SelectedAircraftIndex = i;
		int wp = -1;
		if (data.Aircraft.Count > 0 && data.Aircraft[i].Route != null && data.Aircraft[i].Route.Count > 0)
		{
			wp = 0;
		}
		SelectedWaypointIndex = wp;
		if (data.Aircraft.Count > 0 && SelectedAircraftIndex >= 0 && SelectedAircraftIndex < data.Aircraft.Count)
		{
			RadarScenarioAircraft aircraft = data.Aircraft[SelectedAircraftIndex];
			SelectedModelKey = aircraft.ModelKey;
			SelectedModelName = aircraft.ModelName;
		}
	}

	public void SetSelectedAircraftModel(RadarScenarioData data, string key, string name)
	{
		SelectedModelKey = key;
		SelectedModelName = name;
		if (data.Aircraft.Count > 0 && SelectedAircraftIndex >= 0 && SelectedAircraftIndex < data.Aircraft.Count)
		{
			RadarScenarioAircraft aircraft = data.Aircraft[SelectedAircraftIndex];
			aircraft.ModelKey = key;
			aircraft.ModelName = name;
			ModelSelectionChanged?.Invoke(aircraft);
		}
	}

	public RadarScenarioAircraft AddAircraft(RadarScenarioData data, Vector2 position)
	{
		if (data.Aircraft.Count >= 12)
		{
			StatusKey = "map.limit";
			return null;
		}
		int id = 1;
		for (int i = 0; i < data.Aircraft.Count; i++)
		{
			id = Math.Max(id, data.Aircraft[i].Id + 1);
		}
		RadarScenarioAircraft aircraft = new RadarScenarioAircraft();
		aircraft.Id = id;
		aircraft.Name = SelectedModelName + " " + id.ToString();
		aircraft.ModelName = SelectedModelName;
		aircraft.ModelKey = SelectedModelKey;
		Vector2 pos = ClampWorld(position);
		RadarScenarioWaypoint waypoint = new RadarScenarioWaypoint(pos, DefaultAltitude);
		aircraft.Route.Add(waypoint);
		data.Aircraft.Add(aircraft);
		StatusKey = null;
		SelectedAircraftIndex = data.Aircraft.Count - 1;
		SelectedWaypointIndex = 0;
		return aircraft;
	}

	public RadarScenarioStation AddStation(RadarScenarioData data, Vector2 position, bool defence)
	{
		int posts = 0;
		int id = 1;
		for (int i = 0; i < data.Stations.Count; i++)
		{
			if (data.Stations[i].IsDefencePost == defence)
			{
				posts++;
			}
			id = Math.Max(id, data.Stations[i].Id + 1);
		}
		if (posts >= (defence ? RadarScenarioData.MaxDefencePosts : RadarScenarioData.MaxRadars))
		{
			StatusKey = "map.limit";
			return null;
		}
		RadarScenarioStation station = new RadarScenarioStation();
		station.Id = id;
		station.Position = ClampWorld(position);
		station.IsDefencePost = defence;
		station.PresetIndex = 3;
		data.Stations.Add(station);
		StatusKey = null;
		SelectedStationIndex = data.Stations.Count - 1;
		return station;
	}

	public void ClearRoute(RadarScenarioData data)
	{
		if (data.Aircraft.Count > 0 && SelectedAircraftIndex >= 0 && SelectedAircraftIndex < data.Aircraft.Count)
		{
			RadarScenarioAircraft aircraft = data.Aircraft[SelectedAircraftIndex];
			if (aircraft.Route.Count > 1)
			{
				aircraft.Route.RemoveRange(1, aircraft.Route.Count - 1);
			}
			SelectedWaypointIndex = ((aircraft.Route.Count > 0) ? (aircraft.Route.Count - 1) : (-1));
			drag = Drag.DrawRoute;
		}
	}

	public void ResetView()
	{
		centre = RadarScenarioData.MapCentre;
		zoom = 1f;
	}

	private void ClampSelection(RadarScenarioData data)
	{
		SelectedAircraftIndex = Mathf.Clamp(SelectedAircraftIndex, 0, Math.Max(0, data.Aircraft.Count - 1));
		int wp = -1;
		if (data.Aircraft.Count > 0 && SelectedAircraftIndex >= 0 && SelectedAircraftIndex < data.Aircraft.Count)
		{
			RadarScenarioAircraft aircraft = data.Aircraft[SelectedAircraftIndex];
			wp = Mathf.Clamp(SelectedWaypointIndex, -1, aircraft.Route.Count - 1);
		}
		SelectedWaypointIndex = wp;
		SelectedStationIndex = Mathf.Clamp(SelectedStationIndex, -1, data.Stations.Count - 1);
	}

	private RadarScenarioAircraft SelectedAircraft(RadarScenarioData data)
	{
		if (data.Aircraft.Count > 0 && SelectedAircraftIndex >= 0 && SelectedAircraftIndex < data.Aircraft.Count)
		{
			return data.Aircraft[SelectedAircraftIndex];
		}
		return null;
	}

	private static string StationName(RadarScenarioStation station)
	{
		return L((station.IsDefencePost ? "map.post" : "map.radar")) + " " + station.Id.ToString();
	}

	private static Vector2 ClampWorld(Vector2 p)
	{
		return new Vector2(Mathf.Clamp(p.x, 0f, 900f), Mathf.Clamp(p.y, 0f, 450f));
	}

	private string HintKey()
	{
		switch (tool)
		{
		case Tool.Route:
			return "map.hint_draw";
		case Tool.Aircraft:
			return "map.hint_aircraft";
		case Tool.Radar:
			return "map.hint_radar";
		case Tool.Post:
			return "map.hint_post";
		default:
			return "map.hint_select";
		}
	}

	private void EnsureTerrain(int seed, bool coast)
	{
		if (terrain != null && terrainSeed == seed && terrainCoast == coast)
		{
			return;
		}
		if (terrain != null)
		{
			UnityEngine.Object.Destroy(terrain);
		}
		terrain = new Texture2D(1024, 512, TextureFormat.RGB24, mipChain: false, linear: false);
		terrain.name = "RadarScenarioMapTerrain";
		terrain.filterMode = FilterMode.Bilinear;
		terrain.wrapMode = TextureWrapMode.Clamp;
		Color32[] pixels = new Color32[524288];
		for (int y = 0; y < 512; y++)
		{
			for (int x = 0; x < 1024; x++)
			{
				Vector2 point = new Vector2(((float)x + 0.5f) * 900f * 0.0009765625f, ((float)y + 0.5f) * 450f * 0.001953125f);
				RadarScenarioTerrainSample sample = RadarScenarioTerrain.Sample(point, seed, coast);
				pixels[y * 1024 + x] = (Color32)RadarScenarioTerrain.MapColor(sample);
			}
		}
		terrain.SetPixels32(pixels);
		terrain.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		terrainSeed = seed;
		terrainCoast = coast;
	}

	private void DrawMap(Rect map, RadarScenarioData data)
	{
		RadarScenarioMapDraw.ClipBounds = map;
		float halfW = 900f / zoom * 0.5f;
		float halfH = 450f / zoom * 0.5f;
		Rect texCoords = new Rect((centre.x - halfW) / 900f, (centre.y - halfH) / 450f, 900f / zoom / 900f, 450f / zoom / 450f);
		GUI.DrawTextureWithTexCoords(map, terrain, texCoords);
		Color grid = new Color(0.15f, 0.2f, 0.19f, 0.15f);
		for (int x = 0; x <= 900; x += 50)
		{
			float px = map.x + WorldToMap(new Vector2(x, 0f), map).x;
			RadarScenarioMapDraw.Line(new Vector2(px, map.y), new Vector2(px, map.y + map.height), grid, 1f);
			RadarScenarioMapDraw.Label(new Rect(px + 3f, map.y + 2f, 38f, 22f), x.ToString(), small);
		}
		for (int y = 0; y <= 450; y += 50)
		{
			float py = map.y + WorldToMap(new Vector2(0f, y), map).y;
			RadarScenarioMapDraw.Line(new Vector2(map.x, py), new Vector2(map.x + map.width, py), grid, 1f);
			RadarScenarioMapDraw.Label(new Rect(map.x + 3f, py - 24f, 38f, 22f), y.ToString(), small);
		}
		for (int i = 0; i < data.Stations.Count; i++)
		{
			if (data.DetectionModel == RadarDetectionModel.Physical)
			{
				break;
			}
			RadarScenarioStation station = data.Stations[i];
			if (RadarScenarioGroundCatalog.IsRadar(station))
			{
				Vector2 p = WorldToMap(station.Position, map) + new Vector2(map.x, map.y);
				float r = CoverageMapRadius(station, map.width, zoom);
				RadarScenarioMapDraw.Circle(p, r, new Color(0.02f, 0.35f, 0.48f, (i == SelectedStationIndex) ? 0.95f : 0.45f), (i == SelectedStationIndex) ? 2.5f : 1.5f);
			}
		}
		if (data.DetectionModel == RadarDetectionModel.Physical)
		{
			physicalPanel.DrawMap(map, data, (Vector2 w) => WorldToMap(w, map) + new Vector2(map.x, map.y), zoom * map.width / 900f, small, SelectedAircraftIndex);
		}
		for (int i = 0; i < data.Aircraft.Count; i++)
		{
			DrawAircraft(map, data.Aircraft[i], i);
		}
		for (int i = 0; i < data.Stations.Count; i++)
		{
			RadarScenarioStation station = data.Stations[i];
			Vector2 p = WorldToMap(station.Position, map) + new Vector2(map.x, map.y);
			Color color = (station.IsDefencePost ? new Color(0.04f, 0.24f, 0.24f, 1f) : new Color(0.5f, 0.12f, 0.09f, 1f));
			if (i == SelectedStationIndex)
			{
				RadarScenarioMapDraw.Circle(p, 17f, new Color(1f, 0.97f, 0.65f, 1f), 3f);
			}
			if (station.IsDefencePost)
			{
				RadarScenarioMapDraw.Fill(new Rect(p.x - 7f, p.y - 7f, 14f, 14f), color);
				RadarScenarioMapDraw.Line(p + new Vector2(-11f, 0f), p + new Vector2(11f, 0f), color, 2f);
				RadarScenarioMapDraw.Line(p + new Vector2(0f, -11f), p + new Vector2(0f, 11f), color, 2f);
			}
			else
			{
				RadarScenarioMapDraw.Circle(p, 9f, color, 3f);
				RadarScenarioMapDraw.Line(p, p + new Vector2(0f, -16f), color, 3f);
				RadarScenarioMapDraw.Line(new Vector2(p.x - 7f, p.y - 16f), new Vector2(p.x + 7f, p.y - 16f), color, 2f);
			}
			MapLabel(new Rect(p.x + 15f, p.y - 4f, 180f, 24f), StationName(station));
		}
		RadarScenarioMapDraw.ClipBounds = null;
		Color border = new Color(0.24f, 0.29f, 0.29f, 1f);
		RadarScenarioMapDraw.Fill(new Rect(map.x, map.y, map.width, 2f), border);
		RadarScenarioMapDraw.Fill(new Rect(map.x, map.y + map.height - 2f, map.width, 2f), border);
		RadarScenarioMapDraw.Fill(new Rect(map.x, map.y, 2f, map.height), border);
		RadarScenarioMapDraw.Fill(new Rect(map.x + map.width - 2f, map.y, 2f, map.height), border);
	}

	public static float CoverageMapRadius(RadarScenarioStation station, float mapWidth, float mapZoom)
	{
		if (!RadarScenarioGroundCatalog.IsRadar(station))
		{
			return 0f;
		}
		return RadarScenarioGroundCatalog.CoverageRadius(station.PresetIndex) * mapWidth * mapZoom / 900f;
	}

	private void DrawAircraft(Rect map, RadarScenarioAircraft aircraft, int index)
	{
		Color color = AircraftColors[index % AircraftColors.Length];
		color.a = ((index == SelectedAircraftIndex) ? color.a : 0.6f);
		for (int i = 1; i < aircraft.Route.Count; i++)
		{
			Vector2 prev = WorldToMap(aircraft.Route[i - 1].Position, map) + new Vector2(map.x, map.y);
			Vector2 cur = WorldToMap(aircraft.Route[i].Position, map) + new Vector2(map.x, map.y);
			RadarScenarioMapDraw.Line(prev, cur, color, (index == SelectedAircraftIndex) ? 3f : 2f);
			if (Vector2.Distance(prev, cur) > 38f)
			{
				Vector2 mid = prev + (cur - prev) * 0.6f;
				Vector2 dir = cur - prev;
				dir.Normalize();
				DrawArrow(mid, dir, color, 6f);
			}
		}
		if (aircraft.Loop && aircraft.Route.Count > 2)
		{
			Vector2 last = WorldToMap(aircraft.Route[aircraft.Route.Count - 1].Position, map) + new Vector2(map.x, map.y);
			Vector2 first = WorldToMap(aircraft.Route[0].Position, map) + new Vector2(map.x, map.y);
			float dist = Vector2.Distance(last, first);
			if (dist > 0.01f)
			{
				Vector2 dir = (first - last) / dist;
				float t = 0f;
				while (t < dist)
				{
					float end = Mathf.Min(t + 7f, dist);
					RadarScenarioMapDraw.Line(last + dir * t, last + dir * end, color, 2f);
					t += 13f;
				}
			}
		}
		if (aircraft.Route.Count > 0)
		{
			Vector2 p0 = WorldToMap(aircraft.Route[0].Position, map) + new Vector2(map.x, map.y);
			Vector2 dir;
			if (aircraft.Route.Count > 1)
			{
				Vector2 p1 = WorldToMap(aircraft.Route[1].Position, map) + new Vector2(map.x, map.y);
				dir = p1 - p0;
				dir.Normalize();
			}
			else
			{
				dir = Vector2.up;
			}
			if (dir.sqrMagnitude < 0.1f)
			{
				dir = Vector2.up;
			}
			Texture texture = ModelTexture?.Invoke(aircraft.ModelKey);
			if (texture != null)
			{
				Color prevColor = GUI.color;
				GUI.color = ((index == SelectedAircraftIndex) ? Color.white : new Color(1f, 1f, 1f, 0.75f));
				GUI.BeginGroup(map);
				GUI.DrawTexture(new Rect(p0.x - map.x - 20f, p0.y - map.y - 20f, 40f, 40f), texture, ScaleMode.ScaleToFit, alphaBlend: true);
				GUI.EndGroup();
				GUI.color = prevColor;
			}
			else
			{
				DrawArrow(p0, dir, Color.white, 13f);
				DrawArrow(p0, dir, color, 10f);
				RadarScenarioMapDraw.Line(p0 - dir * 8f, p0 + dir * 11f, color, 3f);
			}
			MapLabel(new Rect(p0.x + 23f, p0.y - 27f, 190f, 25f), aircraft.Name);
		}
	}

	private static void DrawArrow(Vector2 p, Vector2 direction, Color color, float size)
	{
		Vector2 tip = p + direction * size;
		Vector2 back = p - direction * size * 0.65f;
		Vector2 perp = new Vector2(0f - direction.y, direction.x) * size * 0.65f;
		RadarScenarioMapDraw.Line(tip, back + perp, color, 2.5f);
		RadarScenarioMapDraw.Line(tip, back - perp, color, 2.5f);
	}

	private void MapLabel(Rect rect, string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return;
		}
		GUIContent content = new GUIContent(value);
		float w = Mathf.Min(small.CalcSize(content).x + 6f, rect.width);
		RadarScenarioMapDraw.Fill(new Rect(rect.x, rect.y, w, rect.height), new Color(0.95f, 0.96f, 0.9f, 0.82f));
		RadarScenarioMapDraw.Label(new Rect(rect.x + 3f, rect.y, w - 3f, rect.height), value, small);
	}

	private void DrawLegend(Rect rect)
	{
		string[] keys = new string[4] { "map.plain", "map.forest", "map.city", "map.mountain" };
		Color[] colors = new Color[4]
		{
			new Color(0.59f, 0.66f, 0.48f, 1f),
			new Color(0.23f, 0.43f, 0.27f, 1f),
			new Color(0.54f, 0.53f, 0.51f, 1f),
			new Color(0.56f, 0.47f, 0.34f, 1f)
		};
		float w = rect.width / keys.Length;
		for (int i = 0; i < keys.Length; i++)
		{
			RadarScenarioMapDraw.Fill(new Rect(rect.x + i * w, rect.y + 6f, 15f, 15f), colors[i]);
			GUI.Label(new Rect(rect.x + i * w + 21f, rect.y, w - 21f, 26f), L(keys[i]), small);
		}
	}

	public static Rect FitMapRect(Rect available)
	{
		float z = Mathf.Max(0f, Mathf.Min(available.width / 900f, available.height / 450f));
		return new Rect(available.x + available.width * 0.5f - z * 900f * 0.5f, available.y + available.height * 0.5f - z * 450f * 0.5f, z * 900f, z * 450f);
	}

	public static Vector2 ProjectToMap(Vector2 world, Rect map, Vector2 viewCentre, float viewZoom)
	{
		float scale = viewZoom * map.width / 900f;
		return new Vector2((world.x - viewCentre.x) * scale + map.width * 0.5f, map.height * 0.5f - (world.y - viewCentre.y) * scale);
	}

	public static Vector2 UnprojectFromMap(Vector2 local, Rect map, Vector2 viewCentre, float viewZoom)
	{
		float scale = viewZoom * map.width / 900f;
		return new Vector2((local.x - map.width * 0.5f) / scale + viewCentre.x, (map.height * 0.5f - local.y) / scale + viewCentre.y);
	}

	private Vector2 WorldToMap(Vector2 world, Rect map)
	{
		return ProjectToMap(world, map, centre, zoom);
	}

	private Vector2 MapToWorld(Vector2 local, Rect map)
	{
		return UnprojectFromMap(local, map, centre, zoom);
	}

	private void ClampCentre()
	{
		float hw = 900f / zoom * 0.5f;
		float hh = 450f / zoom * 0.5f;
		centre.x = Mathf.Clamp(centre.x, hw, 900f - hw);
		centre.y = Mathf.Clamp(centre.y, hh, 450f - hh);
	}

	private void HandleMapInput(Rect map, RadarScenarioData data)
	{
		Event e = Event.current;
		int id = GUIUtility.GetControlID("RadarScenarioMapInput".GetHashCode(), FocusType.Passive, map);
		Vector2 mouse = e.mousePosition;
		Vector2 local = new Vector2(mouse.x - map.x, mouse.y - map.y);
		bool inMap = mouse.x >= map.x && mouse.x < map.x + map.width && mouse.y >= map.y && mouse.y < map.y + map.height;
		if (e.type == EventType.ScrollWheel && inMap)
		{
			Vector2 worldBefore = MapToWorld(local, map);
			float k = (float)Math.Pow(1.13, -e.delta.y);
			zoom = Mathf.Clamp(zoom * k, 1f, 5f);
			Vector2 worldAfter = MapToWorld(local, map);
			centre.x += worldBefore.x - worldAfter.x;
			centre.y += worldBefore.y - worldAfter.y;
			ClampCentre();
			e.Use();
			return;
		}
		if (e.type == EventType.MouseDown && inMap && (e.button == 0 || e.button == 2))
		{
			GUIUtility.hotControl = id;
			GUI.FocusControl(null);
			drag = Drag.None;
			if (e.button == 2)
			{
				drag = Drag.Pan;
			}
			else
			{
				BeginLeftDrag(map, local, data);
			}
			e.Use();
			return;
		}
		if (e.type == EventType.MouseDrag && GUIUtility.hotControl == id)
		{
			Vector2 world = ClampWorld(MapToWorld(local, map));
			RadarScenarioAircraft aircraft = SelectedAircraft(data);
			if (drag == Drag.Pan)
			{
				float s = 900f / (zoom * map.width);
				centre.x += -e.delta.x * s;
				centre.y += e.delta.y * s;
				ClampCentre();
				e.Use();
				return;
			}
			if (inMap && drag == Drag.DrawRoute)
			{
				if (aircraft != null && aircraft.Route.Count > 0)
				{
					Vector2 last = aircraft.Route[aircraft.Route.Count - 1].Position;
					if (Vector2.Distance(last, world) >= 1.8f / zoom)
					{
						AppendWaypoint(aircraft, world);
					}
				}
				e.Use();
				return;
			}
			if (drag == Drag.Waypoint)
			{
				if (aircraft != null && SelectedWaypointIndex >= 0 && SelectedWaypointIndex < aircraft.Route.Count)
				{
					aircraft.Route[SelectedWaypointIndex].Position = world;
				}
				e.Use();
				return;
			}
			if (drag == Drag.Station)
			{
				if (SelectedStationIndex >= 0 && data.Stations != null && SelectedStationIndex < data.Stations.Count)
				{
					data.Stations[SelectedStationIndex].Position = world;
				}
				e.Use();
				return;
			}
			e.Use();
			return;
		}
		if (e.type == EventType.MouseUp && GUIUtility.hotControl == id)
		{
			GUIUtility.hotControl = 0;
			drag = Drag.None;
			e.Use();
		}
	}

	private void BeginLeftDrag(Rect map, Vector2 local, RadarScenarioData data)
	{
		Vector2 world = ClampWorld(MapToWorld(local, map));
		if (tool == Tool.Aircraft)
		{
			if (AddAircraft(data, world) != null)
			{
				tool = Tool.Route;
				drag = Drag.DrawRoute;
			}
			return;
		}
		if (tool == Tool.Radar || tool == Tool.Post)
		{
			if (AddStation(data, world, tool == Tool.Post) != null)
			{
				tool = Tool.Select;
				drag = Drag.Station;
			}
			return;
		}
		int bestAircraft = -1;
		int bestWaypoint = -1;
		float bestDist = 13f;
		for (int a = 0; a < data.Aircraft.Count; a++)
		{
			if (tool == Tool.Route && a == SelectedAircraftIndex)
			{
				continue;
			}
			for (int w = 0; w < data.Aircraft[a].Route.Count; w++)
			{
				Vector2 mp = WorldToMap(data.Aircraft[a].Route[w].Position, map);
				float d = Vector2.Distance(local, mp);
				if (d < bestDist)
				{
					bestDist = d;
					bestAircraft = a;
					bestWaypoint = w;
				}
			}
		}
		if (bestAircraft >= 0)
		{
			SelectAircraft(data, bestAircraft);
			SelectedWaypointIndex = bestWaypoint;
			if (tool == Tool.Route)
			{
				drag = ((bestWaypoint == data.Aircraft[bestAircraft].Route.Count - 1) ? Drag.DrawRoute : Drag.None);
			}
			else
			{
				drag = Drag.Waypoint;
			}
			return;
		}
		if (tool == Tool.Route)
		{
			RadarScenarioAircraft aircraft = SelectedAircraft(data);
			if (aircraft == null)
			{
				StatusKey = "map.no_plane";
				return;
			}
			AppendWaypoint(aircraft, world);
			drag = Drag.DrawRoute;
			return;
		}
		float stationBest = 18f;
		int bestStation = -1;
		for (int i = 0; i < data.Stations.Count; i++)
		{
			Vector2 mp2 = WorldToMap(data.Stations[i].Position, map);
			float d2 = Vector2.Distance(local, mp2);
			if (d2 < stationBest)
			{
				stationBest = d2;
			}
			if (d2 < 18f)
			{
				bestStation = i;
			}
		}
		if (bestStation >= 0)
		{
			SelectedStationIndex = bestStation;
			drag = Drag.Station;
		}
	}

	private void AppendWaypoint(RadarScenarioAircraft aircraft, Vector2 world)
	{
		if (aircraft.Route.Count >= 256)
		{
			StatusKey = "map.limit";
			return;
		}
		RadarScenarioWaypoint waypoint = new RadarScenarioWaypoint(world, DefaultAltitude);
		aircraft.Route.Add(waypoint);
		StatusKey = null;
		SelectedWaypointIndex = aircraft.Route.Count - 1;
	}

	public void Dispose()
	{
		if (terrain != null)
		{
			UnityEngine.Object.Destroy(terrain);
		}
		terrain = null;
		terrainSeed = int.MinValue;
	}
}
