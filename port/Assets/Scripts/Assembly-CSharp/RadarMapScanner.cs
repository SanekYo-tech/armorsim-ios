using System;
using System.Collections.Generic;

public sealed class RadarMapScanner
{
	public readonly RadarMapStationSpec Spec;

	private double segmentTime;

	private double segmentTheta;

	private double rpmEffective;

	private double anchorTime;

	private long anchorScan;

	private double lastTime;

	private const double AnchorOffset = 1E-06;

	public int Integration { get; private set; }

	public double SectorCentreDeg { get; private set; }

	public double SectorWidthDeg { get; private set; }

	public double MaxCrossingResidualDeg { get; private set; }

	public double Dwell => RadarMapDetector.Dwell(Spec, Integration);

	public double Revisit => RadarMapDetector.Revisit(Spec, Integration, SectorWidthDeg);

	public int Beams => RadarMapDetector.BeamPositions(Spec, SectorWidthDeg);

	private double DegPerSecond => rpmEffective * 6.0;

	private double SectorStart => SectorCentreDeg - SectorWidthDeg * 0.5;

	public double BeamWidthDeg
	{
		get
		{
			if (Spec.Scan == RadarMapScan.Rotating)
			{
				return 360.0 / Beams;
			}
			return Spec.BeamAzimuthDeg;
		}
	}

	public RadarMapScanner(RadarMapStationSpec spec, double sectorCentreDeg, double startTime = 0.0)
	{
		Integration = 1;
		Spec = spec;
		SectorCentreDeg = Wrap360(sectorCentreDeg);
		SectorWidthDeg = spec.SectorDeg;
		segmentTime = 0.0;
		segmentTheta = 0.0;
		rpmEffective = spec.Rpm;
		anchorTime = startTime + 1E-06;
		anchorScan = 0L;
		lastTime = startTime;
	}

	public static double Wrap360(double deg)
	{
		deg %= 360.0;
		if (deg < 0.0)
		{
			deg += 360.0;
		}
		return deg;
	}

	public double Theta(double t)
	{
		return segmentTheta + (t - segmentTime) * DegPerSecond;
	}

	private double TimeOfTheta(double theta)
	{
		return segmentTime + (theta - segmentTheta) / DegPerSecond;
	}

	public double BeamAzimuth(double t)
	{
		if (Spec.Scan == RadarMapScan.Rotating)
		{
			return Wrap360(Theta(t));
		}
		double revisit = Revisit;
		double dwell = Dwell;
		double num = Math.Floor((t - anchorTime) / revisit);
		int num2 = (int)Math.Floor((t - anchorTime - num * revisit) / dwell);
		num2 = Math.Min(Beams - 1, num2);
		return Wrap360(SectorStart + ((double)num2 + 0.5) * Spec.BeamAzimuthDeg);
	}

	public long ScanAt(double t)
	{
		if (Spec.Scan == RadarMapScan.Rotating)
		{
			return (long)Math.Floor(Theta(t) / 360.0);
		}
		return anchorScan + (long)Math.Floor((t - anchorTime) / Revisit);
	}

	public void SetIntegration(int m, double now)
	{
		if (m != 1 && m != 2 && m != 4)
		{
			throw new ArgumentOutOfRangeException("m");
		}
		if (m != Integration)
		{
			Reanchor(now);
			Integration = m;
			rpmEffective = Spec.Rpm / m;
		}
	}

	public void SetSector(double centreDeg, double widthDeg, double now)
	{
		if (Spec.Scan != RadarMapScan.Sector)
		{
			return;
		}
		widthDeg = Math.Max(Spec.BeamAzimuthDeg, Math.Min(Spec.SectorDeg, widthDeg));
		centreDeg = Wrap360(centreDeg);
		if (centreDeg == SectorCentreDeg && widthDeg == SectorWidthDeg)
		{
			return;
		}
		Reanchor(now);
		SectorCentreDeg = centreDeg;
		SectorWidthDeg = widthDeg;
	}

	private void Reanchor(double now)
	{
		if (Spec.Scan == RadarMapScan.Rotating)
		{
			segmentTime = now;
			segmentTheta = Theta(now);
		}
		else
		{
			anchorScan = ScanAt(now) + 1;
			anchorTime = now + 1E-06;
		}
	}

	public void RotatingCrossings(double t0, double t1, Func<double, double> bearingAt, List<(double time, long scan)> output)
	{
		output.Clear();
		if (t1 <= t0)
		{
			return;
		}
		long num = (long)Math.Floor(Theta(t1) / 360.0) + 1;
		for (long num2 = (long)Math.Floor(Theta(t0) / 360.0) - 1; num2 <= num; num2++)
		{
			double num3 = Wrap360(bearingAt(TimeOfTheta(num2 * 360.0 + 180.0)));
			double num4 = Wrap360(bearingAt(TimeOfTheta(num2 * 360.0 + num3)));
			if (Math.Abs(num4 - num3) > 180.0)
			{
				num4 = num3;
			}
			double num5 = TimeOfTheta(num2 * 360.0 + num4);
			double num6 = Math.Abs(Wrap180(bearingAt(num5) - num4));
			if (num6 > MaxCrossingResidualDeg && num6 < 180.0)
			{
				MaxCrossingResidualDeg = num6;
			}
			if (num5 > t0 && num5 <= t1 && num5 >= segmentTime - 1E-12)
			{
				output.Add((num5, num2));
			}
		}
	}

	public void BeamEvents(double t0, double t1, List<(double time, long scan, int beam)> output)
	{
		output.Clear();
		if (t1 <= t0)
		{
			return;
		}
		int beams = Beams;
		if (Spec.Scan == RadarMapScan.Sector)
		{
			double revisit = Revisit;
			double dwell = Dwell;
			long num = ScanAt(t1);
			for (long num2 = ScanAt(t0); num2 <= num; num2++)
			{
				for (int i = 0; i < beams; i++)
				{
					double num3 = anchorTime + (num2 - anchorScan) * revisit + i * dwell;
					if (num3 > t0 && num3 <= t1)
					{
						output.Add((num3, num2, i));
					}
				}
			}
		}
		else
		{
			double num4 = 360.0 / beams;
			long num5 = (long)Math.Floor(Theta(t1) / 360.0);
			for (long num6 = (long)Math.Floor(Theta(t0) / 360.0); num6 <= num5; num6++)
			{
				for (int j = 0; j < beams; j++)
				{
					double num7 = TimeOfTheta(num6 * 360.0 + ((double)j + 0.5) * num4);
					if (num7 > t0 && num7 <= t1)
					{
						output.Add((num7, num6, j));
					}
				}
			}
		}
	}

	public int SectorBeamOf(double bearingDeg)
	{
		double num = SectorWidthDeg * 0.5 + Wrap180(bearingDeg - SectorCentreDeg);
		if (num < 0.0 || num > SectorWidthDeg)
		{
			return -1;
		}
		return Math.Min(Beams - 1, (int)Math.Floor(num / Spec.BeamAzimuthDeg));
	}

	public double BeamCentre(int k)
	{
		if (Spec.Scan == RadarMapScan.Rotating)
		{
			return ((double)k + 0.5) * 360.0 / Beams;
		}
		return Wrap360(SectorStart + ((double)k + 0.5) * Spec.BeamAzimuthDeg);
	}

	public static double Wrap180(double deg)
	{
		deg = Wrap360(deg);
		if (deg > 180.0)
		{
			deg -= 360.0;
		}
		return deg;
	}
}
