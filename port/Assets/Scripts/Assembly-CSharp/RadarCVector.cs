using Unity.Mathematics;

public readonly struct RadarCVector
{
	public readonly Complex64 X;

	public readonly Complex64 Y;

	public readonly Complex64 Z;

	public static RadarCVector Zero => default;

	public RadarCVector(Complex64 x, Complex64 y, Complex64 z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public static RadarCVector FromReal(double3 value)
	{
		return new RadarCVector(new Complex64(value.x), new Complex64(value.y), new Complex64(value.z));
	}

	public static RadarCVector operator +(RadarCVector a, RadarCVector b)
	{
		return new RadarCVector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
	}

	public static RadarCVector operator -(RadarCVector a, RadarCVector b)
	{
		return new RadarCVector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
	}

	public static RadarCVector operator *(RadarCVector a, Complex64 b)
	{
		return new RadarCVector(a.X * b, a.Y * b, a.Z * b);
	}

	public static RadarCVector operator *(RadarCVector a, double b)
	{
		return new RadarCVector(a.X * b, a.Y * b, a.Z * b);
	}

	public static RadarCVector operator *(double b, RadarCVector a)
	{
		return new RadarCVector(b * a.X, b * a.Y, b * a.Z);
	}

	public Complex64 Dot(double3 b)
	{
		return X * b.x + Y * b.y + Z * b.z;
	}

	public static RadarCVector Cross(double3 a, RadarCVector b)
	{
		return new RadarCVector(a.y * b.Z - a.z * b.Y, a.z * b.X - a.x * b.Z, a.x * b.Y - a.y * b.X);
	}
}
