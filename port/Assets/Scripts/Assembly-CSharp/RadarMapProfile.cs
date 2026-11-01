using System;
using System.Globalization;
using Unity.Mathematics;
using UnityEngine;

public static class RadarMapProfile
{
	public const double StepM = 1000.0;

	public const double TerrainDetailM = 6000.0;

	public static long DirectClosed;

	public static long HorizonClosed;

	public static long LegsClosed;

	public static long Evaluations;

	public static long ProfilePoints;

	public static bool Open(double startHeight, double endHeight, double length, Func<double, double> terrainAt, double step, out double worstClearance, out double worstAt, out int samples)
	{
		worstClearance = double.PositiveInfinity;
		worstAt = double.NaN;
		samples = 0;
		if (length <= step)
		{
			return true;
		}
		bool flag = true;
		for (int i = 1; i * step < length; i++)
		{
			double num = i * step;
			double num2 = (endHeight - startHeight) * num / length + startHeight - (length - num) * num / (2.0 * RadarMapUnits.EffectiveEarthRadius) - terrainAt(num);
			samples++;
			if (num2 < worstClearance)
			{
				worstClearance = num2;
				worstAt = num;
			}
			if (num2 < 0.0)
			{
				flag = false;
			}
		}
		ProfilePoints += samples;
		return flag;
	}

	public static RadarMapPath Evaluate(RadarMapWorld world, double3 antenna, double3 target, double frequencyHz, int polarization, double patternRatio)
	{
		RadarMapPath path = default(RadarMapPath);
		Evaluations++;
		double num = target.x - antenna.x;
		double num2 = target.y - antenna.y;
		double num3 = Math.Sqrt(num2 * num2 + num * num);
		double num4 = RadarMapGeo.Horizon(Math.Max(0.0, antenna.z), Math.Max(0.0, target.z));
		path.GroundRange = num3;
		path.RadarHeight = antenna.z;
		path.TargetHeight = target.z;
		path.HorizonM = num4;
		path.WorstClearance = double.PositiveInfinity;
		path.WorstAt = double.NaN;
		path.PatternRatio = patternRatio;
		if (num3 >= num4)
		{
			path.State = RadarMapPathState.Horizon;
			HorizonClosed++;
			return path;
		}
		double ux = (num3 > 0.0) ? (num / num3) : 0.0;
		double uy = (num3 > 0.0) ? (num2 / num3) : 0.0;
		Func<double, double> func = s => RadarMapWorld.HeightOf(world.Sample(antenna.x + s * ux, antenna.y + s * uy));
		if (!Open(antenna.z, target.z, num3, func, StepM, out path.WorstClearance, out path.WorstAt, out path.ProfileSamples))
		{
			path.State = RadarMapPathState.Terrain;
			DirectClosed++;
			return path;
		}
		path.State = RadarMapPathState.Open;
		path.F = 1.0;
		path.F4 = 1.0;
		if (!RadarMapPropagation.KerrBlake(num3, antenna.z, target.z, out _))
		{
			return path;
		}
		double num5 = world.Height(path.WorstAt * ux + antenna.x, path.WorstAt * uy + antenna.y);
		if (!RadarMapPropagation.KerrBlake(num3, antenna.z - num5, target.z - num5, out RadarMapTwoRay ray))
		{
			return path;
		}
		path.Ray = ray;
		path.HasRay = true;
		path.SurfaceLevel = num5;
		path.SpecularX = ray.RadarDistance * ux + antenna.x;
		path.SpecularY = ray.RadarDistance * uy + antenna.y;
		RadarScenarioTerrainSample sample = RadarScenarioTerrain.Sample(new Vector2((float)(path.SpecularX / RadarMapUnits.MetersPerMapUnit), (float)(path.SpecularY / RadarMapUnits.MetersPerMapUnit)), world.Seed, world.Coast);
		path.SurfaceKind = sample.Kind;
		path.Surface = RadarMapWorld.SurfaceOf(sample.Kind);
		if (path.Surface != RadarMapSurface.Diffuse)
		{
			Coefficients(ref path, frequencyHz, polarization, world.SeaState);
			bool flag = Open(antenna.z, num5, ray.RadarDistance, func, StepM, out double worstClearance, out double worstAt, out int samples);
			double start = ray.RadarDistance;
			bool flag2 = Open(num5, target.z, ray.TargetDistance, s => func(s + start), StepM, out worstClearance, out worstAt, out samples);
			if (flag && flag2)
			{
				Combine(ref path, frequencyHz, patternRatio);
			}
			else
			{
				path.ReflectionMasked = true;
				LegsClosed++;
			}
		}
		return path;
	}

