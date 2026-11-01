using UnityEngine;

public sealed class BoxShotObject : IShotObject
{
	public string Id { get; }

	public Vector3 HalfExtentsMm { get; }

	public float WallThicknessMm { get; }

	public bool Hollow { get; }

	public IMaterialRef Material { get; }

	public BoxShotObject(string id, Vector3 halfExtentsMm, IMaterialRef material, bool hollow, float wallThicknessMm)
	{
		Id = id;
		HalfExtentsMm = halfExtentsMm;
		Material = material;
		WallThicknessMm = wallThicknessMm;
		Hollow = hollow;
	}

	public float AxialHalfExtentMm(in BodyLocalRay ray)
	{
		return Mathf.Abs(ray.Direction.x) * HalfExtentsMm.x + Mathf.Abs(ray.Direction.y) * HalfExtentsMm.y + Mathf.Abs(ray.Direction.z) * HalfExtentsMm.z;
	}

	public void Trace(in BodyLocalRay ray, ISegmentSink sink)
	{
		Vector3 half = HalfExtentsMm;
		if (half.x <= 0f || half.y <= 0f || half.z <= 0f)
		{
			sink.Reject(BodyRejectCode.DegenerateObject, string.Format("ящик «{0}»: габарит {1}", Id, half));
			return;
		}
		if (Hollow && WallThicknessMm <= 0f)
		{
			sink.Reject(BodyRejectCode.DegenerateObject, string.Format("ящик «{0}»: полый со стенкой {1} мм", Id, WallThicknessMm));
			return;
		}
		if (!BodyGeometry.Box(ray, half, out float t0, out float t1, out int enterAxis))
		{
			return;
		}
		Vector3 normal = Vector3.zero;
		normal[(enterAxis < 0) ? 2 : enterAxis] = 1f;
		float obliquity = BodyGeometry.ObliquityDeg(ray.Direction, normal);
		EntrySurface entry = ((enterAxis == 2) ? ((ray.Direction.z < 0f) ? EntrySurface.Rear : EntrySurface.Front) : EntrySurface.Edge);
		if (!Hollow)
		{
			sink.AddSpan(t0, t1, Material, 2f * half[(enterAxis < 0) ? 2 : enterAxis], obliquity, entry);
			return;
		}
		Vector3 innerHalf = half - new Vector3(WallThicknessMm, WallThicknessMm, WallThicknessMm);
		if (innerHalf.x > 0f && innerHalf.y > 0f && innerHalf.z > 0f && BodyGeometry.Box(ray, innerHalf, out float innerT0, out float innerT1, out _) && t0 < innerT0 && innerT1 < t1)
		{
			sink.AddSpan(t0, innerT0, Material, WallThicknessMm, obliquity, entry);
			sink.AddSpan(innerT1, t1, Material, WallThicknessMm, obliquity, EntrySurface.Internal);
			return;
		}
		sink.AddSpan(t0, t1, Material, WallThicknessMm, obliquity, entry);
	}

	float IShotObject.AxialHalfExtentMm(in BodyLocalRay ray)
	{
		return AxialHalfExtentMm(in ray);
	}

	void IShotObject.Trace(in BodyLocalRay ray, ISegmentSink sink)
	{
		Trace(in ray, sink);
	}
}
