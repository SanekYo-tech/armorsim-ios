using System;

public readonly struct Complex64
{
	public readonly double Real;

	public readonly double Imaginary;

	public double MagnitudeSquared => Real * Real + Imaginary * Imaginary;

	public double Magnitude => Math.Sqrt(Real * Real + Imaginary * Imaginary);

	public static Complex64 Zero => new Complex64(0.0, 0.0);

	public Complex64(double real, double imaginary = 0.0)
	{
		Real = real;
		Imaginary = imaginary;
	}

	public static Complex64 Phase(double radians)
	{
		return new Complex64(Math.Cos(radians), Math.Sin(radians));
	}

	public static Complex64 operator +(Complex64 a, Complex64 b)
	{
		return new Complex64(a.Real + b.Real, a.Imaginary + b.Imaginary);
	}

	public static Complex64 operator -(Complex64 a, Complex64 b)
	{
		return new Complex64(a.Real - b.Real, a.Imaginary - b.Imaginary);
	}

	public static Complex64 operator *(Complex64 a, Complex64 b)
	{
		return new Complex64(a.Real * b.Real - a.Imaginary * b.Imaginary, a.Real * b.Imaginary + a.Imaginary * b.Real);
	}

	public static Complex64 operator *(Complex64 a, double b)
	{
		return new Complex64(a.Real * b, a.Imaginary * b);
	}

	public static Complex64 operator *(double a, Complex64 b)
	{
		return new Complex64(a * b.Real, a * b.Imaginary);
	}

	public static Complex64 operator /(Complex64 a, double b)
	{
		return new Complex64(a.Real / b, a.Imaginary / b);
	}

	public override string ToString()
	{
		return string.Format("({0:R}, {1:R})", Real, Imaginary);
	}
}
