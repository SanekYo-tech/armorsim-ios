using System;
using UnityEngine;

public static class SuperEnsemble
{
	public const int Members = 8;

	public const int UlpAmplitude = 1;

	private static uint Hash(uint a)
	{
		a ^= a >> 16;
		a *= 0x85EBCA6Bu;
		a ^= a >> 13;
		a *= 0xC2B2AE35u;
		return a ^= a >> 16;
	}

	public static float NudgeUlp(float value, int steps)
	{
		if (steps == 0 || value == 0f || float.IsNaN(value) || float.IsInfinity(value))
		{
			return value;
		}
		if (value < 0f)
		{
			steps = -steps;
		}
		return BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(value) + steps);
	}

	public static void Perturb(SuperParticles p, int member)
	{
		if (member == 0)
		{
			return;
		}
		for (int i = 0; i < p.count; i++)
		{
			uint h = Hash((uint)(member * 0x466F45D) ^ (uint)(i * 0x127409F));
			uint u = Hash(h ^ 0x9E3779B9u);
			uint w = Hash(u ^ 0x85EBCA6Bu);
			Vector3 v = p.x[i];
			p.x[i] = new Vector3(NudgeUlp(v.x, (int)(h & 3) - 1), NudgeUlp(v.y, (int)(u & 3) - 1), NudgeUlp(v.z, (int)(w & 3) - 1));
		}
	}

	public static void Stats(double[] v, out double mean, out double spread, out double sd)
	{
		RadarKahan sum = new RadarKahan();
		RadarKahan sq = new RadarKahan();
		double min = double.MaxValue;
		double max = double.MinValue;
		for (int i = 0; i < v.Length; i++)
		{
			sum.Add(v[i]);
			min = Math.Min(min, v[i]);
			max = Math.Max(max, v[i]);
		}
		spread = (max - min) * 0.5;
		mean = sum.Value / v.Length;
		for (int j = 0; j < v.Length; j++)
		{
			double d = v[j] - mean;
			sq.Add(d * d);
		}
		if (v.Length <= 1)
		{
			sd = 0.0;
		}
		else
		{
			sd = Math.Sqrt(sq.Value / (v.Length - 1));
		}
	}
}
