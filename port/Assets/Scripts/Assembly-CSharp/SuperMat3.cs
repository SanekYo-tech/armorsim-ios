using System;
using UnityEngine;

public struct SuperMat3
{
	public float m00;

	public float m01;

	public float m02;

	public float m10;

	public float m11;

	public float m12;

	public float m20;

	public float m21;

	public float m22;

	public static SuperMat3 Identity => new SuperMat3
	{
		m00 = 1f,
		m11 = 1f,
		m22 = 1f
	};

	public static SuperMat3 Zero => default;

	public SuperMat3 Transposed => new SuperMat3
	{
		m01 = m10,
		m02 = m20,
		m10 = m01,
		m12 = m21,
		m20 = m02,
		m21 = m12
	};

	public float Trace => m00 + m11 + m22;

	public float Det => m00 * (m11 * m22 - m12 * m21) - m01 * (m10 * m22 - m12 * m20) + m02 * (m10 * m21 - m11 * m20);

	public SuperMat3 Sym => new SuperMat3
	{
		m01 = (m01 + m10) * 0.5f,
		m02 = (m02 + m20) * 0.5f,
		m10 = (m10 + m01) * 0.5f,
		m12 = (m12 + m21) * 0.5f,
		m20 = (m20 + m02) * 0.5f,
		m21 = (m21 + m12) * 0.5f
	};

	public SuperMat3 Skew => new SuperMat3
	{
		m01 = (m01 - m10) * 0.5f,
		m02 = (m02 - m20) * 0.5f,
		m10 = (m10 - m01) * 0.5f,
		m12 = (m12 - m21) * 0.5f,
		m20 = (m20 - m02) * 0.5f,
		m21 = (m21 - m12) * 0.5f
	};

	public static SuperMat3 Outer(Vector3 a, Vector3 b)
	{
		return new SuperMat3
		{
			m00 = a.x * b.x,
			m01 = a.x * b.y,
			m02 = a.x * b.z,
			m10 = a.y * b.x,
			m11 = a.y * b.y,
			m12 = a.y * b.z,
			m20 = a.z * b.x,
			m21 = a.z * b.y,
			m22 = a.z * b.z
		};
	}

	public static SuperMat3 operator +(SuperMat3 a, SuperMat3 b)
	{
		return new SuperMat3
		{
			m00 = a.m00 + b.m00,
			m01 = a.m01 + b.m01,
			m02 = a.m02 + b.m02,
			m10 = a.m10 + b.m10,
			m11 = a.m11 + b.m11,
			m12 = a.m12 + b.m12,
			m20 = a.m20 + b.m20,
			m21 = a.m21 + b.m21,
			m22 = a.m22 + b.m22
		};
	}

	public static SuperMat3 operator -(SuperMat3 a, SuperMat3 b)
	{
		return new SuperMat3
		{
			m00 = a.m00 - b.m00,
			m01 = a.m01 - b.m01,
			m02 = a.m02 - b.m02,
			m10 = a.m10 - b.m10,
			m11 = a.m11 - b.m11,
			m12 = a.m12 - b.m12,
			m20 = a.m20 - b.m20,
			m21 = a.m21 - b.m21,
			m22 = a.m22 - b.m22
		};
	}

	public static SuperMat3 operator *(SuperMat3 a, float k)
	{
		return new SuperMat3
		{
			m00 = a.m00 * k,
			m01 = a.m01 * k,
			m02 = a.m02 * k,
			m10 = a.m10 * k,
			m11 = a.m11 * k,
			m12 = a.m12 * k,
			m20 = a.m20 * k,
			m21 = a.m21 * k,
			m22 = a.m22 * k
		};
	}

	public static SuperMat3 operator *(SuperMat3 a, SuperMat3 b)
	{
		return new SuperMat3
		{
			m00 = a.m00 * b.m00 + a.m01 * b.m10 + a.m02 * b.m20,
			m01 = a.m00 * b.m01 + a.m01 * b.m11 + a.m02 * b.m21,
			m02 = a.m00 * b.m02 + a.m01 * b.m12 + a.m02 * b.m22,
			m10 = a.m10 * b.m00 + a.m11 * b.m10 + a.m12 * b.m20,
			m11 = a.m10 * b.m01 + a.m11 * b.m11 + a.m12 * b.m21,
			m12 = a.m10 * b.m02 + a.m11 * b.m12 + a.m12 * b.m22,
			m20 = a.m20 * b.m00 + a.m21 * b.m10 + a.m22 * b.m20,
			m21 = a.m20 * b.m01 + a.m21 * b.m11 + a.m22 * b.m21,
			m22 = a.m20 * b.m02 + a.m21 * b.m12 + a.m22 * b.m22
		};
	}

	public Vector3 MulVec(Vector3 v)
	{
		return new Vector3(m00 * v.x + m01 * v.y + m02 * v.z, m10 * v.x + m11 * v.y + m12 * v.z, m20 * v.x + m21 * v.y + m22 * v.z);
	}

	public static float DoubleDot(SuperMat3 a, SuperMat3 b)
	{
		return a.m00 * b.m00 + a.m01 * b.m01 + a.m02 * b.m02 + a.m10 * b.m10 + a.m11 * b.m11 + a.m12 * b.m12 + a.m20 * b.m20 + a.m21 * b.m21 + a.m22 * b.m22;
	}

	public static float VonMises(SuperMat3 s)
	{
		return MathF.Sqrt(1.5f * DoubleDot(s, s));
	}

	public static float MaxPrincipal(SuperMat3 s, float sigEq, float pressurePlusQ, float sigEqFloor)
	{
		if (sigEqFloor >= sigEq)
		{
			return -pressurePlusQ;
		}
		float t = 13.5f * s.Det / (sigEq * sigEq * sigEq);
		if (t > 1f)
		{
			t = 1f;
		}
		else if (t < -1f)
		{
			t = -1f;
		}
		return 2f / 3f * sigEq * MathF.Cos(MathF.Acos(t) / 3f) - pressurePlusQ;
	}
}
