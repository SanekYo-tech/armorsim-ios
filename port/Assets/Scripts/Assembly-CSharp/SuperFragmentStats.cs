using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class SuperFragmentStats
{
	public struct PieceRow
	{
		public int index;

		public int particles;

		public double massKg;

		public Vector3 centroid;

		public Vector3 meanVelocity;

		public double speedLabMps;

		public double speedComMps;

		public double radiusM;

		public double polarDeg;
	}

	public struct Decade
	{
		public int exp10;

		public int count;

		public double massKg;
	}

	public const string RefusalDamageOff = "DAMAGE_OFF";

	public const string RefusalEmptyBody = "EMPTY_BODY";

	public const string RefusalNoMass = "NO_MASS";

	public const string RefusalNoPieces = "NO_PIECES";

	public const string RefusalBadArrays = "BAD_ARRAYS";

	public const string RefusalDegenerateAxis = "DEGENERATE_AXIS";

	public static readonly double[] Quantiles = new double[5]
	{
		0.1,
		0.25,
		0.5,
		0.75,
		0.9
	};

	public const int PolarBins = 18;

	public const double BigPieceFraction = 0.01;

	public const double HugePieceFraction = 0.1;

	public bool ok;

	public string refusalName;

	public string refusal;

	public string tag;

	public int bodyParticles;

	public double casingMassKg;

	public double crackMassKg;

	public int crackedParticles;

	public double piecesMassKg;

	public int pieceCount;

	public double mainMassKg;

	public int mainParticles;

	public double[] massQuantilesKg;

	public int medianPieceParticles;

	public int maxPieceParticles;

	public Decade[] decades;

	public int piecesAboveBig;

	public int piecesAboveHuge;

	public Vector3 comVelocityMps;

	public double[] speedQuantilesLabMps;

	public double[] speedQuantilesComMps;

	public double meanSpeedLabMps;

	public double meanSpeedComMps;

	public double maxSpeedComMps;

	public Vector3 burstCentreM;

	public Vector3 axis;

	public double rmsRadiusM;

	public double[] polarMassFraction;

	public double polarAtCentre;

	public double separatedMassKg;

	public double separatedMassRatio;

	public PieceRow[] pieces;

	public SuperFragmentStats()
	{
		tag = "";
		decades = new Decade[0];
		polarMassFraction = new double[18];
		pieces = new PieceRow[0];
	}

	public static SuperFragmentStats Measure(Vector3[] pos, Vector3[] vel, float[] mass, float[] damage, in SuperBaker.Body body, float spacingM, Vector3 burstCentreM, Vector3 axisToNose, Vector3 comVelocityMps, string tag = "")
	{
		SuperFragmentStats r = new SuperFragmentStats();
		r.tag = tag ?? "";
		r.bodyParticles = body.count;
		r.burstCentreM = burstCentreM;
		r.comVelocityMps = comVelocityMps;
		r.massQuantilesKg = new double[Quantiles.Length];
		r.speedQuantilesLabMps = new double[Quantiles.Length];
		r.speedQuantilesComMps = new double[Quantiles.Length];
		if (!SuperDamage.Enabled || damage == null)
		{
			return r.Refuse("DAMAGE_OFF", "модель разрушения выключена (SuperDamage.Enabled = " + SuperDamage.Enabled.ToString() + ((damage == null) ? ", повреждение по частицам не передано" : "") + "): без неё SuperFragments мостит трещину радиусом связи, и перепись показала бы «кусков 1, трещин 0» — прибор ответил бы, ничего не измерив. Это отказ МЕРИТЬ, а не результат.");
		}
		if (pos == null || vel == null || mass == null)
		{
			return r.Refuse("BAD_ARRAYS", "состояние частиц не передано целиком: положения " + ((pos == null) ? "нет" : "есть") + ", скорости " + ((vel == null) ? "нет" : "есть") + ", массы " + ((mass == null) ? "нет" : "есть") + ".");
		}
		if (body.count <= 0)
		{
			return r.Refuse("EMPTY_BODY", "у тела «" + body.name + "» нет ни одной частицы.");
		}
		if (body.start < 0 || body.start + body.count > mass.Length || body.start + body.count > pos.Length || body.start + body.count > vel.Length || body.start + body.count > damage.Length)
		{
			return r.Refuse("BAD_ARRAYS", string.Format("тело «{0}» занимает [{1}; {2}), а массивы ", body.name, body.start, body.start + body.count) + string.Format("состояния короче (масс {0}, положений {1}, скоростей {2}, ", mass.Length, pos.Length, vel.Length) + string.Format("повреждений {0}).", damage.Length));
		}
		if (spacingM <= 0f)
		{
			return r.Refuse("BAD_ARRAYS", string.Format("шаг частиц {0} — радиус связи кусков неопределим.", spacingM));
		}
		double ax = axisToNose.x;
		double ay = axisToNose.y;
		double az = axisToNose.z;
		double axisLen = Math.Sqrt(ay * ay + ax * ax + az * az);
		if (axisLen < 1e-09)
		{
			return r.Refuse("DEGENERATE_AXIS", "ось корпуса вырождена (длина " + axisLen.ToString("E2") + "): полярная гистограмма разлёта считалась бы от неопределённого направления.");
		}
		double nx = ax / axisLen;
		double ny = ay / axisLen;
		double nz = az / axisLen;
		r.axis = new Vector3((float)nx, (float)ny, (float)nz);
		RadarKahan massSum = default(RadarKahan);
		RadarKahan crackSum = default(RadarKahan);
		for (int i = body.start; i < body.start + body.count; i++)
		{
			massSum.Add(mass[i]);
			if (damage[i] >= 1f)
			{
				r.crackedParticles++;
				crackSum.Add(mass[i]);
			}
		}
		r.casingMassKg = massSum.Value;
		r.crackMassKg = crackSum.Value;
		if (massSum.Value <= 1e-30)
		{
			return r.Refuse("NO_MASS", "масса тела «" + body.name + "» вышла нулевой — долей считать не от чего.");
		}
		List<SuperFragments.Piece> pieces = SuperFragments.Split(pos, vel, mass, body.start, body.count, spacingM, damage);
		r.pieceCount = pieces.Count;
		if (pieces.Count == 0)
		{
			return r.Refuse("NO_PIECES", string.Format("у тела «{0}» ({1} частиц, вещества трещин {2}) не нашлось НИ ОДНОГО куска: всё вещество помечено трещиной. Это не «ноль осколков», а замер, которого не было.", body.name, body.count, r.crackedParticles));
		}
		int mainIdx = 0;
		for (int j = 1; j < pieces.Count; j++)
		{
			if (pieces[j].massKg > pieces[mainIdx].massKg)
			{
				mainIdx = j;
			}
		}
		if (mainIdx != 0)
		{
			return r.Refuse("BAD_ARRAYS", string.Format("SuperFragments вернул куски НЕ по убыванию массы: самый тяжёлый под номером {0}. ", mainIdx) + "Порядок — договор прибора, и вся эта перепись на нём стоит.");
		}
		r.mainMassKg = pieces[0].massKg;
		r.mainParticles = pieces[0].particles.Count;
		r.pieces = new PieceRow[pieces.Count];
		double halfSpacing = spacingM * 0.5;
		RadarKahan piecesMass = default(RadarKahan);
		RadarKahan labSpeedSum = default(RadarKahan);
		RadarKahan comSpeedSum = default(RadarKahan);
		RadarKahan rmsSum = default(RadarKahan);
		RadarKahan separatedMass = default(RadarKahan);
		for (int k = 0; k < pieces.Count; k++)
		{
			SuperFragments.Piece piece = pieces[k];
			double m = piece.massKg;
			piecesMass.Add(m);
			if (k != 0)
			{
				separatedMass.Add(m);
			}
			Vector3 mv = piece.meanVelocity;
			double speedLabMps = Math.Sqrt((double)mv.x * mv.x + (double)mv.y * mv.y + (double)mv.z * mv.z);
			double dx = (double)mv.x - comVelocityMps.x;
			double dy = (double)mv.y - comVelocityMps.y;
			double dz = (double)mv.z - comVelocityMps.z;
			double speedComMps = Math.Sqrt(dx * dx + dy * dy + dz * dz);
			labSpeedSum.Add(speedLabMps * m);
			comSpeedSum.Add(speedComMps * m);
			if (speedComMps > r.maxSpeedComMps)
			{
				r.maxSpeedComMps = speedComMps;
			}
			Vector3 rel = piece.centroid - burstCentreM;
			double radiusM = Math.Sqrt((double)rel.x * rel.x + (double)rel.y * rel.y + (double)rel.z * rel.z);
			rmsSum.Add(m * radiusM * radiusM);
			double polarDeg;
			if (halfSpacing > radiusM)
			{
				polarDeg = -1.0;
			}
			else
			{
				double cosA = ((double)rel.x * nx + (double)rel.y * ny + (double)rel.z * nz) / radiusM;
				if (cosA > 1.0)
				{
					cosA = 1.0;
				}
				else if (cosA < -1.0)
				{
					cosA = -1.0;
				}
				polarDeg = Math.Acos(cosA) * 57.29577951308232;
			}
			r.pieces[k] = new PieceRow
			{
				index = k,
				particles = piece.particles.Count,
				massKg = m,
				centroid = piece.centroid,
				meanVelocity = piece.meanVelocity,
				speedLabMps = speedLabMps,
				speedComMps = speedComMps,
				radiusM = radiusM,
				polarDeg = polarDeg
			};
		}
		r.piecesMassKg = piecesMass.Value;
		r.separatedMassKg = separatedMass.Value;
		r.separatedMassRatio = separatedMass.Value / r.casingMassKg;
		r.meanSpeedLabMps = ((piecesMass.Value > 1e-30) ? (labSpeedSum.Value / piecesMass.Value) : 0.0);
		r.meanSpeedComMps = ((piecesMass.Value > 1e-30) ? (comSpeedSum.Value / piecesMass.Value) : 0.0);
		r.rmsRadiusM = ((piecesMass.Value > 1e-30) ? Math.Sqrt(rmsSum.Value / piecesMass.Value) : 0.0);
		double[] masses = new double[pieces.Count];
		for (int l = 0; l < pieces.Count; l++)
		{
			masses[l] = r.pieces[l].massKg;
		}
		Array.Sort(masses);
		for (int m2 = 0; m2 < Quantiles.Length; m2++)
		{
			r.massQuantilesKg[m2] = ((masses.Length == 0) ? 0.0 : NearestRank(masses, Quantiles[m2]));
		}
		List<int> particleCounts = new List<int>(Math.Max(1, pieces.Count - 1));
		for (int n = 1; n < pieces.Count; n++)
		{
			particleCounts.Add(r.pieces[n].particles);
			if (r.pieces[n].massKg > r.casingMassKg * BigPieceFraction)
			{
				r.piecesAboveBig++;
			}
			if (r.pieces[n].massKg > r.casingMassKg * HugePieceFraction)
			{
				r.piecesAboveHuge++;
			}
			if (r.pieces[n].particles > r.maxPieceParticles)
			{
				r.maxPieceParticles = r.pieces[n].particles;
			}
		}
		particleCounts.Sort();
		if (particleCounts.Count != 0)
		{
			int mid = Mathf.RoundToInt((particleCounts.Count - 1) * 0.5f);
			if (mid < 0)
			{
				mid = 0;
			}
			else if (mid > particleCounts.Count - 1)
			{
				mid = particleCounts.Count - 1;
			}
			r.medianPieceParticles = particleCounts[mid];
		}
		Dictionary<int, int> expCounts = new Dictionary<int, int>();
		Dictionary<int, double> expMass = new Dictionary<int, double>();
		for (int num = 0; num < pieces.Count; num++)
		{
			double grams = r.pieces[num].massKg * 1000.0;
			if (grams <= 0.0)
			{
				continue;
			}
			int exp = (int)Math.Floor(Math.Log10(grams));
			expCounts.TryGetValue(exp, out int count);
			expCounts[exp] = count + 1;
			expMass.TryGetValue(exp, out double sum);
			expMass[exp] = sum + r.pieces[num].massKg;
		}
		List<int> exps = new List<int>(expCounts.Keys);
		exps.Sort();
		r.decades = new Decade[exps.Count];
		for (int num2 = 0; num2 < exps.Count; num2++)
		{
			r.decades[num2] = new Decade
			{
				exp10 = exps[num2],
				count = expCounts[exps[num2]],
				massKg = expMass[exps[num2]]
			};
		}
		r.speedQuantilesLabMps = WeightedQuantiles(r.pieces, r.piecesMassKg, lab: true);
		r.speedQuantilesComMps = WeightedQuantiles(r.pieces, r.piecesMassKg, lab: false);
		RadarKahan[] polar = new RadarKahan[PolarBins];
		RadarKahan centreSum = default(RadarKahan);
		for (int num3 = 0; num3 < pieces.Count; num3++)
		{
			if (0.0 > r.pieces[num3].polarDeg)
			{
				centreSum.Add(r.pieces[num3].massKg);
				continue;
			}
			int bin = (int)(r.pieces[num3].polarDeg / 10.0);
			if (bin < 0)
			{
				bin = 0;
			}
			else if (bin >= PolarBins)
			{
				bin = PolarBins - 1;
			}
			polar[bin].Add(r.pieces[num3].massKg);
		}
		double total = ((r.piecesMassKg > 1e-30) ? r.piecesMassKg : 1.0);
		for (int num4 = 0; num4 < PolarBins; num4++)
		{
			r.polarMassFraction[num4] = polar[num4].Value / total;
		}
		r.polarAtCentre = centreSum.Value / total;
		r.ok = true;
		return r;
	}

	public double MassFractionBeyondPlane(Vector3 pointM, Vector3 normal, bool separatedOnly)
	{
		if (!ok || pieces.Length == 0)
		{
			return 0.0;
		}
		double nx = normal.x;
		double ny = normal.y;
		double nz = normal.z;
		double len = Math.Sqrt(ny * ny + nx * nx + nz * nz);
		if (len < 1e-12)
		{
			return 0.0;
		}
		nx /= len;
		ny /= len;
		nz /= len;
		RadarKahan total = default(RadarKahan);
		RadarKahan beyond = default(RadarKahan);
		for (int i = separatedOnly ? 1 : 0; i < pieces.Length; i++)
		{
			total.Add(pieces[i].massKg);
			double d = (double)(pieces[i].centroid.x - pointM.x) * nx + (double)(pieces[i].centroid.y - pointM.y) * ny + (double)(pieces[i].centroid.z - pointM.z) * nz;
			if (d > 0.0)
			{
				beyond.Add(pieces[i].massKg);
			}
		}
		if (total.Value > 1e-30)
		{
			return beyond.Value / total.Value;
		}
		return 0.0;
	}

	public List<string> Report(int topN = 10)
	{
		List<string> result = new List<string>();
		if (!ok)
		{
			result.Add("[осколки " + tag + "] ОТКАЗ ПРИБОРА [" + refusalName + "]: " + refusal);
			return result;
		}
		result.Add(string.Format("[осколки {0}] кусков {1}; масса корпуса {2:F2} г; ", tag, pieceCount, casingMassKg * 1000.0) + string.Format("вещество трещин {0} частиц ({1:F2} г, ", crackedParticles, crackMassKg * 1000.0) + string.Format("{0:P2} массы); ", (casingMassKg > 0.0) ? (crackMassKg / casingMassKg) : 0.0) + string.Format("сумма кусков {0:F2} г; частиц в теле {1}", piecesMassKg * 1000.0, bodyParticles));
		result.Add(string.Format("  §2.4 отделившаяся масса: {0:F2} г из {1:F2} г = ", separatedMassKg * 1000.0, casingMassKg * 1000.0) + string.Format("{0:P2}; главный кусок {1:F2} г ({2} частиц)", separatedMassRatio, mainMassKg * 1000.0, mainParticles));
		result.Add("  §2.1 массы кусков, квантили (г): " + QLine(massQuantilesKg, 1000.0, "F4") + string.Format("; крупнее 1 % корпуса {0} шт., крупнее 10 % {1} шт.", piecesAboveBig, piecesAboveHuge));
		result.Add(string.Format("  §2.1 размер НЕглавного куска в ЧАСТИЦАХ: медиана {0}, max {1} ", medianPieceParticles, maxPieceParticles) + "(S13: размер куска задаёт решётка — читать массы только рядом с этим числом)");
		StringBuilder decadesSb = new StringBuilder("  §2.1 по декадам массы:");
		for (int i = 0; i < decades.Length; i++)
		{
			decadesSb.Append(string.Format("  10^{0} г: {1} шт. / {2:F2} г;", decades[i].exp10, decades[i].count, decades[i].massKg * 1000.0));
		}
		result.Add(decadesSb.ToString());
		result.Add("  §2.2 |v| массо-взвешенные квантили, лаборатория (м/с): " + QLine(speedQuantilesLabMps, 1.0, "F0") + string.Format("; среднее {0:F0}", meanSpeedLabMps));
		result.Add("  §2.2 |v| массо-взвешенные квантили, система корпуса до разрыва V = " + string.Format("({0:F0}, {1:F0}, {2:F0}) м/с: ", comVelocityMps.x, comVelocityMps.y, comVelocityMps.z) + QLine(speedQuantilesComMps, 1.0, "F0") + string.Format("; среднее {0:F0}, max {1:F0}", meanSpeedComMps, maxSpeedComMps));
		result.Add(string.Format("  §2.3 разлёт: RMS радиус {0:F1} мм от центра ", rmsRadiusM * 1000.0) + string.Format("({0:F0}, {1:F0}, {2:F0}) мм; ", burstCentreM.x * 1000.0, burstCentreM.y * 1000.0, burstCentreM.z * 1000.0) + string.Format("ось ({0:F3}, {1:F3}, {2:F3}); в центре {3:P1} массы", axis.x, axis.y, axis.z, polarAtCentre));
		StringBuilder polarSb = new StringBuilder("  §2.3 полярный угол к оси, доля МАССЫ по 10 градусов:");
		for (int j = 0; j < PolarBins; j++)
		{
			if (polarMassFraction[j] > 0.0005)
			{
				polarSb.Append(string.Format("  {0}-{1}: {2:P1};", j * 10, j * 10 + 10, polarMassFraction[j]));
			}
		}
		result.Add(polarSb.ToString());
		for (int k = 1; k < pieces.Length && k - 1 < topN; k++)
		{
			result.Add(string.Format("    кусок {0}: {1:F3} г, {2} частиц, ", pieces[k].index, pieces[k].massKg * 1000.0, pieces[k].particles) + string.Format("|v| {0:F0} м/с (в системе корпуса {1:F0}), ", pieces[k].speedLabMps, pieces[k].speedComMps) + string.Format("R {0:F1} мм, угол {1:F0} град.", pieces[k].radiusM * 1000.0, (pieces[k].polarDeg < 0.0) ? (-1.0) : pieces[k].polarDeg));
		}
		if (pieces.Length <= 1)
		{
			result.Add("    НЕглавных кусков нет: корпус не разделился");
		}
		return result;
	}

	private SuperFragmentStats Refuse(string name, string why)
	{
		ok = false;
		refusalName = name;
		refusal = why;
		return this;
	}

	private static double NearestRank(double[] sortedAscending, double q)
	{
		int num = Mathf.RoundToInt((float)((double)(sortedAscending.Length - 1) * q));
		if (num < 0)
		{
			num = 0;
		}
		if (num > sortedAscending.Length - 1)
		{
			num = sortedAscending.Length - 1;
		}
		return sortedAscending[num];
	}

	private static double[] WeightedQuantiles(PieceRow[] rows, double totalMass, bool lab)
	{
		double[] result = new double[Quantiles.Length];
		if (rows.Length == 0 || totalMass <= 1e-30)
		{
			return result;
		}
		int[] idx = new int[rows.Length];
		for (int i = 0; i < rows.Length; i++)
		{
			idx[i] = i;
		}
		Array.Sort(idx, (int a, int b) => (lab ? rows[a].speedLabMps : rows[a].speedComMps).CompareTo(lab ? rows[b].speedLabMps : rows[b].speedComMps));
		RadarKahan sum = default(RadarKahan);
		int qi = 0;
		for (int j = 0; j < idx.Length; j++)
		{
			sum.Add(rows[idx[j]].massKg);
			double cum = sum.Value / totalMass;
			while (qi < Quantiles.Length && cum >= Quantiles[qi])
			{
				result[qi] = (lab ? rows[idx[j]].speedLabMps : rows[idx[j]].speedComMps);
				qi++;
			}
		}
		double last = (lab ? rows[idx[idx.Length - 1]].speedLabMps : rows[idx[idx.Length - 1]].speedComMps);
		for (; qi < Quantiles.Length; qi++)
		{
			result[qi] = last;
		}
		return result;
	}

	private static string QLine(double[] v, double scale, string fmt)
	{
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < Quantiles.Length; i++)
		{
			if (i > 0)
			{
				sb.Append(" / ");
			}
			sb.Append(string.Format("p{0:F0} {1}", Quantiles[i] * 100.0, (v[i] * scale).ToString(fmt)));
		}
		return sb.ToString();
	}
}
