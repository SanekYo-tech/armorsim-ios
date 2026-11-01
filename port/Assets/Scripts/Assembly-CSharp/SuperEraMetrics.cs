using System.Collections.Generic;
using UnityEngine;

public static class SuperEraMetrics
{
	public sealed class Sample
	{
		public double throughFraction;

		public float penetrationDepthM;

		public float armourThicknessM;

		public int rodPieces;

		public double largestPieceFraction;

		public double rodMassKg;

		public bool reachedArmour;

		public float finalApproachM;

		public double escapedFraction;

		public bool ok;

		public string failure;

		public string Line()
		{
			string[] array = new string[7];
			array[0] = string.Format("пробито {0:P2}, глубина {1:F1}/{2:F0} мм, ", throughFraction, penetrationDepthM * 1000f, armourThicknessM * 1000f);
			array[1] = string.Format("кусков {0}, крупнейший {1:P0}, ", rodPieces, largestPieceFraction);
			array[2] = "подход на послед. кадре ";
			array[3] = ((finalApproachM == float.MaxValue) ? "вне габарита" : ((finalApproachM * 1000f).ToString("F1") + " мм"));
			array[4] = ", ";
			array[5] = string.Format("вне сетки {0:P3}", escapedFraction);
			array[6] = (ok ? "" : "  ⛔ " + failure);
			return string.Concat(array);
		}
	}

	public const float BaselineDepthFraction = 0.5f;

	public static void RodPieces(Vector3[] pos, Vector3[] vel, float[] mass, float[] damage, SuperBaker.Body rod, float spacingM, out int pieces, out double largestFraction, out double totalKg)
	{
		List<SuperFragments.Piece> list = SuperFragments.Split(pos, vel, mass, rod.start, rod.count, spacingM, damage);
		pieces = list.Count;
		RadarKahan radarKahan = default(RadarKahan);
		for (int i = rod.start; i < rod.start + rod.count; i++)
		{
			radarKahan.Add(mass[i]);
		}
		totalKg = radarKahan.Value;
		double num = 0.0;
		foreach (SuperFragments.Piece item in list)
		{
			if (item.massKg > num)
			{
				num = item.massKg;
			}
		}
		largestFraction = ((totalKg > 1E-30) ? (num / totalKg) : 0.0);
	}

	public static bool WithinFootprint(Vector3 p, SuperBaker.Body plate)
	{
		if (plate.halfSideM <= 0f)
		{
			return true;
		}
		Vector3 vector = p - plate.centre;
		float num = Vector3.Dot(vector, plate.axisU);
		if (plate.halfUM < Mathf.Abs(num))
		{
			return false;
		}
		float num2 = Vector3.Dot(vector, plate.axisV);
		return plate.halfVM >= Mathf.Abs(num2);
	}

	public static float PenetrationDepth(Vector3[] pos, SuperBaker.Body rod, SuperBaker.Body armour)
	{
		Vector3 vector = armour.normal.normalized;
		float plane = Vector3.Dot(armour.faceCentre, vector);
		float best = 0f;
		for (int i = rod.start; i < rod.start + rod.count; i++)
		{
			if (!WithinFootprint(pos[i], armour))
			{
				continue;
			}
			float num = plane - Vector3.Dot(pos[i], vector);
			if (num > best)
			{
				best = num;
			}
		}
		return Mathf.Min(best, armour.thicknessM);
	}

	public static float FinalApproach(Vector3[] pos, SuperBaker.Body rod, SuperBaker.Body armour)
	{
		Vector3 vector = armour.normal.normalized;
		float plane = Vector3.Dot(armour.faceCentre, vector);
		float best = float.MaxValue;
		for (int i = rod.start; i < rod.start + rod.count; i++)
		{
			if (!WithinFootprint(pos[i], armour))
			{
				continue;
			}
			float num = Vector3.Dot(pos[i], vector) - plane;
			if (num < best)
			{
				best = num;
			}
		}
		if (best == float.MaxValue)
		{
			return float.MaxValue;
		}
		return best;
	}

	public static double ThroughFraction(Vector3[] pos, float[] mass, SuperBaker.Body rod, SuperBaker.Body armour)
	{
		Vector3 vector = armour.normal.normalized;
		float plane = Vector3.Dot(armour.faceCentre, vector);
		RadarKahan total = default(RadarKahan);
		RadarKahan esc = default(RadarKahan);
		for (int i = rod.start; i < rod.start + rod.count; i++)
		{
			total.Add(mass[i]);
			if (Vector3.Dot(pos[i], vector) < plane - armour.thicknessM && WithinFootprint(pos[i], armour))
			{
				esc.Add(mass[i]);
			}
		}
		if (total.Value > 1E-30)
		{
			return esc.Value / total.Value;
		}
		return 0.0;
	}

	public static float Severity(Sample s)
	{
		if (s.throughFraction > 0.0)
		{
			return (float)s.throughFraction;
		}
		float num = Mathf.Max(1E-06f, s.armourThicknessM);
		float num2 = s.penetrationDepthM / num;
		return Mathf.Clamp(num2, 0f, 1f) - 1f;
	}

	public static float Gain(Sample off, Sample on)
	{
		return Severity(off) - Severity(on);
	}

	public static bool BaselineUsable(Sample off)
	{
		return off.ok && off.penetrationDepthM >= off.armourThicknessM * BaselineDepthFraction;
	}

	public static bool GainGrows(IList<float> gains, float tolerance)
	{
		for (int i = 1; i < gains.Count; i++)
		{
			if (gains[i - 1] - tolerance > gains[i])
			{
				return false;
			}
		}
		return true;
	}
}
