using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CatalogGrid
{
	private class HoverTip : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		public Action onEnter;

		public Action onExit;

		public void OnPointerEnter(PointerEventData eventData)
		{
			onEnter?.Invoke();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			onExit?.Invoke();
		}
	}

	private const float HeaderHeight = 14f;

	private const float RowHeight = 24f;

	private const float RowGap = 2f;

	private const float GroupGap = 5f;

	private const float ColumnGap = 4f;

	private const float MinButtonWidth = 86f;

	private const int MaxColumns = 4;

	private readonly Dictionary<string, Image> buttons = new Dictionary<string, Image>();

	private readonly Dictionary<string, TMP_Text> labels = new Dictionary<string, TMP_Text>();

	private readonly Dictionary<string, string> tooltips = new Dictionary<string, string>();

	private readonly Dictionary<string, string> refused = new Dictionary<string, string>();

	private readonly Dictionary<string, string> notes = new Dictionary<string, string>();

	private Action<string> onTooltip;

	private string selectedId = string.Empty;

	private static readonly List<CatalogGrid> live = new List<CatalogGrid>();

	public static Color ButtonColor => MenuTheme.SuperBenchActive ? MenuTheme.Pressed : new Color(0.25f, 0.25f, 0.3f, 1f);

	public static Color SelectedColor => MenuTheme.SuperBenchActive ? MenuTheme.Accent : new Color(0.3f, 0.6f, 1f, 1f);

	private static Color HeaderColor => MenuTheme.SuperBenchActive ? MenuTheme.TextMuted : new Color(0.62f, 0.66f, 0.72f, 1f);

	public float EndY { get; private set; }

	public static IReadOnlyList<CatalogGrid> Live
	{
		get
		{
			live.RemoveAll((CatalogGrid g) => !g.Alive);
			return live;
		}
	}

	private bool Alive
	{
		get
		{
			foreach (Image image in buttons.Values)
			{
				return image != null;
			}
			return false;
		}
	}

	public IEnumerable<string> Ids => buttons.Keys;

	public RectTransform ButtonRect(string id)
	{
		Image image;
		if (buttons.TryGetValue(id, out image) && image != null)
		{
			return (RectTransform)image.transform;
		}
		return null;
	}

	public TMP_Text ButtonLabel(string id)
	{
		TMP_Text label;
		return labels.TryGetValue(id, out label) ? label : null;
	}

	public void SetRefused(string id, string reason)
	{
		if (id == null || !buttons.ContainsKey(id))
		{
			return;
		}
		bool hadReason = refused.ContainsKey(id);
		if (reason == null)
		{
			if (!hadReason)
			{
				return;
			}
			refused.Remove(id);
		}
		else
		{
			if (hadReason && refused[id] == reason)
			{
				return;
			}
			refused[id] = reason;
		}
		Paint(id);
		if (id == selectedId)
		{
			ShowTooltip(selectedId);
		}
	}

	public bool IsRefused(string id)
	{
		return id != null && refused.ContainsKey(id);
	}

	public void SetNote(string id, string note)
	{
		if (id == null || !buttons.ContainsKey(id))
		{
			return;
		}
		bool hadNote = notes.ContainsKey(id);
		if (string.IsNullOrEmpty(note))
		{
			if (!hadNote)
			{
				return;
			}
			notes.Remove(id);
		}
		else
		{
			if (hadNote && notes[id] == note)
			{
				return;
			}
			notes[id] = note;
		}
		if (id == selectedId)
		{
			ShowTooltip(selectedId);
		}
	}

	public string NoteOf(string id)
	{
		string note;
		if (id != null && notes.TryGetValue(id, out note))
		{
			return note;
		}
		return null;
	}

	private void Paint(string id)
	{
		Image image;
		if (!buttons.TryGetValue(id, out image) || image == null)
		{
			return;
		}
		bool isRefused = refused.ContainsKey(id);
		bool isSelected = id == selectedId;
		image.color = (isRefused ? MenuTheme.Refused : (isSelected ? SelectedColor : ButtonColor));
		TMP_Text label;
		if (labels.TryGetValue(id, out label) && label != null)
		{
			label.color = (isRefused ? MenuTheme.RefusedText : Color.white);
			label.fontStyle = ((isRefused && isSelected) ? FontStyles.Bold : FontStyles.Normal);
		}
	}

	public static CatalogGrid Build(GameObject panel, CatalogEntry[] entries, float topY, float panelWidth, float sidePad, Action<string> onPick, Action<string> onTooltip, int forceColumns = 0, float rowHeightOverride = 0f)
	{
		CatalogGrid grid = new CatalogGrid();
		grid.onTooltip = onTooltip;
		float rowHeight = ((rowHeightOverride > 0f) ? rowHeightOverride : RowHeight);
		float usableWidth = panelWidth - sidePad - sidePad;
		if (usableWidth <= MinButtonWidth)
		{
			usableWidth = MinButtonWidth;
		}
		int columns = forceColumns;
		if (columns < 1)
		{
			columns = (int)Math.Floor((double)(usableWidth / MinButtonWidth));
			if (columns < 1)
			{
				columns = 1;
			}
			else if (columns > MaxColumns)
			{
				columns = MaxColumns;
			}
		}
		List<string> categories = new List<string>();
		for (int i = 0; i < entries.Length; i++)
		{
			if (!categories.Contains(entries[i].category))
			{
				categories.Add(entries[i].category);
			}
		}
		foreach (string category in categories)
		{
			List<CatalogEntry> group = new List<CatalogEntry>();
			for (int j = 0; j < entries.Length; j++)
			{
				if (entries[j].category == category)
				{
					group.Add(entries[j]);
				}
			}
			if (group.Count == 0)
			{
				continue;
			}
			grid.MakeHeader(panel, group[0].CategoryName, topY, sidePad);
			topY += -16f;
			for (int k = 0; k < group.Count; k++)
			{
				grid.MakeEntryButton(panel, group[k], topY, k % columns, columns, sidePad, onPick, rowHeight);
				if (k % columns == columns - 1 || k == group.Count - 1)
				{
					topY -= rowHeight + RowGap;
				}
			}
			topY += -GroupGap;
		}
		grid.EndY = topY;
		live.Add(grid);
		return grid;
	}

	public void SetSelected(string id)
	{
		selectedId = id ?? string.Empty;
		foreach (string key in buttons.Keys)
		{
			Paint(key);
		}
		ShowTooltip(selectedId);
	}

	public void SetInteractable(bool on)
	{
		foreach (Image image in buttons.Values)
		{
			Button button = image.GetComponent<Button>();
			if (button != null)
			{
				button.interactable = on;
			}
		}
	}

	private void MakeHeader(GameObject panel, string text, float y, float sidePad)
	{
		GameObject headerObject = new GameObject("Header_" + text, typeof(TextMeshProUGUI));
		headerObject.transform.SetParent(panel.transform, false);
		TextMeshProUGUI label = headerObject.GetComponent<TextMeshProUGUI>();
		label.text = text.ToUpperInvariant();
		label.fontSize = 11f;
		label.alignment = TextAlignmentOptions.Left;
		label.color = HeaderColor;
		label.textWrappingMode = TextWrappingModes.NoWrap;
		label.overflowMode = TextOverflowModes.Ellipsis;
		RectTransform rect = headerObject.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0f, 1f);
		rect.anchorMax = new Vector2(1f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.offsetMin = new Vector2(sidePad, y - HeaderHeight);
		rect.offsetMax = new Vector2(-sidePad, y);
	}

	private void MakeEntryButton(GameObject panel, CatalogEntry entry, float y, int col, int cols, float sidePad, Action<string> onPick, float rowHeight = 0f)
	{
		string id = entry.id;
		if (rowHeight <= 0f)
		{
			rowHeight = RowHeight;
		}
		GameObject buttonObject = new GameObject("Btn_" + entry.id, typeof(Image), typeof(Button));
		buttonObject.transform.SetParent(panel.transform, false);
		Image image = buttonObject.GetComponent<Image>();
		image.color = ButtonColor;
		buttonObject.GetComponent<Button>().onClick.AddListener(delegate
		{
			onPick(id);
		});
		float left = (float)col / (float)cols;
		float right = (float)(col + 1) / (float)cols;
		RectTransform rect = buttonObject.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(left, 1f);
		rect.anchorMax = new Vector2(right, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.offsetMin = new Vector2((1f - left - left) * sidePad + ColumnGap * 0.5f, y - rowHeight);
		rect.offsetMax = new Vector2((right + right - 1f) * -sidePad - ColumnGap * 0.5f, y);
		GameObject textObject = new GameObject("Text", typeof(TextMeshProUGUI));
		textObject.transform.SetParent(buttonObject.transform, false);
		TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
		label.text = entry.Name;
		label.alignment = TextAlignmentOptions.Center;
		label.color = Color.white;
		label.textWrappingMode = TextWrappingModes.Normal;
		label.enableAutoSizing = true;
		label.fontSizeMin = 7f;
		label.fontSizeMax = 12f;
		RectTransform textRect = textObject.GetComponent<RectTransform>();
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.offsetMin = new Vector2(2f, 1f);
		textRect.offsetMax = new Vector2(-2f, -1f);
		buttons[entry.id] = image;
		labels[entry.id] = label;
		tooltips[entry.id] = entry.Tip;
		HoverTip hover = buttonObject.AddComponent<HoverTip>();
		hover.onEnter = delegate
		{
			ShowTooltip(id);
		};
		hover.onExit = delegate
		{
			ShowTooltip(selectedId);
		};
	}

	private void ShowTooltip(string id)
	{
		if (onTooltip == null)
		{
			return;
		}
		string tip;
		string text = ((id != null && tooltips.TryGetValue(id, out tip)) ? tip : string.Empty);
		if (id != null)
		{
			string extra;
			if (!refused.TryGetValue(id, out extra) || string.IsNullOrEmpty(extra))
			{
				if (!notes.TryGetValue(id, out extra) || string.IsNullOrEmpty(extra))
				{
					onTooltip(text);
					return;
				}
			}
			text = extra + (string.IsNullOrEmpty(text) ? string.Empty : "\n" + text);
		}
		onTooltip(text);
	}
}
