using UnityEngine;
using UnityEngine.UI;

public sealed class SidePanelViewport : MonoBehaviour
{
	internal RectTransform panel;

	internal float width;

	internal float contentH;

	internal float marginY;

	private float _lastCanvasH = -1f;

	internal void Apply()
	{
		RectTransform rectTransform = (RectTransform)base.transform;
		if (panel == null)
		{
			return;
		}
		RectTransform rectTransform2 = SidePanelScroll.CanvasRectOf(rectTransform);
		if (rectTransform2 == null)
		{
			return;
		}
		Rect rect = rectTransform2.rect;
		_lastCanvasH = rect.height;
		float num = Mathf.Max(64f, rect.height - marginY * 2f);
		rectTransform.sizeDelta = new Vector2(width, num);
		float num2 = Mathf.Max(contentH, num);
		panel.anchorMin = new Vector2(0f, 1f);
		panel.anchorMax = new Vector2(1f, 1f);
		panel.pivot = new Vector2(0.5f, 1f);
		panel.sizeDelta = new Vector2(0f, num2);
		if (contentH > num)
		{
			panel.anchoredPosition = new Vector2(0f, Mathf.Clamp(panel.anchoredPosition.y, 0f, num2 - num));
		}
		else
		{
			panel.anchoredPosition = Vector2.zero;
		}
		ScrollRect component = rectTransform.GetComponent<ScrollRect>();
		if (component != null)
		{
			component.content = panel;
			component.viewport = rectTransform;
			component.StopMovement();
			component.enabled = contentH > num;
		}
	}

	private void LateUpdate()
	{
		if (panel == null)
		{
			return;
		}
		RectTransform rectTransform = (RectTransform)base.transform;
		Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
		if (canvas == null)
		{
			return;
		}
		RectTransform rectTransform2 = canvas.rootCanvas.transform as RectTransform;
		if (rectTransform2 == null)
		{
			return;
		}
		if (!Mathf.Approximately(_lastCanvasH, rectTransform2.rect.height))
		{
			Apply();
		}
	}
}
