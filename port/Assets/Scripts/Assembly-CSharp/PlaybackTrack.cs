using System.Collections.Generic;
using UnityEngine;

public static class PlaybackTrack
{
	private struct LayerNodes
	{
		public float[] tau;

		public float[] depth;

		public float[] len;
	}

	public const float VisualDepthScale = 999.99994f;

	public const float WorldPerMeter = 1f;

	public const float FloorReferenceDepthMM = 50f;

	public const int FloorMinFrames = 3;

	private const float EdgeEps = 0.001f;

	public static float MinVisibleLayerSeconds => 0.05f;

	public static float LayerFloorSeconds(float depthMM, float fullFloorSeconds)
	{
		float num = Mathf.Max(fullFloorSeconds, 0f);
		depthMM = Mathf.Max(depthMM, 0f);
		return Mathf.Clamp(depthMM * fullFloorSeconds / FloorReferenceDepthMM, MinVisibleLayerSeconds, num);
	}

	public static float SpeedIn(ShotPlan plan, IList<PlaybackSegment> segs, int i, float baseVisualLen)
	{
		return EdgeSpeed(plan, segs, i, baseVisualLen, atEnd: false);
	}

	public static float SpeedOut(ShotPlan plan, IList<PlaybackSegment> segs, int i, float baseVisualLen)
	{
		return EdgeSpeed(plan, segs, i, baseVisualLen, atEnd: true);
	}

	private static float EdgeSpeed(ShotPlan plan, IList<PlaybackSegment> segs, int i, float baseVisualLen, bool atEnd)
	{
		if (segs == null || i < 0 || i >= segs.Count)
		{
			return -1f;
		}
		PlaybackSegment playbackSegment = segs[i];
		float num = Mathf.Max(playbackSegment.duration, 1e-06f);
		float k = atEnd ? (1f - EdgeEps) : 0f;
		float k2 = atEnd ? 1f : EdgeEps;
		Vector3 a = LocalCenter(plan, segs, i, baseVisualLen, k);
		Vector3 b = LocalCenter(plan, segs, i, baseVisualLen, k2);
		return (b - a).magnitude / (num * EdgeEps);
	}

	private static LayerNodes MonotoneNodes(ShotPlan plan, LayerPlaybackData l, float baseVisualLen)
	{
		List<TateStep> steps = l.outcome.steps;
		if (steps == null || steps.Count == 0)
		{
			float num = l.outcome.penetrationDepthMM;
			if (num <= 0f)
			{
				num = l.tEffMM;
			}
			return new LayerNodes
			{
				tau = new float[2] { 0f, 1f },
				depth = new float[2] { 0f, num },
				len = new float[2] { l.entryLengthM, l.exitLengthM }
			};
		}
		int count = steps.Count;
		TateStep tateStep = steps[count - 1];
		bool flag = Mathf.Abs(tateStep.L - l.exitLengthM) > 1e-07f;
		int num2 = count + 1 + (flag ? 1 : 0);
		float[] array = new float[num2];
		float[] array2 = new float[num2];
		float[] array3 = new float[num2];
		array[0] = 0f;
		array2[0] = l.entryLengthM;
		array3[0] = 0f;
		for (int i = 1; i <= count; i++)
		{
			array[i] = steps[i - 1].depthMM;
			array2[i] = steps[i - 1].L;
			array3[i] = steps[i - 1].t;
		}
		if (flag)
		{
			array[num2 - 1] = steps[count - 1].depthMM;
			array2[num2 - 1] = l.exitLengthM;
			array3[num2 - 1] = array3[count] + Mathf.Max(array3[count] * 0.01f, 1e-09f);
		}
		float num3 = Mathf.Max(plan.L0M, 1e-06f);
		float[] array4 = new float[num2];
		for (int j = 0; j < num2; j++)
		{
			array4[j] = (Mathf.Clamp01(array2[0] / num3) - Mathf.Clamp01(array2[j] / num3)) * baseVisualLen * 0.5f + array[j] / VisualDepthScale;
		}
		float[] array5 = new float[num2];
		float num4 = float.PositiveInfinity;
		for (int k = 1; k < num2; k++)
		{
			float num5 = Mathf.Max(array4[k] - array4[k - 1], 0f);
			float num6 = Mathf.Max(array3[k] - array3[k - 1], 1e-12f);
			if (num5 / num6 < num4)
			{
				num4 = num5 / num6;
			}
			float num7 = ((num4 > 1e-12f) ? (num5 / num4) : num6);
			array5[k] = num7 + array5[k - 1];
		}
		return new LayerNodes
		{
			tau = array5,
			depth = array,
			len = array2
		};
	}

