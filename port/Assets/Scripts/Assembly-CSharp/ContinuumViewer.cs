using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Unity.Collections;
using UnityEngine;

public class ContinuumViewer : MonoBehaviour
{
	[Header("Запись прогона")]
	[Tooltip("Путь к файлу .ultra.bin. Пусто — прогон берётся из RunInline().")]
	public string timelinePath;

	[Header("Показ")]
	public int frame;

	public bool playing;

	[Range(0.05f, 4f)]
	public float playbackSpeed;

	public bool showProjectile;

	public bool showTarget;

	public bool wireframe;

	public bool flatShaded;

	public bool sectionCut;

	[Header("Качество токарки")]
	[Tooltip("Число сегментов по окружности: 24 / 48 / 96.")]
	public int segments;

	public int profilePoints;

	[Header("Показания кадра (только чтение)")]
	public float readTimeUs;

	public float readErodedMassG;

	public float readTipVelocity;

	public int readProjectilePoints;

	public int readTargetPoints;

	public int readFrameCount;

	public float readFinTailVelocity;

	[Header("Оперение (ТОЛЬКО ПОКАЗ, в решателе его нет)")]
	public bool showFins;

	public float finRodDiameterM;

	public int finBlades;

	public float plateFaceZ;

	[Header("Сглаживание контура (ТОЛЬКО ПОКАЗ)")]
	public bool smoothProfile;

	[Header("Поле на срезе")]
	public bool showField;

	private Texture2D fieldTex;

	private Color32[] fieldPixels;

	private bool capFallbackReported;

	private Vector2[] checkBuf;

	private GeometryTimeline timeline;

	private ContinuumMeshBinder binder;

	private GameObject projectileGo;

	private GameObject targetGo;

	private UltraTailFins fins;

	private float playClock;

	public const float CapDepthNudgeCalibres = 0.002f;

	public bool DiagnoseHoles;

	public bool DiagnoseGeometry;

	public const float InsideDim = 0.62f;

	public const string ShaderBody = "ArmorSim/UltraSectionBody";

	public const string ShaderCap = "ArmorSim/UltraSectionCap";

	private float runMnRod;

	private float runMxRod;

	private float runMnTar;

	private float runMxTar;

	private bool runRangeReady;

	private readonly List<int> shellBounds;

	public ContinuumViewer()
	{
		timelinePath = "";
		playbackSpeed = 1f;
		showProjectile = true;
		showTarget = true;
		flatShaded = true;
		sectionCut = true;
		segments = 48;
		profilePoints = 256;
		showFins = true;
		finRodDiameterM = 0.03f;
		finBlades = 6;
		smoothProfile = true;
		showField = true;
		checkBuf = new Vector2[0];
		shellBounds = new List<int>();
	}

	private void OnEnable()
	{
		Rebuild();
	}

	private void OnDisable()
	{
		Teardown();
	}

	[ContextMenu("Загрузить и построить")]
	public void Rebuild()
	{
		Teardown();
		if (!string.IsNullOrEmpty(timelinePath))
		{
			timeline = GeometryTimelineFile.Load(timelinePath, out string error);
			if (timeline == null)
			{
				Debug.LogError("ULTRA просмотрщик: " + error);
				return;
			}
		}
		else if (timeline == null)
		{
			return;
		}
		if (timeline.frames == null || timeline.frames.Count == 0)
		{
			return;
		}
		readFrameCount = 0;
		BuildFieldTexture();
		ComputeRunRange();
		binder = new ContinuumMeshBinder(profilePoints, segments);
		binder.flatShaded = flatShaded;
		binder.halfTurn = sectionCut;
		binder.fieldBounds = timeline.fieldBounds;
		projectileGo = MakeBody("Стержень", binder.ProjectileMesh, new Color(0.52f, 0.56f, 0.66f), new Color(0.85f, 0.89f, 0.96f), nudgeToCamera: true, isProjectile: true);
		targetGo = MakeBody("Преграда", binder.TargetMesh, new Color(0.33f, 0.3f, 0.27f), new Color(0.6f, 0.51f, 0.42f));
		if (showFins)
		{
			MeshRenderer renderer = projectileGo.GetComponent<MeshRenderer>();
			fins = new UltraTailFins(projectileGo.transform, finRodDiameterM, renderer.sharedMaterial);
			fins.finCount = finBlades;
			fins.Bake(timeline, plateFaceZ);
		}
		if (DiagnoseGeometry)
		{
			Debug.LogWarning("ULTRA ЗОНД ГЕОМЕТРИИ ВКЛЮЧЁН — поле НЕ показывается. голубое = крышка стержня, красное = крышка преграды, синее = бок стержня, зелёное = бок преграды (стенка кратера), чёрное = не нарисовано ничего.");
		}
		CheckTimeline();
		if (DiagnoseGeometry)
		{
			DumpContours();
		}
		readFrameCount = timeline.frames.Count;
		if (frame < 0)
		{
			frame = 0;
		}
		else if (frame > readFrameCount - 1)
		{
			frame = readFrameCount - 1;
		}
		ShowFrame(frame);
	}

