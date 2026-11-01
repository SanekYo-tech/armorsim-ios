using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class RadarCoating
{
	public enum Kind
	{
		Metal = 0,
		Salisbury = 1,
		Jaumann = 2,
		Custom = 3,
		Fixed = 4
	}

	private struct Element
	{
		public bool Sheet;

		public Complex64 Eps;

		public Complex64 Mu;

		public double Thickness;

		public double Resistance;
	}

	public const double Eta0 = 376.730313668;

	public const double TangentLimit = 20.0;

	public const double JaumannOuterFraction = 1.0 / 3.0;

	public const double NormalIncidenceFloor = 1E-09;

	public readonly Kind Id;

	public readonly double F0Hz;

	private readonly Element[] stack;

	private readonly Complex64 fixedTE;

	private readonly Complex64 fixedTM;

	private static long tangentLimitHits;

	private static long normalIncidences;

	private static long maxTangentRatioBits;

	private static long maxGammaBits;

	public static readonly RadarCoating Metal;

	public static long TangentLimitHits => Interlocked.Read(ref tangentLimitHits);

	public static double MaxTangentRatio => BitConverter.Int64BitsToDouble(Interlocked.Read(ref maxTangentRatioBits));

	public static long NormalIncidences => Interlocked.Read(ref normalIncidences);

	public static double MaxGamma => BitConverter.Int64BitsToDouble(Interlocked.Read(ref maxGammaBits));

	public bool IsMetal => Id == Kind.Metal;

	public string ScenarioId
	{
		get
		{
			if (Id == Kind.Metal)
			{
				return "metal";
			}
			if (Id == Kind.Salisbury)
			{
				return "salisbury";
			}
			if (Id == Kind.Jaumann)
			{
				return "jaumann";
			}
			return "custom";
		}
	}

	public static void ResetGuards()
	{
		Interlocked.Exchange(ref tangentLimitHits, 0L);
		Interlocked.Exchange(ref normalIncidences, 0L);
		Interlocked.Exchange(ref maxTangentRatioBits, 0L);
		Interlocked.Exchange(ref maxGammaBits, 0L);
	}

	private RadarCoating(Kind id, double f0, Element[] layers, Complex64 te = default(Complex64), Complex64 tm = default(Complex64))
	{
		Id = id;
		F0Hz = f0;
		stack = layers;
		fixedTE = te;
		fixedTM = tm;
	}

	private static Element Air(double d)
	{
		return new Element
		{
			Eps = new Complex64(1.0),
			Mu = new Complex64(1.0),
			Thickness = d
		};
	}

	private static Element Sheet(double r)
	{
		return new Element
		{
			Sheet = true,
			Resistance = r
		};
	}

	public static RadarCoating Salisbury(double f0)
	{
		Require(f0);
		Element[] layers = new Element[2];
		layers[0] = Air(299792458.0 / f0 * 0.25);
		layers[1] = Sheet(Eta0);
		return new RadarCoating(Kind.Salisbury, f0, layers);
	}

	public static RadarCoating Jaumann(double f0)
	{
		Require(f0);
		Element[] layers = new Element[4];
		layers[0] = Air(299792458.0 / f0 * 0.25);
		layers[1] = Sheet(Eta0 * (1.0 - JaumannOuterFraction));
		layers[2] = Air(299792458.0 / f0 * 0.25);
		layers[3] = Sheet(Eta0 / JaumannOuterFraction);
		return new RadarCoating(Kind.Jaumann, f0, layers);
	}

	public static RadarCoating Custom(double epsRe, double epsLoss, double muRe, double muLoss, double thickness)
	{
		double[] array = new double[5] { epsRe, epsLoss, muRe, muLoss, thickness };
		for (int i = 0; i < array.Length; i++)
		{
			if (double.IsNaN(array[i]) || double.IsInfinity(array[i]))
			{
				throw new ArgumentOutOfRangeException("thickness", "non-finite layer");
			}
		}
		if (epsLoss < 0.0 || muLoss < 0.0 || thickness <= 0.0)
		{
			throw new ArgumentOutOfRangeException("thickness", "passive layer needs losses ≥ 0 and d > 0");
		}
		Element[] array2 = new Element[1];
		array2[0] = new Element
		{
			Eps = new Complex64(epsRe, -epsLoss),
			Mu = new Complex64(muRe, -muLoss),
			Thickness = thickness
		};
		return new RadarCoating(Kind.Custom, 0.0, array2);
	}

	public static RadarCoating Stack(params object[] elements)
	{
		Element[] array = new Element[elements.Length];
		for (int i = 0; i < elements.Length; i++)
		{
			if (elements[i] is double num)
			{
				array[i] = Sheet(num);
				continue;
			}
			Complex64[] array2 = elements[i] as Complex64[];
			if (array2 == null || array2.Length != 3)
			{
				throw new ArgumentException("element " + i);
			}
			array[i] = new Element
			{
				Eps = array2[0],
				Mu = array2[1],
				Thickness = array2[2].Real
			};
		}
		return new RadarCoating(Kind.Custom, 0.0, array);
	}

	public static RadarCoating Fixed(Complex64 gammaTE, Complex64 gammaTM)
	{
		return new RadarCoating(Kind.Fixed, 0.0, Array.Empty<Element>(), gammaTE, gammaTM);
	}

	private static void Require(double f0)
	{
		if (f0 > 0.0 && !double.IsInfinity(f0))
		{
			return;
		}
		throw new ArgumentOutOfRangeException("f0");
	}

	public void Gamma(double k0, double cosTheta, out Complex64 te, out Complex64 tm)
	{
		if (Id == Kind.Metal)
		{
			te = new Complex64(-1.0);
			tm = new Complex64(-1.0);
			return;
		}
		if (Id == Kind.Fixed)
		{
			te = fixedTE;
			tm = fixedTM;
			return;
		}
		double num = Math.Max(1e-12, Math.Min(1.0, Math.Abs(cosTheta)));
		double num2 = 1.0 - num * num;
		te = Recursion(k0, num2, num, transverseElectric: true);
		tm = Recursion(k0, num2, num, transverseElectric: false);
		double num3 = Math.Max(te.Magnitude, tm.Magnitude);
		RaiseMax(ref maxGammaBits, num3);
		if (num3 > 1.000000001)
		{
			throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "passive coating gave |Γ| = {0:R} > 1 (cos θ = {1:R}, k0 = {2:R})", num3, cosTheta, k0));
		}
	}

	private Complex64 Recursion(double k0, double sin2, double cos, bool transverseElectric)
	{
		Complex64 gamma = default(Complex64);
		for (int i = 0; i < stack.Length; i++)
		{
			Element element = stack[i];
			if (element.Sheet)
			{
				Complex64 value = new Complex64(element.Resistance);
				gamma = Div(value * gamma, gamma + value);
				continue;
			}
			Complex64 eps = element.Eps;
			Complex64 mu = element.Mu;
			Complex64 complex = k0 * Sqrt(eps * mu - new Complex64(sin2));
			if (complex.Imaginary > 0.0)
			{
				complex = complex * -1.0;
			}
			Complex64 z;
			if (transverseElectric)
			{
				z = Div(k0 * Eta0 * mu, complex);
			}
			else
			{
				z = Div(Eta0 * complex, k0 * eps);
			}
			double num = complex.Imaginary * element.Thickness;
			RaiseMax(ref maxTangentRatioBits, Math.Abs(num) / TangentLimit);
			Complex64 complex2;
			if (num < -TangentLimit)
			{
				Interlocked.Increment(ref tangentLimitHits);
				complex2 = new Complex64(0.0, -1.0);
			}
			else if (num > TangentLimit)
			{
				Interlocked.Increment(ref tangentLimitHits);
				complex2 = new Complex64(0.0, 1.0);
			}
			else
			{
				complex2 = Tan(complex * element.Thickness);
			}
			Complex64 imaginaryOne = new Complex64(0.0, 1.0);
			gamma = Div(z * (gamma + imaginaryOne * z * complex2), z + imaginaryOne * gamma * complex2);
		}
		Complex64 eta = (transverseElectric ? new Complex64(Eta0 / cos) : new Complex64(Eta0 * cos));
		return Div(gamma - eta, gamma + eta);
	}

	public RadarCVector Reflect(RadarCVector field, double3 direction, double3 normal, double k0, out double3 reflectedDirection)
	{
		double num = direction.x * normal.x + direction.y * normal.y + direction.z * normal.z;
		reflectedDirection = direction - 2.0 * num * normal;
		Gamma(k0, Math.Abs(num), out Complex64 te, out Complex64 tm);
		return ReflectWith(te, tm, field, direction, normal, reflectedDirection);
	}

	public static RadarCVector ReflectWith(Complex64 te, Complex64 tm, RadarCVector field, double3 direction, double3 normal, double3 reflectedDirection)
	{
		double3 crossDN = new double3(direction.y * normal.z - direction.z * normal.y, direction.z * normal.x - direction.x * normal.z, direction.x * normal.y - direction.y * normal.x);
		double num = Math.Sqrt(crossDN.x * crossDN.x + crossDN.y * crossDN.y + crossDN.z * crossDN.z);
		double3 h;
		if (num < NormalIncidenceFloor)
		{
			Interlocked.Increment(ref normalIncidences);
			h = AnyPerpendicular(direction);
		}
		else
		{
			h = crossDN / num;
		}
		double3 v = new double3(h.y * direction.z - h.z * direction.y, h.z * direction.x - h.x * direction.z, h.x * direction.y - h.y * direction.x);
		Complex64 dotH = field.Dot(h);
		Complex64 eh = te * dotH;
		double3 crossH = new double3(h.y * reflectedDirection.z - h.z * reflectedDirection.y, h.z * reflectedDirection.x - h.x * reflectedDirection.z, h.x * reflectedDirection.y - h.y * reflectedDirection.x);
		Complex64 dotV = field.Dot(v);
		Complex64 ev = dotV * tm * -1.0;
		return RadarCVector.FromReal(crossH) * ev + RadarCVector.FromReal(h) * eh;
	}

	private static double3 AnyPerpendicular(double3 v)
	{
		double3 axis = new double3(1.0, 0.0, 0.0);
		if (Math.Abs(v.y) < 0.9)
		{
			axis = new double3(0.0, 1.0, 0.0);
		}
		double3 cross = new double3(axis.y * v.z - axis.z * v.y, axis.z * v.x - axis.x * v.z, axis.x * v.y - axis.y * v.x);
		double num = 1.0 / Math.Sqrt(cross.x * cross.x + cross.y * cross.y + cross.z * cross.z);
		return new double3(num * cross.x, num * cross.y, num * cross.z);
	}

	public byte[] CanonicalBytes()
	{
		using (MemoryStream memoryStream = new MemoryStream())
		{
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8))
			{
				binaryWriter.Write(Encoding.ASCII.GetBytes("RADCOAT1"));
				binaryWriter.Write((int)Id);
				binaryWriter.Write(F0Hz);
				binaryWriter.Write(stack.Length);
				for (int i = 0; i < stack.Length; i++)
				{
					binaryWriter.Write(stack[i].Sheet);
					binaryWriter.Write(stack[i].Eps.Real);
					binaryWriter.Write(stack[i].Eps.Imaginary);
					binaryWriter.Write(stack[i].Mu.Real);
					binaryWriter.Write(stack[i].Mu.Imaginary);
					binaryWriter.Write(stack[i].Thickness);
					binaryWriter.Write(stack[i].Resistance);
				}
				binaryWriter.Write(fixedTE.Real);
				binaryWriter.Write(fixedTE.Imaginary);
				binaryWriter.Write(fixedTM.Real);
				binaryWriter.Write(fixedTM.Imaginary);
				binaryWriter.Flush();
				return memoryStream.ToArray();
			}
		}
	}

	public string Describe()
	{
		if (Id == Kind.Metal)
		{
			return "metal";
		}
		return string.Format(CultureInfo.InvariantCulture, "{0} f0={1:0.###} GHz layers={2}", ScenarioId, F0Hz / 1e9, stack.Length);
	}

	internal static Complex64 Div(Complex64 a, Complex64 b)
	{
		if (Math.Abs(b.Imaginary) <= Math.Abs(b.Real))
		{
			double num = b.Imaginary / b.Real;
			double num2 = num * b.Imaginary + b.Real;
			return new Complex64((num * a.Imaginary + a.Real) / num2, (a.Imaginary - num * a.Real) / num2);
		}
		double num3 = b.Real / b.Imaginary;
		double num4 = num3 * b.Real + b.Imaginary;
		return new Complex64((num3 * a.Real + a.Imaginary) / num4, (num3 * a.Imaginary - a.Real) / num4);
	}

	internal static Complex64 Sqrt(Complex64 z)
	{
		double magnitude = z.Magnitude;
		if (magnitude == 0.0)
		{
			return new Complex64(0.0, 0.0);
		}
		double num = Math.Sqrt((z.Real + magnitude) * 0.5);
		double num2 = Math.Sqrt((magnitude - z.Real) * 0.5);
		if (z.Imaginary < 0.0 || (z.Imaginary == 0.0 && BitConverter.DoubleToInt64Bits(z.Imaginary) < 0))
		{
			num2 = -num2;
		}
		return new Complex64(num, num2);
	}

	internal static Complex64 Tan(Complex64 z)
	{
		double num = z.Real + z.Real;
		double num2 = z.Imaginary + z.Imaginary;
		double num3 = Math.Cos(num) + Math.Cosh(num2);
		return new Complex64(Math.Sin(num) / num3, Math.Sinh(num2) / num3);
	}

	private static void RaiseMax(ref long bits, double value)
	{
		if (double.IsNaN(value))
		{
			return;
		}
		long num = BitConverter.DoubleToInt64Bits(Math.Max(0.0, value));
		long num2;
		do
		{
			num2 = Interlocked.Read(ref bits);
			if (num2 >= num)
			{
				return;
			}
		}
		while (Interlocked.CompareExchange(ref bits, num, num2) != num2);
	}

	static RadarCoating()
	{
		Metal = new RadarCoating(Kind.Metal, 0.0, Array.Empty<Element>());
	}
}
