using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine;

[AddComponentMenu(null)]
public class RodPBDSolver : MonoBehaviour
{
	public enum RodMaterial
	{
		WHA = 0,
		DU = 1,
		Steel4340 = 2,
		WC = 3
	}

	public enum TargetMaterial
	{
		RHA = 0,
		HHA = 1,
		Aluminum = 2,
		Titanium = 3,
		Ceramic = 4,
		Tungsten = 5
	}

	[Header("Выстрел (тест 2C — осевой удар)")]
	public RodMaterial material;

	public TargetMaterial target;

	public float velocityMps;

	public float plateMM;

	public float rodLengthMM;

	public float diameterMM;

	[Header("Сетка (оболочка + осевая спица; РАВНОМЕРНАЯ вдоль оси)")]
	public int vertsPerRing;

	public int rings;

	[Header("Жёстко-пластический решатель (константы из стенда)")]
	public int nIter;

	public float dtSub;

	[Range(0.9f, 1f)]
	public float damping;

	[Range(0f, 1f)]
	public float edgeViscosity;

	[Range(0.1f, 1f)]
	public float sor;

	[Header("Пластика (крутилки калибровки — якорь R₁/r₀=1.35–1.50)")]
	[Range(0.5f, 0.9f)]
	public float hFloor;

	[Range(0.1f, 1f)]
	public float kFlow;

	public float aEdgeMult;

	[Header("Тест")]
	public bool autoRunArchivedBench;

	public bool autoFireOnPlay;

	public bool fire;

	public KeyCode fireKey;

	public float playbackSeconds;

	public int savedFrames;

	[Header("Видимость (инсет-камера + свет)")]
	public bool spawnOwnCamera;

	public bool spawnOwnLight;

	public Vector3 spawnPosition;

	private const float WorldPerMeter = 1f;

	private Vector3[] pos;

	private Vector3[] prev;

	private Vector3[] pred;

	private float[] invMass;

	private float[] aRing;

	private int V;

	private int shellCount;

	private float r0M;

	private float L0M;

	private int[] cA;

	private int[] cB;

	private byte[] cType;

	private int[] cLayer;

	private float[] cL0;

	private float[] cL0init;

	private float[] lambda;

	private float[] epsP;

	private float[] Fy;

	private float[] Favg;

	private float[] plasticFloor;

	private float[] diagH0;

	private float aEdgeArea;

	private float[] layerH;

	private int[] layerCnt;

	private readonly List<Vector3[]> frames;

	private float playT;

	private Mesh mesh;

	private int[] tris;

	private Vector3[] renderVerts;

	private float simTime;

	private float maxFratio;

	public RodPBDSolver()
	{
		spawnPosition = new Vector3(4f, 3f, 4f);
		velocityMps = 1100f;
		plateMM = 50f;
		rodLengthMM = 300f;
		diameterMM = 30f;
		vertsPerRing = 24;
		rings = 40;
		nIter = 30;
		dtSub = 2e-07f;
		damping = 0.995f;
		edgeViscosity = 0.5f;
		sor = 0.8f;
		hFloor = 0.5f;
		kFlow = 1f;
		aEdgeMult = 1f;
		fireKey = KeyCode.F;
		playbackSeconds = 2f;
		savedFrames = 60;
		spawnOwnCamera = true;
		spawnOwnLight = true;
		frames = new List<Vector3[]>();
		playT = -1f;
	}

	private static TateModel.Penetrator Pen(RodMaterial m)
	{
		switch (m)
		{
		case RodMaterial.DU:
			return TateModel.DU();
		case RodMaterial.Steel4340:
			return TateModel.Steel();
		case RodMaterial.WC:
			return TateModel.WC();
		default:
			return TateModel.WHA();
		}
	}

	private static TateModel.Target Tgt(TargetMaterial m)
	{
		switch (m)
		{
		case TargetMaterial.HHA:
			return TateModel.HHA();
		case TargetMaterial.Aluminum:
			return TateModel.Aluminum();
		case TargetMaterial.Titanium:
			return TateModel.Titanium();
		case TargetMaterial.Ceramic:
			return TateModel.Ceramic();
		case TargetMaterial.Tungsten:
			return TateModel.Tungsten();
		default:
			return TateModel.RHA();
		}
	}

