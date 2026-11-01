using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class RadarMapScope
{
	public int LastLaunchOutcomeCode;

	private string manualHeightText;

	public float PersistenceSeconds;

	public float Brightness;

	public float DisplayRangeKm;

	public int StationIndex;

	public int SelectedPlotId;

	public int SelectedLauncherId;

	public string Message;

	public Action<int, bool> SetEmission;

	public Action<int, double> SetPfa;

	public Action<int, int> SetIntegration;

	public Action<int, double, double> SetSector;

	public Action<int, bool> SetLowAltitude;

	public Func<RadarMapScopeContact, int, bool> Launch;

	public const double AssociationKm = 2.0;

	private static readonly Color Green = new Color(0.23f, 0.95f, 0.37f, 1f);

	private GUIStyle caption;

	private GUIStyle text;

	private GUIStyle small;

	private GUIStyle button;

	private readonly RadarMapScopeContact contact;

	public RadarMapScopeContact Contact => contact;

	public RadarMapScope()
	{
		manualHeightText = "";
		PersistenceSeconds = 20f;
		Brightness = 1f;
		DisplayRangeKm = 200f;
		Message = "";
		contact = new RadarMapScopeContact();
	}

	private void DrawPanel(Rect panel, IReadOnlyList<RadarMapScopeStation> stations, IReadOnlyList<RadarMapScopeLauncher> launchers, double time)
	{
		if (stations == null || stations.Count == 0)
		{
			return;
		}
		StationIndex = Math.Clamp(StationIndex, 0, stations.Count - 1);
		RadarMapScopeStation s = stations[StationIndex];
		float num = panel.y + 36f;
		GUI.Label(new Rect(panel.x, num, panel.width, 28f), L("map.scope.station") + ":", text);
		num += 26f;
		if (GUI.Button(new Rect(panel.x, num, 36f, 32f), "<", button))
		{
			StationIndex = (StationIndex - 1 + stations.Count) % stations.Count;
		}
		GUI.Label(new Rect(panel.x + 44f, num + 4f, panel.width - 88f, 28f), s.Name, text);
		if (GUI.Button(new Rect(panel.x + panel.width - 36f, num, 36f, 32f), ">", button))
		{
			StationIndex = (StationIndex + 1) % stations.Count;
		}
		num += 40f;
		double num2 = RadarMapDetector.R0(s.Spec, 1.0, s.Scanner.Integration, 0.5, s.Pfa) / 1000.0;
		double num3 = RadarMapDetector.FalseAlarmsPerBeam(s.Spec, s.Pfa);
		double num4 = (double)RadarMapDetector.BeamPositions(s.Scanner.Spec, s.Scanner.SectorWidthDeg) * num3;
		float num5 = panel.width * 0.5f - 4f;
		if (GUI.Button(new Rect(panel.x, num, num5, 34f), L("map.scope.emission") + ": " + (s.Emitting ? L("map.scope.on") : L("map.scope.off")), button) && SetEmission != null)
		{
			SetEmission(StationIndex, !s.Emitting);
		}
		string text2 = "R0(1 " + L("map.phys.m") + "²) " + num2.ToString("F1", CultureInfo.InvariantCulture) + " " + L("map.phys.km");
		GUI.Label(new Rect(panel.x + panel.width * 0.5f + 4f, num + 6f, panel.width * 0.5f, 26f), text2, text);
		if (s.Spec.Is3D)
		{
			Color backgroundColor = GUI.backgroundColor;
			if (s.LowAltitude)
			{
				GUI.backgroundColor = new Color(0.44f, 0.8f, 0.5f, 1f);
			}
			if (GUI.Button(new Rect(panel.x, num, num5, 30f), L("map.prop.lowAlt") + ": " + (s.LowAltitude ? L("map.prop.on") : L("map.prop.off")), button) && SetLowAltitude != null)
			{
				SetLowAltitude(StationIndex, !s.LowAltitude);
			}
			GUI.backgroundColor = backgroundColor;
		}
		num += 34f;
		GUI.Label(new Rect(panel.x, num, panel.width, 24f), L("map.scope.threshold"), text);
		num += 24f;
		float num6 = (panel.width - 20f) / 6f;
		for (int i = 0; i < RadarMapDetector.PfaSteps.Length; i++)
		{
			Color backgroundColor2 = GUI.backgroundColor;
			if (RadarMapDetector.PfaSteps[i] * 1E-06 > Math.Abs(RadarMapDetector.PfaSteps[i] - s.Pfa))
			{
				GUI.backgroundColor = new Color(0.44f, 0.8f, 0.5f, 1f);
			}
			if (GUI.Button(new Rect(panel.x + (num6 + 4f) * (float)i, num, num6, 30f), "1e-" + (i + 3), button) && SetPfa != null)
			{
				SetPfa(StationIndex, RadarMapDetector.PfaSteps[i]);
			}
			GUI.backgroundColor = backgroundColor2;
		}
		num += 34f;
		string text3 = "T = " + RadarMapDetector.Db(RadarMapDetector.Threshold(s.Pfa)).ToString("F2", CultureInfo.InvariantCulture) + " dB · " + L("map.scope.fa") + " " + num4.ToString((num4 < 10.0) ? "F2" : "F0", CultureInfo.InvariantCulture);
		GUI.Label(new Rect(panel.x, num, panel.width, 40f), text3, text);
		num += 42f;
		GUI.Label(new Rect(panel.x, num, 150f, 30f), L("map.scope.integration"), text);
		for (int j = 0; j < RadarMapDetector.IntegrationSteps.Length; j++)
		{
			Color backgroundColor3 = GUI.backgroundColor;
			if (RadarMapDetector.IntegrationSteps[j] == s.Scanner.Integration)
			{
				GUI.backgroundColor = new Color(0.44f, 0.8f, 0.5f, 1f);
			}
			if (GUI.Button(new Rect(panel.x + 160f + 62f * (float)j, num, 60f, 30f), "×" + RadarMapDetector.IntegrationSteps[j], button) && SetIntegration != null)
			{
				SetIntegration(StationIndex, RadarMapDetector.IntegrationSteps[j]);
			}
			GUI.backgroundColor = backgroundColor3;
		}
		string text4 = "N = " + RadarMapDetector.Pulses(s.Spec, s.Scanner.Integration) + " · " + L("map.scope.period") + " " + RadarMapDetector.Revisit(s.Scanner.Spec, s.Scanner.Integration, s.Scanner.SectorWidthDeg).ToString("F3", CultureInfo.InvariantCulture) + " s";
		GUI.Label(new Rect(panel.x, num, panel.width, 24f), text4, text);
		num += 26f;
		if (s.Spec.Scan == RadarMapScan.Sector)
		{
			string text5 = L("map.scope.sector") + ": " + s.Scanner.SectorCentreDeg.ToString("F0", CultureInfo.InvariantCulture) + "° · " + s.Scanner.SectorWidthDeg.ToString("F0", CultureInfo.InvariantCulture) + "°";
			GUI.Label(new Rect(panel.x, num, panel.width, 24f), text5, text);
			num += 24f;
			float num7 = GUI.HorizontalSlider(new Rect(panel.x, num + 6f, panel.width * 0.5f - 8f, 20f), (float)s.Scanner.SectorCentreDeg, 0f, 359f);
			float num8 = GUI.HorizontalSlider(new Rect(panel.x + panel.width * 0.5f, num + 6f, panel.width * 0.5f, 20f), (float)s.Scanner.SectorWidthDeg, (float)s.Spec.BeamAzimuthDeg, (float)s.Spec.SectorDeg);
			if (Mathf.Round(num7) != (float)Math.Round(s.Scanner.SectorCentreDeg) || Mathf.Abs(num8 - (float)s.Scanner.SectorWidthDeg) >= 1f)
			{
				SetSector?.Invoke(StationIndex, Mathf.Round(num7), Mathf.Round(num8));
			}
			num += 30f;
		}
		GUI.Label(new Rect(panel.x, num, panel.width, 22f), L("map.scope.display"), small);
		num += 22f;
		DisplaySlider(new Rect(panel.x, num, panel.width, 24f), L("map.scope.scale"), ref DisplayRangeKm, 25f, 500f, "F0");
		num += 26f;
		DisplaySlider(new Rect(panel.x, num, panel.width, 24f), L("map.scope.persistence"), ref PersistenceSeconds, 2f, 60f, "F0");
		num += 26f;
		DisplaySlider(new Rect(panel.x, num, panel.width, 24f), L("map.scope.brightness"), ref Brightness, 0.3f, 1.5f, "F2");
		num += 32f;
		float num9 = s.Spec.Is3D ? 216f : 190f;
		float num10 = DrawFormular(new Rect(panel.x, num, panel.width, num9), s, time);
		DrawLaunch(new Rect(panel.x, num10, panel.width, panel.y + panel.height - num10), s, launchers, time);
	}

	private void DisplaySlider(Rect rect, string label, ref float value, float min, float max, string format)
	{
		GUI.Label(new Rect(rect.x, rect.y, rect.width * 0.45f, rect.height), label + " " + value.ToString(format, CultureInfo.InvariantCulture), small);
		value = GUI.HorizontalSlider(new Rect(rect.x + rect.width * 0.47f, rect.y + 6f, rect.width * 0.53f, rect.height), value, min, max);
	}

	private float DrawFormular(Rect r, RadarMapScopeStation s, double time)
	{
		RadarScenarioMapDraw.Fill(r, new Color(0.03f, 0.08f, 0.05f, 1f));
		RadarScenarioMapDraw.Outline(r, new Color(0.1f, 0.35f, 0.16f, 1f), 1f);
		float num = r.y + 4f;
		if (contact != null && contact.Active && s != null && contact.StationId == s.StationId)
		{
			RadarPlot last = contact.Last;
			string text = L("map.scope.contact") + " " + contact.PlotIds[0] + " · " + contact.PlotIds.Count + " " + L("map.scope.plots");
			GUI.Label(new Rect(r.x + 8f, num, r.width - 16f, 24f), text);
			num += 22f;
			text = "D " + F(last.RangeM / 1000.0, "F2") + " " + L("map.phys.km") + " · az " + F(last.BearingDeg, "F1") + "°";
			if (!double.IsNaN(last.ElevationDeg))
			{
				text = text + " · el " + F(last.ElevationDeg, "F2") + "° · H " + F(last.HeightM, "F0") + " " + L("map.phys.m");
				if (last.LowAngle)
				{
					text += " · (!) " + L("map.prop.lowAngle");
				}
			}
			GUI.Label(new Rect(r.x + 8f, num, r.width - 16f, 24f), text);
			num += 22f;
			if (!double.IsNaN(last.ElevationDeg))
			{
				num = DrawManualHeight(new Rect(r.x + 8f, num, r.width - 16f, 26f));
			}
			GUI.Label(new Rect(r.x + 8f, num, r.width - 16f, 24f), L("map.scope.snr") + " " + F(last.SnrDb, "F1") + " dB · " + L("map.scope.sigmaEcho") + " " + F(last.SigmaHatM2, (last.SigmaHatM2 < 0.1) ? "0.0000" : "F2") + " " + L("map.phys.m") + "² (" + F(RadarMapDetector.Db(last.SigmaHatM2), "F1") + " dBsm)");
			num += 22f;
			GUI.Label(new Rect(r.x + 8f, num, r.width - 16f, 24f), L("map.scope.since") + " " + F(time - last.Time, "F1") + " s");
			num += 22f;
			GUI.Label(new Rect(r.x + 8f, num, r.width - 16f, 40f), L("map.scope.estimateNote"));
			num += 36f;
			GUI.Label(new Rect(r.x + 8f, num, r.width - 16f, 40f), L("map.scope.analyzer") + ": σ̂ " + F(RadarMapDetector.Db(last.SigmaHatM2), "F1") + " dBsm · SNR̂ " + F(last.SnrDb, "F1") + " dB · T " + F(RadarMapDetector.Db(RadarMapDetector.Threshold(s.Pfa)), "F1") + " dB · ×" + s.Scanner.Integration + " · FA/" + L("map.scope.scan") + " " + F(RadarMapDetector.FalseAlarmsPerBeam(s.Spec, s.Pfa) * (double)s.Scanner.Beams, "F2"), small);
		}
		else
		{
			GUI.Label(new Rect(r.x + 8f, num, r.width - 16f, 40f), L("map.scope.pick"), small);
		}
		return r.y + r.height + 6f;
	}

	private float DrawManualHeight(Rect r)
	{
		double num = 0.0;
		GUI.Label(new Rect(r.x, r.y + 2f, 180f, 22f), L("map.prop.manualHeight"), small);
		GUI.SetNextControlName("RadarMapManualHeight");
		manualHeightText = GUI.TextField(new Rect(r.x + 184f, r.y, 20f, 24f), manualHeightText ?? "", 7);
		bool flag = false;
		if (Event.current != null && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
		{
			flag = GUI.GetNameOfFocusedControl() == "RadarMapManualHeight";
		}
		if (GUI.Button(new Rect(r.x + 270f, r.y, 70f, 24f), L("map.prop.set"), button) || flag)
		{
			if (double.TryParse(manualHeightText, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && result > -1000.0 && 30000.0 > result)
			{
				contact.ManualHeightM = result;
			}
		}
		if (GUI.Button(new Rect(r.x + 346f, r.y, 70f, 24f), L("map.prop.clear"), button))
		{
			contact.ManualHeightM = double.NaN;
			manualHeightText = "";
		}
		if (!double.IsNaN(contact.ManualHeightM))
		{
			GUI.Label(new Rect(r.x + 422f, r.y + 2f, r.width - 422f, 22f), string.Format(CultureInfo.InvariantCulture, L("map.prop.manualShown"), contact.ManualHeightM.ToString("F0", CultureInfo.InvariantCulture)), small);
		}
		return r.y + 26f;
	}

	private float DrawLaunch(Rect r, RadarMapScopeStation s, IReadOnlyList<RadarMapScopeLauncher> launchers, double time)
	{
		float num = r.y;
		if (launchers == null || launchers.Count == 0)
		{
			return num;
		}
		GUI.Label(new Rect(r.x, r.y, r.width, 22f), L("map.scope.launchers"), text);
		float num2 = r.y + 24f;
		float num3 = Mathf.Min(150f, (r.width - (float)(launchers.Count - 1) * 4f) / (float)launchers.Count);
		int num4 = 0;
		foreach (RadarMapScopeLauncher launcher in launchers)
		{
			if (SelectedLauncherId == 0)
			{
				SelectedLauncherId = launcher.Id;
			}
			Color backgroundColor = GUI.backgroundColor;
			if (launcher.Id == SelectedLauncherId)
			{
				GUI.backgroundColor = new Color(0.44f, 0.8f, 0.5f, 1f);
			}
			if (GUI.Button(new Rect(r.x + (num3 + 4f) * (float)num4, num2, num3, 30f), launcher.Id + " · " + launcher.Ammo + ((launcher.Cooldown > 0f) ? " *" : ""), button))
			{
				SelectedLauncherId = launcher.Id;
			}
			GUI.backgroundColor = backgroundColor;
			num4++;
		}
		num2 += 34f;
		bool flag = contact != null && contact.Active && s != null && contact.StationId == s.StationId;
		GUI.enabled = flag;
		if (GUI.Button(new Rect(r.x, num2, 180f, 34f), L("map.scope.launch"), button) && Launch != null)
		{
			bool flag2 = Launch(contact, SelectedLauncherId);
			Message = flag2 ? L("map.scope.launched") : L("map.scope.launchRefused");
		}
		GUI.enabled = true;
		GUI.Label(new Rect(r.x + 190f, num2 + 4f, r.width - 190f, 40f), Message, small);
		return num2 + 40f;
	}

	private static string L(string key)
	{
		return RadarScenarioText.Get(key);
	}

	private static string F(double v, string f)
	{
		return v.ToString(f, CultureInfo.InvariantCulture);
	}

	private void Styles()
	{
		if (caption != null)
		{
			return;
		}
		caption = new GUIStyle(GUI.skin.label);
		caption.fontSize = 20;
		caption.fontStyle = FontStyle.Bold;
		caption.normal.textColor = Green;
		text = new GUIStyle(GUI.skin.label);
		text.fontSize = 17;
		text.wordWrap = true;
		text.normal.textColor = new Color(0.66f, 0.9f, 0.7f, 1f);
		small = new GUIStyle(text);
		small.fontSize = 14;
		button = new GUIStyle(GUI.skin.button);
		button.fontSize = 15;
	}

	public void Draw(Rect area, IReadOnlyList<RadarMapScopeStation> stations, IReadOnlyList<RadarMapScopeLauncher> launchers, double time)
	{
		Styles();
		RadarScenarioMapDraw.Fill(area, new Color(0.012f, 0.035f, 0.022f, 1f));
		RadarScenarioMapDraw.Outline(area, new Color(0.1f, 0.35f, 0.16f, 1f), 2f);
		if (stations == null || stations.Count == 0)
		{
			return;
		}
		StationIndex = Math.Clamp(StationIndex, 0, stations.Count - 1);
		RadarMapScopeStation s = stations[StationIndex];
		float num = area.width * 0.55f;
		float num2 = Math.Min(num, area.height - 20f);
		Rect ppi = new Rect(area.x + 12f, area.y + 10f, num2, num2);
		DrawPpi(ppi, s, time);
		if (s.Spec.Is3D)
		{
			float num3 = Math.Min(num2 * 0.45f, 320f);
			DrawHeightScope(new Rect(ppi.x + 8f, ppi.y + num2 - 176f, num3, 168f), s, time);
		}
		GUI.Label(new Rect(area.x + 20f, area.y + 14f, 820f, 28f), L("map.model.physical"), caption);
		DrawPanel(new Rect(ppi.x + num2 + 16f, area.y + 12f, area.x + area.width - (ppi.x + num2) - 28f, area.height - 24f), stations, launchers, time);
		UpdateContact(s, time);
		HandleClick(ppi, s, time);
	}

	private Vector2 PlotPoint(Rect ppi, double rangeM, double bearingDeg)
	{
		float num = (float)(bearingDeg * RadarMapUnits.DegToRad);
		float num2 = (float)(rangeM / 1000.0 / (double)DisplayRangeKm) * ppi.width * 0.5f;
		return new Vector2(MathF.Sin(num) * num2 + ppi.width * 0.5f + ppi.x, -MathF.Cos(num) * num2 + ppi.height * 0.5f + ppi.y);
	}

	private void DrawPpi(Rect ppi, RadarMapScopeStation s, double time)
	{
		float num = Brightness * 0.28f;
		Vector2 centre = new Vector2(ppi.x + ppi.width * 0.5f, ppi.y + ppi.height * 0.5f);
		RadarScenarioMapDraw.Circle(centre, ppi.width * 0.5f, new Color(Green.r, Green.g, Green.b, 0.55f), 1.5f);
		for (long num2 = 50L; num2 <= (double)DisplayRangeKm; num2 += 50L)
		{
			float num3 = (float)((double)num2 / (double)DisplayRangeKm) * (ppi.width * 0.5f);
			RadarScenarioMapDraw.Circle(centre, num3, new Color(Green.r, Green.g, Green.b, num), 1f);
			GUI.Label(new Rect(centre.x + 4f, centre.y - num3 - 18f, 20f, 20f), num2 + " " + L("map.phys.km"), small);
		}
		float num4 = (float)(s.Spec.InstrumentedRangeM / 1000.0 / (double)DisplayRangeKm) * (ppi.width * 0.5f);
		if (num4 <= ppi.width * 0.5f * 1.001f)
		{
			RadarScenarioMapDraw.Circle(centre, num4, new Color(Green.r, Green.g, Green.b, 0.75f), 1.5f);
		}
		for (int i = 0; i < 360; i += 10)
		{
			float num5 = (float)i * 0.017453292f;
			float num6 = ((i % 30 == 0) ? 0.93f : 0.96f);
			Vector2 a = new Vector2(MathF.Sin(num5) * (ppi.width * 0.5f) * num6 + centre.x, -MathF.Cos(num5) * (ppi.width * 0.5f) * num6 + centre.y);
			Vector2 b = new Vector2(MathF.Sin(num5) * (ppi.width * 0.5f) + centre.x, -MathF.Cos(num5) * (ppi.width * 0.5f) + centre.y);
			RadarScenarioMapDraw.Line(b, a, new Color(Green.r, Green.g, Green.b, num), 1f);
			if (i % 30 == 0)
			{
				GUI.Label(new Rect(MathF.Sin(num5) * (ppi.width * 0.5f) * 0.88f + centre.x - 18f, -MathF.Cos(num5) * (ppi.width * 0.5f) * 0.88f + centre.y - 10f, 40f, 20f), i.ToString(), small);
			}
		}
		double num7 = s.Scanner.BeamAzimuth(time);
		if (s.Spec.Scan == RadarMapScan.Sector)
		{
			double[] array = new double[2]
			{
				s.Scanner.SectorCentreDeg - s.Scanner.SectorWidthDeg * 0.5,
				s.Scanner.SectorCentreDeg + s.Scanner.SectorWidthDeg * 0.5
			};
			double[] array2 = array;
			foreach (double num8 in array2)
			{
				float num9 = (float)(num8 * RadarMapUnits.DegToRad);
				Vector2 a2 = new Vector2(MathF.Sin(num9) * (ppi.width * 0.5f) + centre.x, -MathF.Cos(num9) * (ppi.width * 0.5f) + centre.y);
				RadarScenarioMapDraw.Line(centre, a2, new Color(Green.r, Green.g, Green.b, 0.6f), 1.5f);
			}
		}
		else if (s.Emitting)
		{
			float num10 = (float)(num7 * RadarMapUnits.DegToRad);
			Vector2 a3 = new Vector2(MathF.Sin(num10) * (ppi.width * 0.5f) + centre.x, -MathF.Cos(num10) * (ppi.width * 0.5f) + centre.y);
			RadarScenarioMapDraw.Line(centre, a3, new Color(Green.r, Green.g, Green.b, Brightness * 0.9f), 2.5f);
		}
		double num11 = RadarMapDetector.Db(RadarMapDetector.Threshold(s.Pfa));
		RadarScenarioMapDraw.ClipBounds = ppi;
		foreach (RadarPlot plot in s.Plots)
		{
			double num12 = time - plot.Time;
			if (num12 < 0.0 || num12 > (double)PersistenceSeconds)
			{
				continue;
			}
			float num13 = Math.Max(0.15f, Math.Min(1f, (float)((plot.SnrDb - num11) / 20.0)));
			float num14 = (1f - (float)(num12 / (double)PersistenceSeconds)) * num13 * Brightness;
			Vector2 p = PlotPoint(ppi, plot.RangeM, plot.BearingDeg);
			RadarScenarioMapDraw.Fill(new Rect(p.x - 3f, p.y - 3f, 6f, 6f), new Color(Green.r, Green.g, Green.b, num14));
			if (contact.Contains(plot.PlotId))
			{
				float num15 = Math.Min(1f, Math.Max(0f, num14 + 0.4f));
				RadarScenarioMapDraw.Circle(p, 9f, new Color(1f, 0.95f, 0.55f, num15), 1.5f);
			}
		}
		RadarScenarioMapDraw.ClipBounds = null;
	}

	private void HandleClick(Rect ppi, RadarMapScopeStation s, double time)
	{
		Event current = Event.current;
		if (current == null || current.type != EventType.MouseDown || current.button != 0)
		{
			return;
		}
		Vector2 mousePosition = current.mousePosition;
		if (!ppi.Contains(mousePosition))
		{
			return;
		}
		float num = 14f;
		int num2 = 0;
		foreach (RadarPlot plot in s.Plots)
		{
			if (time - plot.Time <= (double)PersistenceSeconds && plot.Time <= time)
			{
				Vector2 b = PlotPoint(ppi, plot.RangeM, plot.BearingDeg);
				float num3 = Vector2.Distance(current.mousePosition, b);
				if (num3 < num)
				{
					num = num3;
					num2 = plot.PlotId;
				}
			}
		}
		if (num2 != 0)
		{
			SelectPlot(s, num2);
			current.Use();
		}
	}

	public void SelectPlot(RadarMapScopeStation s, int plotId)
	{
		foreach (RadarPlot plot in s.Plots)
		{
			if (plot.PlotId == plotId)
			{
				SelectedPlotId = plotId;
				contact.Start(s.StationId, plot, s.MapPosition);
				return;
			}
		}
	}

	private void UpdateContact(RadarMapScopeStation s, double time)
	{
		if (contact.StationId == s.StationId)
		{
			foreach (RadarPlot plot in s.Plots)
			{
				contact.Offer(plot, s.MapPosition, AssociationKm);
			}
		}
	}

	private void DrawHeightScope(Rect r, RadarMapScopeStation s, double time)
	{
		if (contact == null || !contact.Active || s == null || contact.StationId != s.StationId)
		{
			return;
		}
		RadarScenarioMapDraw.Fill(r, new Color(0.02f, 0.06f, 0.035f, 0.92f));
		RadarScenarioMapDraw.Outline(r, new Color(0.1f, 0.35f, 0.16f, 1f), 1f);
		GUI.Label(new Rect(r.x + 6f, r.y + 2f, r.width - 12f, 18f), L("map.prop.heightScope"), small);
		Rect plot = default(Rect);
		double near = double.PositiveInfinity;
		double far = 0.0;
		float zero = 0f;
		double top = 100.0;
		bool flag = false;
		foreach (RadarPlot plot2 in s.Plots)
		{
			if (!contact.Contains(plot2.PlotId))
			{
				continue;
			}
			if (double.IsNaN(plot2.HeightM))
			{
				continue;
			}
			top = Math.Max(top, Math.Abs(plot2.HeightM) * 1.2);
			near = Math.Min(near, plot2.RangeM);
			far = Math.Max(far, plot2.RangeM);
			flag = flag || plot2.LowAngle;
		}
		if (!double.IsNaN(contact.ManualHeightM))
		{
			top = Math.Max(top, Math.Abs(contact.ManualHeightM) * 1.2);
		}
		if (near == double.PositiveInfinity)
		{
			return;
		}
		if (far - near < 2000.0)
		{
			double num = (far + near) * 0.5;
			near = num - 1000.0;
			far = num + 1000.0;
		}
		Vector2 Point(double range, double height)
		{
			return new Vector2((float)((range - near) / (far - near)) * plot.width + plot.x, zero - (float)(height / top) * plot.height * 0.5f);
		}
		plot = new Rect(r.x + 6f, r.y + 22f, r.width - 12f, r.height - (flag ? 46f : 28f));
		zero = (r.height - (flag ? 46f : 28f)) * 0.5f + (r.y + 22f);
		RadarScenarioMapDraw.Line(new Vector2(r.x + 6f, zero), new Vector2(r.x + r.width - 6f, zero), new Color(Green.r, Green.g, Green.b, 0.35f), 1f);
		foreach (RadarPlot plot3 in s.Plots)
		{
			if (!contact.Contains(plot3.PlotId))
			{
				continue;
			}
			if (double.IsNaN(plot3.HeightM))
			{
				continue;
			}
			Vector2 p = Point(plot3.RangeM, plot3.HeightM);
			RadarScenarioMapDraw.Fill(new Rect(p.x - 2.5f, p.y - 2.5f, 5f, 5f), Green);
			if (plot3.LowAngle)
			{
				Vector2 p2 = Point(plot3.RangeM, 0.0 - plot3.HeightM);
				RadarScenarioMapDraw.Fill(new Rect(p2.x - 2f, p2.y - 2f, 4f, 4f), new Color(Green.r, Green.g, Green.b, 0.3f));
			}
		}
		if (!double.IsNaN(contact.ManualHeightM))
		{
			float num2 = zero - (float)(contact.ManualHeightM / top) * plot.height * 0.5f;
			RadarScenarioMapDraw.Line(new Vector2(plot.x, num2), new Vector2(plot.x + plot.width, num2), new Color(1f, 0.95f, 0.55f, 0.8f), 1f);
		}
		GUI.Label(new Rect(r.x + 6f, plot.y - 2f, 120f, 16f), "+" + top.ToString("F0", CultureInfo.InvariantCulture) + " " + L("map.phys.m"), small);
		if (flag)
		{
			GUI.Label(new Rect(r.x + 6f, r.y + r.height - 22f, r.width - 12f, 20f), L("map.prop.ghost"), small);
		}
	}
}
