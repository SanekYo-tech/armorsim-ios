using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

public sealed class RadarMapSession
{
	private struct ScanEvent
	{
		public double Time;

		public int Radar;

		public int Kind;

		public int Beam;

		public int Plane;

		public long Scan;
	}

	public sealed class Plane
	{
		public RadarScenarioAircraft Source;

		public RadarMapFlight Flight;

		public string ModelKey;

		public string ModelName;

		public RadarModel Model;

		public RadarScene Scene;

		public RadarAtlas[] Atlases;

		public RadarCoating Coating;
	}

	public sealed class MissingAtlas
	{
		public string ModelKey;

		public string ModelName;

		public RadarScene Scene;

		public double FrequencyHz;

		public RadarMapModelRecord Record;

		public RadarCoating Coating;
	}

	public sealed class Radar
	{
		public RadarScenarioStation Source;

		public RadarMapStationSpec Spec;

		public double3 Antenna;

		public double MastM;

		public RadarMapScanner Scanner;

		public bool Emitting;

		public double Pfa;

		public readonly List<RadarPlot> Plots;

		internal readonly Dictionary<int, RadarPlotTruth> Truths;

		internal readonly List<RadarMapOpportunity> Opportunities;

		public long FalseAlarms;

		public long TargetPlots;

		public long OpportunityCount;

		public long FalseAlarmLimitHits;

		public double SiteHeightM;

		public bool LowAltitude;
	}

	public const double PlotMemorySeconds = 120.0;

	public const double OpportunityMemorySeconds = 60.0;

	private const long KindTarget = 1L;

	private const long KindFalse = 2L;

	private readonly List<ScanEvent> events;

	private readonly List<(double time, long scan)> crossings;

	private readonly List<(double time, long scan, int beam)> beamEvents;

	private int nextPlotId;

	private bool detectionReady;

	public Func<int, bool> IsDestroyed;

	public long GateHorizon;

	public long GateRange;

	public long GateElevation;

	public long GateEmission;

	public long GateMasked;

	public long MultipathPlots;

	public long LowAnglePlots;

	private readonly Dictionary<(int, long), int> falseCounts;

	private readonly List<Plane> planes;

	private readonly List<Radar> radars;

	public ulong Seed => (ulong)Data.Seed;

	public IReadOnlyList<Plane> Planes => planes;

	public IReadOnlyList<Radar> Radars => radars;

	public RadarScenarioData Data { get; private set; }

	public RadarMapWorld World { get; private set; }

	public double Time { get; internal set; }

	public void EnableDetection()
	{
		detectionReady = true;
		ResetDetection();
	}

	internal void ResetDetection()
	{
		nextPlotId = 1;
		GateHorizon = 0L;
		GateRange = 0L;
		GateElevation = 0L;
		GateEmission = 0L;
		GateMasked = 0L;
		MultipathPlots = 0L;
		LowAnglePlots = 0L;
		foreach (Radar radar in radars)
		{
			radar.Scanner = new RadarMapScanner(radar.Spec, radar.Source.SectorAzimuthDeg);
			radar.Plots.Clear();
			radar.Truths.Clear();
			radar.Opportunities.Clear();
			radar.FalseAlarms = 0L;
			radar.TargetPlots = 0L;
			radar.OpportunityCount = 0L;
			radar.FalseAlarmLimitHits = 0L;
		}
		falseCounts.Clear();
	}

	private double BearingFromStation(int plane, int radar, double t)
	{
		RadarMapPose radarMapPose = Pose(plane, t);
		Radar radar2 = radars[radar];
		return RadarMapScanner.Wrap360(Math.Atan2(radarMapPose.Position.x - radar2.Antenna.x, radarMapPose.Position.y - radar2.Antenna.y) * 57.29577951308232);
	}

