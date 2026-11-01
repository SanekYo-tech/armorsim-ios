using System.Collections.Generic;
using UnityEngine;

public sealed class ShellOfRevolutionShotObject : IShotObject
{
	public string Id { get; }

	public IMaterialRef Material { get; }

	public IReadOnlyList<ShellBand> Bands { get; }

	public float DepthOffsetMm { get; }

	public ShellOfRevolutionShotObject(string id, IMaterialRef material, IReadOnlyList<ShellBand> bands)
	{
		Id = id;
		Material = material;
		Bands = bands;
	}

	public ShellOfRevolutionShotObject(string id, IMaterialRef material, IReadOnlyList<ShellBand> bands, float depthOffsetMm)
	{
		Id = id;
		Material = material;
		Bands = bands;
		DepthOffsetMm = depthOffsetMm;
	}

	private float RatioAt(float r)
	{
		if (r <= DepthOffsetMm + 1e-07f)
		{
			return 1f;
		}
		return (r - DepthOffsetMm) / r;
	}

	public float AxialHalfExtentMm(in BodyLocalRay ray)
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < Bands.Count; i++)
		{
			ShellBand shellBand = Bands[i];
			num = Mathf.Max(num, Mathf.Max(shellBand.ROuterAtYMinMm, shellBand.ROuterAtYMaxMm));
			num2 = Mathf.Max(num2, Mathf.Max(Mathf.Abs(shellBand.YMinMm), Mathf.Abs(shellBand.YMaxMm)));
		}
		return Mathf.Abs(ray.Direction.z) * Mathf.Max(0f, num - DepthOffsetMm) + Mathf.Abs(ray.Direction.x) * num + Mathf.Abs(ray.Direction.y) * num2;
	}

	public void Trace(in BodyLocalRay ray, ISegmentSink sink)
	{
		if (Bands == null || Bands.Count == 0)
		{
			sink.Reject(BodyRejectCode.DegenerateObject, "тело вращения «" + Id + "»: пустой профиль");
			return;
		}
		for (int i = 0; i < Bands.Count; i++)
		{
			TraceBand(Bands[i], i, ray, sink);
		}
	}

	private void TraceBand(in ShellBand band, int index, in BodyLocalRay world, ISegmentSink sink)
	{
		float num = band.YMaxMm - band.YMinMm;
		if (num <= 0f)
		{
			sink.Reject(BodyRejectCode.DegenerateObject, string.Format("тело вращения «{0}»: полоса {1} высотой {2} мм", Id, index, num));
			return;
		}
		float num2 = (band.ROuterAtYMaxMm - band.ROuterAtYMinMm) / num;
		float num3 = band.ROuterAtYMinMm - num2 * band.YMinMm;
		float num4;
		float num5;
		float num6;
		if (float.IsFinite(band.RInnerAtYMinMm) && float.IsFinite(band.RInnerAtYMaxMm))
		{
			num5 = (band.RInnerAtYMaxMm - band.RInnerAtYMinMm) / num;
			num4 = band.RInnerAtYMinMm - num5 * band.YMinMm;
			num6 = (band.ROuterAtYMinMm - band.RInnerAtYMinMm) / Mathf.Sqrt(num2 * num2 + 1f);
		}
		else
		{
			if (band.WallThicknessMm <= 0f)
			{
				sink.Reject(BodyRejectCode.DegenerateObject, string.Format("тело вращения «{0}»: полоса {1}, стенка {2} мм", Id, index, band.WallThicknessMm));
				return;
			}
			num5 = num2;
			num4 = num3 - Mathf.Sqrt(num2 * num2 + 1f) * band.WallThicknessMm;
			num6 = band.WallThicknessMm;
		}
		float num7 = num2 * band.YMinMm + num3;
		float num8 = num5 * band.YMinMm + num4;
		float num9 = RatioAt(num7);
		float num10 = RatioAt(num8);
		BodyLocalRay ray = BodyGeometry.ToCircularSection(world, num9);
		if (!BodyGeometry.Revolved(ray, num3, num2, band.YMinMm, band.YMaxMm, out float t, out float t2, out bool unsupported))
		{
			if (unsupported)
			{
				sink.Reject(BodyRejectCode.DegenerateObject, string.Format("тело вращения «{0}»: полоса {1} — луч круче образующей, ", Id, index) + "пересечение не выражается одним промежутком; не поддержано");
			}
			return;
		}
		float num11 = world.Direction.x * t + world.OriginMm.x;
		float num12 = world.Direction.z * t + world.OriginMm.z;
		float num13 = Mathf.Max(1f, num9);
		Vector3 vector = new Vector3(num11 + num11, ((world.Direction.y * t + world.OriginMm.y) * num2 + num3) * num2 * -2f, (num12 + num12) / (num13 * num13));
		float magnitude = vector.magnitude;
		if (BodyGeometry.Eps <= magnitude)
		{
			vector /= magnitude;
		}
		else
		{
			vector = Vector3.up;
		}
		float num14 = BodyGeometry.ObliquityDeg(world.Direction, vector);
		if (DepthOffsetMm + BodyGeometry.Eps < num8)
		{
			BodyLocalRay ray2 = BodyGeometry.ToCircularSection(world, num10);
			if (BodyGeometry.Revolved(ray2, num4, num5, band.YMinMm, band.YMaxMm, out float t3, out float t4, out bool unsupported2) && BodyGeometry.Eps < t4 - t3 && t < t3 && t4 < t2)
			{
				sink.AddSpan(t, t3, Material, num6, num14, EntrySurface.Front);
				Vector3 vector2 = new Vector3(world.Direction.x * t4 + world.OriginMm.x, world.Direction.y * t4 + world.OriginMm.y, world.Direction.z * t4 + world.OriginMm.z);
				Vector3 normal = BodyGeometry.RevolvedNormal(vector2, num4, num5, num10);
				float num15 = BodyGeometry.ObliquityDeg(world.Direction, normal);
				sink.AddSpan(t4, t2, Material, num6, num15, EntrySurface.Internal);
				return;
			}
		}
		sink.AddSpan(t, t2, Material, num6, num14, EntrySurface.Front);
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
