using System;
using System.Collections.Generic;
using Unity.Mathematics;

public sealed class RadarMapFlight
{
	private struct Node
	{
		public double X;

		public double Y;

		public double H;

		public double Psi;

		public double Theta;

		public double Phi;
	}

	public const double StepSeconds = 0.05;

	public const double HeadingGain = 1.0;

	public const double MaxClimbRad = Math.PI / 12.0;

	public const double MinCaptureRadius = 200.0;

	public const double MaxLeadTurnRad = Math.PI * 5.0 / 6.0;

	public const double OpenRouteLimitSeconds = 14400.0;

	public const double ClosedChunkSeconds = 60.0;

	public const double DuplicateWaypointMeters = 1.0;

	public const double LargeTurnRad = Math.PI / 2.0;

	private readonly List<Node> nodes;

	private readonly double[] wx;

	private readonly double[] wy;

	private readonly double[] wh;

	private readonly bool loop;

	private readonly int count;

	private readonly double v;

	private readonly double phiMax;

	private readonly double rollRate;

	private readonly Func<double, double, double> floor;

	private double x;

	private double y;

	private double h;

	private double psi;

	private double phi;

	private double theta;

	private int target;

	private bool finished;

	public int Overshoots { get; private set; }

	public int Captures { get; private set; }

	public bool TimeLimitReached { get; private set; }

	public double EndTime { get; private set; }

	public bool Loop => loop;

	public int WaypointCount => count;

	public double Speed => v;

	public double MaxBank => phiMax;

	public double TurnRadius => v * v / (Math.Tan(phiMax) * 9.80665);

	public int NodeCount => nodes.Count;

	public double SimulatedSeconds => (nodes.Count - 1) * 0.05;

	public long FloorClamps { get; private set; }

	public double MaxFloorLift { get; private set; }

	public RadarMapFlight(RadarScenarioAircraft aircraft)
		: this(aircraft, null)
	{
	}

	public RadarMapFlight(RadarScenarioAircraft aircraft, Func<double, double, double> floorAt)
	{
		nodes = new List<Node>(4096);
		EndTime = double.PositiveInfinity;
		floor = floorAt;
		if (aircraft == null || aircraft.Route == null)
		{
			throw new ArgumentNullException("aircraft");
		}
		v = aircraft.SpeedMps;
		phiMax = (double)aircraft.MaxBankDeg * (Math.PI / 180.0);
		rollRate = (double)aircraft.RollRateDegPerSec * (Math.PI / 180.0);
		if (v <= 0.0 || phiMax <= 0.0 || phiMax >= Math.PI / 2.0 || rollRate <= 0.0)
		{
			throw new ArgumentOutOfRangeException("aircraft", "Physical flight fields are not set.");
		}
		loop = aircraft.Loop;
		List<double> list = new List<double>();
		List<double> list2 = new List<double>();
		List<double> list3 = new List<double>();
		foreach (RadarScenarioWaypoint item in aircraft.Route)
		{
			double num = RadarMapUnits.MapToMeters(item.Position.x);
			double num2 = RadarMapUnits.MapToMeters(item.Position.y);
			if (list.Count > 0 && Hypot(num - list[list.Count - 1], num2 - list2[list2.Count - 1]) < 1.0)
			{
				list3[list3.Count - 1] = item.Altitude;
			}
			else
			{
				list.Add(num);
				list2.Add(num2);
				list3.Add(item.Altitude);
			}
		}
		if (loop && list.Count > 2 && Hypot(list[0] - list[list.Count - 1], list2[0] - list2[list2.Count - 1]) < 1.0)
		{
			list.RemoveAt(list.Count - 1);
			list2.RemoveAt(list2.Count - 1);
			list3.RemoveAt(list3.Count - 1);
		}
		wx = list.ToArray();
		wy = list2.ToArray();
		wh = list3.ToArray();
		count = wx.Length;
		if (count < 2)
		{
			throw new ArgumentException("A physical route needs two distinct waypoints.", "aircraft");
		}
		x = wx[0];
		y = wy[0];
		h = wh[0];
		psi = RadarMapUnits.Wrap2Pi(Math.Atan2(wx[1] - x, wy[1] - y));
		phi = 0.0;
		theta = 0.0;
		target = 1;
		if (floor != null)
		{
			KeepFloor(h, 0.0);
		}
		Record();
		if (loop)
		{
			EnsureUntil(60.0);
			return;
		}
		long num3 = (long)Math.Round(14400.0 / 0.05);
		while (!finished && nodes.Count - 1 < num3)
		{
			Step();
		}
		if (!finished)
		{
			TimeLimitReached = true;
			EndTime = SimulatedSeconds;
		}
	}