	private void ProcessDetections(double t0, double t1)
	{
		if (!detectionReady)
		{
			Prune(t1);
			return;
		}
		events.Clear();
		for (int i = 0; i < radars.Count; i++)
		{
			Radar radar = radars[i];
			radar.Scanner.BeamEvents(t0, t1, beamEvents);
			foreach (var beamEvent in beamEvents)
			{
				events.Add(new ScanEvent
				{
					Time = beamEvent.time,
					Radar = i,
					Beam = beamEvent.beam,
					Plane = -1,
					Scan = beamEvent.scan
				});
			}
			if (radar.Scanner.Spec.Scan != RadarMapScan.Rotating)
			{
				continue;
			}
			for (int j = 0; j < planes.Count; j++)
			{
				Func<double, double> bearingAt = delegate(double t)
				{
					return BearingFromStation(j, i, t);
				};
				radar.Scanner.RotatingCrossings(t0, t1, bearingAt, crossings);
				foreach (var crossing in crossings)
				{
					events.Add(new ScanEvent
					{
						Time = crossing.time,
						Radar = i,
						Kind = (int)KindTarget,
						Beam = -1,
						Plane = j,
						Scan = crossing.scan
					});
				}
			}
		}
		events.Sort(delegate(ScanEvent a, ScanEvent b)
		{
			int num = a.Time.CompareTo(b.Time);
			if (num != 0)
			{
				return num;
			}
			num = a.Radar.CompareTo(b.Radar);
			if (num != 0)
			{
				return num;
			}
			num = a.Kind.CompareTo(b.Kind);
			if (num != 0)
			{
				return num;
			}
			num = a.Beam.CompareTo(b.Beam);
			if (num != 0)
			{
				return num;
			}
			return a.Plane.CompareTo(b.Plane);
		});
		foreach (ScanEvent scanEvent in events)
		{
			if (scanEvent.Kind == KindTarget)
			{
				Opportunity(scanEvent.Radar, scanEvent.Plane, scanEvent.Time, scanEvent.Scan, -1);
			}
			else
			{
				FalseAlarms(scanEvent.Radar, scanEvent.Time, scanEvent.Scan, scanEvent.Beam);
				Radar radar2 = radars[scanEvent.Radar];
				if (radar2.Spec.Scan == RadarMapScan.Sector)
				{
					for (int k = 0; k < planes.Count; k++)
					{
						if (radar2.Scanner.SectorBeamOf(BearingFromStation(k, scanEvent.Radar, scanEvent.Time)) == scanEvent.Beam)
						{
							Opportunity(scanEvent.Radar, k, scanEvent.Time, scanEvent.Scan, scanEvent.Beam);
						}
					}
				}
			}
		}
		Prune(t1);
	}

	public ulong OpportunityState(int radar, long scan, int beam, int aircraftId)
	{
		return RadarMapRng.Key((ulong)Data.Seed, radars[radar].Source.Id, scan, beam, aircraftId, 1L);
	}

