using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class SuperTapeRender : IDisposable
{
	public const int Bands = 9;

	private readonly GameObject root;

	private readonly Camera cam;

	private readonly List<MeshFilter> solid;

	private readonly List<MeshFilter> stencil;

	private readonly List<MeshRenderer> capRend;

	private readonly List<Material> solidMat;

	private readonly List<Material> stencilMat;

	private readonly List<Material> capMat;

	private readonly List<Texture3D> fieldTex;

	private readonly List<Mesh> ownedMeshes;

	private MeshFilter debrisMf;

	private Material debrisMat;

	private Mesh debrisMesh;

	private RenderTexture rt;

	private MeshFilter decorMf;

	private Material decorMat;

	private Bounds decorBounds;

	private bool decorPresent;

	private bool decorVisible;

	public static readonly Color DecorColor;

	public const float DebrisCubeM = 0.0012f;

	public float orthoSize;

	public Vector3 centre;

	public Color background;

	public float yawDeg;

	public float pitchDeg;

	public float depthHalf;

	public float capSideMin;

	public const float PitchLimitDeg = 85f;

	public static bool SabotageCtorThrows;

	private static int layer;

	private Vector3 cutNormal;

	private bool cutOn;

	private readonly List<Color[]> colorBuf;

	private byte[] halfScratch;

	public static bool SabotageNoDebrisRender;

	private Vector3[] debrisVerts;

	private Vector3[] debrisNorms;

	private Color[] debrisCols;

	private int[] debrisTris;

	private int debrisFilled;

	public static int DebrisDrawCap;

	private static readonly Vector3[] CubeNormals;

	private static readonly int[] CubeTris;

	public const int Msaa = 4;

	private RenderTexture msaaRt;

	public bool HasDecor => decorPresent;

	public int DecorTrianglesDrawn { get; private set; }

	public Bounds DecorBounds => decorBounds;

	public bool DecorVisible
	{
		get
		{
			return decorVisible;
		}
		set
		{
			decorVisible = value;
			if (decorMf != null)
			{
				decorMf.gameObject.SetActive(value);
			}
		}
	}

	public static int Layer => ResolveLayer();

	public int LastDebrisDrawn { get; private set; }

	public int LastDebrisTotal { get; private set; }

	public void SetDecor(SuperDecor d)
	{
		decorPresent = false;
		DecorTrianglesDrawn = 0;
		decorBounds = default(Bounds);
		if (d == null || d.Empty)
		{
			if (decorMf != null)
			{
				decorMf.sharedMesh.Clear();
				decorMf.gameObject.SetActive(false);
			}
			return;
		}
		if (decorMat == null)
		{
			decorMat = new Material(Shader.Find("Super/S3Solid"));
			decorMat.renderQueue = 1999;
			decorMat.SetColor("_Color", DecorColor);
		}
		decorMf = NewMeshObject("декор за областью", decorMat, ownMesh: true);
		int num = ResolveLayer();
		if (num >= 0)
		{
			decorMf.gameObject.layer = num;
		}
		int num2 = d.TriangleCount * 3;
		Vector3[] verts = new Vector3[num2];
		int[] tris = new int[num2];
		int num3 = 0;
		foreach (SuperDecor.Piece piece in d.pieces)
		{
			Vector3[] solid = piece.vertices;
			for (int i = 0; i < solid.Length; i++)
			{
				verts[num3] = solid[i];
				tris[num3] = num3;
				num3++;
			}
		}
		Mesh sharedMesh = decorMf.sharedMesh;
		sharedMesh.Clear();
		sharedMesh.SetVertices(verts);
		sharedMesh.SetTriangles(tris, 0, calculateBounds: true);
		sharedMesh.RecalculateNormals();
		decorBounds = sharedMesh.bounds;
		DecorTrianglesDrawn = num2 / 3;
		decorPresent = true;
		decorMf.gameObject.SetActive(decorVisible);
	}

	private float DecorDepthHalf(Vector3 toCam)
	{
		if (!decorPresent || !decorVisible)
		{
			return 0f;
		}
		Vector3 center = decorBounds.center;
		Vector3 extents = decorBounds.extents;
		float num = 0f;
		for (int i = 0; i < 8; i++)
		{
			Vector3 vector = new Vector3(((i & 1) == 0) ? (center.x - extents.x - centre.x) : (center.x + extents.x - centre.x), ((i & 2) == 0) ? (center.y - extents.y - centre.y) : (center.y + extents.y - centre.y), ((i & 4) == 0) ? (center.z - extents.z - centre.z) : (center.z + extents.z - centre.z));
			num = Mathf.Max(num, Mathf.Abs(Vector3.Dot(vector, toCam)));
		}
		return num;
	}

	public static float WrapYawDeg(float yawDeg)
	{
		if (yawDeg <= -180f || yawDeg > 180f)
		{
			yawDeg = Mathf.Repeat(yawDeg + 180f, 360f) - 180f;
			if (yawDeg <= -180f)
			{
				yawDeg = 180f;
			}
		}
		return yawDeg;
	}

	public static void ViewBasis(Vector3 n, float yawDeg, float pitchDeg, out Vector3 toCamera, out Vector3 up)
	{
		Vector3 vector = ((Mathf.Abs(n.y) > 0.9f) ? Vector3.right : Vector3.up);
		if (yawDeg == 0f && pitchDeg == 0f)
		{
			toCamera = n;
			up = vector;
			return;
		}
		yawDeg = WrapYawDeg(yawDeg);
		pitchDeg = Mathf.Clamp(pitchDeg, 0f - PitchLimitDeg, PitchLimitDeg);
		Vector3 axis = Vector3.Cross(n, vector).normalized;
		Quaternion rotation = Quaternion.AngleAxis(yawDeg, axis) * Quaternion.AngleAxis(0f - pitchDeg, n);
		toCamera = (rotation * n).normalized;
		up = (rotation * vector).normalized;
	}

	public static float OrthoForView(float halfY, float halfZ, float aspect, Vector3 n, float yawDeg, float pitchDeg, out float depthHalf)
	{
		if (yawDeg == 0f && pitchDeg == 0f)
		{
			depthHalf = 0f;
			return OrthoFor(halfY, halfZ, aspect);
		}
		return OrthoForProjected(halfY, halfZ, aspect, n, yawDeg, pitchDeg, out depthHalf);
	}

	public static float OrthoForProjected(float halfY, float halfZ, float aspect, Vector3 n, float yawDeg, float pitchDeg, out float depthHalf)
	{
		ViewBasis(n, yawDeg, pitchDeg, out var toCamera, out var up);
		Vector3 vector = Vector3.Cross(toCamera, up).normalized;
		up = Vector3.Cross(toCamera, vector).normalized;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		for (int i = 0; i < 8; i++)
		{
			Vector3 vector2 = new Vector3(((i & 1) == 0) ? halfY : (-halfY), ((i & 2) == 0) ? halfY : (-halfY), ((i & 4) == 0) ? halfZ : (-halfZ));
			num = Mathf.Max(num, Mathf.Abs(Vector3.Dot(vector2, vector)));
			num2 = Mathf.Max(num2, Mathf.Abs(Vector3.Dot(vector2, up)));
			num3 = Mathf.Max(num3, Mathf.Abs(Vector3.Dot(vector2, toCamera)));
		}
		depthHalf = num3;
		return Mathf.Max(0.005f, Mathf.Max(num2, num / Mathf.Max(0.2f, aspect)) * 1.06f);
	}

	public SuperTapeRender(int bodyCount)
	{
		solid = new List<MeshFilter>();
		stencil = new List<MeshFilter>();
		capRend = new List<MeshRenderer>();
		solidMat = new List<Material>();
		stencilMat = new List<Material>();
		capMat = new List<Material>();
		fieldTex = new List<Texture3D>();
		ownedMeshes = new List<Mesh>();
		decorVisible = true;
		centre = new Vector3(0f, 0f, -0.02f);
		background = new Color(0.92f, 0.92f, 0.93f, 1f);
		orthoSize = 0.08f;
		cutNormal = new Vector3(1f, 0f, 0f);
		cutOn = true;
		colorBuf = new List<Color[]>();
		halfScratch = new byte[0];
		debrisVerts = new Vector3[0];
		debrisNorms = new Vector3[0];
		debrisCols = new Color[0];
		debrisTris = new int[0];
		if (SabotageCtorThrows)
		{
			throw new InvalidOperationException("ДИВЕРСИЯ ВОРОТ SuperTapeRender.SabotageCtorThrows: рендер показа намеренно не создан");
		}
		root = new GameObject("SUPER S3 показ");
		root.hideFlags = (HideFlags)61;
		GameObject gameObject = new GameObject("камера");
		gameObject.transform.SetParent(root.transform, worldPositionStays: false);
		cam = gameObject.AddComponent<Camera>();
		cam.orthographic = true;
		cam.clearFlags = (CameraClearFlags)2;
		cam.nearClipPlane = 0.01f;
		cam.farClipPlane = 2f;
		cam.enabled = false;
		Shader shader = Shader.Find("Super/S3Solid");
		Shader shader2 = Shader.Find("Super/S3Stencil");
		Shader shader3 = Shader.Find("Super/S3Cap");
		if (shader == null || shader2 == null || shader3 == null)
		{
			throw new InvalidOperationException("шейдеры S3 не найдены — Shader.Find вернул null (батч без графики? снимкам нужен -batchmode БЕЗ -nographics)");
		}
		for (int i = 0; i < bodyCount; i++)
		{
			Material material = new Material(shader);
			material.renderQueue = 2000 + i;
			material.SetColor("_Color", ((i == 0) ? new Color(0.58f, 0.55f, 0.52f, 1f) : new Color(0.63f, 0.65f, 0.68f, 1f)));
			Material material2 = new Material(shader2);
			material2.renderQueue = 3000 + 10 * i;
			Material material3 = new Material(shader3);
			material3.renderQueue = 3001 + 10 * i;
			solidMat.Add(material);
			stencilMat.Add(material2);
			capMat.Add(material3);
			solid.Add(NewMeshObject(string.Format("тело {0} поверхность", i), material, ownMesh: true));
			stencil.Add(NewMeshObject(string.Format("тело {0} трафарет", i), material2, ownMesh: false));
			if (i == 0)
			{
				debrisMat = new Material(shader);
				debrisMat.renderQueue = 2002;
				debrisMat.SetFloat("_UseField", 1f);
				debrisMf = NewMeshObject("обломки", debrisMat, ownMesh: true);
				debrisMesh = debrisMf.sharedMesh;
			}
			GameObject gameObject2 = GameObject.CreatePrimitive(PrimitiveType.Quad);
			gameObject2.name = string.Format("тело {0} крышка", i);
			gameObject2.hideFlags = (HideFlags)61;
			gameObject2.transform.SetParent(root.transform, worldPositionStays: false);
			UnityEngine.Object.DestroyImmediate(gameObject2.GetComponent<Collider>());
			MeshRenderer meshRenderer = gameObject2.GetComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = material3;
			capRend.Add(meshRenderer);
			fieldTex.Add(null);
		}
		PutOnOwnLayer();
	}

	private static int ResolveLayer()
	{
		if (layer == -2)
		{
			layer = -1;
			for (int num = 31; num >= 8; num--)
			{
				if (string.IsNullOrEmpty(LayerMask.LayerToName(num)))
				{
					layer = num;
					break;
				}
			}
			if (layer < 0)
			{
				Debug.LogError("[Super] Свободного слоя нет — камера показа снимает ВСЮ сцену, и в кадр попадёт песочница (плиты сборщика). Освободи пользовательский слой.");
			}
		}
		return layer;
	}

	private void PutOnOwnLayer()
	{
		int num = ResolveLayer();
		if (num < 0)
		{
			cam.cullingMask = -1;
			return;
		}
		Component[] componentsInChildren = root.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].gameObject.layer = num;
		}
		cam.cullingMask = 1 << (num & 0x1F);
	}

	private MeshFilter NewMeshObject(string name, Material m, bool ownMesh)
	{
		GameObject gameObject = new GameObject(name);
		gameObject.hideFlags = (HideFlags)61;
		gameObject.transform.SetParent(root.transform, worldPositionStays: false);
		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
		if (ownMesh)
		{
			Mesh mesh = new Mesh();
			mesh.indexFormat = (UnityEngine.Rendering.IndexFormat)1;
			meshFilter.sharedMesh = mesh;
			ownedMeshes.Add(mesh);
		}
		Renderer renderer = gameObject.AddComponent<MeshRenderer>();
		renderer.sharedMaterial = m;
		return meshFilter;
	}

	public void SetFrame(SuperTape.Frame f, int fieldIdx, bool cut, float azimuthDeg, bool fieldOnSurface = false)
	{
		if (!f.HasField(fieldIdx))
		{
			throw new InvalidOperationException(string.Format("кадр раскодирован под канал {0}, а показать просят {1} — ", f.detailField, fieldIdx) + "лента читалась выборочно (SuperTape.Detail.OneField)");
		}
		float num = azimuthDeg * Mathf.Deg2Rad;
		cutNormal = new Vector3(Mathf.Cos(num), Mathf.Sin(num), 0f);
		cutOn = cut;
		float num2 = f.fieldMin[fieldIdx];
		float num3 = f.fieldMax[fieldIdx];
		for (int i = 0; i < f.bodies.Length; i++)
		{
			SuperTape.BodyFrame bodyFrame = f.bodies[i];
			Mesh mesh = solid[i].sharedMesh;
			mesh.Clear();
			mesh.SetVertices(bodyFrame.vertices);
			mesh.SetTriangles(bodyFrame.triangles, 0, calculateBounds: false);
			mesh.RecalculateNormals();
			mesh.bounds = BoundsOf(bodyFrame.aabbLo, bodyFrame.aabbHi);
			if (fieldOnSurface)
			{
				Color[] array = ColorBuffer(i, bodyFrame.vertices.Length);
				float[] array2 = bodyFrame.fields[fieldIdx];
				float num4 = 1f / Mathf.Max(num3 - num2, 1e-20f);
				for (int j = 0; j < bodyFrame.vertices.Length; j++)
				{
					array[j] = BandColor((array2[j] - num2) * num4);
				}
				mesh.SetColors(array, 0, bodyFrame.vertices.Length);
			}
			stencil[i].sharedMesh = mesh;
			solidMat[i].SetFloat("_CutOn", cut ? 1f : 0f);
			solidMat[i].SetFloat("_UseField", fieldOnSurface ? 1f : 0f);
			solidMat[i].SetVector("_CutPoint", Vector3.zero);
			solidMat[i].SetVector("_CutNormal", cutNormal);
			stencilMat[i].SetVector("_CutPoint", Vector3.zero);
			stencilMat[i].SetVector("_CutNormal", cutNormal);
			EnsureFieldTex(i, bodyFrame.bnx, bodyFrame.bny, bodyFrame.bnz);
			Texture3D texture3D = fieldTex[i];
			byte[] array3 = null;
			if (bodyFrame.blockHalf != null)
			{
				array3 = bodyFrame.blockHalf[fieldIdx];
			}
			if (array3 == null)
			{
				array3 = HalvesOf(bodyFrame.blockFields[fieldIdx]);
			}
			texture3D.SetPixelData(array3, 0);
			texture3D.Apply(false);
			capMat[i].SetTexture("_FieldTex", texture3D);
			capMat[i].SetVector("_BlockLo", bodyFrame.blockLo);
			capMat[i].SetVector("_BlockInv", new Vector3(1f / ((float)bodyFrame.bnx * bodyFrame.blockCell), 1f / ((float)bodyFrame.bny * bodyFrame.blockCell), 1f / ((float)bodyFrame.bnz * bodyFrame.blockCell)));
			capMat[i].SetVector("_HalfTexel", new Vector3(0.5f / (float)bodyFrame.bnx, 0.5f / (float)bodyFrame.bny, 0.5f / (float)bodyFrame.bnz));
			capMat[i].SetFloat("_RangeMin", num2);
			capMat[i].SetFloat("_RangeMax", num3);
			capMat[i].SetFloat("_Bands", (float)Bands);
			stencil[i].gameObject.SetActive(cut);
			capRend[i].gameObject.SetActive(cut);
		}
		BuildDebris(f, fieldIdx, num2, num3);
		debrisMat.SetFloat("_CutOn", cut ? 1f : 0f);
		debrisMat.SetVector("_CutPoint", Vector3.zero);
		debrisMat.SetVector("_CutNormal", cutNormal);
		if (decorMat != null)
		{
			decorMat.SetFloat("_CutOn", cut ? 1f : 0f);
			decorMat.SetVector("_CutPoint", Vector3.zero);
			decorMat.SetVector("_CutNormal", cutNormal);
		}
		ApplyView();
	}

	private static Bounds BoundsOf(Vector3 lo, Vector3 hi)
	{
		return new Bounds((lo + hi) * 0.5f, (hi - lo) * 0.5f);
	}

	private Color[] ColorBuffer(int b, int n)
	{
		while (colorBuf.Count <= b)
		{
			colorBuf.Add(null);
		}
		if (colorBuf[b] == null || colorBuf[b].Length < n)
		{
			colorBuf[b] = new Color[Mathf.Max(n, 1024)];
		}
		return colorBuf[b];
	}

	private void EnsureFieldTex(int b, int nx, int ny, int nz)
	{
		Texture3D texture3D = fieldTex[b];
		if (texture3D != null && texture3D.width == nx && texture3D.height == ny && texture3D.depth == nz)
		{
			return;
		}
		if (texture3D != null)
		{
			UnityEngine.Object.DestroyImmediate(texture3D);
		}
		Texture3D texture3D2 = new Texture3D(nx, ny, nz, (TextureFormat)15, mipChain: false);
		texture3D2.wrapMode = (TextureWrapMode)1;
		texture3D2.filterMode = (FilterMode)1;
		fieldTex[b] = texture3D2;
	}

	private byte[] HalvesOf(float[] v)
	{
		if (halfScratch.Length < v.Length * 2)
		{
			halfScratch = new byte[v.Length * 2];
		}
		for (int i = 0; i < v.Length; i++)
		{
			ushort num = Mathf.FloatToHalf(v[i]);
			halfScratch[i * 2] = (byte)num;
			halfScratch[i * 2 + 1] = (byte)(num >> 8);
		}
		return halfScratch;
	}

	public void ApplyView()
	{
		Vector3 toCamera;
		Vector3 up;
		for (int i = 0; i < capRend.Count; i++)
		{
			Transform transform = capRend[i].transform;
			transform.rotation = Quaternion.FromToRotation(Vector3.forward, cutNormal);
			Vector3 vector = centre;
			if (cutNormal.sqrMagnitude > Mathf.Epsilon)
			{
				vector = centre - cutNormal * (Vector3.Dot(centre, cutNormal) / cutNormal.sqrMagnitude);
			}
			transform.position = vector;
			float num = Mathf.Max(orthoSize * 8f, capSideMin);
			transform.localScale = new Vector3(num, num, 1f);
		}
		ViewBasis(cutNormal, yawDeg, pitchDeg, out toCamera, out up);
		float num2 = Mathf.Max(depthHalf, DecorDepthHalf(toCamera));
		float num3 = orthoSize * 3f + 0.02f + num2;
		cam.transform.position = toCamera * num3 + centre;
		cam.transform.rotation = Quaternion.LookRotation(-toCamera, up);
		cam.nearClipPlane = Mathf.Max(orthoSize * 0.05f, 0.0001f);
		cam.farClipPlane = orthoSize * 3f + 0.02f + orthoSize * 4f + orthoSize * 0.05f + num2 * 2f;
		cam.backgroundColor = background;
		if (!cam.orthographic)
		{
			float num4 = Mathf.Max(cam.fieldOfView * 0.5f * ((float)Math.PI / 180f), 0.0001f);
			float num5 = orthoSize / num4 + num2;
			cam.transform.position = toCamera * num5 + centre;
			cam.nearClipPlane = Mathf.Max(orthoSize * 0.05f, 0.0001f);
			cam.farClipPlane = orthoSize * 4f + num5 + orthoSize * 0.05f + num2 * 2f;
		}
		else
		{
			cam.orthographicSize = orthoSize;
		}
	}

	private void BuildDebris(SuperTape.Frame f, int fieldIdx, float mn, float mx)
	{
		LastDebrisDrawn = 0;
		LastDebrisTotal = f.debrisCount;
		int num = SabotageNoDebrisRender ? 0 : f.debrisCount;
		int num2 = Mathf.Max(DebrisDrawCap, 1);
		int num3 = 1;
		if (num > num2)
		{
			num3 = (num - 1 + num2) / num2;
			if (num3 > 1)
			{
				num = (num - 1 + num3) / num3;
			}
		}
		debrisMf.gameObject.SetActive(num > 0);
		if (num == 0)
		{
			debrisMesh.Clear();
			return;
		}
		EnsureDebrisCapacity(num);
		float num4 = 1f / Mathf.Max(mx - mn, 1e-20f);
		float[] array = f.debrisFields[fieldIdx];
		float num5 = DebrisCubeM * 0.5f;
		float num6 = float.MaxValue;
		float num7 = float.MaxValue;
		float num8 = float.MaxValue;
		float num9 = -float.MaxValue;
		float num10 = -float.MaxValue;
		float num11 = -float.MaxValue;
		int num12 = 0;
		for (int i = 0; i < num; i++)
		{
			Vector3 vector = f.debrisPos[num12];
			Color item = BandColor((array[num12] - mn) * num4);
			for (int j = 0; j < 8; j++)
			{
				debrisVerts[i * 8 + j] = new Vector3(vector.x + (((j & 1) == 0) ? (-num5) : num5), vector.y + (((j & 2) == 0) ? (-num5) : num5), vector.z + (((j & 4) == 0) ? (-num5) : num5));
				debrisCols[i * 8 + j] = item;
			}
			num6 = Mathf.Min(num6, vector.x);
			num7 = Mathf.Min(num7, vector.y);
			num8 = Mathf.Min(num8, vector.z);
			num9 = Mathf.Max(num9, vector.x);
			num10 = Mathf.Max(num10, vector.y);
			num11 = Mathf.Max(num11, vector.z);
			num12 += num3;
		}
		debrisMesh.Clear();
		debrisMesh.SetVertices(debrisVerts, 0, num * 8);
		debrisMesh.SetNormals(debrisNorms, 0, num * 8);
		debrisMesh.SetColors(debrisCols, 0, num * 8);
		debrisMesh.SetTriangles(debrisTris, 0, num * 36, 0, calculateBounds: false, baseVertex: 0);
		debrisMesh.bounds = BoundsOf(new Vector3(num6, num7, num8) - Vector3.one * num5, new Vector3(num9, num10, num11) + Vector3.one * num5);
		LastDebrisDrawn = debrisMesh.vertexCount / 8;
	}

	private void EnsureDebrisCapacity(int nD)
	{
		int num = debrisVerts.Length / 8;
		if (num < nD)
		{
			int num2 = num * 2;
			if (num2 < 1024)
			{
				num2 = 1024;
			}
			if (num2 < nD)
			{
				num2 = nD;
			}
			Array.Resize(ref debrisVerts, num2 * 8);
			Array.Resize(ref debrisNorms, num2 * 8);
			Array.Resize(ref debrisCols, num2 * 8);
			Array.Resize(ref debrisTris, num2 * 36);
		}
		for (int i = debrisFilled; i < nD; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				debrisNorms[i * 8 + j] = CubeNormals[j];
			}
			for (int k = 0; k < 36; k++)
			{
				debrisTris[i * 36 + k] = CubeTris[k] + i * 8;
			}
		}
		if (nD > debrisFilled)
		{
			debrisFilled = nD;
		}
	}

	private static Vector3[] BuildCubeNormals()
	{
		Vector3[] array = new Vector3[8];
		for (int i = 0; i < 8; i++)
		{
			array[i] = new Vector3((float)(((i & 1) == 0) ? 1 : (-1)), (float)(((i & 2) == 0) ? 1 : (-1)), (float)(((i & 4) == 0) ? 1 : (-1))).normalized;
		}
		return array;
	}

	public static void FitTape(SuperTape.Reader tape, out float centreZ, out float halfY, out float halfZ)
	{
		float num = 0f - float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = 0f;
		for (int i = 0; i < tape.FrameCount; i++)
		{
			SuperTape.Frame frame = tape.ReadFrame(i, SuperTape.Detail.BoundsOnly, -1);
			SuperTape.BodyFrame[] bodies = frame.bodies;
			for (int j = 0; j < bodies.Length; j++)
			{
				SuperTape.BodyFrame bodyFrame = bodies[j];
				num2 = Mathf.Min(num2, bodyFrame.aabbLo.z);
				num = Mathf.Max(num, bodyFrame.aabbHi.z);
				num3 = Mathf.Max(num3, Mathf.Abs(bodyFrame.aabbLo.y), Mathf.Abs(bodyFrame.aabbHi.y), Mathf.Abs(bodyFrame.aabbLo.x), Mathf.Abs(bodyFrame.aabbHi.x));
			}
			Vector3[] debrisPos = frame.debrisPos;
			for (int k = 0; k < frame.debrisCount; k++)
			{
				Vector3 vector = debrisPos[k];
				num2 = Mathf.Min(num2, vector.z);
				num = Mathf.Max(num, vector.z);
				num3 = Mathf.Max(num3, Mathf.Abs(vector.y), Mathf.Abs(vector.x));
			}
		}
		Framing(num2, num, num3, out centreZ, out halfY, out halfZ);
	}

	public static void Framing(float zLo, float zHi, float half, out float centreZ, out float halfY, out float halfZ)
	{
		if (zLo > zHi)
		{
			zHi = 0f;
			zLo = 0f;
		}
		centreZ = (zLo + zHi) * 0.5f;
		halfY = half;
		halfZ = (zHi - zLo) * 0.5f;
	}

	public static float OrthoFor(float halfY, float halfZ, float aspect)
	{
		float num = Mathf.Max(0.2f, aspect);
		float num2 = Mathf.Max(halfY, halfZ / num);
		return Mathf.Max(0.005f, num2 * 1.06f);
	}

	public static float CapSideFor(float orthoSize)
	{
		return orthoSize * 8f;
	}

	public static float SceneRadius(float halfY, float halfZ)
	{
		return Mathf.Sqrt(2f * halfY * halfY + halfZ * halfZ) * 1.06f;
	}

	public static float CapSideForView(float orthoSize, float halfY, float halfZ)
	{
		return Mathf.Max(orthoSize * 8f, SceneRadius(halfY, halfZ) * 2f);
	}

	public static float DepthFloorFor(float halfY, float halfZ)
	{
		return SceneRadius(halfY, halfZ);
	}

	public static float CamDistFor(float orthoSize)
	{
		return orthoSize * 3f + 0.02f;
	}

	public static float NearFor(float orthoSize)
	{
		return Mathf.Max(orthoSize * 0.05f, 0.0001f);
	}

	public static float FarFor(float orthoSize)
	{
		return CamDistFor(orthoSize) + orthoSize * 4f + orthoSize * 0.05f;
	}

	public RenderTexture RenderToTexture(int w, int h)
	{
		if (rt == null || rt.width != w || rt.height != h)
		{
			if (rt != null)
			{
				rt.Release();
			}
			if (msaaRt != null)
			{
				msaaRt.Release();
				msaaRt = null;
			}
			rt = new RenderTexture(w, h, 24);
			if (msaaRt == null)
			{
				msaaRt = new RenderTexture(w, h, 24);
				msaaRt.antiAliasing = 4;
				if (!msaaRt.Create())
				{
					msaaRt.Release();
					msaaRt = null;
				}
			}
		}
		cam.targetTexture = ((msaaRt != null) ? msaaRt : rt);
		cam.Render();
		cam.targetTexture = null;
		if (msaaRt != null)
		{
			Graphics.Blit(msaaRt, rt);
		}
		return rt;
	}

	public void CapturePng(SuperTape.Frame f, SuperTape.Header hdr, int fieldIdx, bool cut, float azimuthDeg, int w, int h, string path, bool fieldOnSurface = false)
	{
		SetFrame(f, fieldIdx, cut, azimuthDeg, fieldOnSurface);
		RenderTexture renderTexture = RenderToTexture(w, h);
		RenderTexture.active = renderTexture;
		Texture2D texture2D = new Texture2D(w, h, (TextureFormat)3, mipChain: false);
		texture2D.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
		texture2D.Apply();
		RenderTexture.active = null;
		Color32[] pixels = texture2D.GetPixels32();
		SuperLegend.Compose(pixels, w, h, hdr.fieldNames[fieldIdx], hdr.fieldUnits[fieldIdx], f.fieldMin[fieldIdx], f.fieldMax[fieldIdx], 9, f.simTimeS, f.index, hdr.frameCount);
		texture2D.SetPixels32(pixels);
		texture2D.Apply();
		File.WriteAllBytes(path, texture2D.EncodeToPNG());
		UnityEngine.Object.DestroyImmediate(texture2D);
	}

	public static Color BandColor(float t)
	{
		float num = Mathf.Min(Mathf.Clamp(t, 0f, 1f) * (float)Bands, 8f);
		return Rainbow(Mathf.Floor(num) * 0.125f);
	}

	public static Color Rainbow(float t)
	{
		return new Color(Mathf.Clamp(1.5f - Mathf.Abs(4f * t - 3f), 0f, 1f), Mathf.Clamp(1.5f - Mathf.Abs(4f * t - 2f), 0f, 1f), Mathf.Clamp(1.5f - Mathf.Abs(4f * t - 1f), 0f, 1f), 1f);
	}

	public void Dispose()
	{
		if (rt != null)
		{
			rt.Release();
			rt = null;
		}
		if (msaaRt != null)
		{
			msaaRt.Release();
			msaaRt = null;
		}
		if (ownedMeshes != null)
		{
			foreach (Mesh ownedMesh in ownedMeshes)
			{
				if (ownedMesh != null)
				{
					UnityEngine.Object.DestroyImmediate(ownedMesh);
				}
			}
		}
		if (fieldTex != null)
		{
			foreach (Texture3D item in fieldTex)
			{
				if (item != null)
				{
					UnityEngine.Object.DestroyImmediate(item);
				}
			}
		}
		if (debrisMat != null)
		{
			UnityEngine.Object.DestroyImmediate(debrisMat);
		}
		if (decorMat != null)
		{
			UnityEngine.Object.DestroyImmediate(decorMat);
		}
		if (solidMat != null)
		{
			foreach (Material item2 in solidMat)
			{
				UnityEngine.Object.DestroyImmediate(item2);
			}
		}
		if (stencilMat != null)
		{
			foreach (Material item3 in stencilMat)
			{
				UnityEngine.Object.DestroyImmediate(item3);
			}
		}
		if (capMat != null)
		{
			foreach (Material item4 in capMat)
			{
				UnityEngine.Object.DestroyImmediate(item4);
			}
		}
		if (root != null)
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	static SuperTapeRender()
	{
		DecorColor = new Color(0.76f, 0.77f, 0.79f, 1f);
		SabotageCtorThrows = false;
		layer = -2;
		SabotageNoDebrisRender = false;
		DebrisDrawCap = 400000;
		CubeNormals = BuildCubeNormals();
		CubeTris = new int[36]
		{
			0, 2, 1, 1, 2, 3, 4, 5, 6, 5,
			7, 6, 0, 1, 4, 1, 5, 4, 2, 6,
			3, 3, 6, 7, 0, 4, 2, 2, 4, 6,
			1, 3, 5, 3, 7, 5
		};
	}
}