	public static void SampleLayerMonotone(ShotPlan plan, LayerPlaybackData l, float baseVisualLen, float k, out float depthMM, out float lengthM)
	{
		LayerNodes nodes = MonotoneNodes(plan, l, baseVisualLen);
		float[] array = nodes.tau;
		if (array.Length < 2)
		{
			depthMM = 0f;
			lengthM = l.entryLengthM;
			return;
		}
		int num = array.Length;
		float num2 = array[num - 1];
		if (num2 <= 1e-12f)
		{
			depthMM = nodes.depth[num - 1];
			lengthM = nodes.len[num - 1];
			return;
		}
		k = Mathf.Clamp01(k);
		float num3 = k * num2;
		int num4 = 1;
		while (num4 < num - 1 && num3 > array[num4])
		{
			num4++;
		}
		float num5 = array[num4] - array[num4 - 1];
		float num6;
		if (num5 <= 1e-12f)
		{
			num6 = 1f;
		}
		else
		{
			num6 = (num3 - array[num4 - 1]) / num5;
		}
		depthMM = (nodes.depth[num4] - nodes.depth[num4 - 1]) * Mathf.Clamp01(num6) + nodes.depth[num4 - 1];
		lengthM = (nodes.len[num4] - nodes.len[num4 - 1]) * Mathf.Clamp01(num6) + nodes.len[num4 - 1];
	}

	public static Vector3 PhaseEndpoint(ShotPlan plan, IList<PlaybackSegment> segs, int i, float baseVisualLen, float k)
	{
		return LocalCenter(plan, segs, i, baseVisualLen, k);
	}

	private static Vector3 LocalCenter(ShotPlan plan, IList<PlaybackSegment> segs, int i, float baseVisualLen, float k)
	{
		PlaybackSegment playbackSegment = segs[i];
		if (playbackSegment.kind != PlaybackSegKind.Layer)
		{
			return Vector3.Lerp(playbackSegment.fromCenter, playbackSegment.toCenter, Mathf.Clamp01(k));
		}
		LayerPlaybackData layerPlaybackData = plan.layers[playbackSegment.layerIndex];
		SampleLayerMonotone(plan, layerPlaybackData, baseVisualLen, k, out var depthMM, out var lengthM);
		float num = Mathf.Clamp01(lengthM / Mathf.Max(plan.L0M, 1e-06f));
		return layerPlaybackData.entryPoint + layerPlaybackData.direction * (depthMM / VisualDepthScale) - layerPlaybackData.direction * (num * baseVisualLen * 0.5f);
	}

	public static float RealDuration(LayerPlaybackData l)
	{
		List<TateStep> steps = l.outcome.steps;
		if (steps == null || steps.Count < 1)
		{
			return 0f;
		}
		return steps[steps.Count - 1].t;
	}

	public static float NoseRateMMps(LayerPlaybackData l, bool atEnd)
	{
		List<TateStep> steps = l.outcome.steps;
		if (steps == null || steps.Count == 0)
		{
			return -1f;
		}
		if (atEnd)
		{
			int num = steps.Count - 1;
			while (num >= 1)
			{
				if (steps[num].t - steps[num - 1].t > 1e-12f)
				{
					return (steps[num].depthMM - steps[num - 1].depthMM) / (steps[num].t - steps[num - 1].t);
				}
				num--;
			}
			return -1f;
		}
		for (int i = 0; i < steps.Count; i++)
		{
			if (steps[i].t > 1e-12f)
			{
				return steps[i].depthMM / steps[i].t;
			}
		}
		return -1f;
	}

	public static void EnforceMonotonicSpeed(ShotPlan plan, IList<PlaybackSegment> segs, float baseVisualLen)
	{
		bool Adaptive(PlaybackSegKind k)
		{
			return k != PlaybackSegKind.Layer;
		}
		if (plan == null || segs == null || segs.Count == 0)
		{
			return;
		}
		float num = float.PositiveInfinity;
		for (int i = 0; i < segs.Count; i++)
		{
			if (segs[i].kind == PlaybackSegKind.Layer)
			{
				float num2 = EdgeSpeed(plan, segs, i, baseVisualLen, atEnd: false);
				if (num2 > num && num2 > 1e-09f && num > 1e-09f)
				{
					segs[i].duration = num2 / num * segs[i].duration;
					num2 = EdgeSpeed(plan, segs, i, baseVisualLen, atEnd: false);
				}
				float num3 = EdgeSpeed(plan, segs, i, baseVisualLen, atEnd: true);
				if (num3 <= 1e-09f)
				{
					num3 = num2;
				}
				if (num2 < num3)
				{
					num3 = num2;
				}
				num = num3;
			}
		}
		for (int j = 0; j < segs.Count; j++)
		{
			if (Adaptive(segs[j].kind))
			{
				PlaybackSegment playbackSegment = segs[j];
				float num4 = (playbackSegment.toCenter - playbackSegment.fromCenter).magnitude;
				if (num4 > 1e-06f)
				{
					float num5 = PrevOwnSpeed(plan, segs, j, baseVisualLen);
					float num6 = NextOwnSpeed(plan, segs, j, baseVisualLen);
					float num7 = Mathf.Max(playbackSegment.duration, 1e-06f);
					if (num5 < 0f)
					{
						float num8 = num4 / num7;
						num5 = Mathf.Max(num8, ((num6 > 0f) ? num6 : num8));
					}
					if (num6 < 0f)
					{
						num6 = num5;
					}
					if (num6 > num5)
					{
						num6 = num5;
					}
					float num9 = (num6 + num5) * 0.5f;
					if (num9 > 1e-09f)
					{
						playbackSegment.duration = num4 / num9;
					}
				}
			}
		}
	}