	private void Opportunity(int r, int p, double t, long scan, int beam)
	{
		Radar radar = radars[r];
		Plane plane = planes[p];
		RadarMapPose radarMapPose = Pose(p, t);
		if (radarMapPose.Finished)
		{
			return;
		}
		if (IsDestroyed != null && IsDestroyed(plane.Source.Id))
		{
			return;
		}
		if (!radar.Emitting)
		{
			GateEmission++;
			return;
		}
		RadarAspect aspect = RadarMapFrames.Aspect(radarMapPose, radar.Antenna);
		if (World == null)
		{
			if (aspect.Horizon <= aspect.GroundRange)
			{
				GateHorizon++;
				return;
			}
		}
		if (aspect.SlantRange > radar.Spec.InstrumentedRangeM)
		{
			GateRange++;
			return;
		}
		double num = aspect.ElevationAtRadar * 57.29577951308232;
		if (num < ElevationMin(radar) || num > radar.Spec.ElevationMaxDeg)
		{
			GateElevation++;
			return;
		}
		RadarAtlas radarAtlas = plane.Atlases?[r];
		if (radarAtlas == null)
		{
			return;
		}
		double num2 = 1.0;
		RadarMapPath path = default(RadarMapPath);
		if (World != null)
		{
			path = PathAt(r, radarMapPose);
			if (path.State == RadarMapPathState.Horizon)
			{
				GateHorizon++;
				return;
			}
			if (path.State == RadarMapPathState.Terrain)
			{
				GateMasked++;
				return;
			}
			num2 = path.F4;
		}
		double num3 = radarAtlas.SigmaMean(aspect.Azimuth, aspect.Elevation, aspect.Xi, (RadarAtlasPair)(radar.Source.Polarization == 1 ? 1 : 0));
		int num4 = RadarMapDetector.Pulses(radar.Spec, radar.Scanner.Integration);
		double num5 = LowAltitudeLoss(radar, num) * (double)num4 * RadarMapDetector.SinglePulseSnr(radar.Spec, aspect.SlantRange, 1.0, num2);
		ulong state = OpportunityState(r, scan, beam, plane.Source.Id);
		double threshold = RadarMapDetector.Threshold(radar.Pfa);
		bool flag = RadarMapDetector.Detect(num3, num5, threshold, ref state, out var sigmaDraw, out var snr, out var power);
		radar.OpportunityCount++;
		radar.Opportunities.Add(new RadarMapOpportunity(t, plane.Source.Id, r, num3, num5 * num3, snr, num2, flag));
		if (!flag)
		{
			return;
		}
		RadarMapDetector.Accuracy(radar.Spec, snr, out var rangeSigmaM, out var bearingSigmaDeg, out var elevationSigmaDeg);
		RadarMapDetector.Normals(ref state, out var a, out var b);
		RadarMapDetector.Normals(ref state, out var a2, out var b2);
		double num6 = aspect.SlantRange + a * rangeSigmaM;
		double num7 = RadarMapScanner.Wrap360(BearingFromStation(p, r, t) + b * bearingSigmaDeg);
		double num8 = double.NaN;
		double num9 = double.NaN;
		bool flag2 = false;
		if (radar.Spec.Is3D)
		{
			double num10 = 0.0;
			if (World != null && path.Reflection)
			{
				RadarMapMonopulse.Result result = RadarMapMonopulse.Evaluate(path, radar.Spec.BeamElevationDeg);
				num10 = result.Error * 57.29577951308232;
				MultipathPlots++;
				flag2 = result.Ratio > 0.5;
				if (flag2)
				{
					LowAnglePlots++;
				}
			}
			num8 = num10 + num + a2 * elevationSigmaDeg;
			num9 = RadarMapGeo.TargetHeight(num6, num8 * 0.017453292519943295, radar.MastM);
		}
		int num11 = nextPlotId++;
		double num12 = threshold - Math.Log(RadarMapDetector.OpenUnit(ref state)) - 1.0;
		RadarPlot item = new RadarPlot(num11, radar.Source.Id, scan, t, num6, num7, num8, num9, RadarMapDetector.Db(num12), RadarMapDetector.SigmaHat(radar.Spec, radar.Scanner.Integration, num12, num6), flag2);
		radar.Plots.Add(item);
		RadarPlotTruth radarPlotTruth = new RadarPlotTruth();
		radarPlotTruth.PlotId = item.PlotId;
		radarPlotTruth.FalseAlarm = false;
		radarPlotTruth.AircraftId = plane.Source.Id;
		radarPlotTruth.SigmaBarM2 = num3;
		radarPlotTruth.SigmaDrawM2 = sigmaDraw;
		radarPlotTruth.Snr = snr;
		radarPlotTruth.F4 = num2;
		radarPlotTruth.Aspect = aspect;
		radarPlotTruth.Path = path;
		radarPlotTruth.TrueHeightM = radarMapPose.Position.z;
		radar.Truths[item.PlotId] = radarPlotTruth;
		radar.TargetPlots++;
	}