	private int CenterIdx(int ring)
	{
		return shellCount + ring;
	}

	private void Start()
	{
		if (!autoRunArchivedBench)
		{
			return;
		}
		Build();
		if ((base.transform.position - Vector3.zero).sqrMagnitude < 1e-10f)
		{
			base.transform.position = spawnPosition;
		}
		if (spawnOwnLight)
		{
			SetupLight();
		}
		if (spawnOwnCamera)
		{
			SetupCamera();
		}
		Debug.Log(string.Format("[PBD 2C] Жёстко-пластический каркас: {0} частиц, {1} ограничений | ", V, cA.Length) + string.Format("{0} (Y_p={1:F1} ГПа из ядра) | hFloor={2:F2} ", material, Pen(material).Yp / 1e9f, hFloor) + string.Format("(ожидание R₁/r₀=√(1/hFloor)={0:F2}) | {1}=выстрел", Mathf.Sqrt(1f / hFloor), fireKey));
	}

	private void Build()
	{
		int rn = Mathf.Max(rings, 4);
		int vp = Mathf.Max(vertsPerRing, 6);
		shellCount = vp * rn;
		V = (vp + 1) * rn;
		r0M = Mathf.Max(diameterMM * 0.5f / 1000f, 0.0001f);
		L0M = Mathf.Max(rodLengthMM / 1000f, 0.001f);
		pos = new Vector3[V];
		prev = new Vector3[V];
		pred = new Vector3[V];
		invMass = new float[V];
		aRing = new float[rn];
		for (int i = 0; i < rn; i++)
		{
			aRing[i] = (float)i * L0M / (rn - 1f);
			for (int j = 0; j < vp; j++)
			{
				float a = (float)j / (float)vp * Mathf.PI * 2f;
				pos[i * vp + j] = new Vector3(Mathf.Cos(a) * r0M, Mathf.Sin(a) * r0M, -aRing[i]);
			}
			pos[shellCount + i] = new Vector3(0f, 0f, -aRing[i]);
		}
		TateModel.Penetrator pen = Pen(material);
		float invM = (float)V / (pen.density * Mathf.PI * r0M * r0M * L0M);
		for (int i = 0; i < V; i++)
		{
			prev[i] = pos[i];
			invMass[i] = invM;
		}
		BuildConstraints(rn, vp);
		BuildRenderMesh(rn, vp);
		frames.Clear();
		simTime = 0f;
		playT = -1f;
	}

	private void BuildConstraints(int rn, int vp)
	{
		HashSet<long> seen = new HashSet<long>();
		List<int> la = new List<int>();
		List<int> lb = new List<int>();
		List<float> ll = new List<float>();
		List<byte> lt = new List<byte>();
		List<int> lly = new List<int>();
		void AddEdge(int a, int b, byte type, int layer)
		{
			long key = (long)Mathf.Min(a, b) * V + Mathf.Max(a, b);
			if (!seen.Add(key))
			{
				return;
			}
			la.Add(a);
			lb.Add(b);
			ll.Add((pos[a] - pos[b]).magnitude);
			lt.Add(type);
			lly.Add(layer);
		}
		for (int i = 0; i < rn; i++)
		{
			if (i + 1 < rn)
			{
				for (int j = 0; j < vp; j++)
				{
					int a = i * vp + j;
					int b = i * vp + (j + 1) % vp;
					AddEdge(a, b, 0, i);
					AddEdge(a, a + vp, 1, i);
					AddEdge(b, a + vp, 2, i);
					AddEdge(a, b + vp, 2, i);
					AddEdge(shellCount + i, a, 3, i);
				}
			}
			else
			{
				for (int j = 0; j < vp; j++)
				{
					int a = i * vp + j;
					int b = i * vp + (j + 1) % vp;
					AddEdge(a, b, 0, i);
					AddEdge(shellCount + i, a, 3, i);
				}
			}
		}
		for (int i = 0; i + 1 < rn; i++)
		{
			AddEdge(shellCount + i, shellCount + i + 1, 4, i);
		}
		cA = la.ToArray();
		cB = lb.ToArray();
		cType = lt.ToArray();
		cLayer = lly.ToArray();
		cL0 = ll.ToArray();
		cL0init = (float[])cL0.Clone();
		lambda = new float[cA.Length];
		epsP = new float[cA.Length];
		Favg = new float[cA.Length];
		layerH = new float[rn];
		layerCnt = new int[rn];
		diagH0 = new float[cA.Length];
		plasticFloor = new float[cA.Length];
		for (int i = 0; i < cA.Length; i++)
		{
			if (cType[i] == 2)
			{
				diagH0[i] = Mathf.Abs(pos[cA[i]].z - pos[cB[i]].z);
			}
			plasticFloor[i] = cL0init[i] * 0.15f;
		}
		aEdgeArea = aEdgeMult * Mathf.PI * r0M * r0M / vp;
		TateModel.Penetrator pen = Pen(material);
		float yMax = Mathf.Max(pen.Yp, 1000000f);
		Fy = new float[cA.Length];
		for (int i = 0; i < cA.Length; i++)
		{
			Fy[i] = yMax * aEdgeArea;
		}
	}

