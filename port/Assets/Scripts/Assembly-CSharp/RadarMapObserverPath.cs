using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class RadarMapObserverPath
{
	public const int ProfileSamples = 160;

	private GUIStyle label;

	private GUIStyle value;

	private GUIStyle small;

	private GUIStyle markHit;

	private GUIStyle markMiss;

	private readonly List<RadarMapOpportunity> opportunities;

	public RadarMapObserverPath()
	{
		LastMonopulseHeight = double.NaN;
		opportunities = new List<RadarMapOpportunity>();
	}

	public RadarMapPath LastPath { get; private set; }

	public double LastMonopulseHeight { get; private set; }

	public int Draws { get; private set; }

	public int LastF4Marks { get; private set; }

	private static string L(string key)
	{
		return RadarScenarioText.Get(key);
	}

	private static string F(double v, string format)
	{
		return v.ToString(format, CultureInfo.InvariantCulture);
	}

	public void Draw(Rect area, RadarMapSession session, RadarMapObserver observer)
	{
		RadarMapPath path = default(RadarMapPath);
		if (label == null)
		{
			label = new GUIStyle(GUI.skin.label);
			label.fontSize = 14;
			label.wordWrap = true;
			label.normal.textColor = new Color(0.2f, 0.24f, 0.26f, 1f);
			value = new GUIStyle(label);
			value.fontSize = 15;
			value.fontStyle = FontStyle.Bold;
			value.wordWrap = false;
			value.normal.textColor = new Color(0.08f, 0.1f, 0.12f, 1f);
			small = new GUIStyle(label);
			small.fontSize = 12;
			markHit = new GUIStyle(small);
			markHit.normal.textColor = new Color(0.05f, 0.55f, 0.15f, 1f);
			markMiss = new GUIStyle(small);
			markMiss.normal.textColor = new Color(0.75f, 0.12f, 0.08f, 1f);
		}
		Draws++;
		RadarMapSession.Radar radar = session.Radars[observer.RadarIndex];
		RadarMapPose pose = session.Pose(observer.PlaneIndex);
		path = session.PathAt(observer.RadarIndex, pose);
		LastPath = path;
		float num = area.x;
		float num2 = area.y;
		float num3 = area.height * 0.42f;
		string text = L((path.State == RadarMapPathState.Open) ? "map.prop.state.open" : ((path.State == RadarMapPathState.Horizon) ? "map.prop.state.horizon" : "map.prop.state.terrain"));
		string text2 = (path.State != RadarMapPathState.Open) ? "0" : (F(path.F4Db, "F1") + " dB");
		GUI.Label(new Rect(num, num2, num3, 20f), text + " · F⁴ " + text2, value);
		num2 += 22f;
		GUI.Label(new Rect(num, num2, num3, 18f), L("map.obs.horizon") + " " + F(path.HorizonMargin / 1000.0, "F1") + " " + L("map.phys.km"), small);
		if (path.State == RadarMapPathState.Open)
		{
			if (path.HasRay)
			{
				double num4 = path.Ray.Grazing * 57.29577951308232;
				string text3 = L((path.Surface == RadarMapSurface.FreshWater) ? "map.prop.surface.fresh" : ((path.Surface == RadarMapSurface.SeaWater) ? "map.prop.surface.sea" : ((path.Surface == RadarMapSurface.Ground) ? "map.prop.surface.ground" : "map.prop.surface.diffuse")));
				GUI.Label(new Rect(num, num2, num3, 36f), L("map.prop.surface") + ": " + text3, small);
				num2 += 34f;
				GUI.Label(new Rect(num, num2, num3, 18f), "ψ " + F(num4, "F3") + "° · ΔR " + F(path.Ray.PathDifference * 1000.0, "F1") + " mm · " + L("map.prop.level") + " " + F(path.SurfaceLevel, "F0") + " " + L("map.phys.m"), small);
				num2 += 18f;
				if (path.Surface != RadarMapSurface.Diffuse)
				{
					string text4 = ((radar.Source.Polarization == 1) ? " · V" : " · H");
					GUI.Label(new Rect(num, num2, num3, 18f), "|Γ| " + F(path.Gamma.Magnitude, "F4") + " (" + F(Math.Atan2(path.Gamma.Imaginary, path.Gamma.Real) * 57.29577951308232, "F1") + "°) · ρ " + F(path.Rho, "F4") + text4, small);
					num2 += 18f;
					string text5 = L("map.prop.legs");
					if (!path.ReflectionMasked)
					{
						text5 = "x " + F(path.MonopulseRatio, "F4") + " · α " + F(path.Alpha * 57.29577951308232, "F1") + "°";
					}
					GUI.Label(new Rect(num, num2, num3, 18f), text5, small);
					num2 += 18f;
					if (path.PermittivityClamped)
					{
						GUI.Label(new Rect(num, num2, num3, 18f), L("map.prop.clamped"), small);
						num2 += 18f;
					}
				}
			}
			else
			{
				GUI.Label(new Rect(num, num2, num3, 18f), L("map.prop.noSpecular"), small);
				num2 += 18f;
			}
		}
		if (radar.Spec.Is3D)
		{
			RadarMapMonopulse.Result result = RadarMapMonopulse.Evaluate(path, radar.Spec.BeamElevationDeg);
			LastMonopulseHeight = result.Height;
			GUI.Label(new Rect(num, num2, num3, 36f), string.Format(CultureInfo.InvariantCulture, L("map.prop.twin"), F(pose.Position.z, "F1"), F(result.Height, "F1")), small);
			num2 += 34f;
		}
		DrawProfile(new Rect(num + num3 + 10f, area.y + 18f, area.width - num3 - 10f, area.height - 22f), session, radar, pose, path);
		GUI.Label(new Rect(num + num3 + 10f, area.y, area.width - num3 - 10f, 18f), L("map.prop.profile"), small);
		if (area.y + area.height - num2 > 56f)
		{
			DrawF4(new Rect(num, num2 + 4f, num3, area.y + area.height - num2 - 6f), session, observer);
		}
	}

	private void DrawF4(Rect r, RadarMapSession session, RadarMapObserver observer)
	{
		RadarScenarioMapDraw.Fill(r, new Color(0.97f, 0.97f, 0.97f, 1f));
		RadarScenarioMapDraw.Outline(r, new Color(0.45f, 0.48f, 0.5f, 1f), 1f);
		session.OpportunitiesOf(observer.RadarIndex, session.Planes[observer.PlaneIndex].Source.Id, opportunities);
		LastF4Marks = opportunities.Count;
		double num = session.Time;
		float num2 = r.y + 1f;
		float num3 = r.x + 4f;
		GUI.Label(new Rect(num3, num2, r.width, 16f), "F⁴(t) · 60 s · " + 15.0.ToString("+0", CultureInfo.InvariantCulture) + "…" + (-40.0).ToString("0", CultureInfo.InvariantCulture) + " dB", small);
		float num4 = r.y + r.height - 3f - (r.height - 20f) * 0.72727275f;
		RadarScenarioMapDraw.Line(new Vector2(r.x, num4), new Vector2(r.x + r.width, num4), new Color(0.6f, 0.6f, 0.6f, 1f), 1f);
		Vector2 vector = default(Vector2);
		bool flag = true;
		foreach (RadarMapOpportunity opportunity in opportunities)
		{
			double num5 = Math.Max(-40.0, Math.Min(15.0, 10.0 * Math.Log10(Math.Max(opportunity.F4, 1E-48))));
			float num6 = Mathf.Clamp01((float)((opportunity.Time - (num - 60.0)) / 60.0));
			float num7 = r.y + r.height - 3f - (float)((num5 + 40.0) / 55.0) * (r.height - 20f);
			float num8 = num6 * r.width + r.x;
			if (!flag)
			{
				RadarScenarioMapDraw.Line(vector, new Vector2(num8, num7), new Color(0.2f, 0.3f, 0.55f, 1f), 1.2f);
			}
			RadarScenarioMapDraw.Label(new Rect(num8 - 4f, num7 - 9f, 12f, 16f), opportunity.Detected ? "●" : "×", opportunity.Detected ? markHit : markMiss);
			vector = new Vector2(num8, num7);
			flag = false;
		}
	}

	private void DrawProfile(Rect r, RadarMapSession session, RadarMapSession.Radar radar, RadarMapPose pose, in RadarMapPath path)
	{
		RadarScenarioMapDraw.Fill(r, new Color(0.97f, 0.97f, 0.97f, 1f));
		RadarScenarioMapDraw.Outline(r, new Color(0.45f, 0.48f, 0.5f, 1f), 1f);
		if (session.World == null || path.GroundRange <= 0.0)
		{
			return;
		}
		double groundRange = path.GroundRange;
		double num = (pose.Position.x - radar.Antenna.x) / groundRange;
		double num2 = (pose.Position.y - radar.Antenna.y) / groundRange;
		double[] array = new double[ProfileSamples + 1];
		double num3 = Math.Max(radar.Antenna.z, pose.Position.z);
		for (int i = 0; i <= ProfileSamples; i++)
		{
			double num4 = (double)i * groundRange / (double)ProfileSamples;
			RadarScenarioTerrainSample radarScenarioTerrainSample = RadarScenarioTerrain.Sample(new Vector2((float)((num4 * num + radar.Antenna.x) / 1000.0), (float)((num4 * num2 + radar.Antenna.y) / 1000.0)), session.World.Seed, session.World.Coast);
			double num5 = ((radarScenarioTerrainSample.Kind == RadarScenarioTerrainKind.Sea) ? 0.0 : ((double)radarScenarioTerrainSample.Height01 * RadarMapWorld.HeightScaleM));
			array[i] = (groundRange - num4) * num4 / (2.0 * RadarMapUnits.EffectiveEarthRadius) + num5;
			num3 = Math.Max(num3, array[i]);
		}
		double num6 = Math.Max(10.0, num3 * 1.15);
		Vector2 P(double s, double h)
		{
			return new Vector2((float)(s / groundRange) * r.width + r.x, r.y + r.height - 2f - (float)(h / num6) * (r.height - 4f));
		}
		for (int j = 0; j < ProfileSamples; j++)
		{
			double num7 = (double)j * groundRange / (double)ProfileSamples;
			double num8 = (double)(j + 1) * groundRange / (double)ProfileSamples;
			RadarScenarioMapDraw.Line(P(num7, array[j]), P(num8, array[j + 1]), new Color(0.55f, 0.47f, 0.36f, 1f), 1.6f);
			RadarScenarioMapDraw.Line(P(num7, (groundRange - num7) * num7 / (2.0 * RadarMapUnits.EffectiveEarthRadius)), P(num8, (groundRange - num8) * num8 / (2.0 * RadarMapUnits.EffectiveEarthRadius)), new Color(0.25f, 0.45f, 0.65f, 1f), 1f);
		}
		Color color = ((path.State == RadarMapPathState.Open) ? new Color(0.1f, 0.45f, 0.15f, 1f) : new Color(0.75f, 0.12f, 0.08f, 1f));
		RadarScenarioMapDraw.Line(P(0.0, radar.Antenna.z), P(groundRange, pose.Position.z), color, 1.8f);
		if (path.HasRay && path.State == RadarMapPathState.Open)
		{
			double num9 = (groundRange - path.Ray.RadarDistance) * path.Ray.RadarDistance / (2.0 * RadarMapUnits.EffectiveEarthRadius) + path.SurfaceLevel;
			Color color2 = (path.Reflection ? new Color(0.2f, 0.3f, 0.7f, 1f) : new Color(0.55f, 0.55f, 0.6f, 1f));
			Vector2 vector = P(path.Ray.RadarDistance, num9);
			RadarScenarioMapDraw.Line(P(0.0, radar.Antenna.z), vector, color2, 1.3f);
			RadarScenarioMapDraw.Line(vector, P(groundRange, pose.Position.z), color2, 1.3f);
			RadarScenarioMapDraw.Circle(vector, 3f, color2, 1.5f);
		}
		GUI.Label(new Rect(r.x + 4f, r.y + 2f, r.width, 16f), F(num6, "F0") + " " + L("map.phys.m"), small);
		GUI.Label(new Rect(r.x + r.width - 70f, r.y + r.height - 18f, 40f, 16f), F(groundRange / 1000.0, "F1") + " " + L("map.phys.km"), small);
	}
}
