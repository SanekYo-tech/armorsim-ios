using System;
using System.Collections.Generic;
using UnityEngine;

public static class BodyTracer
{
	public const float DirectionTolerance = 0.0001f;

	public const float OverlapToleranceMm = 0.001f;

	public static BodyTraceResult Trace(BodyScene scene)
	{
		List<TraceRejection> rejections = new List<TraceRejection>();
		if (scene == null)
		{
			rejections.Add(new TraceRejection(BodyRejectCode.DegenerateObject, "сцены нет вовсе", -1));
			return Rejected(rejections);
		}
		Validate(scene, rejections);
		if (rejections.Count > 0)
		{
			return Rejected(rejections);
		}
		List<TraceSegment> segments = new List<TraceSegment>();
		SpanCollector spanCollector = new SpanCollector();
		float num = 0f;
		float num2 = float.NegativeInfinity;
		for (int i = 0; i < scene.Placements.Count; i++)
		{
			BodyPlacement bodyPlacement = scene.Placements[i];
			BodyLocalRay ray = LocalRay(scene.Shot, bodyPlacement);
			spanCollector.Clear();
			bodyPlacement.Object.Trace(ray, spanCollector);
			if (spanCollector.Rejections.Count > 0)
			{
				for (int j = 0; j < spanCollector.Rejections.Count; j++)
				{
					rejections.Add(spanCollector.Rejections[j]);
				}
				return Rejected(rejections);
			}
			spanCollector.SortStable();
			if (bodyPlacement.StandoffMm > 0f)
			{
				segments.Add(TraceSegment.MakeVoid(bodyPlacement.StandoffMm, i, bodyPlacement.Object.Id));
				num += bodyPlacement.StandoffMm;
			}
			if (spanCollector.Spans.Count > 0)
			{
				num -= spanCollector.Spans[0].EnterMm;
			}
			float num3 = bodyPlacement.Object.AxialHalfExtentMm(ray);
			float num4 = num + num3;
			if (i > 0 && num - num3 < num2 - OverlapToleranceMm)
			{
				rejections.Add(new TraceRejection(BodyRejectCode.PlacementOverlap, string.Format("габарит размещения {0} («{1}») начинается на {2:F3} мм, ", i, bodyPlacement.Object.Id, num - num3) + string.Format("предыдущее кончается на {0:F3} мм", num2), i));
				return Rejected(rejections);
			}
			num2 = ((spanCollector.Spans.Count >= 1) ? spanCollector.Spans[0].EnterMm : 0f);
			for (int k = 0; k < spanCollector.Spans.Count; k++)
			{
				RaySpan raySpan = spanCollector.Spans[k];
				float num5 = ((raySpan.EnterMm < num2) ? num2 : raySpan.EnterMm);
				if (num5 < raySpan.ExitMm)
				{
					if (num5 - num2 > 0f)
					{
						segments.Add(TraceSegment.MakeVoid(num5 - num2, i, bodyPlacement.Object.Id));
					}
					segments.Add(new TraceSegment(SegmentKind.Material, raySpan.Material, raySpan.ExitMm - num5, raySpan.NormalThicknessMm, raySpan.ObliquityDeg, i, bodyPlacement.Object.Id, raySpan.Entry));
					num2 = raySpan.ExitMm;
				}
			}
			num += num2;
			num2 = num4;
		}
		List<TraceInterface> interfaces = BuildInterfaces(scene.Shot, segments);
		TraceBranch[] branches = new TraceBranch[1] { new TraceBranch(0, -1, -1, segments) };
		return new BodyTraceResult(branches, interfaces, new TraceRejection[0], BodyFingerprint.Of(segments));
	}

	private static void Validate(BodyScene scene, List<TraceRejection> rejections)
	{
		float num = scene.Shot.Direction.magnitude;
		if (Math.Abs(num - 1f) > DirectionTolerance)
		{
			rejections.Add(new TraceRejection(BodyRejectCode.DirectionNotNormalised, string.Format("|направление| = {0:F6}, допуск {1}", num, DirectionTolerance), -1));
		}
		int num2 = 0;
		int num3 = -1;
		for (int i = 0; i < scene.Placements.Count; i++)
		{
			BodyPlacement bodyPlacement = scene.Placements[i];
			if (bodyPlacement == null || bodyPlacement.Object == null)
			{
				rejections.Add(new TraceRejection(BodyRejectCode.DegenerateObject, "пустое размещение", i));
			}
			else
			{
				if (bodyPlacement.StandoffMm < 0f)
				{
					rejections.Add(new TraceRejection(BodyRejectCode.NegativeStandoff, string.Format("зазор {0} мм перед «{1}»", bodyPlacement.StandoffMm, bodyPlacement.Object.Id), i));
				}
				if (bodyPlacement.Object is BodyShotObject)
				{
					num2++;
					num3 = i;
				}
			}
		}
		if (num2 > 1)
		{
			rejections.Add(new TraceRejection(BodyRejectCode.BodyMultiple, string.Format("тел в сцене {0}", num2), num3));
			return;
		}
		if (num2 == 1 && num3 != scene.Placements.Count - 1)
		{
			rejections.Add(new TraceRejection(BodyRejectCode.BodyNotLast, string.Format("тело стоит {0}-м из {1}", num3, scene.Placements.Count), num3));
		}
	}

	private static BodyTraceResult Rejected(List<TraceRejection> rejections)
	{
		return new BodyTraceResult(new TraceBranch[1] { new TraceBranch(0, -1, -1, new TraceSegment[0]) }, new TraceInterface[0], rejections, BodyFingerprint.Of(new TraceSegment[0]));
	}

	public static BodyLocalRay LocalRay(in BodyShotSetup shot, BodyPlacement placement)
	{
		Vector3 vector = Quaternion.Inverse(placement.Orientation) * shot.Direction;
		vector = vector.normalized;
		BodyRayFrame.Build(vector, out Vector3 u, out Vector3 v);
		return new BodyLocalRay(u * placement.AimOffsetMm.x + v * placement.AimOffsetMm.y, vector);
	}

	private static List<TraceInterface> BuildInterfaces(in BodyShotSetup shot, List<TraceSegment> segments)
	{
		List<TraceInterface> list = new List<TraceInterface>();
		IMaterialRef materialRef = null;
		float num = 0f;
		for (int i = 0; i < segments.Count; i++)
		{
			IMaterialRef materialRef2 = ((segments[i].Kind == SegmentKind.Material) ? segments[i].Material : null);
			if (materialRef != materialRef2)
			{
				list.Add(new TraceInterface(i, materialRef, materialRef2, segments[i].ObliquityDeg, shot.OriginMm + shot.Direction * num));
			}
			num += segments[i].LengthMm;
			materialRef = materialRef2;
		}
		return list;
	}
}
