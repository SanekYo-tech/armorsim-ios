using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RadarScenarioRadarScreen : IDisposable
{
	public readonly struct EchoSnapshot
	{
		public readonly Vector2 Position;

		public readonly float Strength;

		public readonly float SampleTime;

		public readonly int ContactNumber;

		public readonly bool IsTerrain;

		internal EchoSnapshot(Vector2 position, float strength, float sampleTime, int contactNumber, bool terrain)
		{
			Position = position;
			Strength = strength;
			SampleTime = sampleTime;
			ContactNumber = contactNumber;
			IsTerrain = terrain;
		}
	}

	private sealed class Echo
	{
		public string Key;

		public Vector2 Position;

		public float Strength;

		public float SampleTime;

		public bool Terrain;

		public int ContactNumber;
	}

	private string launchStatusKey;

	private Vector2 markScroll;

	private readonly List<Vector2> trackHistory;

	private readonly HashSet<int> activeAircraft;

	private string trackedKey;

	private int trackedContact;

	private float trackedSampleTime;

	public const float SweepPeriodSeconds = 8f;

	public const float DetectionThreshold = 0.22f;

	public const int MaxTerrainEchoes = 1200;

	private readonly Dictionary<string, Echo> aircraftEchoes;

	private readonly List<Echo> terrainEchoes;

	private readonly List<Echo> markedEchoes;

	private readonly HashSet<string> detectedAircraft;

	private readonly HashSet<string> markedAircraft;

	private readonly HashSet<string> falseMarks;

	private readonly List<EchoSnapshot> snapshots;

	private readonly List<string> staleKeys;

	private readonly Color green;

	private readonly RadarScenarioTerrainEchoField terrainField;

	private readonly Func<string, RadarScenarioShapeProfile> shapeForModel;

	private int stationIndex;

	private int cachedSeed;

	private int nextContact;

	private string cachedStationId;

	private Vector2 stationPosition;

	private float range;

	private float gain;

	private float clutter;

	private float persistence;

	private float lastElapsed;

	private float now;

	private bool worldDirty;

	private bool rings;

	private Echo selected;

	private Texture2D gridTexture;

	private Texture2D plainGridTexture;

	private Texture2D dotTexture;

	private GUIStyle titleStyle;

	private GUIStyle labelStyle;

	private GUIStyle dimStyle;

	private GUIStyle buttonStyle;

	private GUIStyle smallStyle;

	public RadarScenarioLaunchGame Launches { get; set; }

	public bool LaunchEnabled { get; set; }

	public int SelectedLauncherId { get; set; }

	public int LockedTargetId
	{
		get
		{
			if (int.TryParse(trackedKey, out var result))
			{
				return result;
			}
			return 0;
		}
	}

	public int LockedContactNumber => trackedContact;

	public int TrackHistoryCount => trackHistory.Count;

	public string TrackStatusKey
	{
		get
		{
			if (trackedKey == null)
			{
				return "track.none";
			}
			if (Launches != null && int.TryParse(trackedKey, out var result) && Launches.IsDestroyed(result))
			{
				return "track.destroyed";
			}
			if (int.TryParse(trackedKey, out var targetId) && ResolveTrackedTarget(targetId).Available)
			{
				return "track.locked";
			}
			return "track.lost";
		}
	}

	public float ActiveCoverageRadius { get; private set; }

	public int SelectedStationIndex
	{
		get
		{
			return stationIndex;
		}
		set
		{
			int num = ((value < 0) ? 0 : value);
			if (num != stationIndex)
			{
				stationIndex = num;
				ClearEchoes();
				worldDirty = true;
			}
		}
	}

	public float Gain
	{
		get
		{
			return gain;
		}
		set
		{
			gain = ((value < 0.4f) ? 0.4f : ((value > 3.5f) ? 3.5f : value));
		}
	}

	public float Clutter
	{
		get
		{
			return clutter;
		}
		set
		{
			clutter = ((value < 0f) ? 0f : ((value > 1f) ? 1f : value));
		}
	}

	public float Persistence
	{
		get
		{
			return persistence;
		}
		set
		{
			persistence = ((value < 0f) ? 0f : ((value > 16f) ? 16f : value));
		}
	}

	public float TerrainMinimumHeight
	{
		get
		{
			return terrainField.MinimumHeight01;
		}
		set
		{
			terrainField.MinimumHeight01 = value;
			for (int num = terrainEchoes.Count - 1; num >= 0; num--)
			{
				if (terrainEchoes[num].Terrain && !IsAlive(terrainEchoes[num]))
				{
					terrainEchoes[num].ContactNumber = 0;
					terrainEchoes.RemoveAt(num);
				}
			}
			if (selected != null && selected.Terrain && terrainField.VisibilityAt(selected.Position, now, gain, clutter, persistence) < 0.025f)
			{
				selected = null;
			}
			RefreshSnapshots();
		}
	}

	public float Range
	{
		get
		{
			return range;
		}
		set
		{
			value = ((value < 20f) ? 20f : ((value > 180f) ? 180f : value));
			if (Mathf.Abs(range - value) > 0.01f)
			{
				range = value;
				ClearEchoes();
				worldDirty = true;
			}
		}
	}

	public float SweepDegrees { get; private set; }

	public int DetectedAircraftCount => detectedAircraft.Count;

	public int MarkedAircraftCount => markedAircraft.Count;

	public int FalseMarkCount => falseMarks.Count;

	public int MarkedContactCount => markedEchoes.Count;

	public int VisibleEchoCount { get; private set; }

	public int TerrainEchoCount { get; private set; }

	public int SampleCount { get; private set; }

	public int SelectedContactNumber => selected?.ContactNumber ?? 0;

	public IReadOnlyList<EchoSnapshot> Echoes => snapshots;

	public bool TryGetSelectedContact(out Vector2 observedPosition, out int contact)
	{
		observedPosition = Vector2.zero;
		contact = 0;
		if (selected == null || selected.ContactNumber <= 0)
		{
			return false;
		}
		if (DisplayStrength(selected) < 0.08f)
		{
			return false;
		}
		observedPosition = selected.Position;
		contact = selected.ContactNumber;
		return true;
	}

	public bool TryLaunchSelected(RadarScenarioData data)
	{
		if (!LaunchEnabled)
		{
			launchStatusKey = "launch.paused";
			return false;
		}
		if (Launches == null)
		{
			launchStatusKey = "launch.noLauncher";
			return false;
		}
		RadarScenarioLaunchGame.TrackedTarget trackedTarget = ResolveTrackedTarget(LockedTargetId);
		if (!trackedTarget.Available)
		{
			launchStatusKey = "track.needLock";
			return false;
		}
		bool result = Launches.TryLaunchTracked(data, SelectedLauncherId, trackedTarget.ObservedPosition, trackedContact, LockedTargetId, now, out RadarScenarioLaunchGame.LaunchOutcome outcome);
		switch (outcome)
		{
		case RadarScenarioLaunchGame.LaunchOutcome.Launched:
			launchStatusKey = "launch.sent";
			break;
		case RadarScenarioLaunchGame.LaunchOutcome.NoLauncher:
			launchStatusKey = "launch.noLauncher";
			break;
		case RadarScenarioLaunchGame.LaunchOutcome.NoContact:
			launchStatusKey = "launch.noContact";
			break;
		case RadarScenarioLaunchGame.LaunchOutcome.Reloading:
			launchStatusKey = "launch.reload";
			break;
		case RadarScenarioLaunchGame.LaunchOutcome.Empty:
			launchStatusKey = "launch.empty";
			break;
		default:
			launchStatusKey = "launch.noContact";
			break;
		}
		return result;
	}

	private void DrawLauncherControls(Rect area, RadarScenarioData data)
	{
		if (Launches == null)
		{
			return;
		}
		GUI.Label(new Rect(area.x, area.y, area.width, 30f), T("launch.title"), labelStyle);
		if (data.Stations.Find((RadarScenarioStation s) => s.IsDefencePost && s.Id == SelectedLauncherId) == null)
		{
			RadarScenarioStation station = data.Stations.Find((RadarScenarioStation s) => s.IsDefencePost);
			SelectedLauncherId = ((station != null) ? station.Id : -1);
		}
		int num = 0;
		float num2 = area.y + 34f;
		foreach (RadarScenarioStation station2 in data.Stations)
		{
			if (station2.IsDefencePost)
			{
				float num3 = (area.width - 8f) * 0.5f;
				GUI.color = ((station2.Id == SelectedLauncherId) ? new Color(1f, 0.82f, 0.43f, 1f) : Color.white);
				if (GUI.Button(new Rect(area.x + (float)(num % 2) * (num3 + 8f), num2 + (float)(num / 2) * 39f, num3, 35f), T("launch.unit") + " " + station2.Id.ToString(), buttonStyle))
				{
					SelectedLauncherId = station2.Id;
				}
				num++;
			}
		}
		GUI.color = Color.white;
		num2 += (float)((num + 1) / 2) * 39f;
		if (num == 0)
		{
			GUI.Label(new Rect(area.x, num2, area.width, 64f), T("launch.noLauncher"), dimStyle);
			GUI.Label(new Rect(area.x, num2, area.width, Mathf.Max(54f, area.yMax - num2)), T("track.gameHint"), dimStyle);
			return;
		}
		int ammo = Launches.AmmoRemaining(SelectedLauncherId);
		float cooldown = Launches.CooldownRemaining(SelectedLauncherId, now);
		GUI.Label(new Rect(area.x, num2, area.width, 26f), T("launch.ammo") + " " + ammo.ToString() + " / 4   |   " + T("launch.active") + " " + Launches.ActiveCount.ToString(), dimStyle);
		num2 += 26f;
		GUI.Label(new Rect(area.x, num2, area.width, 26f), T("track.hits") + " " + Launches.HitCount.ToString() + "   |   " + T("track.misses") + " " + Launches.MissedCount.ToString(), dimStyle);
		num2 += 29f;
		RadarScenarioLaunchGame.TrackedTarget trackedTarget = ResolveTrackedTarget(LockedTargetId);
		bool flag = GUI.enabled;
		GUI.enabled = flag && LaunchEnabled && ammo > 0 && cooldown <= 0f && trackedTarget.Available;
		Color backgroundColor = GUI.backgroundColor;
		GUI.backgroundColor = new Color(0.78f, 0.63f, 0.29f, 1f);
		string text = T("launch.fire") + ((trackedTarget.Available) ? (" M" + trackedContact.ToString("00")) : "");
		if (GUI.Button(new Rect(area.x, num2, area.width, 40f), text, buttonStyle))
		{
			TryLaunchSelected(data);
		}
		GUI.backgroundColor = backgroundColor;
		GUI.enabled = flag;
		num2 += 47f;
		string text2;
		if (!LaunchEnabled)
		{
			text2 = "launch.paused";
		}
		else if (ammo == 0)
		{
			text2 = "launch.empty";
		}
		else if (cooldown > 0f)
		{
			text2 = "launch.reload";
		}
		else if (!trackedTarget.Available)
		{
			text2 = "track.needLock";
		}
		else
		{
			text2 = launchStatusKey;
		}
		if (!string.IsNullOrEmpty(text2))
		{
			string text3 = T(text2);
			string text4 = ((cooldown > 0f && LaunchEnabled) ? (" " + cooldown.ToString("F1") + " s") : "");
			GUI.Label(new Rect(area.x, num2, area.width, 40f), text3 + text4, dimStyle);
			num2 += 43f;
		}
		GUI.Label(new Rect(area.x, num2, area.width, Mathf.Max(54f, area.yMax - num2)), T("track.gameHint"), dimStyle);
	}

	private void DrawLaunchAnimations(RadarScenarioData data, Vector2 center, float radius)
	{
		if (Launches == null)
		{
			return;
		}
		Color color = new Color(1f, 0.74f, 0.19f, 1f);
		foreach (RadarScenarioStation station in data.Stations)
		{
			if (station.IsDefencePost && OnScope(station.Position))
			{
				Vector2 vector = ScopePoint(station.Position, center, radius);
				Line(new Vector2(vector.x - 6f, vector.y - 6f), new Vector2(vector.x + 6f, vector.y + 6f), color, 2f);
				Line(new Vector2(vector.x + 6f, vector.y - 6f), new Vector2(vector.x - 6f, vector.y + 6f), color, 2f);
				GUI.Label(new Rect(vector.x + 8f, vector.y - 11f, 100f, 25f), T("launch.short") + station.Id.ToString(), smallStyle);
			}
		}
		foreach (RadarScenarioLaunchGame.Projectile projectile in Launches.Projectiles)
		{
			if (projectile.Complete && projectile.ArrivalAlpha <= 0f)
			{
				continue;
			}
			Color color2 = (projectile.ContactLost ? new Color(1f, 0.35f, 0.15f, 1f) : color);
			Vector2 vector2 = projectile.CurrentPosition;
			Vector2 vector3 = Vector2.Lerp(projectile.LaunchPosition, vector2, 0.86f);
			for (int i = 0; i < 12; i++)
			{
				Vector2 a = Vector2.Lerp(vector3, vector2, (float)i / 12f);
				Vector2 b = Vector2.Lerp(vector3, vector2, (float)(i + 1) / 12f);
				if (OnScope(a) && OnScope(b))
				{
					Line(ScopePoint(a, center, radius), ScopePoint(b, center, radius), color2, 2.5f);
				}
			}
			if (OnScope(vector2))
			{
				Vector2 vector4 = ScopePoint(vector2, center, radius);
				if (!projectile.Complete)
				{
					GUI.color = color2;
					GUI.DrawTexture(new Rect(vector4.x - 3f, vector4.y - 3f, 6f, 6f), Texture2D.whiteTexture);
					GUI.color = Color.white;
					GUI.Label(new Rect(vector4.x + 8f, vector4.y - 12f, 80f, 25f), T("launch.short") + projectile.LauncherId.ToString(), smallStyle);
				}
				else
				{
					RadarScenarioMapDraw.Circle(vector4, (1f - projectile.ArrivalAlpha) * 14f + 5f, new Color(1f, 0.74f, 0.19f, projectile.ArrivalAlpha), 2f);
					string key;
					if (projectile.Hit)
					{
						key = "track.destroyed";
					}
					else if (projectile.Missed)
					{
						key = "track.miss";
					}
					else
					{
						key = "launch.arrived";
					}
					GUI.Label(new Rect(vector4.x - 75f, vector4.y + 20f, 150f, 30f), T(key), smallStyle);
				}
			}
		}
		GUI.color = Color.white;
	}

	private bool OnScope(Vector2 point)
	{
		Vector2 vector = point - stationPosition;
		return range * range >= vector.x * vector.x + vector.y * vector.y;
	}

	private Vector2 ScopePoint(Vector2 point, Vector2 center, float radius)
	{
		float num = radius / range;
		Vector2 vector = point - stationPosition;
		return new Vector2(vector.x * num + center.x, -vector.y * num + center.y);
	}

	public bool TryLockSelected()
	{
		if (selected == null || selected.Terrain || selected.ContactNumber <= 0)
		{
			return false;
		}
		if (DisplayStrength(selected) < 0.08f)
		{
			return false;
		}
		if (!int.TryParse(selected.Key, out var result) || result <= 0)
		{
			return false;
		}
		if (!activeAircraft.Contains(result))
		{
			return false;
		}
		if (Launches != null && Launches.IsDestroyed(result))
		{
			return false;
		}
		if (selected.Key != trackedKey)
		{
			ClearTrack();
		}
		trackedKey = selected.Key;
		trackedContact = selected.ContactNumber;
		ObserveTrack(selected);
		return true;
	}

	public void ClearTrack()
	{
		trackedKey = null;
		trackedContact = 0;
		trackedSampleTime = float.NegativeInfinity;
		trackHistory.Clear();
	}

	public RadarScenarioLaunchGame.TrackedTarget ResolveTrackedTarget(int targetId)
	{
		RadarScenarioLaunchGame.TrackedTarget result = default(RadarScenarioLaunchGame.TrackedTarget);
		if (targetId <= 0)
		{
			return result;
		}
		if (trackedKey == null || !int.TryParse(trackedKey, out var result2) || result2 != targetId)
		{
			return result;
		}
		if (!activeAircraft.Contains(targetId))
		{
			return result;
		}
		if (Launches != null && Launches.IsDestroyed(targetId))
		{
			return result;
		}
		if (aircraftEchoes.TryGetValue(trackedKey, out var echo) && IsAlive(echo) && DisplayStrength(echo) >= 0.08f)
		{
			result.Available = true;
			result.ObservedPosition = echo.Position;
		}
		return result;
	}

	private void ObserveTrack(Echo echo)
	{
		if (echo == null || echo.Key != trackedKey || echo.Terrain)
		{
			return;
		}
		echo.ContactNumber = trackedContact;
		if (!markedEchoes.Contains(echo))
		{
			markedEchoes.Add(echo);
		}
		if (echo.SampleTime == trackedSampleTime)
		{
			return;
		}
		trackedSampleTime = echo.SampleTime;
		if (trackHistory.Count == 12)
		{
			trackHistory.RemoveAt(0);
		}
		trackHistory.Add(echo.Position);
	}

	public void RemoveDestroyedEchoes()
	{
		if (Launches == null)
		{
			return;
		}
		staleKeys.Clear();
		foreach (KeyValuePair<string, Echo> pair in aircraftEchoes)
		{
			if (int.TryParse(pair.Key, out var result) && Launches.IsDestroyed(result))
			{
				staleKeys.Add(pair.Key);
			}
		}
		foreach (string key in staleKeys)
		{
			Echo value = aircraftEchoes[key];
			aircraftEchoes.Remove(key);
			markedEchoes.Remove(value);
			if (selected == value)
			{
				selected = null;
			}
		}
		RefreshSnapshots();
	}

	private void DrawTrack(Vector2 center, float radius)
	{
		if (trackedKey == null)
		{
			return;
		}
		RadarScenarioLaunchGame.TrackedTarget trackedTarget = ResolveTrackedTarget(LockedTargetId);
		Color color = (trackedTarget.Available ? new Color(0.55f, 1f, 0.72f, 0.7f) : new Color(0.8f, 0.72f, 0.3f, 0.35f));
		for (int i = 1; i < trackHistory.Count; i++)
		{
			if (OnScope(trackHistory[i - 1]) && OnScope(trackHistory[i]))
			{
				Line(ScopePoint(trackHistory[i - 1], center, radius), ScopePoint(trackHistory[i], center, radius), color, 1.5f);
			}
		}
		if (trackHistory.Count == 0)
		{
			return;
		}
		Vector2 last = trackHistory[trackHistory.Count - 1];
		if (!OnScope(last))
		{
			return;
		}
		Vector2 vector = ScopePoint(last, center, radius);
		if (Launches != null && Launches.IsDestroyed(LockedTargetId))
		{
			Line(new Vector2(vector.x - 12f, vector.y - 12f), new Vector2(vector.x + 12f, vector.y + 12f), color, 2f);
			Line(new Vector2(vector.x + 12f, vector.y - 12f), new Vector2(vector.x - 12f, vector.y + 12f), color, 2f);
			return;
		}
		for (int j = -1; j <= 1; j += 2)
		{
			for (int k = -1; k <= 1; k += 2)
			{
				Vector2 vector2 = new Vector2(vector.x + j * 17, vector.y + k * 17);
				Line(vector2, new Vector2(vector.x + j * 17 - j * 9, vector.y + k * 17), color, 2f);
				Line(vector2, new Vector2(vector.x + j * 17, vector.y + k * 17 - k * 9), color, 2f);
			}
		}
	}

	public RadarScenarioRadarScreen(Func<string, RadarScenarioShapeProfile> shapeForModel)
	{
		SelectedLauncherId = -1;
		trackHistory = new List<Vector2>(12);
		activeAircraft = new HashSet<int>();
		trackedSampleTime = float.NegativeInfinity;
		aircraftEchoes = new Dictionary<string, Echo>();
		terrainEchoes = new List<Echo>();
		markedEchoes = new List<Echo>();
		detectedAircraft = new HashSet<string>();
		markedAircraft = new HashSet<string>();
		falseMarks = new HashSet<string>();
		snapshots = new List<EchoSnapshot>();
		staleKeys = new List<string>();
		green = new Color(0.23f, 0.95f, 0.37f, 1f);
		terrainField = new RadarScenarioTerrainEchoField(384);
		if (shapeForModel == null)
		{
			throw new ArgumentNullException("shapeForModel");
		}
		this.shapeForModel = shapeForModel;
		cachedSeed = int.MinValue;
		nextContact = 1;
		lastElapsed = -1f;
		range = 120f;
		gain = 1.4f;
		clutter = 0.55f;
		persistence = 9f;
		worldDirty = true;
		rings = true;
	}

	public static bool SweepCrossed(float startDegrees, float advanceDegrees, float bearingDegrees)
	{
		if (advanceDegrees <= 0f)
		{
			return false;
		}
		if (advanceDegrees >= 360f)
		{
			return true;
		}
		float num = bearingDegrees - startDegrees;
		num -= Mathf.Floor(num / 360f) * 360f;
		if (num < 0f)
		{
			return false;
		}
		if (num > 360f)
		{
			num = 360f;
		}
		if (num <= 0f)
		{
			return false;
		}
		return advanceDegrees >= num;
	}

	public static float Bearing(Vector2 offset)
	{
		float num = Mathf.Atan2(offset.x, offset.y) * 57.29578f;
		num -= Mathf.Floor(num / 360f) * 360f;
		if (num < 0f)
		{
			return 0f;
		}
		if (num > 360f)
		{
			return 360f;
		}
		return num;
	}

	public void Tick(RadarScenarioData data, float elapsed, float dt)
	{
		if (data == null)
		{
			return;
		}
		if (now > elapsed + 0.001f)
		{
			ClearEchoes();
			detectedAircraft.Clear();
			markedAircraft.Clear();
			falseMarks.Clear();
			cachedStationId = null;
			cachedSeed = int.MinValue;
			lastElapsed = -1f;
			now = 0f;
			SweepDegrees = 0f;
			SampleCount = 0;
			nextContact = 1;
			worldDirty = true;
			ActiveCoverageRadius = 0f;
			launchStatusKey = null;
		}
		if (elapsed < 0f)
		{
			elapsed = 0f;
		}
		now = elapsed;
		float num = elapsed * 45f;
		num -= Mathf.Floor(num / 360f) * 360f;
		if (num < 0f)
		{
			num = 0f;
		}
		else if (num > 360f)
		{
			num = 360f;
		}
		SweepDegrees = num;
		int num2 = FindRadarIndex(data, stationIndex);
		if (num2 < 0)
		{
			ClearEchoes();
			ActiveCoverageRadius = 0f;
			cachedStationId = null;
			lastElapsed = now;
			return;
		}
		if (stationIndex != num2)
		{
			stationIndex = num2;
			ClearEchoes();
			worldDirty = true;
		}
		RadarScenarioStation station = data.Stations[stationIndex];
		float num3 = RadarScenarioGroundCatalog.CoverageRadius(station.Polarization);
		string text = station.Id.ToString();
		if (worldDirty || text != cachedStationId || cachedSeed != data.Seed || (stationPosition - station.Position).sqrMagnitude >= 1e-10f || ActiveCoverageRadius != num3)
		{
			ClearEchoes();
			ActiveCoverageRadius = num3;
			stationPosition = station.Position;
			cachedStationId = text;
			cachedSeed = data.Seed;
			BuildTerrain(data);
			worldDirty = false;
		}
		float num4 = lastElapsed;
		if (num4 < 0f)
		{
			float num5 = ((dt < 0f) ? 0f : dt);
			num4 = now - num5;
			if (num4 < 0f)
			{
				num4 = 0f;
			}
		}
		float num6 = now - num4;
		if (num6 < 0f)
		{
			num6 = 0f;
		}
		float num7 = num4 * 45f;
		num7 -= Mathf.Floor(num7 / 360f) * 360f;
		if (num7 < 0f)
		{
			num7 = 0f;
		}
		else if (num7 > 360f)
		{
			num7 = 360f;
		}
		lastElapsed = now;
		float num8 = num6 * 45f;
		terrainField.Advance(now, num7, num8);
		for (int i = 0; i < terrainEchoes.Count; i++)
		{
			Echo echo = terrainEchoes[i];
			if (SweepCrossed(num7, num8, Bearing(echo.Position - stationPosition)))
			{
				echo.SampleTime = now;
			}
		}
		activeAircraft.Clear();
		for (int j = 0; j < data.Aircraft.Count; j++)
		{
			RadarScenarioAircraft aircraft = data.Aircraft[j];
			if (Launches != null && Launches.IsDestroyed(aircraft.Id))
			{
				continue;
			}
			RadarScenarioPose pose = RadarScenarioGameModel.Pose(aircraft, now);
			if (pose.Finished)
			{
				continue;
			}
			activeAircraft.Add(aircraft.Id);
			Vector2 vector = pose.Position - stationPosition;
			float num9 = ((ActiveCoverageRadius <= range) ? ActiveCoverageRadius : range);
			if (vector.x * vector.x + vector.y * vector.y > num9 * num9)
			{
				continue;
			}
			if (!SweepCrossed(num7, num8, Bearing(vector)))
			{
				continue;
			}
			RadarScenarioShapeProfile shape = shapeForModel(aircraft.ModelKey);
			if (shape == null)
			{
				throw new InvalidOperationException("Missing scenario shape profile: " + aircraft.ModelKey);
			}
			float num10 = RadarScenarioGameModel.Visibility(data, aircraft, station, pose, shape);
			if (num10 * gain < DetectionThreshold)
			{
				continue;
			}
			string key = aircraft.Id.ToString();
			if (!aircraftEchoes.TryGetValue(key, out var echo2))
			{
				echo2 = new Echo();
				echo2.Key = key;
				aircraftEchoes.Add(key, echo2);
			}
			echo2.Position = pose.Position;
			echo2.Strength = num10;
			echo2.SampleTime = now;
			ObserveTrack(echo2);
			SampleCount++;
			detectedAircraft.Add(key);
		}
		staleKeys.Clear();
		foreach (KeyValuePair<string, Echo> pair in aircraftEchoes)
		{
			if (!IsAlive(pair.Value))
			{
				staleKeys.Add(pair.Key);
			}
		}
		for (int k = 0; k < staleKeys.Count; k++)
		{
			aircraftEchoes.Remove(staleKeys[k]);
		}
		for (int num11 = markedEchoes.Count - 1; num11 >= 0; num11--)
		{
			if (!IsAlive(markedEchoes[num11]))
			{
				markedEchoes[num11].ContactNumber = 0;
				markedEchoes.RemoveAt(num11);
			}
		}
		if (selected != null && !IsAlive(selected))
		{
			selected = null;
		}
		RefreshSnapshots();
	}

	private void BuildTerrain(RadarScenarioData data)
	{
		int num = (int)Math.Round(RadarScenarioData.MapWidth / 6.25);
		int num2 = (int)Math.Round(RadarScenarioData.MapHeight / 6.25);
		System.Random random = new System.Random(data.Seed);
		terrainField.Rebuild(data.Seed, stationPosition, range, ActiveCoverageRadius, terrainField.MinimumHeight01);
		int num3 = 0;
		for (int i = 0; i < num2; i++)
		{
			if (terrainEchoes.Count >= MaxTerrainEchoes)
			{
				break;
			}
			for (int j = 0; j < num; j++)
			{
				if (terrainEchoes.Count >= MaxTerrainEchoes)
				{
					break;
				}
				Vector2 vector = new Vector2(((float)random.NextDouble() * 0.7f + (float)j + 0.15f) * 6.25f, ((float)random.NextDouble() * 0.7f + (float)i + 0.15f) * 6.25f);
				float num4 = ((ActiveCoverageRadius <= range) ? ActiveCoverageRadius : range);
				if ((vector - stationPosition).sqrMagnitude > num4 * num4)
				{
					continue;
				}
				RadarScenarioTerrainSample sample = RadarScenarioTerrain.Sample(vector, data.Seed);
				float num5 = ((float)random.NextDouble() * 0.35f + 0.65f) * sample.Clutter01;
				if (0.08f > num5)
				{
					continue;
				}
				Echo echo = new Echo();
				echo.Key = "terrain_" + (num3 + j);
				echo.Position = vector;
				echo.Strength = num5;
				echo.Terrain = true;
				terrainEchoes.Add(echo);
			}
			num3 += num;
		}
	}

	private bool IsAlive(Echo echo)
	{
		if (echo == null)
		{
			return false;
		}
		if (echo.Terrain)
		{
			return terrainField.VisibilityAt(echo.Position, now, gain, clutter, persistence) >= 0.025f;
		}
		return persistence > now - echo.SampleTime;
	}

	private float DisplayStrength(Echo echo)
	{
		if (echo == null)
		{
			return 0f;
		}
		if (echo.Terrain)
		{
			return terrainField.VisibilityAt(echo.Position, now, gain, clutter, persistence);
		}
		if (!IsAlive(echo))
		{
			return 0f;
		}
		float num = now - echo.SampleTime;
		float num2 = 1f - num / persistence;
		if (num2 < 0f)
		{
			num2 = 0f;
		}
		else if (num2 > 1f)
		{
			num2 = 1f;
		}
		float num3 = echo.Strength * gain * (echo.Terrain ? clutter : 1f);
		if (num3 < 0f)
		{
			num3 = 0f;
		}
		else if (num3 > 1f)
		{
			num3 = 1f;
		}
		return num3 * num2;
	}

	private void RefreshSnapshots()
	{
		snapshots.Clear();
		VisibleEchoCount = 0;
		foreach (Echo value in aircraftEchoes.Values)
		{
			AddSnapshot(value);
		}
		for (int i = 0; i < terrainEchoes.Count; i++)
		{
			AddSnapshot(terrainEchoes[i]);
		}
	}

	private void AddSnapshot(Echo echo)
	{
		float num = DisplayStrength(echo);
		if (num < 0.025f)
		{
			return;
		}
		snapshots.Add(new EchoSnapshot(echo.Position, num, echo.SampleTime, echo.ContactNumber, echo.Terrain));
		if (echo.Terrain)
		{
			TerrainEchoCount++;
		}
		else
		{
			VisibleEchoCount++;
		}
	}

	public void Reset()
	{
		ClearEchoes();
		detectedAircraft.Clear();
		markedAircraft.Clear();
		falseMarks.Clear();
		launchStatusKey = null;
		cachedSeed = int.MinValue;
		lastElapsed = -1f;
		now = 0f;
		SweepDegrees = 0f;
		SampleCount = 0;
		nextContact = 1;
		worldDirty = true;
		ActiveCoverageRadius = 0f;
		markScroll = Vector2.zero;
	}

	private void ClearEchoes()
	{
		aircraftEchoes.Clear();
		terrainEchoes.Clear();
		markedEchoes.Clear();
		snapshots.Clear();
		selected = null;
		VisibleEchoCount = 0;
		activeAircraft.Clear();
		trackedKey = null;
		trackedContact = 0;
		trackedSampleTime = float.NegativeInfinity;
		trackHistory.Clear();
	}

	public void ClearMarks()
	{
		for (int i = 0; i < markedEchoes.Count; i++)
		{
			markedEchoes[i].ContactNumber = 0;
		}
		markedEchoes.Clear();
		selected = null;
		trackedKey = null;
		trackedContact = 0;
		trackedSampleTime = float.NegativeInfinity;
		trackHistory.Clear();
	}

	public bool MarkEchoAt(Vector2 worldPosition, float tolerance)
	{
		Echo closest = null;
		float best = tolerance * tolerance;
		foreach (Echo echo in aircraftEchoes.Values)
		{
			FindClosest(echo, worldPosition, ref closest, ref best);
		}
		for (int i = 0; i < terrainEchoes.Count; i++)
		{
			FindClosest(terrainEchoes[i], worldPosition, ref closest, ref best);
		}
		if (closest == null)
		{
			return false;
		}
		if (closest.ContactNumber == 0)
		{
			closest.ContactNumber = nextContact++;
			markedEchoes.Add(closest);
			if (closest.Terrain)
			{
				falseMarks.Add(closest.Key);
			}
			else
			{
				markedAircraft.Add(closest.Key);
			}
		}
		selected = closest;
		RefreshSnapshots();
		return true;
	}

	private void FindClosest(Echo echo, Vector2 point, ref Echo closest, ref float best)
	{
		if (DisplayStrength(echo) < 0.08f)
		{
			return;
		}
		Vector2 vector = point - echo.Position;
		float num = vector.x * vector.x + vector.y * vector.y;
		if (num <= best)
		{
			best = num;
			closest = echo;
		}
	}

	public void Draw(Rect area, RadarScenarioData data, float elapsed)
	{
		if (data == null)
		{
			return;
		}
		EnsureStyles();
		EnsureTextures();
		Color color = GUI.color;
		GUI.color = new Color(0.095f, 0.12f, 0.11f, 1f);
		GUI.DrawTexture(area, Texture2D.whiteTexture);
		GUI.color = Color.white;
		float num = area.width * 0.54f;
		float num2 = area.height - 24f;
		if (num <= num2)
		{
			num2 = num;
		}
		Rect rect = new Rect(area.x + 12f, area.y + 12f, num2, num2);
		GUI.color = new Color(0.004f, 0.018f, 0.009f, 1f);
		GUI.DrawTexture(rect, Texture2D.whiteTexture);
		GUI.color = Color.white;
		GUI.DrawTexture(rect, rings ? gridTexture : plainGridTexture);
		GUI.color = Color.white;
		if (ActiveCoverageRadius > 0f)
		{
			GUI.DrawTexture(rect, terrainField.Render(now, gain, clutter, persistence));
		}
		Vector2 center = new Vector2(rect.x + num2 * 0.5f, rect.y + num2 * 0.5f);
		float num3 = num2 * 0.45f;
		if (ActiveCoverageRadius > 0f && range > ActiveCoverageRadius)
		{
			RadarScenarioMapDraw.Circle(center, num3 * ActiveCoverageRadius / range, new Color(0.2f, 0.52f, 0.3f, 0.75f), 1.5f);
		}
		DrawTrack(center, num3);
		for (int i = 0; i < terrainEchoes.Count; i++)
		{
			if (terrainEchoes[i].ContactNumber > 0)
			{
				DrawEcho(terrainEchoes[i], center, num3);
			}
		}
		foreach (Echo echo in aircraftEchoes.Values)
		{
			DrawEcho(echo, center, num3);
		}
		DrawLaunchAnimations(data, center, num3);
		for (int j = 7; j >= 0; j--)
		{
			float angle = (SweepDegrees - (float)j * 1.8f) * 0.017453292f;
			float num4;
			float num5;
			if (j == 0)
			{
				num4 = 0.72f;
				num5 = 2f;
			}
			else
			{
				num4 = 0.12f - (float)j * 0.012f;
				num5 = 5f;
			}
			Line(center, new Vector2(center.x + Mathf.Sin(angle) * num3, center.y - Mathf.Cos(angle) * num3), new Color(0.16f, 1f, 0.49f, num4), num5);
		}
		for (int k = 0; k < 360; k += 30)
		{
			float angle2 = (float)k * 0.017453292f;
			GUI.Label(new Rect(center.x + Mathf.Sin(angle2) * (num3 + 19f) - 24f, center.y - Mathf.Cos(angle2) * (num3 + 19f) - 12f, 48f, 24f), k.ToString("000"), smallStyle);
		}
		GUI.Label(new Rect(rect.x + 18f, rect.y + 14f, 300f, 26f), "PPI / " + T("radar.units"), labelStyle);
		GUI.Label(new Rect(rect.x + 18f, rect.y + 42f, num2 - 36f, 26f), T("radar.range") + " " + range.ToString("0") + "  |  " + T("radar.signal") + " " + SweepDegrees.ToString("000") + "°", smallStyle);
		if (GUI.enabled && Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			Vector2 vector = Event.current.mousePosition - center;
			if (num3 * num3 >= vector.x * vector.x + vector.y * vector.y)
			{
				float num6 = range / num3;
				MarkEchoAt(new Vector2((Event.current.mousePosition.x - center.x) * num6 + stationPosition.x, -((Event.current.mousePosition.y - center.y) * num6) + stationPosition.y), range * 15f / num3);
				Event.current.Use();
			}
		}
		DrawControls(new Rect(rect.x + num2 + 24f, area.y + 18f, area.xMax - rect.xMax - 42f, area.height - 36f), data);
		GUI.color = color;
	}

	private void DrawEcho(Echo echo, Vector2 center, float radius)
	{
		float num = DisplayStrength(echo);
		if (num < 0.025f)
		{
			return;
		}
		if (!OnScope(echo.Position))
		{
			return;
		}
		Vector2 vector = ScopePoint(echo.Position, center, radius);
		float num2;
		float num3;
		if (echo.Terrain)
		{
			num2 = num * 7f + 6f;
			num3 = 0.68f;
		}
		else
		{
			num2 = num * 8f + 7f;
			num3 = 1f;
		}
		GUI.color = new Color(0.25f, 1f, 0.345f, num3 * num);
		if (!echo.Terrain)
		{
			GUI.DrawTexture(new Rect(vector.x - num2, vector.y - num2, num2 * 2f, num2 * 2f), dotTexture);
		}
		if (echo.ContactNumber != 0)
		{
			Color color = ((echo == selected) ? new Color(0.85f, 1f, 0.45f, 1f) : green);
			Line(new Vector2(vector.x - 12f, vector.y - 12f), new Vector2(vector.x - 12f, vector.y + 12f), color, 1f);
			Line(new Vector2(vector.x + 12f, vector.y - 12f), new Vector2(vector.x + 12f, vector.y + 12f), color, 1f);
			GUI.color = Color.white;
			GUI.Label(new Rect(vector.x + 14f, vector.y - 15f, 70f, 26f), "M" + echo.ContactNumber.ToString("00"), smallStyle);
		}
		GUI.color = Color.white;
	}

	private void DrawControls(Rect panel, RadarScenarioData data)
	{
		float num = 0f;
		GUI.Label(new Rect(panel.x, panel.y, panel.width, 40f), T("radar.operator"), titleStyle);
		int num2 = 0;
		for (int i = 0; i < data.Stations.Count; i++)
		{
			RadarScenarioStation station = data.Stations[i];
			if (RadarScenarioGroundCatalog.IsRadar(station))
			{
				int num3 = Mathf.Clamp(station.Polarization, 0, RadarCatalog.Presets.Count - 1);
				RadarPreset preset = RadarCatalog.Presets[num3];
				GUI.color = ((i == stationIndex) ? new Color(0.54f, 0.86f, 0.61f, 1f) : Color.white);
				if (GUI.Button(new Rect(panel.x + (float)(num2 % 2) * ((panel.width - 10f) * 0.5f + 10f), panel.y + 46f + (float)(num2 / 2) * 48f, (panel.width - 10f) * 0.5f, 45f), T("radar.station") + " " + station.Id.ToString() + " / " + preset.BandLetter, buttonStyle))
				{
					SelectedStationIndex = i;
				}
				num2++;
			}
		}
		GUI.color = Color.white;
		float num4 = panel.y + 46f + (float)((num2 + 1) / 2) * 48f;
		int num5 = FindRadarIndex(data, stationIndex);
		if (num5 >= 0)
		{
			RadarScenarioStation station2 = data.Stations[num5];
			int num6 = station2.Polarization;
			RadarPreset preset2 = RadarCatalog.Presets[num6];
			num = RadarScenarioGroundCatalog.CoverageRadius(num6);
			GUI.Label(new Rect(panel.x, num4, panel.width, 34f), Localization.T(preset2.NameKey) + "  |  " + T("map.coverage") + " " + num.ToString("0"), dimStyle);
			num4 += 38f;
		}
		num4 += 8f;
		float num7 = Mathf.Min(panel.width * 0.49f, 440f);
		float num8 = panel.x + num7 + 28f;
		float num9 = panel.xMax - num8;
		float num10 = num4;
		Gain = Slider(new Rect(panel.x, num4, num7, 68f), T("radar.gain"), gain, 0.4f, 3.5f, "0.0");
		num4 += 76f;
		Range = Slider(new Rect(panel.x, num4, num7, 68f), T("radar.range"), range, 20f, 180f, "0");
		num4 += 76f;
		Clutter = Slider(new Rect(panel.x, num4, num7, 68f), T("radar.clutter"), clutter, 0f, 1f, "0.00");
		num4 += 76f;
		Persistence = Slider(new Rect(panel.x, num4, num7, 68f), T("radar.persistence"), persistence, 0f, 16f, "0.0");
		num4 += 80f;
		float num11 = Slider(new Rect(panel.x, num4, num7, 68f), T("terrain.heightFilter"), TerrainMinimumHeight * 100f, 0f, 100f, "0");
		num4 += 76f;
		if (Mathf.Abs(num11 * 0.01f - TerrainMinimumHeight) > 0.0001f)
		{
			TerrainMinimumHeight = num11 * 0.01f;
		}
		if (GUI.Button(new Rect(panel.x, num4, num7, 45f), T("radar.rings") + ": " + (rings ? T("radar.on") : T("radar.off")), buttonStyle))
		{
			rings = !rings;
		}
		num4 += 51f;
		if (GUI.Button(new Rect(panel.x, num4, num7, 45f), T("radar.clear"), buttonStyle))
		{
			ClearMarks();
		}
		num4 += 60f;
		GUI.Label(new Rect(panel.x, num4, num7, Mathf.Max(80f, panel.yMax - num4)), T("terrain.heightHint"), dimStyle);
		GUI.Label(new Rect(num8, num10, num9, 30f), T("radar.marked"), titleStyle);
		float num12 = num10 + 34f;
		if (markedEchoes.Count == 0)
		{
			GUI.Label(new Rect(num8, num12, num9, 64f), T("radar.none"), dimStyle);
		}
		markScroll = GUI.BeginScrollView(new Rect(num8, num12, num9, 80f), markScroll, new Rect(0f, 0f, num9 - 22f, Mathf.Max(75f, (float)markedEchoes.Count * 38f)));
		for (int j = 0; j < markedEchoes.Count; j++)
		{
			Echo echo = markedEchoes[j];
			string text = "M" + echo.ContactNumber.ToString("00") + "   " + (now - echo.SampleTime).ToString("0.0") + " s";
			GUI.color = ((echo == selected) ? new Color(0.73f, 0.89f, 0.46f, 1f) : Color.white);
			if (GUI.Button(new Rect(0f, (float)j * 38f, num9 - 22f, 34f), text, buttonStyle))
			{
				selected = echo;
			}
		}
		GUI.EndScrollView();
		GUI.color = Color.white;
		float num13 = num10 + 116f;
		if (selected != null && IsAlive(selected))
		{
			float num14 = selected.Position.x - stationPosition.x;
			float num15 = selected.Position.y - stationPosition.y;
			GUI.Label(new Rect(num8, num13, num9, 27f), T("radar.contact") + " M" + selected.ContactNumber.ToString("00"), titleStyle);
			num13 += 29f;
			GUI.Label(new Rect(num8, num13, num9, 50f), T("radar.bearing") + "  " + Bearing(new Vector2(num14, num15)).ToString("000") + "°  |  " + T("radar.distance") + "  " + Mathf.Sqrt(num14 * num14 + num15 * num15).ToString("0.0") + "\n" + T("radar.age") + "  " + (now - selected.SampleTime).ToString("0.0") + " s  |  " + T("radar.gameStrength") + " " + selected.Strength.ToString("0.00"), dimStyle);
		}
		bool flag = GUI.enabled;
		GUI.enabled = flag && selected != null && !selected.Terrain && DisplayStrength(selected) >= 0.08f;
		if (GUI.Button(new Rect(num8, num10 + 199f, num9 * 0.65f - 5f, 40f), T("track.acquire"), buttonStyle))
		{
			TryLockSelected();
		}
		GUI.enabled = flag && trackedKey != null;
		if (GUI.Button(new Rect(num8 + num9 * 0.65f, num10 + 199f, num9 * 0.35f, 40f), T("track.release"), buttonStyle))
		{
			ClearTrack();
		}
		GUI.enabled = flag;
		GUI.Label(new Rect(num8, num10 + 243f, num9, 38f), T(TrackStatusKey) + ((trackedContact > 0) ? (" M" + trackedContact.ToString("00")) : ""), dimStyle);
		DrawLauncherControls(new Rect(num8, num10 + 290f, num9, panel.yMax - num10 - 290f), data);
	}

	private float Slider(Rect rect, string caption, float value, float min, float max, string format)
	{
		GUI.Label(new Rect(rect.x, rect.y, rect.width, 30f), caption + "   " + value.ToString(format), labelStyle);
		return GUI.HorizontalSlider(new Rect(rect.x + 2f, rect.y + 39f, rect.width - 4f, 25f), value, min, max);
	}

	private static string T(string key)
	{
		return RadarScenarioText.Get(key);
	}

	private void EnsureStyles()
	{
		if (titleStyle == null)
		{
			titleStyle = new GUIStyle(GUI.skin.label);
			titleStyle.fontSize = 28;
			titleStyle.fontStyle = FontStyle.Bold;
			titleStyle.wordWrap = true;
			titleStyle.normal.textColor = green;
		}
		if (labelStyle == null)
		{
			labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.fontSize = 21;
			labelStyle.wordWrap = true;
			labelStyle.normal.textColor = new Color(0.67f, 0.91f, 0.71f, 1f);
		}
		if (dimStyle == null)
		{
			dimStyle = new GUIStyle(GUI.skin.label);
			dimStyle.fontSize = 19;
			dimStyle.normal.textColor = new Color(0.47f, 0.65f, 0.52f, 1f);
		}
		if (buttonStyle == null)
		{
			buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.fontSize = 17;
		}
		if (smallStyle == null)
		{
			smallStyle = new GUIStyle(GUI.skin.label);
			smallStyle.fontSize = 15;
			smallStyle.normal.textColor = new Color(0.47f, 0.65f, 0.52f, 1f);
		}
	}

	private void EnsureTextures()
	{
		if (gridTexture != null)
		{
			return;
		}
		gridTexture = MakeGrid(withRings: true);
		plainGridTexture = MakeGrid(false);
		dotTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false, false);
		dotTexture.name = "RadarScenarioEcho";
		dotTexture.hideFlags = (HideFlags)61;
		Color[] array = new Color[1024];
		for (int i = 0; i < 32; i++)
		{
			for (int j = 0; j < 32; j++)
			{
				float num = Vector2.Distance(new Vector2((float)j + 0.5f, (float)i + 0.5f), new Vector2(16f, 16f));
				float num2 = 1f - num * 0.0625f;
				if (num2 < 0f)
				{
					num2 = 0f;
				}
				else if (num2 > 1f)
				{
					num2 = 1f;
				}
				array[i * 32 + j] = new Color(1f, 1f, 1f, Mathf.Pow(num2, 2.5f));
			}
		}
		dotTexture.SetPixels(array);
		dotTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
	}

	private static Texture2D MakeGrid(bool withRings)
	{
		Texture2D texture = new Texture2D(768, 768, TextureFormat.RGBA32, false, false);
		texture.name = (withRings ? "RadarScenarioGrid" : "RadarScenarioRim");
		texture.hideFlags = (HideFlags)61;
		Color32[] array = new Color32[589824];
		for (int i = 0; i < 768; i++)
		{
			float y = (float)i + 0.5f - 384f;
			for (int j = 0; j < 768; j++)
			{
				float x = (float)j + 0.5f - 384f;
				float dist = Mathf.Sqrt(x * x + y * y);
				Color32 color = ((i % 3 == 0) ? new Color32(1, 7, 3, byte.MaxValue) : new Color32(1, 9, 4, byte.MaxValue));
				bool flag = 0.8f > Mathf.Abs(dist - 345.6f);
				if (withRings && (0.6f > Mathf.Abs(dist - 86.4f) || 0.6f > Mathf.Abs(dist - 172.8f) || 0.6f > Mathf.Abs(dist - 259.2f)))
				{
					flag = true;
				}
				if (flag)
				{
					color = new Color32(28, 86, 39, byte.MaxValue);
				}
				if (dist < 345.6f && (0.5f > Mathf.Abs(x) || 0.5f > Mathf.Abs(y)))
				{
					color = new Color32(12, 44, 20, byte.MaxValue);
				}
				if (331.776f < dist && dist < 345.6f)
				{
					float angle = Mathf.Atan2(x, y) * 57.29578f;
					if (0.16f > Mathf.Abs(Mathf.DeltaAngle(Mathf.Floor(angle / 5f) * 5f, angle)))
					{
						color = new Color32(39, 108, 51, byte.MaxValue);
					}
				}
				array[i * 768 + j] = color;
			}
		}
		texture.SetPixels32(array);
		texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		return texture;
	}

	private static void Fill(Rect rect, Color color)
	{
		GUI.color = color;
		GUI.DrawTexture(rect, Texture2D.whiteTexture);
		GUI.color = Color.white;
	}

	private static void Line(Vector2 a, Vector2 b, Color color, float thickness)
	{
		RadarScenarioGuiGeometry.Line(a, b, color, thickness);
	}

	public static int FindRadarIndex(RadarScenarioData data, int preferred)
	{
		if (data != null && data.Stations != null)
		{
			if (preferred >= 0 && preferred < data.Stations.Count && RadarScenarioGroundCatalog.IsRadar(data.Stations[preferred]))
			{
				return preferred;
			}
			for (int i = 0; i < data.Stations.Count; i++)
			{
				if (RadarScenarioGroundCatalog.IsRadar(data.Stations[i]))
				{
					return i;
				}
			}
		}
		return -1;
	}

	public void Dispose()
	{
		DestroyTexture(gridTexture);
		DestroyTexture(plainGridTexture);
		DestroyTexture(dotTexture);
		plainGridTexture = null;
		gridTexture = null;
		dotTexture = null;
		terrainField.Dispose();
		ClearEchoes();
	}

	private static void DestroyTexture(Texture2D texture)
	{
		if (texture != null && !Application.isEditor)
		{
			UnityEngine.Object.Destroy(texture);
		}
	}
}
