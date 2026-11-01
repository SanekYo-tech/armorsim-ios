using System;
using System.Collections.Generic;

using UnityEngine;

public sealed class RadarMapObserverAtlas : IDisposable
{
	public const double RangeDb = 40.0;

	public const double TraceSeconds = 60.0;

	public const double TraceStep = 0.5;

	private readonly Dictionary<RadarAtlas, Texture2D> textures;

	private readonly List<RadarMapOpportunity> opportunities;

	private GUIStyle label;

	private GUIStyle value;

	private GUIStyle small;

	private GUIStyle markHit;

	private GUIStyle markMiss;

	public int Tab;

	public readonly RadarMapObserverPath Path;

	public RadarMapObserverAtlas()
	{
		textures = new Dictionary<RadarAtlas, Texture2D>();
		opportunities = new List<RadarMapOpportunity>();
		LastSigma = 0.0;
		LastMeanSnr = 0.0;
		LastPd = 0.0;
		Path = new RadarMapObserverPath();
	}

	public double LastSigma { get; private set; }

	public double LastMeanSnr { get; private set; }

	public double LastPd { get; private set; }

	public RadarAtlasCell LastCell { get; private set; }

	public int LastTracePoints { get; private set; }

	public int LastMarks { get; private set; }

	private static string L(string key)
	{
		return RadarScenarioText.Get(key);
	}

	private static string F(double v, string format)
	{
		return v.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
	}

	public void Draw(Rect area, RadarMapSession session, RadarMapObserver observer)
	{
		if (label == null)
		{
			label = new GUIStyle(GUI.skin.label);
			label.fontSize = 15;
			label.wordWrap = true;
			label.normal.textColor = new Color(0.2f, 0.24f, 0.26f, 1f);
			value = new GUIStyle(label);
			value.fontSize = 17;
			value.fontStyle = FontStyle.Bold;
			value.wordWrap = false;
			value.normal.textColor = new Color(0.08f, 0.1f, 0.12f, 1f);
			small = new GUIStyle(label);
			small.fontSize = 13;
			markHit = new GUIStyle(small);
			markHit.normal.textColor = new Color(0.05f, 0.55f, 0.15f, 1f);
			markMiss = new GUIStyle(small);
			markMiss.normal.textColor = new Color(0.75f, 0.12f, 0.08f, 1f);
		}
		if (session.World != null)
		{
			for (int i = 0; i < 2; i++)
			{
				Color backgroundColor = GUI.backgroundColor;
				if (Tab == i)
				{
					GUI.backgroundColor = new Color(0.95f, 0.8f, 0.55f, 1f);
				}
				if (GUI.Button(new Rect(area.x + (float)i * 184f, area.y, 180f, 24f), L((i != 0) ? "map.prop.tabPath" : "map.prop.tabSignal")))
				{
					Tab = i;
				}
				GUI.backgroundColor = backgroundColor;
			}
			area.y += 28f;
			area.height -= 28f;
			if (Tab == 1)
			{
				Path.Draw(area, session, observer);
				return;
			}
		}
		RadarMapSession.Plane plane = session.Planes[observer.PlaneIndex];
		RadarAtlas radarAtlas = plane.Atlases[observer.RadarIndex];
		if (radarAtlas == null)
		{
			GUI.Label(area, L("map.atlas.state.none"), label);
			return;
		}
		RadarAspect aspect = default(RadarAspect);
		double num3 = session.SigmaMean(observer.PlaneIndex, observer.RadarIndex, out aspect);
		LastSigma = num3;
		RadarAtlasCell radarAtlasCell = radarAtlas.CellAt(aspect.AzimuthDeg, aspect.ElevationDeg);
		LastCell = radarAtlasCell;
		float num4 = area.width * 0.5f - 6f;
		float num5 = ((num4 > 280f) ? 280f : num4);
		float num6 = num5 * 0.5f;
		GUI.Label(new Rect(area.x, area.y, num4, 20f), L("map.obs.trace"), small);
		float num7 = area.y + 20f;
		Texture2D texture2D;
		if (!textures.TryGetValue(radarAtlas, out texture2D))
		{
			double topDb;
			texture2D = RadarAtlasPalette.Texture(radarAtlas, RangeDb, out topDb);
			textures[radarAtlas] = texture2D;
		}
		GUI.DrawTexture(new Rect(area.x, num7, num5, num6), texture2D, ScaleMode.StretchToFill, alphaBlend: false);
		RadarScenarioMapDraw.Outline(new Rect(area.x, num7, num5, num6), new Color(0.3f, 0.33f, 0.35f, 1f), 1f);
		int num8 = 0;
		for (double num9 = Math.Max(0.0, session.Time - TraceSeconds); num9 <= session.Time + 5E-09; num9 += TraceStep)
		{
			RadarAspect aspect2 = session.Aspect(observer.PlaneIndex, observer.RadarIndex, num9);
			float num10 = (float)((aspect2.AzimuthDeg + 180.0) / 360.0) * num5 + area.x - 2.5f;
			float num11 = num7 + num6 - (float)((aspect2.ElevationDeg + 90.0) / 180.0) * num6 - 2.5f;
			RadarScenarioMapDraw.Fill(new Rect(num10, num11, 3f, 3f), Color.black);
			num8++;
		}
		LastTracePoints = num8;
		float num12 = (float)((aspect.AzimuthDeg + 180.0) / 360.0) * num5 + area.x;
		float num13 = num7 + num6 - (float)((aspect.ElevationDeg + 90.0) / 180.0) * num6;
		RadarScenarioMapDraw.Circle(new Vector2(num12, num13), 5f, Color.white, 2.5f);
		RadarScenarioMapDraw.Circle(new Vector2(num12, num13), 5f, Color.black, 1.2f);
		string text = ((num3 > 0.0) ? F(10.0 * Math.Log10(num3), "F2") : "−∞");
		GUI.Label(new Rect(area.x, num7 + num6 + 4f, num4, 22f), "σ̄ = " + F(num3, (num3 >= 1.45) ? "0.0000" : "F2") + " " + L("map.phys.m") + "² (" + text + " dBsm) · k2 " + F(radarAtlasCell.K2, "F2"), value);
		string text4 = RadarAtlasLibrary.Note(radarAtlas) + " · M = " + radarAtlas.CellCount;
		if (radarAtlas.Header.coating != null)
		{
			text4 = text4 + " · " + L("map.atlas.boundary") + " " + F(radarAtlas.Header.frequencyHz / 1000000000.0, "F2");
		}
		if (plane.Coating != null && !plane.Coating.IsMetal)
		{
			text4 = text4 + " · " + L("map.coat.note");
		}
		GUI.Label(new Rect(area.x, num7 + num6 + 26f, num4, area.height - num6 - 26f - 4f), text4, small);
		float num15 = area.x + num5 + 12f;
		float num16 = area.x + area.width - num15;
		double num17 = session.MeanSnr(observer.PlaneIndex, observer.RadarIndex, out var pd, out var sigmaBar, out var f4);
		LastMeanSnr = num17;
		LastSigma = sigmaBar;
		LastPd = pd;
		GUI.Label(new Rect(num15, area.y, num16, 20f), L("map.obs.snr"), small);
		float num18 = area.y + 20f;
		string text5 = ((num17 > 0.0) ? F(10.0 * Math.Log10(num17), "F2") : "−∞");
		string text6 = ((pd > 0.0) ? F(10.0 * Math.Log10(pd), "F2") : "−∞");
		string text7 = ((f4 > 0.0) ? F(10.0 * Math.Log10(f4), "F3") : "−∞");
		GUI.Label(new Rect(num15, num18, num16, 22f), "SNR̄ " + text5 + " dB · Pd " + text6 + " · F⁴ " + text7 + " dB", value);
		GUI.Label(new Rect(num15, area.y + 42f, num16, 20f), L("map.obs.graphs"), small);
		session.OpportunitiesOf(observer.RadarIndex, plane.Source.Id, opportunities);
		LastMarks = opportunities.Count;
		float num19 = area.height - 70f;
		float num20 = num19 * 0.5f - num6;
		Graph(new Rect(num15, area.y + 64f, num16, num20), session.Time, "σ̄", (RadarMapOpportunity o) => o.SigmaBarM2);
		Graph(new Rect(num15, area.y + 64f + num20 + 8f, num16, num20), session.Time, "SNR̄", (RadarMapOpportunity o) => o.MeanSnr);
	}

