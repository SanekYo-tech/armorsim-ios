using System.Collections.Generic;
using UnityEngine;

public sealed class BodyShotObject : IShotObject
{
	public string Id { get; }

	public float FatMm { get; }

	public float MuscleMm { get; }

	public IReadOnlyList<TorsoPart> Parts { get; }

	public BodyShotObject(string id)
	{
		Id = id;
		FatMm = BodyAnatomy.FatDefaultMm;
		MuscleMm = BodyAnatomy.MuscleDefaultMm;
		Parts = Build(FatMm, MuscleMm);
	}

	public BodyShotObject(string id, float fatMm, float muscleMm)
	{
		Id = id;
		FatMm = Mathf.Clamp(fatMm, BodyAnatomy.FatMinMm, BodyAnatomy.FatMaxMm);
		MuscleMm = Mathf.Clamp(muscleMm, BodyAnatomy.MuscleMinMm, BodyAnatomy.MuscleMaxMm);
		Parts = Build(FatMm, MuscleMm);
	}

	private static IReadOnlyList<TorsoPart> Build(float fatMm, float muscleMm)
	{
		List<TorsoPart> parts = new List<TorsoPart>();
		PlateShotObject sternum = new PlateShotObject("sternum", BodyAnatomy.SternumThicknessMm, new EstimatedMaterialRef("sternum"), false, BodyAnatomy.SternumHalfWidthMm, BodyAnatomy.SternumHalfHeightMm);
		parts.Add(new TorsoPart("sternum", TissueKind.BoneCortical, new Vector3(BodyAnatomy.SternumCentreMm.x, BodyAnatomy.SternumCentreMm.y, -(BodyAnatomy.SoftTissueRadiusMm(fatMm, muscleMm) - BodyAnatomy.DepthOffsetMm - BodyAnatomy.SternumThicknessMm * 0.5f)), sternum));
		parts.Add(new TorsoPart("spine", TissueKind.BoneCortical, new Vector3(BodyAnatomy.SpineCentreMm.x, BodyAnatomy.SpineCentreMm.y, 45f), SolidCylinder("spine", BodyAnatomy.SpineDiameterMm * 0.5f, BodyAnatomy.HeightMm)));
		List<ShellBand> corticalBands = new List<ShellBand>();
		List<ShellBand> cancellousBands = new List<ShellBand>();
		for (int i = 0; i < BodyAnatomy.RibCount; i++)
		{
			float y0 = BodyAnatomy.RibFirstYMm + (float)i * (BodyAnatomy.RibThicknessMm + BodyAnatomy.RibGapMm);
			float y1 = y0 + BodyAnatomy.RibThicknessMm;
			float rOuter = BodyAnatomy.RibOuterRadiusMm;
			float rBone = rOuter - BodyAnatomy.RibBoneMm;
			float rMid = rBone + BodyAnatomy.RibCorticalMm;
			float rCortical = rOuter - BodyAnatomy.RibCorticalMm;
			corticalBands.Add(new ShellBand(y0, y1, rOuter, rOuter, 0f, rCortical, rCortical));
			corticalBands.Add(new ShellBand(y0, y1, rMid, rMid, 0f, rBone, rBone));
			cancellousBands.Add(new ShellBand(y0, y1, rCortical, rCortical, 0f, rMid, rMid));
		}
		parts.Add(new TorsoPart("rib.cortical", TissueKind.BoneCortical, Vector3.zero, new ShellOfRevolutionShotObject("rib.cortical", new EstimatedMaterialRef("rib.cortical"), corticalBands, BodyAnatomy.DepthOffsetMm)));
		parts.Add(new TorsoPart("rib.cancellous", TissueKind.BoneCancellous, Vector3.zero, new ShellOfRevolutionShotObject("rib.cancellous", new EstimatedMaterialRef("rib.cancellous"), cancellousBands, BodyAnatomy.DepthOffsetMm)));
		parts.Add(new TorsoPart("aorta", TissueKind.Organ, new Vector3(BodyAnatomy.AortaCentreMm.x, BodyAnatomy.AortaCentreMm.y, 15.5f), SolidCylinder("aorta", BodyAnatomy.AortaDiameterMm * 0.5f, BodyAnatomy.HeightMm)));
		for (int j = 0; j < BodyAnatomy.Organs.Length; j++)
		{
			BodyAnatomy.Organ organ = BodyAnatomy.Organs[j];
			parts.Add(new TorsoPart(organ.Id, TissueKind.Organ, Squash(organ.CentreMm), new EllipsoidShotObject(organ.Id, Squash(organ.SemiAxesMm), new EstimatedMaterialRef(organ.Id))));
		}
		parts.Add(Shell("skin", TissueKind.Skin, BodyAnatomy.HeightMm, BodyAnatomy.OuterRadiusMm, BodyAnatomy.SkinMm));
		parts.Add(Shell("fat", TissueKind.Fat, BodyAnatomy.HeightMm, BodyAnatomy.OuterRadiusMm - BodyAnatomy.SkinMm, fatMm));
		parts.Add(Shell("muscle", TissueKind.Muscle, BodyAnatomy.HeightMm, BodyAnatomy.OuterRadiusMm - BodyAnatomy.SkinMm - fatMm, muscleMm));
		List<ShellBand> softBands = new List<ShellBand>();
		softBands.Add(new ShellBand(-BodyAnatomy.HeightMm, BodyAnatomy.HeightMm, BodyAnatomy.SoftTissueRadiusMm(fatMm, muscleMm), BodyAnatomy.SoftTissueRadiusMm(fatMm, muscleMm), BodyAnatomy.SoftTissueRadiusMm(fatMm, muscleMm)));
		parts.Add(new TorsoPart("soft_tissue", TissueKind.SoftTissue, Vector3.zero, new ShellOfRevolutionShotObject("soft_tissue", new EstimatedMaterialRef("soft_tissue"), softBands, BodyAnatomy.DepthOffsetMm)));
		return parts;
	}