	private GameObject MakeBody(string name, Mesh mesh, Color albedo, Color capColor, bool nudgeToCamera = false, bool isProjectile = false)
	{
		if (DiagnoseGeometry)
		{
			if (isProjectile)
			{
				albedo = new Color(0.12f, 0.16f, 0.85f);
				capColor = new Color(0f, 0.95f, 1f);
			}
			else
			{
				albedo = new Color(0.1f, 0.7f, 0.2f);
				capColor = new Color(1f, 0.16f, 0.08f);
			}
		}
		GameObject gameObject = new GameObject(name);
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		gameObject.transform.localPosition = (nudgeToCamera ? new Vector3(0f, (0f - finRodDiameterM) * CapDepthNudgeCalibres, 0f) : Vector3.zero);
		gameObject.transform.localRotation = Quaternion.identity;
		gameObject.transform.localScale = Vector3.one;
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		Material material = new Material(NeedShader(ShaderBody, "Standard"));
		material.SetColor("_Color", albedo);
		material.SetFloat("_Metallic", 0.85f);
		material.SetFloat("_Glossiness", 0.45f);
		material.SetFloat("_InsideDim", InsideDim);
		material.SetFloat("_InsideProbe", DiagnoseHoles ? 1f : 0f);
		bool useField = fieldTex != null && !DiagnoseGeometry;
		Material material2 = new Material(NeedShader(ShaderCap, "Unlit/Texture"));
		material2.SetTexture("_MainTex", useField ? fieldTex : Texture2D.whiteTexture);
		material2.SetColor("_Color", useField ? Color.white : capColor);
		UltraFieldGrid.Bounds bounds = timeline.fieldBounds;
		material2.SetFloat("_FieldOn", (useField && bounds.Valid) ? 1f : 0f);
		material2.SetFloat("_FieldRMax", Mathf.Max(bounds.rMaxM, 1e-09f));
		material2.SetFloat("_FieldZMin", bounds.zMinM);
		material2.SetFloat("_FieldZSpan", Mathf.Max(bounds.zMaxM - bounds.zMinM, 1e-09f));
		meshRenderer.sharedMaterials = new Material[2] { material, material2 };
		return gameObject;
	}

	private static Shader NeedShader(string name, string fallback)
	{
		Shader shader = Shader.Find(name);
		if (shader != null)
		{
			return shader;
		}
		Debug.LogError("ULTRA сечение: шейдер " + name + " НЕ НАЙДЕН. Он обязан лежать в Assets/Resources/Shaders. Откат на " + fallback + " односторонний: изнанка среза рисоваться не будет, и кратер снова станет чёрной дырой.");
		return Shader.Find(fallback);
	}

	private void BuildFieldTexture()
	{
		if (!showField || timeline == null || !timeline.HasField)
		{
			return;
		}
		int w = timeline.fieldGridR;
		int h = timeline.fieldGridZ;
		if (fieldTex == null || fieldTex.width != w || fieldTex.height != h)
		{
			DestroyTex();
			fieldTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
			fieldTex.name = "ULTRA/поле";
			fieldTex.wrapMode = TextureWrapMode.Clamp;
			fieldTex.filterMode = FilterMode.Bilinear;
			fieldPixels = new Color32[w * h];
		}
	}

