using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class AeroMenuScroll : MonoBehaviour
{
	private ScrollRect scroll;

	private readonly List<Button> buttons;

	private readonly List<UnityAction> actions;

	private readonly List<AeroMenuCardFocus> focusHandlers;

	public AeroMenuScroll()
	{
		buttons = new List<Button>();
		actions = new List<UnityAction>();
		focusHandlers = new List<AeroMenuCardFocus>();
	}

	public void Configure(ScrollRect target)
	{
		Unbind();
		scroll = target;
		Button[] componentsInChildren = target.content.GetComponentsInChildren<Button>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Button button = componentsInChildren[i];
			RectTransform rectTransform = button.transform as RectTransform;
			while (rectTransform != null && rectTransform.parent != target.content)
			{
				rectTransform = rectTransform.parent as RectTransform;
			}
			if (rectTransform == null)
			{
				continue;
			}
			RectTransform captured = rectTransform;
			UnityAction unityAction = delegate
			{
				Reveal(captured);
			};
			button.onClick.AddListener(unityAction);
			buttons.Add(button);
			actions.Add(unityAction);
			AeroMenuCardFocus aeroMenuCardFocus = button.GetComponent<AeroMenuCardFocus>();
			if (aeroMenuCardFocus == null)
			{
				aeroMenuCardFocus = button.gameObject.AddComponent<AeroMenuCardFocus>();
			}
			aeroMenuCardFocus.owner = this;
			aeroMenuCardFocus.card = rectTransform;
			focusHandlers.Add(aeroMenuCardFocus);
		}
	}

	public void Reveal(RectTransform card)
	{
		if (scroll == null || card == null)
		{
			return;
		}
		if (!scroll.gameObject.activeInHierarchy)
		{
			return;
		}
		Canvas.ForceUpdateCanvases();
		LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
		Vector3[] array = new Vector3[4];
		card.GetWorldCorners(array);
		float y = scroll.viewport.InverseTransformPoint(array[0]).y;
		float y2 = scroll.viewport.InverseTransformPoint(array[1]).y;
		Rect rect = scroll.viewport.rect;
		float num;
		if (rect.y <= y)
		{
			if (y2 <= rect.yMax)
			{
				num = 0f;
			}
			else
			{
				num = rect.yMax - y2;
			}
		}
		else
		{
			num = rect.y - y;
		}
		float max = Mathf.Max(0f, scroll.content.rect.height - scroll.viewport.rect.height);
		Vector2 anchoredPosition = scroll.content.anchoredPosition;
		anchoredPosition.y = Mathf.Clamp(anchoredPosition.y + num, 0f, max);
		scroll.StopMovement();
		scroll.content.anchoredPosition = anchoredPosition;
	}

	private void Unbind()
	{
		for (int i = 0; i < buttons.Count; i++)
		{
			if (buttons[i] != null)
			{
				buttons[i].onClick.RemoveListener(actions[i]);
			}
		}
		foreach (AeroMenuCardFocus focusHandler in focusHandlers)
		{
			if (focusHandler != null)
			{
				focusHandler.owner = null;
				focusHandler.card = null;
			}
		}
		buttons.Clear();
		actions.Clear();
		focusHandlers.Clear();
	}

	private void OnDestroy()
	{
		Unbind();
	}
}