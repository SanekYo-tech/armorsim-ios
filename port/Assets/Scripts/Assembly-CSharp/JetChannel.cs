using System.Collections.Generic;
using UnityEngine;

public class JetChannel
{
	private const int DensifyNodes = 48;

	public float[] depthM { get; private set; }

	public float[] radiusM { get; private set; }

	public JetElement[] Timeline { get; private set; }

	public int LastIndex { get; private set; }

	public float FinalDepthM { get; private set; }

	public float DisplacedVolumeM3 { get; private set; }

	public bool Valid => depthM != null && depthM.Length > 1;

	public float EntryRadiusM => Valid ? radiusM[0] : 0f;

	public float ExitRadiusM => Valid ? RadiusAtDepthM(FinalDepthM) : 0f;

	public JetChannel(JetElement[] timeline, float limitM)
	{
		Timeline = timeline;
		if (timeline == null || timeline.Length == 0)
		{
			LastIndex = -1;
			return;
		}
		if (limitM <= 0f)
		{
			limitM = float.PositiveInfinity;
		}
		LastIndex = -1;
		for (int i = 0; i < timeline.Length; i++)
		{
			if (timeline[i].deltaP > 0f)
			{
				LastIndex = i;
				if (timeline[i].depthAfter >= limitM)
				{
					break;
				}
			}
		}
		if (LastIndex < 0)
		{
			return;
		}
		FinalDepthM = Mathf.Min(limitM, timeline[LastIndex].depthAfter);
		List<float> depths = new List<float>(LastIndex + 2);
		List<float> radii = new List<float>(LastIndex + 2);
		depths.Add(0f);
		radii.Add(timeline[0].craterRadiusM);
		float previousDepth = 0f;
		float volume = 0f;
		for (int i = 0; i <= LastIndex; i++)
		{
			if (timeline[i].deltaP > 0f)
			{
				float depth = Mathf.Min(limitM, timeline[i].depthAfter);
				if (depth > previousDepth)
				{
					float radius = timeline[i].craterRadiusM;
					volume += radius * Mathf.PI * radius * (depth - previousDepth);
					depths.Add(depth);
					radii.Add(radius);
					previousDepth = depth;
				}
			}
		}
		depthM = depths.ToArray();
		radiusM = radii.ToArray();
		DisplacedVolumeM3 = volume;
	}

	public void ReprofileComposite(float entryRadiusM, float halfAngleRad, float rampExponent)
	{
		if (!Valid || FinalDepthM <= 1E-09f)
		{
			return;
		}
		entryRadiusM = Mathf.Max(0f, entryRadiusM);
		halfAngleRad = Mathf.Clamp(halfAngleRad, 0f, 1.3f);
		rampExponent = Mathf.Clamp(rampExponent, 1f, 6f);
		float radialGrowth = Mathf.Tan(halfAngleRad) * FinalDepthM;
		SortedSet<float> depths = new SortedSet<float>();
		for (int i = 0; i < depthM.Length; i++)
		{
			depths.Add(Mathf.Clamp(depthM[i], 0f, FinalDepthM));
		}
		for (int i = 0; i <= DensifyNodes; i++)
		{
			depths.Add((float)i * FinalDepthM / DensifyNodes);
		}
		depthM = new float[depths.Count];
		radiusM = new float[depths.Count];
		int index = 0;
		float volume = 0f;
		float previousDepth = 0f;
		foreach (float depth in depths)
		{
			float fraction = Mathf.Clamp01(depth / FinalDepthM);
			depthM[index] = depth;
			radiusM[index] = entryRadiusM + Mathf.Pow(fraction, rampExponent) * radialGrowth;
			if (index > 0)
			{
				volume += radiusM[index] * Mathf.PI * radiusM[index] * (depth - previousDepth);
			}
			previousDepth = depth;
			index++;
		}
		DisplacedVolumeM3 = volume;
	}

	public void ScaleRadii(float k)
	{
		if (!Valid || k <= 0f || Mathf.Approximately(k, 1f))
		{
			return;
		}
		float volume = 0f;
		float previousDepth = 0f;
		for (int i = 0; i < radiusM.Length; i++)
		{
			radiusM[i] *= k;
			if (i > 0)
			{
				volume += radiusM[i] * Mathf.PI * radiusM[i] * (depthM[i] - previousDepth);
			}
			previousDepth = depthM[i];
		}
		DisplacedVolumeM3 = volume;
	}

