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

	public bool HasField => fieldGridR > 0 && fieldGridZ > 0 && fieldBounds.Valid;

	public void Dispose()
	{
		for (int i = 0; i < frames.Count; i++)
		{
			GeometryFrame geometryFrame = frames[i];
			geometryFrame.Dispose();
		}
		frames.Clear();
	}
}
