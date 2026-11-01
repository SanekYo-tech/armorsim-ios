using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class RadarMapEditorPanel
{
	private sealed class Preview
	{
		public long Signature;

		public List<Vector2> Points;

		public List<int> TightTurns;

		public long FloorLifts;

		public double MaxLift;

		public Preview()
		{
			Points = new List<Vector2>();
			TightTurns = new List<int>();
		}
	}

	public static readonly double[] RingTargetHeights = new double[2] { 1000.0, 10000.0 };

	private const double PreviewStep = 2.0;

	private const double PreviewLimit = 3600.0;

	private RadarMapWorld world;

	private readonly Dictionary<RadarScenarioAircraft, Preview> previews;

	private readonly Dictionary<string, string> coatingBuffers;

	private string coatingReason;

	public static bool LastSwitchDrawn { get; private set; }

	public static int SwitchDraws { get; private set; }

	public RadarMapEditorPanel()
	{
		previews = new Dictionary<RadarScenarioAircraft, Preview>();
		coatingBuffers = new Dictionary<string, string>();
	}

	private static string L(string key)
	{
		return RadarScenarioText.Get(key);
	}

	private static string F(double v, string format)
	{
		return v.ToString(format, CultureInfo.InvariantCulture);
	}

	private static string Format(string key, params object[] args)
	{
		return string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get(key), args);
	}

	public static void ScenarioSwitch(RadarScenarioData data, GUIStyle heading, GUIStyle small, GUIStyle button)
	{
		SwitchDraws++;
		if (!LastSwitchDrawn)
		{
			return;
		}
		GUILayout.Label(L("map.phys.switch") + "", heading);
		GUILayout.BeginHorizontal();
		bool flag = data.DetectionModel == RadarDetectionModel.Physical;
		Color backgroundColor = GUI.backgroundColor;
		if (flag)
		{
			GUI.backgroundColor = new Color(0.44f, 0.7f, 0.93f, 1f);
		}
		if (GUILayout.Button(L("map.phys.arcadeShort"), button))
		{
			data.DetectionModel = RadarDetectionModel.Arcade;
		}
		GUI.backgroundColor = (flag ? new Color(0.44f, 0.7f, 0.93f, 1f) : backgroundColor);
		if (GUILayout.Button(L("map.phys.physicalShort"), button))
		{
			data.DetectionModel = RadarDetectionModel.Physical;
		}
		GUI.backgroundColor = backgroundColor;
		GUILayout.EndHorizontal();
		GUILayout.Label(L(flag ? "map.model.physical" : "map.model.arcade"), small);
		GUILayout.Space(8f);
	}

	public static void WorldFields(RadarScenarioData data, GUIStyle label, GUIStyle small, GUIStyle button, GUIStyle toggle)
	{
		data.CoastMode = GUILayout.Toggle(data.CoastMode, L("map.prop.coast"), toggle);
		GUILayout.Label(L("map.prop.seaState") + ": " + data.SeaState, label);
		GUILayout.BeginHorizontal();
		for (int i = 0; i <= 6; i++)
		{
			Color backgroundColor = GUI.backgroundColor;
			if (i == data.SeaState)
			{
				GUI.backgroundColor = new Color(0.44f, 0.7f, 0.93f, 1f);
			}
			if (GUILayout.Button(i.ToString(CultureInfo.InvariantCulture), button, GUILayout.Width(34f)))
			{
				data.SeaState = i;
			}
			GUI.backgroundColor = backgroundColor;
		}
		GUILayout.EndHorizontal();
		GUILayout.Label(Format("map.prop.seaNote", RadarMapPropagation.SeaStateWaveHeight(data.SeaState).ToString("0.###", CultureInfo.InvariantCulture)), small);
		GUILayout.Space(8f);
	}

	public void AircraftFields(RadarScenarioAircraft aircraft, GUIStyle label, GUIStyle small)
	{
		GUILayout.Label(L("map.phys.speed") + ": " + F(aircraft.SpeedMps, "F0"), label);
		aircraft.SpeedMps = Mathf.Round(GUILayout.HorizontalSlider(aircraft.SpeedMps, 60f, 1000f) / 10f) * 10f;
		GUILayout.Label(L("map.phys.bank") + ": " + F(aircraft.MaxBankDeg, "F0"), label);
		aircraft.MaxBankDeg = Mathf.Round(GUILayout.HorizontalSlider(aircraft.MaxBankDeg, 10f, 80f));
		GUILayout.Label(L("map.phys.rollRate") + ": " + F(aircraft.RollRateDegPerSec, "F0"), label);
		aircraft.RollRateDegPerSec = Mathf.Round(GUILayout.HorizontalSlider(aircraft.RollRateDegPerSec, 30f, 360f) / 5f) * 5f;
		double num = (double)aircraft.MaxBankDeg * 0.017453292519943295;
		double num2 = (double)aircraft.SpeedMps * aircraft.SpeedMps / (Math.Tan(num) * 9.80665) / 1000.0;
		GUILayout.Label(Format("map.phys.turn", F(num2, "F2"), F(1.0 / Math.Cos(num), "F2")), small);
		CoatingFields(aircraft, label, small);
		if (previews.TryGetValue(aircraft, out Preview value))
		{
			foreach (int item in value.TightTurns)
			{
				GUILayout.Label(Format("map.phys.warnTurn", item + 1), small);
			}
			if (value.FloorLifts > 0)
			{
				GUILayout.Label(Format("map.prop.throughTerrain", F((double)value.FloorLifts * 0.05, "F0") + " s, +" + F(value.MaxLift, "F0") + " " + L("map.phys.m")), small);
			}
		}
	}

	private void CoatingFields(RadarScenarioAircraft aircraft, GUIStyle label, GUIStyle small)
	{
		GUILayout.Space(6f);
		GUILayout.Label(L("map.coat.title"), label);
		int num = Math.Max(Array.IndexOf(RadarScenarioData.PhysicalCoatingIds, aircraft.PhysicalCoatingId), 0);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("<", GUILayout.Width(39f), GUILayout.Height(33f)))
		{
			aircraft.PhysicalCoatingId = RadarScenarioData.PhysicalCoatingIds[(RadarScenarioData.PhysicalCoatingIds.Length - 1 + num) % RadarScenarioData.PhysicalCoatingIds.Length];
			EnsureCustom(aircraft);
		}
		string text;
		RadarMapCoatings.FromAircraft(aircraft, out text);
		GUILayout.Label(CoatingTitle(aircraft), small, GUILayout.MinHeight(33f));
		if (GUILayout.Button(">", GUILayout.Width(39f), GUILayout.Height(33f)))
		{
			aircraft.PhysicalCoatingId = RadarScenarioData.PhysicalCoatingIds[(num + 1) % RadarScenarioData.PhysicalCoatingIds.Length];
			EnsureCustom(aircraft);
		}
		GUILayout.EndHorizontal();
		if (aircraft.PhysicalCoatingId == "salisbury" || aircraft.PhysicalCoatingId == "jaumann")
		{
			TypedNumber(aircraft, "f0", L("map.coat.f0"), (double)aircraft.CoatingF0Hz / 1000000000.0, delegate (double value)
			{
				if (!RadarMapCoatings.F0InRange(value * 1000000000.0))
				{
					return "f0 [0.1, 40] GHz";
				}
				aircraft.CoatingF0Hz = (float)(value * 1000000000.0);
				return null;
			}, small);
		}
		if (aircraft.PhysicalCoatingId == "custom")
		{
			string[] array = new string[5] { "ε′", "ε″", "μ′", "μ″", L("map.coat.thickness") };
			for (int i = 0; i < 5; i++)
			{
				float[] customLayer = aircraft.CustomLayer;
				double current = ((i == 4) ? ((double)(customLayer[4] * 1000f)) : customLayer[i]);
				TypedNumber(aircraft, "c" + i, array[i], current, delegate (double value)
				{
					float[] array2 = (float[])aircraft.CustomLayer.Clone();
					if (i == 4)
					{
						value /= 1000.0;
					}
					array2[i] = (float)value;
					string reason;
					if (RadarMapCoatings.CustomInRange(array2, out reason))
					{
						aircraft.CustomLayer = array2;
					}
					return reason;
				}, small);
			}
		}
		GUILayout.Label(L("map.coat.assumed"), small);
		if (!string.IsNullOrEmpty(coatingReason))
		{
			GUILayout.Label(string.Format(CultureInfo.InvariantCulture, L("map.coat.range"), coatingReason), small);
		}
		if (text != null)
		{
			GUILayout.Label(string.Format(CultureInfo.InvariantCulture, L("map.coat.range"), text), small);
		}
		if (aircraft.PhysicalCoatingId != "metal")
		{
			GUILayout.Label(L("map.coat.note"), small);
		}
	}

	private static string CoatingTitle(RadarScenarioAircraft aircraft)
	{
		if (aircraft.PhysicalCoatingId == "salisbury")
		{
			return string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.coat.salisbury"), F(aircraft.CoatingF0Hz / 1000000000.0, "0.###"));
		}
		if (aircraft.PhysicalCoatingId == "jaumann")
		{
			return string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.coat.jaumann"), F(aircraft.CoatingF0Hz / 1000000000.0, "0.###"));
		}
		if (aircraft.PhysicalCoatingId == "custom")
		{
			return L("map.coat.custom");
		}
		return L("map.coat.metal");
	}

	private static void EnsureCustom(RadarScenarioAircraft aircraft)
	{
		if (aircraft.PhysicalCoatingId != "custom")
		{
			return;
		}
		if (aircraft.CustomLayer == null || aircraft.CustomLayer.Length != 5)
		{
			aircraft.CustomLayer = (float[])RadarMapCoatings.DefaultCustom.Clone();
		}
	}

	private void TypedNumber(RadarScenarioAircraft aircraft, string field, string caption, double current, Func<double, string> apply, GUIStyle small)
	{
		string text = aircraft.Id + ":" + caption;
		string text2 = "RadarMapCoat_" + text;
		string value;
		if (!coatingBuffers.TryGetValue(text2, out value) || GUI.GetNameOfFocusedControl() != text2)
		{
			value = current.ToString("0.####", CultureInfo.InvariantCulture);
		}
		bool flag = false;
		if (GUI.GetNameOfFocusedControl() == text2)
		{
			flag = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
		}
		GUILayout.BeginHorizontal();
		GUILayout.Label(caption, small, GUILayout.Width(150f));
		GUI.SetNextControlName(text2);
		value = GUILayout.TextField(value, 16);
		GUILayout.EndHorizontal();
		coatingBuffers[text2] = value;
		if (flag)
		{
			double result;
			if (double.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out result) && !double.IsNaN(result) && !double.IsInfinity(result))
			{
				coatingReason = apply(result);
			}
			else
			{
				coatingReason = value;
			}
			Event.current.Use();
			GUI.FocusControl(null);
		}
	}

	public static void StationFields(RadarScenarioStation station, GUIStyle label, GUIStyle small, GUIStyle button, GUIStyle toggle)
	{
		StationFields(station, null, label, small, button, toggle);
	}

	public static void StationFields(RadarScenarioStation station, RadarMapWorld world, GUIStyle label, GUIStyle small, GUIStyle button, GUIStyle toggle)
	{
		string text = RadarMapStations.IdFor(station);
		int num = -1;
		for (int i = 0; i < RadarMapStations.All.Count; i++)
		{
			if (RadarMapStations.All[i].Id == text)
			{
				num = i;
			}
		}
		GUILayout.Label(L("map.phys.station"), small);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("<", button, GUILayout.Width(39f)))
		{
			int num2 = Math.Max(num, 0);
			station.PhysicalStationId = RadarMapStations.All[(RadarMapStations.All.Count - 1 + num2) % RadarMapStations.All.Count].Id;
		}
		string text2;
		if (num < 0)
		{
			text2 = Format("map.phys.refuse.hf");
		}
		else
		{
			RadarMapStationSpec radarMapStationSpec = RadarMapStations.All[num];
			string text3;
			if (RadarCatalog.TryGet(radarMapStationSpec.Id, out RadarPreset preset))
			{
				text3 = Localization.T(preset.NameKey);
			}
			else
			{
				text3 = L("map.phys.st." + radarMapStationSpec.Id);
			}
			text2 = radarMapStationSpec.Id + " · " + text3;
		}
		GUILayout.Label(text2, small, GUILayout.MinHeight(43f));
		if (GUILayout.Button(">", button, GUILayout.Width(39f)))
		{
			station.PhysicalStationId = RadarMapStations.All[(num + 1) % RadarMapStations.All.Count].Id;
		}
		GUILayout.EndHorizontal();
		if (num < 0)
		{
			return;
		}
		RadarMapStationSpec radarMapStationSpec2 = RadarMapStations.All[num];
		object[] array = new object[2];
		array[0] = RadarBands.FormatFrequency(radarMapStationSpec2.FrequencyHz, Localization.T("radar.freq.mhz"), Localization.T("radar.freq.ghz"));
		array[1] = F(radarMapStationSpec2.WavelengthM * 100.0, "F1");
		GUILayout.Label(Format("map.phys.freq", array), small);
		bool flag = station.MastHeightM < 0f;
		bool flag2 = GUILayout.Toggle(flag, Format("map.phys.mastDefault", F(radarMapStationSpec2.MastM, "F0")), toggle);
		if (flag2 != flag)
		{
			station.MastHeightM = (flag2 ? (-1f) : ((float)radarMapStationSpec2.MastM));
		}
		if (!flag2)
		{
			GUILayout.Label(L("map.phys.mast") + ": " + F(station.MastHeightM, "F0"), label);
			station.MastHeightM = Mathf.Round(GUILayout.HorizontalSlider(station.MastHeightM, 3f, 60f));
		}
		if (radarMapStationSpec2.Scan == RadarMapScan.Sector)
		{
			GUILayout.Label(L("map.phys.sector") + ": " + F(station.SectorAzimuthDeg, "F0") + "° · " + F(radarMapStationSpec2.SectorDeg, "F0") + "°", label);
			station.SectorAzimuthDeg = Mathf.Round(GUILayout.HorizontalSlider(station.SectorAzimuthDeg, 0f, 359f));
		}
		double num3 = RadarMapStations.MastHeight(station, radarMapStationSpec2);
		if (world != null)
		{
			double num4 = RadarMapUnits.MapToMeters(station.Position.x);
			double num5 = RadarMapUnits.MapToMeters(station.Position.y);
			double num6 = world.Height(num4, num5);
			num3 += num6;
			GUILayout.Label(Format("map.prop.site", F(num6, "F0"), F(num3, "F0")), small);
		}
		GUILayout.BeginHorizontal();
		GUILayout.Label(L("map.prop.pol"), small, GUILayout.Width(150f));
		for (int j = 0; j < 2; j++)
		{
			Color backgroundColor = GUI.backgroundColor;
			if (station.Polarization == j)
			{
				GUI.backgroundColor = new Color(0.44f, 0.7f, 0.93f, 1f);
			}
			if (GUILayout.Button(L((j != 0) ? "map.prop.polV" : "map.prop.polH"), button, GUILayout.Width(60f)))
			{
				station.Polarization = j;
			}
			GUI.backgroundColor = backgroundColor;
		}
		GUILayout.EndHorizontal();
		GUILayout.Label(Format("map.phys.horizonRows", F(RadarMapGeo.Horizon(num3, RingTargetHeights[0]) / 1000.0, "F1"), F(RadarMapGeo.Horizon(num3, RingTargetHeights[1]) / 1000.0, "F1")), small);
	}

	public static string StationName(RadarMapStationSpec spec)
	{
		if (RadarCatalog.TryGet(spec.Id, out RadarPreset preset))
		{
			return spec.Id + " · " + Localization.T(preset.NameKey);
		}
		return spec.Id + " · " + L("map.phys.st." + spec.Id);
	}

	public void DrawMap(Rect map, RadarScenarioData data, Func<Vector2, Vector2> toScreen, float pixelsPerUnit, GUIStyle small, int selectedAircraft)
	{
		if (world == null || world.Seed != data.Seed || world.Coast != data.CoastMode || world.SeaState != data.SeaState)
		{
			world = RadarMapWorld.Of(data);
		}
		for (int i = 0; i < data.Stations.Count; i++)
		{
			RadarScenarioStation station = data.Stations[i];
			if (station.IsDefencePost)
			{
				continue;
			}
			if (!RadarMapStations.TryResolve(station, out RadarMapStationSpec spec, out string refusalKey))
			{
				continue;
			}
			double num = RadarMapStations.MastHeight(station, spec) + world.Height(RadarMapUnits.MapToMeters(station.Position.x), RadarMapUnits.MapToMeters(station.Position.y));
			Vector2 vector = toScreen(station.Position);
			for (int j = 0; j < RingTargetHeights.Length; j++)
			{
				float num2 = (float)(RadarMapGeo.Horizon(num, RingTargetHeights[j]) / 1000.0) * pixelsPerUnit;
				DashedCircle(vector, num2, new Color(0.02f, 0.35f, 0.48f, (j == 0) ? 0.95f : 0.7f), 1.8f);
				RadarScenarioMapDraw.Label(new Rect(vector.x + num2 * 0.7071f + 4f, vector.y - num2 * 0.7071f - 22f, 150f, 22f), Format("map.phys.ring", F(RingTargetHeights[j] / 1000.0, "F0")), small);
			}
		}
		PrunePreviews(data);
		for (int k = 0; k < data.Aircraft.Count; k++)
		{
			Preview preview = PreviewOf(data.Aircraft[k]);
			if (preview == null || preview.Points == null || preview.Points.Count < 2)
			{
				continue;
			}
			Color color = new Color(0.02f, 0.02f, 0.02f, (k == selectedAircraft) ? 0.85f : 0.45f);
			for (int l = 1; l < preview.Points.Count; l++)
			{
				RadarScenarioMapDraw.Line(toScreen(preview.Points[l - 1]), toScreen(preview.Points[l]), color, (k == selectedAircraft) ? 2f : 1.4f);
			}
			foreach (int item in preview.TightTurns)
			{
				RadarScenarioWaypoint waypoint = data.Aircraft[k].Route[item];
				RadarScenarioMapDraw.Circle(toScreen(waypoint.Position), 15f, new Color(0.8f, 0.1f, 0.05f, 1f), 2.5f);
			}
		}
		ScaleBar(new Rect(map.x + 14f, map.y + map.height - 46f, map.width * 0.4f, 36f), pixelsPerUnit, small);
		string text = L("map.phys.flown");
		GUIContent gUIContent = new GUIContent(text);
		Vector2 vector2 = small.CalcSize(gUIContent);
		float num3 = vector2.x + 8f;
		if (num3 > map.width * 0.55f)
		{
			num3 = map.width * 0.55f;
		}
		float num4 = map.x + map.width - num3 - 10f;
		float num5 = map.y + map.height - 34f;
		RadarScenarioMapDraw.Fill(new Rect(num4, num5, num3, 24f), new Color(0.95f, 0.96f, 0.9f, 0.82f));
		RadarScenarioMapDraw.Label(new Rect(num4 + 4f, num5, num3 - 4f, 24f), text, small);
	}

	private static void DashedCircle(Vector2 centre, float radius, Color color, float width)
	{
		for (int i = 0; i < 120; i += 2)
		{
			float num = (float)i * Mathf.PI * 2f / 120f;
			float num2 = (float)(i + 1) * Mathf.PI * 2f / 120f;
			RadarScenarioMapDraw.Line(new Vector2(Mathf.Cos(num) * radius + centre.x, Mathf.Sin(num) * radius + centre.y), new Vector2(Mathf.Cos(num2) * radius + centre.x, Mathf.Sin(num2) * radius + centre.y), color, width);
		}
	}

	private static void ScaleBar(Rect area, float pixelsPerUnit, GUIStyle small)
	{
		if (pixelsPerUnit <= 0f)
		{
			return;
		}
		float num = area.width / pixelsPerUnit;
		float[] array = new float[7] { 500f, 200f, 100f, 50f, 20f, 10f, 5f };
		float num2 = 5f;
		for (int i = 0; i < array.Length; i++)
		{
			if (num >= array[i])
			{
				num2 = array[i];
				break;
			}
		}
		float num3 = num2 * pixelsPerUnit;
		float num4 = area.y + area.height - 6f;
		RadarScenarioMapDraw.Fill(new Rect(area.x - 2f, num4 - 16f, num3 + 70f, 26f), new Color(0.95f, 0.96f, 0.9f, 0.82f));
		RadarScenarioMapDraw.Line(new Vector2(area.x, num4), new Vector2(area.x + num3, num4), Color.black, 3f);
		RadarScenarioMapDraw.Line(new Vector2(area.x, num4), new Vector2(area.x, num4 - 10f), Color.black, 2f);
		RadarScenarioMapDraw.Line(new Vector2(area.x + num3, num4), new Vector2(area.x + num3, num4 - 10f), Color.black, 2f);
		RadarScenarioMapDraw.Label(new Rect(area.x + num3 + 6f, num4 - 17f, 70f, 22f), F(num2, "F0") + " " + L("map.phys.km"), small);
	}

	private void PrunePreviews(RadarScenarioData data)
	{
		if (previews.Count <= data.Aircraft.Count)
		{
			return;
		}
		List<RadarScenarioAircraft> list = new List<RadarScenarioAircraft>();
		foreach (RadarScenarioAircraft item in previews.Keys)
		{
			if (!data.Aircraft.Contains(item))
			{
				list.Add(item);
			}
		}
		foreach (RadarScenarioAircraft item in list)
		{
			previews.Remove(item);
		}
	}

	private Preview PreviewOf(RadarScenarioAircraft aircraft)
	{
		long num = Signature(aircraft);
		long num2 = 0L;
		if (world != null)
		{
			num2 = world.Seed * 31;
			if (world.Coast)
			{
				num2 += 7L;
			}
		}
		num ^= num2;
		if (previews.TryGetValue(aircraft, out Preview value) && value != null && value.Signature == num)
		{
			return value;
		}
		Preview preview = new Preview();
		preview.Signature = num;
		previews[aircraft] = preview;
		preview.TightTurns = RadarMapFlight.TightTurns(aircraft);
		RadarMapFlight radarMapFlight = ((world == null) ? new RadarMapFlight(aircraft) : new RadarMapFlight(aircraft, world.Height));
		double num3 = (aircraft.Loop ? 3600.0 : Math.Min(PreviewLimit, radarMapFlight.EndTime));
		for (double num4 = 0.0; num4 <= num3; num4 += 2.0)
		{
			RadarMapPose radarMapPose = radarMapFlight.Pose(num4);
			preview.Points.Add(new Vector2((float)(radarMapPose.Position.x / 1000.0), (float)(radarMapPose.Position.y / 1000.0)));
			if ((aircraft.Loop && radarMapFlight.Captures > radarMapFlight.WaypointCount) || radarMapPose.Finished)
			{
				break;
			}
		}
		preview.FloorLifts = radarMapFlight.FloorClamps;
		preview.MaxLift = radarMapFlight.MaxFloorLift;
		return preview;
	}

	private static long Signature(RadarScenarioAircraft aircraft)
	{
		long hash = unchecked((long)14695981039346656037uL);
		void Mix(float value)
		{
			hash = (hash ^ (long)(uint)BitConverter.SingleToInt32Bits(value)) * 1099511628211L;
		}
		Mix(aircraft.SpeedMps);
		Mix(aircraft.MaxBankDeg);
		Mix(aircraft.RollRateDegPerSec);
		Mix(aircraft.Loop ? 1f : 0f);
		foreach (RadarScenarioWaypoint waypoint in aircraft.Route)
		{
			Mix(waypoint.Position.x);
			Mix(waypoint.Position.y);
			Mix(waypoint.Altitude);
		}
		return hash;
	}
}
