using System;
using System.Collections.Generic;

public sealed class SuperUtilSeries
{
	private readonly List<double> _s;

	public int Count => _s.Count;

	public double Min
	{
		get
		{
			if (_s.Count == 0)
			{
				return -1.0;
			}
			return Quantile(0.0);
		}
	}

	public double Mean
	{
		get
		{
			if (_s.Count == 0)
			{
				return -1.0;
			}
			double sum = 0.0;
			foreach (double value in _s)
			{
				sum += value;
			}
			return sum / (double)_s.Count;
		}
	}

	public SuperUtilSeries()
	{
		_s = new List<double>(4096);
	}

	public void Add(double util)
	{
		if (util >= 0.0)
		{
			_s.Add(util);
		}
	}

	public double Quantile(double q)
	{
		if (_s.Count == 0)
		{
			return -1.0;
		}
		double[] array = _s.ToArray();
		Array.Sort(array);
		double position = (double)(array.Length - 1) * q;
		int lo = (int)Math.Floor(position);
		int hi = Math.Min(lo + 1, array.Length - 1);
		double frac = position - (double)lo;
		return array[lo] * (1.0 - frac) + array[hi] * frac;
	}
}
