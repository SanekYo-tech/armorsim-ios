using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CompositeAssemblyShotObject : IShotObject
{
	private readonly string id;

	private readonly IReadOnlyList<CompositeChild> children;

	private readonly IReadOnlyList<LocalThickening> thickenings;

	public string Id => id;

	public IReadOnlyList<CompositeChild> Children => children;

	public IReadOnlyList<LocalThickening> Thickenings => thickenings;

	public CompositeAssemblyShotObject(string id, IReadOnlyList<CompositeChild> children, IReadOnlyList<LocalThickening> thickenings)
	{
		this.id = id;
		this.children = children;
		this.thickenings = thickenings ?? new List<LocalThickening>();
	}

	public float AxialHalfExtentMm(in BodyLocalRay ray)
	{
		float maxExtent = 0f;
		for (int i = 0; i < children.Count; i++)
		{
			maxExtent = Mathf.Max(children[i].Child.AxialHalfExtentMm(ray), maxExtent);
		}
		return maxExtent * 0.5f;
	}

	public void Trace(in BodyLocalRay ray, ISegmentSink sink)
	{
		if (children == null || children.Count == 0)
		{
			sink.Reject(BodyRejectCode.DegenerateObject, "сборка «" + id + "»: нет содержимого");
			return;
		}
		Vector3 u;
		Vector3 v;
		BodyRayFrame.Build(ray.Direction, out u, out v);
		Vector2 lateral = new Vector2(Vector3.Dot(u, ray.OriginMm), Vector3.Dot(v, ray.OriginMm));
		SpanCollector collector = new SpanCollector();
		float cursor = 0f;
		for (int i = 0; i < children.Count; i++)
		{
			CompositeChild child = children[i];
			if (child.Child == null)
			{
				sink.Reject(BodyRejectCode.DegenerateObject, string.Format("сборка «{0}»: пустой ребёнок {1}", id, i));
				return;
			}
			collector.Clear();
			child.Child.Trace(ray, collector);
			for (int j = 0; j < collector.Rejections.Count; j++)
			{
				sink.Reject(collector.Rejections[j].Code, collector.Rejections[j].Reason);
			}
			collector.SortStable();
			float offset = cursor - collector.Spans[0].EnterMm;
			for (int s = 0; s < collector.Spans.Count; s++)
			{
				RaySpan span = collector.Spans[s];
				sink.AddSpan(span.EnterMm + offset, span.ExitMm + offset, span.Material, span.NormalThicknessMm, span.ObliquityDeg, span.Entry);
				cursor = Mathf.Max(cursor, span.ExitMm + offset);
			}
			cursor = Mathf.Max(cursor, collector.Spans[0].ExitMm + offset);
			cursor = EmitThickenings(i, lateral, ray, cursor, sink);
		}
	}

	private float EmitThickenings(int childIndex, Vector2 lateral, in BodyLocalRay ray, float cursor, ISegmentSink sink)
	{
		for (int i = 0; i < thickenings.Count; i++)
		{
			LocalThickening t = thickenings[i];
			if (t.AfterChildIndex != childIndex)
			{
				continue;
			}
			Vector2 delta = lateral - t.CenterMm;
			if (delta.magnitude > t.RadiusMm)
			{
				continue;
			}
			if (t.ExtraThicknessMm <= 0f)
			{
				continue;
			}
			float obliquity = BodyGeometry.ObliquityDeg(ray.Direction, Vector3.forward);
			float axial = t.ExtraThicknessMm / Mathf.Max(Mathf.Abs(ray.Direction.z), 1e-07f);
			sink.AddSpan(cursor, cursor + axial, t.Material, t.ExtraThicknessMm, obliquity, EntrySurface.Internal);
			cursor += axial;
		}
		return cursor;
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
