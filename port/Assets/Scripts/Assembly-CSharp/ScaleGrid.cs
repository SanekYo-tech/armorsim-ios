using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ScaleGrid : MonoBehaviour
{
	[Header("Метровая сетка (§5) — инструмент проверки масштаба")]
	public bool show;

	[Header("Размер поля")]
	public int halfCells = SceneDefaults.GridHalfCells;

	public float cellM = 1f;

	public float groundY = 0.5f;

	[Header("Дециметровая подсетка")]
	public bool showRuler = true;

	public float rulerLengthM = 1f;

	[Header("Цвета")]
	public Color minorColor = new Color(0.42f, 0.44f, 0.47f, 1f);

	public Color majorColor = new Color(0.26f, 0.28f, 0.32f, 1f);

	public Color rulerColorA = new Color(0.18f, 0.19f, 0.22f, 1f);

	public Color rulerColorB = new Color(0.72f, 0.74f, 0.78f, 1f);

	private const string RootName = "ScaleGridLines";

	private Transform root;

	private bool builtShow;

	private int builtHalf;

	private float builtCell;

	private float builtY;

	private float builtRuler;

	private bool builtRulerOn;

	private static readonly Dictionary<Color, Material> matCache = new Dictionary<Color, Material>();

	public static ScaleGrid Instance { get; private set; }

	public static void Bootstrap()
	{
		if (Object.FindFirstObjectByType<ScaleGrid>() != null)
		{
			return;
		}
		GameObject gameObject = new GameObject("ScaleGrid");
		gameObject.AddComponent<ScaleGrid>();
	}

	private void Awake()
	{
		Instance = this;
	}

	private void OnEnable()
	{
		Rebuild();
	}

	private void Update()
	{
		if (show == builtShow && halfCells == builtHalf && Mathf.Approximately(cellM, builtCell) && Mathf.Approximately(groundY, builtY) && showRuler == builtRulerOn && Mathf.Approximately(rulerLengthM, builtRuler))
		{
			return;
		}
		Rebuild();
	}

	public void Toggle()
	{
		show = !show;
		Rebuild();
	}

	private void Rebuild()
	{
		builtShow = show;
		builtHalf = halfCells;
		builtCell = cellM;
		builtY = groundY;
		builtRulerOn = showRuler;
		builtRuler = rulerLengthM;
		if (root != null)
		{
			if (Application.isPlaying)
			{
				Object.Destroy(root.gameObject);
			}
			else
			{
				Object.DestroyImmediate(root.gameObject);
			}
			root = null;
		}
		if (!show)
		{
			return;
		}
		GameObject gameObject = new GameObject(RootName);
		root = gameObject.transform;
		root.SetParent(base.transform, worldPositionStays: false);
		int num = Mathf.Clamp(halfCells, 1, 200);
		float num2 = Mathf.Max(cellM, 0.001f);
		float num4 = num * num2;
		for (int i = -num; i <= num; i++)
		{
			bool flag = i == 0 || i % 5 == 0;
			Color color = flag ? majorColor : minorColor;
			float num5 = num2 * (flag ? 0.014f : 0.006f);
			Line(string.Format("GridX_{0}", i), new Vector3(i * num2, groundY, 0f), new Vector3(num5, num5, num4 * 2f + num5), color);
			Line(string.Format("GridZ_{0}", i), new Vector3(0f, groundY, i * num2), new Vector3(num4 * 2f + num5, num5, num5), color);
		}
		if (showRuler)
		{
			BuildRuler(num2);
		}
		Debug.Log(string.Format("[ScaleGrid] Сетка включена: {0}×{1} клеток по {2:F2} м ", num * 2, num * 2, cellM) + string.Format("(поле {0:F1}×{1:F1} м). ", num4 * 2f, num4 * 2f) + (string.Format("Масштаб сцены: 1 мировая единица = {0:F3} м ({1:F0} мм). ", 1f, 999.99994f) + "Габарит нарисованного объекта равен физическому (инвариант шага 26)."));
	}

	private void BuildRuler(float cell)
	{
		float num = Mathf.Max(rulerLengthM, 0.05f);
		int num2 = Mathf.Clamp((int)Math.Round(rulerLengthM * 10f), 1, 100);
		float num3 = num / (float)num2;
		float num4 = num3 * 0.98f;
		for (int i = 0; i < num2; i++)
		{
			Line(string.Format("RulerDm_{0}", i), new Vector3((i + 0.5f) * num3, groundY + 0.004f, -0.03f), new Vector3(num4, 0.004f, 0.03f), (i % 2 == 0) ? rulerColorA : rulerColorB);
		}
		float num5 = num3 / 10f;
		float num6 = num5 * 0.9f;
		for (int j = 0; j < 10; j++)
		{
			Line(string.Format("RulerCm_{0}", j), new Vector3((j + 0.5f) * num5, groundY + 0.008f, -0.051f), new Vector3(num6, 0.004f, 0.018f), (j % 2 == 0) ? rulerColorB : rulerColorA);
		}
	}

	private void Line(string name, Vector3 pos, Vector3 scale, Color c)
	{
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		gameObject.name = name;
		Collider component = gameObject.GetComponent<Collider>();
		if (component != null)
		{
			if (Application.isPlaying)
			{
				Object.Destroy(component);
			}
			else
			{
				Object.DestroyImmediate(component);
			}
		}
		gameObject.transform.SetParent(root, worldPositionStays: false);
		gameObject.transform.localPosition = pos;
		gameObject.transform.localScale = scale;
		Renderer component2 = gameObject.GetComponent<Renderer>();
		component2.shadowCastingMode = ShadowCastingMode.Off;
		component2.receiveShadows = false;
		component2.sharedMaterial = GridMaterial(c);
	}

	private static Material GridMaterial(Color c)
	{
		if (matCache.TryGetValue(c, out var material) && material != null)
		{
			return material;
		}
		Shader shader = Shader.Find("Unlit/Color");
		if (shader == null)
		{
			shader = Shader.Find("Standard");
		}
		Material material2 = new Material(shader);
		if (material2.HasProperty("_Color"))
		{
			material2.color = c;
		}
		if (shader != null && shader.name == "Unlit/Color" && material2 != null)
		{
			material2.SetOverrideTag("RenderType", "ArmorSimUnlit");
		}
		matCache[c] = material2;
		return material2;
	}
}
