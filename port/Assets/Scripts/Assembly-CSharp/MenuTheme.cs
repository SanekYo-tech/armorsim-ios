using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuTheme
{
	public enum Skin
	{
		Classic = 0,
		SuperBench = 1
	}

	private static readonly Color cBackground = Hex("191919");

	private static readonly Color cSurface = Hex("202020");

	private static readonly Color cSurfaceAlt = Hex("161616");

	private static readonly Color cHover = Hex("2C2C2C");

	private static readonly Color cPressed = Hex("383838");

	private static readonly Color cLine = Hex("3A3A3A");

	private static readonly Color cTextPrimary = Hex("F2F2F2");

	private static readonly Color cTextMuted = Hex("8C8C8C");

	private static readonly Color cTextLocked = Hex("565656");

	private static readonly Color cSelected = Hex("343434");

	private static readonly Color cAccent = new Color(0.3f, 0.6f, 1f, 1f);

	private static readonly Color bBackground = Hex("0E1626");

	private static readonly Color bSurface = Hex("152036");

	private static readonly Color bSurfaceAlt = Hex("101A2C");

	private static readonly Color bHover = Hex("1E2C48");

	private static readonly Color bPressed = Hex("27395C");

	private static readonly Color bLine = Hex("2C3E5E");

	private static readonly Color bTextPrimary = Hex("EAF0FA");

	private static readonly Color bTextMuted = Hex("8FA0BC");

	private static readonly Color bTextLocked = Hex("55668A");

	private static readonly Color bSelected = Hex("24365A");

	private static readonly Color bAccent = Hex("4C8BE0");

	public static readonly Color Refused = Hex("F08A2B");

	public static readonly Color RefusedText = Hex("1B1200");

	public static readonly Color Failure = Hex("E0554C");

	public static readonly Color Promo = Hex("F08A2B");

	public static TMP_FontAsset Font;

	public const float ScrollbarWidth = 10f;

	public static bool SuperBenchActive
	{
		get
		{
			if (AppFlow.ActiveMode != SimMode.Super)
			{
				return false;
			}
			string scene = SceneManager.GetActiveScene().name ?? "";
			string sandbox = AppFlow.SandboxSceneName ?? "";
			return scene == sandbox || scene == "SuperViewer";
		}
	}

	public static Skin Current => SuperBenchActive ? Skin.SuperBench : Skin.Classic;

	public static Color Background => Pick(cBackground, bBackground);

	public static Color Surface => Pick(cSurface, bSurface);

	public static Color SurfaceAlt => Pick(cSurfaceAlt, bSurfaceAlt);

	public static Color Hover => Pick(cHover, bHover);

	public static Color Pressed => Pick(cPressed, bPressed);

	public static Color Line => Pick(cLine, bLine);

	public static Color TextPrimary => Pick(cTextPrimary, bTextPrimary);

	public static Color TextMuted => Pick(cTextMuted, bTextMuted);

	public static Color TextLocked => Pick(cTextLocked, bTextLocked);

	public static Color Selected => Pick(cSelected, bSelected);

	public static Color Accent => Pick(cAccent, bAccent);

	public static Color PanelBackground
	{
		get
		{
			if (!SuperBenchActive)
			{
				return new Color(0.1f, 0.1f, 0.12f, 0.85f);
			}
			return new Color(bSurface.r, bSurface.g, bSurface.b, 0.92f);
		}
	}

	public static Color SliderTrack => SuperBenchActive ? bLine : new Color(0.3f, 0.3f, 0.3f, 1f);

	public static Color SliderFill => SuperBenchActive ? bAccent : new Color(0.4f, 0.6f, 1f, 1f);

	public static Color FieldHover => SuperBenchActive ? bAccent : new Color(1f, 0.85f, 0.35f, 1f);

	public static Color GizmoHot => SuperBenchActive ? bTextPrimary : new Color(1f, 0.84f, 0.25f, 1f);

	public static Color DeleteButton => SuperBenchActive ? bPressed : new Color(0.5f, 0.4f, 0.2f, 1f);

	private static Color Pick(Color classic, Color bench)
	{
		return SuperBenchActive ? bench : classic;
	}

	public static Color Hex(string hex)
	{
		if (!ColorUtility.TryParseHtmlString("#" + hex, out Color color))
		{
			return Color.magenta;
		}
		return color;
	}

	public static RectTransform MakeRect(Transform parent, string name)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, worldPositionStays: false);
		return go.transform as RectTransform;
	}

	public static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
	{
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = offsetMin;
		rt.offsetMax = offsetMax;
	}

	public static void Stretch(RectTransform rt)
	{
		Stretch(rt, Vector2.zero, Vector2.zero);
	}

	public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
	{
		rt.anchorMin = anchor;
		rt.anchorMax = anchor;
		rt.pivot = pivot;
		rt.anchoredPosition = pos;
		rt.sizeDelta = size;
	}

	public static Image AddImage(RectTransform rt, Color color)
	{
		Image image = rt.gameObject.AddComponent<Image>();
		image.color = color;
		image.raycastTarget = false;
		return image;
	}

	public static TextMeshProUGUI MakeLabelRaw(Transform parent, string name, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
	{
		RectTransform rect = MakeRect(parent, name);
		TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
		if (Font != null)
		{
			label.font = Font;
		}
		label.text = text;
		label.fontSize = size;
		label.fontStyle = style;
		label.color = color;
		label.alignment = align;
		label.raycastTarget = false;
		label.overflowMode = TextOverflowModes.Overflow;
		return label;
	}

	public static TextMeshProUGUI MakeLabel(Transform parent, string name, string key, float size, FontStyles style, Color color, TextAlignmentOptions align)
	{
		TextMeshProUGUI label = MakeLabelRaw(parent, name, "", size, style, color, align);
		LocalizedLabel localized = label.gameObject.AddComponent<LocalizedLabel>();
		localized.key = key;
		localized.Apply();
		return label;
	}

	public static void ApplyColors(Button btn)
	{
		ColorBlock colors = btn.colors;
		btn.colors = new ColorBlock
		{
			normalColor = Color.white,
			highlightedColor = new Color(1.35f, 1.35f, 1.35f, 1f),
			pressedColor = new Color(1.7f, 1.7f, 1.7f, 1f),
			selectedColor = Color.white,
			disabledColor = new Color(0.7f, 0.7f, 0.7f, 1f),
			colorMultiplier = colors.colorMultiplier,
			fadeDuration = 0.08f
		};
	}

	private static Button BuildButton(Transform parent, string name, float height, UnityAction onClick, out TextMeshProUGUI label, float fontSize, TextAlignmentOptions align, float leftPadding)
	{
		RectTransform rect = MakeRect(parent, name);
		Image background = rect.gameObject.AddComponent<Image>();
		background.color = Surface;
		background.raycastTarget = true;
		Button button = rect.gameObject.AddComponent<Button>();
		button.targetGraphic = background;
		ApplyColors(button);
		if (onClick != null)
		{
			button.onClick.AddListener(onClick);
		}
		LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = height;
		layout.preferredHeight = height;
		RectTransform accent = MakeRect(rect, "Accent");
		accent.anchorMin = new Vector2(0f, 0f);
		accent.anchorMax = new Vector2(0f, 1f);
		accent.pivot = new Vector2(0f, 0.5f);
		accent.offsetMin = new Vector2(0f, 0f);
		accent.offsetMax = new Vector2(3f, 0f);
		Image accentImage = accent.gameObject.AddComponent<Image>();
		accentImage.color = Line;
		accentImage.raycastTarget = false;
		label = MakeLabelRaw(rect, "Label", "", fontSize, FontStyles.Bold, TextPrimary, align);
		Stretch(label.rectTransform, new Vector2(-leftPadding, -height * 0.26f), new Vector2(leftPadding, height * 0.26f));
		label.characterSpacing = 4f;
		label.enableAutoSizing = true;
		label.fontSizeMin = fontSize * 0.62f;
		label.fontSizeMax = fontSize;
		return button;
	}

	public static Button MakeButton(Transform parent, string name, string key, float height, UnityAction onClick, float fontSize = 22f, TextAlignmentOptions align = TextAlignmentOptions.Left, float leftPadding = 24f)
	{
		Button button = BuildButton(parent, name, height, onClick, out TextMeshProUGUI label, fontSize, align, leftPadding);
		LocalizedLabel localized = button.gameObject.AddComponent<LocalizedLabel>();
		localized.key = key;
		localized.Apply();
		return button;
	}

	public static Button MakeButtonRaw(Transform parent, string name, string text, float height, UnityAction onClick, float fontSize = 22f, TextAlignmentOptions align = TextAlignmentOptions.Center, float leftPadding = 16f)
	{
		Button button = BuildButton(parent, name, height, onClick, out TextMeshProUGUI label, fontSize, align, leftPadding);
		label.text = text;
		return button;
	}

	public static TextMeshProUGUI LabelOf(Button btn)
	{
		return btn.GetComponentInChildren<TextMeshProUGUI>();
	}

	public static Image AccentOf(Button btn)
	{
		Transform accent = btn.transform.Find("Accent");
		if (accent != null)
		{
			return accent.GetComponent<Image>();
		}
		return null;
	}

	public static void SetLocked(Button btn, bool locked, string badgeKey = "mode.soon")
	{
		btn.interactable = !locked;
		TextMeshProUGUI label = LabelOf(btn);
		if (label != null)
		{
			label.color = (locked ? TextLocked : TextPrimary);
		}
		Transform badge = btn.transform.Find("Badge");
		if (locked && badge == null && !string.IsNullOrEmpty(badgeKey))
		{
			TextMeshProUGUI badgeLabel = MakeLabelRaw(btn.transform, "Badge", "", 14f, FontStyles.Bold, TextLocked, TextAlignmentOptions.TopRight);
			LocalizedLabel localized = badgeLabel.gameObject.AddComponent<LocalizedLabel>();
			localized.key = badgeKey;
			localized.Apply();
			RectTransform badgeRect = badgeLabel.rectTransform;
			badgeRect.anchorMin = new Vector2(1f, 0.5f);
			badgeRect.anchorMax = new Vector2(1f, 0.5f);
			badgeRect.pivot = new Vector2(1f, 0.5f);
			badgeRect.anchoredPosition = new Vector2(-20f, 0f);
			badgeRect.sizeDelta = new Vector2(140f, 24f);
			badgeLabel.characterSpacing = 6f;
		}
		else if (!locked && badge != null)
		{
			Object.Destroy(badge.gameObject);
		}
	}

	public static void SetSelectedVisual(Button btn, bool selected)
	{
		Image image = btn.GetComponent<Image>();
		if (image != null)
		{
			image.color = (selected ? Selected : Surface);
		}
		Image accent = AccentOf(btn);
		if (accent != null)
		{
			accent.color = (selected ? TextPrimary : Line);
		}
	}

	public static RectTransform MakeColumn(Transform parent, string name, float spacing, int padL = 0, int padR = 0, int padT = 0, int padB = 0, TextAnchor align = TextAnchor.UpperCenter)
	{
		RectTransform rect = MakeRect(parent, name);
		VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
		layout.spacing = spacing;
		layout.padding = new RectOffset(padL, padR, padT, padB);
		layout.childAlignment = align;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = false;
		return rect;
	}

	public static RectTransform MakeRow(Transform parent, string name, float spacing, TextAnchor align = TextAnchor.MiddleCenter, bool forceExpandWidth = false)
	{
		RectTransform rect = MakeRect(parent, name);
		HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
		layout.spacing = spacing;
		layout.childAlignment = align;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = forceExpandWidth;
		layout.childForceExpandHeight = true;
		return rect;
	}

	public static RectTransform MakeSeparator(Transform parent, float height = 1f)
	{
		RectTransform rect = MakeRect(parent, "Separator");
		Image image = rect.gameObject.AddComponent<Image>();
		image.color = Line;
		image.raycastTarget = false;
		LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
		layout.minHeight = height;
		layout.preferredHeight = height;
		return rect;
	}

	public static ScrollRect MakeScrollList(Transform parent, string name, out RectTransform content)
	{
		RectTransform root = MakeRect(parent, name);
		ScrollRect scrollRect = root.gameObject.AddComponent<ScrollRect>();
		scrollRect.horizontal = false;
		scrollRect.vertical = true;
		scrollRect.movementType = ScrollRect.MovementType.Clamped;
		scrollRect.scrollSensitivity = 40f;
		RectTransform viewport = MakeRect(root, "Viewport");
		Stretch(viewport, Vector2.zero, new Vector2(-14f, 0f));
		viewport.gameObject.AddComponent<Mask>();
		Image viewportImage = viewport.gameObject.AddComponent<Image>();
		viewportImage.color = new Color(0f, 0f, 0f, 0f);
		viewportImage.raycastTarget = true;
		content = MakeRect(viewport, "Content");
		content.anchorMin = new Vector2(0f, 1f);
		content.anchorMax = Vector2.one;
		content.pivot = new Vector2(0.5f, 1f);
		content.anchoredPosition = Vector2.zero;
		content.sizeDelta = new Vector2(0f, 0f);
		VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
		contentLayout.spacing = 10f;
		contentLayout.childAlignment = TextAnchor.UpperCenter;
		contentLayout.childControlWidth = true;
		contentLayout.childControlHeight = true;
		contentLayout.childForceExpandWidth = true;
		contentLayout.childForceExpandHeight = false;
		content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		scrollRect.viewport = viewport;
		scrollRect.content = content;
		WheelZones.Register(root);
		RectTransform scrollbarRect = MakeRect(root, "Scrollbar");
		scrollbarRect.anchorMin = new Vector2(1f, 0f);
		scrollbarRect.anchorMax = Vector2.one;
		scrollbarRect.pivot = new Vector2(1f, 0.5f);
		scrollbarRect.offsetMin = new Vector2(-10f, 0f);
		scrollbarRect.offsetMax = Vector2.zero;
		Image scrollbarImage = scrollbarRect.gameObject.AddComponent<Image>();
		scrollbarImage.color = SurfaceAlt;
		scrollbarImage.raycastTarget = false;
		RectTransform handle = MakeRect(scrollbarRect, "Handle");
		Stretch(handle);
		Image handleImage = handle.gameObject.AddComponent<Image>();
		handleImage.color = Line;
		handleImage.raycastTarget = false;
		Scrollbar scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
		scrollbar.direction = Scrollbar.Direction.BottomToTop;
		scrollbar.handleRect = handle;
		scrollbar.targetGraphic = handle.GetComponent<Image>();
		handle.GetComponent<Image>().raycastTarget = true;
		scrollRect.verticalScrollbar = scrollbar;
		scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
		scrollRect.verticalScrollbarSpacing = 4f;
		return scrollRect;
	}
}