	private static TorsoPart Shell(string id, TissueKind kind, float height, float outer, float thickness)
	{
		List<ShellBand> bands = new List<ShellBand>();
		bands.Add(new ShellBand(0f, height, outer, outer, thickness));
		ShellOfRevolutionShotObject shell = new ShellOfRevolutionShotObject(id, new EstimatedMaterialRef(id), bands, BodyAnatomy.DepthOffsetMm);
		return new TorsoPart(id, kind, Vector3.zero, shell);
	}

	private static ShellOfRevolutionShotObject SolidCylinder(string id, float radius, float height)
	{
		List<ShellBand> bands = new List<ShellBand>();
		bands.Add(new ShellBand(-height, height, radius, radius, radius));
		return new ShellOfRevolutionShotObject(id, new EstimatedMaterialRef(id), bands);
	}

	private static Vector3 Squash(Vector3 v)
	{
		return new Vector3(v.x, v.y, v.z * BodyAnatomy.DepthRatio);
	}

	public static BodyLocalRay PartRay(in BodyLocalRay ray, Vector3 centreMm, out float shiftMm)
	{
		Vector3 rel = ray.OriginMm - centreMm;
		float shift = Vector3.Dot(rel, ray.Direction);
		shiftMm = shift;
		return new BodyLocalRay(rel - ray.Direction * shift, ray.Direction);
	}

	public float AxialHalfExtentMm(in BodyLocalRay ray)
	{
		return Mathf.Abs(ray.Direction.z) * BodyAnatomy.DepthRadiusMm + Mathf.Abs(ray.Direction.x) * BodyAnatomy.OuterRadiusMm + Mathf.Abs(ray.Direction.y) * BodyAnatomy.HeightMm;
	}

	public void Trace(in BodyLocalRay ray, ISegmentSink sink)
	{
		List<Vector2> occ = new List<Vector2>();
		SpanCollector spans = new SpanCollector();
		for (int i = 0; i < Parts.Count; i++)
		{
			TorsoPart part = Parts[i];
			BodyLocalRay local = PartRay(ray, part.CentreMm, out float shift);
			spans.Clear();
			part.Object.Trace(local, spans);
			for (int j = 0; j < spans.Rejections.Count; j++)
			{
				TraceRejection rejection = spans.Rejections[j];
				sink.Reject(rejection.Code, rejection.Reason);
			}
			if (spans.Rejections.Count > 0)
			{
				return;
			}
			spans.SortStable();
			for (int k = 0; k < spans.Spans.Count; k++)
			{
				RaySpan span = spans.Spans[k];
				bool first = true;
				List<Vector2> holes = Remainder(span.EnterMm - shift, span.ExitMm - shift, occ);
				foreach (Vector2 hole in holes)
				{
					sink.AddSpan(hole.x, hole.y, span.Material, span.NormalThicknessMm, span.ObliquityDeg, first ? span.Entry : EntrySurface.Internal);
					Occupy(occ, hole.x, hole.y);
					first = false;
				}
			}
		}
	}

	private static List<Vector2> Remainder(float e, float x, List<Vector2> occ)
	{
		List<Vector2> holes = new List<Vector2>();
		for (int i = 0; i < occ.Count; i++)
		{
			if (x <= e)
			{
				return holes;
			}
			Vector2 o = occ[i];
			if (e >= o.y)
			{
				continue;
			}
			if (o.x >= x)
			{
				break;
			}
			if (o.x > e)
			{
				holes.Add(new Vector2(e, o.x));
			}
			if (o.y > e)
			{
				e = o.y;
			}
		}
		if (x > e)
		{
			holes.Add(new Vector2(e, x));
		}
		return holes;
	}

	private static void Occupy(List<Vector2> occ, float a, float b)
	{
		int i = 0;
		while (i < occ.Count && a > occ[i].x)
		{
			i++;
		}
		occ.Insert(i, new Vector2(a, b));
		int j = 0;
		while (j + 1 < occ.Count)
		{
			if (occ[j].y >= occ[j + 1].x)
			{
				occ[j] = new Vector2(occ[j].x, Mathf.Max(occ[j].y, occ[j + 1].y));
				occ.RemoveAt(j + 1);
			}
			else
			{
				j++;
			}
		}
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