	private void BuildRenderMesh(int rn, int vp)
	{
		int tailCenterIdx = shellCount + rn - 1;
		List<int> triList = new List<int>(rn * vp * 6);
		for (int j = 0; j < vp; j++)
		{
			triList.Add(shellCount);
			triList.Add(j);
			triList.Add((j + 1) % vp);
		}
		for (int i = 1; i < rn; i++)
		{
			for (int j = 0; j < vp; j++)
			{
				int a = i * vp + j;
				int b = i * vp + (j + 1) % vp;
				triList.Add(a - vp);
				triList.Add(a);
				triList.Add(b - vp);
				triList.Add(b - vp);
				triList.Add(a);
				triList.Add(b);
			}
		}
		for (int j = 0; j < vp; j++)
		{
			int a = (rn - 1) * vp + j;
			int b = (rn - 1) * vp + (j + 1) % vp;
			triList.Add(tailCenterIdx);
			triList.Add(b);
			triList.Add(a);
		}
		tris = triList.ToArray();
		renderVerts = new Vector3[V];
		MeshFilter meshFilter = base.gameObject.GetComponent<MeshFilter>();
		if (meshFilter == null)
		{
			meshFilter = base.gameObject.AddComponent<MeshFilter>();
		}
		MeshRenderer meshRenderer = base.gameObject.GetComponent<MeshRenderer>();
		if (meshRenderer == null)
		{
			meshRenderer = base.gameObject.AddComponent<MeshRenderer>();
		}
		mesh = new Mesh();
		mesh.name = "RodPBD2C";
		meshFilter.sharedMesh = mesh;
		GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		Material material = new Material(cube.GetComponent<MeshRenderer>().sharedMaterial);
		material.color = new Color(0.45f, 0.5f, 0.58f);
		meshRenderer.sharedMaterial = material;
		Object.Destroy(cube.GetComponent<Collider>());
		Object.Destroy(cube);
		for (int i = 0; i < V; i++)
		{
			renderVerts[i] = pos[i];
		}
		mesh.Clear();
		mesh.vertices = renderVerts;
		mesh.triangles = tris;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	}

	private void PushMesh(Vector3[] p)
	{
		for (int i = 0; i < V; i++)
		{
			renderVerts[i] = p[i];
		}
		mesh.Clear();
		mesh.vertices = renderVerts;
		mesh.triangles = tris;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	}

	private void SetupLight()
	{
		GameObject lightGO = new GameObject("PBD_Light");
		lightGO.transform.SetParent(base.transform, false);
		lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
		Light light = lightGO.AddComponent<Light>();
		light.type = LightType.Directional;
		light.intensity = 1.2f;
	}