	private static int BestNeighbour(NativeArray<float> src, int i, int w, int h)
	{
		if (w <= 0 || h <= 0)
		{
			return -1;
		}
		int row = i / w;
		int col = i % w;
		int best = -1;
		float bestV = -1f;
		if (col > 0)
		{
			Consider(src, i - 1, ref best, ref bestV);
		}
		if (col < w - 1)
		{
			Consider(src, i + 1, ref best, ref bestV);
		}
		if (row > 0)
		{
			Consider(src, i - w, ref best, ref bestV);
		}
		if (row < h - 1)
		{
			Consider(src, i + w, ref best, ref bestV);
		}
		return best;
	}

	private static void Consider(NativeArray<float> src, int j, ref int best, ref float bestV)
	{
		if (src[j] > bestV)
		{
			bestV = src[j];
			best = j;
		}
	}

	private static void RangeOfBody(GeometryFrame f, int n, bool hasBody, byte body, out float min, out float max)
	{
		min = float.MaxValue;
		max = float.MinValue;
		for (int j = 0; j < n; j++)
		{
			float v = f.fieldPlastic[j];
			if (v <= -1f)
			{
				continue;
			}
			if (hasBody && f.fieldBody[j] != body)
			{
				continue;
			}
			if (v < min)
			{
				min = v;
			}
			if (v > max)
			{
				max = v;
			}
		}
	}

	private void DumpContours()
	{
		if (timeline == null || timeline.frames == null || timeline.frames.Count == 0)
		{
			return;
		}
		string path = Path.Combine(Application.dataPath, "../Docs/ultra-contour-dump.txt");
		try
		{
			using (StreamWriter w = new StreamWriter(path))
			{
				w.WriteLine("# кадр тело номер r z   (тело: 0 стержень, 1 преграда; метры)");
				w.WriteLine("# кадров всего " + timeline.frames.Count);
				int[] array = new int[12] { 0, 5, 10, 15, 18, 20, 21, 22, 23, 25, 30, 40 };
				for (int k = 0; k < array.Length; k++)
				{
					int fi = array[k];
					if (fi >= timeline.frames.Count)
					{
						continue;
					}
					GeometryFrame f = timeline.frames[fi];
					w.WriteLine(string.Format("# кадр {0} t={1:F2} мкс  петель: стержень {2}, преграда {3}", fi, f.time * 1000000f, f.projectileLoopCount, f.targetLoopCount));
					Write(w, fi, 0, f.projectileProfile);
					Write(w, fi, 1, f.targetProfile);
				}
			}
			Debug.LogWarning("ULTRA ЗОНД: контуры сняты в " + Path.GetFullPath(path));
		}
		catch (Exception ex)
		{
			Debug.LogError("ULTRA ЗОНД: контуры снять не удалось — " + ex.Message);
		}
	}

	private static void Write(StreamWriter w, int frame, int body, NativeArray<Vector2> pts)
	{
		if (!pts.IsCreated)
		{
			w.WriteLine(string.Format("{0} {1} НЕТ", frame, body));
			return;
		}
		for (int i = 0; i < pts.Length; i++)
		{
			w.WriteLine(string.Format("{0} {1} {2} {3:R} {4:R}", frame, body, i, pts[i].x, pts[i].y));
		}
	}

