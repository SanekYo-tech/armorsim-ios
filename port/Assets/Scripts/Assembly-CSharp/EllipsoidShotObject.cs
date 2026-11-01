using System;
using UnityEngine;

public sealed class EllipsoidShotObject : IShotObject
{
	public string Id { get; }

	public Vector3 SemiAxesMm { get; }

	public IMaterialRef Material { get; }

	public EllipsoidShotObject(string id, Vector3 semiAxesMm, IMaterialRef material)
	{
		Id = id;
		SemiAxesMm = semiAxesMm;
		Material = material;
	}

	public float AxialHalfExtentMm(in BodyLocalRay ray)
	{
		return Mathf.Abs(ray.Direction.x) * SemiAxesMm.x + Mathf.Abs(ray.Direction.y) * SemiAxesMm.y + Mathf.Abs(ray.Direction.z) * SemiAxesMm.z;
	}

	public void Trace(in BodyLocalRay ray, ISegmentSink sink)
	{
		if (SemiAxesMm.x <= 0f || SemiAxesMm.y <= 0f || SemiAxesMm.z <= 0f)
		{
			sink.Reject(BodyRejectCode.DegenerateObject, string.Format("эллипсоид «{0}»: полуоси {1}", Id, SemiAxesMm));
			return;
		}
		float num = ray.Direction.x / SemiAxesMm.x;
		float num2 = ray.Direction.y / SemiAxesMm.y;
		float num3 = ray.Direction.z / SemiAxesMm.z;
		float num4 = num * num + num2 * num2 + num3 * num3;
		if (num4 <= 1E-07f)
		{
			return;
		}
		float num5 = -(num * (ray.OriginMm.x / SemiAxesMm.x) + num2 * (ray.OriginMm.y / SemiAxesMm.y) + num3 * (ray.OriginMm.z / SemiAxesMm.z)) / num4;
		float num6 = num5 * num + ray.OriginMm.x / SemiAxesMm.x;
		float num7 = num5 * num2 + ray.OriginMm.y / SemiAxesMm.y;
		float num8 = num5 * num3 + ray.OriginMm.z / SemiAxesMm.z;
		float num9 = MathF.Sqrt(num6 * num6 + num7 * num7 + num8 * num8);
		float num10 = -(num9 - 1f) * (num9 + 1f) / num4;
		if (num10 < 0f)
		{
			return;
		}
		num10 = MathF.Sqrt(num10);
		float num11 = num5 - num10;
		float num12 = num5 + num10;
		Vector3 vector = new Vector3((ray.Direction.x * num11 + ray.OriginMm.x) / (SemiAxesMm.x * SemiAxesMm.x), (ray.Direction.y * num11 + ray.OriginMm.y) / (SemiAxesMm.y * SemiAxesMm.y), (ray.Direction.z * num11 + ray.OriginMm.z) / (SemiAxesMm.z * SemiAxesMm.z));
		Vector3 vector2 = vector.normalized;
		float num13 = BodyGeometry.ObliquityDeg(ray.Direction, vector);
		sink.AddSpan(num11, num12, Material, (Mathf.Abs(vector2.x) * SemiAxesMm.x + Mathf.Abs(vector2.y) * SemiAxesMm.y + Mathf.Abs(vector2.z) * SemiAxesMm.z) * 2f, num13, EntrySurface.Front);
	}

	float IShotObject.AxialHalfExtentMm(in BodyLocalRay ray)
	{
		return AxialHalfExtentMm(ray);
	}

	void IShotObject.Trace(in BodyLocalRay ray, ISegmentSink sink)
	{
		Trace(ray, sink);
	}
}