	private void SetupCamera()
	{
		GameObject camGO = new GameObject("PBD_Camera");
		camGO.transform.SetParent(base.transform, false);
		float dist = L0M * 0.9f + 1f;
		camGO.transform.localPosition = new Vector3(dist * 0.55f, dist * 0.5f, dist * 0.35f - L0M * 0.5f);
		camGO.transform.LookAt(base.transform.TransformPoint(new Vector3(0f, 0f, -L0M * 0.5f)));
		Camera camera = camGO.AddComponent<Camera>();
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
		camera.rect = new Rect(0.63f, 0.02f, 0.35f, 0.35f);
		camera.depth = 50f;
		camera.nearClipPlane = 0.01f;
	}

	[ContextMenu("Выстрел 2C (осевой)")]
	public void Fire()
	{
		Build();
		TateModel.Penetrator pen = Pen(material);
		TateModel.Target tgt = Tgt(target);
		float v0 = Mathf.Max(velocityMps, 1f);
		float u = Mathf.Clamp(TateModel.PenetrationVelocity(pen, tgt, v0), 0f, v0);
		float P = tgt.density * 0.5f * u * u + tgt.Rt;
		bool eats = P > pen.Yp;
		float frontSpeed = Mathf.Max(v0 - u, Mathf.Sqrt(pen.Yp / pen.density));
		float eventS = ((u > 1f) ? (plateMM / 1000f / u) : 0.0001f);
		int steps = Mathf.Max((int)(eventS / dtSub), 1);
		string[] parts = new string[5];
		object[] args = new object[4];
		args[0] = material;
		args[1] = v0;
		args[2] = target;
		args[3] = plateMM;
		parts[0] = string.Format("[PBD 2C] ВЫСТРЕЛ: {0} @{1:F0} vs {2} {3:F0}мм | ", args);
		parts[1] = string.Format("u={0:F0} м/с (Бернулли ядра) | P={1:F2} ГПа vs Y_p={2:F1} ГПа → ", u, P / 1e9f, pen.Yp / 1e9f);
		parts[2] = (eats ? ("ТЕЧЁТ (фронт (v−u)=" + frontSpeed.ToString("F0") + " м/с)") : "НЕ течёт (жёсткий контакт)");
		parts[3] = " | ";
		parts[4] = string.Format("событие {0:F0} мкс = {1} шагов dt={2:E0}", eventS * 1000000f, steps, dtSub);
		Debug.Log(string.Concat(parts));
		Stopwatch stopwatch = Stopwatch.StartNew();
		int frameStride = Mathf.Max(steps / Mathf.Max(savedFrames, 8), 1);
		frames.Clear();
		for (int step = 0; step < steps; step++)
		{
			Substep(eats, frontSpeed, P);
			if (step % frameStride == 0 || step == steps - 1)
			{
				frames.Add((Vector3[])pos.Clone());
			}
		}
		stopwatch.Stop();
		LogShotReport(stopwatch.Elapsed.TotalSeconds, eats);
		playT = 0f;
	}

