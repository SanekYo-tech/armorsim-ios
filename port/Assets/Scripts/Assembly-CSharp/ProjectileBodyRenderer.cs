using System.Collections.Generic;
using UnityEngine;

public class ProjectileBodyRenderer
{
	private class Probed
	{
		public string path;

		public GameObject prefab;

		public bool placeholder;

		public Vector3 noseDirLocal;

		public float nativeLength;

		public float nativeCalibre;

		public Vector3 nativeNose;

		public bool aspectLogged;
	}

	public const string DefaultResourcePath = "Models/HeatRound";

	public const string Rpg7ResourcePath = "Models/RGP-7ed";

	public const string Rpg7DroneResourcePath = "Models/RGP-7eDRONEd";

	public const string JavelinResourcePath = "Models/javelineNEitled";

	public const string GenericHeatResourcePath = "Models/defaulttled";

	public const string Tow2aResourcePath = "Models/Tow-2A";

	public const string Pars3ResourcePath = "Models/PARS_3_LR";

	private const float DegenerateMargin = 1.05f;

	private static readonly Dictionary<string, bool> DeclaredNoseAtHigh;

	private static string ResourcePath;

	private static GameObject prefab;

	private static bool probed;

	private static bool measured;

	private static Vector3 noseDirLocal;

	private static float nativeLength;

	private static float nativeCalibre;

	private static Vector3 nativeNose;

	private static readonly Dictionary<string, Probed> probes;

	private static GameObject cubeTemplate;

	private GameObject go;

	private Transform model;

	private Renderer[] renderers;

	private Probed rec;

	private float appliedLenW;

	private float appliedCalW;

	private Projectile.CoreType appliedCore;

	private bool coreApplied;

	static ProjectileBodyRenderer()
	{
		DeclaredNoseAtHigh = new Dictionary<string, bool>();
		DeclaredNoseAtHigh.Add("Models/RGP-7ed", value: true);
		DeclaredNoseAtHigh.Add("Models/RGP-7eDRONEd", value: true);
		DeclaredNoseAtHigh.Add("Models/javelineNEitled", value: true);
		DeclaredNoseAtHigh.Add("Models/defaulttled", value: true);
		ResourcePath = "Models/HeatRound";
		probes = new Dictionary<string, Probed>();
	}

	public ProjectileBodyRenderer()
	{
		appliedLenW = -1f;
		appliedCalW = -1f;
		appliedCore = (Projectile.CoreType)(-1);
	}

	public static bool Available => true;

	public Renderer DrawnRenderer
	{
		get
		{
			if (go != null && go.activeInHierarchy && renderers != null)
			{
				for (int i = 0; i < renderers.Length; i++)
				{
					if (renderers[i] != null && renderers[i].enabled)
					{
						return renderers[i];
					}
				}
			}
			return null;
		}
	}

	private static Probed Placeholder(string path)
	{
		if (cubeTemplate == null)
		{
			cubeTemplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
			cubeTemplate.name = "ProjectileBodyPlaceholderTemplate";
			BoxCollider component = cubeTemplate.GetComponent<BoxCollider>();
			if (component != null)
			{
				Object.Destroy(component);
			}
			cubeTemplate.SetActive(false);
			cubeTemplate.hideFlags = HideFlags.HideAndDontSave;
		}
		Probed probed = new Probed();
		probed.path = path ?? "";
		probed.prefab = cubeTemplate;
		probed.placeholder = true;
		probed.noseDirLocal = Vector3.forward;
		probed.nativeLength = 1f;
		probed.nativeCalibre = 1f;
		probed.nativeNose = new Vector3(0f, 0f, 0.5f);
		return probed;
	}

	private static Probed ProbeFor(string path)
	{
		path = path ?? "";
		if (probes.TryGetValue(path, out Probed value) && value.prefab != null)
		{
			return value;
		}
		if (value != null)
		{
			probes.Remove(path);
		}
		Probed probed;
		if (string.IsNullOrEmpty(path))
		{
			probed = Placeholder(path);
		}
		else
		{
			ResourcePath = path;
			probed = null;
			measured = false;
			Probe();
			if (measured)
			{
				probed = new Probed();
				probed.path = path;
				probed.prefab = prefab;
				probed.placeholder = false;
				probed.noseDirLocal = noseDirLocal;
				probed.nativeLength = nativeLength;
				probed.nativeCalibre = nativeCalibre;
				probed.nativeNose = nativeNose;
			}
			else
			{
				probed = Placeholder(path);
			}
		}
		probes[path] = probed;
		return probed;
	}