	private void FalseAlarms(int r, double t, long scan, int beam)
	{
		Radar radar = radars[r];
		ulong state = RadarMapRng.Key((ulong)Data.Seed, radar.Source.Id, scan, beam, 0L, KindFalse);
		int num = RadarMapDetector.Poisson(RadarMapDetector.FalseAlarmsPerBeam(radar.Spec, radar.Pfa), ref state);
		if (num == 0)
		{
			return;
		}
		falseCounts.TryGetValue((r, scan), out var value);
		int num2 = Math.Max(0, RadarMapDetector.FalseAlarmLimitPerScan - value);
		if (num2 < num)
		{
			radar.FalseAlarmLimitHits++;
			num = num2;
		}
		falseCounts[(r, scan)] = value + num;
		int num3 = (int)Math.Floor(radar.Spec.InstrumentedRangeM / radar.Spec.RangeCellM + 1E-09);
		double num4 = RadarMapDetector.Threshold(radar.Pfa);
		for (int i = 0; i < num; i++)
		{
			int num5 = Math.Min(num3 - 1, (int)Math.Floor(RadarMapRng.NextUnit(ref state) * (double)num3));
			double num6 = ((double)num5 + 0.5) * radar.Spec.RangeCellM;
			double num7 = RadarMapScanner.Wrap360((RadarMapRng.NextUnit(ref state) - 0.5) * radar.Spec.BeamAzimuthDeg + radar.Scanner.BeamCentre(beam));
			double num8 = num4 - Math.Log(RadarMapDetector.OpenUnit(ref state)) - 1.0;
			double num9 = double.NaN;
			double num10 = double.NaN;
			if (radar.Spec.Is3D)
			{
				num9 = RadarMapRng.NextUnit(ref state) * (radar.Spec.ElevationMaxDeg - radar.Spec.ElevationMinDeg) + radar.Spec.ElevationMinDeg;
				num10 = RadarMapGeo.TargetHeight(num6, num9 * 0.017453292519943295, radar.MastM);
			}
			int num11 = nextPlotId++;
			RadarPlot item = new RadarPlot(num11, radar.Source.Id, scan, t, num6, num7, num9, num10, RadarMapDetector.Db(num8), RadarMapDetector.SigmaHat(radar.Spec, radar.Scanner.Integration, num8, num6));
			radar.Plots.Add(item);
			RadarPlotTruth radarPlotTruth = new RadarPlotTruth();
			radarPlotTruth.PlotId = item.PlotId;
			radarPlotTruth.FalseAlarm = true;
			radarPlotTruth.AircraftId = 0;
			radarPlotTruth.Snr = num8;
			radar.Truths[item.PlotId] = radarPlotTruth;
			radar.FalseAlarms++;
		}
	}

	private void Prune(double now)
	{
		foreach (Radar radar in radars)
		{
			int num = 0;
			while (num < radar.Plots.Count && radar.Plots[num].Time < now - PlotMemorySeconds)
			{
				radar.Truths.Remove(radar.Plots[num].PlotId);
				num++;
			}
			if (num > 0)
			{
				radar.Plots.RemoveRange(0, num);
			}
			int num2 = 0;
			while (num2 < radar.Opportunities.Count && radar.Opportunities[num2].Time < now - OpportunityMemorySeconds)
			{
				num2++;
			}
			if (num2 > 0)
			{
				radar.Opportunities.RemoveRange(0, num2);
			}
			if (falseCounts.Count > 4096)
			{
				falseCounts.Clear();
			}
		}
	}

	public int JudgeAircraft(int radar, int plotId)
	{
		if (radars[radar].Truths.TryGetValue(plotId, out var value) && !value.FalseAlarm)
		{
			return value.AircraftId;
		}
		return 0;
	}

	public RadarPlotTruth TruthOf(int radar, int plotId)
	{
		radars[radar].Truths.TryGetValue(plotId, out var value);
		return value;
	}

	public void OpportunitiesOf(int radar, int aircraftId, List<RadarMapOpportunity> output)
	{
		output.Clear();
		foreach (RadarMapOpportunity item in radars[radar].Opportunities)
		{
			if (item.AircraftId == aircraftId)
			{
				output.Add(item);
			}
		}
	}

	public double MeanSnr(int plane, int radar, out double pd, out double sigmaBar, out double f4)
	{
		RadarMapPose pose = planes[plane].Flight.Pose(Time);
		Radar radar2 = radars[radar];
		RadarAspect aspect = RadarMapFrames.Aspect(pose, radar2.Antenna);
		sigmaBar = SigmaMean(plane, radar, out var aspect2);
		f4 = 1.0;
		if (World == null)
		{
			if (aspect.Horizon <= aspect.GroundRange)
			{
				pd = 0.0;
				f4 = 0.0;
				return 0.0;
			}
		}
		else
		{
			RadarMapPath radarMapPath = PathAt(radar, pose);
			if (radarMapPath.State != RadarMapPathState.Open)
			{
				pd = 0.0;
				f4 = 0.0;
				return 0.0;
			}
			f4 = radarMapPath.F4;
		}
		int num = RadarMapDetector.Pulses(radar2.Spec, radar2.Scanner.Integration);
		double num2 = (double)num * sigmaBar * RadarMapDetector.SinglePulseSnr(radar2.Spec, aspect.SlantRange, 1.0, f4);
		double num3 = LowAltitudeLoss(radar2, aspect.ElevationAtRadar * 57.29577951308232) * num2;
		pd = RadarMapDetector.ProbabilityOfDetection(num3, radar2.Pfa);
		return num3;
	}

