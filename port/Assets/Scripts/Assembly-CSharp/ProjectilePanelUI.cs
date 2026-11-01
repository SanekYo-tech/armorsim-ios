using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ProjectilePanelUI : MonoBehaviour
{
	public Projectile projectile;

	private Canvas canvas;

	private GameObject panelGO;

	private TMP_Text massLabel;

	private TMP_Text tooltipLabel;

	private CatalogGrid projectileGrid;

	private CatalogGrid coreGrid;

	private string selectedId;

	private const float PanelWidth = 220f;

	private const float SidePad = 15f;

	private TMP_Text qualityLabel;

	private void Start()
	{
		if (projectile == null)
		{
			projectile = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		}
		if (projectile == null)
		{
			Debug.LogWarning("ProjectilePanelUI: не найден Projectile в сцене!");
			return;
		}
		Catalog.AssertComplete();
		selectedId = Catalog.ProjectileIdFor(projectile.GetDefinition());
		BuildPanel();
		Localization.OnLanguageChanged += BuildPanel;
	}

	private void OnDestroy()
	{
		Localization.OnLanguageChanged -= BuildPanel;
	}

	private void BuildPanel()
	{
		canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
		if (canvas == null)
		{
			GameObject gameObject = new GameObject("Canvas_Auto");
			canvas = gameObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			gameObject.AddComponent<CanvasScaler>();
			gameObject.AddComponent<GraphicRaycaster>();
		}
		if (panelGO != null)
		{
			SidePanelScroll.DestroyPanel(panelGO);
		}
		panelGO = MakePanel("ProjectilePanel", new Vector2(20f, 20f), new Vector2(PanelWidth, 500f), new Vector2(0f, 0f));
		MakeLabel(panelGO, Localization.T("sandbox.projectile"), new Vector2(0f, -15f), 16f, TextAlignmentOptions.Center, 200f, 24f);
		projectileGrid = CatalogGrid.Build(panelGO, Catalog.ProjectileMeta(), -41f, PanelWidth, SidePad, PickProjectile, SetTooltip);
		float num = projectileGrid.EndY - 4f;
		MakeLabel(panelGO, Localization.T("sandbox.core"), new Vector2(0f, num), 13f, TextAlignmentOptions.Center, 200f, 18f);
		coreGrid = CatalogGrid.Build(panelGO, Catalog.CoreMeta(), num - 20f, PanelWidth, SidePad, PickCore, SetTooltip);
		float num2 = coreGrid.EndY - 2f;
		tooltipLabel = MakeLabel(panelGO, "", new Vector2(0f, num2), 10f, TextAlignmentOptions.TopLeft, 190f, 118f);
		tooltipLabel.color = new Color(0.75f, 0.78f, 0.82f, 1f);
		tooltipLabel.textWrappingMode = TextWrappingModes.Normal;
		float y = num2 - 124f;
		coreGrid.SetSelected(Catalog.CoreIdFor(projectile.coreType));
		projectileGrid.SetSelected(selectedId);
		MakeSliderRow(panelGO, "sandbox.caliber", ref y, 5f, 140f, projectile.diameter, "unit.mm", delegate(float v)
		{
			projectile.diameter = v;
			UpdateMassLabel();
		}, 1f, 500f);
		MakeSliderRow(panelGO, "sandbox.velocity", ref y, 200f, 2000f, projectile.combatVelocity, "unit.mps", delegate(float v)
		{
			projectile.combatVelocity = v;
		}, 50f, 4000f);
		MakeSliderRow(panelGO, "sandbox.rod_length", ref y, 50f, 600f, projectile.rodLengthMM, "unit.mm", delegate(float v)
		{
			projectile.rodLengthMM = v;
			UpdateMassLabel();
		}, 5f, 3000f);
		PenetrationPlayer player = PenetrationPlayer.FindOrCreate();
		MakeSliderRow(panelGO, "sandbox.playback", ref y, 1f, 60f, player.playbackDuration, "unit.sec", delegate(float v)
		{
			player.playbackDuration = v;
		}, 0.2f, 600f);
		float num3 = y - 6f;
		qualityLabel = MakeLabel(panelGO, QualityText(), new Vector2(0f, num3), 12f, TextAlignmentOptions.Center, 200f, 24f);
		qualityLabel.color = new Color(0.62f, 0.66f, 0.72f, 1f);
		massLabel = MakeLabel(panelGO, "", new Vector2(0f, num3 - 24f), 12f, TextAlignmentOptions.Left, 200f, 24f);
		UpdateMassLabel();
		RectTransform component = panelGO.GetComponent<RectTransform>();
		component.sizeDelta = new Vector2(PanelWidth, SidePad - (num3 - 44f));
		SidePanelScroll.Fit(component, 20f);
	}

	private void PickProjectile(string id)
	{
		ProjectileDefinition projectileDefinition = Catalog.MakeProjectile(id);
		if (projectileDefinition == null)
		{
			Debug.LogError("ProjectilePanelUI: в реестре нет снаряда с ключом '" + id + "'");
			return;
		}
		selectedId = id;
		projectile.ApplyDefinition(projectileDefinition);
		BuildPanel();
		UpdateMassLabel();
	}

	private void PickCore(string id)
	{
		if (!Catalog.TryCoreType(id, out Projectile.CoreType type))
		{
			Debug.LogError("ProjectilePanelUI: в реестре нет сердечника с ключом '" + id + "'");
			return;
		}
		projectile.coreType = type;
		projectile.isLongRod = type == Projectile.CoreType.DU || type == Projectile.CoreType.WHA;
		coreGrid.SetSelected(id);
		UpdateMassLabel();
	}

	private void SetTooltip(string text)
	{
		if (tooltipLabel != null)
		{
			tooltipLabel.text = text;
		}
	}

	private string QualityText()
	{
		return Localization.T("sandbox.quality", new object[1] { Localization.T((projectile.quality == Projectile.QualityMode.Продвинутое) ? "mode.advanced" : "mode.normal") });
	}

	private void UpdateMassLabel()
	{
		if (massLabel == null)
		{
			return;
		}
		massLabel.text = Localization.T("sandbox.mass_g", new object[1] { string.Format("{0:F0}", projectile.ComputeMassKg() * 1000f) });
	}

	private GameObject MakePanel(string name, Vector2 anchoredPos, Vector2 size, Vector2 anchor)
	{
		GameObject gameObject = new GameObject(name, typeof(Image));
		gameObject.transform.SetParent(canvas.transform, false);
		gameObject.GetComponent<Image>().color = MenuTheme.PanelBackground;
		RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
		rectTransform.anchorMin = anchor;
		rectTransform.anchorMax = anchor;
		rectTransform.pivot = anchor;
		rectTransform.sizeDelta = size;
		rectTransform.anchoredPosition = anchoredPos;
		return gameObject;
	}

	private TMP_Text MakeLabel(GameObject parent, string text, Vector2 pos, float size, TextAlignmentOptions align, float width, float height = 24f)
	{
		GameObject gameObject = new GameObject("Label", typeof(TextMeshProUGUI));
		gameObject.transform.SetParent(parent.transform, false);
		TMP_Text tMP_Text = gameObject.GetComponent<TextMeshProUGUI>();
		tMP_Text.text = text;
		tMP_Text.fontSize = size;
		tMP_Text.alignment = align;
		tMP_Text.color = Color.white;
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 1f);
		component.anchorMax = new Vector2(0.5f, 1f);
		component.pivot = new Vector2(0.5f, 1f);
		component.sizeDelta = new Vector2(width, height);
		component.anchoredPosition = pos;
		return tMP_Text;
	}

	private void MakeButton(GameObject parent, string text, Vector2 pos, Vector2 size, UnityAction onClick)
	{
		GameObject gameObject = new GameObject("Button", typeof(Image), typeof(Button));
		gameObject.transform.SetParent(parent.transform, false);
		gameObject.GetComponent<Image>().color = CatalogGrid.ButtonColor;
		gameObject.GetComponent<Button>().onClick.AddListener(onClick);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 1f);
		component.anchorMax = new Vector2(0.5f, 1f);
		component.pivot = new Vector2(0.5f, 1f);
		component.sizeDelta = size;
		component.anchoredPosition = pos;
		TMP_Text tMP_Text = MakeLabel(gameObject, text, Vector2.zero, 13f, TextAlignmentOptions.Center, size.x, 24f);
		StretchFull(tMP_Text.GetComponent<RectTransform>());
	}

	private void MakeSliderRow(GameObject parent, string titleKey, ref float y, float min, float max, float value, string unitKey, Action<float> onChange, float typedMin = 0f / 0f, float typedMax = 0f / 0f)
	{
		float shown = value;
		string title = Localization.T(titleKey);
		string unit = Localization.T(unitKey);
		string text = string.Format("{0}: {1:F0} {2}", title, shown, unit);
		TMP_Text rowLabel = MakeLabel(parent, text, new Vector2(0f, y), 12f, TextAlignmentOptions.Left, 180f, 24f);
		y -= 20f;
		GameObject gameObject = new GameObject("Slider", typeof(Slider));
		gameObject.transform.SetParent(parent.transform, false);
		Slider slider = gameObject.GetComponent<Slider>();
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 1f);
		component.anchorMax = new Vector2(0.5f, 1f);
		component.pivot = new Vector2(0.5f, 1f);
		component.sizeDelta = new Vector2(180f, 18f);
		component.anchoredPosition = new Vector2(0f, y);
		GameObject gameObject2 = new GameObject("BG", typeof(Image));
		gameObject2.transform.SetParent(gameObject.transform, false);
		gameObject2.GetComponent<Image>().color = MenuTheme.SliderTrack;
		StretchFull(gameObject2.GetComponent<RectTransform>());
		GameObject gameObject3 = new GameObject("Fill Area", typeof(RectTransform));
		gameObject3.transform.SetParent(gameObject.transform, false);
		StretchFull(gameObject3.GetComponent<RectTransform>());
		GameObject gameObject4 = new GameObject("Fill", typeof(Image));
		gameObject4.transform.SetParent(gameObject3.transform, false);
		gameObject4.GetComponent<Image>().color = MenuTheme.SliderFill;
		StretchFull(gameObject4.GetComponent<RectTransform>());
		GameObject gameObject5 = new GameObject("Handle Slide Area", typeof(RectTransform));
		gameObject5.transform.SetParent(gameObject.transform, false);
		StretchFull(gameObject5.GetComponent<RectTransform>());
		GameObject gameObject6 = new GameObject("Handle", typeof(Image));
		gameObject6.transform.SetParent(gameObject5.transform, false);
		gameObject6.GetComponent<Image>().color = Color.white;
		gameObject6.GetComponent<RectTransform>().sizeDelta = new Vector2(10f, 18f);
		slider.fillRect = gameObject4.GetComponent<RectTransform>();
		slider.handleRect = gameObject6.GetComponent<RectTransform>();
		slider.targetGraphic = gameObject6.GetComponent<Image>();
		slider.minValue = min;
		slider.maxValue = max;
		slider.value = shown;
		slider.onValueChanged.AddListener(delegate(float v)
		{
			shown = v;
			onChange(v);
			rowLabel.text = string.Format("{0}: {1:F0} {2}", title, v, unit);
		});
		NumberField.Attach(rowLabel, () => shown, delegate(float v)
		{
			float num = Mathf.Clamp(v, float.IsNaN(typedMin) ? min : typedMin, float.IsNaN(typedMax) ? max : typedMax);
			shown = num;
			onChange(num);
			slider.value = Mathf.Clamp(num, min, max);
			rowLabel.text = string.Format("{0}: {1:F0} {2}", title, num, unit);
		}, "F0");
		y -= 30f;
	}

	private void StretchFull(RectTransform rt)
	{
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.sizeDelta = Vector2.zero;
		rt.anchoredPosition = Vector2.zero;
	}
}