	private static void Probe()
	{
		if (probed)
		{
			return;
		}
		probed = true;
		prefab = Resources.Load<GameObject>(ResourcePath);
		if (prefab == null)
		{
			Debug.LogWarning("◈ МОДЕЛЬ КОРПУСА не найдена: Resources/" + ResourcePath + ". Кумулятивный выстрел до подрыва рисуется прежним цилиндром деформ-сетки. Это не ошибка — это откат.");
			return;
		}
		MeshFilter[] source = prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true);
		MeshFilter meshFilter = null;
		int num = -1;
		MeshFilter[] array = source;
		for (int i = 0; i < array.Length; i++)
		{
			MeshFilter meshFilter2 = array[i];
			if (!(meshFilter2.sharedMesh == null) && meshFilter2.sharedMesh.vertexCount > num)
			{
				num = meshFilter2.sharedMesh.vertexCount;
				meshFilter = meshFilter2;
			}
		}
		if (meshFilter == null)
		{
			Debug.LogWarning("◈ МОДЕЛЬ КОРПУСА Resources/" + ResourcePath + " загрузилась, но мешей в ней нет. Откат на цилиндр деформ-сетки.");
			return;
		}
		// PARTIAL(восстановление): замер геометрии (оси/габариты через Matrix4x4 + Corners,
		// выбор носа по тонкому концу и сверка с DeclaredNoseAtHigh) не восстановлен дословно —
		// см. 0x1804BB3E0 в дампе. Оставлен откат на плейсхолдер (measured = false).
		measured = false;
	}

	private static void LogProbe(int bodyVerts, int meshCount, float rNose, float rTail, bool declaredUsed, bool noseAtLow)
	{
		float num = Mathf.Max(rNose, rTail);
		float num2 = Mathf.Min(rNose, rTail);
		num2 = Mathf.Max(num2, 1e-06f);
		float num3 = num / num2;
		string text;
		if (declaredUsed)
		{
			text = string.Format("⚠️ ЗАМЕР НЕМОЙ (×{0:F2} — габарит тела достаёт до обеих полос). Нос взят из ", num3) + "ОБЪЯВЛЕНИЯ таблицы: конец " + (noseAtLow ? "minS" : "maxS") + " (основание — автономный разбор вершин этого файла, см. шапку блока)";
		}
		else if (num3 < 1.5f)
		{
			text = "⚠️ ЗАПАС МАЛЫЙ — если модель показана хвостом вперёд, причина здесь";
		}
		else
		{
			text = "запас уверенный";
		}
		Debug.Log(string.Format("◈ МОДЕЛЬ КОРПУСА Resources/{0} принята: мешей {1}, ", ResourcePath, meshCount) + string.Format("тело — крупнейший из них ({0} вершин). Замер в авторских единицах: ", bodyVerts) + string.Format("длина {0:F3}, калибр ТЕЛА {1:F3} (L/D {2:F2}). ", nativeLength, nativeCalibre, nativeLength / nativeCalibre) + string.Format("Нос определён по тонкому концу: радиус носовой полосы {0:F2} против ", rNose) + string.Format("хвостовой {0:F2}, ×{1:F2} — {2}.", rTail, num3, text));
	}

	private void LogAspectOnce(float lenW, float calW)
	{
		if (rec == null || rec.placeholder || rec.aspectLogged)
		{
			return;
		}
		rec.aspectLogged = true;
		float num = appliedLenW / appliedCalW;
		float num2 = Mathf.Max(num, 1e-06f);
		float num3 = lenW / calW / num2;
		if (Mathf.Abs(num3 - 1f) < 0.05f)
		{
			return;
		}
		Debug.Log(string.Format("◈ МОДЕЛЬ КОРПУСА растягивается НЕОДНОРОДНО: L/D модели {0:F2}, ", num) + string.Format("требуемое выстрелом {0:F2} ⇒ ×{1:F3} по стройности ", lenW / calW, num3) + string.Format("({0:+0.0;-0.0} %). Габарит показа верен по обоим измерениям ", (num3 - 1f) * 100f) + "(инвариант шага 26), расходятся ПРОПОРЦИИ: rodLengthMM/diameterMM задаёт реестр, а L/D — автор модели. Чтобы вернуть авторские пропорции, менять " + string.Format("надо реестр (rodLengthMM ≈ {0:F0} мм), а не меш.", num * calW * 999.99994f));
	}

	private static Vector3[] Corners(Bounds b)
	{
		Vector3 min = b.min;
		Vector3 max = b.max;
		return new Vector3[8]
		{
			new Vector3(min.x, min.y, min.z),
			new Vector3(max.x, min.y, min.z),
			new Vector3(min.x, max.y, min.z),
			new Vector3(max.x, max.y, min.z),
			new Vector3(min.x, min.y, max.z),
			new Vector3(max.x, min.y, max.z),
			new Vector3(min.x, max.y, max.z),
			new Vector3(max.x, max.y, max.z)
		};
	}

	public void SetActive(bool on)
	{
		if (go != null)
		{
			go.SetActive(on);
		}
	}

	public void SetModel(string resourcePath)
	{
		Probed probed = ProbeFor(resourcePath);
		if (rec != probed)
		{
			rec = probed;
			if (go != null)
			{
				Object.Destroy(go);
			}
			go = null;
			model = null;
			renderers = null;
			appliedLenW = -1f;
			appliedCalW = -1f;
			coreApplied = false;
		}
	}

	public void Destroy()
	{
		if (go != null)
		{
			Object.Destroy(go);
		}
		go = null;
		model = null;
		renderers = null;
	}

	public void Configure(float diameterM, float lengthM, Projectile.CoreType coreType, float worldPerMeter)
	{
		if (rec == null)
		{
			SetModel(DefaultResourcePath);
		}
		if (rec == null)
		{
			return;
		}
		float num = Mathf.Max(lengthM * worldPerMeter, 1e-04f);
		float num2 = Mathf.Max(diameterM * worldPerMeter, 1e-04f);
		if (go != null && Mathf.Approximately(num, appliedLenW) && Mathf.Approximately(num2, appliedCalW) && coreApplied && coreType == appliedCore)
		{
			return;
		}
		appliedLenW = num;
		appliedCalW = num2;
		LogAspectOnce(num, num2);
		if (go != null)
		{
			go.transform.localScale = new Vector3(num2 / rec.nativeCalibre, num2 / rec.nativeCalibre, num / rec.nativeLength);
		}
		if (!coreApplied || coreType != appliedCore)
		{
			appliedCore = coreType;
			coreApplied = true;
			ApplyMaterial();
		}
	}

	public void Render(Transform parent, Vector3 nosePos, Vector3 dir)
	{
		if (rec == null)
		{
			SetModel(DefaultResourcePath);
		}
		if (rec == null)
		{
			return;
		}
		Ensure(parent);
		if (!go.activeSelf)
		{
			go.SetActive(true);
		}
		Vector3 vector = ((dir.x * dir.x + dir.y * dir.y + dir.z * dir.z > 1e-12f) ? dir.normalized : Vector3.forward);
		go.transform.position = nosePos;
		go.transform.rotation = Quaternion.LookRotation(vector);
	}

	private void Ensure(Transform parent)
	{
		if (go != null)
		{
			return;
		}
		go = new GameObject("ProjectileBodyModel");
		go.transform.SetParent(parent, false);
		GameObject gameObject = Object.Instantiate(rec.prefab, go.transform);
		model = gameObject.transform;
		gameObject.SetActive(true);
		gameObject.hideFlags = HideFlags.None;
		Quaternion rotation = Quaternion.FromToRotation(rec.noseDirLocal, Vector3.forward);
		model.localRotation = rotation;
		model.localPosition = -(rotation * rec.nativeNose);
		model.localScale = Vector3.one;
		StripSceneComponents(gameObject);
		renderers = model.GetComponentsInChildren<Renderer>(includeInactive: true);
		ApplyMaterial();
		if (appliedLenW > 0f && appliedCalW > 0f)
		{
			go.transform.localScale = new Vector3(appliedCalW / rec.nativeCalibre, appliedCalW / rec.nativeCalibre, appliedLenW / rec.nativeLength);
		}
	}

	private static void StripSceneComponents(GameObject root)
	{
		// NOTE(восстановление): пять generic-параметров GetComponentsInChildren не удалось
		// разрешить из дампа (токены 0x1825FBB08/0x1825FBC48/0x1825FBA68/0x1825FBBA8/0x1825FB9C8);
		// Collider подтверждён косвенно (CrewVisualAsset: "must not own colliders or rigidbodies"),
		// остальные — наиболее вероятные кандидаты для зачистки модели корпуса.
		Collider[] array = root.GetComponentsInChildren<Collider>(includeInactive: true);
		for (int i = 0; i < array.Length; i++)
		{
			Object.Destroy(array[i]);
		}
		Rigidbody[] array2 = root.GetComponentsInChildren<Rigidbody>(includeInactive: true);
		for (int j = 0; j < array2.Length; j++)
		{
			Object.Destroy(array2[j]);
		}
		AudioSource[] array3 = root.GetComponentsInChildren<AudioSource>(includeInactive: true);
		for (int k = 0; k < array3.Length; k++)
		{
			Object.Destroy(array3[k]);
		}
		ParticleSystem[] array4 = root.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
		for (int l = 0; l < array4.Length; l++)
		{
			Object.Destroy(array4[l]);
		}
		Animator[] array5 = root.GetComponentsInChildren<Animator>(includeInactive: true);
		for (int m = 0; m < array5.Length; m++)
		{
			Object.Destroy(array5[m]);
		}
	}

	private void ApplyMaterial()
	{
		if (renderers == null || !coreApplied)
		{
			return;
		}
		Material material = MaterialLibrary.Core(appliedCore);
		for (int i = 0; i < renderers.Length; i++)
		{
			if (renderers[i] == null)
			{
				continue;
			}
			Material[] array = new Material[(renderers[i].sharedMaterials.Length >= 1) ? renderers[i].sharedMaterials.Length : 1];
			for (int j = 0; j < array.Length; j++)
			{
				array[j] = material;
			}
			renderers[i].sharedMaterials = array;
		}
	}
}
