using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class AnchorRun
{
	public sealed class Result
	{
		public ContinuumScenario scenario;

		public CraterProfile crater;

		public RodLengthSeries lengths;

		public ClearanceGuard guard;

		public SolverDiagnostics diag;

		public int steps;

		public float finalTimeS;

		public double wallSeconds;

		public int elementsInitial;

		public bool blockTooSmall;

		public double dcMeasuredM;

		public int fastestNode;

		public float fastestSpeed;

		public float fastestTimeS;

		public float fastestR;

		public float fastestZ;

		public float amplitudePa;

		public float ampMaxPa;

		public float ampMinPa;

		public int ampElements;

		public int fixedElem;

		public float fixedAmplitudePa;

		public float fixedMaxPa;

		public float fixedMinPa;

		public float amplitudeP99Pa;

		public int amplitudeSamples;

		public Result()
		{
			dcMeasuredM = double.NaN;
			fastestNode = -1;
			fixedElem = -1;
		}
	}

	public static Result Run(ContinuumScenario s)
	{
		Stopwatch clock = Stopwatch.StartNew();
		ContinuumBackend.Run run = new ContinuumBackend.Run(s);
		AxiMesh m = run.mesh;
		Result res = new Result
		{
			scenario = s,
			lengths = new RodLengthSeries()
		};
		res.fixedElem = PickFixedElement(m);
		res.lengths.Sample(m, 0f, s.rodDiameterM);
		while (!run.Finished)
		{
			run.Step();
			res.lengths.Sample(m, run.time, s.rodDiameterM);
			TrackFastest(m, run.time, res);
		}
		run.Finish().Dispose();
		res.crater = CraterProfile.Measure(m, s.ElementSizeM);
		MeasureAmplitude(m, run, res);
		res.diag = run.diag;
		res.steps = run.StepIndex;
		res.finalTimeS = run.time;
		res.elementsInitial = run.diag.elementsInitial;
		run.ReleaseWorkingSet();
		clock.Stop();
		res.wallSeconds = clock.Elapsed.TotalSeconds;
		return res;
	}

	private static void MeasureAmplitude(AxiMesh m, ContinuumBackend.Run run, Result res)
	{
		float[] minP = run.ElemMinPureP;
		float[] maxP = run.ElemMaxPureP;
		if (minP == null || maxP == null || res.fastestNode < 0)
		{
			return;
		}
		float best = -1f;
		for (int adj = m.nodeAdjStart[res.fastestNode]; adj < m.nodeAdjStart[res.fastestNode + 1]; adj++)
		{
			int e = m.nodeAdjElem[adj];
			res.ampElements++;
			float amplitude = maxP[e] - minP[e];
			if (amplitude > best)
			{
				res.amplitudePa = amplitude;
				res.ampMaxPa = maxP[e];
				res.ampMinPa = minP[e];
				best = amplitude;
			}
		}
		if (res.fixedElem >= 0)
		{
			res.fixedAmplitudePa = maxP[res.fixedElem] - minP[res.fixedElem];
			res.fixedMaxPa = maxP[res.fixedElem];
			res.fixedMinPa = minP[res.fixedElem];
		}
		List<float> samples = new List<float>();
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemBody[e] == 0)
			{
				float amplitude = maxP[e] - minP[e];
				if (amplitude > 0f)
				{
					samples.Add(amplitude);
				}
			}
		}
		samples.Sort();
		res.amplitudeSamples = samples.Count;
		if (samples.Count > 0)
		{
			int index = Mathf.Clamp((int)Math.Floor((samples.Count - 1) * 0.99f), 0, samples.Count - 1);
			res.amplitudeP99Pa = samples[index];
		}
	}

	private static int PickFixedElement(AxiMesh m)
	{
		float zMin = float.MaxValue;
		float zMax = float.MinValue;
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemBody[e] == 0)
			{
				for (int corner = 0; corner < 4; corner++)
				{
					float z = m.nodeZ[m.elemNodes[e * 4 + corner]];
					zMin = Mathf.Min(zMin, z);
					zMax = Mathf.Max(zMax, z);
				}
			}
		}
		if (zMax < zMin)
		{
			return -1;
		}
		float targetZ = zMax - (zMax - zMin) * 0.25f;
		float best = float.MaxValue;
		int result = -1;
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemBody[e] == 0)
			{
				float z = 0f;
				for (int corner = 0; corner < 4; corner++)
				{
					z += m.nodeZ[m.elemNodes[e * 4 + corner]];
				}
				float score = Mathf.Abs(z * 0.25f - targetZ) + m.elemRc[e] * 4f;
				if (score < best)
				{
					best = score;
					result = e;
				}
			}
		}
		return result;
	}

	private static void TrackFastest(AxiMesh m, float timeS, Result res)
	{
		for (int i = 0; i < m.nodeCount; i++)
		{
			if (m.nodeBody[i] == 0 && m.nodeOrphan[i] == 0)
			{
				float speed = Mathf.Sqrt(m.nodeVz[i] * m.nodeVz[i] + m.nodeVr[i] * m.nodeVr[i]);
				if (speed > res.fastestSpeed)
				{
					res.fastestSpeed = speed;
					res.fastestTimeS = timeS;
					res.fastestNode = i;
					res.fastestR = m.nodeR[i];
					res.fastestZ = m.nodeZ[i];
				}
			}
		}
	}
}
