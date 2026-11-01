using System;
using System.Collections.Generic;

public sealed class GeometryTimeline : IDisposable
{
	public float dtOutput;

	public List<GeometryFrame> frames;

	public SolverDiagnostics diagnostics;

	public int fieldGridR;

	public int fieldGridZ;

	public UltraFieldGrid.Bounds fieldBounds;

	public bool Completed;

	public bool Cancelled;

	public bool HasField => false;

	public void Dispose()
	{
	}
}
