using System;
using System.Globalization;

public sealed class RadarAtlasSpec
{
	public const int FormatVersion = 1;

	public const ulong DefaultSeed = 5927093631938940209uL;

	public const string MetalSolverTag = "PO1-frozen-v1";

	public const string CoatedSolverTag = "PO1-frozen-coat-v1";

	public const double RefuseBelowWavelengths = 3.0;

	public const double BoundaryBelowWavelengths = 20.0;

	public readonly double DeltaDeg;

	public readonly int SamplesPerSide;

	public readonly ulong Seed;

	public readonly string SolverTag;

	public static RadarAtlasSpec Standard => new RadarAtlasSpec(2.0, 6);

	public static RadarAtlasSpec Draft => new RadarAtlasSpec(5.0, 3);

	public int Samples => SamplesPerSide * SamplesPerSide;

	public int AzimuthCells => (int)Math.Round(360.0 / DeltaDeg);

	public int ElevationCells => (int)Math.Round(180.0 / DeltaDeg);

	public int CellCount => AzimuthCells * ElevationCells;

	public RadarAtlasSpec(double deltaDeg, int samplesPerSide, ulong seed = 5927093631938940209uL, string solverTag = "PO1-frozen-v1")
	{
		if (deltaDeg <= 0.0 || Math.Abs(360.0 / deltaDeg - Math.Round(360.0 / deltaDeg)) > 1E-09 || Math.Abs(180.0 / deltaDeg - Math.Round(180.0 / deltaDeg)) > 1E-09)
		{
			throw new ArgumentOutOfRangeException("deltaDeg", deltaDeg, "Δ must divide 180°.");
		}
		if (samplesPerSide < 1 || samplesPerSide > 64)
		{
			throw new ArgumentOutOfRangeException("samplesPerSide");
		}
		SamplesPerSide = samplesPerSide;
		DeltaDeg = deltaDeg;
		Seed = seed;
		if (string.IsNullOrEmpty(solverTag))
		{
			throw new ArgumentException("solverTag");
		}
		SolverTag = solverTag;
	}

	public RadarAtlasSpec For(RadarCoating coating)
	{
		string text = "PO1-frozen-v1";
		if (coating != null && !coating.IsMetal)
		{
			text = "PO1-frozen-coat-v1";
		}
		return new RadarAtlasSpec(DeltaDeg, SamplesPerSide, Seed, text);
	}

	public int CellIndex(int i, int j)
	{
		return j * AzimuthCells + i;
	}

	public double CellAzimuthLow(int i)
	{
		return (double)i * DeltaDeg - 180.0;
	}

	public double CellElevationLow(int j)
	{
		return (double)j * DeltaDeg - 90.0;
	}

	public double CellAzimuthCentre(int i)
	{
		return ((double)i + 0.5) * DeltaDeg - 180.0;
	}

	public double CellElevationCentre(int j)
	{
		return ((double)j + 0.5) * DeltaDeg - 90.0;
	}

	public void Probe(int i, int j, int s, out double azDeg, out double elDeg, out double u1, out double u2)
	{
		ulong state = RadarMapRng.AtlasState(Seed, CellIndex(i, j), s);
		u1 = RadarMapRng.NextUnit(ref state);
		u2 = RadarMapRng.NextUnit(ref state);
		int num = s % SamplesPerSide;
		int num2 = s / SamplesPerSide;
		double num3 = (double)i * DeltaDeg - 180.0;
		double num4 = (double)j * DeltaDeg - 90.0;
		double num5 = Math.Sin(num4 * (Math.PI / 180.0));
		double num6 = Math.Sin((num4 + DeltaDeg) * (Math.PI / 180.0)) - num5;
		azDeg = num3 + ((double)num + u1) * DeltaDeg / SamplesPerSide;
		elDeg = Math.Asin(Math.Max(-1.0, Math.Min(1.0, num5 + ((double)num2 + u2) * num6 / SamplesPerSide))) * (180.0 / Math.PI);
	}

	public void CellOf(double azDeg, double elDeg, out int i, out int j)
	{
		i = (int)Math.Floor((azDeg + 180.0) / DeltaDeg) % AzimuthCells;
		if (i < 0)
		{
			i += AzimuthCells;
		}
		j = Math.Min(ElevationCells - 1, Math.Max(0, (int)Math.Floor((elDeg + 90.0) / DeltaDeg)));
	}

	public override string ToString()
	{
		return DeltaDeg.ToString("0.###", CultureInfo.InvariantCulture) + "° · M = " + Samples.ToString();
	}
}