	public RadarMapPose Pose(double time)
	{
		if (double.IsNaN(time) || time < 0.0)
		{
			time = 0.0;
		}
		EnsureUntil(time + 0.05);
		if (time >= EndTime)
		{
			return Make(nodes.Count - 1, 0.0, EndTime, true);
		}
		double num = time / 0.05;
		int num2 = (int)Math.Floor(num);
		if (num2 < nodes.Count - 1)
		{
			return Make(num2, num - num2, time, false);
		}
		return Make(nodes.Count - 1, 0.0, time, !loop);
	}

	public void EnsureUntil(double time)
	{
		if (!loop || double.IsInfinity(time))
		{
			return;
		}
		while (time > SimulatedSeconds)
		{
			long num = nodes.Count - 1 + (long)Math.Round(60.0 / 0.05);
			while (nodes.Count - 1 < num)
			{
				Step();
			}
		}
	}

	private RadarMapPose Make(int index, double fraction, double time, bool ended)
	{
		Node node = nodes[index];
		Node node2 = ((index + 1 < nodes.Count) ? nodes[index + 1] : node);
		return new RadarMapPose
		{
			Position = new double3(node.X + (node2.X - node.X) * fraction, node.Y + (node2.Y - node.Y) * fraction, node.H + (node2.H - node.H) * fraction),
			Heading = RadarMapUnits.Wrap2Pi(node.Psi + RadarMapUnits.WrapPi(node2.Psi - node.Psi) * fraction),
			Pitch = node.Theta + (node2.Theta - node.Theta) * fraction,
			Bank = node.Phi + (node2.Phi - node.Phi) * fraction,
			Speed = v,
			Time = time,
			Finished = ended
		};
	}

	private double CaptureRadius(int k)
	{
		double num = 0.0;
		if (loop || k != count - 1)
		{
			int num2 = (count - 1 + k) % count;
			int num3 = (k + 1) % count;
			double num4 = Math.Atan2(wx[k] - wx[num2], wy[k] - wy[num2]);
			double num5 = Math.Atan2(wx[num3] - wx[k], wy[num3] - wy[k]);
			num = Math.Abs(RadarMapUnits.WrapPi(num4 - num5));
		}
		return Math.Max(200.0, TurnRadius * Math.Tan(Math.Min(num, Math.PI * 5.0 / 6.0) * 0.5));
	}

