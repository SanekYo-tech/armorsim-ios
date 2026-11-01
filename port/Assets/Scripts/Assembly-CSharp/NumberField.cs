using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class NumberField
{
	public class ClickCatcher : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler
	{
		public Action onClick;

		public TMP_Text label;

		private Color rest;

		private bool restTaken;

		public void OnPointerClick(PointerEventData e)
		{
			onClick?.Invoke();
		}

		public void OnPointerEnter(PointerEventData e)
		{
			if (label == null)
			{
				return;
			}
			if (!restTaken)
			{
				rest = label.color;
				restTaken = true;
			}
			label.color = MenuTheme.FieldHover;
		}

		public void OnPointerExit(PointerEventData e)
		{
			if (label == null)
			{
				return;
			}
			if (restTaken)
			{
				label.color = rest;
			}
		}
	}

	private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

	private static readonly HashSet<TMP_InputField> closing = new HashSet<TMP_InputField>();

	public static bool TypingNow
	{
		get
		{
			GameObject selected = ((EventSystem.current != null) ? EventSystem.current.currentSelectedGameObject : null);
			if (selected == null)
			{
				return false;
			}
			if (selected.GetComponent<TMPro.TMP_InputField>() != null)
			{
				return true;
			}
			return selected.GetComponent<UnityEngine.UI.InputField>() != null;
		}
	}

	public static bool TryParse(string s, out float value)
	{
		value = 0f;
		if (string.IsNullOrEmpty(s))
		{
			return false;
		}
		string t = s.Trim();
		t = t.Replace(',', '.');
		t = t.Replace(" ", "");
		t = t.Replace("\u00a0", "");
		if (t.Length == 0)
		{
			return false;
		}
		return float.TryParse(t, NumberStyles.Float, Inv, out value);
	}

	public static string ToText(float value, string format)
	{
		if (string.IsNullOrEmpty(format))
		{
			format = "0.###";
		}
		return value.ToString(format, Inv);
	}

	public static TMP_InputField Attach(TMP_Text label, Func<float> read, Action<float> apply, string format)
	{
		if (label == null || read == null || apply == null)
		{
			return null;
		}
		TMP_InputField field = Build(label);
		if (field == null)
		{
			return null;
		}
		ClickCatcher catcher = label.gameObject.GetComponent<ClickCatcher>();
		if (catcher == null)
		{
			catcher = label.gameObject.AddComponent<ClickCatcher>();
		}
		catcher.label = label;
		catcher.onClick = delegate
		{
			Open(field, read(), format);
		};
		field.onSubmit.AddListener(delegate(string text)
		{
			if (field.wasCanceled)
			{
				return;
			}
			if (TryParse(text, out float value))
			{
				apply(value);
			}
		});
		field.onEndEdit.AddListener(delegate(string _)
		{
			if (!closing.Contains(field))
			{
				Close(field);
			}
		});
		return field;
	}

	public static void Open(TMP_InputField field, float value, string format)
	{
		if (field == null)
		{
			return;
		}
		field.gameObject.SetActive(true);
		field.SetTextWithoutNotify(ToText(value, format));
		field.Select();
		field.ActivateInputField();
	}

	public static void Close(TMP_InputField field)
	{
		if (field == null)
		{
			return;
		}
		if (field.gameObject == null)
		{
			return;
		}
		if (!field.gameObject.activeSelf)
		{
			return;
		}
		closing.Add(field);
		if (EventSystem.current != null && !EventSystem.current.alreadySelecting && EventSystem.current.currentSelectedGameObject == field.gameObject)
		{
			EventSystem.current.SetSelectedGameObject(null);
		}
		field.DeactivateInputField();
		closing.Remove(field);
		field.gameObject.SetActive(false);
	}

	private static TMP_InputField Build(TMP_Text label)
	{
		RectTransform rt = label.rectTransform;
		Transform parent = rt.parent;
		GameObject root = new GameObject("NumberInput", typeof(RectTransform), typeof(Image));
		root.transform.SetParent(parent, false);
		root.transform.SetSiblingIndex(rt.GetSiblingIndex() + 1);
		RectTransform rootRt = (RectTransform)root.transform;
		rootRt.anchorMin = rt.anchorMin;
		rootRt.anchorMax = rt.anchorMax;
		rootRt.pivot = rt.pivot;
		rootRt.sizeDelta = rt.sizeDelta;
		rootRt.anchoredPosition = rt.anchoredPosition;
		Image image = root.GetComponent<Image>();
		image.color = new Color(0.05f, 0.06f, 0.08f, 0.96f);
		GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
		textArea.transform.SetParent(root.transform, false);
		RectTransform textAreaRt = (RectTransform)textArea.transform;
		textAreaRt.anchorMin = Vector2.zero;
		textAreaRt.anchorMax = Vector2.one;
		textAreaRt.offsetMin = new Vector2(4f, 1f);
		textAreaRt.offsetMax = new Vector2(-4f, -1f);
		GameObject textGo = new GameObject("Text", typeof(TextMeshProUGUI));
		textGo.transform.SetParent(textArea.transform, false);
		TMP_Text text = textGo.GetComponent<TMP_Text>();
		text.text = "";
		text.fontSize = label.fontSize;
		text.color = Color.white;
		text.alignment = TextAlignmentOptions.Left;
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.richText = false;
		RectTransform textRt = text.rectTransform;
		textRt.anchorMin = Vector2.zero;
		textRt.anchorMax = Vector2.one;
		textRt.offsetMin = Vector2.zero;
		textRt.offsetMax = Vector2.zero;
		TMP_InputField input = root.AddComponent<TMP_InputField>();
		input.targetGraphic = image;
		input.textViewport = textAreaRt;
		input.textComponent = text;
		input.lineType = TMP_InputField.LineType.SingleLine;
		input.richText = false;
		input.onFocusSelectAll = true;
		input.restoreOriginalTextOnEscape = true;
		input.caretColor = Color.white;
		input.customCaretColor = true;
		input.selectionColor = new Color(0.3f, 0.52f, 1f, 0.55f);
		root.SetActive(false);
		return input;
	}
}
