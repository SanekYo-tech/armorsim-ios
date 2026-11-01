using System;
using System.Collections.Generic;
using UnityEngine;

public static class RadarScenarioGameModel
{
	public static readonly float[] CoatingVisibility;

	public static readonly string[] CoatingNames;

	static RadarScenarioGameModel()
	{
		CoatingVisibility = new float[4]
		{
			1f,
			0.79f,
			0.62f,
			0.44f
		};
		CoatingNames = new string[4] { "Bare", "Veil", "Mist", "Shadow" };
	}

	public static RadarScenarioPose Pose(RadarScenarioAircraft aircraft, float elapsed)
	{
		if (aircraft == null || aircraft.Route == null || aircraft.Route.Count == 0)
		{
			return new RadarScenarioPose
			{
				Finished = true
			};
		}
		List<RadarScenarioWaypoint> route = aircraft.Route;
		RadarScenarioWaypoint radarScenarioWaypoint = route[0];
		RadarScenarioPose radarScenarioPose = default(RadarScenarioPose);
		radarScenarioPose.Position = radarScenarioWaypoint.Position;
		radarScenarioPose.Heading = 0f;
		radarScenarioPose.Altitude = radarScenarioWaypoint.Altitude;
		if (route.Count == 1)
		{
			radarScenarioPose.Finished = true;
			return radarScenarioPose;
		}
		int num = route.Count;
		if (!aircraft.Loop)
		{
			num--;
		}
		double num2 = 0.0;
		for (int i = 0; i < num; i++)
		{
			float x = route[i].Position.x - route[(i + 1) % route.Count].Position.x;
			float y = route[i].Position.y - route[(i + 1) % route.Count].Position.y;
			num2 += (float)Math.Sqrt(x * x + y * y);
		}
		if (num2 <= 1E-06)
		{
			radarScenarioPose.Finished = true;
			return radarScenarioPose;
		}
		double num3 = (RadarScenarioData.Finite(elapsed) ? Math.Max(0.0, elapsed) : 0.0) * (RadarScenarioData.Finite(aircraft.Speed) ? Math.Max(0.0, aircraft.Speed) : 0.0);
		bool flag = false;
		double num4;
		if (aircraft.Loop)
		{
			num4 = num3 % num2;
		}
		else
		{
			flag = num2 <= num3;
			num4 = Math.Min(num3, num2);
		}
		for (int j = 0; j < num; j++)
		{
			RadarScenarioWaypoint radarScenarioWaypoint2 = route[j];
			RadarScenarioWaypoint radarScenarioWaypoint3 = route[(j + 1) % route.Count];
			float x2 = radarScenarioWaypoint3.Position.x - radarScenarioWaypoint2.Position.x;
			float y2 = radarScenarioWaypoint3.Position.y - radarScenarioWaypoint2.Position.y;
			float num5 = (float)Math.Sqrt(x2 * x2 + y2 * y2);
			if (num5 > 1E-06f)
			{
				if (num4 < (double)num5 || j == num - 1 || (flag && num4 <= (double)num5 + 1E-05))
				{
					float num6 = Mathf.Clamp01((float)(num4 / (double)num5));
					radarScenarioPose.Position = Vector2.Lerp(radarScenarioWaypoint2.Position, radarScenarioWaypoint3.Position, num6);
					radarScenarioPose.Altitude = (radarScenarioWaypoint3.Altitude - radarScenarioWaypoint2.Altitude) * Mathf.Clamp01(num6) + radarScenarioWaypoint2.Altitude;
					float num7 = MathF.Atan2(x2, y2) * Mathf.Rad2Deg;
					num7 -= MathF.Floor(num7 / 360f) * 360f;
					radarScenarioPose.Heading = Mathf.Clamp(num7, 0f, 360f);
					radarScenarioPose.Finished = flag;
					return radarScenarioPose;
				}
				num4 -= num5;
			}
		}
		RadarScenarioWaypoint radarScenarioWaypoint4 = route[route.Count - 1];
		radarScenarioPose.Position = radarScenarioWaypoint4.Position;
		radarScenarioPose.Altitude = radarScenarioWaypoint4.Altitude;
		float num8 = 0f;
		for (int k = route.Count - 2; k >= 0; k--)
		{
			float x3 = radarScenarioWaypoint4.Position.x - route[k].Position.x;
			float y3 = radarScenarioWaypoint4.Position.y - route[k].Position.y;
			if (x3 * x3 + y3 * y3 > 1E-06f)
			{
				num8 = MathF.Atan2(x3, y3) * Mathf.Rad2Deg;
				num8 -= MathF.Floor(num8 / 360f) * 360f;
				num8 = Mathf.Clamp(num8, 0f, 360f);
				break;
			}
		}
		radarScenarioPose.Heading = num8;
		radarScenarioPose.Finished = flag;
		return radarScenarioPose;
	}

	public static float Visibility(RadarScenarioData data, RadarScenarioAircraft aircraft, RadarScenarioStation station, RadarScenarioPose pose)
	{
		if (data == null || aircraft == null || station == null)
		{
			return 0f;
		}
		float num = MathF.Atan2(station.Position.x - pose.Position.x, station.Position.y - pose.Position.y) * Mathf.Rad2Deg;
		float num2 = MathF.Abs(MathF.Cos((num - pose.Heading) * 0.017453292f)) * 0.3f + 0.7f;
		int num3 = Mathf.Clamp(aircraft.Coating, 0, CoatingVisibility.Length - 1);
		float num4;
		if (pose.Altitude < 500f)
		{
			num4 = 1f;
		}
		else if (pose.Altitude < 2000f)
		{
			num4 = 0.65f;
		}
		else if (pose.Altitude < 5000f)
		{
			num4 = 0.35f;
		}
		else
		{
			num4 = 0.15f;
		}
		RadarScenarioTerrainSample radarScenarioTerrainSample = RadarScenarioTerrain.Sample(pose.Position, data.Seed);
		return Mathf.Clamp01((1f - radarScenarioTerrainSample.Clutter01 * 0.42f * num4) * (CoatingVisibility[num3] * num2));
	}

	public static float Visibility(RadarScenarioData data, RadarScenarioAircraft aircraft, RadarScenarioStation station, RadarScenarioPose pose, RadarScenarioShapeProfile shape)
	{
		if (shape == null)
		{
			throw new ArgumentNullException("shape");
		}
		if (data == null || aircraft == null || station == null)
		{
			return 0f;
		}
		Vector2 vector = station.Position - pose.Position;
		float num = MathF.Atan2(vector.x, vector.y) * Mathf.Rad2Deg;
		float num2 = vector.magnitude;
		float num3 = Mathf.Max(1f, RadarScenarioGroundCatalog.CoverageRadius(station.PresetIndex));
		float num4 = Mathf.Clamp01(num2 / num3);
		float num5 = Mathf.Clamp01(pose.Altitude / 12000f);
		float num6 = Mathf.Clamp01(1f - num4);
		float num7 = (num6 * 0.75f + 0.25f) * num5;
		return Mathf.Clamp01(shape.Score(num - pose.Heading, num7) * Visibility(data, aircraft, station, pose));
	}
}