	private void Step()
	{
		if (finished)
		{
			return;
		}
		double num = wx[target] - x;
		double num2 = wy[target] - y;
		double num3 = Hypot(num, num2);
		double num4 = RadarMapUnits.WrapPi(Math.Atan2(num, num2) - psi);
		double num5 = CaptureRadius(target);
		bool flag = num3 <= num5;
		bool flag2 = Math.Abs(num4) > Math.PI / 2.0 && 2.0 * TurnRadius > num3;
		if (flag || flag2)
		{
			if (!flag && flag2)
			{
				Overshoots++;
			}
			Captures++;
			if (!loop && target == count - 1)
			{
				finished = true;
				EndTime = SimulatedSeconds;
				return;
			}
			target = (target + 1) % count;
			num = wx[target] - x;
			num2 = wy[target] - y;
			num3 = Hypot(num, num2);
			num4 = RadarMapUnits.WrapPi(Math.Atan2(num, num2) - psi);
		}
		double limit = Math.Tan(phiMax) * 9.80665 / v;
		double num6 = Math.Atan(Clamp(num4, limit) * v / 9.80665);
		double limit2 = rollRate * 0.05;
		phi += Clamp(num6 - phi, limit2);
		psi = RadarMapUnits.Wrap2Pi(psi + Math.Tan(phi) * 9.80665 / v * 0.05);
		theta = Clamp(Math.Atan2(wh[target] - h, Math.Max(num3, 1.0)), Math.PI / 12.0);
		x += Math.Sin(psi) * (Math.Cos(theta) * v) * 0.05;
		y += Math.Cos(psi) * (Math.Cos(theta) * v) * 0.05;
		double num7 = h;
		h += Math.Sin(theta) * v * 0.05;
		if (floor != null)
		{
			KeepFloor(num7, Math.Cos(theta) * v * 0.05);
		}
		Record();
	}

	private void KeepFloor(double before, double horizontal)
	{
		double num = floor(x, y);
		if (num > h)
		{
			MaxFloorLift = Math.Max(MaxFloorLift, num - h);
			FloorClamps++;
			h = num;
			if (horizontal > 0.0)
			{
				theta = Math.Atan2(num - before, horizontal);
			}
		}
	}

	private void Record()
	{
		nodes.Add(new Node
		{
			X = x,
			Y = y,
			H = h,
			Psi = psi,
			Theta = theta,
			Phi = phi
		});
	}

	private static double Clamp(double value, double limit)
	{
		if (value > limit)
		{
			return limit;
		}
		if (value < -limit)
		{
			return -limit;
		}
		return value;
	}

	private static double Hypot(double a, double b)
	{
		return Math.Sqrt(a * a + b * b);
	}

	public static List<int> TightTurns(RadarScenarioAircraft aircraft)
	{
		List<int> list = new List<int>();
		if (aircraft == null || aircraft.Route == null || aircraft.Route.Count < 3 || aircraft.SpeedMps <= 0f)
		{
			return list;
		}
		double num = (double)aircraft.MaxBankDeg * (Math.PI / 180.0);
		if (num <= 0.0 || num >= Math.PI / 2.0)
		{
			return list;
		}
		double num2 = 2.0 * aircraft.SpeedMps * aircraft.SpeedMps / (Math.Tan(num) * 9.80665);
		int num3 = aircraft.Route.Count;
		for (int i = 0; i < num3; i++)
		{
			if (!aircraft.Loop && (i == 0 || i == num3 - 1))
			{
				continue;
			}
			RadarScenarioWaypoint radarScenarioWaypoint = aircraft.Route[(num3 - 1 + i) % num3];
			RadarScenarioWaypoint radarScenarioWaypoint2 = aircraft.Route[i];
			RadarScenarioWaypoint radarScenarioWaypoint3 = aircraft.Route[(i + 1) % num3];
			double num4 = (radarScenarioWaypoint3.Position.x - radarScenarioWaypoint2.Position.x) * 1000.0;
			double num5 = (radarScenarioWaypoint3.Position.y - radarScenarioWaypoint2.Position.y) * 1000.0;
			double num6 = (radarScenarioWaypoint2.Position.x - radarScenarioWaypoint.Position.x) * 1000.0;
			double num7 = (radarScenarioWaypoint2.Position.y - radarScenarioWaypoint.Position.y) * 1000.0;
			double num8 = Hypot(num6, num7);
			double num9 = Hypot(num4, num5);
			if (num8 < 1.0 || num9 < 1.0)
			{
				continue;
			}
			double num10 = Math.Abs(RadarMapUnits.WrapPi(Math.Atan2(num4, num5) - Math.Atan2(num6, num7)));
			if (num10 >= Math.PI / 2.0 && num2 > Math.Min(num8, num9))
			{
				list.Add(i);
			}
		}
		return list;
	}
}