	public void FlattenTo(float radiusM_)
	{
		if (!Valid || radiusM_ <= 0f)
		{
			return;
		}
		float volume = 0f;
		float previousDepth = 0f;
		for (int i = 0; i < radiusM.Length; i++)
		{
			radiusM[i] = radiusM_;
			if (i > 0)
			{
				volume += radiusM_ * Mathf.PI * radiusM_ * (depthM[i] - previousDepth);
			}
			previousDepth = depthM[i];
		}
		DisplacedVolumeM3 = volume;
	}

	public int ActiveIndex(float tSinceDetonationS)
	{
		if (LastIndex < 0 || tSinceDetonationS < Timeline[0].arrivalTime)
		{
			return -1;
		}
		for (int i = 0; i <= LastIndex; i++)
		{
			if (tSinceDetonationS < Timeline[i].arrivalTime)
			{
				return i - 1;
			}
		}
		return LastIndex;
	}

	public float DepthAtTimeM(float tSinceDetonationS)
	{
		int index = ActiveIndex(tSinceDetonationS);
		return index < 0 ? 0f : Mathf.Min(FinalDepthM, Timeline[index].depthAfter);
	}

	public int RetiredAtTime(float tSinceDetonationS)
	{
		int index = ActiveIndex(tSinceDetonationS);
		return index < 0 ? 0 : index + 1;
	}

	public float RadiusAtDepthM(float z)
	{
		if (!Valid)
		{
			return 0f;
		}
		if (z <= depthM[0])
		{
			return radiusM[0];
		}
		for (int i = 1; i < depthM.Length; i++)
		{
			if (z <= depthM[i])
			{
				float dz = depthM[i] - depthM[i - 1];
				float t = dz <= 1E-09f ? 1f : (z - depthM[i - 1]) / dz;
				return Mathf.Lerp(radiusM[i - 1], radiusM[i], Mathf.Clamp01(t));
			}
		}
		return radiusM[radiusM.Length - 1];
	}

	public float DisplacedVolumeToDepthM3(float z)
	{
		if (!Valid)
		{
			return 0f;
		}
		float volume = 0f;
		for (int i = 1; i < depthM.Length; i++)
		{
			float z0 = depthM[i - 1];
			if (z <= z0)
			{
				break;
			}
			float z1 = Mathf.Min(z, depthM[i]);
			volume += radiusM[i] * Mathf.PI * radiusM[i] * (z1 - z0);
		}
		return volume;
	}

	public DeformableMesh.ChannelProfile ToWorldProfile(float depthPerMeter, float radiusPerMeter)
	{
		DeformableMesh.ChannelProfile profile = default;
		if (!Valid)
		{
			return profile;
		}
		profile.depth = new float[depthM.Length];
		profile.radius = new float[radiusM.Length];
		for (int i = 0; i < depthM.Length; i++)
		{
			profile.depth[i] = depthM[i] * depthPerMeter;
			profile.radius[i] = radiusM[i] * radiusPerMeter;
		}
		return profile;
	}

	public string DebugLine(float tSinceDetonationS, float cdM)
	{
		int index = ActiveIndex(tSinceDetonationS);
		if (index < 0)
		{
			return string.Format("канал: струя в пути (t = {0:F1} мкс < ", tSinceDetonationS * 1000000f) + string.Format("t_прихода {0:F1} мкс)", Timeline[0].arrivalTime * 1000000f);
		}
		float depth = Mathf.Min(FinalDepthM, Timeline[index].depthAfter);
		float radius = RadiusAtDepthM(depth);
		return string.Concat(string.Format("канал: t = {0:F1} мкс | индекс {1}/{2} | ", tSinceDetonationS * 1000000f, index, LastIndex), string.Format("снято с головы {0} | глубина {1:F1} мм | ", index + 1, depth * 1000f), string.Format("r на дне {0:F2} мм (CD/{1:F1})", radius * 1000f, cdM / (2f * Mathf.Max(1E-09f, radius))), index >= LastIndex ? " | ОСТАНОВЛЕН (последний вносящий элемент)" : "");
	}

	public string ProfileSummary(float cdM)
	{
		if (!Valid)
		{
			return "канал: профиль пуст";
		}
		float entry = radiusM[0];
		float exit = radiusM[radiusM.Length - 1];
		float middle = RadiusAtDepthM(FinalDepthM * 0.5f);
		return string.Concat(string.Format("профиль канала: устье r={0:F2} мм (CD/{1:F2}), ", entry * 1000f, cdM / (2f * entry)), string.Format("середина r={0:F2} мм (CD/{1:F2}), ", middle * 1000f, cdM / (2f * middle)), string.Format("дно r={0:F2} мм (CD/{1:F2}) | ", exit * 1000f, cdM / (2f * exit)), string.Format("глубина {0:F1} мм | вытеснено {1:F1} см³", FinalDepthM * 1000f, DisplacedVolumeM3 * 1000000f));
	}
}
