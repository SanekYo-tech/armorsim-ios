using System;

public struct RadarMapPath
{
	public RadarMapPathState State;

	public double F;

	public double F4;

	public double GroundRange;

	public double RadarHeight;

	public double TargetHeight;

	public double HorizonM;

	public double WorstClearance;

	public double WorstAt;

	public int ProfileSamples;

	public bool Reflection;

	public bool ReflectionMasked;

	public bool PermittivityClamped;

	public RadarScenarioTerrainKind SurfaceKind;

	public RadarMapSurface Surface;

	public double SurfaceLevel;

	public double SpecularX;

	public double SpecularY;

	public RadarMapTwoRay Ray;

	public bool HasRay;

	public Complex64 Gamma;

	public Complex64 Reflected;

	public double Rho;

	public double PatternRatio;

	public double MonopulseRatio;

	public double Alpha;

	public double HorizonMargin => HorizonM - GroundRange;

	public double F4Db => Math.Log10(Math.Max(F, 1E-12)) * 40.0;
}