	private void LogShotReport(double seconds, bool eats)
	{
		float noseZ = float.MinValue;
		for (int i = 0; i < shellCount; i++)
		{
			noseZ = Mathf.Max(noseZ, pos[i].z);
		}
		float bestR = 0f;
		float bestAt = 0f;
		for (int ring = 0; ring < rings; ring++)
		{
			float sumR = 0f;
			float sumZ = 0f;
			for (int j = 0; j < vertsPerRing; j++)
			{
				Vector3 p = pos[ring * vertsPerRing + j];
				sumR += Mathf.Sqrt(p.x * p.x + p.y * p.y);
				sumZ += p.z;
			}
			float avgR = sumR / (float)vertsPerRing;
			if (avgR > bestR)
			{
				bestR = avgR;
				bestAt = noseZ - sumZ / (float)vertsPerRing;
			}
		}
		float ratio = bestR / r0M;
		float maxEps = 0f;
		float maxStretch = 0f;
		for (int i = 0; i < cA.Length; i++)
		{
			maxEps = Mathf.Max(maxEps, epsP[i]);
			float l = (pos[cA[i]] - pos[cB[i]]).magnitude;
			maxStretch = Mathf.Max(maxStretch, Mathf.Abs(l - cL0[i]) / Mathf.Max(cL0[i], 1e-09f));
		}
		Debug.Log(string.Format("[PBD 2C] ГРИБ: R₁/r₀ = {0:F2} (якорь 1.35–1.50; этап 1 дал 1.42; ", ratio) + string.Format("max-кольцо на {0:F0} мм от носа) | hFloor={1:F2} kFlow={2:F2} aEdgeMult={3:F1}", bestAt * 1000f, hFloor, kFlow, aEdgeMult));
		Debug.Log(string.Format("[PBD 2C] max ε_p={0:F2} (база для ε_крит 2E) | остаточное растяжение рёбер {1:F0}% | ", maxEps, maxStretch * 100f) + string.Format("max F/Fy (λ-детектор, справочно)={0:F0} | предрасчёт {1:F1} с | ", maxFratio, seconds) + (eats ? "деформация показана" : "деформации НЕТ (σ<Y_p — физичность ✓)"));
	}

	private void Substep(bool eats, float frontSpeed, float P)
	{
		simTime += dtSub;
		float t = Mathf.Min(simTime / 1e-05f, 1f);
		float dt2 = dtSub * dtSub;
		float growth = t * P * Mathf.PI * r0M * r0M / (vertsPerRing + 1);
		for (int i = 0; i < V; i++)
		{
			pred[i] = pos[i] + (pos[i] - prev[i]) * damping;
		}
		for (int i = 0; i < vertsPerRing; i++)
		{
			pred[i] += Vector3.back * (growth * invMass[i] * dt2);
		}
		pred[shellCount] += Vector3.back * (growth * invMass[shellCount] * dt2);
		if (edgeViscosity > 0f)
		{
			for (int ci = 0; ci < cA.Length; ci++)
			{
				int a = cA[ci];
				int b = cB[ci];
				Vector3 d = pos[a] - pos[b];
				float len = d.magnitude;
				if (len < 1e-12f)
				{
					continue;
				}
				Vector3 n = d / len;
				Vector3 rel = (pred[a] - pos[a]) - (pred[b] - pos[b]);
				float s = Vector3.Dot(rel, n) * (edgeViscosity * 0.5f);
				Vector3 imp = n * s;
				pred[a] -= imp;
				pred[b] += imp;
			}
		}
		Array.Clear(lambda, 0, lambda.Length);
		for (int it = 0; it < nIter; it++)
		{
			for (int ci = 0; ci < cA.Length; ci++)
			{
				int a = cA[ci];
				int b = cB[ci];
				float w = invMass[a] + invMass[b];
				if (w <= 0f)
				{
					continue;
				}
				Vector3 d = pred[a] - pred[b];
				float len = d.magnitude;
				if (len < 1e-12f)
				{
					continue;
				}
				Vector3 n = d / len;
				float dl = (cL0[ci] - len) / w * sor;
				lambda[ci] += dl;
				pred[a] += n * (dl * invMass[a]);
				pred[b] -= n * (dl * invMass[b]);
			}
		}
		for (int ci = 0; ci < cA.Length; ci++)
		{
			Favg[ci] += (Mathf.Abs(lambda[ci]) / dt2 - Favg[ci]) * (dtSub / 5e-06f);
			maxFratio = Mathf.Max(maxFratio, Favg[ci] / Fy[ci]);
		}
		if (eats)
		{
			for (int ci = 0; ci < cA.Length; ci++)
			{
				if (cType[ci] != 1 && cType[ci] != 4)
				{
					continue;
				}
				int layer = cLayer[ci];
				float zNext = ((layer + 1 < rings) ? aRing[layer + 1] : (aRing[layer] + 0.0001f));
				float ringH = zNext - aRing[layer];
				float s = Mathf.Clamp01((frontSpeed * simTime - aRing[layer]) / Mathf.Max(ringH, 1e-09f));
				if (s <= 0f)
				{
					continue;
				}
				float newLen = (1f - (1f - hFloor) * s) * cL0init[ci];
				if (newLen < cL0[ci])
				{
					float shrink = Mathf.Min(cL0[ci] - newLen, frontSpeed * kFlow * dtSub);
					cL0[ci] -= shrink;
					epsP[ci] += shrink / Mathf.Max(cL0init[ci], 1e-09f);
				}
			}
		}
		ComputeLayerH();
		for (int ci = 0; ci < cA.Length; ci++)
		{
			byte type = cType[ci];
			if (type == 1 || type == 4)
			{
				continue;
			}
			int layer = cLayer[ci];
			float h = ((type == 2) ? Mathf.Max(layerH[layer], 0.1f) : RingH(layer));
			if (h >= 0.999f)
			{
				continue;
			}
			float ratio = Mathf.Min(Mathf.Sqrt(1f / h), 2.6f);
			float target;
			if (type == 3)
			{
				target = ratio * r0M;
			}
			else if (type == 0)
			{
				target = Mathf.Sin(Mathf.PI / (float)vertsPerRing) * (r0M * 2f * ratio);
			}
			else
			{
				float d = diagH0[ci];
				target = Mathf.Sqrt(Mathf.Max(cL0init[ci] * d - d * d, 0f) * ratio * ratio + (h * d) * (h * d));
			}
			if (target > plasticFloor[ci])
			{
				plasticFloor[ci] = Mathf.Min(cL0[ci] * 0.02f + cL0[ci], target);
			}
			if (cL0[ci] < target)
			{
				float inc = Mathf.Min(target - cL0[ci], cL0[ci] * 0.02f);
				cL0[ci] += inc;
				epsP[ci] += inc / Mathf.Max(cL0init[ci], 1e-09f);
			}
		}
		for (int i = 0; i < V; i++)
		{
			if (pred[i].z > 0f)
			{
				pred[i].z = 0f;
			}
		}
		for (int i = 0; i < V; i++)
		{
			prev[i] = pos[i];
			pos[i] = pred[i];
		}
	}

