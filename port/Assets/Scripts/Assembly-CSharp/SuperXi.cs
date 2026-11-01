using System;
using System.Collections.Generic;
using System.Globalization;

public static class SuperXi
{
	public struct ComposeCount
	{
		public long n;

		public long floorHits;

		public long ceilHits;

		public double multMin;

		public double multMax;

		public static ComposeCount Fresh => new ComposeCount
		{
			multMin = double.PositiveInfinity,
			multMax = double.NegativeInfinity
		};

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "n = {0}, множитель [{1:G6}; {2:G6}], на полу {3}, на потолке {4}", n, multMin, multMax, floorHits, ceilHits);
		}
	}

	public struct Stats
	{
		public int n;

		public int clipped;

		public double mean;

		public double cov;

		public double min;

		public double max;

		public double ks;

		public override string ToString()
		{
			double clippedShare = ((n > 0) ? ((double)clipped / n) : 0.0);
			return string.Format(CultureInfo.InvariantCulture, "n = {0}, среднее {1:F6}, CoV {2:F4}, min {3:F4}, max {4:F4}, на полу {5} ({6:E2}), KS {7:E3}", n, mean, cov, min, max, clipped, clippedShare, ks);
		}
	}

	public const float Floor = 0.1f;

	public const uint DefaultSalt = 1395785729u;

	public const string Declaration = "Вейбулл — предельное распределение слабейшего звена (минимум прочностей многих независимых дефектов в объёме частицы); единственный параметр формы m задаёт ширину (CoV ≈ 1.2/m); ASSUMED";

	public const string FloorDeclaration = "пол 0.10 ASSUMED, объявлен до первого прогона: ни одна частица не рождается разрушенной — ε_f·ξ ⩾ 0.1·ε_f";

	private const int IndexBits = 20;

	private const ulong IndexMask = 1048575uL;

	public const int IndexMin = -524288;

	public const int IndexMaxExclusive = 524288;

	private const double TwoPowMinus53 = 1.1102230246251565E-16;

	private const double OneMinusUlp = 0.9999999999999999;

	private static readonly Dictionary<double, double> LambdaCache;

	private static readonly double[] Lanczos;

	public const float CeilingS43 = 10f;

	public const string CeilingDeclaration = "потолок 10 ASSUMED (S43 P1.3), только при масштабировании ВКЛ: множитель выше потолка — счётчик, не молчание";

	static SuperXi()
	{
		LambdaCache = new Dictionary<double, double>();
		Lanczos = new double[9]
		{
			0.9999999999998099,
			676.5203681218851,
			-1259.1392167224028,
			771.3234287776531,
			-176.6150291621406,
			12.507343278686905,
			-0.13857109526572012,
			9.984369578019572E-06,
			1.5056327351493116E-07
		};
	}

	public static ulong PackLatKey(int a, int b, int c, int sub)
	{
		CheckIndex(a, "a");
		CheckIndex(b, "b");
		CheckIndex(c, "c");
		if ((uint)sub > 15u)
		{
			throw new ArgumentOutOfRangeException("sub", string.Format("SUPER S32: номер подъячейки ключа ξ = {0} вне 0..15 — ключ решётки наложился бы на соседний.", sub));
		}
		return (((ulong)(c & 0xFFFFF) | ((ulong)(uint)sub << 20)) << 20 | (ulong)(b & 0xFFFFF)) << 20 | (ulong)(a & 0xFFFFF);
	}

	private static void CheckIndex(int v, string name)
	{
		if ((uint)(v + 524288) > 1048575u)
		{
			throw new ArgumentOutOfRangeException(name, string.Format("SUPER S32: индекс решётки {0} = {1} вне [{2}; {3}) — 20 бит ключа ξ ", name, v, -524288, 524288) + "не различили бы его с другим; две частицы получили бы один ξ молча.");
		}
	}

	private static ulong Mix(ulong z)
	{
		z += 11400714819323198485uL;
		z = (z ^ (z >> 30)) * 13787848793156543929uL;
		z = (z ^ (z >> 27)) * 10723151780598845931uL;
		return z ^ (z >> 31);
	}

	public static double Uniform(uint salt, int matId, int body, ulong latKey)
	{
		if (matId < 0)
		{
			throw new ArgumentOutOfRangeException("matId", string.Format("SUPER S32: номер вещества {0} < 0 в ключе ξ.", matId));
		}
		if (body < 0)
		{
			throw new ArgumentOutOfRangeException("body", string.Format("SUPER S32: номер тела {0} < 0 в ключе ξ.", body));
		}
		ulong z = Mix(salt);
		z = Mix(z ^ (uint)matId);
		z = Mix(z ^ (uint)body);
		z = Mix(z ^ latKey);
		double u = ((double)(z >> 11) + 0.5) * TwoPowMinus53;
		if (u >= 1.0)
		{
			u = OneMinusUlp;
		}
		return u;
	}

	public static double Lambda(double m, double floor = 0.10000000149011612)
	{
		CheckModulus(m);
		if (double.IsInfinity(floor) || floor < 0.0 || floor >= 1.0)
		{
			throw new ArgumentException(string.Format("SUPER S32: пол ξ = {0} вне [0; 1) — среднее 1 после среза недостижимо.", floor), "floor");
		}
		bool cache = floor == 0.10000000149011612;
		if (cache)
		{
			lock (LambdaCache)
			{
				if (LambdaCache.TryGetValue(m, out double cached))
				{
					return cached;
				}
			}
		}
		double gamma = Math.Exp(LnGamma(1.0 + 1.0 / m));
		double lam = 1.0 / gamma;
		for (int iteration = 0; iteration < 50; iteration++)
		{
			double next = (1.0 - ClipIntegral(lam, m, floor)) / gamma;
			if (Math.Abs(next - lam) < 1E-15)
			{
				lam = next;
				break;
			}
			lam = next;
		}
		if (!(lam > 0.0) || double.IsInfinity(lam))
		{
			throw new InvalidOperationException(string.Format("SUPER S32: масштаб Вейбулла λ(m = {0}, пол {1}) = {2} — неподвижная точка не сошлась к конечному ", m, floor, lam) + "положительному числу; ξ с таким λ не имел бы среднего 1.");
		}
		if (cache)
		{
			lock (LambdaCache)
			{
				LambdaCache[m] = lam;
			}
		}
		return lam;
	}

	private static void CheckModulus(double m)
	{
		if (double.IsNaN(m) || double.IsInfinity(m) || m <= 0.0)
		{
			throw new ArgumentException(string.Format("SUPER S32: модуль Вейбулла m = {0} — ширина ξ не выбрана (нужно конечное m > 0; довод -s32m).", m), "m");
		}
	}

	private static double WeibullCdf(double x, double lam, double m)
	{
		if (x <= 0.0)
		{
			return 0.0;
		}
		return 1.0 - Math.Exp(-Math.Pow(x / lam, m));
	}

	private static double ClipIntegral(double lam, double m, double floor)
	{
		if (floor <= 0.0)
		{
			return 0.0;
		}
		double h = floor / 2000.0;
		double sum = WeibullCdf(0.0, lam, m) + WeibullCdf(floor, lam, m);
		for (int i = 1; i < 2000; i++)
		{
			double weight = ((i == 1) ? 2.0 : 4.0);
			sum += WeibullCdf(i * h, lam, m) * weight;
		}
		return sum * h / 3.0;
	}

	private static double ClipSecondIntegral(double lam, double m, double floor)
	{
		if (floor <= 0.0)
		{
			return 0.0;
		}
		double h = floor / 2000.0;
		double sum = WeibullCdf(floor, lam, m) * (2.0 * floor);
		for (int i = 1; i < 2000; i++)
		{
			double x = i * h;
			double weight = ((i == 1) ? 2.0 : 4.0);
			sum += WeibullCdf(x, lam, m) * (2.0 * weight * x);
		}
		return sum * h / 3.0;
	}

	private static double LnGamma(double x)
	{
		if (x < 0.5 || double.IsInfinity(x))
		{
			throw new ArgumentOutOfRangeException("x", string.Format("SUPER S32: ln Γ({0}) — вне области формы Ланцоша без отражения.", x));
		}
		x -= 1.0;
		double a = Lanczos[0];
		double t = x + 7.5;
		for (int i = 1; i < 9; i++)
		{
			a += Lanczos[i] / (x + i);
		}
		return Math.Log(a) + Math.Log(6.283185307179586) * 0.5 + (x + 0.5) * Math.Log(t) - t;
	}

	public static float FromUniform(double u, double m)
	{
		if (!(u > 0.0 && u < 1.0))
		{
			throw new ArgumentOutOfRangeException("u", string.Format("SUPER S32: равномерное u = {0} вне открытого (0; 1).", u));
		}
		double raw = Math.Max(Lambda(m) * Math.Pow(-Math.Log(1.0 - u), 1.0 / m), 0.10000000149011612);
		float result = (float)raw;
		if (result < 0.1f || float.IsInfinity(result) || float.IsNaN(result))
		{
			throw new InvalidOperationException(string.Format("SUPER S32: ξ = {0} (u = {1}, m = {2}) — не конечен или ниже пола {3}; делитель ΔD = Δε_p/(ε_f·ξ) взорвался бы.", result, u, m, 0.1f));
		}
		return result;
	}

	public static float For(uint salt, ContinuumMaterialId mat, int body, ulong latKey, double m)
	{
		return FromUniform(Uniform(salt, (int)mat, body, latKey), m);
	}

	public static double RawFromUniform(double u, double m)
	{
		if (!(u > 0.0 && u < 1.0))
		{
			throw new ArgumentOutOfRangeException("u", string.Format("SUPER S43: равномерное u = {0} вне открытого (0; 1).", u));
		}
		return Lambda(m) * Math.Pow(-Math.Log(1.0 - u), 1.0 / m);
	}

	public static double ScaleMultiplier(double vM3, double alpha, double beta, double vRefM3, double hRefM)
	{
		if (double.IsNaN(vM3) || double.IsInfinity(vM3) || vM3 <= 0.0)
		{
			throw new ArgumentException(string.Format("SUPER S43: объём частицы V = {0} м³ — не конечное число > 0; множитель ξ не определён.", vM3), "vM3");
		}
		if (double.IsNaN(alpha) || double.IsInfinity(alpha) || double.IsNaN(beta) || double.IsInfinity(beta))
		{
			throw new ArgumentException(string.Format("SUPER S43: показатели α = {0}, β = {1} не конечны.", alpha, beta), "alpha");
		}
		double mult = 1.0;
		if (alpha != 0.0)
		{
			if (double.IsNaN(vRefM3) || double.IsInfinity(vRefM3) || vRefM3 <= 0.0)
			{
				throw new ArgumentException(string.Format("SUPER S43: α = {0} ≠ 0 при якоре V_ref = {1} м³ — якорь объёма не объявлен.", alpha, vRefM3), "vRefM3");
			}
			mult = Math.Pow(vM3 / vRefM3, -alpha);
		}
		if (beta != 0.0)
		{
			if (double.IsNaN(hRefM) || double.IsInfinity(hRefM) || hRefM <= 0.0)
			{
				throw new ArgumentException(string.Format("SUPER S43: β = {0} ≠ 0 при якоре h_ref = {1} м — якорь шага не объявлен.", beta, hRefM), "hRefM");
			}
			mult *= Math.Pow(hRefM * hRefM * hRefM / vM3, beta / 3.0);
		}
		return mult;
	}

	public static float Compose(bool scatterOn, bool scalingOn, uint salt, ContinuumMaterialId mat, int body, ulong latKey, double m, double mult, ref ComposeCount c)
	{
		if (scalingOn)
		{
			if (double.IsNaN(mult) || double.IsInfinity(mult) || mult <= 0.0)
			{
				throw new ArgumentException(string.Format("SUPER S43: множитель M = {0} — не конечное число > 0.", mult), "mult");
			}
			double raw = (scatterOn ? RawFromUniform(Uniform(salt, (int)mat, body, latKey), m) : 1.0);
			double value = raw * mult;
			if (double.IsNaN(value) || double.IsInfinity(value))
			{
				throw new InvalidOperationException(string.Format("SUPER S43: ξ = {0} (x = {1}, M = {2}, m = {3}) — не конечен; делитель ΔD = Δε_p/(ε_f·ξ) взорвался бы.", value, raw, mult, m));
			}
			if (c.n == 0L)
			{
				c.multMin = double.PositiveInfinity;
				c.multMax = double.NegativeInfinity;
			}
			if (mult < c.multMin)
			{
				c.multMin = mult;
			}
			if (mult > c.multMax)
			{
				c.multMax = mult;
			}
			c.n++;
			if (value < 0.10000000149011612)
			{
				c.floorHits++;
				value = 0.10000000149011612;
			}
			else if (value > 10.0)
			{
				c.ceilHits++;
				value = 10.0;
			}
			return (float)value;
		}
		if (scatterOn)
		{
			return For(salt, mat, body, latKey, m);
		}
		throw new ArgumentException("scatterOn", "SUPER S43: Compose вызван при разбросе ВЫКЛ и масштабировании ВЫКЛ — поля ξ нет, звать его некому.");
	}

	public static double Cdf(double x, double m)
	{
		if (double.IsNaN(x))
		{
			throw new ArgumentException("SUPER S32: CDF ξ в NaN.", "x");
		}
		if (x < 0.0)
		{
			return 0.0;
		}
		return WeibullCdf(x, Lambda(m), m);
	}

	public static double ClippedShare(double m)
	{
		return Cdf(0.10000000149011612, m);
	}

	public static double AnalyticCov(double m)
	{
		double lam = Lambda(m);
		double second = lam * lam * Math.Exp(LnGamma(1.0 + 2.0 / m)) + ClipSecondIntegral(lam, m, 0.10000000149011612) - 1.0;
		return ((second > 0.0) ? Math.Sqrt(second) : 0.0);
	}

	public static Stats Measure(float[] xi, int start, int count, double m, bool withKs = true)
	{
		if (xi == null)
		{
			throw new ArgumentNullException("xi");
		}
		if (start < 0 || count < 0 || (long)start + count > xi.Length)
		{
			throw new ArgumentOutOfRangeException("count", string.Format("SUPER S32: диапазон [{0}; {1}) вне поля ξ из {2}.", start, start + count, xi.Length));
		}
		if (withKs)
		{
			CheckModulus(m);
		}
		Stats stats = new Stats
		{
			n = count,
			min = double.PositiveInfinity,
			max = double.NegativeInfinity
		};
		if (count == 0)
		{
			stats.mean = double.NaN;
			stats.cov = double.NaN;
			stats.min = double.NaN;
			stats.max = double.NaN;
			stats.ks = double.NaN;
			return stats;
		}
		double[] values = new double[count];
		KahanSum sum = default(KahanSum);
		for (int i = 0; i < count; i++)
		{
			float value = xi[start + i];
			if (float.IsNaN(value) || float.IsInfinity(value))
			{
				throw new InvalidOperationException(string.Format("SUPER S32: ξ[{0}] = {1} — поле не конечно.", start + i, value));
			}
			values[i] = value;
			sum.Add(value);
			if (value < stats.min)
			{
				stats.min = value;
			}
			if (value > stats.max)
			{
				stats.max = value;
			}
			if (value <= 0.1f)
			{
				stats.clipped++;
			}
		}
		stats.mean = sum.Value / count;
		KahanSum squares = default(KahanSum);
		for (int i = 0; i < count; i++)
		{
			double d = values[i] - stats.mean;
			squares.Add(d * d);
		}
		double sd = ((count > 1) ? Math.Sqrt(squares.Value / (count - 1)) : 0.0);
		stats.cov = ((stats.mean == 0.0) ? 0.0 : (sd / stats.mean));
		if (!withKs)
		{
			stats.ks = double.NaN;
			return stats;
		}
		Array.Sort(values);
		double ks = 0.0;
		for (int i = 0; i < count;)
		{
			double value = values[i];
			int end = i + 1;
			while (end < count && values[end] == value)
			{
				end++;
			}
			double f = Cdf(value, m);
			double lowerF = ((value > 0.0) ? f : 0.0);
			double d = Math.Max(Math.Abs((double)end / count - f), Math.Abs((double)i / count - lowerF));
			ks = Math.Max(ks, d);
			i = end;
		}
		stats.ks = ks;
		return stats;
	}
}
