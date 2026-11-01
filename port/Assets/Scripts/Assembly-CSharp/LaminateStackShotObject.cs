using UnityEngine;

public sealed class LaminateStackShotObject : IShotObject
{
	public string Id { get; }

	public int SheetCount { get; }

	public float SheetThicknessMm { get; }

	public float GapMm { get; }

	public IMaterialRef Material { get; }

	public float HalfWidthMm { get; }

	public float HalfHeightMm { get; }

	public LaminateStackShotObject(string id, int sheetCount, float sheetThicknessMm, float gapMm, IMaterialRef material)
		: this(id, sheetCount, sheetThicknessMm, gapMm, material, halfWidthMm: float.PositiveInfinity, halfHeightMm: float.PositiveInfinity)
	{
	}

	public LaminateStackShotObject(string id, int sheetCount, float sheetThicknessMm, float gapMm, IMaterialRef material, float halfWidthMm, float halfHeightMm)
	{
		Id = id;
		SheetCount = sheetCount;
		SheetThicknessMm = sheetThicknessMm;
		GapMm = gapMm;
		Material = material;
		HalfWidthMm = halfWidthMm;
		HalfHeightMm = halfHeightMm;
	}

	public float TotalThicknessMm => SheetCount * SheetThicknessMm + Mathf.Max(0, SheetCount - 1) * GapMm;

	public float AxialHalfExtentMm(in BodyLocalRay ray)
	{
		return TotalThicknessMm * 0.5f / Mathf.Max(Mathf.Abs(ray.Direction.z), 1e-07f);
	}

	public void Trace(in BodyLocalRay ray, ISegmentSink sink)
	{
		if (SheetCount <= 0 || SheetThicknessMm <= 0f)
		{
			sink.Reject(BodyRejectCode.DegenerateObject, string.Format("стопка «{0}»: листов {1}, лист {2} мм", Id, SheetCount, SheetThicknessMm));
			return;
		}
		if (GapMm < 0f)
		{
			sink.Reject(BodyRejectCode.DegenerateObject, string.Format("стопка «{0}»: зазор {1} мм", Id, GapMm));
			return;
		}
		float obliquity = BodyGeometry.ObliquityDeg(ray.Direction, Vector3.right);
		Vector3 half = new Vector3(HalfWidthMm, HalfHeightMm, SheetThicknessMm * 0.5f);
		float num = GapMm + SheetThicknessMm;
		float num2 = -0.5f * TotalThicknessMm;
		for (int i = 0; i < SheetCount; i++)
		{
			BodyLocalRay bodyLocalRay = new BodyLocalRay(ray.OriginMm - new Vector3(0f, 0f, num2 + i * num + SheetThicknessMm * 0.5f), ray.Direction);
			if (!BodyGeometry.Box(bodyLocalRay, half, out float t0, out float t1, out int enterAxis))
			{
				continue;
			}
			EntrySurface entry = ((i != 0) ? EntrySurface.Internal : ((enterAxis == 2) ? ((ray.Direction.z < 0f) ? EntrySurface.Rear : EntrySurface.Front) : EntrySurface.Edge));
			sink.AddSpan(t0, t1, Material, SheetThicknessMm, obliquity, entry);
		}
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