	private void Graph(Rect r, double now, string name, Func<RadarMapOpportunity, double> db)
	{
		RadarScenarioMapDraw.Fill(r, new Color(0.97f, 0.97f, 0.97f, 1f));
		RadarScenarioMapDraw.Outline(r, new Color(0.45f, 0.48f, 0.5f, 1f), 1f);
		if (opportunities.Count == 0)
		{
			GUI.Label(new Rect(r.x + 4f, r.y + 2f, r.width, 18f), name + " —", small);
			return;
		}
		double num = double.PositiveInfinity;
		double num2 = double.NegativeInfinity;
		foreach (RadarMapOpportunity opportunity in opportunities)
		{
			double num3 = db(opportunity);
			if (!double.IsNaN(num3) && !double.IsInfinity(num3))
			{
				num = Math.Min(num, num3);
				num2 = Math.Max(num2, num3);
			}
		}
		if (double.IsInfinity(num))
		{
			num = -40.0;
			num2 = 0.0;
		}
		if (num2 - num < 10.0)
		{
			double num4 = (num2 + num) * 0.5;
			num = num4 - 5.0;
			num2 = num4 + 5.0;
		}
		Vector2 vector = default(Vector2);
		bool flag = true;
		foreach (RadarMapOpportunity opportunity2 in opportunities)
		{
			double num5 = db(opportunity2);
			if (!double.IsNaN(num5) && !double.IsInfinity(num5))
			{
				float num6 = Mathf.Clamp01((float)((opportunity2.Time - (now - TraceSeconds)) / TraceSeconds));
				float num7 = Mathf.Clamp01((float)((num5 - num) / (num2 - num)));
				float num8 = r.y + r.height - (r.height - 16f) * num7 - 4f;
				float num9 = num6 * r.width + r.x;
				if (!flag)
				{
					RadarScenarioMapDraw.Line(vector, new Vector2(num9, num8), new Color(0.2f, 0.3f, 0.55f, 1f), 1.5f);
				}
				RadarScenarioMapDraw.Label(new Rect(num9 - 5f, num8 - 16f, 12f, 16f), opportunity2.Detected ? "●" : "×", opportunity2.Detected ? markHit : markMiss);
				vector = new Vector2(num9, num8);
				flag = false;
			}
		}
		GUI.Label(new Rect(r.x + 4f, r.y + 1f, r.width, 18f), name + " " + F(num2, "F1") + "…" + F(num, "F1") + " dB", small);
	}

	public void Dispose()
	{
		foreach (Texture2D value in textures.Values)
		{
			if (value != null)
			{
				UnityEngine.Object.Destroy(value);
			}
		}
		textures.Clear();
	}
}
