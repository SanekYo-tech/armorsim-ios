using System;
using UnityEngine;

public sealed class DeletionAccounting
{
	public sealed class Bucket
	{
		public int count;

		public double massKg;
	}

	public const float FaceWindowCalibres = 1f;

	public const float TailWindowCalibres = 2f;

	public readonly Bucket[,] byCause;

	public readonly Bucket[] byBody;

	public int multiCause;

	public int removalsLogged;

	public int removalsDropped;

	public Bucket rodFaceInWindow;

	public Bucket rodElsewhereInWindow;

	public bool windowValid;

	public float windowT0;

	public float windowT1;

	public int[] tailMapCounts;

	public double[] tailMapMass;

	public float tailMapBinM;

	public int[] tipMapCounts;

	public double[] tipMapMass;

	public int rodRemovalsTotal;

	public int rodRemovalsNearTail;

	public double rodMassNearTail;

	public double rodMassTotal;

	public double CriterionShare
	{
		get
		{
			double total = rodElsewhereInWindow.massKg + rodFaceInWindow.massKg;
			return total > 0.0 ? rodElsewhereInWindow.massKg / total : double.NaN;
		}
	}

	public DeletionAccounting()
	{
		byCause = new Bucket[6, 2];
		byBody = new Bucket[2];
		rodFaceInWindow = new Bucket();
		rodElsewhereInWindow = new Bucket();
	}

	public static DeletionAccounting Build(AnchorRun.Result r, int windowI0, int windowI1)
	{
		DeletionAccounting a = new DeletionAccounting();
		for (int cause = 0; cause < 6; cause++)
		{
			for (int body = 0; body < 2; body++)
			{
				a.byCause[cause, body] = new Bucket();
			}
		}
		a.byBody[0] = new Bucket();
		a.byBody[1] = new Bucket();
		float h = r.scenario.ElementSizeM;
		float rodDiameterM = r.scenario.rodDiameterM;
		a.multiCause = r.diag.multiCauseRemovals;
		a.removalsLogged = r.diag.removals.Count;
		a.removalsDropped = r.diag.removalsDropped;
		a.windowValid = windowI0 >= 0 && windowI0 < windowI1;
		if (a.windowValid)
		{
			a.windowT0 = r.lengths.timeS[windowI0];
			a.windowT1 = r.lengths.timeS[windowI1];
		}
		a.tailMapBinM = h;
		int bins = Math.Max(4, (int)Math.Ceiling(r.scenario.rodLengthM / h) + 2);
		a.tailMapCounts = new int[bins];
		a.tailMapMass = new double[bins];
		a.tipMapCounts = new int[bins];
		a.tipMapMass = new double[bins];
		foreach (SolverDiagnostics.ElementRemoval removal in r.diag.removals)
		{
			int bodyIndex = removal.body != 0 ? 1 : 0;
			a.byBody[bodyIndex].count++;
			a.byBody[bodyIndex].massKg += removal.massKg;
			int cause = SolverDiagnostics.PrimaryCause(removal.causeMask);
			if (cause >= 0)
			{
				a.byCause[cause, bodyIndex].count++;
				a.byCause[cause, bodyIndex].massKg += removal.massKg;
			}
			if (removal.body == 0)
			{
				a.rodRemovalsTotal++;
				a.rodMassTotal += removal.massKg;
				float fromTail = Mathf.Max(0f, removal.zM - r.lengths.TailAt(removal.timeS));
				int tailBin = Math.Min(bins - 1, Mathf.FloorToInt(fromTail / h));
				a.tailMapCounts[tailBin]++;
				a.tailMapMass[tailBin] += removal.massKg;
				if (fromTail <= rodDiameterM * TailWindowCalibres)
				{
					a.rodRemovalsNearTail++;
					a.rodMassNearTail += removal.massKg;
				}
				float tip = r.lengths.TipAt(removal.timeS);
				float fromTip = Mathf.Max(0f, tip - removal.zM);
				int tipBin = Math.Min(bins - 1, Mathf.FloorToInt(fromTip / h));
				a.tipMapCounts[tipBin]++;
				a.tipMapMass[tipBin] += removal.massKg;
				if (a.windowValid && removal.timeS >= a.windowT0 && removal.timeS <= a.windowT1)
				{
					Bucket bucket = removal.zM >= tip - rodDiameterM * FaceWindowCalibres ? a.rodFaceInWindow : a.rodElsewhereInWindow;
					bucket.count++;
					bucket.massKg += removal.massKg;
				}
			}
		}
		return a;
	}

	public static double RateFromMass(AnchorRun.Result r, DeletionAccounting a, float rho0)
	{
		if (a.windowValid)
		{
			double dt = (double)(a.windowT1 - a.windowT0);
			if (dt > 0.0)
			{
				double mass = a.rodElsewhereInWindow.massKg + a.rodFaceInWindow.massKg;
				float radius = r.lengths.MeanHeadRadius(a.windowT0, a.windowT1);
				if (radius > 0f)
				{
					return mass / dt / ((double)radius * Math.PI * (double)radius * (double)rho0);
				}
			}
		}
		return double.NaN;
	}
}
