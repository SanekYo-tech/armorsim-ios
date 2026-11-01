using UnityEngine;

public sealed class PlateShotObject : IShotObject
{
	public string Id { get; }

	public float ThicknessMm { get; }

	public IMaterialRef Material { get; }

	public bool Unbounded { get; }

	public float HalfWidthMm { get; }

	public float HalfHeightMm { get; }

	public PlateShotObject(string id, float thicknessMm, IMaterialRef material)
		: this(id, thicknessMm, material, unbounded: true, halfWidthMm: 0f, halfHeightMm: 0f)
	{
	}

	public PlateShotObject(string id, float thicknessMm, IMaterialRef material, bool unbounded, float halfWidthMm, float halfHeightMm)
	{
		Id = id;
		ThicknessMm = thicknessMm;
		Material = material;
		Unbounded = unbounded;
		HalfWidthMm = halfWidthMm;
		HalfHeightMm = halfHeightMm;
	}

	private Vector3 Half => new Vector3(Unbounded ? float.PositiveInfinity : HalfWidthMm, Unbounded ? float.PositiveInfinity : HalfHeightMm, ThicknessMm * 0.5f);

	public float AxialHalfExtentMm(in BodyLocalRay ray)
	{
		if (Unbounded)
		{
			return ThicknessMm * 0.5f / Mathf.Max(Mathf.Abs(ray.Direction.z), 1e-07f);
		}
		return Mathf.Abs(ray.Direction.x) * HalfWidthMm + Mathf.Abs(ray.Direction.y) * HalfHeightMm + Mathf.Abs(ray.Direction.z) * (ThicknessMm * 0.5f);
	}

	public void Trace(in BodyLocalRay ray, ISegmentSink sink)
	{
		if (ThicknessMm <= 0f)
		{
			sink.Reject(BodyRejectCode.DegenerateObject, string.Format("плита «{0}»: толщина {1} мм", Id, ThicknessMm));
			return;
		}
		if (!Unbounded && (HalfWidthMm <= 0f || HalfHeightMm <= 0f))
		{
			sink.Reject(BodyRejectCode.DegenerateObject, string.Format("плита «{0}»: нулевой габарит {1}×{2} мм", Id, HalfWidthMm, HalfHeightMm));
			return;
		}
		Vector3 half = Half;
		if (!BodyGeometry.Box(ray, half, out float t0, out float t1, out int enterAxis))
		{
			return;
		}
		Vector3 normal = Vector3.zero;
		normal[(enterAxis < 0) ? 2 : enterAxis] = 1f;
		float num = ThicknessMm;
		if (enterAxis == 0)
		{
			num = 2f * HalfWidthMm;
		}
		else if (enterAxis == 1)
		{
			num = 2f * HalfHeightMm;
		}
		EntrySurface entry = ((enterAxis < 0 || enterAxis == 2) ? ((ray.Direction.z < 0f) ? EntrySurface.Rear : EntrySurface.Front) : EntrySurface.Edge);
		float obliquity = BodyGeometry.ObliquityDeg(ray.Direction, normal);
		sink.AddSpan(t0, t1, Material, num, obliquity, entry);
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
