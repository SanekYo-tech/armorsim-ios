using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultDisplay : MonoBehaviour
{
	public static ResultDisplay Instance;

	private TMP_Text resultText;

	private CanvasGroup group;

	private RectTransform panelRT;

	private RectTransform canvasRT;

	private void Awake()
	{
		Instance = this;
		if (panelRT == null)
		{
			BuildUI();
		}
	}

	public void Build()
	{
		if (panelRT == null)
		{
			BuildUI();
		}
	}

	private void BuildUI()
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
		canvasRT = canvas.GetComponent<RectTransform>();
		GameObject gameObject2 = new GameObject("ResultPanel", typeof(RectTransform), typeof(CanvasGroup));
		gameObject2.transform.SetParent(canvas.transform, worldPositionStays: false);
		group = gameObject2.GetComponent<CanvasGroup>();
		group.alpha = 0f;
		panelRT = gameObject2.GetComponent<RectTransform>();
		panelRT.anchorMin = new Vector2(0.5f, 0.5f);
		panelRT.anchorMax = new Vector2(0.5f, 0.5f);
		panelRT.pivot = new Vector2(0.5f, 0.5f);
		panelRT.sizeDelta = new Vector2(600f, 60f);
		panelRT.anchoredPosition = new Vector2(0f, -30f);
		GameObject gameObject3 = new GameObject("ResultText", typeof(TextMeshProUGUI));
		gameObject3.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		resultText = gameObject3.GetComponent<TMP_Text>();
		resultText.fontSize = 28f;
		resultText.alignment = TextAlignmentOptions.Center;
		resultText.fontStyle = FontStyles.Bold;
		resultText.textWrappingMode = TextWrappingModes.Normal;
		resultText.overflowMode = TextOverflowModes.Overflow;
		resultText.raycastTarget = false;
		RectTransform component = gameObject3.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = new Vector2(12f, 12f);
		component.offsetMax = new Vector2(-12f, -12f);
		component.anchoredPosition = Vector2.zero;
	}

	public void Show(string message, Color color)
	{
		if (resultText == null)
		{
			return;
		}
		resultText.text = message;
		resultText.color = color;
		FitToText(message);
		if (group != null)
		{
			group.alpha = 1f;
		}
	}

	private void FitToText(string message)
	{
		if (panelRT == null)
		{
			return;
		}
		Vector2 sizeDelta = panelRT.sizeDelta;
		resultText.enableAutoSizing = false;
		resultText.fontSize = 28f;
		Vector2 vector = resultText.GetPreferredValues(message, 28f, sizeDelta.x - 24f);
		float num = ((canvasRT != null) ? canvasRT.rect.height : Screen.height);
		float num2 = vector.y + 24f;
		float num3 = 60f;
		bool flag = true;
		bool flag2 = false;
		if (num3 < num2)
		{
			num3 = Mathf.Max(num3, num * 0.33333334f);
			flag2 = num2 > num3;
			if (!flag2)
			{
				num3 = num2;
			}
		}
		else
		{
			flag = false;
		}
		panelRT.sizeDelta = new Vector2(panelRT.sizeDelta.x, num3);
		resultText.alignment = (flag ? TextAlignmentOptions.Top : TextAlignmentOptions.Center);
		if (flag2)
		{
			resultText.enableAutoSizing = true;
			resultText.fontSizeMin = 20f;
			resultText.fontSizeMax = 28f;
		}
	}

	public void Hide()
	{
		if (group != null)
		{
			group.alpha = 0f;
		}
	}
}