	private void DestroyTex()
	{
		if (fieldTex != null)
		{
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(fieldTex);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(fieldTex);
			}
			fieldTex = null;
		}
		fieldPixels = null;
	}

	private void ComputeRunRange()
	{
		runRangeReady = false;
		runMnTar = float.MaxValue;
		runMnRod = float.MaxValue;
		runMxTar = float.MinValue;
		runMxRod = float.MinValue;
		if (timeline == null)
		{
			return;
		}
		float worstRatio = 1f;
		int worstFrame = -1;
		float prevMax = -1f;
		float minOfMax = float.MaxValue;
		float maxOfMax = float.MinValue;
		int frameIndex = 0;
		foreach (GeometryFrame f in timeline.frames)
		{
			if (!f.fieldPlastic.IsCreated)
			{
				continue;
			}
			int n = f.fieldPlastic.Length;
			bool hasBody = f.fieldBody.IsCreated && f.fieldBody.Length >= n;
			float frameMax = float.MinValue;
			for (int j = 0; j < n; j++)
			{
				float v = f.fieldPlastic[j];
				if (v <= -1f)
				{
					continue;
				}
				if (hasBody && f.fieldBody[j] == 1)
				{
					if (v < runMnTar)
					{
						runMnTar = v;
					}
					if (v > runMxTar)
					{
						runMxTar = v;
					}
					if (v > frameMax)
					{
						frameMax = v;
					}
				}
				else
				{
					if (v < runMnRod)
					{
						runMnRod = v;
					}
					if (v > runMxRod)
					{
						runMxRod = v;
					}
				}
			}
			if (frameMax == float.MinValue)
			{
				continue;
			}
			if (frameMax < minOfMax)
			{
				minOfMax = frameMax;
			}
			if (frameMax > maxOfMax)
			{
				maxOfMax = frameMax;
			}
			if (prevMax > 0f)
			{
				float ratio = ((frameMax > prevMax) ? (frameMax / prevMax) : (prevMax / frameMax));
				if (ratio > worstRatio)
				{
					worstRatio = ratio;
					worstFrame = frameIndex;
				}
			}
			prevMax = frameMax;
			frameIndex++;
		}
		runRangeReady = runMxRod >= runMnRod || runMxTar >= runMnTar;
		if (maxOfMax > minOfMax)
		{
			Debug.Log("ULTRA поле: шкала на весь прогон. Покадровый максимум преграды гулял " + string.Format("{0:F4}…{1:F4}, ", minOfMax, maxOfMax) + string.Format("худший скачок между соседними кадрами ×{0:F2} на кадре {1}. ", worstRatio, worstFrame) + "Во столько же раз и менялся цвет при неизменной деформации — это и было мигание. Если множители близки к 1, причина мигания ДРУГАЯ.");
		}
	}

	private void UploadField(GeometryFrame f)
	{
		if (fieldTex == null || fieldPixels == null || !f.fieldPlastic.IsCreated)
		{
			return;
		}
		int n = ((fieldPixels.Length < f.fieldPlastic.Length) ? fieldPixels.Length : f.fieldPlastic.Length);
		bool hasBody = f.fieldBody.IsCreated && f.fieldBody.Length >= n;
		float rodMin;
		float rodMax;
		float tarMin;
		float tarMax;
		if (runRangeReady)
		{
			rodMin = runMnRod;
			rodMax = runMxRod;
			tarMin = runMnTar;
			tarMax = runMxTar;
		}
		else
		{
			RangeOfBody(f, n, hasBody, 0, out rodMin, out rodMax);
			RangeOfBody(f, n, hasBody, 1, out tarMin, out tarMax);
		}
		if (rodMin > rodMax && tarMin > tarMax)
		{
			return;
		}
		float spanRod = Mathf.Max(rodMax - rodMin, 1e-09f);
		float spanTar = Mathf.Max(tarMax - tarMin, 1e-09f);
		Color32 empty = (Color32)(DiagnoseHoles ? new Color(1f, 0f, 1f) : new Color(0.02f, 0.02f, 0.03f));
		int w = timeline.fieldGridR;
		int h = timeline.fieldGridZ;
		for (int i = 0; i < n; i++)
		{
			float v = f.fieldPlastic[i];
			int j = i;
			if (v <= -1f)
			{
				int neighbour = BestNeighbour(f.fieldPlastic, i, w, h);
				if (neighbour >= 0)
				{
					j = neighbour;
					v = f.fieldPlastic[neighbour];
				}
			}
			if (v <= -1f)
			{
				fieldPixels[i] = empty;
			}
			else if (hasBody && f.fieldBody[j] == 1)
			{
				fieldPixels[i] = (Color32)UltraComposition.RampTarget((v - tarMin) / spanTar);
			}
			else
			{
				fieldPixels[i] = (Color32)UltraComposition.Ramp((v - rodMin) / spanRod);
			}
		}
		fieldTex.SetPixels32(fieldPixels);
		fieldTex.Apply();
	}

	private void CheckTimeline()
	{
		if (timeline == null || timeline.frames == null || timeline.frames.Count == 0)
		{
			return;
		}
		List<int> badProj = new List<int>();
		List<int> badTarg = new List<int>();
		List<int> suspectFrames = new List<int>();
		List<int> failedFrames = new List<int>();
		string firstProjReason = null;
		string firstTargReason = null;
		int multiShellFrames = 0;
		int lostLoopFrames = 0;
		float worstAreaError = 0f;
		int capRetries0 = ContinuumMeshBinder.CapRetries;
		int shellSplit0 = ContinuumMeshBinder.ShellSplitMismatches;
		int simplified0 = MeridianCap.Simplified;
		int removedVerts0 = MeridianCap.RemovedVerts;
		int savedByRaw0 = ContinuumMeshBinder.CapSavedByRaw;
		for (int i = 0; i < timeline.frames.Count; i++)
		{
			GeometryFrame f = timeline.frames[i];
			int shells = ProfileLathe.SplitShells(f.targetProfile, shellBounds);
			if (shells > 1)
			{
				multiShellFrames++;
			}
			if (f.projectileLoopCount > 1 || f.targetLoopCount > shells)
			{
				lostLoopFrames++;
			}
			string projReason = Bad(f.projectileProfile);
			if (projReason != null)
			{
				badProj.Add(i);
				if (firstProjReason == null)
				{
					firstProjReason = projReason;
				}
			}
			string targReason = Bad(f.targetProfile);
			if (targReason != null)
			{
				badTarg.Add(i);
				if (firstTargReason == null)
				{
					firstTargReason = targReason;
				}
			}
			int failures0 = MeridianCap.Failures;
			int suspect0 = MeridianCap.Suspect;
			int savedByRawFrame0 = ContinuumMeshBinder.CapSavedByRaw;
			binder.Apply(f);
			if (MeridianCap.Failures - failures0 > ContinuumMeshBinder.CapSavedByRaw - savedByRawFrame0)
			{
				failedFrames.Add(i);
			}
			if (MeridianCap.Suspect > suspect0)
			{
				suspectFrames.Add(i);
				if (MeridianCap.LastAreaError > worstAreaError)
				{
					worstAreaError = MeridianCap.LastAreaError;
				}
			}
		}
		if (suspectFrames.Count > 0 || failedFrames.Count > 0)
		{
			string suspectPart = ((suspectFrames.Count > 0) ? ("  невязка площади (контур не простой): " + List(suspectFrames) + "\n" + string.Format("    худшая невязка {0:F1} % при допуске ", worstAreaError * 100f) + string.Format("{0:F0} %", MeridianCap.AreaTolerance * 100f) + "\n") : "");
			Debug.LogError(string.Format("ULTRA сечение: крышка построена НЕВЕРНО на {0} кадрах и не ", suspectFrames.Count) + string.Format("построена вовсе на {0} из {1}.", failedFrames.Count, timeline.frames.Count) + "\n" + suspectPart + "  Это дефект ПОСТРОЕНИЯ СЕЧЕНИЯ, а не решателя: контуры этих кадров могут быть годными.");
		}
		int capRetriesDelta = ContinuumMeshBinder.CapRetries - capRetries0;
		int savedByRawDelta = ContinuumMeshBinder.CapSavedByRaw - savedByRaw0;
		if (failedFrames.Count > 0)
		{
			Debug.LogError(string.Format("ULTRA сечение: КРАТЕР ЗАЛИТ на {0} кадрах из ", failedFrames.Count) + string.Format("{0} — крышка не построилась и заменена лентой.\n", timeline.frames.Count) + "  кадры: " + List(failedFrames) + "\n  На этих кадрах поле вокруг кратера закрыто и выглядит пропавшим.");
		}
		int simplifiedDelta = MeridianCap.Simplified - simplified0;
		int removedVertsDelta = MeridianCap.RemovedVerts - removedVerts0;
		if (simplifiedDelta > 0)
		{
			Debug.Log(string.Format("ULTRA сечение: контур распутан на {0} крышках, выброшено ", simplifiedDelta) + string.Format("{0} вершин — в среднем {1:F1} на крышку ", removedVertsDelta, (float)removedVertsDelta / (float)simplifiedDelta) + string.Format("при {0} точках профиля. ⚠️ Это ПОТЕРЯ геометрии: складка ", profilePoints) + "губы срезается. Единицы вершин — лепесток; десятки — срезано тело, и тогда распутывание негодно.");
		}
		if (capRetriesDelta > 0)
		{
			Debug.Log(string.Format("ULTRA сечение: крышка не резалась по СГЛАЖЕННОМУ контуру {0} раз, ", capRetriesDelta) + string.Format("из них {0} спас СЫРОЙ ({1:F0} %). ", savedByRawDelta, (float)savedByRawDelta * 100f / (float)capRetriesDelta) + "Все спасены ⇒ самопересечение наводит сглаживание. Ни один ⇒ складка настоящая, и чинить надо триангуляцию.");
		}
		int shellSplitDelta = ContinuumMeshBinder.ShellSplitMismatches - shellSplit0;
		if (shellSplitDelta > 0)
		{
			Debug.LogError(string.Format("ULTRA показ: разрез оболочек разошёлся с извлечением на {0} ", shellSplitDelta) + string.Format("кадрах из {0}. Первый случай напечатан выше со ", timeline.frames.Count) + "временем кадра и обоими числами; меши этих кадров недействительны.");
		}
		else
		{
			Debug.Log("ULTRA показ: разрез оболочек сошёлся с извлечением на всех " + string.Format("{0} кадрах (расхождений 0).", timeline.frames.Count));
		}
		if (badProj.Count > 0 || badTarg.Count > 0)
		{
			string projPart = ((badProj.Count > 0) ? ("  стержень: " + List(badProj) + "\n    причина первого: " + firstProjReason + "\n") : "");
			string targPart = ((badTarg.Count > 0) ? ("  преграда: " + List(badTarg) + "\n    причина первого: " + firstTargReason + "\n") : "");
			Debug.LogError(string.Format("ULTRA показ: НЕГОДНЫХ КАДРОВ — стержень {0}, преграда {1} ", badProj.Count, badTarg.Count) + string.Format("из {0}. Меши этих кадров построены по негодным данным, и ", timeline.frames.Count) + "как они выглядят, ни о чём не говорит.\n" + projPart + targPart + string.Format("  кадров с потерянными петлями: {0}; кадров с несколькими ", lostLoopFrames) + string.Format("оболочками преграды (пакет): {0}", multiShellFrames));
		}
		else
		{
			if (multiShellFrames > 0)
			{
				Debug.Log(string.Format("ULTRA показ: у преграды несколько оболочек на {0} кадрах ", multiShellFrames) + "(пакет слоёв — это и есть несколько замкнутых петель), и рисуются ВСЕ.");
			}
			if (lostLoopFrames > 0)
			{
				Debug.Log(string.Format("ULTRA показ: контуры годны на всех {0} кадрах; ", timeline.frames.Count) + string.Format("на {0} кадрах тело распадалось на петли СВЕРХ отданных наружу — ", lostLoopFrames) + "наружу идёт петля НАИБОЛЬШЕЙ ПЛОЩАДИ, остальные НЕ РИСУЮТСЯ.");
			}
		}
	}

	private static string List(List<int> a)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int count = a.Count;
		int num = Math.Min(count, 24);
		for (int i = 0; i < num; i++)
		{
			stringBuilder.Append(i > 0 ? ", " : "");
			stringBuilder.Append(a[i]);
		}
		if (count > num)
		{
			stringBuilder.Append(" … и ещё ");
			stringBuilder.Append(count - num);
		}
		return stringBuilder.ToString();
	}

	private string Bad(NativeArray<Vector2> raw)
	{
		if (!raw.IsCreated || raw.Length < 3)
		{
			return "контура нет или в нём меньше трёх точек";
		}
		int shellCount = ProfileLathe.SplitShells(raw, shellBounds);
		for (int s = 0; s < shellCount; s++)
		{
			int from = shellBounds[s];
			int count = shellBounds[s + 1] - from;
			if (count < 3)
			{
				return string.Format("оболочка {0}: точек {1} — меньше трёх", s, count);
			}
			if (checkBuf == null || checkBuf.Length != count)
			{
				checkBuf = new Vector2[count];
			}
			for (int i = 0; i < count; i++)
			{
				checkBuf[i] = raw[from + i];
			}
			string problem = UltraComposition.Validate(checkBuf, count, 0f);
			if (problem != null)
			{
				if (shellCount <= 1)
				{
					return problem;
				}
				return string.Format("оболочка {0} из {1}: {2}", s, shellCount, problem);
			}
		}
		return null;
	}

	private void Teardown()
	{
		DestroyTex();
		if (fins != null)
		{
			fins.Dispose();
			fins = null;
		}
		if (binder != null)
		{
			binder.Dispose();
			binder = null;
		}
		DestroySafe(ref projectileGo);
		DestroySafe(ref targetGo);
	}

	private static void DestroySafe(ref GameObject go)
	{
		if (go != null)
		{
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(go);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(go);
			}
			go = null;
		}
	}

	public void SetTimeline(GeometryTimeline tl)
	{
		timeline = tl;
		timelinePath = "";
		Rebuild();
	}

	private void Update()
	{
		if (timeline == null || binder == null)
		{
			return;
		}
		if (playing && timeline.dtOutput > 0f)
		{
			playClock += Time.deltaTime * playbackSpeed;
			if (playClock >= 1f / 60f)
			{
				playClock = Mathf.Min(playClock - 1f / 60f, 1f / 60f);
				frame = (frame + 1) % timeline.frames.Count;
				ShowFrame(frame);
			}
		}
		if (projectileGo != null)
		{
			projectileGo.SetActive(showProjectile);
		}
		if (targetGo != null)
		{
			targetGo.SetActive(showTarget);
		}
	}

	[ContextMenu("Показать текущий кадр")]
	public void ShowCurrent()
	{
		ShowFrame(frame);
	}

	public void ShowFrame(int i)
	{
		if (timeline == null || binder == null)
		{
			return;
		}
		if (timeline.frames == null || timeline.frames.Count == 0)
		{
			return;
		}
		if (i < 0)
		{
			i = 0;
		}
		else if (i > timeline.frames.Count - 1)
		{
			i = timeline.frames.Count - 1;
		}
		frame = i;
		binder.flatShaded = flatShaded;
		binder.halfTurn = sectionCut;
		binder.smoothProfile = smoothProfile && !wireframe;
		GeometryFrame f = timeline.frames[i];
		UploadField(f);
		int failures0 = MeridianCap.Failures;
		binder.Apply(f);
		if (MeridianCap.Failures > failures0 && !capFallbackReported)
		{
			capFallbackReported = true;
			Debug.LogWarning(string.Format("ULTRA сечение: крышка кадра {0} не режется по контуру ", i) + "(контур не простой) — нарисована прежней лентой, то есть кратер на этом кадре ЗАЛИТ. Дальнейшие такие кадры молчат.");
		}
		if (fins != null)
		{
			GeometryFrame previous = ((i > 0) ? timeline.frames[i - 1] : default(GeometryFrame));
			fins.Apply(f, previous, timeline.dtOutput, i);
		}
		readTimeUs = f.time * 1000000f;
		readErodedMassG = f.erodedMassKg * 1000f;
		readTipVelocity = f.tipAxialVelocity;
		readProjectilePoints = (f.projectileProfile.IsCreated ? f.projectileProfile.Length : 0);
		readTargetPoints = (f.targetProfile.IsCreated ? f.targetProfile.Length : 0);
		readFinTailVelocity = ((fins != null) ? fins.TailSpeed : 0f);
	}

	private void OnDrawGizmosSelected()
	{
		if (!wireframe || binder == null)
		{
			return;
		}
		Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.6f);
		if (showProjectile && binder.ProjectileMesh != null)
		{
			Gizmos.DrawWireMesh(binder.ProjectileMesh, base.transform.position, base.transform.rotation);
		}
		if (showTarget && binder.TargetMesh != null)
		{
			Gizmos.DrawWireMesh(binder.TargetMesh, base.transform.position, base.transform.rotation);
		}
	}

	[ContextMenu("★ Посчитать эталон и показать")]
	public void RunInline()
	{
		ContinuumBackend backend = new ContinuumBackend();
		ContinuumScenario s = ContinuumScenario.Etalon();
		GeometryTimeline tl = backend.Solve(s, null, default(CancellationToken));
		if (tl.diagnostics != null)
		{
			Debug.Log(tl.diagnostics.Report());
		}
		SetTimeline(tl);
	}

	[ContextMenu("Сохранить запись прогона")]
	public void SaveTimeline()
	{
		if (timeline == null)
		{
			Debug.LogWarning("нечего сохранять");
			return;
		}
		string path = Path.Combine(Application.dataPath, "..", "ultra_etalon.ultra.bin");
		GeometryTimelineFile.Save(timeline, path);
		Debug.Log("ULTRA: запись сохранена в " + Path.GetFullPath(path));
	}
}
