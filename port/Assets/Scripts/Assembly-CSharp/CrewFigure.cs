using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class CrewFigure : MonoBehaviour
{
	public struct FigurePart
	{
		public string name;

		public Vector3 scale;

		public Vector3 pos;

		public CrewShape shape;

		public CrewRegion region;
	}

	private struct OrganVisual
	{
		public Renderer renderer;

		public CrewOrgan organ;

		public Color[] baseColors;
	}

	public const string RootName = "CrewFigureParts";

	public const string OrgansName = "CrewFigureOrgans";

	public const float WidthM = 0.6f;

	public const float HeightM = 1.3f;

	public const float DepthMM = 700f;

	public const float AuthoredDepthM = 0.4f;

	public static readonly FigurePart[] Parts = new FigurePart[25]
	{
		P("Crew_Head", new Vector3(0.267f, 0.169f, 0.5f), new Vector3(0f, 0.408f, -0.05f), CrewShape.Ellipsoid, CrewRegion.Head),
		P("Crew_Neck", new Vector3(0.183f, 0.069f, 0.275f), new Vector3(0f, 0.304f, -0.05f), CrewShape.Box, CrewRegion.Neck),
		P("Crew_Chest", new Vector3(0.567f, 0.185f, 0.575f), new Vector3(0f, 0.169f, -0.138f), CrewShape.Box, CrewRegion.Thorax),
		P("Crew_Abdomen", new Vector3(0.5f, 0.155f, 0.538f), new Vector3(0f, 0.004f, -0.15f), CrewShape.Box, CrewRegion.Abdomen),
		P("Crew_Pelvis", new Vector3(0.567f, 0.131f, 0.575f), new Vector3(0f, -0.131f, -0.125f), CrewShape.Box, CrewRegion.Pelvis),
		P("Crew_ShoulderL", new Vector3(0.217f, 0.1f, 0.325f), new Vector3(-0.325f, 0.238f, -0.138f), CrewShape.Ellipsoid, CrewRegion.Limb),
		P("Crew_ShoulderR", new Vector3(0.217f, 0.1f, 0.325f), new Vector3(0.325f, 0.238f, -0.138f), CrewShape.Ellipsoid, CrewRegion.Limb),
		P("Crew_UpperArmL", new Vector3(0.183f, 0.2f, 0.275f), new Vector3(-0.333f, 0.138f, -0.1f), CrewShape.Box, CrewRegion.Limb),
		P("Crew_UpperArmR", new Vector3(0.183f, 0.2f, 0.275f), new Vector3(0.333f, 0.138f, -0.1f), CrewShape.Box, CrewRegion.Limb),
		P("Crew_ElbowL", new Vector3(0.183f, 0.085f, 0.275f), new Vector3(-0.333f, 0.038f, -0.075f), CrewShape.Ellipsoid, CrewRegion.Limb),
		P("Crew_ElbowR", new Vector3(0.183f, 0.085f, 0.275f), new Vector3(0.333f, 0.038f, -0.075f), CrewShape.Ellipsoid, CrewRegion.Limb),
		P("Crew_ForearmL", new Vector3(0.167f, 0.077f, 0.625f), new Vector3(-0.325f, 0.031f, 0.15f), CrewShape.Box, CrewRegion.Limb),
		P("Crew_ForearmR", new Vector3(0.167f, 0.077f, 0.625f), new Vector3(0.325f, 0.031f, 0.15f), CrewShape.Box, CrewRegion.Limb),
		P("Crew_HandL", new Vector3(0.15f, 0.069f, 0.275f), new Vector3(-0.317f, 0.031f, 0.513f), CrewShape.Box, CrewRegion.Limb),
		P("Crew_HandR", new Vector3(0.15f, 0.069f, 0.275f), new Vector3(0.317f, 0.031f, 0.513f), CrewShape.Box, CrewRegion.Limb),
		P("Crew_ThighL", new Vector3(0.25f, 0.108f, 0.825f), new Vector3(-0.158f, -0.146f, 0.038f), CrewShape.Box, CrewRegion.Limb),
		P("Crew_ThighR", new Vector3(0.25f, 0.108f, 0.825f), new Vector3(0.158f, -0.146f, 0.038f), CrewShape.Box, CrewRegion.Limb),
		P("Crew_KneeL", new Vector3(0.233f, 0.108f, 0.35f), new Vector3(-0.158f, -0.177f, 0.47f), CrewShape.Ellipsoid, CrewRegion.Limb),
		P("Crew_KneeR", new Vector3(0.233f, 0.108f, 0.35f), new Vector3(0.158f, -0.177f, 0.47f), CrewShape.Ellipsoid, CrewRegion.Limb),
		P("Crew_ShinL", new Vector3(0.2f, 0.254f, 0.325f), new Vector3(-0.158f, -0.304f, 0.438f), CrewShape.Box, CrewRegion.Limb),
		P("Crew_ShinR", new Vector3(0.2f, 0.254f, 0.325f), new Vector3(0.158f, -0.304f, 0.438f), CrewShape.Box, CrewRegion.Limb),
		P("Crew_AnkleL", new Vector3(0.15f, 0.062f, 0.2f), new Vector3(-0.158f, -0.446f, 0.438f), CrewShape.Ellipsoid, CrewRegion.Limb),
		P("Crew_AnkleR", new Vector3(0.15f, 0.062f, 0.2f), new Vector3(0.158f, -0.446f, 0.438f), CrewShape.Ellipsoid, CrewRegion.Limb),
		P("Crew_FootL", new Vector3(0.183f, 0.069f, 0.55f), new Vector3(-0.158f, -0.465f, 0.588f), CrewShape.Box, CrewRegion.Limb),
		P("Crew_FootR", new Vector3(0.183f, 0.069f, 0.55f), new Vector3(0.158f, -0.465f, 0.588f), CrewShape.Box, CrewRegion.Limb)
	};

	private Transform partsRoot;

	private Transform organsRoot;

	private readonly List<Renderer> parts;

	private readonly List<Renderer> organs;

	private readonly List<Material> ownedMaterials;

	private readonly List<OrganVisual> organVisuals;

	private bool lastShellShown;

	private bool lastOrgansShown;

	private bool subscribed;

	private static int lastToggleFrame = -1;

	public static readonly Color OrganHitColor = new Color(0.92f, 0.025f, 0.035f, 1f);

	public CrewFigure()
	{
		parts = new List<Renderer>();
		organs = new List<Renderer>();
		ownedMaterials = new List<Material>();
		organVisuals = new List<OrganVisual>();
		lastShellShown = true;
	}

	private static float ZNorm => AuthoredDepthM / (DepthMM / 1000f);

	public static float ZNormFactor => ZNorm;

	public static KeyCode OrgansToggleKey => ControlBindings.Key(ControlAction.CrewOrgans);

	private static bool SectionOn => SectionView.Instance != null && SectionView.Instance.sectionOn;

	public static bool OrgansShown => GameSettings.CrewOrgans || SectionOn;

	public static bool ShellShown => !GameSettings.CrewOrgans || SectionOn;

	private static FigurePart P(string n, Vector3 s, Vector3 p, CrewShape sh, CrewRegion r)
	{
		return new FigurePart
		{
			name = n,
			scale = s,
			pos = p,
			shape = sh,
			region = r
		};
	}

	public static PrimitiveType PrimitiveOf(CrewShape s)
	{
		if (s != CrewShape.Ellipsoid)
		{
			return PrimitiveType.Cube;
		}
		return PrimitiveType.Sphere;
	}

	public static FigurePart NormalizedPart(int i)
	{
		FigurePart figurePart = Parts[i];
		figurePart.pos.z *= ZNorm;
		figurePart.scale.z *= ZNorm;
		return figurePart;
	}

	public static Vector3 PartsHalfExtent()
	{
		Vector3 vector = Vector3.zero;
		for (int i = 0; i < Parts.Length; i++)
		{
			FigurePart figurePart = NormalizedPart(i);
			vector.x = Mathf.Max(vector.x, Mathf.Abs(figurePart.pos.x) + figurePart.scale.x * 0.5f);
			vector.y = Mathf.Max(vector.y, Mathf.Abs(figurePart.pos.y) + figurePart.scale.y * 0.5f);
			vector.z = Mathf.Max(vector.z, Mathf.Abs(figurePart.pos.z) + figurePart.scale.z * 0.5f);
		}
		return vector;
	}

	public static void Refresh(ArmorPlate plate)
	{
		if (plate == null)
		{
			return;
		}
		CrewFigure crewFigure = plate.GetComponent<CrewFigure>();
		if (!plate.IsCrew)
		{
			if (crewFigure != null)
			{
				crewFigure.Clear();
			}
			return;
		}
		if (DepthMM > plate.thicknessMM + 0.5f)
		{
			Debug.Log(string.Format("Экипаж [{0}]: глубина габарита {1:F0} мм меньше ", plate.name, plate.thicknessMM) + string.Format("{0:F0} мм — манекен в неё не помещается, поправлено. ", DepthMM) + "⚠ Нарисованная фигура не изменилась: расширена только коробка коллайдера.");
			plate.SetThickness(DepthMM);
			return;
		}
		if (crewFigure == null)
		{
			crewFigure = plate.gameObject.AddComponent<CrewFigure>();
		}
		crewFigure.Build(plate);
	}

	private void Clear()
	{
		CrewImpactHoles crewImpactHoles = GetComponent<CrewImpactHoles>();
		if (crewImpactHoles != null)
		{
			crewImpactHoles.Bind(Array.Empty<Renderer>());
		}
		if (partsRoot == null)
		{
			partsRoot = base.transform.Find(RootName);
		}
		if (organsRoot == null)
		{
			organsRoot = base.transform.Find(OrgansName);
		}
		DestroyRoot(ref partsRoot);
		DestroyRoot(ref organsRoot);
		parts.Clear();
		organs.Clear();
		organVisuals.Clear();
		ReleaseMaterials();
		Renderer renderer = GetComponent<Renderer>();
		if (renderer != null)
		{
			renderer.enabled = true;
		}
	}

	private void DestroyRoot(ref Transform t)
	{
		if (t != null)
		{
			t.gameObject.SetActive(false);
			if (Application.isPlaying)
			{
				Object.Destroy(t.gameObject);
			}
			else
			{
				Object.DestroyImmediate(t.gameObject);
			}
			t = null;
		}
	}

	private void Build(ArmorPlate plate)
	{
		Clear();
		GameObject gameObject = CrewVisualAsset.LoadRequired();
		partsRoot = MakeRoot(RootName);
		organsRoot = MakeRoot(OrgansName);
		GameObject gameObject2 = Object.Instantiate(gameObject, partsRoot);
		gameObject2.name = "SeatedCrew";
		gameObject2.transform.localScale = CrewVisualAsset.MeterToFigureScale;
		organsRoot.localScale = CrewVisualAsset.MeterToFigureScale;
		Transform transform = gameObject2.transform.Find("Shell");
		Transform transform2 = gameObject2.transform.Find("Organs");
		Transform transform3 = gameObject2.transform.Find("Details");
		transform2.SetParent(organsRoot, false);
		transform3.SetParent(organsRoot, false);
		Renderer[] array = transform.GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < array.Length; i++)
		{
			PrepareMaterials(array[i], new Color(0.72f, 0.73f, 0.75f, 1f));
			parts.Add(array[i]);
		}
		CrewImpactHoles crewImpactHoles = plate.GetComponent<CrewImpactHoles>();
		if (crewImpactHoles == null)
		{
			crewImpactHoles = plate.gameObject.AddComponent<CrewImpactHoles>();
		}
		crewImpactHoles.Bind(parts.ToArray());
		Renderer[] array2 = transform2.GetComponentsInChildren<Renderer>(true);
		for (int j = 0; j < array2.Length; j++)
		{
			Renderer renderer = array2[j];
			int num = CrewVisualAsset.OrganIndex(renderer.transform, transform2);
			Color[] array3 = PrepareMaterials(renderer, null);
			organs.Add(renderer);
			OrganVisual item = default(OrganVisual);
			item.renderer = renderer;
			item.organ = CrewAnatomy.Organs[num].organ;
			item.baseColors = array3;
			organVisuals.Add(item);
		}
		Renderer[] array4 = transform3.GetComponentsInChildren<Renderer>(true);
		for (int k = 0; k < array4.Length; k++)
		{
			PrepareMaterials(array4[k], null);
			organs.Add(array4[k]);
		}
		Renderer renderer2 = plate.GetComponent<Renderer>();
		if (renderer2 != null)
		{
			renderer2.enabled = false;
		}
		Subscribe();
		SyncVisibility(true);
		RefreshOrganTint(plate);
		Vector3 vector = PartsHalfExtent();
		Debug.Log(string.Format("Экипаж [{0}]: Blender-манекен собран ({1} наружных и {2} внутренних мешей), ", plate.name, parts.Count, organs.Count) + string.Format("габарит {0:F2}×{1:F2}×{2:F2} м, ", plate.widthM, plate.heightM, plate.thicknessMM / 1000f) + string.Format("огибающая расчётных объёмов ±({0:F3}, {1:F3}, {2:F3}) доли габарита. ", vector.x, vector.y, vector.z) + "⚠ Детальная геометрия — визуальное представление (ASSUMED). Вход в тело определяется оболочкой, попадания в органы — упрощёнными объёмами. Рёбра и сосуды отдельно не рассчитываются. Красный цвет обозначает задетый орган, а не тяжесть травмы.");
	}

	private Transform MakeRoot(string n)
	{
		GameObject gameObject = new GameObject(n);
		Transform transform = gameObject.transform;
		transform.SetParent(base.transform, false);
		transform.localPosition = Vector3.zero;
		transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
		transform.localScale = Vector3.one;
		return transform;
	}

	private Color[] PrepareMaterials(Renderer renderer, Color? tint)
	{
		Material[] sharedMaterials = renderer.sharedMaterials;
		Color[] array = new Color[sharedMaterials.Length];
		for (int i = 0; i < sharedMaterials.Length; i++)
		{
			array[i] = (tint ?? sharedMaterials[i].color);
			Material material = new Material(MaterialLibrary.Armor(ArmorPlate.ArmorType.RHA));
			material.name = "Crew_" + sharedMaterials[i].name;
			material.color = array[i];
			if (material.HasProperty("_Metallic"))
			{
				material.SetFloat("_Metallic", 0f);
			}
			if (material.HasProperty("_Glossiness"))
			{
				material.SetFloat("_Glossiness", 0.25f);
			}
			if (material.HasProperty("_MainTex"))
			{
				material.SetTexture("_MainTex", null);
			}
			if (material.HasProperty("_TexInfluence"))
			{
				material.SetFloat("_TexInfluence", 0f);
			}
			sharedMaterials[i] = material;
			ownedMaterials.Add(material);
		}
		renderer.sharedMaterials = sharedMaterials;
		renderer.gameObject.layer = base.gameObject.layer;
		return array;
	}

	private static void SetTint(Renderer renderer, Color color)
	{
		Material[] sharedMaterials = renderer.sharedMaterials;
		for (int i = 0; i < sharedMaterials.Length; i++)
		{
			sharedMaterials[i].color = color;
		}
	}

	private void ReleaseMaterials()
	{
		foreach (Material item in ownedMaterials)
		{
			if (item != null)
			{
				if (Application.isPlaying)
				{
					Object.Destroy(item);
				}
				else
				{
					Object.DestroyImmediate(item);
				}
			}
		}
		ownedMaterials.Clear();
	}

	private void Subscribe()
	{
		if (!subscribed)
		{
			GameSettings.OnDisplayChanged += OnDisplayChanged;
			subscribed = true;
		}
	}

	private void Unsubscribe()
	{
		if (subscribed)
		{
			GameSettings.OnDisplayChanged -= OnDisplayChanged;
			subscribed = false;
		}
	}

	private void OnEnable()
	{
		Subscribe();
	}

	private void OnDisable()
	{
		Unsubscribe();
	}

	private void OnDestroy()
	{
		Unsubscribe();
		ReleaseMaterials();
	}

	private void OnDisplayChanged()
	{
		if (this == null)
		{
			Unsubscribe();
			return;
		}
		SyncVisibility(false);
		RefreshColor(GetComponent<ArmorPlate>());
	}

	private void Update()
	{
		if (!NumberField.TypingNow && Input.GetKeyDown(OrgansToggleKey) && lastToggleFrame != Time.frameCount)
		{
			lastToggleFrame = Time.frameCount;
			GameSettings.SetCrewOrgans(!GameSettings.CrewOrgans);
			Debug.Log((!GameSettings.CrewOrgans) ? "[Экипаж] Показ органов ВЫКЛ." : "[Экипаж] Показ органов ВКЛ. ⚠ Детальная геометрия — визуальное представление (ASSUMED). Вход в тело определяется оболочкой, попадания в органы — упрощёнными объёмами. Рёбра и сосуды отдельно не рассчитываются. Красный цвет обозначает задетый орган, а не тяжесть травмы.");
		}
		SyncVisibility(false);
	}

	private void SyncVisibility(bool force)
	{
		bool shellShown = ShellShown;
		bool organsShown = OrgansShown;
		if (!force && shellShown == lastShellShown && organsShown == lastOrgansShown)
		{
			return;
		}
		lastShellShown = shellShown;
		lastOrgansShown = organsShown;
		for (int i = 0; i < parts.Count; i++)
		{
			if (parts[i] != null)
			{
				parts[i].enabled = shellShown;
			}
		}
		for (int j = 0; j < organs.Count; j++)
		{
			if (organs[j] != null)
			{
				organs[j].enabled = organsShown;
			}
		}
	}

	private static Color BodyColor(ArmorPlate plate)
	{
		return new Color(0.72f, 0.73f, 0.75f, 1f);
	}

	public static void RefreshColor(ArmorPlate plate)
	{
		if (plate == null)
		{
			return;
		}
		CrewFigure crewFigure = plate.GetComponent<CrewFigure>();
		if (crewFigure == null)
		{
			return;
		}
		Color color = BodyColor(plate);
		foreach (Renderer renderer in crewFigure.parts)
		{
			if (renderer != null)
			{
				Material[] sharedMaterials = renderer.sharedMaterials;
				for (int i = 0; i < sharedMaterials.Length; i++)
				{
					sharedMaterials[i].color = color;
				}
			}
		}
		crewFigure.RefreshOrganTint(plate);
	}

	private void RefreshOrganTint(ArmorPlate plate)
	{
		if (plate == null)
		{
			return;
		}
		foreach (OrganVisual organVisual in organVisuals)
		{
			if (organVisual.renderer == null)
			{
				continue;
			}
			bool flag = GameSettings.CrewDamageHighlight && plate.CrewOrganWasHit(organVisual.organ);
			Material[] sharedMaterials = organVisual.renderer.sharedMaterials;
			for (int i = 0; i < sharedMaterials.Length; i++)
			{
				sharedMaterials[i].color = (flag ? OrganHitColor : organVisual.baseColors[i]);
			}
		}
	}
}