	private static float PrevOwnSpeed(ShotPlan plan, IList<PlaybackSegment> segs, int i, float baseVisualLen)
	{
		for (int num = i - 1; num >= 0; num--)
		{
			if (segs[num].kind == PlaybackSegKind.Layer)
			{
				return EdgeSpeed(plan, segs, num, baseVisualLen, atEnd: true);
			}
		}
		return -1f;
	}

	private static float NextOwnSpeed(ShotPlan plan, IList<PlaybackSegment> segs, int i, float baseVisualLen)
	{
		for (int num = i + 1; num < segs.Count; num++)
		{
			if (segs[num].kind == PlaybackSegKind.Layer)
			{
				return EdgeSpeed(plan, segs, num, baseVisualLen, atEnd: false);
			}
		}
		return -1f;
	}

	public static float Ease(float k, float m0, float m1)
	{
		float num = m0 * m0 + m1 * m1;
		if (num > 9f)
		{
			float num2 = 3f / Mathf.Sqrt(num);
			m0 *= num2;
			m1 *= num2;
		}
		float num3 = k * k;
		float num4 = num3 * k;
		return (num4 - 2f * num3 + k) * m0 + num4 * -2f + num3 * 3f + (num4 - num3) * m1;
	}

	public static float EasedParam(ShotPlan plan, IList<PlaybackSegment> segs, int i, float k, float baseVisualLen)
	{
		PlaybackSegment playbackSegment = segs[i];
		Vector3 vector = playbackSegment.toCenter - playbackSegment.fromCenter;
		float magnitude = vector.magnitude;
		if (magnitude > 1e-06f)
		{
			float num = magnitude / Mathf.Max(playbackSegment.duration, 1e-06f);
			float num2 = PrevOwnSpeed(plan, segs, i, baseVisualLen);
			float num3 = NextOwnSpeed(plan, segs, i, baseVisualLen);
			if (num2 < 0f)
			{
				num2 = Mathf.Max(num, ((num3 > 0f) ? num3 : num));
			}
			if (num3 < 0f)
			{
				num3 = num2;
			}
			float num4 = Mathf.Clamp(num2 / num, 0f, 3f);
			float num5 = Mathf.Clamp(Mathf.Min(num2, num3) / num, 0f, 3f);
			return Ease(k, num4, num5);
		}
		return k;
	}

	public static Vector3 CenterAt(ShotPlan plan, IList<PlaybackSegment> segs, float baseVisualLen, float t)
	{
		if (segs == null || segs.Count == 0)
		{
			return Vector3.forward;
		}
		int num = 0;
		float num2 = 0f;
		while (num < segs.Count - 1 && t >= num2 + segs[num].duration)
		{
			num2 += segs[num].duration;
			num++;
		}
		PlaybackSegment playbackSegment = segs[num];
		float num3;
		if (playbackSegment.duration <= 1e-09f)
		{
			num3 = 1f;
		}
		else
		{
			num3 = (t - num2) / playbackSegment.duration;
		}
		num3 = Mathf.Clamp01(num3);
		switch (playbackSegment.kind)
		{
		case PlaybackSegKind.Layer:
			return LocalCenter(plan, segs, num, baseVisualLen, num3);
		case PlaybackSegKind.Flight:
		case PlaybackSegKind.Ricochet:
		case PlaybackSegKind.Slide:
		{
			float num4 = Mathf.Clamp01(EasedParam(plan, segs, num, num3, baseVisualLen));
			return Vector3.Lerp(playbackSegment.fromCenter, playbackSegment.toCenter, num4);
		}
		default:
			return playbackSegment.toCenter;
		}
	}

