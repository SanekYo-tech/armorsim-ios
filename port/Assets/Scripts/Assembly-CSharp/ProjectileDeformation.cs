using System;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileDeformation
{
	private class LayerFrames
	{
		public List<float> t;

		public List<Vector3[]> v;
	}

	public int vertsPerRing = 24;

	public int rings = 40;

	public float ringDensifyPow = 2f;

	public int extraNoseRings = 10;

	[Range(0.05f, 0.5f)]
	public float noseFraction = 0.15f;

	public float k = 30000f;

	public float axialBackFraction = 0.43f;

	public float contactPatchFactor = 2.35f;

	public bool mushroomFlow;

	public float contactDepthCalibers = 1f;

	public int maxFrames = 90;

	public float subDtMax = 2e-06f;

	public int maxSubSteps = 400;

	private const int EventFrames = 12;

	private float r0M;

	private float L0M;

	private float r0W;

	private float L0W;

	private TateModel.Penetrator pen;

	private float worldPerMeter;

	private Color color;

	private float[] aRing;

	private Vector3[] baseLocal;

	private Vector3[] curDisp;

	private float[] epsP;

	private Vector3[] undeformedVerts;

	private int ringVerts;

	private int noseCenterIdx;

	private int tailCenterIdx;

	private int V;

	private int[] tris;

	private float builtR0W = -1f;

	private float builtL0W = -1f;

	private int builtRings = -1;

	private int builtVerts = -1;

	private NoseShape builtNose = (NoseShape)(-1);

	private readonly Dictionary<int, LayerFrames> byLayer = new Dictionary<int, LayerFrames>();

	private Vector3[] curVerts;

	private Vector3[] sampleBuf;

	private float peakR1;

	private float maxEps;

	private float maxSigmaSeen;

	private float peakRStopW;

	public float hingeZoneCalibers = 1.5f;

	private float bendThetaDeg;

	private Vector3 bendDirWorld = Vector3.up;

	private bool bendAlignNose;

	private Vector3[] bendVerts;

	public Projectile.CoreType coreType;

	public NoseShape noseShape = NoseShape.Conical;

	public bool raggedTail;

	private Vector3[] raggedBuf;

	private GameObject go;

	private MeshFilter mf;

	private MeshRenderer mr;

	private Mesh mesh;

	public float RadiusM => r0M;

	public float PeakR1Ratio => ((r0M > 1e-09f) ? (peakR1 / r0M) : 1f);

	public float MaxEpsP => maxEps;

	public Renderer DrawnRenderer
	{
		get
		{
			if (mr != null && mr.enabled && go != null && go.activeInHierarchy)
			{
				return mr;
			}
			return null;
		}
	}

	public void SetBendPose(float thetaDeg, Vector3 dirWorld, bool alignNose)
	{
		bendThetaDeg = ((0f < thetaDeg) ? thetaDeg : 0f);
		bendAlignNose = alignNose;
		if (dirWorld.x * dirWorld.x + dirWorld.y * dirWorld.y + dirWorld.z * dirWorld.z > 1e-12f)
		{
			bendDirWorld = dirWorld;
		}
	}

	public float HingeLengthM()
	{
		float num;
		Vector3[] array = ((curVerts != null) ? curVerts : undeformedVerts);
		if (array != null)
		{
			num = Mathf.Max(-array[tailCenterIdx].z, 1e-04f);
		}
		else
		{
			num = L0W;
		}
		float num2 = 2f * hingeZoneCalibers * r0W;
		num = ((num2 <= num * 0.75f) ? num2 : (num * 0.75f));
		return Mathf.Max(num / worldPerMeter, 1e-06f);
	}

	public float SectionRadiusW(float aM)
	{
		if (curVerts == null || aRing == null || aRing.Length == 0)
		{
			return r0W;
		}
		float num = aM * worldPerMeter;
		int num2 = 0;
		float num3 = float.MaxValue;
		for (int i = 0; i < aRing.Length; i++)
		{
			float num4 = Mathf.Abs(aRing[i] - num);
			if (num4 < num3)
			{
				num3 = num4;
				num2 = i;
			}
		}
		float num5 = 0f;
		for (int j = 0; j < vertsPerRing; j++)
		{
			Vector3 vector = curVerts[num2 * vertsPerRing + j];
			num5 += Mathf.Sqrt(vector.x * vector.x + vector.y * vector.y);
		}
		return Mathf.Max(num5 / vertsPerRing, 0.2f * r0W);
	}

	public void Configure(float diameterMM, float L0M, TateModel.Penetrator penetrator, Color color, float worldPerMeter)
	{
		r0M = Mathf.Max(diameterMM * 0.5f / 1000f, 1e-04f);
		this.L0M = Mathf.Max(L0M, 1e-04f);
		pen = penetrator;
		this.color = color;
		this.worldPerMeter = Mathf.Max(worldPerMeter, 1e-04f);
		r0W = this.worldPerMeter * r0M;
		L0W = this.worldPerMeter * this.L0M;
		if (baseLocal == null || Mathf.Abs(r0W - builtR0W) > 1e-06f || Mathf.Abs(L0W - builtL0W) > 1e-06f || builtRings != rings || builtVerts != vertsPerRing || builtNose != noseShape)
		{
			BuildBase();
			Array.Clear(curDisp, 0, V);
			Array.Clear(epsP, 0, V);
			undeformedVerts = RenderFrame(0f);
			builtR0W = r0W;
			builtL0W = L0W;
			builtRings = rings;
			builtVerts = vertsPerRing;
			builtNose = noseShape;
		}
		curVerts = undeformedVerts;
		peakR1 = r0M;
		maxEps = 0f;
	}

	private void BuildBase()
	{
		int num = ((vertsPerRing >= 3) ? vertsPerRing : 3);
		int num2 = ((rings >= 3) ? rings : 3);
		List<float> list = new List<float>(Math.Max(extraNoseRings, 0) + num2);
		for (int i = 0; i < num2; i++)
		{
			list.Add(Mathf.Pow((float)i / (float)(num2 - 1), ringDensifyPow));
		}
		for (int j = 1; j <= extraNoseRings; j++)
		{
			list.Add((float)j * noseFraction / ((float)extraNoseRings + 1f));
		}
		list.Sort();
		List<float> list2 = new List<float>(list.Count);
		foreach (float item in list)
		{
			if (list2.Count == 0 || item - list2[list2.Count - 1] > 1e-04f)
			{
				list2.Add(item);
			}
		}
		ringVerts = list2.Count * num;
		V = list2.Count * num + 2;
		noseCenterIdx = list2.Count * num;
		tailCenterIdx = list2.Count * num + 1;
		aRing = new float[list2.Count];
		baseLocal = new Vector3[V];
		curDisp = new Vector3[V];
		epsP = new float[V];
		float num3 = RodGeometry.NoseLengthM(r0M * 2f, L0M) * worldPerMeter;
		for (int k = 0; k < list2.Count; k++)
		{
			aRing[k] = list2[k] * L0W;
			float num4 = RodGeometry.NoseRadiusFactorAt(noseShape, aRing[k], num3, r0W) * r0W;
			for (int l = 0; l < num; l++)
			{
				float f = (float)l / (float)num * Mathf.PI * 2f;
				baseLocal[k * num + l] = new Vector3(Mathf.Cos(f) * num4, Mathf.Sin(f) * num4, -aRing[k]);
			}
		}
		baseLocal[noseCenterIdx] = Vector3.zero;
		baseLocal[tailCenterIdx] = new Vector3(0f, 0f, -L0W);
		BuildTris(list2.Count, num);
	}

	private void BuildTris(int rn, int vp)
	{
		List<int> list = new List<int>(rn * vp * 6);
		for (int i = 0; i < vp; i++)
		{
			list.Add(noseCenterIdx);
			list.Add(i);
			list.Add((i + 1) % vp);
		}
		for (int j = 0; j < rn - 1; j++)
		{
			for (int k = 0; k < vp; k++)
			{
				int num = j * vp + k;
				int num2 = (j + 1) * vp + k;
				int num3 = j * vp + (k + 1) % vp;
				int num4 = (j + 1) * vp + (k + 1) % vp;
				list.Add(num);
				list.Add(num2);
				list.Add(num3);
				list.Add(num3);
				list.Add(num2);
				list.Add(num4);
			}
		}
		for (int l = 0; l < vp; l++)
		{
			list.Add(tailCenterIdx);
			list.Add((rn - 1) * vp + (l + 1) % vp);
			list.Add((rn - 1) * vp + l);
		}
		tris = list.ToArray();
	}

	public void Precompute(List<LayerPlaybackData> layers)
	{
		Array.Clear(curDisp, 0, V);
		Array.Clear(epsP, 0, V);
		byLayer.Clear();
		peakR1 = r0M;
		maxEps = 0f;
		peakRStopW = 0f;
		int num = aRing.Length;
		int num2 = vertsPerRing;
		float num3 = r0W * contactPatchFactor;
		float num4 = 2f * contactDepthCalibers * r0W;
		float num5 = Mathf.Max(k, 1000000f);
		float num6 = Mathf.Max(0f, axialBackFraction);
		int num7 = 0;
		foreach (LayerPlaybackData layer in layers)
		{
			if (layer.outcome.steps != null)
			{
				num7 += layer.outcome.steps.Count;
			}
		}
		int num8 = ((maxFrames >= 1) ? maxFrames : 1);
		int num9 = ((num7 / num8 >= 1) ? (num7 / num8) : 1);
		int num10 = 0;
		while (num10 < layers.Count)
		{
			LayerPlaybackData layerPlaybackData = layers[num10];
			List<TateStep> list = layerPlaybackData.outcome.steps;
			if (list == null || list.Count == 0)
			{
				BakeEventErosion(num10, layerPlaybackData);
				num10++;
				continue;
			}
			TateModel.Target target = ((layerPlaybackData.plate != null) ? layerPlaybackData.plate.GetTarget() : TateModel.RHA());
			LayerFrames layerFrames = new LayerFrames();
			byLayer[num10] = layerFrames;
			float num11 = target.Rt;
			float num12 = target.density;
			float num13 = 0f;
			float num14 = 0f;
			for (int i = 0; i < list.Count; i++)
			{
				TateStep tateStep = list[i];
				float num15 = Mathf.Max(tateStep.t - num13, 1e-12f);
				float num16 = L0W - tateStep.L * worldPerMeter;
				float num17 = Mathf.Clamp(TateModel.PenetrationVelocity(pen, target, tateStep.v), 0f, tateStep.v);
				float num18 = num12 * 0.5f * num17 * num17 + num11;
				float num19;
				if (num18 <= 1e-09f || num5 / num18 >= 1f)
				{
					num19 = 0f;
				}
				else
				{
					num19 = num3 * (2f / Mathf.PI) * Mathf.Acos(Mathf.Sqrt(num5 / num18));
				}
				if (num19 > peakRStopW)
				{
					peakRStopW = num19;
				}
				if (mushroomFlow)
				{
					int num20 = Mathf.Clamp(Mathf.CeilToInt(num15 / subDtMax), 1, maxSubSteps);
					float num21 = num15 / num20;
					float num22 = num16 - num14;
					for (int num23 = 1; num23 <= num20; num23++)
					{
						float num24 = Mathf.Clamp((float)num23 / (float)num20, 0f, 1f);
						float num25 = num14 + num22 * num24;
						for (int num26 = 0; num26 < num; num26++)
						{
							if (aRing[num26] < num25 || aRing[num26] > num25 + num4)
							{
								continue;
							}
							for (int num27 = num26 * num2; num27 < (num26 + 1) * num2; num27++)
							{
								float num28 = baseLocal[num27].x + curDisp[num27].x;
								float num29 = baseLocal[num27].y + curDisp[num27].y;
								float num30 = baseLocal[num27].z + curDisp[num27].z;
								float num31 = Mathf.Sqrt(num28 * num28 + num29 * num29);
								if (num31 >= num3)
								{
									continue;
								}
								float num32 = Mathf.Cos(num31 * Mathf.PI / (num3 + num3));
								float num33 = num32 * num18 * num32;
								if (num33 > maxSigmaSeen)
								{
									maxSigmaSeen = num33;
								}
								if (num33 <= num5)
								{
									continue;
								}
								float num34 = (num33 - num5) * k / num5 * num21;
								Vector3 dir;
								if (Mathf.Abs(num28) + Mathf.Abs(num29) > 1e-09f)
								{
									dir = new Vector3(num28, num29, 0f);
								}
								else
								{
									dir = new Vector3(baseLocal[num27].x, baseLocal[num27].y, 0f);
								}
								dir = dir.normalized;
								dir += num6 * new Vector3(0f, 0f, -1f);
								dir = dir.normalized;
								curDisp[num27] = new Vector3(curDisp[num27].x + dir.x * num34, curDisp[num27].y + dir.y * num34, curDisp[num27].z + dir.z * num34);
								Vector3 vector = baseLocal[num27] + curDisp[num27];
								float num35 = Mathf.Sqrt(vector.x * vector.x + vector.y * vector.y);
								if (num35 > num19 && num35 > 1e-09f)
								{
									float num36 = num19 / num35;
									curDisp[num27] = new Vector3(vector.x * num36 - baseLocal[num27].x, vector.y * num36 - baseLocal[num27].y, curDisp[num27].z);
								}
								Vector3 vector2 = baseLocal[num27] + curDisp[num27];
								epsP[num27] += (vector2 - new Vector3(num28, num29, num30)).magnitude / r0W;
							}
						}
					}
				}
				if (i % num9 == 0 || i == list.Count - 1)
				{
					layerFrames.t.Add(tateStep.t);
					layerFrames.v.Add(RenderFrame(num16));
				}
				num13 = tateStep.t;
				num14 = num16;
			}
			num10++;
		}
		LogValidation(layers, num6);
	}

	private void BakeEventErosion(int li, LayerPlaybackData l)
	{
		LayerFrames layerFrames = new LayerFrames();
		byLayer[li] = layerFrames;
		for (int i = 0; i <= EventFrames; i++)
		{
			float num = (float)i / EventFrames;
			layerFrames.t.Add(num);
			float num2 = Mathf.Lerp(l.entryLengthM, l.exitLengthM, Mathf.Clamp01(num)) * worldPerMeter;
			layerFrames.v.Add(RenderFrame(Mathf.Max(0f, L0W - num2)));
		}
		Vector3[] array = layerFrames.v[layerFrames.v.Count - 1];
		float num3 = 0f;
		float num4 = 0f;
		for (int j = 0; j < ringVerts; j++)
		{
			float num5 = array[j].z;
			if (num3 > num5)
			{
				num3 = num5;
			}
			if (num5 > num4)
			{
				num4 = num5;
			}
		}
		float num6 = (num4 - num3) / worldPerMeter * 1000f;
		float num7 = l.exitLengthM * 1000f;
		Debug.Log(string.Format("[Деформация] Событие без интегратора (слой {0}, {1}): ", li + 1, l.eventKind) + string.Format("L {0:F0} → {1:F0} мм ", l.entryLengthM * 1000f, num7) + string.Format("(износ {0:F0} мм, стачивается рампом за время контакта) | ", (l.entryLengthM - l.exitLengthM) * 1000f) + string.Format("сетка {0:F1} мм | сверка сетка↔ядро = {1:F2} мм", num6, Mathf.Abs(num6 - num7)));
	}

	private void LogValidation(List<LayerPlaybackData> layers, float axB)
	{
		for (int i = 0; i < V; i++)
		{
			Vector3 vector = baseLocal[i] + curDisp[i];
			float num = Mathf.Sqrt(vector.x * vector.x + vector.y * vector.y) / worldPerMeter;
			if (num > peakR1)
			{
				peakR1 = num;
			}
			if (epsP[i] > maxEps)
			{
				maxEps = epsP[i];
			}
		}
		float num2 = -1f;
		float num3 = -1f;
		for (int num4 = layers.Count - 1; num4 >= 0 && num2 < 0f; num4--)
		{
			if (!byLayer.TryGetValue(num4, out LayerFrames layerFrames) || layerFrames.v.Count == 0)
			{
				continue;
			}
			LayerPlaybackData layerPlaybackData = layers[num4];
			if (layerPlaybackData.outcome.steps == null || layerPlaybackData.outcome.steps.Count == 0)
			{
				continue;
			}
			Vector3[] array = layerFrames.v[layerFrames.v.Count - 1];
			float num5 = 0f;
			float num6 = 0f;
			for (int j = 0; j < ringVerts; j++)
			{
				float num7 = array[j].z;
				if (num5 > num7)
				{
					num5 = num7;
				}
				if (num7 > num6)
				{
					num6 = num7;
				}
			}
			num2 = num6 - num5;
			num3 = layerPlaybackData.outcome.steps[layerPlaybackData.outcome.steps.Count - 1].L * worldPerMeter;
		}
		float num8 = 1f / Mathf.Sqrt(axB * axB + 1f);
		float num9 = ((r0W > 1e-09f) ? (peakRStopW / r0W) : 0f);
		if (mushroomFlow)
		{
			bool flag = k < maxSigmaSeen;
			bool flag2 = false;
			if (num9 > 1f)
			{
				flag = k < maxSigmaSeen;
				flag2 = ((r0M > 1e-09f) ? (peakR1 / r0M) : 1f) >= num9 * 0.97f;
			}
			string text = flag ? "транзиент — поднять k" : "ТЕЧЕНИЯ НЕТ (σ≤Y_p)";
			if (flag2)
			{
				text = "НАСЫЩЕНИЕ (держит диаметр)";
			}
			float num10 = ((r0M > 1e-09f) ? (peakR1 / r0M) : 1f);
			Debug.Log(string.Format("[Деформация] Гриб из течения: R₁/r₀ = {0:F2} (research 1.35–1.50) | ", num10) + string.Format("потолок равновесия r_stop/r₀ = {0:F2} | режим: {1}", num9, text));
			object[] array2 = new object[4]
			{
				num8,
				num8 * axB,
				contactPatchFactor,
				maxEps
			};
			Debug.Log(string.Format("[Деформация] Параметры: k={0:F0} | axialBack={1:F2} ", k, axB) + string.Format("(течение радиаль:осевая = {0:F2}:{1:F2}) | contactPatchFactor={2:F2} | max ε_p={3:F2}", array2));
			Debug.Log(string.Format("[Деформация] Физичность: max σ_local = {0:F2} ГПа vs Y_p = {1:F1} ГПа ", maxSigmaSeen / 1e+09f, k / 1e+09f) + "→ " + ((maxSigmaSeen > k) ? "течёт (гриб есть)" : "НЕ течёт (гриба нет, R₁/r₀≈1.0)"));
		}
		else
		{
			Debug.Log("[Деформация] Гриб ВЫКЛЮЧЕН (mushroomFlow=false): пластического течения нет, стержень — ровный цилиндр, только эрозия. Механизм Этапа 1 цел, вернуть = true.");
		}
		if (num2 >= 0f)
		{
			Debug.Log(string.Format("[Деформация] Сверка длины: сетка {0:F1} мм vs ", num2 / worldPerMeter * 1000f) + string.Format("L ядра {0:F1} мм (расхождение {1:F2} мм)", num3 / worldPerMeter * 1000f, Mathf.Abs(num2 - num3) / worldPerMeter * 1000f));
		}
	}

	private Vector3[] RenderFrame(float eW)
	{
		Vector3[] array = new Vector3[V];
		float num = ((L0W - eW > 0f) ? (L0W - eW) : 0f);
		int num2 = aRing.Length;
		for (int i = 0; i < num2; i++)
		{
			for (int j = i * vertsPerRing; j < (i + 1) * vertsPerRing; j++)
			{
				array[j] = new Vector3(baseLocal[j].x + curDisp[j].x, baseLocal[j].y + curDisp[j].y, Mathf.Min(0f, baseLocal[j].z + curDisp[j].z + eW));
			}
		}
		array[noseCenterIdx] = Vector3.zero;
		array[tailCenterIdx] = new Vector3(0f, 0f, -num);
		return array;
	}

	public void SetSampleState(int layerIndex, float tReal)
	{
		if (byLayer.TryGetValue(layerIndex, out LayerFrames layerFrames) && layerFrames.v.Count != 0)
		{
			List<float> list = layerFrames.t;
			if (tReal <= list[0])
			{
				curVerts = layerFrames.v[0];
				return;
			}
			if (tReal >= list[list.Count - 1])
			{
				curVerts = layerFrames.v[list.Count - 1];
				return;
			}
			int num = 1;
			while (num < list.Count && tReal > list[num])
			{
				num++;
			}
			if (num >= list.Count)
			{
				curVerts = layerFrames.v[list.Count - 1];
				return;
			}
			float num2 = list[num] - list[num - 1];
			float num3 = ((num2 <= 1e-12f) ? 1f : Mathf.Clamp01((tReal - list[num - 1]) / num2));
			if (sampleBuf == null || sampleBuf.Length != V)
			{
				sampleBuf = new Vector3[V];
			}
			Vector3[] array = layerFrames.v[num - 1];
			Vector3[] array2 = layerFrames.v[num];
			for (int i = 0; i < V; i++)
			{
				sampleBuf[i] = Vector3.Lerp(array[i], array2[i], num3);
			}
			curVerts = sampleBuf;
		}
	}

	public void SetFinalState(int layerIndex)
	{
		if (byLayer.TryGetValue(layerIndex, out LayerFrames layerFrames) && layerFrames.v.Count > 0)
		{
			curVerts = layerFrames.v[layerFrames.v.Count - 1];
		}
	}

	public void Render(Transform parent, Vector3 nosePos, Vector3 dir)
	{
		EnsureMesh(parent);
		if (curVerts == null)
		{
			curVerts = RenderFrame(0f);
		}
		if (!go.activeSelf)
		{
			go.SetActive(true);
		}
		Vector3 vector = ((dir.x * dir.x + dir.y * dir.y + dir.z * dir.z > 1e-12f) ? dir.normalized : Vector3.forward);
		go.transform.position = nosePos;
		go.transform.rotation = Quaternion.LookRotation(vector);
		Vector3[] array = curVerts;
		if (bendThetaDeg > 0.05f)
		{
			array = ApplyBendPose(curVerts);
		}
		if (raggedTail)
		{
			array = ApplyRaggedTail(array);
		}
		mesh.Clear();
		mesh.vertices = array;
		mesh.triangles = tris;
		MeshNormals.RecalculateSmooth(mesh, 55f, false);
		mesh.RecalculateBounds();
	}

	private Vector3[] ApplyBendPose(Vector3[] src)
	{
		if (bendVerts == null || bendVerts.Length != V)
		{
			bendVerts = new Vector3[V];
		}
		float num = Mathf.Max(-src[tailCenterIdx].z, 1e-04f);
		float num2 = Mathf.Min(2f * hingeZoneCalibers * r0W, num * 0.75f);
		Transform transform = go.transform;
		Vector3 vector = transform.InverseTransformDirection(bendDirWorld);
		if (vector.x * vector.x + vector.y * vector.y < 1e-10f)
		{
			return src;
		}
		Vector3 vector2 = new Vector3(vector.x, vector.y, 0f).normalized;
		Vector3 axis = Vector3.Cross(Vector3.forward, vector2);
		float f = bendThetaDeg * 0.0174532924f;
		float num3 = num2 / f;
		for (int i = 0; i < V; i++)
		{
			float num4 = src[i].z + num2;
			if (num4 <= 0f)
			{
				bendVerts[i] = src[i];
				continue;
			}
			if (num2 <= num4)
			{
				num4 = num2;
			}
			float num5 = num4 / num3;
			Quaternion quaternion = Quaternion.AngleAxis(num5 * 57.29578f, axis);
			Vector3 vector3 = quaternion * new Vector3(src[i].x, src[i].y, 0f);
			bendVerts[i] = Mathf.Sin(num5) * num3 * Vector3.forward + new Vector3(0f, 0f, -num2) + (1f - Mathf.Cos(num5)) * num3 * vector2 + vector3;
		}
		if (bendAlignNose)
		{
			Quaternion quaternion2 = Quaternion.AngleAxis(0f - bendThetaDeg, axis);
			for (int j = 0; j < V; j++)
			{
				bendVerts[j] = quaternion2 * (bendVerts[j] - new Vector3(0f, 0f, -num2)) + new Vector3(0f, 0f, -num2);
			}
		}
		Vector3 vector4 = bendVerts[noseCenterIdx];
		for (int k = 0; k < V; k++)
		{
			bendVerts[k] -= vector4;
		}
		return bendVerts;
	}

	private Vector3[] ApplyRaggedTail(Vector3[] src)
	{
		float H(int k, int salt)
		{
			float num = Mathf.Sin((float)k * 12.9898f + (float)salt * 78.233f) * 43758.5453f;
			return (num - Mathf.Floor(num)) * 2f - 1f;
		}
		if (raggedBuf == null || raggedBuf.Length != V)
		{
			raggedBuf = new Vector3[V];
		}
		Array.Copy(src, raggedBuf, V);
		int num = aRing.Length;
		float num2 = r0W * 0.18f;
		int num3 = (num - 1) * vertsPerRing;
		for (int i = 0; i < vertsPerRing; i++)
		{
			Vector3 vector = raggedBuf[num3 + i];
			Vector2 vector2 = new Vector2(vector.x, vector.y);
			if (vector2.x * vector2.x + vector2.y * vector2.y > 1e-09f)
			{
				vector2.Normalize();
			}
			raggedBuf[num3 + i] = new Vector3(vector.x + H(i, 1) * vector2.x * num2, vector.y + H(i, 1) * vector2.y * num2, vector.z + H(i, 2) * num2);
		}
		raggedBuf[tailCenterIdx] = new Vector3(raggedBuf[tailCenterIdx].x + H(0, 3) * num2, raggedBuf[tailCenterIdx].y + H(0, 4) * num2, raggedBuf[tailCenterIdx].z + H(0, 5) * num2);
		return raggedBuf;
	}

	private void EnsureMesh(Transform parent)
	{
		if (go == null)
		{
			GameObject gameObject = new GameObject("ProjectileDeformMesh", typeof(MeshFilter), typeof(MeshRenderer));
			go = gameObject;
			go.transform.SetParent(parent, false);
			mf = go.GetComponent<MeshFilter>();
			mr = go.GetComponent<MeshRenderer>();
			Mesh mesh = new Mesh();
			mesh.name = "ProjectileDeformMesh";
			this.mesh = mesh;
			mf.sharedMesh = this.mesh;
		}
		if (mr != null)
		{
			mr.sharedMaterial = MaterialLibrary.Core(coreType);
		}
	}

	public void SetActive(bool on)
	{
		if (go != null)
		{
			go.SetActive(on);
		}
	}
}
