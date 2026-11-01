using System;
using System.Collections.Generic;

public sealed class RodLengthSeries
{
	public const float SampleIntervalS = 1E-06f;

	public readonly List<float> timeS = new List<float>();

	public readonly List<float> lengthM = new List<float>();

	public readonly List<float> tipZM = new List<float>();

	public readonly List<float> tailZM = new List<float>();

	public readonly List<float> headRadiusM = new List<float>();

	private float nextSampleS;

	public float L0M;

	public void Sample(AxiMesh m, float timeNowS, float rodDiameterM)
	{
		if (timeS.Count > 0 && nextSampleS > timeNowS)
		{
			return;
		}
		Extent(m, out float lo, out float hi);
		if (lo > hi)
		{
			return;
		}
		float length = hi - lo;
		if (timeS.Count == 0)
		{
			L0M = length;
		}
		timeS.Add(timeNowS);
		lengthM.Add(length);
		tipZM.Add(hi);
		tailZM.Add(lo);
		headRadiusM.Add(HeadRadius(m, hi, rodDiameterM));
		nextSampleS = timeNowS + SampleIntervalS;
	}

	private static float HeadRadius(AxiMesh m, float tipZ, float rodDiameterM)
	{
		float zMin = tipZ - rodDiameterM * 0.25f;
		float radius = 0f;
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemAlive[e] == 0 || m.elemBody[e] != 0)
			{
				continue;
			}
			for (int c = 0; c < 4; c++)
			{
				int node = m.elemNodes[e * 4 + c];
				if (m.nodeZ[node] >= zMin && m.nodeR[node] > radius)
				{
					radius = m.nodeR[node];
				}
			}
		}
		return radius;
	}

	public float TimeWhenTipReached(float zM)
	{
		for (int i = 0; i < tipZM.Count; i++)
		{
			if (tipZM[i] >= zM)
			{
				return timeS[i];
			}
		}
		return -1f;
	}

	public float TipAt(float t)
	{
		return Interp(tipZM, t);
	}

	public float MeanHeadRadius(float t0, float t1)
	{
		double sum = 0.0;
		int count = 0;
		for (int i = 0; i < timeS.Count; i++)
		{
			if (timeS[i] >= t0 && timeS[i] <= t1)
			{
				sum += headRadiusM[i];
				count++;
			}
		}
		return count == 0 ? 0f : (float)(sum / count);
	}

	public float TailAt(float t)
	{
		return Interp(tailZM, t);
	}

	private float Interp(List<float> v, float t)
	{
		if (v.Count == 0)
		{
			return 0f;
		}
		if (timeS[0] >= t)
		{
			return v[0];
		}
		for (int i = 1; i < timeS.Count; i++)
		{
			if (t <= timeS[i])
			{
				float dt = timeS[i] - timeS[i - 1];
				float f = dt > 1E-12f ? (t - timeS[i - 1]) / dt : 0f;
				f = Math.Max(0f, Math.Min(1f, f));
				return v[i - 1] + (v[i] - v[i - 1]) * f;
			}
		}
		return v[v.Count - 1];
	}

	public static void Extent(AxiMesh m, out float lo, out float hi)
	{
		lo = float.MaxValue;
		hi = float.MinValue;
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemAlive[e] == 0 || m.elemBody[e] != 0)
			{
				continue;
			}
			for (int c = 0; c < 4; c++)
			{
				float z = m.nodeZ[m.elemNodes[e * 4 + c]];
				lo = Math.Min(lo, z);
				hi = Math.Max(hi, z);
			}
		}
	}

	public static float Length(AxiMesh m)
	{
		Extent(m, out float lo, out float hi);
		return lo > hi ? -1f : hi - lo;
	}

	public int FirstAtOrBelow(float f)
	{
		float threshold = f * L0M;
		for (int i = 0; i < lengthM.Count; i++)
		{
			if (lengthM[i] <= threshold)
			{
				return i;
			}
		}
		return -1;
	}

	public float MinFraction()
	{
		float min = float.MaxValue;
		for (int i = 0; i < lengthM.Count; i++)
		{
			if (lengthM[i] < min)
			{
				min = lengthM[i];
			}
		}
		return L0M > 1E-09f ? min / L0M : 0f;
	}

	public int IndexOfMin()
	{
		float min = float.MaxValue;
		int index = -1;
		for (int i = 0; i < lengthM.Count; i++)
		{
			if (lengthM[i] < min)
			{
				min = lengthM[i];
				index = i;
			}
		}
		return index;
	}

	public bool Fit(int i0, int i1, out double slopeMps, out double residualRmsM, out int n)
	{
		slopeMps = 0.0;
		residualRmsM = 0.0;
		n = 0;
		if (i0 < 0 || i1 < 0 || i1 <= i0)
		{
			return false;
		}
		n = i1 - i0 + 1;
		if (n < 3)
		{
			return false;
		}
		double sumT = 0.0;
		double sumL = 0.0;
		for (int i = i0; i <= i1; i++)
		{
			sumT += timeS[i];
			sumL += lengthM[i];
		}
		double meanT = sumT / n;
		double meanL = sumL / n;
		double sxy = 0.0;
		double sxx = 0.0;
		for (int i = i0; i <= i1; i++)
		{
			double dt = timeS[i] - meanT;
			sxy += (lengthM[i] - meanL) * dt;
			sxx += dt * dt;
		}
		if (sxx <= 0.0)
		{
			return false;
		}
		slopeMps = sxy / sxx;
		double intercept = meanL - slopeMps * meanT;
		double residual = 0.0;
		for (int i = i0; i <= i1; i++)
		{
			double error = lengthM[i] - (slopeMps * timeS[i] + intercept);
			residual += error * error;
		}
		residualRmsM = Math.Sqrt(residual / n);
		return true;
	}
}