	public static float RodLengthAt(ShotPlan plan, IList<PlaybackSegment> segs, float baseVisualLen, float t)
	{
		if (segs == null || segs.Count == 0 || plan == null)
		{
			return 0f;
		}
		int num = 0;
		float num2 = 0f;
		while (num < segs.Count - 1 && t >= num2 + segs[num].duration)
		{
			num2 += segs[num].duration;
			num++;
		}
		float num3;
		if (segs[num].duration <= 1e-09f)
		{
			num3 = 1f;
		}
		else
		{
			num3 = (t - num2) / segs[num].duration;
		}
		num3 = Mathf.Clamp01(num3);
		return LocalLength(plan, segs, num, baseVisualLen, num3);
	}

	private static float LocalLength(ShotPlan plan, IList<PlaybackSegment> segs, int i, float baseVisualLen, float k)
	{
		PlaybackSegment playbackSegment = segs[i];
		if (playbackSegment.kind == PlaybackSegKind.Layer)
		{
			LayerPlaybackData layerPlaybackData = plan.layers[playbackSegment.layerIndex];
			SampleLayerMonotone(plan, layerPlaybackData, baseVisualLen, k, out var _, out var lengthM);
			return lengthM;
		}
		if (playbackSegment.layerIndex >= 0 && playbackSegment.layerIndex < plan.layers.Count)
		{
			LayerPlaybackData layerPlaybackData2 = plan.layers[playbackSegment.layerIndex];
			return (layerPlaybackData2.exitLengthM - layerPlaybackData2.entryLengthM) * Mathf.Clamp01(k) + layerPlaybackData2.entryLengthM;
		}
		for (int num = i; num >= 0; num--)
		{
			PlaybackSegment playbackSegment2 = segs[num];
			if (playbackSegment2.layerIndex >= 0 && playbackSegment2.layerIndex < plan.layers.Count)
			{
				return plan.layers[playbackSegment2.layerIndex].exitLengthM;
			}
		}
		return plan.L0M;
	}

	public static void SampleDepth(LayerPlaybackData l, float tReal, out float depthMM, out float lengthM)
	{
		List<TateStep> steps = l.outcome.steps;
		if (steps == null || steps.Count == 0)
		{
			depthMM = 0f;
			lengthM = l.entryLengthM;
			return;
		}
		int num = 0;
		while (num < steps.Count && steps[num].t < tReal)
		{
			num++;
		}
		if (num >= steps.Count)
		{
			depthMM = steps[steps.Count - 1].depthMM;
			lengthM = steps[steps.Count - 1].L;
			return;
		}
		TateStep tateStep = steps[num];
		float num2 = l.entryLengthM;
		float num3 = 0f;
		float num4 = 0f;
		if (num > 0)
		{
			TateStep tateStep2 = steps[num - 1];
			num2 = tateStep2.L;
			num3 = tateStep2.depthMM;
			num4 = tateStep2.t;
		}
		float num5 = ((tateStep.t - num4 > 1e-12f) ? Mathf.Clamp01((tReal - num4) / (tateStep.t - num4)) : 1f);
		depthMM = (tateStep.depthMM - num3) * Mathf.Clamp01(num5) + num3;
		lengthM = (tateStep.L - num2) * Mathf.Clamp01(num5) + num2;
	}

	public static float TotalDuration(IList<PlaybackSegment> segs)
	{
		float num = 0f;
		if (segs != null)
		{
			foreach (PlaybackSegment segment in segs)
			{
				num += segment.duration;
			}
		}
		return num;
	}

	public static int SegmentAt(IList<PlaybackSegment> segs, float t)
	{
		float num = 0f;
		int num2 = 0;
		while (num2 < segs.Count - 1)
		{
			if (t < num + segs[num2].duration)
			{
				return num2;
			}
			num += segs[num2].duration;
			num2++;
		}
		return num2;
	}

	public static int CountSpeedIncreases(ShotPlan plan, IList<PlaybackSegment> segs, float baseVisualLen, int samples, out float worstRatio, out float worstAtT)
	{
		worstRatio = 1f;
		worstAtT = -1f;
		int result = 0;
		float num = TotalDuration(segs);
		if (num <= 1e-06f || samples < FloorMinFrames)
		{
			return 0;
		}
		float num2 = num / samples;
		Vector3 vector = CenterAt(plan, segs, baseVisualLen, 0f);
		float num3 = float.PositiveInfinity;
		for (int i = 1; i <= samples; i++)
		{
			float num4 = i * num2;
			Vector3 vector2 = CenterAt(plan, segs, baseVisualLen, num4);
			float num5 = (vector2 - vector).magnitude / num2;
			if (num3 < float.PositiveInfinity && num5 > num3 * 1.02f && num3 > 1e-06f)
			{
				result++;
				float num6 = num5 / num3;
				if (num6 > worstRatio)
				{
					worstRatio = num6;
					worstAtT = num4;
				}
			}
			num3 = num5;
			vector = vector2;
		}
		return result;
	}
}