	public static RadarMapPath EvaluateUniform(double groundRange, double radarHeight, double targetHeight, double frequencyHz, int polarization, RadarMapSurface surface, int seaState, double patternRatio)
	{
		RadarMapPath path = default(RadarMapPath);
		double num = RadarMapGeo.Horizon(Math.Max(0.0, radarHeight), Math.Max(0.0, targetHeight));
		path.GroundRange = groundRange;
		path.RadarHeight = radarHeight;
		path.TargetHeight = targetHeight;
		path.HorizonM = num;
		path.WorstClearance = double.PositiveInfinity;
		path.WorstAt = double.NaN;
		path.PatternRatio = patternRatio;
		path.Surface = surface;
		if (groundRange >= num)
		{
			path.State = RadarMapPathState.Horizon;
			return path;
		}
		path.State = RadarMapPathState.Open;
		path.F = 1.0;
		path.F4 = 1.0;
		if (RadarMapPropagation.KerrBlake(groundRange, radarHeight, targetHeight, out RadarMapTwoRay ray))
		{
			path.HasRay = true;
			path.Ray = ray;
			if (surface != RadarMapSurface.Diffuse)
			{
				Coefficients(ref path, frequencyHz, polarization, seaState);
				Combine(ref path, frequencyHz, patternRatio);
			}
		}
		return path;
	}

	private static void Coefficients(ref RadarMapPath path, double frequencyHz, int polarization, int seaState)
	{
		double num = RadarMapUnits.SpeedOfLight / frequencyHz;
		Complex64 eps = RadarMapPropagation.Permittivity(path.Surface, frequencyHz, out path.PermittivityClamped);
		RadarMapPropagation.Fresnel(eps, path.Ray.Grazing, out Complex64 gammaH, out Complex64 gammaV);
		path.Gamma = ((polarization == 1) ? gammaV : gammaH);
		path.Rho = RadarMapPropagation.Roughness(path.Surface, seaState, path.Ray.Grazing, num);
	}

	private static void Combine(ref RadarMapPath path, double frequencyHz, double patternRatio)
	{
		path.Reflection = true;
		Complex64 num = path.Gamma * (path.Rho * patternRatio);
		Complex64 num2 = Complex64.Phase(0.0 - Math.PI * 2.0 / (RadarMapUnits.SpeedOfLight / frequencyHz) * path.Ray.PathDifference);
		path.Reflected = num * num2;
		Complex64 num3 = new Complex64(1.0) + path.Reflected;
		double num4 = num3.Magnitude;
		path.F = num4;
		double num5 = num4 * num4;
		path.F4 = num5 * num5;
		path.MonopulseRatio = path.Gamma.Magnitude * path.Rho * patternRatio;
		path.Alpha = Math.Atan2(path.Reflected.Imaginary, path.Reflected.Real);
	}

	public static void ResetGuards()
	{
		ProfilePoints = 0L;
		Evaluations = 0L;
		LegsClosed = 0L;
		HorizonClosed = 0L;
		DirectClosed = 0L;
	}

	public static string GuardLine()
	{
		return string.Format(CultureInfo.InvariantCulture, "profile Δs {0:F0} m = {1:F3} × terrain detail {2:F0} m; paths {3}, closed by horizon {4}, by terrain {5}, reflected legs closed {6}, profile points {7}; specular points {8}, refusals {9}, arccos clamps {10}", StepM, StepM / TerrainDetailM, TerrainDetailM, Evaluations, HorizonClosed, DirectClosed, LegsClosed, ProfilePoints, RadarMapPropagation.SpecularPoints, RadarMapPropagation.SpecularRefusals, RadarMapPropagation.ArccosClamps);
	}
}
