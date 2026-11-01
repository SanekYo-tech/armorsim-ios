using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class AeroCard
{
	public static AeroCardShape Describe(AeroFacts facts)
	{
		AeroCardShape result = default(AeroCardShape);
		switch (AeroRoute.State(facts))
		{
		case AeroAccessState.Open:
			result.enterable = true;
			return result;
		case AeroAccessState.NotImplemented:
			result.badgeKey = "mode.soon";
			return result;
		case AeroAccessState.NotOwned:
			result.badgeKey = "dlc.badge";
			result.reasonKey = "dlc.not_owned_hint";
			result.storeButton = true;
			result.demoButton = facts.demoAvailable;
			return result;
		case AeroAccessState.Unverified:
			result.badgeKey = "dlc.badge";
			result.reasonKey = "dlc.unverified";
			result.storeButton = true;
			return result;
		case AeroAccessState.GateClosed:
			result.reasonKey = (string.IsNullOrEmpty(facts.gateReason) ? "aero.gate.closed" : facts.gateReason);
			return result;
		default:
			Debug.LogError("[AeroCard] Unknown access state; card refused.");
			result.badgeKey = "dlc.badge";
			result.reasonKey = "dlc.unverified";
			return result;
		}
	}

	public static Button Build(Transform parent, AeroFacts facts, UnityAction select, UnityAction store, UnityAction demo)
	{
		AeroCardShape shape = Describe(facts);
		RectTransform card = MenuTheme.MakeRect(parent, "Mode_Aero");
		Image image = card.gameObject.AddComponent<Image>();
		image.color = MenuTheme.SurfaceAlt;
		image.raycastTarget = true;
		Button button = card.gameObject.AddComponent<Button>();
		button.targetGraphic = image;
		MenuTheme.ApplyColors(button);
		button.interactable = shape.enterable;
		button.onClick.AddListener(select);
		LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = 104f;
		layout.preferredHeight = 104f;
		RectTransform accent = MenuTheme.MakeRect(card, "Accent");
		accent.anchorMin = new Vector2(0f, 0f);
		accent.anchorMax = new Vector2(0f, 1f);
		accent.pivot = new Vector2(0f, 0.5f);
		accent.offsetMin = Vector2.zero;
		accent.offsetMax = new Vector2(4f, 0f);
		MenuTheme.AddImage(accent, MenuTheme.Line);
		TextMeshProUGUI title = MenuTheme.MakeLabel(card, "Title", "aero.title", 24f, FontStyles.Bold, shape.enterable ? MenuTheme.TextPrimary : MenuTheme.TextLocked, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -18f), new Vector2(700f, 30f));
		title.characterSpacing = 4f;
		TextMeshProUGUI desc = MenuTheme.MakeLabel(card, "Desc", "aero.desc", 16f, FontStyles.Normal, shape.enterable ? MenuTheme.TextMuted : MenuTheme.TextLocked, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -54f), new Vector2(1000f, 44f));
		if (shape.badgeKey != null)
		{
			TextMeshProUGUI badge = MenuTheme.MakeLabel(card, "Badge", shape.badgeKey, 15f, FontStyles.Bold, MenuTheme.TextMuted, TextAlignmentOptions.Right);
			MenuTheme.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(200f, 24f));
			badge.characterSpacing = 6f;
		}
		if (facts.freeAccess && shape.enterable)
		{
			TextMeshProUGUI freeUntil = MenuTheme.MakeLabel(card, "FreeUntil", "aero.free_until", 15f, FontStyles.Bold, MenuTheme.Promo, TextAlignmentOptions.Right);
			MenuTheme.Place(freeUntil.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(640f, 24f));
			freeUntil.characterSpacing = 3f;
		}
		if (shape.reasonKey != null)
		{
			TextMeshProUGUI reason = MenuTheme.MakeLabel(card, "Reason", shape.reasonKey, 14f, FontStyles.Normal, MenuTheme.TextLocked, TextAlignmentOptions.TopLeft);
			MenuTheme.Place(reason.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(26f, 14f), new Vector2(1000f, 20f));
		}
		if (shape.storeButton)
		{
			AddButton(card, "Aero_Store", "dlc.open_store", -24f, store);
		}
		if (shape.demoButton)
		{
			AddButton(card, "Aero_Demo", "dlc.show_demo", -236f, demo);
		}
		return button;
	}

	private static void AddButton(RectTransform card, string name, string key, float right, UnityAction action)
	{
		Button button = MenuTheme.MakeButton(card, name, key, 40f, action, 15f, TextAlignmentOptions.Center, 8f);
		MenuTheme.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(right, -16f), new Vector2(200f, 40f));
	}
}