	private void ComputeLayerH()
	{
		Array.Clear(layerH, 0, rings);
		Array.Clear(layerCnt, 0, rings);
		for (int ci = 0; ci < cA.Length; ci++)
		{
			if (cType[ci] != 1 && cType[ci] != 4)
			{
				continue;
			}
			int layer = cLayer[ci];
			layerH[layer] += cL0[ci] / Mathf.Max(cL0init[ci], 1e-09f);
			layerCnt[layer]++;
		}
		for (int i = 0; i < rings; i++)
		{
			if (layerCnt[i] > 0)
			{
				layerH[i] /= layerCnt[i];
			}
			else
			{
				layerH[i] = 1f;
			}
		}
	}

	private float RingH(int ring)
	{
		float sum = 0f;
		int cnt = 0;
		if (ring < rings - 1)
		{
			sum += layerH[ring];
			cnt++;
		}
		if (ring > 0)
		{
			sum += layerH[ring - 1];
			cnt++;
		}
		if (cnt == 0)
		{
			return 1f;
		}
		return Mathf.Max(sum / (float)cnt, 0.1f);
	}

	private void Update()
	{
		if (pos == null)
		{
			return;
		}
		if (Input.GetKeyDown(fireKey))
		{
			Fire();
		}
		if (fire)
		{
			fire = false;
			Fire();
		}
		if (autoFireOnPlay && frames.Count == 0 && Time.timeSinceLevelLoad > 1f)
		{
			Fire();
		}
		if (playT >= 0f && frames.Count > 0)
		{
			playT += Time.deltaTime;
			float t = Mathf.Clamp01(playT / Mathf.Max(playbackSeconds, 0.1f));
			int fi = Mathf.Min((int)Mathf.Floor((float)(frames.Count - 1) * t), frames.Count - 1);
			PushMesh(frames[fi]);
			if (t >= 1f)
			{
				playT = -1f;
			}
		}
	}
}
