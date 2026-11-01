using System;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UI;

public static class SidePanelScroll
{
	public const string ViewportSuffix = "_Viewport";

	public static void Fit(RectTransform panel, float marginY)
	{
		if (panel == null || CanvasRectOf(panel) == null)
		{
			return;
		}
		float num = panel.sizeDelta.x;
		float num2 = panel.sizeDelta.y;
		RectTransform rectTransform = EnsureViewport(panel);
		if (rectTransform == null)
		{
			return;
		}
		SidePanelViewport sidePanelViewport = rectTransform.GetComponent<SidePanelViewport>();
		if (sidePanelViewport == null)
		{
			sidePanelViewport = rectTransform.gameObject.AddComponent<SidePanelViewport>();
		}
		sidePanelViewport.panel = panel;
		sidePanelViewport.width = num;
		sidePanelViewport.contentH = num2;
		sidePanelViewport.marginY = marginY;
		sidePanelViewport.Apply();
		WheelZones.Register(rectTransform);
	}

	public static void DestroyPanel(GameObject panel)
	{
		if (panel == null)
		{
			return;
		}
		GameObject gameObject = panel;
		Transform parent = panel.transform.parent;
		if (parent != null && parent.name.EndsWith(ViewportSuffix, StringComparison.Ordinal))
		{
			gameObject = parent.gameObject;
		}
		Object.Destroy(gameObject);
	}

	private static RectTransform EnsureViewport(RectTransform panel)
	{
		Transform parent = panel.parent;
		if (parent != null && parent.name.EndsWith(ViewportSuffix, StringComparison.Ordinal))
		{
			return parent as RectTransform;
		}
		if (parent == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject(panel.name + ViewportSuffix, typeof(RectTransform));
		RectTransform rectTransform = (RectTransform)gameObject.transform;
		rectTransform.SetParent(parent, worldPositionStays: false);
		rectTransform.anchorMin = panel.anchorMin;
		rectTransform.anchorMax = panel.anchorMax;
		rectTransform.pivot = panel.pivot;
		rectTransform.anchoredPosition = panel.anchoredPosition;
		rectTransform.SetSiblingIndex(panel.GetSiblingIndex());
		gameObject.AddComponent<Mask>();
		ScrollRect scrollRect = gameObject.AddComponent<ScrollRect>();
		scrollRect.horizontal = false;
		scrollRect.movementType = ScrollRect.MovementType.Clamped;
		scrollRect.scrollSensitivity = 30f;
		scrollRect.inertia = false;
		panel.SetParent(rectTransform, worldPositionStays: false);
		return rectTransform;
	}

	internal static RectTransform CanvasRectOf(RectTransform rt)
	{
		Canvas canvas = rt.GetComponentInParent<Canvas>();
		if (canvas == null)
		{
			return null;
		}
		return canvas.rootCanvas.transform as RectTransform;
	}
}
