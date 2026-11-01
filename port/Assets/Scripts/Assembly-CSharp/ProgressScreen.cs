using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressScreen : MonoBehaviour
{
	public static ProgressScreen Instance;

	private CanvasGroup group;

	private TMP_Text label;

	private Image barFill;

	private void Awake()
	{
		Instance = this;
		Build();
	}

	public static ProgressScreen FindOrCreate()
	{
		if (Instance != null)
		{
			return Instance;
		}
		ProgressScreen progressScreen = Object.FindFirstObjectByType<ProgressScreen>();
		if (progressScreen == null)
		{
			GameObject gameObject = new GameObject("ProgressScreen");
			progressScreen = gameObject.AddComponent<ProgressScreen>();
		}
		Instance = progressScreen;
		return progressScreen;
	}

	private void Build()
	{
		Canvas canvas = Object.FindFirstObjectByType<Canvas>();
		if (canvas == null)
		{
			GameObject gameObject = new GameObject("Canvas_Auto");
			canvas = gameObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
			canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
			canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			canvasScaler.matchWidthOrHeight = 0.5f;
			gameObject.AddComponent<GraphicRaycaster>();
		}
		GameObject gameObject2 = new GameObject("ProgressPanel", typeof(RectTransform), typeof(CanvasGroup));
		gameObject2.transform.SetParent(canvas.transform, worldPositionStays: false);
		group = gameObject2.GetComponent<CanvasGroup>();
		group.alpha = 0f;
		group.blocksRaycasts = false;
		RectTransform rectTransform = gameObject2.GetComponent<RectTransform>();
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.sizeDelta = new Vector2(420f, 90f);
		GameObject gameObject3 = new GameObject("ProgressText", typeof(TextMeshProUGUI));
		gameObject3.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		label = gameObject3.GetComponent<TMP_Text>();
		label.fontSize = 26f;
		label.alignment = TextAlignmentOptions.Center;
		label.fontStyle = FontStyles.Bold;
		RectTransform rectTransform2 = gameObject3.GetComponent<RectTransform>();
		rectTransform2.anchorMin = new Vector2(0f, 0.5f);
		rectTransform2.anchorMax = new Vector2(1f, 1f);
		rectTransform2.sizeDelta = Vector2.zero;
		rectTransform2.anchoredPosition = Vector2.zero;
		GameObject gameObject4 = new GameObject("BarBg", typeof(Image));
		gameObject4.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		gameObject4.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.22f, 0.9f);
		RectTransform rectTransform3 = gameObject4.GetComponent<RectTransform>();
		rectTransform3.anchorMin = new Vector2(0f, 0f);
		rectTransform3.anchorMax = new Vector2(1f, 0.35f);
		rectTransform3.sizeDelta = new Vector2(-20f, 0f);
		rectTransform3.anchoredPosition = new Vector2(0f, 8f);
		GameObject gameObject5 = new GameObject("BarFill", typeof(Image));
		gameObject5.transform.SetParent(gameObject4.transform, worldPositionStays: false);
		barFill = gameObject5.GetComponent<Image>();
		barFill.color = new Color(0.5f, 0.8f, 0.55f, 1f);
		barFill.type = Image.Type.Filled;
		barFill.fillMethod = Image.FillMethod.Horizontal;
		barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
		RectTransform rectTransform4 = gameObject5.GetComponent<RectTransform>();
		rectTransform4.anchorMin = Vector2.zero;
		rectTransform4.anchorMax = Vector2.one;
		rectTransform4.sizeDelta = Vector2.zero;
		rectTransform4.anchoredPosition = Vector2.zero;
	}

	public void Show()
	{
		if (group != null)
		{
			group.alpha = 1f;
		}
		Set(0f);
	}

	public void Hide()
	{
		if (group != null)
		{
			group.alpha = 0f;
		}
	}

	public void Set(float progress01)
	{
		if (progress01 < 0f)
		{
			progress01 = 0f;
		}
		else if (progress01 > 1f)
		{
			progress01 = 1f;
		}
		if (label != null)
		{
			label.text = string.Format("{0} {1}%", Localization.T("calc.title"), Mathf.RoundToInt(progress01 * 100f));
		}
		if (barFill != null)
		{
			barFill.fillAmount = progress01;
		}
	}
}
