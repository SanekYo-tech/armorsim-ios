using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RadarScenePlaceholder : MonoBehaviour
{
	private void Start()
	{
		RadarFacts facts = default(RadarFacts);
		facts.implemented = true;
		facts.ownership = Entitlements.State(ContentPack.Radar);
		facts.gateOpen = true;
		facts.freeAccess = true;
		RadarAccessState radarAccessState = RadarRoute.State(facts);
		switch (radarAccessState)
		{
		case RadarAccessState.NotImplemented:
		case RadarAccessState.NotOwned:
		case RadarAccessState.Unverified:
		case RadarAccessState.GateClosed:
			Debug.Log("[RadarSim] Access unavailable; returning to menu.");
			AppFlow.OpenMenu();
			return;
		case RadarAccessState.Open:
			break;
		default:
			Debug.LogError(string.Format("[RadarRoute] Unknown access state {0}; entry refused.", radarAccessState));
			Debug.Log("[RadarSim] Access unavailable; returning to menu.");
			AppFlow.OpenMenu();
			return;
		}
		if (EventSystem.current == null)
		{
			GameObject eventSystem = new GameObject("Radar EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
			eventSystem.transform.SetParent(transform, false);
		}
		RectTransform rectTransform = MenuTheme.MakeRect(transform, "RadarCanvas");
		rectTransform.gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
		rectTransform.gameObject.AddComponent<GraphicRaycaster>();
		CanvasScaler canvasScaler = rectTransform.gameObject.AddComponent<CanvasScaler>();
		canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
		canvasScaler.matchWidthOrHeight = 0.5f;
		RectTransform rectTransform2 = MenuTheme.MakeRect(rectTransform, "Background");
		MenuTheme.Stretch(rectTransform2);
		MenuTheme.AddImage(rectTransform2, ResultPalette.Background);
		RectTransform rectTransform3 = MenuTheme.MakeRect(rectTransform, "Panel");
		MenuTheme.Place(rectTransform3, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 300f));
		MenuTheme.AddImage(rectTransform3, MenuTheme.SurfaceAlt);
		TextMeshProUGUI textMeshProUGUI = MenuTheme.MakeLabel(rectTransform3, "Placeholder", "radar.placeholder", 28f, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.Center);
		if (textMeshProUGUI != null)
		{
			MenuTheme.Place(textMeshProUGUI.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1360f, 120f));
		}
		Button button = MenuTheme.MakeButton(rectTransform3, "Radar_Menu", "radar.menu", 56f, AppFlow.OpenMenu, 22f, TextAlignmentOptions.Center, 12f);
		if (button != null)
		{
			MenuTheme.Place(button.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(320f, 56f));
		}
	}
}