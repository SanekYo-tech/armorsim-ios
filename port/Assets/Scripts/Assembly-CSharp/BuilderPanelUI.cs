using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BuilderPanelUI : MonoBehaviour
{
	public ArmorBuilder builder;

	public PlateSelector selector;

	private void Start()
	{
		if (builder == null)
		{
			builder = Object.FindFirstObjectByType<ArmorBuilder>();
		}
		if (selector == null)
		{
			selector = Object.FindFirstObjectByType<PlateSelector>();
		}
		if (builder == null)
		{
			Debug.LogWarning("BuilderPanelUI: не найден ArmorBuilder!");
			return;
		}
		Canvas canvas = Object.FindFirstObjectByType<Canvas>();
		if (canvas == null)
		{
			return;
		}
		GameObject panel = new GameObject("BuilderPanel", typeof(RectTransform));
		panel.transform.SetParent(canvas.transform, false);
		RectTransform rect = panel.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.sizeDelta = new Vector2(700f, 40f);
		rect.anchoredPosition = new Vector2(0f, -100f);
		MakeButton(panel, "sandbox.add_plate", new Vector2(-210f, 0f), new Vector2(130f, 32f), new Color(0.2f, 0.45f, 0.25f, 1f), builder.AddPlate);
		MakeButton(panel, "sandbox.delete_selected", new Vector2(-70f, 0f), new Vector2(130f, 32f), MenuTheme.DeleteButton, DeleteSelected);
		MakeButton(panel, "sandbox.reset_damage", new Vector2(70f, 0f), new Vector2(130f, 32f), new Color(0.24f, 0.36f, 0.46f, 1f), ResetDamage);
		MakeButton(panel, "sandbox.clear_all", new Vector2(210f, 0f), new Vector2(130f, 32f), new Color(0.5f, 0.25f, 0.25f, 1f), builder.ClearAll);
	}

	private void ResetDamage()
	{
		Projectile projectile = Object.FindFirstObjectByType<Projectile>();
		if (projectile == null)
		{
			Debug.Log("В сцене нет снаряда — след выстрела убирать нечем.");
			return;
		}
		projectile.ClearScene();
	}

	private void DeleteSelected()
	{
		if (selector == null || builder == null)
		{
			return;
		}
		ArmorPlate selectedPlate = selector.SelectedPlate;
		if (selectedPlate == null)
		{
			Debug.Log("Нет выбранной пластины для удаления.");
			return;
		}
		builder.RemovePlate(selectedPlate.gameObject);
	}

	private void MakeButton(GameObject parent, string key, Vector2 pos, Vector2 size, Color color, UnityAction onClick)
	{
		GameObject buttonObject = new GameObject("Btn_" + key, typeof(Image), typeof(Button));
		buttonObject.transform.SetParent(parent.transform, false);
		buttonObject.GetComponent<Image>().color = color;
		buttonObject.GetComponent<Button>().onClick.AddListener(onClick);
		RectTransform rect = buttonObject.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.sizeDelta = size;
		rect.anchoredPosition = pos;
		GameObject textObject = new GameObject("Text", typeof(TextMeshProUGUI));
		textObject.transform.SetParent(buttonObject.transform, false);
		TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
		text.fontSize = 14f;
		text.alignment = TextAlignmentOptions.Center;
		text.color = Color.white;
		text.enableAutoSizing = true;
		text.fontSizeMin = 9f;
		text.fontSizeMax = 14f;
		LocalizedLabel localized = textObject.AddComponent<LocalizedLabel>();
		localized.key = key;
		localized.Apply();
		RectTransform textRect = textObject.GetComponent<RectTransform>();
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.sizeDelta = Vector2.zero;
		textRect.anchoredPosition = Vector2.zero;
	}
}