	public static double PatternRatio(Radar radar)
	{
		if (radar.LowAltitude && radar.Spec.Is3D)
		{
			return 0.3;
		}
		return 1.0;
	}

	public static double ElevationMin(Radar radar)
	{
		if (radar.LowAltitude && radar.Spec.Is3D)
		{
			return Math.Max(radar.Spec.ElevationMinDeg, 0.2);
		}
		return radar.Spec.ElevationMinDeg;
	}

	public static double LowAltitudeLoss(Radar radar, double elevationDeg)
	{
		if (radar.LowAltitude && radar.Spec.Is3D && elevationDeg < 0.5)
		{
			return Math.Pow(10.0, -0.3);
		}
		return 1.0;
	}

	public RadarMapPath PathAt(int radar, RadarMapPose pose)
	{
		Radar radar2 = radars[radar];
		if (World == null)
		{
			return new RadarMapPath
			{
				State = RadarMapPathState.Open,
				F = 1.0,
				F4 = 1.0
			};
		}
		return RadarMapProfile.Evaluate(World, radar2.Antenna, pose.Position, radar2.Spec.FrequencyHz, radar2.Source.Polarization, PatternRatio(radar2));
	}

	public RadarMapPath PathOf(int plane, int radar)
	{
		RadarMapPose pose = planes[plane].Flight.Pose(Time);
		return PathAt(radar, pose);
	}

