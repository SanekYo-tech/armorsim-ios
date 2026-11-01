using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class RadarCard
{
	public static RadarCardShape Describe(RadarFacts facts)
	{
		RadarCardShape result = default(RadarCardShape);
		switch (RadarRoute.State(facts))
		{
		case RadarAccessState.Open:
			result.enterable = true;
			return result;
		case RadarAccessState.GateClosed:
			result.reasonKey = facts.gateReason ?? "radar.gate.closed";
			return result;
		case RadarAccessState.Unverified:
			result.badgeKey = "dlc.badge";
			result.reasonKey = "dlc.unverified";
			result.storeButton = true;
			return result;
		case RadarAccessState.NotOwned:
			result.badgeKey = "dlc.badge";
			result.reasonKey = "dlc.not_owned_hint";
			result.demoButton = facts.demoAvailable;
			result.storeButton = true;
			return result;
		case RadarAccessState.NotImplemented:
			result.badgeKey = "mode.soon";
			return result;
		default:
			Debug.LogError("[RadarCard] Unknown access state; card refused.");
			result.badgeKey = "dlc.badge";
			result.reasonKey = "dlc.unverified";
			return result;
		}
	}

	public static Button Build(Transform parent, RadarFacts facts, UnityAction select, UnityAction store, UnityAction demo)
	{
		RadarCardShape shape = Describe(facts);
		RectTransform card = MenuTheme.MakeRect(parent, "Mode_Radar");
		Image bg = card.gameObject.AddComponent<Image>();
		bg.color = MenuTheme.SurfaceAlt;
		bg.raycastTarget = true;
		Button button = card.gameObject.AddComponent<Button>();
		button.targetGraphic = bg;
		MenuTheme.ApplyColors(button);
		button.interactable = shape.enterable;
		button.onClick.AddListener(select);
		LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = 104f;
		layout.preferredHeight = 104f;
		RectTransform stripe = MenuTheme.MakeRect(card, "Accent");
		stripe.anchorMin = new Vector2(0f, 0f);
		stripe.anchorMax = new Vector2(0f, 1f);
		stripe.pivot = new Vector2(0f, 0.5f);
		stripe.offsetMin = Vector2.zero;
		stripe.offsetMax = new Vector2(4f, 0f);
		MenuTheme.AddImage(stripe, MenuTheme.Line);
		TextMeshProUGUI title = MenuTheme.MakeLabel(card, "Title", "radar.title", 24f, (FontStyles)1, shape.enterable ? MenuTheme.TextPrimary : MenuTheme.TextLocked, (TextAlignmentOptions)257);
		MenuTheme.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -18f), new Vector2(700f, 30f));
		title.characterSpacing = 4f;
		TextMeshProUGUI desc = MenuTheme.MakeLabel(card, "Desc", "radar.desc", 16f, (FontStyles)0, shape.enterable ? MenuTheme.TextMuted : MenuTheme.TextLocked, (TextAlignmentOptions)257);
		MenuTheme.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -54f), new Vector2(1000f, 44f));
		if (shape.badgeKey != null)
		{
			TextMeshProUGUI badge = MenuTheme.MakeLabel(card, "Badge", shape.badgeKey, 15f, (FontStyles)1, MenuTheme.TextMuted, (TextAlignmentOptions)516);
			MenuTheme.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(200f, 24f));
			badge.characterSpacing = 6f;
		}
		if (facts.freeAccess && shape.enterable)
		{
			TextMeshProUGUI free = MenuTheme.MakeLabel(card, "FreeAccess", "radar.free", 15f, (FontStyles)1, MenuTheme.Promo, (TextAlignmentOptions)516);
			MenuTheme.Place(free.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(640f, 24f));
			free.characterSpacing = 3f;
		}
		if (shape.reasonKey != null)
		{
			TextMeshProUGUI reason = MenuTheme.MakeLabel(card, "Reason", shape.reasonKey, 14f, (FontStyles)0, MenuTheme.TextLocked, (TextAlignmentOptions)257);
			MenuTheme.Place(reason.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(26f, 14f), new Vector2(1000f, 20f));
		}
		if (shape.storeButton)
		{
			AddButton(card, "Radar_Store", "dlc.open_store", -236f, store);
		}
		if (shape.demoButton)
		{
			AddButton(card, "Radar_Demo", "dlc.show_demo", -236f, demo);
		}
		return button;
	}

	private static void AddButton(RectTransform card, string name, string key, float right, UnityAction action)
	{
		Button b = MenuTheme.MakeButton(card, name, key, 40f, action, 15f, (TextAlignmentOptions)514, 8f);
		MenuTheme.Place((RectTransform)b.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(right, -16f), new Vector2(200f, 40f));
	}
}
