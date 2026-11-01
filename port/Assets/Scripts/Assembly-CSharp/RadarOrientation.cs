using System;
using Unity.Mathematics;

public readonly struct RadarOrientation : IEquatable<RadarOrientation>
{
	private static readonly RadarOrientation XQuarter = new RadarOrientation(1, 0, 0, 0, 0, -1, 0, 1, 0);

	private static readonly RadarOrientation YQuarter = new RadarOrientation(0, 0, 1, 0, 1, 0, -1, 0, 0);

	private static readonly RadarOrientation ZQuarter = new RadarOrientation(0, -1, 0, 1, 0, 0, 0, 0, 1);

	public readonly int M00;

	public readonly int M01;

	public readonly int M02;

	public readonly int M10;

	public readonly int M11;

	public readonly int M12;

	public readonly int M20;

	public readonly int M21;

	public readonly int M22;

	public static RadarOrientation Identity => new RadarOrientation(1, 0, 0, 0, 1, 0, 0, 0, 1);

	public bool IsIdentity
	{
		get
		{
			return M00 == 1 && M01 == 0 && M02 == 0 && M10 == 0 && M11 == 1 && M12 == 0 && M20 == 0 && M21 == 0 && M22 == 1;
		}
	}

	private RadarOrientation(int m00, int m01, int m02, int m10, int m11, int m12, int m20, int m21, int m22)
	{
		M00 = m00;
		M01 = m01;
		M02 = m02;
		M10 = m10;
		M11 = m11;
		M12 = m12;
		M20 = m20;
		M21 = m21;
		M22 = m22;
	}

	public RadarOrientation RotateX()
	{
		return Multiply(XQuarter, this);
	}

	public RadarOrientation RotateY()
	{
		return Multiply(YQuarter, this);
	}

	public RadarOrientation RotateZ()
	{
		return Multiply(ZQuarter, this);
	}

	public double3 Apply(double3 value)
	{
		return new double3(M00 * value.x + M01 * value.y + M02 * value.z, M10 * value.x + M11 * value.y + M12 * value.z, M20 * value.x + M21 * value.y + M22 * value.z);
	}

	public static RadarOrientation Multiply(RadarOrientation a, RadarOrientation b)
	{
		return new RadarOrientation(a.M00 * b.M00 + a.M01 * b.M10 + a.M02 * b.M20, a.M00 * b.M01 + a.M01 * b.M11 + a.M02 * b.M21, a.M00 * b.M02 + a.M01 * b.M12 + a.M02 * b.M22, a.M10 * b.M00 + a.M11 * b.M10 + a.M12 * b.M20, a.M10 * b.M01 + a.M11 * b.M11 + a.M12 * b.M21, a.M10 * b.M02 + a.M11 * b.M12 + a.M12 * b.M22, a.M20 * b.M00 + a.M21 * b.M10 + a.M22 * b.M20, a.M20 * b.M01 + a.M21 * b.M11 + a.M22 * b.M21, a.M20 * b.M02 + a.M21 * b.M12 + a.M22 * b.M22);
	}

	public bool Equals(RadarOrientation other)
	{
		return M00 == other.M00 && M01 == other.M01 && M02 == other.M02 && M10 == other.M10 && M11 == other.M11 && M12 == other.M12 && M20 == other.M20 && M21 == other.M21 && M22 == other.M22;
	}

	public override bool Equals(object obj)
	{
		if (obj is RadarOrientation)
		{
			return Equals((RadarOrientation)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int hash = 17;
		hash = hash * 31 + M00;
		hash = hash * 31 + M01;
		hash = hash * 31 + M02;
		hash = hash * 31 + M10;
		hash = hash * 31 + M11;
		hash = hash * 31 + M12;
		hash = hash * 31 + M20;
		hash = hash * 31 + M21;
		hash = hash * 31 + M22;
		return hash;
	}

	public static bool operator ==(RadarOrientation a, RadarOrientation b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(RadarOrientation a, RadarOrientation b)
	{
		return !a.Equals(b);
	}
}