	public string DetectionGuardLine()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Radar radar in radars)
		{
			double num = radar.Scanner.Beams * RadarMapDetector.FalseAlarmsPerBeam(radar.Spec, radar.Pfa);
			stringBuilder.Append(radar.Spec.Id).Append(": opportunities ").Append(radar.OpportunityCount).Append(", target plots ").Append(radar.TargetPlots).Append(", false plots ").Append(radar.FalseAlarms).Append(", FA limit ").Append(RadarMapDetector.FalseAlarmLimitPerScan).Append(" = ").Append((RadarMapDetector.FalseAlarmLimitPerScan / Math.Max(1E-12, num)).ToString("E2", CultureInfo.InvariantCulture)).Append("×λ_FA per scan, hits ").Append(radar.FalseAlarmLimitHits).Append(", crossing residual max ").Append(radar.Scanner.MaxCrossingResidualDeg.ToString("E2", CultureInfo.InvariantCulture)).Append("°; ");
		}
		stringBuilder.Append("gates: horizon ").Append(GateHorizon).Append(", range ").Append(GateRange).Append(", elevation ").Append(GateElevation).Append(", emission ").Append(GateEmission).Append(", masked ").Append(GateMasked);
		if (World != null)
		{
			stringBuilder.Append("; M3: multipath plots ").Append(MultipathPlots).Append(", low-angle ").Append(LowAnglePlots).Append(", monopulse clamps ").Append(RadarMapMonopulse.Clamps).Append("; ").Append(RadarMapProfile.GuardLine());
		}
		return stringBuilder.ToString();
	}

	private RadarMapSession()
	{
		events = new List<ScanEvent>();
		crossings = new List<(double time, long scan)>();
		beamEvents = new List<(double time, long scan, int beam)>();
		nextPlotId = 1;
		falseCounts = new Dictionary<(int, long), int>();
		planes = new List<Plane>();
		radars = new List<Radar>();
	}

	public static RadarMapSession Create(RadarScenarioData data, RadarMapModelRegistry registry, List<string> reasons)
	{
		return Create(data, registry, reasons, world: false);
	}

	public static RadarMapSession Create(RadarScenarioData data, RadarMapModelRegistry registry, List<string> reasons, bool world)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (registry == null)
		{
			throw new ArgumentNullException("registry");
		}
		if (reasons == null)
		{
			throw new ArgumentNullException("reasons");
		}
		if (!data.Validate(out string reason))
		{
			reasons.Add(RadarScenarioText.Get("shell.invalid") + " " + reason);
			return null;
		}
		RadarScenarioData radarScenarioData = JsonUtility.FromJson<RadarScenarioData>(JsonUtility.ToJson(data));
		RadarMapSession radarMapSession = new RadarMapSession();
		radarMapSession.Data = radarScenarioData;
		radarMapSession.World = (world ? RadarMapWorld.Of(radarScenarioData) : null);
		Func<double, double, double> func = ((radarMapSession.World != null) ? new Func<double, double, double>(radarMapSession.World.Height) : null);
		Dictionary<string, RadarModel> dictionary = new Dictionary<string, RadarModel>();
		foreach (RadarScenarioAircraft item in radarScenarioData.Aircraft)
		{
			RadarModel model;
			if (!dictionary.TryGetValue(item.ModelKey, out model))
			{
				string refusalKey;
				if (!registry.TryBuildModel(item.ModelKey, out model, out refusalKey))
				{
					reasons.Add(Format(refusalKey, item.ModelName));
					dictionary.Add(item.ModelKey, null);
					continue;
				}
				dictionary.Add(item.ModelKey, model);
			}
			if (model != null)
			{
				Plane plane = new Plane();
				plane.Source = item;
				plane.Flight = new RadarMapFlight(item, func);
				plane.ModelKey = item.ModelKey;
				plane.ModelName = item.ModelName;
				plane.Model = model;
				radarMapSession.planes.Add(plane);
			}
		}
		foreach (RadarScenarioStation item2 in radarScenarioData.Stations)
		{
			if (item2.IsDefencePost)
			{
				continue;
			}
			if (!RadarMapStations.TryResolve(item2, out RadarMapStationSpec spec, out string refusalKey2))
			{
				reasons.Add(Format(refusalKey2, RadarMapStations.IdFor(item2)));
			}
			else
			{
				double num = ((item2.MastHeightM < 0f) ? spec.MastM : item2.MastHeightM);
				double num2 = (double)item2.Position.x * RadarMapUnits.MetersPerMapUnit;
				double num3 = (double)item2.Position.y * RadarMapUnits.MetersPerMapUnit;
				double num4 = ((radarMapSession.World != null) ? radarMapSession.World.Height(num2, num3) : 0.0);
				Radar radar = new Radar();
				radar.Source = item2;
				radar.Spec = spec;
				radar.MastM = num;
				radar.SiteHeightM = num4;
				radar.Antenna = new double3(num2, num3, num + num4);
				radarMapSession.radars.Add(radar);
			}
		}
		if (radarMapSession.radars.Count == 0 && reasons.Count == 0)
		{
			reasons.Add(RadarScenarioText.Get("map.phys.refuse.noRadar"));
		}
		return (reasons.Count == 0) ? radarMapSession : null;
	}

	public static RadarMapSession Create(RadarScenarioData data, RadarMapModelRegistry registry, RadarAtlasLibrary atlases, List<string> reasons, List<MissingAtlas> missing)
	{
		RadarMapSession radarMapSession = Create(data, registry, reasons, world: true);
		if (radarMapSession == null || atlases == null)
		{
			return radarMapSession;
		}
		Dictionary<string, RadarScene> dictionary = new Dictionary<string, RadarScene>();
		HashSet<string> hashSet = new HashSet<string>();
		foreach (Plane plane in radarMapSession.planes)
		{
			RadarScene radarScene;
			if (!dictionary.TryGetValue(plane.ModelKey, out radarScene))
			{
				radarScene = new RadarScene(plane.Model);
				dictionary.Add(plane.ModelKey, radarScene);
			}
			plane.Scene = radarScene;
			plane.Atlases = new RadarAtlas[radarMapSession.radars.Count];
			RadarCoating radarCoating = RadarMapCoatings.FromAircraft(plane.Source, out string refusalReason);
			plane.Coating = radarCoating;
			if (radarCoating == null)
			{
				reasons.Add(string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.coat.range"), refusalReason) + " — " + plane.Source.Name);
				continue;
			}
			for (int i = 0; i < radarMapSession.radars.Count; i++)
			{
				Radar radar = radarMapSession.radars[i];
				double num = radarScene.LongestSide / (299792458.0 / radar.Spec.FrequencyHz);
				if (num < RadarAtlasSpec.RefuseBelowWavelengths)
				{
					reasons.Add(string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.phys.refuse.atlas"), num.ToString("F2", CultureInfo.InvariantCulture)) + " — " + plane.ModelName + " · " + radar.Spec.Id);
					continue;
				}
				RadarAtlas radarAtlas = atlases.TryGet(plane.ModelKey, radarScene, radar.Spec.FrequencyHz, RadarAtlasSpec.Standard, out string key, plane.Coating);
				if (radarAtlas == null)
				{
					if (atlases.LastError != null)
					{
						reasons.Add(RadarScenarioText.Get("map.atlas.title") + ": " + atlases.LastError);
					}
					if (hashSet.Add(key))
					{
						reasons.Add(string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get("map.phys.bake.offer"), plane.ModelName + (plane.Coating.IsMetal ? "" : (" · " + RadarMapCoatings.Name(plane.Coating))), RadarBands.FormatFrequency(radar.Spec.FrequencyHz, Localization.T("radar.freq.mhz"), Localization.T("radar.freq.ghz")), ""));
						MissingAtlas missingAtlas = new MissingAtlas();
						missingAtlas.ModelKey = plane.ModelKey;
						missingAtlas.ModelName = plane.ModelName;
						missingAtlas.Scene = radarScene;
						missingAtlas.FrequencyHz = radar.Spec.FrequencyHz;
						missingAtlas.Record = registry.Find(plane.ModelKey);
						missingAtlas.Coating = plane.Coating;
						missing.Add(missingAtlas);
					}
					continue;
				}
				plane.Atlases[i] = radarAtlas;
			}
		}
		if (reasons.Count > 0)
		{
			return null;
		}
		radarMapSession.EnableDetection();
		return radarMapSession;
	}

	public double SigmaMean(int plane, int radar, out RadarAspect aspect)
	{
		RadarMapPose pose = planes[plane].Flight.Pose(Time);
		Radar radar2 = radars[radar];
		aspect = RadarMapFrames.Aspect(pose, radar2.Antenna);
		RadarAtlas radarAtlas = planes[plane].Atlases[radar];
		if (radarAtlas == null)
		{
			return 0.0;
		}
		return radarAtlas.SigmaMean(aspect.Azimuth, aspect.Elevation, aspect.Xi, (RadarAtlasPair)(radars[radar].Source.Polarization == 1 ? 1 : 0));
	}

	private static string Format(string key, string argument)
	{
		return string.Format(CultureInfo.InvariantCulture, RadarScenarioText.Get(key), argument ?? "?");
	}

	public void Advance(double gameSeconds)
	{
		if (gameSeconds > 0.0 && !double.IsInfinity(gameSeconds))
		{
			double num = Time;
			Time += gameSeconds;
			ProcessDetections(num, Time);
		}
	}

	public void Restart()
	{
		Time = 0.0;
		ResetDetection();
	}

	public RadarMapPose Pose(int plane)
	{
		return planes[plane].Flight.Pose(Time);
	}

	public RadarMapPose Pose(int plane, double time)
	{
		return planes[plane].Flight.Pose(time);
	}

	public RadarAspect Aspect(int plane, int radar)
	{
		return RadarMapFrames.Aspect(planes[plane].Flight.Pose(Time), radars[radar].Antenna);
	}

	public RadarAspect Aspect(int plane, int radar, double time)
	{
		return RadarMapFrames.Aspect(Pose(plane, time), radars[radar].Antenna);
	}

	public string GuardLine()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Plane plane in planes)
		{
			RadarMapFlight flight = plane.Flight;
			double num = ((flight.Captures != 0) ? ((double)flight.Overshoots / (double)flight.Captures) : 0.0);
			stringBuilder.Append(plane.Source.Name).Append(": overshoots ").Append(flight.Overshoots).Append('/').Append(flight.Captures).Append(" (").Append(num.ToString("F3", CultureInfo.InvariantCulture)).Append(" of captures, ").Append(flight.WaypointCount).Append(" waypoints), 4h limit ").Append(flight.TimeLimitReached ? "HIT" : "not hit");
			if (World != null)
			{
				stringBuilder.Append(", floor lifts ").Append(flight.FloorClamps).Append(" steps (max ").Append(flight.MaxFloorLift.ToString("F1", CultureInfo.InvariantCulture)).Append(" m)");
			}
			stringBuilder.Append("; ");
		}
		return stringBuilder.ToString();
	}

	public bool AllFinished()
	{
		foreach (Plane item in planes)
		{
			if (!planes[planes.IndexOf(item)].Flight.Pose(Time).Finished)
			{
				return false;
			}
		}
		return planes.Count > 0;
	}
}
